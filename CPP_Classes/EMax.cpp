#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/ml.hpp>

using namespace std;
using namespace  cv;
using namespace cv::ml;
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
		for (int y = 0; y < output.rows; y+=stepSize)
		{
			for (int x = 0; x < output.cols; x+= stepSize)
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
EMax_Raw *EMax_Open()
{
    EMax_Raw * cPtr = new EMax_Raw();
    return cPtr;
}

extern "C" __declspec(dllexport)
int * EMax_Close(EMax_Raw * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int *EMax_Run(EMax_Raw *cPtr, int *samplePtr, int *labelsPtr, int inCount, int dimension, int rows, int cols, int clusters,
					 int stepSize, int covarianceMatrixType)
{
	cPtr->covarianceMatrixType = covarianceMatrixType;
	cPtr->stepSize = stepSize;
	cPtr->clusters = clusters;
	cPtr->labels = Mat(inCount, 1, CV_32S, labelsPtr);
	cPtr->samples = Mat(inCount, dimension, CV_32FC1, samplePtr);
	cPtr->output = Mat(rows, cols, CV_32S);
	cPtr->Run();
    return (int *)cPtr->output.data;
}