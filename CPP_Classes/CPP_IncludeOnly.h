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

using namespace std;
using namespace cv;
using namespace ximgproc;
using namespace ml;

#include "../CPP_Classes/PragmaLibs.h"

struct rcData {
    Rect rect;
    Mat mask;
    int pixels;

    Scalar depthMean;
    Scalar depthStdev;
    double depthMin;
    Point depthMinLoc;
    double depthMax;
    Point depthMaxLoc;

    Point maxDist;

    int index;
    int indexLast;
    int indexCurr;// lastCells will link to the redCells with this link.

    Vec3b color;
    vector<Point> contour;
    vector<Point> hull;

    Vec4f eq;

    Point2f center;
};


struct mmData
{
    double minVal;
    double maxVal;
    Point minLoc;
    Point maxLoc;
};

// https://stackoverflow.com/questions/1380463/sorting-a-vector-of-custom-objects
struct sortInt
{
    int key;
    int value;

    sortInt(int key, int value) : key(key), value(value) {}

    // these allows you to sort the struct by the key with sort(vec.begin(), vec.end());
    bool operator < (const sortInt& keyVal)
    {
        return (key < keyVal.key);
    }
    bool operator > (const sortInt& keyVal)
    {
        return (key > keyVal.key);
    }
};

class linePoints {
public:
    Point2f p1;
    Point2f p2;
    float slope;
    float yIntercept;

    linePoints(const Point2f& _p1, const Point2f& _p2) : p1(_p1), p2(_p2) {
        float verticalSlope = 100000;
        slope = (p1.x != p2.x) ? (p1.y - p2.y) / (p1.x - p2.x) : verticalSlope;
        yIntercept = p1.y - slope * p1.x;
    }

    linePoints() :
        p1(Point2f()), p2(Point2f()) {}

    bool compare(const linePoints& mp) const {
        return mp.p1.x == p1.x && mp.p1.y == p1.y && mp.p2.x == p2.x && mp.p2.y == p2.y;
    }
};

#define LINE_WIDTH 1
#define WHITE Scalar(255, 255, 255)
#define BLUE Scalar(255, 0, 0)
#define RED Scalar(0, 0, 255)
#define GREEN Scalar(0, 255, 0)
#define YELLOW Scalar(0, 255, 255)
#define BLACK Scalar(0, 0, 0)
#define GRAY Scalar(127, 127, 127)
vector<Scalar> highlightColors = { YELLOW, WHITE, BLUE, GRAY, RED, GREEN };

vector<string> mapNames = { "Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                            "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter" };
enum functions
{
    CPP_AddWeighted_Basics_,
CPP_RedCloud_Core_,
CPP_FPoly_TopFeatures_,
    CPP_Random_Enumerable_,
    CPP_Bezier_Basics_,
    CPP_Feature_Agast_,
    CPP_Resize_Basics_,
    CPP_Delaunay_Basics_,
    CPP_Delaunay_GenerationsNoKNN_,
    CPP_KNN_Basics_,
    CPP_Random_Basics_,
    CPP_KNN_Lossy_,
    CPP_Delaunay_Generations_,
    CPP_Stable_Basics_,
    CPP_Feature_Basics_,
    CPP_Remap_Basics_,
    CPP_Edge_Canny_,
    CPP_Edge_Sobel_,
    CPP_Edge_Scharr_,
    CPP_Mat_4to1_,
    CPP_Grid_Basics_,
    CPP_Depth_Colorizer_,
    CPP_RedCloud_Flood_,
    CPP_Depth_PointCloud_,
    CPP_IMU_GMatrix_,
    CPP_IMU_GMatrix_QT_,
    CPP_Depth_PointCloud_IMU_,
    CPP_Binarize_Simple_,
    CPP_Plot_Histogram_,
    CPP_Histogram_Basics_,
    CPP_BackProject_Basics_,
    CPP_Rectangle_Basics_,
    CPP_Rectangle_Rotated_,
    CPP_Contour_Largest_,
    CPP_Diff_Basics_,
    CPP_ApproxPoly_FindandDraw_,
    CPP_ApproxPoly_Basics_,
    CPP_Hull_Basics_,
    CPP_ApproxPoly_Hull_,
    CPP_Edge_Segments_,
    CPP_Motion_Basics_,
    CPP_Edge_MotionAccum_,
    CPP_Edge_MotionFrames_,
    CPP_EdgePreserving_Basics_,
    CPP_EdgeDraw_Basics_,
    CPP_TEE_Basics_,
    CPP_RedCloud_Hulls_,
    CPP_Distance_Basics_,
    CPP_FeatureLess_Basics_,
    CPP_FeatureLess_Edge_,
    CPP_RedCloud_FeatureLess2_,
    CPP_Stable_BasicsCount_,
MAX_FUNCTION = CPP_Stable_BasicsCount_,
};


class algorithmCPP
{
private:
public:
    Mat dst0, dst1, dst2, dst3, empty;
    bool standalone;
    String advice;
    String desc;
    string traceName;
    Vec3b black = Vec3b(0, 0, 0);
    vector<string> labels{ "", "", "", "" };
    algorithmCPP() {}
    algorithmCPP(int rows, int cols)
    {
        dst0 = Mat(rows, cols, CV_8UC3);
        dst0.setTo(0);
        dst1 = Mat(rows, cols, CV_8UC3);
        dst1.setTo(0);
        dst2 = Mat(rows, cols, CV_8UC3);
        dst2.setTo(0);
        dst3 = Mat(rows, cols, CV_8UC3);
        dst3.setTo(0);
    };

    virtual void Run(Mat src) = 0;  // forces the child class to have a void Run function has the same paramters.
};






class cppTask
{
private:
public:
    algorithmCPP* alg;
    Mat color, depthRGB, depth32f, pointCloud, gCloud, leftView, rightView; 
    int cppFunction; int lineWidth; int lineType; 
    int gridRows, gridCols;
    Mat depthMask, noDepthMask, gridMask, maxDepthMask;
    int font; 
    float fontSize; 
    Scalar fontColor;
    int frameCount;  Point3f accRadians; vector<Rect> roiList;

    bool heartBeat; bool debugCheckBox; Size minRes; int PCReduction;
    bool optionsChanged; double addWeighted; int dotSize; int gridSize; float maxZmeters;
    int histogramBins; int pixelDiffThreshold; bool gravityPointCloud; bool useKalman;
    int paletteIndex; int polyCount; bool firstPass; Scalar highlightColor; int frameHistory;
    Point clickPoint; bool mouseClickFlag; int mousePicTag; Point mouseMovePoint; bool mouseMovePointUpdated;
    Scalar scalarColors[256]; Vec3b vecColors[256]; Rect drawRect; bool displayDst0; bool displayDst1;
    bool gridROIclicked;
    Mat gridToRoiIndex;
    vector<Rect> gridList;
    vector<vector<int>> gridNeighbors;

    Mat gMatrix; vector<Mat> pcSplit;
    bool paused = false;
    Mat xdst0, xdst1, xdst2, xdst3;
    cppTask(int rows, int cols)
    {
        cppFunction = -1;
        firstPass = true;

        polyCount = 10; // use FPoly_TopFeatures and the slider to double-check this value.  It seems pretty good.
        buildColors();
    };
    void buildColors()
    {
        srand(time(0));
        for (int i = 0; i < 256; i++)
        {
            vecColors[i] = Vec3b(rand() % 256, rand() % 256, rand() % 256);
            scalarColors[i] = Scalar(vecColors[i].val[0], vecColors[i].val[1], vecColors[i].val[2]);
        }
    }
    void drawContour(Mat dst, vector<Point> contour, Scalar color, int lineWidth = -10)
    {
        if (lineWidth == -10) lineWidth = this->lineWidth;
        if (contour.size() < 3) return;
        vector<vector<Point>> pointList;
        pointList.push_back(contour);
        drawContours(dst, pointList, -1, color, lineWidth, this->lineType);
    }
    vector<Point> convert2f2i(Mat hull2f)
    {
        Mat hull2i = Mat(hull2f.rows, 1, CV_32SC2);
        hull2f.convertTo(hull2i, CV_32SC2);
        vector<Point> vec;
        vec.assign((Point *)hull2i.data, (Point *)hull2i.data + hull2i.total());
        return vec;
    }
    void setTrueText(String text, Mat dst, Point2f pt = Point2f(10, 50))
    {
        if (cppFunction < 0) return;
        putText(dst, text, pt, this->font, this->fontSize, this->fontColor);
    }
    mmData getMinMax(Mat mat, Mat mask = Mat())
    {
        mmData mm;
        if (mask.rows == 0)
        {
            minMaxLoc(mat, &mm.minVal, &mm.maxVal, &mm.minLoc, &mm.maxLoc);
        } else {
            minMaxLoc(mat, &mm.minVal, &mm.maxVal, &mm.minLoc, &mm.maxLoc, mask);
        }
        return mm;
    }
    Vec3b randomCellColor() {
        static random_device rd;
        static mt19937 gen(rd());  // Mersenne Twister engine for randomness
        uniform_int_distribution<> dist(50, 240);

        // Generate three random values between 50 and 240 (inclusive)
        // using uniform_int_distribution for more control over range
        return Vec3b(dist(gen), dist(gen), dist(gen));
    }
    void AddPlotScale(Mat dst, double minVal, double maxVal, int lineCount) {
        auto spacer = int(dst.rows / (lineCount + 1));
        auto spaceVal = int((maxVal - minVal) / (lineCount + 1));
        if (spaceVal < 1) spaceVal = 1;
        if (spaceVal > 10) spaceVal += spaceVal % 10;
        string strOut = "";
        for (auto i = 0; i <= lineCount; i++) {
            auto p1 = Point(0, spacer * i);
            auto p2 = Point(dst.cols, spacer * i);
            line(dst, p1, p2, WHITE, 1);
            if (i == 0) p1.y += 10;
            auto nextVal = (maxVal - spaceVal * i);
            if(maxVal > 1000)
                strOut = to_string(int(nextVal / 1000)) + "k";
            else
                strOut = to_string(int(nextVal));
            setTrueText(strOut, dst, p1);
        }
    }
    Mat normalize32f(Mat input)
    {
        Mat outMat;
        normalize(input, outMat, 255, 0, NormTypes::NORM_MINMAX);
        outMat.convertTo(outMat, CV_8U);
        cvtColor(outMat, outMat, COLOR_GRAY2BGR);
        return outMat;
    }
    Point2f getMaxDist(Mat mask, Rect rect)
    {
        Mat tmpMask = mask.clone();
        rectangle(tmpMask, Rect(0, 0, mask.cols - 1, mask.rows - 1), 0, 1);
        Mat distance32f;
        distanceTransform(tmpMask, distance32f, DIST_L1, 3);
        double minVal, maxVal;
        Point minDistanceLoc, maxDistanceLoc;
        minMaxLoc(distance32f, &minVal, &maxVal, &minDistanceLoc, &maxDistanceLoc);
        maxDistanceLoc.x += rect.x;
        maxDistanceLoc.y += rect.y;
        return maxDistanceLoc;
    }
    float shapeCorrelation(vector<Point> points)
    {
        Mat pts = Mat(int(points.size()), 1, CV_32SC2, points.data());
        Mat pts32f;
        pts.convertTo(pts32f, CV_32FC2);
        vector<Mat> splitMat;
        split(pts32f, splitMat);
        Mat correlationMat;
        matchTemplate(splitMat[0], splitMat[1], correlationMat, TemplateMatchModes::TM_CCOEFF_NORMED);
        return correlationMat.at<float>(0, 0);
    }
    Rect validateRect(Rect r, int w, int h)
    {
        if (r.width <= 0) r.width = 1;
        if (r.height <= 0) r.height = 1;
        if (r.x < 0) r.x = 0;
        if (r.y < 0) r.y = 0;
        if (r.x > w) r.x = w - 1;
        if (r.y > h) r.y = h - 1;
        if (r.x + r.width >= w) r.width = w - r.x - 1;
        if (r.y + r.height >= h) r.height = h - r.y - 1;
        if (r.width <= 0) r.width = 1;
        if (r.height <= 0) r.height = 1;
        if (r.x == w) r.x = r.x - 1;
        if (r.y == h) r.y = r.y - 1;
        return r;
    }

    void DrawRotatedRectangle(Mat& image, Point centerPoint, Size rectangleSize, double rotationDegrees, 
                              Scalar color)
    {
        // Create the rotated rectangle
        RotatedRect rotatedRectangle(centerPoint, rectangleSize, rotationDegrees);

        // We take the edges that OpenCV calculated for us
        Point2f vertices2f[4];
        rotatedRectangle.points(vertices2f);

        // Convert them so we can use them in a fillConvexPoly
        Point vertices[4];
        for (int i = 0; i < 4; ++i) {
            vertices[i] = vertices2f[i];
        }

        // Now we can fill the rotated rectangle with our specified color
        fillConvexPoly(image, vertices, 4, color, lineType);
    }
};



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






class CPP_Plot_Histogram : public algorithmCPP
{
private:
public:
    Mat hist;
    float minRange = 0;
    float maxRange = 255;
    Scalar backColor = RED;
    int plotMaxValue;
    int plotCenter;
    int barWidth;
    bool addLabels = true;
    int labelImage = 2;
    Mat dst;
    CPP_Plot_Histogram(int rows, int cols) : algorithmCPP(rows, cols) {
        dst = dst2;
        desc = "Plot histogram data with a stable scale at the left of the image.";
    }
    void Run(Mat src) {
        if (standalone) {
            if (src.channels() != 1) cvtColor(src, src, COLOR_BGR2GRAY);
            int chan[] = { 0 };
            int bins[] = { task->histogramBins };
            float hRange[] = { minRange, maxRange };
            const float* range[] = { hRange };
            calcHist(&src, 1, chan, Mat(), hist, 1, bins, range, true, false);
        }
        else {
            hist = src;
        }
        dst.setTo(backColor);
        barWidth = dst.cols / hist.rows;
        plotCenter = barWidth * hist.rows / 2 + barWidth / 2;
        auto mm = task->getMinMax(hist);
        if (plotMaxValue > 0) mm.maxVal = plotMaxValue;
        if (mm.maxVal > 0 && hist.rows > 0) {
            auto incr = int(255 / hist.rows);
            // Review this For Loop >>>> i = 0 To hist.rows - 1
            for (auto i = 0; i < hist.rows; i++) {
                auto offset = hist.at<float>(i);
                if (isnan(offset)) offset = 0;
                auto h = int(offset * dst.rows / mm.maxVal);
                auto sIncr = int((i % 256) * incr);
                auto color = Scalar(sIncr, sIncr, sIncr);
                if (hist.rows > 255) color = BLACK;
                rectangle(dst, Rect(i * barWidth, dst.rows - h, barWidth, h), color, -1);
            }
            if (addLabels) task->AddPlotScale(dst, 0, mm.maxVal, 3);
        }

    }
};




class CPP_Histogram_Basics : public algorithmCPP
{
private:
public:
    Mat histogram;
    CPP_Plot_Histogram* plot;
    bool removeZeroEntry;
    double srcMin;
    double srcMax;
    Range ranges[1];
    int splitIndex;
    CPP_Histogram_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        plot = new CPP_Plot_Histogram(rows, cols);
        desc = "Create a raw histogram (no Kalman)";
    }
    void plotHistogram() {
        if (removeZeroEntry) histogram.at<float>(0, 0) = 0;
        plot->Run(histogram);
        dst2 = plot->dst2;
    }
    void Run(Mat src) {
        if (standalone) {
            if (task->heartBeat) {
                splitIndex += 1;
                splitIndex %= 3;
                switch (splitIndex)
                {
                case 0:
                {
                    plot->backColor = BLUE;
                    break;
                }
                case 1:
                {
                    plot->backColor = GREEN;
                    break;
                }
                case 2:
                {
                    plot->backColor = RED;
                    break;
                }
                }
            }
            Mat msplit[3];
            split(src, msplit);
            src = msplit[splitIndex];
        }
        if (src.channels() != 1) cvtColor(src, src, COLOR_BGR2GRAY);
        auto mm = task->getMinMax(src);
        srcMin = mm.minVal;
        srcMax = mm.maxVal;
        if (mm.minVal == mm.maxVal) {
            task->setTrueText("The input image is empty - srcMin and srcMax are both zero...", dst2);
            return;
        }
        int chan[] = { 0 };
        int bins[] = { task->histogramBins };
        float hRange[] = { float(srcMin), float(srcMax)};
        const float* range[] = { hRange };
        calcHist(&src, 1, chan, Mat(), histogram, 1, bins, range, true, false);
        plotHistogram();
        // 		labels[2] = Choose(splitIndex + 1, "Blue", "Green", "Red") + " histogram, bins = " + to_string(task->histogramBins) + ", X ranges from " + Format(mm.minVal, "0.0") + " to " + Format(mm.maxVal, "0.0") + ", y is occurances";// <<<<< build an array and index it.
    }
};









class CPP_BackProject_Basics : public algorithmCPP
{
private:
public:
    CPP_Histogram_Basics* hist;
    CPP_BackProject_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_BackProject_Basics";
        hist = new CPP_Histogram_Basics(rows, cols);
        labels[2] = "Move mouse to backproject a histogram column";
        dst1 = Mat(dst1.size(), CV_8U);
        desc = "Use the mouse to select what bin in the provided histogram should be backprojected.";
    }
    void Run(Mat src) {
        auto input = src.clone();
        if (input.channels() != 1) cvtColor(input, input, COLOR_BGR2GRAY);
        hist->Run(input);
        if (hist->srcMin == hist->srcMax) {
            task->setTrueText("The input image is empty - srcMin and srcMax are both zero...", dst2);
            return;
        }
        dst2 = hist->dst2;
        auto totalPixels = dst2.total();
        if (hist->removeZeroEntry) totalPixels = countNonZero(input);
        auto barWidth = dst2.cols / task->histogramBins;
        auto incr = (hist->srcMax - hist->srcMin) / task->histogramBins;
        int histIndex = task->mouseMovePoint.x / barWidth;
        auto minRange = Scalar(histIndex * incr);
        auto maxRange = Scalar((histIndex + 1) * incr);
        if (histIndex + 1 == task->histogramBins) maxRange = Scalar(255);
        Mat mask;
        float bRange[] = { float(minRange.val[0]), float(maxRange.val[0])};
        const float* ranges[] = { bRange };
        calcBackProject(&input, 1, 0, hist->histogram, mask, ranges, 1, true);
        auto actualCount = countNonZero(mask);
        dst3 = src;
        dst3.setTo(YELLOW, mask);
        auto count = hist->histogram.at<float>(int(histIndex), 0);
        mmData histMax = task->getMinMax(hist->histogram);
        labels[3] = "Backprojecting " + to_string(int(minRange.val[0])) + " to " + to_string(int(maxRange.val[0])) + " with " +
            to_string(count) + " of " + to_string(totalPixels) + " samples compared to " + " mask pixels = " + to_string(actualCount) +
            " Histogram max count = " + to_string(int(histMax.maxVal));
        rectangle(dst2, Rect(int(histIndex * barWidth), 0, barWidth, dst2.rows), YELLOW, task->lineWidth);
    }
};






class CPP_Rectangle_Basics : public algorithmCPP
{
private:
public:
    vector < Rect > rectangles;
    vector < RotatedRect > rotatedRectangles;
    bool options_drawRotated = false;
    bool options_drawFilled = false;
    int options_drawCount = 5;
    CPP_Rectangle_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Rectangle_Basics";
        desc = "Draw the requested number of rectangles.";
    }
    void Run(Mat src) {
        if (task->heartBeat) {
            dst2.setTo(BLACK);
            rectangles.clear();
            rotatedRectangles.clear();
            for (auto i = 0; i < options_drawCount; i++) {
                Point2f nPoint = Point2f(rand() % src.cols, rand() % src.rows);
                auto width = rand() % int(src.cols - nPoint.x);
                auto height = rand() % int(src.rows - nPoint.y);
                auto eSize = Size2f(float(rand() % src.cols - nPoint.x - 1), float(rand() % src.rows - nPoint.y - 1));
                auto angle = 180.0F * float(rand() % 1000) / 1000.0f;
                auto nextColor = Scalar(task->vecColors[i].val[0], task->vecColors[i].val[1], task->vecColors[i].val[2]);
                auto rr = RotatedRect(nPoint, eSize, angle);
                Rect r = Rect(nPoint.x, nPoint.y, width, height);
                if (options_drawRotated) {
                    task->DrawRotatedRectangle(dst2, nPoint, eSize, angle, 
                                               task->vecColors[i]);
                } else {
                    rectangle(dst2, r, nextColor, task->lineWidth);
                }
                rotatedRectangles.push_back(rr);
                rectangles.push_back(r);
            }
        }
    }
};





class CPP_Rectangle_Rotated : public algorithmCPP
{
private:
public:
    CPP_Rectangle_Basics* rRectangle;
    CPP_Rectangle_Rotated(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Rectangle_Rotated";
        rRectangle = new CPP_Rectangle_Basics(rows, cols);
        rRectangle->options_drawRotated = true;
        desc = "Draw the requested number of rectangles.";
    }
    void Run(Mat src) {
        rRectangle->Run(src);
        dst2 = rRectangle->dst2;
    }
};





class CPP_Contour_Largest : public algorithmCPP
{
private:
public:
    vector<Point> bestContour;
    vector<vector<Point>> allContours;
    int maxIndex;
    CPP_Rectangle_Rotated* rotatedRect;
    CPP_Contour_Largest(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Contour_Largest";
        labels = { "", "", "Input to FindContours", "Largest single contour in the input image." };
        rotatedRect = new CPP_Rectangle_Rotated(rows, cols);
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
        findContours(dst2, allContours, RetrievalModes::RETR_LIST, ContourApproximationModes::CHAIN_APPROX_NONE);
        auto maxCount = 0;
        maxIndex = -1;
        // Review this For Loop >>>> i = 0 To allContours.size() - 1
        for (auto i = 0; i < allContours.size(); i++) {
            auto len = int(allContours[i].size());
            if (len > maxCount) {
                maxCount = len;
                maxIndex = i;
            }
        }
        if (maxIndex >= 0 && maxCount >= 2) {
            dst3.setTo(0);
            drawContours(dst3, allContours, maxIndex, WHITE, -1, task->lineType);
            bestContour = allContours[maxIndex];
        }
    }
};






class CPP_Diff_Basics : public algorithmCPP
{
private:
public:
    int changedPixels;
    Mat lastGray;
    CPP_Diff_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Diff_Basics";
        labels = { "", "", "Stable gray", "Unstable mask" };
        desc = "Capture an image and compare it to previous frame using absDiff and threshold";
    }
    void Run(Mat src) {
        if (src.channels() == 3) cvtColor(src, src, COLOR_BGR2GRAY);
        if(task->firstPass) lastGray = src.clone();
        if (task->optionsChanged || lastGray.size() != src.size()) {
            lastGray = src.clone();
            dst3 = src.clone();
        }
        absdiff(src, lastGray, dst0);
        task->pixelDiffThreshold = 25;  // NOTE: normally this is in the constructor but is overidden if so.
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







class CPP_ApproxPoly_FindandDraw : public algorithmCPP
{
private:
public:
    CPP_Rectangle_Rotated* rotatedRect;
    CPP_ApproxPoly_FindandDraw(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_ApproxPoly_FindandDraw";
        rotatedRect = new CPP_Rectangle_Rotated(rows, cols);
        labels[2] = "FindandDraw input";
        labels[3] = "FindandDraw output - note the change in line width where ApproxPoly differs from DrawContours";
        desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours.";
    }
    void Run(Mat src) {
        rotatedRect->Run(src);
        dst2 = rotatedRect->dst2;
        cvtColor(dst2, dst0, COLOR_BGR2GRAY);
        threshold(dst0, dst0, 1, 255, THRESH_BINARY);
        dst0.convertTo(dst1, CV_32SC1);
        vector<vector<Point>> allContours;
        findContours(dst1, allContours, RetrievalModes::RETR_FLOODFILL, ContourApproximationModes::CHAIN_APPROX_SIMPLE);
        dst3.setTo(0);
        Mat nextContour;
        vector<Mat> contours;
        for (auto i = 0; i < allContours.size(); i++) {
            approxPolyDP(allContours[i], nextContour, 3, true);
            if (nextContour.rows > 2) contours.push_back(nextContour);
        }
        drawContours(dst3, contours, -1, YELLOW, task->lineWidth, task->lineType);
    }
};







class CPP_ApproxPoly_Basics : public algorithmCPP
{
private:
public:
    CPP_Contour_Largest* contour;
    CPP_Rectangle_Rotated* rotatedRect;
    CPP_ApproxPoly_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_ApproxPoly_Basics";
        contour = new CPP_Contour_Largest(rows, cols);
        rotatedRect = new CPP_Rectangle_Rotated(rows, cols);
        labels = { "", "", "Input to the ApproxPolyDP", "Output of ApproxPolyDP - note smoother edges." };
        desc = "Using the input contours, create a list of ApproxPoly output";
    }
    void Run(Mat src) {
        if (standalone) {
            if (task->heartBeat) rotatedRect->Run(src);
            src = rotatedRect->dst2;
        }
        contour->Run(src);
        dst2 = contour->dst3;
        Mat nextContour;
        approxPolyDP(contour->allContours[contour->maxIndex], nextContour, 3, true);
        dst3.setTo(0);
        task->drawContour(dst3, nextContour, YELLOW);
    }
};






class CPP_Hull_Basics : public algorithmCPP
{
private:
public:
    CPP_Random_Basics* random;
    vector<Point2f> inputPoints;
    vector<Point> hull;
    bool useRandomPoints;
    CPP_Hull_Basics(int rows, int cols) : algorithmCPP(rows, cols) {
        traceName = "CPP_Hull_Basics";
        random = new CPP_Random_Basics(rows, cols);
        labels = { "", "", "Input Points - draw a rectangle anywhere.  Enclosing rectangle in yellow.", "" };
        desc = "Given a list of points, create a hull that encloses them.";
    }
    void Run(Mat src) {
        if ((standalone && task->heartBeat) || (useRandomPoints && task->heartBeat)) {
            random->Run(src);
            dst2 = random->dst2;
            inputPoints = random->pointList;
        }
        Mat hull2f;
        convexHull(inputPoints, hull2f, true);
        hull = task->convert2f2i(hull2f);
        task->drawContour(dst2, hull, YELLOW);
    }
};






class CPP_ApproxPoly_Hull : public algorithmCPP
{
private:
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
    void Run(Mat src) {
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







class CPP_Motion_Basics : public algorithmCPP
{
private:
public:
    CPP_Diff_Basics* diff;
    int changedPixels;
    int cumulativePixels;
    bool resetAll = true;
    CPP_Motion_Basics(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_Motion_Basics";
        diff = new CPP_Diff_Basics(rows, cols);
        task->pixelDiffThreshold = 25;
        dst3 = Mat(dst3.size(), CV_8U);
        dst3.setTo(0);
        desc = "Detect contours in the motion data and the resulting rectangles";
    }
    void Run(Mat src)
    {
        auto percentOfImage = float(10.0f / 100.0f);
        static auto saveFrameCount = task->frameCount;
        if (saveFrameCount != task->frameCount)
        {
            saveFrameCount = task->frameCount;
            if (src.channels() != 1) cvtColor(src, src, COLOR_BGR2GRAY);
            diff->Run(src);
            dst2 = diff->dst3;
            changedPixels = diff->changedPixels;
            if (changedPixels > 0)
            {
                cumulativePixels += changedPixels;
                resetAll = cumulativePixels / src.total() > percentOfImage || changedPixels > dst2.total() / 16 || task->optionsChanged;
                if (resetAll || task->heartBeat)
                {
                    dst2.copyTo(dst3);
                    cumulativePixels = 0;
                }
                else
                {
                    dst3.setTo(255, dst2);
                }
            }
            auto threshold = src.total() * 10 / 100;
            auto strOut = "Cumulative threshold = " + to_string(int(threshold / 1000)) + "k";
            strOut += "Current cumulative pixels changed = " + to_string(int(cumulativePixels / 1000)) + "k" + "/n";
            labels[2] = strOut;
        }
    }
};








class CPP_Edge_MotionAccum : public algorithmCPP
{
private:
public:
    CPP_Edge_Canny* edges;
    CPP_Motion_Basics* motion;
    float percentMotion;
    CPP_Edge_MotionAccum(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_Edge_MotionAccum";
        edges = new CPP_Edge_Canny(rows, cols);
        motion = new CPP_Motion_Basics(rows, cols);
        dst2 = Mat(dst2.size(), CV_8U);
        dst2.setTo(0);
        labels = { "", "", "Accumulated edges tempered by motion thresholds", "" };
        desc = "Accumulate edges and use motion to clear";
    }
    void Run(Mat src)
    {
        if (task->optionsChanged) motion->resetAll = true;
        motion->Run(src);
        if (motion->resetAll || task->heartBeat) dst2.setTo(0);
        edges->Run(src);
        dst2.setTo(255, edges->dst2);
        labels[3] = motion->labels[2];
    }
};







class CPP_Edge_MotionFrames : public algorithmCPP
{
private:
public:
    CPP_Edge_Canny* edges;
    CPP_Edge_MotionFrames(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_Edge_MotionFrames";
        edges = new CPP_Edge_Canny(rows, cols);
        dst2 = Mat(dst2.size(), CV_8U);
        dst2.setTo(0);
        labels = { "", "", "The multi-frame edges output", "The Edge_Canny output for the last frame only" };
        desc = "Collect edges over several frames controlled with global frame history";
    }
    void Run(Mat src)
    {
        static vector<Mat> frames;
        auto fCount = task->frameHistory;
        if (task->optionsChanged) frames.clear();
        edges->Run(src);
        threshold(edges->dst2, dst1, 0, 255.0f / fCount, THRESH_BINARY);
        dst2 += dst1;
        frames.push_back(dst1);
        if (frames.size() >= fCount)
        {
            dst2 -= frames[0];
            frames.erase(frames.begin());
        }
        dst3 = edges->dst2;
    }
};











class CPP_EdgePreserving_Basics : public algorithmCPP
{
private:
public:
    CPP_Diff_Basics* diff;
    CPP_EdgePreserving_Basics(int rows, int cols) : algorithmCPP(rows, cols)
    {
        diff = new CPP_Diff_Basics(rows, cols);
        traceName = "CPP_EdgePreserving_Basics";
        labels[2] = "Output of the EdgePreserving Filter - draw anywhere to test more";
        task->drawRect = Rect(100, 100, 50, 50);
        desc = "Example using the OpenCV ximgproc extention for edge preserving filter";
    }
    void Run(Mat src)
    {
        dst2 = src;

        if (task->drawRect.width == 0) task->drawRect = Rect(100, 100, 50, 50);
        Mat small = src(task->drawRect);

        ximgproc::edgePreservingFilter(small, dst2(task->drawRect), 9, 20);
    }
};






class CPP_TEE_Basics : public algorithmCPP
{
private:
public:
    map<int, int> sizeSorted;
    vector<rcData> redCells;
    vector<rcData> lastCells;
    Mat lastcellMap;
    Mat cellMap = Mat(dst2.size(), CV_32S);
    CPP_Contour_Largest* contours;
    CPP_RedCloud_Flood* redF;
    CPP_TEE_Basics(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_TEE_Basics";
        cellMap.setTo(0);
        contours = new CPP_Contour_Largest(rows, cols);
        redF = new CPP_RedCloud_Flood(rows, cols);
        if (standalone) task->displayDst0 = true;
        task->gravityPointCloud = true;
        //labels[3] = "Note that the maxDist point does not move until it reaches the boundary of the cell.";
        desc = "Run floodFill on XY reduced point cloud";
    }
    rcData rcSelection(Mat output)
    {
        int index = cellMap.at<int>(task->clickPoint.y, task->clickPoint.x);
        rcData rc = redCells[index];
        dst0 = task->color;
        if (redF->options_highlightCell == false) return rc;
        if (task->displayDst0) task->drawContour(dst0(rc.rect), rc.contour, YELLOW);
        task->drawContour(output(rc.rect), rc.contour, WHITE, -1);
        return rc;
    }
    void Run(Mat src)
    {
        lastCells = vector<rcData>(redCells);
        lastcellMap = cellMap.clone();
        if (task->optionsChanged) lastcellMap.setTo(0);
        redF->Run(src);
        if (redF->totalCount == 0) return;
        redCells.clear();
        auto rc = redF->buildZeroEntry();
        redCells.push_back(rc);
        sizeSorted.clear();
        int count8u = 1;
        cellMap.setTo(0);
        dst2.setTo(0);
        int matchedCells = 0;
        vector<Vec3b> colors;
        for (auto i = 1; i < redF->totalCount; i++)
        {
            rc.rect = redF->rects[i];
            rc.rect = task->validateRect(rc.rect, dst2.cols, dst2.rows);
            rc.pixels = redF->sizes[i];
            rc.mask = redF->dst2(rc.rect).clone();
            inRange(rc.mask, count8u, count8u, rc.mask);
            count8u += 1;
            if (count8u == 256) count8u = 1;
            rc.maxDist = task->getMaxDist(rc.mask, rc.rect);
            rc.center = Point(rc.rect.x + rc.rect.width / 2, rc.rect.y + rc.rect.height / 2);
            rc.index = i;
            rc.indexLast = lastcellMap.at<int>(rc.maxDist.y, rc.maxDist.x);
            if (rc.indexLast > 0)
            {
                rcData lrc = lastCells[rc.indexLast];
                int testIndex = lastcellMap.at<int>(lrc.maxDist.y, lrc.maxDist.x);
                if (testIndex == rc.indexLast)
                {
                    //rc.maxDist = lrc.maxDist;
                }
                rc.color = lrc.color;
                matchedCells += 1;
                lrc.indexCurr = i;
                lastCells[rc.indexLast] = lrc;
            }
            if (rc.color == black || count(colors.begin(), colors.end(), rc.color))
            {
                rc.color = Vec3b(rand() % 240, rand() % 240, rand() % 240);
            }
            colors.push_back(rc.color);
            contours->Run(rc.mask);
            rc.contour = vector<Point>(contours->bestContour);
            Mat tmp(rc.mask.rows, rc.mask.cols, CV_32FC1);
            tmp.setTo(0);
            task->pcSplit[2](rc.rect).copyTo(tmp, rc.mask);
            minMaxLoc(tmp, &rc.depthMin, &rc.depthMax, &rc.depthMinLoc, &rc.depthMaxLoc);
            meanStdDev(task->gCloud(rc.rect), rc.depthMean, rc.depthStdev, rc.mask);
            if (rc.depthMax > rc.depthMean.val[2] + rc.depthStdev.val[2] * 3) rc.depthMax = rc.depthMean.val[2] + 3 * rc.depthStdev.val[2];
            dst2(rc.rect).setTo(rc.color, rc.mask);
            cellMap(rc.rect).setTo(rc.index, rc.mask);
            sizeSorted.insert(make_pair(rc.pixels, rc.index));
            redCells.push_back(rc);
        }
        labels[2] = to_string(matchedCells) + " of " + to_string(redCells.size()) + " cells matched previous generation. " +
                    to_string(redCells.size() - matchedCells) + " not matched.";
        rc = rcSelection(dst2);
        dst3 = dst2.clone();
        for (rcData rc : redCells)
        {
            circle(dst3, rc.maxDist, task->dotSize, YELLOW, -1, task->lineType);
        }
    }
};




class CPP_RedCloud_Hulls : public algorithmCPP
{
private:
public:
    CPP_TEE_Basics* redC;
    vector<rcData> redCells;
    Mat cellMap;
    int selectedIndex;
    Mat lastcellMap;
    CPP_RedCloud_Hulls(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_RedCloud_Hulls";
        redC = new CPP_TEE_Basics(rows, cols);
        if (standalone) task->displayDst0 = true;
        lastcellMap = Mat(dst2.size(), CV_32S);
        labels = { "", "", "RedCloud hulls", "Outline of each RedCloud hull showing overlap" };
        lastcellMap.setTo(0);
        cellMap = Mat(dst2.size(), CV_32S);
        desc = "Add contours and hulls to each RedCloud Cell";
    }
    rcData showHull(Mat dstFill)
    {
        selectedIndex = cellMap.at<int>(task->clickPoint.y, task->clickPoint.x);
        rcData rc = redCells[selectedIndex];
        if (redC->redF->options_highlightCell == false) return rc;
        task->drawContour(dstFill(rc.rect), rc.hull, WHITE, -1);
        return rc;
    }
    void Run(Mat src)
    {
        redC->Run(src);
        dst3 = src.clone();
        redCells.clear();
        dst2.setTo(0);
        cellMap.setTo(0);
        for (rcData rc : redC->redCells)
        {
            convexHull(rc.contour, rc.hull);
            rc.index = int(redCells.size());
            rc.indexLast = lastcellMap.at<int>(rc.maxDist.y, rc.maxDist.x);
            redCells.push_back(rc);
            task->drawContour(dst2(rc.rect), rc.hull, rc.color, -1);
            task->drawContour(cellMap(rc.rect), rc.hull, Scalar(rc.index, rc.index, rc.index), -1);
            task->drawContour(dst3(rc.rect), rc.hull, YELLOW);
        }
        if (redCells.size() == 0) return;
        auto rcX = showHull(dst2);
        if (task->displayDst0)
        {
            dst0 = task->color.clone();
            task->drawContour(dst0(rcX.rect), rcX.hull, YELLOW);
        }
        lastcellMap = cellMap.clone();
        labels[2] = to_string(redCells.size()) + " hulls identified below.";
    }
};







class CPP_Distance_Basics : public algorithmCPP
{
private:
public:
    CPP_Distance_Basics(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_Distance_Basics";
        labels[2] = "Distance Transform";
        desc = "Distance algorithmCPP basics.";
    }
    void Run(Mat src)
    {
        if (standalone) src = task->depthRGB;
        if (src.channels() == 3) cvtColor(src, src, COLOR_BGR2GRAY);
        distanceTransform(src, dst0, DIST_L1, 3);
        normalize(dst0, dst1, 0, 255, NormTypes::NORM_MINMAX);
        dst1.convertTo(dst2, CV_8UC1);
    }
};






class CPP_FeatureLess_Basics : public algorithmCPP
{
private:
public:
    CPP_Edge_Canny* edges;
    CPP_Distance_Basics* dist;
    int options_threshold = 10;
    CPP_FeatureLess_Basics(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_FeatureLess_Basics";
        edges = new CPP_Edge_Canny(rows, cols);
        dist = new CPP_Distance_Basics(rows, cols);
        desc = "Find the top pixels in the distance algorithmCPP.";
    }
    void Run(Mat src)
    {
        edges->Run(src);
        bitwise_not(edges->dst2, dst0);
        dist->Run(dst0);
        threshold(dist->dst2, dst2, options_threshold, 255, THRESH_BINARY);
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








class CPP_EdgeDraw_Basics : public algorithmCPP
{
private:
public:
    CPP_Edge_Segments* eDraw;
    CPP_EdgeDraw_Basics(int rows, int cols) : algorithmCPP(rows, cols)
    {
        eDraw = new CPP_Edge_Segments(rows, cols);
        traceName = "CPP_EdgeDraw_Basics";
        dst3 = Mat(dst3.size(), CV_8U);
        labels[2] = "Line segments of the Edge Drawing Filter - drawn on the src input";
        labels[3] = "Line segments of the Edge Drawing Filter";
        desc = "Display segments found with the EdgeDrawing filter in ximgproc";
    }
    void Run(Mat src)
    {
        dst3 = src;
        eDraw->Run(src);
        vector<vector<Point> > segments = eDraw->ed->getSegments();

        dst2.setTo(0);
        for (size_t i = 0; i < segments.size(); i++)
        {
            const Point* pts = &segments[i][0];
            int n = (int)segments[i].size();
            float distance = sqrt((pts[0].x - pts[n - 1].x) * (pts[0].x - pts[n - 1].x) + (pts[0].y - pts[n - 1].y) * (pts[0].y - pts[n - 1].y));
            bool drawClosed = distance < 10;
            polylines(dst2, &pts, &n, 1, drawClosed, Scalar(255, 255, 255), task->lineWidth, task->lineType);
            polylines(dst3, &pts, &n, 1, drawClosed, YELLOW, task->lineWidth, task->lineType);
        }

        rectangle(dst2, Rect(0, 0, dst3.cols, dst3.rows), 255, task->lineWidth, task->lineType);
        threshold(dst2, dst2, 0, 255, THRESH_BINARY);
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






class CPP_RedCloud_FeatureLess2 : public algorithmCPP
{
private:
public:
    CPP_FeatureLess_Edge* fLess;
    CPP_TEE_Basics* redC;
    CPP_RedCloud_FeatureLess2(int rows, int cols) : algorithmCPP(rows, cols)
    {
        traceName = "CPP_RedCloud_FeatureLess2";
        fLess = new CPP_FeatureLess_Edge(rows, cols);
        redC = new CPP_TEE_Basics(rows, cols);
        labels[3] = "Mask showing regions that are NOT featureless.";
        desc = "Identify and track the featureless regions produced with EdgeDrawing2.";
    }
    void Run(Mat src)
    {
        fLess->Run(src);
        bitwise_not(fLess->dst2, dst3);
        dst3.convertTo(dst0, CV_32S);
        redC->redF->inputMask = fLess->dst2;
        redC->Run(dst0);
        dst2 = redC->dst2;
        labels[2] = "There were " + to_string(redC->redCells.size()) + " featureless regions identified";
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

