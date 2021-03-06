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
#include "../../CPP_Classes/DepthColorizer.hpp"
#ifdef _DEBUG
#include "../Data/PragmaLibsD.h"
#else
#include "../Data/PragmaLibs.h"
#endif

using namespace std;
using namespace cv;
using namespace k4aviewer;

class KinectCamera
{
public:
	char *serial_number = NULL;
	uint32_t deviceCount = 0;
	k4a_imu_sample_t imu_sample;
	k4a_device_t device = NULL;
	k4a_calibration_t calibration;
	k4a_transformation_t transformation;
	uint16_t* depthBuffer;
	uint8_t* colorBuffer;
	k4a_image_t point_cloud_image;
	int pointCloudBuffSize = 0;
	k4a_image_t colorImage = NULL;
	Depth_Colorizer16* dcptr = NULL;
	k4a_device_configuration_t config = K4A_DEVICE_CONFIG_INIT_DISABLE_ALL;
	char outMsg[10000];
	Mat color;
	int colorRows, colorCols, colorBuffSize;

private:
	k4a_capture_t capture = NULL;
	const int32_t TIMEOUT_IN_MS = 1000;
	size_t infraredSize = 0;
	k4a_image_t point_cloud = NULL;
	k4a_image_t depthInColor;
public:
	~KinectCamera()
	{
	}
	KinectCamera() {}
	KinectCamera(int width, int height)
	{
		deviceCount = k4a_device_get_installed_count();
		if (deviceCount > 0)
		{
			colorCols = width;
			colorRows = height;
			colorBuffSize = width * height;
			device = NULL;
			if (K4A_RESULT_SUCCEEDED != k4a_device_open(K4A_DEVICE_DEFAULT, &device)) { deviceCount = 0; return; }

			KinectSerialNumber();

			config.color_format = K4A_IMAGE_FORMAT_COLOR_BGRA32;
			config.color_resolution = K4A_COLOR_RESOLUTION_720P;
			config.depth_mode = K4A_DEPTH_MODE_WFOV_2X2BINNED;
			config.camera_fps = K4A_FRAMES_PER_SECOND_30;

			k4a_device_get_calibration(device, config.depth_mode, config.color_resolution, &calibration);

			pointCloudBuffSize = colorBuffSize * 3 * (int) sizeof(int16_t);

			k4a_image_create(K4A_IMAGE_FORMAT_DEPTH16, colorCols, colorRows, colorCols * (int)sizeof(int16_t), &depthInColor);
			k4a_image_create(K4A_IMAGE_FORMAT_CUSTOM, colorCols, colorRows, colorCols * 3 * (int)sizeof(int16_t), &point_cloud_image); // int16_t - not a mistake.
			k4a_image_create(K4A_IMAGE_FORMAT_COLOR_BGRA32, colorCols, colorRows, colorCols * 4 * (int)sizeof(uint8_t), &colorImage);

			k4a_device_start_cameras(device, &config);

			transformation = k4a_transformation_create(&calibration);

			k4a_device_start_imu(device);
			dcptr = new Depth_Colorizer16();
		}
	}

	void KinectSerialNumber()
	{
		size_t length = 0;
		if (K4A_BUFFER_RESULT_TOO_SMALL != k4a_device_get_serialnum(device, NULL, &length))
		{
			printf("%d: Failed to get serial number length\n", 0);
			k4a_device_close(device);
		}

		serial_number = (char *)malloc(length);
		k4a_device_get_serialnum(device, serial_number, &length);
	}

	int *waitForFrame()
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
			depthBuffer = (uint16_t *) k4a_image_get_buffer(depthInColor);
			dcptr->depth16 = Mat(colorRows, colorCols, CV_16U, depthBuffer);
			dcptr->Run();
		}

		k4a_transformation_depth_image_to_point_cloud(transformation, depthInColor, K4A_CALIBRATION_TYPE_COLOR, point_cloud_image);
		k4a_device_get_imu_sample(device, &imu_sample, 2000);

		if (depthImage) k4a_image_release(depthImage);

		k4a_capture_release(capture);

		auto now = std::chrono::system_clock::now().time_since_epoch();
		double now_ms = static_cast<double>(std::chrono::duration_cast<std::chrono::milliseconds>(now).count());
		static double hostStartTime = now_ms;
		double timeStamp = static_cast<double>(imu_sample.acc_timestamp_usec) / 1000;
		
		return (int *)&imu_sample;
	}
};

KinectCamera* kcPtr;

extern "C" __declspec(dllexport)
int KinectDeviceCount(KinectCamera *kc)
{
	return kc->deviceCount;
}

extern "C" __declspec(dllexport)
int *KinectOpen(int width, int height)
{
	KinectCamera *kc = new KinectCamera(width, height);
	if (kc->deviceCount == 0) return 0;
	kcPtr = kc;
	return (int *)kc;
}

extern "C" __declspec(dllexport)
int *KinectDeviceName(KinectCamera *kc)
{
	return (int *)kc->serial_number;
}

extern "C" __declspec(dllexport)
int *KinectExtrinsics(KinectCamera *kc)
{
	return (int *)&kc->calibration.extrinsics[K4A_CALIBRATION_TYPE_DEPTH][K4A_CALIBRATION_TYPE_COLOR];
}

extern "C" __declspec(dllexport)
int *KinectintrinsicsLeft(KinectCamera *kc)
{
	return (int *)&kc->calibration.color_camera_calibration.intrinsics.parameters.v;
}

extern "C" __declspec(dllexport)
int* KinectPointCloud(KinectCamera * kc)
{
	return (int*)k4a_image_get_buffer(kc->point_cloud_image);
}

extern "C" __declspec(dllexport)
int* KinectRawDepth(KinectCamera * kc)
{
	return (int*)kc->dcptr->depth16.data;
}

extern "C" __declspec(dllexport)
int* KinectRGBA(KinectCamera * kc)
{
	return (int*)kc->colorBuffer;
}

extern "C" __declspec(dllexport)
int* KinectLeftView(KinectCamera * kc)
{
	return (int*)kc->depthBuffer;
}

extern "C" __declspec(dllexport)
int* KinectRGBdepth(KinectCamera * kc)
{
	return (int*)kc->dcptr->output.data;
}

extern "C" __declspec(dllexport)
int* KinectWaitFrame(KinectCamera* kc)
{
	return kc->waitForFrame();
}

extern "C" __declspec(dllexport)
void KinectClose(KinectCamera *kc)
{
	if (kc->point_cloud_image) k4a_image_release(kc->point_cloud_image);
	if (kc->colorImage) k4a_image_release(kc->colorImage);

	k4a_device_stop_imu(kc->device);
	k4a_device_stop_cameras(kc->device);
	k4a_transformation_destroy(kc->transformation);
	free(kc->serial_number);
	if (kc == 0) return;
	k4a_device_close(kc->device);
	delete kc;
}

extern "C" __declspec(dllexport)
char* KinectRodrigues()
{
	k4a_calibration_t calibration;
	if (K4A_RESULT_SUCCEEDED != k4a_device_get_calibration(kcPtr->device, kcPtr->config.depth_mode, kcPtr->config.color_resolution, &calibration))
	{
		printf("Failed to get calibration\n");
		return 0;
	}

	vector<k4a_float3_t> points_3d = { { { 0.f, 0.f, 1000.f } },          // color camera center
									   { { -1000.f, -1000.f, 1000.f } },  // color camera top left
									   { { 1000.f, -1000.f, 1000.f } },   // color camera top right
									   { { 1000.f, 1000.f, 1000.f } },    // color camera bottom right
									   { { -1000.f, 1000.f, 1000.f } } }; // color camera bottom left

	// k4a project function
	vector<k4a_float2_t> k4a_points_2d(points_3d.size());
	for (size_t i = 0; i < points_3d.size(); i++)
	{
		int valid = 0;
		k4a_calibration_3d_to_2d(&calibration, &points_3d[i], K4A_CALIBRATION_TYPE_COLOR, K4A_CALIBRATION_TYPE_DEPTH, &k4a_points_2d[i], &valid);
	}

	// converting the calibration data to OpenCV format
	// extrinsic transformation from color to depth camera
	Mat se3 = Mat(3, 3, CV_32FC1, calibration.extrinsics[K4A_CALIBRATION_TYPE_COLOR][K4A_CALIBRATION_TYPE_DEPTH].rotation);
	Mat r_vec = Mat(3, 1, CV_32FC1);
	cv::Rodrigues(se3, r_vec);
	Mat t_vec = Mat(3, 1, CV_32F, calibration.extrinsics[K4A_CALIBRATION_TYPE_COLOR][K4A_CALIBRATION_TYPE_DEPTH].translation);

	// intrinsic parameters of the depth camera
	k4a_calibration_intrinsic_parameters_t* intrinsics = &calibration.depth_camera_calibration.intrinsics.parameters;
	vector<float> _camera_matrix = { intrinsics->param.fx, 0.f, intrinsics->param.cx, 0.f, intrinsics->param.fy, intrinsics->param.cy,
									 0.f, 0.f, 1.f };
	Mat camera_matrix = Mat(3, 3, CV_32F, &_camera_matrix[0]);
	vector<float> _dist_coeffs = { intrinsics->param.k1, intrinsics->param.k2, intrinsics->param.p1,
								   intrinsics->param.p2, intrinsics->param.k3, intrinsics->param.k4,
								   intrinsics->param.k5, intrinsics->param.k6 };
	Mat dist_coeffs = Mat(8, 1, CV_32F, &_dist_coeffs[0]);

	// OpenCV projectPoints function
	vector<Point2f> cv_points_2d(points_3d.size());
	cv::projectPoints(*reinterpret_cast<vector<Point3f>*>(&points_3d), r_vec, t_vec, camera_matrix, dist_coeffs, cv_points_2d);

	for (size_t i = 0; i < points_3d.size(); i++)
	{
		sprintf_s(kcPtr->outMsg, "3d point:\t\t\t(%.5f, %.5f, %.5f)\n OpenCV projectPoints:\t\t(%.5f, %.5f)\n k4a_calibration_3d_to_2d:\t(%.5f, %.5f)\n\n",
			points_3d[i].v[0], points_3d[i].v[1], points_3d[i].v[2], cv_points_2d[i].x, cv_points_2d[i].y, k4a_points_2d[i].v[0], k4a_points_2d[i].v[1]);
	}

	return kcPtr->outMsg;
}

extern "C" __declspec(dllexport)
void KinectRodriguesOnly(float *extrinsics, float *vectorOut)
{
	Mat extIn = Mat(3, 3, CV_32FC1, extrinsics);
	Mat extOut = Mat(3, 1, CV_32FC1, vectorOut);
	cv::Rodrigues(extIn, extOut);
}

