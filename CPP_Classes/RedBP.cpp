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




class RedBP
{
private:
public:
    Mat src, mask;
    vector<Point>floodPoints;
    vector<int>cellSizes;

    RedBP() {}
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

extern "C" __declspec(dllexport) RedBP * RedBP_Open() { RedBP* cPtr = new RedBP(); return cPtr; }
extern "C" __declspec(dllexport) int* RedBP_Points(RedBP * cPtr) { return (int*)&cPtr->floodPoints[0]; }
extern "C" __declspec(dllexport) int* RedBP_Sizes(RedBP * cPtr) { return (int*)&cPtr->cellSizes[0]; }
extern "C" __declspec(dllexport) int* RedBP_Close(RedBP * cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int RedBP_Run(RedBP * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);

    cPtr->mask = Mat(rows + 2, cols + 2, CV_8U);
    cPtr->mask.setTo(0);

    cPtr->RunCPP();
    return (int)cPtr->floodPoints.size();
}








class RedBP_FindCells
{
private:
public:
    vector <int> cellList;
    RedBP_FindCells() {}
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
RedBP_FindCells * RedBP_FindCells_Open() {
    RedBP_FindCells* cPtr = new RedBP_FindCells();
    return cPtr;
}
extern "C" __declspec(dllexport)
void RedBP_FindCells_Close(RedBP_FindCells * cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport) int RedBP_FindCells_TotalCount(RedBP_FindCells * cPtr) { return int(cPtr->cellList.size()); }
extern "C" __declspec(dllexport)
int* RedBP_FindCells_RunCPP(RedBP_FindCells * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->RunCPP(Mat(rows, cols, CV_8UC1, dataPtr));
    return (int*)&cPtr->cellList[0];
}
