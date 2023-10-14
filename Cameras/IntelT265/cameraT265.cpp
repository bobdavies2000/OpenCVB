#include <iostream>
#include <iomanip>
#include <cstring>
#include <string>
#include <thread>
#include <librealsense2/rs.hpp>
#include <mutex>
#include <cstdlib>
#include <cstdio>
#include <algorithm>
#include "example.hpp"
#include <opencv2/core.hpp>
#include <opencv2/calib3d.hpp>
#include "opencv2/imgproc.hpp"
#include "opencv2/imgcodecs.hpp"
#include "opencv2/highgui.hpp"
#include "../../CPP_Classes/DepthColorizer.hpp"

using namespace std;
using namespace cv;




class t265sgm
{
private:
public:
	Mat depth16s;
	Ptr<StereoSGBM> stereo;
	t265sgm(int minDisp, int windowSize, int numDisp)
	{
		stereo = cv::StereoSGBM::create(minDisp, numDisp, 16, 8 * 3 * windowSize * windowSize, 32 * 3 * windowSize * windowSize, 1, 0, 10, 100, 32);
	}
	Mat Run(Mat leftimg, Mat rightimg, int maxDisp)
	{
		stereo->compute(leftimg, rightimg, depth16s);
		return depth16s;
	}
};

class t265Camera
{
public:
	Mat leftViewMap1, leftViewMap2, rightViewMap1, rightViewMap2, disparity;
	Mat color, depth16, leftViewRaw, rightViewRaw, depth32f, depth16s;
	int rawWidth, rawHeight;
	rs2_intrinsics intrinsicsLeft, intrinsicsRight;
	rs2_extrinsics extrinsics;
	int stereo_width_px, stereo_height_px;
	rs2_pose pose_data;
	double IMU_TimeStamp;
	rs2::pipeline pipeline;
	float3 *gyro_data;
	float3 *accel_data;
	Depth_Colorizer* cPtr;

private:
	int width, height;
	rs2::config cfg;
	std::mutex data_mutex;
	rs2::pipeline_profile pipeline_profile;
	int numDisp;
	int minDisp = 0;
	int maxDisp;
	int dispOffset = 112;
	int windowSize = 5;
	Mat lm1, lm2, rm1, rm2;
	
	double stereo_fov_rad, stereo_focal_px, stereo_cx, stereo_cy;
	cv::Size stereo_size;
	t265sgm* sgm;

public:
	~t265Camera(){}

	t265Camera(int w, int h)
	{
		width = w;
		height = h;
		cPtr = new Depth_Colorizer();

		cfg.enable_stream(RS2_STREAM_POSE, RS2_FORMAT_6DOF);
		cfg.enable_stream(RS2_STREAM_GYRO);
		cfg.enable_stream(RS2_STREAM_ACCEL);
		cfg.enable_stream(RS2_STREAM_FISHEYE, 1, RS2_FORMAT_Y8);
		cfg.enable_stream(RS2_STREAM_FISHEYE, 2, RS2_FORMAT_Y8);

		pipeline_profile = pipeline.start(cfg);

		numDisp = dispOffset - minDisp;
		maxDisp = minDisp + numDisp;
		
		auto leftStream = pipeline_profile.get_stream(RS2_STREAM_FISHEYE, 1).as<rs2::video_stream_profile>();
		auto rightStream = pipeline_profile.get_stream(RS2_STREAM_FISHEYE, 2).as<rs2::video_stream_profile>();

		extrinsics = leftStream.get_extrinsics_to(rightStream);

		intrinsicsLeft = leftStream.as<rs2::video_stream_profile>().get_intrinsics();
		intrinsicsRight = rightStream.as<rs2::video_stream_profile>().get_intrinsics();

		rawWidth = intrinsicsLeft.width;
		rawHeight = intrinsicsLeft.height;

		double kLeft[9] = { intrinsicsLeft.fx, 0, intrinsicsLeft.ppx, 0, intrinsicsLeft.fy, intrinsicsLeft.ppy, 0, 0, 1 };
		double dLeft[4] = { intrinsicsLeft.coeffs[0], intrinsicsLeft.coeffs[1], intrinsicsLeft.coeffs[2], intrinsicsLeft.coeffs[3] };

		double kRight[9] = { intrinsicsRight.fx, 0, intrinsicsRight.ppx, 0, intrinsicsRight.fy, intrinsicsRight.ppy, 0, 0, 1 };
		double dRight[4] = { intrinsicsRight.coeffs[0], intrinsicsRight.coeffs[1], intrinsicsRight.coeffs[2], intrinsicsRight.coeffs[3] };

		// We need To determine what focal length our undistorted images should have
		// In order To Set up the camera matrices For initUndistortRectifyMap.  We
		// could use stereoRectify, but here we show how To derive these projection
		// matrices from the calibration And a desired height And field Of view      
		// We calculate the undistorted focal length :
		//
		//         h
		// -----------------
		//  \      |      /
		//    \    | f  /
		//     \   |   /
		//      \ fov /
		//        \|/
		stereo_fov_rad = 90.0 * CV_PI / 180.0;  // 90 degree desired fov
		stereo_height_px = 300; // 300x300 pixel stereo output
		stereo_focal_px = stereo_height_px / 2 / tan(stereo_fov_rad / 2);

		// We Set the left rotation To identity And the right rotation
		// the rotation between the cameras
		double rLeft[9] = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
		float* r = (float *) &extrinsics.rotation;
		double rRight[9] = { r[0],r[1],r[2],r[3],r[4],r[5],r[6],r[7],r[8] };
		// The stereo algorithm needs max_disp extra pixels In order To produce valid
		// disparity On the desired output region. This changes the width, but the
		// center Of projection should be On the center Of the cropped image
		stereo_width_px = stereo_height_px + maxDisp;
		stereo_size = cv::Size(stereo_width_px, stereo_height_px);
		stereo_cx = double((stereo_height_px - 1.0) / 2.0 + maxDisp);
		stereo_cy = (stereo_height_px - 1.0) / 2.0;

		// Construct the left And right projection matrices, the only difference Is
		// that the right projection matrix should have a shift along the x axis Of
		// baseline*focal_length
		double pLeft[12] = { stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0 };
		double pRight[12] = { stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0 };
		pRight[3] = extrinsics.translation[0] * stereo_focal_px;

		Mat kMatleft = Mat(3, 3, CV_64F, kLeft);
		Mat dMatleft = Mat(1, 4, CV_64F, dLeft);
		Mat rMatleft = Mat(3, 3, CV_64F, rLeft);
		Mat pMatleft = Mat(3, 4, CV_64F, pLeft);
		cv::fisheye::initUndistortRectifyMap(kMatleft, dMatleft, rMatleft, pMatleft, stereo_size, CV_32FC1, lm1, lm2);
		cv::fisheye::initUndistortRectifyMap(kMatleft, dMatleft, rMatleft, pMatleft, Size(rawWidth, rawHeight), CV_32FC1, leftViewMap1, leftViewMap2);

		Mat kMatRight = Mat(3, 3, CV_64F, kRight);
		Mat dMatRight = Mat(1, 4, CV_64F, dRight);
		Mat rMatRight = Mat(3, 3, CV_64F, rRight);
		Mat pMatRight = Mat(3, 4, CV_64F, pRight);

		cv::fisheye::initUndistortRectifyMap(kMatRight, dMatRight, rMatRight, pMatRight, stereo_size, CV_32FC1, rm1, rm2);
		cv::fisheye::initUndistortRectifyMap(kMatRight, dMatRight, rMatRight, pMatRight, Size(rawWidth, rawHeight), CV_32FC1, rightViewMap1, rightViewMap2);

		sgm = new t265sgm(minDisp, windowSize, numDisp);
	}

	void waitForFrame()
	{
		auto frameset = pipeline.wait_for_frames(1000);
		auto f = frameset.first_or_default(RS2_STREAM_POSE);
		IMU_TimeStamp = f.get_timestamp();
		pose_data = f.as<rs2::pose_frame>().get_pose_data(); // Cast the frame to pose_frame and get its data

		// Cast the frame that arrived to motion frame
		auto accel = frameset.first_or_default(RS2_STREAM_ACCEL);
		accel_data = (float3 *) accel.get_data();
		auto gyro = frameset.first_or_default(RS2_STREAM_GYRO);
		gyro_data = (float3*)gyro.get_data();

		Mat tmpColor, remapLeft, remapRight;

		auto fs = frameset.as<rs2::frameset>();
		rs2::frame leftImage = fs.get_fisheye_frame(1);
		leftViewRaw = Mat(rawHeight, rawWidth, CV_8U, (void*)leftImage.get_data());
		remap(leftViewRaw, tmpColor, leftViewMap1, leftViewMap2, INTER_LINEAR);
		resize(tmpColor, tmpColor, Size(width, height));
		cvtColor(tmpColor, color, COLOR_GRAY2BGR);
		cv::remap(leftViewRaw, remapLeft, lm1, lm2, INTER_LINEAR);

		rs2::frame rightImage = fs.get_fisheye_frame(2);
		rightViewRaw = Mat(rawHeight, rawWidth, CV_8U, (void*)rightImage.get_data());
		
		//remap(rightViewRaw, tmpColor, rightViewMap1, rightViewMap2, INTER_LINEAR);
		//resize(tmpColor, tmpColor, Size(width, height));

		cv::remap(rightViewRaw, remapRight, rm1, rm2, INTER_LINEAR);

		depth16s = sgm->Run(remapLeft, remapRight, maxDisp);

		Rect validRect = Rect(maxDisp, 0, depth16s.cols - maxDisp, depth16s.rows);
		depth16s = depth16s(validRect);
		Mat tmp = Mat(stereo_height_px, stereo_height_px, CV_32F, 20000); // this is just an estimate of an actual disparity to depth conversion.
		disparity = Mat(tmp.rows, tmp.cols, CV_32F);
		depth16s.convertTo(disparity, CV_32F, 1.0f / 16.0f);
		threshold(disparity, disparity, 0, 0, cv::THRESH_TOZERO); // anything below zero is now zero...

		depth32f = Mat(tmp.rows, tmp.cols, CV_32F);
		divide(tmp, disparity, depth32f);  // this is a hack to dummy up an approximate depth32f and point cloud.

		float* pc = (float*)depth32f.data;
		for (int i = 0; i < tmp.rows * tmp.cols; ++i)
			if (isnan(pc[i]) || isinf(pc[i])) pc[i] = 0;

		cPtr->depth32f = depth32f;
		cPtr->Run(); 
	}
};

extern "C" __declspec(dllexport)
int *T265Open(int w, int h)
{
	t265Camera *tp = new t265Camera(w, h);
	return (int *)tp;
}

extern "C" __declspec(dllexport)
int* T265intrinsicsLeft(t265Camera * tp)
{
	return (int*)&tp->intrinsicsLeft;
}

extern "C" __declspec(dllexport)
int* T265intrinsicsRight(t265Camera * tp)
{
	return (int*)&tp->intrinsicsRight;
}

extern "C" __declspec(dllexport)
int T265RawWidth(t265Camera * tp)
{
	return tp->rawWidth;
}

extern "C" __declspec(dllexport)
int T265RawHeight(t265Camera * tp)
{
	return tp->rawHeight;
}

extern "C" __declspec(dllexport)
int* T265Extrinsics(t265Camera * tp)
{
	return (int*)&tp->extrinsics;
}

extern "C" __declspec(dllexport)
int* T265LeftRaw(t265Camera* tp)	
{
	return (int *) tp->leftViewRaw.data;
}

extern "C" __declspec(dllexport)
int* T265RightRaw(t265Camera * tp)
{
	return (int*)tp->rightViewRaw.data;
}

extern "C" __declspec(dllexport)
int* T265Color(t265Camera * tp)
{
	return (int*)tp->color.data;
}


extern "C" __declspec(dllexport)
int* T265RGBdepth(t265Camera * tp)
{
	return (int*)tp->cPtr->output.data;
}

extern "C" __declspec(dllexport)
int* T265PoseData(t265Camera * tp)
{
	return (int*)&tp->pose_data;
}

extern "C" __declspec(dllexport)
int* T265GyroData(t265Camera * tp)
{
	return (int*)tp->gyro_data;
}

extern "C" __declspec(dllexport)
int* T265AccelData(t265Camera * tp)
{
	return (int*)tp->accel_data;
}

extern "C" __declspec(dllexport)
int* T265Depth32f(t265Camera * tp)
{
	return (int*)tp->depth32f.data;
}

extern "C" __declspec(dllexport)
double T265IMUTimeStamp(t265Camera * tp)
{
	return tp->IMU_TimeStamp;
}

extern "C" __declspec(dllexport)
void T265WaitFrame(t265Camera * tp)
{
	tp->waitForFrame();
}
