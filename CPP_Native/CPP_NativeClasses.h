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
#include "../CPP_Managed/PragmaLibs.h"

using namespace std;
using namespace cv;
using namespace bgsegm;
using namespace ximgproc;
using namespace ml;

#include "CPP_Parent.h"
#include "Options.h"


#ifndef VIDEOSTAB_H
#define VIDEOSTAB_H
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace  cv;

class VideoStab
{
public:
    VideoStab();
    Mat errScale, qScale, rScale, sScale, sumScale;
    Mat lastFrame;
    Mat gray;
    int k;

    const int HORIZONTAL_BORDER_CROP = 30;

    Mat affine;
    Mat smoothedMat;
    Mat smoothedFrame;

    Mat stabilize(Mat rgb);
    void Kalman_Filter();
};

#endif // VIDEOSTAB_H







class EdgeDraw_Basics
{
private:
public:
    Mat src, dst;
    vector<Vec4f> lines;
    Ptr<EdgeDrawing> ed;
    EdgeDraw_Basics()
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
EdgeDraw_Basics* EdgeDraw_Basics_Open() {
    EdgeDraw_Basics* cPtr = new EdgeDraw_Basics();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Basics_Close(EdgeDraw_Basics* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Basics_RunCPP(EdgeDraw_Basics* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
    if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->RunCPP(lineWidth);
    return (int*)cPtr->dst.data;
}








class EdgeDraw
{
private:
public:
    Mat src, dst;
    Ptr<EdgeDraw_Basics> eDraw;
    vector<Vec4f> lines;
    EdgeDraw()
    {
        eDraw = new EdgeDraw_Basics();
    }
    void RunCPP(int lineWidth) {
        eDraw->ed->detectEdges(src);
        eDraw->ed->detectLines(lines);
        vector< vector<Point> > segments = eDraw->ed->getSegments();

        dst.setTo(0);
        for (size_t i = 0; i < segments.size(); i++)
        {
            const Point* pts = &segments[i][0];
            int n = (int)segments[i].size();
            float distance = sqrt((pts[0].x - pts[n - 1].x) * (pts[0].x - pts[n - 1].x) + (pts[0].y - pts[n - 1].y) * (pts[0].y - pts[n - 1].y));
            bool drawClosed = distance < 10;
            polylines(dst, &pts, &n, 1, drawClosed, Scalar(255, 255, 255), lineWidth, LINE_AA);
        }
    }
};

extern "C" __declspec(dllexport)
EdgeDraw* EdgeDraw_Edges_Open() {
    EdgeDraw* cPtr = new EdgeDraw();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Edges_Close(EdgeDraw* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_RunCPP(EdgeDraw* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
    if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->RunCPP(lineWidth);
    return (int*)cPtr->dst.data;
}









class EdgeDraw_Lines
{
private:
public:
    Mat src, dst;
    Ptr<EdgeDraw_Basics> eDraw;
    vector<Vec4f> lines;
    EdgeDraw_Lines()
    {
        eDraw = new EdgeDraw_Basics();
    }
    void RunCPP(int lineWidth) {
        eDraw->ed->detectEdges(src);
        eDraw->ed->detectLines(lines);
    }
};

extern "C" __declspec(dllexport)
EdgeDraw_Lines* EdgeDraw_Lines_Open() {
    EdgeDraw_Lines* cPtr = new EdgeDraw_Lines();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Lines_Close(EdgeDraw_Lines* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int EdgeDraw_Lines_Count(EdgeDraw_Lines* cPtr)
{
    return int(cPtr->lines.size());
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Lines_RunCPP(EdgeDraw_Lines* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
    if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->RunCPP(lineWidth);
    return (int*)&cPtr->lines[0];
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
extern "C" __declspec(dllexport)
BGRPattern_Basics* BGRPattern_Open() {
    BGRPattern_Basics* cPtr = new BGRPattern_Basics();
    return cPtr;
}
extern "C" __declspec(dllexport)
void BGRPattern_Close(BGRPattern_Basics* cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int BGRPattern_ClassCount(BGRPattern_Basics* cPtr)
{
    return cPtr->classCount;
}

extern "C" __declspec(dllexport)
int* BGRPattern_RunCPP(BGRPattern_Basics* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
    cPtr->dst = Mat(rows, cols, CV_8U);
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







//// https://www.codeproject.com/Articles/5362105/Perceptual-Hash-based-Image-Comparison-Coded-in-pl
//double dctSimple(double* DCTMatrix, double* ImgMatrix, int N, int M)
//{
//	int i, j, u, v;
//	int cnt = 0;
//	double DCTsum = 0.0;
//	for (u = 0; u < N; ++u)
//	{
//		for (v = 0; v < M; ++v)
//		{
//			DCTMatrix[(u * N) + v] = 0;
//			for (i = 0; i < N; i++)
//			{
//				for (j = 0; j < M; j++)
//				{
//					DCTMatrix[(u * N) + v] += ImgMatrix[(i * N) + j]
//						* cos(CV_PI / ((double)N) * (i + 1. / 2.) * u)
//						* cos(CV_PI / ((double)M) * (j + 1. / 2.) * v);
//					cnt = cnt++;
//				}
//			}
//			DCTsum += DCTMatrix[(u * N) + v];
//		}
//	}
//	DCTsum -= DCTMatrix[0];
//	return   DCTsum / cnt;
//}
//
//
//
//
//
//
//int CoeffFlag = 0;
//double Coeff[32][32];
//
//// https://www.codeproject.com/Articles/5362105/Perceptual-Hash-based-Image-Comparison-Coded-in-pl
//double DCT(double* DCTMatrix, double* IMGMatrix)
//{
//	/*
//		Concerning: the expression
//		dctRCvalue+= IMGMatrix[(imgRow * RowMax) + imgCol]    // this is dependent on the IMAGE
//		* cos( PIover32 *(imgRow+0.5)*dctRow)    //  this is a set of FIXED values
//		* cos( PIover32 *(imgCol+0.5)*dctCol);   //  this is a set of FIXED values
//
//		Let us call the  2 sets of FIXED values   rCoeff and cCoeff
//		they both have the same set of values
//		=  cos (  PIover32  * ( x + 0.5 ) * y )   for x = 0 .. 31    and y = for x = 0 .. 31
//		= 32*32 distinct COSINE values
//		= we could calculate these COSINE values in advance, place them in an array, and (hopefully) speed things up by doing a simple look up .
//	*/
//#define  PIover32 0.09817477042
//#define  RowMax 32
//#define  ColMax 32    
//	int imgRow, imgCol;
//	int dctRow, dctCol;
//	int x, y;
//	int cnt = 0;
//	double DCTsum = 0.0;
//	double dctRCvalue = 0.0;
//
//	if (!CoeffFlag)
//	{
//		for (x = 0; x < 32; x++)
//		{
//			for (y = 0; y < 32; y++)
//			{
//				Coeff[x][y] = cos(PIover32 * (x + 0.5) * y);
//			}
//		}
//		CoeffFlag = 1;
//	}
//
//	for (dctRow = 0; dctRow < 8; dctRow++)
//	{
//		for (dctCol = 0; dctCol < 8; dctCol++)
//		{
//			dctRCvalue = 0;
//
//			for (imgRow = 0; imgRow < RowMax; imgRow++)
//			{
//				for (imgCol = 0; imgCol < ColMax; imgCol++)
//				{
//					dctRCvalue += IMGMatrix[(imgRow * RowMax) + imgCol]
//						* Coeff[imgRow][dctRow]    //    cos( PIover32 *(imgRow+0.5)*dctRow)   
//						* Coeff[imgCol][dctCol];  //    cos( PIover32 *(imgCol+0.5)*dctCol) ; 
//					cnt = cnt++;
//				}
//			}
//			DCTMatrix[(dctRow * RowMax) + dctCol] = dctRCvalue;
//			DCTsum += dctRCvalue;
//		}
//	}
//	DCTsum -= DCTMatrix[0];
//	return   DCTsum / cnt;
//}






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







class RedCloud_FindCells
{
private:
public:
    vector <int> cellList;
    RedCloud_FindCells() {}
    void RunCPP(Mat src)
    {
        cellList.clear();
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                auto val = src.at<unsigned char>(y, x);
                if (count(cellList.begin(), cellList.end(), val) == 0)
                    cellList.push_back(val);
            }
        }
    }
};
extern "C" __declspec(dllexport)
RedCloud_FindCells* RedCloud_FindCells_Open() {
    RedCloud_FindCells* cPtr = new RedCloud_FindCells();
    return cPtr;
}
extern "C" __declspec(dllexport)
void RedCloud_FindCells_Close(RedCloud_FindCells* cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport) int RedCloud_FindCells_TotalCount(RedCloud_FindCells* cPtr) { return int(cPtr->cellList.size()); }
extern "C" __declspec(dllexport)
int* RedCloud_FindCells_RunCPP(RedCloud_FindCells* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->RunCPP(Mat(rows, cols, CV_8UC1, dataPtr));
    if (cPtr->cellList.size() == 0) return 0;
    return (int*)&cPtr->cellList[0];
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





class Stabilizer_Basics_CC 
{
private:
public:
    VideoStab stab;
    Mat rgb;
    Mat smoothedFrame;
    Stabilizer_Basics_CC() 
    {
        smoothedFrame = Mat(2, 3, CV_64F);
    }
    void Run()
    {
        smoothedFrame = stab.stabilize(rgb);
    }
};

extern "C" __declspec(dllexport)
Stabilizer_Basics_CC* Stabilizer_Basics_Open()
{
    Stabilizer_Basics_CC* cPtr = new Stabilizer_Basics_CC();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Stabilizer_Basics_Close(Stabilizer_Basics_CC* cPtr)
{
    delete cPtr;
    return (int*)0;
}

// https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
extern "C" __declspec(dllexport)
int* Stabilizer_Basics_Run(Stabilizer_Basics_CC* cPtr, int* bgrPtr, int rows, int cols)
{
    cPtr->rgb = Mat(rows, cols, CV_8UC3, bgrPtr);
    cvtColor(cPtr->rgb, cPtr->stab.gray, COLOR_BGR2GRAY);
    if (cPtr->stab.lastFrame.rows > 0) cPtr->Run(); // skips the first pass while the frames get loaded.
    cPtr->stab.gray.copyTo(cPtr->stab.lastFrame);
    return (int*)cPtr->stab.smoothedFrame.data;
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

VideoStab::VideoStab()
{
    smoothedMat.create(2, 3, CV_64F);
    k = 1;
    errScale = Mat(5, 1, CV_64F).setTo(1);
    qScale = Mat(5, 1, CV_64F).setTo(Q1);
    rScale = Mat(5, 1, CV_64F).setTo(R1);
    sumScale = Mat(5, 1, CV_64F).setTo(0);
    sScale = Mat(5, 1, CV_64F).setTo(0);
}

// https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
//The main stabilization function
Mat VideoStab::stabilize(Mat rgb)
{
    int vert_border = HORIZONTAL_BORDER_CROP * gray.rows / gray.cols;

    vector <Point2f> features1, features2;
    vector <Point2f> goodFeatures1, goodFeatures2;
    vector <uchar> status;
    vector <float> err;

    //Estimating the features in gray1 and gray2
    goodFeaturesToTrack(gray, features1, 200, 0.01, 30);
    calcOpticalFlowPyrLK(gray, lastFrame, features1, features2, status, err);

    for (size_t i = 0; i < status.size(); i++)
    {
        if (status[i] && err[i] < 2)
        {
            goodFeatures1.push_back(features1[i]);
            goodFeatures2.push_back(features2[i]);
        }
    }

    if (goodFeatures1.size() > 0 && goodFeatures2.size() > 0)
    {
        //All the parameters scale, angle, and translation are stored in affine
        affine = getAffineTransform(goodFeatures1.data(), goodFeatures2.data());

        double dx = affine.at<double>(0, 2);
        double dy = affine.at<double>(1, 2);
        double da = atan2(affine.at<double>(1, 0), affine.at<double>(0, 0));
        double ds_x = affine.at<double>(0, 0) / cos(da);
        double ds_y = affine.at<double>(1, 1) / cos(da);
        double saveDX = dx, saveDY = dy, saveDA = da;

        char original[1000];
        sprintf_s(original, "da = %f, dx = %f, dy = %f", da, dx, dy);

        double sx = ds_x;
        double sy = ds_y;

        double deltaArray[5] = { ds_x, ds_y, da, dx, dy };
        Mat delta(5, 1, CV_64F, deltaArray);
        add(sumScale, delta, sumScale);

        //Don't calculate the predicted state of Kalman Filter on 1st iteration
        if (k == 1) k++; else Kalman_Filter();

        Mat diff(5, 1, CV_64F);
        subtract(sScale, sumScale, diff);

        if (diff.at<double>(2, 0) < 1000 && diff.at<double>(3, 0) < 1000 && diff.at<double>(4, 0) < 1000)
        {
            da += diff.at<double>(2, 0);
            dx += diff.at<double>(3, 0);
            dx += diff.at<double>(4, 0);
        }
        if (fabs(dx) > 50)  dx = saveDX;
        if (fabs(dy) > 50)  dy = saveDY;
        if (fabs(da) > 50)  da = saveDA;

        //Creating the smoothed parameters matrix
        smoothedMat.at<double>(0, 0) = sx * cos(da);
        smoothedMat.at<double>(0, 1) = sx * -sin(da);
        smoothedMat.at<double>(1, 0) = sy * sin(da);
        smoothedMat.at<double>(1, 1) = sy * cos(da);

        smoothedMat.at<double>(0, 2) = dx;
        smoothedMat.at<double>(1, 2) = dy;

        //Warp the new frame using the smoothed parameters
        warpAffine(rgb, smoothedFrame, smoothedMat, rgb.size());

        //Crop the smoothed frame a little to eliminate black region due to Kalman Filter
        smoothedFrame = smoothedFrame(Range(vert_border, smoothedFrame.rows - vert_border), Range(HORIZONTAL_BORDER_CROP, smoothedFrame.cols - HORIZONTAL_BORDER_CROP));

        resize(smoothedFrame, smoothedFrame, rgb.size());
        for (int i = 0; i < features1.size(); ++i)
        {
            circle(smoothedFrame, features1[i], 5, Scalar::all(255), -1, LineTypes::LINE_AA);
        }
        putText(smoothedFrame, original, Point(10, 50), HersheyFonts::FONT_HERSHEY_COMPLEX, 0.4, Scalar::all(255), 1);

        char buffer[1000];
        sprintf_s(buffer, "da = %f, dx = %f, dy = %f", da, dx, dy);
        putText(smoothedFrame, buffer, Point(10, 100), HersheyFonts::FONT_HERSHEY_COMPLEX, 0.4, Scalar::all(255), 1);
    }
    return smoothedFrame;
}

void VideoStab::Kalman_Filter()
{
    Mat f1err = Mat(5, 1, CV_64F);
    add(errScale, qScale, f1err);
    for (int i = 0; i < f1err.rows; ++i)
    {
        double gainScale = f1err.at<double>(i, 0) / (f1err.at<double>(i, 0) + rScale.at<double>(i, 0));
        sScale.at<double>(i, 0) = sScale.at<double>(i, 0) + gainScale * (sumScale.at<double>(i, 0) - sScale.at<double>(i, 0));
        errScale.at<double>(i, 0) = (1.0 - gainScale) * f1err.at<double>(i, 0);
    }
}








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






class xPhoto_OilPaint
{
private:
public:
    Mat src, dst;
    xPhoto_OilPaint() {}
    void Run(int size, int dynRatio, int colorCode)
    {
        xphoto::oilPainting(src, dst, size, dynRatio, colorCode);
    }
};

extern "C" __declspec(dllexport)
xPhoto_OilPaint* xPhoto_OilPaint_Open()
{
    xPhoto_OilPaint* cPtr = new xPhoto_OilPaint();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* xPhoto_OilPaint_Close(xPhoto_OilPaint* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* xPhoto_OilPaint_Run(xPhoto_OilPaint* cPtr, int* imagePtr, int rows, int cols, int size, int dynRatio, int colorCode)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, imagePtr);
    cPtr->Run(size, dynRatio, colorCode);
    return (int*)cPtr->dst.data;
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
xPhoto_Inpaint* xPhoto_Inpaint_Open()
{
    xPhoto_Inpaint* cPtr = new xPhoto_Inpaint();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* xPhoto_Inpaint_Close(xPhoto_Inpaint* cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* xPhoto_Inpaint_Run(xPhoto_Inpaint* cPtr, int* imagePtr, int* maskPtr, int rows, int cols, int iType)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, imagePtr);
    cPtr->dst = Mat(rows, cols, CV_8UC3);
    Mat mask = Mat(rows, cols, CV_8UC1, maskPtr);
    cPtr->Run(mask, iType);
    return (int*)cPtr->dst.data;
}




cppTask* task;

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
        if (!task->paused) {
            while (pointList.size() < sizeRequest) {
                pointList.push_back(Point2f(range.x + float((rand() % range.width)), range.y + float((rand() % range.height))));
            }

            dst2.setTo(0);
            for (Point2f pt : pointList) {
                circle(dst2, pt, task->DotSize, Scalar(0, 255, 255), -1, task->lineType, 0);
            }
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
        Random_Basics_CC* random = new Random_Basics_CC();
        random->sizeRequest = count;

        random->range = Rect(0, 0, cols * 3 / 4, rows * 3 / 4);
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
extern "C" __declspec(dllexport) Neighbors* Neighbors_Open() { Neighbors* cPtr = new Neighbors(); return cPtr; }
extern "C" __declspec(dllexport) void Neighbors_Close(Neighbors* cPtr) { delete cPtr; }
extern "C" __declspec(dllexport) int* Neighbors_NabList(Neighbors* cPtr) { return (int*)&cPtr->nabList[0]; }
extern "C" __declspec(dllexport)
int Neighbors_RunCPP(Neighbors* cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int)cPtr->nabList.size();
}









class RedCloud
{
private:
public:
    Mat src, result;
    vector<Rect>cellRects;
    vector<Point> floodPoints;

    RedCloud() {}
    void RunCPP(Mat inputMask) {
        Mat maskCopy = inputMask.clone();
        Rect rect;

        multimap<int, Point, greater<int>> sizeSorted;
        int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
        Point pt;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                if (inputMask.at<unsigned char>(y, x) == 0)
                {
                    pt = Point(x, y);
                    int count = floodFill(src, inputMask, pt, 255, &rect, 0, 0, 4 | floodFlag | (255 << 8));
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
        Rect r = Rect(1, 1, inputMask.cols - 2, inputMask.rows - 2);
        maskCopy(r).copyTo(result);
    }
};

extern "C" __declspec(dllexport) RedCloud* RedCloud_Open() { return new RedCloud(); }
extern "C" __declspec(dllexport) int RedCloud_Count(RedCloud* cPtr)
{
    return (int)cPtr->cellRects.size();
}

extern "C" __declspec(dllexport) int* RedCloud_Rects(RedCloud* cPtr)
{
    return (int*)&cPtr->cellRects[0];
}

extern "C" __declspec(dllexport) int* RedCloud_FloodPoints(RedCloud* cPtr)
{
    return (int*)&cPtr->floodPoints[0];
}

extern "C" __declspec(dllexport) int* RedCloud_Close(RedCloud* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int*
RedCloud_Run(RedCloud* cPtr, int* dataPtr, unsigned char* maskPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);

    Mat inputMask = Mat(rows, cols, CV_8U, maskPtr);

    copyMakeBorder(inputMask, inputMask, 1, 1, 1, 1, BORDER_CONSTANT, 0);
    cPtr->RunCPP(inputMask);

    return (int*)cPtr->result.data;
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

extern "C" __declspec(dllexport) RedCloudMaxDist* RedCloudMaxDist_Open() { return new RedCloudMaxDist(); }
extern "C" __declspec(dllexport) int RedCloudMaxDist_Count(RedCloudMaxDist* cPtr)
{
    return (int)cPtr->cellRects.size();
}

extern "C" __declspec(dllexport) int* RedCloudMaxDist_Rects(RedCloudMaxDist* cPtr)
{
    return (int*)&cPtr->cellRects[0];
}

extern "C" __declspec(dllexport) int* RedCloudMaxDist_FloodPoints(RedCloudMaxDist* cPtr)
{
    return (int*)&cPtr->floodPoints[0];
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
        Mat inputMask;
        inputMask = Mat(rows, cols, CV_8U, maskPtr);
        inputMask.copyTo(cPtr->mask(r));
    }
    cPtr->maskCopy = cPtr->mask.clone();
    if (cPtr->maxList.size() > 0)
        cPtr->RunMaxList();
    else
        cPtr->RunCPP();
    cPtr->maskCopy(r).copyTo(cPtr->result);
    return (int*)cPtr->result.data;
}




class AddWeighted_Basics_CC : public CPP_Parent {
public:
    double weight;
    cv::Mat src2;
    Options_AddWeighted* options = new Options_AddWeighted();
    AddWeighted_Basics_CC() : CPP_Parent()
    {
        task->UpdateAdvice(traceName + ": use the local option slider 'Add Weighted %'");
        desc = "Add 2 images with specified weights.";
    }
    void Run(cv::Mat src)
    {
        options->RunOpt();
        cv::Mat srcPlus = src2;
        // algorithm user normally provides src2! 
        if (standalone || src2.empty()) srcPlus = task->depthRGB;
        if (srcPlus.type() != src.type())
        {
            if (src.type() != CV_8UC3 || srcPlus.type() != CV_8UC3)
            {
                if (src.type() == CV_32FC1) normalize(src, src, 0, 255, NORM_MINMAX);
                if (srcPlus.type() == CV_32FC1) normalize(srcPlus, srcPlus, 0, 255, NORM_MINMAX);
                if (src.type() != CV_8UC3) cv::cvtColor(src, src, cv::COLOR_GRAY2BGR);
                if (srcPlus.type() != CV_8UC3) cv::cvtColor(srcPlus, srcPlus, cv::COLOR_GRAY2BGR);
            }
        }

        weight = options->addWeighted;
        cv::addWeighted(src, weight, srcPlus, 1.0 - weight, 0, dst2);
        labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));
    }
};







class Resize_Basics_CC : public CPP_Parent {
public:
    Size newSize;
    float resizePercent = 0.5;

    Resize_Basics_CC() : CPP_Parent() {
        if (standalone) {
            task->drawRect = Rect(dst2.cols / 4, dst2.rows / 4, dst2.cols / 2, dst2.rows / 2);
        }
        desc = "Resize with different options and compare them";
        labels[2] = "Rectangle highlight above resized";
    }

    void Run(Mat src) {
        if (task->drawRect.width != 0) {
            src = src(task->drawRect);
            newSize = task->drawRect.size();
        }

        resize(src, dst2, Size(int(src.cols * resizePercent), int(src.rows * resizePercent)), 0, 0, INTER_NEAREST_EXACT);
    }
};






class Remap_Basics_CC : public CPP_Parent {
public:
    int direction = 3;  // Default to remap horizontally and vertically
    Mat mapx1, mapx2, mapx3, mapy1, mapy2, mapy3;

    Remap_Basics_CC() : CPP_Parent() {
        // Initialize map matrices with appropriate size and type
        mapx1 = Mat::zeros(dst2.size(), CV_32FC1);
        mapy1 = Mat::zeros(dst2.size(), CV_32FC1);
        mapx2 = Mat::zeros(dst2.size(), CV_32FC1);
        mapy2 = Mat::zeros(dst2.size(), CV_32FC1);
        mapx3 = Mat::zeros(dst2.size(), CV_32FC1);
        mapy3 = Mat::zeros(dst2.size(), CV_32FC1);

        // Populate map matrices with values for remapping
        for (int j = 0; j < mapx1.rows; j++) {
            for (int i = 0; i < mapx1.cols; i++) {
                mapx1.at<float>(j, i) = i;
                mapy1.at<float>(j, i) = dst2.rows - j;
                mapx2.at<float>(j, i) = dst2.cols - i;
                mapy2.at<float>(j, i) = j;
                mapx3.at<float>(j, i) = dst2.cols - i;
                mapy3.at<float>(j, i) = dst2.rows - j;
            }
        }

        desc = "Use remap to reflect an image in 4 directions.";
    }

    void Run(Mat src) {
        vector<string> labels = {
            "Remap_Basics - original",
            "Remap vertically",
            "Remap horizontally",
            "Remap horizontally and vertically"
        };
        string label = labels[direction];

        switch (direction) {
        case 0:
            dst2 = src.clone();  // Use clone for copying
            break;
        case 1:
            remap(src, dst2, mapx1, mapy1, INTER_NEAREST);
            break;
        case 2:
            remap(src, dst2, mapx2, mapy2, INTER_NEAREST);
            break;
        case 3:
            remap(src, dst2, mapx3, mapy3, INTER_NEAREST);
            break;
        }

        if (task->heartBeat) {
            direction = (direction + 1) % 4;
        }
    }
};




class Random_Enumerable_CC : public CPP_Parent {
public:
    int sizeRequest = 100;
    vector<Point2f> points;

    Random_Enumerable_CC() : CPP_Parent() {
        desc = "Create an enumerable list of points using a lambda function";
    }

    void Run(Mat src) {
        random_device rd;
        mt19937 gen(rd());  // Mersenne Twister engine for randomness
        uniform_int_distribution<> dist_width(0, dst2.cols - 1);  // Ensure width is within bounds
        uniform_int_distribution<> dist_height(0, dst2.rows - 1); // Ensure height is within bounds

        points.clear();
        for (int i = 0; i < sizeRequest; i++) {
            points.push_back(Point2f(dist_width(gen), dist_height(gen)));
        }

        dst2 = Mat::zeros(dst2.size(), dst2.type());  // Set dst2 to black
        for (const Point2f& pt : points) {
            circle(dst2, pt, task->DotSize, Scalar(0, 255, 255), -1, task->lineType, 0);
        }
    }
};






class Delaunay_Basics_CC : public CPP_Parent {
public:
    std::vector<cv::Point2f> inputPoints;
    cv::Mat facet32s;
    Random_Enumerable_CC* randEnum;
    cv::Subdiv2D subdiv;
    Mat lastColor;
    std::vector<std::vector<cv::Point>> facetlist;
    Delaunay_Basics_CC() : CPP_Parent() {
        randEnum = new Random_Enumerable_CC();
        lastColor = Mat(task->WorkingRes, CV_8UC3);
        lastColor.setTo(0);
        facet32s = cv::Mat::zeros(dst2.size(), CV_32SC1);
        desc = "Subdivide an image based on the points provided.";
    }
    void randomInput(cv::Mat src) {
        randEnum->Run(src);
        inputPoints = randEnum->points;
    }
    void Run(cv::Mat src) {
        if (task->heartBeat && standalone) {
            randEnum->Run(src);
            inputPoints = randEnum->points;
            dst3 = randEnum->dst2;
        }
        subdiv.initDelaunay(cv::Rect(0, 0, dst2.cols, dst2.rows));
        subdiv.insert(inputPoints);
        std::vector<std::vector<cv::Point2f>> facets;
        std::vector<cv::Point2f> centers;
        subdiv.getVoronoiFacetList(std::vector<int>(), facets, centers);
        std::vector<cv::Vec3b> usedColors;
        facetlist.clear();
        for (size_t i = 0; i < facets.size(); i++) {
            std::vector<cv::Point> nextFacet;
            for (size_t j = 0; j < facets[i].size(); j++) {
                nextFacet.push_back(cv::Point(facets[i][j].x, facets[i][j].y));
            }
            cv::Point2f pt = inputPoints[i];
            cv::Vec3b nextColor = lastColor.at<cv::Vec3b>(pt.y, pt.x);
            if (std::find(usedColors.begin(), usedColors.end(), nextColor) != usedColors.end()) {
                nextColor = task->randomCellColor();
            }
            usedColors.push_back(nextColor);
            cv::fillConvexPoly(dst2, nextFacet, cv::Scalar(nextColor[0], nextColor[1], nextColor[2]));
            cv::fillConvexPoly(facet32s, nextFacet, i, task->lineType);
            facetlist.push_back(nextFacet);
        }
        facet32s.convertTo(dst1, CV_8U);
        lastColor = dst2.clone();
        labels[2] = traceName + ": " + std::to_string(inputPoints.size()) + " cells were present.";
    }
};






class Delaunay_GenerationsNoKNN_CC : public CPP_Parent {
public:
    vector<Point2f> inputPoints;
    Random_Basics_CC* random;
    Delaunay_Basics_CC* facet;
    Mat generationMap;
    Delaunay_GenerationsNoKNN_CC() : CPP_Parent() {
        facet = new Delaunay_Basics_CC();
        random = new Random_Basics_CC();
        generationMap = Mat::zeros(dst3.size(), CV_32S);
        labels = { "", "Mask of unmatched regions - generation set to 0", "Facet Image with index of each region", "Generation counts for each region." };
        desc = "Create a region in an image for each point provided with KNN.";
    }

    void Run(Mat src) {
        if (standalone && task->heartBeat) {
            random->Run(empty);
            inputPoints = random->pointList;   
        }

        facet->inputPoints = inputPoints;
        facet->Run(src);
        dst2 = facet->dst2;

        if (task->heartBeat) generationMap.setTo(0);
        vector<int> usedG;
        int g;
        for (const auto& pt : inputPoints) {
            int index = facet->facet32s.at<int>(pt.y, pt.x);
            if (index >= facet->facetlist.size()) continue;
            vector<Point> nextFacet = facet->facetlist[index];

            // Ensure unique generation numbers
            if (task->FirstPass) {
                g = (int) usedG.size();
            }
            else {
                g = generationMap.at<int>(pt.y, pt.x) + 1;
                while (find(usedG.begin(), usedG.end(), g) != usedG.end()) {
                    g++;
                }
            }
            fillConvexPoly(generationMap, nextFacet, g, task->lineType);
            usedG.push_back(g);
            task->SetTrueText(to_string(g), dst2, pt);   
        }
        dst3 = generationMap.clone();
    }
};







class Grid_Basics_CC : public CPP_Parent {
public:
    Grid_Basics_CC() : CPP_Parent() {
        desc = "Create a grid of squares covering the entire image.";
    }

    void Run(Mat src) {
        if (task->mouseClickFlag && !task->FirstPass) {
            task->gridROIclicked = task->gridToRoiIndex.at<int>(task->ClickPoint.y, task->ClickPoint.x);
        }
        if (task->optionsChanged) {
            task->gridMask = Mat::zeros(src.size(), CV_8U);
            task->gridToRoiIndex = Mat::zeros(src.size(), CV_32S);

            task->gridList.clear();
            task->gridRows = 0;
            task->gridCols = 0;
            for (int y = 0; y < src.rows; y += task->gridSize) {
                for (int x = 0; x < src.cols; x += task->gridSize) {
                    Rect roi(x, y, task->gridSize, task->gridSize);
                    if (x + roi.width >= src.cols) roi.width = src.cols - x;
                    if (y + roi.height >= src.rows) roi.height = src.rows - y;
                    if (roi.width > 0 && roi.height > 0) {
                        if (x == 0) task->gridRows++;
                        if (y == 0) task->gridCols++;
                        task->gridList.push_back(roi);
                    }
                }
            }
            task->gridMask = Mat::zeros(src.size(), CV_8U);
            for (int x = task->gridSize; x < src.cols; x += task->gridSize) {
                Point p1(x, 0), p2(x, src.rows);
                line(task->gridMask, p1, p2, Scalar(255), task->lineWidth);
            }
            for (int y = task->gridSize; y < src.rows; y += task->gridSize) {
                Point p1(0, y), p2(src.cols, y);
                line(task->gridMask, p1, p2, Scalar(255), task->lineWidth);
            }

            for (int i = 0; i < task->gridList.size(); i++) {
                Rect roi = task->gridList[i];
                rectangle(task->gridToRoiIndex, roi, Scalar(255, 255, 255), 1);
            }

            task->gridNeighbors.clear();
            for (const Rect& roi : task->gridList) {
                vector<int> neighbors;
                task->gridNeighbors.push_back(neighbors);
                for (int i = 0; i < 8; i++) {
                    int x = (i % 3 == 1) ? roi.x + roi.width : (i < 3) ? roi.x - 1 : roi.x + roi.width + 1;
                    int y = (i < 3) ? roi.y - 1 : (i < 6) ? roi.y + roi.height + 1 : roi.y;
                    if (x >= 0 && x < src.cols && y >= 0 && y < src.rows) {
                        task->gridNeighbors.back().push_back(task->gridToRoiIndex.at<int>(y, x));
                    }
                }
            }
        }
        if (standalone) {
            dst2 = Mat::zeros(src.size(), CV_8U);
            task->color.copyTo(dst2);
            dst2.setTo(Scalar(255, 255, 255), task->gridMask); 
            stringstream ss;
            ss << "Grid_Basics_CC " << task->gridList.size() << " (" << task->gridRows << "X" << task->gridCols << ") "
                << task->gridSize << "X" << task->gridSize << " regions";
            labels[2] = ss.str();
        }
    }
};







class KNN_Core_CC : public CPP_Parent {
public:
    Ptr<ml::KNearest> knn = ml::KNearest::create();
    vector<Point2f> trainInput;
    vector<Point2f> queries;
    vector<vector<int>> neighbors;
    Mat result;
    int desiredMatches = -1;
    Random_Basics_CC* random;
    vector<int> neighborIndexToTrain;
    
    KNN_Core_CC() : CPP_Parent()
    {
        random = new Random_Basics_CC();
        labels[2] = "Red=TrainingData, yellow = queries";
        desc = "Train a KNN model and map each query to the nearest training neighbor.";
    }
    void displayResults() {
        dst2.setTo(0);  // Assuming dst2 is a Mat object
        int dm = min(trainInput.size(), queries.size());

        for (int i = 0; i < queries.size(); i++) {
            Point2f pt = queries[i];
            int test = result.at<int>(i, 0);
            if (test >= trainInput.size() || test < 0) continue;
            Point2f nn = trainInput[result.at<int>(i, 0)];
            circle(dst2, pt, task->DotSize + 4, Scalar(0, 255, 255), -1, task->lineType);  // Yellow
            line(dst2, pt, nn, Scalar(0, 255, 255), task->lineWidth, task->lineType);
        }

        for (Point2f pt : trainInput) {
            circle(dst2, pt, task->DotSize + 4, Scalar(0, 0, 255), -1, task->lineType);  // Red
        }
    }
    void generateRandom(Mat src)
    {
        random->Run(src);
        queries = random->pointList;
        if (task->heartBeat)
            trainInput = queries;
    }
    void Run(Mat src)
    {
        int KNNdimension = 2;

        if (standalone) {
            if (task->heartBeat) {
                random->Run(empty);
                trainInput = random->pointList;
            }
            random->Run(empty);
            queries = random->pointList;
        }

        Mat queryMat((int)queries.size(), KNNdimension, CV_32F, queries.data());
        if (queryMat.rows == 0) {
            task->SetTrueText("There were no queries provided. There is nothing to do...", dst2);
            return;
        }

        if (trainInput.empty()) {
            trainInput = queries;  // First pass, just match the queries.
        }
        Mat trainData((int)trainInput.size(), KNNdimension, CV_32F, trainInput.data());
        vector<int> array(trainData.rows);
        iota(array.begin(), array.end(), 0);
        Mat response(trainData.rows, 1, CV_32S, array.data());  // Create response matrix
        knn->train(trainData, ml::ROW_SAMPLE, response);

        int dm = desiredMatches < 0 ? (int)trainInput.size() : desiredMatches;
        Mat neighborMat;
        knn->findNearest(queryMat, dm, noArray(), neighborMat);

        if (neighborMat.rows != queryMat.rows || neighborMat.cols != dm) {
            cerr << "KNN's FindNearest did not return the correct number of neighbors.\n";
            return;
        }

        vector<float> nData(queryMat.rows * dm);
        if (nData.empty()) return;
        memcpy(nData.data(), neighborMat.data, nData.size() * sizeof(float));

        for (int i = 0; i < nData.size(); i++) {
            if (abs(nData[i]) > trainInput.size()) nData[i] = 0;  // Clamp within trainInput range
        }

        result.create(queryMat.rows, dm - 1, CV_32S);
        neighbors.clear();
        for (int i = 0; i < queryMat.rows; i++) {
            Point2f pt = queries[i];
            vector<int> res;
            for (int j = 0; j < dm - 1; j++) {
                int test = static_cast<int>(nData[i * dm + j]);
                if (test >= 0 && test < nData.size()) {
                    result.at<int>(i, j) = test;
                    res.push_back(test);
                }
            }
            neighbors.push_back(res);
        }
        if (neighbors.size() > 1) displayResults();
    }
};





class KNN_Basics_CC : public CPP_Parent {
private:
public:
    vector<PointPair> matches;
    vector<Point> noMatch;
    KNN_Core_CC* basics;
    vector<Point2f> queries;
    vector<int> neighbors;
    Random_Basics_CC* random;

    KNN_Basics_CC() : CPP_Parent() {
        basics = new KNN_Core_CC();
        random = new Random_Basics_CC();
        desc = "Map points 1:1 with losses. Toss any duplicates that are farther.";
    }
    void Run(Mat src)
    {
        if (standalone) {
            if (task->heartBeat) {
                random->Run(empty);
                basics->trainInput = random->pointList;
            }
            random->Run(empty);
            queries = random->pointList;
        }

        if (queries.empty()) {
            task->SetTrueText("Place some input points in queries before starting the knn run.", dst2);
            return;
        }

        basics->queries = queries;
        basics->Run(empty);
        if (queries.size() > 1) basics->displayResults();
        dst2 = basics->dst2;

        // Extract the first nearest neighbor for each query
        neighbors.clear();
        if (basics->neighbors.size() > 1)
            for (const auto& neighborRow : basics->neighbors) {
                neighbors.push_back(neighborRow[0]);
            }

        // Resolve duplicate matches based on distances
        for (int i = 0; i < neighbors.size(); i++) {
            if (neighbors[i] == -1) continue;
            Point2f p1 = queries[i];
            Point2f ptn = basics->trainInput[neighbors[i]];
            for (int j = i + 1; j < neighbors.size(); j++) {
                if (neighbors[j] == neighbors[i]) {
                    Point2f p2 = queries[j];
                    double d1 = norm(p1 - ptn);
                    double d2 = norm(p2 - ptn);
                    neighbors[d1 > d2 ? i : j] = -1;
                }
            }
        }

        // Display results
        dst3.setTo(0);
        for (const Point2f& pt : basics->trainInput) {
            circle(dst3, pt, task->DotSize + 4, Scalar(0, 0, 255), -1, task->lineType);
        }

        noMatch.clear();
        matches.clear();
        for (int i = 0; i < neighbors.size(); i++) {
            Point2f pt = queries[i];
            circle(dst3, pt, task->DotSize + 4, Scalar(0, 255, 255), -1, task->lineType);
            if (neighbors[i] == -1) {
                noMatch.push_back(pt);
            }
            else {
                Point2f nn = basics->trainInput[neighbors[i]];
                matches.emplace_back(pt, nn);
                line(dst3, nn, pt, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            }
        }

        if (!standalone) {
            basics->trainInput = queries;
        }
    }
};







class Delaunay_Generations_CC : public CPP_Parent {
private:
public:
    vector<Point2f> inputPoints;
    Delaunay_Basics_CC* facet;
    KNN_Basics_CC* knn;
    Random_Basics_CC* random;
    Mat generationMap;

    Delaunay_Generations_CC() : CPP_Parent()
    {
        generationMap = Mat::zeros(dst3.size(), CV_32S);
        knn = new KNN_Basics_CC();
        facet = new Delaunay_Basics_CC();
        random = new Random_Basics_CC();
        random->sizeRequest = 10;
        labels = {"", "Mask of unmatched regions - generation set to 0", "Facet Image with count for each region",
                    "Generation counts in CV_32SC1 format" };
        desc = "Create a region in an image for each point provided";
    }
    void Run(Mat src) {
        if (standalone) {
            if (task->heartBeat) random->Run(empty);
            inputPoints = random->pointList;
        }

        knn->queries = inputPoints;
        knn->Run(empty);

        facet->inputPoints = inputPoints;
        facet->Run(src);
        dst2 = facet->dst2;

        vector<int> usedG;
        int g;
        for (const PointPair& mp : knn->matches) {
            int index = facet->facet32s.at<int>(mp.p2.y, mp.p2.x);
            if (index >= facet->facetlist.size()) continue;
            const vector<Point>& nextFacet = facet->facetlist[index];

            // Ensure unique generation numbers
            if (task->FirstPass) {
                g = (int)usedG.size();
            }
            else {
                g = generationMap.at<int>(mp.p2.y, mp.p2.x) + 1;
                while (find(usedG.begin(), usedG.end(), g) != usedG.end()) {
                    g++;
                }
            }

            fillConvexPoly(generationMap, nextFacet, g, task->lineType);
            usedG.push_back(g);
            task->SetTrueText(to_string(g), dst2, mp.p2);
        }
        dst3 = generationMap.clone();
    }
};
    



class Feature_Stable_CC : public CPP_Parent {
private:
    cv::Ptr<cv::BRISK> Brisk;
public:
    vector<cv::Point2f> featurePoints;
    Options_Features* options = new Options_Features;

    Feature_Stable_CC() : CPP_Parent() {
        Brisk = cv::BRISK::create();
        desc = "Find good features to track in a BGR image.";
    }

    void Run(cv::Mat src) {
        dst2 = src.clone();
        if (src.channels() == 3) cvtColor(src, src, COLOR_BGR2GRAY);
        featurePoints.clear();
        int sampleSize = options->featurePoints;
        if (options->useBRISK) {
            vector<cv::KeyPoint> keyPoints;
            Brisk->detect(src, keyPoints);
            for (const auto& kp : keyPoints) {
                if (kp.size >= options->minDistance) featurePoints.push_back(kp.pt);
            }
        }
        else {
            cv::goodFeaturesToTrack(src, featurePoints, sampleSize, options->quality, options->minDistance);
        }
        cv::Scalar color = (dst2.channels() == 3) ? cv::Scalar(0, 255, 255) : cv::Scalar(255, 255, 255);
        for (const auto& pt : featurePoints) {
            cv::circle(dst2, pt, task->DotSize, color, -1, task->lineType);
        }
        labels[2] = "Found " + to_string(featurePoints.size()) + " points with quality = " + to_string(options->quality) +
            " and minimum distance = " + to_string(options->minDistance);
    }
};





class Stable_Basics_CC : public CPP_Parent {
public:
    Delaunay_Generations_CC* facetGen;
    vector<Point2f> ptList;
    Point2f anchorPoint;
    Feature_Stable_CC* good;

    Stable_Basics_CC() : CPP_Parent() {
        good = new Feature_Stable_CC();
        facetGen = new Delaunay_Generations_CC();
        desc = "Maintain the generation counts around the feature points.";
    }

    void Run(Mat src) {
        if (standalone) {
            good->Run(src);
            facetGen->inputPoints = good->featurePoints;
        }

        facetGen->Run(src);
        if (facetGen->inputPoints.empty()) return; // nothing to work on

        ptList.clear();
        vector<int> generations;
        for (const Point2f& pt : facetGen->inputPoints) {
            int fIndex = facetGen->facet->facet32s.at<int>(pt.y, pt.x);
            if (fIndex >= facetGen->facet->facetlist.size()) continue; // new point
            int g = facetGen->dst3.at<int>(pt.y, pt.x);
            generations.push_back(g);
            ptList.push_back(pt);
            task->SetTrueText(to_string(g), dst2, pt);
        }

        int maxGens = *max_element(generations.begin(), generations.end());
        int index = distance(generations.begin(), find(generations.begin(), generations.end(), maxGens));
        anchorPoint = ptList[index];
        if (index < facetGen->facet->facetlist.size()) {
            const vector<Point>& bestFacet = facetGen->facet->facetlist[index];
            fillConvexPoly(dst2, bestFacet, Scalar(0, 0, 0), task->lineType);
            drawContours(dst2, vector<vector<Point>> {bestFacet}, -1, Scalar(0, 255, 255), task->lineWidth);
        }

        dst2 = facetGen->dst2;
        dst3 = src.clone();
        for (const Point2f& pt : ptList) {
            circle(dst2, pt, task->DotSize, Scalar(0, 255, 255), task->lineWidth, task->lineType);
            circle(dst3, pt, task->DotSize, Scalar(255, 255, 255), task->lineWidth, task->lineType);
        }

        string text = to_string(ptList.size()) + " stable points were identified with " + to_string(maxGens) + " generations at the anchor point";
        labels[2] = text;
    }
};





class Stable_BasicsCount_CC : public CPP_Parent
{
private:
public:
    Stable_Basics_CC* basics;
    Feature_Stable_CC* good;
    map<int, int, greater<float>> goodCounts;
    Stable_BasicsCount_CC() : CPP_Parent()
    {
        good = new Feature_Stable_CC();
        basics = new Stable_Basics_CC();
        desc = "Track the stable good features found in the BGR image.";
    }
    void Run(Mat src)
    {
        good->Run(src);
        basics->facetGen->inputPoints = good->featurePoints;
        basics->Run(src);
        dst2 = basics->dst2;
        dst3 = basics->dst3;

        goodCounts.clear();
        int g;
        for (auto i = 0; i < basics->ptList.size(); i++)
        {
            Point2f pt = basics->ptList[i];
            circle(dst2, pt, task->DotSize, YELLOW, task->lineWidth, task->lineType);
            g = basics->facetGen->dst3.at<int>(pt.y, pt.x);
            goodCounts[g] = i;
            task->SetTrueText(to_string(g), dst2, pt);
        }
    }
};


class FPoly_TopFeatures_CC : public CPP_Parent {
public:
    Stable_BasicsCount_CC* stable;
    vector<Point2f> poly;

    FPoly_TopFeatures_CC() : CPP_Parent() {
        stable = new Stable_BasicsCount_CC();
        desc = "Get the top features and validate them";
    }

    void Run(Mat src) {
        stable->Run(src);
        dst2 = stable->dst2;
        poly.clear();
        for (const auto& keyVal : stable->goodCounts) {
            const Point2f& pt = stable->basics->ptList[keyVal.second];
            int g = stable->basics->facetGen->dst3.at<int>(pt.y, pt.x);

            if (poly.size() < task->polyCount) poly.push_back(pt);
        }

        if (poly.size() > 1)
            for (int i = 0; i < poly.size() - 1; i++) {
                line(dst2, poly[i], poly[i + 1], Scalar(255, 255, 255), task->lineWidth, task->lineType);
            }
    }
};






class Edge_Scharr_CC : public CPP_Parent {
public:
    Edge_Scharr_CC() : CPP_Parent() {
        desc = "Scharr is most accurate with 3x3 kernel.";
    }

    void Run(Mat src) {
        float optionScharrMultiplier = 50;
        Mat gray;
        cvtColor(src, gray, COLOR_BGR2GRAY);

        Mat xField, yField;
        Scharr(gray, xField, CV_32FC1, 1, 0);
        Scharr(gray, yField, CV_32FC1, 0, 1);

        add(xField, yField, dst0);
        dst0.convertTo(dst2, CV_8U, optionScharrMultiplier / 100.0);
    }
};



class Depth_Colorizer_CC : public CPP_Parent {
public:
    Depth_Colorizer_CC() : CPP_Parent() {
        desc = "Colorize the depth based on the near and far colors.";
    }

    void Run(Mat src) {
        if (src.type() != CV_32F) {
            src = task->pcSplit[2];
        }

        dst2.setTo(0);
        Scalar nearColor = Scalar(0, 1.0f, 1.0f);
        Scalar farColor = Scalar(1.0f, 0, 0);
        for (int y = 0; y < src.rows; y++) {
            for (int x = 0; x < src.cols; x++) {
                float pixel = src.at<float>(y, x);
                if (pixel != 0)
                    int k = 0;
                if (pixel > 0 && pixel <= task->MaxZmeters) {
                    float t = pixel / task->MaxZmeters;
                    Scalar colorS = 255 * ((1 - t) * nearColor + t * farColor);
                    Vec3b color = Vec3b(colorS[0], colorS[1], colorS[2]);
                    dst2.at<Vec3b>(y, x) = color;
                }
            }
        }
    }
};



class RedCloud_Reduce_CC : public CPP_Parent {
public:
    int classCount;
    int givenClassCount = 0;
    int reduceAmt = 250;
    RedCloud_Reduce_CC() : CPP_Parent() {
        //if (standalone) {
        //    task.redOptions.RedCloud_Reduce.Checked = true;
        //}
        desc = "Reduction transform for the point cloud";
    }
    void Run(Mat src) {
        task->pointCloud.convertTo(dst0, CV_32S, 1000.0 / reduceAmt);
        vector<Mat> msplit;
        split(dst0, msplit);
        switch (task->PCReduction) {
        case 0: // "X Reduction":
            dst0 = msplit[0] * reduceAmt;
            break;
        case 1: // "Y Reduction":
            dst0 = msplit[1] * reduceAmt;
            break;
        case 2: // "Z Reduction":
            dst0 = msplit[2] * reduceAmt;
            break;
        case 3: // "XY Reduction":
            dst0 = msplit[0] * reduceAmt + msplit[1] * reduceAmt;
            break;
        case 4: // "XZ Reduction":
            dst0 = msplit[0] * reduceAmt + msplit[2] * reduceAmt;
            break;
        case 5: // "YZ Reduction":
            dst0 = msplit[1] * reduceAmt + msplit[2] * reduceAmt;
            break;
        case 6: // "XYZ Reduction":
            dst0 = msplit[0] * reduceAmt + msplit[1] * reduceAmt + msplit[2] * reduceAmt;
            break;
        }
        double minVal, maxVal;
        minMaxLoc(dst0, &minVal, &maxVal);
        dst2 = dst0 - minVal;
        dst2.setTo(maxVal - minVal, task->maxDepthMask);

        minMaxLoc(dst2, &minVal, &maxVal);
        classCount = 255 - givenClassCount - 1;
        dst2 *= (double)classCount / maxVal;
        dst2 += givenClassCount + 1;
        dst2.convertTo(dst2, CV_8U);
        labels[2] = "Reduced Pointcloud - reduction factor = " + to_string(reduceAmt) + " produced " + to_string(classCount) + " regions";
    }
};







class Depth_PointCloud_CC : public CPP_Parent {
private: 
public: 
	Depth_PointCloud_CC() : CPP_Parent()
    {
        desc = "Display the contents of the point cloud as a 2D image";
    }
	void Run(Mat src)
	{
        if (src.type() != CV_32FC3) src = task->pointCloud.clone();
        Mat mSplit[3];
        split(src, mSplit);
        convertScaleAbs(mSplit[0], mSplit[0], 255);
        convertScaleAbs(mSplit[1], mSplit[1], 255);
        convertScaleAbs(mSplit[2], mSplit[2], 255);
        vector<Mat> channels = { mSplit[0], mSplit[1], mSplit[2] };
        merge(channels, dst2);
        dst2.convertTo(dst2, CV_8UC3);
        dst3 = task->depth32f > 0;
    }
};






class IMU_GMatrix_QT_CC : public CPP_Parent {
private:
public:
    float cx = 1.0f, sx = 0.0f, cy = 1.0f, sy = 0.0f, cz = 1.0f, sz = 0.0f;
    bool usingSliders = false;
    IMU_GMatrix_QT_CC() : CPP_Parent() {
        desc = "Find the angle of tilt for the camera with respect to gravity without any options_";
    }
    void Run(Mat src) {
        if (usingSliders == false) {
            cz = cos(task->accRadians.z);
            sz = sin(task->accRadians.z);
            cx = cos(task->accRadians.x);
            sx = sin(task->accRadians.x);
        }
        float gMt[3][3] = { {cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                            {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz}, 
                            {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz} };

        float gMatrix[3][3] = {{gMt[0][0] * cy  + gMt[0][1] * 0 + gMt[0][2] * sy,
                                gMt[0][0] * 0   + gMt[0][1] * 1 + gMt[0][2] * 0,
                                gMt[0][0] * -sy + gMt[0][1] * 0 + gMt[0][2] * cy},
                                {gMt[1][0] * cy  + gMt[1][1] * 0 + gMt[1][2] * sy,
                                gMt[1][0] * 0   + gMt[1][1] * 1 + gMt[1][2] * 0,
                                gMt[1][0] * -sy + gMt[1][1] * 0 + gMt[1][2] * cy},
                                {gMt[2][0] * cy  + gMt[2][1] * 0 + gMt[2][2] * sy,
                                gMt[2][0] * 0   + gMt[2][1] * 1 + gMt[2][2] * 0,
                                gMt[2][0] * -sy + gMt[2][1] * 0 + gMt[2][2] * cy}};
        task->gMatrix = Mat(3, 3, CV_32F, gMatrix).clone();
        task->SetTrueText("task->gMatrix is set...", dst2);
    }
};







class IMU_GMatrix_CC : public CPP_Parent
{
private:
public:
    string strOut;
    IMU_GMatrix_QT_CC* qt;
    IMU_GMatrix_CC() : CPP_Parent() {
        qt = new IMU_GMatrix_QT_CC();
        qt->usingSliders = true;
        desc = "Find the angle of tilt for the camera with respect to gravity.";
    }
    void Run(Mat src) {
        qt->cz = cos(task->accRadians.z);
        qt->sz = sin(task->accRadians.z);
        qt->cx = cos(task->accRadians.x);
        qt->sx = sin(task->accRadians.x);
        //qt->cy = cos(rotateY * PI / 180);
        //qt->sy = sin(rotateY * PI / 180);
        qt->Run(src);
        task->SetTrueText("task->gMatrix is set...", dst2);
    }
};







class Depth_PointCloud_IMU_CC : public CPP_Parent
{
private:
public:
    float option_resizeFactor = 1;
    IMU_GMatrix_QT_CC* gMatrix;
    Depth_PointCloud_CC* cloud;
    Depth_PointCloud_IMU_CC() : CPP_Parent() {
        gMatrix = new IMU_GMatrix_QT_CC();
        cloud = new Depth_PointCloud_CC();
        desc = "Rotate the PointCloud around the X-axis and the Z-axis using the gravity vector from the IMU.";
    }
    void Run(Mat src) {
        if (src.type() != CV_32FC3) src = task->pointCloud.clone();
        gMatrix->Run(src);
        Mat gOutput = src.reshape(1, src.rows * src.cols);
        gOutput *= task->gMatrix;
        dst0 = gOutput.reshape(3, src.rows);
        task->gCloud = dst0;
        if (option_resizeFactor != 1) {
            resize(dst0, dst1, Size(int((dst2.cols * option_resizeFactor)), int((dst2.rows * option_resizeFactor))));
        }
        task->SetTrueText("gCloud is ready for use", dst3);

        cloud->Run(task->gCloud);
        dst2 = cloud->dst2;
    }
};




class Edge_Sobel_CC : public CPP_Parent {
public:
    bool horizontalDerivative = true;
    bool verticalDerivative = true;
    int kernelSize = 3;
    AddWeighted_Basics_CC* addw;
    Edge_Sobel_CC() : CPP_Parent() {
        labels = { "", "", "Horizontal derivative", "Vertical derivative" };
        desc = "Show Sobel edge detection with varying kernel sizes and directions.";
    }

    void Run(Mat src) {
        if (src.channels() == 3) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }

        if (horizontalDerivative) {
            Sobel(src, dst2, CV_32F, 1, 0, kernelSize);
            convertScaleAbs(dst2, dst2);
        }

        if (verticalDerivative) {
            Sobel(src, dst3, CV_32F, 0, 1, kernelSize);
            convertScaleAbs(dst3, dst3);
        }
    }
};




class Binarize_Simple_CC : public CPP_Parent {
public:
    Scalar meanScalar;
    int injectVal = 255;

    Binarize_Simple_CC() : CPP_Parent() {
        desc = "Binarize an image using Threshold with OTSU.";
    }

    void Run(Mat src) {
        if (src.channels() == 3) cvtColor(src, src, COLOR_BGR2GRAY);
        meanScalar = mean(src);
        threshold(src, dst2, meanScalar[0], injectVal, THRESH_BINARY);
    }
};





class Plot_Histogram_CC : public CPP_Parent {
public:
    Mat histogram;
    float minRange = 0;
    float maxRange = 255;
    Scalar backColor = Scalar(0, 0, 255);
    float maxValue;
    float minValue;
    float plotCenter;
    float barWidth;
    bool addLabels = true;
    bool removeZeroEntry = true;
    bool createHistogram = false;

    Plot_Histogram_CC() : CPP_Parent() {
        desc = "Plot histogram data with a stable scale at the left of the image.";
    }

    void Run(Mat src) {
        if (standalone || createHistogram) {
            if (src.channels() != 1) {
                cvtColor(src, src, COLOR_BGR2GRAY);
            }
            int bins[] = { task->histogramBins };
            float hRange[] = { minRange, maxRange };
            const float* range[] = { hRange };
            calcHist(&src, 1, { 0 }, Mat(), histogram, 1, bins, range, true, false);
        }
        else {
            histogram = src;
        }

        if (removeZeroEntry) {
            histogram.at<float>(0) = 0;
        }

        dst2 = backColor;
        barWidth = dst2.cols / histogram.rows;
        plotCenter = barWidth * histogram.rows / 2 + barWidth / 2;

        vector<float> histArray(histogram.rows);
        memcpy(histArray.data(), histogram.ptr<float>(), histArray.size() * sizeof(float));

        double minVal, maxVal;
        minMaxIdx(histogram, &minVal, &maxVal);

        if (maxVal > 0 && histogram.rows > 0) {
            int incr = 255 / histogram.rows;
            for (int i = 0; i < histArray.size(); i++) {
                if (isnan(histArray[i])) {
                    histArray[i] = 0;
                }
                if (histArray[i] > 0) {
                    int h = cvRound(histArray[i] * dst2.rows / maxVal);
                    int sIncr = (i % 256) * incr;
                    Scalar color(sIncr, sIncr, sIncr);
                    if (histogram.rows > 255) {
                        color = Scalar(0, 0, 0);
                    }
                    rectangle(dst2, Rect(i * barWidth, dst2.rows - h, fmax(1, barWidth), h), color, -1);
                }
            }
            if (addLabels) {
                task->AddPlotScale(dst2, minVal, maxVal);
            }
        }
    }
};





class Hist_Basics_CC : public CPP_Parent
{
public:
    Mat histogram;
    mmData mm;
    Plot_Histogram_CC* plot;
    vector<Range> ranges;
    int splitIndex = 0;
    Hist_Basics_CC() : CPP_Parent()
    {
        plot = new Plot_Histogram_CC();
        desc = "Create a histogram (no Kalman)";
    }

    void Run(Mat src) {
        if (standalone) {
            if (task->heartBeat) {
                splitIndex = (splitIndex + 1) % 3;
            }

            Mat msplit[3];
            split(src, msplit);
            src = msplit[splitIndex];

            plot->backColor = Scalar(splitIndex == 0 ? 255 : 0,
                                    splitIndex == 1 ? 255 : 0,
                                    splitIndex == 2 ? 255 : 0);
        }
        else {
            if (src.channels() != 1) {
                cvtColor(src, src, COLOR_BGR2GRAY);
            }
        }
        mm = task->vbMinMax(src);

        float histDelta = 0.001f;
        int bins[] = { task->histogramBins };
        float hRange[] = { (float)(mm.minVal - histDelta), (float)(mm.maxVal + histDelta) };
        const float* range[] = { hRange };
        calcHist(&src, 1, { 0 }, Mat(), histogram, 1, bins, range, true, false);

        plot->Run(histogram);
        histogram = plot->histogram;


        dst2 = plot->dst2;
        labels[2] = " histogram, bins = " +
                    to_string(task->histogramBins) + ", X ranges from " + to_string(int(mm.minVal)) +
                    " to " + to_string(int(mm.maxVal)) + ", y is sample count";
        labels[2] = (splitIndex == 0 ? "Blue" : splitIndex == 1 ? "Green" : "Red") + labels[2];
    }
};


class Kalman_Simple {
public:
    KalmanFilter* kf;
    Mat processNoise = Mat(2, 1, CV_32F);
    Mat measurement = Mat(1, 1, CV_32F, 0.0);
    float inputReal;
    float stateResult;
    float processNoiseCov = 0.00001f;
    float measurementNoiseCov = 0.1f;
    float errorCovPost = 1;
    float transitionMatrix[4] = { 1, 1, 0, 1 };  // Change externally and set new TransmissionMatrix
    bool newTMatrix = true;

    void updateTMatrix()
    {
        kf->transitionMatrix = Mat(2, 2, CV_32F, transitionMatrix);
        kf->measurementMatrix = Mat::eye(1, 2, CV_32F);  // Set identity
        kf->processNoiseCov = Mat::eye(2, 2, CV_32F) * processNoiseCov;
        kf->measurementNoiseCov = Mat::eye(1, 1, CV_32F) * measurementNoiseCov;
        kf->errorCovPost = Mat::eye(2, 2, CV_32F) * errorCovPost;
    }
    Kalman_Simple() {
        kf = new KalmanFilter(2, 1, 0);
    }

    void Run() {
        if (newTMatrix)
        {
            newTMatrix = false;
            updateTMatrix();
        }

        Mat prediction = kf->predict();
        measurement.at<float>(0, 0) = inputReal;
        stateResult = kf->correct(measurement).at<float>(0, 0);
    }

    ~Kalman_Simple() {}
};



class Kalman_Basics_CC : public CPP_Parent {
public:
    vector<Kalman_Simple> kalman;
    vector<float> kInput = { 0, 0, 0, 0 };
    vector<float> kOutput;
    int saveDimension = -1;

    Kalman_Basics_CC() : CPP_Parent() {
        desc = "Use Kalman to stabilize values (such as a Rect)";
    }

    void Run(Mat src) {
        if (task->optionsChanged) {
            kalman.clear();
            saveDimension = int(kInput.size());
            for (int i = 0; i < kInput.size(); i++) {
                kalman.push_back(Kalman_Simple());
            }
            kOutput.resize(kInput.size());
        }

        if (task->UseKalman) {
            for (int i = 0; i < kalman.size(); i++) {
                kalman[i].inputReal = kInput[i];
                kalman[i].Run();
                if (isnan(kalman[i].stateResult)) {
                    kalman[i].stateResult = kalman[i].inputReal;
                }
                kOutput[i] = kalman[i].stateResult;
            }
        }
        else {
            kOutput = kInput;
        }

        if (standalone) {
            dst2 = src.clone();
            Rect rect(cvRound(kOutput[0]), cvRound(kOutput[1]), cvRound(kOutput[2]), cvRound(kOutput[3]));
            rect = task->validateRect(rect, dst2.cols, dst2.rows);
            static Rect lastRect = rect;
            if (rect == lastRect) {
                Rect r = task->InitRandomRect(src.rows <= 240 ? 20 : 50, dst2.cols);
                kInput = { float(r.x), float(r.y), float(r.width), float(r.height) };
            }
            lastRect = rect;
            rectangle(dst2, rect, Scalar(255, 255, 255), task->lineWidth + 1);
            rectangle(dst2, rect, Scalar(0, 0, 255), task->lineWidth);
        }
    }
};





class Hist_Kalman_CC : public CPP_Parent {
public:
    Hist_Basics_CC* hist;
    Kalman_Basics_CC* kalman;

    Hist_Kalman_CC() : CPP_Parent() {
        kalman = new Kalman_Basics_CC();
        hist = new Hist_Basics_CC();
        labels = { "", "", "With Kalman", "Without Kalman" };
        desc = "Use Kalman to smooth the histogram results.";
    }

    void Run(Mat src) {
        hist->Run(src);
        dst3 = hist->dst2.clone();

        if (hist->histogram.empty()) {
            hist->histogram = Mat(task->histogramBins, 1, CV_32F, 0);
        }

        if (kalman->kInput.size() != task->histogramBins) {
            kalman->kInput.resize(task->histogramBins);
        }
        for (int i = 0; i < task->histogramBins; i++) {
            kalman->kInput[i] = hist->histogram.at<float>(i, 0);
        }

        kalman->Run(src);

        hist->histogram = Mat(int(kalman->kOutput.size()), 1, CV_32FC1, kalman->kOutput.data());
        hist->plot->Run(hist->histogram);
        dst2 = hist->dst2;
    }
};





class BackProject_Basics_CC : public CPP_Parent {
public:
    Hist_Kalman_CC* histK;
    Scalar minRange, maxRange;

    BackProject_Basics_CC() : CPP_Parent() {
        histK = new Hist_Kalman_CC();
        labels[2] = "Move mouse to backproject a histogram column";
        desc = "Mouse over any bin to see the color histogram backprojected.";
    }

    void Run(Mat src) {
        Mat input = src.clone();
        if (input.channels() != 1) {
            cvtColor(input, input, COLOR_BGR2GRAY);
        }

        histK->Run(input);
        if (histK->hist->mm.minVal == histK->hist->mm.maxVal) {

            task->SetTrueText("The input image is empty - mm.minVal and mm.maxVal are both zero...", dst2);
            return;
        }

        dst2 = histK->dst2;

        int totalPixels = int(dst2.total());
        //if (histK->hist->plot.removeZeroEntry) {
        //    totalPixels = countNonZero(input);
        //}

        int brickWidth = dst2.cols / task->histogramBins;
        float incr = (histK->hist->mm.maxVal - histK->hist->mm.minVal) / task->histogramBins;
        int histIndex = floor(task->mouseMovePoint.x / brickWidth);
        if (histIndex >= task->histogramBins) histIndex = task->histogramBins - 1;

        minRange = Scalar(histIndex * incr);
        maxRange = Scalar((histIndex + 1) * incr);
        if (histIndex == task->histogramBins) {
            maxRange = Scalar(255);
        }

        inRange(input, minRange, maxRange, dst0);

        int actualCount = countNonZero(dst0);

        dst3 = task->color.clone();
        dst3.setTo(Scalar(0, 255, 255), dst0);

        auto count = histK->hist->histogram.at<float>(histIndex, 0);
        mmData histMax = task->vbMinMax(histK->hist->histogram);

        labels[3] = "Backprojecting " + to_string(minRange(0)) + " to " + to_string(maxRange(0)) +
            " with " + to_string(count) + " of " + to_string(totalPixels) + " samples compared to " +
            " mask pixels = " + to_string(actualCount) +
            " Histogram max count = " + to_string(histMax.maxVal);
        rectangle(dst2, Rect(histIndex * brickWidth, 0, brickWidth, dst2.rows), Scalar(0, 255, 255), task->lineWidth);
    }
};




class Rectangle_Basics_CC : public CPP_Parent {
public:
    vector<Rect> rectangles;
    vector<RotatedRect> rotatedRectangles;
    int options_DrawCount = 3;
    bool options_drawFilled = false;
    bool options_drawRotated = false;

    Rectangle_Basics_CC() : CPP_Parent() {
        desc = "Draw the requested number of rectangles.";
    }

    void Run(Mat src) {
        if (task->heartBeat) {
            dst2 = Mat::zeros(src.size(), CV_8UC3);
            rectangles.clear();
            rotatedRectangles.clear();

            for (int i = 0; i < options_DrawCount; i++) {
                Point2f nPoint = Point2f(rand() % src.cols, rand() % src.rows);
                auto width = rand() % int(src.cols - nPoint.x);
                auto height = rand() % int(src.rows - nPoint.y);
                auto eSize = Size2f(float(rand() % src.cols - nPoint.x - 1), float(rand() % src.rows - nPoint.y - 1));
                auto angle = 180.0F * float(rand() % 1000) / 1000.0f;
                auto nextColor = Scalar(task->vecColors[i].val[0], task->vecColors[i].val[1], task->vecColors[i].val[2]);
                auto rr = RotatedRect(nPoint, eSize, angle);
                Rect r = Rect(nPoint.x, nPoint.y, width, height);

                if (options_drawRotated) {
                    task->DrawRotatedRect(rr, dst2, nextColor);
                }
                else {
                    rectangle(dst2, r, nextColor, options_drawFilled ? -1 : 1);
                }

                rotatedRectangles.push_back(rr);
                rectangles.push_back(r);
            }
        }
    }
};




class Rectangle_Rotated_CC : public CPP_Parent {
public:
    Rectangle_Basics_CC* rectangle;

    Rectangle_Rotated_CC() : CPP_Parent() {
        rectangle = new Rectangle_Basics_CC();
        rectangle->options_drawRotated = true;
        desc = "Draw the requested number of rectangles.";
    }

    void Run(Mat src) {
        rectangle->Run(src);
        dst2 = rectangle->dst2;
    }
};




class Contour_Largest_CC : public CPP_Parent {
public:
    vector<Point> bestContour;
    vector<vector<Point>> allContours;
    RetrievalModes options_retrievalMode = RetrievalModes::RETR_LIST;
    ContourApproximationModes options_approximationMode = ContourApproximationModes::CHAIN_APPROX_NONE;
    int maxIndex;
    Rectangle_Rotated_CC* rotatedRect;

    Contour_Largest_CC() : CPP_Parent() {
        rotatedRect = new Rectangle_Rotated_CC();
        labels = { "", "", "Input to FindContours", "Largest single contour in the input image." };
        desc = "Create a mask from the largest contour of the input.";
    }

    void Run(Mat src) {
        if (standalone) {
            if (task->heartBeat) {
                rotatedRect->Run(src);
                dst2 = rotatedRect->dst2;
            }
        }
        else {
            dst2 = src;
        }

        if (dst2.channels() != 1) cvtColor(dst2, dst2, COLOR_BGR2GRAY);
        if (options_retrievalMode == RETR_FLOODFILL) {
            dst2.convertTo(dst1, CV_32SC1);
            findContours(dst1, allContours, noArray(), options_retrievalMode, options_approximationMode);
            dst1.convertTo(dst3, CV_8UC1);
        }
        else {
            findContours(dst2, allContours, noArray(), options_retrievalMode, options_approximationMode);
        }

        int maxCount = 0;
        maxIndex = -1;
        if (allContours.empty()) return;
        for (int i = 0; i < allContours.size(); i++) {
            int len = int(allContours[i].size());
            if (len > maxCount) {
                maxCount = len;
                maxIndex = i;
            }
        }
        bestContour = allContours[maxIndex];
        if (standalone) {
            dst3 = Mat::zeros(dst2.size(), CV_8UC1);
            if (maxIndex >= 0 && maxCount >= 2) {
                drawContours(dst3, allContours, maxIndex, Scalar(255), -1, task->lineType);
            }
        }
    }
};




class Diff_Basics_CC : public CPP_Parent {
public:
    int changedPixels;
    Mat lastGray;

    Diff_Basics_CC() : CPP_Parent() {
        labels = {"", "", "Stable gray", "Unstable mask"};
        desc = "Capture an image and compare it to previous frame using absDiff and threshold";
    }

    void Run(Mat src) override {
        if (src.channels() == 3) cvtColor(src, src, COLOR_BGR2GRAY);
        if (task->FirstPass) lastGray = src.clone();
        if (task->optionsChanged || lastGray.size() != src.size()) {
            lastGray = src.clone();
            dst3 = src.clone();
        }

        absdiff(src, lastGray, dst0);
        threshold(dst0, dst3, task->pixelDiffThreshold, 255, THRESH_BINARY);
        changedPixels = countNonZero(dst3);
        if (changedPixels > 0) {
            threshold(dst0, dst3, task->pixelDiffThreshold, 255, THRESH_BINARY);
            dst2 = src.clone();
            dst2.setTo(0, dst3);
            lastGray = src.clone();
        }
    }
};




class ApproxPoly_FindandDraw_CC : public CPP_Parent {
public:
    Rectangle_Rotated_CC* rotatedRect;
    vector<vector<Point>> allContours;
    ApproxPoly_FindandDraw_CC() : CPP_Parent() {
        rotatedRect = new Rectangle_Rotated_CC();
        labels[2] = "FindandDraw input";
        labels[3] = "FindandDraw output - note the change in line width where ApproxPoly differs from DrawContours";
        desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours.";
    }
    void Run(Mat src) override {
        rotatedRect->Run(src);
        dst2 = rotatedRect->dst2;
        cvtColor(dst2, dst0, COLOR_BGR2GRAY);
        threshold(dst0, dst0, 1, 255, THRESH_BINARY);
        dst0.convertTo(dst1, CV_32SC1);
        findContours(dst1, allContours, noArray(), RETR_FLOODFILL, CHAIN_APPROX_SIMPLE);
        dst3 = Mat::zeros(dst2.size(), CV_8UC1);
        vector<vector<Point>> contours;
        vector<Point> nextContour;
        for (int i = 0; i < allContours.size(); i++) {
            approxPolyDP(allContours[i], nextContour, 3, true);
            if (nextContour.size() > 2) contours.push_back(nextContour);
        }
        drawContours(dst3, contours, -1, Scalar(0, 255, 255), task->lineWidth, task->lineType);
    }
};




class ApproxPoly_Basics_CC : public CPP_Parent {
public:
    Contour_Largest_CC* contour;
    Rectangle_Rotated_CC* rotatedRect;
    ApproxPoly_Basics_CC() : CPP_Parent() {
        contour = new Contour_Largest_CC();
        rotatedRect = new Rectangle_Rotated_CC();
        labels = { "", "", "Input to the ApproxPolyDP", "Output of ApproxPolyDP" };
        desc = "Using the input contours, create ApproxPoly output";
    }
    void Run(Mat src) override {
        double epsilon = 3; // getSliderValue("epsilon - max distance from original curve");
        bool closedPoly = true; // getCheckBoxState("Closed polygon - connect first and last vertices.");
        if (standalone) {
            if (task->heartBeat) rotatedRect->Run(src);
            dst2 = rotatedRect->dst2;
        }
        contour->Run(dst2);
        dst2 = contour->dst2;
        if (contour->allContours.size()) {
            vector<Point> nextContour;
            approxPolyDP(contour->bestContour, nextContour, epsilon, closedPoly);
            dst3 = Mat::zeros(dst2.size(), CV_8UC1);
            task->drawContour(dst3, nextContour, Scalar(0, 255, 255));
        }
        else {
            // task->SetTrueText("No contours found", dst2);
        }
    }
};




class Hull_Basics_CC : public CPP_Parent {
public:
    Random_Basics_CC* random;
    vector<Point2f> inputPoints;
    vector<Point> hull;
    bool useRandomPoints;
    Hull_Basics_CC() : CPP_Parent() {
        random = new Random_Basics_CC();
        labels = { "", "", "Input Points - draw a rectangle anywhere. Enclosing rectangle in yellow.", "" };
        if (standalone) random->range = Rect(100, 100, 50, 50);
        desc = "Given a list of points, create a hull that encloses them.";
    }
    void Run(Mat src) override {
        if ((standalone && task->heartBeat) || (useRandomPoints && task->heartBeat)) {
            random->Run(empty);
            dst2 = random->dst2;
            inputPoints = random->pointList;
        }
        Mat hull2f;
        convexHull(inputPoints, hull2f);
        hull = task->convert2f2i(hull2f);
        dst2.setTo(0);
        task->drawContour(dst2, hull, Scalar(0, 255, 255));
    }
};





class ApproxPoly_Hull_CC : public CPP_Parent {
public:
    Hull_Basics_CC* hull;
    ApproxPoly_Basics_CC* aPoly;
    ApproxPoly_Hull_CC() : CPP_Parent() {
        hull = new Hull_Basics_CC();
        aPoly = new ApproxPoly_Basics_CC();
        hull->useRandomPoints = true;
        labels = { "", "", "Original Hull", "Hull after ApproxPoly" };
        desc = "Use ApproxPolyDP on a hull to show impact of options (which appears to be minimal - what is wrong?)";
    }
    void Run(Mat src) override {
        hull->Run(src);
        dst2 = hull->dst2;
        aPoly->Run(dst2);
        dst3 = aPoly->dst2;
    }
};





class RedCloud_Flood_CC : public CPP_Parent
{
private:
public:
    Mat inputMask;
    RedCloud_Reduce_CC* prepData;
    int option_loDiff = 0;
    int option_hiDiff = 0;
    int option_minSizeCell = 75;
    int options_historyMax = 10;
    bool options_highlightCell;
    int totalCount;
    vector<Rect>rects;
    vector<int> sizes;
    RedCloud_Flood_CC() : CPP_Parent() {
        prepData = new RedCloud_Reduce_CC();
        desc = "Perform the RedCloud low level FloodFill";
    }
    rcData buildZeroEntry() {
        rcData rc;
        Point pt;
        rc.contour = vector<Point>({ pt, pt, pt, pt });
        rc.hull = vector<Point>({ pt, pt, pt, pt });
        rc.rect = Rect(0, 0, 1, 1);
        rc.mask = Mat(1, 1, CV_8U);
        return rc;
    }
    void Run(Mat src) {
        if (src.type() != CV_32SC1) {
            if (task->gravityPointCloud)
                prepData->Run(task->gCloud);
            else
                prepData->Run(task->pointCloud);

            src = prepData->dst2;
        }

        if (inputMask.rows == 0) dst2 = task->noDepthMask; else dst2 = inputMask;
        rects.clear();
        sizes.clear();

        rects.push_back(Rect(0, 0, 1, 1));
        sizes.push_back(1);
        int cellCount = 1;

        src = src(Rect(1, 1, src.cols - 2, src.rows - 2)).clone();
        Rect rect;
        totalCount = 0;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                if (dst2.at<unsigned char>(y, x) == 0)
                {
                    int count = floodFill(src, dst2, Point(x, y), cellCount, &rect, option_loDiff, option_hiDiff,
                        4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE | (cellCount << 8));
                    if (option_minSizeCell < count)
                    {
                        rect.height += 1;
                        rects.push_back(rect);
                        sizes.push_back(count);
                        totalCount++;
                        if (++cellCount == 256) cellCount = 1;
                    }
                }
            }
        }
    }
};




enum FeatureSrc : uint8_t
{
    GoodFeaturesFull = 0,
    GoodFeaturesGrid = 1,
    Agast = 2,
    BRISK = 3,
    Harris = 4,
    FAST = 5
};




class Motion_Simple_CC : public CPP_Parent {
public:
    Diff_Basics_CC* diff;
    double cumulativePixels;
    float options_cumulativePercentThreshold = 0.1f;
    int options_motionThreshold;
    int saveFrameCount;
    Motion_Simple_CC() : CPP_Parent() {
        task->pixelDiffThreshold = 25;
        options_motionThreshold = dst2.rows * dst2.cols / 16;
        diff = new Diff_Basics_CC();
        dst3 = Mat::zeros(dst3.size(), CV_8U);
        labels[3] = "Accumulated changed pixels from the last heartbeat";
        desc = "Accumulate differences from the previous BGR image.";
    }
    void Run(Mat src) override {
        diff->Run(src);
        dst2 = diff->dst3;
        if (task->heartBeat) cumulativePixels = 0;
        if (diff->changedPixels > 0 || task->heartBeat) {
            cumulativePixels += diff->changedPixels;
            if (cumulativePixels / src.total() > options_cumulativePercentThreshold || diff->changedPixels > options_motionThreshold ||
                task->optionsChanged)
            {
                task->motionRect = Rect(0, 0, dst2.cols, dst2.rows);
            }
            if (task->motionRect.width == dst2.cols || task->heartBeat) {
                dst2.copyTo(dst3);
                cumulativePixels = 0;
                saveFrameCount = task->frameCount;
            }
            else {
                dst3.setTo(255, dst2);
            }
        }
        int threshold = src.total() * options_cumulativePercentThreshold;
        string strOut = "Cumulative threshold = " + to_string(threshold / 1000) + "k ";
        labels[2] = strOut + "Current cumulative pixels changed = " + to_string(cumulativePixels / 1000) + "k";
    }
};




class History_Basics_CC : public CPP_Parent {
public:
    vector<Mat> saveFrames;
    History_Basics_CC() : CPP_Parent() {
        desc = "Create a frame history to sum the last X frames";
    }
    void Run(Mat src) override {
        if (task->frameHistoryCount == 1) {
            dst2 = src.clone();
            return;
        }
        if (src.type() != CV_32F) src.convertTo(src, CV_32F);
        if (dst1.type() != src.type() || dst1.channels() != src.channels() || task->optionsChanged) {
            dst1 = src.clone();
            saveFrames.clear();
        }
        if (saveFrames.size() >= task->frameHistoryCount) {
            saveFrames.erase(saveFrames.begin());
        }
        saveFrames.push_back(src.clone());
        for (const auto& m : saveFrames) {
            add(dst1, m, dst1);
        }
        dst1 *= 1.0 / saveFrames.size();
        if (src.channels() == 1) {
            dst1.convertTo(dst2, CV_8U);
        }
        else {
            dst1.convertTo(dst2, CV_8UC3);
        }
    }
};






// https://docs.opencv.org/4.x/da/d22/tutorial_py_canny.html
class Edge_Canny_CC : public CPP_Parent {
public:
    Options_Canny* options = new Options_Canny();
    Edge_Canny_CC() : CPP_Parent() {
        desc = "Show canny edge detection with varying thresholds";
    }

    void Run(Mat src) {
        options->RunOpt();

        if (src.channels() == 3) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }

        if (src.depth() != CV_8U) {
            src.convertTo(src, CV_8U);
        }

        Canny(src, dst2, options->threshold1, options->threshold2, options->aperture, true);
    }
};







class Edge_MotionFrames_CC : public CPP_Parent {
public:
    Edge_Canny_CC* edges;
    History_Basics_CC* frames;
    Edge_MotionFrames_CC() : CPP_Parent() {
        edges = new Edge_Canny_CC();
        frames = new History_Basics_CC();
        labels = { "", "", "The multi-frame edges output", "The Edge_Canny output for the last frame only" };
        desc = "Collect edges over several frames controlled with global frame history";
    }
    void Run(Mat src) override {
        edges->Run(src);
        threshold(edges->dst2, dst3, 0, 255, THRESH_BINARY);
        frames->Run(edges->dst2);
        dst2 = frames->dst2;
    }
};





class Edge_Preserving_CC : public CPP_Parent {
public:
    int sigma_s = 10;
    double sigma_r = 40;
    Edge_Preserving_CC() : CPP_Parent() {
        task->drawRect = Rect(50, 50, 25, 25);
        labels = { "", "", "", "Edge preserving blur for BGR depth image above" };
        desc = "OpenCV's edge preserving filter.";
    }
    void Run(Mat src) override {
        dst2 = src;
        dst3 = task->depthRGB;
        edgePreservingFilter(dst2(task->drawRect), dst2(task->drawRect), sigma_s, sigma_r);
        edgePreservingFilter(task->depthRGB(task->drawRect), dst3(task->drawRect), sigma_s, sigma_r);
    }
};






class Resize_Preserve_CC : public CPP_Parent {
public:
    int options_resizePercent = 120;
    int options_topLeftOffset = 10;
    InterpolationFlags options_warpFlag = INTER_NEAREST; 
    Size newSize;
    Resize_Preserve_CC() : CPP_Parent() {
        desc = "Decrease the size but preserve the full image size.";
    }
    void Run(Mat src) override {
        newSize = Size(ceil(src.cols * options_resizePercent / 100.0),
        ceil(src.rows * options_resizePercent / 100.0));
        Mat dst0;
        resize(src, dst0, newSize, 0, 0, INTER_NEAREST);
        dst0.setTo(0);
        Rect rect(options_topLeftOffset, options_topLeftOffset, src.cols, src.rows);
        src.copyTo(dst0(rect));
        resize(dst0, dst2, dst2.size(), 0, 0, options_warpFlag);
        labels[2] = "Image after resizing to: " + to_string(newSize.width) + "X" + to_string(newSize.height);
    }
};




class Convex_Basics_CC : public CPP_Parent {
public: 
    vector<Point> hull;
    Convex_Basics_CC() : CPP_Parent() {
        desc = "Surround a set of random points with a convex hull";
        labels = { "", "", "Convex Hull - red dot is center and the black dots are the input points", "" };
    }
    vector<Point> buildRandomHullPoints() {
        int count = 10;
        int pad = 4; 
        int w = dst2.cols - dst2.cols / pad;
        int h = dst2.rows - dst2.rows / pad;
        random_device rd;
        mt19937 gen(rd());
        uniform_int_distribution<> dist_w(dst2.cols / pad, w);
        uniform_int_distribution<> dist_h(dst2.rows / pad, h);
        vector<Point> hullList;
        for (int i = 0; i < count; i++) {
            hullList.emplace_back(dist_w(gen), dist_h(gen));
        }
        return hullList;
    }
    void Run(Mat src) override {
        vector<Point> hullList = task->rcSelect.contour;
        if (standalone) {
            if (!task->heartBeat) return;
            hullList = buildRandomHullPoints();
        } 

        if (hullList.empty()) {
            task->SetTrueText("No points were provided. Update hullList before running.", dst2);
            return;
        }
        hull.clear();
        convexHull(hullList, hull);
        dst2.setTo(0);

        task->drawContour(dst2, hullList, Scalar(255, 255, 255), -1);
        for (int i = 0; i < hull.size(); i++) {
            line(dst2, hull[i], hull[(i + 1) % hull.size()], Scalar(255, 255, 255), task->lineWidth);
        }
    }
};




class Distance_Basics_CC : public CPP_Parent {
public:
    DistanceTypes options_distanceType = DIST_L1;
    int options_kernelSize = 0;
    Distance_Basics_CC() : CPP_Parent() {
        labels = { "", "", "Distance transform - create a mask with threshold", "" };
        desc = "Distance algorithm basics.";
    }
    void Run(Mat src) override {
        if (standalone) {
            src = task->depthRGB;
        }
        if (src.channels() == 3) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }
        distanceTransform(src, dst0, options_distanceType, options_kernelSize);
        normalize(dst0, dst0, 0, 255, NORM_MINMAX);
        dst0.convertTo(dst2, CV_8UC1);
    }
};




class Line_BasicsOld_CC : public CPP_Parent {
public: 
    Ptr<ximgproc::FastLineDetector> ld;
    map<float, int> sortLength;
    vector<PointPair> mpList;
    vector<Point2f> ptList;
    Rect subsetRect;
    int options_lineLengthThreshold = 20;
    // vector<tCell> tCells;
    Line_BasicsOld_CC() : CPP_Parent() {
        subsetRect = Rect(0, 0, dst2.cols, dst2.rows);
        dst3 = Mat::zeros(dst3.size(), CV_8U);
        ld = ximgproc::createFastLineDetector();
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present.";
    }
    void Run(Mat src) override {
        if (src.channels() == 3) {
            cvtColor(src, dst2, COLOR_BGR2GRAY);
        }
        else {
            dst2 = src.clone();
        }
        if (dst2.type() != CV_8U) {
            dst2.convertTo(dst2, CV_8U);
        }
        vector<Vec4f> lines;
        ld->detect(dst2(subsetRect), lines);
        sortLength.clear();
        mpList.clear();
        ptList.clear();
        //  tCells.clear();
        for (const Vec4f& v : lines) {
            int x1 = v[0] + subsetRect.x, y1 = v[1] + subsetRect.y;
            int x2 = v[2] + subsetRect.x, y2 = v[3] + subsetRect.y;
            if (0 <= x1 && x1 < dst2.cols && 0 <= y1 && y1 < dst2.rows &&
                0 <= x2 && x2 < dst2.cols && 0 <= y2 && y2 < dst2.rows) {
                Point p1(x1, y1), p2(x2, y2);
                if (norm(p1 - p2) >= options_lineLengthThreshold) {
                    PointPair mps(p1, p2);
                    mpList.push_back(mps);
                    ptList.push_back(p1);
                    ptList.push_back(p2);
                    sortLength[norm(p1 - p2)] = int(mpList.size()) - 1;
                    //tCells.push_back({ p1 });
                    //tCells.push_back({ p2 });
                }
            }
        }
        dst2 = src.clone();
        dst3.setTo(0);
        for (const auto& nextLine : sortLength) {
            const PointPair& mps = mpList[nextLine.second];
            line(dst2, mps.p1, mps.p2, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            line(dst3, mps.p1, mps.p2, 255, task->lineWidth, task->lineType);
        }
        labels[2] = to_string(mpList.size()) + " lines were detected in the current frame";
    }
};






class Edge_Segments_CC : public CPP_Parent {
private:
public:
    Ptr<EdgeDrawing> ed;
    Edge_Segments_CC() : CPP_Parent()
    {
        labels[2] = "Lines found with the Edge Drawing Filter";
        ed = createEdgeDrawing();
        ed->params.EdgeDetectionOperator = EdgeDrawing::SOBEL;
        ed->params.GradientThresholdValue = 38;
        ed->params.AnchorThresholdValue = 8;
        desc = "Example using the OpenCV ximgproc extention for edge drawing.";
    }
    void Run(Mat src)
    {
        if (src.channels() == 3)
        {
            dst2 = src;
            cvtColor(src, dst0, COLOR_BGR2GRAY);
        }
        else
        {
            dst0 = src;
            cvtColor(src, dst2, COLOR_GRAY2BGR);
        }
        ed->detectEdges(dst0);

        vector<Vec4f> lines;
        ed->detectLines(lines);

        for (size_t i = 0; i < lines.size(); i++)
        {
            Point2f p1 = Point2f(lines[i].val[0], lines[i].val[1]);
            Point2f p2 = Point2f(lines[i].val[2], lines[i].val[3]);
            line(dst2, p1, p2, YELLOW, task->lineWidth, task->lineType);
        }
    }
};




class EdgeDraw_Basics_CC : public CPP_Parent {
public:
    EdgeDraw* cPtr;
    EdgeDraw_Basics_CC() : CPP_Parent() {
        cPtr = new EdgeDraw;
        labels = { "", "", "EdgeDraw_Basics output", "" };
        desc = "Access EdgeDraw directly for efficiency";
    }
    void Run(Mat src) {
        if (src.channels() != 1) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }
        Mat* srcPtr = &src;
        int* imagePtr = EdgeDraw_RunCPP(cPtr, (int *)srcPtr->data, src.rows, src.cols, task->lineWidth);
        if (imagePtr != nullptr) {
            dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr);
            rectangle(dst2, Rect(0, 0, dst2.cols, dst2.rows), Scalar(255), task->lineWidth);
        }
    }
    ~EdgeDraw_Basics_CC() {
        EdgeDraw_Edges_Close(cPtr);
        delete cPtr;
    }
};






class FeatureLess_Basics_CC : public CPP_Parent {
public:
    EdgeDraw_Basics_CC* edgeD;
    FeatureLess_Basics_CC() : CPP_Parent()
    {
        edgeD = new EdgeDraw_Basics_CC();
        desc = "Access the EdgeDraw_Basics algorithm directly rather than through the Basics interface - more efficient";
    }
    void Run(Mat src) {
        edgeD->Run(src);
        dst2 = edgeD->dst2;
        if (standalone) {
            dst3 = src.clone();
            dst3.setTo(Scalar(0, 255, 255), dst2);
        }
    }
};



class FeatureLess_Edge_CC : public CPP_Parent
{
private:
public:
    Distance_Basics_CC* dist;
    EdgeDraw_Basics_CC* eDraw;
    FeatureLess_Edge_CC() : CPP_Parent()
    {
        eDraw = new EdgeDraw_Basics_CC();
        dist = new Distance_Basics_CC();
        labels[2] = "Edges found using the Segments and the distance function";
        desc = "Floodfill the output of the Edge Drawing filter (C++)";
    }
    void Run(Mat src)
    {
        eDraw->Run(src);
        bitwise_not(eDraw->dst2, dst0);
        dist->Run(dst0);
        threshold(dist->dst2, dst2, 0, 255, THRESH_BINARY);
    }
};








class Bezier_Basics_CC : public CPP_Parent {
public:
    vector<Point> points;

    Bezier_Basics_CC() : CPP_Parent() {
        advice = "Update the public points array and then Run.";
        points = { Point(100, 100),
                    Point(150, 50),
                    Point(250, 150),
                    Point(300, 100),
                    Point(350, 150),
                    Point(450, 50) };
        desc = "Use n points to draw a Bezier curve.";
    }

    Point nextPoint(vector<Point> points, int i, float t) {
        int x = pow(1 - t, 3) * points[i].x +
            3 * t * pow(1 - t, 2) * points[i + 1].x +
            3 * pow(t, 2) * (1 - t) * points[i + 2].x +
            pow(t, 3) * points[i + 3].x;

        int y = pow(1 - t, 3) * points[i].y +
            3 * t * pow(1 - t, 2) * points[i + 1].y +
            3 * pow(t, 2) * (1 - t) * points[i + 2].y +
            pow(t, 3) * points[i + 3].y;
        return Point(x, y);
    }

    void Run(Mat src) {
        Point p1;
        for (int i = 0; i < points.size() - 4; i += 3) {
            for (int j = 0; j <= 100; j++) {
                Point p2 = nextPoint(points, i, j / 100.0f);
                if (j > 0) {
                    line(dst2, p1, p2, task->HighlightColor, task->lineWidth);
                }
                p1 = p2;
            }
        }
        labels[2] = "Bezier output";
    }
};




class FeatureLess_History_CC : public CPP_Parent {
public:
    EdgeDraw_Basics_CC* edgeD;
    History_Basics_CC* frames;
    FeatureLess_History_CC() : CPP_Parent()
    {
        dst2 = Mat::zeros(dst2.size(), CV_8U); 
        edgeD = new EdgeDraw_Basics_CC();
        frames = new History_Basics_CC();
        desc = "Access the EdgeDraw_Basics algorithm directly rather than through the Basics interface - more efficient";
        task->frameHistoryCount = 10;
    }
    void Run(Mat src) {
        edgeD->Run(src);
        frames->Run(edgeD->dst2);
        dst2 = frames->dst2;
        if (standalone) {
            dst3 = src.clone();
            dst3.setTo(Scalar(0, 255, 255), dst2);
        }
    }
};





class Palette_Basics_CC : public CPP_Parent {
public:
    bool whitebackground;
    Palette_Basics_CC() : CPP_Parent() {
        desc = "Apply the different color maps in OpenCV";
    }
    void Run(Mat src) {
        if (src.type() == CV_32F) {
            normalize(src, src, 0, 255, NORM_MINMAX);
            src.convertTo(src, CV_8U);
        }
        vector<ColormapTypes> colormapTypes = {
            COLORMAP_AUTUMN, COLORMAP_BONE, COLORMAP_CIVIDIS, COLORMAP_COOL,
            COLORMAP_HOT, COLORMAP_HSV, COLORMAP_INFERNO, COLORMAP_JET,
            COLORMAP_MAGMA, COLORMAP_OCEAN, COLORMAP_PARULA, COLORMAP_PINK,
            COLORMAP_PLASMA, COLORMAP_RAINBOW, COLORMAP_SPRING, COLORMAP_SUMMER,
            COLORMAP_TWILIGHT, COLORMAP_TWILIGHT_SHIFTED, COLORMAP_VIRIDIS, COLORMAP_WINTER
        };
        ColormapTypes mapIndex = colormapTypes[task->paletteIndex];
        applyColorMap(src, dst2, mapIndex);
    }
};


vector<Point> ContourBuild(const Mat& mask, int approxMode) {
    std::vector<std::vector<Point>> allContours;
    findContours(mask, allContours, RETR_EXTERNAL, approxMode);  // Adjusted retrieval mode

    int maxCount = 0, maxIndex = -1;
    for (int i = 0; i < allContours.size(); i++) {
        int len = int(allContours[i].size());
        if (len > maxCount) {
            maxCount = len;
            maxIndex = i;
        }
    }

    return (maxIndex >= 0) ? allContours[maxIndex] : std::vector<Point>();
}

Mat vbPalette(Mat input)
{
    static Palette_Basics_CC* palette = new Palette_Basics_CC();
    if (input.type() == CV_8U) palette->Run(input);
    return palette->dst2;
}




class RedColor_FeatureLessCore_CC : public CPP_Parent {
public:
    map<int, rcData, compareAllowIdenticalIntegerInverted> sortedCells;
    Mat inputMask;
    FeatureLess_Basics_CC* fLess;
    RedCloud* cPtr;
    RedColor_FeatureLessCore_CC() : CPP_Parent() {
        vbPalette(dst2);
        fLess = new FeatureLess_Basics_CC();
        cPtr = new RedCloud();
        advice = "In redOptions the 'Desired RedCloud Cells' slider has a big impact.";
        inputMask = Mat(dst2.size(), CV_8U);
        inputMask.setTo(0);
        desc = "Another minimalist approach to building RedCloud color-based cells.";
    }
    ~RedColor_FeatureLessCore_CC() {

        if (cPtr) { 
            RedCloud_Close(cPtr);
            delete cPtr;
        }
    }
    void Run(Mat src) {
        if (src.channels() != 1) {
            fLess->Run(src);
            src = fLess->dst2;
        }
        void* imagePtr;
        Mat* srcPtr = &src;
        if (inputMask.empty()) {
            imagePtr = RedCloud_Run(cPtr, (int *)srcPtr->data, 0, src.rows, src.cols);
        }
        else {
            Mat* maskPtr = &inputMask;
            imagePtr = RedCloud_Run(cPtr, (int *)srcPtr->data, (uchar *)maskPtr->data, src.rows, src.cols);
        }
        int classCount = RedCloud_Count(cPtr);

        dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr);
        dst3 = vbPalette(dst2 * 255 / classCount);

        if (task->heartBeat) {
            labels[3] = to_string(classCount) + " cells found";
        }

        if (classCount <= 1) {
            return;
        }
        Mat rectData(classCount, 1, CV_32SC4, RedCloud_Rects(cPtr));
        Mat floodPointData(classCount, 1, CV_32SC2, RedCloud_FloodPoints(cPtr));
        sortedCells.clear();

        for (int i = 0; i < classCount; i++) {
            rcData cell;
            cell.index = int(sortedCells.size()) + 1;
            cell.rect = task->validateRect(rectData.at<cv::Rect>(i, 0), dst2.cols, dst2.rows);
            inRange(dst2(cell.rect), cell.index, cell.index, cell.mask);
            //vector<Point> contour = ContourBuild(cell.mask, cv::CHAIN_APPROX_NONE); 
            //DrawContours(cell.mask, vector<vector<Point>> {contour}, 255, -1);

            cell.floodPoint = floodPointData.at<cv::Point>(i, 0);
            rectangle(cell.mask, cv::Rect(0, 0, cell.mask.cols, cell.mask.rows), 0, 1);
            Point pt = task->vbGetMaxDist(cell.mask); 
            cell.maxDist = cv::Point(pt.x + cell.rect.x, pt.y + cell.rect.y);
            sortedCells.insert({ cell.pixels, cell }); 
        }

        if (task->heartBeat) {
            labels[2] = "CV_8U format - " + to_string(classCount) + " cells were identified.";
        }
    }
};









class RedColor_FeatureLess_CC : public CPP_Parent {
public:
    RedColor_FeatureLessCore_CC* minCore;
    vector<rcData> redCells;
    Mat lastColors;
    Mat lastMap = dst2.clone();
    RedColor_FeatureLess_CC() : CPP_Parent() {
        minCore = new RedColor_FeatureLessCore_CC();
        advice = minCore->advice;
        dst2 = Mat::zeros(dst2.size(), CV_8U);  
        labels = { "", "Mask of active RedCloud cells", "CV_8U representation of redCells", "" };
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells.";
    }
    void Run(Mat src) {
        minCore->Run(src);
        vector<rcData> lastCells = redCells;
        if (task->FirstPass) lastColors = dst3.clone();
        redCells.clear();
        dst2.setTo(0);
        dst3.setTo(0);
        vector<Vec3b> usedColors = { Vec3b(0, 0, 0) };
        for (const auto ele : minCore->sortedCells) {
            rcData cell = ele.second;
            uchar index = lastMap.at<uchar>(cell.maxDist.y, cell.maxDist.x);
            if (index > 0 && index < lastCells.size()) {
                cell.color = lastColors.at<Vec3b>(cell.maxDist.y, cell.maxDist.x);
            }
            if (find(usedColors.begin(), usedColors.end(), cell.color) != usedColors.end()) {
                cell.color = task->randomCellColor();
            }
            usedColors.push_back(cell.color);
            if (dst2.at<uchar>(cell.maxDist.y, cell.maxDist.x) == 0) {
                cell.index = int(redCells.size()) + 1;
                redCells.push_back(cell);
                dst2(cell.rect).setTo(cell.index, cell.mask);
                dst3(cell.rect).setTo(cell.color, cell.mask);
                //task->SetTrueText(to_string(cell.index), cell.maxDist, 2);
                //task->SetTrueText(to_string(cell.index), cell.maxDist, 3);
            }
        }
        labels[3] = to_string(redCells.size()) + " cells were identified.";
        task->rcSelect = rcData();
        if (task->ClickPoint == Point(0, 0)) {
            if (redCells.size() > 2) {
                task->ClickPoint = redCells[0].maxDist;
                task->rcSelect = redCells[0];
            }
        }
        else {
            uchar index = dst2.at<uchar>(task->ClickPoint.y, task->ClickPoint.x);
            if (index != 0) task->rcSelect = redCells[index - 1];
        }
        lastColors = dst3.clone();
        lastMap = dst2.clone();
        if (redCells.size() > 0) dst1 = vbPalette(lastMap * 255 / redCells.size());
    }
};





class Mesh_Basics_CC : public CPP_Parent {
public:
    Random_Basics_CC* random;
    KNN_Core_CC* knn;
    Mesh_Basics_CC() : CPP_Parent() {
        random = new Random_Basics_CC();
        knn = new KNN_Core_CC();
        labels[2] = "Triangles built with each random point and its 2 nearest neighbors.";
        advice = "Adjust the number of points with the options_random";
        desc = "Build triangles from random points";
    }
    Mat showMesh(const vector<Point2f>& pointList) {
        if (pointList.size() <= 3) return dst2; // not enough points to draw...
        knn->queries = pointList;
        knn->trainInput = knn->queries;
        knn->Run(empty);
        dst2.setTo(0);
        for (int i = 0; i < knn->queries.size(); i++) {
            Point2f p0 = knn->queries[i];
            Point2f p1 = knn->queries[knn->result.at<int>(i, 1)];
            Point2f p2 = knn->queries[knn->result.at<int>(i, 2)];
            line(dst2, p0, p1, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            line(dst2, p0, p2, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            line(dst2, p1, p2, Scalar(255, 255, 255), task->lineWidth, task->lineType);
        }
        for (int i = 0; i < knn->queries.size(); i++) {
            circle(dst2, knn->queries[i], task->DotSize + 2, Scalar(0, 0, 255), -1, task->lineType);
        }
        return dst2;
    }
    void Run(Mat src) {
        if (task->heartBeat) {
            random->Run(empty);
            showMesh(random->pointList);
        }
    }
};






class Mesh_Features_CC : public CPP_Parent {
public:
    Feature_Stable_CC* feat;
    Mesh_Basics_CC* mesh;
    Mesh_Features_CC() : CPP_Parent() {
        feat = new Feature_Stable_CC();
        mesh = new Mesh_Basics_CC();
        labels[2] = "Triangles built with each feature point and its 2 nearest neighbors.";
        advice = "Use options_Features to update results.";
        desc = "Build triangles from feature points";
    }
    void Run(Mat src) {
        feat->Run(src);
        if (feat->featurePoints.size() < 3) {
            return;
        }
        mesh->dst2 = src;
        dst2 = mesh->showMesh(feat->featurePoints);
    }
};






class Area_MinRect_CC : public CPP_Parent {
public:
    RotatedRect minRect;
    vector<Point2f> inputPoints;
    Area_MinRect_CC() : CPP_Parent() {
        desc = "Find minimum containing rectangle for a set of points.";
    }
    void Run(Mat src) {
        if (standalone) {
            if (!task->heartBeat) return;
            inputPoints = task->quickRandomPoints(10);
        }
        minRect = minAreaRect(inputPoints);
        if (standalone) {
            dst2.setTo(0);
            for (const Point2f& pt : inputPoints) {
                circle(dst2, pt, task->DotSize + 2, Scalar(0, 0, 255), -1, task->lineType);
            }
            task->DrawRotatedOutline(minRect, dst2, Scalar(0, 255, 255));
        }
    }
};







class Mat_4to1_CC : public CPP_Parent {
public:
    Mat mat[4];
    bool lineSeparators = true;

    Mat_4to1_CC() : CPP_Parent() {
        for (int i = 0; i < 4; i++) {
            mat[i] = dst2.clone();
        }

        desc = "Use one Mat for up to 4 images";
    }

    void defaultMats(Mat src)
    {
        Mat tmpLeft = task->leftView;
        if (task->leftView.channels() == 1) cvtColor(task->leftView, task->leftView, COLOR_GRAY2BGR);
        Mat tmpRight = task->rightView;
        if (task->rightView.channels() == 1) cvtColor(task->rightView, task->rightView, COLOR_GRAY2BGR);
        mat[0] = src.clone();
        mat[1] = task->depthRGB.clone();
        mat[2] = tmpLeft;
        mat[3] = tmpRight;
    }
    void Run(Mat src) {
        Size nSize(dst2.cols / 2, dst2.rows / 2);
        Rect roiTopLeft(0, 0, nSize.width, nSize.height);
        Rect roiTopRight(nSize.width, 0, nSize.width, nSize.height);
        Rect roiBotLeft(0, nSize.height, nSize.width, nSize.height);
        Rect roiBotRight(nSize.width, nSize.height, nSize.width, nSize.height);

        if (standalone) defaultMats(src);

        dst2 = Mat(dst2.size(), CV_8UC3);
        Rect rois[] = { roiTopLeft, roiTopRight, roiBotLeft, roiBotRight };
        for (int i = 0; i < 4; i++) {
            if (mat[i].channels() == 1) {
                cvtColor(mat[i], mat[i], COLOR_GRAY2BGR);
            }
            resize(mat[i], dst2(rois[i]), nSize);
        }

        if (lineSeparators) {
            line(dst2, Point(0, dst2.rows / 2), Point(dst2.cols, dst2.rows / 2), Scalar(255, 255, 255), task->lineWidth + 1);
            line(dst2, Point(dst2.cols / 2, 0), Point(dst2.cols / 2, dst2.rows), Scalar(255, 255, 255), task->lineWidth + 1);
        }
    }
};


class Mat_4Click_CC : public CPP_Parent {
public:
    Mat_4to1_CC* mats;
    int quadrant = 0;
    Mat_4Click_CC() : CPP_Parent() {
        mats = new Mat_4to1_CC();
        labels[3] = "Click a quadrant in dst2 to view it in dst3";
        desc = "Split an image into 4 segments and allow clicking on a quadrant to open it in dst3";
    }
    void Run(Mat src) {
        if (standalone) mats->defaultMats(src);
        if (task->FirstPass) {
            task->mouseClickFlag = true;
            task->ClickPoint = Point(0, 0);
            task->mousePicTag = 2;
        }
        if (task->mouseClickFlag && task->mousePicTag == 2) {
            if (task->ClickPoint.y < task->WorkingRes.height / 2) {
                quadrant = (task->ClickPoint.x < task->WorkingRes.width / 2) ? 0 : 1;
            }
            else {
                quadrant = (task->ClickPoint.x < task->WorkingRes.width / 2) ? 2 : 3;
            }
        }
        mats->Run(empty);
        dst2 = mats->dst2;
        dst3 = mats->mat[quadrant];
    }
};






class Bin4Way_Regions_CC : public CPP_Parent {
public:
    Binarize_Simple_CC* binarize;
    Mat_4Click_CC* mats;
    Bin4Way_Regions_CC() : CPP_Parent() {
        binarize = new Binarize_Simple_CC();
        mats = new Mat_4Click_CC();
        labels[2] = "A 4-way split - lightest (upper left) to darkest (lower right)";
        desc = "Binarize an image twice using masks";
    }
    void Run(Mat src) {
        Mat gray;
        if (src.channels() == 1) {
            gray = src;
        }
        else {
            cvtColor(src, gray, COLOR_BGR2GRAY);
        }
        binarize->Run(gray);
        Mat mask = binarize->dst2.clone();
        double midColor = binarize->meanScalar(0);
        double topColor = mean(gray, mask)(0);
        double botColor = mean(gray, ~mask)(0);
        inRange(gray, topColor, 255, mats->mats->mat[0]);
        inRange(gray, midColor, topColor, mats->mats->mat[1]);
        inRange(gray, botColor, midColor, mats->mats->mat[2]);
        inRange(gray, 0, botColor, mats->mats->mat[3]);
        if (standalone) {
            mats->Run(empty);
            dst2 = mats->dst2;
            dst3 = mats->dst3;
            labels[3] = mats->labels[3];
        }
    }
};





class Bin4Way_RegionsCombine_CC : public CPP_Parent {
public:
    Bin4Way_Regions_CC* binarize;
    int classCount = 4;
    Bin4Way_RegionsCombine_CC() : CPP_Parent() {
        binarize = new Bin4Way_Regions_CC();
        dst1 = Mat::zeros(dst3.size(), CV_8U);
        desc = "Add the 4-way split of images to define the different regions.";
    }
    void Run(Mat src) {
        binarize->Run(src);
        dst1.setTo(1, binarize->mats->mats->mat[0]);
        dst1.setTo(2, binarize->mats->mats->mat[1]);
        dst1.setTo(3, binarize->mats->mats->mat[2]);
        dst1.setTo(4, binarize->mats->mats->mat[3]);
        if (standalone) {
            dst3 = dst1 * 255 / 5;
            dst2 = vbPalette(dst3);
        }
    }
};






class Color_Basics_CC : public CPP_Parent {
public:
    int classCount;
    string CurrentColorClassifier;
    //BackProject_Full* backP;
    //KMeans_Basics* km;
    //LUT_Basics* lut;
    //Reduction_Basics* reduction;
    //Hist3Dcolor_Basics* hColor;
    String colorInput; 
    Bin4Way_RegionsCombine_CC* binarize;
    Color_Basics_CC() : CPP_Parent() {
        binarize = new Bin4Way_RegionsCombine_CC();
        desc = "Classify pixels by color using a variety of techniques";
    }
    void Run(Mat src) {

        if (src.channels() == 3)
        {
            cvtColor(src, dst1, COLOR_BGR2GRAY);
            switch (task->colorInputIndex) {
            case 0: // "BackProject_Full":
                //backP->Run(dst1);
                //classCount = backP->classCount;
                //dst2 = backP->dst2;
                //colorInput = backP->traceName;
                break;
            case 1: // "KMeans_Basics":
                //km->Run(dst1);
                //classCount = km->classCount;
                //dst2 = km->dst2;
                //colorInput = km->traceName;
                break;
            case 2: //"LUT_Basics":
                //lut->Run(dst1);
                //classCount = lut->classCount;
                //dst2 = lut->dst2;
                //colorInput = lut->traceName;
                break;
            case 3: //"Reduction_Basics":
                // reduction->Run(dst1);
                //classCount = reduction->classCount;
                //dst2 = reduction->dst2;
                //colorInput = reduction->traceName;
                break;
            case 4: //"3D BackProjection":
                //hColor->Run(dst1);
                //classCount = hColor->classCount;
                //dst2 = hColor->dst2;
                //colorInput = hColor->traceName;
                break;
            case 5:
                binarize->Run(dst1);
                classCount = binarize->classCount;
                dst2 = binarize->dst1;
                colorInput = binarize->traceName;
                break;
            }
        }
        dst3 = vbPalette(dst2 * 255 / classCount);
        labels[2] = "Color_Basics: method = " + colorInput + " produced " + to_string(classCount) + 
                    " pixel classifications";
    }
};




class Blur_Basics_CC : public CPP_Parent {
public:
    Options_Blur* options = new Options_Blur;
    Blur_Basics_CC() : CPP_Parent() {
        desc = "Smooth each pixel with a Gaussian kernel of different sizes.";
    }
    void Run(cv::Mat src) {
        options->RunOpt();
        cv::GaussianBlur(src, dst2, cv::Size(options->kernelSize, options->kernelSize), 
                            options->sigma, options->sigma);
    }
};






class Palette_Random_CC : public CPP_Parent {
public:
    Mat colorMap;
    Palette_Random_CC() : CPP_Parent() {
        colorMap = Mat(256, 1, CV_8UC3, Scalar(0, 0, 0));
        for (int i = 1; i < 256; i++) {
            colorMap.at<Vec3b>(i, 0) = task->randomCellColor();
        }
        desc = "Build a random colorGrad - no smooth transitions.";
    }
    void Run(Mat src) {
        applyColorMap(src, dst2, colorMap);
    }
};






class Hist_RedOptions_CC : public CPP_Parent {
public:
    std::vector<cv::Range> ranges;
    std::vector<cv::Range> rangesCloud;
    Hist_RedOptions_CC() : CPP_Parent() {
        advice = "See redOption 'Histogram Channels' to control the settings here.";
        desc = "Build the channels, channel count, and ranges based on the PointCloud Reduction setting.";
    }
    void Run(Mat src) {
        cv::Vec2f rx(-task->xRangeDefault, task->xRangeDefault);
        cv::Vec2f ry(-task->yRangeDefault, task->yRangeDefault);
        cv::Vec2f rz(0, task->MaxZmeters);
        rangesCloud = { cv::Range(rx[0], rx[1]), cv::Range(ry[0], ry[1]), cv::Range(rz[0], rz[1]) };

        task->channelCount = 1;
        task->histBins[0] = task->histogramBins;
        task->histBins[1] = task->histogramBins;
        task->histBins[2] = task->histogramBins;
        switch (task->PCReduction) {
        case 0: // "X Reduction"
            ranges = { cv::Range(rx[0], rx[1]) };
            task->channels[0] = 0;
            break;
        case 1: // "Y Reduction"
            ranges = { cv::Range(ry[0], ry[1]) };
            task->channels[0] = 1;
            break;
        case 2: // "Z Reduction"
            ranges = { cv::Range(rz[0], rz[1]) };
            task->channels[0] = 2;
            break;
        case 3: // "XY Reduction"
            ranges = { cv::Range(rx[0], rx[1]), cv::Range(ry[0], ry[1]) };
            task->channelCount = 2;
            task->channels[0] = 0;
            task->channels[1] = 1;
            break;
        case 4: // "XZ Reduction"
            ranges = { cv::Range(rx[0], rx[1]), cv::Range(rz[0], rz[1]) };
            task->channelCount = 2;
            task->channels[0] = 0;
            task->channels[1] = 2;
            task->channelIndex = 1;
            break;
        case 5: // "YZ Reduction"
            ranges = { cv::Range(ry[0], ry[1]), cv::Range(rz[0], rz[1]) };
            task->channelCount = 2;
            task->channels[0] = 1;
            task->channels[1] = 2;
            task->channelIndex = 1;
            break;
        case 6: // "XYZ Reduction"
            ranges = { cv::Range(rx[0], rx[1]), cv::Range(ry[0], ry[1]), cv::Range(rz[0], rz[1]) };
            task->channelCount = 3;
            task->channels[0] = 0;
            task->channels[1] = 1;
            task->channels[2] = 2;
            task->channelIndex = 2;
            break;
        }

        labels[2] = "RedOptions are now set...";
    }
};






class Plot_Histogram2D_CC : public CPP_Parent {
public:
    Plot_Histogram2D_CC() : CPP_Parent() {
        labels = { "", "", "2D Histogram", "Threshold of all non-zero values in the plot at left." };
        desc = "Plot a 2D histogram from the input Mat";
    }
    void Run(Mat src) {
        Mat histogram = src.clone();
        if (standalone) {
            float hRange[] = { 0, 255 };
            const float* range[] = { hRange,hRange,hRange };
            calcHist(&src, 1, task->channels, Mat(), histogram, task->channelCount, task->histBins, range, true, false);
        }
        if (histogram.rows > 0 && histogram.cols > 0)
            resize(histogram, dst2, dst2.size(), 0, 0, INTER_NEAREST);
        else
            labels[2] = "Unable to portray a 3D histogram here.";
        if (standalone) threshold(dst2, dst3, 0, 255, THRESH_BINARY);
    }
};





class Feature_StableSorted_CC : public CPP_Parent {
public:
    Feature_Stable_CC* feat;
    int desiredCount = 200;
    vector<Point2f> stablePoints;
    vector<int> generations;
    Feature_StableSorted_CC() : CPP_Parent() {
        feat = new Feature_Stable_CC();
        desc = "Display the top X feature points ordered by generations they were present.";
    }
    void Run(Mat src) {
        feat->Run(src.clone());
        dst3 = feat->dst2;
        dst2 = src;
        if (task->optionsChanged) {
            stablePoints.clear();
            generations.clear();
        }
        for (int i = 0; i < feat->featurePoints.size(); i++) {
            Point2f pt = feat->featurePoints[i];
            auto it = find(stablePoints.begin(), stablePoints.end(), pt);
            if (it != stablePoints.end()) {
                int index = distance(stablePoints.begin(), it);
                generations[index]++;
            }
            else {
                stablePoints.push_back(pt);
                generations.push_back(1);
            }
        }
        map<int, Point2f, greater<int>> sortByGen;
        for (int i = 0; i < stablePoints.size(); i++) {
            sortByGen[generations[i]] = stablePoints[i];
        }
        int displayCount = 0;
        stablePoints.clear();
        generations.clear();
        for (auto it = sortByGen.begin(); it != sortByGen.end(); it++) {
            if (displayCount >= desiredCount * 2) break;
            Point2f pt = it->second;
            if (displayCount < desiredCount) {
                circle(dst2, pt, task->DotSize, Scalar(255, 255, 255), -1, task->lineType);
                displayCount++;
            }
            stablePoints.push_back(pt);
            generations.push_back(it->first);
        }
        labels[2] = "The most stable " + to_string(displayCount) + " are highlighted below";
        labels[3] = "Output of Feature_Stable" + to_string(feat->featurePoints.size()) + " points found";
    }
};






class BGSubtract_Basics_CC : public CPP_Parent {
public:
    Options_BGSubtract* options = new Options_BGSubtract;
    BGSubtract_BGFG* cPtr = nullptr;
    vector<string> labels = { "", "", "BGSubtract output - aging differences", "Mask for any changes" };
    BGSubtract_Basics_CC() : CPP_Parent() {
        desc = "Different background subtraction algorithms in OpenCV - some only available in C++";
    }
    void Run(Mat src) {
        if (task->optionsChanged) {
            Close();
            cPtr = BGSubtract_BGFG_Open(options->currMethod);
        }
        void* imagePtr = BGSubtract_BGFG_Run(cPtr, (int*)src.data, src.rows, src.cols, src.channels(), options->learnRate);
        if (imagePtr) {
            dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr).clone();
            threshold(dst2, dst3, 0, 255, THRESH_BINARY);
        }
        labels[2] = options->methodDesc;
    }
    void Close() {
        if (cPtr) {
            BGSubtract_BGFG_Close(cPtr);
            cPtr = nullptr;
        }
    }
};





class Motion_Basics_CC : public CPP_Parent {
public:
    Options_BGSubtract* options = new Options_BGSubtract;
    BGSubtract_BGFG* cPtr = nullptr;
    string desc = "Detect motion using background subtraction algorithms in OpenCV - some only available in C++";
    Motion_Basics_CC() : CPP_Parent() {
        cPtr = BGSubtract_BGFG_Open(options->currMethod);
        labels[2] = "BGSubtract output";
    }
    void Run(Mat src) {
        if (task->optionsChanged) {
            Close();
            cPtr = BGSubtract_BGFG_Open(options->currMethod);
        }
        void* imagePtr = BGSubtract_BGFG_Run(cPtr, (int *)src.data, src.rows, src.cols, src.channels(), options->learnRate);
        dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr);
        labels[2] = options->methodDesc;
    }
    void Close() {
        if (cPtr) {
            BGSubtract_BGFG_Close(cPtr);
            cPtr = nullptr;
        }
    }
};




     

class Feature_Agast_CC : public CPP_Parent {
private:
public:
    vector<KeyPoint> featurePoints;
    vector<Point2f> stablePoints;
    Ptr<AgastFeatureDetector> agastFD;
    std::vector<Point2f> lastPoints;
    Feature_Agast_CC() : CPP_Parent()
    {
        agastFD = AgastFeatureDetector::create(10, true, AgastFeatureDetector::OAST_9_16);
        desc = "Use the Agast Feature Detector in the OpenCV Contrib.";
    }
    void Run(Mat src) {
        int resizeFactor = 1;
        Mat input;
        if (src.cols >= 1280)
        {
            resize(src, input, cv::Size(src.cols / 4, src.rows / 4));
            resizeFactor = 4;
        }
        else {
            input = src;
        }

        featurePoints.clear();

        agastFD->detect(input, featurePoints);
        if (task->heartBeat || lastPoints.size() < 10) {
            lastPoints.clear();
            for (const KeyPoint& kpt : featurePoints) {
                lastPoints.push_back(Point2f(round(kpt.pt.x) * resizeFactor, round(kpt.pt.y) * resizeFactor));
            }
        }

        stablePoints.clear();
        dst2 = src;
        for (const KeyPoint& pt : featurePoints) {
            auto p1 = Point2f(round(pt.pt.x * resizeFactor), round(pt.pt.y * resizeFactor));
            if (find(lastPoints.begin(), lastPoints.end(), p1) != lastPoints.end()) {
                stablePoints.push_back(p1);
                circle(dst2, p1, task->DotSize, Scalar(0, 0, 255), -1, task->lineType);
            }
        }

        lastPoints = stablePoints;
        if (task->midHeartBeat) {
            labels[2] = std::to_string(featurePoints.size()) + " features found and " + std::to_string(stablePoints.size()) + " of them were stable";
        }

        labels[2] = "Found " + to_string(featurePoints.size()) + " features";
    }
};




class Feature_AKaze_CC : public CPP_Parent {
public:
    std::vector<cv::KeyPoint> kazeKeyPoints;
    Feature_AKaze_CC() : CPP_Parent()
    {
        labels[2] = "AKAZE key points";
        desc = "Find keypoints using AKAZE algorithm.";
    }
    void Run(cv::Mat src)
    {
        dst2 = src.clone();
        if (src.channels() != 1)
            cv::cvtColor(src, src, cv::COLOR_BGR2GRAY);
        cv::Ptr<cv::AKAZE> kaze = cv::AKAZE::create();
        cv::Mat kazeDescriptors;
        kaze->detectAndCompute(src, cv::noArray(), kazeKeyPoints, kazeDescriptors);
        for (const auto& keyPoint : kazeKeyPoints)
        {
            cv::circle(dst2, keyPoint.pt, task->DotSize, task->HighlightColor);
        }
    }
};




class AddWeighted_DepthAccumulate_CC : public CPP_Parent {
private:
    Options_AddWeighted* options = new Options_AddWeighted();
public:
    AddWeighted_DepthAccumulate_CC() : CPP_Parent()
    {
        desc = "Update a running average of the image";
    }
    void Run(Mat src)
    {
        options->RunOpt();
        if (task->optionsChanged)
        {
            dst2 = task->pcSplit[2] * 1000;
        }
        cv::accumulateWeighted(task->pcSplit[2] * 1000, dst2, (float)options->accumWeighted, cv::Mat());
    }
};






//class Edge_Basics_CC : public CPP_Parent {
//private:
//    Edge_Canny canny;
//    Edge_Scharr scharr;
//    Edge_BinarizedReduction binRed;
//    Bin4Way_Sobel binSobel;
//    Edge_ColorGap_CC_VB colorGap;
//    Edge_Deriche_CC_VB deriche;
//    Edge_Laplacian Laplacian;
//    Edge_ResizeAdd resizeAdd;
//    Edge_Regions regions;
//public:
//    Options_Edge_Basics* options;
//    Edge_Basics_CC() : CPP_Parent()
//    {
//        desc = "Use Radio Buttons to select the different edge algorithms.";
//    }
//    void Run(cv::Mat src)
//    {
//        options->RunOpt();
//        std::string edgeSelection = options->edgeSelection;
//        if (edgeSelection == "Canny")
//        {
//            canny.Run(src);
//            dst2 = canny.dst2;
//        }
//        else if (edgeSelection == "Scharr")
//        {
//            scharr.Run(src);
//            dst2 = scharr.dst3;
//        }
//        else if (edgeSelection == "Binarized Reduction")
//        {
//            binRed.Run(src);
//            dst2 = binRed.dst2;
//        }
//        else if (edgeSelection == "Binarized Sobel")
//        {
//            binSobel.Run(src);
//            dst2 = binSobel.dst2;
//        }
//        else if (edgeSelection == "Color Gap")
//        {
//            colorGap.Run(src);
//            dst2 = colorGap.dst2;
//        }
//        else if (edgeSelection == "Deriche")
//        {
//            deriche.Run(src);
//            dst2 = deriche.dst2;
//        }
//        else if (edgeSelection == "Laplacian")
//        {
//            Laplacian.Run(src);
//            dst2 = Laplacian.dst2;
//        }
//        else if (edgeSelection == "Resize And Add")
//        {
//            resizeAdd.Run(src);
//            dst2 = resizeAdd.dst2;
//        }
//        else if (edgeSelection == "Depth Region Boundaries")
//        {
//            regions.Run(src);
//            dst2 = regions.dst2;
//        }
//        if (dst2.channels() != 1)
//            cv::cvtColor(dst2, dst2, cv::COLOR_BGR2GRAY);
//        labels[2] = traceName + " - selection = " + edgeSelection;
//    }
//};





class Edge_Basics_CC : public CPP_Parent {
private:
    Edge_Canny_CC* canny = new Edge_Canny_CC();
    Options_Edge_Basics* options = new Options_Edge_Basics();
public:
    Edge_Basics_CC() : CPP_Parent()
    {
        labels[2] = "Edge_Basics_CC";
        desc = "Use Radio Buttons to select the different edge algorithms.";
    }
    void Run(cv::Mat src)
    {
        options->RunOpt();
        canny->Run(src);
        dst2 = canny->dst2;
        if (dst2.channels() != 1)
            cv::cvtColor(dst2, dst2, cv::COLOR_BGR2GRAY);
        labels[2] = traceName + " - selection = " + options->edgeSelection;
    }
};






class Hist_DepthSimple_CC : public CPP_Parent {
public:
    std::vector<float> histList;
    std::vector<float> histArray;
    cv::Mat histogram;
    Plot_Histogram_CC plotHist;
    mmData mm;
    cv::Mat inputMask;
    float ranges[2];
    Hist_DepthSimple_CC() : CPP_Parent()
    {
        plotHist.addLabels = false;
        desc = "Use Kalman to smooth the histogram results.";
    }
    void Run(cv::Mat src)
    {
        if (standalone)
        {
            mm = task->vbMinMax(task->pcSplit[2]);
            ranges[0] = (float)mm.minVal;
            ranges[1] = (float)mm.maxVal;
        }

        int bins[] = { task->histogramBins };
        float hRange[] = { static_cast<float>(ranges[0]), static_cast<float>(ranges[1])};
        const float* range[] = { hRange };
        calcHist(&task->pcSplit[2], 1, { 0 }, Mat(), histogram, 1, bins, range, true, false);

        histArray.resize(histogram.total());
        std::memcpy(histArray.data(), histogram.ptr<float>(), histogram.total() * sizeof(float));
        if (standalone)
        {
            plotHist.Run(histogram);
            dst2 = plotHist.dst2;
        }
        histList = histArray;
    }
};






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
