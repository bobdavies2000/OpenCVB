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

public class unmanagedData
{
public:
    Mat src, color, leftView, rightView, depthRGB, pointCloud;
    Mat pcSplit[3];
    int rows, cols; // working resolution for all Mat's
    bool optionsChanged;
    unmanagedData()
    {
    }
    void update()
    {
        rows = vbc::tInfo->rows;
        cols = vbc::tInfo->cols;
        optionsChanged = vbc::tInfo->optionsChanged;
    }

};

unmanagedData task;

public struct unManagedIO
{
    Mat src, src2, dst0, dst1, dst2, dst3;
    unManagedIO()
    {
        src = Mat(task.rows, task.cols, CV_8UC3);
        dst0 = Mat(task.rows, task.cols, CV_8UC3);
        dst1 = Mat(task.rows, task.cols, CV_8UC3);
        dst2 = Mat(task.rows, task.cols, CV_8UC3);
        dst3 = Mat(task.rows, task.cols, CV_8UC3);

        dst0.setTo(0);
        dst1.setTo(0);
        dst2.setTo(0);
        dst3.setTo(0);
    }
};

vector<unManagedIO*> ioList({});


extern "C" __declspec(dllexport)
int ManagedCPP_Resume(int ioIndex, int* colorPtr, int* leftPtr, int* rightPtr, int* depthRGBPtr, int* cloudPtr)
{
    task.update();
    task.color = Mat(task.rows, task.cols, CV_8UC3, colorPtr).clone();
    task.leftView = Mat(task.rows, task.cols, CV_8UC3, leftPtr).clone();
    task.rightView = Mat(task.rows, task.cols, CV_8UC3, rightPtr).clone();
    task.depthRGB = Mat(task.rows, task.cols, CV_8UC3, depthRGBPtr).clone();
    task.pointCloud = Mat(task.rows, task.cols, CV_32FC3, cloudPtr).clone();
    split(task.pointCloud, task.pcSplit);


    ioList[ioIndex]->src = task.color.clone();
    return (int)ioList.size() - 1;
}


unsigned char** dst = new unsigned char* [4];
unsigned int dstFormats[4];
extern "C" __declspec(dllexport)
unsigned char** ManagedCPP_Pause(int ioIndex)
{
    dst[0] = ioList[ioIndex]->dst0.data;
    dst[1] = ioList[ioIndex]->dst1.data;
    dst[2] = ioList[ioIndex]->dst2.data;
    dst[3] = ioList[ioIndex]->dst3.data;
    return dst;
}
extern "C" __declspec(dllexport)
unsigned int* ManagedCPP_Formats(int ioIndex)
{
    dstFormats[0] = ioList[ioIndex]->dst0.type();
    dstFormats[1] = ioList[ioIndex]->dst1.type();
    dstFormats[2] = ioList[ioIndex]->dst2.type();
    dstFormats[3] = ioList[ioIndex]->dst3.type();
    return dstFormats;
}




// above is unmanaged, below is managed.





namespace CPP_Managed {
    public ref class CPP_IntializeManaged 
    {
    public:
        CPP_IntializeManaged()
        {
            task.rows = vbc::tInfo->rows;
            task.cols = vbc::tInfo->cols;
        }
    };

     
     

    public ref class AddWeighted_Basics_CPP : public VB_Parent
    {
        Options_AddWeighted^ options = gcnew Options_AddWeighted();
    public:
        size_t ioIndex;
        unManagedIO* io;
        double weight;
       // cv::Mat src2; // provided by the callee...
        AddWeighted_Basics_CPP()
        {
            unManagedIO* ioNew = new unManagedIO();
            ioIndex = ioList.size();
            ioList.push_back(ioNew);
            io = ioNew;
            findSliderCPP("Add Weighted %", 49); // showing how to set a slider in managed C++ which doesn't have System.Windows.Forms.
            desc = "Add 2 images with specified weights.";
        }

        void RunAlg()
        {
            io = ioList[ioIndex];
            options->RunOpt();

            if (standaloneTest() || io->src2.empty()) io->src2 = task.depthRGB;
            if (io->src.type() != io->src2.type())
            {
                if (io->src.type() != CV_8UC3 || io->src2.type() != CV_8UC3)
                {
                    // if (io->src.type() == CV_32FC1) io->src = Convert32f_To_8UC3(io->src);
                    //if (srcPlus.type() == CV_32FC1) srcPlus = Convert32f_To_8UC3(srcPlus);
                    if (io->src.type() != CV_8UC3) cvtColor(io->src, io->src, COLOR_GRAY2BGR);
                    if (io->src2.type() != CV_8UC3) cvtColor(io->src2, io->src2, COLOR_GRAY2BGR);
                }
            }

            weight = options->addWeighted;
            addWeighted(io->src, weight, io->src2, 1.0 - weight, 0, io->dst2);

            //labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));
        }
    };

     


    public ref class AddWeighted_LeftRight_CPP : public VB_Parent
    {
        AddWeighted_Basics_CPP^ addw = gcnew AddWeighted_Basics_CPP();
    public:
        size_t ioIndex;
        unManagedIO* io;
        AddWeighted_LeftRight_CPP()
        {
            unManagedIO* ioNew = new unManagedIO();
            ioIndex = ioList.size();
            ioList.push_back(ioNew);
            io = ioNew;

            desc = "Use AddWeighted to add the left and right images";
        }

        void RunAlg()
        {
            io = ioList[ioIndex];
            addw->io->src = task.rightView;
            addw->io->src2 = task.leftView;
            addw->RunAlg();
            io->dst2 = addw->io->dst2;
            io->dst3 = addw->io->dst3;
        }
    };




    public ref class AddWeighted_DepthAccumulate_CPP : public VB_Parent
    {
    private:
        Options_AddWeighted^ options = gcnew Options_AddWeighted();
    public:
        size_t ioIndex;
        unManagedIO* io;
        AddWeighted_DepthAccumulate_CPP()
        {
            unManagedIO* ioNew = new unManagedIO();
            ioIndex = ioList.size();
            ioList.push_back(ioNew);
            io = ioNew;
        }
        void RunAlg()
        {
            io = ioList[ioIndex];
            options->RunOpt();
            if (task.optionsChanged)
            {
                io->dst2 = task.pcSplit[2] * 1000;
            }
            accumulateWeighted(task.pcSplit[2] * 1000, io->dst2, options->accumWeighted, Mat());
        }
    };




}

