#include <cstdlib>
#include <cstdio>
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
#include <opencv2/calib3d.hpp>

using namespace std;
using namespace  cv;

#define BLUR_RADIUS 3
#define PATHS_PER_SCAN 8
#define SMALL_PENALTY 3
#define LARGE_PENALTY 20
#define DEBUG false
#define window_height 9
#define window_width 9

struct path {
	short rowDiff;
	short colDiff;
	short index;
};

struct limits
{
	int start_pt_x; int start_pt_y;
	int end_pt_x; int end_pt_y;
	int direction_x; int direction_y;
};

vector<limits> paths;

void init_paths(int image_height, int image_width)
{
	//8 paths from center pixel based on change in X and Y coordinates
	for (int i = 0; i < PATHS_PER_SCAN; i++)
	{
		paths.push_back(limits());
	}
	for (int i = 0; i < PATHS_PER_SCAN; i++)
	{
		switch (i)
		{
		case 1:
			paths[i].direction_x = 0;
			paths[i].direction_y = 1;
			paths[i].start_pt_y = window_width / 2;
			paths[i].end_pt_y = image_width - window_width / 2;
			paths[i].start_pt_x = window_height / 2;
			paths[i].end_pt_x = image_height - window_height / 2;
			break;

		case 3:
			paths[i].direction_x = 0;
			paths[i].direction_y = -1;
			paths[i].start_pt_y = image_width - window_width / 2;
			paths[i].end_pt_y = window_width / 2;
			paths[i].start_pt_x = window_height / 2;
			paths[i].end_pt_x = image_height - window_height / 2;
			break;

		case 5:
			paths[i].direction_x = 1;
			paths[i].direction_y = -1;
			paths[i].start_pt_y = image_width - window_width / 2;
			paths[i].end_pt_y = window_width / 2;
			paths[i].start_pt_x = window_height / 2;
			paths[i].end_pt_x = image_height - window_height / 2;
			break;

		case 7:
			paths[i].direction_x = -1;
			paths[i].direction_y = -1;
			paths[i].start_pt_y = image_width - window_width / 2;
			paths[i].end_pt_y = window_width / 2;
			paths[i].start_pt_x = image_height - window_height / 2;
			paths[i].end_pt_x = window_height / 2;
			break;


		case 0:
			paths[i].direction_x = 1;
			paths[i].direction_y = 0;
			paths[i].start_pt_y = window_width / 2;
			paths[i].end_pt_y = image_width - window_width / 2;
			paths[i].start_pt_x = window_height / 2;
			paths[i].end_pt_x = image_height - window_height / 2;
			break;

		case 2:
			paths[i].direction_x = -1;
			paths[i].direction_y = 0;
			paths[i].start_pt_y = window_width / 2;
			paths[i].end_pt_y = image_width - window_width / 2;
			paths[i].start_pt_x = image_height - window_height / 2;
			paths[i].end_pt_x = window_height / 2;
			break;

		case 4:
			paths[i].direction_x = 1;
			paths[i].direction_y = 1;
			paths[i].start_pt_y = window_width / 2;
			paths[i].end_pt_y = image_width - window_width / 2;
			paths[i].start_pt_x = window_height / 2;
			paths[i].end_pt_x = image_height - window_height / 2;
			break;

		case 6:
			paths[i].direction_x = -1;
			paths[i].direction_y = 1;
			paths[i].start_pt_y = window_width / 2;
			paths[i].end_pt_y = image_width - window_width / 2;
			paths[i].start_pt_x = image_height - window_height / 2;
			paths[i].end_pt_x = window_height / 2;
			break;

		default:
			cout << "More paths or this is not possible" << endl;
			break;

		}
	}
}

void calculateCostHamming(cv::Mat& firstImage, cv::Mat& secondImage, int disparityRange, unsigned long*** C, unsigned long*** S)
{
	unsigned long census_left = 0;
	unsigned long census_right = 0;
	unsigned int bit = 0;

	const int bit_field = window_width * window_height - 1;
	int i, j, x, y;
	int d = 0;
	int shiftCount = 0;
	int image_height = int(firstImage.rows);
	int image_width = int(firstImage.cols);


	init_paths(image_height, image_width);

	Size imgSize = firstImage.size();
	Mat imgTemp_left = Mat::zeros(imgSize, CV_8U);
	Mat imgTemp_right = Mat::zeros(imgSize, CV_8U);
	Mat disparityMapstage1 = Mat(Size(firstImage.cols, firstImage.rows), CV_8UC1, Scalar::all(0));


	static long census_vleft[720][1280];
	static long census_vright[720][1280];

	for (x = window_height / 2; x < image_height - window_height / 2; x++)
	{
		for (y = window_width / 2; y < image_width - window_width / 2; y++)
		{
			census_left = 0;
			shiftCount = 0;
			int bit_counter = 0;
			int census_array_left[bit_field];
			for (i = x - window_height / 2; i <= x + window_height / 2; i++)
			{
				for (j = y - window_width / 2; j <= y + window_width / 2; j++)
				{

					if (shiftCount != window_width * window_height / 2)//skip the center pixel
					{
						census_left <<= 1;
						if (firstImage.at<uchar>(i, j) < firstImage.at<uchar>(x, y))//compare pixel values in the neighborhood
							bit = 1;
						else
							bit = 0;
						census_left = census_left | bit;
						census_array_left[bit_counter] = bit; bit_counter++;
					}
					shiftCount++;
				}
			}

			imgTemp_left.ptr<uchar>(x)[y] = (uchar)census_left;
			census_vleft[x][y] = census_left;



			census_right = 0;
			shiftCount = 0;
			bit_counter = 0;
			int census_array_right[bit_field];
			for (i = x - window_height / 2; i <= x + window_height / 2; i++)
			{
				for (j = y - window_width / 2; j <= y + window_width / 2; j++)
				{
					if (shiftCount != window_width * window_height / 2)//skip the center pixel
					{
						census_right <<= 1;
						if (secondImage.at<uchar>(i, j) < secondImage.at<uchar>(x, y))//compare pixel values in the neighborhood
							bit = 1;
						else
							bit = 0;
						census_right = census_right | bit;
						census_array_right[bit_counter] = bit; bit_counter++;
					}
					shiftCount++;

				}
			}

			imgTemp_right.ptr<uchar>(x)[y] = (uchar)census_right;
			census_vright[x][y] = census_right;
		}

	}

	for (x = window_height / 2; x < image_height - window_height / 2; x++)
	{
		for (y = window_width / 2; y < image_width - window_width / 2; y++)
		{
			for (int d = 0; d < disparityRange; d++)
			{
				int census_left = 0;
				int  census_right = 0;
				shiftCount = 0;
				int bit_counter = 0;
				census_left = census_vleft[x][y];
				if (y + d < image_width - window_width / 2)
					census_right = census_vright[x][y + d];
				else census_right = census_vright[x][y - disparityRange + d];
				long answer = (long)(census_left ^ census_right); //Hamming Distance
				short dist = 0;
				while (answer)
				{
					++dist;
					answer &= answer - 1;
				}
				C[x][y][d] = dist;
			}
		}
	}

	for (int row = 0; row < firstImage.rows; ++row)
	{
		for (int col = 0; col < firstImage.cols; ++col)
		{
			unsigned long smallest_cost = C[row][col][0];
			long smallest_disparity = 0;
			for (d = disparityRange - 1; d >= 0; d--)
			{
				if (C[row][col][d] < smallest_cost)
				{
					smallest_cost = C[row][col][d];
					smallest_disparity = d;
				}
			}

			disparityMapstage1.at<uchar>(row, col) = (uchar)(smallest_disparity * 255.0 / disparityRange); //Least cost Disparity
		}
	}
}

void disprange_aggregation(int disparityRange, unsigned long*** C, unsigned int**** A, long unsigned last_aggregated_k, int direction_x, int direction_y, int curx, int cury, int current_path, int width, int height)
{
	long unsigned last_aggregated_i = C[curx][cury][0];

	for (int d = 0; d < disparityRange; d++)
	{
		long unsigned term_1 = A[current_path][curx - direction_x][cury - direction_y][0];
		long unsigned term_2 = term_1;
		if (cury == window_width / 2 || cury == width - window_width / 2 || curx == window_height / 2 || curx == height - window_height / 2)
		{
			A[current_path][curx][cury][d] = C[curx][cury][d];
		}
		else
		{
			term_1 = A[current_path][curx - direction_x][cury - direction_y][d];
			if (d == 0)
				term_2 = A[current_path][curx - direction_x][cury - direction_y][d + 1] + SMALL_PENALTY;

			else if (d == disparityRange - 1)
				term_2 = A[current_path][curx - direction_x][cury - direction_y][d - 1] + SMALL_PENALTY;
			else
				term_2 = min(A[current_path][curx - direction_x][cury - direction_y][d - 1] + SMALL_PENALTY,
					A[current_path][curx - direction_x][cury - direction_y][d + 1] + SMALL_PENALTY);
			for (int pdisp = 0; pdisp < disparityRange; pdisp++)
			{

				if ((A[current_path][curx][cury - direction_y][pdisp] + LARGE_PENALTY) < term_1)
					term_1 = A[current_path][curx - direction_x][cury - direction_y][pdisp] + LARGE_PENALTY;
			}
			A[current_path][curx][cury][d] = C[curx][cury][d] + min(term_1, term_2) - last_aggregated_k;
		}
		if (A[current_path][curx][cury][d] < last_aggregated_i)
			last_aggregated_i = A[current_path][curx][cury][d];

	}
	last_aggregated_k = last_aggregated_i;
}


void aggregation(cv::Mat& leftImage, cv::Mat& rightImage, int disparityRange, unsigned long*** C, unsigned long*** S, unsigned int**** A)
{
	for (int ch_path = 0; ch_path < PATHS_PER_SCAN; ++ch_path)

	{
		long unsigned last_aggregated_k = 0;

		if (ch_path % 2 != 0)
		{
			int dirx = paths[ch_path].direction_x;
			int diry = paths[ch_path].direction_y;
			int next_dim = 0;
			if (dirx == 0)
				next_dim = 1;
			else
				next_dim = dirx;
			for (int x = paths[ch_path].start_pt_x; x != paths[ch_path].end_pt_x; x += next_dim)
			{
				for (int y = paths[ch_path].start_pt_y; (y != paths[ch_path].end_pt_y); y += diry)
				{
					disprange_aggregation(disparityRange, C, A, last_aggregated_k, dirx, diry, x, y, ch_path, leftImage.cols, leftImage.rows);
				}
				std::this_thread::sleep_for(std::chrono::microseconds(300));
			}
		}


		else if (ch_path % 2 == 0)
		{
			int dirx = paths[ch_path].direction_x;
			int diry = paths[ch_path].direction_y;
			int next_dim = 0;
			if (diry == 0)
				next_dim = 1;
			else
				next_dim = diry;
			for (int y = paths[ch_path].start_pt_y; y != paths[ch_path].end_pt_y; y += next_dim)
			{
				for (int x = paths[ch_path].start_pt_x; (x != paths[ch_path].end_pt_x); x += dirx)
				{
					disprange_aggregation(disparityRange, C, A, last_aggregated_k, dirx, diry, x, y, ch_path, leftImage.cols, leftImage.rows);
				}
				std::this_thread::sleep_for(std::chrono::microseconds(300));
			}

		}
	}

	for (int row = 0; row < leftImage.rows; ++row)
	{
		for (int col = 0; col < leftImage.cols; ++col)
		{
			for (int d = 0; d < disparityRange; d++)
			{
				for (int path = 0; path < PATHS_PER_SCAN; path++)
					S[row][col][d] += A[path][row][col][d]; //Aggregation
			}
		}
	}

}

void computeDisparity(int disparityRange, int rows, int cols, unsigned long*** S, Mat disparityMapstage2)
{
	for (int row = 0; row < rows; ++row)
	{
		for (int col = 0; col < cols; ++col)
		{
			unsigned long smallest_cost = S[row][col][0];
			int smallest_disparity = 0;
			for (int d = disparityRange - 1; d >= 0; d--)
			{

				if (S[row][col][d] < smallest_cost)
				{
					smallest_cost = S[row][col][d];
					smallest_disparity = d; //Least cost disparity after Aggregation

				}
			}

			disparityMapstage2.at<uchar>(row, col) = (uchar)(smallest_disparity * 255.0 / disparityRange);

		}
	}
}





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





extern "C" __declspec(dllexport)
SemiGlobalMatching *SemiGlobalMatching_Open(int rows, int cols, int disparityRange)
{
  SemiGlobalMatching *SemiGlobalMatchingPtr = new SemiGlobalMatching(rows, cols, disparityRange);
  return SemiGlobalMatchingPtr;
}

extern "C" __declspec(dllexport)
int * SemiGlobalMatching_Close(SemiGlobalMatching *SemiGlobalMatchingPtr)
{
  delete SemiGlobalMatchingPtr;
  return (int*)0;
}

// https://github.com/epiception/SGM-Census
extern "C" __declspec(dllexport)
int *SemiGlobalMatching_Run(SemiGlobalMatching *SemiGlobalMatchingPtr, int *leftPtr, int *rightPtr, int rows, int cols)
{
	SemiGlobalMatchingPtr->leftImage = Mat(rows, cols, CV_8U, leftPtr);
	SemiGlobalMatchingPtr->rightImage = Mat(rows, cols, CV_8U, leftPtr);
	SemiGlobalMatchingPtr->Run();

	return (int *) SemiGlobalMatchingPtr->disparityMapstage2.data; 
}

