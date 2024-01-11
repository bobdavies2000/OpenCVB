#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "rbf.hpp"
#include "OpenCVB_Extern.h"

using namespace std;
using namespace  cv;
class recursiveBilateralFilter
{
private:
public:
	Mat src;
	int recursions = 2;
	float sigma_spatial = 0.03f;
	float sigma_range = 0.01f;
	recursiveBilateralFilter() {}
	void RecursiveBilateralFilter_Run()
	{
		for (int i = 0; i < recursions; ++i)
			_recursive_bf(src.data, sigma_spatial, sigma_range, src.cols, src.rows, 3);
	}
};

VB_EXTERN
recursiveBilateralFilter *RecursiveBilateralFilter_Open()
{
	recursiveBilateralFilter * cPtr = new recursiveBilateralFilter();
	return cPtr;
}

VB_EXTERN
int *RecursiveBilateralFilter_Close(recursiveBilateralFilter * cPtr)
{
  delete cPtr;
  return (int*)0;
}

VB_EXTERN
int *RecursiveBilateralFilter_Run(recursiveBilateralFilter * cPtr, int *bgrPtr, int rows, int cols, int recursions)
{
	cPtr->src = Mat(rows, cols, CV_8U, bgrPtr);
	cPtr->recursions = recursions;
	cPtr->RecursiveBilateralFilter_Run();
	return (int *)cPtr->src.data;
}