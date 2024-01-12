#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "DepthColorizer.hpp"


using namespace std;
using namespace  cv;

class SimpleProjection
{
private:
public:
	Mat depth32f, mask, viewTop, viewSide;
	SimpleProjection() {}

	void Run(float desiredMin, float desiredMax, int w, int h)
	{
		float range = float(desiredMax - desiredMin);
		float hRange = float(h);
		float wRange = float(w);
#pragma omp parallel for
		for (int y = 0; y < depth32f.rows; ++y)
		{
			for (int x = 0; x < depth32f.cols; ++x)
			{
				uchar m = mask.at<uchar>(y, x);
				if (m == 255)
				{
					float d = depth32f.at<float>(y, x);
					float dy = hRange * (d - desiredMin) / range;
					if (dy > 0 && dy < hRange) viewTop.at<uchar>(int((hRange - dy)), x) = 0;
					float dx = wRange * (d - desiredMin) / range;
					if (dx < wRange && dx > 0) viewSide.at<uchar>(y, int(dx)) = 0;
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
SimpleProjection * SimpleProjectionOpen() 
{
	SimpleProjection* cPtr = new SimpleProjection();
	return cPtr;
}

extern "C" __declspec(dllexport)
void SimpleProjectionClose(SimpleProjection * cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionSide(SimpleProjection * cPtr)
{
	return (int*)cPtr->viewSide.data;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionRun(SimpleProjection * cPtr, int* depthPtr, float desiredMin, float desiredMax, int rows, int cols)
{
	cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	threshold(cPtr->depth32f, cPtr->mask, 0, 255, ThresholdTypes::THRESH_BINARY);
	convertScaleAbs(cPtr->mask, cPtr->mask);
	cPtr->viewTop = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->viewSide = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->Run(desiredMin, desiredMax, cols, rows);
	return (int*)cPtr->viewTop.data;
}








class Project_GravityHistogram
{
private:
public:
	Mat xyz, histTop, histSide;
	Project_GravityHistogram() {}

	void Run(float maxZ, int w, int h)
	{
		float zHalf = maxZ / 2;
		float range = float(h);
		int shift = int((histTop.cols - histTop.rows) / 2); // shift to the center of the image.
//#pragma omp parallel for  // this is faster without OpenMP!  But try it again when opportunity arrives...
		for (int y = 0; y < xyz.rows; ++y)
		{
			for (int x = 0; x < xyz.cols; ++x)
			{
				Point3f pt = xyz.at<Point3f>(y, x);
				float d = pt.z;
				if (d > 0 and d < maxZ)
				{
					float fx = pt.x;
					int x = int(range * (zHalf + fx) / maxZ + shift); // maintain a 1:1 aspect ratio
					int y = int(range - range * d / maxZ);
					if (x >= 0 && x < xyz.cols && y >= 0 && y < xyz.rows) histTop.at<float>(y, x) += 1;

					float fy = pt.y;
					if (fy > -zHalf && fy < zHalf)
					{
						int x = int(range * d / maxZ + shift);
						int y = int(range * (zHalf + fy) / maxZ); // maintain a 1:1 aspect ratio
						if (x >= 0 && x < xyz.cols && y >= 0 && y < xyz.rows) histSide.at<float>(y, x) += 1;
					}
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
Project_GravityHistogram * Project_GravityHist_Open()
{
	Project_GravityHistogram* cPtr = new Project_GravityHistogram();
	return cPtr;
}

extern "C" __declspec(dllexport)
int * Project_GravityHist_Close(Project_GravityHistogram * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Side(Project_GravityHistogram * cPtr)
{
	return (int*)cPtr->histSide.data;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Run(Project_GravityHistogram * cPtr, int* xyzPtr, float maxZ, int rows, int cols)
{
	cPtr->xyz = Mat(rows, cols, CV_32FC3, xyzPtr);
	cPtr->histTop = Mat(rows, cols, CV_32F).setTo(0);
	cPtr->histSide = Mat(rows, cols, CV_32F).setTo(0);
	cPtr->Run(maxZ, cols, rows);
	return (int*)cPtr->histTop.data;
}