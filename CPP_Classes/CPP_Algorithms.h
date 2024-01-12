#pragma once
#pragma once
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"
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
    void RunCPP(int maxClassCount, int diff) {
        Rect rect;

        multimap<int, Point, greater<int>> sizeSorted;
        int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
        int count; Point pt;
        float cellSizehreshold = src.total() * 0.0001f; // if the cell is < 1/100 of 1%, then skip it.
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                if (mask.at<unsigned char>(y, x) == 0)
                {
                    pt = Point(x, y);
                    int count = floodFill(src, mask, pt, 255, &rect, diff, diff, floodFlag | (255 << 8));
                    if (count >= cellSizehreshold) sizeSorted.insert(make_pair(count, pt));
                }
            }
        }

        cellRects.clear();
        cellSizes.clear();
        int fill = 1;
        int totalCount = 0;
        float totalImageThreshold = src.total() * 0.95f; // Threshold is 95% of the image.
        for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
        {
            count = floodFill(src, maskCopy, it->second, fill, &rect, diff, diff, floodFlag | (fill << 8));
            if (count >= 1)
            {
                if (rect.width >= src.cols - 2 || rect.height >= src.rows - 2) continue;
                cellRects.push_back(rect);
                cellSizes.push_back(count);
                floodPoints.push_back(it->second);
                totalCount += count;

                if (count > totalImageThreshold || fill >= maxClassCount)
                    break; // just taking up to the top X largest objects found.
                fill++;
            }
        }
    }
};

extern "C" __declspec(dllexport) FloodCell * FloodCell_Open() { return new FloodCell(); }
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
    int maxClassCount, int diff)
{
    cPtr->src = Mat(rows, cols, type, dataPtr);
    cPtr->mask = Mat::zeros(rows + 2, cols + 2, CV_8U);
    Rect r = Rect(1, 1, cols, rows);
    Mat mask;
    if (maskPtr != 0)
    {
        mask = Mat(rows, cols, type, maskPtr);
        mask.copyTo(cPtr->mask(r));
    }
    cPtr->maskCopy = cPtr->mask.clone();
    cPtr->RunCPP(maxClassCount, diff);

    cPtr->maskCopy(r).copyTo(cPtr->result);
    return (int*)cPtr->result.data;
}






class EdgeDraw_Basics
{
private:
public:
	Mat src, dst;
	vector<Vec4f> lines;
	Ptr<EdgeDrawing> ed;
	EdgeDraw_Basics()
	{
		ed = createEdgeDrawing();
		ed->params.EdgeDetectionOperator = EdgeDrawing::SOBEL;
		ed->params.GradientThresholdValue = 38;
		ed->params.AnchorThresholdValue = 8;
	}
	void RunCPP(int lineWidth) {
		ed->detectEdges(src);

		ed->detectLines(lines);

		dst.setTo(0);
		for (size_t i = 0; i < lines.size(); i++)
		{
			Point2f p1 = Point2f(lines[i].val[0], lines[i].val[1]);
			Point2f p2 = Point2f(lines[i].val[2], lines[i].val[3]);
			line(dst, p1, p2, 255, lineWidth);
		}
	}
};

extern "C" __declspec(dllexport)
EdgeDraw_Basics * EdgeDraw_Basics_Open() {
	EdgeDraw_Basics* cPtr = new EdgeDraw_Basics();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Basics_Close(EdgeDraw_Basics * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Basics_RunCPP(EdgeDraw_Basics * cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
	if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
	cPtr->RunCPP(lineWidth);
	return (int*)cPtr->dst.data;
}








class EdgeDraw
{
private:
public:
	Mat src, dst;
	Ptr<EdgeDraw_Basics> eDraw;
	vector<Vec4f> lines;
	EdgeDraw()
	{
		eDraw = new EdgeDraw_Basics();
	}
	void RunCPP(int lineWidth) {
		eDraw->ed->detectEdges(src);
		eDraw->ed->detectLines(lines);
		vector< vector<Point> > segments = eDraw->ed->getSegments();

		dst.setTo(0);
		for (size_t i = 0; i < segments.size(); i++)
		{
			const Point* pts = &segments[i][0];
			int n = (int)segments[i].size();
			float distance = sqrt((pts[0].x - pts[n - 1].x) * (pts[0].x - pts[n - 1].x) + (pts[0].y - pts[n - 1].y) * (pts[0].y - pts[n - 1].y));
			bool drawClosed = distance < 10;
			polylines(dst, &pts, &n, 1, drawClosed, Scalar(255, 255, 255), lineWidth, LINE_AA);
		}
	}
};

extern "C" __declspec(dllexport)
EdgeDraw * EdgeDraw_Edges_Open() {
	EdgeDraw* cPtr = new EdgeDraw();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Edges_Close(EdgeDraw * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_RunCPP(EdgeDraw * cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
	if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
	cPtr->RunCPP(lineWidth);
	return (int*)cPtr->dst.data;
}









class EdgeDraw_Lines
{
private:
public:
	Mat src, dst;
	Ptr<EdgeDraw_Basics> eDraw;
	vector<Vec4f> lines;
	EdgeDraw_Lines()
	{
		eDraw = new EdgeDraw_Basics();
	}
	void RunCPP(int lineWidth) {
		eDraw->ed->detectEdges(src);
		eDraw->ed->detectLines(lines);
	}
};

extern "C" __declspec(dllexport)
EdgeDraw_Lines * EdgeDraw_Lines_Open() {
	EdgeDraw_Lines* cPtr = new EdgeDraw_Lines();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Lines_Close(EdgeDraw_Lines * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int EdgeDraw_Lines_Count(EdgeDraw_Lines * cPtr)
{
	return int(cPtr->lines.size());
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Lines_RunCPP(EdgeDraw_Lines * cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
	if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
	cPtr->RunCPP(lineWidth);
	return (int*)&cPtr->lines[0];
}





class Agast
{
private:
public:
	Mat src, dst;
	std::vector<KeyPoint> keypoints;
	Agast() {}
	void Run() {
		keypoints.clear();
		static Ptr<AgastFeatureDetector> agastFD = AgastFeatureDetector::create(10,
			true, AgastFeatureDetector::OAST_9_16);
		agastFD->detect(src, keypoints);
		dst = Mat(int(keypoints.size()), 7, CV_32F, keypoints.data());
	}
};

extern "C" __declspec(dllexport)
Agast * Agast_Open()
{
	Agast* cPtr = new Agast();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Agast_Close(Agast * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Agast_Run(Agast * cPtr, int* bgrPtr, int rows, int cols, int* count)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, bgrPtr);
	cPtr->Run();
	count[0] = int(cPtr->keypoints.size());
	if (count[0] == 0) return 0;
	return (int*)cPtr->dst.data;
}