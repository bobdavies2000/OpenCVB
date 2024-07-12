#include <cstdlib>
#include <cstdio>
#include <iostream>
#include <algorithm>
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/highgui.hpp>

using namespace std;
using namespace cv;
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
    PCA_NColor(){}
    void RunCPP(uint8_t* imagePtr, uint8_t* palettePtr, int desiredNcolors) {
        std::vector<uint8_t> rgb((uint8_t*)imagePtr, (uint8_t*)imagePtr + src.rows * src.cols * 3);
        std::vector<uint8_t> palette(palettePtr, (uint8_t*)(palettePtr + 256 * 3));

        palettizedImage = RgbToIndex(rgb, palette, desiredNcolors);
    }
};

extern "C" __declspec(dllexport)
PCA_NColor *PCA_NColor_Open() {
    PCA_NColor *cPtr = new PCA_NColor();
    return cPtr;
}

extern "C" __declspec(dllexport)
void PCA_NColor_Close(PCA_NColor *cPtr)
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
