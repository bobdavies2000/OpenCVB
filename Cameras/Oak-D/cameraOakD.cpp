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
	
	~OakDCamera(){}

	OakDCamera()
	{
		// Define sources and outputs
		auto camRgb = pipeline.create<dai::node::ColorCamera>();
		auto left = pipeline.create<dai::node::MonoCamera>();
		auto right = pipeline.create<dai::node::MonoCamera>();
		auto stereo = pipeline.create<dai::node::StereoDepth>();

		auto rgbOut = pipeline.create<dai::node::XLinkOut>();
		auto depthOut = pipeline.create<dai::node::XLinkOut>();

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

		maxDisparity = stereo->getMaxDisparity();
	}

	void waitForFrame()
	{
		// Connect to device and start pipeline
		static dai::Device device(pipeline);

		static bool initialized = false;
		if (initialized == false)
		{
			// Sets queues size and behavior
			for (const auto& name : queueNames) {
				device.getOutputQueue(name, 4, false);
			}
			initialized = true;
		}

		auto queueEvents = device.getQueueEvents(queueNames);
		for (const auto& name : queueEvents) {
			auto packets = device.getOutputQueue(name)->tryGetAll<dai::ImgFrame>();
			auto count = packets.size();
			if (count > 0) {
				latestPacket[name] = packets[count - 1];
			}
		}

		for (const auto& name : queueNames) {
			if (latestPacket.find(name) != latestPacket.end()) {
				if (name == "depth") {
					frame[name] = latestPacket[name]->getFrame();
					// Optional, extend range 0..95 -> 0..255, for a better visualisation
					if (1) frame[name].convertTo(frame[name], CV_8UC1, 255. / maxDisparity);
					// Optional, apply false colorization
					if (1) cv::applyColorMap(frame[name], frame[name], cv::COLORMAP_HOT);
				}
				else {
					frame[name] = latestPacket[name]->getCvFrame();
				}

				cv::imshow(name, frame[name]);
			}
		}

		// Blend when both received
		if (frame.find("rgb") != frame.end() && frame.find("depth") != frame.end()) {
			// Need to have both frames in BGR format before blending
			if (frame["depth"].channels() < 3) {
				cv::cvtColor(frame["depth"], frame["depth"], cv::COLOR_GRAY2BGR);
			}
			cv::Mat blended;
			cv::addWeighted(frame["rgb"], 0.6, frame["depth"], 0.4, 0, blended);
			cv::imshow("rgb-depth", blended);
			frame.clear();
		}
	}
};


extern "C" __declspec(dllexport)
int *OakDOpen(int w, int h)
{
	OakDCamera* tp = new OakDCamera();
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
	Mat rgb = tp->latestPacket["rgb"]->getFrame();
	return (int*)rgb.data;
}

extern "C" __declspec(dllexport)
int* OakDLeftRaw(OakDCamera * tp)
{
	Mat left = tp->latestPacket["rect_left"]->getFrame();
	return (int*)left.data;
}

extern "C" __declspec(dllexport)
int* OakDRightRaw(OakDCamera * tp)
{
	Mat right = tp->latestPacket["rect_right"]->getFrame();
	return (int*)right.data;
}

extern "C" __declspec(dllexport)
int* OakDRawDepth(OakDCamera * tp)
{
	Mat depth = tp->latestPacket["depth"]->getFrame();
	return (int*)depth.data;
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