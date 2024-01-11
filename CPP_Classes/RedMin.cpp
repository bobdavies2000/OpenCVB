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
class RedMin_FindPixels
{
private:
public:
    Mat src, dst;
    RedMin_FindPixels(){}
    vector <Vec3b> pixelList;
    void RunCPP() 
    {
        pixelList.clear();
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                auto val = src.at<Vec3b>(y, x);
                if(count(pixelList.begin(), pixelList.end(), val) == 0)
                    pixelList.push_back(val);
            }
        }
    }
};
extern "C" __declspec(dllexport)
RedMin_FindPixels *RedMin_FindPixels_Open() {
    RedMin_FindPixels *cPtr = new RedMin_FindPixels();
    return cPtr;
}
extern "C" __declspec(dllexport)
void RedMin_FindPixels_Close(RedMin_FindPixels *cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport) int* RedMin_FindPixels_Pixels(RedMin_FindPixels * cPtr)
{
    return (int*)&cPtr->pixelList[0];
}
extern "C" __declspec(dllexport)
int RedMin_FindPixels_RunCPP(RedMin_FindPixels *cPtr, int *dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
	cPtr->RunCPP();
	return (int)cPtr->pixelList.size();
}
