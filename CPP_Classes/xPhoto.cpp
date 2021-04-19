#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/xphoto.hpp>
#include "opencv2/xphoto/oilpainting.hpp"
#include <iostream>
#include <cstdlib>
#include <cstdio>

using namespace std;
using namespace cv;
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
    xPhoto_OilPaint *xPhoto_OilPaint_Ptr = new xPhoto_OilPaint();
    return xPhoto_OilPaint_Ptr;
}

extern "C" __declspec(dllexport)
void xPhoto_OilPaint_Close(xPhoto_OilPaint *xPhoto_OilPaint_Ptr)
{
    delete xPhoto_OilPaint_Ptr;
} 

extern "C" __declspec(dllexport)
int *xPhoto_OilPaint_Run(xPhoto_OilPaint *xPhoto_OilPaint_Ptr, int *imagePtr, int rows, int cols, int size, int dynRatio, int colorCode)
{
	xPhoto_OilPaint_Ptr->src = Mat(rows, cols, CV_8UC3, imagePtr);
	xPhoto_OilPaint_Ptr->Run(size, dynRatio, colorCode);
    return (int *) xPhoto_OilPaint_Ptr->dst.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
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
    xPhoto_Inpaint* xPhoto_Inpaint_Ptr = new xPhoto_Inpaint();
    return xPhoto_Inpaint_Ptr;
}

extern "C" __declspec(dllexport)
void xPhoto_Inpaint_Close(xPhoto_Inpaint * xPhoto_Inpaint_Ptr)
{
    delete xPhoto_Inpaint_Ptr;
}

extern "C" __declspec(dllexport)
int* xPhoto_Inpaint_Run(xPhoto_Inpaint *xPhoto_Inpaint_Ptr, int* imagePtr, int* maskPtr, int rows, int cols, int iType)
{
    xPhoto_Inpaint_Ptr->src = Mat(rows, cols, CV_8UC3, imagePtr);
    xPhoto_Inpaint_Ptr->dst = Mat(rows, cols, CV_8UC3);
    Mat mask = Mat(rows, cols, CV_8UC1, maskPtr);
    xPhoto_Inpaint_Ptr->Run(mask, iType);
    return (int*)xPhoto_Inpaint_Ptr->dst.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}