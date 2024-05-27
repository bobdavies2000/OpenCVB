#include "../CameraDefines.hpp"
#ifdef ORB335L
#pragma warning(disable : 4996)
#pragma comment(lib, "mynteye_depth.lib")
#pragma comment(lib, "opencv_world343.lib") 
#include <iostream>

#define WITH_OPENCV
#include <mynteyed\camera.h>
#include <mynteyed\utils.h>
#include "util/cam_utils.h"
#include "util/counter.h"


MYNTEYE_USE_NAMESPACE
using namespace  cv;
using namespace std;

class CameraMyntD
{
	 
public:
	Camera cam;
	DeviceInfo dev_info;
	uchar *left_color, *right_color;
	int width, height;
	Mat color, leftView, rightView, pointCloud, pcFullSize;
	StreamIntrinsics intrinsicsBoth;
private:
public:
	~CameraMyntD()
	{
		cam.Close();
	}
	CameraMyntD(int _width, int _height)
	{
		width = _width;
		height = _height;
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

		intrinsicsBoth = cam.GetStreamIntrinsics(params.stream_mode);
		pcFullSize = Mat(height, width, CV_32FC3);
	}

	void waitForFrame()
	{
		left_color = right_color = 0;  // assume we don't get any images.
		auto left = cam.GetStreamData(ImageType::IMAGE_LEFT_COLOR);
		if (left.img) left_color = left.img->To(ImageFormat::COLOR_BGR)->ToMat().data;
		
		auto right = cam.GetStreamData(ImageType::IMAGE_RIGHT_COLOR);
		if (right.img) right_color = right.img->To(ImageFormat::COLOR_BGR)->ToMat().data;
		
		auto image_depth = cam.GetStreamData(ImageType::IMAGE_DEPTH);
		Mat depth;
		if (image_depth.img) depth = image_depth.img->ToMat();

		Point3f p;
		CameraIntrinsics cam_in_ = intrinsicsBoth.left;
		for (int m = 0; m < depth.rows; m++) {
			for (int n = 0; n < depth.cols; n++) {
				// get depth value at (m, n)
				std::uint16_t d = depth.ptr<std::uint16_t>(m)[n];
				// when d is equal 0 or 4096 means no depth
				if (d == 0 || d == 4096) d = 0;

				p.z = static_cast<float>(d) / 1000.0f;
				p.x = (n - cam_in_.cx) * p.z / cam_in_.fx;
				p.y = (m - cam_in_.cy) * p.z / cam_in_.fy;

				pcFullSize.at<Point3f>(m, n) = p;
			}
		}
	}
}; 

float acceleration[3];
float gyro[3];
double imuTimeStamp;

extern "C" __declspec(dllexport) void MyntDtaskIMU(CameraMyntD * cPtr)
{
	if (cPtr->cam.IsMotionDatasSupported()) cPtr->cam.EnableMotionDatas(0);
	util::Counter counter;

	// Set motion data callback
	cPtr->cam.SetMotionCallback([&counter](const MotionData& data) {
		if (data.imu->flag == MYNTEYE_IMU_ACCEL) {
			counter.IncrAccelCount();
			acceleration[0] = data.imu->accel[0] * 9.807; 
			acceleration[1] = data.imu->accel[1] * 9.807;
			acceleration[2] = data.imu->accel[2] * 9.807;
			imuTimeStamp = data.imu->timestamp;
			//imuTemperature = data.imu->temperature;
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
		cPtr->cam.WaitForStream();
		counter.Update();
	}
}

extern "C" __declspec(dllexport) int* MyntDWaitFrame(CameraMyntD * cPtr, int w, int h) 
{ 
	cPtr->waitForFrame();
	if (cPtr->left_color == 0 || cPtr->right_color == 0 || cPtr->pcFullSize.data == 0) return 0;
	cPtr->color = Mat(cPtr->height, cPtr->width, CV_8UC3, cPtr->left_color);

	// resize(tmp, cPtr->color, Size(w, h));  // can't get it to link with resize - Mynt is setup with 3.43 version of OpenCV.

	cPtr->rightView = Mat(cPtr->height, cPtr->width, CV_8UC3, (int*) cPtr->right_color);
	// resize(tmp, cPtr->rightView, Size(w, h));

	// resize(cPtr->pcFullSize, cPtr->pointCloud, Size(w, h), INTER_NEAREST);
	cPtr->pointCloud = cPtr->pcFullSize;
	return (int*)cPtr->color.data;
}
extern "C" __declspec(dllexport) int* MyntDOpen(int width, int height, int frameRate) { return (int*) new CameraMyntD(width, height);}
extern "C" __declspec(dllexport) void MyntDClose(CameraMyntD * cPtr) { delete cPtr; }
extern "C" __declspec(dllexport) int* MyntDIntrinsicsLeft(CameraMyntD * cPtr) { return (int*)&cPtr->intrinsicsBoth.left; }
extern "C" __declspec(dllexport) int* MyntDRightImage(CameraMyntD * cPtr) {return (int*)cPtr->rightView.data;}
extern "C" __declspec(dllexport) int* MyntDPointCloud(CameraMyntD * cPtr) { return (int*)cPtr->pointCloud.data; }
extern "C" __declspec(dllexport) int* MyntDAcceleration(CameraMyntD * cPtr){return (int*)&acceleration;}
extern "C" __declspec(dllexport) int* MyntDGyro(CameraMyntD * cPtr){return (int*)&gyro;}
extern "C" __declspec(dllexport) double MyntDIMU_TimeStamp(CameraMyntD * cPtr){return imuTimeStamp;}
#endif