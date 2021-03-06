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
#ifdef _DEBUG
#include "../Data/PragmaLibsD.h"
#else
#include "../Data/PragmaLibs.h"
#endif

using namespace std;
using namespace cv;

class RealSense2Camera
{
public:
	rs2_intrinsics intrinsicsLeft;
	rs2_extrinsics extrinsics;
	rs2::pipeline_profile profiles;
	rs2::pipeline pipeline;
	rs2::frameset frames;
	rs2::frameset processedFrames;
	rs2::colorizer colorizer;
	rs2::align align = rs2::align(RS2_STREAM_COLOR);
	rs2::pointcloud pc;
	rs2::frame RGBDepth;
	int width, height;
	rs2::frame accel, gyro;
	float depth_scale;

private:

	rs2::context ctx;

public:
	~RealSense2Camera(){}

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

	RealSense2Camera(int w, int h, int deviceIndex)
	{
		rs2_error* e = 0;
		width = w;
		height = h;

		rs2::config cfg;

		string serialNumber;
		for (auto&& dev : ctx.query_devices())
		{
			serialNumber = dev.get_info(RS2_CAMERA_INFO_SERIAL_NUMBER);
			if (deviceIndex-- == 0) break;
		}

		cfg.enable_device(serialNumber);
		cfg.enable_stream(RS2_STREAM_COLOR, width, height, RS2_FORMAT_BGR8);
		cfg.enable_stream(RS2_STREAM_DEPTH, width, height, RS2_FORMAT_Z16);
		cfg.enable_stream(RS2_STREAM_INFRARED, 1, width, height, RS2_FORMAT_Y8);
		cfg.enable_stream(RS2_STREAM_INFRARED, 2, width, height, RS2_FORMAT_Y8);

		cfg.enable_stream(RS2_STREAM_GYRO);
		cfg.enable_stream(RS2_STREAM_ACCEL);

		profiles = pipeline.start(cfg);

		depth_scale = get_depth_scale(profiles.get_device());

		auto stream = profiles.get_stream(RS2_STREAM_COLOR);
		intrinsicsLeft = stream.as<rs2::video_stream_profile>().get_intrinsics();
		auto fromStream = profiles.get_stream(RS2_STREAM_COLOR);
		auto toStream = profiles.get_stream(RS2_STREAM_INFRARED);
		extrinsics = fromStream.get_extrinsics_to(toStream);
	}

	void waitForFrame()
	{
		frames = pipeline.wait_for_frames(5000);
		gyro = frames.first_or_default(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F);
		accel = frames.first_or_default(RS2_STREAM_ACCEL, RS2_FORMAT_MOTION_XYZ32F);
	}
};


extern "C" __declspec(dllexport)
int *RS2Open(int w, int h, int deviceIndex)
{
	RealSense2Camera* tp = new RealSense2Camera(w, h, deviceIndex);
	return (int *)tp;
}

extern "C" __declspec(dllexport)
int* RS2intrinsicsLeft(RealSense2Camera * tp)
{
	return (int*)&tp->intrinsicsLeft;
}

extern "C" __declspec(dllexport)
int* RS2Extrinsics(RealSense2Camera* tp)
{
	return (int *) &tp->extrinsics;
}

extern "C" __declspec(dllexport)
double RS2IMUTimeStamp(RealSense2Camera* tp)
{
	if (tp->gyro == 0) return 0;
	return tp->gyro.get_timestamp();
}

extern "C" __declspec(dllexport)
int* RS2Gyro(RealSense2Camera * tp)
{
	if (tp->gyro == 0) return 0;
	return (int*)tp->gyro.get_data();
}

extern "C" __declspec(dllexport)
int * RS2Accel(RealSense2Camera * tp)
{
	if (tp->accel == 0) return 0;
	return (int *)tp->accel.get_data();
}

extern "C" __declspec(dllexport)
int* RS2PointCloud(RealSense2Camera * tp)
{
	return (int*)tp->pc.process(tp->processedFrames.get_depth_frame()).as<rs2::points>().get_data();
}

extern "C" __declspec(dllexport)
int* RS2Color(RealSense2Camera * tp)
{
	tp->processedFrames = tp->colorizer.process(tp->frames);
	tp->processedFrames = tp->align.process(tp->processedFrames);
	return (int*)tp->processedFrames.get_color_frame().get_data();
}

extern "C" __declspec(dllexport)
int* RS2LeftRaw(RealSense2Camera* tp)
{
	return (int*)tp->frames.get_infrared_frame(1).get_data();
}

extern "C" __declspec(dllexport)
int* RS2RightRaw(RealSense2Camera * tp)
{
	return (int*)tp->frames.get_infrared_frame(2).get_data();
}

extern "C" __declspec(dllexport)
int* RS2RGBDepth(RealSense2Camera * tp)
{
	return (int*)tp->colorizer.process(tp->processedFrames.get_depth_frame()).get_data();
}

extern "C" __declspec(dllexport)
int* RS2RawDepth(RealSense2Camera * tp)
{
	return (int*)tp->processedFrames.get_depth_frame().get_data();
}

extern "C" __declspec(dllexport)
void RS2WaitForFrame(RealSense2Camera * tp)
{
	tp->waitForFrame();
}

extern "C" __declspec(dllexport)
float RS2DepthScale(RealSense2Camera * tp)
{
	return tp->depth_scale;
}

extern "C" __declspec(dllexport)
void RS2Stop(RealSense2Camera * tp)
{
	tp->pipeline.stop();
	delete tp;
}