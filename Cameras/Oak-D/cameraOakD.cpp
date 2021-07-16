#include <iostream>
#include <list> 
#include <iterator> 
#include <iomanip>
#include <cstring>
#include <string>
#include <thread>
#include <mutex>
#include <cstdlib>
#include <cstdio>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/calib3d.hpp>
#include "opencv2/imgproc.hpp"
#include "opencv2/imgcodecs.hpp"
#include "opencv2/highgui.hpp"
#include <map>
#include "../CameraDefines.hpp"
#ifdef OPENCV_OAKD
#include "utility.hpp"
#include "depthai/depthai.hpp"
#ifdef _DEBUG
#include "../Data/PragmaLibsD.h"
#else
#include "../Data/PragmaLibs.h"
#endif

using namespace std;
using namespace cv;

// Optional. If set (true), the ColorCamera is downscaled from 1080p to 720p.
// Otherwise (false), the aligned depth is automatically upscaled to 1080p
static std::atomic<bool> downscaleColor{ true };
static constexpr int fps = 30;
// The disparity is computed at this resolution, then upscaled to RGB resolution
static constexpr auto monoRes = dai::MonoCameraProperties::SensorResolution::THE_400_P;

class OakDCamera
{
private:
public:
	dai::Pipeline pipeline;
	std::vector<std::string> queueNames;
	std::unordered_map<std::string, cv::Mat> frame;
	std::unordered_map<std::string, std::shared_ptr<dai::ImgFrame>> latestPacket;
	float intrinsicsLeft[9];
	float intrinsicsRight[9];
	float intrinsicsRGB[9];
	float maxDisparity;
	Mat depth8u;
	Mat RGBdepth;
	Mat rgb;
	Mat leftView;
	Mat rightView;
	
	~OakDCamera(){}

	OakDCamera(int cols, int rows)
	{
		rgb = Mat(rows, cols, CV_8UC3);
		RGBdepth = Mat(rows, cols, CV_8UC3);
		depth8u = Mat(rows, cols, CV_8UC1);
		leftView = Mat(rows, cols, CV_8UC1);
		rightView = Mat(rows, cols, CV_8UC1);

		// Define sources and outputs
		auto camRgb = pipeline.create<dai::node::ColorCamera>();
		auto left = pipeline.create<dai::node::MonoCamera>();
		auto right = pipeline.create<dai::node::MonoCamera>();
		auto stereo = pipeline.create<dai::node::StereoDepth>();

		auto rgbOut = pipeline.create<dai::node::XLinkOut>();
		auto depthOut = pipeline.create<dai::node::XLinkOut>();

		auto monoLeft = pipeline.create<dai::node::MonoCamera>();
		auto monoRight = pipeline.create<dai::node::MonoCamera>();
		auto xoutLeft = pipeline.create<dai::node::XLinkOut>();
		auto xoutRight = pipeline.create<dai::node::XLinkOut>();

		auto imu = pipeline.create<dai::node::IMU>();
		auto xlinkImu = pipeline.create<dai::node::XLinkOut>();
		xlinkImu->setStreamName("imu");
		// enable ACCELEROMETER_RAW and GYROSCOPE_RAW at 500 hz rate
		imu->enableIMUSensor({ dai::IMUSensor::ACCELEROMETER_RAW, dai::IMUSensor::GYROSCOPE_RAW }, 500);
		// above this threshold packets will be sent in batch of X, if the host is not blocked and USB bandwidth is available
		imu->setBatchReportThreshold(1);
		// maximum number of IMU packets in a batch, if it's reached device will block sending until host can receive it
		// if lower or equal to batchReportThreshold then the sending is always blocking on device
		// useful to reduce device's CPU load  and number of lost packets, if CPU load is high on device side due to multiple nodes
		imu->setMaxBatchReports(10);

		// Link plugins IMU -> XLINK
		imu->out.link(xlinkImu->input);

		rgbOut->setStreamName("rgb");
		queueNames.push_back("rgb");
		depthOut->setStreamName("depth");
		queueNames.push_back("depth");

		// Properties
		camRgb->setBoardSocket(dai::CameraBoardSocket::RGB);
		camRgb->setResolution(dai::ColorCameraProperties::SensorResolution::THE_1080_P);
		camRgb->setFps(fps);
		if (downscaleColor) camRgb->setIspScale(2, 3);
		// For now, RGB needs fixed focus to properly align with depth.
		// This value was used during calibration
		camRgb->initialControl.setManualFocus(135);

		left->setResolution(monoRes);
		left->setBoardSocket(dai::CameraBoardSocket::LEFT);
		left->setFps(fps);
		right->setResolution(monoRes);
		right->setBoardSocket(dai::CameraBoardSocket::RIGHT);
		right->setFps(fps);

		stereo->initialConfig.setConfidenceThreshold(230);
		// LR-check is required for depth alignment
		stereo->setLeftRightCheck(true);
		stereo->setDepthAlign(dai::CameraBoardSocket::RGB);

		// Linking
		camRgb->isp.link(rgbOut->input);
		left->out.link(stereo->left);
		right->out.link(stereo->right);
		stereo->disparity.link(depthOut->input);

		xoutLeft->setStreamName("left");
		xoutRight->setStreamName("right");

		// Properties
		monoLeft->setBoardSocket(dai::CameraBoardSocket::LEFT);
		monoLeft->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);
		monoRight->setBoardSocket(dai::CameraBoardSocket::RIGHT);
		monoRight->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);

		// Linking
		monoRight->out.link(xoutRight->input);
		monoLeft->out.link(xoutLeft->input);

		maxDisparity = stereo->getMaxDisparity();
	}

	void waitForFrame()
	{
		using namespace std::chrono;
		// Connect to device and start pipeline
		static dai::Device device(pipeline);
		static bool initialized = false;
		static bool firstTs = false;
		if (initialized == false)
		{
			// Sets queues size and behavior
			for (const auto& name : queueNames) {
				device.getOutputQueue(name, 4, false);
			}
			initialized = true;
		}
		static auto qLeft = device.getOutputQueue("left", 4, false);
		static auto qRight = device.getOutputQueue("right", 4, false);
		static auto imuQueue = device.getOutputQueue("imu", 50, false);
		static auto baseTs = std::chrono::time_point<std::chrono::steady_clock, std::chrono::steady_clock::duration>();
		static auto imuData = imuQueue->get<dai::IMUData>();

		cout << "Starting the next round of frame collection" << endl;
		auto queueEvents = device.getQueueEvents(queueNames);
		for (const auto& name : queueEvents) {
			cout << name << endl;
			auto packets = device.getOutputQueue(name)->tryGetAll<dai::ImgFrame>();
			auto count = packets.size();
			if (count > 0) {
				latestPacket[name] = packets[count - 1];
			}
		}

		for (const auto& name : queueNames) {
			if (latestPacket.find(name) != latestPacket.end()) 
			{
				if (name == "depth")
				{
					cout << "Depth 8u encountered max disparity = " << maxDisparity << endl;
					depth8u = latestPacket[name]->getFrame();
					depth8u.convertTo(depth8u, CV_8UC1, 255. / maxDisparity);
					cv::applyColorMap(depth8u, RGBdepth, cv::COLORMAP_HOT);
				}
				if (name == "rgb") rgb = latestPacket[name]->getCvFrame();
			}
		}
		auto inLeft = qLeft->tryGet<dai::ImgFrame>();
		auto inRight = qRight->tryGet<dai::ImgFrame>();

		auto imuPackets = imuData->packets;
		for (auto& imuPacket : imuPackets) {
			auto& acceleroValues = imuPacket.acceleroMeter;
			auto& gyroValues = imuPacket.gyroscope;

			auto acceleroTs1 = acceleroValues.timestamp.get();
			auto gyroTs1 = gyroValues.timestamp.get();
			if (!firstTs) {
				baseTs = std::min(acceleroTs1, gyroTs1);
				firstTs = true;
			}

			auto acceleroTs = acceleroTs1 - baseTs;
			auto gyroTs = gyroTs1 - baseTs;

			printf("Accelerometer timestamp: %ld ms\n", duration_cast<milliseconds>(acceleroTs).count());
			printf("Accelerometer [m/s^2]: x: %.3f y: %.3f z: %.3f \n", acceleroValues.x, acceleroValues.y, acceleroValues.z);
			printf("Gyroscope timestamp: %ld ms\n", duration_cast<milliseconds>(gyroTs).count());
			printf("Gyroscope [rad/s]: x: %.3f y: %.3f z: %.3f \n", gyroValues.x, gyroValues.y, gyroValues.z);
		}

		if (inLeft) {
			cout << "left encountered" << endl;
			leftView = inRight->getCvFrame();
		}

		if (inRight) {
			cout << "right encountered" << endl;
			rightView = inRight->getCvFrame();
		}


		// Blend when both received
		//if (frame.find("rgb") != frame.end() && frame.find("depth") != frame.end()) {
		//	// Need to have both frames in BGR format before blending
		//	if (frame["depth"].channels() < 3) {
		//		cv::cvtColor(frame["depth"], frame["depth"], cv::COLOR_GRAY2BGR);
		//	}
		//	cv::Mat blended;
		//	cv::addWeighted(frame["rgb"], 0.6, frame["depth"], 0.4, 0, blended);
		//	cv::imshow("rgb-depth", blended);
		//	frame.clear();
		//}
	}
};


extern "C" __declspec(dllexport)
int *OakDOpen(int w, int h)
{
	OakDCamera* tp = new OakDCamera(w, h);
	return (int *)tp;
}

extern "C" __declspec(dllexport)
int* OakDintrinsicsLeft(OakDCamera * tp)
{
	dai::Device device;
	dai::CalibrationHandler calibData = device.readCalibration();
	cout << "Intrinsics from getCameraIntrinsics function 1280 x 720  ->" << endl;
	std::vector<std::vector<float>> intrin = calibData.getCameraIntrinsics(dai::CameraBoardSocket::LEFT, 1280, 720);

	int i = 0;
	for (auto row : intrin) 
	{
		for (auto val : row)
		{
			tp->intrinsicsLeft[i++] = val;
		}
	}
	for (auto row : intrin) {
		for (auto val : row) cout << val << "  ";
		cout << endl;
	}
	return (int *)&tp->intrinsicsLeft;
}

extern "C" __declspec(dllexport)
int* OakDintrinsicsRight(OakDCamera * tp)
{
	dai::Device device;
	dai::CalibrationHandler calibData = device.readCalibration();
	cout << "Intrinsics from getCameraIntrinsics function 1280 x 720  ->" << endl;
	std::vector<std::vector<float>> intrin = calibData.getCameraIntrinsics(dai::CameraBoardSocket::RIGHT, 1280, 720);

	int i = 0;
	for (auto row : intrin)
	{
		for (auto val : row)
		{
			tp->intrinsicsRight[i++] = val;
		}
	}

	for (auto row : intrin) {
		for (auto val : row) cout << val << "  ";
		cout << endl;
	}
	return (int *)&tp->intrinsicsRight;
}

extern "C" __declspec(dllexport)
int* OakDExtrinsics(OakDCamera * tp)
{
	return 0;
}

extern "C" __declspec(dllexport)
double OakDIMUTimeStamp(OakDCamera * tp)
{
	return 0;
}

extern "C" __declspec(dllexport)
int* OakDGyro(OakDCamera * tp)
{
	return 0;
}

extern "C" __declspec(dllexport)
int * OakDAccel(OakDCamera * tp)
{
	return 0;
}

extern "C" __declspec(dllexport)
int* OakDPointCloud(OakDCamera * tp)
{
	return 0;
}

extern "C" __declspec(dllexport)
int* OakDColor(OakDCamera * tp)
{
	return (int*)tp->rgb.data;
}

extern "C" __declspec(dllexport)
int* OakDRGBDepth(OakDCamera * tp)
{
	return (int*)tp->RGBdepth.data;
}

extern "C" __declspec(dllexport)
int* OakDLeftRaw(OakDCamera * tp)
{
	return (int*)tp->leftView.data;
}

extern "C" __declspec(dllexport)
int* OakDRightRaw(OakDCamera * tp)
{
	return (int*)tp->rightView.data;
}

extern "C" __declspec(dllexport)
int* OakDRawDepth(OakDCamera * tp)
{
	return (int*)tp->depth8u.data;
}

extern "C" __declspec(dllexport)
void OakDWaitForFrame(OakDCamera * tp)
{
	tp->waitForFrame();
}

extern "C" __declspec(dllexport)
void OakDStop(OakDCamera * tp)
{
	if (tp != 0) delete tp;
}
#endif // OPENCV_OAKD