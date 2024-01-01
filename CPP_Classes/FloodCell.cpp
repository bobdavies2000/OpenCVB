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

class FloodCell
{
private:
public:
    Mat src, mask, maskCopy, result;
    vector<Rect>cellRects;
    vector<int> cellSizes;
    vector<Point> floodPoints;

    FloodCell() {}
    void RunCPP(float sizeThreshold, int maxClassCount, int diff) {
        Rect rect;

        multimap<int, Point, greater<int>> sizeSorted;
        int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
        int count; Point pt;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                if (mask.at<unsigned char>(y, x) == 0)
                {
                    pt = Point(x, y);
                    int count = floodFill(src, mask, pt, 255, &rect, diff, diff, floodFlag | (255 << 8));
                    sizeSorted.insert(make_pair(count, pt));
                }
            }
        }

        cellRects.clear();
        cellSizes.clear();
        int fill = 1;
        int totalCount = 0;
        float srcTotal = src.total();
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            count = floodFill(src, maskCopy, it->second, fill, &rect, diff, diff, floodFlag | (fill << 8));
            if (count >= 1)
            {
                if (rect.width == src.cols && rect.height == src.rows) continue;
                cellRects.push_back(rect);
                cellSizes.push_back(count);
                floodPoints.push_back(it->second);
                totalCount += count;

                if (totalCount / srcTotal > sizeThreshold || fill >= maxClassCount) 
                    break; // just taking up to the top X largest objects found.
                fill++;
            }
        }
    }
};

extern "C" __declspec(dllexport) FloodCell * FloodCell_Open() { FloodCell* cPtr = new FloodCell(); return cPtr; }
extern "C" __declspec(dllexport) int FloodCell_Count(FloodCell * cPtr)
{
    return (int)cPtr->cellRects.size();
}

extern "C" __declspec(dllexport) int* FloodCell_Rects(FloodCell * cPtr)
{
    return (int*)&cPtr->cellRects[0];
}

extern "C" __declspec(dllexport) int* FloodCell_FloodPoints(FloodCell * cPtr)
{
    return (int*)&cPtr->floodPoints[0];
}

extern "C" __declspec(dllexport) int* FloodCell_Sizes(FloodCell * cPtr)
{
    return (int*)&cPtr->cellSizes[0];
}

extern "C" __declspec(dllexport) int* FloodCell_Close(FloodCell * cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int* 



FloodCell_Run(FloodCell * cPtr, int* dataPtr, unsigned char* maskPtr, int rows, int cols, int type, 
              float sizeThreshold, int maxClassCount, int diff)
{
    cPtr->src = Mat(rows, cols, type, dataPtr);
    cPtr->mask = Mat(rows + 2, cols + 2, CV_8U);
    cPtr->mask.setTo(0);
    Rect r = Rect(1, 1, cols, rows);
    Mat mask;
    if (maskPtr != 0)
    {
        mask = Mat(rows, cols, type, maskPtr);
        mask.copyTo(cPtr->mask(r));
    }
    cPtr->maskCopy = cPtr->mask.clone();
    cPtr->RunCPP(sizeThreshold, maxClassCount, diff);
    if (maskPtr != 0) cPtr->maskCopy(r).setTo(0, mask);

    cPtr->maskCopy(r).copyTo(cPtr->result);
    return (int*)cPtr->result.data;
}