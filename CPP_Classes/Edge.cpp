#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"

using namespace std;
using namespace  cv;
using namespace ximgproc;





// https://docs.opencv.org/3.1.0/d0/da5/tutorial_ximgproc_prediction.html
class Edge_RandomForest
{
private:
	Ptr<StructuredEdgeDetection> pDollar;
public:
	Mat dst32f, src32f, gray8u;
	Edge_RandomForest(char *modelFileName) { pDollar = createStructuredEdgeDetection(modelFileName); }

	void Run(Mat src)
	{
		src.convertTo(src32f, CV_32FC3, 1.0 / 255.0); 
		pDollar->detectEdges(src32f, dst32f);
		dst32f.convertTo(gray8u, CV_8U, 255);
	}
 };

extern "C" __declspec(dllexport)
Edge_RandomForest *Edge_RandomForest_Open(char *modelFileName)
{
  return new Edge_RandomForest(modelFileName);
}

extern "C" __declspec(dllexport)
int * Edge_RandomForest_Close(Edge_RandomForest *Edge_RandomForestPtr)
{
    delete Edge_RandomForestPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int *Edge_RandomForest_Run(Edge_RandomForest *Edge_RandomForestPtr, int *rgbPtr, int rows, int cols)
{
	Edge_RandomForestPtr->Run(Mat(rows, cols, CV_8UC3, rgbPtr));
	return (int *) Edge_RandomForestPtr->gray8u.data; 
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
Edge_Deriche * Edge_Deriche_Open()
{
	Edge_Deriche* dPtr = new Edge_Deriche();
	return dPtr;
}

extern "C" __declspec(dllexport)
int * Edge_Deriche_Close(Edge_Deriche * dPtr)
{
	delete dPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_Deriche_Run(Edge_Deriche * dPtr, int* rgbPtr, int rows, int cols, float alpha, float omega)
{
	dPtr->src = Mat(rows, cols, CV_8UC3, rgbPtr);
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
    Edge_ColorGap(){}
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
Edge_ColorGap *Edge_ColorGap_Open() {
    Edge_ColorGap *cPtr = new Edge_ColorGap();
    return cPtr;
}

extern "C" __declspec(dllexport)
int * Edge_ColorGap_Close(Edge_ColorGap *cPtr)
{
    delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int *Edge_ColorGap_Run(Edge_ColorGap *cPtr, int *grayPtr, int rows, int cols, int distance, int diff)
{
		cPtr->src = Mat(rows, cols, CV_8UC1, grayPtr);
		if (cPtr->dst.rows != rows) cPtr->dst = Mat(rows, cols, CV_8UC1);
		cPtr->Run(distance, diff);
		return (int *) cPtr->dst.data; 
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
Edge_DepthGap * Edge_DepthGap_Open() {
	Edge_DepthGap* cPtr = new Edge_DepthGap();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Edge_DepthGap_Close(Edge_DepthGap * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_DepthGap_RunCPP(Edge_DepthGap * cPtr, int* dataPtr, int rows, int cols, float minDiff)
{
	cPtr->src = Mat(rows, cols, CV_32FC1, dataPtr);
	cPtr->RunCPP(minDiff);
	return (int*)cPtr->dst.data; 
}
