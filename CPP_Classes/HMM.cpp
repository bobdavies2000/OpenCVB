#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "cvHmm.h"


using namespace std;
using namespace  cv;
class HMM
{
private:
public:
	Mat src, output;
	cv::Mat Transition, Emission, initialProbabilities;
	HMM() {}
	void sampleRun()
	{
		std::stringstream buffer;
		buffer << "First we define Transition, Emission and Initial Probabilities of the model\n";
		printf(buffer.str().c_str()); buffer.clear();
		double TRANSdata[] = { 0.5, 0.5, 0.0,
							   0.0, 0.7, 0.3,
							   0.0, 0.0, 1.0 };
		Transition = cv::Mat(3, 3, CV_64F, TRANSdata).clone();
		double EMISdata[] = { 2.0 / 4.0 , 2.0 / 4.0 , 0.0 / 4.0 , 0.0 / 4.0 ,
							  0.0 / 4.0 , 2.0 / 4.0 , 2.0 / 4.0 , 0.0 / 4.0 ,
							  0.0 / 4.0 , 0.0 / 4.0 , 2.0 / 4.0 , 2.0 / 4.0 };
		Emission = cv::Mat(3, 4, CV_64F, EMISdata).clone();
		double INITdata[] = { 1.0  , 0.0 , 0.0 };
		initialProbabilities = cv::Mat(1, 3, CV_64F, INITdata).clone();
		CvHMM hmm;
		hmm.printModel(Transition, Emission, initialProbabilities);



		//----------------------------------------------------------------------------------
		buffer << "\nAs an example, we generate 25 sequences each with 20 observations\nper sequence using the defined Markov model\n";
		printf(buffer.str().c_str()); buffer.clear();
		srand((unsigned int)time(NULL));
		cv::Mat seq, states;
		hmm.generate(20, 25, Transition, Emission, initialProbabilities, seq, states);
		buffer << "\nGenerated Sequences:\n";
		for (int i = 0; i < seq.rows; i++)
		{
			buffer << i << ": ";
			for (int j = 0; j < seq.cols; j++)
				buffer << seq.at<int>(i, j);
			buffer << "\n";
		}
		buffer << "\nGenerated States:\n";
		for (int i = 0; i < seq.rows; i++)
		{
			buffer << i << ": ";
			for (int j = 0; j < seq.cols; j++)
				buffer << states.at<int>(i, j);
			buffer << "\n";
		}
		buffer << "\n";
		//----------------------------------------------------------------------------------
		buffer << "\nProblem 1: Given the observation sequence and a model,\n";
		buffer << "how do we efficiently compute P(O|Y), the probability of\n";
		buffer << "the observation sequence, given the model?\n";
		buffer << "Example: To demonstrate this we estimate the probabilities\n";
		buffer << "for all sequences, given the defined model above.\n";
		cv::Mat pstates, forward, backward;
		double logpseq;
		buffer << "\n";
		for (int i = 0; i < seq.rows; i++)
		{
			hmm.decode(seq.row(i), Transition, Emission, initialProbabilities, logpseq, pstates, forward, backward);
			buffer << "logpseq" << i << " " << logpseq << "\n";
		}
		buffer << "\n";
		//----------------------------------------------------------------------------------
		buffer << "\nProblem 2: Given the model and an observation sequence,\n";
		buffer << "how do we find an optimal state sequence for the underlying\n";
		buffer << "Markov Process? One answer is by using Viterbi algorithm.\n";
		buffer << "As an example here we estimate the optimal states for all sequences\n";
		buffer << "using Viterbi algorithm and the defined model.\n";
		cv::Mat estates;
		buffer << "\n";
		for (int i = 0; i < seq.rows; i++)
		{
			buffer << i << ": ";
			hmm.viterbi(seq.row(i), Transition, Emission, initialProbabilities, estates);
			for (int i = 0; i < estates.cols; i++)
				buffer << estates.at<int>(0, i);
			buffer << "\n";
		}
		buffer << "\n";
		//----------------------------------------------------------------------------------
		buffer << "\nProblem 3: Given an observation sequence O (can be several observations),\n";
		buffer << "how do we find a model that maximizes the probability of O ?\n";
		buffer << "The answer is by using the Baum-Welch algorithm to train a model.\n";
		buffer << "To demonstrate this, initially we define a model by guess\n";
		buffer << "and we estimate the parameters of the model for all the sequences\n";
		buffer << "that we already got.\n";
		double TRGUESSdata[] = { 2.0 / 3.0 , 1.0 / 3.0 , 0.0 / 3.0,
								0.0 / 3.0 , 2.0 / 3.0 , 1.0 / 3.0,
								0.0 / 3.0 , 0.0 / 3.0 , 3.0 / 3.0 };
		cv::Mat TRGUESS = cv::Mat(3, 3, CV_64F, TRGUESSdata).clone();
		double EMITGUESSdata[] = { 1.0 / 4.0 , 1.0 / 4.0 , 1.0 / 4.0 , 1.0 / 4.0 ,
								  1.0 / 4.0 , 1.0 / 4.0 , 1.0 / 4.0 , 1.0 / 4.0 ,
								  1.0 / 4.0 , 1.0 / 4.0 , 1.0 / 4.0 , 1.0 / 4.0 };
		cv::Mat EMITGUESS = cv::Mat(3, 4, CV_64F, EMITGUESSdata).clone();
		double INITGUESSdata[] = { 0.6  , 0.2 , 0.2 };
		cv::Mat INITGUESS = cv::Mat(1, 3, CV_64F, INITGUESSdata).clone();
		hmm.train(seq, 100, TRGUESS, EMITGUESS, INITGUESS);
		printf(buffer.str().c_str());
		hmm.printModel(TRGUESS, EMITGUESS, INITGUESS);
		//----------------------------------------------------------------------------------
		buffer << "\ndone.\n";
	}
	void Run() {
		output = src.clone();
		static int testCount = 0;
		if (testCount++ % 100 == 0) sampleRun();
	}
};

extern "C" __declspec(dllexport)
HMM* HMM_Open() {
	HMM* cPtr = new HMM();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* HMM_Close(HMM* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* HMM_Run(HMM* cPtr, int* bgrPtr, int rows, int cols, int channels)
{
	cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, bgrPtr);
	cPtr->Run();
	return (int*)cPtr->output.data;
}