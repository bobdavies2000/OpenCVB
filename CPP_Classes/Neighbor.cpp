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
    Neighbors() {}
    void RunCPP(int cellIndex)
    {
        vector<Point> nPoints; vector<string> cellData;
        neighbors.clear();

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
int* Neighbor_List(Neighbors * cPtr)
{
    return (int*)&cPtr->neighbors[0];
}

extern "C" __declspec(dllexport)
int Neighbor_Count(Neighbors * cPtr)
{
    return (int)cPtr->neighbors.size();
}

extern "C" __declspec(dllexport)
int Neighbor_RunCPP(Neighbors * cPtr, int* dataPtr, int rows, int cols, int cellCount)
{
    cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
    cPtr->RunCPP(cellCount);
    return (int)cPtr->neighbors.size();
}
