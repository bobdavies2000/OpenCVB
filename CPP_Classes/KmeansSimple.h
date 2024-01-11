#pragma once
#include <cstdlib>
#include <cstdio>
#include <iostream> 
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>


using namespace std;
using namespace  cv;
class Kmeans_Simple
{
private:
public:
    Mat src, dst;
    Kmeans_Simple(){}
    void RunCPP(float minVal, float maxVal) {
        dst.setTo(0);
        Vec3b yellow(0, 255, 255);
        Vec3b blue(255, 0, 0);
        for (int y = 0; y < dst.rows; y++)
        {
            for (int x = 0; x < dst.cols; x++)
            {
                float b = src.at<float>(y, x);
                if (b != 0)
                {
                    if ((maxVal - b) < (b - minVal)) dst.at<Vec3b>(y, x) = blue; else dst.at<Vec3b>(y, x) = yellow;
                }
            }
        }
    }
};

extern "C" __declspec(dllexport)
Kmeans_Simple *Kmeans_Simple_Open() {
    Kmeans_Simple *cPtr = new Kmeans_Simple();
    return cPtr;
}

extern "C" __declspec(dllexport)
int * Kmeans_Simple_Close(Kmeans_Simple *cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int *Kmeans_Simple_RunCPP(Kmeans_Simple *cPtr, int *dataPtr, int rows, int cols, float minVal, float maxVal)
{
    cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
    cPtr->dst = Mat(rows, cols, CV_8UC3);
    cPtr->RunCPP(minVal, maxVal);
	return (int *) cPtr->dst.data; 
}
