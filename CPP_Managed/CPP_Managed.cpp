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

namespace CPP_Managed {
    public struct unmanagedTaskStructure
    {
        Mat src, color, leftView, rightView, depthRGB, pointCloud;
        Mat dst0, dst1, dst2, dst3;
        int rows, cols;
    };

    unmanagedTaskStructure task;

    extern "C" __declspec(dllexport)
    void ManagedCPP_Resume(int rows, int cols, int* colorPtr, int* leftPtr, int* rightPtr, int* depthRGBPtr, int *cloudPtr) 
    {
        task.rows = rows;
        task.cols = cols;
        task.color = Mat(rows, cols, CV_8UC3, colorPtr).clone();
        task.leftView = Mat(rows, cols, CV_8UC3, leftPtr).clone();
        task.rightView = Mat(rows, cols, CV_8UC3, rightPtr).clone();
        task.depthRGB = Mat(rows, cols, CV_8UC3, depthRGBPtr).clone();
        task.pointCloud = Mat(rows, cols, CV_8UC3, cloudPtr).clone();

        task.dst0 = Mat(rows, cols, CV_8UC3);
        task.dst1 = Mat(rows, cols, CV_8UC3);
        task.dst2 = Mat(rows, cols, CV_8UC3);
        task.dst3 = Mat(rows, cols, CV_8UC3);

        task.src = task.color.clone();
    }

    extern "C" __declspec(dllexport)
    int* ManagedCPP_Pause() 
    {
        return (int*) task.dst2.data;
    }




    // everything is managed C++ from here.  Anything above is unmanaged.
    public ref class AddWeighted_Basics_CPP : public VB_Parent
    {
        Options_AddWeighted options;
    public:
        double weight;
        AddWeighted_Basics_CPP()
        {
            findSliderCPP("Add Weighted %", 49); // showing how to set a slider in managed C++ which doesn't have System.Windows.Forms.
            desc = "Add 2 images with specified weights.";
        }

        void RunAlg()
        {
            options.RunOpt();

            // algorithm user normally provides src2! 
            Mat src2, srcPlus;
            if (standaloneTest() || src2.empty()) srcPlus = task.depthRGB;
            if (srcPlus.type() != task.src.type())
            {
                if (task.src.type() != CV_8UC3 || srcPlus.type() != CV_8UC3)
                {
                    //if (task.src.type() == CV_32FC1) task.src = Convert32f_To_8UC3(task.src);
                    //if (srcPlus.type() == CV_32FC1) srcPlus = Convert32f_To_8UC3(srcPlus);
                    if (task.src.type() != CV_8UC3) cvtColor(task.src, task.src, COLOR_GRAY2BGR);
                    if (srcPlus.type() != CV_8UC3) cvtColor(srcPlus, srcPlus, COLOR_GRAY2BGR);
                }
            }

            weight = options.addWeighted;
            addWeighted(task.src, weight, srcPlus, 1.0 - weight, 0, task.dst2);

            //labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));
        }
    };
}

