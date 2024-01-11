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

class Neighbors
{
private:
public:
    Mat src, contour;
    vector<Point> nPoints;
    vector<uchar> cellData;
    void RunCPP()
    {
        nPoints.clear();
        cellData.clear();
        for (int y = 1; y < src.rows - 3; y++)
            for (int x = 1; x < src.cols - 3; x++)
            {
                vector<uchar> nabs;
                vector<uchar> ids = { 0, 0, 0, 0 };
                int index = 0;
                for (int yy = y; yy < y + 2; yy++)
                {
                    for (int xx = x; xx < x + 2; xx++)
                    {
                        uchar val = src.at<uchar>(yy, xx);
                        if (count(nabs.begin(), nabs.end(), val) == 0)
                        {
                            nabs.push_back(val);
                        }
                        ids[index++] = val;
                    }
                }
                if (nabs.size() > 2)
                {
                    nPoints.push_back(Point(x, y));
                    cellData.push_back(ids[0]);
                    cellData.push_back(ids[1]);
                    cellData.push_back(ids[2]);
                    cellData.push_back(ids[3]);
                }
            }
    }
};

extern "C" __declspec(dllexport)
Neighbors * Neighbors_Open() {
    Neighbors* cPtr = new Neighbors();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Neighbors_Close(Neighbors * cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* Neighbors_CellData(Neighbors * cPtr)
{
    return (int*)&cPtr->cellData[0];
}

extern "C" __declspec(dllexport)
int* Neighbors_Points(Neighbors * cPtr)
{
    return (int*)&cPtr->nPoints[0];
}

extern "C" __declspec(dllexport)
int Neighbors_RunCPP(Neighbors * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int)cPtr->nPoints.size();
}








class Neighbor2
{
private:
public:
    Mat src, contour;
    vector<Point> nPoints;
    void RunCPP()
    {
        nPoints.clear();
        for (int y = 1; y < src.rows - 2; y++)
            for (int x = 1; x < src.cols - 2; x++)
            {
                Point pt = Point(src.at<uchar>(y, x), src.at<uchar>(y, x - 1));
                if (pt.x == pt.y) continue;
                if (pt.x == 0 || pt.y == 0) continue;
                if (count(nPoints.begin(), nPoints.end(), pt) == 0)
                {
                    pt = Point(src.at<uchar>(y, x - 1), src.at<uchar>(y, x));
                    if (count(nPoints.begin(), nPoints.end(), pt) == 0) nPoints.push_back(pt);
                }
            }
    }
};

extern "C" __declspec(dllexport)
Neighbor2 * Neighbor2_Open() {
    Neighbor2* cPtr = new Neighbor2();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Neighbor2_Close(Neighbor2 * cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* Neighbor2_Points(Neighbor2 * cPtr)
{
    return (int*)&cPtr->nPoints[0];
}

extern "C" __declspec(dllexport)
int Neighbor2_RunCPP(Neighbor2 * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int)cPtr->nPoints.size();
}






class Neighbor_Map
{
private:
public:
    Mat src;
    vector <Point> nabList;

    Neighbor_Map() {}
    void checkPoint(Point pt)
    {
        if (pt.x != pt.y)
        {
            if (pt.x > pt.y) pt = Point(pt.y, pt.x);
            std::vector<Point>::iterator it = std::find(nabList.begin(), nabList.end(), pt);
            if (it == nabList.end()) nabList.push_back(pt);
        }
    }
    void RunCPP() {
        nabList.clear();
        for (int y = 1; y < src.rows; y++)
            for (int x = 1; x < src.cols; x++)
            {
                uchar val = src.at<uchar>(y, x);
                checkPoint(Point(src.at<uchar>(y, x - 1), val));
                checkPoint(Point(src.at<uchar>(y - 1, x), val));
            }
    }
};
extern "C" __declspec(dllexport) Neighbor_Map * Neighbor_Map_Open() { Neighbor_Map* cPtr = new Neighbor_Map(); return cPtr; }
extern "C" __declspec(dllexport) void Neighbor_Map_Close(Neighbor_Map * cPtr){delete cPtr;}
extern "C" __declspec(dllexport) int* Neighbor_NabList(Neighbor_Map * cPtr) { return (int*)&cPtr->nabList[0]; }
extern "C" __declspec(dllexport)
int Neighbor_Map_RunCPP(Neighbor_Map * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int)cPtr->nabList.size();
}
