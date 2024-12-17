#include <iostream>
#include <opencv2/opencv.hpp>
#pragma comment(lib, "opencv_world4100.lib")
//#pragma comment(lib, "opencv_highgui4100.lib")
//#pragma comment(lib, "opencv_imgproc4100.lib")
//#pragma comment(lib, "opencv_imgcodecs4100.lib")
using namespace cv;

extern "C" __declspec(dllexport)
void TestLib_LoadImage()
{
    //Mat img = imread("C:/_src/OpenCVB/Data/asahiyama.jpg", IMREAD_COLOR);
    //imshow("Image", img);
    //waitKey(10000);
}