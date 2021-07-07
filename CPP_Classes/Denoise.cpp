// Cut And paste this code to a module in the "CPP_Classes" project for the C++ interface.
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/photo.hpp"

using namespace std;
using namespace cv;
class Denoise_Basics
{
private:
public:
    Mat dst;
    int frameCount;
    std::vector<Mat> frames;
    Denoise_Basics() {}
    void Run() {
        dst = frames.back();
        fastNlMeansDenoisingMulti(frames, dst, 2, 5);
    }
};

extern "C" __declspec(dllexport)
Denoise_Basics * Denoise_Basics_Open(int frameCount) {
    Denoise_Basics* cPtr = new Denoise_Basics();
    cPtr->frameCount = frameCount;
    return cPtr;
}

extern "C" __declspec(dllexport)
void Denoise_Basics_Close(Denoise_Basics * cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* Denoise_Basics_Run(Denoise_Basics * cPtr, int* bufferPtr, int rows, int cols, int channels)
{
    Mat src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, bufferPtr);
    cPtr->frames.push_back(src);
    if (cPtr->frames.size() < cPtr->frameCount)
    {
        return (int *) src.data;
    } else {
        cPtr->Run();
        cPtr->frames.pop_back();
    }
    return(int*) cPtr->dst.data; // Return this C++ allocated data To managed code
}