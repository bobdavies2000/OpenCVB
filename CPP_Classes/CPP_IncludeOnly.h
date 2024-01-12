#pragma once
#include <string.h>
#include <Windows.h>
#include <OleAuto.h>
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
#include <iostream>
#include <memory>
#include <vector>
#include <random>
#include "opencv2/video/tracking.hpp"
#include <filesystem> 

using namespace std;
using namespace cv;
using namespace ximgproc;
using namespace ml;
namespace fs = std::filesystem;

#include "../CPP_Classes/PragmaLibs.h"
#include "CPP_Task.h"

#include "EdgeDraw.h"
#include "FloodCell.h"

cppTask* task;

class CPP_AddWeighted_Basics : public algorithmCPP {
public:
    Mat src2;

    CPP_AddWeighted_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_AddWeighted_Basics";
        desc = "Add 2 images with specified weights.";
    }

    void Run(Mat src) {
        Mat srcPlus = src2;
        if (standalone || src2.empty()) {
            srcPlus = task->depthRGB;
        }

        if (srcPlus.type() != src.type()) {
            if (src.type() != CV_8UC3 || srcPlus.type() != CV_8UC3) {
                if (src.type() == CV_32FC1) {
                    src.convertTo(src, CV_8UC1, 255.0);  // Normalize 32-bit float
                }
                if (srcPlus.type() == CV_32FC1) {
                    srcPlus.convertTo(srcPlus, CV_8UC1, 255.0);
                }
                if (src.type() != CV_8UC3) {
                    cvtColor(src, src, COLOR_GRAY2BGR);
                }
                if (srcPlus.type() != CV_8UC3) {
                    cvtColor(srcPlus, srcPlus, COLOR_GRAY2BGR);
                }
            }
        }

        double wt = task->addWeighted;
        addWeighted(src, wt, srcPlus, 1.0 - wt, 0.0, dst2);  // Ensure correct syntax
        labels[2] = "depth " + to_string((int)((1 - wt) * 100)) + " BGR " + to_string((int)(wt * 100));
    }
};





class CPP_Random_Basics : public algorithmCPP {
public:
    vector<Point2f> pointList;
    Rect range;
    int sizeRequest = 10;

    CPP_Random_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Random_Basics";
        range = Rect(0, 0, dst2.cols, dst2.rows);
        desc = "Create a uniform random mask with a specified number of pixels.";
    }

    void Run(Mat src) {
        pointList.clear();
        if (!task->paused) {
            while (pointList.size() < sizeRequest) {
                pointList.push_back(Point2f(range.x + float((rand() % range.width)), range.y + float((rand() % range.height))));
            }

            dst2.setTo(0);
            for (Point2f pt : pointList) {
                circle(dst2, pt, task->dotSize, Scalar(0, 255, 255), -1, task->lineType, 0);
            }
        }
    }
};









class CPP_Feature_Agast : public algorithmCPP
{
private:
public:
    vector<KeyPoint> featurePoints;
    CPP_Feature_Agast(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_Feature_Agast";
        desc = "Use the Agast Feature Detector in the OpenCV Contrib.";
    }
    void Run(Mat src) {
        featurePoints.clear();
        static Ptr<AgastFeatureDetector> agastFD = AgastFeatureDetector::create(10, true, AgastFeatureDetector::OAST_9_16);
        agastFD->detect(src, featurePoints);
        dst2 = src;
        for (size_t i = 0; i < featurePoints.size(); i++)
        {
            circle(dst2, featurePoints[i].pt, task->dotSize, RED, -1, task->lineType);
        }
        labels[2] = "Found " + to_string(featurePoints.size()) + " features";
    }
};







class CPP_Resize_Basics : public algorithmCPP {
public:
    Size newSize;
    float resizePercent = 0.5;

    CPP_Resize_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Resize_Basics";
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






class CPP_Remap_Basics : public algorithmCPP {
public:
    int direction = 3;  // Default to remap horizontally and vertically
    Mat mapx1, mapx2, mapx3, mapy1, mapy2, mapy3;

    CPP_Remap_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Remap_Basics";
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




class CPP_Random_Enumerable : public algorithmCPP {
public:
    int sizeRequest = 100;
    vector<Point2f> points;

    CPP_Random_Enumerable(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Random_Enumerable";
        desc = "Create an enumerable list of points using a lambda function";
    }

    void Run(Mat src) {
        random_device rd;
        mt19937 gen(rd());  // Mersenne Twister engine for randomness
        uniform_int_distribution<> dist_width(0, dst2.cols - 1);  // Ensure width is within bounds
        uniform_int_distribution<> dist_height(0, dst2.rows - 1); // Ensure height is within bounds

        points.clear();
        for (int i = 0; i < sizeRequest; i++) {
            points.push_back(Point2f(dist_width(gen), dist_height(gen)));
        }

        dst2 = Mat::zeros(dst2.size(), dst2.type());  // Set dst2 to black
        for (const Point2f& pt : points) {
            circle(dst2, pt, task->dotSize, Scalar(0, 255, 255), -1, task->lineType, 0);
        }
    }
};



class CPP_Delaunay_Basics : public algorithmCPP {
public:
    vector<Point2f> inputPoints;
    Mat facet32s;
    CPP_Random_Enumerable* randEnum;
    Subdiv2D subdiv;
    vector<vector<Point>> facetlist;

    CPP_Delaunay_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Delaunay_Basics";
        randEnum = new CPP_Random_Enumerable(rows, cols);
        facet32s = Mat::zeros(dst2.size(), CV_32SC1);
        desc = "Subdivide an image based on the points provided.";
    }
    void randomInput(Mat src)
    {
        randEnum->Run(src);
        inputPoints = randEnum->points;
    }
    void Run(Mat src) {
        if (task->heartBeat && standalone) {
            randEnum->Run(src);
            inputPoints = randEnum->points;
            dst3 = randEnum->dst2;
        }

        subdiv.initDelaunay(Rect(0, 0, dst2.cols, dst2.rows));
        subdiv.insert(inputPoints);

        vector<vector<Point2f>> facets;
        vector<Point2f> centers;
        subdiv.getVoronoiFacetList(vector<int>(), facets, centers);

        vector<Vec3b> usedColors;
        facetlist.clear();
        static Mat lastColor = Mat::zeros(dst2.size(), CV_8UC3);
        for (int i = 0; i < facets.size(); i++) {
            vector<Point> nextFacet;
            for (int j = 0; j < facets[i].size(); j++) {
                nextFacet.push_back(Point(facets[i][j].x, facets[i][j].y));
            }

            Point2f pt = inputPoints[i];
            Vec3b nextColor = lastColor.at<Vec3b>(pt);
            if (find(usedColors.begin(), usedColors.end(), nextColor) != usedColors.end()) {
                nextColor = task->randomCellColor();
            }
            usedColors.push_back(nextColor);

            fillConvexPoly(dst2, nextFacet, Scalar(nextColor[0], nextColor[1], nextColor[2]));

            fillConvexPoly(facet32s, nextFacet, i, task->lineType);
            facetlist.push_back(nextFacet);
        }
        facet32s.convertTo(dst1, CV_8U);

        lastColor = dst2.clone();
        labels[2] = traceName + ": " + to_string(inputPoints.size()) + " cells were present.";
    }
};





class CPP_Delaunay_GenerationsNoKNN : public algorithmCPP {
public:
    vector<Point2f> inputPoints;
    CPP_Random_Basics* random;
    CPP_Delaunay_Basics* facet;

    CPP_Delaunay_GenerationsNoKNN(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Delaunay_GenerationsNoKNN";
        facet = new CPP_Delaunay_Basics(rows, cols);
        random = new CPP_Random_Basics(rows, cols);
        dst3 = Mat::zeros(dst3.size(), CV_32S);
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

        Mat generationMap = dst3.clone();
        if (task->heartBeat) generationMap.setTo(0);
        dst3.setTo(0);
        vector<int> usedG;
        int g;
        for (const auto& pt : inputPoints) {
            int index = facet->facet32s.at<int>(pt.y, pt.x);
            if (index >= facet->facetlist.size()) continue;
            vector<Point> nextFacet = facet->facetlist[index];

            // Ensure unique generation numbers
            if (task->firstPass) {
                g = (int) usedG.size();
            }
            else {
                g = generationMap.at<int>(pt.y, pt.x) + 1;
                while (find(usedG.begin(), usedG.end(), g) != usedG.end()) {
                    g++;
                }
            }
            fillConvexPoly(dst3, nextFacet, g, task->lineType);
            usedG.push_back(g);
            task->setTrueText(to_string(g), dst2, pt);   
        }
        generationMap = dst3.clone();
    }
};







class CPP_Grid_Basics : public algorithmCPP {
public:
    CPP_Grid_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Grid_Basics";
        desc = "Create a grid of squares covering the entire image.";
    }

    void Run(Mat src) {
        if (task->mouseClickFlag && !task->firstPass) {
            task->gridROIclicked = task->gridToRoiIndex.at<int>(task->clickPoint.y, task->clickPoint.x);
        }
        if (task->optionsChanged) {
            task->gridMask = Mat::zeros(src.size(), CV_8U);
            task->gridToRoiIndex = Mat::zeros(src.size(), CV_32S);

            task->gridList.clear();
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
                        task->gridList.push_back(roi);
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

            for (int i = 0; i < task->gridList.size(); i++) {
                Rect roi = task->gridList[i];
                rectangle(task->gridToRoiIndex, roi, Scalar(255, 255, 255), 1);
            }

            task->gridNeighbors.clear();
            for (const Rect& roi : task->gridList) {
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
            ss << "Grid_Basics " << task->gridList.size() << " (" << task->gridRows << "X" << task->gridCols << ") "
                << task->gridSize << "X" << task->gridSize << " regions";
            labels[2] = ss.str();
        }
    }
};





class CPP_KNN_Basics : public algorithmCPP
{
public:
    Ptr<ml::KNearest> knn = ml::KNearest::create();
    vector<Point2f> trainInput;
    vector<Point2f> queries;
    vector<vector<int>> neighbors;
    Mat result;
    int desiredMatches = -1;
    CPP_Random_Basics* random;
    vector<int> neighborIndexToTrain;
    
    CPP_KNN_Basics(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_KNN_Basics";
        random = new CPP_Random_Basics(rows, cols);
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
            circle(dst2, pt, task->dotSize + 4, Scalar(0, 255, 255), -1, task->lineType);  // Yellow
            line(dst2, pt, nn, Scalar(0, 255, 255), task->lineWidth, task->lineType);
        }

        for (Point2f pt : trainInput) {
            circle(dst2, pt, task->dotSize + 4, Scalar(0, 0, 255), -1, task->lineType);  // Red
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
            task->setTrueText("There were no queries provided. There is nothing to do...", dst2);
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





class CPP_KNN_Lossy : public algorithmCPP
{
private:
public:
    vector<linePoints> matches;
    vector<Point> noMatch;
    CPP_KNN_Basics* basics;
    vector<Point2f> queries;
    vector<int> neighbors;
    CPP_Random_Basics* random;

    CPP_KNN_Lossy(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_KNN_Lossy";
        basics = new CPP_KNN_Basics(rows, cols);
        random = new CPP_Random_Basics(rows, cols);
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
            task->setTrueText("Place some input points in queries before starting the knn run.", dst2);
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
            circle(dst3, pt, task->dotSize + 4, Scalar(0, 0, 255), -1, task->lineType);
        }

        noMatch.clear();
        matches.clear();
        for (int i = 0; i < neighbors.size(); i++) {
            Point2f pt = queries[i];
            circle(dst3, pt, task->dotSize + 4, Scalar(0, 255, 255), -1, task->lineType);
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





class CPP_Delaunay_Generations : public algorithmCPP
{
private:
public:
    vector<Point2f> inputPoints;
    CPP_Delaunay_Basics* facet;
    CPP_KNN_Lossy* knn;
    CPP_Random_Basics* random;

    CPP_Delaunay_Generations(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_Delaunay_Generations";
        dst3 = Mat::zeros(dst3.size(), CV_32S);
        knn = new CPP_KNN_Lossy(rows, cols);
        facet = new CPP_Delaunay_Basics(rows, cols);
        random = new CPP_Random_Basics(rows, cols);
        labels = {"", "Mask of unmatched regions - generation set to 0", "Facet Image with count for each region",
                  "Generation counts in CV_32SC1 format" };
        desc = "Create a region in an image for each point provided";
    }
    void Run(Mat src) {
        if (standalone) {
            if (task->firstPass) {
                random->sizeRequest = 10;
            }
            if (task->heartBeat) {
                random->Run(empty);
            }
            inputPoints = random->pointList;
        }

        knn->queries = inputPoints;
        knn->Run(empty);

        facet->inputPoints = inputPoints;
        facet->Run(src);
        dst2 = facet->dst2;

        Mat generationMap = dst3.clone();
        dst3.setTo(0);
        vector<int> usedG;
        int g;
        for (const linePoints& mp : knn->matches) {
            int index = facet->facet32s.at<int>(mp.p2.y, mp.p2.x);
            if (index >= facet->facetlist.size()) continue;
            const vector<Point>& nextFacet = facet->facetlist[index];

            // Ensure unique generation numbers
            if (task->firstPass) {
                g = (int)usedG.size();
            }
            else {
                g = generationMap.at<int>(mp.p2.y, mp.p2.x) + 1;
                while (find(usedG.begin(), usedG.end(), g) != usedG.end()) {
                    g++;
                }
            }

            fillConvexPoly(dst3, nextFacet, g, task->lineType);
            usedG.push_back(g);
            task->setTrueText(to_string(g), dst2, mp.p2);
        }
    }
};





class CPP_Feature_Basics : public algorithmCPP {
private: 
public: 
    Ptr<BRISK> brisk;
    vector<Point2f> corners;
    bool useBRISK = true;
    int minDistance = 15;
    int sampleSize = 400;
    int quality = 1;

    CPP_Feature_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Feature_Basics";
        brisk = BRISK::create();
        desc = "Find good features to track in a BGR image.";
    }
	void Run(Mat src)
	{
        dst2 = src.clone();

        if (src.channels() == 3) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }

        corners.clear();
        if (useBRISK) {
            vector<KeyPoint> keyPoints;
            brisk->detect(src, keyPoints);
            for (const KeyPoint& kp : keyPoints) {
                if (kp.size >= minDistance) {
                    corners.push_back(kp.pt);
                }
            }
        } else {
            vector<Point2f> tempCorners;
            goodFeaturesToTrack(src, tempCorners, sampleSize, quality, minDistance, noArray(), 7, true, 3);
            corners = tempCorners;
        }

        Scalar color = dst2.channels() == 3 ? Scalar(0, 255, 255) : Scalar(255, 255, 255);
        for (const Point2f& c : corners) {
            circle(dst2, c, task->dotSize, color, -1, task->lineType);
        }

        labels[2] = "Found " + to_string(corners.size()) + " points with quality = " + to_string(quality) +
                    " and minimum distance = " + to_string(minDistance);
    }
};





class CPP_Stable_Basics : public algorithmCPP {
public:
    CPP_Delaunay_Generations* facetGen;
    vector<Point2f> ptList;
    Point2f anchorPoint;
    CPP_Feature_Basics* good;

    CPP_Stable_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Stable_Basics";
        good = new CPP_Feature_Basics(rows, cols);
        facetGen = new CPP_Delaunay_Generations(rows, cols);
        desc = "Maintain the generation counts around the feature points.";
    }

    void Run(Mat src) {
        if (standalone) {
            good->Run(src);
            facetGen->inputPoints = good->corners;
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
            task->setTrueText(to_string(g), dst2, pt);
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
            circle(dst2, pt, task->dotSize, Scalar(0, 255, 255), task->lineWidth, task->lineType);
            circle(dst3, pt, task->dotSize, Scalar(255, 255, 255), task->lineWidth, task->lineType);
        }

        string text = to_string(ptList.size()) + " stable points were identified with " + to_string(maxGens) + " generations at the anchor point";
        labels[2] = text;
    }
};





class CPP_Stable_BasicsCount : public algorithmCPP
{
private:
public:
    CPP_Stable_Basics* basics;
    CPP_Feature_Basics* good;
    map<int, int, greater<float>> goodCounts;
    CPP_Stable_BasicsCount(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_Stable_BasicsCount";
        good = new CPP_Feature_Basics(rows, cols);
        basics = new CPP_Stable_Basics(rows, cols);
        desc = "Track the stable good features found in the BGR image.";
    }
    void Run(Mat src)
    {
        good->Run(src);
        basics->facetGen->inputPoints = good->corners;
        basics->Run(src);
        dst2 = basics->dst2;
        dst3 = basics->dst3;

        goodCounts.clear();
        int g;
        for (auto i = 0; i < basics->ptList.size(); i++)
        {
            Point2f pt = basics->ptList[i];
            circle(dst2, pt, task->dotSize, YELLOW, task->lineWidth, task->lineType);
            g = basics->facetGen->dst3.at<int>(pt.y, pt.x);
            goodCounts[g] = i;
            task->setTrueText(to_string(g), dst2, pt);
        }
    }
};


class CPP_FPoly_TopFeatures : public algorithmCPP {
public:
    CPP_Stable_BasicsCount* stable;
    vector<Point2f> poly;

    CPP_FPoly_TopFeatures(int rows, int cols) : algorithmCPP(rows, cols) {
        stable = new CPP_Stable_BasicsCount(rows, cols);
        traceName = "CPP_FPoly_TopFeatures";
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






class CPP_Edge_Scharr : public algorithmCPP {
public:
    CPP_Edge_Scharr(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Edge_Scharr";
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







class CPP_Mat_4to1 : public algorithmCPP {
public:
    Mat mat[4];
    bool lineSeparators = true;

    CPP_Mat_4to1(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Mat_4to1";
        for (int i = 0; i < 4; i++) {
            mat[i] = dst2.clone();
        }

        desc = "Use one Mat for up to 4 images";
    }

    void Run(Mat src) {
        Size nSize(dst2.cols / 2, dst2.rows / 2);
        Rect roiTopLeft(0, 0, nSize.width, nSize.height);
        Rect roiTopRight(nSize.width, 0, nSize.width, nSize.height);
        Rect roiBotLeft(0, nSize.height, nSize.width, nSize.height);
        Rect roiBotRight(nSize.width, nSize.height, nSize.width, nSize.height);

        if (standalone) {
            Mat tmpLeft = task->leftView;
            if (task->leftView.channels() == 1) cvtColor(task->leftView, task->leftView, COLOR_GRAY2BGR);
            Mat tmpRight = task->rightView;
            if (task->rightView.channels() == 1) cvtColor(task->rightView, task->rightView, COLOR_GRAY2BGR);
            mat[0] = src.clone();
            mat[1] = task->depthRGB.clone();
            mat[2] = tmpLeft;
            mat[3] = tmpRight;
        }

        dst2 = Mat(dst2.size(), CV_8UC3);
        Rect rois[] = {roiTopLeft, roiTopRight, roiBotLeft, roiBotRight};
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


class CPP_Depth_Colorizer : public algorithmCPP {
public:
    CPP_Depth_Colorizer(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Depth_Colorizer";
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
                if (pixel > 0 && pixel <= task->maxZmeters) {
                    float t = pixel / task->maxZmeters;
                    Scalar colorS = 255 * ((1 - t) * nearColor + t * farColor);
                    Vec3b color = Vec3b(colorS[0], colorS[1], colorS[2]);
                    dst2.at<Vec3b>(y, x) = color;
                }
            }
        }
    }
};





class CPP_RedCloud_Core : public algorithmCPP {
public:
    int classCount, givenClassCount;

    CPP_RedCloud_Core(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_RedCloud_Core";
        desc = "Reduction transform for the point cloud";
    }

    void Run(Mat src) {
        int reduceAmt = 500;
        dst0 = task->pointCloud.clone();
        dst0.convertTo(dst0, CV_32S, 1000.0 / reduceAmt);

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
        dst2 *= classCount / maxVal;
        dst2 += givenClassCount + 1;
        dst2.convertTo(dst2, CV_8U);

        labels[2] = "Reduced Pointcloud - reduction factor = " + to_string(reduceAmt) +
                    " produced " + to_string(classCount) + " regions";
    }
};






class CPP_Depth_PointCloud : public algorithmCPP
{
private: 
public: 
	CPP_Depth_PointCloud(int rows, int cols) : algorithmCPP(rows, cols) 
    {
        traceName = "CPP_Depth_PointCloud";
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






class CPP_IMU_GMatrix_QT : public algorithmCPP
{
private:
public:
    float cx = 1.0f, sx = 0.0f, cy = 1.0f, sy = 0.0f, cz = 1.0f, sz = 0.0f;
    bool usingSliders = false;
    CPP_IMU_GMatrix_QT(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_IMU_GMatrix_QT";
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

        float gMatrix[3][3] = {{gMt[0][0] * cy  + gMt[0][1] * 0 + gMt[0][2] * sy,
                                gMt[0][0] * 0   + gMt[0][1] * 1 + gMt[0][2] * 0,
                                gMt[0][0] * -sy + gMt[0][1] * 0 + gMt[0][2] * cy},
                               {gMt[1][0] * cy  + gMt[1][1] * 0 + gMt[1][2] * sy,
                                gMt[1][0] * 0   + gMt[1][1] * 1 + gMt[1][2] * 0,
                                gMt[1][0] * -sy + gMt[1][1] * 0 + gMt[1][2] * cy},
                               {gMt[2][0] * cy  + gMt[2][1] * 0 + gMt[2][2] * sy,
                                gMt[2][0] * 0   + gMt[2][1] * 1 + gMt[2][2] * 0,
                                gMt[2][0] * -sy + gMt[2][1] * 0 + gMt[2][2] * cy}};
        task->gMatrix = Mat(3, 3, CV_32F, gMatrix).clone();
        task->setTrueText("task->gMatrix is set...", dst2);
    }
};







class CPP_IMU_GMatrix : public algorithmCPP
{
private:
public:
    string strOut;
    CPP_IMU_GMatrix_QT * qt;
    CPP_IMU_GMatrix(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_IMU_GMatrix";
        qt = new CPP_IMU_GMatrix_QT(rows, cols);
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
        task->setTrueText("task->gMatrix is set...", dst2);
    }
};







class CPP_Depth_PointCloud_IMU : public algorithmCPP
{
private:
public:
    float option_resizeFactor = 1;
    CPP_IMU_GMatrix_QT* gMatrix;
    CPP_Depth_PointCloud* cloud;
    CPP_Depth_PointCloud_IMU(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Depth_PointCloud_IMU";
        gMatrix = new CPP_IMU_GMatrix_QT(rows, cols);
        cloud = new CPP_Depth_PointCloud(rows, cols);
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
        task->setTrueText("gCloud is ready for use", dst3);

        cloud->Run(task->gCloud);
        dst2 = cloud->dst2;
    }
};




class CPP_Edge_Sobel : public algorithmCPP {
public:
    bool horizontalDerivative = true;
    bool verticalDerivative = true;
    int kernelSize = 3;
    CPP_AddWeighted_Basics* addw;
    CPP_Edge_Sobel(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Edge_Sobel";
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




class CPP_Binarize_Simple : public algorithmCPP {
public:
    Scalar meanScalar;
    Mat mask;
    int injectVal = 255;

    CPP_Binarize_Simple(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Binarize_Simple";
        mask = Mat::ones(dst2.size(), CV_8U) * 255;
        desc = "Binarize an image using Threshold with OTSU.";
    }

    void Run(Mat src) {
        if (src.channels() == 3) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }

        if (mask.empty()) {
            meanScalar = mean(src);
        }
        else {
            Mat tmp = Mat::ones(src.size(), CV_8U) * 255;
            src.copyTo(tmp, mask);
            meanScalar = mean(tmp, mask);
        }

        threshold(src, dst2, meanScalar[0], injectVal, THRESH_BINARY);
    }
};





class CPP_Plot_Histogram : public algorithmCPP {
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

    CPP_Plot_Histogram(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Plot_Histogram";
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





class CPP_Histogram_Basics : public algorithmCPP 
{
public:
    Mat histogram;
    mmData mm;
    CPP_Plot_Histogram* plot;
    vector<Range> ranges;
    int splitIndex = 0;
    CPP_Histogram_Basics(int rows, int cols) : algorithmCPP(rows, cols) 
    {
        plot = new CPP_Plot_Histogram(rows, cols);
        traceName = "CPP_Histogram_Basics";
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
                    to_string(task->histogramBins) + ", X ranges from " + to_string(mm.minVal) +
                    " to " + to_string(mm.maxVal) + ", y is sample count";
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



class CPP_Kalman_Basics : public algorithmCPP {
public:
    vector<Kalman_Simple> kalman;
    vector<float> kInput = { 0, 0, 0, 0 };
    vector<float> kOutput;
    int saveDimension = -1;

    CPP_Kalman_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Kalman_Basics";
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

        if (task->useKalman) {
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
                Rect r = task->initRandomRect(src.rows <= 240 ? 20 : 50, dst2.cols);
                kInput = { float(r.x), float(r.y), float(r.width), float(r.height) };
            }
            lastRect = rect;
            rectangle(dst2, rect, Scalar(255, 255, 255), task->lineWidth + 1);
            rectangle(dst2, rect, Scalar(0, 0, 255), task->lineWidth);
        }
    }
};





class CPP_Histogram_Kalman : public algorithmCPP {
public:
    CPP_Histogram_Basics* hist;
    CPP_Kalman_Basics* kalman;

    CPP_Histogram_Kalman(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Histogram_Kalman";
        kalman = new CPP_Kalman_Basics(rows, cols);
        hist = new CPP_Histogram_Basics(rows, cols);
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





class CPP_BackProject_Basics : public algorithmCPP {
public:
    CPP_Histogram_Kalman* histK;
    Scalar minRange, maxRange;

    CPP_BackProject_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        histK = new CPP_Histogram_Kalman(rows, cols);
        traceName = "CPP_BackProject_Basics";
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

            task->setTrueText("The input image is empty - mm.minVal and mm.maxVal are both zero...", dst2);
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




class CPP_Rectangle_Basics : public algorithmCPP {
public:
    vector<Rect> rectangles;
    vector<RotatedRect> rotatedRectangles;
    int options_DrawCount = 3;
    bool options_drawFilled = false;
    bool options_drawRotated = false;

    CPP_Rectangle_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Rectangle_Basics";
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
                    task->drawRotatedRectangle(rr, dst2, nextColor);
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




class CPP_Rectangle_Rotated : public algorithmCPP {
public:
    CPP_Rectangle_Basics* rectangle;

    CPP_Rectangle_Rotated(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Rectangle_Rotated";
        rectangle = new CPP_Rectangle_Basics(rows, cols);
        rectangle->options_drawRotated = true;
        desc = "Draw the requested number of rectangles.";
    }

    void Run(Mat src) {
        rectangle->Run(src);
        dst2 = rectangle->dst2;
    }
};




class CPP_Contour_Largest : public algorithmCPP {
public:
    vector<Point> bestContour;
    vector<vector<Point>> allContours;
    RetrievalModes options_retrievalMode = RetrievalModes::RETR_LIST;
    ContourApproximationModes options_approximationMode = ContourApproximationModes::CHAIN_APPROX_NONE;
    int maxIndex;
    CPP_Rectangle_Rotated* rotatedRect;

    CPP_Contour_Largest(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Contour_Largest";
        rotatedRect = new CPP_Rectangle_Rotated(rows, cols);
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




class CPP_Diff_Basics : public algorithmCPP {
public:
    int changedPixels;
    Mat lastGray;

    CPP_Diff_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Diff_Basics";
        labels = {"", "", "Stable gray", "Unstable mask"};
        desc = "Capture an image and compare it to previous frame using absDiff and threshold";
    }

    void Run(Mat src) override {
        if (src.channels() == 3) cvtColor(src, src, COLOR_BGR2GRAY);
        if (task->firstPass) lastGray = src.clone();
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




class CPP_ApproxPoly_FindandDraw : public algorithmCPP {
public:
    CPP_Rectangle_Rotated* rotatedRect;
    vector<vector<Point>> allContours;
    CPP_ApproxPoly_FindandDraw(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_ApproxPoly_FindandDraw";
        rotatedRect = new CPP_Rectangle_Rotated(rows, cols);
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




class CPP_ApproxPoly_Basics : public algorithmCPP {
public:
    CPP_Contour_Largest* contour;
    CPP_Rectangle_Rotated* rotatedRect;
    CPP_ApproxPoly_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_ApproxPoly_Basics";
        contour = new CPP_Contour_Largest(rows, cols);
        rotatedRect = new CPP_Rectangle_Rotated(rows, cols);
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
        if (!contour->allContours.empty()) {
            vector<Point> nextContour;
            approxPolyDP(contour->bestContour, nextContour, epsilon, closedPoly);
            dst3 = Mat::zeros(dst2.size(), CV_8UC1);
            task->drawContour(dst3, nextContour, Scalar(0, 255, 255));
        }
        else {
            task->setTrueText("No contours found", dst2);
        }
    }
};




class CPP_Hull_Basics : public algorithmCPP {
public:
    CPP_Random_Basics* random;
    vector<Point2f> inputPoints;
    vector<Point> hull;
    bool useRandomPoints;
    CPP_Hull_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Hull_Basics";
        random = new CPP_Random_Basics(rows, cols);
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
        task->drawContour(dst2, hull, Scalar(0, 255, 255));
    }
};





class CPP_ApproxPoly_Hull : public algorithmCPP {
public:
    CPP_Hull_Basics* hull;
    CPP_ApproxPoly_Basics* aPoly;
    CPP_ApproxPoly_Hull(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_ApproxPoly_Hull";
        hull = new CPP_Hull_Basics(rows, cols);
        aPoly = new CPP_ApproxPoly_Basics(rows, cols);
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





class CPP_RedCloud_Flood : public algorithmCPP
{
private:
public:
    Mat inputMask;
    CPP_RedCloud_Core* prepData;
    int option_loDiff = 0;
    int option_hiDiff = 0;
    int option_minSizeCell = 75;
    int options_historyMax = 10;
    bool options_highlightCell;
    int totalCount;
    vector<Rect>rects;
    vector<int> sizes;
    CPP_RedCloud_Flood(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_RedCloud_Flood";
        prepData = new CPP_RedCloud_Core(rows, cols);
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

        if (inputMask.rows == 0) dst2 = task->noDepthMask; else dst2 = inputMask;
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






class CPP_Motion_Core : public algorithmCPP {
public:
    CPP_Diff_Basics* diff;
    int cumulativePixels;
    float options_cumulativePercentThreshold = 0.1f;
    int options_motionThreshold;
    int saveFrameCount;
    CPP_Motion_Core(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Motion_Core";
        task->pixelDiffThreshold = 25;
        options_motionThreshold = rows * cols / 16;
        diff = new CPP_Diff_Basics(rows, cols);
        dst3 = Mat::zeros(dst3.size(), CV_8U);
        labels[3] = "Accumulated changed pixels from the last heartbeat";
        desc = "Accumulate differences from the previous BGR image.";
    }
    void Run(Mat src) override {
        diff->Run(src);
        dst2 = diff->dst3;
        if (task->heartBeat) cumulativePixels = 0;
        if (diff->changedPixels > 0 || task->heartBeat) {
            cumulativePixels += diff->changedPixels;
            task->motionReset =
                (double)cumulativePixels / src.total() > options_cumulativePercentThreshold ||
                diff->changedPixels > options_motionThreshold || task->optionsChanged;
            if (task->motionReset || task->heartBeat) {
                dst2.copyTo(dst3);
                cumulativePixels = 0;
                saveFrameCount = task->frameCount;
            }
            else {
                dst3.setTo(255, dst2);
            }
        }
        int threshold = src.total() * options_cumulativePercentThreshold;
        string strOut = "Cumulative threshold = " + to_string(threshold / 1000) + "k ";
        labels[2] = strOut + "Current cumulative pixels changed = " + to_string(cumulativePixels / 1000) + "k";
    }
};




class CPP_History_Basics : public algorithmCPP {
public:
    vector<Mat> saveFrames;
    CPP_History_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_History_Basics";
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





class CPP_Motion_Basics : public algorithmCPP {
public:
    CPP_Motion_Core* motionCore;
    CPP_History_Basics* sum8u;
    CPP_Motion_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Motion_Basics";
        motionCore = new CPP_Motion_Core(rows, cols);
        sum8u = new CPP_History_Basics(rows, cols);
        task->frameHistoryCount = 10;
        desc = "Accumulate differences from the previous BGR images.";
    }
    void Run(Mat src) override {
        motionCore->Run(src);
        dst2 = motionCore->dst2;
        sum8u->Run(dst2);
        dst3 = sum8u->dst2;
    }
};





// https://docs.opencv.org/4.x/da/d22/tutorial_py_canny.html
class CPP_Edge_Canny : public algorithmCPP {
public:
    CPP_Edge_Canny(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Edge_Canny";
        desc = "Show canny edge detection with varying thresholds";
    }

    void Run(Mat src) {
        int threshold1 = 50;
        int threshold2 = 50;
        int aperture = 3;

        if (src.channels() == 3) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }

        if (src.depth() != CV_8U) {
            src.convertTo(src, CV_8U);
        }

        Canny(src, dst2, threshold1, threshold2, aperture, true);
    }
};





class CPP_Edge_MotionAccum : public algorithmCPP {
public:
    CPP_Edge_Canny* edges;
    CPP_Motion_Basics* motion;
    float percentMotion;
    CPP_Edge_MotionAccum(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Edge_MotionAccum";
        edges = new CPP_Edge_Canny(rows, cols);
        motion = new CPP_Motion_Basics(rows, cols);
        dst2 = Mat::zeros(dst2.size(), CV_8U);
        labels = { "", "", "Accumulated edges tempered by motion thresholds", "" };
        desc = "Accumulate edges and use motion to clear";
    }
    void Run(Mat src) override {
        if (task->optionsChanged) task->motionReset = true;
        motion->Run(src);
        if (task->frameCount % task->frameHistoryCount == 0) dst2.setTo(0);
        edges->Run(src);
        dst2.setTo(255, edges->dst2);
        labels[3] = motion->labels[2];
    }
};






class CPP_Edge_MotionFrames : public algorithmCPP {
public:
    CPP_Edge_Canny* edges;
    CPP_History_Basics* frames;
    CPP_Edge_MotionFrames(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Edge_MotionFrames";
        edges = new CPP_Edge_Canny(rows, cols);
        frames = new CPP_History_Basics(rows, cols);
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





class CPP_Edge_Preserving : public algorithmCPP {
public:
    int sigma_s = 10;
    double sigma_r = 40;
    CPP_Edge_Preserving(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Edge_Preserving";
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






class CPP_Resize_Preserve : public algorithmCPP {
public:
    int options_resizePercent = 120;
    int options_topLeftOffset = 10;
    InterpolationFlags options_warpFlag = INTER_NEAREST; 
    Size newSize;
    CPP_Resize_Preserve(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Resize_Preserve";
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




class CPP_Convex_Basics : public algorithmCPP {
public: 
    vector<Point> hull;
    CPP_Convex_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Convex_Basics";
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
            task->setTrueText("No points were provided. Update hullList before running.", dst2);
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




class CPP_Distance_Basics : public algorithmCPP {
public:
    DistanceTypes options_distanceType = DIST_L1;
    int options_kernelSize = 0;
    CPP_Distance_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Distance_Basics";
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




class CPP_Line_Basics : public algorithmCPP {
public: 
    Ptr<ximgproc::FastLineDetector> ld;
    map<float, int> sortLength;
    vector<linePoints> mpList;
    vector<Point2f> ptList;
    Rect subsetRect;
    int options_lineLengthThreshold = 20;
    // vector<tCell> tCells;
    CPP_Line_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        subsetRect = Rect(0, 0, dst2.cols, dst2.rows);
        traceName = "CPP_Line_Basics";
        dst3 = Mat::zeros(dst3.size(), CV_8U);
        ld = ximgproc::createFastLineDetector();
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present.";
    }
    void Run(Mat src) override {
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
        mpList.clear();
        ptList.clear();
       //  tCells.clear();
        for (const Vec4f& v : lines) {
            int x1 = v[0] + subsetRect.x, y1 = v[1] + subsetRect.y;
            int x2 = v[2] + subsetRect.x, y2 = v[3] + subsetRect.y;
            if (0 <= x1 && x1 < dst2.cols && 0 <= y1 && y1 < dst2.rows &&
                0 <= x2 && x2 < dst2.cols && 0 <= y2 && y2 < dst2.rows) {
                Point p1(x1, y1), p2(x2, y2);
                if (norm(p1 - p2) >= options_lineLengthThreshold) {
                    linePoints mps(p1, p2);
                    mpList.push_back(mps);
                    ptList.push_back(p1);
                    ptList.push_back(p2);
                    sortLength[norm(p1 - p2)] = int(mpList.size()) - 1;
                    //tCells.push_back({ p1 });
                    //tCells.push_back({ p2 });
                }
            }
        }
        dst2 = src.clone();
        dst3.setTo(0);
        for (const auto& nextLine : sortLength) {
            const linePoints& mps = mpList[nextLine.second];
            line(dst2, mps.p1, mps.p2, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            line(dst3, mps.p1, mps.p2, 255, task->lineWidth, task->lineType);
        }
        labels[2] = to_string(mpList.size()) + " lines were detected in the current frame";
    }
};






class CPP_Edge_Segments : public algorithmCPP
{
private:
public:
    Ptr<EdgeDrawing> ed;
    CPP_Edge_Segments(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_Edge_Segments";
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




//class CPP_EdgeDraw_Basics : public algorithmCPP
//{
//private:
//public:
//    CPP_Edge_Segments* eDraw;
//    CPP_EdgeDraw_Basics(int rows, int cols) : algorithmCPP(rows, cols)
//    {
//        eDraw = new CPP_Edge_Segments(rows, cols);
//        traceName = "CPP_EdgeDraw_Basics";
//        dst3 = Mat(dst3.size(), CV_8U);
//        labels[2] = "Line segments of the Edge Drawing Filter - drawn on the src input";
//        labels[3] = "Line segments of the Edge Drawing Filter";
//        desc = "Display segments found with the EdgeDrawing filter in ximgproc";
//    }
//    void Run(Mat src)
//    {
//        dst3 = src;
//        eDraw->Run(src);
//        vector<vector<Point> > segments = eDraw->ed->getSegments();
//
//        dst2.setTo(0);
//        for (size_t i = 0; i < segments.size(); i++)
//        {
//            const Point* pts = &segments[i][0];
//            int n = (int)segments[i].size();
//            float distance = sqrt((pts[0].x - pts[n - 1].x) * (pts[0].x - pts[n - 1].x) + (pts[0].y - pts[n - 1].y) * (pts[0].y - pts[n - 1].y));
//            bool drawClosed = distance < 10;
//            polylines(dst2, &pts, &n, 1, drawClosed, Scalar(255, 255, 255), task->lineWidth, task->lineType);
//            polylines(dst3, &pts, &n, 1, drawClosed, YELLOW, task->lineWidth, task->lineType);
//        }
//
//        rectangle(dst2, Rect(0, 0, dst3.cols, dst3.rows), 255, task->lineWidth, task->lineType);
//        threshold(dst2, dst2, 0, 255, THRESH_BINARY);
//    }
//};



class CPP_EdgeDraw_Basics : public algorithmCPP {
public:
    EdgeDraw* cPtr;
    CPP_EdgeDraw_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_EdgeDraw_Basics";
        cPtr = new EdgeDraw;
        labels = { "", "", "EdgeDraw_Basics output", "" };
        desc = "Access EdgeDraw directly for efficiency";
    }
    void Run(Mat src) {
        if (src.channels() != 1) {
            cvtColor(src, src, COLOR_BGR2GRAY);
        }
        Mat* srcPtr = &src;
        int* imagePtr = EdgeDraw_RunCPP(cPtr, (int *)srcPtr->data, src.rows, src.cols, task->lineWidth);
        if (imagePtr != nullptr) {
            dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr);
            rectangle(dst2, Rect(0, 0, dst2.cols, dst2.rows), Scalar(255), task->lineWidth);
        }
    }
    ~CPP_EdgeDraw_Basics() {
        EdgeDraw_Edges_Close(cPtr);
        delete cPtr;
    }
};






class CPP_FeatureLess_Basics : public algorithmCPP {
public:
    CPP_EdgeDraw_Basics* edgeD;
    CPP_FeatureLess_Basics(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_FeatureLess_Basics";
        edgeD = new CPP_EdgeDraw_Basics(rows, cols);
        desc = "Access the EdgeDraw_Basics algorithm directly rather than through the CPP_Basics interface - more efficient";
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



class CPP_FeatureLess_Edge : public algorithmCPP
{
private:
public:
    CPP_Distance_Basics* dist;
    CPP_EdgeDraw_Basics* eDraw;
    CPP_FeatureLess_Edge(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_FeatureLess_Edge";
        eDraw = new CPP_EdgeDraw_Basics(rows, cols);
        dist = new CPP_Distance_Basics(rows, cols);
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








class CPP_Bezier_Basics : public algorithmCPP {
public:
    vector<Point> points;

    CPP_Bezier_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Bezier_Basics";
        advice = "Update the public points array and then Run.";
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
                    line(dst2, p1, p2, task->highlightColor, task->lineWidth);
                }
                p1 = p2;
            }
        }
        labels[2] = "Bezier output";
    }
};




class CPP_FeatureLess_History : public algorithmCPP {
public:
    CPP_EdgeDraw_Basics* edgeD;
    CPP_History_Basics* frames;
    CPP_FeatureLess_History(int rows, int cols) : algorithmCPP(rows, cols) 
    {
        dst2 = Mat::zeros(dst2.size(), CV_8U); 
        traceName = "CPP_FeatureLess_History";
        edgeD = new CPP_EdgeDraw_Basics(rows, cols);
        frames = new CPP_History_Basics(rows, cols);
        desc = "Access the EdgeDraw_Basics algorithm directly rather than through the CPP_Basics interface - more efficient";
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





class CPP_Palette_Basics : public algorithmCPP {
public:
    bool whitebackground;
    int paletteIndex = 8;
    CPP_Palette_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Palette_Basics";
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
        ColormapTypes mapIndex = colormapTypes[paletteIndex];
        applyColorMap(src, dst2, mapIndex);
    }
};


vector<Point> contourBuild(const Mat& mask, int approxMode) {
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
    static CPP_Palette_Basics* palette = new CPP_Palette_Basics(input.rows, input.cols);
    if (input.type() == CV_8U) palette->Run(input);
    return palette->dst2;
}




class CPP_RedMin_Core : public algorithmCPP {
public:
    map<int, segCell, compareAllowIdenticalIntegerInverted> sortedCells;
    Mat inputMask;
    CPP_FeatureLess_Basics* fLess;
    FloodCell* cPtr;
    float redOptions_imageThresholdPercent = 0.95f;
    int redOptions_DesiredCellSlider = 30;
    CPP_RedMin_Core(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_RedMin_Core";
        vbPalette(dst2);
        task->paletteIndex = 1;
        fLess = new CPP_FeatureLess_Basics(rows, cols);
        cPtr = new FloodCell();
        desc = "Another minimalist approach to building RedCloud color-based cells.";
    }
    ~CPP_RedMin_Core() {

        if (cPtr) { 
            FloodCell_Close(cPtr);
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
        if (inputMask.empty()) {
            imagePtr = FloodCell_Run(cPtr, (int *)srcPtr->data, 0, src.rows, src.cols, src.type(), 
                                     redOptions_DesiredCellSlider, 0);
        }
        else {
            Mat* maskPtr = &inputMask;
            imagePtr = FloodCell_Run(cPtr, (int *)srcPtr->data, (uchar *)maskPtr->data, src.rows, src.cols,
                                     src.type(), redOptions_DesiredCellSlider, 0);
        }
        int classCount = FloodCell_Count(cPtr);

        dst2 = Mat(src.rows, src.cols, CV_8UC1, imagePtr);
        dst3 = vbPalette(dst2 * 255 / classCount);

        if (task->heartBeat) {
            labels[3] = to_string(classCount) + " cells found";
        }

        if (classCount <= 1) {
            return;
        }
        Mat sizeData(classCount, 1, CV_32SC1, FloodCell_Sizes(cPtr));
        Mat rectData(classCount, 1, CV_32SC4, FloodCell_Rects(cPtr));
        Mat floodPointData(classCount, 1, CV_32SC2, FloodCell_FloodPoints(cPtr));
        sortedCells.clear();

        for (int i = 0; i < classCount; i++) {
            segCell cell;
            cell.index = int(sortedCells.size()) + 1;
            cell.rect = task->validateRect(rectData.at<cv::Rect>(i, 0), dst2.cols, dst2.rows);
            inRange(dst2(cell.rect), cell.index, cell.index, cell.mask);
            //vector<Point> contour = contourBuild(cell.mask, cv::CHAIN_APPROX_NONE); 
            //drawContours(cell.mask, vector<vector<Point>> {contour}, 255, -1);

            cell.pixels = sizeData.at<int>(i, 0);
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









class CPP_RedMin_Basics : public algorithmCPP {
public:
    CPP_RedMin_Core* minCore;
    vector<segCell> minCells;
    Mat lastColors;
    Mat lastMap = dst2.clone();
    CPP_RedMin_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_RedMin_Basics";
        minCore = new CPP_RedMin_Core(rows, cols);
        dst2 = Mat::zeros(dst2.size(), CV_8U);  
        labels = { "", "Mask of active RedMin cells", "CV_8U representation of minCells", "" };
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells.";
    }
    void Run(Mat src) {
        minCore->Run(src);
        vector<segCell> lastCells = minCells;
        if (task->firstPass) lastColors = dst3.clone();
        minCells.clear();
        dst2.setTo(0);
        dst3.setTo(0);
        vector<Vec3b> usedColors = { Vec3b(0, 0, 0) };
        for (const auto ele : minCore->sortedCells) {
            segCell cell = ele.second;
            uchar index = lastMap.at<uchar>(cell.maxDist.y, cell.maxDist.x);
            if (index > 0 && index < lastCells.size()) {
                cell.color = lastColors.at<Vec3b>(cell.maxDist.y, cell.maxDist.x);
            }
            if (find(usedColors.begin(), usedColors.end(), cell.color) != usedColors.end()) {
                cell.color = task->randomCellColor();
            }
            usedColors.push_back(cell.color);
            if (dst2.at<uchar>(cell.maxDist.y, cell.maxDist.x) == 0) {
                cell.index = int(minCells.size()) + 1;
                minCells.push_back(cell);
                dst2(cell.rect).setTo(cell.index, cell.mask);
                dst3(cell.rect).setTo(cell.color, cell.mask);
                //task->setTrueText(to_string(cell.index), cell.maxDist, 2);
                //task->setTrueText(to_string(cell.index), cell.maxDist, 3);
            }
        }
        labels[3] = to_string(minCells.size()) + " cells were identified.";
        task->cellSelect = segCell();
        if (task->clickPoint == Point(0, 0)) {
            if (minCells.size() > 2) {
                task->clickPoint = minCells[0].maxDist;
                task->cellSelect = minCells[0];
            }
        }
        else {
            uchar index = dst2.at<uchar>(task->clickPoint.y, task->clickPoint.x);
            if (index != 0) task->cellSelect = minCells[index - 1];
        }
        lastColors = dst3.clone();
        lastMap = dst2.clone();
        if (minCells.size() > 0) dst1 = vbPalette(lastMap * 255 / minCells.size());
    }
};

