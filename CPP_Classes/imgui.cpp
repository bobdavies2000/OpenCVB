#include <librealsense2/rs.hpp>
#include "example-imgui.hpp"

#include <sstream>
#include <iostream>
#include <fstream>
#include <algorithm>
#include <cstring>

#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/photo.hpp"


using namespace std;
using namespace  cv;
class imgui_Example
{
private:
public:
    Mat dst;
    int frameCount;
    std::vector<Mat> frames;
    imgui_Example() 
    {
        
        // can't figure out how to get access to the imgui C++ interface.  What am I doing wrong...
        window app(1280, 720, "imgui C++ example"); 
        // ImGui_ImplGlfw_Init(app, false);      
    }
    void Run() {
        dst = frames.back();
        fastNlMeansDenoisingMulti(frames, dst, 2, 5);
    }
};

extern "C" __declspec(dllexport)
imgui_Example * imgui_Example_Open(int frameCount) {
    imgui_Example* cPtr = new imgui_Example();
    cPtr->frameCount = frameCount;
    return cPtr;
}

extern "C" __declspec(dllexport)
int * imgui_Example_Close(imgui_Example * cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* imgui_Example_Run(imgui_Example * cPtr, int* bufferPtr, int rows, int cols, int channels)
{
    Mat src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, bufferPtr);
    cPtr->frames.push_back(src);
    if (cPtr->frames.size() < cPtr->frameCount)
    {
        return (int*)src.data;
    }
    else {
        cPtr->Run();
        cPtr->frames.pop_back();
    }
    return(int*)cPtr->dst.data; 
}







class imgui_OpenGL
{
private:
public:
    Mat dst;
    int frameCount;
    std::vector<Mat> frames;
    imgui_OpenGL()
    {
        // can't figure out how to get access to the imgui C++ interface.  What am I doing wrong...
        //window app(1280, 720, "imgui C++ example"); 
        //ImGui_ImplGlfw_Init(app, false);      
    }
    void Run() {
        dst = frames.back();
        fastNlMeansDenoisingMulti(frames, dst, 2, 5);
    }
};

extern "C" __declspec(dllexport)
imgui_OpenGL * imgui_OpenGL_Open(int frameCount) {
    // ImGui_ImplGlfw_InitForOpenGL(window, true);
    imgui_OpenGL* cPtr = new imgui_OpenGL();
    cPtr->frameCount = frameCount;
    return cPtr;
}

extern "C" __declspec(dllexport)
int * imgui_OpenGL_Close(imgui_OpenGL * cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* imgui_OpenGL_Run(imgui_OpenGL * cPtr, int* bufferPtr, int rows, int cols, int channels)
{
    Mat src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, bufferPtr);
    cPtr->frames.push_back(src);
    if (cPtr->frames.size() < cPtr->frameCount)
    {
        return (int*)src.data;
    }
    else {
        cPtr->Run();
        cPtr->frames.pop_back();
    }
    return(int*)cPtr->dst.data; 
}