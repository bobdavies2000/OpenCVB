#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/core/utils/trace.hpp>

using namespace std;
using namespace cv;
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
	Trace_OpenCV *Trace_OpenCVPtr = new Trace_OpenCV();
	CV_TRACE_REGION("Start");
	return Trace_OpenCVPtr;
}

extern "C" __declspec(dllexport)
void Trace_OpenCV_Close(Trace_OpenCV *Trace_OpenCVPtr)
{
    delete Trace_OpenCVPtr;
}

extern "C" __declspec(dllexport)
int* Trace_OpenCV_Run(Trace_OpenCV * Trace_OpenCVPtr, int* rgbPtr, int rows, int cols, int channels)
{
	CV_TRACE_REGION_NEXT("process");
	Trace_OpenCVPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, rgbPtr);
	Trace_OpenCVPtr->Run();

	CV_TRACE_REGION("read"); // we are off to read the next frame...
	return (int*)Trace_OpenCVPtr->processed.data; // return this C++ allocated data to managed code
}