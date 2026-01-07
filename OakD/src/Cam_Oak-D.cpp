#include <iostream>
#include "depthai/depthai.hpp"
#include "depthai/pipeline/datatype/StereoDepthConfig.hpp"
#include "depthai/pipeline/node/StereoDepth.hpp"
#include "depthai/properties/StereoDepthProperties.hpp"
#include "../CPP_Native/PragmaLibs.h"

using namespace std;
using namespace cv;

constexpr float FPS = 60.0f;

// Closer-in minimum depth, disparity range is doubled (from 95 to 190):
static std::atomic<bool> extended_disparity{ false };
// Better accuracy for longer distance, fractional disparity 32-levels:
static std::atomic<bool> subpixel{ true };
// Better handling for occlusions:
static std::atomic<bool> lr_check{ true };

class OakDCamera
{
private:
public:
	dai::Pipeline pipeline;
	std::shared_ptr<dai::Device> device; 
	Mat rgb, leftView, rightView, depth16u, disparity;
	dai::CalibrationHandler deviceCalib;
	double imuTimeStamp;
	bool firstTs = false;
	std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration> baseTs; int rows, cols;

	float intrinsics[9];
	float extrinsicsRGBtoLeft[12];
	float extrinsicsLeftToRight[12];
	
	// v3 Camera nodes (replacing ColorCamera and MonoCamera)
	std::shared_ptr<dai::node::Camera> pipeRGB;
	std::shared_ptr<dai::node::Camera> pipeLeft;
	std::shared_ptr<dai::node::Camera> pipeRight;
	std::shared_ptr<dai::node::StereoDepth> pipeStereo;
	std::shared_ptr<dai::node::Sync> pipeSync;
	std::shared_ptr<dai::node::IMU> pipeIMU;
	
	std::shared_ptr<dai::MessageQueue> qRGB;
	std::shared_ptr<dai::MessageQueue> qLeft;
	std::shared_ptr<dai::MessageQueue> qRight;
	std::shared_ptr<dai::MessageQueue> qMessage;
	std::shared_ptr<dai::MessageQueue> qIMU;
	dai::Node::Output* outRGB;
	dai::Node::Output* outLeft;
	dai::Node::Output* outRight;

	dai::IMUReportAccelerometer acceleroValues;
	dai::IMUReportGyroscope gyroValues;

	OakDCamera(int _cols, int _rows)
	{
		rows = _rows;
		cols = _cols;

		pipeRGB = pipeline.create<dai::node::Camera>()->build(dai::CameraBoardSocket::CAM_A);
		pipeLeft = pipeline.create<dai::node::Camera>()->build(dai::CameraBoardSocket::CAM_B);
		pipeRight = pipeline.create<dai::node::Camera>()->build(dai::CameraBoardSocket::CAM_C);
		pipeStereo = pipeline.create<dai::node::StereoDepth>();
		pipeSync = pipeline.create<dai::node::Sync>();
		pipeIMU = pipeline.create<dai::node::IMU>();

		// Check if platform is RVC4 and create ImageAlign node if needed
		auto platform = pipeline.getDefaultDevice()->getPlatform();
		std::shared_ptr<dai::node::ImageAlign> align;
		if (platform == dai::Platform::RVC4) {
			align = pipeline.create<dai::node::ImageAlign>();
		}

		// Enable ACCELEROMETER_RAW at 480 hz rate
		pipeIMU->enableIMUSensor(dai::IMUSensor::ACCELEROMETER_RAW, 480);
		// Enable GYROSCOPE_RAW at 400 hz rate
		pipeIMU->enableIMUSensor(dai::IMUSensor::GYROSCOPE_RAW, 400);

		// Set batch report threshold and max batch reports
		pipeIMU->setBatchReportThreshold(1);
		pipeIMU->setMaxBatchReports(10);

		outRGB = pipeRGB->requestOutput({ cols, rows }, dai::ImgFrame::Type::BGR888i);
		outLeft = pipeLeft->requestOutput({ cols, rows });
		outRight = pipeRight->requestOutput({ cols, rows });

		pipeStereo->setExtendedDisparity(extended_disparity);
		pipeStereo->setSubpixel(subpixel);

		pipeSync->setSyncThreshold(std::chrono::duration<int64_t, std::nano>(static_cast<int64_t>(1e9 / (2.0 * FPS))));

		qRGB = outRGB->createOutputQueue();
		qLeft = outLeft->createOutputQueue();
		qRight = outRight->createOutputQueue();
		qIMU = pipeIMU->out.createOutputQueue(50, false);

		// Link nodes
		outRGB->link(pipeSync->inputs["rgb"]);
		outLeft->link(pipeStereo->left);
		outRight->link(pipeStereo->right);

		if (platform == dai::Platform::RVC4) {
			pipeStereo->depth.link(align->input);
			outRGB->link(align->inputAlignTo);
			align->outputAligned.link(pipeSync->inputs["depth_aligned"]);
		}
		else {
			pipeStereo->depth.link(pipeSync->inputs["depth_aligned"]);
			outRGB->link(pipeStereo->inputAlignTo);
		}

		// Create output queue
		qMessage = pipeSync->out.createOutputQueue();
		pipeline.start();

		device = pipeline.getDefaultDevice();
		deviceCalib = device->readCalibration();
		baseTs = std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration>();
	}

	void waitForFrame()
	{
		auto messageGroup = qMessage->get<dai::MessageGroup>();

		auto inRGB = qRGB->get<dai::ImgFrame>();
		auto inLeft = qLeft->get<dai::ImgFrame>();
		auto inRight = qRight->get<dai::ImgFrame>();
		auto inDepth = messageGroup->get<dai::ImgFrame>("depth_aligned");

		// Get frames and resize to desired output resolution
		rgb = inRGB->getFrame().clone();
		leftView = inLeft->getFrame().clone();
		rightView = inRight->getFrame().clone();
		
		depth16u = inDepth->getFrame().clone();

		auto imuData = qIMU->get<dai::IMUData>();
		if (imuData != nullptr)
		{
			for (const auto& imuPacket : imuData->packets) {
				acceleroValues = imuPacket.acceleroMeter;
				gyroValues = imuPacket.gyroscope;

				auto acceleroTs = acceleroValues.getTimestamp();
				auto gyroTs = gyroValues.getTimestamp();
				
				if (!firstTs) {
					baseTs = std::min(acceleroTs, gyroTs);
					firstTs = true;
				}
				imuTimeStamp = std::chrono::duration<double>(std::min(acceleroTs, gyroTs) - baseTs).count();
			}
		}
	}
};


extern "C" __declspec(dllexport)
int* OakDOpen(int w, int h)
{
	OakDCamera* cPtr = new OakDCamera(w, h);
	return (int*)cPtr;
}

extern "C" __declspec(dllexport)
int* OakDintrinsics(OakDCamera* cPtr, int camera)
{
	std::vector<std::vector<float>> intrin;
	if (camera == 1) // rgb camera
	{
		intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::CAM_A, cPtr->cols, cPtr->rows);
	}
	else {
		if (camera == 2)  // left camera
			intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::CAM_B, cPtr->cols, cPtr->rows);
		else
			intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::CAM_C, cPtr->cols, cPtr->rows);  // right camera
	}

	int i = 0;
	for (auto row : intrin)  for (auto val : row) cPtr->intrinsics[i++] = val;
	return (int*)&cPtr->intrinsics;
}


extern "C" __declspec(dllexport)
int* OakDExtrinsicsRGBtoLeft(OakDCamera* cPtr)
{
	auto extrinsics = cPtr->deviceCalib.getCameraExtrinsics(dai::CameraBoardSocket::CAM_A, dai::CameraBoardSocket::CAM_B);
	
	for (auto i = 0; i < 3; i++) {
		cPtr->extrinsicsRGBtoLeft[i] = extrinsics[i][3];
	}

	int index = 3;
	for (auto i = 0; i < 3; i++) {
		for (auto j = 0; j < 3; j++)
		{
			cPtr->extrinsicsRGBtoLeft[index++] = extrinsics[i][j];
		}
	}

	return (int*)&cPtr->extrinsicsRGBtoLeft[0];
}


extern "C" __declspec(dllexport)
int* OakDExtrinsicsLeftToRight(OakDCamera* cPtr)
{
	auto extrinsics = cPtr->deviceCalib.getCameraExtrinsics(dai::CameraBoardSocket::CAM_B, dai::CameraBoardSocket::CAM_C);
	
	for (auto i = 0; i < 3; i++) {
		cPtr->extrinsicsLeftToRight[i] = extrinsics[i][3];
	}

	int index = 3;
	for (auto i = 0; i < 3; i++) {
		for (auto j = 0; j < 3; j++)
		{
			cPtr->extrinsicsLeftToRight[index++] = extrinsics[i][j];
		}
	}

	return (int*)&cPtr->extrinsicsLeftToRight[0];
}


extern "C" __declspec(dllexport) int* OakDRawDepth(OakDCamera* cPtr)
{
	return (int*)cPtr->depth16u.data;
}


extern "C" __declspec(dllexport) int* OakDPointCloud(OakDCamera* cPtr) { return 0; }
extern "C" __declspec(dllexport) double OakDIMUTimeStamp(OakDCamera* cPtr) { return cPtr->imuTimeStamp; }
extern "C" __declspec(dllexport) int* OakDGyro(OakDCamera* cPtr) { return (int*)&cPtr->gyroValues.x; }
extern "C" __declspec(dllexport) int* OakDAccel(OakDCamera* cPtr) { return (int*)&cPtr->acceleroValues.x; }
extern "C" __declspec(dllexport) int* OakDColor(OakDCamera* cPtr) { return (int*)cPtr->rgb.data; }
extern "C" __declspec(dllexport) void OakDWaitForFrame(OakDCamera* cPtr) { cPtr->waitForFrame(); }
extern "C" __declspec(dllexport) int* OakDLeftImage(OakDCamera* cPtr) { return (int*)cPtr->leftView.data; }
extern "C" __declspec(dllexport) int* OakDRightImage(OakDCamera* cPtr) { return (int*)cPtr->rightView.data; }

extern "C" __declspec(dllexport) bool OakGetDevice() {
	auto devices = dai::Device::getAllAvailableDevices();
	if (devices.empty()) return false;
	return true;
}
