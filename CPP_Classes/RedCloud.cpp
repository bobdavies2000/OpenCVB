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






class RedCloud_Neighbors
{
private:
    void testCellMap(Point pt)
    {
        if (pt.y < 0) pt.y = 0;
        if (pt.y >= src.rows) pt.y = src.rows - 1;
        if (pt.x < 0) pt.x = 0;
        if (pt.x >= src.cols) pt.x = src.cols - 1;
        int id = src.at<uchar>(pt.y, pt.x);
        if (id == 0) return;
        if (count(neighbors.begin(), neighbors.end(), id) == 0)
            neighbors.push_back(id);
    }
public:
    Mat src, contour;
    vector<int> neighbors;
    RedCloud_Neighbors(){}
    void RunCPP(int cellIndex)
    {
        neighbors.clear();
        neighbors.push_back(cellIndex);
        for (int i = 0; i < contour.rows; i++)
        {
            Point pt = contour.at<Point>(i, 0);
            pt.x += 1;
            pt.y += 1;

            testCellMap(Point(pt.x - 1, pt.y - 1));
            testCellMap(Point(pt.x, pt.y - 1));
            testCellMap(Point(pt.x + 1, pt.y - 1));
            testCellMap(Point(pt.x - 1, pt.y));
            testCellMap(Point(pt.x + 1, pt.y));
            testCellMap(Point(pt.x - 1, pt.y + 1));
            testCellMap(Point(pt.x, pt.y + 1));
            testCellMap(Point(pt.x + 1, pt.y + 1));
        }
    }
};

extern "C" __declspec(dllexport)
RedCloud_Neighbors *RedCloud_Neighbors_Open() {
    RedCloud_Neighbors *cPtr = new RedCloud_Neighbors();
    return cPtr;
}
extern "C" __declspec(dllexport)
void RedCloud_Neighbors_Close(RedCloud_Neighbors * cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* RedCloud_Neighbors_List(RedCloud_Neighbors* cPtr)
{
    return (int*)&cPtr->neighbors[0];
}

extern "C" __declspec(dllexport)
int RedCloud_Neighbors_Count(RedCloud_Neighbors* cPtr)
{
    return (int)cPtr->neighbors.size();
}

extern "C" __declspec(dllexport)
int RedCloud_Neighbors_RunCPP(RedCloud_Neighbors *cPtr, int *dataPtr, int rows, int cols,
                              int* contour, int contourCount, int cellIndex)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->contour = Mat(contourCount, 1, CV_32SC2, contour);
    cPtr->RunCPP(cellIndex);
	return (int)cPtr->neighbors.size();
}





class RedCloud_Corners
{
private:
public:
    Mat src;
    RedCloud_Corners(){}
    vector<int> idList;
    vector<Point> ptList;
    void testCellMap(Point pt)
    {
        int id = src.at<uchar>(pt.y, pt.x);
        if (id == 0) return;
        if (count(idList.begin(), idList.end(), id) == 0)
        {
            idList.push_back(id);
        }
    }
    void clearCellMap(Point pt)
    {
        src.at<uchar>(pt.y, pt.x) = 0;
    }
    void RunCPP() 
    {
        ptList.clear();
        for (int y = 1; y < src.rows - 1; y++)
            for (int x = 1; x < src.cols - 1; x++)
            {
                Point pt = Point(x, y);
                idList.clear();

                testCellMap(Point(pt.x - 1, pt.y - 1));
                testCellMap(Point(pt.x, pt.y - 1));
                testCellMap(Point(pt.x + 1, pt.y - 1));
                testCellMap(Point(pt.x - 1, pt.y));
                testCellMap(Point(pt.x + 1, pt.y));
                testCellMap(Point(pt.x - 1, pt.y + 1));
                testCellMap(Point(pt.x, pt.y + 1));
                testCellMap(Point(pt.x + 1, pt.y + 1));
                if (idList.size() >= 3)
                {
                    ptList.push_back(pt);
                    circle(src, pt, 10, 0, FILLED, LINE_8);
                }
            }
    }
};
extern "C" __declspec(dllexport)
RedCloud_Corners *RedCloud_Corners_Open() {
    RedCloud_Corners *cPtr = new RedCloud_Corners();
    return cPtr;
}
extern "C" __declspec(dllexport)
void RedCloud_Corners_Close(RedCloud_Corners * cPtr)
{
    delete cPtr;
}
extern "C" __declspec(dllexport)
int* RedCloud_Corners_List(RedCloud_Corners * cPtr)
{
    return (int *)&cPtr->ptList[0];
}
extern "C" __declspec(dllexport)
int RedCloud_Corners_RunCPP(RedCloud_Corners *cPtr, int *dataPtr, int rows, int cols)
{
		cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
		cPtr->RunCPP();
		return (int) cPtr->ptList.size();
}
