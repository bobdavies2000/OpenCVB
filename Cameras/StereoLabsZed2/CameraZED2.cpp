#include <iostream>
#include <iomanip>
#include <cstring>
#include <string>
#include <thread>
#include <mutex>
#include <cstdlib>
#include <cstdio>
#include <algorithm>

#include "../CameraDefines.hpp"
#ifdef STEREOLAB_INSTALLED

#include <opencv2/core.hpp>
#include <opencv2/calib3d.hpp>
#include <opencv2/imgproc.hpp> 
#include <opencv2/imgcodecs.hpp>
#include <opencv2/highgui.hpp>
#pragma comment(lib, "sl_zed64.lib")
#pragma comment(lib, "cuda.lib") 
#pragma comment(lib, "cudart.lib") 

#include <sl/Camera.hpp>
#include "PragmaLibs.h" 

using namespace sl;
using namespace std;
using namespace  cv;

class StereoLabsZed2
{
public:
	int serialNumber = 0;
	Translation extrinsicsTranslation;
	Rotation extrinsicsRotationMatrix;
	float acceleration[3] = { 0, 0, 0 };
	sl::float3 RotationVector;
	Rotation RotationMatrix;
	Translation IMU_Translation;
	SensorsData sensordata;
	Orientation orientation;
	float cameraData[7];
	float imuTemperature = 0;
	double imuTimeStamp = 0;
	Pose zed_pose;
	Camera zed;
	cv::Mat color, rightView, pointCloud;
	int captureWidth, captureHeight;
private:
	InitParameters init_params;
	float imuData = 0;
public:
	~StereoLabsZed2() {}
	StereoLabsZed2(int w, int h)
	{
		captureWidth = w;
		captureHeight = h;

		init_params.sensors_required = true;
		init_params.depth_mode = DEPTH_MODE::ULTRA;
		init_params.coordinate_system = COORDINATE_SYSTEM::IMAGE;
		init_params.coordinate_units = UNIT::METER;
		init_params.camera_fps = 0; // use the highest frame rate available.

		init_params.camera_resolution = sl::RESOLUTION::AUTO;
		if (w == 1920 && h == 1080) init_params.camera_resolution = sl::RESOLUTION::HD1080;
		if (w == 1920 && h == 1200) init_params.camera_resolution = sl::RESOLUTION::HD1200;
		if (w == 1280) init_params.camera_resolution = sl::RESOLUTION::HD720;
		if (w == 960) init_params.camera_resolution = sl::RESOLUTION::SVGA;
		if (w == 672) init_params.camera_resolution = sl::RESOLUTION::VGA;

		zed.open(init_params);

		CameraInformation camera_info = zed.getCameraInformation();
		cameraData[0] = camera_info.camera_configuration.calibration_parameters.left_cam.fx;
		cameraData[1] = camera_info.camera_configuration.calibration_parameters.left_cam.fy;
		cameraData[2] = camera_info.camera_configuration.calibration_parameters.left_cam.cx;
		cameraData[3] = camera_info.camera_configuration.calibration_parameters.left_cam.cy;
		cameraData[4] = camera_info.camera_configuration.calibration_parameters.left_cam.v_fov;
		cameraData[5] = camera_info.camera_configuration.calibration_parameters.left_cam.h_fov;
		cameraData[6] = camera_info.camera_configuration.calibration_parameters.left_cam.d_fov;
		serialNumber = camera_info.serial_number;
		printf("serial number = %d", serialNumber);
		extrinsicsTranslation = camera_info.camera_configuration.calibration_parameters.stereo_transform.getTranslation();
		extrinsicsRotationMatrix = camera_info.camera_configuration.calibration_parameters.stereo_transform.getRotationMatrix();

		PositionalTrackingParameters positional_tracking_param;
		positional_tracking_param.enable_area_memory = true;
		auto returned_state = zed.enablePositionalTracking(positional_tracking_param);
	}

	void waitForFrame()
	{
		while(1)
			if (zed.grab() == ERROR_CODE::SUCCESS) return;
	}

	cv::Mat getMat(void* dataPtr, int w, int h)
	{
		cv::Mat tmp;
		if (dataPtr != 0)
		{
			tmp = cv::Mat(captureHeight, captureWidth, CV_8UC4, dataPtr);
			cvtColor(tmp, tmp, ColorConversionCodes::COLOR_BGRA2BGR);
		}
		else {
			tmp = cv::Mat(captureHeight, captureWidth, CV_8UC3);
			tmp.setTo(0);
			float scale = 0.6f;
			int thickness = 2;
			int y = 20, incr = 20;
			int font = FONT_HERSHEY_SIMPLEX;
			putText(tmp, "Buffers from StereoLabs ZED driver are 0", Point(20, y), font, scale,
				Scalar::all(255), thickness);
			putText(tmp, "Recommendation: install the latest CUDA and StereoLabs SDK",
				Point(20, y + incr), font, scale, Scalar::all(255), thickness);
			putText(tmp, "A working version of CUDA is required for StereoLabs cameras.",
				Point(20, y + incr * 2), font, scale, Scalar::all(255), thickness);
			putText(tmp, "And make sure that the latest CUDA installed properly.",
				Point(20, y + incr * 3), font, scale, Scalar::all(255), thickness);
			putText(tmp, "The NSight Visual Studio Edition can fail and is not essential.",
				Point(20, y + incr * 4), font, scale, Scalar::all(255), thickness);
			putText(tmp, "Use the 'Custom' install to remove NSight.",
				Point(20, y + incr * 5), font, scale, Scalar::all(255), thickness);
		}
		if (w != captureWidth) resize(tmp, tmp, Size(w, h), INTER_NEAREST_EXACT);
		return tmp;
	}

	void GetData(int w, int h)
	{
		sl::Mat colorSL, rightViewSL, pcMatSL;

		zed.retrieveImage(colorSL, VIEW::LEFT);
		color = getMat((void*)colorSL.getPtr<sl::uchar1>(), w, h);

		zed.retrieveImage(rightViewSL, VIEW::RIGHT);
		rightView = getMat((void*)rightViewSL.getPtr<sl::uchar1>(), w, h);

		zed.retrieveMeasure(pcMatSL, MEASURE::XYZBGRA); // XYZ has an extra float!

		pointCloud = cv::Mat(captureHeight, captureWidth, CV_32FC4, pcMatSL.getPtr<sl::uchar1>());
		cv::patchNaNs(pointCloud);
		if (captureWidth != w) resize(pointCloud, pointCloud, Size(w, h), 0, 0, INTER_NEAREST);
		cvtColor(pointCloud, pointCloud, ColorConversionCodes::COLOR_BGRA2BGR);

		zed.getPosition(zed_pose, REFERENCE_FRAME::WORLD);
		RotationMatrix = zed_pose.getRotationMatrix();
		RotationVector = zed_pose.getRotationVector();
		IMU_Translation = zed_pose.getTranslation();

		zed.getSensorsData(sensordata, TIME_REFERENCE::CURRENT);
		imuTimeStamp = static_cast<double>(zed_pose.timestamp.getMilliseconds());
	}
};


extern "C" __declspec(dllexport) int* Zed2Open(int w, int h) { StereoLabsZed2* cPtr = new StereoLabsZed2(w, h); return (int*)cPtr; }
extern "C" __declspec(dllexport) void Zed2Close(StereoLabsZed2 * cPtr) { cPtr->zed.close(); }
extern "C" __declspec(dllexport) int* Zed2Acceleration(StereoLabsZed2 * cPtr) { return (int*)&cPtr->sensordata.imu.linear_acceleration; }
extern "C" __declspec(dllexport) int* Zed2AngularVelocity(StereoLabsZed2 * cPtr) { return (int*)&cPtr->sensordata.imu.angular_velocity; }
extern "C" __declspec(dllexport) int Zed2SerialNumber(StereoLabsZed2 * cPtr) { return cPtr->serialNumber; }
extern "C" __declspec(dllexport) void Zed2WaitForFrame(StereoLabsZed2 * cPtr) { cPtr->waitForFrame(); }
extern "C" __declspec(dllexport) double Zed2IMU_TimeStamp(StereoLabsZed2 * cPtr) { return cPtr->imuTimeStamp; }
extern "C" __declspec(dllexport) void Zed2GetData(StereoLabsZed2 * cPtr, int w, int h) { cPtr->GetData(w, h); }
extern "C" __declspec(dllexport) int* Zed2Color(StereoLabsZed2 * cPtr)
{
	return (int*)cPtr->color.data;
}
extern "C" __declspec(dllexport) int* Zed2PointCloud(StereoLabsZed2 * cPtr)
{
	return (int*)cPtr->pointCloud.data;
}
extern "C" __declspec(dllexport) int* Zed2RightView(StereoLabsZed2 * cPtr)
{
	return (int*)cPtr->rightView.data;
}
extern "C" __declspec(dllexport) int* Zed2Intrinsics(StereoLabsZed2 * cPtr) { return (int*)&cPtr->cameraData; }
#else
extern "C" __declspec(dllexport) int placeholder() { return 0; }
#endif
