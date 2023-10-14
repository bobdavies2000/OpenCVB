#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace  cv;
// http://man.hubwiz.com/docset/OpenCV.docset/Contents/Resources/Documents/d9/dde/samples_2cpp_2kmeans_8cpp-example.html

cv::Scalar colorTab[] =
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
KMeans_MultiGaussian *KMeans_MultiGaussian_Open() {
    KMeans_MultiGaussian *cPtr = new KMeans_MultiGaussian();
    return cPtr;
}

extern "C" __declspec(dllexport)
int *KMeans_MultiGaussian_Close(KMeans_MultiGaussian *cPtr)
{
    delete cPtr;
    return (int*)0;
}

extern "C" __declspec(dllexport)
int *KMeans_MultiGaussian_RunCPP(KMeans_MultiGaussian *cPtr, int rows, int cols)
{
		cPtr->dst = Mat(rows, cols, CV_8UC3);
        cPtr->dst.setTo(0);
		cPtr->RunCPP();
		return (int *) cPtr->dst.data; 
}