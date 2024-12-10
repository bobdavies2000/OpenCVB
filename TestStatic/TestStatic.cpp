// TestStatic.cpp : Defines the functions for the static library.
//

#include "pch.h"
#include "framework.h"
#include <opencv2/opencv.hpp>
#include "../CPP_Managed/PragmaLibs.h"

using namespace std;

int k = 1;
extern "C" __declspec(dllexport)
int * fnTestStatic()
{
	std::cout << "OpenCV Version: " << CV_VERSION << std::endl;
	return &k;
}
