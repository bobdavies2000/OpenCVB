#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/core/utility.hpp>
#include "OpenCVB_Extern.h"
using namespace std;
using namespace  cv;
class BGRPattern_Basics
{
private:
public:
    Mat src, dst;
    int classCount = 5;
    BGRPattern_Basics() {}
    void RunCPP() {
        for (int y = 0; y < src.rows; y++)
            for (int x = 0; x < src.cols; x++)
            {
                Vec3b vec = src.at<Vec3b>(y, x);
                int b = vec[0]; int g = vec[1]; int r = vec[2];
                if (b == g && g == r)
                {
                    dst.at<uchar>(y, x) = 1;
                }
                else if (b <= g && g <= r)
                {
                    dst.at<uchar>(y, x) = 2;
                }
                else if (b >= g && g >= r)
                {
                    dst.at<uchar>(y, x) = 3;
                }
                else if (b >= g && g <= r)
                {
                    dst.at<uchar>(y, x) = 4;
                }
                else if (b <= g && g >= r)
                {
                    dst.at<uchar>(y, x) = classCount;
                }
            }
    }
};
VB_EXTERN
BGRPattern_Basics * BGRPattern_Open() {
    BGRPattern_Basics* cPtr = new BGRPattern_Basics();
    return cPtr;
}
VB_EXTERN
void BGRPattern_Close(BGRPattern_Basics * cPtr)
{
    delete cPtr;
}

VB_EXTERN
int BGRPattern_ClassCount(BGRPattern_Basics * cPtr)
{
    return cPtr->classCount;
}

VB_EXTERN
int* BGRPattern_RunCPP(BGRPattern_Basics * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
    cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->RunCPP();
    return (int*)cPtr->dst.data;
}
