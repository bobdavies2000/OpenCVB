#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/core/utility.hpp>
#include <map>
using namespace std;
using namespace  cv;
class MSER_Basics
{
private:
public:
    Mat src, dst;
    cv::Ptr<cv::MSER> mser;
    vector<Rect> containers;
    vector<Point> floodPoints;
    vector<int> maskCounts;
    vector<vector<Point>> regions;
    vector<Rect> boxes;
    MSER_Basics(int delta, int minArea, int maxArea, float maxVariation, float minDiversity, int maxEvolution, float areaThreshold,
                float minMargin, int edgeBlurSize, int pass2Setting)
    {
        mser = mser->create(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin, edgeBlurSize);
        mser->setPass2Only(pass2Setting);
    }
    void RunCPP() {
        mser->detectRegions(src, regions, boxes);

        multimap<int, int, greater<int>> sizeSorted;
        for (auto i = 0; i < regions.size(); i++)
        {
            sizeSorted.insert(make_pair(regions[i].size(), i));
        }

        int index = 0;
        maskCounts.clear();
        containers.clear();
        floodPoints.clear();
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            Rect box = boxes[it->second];
            Point center = Point(box.x + box.width / 2, box.y + box.height / 2);
            int val = dst.at<uchar>(center.y, center.x);
            if (val == 255)
            {
                floodPoints.push_back(regions[it->second][0]);
                maskCounts.push_back(regions[it->second].size());
                for (Point pt : regions[it->second])
                {
                    dst.at<uchar>(pt.y, pt.x) = index;
                }
                index++;
                containers.push_back(box);
            }
        }
    }
};
extern "C" __declspec(dllexport)
MSER_Basics *MSER_Open(int delta, int minArea, int maxArea, float maxVariation, float minDiversity, int maxEvolution, float areaThreshold,
                       float minMargin, int edgeBlurSize, int pass2Setting) 
{
    MSER_Basics*cPtr = new MSER_Basics(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin, 
                                       edgeBlurSize, pass2Setting);
    return cPtr;
}
extern "C" __declspec(dllexport)
void MSER_Close(MSER_Basics *cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* MSER_Rects(MSER_Basics * cPtr)
{
    return (int*)&cPtr->containers[0];
}
extern "C" __declspec(dllexport)
int* MSER_FloodPoints(MSER_Basics * cPtr)
{
    return (int*)&cPtr->floodPoints[0];
}
extern "C" __declspec(dllexport)
int* MSER_MaskCounts(MSER_Basics * cPtr)
{
    return (int*)&cPtr->maskCounts[0];
}
extern "C" __declspec(dllexport)
int MSER_Count(MSER_Basics * cPtr)
{
    return (int)cPtr->containers.size();
}
extern "C" __declspec(dllexport)
int *MSER_RunCPP(MSER_Basics *cPtr, int *dataPtr, int rows, int cols, int channels)
{
		cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, dataPtr);
        cPtr->dst = Mat(rows, cols, CV_8UC1);
        cPtr->dst.setTo(255);
		cPtr->RunCPP();
		return (int *) cPtr->dst.data; 
}
