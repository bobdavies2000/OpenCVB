#pragma once
#include "CPP_AI_Generated.h"

namespace CPP_Classes
{
    Grid_Basics_CPP* gridBasics;
    Hist_RedOptions_CPP* redOptions;

    extern "C" __declspec(dllexport)
        int* cppTask_Open(int function, int rows, int cols, bool heartBeat, float addWeighted,
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
        case _AddWeighted_Basics_CPP:
        { task->alg = new AddWeighted_Basics_CPP(); task->alg->traceName = "AddWeighted_Basics_CPP"; break; }
        case _RedCloud_BasicsNative_CPP:
        { task->alg = new RedCloud_BasicsNative_CPP(); task->alg->traceName = "RedCloud_BasicsNative_CPP"; break; }
        case _RedCloud_Basics_CPP:
        { task->alg = new RedCloud_Basics_CPP(); task->alg->traceName = "RedCloud_Basics_CPP"; break; }
        case _BGSubtract_Basics_CPP:
        { task->alg = new BGSubtract_Basics_CPP(); task->alg->traceName = "BGSubtract_Basics_CPP"; break; }
        case _Plot_Histogram2D_CPP:
        { task->alg = new Plot_Histogram2D_CPP(); task->alg->traceName = "Plot_Histogram2D_CPP"; break; }
        case _Hist_RedOptions_CPP:
        { task->alg = new Hist_RedOptions_CPP(); task->alg->traceName = "Hist_RedOptions_CPP"; break; }
        case _Palette_Random_CPP:
        { task->alg = new Palette_Random_CPP(); task->alg->traceName = "Palette_Random_CPP"; break; }
        case _Blur_Basics_CPP:
        { task->alg = new Blur_Basics_CPP(); task->alg->traceName = "Blur_Basics_CPP"; break; }
        case _Color_Basics_CPP:
        { task->alg = new Color_Basics_CPP(); task->alg->traceName = "Color_Basics_CPP"; break; }
        case _Bin4Way_RegionsCombine_CPP:
        { task->alg = new Bin4Way_RegionsCombine_CPP(); task->alg->traceName = "Bin4Way_RegionsCombine_CPP"; break; }
        case _Bin4Way_Regions_CPP:
        { task->alg = new Bin4Way_Regions_CPP(); task->alg->traceName = "Bin4Way_Regions_CPP"; break; }
        case _Mat_4Click_CPP:
        { task->alg = new Mat_4Click_CPP(); task->alg->traceName = "Mat_4Click_CPP"; break; }
        case _Area_MinRect_CPP:
        { task->alg = new Area_MinRect_CPP(); task->alg->traceName = "Area_MinRect_CPP"; break; }
        case _Mesh_Basics_CPP:
        { task->alg = new Mesh_Basics_CPP(); task->alg->traceName = "Mesh_Basics_CPP"; break; }
        case _RedColor_FeatureLess_CPP:
        { task->alg = new RedColor_FeatureLess_CPP(); task->alg->traceName = "RedColor_FeatureLess_CPP"; break; }
        case _RedColor_FeatureLessCore_CPP:
        { task->alg = new RedColor_FeatureLessCore_CPP(); task->alg->traceName = "RedColor_FeatureLessCore_CPP"; break; }
        case _Palette_Basics_CPP:
        { task->alg = new Palette_Basics_CPP(); task->alg->traceName = "Palette_Basics_CPP"; break; }
        case _FeatureLess_History_CPP:
        { task->alg = new FeatureLess_History_CPP(); task->alg->traceName = "FeatureLess_History_CPP"; break; }
        case _Line_BasicsOld_CPP:
        { task->alg = new Line_BasicsOld_CPP(); task->alg->traceName = "Line_BasicsOld_CPP"; break; }
        case _Convex_Basics_CPP:
        { task->alg = new Convex_Basics_CPP(); task->alg->traceName = "Convex_Basics_CPP"; break; }
        case _Resize_Preserve_CPP:
        { task->alg = new Resize_Preserve_CPP(); task->alg->traceName = "Resize_Preserve_CPP"; break; }
        case _History_Basics_CPP:
        { task->alg = new History_Basics_CPP(); task->alg->traceName = "History_Basics_CPP"; break; }
        case _Motion_Simple_CPP:
        { task->alg = new Motion_Simple_CPP(); task->alg->traceName = "Motion_Simple_CPP"; break; }
        case _Hist_Kalman_CPP:
        { task->alg = new Hist_Kalman_CPP(); task->alg->traceName = "Hist_Kalman_CPP"; break; }
        case _Kalman_Basics_CPP:
        { task->alg = new Kalman_Basics_CPP(); task->alg->traceName = "Kalman_Basics_CPP"; break; }
        case _RedCloud_Reduce_CPP:
        { task->alg = new RedCloud_Reduce_CPP(); task->alg->traceName = "RedCloud_Reduce_CPP"; break; }
        case _Random_Enumerable_CPP:
        { task->alg = new RedCloud_Reduce_CPP(); task->alg->traceName = "RedCloud_Reduce_CPP"; break; }
        case _Bezier_Basics_CPP:
        { task->alg = new Bezier_Basics_CPP(); task->alg->traceName = "Bezier_Basics_CPP"; break; }
        case _Feature_Agast_CPP:
        { task->alg = new Feature_Agast_CPP(); task->alg->traceName = "Feature_Agast_CPP"; break; }
        case _Resize_Basics_CPP:
        { task->alg = new Resize_Basics_CPP(); task->alg->traceName = "Resize_Basics_CPP"; break; }
        case _Delaunay_Basics_CPP:
        { task->alg = new Delaunay_Basics_CPP(); task->alg->traceName = "Delaunay_Basics_CPP"; break; }
        case _Delaunay_GenerationsNoKNN_CPP:
        { task->alg = new Delaunay_GenerationsNoKNN_CPP(); task->alg->traceName = "Delaunay_GenerationsNoKNN_CPP"; break; }
        case _KNN_Core_CPP:
        { task->alg = new KNN_Core_CPP(); task->alg->traceName = "KNN_Core_CPP"; break; }
        case _Random_Basics_CPP:
        { task->alg = new Random_Basics_CPP(); task->alg->traceName = "Random_Basics_CPP"; break; }
        case _KNN_Basics_CPP:
        { task->alg = new KNN_Basics_CPP(); task->alg->traceName = "KNN_Basics_CPP"; break; }
        case _Delaunay_Generations_CPP:
        { task->alg = new Delaunay_Generations_CPP(); task->alg->traceName = "Delaunay_Generations_CPP"; break; }
        case _Remap_Basics_CPP:
        { task->alg = new Remap_Basics_CPP(); task->alg->traceName = "Remap_Basics_CPP"; break; }
        case _Edge_Canny_CPP:
        { task->alg = new Edge_Canny_CPP(); task->alg->traceName = "Edge_Canny_CPP"; break; }
        case _Edge_Sobel_CPP:
        { task->alg = new Edge_Sobel_CPP(); task->alg->traceName = "Edge_Sobel_CPP"; break; }
        case _Edge_Scharr_CPP:
        { task->alg = new Edge_Scharr_CPP(); task->alg->traceName = "Edge_Scharr_CPP"; break; }
        case _Mat_4to1_CPP:
        { task->alg = new Mat_4to1_CPP(); task->alg->traceName = "Mat_4to1_CPP"; break; }
        case _Grid_Basics_CPP:
        { task->alg = new Grid_Basics_CPP(); task->alg->traceName = "Grid_Basics_CPP"; break; }
        case _Depth_Colorizer_CPP:
        { task->alg = new Depth_Colorizer_CPP(); task->alg->traceName = "Depth_Colorizer_CPP"; break; }
        case _RedCloud_Flood_CPP:
        { task->alg = new RedCloud_Flood_CPP(); task->alg->traceName = "RedCloud_Flood_CPP"; break; }
        case _Depth_PointCloud_CPP:
        { task->alg = new Depth_PointCloud_CPP(); task->alg->traceName = "Depth_PointCloud_CPP"; break; }
        case _IMU_GMatrix_CPP:
        { task->alg = new IMU_GMatrix_CPP(); task->alg->traceName = "IMU_GMatrix_CPP"; break; }
        case _IMU_GMatrix_QT_CPP:
        { task->alg = new IMU_GMatrix_QT_CPP(); task->alg->traceName = "IMU_GMatrix_QT_CPP"; break; }
        case _Depth_PointCloud_IMU_CPP:
        { task->alg = new Depth_PointCloud_IMU_CPP(); task->alg->traceName = "Depth_PointCloud_IMU_CPP"; break; }
        case _Binarize_Simple_CPP:
        { task->alg = new Binarize_Simple_CPP(); task->alg->traceName = "Binarize_Simple_CPP"; break; }
        case _Plot_Histogram_CPP:
        { task->alg = new Plot_Histogram_CPP(); task->alg->traceName = "Plot_Histogram_CPP"; break; }
        case _Hist_Basics_CPP:
        { task->alg = new Hist_Basics_CPP(); task->alg->traceName = "Hist_Basics_CPP"; break; }
        case _BackProject_Basics_CPP:
        { task->alg = new BackProject_Basics_CPP(); task->alg->traceName = "BackProject_Basics_CPP"; break; }
        case _Rectangle_Basics_CPP:
        { task->alg = new Rectangle_Basics_CPP(); task->alg->traceName = "Rectangle_Basics_CPP"; break; }
        case _Rectangle_Rotated_CPP:
        { task->alg = new Rectangle_Rotated_CPP(); task->alg->traceName = "Rectangle_Rotated_CPP"; break; }
        case _Contour_Largest_CPP:
        { task->alg = new Contour_Largest_CPP(); task->alg->traceName = "Contour_Largest_CPP"; break; }
        case _Diff_Basics_CPP:
        { task->alg = new Diff_Basics_CPP(); task->alg->traceName = "Diff_Basics_CPP"; break; }
        case _ApproxPoly_FindandDraw_CPP:
        { task->alg = new ApproxPoly_FindandDraw_CPP(); task->alg->traceName = "ApproxPoly_FindandDraw_CPP"; break; }
        case _ApproxPoly_Basics_CPP:
        { task->alg = new ApproxPoly_Basics_CPP(); task->alg->traceName = "ApproxPoly_Basics_CPP"; break; }
        case _Hull_Basics_CPP:
        { task->alg = new Hull_Basics_CPP(); task->alg->traceName = "Hull_Basics_CPP"; break; }
        case _ApproxPoly_Hull_CPP:
        { task->alg = new ApproxPoly_Hull_CPP(); task->alg->traceName = "ApproxPoly_Hull_CPP"; break; }
        case _Edge_Segments_CPP:
        { task->alg = new Edge_Segments_CPP(); task->alg->traceName = "Edge_Segments_CPP"; break; }
        case _Motion_Basics_CPP:
        { task->alg = new Motion_Basics_CPP(); task->alg->traceName = "Motion_Basics_CPP"; break; }
        case _Edge_MotionFrames_CPP:
        { task->alg = new Edge_MotionFrames_CPP(); task->alg->traceName = "Edge_MotionFrames_CPP"; break; }
        case _Edge_Preserving_CPP:
        { task->alg = new Edge_Preserving_CPP(); task->alg->traceName = "Edge_Preserving_CPP"; break; }
        case _EdgeDraw_Basics_CPP:
        { task->alg = new EdgeDraw_Basics_CPP(); task->alg->traceName = "EdgeDraw_Basics_CPP"; break; }
        case _Distance_Basics_CPP:
        { task->alg = new Distance_Basics_CPP(); task->alg->traceName = "Distance_Basics_CPP"; break; }
        case _FeatureLess_Basics_CPP:
        { task->alg = new FeatureLess_Basics_CPP(); task->alg->traceName = "FeatureLess_Basics_CPP"; break; }
        case _FeatureLess_Edge_CPP:
        { task->alg = new FeatureLess_Edge_CPP(); task->alg->traceName = "FeatureLess_Edge_CPP"; break; }
        case _Stable_Basics_CPP:
        { task->alg = new Stable_Basics_CPP(); task->alg->traceName = "Stable_Basics_CPP"; break; }
        case _Feature_Basics_CPP:
        { task->alg = new Feature_Basics_CPP(); task->alg->traceName = "Feature_Basics_CPP"; break; }
        case _Stable_BasicsCount_CPP:
        { task->alg = new Stable_BasicsCount_CPP(); task->alg->traceName = "Stable_BasicsCount_CPP"; break; }
        case _FPoly_TopFeatures_CPP:
        { task->alg = new FPoly_TopFeatures_CPP(); task->alg->traceName = "FPoly_TopFeatures_CPP"; break; }
        case _Mesh_Features_CPP:
        { task->alg = new Mesh_Features_CPP(); task->alg->traceName = "Mesh_Features_CPP"; break; }
        case _Feature_AKaze_CPP:
        { task->alg = new Feature_AKaze_CPP(); task->alg->traceName = "Feature_AKaze_CPP"; break; }
        case _Feature_StableSorted_CPP:
        { task->alg = new Feature_StableSorted_CPP(); task->alg->traceName = "Feature_StableSorted_CPP"; break; }
		case _AddWeighted_DepthAccumulate_CPP:
		{ task->alg = new AddWeighted_DepthAccumulate_CPP(); task->alg->traceName = "AddWeighted_DepthAccumulate_CPP"; break; }
		case _Edge_Basics_CPP:
		{ task->alg = new Edge_Basics_CPP(); task->alg->traceName = "Edge_Basics_CPP"; break; }
		case _Hist_DepthSimple_CPP:
		{ task->alg = new Hist_DepthSimple_CPP(); task->alg->traceName = "Hist_DepthSimple_CPP"; break; }
        // end of switch - don't remove...
        }

        task->alg->standalone = true;
        task->font = FONT_HERSHEY_SIMPLEX; // fontSize is set below...
        task->fontColor = Scalar(255, 255, 255);
        task->cppFunction = function;
        gridBasics = new Grid_Basics_CPP();
        redOptions = new Hist_RedOptions_CPP();

        return (int*)task;
    }



    extern "C" __declspec(dllexport)
        void cppTask_Labels(cppTask* task)
    {
    }




    // https://www.codeproject.com/Articles/197493/Marshal-variable-length-array-of-structs-from-C-to
    extern "C" __declspec(dllexport)
        void cppTask_OptionsCPPtoVB(cppTask* task, int& gridSize,
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
        void cppTask_OptionsVBtoCPP(cppTask* task, int gridSize,
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
        int* cppTask_Close(cppTask* task)
    {
        if (task == (cppTask*)0) return (int*)0;
        if (task->cppFunction != _Delaunay_GenerationsNoKNN_CPP) delete task;  // why does fDelaunay_GenerationsNoKNN fail.  Skipping it for now.
        return (int*)0;
    }

    extern "C" __declspec(dllexport)
        int* cppTask_PointCloud(cppTask* task, int* dataPtr, int rows, int cols)
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

        static Depth_PointCloud_IMU_CPP* pCloud = new Depth_PointCloud_IMU_CPP();
        pCloud->Run(task->pointCloud); // build the task->gCloud - oriented toward gravity.

        return (int*)task->depthRGB.data;
    }

    extern "C" __declspec(dllexport)
        void cppTask_DepthLeftRight(cppTask* task, int* depthRGBPtr, int* leftView, int* rightView, int rows, int cols)
    {
        task->depthRGB = Mat(rows, cols, CV_8UC3, depthRGBPtr);
        task->leftView = Mat(rows, cols, CV_8UC3, leftView);
        task->rightView = Mat(rows, cols, CV_8UC3, rightView);
    }

    extern "C" __declspec(dllexport)
        int* cppTask_GetDst(cppTask* task, int index, int& type)
    {
        switch (index)
        {
        case 0:
            type = task->alg->dst0.type();
            return (int*)task->alg->dst0.data;
        case 1:
            type = task->alg->dst1.type();
            return (int*)task->alg->dst1.data;
        case 2:
            type = task->alg->dst2.type();
            return (int*)task->alg->dst2.data;
        case 3:
            type = task->alg->dst3.type();
            return (int*)task->alg->dst3.data;
        }
        return 0;
    }


    cv::Mat Convert32f_To_8UC3(const cv::Mat& input) {
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
            outMat = Convert32f_To_8UC3(input);  // Assuming this function is defined
        }
        else if (input.type() == CV_32SC1) {
            input.convertTo(outMat, CV_32F);
            outMat = Convert32f_To_8UC3(outMat);
        }
        else if (input.type() == CV_32SC3) {
            input.convertTo(outMat, CV_32F);
            cvtColor(outMat, outMat, COLOR_BGR2GRAY);
            outMat = Convert32f_To_8UC3(outMat);
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
        int* cppTask_RunCPP(cppTask* task, int* dataPtr, int channels, int frameCount, int rows, int cols, float x, float y, float z,
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

        //if (task->alg->dst0.type() != CV_8UC3) task->alg->dst0 = MakeSureImage8uC3(task->alg->dst0);
        //if (task->alg->dst1.type() != CV_8UC3) task->alg->dst1 = MakeSureImage8uC3(task->alg->dst1);
        //if (task->alg->dst2.type() != CV_8UC3) task->alg->dst2 = MakeSureImage8uC3(task->alg->dst2);
        //if (task->alg->dst3.type() != CV_8UC3) task->alg->dst3 = MakeSureImage8uC3(task->alg->dst3);

        return 0;
    }
}

