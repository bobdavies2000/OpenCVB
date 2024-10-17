#include <winsock2.h>
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/calib3d.hpp"
#include <string>
#include "time.h"
#include <iomanip> 
#include <sstream>
#include <random>
#include <intrin.h>

#include <stdio.h>
#include <stdlib.h>
#include <k4a/k4a.h>
#include <k4a/k4a.hpp>
#include <k4apixel.h>
#include "PragmaLibs.h"

using namespace std;
using namespace  cv;
using namespace k4aviewer;

class K4Acamera
{
public:
	char* serial_number = NULL;
	uint32_t deviceCount = 0;
	k4a_imu_sample_t imu_sample;
	k4a_device_t device = NULL;
	k4a_calibration_t calibration;
	k4a_transformation_t transformation = NULL;
	int* depthBuffer = 0;
	uint8_t* colorBuffer = 0;;
	k4a_image_t point_cloud_image = NULL;
	k4a_image_t colorImage = NULL;
	k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
	char outMsg[10000];
	Mat colorMat, leftView;
	int width, height;

private:
	k4a_capture_t capture = NULL;
	const int32_t TIMEOUT_IN_MS = 1000;
	size_t infraredSize = 0;
	k4a_image_t point_cloud = NULL;
	k4a_image_t depthInColor;
public:
	~K4Acamera()
	{
	}
	K4Acamera() {}
	K4Acamera(int _width, int _height)
	{
		deviceCount = k4a_device_get_installed_count();
		if (deviceCount > 0)
		{
			width = _width;
			height = _height;
			device = NULL;
			if (K4A_RESULT_SUCCEEDED != k4a_device_open(K4A_DEVICE_DEFAULT, &device)) { deviceCount = 0; return; }

			K4ASerialNumber();

			config.color_format = K4A_IMAGE_FORMAT_COLOR_BGRA32;
			config.color_resolution = K4A_COLOR_RESOLUTION_720P;
			if (width == 1920) config.color_resolution = K4A_COLOR_RESOLUTION_1080P;
			config.depth_mode = K4A_DEPTH_MODE_WFOV_2X2BINNED;
			config.camera_fps = K4A_FRAMES_PER_SECOND_30;

			k4a_device_get_calibration(device, config.depth_mode, config.color_resolution, &calibration);

			k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, width, height, width * int(sizeof(int16_t)), &depthInColor);
			k4a_image_create(K4A_IMAGE_FORMAT_CUSTOM, width, height, width * 3 * int(sizeof(int16_t)), 
							 &point_cloud_image); // int16_t - not a mistake.

			k4a_image_create(K4A_IMAGE_FORMAT_COLOR_BGRA32, width, height, width * 4 * int(sizeof(uint8_t)), &colorImage);

			k4a_device_start_cameras(device, &config);

			transformation = k4a_transformation_create(&calibration);

			k4a_device_start_imu(device);
		}
	}

	void K4ASerialNumber()
	{
		size_t length = 0;
		if (K4A_BUFFER_RESULT_TOO_SMALL != k4a_device_get_serialnum(device, NULL, &length))
		{
			printf("%d: Failed to get serial number length\n", 0);
			k4a_device_close(device);
		}

		serial_number = (char*)malloc(length);
		k4a_device_get_serialnum(device, serial_number, &length);
	}

	int* waitForFrame()
	{
		bool waiting = true;
		while (waiting)
		{
			switch (k4a_device_get_capture(device, &capture, TIMEOUT_IN_MS))
			{
			case K4A_WAIT_RESULT_SUCCEEDED:
				waiting = false;
				break;
			case K4A_WAIT_RESULT_TIMEOUT:
				return 0;
			case K4A_WAIT_RESULT_FAILED:
				return 0;
			}
		}

		if (colorImage) k4a_image_release(colorImage);  // we want to keep the colorimage around between calls.
		colorImage = k4a_capture_get_color_image(capture);
		if (colorImage)
		{
			colorBuffer = k4a_image_get_buffer(colorImage);
			if (colorBuffer == NULL) return 0; // just have to use the last buffers.  Nothing new...
		}

		k4a_image_t depthImage = k4a_capture_get_depth_image(capture);
		if (depthImage != NULL and depthImage->_rsvd != 0)
		{
			k4a_transformation_depth_image_to_color_camera(transformation, depthImage, depthInColor);
			depthBuffer = (int*)k4a_image_get_buffer(depthInColor);
		}

		k4a_transformation_depth_image_to_point_cloud(transformation, depthInColor, K4A_CALIBRATION_TYPE_COLOR, point_cloud_image);
		
		for (int i = 0; i < 1000; i++)
		{
			auto test = k4a_device_get_imu_sample(device, &imu_sample, 0); // get the latest sample
			if (test == K4A_WAIT_RESULT_TIMEOUT) break;
		}

		if (depthImage) k4a_image_release(depthImage);

		k4a_capture_release(capture);

		//auto now = std::chrono::system_clock::now().time_since_epoch();
		//double now_ms = static_cast<double>(std::chrono::duration_cast<std::chrono::milliseconds>(now).count());
		//static double hostStartTime = now_ms;
		//double timeStamp = static_cast<double>(imu_sample.acc_timestamp_usec) / 1000;

		return (int*)&imu_sample;
	}
};

extern "C" __declspec(dllexport) int K4ADeviceCount(K4Acamera * cPtr) { return cPtr->deviceCount; }
extern "C" __declspec(dllexport) int* K4ADeviceName(K4Acamera * cPtr) { return (int*)cPtr->serial_number; }
extern "C" __declspec(dllexport) int* K4AIntrinsics(K4Acamera * cPtr) { return (int*)&cPtr->calibration.color_camera_calibration.intrinsics.parameters.v; }
extern "C" __declspec(dllexport) int* K4AColor(K4Acamera * cPtr) { return (int*)cPtr->colorMat.data; }
extern "C" __declspec(dllexport) int* K4APointCloud(K4Acamera * cPtr) { return (int*)k4a_image_get_buffer(cPtr->point_cloud_image); }
extern "C" __declspec(dllexport) int* K4ALeftView(K4Acamera * cPtr) { return (int *)cPtr->leftView.data; }
extern "C" __declspec(dllexport) 
int* K4AWaitFrame(K4Acamera* cPtr, int w, int h)
{ 
	int* imuFrame = cPtr->waitForFrame();
	if (cPtr->colorBuffer == 0) return 0;
	Mat tmp = Mat(cPtr->height, cPtr->width, CV_8UC4, (int*)cPtr->colorBuffer);
	resize(tmp, tmp, Size(w, h), INTER_NEAREST);
	cvtColor(tmp, cPtr->colorMat, COLOR_BGRA2BGR);

	tmp = Mat(cPtr->height, cPtr->width, CV_16U, cPtr->depthBuffer);
	resize(tmp, cPtr->leftView, Size(w, h), INTER_NEAREST);

	return imuFrame;
}

extern "C" __declspec(dllexport) int* K4AOpen(int width, int height)
{
	K4Acamera* cPtr = new K4Acamera(width, height);
	if (cPtr->deviceCount == 0) return 0;
	return (int*)cPtr;
}
extern "C" __declspec(dllexport) void K4AClose(K4Acamera * cPtr)
{
	if (cPtr->point_cloud_image) k4a_image_release(cPtr->point_cloud_image);
	if (cPtr->colorImage) k4a_image_release(cPtr->colorImage);

	k4a_device_stop_imu(cPtr->device);
	k4a_device_stop_cameras(cPtr->device);
	k4a_transformation_destroy(cPtr->transformation);
	if (cPtr == 0) return;
	free(cPtr->serial_number);
	k4a_device_close(cPtr->device);
	delete cPtr;
}