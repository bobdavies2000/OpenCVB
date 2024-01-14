#pragma once
#include "CPP_AI_Generated.h"

CPP_Grid_Basics* gridBasics;

extern "C" __declspec(dllexport)
int * cppTask_Open(int function, int rows, int cols, bool heartBeat, float addWeighted, 
                   int lineWidth, int lineType, int dotSize,
                   int gridSize, int histogramBins, bool gravityPointCloud, int pixelDiffThreshold,
                   bool useKalman, int paletteIndex, bool optionsChanged, int frameHistory,
                   bool displayDst0, bool displayDst1)
{
    task = new cppTask(rows, cols);
    workingRes = Size(rows, cols);
    task->heartBeat = heartBeat;
    task->lineType = lineType;
    task->lineWidth = lineWidth;
    task->dotSize = dotSize;
    task->pixelDiffThreshold = pixelDiffThreshold;
    task->gravityPointCloud = gravityPointCloud;
    task->AddWeighted = double(addWeighted);
    task->gridSize = gridSize;
    task->histogramBins = histogramBins;    
    task->useKalman = useKalman;
    task->paletteIndex = paletteIndex;
    task->optionsChanged = optionsChanged;
    task->frameHistoryCount = frameHistory;
    task->displayDst0 = displayDst0;
    task->displayDst1 = displayDst1;

    task->highlightColor = highlightColors[task->frameCount % highlightColors.size()];

    switch (function)
    {
    case _CPP_AddWeighted_Basics :
    {task->alg = new CPP_AddWeighted_Basics(); break; }
	case _CPP_Area_MinRect :
	{task->alg = new CPP_Area_MinRect(); break; }
	case _CPP_Mesh_Features :
	{task->alg = new CPP_Mesh_Features(); break; }
	case _CPP_Mesh_Agast:
	{task->alg = new CPP_Mesh_Agast(); break; }
	case _CPP_Mesh_Basics :
	{task->alg = new CPP_Mesh_Basics(); break; }
	case _CPP_RedMin_Basics :
	{task->alg = new CPP_RedMin_Basics(); break; }
	case _CPP_RedMin_Core :
	{task->alg = new CPP_RedMin_Core(); break; }
	case _CPP_Palette_Basics :
	{task->alg = new CPP_Palette_Basics(); break; }
	case _CPP_FeatureLess_History :
	{task->alg = new CPP_FeatureLess_History(); break; }
	case _CPP_Line_Basics :
	{task->alg = new CPP_Line_Basics(); break; }
	case _CPP_Convex_Basics :
	{task->alg = new CPP_Convex_Basics(); break; }
	case _CPP_Resize_Preserve :
	{task->alg = new CPP_Resize_Preserve(); break; } 
	case _CPP_History_Basics :
	{task->alg = new CPP_History_Basics(); break; }
	case _CPP_Motion_Core :
	{task->alg = new CPP_Motion_Core(); break; }
	case _CPP_Histogram_Kalman :
	{task->alg = new CPP_Histogram_Kalman(); break; }
	case _CPP_Kalman_Basics :
	{task->alg = new CPP_Kalman_Basics(); break; }
	case _CPP_RedCloud_Core :
	{task->alg = new CPP_RedCloud_Core(); break; }
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
    case _CPP_KNN_Basics :
    {task->alg = new CPP_KNN_Basics(); break; }
    case _CPP_Random_Basics :
    {task->alg = new CPP_Random_Basics();break;}
    case _CPP_KNN_Lossy :
    {task->alg = new CPP_KNN_Lossy();break;}
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
	case _CPP_Histogram_Basics :
	{task->alg = new CPP_Histogram_Basics();break;}
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
	case _CPP_Edge_MotionAccum :
	{task->alg = new CPP_Edge_MotionAccum();break;}
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
    LPSTR labelBuffer, LPSTR desc, LPSTR advice)
{
    pixelDiffThreshold = task->pixelDiffThreshold;
    gridSize = task->gridSize;
    histogramBins = task->histogramBins;
    useKalman = task->useKalman;
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
                            int histogramBins, int pixelDiffThreshold, bool useKalman,
                            int frameHistory, int rectX, int rectY, int rectWidth, int rectHeight,
                            int lineWidth, int lineType, int dotSize, int minResWidth, int minResHeight,
                            float maxZmeters, int PCReduction, float fontSize, int fontThickness,
                            int clickX, int clickY, bool clickFlag, int picTag, int moveX, int moveY,
                            int paletteIndex, int desiredCells, bool midHeartBeat, bool quarterBeat)
{
    task->pixelDiffThreshold = pixelDiffThreshold;
    task->gridSize = gridSize;
    task->histogramBins = histogramBins;
    task->useKalman = useKalman;
    task->frameHistoryCount = frameHistory;
    task->drawRect.x = rectX;
    task->drawRect.y = rectY;
    task->drawRect.width = rectWidth;
    task->drawRect.height = rectHeight;
    task->lineWidth = lineWidth;
    task->lineType = lineType;
    task->dotSize = dotSize;
    task->minRes = Size(minResWidth, minResHeight);
    task->maxZmeters = maxZmeters;
    task->PCReduction = PCReduction;
    task->cvFontSize = fontSize * 0.6;
    task->cvFontThickness = fontThickness;
    task->clickPoint = Point(clickX, clickY);
    task->mouseMovePoint = Point(moveX, moveY);
    task->mouseClickFlag = clickFlag;
    task->mousePicTag = picTag;
    task->paletteIndex = paletteIndex;
    task->desiredCells = desiredCells;
    task->midHeartBeat = midHeartBeat;
    task->quarterBeat = quarterBeat;
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

    threshold(task->pcSplit[2], task->maxDepthMask, task->maxZmeters, 255, THRESH_BINARY);  
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
        channels = task->xdst0.channels();
        return (int*)task->xdst0.data;
    case 1:
        channels = task->xdst1.channels();
        return (int*)task->xdst1.data;
    case 2:
        channels = task->xdst2.channels();
        return (int*)task->xdst2.data;
    case 3:
        channels = task->xdst3.channels();
        return (int*)task->xdst3.data;
    }
    return 0;
}

extern "C" __declspec(dllexport)
int* cppTask_RunCPP(cppTask * task, int* dataPtr, int channels, int frameCount, int rows, int cols, float x, float y, float z,
                    bool optionsChanged, bool heartBeat, bool displayDst0, bool displayDst1, float AddWeighted,
                    bool debugCheckBox)
{
    task->optionsChanged = optionsChanged;
    task->heartBeat = heartBeat;
    task->AddWeighted = double(AddWeighted);
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

    if (src.size() != task->alg->dst0.size()) resize(task->alg->dst0, task->alg->dst0, src.size());
    if (src.size() != task->alg->dst1.size()) resize(task->alg->dst1, task->alg->dst1, src.size());
    if (src.size() != task->alg->dst2.size()) resize(task->alg->dst2, task->alg->dst2, src.size());
    if (src.size() != task->alg->dst3.size()) resize(task->alg->dst3, task->alg->dst3, src.size());

    task->xdst0 = task->alg->dst0;
    task->xdst1 = task->alg->dst1;
    task->xdst2 = task->alg->dst2;
    task->xdst3 = task->alg->dst3;

    if (task->alg->dst0.type() == CV_32S) task->alg->dst0.convertTo(task->xdst0, CV_8U);
    if (task->alg->dst1.type() == CV_32S) task->alg->dst1.convertTo(task->xdst1, CV_8U);
    if (task->alg->dst2.type() == CV_32S) task->alg->dst2.convertTo(task->xdst2, CV_8U);
    if (task->alg->dst3.type() == CV_32S) task->alg->dst3.convertTo(task->xdst3, CV_8U);
    return (int*)task->xdst2.data;
}


