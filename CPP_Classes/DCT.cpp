#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace  cv;

// https://www.codeproject.com/Articles/5362105/Perceptual-Hash-based-Image-Comparison-Coded-in-pl
double dctSimple(double* DCTMatrix, double* ImgMatrix, int N, int M)
{
    int i, j, u, v;
    int cnt = 0;
    double DCTsum = 0.0;
    for (u = 0; u < N; ++u)
    {
        for (v = 0; v < M; ++v)
        {
            DCTMatrix[(u * N) + v] = 0;
            for (i = 0; i < N; i++)
            {
                for (j = 0; j < M; j++)
                {
                    DCTMatrix[(u * N) + v] += ImgMatrix[(i * N) + j]
                        * cos(CV_PI / ((double)N) * (i + 1. / 2.) * u)
                        * cos(CV_PI / ((double)M) * (j + 1. / 2.) * v);
                    cnt = cnt++;
                }
            }
            DCTsum += DCTMatrix[(u * N) + v];
        }
    }
    DCTsum -= DCTMatrix[0];
    return   DCTsum / cnt;
}






int CoeffFlag = 0;
double Coeff[32][32];

// https://www.codeproject.com/Articles/5362105/Perceptual-Hash-based-Image-Comparison-Coded-in-pl
double DCT(double* DCTMatrix, double* IMGMatrix)
{
    /*
        Concerning: the expression
        dctRCvalue+= IMGMatrix[(imgRow * RowMax) + imgCol]    // this is dependent on the IMAGE
        * cos( PIover32 *(imgRow+0.5)*dctRow)    //  this is a set of FIXED values
        * cos( PIover32 *(imgCol+0.5)*dctCol);   //  this is a set of FIXED values

        Let us call the  2 sets of FIXED values   rCoeff and cCoeff
        they both have the same set of values
        =  cos (  PIover32  * ( x + 0.5 ) * y )   for x = 0 .. 31    and y = for x = 0 .. 31
        = 32*32 distinct COSINE values
        = we could calculate these COSINE values in advance, place them in an array, and (hopefully) speed things up by doing a simple look up .
    */
#define  PIover32 0.09817477042
#define  RowMax 32
#define  ColMax 32    
    int imgRow, imgCol;
    int dctRow, dctCol;
    int x, y;
    int cnt = 0;
    double DCTsum = 0.0;
    double dctRCvalue = 0.0;

    if (!CoeffFlag)
    {
        for (x = 0; x < 32; x++)
        {
            for (y = 0; y < 32; y++)
            {
                Coeff[x][y] = cos(PIover32 * (x + 0.5) * y);
            }
        }
        CoeffFlag = 1;
    }

    for (dctRow = 0; dctRow < 8; dctRow++)
    {
        for (dctCol = 0; dctCol < 8; dctCol++)
        {
            dctRCvalue = 0;

            for (imgRow = 0; imgRow < RowMax; imgRow++)
            {
                for (imgCol = 0; imgCol < ColMax; imgCol++)
                {
                    dctRCvalue += IMGMatrix[(imgRow * RowMax) + imgCol]
                        * Coeff[imgRow][dctRow]    //    cos( PIover32 *(imgRow+0.5)*dctRow)   
                        * Coeff[imgCol][dctCol];  //    cos( PIover32 *(imgCol+0.5)*dctCol) ; 
                    cnt = cnt++;
                }
            }
            DCTMatrix[(dctRow * RowMax) + dctCol] = dctRCvalue;
            DCTsum += dctRCvalue;
        }
    }
    DCTsum -= DCTMatrix[0];
    return   DCTsum / cnt;
}