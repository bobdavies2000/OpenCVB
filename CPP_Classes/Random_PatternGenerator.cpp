#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <random>
#include <map>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/ccalib/randpattern.hpp"

using namespace std;
using namespace  cv;
class Random_PatternGenerator
{
private:
public:
	Mat pattern;
    Random_PatternGenerator(){}
    void Run() {}
};

extern "C" __declspec(dllexport)
Random_PatternGenerator *Random_PatternGenerator_Open() {
    Random_PatternGenerator *Random_PatternGeneratorPtr = new Random_PatternGenerator();
    return Random_PatternGeneratorPtr;
}

extern "C" __declspec(dllexport)
int *Random_PatternGenerator_Close(Random_PatternGenerator * rPtr)
{
    delete rPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int *Random_PatternGenerator_Run(Random_PatternGenerator *rPtr, int rows, int cols)
{
	randpattern::RandomPatternGenerator generator(cols, rows);
	generator.generatePattern();
	rPtr->pattern = generator.getPattern();
	return (int *)rPtr->pattern.data; 
}








// https://en.cppreference.com/w/cpp/numeric/random/discrete_distribution
class Random_DiscreteDistribution
{
private:
	std::random_device rd;
public:
	Mat discrete;
	Random_DiscreteDistribution() {}
	void Run() 
	{
		std::mt19937 gen(rd());
		std::map<int, int> m;
		std::discrete_distribution<> d({ 40, 10, 10, 40 });
		for (int n = 0; n < 10000; ++n) {
			++m[d(gen)];
		}
		for (auto p : m) {
			std::cout << p.first << " generated " << p.second << " times\n";
		}
	}
};

extern "C" __declspec(dllexport)
Random_DiscreteDistribution * Random_DiscreteDistribution_Open() {
	Random_DiscreteDistribution* cPtr = new Random_DiscreteDistribution();
	return cPtr;
}

extern "C" __declspec(dllexport)
int *Random_DiscreteDistribution_Close(Random_DiscreteDistribution * cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Random_DiscreteDistribution_Run(Random_DiscreteDistribution * cPtr, int rows, int cols)
{
	cPtr->discrete = Mat(rows, cols, CV_32F, 0);
	return (int*)cPtr->discrete.data;
}