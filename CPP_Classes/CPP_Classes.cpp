#include "CPP_Classes.h"
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

using namespace System;
using namespace VB_Classes;

namespace CPP_Classes {
    public class CPP_Parent
    {
        gcroot<VBtask^> task;
        bool standalone;
        char* chars = new char[1];
        gcroot<String^> desc = gcnew String(chars);
        cv::Mat dst0, dst1, dst2, dst3, empty;
    };
    
    public class AddWeighted_Basics_CPP : public CPP_Parent
    {
    public:
        double weight;
        cv::Mat src2, dst2;
        gcroot<Options_AddWeighted^> options = gcnew Options_AddWeighted();

        AddWeighted_Basics_CPP() 
        {
            //desc = "Add 2 images with specified weights.";
        }

        void RunCS(cv::Mat& src)
        {
            options->RunVB();

            //cv::Mat srcPlus = src2;
            //// algorithm user normally provides src2! 
            //if (standaloneTest() || src2.empty()) srcPlus = task.depthRGB;
            //if (srcPlus.type() != src.type())
            //{
            //    if (src.type() != CV_8UC3 || srcPlus.type() != CV_8UC3)
            //    {
            //        if (src.type() == CV_32FC1) src = Convert32f_To_8UC3(src);
            //        if (srcPlus.type() == CV_32FC1) srcPlus = Convert32f_To_8UC3(srcPlus);
            //        if (src.type() != CV_8UC3) cv::cvtColor(src, src, cv::COLOR_GRAY2BGR);
            //        if (srcPlus.type() != CV_8UC3) cv::cvtColor(srcPlus, srcPlus, cv::COLOR_GRAY2BGR);
            //    }
            //}

            weight = 0.5;
            cv::addWeighted(src, weight, src2, 1.0 - weight, 0, dst2);
           //labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));
        }
    };
}

