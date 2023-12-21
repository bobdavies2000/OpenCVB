#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace  cv;

//extern "C" __declspec(dllexport)
//float* Histogram_1DBug(int* bgrPtr, int rows, int cols, int bins)
//{
//	float hRange[] = { 0, 256 }; // ranges are exclusive in the top of the range, hence 256
//	const float* range[] = { hRange, hRange, hRange };
//	int hbins[] = { bins, bins, bins };
//	int channel[] = { 0, 1, 2 };
//	Mat src = Mat(rows, cols, CV_8UC3, bgrPtr);
//
//	static Mat histogram;
//	calcHist(&src, 1, channel, Mat(), histogram, 3, hbins, range, true, false); // for 3D histograms, all 3 bins must be equal.
//
//	return (float*)histogram.data;
//}

class Histogram_1D
{
private:
public:
    Mat src;
	Mat histogram;
	Histogram_1D(){}
    void RunCPP(int bins) {
		float hRange[] = { 0, 256 }; 
		int hbins[] = { bins };
		const float* range[] = { hRange };
		calcHist(&src, 1, { 0 }, Mat(), histogram, 1, hbins, range, true, false);
	}
};
extern "C" __declspec(dllexport)
Histogram_1D * Histogram_1D_Open() {
	Histogram_1D* cPtr = new Histogram_1D();
	return cPtr;
}
extern "C" __declspec(dllexport)
float Histogram_1D_Sum(Histogram_1D *cPtr) {
	Scalar count = cv::sum(cPtr->histogram);
	return count[0];
}
extern "C" __declspec(dllexport)
void Histogram_1D_Close(Histogram_1D *cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int *Histogram_1D_RunCPP(Histogram_1D *cPtr, int *dataPtr, int rows, int cols, int bins)
{
		cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
		cPtr->RunCPP(bins);
		return (int *) cPtr->histogram.data;
}