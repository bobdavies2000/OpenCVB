#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"

using namespace std;
using namespace  cv;

class Agast
{
private:
public:
	Mat src, dst;
	std::vector<cv::KeyPoint> keypoints;
	Agast()	{}
	void Run() {
		keypoints.clear();
		static cv::Ptr<cv::AgastFeatureDetector> agastFD = cv::AgastFeatureDetector::create(10,
												 true, cv::AgastFeatureDetector::OAST_9_16);
		agastFD->detect(src, keypoints);
		dst = Mat(int(keypoints.size()), 7, CV_32F, keypoints.data());
	}
};

extern "C" __declspec(dllexport)
Agast * Agast_Open()
{
	Agast* cPtr = new Agast();
	return cPtr;
}

extern "C" __declspec(dllexport)
int * Agast_Close(Agast * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Agast_Run(Agast * cPtr, int* rgbPtr, int rows, int cols, int* count)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, rgbPtr);
	cPtr->Run();
	count[0] = int(cPtr->keypoints.size());
	if (count[0] == 0) return 0;
	return (int*)cPtr->dst.data; 
}