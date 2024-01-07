#pragma once
#include "CPP_IncludeOnly.h"

CPP_Grid_Basics* gridBasics;

extern "C" __declspec(dllexport)
int * cppTask_Open(int function, int rows, int cols, bool heartBeat, float addWeighted, 
                   int lineWidth, int lineType, int dotSize,
                   int gridSize, int histogramBins, bool gravityPointCloud, int pixelDiffThreshold,
                   bool useKalman, int paletteIndex, bool optionsChanged, int frameHistory,
                   int clickX, int clickY, bool clickFlag, int picTag, int moveX, int moveY, bool moveUpdated,
                   int rectX, int rectY, int rectWidth, int rectHeight, bool displayDst0, bool displayDst1)
{
    task = new cppTask(rows, cols);

    task->heartBeat = heartBeat;
    task->lineType = lineType;
    task->lineWidth = lineWidth;
    task->dotSize = dotSize;
    task->pixelDiffThreshold = pixelDiffThreshold;
    task->gravityPointCloud = gravityPointCloud;
    task->addWeighted = double(addWeighted);
    task->gridSize = gridSize;
    task->histogramBins = histogramBins;
    task->useKalman = useKalman;
    task->paletteIndex = paletteIndex;
    task->optionsChanged = optionsChanged;
    task->frameHistory = frameHistory;
    task->clickPoint = Point(clickX, clickY);
    task->mouseMovePoint = Point(moveX, moveY);
    task->mouseClickFlag = clickFlag;
    task->mousePicTag = picTag;
    task->mouseMovePointUpdated = moveUpdated;
    task->drawRect = Rect(rectX, rectY, rectWidth, rectHeight);
    task->displayDst0 = displayDst0;
    task->displayDst1 = displayDst1;

    task->highlightColor = highlightColors[task->frameCount % highlightColors.size()];

    switch (function)
    {
    case CPP_AddWeighted_Basics_:
    {task->alg = new CPP_AddWeighted_Basics(rows, cols); break; }
	case CPP_Random_Enumerable_ :
	{task->alg = new CPP_Random_Enumerable(rows, cols); break; }
    case CPP_Bezier_Basics_:
    {task->alg = new CPP_Bezier_Basics(rows, cols); break; }
    case CPP_Feature_Agast_ :
    {task->alg = new CPP_Feature_Agast(rows, cols);break;}
    case CPP_Resize_Basics_ :
    {task->alg = new CPP_Resize_Basics(rows, cols);break;}
    case CPP_Delaunay_Basics_ :
    {task->alg = new CPP_Delaunay_Basics(rows, cols);break;}
    case CPP_Delaunay_GenerationsNoKNN_ :
    {task->alg = new CPP_Delaunay_GenerationsNoKNN(rows, cols);break;}
    case CPP_KNN_Basics_:
    {task->alg = new CPP_KNN_Basics(rows, cols); break; }
    case CPP_Random_Basics_ :
    {task->alg = new CPP_Random_Basics(rows, cols);break;}
    case CPP_KNN_Lossy_ :
    {task->alg = new CPP_KNN_Lossy(rows, cols);break;}
    case CPP_Delaunay_Generations_ :
    {task->alg = new CPP_Delaunay_Generations(rows, cols);break;}
    case CPP_Stable_Basics_ :
    {task->alg = new CPP_Stable_Basics(rows, cols);break;}
    case CPP_Feature_Basics_ :
    {task->alg = new CPP_Feature_Basics(rows, cols);break;}
	case CPP_Stable_BasicsCount_ :
	{task->alg = new CPP_Stable_BasicsCount(rows, cols);break;}
	case CPP_Remap_Basics_ :
	{task->alg = new CPP_Remap_Basics(rows, cols);break;}
	case CPP_Edge_Canny_ :
	{task->alg = new CPP_Edge_Canny(rows, cols);break;}
	case CPP_Edge_Sobel_ :
	{task->alg = new CPP_Edge_Sobel(rows, cols);break;}
	case CPP_Edge_Scharr_ :
	{task->alg = new CPP_Edge_Scharr(rows, cols);break;}
	case CPP_Mat_4to1_ :
	{task->alg = new CPP_Mat_4to1(rows, cols);break;}
	case CPP_Grid_Basics_ :
	{task->alg = new CPP_Grid_Basics(rows, cols);break;}
	case CPP_Depth_Colorizer_ :
	{task->alg = new CPP_Depth_Colorizer(rows, cols);break;}
	case CPP_RedCloud_PrepData_ :
	{task->alg = new CPP_RedCloud_PrepData(rows, cols);break;}
	case CPP_RedCloud_Flood_ :
	{task->alg = new CPP_RedCloud_Flood(rows, cols);break;}
	case CPP_Depth_PointCloud_ :
	{task->alg = new CPP_Depth_PointCloud(rows, cols);break;}
	case CPP_IMU_GMatrix_ :
	{task->alg = new CPP_IMU_GMatrix(rows, cols);break;}
	case CPP_IMU_GMatrix_QT_ :
	{task->alg = new CPP_IMU_GMatrix_QT(rows, cols);break;}
	case CPP_Depth_PointCloud_IMU_ :
	{task->alg = new CPP_Depth_PointCloud_IMU(rows, cols);break;}
	case CPP_Binarize_Simple_ :
	{task->alg = new CPP_Binarize_Simple(rows, cols);break;}
	case CPP_Plot_Histogram_ :
	{task->alg = new CPP_Plot_Histogram(rows, cols);break;}
	case CPP_Histogram_Basics_ :
	{task->alg = new CPP_Histogram_Basics(rows, cols);break;}
	case CPP_BackProject_Basics_ :
	{task->alg = new CPP_BackProject_Basics(rows, cols);break;}
	case CPP_Rectangle_Basics_ :
	{task->alg = new CPP_Rectangle_Basics(rows, cols);break;}
	case CPP_Rectangle_Rotated_ :
	{task->alg = new CPP_Rectangle_Rotated(rows, cols);break;}
	case CPP_Contour_Largest_ :
	{task->alg = new CPP_Contour_Largest(rows, cols);break;}
	case CPP_Diff_Basics_ :
	{task->alg = new CPP_Diff_Basics(rows, cols);break;}
	case CPP_ApproxPoly_FindandDraw_ :
	{task->alg = new CPP_ApproxPoly_FindandDraw(rows, cols);break;}
	case CPP_ApproxPoly_Basics_ :
	{task->alg = new CPP_ApproxPoly_Basics(rows, cols);break;}
	case CPP_Hull_Basics_ :
	{task->alg = new CPP_Hull_Basics(rows, cols);break;}
	case CPP_ApproxPoly_Hull_ :
	{task->alg = new CPP_ApproxPoly_Hull(rows, cols);break;}
	case CPP_Edge_Segments_ :
	{task->alg = new CPP_Edge_Segments(rows, cols);break;}
	case CPP_Motion_Basics_ :
	{task->alg = new CPP_Motion_Basics(rows, cols);break;}
	case CPP_Edge_MotionAccum_ :
	{task->alg = new CPP_Edge_MotionAccum(rows, cols);break;}
	case CPP_Edge_MotionFrames_ :
	{task->alg = new CPP_Edge_MotionFrames(rows, cols);break;}
	case CPP_EdgePreserving_Basics_ :
	{task->alg = new CPP_EdgePreserving_Basics(rows, cols);break;}
	case CPP_EdgeDraw_Basics_ :
	{task->alg = new CPP_EdgeDraw_Basics(rows, cols);break;}
	case CPP_TEE_Basics_ :
	{task->alg = new CPP_TEE_Basics(rows, cols);break;}
	case CPP_RedCloud_Hulls_ :
	{task->alg = new CPP_RedCloud_Hulls(rows, cols);break;}
	case CPP_Distance_Basics_ :
	{task->alg = new CPP_Distance_Basics(rows, cols);break;}
	case CPP_FeatureLess_Basics_ :
	{task->alg = new CPP_FeatureLess_Basics(rows, cols);break;}
	case CPP_FeatureLess_Edge_ :
	{task->alg = new CPP_FeatureLess_Edge(rows, cols);break;}
	case CPP_RedCloud_FeatureLess2_ :
	{task->alg = new CPP_RedCloud_FeatureLess2(rows, cols);break;}
    // end of switch - don't remove...
    }

    task->alg->standalone = true;
    task->font = FONT_HERSHEY_SIMPLEX;
    switch (cols)
    {
    case 1280:
    {
        task->fontSize = 1.2f;
        break;
    }
    case 640:
    {
        task->fontSize = 0.6f;
        break;
    }
    case 320:
    {
        task->fontSize = 0.35f;
        break;
    }
    case 160:
    {
        task->fontSize = 0.2f;
        break;
    }
    }
    task->fontColor = Scalar(255, 255, 255);
    task->cppFunction = function;
    gridBasics = new CPP_Grid_Basics(rows, cols);

    return (int *) task;
}



extern "C" __declspec(dllexport)
void cppTask_Labels(cppTask * task)
{
}




// https://www.codeproject.com/Articles/197493/Marshal-variable-length-array-of-structs-from-C-to
extern "C" __declspec(dllexport)
void cppTask_OptionsCPPtoVB(cppTask * task, int& gridSize,
    int& histogramBins, int& pixelDiffThreshold, bool& useKalman,
    int& frameHistory, int& rectX, int& rectY, int& rectWidth, int& rectHeight,
    LPSTR labelBuffer, LPSTR buffer)
{
    pixelDiffThreshold = task->pixelDiffThreshold;
    gridSize = task->gridSize;
    histogramBins = task->histogramBins;
    useKalman = task->useKalman;
    frameHistory = task->frameHistory;
    rectX = task->drawRect.x;
    rectY = task->drawRect.y;
    rectWidth = task->drawRect.width;
    rectHeight = task->drawRect.height;

    string labels = task->alg->labels[0] + "|" + task->alg->labels[1] + "|" + task->alg->labels[2] + "|" + task->alg->labels[3];
    memcpy(labelBuffer, labels.c_str(), labels.length() + 1);
    memcpy(buffer, task->alg->desc.c_str(), task->alg->desc.length() + 1);
}




// https://www.codeproject.com/Articles/197493/Marshal-variable-length-array-of-structs-from-C-to
extern "C" __declspec(dllexport)
void cppTask_OptionsVBtoCPP(cppTask * task, int& gridSize, 
    int& histogramBins, int& pixelDiffThreshold, bool& useKalman,
    int& frameHistory, int& rectX, int& rectY, int& rectWidth, int& rectHeight,
    int& lineWidth, int& lineType, int& dotSize)
{
    task->pixelDiffThreshold = pixelDiffThreshold;
    task->gridSize = gridSize;
    task->histogramBins = histogramBins;
    task->useKalman = useKalman;
    task->frameHistory = frameHistory;
    task->drawRect.x = rectX;
    task->drawRect.y = rectY;
    task->drawRect.width = rectWidth;
    task->drawRect.height = rectHeight;
    task->lineWidth = lineWidth;
    task->lineType = lineType;
    task->dotSize = dotSize;
}



extern "C" __declspec(dllexport)
int* cppTask_Close(cppTask * task)
{
    if (task == (cppTask*)0) return (int*)0;
    if (task->cppFunction != CPP_Delaunay_GenerationsNoKNN_) delete task;  // why does fDelaunay_GenerationsNoKNN fail.  Skipping it for now.
    return (int*)0;
}

extern "C" __declspec(dllexport)
int* cppTask_PointCloud(cppTask * task, int* dataPtr, int rows, int cols)
{
    task->pointCloud = Mat(rows, cols, CV_32FC3, dataPtr);
    split(task->pointCloud, task->pcSplit);
    task->depth32f = task->pcSplit[2] * 1000.0f;
    Mat depthRGBsplit[3];
    convertScaleAbs(task->pcSplit[0], depthRGBsplit[0], 255);
    convertScaleAbs(task->pcSplit[1], depthRGBsplit[1], 255);
    convertScaleAbs(task->pcSplit[2], depthRGBsplit[2], 255);
    vector<Mat> channels = { depthRGBsplit[0], depthRGBsplit[1], depthRGBsplit[2] };
    merge(channels, task->depthRGB);
    task->depthMask = task->depth32f > 0;
    bitwise_not(task->depthMask, task->noDepthMask);
    
    static CPP_Depth_PointCloud_IMU* pCloud = new CPP_Depth_PointCloud_IMU(rows, cols);
    pCloud->Run(task->pointCloud); // build the task->gCloud - oriented toward gravity.

    return (int*)task->depthRGB.data;
}

extern "C" __declspec(dllexport)
void cppTask_depthRGB(cppTask * task, int* dataPtr, int rows, int cols)
{
    task->pointCloud = Mat(rows, cols, CV_32FC3); // just in case the cppTask_PointCloud is not in use...
    task->pointCloud.setTo(0);
    task->depthRGB = Mat(rows, cols, CV_8UC3, dataPtr);
}


extern "C" __declspec(dllexport)
int* cppTask_GetDst3(cppTask * task)
{
    return (int*)task->xdst3.data;
}

extern "C" __declspec(dllexport)
int* cppTask_RunCPP(cppTask * task, int* dataPtr, int channels, int frameCount, int rows, int cols, float x, float y, float z,
                    bool optionsChanged, bool heartBeat, bool displayDst0, bool displayDst1, float addWeighted,
                    bool debugCheckBox)
{
    task->optionsChanged = optionsChanged;
    task->heartBeat = heartBeat;
    task->addWeighted = double(addWeighted);
    task->displayDst0 = displayDst0;
    task->displayDst1 = displayDst1;
    task->accRadians = Point3f(x, y, z);
    task->debugCheckBox = debugCheckBox;
    if (task->alg->dst0.rows != rows || task->alg->dst0.cols != cols)
    {
        task->alg->dst0 = Mat(rows, cols, CV_8UC3);
        task->alg->dst0.setTo(0);
    }
    if (task->alg->dst1.rows != rows || task->alg->dst1.cols != cols)
    {
        task->alg->dst1 = Mat(rows, cols, CV_8UC3);
        task->alg->dst1.setTo(0);
    }
    if (task->alg->dst2.rows != rows || task->alg->dst2.cols != cols)
    {
        task->alg->dst2 = Mat(rows, cols, CV_8UC3);
        task->alg->dst2.setTo(0);
    }
    if (task->alg->dst3.rows != rows || task->alg->dst3.cols != cols)
    {
        task->alg->dst3 = Mat(rows, cols, CV_8UC3);
        task->alg->dst3.setTo(0);
    }

    Mat src;
    if (channels == 3) src = Mat(rows, cols, CV_8UC3, dataPtr); else src = Mat(rows, cols, CV_8UC1, dataPtr);
    task->frameCount = frameCount;
    task->color = src.clone();

    // any algorithms run on every iteration are inserted here...
    gridBasics->Run(src);

    task->alg->Run(src);   //<<<<<< the real work is done here...

    task->firstPass = false;

    if (src.size() != task->alg->dst2.size()) resize(task->alg->dst2, task->alg->dst2, src.size());
    if (src.size() != task->alg->dst3.size()) resize(task->alg->dst3, task->alg->dst3, src.size());
    task->xdst0 = task->alg->dst0;
    task->xdst1 = task->alg->dst1;
    task->xdst2 = task->alg->dst2;
    task->xdst3 = task->alg->dst3;
    if (task->alg->dst0.type() == CV_32S) task->alg->dst3.convertTo(task->xdst3, CV_8U);
    if (task->alg->dst1.type() == CV_32S) task->alg->dst3.convertTo(task->xdst3, CV_8U);
    if (task->alg->dst2.type() == CV_32S) task->alg->dst3.convertTo(task->xdst3, CV_8U);
    if (task->alg->dst3.type() == CV_32S) task->alg->dst3.convertTo(task->xdst3, CV_8U);
    if (task->xdst0.type() == CV_8U) cvtColor(task->xdst0, task->xdst0, COLOR_GRAY2BGR);
    if (task->xdst1.type() == CV_8U) cvtColor(task->xdst1, task->xdst1, COLOR_GRAY2BGR);
    if (task->xdst2.type() == CV_8U) cvtColor(task->xdst2, task->xdst2, COLOR_GRAY2BGR);
    if (task->xdst3.type() == CV_8U) cvtColor(task->xdst3, task->xdst3, COLOR_GRAY2BGR);
    return (int*)task->xdst2.data;
}
