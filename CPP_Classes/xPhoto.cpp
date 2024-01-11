#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/xphoto.hpp>
#include "opencv2/xphoto/oilpainting.hpp"
#include <iostream>
#include <cstdlib>
#include <cstdio>


using namespace std;
using namespace  cv;
class xPhoto_OilPaint
{
private:
public:
    Mat src, dst;
    xPhoto_OilPaint(){}
    void Run(int size, int dynRatio, int colorCode)
    {
		xphoto::oilPainting(src, dst, size, dynRatio, colorCode);
    }
};

extern "C" __declspec(dllexport)
xPhoto_OilPaint *xPhoto_OilPaint_Open()
{
    xPhoto_OilPaint * cPtr = new xPhoto_OilPaint();
    return cPtr;
}

extern "C" __declspec(dllexport)
int *xPhoto_OilPaint_Close(xPhoto_OilPaint * cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int *xPhoto_OilPaint_Run(xPhoto_OilPaint * cPtr, int *imagePtr, int rows, int cols, int size, int dynRatio, int colorCode)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, imagePtr);
    cPtr->Run(size, dynRatio, colorCode);
    return (int *)cPtr->dst.data; 
}






class xPhoto_Inpaint
{
private:
public:
    Mat src, dst;
    xPhoto_Inpaint() {}
    void Run(Mat mask, int iType)
    {
        dst.setTo(0);
        //xphoto::inpaint(src, mask, dst, iType);
    }
};

extern "C" __declspec(dllexport)
xPhoto_Inpaint * xPhoto_Inpaint_Open()
{
    xPhoto_Inpaint* cPtr = new xPhoto_Inpaint();
    return cPtr;
}

extern "C" __declspec(dllexport)
int * xPhoto_Inpaint_Close(xPhoto_Inpaint * cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* xPhoto_Inpaint_Run(xPhoto_Inpaint * cPtr, int* imagePtr, int* maskPtr, int rows, int cols, int iType)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, imagePtr);
    cPtr->dst = Mat(rows, cols, CV_8UC3);
    Mat mask = Mat(rows, cols, CV_8UC1, maskPtr);
    cPtr->Run(mask, iType);
    return (int*)cPtr->dst.data; 
}