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
class Density_2D
{
private:
public:
    Mat src, dst;
    Density_2D() {}
    float distanceTo(Point3f v1, Point3f v2)
    {
        return sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y) + (v1.z - v2.z) * (v1.z - v2.z));
    }
    void RunCPP(float zDistance) {
        dst.setTo(0);
        int offx[] = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int offy[] = { -1, -1, -1, 0, 0, 1, 1, 1 };
        for (int y = 1; y < src.rows - 1; y++)
        {
            for (int x = 1; x < src.cols - 1; x++)
            {
                float z1 = src.at<float>(y, x);
                if (z1 == 0) continue;
                float d = 0.0f;
                for (int i = 0; i < 8; i++)
                {
                    float z2 = src.at<float>(y + offy[i], x + offx[i]);
                    if (z2 == 0) continue;
                    d += abs(z1 - z2);
                }
                if (d < zDistance && d != 0.0f) dst.at<unsigned char>(y, x) = 255;
            }
        }
    }
};

VB_EXTERN
Density_2D * Density_2D_Open() {
    Density_2D* cPtr = new Density_2D();
    return cPtr;
}

VB_EXTERN
void Density_2D_Close(Density_2D * cPtr)
{
    delete cPtr;
}

VB_EXTERN
int* Density_2D_RunCPP(Density_2D * cPtr, int* dataPtr, int rows, int cols, float zDistance)
{
    cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
    cPtr->RunCPP(zDistance);
    return (int*)cPtr->dst.data;
}







class Density_Count
{
private:
public:
    Mat src, dst;
    Density_Count() {}
    void RunCPP(int zCount) {
        dst.setTo(0);
        int offx[] = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int offy[] = { -1, -1, -1, 0, 0, 1, 1, 1 };
        for (int y = 1; y < src.rows - 1; y++)
        {
            for (int x = 1; x < src.cols - 1; x++)
            {
                float z1 = src.at<float>(y, x);
                if (z1 == 0) continue;
                int count = 0;
                for (int i = 0; i < 8; i++)
                {
                    float z2 = src.at<float>(y + offy[i], x + offx[i]);
                    if (z2 > 0) count += 1;
                }
                if (count >= zCount) dst.at<unsigned char>(y, x) = 255;
            }
        }
    }
};

VB_EXTERN
Density_Count * Density_Count_Open() {
    Density_Count* cPtr = new Density_Count();
    return cPtr;
}

VB_EXTERN
void Density_Count_Close(Density_Count * cPtr)
{
    delete cPtr;
}

VB_EXTERN
int* Density_Count_RunCPP(Density_Count * cPtr, int* dataPtr, int rows, int cols, int zCount)
{
    cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
    cPtr->RunCPP(zCount);
    return (int*)cPtr->dst.data;
}
