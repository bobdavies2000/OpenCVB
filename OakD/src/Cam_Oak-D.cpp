#include <iostream>

// Includes common necessary includes for development using depthai library
#include "depthai/depthai.hpp"
#include "depthai/pipeline/datatype/StereoDepthConfig.hpp"
#include "depthai/pipeline/node/StereoDepth.hpp"
#include "depthai/properties/StereoDepthProperties.hpp"
#include "../CPP_Native/PragmaLibs.h"

using namespace std;
using namespace cv;

constexpr float FPS = 25.0f;

// Closer-in minimum depth, disparity range is doubled (from 95 to 190):
static std::atomic<bool> extended_disparity{ false };
// Better accuracy for longer distance, fractional disparity 32-levels:
static std::atomic<bool> subpixel{ false };
// Better handling for occlusions:
static std::atomic<bool> lr_check{ true };

class OakDCamera
{
private:
public:
	dai::Pipeline pipeline;
	Mat rgb, leftView, rightView, depth16u;
	dai::CalibrationHandler deviceCalib;
	double imuTimeStamp;
	bool firstTs = false;
	int rows, cols;
	float intrinsics[9];
	float extrinsicsRGBtoLeft[12];
	float extrinsicsLeftToRight[12];
	
	float maxDisparity = 1;
	std::shared_ptr<dai::Device> device;
	
	// v3 Camera nodes (replacing ColorCamera and MonoCamera)
	std::shared_ptr<dai::node::Camera> pipeRGB;
	std::shared_ptr<dai::node::Camera> pipeLeft;
	std::shared_ptr<dai::node::Camera> pipeRight;
	std::shared_ptr<dai::node::StereoDepth> pipeDepth;
	std::shared_ptr<dai::node::Sync> sync;
	std::shared_ptr<dai::node::IMU> imu;
	
	std::shared_ptr<dai::MessageQueue> qDisparity;
	std::shared_ptr<dai::MessageQueue> qRGB;
	std::shared_ptr<dai::MessageQueue> qLeft;
	std::shared_ptr<dai::MessageQueue> qRight;
	std::shared_ptr<dai::MessageQueue> stereoOut;
	std::shared_ptr<dai::MessageQueue> imuQueue;
	dai::Node::Output* outRGB;
	dai::Node::Output* outLeft;
	dai::Node::Output* outRight;

	std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration> baseTs;

	dai::IMUReportAccelerometer acceleroValues;
	dai::IMUReportGyroscope gyroValues;

	~OakDCamera() {}

	OakDCamera(int _cols, int _rows)
	{
		rows = _rows;
		cols = _cols;

		pipeRGB = pipeline.create<dai::node::Camera>()->build(dai::CameraBoardSocket::CAM_A);
		pipeLeft = pipeline.create<dai::node::Camera>()->build(dai::CameraBoardSocket::CAM_B);
		pipeRight = pipeline.create<dai::node::Camera>()->build(dai::CameraBoardSocket::CAM_C);
		pipeDepth = pipeline.create<dai::node::StereoDepth>();

		outRGB = pipeRGB->requestOutput({ cols, rows }, dai::ImgFrame::Type::BGR888i);
		outLeft = pipeLeft->requestOutput({ cols, rows });
		outRight = pipeRight->requestOutput({ cols, rows });

		// Create a node that will produce the depth map (using disparity output as it's easier to visualize depth this way)
		pipeDepth->build(*outLeft, *outRight, dai::node::StereoDepth::PresetMode::DEFAULT);

		// Options: MEDIAN_OFF, KERNEL_3x3, KERNEL_5x5, KERNEL_7x7 (default)
		pipeDepth->initialConfig->setMedianFilter(dai::StereoDepthConfig::MedianFilter::KERNEL_7x7);
		pipeDepth->setLeftRightCheck(lr_check);
		pipeDepth->setExtendedDisparity(extended_disparity);
		pipeDepth->setSubpixel(subpixel);

		// Create output queues - StereoDepth outputs are accessed directly (depth, disparity, etc.)
		qDisparity = pipeDepth->disparity.createOutputQueue();
		// If you need depth output instead of disparity, use: pipeDepth->depth.createOutputQueue()
		
		qRGB = outRGB->createOutputQueue();
		qLeft = outLeft->createOutputQueue();
		qRight = outRight->createOutputQueue();

		pipeline.start();

		maxDisparity = (float)pipeDepth->initialConfig->getMaxDisparity();
		rgb = Mat(rows, cols, CV_8UC3);
		depth16u = Mat(rows, cols, CV_16UC1);
		leftView = Mat(rows, cols, CV_8UC1);
		rightView = Mat(rows, cols, CV_8UC1);
		leftView.setTo(0);
		rightView.setTo(0);
	}

	void waitForFrame()
	{
		auto inRGB = qRGB->get<dai::ImgFrame>();
		auto inDepth = qDisparity->get<dai::ImgFrame>();
		auto inLeft = qLeft->get<dai::ImgFrame>();
		auto inRight = qRight->get<dai::ImgFrame>();
		rgb = inRGB->getFrame().clone();
		depth16u = inDepth->getFrame().clone();
		leftView = inLeft->getFrame().clone();
		rightView = inRight->getFrame().clone();
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
		intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::CAM_A, 1280, 720);
	}
	else {
		if (camera == 2)  // left camera
			intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::CAM_B, 1280, 720);
		else
			intrin = cPtr->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::CAM_C, 1280, 720);  // right camera
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
extern "C" __declspec(dllexport) void OakDStop(OakDCamera* cPtr)
{
	if (cPtr != nullptr) {
		if (cPtr->device != nullptr) {
			cPtr->device->close();
		}
		delete cPtr;
	}
}
extern "C" __declspec(dllexport) int* OakDLeftImage(OakDCamera* cPtr) { return (int*)cPtr->leftView.data; }
extern "C" __declspec(dllexport) int* OakDRightImage(OakDCamera* cPtr) { return (int*)cPtr->rightView.data; }
