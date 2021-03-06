#include "../CameraDefines.hpp"
#ifdef MYNTD_1000
#pragma warning(disable : 4996)
#pragma comment(lib, "mynteye_depth.lib")
#pragma comment(lib, "opencv_world343.lib") 
#include <iostream>

#include <opencv2/highgui/highgui.hpp>

#define WITH_OPENCV
#include <mynteyed/camera.h>
#include <mynteyed/utils.h>
#include "util/cam_utils.h"
#include "util/counter.h"

#include "../../CPP_Classes/DepthColorizer.hpp"
#ifdef _DEBUG
#include "../Data/PragmaLibsD.h"
#else
#include "../Data/PragmaLibs.h"
#endif

MYNTEYE_USE_NAMESPACE
using namespace cv;
using namespace std;

class CameraMyntD
{
public:
	Camera cam;
	DeviceInfo dev_info;
	uchar *left_color, *right_color;
	cv::Mat pointCloud, depth16;
	int rows, cols;
	Depth_Colorizer16 * cPtr;
	StreamIntrinsics intrinsicsBoth;
	StreamExtrinsics extrinsics;
private:
public:
	~CameraMyntD()
	{
		cam.Close();
	}
	CameraMyntD(int width, int height, int fps)
	{
		DeviceInfo dev_info;
		if (!util::select(cam, &dev_info))
			std::cerr << "Error: select failed" << std::endl;

		util::print_stream_infos(cam, dev_info.index);

		OpenParams params(dev_info.index);
		params.stream_mode = StreamMode::STREAM_2560x720;
		if (width == 640) params.stream_mode = StreamMode::STREAM_1280x480;
		params.framerate = 30;
		params.ir_intensity = 4;
		params.color_mode = ColorMode::COLOR_RECTIFIED;
		params.color_stream_format = StreamFormat::STREAM_YUYV;
		params.depth_mode = DepthMode::DEPTH_RAW;

		cam.Open(params);

		cPtr = new Depth_Colorizer16();
		rows = height;
		cols = width;
		intrinsicsBoth = cam.GetStreamIntrinsics(params.stream_mode);
		bool ex_ok;
		extrinsics = cam.GetStreamExtrinsics(StreamMode::STREAM_2560x720, &ex_ok);
		pointCloud = cv::Mat(height, width, CV_32FC3);

	}

	int *waitForFrame()
	{
		left_color = right_color = 0;  // assume we don't get any images.
		auto left = cam.GetStreamData(ImageType::IMAGE_LEFT_COLOR);
		if (left.img) left_color = left.img->To(ImageFormat::COLOR_BGR)->ToMat().data;
		
		auto right = cam.GetStreamData(ImageType::IMAGE_RIGHT_COLOR);
		if (right.img) right_color = right.img->To(ImageFormat::COLOR_BGR)->ToMat().data;
		
		auto depth = cam.GetStreamData(ImageType::IMAGE_DEPTH);
		if (depth.img)
		{
			depth16 = depth.img->To(ImageFormat::DEPTH_RAW)->ToMat();
			cPtr->depth16 = depth16;
			cPtr->Run();

			Point3f* p = (Point3f*)pointCloud.data;
			int cols = pointCloud.cols;
			int rows = pointCloud.rows;
			float z;
			auto intrin = intrinsicsBoth.left;	
			Point3f zero(0, 0, 0);
			for (int y = 0; y < rows; y++) {
				for (int x = 0; x < cols; x++) {
					std::uint16_t d = depth16.ptr<std::uint16_t>(y)[x];
					if (d != 0)
					{
						z = static_cast<float>(d) * 0.001;
						p[y * cols + x] = Point3f((x - intrin.cx) * z / intrin.fx, (y - intrin.cy) * z / intrin.fy, z);
					}
					else {
						p[y * cols + x] = zero;
					}
				}
			}
		}
		return (int *) left_color;
	}
}; 

float acceleration[3];
float gyro[3];
float imuTemperature;
double imuTimeStamp;

extern "C" __declspec(dllexport) void MyntDtaskIMU(CameraMyntD * MyntD)
{
	if (MyntD->cam.IsMotionDatasSupported()) MyntD->cam.EnableMotionDatas(0);
	util::Counter counter;

	// Set motion data callback
	MyntD->cam.SetMotionCallback([&counter](const MotionData& data) {
		if (data.imu->flag == MYNTEYE_IMU_ACCEL) {
			counter.IncrAccelCount();
			acceleration[0] = data.imu->accel[0] * 9.807; 
			acceleration[1] = data.imu->accel[1] * 9.807;
			acceleration[2] = data.imu->accel[2] * 9.807;
			imuTimeStamp = data.imu->timestamp;
			imuTemperature = data.imu->temperature;
		}
		else if (data.imu->flag == MYNTEYE_IMU_GYRO) {
			counter.IncrGyroCount();
			gyro[0] = data.imu->gyro[0];
			gyro[1] = data.imu->gyro[1];
			gyro[2] = data.imu->gyro[2];
		}
	});

	while (1)
	{
		MyntD->cam.WaitForStream();
		counter.Update();
	}
}

extern "C" __declspec(dllexport) int* MyntDWaitFrame(CameraMyntD * MyntD)
{
	return MyntD->waitForFrame();
}
extern "C" __declspec(dllexport) int* MyntDOpen(int width, int height, int fps)
{
	CameraMyntD* MyntD = new CameraMyntD(width, height, fps);
	return (int*)MyntD;
}
extern "C" __declspec(dllexport) void MyntDClose(CameraMyntD * MyntD)
{
	delete MyntD;
}
extern "C" __declspec(dllexport) int* MyntDLeftImage(CameraMyntD * MyntD)
{
	return (int*)MyntD->left_color;
}
extern "C" __declspec(dllexport) int* MyntDRightImage(CameraMyntD * MyntD)
{
	return (int*)MyntD->right_color;
}
extern "C" __declspec(dllexport) int* MyntDImageRGBdepth(CameraMyntD * MyntD)
{
	return (int*)MyntD->cPtr->output.data;
}
extern "C" __declspec(dllexport) int* MyntDintrinsicsLeft(CameraMyntD * MyntD)
{
	return (int*)&MyntD->intrinsicsBoth.left.width;
}
extern "C" __declspec(dllexport) int* MyntDintrinsicsRight(CameraMyntD * MyntD)
{
	return (int*)&MyntD->intrinsicsBoth.right.width;
}

extern "C" __declspec(dllexport) int* MyntDProjectionMatrix(CameraMyntD * MyntD)
{
	return (int*)&MyntD->intrinsicsBoth.right.p[0];
}

extern "C" __declspec(dllexport) int* MyntDRotationMatrix(CameraMyntD * MyntD)
{
	return (int*)&MyntD->intrinsicsBoth.right.r[0];
}
extern "C" __declspec(dllexport) int* MyntDExtrinsics(CameraMyntD * MyntD)
{
	return (int*)&MyntD->extrinsics;
}
extern "C" __declspec(dllexport) int* MyntDPointCloud(CameraMyntD * MyntD)
{
	return (int*)MyntD->pointCloud.data;
}
extern "C" __declspec(dllexport) int* MyntDRawDepth(CameraMyntD * MyntD)
{
	return (int*)MyntD->depth16.data;
}

extern "C" __declspec(dllexport) int* MyntDAcceleration(CameraMyntD * MyntD)
{
	return (int*)&acceleration;
}

extern "C" __declspec(dllexport) int* MyntDGyro(CameraMyntD * MyntD)
{
	return (int*)&gyro;
}

extern "C" __declspec(dllexport) double MyntDIMU_TimeStamp(CameraMyntD * MyntD)
{
	return imuTimeStamp;
}
extern "C" __declspec(dllexport)float MyntDIMU_Temperature(CameraMyntD * MyntD)
{
	return imuTemperature;
}
#endif