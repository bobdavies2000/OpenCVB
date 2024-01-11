#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>


using namespace std;
using namespace  cv;


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
uchar * BackProjectBGR_Run(int* bgrPtr, int rows, int cols, int bins, float threshold)
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
uchar * Hist3Dcloud_Run(int* inputPtr, int rows, int cols, int bins,
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

	float* hist = (float *) histogram.data;
	for (int i = 0; i < bins * bins * bins; i++)
	{
		if (hist[i] < threshold) hist[i] = 0; else hist[i] = 255; // building a mask
	}

	Mat mask32f;
	calcBackProject(&input, 1, channels, histogram, mask32f, range);
	mask32f.convertTo(mask, CV_8U);

	return (uchar*)mask.data;
}