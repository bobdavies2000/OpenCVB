#pragma once
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <map>

using namespace std;
using namespace  cv;




class RedCloud
{
private:
public:
    Mat src, mask;
    vector<Point>floodPoints;
    vector<int>cellSizes;

    RedCloud() {}
    void RunCPP() {
        vector<Point>points;
        Rect rect;

        multimap<int, int, greater<int>> sizeSorted;
        int floodFlag = 4 | FLOODFILL_FIXED_RANGE;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                Point pt = Point(x, y);
                uchar valCurr = src.at<unsigned char>(y, x);
                if (mask.at<unsigned char>(y, x) == 0)
                {
                    int count = floodFill(src, mask, pt, valCurr, &rect, 0, 0, floodFlag | (255 << 8));
                    if (count > 1)
                    {
                        points.push_back(pt);
                        sizeSorted.insert(make_pair(count, sizeSorted.size()));
                    }
                }
            }
        }

        floodPoints.clear();
        cellSizes.clear();
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            int index = it->second;
            floodPoints.push_back(points[index]);
            cellSizes.push_back(it->first);
        }
    }
};

extern "C" __declspec(dllexport) RedCloud * RedCloud_Open() { RedCloud* cPtr = new RedCloud(); return cPtr; }
extern "C" __declspec(dllexport) int* RedCloud_Points(RedCloud * cPtr) { return (int*)&cPtr->floodPoints[0]; }
extern "C" __declspec(dllexport) int* RedCloud_Sizes(RedCloud * cPtr) { return (int*)&cPtr->cellSizes[0]; }
extern "C" __declspec(dllexport) int* RedCloud_Close(RedCloud * cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int RedCloud_Run(RedCloud * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);

    cPtr->mask = Mat(rows + 2, cols + 2, CV_8U);
    cPtr->mask.setTo(0);

    cPtr->RunCPP();
    return (int)cPtr->floodPoints.size();
}








class RedCloud_FindCells
{
private:
public:
    vector <int> cellList;
    RedCloud_FindCells() {}
    void RunCPP(Mat src) {
        cellList.clear();
        for (int y = 0; y < src.rows; y++)
            for (int x = 0; x < src.cols; x++)
            {
                auto val = src.at<unsigned char>(y, x);
                if (val > 0)
                {
                    if (count(cellList.begin(), cellList.end(), val) == 0)
                        cellList.push_back(val);
                }
            }
    }
};
extern "C" __declspec(dllexport)
RedCloud_FindCells * RedCloud_FindCells_Open() {
    RedCloud_FindCells* cPtr = new RedCloud_FindCells();
    return cPtr;
}
extern "C" __declspec(dllexport)
void RedCloud_FindCells_Close(RedCloud_FindCells * cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport) int RedCloud_FindCells_TotalCount(RedCloud_FindCells * cPtr) { return int(cPtr->cellList.size()); }
extern "C" __declspec(dllexport)
int* RedCloud_FindCells_RunCPP(RedCloud_FindCells * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->RunCPP(Mat(rows, cols, CV_8UC1, dataPtr));
    return (int*)&cPtr->cellList[0];
}