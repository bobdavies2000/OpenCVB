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
	Mat depth16u;
	Mat RGBdepth;
	Mat rgb;
	Mat leftView;
	Mat rightView;
	Mat disparity;
	dai::CalibrationHandler deviceCalib;
	double imuTimeStamp;

	dai::IMUReportAccelerometer acceleroMeter;
	dai::IMUReportGyroscope gyroscope;
	
	~OakDCamera(){}

	OakDCamera(int cols, int rows)
	{
		rgb = Mat(rows, cols, CV_8UC3);
		RGBdepth = Mat(rows, cols, CV_8UC3);
		depth16u = Mat(rows, cols, CV_16UC1);
		leftView = Mat(rows, cols, CV_8UC1);
		rightView = Mat(rows, cols, CV_8UC1);
		disparity = Mat(rows, cols, CV_8UC1);

		// Define sources and outputs
		static auto camRgb = pipeline.create<dai::node::ColorCamera>();
		static auto monoLeft = pipeline.create<dai::node::MonoCamera>();
		static auto monoRight = pipeline.create<dai::node::MonoCamera>();
		static auto stereo = pipeline.create<dai::node::StereoDepth>();
		
		static auto xoutLeft = pipeline.create<dai::node::XLinkOut>();
		static auto xoutRight = pipeline.create<dai::node::XLinkOut>();
		static auto xoutDisp = pipeline.create<dai::node::XLinkOut>();
		static auto xoutDepth = pipeline.create<dai::node::XLinkOut>();
		static auto xoutRectifL = pipeline.create<dai::node::XLinkOut>();
		static auto xoutRectifR = pipeline.create<dai::node::XLinkOut>();
		static auto xoutRgb = pipeline.create<dai::node::XLinkOut>();

		// XLinkOut
		xoutLeft->setStreamName("left");
		xoutRight->setStreamName("right");
		xoutDisp->setStreamName("disparity");
		xoutDepth->setStreamName("depth");
		xoutRectifL->setStreamName("rectified_left");
		xoutRectifR->setStreamName("rectified_right");
		xoutRgb->setStreamName("rgb");
		camRgb->setInterleaved(false);
		camRgb->setColorOrder(dai::ColorCameraProperties::ColorOrder::RGB);
		camRgb->setResolution(dai::ColorCameraProperties::SensorResolution::THE_1080_P);
		camRgb->setBoardSocket(dai::CameraBoardSocket::RGB);
		camRgb->isp.link(xoutRgb->input);
		camRgb->setPreviewSize(1280, 720);
		camRgb->initialControl.setManualFocus(135);

		// Properties
		monoLeft->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);
		monoLeft->setBoardSocket(dai::CameraBoardSocket::LEFT);
		monoRight->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);
		monoRight->setBoardSocket(dai::CameraBoardSocket::RIGHT);

		// StereoDepth
		stereo->initialConfig.setConfidenceThreshold(230);
		stereo->setRectifyEdgeFillColor(0);  // black, to better see the cutout
		stereo->initialConfig.setMedianFilter(dai::MedianFilter::MEDIAN_OFF);
		stereo->setLeftRightCheck(true);
		stereo->setExtendedDisparity(false);
		stereo->setSubpixel(false);

		// Linking
		monoLeft->out.link(stereo->left);
		monoRight->out.link(stereo->right);

		stereo->syncedLeft.link(xoutLeft->input);
		stereo->syncedRight.link(xoutRight->input);
		stereo->disparity.link(xoutDisp->input);

		stereo->rectifiedLeft.link(xoutRectifL->input);
		stereo->rectifiedRight.link(xoutRectifR->input);
		stereo->depth.link(xoutDepth->input);

		// LR-check is required for depth alignment
		stereo->setLeftRightCheck(true);
		stereo->setDepthAlign(dai::CameraBoardSocket::RGB);

		static auto imu = pipeline.create<dai::node::IMU>();
		static auto xlinkImu = pipeline.create<dai::node::XLinkOut>();
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

		monoLeft->out.link(xoutLeft->input);

		maxDisparity = stereo->getMaxDisparity();
	}

	void waitForFrame(bool close)
	{
		using namespace std::chrono;
		// Connect to device and start pipeline
		static dai::Device device(pipeline);
		static auto qRgb = device.getOutputQueue("rgb", 4, false);
		static auto leftQueue = device.getOutputQueue("left", 8, false);
		static auto rightQueue = device.getOutputQueue("right", 8, false);
		static auto dispQueue = device.getOutputQueue("disparity", 8, false);
		static auto depthQueue = device.getOutputQueue("depth", 8, false);
		static auto rectifLeftQueue = device.getOutputQueue("rectified_left", 8, false);
		static auto rectifRightQueue = device.getOutputQueue("rectified_right", 8, false);
		static auto imuQueue = device.getOutputQueue("imu", 50, false);

		if (close) {
			device.close();
			while (1)
				if (device.isClosed()) return;
			return;
		}

		rgb = qRgb->get<dai::ImgFrame>()->getCvFrame().clone();

		disparity = dispQueue->get<dai::ImgFrame>()->getCvFrame().clone();
		disparity.convertTo(disparity, CV_8UC1, maxDisparity);  // Extend disparity range
		cv::applyColorMap(disparity, RGBdepth, cv::COLORMAP_JET);

		static auto depth = depthQueue->get<dai::ImgFrame>();
		depth16u = cv::Mat(depth->getHeight(), depth->getWidth(), CV_16UC1, depth->getData().data()).clone();

		auto left = rectifLeftQueue->get<dai::ImgFrame>();
		leftView = left->getFrame().clone();
		auto right = rectifRightQueue->get<dai::ImgFrame>();
		rightView = right->getFrame().clone();

		//auto left = leftQueue->get<dai::ImgFrame>();
		//leftView = left->getFrame().clone();

		//auto right = rightQueue->get<dai::ImgFrame>();
		//rightView = right->getFrame().clone();

		auto imuData = imuQueue->get<dai::IMUData>();
		auto imuPackets = imuData->packets; 
		for (auto& imuPacket : imuPackets) {
			auto& acceleroValues = imuPacket.acceleroMeter;
			auto& gyroValues = imuPacket.gyroscope;
			acceleroMeter = acceleroValues;
			gyroscope = gyroValues;

			// auto tmp = acceleroValues.timestamp.get().time_since_epoch();
			auto ms_time = std::chrono::time_point_cast<std::chrono::milliseconds>(acceleroValues.timestamp.get());
			imuTimeStamp = (double) std::chrono::duration_cast<std::chrono::milliseconds>(ms_time.time_since_epoch()).count();
		}
	}
};


extern "C" __declspec(dllexport)
int *OakDOpen(int w, int h)
{
	OakDCamera* tp = new OakDCamera(w, h);
	dai::Device device;
	tp->deviceCalib = device.readCalibration();
	return (int *)tp;
}

extern "C" __declspec(dllexport)
int* OakDintrinsicsLeft(OakDCamera * tp)
{
	std::vector<std::vector<float>> intrin = tp->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::LEFT, 1280, 720);
	int i = 0;
	for (auto row : intrin)  for (auto val : row) tp->intrinsicsLeft[i++] = val;
	return (int *)&tp->intrinsicsLeft;
}

extern "C" __declspec(dllexport)
int* OakDintrinsicsRight(OakDCamera * tp)
{
	std::vector<std::vector<float>> intrin = tp->deviceCalib.getCameraIntrinsics(dai::CameraBoardSocket::RIGHT, 1280, 720);
	int i = 0;
	for (auto row : intrin) for (auto val : row) tp->intrinsicsRight[i++] = val; 
	return (int *)&tp->intrinsicsRight;
}

//http://graphics.cs.cmu.edu/courses/15-463/2017_fall/lectures/lecture19.pdf
extern "C" __declspec(dllexport) int* OakDPointCloud(OakDCamera * tp) { return 0; }

extern "C" __declspec(dllexport) int* OakDExtrinsics(OakDCamera * tp) { return 0; }
extern "C" __declspec(dllexport) double OakDIMUTimeStamp(OakDCamera * tp) { return tp->imuTimeStamp;}
extern "C" __declspec(dllexport) int* OakDGyro(OakDCamera * tp) { return (int *)&tp->gyroscope.x; }
extern "C" __declspec(dllexport) int* OakDAccel(OakDCamera * tp) { return (int*)&tp->acceleroMeter.x;}
extern "C" __declspec(dllexport) int* OakDDisparity(OakDCamera * tp) { return (int*)tp->disparity.data; }
extern "C" __declspec(dllexport) float OakDMaxDisparity(OakDCamera * tp) { return tp->maxDisparity; }
extern "C" __declspec(dllexport) int* OakDColor(OakDCamera * tp) { return (int*)tp->rgb.data; }
extern "C" __declspec(dllexport) int* OakDRGBDepth(OakDCamera * tp){ return (int*)tp->RGBdepth.data; }
extern "C" __declspec(dllexport) int* OakDLeftRaw(OakDCamera * tp) { return (int*)tp->leftView.data;}
extern "C" __declspec(dllexport) int* OakDRightRaw(OakDCamera * tp) { return (int*)tp->rightView.data;}
extern "C" __declspec(dllexport) int* OakDRawDepth(OakDCamera * tp) { return (int*)tp->depth16u.data; }
extern "C" __declspec(dllexport) void OakDWaitForFrame(OakDCamera * tp) { tp->waitForFrame(false);}
extern "C" __declspec(dllexport) void OakDStop(OakDCamera * tp) 
{ 
	cout << "stopping the camera" << endl;
	tp->waitForFrame(true);
	if (tp != 0) delete tp;
	cout << "stopped" << endl;
}
#endif // OPENCV_OAKD