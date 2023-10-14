#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/plot.hpp>

using namespace std;
using namespace  cv;

// https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
extern "C" __declspec(dllexport)
void Plot_OpenCVBasics(double *inX, double *inY, int len, int* dstptr, int rows, int cols)
{
	Mat srcX = Mat(1, len, CV_64F, inX);
	Mat srcY = Mat(1, len, CV_64F, inY);
	Mat output = Mat(rows, cols, CV_8UC3, dstptr);
	Mat result;

	Ptr<plot::Plot2d> plot = plot::Plot2d::create(srcX, srcY);
	plot->setInvertOrientation(true);
	plot->setShowText(true);
	plot->setShowGrid(false);
	plot->setPlotBackgroundColor(Scalar(255, 200, 200));
	plot->setPlotLineColor(Scalar(255, 0, 0));
	plot->setPlotLineWidth(2);
	plot->render(result);
	resize(result, output, output.size());
}