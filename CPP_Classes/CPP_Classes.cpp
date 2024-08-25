#include "CPP_Classes.h"
#include <string.h>
#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/ximgproc.hpp>
#include <opencv2/highgui.hpp>
#include "opencv2/core/utility.hpp"
#include "opencv2/ml/ml.hpp "
#include "opencv2/imgproc.hpp"
#include "opencv2/videoio.hpp"
#include <numeric>
#include <iomanip>
#include <sstream>
#include <memory>
#include <vector>
#include <random>
#include "opencv2/video/tracking.hpp"
#include "opencv2/bgsegm.hpp"
#include "opencv2/photo.hpp"
#include <map>
#include <opencv2/ml.hpp>
#include <opencv2/plot.hpp>
#include "opencv2/ccalib/randpattern.hpp"
#include "opencv2/xphoto/oilpainting.hpp"

using namespace System;

namespace CPP_Classes {
    class AddWeighted_Basics_CC
    {
    public:
        double weight;
        cv::Mat src2;
        cv::Mat dst2;
        AddWeighted_Basics_CC()
        {
            auto desc = "Add 2 images with specified weights.";
        }

        void RunCS(cv::Mat& src)
        {
            cv::Mat srcPlus = src2;
            double weight = 0.5;
            addWeighted(src, weight, srcPlus, 1.0 - weight, 0, dst2);
        }
    };
}

