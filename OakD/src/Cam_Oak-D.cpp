#include <csignal>
#include <thread>
#include <chrono>
#include <opencv2/opencv.hpp>
#include <depthai/depthai.hpp>
#include <depthai/remote_connection/RemoteConnection.hpp>
#include "../CPP_Native/PragmaLibs.h"

using namespace std;
using namespace cv;
#if 1

constexpr float FPS = 25.0f;

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
	bool firstPass;
	float intrinsics[9];
	float extrinsicsRGBtoLeft[12];
	float extrinsicsLeftToRight[12];
	
	std::shared_ptr<dai::Device> device;
	
	// v3 Camera nodes (replacing ColorCamera and MonoCamera)
	std::shared_ptr<dai::node::Camera> color;
	std::shared_ptr<dai::node::Camera> monoLeft;
	std::shared_ptr<dai::node::Camera> monoRight;
	std::shared_ptr<dai::node::StereoDepth> stereo;
	std::shared_ptr<dai::node::IMU> imu;
	
	// Output pointers from nodes
	dai::Node::Output* rgbOut = nullptr;
	
	// v3 MessageQueues (replacing DataOutputQueue)
	std::shared_ptr<dai::MessageQueue> colorOut;
	std::shared_ptr<dai::MessageQueue> rightOut;
	std::shared_ptr<dai::MessageQueue> stereoOut;
	std::shared_ptr<dai::MessageQueue> depthQueue;
	std::shared_ptr<dai::MessageQueue> imuQueue;
	dai::Node::Output* colorCamOut;
	dai::Node::Output* monoLeftOut;
	dai::Node::Output* monoRightOut;

	std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration> baseTs;

	dai::IMUReportAccelerometer acceleroValues;
	dai::IMUReportGyroscope gyroValues;

	const dai::CameraBoardSocket RGB_SOCKET = dai::CameraBoardSocket::CAM_A;
	const dai::CameraBoardSocket LEFT_SOCKET = dai::CameraBoardSocket::CAM_B;
	const dai::CameraBoardSocket RIGHT_SOCKET = dai::CameraBoardSocket::CAM_C;

	~OakDCamera() {}
	
	cv::Mat processDepthFrame(const cv::Mat& depthFrame) {
		cv::Mat depth_downscaled;
		cv::resize(depthFrame, depth_downscaled, cv::Size(), 0.25, 0.25);

		double min_depth = 0;
		if (!cv::countNonZero(depth_downscaled == 0)) {
			std::vector<float> nonZeroDepth;
			nonZeroDepth.reserve(depth_downscaled.rows * depth_downscaled.cols);

			for (int i = 0; i < depth_downscaled.rows; i++) {
				for (int j = 0; j < depth_downscaled.cols; j++) {
					float depth = depth_downscaled.at<float>(i, j);
					if (depth > 0) nonZeroDepth.push_back(depth);
				}
			}

			if (!nonZeroDepth.empty()) {
				std::sort(nonZeroDepth.begin(), nonZeroDepth.end());
				min_depth = nonZeroDepth[static_cast<int>(nonZeroDepth.size() * 0.01)];  // 1st percentile
			}
		}

		std::vector<float> allDepth;
		allDepth.reserve(depth_downscaled.rows * depth_downscaled.cols);
		for (int i = 0; i < depth_downscaled.rows; i++) {
			for (int j = 0; j < depth_downscaled.cols; j++) {
				allDepth.push_back(depth_downscaled.at<float>(i, j));
			}
		}
		std::sort(allDepth.begin(), allDepth.end());
		double max_depth = allDepth[static_cast<int>(allDepth.size() * 0.99)];  // 99th percentile

		// Normalize and colorize
		cv::Mat normalized;
		cv::normalize(depthFrame, normalized, 0, 255, cv::NORM_MINMAX, CV_8UC1, depthFrame > min_depth);
		cv::Mat colorized;
		cv::applyColorMap(normalized, colorized, cv::COLORMAP_HOT);
		return colorized;
	}

	OakDCamera(int _cols, int _rows)
	{
		rows = _rows;
		cols = _cols;
		firstPass = true;
	}

	void waitForFrame()
	{
		if (firstPass)
		{
			// Create and configure nodes
			color = pipeline.create<dai::node::Camera>();
			color->build(dai::CameraBoardSocket::CAM_A);

			monoLeft = pipeline.create<dai::node::Camera>();
			monoLeft->build(dai::CameraBoardSocket::CAM_B);

			monoRight = pipeline.create<dai::node::Camera>();
			monoRight->build(dai::CameraBoardSocket::CAM_C);

			stereo = pipeline.create<dai::node::StereoDepth>();

			// Configure stereo node
			stereo->setDefaultProfilePreset(dai::node::StereoDepth::PresetMode::DEFAULT);
			stereo->setDepthAlign(dai::CameraBoardSocket::CAM_A);
			stereo->setOutputSize(640, 400);

			// Configure outputs
			colorCamOut = color->requestOutput(std::make_pair(640, 480));
			monoLeftOut = monoLeft->requestOutput(std::make_pair(640, 480));
			monoRightOut = monoRight->requestOutput(std::make_pair(640, 480));

			// Link mono cameras to stereo
			monoLeftOut->link(stereo->left);
			monoRightOut->link(stereo->right);

			// Create output queues with blocking enabled (default)
			colorOut = colorCamOut->createOutputQueue(8, false);
			rightOut = monoRightOut->createOutputQueue(8, false);
			stereoOut = stereo->depth.createOutputQueue(8, false);

			// Create device and start pipeline
			device = std::make_shared<dai::Device>();
			device->startPipeline(pipeline);

			// Get calibration data
			deviceCalib = device->readCalibration();

			rgb = Mat(rows, cols, CV_8UC3);
			depth16u = Mat(rows, cols, CV_16UC1);
			leftView = Mat(rows, cols, CV_8UC1);
			rightView = Mat(rows, cols, CV_8UC1);
			leftView.setTo(0);
			rightView.setTo(0);

			firstPass = false;
			
			// Give the device a moment to start producing frames
			std::this_thread::sleep_for(std::chrono::milliseconds(100));
		}
		
		// Check if queues are valid before reading
		if (!colorOut || !stereoOut) {
			return;  // Queues not ready yet
		}
		
		// Get frames (blocking call - will wait for frames)
		auto colorFrame = colorOut->get<dai::ImgFrame>();
		auto stereoFrame = stereoOut->get<dai::ImgFrame>();

		// Check if we got valid frames
		if (colorFrame == nullptr || stereoFrame == nullptr) {
			return;  // Skip this frame if either is null
		}

		// Validate transformations
		if (!colorFrame->validateTransformations() || !stereoFrame->validateTransformations()) {
			std::cerr << "Invalid transformations!" << std::endl;
			return;
		}

		// Get frames
		rgb = colorFrame->getCvFrame();
		//cv::imshow("rgb", rgb);
		//cv::waitKey(1);

		depth16u = processDepthFrame(stereoFrame->getCvFrame());

		// Create and remap rectangle
		//dai::RotatedRect rect(dai::Point2f(300, 200), dai::Size2f(200, 100), 10);
		//auto remappedRect = colorFrame->transformation.remapRectTo(stereoFrame->transformation, rect);

		//// Convert to OpenCV Mats using raw data (avoids ABI issues with getCvFrame)
		//// Depth - 16-bit unsigned
		//cv::Mat depthRaw(depth->getHeight(), depth->getWidth(), CV_16UC1, depth->getData().data());
		//cv::resize(depthRaw, depth16u, cv::Size(cols, rows));
		//
		//// RGB - BGR 8-bit
		//cv::Mat rgbRaw(inRGB->getHeight(), inRGB->getWidth(), CV_8UC3, inRGB->getData().data());
		//cv::resize(rgbRaw, rgb, cv::Size(cols, rows));
		//
		//// Left - Grayscale 8-bit
		//cv::Mat leftRaw(inLeft->getHeight(), inLeft->getWidth(), CV_8UC1, inLeft->getData().data());
		//cv::resize(leftRaw, leftView, cv::Size(cols, rows));
		//
		//// Right - Grayscale 8-bit
		//cv::Mat rightRaw(inRight->getHeight(), inRight->getWidth(), CV_8UC1, inRight->getData().data());
		//cv::resize(rightRaw, rightView, cv::Size(cols, rows));

		//// Get IMU data
		//auto imuData = imuQueue->get<dai::IMUData>();
		//auto imuPackets = imuData->packets;
		//
		//for (auto& imuPacket : imuPackets) {
		//	acceleroValues = imuPacket.acceleroMeter;
		//	gyroValues = imuPacket.gyroscope;

		//	auto acceleroTs1 = acceleroValues.getTimestampDevice();
		//	auto gyroTs1 = gyroValues.getTimestampDevice();
		//	
		//	if (!firstTs) {
		//		baseTs = std::min(acceleroTs1, gyroTs1);
		//		firstTs = true;
		//	}

		//	auto acceleroTs = acceleroTs1 - baseTs;
		//	auto gyroTs = gyroTs1 - baseTs;
		//	
		//	// Store timestamp in seconds
		//	imuTimeStamp = std::chrono::duration<double>(acceleroTs).count();
		//}
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

#else
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
	bool firstPass;
	float intrinsics[9];
	float extrinsicsRGBtoLeft[12];
	float extrinsicsLeftToRight[12];

	std::shared_ptr<dai::Device> device;

	// v3 Camera nodes (replacing ColorCamera and MonoCamera)
	std::shared_ptr<dai::node::Camera> camRgb;
	std::shared_ptr<dai::node::Camera> camLeft;
	std::shared_ptr<dai::node::Camera> camRight;
	std::shared_ptr<dai::node::StereoDepth> stereo;
	std::shared_ptr<dai::node::IMU> imu;

	// Output pointers from nodes
	dai::Node::Output* rgbOut = nullptr;
	dai::Node::Output* leftOut = nullptr;
	dai::Node::Output* rightOut = nullptr;

	// v3 MessageQueues (replacing DataOutputQueue)
	std::shared_ptr<dai::MessageQueue> rgbQueue;
	std::shared_ptr<dai::MessageQueue> leftQueue;
	std::shared_ptr<dai::MessageQueue> rightQueue;
	std::shared_ptr<dai::MessageQueue> depthQueue;
	std::shared_ptr<dai::MessageQueue> imuQueue;

	std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration> baseTs;

	dai::IMUReportAccelerometer acceleroValues;
	dai::IMUReportGyroscope gyroValues;

	~OakDCamera() {}

	OakDCamera(int _cols, int _rows)
	{
		firstPass = true;
		rows = _rows;
		cols = _cols;

		// Create Camera nodes (v3 unified Camera replaces ColorCamera and MonoCamera)
		camRgb = pipeline.create<dai::node::Camera>();
		camLeft = pipeline.create<dai::node::Camera>();
		camRight = pipeline.create<dai::node::Camera>();
		stereo = pipeline.create<dai::node::StereoDepth>();
		imu = pipeline.create<dai::node::IMU>();

		// Configure IMU
		imu->enableIMUSensor(dai::IMUSensor::ACCELEROMETER_RAW, 500);
		imu->enableIMUSensor(dai::IMUSensor::GYROSCOPE_RAW, 400);
		imu->setBatchReportThreshold(1);
		imu->setMaxBatchReports(1);

		// Build cameras with their board sockets
		// RGB camera on CAM_A
		camRgb->build(dai::CameraBoardSocket::CAM_A);

		// Left mono camera on CAM_B
		camLeft->build(dai::CameraBoardSocket::CAM_B);

		// Right mono camera on CAM_C
		camRight->build(dai::CameraBoardSocket::CAM_C);

		// Request outputs from cameras
		// RGB output - request BGR format at desired resolution
		rgbOut = camRgb->requestOutput(
			{ static_cast<uint32_t>(cols), static_cast<uint32_t>(rows) },
			dai::ImgFrame::Type::BGR888i,
			dai::ImgResizeMode::CROP,
			60.0f  // FPS
		);

		// Mono camera outputs for stereo (720p grayscale)
		leftOut = camLeft->requestOutput(
			{ 1280, 720 },
			dai::ImgFrame::Type::GRAY8,
			dai::ImgResizeMode::CROP
		);

		auto rightOutput = camRight->requestOutput(
			{ 1280, 720 },
			dai::ImgFrame::Type::GRAY8,
			dai::ImgResizeMode::CROP
		);

		// Configure stereo depth
		stereo->setDefaultProfilePreset(dai::node::StereoDepth::PresetMode::FAST_DENSITY);
		stereo->initialConfig->setMedianFilter(dai::StereoDepthConfig::MedianFilter::KERNEL_7x7);
		stereo->initialConfig->setConfidenceThreshold(230);
		stereo->setLeftRightCheck(true);
		stereo->setSubpixel(false);
		stereo->setDepthAlign(dai::CameraBoardSocket::CAM_A);
		stereo->setRectifyEdgeFillColor(0);

		// Link mono cameras to stereo
		leftOut->link(stereo->left);
		rightOutput->link(stereo->right);

		// Create output queues using v3 API - call createOutputQueue on the Output directly
		rgbQueue = rgbOut->createOutputQueue(8, false);
		depthQueue = stereo->depth.createOutputQueue(8, false);
		leftQueue = stereo->rectifiedLeft.createOutputQueue(8, false);
		rightQueue = stereo->rectifiedRight.createOutputQueue(8, false);
		imuQueue = imu->out.createOutputQueue(50, false);

		// Start the device with the pipeline
		device = std::make_shared<dai::Device>();
		device->startPipeline(pipeline);

		// Get calibration data
		deviceCalib = device->readCalibration();

		baseTs = std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration>();

		rgb = Mat(rows, cols, CV_8UC3);
		depth16u = Mat(rows, cols, CV_16UC1);
		leftView = Mat(rows, cols, CV_8UC1);
		rightView = Mat(rows, cols, CV_8UC1);
	}

	void waitForFrame()
	{
		// Get frames from queues
		auto inRGB = rgbQueue->get<dai::ImgFrame>();
		auto inLeft = leftQueue->get<dai::ImgFrame>();
		auto inRight = rightQueue->get<dai::ImgFrame>();
		auto depth = depthQueue->get<dai::ImgFrame>();

		// Convert to OpenCV Mats using raw data (avoids ABI issues with getCvFrame)
		// Depth - 16-bit unsigned
		cv::Mat depthRaw(depth->getHeight(), depth->getWidth(), CV_16UC1, depth->getData().data());
		cv::resize(depthRaw, depth16u, cv::Size(cols, rows));

		// RGB - BGR 8-bit
		cv::Mat rgbRaw(inRGB->getHeight(), inRGB->getWidth(), CV_8UC3, inRGB->getData().data());
		cv::resize(rgbRaw, rgb, cv::Size(cols, rows));

		// Left - Grayscale 8-bit
		cv::Mat leftRaw(inLeft->getHeight(), inLeft->getWidth(), CV_8UC1, inLeft->getData().data());
		cv::resize(leftRaw, leftView, cv::Size(cols, rows));

		// Right - Grayscale 8-bit
		cv::Mat rightRaw(inRight->getHeight(), inRight->getWidth(), CV_8UC1, inRight->getData().data());
		cv::resize(rightRaw, rightView, cv::Size(cols, rows));

		// Get IMU data
		auto imuData = imuQueue->get<dai::IMUData>();
		auto imuPackets = imuData->packets;

		for (auto& imuPacket : imuPackets) {
			acceleroValues = imuPacket.acceleroMeter;
			gyroValues = imuPacket.gyroscope;

			auto acceleroTs1 = acceleroValues.getTimestampDevice();
			auto gyroTs1 = gyroValues.getTimestampDevice();

			if (!firstTs) {
				baseTs = std::min(acceleroTs1, gyroTs1);
				firstTs = true;
			}

			auto acceleroTs = acceleroTs1 - baseTs;
			auto gyroTs = gyroTs1 - baseTs;

			// Store timestamp in seconds
			imuTimeStamp = std::chrono::duration<double>(acceleroTs).count();
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
#endif