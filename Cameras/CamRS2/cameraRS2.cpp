#include <iostream>
#include <list> 
#include <iterator> 
#include <iomanip>
#include <cstring>
#include <string>
#include <thread>
#include <librealsense2/rs.hpp>
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
#include "example.hpp"
#include "PragmaLibs.h" 
#include <string.h>
#include <Windows.h>
#include <OleAuto.h>
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <numeric>
#include <iomanip>
#include <sstream>
#include <iostream>
#include <memory>


using namespace std;
using namespace  cv;

class RealSense2Camera
{
public:
	rs2_intrinsics intrinsics;
	rs2::pipeline_profile profiles;
	rs2::pipeline pipe;
	rs2::frameset frames;
	rs2::frameset processedFrames;
	rs2::pointcloud pc;
	rs2::frame accel, gyro;
	float depth_scale = 0.0f;
	string serialNumber;
	Size captureRes;
	Mat color, leftView, rightView, pointCloud;
private:


public:
	~RealSense2Camera() {}

	float get_depth_scale(rs2::device dev)
	{
		// Go over the device's sensors
		for (rs2::sensor& sensor : dev.query_sensors())
		{
			// Check if the sensor if a depth sensor
			if (rs2::depth_sensor dpt = sensor.as<rs2::depth_sensor>())
			{
				return dpt.get_depth_scale();
			}
		}
		throw std::runtime_error("Device does not have a depth sensor");
	}
	RealSense2Camera() {}
	RealSense2Camera(int w, int h, string devName)
	{
		captureRes = Size(w, h);

		string serialNumber;
		rs2::context ctx;
		std::string searchName = devName;
		for (auto&& dev : ctx.query_devices())
		{
			std::string device_name(dev.get_info(RS2_CAMERA_INFO_NAME));
			if (device_name.compare(searchName) == 0)
			{
				serialNumber = dev.get_info(RS2_CAMERA_INFO_SERIAL_NUMBER);
			}
		}

		rs2::config cfg;
		cfg.enable_device(serialNumber);

		cfg.enable_stream(RS2_STREAM_COLOR, w, h, RS2_FORMAT_BGR8);
		cfg.enable_stream(RS2_STREAM_DEPTH, w, h, RS2_FORMAT_Z16);
		cfg.enable_stream(RS2_STREAM_INFRARED, 1, w, h, RS2_FORMAT_Y8);
		cfg.enable_stream(RS2_STREAM_INFRARED, 2, w, h, RS2_FORMAT_Y8);

		cfg.enable_stream(RS2_STREAM_GYRO);
		cfg.enable_stream(RS2_STREAM_ACCEL);

		profiles = pipe.start(cfg);

		depth_scale = get_depth_scale(profiles.get_device());
		auto stream = profiles.get_stream(RS2_STREAM_COLOR);
		intrinsics = stream.as<rs2::video_stream_profile>().get_intrinsics();
	}		

	void waitForFrame(int w, int h)
	{
		frames = pipe.wait_for_frames(5000);
		
		static rs2::align align_to_color(RS2_STREAM_COLOR);
		processedFrames = align_to_color.process(frames);
		color = Mat(captureRes.height, captureRes.width, CV_8UC3, (int*)processedFrames.get_color_frame().get_data());
		if (w != captureRes.width || h != captureRes.height)
			resize(color, color, Size(w, h), 0, 0, INTER_NEAREST);

		leftView = Mat(captureRes.height, captureRes.width, CV_8UC1, (int*)processedFrames.get_infrared_frame(1).get_data());
		if (w != captureRes.width || h != captureRes.height)
			resize(leftView, leftView, Size(w, h), 0, 0, INTER_NEAREST);

		rightView = Mat(captureRes.height, captureRes.width, CV_8UC1, (int*)processedFrames.get_infrared_frame(2).get_data());
		if (w != captureRes.width || h != captureRes.height)
			resize(rightView, rightView, Size(w, h), 0, 0, INTER_NEAREST);

		gyro = frames.first_or_default(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F);
		accel = frames.first_or_default(RS2_STREAM_ACCEL, RS2_FORMAT_MOTION_XYZ32F);
	}
};

extern "C" __declspec(dllexport) double RS2IMUTimeStamp(RealSense2Camera* cPtr) { if (cPtr->gyro == 0) return 0; return cPtr->gyro.get_timestamp();}
extern "C" __declspec(dllexport) int* RS2Gyro(RealSense2Camera * cPtr) {if (cPtr->gyro == 0) return 0; return (int*)cPtr->gyro.get_data();}
extern "C" __declspec(dllexport) int * RS2Accel(RealSense2Camera * cPtr) { if (cPtr->accel == 0) return 0; return (int *)cPtr->accel.get_data(); }
extern "C" __declspec(dllexport) int* RS2LeftRaw(RealSense2Camera* cPtr) { return (int*)cPtr->leftView.data;}
extern "C" __declspec(dllexport) int* RS2RightRaw(RealSense2Camera * cPtr) { return (int*)cPtr->rightView.data; }
extern "C" __declspec(dllexport) int* RS2intrinsics(RealSense2Camera * cPtr) { return (int*)&cPtr->intrinsics.ppx; }
extern "C" __declspec(dllexport) int* RS2Color(RealSense2Camera * cPtr) { return (int*)cPtr->color.data; }
extern "C" __declspec(dllexport) int* RS2PointCloud(RealSense2Camera * cPtr)
{
	return (int*)cPtr->pc.process(cPtr->processedFrames.get_depth_frame()).as<rs2::points>().get_data();
}
extern "C" __declspec(dllexport) void RS2WaitForFrame(RealSense2Camera * cPtr, int w, int h)
{ 
	cPtr->waitForFrame(w, h); 
}
extern "C" __declspec(dllexport) 
void RS2Stop(RealSense2Camera * cPtr) { 
	if (cPtr != 0) 
	{ 
		cPtr->pipe.stop(); 
		delete cPtr; 
	} 
}
extern "C" __declspec(dllexport) int* RS2Open(LPSTR deviceName, int captureWidth, int captureHeight)
{
	std::string devName = deviceName;
	return (int*) new RealSense2Camera(captureWidth, captureHeight, devName);
}

