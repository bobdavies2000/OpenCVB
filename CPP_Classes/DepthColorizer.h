#pragma once
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "DepthColorizer.h"


using namespace std;
using namespace  cv;

class Depth_Colorizer
{
private:
public:
	Mat depth32f, output;
	int histSize = 255;
	Depth_Colorizer() {}
	void Run(float maxDepth)
	{
		float nearColor[3] = { 0, 1.0f, 1.0f };
		float farColor[3] = { 1.0f, 0, 0 };
		output = Mat(depth32f.size(), CV_8UC3);
		auto rgb = (unsigned char*)output.data;
		float* depthImage = (float*)depth32f.data;
		for (int i = 0; i < output.cols * output.rows; i++)
		{
			float t = depthImage[i] / maxDepth;
			if (t > 0 && t <= 1)
			{
				*rgb++ = uchar(((1 - t) * nearColor[0] + t * farColor[0]) * 255);
				*rgb++ = uchar(((1 - t) * nearColor[1] + t * farColor[1]) * 255);
				*rgb++ = uchar(((1 - t) * nearColor[2] + t * farColor[2]) * 255);
			}
			else {
				*rgb++ = 0; *rgb++ = 0; *rgb++ = 0;
			}
		}
	}
};




class DepthXYZ
{
private:
public:
	Mat depth, depthxyz;
	float ppx, ppy, fx, fy;
	DepthXYZ(float _ppx, float _ppy, float _fx, float _fy)
	{
		ppx = _ppx; ppy = _ppy; fx = _fx; fy = _fy;
	}
	void GetImageCoordinates()
	{
		depthxyz = Mat(depth.rows, depth.cols, CV_32FC3);
#ifdef _DEBUG
// #pragma omp parallel for  // doubles performance in debug mode but is much worse in Release mode.
#endif
		for (int y = 0; y < depth.rows; y++)
		{
			for (int x = 0; x < depth.cols; x++)
			{
				float d = float(depth.at<float>(y, x)) * 0.001f;
				depthxyz.at<Vec3f>(y, x) = Vec3f(float(x), float(y), d);
			}
		}
	}
	void Run()
	{
		depthxyz = Mat(depth.rows, depth.cols, CV_32FC3);
#ifdef _DEBUG
//#pragma omp parallel for  // doubles performance in debug mode but is much worse in Release mode.
#endif
		for (int y = 0; y < depth.rows; y++)
		{
			for (int x = 0; x < depth.cols; x++)
			{
				float d = float(depth.at<float>(y, x)) * 0.001f;
				if (d > 0) depthxyz.at< Vec3f >(y, x) = Vec3f(float((x - ppx) / fx), float((y - ppy) / fy), d);
			}
		}
	}
};



extern "C" __declspec(dllexport)
Depth_Colorizer * Depth_Colorizer_Open()
{
	Depth_Colorizer* cPtr = new Depth_Colorizer();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer_Close(Depth_Colorizer * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer_Run(Depth_Colorizer * cPtr, int* depthPtr, int rows, int cols, float maxDepth)
{
	cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	cPtr->Run(maxDepth);
	return (int*)cPtr->output.data;
}