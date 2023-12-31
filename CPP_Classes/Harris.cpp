#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "harrisDetector.h"

using namespace std;
using namespace cv;
class Harris_Features
{
private:
public: 
	Mat src, dst;
	float threshold = 0.0001f;
	int neighborhood = 3;
	int aperture = 3;
	float HarrisParm = 0.01f;
	Harris_Features() {}
	void Run()
	{
		cv::Mat cornerStrength;
		cv::cornerHarris(src, cornerStrength, neighborhood, aperture, HarrisParm);
		cv::threshold(cornerStrength, dst, threshold, 255, cv::THRESH_BINARY_INV);
	}

};

extern "C" __declspec(dllexport)
Harris_Features *Harris_Features_Open()
{
	return new Harris_Features();
}

extern "C" __declspec(dllexport)
void Harris_Features_Close(Harris_Features *Harris_FeaturesPtr)
{
	delete Harris_FeaturesPtr;
}

extern "C" __declspec(dllexport)
int *Harris_Features_Run(Harris_Features *Harris_FeaturesPtr, int *rgbPtr, int rows, int cols, float threshold, int neighborhood, int aperture, float HarrisParm)
{
	Harris_FeaturesPtr->threshold = threshold;
	Harris_FeaturesPtr->neighborhood = neighborhood;
	Harris_FeaturesPtr->aperture = aperture;
	Harris_FeaturesPtr->HarrisParm = HarrisParm;
	Harris_FeaturesPtr->src = Mat(rows, cols, CV_8U, rgbPtr);
	Harris_FeaturesPtr->Run();
	return (int *) Harris_FeaturesPtr->dst.data; // return this C++ allocated data to managed code where it will be in the marshal.copy
}



class Harris_Detector
{
private:
public:
	std::vector<cv::Point> pts;
	Mat src;
	double qualityLevel = 0.02;
	HarrisDetector harris;
	Harris_Detector() {}
	void Run()
	{
		harris.detect(src);
		harris.getCorners(pts, qualityLevel);
		harris.drawOnImage(src, pts);
	}
};

extern "C" __declspec(dllexport)
Harris_Detector *Harris_Detector_Open()
{
	return new Harris_Detector();
}

extern "C" __declspec(dllexport)
void Harris_Detector_Close(Harris_Detector *Harris_DetectorPtr)
{
	delete Harris_DetectorPtr;
}

extern "C" __declspec(dllexport)
int *Harris_Detector_Run(Harris_Detector *Harris_DetectorPtr, int *rgbPtr, int rows, int cols, double qualityLevel, int *count)
{ 
	Harris_DetectorPtr->qualityLevel = qualityLevel;
	Harris_DetectorPtr->src = Mat(rows, cols, CV_8U, rgbPtr);
	Harris_DetectorPtr->pts.clear();
	Harris_DetectorPtr->Run();
	count[0] = (int) Harris_DetectorPtr->pts.size();
	if (count[0] == 0) return 0;
	return (int *)&Harris_DetectorPtr->pts[0]; // return this C++ allocated data to managed code where it will be in the marshal.copy
}