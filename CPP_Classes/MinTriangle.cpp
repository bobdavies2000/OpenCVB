#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>


using namespace std;
using namespace  cv;

extern "C" __declspec(dllexport)
void MinTriangle_Run(float *inputPoints, int count, float *outputTriangle)
{
	Mat input(count, 1, CV_32FC2, inputPoints);
	vector<Point2f> triangle;
	minEnclosingTriangle( input, triangle);
	for (int i = 0; i < 3; ++i)
	{
		outputTriangle[i * 2 + 0] = triangle.at(i).x;
		outputTriangle[i * 2 + 1] = triangle.at(i).y;
	}
}