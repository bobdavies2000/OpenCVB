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
        int rows, cols;
    };

    unmanagedTaskStructure task;

    public struct unManagedIO
    {
        Mat src, dst0, dst1, dst2, dst3;
        int rows, cols;
        unManagedIO(int _rows, int _cols)
        {
            rows = _rows;
            cols = _cols;
            src = task.color.clone();
            dst0 = Mat(rows, cols, CV_8UC3);
            dst1 = Mat(rows, cols, CV_8UC3);
            dst2 = Mat(rows, cols, CV_8UC3);
            dst3 = Mat(rows, cols, CV_8UC3);
        }
    };

    vector<unManagedIO*> ioList;

    extern "C" __declspec(dllexport)
    int ManagedCPP_Initialize(int rows, int cols)
    {
        unManagedIO *io = new unManagedIO(rows, cols);
        int ioIndex = ioList.size();
        ioList.push_back(io);
        return ioIndex;
    }



    extern "C" __declspec(dllexport)
        void ManagedCPP_Resume(int ioIndex, int* colorPtr, int* leftPtr, int* rightPtr, int* depthRGBPtr, int* cloudPtr)
    {
        int rows = ioList[ioIndex]->rows;
        int cols = ioList[ioIndex]->cols;
        task.color = Mat(rows, cols, CV_8UC3, colorPtr).clone();
        task.leftView = Mat(rows, cols, CV_8UC3, leftPtr).clone();
        task.rightView = Mat(rows, cols, CV_8UC3, rightPtr).clone();
        task.depthRGB = Mat(rows, cols, CV_8UC3, depthRGBPtr).clone();
        task.pointCloud = Mat(rows, cols, CV_8UC3, cloudPtr).clone();

        ioList[ioIndex]->src = task.color.clone();
    }

    unsigned char** dst = new unsigned char* [4];
    extern "C" __declspec(dllexport)
    unsigned char** ManagedCPP_Pause(int ioIndex)
    {
        dst[0] = ioList[ioIndex]->dst0.data;
        dst[1] = ioList[ioIndex]->dst1.data;
        dst[2] = ioList[ioIndex]->dst2.data;
        dst[3] = ioList[ioIndex]->dst3.data;
        return dst;
    }




    // everything is managed C++ from here.  Anything above is unmanaged.
    public ref class AddWeighted_Basics_CPP : public VB_Parent
    {
        Options_AddWeighted options;
    public:
        int ioIndex;
        double weight;
        AddWeighted_Basics_CPP()
        {
            ioIndex = ioList.size();
            findSliderCPP("Add Weighted %", 49); // showing how to set a slider in managed C++ which doesn't have System.Windows.Forms.
            desc = "Add 2 images with specified weights.";
        }

        void RunAlg(int ioIndex)
        {
            unManagedIO* io = ioList[ioIndex];
            options.RunOpt();

            // algorithm user normally provides src2! 
            Mat src2, srcPlus;
            if (standaloneTest() || src2.empty()) srcPlus = task.depthRGB;
            if (srcPlus.type() != io->src.type())
            {
                if (io->src.type() != CV_8UC3 || srcPlus.type() != CV_8UC3)
                {
                    //if (io->src.type() == CV_32FC1) io->src = Convert32f_To_8UC3(io->src);
                    //if (srcPlus.type() == CV_32FC1) srcPlus = Convert32f_To_8UC3(srcPlus);
                    if (io->src.type() != CV_8UC3) cvtColor(io->src, io->src, COLOR_GRAY2BGR);
                    if (srcPlus.type() != CV_8UC3) cvtColor(srcPlus, srcPlus, COLOR_GRAY2BGR);
                }
            }

            weight = options.addWeighted;
            addWeighted(io->src, weight, srcPlus, 1.0 - weight, 0, io->dst2);
            io->dst3 = task.depthRGB;

            //labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));
        }
    };




    // everything is managed C++ from here.  Anything above is unmanaged.
    //public ref class AddWeighted_Basics1_CPP : public VB_Parent
    //{
    //    Options_AddWeighted options;
    //    AddWeighted_Basics_CPP addw = new AddWeighted_Edges();
    //public:
    //    AddWeighted_Basics1_CPP()
    //    {
    //        desc = "Test calling another C++/CLR algorithm from a C++/CLR algorithm.";
    //    }

    //    void RunAlg(int ioIndex)
    //    {
    //        unManagedIO* io = ioList[ioIndex];
    //        addw.RunAlg(addw)
    //    }
    //};


}

