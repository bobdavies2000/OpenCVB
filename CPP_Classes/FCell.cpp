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

class FCell
{
private:
public:
    Mat src, mask, maskCopy, result;
    vector<Rect>cellRects;
    vector<int> cellSizes;

    FCell() {}
    void RunCPP(int minPixels, int diff) {
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
                    if (count > minPixels)
                        sizeSorted.insert(make_pair(count, pt));
                }
            }
        }

        cellRects.clear();
        cellSizes.clear();
        int fill = 1;
            
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            count = floodFill(src, maskCopy, it->second, fill, &rect, diff, diff, floodFlag | (fill << 8));
            if (count >= 1)
            {
                if (rect.width == src.cols && rect.height == src.rows) continue;
                cellRects.push_back(rect);
                cellSizes.push_back(count);

                if (fill >= 255) break; // just taking the top 255 largest objects found.
                fill++;
            }
        }
    }
};

extern "C" __declspec(dllexport) FCell * FCell_Open() { FCell* cPtr = new FCell(); return cPtr; }
extern "C" __declspec(dllexport) int FCell_Count(FCell * cPtr)
{
    return (int)cPtr->cellRects.size();
}

extern "C" __declspec(dllexport) int* FCell_Rects(FCell * cPtr)
{
    return (int*)&cPtr->cellRects[0];
}

extern "C" __declspec(dllexport) int* FCell_Sizes(FCell * cPtr)
{
    return (int*)&cPtr->cellSizes[0];
}

extern "C" __declspec(dllexport) int* FCell_Close(FCell * cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int* FCell_Run(FCell * cPtr, int* dataPtr, unsigned char* maskPtr,
    int rows, int cols, int type, int minPixels, int diff)
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
    cPtr->RunCPP(minPixels, diff);
    if (maskPtr != 0) cPtr->maskCopy(r).setTo(0, mask);

    cPtr->maskCopy(r).copyTo(cPtr->result);
    return (int*)cPtr->result.data;
}