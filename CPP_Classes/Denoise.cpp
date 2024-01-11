#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/photo.hpp"


using namespace std;
using namespace  cv;
class Denoise_Basics
{
private:
public:
    Mat dst;
    int frameCount = 5;
    std::vector<Mat> frames;
    Denoise_Basics() {}
    void Run() {
        dst = frames.back();
        fastNlMeansDenoisingMulti(frames, dst, 2, 1);
    }
};

extern "C" __declspec(dllexport)
Denoise_Basics * Denoise_Basics_Open(int frameCount) {
    Denoise_Basics* cPtr = new Denoise_Basics();
    cPtr->frameCount = frameCount;
    return cPtr;
}

extern "C" __declspec(dllexport)
int * Denoise_Basics_Close(Denoise_Basics * cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* Denoise_Basics_Run(Denoise_Basics * cPtr, int* bufferPtr, int rows, int cols)
{
    Mat src = Mat(rows, cols, CV_8UC1, bufferPtr);
    cPtr->frames.push_back(src.clone());
    if (cPtr->frames.size() < cPtr->frameCount)
    {
        return (int *) src.data;
    } else {
        cPtr->Run();
        cPtr->frames.pop_back();
    }
    return(int*) cPtr->dst.data; 
}






class Denoise_Pixels
{
private:
public:
    Mat src;
    int edgeCountAfter, edgeCountBefore;
    Denoise_Pixels() {}
    void RunCPP() {
        edgeCountBefore = 0;
        for (int y = 0; y < src.rows; y++)
            for (int x = 1; x < src.cols - 1; x++)
            {
                int last = src.at<uchar>(y, x - 1);
                int curr = src.at<uchar>(y, x);
                if (last != curr)
                {
                    edgeCountBefore++;
                    if (last == src.at<uchar>(y, x + 1))
                        src.at<uchar>(y, x) = last;
                }
            }
        for (int y = 1; y < src.rows - 1; y++)
            for (int x = 0; x < src.cols; x++)
            {
                int last = src.at<uchar>(y - 1, x);
                int curr = src.at<uchar>(y, x);
                if (last != curr)
                    if (last == src.at<uchar>(y + 1, x))
                        src.at<uchar>(y, x) = last;
            }
        edgeCountAfter = 0;
        for (int y = 0; y < src.rows; y++)
            for (int x = 1; x < src.cols; x++)
            {
                int last = src.at<uchar>(y, x - 1);
                int curr = src.at<uchar>(y, x);
                if (last != curr) edgeCountAfter++;
            }
    }
};
extern "C" __declspec(dllexport)
Denoise_Pixels * Denoise_Pixels_Open() {
    Denoise_Pixels* cPtr = new Denoise_Pixels();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Denoise_Pixels_Close(Denoise_Pixels * cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int Denoise_Pixels_EdgeCountBefore(Denoise_Pixels * cPtr)
{
    return cPtr->edgeCountBefore;
}

extern "C" __declspec(dllexport)
int Denoise_Pixels_EdgeCountAfter(Denoise_Pixels * cPtr)
{
    return cPtr->edgeCountAfter;
}
extern "C" __declspec(dllexport)
int* Denoise_Pixels_RunCPP(Denoise_Pixels * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int*)cPtr->src.data;
}
