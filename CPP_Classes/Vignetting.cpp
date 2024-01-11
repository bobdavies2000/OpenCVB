#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "OpenCVB_Extern.h"

using namespace std;
using namespace  cv;

// https://stackoverflow.com/questions/22654770/creating-vignette-filter-in-opencv
class Vignetting_CPP
{
private:
public:
    Mat src, dst;
    Vignetting_CPP(){}
    double fastCos(double x) {
        x += 1.57079632;
        if (x > 3.14159265) x -= 6.28318531;
        if (x < 0) return 1.27323954 * x + 0.405284735 * x * x;
        return 1.27323954 * x - 0.405284735 * x * x;
    }
    double dist(double ax, double ay, double bx, double by) {
        return sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
    }
    void RunCPP(double radius, double centerX, double centerY, bool removal) {
        dst = src.clone();
        double maxDis = radius * dist(0, 0, centerX , centerY);
        double temp;

        for (int y = 0; y < src.rows; y++) {
            for (int x = 0; x < src.cols; x++) {
                temp = fastCos(dist(centerX, centerY, x, y) / maxDis);
                temp *= temp;
                if (removal)
                {
                    dst.at<Vec3b>(y, x)[0] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[0]) / temp);
                    dst.at<Vec3b>(y, x)[1] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[1]) / temp);
                    dst.at<Vec3b>(y, x)[2] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[2]) / temp);
                } else {
                    dst.at<Vec3b>(y, x)[0] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[0]) * temp);
                    dst.at<Vec3b>(y, x)[1] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[1]) * temp);
                    dst.at<Vec3b>(y, x)[2] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[2]) * temp);
                }
            }
        }
    }
};

VB_EXTERN
Vignetting_CPP *Vignetting_Open() {
    Vignetting_CPP *cPtr = new Vignetting_CPP();
    return cPtr;
}

VB_EXTERN
int * Vignetting_Close(Vignetting_CPP *cPtr)
{
    delete cPtr;
    return (int*)0;
}

VB_EXTERN
int *Vignetting_RunCPP(Vignetting_CPP *cPtr, int *dataPtr, int rows, int cols, double radius, double centerX, double centerY, bool removal)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
	cPtr->RunCPP(radius, centerX, centerY, removal);
	return (int *) cPtr->dst.data; 
}