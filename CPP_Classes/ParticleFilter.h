#pragma once
#include <iostream>
#include <algorithm>
#include <vector>
#include <cmath>
#include <ctime>
#include <thread>
#include <chrono>

#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace  cv; 

class ParticleFilter
{
	// Particles
	Mat Xn;

	// Weights
	Mat Wn;

	// Process noise
	Mat R;

	// state transition model
	Mat A;

	// measurement model
	Mat H;

	// dimensions of the state vector
	unsigned int Ds;

	// number of particles
	unsigned int N;

	// redistributions threshold
	unsigned int N_threshold;
	
public:
	ParticleFilter(void);
	ParticleFilter(const Mat &inZ, const Mat &inR, unsigned int inN=1000);
	void resampleParticles();
	void predict();
	void update(const Mat &inZ);
	Mat currentPrediction();
	Mat showParticles(const Mat &inImage);
	Mat showPredictedLocation(const Mat &inImage);
	~ParticleFilter(void);
	
private:   // helper function
	void initParticles(Mat &lR, Mat &uR);
	void selectDynamicModel(unsigned int D);
	void normalizeWeights();
	void weightingParticles(const Mat &inZ);
	double distanceGaussian(const Mat &inZ,  const Mat &pZt);
	Mat resampler(const Mat &inWn);
};

