#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace  cv;

Mat dst;
extern "C" __declspec(dllexport)
int * Corners_ShiTomasi(int *grayPtr, int rows, int cols, int blockSize, int apertureSize)
{
	Mat gray = Mat(rows, cols, CV_8UC1, grayPtr);
	dst = Mat::zeros(gray.size(), CV_32FC1);
	/// Shi-Tomasi -- Using cornerMinEigenVal - can't access this from opencvSharp...
	cornerMinEigenVal(gray, dst, blockSize, apertureSize, BORDER_DEFAULT);
	return (int *) dst.data;
}