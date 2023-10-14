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
class MSER_Fast
{
private:
public:
    Mat src, dst;
    MSER_Fast(){}
    void RunCPP() {
        dst = src.clone();
    }
};
extern "C" __declspec(dllexport)
MSER_Fast *MSER_Fast_Open() {
    MSER_Fast *cPtr = new MSER_Fast();
    return cPtr;
}
extern "C" __declspec(dllexport)
void MSER_Fast_Close(MSER_Fast *cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int *MSER_Fast_RunCPP(MSER_Fast *cPtr, int *dataPtr, int rows, int cols, int channels)
{
		cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, dataPtr);
		cPtr->RunCPP();
		return (int *) cPtr->dst.data; 
}
