#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>


using namespace std;
using namespace  cv;
class MLPrepLearn
{
private:
public:
    Mat src, dst, response;
    int index = 0;
    int inputCount = 0;
    MLPrepLearn(){}
    void Run() {
        dst = Mat(src.rows * src.cols, 2, CV_32F);
        response = Mat(src.rows * src.cols, 1, CV_32F);
        dst.setTo(0);
        index = 0;
        inputCount = 0;
        int lastGrayDisp = -1;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                int grayDisp = src.at<int>(y, x);
                if (grayDisp == 0) break;
                inputCount++;
                int gray = int(grayDisp >> 8);
                int disp = grayDisp & 0xff;
                if (grayDisp != lastGrayDisp)
                {
                    dst.at<cv::Vec2f>(index, 0) = cv::Vec2f(float(gray), float(y));
                    response.at<float>(index, 0) = float(disp);
                    index++;
                    lastGrayDisp = grayDisp;
                }
            }
        }
    }
};

extern "C" __declspec(dllexport) MLPrepLearn *MLPrepLearn_Open() { MLPrepLearn*cPtr = new MLPrepLearn(); return cPtr; }
extern "C" __declspec(dllexport) int* MLPrepLearn_Close(MLPrepLearn * cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int MLPrepLearn_GetCount(MLPrepLearn * cPtr) { return cPtr->index - 1;} 
extern "C" __declspec(dllexport) int *MLPrepLearn_GetResponse(MLPrepLearn * cPtr) {  return (int *) cPtr->response.data; }
extern "C" __declspec(dllexport) int MLPrepLearn_GetInputCount(MLPrepLearn * cPtr) { return cPtr->inputCount; }
extern "C" __declspec(dllexport) int *MLPrepLearn_Run(MLPrepLearn *cPtr, int *grayDispPtr, int rows, int cols)
{
		cPtr->src = Mat(rows, cols, CV_32S, grayDispPtr);
		cPtr->Run();
		return (int *) cPtr->dst.data; 
}








class Sort_MLPrepTest
{
private:
public:
    Mat src, dst;
    Sort_MLPrepTest(){}
    void Run() {
        dst = Mat(src.rows, src.cols, CV_32FC2);
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                int gray = src.at<unsigned char>(y, x);
                dst.at<cv::Point2f>(y, x) = cv::Point2f(float(gray), float(y));
            }
        }
    }
};

extern "C" __declspec(dllexport) Sort_MLPrepTest *Sort_MLPrepTest_Open() { Sort_MLPrepTest *cPtr = new Sort_MLPrepTest(); return cPtr; }
extern "C" __declspec(dllexport) int * Sort_MLPrepTest_Close(Sort_MLPrepTest *cPtr) { delete cPtr; return (int*)0;}
extern "C" __declspec(dllexport) int *Sort_MLPrepTest_Run(Sort_MLPrepTest *cPtr, int * grayPtr, int rows, int cols) 
{
		cPtr->src = Mat(rows, cols, CV_8U, grayPtr);
		cPtr->Run();
		return (int *) cPtr->dst.data; 
}






class ML_RemoveDups
{
private:
public:
    Mat src, dst;
    ML_RemoveDups(){}
    int index = 0;
    void Run() {
        index = 0;
        int lastVal = -1;
        if (src.type() == CV_32S)
        {
            dst = Mat(int(src.total()), 1, CV_32S);
            dst.setTo(0);
            for (int y = 0; y < src.rows; y++)
            {
                for (int x = 0; x < src.cols; x++)
                {
                    int val = src.at<int>(y, x);
                    if (val != lastVal)
                    {
                        dst.at<int>(index, 0) = val;
                        lastVal = val;
                        index++;
                    }
                }
            }
        }
        else
        {
            dst = Mat(int(src.total()), 1, CV_8U);
            dst.setTo(0);
            for (int y = 0; y < src.rows; y++)
            {
                for (int x = 0; x < src.cols; x++)
                {
                    int val = src.at<unsigned char>(y, x);
                    if (val != lastVal)
                    {
                        dst.at<unsigned char>(index, 0) = val; 
                        lastVal = val;
                        index++;
                    }
                }
            }
        }
    }
};

extern "C" __declspec(dllexport) ML_RemoveDups *ML_RemoveDups_Open() { ML_RemoveDups *cPtr = new ML_RemoveDups(); return cPtr; }
extern "C" __declspec(dllexport) int ML_RemoveDups_GetCount(ML_RemoveDups * cPtr) { return cPtr->index - 1; }
extern "C" __declspec(dllexport) int* ML_RemoveDups_Close(ML_RemoveDups * cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int *ML_RemoveDups_Run(ML_RemoveDups *cPtr, int *dataPtr, int rows, int cols, int type)
{
		cPtr->src = Mat(rows, cols, type, dataPtr);
		cPtr->Run();
		return (int *) cPtr->dst.data; 
}










class FLess_Range
{
private:
public:
    Mat src, dst;
    vector<unsigned char> fBytes;
    FLess_Range() {}
    void Run() {
        dst = Mat(src.rows, src.cols, CV_32FC2);
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                int gray = src.at<unsigned char>(y, x);
                if (count(fBytes.begin(), fBytes.end(), gray) == 0)
                    fBytes.push_back(gray);
            }
        }
    }
};

extern "C" __declspec(dllexport) FLess_Range * FLess_Range_Open() { FLess_Range* cPtr = new FLess_Range(); return cPtr; }
extern "C" __declspec(dllexport) int* FLess_Range_Close(FLess_Range * cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int FLess_Range_Count(FLess_Range * cPtr) { return (int)cPtr->fBytes.size(); }
extern "C" __declspec(dllexport) int* FLess_Range_Run(FLess_Range * cPtr, int* grayPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8U, grayPtr);
    cPtr->Run();
    return (int*)cPtr->fBytes.data();
}
