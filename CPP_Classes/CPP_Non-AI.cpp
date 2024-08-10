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

/*****************************************************************************
FILE       : Dither.h
Author     : Svetoslav Chekanov
Description: Collection of dithering algorithms

Copyright (c) 2014 Brosix
*****************************************************************************/
/* #    Revisions    # */

#include <iostream>
#include <cstdlib>

/////////////////////////////////////////////////////////////////////////////
//	Macroses used in dithering functions
/////////////////////////////////////////////////////////////////////////////
typedef	unsigned char	BYTE;
#ifndef MIN
#define MIN(a,b)            (((a) < (b)) ? (a) : (b))
#endif

#ifndef MAX
#define MAX(a,b)            (((a) > (b)) ? (a) : (b))
#endif

#ifndef	CLAMP
//	This produces faster code without jumps
#define		CLAMP( x, xmin, xmax )		(x)	= MAX( (xmin), (x) );	\
										(x)	= MIN( (xmax), (x) )
#define		CLAMPED( x, xmin, xmax )	MAX( (xmin), MIN( (xmax), (x) ) )
#endif

#define	GRAY( r,g,b )	(((r) + (g) + (b)) / 3)

typedef	int	pixel;

/////////////////////////////////////////////////////////////////////////////


/////////////////////////////////////////////////////////////////////////////
//	Color discretization arrays
/////////////////////////////////////////////////////////////////////////////

const BYTE	VALUES_6BPP[] = { 0,  85, 170, 255 };
const BYTE	VALUES_9BPP[] = { 0,  36,  72, 108, 144, 180, 216, 255 };
const BYTE	VALUES_12BPP[] = { 0,  17,  34,  51,  68,  85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255 };
const BYTE	VALUES_15BPP[] = { 0,   8,  16,  24,  32,  40,  48,  56,  64,  80,  88,  96, 104, 112, 120, 128, 136, 144, 152, 160, 168, 176, 184, 192, 200, 208, 216, 224, 232, 240, 248, 255 };
const BYTE	VALUES_18BPP[] = { 0,   4,   8,  12,  16,  20,  24,  28,  32,  36,  40,  44,  48,  52,  56,  60,  64,  68,  72,  76,  80,  84,  88,  92,  96, 100, 104, 108, 112, 116, 120, 126, 130, 136, 140, 144, 148, 152, 156, 160, 164, 168, 172, 176, 180, 184, 188, 192, 196, 200, 204, 208, 212, 216, 220, 224, 228, 232, 236, 240, 244, 248, 252, 255 };


//	Ordered dither matrices
const	int BAYER_PATTERN_2X2[2][2] = {	//	2x2 Bayer Dithering Matrix. Color levels: 5
												{	 51, 206	},
												{	153, 102	}
};

const	int BAYER_PATTERN_3X3[3][3] = {	//	3x3 Bayer Dithering Matrix. Color levels: 10
												{	 181, 231, 131	},
												{	  50,  25, 100	},
												{	 156,  75, 206	}
};

const	int BAYER_PATTERN_4X4[4][4] = {	//	4x4 Bayer Dithering Matrix. Color levels: 17
												{	 15, 195,  60, 240	},
												{	135,  75, 180, 120	},
												{	 45, 225,  30, 210	},
												{	165, 105, 150,  90	}

};



const	int BAYER_PATTERN_8X8[8][8] = {	//	8x8 Bayer Dithering Matrix. Color levels: 65
												{	  0, 128,  32, 160,   8, 136,  40, 168	},
												{	192,  64, 224,  96, 200,  72, 232, 104	},
												{	 48, 176,  16, 144,  56, 184,  24, 152	},
												{	240, 112, 208,  80, 248, 120, 216,  88	},
												{	 12, 140,  44, 172,   4, 132,  36, 164	},
												{	204,  76, 236, 108, 196,  68, 228, 100	},
												{	 60, 188,  28, 156,  52, 180,  20, 148	},
												{	252, 124, 220,  92, 244, 116, 212,  84	}
};

const	int	BAYER_PATTERN_16X16[16][16] = {	//	16x16 Bayer Dithering Matrix.  Color levels: 256
												{	  0, 191,  48, 239,  12, 203,  60, 251,   3, 194,  51, 242,  15, 206,  63, 254	},
												{	127,  64, 175, 112, 139,  76, 187, 124, 130,  67, 178, 115, 142,  79, 190, 127	},
												{	 32, 223,  16, 207,  44, 235,  28, 219,  35, 226,  19, 210,  47, 238,  31, 222	},
												{	159,  96, 143,  80, 171, 108, 155,  92, 162,  99, 146,  83, 174, 111, 158,  95	},
												{	  8, 199,  56, 247,   4, 195,  52, 243,  11, 202,  59, 250,   7, 198,  55, 246	},
												{	135,  72, 183, 120, 131,  68, 179, 116, 138,  75, 186, 123, 134,  71, 182, 119	},
												{	 40, 231,  24, 215,  36, 227,  20, 211,  43, 234,  27, 218,  39, 230,  23, 214	},
												{	167, 104, 151,  88, 163, 100, 147,  84, 170, 107, 154,  91, 166, 103, 150,  87	},
												{	  2, 193,  50, 241,  14, 205,  62, 253,   1, 192,  49, 240,  13, 204,  61, 252	},
												{	129,  66, 177, 114, 141,  78, 189, 126, 128,  65, 176, 113, 140,  77, 188, 125	},
												{	 34, 225,  18, 209,  46, 237,  30, 221,  33, 224,  17, 208,  45, 236,  29, 220	},
												{	161,  98, 145,  82, 173, 110, 157,  94, 160,  97, 144,  81, 172, 109, 156,  93	},
												{	 10, 201,  58, 249,   6, 197,  54, 245,   9, 200,  57, 248,   5, 196,  53, 244	},
												{	137,  74, 185, 122, 133,  70, 181, 118, 136,  73, 184, 121, 132,  69, 180, 117	},
												{	 42, 233,  26, 217,  38, 229,  22, 213,  41, 232,  25, 216,  37, 228,  21, 212	},
												{	169, 106, 153,  90, 165, 102, 149,  86, 168, 105, 152,  89, 164, 101, 148,  85	}
};
//	Ordered dither using matrix
extern "C" __declspec(dllexport)
void ditherBayer16(BYTE* pixels, int width, int height)	
{
	int	col = 0;
	int	row = 0;

	for (int y = 0; y < height; y++)
	{
		row = y & 15;	//	y % 16

		for (int x = 0; x < width; x++)
		{
			col = x & 15;	//	x % 16

			const pixel	blue = pixels[x * 3 + 0];
			const pixel	green = pixels[x * 3 + 1];
			const pixel	red = pixels[x * 3 + 2];

			pixel	color = ((red + green + blue) / 3 < BAYER_PATTERN_16X16[col][row] ? 0 : 255);

			pixels[x * 3 + 0] = color;	//	blue
			pixels[x * 3 + 1] = color;	//	green
			pixels[x * 3 + 2] = color;	//	red
		}

		pixels += width * 3;
	}
}
extern "C" __declspec(dllexport)
void ditherBayer8(BYTE* pixels, int width, int height)	
{
	int	col = 0;
	int	row = 0;

	for (int y = 0; y < height; y++)
	{
		row = y & 7;		//	% 8;

		for (int x = 0; x < width; x++)
		{
			col = x & 7;	//	% 8;

			const pixel	blue = pixels[x * 3 + 0];
			const pixel	green = pixels[x * 3 + 1];
			const pixel	red = pixels[x * 3 + 2];

			pixel	color = ((red + green + blue) / 3 < BAYER_PATTERN_8X8[col][row] ? 0 : 255);

			pixels[x * 3 + 0] = color;	//	blue
			pixels[x * 3 + 1] = color;	//	green
			pixels[x * 3 + 2] = color;	//	red
		}

		pixels += width * 3;
	}
}
extern "C" __declspec(dllexport)
void ditherBayer4(BYTE* pixels, int width, int height)	
{
	int	col = 0;
	int	row = 0;

	for (int y = 0; y < height; y++)
	{
		row = y & 3;	//	% 4

		for (int x = 0; x < width; x++)
		{
			col = x & 3;	//	% 4

			const pixel	blue = pixels[x * 3 + 0];
			const pixel	green = pixels[x * 3 + 1];
			const pixel	red = pixels[x * 3 + 2];

			pixel	color = ((red + green + blue) / 3 < BAYER_PATTERN_4X4[col][row] ? 0 : 255);

			pixels[x * 3 + 0] = color;	//	blue
			pixels[x * 3 + 1] = color;	//	green
			pixels[x * 3 + 2] = color;	//	red
		}

		pixels += width * 3;
	}
}
extern "C" __declspec(dllexport)
void ditherBayer3(BYTE* pixels, int width, int height)	
{
	int	col = 0;
	int	row = 0;

	for (int y = 0; y < height; y++)
	{
		row = y % 3;

		for (int x = 0; x < width; x++)
		{
			col = x % 3;

			const pixel	blue = pixels[x * 3 + 0];
			const pixel	green = pixels[x * 3 + 1];
			const pixel	red = pixels[x * 3 + 2];

			pixel	color = ((red + green + blue) / 3 < BAYER_PATTERN_3X3[col][row] ? 0 : 255);

			pixels[x * 3 + 0] = color;	//	blue
			pixels[x * 3 + 1] = color;	//	green
			pixels[x * 3 + 2] = color;	//	red
		}

		pixels += width * 3;
	}
}
extern "C" __declspec(dllexport)
void ditherBayer2(BYTE* pixels, int width, int height)	
{
	int	col = 0;
	int	row = 0;

	for (int y = 0; y < height; y++)
	{
		row = y & 1;	//	y % 2

		for (int x = 0; x < width; x++)
		{
			col = x & 1;	//	x % 2

			const pixel	blue = pixels[x * 3 + 0];
			const pixel	green = pixels[x * 3 + 1];
			const pixel	red = pixels[x * 3 + 2];

			pixel	color = ((red + green + blue) / 3 < BAYER_PATTERN_2X2[col][row] ? 0 : 255);

			pixels[x * 3 + 0] = color;	//	blue
			pixels[x * 3 + 1] = color;	//	green
			pixels[x * 3 + 2] = color;	//	red
		}

		pixels += width * 3;
	}
}
//	Colored Ordered dither, using 16x16 matrix (dither is applied on all color planes (r,g,b)
//	This is the ultimate method for Bayer Ordered Dither with 16x16 matrix
//	ncolors - number of colors diapazons to use. Valid values 0..255, but interesed are 0..40
//	1		- color (1 bit per color plane,  3 bits per pixel)
//	3		- color (2 bit per color plane,  6 bits per pixel)
//	7		- color (3 bit per color plane,  9 bits per pixel)
//	15		- color (4 bit per color plane, 12 bits per pixel)
//	31		- color (5 bit per color plane, 15 bits per pixel)
extern "C" __declspec(dllexport)
void ditherBayerRgbNbpp(BYTE* pixels, int width, int height, int ncolors)	
{
	int	divider = 256 / ncolors;

	for (int y = 0; y < height; y++)
	{
		const int	row = y & 15;	//	y % 16

		for (int x = 0; x < width; x++)
		{
			const int	col = x & 15;	//	x % 16

			const int	t = BAYER_PATTERN_16X16[col][row];
			const int	corr = (t / ncolors);

			const int	blue = pixels[x * 3 + 0];
			const int	green = pixels[x * 3 + 1];
			const int	red = pixels[x * 3 + 2];

			int	i1 = (blue + corr) / divider;	CLAMP(i1, 0, ncolors);
			int	i2 = (green + corr) / divider;	CLAMP(i2, 0, ncolors);
			int	i3 = (red + corr) / divider;	CLAMP(i3, 0, ncolors);

			//	If you want to compress the image, use the values of i1,i2,i3
			//	they have values in the range 0..ncolors
			//	So if the ncolors is 4 - you have values: 0,1,2,3 which is encoded in 2 bits
			//	2 bits for 3 planes == 6 bits per pixel

			pixels[x * 3 + 0] = CLAMPED(i1 * divider, 0, 255);	//VALUES_6BPP[i1];	//	blue
			pixels[x * 3 + 1] = CLAMPED(i2 * divider, 0, 255);	//VALUES_6BPP[i2];	//	green
			pixels[x * 3 + 2] = CLAMPED(i3 * divider, 0, 255);	//VALUES_6BPP[i3];	//	red
		}

		pixels += width * 3;
	}
}
//	Color ordered dither using 3 bits per pixel (1 bit per color plane)
extern "C" __declspec(dllexport)
void ditherBayerRgb3bpp(BYTE* pixels, int width, int height)	
{
	int	col = 0;
	int	row = 0;

	for (int y = 0; y < height; y++)
	{
		row = y & 15;	//	y % 16

		for (int x = 0; x < width; x++)
		{
			col = x & 15;	//	x % 16

			pixels[x * 3 + 0] = (pixels[x * 3 + 0] > BAYER_PATTERN_16X16[col][row] ? 255 : 0);
			pixels[x * 3 + 1] = (pixels[x * 3 + 1] > BAYER_PATTERN_16X16[col][row] ? 255 : 0);
			pixels[x * 3 + 2] = (pixels[x * 3 + 2] > BAYER_PATTERN_16X16[col][row] ? 255 : 0);
		}

		pixels += width * 3;
	}
}
//	Color ordered dither using 6 bits per pixel (2 bit per color plane)
extern "C" __declspec(dllexport)
void ditherBayerRgb6bpp(BYTE* pixels, int width, int height)	
{
	for (int y = 0; y < height; y++)
	{
		const int	row = y & 15;	//	y % 16

		for (int x = 0; x < width; x++)
		{
			const int	col = x & 15;	//	x % 16

			const int	t = BAYER_PATTERN_16X16[col][row];
			const int	corr = (t / 3);

			const int	blue = pixels[x * 3 + 0];
			const int	green = pixels[x * 3 + 1];
			const int	red = pixels[x * 3 + 2];

			int	i1 = (blue + corr) / 85;	CLAMP(i1, 0, 3);
			int	i2 = (green + corr) / 85;	CLAMP(i2, 0, 3);
			int	i3 = (red + corr) / 85;	CLAMP(i3, 0, 3);

			//	If you want to compress the image, use the values of i1,i2,i3
			//	they have values in the range 0..3 which is encoded in 2 bits
			//	2 bits for 3 planes == 6 bits per pixel
			//	out.writeBit( i1 & 0x01 );
			//	out.writeBit( i1 & 0x02 );
			//	out.writeBit( i2 & 0x01 );
			//	out.writeBit( i2 & 0x02 );
			//	out.writeBit( i3 & 0x01 );
			//	out.writeBit( i3 & 0x02 );

			pixels[x * 3 + 0] = i1 * 85;	//VALUES_6BPP[i1];	//	blue
			pixels[x * 3 + 1] = i2 * 85;	//VALUES_6BPP[i2];	//	green
			pixels[x * 3 + 2] = i3 * 85;	//VALUES_6BPP[i3];	//	red
		}

		pixels += width * 3;
	}
}

/*Faster but slightly inaccurate version
void ditherBayerRgb6bpp( BYTE* pixels, int width, int height )	
{
	for( int y = 0; y < height; y++ )
	{
		const int	row	= y & 15;	//	y % 16

		for( int x = 0; x < width; x++ )
		{
			const int	col	= x & 15;	//	x % 16

			const int	t		= BAYER_PATTERN_16X16[col][row];
			const int	corr	= (t >> 2) - 32;

			const int	blue	= pixels[x * 3 + 0];
			const int	green	= pixels[x * 3 + 1];
			const int	red		= pixels[x * 3 + 2];

			int	i1	= (blue  + corr) >> 6;	CLAMP( i1, 0, 3 );
			int	i2	= (green + corr) >> 6;	CLAMP( i2, 0, 3 );
			int	i3	= (red   + corr) >> 6;	CLAMP( i3, 0, 3 );

			pixels[x * 3 + 0]	= VALUES_6BPP[i1];	//	blue
			pixels[x * 3 + 1]	= VALUES_6BPP[i2];	//	green
			pixels[x * 3 + 2]	= VALUES_6BPP[i3];	//	red
		}

		pixels	+= width * 3;
	}
}
*/
//	Color ordered dither using 9 bits per pixel (3 bit per color plane)
extern "C" __declspec(dllexport)
void ditherBayerRgb9bpp(BYTE* pixels, int width, int height)	
{
	for (int y = 0; y < height; y++)
	{
		int	row = y & 15;	//	y % 16

		for (int x = 0; x < width; x++)
		{
			int	col = x & 15;	//	x % 16

			const int	t = BAYER_PATTERN_16X16[col][row];
			const int	corr = (t / 7) - 2;	//	 -2 because: 256/7=36  36*7=252  256-252=4   4/2=2 - correction -2

			const int	blue = pixels[x * 3 + 0];
			const int	green = pixels[x * 3 + 1];
			const int	red = pixels[x * 3 + 2];

			int	i1 = (blue + corr) / 36;		CLAMP(i1, 0, 7);
			int	i2 = (green + corr) / 36;		CLAMP(i2, 0, 7);
			int	i3 = (red + corr) / 36;		CLAMP(i3, 0, 7);

			pixels[x * 3 + 0] = i1 * 36;	//VALUES_9BPP[i1];
			pixels[x * 3 + 1] = i2 * 36;	//VALUES_9BPP[i2];
			pixels[x * 3 + 2] = i3 * 36;	//VALUES_9BPP[i3];
		}

		pixels += width * 3;
	}
}

/*Faster but slightly inaccurate version
void ditherBayerRgb9bpp( BYTE* pixels, int width, int height )	
{
	for( int y = 0; y < height; y++ )
	{
		int	row	= y & 15;	//	y % 16

		BYTE*   prow   = pixels + ( y * width * 3 );

		for( int x = 0; x < width; x++ )
		{
			int	col	= x & 15;	//	x % 16

			int	t		= BAYER_PATTERN_16X16[col][row];
			int	corr	= (t >> 3) - 16;

			const int	blue	= prow[x * 3 + 0];
			const int	green	= prow[x * 3 + 1];
			const int	red		= prow[x * 3 + 2];

			int	i1	= (blue  + corr) >> 5;		CLAMP( i1, 0, 7 );
			int	i2	= (green + corr) >> 5;		CLAMP( i2, 0, 7 );
			int	i3	= (red   + corr) >> 5;		CLAMP( i3, 0, 7 );

			prow[x * 3 + 0]	= VALUES_9BPP[i1];
			prow[x * 3 + 1]	= VALUES_9BPP[i2];
			prow[x * 3 + 2]	= VALUES_9BPP[i3];
		}
	}
}
*/
//	Color ordered dither using 12 bits per pixel (4 bit per color plane)
extern "C" __declspec(dllexport)
void ditherBayerRgb12bpp(BYTE* pixels, int width, int height)	
{
	for (int y = 0; y < height; y++)
	{
		int	row = y & 15;	//	y % 16

		for (int x = 0; x < width; x++)
		{
			int	col = x & 15;	//	x % 16

			const int	t = BAYER_PATTERN_16X16[col][row];
			const int	corr = t / 15;

			const int	blue = pixels[x * 3 + 0];
			const int	green = pixels[x * 3 + 1];
			const int	red = pixels[x * 3 + 2];

			int	i1 = (blue + corr) / 17;		CLAMP(i1, 0, 15);
			int	i2 = (green + corr) / 17;		CLAMP(i2, 0, 15);
			int	i3 = (red + corr) / 17;		CLAMP(i3, 0, 15);

			pixels[x * 3 + 0] = i1 * 17;
			pixels[x * 3 + 1] = i2 * 17;
			pixels[x * 3 + 2] = i3 * 17;
		}

		pixels += width * 3;
	}
}

/*Faster but slightly inaccurate version
void ditherBayerRgb12bpp( BYTE* pixels, int width, int height )	
{
	for( int y = 0; y < height; y++ )
	{
		int	row	= y & 15;	//	y % 16

		BYTE*   prow   = pixels + ( y * width * 3 );

		for( int x = 0; x < width; x++ )
		{
			int	col	= x & 15;	//	x % 16

			int	t		= BAYER_PATTERN_16X16[col][row];
			int	corr	= (t >> 4) - 8;

			const int	blue	= prow[x * 3 + 0];
			const int	green	= prow[x * 3 + 1];
			const int	red		= prow[x * 3 + 2];

			int	i1	= (blue  + corr) >> 4;		CLAMP( i1, 0, 15 );
			int	i2	= (green + corr) >> 4;		CLAMP( i2, 0, 15 );
			int	i3	= (red   + corr) >> 4;		CLAMP( i3, 0, 15 );

			prow[x * 3 + 0]	= VALUES_12BPP[i1];
			prow[x * 3 + 1]	= VALUES_12BPP[i2];
			prow[x * 3 + 2]	= VALUES_12BPP[i3];
		}
	}
}
*/
//	Color ordered dither using 15 bits per pixel (5 bit per color plane)
extern "C" __declspec(dllexport)
void ditherBayerRgb15bpp(BYTE* pixels, int width, int height)	
{
	for (int y = 0; y < height; y++)
	{
		int	row = y & 15;	//	y % 16

		for (int x = 0; x < width; x++)
		{
			int	col = x & 15;	//	x % 16

			const int	t = BAYER_PATTERN_16X16[col][row];
			const int	corr = (t / 31);

			const int	blue = pixels[x * 3 + 0];
			const int	green = pixels[x * 3 + 1];
			const int	red = pixels[x * 3 + 2];

			int	i1 = (blue + corr) / 8;		CLAMP(i1, 0, 31);
			int	i2 = (green + corr) / 8;		CLAMP(i2, 0, 31);
			int	i3 = (red + corr) / 8;		CLAMP(i3, 0, 31);

			pixels[x * 3 + 0] = i1 * 8;
			pixels[x * 3 + 1] = i2 * 8;
			pixels[x * 3 + 2] = i3 * 8;
		}

		pixels += width * 3;
	}
}

/*Faster but slightly inaccurate version
void ditherBayerRgb15bpp( BYTE* pixels, int width, int height )	
{
	for( int y = 0; y < height; y++ )
	{
		int	row	= y & 15;	//	y % 16

		BYTE*   prow   = pixels + ( y * width * 3 );

		for( int x = 0; x < width; x++ )
		{
			int	col	= x & 15;	//	x % 16

			int	t		= BAYER_PATTERN_16X16[col][row];
			int	corr	= (t >> 5) - 4;

			const int	blue	= prow[x * 3 + 0];
			const int	green	= prow[x * 3 + 1];
			const int	red		= prow[x * 3 + 2];

			int	i1	= (blue  + corr) >> 3;		CLAMP( i1, 0, 31 );
			int	i2	= (green + corr) >> 3;		CLAMP( i2, 0, 31 );
			int	i3	= (red   + corr) >> 3;		CLAMP( i3, 0, 31 );

			prow[x * 3 + 0]	= VALUES_15BPP[i1];
			prow[x * 3 + 1]	= VALUES_15BPP[i2];
			prow[x * 3 + 2]	= VALUES_15BPP[i3];
		}
	}
}
*/
//	Color ordered dither using 18 bits per pixel (6 bit per color plane)
extern "C" __declspec(dllexport)
void ditherBayerRgb18bpp(BYTE* pixels, int width, int height)	
{
	for (int y = 0; y < height; y++)
	{
		int	row = y & 15;	//	y % 16

		for (int x = 0; x < width; x++)
		{
			int	col = x & 15;	//	x % 16

			const int	t = BAYER_PATTERN_16X16[col][row];
			const int	corr = t / 63;

			const int	blue = pixels[x * 3 + 0];
			const int	green = pixels[x * 3 + 1];
			const int	red = pixels[x * 3 + 2];

			int	i1 = (blue + corr) / 4;		CLAMP(i1, 0, 63);
			int	i2 = (green + corr) / 4;		CLAMP(i2, 0, 63);
			int	i3 = (red + corr) / 4;		CLAMP(i3, 0, 63);

			pixels[x * 3 + 0] = i1 * 4;//VALUES_18BPP[i1];
			pixels[x * 3 + 1] = i2 * 4;//VALUES_18BPP[i2];
			pixels[x * 3 + 2] = i3 * 4;//VALUES_18BPP[i3];
		}

		pixels += width * 3;
	}
}

/*Faster but slightly inaccurate version
void ditherBayerRgb18bpp( BYTE* pixels, int width, int height )	
{
	for( int y = 0; y < height; y++ )
	{
		int	row	= y & 15;	//	y % 16

		BYTE*   prow   = pixels + ( y * width * 3 );

		for( int x = 0; x < width; x++ )
		{
			int	col	= x & 15;	//	x % 16

			int	t		= BAYER_PATTERN_16X16[col][row];
			int	corr	= (t >> 6) - 2;

			const int	blue	= prow[x * 3 + 0];
			const int	green	= prow[x * 3 + 1];
			const int	red		= prow[x * 3 + 2];

			int	i1	= (blue  + corr) >> 2;		CLAMP( i1, 0, 63 );
			int	i2	= (green + corr) >> 2;		CLAMP( i2, 0, 63 );
			int	i3	= (red   + corr) >> 2;		CLAMP( i3, 0, 63 );

			prow[x * 3 + 0]	= VALUES_18BPP[i1];
			prow[x * 3 + 1]	= VALUES_18BPP[i2];
			prow[x * 3 + 2]	= VALUES_18BPP[i3];
		}
	}
}*/
//	Floyd-Steinberg dither
//	Floyd-Steinberg dither uses constants 7/16 5/16 3/16 and 1/16
//	But instead of using real arythmetic, I will use integer on by
//	applying shifting ( << 8 )
//	When use the constants, don't foget to shift back the result ( >> 8 )
#define	f7_16	112		//const int	f7	= (7 << 8) / 16;
#define	f5_16	 80		//const int	f5	= (5 << 8) / 16;
#define	f3_16	 48		//const int	f3	= (3 << 8) / 16;
#define	f1_16	 16		//const int	f1	= (1 << 8) / 16;

//#define	FS_COEF( v, err )	(( ((err) * ((v) * 100)) / 16) / 100)
//#define	FS_COEF( v, err )	(( ((err) * ((v) << 8)) >> 4) >> 8)
#define	FS_COEF( v, err )	(((err) * ((v) << 8)) >> 12)


//	This is the ultimate method for Floyd-Steinberg colored Diher
//	ncolors - number of colors diapazons to use. Valid values 0..255, but interesed are 0..40
//	1		- color (1 bit per color plane,  3 bits per pixel)
//	3		- color (2 bit per color plane,  6 bits per pixel)
//	7		- color (3 bit per color plane,  9 bits per pixel)
//	15		- color (4 bit per color plane, 12 bits per pixel)
//	31		- color (5 bit per color plane, 15 bits per pixel)
extern "C" __declspec(dllexport)
void ditherFSRgbNbpp(BYTE* pixels, int width, int height, int ncolors)	
{
	int	divider = 256 / ncolors;

	const int	size = width * height;

	int* errorB = (int*)malloc(size * sizeof(int));
	int* errorG = (int*)malloc(size * sizeof(int));
	int* errorR = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(errorB, 0, size * sizeof(int));
	memset(errorG, 0, size * sizeof(int));
	memset(errorR, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			const int	newValB = blue + errorB[i];
			const int	newValG = green + errorG[i];
			const int	newValR = red + errorR[i];

			int	i1 = newValB / divider;	CLAMP(i1, 0, ncolors);
			int	i2 = newValG / divider;	CLAMP(i2, 0, ncolors);
			int	i3 = newValR / divider;	CLAMP(i3, 0, ncolors);

			//	If you want to compress the image, use the values of i1,i2,i3
			//	they have values in the range 0..ncolors
			//	So if the ncolors is 4 - you have values: 0,1,2,3 which is encoded in 2 bits
			//	2 bits for 3 planes == 6 bits per pixel

			const int	newcB = CLAMPED(i1 * divider, 0, 255);	//	blue
			const int	newcG = CLAMPED(i2 * divider, 0, 255);	//	green
			const int	newcR = CLAMPED(i3 * divider, 0, 255);	//	red

			prow[x * 3 + 0] = newcB;
			prow[x * 3 + 1] = newcG;
			prow[x * 3 + 2] = newcR;

			const int cerrorB = newValB - newcB;
			const int cerrorG = newValG - newcG;
			const int cerrorR = newValR - newcR;

			int	idx = i + 1;
			if (x + 1 < width)
			{
				errorR[idx] += FS_COEF(7, cerrorR);
				errorG[idx] += FS_COEF(7, cerrorG);
				errorB[idx] += FS_COEF(7, cerrorB);
			}

			idx += width - 2;
			if (x - 1 > 0 && y + 1 < height)
			{
				errorR[idx] += FS_COEF(3, cerrorR);
				errorG[idx] += FS_COEF(3, cerrorG);
				errorB[idx] += FS_COEF(3, cerrorB);
			}

			idx++;
			if (y + 1 < height)
			{
				errorR[idx] += FS_COEF(5, cerrorR);
				errorG[idx] += FS_COEF(5, cerrorG);
				errorB[idx] += FS_COEF(5, cerrorB);
			}

			idx++;
			if (x + 1 < width && y + 1 < height)
			{
				errorR[idx] += FS_COEF(1, cerrorR);
				errorG[idx] += FS_COEF(1, cerrorG);
				errorB[idx] += FS_COEF(1, cerrorB);
			}
		}
	}

	free(errorB);
	free(errorG);
	free(errorR);
}
//	Back-white Floyd-Steinberg dither
extern "C" __declspec(dllexport)
void ditherFS(BYTE* pixels, int width, int height)	
{
	const int	size = width * height;

	int* error = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(error, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			//	Get the pixel gray value.
			int	newVal = (red + green + blue) / 3 + (error[i] >> 8);	//	PixelGray + error correction

			int	newc = (newVal < 128 ? 0 : 255);
			prow[x * 3 + 0] = newc;	//	blue
			prow[x * 3 + 1] = newc;	//	green
			prow[x * 3 + 2] = newc;	//	red

			//	Correction - the new error
			const int	cerror = newVal - newc;

			int idx = i + 1;
			if (x + 1 < width)
				error[idx] += (cerror * f7_16);

			idx += width - 2;
			if (x - 1 > 0 && y + 1 < height)
				error[idx] += (cerror * f3_16);

			idx++;
			if (y + 1 < height)
				error[idx] += (cerror * f5_16);

			idx++;
			if (x + 1 < width && y + 1 < height)
				error[idx] += (cerror * f1_16);
		}
	}

	free(error);
}
//	Color Floyd-Steinberg dither using 3 bits per pixel (1 bit per color plane)
extern "C" __declspec(dllexport)
void ditherFSRgb3bpp(BYTE* pixels, int width, int height)	
{
	const int	size = width * height;

	int* errorB = (int*)malloc(size * sizeof(int));
	int* errorG = (int*)malloc(size * sizeof(int));
	int* errorR = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(errorB, 0, size * sizeof(int));
	memset(errorG, 0, size * sizeof(int));
	memset(errorR, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			int	newValB = blue + (errorB[i] >> 8);	//	PixelRed   + error correctionB
			int	newValG = green + (errorG[i] >> 8);	//	PixelGreen + error correctionG
			int	newValR = red + (errorR[i] >> 8);	//	PixelBlue  + error correctionR

			int	newcb = (newValB < 128 ? 0 : 255);
			int	newcg = (newValG < 128 ? 0 : 255);
			int	newcr = (newValR < 128 ? 0 : 255);

			prow[x * 3 + 0] = newcb;
			prow[x * 3 + 1] = newcg;
			prow[x * 3 + 2] = newcr;

			//	Correction - the new error
			int	cerrorR = newValR - newcr;
			int	cerrorG = newValG - newcg;
			int	cerrorB = newValB - newcb;

			int	idx = i + 1;
			if (x + 1 < width)
			{
				errorR[idx] += (cerrorR * f7_16);
				errorG[idx] += (cerrorG * f7_16);
				errorB[idx] += (cerrorB * f7_16);
			}

			idx += width - 2;
			if (x - 1 > 0 && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f3_16);
				errorG[idx] += (cerrorG * f3_16);
				errorB[idx] += (cerrorB * f3_16);
			}

			idx++;
			if (y + 1 < height)
			{
				errorR[idx] += (cerrorR * f5_16);
				errorG[idx] += (cerrorG * f5_16);
				errorB[idx] += (cerrorB * f5_16);
			}

			idx++;
			if (x + 1 < width && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f1_16);
				errorG[idx] += (cerrorG * f1_16);
				errorB[idx] += (cerrorB * f1_16);
			}
		}
	}

	free(errorB);
	free(errorG);
	free(errorR);
}
//	Color Floyd-Steinberg dither using 6 bits per pixel (2 bit per color plane)
extern "C" __declspec(dllexport)
void ditherFSRgb6bpp(BYTE* pixels, int width, int height)	
{
	const int	size = width * height;

	int* errorB = (int*)malloc(size * sizeof(int));
	int* errorG = (int*)malloc(size * sizeof(int));
	int* errorR = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(errorB, 0, size * sizeof(int));
	memset(errorG, 0, size * sizeof(int));
	memset(errorR, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			int	newValB = int(blue + (errorB[i] >> 8));	//	PixelBlue  + error correctionB
			int	newValG = int(green + (errorG[i] >> 8));	//	PixelGreen + error correctionG
			int	newValR = int(red + (errorR[i] >> 8));	//	PixelRed   + error correctionR

			//	The error could produce values beyond the borders, so need to keep the color in range
			int	idxR = CLAMPED(newValR, 0, 255);
			int	idxG = CLAMPED(newValG, 0, 255);
			int	idxB = CLAMPED(newValB, 0, 255);

			int	newcR = VALUES_6BPP[idxR >> 6];	//	x >> 6 is the same as x / 64
			int	newcG = VALUES_6BPP[idxG >> 6];	//	x >> 6 is the same as x / 64
			int	newcB = VALUES_6BPP[idxB >> 6];	//	x >> 6 is the same as x / 64

			prow[x * 3 + 0] = newcB;
			prow[x * 3 + 1] = newcG;
			prow[x * 3 + 2] = newcR;

			int cerrorB = newValB - newcB;
			int cerrorG = newValG - newcG;
			int cerrorR = newValR - newcR;

			int	idx = i + 1;
			if (x + 1 < width)
			{
				errorR[idx] += (cerrorR * f7_16);
				errorG[idx] += (cerrorG * f7_16);
				errorB[idx] += (cerrorB * f7_16);
			}

			idx += width - 2;
			if (x - 1 > 0 && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f3_16);
				errorG[idx] += (cerrorG * f3_16);
				errorB[idx] += (cerrorB * f3_16);
			}

			idx++;
			if (y + 1 < height)
			{
				errorR[idx] += (cerrorR * f5_16);
				errorG[idx] += (cerrorG * f5_16);
				errorB[idx] += (cerrorB * f5_16);
			}

			idx++;
			if (x + 1 < width && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f1_16);
				errorG[idx] += (cerrorG * f1_16);
				errorB[idx] += (cerrorB * f1_16);
			}
		}
	}

	free(errorB);
	free(errorG);
	free(errorR);
}
//	Color Floyd-Steinberg dither using 9 bits per pixel (3 bit per color plane)
extern "C" __declspec(dllexport)
void ditherFSRgb9bpp(BYTE* pixels, int width, int height)	
{
	const int	size = width * height;

	int* errorB = (int*)malloc(size * sizeof(int));
	int* errorG = (int*)malloc(size * sizeof(int));
	int* errorR = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(errorB, 0, size * sizeof(int));
	memset(errorG, 0, size * sizeof(int));
	memset(errorR, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			int	newValB = int(blue + (errorB[i] >> 8));	//	PixelBlue  + error correctionB
			int	newValG = int(green + (errorG[i] >> 8));	//	PixelGreen + error correctionG
			int	newValR = int(red + (errorR[i] >> 8));	//	PixelRed   + error correctionR

			//	The error could produce values beyond the borders, so need to keep the color in range
			int	idxR = CLAMPED(newValR, 0, 255);
			int	idxG = CLAMPED(newValG, 0, 255);
			int	idxB = CLAMPED(newValB, 0, 255);

			int	newcR = VALUES_9BPP[idxR >> 5];	//	x >> 5 is the same as x / 32
			int	newcG = VALUES_9BPP[idxG >> 5];	//	x >> 5 is the same as x / 32
			int	newcB = VALUES_9BPP[idxB >> 5];	//	x >> 5 is the same as x / 32

			prow[x * 3 + 0] = newcB;
			prow[x * 3 + 1] = newcG;
			prow[x * 3 + 2] = newcR;

			int cerrorB = newValB - newcB;
			int cerrorG = newValG - newcG;
			int cerrorR = newValR - newcR;

			int	idx = i + 1;
			if (x + 1 < width)
			{
				errorR[idx] += (cerrorR * f7_16);
				errorG[idx] += (cerrorG * f7_16);
				errorB[idx] += (cerrorB * f7_16);
			}

			idx += width - 2;
			if (x - 1 > 0 && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f3_16);
				errorG[idx] += (cerrorG * f3_16);
				errorB[idx] += (cerrorB * f3_16);
			}

			idx++;
			if (y + 1 < height)
			{
				errorR[idx] += (cerrorR * f5_16);
				errorG[idx] += (cerrorG * f5_16);
				errorB[idx] += (cerrorB * f5_16);
			}

			idx++;
			if (x + 1 < width && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f1_16);
				errorG[idx] += (cerrorG * f1_16);
				errorB[idx] += (cerrorB * f1_16);
			}
		}
	}

	free(errorB);
	free(errorG);
	free(errorR);
}
//	Color Floyd-Steinberg dither using 12 bits per pixel (4 bit per color plane)
extern "C" __declspec(dllexport)
void ditherFSRgb12bpp(BYTE* pixels, int width, int height)	
{
	const int	size = width * height;

	int* errorB = (int*)malloc(size * sizeof(int));
	int* errorG = (int*)malloc(size * sizeof(int));
	int* errorR = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(errorB, 0, size * sizeof(int));
	memset(errorG, 0, size * sizeof(int));
	memset(errorR, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			int	newValB = int(blue + (errorB[i] >> 8));	//	PixelBlue  + error correctionB
			int	newValG = int(green + (errorG[i] >> 8));	//	PixelGreen + error correctionG
			int	newValR = int(red + (errorR[i] >> 8));	//	PixelRed   + error correctionR

			//	The error could produce values beyond the borders, so need to keep the color in range
			int	idxR = CLAMPED(newValR, 0, 255);
			int	idxG = CLAMPED(newValG, 0, 255);
			int	idxB = CLAMPED(newValB, 0, 255);

			int	newcR = VALUES_12BPP[idxR >> 4];	//	x >> 4 is the same as x / 16
			int	newcG = VALUES_12BPP[idxG >> 4];	//	x >> 4 is the same as x / 16
			int	newcB = VALUES_12BPP[idxB >> 4];	//	x >> 4 is the same as x / 16

			prow[x * 3 + 0] = newcB;
			prow[x * 3 + 1] = newcG;
			prow[x * 3 + 2] = newcR;

			int cerrorB = newValB - newcB;
			int cerrorG = newValG - newcG;
			int cerrorR = newValR - newcR;

			int	idx = i + 1;
			if (x + 1 < width)
			{
				errorR[idx] += (cerrorR * f7_16);
				errorG[idx] += (cerrorG * f7_16);
				errorB[idx] += (cerrorB * f7_16);
			}

			idx += width - 2;
			if (x - 1 > 0 && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f3_16);
				errorG[idx] += (cerrorG * f3_16);
				errorB[idx] += (cerrorB * f3_16);
			}

			idx++;
			if (y + 1 < height)
			{
				errorR[idx] += (cerrorR * f5_16);
				errorG[idx] += (cerrorG * f5_16);
				errorB[idx] += (cerrorB * f5_16);
			}

			idx++;
			if (x + 1 < width && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f1_16);
				errorG[idx] += (cerrorG * f1_16);
				errorB[idx] += (cerrorB * f1_16);
			}
		}
	}

	free(errorB);
	free(errorG);
	free(errorR);
}
//	Color Floyd-Steinberg dither using 15 bits per pixel (5 bit per color plane)
extern "C" __declspec(dllexport)
void ditherFSRgb15bpp(BYTE* pixels, int width, int height)	
{
	const int	size = width * height;

	int* errorB = (int*)malloc(size * sizeof(int));
	int* errorG = (int*)malloc(size * sizeof(int));
	int* errorR = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(errorB, 0, size * sizeof(int));
	memset(errorG, 0, size * sizeof(int));
	memset(errorR, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			int	newValB = int(blue + (errorB[i] >> 8));	//	PixelBlue  + error correctionB
			int	newValG = int(green + (errorG[i] >> 8));	//	PixelGreen + error correctionG
			int	newValR = int(red + (errorR[i] >> 8));	//	PixelRed   + error correctionR

			//	The error could produce values beyond the borders, so need to keep the color in range
			int	idxR = CLAMPED(newValR, 0, 255);
			int	idxG = CLAMPED(newValG, 0, 255);
			int	idxB = CLAMPED(newValB, 0, 255);

			int	newcR = VALUES_15BPP[idxR >> 3];	//	x >> 3 is the same as x / 8
			int	newcG = VALUES_15BPP[idxG >> 3];	//	x >> 3 is the same as x / 8
			int	newcB = VALUES_15BPP[idxB >> 3];	//	x >> 3 is the same as x / 8

			prow[x * 3 + 0] = newcB;
			prow[x * 3 + 1] = newcG;
			prow[x * 3 + 2] = newcR;

			int cerrorB = newValB - newcB;
			int cerrorG = newValG - newcG;
			int cerrorR = newValR - newcR;

			int	idx = i + 1;
			if (x + 1 < width)
			{
				errorR[idx] += (cerrorR * f7_16);
				errorG[idx] += (cerrorG * f7_16);
				errorB[idx] += (cerrorB * f7_16);
			}

			idx += width - 2;
			if (x - 1 > 0 && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f3_16);
				errorG[idx] += (cerrorG * f3_16);
				errorB[idx] += (cerrorB * f3_16);
			}

			idx++;
			if (y + 1 < height)
			{
				errorR[idx] += (cerrorR * f5_16);
				errorG[idx] += (cerrorG * f5_16);
				errorB[idx] += (cerrorB * f5_16);
			}

			idx++;
			if (x + 1 < width && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f1_16);
				errorG[idx] += (cerrorG * f1_16);
				errorB[idx] += (cerrorB * f1_16);
			}
		}
	}

	free(errorB);
	free(errorG);
	free(errorR);
}
//	Color Floyd-Steinberg dither using 18 bits per pixel (6 bit per color plane)
extern "C" __declspec(dllexport)
void ditherFSRgb18bpp(BYTE* pixels, int width, int height)	
{
	const int	size = width * height;

	int* errorB = (int*)malloc(size * sizeof(int));
	int* errorG = (int*)malloc(size * sizeof(int));
	int* errorR = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(errorB, 0, size * sizeof(int));
	memset(errorG, 0, size * sizeof(int));
	memset(errorR, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			int	newValB = int(blue + (errorB[i] >> 8));	//	PixelBlue  + error correctionB
			int	newValG = int(green + (errorG[i] >> 8));	//	PixelGreen + error correctionG
			int	newValR = int(red + (errorR[i] >> 8));	//	PixelRed   + error correctionR

			//	The error could produce values beyond the borders, so need to keep the color in range
			int	idxR = CLAMPED(newValR, 0, 255);
			int	idxG = CLAMPED(newValG, 0, 255);
			int	idxB = CLAMPED(newValB, 0, 255);

			int	newcR = VALUES_18BPP[idxR >> 2];	//	x >> 2 is the same as x / 4
			int	newcG = VALUES_18BPP[idxG >> 2];	//	x >> 2 is the same as x / 4
			int	newcB = VALUES_18BPP[idxB >> 2];	//	x >> 2 is the same as x / 4

			prow[x * 3 + 0] = newcB;
			prow[x * 3 + 1] = newcG;
			prow[x * 3 + 2] = newcR;

			int cerrorB = newValB - newcB;
			int cerrorG = newValG - newcG;
			int cerrorR = newValR - newcR;

			int	idx = i + 1;
			if (x + 1 < width)
			{
				errorR[idx] += (cerrorR * f7_16);
				errorG[idx] += (cerrorG * f7_16);
				errorB[idx] += (cerrorB * f7_16);
			}

			idx += width - 2;
			if (x - 1 > 0 && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f3_16);
				errorG[idx] += (cerrorG * f3_16);
				errorB[idx] += (cerrorB * f3_16);
			}

			idx++;
			if (y + 1 < height)
			{
				errorR[idx] += (cerrorR * f5_16);
				errorG[idx] += (cerrorG * f5_16);
				errorB[idx] += (cerrorB * f5_16);
			}

			idx++;
			if (x + 1 < width && y + 1 < height)
			{
				errorR[idx] += (cerrorR * f1_16);
				errorG[idx] += (cerrorG * f1_16);
				errorB[idx] += (cerrorB * f1_16);
			}
		}
	}

	free(errorB);
	free(errorG);
	free(errorR);
}
//#define	SIERRA_LITE_COEF( v, err )	((( (err) * ((v) * 100)) / 4) / 100)
#define	SIERRA_LITE_COEF( v, err )	((( (err) * ((v) << 8)) >> 2) >> 8)

//	This is the ultimate method for SierraLite colored Diher
//	ncolors - number of colors diapazons to use. Valid values 0..255, but interesed are 0..40
//	1		- color (1 bit per color plane,  3 bits per pixel)
//	3		- color (2 bit per color plane,  6 bits per pixel)
//	7		- color (3 bit per color plane,  9 bits per pixel)
//	15		- color (4 bit per color plane, 12 bits per pixel)
//	31		- color (5 bit per color plane, 15 bits per pixel)
extern "C" __declspec(dllexport)
void ditherSierraLiteRgbNbpp(BYTE* pixels, int width, int height, int ncolors)	
{
	int	divider = 256 / ncolors;

	const int	size = width * height;

	int* errorB = (int*)malloc(size * sizeof(int));
	int* errorG = (int*)malloc(size * sizeof(int));
	int* errorR = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(errorB, 0, size * sizeof(int));
	memset(errorG, 0, size * sizeof(int));
	memset(errorR, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			const int	newValB = blue + errorB[i];
			const int	newValG = green + errorG[i];
			const int	newValR = red + errorR[i];

			int	i1 = newValB / divider;	CLAMP(i1, 0, ncolors);
			int	i2 = newValG / divider;	CLAMP(i2, 0, ncolors);
			int	i3 = newValR / divider;	CLAMP(i3, 0, ncolors);

			//	If you want to compress the image, use the values of i1,i2,i3
			//	they have values in the range 0..ncolors
			//	So if the ncolors is 4 - you have values: 0,1,2,3 which is encoded in 2 bits
			//	2 bits for 3 planes == 6 bits per pixel

			const int	newcB = CLAMPED(i1 * divider, 0, 255);	//	blue
			const int	newcG = CLAMPED(i2 * divider, 0, 255);	//	green
			const int	newcR = CLAMPED(i3 * divider, 0, 255);	//	red

			prow[x * 3 + 0] = newcB;
			prow[x * 3 + 1] = newcG;
			prow[x * 3 + 2] = newcR;

			const int cerrorB = (newValB - newcB);
			const int cerrorG = (newValG - newcG);
			const int cerrorR = (newValR - newcR);

			int	idx = i;
			if (x + 1 < width)
			{
				errorR[idx + 1] += SIERRA_LITE_COEF(2, cerrorR);
				errorG[idx + 1] += SIERRA_LITE_COEF(2, cerrorG);
				errorB[idx + 1] += SIERRA_LITE_COEF(2, cerrorB);
			}

			idx += width;
			if (y + 1 < height)
			{
				if (x - 1 > 0 && y + 1 < height)
				{
					errorR[idx - 1] += SIERRA_LITE_COEF(1, cerrorR);
					errorG[idx - 1] += SIERRA_LITE_COEF(1, cerrorG);
					errorB[idx - 1] += SIERRA_LITE_COEF(1, cerrorB);
				}

				errorR[idx] += SIERRA_LITE_COEF(1, cerrorR);
				errorG[idx] += SIERRA_LITE_COEF(1, cerrorG);
				errorB[idx] += SIERRA_LITE_COEF(1, cerrorB);
			}
		}
	}

	free(errorB);
	free(errorG);
	free(errorR);
}
//	Black-white Sierra Lite dithering (variation of Floyd-Steinberg with less computational cost)
extern "C" __declspec(dllexport)
void ditherSierraLite(BYTE* pixels, int width, int height)	
{
	//	To avoid real number calculations, I will raise the level of INT arythmetics by shifting with 8 bits to the left ( << 8 )
	//	Later, when it is necessary will return to te normal level by shifting back 8 bits to the right ( >> 8 )
	//	    X   2
	//	1   1
	//	  (1/4)

	//~~~~~~~~

	const int	size = width * height;

	int* error = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(error, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		for (int x = 0; x < width; x++, i++)
		{
			const pixel	blue = pixels[x * 3 + 0];
			const pixel	green = pixels[x * 3 + 1];
			const pixel	red = pixels[x * 3 + 2];

			//	Get the pixel gray value.
			int	newVal = (red + green + blue) / 3 + error[i];		//	PixelGray + error correction
			int	newc = (newVal < 128 ? 0 : 255);

			pixels[x * 3 + 0] = newc;
			pixels[x * 3 + 1] = newc;
			pixels[x * 3 + 2] = newc;

			//	Correction - the new error
			const int	cerror = newVal - newc;

			int idx = i;
			if (x + 1 < width)
				error[idx + 1] += SIERRA_LITE_COEF(2, cerror);

			idx += width;
			if (y + 1 < height)
			{
				if (x - 1 >= 0)
					error[idx - 1] += SIERRA_LITE_COEF(1, cerror);

				error[idx] += SIERRA_LITE_COEF(1, cerror);
			}
		}

		pixels += width * 3;
	}

	free(error);
}
//#define	SIERRA_COEF( v, err )	((( (err) * ((v) * 100)) / 32) / 100)
#define	SIERRA_COEF( v, err )	((( (err) * ((v) << 8)) >> 5) >> 8)

//	This is the ultimate method for SierraLite colored Diher
//	ncolors - number of colors diapazons to use. Valid values 0..255, but interesed are 0..40
//	1		- color (1 bit per color plane,  3 bits per pixel)
//	3		- color (2 bit per color plane,  6 bits per pixel)
//	7		- color (3 bit per color plane,  9 bits per pixel)
//	15		- color (4 bit per color plane, 12 bits per pixel)
//	31		- color (5 bit per color plane, 15 bits per pixel)
extern "C" __declspec(dllexport)
void ditherSierraRgbNbpp(BYTE* pixels, int width, int height, int ncolors)	
{
	int	divider = 256 / ncolors;

	const int	size = width * height;

	int* errorB = (int*)malloc(size * sizeof(int));
	int* errorG = (int*)malloc(size * sizeof(int));
	int* errorR = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(errorB, 0, size * sizeof(int));
	memset(errorG, 0, size * sizeof(int));
	memset(errorR, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		BYTE* prow = pixels + (y * width * 3);

		for (int x = 0; x < width; x++, i++)
		{
			const int	blue = prow[x * 3 + 0];
			const int	green = prow[x * 3 + 1];
			const int	red = prow[x * 3 + 2];

			const int	newValB = blue + errorB[i];
			const int	newValG = green + errorG[i];
			const int	newValR = red + errorR[i];

			int	i1 = newValB / divider;	CLAMP(i1, 0, ncolors);
			int	i2 = newValG / divider;	CLAMP(i2, 0, ncolors);
			int	i3 = newValR / divider;	CLAMP(i3, 0, ncolors);

			//	If you want to compress the image, use the values of i1,i2,i3
			//	they have values in the range 0..ncolors
			//	So if the ncolors is 4 - you have values: 0,1,2,3 which is encoded in 2 bits
			//	2 bits for 3 planes == 6 bits per pixel

			const int	newcB = CLAMPED(i1 * divider, 0, 255);	//	blue
			const int	newcG = CLAMPED(i2 * divider, 0, 255);	//	green
			const int	newcR = CLAMPED(i3 * divider, 0, 255);	//	red

			prow[x * 3 + 0] = newcB;
			prow[x * 3 + 1] = newcG;
			prow[x * 3 + 2] = newcR;

			const int cerrorB = (newValB - newcB);
			const int cerrorG = (newValG - newcG);
			const int cerrorR = (newValR - newcR);

			int idx = i;
			if (x + 1 < width)
			{
				errorR[idx + 1] += SIERRA_COEF(5, cerrorR);
				errorG[idx + 1] += SIERRA_COEF(5, cerrorG);
				errorB[idx + 1] += SIERRA_COEF(5, cerrorB);
			}

			if (x + 2 < width)
			{
				errorR[idx + 2] += SIERRA_COEF(3, cerrorR);
				errorG[idx + 2] += SIERRA_COEF(3, cerrorG);
				errorB[idx + 2] += SIERRA_COEF(3, cerrorB);
			}

			if (y + 1 < height)
			{
				idx += width;
				if (x - 2 >= 0)
				{
					errorR[idx - 2] += SIERRA_COEF(2, cerrorR);
					errorG[idx - 2] += SIERRA_COEF(2, cerrorG);
					errorB[idx - 2] += SIERRA_COEF(2, cerrorB);
				}


				if (x - 1 >= 0)
				{
					errorR[idx - 1] += SIERRA_COEF(4, cerrorR);
					errorG[idx - 1] += SIERRA_COEF(4, cerrorG);
					errorB[idx - 1] += SIERRA_COEF(4, cerrorB);
				}

				errorR[idx] += SIERRA_COEF(5, cerrorR);
				errorG[idx] += SIERRA_COEF(5, cerrorG);
				errorB[idx] += SIERRA_COEF(5, cerrorB);

				if (x + 1 < width)
				{
					errorR[idx + 1] += SIERRA_COEF(4, cerrorR);
					errorG[idx + 1] += SIERRA_COEF(4, cerrorG);
					errorB[idx + 1] += SIERRA_COEF(4, cerrorB);
				}

				if (x + 2 < width)
				{
					errorR[idx + 2] += SIERRA_COEF(2, cerrorR);
					errorG[idx + 2] += SIERRA_COEF(2, cerrorG);
					errorB[idx + 2] += SIERRA_COEF(2, cerrorB);
				}
			}

			if (y + 2 < height)
			{
				idx += width;
				if (x - 1 >= 0)
				{
					errorR[idx - 1] += SIERRA_COEF(2, cerrorR);
					errorG[idx - 1] += SIERRA_COEF(2, cerrorG);
					errorB[idx - 1] += SIERRA_COEF(2, cerrorB);
				}

				errorR[idx] += SIERRA_COEF(3, cerrorR);
				errorG[idx] += SIERRA_COEF(3, cerrorG);
				errorB[idx] += SIERRA_COEF(3, cerrorB);

				if (x + 1 < width)
				{
					errorR[idx + 1] += SIERRA_COEF(2, cerrorR);
					errorG[idx + 1] += SIERRA_COEF(2, cerrorG);
					errorB[idx + 1] += SIERRA_COEF(2, cerrorB);
				}
			}

		}
	}

	free(errorB);
	free(errorG);
	free(errorR);
}
//	Black-white Sierra Lite dithering (variation of Floyd-Steinberg with less computational cost)
extern "C" __declspec(dllexport)
void ditherSierra(BYTE* pixels, int width, int height)	
{
	//	To avoid real number calculations, I will raise the level of INT arythmetics by shifting with 8 bits to the left ( << 8 )
	//	Later, when it is necessary will return to te normal level by shifting back 8 bits to the right ( >> 8 )
	//	       X   5   3
	//	2   4  5   4   2
	//	    2  3   2
	//	    (1/32)

	//~~~~~~~~

	const int	size = width * height;

	int* error = (int*)malloc(size * sizeof(int));

	//	Clear the errors buffer.
	memset(error, 0, size * sizeof(int));

	//~~~~~~~~

	int	i = 0;

	for (int y = 0; y < height; y++)
	{
		for (int x = 0; x < width; x++, i++)
		{
			const pixel	blue = pixels[x * 3 + 0];
			const pixel	green = pixels[x * 3 + 1];
			const pixel	red = pixels[x * 3 + 2];

			//	Get the pixel gray value.
			int	newVal = (red + green + blue) / 3 + error[i];		//	PixelGray + error correction
			int	newc = (newVal < 128 ? 0 : 255);

			pixels[x * 3 + 0] = newc;
			pixels[x * 3 + 1] = newc;
			pixels[x * 3 + 2] = newc;

			//	Correction - the new error
			const int	cerror = newVal - newc;

			int idx = i;
			if (x + 1 < width)
				error[idx + 1] += SIERRA_COEF(5, cerror);

			if (x + 2 < width)
				error[idx + 2] += SIERRA_COEF(3, cerror);

			if (y + 1 < height)
			{
				idx += width;
				if (x - 2 >= 0)
					error[idx - 2] += SIERRA_COEF(2, cerror);

				if (x - 1 >= 0)
					error[idx - 1] += SIERRA_COEF(4, cerror);

				error[idx] += SIERRA_COEF(5, cerror);

				if (x + 1 < width)
					error[idx + 1] += SIERRA_COEF(4, cerror);

				if (x + 2 < width)
					error[idx + 2] += SIERRA_COEF(2, cerror);
			}

			if (y + 2 < height)
			{
				idx += width;
				if (x - 1 >= 0)
					error[idx - 1] += SIERRA_COEF(2, cerror);

				error[idx] += SIERRA_COEF(3, cerror);

				if (x + 1 < width)
					error[idx + 1] += SIERRA_COEF(2, cerror);
			}
		}

		pixels += width * 3;
	}

	free(error);
}




#include "ParticleFilter.h"
#include <fstream>


#define CV_REDUCE_SUM 0
#define CV_REDUCE_AVG 1
#define CV_REDUCE_MAX 2
#define CV_REDUCE_MIN 3

ParticleFilter::ParticleFilter(void) {}

ParticleFilter::ParticleFilter(const Mat& inZ, const Mat& inR, unsigned int inN)
{
	// TODO: Improve by asserting using opencv assert calls
	if (inZ.empty() || inR.empty())
	{
		std::cout << "Error: insufficient inputs" << std::endl;
	}

	this->N = inN;
	this->R = inR.clone();

	this->N_threshold = int(float(6 * N) / 10.0f);

	selectDynamicModel(inZ.rows); //  with dimensions of observation

	this->Ds = this->A.cols; // A is square matrix providing dimensions of state vector

	Mat lR, uR; // store upper and lower bounds for each particle

	// assuming that the state vector has constant velocity model
	lR = Mat::zeros(3 * inZ.rows, 1, CV_32FC1);
	uR = Mat::zeros(3 * inZ.rows, 1, CV_32FC1);

	lR.at<float>(0, 0) = inZ.at<float>(0, 0); // posX
	lR.at<float>(1, 0) = -30;				  // velX
	lR.at<float>(2, 0) = -1;				  // accX

	lR.at<float>(0 + 3, 0) = inZ.at<float>(1, 0); // posY
	lR.at<float>(1 + 3, 0) = -30;				  // velY
	lR.at<float>(2 + 3, 0) = -1;				  // accY

	uR.at<float>(0, 0) = inZ.at<float>(0, 0);
	uR.at<float>(1, 0) = 30;
	uR.at<float>(2, 0) = 1;

	uR.at<float>(0 + 3, 0) = inZ.at<float>(1, 0);
	uR.at<float>(1 + 3, 0) = 30;
	uR.at<float>(2 + 3, 0) = 1;

	initParticles(lR, uR);

}

void ParticleFilter::resampleParticles()
{
	Mat outXn = Mat::zeros(Xn.rows, Xn.cols, Xn.type());

	Mat Neff;

	reduce(Wn.mul(Wn), Neff, 1, CV_REDUCE_SUM);

	//std::cout << Neff.cols << " " << Neff.rows << std::endl;

	if (Neff.at<float>(0, 0) < this->N_threshold)
	{
		Mat outIdx = this->resampler(Wn);
		Wn = Mat::ones(Wn.rows, Wn.cols, Wn.type());
		this->normalizeWeights();
		for (int i = 0; i < int(this->N); i++)
		{
			Xn.col(int((outIdx.at<float>(0, i)))).copyTo(outXn.col(i));
		}

		Xn = outXn.clone();
	}
}

void ParticleFilter::predict()
{
	Mat gaussianNoise = Mat::zeros(Ds, N, CV_32FC1);
	theRNG().fill(gaussianNoise, RNG::NORMAL, 0, 3);
	//Mat RTemp = repeat(R, 1, N);
	Xn = A * Xn + R * gaussianNoise;
}

void ParticleFilter::update(const Mat& inZ)
{
	weightingParticles(inZ);
}

Mat ParticleFilter::currentPrediction()
{
	Mat pLocs = H * (Xn * Wn.t());;
	return pLocs;
}

Mat ParticleFilter::showParticles(const Mat& inImage)
{
	Vec3b cVal;
	cVal[0] = 255;
	cVal[1] = 255;
	cVal[2] = 255;

	Mat pLocs = H * Xn;

	Mat retImage = inImage.clone();

	for (int i = 0; i < int(this->N); i++)
	{
		int xIdx = int(std::floor(pLocs.at<float>(0, i)));
		int yIdx = int(std::floor(pLocs.at<float>(1, i)));

		if (xIdx >= 0 && xIdx < retImage.cols && yIdx >= 0 && yIdx < retImage.rows)
			retImage.at<Vec3b>(yIdx, xIdx) = cVal;
	}

	return retImage;
}

Mat ParticleFilter::showPredictedLocation(const Mat& inImage)
{
	Vec3b cVal;
	cVal[0] = 255;
	cVal[1] = 0;
	cVal[2] = 0;

	// find prediction of model
	Mat pLocs = H * (Xn * Wn.t());

	// std::cout << "Tracked Location: " << pLocs << std::endl;
	Mat retImage = inImage.clone();

	int xIdx = int(std::floor(pLocs.at<float>(0, 0)));
	int yIdx = int(std::floor(pLocs.at<float>(1, 0)));

	// draw a crosshair
	int sizeIn = 3;
	int sizeOut = 5;
	for (int j = -sizeOut; j < sizeOut + 1; j++)
	{
		for (int i = -sizeIn; i < sizeIn + 1; i++)
			if (xIdx + i >= 0 && xIdx + i < retImage.cols && yIdx >= 0 && yIdx < retImage.rows)
				retImage.at<Vec3b>(yIdx + j, xIdx + i) = cVal;

		for (int i = -sizeIn; i < sizeIn + 1; i++)
			if (xIdx >= 0 && xIdx < retImage.cols && yIdx + i >= 0 && yIdx + i < retImage.rows)
				retImage.at<Vec3b>(yIdx + i, xIdx + j) = cVal;

	}
	return retImage;

}

ParticleFilter::~ParticleFilter(void)
{
	// make sure that large Matrices are cleared
	this->Wn.release();
	this->Xn.release();

	// opencv will automatically clear all variables
}

// Private helper functions

void ParticleFilter::initParticles(Mat& lR, Mat& uR)
{
	Mat Rn = Mat::zeros(this->Ds, this->N, CV_32FC1);
	theRNG().fill(Rn, RNG::UNIFORM, 0, 1);

	Mat Sn = repeat(uR - lR, 1, this->N);

	Mat Tn = repeat(lR, 1, this->N);

	Mat temp = (Rn.mul(Sn) + Tn);

	Xn = temp.clone();
}

// TODO:: Add mode for options for different models
void ParticleFilter::selectDynamicModel(unsigned int D)
{
	A = Mat::zeros(6, 6, CV_32FC1);
	H = Mat::zeros(2, 6, CV_32FC1);

	A.at<float>(0, 0) = 1;
	A.at<float>(0, 1) = 1;
	A.at<float>(1, 1) = 1;
	A.at<float>(1, 2) = 1;

	A.at<float>(0 + 3, 0 + 3) = 1;
	A.at<float>(0 + 3, 1 + 3) = 1;
	A.at<float>(1 + 3, 1 + 3) = 1;
	A.at<float>(1 + 3, 2 + 3) = 1;

	H.at<float>(0, 0) = 1;
	H.at<float>(1, 3) = 1;

	//std::cout << "Dynamic Model A = " << A << std::endl;
	//std::cout << "Observation Model H = " << H << std::endl;
}

void ParticleFilter::normalizeWeights()
{
	Mat wSum;
	reduce(Wn, wSum, 1, CV_REDUCE_SUM);
	float sum = wSum.at<float>(0, 0);
	//if (sum == 0) 
	//	sum = 1;
	Wn = Wn / sum;
}

void ParticleFilter::weightingParticles(const Mat& inZ)
{
	Wn = Mat::zeros(1, N, CV_32FC1);

	Mat allpZt = H * Xn;
	//std::cout << "inZ" << inZ << std::endl;
	//std::cout << "allpZt" << allpZt << std::endl;
	//Mat pZt;

	for (int i = 0; i < int(N); i++)
	{

		double d = distanceGaussian(inZ, allpZt.col(i).clone());
		//std::cout << "inZ" << inZ << std::endl;
		//std::cout << "allpZt" << allpZt.col(i) << std::endl;

		Wn.at<float>(0, i) = float(d);

		//std::cout << "Distance is:" << d << std::endl;
	}

	this->normalizeWeights();
}

double ParticleFilter::distanceGaussian(const Mat& inZ, const Mat& pZt)
{
	double sigma = 5;

	int n = inZ.rows;

	double bTerm = 0;

	for (int j = 0; j < n; j++)
	{
		bTerm = bTerm - std::pow(inZ.at<float>(j, 0) - pZt.at<float>(j, 0), 2) / (2 * std::pow(sigma, 2));
	}

	double aTerm = 1 / std::pow(std::sqrt(2 * CV_PI * std::pow(sigma, 2)), n);

	return aTerm * std::exp(bTerm);
}

Mat ParticleFilter::resampler(const Mat& inProbs)
{
	// resample function - for resampling the particles based on their weights
	Mat retIndex = Mat::zeros(inProbs.rows, inProbs.cols, CV_32FC1);

	int idx = theRNG().uniform(0, inProbs.cols - 1);
	double mW;
	Mat maxProb;
	double beta = 0.0;
	// reducing to get the max for all the input feature samples
	reduce(inProbs, maxProb, 1 /*means reduced to single column*/, CV_REDUCE_MAX);
	mW = maxProb.at<float>(0, 0);

	for (int i = 0; i < inProbs.cols; i++)
	{
		beta = beta + theRNG().uniform(0.0, mW);
		while (beta > inProbs.at<float>(0, idx))
		{
			beta = beta - inProbs.at<float>(0, idx);
			idx = (idx + 1) % inProbs.cols;
		}
		retIndex.at<float>(0, i) = float(idx); // not matlab so idx+1 is not required
	}
	return retIndex;
}







class ParticleFilterTest
{
private:
public:
	Mat ballLocation;
	Mat inR;
	int currentFrame;
	Mat fullImage;
	std::string dataDir;
	ParticleFilter pf;
	ParticleFilterTest(std::string dataDirectory)
	{
		dataDir = dataDirectory;
		std::string filename(dataDir + "ballLocation.dat");
		std::fstream file;
		file.open(filename.c_str(), std::ios::in | std::ios::binary);
		if (!file.is_open())
		{
			std::cout << "Error opening matlab binary file" << std::endl;
			std::cout << filename << std::endl;
		}

		// read the size of the Mat (including the number of channels)
		double colsMat, rowsMat;

		file.read((char*)&rowsMat, sizeof(rowsMat));
		file.read((char*)&colsMat, sizeof(colsMat));

		ballLocation = Mat::zeros(int(rowsMat), int(colsMat), CV_32FC1);

		for (int i = 0; i < ballLocation.cols; i++)
		{

			for (int j = 0; j < ballLocation.rows; j++)
			{
				double buff;
				file.read((char*)&buff, sizeof(buff));
				ballLocation.at<float>(j, i) = float(buff);
			}
		}
		file.close();

		//std::cout << ballLocation << std::endl;
		inR = Mat::eye(6, 6, CV_32FC1);
		inR = inR / 2;
		//std::cout << inR << std::endl;
		currentFrame = 13;
		pf = ParticleFilter(ballLocation.col(currentFrame), inR);
	}
	Mat loadImage(int imageNum)
	{
		Mat inImage;
		char buffer[256];
		sprintf_s(buffer, "Color_%d.png", imageNum);
		std::string filename(dataDir + buffer);
		inImage = imread(filename);
		return inImage;
	}
	void Run()
	{
		if (currentFrame == 45) currentFrame = 13; // restart at the beginning
		Mat inImage = loadImage(currentFrame + 1);
		if (ballLocation.at<float>(0, currentFrame) + ballLocation.at<float>(1, currentFrame) != 0)
		{
			// update particle weights to the input location
			pf.update(ballLocation.col(currentFrame));
		}

		// resample particles using importance sampling
		pf.resampleParticles();
		Mat pImage = pf.showParticles(inImage);
		pImage = pf.showPredictedLocation(pImage);
		//imshow("pf Out", pImage);
		//waitKey(0);

		// predict next state
		pf.predict();

		currentFrame += 1;
		resize(pImage, fullImage, fullImage.size());
	}
};

extern "C" __declspec(dllexport)
ParticleFilterTest* ParticleFilterTest_Open(char* dataDirName, int rows, int cols) {
	std::string dataDir(dataDirName);
	ParticleFilterTest* cPtr = new ParticleFilterTest(dataDir);
	cPtr->fullImage = Mat(rows, cols, CV_8UC3);
	return cPtr;
}

extern "C" __declspec(dllexport)
int* ParticleFilterTest_Close(ParticleFilterTest* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* ParticleFilterTest_Run(ParticleFilterTest* cPtr)
{
	cPtr->Run();
	return (int*)cPtr->fullImage.data;
}








/////////////////////////////////////////////////////////////////
// 
//          Copyright Vadim Stadnik 2015.
// Distributed under the Code Project Open License (CPOL) 1.02. 
// (See or copy at http://www.codeproject.com/info/cpol10.aspx) 
//
/////////////////////////////////////////////////////////////////


#include <vector> 
#include <list> 
#include <deque> 
#include <set> 
#include <algorithm> 
#include <sstream>

#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>


//  class Point represents a two dimensional point 
class vPoint
{
public:
	explicit
		vPoint(int _x = 0, int _y = 0) : x(_x), y(_y) {  }

	int	 X() const { return x; }
	int	 Y() const { return y; }

	bool operator <  (const vPoint& that) const
	{
		if (x < that.x)
			return true;
		else if (that.x < x)
			return false;
		else
			return (y < that.y);
	}

	bool operator == (const vPoint& that) const
	{
		return (x == that.x) && (y == that.y);
	}

	bool operator != (const vPoint& that) const
	{
		return !(*this == that);
	}

protected:
	int     x;
	int     y;
};

//  several types of containers to store two dimensional points
typedef std::vector<vPoint>      VectorPoints;
typedef std::deque<vPoint>       DequePoints;
typedef std::list<vPoint>        ListPoints;
typedef std::set<vPoint>         SetPoints;

//  
//  the namespace VoronoiDemo provides a demonstration variant of 
//  the nearest neighbor search in an ordered set of two dimensional points; 
//  the algorithm has square root computational complexity, on average; 
//  
//  the algorithm is parameterized on types of containers, 
//  iterators and supporting algorithms; a user algorithm 
//  can take advantage of the interchangeable C++ standard 
//  containers and algorithms;
//
//  the performance test emulates the computation of 
//  the distance transform; the performance of the developed 
//  algorithm can be compared with the performance of 
//  the brute force algorithm; 
//  
//  the code of the namespace VoronoiDemo has been written  
//  to avoid complications associated with numeric errors of 
//  floating data types in comparison operations; 
//  the code uses integer type for X and Y coordinates of 
//  two dimensional points; in addition to this, instead of 
//  distance between two points, it calculates squared distance, 
//  which also takes advantage of the exact integer value; 
//  
class VoronoiDemo
{
	//  type for non-negative values of DistanceSquared()  
	typedef unsigned int    uint;
public:
	cv::Mat outImage;
	std::vector<vPoint> test_points;
	VoronoiDemo(void) {}

	uint DistanceSquared
	(
		vPoint const& pnt_a,
		vPoint const& pnt_b
	)
	{
		int     x = pnt_b.X() - pnt_a.X();
		int     y = pnt_b.Y() - pnt_a.Y();
		uint    d = static_cast<uint>(x * x + y * y);

		return d;
	}


	//  struct LowerBoundMemberFunction is a 
	//  function object for STL containers, such as std::set<T>, 
	//  that support member functions lower_bound(); 
	template < typename _OrderedSet >
	struct LowerBoundMemberFunction
	{
		typedef typename _OrderedSet::const_iterator  CtIterator;

		LowerBoundMemberFunction() { };

		CtIterator
			operator ( )
			(
				_OrderedSet const& set_points,
				vPoint const& pnt
				) const
		{
			CtIterator  iter_res = set_points.lower_bound(pnt);
			return iter_res;
		}
	};


	//  struct LowerBoundSTDAlgorithm is a 
	//  function object for STL sequence containers 
	//  that store ordered elements; 
	//  for the best performance requires a container 
	//  with random access iterators;
	template < typename _OrderedContainer >
	struct LowerBoundSTDAlgorithm
	{
		typedef typename _OrderedContainer::const_iterator  CtIterator;

		LowerBoundSTDAlgorithm() { };

		CtIterator
			operator ( )
			(
				_OrderedContainer const& ordered_points,
				vPoint const& pnt
				) const
		{
			CtIterator  iter_begin = ordered_points.begin();
			CtIterator  iter_end = ordered_points.end();
			CtIterator  iter_res = std::lower_bound(iter_begin, iter_end, pnt);
			return iter_res;
		}
	};


	//  the function FindForward() is a helper 
	//  for the function MinDistanceOrderedSet() 
	template < typename _CtIterator >
	void FindForward
	(
		vPoint const& pnt,
		_CtIterator     it_cur,
		_CtIterator     it_end,
		uint& dist_min
	)
	{
		uint        dist_cur = 0;
		uint        dist_x = 0;

		while (it_cur != it_end)
		{
			dist_cur = DistanceSquared(*it_cur, pnt);
			dist_x = (it_cur->X() - pnt.X()) * (it_cur->X() - pnt.X());

			if (dist_cur < dist_min)
				dist_min = dist_cur;

			if (dist_x > dist_min)
				break;

			++it_cur;
		}
	}


	//  the function FindBackward() is a helper 
	//  for the function MinDistanceOrderedSet(), 
	//  generally, it is NOT safe if container is empty;
	template < typename _CtIterator >
	void FindBackward
	(
		vPoint const& pnt,
		_CtIterator     it_cur,
		_CtIterator     it_begin,
		uint& dist_min
	)
	{
		uint        dist_cur = 0;
		uint        dist_x = 0;

		do
		{
			//  it is safe if input ( it_cur == container.end() )  
			//  and container is NOT empty 
			--it_cur;

			dist_cur = DistanceSquared(*it_cur, pnt);
			dist_x = (it_cur->X() - pnt.X()) * (it_cur->X() - pnt.X());

			if (dist_cur < dist_min)
				dist_min = dist_cur;

			if (dist_x > dist_min)
				break;
		} while (it_cur != it_begin);
	}


	//  the function MinDistanceOrderedSet() implements  
	//  the nearest neighbor search in an ordered set of points, 
	//  its average computational complexity - O ( sqrt(N) ) ,
	//  where N is the number of points in the set; 
	//  
	//  the template parameter <_OrderedSet> represents either 
	//  an ordered set or an ordered sequence of two-dimensional points; 
	//  both std::vector<T> and std::set<T> can be used as template arguments; 
	//  
	//  the template parameter <_FuncLowBound> represents an algorithm 
	//  that finds for an input point its lower bound in 
	//  either an ordered set or an ordered sequence of two dimensional points; 
	//  the namespace VoronoiDemo provides two function objects of this algorithm: 
	//  LowerBoundMemberFunction and LowerBoundSTDAlgorithm; 
	//  to achieve the specified computational complexity 
	//  an object of LowerBoundSTDAlgorithm should be used 
	//  with a container that supports random access iterators; 
	//
	//  for examples how to use the function MinDistanceOrderedSet(), 
	//  see the code below in this file; 
	//  
	template < typename _OrderedSet, typename _FuncLowBound >
	uint  MinDistanceOrderedSet
	(
		_OrderedSet const& set_points,
		_FuncLowBound           find_LB,
		vPoint const& pnt
	)
	{
		typedef typename _OrderedSet::const_iterator  CtIterator;

		uint        dist_min = UINT_MAX;
		CtIterator  iter_begin = set_points.begin();
		CtIterator  iter_end = set_points.end();
		//  call lower boundary algorithm through a function object
		CtIterator  iter_forw = find_LB(set_points, pnt);
		CtIterator  iter_back = iter_forw;

		bool        move_forward = (iter_forw != iter_end);
		bool        move_backward = (iter_back != iter_begin);

		if (move_forward)
			FindForward(pnt, iter_forw, iter_end, dist_min);
		if (move_backward)
			FindBackward(pnt, iter_back, iter_begin, dist_min);

		return dist_min;
	}


	//  the function TestOrderedSet() tests the efficiency of 
	//  the nearest neighbor search in an ordered set of points;
	//
	//  this test emulates the computation of the distance transform; 
	//  it calculates the minimum distance from each point in 
	//  the given rectangle to a point in the input set; 
	//
	template < typename _OrderedSet, typename _FuncLowBound >
	cv::Mat TestOrderedSet(const int rect_width, const int rect_height)
	{
		_OrderedSet     set_points(test_points.begin(), test_points.end());
		_FuncLowBound   func_lower_bound;

		cv::Mat dist = cv::Mat(rect_height, rect_width, CV_32F);
		for (int x = 0; x < rect_width; ++x)
		{
			for (int y = 0; y < rect_height; ++y)
			{
				vPoint p(x, y);
				float nextVal = (float)MinDistanceOrderedSet(test_points, func_lower_bound, p);
				dist.at<float>(y, x) = nextVal;
			}
		}


		return dist;
	}


	//  the function Run: 
	//  the result is sorted and contains no duplicates,
	//  note that  test_points.size() <= n_points  ;
	void Run(int width, int height)
	{
		std::sort(test_points.begin(), test_points.end());

		//  remove duplicates 
		std::vector<vPoint>::iterator  it_new_end;
		it_new_end = std::unique(test_points.begin(), test_points.end());
		test_points.erase(it_new_end, test_points.end());

		outImage = TestOrderedSet< VectorPoints, LowerBoundSTDAlgorithm<VectorPoints>>(width, height);
	}
};


extern "C" __declspec(dllexport)
VoronoiDemo* VoronoiDemo_Open()
{
	VoronoiDemo* cPtr = new VoronoiDemo();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* VoronoiDemo_Close(VoronoiDemo* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* VoronoiDemo_Run(VoronoiDemo* cPtr, cv::Point* input, int pointCount, int width, int height)
{
	cPtr->test_points.clear();
	for (int i = 0; i < pointCount; ++i)
	{
		cv::Point pt = input[i];
		cPtr->test_points.push_back(vPoint(pt.x, pt.y));
	}

	cPtr->Run(width, height);
	return (int*)cPtr->outImage.data;
}




class PCA_NColor
{
private:


	double CDiff(const std::vector<uint8_t>& a, int start, const std::vector<uint8_t>& b, int startPal) {
		return (static_cast<int>(a[start + 0]) - static_cast<int>(b[startPal + 0])) * (static_cast<int>(a[start + 0]) - static_cast<int>(b[startPal + 0])) * 5 +
			(static_cast<int>(a[start + 1]) - static_cast<int>(b[startPal + 1])) * (static_cast<int>(a[start + 1]) - static_cast<int>(b[startPal + 1])) * 8 +
			(static_cast<int>(a[start + 2]) - static_cast<int>(b[startPal + 2])) * (static_cast<int>(a[start + 2]) - static_cast<int>(b[startPal + 2])) * 2;
	}

	// Convert an image to indexed form, using passed-in palette
	std::vector<uint8_t> RgbToIndex(const std::vector<uint8_t>& rgb, const std::vector<uint8_t>& pal, int nColor) {
		std::vector<uint8_t> answer(src.total());

		for (int i = 0; i < src.total(); ++i) {
			double best = CDiff(rgb, i * 3, pal, 0);
			int bestii = 0;

			for (int ii = 1; ii < nColor; ++ii) {
				double nextError = CDiff(rgb, i * 3, pal, ii * 3);
				if (nextError < best) {
					best = nextError;
					bestii = ii;
				}
			}

			answer[i] = static_cast<uint8_t>(bestii);
		}

		return answer;
	}
public:
	Mat src;
	std::vector<uint8_t> palettizedImage;
	PCA_NColor() {}
	void RunCPP(uint8_t* imagePtr, uint8_t* palettePtr, int desiredNcolors) {
		std::vector<uint8_t> rgb((uint8_t*)imagePtr, (uint8_t*)imagePtr + src.rows * src.cols * 3);
		std::vector<uint8_t> palette(palettePtr, (uint8_t*)(palettePtr + 256 * 3));

		palettizedImage = RgbToIndex(rgb, palette, desiredNcolors);
	}
};

extern "C" __declspec(dllexport)
PCA_NColor* PCA_NColor_Open() {
	PCA_NColor* cPtr = new PCA_NColor();
	return cPtr;
}

extern "C" __declspec(dllexport)
void PCA_NColor_Close(PCA_NColor* cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* PCA_NColor_RunCPP(PCA_NColor* cPtr, uint8_t* imagePtr, uint8_t* palettePtr, int rows, int cols, int desiredColors)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, imagePtr);
	cPtr->RunCPP(imagePtr, palettePtr, desiredColors);
	return (int*)&cPtr->palettizedImage[0];
}




#include "opencv2/bioinspired.hpp"


// https://docs.opencv.org/3.4/d3/d86/tutorial_bioinspired_retina_model.html
using namespace std;
using namespace  cv;
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
	Ptr<bioinspired::Retina> myRetina;
	Retina(int rows, int cols, bool _useLogSampling, float _samplingFactor)
	{
		useLogSampling = _useLogSampling;
		samplingFactor = _samplingFactor;
		src = Mat(rows, cols, CV_8UC3);
		if (useLogSampling)
		{
			myRetina = bioinspired::Retina::create(src.size(), true, bioinspired::RETINA_COLOR_BAYER, useLogSampling, samplingFactor, 10.0);
		}
		else// -> else allocate "classical" retina :
			myRetina = bioinspired::Retina::create(src.size());

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
		catch (const Exception& e)
		{
			std::cerr << "Error using Retina : " << e.what() << std::endl;
		}
	}
};

extern "C" __declspec(dllexport)
Retina* Retina_Basics_Open(int rows, int cols, int useLogSampling, float samplingFactor)
{
	Retina* cPtr = new Retina(rows, cols, useLogSampling, samplingFactor);
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Retina_Basics_Close(Retina* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Retina_Basics_Run(Retina* cPtr, int* bgrPtr, int rows, int cols, int* magno)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, bgrPtr);
	cPtr->Run();
	if (cPtr->useLogSampling)
		memcpy(magno, cPtr->retinaOutput_magno.data, int((rows * cols / (cPtr->samplingFactor * cPtr->samplingFactor))));
	else
		memcpy(magno, cPtr->retinaOutput_magno.data, rows * cols);
	return (int*)cPtr->retinaOutput_parvo.data;
}





#ifndef INCLUDE_RBF
#define INCLUDE_RBF
#include <math.h>
#include <string.h>
#define QX_DEF_CHAR_MAX 255

/* ======================================================================

RecursiveBF: A lightweight library for recursive bilateral filtering.

-------------------------------------------------------------------------

Intro:      Recursive bilateral filtering (developed by Qingxiong Yang)
			is pretty fast compared with most edge-preserving filtering
			methods.

			-   computational complexity is linear in both input size and
				dimensionality
			-   takes about 43 ms to process a one mega-pixel color image
				(i7 1.8GHz & 4GB memory)
			-   about 18x faster than Fast high-dimensional filtering
				using the permutohedral lattice
			-   about 86x faster than Gaussian kd-trees for fast high-
				dimensional filtering


Usage:      // ----------------------------------------------------------
			// Basic Usage
			// ----------------------------------------------------------

			unsigned char * img = ...;                    // input image
			unsigned char * img_out = 0;            // output image
			int width = ..., height = ..., channel = ...; // image size
			recursive_bf(img, img_out,
						 sigma_spatial, sigma_range,
						 width, height, channel);

			// ----------------------------------------------------------
			// Advanced: using external buffer for better performance
			// ----------------------------------------------------------

			unsigned char * img = ...;                    // input image
			unsigned char * img_out = 0;            // output image
			int width = ..., height = ..., channel = ...; // image size
			float * buffer = new float[                   // external buf
								 ( width * height* channel
								 + width * height
								 + width * channel
								 + width) * 2];
			recursive_bf(img, img_out,
						 sigma_spatial, sigma_range,
						 width, height, channel,
						 buffer);
			delete[] buffer;


Notice:     Large sigma_spatial/sigma_range parameter may results in
			visible artifact which can be removed by an additional
			filter with small sigma_spatial/sigma_range parameter.

-------------------------------------------------------------------------

Reference:  Qingxiong Yang, Recursive Bilateral Filtering,
			European Conference on Computer Vision (ECCV) 2012, 399-413.

====================================================================== */

void recursive_bf(
	unsigned char* img_in,
	unsigned char*& img_out,
	float sigma_spatial, float sigma_range,
	int width, int height, int channel,
	float* buffer /*= 0*/);

// ----------------------------------------------------------------------

void _recursive_bf(
	unsigned char* img,
	float sigma_spatial, float sigma_range,
	int width, int height, int channel,
	float* buffer = 0)
{
	const int width_height = width * height;
	const int width_channel = width * channel;
	const int width_height_channel = width * height * channel;

	bool is_buffer_internal = (buffer == 0);
	if (is_buffer_internal)
		buffer = new float[(width_height_channel + width_height
			+ width_channel + width) * 2];

	float* img_out_f = buffer;
	float* img_temp = &img_out_f[width_height_channel];
	float* map_factor_a = &img_temp[width_height_channel];
	float* map_factor_b = &map_factor_a[width_height];
	float* slice_factor_a = &map_factor_b[width_height];
	float* slice_factor_b = &slice_factor_a[width_channel];
	float* line_factor_a = &slice_factor_b[width_channel];
	float* line_factor_b = &line_factor_a[width];

	//compute a lookup table
	float range_table[QX_DEF_CHAR_MAX + 1];
	float inv_sigma_range = 1.0f / (sigma_range * QX_DEF_CHAR_MAX);
	for (int i = 0; i <= QX_DEF_CHAR_MAX; i++)
		range_table[i] = static_cast<float>(exp(-i * inv_sigma_range));

	float alpha = static_cast<float>(exp(-sqrt(2.0) / (sigma_spatial * width)));
	float ypr, ypg, ypb, ycr, ycg, ycb;
	float fp, fc;
	float inv_alpha_ = 1 - alpha;
	for (int y = 0; y < height; y++)
	{
		float* temp_x = &img_temp[y * width_channel];
		unsigned char* in_x = &img[y * width_channel];
		unsigned char* texture_x = &img[y * width_channel];
		*temp_x++ = ypr = *in_x++;
		*temp_x++ = ypg = *in_x++;
		*temp_x++ = ypb = *in_x++;
		unsigned char tpr = *texture_x++;
		unsigned char tpg = *texture_x++;
		unsigned char tpb = *texture_x++;

		float* temp_factor_x = &map_factor_a[y * width];
		*temp_factor_x++ = fp = 1;

		// from left to right
		for (int x = 1; x < width; x++)
		{
			unsigned char tcr = *texture_x++;
			unsigned char tcg = *texture_x++;
			unsigned char tcb = *texture_x++;
			unsigned char dr = abs(tcr - tpr);
			unsigned char dg = abs(tcg - tpg);
			unsigned char db = abs(tcb - tpb);
			int range_dist = (((dr << 1) + dg + db) >> 2);
			float weight = range_table[range_dist];
			float alpha_ = weight * alpha;
			*temp_x++ = ycr = inv_alpha_ * (*in_x++) + alpha_ * ypr;
			*temp_x++ = ycg = inv_alpha_ * (*in_x++) + alpha_ * ypg;
			*temp_x++ = ycb = inv_alpha_ * (*in_x++) + alpha_ * ypb;
			tpr = tcr; tpg = tcg; tpb = tcb;
			ypr = ycr; ypg = ycg; ypb = ycb;
			*temp_factor_x++ = fc = inv_alpha_ + alpha_ * fp;
			fp = fc;
		}
		*--temp_x; *temp_x = 0.5f * ((*temp_x) + (*--in_x));
		*--temp_x; *temp_x = 0.5f * ((*temp_x) + (*--in_x));
		*--temp_x; *temp_x = 0.5f * ((*temp_x) + (*--in_x));
		tpr = *--texture_x;
		tpg = *--texture_x;
		tpb = *--texture_x;
		ypr = *in_x; ypg = *in_x; ypb = *in_x;

		*--temp_factor_x; *temp_factor_x = 0.5f * ((*temp_factor_x) + 1);
		fp = 1;

		// from right to left
		for (int x = width - 2; x >= 0; x--)
		{
			unsigned char tcr = *--texture_x;
			unsigned char tcg = *--texture_x;
			unsigned char tcb = *--texture_x;
			unsigned char dr = abs(tcr - tpr);
			unsigned char dg = abs(tcg - tpg);
			unsigned char db = abs(tcb - tpb);
			int range_dist = (((dr << 1) + dg + db) >> 2);
			float weight = range_table[range_dist];
			float alpha_ = weight * alpha;

			ycr = inv_alpha_ * (*--in_x) + alpha_ * ypr;
			ycg = inv_alpha_ * (*--in_x) + alpha_ * ypg;
			ycb = inv_alpha_ * (*--in_x) + alpha_ * ypb;
			*--temp_x; *temp_x = 0.5f * ((*temp_x) + ycr);
			*--temp_x; *temp_x = 0.5f * ((*temp_x) + ycg);
			*--temp_x; *temp_x = 0.5f * ((*temp_x) + ycb);
			tpr = tcr; tpg = tcg; tpb = tcb;
			ypr = ycr; ypg = ycg; ypb = ycb;

			fc = inv_alpha_ + alpha_ * fp;
			*--temp_factor_x;
			*temp_factor_x = 0.5f * ((*temp_factor_x) + fc);
			fp = fc;
		}
	}
	alpha = static_cast<float>(exp(-sqrt(2.0) / (sigma_spatial * height)));
	inv_alpha_ = 1 - alpha;
	float* ycy, * ypy, * xcy;
	unsigned char* tcy, * tpy;
	memcpy(img_out_f, img_temp, sizeof(float) * width_channel);

	float* in_factor = map_factor_a;
	float* ycf, * ypf, * xcf;
	memcpy(map_factor_b, in_factor, sizeof(float) * width);
	for (int y = 1; y < height; y++)
	{
		tpy = &img[(y - 1) * width_channel];
		tcy = &img[y * width_channel];
		xcy = &img_temp[y * width_channel];
		ypy = &img_out_f[(y - 1) * width_channel];
		ycy = &img_out_f[y * width_channel];

		xcf = &in_factor[y * width];
		ypf = &map_factor_b[(y - 1) * width];
		ycf = &map_factor_b[y * width];
		for (int x = 0; x < width; x++)
		{
			unsigned char dr = abs((*tcy++) - (*tpy++));
			unsigned char dg = abs((*tcy++) - (*tpy++));
			unsigned char db = abs((*tcy++) - (*tpy++));
			int range_dist = (((dr << 1) + dg + db) >> 2);
			float weight = range_table[range_dist];
			float alpha_ = weight * alpha;
			for (int c = 0; c < channel; c++)
				*ycy++ = inv_alpha_ * (*xcy++) + alpha_ * (*ypy++);
			*ycf++ = inv_alpha_ * (*xcf++) + alpha_ * (*ypf++);
		}
	}
	int h1 = height - 1;
	ycf = line_factor_a;
	ypf = line_factor_b;
	memcpy(ypf, &in_factor[h1 * width], sizeof(float) * width);
	for (int x = 0; x < width; x++)
		map_factor_b[h1 * width + x] = 0.5f * (map_factor_b[h1 * width + x] + ypf[x]);

	ycy = slice_factor_a;
	ypy = slice_factor_b;
	memcpy(ypy, &img_temp[h1 * width_channel], sizeof(float) * width_channel);
	int k = 0;
	for (int x = 0; x < width; x++) {
		for (int c = 0; c < channel; c++) {
			int idx = (h1 * width + x) * channel + c;
			img_out_f[idx] = 0.5f * (img_out_f[idx] + ypy[k++]) / map_factor_b[h1 * width + x];
		}
	}

	for (int y = h1 - 1; y >= 0; y--)
	{
		tpy = &img[(y + 1) * width_channel];
		tcy = &img[y * width_channel];
		xcy = &img_temp[y * width_channel];
		float* ycy_ = ycy;
		float* ypy_ = ypy;
		float* out_ = &img_out_f[y * width_channel];

		xcf = &in_factor[y * width];
		float* ycf_ = ycf;
		float* ypf_ = ypf;
		float* factor_ = &map_factor_b[y * width];
		for (int x = 0; x < width; x++)
		{
			unsigned char dr = abs((*tcy++) - (*tpy++));
			unsigned char dg = abs((*tcy++) - (*tpy++));
			unsigned char db = abs((*tcy++) - (*tpy++));
			int range_dist = (((dr << 1) + dg + db) >> 2);
			float weight = range_table[range_dist];
			float alpha_ = weight * alpha;

			float fcc = inv_alpha_ * (*xcf++) + alpha_ * (*ypf_++);
			*ycf_++ = fcc;
			*factor_ = 0.5f * (*factor_ + fcc);

			for (int c = 0; c < channel; c++)
			{
				float ycc = inv_alpha_ * (*xcy++) + alpha_ * (*ypy_++);
				*ycy_++ = ycc;
				*out_ = 0.5f * (*out_ + ycc) / (*factor_);
				*out_++;
			}
			*factor_++;
		}
		memcpy(ypy, ycy, sizeof(float) * width_channel);
		memcpy(ypf, ycf, sizeof(float) * width);
	}

	for (int i = 0; i < width_height_channel; ++i)
		img[i] = static_cast<unsigned char>(img_out_f[i]);

	if (is_buffer_internal)
		delete[] buffer;
}


void recursive_bf(
	unsigned char* img_in,
	unsigned char*& img_out,
	float sigma_spatial, float sigma_range,
	int width, int height, int channel,
	float* buffer = 0)
{
	if (img_out == 0)
		img_out = new unsigned char[width * height * channel];
	for (int i = 0; i < width * height * channel; ++i)
		img_out[i] = img_in[i];
	_recursive_bf(img_out, sigma_spatial, sigma_range, width, height, channel, buffer);
}

#endif // INCLUDE_RBF


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

extern "C" __declspec(dllexport)
recursiveBilateralFilter* RecursiveBilateralFilter_Open()
{
	recursiveBilateralFilter* cPtr = new recursiveBilateralFilter();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* RecursiveBilateralFilter_Close(recursiveBilateralFilter* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* RecursiveBilateralFilter_Run(recursiveBilateralFilter* cPtr, int* bgrPtr, int rows, int cols, int recursions)
{
	cPtr->src = Mat(rows, cols, CV_8U, bgrPtr);
	cPtr->recursions = recursions;
	cPtr->RecursiveBilateralFilter_Run();
	return (int*)cPtr->src.data;
}





#define MAX_NUM_SCALES (6)
class Salience
{
private:
	int ii_ColWidth;
	Mat integralImage;
	Mat intensityScaledOn[MAX_NUM_SCALES];
	Mat intensityScaledOff[MAX_NUM_SCALES];
	Mat intensityOn;
	Mat intensityOff;

	float getMean(Mat src, Point PixArg, int kernelsize, int PixelVal)
	{
		Point P1, P2;
		int value;

		P1.x = PixArg.x - kernelsize + 1;
		P1.y = PixArg.y - kernelsize + 1;
		P2.x = PixArg.x + kernelsize + 1;
		P2.y = PixArg.y + kernelsize + 1;

		if (P1.x < 0)
			P1.x = 0;
		else if (P1.x > src.cols - 1)
			P1.x = src.cols - 1;
		if (P2.x < 0)
			P2.x = 0;
		else if (P2.x > src.cols - 1)
			P2.x = src.cols - 1;
		if (P1.y < 0)
			P1.y = 0;
		else if (P1.y > src.rows - 1)
			P1.y = src.rows - 1;
		if (P2.y < 0)
			P2.y = 0;
		else if (P2.y > src.rows - 1)
			P2.y = src.rows - 1;

		// we use the integral image to compute fast features
#if 0
		value = src.at<int>(P2.y, P2.x) + src.at<int>(P1.y, P1.x) - src.at<int>(P2.y, P1.x) - src.at<int>(P1.y, P2.x);
#else
	// Try the other side of this ifdef.  It is a lot slower to use the "at" method.
		value = ((int*)(src.data + ii_ColWidth * P2.y))[P2.x] +
			((int*)(src.data + ii_ColWidth * P1.y))[P1.x] -
			((int*)(src.data + ii_ColWidth * P2.y))[P1.x] -
			((int*)(src.data + ii_ColWidth * P1.y))[P2.x];
#endif
		value = (value - PixelVal) / (((P2.x - P1.x) * (P2.y - P1.y)) - 1);
		return (float)value;
	}

	void getIntensityScaled(Mat gray, int i, int kernelsize)
	{
		float value, meanOn, meanOff;
		Point point;
		int PixelVal;
		for (int x = 0; x < gray.rows; x++) {
			uchar* on = ((uchar*)(intensityScaledOn[i].data + gray.cols * x));
			uchar* off = ((uchar*)(intensityScaledOff[i].data + gray.cols * x));
			for (int y = 0; y < gray.cols; y++) {
				PixelVal = ((uchar*)(gray.data + gray.cols * x))[y];
				value = getMean(integralImage, Point(y, x), kernelsize, PixelVal);

				meanOn = PixelVal - value;
				meanOff = value - PixelVal;

				if (meanOn > 0) on[y] = (uchar)meanOn;
				if (meanOff > 0) off[y] = (uchar)meanOff;
			}
		}
	}

public:
	Mat src, dst;
	int numScales;
	bool matsReady = false;
	Salience()
	{
	}

	void allocateMats(int rows, int cols)
	{
		ii_ColWidth = (cols + 1) * sizeof(int);
		Mat tmp(rows, cols, CV_8U);
		tmp.setTo(0);
		for (int i = 0; i < MAX_NUM_SCALES; ++i) {
			intensityScaledOn[i] = tmp.clone();
			intensityScaledOff[i] = tmp.clone();
		}
		matsReady = true;
	}
	Mat calcIntensity()
	{
		int kernelsize[] = { 3 * 4, 3 * 4 * 2, 3 * 4 * 2 * 2, 7 * 4, 7 * 4 * 2, 7 * 4 * 2 * 2 };

		// Calculate integral image, only once.
		integral(src, integralImage);

		for (int i = 0; i < numScales; ++i) {
			intensityScaledOn[i].setTo(0);
			intensityScaledOff[i].setTo(0);
			getIntensityScaled(src, i, kernelsize[i]);
		}

		Mat tmp32f(src.rows, src.cols, CV_32F), tmp;
		tmp32f.setTo(0);
		intensityOn = tmp32f.clone();
		intensityOff = tmp32f.clone();
		for (int i = 0; i < numScales; ++i) {
			intensityScaledOn[i].convertTo(tmp, CV_32F);
			intensityOn += tmp;
			intensityScaledOff[i].convertTo(tmp, CV_32F);
			intensityOff += tmp;
		}
		return intensityOn + intensityOff;
	}
};

extern "C" __declspec(dllexport)
Salience* Salience_Open()
{
	Salience* cPtr = new Salience();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Salience_Close(Salience* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Salience_Run(Salience* cPtr, int numScales, int* grayInput, int rows, int cols)
{
	cPtr->numScales = numScales;
	if (cPtr->matsReady == true) if (cPtr->src.rows != rows && cPtr->src.cols != cols) cPtr->allocateMats(rows, cols);
	cPtr->src = Mat(rows, cols, CV_8U, grayInput);
	if (cPtr->matsReady == false) cPtr->allocateMats(rows, cols);
	Mat gray32f = cPtr->calcIntensity();

	normalize(gray32f, gray32f, 0, 255, NORM_MINMAX, CV_32F);
	gray32f.convertTo(cPtr->dst, CV_8U);
	return (int*)cPtr->dst.data;
}





#define BLUR_RADIUS 3
#define PATHS_PER_SCAN 8
#define SMALL_PENALTY 3
#define LARGE_PENALTY 20
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

void calculateCostHamming(Mat& firstImage, Mat& secondImage, int disparityRange, unsigned long*** C, unsigned long*** S)
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


void aggregation(Mat& leftImage, Mat& rightImage, int disparityRange, unsigned long*** C, unsigned long*** S, unsigned int**** A)
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
	unsigned long*** C; // pixel cost array W x H x D
	unsigned long*** S; // aggregated cost array W x H x D
	unsigned int**** A; // single path cost array path_nos x W x H x D
public:
	Mat leftImage, rightImage;
	int disparityRange = 3;
	Mat disparityMapstage2;

	SemiGlobalMatching(int rows, int cols, int _disparityRange)
	{
		disparityRange = _disparityRange;
		// allocate cost arrays
		C = new unsigned long** [rows];
		S = new unsigned long** [rows];
		for (int row = 0; row < rows; ++row) {
			C[row] = new unsigned long* [cols];
			S[row] = new unsigned long* [cols];
			for (int col = 0; col < cols; ++col) {
				C[row][col] = new unsigned long[disparityRange]();
				S[row][col] = new unsigned long[disparityRange]();
			}
		}

		A = new unsigned int*** [PATHS_PER_SCAN];
		for (int path = 0; path < PATHS_PER_SCAN; ++path) {
			A[path] = new unsigned int** [rows];
			for (int row = 0; row < rows; ++row) {
				A[path][row] = new unsigned int* [cols];
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
SemiGlobalMatching* SemiGlobalMatching_Open(int rows, int cols, int disparityRange)
{
	SemiGlobalMatching* SemiGlobalMatchingPtr = new SemiGlobalMatching(rows, cols, disparityRange);
	return SemiGlobalMatchingPtr;
}

extern "C" __declspec(dllexport)
int* SemiGlobalMatching_Close(SemiGlobalMatching* SemiGlobalMatchingPtr)
{
	delete SemiGlobalMatchingPtr;
	return (int*)0;
}

// https://github.com/epiception/SGM-Census
extern "C" __declspec(dllexport)
int* SemiGlobalMatching_Run(SemiGlobalMatching* SemiGlobalMatchingPtr, int* leftPtr, int* rightPtr, int rows, int cols)
{
	SemiGlobalMatchingPtr->leftImage = Mat(rows, cols, CV_8U, leftPtr);
	SemiGlobalMatchingPtr->rightImage = Mat(rows, cols, CV_8U, leftPtr);
	SemiGlobalMatchingPtr->Run();

	return (int*)SemiGlobalMatchingPtr->disparityMapstage2.data;
}




#include <opencv2/tracking.hpp>
#include <opencv2/core/ocl.hpp>



using namespace std;
using namespace  cv;
class Tracker_Basics
{
private:
public:
	Ptr<Tracker> tracker;
	bool bboxInitialized = false;
	Mat src;
	Rect tRect; // tracker output box.
	Tracker_Basics(int trackType)
	{
		// switch is based on the older tracking alternatives.  Some are disabled in the user interface for now.
		switch (trackType)
		{
		case 1: // MIL
			tracker = TrackerMIL::create();
			break;
		case 2: // KCF
			tracker = TrackerKCF::create();
			break;
		case 5: // GOTURN
			tracker = TrackerGOTURN::create();
			break;
		case 7: // CSRT
			tracker = TrackerCSRT::create();
			break;
		default: // MIL
			tracker = TrackerMIL::create();
			break;
		}
	}
	void Run(Rect bbox) {
		if (bboxInitialized == false)
		{
			bboxInitialized = true;
			tracker->init(src, bbox);
		}
		else {
			bool ok = tracker->update(src, bbox);
			if (ok)
			{
				rectangle(src, bbox, Scalar(255, 255, 255), 1, 1);
			}
			else {
				putText(src, "Tracking failure detected", Point(100, 80), FONT_HERSHEY_SIMPLEX, 0.75, Scalar(0, 0, 255), 2);
				bboxInitialized = false;
			}

			tRect = Rect(bbox.x, bbox.y, bbox.width, bbox.height);
		}
	}
};

extern "C" __declspec(dllexport)
Tracker_Basics* Tracker_Basics_Open(int trackType) {
	Tracker_Basics* cPtr = new Tracker_Basics(trackType);
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Tracker_Basics_Close(Tracker_Basics* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Tracker_Basics_Run(Tracker_Basics* cPtr, int* bgrPtr, int rows, int cols, int x, int y, int w, int h)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, bgrPtr);
	Rect bbox(x, y, w, h);
	cPtr->Run(bbox);
	return (int*)&cPtr->tRect;
}