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
	float disparityFactor = 0;
	bool firstTs = false;
	std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration> baseTs; int rows, cols;

	float intrinsics[9]; 
	float extrinsicsRGBtoLeft[12];
	float extrinsicsLeftToRight[12];

	std::shared_ptr<dai::MessageQueue> qLeft;
	std::shared_ptr<dai::MessageQueue> qRight;
	std::shared_ptr<dai::MessageQueue> qIMU;
	std::shared_ptr<dai::MessageQueue> qDisparity;

	dai::IMUReportAccelerometer acceleroValues;
	dai::IMUReportGyroscope gyroValues;

	OakDCamera(int _cols, int _rows)
	{
		rows = _rows;
		cols = _cols;

		auto pipeIMU = pipeline.create<dai::node::IMU>();

		// Enable ACCELEROMETER_RAW at 480 hz rate
		pipeIMU->enableIMUSensor(dai::IMUSensor::ACCELEROMETER_RAW, 480);
		// Enable GYROSCOPE_RAW at 400 hz rate
		pipeIMU->enableIMUSensor(dai::IMUSensor::GYROSCOPE_RAW, 400);

		// Set batch report threshold and max batch reports
		pipeIMU->setBatchReportThreshold(1);
		pipeIMU->setMaxBatchReports(10);

		qIMU = pipeIMU->out.createOutputQueue(50, false);

		// Define sources and outputs
		auto pipeLeft = pipeline.create<dai::node::Camera>()->build(dai::CameraBoardSocket::CAM_B);
		auto pipeRight = pipeline.create<dai::node::Camera>()->build(dai::CameraBoardSocket::CAM_C);
		auto pipeStereo = pipeline.create<dai::node::StereoDepth>();

		// Properties
		auto* outLeft = pipeLeft->requestOutput({ cols, rows });
		auto* outRight = pipeRight->requestOutput({ cols, rows });

		// Create a node that will produce the depth map (using disparity output as it's easier to visualize depth this way)
		pipeStereo->build(*outLeft, *outRight, dai::node::StereoDepth::PresetMode::DEFAULT);
		// Options: MEDIAN_OFF, KERNEL_3x3, KERNEL_5x5, KERNEL_7x7 (default)
		pipeStereo->initialConfig->setMedianFilter(dai::StereoDepthConfig::MedianFilter::KERNEL_7x7);
		pipeStereo->setLeftRightCheck(lr_check);
		pipeStereo->setExtendedDisparity(extended_disparity);
		pipeStereo->setSubpixel(subpixel);

		// Output queue will be used to get the disparity frames from the outputs defined above
		qDisparity = pipeStereo->disparity.createOutputQueue();
		qLeft = outLeft->createOutputQueue();
		qRight = outRight->createOutputQueue();

		pipeline.start();

		device = pipeline.getDefaultDevice();
		deviceCalib = device->readCalibration();
		baseTs = std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration>();

		disparityFactor = 255 / pipeStereo->initialConfig->getMaxDisparity();
	}
	void waitForFrame()
	{
		auto inDisparity = qDisparity->get<dai::ImgFrame>();
		auto inLeft = qLeft->get<dai::ImgFrame>();
		auto inRight = qRight->get<dai::ImgFrame>();

		disparity = inDisparity->getFrame();
		leftView = inLeft->getFrame().clone();
		rightView = inRight->getFrame().clone();
		cv::resize(disparity, disparity, cv::Size(cols, rows));

		//disparity.convertTo(test, CV_8UC1, disparityFactor);

		auto imuData = qIMU->get<dai::IMUData>();
		if (imuData != nullptr)
		{
			for (const auto& imuPacket : imuData->packets) {
				auto acceleroValues = imuPacket.acceleroMeter;
				auto gyroValues = imuPacket.gyroscope;

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


extern "C" __declspec(dllexport) double OakDIMUTimeStamp(OakDCamera* cPtr) { return cPtr->imuTimeStamp; }
extern "C" __declspec(dllexport) int* OakDGyro(OakDCamera* cPtr) { return (int*)&cPtr->gyroValues.x; }
extern "C" __declspec(dllexport) int* OakDAccel(OakDCamera* cPtr) { return (int*)&cPtr->acceleroValues.x; }
extern "C" __declspec(dllexport) int* OakDColor(OakDCamera* cPtr) { return (int*)cPtr->rgb.data; }
extern "C" __declspec(dllexport) void OakDWaitForFrame(OakDCamera* cPtr) { cPtr->waitForFrame(); }
extern "C" __declspec(dllexport) int* OakDLeftImage(OakDCamera* cPtr) { return (int*)cPtr->leftView.data; }
extern "C" __declspec(dllexport) int* OakDRightImage(OakDCamera* cPtr) { return (int*)cPtr->rightView.data; }
extern "C" __declspec(dllexport) int* OakDDisparity(OakDCamera* cPtr) { return (int*)cPtr->disparity.data; }
extern "C" __declspec(dllexport) float OakDDisparityFactor(OakDCamera* cPtr) { return cPtr->disparityFactor; }
extern "C" __declspec(dllexport) void OakDStop(OakDCamera* cPtr) {
	if (cPtr != nullptr) {
		cPtr->device.reset();
		delete cPtr;
	}
}
extern "C" __declspec(dllexport) bool OakGetDevice() {
	auto devices = dai::Device::getAllAvailableDevices();
	if (devices.empty()) return false;
	return true;
}