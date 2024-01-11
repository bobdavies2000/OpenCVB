#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"
#include "OpenCVB_Extern.h"

using namespace std;
using namespace  cv;
using namespace ximgproc;

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

VB_EXTERN
EdgeDraw_Basics * EdgeDraw_Basics_Open() {
	EdgeDraw_Basics* cPtr = new EdgeDraw_Basics();
	return cPtr;
}

VB_EXTERN
int* EdgeDraw_Basics_Close(EdgeDraw_Basics * cPtr)
{
	delete cPtr;
	return (int*)0;
}

VB_EXTERN
int* EdgeDraw_Basics_RunCPP(EdgeDraw_Basics * cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
	if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
	cPtr->RunCPP(lineWidth);
	return (int*)cPtr->dst.data;
}








class EdgeDraw_Segments
{
private:
public:
	Mat src, dst;
	Ptr<EdgeDraw_Basics> eDraw;
	vector<Vec4f> lines;
	EdgeDraw_Segments()
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

VB_EXTERN
EdgeDraw_Segments * EdgeDraw_Edges_Open() {
	EdgeDraw_Segments* cPtr = new EdgeDraw_Segments();
	return cPtr;
}

VB_EXTERN
int* EdgeDraw_Edges_Close(EdgeDraw_Segments * cPtr)
{
	delete cPtr;
	return (int*)0;
}

VB_EXTERN
int* EdgeDraw_Segments_RunCPP(EdgeDraw_Segments * cPtr, int* dataPtr, int rows, int cols, int lineWidth)
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

VB_EXTERN
EdgeDraw_Lines * EdgeDraw_Lines_Open() {
	EdgeDraw_Lines* cPtr = new EdgeDraw_Lines();
	return cPtr;
}

VB_EXTERN
int* EdgeDraw_Lines_Close(EdgeDraw_Lines * cPtr)
{
	delete cPtr;
	return (int*)0;
}

VB_EXTERN
int EdgeDraw_Lines_Count(EdgeDraw_Lines * cPtr)
{
	return int(cPtr->lines.size());
}

VB_EXTERN
int* EdgeDraw_Lines_RunCPP(EdgeDraw_Lines * cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
	if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
	cPtr->RunCPP(lineWidth);
	return (int*)&cPtr->lines[0];
}