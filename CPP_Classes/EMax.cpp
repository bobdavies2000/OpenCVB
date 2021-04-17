#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/ml.hpp>

using namespace std;
using namespace cv;
using namespace cv::ml;
// Why did we need a C++ version of the EM OpenCV API's?  Because the OpenCVSharp Predict2 interface is broken.
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
		for (int i = 0; i < output.rows; i+=stepSize)
		{
//#pragma omp parallel for
			for (int j = 0; j < output.cols; j+= stepSize)
			{
				sample.at<float>(0) = (float)j;
				sample.at<float>(1) = (float)i;
				int response = cvRound(em_model->predict2(sample, noArray())[1]);
				circle(output, Point(j, i), stepSize, response, FILLED);
			}
		}
	}
};

extern "C" __declspec(dllexport)
EMax_Raw *EMax_Raw_Open()
{
    EMax_Raw *EMax_RawPtr = new EMax_Raw();
    return EMax_RawPtr;
}

extern "C" __declspec(dllexport)
void EMax_Raw_Close(EMax_Raw *EMax_RawPtr)
{
    delete EMax_RawPtr;
}

extern "C" __declspec(dllexport)
int *EMax_Raw_Run(EMax_Raw *EMax_RawPtr, int *samplePtr, int *labelsPtr, int rows, int cols, int imgRows, int imgCols, int clusters,
					 int stepSize, int covarianceMatrixType)
{
	EMax_RawPtr->covarianceMatrixType = covarianceMatrixType;
	EMax_RawPtr->stepSize = stepSize;
	EMax_RawPtr->clusters = clusters;
	EMax_RawPtr->labels = Mat(rows, 1, CV_32S, labelsPtr);
	EMax_RawPtr->samples = Mat(rows, cols, CV_32FC1, samplePtr);
	EMax_RawPtr->output = Mat(imgRows, imgCols, CV_8U);
	EMax_RawPtr->Run();
    return (int *) EMax_RawPtr->output.data; // return this C++ allocated data to managed code where it will be used in the marshal.copy
}