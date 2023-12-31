#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/bioinspired.hpp"

// https://docs.opencv.org/3.4/d3/d86/tutorial_bioinspired_retina_model.html
using namespace std;
using namespace cv;
class Retina
{
private:
public:
	// declare retina output buffers
	Mat retinaOutput_parvo;
	Mat retinaOutput_magno;
	bool useLogSampling;
	float samplingFactor;
	Mat src;
	// create a retina instance with default parameters setup, uncomment the initialisation you wanna test
	cv::Ptr<cv::bioinspired::Retina> myRetina;
	Retina(int rows, int cols, bool _useLogSampling, float _samplingFactor)
	{
		useLogSampling = _useLogSampling;
		samplingFactor = _samplingFactor;
		src = Mat(rows, cols, CV_8UC3);
		if (useLogSampling)
		{
			myRetina = cv::bioinspired::Retina::create(src.size(), true, cv::bioinspired::RETINA_COLOR_BAYER, useLogSampling, samplingFactor, 10.0);
		}
		else// -> else allocate "classical" retina :
			myRetina = cv::bioinspired::Retina::create(src.size());

		// save default retina parameters file in order to let you see this and maybe modify it and reload using method "setup"
		myRetina->write("RetinaDefaultParameters.xml");

		// load parameters if file exists
		myRetina->setup("RetinaSpecificParameters.xml");
		myRetina->clearBuffers();
	}
	void Run()
	{
		try
		{
			// run retina filter
			myRetina->run(src);
			myRetina->getParvo(retinaOutput_parvo);
			myRetina->getMagno(retinaOutput_magno);
		}
		catch (const cv::Exception& e)
		{
			std::cerr << "Error using Retina : " << e.what() << std::endl;
		}
	}
};

extern "C" __declspec(dllexport)
Retina *Retina_Basics_Open(int rows, int cols, int useLogSampling, float samplingFactor)
{
	Retina *RetinaPtr = new Retina(rows, cols, useLogSampling, samplingFactor);
	return RetinaPtr;
}

extern "C" __declspec(dllexport)
void Retina_Basics_Close(Retina *RetinaPtr)
{
	delete RetinaPtr;
}

extern "C" __declspec(dllexport)
int *Retina_Basics_Run(Retina *RetinaPtr, int *rgbPtr, int rows, int cols, int *magno)
{
	RetinaPtr->src = Mat(rows, cols, CV_8UC3, rgbPtr);
	RetinaPtr->Run();
	if (RetinaPtr->useLogSampling)
		memcpy(magno, RetinaPtr->retinaOutput_magno.data, (int)(rows * cols / (RetinaPtr->samplingFactor * RetinaPtr->samplingFactor)));
	else
		memcpy(magno, RetinaPtr->retinaOutput_magno.data, rows * cols);
	return (int *)RetinaPtr->retinaOutput_parvo.data;
}


