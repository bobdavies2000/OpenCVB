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
	std::unordered_map<std::string, Mat> frame;
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
	Mat getRGBDepth(Mat depth16) {
		int histSize = 65535;
		float nearColor[3] = { 0, 1.0f, 1.0f };
		float farColor[3] = { 1.0f, 0, 0 };
		float hRange[] = { 1, float(histSize) };  // ranges are exclusive at the top of the range
		const float* range[] = { hRange };
		int hbins[] = { histSize };
		Mat hist;
		if (countNonZero(depth16) > 0)
		{
			// use OpenCV's histogram rather than binning manually.
			calcHist(&depth16, 1, 0, Mat(), hist, 1, hbins, range, true, false);
		}

		float* histogram = (float*)hist.data;
		// Produce a cumulative histogram of depth values
		for (int i = 1; i < histSize; i++)
		{
			histogram[i] += histogram[i - 1] + 1;
		}

		Mat output(depth16.size(), CV_8UC3);
		output.setTo(0);
		if (histogram[histSize - 1] > 0)
		{
			hist *= 1.0f / histogram[histSize - 1];

			// Produce RGB image by using the histogram to interpolate between two colors
			auto rgb = (unsigned char*)output.data;
			unsigned short* depthImage = (unsigned short*)depth16.data;
			for (int i = 0; i < output.cols * output.rows; i++) {
				auto d = depthImage[i];
				if (d > 0 && d < histSize) {
					auto t = histogram[d];  // Use the histogram entry (in the range of 0..1) to interpolate between nearColor and farColor
					*rgb++ = uchar(((1 - t) * nearColor[0] + t * farColor[0]) * 255);
					*rgb++ = uchar(((1 - t) * nearColor[1] + t * farColor[1]) * 255);
					*rgb++ = uchar(((1 - t) * nearColor[2] + t * farColor[2]) * 255);
				}
				else {
					*rgb++ = 0;
					*rgb++ = 0;
					*rgb++ = 0;
				}
			}
		}
		return output;
	}

	OakDCamera(int cols, int rows)
	{
		rgb = Mat(rows, cols, CV_8UC3);
		RGBdepth = Mat(rows, cols, CV_8UC3);
		depth16u = Mat(rows, cols, CV_16UC1);
		leftView = Mat(rows, cols, CV_8UC1);
		rightView = Mat(rows, cols, CV_8UC1);
		disparity = Mat(rows, cols, CV_8UC1);

		static auto camRgb = pipeline.create<dai::node::ColorCamera>();
		static auto monoLeft = pipeline.create<dai::node::MonoCamera>();
		static auto monoRight = pipeline.create<dai::node::MonoCamera>();
		static auto stereo = pipeline.create<dai::node::StereoDepth>() ;
		static auto xoutLeft = pipeline.create<dai::node::XLinkOut>();
		static auto xoutRight = pipeline.create<dai::node::XLinkOut>();
		static auto xoutDepth = pipeline.create<dai::node::XLinkOut>();
		static auto xoutRectifL = pipeline.create<dai::node::XLinkOut>();
		static auto xoutRectifR = pipeline.create<dai::node::XLinkOut>();
		static auto xoutRgb = pipeline.create<dai::node::XLinkOut>();

		camRgb->setInterleaved(false);
		camRgb->setColorOrder(dai::ColorCameraProperties::ColorOrder::RGB);
		camRgb->setResolution(dai::ColorCameraProperties::SensorResolution::THE_1080_P);
		camRgb->setBoardSocket(dai::CameraBoardSocket::RGB);
		camRgb->isp.link(xoutRgb->input); // If the Oak-D camera is stopped and then restarted, it will fail here.  Need to find out why.  Should be simple.
		camRgb->setPreviewSize(cols, rows); 
		camRgb->initialControl.setManualFocus(135);

		// added these 
		camRgb->setFps(30);
		if (downscaleColor) camRgb->setIspScale(2, 3);

		// XLinkOut
		xoutLeft->setStreamName("left");
		xoutRight->setStreamName("right");
		xoutDepth->setStreamName("depth");
		xoutRectifL->setStreamName("rectified_left");
		xoutRectifR->setStreamName("rectified_right");
		xoutRgb->setStreamName("rgb");

		// Properties
		monoLeft->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);
		monoLeft->setBoardSocket(dai::CameraBoardSocket::LEFT);
		monoRight->setResolution(dai::MonoCameraProperties::SensorResolution::THE_720_P);
		monoRight->setBoardSocket(dai::CameraBoardSocket::RIGHT);

		stereo->setRectifyEdgeFillColor(0);  // black, to better see the cutout
		stereo->initialConfig.setMedianFilter(dai::MedianFilter::MEDIAN_OFF);
		stereo->setLeftRightCheck(true); // LR-check is required for depth alignment
		stereo->setSubpixel(false);

		// added these
		stereo->initialConfig.setConfidenceThreshold(230);
		stereo->setDepthAlign(dai::CameraBoardSocket::RGB);

		// Linking
		monoLeft->out.link(stereo->left);
		monoRight->out.link(stereo->right);

		stereo->rectifiedLeft.link(xoutRectifL->input);
		stereo->rectifiedRight.link(xoutRectifR->input);
		stereo->depth.link(xoutDepth->input);
		stereo->syncedLeft.link(xoutLeft->input);
		stereo->syncedRight.link(xoutRight->input);

		static auto imu = pipeline.create<dai::node::IMU>();
		static auto xlinkImu = pipeline.create<dai::node::XLinkOut>();
		xlinkImu->setStreamName("imu");
		imu->enableIMUSensor({ dai::IMUSensor::ACCELEROMETER_RAW, dai::IMUSensor::GYROSCOPE_RAW }, 500);

		// above this threshold packets will be sent in batch of X, if the host is not blocked and USB bandwidth is available
		imu->setBatchReportThreshold(1);

		// maximum number of IMU packets in a batch, if it's reached device will block sending until host can receive it
		// if lower or equal to batchReportThreshold then the sending is always blocking on device
		// useful to reduce device's CPU load  and number of lost packets, if CPU load is high on device side due to multiple nodes
		imu->setMaxBatchReports(1);
		imu->out.link(xlinkImu->input);

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

		auto depth = depthQueue->get<dai::ImgFrame>();
		depth16u = depth->getCvFrame();
		RGBdepth = getRGBDepth(depth16u);

		auto left = rectifLeftQueue->get<dai::ImgFrame>();
		leftView = left->getFrame().clone();
		auto right = rectifRightQueue->get<dai::ImgFrame>();
		rightView = right->getFrame().clone();

		//auto left = leftQueue->get<dai::ImgFrame>();
		//leftView = left->getFrame().clone();

		//auto right = rightQueue->get<dai::ImgFrame>();
		//rightView = right->getFrame().clone();

		auto imuData = imuQueue->get<dai::IMUData>();
		acceleroMeter = imuData->packets[0].acceleroMeter;
		gyroscope = imuData->packets[0].gyroscope;

		// auto tmp = acceleroValues.timestamp.get().time_since_epoch();
		auto ms_time = std::chrono::time_point_cast<std::chrono::milliseconds>(acceleroMeter.timestamp.get());
		imuTimeStamp = (double) std::chrono::duration_cast<std::chrono::milliseconds>(ms_time.time_since_epoch()).count();
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
	tp->waitForFrame(true);
	if (tp != 0) delete tp;
}
#endif // OPENCV_OAKD