#pragma once
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/core/utility.hpp>

using namespace std;
using namespace  cv;
class Guess_Depth
{
private:
public:
    Mat src;
    Guess_Depth(){}
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
Guess_Depth *Guess_Depth_Open() {
    Guess_Depth *cPtr = new Guess_Depth();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Guess_Depth_Close(Guess_Depth *cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int *Guess_Depth_RunCPP(Guess_Depth *cPtr, int *dataPtr, int rows, int cols)
{
		cPtr->src = Mat(rows, cols, CV_32FC3, dataPtr);
		cPtr->RunCPP();
		return (int *) cPtr->src.data; 
}






class Guess_ImageEdges
{
private:
public:
    Mat pc;
    Guess_ImageEdges(){}
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
Guess_ImageEdges *Guess_ImageEdges_Open() {
    Guess_ImageEdges *cPtr = new Guess_ImageEdges();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Guess_ImageEdges_Close(Guess_ImageEdges *cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int *Guess_ImageEdges_RunCPP(Guess_ImageEdges *cPtr, int *dataPtr, int rows, int cols, int maxDistanceToEdge)
{
	cPtr->pc = Mat(rows, cols, CV_32FC3, dataPtr);
	cPtr->RunCPP(maxDistanceToEdge);
	return (int *) cPtr->pc.data; 
}
