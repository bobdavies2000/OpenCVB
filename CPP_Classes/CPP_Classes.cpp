#include <iostream>
#include <string>
#include <cstdlib>
#include <cstdio>
#include <algorithm>
#include <map>
#include <numeric>
#include <iomanip>
#include <sstream>
#include <memory>
#include <vector>
#include <random>
#include <vcclr.h>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"
#include "opencv2/ml/ml.hpp "
#include "opencv2/imgproc.hpp"
#include "opencv2/videoio.hpp"
#include "opencv2/video/tracking.hpp"
#include "opencv2/bgsegm.hpp"
#include "opencv2/photo.hpp"
#include <opencv2/ml.hpp>
#include <opencv2/plot.hpp>
#include "opencv2/ccalib/randpattern.hpp"
#include "opencv2/xphoto/oilpainting.hpp"
#include "PragmaLibs.h"

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace VB_Classes;
using namespace std;
using namespace cv;

namespace CPP_Classes {
    Mat color, leftView, rightView, depthRGB, pointCloud;
    Mat dst0, dst1, dst2, dst3;
    int rows, cols;

    extern "C" __declspec(dllexport)
    void ManagedCPP_Resume(int _rows, int _cols, int* colorPtr, int* leftPtr, int* rightPtr, int* depthRGBPtr, int *cloudPtr) 
    {
        rows = _rows;
        cols = _cols;
        color = Mat(rows, cols, CV_8UC3, colorPtr).clone();
        leftView = Mat(rows, cols, CV_8UC3, leftPtr).clone();
        rightView = Mat(rows, cols, CV_8UC3, rightPtr).clone();
        depthRGB = Mat(rows, cols, CV_8UC3, depthRGBPtr).clone();
        pointCloud = Mat(rows, cols, CV_8UC3, cloudPtr).clone();

    }

    extern "C" __declspec(dllexport)
    void ManagedCPP_Pause() 
    {
    }

    public ref class cpp_Task : public VB_Parent
    {
    public:
        CPP_Managed^ tin = gcnew CPP_Managed();
        cpp_Task()
        {

        }

        Mat resumeTask()
        {
            return color.clone();
        }
        void pauseTask()
        {
            //tin->pauseTask((IntPtr)tdst0.data, (IntPtr)tdst1.data, (IntPtr)tdst2.dsta, (IntPtr)tdst3.data);
        }
    };


    public ref class AddWeighted_Basics_CPP : public VB_Parent
    {
        Options_AddWeighted options;
    public:
        double weight;
        cpp_Task^ task = gcnew cpp_Task();
        AddWeighted_Basics_CPP()
        {
            desc = "Add 2 images with specified weights.";
        }

        void RunAlg()
        {
            Mat src = task->resumeTask();
            Mat dst0 = Mat(rows, cols, CV_8UC3);
            Mat dst1 = Mat(rows, cols, CV_8UC3);
            Mat dst2 = Mat(rows, cols, CV_8UC3);
            Mat dst3 = Mat(rows, cols, CV_8UC3);
            
            options.RunOpt();

            //Size workingRes = test.workingRes;
             
            // algorithm user normally provides src2! 
            Mat src2, srcPlus;
            if (standaloneTest() || src2.empty()) srcPlus = depthRGB;
            if (srcPlus.type() != src.type())
            {
                if (src.type() != CV_8UC3 || srcPlus.type() != CV_8UC3)
                {
                    //if (src.type() == CV_32FC1) src = Convert32f_To_8UC3(src);
                    //if (srcPlus.type() == CV_32FC1) srcPlus = Convert32f_To_8UC3(srcPlus);
                    //if (src.type() != CV_8UC3) cvtColor(src, src, COLOR_GRAY2BGR);
                    //if (srcPlus.type() != CV_8UC3) cvtColor(srcPlus, srcPlus, COLOR_GRAY2BGR);
                }
            }

            weight = options.addWeighted;
            addWeighted(src, weight, depthRGB, 1.0 - weight, 0, dst2);

            imshow("dst2", dst2);
            //labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));

            task->pauseTask();
        }
    };
}

