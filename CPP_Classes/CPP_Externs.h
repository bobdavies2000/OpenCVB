#pragma once
#include "CPP_AI_Generated.h"

CPP_Grid_Basics* gridBasics;
CPP_Hist_RedOptions* redOptions;

extern "C" __declspec(dllexport)
int * cppTask_Open(int function, int rows, int cols, bool heartBeat, float addWeighted, 
                   int lineWidth, int lineType, int DotSize,
                   int gridSize, int histogramBins, bool gravityPointCloud, int pixelDiffThreshold,
                   bool UseKalman, int paletteIndex, bool optionsChanged, int frameHistory,
                   bool displayDst0, bool displayDst1)
{
    task = new cppTask(rows, cols);
    WorkingRes = Size(cols, rows);
    task->heartBeat = heartBeat;
    task->lineType = lineType;
    task->lineWidth = lineWidth;
    task->DotSize = DotSize;
    task->pixelDiffThreshold = pixelDiffThreshold;
    task->gravityPointCloud = gravityPointCloud;
    task->AddWeighted = double(addWeighted);
    task->gridSize = gridSize;
    task->histogramBins = histogramBins;    
    task->UseKalman = UseKalman;
    task->paletteIndex = paletteIndex;
    task->optionsChanged = optionsChanged;
    task->frameHistoryCount = frameHistory;
    task->displayDst0 = displayDst0;
    task->displayDst1 = displayDst1;

    task->HighlightColor = HighlightColors[task->frameCount % HighlightColors.size()];

    switch (function)
    {
    case _CPP_AddWeighted_Basics :
    {task->alg = new CPP_AddWeighted_Basics(); break; } 
    case _CPP_RedCloud_BasicsNative:
    {task->alg = new CPP_RedCloud_BasicsNative(); break; }
    case _CPP_RedCloud_Basics:
    {task->alg = new CPP_RedCloud_Basics(); break; }
    case _CPP_BGSubtract_Basics :
	{task->alg = new CPP_BGSubtract_Basics(); break; }
	case _CPP_Feature_StableSorted :
	{task->alg = new CPP_Feature_StableSorted(); break; }
    case _CPP_Plot_Histogram2D:
    {task->alg = new CPP_Plot_Histogram2D(); break; }
    case _CPP_Hist_RedOptions:
    {task->alg = new CPP_Hist_RedOptions(); break; }
    case _CPP_Palette_Random :
	{task->alg = new CPP_Palette_Random(); break; }
	case _CPP_Blur_Basics :
	{task->alg = new CPP_Blur_Basics(); break; }
	case _CPP_Color_Basics :
	{task->alg = new CPP_Color_Basics(); break; }
	case _CPP_Bin4Way_RegionsCombine :
	{task->alg = new CPP_Bin4Way_RegionsCombine(); break; }
	case _CPP_Bin4Way_Regions :
	{task->alg = new CPP_Bin4Way_Regions(); break; }
	case _CPP_Mat_4Click :
	{task->alg = new CPP_Mat_4Click(); break; }
	case _CPP_Area_MinRect :
	{task->alg = new CPP_Area_MinRect(); break; }
	case _CPP_Mesh_Features :
	{task->alg = new CPP_Mesh_Features(); break; }
	case _CPP_Mesh_Agast:
	{task->alg = new CPP_Mesh_Agast(); break; }
	case _CPP_Mesh_Basics :
	{task->alg = new CPP_Mesh_Basics(); break; }
	case _CPP_RedColor_FeatureLess :
	{task->alg = new CPP_RedColor_FeatureLess(); break; }
	case _CPP_RedColor_FeatureLessCore :
	{task->alg = new CPP_RedColor_FeatureLessCore(); break; }
	case _CPP_Palette_Basics :
	{task->alg = new CPP_Palette_Basics(); break; }
	case _CPP_FeatureLess_History :
	{task->alg = new CPP_FeatureLess_History(); break; }
	case _CPP_Line_BasicsOld :
	{task->alg = new CPP_Line_BasicsOld(); break; }
	case _CPP_Convex_Basics :
	{task->alg = new CPP_Convex_Basics(); break; }
	case _CPP_Resize_Preserve :
	{task->alg = new CPP_Resize_Preserve(); break; } 
	case _CPP_History_Basics :
	{task->alg = new CPP_History_Basics(); break; }
	case _CPP_Motion_Simple :
	{task->alg = new CPP_Motion_Simple(); break; }
	case _CPP_Hist_Kalman :
	{task->alg = new CPP_Hist_Kalman(); break; }
	case _CPP_Kalman_Basics :
	{task->alg = new CPP_Kalman_Basics(); break; }
	case _CPP_RedCloud_Reduce :
	{task->alg = new CPP_RedCloud_Reduce(); break; }
	case _CPP_FPoly_TopFeatures :
	{task->alg = new CPP_FPoly_TopFeatures(); break; }
	case _CPP_Random_Enumerable :
	{task->alg = new CPP_Random_Enumerable(); break; }
    case _CPP_Bezier_Basics :
    {task->alg = new CPP_Bezier_Basics(); break; }
    case _CPP_Feature_Agast :
    {task->alg = new CPP_Feature_Agast();break;}
    case _CPP_Resize_Basics :
    {task->alg = new CPP_Resize_Basics();break;}
    case _CPP_Delaunay_Basics :
    {task->alg = new CPP_Delaunay_Basics();break;}
    case _CPP_Delaunay_GenerationsNoKNN :
    {task->alg = new CPP_Delaunay_GenerationsNoKNN();break;}
    case _CPP_KNN_Core :
    {task->alg = new CPP_KNN_Core(); break; }
    case _CPP_Random_Basics :
    {task->alg = new CPP_Random_Basics();break;}
    case _CPP_KNN_Basics :
    {task->alg = new CPP_KNN_Basics();break;}
    case _CPP_Delaunay_Generations :
    {task->alg = new CPP_Delaunay_Generations();break;}
    case _CPP_Stable_Basics :
    {task->alg = new CPP_Stable_Basics();break;}
    case _CPP_Feature_Basics :
    {task->alg = new CPP_Feature_Basics();break;}
	case _CPP_Stable_BasicsCount :
	{task->alg = new CPP_Stable_BasicsCount();break;}
	case _CPP_Remap_Basics :
	{task->alg = new CPP_Remap_Basics();break;}
	case _CPP_Edge_Canny :
	{task->alg = new CPP_Edge_Canny();break;}
	case _CPP_Edge_Sobel :
	{task->alg = new CPP_Edge_Sobel();break;}
	case _CPP_Edge_Scharr :
	{task->alg = new CPP_Edge_Scharr();break;}
	case _CPP_Mat_4to1 :
	{task->alg = new CPP_Mat_4to1();break;}
	case _CPP_Grid_Basics :
	{task->alg = new CPP_Grid_Basics();break;}
	case _CPP_Depth_Colorizer :
	{task->alg = new CPP_Depth_Colorizer();break;}
	case _CPP_RedCloud_Flood :
	{task->alg = new CPP_RedCloud_Flood();break;}
	case _CPP_Depth_PointCloud :
	{task->alg = new CPP_Depth_PointCloud();break;}
	case _CPP_IMU_GMatrix :
	{task->alg = new CPP_IMU_GMatrix();break;}
	case _CPP_IMU_GMatrix_QT :
	{task->alg = new CPP_IMU_GMatrix_QT();break;}
	case _CPP_Depth_PointCloud_IMU :
	{task->alg = new CPP_Depth_PointCloud_IMU();break;}
	case _CPP_Binarize_Simple :
	{task->alg = new CPP_Binarize_Simple();break;}
	case _CPP_Plot_Histogram :
	{task->alg = new CPP_Plot_Histogram();break;}
	case _CPP_Hist_Basics :
	{task->alg = new CPP_Hist_Basics();break;}
	case _CPP_BackProject_Basics :
	{task->alg = new CPP_BackProject_Basics();break;}
	case _CPP_Rectangle_Basics :
	{task->alg = new CPP_Rectangle_Basics();break;}
	case _CPP_Rectangle_Rotated :
	{task->alg = new CPP_Rectangle_Rotated();break;}
	case _CPP_Contour_Largest :
	{task->alg = new CPP_Contour_Largest();break;}
	case _CPP_Diff_Basics :
	{task->alg = new CPP_Diff_Basics();break;}
	case _CPP_ApproxPoly_FindandDraw :
	{task->alg = new CPP_ApproxPoly_FindandDraw();break;}
	case _CPP_ApproxPoly_Basics :
	{task->alg = new CPP_ApproxPoly_Basics();break;}
	case _CPP_Hull_Basics :
	{task->alg = new CPP_Hull_Basics();break;}
	case _CPP_ApproxPoly_Hull :
	{task->alg = new CPP_ApproxPoly_Hull();break;}
	case _CPP_Edge_Segments :
	{task->alg = new CPP_Edge_Segments();break;}
	case _CPP_Motion_Basics :
	{task->alg = new CPP_Motion_Basics();break;}
	case _CPP_Edge_MotionFrames :
	{task->alg = new CPP_Edge_MotionFrames();break;}
	case _CPP_Edge_Preserving:
	{task->alg = new CPP_Edge_Preserving();break;}
	case _CPP_EdgeDraw_Basics :
	{task->alg = new CPP_EdgeDraw_Basics();break;}
	case _CPP_Distance_Basics :
	{task->alg = new CPP_Distance_Basics();break;}
	case _CPP_FeatureLess_Basics :
	{task->alg = new CPP_FeatureLess_Basics();break;}
	case _CPP_FeatureLess_Edge :
	{task->alg = new CPP_FeatureLess_Edge();break;}
    // end of switch - don't remove...
    }

    task->alg->standalone = true;
    task->font = FONT_HERSHEY_SIMPLEX; // fontSize is set below...
    task->fontColor = Scalar(255, 255, 255);
    task->cppFunction = function;
    gridBasics = new CPP_Grid_Basics();
    redOptions = new CPP_Hist_RedOptions();

    return (int *) task;
}



extern "C" __declspec(dllexport)
void cppTask_Labels(cppTask * task)
{
}




// https://www.codeproject.com/Articles/197493/Marshal-variable-length-array-of-structs-from-C-to
extern "C" __declspec(dllexport)
void cppTask_OptionsCPPtoVB(cppTask * task, int& gridSize,
    int& histogramBins, int& pixelDiffThreshold, bool& UseKalman,
    int& frameHistory, int& rectX, int& rectY, int& rectWidth, int& rectHeight,
    LPSTR labelBuffer, LPSTR desc, LPSTR advice)
{
    pixelDiffThreshold = task->pixelDiffThreshold;
    gridSize = task->gridSize;
    histogramBins = task->histogramBins;
    UseKalman = task->UseKalman;
    frameHistory = task->frameHistoryCount;
    rectX = task->drawRect.x;
    rectY = task->drawRect.y;
    rectWidth = task->drawRect.width;
    rectHeight = task->drawRect.height;

    string labels = task->alg->labels[0] + "|" + task->alg->labels[1] + "|" + task->alg->labels[2] + "|" + task->alg->labels[3];
    memcpy(labelBuffer, labels.c_str(), labels.length() + 1);
    memcpy(desc, task->alg->desc.c_str(), task->alg->desc.length() + 1);
    memcpy(advice, task->alg->advice.c_str(), task->alg->advice.length() + 1);
}




// https://www.codeproject.com/Articles/197493/Marshal-variable-length-array-of-structs-from-C-to
extern "C" __declspec(dllexport)
void cppTask_OptionsVBtoCPP(cppTask * task, int gridSize,
                            int histogramBins, int pixelDiffThreshold, bool UseKalman,
                            int frameHistory, int rectX, int rectY, int rectWidth, int rectHeight,
                            int lineWidth, int lineType, int DotSize, int lowResWidth, int lowResHeight,
                            float MaxZmeters, int PCReduction, float fontSize, int fontThickness,
                            int clickX, int clickY, bool clickFlag, int picTag, int moveX, int moveY,
                            int paletteIndex, int desiredCells, bool midHeartBeat, bool quarterBeat,
                            int colorIndex, int depthInputIndex, float xRangeDefault, float yRangeDefault)
{
    task->pixelDiffThreshold = pixelDiffThreshold;
    task->gridSize = gridSize;
    task->histogramBins = histogramBins;
    task->UseKalman = UseKalman;
    task->frameHistoryCount = frameHistory;
    task->drawRect.x = rectX;
    task->drawRect.y = rectY;
    task->drawRect.width = rectWidth;
    task->drawRect.height = rectHeight;
    task->lineWidth = lineWidth;
    task->lineType = lineType;
    task->DotSize = DotSize;
    task->lowRes = Size(lowResWidth, lowResHeight);
    task->MaxZmeters = MaxZmeters;
    task->PCReduction = PCReduction;
    task->cvFontSize = fontSize * 0.6;
    task->cvFontThickness = fontThickness;
    task->ClickPoint = Point(clickX, clickY);
    task->mouseMovePoint = Point(moveX, moveY);
    task->mouseClickFlag = clickFlag;
    task->mousePicTag = picTag;
    task->paletteIndex = paletteIndex;
    task->desiredCells = desiredCells;
    task->midHeartBeat = midHeartBeat;
    task->quarterBeat = quarterBeat;
    task->colorInputIndex = colorIndex;
    task->depthInputIndex = depthInputIndex;
    task->xRangeDefault = xRangeDefault;
    task->yRangeDefault = yRangeDefault;
}



extern "C" __declspec(dllexport)
int* cppTask_Close(cppTask * task)
{
    if (task == (cppTask*)0) return (int*)0;
    if (task->cppFunction != _CPP_Delaunay_GenerationsNoKNN) delete task;  // why does fDelaunay_GenerationsNoKNN fail.  Skipping it for now.
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
    task->depthMask = task->depth32f > 0;
    bitwise_not(task->depthMask, task->noDepthMask);

    threshold(task->pcSplit[2], task->maxDepthMask, task->MaxZmeters, 255, THRESH_BINARY);  
    task->maxDepthMask.convertTo(task->maxDepthMask, CV_8U);

    static CPP_Depth_PointCloud_IMU* pCloud = new CPP_Depth_PointCloud_IMU();
    pCloud->Run(task->pointCloud); // build the task->gCloud - oriented toward gravity.

    return (int*)task->depthRGB.data;
}

extern "C" __declspec(dllexport)
void cppTask_DepthLeftRight(cppTask * task, int* depthRGBPtr, int* leftView, int* rightView, int rows, int cols)
{
    task->depthRGB = Mat(rows, cols, CV_8UC3, depthRGBPtr);
    task->leftView = Mat(rows, cols, CV_8UC3, leftView);
    task->rightView = Mat(rows, cols, CV_8UC3, rightView);
}

extern "C" __declspec(dllexport)
int* cppTask_GetDst(cppTask * task, int index, int& channels)
{
    switch (index)
    {
    case 0:
        channels = task->alg->dst0.channels();
        return (int*)task->alg->dst0.data;
    case 1:
        channels = task->alg->dst1.channels();
        return (int*)task->alg->dst1.data;
    case 2:
        channels = task->alg->dst2.channels();
        return (int*)task->alg->dst2.data;
    case 3:
        channels = task->alg->dst3.channels();
        return (int*)task->alg->dst3.data;
    }
    return 0;
}


cv::Mat GetNormalize32f(const cv::Mat& input) {
    cv::Mat outMat;
    cv::normalize(input, outMat, 0, 255, cv::NORM_MINMAX, CV_8U);  // Normalize to 8-bit unsigned

    if (input.channels() == 1) {
        cv::cvtColor(outMat, outMat, cv::COLOR_GRAY2BGR);  // Convert single-channel to BGR
    }
    else {
        outMat.convertTo(outMat, CV_8UC3);  // Convert to 3-channel CV_8UC3 for consistency
    }

    return outMat;
}

Mat MakeSureImage8uC3(const Mat& input) {
    Mat outMat;
    if (input.type() == CV_8UC3) {
        outMat = input.clone();
        return outMat;
    }

    if (input.type() == CV_32F) {
        outMat = GetNormalize32f(input);  // Assuming this function is defined
    }
    else if (input.type() == CV_32SC1) {
        input.convertTo(outMat, CV_32F);
        outMat = GetNormalize32f(outMat);
    }
    else if (input.type() == CV_32SC3) {
        input.convertTo(outMat, CV_32F);
        cvtColor(outMat, outMat, COLOR_BGR2GRAY);
        outMat = GetNormalize32f(outMat);
    }
    else if (input.type() == CV_32FC3) {
        std::vector<Mat> split;
        cv::split(input, split);
        split[0].convertTo(split[0], CV_8U, 255);  // ConvertScaleAbs equivalent
        split[1].convertTo(split[1], CV_8U, 255);
        split[2].convertTo(split[2], CV_8U, 255);
        merge(split, outMat);
    }
    else {
        outMat = input.clone();
    }

    if (input.channels() == 1 && input.type() == CV_8UC1) {
        cvtColor(input, outMat, COLOR_GRAY2BGR);
    }

    return outMat;
}


extern "C" __declspec(dllexport)
int* cppTask_RunCPP(cppTask * task, int* dataPtr, int channels, int frameCount, int rows, int cols, float x, float y, float z,
                    bool optionsChanged, bool heartBeat, bool displayDst0, bool displayDst1, bool debugCheckBox)
{
    task->optionsChanged = optionsChanged;
    task->heartBeat = heartBeat;
    task->AddWeighted = double(0.5);
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
    redOptions->Run(src);

    task->alg->Run(src);   //<<<<<< the real work is done here...

    task->FirstPass = false;

    if (src.size() != task->alg->dst0.size()) resize(task->alg->dst0, task->alg->dst0, src.size());
    if (src.size() != task->alg->dst1.size()) resize(task->alg->dst1, task->alg->dst1, src.size());
    if (src.size() != task->alg->dst2.size()) resize(task->alg->dst2, task->alg->dst2, src.size());
    if (src.size() != task->alg->dst3.size()) resize(task->alg->dst3, task->alg->dst3, src.size());

    if (task->alg->dst0.type() != CV_8UC3) task->alg->dst0 = MakeSureImage8uC3(task->alg->dst0);
    if (task->alg->dst1.type() != CV_8UC3) task->alg->dst1 = MakeSureImage8uC3(task->alg->dst1);
    if (task->alg->dst2.type() != CV_8UC3) task->alg->dst2 = MakeSureImage8uC3(task->alg->dst2);
    if (task->alg->dst3.type() != CV_8UC3) task->alg->dst3 = MakeSureImage8uC3(task->alg->dst3);

    return 0;
}


