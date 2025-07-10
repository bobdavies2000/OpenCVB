#pragma once
#if 0 
#include "Options.h"


#ifndef VIDEOSTAB_H
#define VIDEOSTAB_H

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





class Stabilizer_Basics_CC
{
private:
public:
    VideoStab stab;
    Mat rgb;
    Mat smoothedFrame;
    Stabilizer_Basics_CC()
    {
        smoothedFrame = Mat(2, 3, CV_64F);
    }
    void Run()
    {
        smoothedFrame = stab.stabilize(rgb);
    }
};

extern "C" __declspec(dllexport)
Stabilizer_Basics_CC* Stabilizer_Basics_Open()
{
    Stabilizer_Basics_CC* cPtr = new Stabilizer_Basics_CC();
    return cPtr;
}

extern "C" __declspec(dllexport)
int* Stabilizer_Basics_Close(Stabilizer_Basics_CC* cPtr)
{
    delete cPtr;
    return (int*)0;
}

// https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
extern "C" __declspec(dllexport)
int* Stabilizer_Basics_Run(Stabilizer_Basics_CC* cPtr, int* bgrPtr, int rows, int cols)
{
    cPtr->rgb = Mat(rows, cols, CV_8UC3, bgrPtr);
    cvtColor(cPtr->rgb, cPtr->stab.gray, COLOR_BGR2GRAY);
    if (cPtr->stab.lastFrame.rows > 0) cPtr->Run(); // skips the first pass while the frames get loaded.
    cPtr->stab.gray.copyTo(cPtr->stab.lastFrame);
    return (int*)cPtr->stab.smoothedFrame.data;
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




class AddWeighted_Basics_CC : public CPP_Parent {
public:
    double weight;
    cv::Mat src2;
    Options_AddWeighted* options = new Options_AddWeighted();
    AddWeighted_Basics_CC() : CPP_Parent()
    {
        desc = "Add 2 images with specified weights.";
    }
    void Run(cv::Mat src)
    {
        options->Run();
        cv::Mat srcPlus = src2;
        // algorithm user normally provides src2! 
        if (standalone || src2.empty()) srcPlus = task->depthRGB;
        if (srcPlus.type() != src.type())
        {
            if (src.type() != CV_8UC3 || srcPlus.type() != CV_8UC3)
            {
                if (src.type() == CV_32FC1) normalize(src, src, 0, 255, NORM_MINMAX);
                if (srcPlus.type() == CV_32FC1) normalize(srcPlus, srcPlus, 0, 255, NORM_MINMAX);
                if (src.type() != CV_8UC3) cv::cvtColor(src, src, cv::COLOR_GRAY2BGR);
                if (srcPlus.type() != CV_8UC3) cv::cvtColor(srcPlus, srcPlus, cv::COLOR_GRAY2BGR);
            }
        }

        weight = options->addWeighted;
        cv::addWeighted(src, weight, srcPlus, 1.0 - weight, 0, dst2);
        labels[2] = "Depth %: " + std::to_string(100 - weight * 100) + " BGR %: " + std::to_string(static_cast<int>(weight * 100));
    }
};







class Resize_Basics_CC : public CPP_Parent {
public:
    Size newSize;
    float resizePercent = 0.5;

    Resize_Basics_CC() : CPP_Parent() {
        if (standalone) {
            task->drawRect = Rect(dst2.cols / 4, dst2.rows / 4, dst2.cols / 2, dst2.rows / 2);
        }
        desc = "Resize with different options and compare them";
        labels[2] = "Rectangle highlight above resized";
    }

    void Run(Mat src) {
        if (task->drawRect.width != 0) {
            src = src(task->drawRect);
            newSize = task->drawRect.size();
        }

        resize(src, dst2, Size(int(src.cols * resizePercent), int(src.rows * resizePercent)), 0, 0, INTER_NEAREST_EXACT);
    }
};






class Remap_Basics_CC : public CPP_Parent {
public:
    int direction = 3;  // Default to remap horizontally and vertically
    Mat mapx1, mapx2, mapx3, mapy1, mapy2, mapy3;

    Remap_Basics_CC() : CPP_Parent() {
        // Initialize map matrices with appropriate size and type
        mapx1 = Mat::zeros(dst2.size(), CV_32FC1);
        mapy1 = Mat::zeros(dst2.size(), CV_32FC1);
        mapx2 = Mat::zeros(dst2.size(), CV_32FC1);
        mapy2 = Mat::zeros(dst2.size(), CV_32FC1);
        mapx3 = Mat::zeros(dst2.size(), CV_32FC1);
        mapy3 = Mat::zeros(dst2.size(), CV_32FC1);

        // Populate map matrices with values for remapping
        for (int j = 0; j < mapx1.rows; j++) {
            for (int i = 0; i < mapx1.cols; i++) {
                mapx1.at<float>(j, i) = i;
                mapy1.at<float>(j, i) = dst2.rows - j;
                mapx2.at<float>(j, i) = dst2.cols - i;
                mapy2.at<float>(j, i) = j;
                mapx3.at<float>(j, i) = dst2.cols - i;
                mapy3.at<float>(j, i) = dst2.rows - j;
            }
        }

        desc = "Use remap to reflect an image in 4 directions.";
    }

    void Run(Mat src) {
        vector<string> labels = {
            "Remap_Basics - original",
            "Remap vertically",
            "Remap horizontally",
            "Remap horizontally and vertically"
        };
        string label = labels[direction];

        switch (direction) {
        case 0:
            dst2 = src.clone();  // Use clone for copying
            break;
        case 1:
            remap(src, dst2, mapx1, mapy1, INTER_NEAREST);
            break;
        case 2:
            remap(src, dst2, mapx2, mapy2, INTER_NEAREST);
            break;
        case 3:
            remap(src, dst2, mapx3, mapy3, INTER_NEAREST);
            break;
        }

        if (task->heartBeat) {
            direction = (direction + 1) % 4;
        }
    }
};







class Delaunay_Basics_CC : public CPP_Parent {
public:
    std::vector<cv::Point2f> inputPoints;
    cv::Mat facet32s;
    Random_Enumerable_CC* randEnum;
    cv::Subdiv2D subdiv;
    Mat lastColor;
    std::vector<std::vector<cv::Point>> facetlist;
    Delaunay_Basics_CC() : CPP_Parent() {
        randEnum = new Random_Enumerable_CC();
        lastColor = Mat(task->workRes, CV_8UC3);
        lastColor.setTo(0);
        facet32s = cv::Mat::zeros(dst2.size(), CV_32SC1);
        desc = "Subdivide an image based on the points provided.";
    }
    void randomInput(cv::Mat src) {
        randEnum->Run(src);
        inputPoints = randEnum->pointList;
    }
    void Run(cv::Mat src) {
        if (task->heartBeat && standalone) {
            randEnum->Run(src);
            inputPoints = randEnum->pointList;
            dst3 = randEnum->dst2;
        }
        subdiv.initDelaunay(cv::Rect(0, 0, dst2.cols, dst2.rows));
        subdiv.insert(inputPoints);
        std::vector<std::vector<cv::Point2f>> facets;
        std::vector<cv::Point2f> centers;
        subdiv.getVoronoiFacetList(std::vector<int>(), facets, centers);
        std::vector<cv::Vec3b> usedColors;
        facetlist.clear();
        for (size_t i = 0; i < facets.size(); i++) {
            std::vector<cv::Point> nextFacet;
            for (size_t j = 0; j < facets[i].size(); j++) {
                nextFacet.push_back(cv::Point(facets[i][j].x, facets[i][j].y));
            }
            cv::Point2f pt = inputPoints[i];
            cv::Vec3b nextColor = lastColor.at<cv::Vec3b>(pt.y, pt.x);
            if (std::find(usedColors.begin(), usedColors.end(), nextColor) != usedColors.end()) {
                nextColor = task->randomCellColor();
            }
            usedColors.push_back(nextColor);
            cv::fillConvexPoly(dst2, nextFacet, cv::Scalar(nextColor[0], nextColor[1], nextColor[2]));
            cv::fillConvexPoly(facet32s, nextFacet, i, task->lineType);
            facetlist.push_back(nextFacet);
        }
        facet32s.convertTo(dst1, CV_8U);
        lastColor = dst2.clone();
        labels[2] = traceName + ": " + std::to_string(inputPoints.size()) + " cells were present.";
    }
};






class Delaunay_GenerationsNoKNN_CC : public CPP_Parent {
public:
    vector<Point2f> inputPoints;
    Random_Basics_CC* random;
    Delaunay_Basics_CC* facet;
    Mat generationMap;
    Delaunay_GenerationsNoKNN_CC() : CPP_Parent() {
        facet = new Delaunay_Basics_CC();
        random = new Random_Basics_CC();
        generationMap = Mat::zeros(dst3.size(), CV_32S);
        labels = { "", "Mask of unmatched regions - generation set to 0", "Facet Image with index of each region", "Generation counts for each region." };
        desc = "Create a region in an image for each point provided with KNN.";
    }

    void Run(Mat src) {
        if (standalone && task->heartBeat) {
            random->Run(empty);
            inputPoints = random->pointList;
        }

        facet->inputPoints = inputPoints;
        facet->Run(src);
        dst2 = facet->dst2;

        if (task->heartBeat) generationMap.setTo(0);
        vector<int> usedG;
        int g;
        for (const auto& pt : inputPoints) {
            int index = facet->facet32s.at<int>(pt.y, pt.x);
            if (index >= facet->facetlist.size()) continue;
            vector<Point> nextFacet = facet->facetlist[index];

            // Ensure unique generation numbers
            if (task->FirstPass) {
                g = (int)usedG.size();
            }
            else {
                g = generationMap.at<int>(pt.y, pt.x) + 1;
                while (find(usedG.begin(), usedG.end(), g) != usedG.end()) {
                    g++;
                }
            }
            fillConvexPoly(generationMap, nextFacet, g, task->lineType);
            usedG.push_back(g);
            task->SetTrueText(to_string(g), dst2, pt);
        }
        dst3 = generationMap.clone();
    }
};







class Grid_Basics_CC : public CPP_Parent {
public:
    Grid_Basics_CC() : CPP_Parent() {
        desc = "Create a grid of squares covering the entire image.";
    }

    void Run(Mat src) {
        if (task->mouseClickFlag && !task->FirstPass) {
            task->gridROIclicked = task->gridToRoiIndex.at<int>(task->ClickPoint.y, task->ClickPoint.x);
        }
        if (task->optionsChanged) {
            task->gridMask = Mat::zeros(src.size(), CV_8U);
            task->gridToRoiIndex = Mat::zeros(src.size(), CV_32S);

            task->gridRects.clear();
            task->gridRows = 0;
            task->gridCols = 0;
            for (int y = 0; y < src.rows; y += task->gridSize) {
                for (int x = 0; x < src.cols; x += task->gridSize) {
                    Rect roi(x, y, task->gridSize, task->gridSize);
                    if (x + roi.width >= src.cols) roi.width = src.cols - x;
                    if (y + roi.height >= src.rows) roi.height = src.rows - y;
                    if (roi.width > 0 && roi.height > 0) {
                        if (x == 0) task->gridRows++;
                        if (y == 0) task->gridCols++;
                        task->gridRects.push_back(roi);
                    }
                }
            }
            task->gridMask = Mat::zeros(src.size(), CV_8U);
            for (int x = task->gridSize; x < src.cols; x += task->gridSize) {
                Point p1(x, 0), p2(x, src.rows);
                line(task->gridMask, p1, p2, Scalar(255), task->lineWidth);
            }
            for (int y = task->gridSize; y < src.rows; y += task->gridSize) {
                Point p1(0, y), p2(src.cols, y);
                line(task->gridMask, p1, p2, Scalar(255), task->lineWidth);
            }

            for (int i = 0; i < task->gridRects.size(); i++) {
                Rect roi = task->gridRects[i];
                rectangle(task->gridToRoiIndex, roi, Scalar(255, 255, 255), 1);
            }

            task->gridNeighbors.clear();
            for (const Rect& roi : task->gridRects) {
                vector<int> neighbors;
                task->gridNeighbors.push_back(neighbors);
                for (int i = 0; i < 8; i++) {
                    int x = (i % 3 == 1) ? roi.x + roi.width : (i < 3) ? roi.x - 1 : roi.x + roi.width + 1;
                    int y = (i < 3) ? roi.y - 1 : (i < 6) ? roi.y + roi.height + 1 : roi.y;
                    if (x >= 0 && x < src.cols && y >= 0 && y < src.rows) {
                        task->gridNeighbors.back().push_back(task->gridToRoiIndex.at<int>(y, x));
                    }
                }
            }
        }
        if (standalone) {
            dst2 = Mat::zeros(src.size(), CV_8U);
            task->color.copyTo(dst2);
            dst2.setTo(Scalar(255, 255, 255), task->gridMask);
            stringstream ss;
            ss << "Grid_Basics_CC " << task->gridRects.size() << " (" << task->gridRows << "X" << task->gridCols << ") "
                << task->gridSize << "X" << task->gridSize << " regions";
            labels[2] = ss.str();
        }
    }
};







class KNN_Basics_CC : public CPP_Parent {
public:
    Ptr<ml::KNearest> knn = ml::KNearest::create();
    vector<Point2f> trainInput;
    vector<Point2f> queries;
    vector<vector<int>> neighbors;
    Mat result;
    int desiredMatches = -1;
    Random_Basics_CC* random;
    vector<int> neighborIndexToTrain;

    KNN_Basics_CC() : CPP_Parent()
    {
        random = new Random_Basics_CC();
        labels[2] = "Red=TrainingData, yellow = queries";
        desc = "Train a KNN model and map each query to the nearest training neighbor.";
    }
    void displayResults() {
        dst2.setTo(0);  // Assuming dst2 is a Mat object
        int dm = min(trainInput.size(), queries.size());

        for (int i = 0; i < queries.size(); i++) {
            Point2f pt = queries[i];
            int test = result.at<int>(i, 0);
            if (test >= trainInput.size() || test < 0) continue;
            Point2f nn = trainInput[result.at<int>(i, 0)];
            circle(dst2, pt, task->DotSize + 4, Scalar(0, 255, 255), -1, task->lineType);  // Yellow
            line(dst2, pt, nn, Scalar(0, 255, 255), task->lineWidth, task->lineType);
        }

        for (Point2f pt : trainInput) {
            circle(dst2, pt, task->DotSize + 4, Scalar(0, 0, 255), -1, task->lineType);  // Red
        }
    }
    void generateRandom(Mat src)
    {
        random->Run(src);
        queries = random->pointList;
        if (task->heartBeat)
            trainInput = queries;
    }
    void Run(Mat src)
    {
        int KNNdimension = 2;

        if (standalone) {
            if (task->heartBeat) {
                random->Run(empty);
                trainInput = random->pointList;
            }
            random->Run(empty);
            queries = random->pointList;
        }

        Mat queryMat((int)queries.size(), KNNdimension, CV_32F, queries.data());
        if (queryMat.rows == 0) {
            task->SetTrueText("There were no queries provided. There is nothing to do...", dst2);
            return;
        }

        if (trainInput.empty()) {
            trainInput = queries;  // First pass, just match the queries.
        }
        Mat trainData((int)trainInput.size(), KNNdimension, CV_32F, trainInput.data());
        vector<int> array(trainData.rows);
        iota(array.begin(), array.end(), 0);
        Mat response(trainData.rows, 1, CV_32S, array.data());  // Create response matrix
        knn->train(trainData, ml::ROW_SAMPLE, response);

        int dm = desiredMatches < 0 ? (int)trainInput.size() : desiredMatches;
        Mat neighborMat;
        knn->findNearest(queryMat, dm, noArray(), neighborMat);

        if (neighborMat.rows != queryMat.rows || neighborMat.cols != dm) {
            cerr << "KNN's FindNearest did not return the correct number of neighbors.\n";
            return;
        }

        vector<float> nData(queryMat.rows * dm);
        if (nData.empty()) return;
        memcpy(nData.data(), neighborMat.data, nData.size() * sizeof(float));

        for (int i = 0; i < nData.size(); i++) {
            if (abs(nData[i]) > trainInput.size()) nData[i] = 0;  // Clamp within trainInput range
        }

        result.create(queryMat.rows, dm - 1, CV_32S);
        neighbors.clear();
        for (int i = 0; i < queryMat.rows; i++) {
            Point2f pt = queries[i];
            vector<int> res;
            for (int j = 0; j < dm - 1; j++) {
                int test = static_cast<int>(nData[i * dm + j]);
                if (test >= 0 && test < nData.size()) {
                    result.at<int>(i, j) = test;
                    res.push_back(test);
                }
            }
            neighbors.push_back(res);
        }
        if (neighbors.size() > 1) displayResults();
    }
};





class KNN_NoDups_CC : public CPP_Parent {
private:
public:
    vector<lpData> matches;
    vector<Point> noMatch;
    KNN_Basics_CC* basics;
    vector<Point2f> queries;
    vector<int> neighbors;
    Random_Basics_CC* random;

    KNN_NoDups_CC() : CPP_Parent() {
        basics = new KNN_Basics_CC();
        random = new Random_Basics_CC();
        desc = "Map points 1:1 with losses. Toss any duplicates that are farther.";
    }
    void Run(Mat src)
    {
        if (standalone) {
            if (task->heartBeat) {
                random->Run(empty);
                basics->trainInput = random->pointList;
            }
            random->Run(empty);
            queries = random->pointList;
        }

        if (queries.empty()) {
            task->SetTrueText("Place some input points in queries before starting the knn run.", dst2);
            return;
        }

        basics->queries = queries;
        basics->Run(empty);
        if (queries.size() > 1) basics->displayResults();
        dst2 = basics->dst2;

        // Extract the first nearest neighbor for each query
        neighbors.clear();
        if (basics->neighbors.size() > 1)
            for (const auto& neighborRow : basics->neighbors) {
                neighbors.push_back(neighborRow[0]);
            }

        // Resolve duplicate matches based on distances
        for (int i = 0; i < neighbors.size(); i++) {
            if (neighbors[i] == -1) continue;
            Point2f p1 = queries[i];
            Point2f ptn = basics->trainInput[neighbors[i]];
            for (int j = i + 1; j < neighbors.size(); j++) {
                if (neighbors[j] == neighbors[i]) {
                    Point2f p2 = queries[j];
                    double d1 = norm(p1 - ptn);
                    double d2 = norm(p2 - ptn);
                    neighbors[d1 > d2 ? i : j] = -1;
                }
            }
        }

        // Display results
        dst3.setTo(0);
        for (const Point2f& pt : basics->trainInput) {
            circle(dst3, pt, task->DotSize + 4, Scalar(0, 0, 255), -1, task->lineType);
        }

        noMatch.clear();
        matches.clear();
        for (int i = 0; i < neighbors.size(); i++) {
            Point2f pt = queries[i];
            circle(dst3, pt, task->DotSize + 4, Scalar(0, 255, 255), -1, task->lineType);
            if (neighbors[i] == -1) {
                noMatch.push_back(pt);
            }
            else {
                Point2f nn = basics->trainInput[neighbors[i]];
                matches.emplace_back(pt, nn);
                line(dst3, nn, pt, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            }
        }

        if (!standalone) {
            basics->trainInput = queries;
        }
    }
};







class Delaunay_Generations_CC : public CPP_Parent {
private:
public:
    vector<Point2f> inputPoints;
    Delaunay_Basics_CC* facet;
    KNN_NoDups_CC* knn;
    Random_Basics_CC* random;
    Mat generationMap;

    Delaunay_Generations_CC() : CPP_Parent()
    {
        generationMap = Mat::zeros(dst3.size(), CV_32S);
        knn = new KNN_NoDups_CC();
        facet = new Delaunay_Basics_CC();
        random = new Random_Basics_CC();
        random->sizeRequest = 10;
        labels = { "", "Mask of unmatched regions - generation set to 0", "Facet Image with count for each region",
                    "Generation counts in CV_32SC1 format" };
        desc = "Create a region in an image for each point provided";
    }
    void Run(Mat src) {
        if (standalone) {
            if (task->heartBeat) random->Run(empty);
            inputPoints = random->pointList;
        }

        knn->queries = inputPoints;
        knn->Run(empty);

        facet->inputPoints = inputPoints;
        facet->Run(src);
        dst2 = facet->dst2;

        vector<int> usedG;
        int g;
        for (const lpData& mp : knn->matches) {
            int index = facet->facet32s.at<int>(mp.p2.y, mp.p2.x);
            if (index >= facet->facetlist.size()) continue;
            const vector<Point>& nextFacet = facet->facetlist[index];

            // Ensure unique generation numbers
            if (task->FirstPass) {
                g = (int)usedG.size();
            }
            else {
                g = generationMap.at<int>(mp.p2.y, mp.p2.x) + 1;
                while (find(usedG.begin(), usedG.end(), g) != usedG.end()) {
                    g++;
                }
            }

            fillConvexPoly(generationMap, nextFacet, g, task->lineType);
            usedG.push_back(g);
            task->SetTrueText(to_string(g), dst2, mp.p2);
        }
        dst3 = generationMap.clone();
    }
};




class Feature_Stable_CC : public CPP_Parent {
private:
    cv::Ptr<cv::BRISK> Brisk;
public:
    vector<cv::Point2f> featurePoints;
    Options_Features* options = new Options_Features;

    Feature_Stable_CC() : CPP_Parent() {
        Brisk = cv::BRISK::create();
        desc = "Find good features to track in a BGR image.";
    }

    void Run(cv::Mat src) {
        dst2 = src.clone();
        if (src.channels() == 3) cvtColor(src, src, COLOR_BGR2GRAY);
        featurePoints.clear();
        int sampleSize = options->featurePoints;
        if (options->useBRISK) {
            vector<cv::KeyPoint> keyPoints;
            Brisk->detect(src, keyPoints);
            for (const auto& kp : keyPoints) {
                if (kp.size >= options->minDistance) featurePoints.push_back(kp.pt);
            }
        }
        else {
            cv::goodFeaturesToTrack(src, featurePoints, sampleSize, options->quality, options->minDistance);
        }
        cv::Scalar color = (dst2.channels() == 3) ? cv::Scalar(0, 255, 255) : cv::Scalar(255, 255, 255);
        for (const auto& pt : featurePoints) {
            cv::circle(dst2, pt, task->DotSize, color, -1, task->lineType);
        }
        labels[2] = "Found " + to_string(featurePoints.size()) + " points with quality = " + to_string(options->quality) +
            " and minimum distance = " + to_string(options->minDistance);
    }
};





class Stable_Basics_CC : public CPP_Parent {
public:
    Delaunay_Generations_CC* facetGen;
    vector<Point2f> ptList;
    Point2f anchorPoint;
    Feature_Stable_CC* good;

    Stable_Basics_CC() : CPP_Parent() {
        good = new Feature_Stable_CC();
        facetGen = new Delaunay_Generations_CC();
        desc = "Maintain the generation counts around the feature points.";
    }

    void Run(Mat src) {
        if (standalone) {
            good->Run(src);
            facetGen->inputPoints = good->featurePoints;
        }

        facetGen->Run(src);
        if (facetGen->inputPoints.empty()) return; // nothing to work on

        ptList.clear();
        vector<int> generations;
        for (const Point2f& pt : facetGen->inputPoints) {
            int fIndex = facetGen->facet->facet32s.at<int>(pt.y, pt.x);
            if (fIndex >= facetGen->facet->facetlist.size()) continue; // new point
            int g = facetGen->dst3.at<int>(pt.y, pt.x);
            generations.push_back(g);
            ptList.push_back(pt);
            task->SetTrueText(to_string(g), dst2, pt);
        }

        int maxGens = *max_element(generations.begin(), generations.end());
        int index = distance(generations.begin(), find(generations.begin(), generations.end(), maxGens));
        anchorPoint = ptList[index];
        if (index < facetGen->facet->facetlist.size()) {
            const vector<Point>& bestFacet = facetGen->facet->facetlist[index];
            fillConvexPoly(dst2, bestFacet, Scalar(0, 0, 0), task->lineType);
            drawContours(dst2, vector<vector<Point>> {bestFacet}, -1, Scalar(0, 255, 255), task->lineWidth);
        }

        dst2 = facetGen->dst2;
        dst3 = src.clone();
        for (const Point2f& pt : ptList) {
            circle(dst2, pt, task->DotSize, Scalar(0, 255, 255), task->lineWidth, task->lineType);
            circle(dst3, pt, task->DotSize, Scalar(255, 255, 255), task->lineWidth, task->lineType);
        }

        string text = to_string(ptList.size()) + " stable points were identified with " + to_string(maxGens) + " generations at the anchor point";
        labels[2] = text;
    }
};





class Stable_BasicsCount_CC : public CPP_Parent
{
private:
public:
    Stable_Basics_CC* basics;
    Feature_Stable_CC* good;
    map<int, int, greater<float>> goodCounts;
    Stable_BasicsCount_CC() : CPP_Parent()
    {
        good = new Feature_Stable_CC();
        basics = new Stable_Basics_CC();
        desc = "Track the stable good features found in the BGR image.";
    }
    void Run(Mat src)
    {
        good->Run(src);
        basics->facetGen->inputPoints = good->featurePoints;
        basics->Run(src);
        dst2 = basics->dst2;
        dst3 = basics->dst3;

        goodCounts.clear();
        int g;
        for (auto i = 0; i < basics->ptList.size(); i++)
        {
            Point2f pt = basics->ptList[i];
            circle(dst2, pt, task->DotSize, YELLOW, task->lineWidth, task->lineType);
            g = basics->facetGen->dst3.at<int>(pt.y, pt.x);
            goodCounts[g] = i;
            task->SetTrueText(to_string(g), dst2, pt);
        }
    }
};


class FPoly_TopFeatures_CC : public CPP_Parent {
public:
    Stable_BasicsCount_CC* stable;
    vector<Point2f> poly;

    FPoly_TopFeatures_CC() : CPP_Parent() {
        stable = new Stable_BasicsCount_CC();
        desc = "Get the top features and validate them";
    }

    void Run(Mat src) {
        stable->Run(src);
        dst2 = stable->dst2;
        poly.clear();
        for (const auto& keyVal : stable->goodCounts) {
            const Point2f& pt = stable->basics->ptList[keyVal.second];
            int g = stable->basics->facetGen->dst3.at<int>(pt.y, pt.x);

            if (poly.size() < task->polyCount) poly.push_back(pt);
        }

        if (poly.size() > 1)
            for (int i = 0; i < poly.size() - 1; i++) {
                line(dst2, poly[i], poly[i + 1], Scalar(255, 255, 255), task->lineWidth, task->lineType);
            }
    }
};






class Edge_Scharr_CC : public CPP_Parent {
public:
    Edge_Scharr_CC() : CPP_Parent() {
        desc = "Scharr is most accurate with 3x3 kernel.";
    }

    void Run(Mat src) {
        float optionScharrMultiplier = 50;
        Mat gray;
        cvtColor(src, gray, COLOR_BGR2GRAY);

        Mat xField, yField;
        Scharr(gray, xField, CV_32FC1, 1, 0);
        Scharr(gray, yField, CV_32FC1, 0, 1);

        add(xField, yField, dst0);
        dst0.convertTo(dst2, CV_8U, optionScharrMultiplier / 100.0);
    }
};



class Depth_Colorizer_CC : public CPP_Parent {
public:
    Depth_Colorizer_CC() : CPP_Parent() {
        desc = "Colorize the depth based on the near and far colors.";
    }

    void Run(Mat src) {
        if (src.type() != CV_32F) {
            src = task->pcSplit[2];
        }

        dst2.setTo(0);
        Scalar nearColor = Scalar(0, 1.0f, 1.0f);
        Scalar farColor = Scalar(1.0f, 0, 0);
        for (int y = 0; y < src.rows; y++) {
            for (int x = 0; x < src.cols; x++) {
                float pixel = src.at<float>(y, x);
                if (pixel != 0)
                    int k = 0;
                if (pixel > 0 && pixel <= task->MaxZmeters) {
                    float t = pixel / task->MaxZmeters;
                    Scalar colorS = 255 * ((1 - t) * nearColor + t * farColor);
                    Vec3b color = Vec3b(colorS[0], colorS[1], colorS[2]);
                    dst2.at<Vec3b>(y, x) = color;
                }
            }
        }
    }
};



class RedCloud_Basics_CC : public CPP_Parent {
public:
    int classCount;
    int givenClassCount = 0;
    int reduceAmt = 250;
    RedCloud_Basics_CC() : CPP_Parent() {
        //if (standalone) {
        //    task.gOptions.RedCloud_Basics.Checked = true;
        //}
        desc = "Reduction transform for the point cloud";
    }
    void Run(Mat src) {
        task->pointCloud.convertTo(dst0, CV_32S, 1000.0 / reduceAmt);
        vector<Mat> msplit;
        split(dst0, msplit);
        switch (task->PCReduction) {
        case 0: // "X Reduction":
            dst0 = msplit[0] * reduceAmt;
            break;
        case 1: // "Y Reduction":
            dst0 = msplit[1] * reduceAmt;
            break;
        case 2: // "Z Reduction":
            dst0 = msplit[2] * reduceAmt;
            break;
        case 3: // "XY Reduction":
            dst0 = msplit[0] * reduceAmt + msplit[1] * reduceAmt;
            break;
        case 4: // "XZ Reduction":
            dst0 = msplit[0] * reduceAmt + msplit[2] * reduceAmt;
            break;
        case 5: // "YZ Reduction":
            dst0 = msplit[1] * reduceAmt + msplit[2] * reduceAmt;
            break;
        case 6: // "XYZ Reduction":
            dst0 = msplit[0] * reduceAmt + msplit[1] * reduceAmt + msplit[2] * reduceAmt;
            break;
        }
        double minVal, maxVal;
        minMaxLoc(dst0, &minVal, &maxVal);
        dst2 = dst0 - minVal;
        dst2.setTo(maxVal - minVal, task->maxDepthMask);

        minMaxLoc(dst2, &minVal, &maxVal);
        classCount = 255 - givenClassCount - 1;
        dst2 *= (double)classCount / maxVal;
        dst2 += givenClassCount + 1;
        dst2.convertTo(dst2, CV_8U);
        labels[2] = "Reduced Pointcloud - reduction factor = " + to_string(reduceAmt) + " produced " + to_string(classCount) + " regions";
    }
};







class Depth_PointCloud_CC : public CPP_Parent {
private:
public:
    Depth_PointCloud_CC() : CPP_Parent()
    {
        desc = "Display the contents of the point cloud as a 2D image";
    }
    void Run(Mat src)
    {
        if (src.type() != CV_32FC3) src = task->pointCloud.clone();
        Mat mSplit[3];
        split(src, mSplit);
        convertScaleAbs(mSplit[0], mSplit[0], 255);
        convertScaleAbs(mSplit[1], mSplit[1], 255);
        convertScaleAbs(mSplit[2], mSplit[2], 255);
        vector<Mat> channels = { mSplit[0], mSplit[1], mSplit[2] };
        merge(channels, dst2);
        dst2.convertTo(dst2, CV_8UC3);
        dst3 = task->depth32f > 0;
    }
};






class IMU_GMatrix_QT_CC : public CPP_Parent {
private:
public:
    float cx = 1.0f, sx = 0.0f, cy = 1.0f, sy = 0.0f, cz = 1.0f, sz = 0.0f;
    bool usingSliders = false;
    IMU_GMatrix_QT_CC() : CPP_Parent() {
        desc = "Find the angle of tilt for the camera with respect to gravity without any options_";
    }
    void Run(Mat src) {
        if (usingSliders == false) {
            cz = cos(task->accRadians.z);
            sz = sin(task->accRadians.z);
            cx = cos(task->accRadians.x);
            sx = sin(task->accRadians.x);
        }
        float gMt[3][3] = { {cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                            {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                            {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz} };

        float gMatrix[3][3] = { {gMt[0][0] * cy + gMt[0][1] * 0 + gMt[0][2] * sy,
                                gMt[0][0] * 0 + gMt[0][1] * 1 + gMt[0][2] * 0,
                                gMt[0][0] * -sy + gMt[0][1] * 0 + gMt[0][2] * cy},
                                {gMt[1][0] * cy + gMt[1][1] * 0 + gMt[1][2] * sy,
                                gMt[1][0] * 0 + gMt[1][1] * 1 + gMt[1][2] * 0,
                                gMt[1][0] * -sy + gMt[1][1] * 0 + gMt[1][2] * cy},
                                {gMt[2][0] * cy + gMt[2][1] * 0 + gMt[2][2] * sy,
                                gMt[2][0] * 0 + gMt[2][1] * 1 + gMt[2][2] * 0,
                                gMt[2][0] * -sy + gMt[2][1] * 0 + gMt[2][2] * cy} };
        task->gMatrix = Mat(3, 3, CV_32F, gMatrix).clone();
        task->SetTrueText("task->gMatrix is set...", dst2);
    }
};







class IMU_GMatrix_CC : public CPP_Parent
{
private:
public:
    string strOut;
    IMU_GMatrix_QT_CC* qt;
    IMU_GMatrix_CC() : CPP_Parent() {
        qt = new IMU_GMatrix_QT_CC();
        qt->usingSliders = true;
        desc = "Find the angle of tilt for the camera with respect to gravity.";
    }
    void Run(Mat src) {
        qt->cz = cos(task->accRadians.z);
        qt->sz = sin(task->accRadians.z);
        qt->cx = cos(task->accRadians.x);
        qt->sx = sin(task->accRadians.x);
        //qt->cy = cos(rotateY * PI / 180);
        //qt->sy = sin(rotateY * PI / 180);
        qt->Run(src);
        task->SetTrueText("task->gMatrix is set...", dst2);
    }
};







class Depth_PointCloud_IMU_CC : public CPP_Parent
{
private:
public:
    float option_resizeFactor = 1;
    IMU_GMatrix_QT_CC* gMatrix;
    Depth_PointCloud_CC* cloud;
    Depth_PointCloud_IMU_CC() : CPP_Parent() {
        gMatrix = new IMU_GMatrix_QT_CC();
        cloud = new Depth_PointCloud_CC();
        desc = "Rotate the PointCloud around the X-axis and the Z-axis using the gravity vector from the IMU.";
    }
    void Run(Mat src) {
        if (src.type() != CV_32FC3) src = task->pointCloud.clone();
        gMatrix->Run(src);
        Mat gOutput = src.reshape(1, src.rows * src.cols);
        gOutput *= task->gMatrix;
        dst0 = gOutput.reshape(3, src.rows);
        task->gCloud = dst0;
        if (option_resizeFactor != 1) {
            resize(dst0, dst1, Size(int((dst2.cols * option_resizeFactor)), int((dst2.rows * option_resizeFactor))));
        }
        task->SetTrueText("gCloud is ready for use", dst3);

        cloud->Run(task->gCloud);
        dst2 = cloud->dst2;
    }
};




class Edge_Sobel_CC : public CPP_Parent {
public:
    bool horizontalDerivative = true;
    bool verticalDerivative = true;
    int kernelSize = 3;
    AddWeighted_Basics_CC* addw;
    Edge_Sobel_CC() : CPP_Parent() {
        labels = { "", "", "Horizontal derivative", "Vertical derivative" };
        desc = "Show Sobel edge detection with varying kernel sizes and directions.";
    }

    void Run(Mat src) {
        if (src.channels() == 3) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }

        if (horizontalDerivative) {
            Sobel(src, dst2, CV_32F, 1, 0, kernelSize);
            convertScaleAbs(dst2, dst2);
        }

        if (verticalDerivative) {
            Sobel(src, dst3, CV_32F, 0, 1, kernelSize);
            convertScaleAbs(dst3, dst3);
        }
    }
};




class Binarize_Simple_CC : public CPP_Parent {
public:
    Scalar meanScalar;
    int injectVal = 255;

    Binarize_Simple_CC() : CPP_Parent() {
        desc = "Binarize an image using Threshold with OTSU.";
    }

    void Run(Mat src) {
        if (src.channels() == 3) cvtColor(src, src, COLOR_BGR2GRAY);
        meanScalar = mean(src);
        threshold(src, dst2, meanScalar[0], injectVal, THRESH_BINARY);
    }
};





class Plot_Histogram_CC : public CPP_Parent {
public:
    Mat histogram;
    float minRange = 0;
    float maxRange = 255;
    Scalar backColor = Scalar(0, 0, 255);
    float maxValue;
    float minValue;
    float plotCenter;
    float barWidth;
    bool addLabels = true;
    bool removeZeroEntry = true;
    bool createHistogram = false;

    Plot_Histogram_CC() : CPP_Parent() {
        desc = "Plot histogram data with a stable scale at the left of the image.";
    }

    void Run(Mat src) {
        if (standalone || createHistogram) {
            if (src.channels() != 1) {
                cvtColor(src, src, COLOR_BGR2GRAY);
            }
            int bins[] = { task->histogramBins };
            float hRange[] = { minRange, maxRange };
            const float* range[] = { hRange };
            calcHist(&src, 1, { 0 }, Mat(), histogram, 1, bins, range, true, false);
        }
        else {
            histogram = src;
        }

        if (removeZeroEntry) {
            histogram.at<float>(0) = 0;
        }

        dst2 = backColor;
        barWidth = dst2.cols / histogram.rows;
        plotCenter = barWidth * histogram.rows / 2 + barWidth / 2;

        vector<float> histArray(histogram.rows);
        memcpy(histArray.data(), histogram.ptr<float>(), histArray.size() * sizeof(float));

        double minVal, maxVal;
        minMaxIdx(histogram, &minVal, &maxVal);

        if (maxVal > 0 && histogram.rows > 0) {
            int incr = 255 / histogram.rows;
            for (int i = 0; i < histArray.size(); i++) {
                if (isnan(histArray[i])) {
                    histArray[i] = 0;
                }
                if (histArray[i] > 0) {
                    int h = cvRound(histArray[i] * dst2.rows / maxVal);
                    int sIncr = (i % 256) * incr;
                    Scalar color(sIncr, sIncr, sIncr);
                    if (histogram.rows > 255) {
                        color = Scalar(0, 0, 0);
                    }
                    rectangle(dst2, Rect(i * barWidth, dst2.rows - h, fmax(1, barWidth), h), color, -1);
                }
            }
            if (addLabels) {
                task->AddPlotScale(dst2, minVal, maxVal);
            }
        }
    }
};





class Hist_Basics_CC : public CPP_Parent
{
public:
    Mat histogram;
    mmData mm;
    Plot_Histogram_CC* plot;
    vector<Range> ranges;
    int splitIndex = 0;
    Hist_Basics_CC() : CPP_Parent()
    {
        plot = new Plot_Histogram_CC();
        desc = "Create a histogram (no Kalman)";
    }

    void Run(Mat src) {
        if (standalone) {
            if (task->heartBeat) {
                splitIndex = (splitIndex + 1) % 3;
            }

            Mat msplit[3];
            split(src, msplit);
            src = msplit[splitIndex];

            plot->backColor = Scalar(splitIndex == 0 ? 255 : 0,
                splitIndex == 1 ? 255 : 0,
                splitIndex == 2 ? 255 : 0);
        }
        else {
            if (src.channels() != 1) {
                cvtColor(src, src, COLOR_BGR2GRAY);
            }
        }
        mm = task->vbMinMax(src);

        float histDelta = 0.001f;
        int bins[] = { task->histogramBins };
        float hRange[] = { (float)(mm.minVal - histDelta), (float)(mm.maxVal + histDelta) };
        const float* range[] = { hRange };
        calcHist(&src, 1, { 0 }, Mat(), histogram, 1, bins, range, true, false);

        plot->Run(histogram);
        histogram = plot->histogram;


        dst2 = plot->dst2;
        labels[2] = " histogram, bins = " +
            to_string(task->histogramBins) + ", X ranges from " + to_string(int(mm.minVal)) +
            " to " + to_string(int(mm.maxVal)) + ", y is sample count";
        labels[2] = (splitIndex == 0 ? "Blue" : splitIndex == 1 ? "Green" : "Red") + labels[2];
    }
};


class Kalman_Simple {
public:
    KalmanFilter* kf;
    Mat processNoise = Mat(2, 1, CV_32F);
    Mat measurement = Mat(1, 1, CV_32F, 0.0);
    float inputReal;
    float stateResult;
    float processNoiseCov = 0.00001f;
    float measurementNoiseCov = 0.1f;
    float errorCovPost = 1;
    float transitionMatrix[4] = { 1, 1, 0, 1 };  // Change externally and set new TransmissionMatrix
    bool newTMatrix = true;

    void updateTMatrix()
    {
        kf->transitionMatrix = Mat(2, 2, CV_32F, transitionMatrix);
        kf->measurementMatrix = Mat::eye(1, 2, CV_32F);  // Set identity
        kf->processNoiseCov = Mat::eye(2, 2, CV_32F) * processNoiseCov;
        kf->measurementNoiseCov = Mat::eye(1, 1, CV_32F) * measurementNoiseCov;
        kf->errorCovPost = Mat::eye(2, 2, CV_32F) * errorCovPost;
    }
    Kalman_Simple() {
        kf = new KalmanFilter(2, 1, 0);
    }

    void Run() {
        if (newTMatrix)
        {
            newTMatrix = false;
            updateTMatrix();
        }

        Mat prediction = kf->predict();
        measurement.at<float>(0, 0) = inputReal;
        stateResult = kf->correct(measurement).at<float>(0, 0);
    }

    ~Kalman_Simple() {}
};



class Kalman_Basics_CC : public CPP_Parent {
public:
    vector<Kalman_Simple> kalman;
    vector<float> kInput = { 0, 0, 0, 0 };
    vector<float> kOutput;
    int saveDimension = -1;

    Kalman_Basics_CC() : CPP_Parent() {
        desc = "Use Kalman to stabilize values (such as a Rect)";
    }

    void Run(Mat src) {
        if (task->optionsChanged) {
            kalman.clear();
            saveDimension = int(kInput.size());
            for (int i = 0; i < kInput.size(); i++) {
                kalman.push_back(Kalman_Simple());
            }
            kOutput.resize(kInput.size());
        }

        if (task->UseKalman) {
            for (int i = 0; i < kalman.size(); i++) {
                kalman[i].inputReal = kInput[i];
                kalman[i].Run();
                if (isnan(kalman[i].stateResult)) {
                    kalman[i].stateResult = kalman[i].inputReal;
                }
                kOutput[i] = kalman[i].stateResult;
            }
        }
        else {
            kOutput = kInput;
        }

        if (standalone) {
            dst2 = src.clone();
            Rect rect(cvRound(kOutput[0]), cvRound(kOutput[1]), cvRound(kOutput[2]), cvRound(kOutput[3]));
            rect = task->validateRect(rect, dst2.cols, dst2.rows);
            static Rect lastRect = rect;
            if (rect == lastRect) {
                Rect r = task->InitRandomRect(src.rows <= 240 ? 20 : 50, dst2.cols);
                kInput = { float(r.x), float(r.y), float(r.width), float(r.height) };
            }
            lastRect = rect;
            rectangle(dst2, rect, Scalar(255, 255, 255), task->lineWidth + 1);
            rectangle(dst2, rect, Scalar(0, 0, 255), task->lineWidth);
        }
    }
};





class Hist_Kalman_CC : public CPP_Parent {
public:
    Hist_Basics_CC* hist;
    Kalman_Basics_CC* kalman;

    Hist_Kalman_CC() : CPP_Parent() {
        kalman = new Kalman_Basics_CC();
        hist = new Hist_Basics_CC();
        labels = { "", "", "With Kalman", "Without Kalman" };
        desc = "Use Kalman to smooth the histogram results.";
    }

    void Run(Mat src) {
        hist->Run(src);
        dst3 = hist->dst2.clone();

        if (hist->histogram.empty()) {
            hist->histogram = Mat(task->histogramBins, 1, CV_32F, 0);
        }

        if (kalman->kInput.size() != task->histogramBins) {
            kalman->kInput.resize(task->histogramBins);
        }
        for (int i = 0; i < task->histogramBins; i++) {
            kalman->kInput[i] = hist->histogram.at<float>(i, 0);
        }

        kalman->Run(src);

        hist->histogram = Mat(int(kalman->kOutput.size()), 1, CV_32FC1, kalman->kOutput.data());
        hist->plot->Run(hist->histogram);
        dst2 = hist->dst2;
    }
};





class BackProject_Basics_CC : public CPP_Parent {
public:
    Hist_Kalman_CC* histK;
    Scalar minRange, maxRange;

    BackProject_Basics_CC() : CPP_Parent() {
        histK = new Hist_Kalman_CC();
        labels[2] = "Move mouse to backproject a histogram column";
        desc = "Mouse over any bin to see the color histogram backprojected.";
    }

    void Run(Mat src) {
        Mat input = src.clone();
        if (input.channels() != 1) {
            cvtColor(input, input, COLOR_BGR2GRAY);
        }

        histK->Run(input);
        if (histK->hist->mm.minVal == histK->hist->mm.maxVal) {

            task->SetTrueText("The input image is empty - mm.minVal and mm.maxVal are both zero...", dst2);
            return;
        }

        dst2 = histK->dst2;

        int totalPixels = int(dst2.total());
        //if (histK->hist->plot.removeZeroEntry) {
        //    totalPixels = countNonZero(input);
        //}

        int brickWidth = dst2.cols / task->histogramBins;
        float incr = (histK->hist->mm.maxVal - histK->hist->mm.minVal) / task->histogramBins;
        int histIndex = floor(task->mouseMovePoint.x / brickWidth);
        if (histIndex >= task->histogramBins) histIndex = task->histogramBins - 1;

        minRange = Scalar(histIndex * incr);
        maxRange = Scalar((histIndex + 1) * incr);
        if (histIndex == task->histogramBins) {
            maxRange = Scalar(255);
        }

        inRange(input, minRange, maxRange, dst0);

        int actualCount = countNonZero(dst0);

        dst3 = task->color.clone();
        dst3.setTo(Scalar(0, 255, 255), dst0);

        auto count = histK->hist->histogram.at<float>(histIndex, 0);
        mmData histMax = task->vbMinMax(histK->hist->histogram);

        labels[3] = "Backprojecting " + to_string(minRange(0)) + " to " + to_string(maxRange(0)) +
            " with " + to_string(count) + " of " + to_string(totalPixels) + " samples compared to " +
            " mask pixels = " + to_string(actualCount) +
            " Histogram max count = " + to_string(histMax.maxVal);
        rectangle(dst2, Rect(histIndex * brickWidth, 0, brickWidth, dst2.rows), Scalar(0, 255, 255), task->lineWidth);
    }
};




class Rectangle_Basics_CC : public CPP_Parent {
public:
    vector<Rect> rectangles;
    vector<RotatedRect> rotatedRectangles;
    int options_DrawCount = 3;
    bool options_drawFilled = false;
    bool options_drawRotated = false;

    Rectangle_Basics_CC() : CPP_Parent() {
        desc = "Draw the requested number of rectangles.";
    }

    void Run(Mat src) {
        if (task->heartBeat) {
            dst2 = Mat::zeros(src.size(), CV_8UC3);
            rectangles.clear();
            rotatedRectangles.clear();

            for (int i = 0; i < options_DrawCount; i++) {
                Point2f nPoint = Point2f(rand() % src.cols, rand() % src.rows);
                auto width = rand() % int(src.cols - nPoint.x);
                auto height = rand() % int(src.rows - nPoint.y);
                auto eSize = Size2f(float(rand() % src.cols - nPoint.x - 1), float(rand() % src.rows - nPoint.y - 1));
                auto angle = 180.0F * float(rand() % 1000) / 1000.0f;
                auto nextColor = Scalar(task->vecColors[i].val[0], task->vecColors[i].val[1], task->vecColors[i].val[2]);
                auto rr = RotatedRect(nPoint, eSize, angle);
                Rect r = Rect(nPoint.x, nPoint.y, width, height);

                if (options_drawRotated) {
                    task->DrawRotatedRect(rr, dst2, nextColor);
                }
                else {
                    rectangle(dst2, r, nextColor, options_drawFilled ? -1 : 1);
                }

                rotatedRectangles.push_back(rr);
                rectangles.push_back(r);
            }
        }
    }
};




class Rectangle_Rotated_CC : public CPP_Parent {
public:
    Rectangle_Basics_CC* rectangle;

    Rectangle_Rotated_CC() : CPP_Parent() {
        rectangle = new Rectangle_Basics_CC();
        rectangle->options_drawRotated = true;
        desc = "Draw the requested number of rectangles.";
    }

    void Run(Mat src) {
        rectangle->Run(src);
        dst2 = rectangle->dst2;
    }
};




class Contour_Largest_CC : public CPP_Parent {
public:
    vector<Point> bestContour;
    vector<vector<Point>> allContours;
    RetrievalModes options_retrievalMode = RetrievalModes::RETR_LIST;
    ContourApproximationModes options_approximationMode = ContourApproximationModes::CHAIN_APPROX_NONE;
    int maxIndex;
    Rectangle_Rotated_CC* rotatedRect;

    Contour_Largest_CC() : CPP_Parent() {
        rotatedRect = new Rectangle_Rotated_CC();
        labels = { "", "", "Input to FindContours", "Largest single contour in the input image." };
        desc = "Create a mask from the largest contour of the input.";
    }

    void Run(Mat src) {
        if (standalone) {
            if (task->heartBeat) {
                rotatedRect->Run(src);
                dst2 = rotatedRect->dst2;
            }
        }
        else {
            dst2 = src;
        }

        if (dst2.channels() != 1) cvtColor(dst2, dst2, COLOR_BGR2GRAY);
        if (options_retrievalMode == RETR_FLOODFILL) {
            dst2.convertTo(dst1, CV_32SC1);
            findContours(dst1, allContours, noArray(), options_retrievalMode, options_approximationMode);
            dst1.convertTo(dst3, CV_8UC1);
        }
        else {
            findContours(dst2, allContours, noArray(), options_retrievalMode, options_approximationMode);
        }

        int maxCount = 0;
        maxIndex = -1;
        if (allContours.empty()) return;
        for (int i = 0; i < allContours.size(); i++) {
            int len = int(allContours[i].size());
            if (len > maxCount) {
                maxCount = len;
                maxIndex = i;
            }
        }
        bestContour = allContours[maxIndex];
        if (standalone) {
            dst3 = Mat::zeros(dst2.size(), CV_8UC1);
            if (maxIndex >= 0 && maxCount >= 2) {
                drawContours(dst3, allContours, maxIndex, Scalar(255), -1, task->lineType);
            }
        }
    }
};




class Diff_Basics_CC : public CPP_Parent {
public:
    int changedPixels;
    Mat lastGray;

    Diff_Basics_CC() : CPP_Parent() {
        labels = { "", "", "Stable gray", "Unstable mask" };
        desc = "Capture an image and compare it to previous frame using absDiff and threshold";
    }

    void Run(Mat src) override {
        if (src.channels() == 3) cvtColor(src, src, COLOR_BGR2GRAY);
        if (task->FirstPass) lastGray = src.clone();
        if (task->optionsChanged || lastGray.size() != src.size()) {
            lastGray = src.clone();
            dst3 = src.clone();
        }

        absdiff(src, lastGray, dst0);
        threshold(dst0, dst3, task->pixelDiffThreshold, 255, THRESH_BINARY);
        changedPixels = countNonZero(dst3);
        if (changedPixels > 0) {
            threshold(dst0, dst3, task->pixelDiffThreshold, 255, THRESH_BINARY);
            dst2 = src.clone();
            dst2.setTo(0, dst3);
            lastGray = src.clone();
        }
    }
};




class ApproxPoly_FindandDraw_CC : public CPP_Parent {
public:
    Rectangle_Rotated_CC* rotatedRect;
    vector<vector<Point>> allContours;
    ApproxPoly_FindandDraw_CC() : CPP_Parent() {
        rotatedRect = new Rectangle_Rotated_CC();
        labels[2] = "FindandDraw input";
        labels[3] = "FindandDraw output - note the change in line width where ApproxPoly differs from DrawContours";
        desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours.";
    }
    void Run(Mat src) override {
        rotatedRect->Run(src);
        dst2 = rotatedRect->dst2;
        cvtColor(dst2, dst0, COLOR_BGR2GRAY);
        threshold(dst0, dst0, 1, 255, THRESH_BINARY);
        dst0.convertTo(dst1, CV_32SC1);
        findContours(dst1, allContours, noArray(), RETR_FLOODFILL, CHAIN_APPROX_SIMPLE);
        dst3 = Mat::zeros(dst2.size(), CV_8UC1);
        vector<vector<Point>> contours;
        vector<Point> nextContour;
        for (int i = 0; i < allContours.size(); i++) {
            approxPolyDP(allContours[i], nextContour, 3, true);
            if (nextContour.size() > 2) contours.push_back(nextContour);
        }
        drawContours(dst3, contours, -1, Scalar(0, 255, 255), task->lineWidth, task->lineType);
    }
};




class ApproxPoly_Basics_CC : public CPP_Parent {
public:
    Contour_Largest_CC* contour;
    Rectangle_Rotated_CC* rotatedRect;
    ApproxPoly_Basics_CC() : CPP_Parent() {
        contour = new Contour_Largest_CC();
        rotatedRect = new Rectangle_Rotated_CC();
        labels = { "", "", "Input to the ApproxPolyDP", "Output of ApproxPolyDP" };
        desc = "Using the input contours, create ApproxPoly output";
    }
    void Run(Mat src) override {
        double epsilon = 3; // getSliderValue("epsilon - max distance from original curve");
        bool closedPoly = true; // getCheckBoxState("Closed polygon - connect first and last vertices.");
        if (standalone) {
            if (task->heartBeat) rotatedRect->Run(src);
            dst2 = rotatedRect->dst2;
        }
        contour->Run(dst2);
        dst2 = contour->dst2;
        if (contour->allContours.size()) {
            vector<Point> nextContour;
            approxPolyDP(contour->bestContour, nextContour, epsilon, closedPoly);
            dst3 = Mat::zeros(dst2.size(), CV_8UC1);
            task->drawContour(dst3, nextContour, Scalar(0, 255, 255));
        }
        else {
            // task->SetTrueText("No contours found", dst2);
        }
    }
};




class Hull_Basics_CC : public CPP_Parent {
public:
    Random_Basics_CC* random;
    vector<Point2f> inputPoints;
    vector<Point> hull;
    bool useRandomPoints;
    Hull_Basics_CC() : CPP_Parent() {
        random = new Random_Basics_CC();
        labels = { "", "", "Input Points - draw a rectangle anywhere. Enclosing rectangle in yellow.", "" };
        if (standalone) random->range = Rect(100, 100, 50, 50);
        desc = "Given a list of points, create a hull that encloses them.";
    }
    void Run(Mat src) override {
        if ((standalone && task->heartBeat) || (useRandomPoints && task->heartBeat)) {
            random->Run(empty);
            dst2 = random->dst2;
            inputPoints = random->pointList;
        }
        Mat hull2f;
        convexHull(inputPoints, hull2f);
        hull = task->convert2f2i(hull2f);
        dst2.setTo(0);
        task->drawContour(dst2, hull, Scalar(0, 255, 255));
    }
};





class ApproxPoly_Hull_CC : public CPP_Parent {
public:
    Hull_Basics_CC* hull;
    ApproxPoly_Basics_CC* aPoly;
    ApproxPoly_Hull_CC() : CPP_Parent() {
        hull = new Hull_Basics_CC();
        aPoly = new ApproxPoly_Basics_CC();
        hull->useRandomPoints = true;
        labels = { "", "", "Original Hull", "Hull after ApproxPoly" };
        desc = "Use ApproxPolyDP on a hull to show impact of options (which appears to be minimal - what is wrong?)";
    }
    void Run(Mat src) override {
        hull->Run(src);
        dst2 = hull->dst2;
        aPoly->Run(dst2);
        dst3 = aPoly->dst2;
    }
};





class RedColor_Flood_CC : public CPP_Parent
{
private:
public:
    Mat inputRemoved;
    RedCloud_Basics_CC* prepData;
    int option_loDiff = 0;
    int option_hiDiff = 0;
    int option_minSizeCell = 75;
    int options_historyMax = 10;
    bool options_highlightCell;
    int totalCount;
    vector<Rect>rects;
    vector<int> sizes;
    RedColor_Flood_CC() : CPP_Parent() {
        prepData = new RedCloud_Basics_CC();
        desc = "Perform the RedCloud low level FloodFill";
    }
    rcData buildZeroEntry() {
        rcData rc;
        Point pt;
        rc.contour = vector<Point>({ pt, pt, pt, pt });
        rc.hull = vector<Point>({ pt, pt, pt, pt });
        rc.rect = Rect(0, 0, 1, 1);
        rc.mask = Mat(1, 1, CV_8U);
        return rc;
    }
    void Run(Mat src) {
        if (src.type() != CV_32SC1) {
            if (task->gravityPointCloud)
                prepData->Run(task->gCloud);
            else
                prepData->Run(task->pointCloud);

            src = prepData->dst2;
        }

        if (inputRemoved.rows == 0) dst2 = task->noDepthMask; else dst2 = inputRemoved;
        rects.clear();
        sizes.clear();

        rects.push_back(Rect(0, 0, 1, 1));
        sizes.push_back(1);
        int cellCount = 1;

        src = src(Rect(1, 1, src.cols - 2, src.rows - 2)).clone();
        Rect rect;
        totalCount = 0;
        for (int y = 0; y < src.rows; y++)
        {
            for (int x = 0; x < src.cols; x++)
            {
                if (dst2.at<unsigned char>(y, x) == 0)
                {
                    int count = floodFill(src, dst2, Point(x, y), cellCount, &rect, option_loDiff, option_hiDiff,
                        4 | FLOODFILL_MASK_ONLY | FLOODFILL_FIXED_RANGE | (cellCount << 8));
                    if (option_minSizeCell < count)
                    {
                        rect.height += 1;
                        rects.push_back(rect);
                        sizes.push_back(count);
                        totalCount++;
                        if (++cellCount == 256) cellCount = 1;
                    }
                }
            }
        }
    }
};




enum FeatureSrc : uint8_t
{
    GoodFeaturesFull = 0,
    GoodFeaturesGrid = 1,
    Agast = 2,
    BRISK = 3,
    Harris = 4,
    FAST = 5
};





class History_Basics_CC : public CPP_Parent {
public:
    vector<Mat> saveFrames;
    History_Basics_CC() : CPP_Parent() {
        desc = "Create a frame history to sum the last X frames";
    }
    void Run(Mat src) override {
        if (task->frameHistoryCount == 1) {
            dst2 = src.clone();
            return;
        }
        if (src.type() != CV_32F) src.convertTo(src, CV_32F);
        if (dst1.type() != src.type() || dst1.channels() != src.channels() || task->optionsChanged) {
            dst1 = src.clone();
            saveFrames.clear();
        }
        if (saveFrames.size() >= task->frameHistoryCount) {
            saveFrames.erase(saveFrames.begin());
        }
        saveFrames.push_back(src.clone());
        for (const auto& m : saveFrames) {
            add(dst1, m, dst1);
        }
        dst1 *= 1.0 / saveFrames.size();
        if (src.channels() == 1) {
            dst1.convertTo(dst2, CV_8U);
        }
        else {
            dst1.convertTo(dst2, CV_8UC3);
        }
    }
};






// https://docs.opencv.org/4.x/da/d22/tutorial_py_canny.html
class Edge_Canny_CC : public CPP_Parent {
public:
    Options_Canny* options = new Options_Canny();
    Edge_Canny_CC() : CPP_Parent() {
        desc = "Show canny edge detection with varying thresholds";
    }

    void Run(Mat src) {
        options->Run();

        if (src.channels() == 3) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }

        if (src.depth() != CV_8U) {
            src.convertTo(src, CV_8U);
        }

        Canny(src, dst2, options->threshold1, options->threshold2, options->aperture, true);
    }
};







class Edge_MotionFrames_CC : public CPP_Parent {
public:
    Edge_Canny_CC* edges;
    History_Basics_CC* frames;
    Edge_MotionFrames_CC() : CPP_Parent() {
        edges = new Edge_Canny_CC();
        frames = new History_Basics_CC();
        labels = { "", "", "The multi-frame edges output", "The Edge_Canny output for the last frame only" };
        desc = "Collect edges over several frames controlled with global frame history";
    }
    void Run(Mat src) override {
        edges->Run(src);
        threshold(edges->dst2, dst3, 0, 255, THRESH_BINARY);
        frames->Run(edges->dst2);
        dst2 = frames->dst2;
    }
};





class Edge_Preserving_CC : public CPP_Parent {
public:
    int sigma_s = 10;
    double sigma_r = 40;
    Edge_Preserving_CC() : CPP_Parent() {
        task->drawRect = Rect(50, 50, 25, 25);
        labels = { "", "", "", "Edge preserving blur for BGR depth image above" };
        desc = "OpenCV's edge preserving filter.";
    }
    void Run(Mat src) override {
        dst2 = src;
        dst3 = task->depthRGB;
        edgePreservingFilter(dst2(task->drawRect), dst2(task->drawRect), sigma_s, sigma_r);
        edgePreservingFilter(task->depthRGB(task->drawRect), dst3(task->drawRect), sigma_s, sigma_r);
    }
};






class Resize_Preserve_CC : public CPP_Parent {
public:
    int options_resizePercent = 120;
    int options_topLeftOffset = 10;
    InterpolationFlags options_warpFlag = INTER_NEAREST;
    Size newSize;
    Resize_Preserve_CC() : CPP_Parent() {
        desc = "Decrease the size but preserve the full image size.";
    }
    void Run(Mat src) override {
        newSize = Size(ceil(src.cols * options_resizePercent / 100.0),
            ceil(src.rows * options_resizePercent / 100.0));
        Mat dst0;
        resize(src, dst0, newSize, 0, 0, INTER_NEAREST);
        dst0.setTo(0);
        Rect rect(options_topLeftOffset, options_topLeftOffset, src.cols, src.rows);
        src.copyTo(dst0(rect));
        resize(dst0, dst2, dst2.size(), 0, 0, options_warpFlag);
        labels[2] = "Image after resizing to: " + to_string(newSize.width) + "X" + to_string(newSize.height);
    }
};




class Convex_Basics_CC : public CPP_Parent {
public:
    vector<Point> hull;
    Convex_Basics_CC() : CPP_Parent() {
        desc = "Surround a set of random points with a convex hull";
        labels = { "", "", "Convex Hull - red dot is center and the black dots are the input points", "" };
    }
    vector<Point> buildRandomHullPoints() {
        int count = 10;
        int pad = 4;
        int w = dst2.cols - dst2.cols / pad;
        int h = dst2.rows - dst2.rows / pad;
        random_device rd;
        mt19937 gen(rd());
        uniform_int_distribution<> dist_w(dst2.cols / pad, w);
        uniform_int_distribution<> dist_h(dst2.rows / pad, h);
        vector<Point> hullList;
        for (int i = 0; i < count; i++) {
            hullList.emplace_back(dist_w(gen), dist_h(gen));
        }
        return hullList;
    }
    void Run(Mat src) override {
        vector<Point> hullList = task->rcSelect.contour;
        if (standalone) {
            if (!task->heartBeat) return;
            hullList = buildRandomHullPoints();
        }

        if (hullList.empty()) {
            task->SetTrueText("No points were provided. Update hullList before running.", dst2);
            return;
        }
        hull.clear();
        convexHull(hullList, hull);
        dst2.setTo(0);

        task->drawContour(dst2, hullList, Scalar(255, 255, 255), -1);
        for (int i = 0; i < hull.size(); i++) {
            line(dst2, hull[i], hull[(i + 1) % hull.size()], Scalar(255, 255, 255), task->lineWidth);
        }
    }
};




class Distance_Basics_CC : public CPP_Parent {
public:
    DistanceTypes options_distanceType = DIST_L1;
    int options_kernelSize = 0;
    Distance_Basics_CC() : CPP_Parent() {
        labels = { "", "", "Distance transform - create a mask with threshold", "" };
        desc = "Distance algorithm basics.";
    }
    void Run(Mat src) override {
        if (standalone) {
            src = task->depthRGB;
        }
        if (src.channels() == 3) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }
        distanceTransform(src, dst0, options_distanceType, options_kernelSize);
        normalize(dst0, dst0, 0, 255, NORM_MINMAX);
        dst0.convertTo(dst2, CV_8UC1);
    }
};




class Line_FastDetect_CC : public CPP_Parent {
public:
    Ptr<ximgproc::FastLineDetector> ld;
    map<float, int> sortLength;
    vector<lpData> lpList;
    vector<Point2f> ptList;
    Rect subsetRect;
    // vector<tCell> tCells;
    Line_FastDetect_CC() : CPP_Parent() {
        subsetRect = Rect(0, 0, dst2.cols, dst2.rows);
        dst3 = Mat::zeros(dst3.size(), CV_8U);
        ld = ximgproc::createFastLineDetector();
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present.";
    }
    void Run(Mat src) override {
        int options_lineLengthThreshold = dst2.rows / 10;
        if (src.channels() == 3) {
            cvtColor(src, dst2, COLOR_BGR2GRAY);
        }
        else {
            dst2 = src.clone();
        }
        if (dst2.type() != CV_8U) {
            dst2.convertTo(dst2, CV_8U);
        }
        vector<Vec4f> lines;
        ld->detect(dst2(subsetRect), lines);
        sortLength.clear();
        lpList.clear();
        ptList.clear();
        //  tCells.clear();
        for (const Vec4f& v : lines) {
            int x1 = v[0] + subsetRect.x, y1 = v[1] + subsetRect.y;
            int x2 = v[2] + subsetRect.x, y2 = v[3] + subsetRect.y;
            if (0 <= x1 && x1 < dst2.cols && 0 <= y1 && y1 < dst2.rows &&
                0 <= x2 && x2 < dst2.cols && 0 <= y2 && y2 < dst2.rows) {
                Point p1(x1, y1), p2(x2, y2);
                if (norm(p1 - p2) >= options_lineLengthThreshold) {
                    lpData mps(p1, p2);
                    lpList.push_back(mps);
                    ptList.push_back(p1);
                    ptList.push_back(p2);
                    sortLength[norm(p1 - p2)] = int(lpList.size()) - 1;
                    //tCells.push_back({ p1 });
                    //tCells.push_back({ p2 });
                }
            }
        }
        dst2 = src.clone();
        dst3.setTo(0);
        for (const auto& nextLine : sortLength) {
            const lpData& mps = lpList[nextLine.second];
            line(dst2, mps.p1, mps.p2, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            line(dst3, mps.p1, mps.p2, 255, task->lineWidth, task->lineType);
        }
        labels[2] = to_string(lpList.size()) + " lines were detected in the current frame";
    }
};






class Edge_Segments_CC : public CPP_Parent {
private:
public:
    Ptr<EdgeDrawing> ed;
    Edge_Segments_CC() : CPP_Parent()
    {
        labels[2] = "Lines found with the Edge Drawing Filter";
        ed = createEdgeDrawing();
        ed->params.EdgeDetectionOperator = EdgeDrawing::SOBEL;
        ed->params.GradientThresholdValue = 38;
        ed->params.AnchorThresholdValue = 8;
        desc = "Example using the OpenCV ximgproc extention for edge drawing.";
    }
    void Run(Mat src)
    {
        if (src.channels() == 3)
        {
            dst2 = src;
            cvtColor(src, dst0, COLOR_BGR2GRAY);
        }
        else
        {
            dst0 = src;
            cvtColor(src, dst2, COLOR_GRAY2BGR);
        }
        ed->detectEdges(dst0);

        vector<Vec4f> lines;
        ed->detectLines(lines);

        for (size_t i = 0; i < lines.size(); i++)
        {
            Point2f p1 = Point2f(lines[i].val[0], lines[i].val[1]);
            Point2f p2 = Point2f(lines[i].val[2], lines[i].val[3]);
            line(dst2, p1, p2, YELLOW, task->lineWidth, task->lineType);
        }
    }
};




class EdgeLine_Image_CC : public CPP_Parent {
public:
    EdgeDraw* cPtr;
    EdgeLine_Image_CC() : CPP_Parent() {
        cPtr = new EdgeDraw;
        labels = { "", "", "EdgeLine_Image output", "" };
        desc = "Access EdgeDraw directly for efficiency";
    }
    void Run(Mat src) {
        if (src.channels() != 1) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }
        Mat* srcPtr = &src;
        int* imagePtr = EdgeLine_RunCPP(cPtr, (int*)srcPtr->data, src.rows, src.cols, task->lineWidth);
        if (imagePtr != nullptr) {
            dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr);
            rectangle(dst2, Rect(0, 0, dst2.cols, dst2.rows), Scalar(255), task->lineWidth);
        }
    }
    ~EdgeLine_Image_CC() {
        EdgeLine_Edges_Close(cPtr);
        delete cPtr;
    }
};






class FeatureLess_Basics_CC : public CPP_Parent {
public:
    EdgeLine_Image_CC* edgeD;
    FeatureLess_Basics_CC() : CPP_Parent()
    {
        edgeD = new EdgeLine_Image_CC();
        desc = "Access the EdgeLine_Image algorithm directly rather than through the Basics interface - more efficient";
    }
    void Run(Mat src) {
        edgeD->Run(src);
        dst2 = edgeD->dst2;
        if (standalone) {
            dst3 = src.clone();
            dst3.setTo(Scalar(0, 255, 255), dst2);
        }
    }
};



class FeatureLess_Edge_CC : public CPP_Parent
{
private:
public:
    Distance_Basics_CC* dist;
    EdgeLine_Image_CC* eDraw;
    FeatureLess_Edge_CC() : CPP_Parent()
    {
        eDraw = new EdgeLine_Image_CC();
        dist = new Distance_Basics_CC();
        labels[2] = "Edges found using the Segments and the distance function";
        desc = "Floodfill the output of the Edge Drawing filter (C++)";
    }
    void Run(Mat src)
    {
        eDraw->Run(src);
        bitwise_not(eDraw->dst2, dst0);
        dist->Run(dst0);
        threshold(dist->dst2, dst2, 0, 255, THRESH_BINARY);
    }
};








class Bezier_Basics_CC : public CPP_Parent {
public:
    vector<Point> points;

    Bezier_Basics_CC() : CPP_Parent() {
        points = { Point(100, 100),
                    Point(150, 50),
                    Point(250, 150),
                    Point(300, 100),
                    Point(350, 150),
                    Point(450, 50) };
        desc = "Use n points to draw a Bezier curve.";
    }

    Point nextPoint(vector<Point> points, int i, float t) {
        int x = pow(1 - t, 3) * points[i].x +
            3 * t * pow(1 - t, 2) * points[i + 1].x +
            3 * pow(t, 2) * (1 - t) * points[i + 2].x +
            pow(t, 3) * points[i + 3].x;

        int y = pow(1 - t, 3) * points[i].y +
            3 * t * pow(1 - t, 2) * points[i + 1].y +
            3 * pow(t, 2) * (1 - t) * points[i + 2].y +
            pow(t, 3) * points[i + 3].y;
        return Point(x, y);
    }

    void Run(Mat src) {
        Point p1;
        for (int i = 0; i < points.size() - 4; i += 3) {
            for (int j = 0; j <= 100; j++) {
                Point p2 = nextPoint(points, i, j / 100.0f);
                if (j > 0) {
                    line(dst2, p1, p2, task->highlight, task->lineWidth);
                }
                p1 = p2;
            }
        }
        labels[2] = "Bezier output";
    }
};




class FeatureLess_History_CC : public CPP_Parent {
public:
    EdgeLine_Image_CC* edgeD;
    History_Basics_CC* frames;
    FeatureLess_History_CC() : CPP_Parent()
    {
        dst2 = Mat::zeros(dst2.size(), CV_8U);
        edgeD = new EdgeLine_Image_CC();
        frames = new History_Basics_CC();
        desc = "Access the EdgeLine_Image algorithm directly rather than through the Basics interface - more efficient";
        task->frameHistoryCount = 10;
    }
    void Run(Mat src) {
        edgeD->Run(src);
        frames->Run(edgeD->dst2);
        dst2 = frames->dst2;
        if (standalone) {
            dst3 = src.clone();
            dst3.setTo(Scalar(0, 255, 255), dst2);
        }
    }
};





class Palette_Basics_CC : public CPP_Parent {
public:
    bool whitebackground;
    Palette_Basics_CC() : CPP_Parent() {
        desc = "Apply the different color maps in OpenCV";
    }
    void Run(Mat src) {
        if (src.type() == CV_32F) {
            normalize(src, src, 0, 255, NORM_MINMAX);
            src.convertTo(src, CV_8U);
        }
        vector<ColormapTypes> colormapTypes = {
            COLORMAP_AUTUMN, COLORMAP_BONE, COLORMAP_CIVIDIS, COLORMAP_COOL,
            COLORMAP_HOT, COLORMAP_HSV, COLORMAP_INFERNO, COLORMAP_JET,
            COLORMAP_MAGMA, COLORMAP_OCEAN, COLORMAP_PARULA, COLORMAP_PINK,
            COLORMAP_PLASMA, COLORMAP_RAINBOW, COLORMAP_SPRING, COLORMAP_SUMMER,
            COLORMAP_TWILIGHT, COLORMAP_TWILIGHT_SHIFTED, COLORMAP_VIRIDIS, COLORMAP_WINTER
        };
        ColormapTypes mapIndex = colormapTypes[task->paletteIndex];
        applyColorMap(src, dst2, mapIndex);
    }
};


vector<Point> ContourBuild(const Mat& mask, int approxMode) {
    std::vector<std::vector<Point>> allContours;
    findContours(mask, allContours, RETR_EXTERNAL, approxMode);  // Adjusted retrieval mode

    int maxCount = 0, maxIndex = -1;
    for (int i = 0; i < allContours.size(); i++) {
        int len = int(allContours[i].size());
        if (len > maxCount) {
            maxCount = len;
            maxIndex = i;
        }
    }

    return (maxIndex >= 0) ? allContours[maxIndex] : std::vector<Point>();
}

Mat vbPalette(Mat input)
{
    static Palette_Basics_CC* palette = new Palette_Basics_CC();
    if (input.type() == CV_8U) palette->Run(input);
    return palette->dst2;
}




class RedColor_FeatureLessCore_CC : public CPP_Parent {
public:
    map<int, rcData, compareAllowIdenticalIntegerInverted> sortedCells;
    Mat inputRemoved;
    FeatureLess_Basics_CC* fLess;
    RedCloud* cPtr;
    RedColor_FeatureLessCore_CC() : CPP_Parent() {
        vbPalette(dst2);
        fLess = new FeatureLess_Basics_CC();
        cPtr = new RedCloud();
        inputRemoved = Mat(dst2.size(), CV_8U);
        inputRemoved.setTo(0);
        desc = "Another minimalist approach to building RedCloud color-based cells.";
    }
    ~RedColor_FeatureLessCore_CC() {

        if (cPtr) {
            RedColor_Close(cPtr);
            delete cPtr;
        }
    }
    void Run(Mat src) {
        if (src.channels() != 1) {
            fLess->Run(src);
            src = fLess->dst2;
        }
        void* imagePtr;
        Mat* srcPtr = &src;
        if (inputRemoved.empty()) {
            imagePtr = RedColor_Run(cPtr, (int*)srcPtr->data, 0, src.rows, src.cols);
        }
        else {
            Mat* maskPtr = &inputRemoved;
            imagePtr = RedColor_Run(cPtr, (int*)srcPtr->data, (uchar*)maskPtr->data, src.rows, src.cols);
        }
        int classCount = RedColor_Count(cPtr);

        dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr);
        dst3 = vbPalette(dst2 * 255 / classCount);

        if (task->heartBeat) {
            labels[3] = to_string(classCount) + " cells found";
        }

        if (classCount <= 1) {
            return;
        }
        Mat rectData(classCount, 1, CV_32SC4, RedColor_Rects(cPtr));
        Mat floodPointData(classCount, 1, CV_32SC2, RedColor_FloodPoints(cPtr));
        sortedCells.clear();

        for (int i = 0; i < classCount; i++) {
            rcData cell;
            cell.index = int(sortedCells.size()) + 1;
            cell.rect = task->validateRect(rectData.at<cv::Rect>(i, 0), dst2.cols, dst2.rows);
            inRange(dst2(cell.rect), cell.index, cell.index, cell.mask);
            //vector<Point> contour = ContourBuild(cell.mask, cv::CHAIN_APPROX_NONE); 
            //DrawContours(cell.mask, vector<vector<Point>> {contour}, 255, -1);

            cell.floodPoint = floodPointData.at<cv::Point>(i, 0);
            rectangle(cell.mask, cv::Rect(0, 0, cell.mask.cols, cell.mask.rows), 0, 1);
            Point pt = task->vbGetMaxDist(cell.mask);
            cell.maxDist = cv::Point(pt.x + cell.rect.x, pt.y + cell.rect.y);
            sortedCells.insert({ cell.pixels, cell });
        }

        if (task->heartBeat) {
            labels[2] = "CV_8U format - " + to_string(classCount) + " cells were identified.";
        }
    }
};









class RedColor_FeatureLess_CC : public CPP_Parent {
public:
    RedColor_FeatureLessCore_CC* minCore;
    vector<rcData> redCells;
    Mat lastColors;
    Mat lastMap = dst2.clone();
    RedColor_FeatureLess_CC() : CPP_Parent() {
        minCore = new RedColor_FeatureLessCore_CC();
        dst2 = Mat::zeros(dst2.size(), CV_8U);
        labels = { "", "Mask of active RedCloud cells", "CV_8U representation of redCells", "" };
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells.";
    }
    void Run(Mat src) {
        minCore->Run(src);
        vector<rcData> lastCells = redCells;
        if (task->FirstPass) lastColors = dst3.clone();
        redCells.clear();
        dst2.setTo(0);
        dst3.setTo(0);
        vector<Vec3b> usedColors = { Vec3b(0, 0, 0) };
        for (const auto ele : minCore->sortedCells) {
            rcData cell = ele.second;
            uchar index = lastMap.at<uchar>(cell.maxDist.y, cell.maxDist.x);
            if (index > 0 && index < lastCells.size()) {
                cell.color = lastColors.at<Vec3b>(cell.maxDist.y, cell.maxDist.x);
            }
            if (find(usedColors.begin(), usedColors.end(), cell.color) != usedColors.end()) {
                cell.color = task->randomCellColor();
            }
            usedColors.push_back(cell.color);
            if (dst2.at<uchar>(cell.maxDist.y, cell.maxDist.x) == 0) {
                cell.index = int(redCells.size()) + 1;
                redCells.push_back(cell);
                dst2(cell.rect).setTo(cell.index, cell.mask);
                dst3(cell.rect).setTo(cell.color, cell.mask);
                //task->SetTrueText(to_string(cell.index), cell.maxDist, 2);
                //task->SetTrueText(to_string(cell.index), cell.maxDist, 3);
            }
        }
        labels[3] = to_string(redCells.size()) + " cells were identified.";
        task->rcSelect = rcData();
        if (task->ClickPoint == Point(0, 0)) {
            if (redCells.size() > 2) {
                task->ClickPoint = redCells[0].maxDist;
                task->rcSelect = redCells[0];
            }
        }
        else {
            uchar index = dst2.at<uchar>(task->ClickPoint.y, task->ClickPoint.x);
            if (index != 0) task->rcSelect = redCells[index - 1];
        }
        lastColors = dst3.clone();
        lastMap = dst2.clone();
        if (redCells.size() > 0) dst1 = vbPalette(lastMap * 255 / redCells.size());
    }
};





class Mesh_Basics_CC : public CPP_Parent {
public:
    Random_Basics_CC* random;
    KNN_Basics_CC* knn;
    Mesh_Basics_CC() : CPP_Parent() {
        random = new Random_Basics_CC();
        knn = new KNN_Basics_CC();
        labels[2] = "Triangles built with each random point and its 2 nearest neighbors.";
        desc = "Build triangles from random points";
    }
    Mat showMesh(const vector<Point2f>& pointList) {
        if (pointList.size() <= 3) return dst2; // not enough points to draw...
        knn->queries = pointList;
        knn->trainInput = knn->queries;
        knn->Run(empty);
        dst2.setTo(0);
        for (int i = 0; i < knn->queries.size(); i++) {
            Point2f p0 = knn->queries[i];
            Point2f p1 = knn->queries[knn->result.at<int>(i, 1)];
            Point2f p2 = knn->queries[knn->result.at<int>(i, 2)];
            line(dst2, p0, p1, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            line(dst2, p0, p2, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            line(dst2, p1, p2, Scalar(255, 255, 255), task->lineWidth, task->lineType);
        }
        for (int i = 0; i < knn->queries.size(); i++) {
            circle(dst2, knn->queries[i], task->DotSize + 2, Scalar(0, 0, 255), -1, task->lineType);
        }
        return dst2;
    }
    void Run(Mat src) {
        if (task->heartBeat) {
            random->Run(empty);
            showMesh(random->pointList);
        }
    }
};






class Mesh_Features_CC : public CPP_Parent {
public:
    Feature_Stable_CC* feat;
    Mesh_Basics_CC* mesh;
    Mesh_Features_CC() : CPP_Parent() {
        feat = new Feature_Stable_CC();
        mesh = new Mesh_Basics_CC();
        labels[2] = "Triangles built with each feature point and its 2 nearest neighbors.";
        desc = "Build triangles from feature points";
    }
    void Run(Mat src) {
        feat->Run(src);
        if (feat->featurePoints.size() < 3) {
            return;
        }
        mesh->dst2 = src;
        dst2 = mesh->showMesh(feat->featurePoints);
    }
};






class Area_MinRect_CC : public CPP_Parent {
public:
    RotatedRect minRect;
    vector<Point2f> inputPoints;
    Area_MinRect_CC() : CPP_Parent() {
        desc = "Find minimum containing rectangle for a set of points.";
    }
    void Run(Mat src) {
        if (standalone) {
            if (!task->heartBeat) return;
            inputPoints = task->quickRandomPoints(10);
        }
        minRect = minAreaRect(inputPoints);
        if (standalone) {
            dst2.setTo(0);
            for (const Point2f& pt : inputPoints) {
                circle(dst2, pt, task->DotSize + 2, Scalar(0, 0, 255), -1, task->lineType);
            }
            task->DrawRotatedOutline(minRect, dst2, Scalar(0, 255, 255));
        }
    }
};







class Mat_4to1_CC : public CPP_Parent {
public:
    Mat mat[4];
    bool lineSeparators = true;

    Mat_4to1_CC() : CPP_Parent() {
        for (int i = 0; i < 4; i++) {
            mat[i] = dst2.clone();
        }

        desc = "Use one Mat for up to 4 images";
    }

    void defaultMats(Mat src)
    {
        Mat tmpLeft = task->leftView;
        if (task->leftView.channels() == 1) cvtColor(task->leftView, task->leftView, COLOR_GRAY2BGR);
        Mat tmpRight = task->rightView;
        if (task->rightView.channels() == 1) cvtColor(task->rightView, task->rightView, COLOR_GRAY2BGR);
        mat[0] = src.clone();
        mat[1] = task->depthRGB.clone();
        mat[2] = tmpLeft;
        mat[3] = tmpRight;
    }
    void Run(Mat src) {
        Size nSize(dst2.cols / 2, dst2.rows / 2);
        Rect roiTopLeft(0, 0, nSize.width, nSize.height);
        Rect roiTopRight(nSize.width, 0, nSize.width, nSize.height);
        Rect roiBotLeft(0, nSize.height, nSize.width, nSize.height);
        Rect roiBotRight(nSize.width, nSize.height, nSize.width, nSize.height);

        if (standalone) defaultMats(src);

        dst2 = Mat(dst2.size(), CV_8UC3);
        Rect rois[] = { roiTopLeft, roiTopRight, roiBotLeft, roiBotRight };
        for (int i = 0; i < 4; i++) {
            if (mat[i].channels() == 1) {
                cvtColor(mat[i], mat[i], COLOR_GRAY2BGR);
            }
            resize(mat[i], dst2(rois[i]), nSize);
        }

        if (lineSeparators) {
            line(dst2, Point(0, dst2.rows / 2), Point(dst2.cols, dst2.rows / 2), Scalar(255, 255, 255), task->lineWidth + 1);
            line(dst2, Point(dst2.cols / 2, 0), Point(dst2.cols / 2, dst2.rows), Scalar(255, 255, 255), task->lineWidth + 1);
        }
    }
};


class Mat_4Click_CC : public CPP_Parent {
public:
    Mat_4to1_CC* mats;
    int quadrant = 0;
    Mat_4Click_CC() : CPP_Parent() {
        mats = new Mat_4to1_CC();
        labels[3] = "Click a quadrant in dst2 to view it in dst3";
        desc = "Split an image into 4 segments and allow clicking on a quadrant to open it in dst3";
    }
    void Run(Mat src) {
        if (standalone) mats->defaultMats(src);
        if (task->FirstPass) {
            task->mouseClickFlag = true;
            task->ClickPoint = Point(0, 0);
            task->mousePicTag = 2;
        }
        if (task->mouseClickFlag && task->mousePicTag == 2) {
            if (task->ClickPoint.y < task->workRes.height / 2) {
                quadrant = (task->ClickPoint.x < task->workRes.width / 2) ? 0 : 1;
            }
            else {
                quadrant = (task->ClickPoint.x < task->workRes.width / 2) ? 2 : 3;
            }
        }
        mats->Run(empty);
        dst2 = mats->dst2;
        dst3 = mats->mat[quadrant];
    }
};






class Bin4Way_Regions_CC : public CPP_Parent {
public:
    Binarize_Simple_CC* binarize;
    Mat_4Click_CC* mats;
    Bin4Way_Regions_CC() : CPP_Parent() {
        binarize = new Binarize_Simple_CC();
        mats = new Mat_4Click_CC();
        labels[2] = "A 4-way split - lightest (upper left) to darkest (lower right)";
        desc = "Binarize an image twice using masks";
    }
    void Run(Mat src) {
        Mat gray;
        if (src.channels() == 1) {
            gray = src;
        }
        else {
            cvtColor(src, gray, COLOR_BGR2GRAY);
        }
        binarize->Run(gray);
        Mat mask = binarize->dst2.clone();
        double midColor = binarize->meanScalar(0);
        double topColor = mean(gray, mask)(0);
        double botColor = mean(gray, ~mask)(0);
        inRange(gray, topColor, 255, mats->mats->mat[0]);
        inRange(gray, midColor, topColor, mats->mats->mat[1]);
        inRange(gray, botColor, midColor, mats->mats->mat[2]);
        inRange(gray, 0, botColor, mats->mats->mat[3]);
        if (standalone) {
            mats->Run(empty);
            dst2 = mats->dst2;
            dst3 = mats->dst3;
            labels[3] = mats->labels[3];
        }
    }
};





class Bin4Way_RegionsCombine_CC : public CPP_Parent {
public:
    Bin4Way_Regions_CC* binarize;
    int classCount = 4;
    Bin4Way_RegionsCombine_CC() : CPP_Parent() {
        binarize = new Bin4Way_Regions_CC();
        dst1 = Mat::zeros(dst3.size(), CV_8U);
        desc = "Add the 4-way split of images to define the different regions.";
    }
    void Run(Mat src) {
        binarize->Run(src);
        dst1.setTo(1, binarize->mats->mats->mat[0]);
        dst1.setTo(2, binarize->mats->mats->mat[1]);
        dst1.setTo(3, binarize->mats->mats->mat[2]);
        dst1.setTo(4, binarize->mats->mats->mat[3]);
        if (standalone) {
            dst3 = dst1 * 255 / 5;
            dst2 = vbPalette(dst3);
        }
    }
};






class Color_Basics_CC : public CPP_Parent {
public:
    int classCount;
    string CurrentColorClassifier;
    //BackProject_Full* backP;
    //KMeans_Basics* km;
    //LUT_Basics* lut;
    //Reduction_Basics* reduction;
    //Hist3Dcolor_Basics* hColor;
    String colorInput;
    Bin4Way_RegionsCombine_CC* binarize;
    Color_Basics_CC() : CPP_Parent() {
        binarize = new Bin4Way_RegionsCombine_CC();
        desc = "Classify pixels by color using a variety of techniques";
    }
    void Run(Mat src) {

        if (src.channels() == 3)
        {
            cvtColor(src, dst1, COLOR_BGR2GRAY);
            switch (task->colorInputIndex) {
            case 0: // "BackProject_Full":
                //backP->Run(dst1);
                //classCount = backP->classCount;
                //dst2 = backP->dst2;
                //colorInput = backP->traceName;
                break;
            case 1: // "KMeans_Basics":
                //km->Run(dst1);
                //classCount = km->classCount;
                //dst2 = km->dst2;
                //colorInput = km->traceName;
                break;
            case 2: //"LUT_Basics":
                //lut->Run(dst1);
                //classCount = lut->classCount;
                //dst2 = lut->dst2;
                //colorInput = lut->traceName;
                break;
            case 3: //"Reduction_Basics":
                // reduction->Run(dst1);
                //classCount = reduction->classCount;
                //dst2 = reduction->dst2;
                //colorInput = reduction->traceName;
                break;
            case 4: //"3D BackProjection":
                //hColor->Run(dst1);
                //classCount = hColor->classCount;
                //dst2 = hColor->dst2;
                //colorInput = hColor->traceName;
                break;
            case 5:
                binarize->Run(dst1);
                classCount = binarize->classCount;
                dst2 = binarize->dst1;
                colorInput = binarize->traceName;
                break;
            }
        }
        dst3 = vbPalette(dst2 * 255 / classCount);
        labels[2] = "Color_Basics: method = " + colorInput + " produced " + to_string(classCount) +
            " pixel classifications";
    }
};




class Blur_Basics_CC : public CPP_Parent {
public:
    Options_Blur* options = new Options_Blur;
    Blur_Basics_CC() : CPP_Parent() {
        desc = "Smooth each pixel with a Gaussian kernel of different sizes.";
    }
    void Run(cv::Mat src) {
        options->Run();
        cv::GaussianBlur(src, dst2, cv::Size(options->kernelSize, options->kernelSize),
            options->sigmax, options->sigmay);
    }
};






class Palette_Random_CC : public CPP_Parent {
public:
    Mat colorMap;
    Palette_Random_CC() : CPP_Parent() {
        colorMap = Mat(256, 1, CV_8UC3, Scalar(0, 0, 0));
        for (int i = 1; i < 256; i++) {
            colorMap.at<Vec3b>(i, 0) = task->randomCellColor();
        }
        desc = "Build a random colorGrad - no smooth transitions.";
    }
    void Run(Mat src) {
        applyColorMap(src, dst2, colorMap);
    }
};






class Hist_RedOptions_CC : public CPP_Parent {
public:
    std::vector<cv::Range> ranges;
    std::vector<cv::Range> rangesCloud;
    Hist_RedOptions_CC() : CPP_Parent() {
        desc = "Build the channels, channel count, and ranges based on the PointCloud Reduction setting.";
    }
    void Run(Mat src) {
        cv::Vec2f rx(-task->xRangeDefault, task->xRangeDefault);
        cv::Vec2f ry(-task->yRangeDefault, task->yRangeDefault);
        cv::Vec2f rz(0, task->MaxZmeters);
        rangesCloud = { cv::Range(rx[0], rx[1]), cv::Range(ry[0], ry[1]), cv::Range(rz[0], rz[1]) };

        task->channelCount = 1;
        task->histBins[0] = task->histogramBins;
        task->histBins[1] = task->histogramBins;
        task->histBins[2] = task->histogramBins;
        switch (task->PCReduction) {
        case 0: // "X Reduction"
            ranges = { cv::Range(rx[0], rx[1]) };
            task->channels[0] = 0;
            break;
        case 1: // "Y Reduction"
            ranges = { cv::Range(ry[0], ry[1]) };
            task->channels[0] = 1;
            break;
        case 2: // "Z Reduction"
            ranges = { cv::Range(rz[0], rz[1]) };
            task->channels[0] = 2;
            break;
        case 3: // "XY Reduction"
            ranges = { cv::Range(rx[0], rx[1]), cv::Range(ry[0], ry[1]) };
            task->channelCount = 2;
            task->channels[0] = 0;
            task->channels[1] = 1;
            break;
        case 4: // "XZ Reduction"
            ranges = { cv::Range(rx[0], rx[1]), cv::Range(rz[0], rz[1]) };
            task->channelCount = 2;
            task->channels[0] = 0;
            task->channels[1] = 2;
            task->channelIndex = 1;
            break;
        case 5: // "YZ Reduction"
            ranges = { cv::Range(ry[0], ry[1]), cv::Range(rz[0], rz[1]) };
            task->channelCount = 2;
            task->channels[0] = 1;
            task->channels[1] = 2;
            task->channelIndex = 1;
            break;
        case 6: // "XYZ Reduction"
            ranges = { cv::Range(rx[0], rx[1]), cv::Range(ry[0], ry[1]), cv::Range(rz[0], rz[1]) };
            task->channelCount = 3;
            task->channels[0] = 0;
            task->channels[1] = 1;
            task->channels[2] = 2;
            task->channelIndex = 2;
            break;
        }

        labels[2] = "RedOptions are now set...";
    }
};






class Plot_Histogram2D_CC : public CPP_Parent {
public:
    Plot_Histogram2D_CC() : CPP_Parent() {
        labels = { "", "", "2D Histogram", "Threshold of all non-zero values in the plot at left." };
        desc = "Plot a 2D histogram from the input Mat";
    }
    void Run(Mat src) {
        Mat histogram = src.clone();
        if (standalone) {
            float hRange[] = { 0, 255 };
            const float* range[] = { hRange,hRange,hRange };
            calcHist(&src, 1, task->channels, Mat(), histogram, task->channelCount, task->histBins, range, true, false);
        }
        if (histogram.rows > 0 && histogram.cols > 0)
            resize(histogram, dst2, dst2.size(), 0, 0, INTER_NEAREST);
        else
            labels[2] = "Unable to portray a 3D histogram here.";
        if (standalone) threshold(dst2, dst3, 0, 255, THRESH_BINARY);
    }
};





class Feature_StableSorted_CC : public CPP_Parent {
public:
    Feature_Stable_CC* feat;
    int desiredCount = 200;
    vector<Point2f> stablePoints;
    vector<int> generations;
    Feature_StableSorted_CC() : CPP_Parent() {
        feat = new Feature_Stable_CC();
        desc = "Display the top X feature points ordered by generations they were present.";
    }
    void Run(Mat src) {
        feat->Run(src.clone());
        dst3 = feat->dst2;
        dst2 = src;
        if (task->optionsChanged) {
            stablePoints.clear();
            generations.clear();
        }
        for (int i = 0; i < feat->featurePoints.size(); i++) {
            Point2f pt = feat->featurePoints[i];
            auto it = find(stablePoints.begin(), stablePoints.end(), pt);
            if (it != stablePoints.end()) {
                int index = distance(stablePoints.begin(), it);
                generations[index]++;
            }
            else {
                stablePoints.push_back(pt);
                generations.push_back(1);
            }
        }
        map<int, Point2f, greater<int>> sortByGen;
        for (int i = 0; i < stablePoints.size(); i++) {
            sortByGen[generations[i]] = stablePoints[i];
        }
        int displayCount = 0;
        stablePoints.clear();
        generations.clear();
        for (auto it = sortByGen.begin(); it != sortByGen.end(); it++) {
            if (displayCount >= desiredCount * 2) break;
            Point2f pt = it->second;
            if (displayCount < desiredCount) {
                circle(dst2, pt, task->DotSize, Scalar(255, 255, 255), -1, task->lineType);
                displayCount++;
            }
            stablePoints.push_back(pt);
            generations.push_back(it->first);
        }
        labels[2] = "The most stable " + to_string(displayCount) + " are highlighted below";
        labels[3] = "Output of Feature_Stable" + to_string(feat->featurePoints.size()) + " points found";
    }
};






class BGSubtract_Basics_CC : public CPP_Parent {
public:
    Options_BGSubtract* options = new Options_BGSubtract;
    BGSubtract_BGFG* cPtr = nullptr;
    vector<string> labels = { "", "", "BGSubtract output - aging differences", "Mask for any changes" };
    BGSubtract_Basics_CC() : CPP_Parent() {
        desc = "Different background subtraction algorithms in OpenCV - some only available in C++";
    }
    void Run(Mat src) {
        if (task->optionsChanged) {
            Close();
            cPtr = BGSubtract_BGFG_Open(options->currMethod);
        }
        void* imagePtr = BGSubtract_BGFG_Run(cPtr, (int*)src.data, src.rows, src.cols, src.channels(), options->learnRate);
        if (imagePtr) {
            dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr).clone();
            threshold(dst2, dst3, 0, 255, THRESH_BINARY);
        }
        labels[2] = options->methodDesc;
    }
    void Close() {
        if (cPtr) {
            BGSubtract_BGFG_Close(cPtr);
            cPtr = nullptr;
        }
    }
};





class Motion_BGSub_CC : public CPP_Parent {
public:
    Options_BGSubtract* options = new Options_BGSubtract;
    BGSubtract_BGFG* cPtr = nullptr;
    string desc = "Detect motion using background subtraction algorithms in OpenCV - some only available in C++";
    Motion_BGSub_CC() : CPP_Parent() {
        cPtr = BGSubtract_BGFG_Open(options->currMethod);
        labels[2] = "BGSubtract output";
    }
    void Run(Mat src) {
        if (task->optionsChanged) {
            Close();
            cPtr = BGSubtract_BGFG_Open(options->currMethod);
        }
        void* imagePtr = BGSubtract_BGFG_Run(cPtr, (int*)src.data, src.rows, src.cols, src.channels(), options->learnRate);
        dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr);
        labels[2] = options->methodDesc;
    }
    void Close() {
        if (cPtr) {
            BGSubtract_BGFG_Close(cPtr);
            cPtr = nullptr;
        }
    }
};






class Feature_Agast_CC : public CPP_Parent {
private:
public:
    vector<KeyPoint> featurePoints;
    vector<Point2f> stablePoints;
    Ptr<AgastFeatureDetector> agastFD;
    std::vector<Point2f> lastPoints;
    Feature_Agast_CC() : CPP_Parent()
    {
        agastFD = AgastFeatureDetector::create(10, true, AgastFeatureDetector::OAST_9_16);
        desc = "Use the Agast Feature Detector in the OpenCV Contrib.";
    }
    void Run(Mat src) {
        int resizeFactor = 1;
        Mat input;
        if (src.cols >= 1280)
        {
            resize(src, input, cv::Size(src.cols / 4, src.rows / 4));
            resizeFactor = 4;
        }
        else {
            input = src;
        }

        featurePoints.clear();

        agastFD->detect(input, featurePoints);
        if (task->heartBeat || lastPoints.size() < 10) {
            lastPoints.clear();
            for (const KeyPoint& kpt : featurePoints) {
                lastPoints.push_back(Point2f(round(kpt.pt.x) * resizeFactor, round(kpt.pt.y) * resizeFactor));
            }
        }

        stablePoints.clear();
        dst2 = src;
        for (const KeyPoint& pt : featurePoints) {
            auto p1 = Point2f(round(pt.pt.x * resizeFactor), round(pt.pt.y * resizeFactor));
            if (find(lastPoints.begin(), lastPoints.end(), p1) != lastPoints.end()) {
                stablePoints.push_back(p1);
                circle(dst2, p1, task->DotSize, Scalar(0, 0, 255), -1, task->lineType);
            }
        }

        lastPoints = stablePoints;
        if (task->midHeartBeat) {
            labels[2] = std::to_string(featurePoints.size()) + " features found and " + std::to_string(stablePoints.size()) + " of them were stable";
        }

        labels[2] = "Found " + to_string(featurePoints.size()) + " features";
    }
};




class Feature_AKaze_CC : public CPP_Parent {
public:
    std::vector<cv::KeyPoint> kazeKeyPoints;
    Feature_AKaze_CC() : CPP_Parent()
    {
        labels[2] = "AKAZE key points";
        desc = "Find keypoints using AKAZE algorithm.";
    }
    void Run(cv::Mat src)
    {
        dst2 = src.clone();
        if (src.channels() != 1)
            cv::cvtColor(src, src, cv::COLOR_BGR2GRAY);
        cv::Ptr<cv::AKAZE> kaze = cv::AKAZE::create();
        cv::Mat kazeDescriptors;
        kaze->detectAndCompute(src, cv::noArray(), kazeKeyPoints, kazeDescriptors);
        for (const auto& keyPoint : kazeKeyPoints)
        {
            cv::circle(dst2, keyPoint.pt, task->DotSize, task->highlight);
        }
    }
};




class AddWeighted_DepthAccumulate_CC : public CPP_Parent {
private:
    Options_AddWeighted* options = new Options_AddWeighted();
public:
    AddWeighted_DepthAccumulate_CC() : CPP_Parent()
    {
        desc = "Update a running average of the image";
    }
    void Run(Mat src)
    {
        options->Run();
        if (task->optionsChanged)
        {
            dst2 = task->pcSplit[2] * 1000;
        }
        cv::accumulateWeighted(task->pcSplit[2] * 1000, dst2, (float)options->accumWeighted, cv::Mat());
    }
};






//class Edge_Basics_CC : public CPP_Parent {
//private:
//    Edge_Canny canny;
//    Edge_Scharr scharr;
//    Edge_BinarizedReduction binRed;
//    Bin4Way_Sobel binSobel;
//    Edge_ColorGap_CC_VB colorGap;
//    Edge_Deriche_CC_VB deriche;
//    Edge_Laplacian Laplacian;
//    Edge_ResizeAdd resizeAdd;
//    Edge_Regions regions;
//public:
//    Options_Edge_Basics* options;
//    Edge_Basics_CC() : CPP_Parent()
//    {
//        desc = "Use Radio Buttons to select the different edge algorithms.";
//    }
//    void Run(cv::Mat src)
//    {
//        options->Run();
//        std::string edgeSelection = options->edgeSelection;
//        if (edgeSelection == "Canny")
//        {
//            canny.Run(src);
//            dst2 = canny.dst2;
//        }
//        else if (edgeSelection == "Scharr")
//        {
//            scharr.Run(src);
//            dst2 = scharr.dst3;
//        }
//        else if (edgeSelection == "Binarized Reduction")
//        {
//            binRed.Run(src);
//            dst2 = binRed.dst2;
//        }
//        else if (edgeSelection == "Binarized Sobel")
//        {
//            binSobel.Run(src);
//            dst2 = binSobel.dst2;
//        }
//        else if (edgeSelection == "Color Gap")
//        {
//            colorGap.Run(src);
//            dst2 = colorGap.dst2;
//        }
//        else if (edgeSelection == "Deriche")
//        {
//            deriche.Run(src);
//            dst2 = deriche.dst2;
//        }
//        else if (edgeSelection == "Laplacian")
//        {
//            Laplacian.Run(src);
//            dst2 = Laplacian.dst2;
//        }
//        else if (edgeSelection == "Resize And Add")
//        {
//            resizeAdd.Run(src);
//            dst2 = resizeAdd.dst2;
//        }
//        else if (edgeSelection == "Depth Region Boundaries")
//        {
//            regions.Run(src);
//            dst2 = regions.dst2;
//        }
//        if (dst2.channels() != 1)
//            cv::cvtColor(dst2, dst2, cv::COLOR_BGR2GRAY);
//        labels[2] = traceName + " - selection = " + edgeSelection;
//    }
//};





class Edge_Basics_CC : public CPP_Parent {
private:
    Edge_Canny_CC* canny = new Edge_Canny_CC();
    Options_Edge_Basics* options = new Options_Edge_Basics();
public:
    Edge_Basics_CC() : CPP_Parent()
    {
        labels[2] = "Edge_Basics_CC";
        desc = "Use Radio Buttons to select the different edge algorithms.";
    }
    void Run(cv::Mat src)
    {
        options->Run();
        canny->Run(src);
        dst2 = canny->dst2;
        if (dst2.channels() != 1)
            cv::cvtColor(dst2, dst2, cv::COLOR_BGR2GRAY);
        labels[2] = traceName + " - selection = " + options->edgeSelection;
    }
};






class Hist_DepthSimple_CC : public CPP_Parent {
public:
    std::vector<float> histList;
    std::vector<float> histArray;
    cv::Mat histogram;
    Plot_Histogram_CC plotHist;
    mmData mm;
    cv::Mat inputRemoved;
    float ranges[2];
    Hist_DepthSimple_CC() : CPP_Parent()
    {
        plotHist.addLabels = false;
        desc = "Use Kalman to smooth the histogram results.";
    }
    void Run(cv::Mat src)
    {
        if (standalone)
        {
            mm = task->vbMinMax(task->pcSplit[2]);
            ranges[0] = (float)mm.minVal;
            ranges[1] = (float)mm.maxVal;
        }

        int bins[] = { task->histogramBins };
        float hRange[] = { static_cast<float>(ranges[0]), static_cast<float>(ranges[1]) };
        const float* range[] = { hRange };
        calcHist(&task->pcSplit[2], 1, { 0 }, Mat(), histogram, 1, bins, range, true, false);

        histArray.resize(histogram.total());
        std::memcpy(histArray.data(), histogram.ptr<float>(), histogram.total() * sizeof(float));
        if (standalone)
        {
            plotHist.Run(histogram);
            dst2 = plotHist.dst2;
        }
        histList = histArray;
    }
};

#endif
