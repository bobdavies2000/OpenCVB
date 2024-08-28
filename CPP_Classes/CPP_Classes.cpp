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

namespace CPP_Classes {
    public ref class AddWeighted_Basics_CPP : public VB_Parent
    {
        Options_AddWeighted options;
        AddWeighted_BasicsTest test;
    public:
        VBtask task;
        double weight;
        AddWeighted_Basics_CPP()
        {
            desc = "Add 2 images with specified weights.";
        }

        void RunAlg(IntPtr dataPtr, int rows, int cols, int type)
        {
            //VBtask^ task = Marshal::PtrToStructure<VBtask^>(taskPtr);
            //imshow("DepthRGB", task->depthRGB);
            uchar* data = static_cast<uchar*>(dataPtr.ToPointer());
            Mat src(rows, cols, type, data);

            options.RunOpt();
            test.RunAlg();
            //Size workingRes = test.workingRes;



            Mat srcPlus(rows, cols, type);
            srcPlus.setTo(0);
            
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
            Mat dst2;
            addWeighted(src, weight, srcPlus, 1.0 - weight, 0, dst2);
           //labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));
            imshow("dst2", dst2);
        }
    };
}

