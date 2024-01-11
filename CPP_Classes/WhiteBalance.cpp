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
// https://blog.csdn.net/just_sort/article/details/85982871
class WhiteBalance
{
private:
public:
	Mat src, output;
	WhiteBalance(){}

	void Run(float thresholdVal)
	{
		int row = src.rows, col = src.cols;
		int HistRGB[768] = { 0 };
		int MaxVal = 0;
		for (int i = 0; i < row; i++) {
			for (int j = 0; j < col; j++) {
				int b = int(src.at<Vec3b>(i, j)[0]);
				int g = int(src.at<Vec3b>(i, j)[1]);
				int r = int(src.at<Vec3b>(i, j)[2]);
				MaxVal = max(MaxVal, b);
				MaxVal = max(MaxVal, g);
				MaxVal = max(MaxVal, r);
				int sum = b + g + r;
				HistRGB[sum]++;
			}
		}
		int Threshold = 0;
		int sum = 0;
		for (int i = 767; i >= 0; i--) {
			sum += HistRGB[i];
			if (sum > int(row * col) * thresholdVal) {
				Threshold = i;
				break;
			}
		}
		 
		int AvgB = 0;
		int AvgG = 0;
		int AvgR = 0;
		int cnt = 0;
		for (int i = 0; i < row; i++) {
			for (int j = 0; j < col; j++) {
				int sumP = src.at<Vec3b>(i, j)[0] + src.at<Vec3b>(i, j)[1] + src.at<Vec3b>(i, j)[2];
				if (sumP > Threshold) {
					AvgB += src.at<Vec3b>(i, j)[0];
					AvgG += src.at<Vec3b>(i, j)[1];
					AvgR += src.at<Vec3b>(i, j)[2];
					cnt++;
				}
			}
		}
		if (cnt > 0)
		{
			AvgB /= cnt;
			AvgG /= cnt;
			AvgR /= cnt;
			for (int i = 0; i < row; i++) {
				for (int j = 0; j < col; j++) {
					if (AvgB == 0 || AvgG == 0 || AvgR == 0) continue;
					int Blue = src.at<Vec3b>(i, j)[0] * MaxVal / AvgB;
					int Green = src.at<Vec3b>(i, j)[1] * MaxVal / AvgG;
					int Red = src.at<Vec3b>(i, j)[2] * MaxVal / AvgR;
					if (Red > 255) Red = 255; else if (Red < 0) Red = 0;
					if (Green > 255) Green = 255; else if (Green < 0) Green = 0;
					if (Blue > 255) Blue = 255; else if (Blue < 0) Blue = 0;
					output.at<Vec3b>(i, j)[0] = Blue;
					output.at<Vec3b>(i, j)[1] = Green;
					output.at<Vec3b>(i, j)[2] = Red;
				}
			}
		}
	}
};

VB_EXTERN
WhiteBalance * WhiteBalance_Open(float ppx, float ppy, float fx, float fy)
{
	WhiteBalance* cPtr =  new WhiteBalance();
	return cPtr;
}

VB_EXTERN
int* WhiteBalance_Close(WhiteBalance * cPtr)
{
	delete cPtr;
	return (int*)0;
}

VB_EXTERN
int* WhiteBalance_Run(WhiteBalance * cPtr, int* rgb, int rows, int cols, float thresholdVal)
{
	cPtr->output = Mat(rows, cols, CV_8UC3);
	cPtr->src = Mat(rows, cols, CV_8UC3, rgb);
	cPtr->Run(thresholdVal);
	return (int*)cPtr->output.data;
}