#include <cstdlib>
#include <cstdio>
#include <string>
#include <iostream>
#include <iomanip>
#include <cstring>
#include <thread>
#include <mutex>

#include <algorithm>

#include "../CameraDefines.hpp"
#ifdef STEREOLAB_INSTALLED

#pragma comment(lib, "sl_zed64.lib")
#pragma comment(lib, "cuda.lib") 
#pragma comment(lib, "cudart.lib") 

#include <sl/Camera.hpp>

using namespace sl;
using namespace std;

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
	sl::float3 acc3f;
	Camera zed;
	sl::Mat colorSL, rightViewSL, pcMatSL;
	int *color, *rightView, *pointCloud;
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

		//init_params.camera_resolution = sl::RESOLUTION::AUTO;
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

	void GetData(int w, int h)
	{
		zed.retrieveImage(colorSL, VIEW::LEFT);
		color = (int*)colorSL.getPtr<sl::uchar1>();

		zed.retrieveImage(rightViewSL, VIEW::RIGHT);
		rightView = (int *)rightViewSL.getPtr<sl::uchar1>();

		zed.retrieveMeasure(pcMatSL, MEASURE::XYZ); // XYZ has an extra float!

		pointCloud = (int *)pcMatSL.getPtr<sl::uchar1>();

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
extern "C" __declspec(dllexport) int* Zed2Acceleration(StereoLabsZed2 * cPtr) 
{ 
	return (int*)&cPtr->sensordata.imu.linear_acceleration;
}
extern "C" __declspec(dllexport) int* Zed2AngularVelocity(StereoLabsZed2 * cPtr) 
{ 
	return (int*)&cPtr->sensordata.imu.angular_velocity; 
}

extern "C" __declspec(dllexport) int Zed2SerialNumber(StereoLabsZed2 * cPtr) { return cPtr->serialNumber; }
extern "C" __declspec(dllexport) void Zed2WaitForFrame(StereoLabsZed2 * cPtr) { cPtr->waitForFrame(); }
extern "C" __declspec(dllexport) double Zed2IMU_TimeStamp(StereoLabsZed2 * cPtr) { return cPtr->imuTimeStamp; }
extern "C" __declspec(dllexport) void Zed2GetData(StereoLabsZed2 * cPtr, int w, int h) { cPtr->GetData(w, h); }
extern "C" __declspec(dllexport) int* Zed2Color(StereoLabsZed2 * cPtr)
{
	return (int*)cPtr->color;
}
extern "C" __declspec(dllexport) int* Zed2PointCloud(StereoLabsZed2 * cPtr)
{
	return (int*)cPtr->pointCloud;
}
extern "C" __declspec(dllexport) int* Zed2RightView(StereoLabsZed2 * cPtr)
{
	return (int*)cPtr->rightView;
}
extern "C" __declspec(dllexport) int* Zed2Intrinsics(StereoLabsZed2 * cPtr) { return (int*)&cPtr->cameraData; }
#else
extern "C" __declspec(dllexport) int placeholder() { return 0; }
#endif
