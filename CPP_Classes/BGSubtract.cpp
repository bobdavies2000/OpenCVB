#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include "opencv2/bgsegm.hpp"
#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/ximgproc.hpp>
#include <atlstr.h>


using namespace std;
using namespace  cv;
using namespace bgsegm;
class BGSubtract_BGFG
{
private:
public:
	Ptr<BackgroundSubtractor> algo;
	Mat src, fgMask;
    BGSubtract_BGFG(){}
    void Run() {
		algo->apply(src, fgMask);
	}
};

extern "C" __declspec(dllexport)
BGSubtract_BGFG *BGSubtract_BGFG_Open(int currMethod) {
    BGSubtract_BGFG *bgfs = new BGSubtract_BGFG();
	if (currMethod == 0)      bgfs->algo = createBackgroundSubtractorGMG(20, 0.7);
	else if (currMethod == 1) bgfs->algo = createBackgroundSubtractorCNT();
	else if (currMethod == 2) bgfs->algo = createBackgroundSubtractorKNN();
	else if (currMethod == 3) bgfs->algo = createBackgroundSubtractorMOG();
	else if (currMethod == 4) bgfs->algo = createBackgroundSubtractorMOG2();
	else if (currMethod == 5) bgfs->algo = createBackgroundSubtractorGSOC();
	else if (currMethod == 6) bgfs->algo = createBackgroundSubtractorLSBP();
	return bgfs;
}

extern "C" __declspec(dllexport)
int * BGSubtract_BGFG_Close(BGSubtract_BGFG *bgfs)
{
    delete bgfs;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int *BGSubtract_BGFG_Run(BGSubtract_BGFG *bgfs, int *bgrPtr, int rows, int cols, int channels)
{
	bgfs->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, bgrPtr);
	bgfs->Run();
    return (int *) bgfs->fgMask.data; 
}





class BGSubtract_Synthetic
{
private:
public:
	Ptr<bgsegm::SyntheticSequenceGenerator> gen;
	Mat src, output, fgMask;
    BGSubtract_Synthetic(){}
    void Run() {
	}
};

extern "C" __declspec(dllexport)
BGSubtract_Synthetic *BGSubtract_Synthetic_Open(int* bgrPtr, int rows, int cols, LPSTR fgFilename, double amplitude, double magnitude,
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
int * BGSubtract_Synthetic_Close(BGSubtract_Synthetic * cPtr)
{
    delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int *BGSubtract_Synthetic_Run(BGSubtract_Synthetic * cPtr)
{
	cPtr->gen->getNextFrame(cPtr->output, cPtr->fgMask);
    return (int *)cPtr->output.data; 
}
