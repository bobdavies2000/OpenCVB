#pragma once
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/video.hpp>


using namespace std;
using namespace  cv;
class WarpModel
{
private:
public:
    int warpMode = MOTION_EUCLIDEAN;
    double termination_eps = 1e-5;
    int number_of_iterations = 50;
    TermCriteria criteria = TermCriteria(TermCriteria::COUNT + TermCriteria::EPS, number_of_iterations, termination_eps);
    Mat warpMatrix;
    WarpModel(){}
    void Run(Mat src1, Mat src2) {
        if (warpMode != MOTION_HOMOGRAPHY)
            warpMatrix = Mat::eye(2, 3, CV_32F);
        else
            warpMatrix = Mat::eye(3, 3, CV_32F);
        findTransformECC(src1, src2, warpMatrix, warpMode, criteria); // wish this were available in C#/VB.Net?
    }
};

extern "C" __declspec(dllexport)
WarpModel *WarpModel_Open() {
    WarpModel * cPtr = new WarpModel();
    return cPtr;
}

extern "C" __declspec(dllexport)
int * WarpModel_Close(WarpModel * cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int *WarpModel_Run(WarpModel * cPtr, int* src1Ptr, int* src2Ptr, int rows, int cols, int channels, int warpMode)
{
    Mat src1 = Mat(rows, cols, CV_8UC1, src1Ptr);
    Mat src2 = Mat(rows, cols, CV_8UC1, src2Ptr);
    cPtr->warpMode = warpMode; // don't worry - the enumerations are identical...
    cPtr->Run(src1, src2);
	return (int *)cPtr->warpMatrix.data;
}