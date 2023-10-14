#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/core/utils/trace.hpp>

using namespace std;
using namespace  cv;
// https://github.com/opencv/opencv/wiki/Profiling-OpenCV-Applications
class Trace_OpenCV
{
private:
public:
	Mat gray, processed;
	Mat src, dst;
    Trace_OpenCV(){}
    void Run() {
		cv::cvtColor(src, gray, COLOR_BGR2GRAY);
		Canny(gray, processed, 32, 64, 3);
	}
};

extern "C" __declspec(dllexport)
Trace_OpenCV *Trace_OpenCV_Open() {
	CV_TRACE_FUNCTION();
	Trace_OpenCV * cPtr = new Trace_OpenCV();
	CV_TRACE_REGION("Start");
	return cPtr;
}

extern "C" __declspec(dllexport)
int * Trace_OpenCV_Close(Trace_OpenCV * cPtr)
{
    delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Trace_OpenCV_Run(Trace_OpenCV * cPtr, int* rgbPtr, int rows, int cols, int channels)
{
	CV_TRACE_REGION_NEXT("process");
	cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, rgbPtr);
	cPtr->Run();

	CV_TRACE_REGION("read"); // we are off to read the next frame...
	return (int*)cPtr->processed.data; // return this C++ allocated data to managed code
}