#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "cvHmm.h"
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"
#include "opencv2/bgsegm.hpp"
#include "opencv2/photo.hpp"
#include <map>
#include <opencv2/ml.hpp>
#include "harrisDetector.h"
#include <opencv2/plot.hpp>
#include "opencv2/ccalib/randpattern.hpp"
#include "opencv2/xphoto/oilpainting.hpp"
#include "CPP_Parent.h"

#include "Pragmalibs.h"

using namespace std;
using namespace  cv;
using namespace bgsegm;
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




#ifndef VIDEOSTAB_H
#define VIDEOSTAB_H

using namespace  cv;

class VideoStab
{
public:
	VideoStab();
	Mat errScale, qScale, rScale, sScale, sumScale;
	Mat lastFrame;
	Mat gray;
	int k;

	const int HORIZONTAL_BORDER_CROP = 30;

	Mat affine;
	Mat smoothedMat;
	Mat smoothedFrame;

	Mat stabilize(Mat rgb);
	void Kalman_Filter();
};

#endif // VIDEOSTAB_H







class EdgeDraw_Basics
{
private:
public:
	Mat src, dst;
	vector<Vec4f> lines;
	Ptr<EdgeDrawing> ed;
	EdgeDraw_Basics()
	{
		ed = createEdgeDrawing();
		ed->params.EdgeDetectionOperator = EdgeDrawing::SOBEL;
		ed->params.GradientThresholdValue = 38;
		ed->params.AnchorThresholdValue = 8;
	}
	void RunCPP(int lineWidth) {
		ed->detectEdges(src);

		ed->detectLines(lines);

		dst.setTo(0);
		for (size_t i = 0; i < lines.size(); i++)
		{
			Point2f p1 = Point2f(lines[i].val[0], lines[i].val[1]);
			Point2f p2 = Point2f(lines[i].val[2], lines[i].val[3]);
			line(dst, p1, p2, 255, lineWidth);
		}
	}
};

extern "C" __declspec(dllexport)
EdgeDraw_Basics* EdgeDraw_Basics_Open() {
	EdgeDraw_Basics* cPtr = new EdgeDraw_Basics();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Basics_Close(EdgeDraw_Basics* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Basics_RunCPP(EdgeDraw_Basics* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
	if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
	cPtr->RunCPP(lineWidth);
	return (int*)cPtr->dst.data;
}








class EdgeDraw
{
private:
public:
	Mat src, dst;
	Ptr<EdgeDraw_Basics> eDraw;
	vector<Vec4f> lines;
	EdgeDraw()
	{
		eDraw = new EdgeDraw_Basics();
	}
	void RunCPP(int lineWidth) {
		eDraw->ed->detectEdges(src);
		eDraw->ed->detectLines(lines);
		vector< vector<Point> > segments = eDraw->ed->getSegments();

		dst.setTo(0);
		for (size_t i = 0; i < segments.size(); i++)
		{
			const Point* pts = &segments[i][0];
			int n = (int)segments[i].size();
			float distance = sqrt((pts[0].x - pts[n - 1].x) * (pts[0].x - pts[n - 1].x) + (pts[0].y - pts[n - 1].y) * (pts[0].y - pts[n - 1].y));
			bool drawClosed = distance < 10;
			polylines(dst, &pts, &n, 1, drawClosed, Scalar(255, 255, 255), lineWidth, LINE_AA);
		}
	}
};

extern "C" __declspec(dllexport)
EdgeDraw* EdgeDraw_Edges_Open() {
	EdgeDraw* cPtr = new EdgeDraw();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Edges_Close(EdgeDraw* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_RunCPP(EdgeDraw* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
	if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
	cPtr->RunCPP(lineWidth);
	return (int*)cPtr->dst.data;
}









class EdgeDraw_Lines
{
private:
public:
	Mat src, dst;
	Ptr<EdgeDraw_Basics> eDraw;
	vector<Vec4f> lines;
	EdgeDraw_Lines()
	{
		eDraw = new EdgeDraw_Basics();
	}
	void RunCPP(int lineWidth) {
		eDraw->ed->detectEdges(src);
		eDraw->ed->detectLines(lines);
	}
};

extern "C" __declspec(dllexport)
EdgeDraw_Lines* EdgeDraw_Lines_Open() {
	EdgeDraw_Lines* cPtr = new EdgeDraw_Lines();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Lines_Close(EdgeDraw_Lines* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int EdgeDraw_Lines_Count(EdgeDraw_Lines* cPtr)
{
	return int(cPtr->lines.size());
}

extern "C" __declspec(dllexport)
int* EdgeDraw_Lines_RunCPP(EdgeDraw_Lines* cPtr, int* dataPtr, int rows, int cols, int lineWidth)
{
	if (cPtr->dst.rows == 0) cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
	cPtr->RunCPP(lineWidth);
	return (int*)&cPtr->lines[0];
}





class Agast
{
private:
public:
	Mat src, dst;
	std::vector<Point2f> points;

	Agast() {}
	void Run(int threshold) {
		std::vector<KeyPoint> keypoints;
		static Ptr<AgastFeatureDetector> agastFD = AgastFeatureDetector::create(threshold, true, AgastFeatureDetector::OAST_9_16);
		agastFD->detect(src, keypoints);
		points.clear();
		for (KeyPoint kpt : keypoints)
		{
			points.push_back(Point2f(round(kpt.pt.x), round(kpt.pt.y)));
		}
	}
};

extern "C" __declspec(dllexport) int Agast_Count(Agast* cPtr)
{
	return (int)cPtr->points.size();
}
extern "C" __declspec(dllexport)
Agast* Agast_Open()
{
	Agast* cPtr = new Agast();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Agast_Close(Agast* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Agast_Run(Agast* cPtr, int* bgrPtr, int rows, int cols, int threshold)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, bgrPtr);
	cPtr->Run(threshold);
	return (int*)&cPtr->points[0];
}







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

extern "C" __declspec(dllexport)
CitySolver* Annealing_Basics_Open(Point2f* cityPositions, int count)
{
	CitySolver* cPtr = new CitySolver();
	cPtr->cityPositions.assign(cityPositions, cityPositions + count);
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Annealing_Basics_Close(CitySolver* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
char* Annealing_Basics_Run(CitySolver* cPtr, int* cityOrder, int count)
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





class BGRPattern_Basics
{
private:
public:
	Mat src, dst;
	int classCount = 5;
	BGRPattern_Basics() {}
	void RunCPP() {
		for (int y = 0; y < src.rows; y++)
			for (int x = 0; x < src.cols; x++)
			{
				Vec3b vec = src.at<Vec3b>(y, x);
				int b = vec[0]; int g = vec[1]; int r = vec[2];
				if (b == g && g == r)
				{
					dst.at<uchar>(y, x) = 1;
				}
				else if (b <= g && g <= r)
				{
					dst.at<uchar>(y, x) = 2;
				}
				else if (b >= g && g >= r)
				{
					dst.at<uchar>(y, x) = 3;
				}
				else if (b >= g && g <= r)
				{
					dst.at<uchar>(y, x) = 4;
				}
				else if (b <= g && g >= r)
				{
					dst.at<uchar>(y, x) = classCount;
				}
			}
	}
};
extern "C" __declspec(dllexport)
BGRPattern_Basics* BGRPattern_Open() {
	BGRPattern_Basics* cPtr = new BGRPattern_Basics();
	return cPtr;
}
extern "C" __declspec(dllexport)
void BGRPattern_Close(BGRPattern_Basics* cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int BGRPattern_ClassCount(BGRPattern_Basics* cPtr)
{
	return cPtr->classCount;
}

extern "C" __declspec(dllexport)
int* BGRPattern_RunCPP(BGRPattern_Basics* cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
	cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->RunCPP();
	return (int*)cPtr->dst.data;
}







class BGSubtract_BGFG
{
private:
public:
	Ptr<BackgroundSubtractor> algo;
	Mat src, fgMask;
	BGSubtract_BGFG() {}
	void Run(double learnRate) {
		algo->apply(src, fgMask, learnRate);
	}
};

extern "C" __declspec(dllexport)
BGSubtract_BGFG* BGSubtract_BGFG_Open(int currMethod) {
	BGSubtract_BGFG* cPtr = new BGSubtract_BGFG();
	if (currMethod == 0)      cPtr->algo = createBackgroundSubtractorGMG(20, 0.7);
	else if (currMethod == 1) cPtr->algo = createBackgroundSubtractorCNT();
	else if (currMethod == 2) cPtr->algo = createBackgroundSubtractorKNN();
	else if (currMethod == 3) cPtr->algo = createBackgroundSubtractorMOG();
	else if (currMethod == 4) cPtr->algo = createBackgroundSubtractorMOG2();
	else if (currMethod == 5) cPtr->algo = createBackgroundSubtractorGSOC();
	else if (currMethod == 6) cPtr->algo = createBackgroundSubtractorLSBP();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* BGSubtract_BGFG_Close(BGSubtract_BGFG* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* BGSubtract_BGFG_Run(BGSubtract_BGFG* cPtr, int* bgrPtr, int rows, int cols, int channels, double learnRate)
{
	cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, bgrPtr);
	cPtr->Run(learnRate);
	return (int*)cPtr->fgMask.data;
}





class BGSubtract_Synthetic
{
private:
public:
	Ptr<bgsegm::SyntheticSequenceGenerator> gen;
	Mat src, output, fgMask;
	BGSubtract_Synthetic() {}
	void Run() {
	}
};

extern "C" __declspec(dllexport)
BGSubtract_Synthetic* BGSubtract_Synthetic_Open(int* bgrPtr, int rows, int cols, LPSTR fgFilename, double amplitude, double magnitude,
	double wavespeed, double objectspeed)
{
	BGSubtract_Synthetic* cPtr = new BGSubtract_Synthetic();
	Mat bg = Mat(rows, cols, CV_8UC3, bgrPtr);
	Mat fg = imread(fgFilename, IMREAD_COLOR);
	resize(fg, fg, Size(10, 10)); // adjust the object size here...
	cPtr->gen = bgsegm::createSyntheticSequenceGenerator(bg, fg, amplitude, magnitude, wavespeed, objectspeed);
	cPtr->gen->getNextFrame(cPtr->output, cPtr->fgMask);
	return cPtr;
}

extern "C" __declspec(dllexport)
int* BGSubtract_Synthetic_Close(BGSubtract_Synthetic* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* BGSubtract_Synthetic_Run(BGSubtract_Synthetic* cPtr)
{
	cPtr->gen->getNextFrame(cPtr->output, cPtr->fgMask);
	return (int*)cPtr->output.data;
}




extern "C" __declspec(dllexport)
int* Corners_ShiTomasi(int* grayPtr, int rows, int cols, int blockSize, int apertureSize)
{
	static Mat dst;
	Mat gray = Mat(rows, cols, CV_8UC1, grayPtr);
	dst = Mat::zeros(gray.size(), CV_32FC1);
	/// Shi-Tomasi -- Using cornerMinEigenVal - can't access this from opencvSharp...
	cornerMinEigenVal(gray, dst, blockSize, apertureSize, BORDER_DEFAULT);
	return (int*)dst.data;
}







//// https://www.codeproject.com/Articles/5362105/Perceptual-Hash-based-Image-Comparison-Coded-in-pl
//double dctSimple(double* DCTMatrix, double* ImgMatrix, int N, int M)
//{
//	int i, j, u, v;
//	int cnt = 0;
//	double DCTsum = 0.0;
//	for (u = 0; u < N; ++u)
//	{
//		for (v = 0; v < M; ++v)
//		{
//			DCTMatrix[(u * N) + v] = 0;
//			for (i = 0; i < N; i++)
//			{
//				for (j = 0; j < M; j++)
//				{
//					DCTMatrix[(u * N) + v] += ImgMatrix[(i * N) + j]
//						* cos(CV_PI / ((double)N) * (i + 1. / 2.) * u)
//						* cos(CV_PI / ((double)M) * (j + 1. / 2.) * v);
//					cnt = cnt++;
//				}
//			}
//			DCTsum += DCTMatrix[(u * N) + v];
//		}
//	}
//	DCTsum -= DCTMatrix[0];
//	return   DCTsum / cnt;
//}
//
//
//
//
//
//
//int CoeffFlag = 0;
//double Coeff[32][32];
//
//// https://www.codeproject.com/Articles/5362105/Perceptual-Hash-based-Image-Comparison-Coded-in-pl
//double DCT(double* DCTMatrix, double* IMGMatrix)
//{
//	/*
//		Concerning: the expression
//		dctRCvalue+= IMGMatrix[(imgRow * RowMax) + imgCol]    // this is dependent on the IMAGE
//		* cos( PIover32 *(imgRow+0.5)*dctRow)    //  this is a set of FIXED values
//		* cos( PIover32 *(imgCol+0.5)*dctCol);   //  this is a set of FIXED values
//
//		Let us call the  2 sets of FIXED values   rCoeff and cCoeff
//		they both have the same set of values
//		=  cos (  PIover32  * ( x + 0.5 ) * y )   for x = 0 .. 31    and y = for x = 0 .. 31
//		= 32*32 distinct COSINE values
//		= we could calculate these COSINE values in advance, place them in an array, and (hopefully) speed things up by doing a simple look up .
//	*/
//#define  PIover32 0.09817477042
//#define  RowMax 32
//#define  ColMax 32    
//	int imgRow, imgCol;
//	int dctRow, dctCol;
//	int x, y;
//	int cnt = 0;
//	double DCTsum = 0.0;
//	double dctRCvalue = 0.0;
//
//	if (!CoeffFlag)
//	{
//		for (x = 0; x < 32; x++)
//		{
//			for (y = 0; y < 32; y++)
//			{
//				Coeff[x][y] = cos(PIover32 * (x + 0.5) * y);
//			}
//		}
//		CoeffFlag = 1;
//	}
//
//	for (dctRow = 0; dctRow < 8; dctRow++)
//	{
//		for (dctCol = 0; dctCol < 8; dctCol++)
//		{
//			dctRCvalue = 0;
//
//			for (imgRow = 0; imgRow < RowMax; imgRow++)
//			{
//				for (imgCol = 0; imgCol < ColMax; imgCol++)
//				{
//					dctRCvalue += IMGMatrix[(imgRow * RowMax) + imgCol]
//						* Coeff[imgRow][dctRow]    //    cos( PIover32 *(imgRow+0.5)*dctRow)   
//						* Coeff[imgCol][dctCol];  //    cos( PIover32 *(imgCol+0.5)*dctCol) ; 
//					cnt = cnt++;
//				}
//			}
//			DCTMatrix[(dctRow * RowMax) + dctCol] = dctRCvalue;
//			DCTsum += dctRCvalue;
//		}
//	}
//	DCTsum -= DCTMatrix[0];
//	return   DCTsum / cnt;
//}






class Denoise_Basics
{
private:
public:
	Mat dst;
	int frameCount = 5;
	std::vector<Mat> frames;
	Denoise_Basics() {}
	void Run() {
		dst = frames.back();
		fastNlMeansDenoisingMulti(frames, dst, 2, 1);
	}
};

extern "C" __declspec(dllexport)
Denoise_Basics* Denoise_Basics_Open(int frameCount) {
	Denoise_Basics* cPtr = new Denoise_Basics();
	cPtr->frameCount = frameCount;
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Denoise_Basics_Close(Denoise_Basics* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Denoise_Basics_Run(Denoise_Basics* cPtr, int* bufferPtr, int rows, int cols)
{
	Mat src = Mat(rows, cols, CV_8UC1, bufferPtr);
	cPtr->frames.push_back(src.clone());
	if (cPtr->frames.size() < cPtr->frameCount)
	{
		return (int*)src.data;
	}
	else {
		cPtr->Run();
		cPtr->frames.pop_back();
	}
	return(int*)cPtr->dst.data;
}






class Denoise_Pixels
{
private:
public:
	Mat src;
	int edgeCountAfter, edgeCountBefore;
	Denoise_Pixels() {}
	void RunCPP() {
		edgeCountBefore = 0;
		for (int y = 0; y < src.rows; y++)
			for (int x = 1; x < src.cols - 1; x++)
			{
				int last = src.at<uchar>(y, x - 1);
				int curr = src.at<uchar>(y, x);
				if (last != curr)
				{
					edgeCountBefore++;
					if (last == src.at<uchar>(y, x + 1))
						src.at<uchar>(y, x) = last;
				}
			}
		for (int y = 1; y < src.rows - 1; y++)
			for (int x = 0; x < src.cols; x++)
			{
				int last = src.at<uchar>(y - 1, x);
				int curr = src.at<uchar>(y, x);
				if (last != curr)
					if (last == src.at<uchar>(y + 1, x))
						src.at<uchar>(y, x) = last;
			}
		edgeCountAfter = 0;
		for (int y = 0; y < src.rows; y++)
			for (int x = 1; x < src.cols; x++)
			{
				int last = src.at<uchar>(y, x - 1);
				int curr = src.at<uchar>(y, x);
				if (last != curr) edgeCountAfter++;
			}
	}
};
extern "C" __declspec(dllexport)
Denoise_Pixels* Denoise_Pixels_Open() {
	Denoise_Pixels* cPtr = new Denoise_Pixels();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Denoise_Pixels_Close(Denoise_Pixels* cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int Denoise_Pixels_EdgeCountBefore(Denoise_Pixels* cPtr)
{
	return cPtr->edgeCountBefore;
}

extern "C" __declspec(dllexport)
int Denoise_Pixels_EdgeCountAfter(Denoise_Pixels* cPtr)
{
	return cPtr->edgeCountAfter;
}
extern "C" __declspec(dllexport)
int* Denoise_Pixels_RunCPP(Denoise_Pixels* cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
	cPtr->RunCPP();
	return (int*)cPtr->src.data;
}





class Density_2D
{
private:
public:
	Mat src, dst;
	Density_2D() {}
	float distanceTo(Point3f v1, Point3f v2)
	{
		return sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y) + (v1.z - v2.z) * (v1.z - v2.z));
	}
	void RunCPP(float zDistance) {
		dst.setTo(0);
		int offx[] = { -1, 0, 1, -1, 1, -1, 0, 1 };
		int offy[] = { -1, -1, -1, 0, 0, 1, 1, 1 };
		for (int y = 1; y < src.rows - 1; y++)
		{
			for (int x = 1; x < src.cols - 1; x++)
			{
				float z1 = src.at<float>(y, x);
				if (z1 == 0) continue;
				float d = 0.0f;
				for (int i = 0; i < 8; i++)
				{
					float z2 = src.at<float>(y + offy[i], x + offx[i]);
					if (z2 == 0) continue;
					d += abs(z1 - z2);
				}
				if (d < zDistance && d != 0.0f) dst.at<unsigned char>(y, x) = 255;
			}
		}
	}
};

extern "C" __declspec(dllexport)
Density_2D* Density_2D_Open() {
	Density_2D* cPtr = new Density_2D();
	return cPtr;
}

extern "C" __declspec(dllexport)
void Density_2D_Close(Density_2D* cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* Density_2D_RunCPP(Density_2D* cPtr, int* dataPtr, int rows, int cols, double zDistance)
{
	cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
	cPtr->RunCPP(zDistance);
	return (int*)cPtr->dst.data;
}







class Density_Count
{
private:
public:
	Mat src, dst;
	Density_Count() {}
	void RunCPP(int zCount) {
		dst.setTo(0);
		int offx[] = { -1, 0, 1, -1, 1, -1, 0, 1 };
		int offy[] = { -1, -1, -1, 0, 0, 1, 1, 1 };
		for (int y = 1; y < src.rows - 1; y++)
		{
			for (int x = 1; x < src.cols - 1; x++)
			{
				float z1 = src.at<float>(y, x);
				if (z1 == 0) continue;
				int count = 0;
				for (int i = 0; i < 8; i++)
				{
					float z2 = src.at<float>(y + offy[i], x + offx[i]);
					if (z2 > 0) count += 1;
				}
				if (count >= zCount) dst.at<unsigned char>(y, x) = 255;
			}
		}
	}
};

extern "C" __declspec(dllexport)
Density_Count* Density_Count_Open() {
	Density_Count* cPtr = new Density_Count();
	return cPtr;
}

extern "C" __declspec(dllexport)
void Density_Count_Close(Density_Count* cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* Density_Count_RunCPP(Density_Count* cPtr, int* dataPtr, int rows, int cols, int zCount)
{
	cPtr->dst = Mat(rows, cols, CV_8U);
	cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
	cPtr->RunCPP(zCount);
	return (int*)cPtr->dst.data;
}






class Depth_Colorizer
{
private:
public:
	Mat depth32f, output;
	int histSize = 255;
	Depth_Colorizer() {}
	void Run(float maxDepth)
	{
		float nearColor[3] = { 0, 1.0f, 1.0f };
		float farColor[3] = { 1.0f, 0, 0 };
		output = Mat(depth32f.size(), CV_8UC3);
		auto rgb = (unsigned char*)output.data;
		float* depthImage = (float*)depth32f.data;
		for (int i = 0; i < output.total(); i++)
		{
			float t = depthImage[i] / maxDepth;
			if (t > 0 && t <= 1)
			{
				*rgb++ = uchar(((1 - t) * nearColor[0] + t * farColor[0]) * 255);
				*rgb++ = uchar(((1 - t) * nearColor[1] + t * farColor[1]) * 255);
				*rgb++ = uchar(((1 - t) * nearColor[2] + t * farColor[2]) * 255);
			}
			else {
				*rgb++ = 0; *rgb++ = 0; *rgb++ = 0;
			}
		}
	}
};




class DepthXYZ
{
private:
public:
	Mat depth, depthxyz;
	float ppx, ppy, fx, fy;
	DepthXYZ(float _ppx, float _ppy, float _fx, float _fy)
	{
		ppx = _ppx; ppy = _ppy; fx = _fx; fy = _fy;
	}
	void GetImageCoordinates()
	{
		depthxyz = Mat(depth.rows, depth.cols, CV_32FC3);
#ifdef _DEBUG
		// #pragma omp parallel for  // doubles performance in debug mode but is much worse in Release mode.
#endif
		for (int y = 0; y < depth.rows; y++)
		{
			for (int x = 0; x < depth.cols; x++)
			{
				float d = float(depth.at<float>(y, x)) * 0.001f;
				depthxyz.at<Vec3f>(y, x) = Vec3f(float(x), float(y), d);
			}
		}
	}
	void Run()
	{
		depthxyz = Mat(depth.rows, depth.cols, CV_32FC3);
#ifdef _DEBUG
		//#pragma omp parallel for  // doubles performance in debug mode but is much worse in Release mode.
#endif
		for (int y = 0; y < depth.rows; y++)
		{
			for (int x = 0; x < depth.cols; x++)
			{
				float d = float(depth.at<float>(y, x)) * 0.001f;
				if (d > 0) depthxyz.at< Vec3f >(y, x) = Vec3f(float((x - ppx) / fx), float((y - ppy) / fy), d);
			}
		}
	}
};





extern "C" __declspec(dllexport)
Depth_Colorizer* Depth_Colorizer_Open()
{
	Depth_Colorizer* cPtr = new Depth_Colorizer();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer_Close(Depth_Colorizer* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Depth_Colorizer_Run(Depth_Colorizer* cPtr, int* depthPtr, int rows, int cols, float maxDepth)
{
	cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	cPtr->Run(maxDepth);
	return (int*)cPtr->output.data;
}







class SimpleProjection
{
private:
public:
	Mat depth32f, mask, viewTop, viewSide;
	SimpleProjection() {}

	void Run(float desiredMin, float desiredMax, int w, int h)
	{
		float range = float(desiredMax - desiredMin);
		float hRange = float(h);
		float wRange = float(w);
#pragma omp parallel for
		for (int y = 0; y < depth32f.rows; ++y)
		{
			for (int x = 0; x < depth32f.cols; ++x)
			{
				uchar m = mask.at<uchar>(y, x);
				if (m == 255)
				{
					float d = depth32f.at<float>(y, x);
					float dy = hRange * (d - desiredMin) / range;
					if (dy > 0 && dy < hRange) viewTop.at<uchar>(int((hRange - dy)), x) = 0;
					float dx = wRange * (d - desiredMin) / range;
					if (dx < wRange && dx > 0) viewSide.at<uchar>(y, int(dx)) = 0;
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
SimpleProjection* SimpleProjectionOpen()
{
	SimpleProjection* cPtr = new SimpleProjection();
	return cPtr;
}

extern "C" __declspec(dllexport)
void SimpleProjectionClose(SimpleProjection* cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionSide(SimpleProjection* cPtr)
{
	return (int*)cPtr->viewSide.data;
}

extern "C" __declspec(dllexport)
int* SimpleProjectionRun(SimpleProjection* cPtr, int* depthPtr, float desiredMin, float desiredMax, int rows, int cols)
{
	cPtr->depth32f = Mat(rows, cols, CV_32F, depthPtr);
	threshold(cPtr->depth32f, cPtr->mask, 0, 255, ThresholdTypes::THRESH_BINARY);
	convertScaleAbs(cPtr->mask, cPtr->mask);
	cPtr->viewTop = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->viewSide = Mat(rows, cols, CV_8U).setTo(255);
	cPtr->Run(desiredMin, desiredMax, cols, rows);
	return (int*)cPtr->viewTop.data;
}








class Project_GravityHistogram
{
private:
public:
	Mat xyz, histTop, histSide;
	Project_GravityHistogram() {}

	void Run(float maxZ, int w, int h)
	{
		float zHalf = maxZ / 2;
		float range = float(h);
		int shift = int((histTop.cols - histTop.rows) / 2); // shift to the center of the image.
		//#pragma omp parallel for  // this is faster without OpenMP!  But try it again when opportunity arrives...
		for (int y = 0; y < xyz.rows; ++y)
		{
			for (int x = 0; x < xyz.cols; ++x)
			{
				Point3f pt = xyz.at<Point3f>(y, x);
				float d = pt.z;
				if (d > 0 && d < maxZ)
				{
					float fx = pt.x;
					int x = int(range * (zHalf + fx) / maxZ + shift); // maintain a 1:1 aspect ratio
					int y = int(range - range * d / maxZ);
					if (x >= 0 && x < xyz.cols && y >= 0 && y < xyz.rows) histTop.at<float>(y, x) += 1;

					float fy = pt.y;
					if (fy > -zHalf && fy < zHalf)
					{
						int x = int(range * d / maxZ + shift);
						int y = int(range * (zHalf + fy) / maxZ); // maintain a 1:1 aspect ratio
						if (x >= 0 && x < xyz.cols && y >= 0 && y < xyz.rows) histSide.at<float>(y, x) += 1;
					}
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
Project_GravityHistogram* Project_GravityHist_Open()
{
	Project_GravityHistogram* cPtr = new Project_GravityHistogram();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Close(Project_GravityHistogram* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Side(Project_GravityHistogram* cPtr)
{
	return (int*)cPtr->histSide.data;
}

extern "C" __declspec(dllexport)
int* Project_GravityHist_Run(Project_GravityHistogram* cPtr, int* xyzPtr, float maxZ, int rows, int cols)
{
	cPtr->xyz = Mat(rows, cols, CV_32FC3, xyzPtr);
	cPtr->histTop = Mat(rows, cols, CV_32F).setTo(0);
	cPtr->histSide = Mat(rows, cols, CV_32F).setTo(0);
	cPtr->Run(maxZ, cols, rows);
	return (int*)cPtr->histTop.data;
}





// https://docs.opencv.org/3.1.0/d0/da5/tutorial_ximgproc_prediction.html
class Edge_RandomForest
{
private:
	Ptr<StructuredEdgeDetection> pDollar;
public:
	Mat dst32f, src32f, gray8u;
	Edge_RandomForest(char* modelFileName) { pDollar = createStructuredEdgeDetection(modelFileName); }

	void Run(Mat src)
	{
		src.convertTo(src32f, CV_32FC3, 1.0 / 255.0);
		pDollar->detectEdges(src32f, dst32f);
		dst32f.convertTo(gray8u, CV_8U, 255);
	}
};

extern "C" __declspec(dllexport)
Edge_RandomForest* Edge_RandomForest_Open(char* modelFileName)
{
	return new Edge_RandomForest(modelFileName);
}

extern "C" __declspec(dllexport)
int* Edge_RandomForest_Close(Edge_RandomForest* Edge_RandomForestPtr)
{
	delete Edge_RandomForestPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_RandomForest_Run(Edge_RandomForest* Edge_RandomForestPtr, int* bgrPtr, int rows, int cols)
{
	Edge_RandomForestPtr->Run(Mat(rows, cols, CV_8UC3, bgrPtr));
	return (int*)Edge_RandomForestPtr->gray8u.data;
}





// https://github.com/opencv/opencv_contrib/blob/master/modules/ximgproc/samples/dericheSample.py
class Edge_Deriche
{
private:
public:
	Mat src, dst;
	Edge_Deriche() {}
	void Run(float alpha, float omega) {
		Mat xdst, ydst;
		ximgproc::GradientDericheX(src, xdst, alpha, omega);
		ximgproc::GradientDericheY(src, ydst, alpha, omega);
		Mat dx2 = xdst.mul(xdst);
		Mat dy2 = ydst.mul(ydst);
		Mat d2 = dx2 + dy2;
		sqrt(d2, d2);
		normalize(d2, d2, 255, NormTypes::NORM_MINMAX);
		d2.convertTo(dst, CV_8UC3, 255, 0);
	}
};

extern "C" __declspec(dllexport)
Edge_Deriche* Edge_Deriche_Open()
{
	Edge_Deriche* dPtr = new Edge_Deriche();
	return dPtr;
}

extern "C" __declspec(dllexport)
int* Edge_Deriche_Close(Edge_Deriche* dPtr)
{
	delete dPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_Deriche_Run(Edge_Deriche* dPtr, int* bgrPtr, int rows, int cols, float alpha, float omega)
{
	dPtr->src = Mat(rows, cols, CV_8UC3, bgrPtr);
	dPtr->Run(alpha, omega);
	return (int*)dPtr->dst.data;
}









using namespace std;
using namespace  cv;
class Edge_ColorGap
{
private:
public:
	Mat src, dst = Mat(0, 0, CV_8U);
	Edge_ColorGap() {}
	void Run(int distance, int diff) {
		dst.setTo(0);
		int half = distance / 2, pix1, pix2;
		for (int y = 0; y < dst.rows; y++)
		{
			for (int x = distance; x < dst.cols - distance; x++)
			{
				pix1 = src.at<unsigned char>(y, x);
				pix2 = src.at<unsigned char>(y, x + distance);
				if (abs(pix1 - pix2) >= diff) dst.at<unsigned char>(y, x + half) = 255;
			}
		}
		for (int y = distance; y < dst.rows - distance; y++)
		{
			for (int x = 0; x < dst.cols; x++)
			{
				pix1 = src.at<unsigned char>(y, x);
				pix2 = src.at<unsigned char>(y + distance, x);
				if (abs(pix1 - pix2) >= diff) dst.at<unsigned char>(y + half, x) = 255;
			}
		}
	}
};

extern "C" __declspec(dllexport)
Edge_ColorGap* Edge_ColorGap_Open() {
	Edge_ColorGap* cPtr = new Edge_ColorGap();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Edge_ColorGap_Close(Edge_ColorGap* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_ColorGap_Run(Edge_ColorGap* cPtr, int* grayPtr, int rows, int cols, int distance, int diff)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, grayPtr);
	if (cPtr->dst.rows != rows) cPtr->dst = Mat(rows, cols, CV_8UC1);
	cPtr->Run(distance, diff);
	return (int*)cPtr->dst.data;
}








class Edge_DepthGap
{
private:
public:
	Mat src, dst;
	Edge_DepthGap() {}
	void RunCPP(float minDiff) {
		dst = Mat(src.rows, src.cols, CV_8UC1);
		dst.setTo(0);
		for (int y = 1; y < src.rows - 1; y++)
		{
			for (int x = 1; x < src.cols - 1; x++)
			{
				float b1 = src.at<float>(y, x - 1);
				float b2 = src.at<float>(y, x);
				if (abs(b1 - b2) >= minDiff)
				{
					Rect r = Rect(x, y - 1, 2, 3);
					dst(r).setTo(255);
				}

				b1 = src.at<float>(y - 1, x);
				if (abs(b1 - b2) >= minDiff)
				{
					Rect r = Rect(x - 1, y, 3, 2);
					dst(r).setTo(255);
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
Edge_DepthGap* Edge_DepthGap_Open() {
	Edge_DepthGap* cPtr = new Edge_DepthGap();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Edge_DepthGap_Close(Edge_DepthGap* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Edge_DepthGap_RunCPP(Edge_DepthGap* cPtr, int* dataPtr, int rows, int cols, float minDiff)
{
	cPtr->src = Mat(rows, cols, CV_32FC1, dataPtr);
	cPtr->RunCPP(minDiff);
	return (int*)cPtr->dst.data;
}





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
		for (int y = 0; y < output.rows; y += stepSize)
		{
			for (int x = 0; x < output.cols; x += stepSize)
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
EMax_Raw* EMax_Open()
{
	EMax_Raw* cPtr = new EMax_Raw();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* EMax_Close(EMax_Raw* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* EMax_Run(EMax_Raw* cPtr, int* samplePtr, int* labelsPtr, int inCount, int dimension, int rows, int cols, int clusters,
	int stepSize, int covarianceMatrixType)
{
	cPtr->covarianceMatrixType = covarianceMatrixType;
	cPtr->stepSize = stepSize;
	cPtr->clusters = clusters;
	cPtr->labels = Mat(inCount, 1, CV_32S, labelsPtr);
	cPtr->samples = Mat(inCount, dimension, CV_32FC1, samplePtr);
	cPtr->output = Mat(rows, cols, CV_32S);
	cPtr->Run();
	return (int*)cPtr->output.data;
}





float outputTriangleAMS[5];
extern "C" __declspec(dllexport)
int* FitEllipse_AMS(float* inputPoints, int count)
{
	Mat input(count, 1, CV_32FC2, inputPoints);
	RotatedRect box = fitEllipseAMS(input);
	outputTriangleAMS[0] = box.angle;
	outputTriangleAMS[1] = box.center.x;
	outputTriangleAMS[2] = box.center.y;
	outputTriangleAMS[3] = box.size.width;
	outputTriangleAMS[4] = box.size.height;
	return (int*)&outputTriangleAMS;
}



float outputTriangle[5];
extern "C" __declspec(dllexport)
int* FitEllipse_Direct(float* inputPoints, int count)
{
	Mat input(count, 1, CV_32FC2, inputPoints);
	RotatedRect box = fitEllipseDirect(input);
	outputTriangle[0] = box.angle;
	outputTriangle[1] = box.center.x;
	outputTriangle[2] = box.center.y;
	outputTriangle[3] = box.size.width;
	outputTriangle[4] = box.size.height;
	return (int*)&outputTriangle;
}








class Fuzzy
{
private:
public:
	Mat src, dst;
	Fuzzy() {}
	void Run() {
		dst = Mat(src.rows, src.cols, CV_8U);
		dst.setTo(0);
		for (int y = 1; y < src.rows - 3; ++y)
		{
			//#pragma omp parallel for 
			for (int x = 1; x < src.cols - 3; ++x)
			{
				int pixel = src.at<uchar>(y, x);
				Rect r = Rect(x, y, 3, 3);
				Scalar sum = cv::sum(src(r));
				if (sum.val[0] == double(pixel * 9)) dst.at<uchar>(y + 1, x + 1) = pixel;
			}
		}
	}
};

extern "C" __declspec(dllexport)
Fuzzy* Fuzzy_Open() {
	Fuzzy* cPtr = new Fuzzy();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Fuzzy_Close(Fuzzy* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Fuzzy_Run(Fuzzy* cPtr, int* grayPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, grayPtr);
	cPtr->Run();
	return (int*)cPtr->dst.data;
}






class Guess_Depth
{
private:
public:
	Mat src;
	Guess_Depth() {}
	void RunCPP() {
		for (int y = 1; y < src.rows - 1; y++)
		{
			for (int x = 1; x < src.cols - 1; x++)
			{
				Point3f s1 = src.at<Point3f>(y, x - 1);
				Point3f s2 = src.at<Point3f>(y, x);
				if (s1.z > 0 && s2.z == 0)
				{
					Point3f s3 = src.at<Point3f>(y - 1, x);
					Point3f s4 = src.at<Point3f>(y + 1, x);
					Point3f s5 = src.at<Point3f>(y, x + 1);
					if ((s3.z > 0 && s4.z > 0) || s5.z > 0) src.at<Point3f>(y, x) = s1; // duplicate the neighbor next to this pixel missing any depth.
				}
			}
		}
	}
};


extern "C" __declspec(dllexport)
Guess_Depth* Guess_Depth_Open() {
	Guess_Depth* cPtr = new Guess_Depth();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Guess_Depth_Close(Guess_Depth* cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport)
int* Guess_Depth_RunCPP(Guess_Depth* cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_32FC3, dataPtr);
	cPtr->RunCPP();
	return (int*)cPtr->src.data;
}






class Guess_ImageEdges
{
private:
public:
	Mat pc;
	Guess_ImageEdges() {}
	void RunCPP(int maxDistanceToEdge) {
		int y = 0;
		int maxGap = maxDistanceToEdge;
		for (int x = 0; x < pc.cols; x++)
		{
			for (int y = 0; y < maxGap; y++)
			{
				Point3f s1 = pc.at<Point3f>(y, x);
				if (s1.z > 0)
				{
					for (int i = 0; i < y; i++)
					{
						pc.at<Point3f>(i, x) = s1;
					}
					break;
				}
			}
			for (int y = 1; y < maxGap; y++)
			{
				Point3f s1 = pc.at<Point3f>(pc.rows - y, x);
				if (s1.z > 0)
				{
					for (int i = 1; i < y; i++)
					{
						pc.at<Point3f>(pc.rows - i, x) = s1;
					}
					break;
				}
			}
		}
		for (int y = 0; y < pc.rows; y++)
		{
			for (int x = 0; x < maxGap; x++)
			{
				Point3f s1 = pc.at<Point3f>(y, x);
				if (s1.z > 0)
				{
					for (int i = 0; i < x; i++)
					{
						pc.at<Point3f>(y, i) = s1;
					}
					break;
				}
			}
			for (int x = 1; x < maxGap; x++)
			{
				Point3f s1 = pc.at<Point3f>(y, pc.cols - x);
				if (s1.z > 0)
				{
					for (int i = 1; i < x; i++)
					{
						pc.at<Point3f>(y, pc.cols - i) = s1;
					}
					break;
				}
			}
		}
	}
};






extern "C" __declspec(dllexport)
Guess_ImageEdges* Guess_ImageEdges_Open() {
	Guess_ImageEdges* cPtr = new Guess_ImageEdges();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Guess_ImageEdges_Close(Guess_ImageEdges* cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport)
int* Guess_ImageEdges_RunCPP(Guess_ImageEdges* cPtr, int* dataPtr, int rows, int cols, int maxDistanceToEdge)
{
	cPtr->pc = Mat(rows, cols, CV_32FC3, dataPtr);
	cPtr->RunCPP(maxDistanceToEdge);
	return (int*)cPtr->pc.data;
}






class Harris_Features
{
private:
public:
	Mat src, dst;
	float threshold = 0.0001f;
	int neighborhood = 3;
	int aperture = 3;
	float HarrisParm = 0.01f;
	Harris_Features() {}
	void Run()
	{
		Mat cornerStrength;
		cornerHarris(src, cornerStrength, neighborhood, aperture, HarrisParm);
		cv::threshold(cornerStrength, dst, threshold, 255, THRESH_BINARY_INV);
	}
};

extern "C" __declspec(dllexport)
Harris_Features* Harris_Features_Open()
{
	return new Harris_Features();
}

extern "C" __declspec(dllexport)
int* Harris_Features_Close(Harris_Features* Harris_FeaturesPtr)
{
	delete Harris_FeaturesPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Harris_Features_Run(Harris_Features* Harris_FeaturesPtr, int* bgrPtr, int rows, int cols, float threshold, int neighborhood, int aperture, float HarrisParm)
{
	Harris_FeaturesPtr->threshold = threshold;
	Harris_FeaturesPtr->neighborhood = neighborhood;
	Harris_FeaturesPtr->aperture = aperture;
	Harris_FeaturesPtr->HarrisParm = HarrisParm;
	Harris_FeaturesPtr->src = Mat(rows, cols, CV_8U, bgrPtr);
	Harris_FeaturesPtr->Run();
	return (int*)Harris_FeaturesPtr->dst.data;
}



class Harris_Detector
{
private:
public:
	std::vector<Point> points;
	Mat src;
	HarrisDetector harris;
	Harris_Detector() {}
	void Run(double qualityLevel)
	{
		points.clear();
		harris.detect(src);
		harris.getCorners(points, qualityLevel);
		//harris.drawOnImage(src, points);
	}
};

extern "C" __declspec(dllexport)
Harris_Detector* Harris_Detector_Open()
{
	return new Harris_Detector();
}

extern "C" __declspec(dllexport) int Harris_Detector_Count(Harris_Detector* cPtr)
{
	return (int)cPtr->points.size();
}

extern "C" __declspec(dllexport)
int* Harris_Detector_Close(Harris_Detector* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Harris_Detector_Run(Harris_Detector* cPtr, int* bgrPtr, int rows, int cols, double qualityLevel, int* count)
{
	cPtr->src = Mat(rows, cols, CV_8U, bgrPtr);
	cPtr->Run(qualityLevel);
	return (int*)&cPtr->points[0];
}







// https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
// https://docs.opencv.org/trunk/d1/d1d/tutorial_histo3D.html
// When run in VB.Net the histogram output has rows = -1 and cols = -1
// The rows and cols are both -1 so I had assumed there was a bug but the data is accessible with the at method.
// If you attempt the same access to the data in managed code, it does not work (AFAIK).
extern "C" __declspec(dllexport)
float* Hist3Dcolor_Run(int* bgrPtr, int rows, int cols, int bins)
{
	float hRange[] = { 0, 256 }; // ranges are exclusive in the top of the range, hence 256
	const float* range[] = { hRange, hRange, hRange };
	int hbins[] = { bins, bins, bins };
	int channels[] = { 0, 1, 2 };
	Mat src = Mat(rows, cols, CV_8UC3, bgrPtr);

	static Mat histogram;
	calcHist(&src, 1, channels, Mat(), histogram, 3, hbins, range);

	return (float*)histogram.data;
}




static Mat newImage;

// https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
// https://docs.opencv.org/trunk/d1/d1d/tutorial_histo3D.html
// When run in VB.Net the histogram output has rows = -1 and cols = -1
// The rows and cols are both -1 so I had assumed there was a bug but the data is accessible with the at method.
// If you attempt the same access to the data in managed code, it does not work (AFAIK).
extern "C" __declspec(dllexport)
uchar* BackProjectBGR_Run(int* bgrPtr, int rows, int cols, int bins, float threshold)
{
	Mat input = Mat(rows, cols, CV_8UC3, bgrPtr);
	float hRange0[] = { 0, 256 };
	float hRange1[] = { 0, 256 };
	float hRange2[] = { 0, 256 };
	const float* range[] = { hRange0, hRange1, hRange2 };
	int hbins[] = { bins, bins, bins };
	int channels[] = { 0, 1, 2 };

	static Mat histogram;
	calcHist(&input, 1, channels, Mat(), histogram, 3, hbins, range); // for 3D histograms, all 3 bins must be equal.

	float* hist = (float*)histogram.data;
	for (int i = 0; i < bins * bins * bins; i++)
	{
		if (hist[i] < threshold) hist[i] = 0;
	}

	calcBackProject(&input, 1, channels, histogram, newImage, range);

	return (uchar*)newImage.data;
}





extern "C" __declspec(dllexport)
uchar* Hist3Dcloud_Run(int* inputPtr, int rows, int cols, int bins,
	float minX, float minY, float minZ,
	float maxX, float maxY, float maxZ)
{
	Mat input = Mat(rows, cols, CV_32FC3, inputPtr);
	float hRange0[] = { float(minX), float(maxX) };
	float hRange1[] = { float(minY), float(maxY) };
	float hRange2[] = { float(minZ), float(maxZ) };
	const float* range[] = { hRange0, hRange1, hRange2 };
	int hbins[] = { bins, bins, bins };
	int channel[] = { 0, 1, 2 };

	static Mat histogram;
	calcHist(&input, 1, channel, Mat(), histogram, 3, hbins, range, true, false); // for 3D histograms, all 3 bins must be equal.

	return (uchar*)histogram.data;
}








static Mat mask;

extern "C" __declspec(dllexport)
uchar* BackProjectCloud_Run(int* inputPtr, int rows, int cols, int bins, float threshold,
	float minX, float minY, float minZ,
	float maxX, float maxY, float maxZ)
{
	Mat input = Mat(rows, cols, CV_32FC3, inputPtr);
	float hRange0[] = { float(minX), float(maxX) };
	float hRange1[] = { float(minY), float(maxY) };
	float hRange2[] = { float(minZ), float(maxZ) };
	const float* range[] = { hRange0, hRange1, hRange2 };
	int hbins[] = { bins, bins, bins };
	int channels[] = { 0, 1, 2 };

	static Mat histogram;
	calcHist(&input, 1, channels, Mat(), histogram, 3, hbins, range); // for 3D histograms, all 3 bins must be equal.

	float* hist = (float*)histogram.data;
	for (int i = 0; i < bins * bins * bins; i++)
	{
		if (hist[i] < threshold) hist[i] = 0; else hist[i] = 255; // building a mask
	}

	Mat mask32f;
	calcBackProject(&input, 1, channels, histogram, mask32f, range);
	mask32f.convertTo(mask, CV_8U);

	return (uchar*)mask.data;
}






class Hist_1D
{
private:
public:
	Mat src;
	Mat histogram;
	Hist_1D() {}
	void RunCPP(int bins) {
		float hRange[] = { 0, 256 };
		int hbins[] = { bins };
		const float* range[] = { hRange };
		calcHist(&src, 1, { 0 }, Mat(), histogram, 1, hbins, range, true, false);
	}
};
extern "C" __declspec(dllexport)
Hist_1D* Hist_1D_Open() {
	Hist_1D* cPtr = new Hist_1D();
	return cPtr;
}
extern "C" __declspec(dllexport)
float Hist_1D_Sum(Hist_1D* cPtr) {
	Scalar count = sum(cPtr->histogram);
	return count[0];
}
extern "C" __declspec(dllexport)
void Hist_1D_Close(Hist_1D* cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport)
int* Hist_1D_RunCPP(Hist_1D* cPtr, int* dataPtr, int rows, int cols, int bins)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
	cPtr->RunCPP(bins);
	return (int*)cPtr->histogram.data;
}






// http://man.hubwiz.com/docset/OpenCV.docset/Contents/Resources/Documents/d9/dde/samples_2cpp_2kmeans_8cpp-example.html

Scalar colorTab[] =
{
	Scalar(0, 0, 255),
	Scalar(0,255,0),
	Scalar(255,100,100),
	Scalar(255,0,255),
	Scalar(0,255,255)
};

class KMeans_MultiGaussian
{
private:
	RNG rng;
public:
	Mat dst;
	KMeans_MultiGaussian() { rng = rng(12345); }
	const int MAX_CLUSTERS = 5;

	void RunCPP() {
		int k, clusterCount = rng.uniform(2, MAX_CLUSTERS + 1);
		int i, sampleCount = rng.uniform(1, 1001);
		Mat points(sampleCount, 1, CV_32FC2), labels;

		clusterCount = MIN(clusterCount, sampleCount);
		std::vector<Point2f> centers;

		/* generate random sample from multigaussian distribution */
		for (k = 0; k < clusterCount; k++)
		{
			Point center;
			center.x = rng.uniform(0, dst.cols);
			center.y = rng.uniform(0, dst.rows);
			Mat pointChunk = points.rowRange(k * sampleCount / clusterCount,
				k == clusterCount - 1 ? sampleCount :
				(k + 1) * sampleCount / clusterCount);
			rng.fill(pointChunk, RNG::NORMAL, Scalar(center.x, center.y), Scalar(dst.cols * 0.05, dst.rows * 0.05));
		}

		randShuffle(points, 1, &rng);

		double compactness = kmeans(points, clusterCount, labels,
			TermCriteria(TermCriteria::EPS + TermCriteria::COUNT, 10, 1.0),
			3, KMEANS_PP_CENTERS, centers);

		for (i = 0; i < sampleCount; i++)
		{
			int clusterIdx = labels.at<int>(i);
			Point ipt = points.at<Point2f>(i);
			circle(dst, ipt, 2, colorTab[clusterIdx], FILLED, LINE_AA);
		}
		for (i = 0; i < int(centers.size()); ++i)
		{
			Point2f c = centers[i];
			circle(dst, c, 40, colorTab[i], 1, LINE_AA);
		}
	}
};

extern "C" __declspec(dllexport)
KMeans_MultiGaussian* KMeans_MultiGaussian_Open() {
	KMeans_MultiGaussian* cPtr = new KMeans_MultiGaussian();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* KMeans_MultiGaussian_Close(KMeans_MultiGaussian* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* KMeans_MultiGaussian_RunCPP(KMeans_MultiGaussian* cPtr, int rows, int cols)
{
	cPtr->dst = Mat(rows, cols, CV_8UC3);
	cPtr->dst.setTo(0);
	cPtr->RunCPP();
	return (int*)cPtr->dst.data;
}







class Kmeans_Simple
{
private:
public:
	Mat src, dst;
	Kmeans_Simple() {}
	void RunCPP(float minVal, float maxVal) {
		dst.setTo(0);
		Vec3b yellow(0, 255, 255);
		Vec3b blue(255, 0, 0);
		for (int y = 0; y < dst.rows; y++)
		{
			for (int x = 0; x < dst.cols; x++)
			{
				float b = src.at<float>(y, x);
				if (b != 0)
				{
					if ((maxVal - b) < (b - minVal)) dst.at<Vec3b>(y, x) = blue; else dst.at<Vec3b>(y, x) = yellow;
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
Kmeans_Simple* Kmeans_Simple_Open() {
	Kmeans_Simple* cPtr = new Kmeans_Simple();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Kmeans_Simple_Close(Kmeans_Simple* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Kmeans_Simple_RunCPP(Kmeans_Simple* cPtr, int* dataPtr, int rows, int cols, float minVal, float maxVal)
{
	cPtr->src = Mat(rows, cols, CV_32F, dataPtr);
	cPtr->dst = Mat(rows, cols, CV_8UC3);
	cPtr->RunCPP(minVal, maxVal);
	return (int*)cPtr->dst.data;
}





extern "C" __declspec(dllexport)
void MinTriangle_Run(float* inputPoints, int count, float* outputTriangle)
{
	Mat input(count, 1, CV_32FC2, inputPoints);
	vector<Point2f> triangle;
	minEnclosingTriangle(input, triangle);
	for (int i = 0; i < 3; ++i)
	{
		outputTriangle[i * 2 + 0] = triangle.at(i).x;
		outputTriangle[i * 2 + 1] = triangle.at(i).y;
	}
}





class Sort_MLPrepTest
{
private:
public:
	Mat src, dst;
	Sort_MLPrepTest() {}
	void Run() {
		dst = Mat(src.rows, src.cols, CV_32FC2);
		for (int y = 0; y < src.rows; y++)
		{
			for (int x = 0; x < src.cols; x++)
			{
				int gray = src.at<unsigned char>(y, x);
				dst.at<Point2f>(y, x) = Point2f(float(gray), float(y));
			}
		}
	}
};

extern "C" __declspec(dllexport) Sort_MLPrepTest* Sort_MLPrepTest_Open() { Sort_MLPrepTest* cPtr = new Sort_MLPrepTest(); return cPtr; }
extern "C" __declspec(dllexport) int* Sort_MLPrepTest_Close(Sort_MLPrepTest* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int* Sort_MLPrepTest_Run(Sort_MLPrepTest* cPtr, int* grayPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8U, grayPtr);
	cPtr->Run();
	return (int*)cPtr->dst.data;
}






class ML_RemoveDups
{
private:
public:
	Mat src, dst;
	ML_RemoveDups() {}
	int index = 0;
	void Run() {
		index = 0;
		int lastVal = -1;
		if (src.type() == CV_32S)
		{
			dst = Mat(int(src.total()), 1, CV_32S);
			dst.setTo(0);
			for (int y = 0; y < src.rows; y++)
			{
				for (int x = 0; x < src.cols; x++)
				{
					int val = src.at<int>(y, x);
					if (val != lastVal)
					{
						dst.at<int>(index, 0) = val;
						lastVal = val;
						index++;
					}
				}
			}
		}
		else
		{
			dst = Mat(int(src.total()), 1, CV_8U);
			dst.setTo(0);
			for (int y = 0; y < src.rows; y++)
			{
				for (int x = 0; x < src.cols; x++)
				{
					int val = src.at<unsigned char>(y, x);
					if (val != lastVal)
					{
						dst.at<unsigned char>(index, 0) = val;
						lastVal = val;
						index++;
					}
				}
			}
		}
	}
};

extern "C" __declspec(dllexport) ML_RemoveDups* ML_RemoveDups_Open() { ML_RemoveDups* cPtr = new ML_RemoveDups(); return cPtr; }
extern "C" __declspec(dllexport) int ML_RemoveDups_GetCount(ML_RemoveDups* cPtr) { return cPtr->index - 1; }
extern "C" __declspec(dllexport) int* ML_RemoveDups_Close(ML_RemoveDups* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int* ML_RemoveDups_Run(ML_RemoveDups* cPtr, int* dataPtr, int rows, int cols, int type)
{
	cPtr->src = Mat(rows, cols, type, dataPtr);
	cPtr->Run();
	return (int*)cPtr->dst.data;
}





class MSER_Interface
{
private:
public:
	Mat src, dst;
	Ptr<MSER> mser;
	vector<Rect> containers;
	vector<Point> floodPoints;
	vector<int> maskCounts;
	vector<vector<Point>> regions;
	vector<Rect> boxes;
	MSER_Interface(int delta, int minArea, int maxArea, float maxVariation, float minDiversity, int maxEvolution, float areaThreshold,
		float minMargin, int edgeBlurSize, int pass2Setting)
	{
		mser = mser->create(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin, edgeBlurSize);
		mser->setPass2Only(pass2Setting);
	}
	void RunCPP() {
		mser->detectRegions(src, regions, boxes);

		multimap<int, int, greater<int>> sizeSorted;
		for (auto i = 0; i < regions.size(); i++)
		{
			sizeSorted.insert(make_pair(regions[i].size(), i));
		}

		int index = 1;
		maskCounts.clear();
		containers.clear();
		floodPoints.clear();
		dst.setTo(255);
		for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
		{
			Rect box = boxes[it->second];
			//Point center = Point(box.x + box.width / 2, box.y + box.height / 2);
			//int val = dst.at<uchar>(center.y, center.x);
			//if (val == 255)
			//{
			floodPoints.push_back(regions[it->second][0]);
			maskCounts.push_back((int)regions[it->second].size());
			for (Point pt : regions[it->second])
			{
				dst.at<uchar>(pt.y, pt.x) = index;
			}
			index++;
			containers.push_back(box);
			//}
		}
	}
};
extern "C" __declspec(dllexport)
MSER_Interface* MSER_Open(int delta, int minArea, int maxArea, float maxVariation, float minDiversity, int maxEvolution, float areaThreshold,
	float minMargin, int edgeBlurSize, int pass2Setting)
{
	MSER_Interface* cPtr = new MSER_Interface(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin,
		edgeBlurSize, pass2Setting);
	return cPtr;
}
extern "C" __declspec(dllexport)
void MSER_Close(MSER_Interface* cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport)
int* MSER_Rects(MSER_Interface* cPtr)
{
	return (int*)&cPtr->containers[0];
}
extern "C" __declspec(dllexport)
int* MSER_FloodPoints(MSER_Interface* cPtr)
{
	return (int*)&cPtr->floodPoints[0];
}
extern "C" __declspec(dllexport)
int* MSER_MaskCounts(MSER_Interface* cPtr)
{
	return (int*)&cPtr->maskCounts[0];
}
extern "C" __declspec(dllexport)
int MSER_Count(MSER_Interface* cPtr)
{
	return (int)cPtr->containers.size();
}
extern "C" __declspec(dllexport)
int* MSER_RunCPP(MSER_Interface* cPtr, int* dataPtr, int rows, int cols, int channels)
{
	cPtr->src = Mat(rows, cols, (channels == 3) ? CV_8UC3 : CV_8UC1, dataPtr);
	cPtr->dst = Mat(rows, cols, CV_8UC1);
	cPtr->RunCPP();
	return (int*)cPtr->dst.data;
}






class PCA_Prep
{
private:
public:
	vector<Point3f>pcaList;
	Mat src;
	PCA_Prep() {}
	void Run() {
		pcaList.clear();
		for (int y = 0; y < src.rows; y++)
		{
			for (int x = 0; x < src.cols; x++)
			{
				auto vec = src.at<Point3f>(y, x);
				if (vec.z > 0) pcaList.push_back(vec);
			}
		}
	}
};

extern "C" __declspec(dllexport) PCA_Prep* PCA_Prep_Open() { PCA_Prep* cPtr = new PCA_Prep(); return cPtr; }
extern "C" __declspec(dllexport) int* PCA_Prep_Close(PCA_Prep* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int PCA_Prep_GetCount(PCA_Prep* cPtr) { return int(cPtr->pcaList.size()); }
extern "C" __declspec(dllexport) int* PCA_Prep_Run(PCA_Prep* cPtr, int* pointCloudData, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_32FC3, pointCloudData);
	cPtr->Run();
	return (int*)cPtr->pcaList.data();
}







class PlotOpenCV
{
private:
public:
	Mat src, srcX, srcY, dst;
	vector<Point>floodPoints;

	PlotOpenCV() {}
	void RunCPP() {
		Mat result;
		Ptr<plot::Plot2d> plot = plot::Plot2d::create(srcX, srcY);
		plot->setInvertOrientation(true);
		plot->setShowText(false);
		plot->setShowGrid(false);
		plot->setPlotBackgroundColor(Scalar(255, 200, 200));
		plot->setPlotLineColor(Scalar(255, 0, 0));
		plot->setPlotLineWidth(2);
		plot->render(result);
		resize(result, dst, dst.size());
	}
};

extern "C" __declspec(dllexport) PlotOpenCV* PlotOpenCV_Open() { PlotOpenCV* cPtr = new PlotOpenCV(); return cPtr; }
extern "C" __declspec(dllexport) void PlotOpenCV_Close(PlotOpenCV* cPtr) { delete cPtr; }

// https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
extern "C" __declspec(dllexport)
int* PlotOpenCV_Run(PlotOpenCV* cPtr, double* inX, double* inY, int len, int rows, int cols)
{
	cPtr->dst.setTo(0);
	cPtr->srcX = Mat(1, len, CV_64F, inX);
	cPtr->srcY = Mat(1, len, CV_64F, inY);
	cPtr->dst = Mat(rows, cols, CV_8UC3);
	cPtr->RunCPP();
	return (int*)cPtr->dst.data;
}






class Random_PatternGenerator
{
private:
public:
	Mat pattern;
	Random_PatternGenerator() {}
	void Run() {}
};

extern "C" __declspec(dllexport)
Random_PatternGenerator* Random_PatternGenerator_Open() {
	Random_PatternGenerator* Random_PatternGeneratorPtr = new Random_PatternGenerator();
	return Random_PatternGeneratorPtr;
}

extern "C" __declspec(dllexport)
int* Random_PatternGenerator_Close(Random_PatternGenerator* rPtr)
{
	delete rPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Random_PatternGenerator_Run(Random_PatternGenerator* rPtr, int rows, int cols)
{
	randpattern::RandomPatternGenerator generator(cols, rows);
	generator.generatePattern();
	rPtr->pattern = generator.getPattern();
	return (int*)rPtr->pattern.data;
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
Random_DiscreteDistribution* Random_DiscreteDistribution_Open() {
	Random_DiscreteDistribution* cPtr = new Random_DiscreteDistribution();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Random_DiscreteDistribution_Close(Random_DiscreteDistribution* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Random_DiscreteDistribution_Run(Random_DiscreteDistribution* cPtr, int rows, int cols)
{
	cPtr->discrete = Mat(rows, cols, CV_32F, 0);
	return (int*)cPtr->discrete.data;
}







class RedCloud_FindCells
{
private:
public:
	vector <int> cellList;
	RedCloud_FindCells() {}
	void RunCPP(Mat src)
	{
		cellList.clear();
		for (int y = 0; y < src.rows; y++)
		{
			for (int x = 0; x < src.cols; x++)
			{
				auto val = src.at<unsigned char>(y, x);
				if (count(cellList.begin(), cellList.end(), val) == 0)
					cellList.push_back(val);
			}
		}
	}
};
extern "C" __declspec(dllexport)
RedCloud_FindCells* RedCloud_FindCells_Open() {
	RedCloud_FindCells* cPtr = new RedCloud_FindCells();
	return cPtr;
}
extern "C" __declspec(dllexport)
void RedCloud_FindCells_Close(RedCloud_FindCells* cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport) int RedCloud_FindCells_TotalCount(RedCloud_FindCells* cPtr) { return int(cPtr->cellList.size()); }
extern "C" __declspec(dllexport)
int* RedCloud_FindCells_RunCPP(RedCloud_FindCells* cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->RunCPP(Mat(rows, cols, CV_8UC1, dataPtr));
	if (cPtr->cellList.size() == 0) return 0;
	return (int*)&cPtr->cellList[0];
}






class Pixels_Vector
{
private:
public:
	Mat src, dst;
	Pixels_Vector() {}
	vector <Vec3b> pixelList;
	void RunCPP()
	{
		pixelList.clear();
		for (int y = 0; y < src.rows; y++)
		{
			for (int x = 0; x < src.cols; x++)
			{
				auto val = src.at<Vec3b>(y, x);
				if (count(pixelList.begin(), pixelList.end(), val) == 0)
					pixelList.push_back(val);
			}
		}
	}
};
extern "C" __declspec(dllexport)
Pixels_Vector* Pixels_Vector_Open() {
	Pixels_Vector* cPtr = new Pixels_Vector();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Pixels_Vector_Close(Pixels_Vector* cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport) int* Pixels_Vector_Pixels(Pixels_Vector* cPtr)
{
	return (int*)&cPtr->pixelList[0];
}
extern "C" __declspec(dllexport)
int Pixels_Vector_RunCPP(Pixels_Vector* cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
	cPtr->RunCPP();
	return (int)cPtr->pixelList.size();
}





class Stabilizer_Basics_CPP
{
private:
public:
	VideoStab stab;
	Mat rgb;
	Mat smoothedFrame;
	Stabilizer_Basics_CPP()
	{
		smoothedFrame = Mat(2, 3, CV_64F);
	}
	void Run()
	{
		smoothedFrame = stab.stabilize(rgb);
	}
};

extern "C" __declspec(dllexport)
Stabilizer_Basics_CPP* Stabilizer_Basics_Open()
{
	Stabilizer_Basics_CPP* cPtr = new Stabilizer_Basics_CPP();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Stabilizer_Basics_Close(Stabilizer_Basics_CPP* cPtr)
{
	delete cPtr;
	return (int*)0;
}

// https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
extern "C" __declspec(dllexport)
int* Stabilizer_Basics_Run(Stabilizer_Basics_CPP* cPtr, int* bgrPtr, int rows, int cols)
{
	cPtr->rgb = Mat(rows, cols, CV_8UC3, bgrPtr);
	cvtColor(cPtr->rgb, cPtr->stab.gray, COLOR_BGR2GRAY);
	if (cPtr->stab.lastFrame.rows > 0) cPtr->Run(); // skips the first pass while the frames get loaded.
	cPtr->stab.gray.copyTo(cPtr->stab.lastFrame);
	return (int*)cPtr->stab.smoothedFrame.data;
}






class SuperPixels
{
private:
public:
	Mat src, labels, dst;
	Ptr<ximgproc::SuperpixelSEEDS> seeds;
	int width, height, num_superpixels = 400, num_levels = 4, prior = 2;
	SuperPixels() {}
	void Run()
	{
		Mat hsv;
		//cvtColor(src, hsv, ColorConversionCodes::COLOR_BGR2HSV);
		seeds->iterate(src);
		seeds->getLabelContourMask(dst, false);
		seeds->getLabels(labels);
	}
};

extern "C" __declspec(dllexport)
SuperPixels* SuperPixel_Open(int _width, int _height, int _num_superpixels, int _num_levels, int _prior)
{
	SuperPixels* spPtr = new SuperPixels();
	spPtr->width = _width;
	spPtr->height = _height;
	spPtr->num_superpixels = _num_superpixels;
	spPtr->num_levels = _num_levels;
	spPtr->prior = _prior;
	spPtr->seeds = ximgproc::createSuperpixelSEEDS(_width, _height, 3, _num_superpixels, _num_levels, _prior);
	spPtr->labels = Mat(spPtr->height, spPtr->width, CV_32S);
	return spPtr;
}

extern "C" __declspec(dllexport)
int* SuperPixel_Close(SuperPixels* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* SuperPixel_GetLabels(SuperPixels* cPtr)
{
	return (int*)cPtr->labels.data;
}

extern "C" __declspec(dllexport)
int* SuperPixel_Run(SuperPixels* cPtr, int* srcPtr)
{
	cPtr->src = Mat(cPtr->height, cPtr->width, CV_8UC3, srcPtr);
	cPtr->Run();
	return (int*)cPtr->dst.data;
}






// Parameters for Kalman Filter
#define Q1 0.004
#define R1 0.5

VideoStab::VideoStab()
{
	smoothedMat.create(2, 3, CV_64F);
	k = 1;
	errScale = Mat(5, 1, CV_64F).setTo(1);
	qScale = Mat(5, 1, CV_64F).setTo(Q1);
	rScale = Mat(5, 1, CV_64F).setTo(R1);
	sumScale = Mat(5, 1, CV_64F).setTo(0);
	sScale = Mat(5, 1, CV_64F).setTo(0);
}

// https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
//The main stabilization function
Mat VideoStab::stabilize(Mat rgb)
{
	int vert_border = HORIZONTAL_BORDER_CROP * gray.rows / gray.cols;

	vector <Point2f> features1, features2;
	vector <Point2f> goodFeatures1, goodFeatures2;
	vector <uchar> status;
	vector <float> err;

	//Estimating the features in gray1 and gray2
	goodFeaturesToTrack(gray, features1, 200, 0.01, 30);
	calcOpticalFlowPyrLK(gray, lastFrame, features1, features2, status, err);

	for (size_t i = 0; i < status.size(); i++)
	{
		if (status[i] && err[i] < 2)
		{
			goodFeatures1.push_back(features1[i]);
			goodFeatures2.push_back(features2[i]);
		}
	}

	if (goodFeatures1.size() > 0 && goodFeatures2.size() > 0)
	{
		//All the parameters scale, angle, and translation are stored in affine
		affine = getAffineTransform(goodFeatures1.data(), goodFeatures2.data());

		double dx = affine.at<double>(0, 2);
		double dy = affine.at<double>(1, 2);
		double da = atan2(affine.at<double>(1, 0), affine.at<double>(0, 0));
		double ds_x = affine.at<double>(0, 0) / cos(da);
		double ds_y = affine.at<double>(1, 1) / cos(da);
		double saveDX = dx, saveDY = dy, saveDA = da;

		char original[1000];
		sprintf_s(original, "da = %f, dx = %f, dy = %f", da, dx, dy);

		double sx = ds_x;
		double sy = ds_y;

		double deltaArray[5] = { ds_x, ds_y, da, dx, dy };
		Mat delta(5, 1, CV_64F, deltaArray);
		add(sumScale, delta, sumScale);

		//Don't calculate the predicted state of Kalman Filter on 1st iteration
		if (k == 1) k++; else Kalman_Filter();

		Mat diff(5, 1, CV_64F);
		subtract(sScale, sumScale, diff);

		if (diff.at<double>(2, 0) < 1000 && diff.at<double>(3, 0) < 1000 && diff.at<double>(4, 0) < 1000)
		{
			da += diff.at<double>(2, 0);
			dx += diff.at<double>(3, 0);
			dx += diff.at<double>(4, 0);
		}
		if (fabs(dx) > 50)  dx = saveDX;
		if (fabs(dy) > 50)  dy = saveDY;
		if (fabs(da) > 50)  da = saveDA;

		//Creating the smoothed parameters matrix
		smoothedMat.at<double>(0, 0) = sx * cos(da);
		smoothedMat.at<double>(0, 1) = sx * -sin(da);
		smoothedMat.at<double>(1, 0) = sy * sin(da);
		smoothedMat.at<double>(1, 1) = sy * cos(da);

		smoothedMat.at<double>(0, 2) = dx;
		smoothedMat.at<double>(1, 2) = dy;

		//Warp the new frame using the smoothed parameters
		warpAffine(rgb, smoothedFrame, smoothedMat, rgb.size());

		//Crop the smoothed frame a little to eliminate black region due to Kalman Filter
		smoothedFrame = smoothedFrame(Range(vert_border, smoothedFrame.rows - vert_border), Range(HORIZONTAL_BORDER_CROP, smoothedFrame.cols - HORIZONTAL_BORDER_CROP));

		resize(smoothedFrame, smoothedFrame, rgb.size());
		for (int i = 0; i < features1.size(); ++i)
		{
			circle(smoothedFrame, features1[i], 5, Scalar::all(255), -1, LineTypes::LINE_AA);
		}
		putText(smoothedFrame, original, Point(10, 50), HersheyFonts::FONT_HERSHEY_COMPLEX, 0.4, Scalar::all(255), 1);

		char buffer[1000];
		sprintf_s(buffer, "da = %f, dx = %f, dy = %f", da, dx, dy);
		putText(smoothedFrame, buffer, Point(10, 100), HersheyFonts::FONT_HERSHEY_COMPLEX, 0.4, Scalar::all(255), 1);
	}
	return smoothedFrame;
}

void VideoStab::Kalman_Filter()
{
	Mat f1err = Mat(5, 1, CV_64F);
	add(errScale, qScale, f1err);
	for (int i = 0; i < f1err.rows; ++i)
	{
		double gainScale = f1err.at<double>(i, 0) / (f1err.at<double>(i, 0) + rScale.at<double>(i, 0));
		sScale.at<double>(i, 0) = sScale.at<double>(i, 0) + gainScale * (sumScale.at<double>(i, 0) - sScale.at<double>(i, 0));
		errScale.at<double>(i, 0) = (1.0 - gainScale) * f1err.at<double>(i, 0);
	}
}








// https://stackoverflow.com/questions/22654770/creating-vignette-filter-in-opencv
class Vignetting_CPP
{
private:
public:
	Mat src, dst;
	Vignetting_CPP() {}
	double fastCos(double x) {
		x += 1.57079632;
		if (x > 3.14159265) x -= 6.28318531;
		if (x < 0) return 1.27323954 * x + 0.405284735 * x * x;
		return 1.27323954 * x - 0.405284735 * x * x;
	}
	double dist(double ax, double ay, double bx, double by) {
		return sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
	}
	void RunCPP(double radius, double centerX, double centerY, bool removal) {
		dst = src.clone();
		double maxDis = radius * dist(0, 0, centerX, centerY);
		double temp;

		for (int y = 0; y < src.rows; y++) {
			for (int x = 0; x < src.cols; x++) {
				temp = fastCos(dist(centerX, centerY, x, y) / maxDis);
				temp *= temp;
				if (removal)
				{
					dst.at<Vec3b>(y, x)[0] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[0]) / temp);
					dst.at<Vec3b>(y, x)[1] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[1]) / temp);
					dst.at<Vec3b>(y, x)[2] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[2]) / temp);
				}
				else {
					dst.at<Vec3b>(y, x)[0] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[0]) * temp);
					dst.at<Vec3b>(y, x)[1] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[1]) * temp);
					dst.at<Vec3b>(y, x)[2] = saturate_cast<uchar>((src.at<Vec3b>(y, x)[2]) * temp);
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
Vignetting_CPP* Vignetting_Open() {
	Vignetting_CPP* cPtr = new Vignetting_CPP();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* Vignetting_Close(Vignetting_CPP* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* Vignetting_RunCPP(Vignetting_CPP* cPtr, int* dataPtr, int rows, int cols, double radius, double centerX, double centerY, bool removal)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
	cPtr->RunCPP(radius, centerX, centerY, removal);
	return (int*)cPtr->dst.data;
}







class WarpModel
{
private:
public:
	int warpMode = MOTION_EUCLIDEAN;
	double termination_eps = 1e-5;
	int number_of_iterations = 50;
	TermCriteria criteria = TermCriteria(TermCriteria::COUNT + TermCriteria::EPS, number_of_iterations, termination_eps);
	Mat warpMatrix;
	WarpModel() {}
	void Run(Mat src1, Mat src2) {
		if (warpMode != MOTION_HOMOGRAPHY)
			warpMatrix = Mat::eye(2, 3, CV_32F);
		else
			warpMatrix = Mat::eye(3, 3, CV_32F);
		findTransformECC(src1, src2, warpMatrix, warpMode, criteria); // wish this were available in C#/VB.Net?
	}
};

extern "C" __declspec(dllexport)
WarpModel* WarpModel_Open() {
	WarpModel* cPtr = new WarpModel();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* WarpModel_Close(WarpModel* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* WarpModel_Run(WarpModel* cPtr, int* src1Ptr, int* src2Ptr, int rows, int cols, int channels, int warpMode)
{
	Mat src1 = Mat(rows, cols, CV_8UC1, src1Ptr);
	Mat src2 = Mat(rows, cols, CV_8UC1, src2Ptr);
	cPtr->warpMode = warpMode; // don't worry - the enumerations are identical...
	cPtr->Run(src1, src2);
	return (int*)cPtr->warpMatrix.data;
}






// https://blog.csdn.net/just_sort/article/details/85982871
class WhiteBalance
{
private:
public:
	Mat src, output;
	WhiteBalance() {}

	void Run(float thresholdVal)
	{
		int row = src.rows, col = src.cols;
		int HistRGB[768] = { 0 };
		int MaxVal = 0;
		for (int i = 0; i < row; i++) {
			for (int j = 0; j < col; j++) {
				int b = int(src.at<Vec3b>(i, j)[0]);
				int g = int(src.at<Vec3b>(i, j)[1]);
				int r = int(src.at<Vec3b>(i, j)[2]);
				MaxVal = max(MaxVal, b);
				MaxVal = max(MaxVal, g);
				MaxVal = max(MaxVal, r);
				int sum = b + g + r;
				HistRGB[sum]++;
			}
		}
		int Threshold = 0;
		int sum = 0;
		for (int i = 767; i >= 0; i--) {
			sum += HistRGB[i];
			if (sum > int(row * col) * thresholdVal) {
				Threshold = i;
				break;
			}
		}

		int AvgB = 0;
		int AvgG = 0;
		int AvgR = 0;
		int cnt = 0;
		for (int i = 0; i < row; i++) {
			for (int j = 0; j < col; j++) {
				int sumP = src.at<Vec3b>(i, j)[0] + src.at<Vec3b>(i, j)[1] + src.at<Vec3b>(i, j)[2];
				if (sumP > Threshold) {
					AvgB += src.at<Vec3b>(i, j)[0];
					AvgG += src.at<Vec3b>(i, j)[1];
					AvgR += src.at<Vec3b>(i, j)[2];
					cnt++;
				}
			}
		}
		if (cnt > 0)
		{
			AvgB /= cnt;
			AvgG /= cnt;
			AvgR /= cnt;
			for (int i = 0; i < row; i++) {
				for (int j = 0; j < col; j++) {
					if (AvgB == 0 || AvgG == 0 || AvgR == 0) continue;
					int Blue = src.at<Vec3b>(i, j)[0] * MaxVal / AvgB;
					int Green = src.at<Vec3b>(i, j)[1] * MaxVal / AvgG;
					int Red = src.at<Vec3b>(i, j)[2] * MaxVal / AvgR;
					if (Red > 255) Red = 255; else if (Red < 0) Red = 0;
					if (Green > 255) Green = 255; else if (Green < 0) Green = 0;
					if (Blue > 255) Blue = 255; else if (Blue < 0) Blue = 0;
					output.at<Vec3b>(i, j)[0] = Blue;
					output.at<Vec3b>(i, j)[1] = Green;
					output.at<Vec3b>(i, j)[2] = Red;
				}
			}
		}
	}
};

extern "C" __declspec(dllexport)
WhiteBalance* WhiteBalance_Open(float ppx, float ppy, float fx, float fy)
{
	WhiteBalance* cPtr = new WhiteBalance();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* WhiteBalance_Close(WhiteBalance* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* WhiteBalance_Run(WhiteBalance* cPtr, int* rgb, int rows, int cols, float thresholdVal)
{
	cPtr->output = Mat(rows, cols, CV_8UC3);
	cPtr->src = Mat(rows, cols, CV_8UC3, rgb);
	cPtr->Run(thresholdVal);
	return (int*)cPtr->output.data;
}






class xPhoto_OilPaint
{
private:
public:
	Mat src, dst;
	xPhoto_OilPaint() {}
	void Run(int size, int dynRatio, int colorCode)
	{
		xphoto::oilPainting(src, dst, size, dynRatio, colorCode);
	}
};

extern "C" __declspec(dllexport)
xPhoto_OilPaint* xPhoto_OilPaint_Open()
{
	xPhoto_OilPaint* cPtr = new xPhoto_OilPaint();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* xPhoto_OilPaint_Close(xPhoto_OilPaint* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* xPhoto_OilPaint_Run(xPhoto_OilPaint* cPtr, int* imagePtr, int rows, int cols, int size, int dynRatio, int colorCode)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, imagePtr);
	cPtr->Run(size, dynRatio, colorCode);
	return (int*)cPtr->dst.data;
}






class xPhoto_Inpaint
{
private:
public:
	Mat src, dst;
	xPhoto_Inpaint() {}
	void Run(Mat mask, int iType)
	{
		dst.setTo(0);
		//xphoto::inpaint(src, mask, dst, iType);
	}
};

extern "C" __declspec(dllexport)
xPhoto_Inpaint* xPhoto_Inpaint_Open()
{
	xPhoto_Inpaint* cPtr = new xPhoto_Inpaint();
	return cPtr;
}

extern "C" __declspec(dllexport)
int* xPhoto_Inpaint_Close(xPhoto_Inpaint* cPtr)
{
	delete cPtr;
	return (int*)0;
}

extern "C" __declspec(dllexport)
int* xPhoto_Inpaint_Run(xPhoto_Inpaint* cPtr, int* imagePtr, int* maskPtr, int rows, int cols, int iType)
{
	cPtr->src = Mat(rows, cols, CV_8UC3, imagePtr);
	cPtr->dst = Mat(rows, cols, CV_8UC3);
	Mat mask = Mat(rows, cols, CV_8UC1, maskPtr);
	cPtr->Run(mask, iType);
	return (int*)cPtr->dst.data;
}





class Random_Basics : public CPP_Parent {
public:
	vector<Point2f> pointList;
	Rect range;
	int sizeRequest = 10;

	Random_Basics() : CPP_Parent() {
		traceName = "Random_Basics";
		desc = "Create a uniform random mask with a specified number of pixels.";
	}

	void Run(Mat src) {
		if (range.width == 0 || range.height == 0) range = Rect(0, 0, dst2.cols, dst2.rows);
		pointList.clear();
		while (pointList.size() < sizeRequest) {
			pointList.push_back(Point2f(range.x + float((rand() % range.width)), range.y + float((rand() % range.height))));
		}
	}
};





class OEX_PointsClassifier
{
private:
public:
	vector<Point2f>trainedPoints;
	vector<int> trainedPointsMarkers;
	Mat dst, img, samples;
	vector<Vec3b>  classColors{ Vec3b(255, 0, 0), Vec3b(0, 255, 0) };
	Mat inputPoints;
	Ptr<NormalBayesClassifier> NBC = cv::ml::NormalBayesClassifier::create();
	Ptr<KNearest> KNN = cv::ml::KNearest::create();
	Ptr<SVM> SVM = cv::ml::SVM::create();
	Ptr<DTrees> DTR = cv::ml::DTrees::create();
	Ptr<Boost> BTR = cv::ml::Boost::create();
	Ptr<RTrees> RF = cv::ml::RTrees::create();
	Ptr<ANN_MLP> ANN = cv::ml::ANN_MLP::create();
	Ptr<EM> EM = cv::ml::EM::create();
	OEX_PointsClassifier() {}

	void OEX_Setup(int count, int rows, int cols) {
		inputPoints = Mat(rows * cols, 2, CV_32F);
		Point2f pt;
		int index = 0;
		for (int y = 0; y < rows; y++) {
			for (int x = 0; x < cols; x++) {
				pt.x = (float)x;
				pt.y = (float)y;
				inputPoints.at<float>(index++) = pt.x;
				inputPoints.at<float>(index++) = pt.y;
			}
		}
		Random_Basics* random = new Random_Basics();
		random->sizeRequest = count;

		random->range = Rect(0, 0, cols * 3 / 4, rows * 3 / 4);
		random->Run(Mat());

		trainedPoints = random->pointList;
		trainedPointsMarkers.clear();
		for (int i = 0; i < int(trainedPoints.size()); i++)
			trainedPointsMarkers.push_back(0);

		random->range = Rect(cols / 4, rows / 4, cols * 3 / 4, rows * 3 / 4);
		random->Run(Mat());
		for (int i = 0; i < int(random->pointList.size()); i++)
		{
			trainedPoints.push_back(random->pointList[i]);
			trainedPointsMarkers.push_back(1);
		}
	}

	void RunCPP(int methodIndex, int reset) {
		samples = Mat(trainedPoints).reshape(1, (int)trainedPoints.size());
		auto trainInput = TrainData::create(samples, ROW_SAMPLE, Mat(trainedPointsMarkers));

		dst.setTo(0);

		switch (methodIndex) {
		case 0:
		{
			if (reset) {
				NBC->train(trainInput);
			}
			NBC->predict(inputPoints, dst);
			break;
		}
		case 1:
		{
			if (reset) {
				KNN->setDefaultK(15);
				KNN->setIsClassifier(true);
				KNN = StatModel::train<KNearest>(trainInput);
			}
			KNN->predict(inputPoints, dst);
			break;
		}
		case 2:
		{
			if (reset) {
				SVM->setType(SVM::C_SVC);
				SVM->setKernel(SVM::POLY); //SVM::LINEAR;
				SVM->setDegree(0.5);
				SVM->setGamma(1);
				SVM->setCoef0(1);
				SVM->setNu(0.5);
				SVM->setP(0);
				SVM->setTermCriteria(TermCriteria(TermCriteria::MAX_ITER + TermCriteria::EPS, 1000, 0.01));
				int C = 1;
				SVM->setC(C);
				SVM->train(trainInput);
			}
			SVM->predict(inputPoints, dst);
			break;
		}
		case 3:
		{
			if (reset) {
				DTR->setMaxDepth(8);
				DTR->setMinSampleCount(2);
				DTR->setUseSurrogates(false);
				DTR->setCVFolds(0); // the number of cross-validation folds
				DTR->setUse1SERule(false);
				DTR->setTruncatePrunedTree(false);
				DTR->train(trainInput);
			}
			DTR->predict(inputPoints, dst);
			break;
		}
		case 4:
		{
			if (reset) {
				BTR->setBoostType(Boost::DISCRETE);
				BTR->setWeakCount(100);
				BTR->setWeightTrimRate(0.95);
				BTR->setMaxDepth(2);
				BTR->setUseSurrogates(false);
				BTR->setPriors(Mat());
				BTR->train(trainInput);
			}
			BTR->predict(inputPoints, dst);
			break;
		}
		case 5:
		{
			if (reset) {
				RF->setMaxDepth(4);
				RF->setMinSampleCount(2);
				RF->setRegressionAccuracy(0.f);
				RF->setUseSurrogates(false);
				RF->setMaxCategories(16);
				RF->setPriors(Mat());
				RF->setCalculateVarImportance(false);
				RF->setActiveVarCount(1);
				RF->setTermCriteria(TermCriteria(TermCriteria::MAX_ITER, 5, 0));
				RF->train(trainInput);
			}
			RF->predict(inputPoints, dst);
			break;
		}
		case 6:
		{
			if (reset)
			{
				Mat layer_sizes(1, 3, CV_32SC1, { 2, 5, 2 });
				Mat trainClasses = Mat::zeros((int)trainedPoints.size(), (int)classColors.size(), CV_32FC1);
				for (int i = 0; i < trainClasses.rows; i++)
				{
					trainClasses.at<float>(i, trainedPointsMarkers[i]) = 1.f;
				}
				Ptr<TrainData> tdata = TrainData::create(samples, ROW_SAMPLE, trainClasses);

				ANN->setLayerSizes(layer_sizes);
				ANN->setActivationFunction(ANN_MLP::SIGMOID_SYM, 1, 1);
				ANN->setTermCriteria(TermCriteria(TermCriteria::MAX_ITER + TermCriteria::EPS, 300, FLT_EPSILON));
				ANN->setTrainMethod(ANN_MLP::BACKPROP, 0.001);
				ANN->train(tdata);
			}

			ANN->predict(inputPoints, dst);
			break;
		}
		case 7:
		{
			if (reset) {
				int i, j, nmodels = (int)classColors.size();
				vector<Ptr<cv::ml::EM> > em_models(nmodels);
				Mat modelSamples;

				for (i = 0; i < nmodels; i++)
				{
					const int componentCount = 3;

					modelSamples.release();
					for (j = 0; j < samples.rows; j++)
					{
						if (trainedPointsMarkers[j] == i)
							modelSamples.push_back(samples.row(j));
					}

					// learn models
					if (!modelSamples.empty())
					{
						Ptr<cv::ml::EM> em = EM::create();
						em->setClustersNumber(componentCount);
						em->setCovarianceMatrixType(EM::COV_MAT_DIAGONAL);
						em->trainEM(modelSamples, noArray(), noArray(), noArray());
						em_models[i] = em;
					}
				}

				// classify coordinate plane points using the bayes classifier, i.e.
				// y(x) = arg max_i=1_modelsCount likelihoods_i(x)
				Mat testSample(1, 2, CV_32FC1);
				Mat logLikelihoods(1, nmodels, CV_64FC1, Scalar(-DBL_MAX));

				for (int y = 0; y < img.rows; y++)
				{
					for (int x = 0; x < img.cols; x++)
					{
						testSample.at<float>(0) = (float)x;
						testSample.at<float>(1) = (float)y;

						for (i = 0; i < nmodels; i++)
						{
							if (!em_models[i].empty())
								logLikelihoods.at<double>(i) = em_models[i]->predict2(testSample, noArray())[0];
						}
						Point maxLoc;
						minMaxLoc(logLikelihoods, 0, 0, 0, &maxLoc);
						dst.at<Vec3b>(y, x) = classColors[maxLoc.x];
					}
				}
			}
			break;
		}
		}

	}
};
extern "C" __declspec(dllexport)
OEX_PointsClassifier* OEX_Points_Classifier_Open() {
	OEX_PointsClassifier* cPtr = new OEX_PointsClassifier();
	return cPtr;
}
extern "C" __declspec(dllexport)
void OEX_Points_Classifier_Close(OEX_PointsClassifier* cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport)
int* OEX_ShowPoints(OEX_PointsClassifier* cPtr, int imgRows, int imgCols, int radius)
{
	cPtr->img = Mat(imgRows, imgCols, CV_8UC3);
	cPtr->img.setTo(0);
	for (int i = 0; i < cPtr->trainedPoints.size(); i++)
	{
		auto color = cPtr->trainedPointsMarkers[i] == 0 ? Scalar(255, 255, 255) : Scalar(0, 255, 255);
		circle(cPtr->img, cPtr->trainedPoints[i], radius, color, -1, LINE_AA);
	}
	return (int*)cPtr->img.data;
}

extern "C" __declspec(dllexport)
int* OEX_Points_Classifier_RunCPP(OEX_PointsClassifier* cPtr, int count, int methodIndex, int imgRows, int imgCols, int reset)
{
	if (reset) cPtr->OEX_Setup(count, imgRows, imgCols);
	cPtr->RunCPP(methodIndex, reset);
	return (int*)cPtr->dst.data;
}











class Classifier_Bayesian
{
private:
public:
	vector<Vec3f>trainPoints;
	vector<int> trainMarkers;
	Mat inputPoints;
	vector<int> responses;
	Ptr<NormalBayesClassifier> NBC = cv::ml::NormalBayesClassifier::create();
	Classifier_Bayesian() {}

	void trainModel(Scalar* trainInput, int* trainResponse, int count) {
		trainPoints.clear();
		trainMarkers.clear();
		for (int i = 0; i < count; i++)
		{
			Vec3f vec(trainInput[i][0], trainInput[i][1], trainInput[i][2]);
			trainPoints.push_back(vec);
			trainMarkers.push_back(trainResponse[i]);
		}
		Mat samples = Mat(trainPoints).reshape(1, (int)trainPoints.size());
		auto inputSamples = TrainData::create(samples, ROW_SAMPLE, Mat(trainMarkers));
		NBC->train(inputSamples);
	}

	void RunCPP(Scalar* input, int count) {
		Mat testSample(1, 3, CV_32FC1);
		responses.clear();
		for (int i = 0; i < count; i++)
		{
			testSample.at<float>(0) = input[i][0];
			testSample.at<float>(1) = input[i][1];
			testSample.at<float>(2) = input[i][2];

			responses.push_back(NBC->predict(testSample));
		}
	}
};
extern "C" __declspec(dllexport)
Classifier_Bayesian* Classifier_Bayesian_Open() {
	Classifier_Bayesian* cPtr = new Classifier_Bayesian();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Classifier_Bayesian_Close(Classifier_Bayesian* cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
void Classifier_Bayesian_Train(Classifier_Bayesian* cPtr, Scalar* trainInput, int* trainResponse, int count)
{
	cPtr->trainModel(trainInput, trainResponse, count);
}

extern "C" __declspec(dllexport)
int* Classifier_Bayesian_RunCPP(Classifier_Bayesian* cPtr, Scalar* input, int count)
{
	cPtr->RunCPP(input, count);
	return (int*)&cPtr->responses[0];
}




class canvas {
public:
	bool setupQ;
	cv::Point origin;
	cv::Point corner;
	int minDims, maxDims;
	double scale;
	int rows, cols;
	cv::Mat img;

	void init(int minD, int maxD) {
		// Initialise the canvas with minimum and maximum rows and column sizes.
		minDims = minD; maxDims = maxD;
		origin = cv::Point(0, 0);
		corner = cv::Point(0, 0);
		scale = 1.0;
		rows = 0;
		cols = 0;
		setupQ = false;
	}

	void stretch(cv::Point2f min, cv::Point2f max) {
		// Stretch the canvas to include the points min and max.
		if (setupQ) {
			if (corner.x < max.x) { corner.x = (int)(max.x + 1.0); };
			if (corner.y < max.y) { corner.y = (int)(max.y + 1.0); };
			if (origin.x > min.x) { origin.x = (int)min.x; };
			if (origin.y > min.y) { origin.y = (int)min.y; };
		}
		else {
			origin = cv::Point((int)min.x, (int)min.y);
			corner = cv::Point((int)(max.x + 1.0), (int)(max.y + 1.0));
		}

		int c = (int)(scale * ((corner.x + 1.0) - origin.x));
		if (c < minDims) {
			scale = scale * (double)minDims / (double)c;
		}
		else {
			if (c > maxDims) {
				scale = scale * (double)maxDims / (double)c;
			}
		}
		int r = (int)(scale * ((corner.y + 1.0) - origin.y));
		if (r < minDims) {
			scale = scale * (double)minDims / (double)r;
		}
		else {
			if (r > maxDims) {
				scale = scale * (double)maxDims / (double)r;
			}
		}
		cols = (int)(scale * ((corner.x + 1.0) - origin.x));
		rows = (int)(scale * ((corner.y + 1.0) - origin.y));
		setupQ = true;
	}

	void stretch(vector<Point2f> pts)
	{   // Stretch the canvas so all the points pts are on the canvas.
		cv::Point2f min = pts[0];
		cv::Point2f max = pts[0];
		for (size_t i = 1; i < pts.size(); i++) {
			Point2f pnt = pts[i];
			if (max.x < pnt.x) { max.x = pnt.x; };
			if (max.y < pnt.y) { max.y = pnt.y; };
			if (min.x > pnt.x) { min.x = pnt.x; };
			if (min.y > pnt.y) { min.y = pnt.y; };
		};
		stretch(min, max);
	}

	void stretch(cv::RotatedRect box)
	{   // Stretch the canvas so that the rectangle box is on the canvas.
		cv::Point2f min = box.center;
		cv::Point2f max = box.center;
		cv::Point2f vtx[4];
		box.points(vtx);
		for (int i = 0; i < 4; i++) {
			cv::Point2f pnt = vtx[i];
			if (max.x < pnt.x) { max.x = pnt.x; };
			if (max.y < pnt.y) { max.y = pnt.y; };
			if (min.x > pnt.x) { min.x = pnt.x; };
			if (min.y > pnt.y) { min.y = pnt.y; };
		}
		stretch(min, max);
	}

	void drawEllipseWithBox(cv::RotatedRect box, cv::Scalar color, int lineThickness)
	{
		if (img.empty()) {
			stretch(box);
			img = cv::Mat::zeros(rows, cols, CV_8UC3);
		}

		box.center = scale * cv::Point2f(box.center.x - origin.x, box.center.y - origin.y);
		box.size.width = (float)(scale * box.size.width);
		box.size.height = (float)(scale * box.size.height);

		ellipse(img, box, color, lineThickness, LINE_AA);

		Point2f vtx[4];
		box.points(vtx);
		for (int j = 0; j < 4; j++) {
			line(img, vtx[j], vtx[(j + 1) % 4], color, lineThickness, LINE_AA);
		}
	}

	void drawPoints(vector<Point2f> pts, cv::Scalar color)
	{
		if (img.empty()) {
			stretch(pts);
			img = cv::Mat::zeros(rows, cols, CV_8UC3);
		}
		for (size_t i = 0; i < pts.size(); i++) {
			Point2f pnt = scale * cv::Point2f(pts[i].x - origin.x, pts[i].y - origin.y);
			img.at<cv::Vec3b>(int(pnt.y), int(pnt.x))[0] = (uchar)color[0];
			img.at<cv::Vec3b>(int(pnt.y), int(pnt.x))[1] = (uchar)color[1];
			img.at<cv::Vec3b>(int(pnt.y), int(pnt.x))[2] = (uchar)color[2];
		};
	}

	void drawLabels()
	{
		if (img.empty())
			img = cv::Mat::zeros(rows, cols, CV_8UC3);
		else
			img.setTo(0);
	}

};



inline static bool isGoodBox(const RotatedRect& box) {
	//size.height >= size.width awalys,only if the pts are on a line or at the same point,size.width=0
	return (box.size.height <= box.size.width * 30) && (box.size.width > 0);
}



class OEX_FitEllipse
{
private:
public:
	Mat src;
	OEX_FitEllipse() {}
	canvas paper;

	cv::Scalar white = Scalar(255, 255, 255);

	void RunCPP(int threshold, int fitType) {
		RotatedRect box;
		vector<vector<Point> > contours;
		Mat bimage = src >= threshold;

		findContours(bimage, contours, RETR_LIST, CHAIN_APPROX_NONE);

		paper.init(int(0.8 * MIN(bimage.rows, bimage.cols)), int(1.2 * MAX(bimage.rows, bimage.cols)));
		paper.stretch(cv::Point2f(0.0f, 0.0f), cv::Point2f((float)(bimage.cols + 2.0), (float)(bimage.rows + 2.0)));

		paper.drawLabels();

		int margin = 2;
		vector< vector<Point2f> > points;
		for (size_t i = 0; i < contours.size(); i++)
		{
			size_t count = contours[i].size();
			if (count < 6)
				continue;

			Mat pointsf;
			Mat(contours[i]).convertTo(pointsf, CV_32F);

			vector<Point2f>pts;
			for (int j = 0; j < pointsf.rows; j++) {
				Point2f pnt = Point2f(pointsf.at<float>(j, 0), pointsf.at<float>(j, 1));
				if ((pnt.x > margin && pnt.y > margin && pnt.x < bimage.cols - margin && pnt.y < bimage.rows - margin)) {
					if (j % 20 == 0) {
						pts.push_back(pnt);
					}
				}
			}
			points.push_back(pts);
		}

		for (size_t i = 0; i < points.size(); i++)
		{
			vector<Point2f> pts = points[i];

			//At least 5 points can fit an ellipse
			if (pts.size() < 5) {
				continue;
			}

			Scalar color;
			switch (fitType) {
			case 0: //fitEllipseQ) {
				box = fitEllipse(pts);
				color = Scalar(255, 0, 0);
				break;
			case 1: // fitEllipseAMSQ) {
				box = fitEllipseAMS(pts);
				color = Scalar(0, 255, 0);
				break;
			case 2:  // fitEllipseDirectQ) {
				box = fitEllipseDirect(pts);
				color = Scalar(0, 0, 255);
				break;
			}
			if (isGoodBox(box)) paper.drawEllipseWithBox(box, color, 3);
			paper.drawPoints(pts, white);
		}
	}
};
extern "C" __declspec(dllexport)
OEX_FitEllipse* OEX_FitEllipse_Open() {
	OEX_FitEllipse* cPtr = new OEX_FitEllipse();
	return cPtr;
}
extern "C" __declspec(dllexport)
void OEX_FitEllipse_Close(OEX_FitEllipse* cPtr)
{
	delete cPtr;
}
extern "C" __declspec(dllexport)
int* OEX_FitEllipse_RunCPP(OEX_FitEllipse* cPtr, int* dataPtr, int rows, int cols, int threshold, int fitType)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
	cPtr->RunCPP(threshold, fitType);
	return (int*)cPtr->paper.img.data;
}







class Neighbors1
{
private:
public:
	Mat src, contour;
	vector<Point> nPoints;
	vector<uchar> cellData;
	void RunCPP()
	{
		nPoints.clear();
		cellData.clear();
		for (int y = 1; y < src.rows - 3; y++)
			for (int x = 1; x < src.cols - 3; x++)
			{
				vector<uchar> nabs;
				vector<uchar> ids = { 0, 0, 0, 0 };
				int index = 0;
				for (int yy = y; yy < y + 2; yy++)
				{
					for (int xx = x; xx < x + 2; xx++)
					{
						uchar val = src.at<uchar>(yy, xx);
						if (count(nabs.begin(), nabs.end(), val) == 0)
						{
							nabs.push_back(val);
						}
						ids[index++] = val;
					}
				}
				if (nabs.size() > 2)
				{
					nPoints.push_back(Point(x, y));
					cellData.push_back(ids[0]);
					cellData.push_back(ids[1]);
					cellData.push_back(ids[2]);
					cellData.push_back(ids[3]);
				}
			}
	}
};

extern "C" __declspec(dllexport)
Neighbors1* Neighbors1_Open() {
	Neighbors1* cPtr = new Neighbors1();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Neighbors1_Close(Neighbors1* cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* Neighbors1_CellData(Neighbors1* cPtr)
{
	return (int*)&cPtr->cellData[0];
}

extern "C" __declspec(dllexport)
int* Neighbors1_Points(Neighbors1* cPtr)
{
	return (int*)&cPtr->nPoints[0];
}

extern "C" __declspec(dllexport)
int Neighbors1_RunCPP(Neighbors1* cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
	cPtr->RunCPP();
	return (int)cPtr->nPoints.size();
}








class Neighbor2
{
private:
public:
	Mat src, contour;
	vector<Point> nPoints;
	void RunCPP()
	{
		nPoints.clear();
		for (int y = 1; y < src.rows - 2; y++)
			for (int x = 1; x < src.cols - 2; x++)
			{
				Point pt = Point(src.at<uchar>(y, x), src.at<uchar>(y, x - 1));
				if (pt.x == pt.y) continue;
				if (pt.x == 0 || pt.y == 0) continue;
				if (count(nPoints.begin(), nPoints.end(), pt) == 0)
				{
					pt = Point(src.at<uchar>(y, x - 1), src.at<uchar>(y, x));
					if (count(nPoints.begin(), nPoints.end(), pt) == 0) nPoints.push_back(pt);
				}
			}
	}
};

extern "C" __declspec(dllexport)
Neighbor2* Neighbor2_Open() {
	Neighbor2* cPtr = new Neighbor2();
	return cPtr;
}
extern "C" __declspec(dllexport)
void Neighbor2_Close(Neighbor2* cPtr)
{
	delete cPtr;
}

extern "C" __declspec(dllexport)
int* Neighbor2_Points(Neighbor2* cPtr)
{
	return (int*)&cPtr->nPoints[0];
}

extern "C" __declspec(dllexport)
int Neighbor2_RunCPP(Neighbor2* cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
	cPtr->RunCPP();
	return (int)cPtr->nPoints.size();
}






class Neighbors
{
private:
public:
	Mat src;
	vector <Point> nabList;

	Neighbors() {}
	void checkPoint(Point pt)
	{
		if (pt.x != pt.y)
		{
			if (pt.x > pt.y) pt = Point(pt.y, pt.x);
			std::vector<Point>::iterator it = std::find(nabList.begin(), nabList.end(), pt);
			if (it == nabList.end()) nabList.push_back(pt);
		}
	}
	void RunCPP() {
		nabList.clear();
		for (int y = 1; y < src.rows; y++)
			for (int x = 1; x < src.cols; x++)
			{
				uchar val = src.at<uchar>(y, x);
				checkPoint(Point(src.at<uchar>(y, x - 1), val));
				checkPoint(Point(src.at<uchar>(y - 1, x), val));
			}
	}
};
extern "C" __declspec(dllexport) Neighbors* Neighbors_Open() { Neighbors* cPtr = new Neighbors(); return cPtr; }
extern "C" __declspec(dllexport) void Neighbors_Close(Neighbors* cPtr) { delete cPtr; }
extern "C" __declspec(dllexport) int* Neighbors_NabList(Neighbors* cPtr) { return (int*)&cPtr->nabList[0]; }
extern "C" __declspec(dllexport)
int Neighbors_RunCPP(Neighbors* cPtr, int* dataPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8UC1, dataPtr);
	cPtr->RunCPP();
	return (int)cPtr->nabList.size();
}









class RedCloud
{
private:
public:
	Mat src, result;
	vector<Rect>cellRects;
	vector<Point> floodPoints;

	RedCloud() {}
	void RunCPP(Mat inputMask) {
		Mat maskCopy = inputMask.clone();
		Rect rect;

		multimap<int, Point, greater<int>> sizeSorted;
		int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
		Point pt;
		for (int y = 0; y < src.rows; y++)
		{
			for (int x = 0; x < src.cols; x++)
			{
				if (inputMask.at<unsigned char>(y, x) == 0)
				{
					pt = Point(x, y);
					int count = floodFill(src, inputMask, pt, 255, &rect, 0, 0, 4 | floodFlag | (255 << 8));
					if (rect.width > 1 && rect.height > 1) sizeSorted.insert(make_pair(count, pt));
				}
			}
		}

		cellRects.clear();
		floodPoints.clear();
		int fill = 1;
		for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
		{
			if (floodFill(src, maskCopy, it->second, fill, &rect, 0, 0, 4 | floodFlag | (fill << 8)) >= 1)
			{
				cellRects.push_back(rect);
				floodPoints.push_back(it->second);

				if (fill >= 255)
					break; // just taking up to the top X largest objects found.
				fill++;
			}
		}
		Rect r = Rect(1, 1, inputMask.cols - 2, inputMask.rows - 2);
		maskCopy(r).copyTo(result);
	}
};

extern "C" __declspec(dllexport) RedCloud* RedCloud_Open() { return new RedCloud(); }
extern "C" __declspec(dllexport) int RedCloud_Count(RedCloud* cPtr)
{
	return (int)cPtr->cellRects.size();
}

extern "C" __declspec(dllexport) int* RedCloud_Rects(RedCloud* cPtr)
{
	return (int*)&cPtr->cellRects[0];
}

extern "C" __declspec(dllexport) int* RedCloud_FloodPoints(RedCloud* cPtr)
{
	return (int*)&cPtr->floodPoints[0];
}

extern "C" __declspec(dllexport) int* RedCloud_Close(RedCloud* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int*
RedCloud_Run(RedCloud* cPtr, int* dataPtr, unsigned char* maskPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);

	Mat inputMask = Mat(rows, cols, CV_8U, maskPtr);

	copyMakeBorder(inputMask, inputMask, 1, 1, 1, 1, BORDER_CONSTANT, 0);
	cPtr->RunCPP(inputMask);

	return (int*)cPtr->result.data;
}





class RedCloudMaxDist
{
private:
public:
	Mat src, mask, maskCopy, result;
	vector<Rect>cellRects;
	vector<Point> floodPoints;
	vector<Point> maxList;

	RedCloudMaxDist() {}
	void RunCPP() {
		Rect rect;

		multimap<int, Point, greater<int>> sizeSorted;
		int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
		Point pt;
		for (int y = 0; y < src.rows; y++)
		{
			for (int x = 0; x < src.cols; x++)
			{
				if (mask.at<unsigned char>(y, x) == 0)
				{
					pt = Point(x, y);
					int count = floodFill(src, mask, pt, 255, &rect, 0, 0, 4 | floodFlag | (255 << 8));
					if (rect.width > 1 && rect.height > 1) sizeSorted.insert(make_pair(count, pt));
				}
			}
		}

		cellRects.clear();
		floodPoints.clear();
		int fill = 1;
		for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
		{
			if (floodFill(src, maskCopy, it->second, fill, &rect, 0, 0, 4 | floodFlag | (fill << 8)) >= 1)
			{
				cellRects.push_back(rect);
				floodPoints.push_back(it->second);

				if (fill >= 255)
					break; // just taking up to the top X largest objects found.
				fill++;
			}
		}
	}

	void RunMaxList() {
		Rect rect;

		int floodFlag = 4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE;
		multimap<int, Point, greater<int>> sizeSorted;
		for (size_t i = 0; i < maxList.size(); i++)
		{
			int count = floodFill(src, mask, maxList[i], 255, &rect, 0, 0, 4 | floodFlag | (255 << 8));
			if (rect.width > 1 && rect.height > 1) sizeSorted.insert(make_pair(count, maxList[i]));
		}

		Point pt;
		for (int y = 0; y < src.rows; y++)
		{
			for (int x = 0; x < src.cols; x++)
			{
				if (mask.at<unsigned char>(y, x) == 0)
				{
					pt = Point(x, y);
					int count = floodFill(src, mask, pt, 255, &rect, 0, 0, 4 | floodFlag | (255 << 8));
					if (rect.width > 1 && rect.height > 1) sizeSorted.insert(make_pair(count, pt));
				}
			}
		}

		cellRects.clear();
		floodPoints.clear();
		int fill = 1;
		for (auto it = sizeSorted.begin(); it != sizeSorted.end(); it++)
		{
			if (floodFill(src, maskCopy, it->second, fill, &rect, 0, 0, 4 | floodFlag | (fill << 8)) >= 1)
			{
				cellRects.push_back(rect);
				floodPoints.push_back(it->second);

				if (fill >= 255)
					break; // just taking up to the top X largest objects found.
				fill++;
			}
		}
	}
};

extern "C" __declspec(dllexport) RedCloudMaxDist* RedCloudMaxDist_Open() { return new RedCloudMaxDist(); }
extern "C" __declspec(dllexport) int RedCloudMaxDist_Count(RedCloudMaxDist* cPtr)
{
	return (int)cPtr->cellRects.size();
}

extern "C" __declspec(dllexport) int* RedCloudMaxDist_Rects(RedCloudMaxDist* cPtr)
{
	return (int*)&cPtr->cellRects[0];
}

extern "C" __declspec(dllexport) int* RedCloudMaxDist_FloodPoints(RedCloudMaxDist* cPtr)
{
	return (int*)&cPtr->floodPoints[0];
}

extern "C" __declspec(dllexport) void
RedCloudMaxDist_SetPoints(RedCloudMaxDist* cPtr, int count, int* dataPtr)
{
	Mat maxList = Mat(count, 1, CV_32SC2, dataPtr);
	cPtr->maxList.clear();
	for (int i = 0; i < count; i++)
	{
		cPtr->maxList.push_back(maxList.at<Point>(i, 0));
	}
}

extern "C" __declspec(dllexport) int* RedCloudMaxDist_Close(RedCloudMaxDist* cPtr) { delete cPtr; return (int*)0; }
extern "C" __declspec(dllexport) int*
RedCloudMaxDist_Run(RedCloudMaxDist* cPtr, int* dataPtr, unsigned char* maskPtr, int rows, int cols)
{
	cPtr->src = Mat(rows, cols, CV_8U, dataPtr);
	cPtr->mask = Mat::zeros(rows + 2, cols + 2, CV_8U);
	cPtr->mask.setTo(0);
	Rect r = Rect(1, 1, cols, rows);
	if (maskPtr != 0)
	{
		Mat inputMask;
		inputMask = Mat(rows, cols, CV_8U, maskPtr);
		inputMask.copyTo(cPtr->mask(r));
	}
	cPtr->maskCopy = cPtr->mask.clone();
	if (cPtr->maxList.size() > 0)
		cPtr->RunMaxList();
	else
		cPtr->RunCPP();
	cPtr->maskCopy(r).copyTo(cPtr->result);
	return (int*)cPtr->result.data;
}