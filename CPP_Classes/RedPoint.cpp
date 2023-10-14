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

class RedPoint
{
private:
public:
    Mat src, mask;
    vector<Point>cellPoints;

    RedPoint() {}
    void RunCPP(int diff) {
        src = src(Rect(1, 1, src.cols - 2, src.rows - 2));
        Rect rect;

        multimap<int, Point, greater<int>> sizeSorted;
        int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                if (mask.at<unsigned char>(y, x) == 0)
                {
                    Point pt = Point(x, y);
                    int count = floodFill(src, mask, pt, 255, &rect, diff, diff, floodFlag | (255 << 8));
                    if (count > 1)
                        sizeSorted.insert(make_pair(count, pt));
                }
            }
        }

        cellPoints.clear();
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            cellPoints.push_back(it->second);
            if (cellPoints.size() >= 255) break;
        }
    }
};

extern "C" __declspec(dllexport) RedPoint * RedPoint_Open() { RedPoint* cPtr = new RedPoint(); return cPtr; }
extern "C" __declspec(dllexport) int* RedPoint_Points(RedPoint * cPtr) { return (int*)&cPtr->cellPoints[0];}
extern "C" __declspec(dllexport) int* RedPoint_Close(RedPoint * cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int RedPoint_Run(RedPoint* cPtr, int* dataPtr, unsigned char* maskPtr,
                                                  int rows, int cols, int diff)
{
    cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
    cPtr->mask = Mat(rows, cols, CV_8U, maskPtr);
    cPtr->RunCPP(diff);
    return cPtr->cellPoints.size();
}


