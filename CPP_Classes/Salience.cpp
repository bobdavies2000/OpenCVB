#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "OpenCVB_Extern.h"

using namespace std;
using namespace  cv; 
#define MAX_NUM_SCALES (6)
class Salience
{
private:
	int ii_ColWidth;
	Mat integralImage;
	Mat intensityScaledOn[MAX_NUM_SCALES];
	Mat intensityScaledOff[MAX_NUM_SCALES];
	Mat intensityOn;
	Mat intensityOff;

	float getMean(Mat src, Point PixArg, int kernelsize, int PixelVal)
	{
		Point P1, P2;
		int value;

		P1.x = PixArg.x - kernelsize + 1;
		P1.y = PixArg.y - kernelsize + 1;
		P2.x = PixArg.x + kernelsize + 1;
		P2.y = PixArg.y + kernelsize + 1;

		if (P1.x < 0)
			P1.x = 0;
		else if (P1.x > src.cols - 1)
			P1.x = src.cols - 1;
		if (P2.x < 0)
			P2.x = 0;
		else if (P2.x > src.cols - 1)
			P2.x = src.cols - 1;
		if (P1.y < 0)
			P1.y = 0;
		else if (P1.y > src.rows - 1)
			P1.y = src.rows - 1;
		if (P2.y < 0)
			P2.y = 0;
		else if (P2.y > src.rows - 1)
			P2.y = src.rows - 1;

		// we use the integral image to compute fast features
#if 0
		value = src.at<int>(P2.y, P2.x) + src.at<int>(P1.y, P1.x) - src.at<int>(P2.y, P1.x) - src.at<int>(P1.y, P2.x);
#else
	// Try the other side of this ifdef.  It is a lot slower to use the "at" method.
		value = ((int*)(src.data + ii_ColWidth * P2.y))[P2.x] +
				((int*)(src.data + ii_ColWidth * P1.y))[P1.x] -
				((int*)(src.data + ii_ColWidth * P2.y))[P1.x] -
				((int*)(src.data + ii_ColWidth * P1.y))[P2.x];
#endif
		value = (value - PixelVal) / (((P2.x - P1.x) * (P2.y - P1.y)) - 1);
		return (float)value;
	}

	void getIntensityScaled(Mat gray, int i, int kernelsize)
	{
		float value, meanOn, meanOff;
		Point point;
		int PixelVal;
		for (int x = 0; x < gray.rows; x++) {
			uchar *on = ((uchar *)(intensityScaledOn[i].data + gray.cols * x));
			uchar *off = ((uchar *)(intensityScaledOff[i].data + gray.cols * x));
			for (int y = 0; y < gray.cols; y++) {
				PixelVal = ((uchar *)(gray.data + gray.cols * x))[y];
				value = getMean(integralImage, Point(y, x), kernelsize, PixelVal);

				meanOn = PixelVal - value;
				meanOff = value - PixelVal;

				if (meanOn > 0) on[y] = (uchar)meanOn;
				if (meanOff > 0) off[y] = (uchar)meanOff;
			}
		}
	}

public:
	Mat src, dst;
	int numScales;
	bool matsReady = false;
	Salience()
	{
	}

	void allocateMats(int rows, int cols)
	{
		ii_ColWidth = (cols + 1) * sizeof(int);
		Mat tmp(rows, cols, CV_8U);
		tmp.setTo(0);
		for (int i = 0; i < MAX_NUM_SCALES; ++i) {
			intensityScaledOn[i] = tmp.clone();
			intensityScaledOff[i] = tmp.clone();
		}
		matsReady = true;
	}
	Mat calcIntensity()
	{
		int kernelsize[] = { 3 * 4, 3 * 4 * 2, 3 * 4 * 2 * 2, 7 * 4, 7 * 4 * 2, 7 * 4 * 2 * 2 };

		// Calculate integral image, only once.
		integral(src, integralImage);

		for (int i = 0; i < numScales; ++i) {
			intensityScaledOn[i].setTo(0);
			intensityScaledOff[i].setTo(0);
			getIntensityScaled(src, i, kernelsize[i]);
		}

		Mat tmp32f(src.rows, src.cols, CV_32F), tmp;
		tmp32f.setTo(0);
		intensityOn = tmp32f.clone();
		intensityOff = tmp32f.clone();
		for (int i = 0; i < numScales; ++i) {
			intensityScaledOn[i].convertTo(tmp, CV_32F);
			intensityOn += tmp;
			intensityScaledOff[i].convertTo(tmp, CV_32F);
			intensityOff += tmp;
		}
		return intensityOn + intensityOff;
	}
};

VB_EXTERN
Salience *Salience_Open()
{
	Salience * cPtr = new Salience();
	return cPtr;
}

VB_EXTERN
int * Salience_Close(Salience * cPtr)
{
	delete cPtr;
	return (int*)0;
}

VB_EXTERN
int *Salience_Run(Salience * cPtr, int numScales, int *grayInput, int rows, int cols)
{
	cPtr->numScales = numScales;
	if (cPtr->matsReady == true) if (cPtr->src.rows != rows && cPtr->src.cols != cols) cPtr->allocateMats(rows, cols);
	cPtr->src = Mat(rows, cols, CV_8U, grayInput);
	if (cPtr->matsReady == false) cPtr->allocateMats(rows, cols);
	Mat gray32f = cPtr->calcIntensity();

	normalize(gray32f, gray32f, 0, 255, NORM_MINMAX, CV_32F);
	gray32f.convertTo(cPtr->dst, CV_8U);
	return (int *)cPtr->dst.data;
}