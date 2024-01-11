#pragma once
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

extern "C" __declspec(dllexport) PlotOpenCV * PlotOpenCV_Open() { PlotOpenCV* cPtr = new PlotOpenCV(); return cPtr; }
extern "C" __declspec(dllexport) void PlotOpenCV_Close(PlotOpenCV * cPtr) { delete cPtr; }

// https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
extern "C" __declspec(dllexport)
int *PlotOpenCV_Run(PlotOpenCV * cPtr, double *inX, double *inY, int len, int rows, int cols)
{
	cPtr->dst.setTo(0);
	cPtr->srcX = Mat(1, len, CV_64F, inX);
	cPtr->srcY = Mat(1, len, CV_64F, inY);
	cPtr->dst = Mat(rows, cols, CV_8UC3);
	cPtr->RunCPP();
	return (int *)cPtr->dst.data;
}