#pragma once
#include <string.h>
#include <Windows.h>
#include <OleAuto.h>
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"
#include "opencv2/ml/ml.hpp "
#include "opencv2/imgproc.hpp"
#include "opencv2/videoio.hpp"
#include <numeric>
#include <iomanip>
#include <sstream>
#include <memory>
#include <vector>
#include <random>
#include "opencv2/video/tracking.hpp"
#include "opencv2/bgsegm.hpp"
#include "opencv2/photo.hpp"
#include <map>
#include <opencv2/ml.hpp>
#include "harrisDetector.h"
#include <opencv2/plot.hpp>
#include "opencv2/ccalib/randpattern.hpp"
#include "opencv2/xphoto/oilpainting.hpp"
#include "../CPP_Native/PragmaLibs.h"

using namespace std;
using namespace cv;
using namespace bgsegm;
using namespace ximgproc;
using namespace ml;

#include "CPP_Parent.h"
class RedCloud
{
private:
public:
    Mat src, result;
    vector<Rect>cellRects;
    vector<Point> floodPoints;

    RedCloud() {}
    void RunCPP(int minSize) {
        Mat mask(cv::Size(src.cols + 2, src.rows + 2), CV_8U);
        mask.setTo(0);
        Rect rect;

        multimap<int, Point, greater<int>> sizeSorted;
        int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
        Point pt;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                pt = Point(x, y);
                int count = floodFill(src, mask, pt, 255, &rect, 0, 0, 4 | floodFlag | (255 << 8));
                if (rect.width > 1 && rect.height > 1) sizeSorted.insert(make_pair(count, pt));
            }
        }

        cellRects.clear();
        floodPoints.clear();
        mask.setTo(0);
        int fill = 1;
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            if (floodFill(src, mask, it->second, fill, &rect, 0, 0, 4 | floodFlag | (fill << 8)) >= 1)
            {
                if (rect.width * rect.height > minSize)
                {
                    cellRects.push_back(rect);
                    floodPoints.push_back(it->second);

                    if (fill >= 255)
                        break; // just taking up to the top X largest objects found.
                    fill++;
                }
            }
        }
        Rect r = Rect(1, 1, mask.cols - 2, mask.rows - 2);
        mask(r).copyTo(result);
    }
};

extern "C" __declspec(dllexport) RedCloud* RedCloud_Open() { return new RedCloud(); }
extern "C" __declspec(dllexport) int RedCloud_Count(RedCloud* cPtr) { return (int)cPtr->cellRects.size(); }
extern "C" __declspec(dllexport) int* RedCloud_Rects(RedCloud* cPtr) { return (int*)&cPtr->cellRects[0]; }
extern "C" __declspec(dllexport) int* RedCloud_Close(RedCloud* cPtr) { delete cPtr; return (int*)0; }

extern "C" __declspec(dllexport) int*
RedCloud_Run(RedCloud* cPtr, int* dataPtr, int rows, int cols, int minSize)
{
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->RunCPP(minSize);
    return (int*)cPtr->result.data;
}








class PrepXY
{
private:
public:
    Mat dst, result;
    vector<Point> floodPoints;

    PrepXY() {}
    void RunCPP(Mat splitX, Mat splitY, float xRange, float yRange, int bins) {
        float ranges[] = { -xRange, xRange };
        Mat histogram; Mat input;
        int hbins[] = { bins };
        for (int i = 0; i < 2; i++)
        {
            if (i == 0)
            {
                ranges[0] = -xRange;
                ranges[1] = xRange;
                input = splitX;
            }
            else {
                ranges[0] = -yRange;
                ranges[1] = yRange;
                input = splitY;
            }

            const float* range[] = { ranges };
            calcHist(&input, 1, { 0 }, Mat(), histogram, 1, hbins, range);

            float* histArray = (float*)histogram.data;
            for (int j = 0; j < histogram.rows; j++) histArray[j] = j;

            calcBackProject(&input, 1, 0, histogram, dst, range);

            if (i == 0) result = dst.clone(); else result += dst;
        }

        result.convertTo(result, CV_8U);
    }
};

extern "C" __declspec(dllexport) PrepXY* PrepXY_Open()
{
    return new PrepXY();
}

extern "C" __declspec(dllexport) int* PrepXY_Close(PrepXY* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int*
PrepXY_Run(PrepXY* cPtr, int* splitXPtr, int* splitYPtr, int rows, int cols, float xRange, float yRange, int bins)
{
    Mat splitX = Mat(rows, cols, CV_32F, splitXPtr);
    Mat splitY = Mat(rows, cols, CV_32F, splitYPtr);

    cPtr->RunCPP(splitX, splitY, xRange, yRange, bins);

    return (int*)cPtr->result.data;
}






class RedMask
{
private:
public:
    Mat src, result;
    vector<Rect>cellRects;
    vector<Point> floodPoints;

    RedMask() {}
    void RunCPP(int minSize) {
        result = Mat(src.rows + 2, src.cols + 2, CV_8U);
        result.setTo(0);

        multimap<int, Point, greater<int>> sizeSorted;
        int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
        Point pt;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                if (src.at<unsigned char>(y, x) != 0)
                {
                    pt = Point(x, y);
                    int count = floodFill(src, result, pt, 255, nullptr, 0, 0, 4 | floodFlag | (255 << 8));
                    if (count > minSize) sizeSorted.insert(make_pair(count, pt));
                }
            }
        }

        cellRects.clear();
        floodPoints.clear();
        int fill = 1;
        result.setTo(0);
        Rect rect;
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            if (floodFill(src, result, it->second, fill, &rect, 0, 0, 4 | floodFlag | (fill << 8)) >= 1)
            {
                cellRects.push_back(rect);
                floodPoints.push_back(it->second);

                if (fill >= 255) break; // just taking up to the top X largest objects found.
                fill++;
            }
        }
    }
};

extern "C" __declspec(dllexport) RedMask* RedMask_Open()
{
    return new RedMask();
}

extern "C" __declspec(dllexport) int RedMask_Count(RedMask* cPtr)
{
    return (int)cPtr->cellRects.size();
}

extern "C" __declspec(dllexport) int* RedMask_Rects(RedMask* cPtr)
{
    return (int*)&cPtr->cellRects[0];
}

extern "C" __declspec(dllexport) int* RedMask_Close(RedMask* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int*
RedMask_Run(RedMask* cPtr, int* dataPtr, int rows, int cols, int minSize)
{
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);

    cPtr->RunCPP(minSize);

    return (int*)cPtr->result.data;
}






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

extern "C" __declspec(dllexport)
Density_2D* Density_2D_Open() {
    Density_2D* cPtr = new Density_2D();
    return cPtr;
}

extern "C" __declspec(dllexport)
void Density_2D_Close(Density_2D* cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* Density_2D_RunCPP(Density_2D* cPtr, int* dataPtr, int rows, int cols, double zDistance)
{
    cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
    cPtr->RunCPP(zDistance);
    return (int*)cPtr->dst.data;
}






class Edge_DepthGap
{
private:
public:
    Mat src, dst;
    Edge_DepthGap() {}
    void RunCPP(float minDiff) {
        dst = Mat(src.rows, src.cols, CV_8UC1);
        dst.setTo(0);
        for (int y = 1; y < src.rows - 1; y++)
        {
            for (int x = 1; x < src.cols - 1; x++)
            {
                float b1 = src.at<float>(y, x - 1);
                float b2 = src.at<float>(y, x);
                if (abs(b1 - b2) >= minDiff)
                {
                    Rect r = Rect(x, y - 1, 2, 3);
                    dst(r).setTo(255);
                }

                b1 = src.at<float>(y - 1, x);
                if (abs(b1 - b2) >= minDiff)
                {
                    Rect r = Rect(x - 1, y, 3, 2);
                    dst(r).setTo(255);
                }
            }
        }
    }
};

extern "C" __declspec(dllexport)
Edge_DepthGap* Edge_DepthGap_Open() {
    Edge_DepthGap* cPtr = new Edge_DepthGap();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Edge_DepthGap_Close(Edge_DepthGap* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_DepthGap_RunCPP(Edge_DepthGap* cPtr, int* dataPtr, int rows, int cols, float minDiff)
{
    cPtr->src = Mat(rows, cols, CV_32FC1, dataPtr);
    cPtr->RunCPP(minDiff);
    return (int*)cPtr->dst.data;
}





class Guess_Depth
{
private:
public:
    Mat src;
    Guess_Depth() {}
    void RunCPP() {
        for (int y = 1; y < src.rows - 1; y++)
        {
            for (int x = 1; x < src.cols - 1; x++)
            {
                Point3f s1 = src.at<Point3f>(y, x - 1);
                Point3f s2 = src.at<Point3f>(y, x);
                if (s1.z > 0 && s2.z == 0)
                {
                    Point3f s3 = src.at<Point3f>(y - 1, x);
                    Point3f s4 = src.at<Point3f>(y + 1, x);
                    Point3f s5 = src.at<Point3f>(y, x + 1);
                    if ((s3.z > 0 && s4.z > 0) || s5.z > 0) src.at<Point3f>(y, x) = s1; // duplicate the neighbor next to this pixel missing any depth.
                }
            }
        }
    }
};


extern "C" __declspec(dllexport)
Guess_Depth* Guess_Depth_Open() {
    Guess_Depth* cPtr = new Guess_Depth();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Guess_Depth_Close(Guess_Depth* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* Guess_Depth_RunCPP(Guess_Depth* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_32FC3, dataPtr);
    cPtr->RunCPP();
    return (int*)cPtr->src.data;
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

extern "C" __declspec(dllexport)
Density_Count* Density_Count_Open() {
    Density_Count* cPtr = new Density_Count();
    return cPtr;
}

extern "C" __declspec(dllexport)
void Density_Count_Close(Density_Count* cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* Density_Count_RunCPP(Density_Count* cPtr, int* dataPtr, int rows, int cols, int zCount)
{
    cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
    cPtr->RunCPP(zCount);
    return (int*)cPtr->dst.data;
}




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
        for (int i = 0; i < output.total(); i++)
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





extern "C" __declspec(dllexport)
Depth_Colorizer* Depth_Colorizer_Open()
{
    Depth_Colorizer* cPtr = new Depth_Colorizer();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer_Close(Depth_Colorizer* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer_Run(Depth_Colorizer* cPtr, int* depthPtr, int rows, int cols, float maxDepth)
{
    cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
    cPtr->Run(maxDepth);
    return (int*)cPtr->output.data;
}







class SimpleProjection
{
private:
public:
    Mat depth32f, mask, viewTop, viewSide;
    SimpleProjection() {}

    void Run(float desiredMin, float desiredMax, int w, int h)
    {
        float range = float(desiredMax - desiredMin);
        float hRange = float(h);
        float wRange = float(w);
#pragma omp parallel for
        for (int y = 0; y < depth32f.rows; ++y)
        {
            for (int x = 0; x < depth32f.cols; ++x)
            {
                uchar m = mask.at<uchar>(y, x);
                if (m == 255)
                {
                    float d = depth32f.at<float>(y, x);
                    float dy = hRange * (d - desiredMin) / range;
                    if (dy > 0 && dy < hRange) viewTop.at<uchar>(int((hRange - dy)), x) = 0;
                    float dx = wRange * (d - desiredMin) / range;
                    if (dx < wRange && dx > 0) viewSide.at<uchar>(y, int(dx)) = 0;
                }
            }
        }
    }
};

extern "C" __declspec(dllexport)
SimpleProjection* SimpleProjectionOpen()
{
    SimpleProjection* cPtr = new SimpleProjection();
    return cPtr;
}

extern "C" __declspec(dllexport)
void SimpleProjectionClose(SimpleProjection* cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionSide(SimpleProjection* cPtr)
{
    return (int*)cPtr->viewSide.data;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionRun(SimpleProjection* cPtr, int* depthPtr, float desiredMin, float desiredMax, int rows, int cols)
{
    cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
    threshold(cPtr->depth32f, cPtr->mask, 0, 255, ThresholdTypes::THRESH_BINARY);
    convertScaleAbs(cPtr->mask, cPtr->mask);
    cPtr->viewTop = Mat(rows, cols, CV_8U).setTo(255);
    cPtr->viewSide = Mat(rows, cols, CV_8U).setTo(255);
    cPtr->Run(desiredMin, desiredMax, cols, rows);
    return (int*)cPtr->viewTop.data;
}








class Project_GravityHistogram
{
private:
public:
    Mat xyz, histTop, histSide;
    Project_GravityHistogram() {}

    void Run(float maxZ, int w, int h)
    {
        float zHalf = maxZ / 2;
        float range = float(h);
        int shift = int((histTop.cols - histTop.rows) / 2); // shift to the center of the image.
        //#pragma omp parallel for  // this is faster without OpenMP!  But try it again when opportunity arrives...
        for (int y = 0; y < xyz.rows; ++y)
        {
            for (int x = 0; x < xyz.cols; ++x)
            {
                Point3f pt = xyz.at<Point3f>(y, x);
                float d = pt.z;
                if (d > 0 && d < maxZ)
                {
                    float fx = pt.x;
                    int x = int(range * (zHalf + fx) / maxZ + shift); // maintain a 1:1 aspect ratio
                    int y = int(range - range * d / maxZ);
                    if (x >= 0 && x < xyz.cols && y >= 0 && y < xyz.rows) histTop.at<float>(y, x) += 1;

                    float fy = pt.y;
                    if (fy > -zHalf && fy < zHalf)
                    {
                        int x = int(range * d / maxZ + shift);
                        int y = int(range * (zHalf + fy) / maxZ); // maintain a 1:1 aspect ratio
                        if (x >= 0 && x < xyz.cols && y >= 0 && y < xyz.rows) histSide.at<float>(y, x) += 1;
                    }
                }
            }
        }
    }
};

extern "C" __declspec(dllexport)
Project_GravityHistogram* Project_GravityHist_Open()
{
    Project_GravityHistogram* cPtr = new Project_GravityHistogram();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Close(Project_GravityHistogram* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Side(Project_GravityHistogram* cPtr)
{
    return (int*)cPtr->histSide.data;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Run(Project_GravityHistogram* cPtr, int* xyzPtr, float maxZ, int rows, int cols)
{
    cPtr->xyz = Mat(rows, cols, CV_32FC3, xyzPtr);
    cPtr->histTop = Mat(rows, cols, CV_32F).setTo(0);
    cPtr->histSide = Mat(rows, cols, CV_32F).setTo(0);
    cPtr->Run(maxZ, cols, rows);
    return (int*)cPtr->histTop.data;
}





// https://docs.opencv.org/3.1.0/d0/da5/tutorial_ximgproc_prediction.html
class Edge_RandomForest
{
private:
    Ptr<StructuredEdgeDetection> pDollar;
public:
    Mat dst32f, src32f, gray8u;
    Edge_RandomForest(char* modelFileName) { pDollar = createStructuredEdgeDetection(modelFileName); }

    void Run(Mat src)
    {
        src.convertTo(src32f, CV_32FC3, 1.0 / 255.0);
        pDollar->detectEdges(src32f, dst32f);
        dst32f.convertTo(gray8u, CV_8U, 255);
    }
};

extern "C" __declspec(dllexport)
Edge_RandomForest* Edge_RandomForest_Open(char* modelFileName)
{
    return new Edge_RandomForest(modelFileName);
}

extern "C" __declspec(dllexport)
int* Edge_RandomForest_Close(Edge_RandomForest* Edge_RandomForestPtr)
{
    delete Edge_RandomForestPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_RandomForest_Run(Edge_RandomForest* Edge_RandomForestPtr, int* bgrPtr, int rows, int cols)
{
    Edge_RandomForestPtr->Run(Mat(rows, cols, CV_8UC3, bgrPtr));
    return (int*)Edge_RandomForestPtr->gray8u.data;
}





// https://github.com/opencv/opencv_contrib/blob/master/modules/ximgproc/samples/dericheSample.py
class Edge_Deriche
{
private:
public:
    Mat src, dst;
    Edge_Deriche() {}
    void Run(float alpha, float omega) {
        Mat xdst, ydst;
        ximgproc::GradientDericheX(src, xdst, alpha, omega);
        ximgproc::GradientDericheY(src, ydst, alpha, omega);
        Mat dx2 = xdst.mul(xdst);
        Mat dy2 = ydst.mul(ydst);
        Mat d2 = dx2 + dy2;
        sqrt(d2, d2);
        normalize(d2, d2, 255, NormTypes::NORM_MINMAX);
        d2.convertTo(dst, CV_8UC3, 255, 0);
    }
};

extern "C" __declspec(dllexport)
Edge_Deriche* Edge_Deriche_Open()
{
    Edge_Deriche* dPtr = new Edge_Deriche();
    return dPtr;
}

extern "C" __declspec(dllexport)
int* Edge_Deriche_Close(Edge_Deriche* dPtr)
{
    delete dPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_Deriche_Run(Edge_Deriche* dPtr, int* bgrPtr, int rows, int cols, float alpha, float omega)
{
    dPtr->src = Mat(rows, cols, CV_8UC3, bgrPtr);
    dPtr->Run(alpha, omega);
    return (int*)dPtr->dst.data;
}









using namespace std;
using namespace  cv;
class Edge_ColorGap
{
private:
public:
    Mat src, dst = Mat(0, 0, CV_8U);
    Edge_ColorGap() {}
    void Run(int distance, int diff) {
        dst.setTo(0);
        int half = distance / 2, pix1, pix2;
        for (int y = 0; y < dst.rows; y++)
        {
            for (int x = distance; x < dst.cols - distance; x++)
            {
                pix1 = src.at<unsigned char>(y, x);
                pix2 = src.at<unsigned char>(y, x + distance);
                if (abs(pix1 - pix2) >= diff) dst.at<unsigned char>(y, x + half) = 255;
            }
        }
        for (int y = distance; y < dst.rows - distance; y++)
        {
            for (int x = 0; x < dst.cols; x++)
            {
                pix1 = src.at<unsigned char>(y, x);
                pix2 = src.at<unsigned char>(y + distance, x);
                if (abs(pix1 - pix2) >= diff) dst.at<unsigned char>(y + half, x) = 255;
            }
        }
    }
};

extern "C" __declspec(dllexport)
Edge_ColorGap* Edge_ColorGap_Open() {
    Edge_ColorGap* cPtr = new Edge_ColorGap();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Edge_ColorGap_Close(Edge_ColorGap* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_ColorGap_Run(Edge_ColorGap* cPtr, int* grayPtr, int rows, int cols, int distance, int diff)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, grayPtr);
    if (cPtr->dst.rows != rows) cPtr->dst = Mat(rows, cols, CV_8UC1);
    cPtr->Run(distance, diff);
    return (int*)cPtr->dst.data;
}






// Why did we need a C++ version of the EM OpenCV API's?  Because the OpenCVSharp Predict2 interface seems to be broken.
class EMax_Raw
{
private:
public:
    Mat samples, output;
    Mat labels;
    Mat testInput;
    Ptr<EM> em_model;
    RNG rng;
    int clusters;
    int covarianceMatrixType;
    int stepSize;
    EMax_Raw()
    {
        em_model = EM::create();
    }
    void Run()
    {
        em_model->setClustersNumber(clusters);
        em_model->setCovarianceMatrixType(covarianceMatrixType);
        em_model->setTermCriteria(TermCriteria(TermCriteria::COUNT + TermCriteria::EPS, 300, 0.1));
        em_model->trainEM(samples, noArray(), labels, noArray());

        // classify every image pixel
        Mat sample(1, 2, CV_32FC1);
        output.setTo(0);
        int half = stepSize / 2;
        for (int y = 0; y < output.rows; y += stepSize)
        {
            for (int x = 0; x < output.cols; x += stepSize)
            {
                sample.at<float>(0) = float(x);
                sample.at<float>(1) = float(y);
                int response = cvRound(em_model->predict2(sample, noArray())[1]);
                circle(output, Point(x, y), stepSize, response, -1);
            }
        }

    }
};

extern "C" __declspec(dllexport)
EMax_Raw* EMax_Open()
{
    EMax_Raw* cPtr = new EMax_Raw();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* EMax_Close(EMax_Raw* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* EMax_Run(EMax_Raw* cPtr, int* samplePtr, int* labelsPtr, int inCount, int dimension, int rows, int cols, int clusters,
    int stepSize, int covarianceMatrixType)
{
    cPtr->covarianceMatrixType = covarianceMatrixType;
    cPtr->stepSize = stepSize;
    cPtr->clusters = clusters;
    cPtr->labels = Mat(inCount, 1, CV_32S, labelsPtr);
    cPtr->samples = Mat(inCount, dimension, CV_32FC1, samplePtr);
    cPtr->output = Mat(rows, cols, CV_32S);
    cPtr->Run();

    return (int*)cPtr->output.data;
}





float outputTriangleAMS[5];
extern "C" __declspec(dllexport)
int* FitEllipse_AMS(float* inputPoints, int count)
{
    Mat input(count, 1, CV_32FC2, inputPoints);
    RotatedRect box = fitEllipseAMS(input);
    outputTriangleAMS[0] = box.angle;
    outputTriangleAMS[1] = box.center.x;
    outputTriangleAMS[2] = box.center.y;
    outputTriangleAMS[3] = box.size.width;
    outputTriangleAMS[4] = box.size.height;
    return (int*)&outputTriangleAMS;
}



float outputTriangle[5];
extern "C" __declspec(dllexport)
int* FitEllipse_Direct(float* inputPoints, int count)
{
    Mat input(count, 1, CV_32FC2, inputPoints);
    RotatedRect box = fitEllipseDirect(input);
    outputTriangle[0] = box.angle;
    outputTriangle[1] = box.center.x;
    outputTriangle[2] = box.center.y;
    outputTriangle[3] = box.size.width;
    outputTriangle[4] = box.size.height;
    return (int*)&outputTriangle;
}








class Fuzzy
{
private:
public:
    Mat src, dst;
    Fuzzy() {}
    void Run() {
        dst = Mat(src.rows, src.cols, CV_8U);
        dst.setTo(0);
        for (int y = 1; y < src.rows - 3; ++y)
        {
            //#pragma omp parallel for 
            for (int x = 1; x < src.cols - 3; ++x)
            {
                int pixel = src.at<uchar>(y, x);
                Rect r = Rect(x, y, 3, 3);
                Scalar sum = cv::sum(src(r));
                if (sum.val[0] == double(pixel * 9)) dst.at<uchar>(y + 1, x + 1) = pixel;
            }
        }
    }
};

extern "C" __declspec(dllexport)
Fuzzy* Fuzzy_Open() {
    Fuzzy* cPtr = new Fuzzy();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Fuzzy_Close(Fuzzy* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Fuzzy_Run(Fuzzy* cPtr, int* grayPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, grayPtr);
    cPtr->Run();
    return (int*)cPtr->dst.data;
}







class Guess_ImageEdges
{
private:
public:
    Mat pc;
    Guess_ImageEdges() {}
    void RunCPP(int maxDistanceToEdge) {
        int y = 0;
        int maxGap = maxDistanceToEdge;
        for (int x = 0; x < pc.cols; x++)
        {
            for (int y = 0; y < maxGap; y++)
            {
                Point3f s1 = pc.at<Point3f>(y, x);
                if (s1.z > 0)
                {
                    for (int i = 0; i < y; i++)
                    {
                        pc.at<Point3f>(i, x) = s1;
                    }
                    break;
                }
            }
            for (int y = 1; y < maxGap; y++)
            {
                Point3f s1 = pc.at<Point3f>(pc.rows - y, x);
                if (s1.z > 0)
                {
                    for (int i = 1; i < y; i++)
                    {
                        pc.at<Point3f>(pc.rows - i, x) = s1;
                    }
                    break;
                }
            }
        }
        for (int y = 0; y < pc.rows; y++)
        {
            for (int x = 0; x < maxGap; x++)
            {
                Point3f s1 = pc.at<Point3f>(y, x);
                if (s1.z > 0)
                {
                    for (int i = 0; i < x; i++)
                    {
                        pc.at<Point3f>(y, i) = s1;
                    }
                    break;
                }
            }
            for (int x = 1; x < maxGap; x++)
            {
                Point3f s1 = pc.at<Point3f>(y, pc.cols - x);
                if (s1.z > 0)
                {
                    for (int i = 1; i < x; i++)
                    {
                        pc.at<Point3f>(y, pc.cols - i) = s1;
                    }
                    break;
                }
            }
        }
    }
};






extern "C" __declspec(dllexport)
Guess_ImageEdges* Guess_ImageEdges_Open() {
    Guess_ImageEdges* cPtr = new Guess_ImageEdges();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Guess_ImageEdges_Close(Guess_ImageEdges* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* Guess_ImageEdges_RunCPP(Guess_ImageEdges* cPtr, int* dataPtr, int rows, int cols, int maxDistanceToEdge)
{
    cPtr->pc = Mat(rows, cols, CV_32FC3, dataPtr);
    cPtr->RunCPP(maxDistanceToEdge);
    return (int*)cPtr->pc.data;
}






class Harris_Features
{
private:
public:
    Mat src, dst;
    float threshold = 0.0001f;
    int neighborhood = 3;
    int aperture = 3;
    float HarrisParm = 0.01f;
    Harris_Features() {}
    void Run()
    {
        Mat cornerStrength;
        cornerHarris(src, cornerStrength, neighborhood, aperture, HarrisParm);
        cv::threshold(cornerStrength, dst, threshold, 255, THRESH_BINARY_INV);
    }
};

extern "C" __declspec(dllexport)
Harris_Features* Harris_Features_Open()
{
    return new Harris_Features();
}

extern "C" __declspec(dllexport)
int* Harris_Features_Close(Harris_Features* Harris_FeaturesPtr)
{
    delete Harris_FeaturesPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Harris_Features_Run(Harris_Features* Harris_FeaturesPtr, int* bgrPtr, int rows, int cols, float threshold, int neighborhood, int aperture, float HarrisParm)
{
    Harris_FeaturesPtr->threshold = threshold;
    Harris_FeaturesPtr->neighborhood = neighborhood;
    Harris_FeaturesPtr->aperture = aperture;
    Harris_FeaturesPtr->HarrisParm = HarrisParm;
    Harris_FeaturesPtr->src = Mat(rows, cols, CV_8U, bgrPtr);
    Harris_FeaturesPtr->Run();
    return (int*)Harris_FeaturesPtr->dst.data;
}



class Harris_Detector
{
private:
public:
    std::vector<Point> points;
    Mat src;
    HarrisDetector harris;
    Harris_Detector() {}
    void Run(double qualityLevel)
    {
        points.clear();
        harris.detect(src);
        harris.getCorners(points, qualityLevel);
        //harris.drawOnImage(src, points);
    }
};

extern "C" __declspec(dllexport)
Harris_Detector* Harris_Detector_Open()
{
    return new Harris_Detector();
}

extern "C" __declspec(dllexport) int Harris_Detector_Count(Harris_Detector* cPtr)
{
    return (int)cPtr->points.size();
}

extern "C" __declspec(dllexport)
int* Harris_Detector_Close(Harris_Detector* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Harris_Detector_Run(Harris_Detector* cPtr, int* bgrPtr, int rows, int cols, double qualityLevel, int* count)
{
    cPtr->src = Mat(rows, cols, CV_8U, bgrPtr);
    cPtr->Run(qualityLevel);
    return (int*)&cPtr->points[0];
}







// https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
// https://docs.opencv.org/trunk/d1/d1d/tutorial_histo3D.html
// When run in VB.Net the histogram output has rows = -1 and cols = -1
// The rows and cols are both -1 so I had assumed there was a bug but the data is accessible with the at method.
// If you attempt the same access to the data in managed code, it does not work (AFAIK).
extern "C" __declspec(dllexport)
float* Hist3Dcolor_Run(int* bgrPtr, int rows, int cols, int bins)
{
    float hRange[] = { 0, 256 }; // ranges are exclusive in the top of the range, hence 256
    const float* range[] = { hRange, hRange, hRange };
    int hbins[] = { bins, bins, bins };
    int channels[] = { 0, 1, 2 };
    Mat src = Mat(rows, cols, CV_8UC3, bgrPtr);

    static Mat histogram;
    calcHist(&src, 1, channels, Mat(), histogram, 3, hbins, range);

    return (float*)histogram.data;
}




static Mat newImage;

// https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
// https://docs.opencv.org/trunk/d1/d1d/tutorial_histo3D.html
// When run in VB.Net the histogram output has rows = -1 and cols = -1
// The rows and cols are both -1 so I had assumed there was a bug but the data is accessible with the at method.
// If you attempt the same access to the data in managed code, it does not work (AFAIK).
extern "C" __declspec(dllexport)
uchar* BackProjectBGR_Run(int* bgrPtr, int rows, int cols, int bins, float threshold)
{
    Mat input = Mat(rows, cols, CV_8UC3, bgrPtr);
    float hRange0[] = { 0, 256 };
    float hRange1[] = { 0, 256 };
    float hRange2[] = { 0, 256 };
    const float* range[] = { hRange0, hRange1, hRange2 };
    int hbins[] = { bins, bins, bins };
    int channels[] = { 0, 1, 2 };

    static Mat histogram;
    calcHist(&input, 1, channels, Mat(), histogram, 3, hbins, range); // for 3D histograms, all 3 bins must be equal.

    float* hist = (float*)histogram.data;
    for (int i = 0; i < bins * bins * bins; i++)
    {
        if (hist[i] < threshold) hist[i] = 0;
    }

    calcBackProject(&input, 1, channels, histogram, newImage, range);

    return (uchar*)newImage.data;
}





extern "C" __declspec(dllexport)
uchar* Hist3Dcloud_Run(int* inputPtr, int rows, int cols, int bins,
    float minX, float minY, float minZ,
    float maxX, float maxY, float maxZ)
{
    Mat input = Mat(rows, cols, CV_32FC3, inputPtr);
    float hRange0[] = { float(minX), float(maxX) };
    float hRange1[] = { float(minY), float(maxY) };
    float hRange2[] = { float(minZ), float(maxZ) };
    const float* range[] = { hRange0, hRange1, hRange2 };
    int hbins[] = { bins, bins, bins };
    int channel[] = { 0, 1, 2 };

    static Mat histogram;
    calcHist(&input, 1, channel, Mat(), histogram, 3, hbins, range, true, false); // for 3D histograms, all 3 bins must be equal.

    return (uchar*)histogram.data;
}








static Mat mask;

extern "C" __declspec(dllexport)
uchar* BackProjectCloud_Run(int* inputPtr, int rows, int cols, int bins, float threshold,
    float minX, float minY, float minZ,
    float maxX, float maxY, float maxZ)
{
    Mat input = Mat(rows, cols, CV_32FC3, inputPtr);
    float hRange0[] = { float(minX), float(maxX) };
    float hRange1[] = { float(minY), float(maxY) };
    float hRange2[] = { float(minZ), float(maxZ) };
    const float* range[] = { hRange0, hRange1, hRange2 };
    int hbins[] = { bins, bins, bins };
    int channels[] = { 0, 1, 2 };

    static Mat histogram;
    calcHist(&input, 1, channels, Mat(), histogram, 3, hbins, range); // for 3D histograms, all 3 bins must be equal.

    float* hist = (float*)histogram.data;
    for (int i = 0; i < bins * bins * bins; i++)
    {
        if (hist[i] < threshold) hist[i] = 0; else hist[i] = 255; // building a mask
    }

    Mat mask32f;
    calcBackProject(&input, 1, channels, histogram, mask32f, range);
    mask32f.convertTo(mask, CV_8U);

    return (uchar*)mask.data;
}






class Hist_1D
{
private:
public:
    Mat src;
    Mat histogram;
    Hist_1D() {}
    void RunCPP(int bins) {
        float hRange[] = { 0, 256 };
        int hbins[] = { bins };
        const float* range[] = { hRange };
        calcHist(&src, 1, { 0 }, Mat(), histogram, 1, hbins, range, true, false);
    }
};
extern "C" __declspec(dllexport)
Hist_1D* Hist_1D_Open() {
    Hist_1D* cPtr = new Hist_1D();
    return cPtr;
}
extern "C" __declspec(dllexport)
float Hist_1D_Sum(Hist_1D* cPtr) {
    Scalar count = sum(cPtr->histogram);
    return count[0];
}
extern "C" __declspec(dllexport)
void Hist_1D_Close(Hist_1D* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* Hist_1D_RunCPP(Hist_1D* cPtr, int* dataPtr, int rows, int cols, int bins)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP(bins);
    return (int*)cPtr->histogram.data;
}






// http://man.hubwiz.com/docset/OpenCV.docset/Contents/Resources/Documents/d9/dde/samples_2cpp_2kmeans_8cpp-example.html

Scalar colorTab[] =
{
    Scalar(0, 0, 255),
    Scalar(0,255,0),
    Scalar(255,100,100),
    Scalar(255,0,255),
    Scalar(0,255,255)
};

class KMeans_MultiGaussian
{
private:
    RNG rng;
public:
    Mat dst;
    KMeans_MultiGaussian() { rng = rng(12345); }
    const int MAX_CLUSTERS = 5;

    void RunCPP() {
        int k, clusterCount = rng.uniform(2, MAX_CLUSTERS + 1);
        int i, sampleCount = rng.uniform(1, 1001);
        Mat points(sampleCount, 1, CV_32FC2), labels;

        clusterCount = MIN(clusterCount, sampleCount);
        std::vector<Point2f> centers;

        /* generate random sample from multigaussian distribution */
        for (k = 0; k < clusterCount; k++)
        {
            Point center;
            center.x = rng.uniform(0, dst.cols);
            center.y = rng.uniform(0, dst.rows);
            Mat pointChunk = points.rowRange(k * sampleCount / clusterCount,
                k == clusterCount - 1 ? sampleCount :
                (k + 1) * sampleCount / clusterCount);
            rng.fill(pointChunk, RNG::NORMAL, Scalar(center.x, center.y), Scalar(dst.cols * 0.05, dst.rows * 0.05));
        }

        randShuffle(points, 1, &rng);

        double compactness = kmeans(points, clusterCount, labels,
            TermCriteria(TermCriteria::EPS + TermCriteria::COUNT, 10, 1.0),
            3, KMEANS_PP_CENTERS, centers);

        for (i = 0; i < sampleCount; i++)
        {
            int clusterIdx = labels.at<int>(i);
            Point ipt = points.at<Point2f>(i);
            circle(dst, ipt, 2, colorTab[clusterIdx], FILLED, LINE_AA);
        }
        for (i = 0; i < int(centers.size()); ++i)
        {
            Point2f c = centers[i];
            circle(dst, c, 40, colorTab[i], 1, LINE_AA);
        }
    }
};

extern "C" __declspec(dllexport)
KMeans_MultiGaussian* KMeans_MultiGaussian_Open() {
    KMeans_MultiGaussian* cPtr = new KMeans_MultiGaussian();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* KMeans_MultiGaussian_Close(KMeans_MultiGaussian* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* KMeans_MultiGaussian_RunCPP(KMeans_MultiGaussian* cPtr, int rows, int cols)
{
    cPtr->dst = Mat(rows, cols, CV_8UC3);
    cPtr->dst.setTo(0);
    cPtr->RunCPP();
    return (int*)cPtr->dst.data;
}







class Kmeans_Simple
{
private:
public:
    Mat src, dst;
    Kmeans_Simple() {}
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
Kmeans_Simple* Kmeans_Simple_Open() {
    Kmeans_Simple* cPtr = new Kmeans_Simple();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Kmeans_Simple_Close(Kmeans_Simple* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Kmeans_Simple_RunCPP(Kmeans_Simple* cPtr, int* dataPtr, int rows, int cols, float minVal, float maxVal)
{
    cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
    cPtr->dst = Mat(rows, cols, CV_8UC3);
    cPtr->RunCPP(minVal, maxVal);
    return (int*)cPtr->dst.data;
}





extern "C" __declspec(dllexport)
void MinTriangle_Run(float* inputPoints, int count, float* outputTriangle)
{
    Mat input(count, 1, CV_32FC2, inputPoints);
    vector<Point2f> triangle;
    minEnclosingTriangle(input, triangle);
    for (int i = 0; i < 3; ++i)
    {
        outputTriangle[i * 2 + 0] = triangle.at(i).x;
        outputTriangle[i * 2 + 1] = triangle.at(i).y;
    }
}





class Sort_MLPrepTest
{
private:
public:
    Mat src, dst;
    Sort_MLPrepTest() {}
    void Run() {
        dst = Mat(src.rows, src.cols, CV_32FC2);
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                int gray = src.at<unsigned char>(y, x);
                dst.at<Point2f>(y, x) = Point2f(float(gray), float(y));
            }
        }
    }
};

extern "C" __declspec(dllexport) Sort_MLPrepTest* Sort_MLPrepTest_Open() { Sort_MLPrepTest* cPtr = new Sort_MLPrepTest(); return cPtr; }
extern "C" __declspec(dllexport) int* Sort_MLPrepTest_Close(Sort_MLPrepTest* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int* Sort_MLPrepTest_Run(Sort_MLPrepTest* cPtr, int* grayPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8U, grayPtr);
    cPtr->Run();
    return (int*)cPtr->dst.data;
}





class MSER_Interface
{
private:
public:
    Mat src, dst;
    Ptr<MSER> mser;
    vector<Rect> containers;
    vector<Point> floodPoints;
    vector<int> maskCounts;
    vector<vector<Point>> regions;
    vector<Rect> boxes;
    MSER_Interface(int delta, int minArea, int maxArea, float maxVariation, float minDiversity, int maxEvolution, float areaThreshold,
        float minMargin, int edgeBlurSize, int pass2Setting)
    {
        mser = mser->create(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin, edgeBlurSize);
        mser->setPass2Only(pass2Setting);
    }
    void RunCPP() {
        mser->detectRegions(src, regions, boxes);

        multimap<int, int, greater<int>> sizeSorted;
        for (auto i = 0; i < regions.size(); i++)
        {
            sizeSorted.insert(make_pair(regions[i].size(), i));
        }

        int index = 1;
        maskCounts.clear();
        containers.clear();
        floodPoints.clear();
        dst.setTo(255);
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            Rect box = boxes[it->second];
            //Point center = Point(box.x + box.width / 2, box.y + box.height / 2);
            //int val = dst.at<uchar>(center.y, center.x);
            //if (val == 255)
            //{
            floodPoints.push_back(regions[it->second][0]);
            maskCounts.push_back((int)regions[it->second].size());
            for (Point pt : regions[it->second])
            {
                dst.at<uchar>(pt.y, pt.x) = index;
            }
            index++;
            containers.push_back(box);
            //}
        }
    }
};
extern "C" __declspec(dllexport)
MSER_Interface* MSER_Open(int delta, int minArea, int maxArea, float maxVariation, float minDiversity, int maxEvolution, float areaThreshold,
    float minMargin, int edgeBlurSize, int pass2Setting)
{
    MSER_Interface* cPtr = new MSER_Interface(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin,
        edgeBlurSize, pass2Setting);
    return cPtr;
}
extern "C" __declspec(dllexport)
void MSER_Close(MSER_Interface* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* MSER_Rects(MSER_Interface* cPtr)
{
    return (int*)&cPtr->containers[0];
}
extern "C" __declspec(dllexport)
int* MSER_FloodPoints(MSER_Interface* cPtr)
{
    return (int*)&cPtr->floodPoints[0];
}
extern "C" __declspec(dllexport)
int* MSER_MaskCounts(MSER_Interface* cPtr)
{
    return (int*)&cPtr->maskCounts[0];
}
extern "C" __declspec(dllexport)
int MSER_Count(MSER_Interface* cPtr)
{
    return (int)cPtr->containers.size();
}
extern "C" __declspec(dllexport)
int* MSER_RunCPP(MSER_Interface* cPtr, int* dataPtr, int rows, int cols, int channels)
{
    cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, dataPtr);
    cPtr->dst = Mat(rows, cols, CV_8UC1);
    cPtr->RunCPP();
    return (int*)cPtr->dst.data;
}






class PCA_Prep
{
private:
public:
    vector<Point3f>pcaList;
    Mat src;
    PCA_Prep() {}
    void Run() {
        pcaList.clear();
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                auto vec = src.at<Point3f>(y, x);
                if (vec.z > 0) pcaList.push_back(vec);
            }
        }
    }
};

extern "C" __declspec(dllexport) PCA_Prep* PCA_Prep_Open() { PCA_Prep* cPtr = new PCA_Prep(); return cPtr; }
extern "C" __declspec(dllexport) int* PCA_Prep_Close(PCA_Prep* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int PCA_Prep_GetCount(PCA_Prep* cPtr) { return int(cPtr->pcaList.size()); }
extern "C" __declspec(dllexport) int* PCA_Prep_Run(PCA_Prep* cPtr, int* pointCloudData, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_32FC3, pointCloudData);
    cPtr->Run();
    return (int*)cPtr->pcaList.data();
}







class PlotOpenCV
{
private:
public:
    Mat src, srcX, srcY, dst;
    vector<Point>floodPoints;

    PlotOpenCV() {}
    void RunCPP() {
        Mat result;
        Ptr<plot::Plot2d> plot = plot::Plot2d::create(srcX, srcY);
        plot->setInvertOrientation(true);
        plot->setShowText(false);
        plot->setShowGrid(false);
        plot->setPlotBackgroundColor(Scalar(255, 200, 200));
        plot->setPlotLineColor(Scalar(255, 0, 0));
        plot->setPlotLineWidth(2);
        plot->render(result);
        resize(result, dst, dst.size());
    }
};

extern "C" __declspec(dllexport) PlotOpenCV* PlotOpenCV_Open() { PlotOpenCV* cPtr = new PlotOpenCV(); return cPtr; }
extern "C" __declspec(dllexport) void PlotOpenCV_Close(PlotOpenCV* cPtr) { delete cPtr; }

// https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
extern "C" __declspec(dllexport)
int* PlotOpenCV_Run(PlotOpenCV* cPtr, double* inX, double* inY, int len, int rows, int cols)
{
    cPtr->dst.setTo(0);
    cPtr->srcX = Mat(1, len, CV_64F, inX);
    cPtr->srcY = Mat(1, len, CV_64F, inY);
    cPtr->dst = Mat(rows, cols, CV_8UC3);
    cPtr->RunCPP();
    return (int*)cPtr->dst.data;
}






class Random_PatternGenerator
{
private:
public:
    Mat pattern;
    Random_PatternGenerator() {}
    void Run() {}
};

extern "C" __declspec(dllexport)
Random_PatternGenerator* Random_PatternGenerator_Open() {
    Random_PatternGenerator* Random_PatternGeneratorPtr = new Random_PatternGenerator();
    return Random_PatternGeneratorPtr;
}

extern "C" __declspec(dllexport)
int* Random_PatternGenerator_Close(Random_PatternGenerator* rPtr)
{
    delete rPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Random_PatternGenerator_Run(Random_PatternGenerator* rPtr, int rows, int cols)
{
    randpattern::RandomPatternGenerator generator(cols, rows);
    generator.generatePattern();
    rPtr->pattern = generator.getPattern();
    return (int*)rPtr->pattern.data;
}








// https://en.cppreference.com/w/cpp/numeric/random/discrete_distribution
class Random_DiscreteDistribution
{
private:
    std::random_device rd;
public:
    Mat discrete;
    Random_DiscreteDistribution() {}
    void Run()
    {
        std::mt19937 gen(rd());
        std::map<int, int> m;
        std::discrete_distribution<> d({ 40, 10, 10, 40 });
        for (int n = 0; n < 10000; ++n) {
            ++m[d(gen)];
        }
        for (auto p : m) {
            std::cout << p.first << " generated " << p.second << " times\n";
        }
    }
};

extern "C" __declspec(dllexport)
Random_DiscreteDistribution* Random_DiscreteDistribution_Open() {
    Random_DiscreteDistribution* cPtr = new Random_DiscreteDistribution();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Random_DiscreteDistribution_Close(Random_DiscreteDistribution* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Random_DiscreteDistribution_Run(Random_DiscreteDistribution* cPtr, int rows, int cols)
{
    cPtr->discrete = Mat(rows, cols, CV_32F, 0);
    return (int*)cPtr->discrete.data;
}







class RedColor_FindCells
{
private:
public:
    vector <int> bricks;
    RedColor_FindCells() {}
    void RunCPP(Mat src)
    {
        bricks.clear();
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                auto val = src.at<unsigned char>(y, x);
                if (count(bricks.begin(), bricks.end(), val) == 0)
                    bricks.push_back(val);
            }
        }
    }
};
extern "C" __declspec(dllexport)
RedColor_FindCells* RedColor_FindBricks_Open() {
    RedColor_FindCells* cPtr = new RedColor_FindCells();
    return cPtr;
}
extern "C" __declspec(dllexport)
void RedColor_FindBricks_Close(RedColor_FindCells* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport) int RedColor_FindBricks_TotalCount(RedColor_FindCells* cPtr) { return int(cPtr->bricks.size()); }
extern "C" __declspec(dllexport)
int* RedColor_FindBricks_RunCPP(RedColor_FindCells* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->RunCPP(Mat(rows, cols, CV_8UC1, dataPtr));
    if (cPtr->bricks.size() == 0) return 0;
    return (int*)&cPtr->bricks[0];
}






class Pixels_Vector
{
private:
public:
    Mat src, dst;
    Pixels_Vector() {}
    vector <Vec3b> pixelList;
    void RunCPP()
    {
        pixelList.clear();
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                auto val = src.at<Vec3b>(y, x);
                if (count(pixelList.begin(), pixelList.end(), val) == 0)
                    pixelList.push_back(val);
            }
        }
    }
};
extern "C" __declspec(dllexport)
Pixels_Vector* Pixels_Vector_Open() {
    Pixels_Vector* cPtr = new Pixels_Vector();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Pixels_Vector_Close(Pixels_Vector* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport) int* Pixels_Vector_Pixels(Pixels_Vector* cPtr)
{
    return (int*)&cPtr->pixelList[0];
}
extern "C" __declspec(dllexport)
int Pixels_Vector_RunCPP(Pixels_Vector* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
    cPtr->RunCPP();
    return (int)cPtr->pixelList.size();
}







class SuperPixels
{
private:
public:
    Mat src, labels, dst;
    Ptr<ximgproc::SuperpixelSEEDS> seeds;
    int width, height, num_superpixels = 400, num_levels = 4, prior = 2;
    SuperPixels() {}
    void Run()
    {
        Mat hsv;
        //cvtColor(src, hsv, ColorConversionCodes::COLOR_BGR2HSV);
        seeds->iterate(src);
        seeds->getLabelContourMask(dst, false);
        seeds->getLabels(labels);
    }
};

extern "C" __declspec(dllexport)
SuperPixels* SuperPixel_Open(int _width, int _height, int _num_superpixels, int _num_levels, int _prior)
{
    SuperPixels* spPtr = new SuperPixels();
    spPtr->width = _width;
    spPtr->height = _height;
    spPtr->num_superpixels = _num_superpixels;
    spPtr->num_levels = _num_levels;
    spPtr->prior = _prior;
    spPtr->seeds = ximgproc::createSuperpixelSEEDS(_width, _height, 3, _num_superpixels, _num_levels, _prior);
    spPtr->labels = Mat(spPtr->height, spPtr->width, CV_32S);
    return spPtr;
}

extern "C" __declspec(dllexport)
int* SuperPixel_Close(SuperPixels* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* SuperPixel_GetLabels(SuperPixels* cPtr)
{
    return (int*)cPtr->labels.data;
}

extern "C" __declspec(dllexport)
int* SuperPixel_Run(SuperPixels* cPtr, int* srcPtr)
{
    cPtr->src = Mat(cPtr->height, cPtr->width, CV_8UC3, srcPtr);
    cPtr->Run();
    return (int*)cPtr->dst.data;
}






// Parameters for Kalman Filter
#define Q1 0.004
#define R1 0.5









// https://stackoverflow.com/questions/22654770/creating-vignette-filter-in-opencv
class Vignetting_CC
{
private:
public:
    Mat src, dst;
    Vignetting_CC() {}
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
        double maxDis = radius * dist(0, 0, centerX, centerY);
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
                }
                else {
                    dst.at<Vec3b>(y, x)[0] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[0]) * temp);
                    dst.at<Vec3b>(y, x)[1] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[1]) * temp);
                    dst.at<Vec3b>(y, x)[2] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[2]) * temp);
                }
            }
        }
    }
};

extern "C" __declspec(dllexport)
Vignetting_CC* Vignetting_Open() {
    Vignetting_CC* cPtr = new Vignetting_CC();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Vignetting_Close(Vignetting_CC* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Vignetting_RunCPP(Vignetting_CC* cPtr, int* dataPtr, int rows, int cols, double radius, double centerX, double centerY, bool removal)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
    cPtr->RunCPP(radius, centerX, centerY, removal);
    return (int*)cPtr->dst.data;
}







class WarpModel
{
private:
public:
    int warpMode = MOTION_EUCLIDEAN;
    double termination_eps = 1e-5;
    int number_of_iterations = 50;
    TermCriteria criteria = TermCriteria(TermCriteria::COUNT + TermCriteria::EPS, number_of_iterations, termination_eps);
    Mat warpMatrix;
    WarpModel() {}
    void Run(Mat src1, Mat src2) {
        if (warpMode != MOTION_HOMOGRAPHY)
            warpMatrix = Mat::eye(2, 3, CV_32F);
        else
            warpMatrix = Mat::eye(3, 3, CV_32F);
        findTransformECC(src1, src2, warpMatrix, warpMode, criteria); // wish this were available in C#/VB.Net?
    }
};

extern "C" __declspec(dllexport)
WarpModel* WarpModel_Open() {
    WarpModel* cPtr = new WarpModel();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* WarpModel_Close(WarpModel* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* WarpModel_Run(WarpModel* cPtr, int* src1Ptr, int* src2Ptr, int rows, int cols, int channels, int warpMode)
{
    Mat src1 = Mat(rows, cols, CV_8UC1, src1Ptr);
    Mat src2 = Mat(rows, cols, CV_8UC1, src2Ptr);
    cPtr->warpMode = warpMode; // don't worry - the enumerations are identical...
    cPtr->Run(src1, src2);
    return (int*)cPtr->warpMatrix.data;
}






// https://blog.csdn.net/just_sort/article/details/85982871
class WhiteBalance
{
private:
public:
    Mat src, output;
    WhiteBalance() {}

    void Run(float thresholdVal)
    {
        int row = src.rows, col = src.cols;
        int HistRGB[768] = { 0 };
        int MaxVal = 0;
        for (int i = 0; i < row; i++) {
            for (int j = 0; j < col; j++) {
                int b = int(src.at<Vec3b>(i, j)[0]);
                int g = int(src.at<Vec3b>(i, j)[1]);
                int r = int(src.at<Vec3b>(i, j)[2]);
                MaxVal = max(MaxVal, b);
                MaxVal = max(MaxVal, g);
                MaxVal = max(MaxVal, r);
                int sum = b + g + r;
                HistRGB[sum]++;
            }
        }
        int Threshold = 0;
        int sum = 0;
        for (int i = 767; i >= 0; i--) {
            sum += HistRGB[i];
            if (sum > int(row * col) * thresholdVal) {
                Threshold = i;
                break;
            }
        }

        int AvgB = 0;
        int AvgG = 0;
        int AvgR = 0;
        int cnt = 0;
        for (int i = 0; i < row; i++) {
            for (int j = 0; j < col; j++) {
                int sumP = src.at<Vec3b>(i, j)[0] + src.at<Vec3b>(i, j)[1] + src.at<Vec3b>(i, j)[2];
                if (sumP > Threshold) {
                    AvgB += src.at<Vec3b>(i, j)[0];
                    AvgG += src.at<Vec3b>(i, j)[1];
                    AvgR += src.at<Vec3b>(i, j)[2];
                    cnt++;
                }
            }
        }
        if (cnt > 0)
        {
            AvgB /= cnt;
            AvgG /= cnt;
            AvgR /= cnt;
            for (int i = 0; i < row; i++) {
                for (int j = 0; j < col; j++) {
                    if (AvgB == 0 || AvgG == 0 || AvgR == 0) continue;
                    int Blue = src.at<Vec3b>(i, j)[0] * MaxVal / AvgB;
                    int Green = src.at<Vec3b>(i, j)[1] * MaxVal / AvgG;
                    int Red = src.at<Vec3b>(i, j)[2] * MaxVal / AvgR;
                    if (Red > 255) Red = 255; else if (Red < 0) Red = 0;
                    if (Green > 255) Green = 255; else if (Green < 0) Green = 0;
                    if (Blue > 255) Blue = 255; else if (Blue < 0) Blue = 0;
                    output.at<Vec3b>(i, j)[0] = Blue;
                    output.at<Vec3b>(i, j)[1] = Green;
                    output.at<Vec3b>(i, j)[2] = Red;
                }
            }
        }
    }
};

extern "C" __declspec(dllexport)
WhiteBalance* WhiteBalance_Open(float ppx, float ppy, float fx, float fy)
{
    WhiteBalance* cPtr = new WhiteBalance();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* WhiteBalance_Close(WhiteBalance* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* WhiteBalance_Run(WhiteBalance* cPtr, int* rgb, int rows, int cols, float thresholdVal)
{
    cPtr->output = Mat(rows, cols, CV_8UC3);
    cPtr->src = Mat(rows, cols, CV_8UC3, rgb);
    cPtr->Run(thresholdVal);
    return (int*)cPtr->output.data;
}






class ExPhoto_OilPaint
{
private:
public:
    Mat src, dst;
    ExPhoto_OilPaint() {}
    void Run(int size, int dynRatio, int colorCode)
    {
        xphoto::oilPainting(src, dst, size, dynRatio, colorCode);
    }
};

extern "C" __declspec(dllexport)
ExPhoto_OilPaint* ExPhoto_OilPaint_Open()
{
    ExPhoto_OilPaint* cPtr = new ExPhoto_OilPaint();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* ExPhoto_OilPaint_Close(ExPhoto_OilPaint* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* ExPhoto_OilPaint_Run(ExPhoto_OilPaint* cPtr, int* imagePtr, int rows, int cols, int size, int dynRatio, int colorCode)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, imagePtr);
    cPtr->Run(size, dynRatio, colorCode);
    return (int*)cPtr->dst.data;
}






class ExPhoto_Inpaint
{
private:
public:
    Mat src, dst;
    ExPhoto_Inpaint() {}
    void Run(Mat mask, int iType)
    {
        dst.setTo(0);
        //xphoto::inpaint(src, mask, dst, iType);
    }
};

extern "C" __declspec(dllexport)
ExPhoto_Inpaint* ExPhoto_Inpaint_Open()
{
    ExPhoto_Inpaint* cPtr = new ExPhoto_Inpaint();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* ExPhoto_Inpaint_Close(ExPhoto_Inpaint* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* ExPhoto_Inpaint_Run(ExPhoto_Inpaint* cPtr, int* imagePtr, int* maskPtr, int rows, int cols, int iType)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, imagePtr);
    cPtr->dst = Mat(rows, cols, CV_8UC3);
    Mat mask = Mat(rows, cols, CV_8UC1, maskPtr);
    cPtr->Run(mask, iType);
    return (int*)cPtr->dst.data;
}




cppTask* ctask;

class Random_Basics_CC : public CPP_Parent {
public:
    vector<Point2f> pointList;
    Rect range;
    int sizeRequest = 10;

    Random_Basics_CC() : CPP_Parent() {
        range = Rect(0, 0, dst2.cols, dst2.rows);
        desc = "Create a uniform random mask with a specified number of pixels.";
    }

    void Run(Mat src) {
        pointList.clear();
        if (!ctask->paused) {
            while (pointList.size() < sizeRequest) {
                pointList.push_back(Point2f(range.x + float((rand() % range.width)), range.y + float((rand() % range.height))));
            }

            dst2.setTo(0);
            for (Point2f pt : pointList) {
                circle(dst2, pt, ctask->DotSize, Scalar(0, 255, 255), -1, ctask->lineType, 0);
            }
        }
    }
};






class Random_Enumerable_CC : public CPP_Parent {
public:
    int sizeRequest = 100;
    Rect range;
    vector<Point2f> pointList;

    Random_Enumerable_CC() : CPP_Parent() {
        desc = "Create an enumerable list of points using a lambda function";
    }

    void Run(Mat src) {
        random_device rd;
        mt19937 gen(rd());  // Mersenne Twister engine for randomness
        uniform_int_distribution<> dist_width(0, dst2.cols - 1);  // Ensure width is within bounds
        uniform_int_distribution<> dist_height(0, dst2.rows - 1); // Ensure height is within bounds

        pointList.clear();
        for (int i = 0; i < sizeRequest; i++) {
            pointList.push_back(Point2f(dist_width(gen), dist_height(gen)));
        }

        dst2 = Mat::zeros(dst2.size(), dst2.type());  // Set dst2 to black
        for (const Point2f& pt : pointList) {
            //circle(dst2, pt, ctask->DotSize, Scalar(0, 255, 255), -1, ctask->lineType, 0);
        }
    }
};




class OEX_PointsClassifier
{
private:
public:
    vector<Point2f>trainedPoints;
    vector<int> trainedPointsMarkers;
    Mat dst, img, samples;
    vector<Vec3b>  classColors{ Vec3b(255, 0, 0), Vec3b(0, 255, 0) };
    Mat inputPoints;
    Ptr<NormalBayesClassifier> NBC = cv::ml::NormalBayesClassifier::create();
    Ptr<KNearest> KNN = cv::ml::KNearest::create();
    Ptr<SVM> SVM = cv::ml::SVM::create();
    Ptr<DTrees> DTR = cv::ml::DTrees::create();
    Ptr<Boost> BTR = cv::ml::Boost::create();
    Ptr<RTrees> RF = cv::ml::RTrees::create();
    Ptr<ANN_MLP> ANN = cv::ml::ANN_MLP::create();
    Ptr<EM> EM = cv::ml::EM::create();
    OEX_PointsClassifier() {}

    void OEX_Setup(int count, int rows, int cols) {
        inputPoints = Mat(rows * cols, 2, CV_32F);
        Point2f pt;
        int index = 0;
        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < cols; x++) {
                pt.x = (float)x;
                pt.y = (float)y;
                inputPoints.at<float>(index++) = pt.x;
                inputPoints.at<float>(index++) = pt.y;
            }
        }
        Random_Enumerable_CC* random = new Random_Enumerable_CC();
        random->sizeRequest = count;

        random->range = Rect(0, 0, cols * 3 / 4, rows * 3 / 4);
        random->dst2 = Mat(rows, cols, CV_8UC3);
        ctask = new cppTask(rows, cols);
        ctask->DotSize = 1;
        random->Run(Mat());

        trainedPoints = random->pointList;
        trainedPointsMarkers.clear();
        for (int i = 0; i < int(trainedPoints.size()); i++)
            trainedPointsMarkers.push_back(0);

        random->range = Rect(cols / 4, rows / 4, cols * 3 / 4, rows * 3 / 4);
        random->Run(Mat());
        for (int i = 0; i < int(random->pointList.size()); i++)
        {
            trainedPoints.push_back(random->pointList[i]);
            trainedPointsMarkers.push_back(1);
        }
    }

    void RunCPP(int methodIndex, int reset) {
        samples = Mat(trainedPoints).reshape(1, (int)trainedPoints.size());
        auto trainInput = TrainData::create(samples, ROW_SAMPLE, Mat(trainedPointsMarkers));

        dst.setTo(0);

        switch (methodIndex) {
        case 0:
        {
            if (reset) {
                NBC->train(trainInput);
            }
            NBC->predict(inputPoints, dst);
            break;
        }
        case 1:
        {
            if (reset) {
                KNN->setDefaultK(15);
                KNN->setIsClassifier(true);
                KNN = StatModel::train<KNearest>(trainInput);
            }
            KNN->predict(inputPoints, dst);
            break;
        }
        case 2:
        {
            if (reset) {
                SVM->setType(SVM::C_SVC);
                SVM->setKernel(SVM::POLY); //SVM::LINEAR;
                SVM->setDegree(0.5);
                SVM->setGamma(1);
                SVM->setCoef0(1);
                SVM->setNu(0.5);
                SVM->setP(0);
                SVM->setTermCriteria(TermCriteria(TermCriteria::MAX_ITER + TermCriteria::EPS, 1000, 0.01));
                int C = 1;
                SVM->setC(C);
                SVM->train(trainInput);
            }
            SVM->predict(inputPoints, dst);
            break;
        }
        case 3:
        {
            if (reset) {
                DTR->setMaxDepth(8);
                DTR->setMinSampleCount(2);
                DTR->setUseSurrogates(false);
                DTR->setCVFolds(0); // the number of cross-validation folds
                DTR->setUse1SERule(false);
                DTR->setTruncatePrunedTree(false);
                DTR->train(trainInput);
            }
            DTR->predict(inputPoints, dst);
            break;
        }
        case 4:
        {
            if (reset) {
                BTR->setBoostType(Boost::DISCRETE);
                BTR->setWeakCount(100);
                BTR->setWeightTrimRate(0.95);
                BTR->setMaxDepth(2);
                BTR->setUseSurrogates(false);
                BTR->setPriors(Mat());
                BTR->train(trainInput);
            }
            BTR->predict(inputPoints, dst);
            break;
        }
        case 5:
        {
            if (reset) {
                RF->setMaxDepth(4);
                RF->setMinSampleCount(2);
                RF->setRegressionAccuracy(0.f);
                RF->setUseSurrogates(false);
                RF->setMaxCategories(16);
                RF->setPriors(Mat());
                RF->setCalculateVarImportance(false);
                RF->setActiveVarCount(1);
                RF->setTermCriteria(TermCriteria(TermCriteria::MAX_ITER, 5, 0));
                RF->train(trainInput);
            }
            RF->predict(inputPoints, dst);
            break;
        }
        case 6:
        {
            if (reset)
            {
                Mat layer_sizes(1, 3, CV_32SC1, { 2, 5, 2 });
                Mat trainClasses = Mat::zeros((int)trainedPoints.size(), (int)classColors.size(), CV_32FC1);
                for (int i = 0; i < trainClasses.rows; i++)
                {
                    trainClasses.at<float>(i, trainedPointsMarkers[i]) = 1.f;
                }
                Ptr<TrainData> tdata = TrainData::create(samples, ROW_SAMPLE, trainClasses);

                ANN->setLayerSizes(layer_sizes);
                ANN->setActivationFunction(ANN_MLP::SIGMOID_SYM, 1, 1);
                ANN->setTermCriteria(TermCriteria(TermCriteria::MAX_ITER + TermCriteria::EPS, 300, FLT_EPSILON));
                ANN->setTrainMethod(ANN_MLP::BACKPROP, 0.001);
                ANN->train(tdata);
            }

            ANN->predict(inputPoints, dst);
            break;
        }
        case 7:
        {
            if (reset) {
                int i, j, nmodels = (int)classColors.size();
                vector<Ptr<cv::ml::EM> > em_models(nmodels);
                Mat modelSamples;

                for (i = 0; i < nmodels; i++)
                {
                    const int componentCount = 3;

                    modelSamples.release();
                    for (j = 0; j < samples.rows; j++)
                    {
                        if (trainedPointsMarkers[j] == i)
                            modelSamples.push_back(samples.row(j));
                    }

                    // learn models
                    if (!modelSamples.empty())
                    {
                        Ptr<cv::ml::EM> em = EM::create();
                        em->setClustersNumber(componentCount);
                        em->setCovarianceMatrixType(EM::COV_MAT_DIAGONAL);
                        em->trainEM(modelSamples, noArray(), noArray(), noArray());
                        em_models[i] = em;
                    }
                }

                // classify coordinate plane points using the bayes classifier, i.e.
                // y(x) = arg max_i=1_modelsCount likelihoods_i(x)
                Mat testSample(1, 2, CV_32FC1);
                Mat logLikelihoods(1, nmodels, CV_64FC1, Scalar(-DBL_MAX));

                for (int y = 0; y < img.rows; y++)
                {
                    for (int x = 0; x < img.cols; x++)
                    {
                        testSample.at<float>(0) = (float)x;
                        testSample.at<float>(1) = (float)y;

                        for (i = 0; i < nmodels; i++)
                        {
                            if (!em_models[i].empty())
                                logLikelihoods.at<double>(i) = em_models[i]->predict2(testSample, noArray())[0];
                        }
                        Point maxLoc;
                        minMaxLoc(logLikelihoods, 0, 0, 0, &maxLoc);
                        dst.at<Vec3b>(y, x) = classColors[maxLoc.x];
                    }
                }
            }
            break;
        }
        }

    }
};

extern "C" __declspec(dllexport)
OEX_PointsClassifier* OEX_Points_Classifier_Open() {
    OEX_PointsClassifier* cPtr = new OEX_PointsClassifier();
    return cPtr;
}
extern "C" __declspec(dllexport)
void OEX_Points_Classifier_Close(OEX_PointsClassifier* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* OEX_ShowPoints(OEX_PointsClassifier* cPtr, int imgRows, int imgCols, int radius)
{
    cPtr->img = Mat(imgRows, imgCols, CV_8UC3);
    cPtr->img.setTo(0);
    for (int i = 0; i < cPtr->trainedPoints.size(); i++)
    {
        auto color = cPtr->trainedPointsMarkers[i] == 0 ? Scalar(255, 255, 255) : Scalar(0, 255, 255);
        circle(cPtr->img, cPtr->trainedPoints[i], radius, color, -1, LINE_AA);
    }
    return (int*)cPtr->img.data;
}

extern "C" __declspec(dllexport)
int* OEX_Points_Classifier_RunCPP(OEX_PointsClassifier* cPtr, int count, int methodIndex, int imgRows, int imgCols, int reset)
{
    if (reset) cPtr->OEX_Setup(count, imgRows, imgCols);
    cPtr->RunCPP(methodIndex, reset);
    return (int*)cPtr->dst.data;
}











class Classifier_Bayesian
{
private:
public:
    vector<Vec3f>trainPoints;
    vector<int> trainMarkers;
    Mat inputPoints;
    vector<int> responses;
    Ptr<NormalBayesClassifier> NBC = cv::ml::NormalBayesClassifier::create();
    Classifier_Bayesian() {}

    void trainModel(Scalar* trainInput, int* trainResponse, int count) {
        trainPoints.clear();
        trainMarkers.clear();
        for (int i = 0; i < count; i++)
        {
            Vec3f vec(trainInput[i][0], trainInput[i][1], trainInput[i][2]);
            trainPoints.push_back(vec);
            trainMarkers.push_back(trainResponse[i]);
        }
        Mat samples = Mat(trainPoints).reshape(1, (int)trainPoints.size());
        auto inputSamples = TrainData::create(samples, ROW_SAMPLE, Mat(trainMarkers));
        NBC->train(inputSamples);
    }

    void RunCPP(Scalar* input, int count) {
        Mat testSample(1, 3, CV_32FC1);
        responses.clear();
        for (int i = 0; i < count; i++)
        {
            testSample.at<float>(0) = input[i][0];
            testSample.at<float>(1) = input[i][1];
            testSample.at<float>(2) = input[i][2];

            responses.push_back(NBC->predict(testSample));
        }
    }
};
extern "C" __declspec(dllexport)
Classifier_Bayesian* Classifier_Bayesian_Open() {
    Classifier_Bayesian* cPtr = new Classifier_Bayesian();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Classifier_Bayesian_Close(Classifier_Bayesian* cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
void Classifier_Bayesian_Train(Classifier_Bayesian* cPtr, Scalar* trainInput, int* trainResponse, int count)
{
    cPtr->trainModel(trainInput, trainResponse, count);
}

extern "C" __declspec(dllexport)
int* Classifier_Bayesian_RunCPP(Classifier_Bayesian* cPtr, Scalar* input, int count)
{
    cPtr->RunCPP(input, count);
    return (int*)&cPtr->responses[0];
}




class canvas {
public:
    bool setupQ;
    cv::Point origin;
    cv::Point corner;
    int minDims, maxDims;
    double scale;
    int rows, cols;
    cv::Mat img;

    void init(int minD, int maxD) {
        // Initialise the canvas with minimum and maximum rows and column sizes.
        minDims = minD; maxDims = maxD;
        origin = cv::Point(0, 0);
        corner = cv::Point(0, 0);
        scale = 1.0;
        rows = 0;
        cols = 0;
        setupQ = false;
    }

    void stretch(cv::Point2f min, cv::Point2f max) {
        // Stretch the canvas to include the points min and max.
        if (setupQ) {
            if (corner.x < max.x) { corner.x = (int)(max.x + 1.0); };
            if (corner.y < max.y) { corner.y = (int)(max.y + 1.0); };
            if (origin.x > min.x) { origin.x = (int)min.x; };
            if (origin.y > min.y) { origin.y = (int)min.y; };
        }
        else {
            origin = cv::Point((int)min.x, (int)min.y);
            corner = cv::Point((int)(max.x + 1.0), (int)(max.y + 1.0));
        }

        int c = (int)(scale * ((corner.x + 1.0) - origin.x));
        if (c < minDims) {
            scale = scale * (double)minDims / (double)c;
        }
        else {
            if (c > maxDims) {
                scale = scale * (double)maxDims / (double)c;
            }
        }
        int r = (int)(scale * ((corner.y + 1.0) - origin.y));
        if (r < minDims) {
            scale = scale * (double)minDims / (double)r;
        }
        else {
            if (r > maxDims) {
                scale = scale * (double)maxDims / (double)r;
            }
        }
        cols = (int)(scale * ((corner.x + 1.0) - origin.x));
        rows = (int)(scale * ((corner.y + 1.0) - origin.y));
        setupQ = true;
    }

    void stretch(vector<Point2f> pts)
    {   // Stretch the canvas so all the points pts are on the canvas.
        cv::Point2f min = pts[0];
        cv::Point2f max = pts[0];
        for (size_t i = 1; i < pts.size(); i++) {
            Point2f pnt = pts[i];
            if (max.x < pnt.x) { max.x = pnt.x; };
            if (max.y < pnt.y) { max.y = pnt.y; };
            if (min.x > pnt.x) { min.x = pnt.x; };
            if (min.y > pnt.y) { min.y = pnt.y; };
        };
        stretch(min, max);
    }

    void stretch(cv::RotatedRect box)
    {   // Stretch the canvas so that the rectangle box is on the canvas.
        cv::Point2f min = box.center;
        cv::Point2f max = box.center;
        cv::Point2f vtx[4];
        box.points(vtx);
        for (int i = 0; i < 4; i++) {
            cv::Point2f pnt = vtx[i];
            if (max.x < pnt.x) { max.x = pnt.x; };
            if (max.y < pnt.y) { max.y = pnt.y; };
            if (min.x > pnt.x) { min.x = pnt.x; };
            if (min.y > pnt.y) { min.y = pnt.y; };
        }
        stretch(min, max);
    }

    void drawEllipseWithBox(cv::RotatedRect box, cv::Scalar color, int lineThickness)
    {
        if (img.empty()) {
            stretch(box);
            img = cv::Mat::zeros(rows, cols, CV_8UC3);
        }

        box.center = scale * cv::Point2f(box.center.x - origin.x, box.center.y - origin.y);
        box.size.width = (float)(scale * box.size.width);
        box.size.height = (float)(scale * box.size.height);

        ellipse(img, box, color, lineThickness, LINE_AA);

        Point2f vtx[4];
        box.points(vtx);
        for (int j = 0; j < 4; j++) {
            line(img, vtx[j], vtx[(j + 1) % 4], color, lineThickness, LINE_AA);
        }
    }

    void drawPoints(vector<Point2f> pts, cv::Scalar color)
    {
        if (img.empty()) {
            stretch(pts);
            img = cv::Mat::zeros(rows, cols, CV_8UC3);
        }
        for (size_t i = 0; i < pts.size(); i++) {
            Point2f pnt = scale * cv::Point2f(pts[i].x - origin.x, pts[i].y - origin.y);
            img.at<cv::Vec3b>(int(pnt.y), int(pnt.x))[0] = (uchar)color[0];
            img.at<cv::Vec3b>(int(pnt.y), int(pnt.x))[1] = (uchar)color[1];
            img.at<cv::Vec3b>(int(pnt.y), int(pnt.x))[2] = (uchar)color[2];
        };
    }

    void drawLabels()
    {
        if (img.empty())
            img = cv::Mat::zeros(rows, cols, CV_8UC3);
        else
            img.setTo(0);
    }

};



inline static bool isGoodBox(const RotatedRect& box) {
    //size.height >= size.width awalys,only if the pts are on a line or at the same point,size.width=0
    return (box.size.height <= box.size.width * 30) && (box.size.width > 0);
}



class OEX_FitEllipse
{
private:
public:
    Mat src;
    OEX_FitEllipse() {}
    canvas paper;

    cv::Scalar white = Scalar(255, 255, 255);

    void RunCPP(int threshold, int fitType) {
        RotatedRect box;
        vector<vector<Point> > contours;
        Mat bimage = src >= threshold;

        findContours(bimage, contours, RETR_LIST, CHAIN_APPROX_NONE);

        paper.init(int(0.8 * MIN(bimage.rows, bimage.cols)), int(1.2 * MAX(bimage.rows, bimage.cols)));
        paper.stretch(cv::Point2f(0.0f, 0.0f), cv::Point2f((float)(bimage.cols + 2.0), (float)(bimage.rows + 2.0)));

        paper.drawLabels();

        int margin = 2;
        vector< vector<Point2f> > points;
        for (size_t i = 0; i < contours.size(); i++)
        {
            size_t count = contours[i].size();
            if (count < 6)
                continue;

            Mat pointsf;
            Mat(contours[i]).convertTo(pointsf, CV_32F);

            vector<Point2f>pts;
            for (int j = 0; j < pointsf.rows; j++) {
                Point2f pnt = Point2f(pointsf.at<float>(j, 0), pointsf.at<float>(j, 1));
                if ((pnt.x > margin && pnt.y > margin && pnt.x < bimage.cols - margin && pnt.y < bimage.rows - margin)) {
                    if (j % 20 == 0) {
                        pts.push_back(pnt);
                    }
                }
            }
            points.push_back(pts);
        }

        for (size_t i = 0; i < points.size(); i++)
        {
            vector<Point2f> pts = points[i];

            //At least 5 points can fit an ellipse
            if (pts.size() < 5) {
                continue;
            }

            Scalar color;
            switch (fitType) {
            case 0: //fitEllipseQ) {
                box = fitEllipse(pts);
                color = Scalar(255, 0, 0);
                break;
            case 1: // fitEllipseAMSQ) {
                box = fitEllipseAMS(pts);
                color = Scalar(0, 255, 0);
                break;
            case 2:  // fitEllipseDirectQ) {
                box = fitEllipseDirect(pts);
                color = Scalar(0, 0, 255);
                break;
            }
            if (isGoodBox(box)) paper.drawEllipseWithBox(box, color, 3);
            paper.drawPoints(pts, white);
        }
    }
};
extern "C" __declspec(dllexport)
OEX_FitEllipse* OEX_FitEllipse_Open() {
    OEX_FitEllipse* cPtr = new OEX_FitEllipse();
    return cPtr;
}
extern "C" __declspec(dllexport)
void OEX_FitEllipse_Close(OEX_FitEllipse* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* OEX_FitEllipse_RunCPP(OEX_FitEllipse* cPtr, int* dataPtr, int rows, int cols, int threshold, int fitType)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP(threshold, fitType);
    return (int*)cPtr->paper.img.data;
}






class ML_RemoveDups
{
private:
public:
    Mat src, dst;
    ML_RemoveDups() {}
    int index = 0;
    void Run() {
        index = 0;
        int lastVal = -1;
        if (src.type() == CV_32S)
        {
            dst = Mat(int(src.total()), 1, CV_32S);
            dst.setTo(0);
            for (int y = 0; y < src.rows; y++)
            {
                for (int x = 0; x < src.cols; x++)
                {
                    int val = src.at<int>(y, x);
                    if (val != lastVal)
                    {
                        dst.at<int>(index, 0) = val;
                        lastVal = val;
                        index++;
                    }
                }
            }
        }
        else
        {
            dst = Mat(int(src.total()), 1, CV_8U);
            dst.setTo(0);
            for (int y = 0; y < src.rows; y++)
            {
                for (int x = 0; x < src.cols; x++)
                {
                    int val = src.at<unsigned char>(y, x);
                    if (val != lastVal)
                    {
                        dst.at<unsigned char>(index, 0) = val;
                        lastVal = val;
                        index++;
                    }
                }
            }
        }
    }
};

extern "C" __declspec(dllexport) ML_RemoveDups* ML_RemoveDups_Open() { ML_RemoveDups* cPtr = new ML_RemoveDups(); return cPtr; }
extern "C" __declspec(dllexport) int ML_RemoveDups_GetCount(ML_RemoveDups* cPtr) { return cPtr->index - 1; }
extern "C" __declspec(dllexport) int* ML_RemoveDups_Close(ML_RemoveDups* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int* ML_RemoveDups_Run(ML_RemoveDups* cPtr, int* dataPtr, int rows, int cols, int type)
{
    cPtr->src = Mat(rows, cols, type, dataPtr);
    cPtr->Run();
    return (int*)cPtr->dst.data;
}






class Neighbors1
{
private:
public:
    Mat src, contour;
    vector<Point> nPoints;
    vector<uchar> cellData;
    void RunCPP()
    {
        nPoints.clear();
        cellData.clear();
        for (int y = 1; y < src.rows - 3; y++)
            for (int x = 1; x < src.cols - 3; x++)
            {
                vector<uchar> nabs;
                vector<uchar> ids = { 0, 0, 0, 0 };
                int index = 0;
                for (int yy = y; yy < y + 2; yy++)
                {
                    for (int xx = x; xx < x + 2; xx++)
                    {
                        uchar val = src.at<uchar>(yy, xx);
                        if (count(nabs.begin(), nabs.end(), val) == 0)
                        {
                            nabs.push_back(val);
                        }
                        ids[index++] = val;
                    }
                }
                if (nabs.size() > 2)
                {
                    nPoints.push_back(Point(x, y));
                    cellData.push_back(ids[0]);
                    cellData.push_back(ids[1]);
                    cellData.push_back(ids[2]);
                    cellData.push_back(ids[3]);
                }
            }
    }
};

extern "C" __declspec(dllexport)
Neighbors1* Neighbors1_Open() {
    Neighbors1* cPtr = new Neighbors1();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Neighbors1_Close(Neighbors1* cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* Neighbors1_CellData(Neighbors1* cPtr)
{
    return (int*)&cPtr->cellData[0];
}

extern "C" __declspec(dllexport)
int* Neighbors1_Points(Neighbors1* cPtr)
{
    return (int*)&cPtr->nPoints[0];
}

extern "C" __declspec(dllexport)
int Neighbors1_RunCPP(Neighbors1* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int)cPtr->nPoints.size();
}








class Neighbor2
{
private:
public:
    Mat src, contour;
    vector<Point> nPoints;
    void RunCPP()
    {
        nPoints.clear();
        for (int y = 1; y < src.rows - 2; y++)
            for (int x = 1; x < src.cols - 2; x++)
            {
                Point pt = Point(src.at<uchar>(y, x), src.at<uchar>(y, x - 1));
                if (pt.x == pt.y) continue;
                if (pt.x == 0 || pt.y == 0) continue;
                if (count(nPoints.begin(), nPoints.end(), pt) == 0)
                {
                    pt = Point(src.at<uchar>(y, x - 1), src.at<uchar>(y, x));
                    if (count(nPoints.begin(), nPoints.end(), pt) == 0) nPoints.push_back(pt);
                }
            }
    }
};

extern "C" __declspec(dllexport)
Neighbor2* Neighbor2_Open() {
    Neighbor2* cPtr = new Neighbor2();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Neighbor2_Close(Neighbor2* cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* Neighbor2_Points(Neighbor2* cPtr)
{
    return (int*)&cPtr->nPoints[0];
}

extern "C" __declspec(dllexport)
int Neighbor2_RunCPP(Neighbor2* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int)cPtr->nPoints.size();
}






class Neighbors
{
private:
public:
    Mat src;
    vector <Point> nabList;

    Neighbors() {}
    void checkPoint(Point pt)
    {
        if (pt.x != pt.y)
        {
            if (pt.x > pt.y) pt = Point(pt.y, pt.x);
            std::vector<Point>::iterator it = std::find(nabList.begin(), nabList.end(), pt);
            if (it == nabList.end()) nabList.push_back(pt);
        }
    }
    void RunCPP() {
        nabList.clear();
        for (int y = 1; y < src.rows; y++)
            for (int x = 1; x < src.cols; x++)
            {
                uchar val = src.at<uchar>(y, x);
                checkPoint(Point(src.at<uchar>(y, x - 1), val));
                checkPoint(Point(src.at<uchar>(y - 1, x), val));
            }
    }
};
extern "C" __declspec(dllexport) Neighbors* Neighbor_Open() { Neighbors* cPtr = new Neighbors(); return cPtr; }
extern "C" __declspec(dllexport) void Neighbor_Close(Neighbors* cPtr) { delete cPtr; }
extern "C" __declspec(dllexport) int* Neighbor_NabList(Neighbors* cPtr) { return (int*)&cPtr->nabList[0]; }
extern "C" __declspec(dllexport)
int Neighbor_RunCPP(Neighbors* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int)cPtr->nabList.size();
}







class Denoise_Basics
{
private:
public:
    Mat dst;
    int frameCount = 5;
    std::vector<Mat> frames;
    Denoise_Basics() {}
    void Run() {
        dst = frames.back();
        fastNlMeansDenoisingMulti(frames, dst, 2, 1);
    }
};

extern "C" __declspec(dllexport)
Denoise_Basics* Denoise_Basics_Open(int frameCount) {
    Denoise_Basics* cPtr = new Denoise_Basics();
    cPtr->frameCount = frameCount;
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Denoise_Basics_Close(Denoise_Basics* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Denoise_Basics_Run(Denoise_Basics* cPtr, int* bufferPtr, int rows, int cols)
{
    Mat src = Mat(rows, cols, CV_8UC1, bufferPtr);
    cPtr->frames.push_back(src.clone());
    if (cPtr->frames.size() < cPtr->frameCount)
    {
        return (int*)src.data;
    }
    else {
        cPtr->Run();
        cPtr->frames.pop_back();
    }
    return(int*)cPtr->dst.data;
}






class Denoise_Pixels
{
private:
public:
    Mat src;
    int edgeCountAfter, edgeCountBefore;
    Denoise_Pixels() {}
    void RunCPP() {
        edgeCountBefore = 0;
        for (int y = 0; y < src.rows; y++)
            for (int x = 1; x < src.cols - 1; x++)
            {
                int last = src.at<uchar>(y, x - 1);
                int curr = src.at<uchar>(y, x);
                if (last != curr)
                {
                    edgeCountBefore++;
                    if (last == src.at<uchar>(y, x + 1))
                        src.at<uchar>(y, x) = last;
                }
            }
        for (int y = 1; y < src.rows - 1; y++)
            for (int x = 0; x < src.cols; x++)
            {
                int last = src.at<uchar>(y - 1, x);
                int curr = src.at<uchar>(y, x);
                if (last != curr)
                    if (last == src.at<uchar>(y + 1, x))
                        src.at<uchar>(y, x) = last;
            }
        edgeCountAfter = 0;
        for (int y = 0; y < src.rows; y++)
            for (int x = 1; x < src.cols; x++)
            {
                int last = src.at<uchar>(y, x - 1);
                int curr = src.at<uchar>(y, x);
                if (last != curr) edgeCountAfter++;
            }
    }
};
extern "C" __declspec(dllexport)
Denoise_Pixels* Denoise_Pixels_Open() {
    Denoise_Pixels* cPtr = new Denoise_Pixels();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Denoise_Pixels_Close(Denoise_Pixels* cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int Denoise_Pixels_EdgeCountBefore(Denoise_Pixels* cPtr)
{
    return cPtr->edgeCountBefore;
}

extern "C" __declspec(dllexport)
int Denoise_Pixels_EdgeCountAfter(Denoise_Pixels* cPtr)
{
    return cPtr->edgeCountAfter;
}
extern "C" __declspec(dllexport)
int* Denoise_Pixels_RunCPP(Denoise_Pixels* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int*)cPtr->src.data;
}







class Denoise_SinglePixels
{
private:
public:
    Mat src;
    Denoise_SinglePixels() {}
    void RunCPP() {
        int kSize = 3;
        Rect r = Rect(0, 0, kSize, kSize);
        for (int y = 0; y < src.rows - kSize; y++)
        {
            for (int x = 0; x < src.cols - kSize; x++)
            {
                if (src.at<uchar>(y + 1, x + 1) != 0)
                {
                    r.x = x;
                    r.y = y;
                    if (countNonZero(src(r)) == 1) src(r).setTo(0);
                }
            }
        }
    }
};
extern "C" __declspec(dllexport)
Denoise_SinglePixels* Denoise_SinglePixels_Open() {
    Denoise_SinglePixels* cPtr = new Denoise_SinglePixels();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Denoise_SinglePixels_Close(Denoise_SinglePixels* cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* Denoise_SinglePixels_Run(Denoise_SinglePixels* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int*)cPtr->src.data;
}





class Edge_DiffX
{
private:
public:
    Mat src, dst;
    Edge_DiffX() {}
    void RunCPP() {
        if (dst.rows == 0) dst = Mat(src.rows, src.cols, CV_8U);
        dst.setTo(0);
        uchar* in = src.data;
        uchar* out = dst.data;
        for (int y = 0; y < src.rows; y++)
        {
            int segID = 0;
            for (int x = 0; x < src.cols - 1; x++)
            {
                int index = y * src.cols + x;
                int v1 = in[index];
                int v2 = in[index + 1];
                if (v1 == 0 || v2 == 0) continue;
                if (v1 == 0 && v2 != segID)
                {
                    out[index + 1] = 255;
                    segID = v2;
                    continue;
                }
                if (v1 == v2) continue;
                out[index] = 255;
                segID = v2;
            }
        }
    }
};
extern "C" __declspec(dllexport)
Edge_DiffX* Edge_DiffX_Open() {
    Edge_DiffX* cPtr = new Edge_DiffX();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Edge_DiffX_Close(Edge_DiffX* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* Edge_DiffX_RunCPP(Edge_DiffX* cPtr, int* dataPtr, int rows, int cols, int channels)
{
    cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int*)cPtr->dst.data;
}





class Edge_DiffY
{
private:
public:
    Mat src, dst;
    Edge_DiffY() {}
    void RunCPP() {
        if (dst.rows == 0) dst = Mat(src.rows, src.cols, CV_8U);
        dst.setTo(0);
        uchar* in = src.data;
        uchar* out = dst.data;
        for (int x = 0; x < src.cols; x++)
        {
            int segID = 0;
            for (int y = 0; y < src.rows - 1; y++)
            {
                int index = y * src.cols + x;
                int v1 = in[index];
                int v2 = in[index + src.cols];
                if (v1 == 0 || v2 == 0) continue;
                if (v1 == 0 && v2 != segID)
                {
                    out[index + src.cols] = 255;
                    segID = v2;
                    continue;
                }
                if (v1 == v2) continue;
                out[index] = 255;
                segID = v2;
            }
        }
    }
};
extern "C" __declspec(dllexport)
Edge_DiffY* Edge_DiffY_Open() {
    Edge_DiffY* cPtr = new Edge_DiffY();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Edge_DiffY_Close(Edge_DiffY* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* Edge_DiffY_RunCPP(Edge_DiffY* cPtr, int* dataPtr, int rows, int cols, int channels)
{
    cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int*)cPtr->dst.data;
}




class BGSubtract_BGFG
{
private:
public:
    Ptr<BackgroundSubtractor> algo;
    Mat src, fgMask;
    BGSubtract_BGFG() {}
    void Run(double learnRate) {
        algo->apply(src, fgMask, learnRate);
    }
};

extern "C" __declspec(dllexport)
BGSubtract_BGFG* BGSubtract_BGFG_Open(int currMethod) {
    BGSubtract_BGFG* cPtr = new BGSubtract_BGFG();
    if (currMethod == 0)      cPtr->algo = createBackgroundSubtractorGMG(20, 0.7);
    else if (currMethod == 1) cPtr->algo = createBackgroundSubtractorCNT();
    else if (currMethod == 2) cPtr->algo = createBackgroundSubtractorKNN();
    else if (currMethod == 3) cPtr->algo = createBackgroundSubtractorMOG();
    else if (currMethod == 4) cPtr->algo = createBackgroundSubtractorMOG2();
    else if (currMethod == 5) cPtr->algo = createBackgroundSubtractorGSOC();
    else if (currMethod == 6) cPtr->algo = createBackgroundSubtractorLSBP();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* BGSubtract_BGFG_Close(BGSubtract_BGFG* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* BGSubtract_BGFG_Run(BGSubtract_BGFG* cPtr, int* bgrPtr, int rows, int cols, int channels, double learnRate)
{
    cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, bgrPtr);
    cPtr->Run(learnRate);
    return (int*)cPtr->fgMask.data;
}





class BGSubtract_Synthetic
{
private:
public:
    Ptr<bgsegm::SyntheticSequenceGenerator> gen;
    Mat src, output, fgMask;
    BGSubtract_Synthetic() {}
    void Run() {
    }
};

extern "C" __declspec(dllexport)
BGSubtract_Synthetic* BGSubtract_Synthetic_Open(int* bgrPtr, int rows, int cols, LPSTR fgFilename, double amplitude, double magnitude,
    double wavespeed, double objectspeed)
{
    BGSubtract_Synthetic* cPtr = new BGSubtract_Synthetic();
    Mat bg = Mat(rows, cols, CV_8UC3, bgrPtr);
    Mat fg = imread(fgFilename, IMREAD_COLOR);
    resize(fg, fg, Size(10, 10)); // adjust the object size here...
    cPtr->gen = bgsegm::createSyntheticSequenceGenerator(bg, fg, amplitude, magnitude, wavespeed, objectspeed);
    cPtr->gen->getNextFrame(cPtr->output, cPtr->fgMask);
    return cPtr;
}

extern "C" __declspec(dllexport)
int* BGSubtract_Synthetic_Close(BGSubtract_Synthetic* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* BGSubtract_Synthetic_Run(BGSubtract_Synthetic* cPtr)
{
    cPtr->gen->getNextFrame(cPtr->output, cPtr->fgMask);
    return (int*)cPtr->output.data;
}




extern "C" __declspec(dllexport)
int* Corners_ShiTomasi(int* grayPtr, int rows, int cols, int blockSize, int apertureSize)
{
    static Mat dst;
    Mat gray = Mat(rows, cols, CV_8UC1, grayPtr);
    dst = Mat::zeros(gray.size(), CV_32FC1);
    /// Shi-Tomasi -- Using cornerMinEigenVal - can't access this from opencvSharp...
    cornerMinEigenVal(gray, dst, blockSize, apertureSize, BORDER_DEFAULT);
    return (int*)dst.data;
}








class Agast
{
private:
public:
    Mat src, dst;
    std::vector<Point2f> points;

    Agast() {}
    void Run(int threshold) {
        std::vector<KeyPoint> keypoints;
        static Ptr<AgastFeatureDetector> agastFD = AgastFeatureDetector::create(threshold, true, AgastFeatureDetector::OAST_9_16);
        agastFD->detect(src, keypoints);
        points.clear();
        for (KeyPoint kpt : keypoints)
        {
            points.push_back(Point2f(round(kpt.pt.x), round(kpt.pt.y)));
        }
    }
};

extern "C" __declspec(dllexport) int Agast_Count(Agast* cPtr)
{
    return (int)cPtr->points.size();
}
extern "C" __declspec(dllexport)
Agast* Agast_Open()
{
    Agast* cPtr = new Agast();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Agast_Close(Agast* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Agast_Run(Agast* cPtr, int* bgrPtr, int rows, int cols, int threshold)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, bgrPtr);
    cPtr->Run(threshold);
    return (int*)&cPtr->points[0];
}








#ifdef _DEBUG
const int cityMultiplier = 100;
const int puzzleMultiplier = 1;
#else
const int puzzleMultiplier = 1000;
const int cityMultiplier = 10000;
#endif

class CitySolver
{
public:
    std::vector<Point2f> cityPositions;
    std::vector<int> cityOrder;
    RNG rng;
    double currentTemperature = 100.0;
    char outMsg[512];
    int d0, d1, d2, d3;
private:

public:
    CitySolver()
    {
        rng = RNG(__rdtsc()); // When called in rapid succession, the rdtsc will prevent duplication when multi-threading...
        // rng = theRNG(); // To see the duplication problem with MT, uncomment this line.  Watch all the energy levels will be the same!
        currentTemperature = 100.0f;
    }
    /** Give energy value for a state of system.*/
    double energy() const
    {
        double e = 0;
        for (size_t i = 0; i < cityOrder.size(); i++)
        {
            e += norm(cityPositions[i] - cityPositions[cityOrder[i]]);
        }
        return e;
    }

    /** Function which change the state of system (random perturbation).*/
    void changeState()
    {
        d0 = rng.uniform(0, static_cast<int>(cityPositions.size()));
        d1 = cityOrder[d0];
        d2 = cityOrder[d1];
        d3 = cityOrder[d2];

        cityOrder[d0] = d2;
        cityOrder[d2] = d1;
        cityOrder[d1] = d3;
    }

    /** Function to reverse to the previous state.*/
    void reverseState()
    {
        cityOrder[d0] = d1;
        cityOrder[d1] = d2;
        cityOrder[d2] = d3;
    }
};

extern "C" __declspec(dllexport)
CitySolver* Annealing_Basics_Open(Point2f* cityPositions, int count)
{
    CitySolver* cPtr = new CitySolver();
    cPtr->cityPositions.assign(cityPositions, cityPositions + count);
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Annealing_Basics_Close(CitySolver* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
char* Annealing_Basics_Run(CitySolver* cPtr, int* cityOrder, int count)
{
    cPtr->cityOrder.assign(cityOrder, cityOrder + count);
    int changesApplied = ml::simulatedAnnealingSolver(*cPtr, cPtr->currentTemperature,
        cPtr->currentTemperature * 0.97, 0.99,
        cityMultiplier * count, &cPtr->currentTemperature, cPtr->rng);
    copy(cPtr->cityOrder.begin(), cPtr->cityOrder.end(), cityOrder);
    string msg = " changesApplied=" + to_string(changesApplied) + " temp=" + to_string(cPtr->currentTemperature) + " result = " + to_string(cPtr->energy());
    strcpy_s(cPtr->outMsg, msg.c_str());
    return cPtr->outMsg;
}




class EdgeLine_Image
{
private:
public:
    Mat src, dst;
    vector<Vec4f> lines;
    Ptr<EdgeDrawing> ed;
    EdgeLine_Image()
    {
        ed = createEdgeDrawing();
        ed->params.EdgeDetectionOperator = EdgeDrawing::SOBEL;
        ed->params.GradientThresholdValue = 38;
        ed->params.AnchorThresholdValue = 8;
    }
    void RunCPP(int lineWidth) {
        ed->detectEdges(src);
        ed->detectLines(lines);

        dst.setTo(0);
        for (size_t i = 0; i < lines.size(); i++)
        {
            Point2f p1 = Point2f(lines[i].val[0], lines[i].val[1]);
            Point2f p2 = Point2f(lines[i].val[2], lines[i].val[3]);
            line(dst, p1, p2, 255, lineWidth);
        }
    }
};

extern "C" __declspec(dllexport)
EdgeLine_Image* EdgeLine_Image_Open() {
    EdgeLine_Image* cPtr = new EdgeLine_Image();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeLine_Image_Close(EdgeLine_Image* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* EdgeLine_Image_RunCPP(EdgeLine_Image* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
    if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->RunCPP(lineWidth);
    return (int*)cPtr->dst.data;
}







class EdgeLine_Lines
{
private:
public:
    Mat src, dst;
    Ptr<EdgeLine_Image> eDraw;
    vector<Vec4f> lines;
    EdgeLine_Lines()
    {
        eDraw = new EdgeLine_Image();
    }
    void RunCPP(int lineWidth) {
        eDraw->ed->detectEdges(src);
        eDraw->ed->detectLines(lines);
    }
};

extern "C" __declspec(dllexport)
EdgeLine_Lines* EdgeLine_Lines_Open() {
    EdgeLine_Lines* cPtr = new EdgeLine_Lines();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeLine_Lines_Close(EdgeLine_Lines* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int EdgeLine_Lines_Count(EdgeLine_Lines* cPtr)
{
    return int(cPtr->lines.size());
}

extern "C" __declspec(dllexport)
int* EdgeLine_Lines_RunCPP(EdgeLine_Lines* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
    if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->RunCPP(lineWidth);
    return (int*)&cPtr->lines[0];
}









class EdgeLine
{
private:
public:
    Mat src, edgeMap, motionMask, histogram, dst8U;
    Ptr<EdgeLine_Image> eDraw;
    vector<Vec4f> lines;
    vector< vector<Point> > segments;
    vector< vector<Point> > edgeList;
    vector<Point> emptyList;
    size_t unChangedCount = 0;
    EdgeLine() 
    {
        eDraw = new EdgeLine_Image();
    }
    void RunCPP(int lineWidth) {
        dst8U.setTo(0);
        edgeList.clear();
        edgeList.push_back(emptyList); // placeholder for zeros...
        if (segments.size() > 0)
        {
            float hRange[] = { 0, (float)segments.size() };
            const float* range[] = { hRange };
            int hbins[] = { (int)segments.size() };

            calcHist(&edgeMap, 1, {0}, ~motionMask, histogram, 1, hbins, range);

            float* hData = (float*)histogram.data;
            for (size_t i = 1; i < segments.size(); i++)
            {
                if (hData[i] > 0) edgeList.push_back(segments[i]);
            }
        }
        unChangedCount = edgeList.size();

        segments.clear();
        eDraw->ed->detectEdges(src);
        eDraw->ed->detectLines(lines);
        segments = eDraw->ed->getSegments();

        edgeMap.setTo(0);
        for (size_t i = 0; i < segments.size(); i++)
        {
            const Point* pts = &segments[i][0];
            int n = (int)segments[i].size();
            float distance = sqrt((pts[0].x - pts[n - 1].x) * (pts[0].x - pts[n - 1].x) + (pts[0].y - pts[n - 1].y) * (pts[0].y - pts[n - 1].y));
            bool drawClosed = distance < 10;
            polylines(edgeMap, &pts, &n, 1, drawClosed, i + 1, lineWidth, LINE_4);
        }

        if (segments.size() == 0) return;
        float hRange[] = { 0, (float)segments.size()};
        const float* range[] = { hRange };
        int hbins[] = { (int)segments.size() };

        Mat tmp;
        bitwise_not(motionMask, tmp);
        edgeMap.setTo(0, tmp);

        calcHist(&edgeMap, 1, { 0 }, motionMask, histogram, 1, hbins, range);

        edgeMap.setTo(0);
        float* hData = (float*)histogram.data;
        for (size_t i = 1; i < segments.size(); i++)
        {
            if (hData[i] > 0) edgeList.push_back(segments[i - 1]);
        }

        segments.clear();
        for (size_t i = 1; i < edgeList.size(); i++)
        {
            const Point* pts = &edgeList[i][0];
            int n = (int)edgeList[i].size();
            float distance = sqrt((pts[0].x - pts[n - 1].x) * (pts[0].x - pts[n - 1].x) + (pts[0].y - pts[n - 1].y) * (pts[0].y - pts[n - 1].y));
            bool drawClosed = distance < 10;
            polylines(edgeMap, &pts, &n, 1, drawClosed, i + 1, lineWidth, LINE_4); // for the next iteration.
            polylines(dst8U, &pts, &n, 1, drawClosed, 255, lineWidth, LINE_4);
            segments.push_back(edgeList[i]);
        }
    }
};

extern "C" __declspec(dllexport)
EdgeLine* EdgeLine_Open() {
    EdgeLine* cPtr = new EdgeLine();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeLine_Close(EdgeLine* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int EdgeLine_GetEdgeLength(EdgeLine* cPtr)
{
    return (int)cPtr->edgeList.size();
}

extern "C" __declspec(dllexport)
int EdgeLine_GetSegCount(EdgeLine* cPtr)
{
    return (int)cPtr->segments.size();
}

extern "C" __declspec(dllexport)
int EdgeLine_UnchangedCount(EdgeLine* cPtr)
{
    return (int)cPtr->unChangedCount;
}

extern "C" __declspec(dllexport)
int* EdgeLine_RunCPP(EdgeLine* cPtr, int* dataPtr, int* motionMask, int rows, int cols, int lineWidth)
{
    if (cPtr->edgeMap.rows == 0)
    {
        cPtr->dst8U = Mat(rows, cols, CV_8U);
        cPtr->edgeMap = Mat(rows, cols, CV_32F);
        cPtr->edgeMap.setTo(0);
    }
    cPtr->motionMask = Mat(rows, cols, CV_8U, motionMask);
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->RunCPP(lineWidth);
    return (int*)cPtr->dst8U.data;
}







class RedCloudMaxDist
{
private:
public:
    Mat src, mask, maskCopy, result;
    vector<Rect>cellRects;
    vector<Point> floodPoints;
    vector<Point> maxList;

    RedCloudMaxDist() {}
    void RunCPP() {
        Rect rect;

        multimap<int, Point, greater<int>> sizeSorted;
        int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
        Point pt;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                if (mask.at<unsigned char>(y, x) == 0)
                {
                    pt = Point(x, y);
                    int count = floodFill(src, mask, pt, 255, &rect, 0, 0, 4 | floodFlag | (255 << 8));
                    if (rect.width > 1 && rect.height > 1) sizeSorted.insert(make_pair(count, pt));
                }
            }
        }

        cellRects.clear();
        floodPoints.clear();
        int fill = 1;
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            if (floodFill(src, maskCopy, it->second, fill, &rect, 0, 0, 4 | floodFlag | (fill << 8)) >= 1)
            {
                cellRects.push_back(rect);
                floodPoints.push_back(it->second);

                if (fill >= 255)
                    break; // just taking up to the top X largest objects found.
                fill++;
            }
        }
    }

    void RunMaxList() {
        Rect rect;

        int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
        multimap<int, Point, greater<int>> sizeSorted;
        for (size_t i = 0; i < maxList.size(); i++)
        {
            int count = floodFill(src, mask, maxList[i], 255, &rect, 0, 0, 4 | floodFlag | (255 << 8));
            if (rect.width > 1 && rect.height > 1) sizeSorted.insert(make_pair(count, maxList[i]));
        }

        Point pt;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                if (mask.at<unsigned char>(y, x) == 0)
                {
                    pt = Point(x, y);
                    int count = floodFill(src, mask, pt, 255, &rect, 0, 0, 4 | floodFlag | (255 << 8));
                    if (rect.width > 1 && rect.height > 1) sizeSorted.insert(make_pair(count, pt));
                }
            }
        }

        cellRects.clear();
        floodPoints.clear();
        int fill = 1;
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            if (floodFill(src, maskCopy, it->second, fill, &rect, 0, 0, 4 | floodFlag | (fill << 8)) >= 1)
            {
                cellRects.push_back(rect);
                floodPoints.push_back(it->second);

                if (fill >= 255)
                    break; // just taking up to the top X largest objects found.
                fill++;
            }
        }
    }
};

extern "C" __declspec(dllexport) RedCloudMaxDist* RedCloudMaxDist_Open()
{
    return new RedCloudMaxDist();
}

extern "C" __declspec(dllexport) int RedCloudMaxDist_Count(RedCloudMaxDist* cPtr)
{
    return (int)cPtr->cellRects.size();
}

extern "C" __declspec(dllexport) int* RedCloudMaxDist_Rects(RedCloudMaxDist* cPtr)
{
    return (int*)&cPtr->cellRects[0];
}

extern "C" __declspec(dllexport) void
RedCloudMaxDist_SetPoints(RedCloudMaxDist* cPtr, int count, int* dataPtr)
{
    Mat maxList = Mat(count, 1, CV_32SC2, dataPtr);
    cPtr->maxList.clear();
    for (int i = 0; i < count; i++)
    {
        cPtr->maxList.push_back(maxList.at<Point>(i, 0));
    }
}

extern "C" __declspec(dllexport) int* RedCloudMaxDist_Close(RedCloudMaxDist* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int*
RedCloudMaxDist_Run(RedCloudMaxDist* cPtr, int* dataPtr, unsigned char* maskPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->mask = Mat::zeros(rows + 2, cols + 2, CV_8U);
    cPtr->mask.setTo(0);
    Rect r = Rect(1, 1, cols, rows);
    if (maskPtr != 0)
    {
        Mat inputRemoved;
        inputRemoved = Mat(rows, cols, CV_8U, maskPtr);
        inputRemoved.copyTo(cPtr->mask(r));
    }
    cPtr->maskCopy = cPtr->mask.clone();
    if (cPtr->maxList.size() > 0)
        cPtr->RunMaxList();
    else
        cPtr->RunCPP();
    cPtr->maskCopy(r).copyTo(cPtr->result);
    return (int*)cPtr->result.data;
}





class EdgeLineSimple
{
private:
public:
    Mat src, dst;
    Ptr<EdgeLine_Image> eDraw;
    vector<Vec4f> lines;
    vector< vector<Point> > segments;
    EdgeLineSimple()
    {
        eDraw = new EdgeLine_Image();
    }
    void RunCPP(int lineWidth) {
        segments.clear();

        eDraw->ed->detectEdges(src);
        eDraw->ed->detectLines(lines);
        segments = eDraw->ed->getSegments();

        dst.setTo(0);
        for (size_t i = 0; i < segments.size(); i++)
        {
            const Point* pts = &segments[i][0];
            int n = (int)segments[i].size();
            // float distance = sqrt((pts[0].x - pts[n - 1].x) * (pts[0].x - pts[n - 1].x) + (pts[0].y - pts[n - 1].y) * (pts[0].y - pts[n - 1].y));
            // bool drawClosed = distance < 10;
            // polylines(dst, &pts, &n, 1, drawClosed, i + 1, lineWidth, LINE_4);
            polylines(dst, &pts, &n, 1, false, i + 1, lineWidth, LINE_4);
            if (i == 255) break; // if there are more segments, it is not likely to add much value.
        }
    }
};

extern "C" __declspec(dllexport)
EdgeLineSimple* EdgeLineSimple_Open() {
    EdgeLineSimple* cPtr = new EdgeLineSimple();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeLineSimple_Close(EdgeLineSimple* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* EdgeLineSimple_RunCPP(EdgeLineSimple* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
    if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->RunCPP(lineWidth);
    return (int*)cPtr->dst.data;
}





class EdgeDrawRaw
{
private:
public:
    Mat src, dst;
    Ptr<EdgeLine_Image> eDraw;
    vector<Vec4f> lines;
    vector< vector<Point> > segments;
    vector< Rect> rects;
    int segmentIndex;
    EdgeDrawRaw()
    {
        eDraw = new EdgeLine_Image();
    }
    void RunCPP(int lineWidth) {
        segments.clear();

        eDraw->ed->detectEdges(src);
        eDraw->ed->detectLines(lines);
        segments = eDraw->ed->getSegments();
        rects.clear();

        dst.setTo(0);
        for (size_t i = 0; i < segments.size(); i++)
        {
            const Point* pts = &segments[i][0];
            int size = (int)segments[i].size();
            //float distance = sqrt((pts[0].x - pts[n - 1].x) * (pts[0].x - pts[n - 1].x) + (pts[0].y - pts[n - 1].y) * (pts[0].y - pts[n - 1].y));
            //bool drawClosed = distance < 10;
            //polylines(dst, &pts, &n, 1, drawClosed, i + 1, lineWidth, LINE_4);
            polylines(dst, &pts, &size, 1, false, i + 1, lineWidth, LINE_4);
            rects.push_back(cv::boundingRect(segments[i]));
        }
    }
};

extern "C" __declspec(dllexport)
EdgeDrawRaw* EdgeLineRaw_Open() {
    EdgeDrawRaw* cPtr = new EdgeDrawRaw();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeLineRaw_Close(EdgeDrawRaw* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* EdgeLineRaw_RunCPP(EdgeDrawRaw* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
    if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_32S);
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->RunCPP(lineWidth);
    cPtr->segmentIndex = 0;
    return (int*)cPtr->dst.data;
}

extern "C" __declspec(dllexport)
int EdgeLineRaw_GetSegCount(EdgeDrawRaw* cPtr)
{
    return (int)cPtr->segments.size();
}

extern "C" __declspec(dllexport) int* EdgeLineRaw_Rects(EdgeDrawRaw* cPtr)
{
    return (int*)&cPtr->rects[0];
}

extern "C" __declspec(dllexport)
int EdgeLineRaw_NextLength(EdgeDrawRaw* cPtr)
{
    return (int)cPtr->segments[cPtr->segmentIndex].size();
}

extern "C" __declspec(dllexport)
int* EdgeLineRaw_NextSegment(EdgeDrawRaw* cPtr)
{
    return (int*)&cPtr->segments[cPtr->segmentIndex++][0];
}