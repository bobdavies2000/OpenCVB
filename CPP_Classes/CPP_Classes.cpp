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
    Mat src, color, leftView, rightView, depthRGB, pointCloud;
    Mat tdst0, tdst1, tdst2, tdst3;

    extern "C" __declspec(dllexport)
        void ManagedCPP_Setup(int* srcPtr, int rows, int cols) {
        src = Mat(rows, cols, CV_8UC3, srcPtr);
    }

    public ref class cpp_Task : public VB_Parent
    {
        uchar* pColor, * pLeft, * pRight, pDepthRGB, pCloud;
    public:
        CPP_Managed^ tin = gcnew CPP_Managed();
        VB_to_ManagedCPP^ vbd;
        cpp_Task()
        {

        }

        void resumeTask()
        {
            vbd = tin->resumeTask();

            imshow("src", src);
            //src = Mat(vbd->rows, vbd->cols, CV_8UC3, vbd->src);
            //src = Mat(vbd->rows, vbd->cols, CV_8UC3, vbd->src);
            //src = Mat(vbd->rows, vbd->cols, CV_8UC3, vbd->src);
            //src = Mat(vbd->rows, vbd->cols, CV_8UC3, vbd->src);
            //src = Mat(vbd->rows, vbd->cols, vbd->type, vbd->src);

            //color = tin->color;
            //depthRGB = tin->depthRGB;
            //leftView = tin->leftView;
            //rightView = tin->rightView;
            //pointCloud = tin->pointCloud;

            //tdst0 = tin->dst0;
            //tdst1 = tin->dst1;
            //tdst2 = tin->dst2;
            //tdst3 = tin->dst3;
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
        cpp_Task^ task = gcnew cpp_Task();

        double weight;
        AddWeighted_Basics_CPP()
        {
            desc = "Add 2 images with specified weights.";
        }

        //void RunAlg(IntPtr srcData, int rows, int cols, int type)
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
            //tdst2 = depthRGB;
            //tdst3 = src;
            //Cv2:AddWeighted(src, weight, task->depthRGB, 1.0 - weight, 0, task->tdst2);
            //labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));

            task->pauseTask();
        }
    };
}

