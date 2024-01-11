#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>


using namespace std;
using namespace  cv;
class Fuzzy
{
private:
public:
    Mat src, dst;
    Fuzzy(){}
    void Run() {
        dst = Mat(src.rows, src.cols, CV_8U);
        dst.setTo(0);
        for (int y = 1; y < src.rows - 3; ++y)
        {
//#pragma omp parallel for 
            for (int x = 1; x < src.cols - 3; ++x)
            {
                int pixel = src.at<uchar>(y, x);
                Rect r = Rect(x, y, 3, 3);
                Scalar sum = cv::sum(src(r));
                if (sum.val[0] == double(pixel * 9)) dst.at<uchar>(y + 1, x + 1) = pixel;
            }
        }
    }
};

extern "C" __declspec(dllexport)
Fuzzy *Fuzzy_Open() {
    Fuzzy *cPtr = new Fuzzy();
    return cPtr;
}

extern "C" __declspec(dllexport)
int * Fuzzy_Close(Fuzzy *cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int *Fuzzy_Run(Fuzzy *cPtr, int *grayPtr, int rows, int cols)
{
		cPtr->src = Mat(rows, cols, CV_8UC1, grayPtr);
		cPtr->Run();
		return (int *) cPtr->dst.data; 
}