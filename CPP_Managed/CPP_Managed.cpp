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
using namespace System;
using namespace System::Drawing;
using namespace System::Windows::Forms;

public class unManagedData
{
public:
    Mat src, color, leftView, rightView, depthRGB, pointCloud;
    Mat pcSplit[3];
    int rows, cols; // working resolution for all Mat's
    bool optionsChanged;
    unManagedData()
    {
    }
    void update()
    {
        rows = vbc::tInfo->rows;
        cols = vbc::tInfo->cols;
        optionsChanged = vbc::tInfo->optionsChanged;
    }

};

unManagedData task;

public struct unManagedIO
{
    Mat src, src2, dst0, dst1, dst2, dst3;
    unManagedIO()
    {
        src = Mat(task.rows, task.cols, CV_8UC3); dst0 = Mat(task.rows, task.cols, CV_8UC3); dst1 = Mat(task.rows, task.cols, CV_8UC3);
        dst2 = Mat(task.rows, task.cols, CV_8UC3); dst3 = Mat(task.rows, task.cols, CV_8UC3);
        dst0.setTo(0); dst1.setTo(0); dst2.setTo(0); dst3.setTo(0); 
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
            FindSlider("Add Weighted %")->Value = 49; // showing how to set a slider in managed C++ which doesn't have System.Windows.Forms.
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





    public ref class Mat_ManualCopyTest_CPP : public VB_Parent
    {
    private:
        Options_BrightnessContrast^ options = gcnew Options_BrightnessContrast();
    public:
        size_t ioIndex;
        unManagedIO* io;
        Mat_ManualCopyTest_CPP()
        {
            unManagedIO* ioNew = new unManagedIO();
            ioIndex = ioList.size();
            ioList.push_back(ioNew);
            io = ioNew;
            desc = "Testing access to the Native C++ buffer for src - it is not managed and should be accessible.";
        }
        void RunAlg()
        {
            io = ioList[ioIndex];
            options->RunOpt();

            int alpha = (int)options->brightness;
            size_t beta = options->contrast;
            size_t stepSize = io->src.cols * io->src.elemSize();
            uchar* srcData = io->src.data;
            uchar* dst2Data = io->dst2.data;
            for (int y = 0; y < io->src.rows; y++)
            {
                for (int x = 0; x < stepSize; x++)
                {
                    uchar b = (uchar)abs(srcData[y * stepSize + x] * alpha - beta);
                    dst2Data[y * stepSize + x] = b;
                }
            }
            memcpy(io->dst3.data, srcData, io->src.rows * stepSize);  // Native buffers are fixed but may not be contiguous.
        }
    };




    public ref class Edge_Canny_CPP : public VB_Parent
    {
    private:
        Options_Canny^ options = gcnew Options_Canny();
    public:
        size_t ioIndex;
        unManagedIO* io;
        Edge_Canny_CPP()
        {
            unManagedIO* ioNew = new unManagedIO();
            ioIndex = ioList.size();
            ioList.push_back(ioNew);
            io = ioNew;
            labels[2] = "Canny using L1 Norm";
            labels[3] = "Canny using L2 Norm";
            desc = "Show canny edge detection with varying thresholds";
        }
        void RunAlg()
        {
            options->RunOpt();

            if (io->src.channels() == 3) {
                cvtColor(io->src, io->src, COLOR_BGR2GRAY);
            }
            if (io->src.type() != CV_8U) {
                io->src.convertTo(io->src, CV_8U);
            }
            Canny(io->src, io->dst2, options->threshold1, options->threshold2, options->aperture, true);
        }
    };





    //public ref class AddWeighted_Edges_CPP : public VB_Parent
    //{
    //private:
    //    Edge_Basics_CPP edges = new Edge_Basics_CPP();
    //    AddWeighted_Basics_CPP addw = AddWeighted_Basics_CPP();
    //public:
    //    size_t ioIndex;
    //    unManagedIO* io;
    //    AddWeighted_Edges_CPP()
    //    {
    //        labels = { "", "", "Edges_BinarizedSobel output", "AddWeighted edges and BGR image" };
    //        unManagedIO* ioNew = new unManagedIO();
    //        ioIndex = ioList.size();
    //        ioList.push_back(ioNew);
    //        io = ioNew;
    //    }
    //    void RunAlg(Mat& io->src)
    //    {
    //        io = ioList[ioIndex];
    //        edges.Run(io->src);
    //        dst2 = edges.dst2;
    //        labels[2] = edges.labels[2];
    //        addw->io->src2 = cvtColor(edges->io->dst2, COLOR_GRAY2BGR);
    //        addw.Run(io->src);
    //        dst3 = addw.dst2;
    //    }
    //};




    //public ref class Edge_Basics_CPP : public VB_Parent
    //{
    //private:
    //    Edge_Canny_CPP canny = new Edge_Canny_CPP();
    //    //Edge_Scharr_CPP scharr = new Edge_Scharr_CPP();
    //    //Edge_BinarizedReduction_CPP binRed = new Edge_BinarizedReduction_CPP();
    //    //Bin4Way_Sobel_CPP binSobel = new Bin4Way_Sobel_CPP();
    //    //Edge_Laplacian_CPP Laplacian = new Edge_Laplacian_CPP();
    //    //Edge_ResizeAdd_CPP resizeAdd = new Edge_ResizeAdd_CPP();
    //    //Edge_Regions_CPP regions = new Edge_Regions_CPP();
    //public:
    //    size_t ioIndex;
    //    unManagedIO* io;
    //    Options_Edge_Basics^ options = gcnew Options_Edge_Basics();
    //    Edge_Basics_CPP()
    //    {
    //        unManagedIO* ioNew = new unManagedIO();
    //        ioIndex = ioList.size();
    //        ioList.push_back(ioNew);
    //        io = ioNew;
    //    }
    //    void RunAlg()
    //    {
    //        io = ioList[ioIndex];
    //        options->RunOpt();
    //        if (options->edgeSelection == "Canny")
    //        {
    //            if (!canny) canny = std::make_unique<Edge_Canny>();
    //            canny->Run(io->src);
    //            dst2 = canny->dst2;
    //        }
    //        //else if (options->edgeSelection == "Scharr")
    //        //{
    //        //    if (!scharr) scharr = std::make_unique<Edge_Scharr>();
    //        //    scharr->Run(io->src);
    //        //    dst2 = scharr->dst3;
    //        //}
    //        //else if (options->edgeSelection == "Binarized Reduction")
    //        //{
    //        //    if (!binRed) binRed = std::make_unique<Edge_BinarizedReduction>();
    //        //    binRed->Run(io->src);
    //        //    dst2 = binRed->dst2;
    //        //}
    //        //else if (options->edgeSelection == "Binarized Sobel")
    //        //{
    //        //    if (!binSobel) binSobel = std::make_unique<Bin4Way_Sobel>();
    //        //    binSobel->Run(io->src);
    //        //    dst2 = binSobel->dst2;
    //        //}
    //        //else if (options->edgeSelection == "Color Gap")
    //        //{
    //        //    if (!colorGap) colorGap = std::make_unique<Edge_ColorGap_CPP_VB>();
    //        //    colorGap->Run(io->src);
    //        //    dst2 = colorGap->dst2;
    //        //}
    //        //else if (options->edgeSelection == "Deriche")
    //        //{
    //        //    if (!deriche) deriche = std::make_unique<Edge_Deriche_CPP_VB>();
    //        //    deriche->Run(io->src);
    //        //    dst2 = deriche->dst2;
    //        //}
    //        //else if (options->edgeSelection == "Laplacian")
    //        //{
    //        //    if (!Laplacian) Laplacian = std::make_unique<Edge_Laplacian>();
    //        //    Laplacian->Run(io->src);
    //        //    dst2 = Laplacian->dst2;
    //        //}
    //        //else if (options->edgeSelection == "Resize And Add")
    //        //{
    //        //    if (!resizeAdd) resizeAdd = std::make_unique<Edge_ResizeAdd>();
    //        //    resizeAdd->Run(io->src);
    //        //    dst2 = resizeAdd->dst2;
    //        //}
    //        //else if (options->edgeSelection == "Depth Region Boundaries")
    //        //{
    //        //    if (!regions) regions = std::make_unique<Edge_Regions>();
    //        //    regions->Run(io->src);
    //        //    dst2 = regions->dst2;
    //        //}
    //        labels[2] = traceName + " - selection = " + options->edgeSelection;
    //    }
    //};



}

