#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"

using namespace std;
using namespace  cv; 

class AgastIO // Include Only version of Agast
{
private:
public:
    std::vector<cv::KeyPoint> keypoints;
    AgastIO() {
        int k = 0;
    }
    Mat Run(Mat src) {
        keypoints.clear();
        static cv::Ptr<cv::AgastFeatureDetector> agastFD = cv::AgastFeatureDetector::create(10,
            true, cv::AgastFeatureDetector::OAST_9_16);
        agastFD->detect(src, keypoints);
        return Mat((int)keypoints.size(), 7, CV_32F, keypoints.data());
    }
};