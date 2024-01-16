#pragma once
#include <algorithm>
#include <cmath>
#include <list>

class rcData {
public:
    Rect rect;
    Rect motionRect;  // the union of the previous rect with the current rect.
    Mat mask;
    Mat depthMask;

    int pixels;
    int depthPixels;

    Vec3b color;
    Scalar colorMean;
    Scalar colorStdev;
    int colorDistance;
    int grayMean;

    Point3f depthMean;
    Point3f depthStdev;
    float depthDistance;  // Adjusted to float for consistency with Point3f

    Point3f minVec;
    Point3f maxVec;

    Point maxDist;
    Point maxDStable;  // keep maxDist the same if it is still on the cell.

    int index;
    int indexLast;
    int matchCount;

    std::vector<Point> contour;
    std::vector<Point> corners;
    std::vector<Point3f> contour3D;
    std::vector<Point> hull;  // Using std::vector for consistency

    bool motionDetected;

    Point floodPoint;
    bool depthCell;  // true if no depth.

    Vec4f eq;  // plane equation
    Vec3f pcaVec;
    std::map<int, int> specG;  // Using std::map for key-value pairs
    std::map<int, int> specD;  // Using std::map for key-value pairs

    rcData() : index(0), depthCell(true) {
        mask = Mat(1, 1, CV_8U);
        rect = Rect(0, 0, 1, 1);
    }
};


struct mmData
{
    double minVal;
    double maxVal;
    Point minLoc;
    Point maxLoc;
};

// Custom comparators for allowing duplicates
struct compareAllowIdenticalDoubleInverted {
    bool operator()(double a, double b) const {
        return a > b; 
    }
};

struct compareAllowIdenticalDouble {
    bool operator()(double a, double b) const {
        return a < b;  
    }
};

struct compareAllowIdenticalSingleInverted {
    bool operator()(float a, float b) const {
        return a > b;
    }
};

struct compareAllowIdenticalSingle {
    bool operator()(float a, float b) const {
        return a < b;
    }
};

struct compareAllowIdenticalIntegerInverted {
    bool operator()(int a, int b) const {
        return a > b;
    }
};

struct compareAllowIdenticalInteger {
    bool operator()(int a, int b) const {
        return a < b;
    }
};

struct CompareMaskSize {
    bool operator()(int a, int b) const {
        return a > b;
    }
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
    static constexpr float verticalSlope = 1000000.0f;  // Using constexpr for constant

    linePoints(const Point2f& _p1, const Point2f& _p2) : p1(_p1), p2(_p2) {
        slope = (p1.x != p2.x) ? (p1.y - p2.y) / (p1.x - p2.x) : verticalSlope;
        yIntercept = p1.y - slope * p1.x;
    }

    linePoints() : p1(), p2() {}  // Default constructor

    bool compare(const linePoints& mp) const {  // Using const for non-modifying methods
        return mp.p1 == p1 && mp.p2 == p2;
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

#include "CPP_Functions.h"

Size workingRes;

class algorithmCPP
{
public:
    Mat dst0, dst1, dst2, dst3, empty;
    bool standalone;
    String advice;
    String desc;
    int* cPtr;
    string traceName;
    vector<string> labels{ "", "", "", "" };
    algorithmCPP()
    {
        dst0 = Mat(workingRes.height, workingRes.width, CV_8UC3);
        dst0.setTo(0);
        dst1 = Mat(dst0.size(), CV_8UC3);
        dst1.setTo(0);
        dst2 = Mat(dst0.size(), CV_8UC3);
        dst2.setTo(0);
        dst3 = Mat(dst0.size(), CV_8UC3);
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
    float cvFontSize;
    int cvFontThickness;
    Scalar fontColor;
    int frameCount;  Point3f accRadians; vector<Rect> roiList;
    bool motionReset; rcData rcSelect; int desiredCells;

    bool heartBeat; bool midHeartBeat; bool quarterBeat; bool debugCheckBox; Size minRes; int PCReduction;
    bool optionsChanged; double AddWeighted; int dotSize; int gridSize; float maxZmeters;
    int histogramBins; int pixelDiffThreshold; bool gravityPointCloud; bool useKalman;
    int paletteIndex; int polyCount; bool firstPass; Scalar highlightColor; int frameHistoryCount;
    Point clickPoint; bool mouseClickFlag; int mousePicTag; Point mouseMovePoint; bool mouseMovePointUpdated;
    Scalar scalarColors[256]; Vec3b vecColors[256]; Rect drawRect; bool displayDst0; bool displayDst1;
    bool gridROIclicked;
    Mat gridToRoiIndex;
    int colorInputIndex;
    vector<Rect> gridList;
    vector<vector<int>> gridNeighbors;

    Mat gMatrix; vector<Mat> pcSplit;
    bool paused = false;
    Mat xdst0, xdst1, xdst2, xdst3;
    Size workingRes;
    cppTask(int rows, int cols)
    {
        workingRes = Size(cols, rows);
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
        vec.assign((Point*)hull2i.data, (Point*)hull2i.data + hull2i.total());
        return vec;
    }
    void setTrueText(String text, Mat dst, Point2f pt = Point2f(10, 50))
    {
        if (cppFunction < 0) return;
        putText(dst, text, pt, this->font, this->cvFontSize, this->fontColor);
    }
   Point vbGetMaxDist(const Mat& mask) {
        Mat distance32f;
        distanceTransform(mask, distance32f, cv::DIST_L1, 0); 
        double minVal, maxVal;
        Point minLoc, maxLoc;
        minMaxLoc(distance32f, &minVal, &maxVal, &minLoc, &maxLoc);
        return maxLoc;
    }
    mmData vbMinMax(Mat mat, Mat mask = Mat())
    {
        mmData mm;
        if (mask.rows == 0)
        {
            minMaxLoc(mat, &mm.minVal, &mm.maxVal, &mm.minLoc, &mm.maxLoc);
        }
        else {
            minMaxLoc(mat, &mm.minVal, &mm.maxVal, &mm.minLoc, &mm.maxLoc, mask);
        }
        return mm;
    }
    void drawRotatedOutline(const RotatedRect& rotatedRect, Mat& dst2, const Scalar& color) {
        vector<Point2f> pts;
        rotatedRect.points(pts);

        Point lastPt = pts[0];
        for (int i = 1; i <= pts.size(); i++) {
            int index = i % pts.size();
            cv::Point pt = pts[index];
            cv::line(dst2, pt, lastPt, highlightColor, lineWidth, lineType);
            lastPt = pt;
        }
    }
    std::vector<cv::Point2f> quickRandomPoints(int howMany) {
        std::vector<cv::Point2f> srcPoints;
        random_device rd;
        mt19937 gen(rd());  // Mersenne Twister engine for randomness
        uniform_int_distribution<> dist_width(0, workingRes.width - 1);  
        uniform_int_distribution<> dist_height(0, workingRes.height - 1);

        srcPoints.clear();
        for (int i = 0; i < howMany; i++) {
            srcPoints.push_back(Point2f(dist_height(gen), dist_width(gen)));
        }

        return srcPoints;
    }
    Vec3b randomCellColor() {
        static random_device rd;
        static mt19937 gen(rd());  // Mersenne Twister engine for randomness
        uniform_int_distribution<> dist(50, 240);

        // Generate three random values between 50 and 240 (inclusive)
        // using uniform_int_distribution for more control over range
        return Vec3b(dist(gen), dist(gen), dist(gen));
    }
    void AddPlotScale(Mat& dst, double minVal, double maxVal, int lineCount = 3) {
        // Draw a scale along the side
        int spacer = cvRound(dst.rows / (lineCount + 1));
        int spaceVal = cvRound((maxVal - minVal) / (lineCount + 1));
        if (lineCount > 1 && spaceVal < 1) {
            spaceVal = 1;
        }
        spaceVal += spaceVal % 10; // Ensure even spacing

        for (int i = 0; i <= lineCount; i++) {
            Point p1(0, spacer * i);
            Point p2(dst.cols, spacer * i);
            line(dst, p1, p2, Scalar(255, 255, 255), this->cvFontThickness); // White line

            double nextVal = maxVal - spaceVal * i;
            std::string nextText;
            if (maxVal > 1000) {
                nextText = std::to_string(cvRound(nextVal / 1000)) + "k"; // Format for thousands
            }
            else {
                nextText = std::to_string(cvRound(nextVal)); // Normal formatting
            }

            putText(dst, nextText, p1, FONT_HERSHEY_PLAIN, this->cvFontSize, Scalar(255, 255, 255),
                this->cvFontThickness, this->lineType);
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

    Rect validateRect(const Rect& r, int width, int height) {
        Rect result = r;
        if (result.width < 0) result.width = 1;
        if (result.height < 0) result.height = 1;
        if (result.x < 0) result.x = 0;
        if (result.y < 0) result.y = 0;
        if (result.x > width) result.x = width;
        if (result.y > height) result.y = height;
        if (result.x + result.width > width) result.width = width - result.x;
        if (result.y + result.height > height) result.height = height - result.y;
        return result;
    }

    Rect initRandomRect(int margin, int width) {
        // Use C++11's random number generator for better randomness
        static random_device rd;
        static mt19937 gen(rd());  // Mersenne Twister engine
        static uniform_int_distribution<> dist(margin, width - 2 * margin);
        return Rect(dist(gen), dist(gen), dist(gen), dist(gen));
    }

    void drawRotatedRectangle(RotatedRect rr, Mat dst2, const Scalar color) {
        vector<Point2f> vertices2f;
        rr.points(vertices2f);
        vector<Point> vertices(vertices2f.size());
        for (int j = 0; j < vertices2f.size(); j++) {
            vertices[j] = Point(cvRound(vertices2f[j].x), cvRound(vertices2f[j].y));
        }
        fillConvexPoly(dst2, vertices, color, this->lineType);
    }
};