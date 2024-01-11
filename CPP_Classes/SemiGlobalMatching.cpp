#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include "SemiGlobalMatching.h"
#include <opencv2/calib3d.hpp>
#include "OpenCVB_Extern.h"
using namespace std;
using namespace  cv;
class SemiGlobalMatching
{
private:
	unsigned long ***C; // pixel cost array W x H x D
	unsigned long ***S; // aggregated cost array W x H x D
	unsigned int ****A; // single path cost array path_nos x W x H x D
public:
	Mat leftImage, rightImage;
	int disparityRange = 3;
	Mat disparityMapstage2;

	SemiGlobalMatching(int rows, int cols, int _disparityRange)
	{
		disparityRange = _disparityRange;
		// allocate cost arrays
		C = new unsigned long**[rows];
		S = new unsigned long**[rows];
		for (int row = 0; row < rows; ++row) {
			C[row] = new unsigned long*[cols];
			S[row] = new unsigned long*[cols];
			for (int col = 0; col < cols; ++col) {
				C[row][col] = new unsigned long[disparityRange]();
				S[row][col] = new unsigned long[disparityRange]();
			}
		}
		
		A = new unsigned int ***[PATHS_PER_SCAN];
		for (int path = 0; path < PATHS_PER_SCAN; ++path) {
			A[path] = new unsigned int **[rows];
			for (int row = 0; row < rows; ++row) {
				A[path][row] = new unsigned int*[cols];
				for (int col = 0; col < cols; ++col) {
					A[path][row][col] = new unsigned int[disparityRange];
					for (int d = 0; d < disparityRange; ++d) {
						A[path][row][col][d] = 0;
					}
				}
			}
		}
		disparityMapstage2 = Mat(Size(cols, rows), CV_8UC1, Scalar::all(0));
	}
	void Run()
	{
		//Initial Smoothing
		GaussianBlur(leftImage, leftImage, Size(BLUR_RADIUS, BLUR_RADIUS), 0, 0);
		GaussianBlur(rightImage, rightImage, Size(BLUR_RADIUS, BLUR_RADIUS), 0, 0);

		calculateCostHamming(leftImage, rightImage, disparityRange, C, S);
		aggregation(leftImage, rightImage, disparityRange, C, S, A);
		computeDisparity(disparityRange, leftImage.rows, leftImage.cols, S, disparityMapstage2);
	}
};





VB_EXTERN
SemiGlobalMatching *SemiGlobalMatching_Open(int rows, int cols, int disparityRange)
{
  SemiGlobalMatching *SemiGlobalMatchingPtr = new SemiGlobalMatching(rows, cols, disparityRange);
  return SemiGlobalMatchingPtr;
}

VB_EXTERN
int * SemiGlobalMatching_Close(SemiGlobalMatching *SemiGlobalMatchingPtr)
{
  delete SemiGlobalMatchingPtr;
  return (int*)0;
}

// https://github.com/epiception/SGM-Census
VB_EXTERN
int *SemiGlobalMatching_Run(SemiGlobalMatching *SemiGlobalMatchingPtr, int *leftPtr, int *rightPtr, int rows, int cols)
{
	SemiGlobalMatchingPtr->leftImage = Mat(rows, cols, CV_8U, leftPtr);
	SemiGlobalMatchingPtr->rightImage = Mat(rows, cols, CV_8U, leftPtr);
	SemiGlobalMatchingPtr->Run();

	return (int *) SemiGlobalMatchingPtr->disparityMapstage2.data; 
}

