#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include <string>
#include <opencv2/ml.hpp>
#include "time.h"
#include <iomanip> 
#include <sstream>
#include <random>
#include <intrin.h>
#include "OpenCVB_Extern.h"

using namespace std;
using namespace  cv;

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

VB_EXTERN
CitySolver *Annealing_Basics_Open(Point2f *cityPositions, int count)
{
	CitySolver *cPtr = new CitySolver();
	cPtr->cityPositions.assign(cityPositions, cityPositions + count);
	return cPtr;
}

VB_EXTERN
int * Annealing_Basics_Close(CitySolver *cPtr)
{
	delete cPtr;
	return (int*)0;
}

VB_EXTERN
char *Annealing_Basics_Run(CitySolver *cPtr, int *cityOrder, int count)
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


