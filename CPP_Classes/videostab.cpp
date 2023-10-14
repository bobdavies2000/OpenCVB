#include "VideoStab.h"
#include <cmath>

#include <iostream>
#include <iomanip>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/video/tracking.hpp>

using namespace std;
using namespace  cv;

// Parameters for Kalman Filter
#define Q1 0.004
#define R1 0.5

VideoStab::VideoStab()
{
	smoothedMat.create(2, 3, CV_64F);
	k = 1;
	errScale = Mat(5, 1, CV_64F).setTo(1);
	qScale = Mat(5, 1, CV_64F).setTo(Q1);
	rScale = Mat(5, 1, CV_64F).setTo(R1);
	sumScale = Mat(5, 1, CV_64F).setTo(0);
	sScale = Mat(5, 1, CV_64F).setTo(0);
}

// https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
//The main stabilization function
Mat VideoStab::stabilize(Mat rgb)
{
	int vert_border = HORIZONTAL_BORDER_CROP * gray.rows / gray.cols;

	vector <Point2f> features1, features2;
	vector <Point2f> goodFeatures1, goodFeatures2;
	vector <uchar> status;
	vector <float> err;

	//Estimating the features in gray1 and gray2
	goodFeaturesToTrack(gray, features1, 200, 0.01, 30);
	calcOpticalFlowPyrLK(gray, lastFrame, features1, features2, status, err);

	for (size_t i = 0; i < status.size(); i++)
	{
		if (status[i] && err[i] < 2)
		{
			goodFeatures1.push_back(features1[i]);
			goodFeatures2.push_back(features2[i]);
		}
	}

	if (goodFeatures1.size() > 0 && goodFeatures2.size() > 0)
	{
		//All the parameters scale, angle, and translation are stored in affine
		affine = getAffineTransform(goodFeatures1.data(), goodFeatures2.data());

		double dx = affine.at<double>(0, 2);
		double dy = affine.at<double>(1, 2);
		double da = atan2(affine.at<double>(1, 0), affine.at<double>(0, 0));
		double ds_x = affine.at<double>(0, 0) / cos(da);
		double ds_y = affine.at<double>(1, 1) / cos(da);
		double saveDX = dx, saveDY = dy, saveDA = da;

		char original[1000];
		sprintf_s(original, "da = %f, dx = %f, dy = %f", da, dx, dy);

		double sx = ds_x;
		double sy = ds_y;
		
		double deltaArray[5] = { ds_x, ds_y, da, dx, dy }; 
		Mat delta(5, 1, CV_64F, deltaArray);
		cv::add(sumScale, delta, sumScale);

		//Don't calculate the predicted state of Kalman Filter on 1st iteration
		if (k == 1) k++; else Kalman_Filter();

		Mat diff(5, 1, CV_64F);
		cv::subtract(sScale, sumScale, diff);

		if (diff.at<double>(2, 0) < 1000 && diff.at<double>(3, 0) < 1000 && diff.at<double>(4, 0) < 1000)
		{
			da += diff.at<double>(2, 0);
			dx += diff.at<double>(3, 0);
			dx += diff.at<double>(4, 0);
		}
		if (fabs(dx) > 50)  dx = saveDX;
		if (fabs(dy) > 50)  dy = saveDY;
		if (fabs(da) > 50)  da = saveDA;

		//Creating the smoothed parameters matrix
		smoothedMat.at<double>(0, 0) = sx * cos(da);
		smoothedMat.at<double>(0, 1) = sx * -sin(da);
		smoothedMat.at<double>(1, 0) = sy * sin(da);
		smoothedMat.at<double>(1, 1) = sy * cos(da);

		smoothedMat.at<double>(0, 2) = dx;
		smoothedMat.at<double>(1, 2) = dy;

		//Warp the new frame using the smoothed parameters
		warpAffine(rgb, smoothedFrame, smoothedMat, rgb.size());

		//Crop the smoothed frame a little to eliminate black region due to Kalman Filter
		smoothedFrame = smoothedFrame(Range(vert_border, smoothedFrame.rows - vert_border), Range(HORIZONTAL_BORDER_CROP, smoothedFrame.cols - HORIZONTAL_BORDER_CROP));

		resize(smoothedFrame, smoothedFrame, rgb.size());
		for (int i = 0; i < features1.size(); ++i)
		{
			cv::circle(smoothedFrame, features1[i], 5, cv::Scalar::all(255), -1, cv::LineTypes::LINE_AA);
		}
		cv::putText(smoothedFrame, original, cv::Point(10, 50), cv::HersheyFonts::FONT_HERSHEY_COMPLEX, 0.4, cv::Scalar::all(255), 1);

		char buffer[1000];
		sprintf_s(buffer, "da = %f, dx = %f, dy = %f", da, dx, dy);
		cv::putText(smoothedFrame, buffer, cv::Point(10, 100), cv::HersheyFonts::FONT_HERSHEY_COMPLEX, 0.4, cv::Scalar::all(255), 1);
	}
	return smoothedFrame;
}

void VideoStab::Kalman_Filter()
{
	Mat f1err = Mat(5, 1, CV_64F);
	cv::add(errScale, qScale, f1err);
	for (int i = 0; i < f1err.rows; ++i)
	{
		double gainScale = f1err.at<double>(i, 0) / (f1err.at<double>(i, 0) + rScale.at<double>(i, 0));
		sScale.at<double>(i, 0) = sScale.at<double>(i, 0) + gainScale * (sumScale.at<double>(i, 0) - sScale.at<double>(i, 0));
		errScale.at<double>(i, 0) = (1.0 - gainScale) * f1err.at<double>(i, 0);
	}
}

