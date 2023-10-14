#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace  cv;

class PCA_Prep
{
private:
public:
    vector<Point3f>pcaList;
    Mat src;
    PCA_Prep() {}
    void Run() {
        pcaList.clear();
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                auto vec = src.at<Point3f>(y, x);
                if (vec.z > 0) pcaList.push_back(vec);
            }
        }
    }
};

extern "C" __declspec(dllexport) PCA_Prep * PCA_Prep_Open() { PCA_Prep* cPtr = new PCA_Prep(); return cPtr; }
extern "C" __declspec(dllexport) int* PCA_Prep_Close(PCA_Prep * cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int PCA_Prep_GetCount(PCA_Prep* cPtr) { return int(cPtr->pcaList.size()); }
extern "C" __declspec(dllexport) int* PCA_Prep_Run(PCA_Prep * cPtr, int* pointCloudData, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_32FC3, pointCloudData);
    cPtr->Run();
    return (int*)cPtr->pcaList.data();
}