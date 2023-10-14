#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace  cv;

extern "C" __declspec(dllexport)
void Palette_Custom(int* imgPtr, int *colorMapPtr, int *dstPtr, int rows, int cols, int channels)
{
	Mat img;
	if (channels == 1) 
		img = Mat(rows, cols, CV_8UC1, imgPtr);
	else
		img = Mat(rows, cols, CV_8UC3, imgPtr);

	Mat colorMap = Mat(256, 1, CV_8UC3, colorMapPtr);
	Mat output = Mat(rows, cols, CV_8UC3, dstPtr);
	applyColorMap(img, output, colorMap);
	return; 
}