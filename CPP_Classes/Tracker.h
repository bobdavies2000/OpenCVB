#pragma once
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/tracking.hpp>
#include <opencv2/core/ocl.hpp>



using namespace std;
using namespace  cv;
class Tracker_Basics
{
private:
public:
    Ptr<Tracker> tracker;
    bool bboxInitialized = false;
    Mat src;
    Rect tRect; // tracker output box.
    Tracker_Basics(int trackType)
    {
        // switch is based on the older tracking alternatives.  Some are disabled in the user interface for now.
        switch (trackType)
        {
        case 1: // MIL
            tracker = cv::TrackerMIL::create();
            break;
        case 2: // KCF
            tracker = cv::TrackerKCF::create();
            break;
        case 5: // GOTURN
            tracker = cv::TrackerGOTURN::create();
            break;
        case 7: // CSRT
            tracker = cv::TrackerCSRT::create();
            break;
        default: // MIL
            tracker = cv::TrackerMIL::create();
            break;
        }
    }
    void Run(Rect bbox) {
        if (bboxInitialized == false)
        {
            bboxInitialized = true;
            tracker->init(src, bbox);
        }
        else {
            bool ok = tracker->update(src, bbox);
            if (ok)
            {
                rectangle(src, bbox, Scalar(255, 255, 255), 1, 1);
            } else {
                putText(src, "Tracking failure detected", Point(100, 80), FONT_HERSHEY_SIMPLEX, 0.75, Scalar(0, 0, 255), 2);
                bboxInitialized = false;
            }

            tRect = Rect(bbox.x, bbox.y, bbox.width, bbox.height);
        }
    }
};

extern "C" __declspec(dllexport)
Tracker_Basics *Tracker_Basics_Open(int trackType) {
    Tracker_Basics *cPtr = new Tracker_Basics(trackType);
    return cPtr;
}

extern "C" __declspec(dllexport)
int *Tracker_Basics_Close(Tracker_Basics * cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int *Tracker_Basics_Run(Tracker_Basics *cPtr, int *bgrPtr, int rows, int cols, int x, int y, int w, int h)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, bgrPtr);
    Rect bbox(x, y, w, h);
	cPtr->Run(bbox);
	return (int*)&cPtr->tRect; 
}