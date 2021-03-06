#include "../CameraDefines.hpp"
#ifdef STEREOLAB_INSTALLED
#pragma comment(lib, "sl_zed64.lib")
#pragma comment(lib, "cuda.lib") 
#pragma comment(lib, "cudart.lib") 

#include <iostream>
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
#include <opencv2/imgproc.hpp> 
#include <opencv2/imgcodecs.hpp>
#include <opencv2/highgui.hpp>
#include <sl/Camera.hpp>
#include "../../CPP_Classes/DepthColorizer.hpp"
#ifdef _DEBUG
#include "../Data/PragmaLibsD.h"
#else
#include "../Data/PragmaLibs.h"
#endif

using namespace sl;
using namespace std;
using namespace cv;

class StereoLabsZed2
{
public:
	int serialNumber = 0;
	CameraParameters intrinsicsLeft;
	CameraParameters intrinsicsRight;
	Translation extrinsicsTranslation;
	Rotation extrinsicsRotationMatrix;
	float acceleration[3] = { 0, 0, 0 };
	sl::float3 RotationVector;
	Rotation RotationMatrix;
	Translation IMU_Translation;
	SensorsData sensordata;
	Orientation orientation;
	float imuTemperature = 0;
	double imuTimeStamp = 0;
	Pose zed_pose;
	sl::Camera zed;
	cv::Mat color, leftView, rightView, pointCloud, depth16;
	Depth_ColorizerZed2* cPtr;
private:
	sl::InitParameters init_params;
	int width, height;
	float imuData = 0;
	long long pixelCount;
public:
	~StereoLabsZed2()
	{
		zed.close();
	}
	StereoLabsZed2(int w, int h, int fps)
	{
		width = w;
		height = h;
		pixelCount = (long long)width * height;

		init_params.sensors_required = true;
		init_params.depth_mode = DEPTH_MODE::ULTRA;
		init_params.coordinate_system = COORDINATE_SYSTEM::RIGHT_HANDED_Y_UP; // OpenGL's coordinate system is right_handed
		init_params.coordinate_units = UNIT::METER;

		init_params.camera_resolution = sl::RESOLUTION::HD720;
		init_params.camera_fps = 60;

		zed.open(init_params);

		CameraInformation camera_info = zed.getCameraInformation();
		serialNumber = camera_info.serial_number;
		printf("serial number = %d", serialNumber);
		extrinsicsTranslation = camera_info.camera_configuration.calibration_parameters.stereo_transform.getTranslation();
		extrinsicsRotationMatrix = camera_info.camera_configuration.calibration_parameters.stereo_transform.getRotationMatrix();
		intrinsicsLeft = camera_info.calibration_parameters.left_cam;
		intrinsicsRight = camera_info.calibration_parameters.right_cam;

		PositionalTrackingParameters positional_tracking_param;
		positional_tracking_param.enable_area_memory = true;
		auto returned_state = zed.enablePositionalTracking(positional_tracking_param);

		pointCloud = cv::Mat(height, width, CV_32FC3);
		depth16 = cv::Mat(height, width, CV_16U);
		cPtr = new Depth_ColorizerZed2();
	}

	void waitForFrame()
	{
		zed.grab();
	}

	void GetData()
	{
		sl::Mat colorSL, depthSL32f, leftViewSL, rightViewSL, pcMatSL;

		zed.retrieveImage(colorSL, VIEW::LEFT, MEM::CPU);
		cv::Mat tmp = cv::Mat(height, width, CV_8UC4, (void*)colorSL.getPtr<sl::uchar1>(sl::MEM::CPU));
		cv::cvtColor(tmp, color, cv::ColorConversionCodes::COLOR_BGRA2BGR);

		zed.retrieveMeasure(depthSL32f, MEASURE::DEPTH, MEM::CPU);
		cPtr->depth32f = cv::Mat(height, width, CV_32FC1, (void*)depthSL32f.getPtr<sl::uchar1>(sl::MEM::CPU)) * 1000;
		cPtr->depth32f.convertTo(depth16, CV_16U);
		cPtr->output = cv::Mat(height, width, CV_8UC3);
		cPtr->Run();

		zed.retrieveImage(leftViewSL, VIEW::LEFT_GRAY, MEM::CPU);
		leftView = cv::Mat(height, width, CV_8U, (void*)leftViewSL.getPtr<sl::uchar1>(sl::MEM::CPU)).clone();

		zed.retrieveImage(rightViewSL, VIEW::RIGHT_GRAY, MEM::CPU);
		rightView = cv::Mat(height, width, CV_8U, (void*)rightViewSL.getPtr<sl::uchar1>(sl::MEM::CPU)).clone();

		zed.retrieveMeasure(pcMatSL, MEASURE::XYZ, MEM::CPU); // XYZ has an extra byte!
		float* pc = (float*)pcMatSL.getPtr<sl::uchar1>(sl::MEM::CPU);
		float* pcXYZ = (float*)pointCloud.data;
		// 4 bytes per pixel to 3 bytes per pixel
		for (int i = 0; i < pixelCount * 4; i += 4)
		{
			if (isnan(pc[i]))
			{
				pcXYZ[0] = 0;
				pcXYZ[1] = 0;
				pcXYZ[2] = 0;
			}
			else {
				pcXYZ[0] = pc[i];
				pcXYZ[1] = -pc[i + 1];
				pcXYZ[2] = -pc[i + 2];
			}
			pcXYZ += 3;
		}

		zed.getPosition(zed_pose, REFERENCE_FRAME::WORLD);
		RotationMatrix = zed_pose.getRotationMatrix();
		RotationVector = zed_pose.getRotationVector();
		IMU_Translation = zed_pose.getTranslation();

		zed.getSensorsData(sensordata, TIME_REFERENCE::CURRENT);
		imuTimeStamp = static_cast<double>(zed_pose.timestamp.getMilliseconds());
	}
};

extern "C" __declspec(dllexport) int* Zed2Open(int w, int h, int fps)
{
	StereoLabsZed2* Zed2 = new StereoLabsZed2(w, h, fps);
	return (int*)Zed2;
}
extern "C" __declspec(dllexport) void Zed2Close(StereoLabsZed2 * Zed2)
{
	delete Zed2;
}
extern "C" __declspec(dllexport) int* Zed2intrinsicsLeft(StereoLabsZed2* Zed2)
{
	return (int*)&Zed2->intrinsicsLeft;
}
extern "C" __declspec(dllexport) int* Zed2intrinsicsRight(StereoLabsZed2* Zed2)
{
	return (int*)&Zed2->intrinsicsRight;
}
extern "C" __declspec(dllexport) int* Zed2Acceleration(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->sensordata.imu.linear_acceleration;
}
extern "C" __declspec(dllexport) int* Zed2Translation(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->IMU_Translation;
}
extern "C" __declspec(dllexport) int* Zed2RotationMatrix(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->RotationMatrix;
}
extern "C" __declspec(dllexport) int* Zed2RotationVector(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->RotationVector;
}
extern "C" __declspec(dllexport) int Zed2Confidence(StereoLabsZed2 * Zed2)
{
	return Zed2->zed_pose.pose_confidence;
}
extern "C" __declspec(dllexport) int* Zed2AngularVelocity(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->sensordata.imu.angular_velocity;
}
extern "C" __declspec(dllexport) float Zed2IMU_Barometer(StereoLabsZed2 * Zed2)
{
	return Zed2->sensordata.barometer.pressure;
}
extern "C" __declspec(dllexport) int* Zed2Orientation(StereoLabsZed2 * Zed2)
{
	Zed2->orientation = Zed2->sensordata.imu.pose.getOrientation();
	return (int*)&Zed2->orientation;
}
extern "C" __declspec(dllexport) int Zed2SerialNumber(StereoLabsZed2 * Zed2)
{
	return Zed2->serialNumber;
}
extern "C" __declspec(dllexport) void Zed2WaitForFrame(StereoLabsZed2 * Zed2)
{
	Zed2->waitForFrame();
}
extern "C" __declspec(dllexport) int* Zed2IMU_Magnetometer(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->sensordata.magnetometer.magnetic_field_uncalibrated; // calibrated values look incorrect.
}
extern "C" __declspec(dllexport) double Zed2IMU_TimeStamp(StereoLabsZed2 * Zed2)
{
	// printf("ts (uint64) =%ju (0x%jx)\n", Zed2->zed_pose.timestamp.getMilliseconds(), Zed2->zed_pose.timestamp.getMilliseconds());
	return Zed2->imuTimeStamp;
}
extern "C" __declspec(dllexport)float Zed2IMU_Temperature(StereoLabsZed2 * Zed2)
{
	Zed2->sensordata.temperature.get(sl::SensorsData::TemperatureData::SENSOR_LOCATION::IMU, Zed2->imuTemperature);
	return Zed2->imuTemperature;
}
extern "C" __declspec(dllexport) int* Zed2GetPoseData(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->zed_pose.pose_data;
}
extern "C" __declspec(dllexport) int* Zed2ExtrinsicsRotationMatrix(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->extrinsicsRotationMatrix;
}
extern "C" __declspec(dllexport) int* Zed2ExtrinsicsTranslation(StereoLabsZed2 * Zed2)
{
	return (int*)&Zed2->extrinsicsTranslation;
}




extern "C" __declspec(dllexport) void Zed2GetData(StereoLabsZed2 * Zed2)
{
	Zed2->GetData();
}
extern "C" __declspec(dllexport)
int* Zed2Color(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->color.data;
}

extern "C" __declspec(dllexport)
int* Zed2RGBDepth(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->cPtr->output.data;
}

extern "C" __declspec(dllexport)
int* Zed2Depth16(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->depth16.data;
}

extern "C" __declspec(dllexport)
int* Zed2PointCloud(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->pointCloud.data;
}

extern "C" __declspec(dllexport)
int* Zed2LeftView(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->leftView.data;
}

extern "C" __declspec(dllexport)
int* Zed2RightView(StereoLabsZed2 * Zed2)
{
	return (int*)Zed2->rightView.data;
}

#endif


