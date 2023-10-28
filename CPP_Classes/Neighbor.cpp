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
Neighbors * Neighbor_Open() {
    Neighbors* cPtr = new Neighbors();
    return cPtr;
}
extern "C" __declspec(dllexport)
void Neighbor_Close(Neighbors * cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* Neighbor_CellData(Neighbors * cPtr)
{
    return (int*)&cPtr->cellData[0];
}

extern "C" __declspec(dllexport)
int* Neighbor_Points(Neighbors * cPtr)
{
    return (int*)&cPtr->nPoints[0];
}

extern "C" __declspec(dllexport)
int Neighbor_RunCPP(Neighbors * cPtr, int* dataPtr, int rows, int cols)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP();
    return (int)cPtr->nPoints.size();
}
