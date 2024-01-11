#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "VideoStab.h"
#include "OpenCVB_Extern.h"

using namespace std;
using namespace  cv;

class Stabilizer_Basics_CPP
{
private:
public:
	VideoStab stab;
	Mat rgb;
	Mat smoothedFrame;
    Stabilizer_Basics_CPP()
	{
		smoothedFrame = Mat(2, 3, CV_64F);
	}
    void Run()
    {
		smoothedFrame = stab.stabilize(rgb);
	}
};

VB_EXTERN
Stabilizer_Basics_CPP *Stabilizer_Basics_Open()
{
    Stabilizer_Basics_CPP * cPtr = new Stabilizer_Basics_CPP();
    return cPtr;
}

VB_EXTERN
int *Stabilizer_Basics_Close(Stabilizer_Basics_CPP * cPtr)
{
    delete cPtr;
	return (int*)0;
}

// https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
VB_EXTERN
int *Stabilizer_Basics_Run(Stabilizer_Basics_CPP * cPtr, int *bgrPtr, int rows, int cols)
{
	cPtr->rgb = Mat(rows, cols, CV_8UC3, bgrPtr);
	cvtColor(cPtr->rgb, cPtr->stab.gray, COLOR_BGR2GRAY);
	if (cPtr->stab.lastFrame.rows > 0) cPtr->Run(); // skips the first pass while the frames get loaded.
	cPtr->stab.gray.copyTo(cPtr->stab.lastFrame);
	return (int *)cPtr->stab.smoothedFrame.data;
}
