#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "DepthColorizer.hpp"
#include "OpenCVB_Extern.h"

using namespace std;
using namespace  cv;

VB_EXTERN
Depth_Colorizer * Depth_Colorizer_Open()
{
	Depth_Colorizer* cPtr = new Depth_Colorizer();
	return cPtr;
}

VB_EXTERN
int * Depth_Colorizer_Close(Depth_Colorizer * cPtr)
{
	delete cPtr;
	return (int*)0;
}

VB_EXTERN
int* Depth_Colorizer_Run(Depth_Colorizer * cPtr, int* depthPtr, int rows, int cols, float maxDepth)
{
	cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	cPtr->Run(maxDepth);
	return (int*)cPtr->output.data;
}
