#include <iostream>
#include <string>
#include <cstdlib>
#include <cstdio>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"
#include "opencv2/ml/ml.hpp "
#include "opencv2/imgproc.hpp"
#include "opencv2/videoio.hpp"
#include <numeric>
#include <iomanip>
#include <sstream>
#include <memory>
#include <vector>
#include <random>
#include "opencv2/video/tracking.hpp"
#include "opencv2/bgsegm.hpp"
#include "opencv2/photo.hpp"
#include <map>
#include <opencv2/ml.hpp>
#include <opencv2/plot.hpp>
#include "opencv2/ccalib/randpattern.hpp"
#include "opencv2/xphoto/oilpainting.hpp"
#include <vcclr.h>
#include "PragmaLibs.h"

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace VB_Classes;
using namespace std;
using namespace cv;
//using namespace OpenCvSharp;

namespace CPP_Classes {
    Mat color, leftView, rightView, pointCloud, depthRGB;
    Mat tdst0, tdst1, tdst2, tdst3; 
    Mat src;
    public ref class cpp_Task : public VB_Parent
    {
    public:
        CPP_Managed^ tin = gcnew CPP_Managed();
        cpp_Task() {}

        void resumeTask()
        {
            tin->resumeTask();
            color = Mat(tin->rows, tin->cols, CV_8UC3, static_cast<uchar*>(tin->color.ToPointer()));
            src = color;
            depthRGB = Mat(tin->rows, tin->cols, CV_8UC3, static_cast<uchar*>(tin->depthRGB.ToPointer()));
            leftView = Mat(tin->rows, tin->cols, CV_8UC3, static_cast<uchar*>(tin->leftView.ToPointer()));
            rightView = Mat(tin->rows, tin->cols, CV_8UC3, static_cast<uchar*>(tin->rightView.ToPointer()));
            pointCloud = Mat(tin->rows, tin->cols, CV_32FC3, static_cast<uchar*>(tin->pointCloud.ToPointer()));
            tdst0 = Mat(tin->rows, tin->cols, CV_8UC3, static_cast<uchar*>(tin->dptr0.ToPointer()));
            tdst1 = Mat(tin->rows, tin->cols, CV_8UC3, static_cast<uchar*>(tin->dptr1.ToPointer()));
            tdst2 = Mat(tin->rows, tin->cols, CV_8UC3, static_cast<uchar*>(tin->dptr2.ToPointer()));
            tdst3 = Mat(tin->rows, tin->cols, CV_8UC3, static_cast<uchar*>(tin->dptr3.ToPointer()));
        }
        void pauseTask() 
        {
            tin->pauseTask();
        } 
    }; 


    public ref class AddWeighted_Basics_CPP : public VB_Parent
    {
        Options_AddWeighted options;
    public:
        cpp_Task^ task = gcnew cpp_Task();
        double weight;
        AddWeighted_Basics_CPP()
        {
            desc = "Add 2 images with specified weights.";
        }

        void RunAlg()
        {
            task->resumeTask();

            options.RunOpt();

            //Size workingRes = test.workingRes;



            //Mat test = task->depthRGB;
            //imshow("cppTask->depthRGB", cppTask->depthRGB);
            // algorithm user normally provides src2! 
            //if (standaloneTest() || src2.empty()) srcPlus = task.depthRGB;
            //if (srcPlus.type() != src.type())
            //{
            //    if (src.type() != CV_8UC3 || srcPlus.type() != CV_8UC3)
            //    {
            //        if (src.type() == CV_32FC1) src = Convert32f_To_8UC3(src);
            //        if (srcPlus.type() == CV_32FC1) srcPlus = Convert32f_To_8UC3(srcPlus);
            //        if (src.type() != CV_8UC3) cvtColor(src, src, COLOR_GRAY2BGR);
            //        if (srcPlus.type() != CV_8UC3) cvtColor(srcPlus, srcPlus, COLOR_GRAY2BGR);
            //    }
            //}

            weight = options.addWeighted;
            addWeighted(src, weight, depthRGB, 1.0 - weight, 0, tdst2);
            //labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));

            task->pauseTask();
        }
    };
}

