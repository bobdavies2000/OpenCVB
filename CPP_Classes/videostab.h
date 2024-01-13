#ifndef VIDEOSTAB_H
#define VIDEOSTAB_H
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

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
	