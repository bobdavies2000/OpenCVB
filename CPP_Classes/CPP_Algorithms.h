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
#include "opencv2/bgsegm.hpp"
#include "opencv2/photo.hpp"
#include <map>
#include <opencv2/ml.hpp>
#include "harrisDetector.h"

using namespace std;
using namespace  cv;
using namespace bgsegm;

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






#ifdef _DEBUG
const int cityMultiplier = 100;
const int puzzleMultiplier = 1;
#else
const int puzzleMultiplier = 1000;
const int cityMultiplier = 10000;
#endif

class CitySolver
{
public:
	std::vector<Point2f> cityPositions;
	std::vector<int> cityOrder;
	RNG rng;
	double currentTemperature = 100.0;
	char outMsg[512];
	int d0, d1, d2, d3;
private:

public:
	CitySolver()
	{
		rng = RNG(__rdtsc()); // When called in rapid succession, the rdtsc will prevent duplication when multi-threading...
		// rng = theRNG(); // To see the duplication problem with MT, uncomment this line.  Watch all the energy levels will be the same!
		currentTemperature = 100.0f;
	}
	/** Give energy value for a state of system.*/
	double energy() const
	{
		double e = 0;
		for (size_t i = 0; i < cityOrder.size(); i++)
		{
			e += norm(cityPositions[i] - cityPositions[cityOrder[i]]);
		}
		return e;
	}

	/** Function which change the state of system (random perturbation).*/
	void changeState()
	{
		d0 = rng.uniform(0, static_cast<int>(cityPositions.size()));
		d1 = cityOrder[d0];
		d2 = cityOrder[d1];
		d3 = cityOrder[d2];

		cityOrder[d0] = d2;
		cityOrder[d2] = d1;
		cityOrder[d1] = d3;
	}

	/** Function to reverse to the previous state.*/
	void reverseState()
	{
		cityOrder[d0] = d1;
		cityOrder[d1] = d2;
		cityOrder[d2] = d3;
	}
};

extern "C" __declspec(dllexport)
CitySolver * Annealing_Basics_Open(Point2f * cityPositions, int count)
{
	CitySolver* cPtr = new CitySolver();
	cPtr->cityPositions.assign(cityPositions, cityPositions + count);
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Annealing_Basics_Close(CitySolver * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
char* Annealing_Basics_Run(CitySolver * cPtr, int* cityOrder, int count)
{
	cPtr->cityOrder.assign(cityOrder, cityOrder + count);
	int changesApplied = ml::simulatedAnnealingSolver(*cPtr, cPtr->currentTemperature,
		cPtr->currentTemperature * 0.97, 0.99,
		cityMultiplier * count, &cPtr->currentTemperature, cPtr->rng);
	copy(cPtr->cityOrder.begin(), cPtr->cityOrder.end(), cityOrder);
	string msg = " changesApplied=" + to_string(changesApplied) + " temp=" + to_string(cPtr->currentTemperature) + " result = " + to_string(cPtr->energy());
	strcpy_s(cPtr->outMsg, msg.c_str());
	return cPtr->outMsg;
}





class BGRPattern_Basics
{
private:
public:
	Mat src, dst;
	int classCount = 5;
	BGRPattern_Basics() {}
	void RunCPP() {
		for (int y = 0; y < src.rows; y++)
			for (int x = 0; x < src.cols; x++)
			{
				Vec3b vec = src.at<Vec3b>(y, x);
				int b = vec[0]; int g = vec[1]; int r = vec[2];
				if (b == g && g == r)
				{
					dst.at<uchar>(y, x) = 1;
				}
				else if (b <= g && g <= r)
				{
					dst.at<uchar>(y, x) = 2;
				}
				else if (b >= g && g >= r)
				{
					dst.at<uchar>(y, x) = 3;
				}
				else if (b >= g && g <= r)
				{
					dst.at<uchar>(y, x) = 4;
				}
				else if (b <= g && g >= r)
				{
					dst.at<uchar>(y, x) = classCount;
				}
			}
	}
};
extern "C" __declspec(dllexport)
BGRPattern_Basics * BGRPattern_Open() {
	BGRPattern_Basics* cPtr = new BGRPattern_Basics();
	return cPtr;
}
extern "C" __declspec(dllexport)
void BGRPattern_Close(BGRPattern_Basics * cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int BGRPattern_ClassCount(BGRPattern_Basics * cPtr)
{
	return cPtr->classCount;
}

extern "C" __declspec(dllexport)
int* BGRPattern_RunCPP(BGRPattern_Basics * cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
	cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->RunCPP();
	return (int*)cPtr->dst.data;
}







class BGSubtract_BGFG
{
private:
public:
	Ptr<BackgroundSubtractor> algo;
	Mat src, fgMask;
	BGSubtract_BGFG() {}
	void Run() {
		algo->apply(src, fgMask);
	}
};

extern "C" __declspec(dllexport)
BGSubtract_BGFG * BGSubtract_BGFG_Open(int currMethod) {
	BGSubtract_BGFG* bgfs = new BGSubtract_BGFG();
	if (currMethod == 0)      bgfs->algo = createBackgroundSubtractorGMG(20, 0.7);
	else if (currMethod == 1) bgfs->algo = createBackgroundSubtractorCNT();
	else if (currMethod == 2) bgfs->algo = createBackgroundSubtractorKNN();
	else if (currMethod == 3) bgfs->algo = createBackgroundSubtractorMOG();
	else if (currMethod == 4) bgfs->algo = createBackgroundSubtractorMOG2();
	else if (currMethod == 5) bgfs->algo = createBackgroundSubtractorGSOC();
	else if (currMethod == 6) bgfs->algo = createBackgroundSubtractorLSBP();
	return bgfs;
}

extern "C" __declspec(dllexport)
int* BGSubtract_BGFG_Close(BGSubtract_BGFG * bgfs)
{
	delete bgfs;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* BGSubtract_BGFG_Run(BGSubtract_BGFG * bgfs, int* bgrPtr, int rows, int cols, int channels)
{
	bgfs->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, bgrPtr);
	bgfs->Run();
	return (int*)bgfs->fgMask.data;
}





class BGSubtract_Synthetic
{
private:
public:
	Ptr<bgsegm::SyntheticSequenceGenerator> gen;
	Mat src, output, fgMask;
	BGSubtract_Synthetic() {}
	void Run() {
	}
};

extern "C" __declspec(dllexport)
BGSubtract_Synthetic * BGSubtract_Synthetic_Open(int* bgrPtr, int rows, int cols, LPSTR fgFilename, double amplitude, double magnitude,
	double wavespeed, double objectspeed)
{
	BGSubtract_Synthetic* cPtr = new BGSubtract_Synthetic();
	Mat bg = Mat(rows, cols, CV_8UC3, bgrPtr);
	Mat fg = imread(fgFilename, IMREAD_COLOR);
	resize(fg, fg, Size(10, 10)); // adjust the object size here...
	cPtr->gen = bgsegm::createSyntheticSequenceGenerator(bg, fg, amplitude, magnitude, wavespeed, objectspeed);
	cPtr->gen->getNextFrame(cPtr->output, cPtr->fgMask);
	return cPtr;
}

extern "C" __declspec(dllexport)
int* BGSubtract_Synthetic_Close(BGSubtract_Synthetic * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* BGSubtract_Synthetic_Run(BGSubtract_Synthetic * cPtr)
{
	cPtr->gen->getNextFrame(cPtr->output, cPtr->fgMask);
	return (int*)cPtr->output.data;
}




extern "C" __declspec(dllexport)
int* Corners_ShiTomasi(int* grayPtr, int rows, int cols, int blockSize, int apertureSize)
{
	static Mat dst;
	Mat gray = Mat(rows, cols, CV_8UC1, grayPtr);
	dst = Mat::zeros(gray.size(), CV_32FC1);
	/// Shi-Tomasi -- Using cornerMinEigenVal - can't access this from opencvSharp...
	cornerMinEigenVal(gray, dst, blockSize, apertureSize, BORDER_DEFAULT);
	return (int*)dst.data;
}







//// https://www.codeproject.com/Articles/5362105/Perceptual-Hash-based-Image-Comparison-Coded-in-pl
//double dctSimple(double* DCTMatrix, double* ImgMatrix, int N, int M)
//{
//	int i, j, u, v;
//	int cnt = 0;
//	double DCTsum = 0.0;
//	for (u = 0; u < N; ++u)
//	{
//		for (v = 0; v < M; ++v)
//		{
//			DCTMatrix[(u * N) + v] = 0;
//			for (i = 0; i < N; i++)
//			{
//				for (j = 0; j < M; j++)
//				{
//					DCTMatrix[(u * N) + v] += ImgMatrix[(i * N) + j]
//						* cos(CV_PI / ((double)N) * (i + 1. / 2.) * u)
//						* cos(CV_PI / ((double)M) * (j + 1. / 2.) * v);
//					cnt = cnt++;
//				}
//			}
//			DCTsum += DCTMatrix[(u * N) + v];
//		}
//	}
//	DCTsum -= DCTMatrix[0];
//	return   DCTsum / cnt;
//}
//
//
//
//
//
//
//int CoeffFlag = 0;
//double Coeff[32][32];
//
//// https://www.codeproject.com/Articles/5362105/Perceptual-Hash-based-Image-Comparison-Coded-in-pl
//double DCT(double* DCTMatrix, double* IMGMatrix)
//{
//	/*
//		Concerning: the expression
//		dctRCvalue+= IMGMatrix[(imgRow * RowMax) + imgCol]    // this is dependent on the IMAGE
//		* cos( PIover32 *(imgRow+0.5)*dctRow)    //  this is a set of FIXED values
//		* cos( PIover32 *(imgCol+0.5)*dctCol);   //  this is a set of FIXED values
//
//		Let us call the  2 sets of FIXED values   rCoeff and cCoeff
//		they both have the same set of values
//		=  cos (  PIover32  * ( x + 0.5 ) * y )   for x = 0 .. 31    and y = for x = 0 .. 31
//		= 32*32 distinct COSINE values
//		= we could calculate these COSINE values in advance, place them in an array, and (hopefully) speed things up by doing a simple look up .
//	*/
//#define  PIover32 0.09817477042
//#define  RowMax 32
//#define  ColMax 32    
//	int imgRow, imgCol;
//	int dctRow, dctCol;
//	int x, y;
//	int cnt = 0;
//	double DCTsum = 0.0;
//	double dctRCvalue = 0.0;
//
//	if (!CoeffFlag)
//	{
//		for (x = 0; x < 32; x++)
//		{
//			for (y = 0; y < 32; y++)
//			{
//				Coeff[x][y] = cos(PIover32 * (x + 0.5) * y);
//			}
//		}
//		CoeffFlag = 1;
//	}
//
//	for (dctRow = 0; dctRow < 8; dctRow++)
//	{
//		for (dctCol = 0; dctCol < 8; dctCol++)
//		{
//			dctRCvalue = 0;
//
//			for (imgRow = 0; imgRow < RowMax; imgRow++)
//			{
//				for (imgCol = 0; imgCol < ColMax; imgCol++)
//				{
//					dctRCvalue += IMGMatrix[(imgRow * RowMax) + imgCol]
//						* Coeff[imgRow][dctRow]    //    cos( PIover32 *(imgRow+0.5)*dctRow)   
//						* Coeff[imgCol][dctCol];  //    cos( PIover32 *(imgCol+0.5)*dctCol) ; 
//					cnt = cnt++;
//				}
//			}
//			DCTMatrix[(dctRow * RowMax) + dctCol] = dctRCvalue;
//			DCTsum += dctRCvalue;
//		}
//	}
//	DCTsum -= DCTMatrix[0];
//	return   DCTsum / cnt;
//}






class Denoise_Basics
{
private:
public:
	Mat dst;
	int frameCount = 5;
	std::vector<Mat> frames;
	Denoise_Basics() {}
	void Run() {
		dst = frames.back();
		fastNlMeansDenoisingMulti(frames, dst, 2, 1);
	}
};

extern "C" __declspec(dllexport)
Denoise_Basics * Denoise_Basics_Open(int frameCount) {
	Denoise_Basics* cPtr = new Denoise_Basics();
	cPtr->frameCount = frameCount;
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Denoise_Basics_Close(Denoise_Basics * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Denoise_Basics_Run(Denoise_Basics * cPtr, int* bufferPtr, int rows, int cols)
{
	Mat src = Mat(rows, cols, CV_8UC1, bufferPtr);
	cPtr->frames.push_back(src.clone());
	if (cPtr->frames.size() < cPtr->frameCount)
	{
		return (int*)src.data;
	}
	else {
		cPtr->Run();
		cPtr->frames.pop_back();
	}
	return(int*)cPtr->dst.data;
}






class Denoise_Pixels
{
private:
public:
	Mat src;
	int edgeCountAfter, edgeCountBefore;
	Denoise_Pixels() {}
	void RunCPP() {
		edgeCountBefore = 0;
		for (int y = 0; y < src.rows; y++)
			for (int x = 1; x < src.cols - 1; x++)
			{
				int last = src.at<uchar>(y, x - 1);
				int curr = src.at<uchar>(y, x);
				if (last != curr)
				{
					edgeCountBefore++;
					if (last == src.at<uchar>(y, x + 1))
						src.at<uchar>(y, x) = last;
				}
			}
		for (int y = 1; y < src.rows - 1; y++)
			for (int x = 0; x < src.cols; x++)
			{
				int last = src.at<uchar>(y - 1, x);
				int curr = src.at<uchar>(y, x);
				if (last != curr)
					if (last == src.at<uchar>(y + 1, x))
						src.at<uchar>(y, x) = last;
			}
		edgeCountAfter = 0;
		for (int y = 0; y < src.rows; y++)
			for (int x = 1; x < src.cols; x++)
			{
				int last = src.at<uchar>(y, x - 1);
				int curr = src.at<uchar>(y, x);
				if (last != curr) edgeCountAfter++;
			}
	}
};
extern "C" __declspec(dllexport)
Denoise_Pixels * Denoise_Pixels_Open() {
	Denoise_Pixels* cPtr = new Denoise_Pixels();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Denoise_Pixels_Close(Denoise_Pixels * cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int Denoise_Pixels_EdgeCountBefore(Denoise_Pixels * cPtr)
{
	return cPtr->edgeCountBefore;
}

extern "C" __declspec(dllexport)
int Denoise_Pixels_EdgeCountAfter(Denoise_Pixels * cPtr)
{
	return cPtr->edgeCountAfter;
}
extern "C" __declspec(dllexport)
int* Denoise_Pixels_RunCPP(Denoise_Pixels * cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
	cPtr->RunCPP();
	return (int*)cPtr->src.data;
}





class Density_2D
{
private:
public:
	Mat src, dst;
	Density_2D() {}
	float distanceTo(Point3f v1, Point3f v2)
	{
		return sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y) + (v1.z - v2.z) * (v1.z - v2.z));
	}
	void RunCPP(float zDistance) {
		dst.setTo(0);
		int offx[] = { -1, 0, 1, -1, 1, -1, 0, 1 };
		int offy[] = { -1, -1, -1, 0, 0, 1, 1, 1 };
		for (int y = 1; y < src.rows - 1; y++)
		{
			for (int x = 1; x < src.cols - 1; x++)
			{
				float z1 = src.at<float>(y, x);
				if (z1 == 0) continue;
				float d = 0.0f;
				for (int i = 0; i < 8; i++)
				{
					float z2 = src.at<float>(y + offy[i], x + offx[i]);
					if (z2 == 0) continue;
					d += abs(z1 - z2);
				}
				if (d < zDistance && d != 0.0f) dst.at<unsigned char>(y, x) = 255;
			}
		}
	}
};

extern "C" __declspec(dllexport)
Density_2D * Density_2D_Open() {
	Density_2D* cPtr = new Density_2D();
	return cPtr;
}

extern "C" __declspec(dllexport)
void Density_2D_Close(Density_2D * cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* Density_2D_RunCPP(Density_2D * cPtr, int* dataPtr, int rows, int cols, float zDistance)
{
	cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
	cPtr->RunCPP(zDistance);
	return (int*)cPtr->dst.data;
}







class Density_Count
{
private:
public:
	Mat src, dst;
	Density_Count() {}
	void RunCPP(int zCount) {
		dst.setTo(0);
		int offx[] = { -1, 0, 1, -1, 1, -1, 0, 1 };
		int offy[] = { -1, -1, -1, 0, 0, 1, 1, 1 };
		for (int y = 1; y < src.rows - 1; y++)
		{
			for (int x = 1; x < src.cols - 1; x++)
			{
				float z1 = src.at<float>(y, x);
				if (z1 == 0) continue;
				int count = 0;
				for (int i = 0; i < 8; i++)
				{
					float z2 = src.at<float>(y + offy[i], x + offx[i]);
					if (z2 > 0) count += 1;
				}
				if (count >= zCount) dst.at<unsigned char>(y, x) = 255;
			}
		}
	}
};

extern "C" __declspec(dllexport)
Density_Count * Density_Count_Open() {
	Density_Count* cPtr = new Density_Count();
	return cPtr;
}

extern "C" __declspec(dllexport)
void Density_Count_Close(Density_Count * cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* Density_Count_RunCPP(Density_Count * cPtr, int* dataPtr, int rows, int cols, int zCount)
{
	cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
	cPtr->RunCPP(zCount);
	return (int*)cPtr->dst.data;
}






class Depth_Colorizer
{
private:
public:
	Mat depth32f, output;
	int histSize = 255;
	Depth_Colorizer() {}
	void Run(float maxDepth)
	{
		float nearColor[3] = { 0, 1.0f, 1.0f };
		float farColor[3] = { 1.0f, 0, 0 };
		output = Mat(depth32f.size(), CV_8UC3);
		auto rgb = (unsigned char*)output.data;
		float* depthImage = (float*)depth32f.data;
		for (int i = 0; i < output.cols * output.rows; i++)
		{
			float t = depthImage[i] / maxDepth;
			if (t > 0 && t <= 1)
			{
				*rgb++ = uchar(((1 - t) * nearColor[0] + t * farColor[0]) * 255);
				*rgb++ = uchar(((1 - t) * nearColor[1] + t * farColor[1]) * 255);
				*rgb++ = uchar(((1 - t) * nearColor[2] + t * farColor[2]) * 255);
			}
			else {
				*rgb++ = 0; *rgb++ = 0; *rgb++ = 0;
			}
		}
	}
};




class DepthXYZ
{
private:
public:
	Mat depth, depthxyz;
	float ppx, ppy, fx, fy;
	DepthXYZ(float _ppx, float _ppy, float _fx, float _fy)
	{
		ppx = _ppx; ppy = _ppy; fx = _fx; fy = _fy;
	}
	void GetImageCoordinates()
	{
		depthxyz = Mat(depth.rows, depth.cols, CV_32FC3);
#ifdef _DEBUG
		// #pragma omp parallel for  // doubles performance in debug mode but is much worse in Release mode.
#endif
		for (int y = 0; y < depth.rows; y++)
		{
			for (int x = 0; x < depth.cols; x++)
			{
				float d = float(depth.at<float>(y, x)) * 0.001f;
				depthxyz.at<Vec3f>(y, x) = Vec3f(float(x), float(y), d);
			}
		}
	}
	void Run()
	{
		depthxyz = Mat(depth.rows, depth.cols, CV_32FC3);
#ifdef _DEBUG
		//#pragma omp parallel for  // doubles performance in debug mode but is much worse in Release mode.
#endif
		for (int y = 0; y < depth.rows; y++)
		{
			for (int x = 0; x < depth.cols; x++)
			{
				float d = float(depth.at<float>(y, x)) * 0.001f;
				if (d > 0) depthxyz.at< Vec3f >(y, x) = Vec3f(float((x - ppx) / fx), float((y - ppy) / fy), d);
			}
		}
	}
};





extern "C" __declspec(dllexport)
Depth_Colorizer * Depth_Colorizer_Open()
{
	Depth_Colorizer* cPtr = new Depth_Colorizer();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer_Close(Depth_Colorizer * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer_Run(Depth_Colorizer * cPtr, int* depthPtr, int rows, int cols, float maxDepth)
{
	cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	cPtr->Run(maxDepth);
	return (int*)cPtr->output.data;
}







class SimpleProjection
{
private:
public:
	Mat depth32f, mask, viewTop, viewSide;
	SimpleProjection() {}

	void Run(float desiredMin, float desiredMax, int w, int h)
	{
		float range = float(desiredMax - desiredMin);
		float hRange = float(h);
		float wRange = float(w);
#pragma omp parallel for
		for (int y = 0; y < depth32f.rows; ++y)
		{
			for (int x = 0; x < depth32f.cols; ++x)
			{
				uchar m = mask.at<uchar>(y, x);
				if (m == 255)
				{
					float d = depth32f.at<float>(y, x);
					float dy = hRange * (d - desiredMin) / range;
					if (dy > 0 && dy < hRange) viewTop.at<uchar>(int((hRange - dy)), x) = 0;
					float dx = wRange * (d - desiredMin) / range;
					if (dx < wRange && dx > 0) viewSide.at<uchar>(y, int(dx)) = 0;
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
SimpleProjection * SimpleProjectionOpen()
{
	SimpleProjection* cPtr = new SimpleProjection();
	return cPtr;
}

extern "C" __declspec(dllexport)
void SimpleProjectionClose(SimpleProjection * cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionSide(SimpleProjection * cPtr)
{
	return (int*)cPtr->viewSide.data;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionRun(SimpleProjection * cPtr, int* depthPtr, float desiredMin, float desiredMax, int rows, int cols)
{
	cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	threshold(cPtr->depth32f, cPtr->mask, 0, 255, ThresholdTypes::THRESH_BINARY);
	convertScaleAbs(cPtr->mask, cPtr->mask);
	cPtr->viewTop = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->viewSide = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->Run(desiredMin, desiredMax, cols, rows);
	return (int*)cPtr->viewTop.data;
}








class Project_GravityHistogram
{
private:
public:
	Mat xyz, histTop, histSide;
	Project_GravityHistogram() {}

	void Run(float maxZ, int w, int h)
	{
		float zHalf = maxZ / 2;
		float range = float(h);
		int shift = int((histTop.cols - histTop.rows) / 2); // shift to the center of the image.
		//#pragma omp parallel for  // this is faster without OpenMP!  But try it again when opportunity arrives...
		for (int y = 0; y < xyz.rows; ++y)
		{
			for (int x = 0; x < xyz.cols; ++x)
			{
				Point3f pt = xyz.at<Point3f>(y, x);
				float d = pt.z;
				if (d > 0 and d < maxZ)
				{
					float fx = pt.x;
					int x = int(range * (zHalf + fx) / maxZ + shift); // maintain a 1:1 aspect ratio
					int y = int(range - range * d / maxZ);
					if (x >= 0 && x < xyz.cols && y >= 0 && y < xyz.rows) histTop.at<float>(y, x) += 1;

					float fy = pt.y;
					if (fy > -zHalf && fy < zHalf)
					{
						int x = int(range * d / maxZ + shift);
						int y = int(range * (zHalf + fy) / maxZ); // maintain a 1:1 aspect ratio
						if (x >= 0 && x < xyz.cols && y >= 0 && y < xyz.rows) histSide.at<float>(y, x) += 1;
					}
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
Project_GravityHistogram * Project_GravityHist_Open()
{
	Project_GravityHistogram* cPtr = new Project_GravityHistogram();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Close(Project_GravityHistogram * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Side(Project_GravityHistogram * cPtr)
{
	return (int*)cPtr->histSide.data;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Run(Project_GravityHistogram * cPtr, int* xyzPtr, float maxZ, int rows, int cols)
{
	cPtr->xyz = Mat(rows, cols, CV_32FC3, xyzPtr);
	cPtr->histTop = Mat(rows, cols, CV_32F).setTo(0);
	cPtr->histSide = Mat(rows, cols, CV_32F).setTo(0);
	cPtr->Run(maxZ, cols, rows);
	return (int*)cPtr->histTop.data;
}





// https://docs.opencv.org/3.1.0/d0/da5/tutorial_ximgproc_prediction.html
class Edge_RandomForest
{
private:
	Ptr<StructuredEdgeDetection> pDollar;
public:
	Mat dst32f, src32f, gray8u;
	Edge_RandomForest(char* modelFileName) { pDollar = createStructuredEdgeDetection(modelFileName); }

	void Run(Mat src)
	{
		src.convertTo(src32f, CV_32FC3, 1.0 / 255.0);
		pDollar->detectEdges(src32f, dst32f);
		dst32f.convertTo(gray8u, CV_8U, 255);
	}
};

extern "C" __declspec(dllexport)
Edge_RandomForest * Edge_RandomForest_Open(char* modelFileName)
{
	return new Edge_RandomForest(modelFileName);
}

extern "C" __declspec(dllexport)
int* Edge_RandomForest_Close(Edge_RandomForest * Edge_RandomForestPtr)
{
	delete Edge_RandomForestPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_RandomForest_Run(Edge_RandomForest * Edge_RandomForestPtr, int* bgrPtr, int rows, int cols)
{
	Edge_RandomForestPtr->Run(Mat(rows, cols, CV_8UC3, bgrPtr));
	return (int*)Edge_RandomForestPtr->gray8u.data;
}





// https://github.com/opencv/opencv_contrib/blob/master/modules/ximgproc/samples/dericheSample.py
class Edge_Deriche
{
private:
public:
	Mat src, dst;
	Edge_Deriche() {}
	void Run(float alpha, float omega) {
		Mat xdst, ydst;
		ximgproc::GradientDericheX(src, xdst, alpha, omega);
		ximgproc::GradientDericheY(src, ydst, alpha, omega);
		Mat dx2 = xdst.mul(xdst);
		Mat dy2 = ydst.mul(ydst);
		Mat d2 = dx2 + dy2;
		sqrt(d2, d2);
		normalize(d2, d2, 255, NormTypes::NORM_MINMAX);
		d2.convertTo(dst, CV_8UC3, 255, 0);
	}
};

extern "C" __declspec(dllexport)
Edge_Deriche * Edge_Deriche_Open()
{
	Edge_Deriche* dPtr = new Edge_Deriche();
	return dPtr;
}

extern "C" __declspec(dllexport)
int* Edge_Deriche_Close(Edge_Deriche * dPtr)
{
	delete dPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_Deriche_Run(Edge_Deriche * dPtr, int* bgrPtr, int rows, int cols, float alpha, float omega)
{
	dPtr->src = Mat(rows, cols, CV_8UC3, bgrPtr);
	dPtr->Run(alpha, omega);
	return (int*)dPtr->dst.data;
}









using namespace std;
using namespace  cv;
class Edge_ColorGap
{
private:
public:
	Mat src, dst = Mat(0, 0, CV_8U);
	Edge_ColorGap() {}
	void Run(int distance, int diff) {
		dst.setTo(0);
		int half = distance / 2, pix1, pix2;
		for (int y = 0; y < dst.rows; y++)
		{
			for (int x = distance; x < dst.cols - distance; x++)
			{
				pix1 = src.at<unsigned char>(y, x);
				pix2 = src.at<unsigned char>(y, x + distance);
				if (abs(pix1 - pix2) >= diff) dst.at<unsigned char>(y, x + half) = 255;
			}
		}
		for (int y = distance; y < dst.rows - distance; y++)
		{
			for (int x = 0; x < dst.cols; x++)
			{
				pix1 = src.at<unsigned char>(y, x);
				pix2 = src.at<unsigned char>(y + distance, x);
				if (abs(pix1 - pix2) >= diff) dst.at<unsigned char>(y + half, x) = 255;
			}
		}
	}
};

extern "C" __declspec(dllexport)
Edge_ColorGap * Edge_ColorGap_Open() {
	Edge_ColorGap* cPtr = new Edge_ColorGap();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Edge_ColorGap_Close(Edge_ColorGap * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_ColorGap_Run(Edge_ColorGap * cPtr, int* grayPtr, int rows, int cols, int distance, int diff)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, grayPtr);
	if (cPtr->dst.rows != rows) cPtr->dst = Mat(rows, cols, CV_8UC1);
	cPtr->Run(distance, diff);
	return (int*)cPtr->dst.data;
}








class Edge_DepthGap
{
private:
public:
	Mat src, dst;
	Edge_DepthGap() {}
	void RunCPP(float minDiff) {
		dst = Mat(src.rows, src.cols, CV_8UC1);
		dst.setTo(0);
		for (int y = 1; y < src.rows - 1; y++)
		{
			for (int x = 1; x < src.cols - 1; x++)
			{
				float b1 = src.at<float>(y, x - 1);
				float b2 = src.at<float>(y, x);
				if (abs(b1 - b2) >= minDiff)
				{
					Rect r = Rect(x, y - 1, 2, 3);
					dst(r).setTo(255);
				}

				b1 = src.at<float>(y - 1, x);
				if (abs(b1 - b2) >= minDiff)
				{
					Rect r = Rect(x - 1, y, 3, 2);
					dst(r).setTo(255);
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
Edge_DepthGap * Edge_DepthGap_Open() {
	Edge_DepthGap* cPtr = new Edge_DepthGap();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Edge_DepthGap_Close(Edge_DepthGap * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_DepthGap_RunCPP(Edge_DepthGap * cPtr, int* dataPtr, int rows, int cols, float minDiff)
{
	cPtr->src = Mat(rows, cols, CV_32FC1, dataPtr);
	cPtr->RunCPP(minDiff);
	return (int*)cPtr->dst.data;
}





// Why did we need a C++ version of the EM OpenCV API's?  Because the OpenCVSharp Predict2 interface seems to be broken.
class EMax_Raw
{
private:
public:
	Mat samples, output;
	Mat labels;
	Mat testInput;
	Ptr<EM> em_model;
	RNG rng;
	int clusters;
	int covarianceMatrixType;
	int stepSize;
	EMax_Raw()
	{
		em_model = EM::create();
	}
	void Run()
	{
		em_model->setClustersNumber(clusters);
		em_model->setCovarianceMatrixType(covarianceMatrixType);
		em_model->setTermCriteria(TermCriteria(TermCriteria::COUNT + TermCriteria::EPS, 300, 0.1));
		em_model->trainEM(samples, noArray(), labels, noArray());

		// classify every image pixel
		Mat sample(1, 2, CV_32FC1);
		output.setTo(0);
		int half = stepSize / 2;
		for (int y = 0; y < output.rows; y += stepSize)
		{
			for (int x = 0; x < output.cols; x += stepSize)
			{
				sample.at<float>(0) = float(x);
				sample.at<float>(1) = float(y);
				int response = cvRound(em_model->predict2(sample, noArray())[1]);
				circle(output, Point(x, y), stepSize, response, -1);
			}
		}
	}
};

extern "C" __declspec(dllexport)
EMax_Raw * EMax_Open()
{
	EMax_Raw* cPtr = new EMax_Raw();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* EMax_Close(EMax_Raw * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* EMax_Run(EMax_Raw * cPtr, int* samplePtr, int* labelsPtr, int inCount, int dimension, int rows, int cols, int clusters,
	int stepSize, int covarianceMatrixType)
{
	cPtr->covarianceMatrixType = covarianceMatrixType;
	cPtr->stepSize = stepSize;
	cPtr->clusters = clusters;
	cPtr->labels = Mat(inCount, 1, CV_32S, labelsPtr);
	cPtr->samples = Mat(inCount, dimension, CV_32FC1, samplePtr);
	cPtr->output = Mat(rows, cols, CV_32S);
	cPtr->Run();
	return (int*)cPtr->output.data;
}





float outputTriangleAMS[5];
extern "C" __declspec(dllexport)
int* FitEllipse_AMS(float* inputPoints, int count)
{
	Mat input(count, 1, CV_32FC2, inputPoints);
	RotatedRect box = fitEllipseAMS(input);
	outputTriangleAMS[0] = box.angle;
	outputTriangleAMS[1] = box.center.x;
	outputTriangleAMS[2] = box.center.y;
	outputTriangleAMS[3] = box.size.width;
	outputTriangleAMS[4] = box.size.height;
	return (int*)&outputTriangleAMS;
}



float outputTriangle[5];
extern "C" __declspec(dllexport)
int* FitEllipse_Direct(float* inputPoints, int count)
{
	Mat input(count, 1, CV_32FC2, inputPoints);
	RotatedRect box = fitEllipseDirect(input);
	outputTriangle[0] = box.angle;
	outputTriangle[1] = box.center.x;
	outputTriangle[2] = box.center.y;
	outputTriangle[3] = box.size.width;
	outputTriangle[4] = box.size.height;
	return (int*)&outputTriangle;
}








class Fuzzy
{
private:
public:
	Mat src, dst;
	Fuzzy() {}
	void Run() {
		dst = Mat(src.rows, src.cols, CV_8U);
		dst.setTo(0);
		for (int y = 1; y < src.rows - 3; ++y)
		{
			//#pragma omp parallel for 
			for (int x = 1; x < src.cols - 3; ++x)
			{
				int pixel = src.at<uchar>(y, x);
				Rect r = Rect(x, y, 3, 3);
				Scalar sum = cv::sum(src(r));
				if (sum.val[0] == double(pixel * 9)) dst.at<uchar>(y + 1, x + 1) = pixel;
			}
		}
	}
};

extern "C" __declspec(dllexport)
Fuzzy * Fuzzy_Open() {
	Fuzzy* cPtr = new Fuzzy();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Fuzzy_Close(Fuzzy * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Fuzzy_Run(Fuzzy * cPtr, int* grayPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, grayPtr);
	cPtr->Run();
	return (int*)cPtr->dst.data;
}






class Guess_Depth
{
private:
public:
	Mat src;
	Guess_Depth() {}
	void RunCPP() {
		for (int y = 1; y < src.rows - 1; y++)
		{
			for (int x = 1; x < src.cols - 1; x++)
			{
				Point3f s1 = src.at<Point3f>(y, x - 1);
				Point3f s2 = src.at<Point3f>(y, x);
				if (s1.z > 0 && s2.z == 0)
				{
					Point3f s3 = src.at<Point3f>(y - 1, x);
					Point3f s4 = src.at<Point3f>(y + 1, x);
					Point3f s5 = src.at<Point3f>(y, x + 1);
					if ((s3.z > 0 && s4.z > 0) || s5.z > 0) src.at<Point3f>(y, x) = s1; // duplicate the neighbor next to this pixel missing any depth.
				}
			}
		}
	}
};


extern "C" __declspec(dllexport)
Guess_Depth * Guess_Depth_Open() {
	Guess_Depth* cPtr = new Guess_Depth();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Guess_Depth_Close(Guess_Depth * cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport)
int* Guess_Depth_RunCPP(Guess_Depth * cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_32FC3, dataPtr);
	cPtr->RunCPP();
	return (int*)cPtr->src.data;
}






class Guess_ImageEdges
{
private:
public:
	Mat pc;
	Guess_ImageEdges() {}
	void RunCPP(int maxDistanceToEdge) {
		int y = 0;
		int maxGap = maxDistanceToEdge;
		for (int x = 0; x < pc.cols; x++)
		{
			for (int y = 0; y < maxGap; y++)
			{
				Point3f s1 = pc.at<Point3f>(y, x);
				if (s1.z > 0)
				{
					for (int i = 0; i < y; i++)
					{
						pc.at<Point3f>(i, x) = s1;
					}
					break;
				}
			}
			for (int y = 1; y < maxGap; y++)
			{
				Point3f s1 = pc.at<Point3f>(pc.rows - y, x);
				if (s1.z > 0)
				{
					for (int i = 1; i < y; i++)
					{
						pc.at<Point3f>(pc.rows - i, x) = s1;
					}
					break;
				}
			}
		}
		for (int y = 0; y < pc.rows; y++)
		{
			for (int x = 0; x < maxGap; x++)
			{
				Point3f s1 = pc.at<Point3f>(y, x);
				if (s1.z > 0)
				{
					for (int i = 0; i < x; i++)
					{
						pc.at<Point3f>(y, i) = s1;
					}
					break;
				}
			}
			for (int x = 1; x < maxGap; x++)
			{
				Point3f s1 = pc.at<Point3f>(y, pc.cols - x);
				if (s1.z > 0)
				{
					for (int i = 1; i < x; i++)
					{
						pc.at<Point3f>(y, pc.cols - i) = s1;
					}
					break;
				}
			}
		}
	}
};






extern "C" __declspec(dllexport)
Guess_ImageEdges * Guess_ImageEdges_Open() {
	Guess_ImageEdges* cPtr = new Guess_ImageEdges();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Guess_ImageEdges_Close(Guess_ImageEdges * cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport)
int* Guess_ImageEdges_RunCPP(Guess_ImageEdges * cPtr, int* dataPtr, int rows, int cols, int maxDistanceToEdge)
{
	cPtr->pc = Mat(rows, cols, CV_32FC3, dataPtr);
	cPtr->RunCPP(maxDistanceToEdge);
	return (int*)cPtr->pc.data;
}






class Harris_Features
{
private:
public:
	Mat src, dst;
	float threshold = 0.0001f;
	int neighborhood = 3;
	int aperture = 3;
	float HarrisParm = 0.01f;
	Harris_Features() {}
	void Run()
	{
		cv::Mat cornerStrength;
		cv::cornerHarris(src, cornerStrength, neighborhood, aperture, HarrisParm);
		cv::threshold(cornerStrength, dst, threshold, 255, cv::THRESH_BINARY_INV);
	}
};

extern "C" __declspec(dllexport)
Harris_Features * Harris_Features_Open()
{
	return new Harris_Features();
}

extern "C" __declspec(dllexport)
int* Harris_Features_Close(Harris_Features * Harris_FeaturesPtr)
{
	delete Harris_FeaturesPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Harris_Features_Run(Harris_Features * Harris_FeaturesPtr, int* bgrPtr, int rows, int cols, float threshold, int neighborhood, int aperture, float HarrisParm)
{
	Harris_FeaturesPtr->threshold = threshold;
	Harris_FeaturesPtr->neighborhood = neighborhood;
	Harris_FeaturesPtr->aperture = aperture;
	Harris_FeaturesPtr->HarrisParm = HarrisParm;
	Harris_FeaturesPtr->src = Mat(rows, cols, CV_8U, bgrPtr);
	Harris_FeaturesPtr->Run();
	return (int*)Harris_FeaturesPtr->dst.data;
}



class Harris_Detector
{
private:
public:
	std::vector<cv::Point> pts;
	Mat src;
	double qualityLevel = 0.02;
	HarrisDetector harris;
	Harris_Detector() {}
	void Run()
	{
		harris.detect(src);
		harris.getCorners(pts, qualityLevel);
		harris.drawOnImage(src, pts);
	}
};

extern "C" __declspec(dllexport)
Harris_Detector * Harris_Detector_Open()
{
	return new Harris_Detector();
}

extern "C" __declspec(dllexport)
int* Harris_Detector_Close(Harris_Detector * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Harris_Detector_Run(Harris_Detector * cPtr, int* bgrPtr, int rows, int cols, double qualityLevel, int* count)
{
	cPtr->qualityLevel = qualityLevel;
	cPtr->src = Mat(rows, cols, CV_8U, bgrPtr);
	cPtr->pts.clear();
	cPtr->Run();
	count[0] = int(cPtr->pts.size());
	if (count[0] == 0) return 0;
	return (int*)&cPtr->pts[0];
}







// https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
// https://docs.opencv.org/trunk/d1/d1d/tutorial_histo3D.html
// When run in VB.Net the histogram output has rows = -1 and cols = -1
// The rows and cols are both -1 so I had assumed there was a bug but the data is accessible with the at method.
// If you attempt the same access to the data in managed code, it does not work (AFAIK).
extern "C" __declspec(dllexport)
float* Hist3Dcolor_Run(int* bgrPtr, int rows, int cols, int bins)
{
	float hRange[] = { 0, 256 }; // ranges are exclusive in the top of the range, hence 256
	const float* range[] = { hRange, hRange, hRange };
	int hbins[] = { bins, bins, bins };
	int channels[] = { 0, 1, 2 };
	Mat src = Mat(rows, cols, CV_8UC3, bgrPtr);

	static Mat histogram;
	calcHist(&src, 1, channels, Mat(), histogram, 3, hbins, range);

	return (float*)histogram.data;
}




static Mat newImage;

// https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
// https://docs.opencv.org/trunk/d1/d1d/tutorial_histo3D.html
// When run in VB.Net the histogram output has rows = -1 and cols = -1
// The rows and cols are both -1 so I had assumed there was a bug but the data is accessible with the at method.
// If you attempt the same access to the data in managed code, it does not work (AFAIK).
extern "C" __declspec(dllexport)
uchar * BackProjectBGR_Run(int* bgrPtr, int rows, int cols, int bins, float threshold)
{
	Mat input = Mat(rows, cols, CV_8UC3, bgrPtr);
	float hRange0[] = { 0, 256 };
	float hRange1[] = { 0, 256 };
	float hRange2[] = { 0, 256 };
	const float* range[] = { hRange0, hRange1, hRange2 };
	int hbins[] = { bins, bins, bins };
	int channels[] = { 0, 1, 2 };

	static Mat histogram;
	calcHist(&input, 1, channels, Mat(), histogram, 3, hbins, range); // for 3D histograms, all 3 bins must be equal.

	float* hist = (float*)histogram.data;
	for (int i = 0; i < bins * bins * bins; i++)
	{
		if (hist[i] < threshold) hist[i] = 0;
	}

	calcBackProject(&input, 1, channels, histogram, newImage, range);

	return (uchar*)newImage.data;
}





extern "C" __declspec(dllexport)
uchar * Hist3Dcloud_Run(int* inputPtr, int rows, int cols, int bins,
	float minX, float minY, float minZ,
	float maxX, float maxY, float maxZ)
{
	Mat input = Mat(rows, cols, CV_32FC3, inputPtr);
	float hRange0[] = { float(minX), float(maxX) };
	float hRange1[] = { float(minY), float(maxY) };
	float hRange2[] = { float(minZ), float(maxZ) };
	const float* range[] = { hRange0, hRange1, hRange2 };
	int hbins[] = { bins, bins, bins };
	int channel[] = { 0, 1, 2 };

	static Mat histogram;
	calcHist(&input, 1, channel, Mat(), histogram, 3, hbins, range, true, false); // for 3D histograms, all 3 bins must be equal.

	return (uchar*)histogram.data;
}








static Mat mask;

extern "C" __declspec(dllexport)
uchar * BackProjectCloud_Run(int* inputPtr, int rows, int cols, int bins, float threshold,
	float minX, float minY, float minZ,
	float maxX, float maxY, float maxZ)
{
	Mat input = Mat(rows, cols, CV_32FC3, inputPtr);
	float hRange0[] = { float(minX), float(maxX) };
	float hRange1[] = { float(minY), float(maxY) };
	float hRange2[] = { float(minZ), float(maxZ) };
	const float* range[] = { hRange0, hRange1, hRange2 };
	int hbins[] = { bins, bins, bins };
	int channels[] = { 0, 1, 2 };

	static Mat histogram;
	calcHist(&input, 1, channels, Mat(), histogram, 3, hbins, range); // for 3D histograms, all 3 bins must be equal.

	float* hist = (float*)histogram.data;
	for (int i = 0; i < bins * bins * bins; i++)
	{
		if (hist[i] < threshold) hist[i] = 0; else hist[i] = 255; // building a mask
	}

	Mat mask32f;
	calcBackProject(&input, 1, channels, histogram, mask32f, range);
	mask32f.convertTo(mask, CV_8U);

	return (uchar*)mask.data;
}