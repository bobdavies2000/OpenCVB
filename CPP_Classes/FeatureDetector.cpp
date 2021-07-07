#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"

using namespace std;
using namespace cv;

class Agast
{
private:
public:
	Mat src;
	cv::Ptr<cv::AgastFeatureDetector> agastFD;
	std::vector<cv::KeyPoint> keypoints;
	Agast()
	{
		agastFD = cv::AgastFeatureDetector::create(10, true, cv::AgastFeatureDetector::OAST_9_16);
	}
	void Run() {
		agastFD->detect(src, keypoints);
	}
};

extern "C" __declspec(dllexport)
Agast * Agast_Open()
{
	Agast* dPtr = new Agast();
	return dPtr;
}

extern "C" __declspec(dllexport)
void Agast_Close(Agast * dPtr)
{
	delete dPtr;
}

extern "C" __declspec(dllexport)
int* Agast_Run(Agast * dPtr, int* rgbPtr, int rows, int cols, int* count)
{
	dPtr->src = Mat(rows, cols, CV_8UC3, rgbPtr);
	dPtr->keypoints.clear();
	dPtr->Run();
	count[0] = (int)dPtr->keypoints.size();
	if (count[0] == 0) return 0;
	return (int*)&dPtr->keypoints[0]; // return this C++ allocated data to managed code
}