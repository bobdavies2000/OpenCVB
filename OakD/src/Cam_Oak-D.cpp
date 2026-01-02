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
	float intrinsics[9];
	float extrinsicsRGBtoLeft[12];
	float extrinsicsLeftToRight[12];
	
	std::shared_ptr<dai::Device> device;
	
	// v3 Camera nodes (replacing ColorCamera and MonoCamera)
	std::shared_ptr<dai::node::Camera> camRgb;
	std::shared_ptr<dai::node::Camera> left;
	std::shared_ptr<dai::node::Camera> right;
	std::shared_ptr<dai::node::StereoDepth> stereo;
	std::shared_ptr<dai::node::StereoDepth> sync;
	std::shared_ptr<dai::node::IMU> imu;

	// Output pointers from nodes
	dai::Node::Output* rgbOut = nullptr;
	
	// v3 MessageQueues (replacing DataOutputQueue)
	//std::shared_ptr<dai::MessageQueue> rgbOut;
	//std::shared_ptr<dai::MessageQueue> leftOut;
	//std::shared_ptr<dai::MessageQueue> rightOut; 
	std::shared_ptr<dai::MessageQueue> queue;
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
	cv::Mat colorizeDepth(cv::Mat frameDepth) {
		try {
			// Early exit if no valid pixels
			if (cv::countNonZero(frameDepth) == 0) {
				return cv::Mat::zeros(frameDepth.rows, frameDepth.cols, CV_8UC3);
			}

			// Convert to float once
			cv::Mat frameDepthFloat;
			frameDepth.convertTo(frameDepthFloat, CV_32F);

			double minVal, maxVal;
			cv::minMaxLoc(frameDepthFloat, &minVal, &maxVal, nullptr, nullptr, frameDepthFloat > 0);

			// Take log in-place
			cv::log(frameDepthFloat, frameDepthFloat);
			float logMinDepth = std::log(minVal);
			float logMaxDepth = std::log(maxVal);

			frameDepthFloat = (frameDepthFloat - logMinDepth) * (255.0f / (logMaxDepth - logMinDepth));

			cv::Mat normalizedDepth;
			frameDepthFloat.convertTo(normalizedDepth, CV_8UC1);

			cv::Mat depthFrameColor;
			cv::applyColorMap(normalizedDepth, depthFrameColor, cv::COLORMAP_JET);

			// Mask invalid pixels
			depthFrameColor.setTo(0, frameDepth == 0);

			return depthFrameColor;

		}
		catch (const std::exception& e) {
			std::cerr << "Error in colorizeDepth: " << e.what() << std::endl;
			return cv::Mat::zeros(frameDepth.rows, frameDepth.cols, CV_8UC3);
		}
	}

	OakDCamera(int _cols, int _rows)
	{
		rows = _rows;
		cols = _cols;

		// Create and configure nodes
		camRgb = pipeline.create<dai::node::Camera>();
		camRgb->build(RGB_SOCKET);
		left = pipeline.create<dai::node::Camera>();
		left->build(LEFT_SOCKET);
		right = pipeline.create<dai::node::Camera>();
		right->build(RIGHT_SOCKET);

		stereo = pipeline.create<dai::node::StereoDepth>();
		auto sync = pipeline.create<dai::node::Sync>();

		// Check if platform is RVC4 and create ImageAlign node if needed
		auto platform = pipeline.getDefaultDevice()->getPlatform();
		std::shared_ptr<dai::node::ImageAlign> align;
		if (platform == dai::Platform::RVC4) {
			align = pipeline.create<dai::node::ImageAlign>();
		}

		stereo->setExtendedDisparity(true);
		sync->setSyncThreshold(std::chrono::duration<int64_t, std::nano>(static_cast<int64_t>(1e9 / (2.0 * FPS))));

		// Configure outputs
		auto rgbOut = camRgb->requestOutput(std::make_pair(640, 480), dai::ImgFrame::Type::NV12, dai::ImgResizeMode::CROP, FPS, true);
		auto leftOut = left->requestOutput(std::make_pair(640, 400), std::nullopt, dai::ImgResizeMode::CROP, FPS);
		auto rightOut = right->requestOutput(std::make_pair(640, 400), std::nullopt, dai::ImgResizeMode::CROP, FPS);

		// Link nodes
		rgbOut->link(sync->inputs["rgb"]);
		leftOut->link(stereo->left);
		rightOut->link(stereo->right);

		if (platform == dai::Platform::RVC4) {
			stereo->depth.link(align->input);
			rgbOut->link(align->inputAlignTo);
			align->outputAligned.link(sync->inputs["depth_aligned"]);
		}
		else {
			stereo->depth.link(sync->inputs["depth_aligned"]);
			rgbOut->link(stereo->inputAlignTo);
		}

		// Create output queue
		queue = sync->out.createOutputQueue();

		// Start pipeline
		pipeline.start();

		rgb = Mat(rows, cols, CV_8UC3);
		depth16u = Mat(rows, cols, CV_16UC1);
		leftView = Mat(rows, cols, CV_8UC1);
		rightView = Mat(rows, cols, CV_8UC1);
		leftView.setTo(0);
		rightView.setTo(0);
	}

	void waitForFrame()
	{
		auto messageGroup = queue->get<dai::MessageGroup>();

		auto frameRgb = messageGroup->get<dai::ImgFrame>("rgb");
		auto frameDepth = messageGroup->get<dai::ImgFrame>("depth_aligned");

		if (frameDepth != nullptr) 
		{
			cv::Mat rgb = frameRgb->getCvFrame();
			cv::imshow("RGB Frame", rgb);

			cv::Mat alignedDepthColorized = colorizeDepth(frameDepth->getFrame());
			cv::imshow("Aligned Depth Colorized", alignedDepthColorized);
			cv::waitKey(1);
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