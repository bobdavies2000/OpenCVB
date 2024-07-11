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
    void Tred2(std::vector<std::vector<double>>& V, std::vector<double>& d, std::vector<double>& e) {
        int dLen = (int)d.size();
        int i, j, k;

        // This is derived from the Algol procedures tred2 by
        // Bowdler, Martin, Reinsch, and Wilkinson, Handbook for
        // Auto. Comp., Vol.ii-Linear Algebra, and the corresponding
        // Fortran subroutine in EISPACK.

        for (j = 0; j < dLen; j++) {
            d[j] = V[dLen - 1][j];
        }

        // Householder reduction to tridiagonal form.

        for (i = dLen - 1; i > 0; i--) {
            // Scale to avoid under/overflow.

            double scale = 0.0;
            double h = 0.0;
            for (k = 0; k < i; k++) {
                scale += std::abs(d[k]);
            }

            if (scale == 0.0) {
                e[i] = d[i - 1];
                for (j = 0; j < i; j++) {
                    d[j] = V[i - 1][j];
                    V[i][j] = 0.0;
                    V[j][i] = 0.0;
                }
            }
            else {
                // Generate Householder vector.
                double f, g;
                double hh;

                for (k = 0; k < i; k++) {
                    d[k] /= scale;
                    h += d[k] * d[k];
                }
                f = d[i - 1];
                g = std::sqrt(h);
                if (f > 0) {
                    g = -g;
                }
                e[i] = scale * g;
                h = h - f * g;
                d[i - 1] = f - g;
                for (j = 0; j < i; j++) {
                    e[j] = 0.0;
                }

                // Apply similarity transformation to remaining columns.

                for (j = 0; j < i; j++) {
                    f = d[j];
                    V[j][i] = f;
                    g = e[j] + V[j][j] * f;
                    for (k = j + 1; k < i; k++) {
                        g += V[k][j] * d[k];
                        e[k] += V[k][j] * f;
                    }
                    e[j] = g;
                }
                f = 0.0;
                for (j = 0; j < i; j++) {
                    e[j] /= h;
                    f += e[j] * d[j];
                }
                hh = f / (h + h);
                for (j = 0; j < i; j++) {
                    e[j] -= hh * d[j];
                }
                for (j = 0; j < i; j++) {
                    f = d[j];
                    g = e[j];
                    for (k = j; k < i; k++) {
                        V[k][j] -= (f * e[k] + g * d[k]);
                    }
                    d[j] = V[i - 1][j];
                    V[i][j] = 0.0;
                }
            }
            d[i] = h;
        }

        // Accumulate transformations.

        for (i = 0; i < dLen - 1; i++) {
            double h;
            V[dLen - 1][i] = V[i][i];
            V[i][i] = 1.0;
            h = d[i + 1];
            if (h != 0.0) {
                for (k = 0; k <= i; k++) {
                    d[k] = V[k][i + 1] / h;
                }
                for (j = 0; j <= i; j++) {
                    double g = 0.0;
                    for (k = 0; k <= i; k++) {
                        g += V[k][i + 1] * V[k][j];
                    }
                    for (k = 0; k <= i; k++) {
                        V[k][j] -= g * d[k];
                    }
                }
            }
            for (k = 0; k <= i; k++) {
                V[k][i + 1] = 0.0;
            }
        }
        for (j = 0; j < dLen; j++) {
            d[j] = V[dLen - 1][j];
            V[dLen - 1][j] = 0.0;
        }
        V[dLen - 1][dLen - 1] = 1.0;
        e[0] = 0.0;
    }

    void Tql2(std::vector<std::vector<double>>& V, std::vector<double>& d, std::vector<double>& e) {
        // This is derived from the Algol procedures tql2, by
        // Bowdler, Martin, Reinsch, and Wilkinson, Handbook for
        // Auto. Comp., Vol.ii-Linear Algebra, and the corresponding
        // Fortran subroutine in EISPACK.

        int dLen = (int)d.size();
        int i, j, k, l;
        double f, tst1, eps;

        for (i = 1; i < dLen; i++) {
            e[i - 1] = e[i];
        }
        e[dLen - 1] = 0.0;

        f = 0.0;
        tst1 = 0.0;
        eps = std::pow(2.0, -52.0);
        for (l = 0; l < dLen; l++) {
            // Find small subdiagonal element

            tst1 = std::max(tst1, std::abs(d[l]) + std::abs(e[l]));
            int m = l;
            while (m < dLen) {
                if (std::abs(e[m]) <= eps * tst1) {
                    break;
                }
                m++;
            }

            // If m == l, d(l) is an eigenvalue,
            // otherwise, iterate.

            if (m > l) {
                int iter = 0;
                do {
                    double g, p, r;
                    double dl1;
                    double h;
                    double c;
                    double c2;
                    double c3;
                    double el1;
                    double s;
                    double s2;

                    iter++;  // (Could check iteration count here.)

                    // Compute implicit shift

                    g = d[l];
                    p = (d[l + 1] - g) / (2.0 * e[l]);
                    r = Hypot(p, 1.0);
                    if (p < 0) {
                        r = -r;
                    }
                    d[l] = e[l] / (p + r);
                    d[l + 1] = e[l] * (p + r);
                    dl1 = d[l + 1];
                    h = g - d[l];
                    for (i = l + 2; i < dLen; i++) {
                        d[i] -= h;
                    }
                    f += h;

                    // Implicit QL transformation.

                    p = d[m];
                    c = 1.0;
                    c2 = c;
                    c3 = c;
                    el1 = e[l + 1];
                    s = 0.0;
                    s2 = 0.0;
                    for (i = m - 1; i >= l; i--) {
                        c3 = c2;
                        c2 = c;
                        s2 = s;
                        g = c * e[i];
                        h = c * p;
                        r = Hypot(p, e[i]);
                        e[i + 1] = s * r;
                        s = e[i] / r;
                        c = p / r;
                        p = c * d[i] - s * g;
                        d[i + 1] = h + s * (c * g + s * d[i]);

                        // Accumulate transformation.

                        for (k = 0; k < dLen; k++) {
                            h = V[k][i + 1];
                            V[k][i + 1] = s * V[k][i] + c * h;
                            V[k][i] = c * V[k][i] - s * h;
                        }
                    }
                    p = -s * s2 * c3 * el1 * e[l] / dl1;
                    e[l] = s * p;
                    d[l] = c * p;

                    // Check for convergence.

                } while (std::abs(e[l]) > eps * tst1);
            }
            d[l] += f;
            e[l] = 0.0;
        }

        // Sort eigenvalues and corresponding vectors.

        for (i = 0; i < dLen - 1; i++) {
            int k1 = i;
            double p = d[i];
            for (j = i + 1; j < dLen; j++) {
                if (d[j] < p) {
                    k1 = j;
                    p = d[j];
                }
            }
            if (k1 != i) {
                d[k1] = d[i];
                d[i] = p;
                for (j = 0; j < dLen; j++) {
                    p = V[j][i];
                    V[j][i] = V[j][k1];
                    V[j][k1] = p;
                }
            }
        }
    }
    struct PaletteEntry {
        int start;
        int nCount;
        uint8_t red;
        uint8_t green;
        uint8_t blue;
        double errorVal;
    };

    // Colour difference function
    double CDiff(uchar* rgb, int start, uchar* pal, int startPal) {
        return (static_cast<int>(rgb[start + 0]) - static_cast<int>(pal[startPal + 0])) * (static_cast<int>(rgb[start + 0]) - static_cast<int>(pal[startPal + 0])) * 5 +
               (static_cast<int>(rgb[start + 1]) - static_cast<int>(pal[startPal + 1])) * (static_cast<int>(rgb[start + 1]) - static_cast<int>(pal[startPal + 1])) * 8 +
               (static_cast<int>(rgb[start + 2]) - static_cast<int>(pal[startPal + 2])) * (static_cast<int>(rgb[start + 2]) - static_cast<int>(pal[startPal + 2])) * 2;
    }

    // Convert an image to indexed form, using passed-in palette
    std::vector<uint8_t> RgbToIndex(uchar* rgb, int width, int height, uchar* pal, int nColor) {
        std::vector<uint8_t> answer(width * height);

        for (int i = 0; i < width * height; ++i) {
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

    double MeanColour(uchar* rgb, int start, int nnCount, int index) {
        if (nnCount == 0) return 0;
        double answer = 0;
        for (int i = 0; i < nnCount; ++i) {
            answer += rgb[start + i * 3 + index];
        }
        return answer / nnCount;
    }

    void CalcError(PaletteEntry& entry, uchar *buff) {
        entry.red = static_cast<uint8_t>(MeanColour(buff, entry.start * 3, entry.nCount, 0));
        entry.green = static_cast<uint8_t>(MeanColour(buff, entry.start * 3, entry.nCount, 1));
        entry.blue = static_cast<uint8_t>(MeanColour(buff, entry.start * 3, entry.nCount, 2));
        entry.errorVal = 0;

        for (int i = 0; i < entry.nCount; ++i) {
            entry.errorVal += std::abs(static_cast<int>(buff[(entry.start + i) * 3]) - entry.red);
            entry.errorVal += std::abs(static_cast<int>(buff[(entry.start + i) * 3 + 1]) - entry.green);
            entry.errorVal += std::abs(static_cast<int>(buff[(entry.start + i) * 3 + 2]) - entry.blue);
        }
    }

    Mat MakePalette(uchar *rgb, int width, int height, int nColors) {
        std::vector<PaletteEntry> entry(nColors);
        Mat palette = Mat(256, 1, CV_8UC3);
        uchar *pal = (uchar *) palette.data;

        entry[0].start = 0;
        entry[0].nCount = width * height;
        CalcError(entry[0], rgb);

        for (int i = 1; i < nColors; ++i) {
            double best = entry[0].errorVal;
            int bestii = 0;
            for (int ii = 0; ii < i; ++ii) {
                if (entry[ii].errorVal > best) {
                    best = entry[ii].errorVal;
                    bestii = ii;
                }
            }
            SplitPCA(entry[bestii], entry[i], rgb);
        }

        for (int i = 0; i < nColors; ++i) {
            pal[i * 3] = entry[i].red;
            pal[i * 3 + 1] = entry[i].green;
            pal[i * 3 + 2] = entry[i].blue;
        }
        
        return palette;
    }
// Get principal components of variance
// Params: ret - return for components of major axis of variance
//         pixels - the pixels
//         nnCount - count of pixels
    void PCA(std::vector<double>& ret, uchar* pixels, int start, int nnCount) {
        std::vector<std::vector<double>> cov(3, std::vector<double>(3));
        std::vector<double> mu(3);
        double var;
        std::vector<double> d(3);
        std::vector<std::vector<double>> v(3, std::vector<double>(3));

        for (int i = 0; i < 3; ++i) {
            mu[i] = MeanColour(pixels, start, nnCount, i);
        }

        // Calculate 3x3 channel covariance matrix
        for (int i = 0; i < 3; ++i) {
            for (int j = 0; j <= i; ++j) {
                var = 0;
                for (int k = 0; k < nnCount; ++k) {
                    var += (pixels[start + k * 3 + i] - mu[i]) * (pixels[start + k * 3 + j] - mu[j]);
                }
                cov[i][j] = var / nnCount;
                cov[j][i] = var / nnCount;
            }
        }

        EigenDecomposition(cov, v, d);
        // Main component in col 3 of eigenvector matrix
        ret[0] = v[0][2];
        ret[1] = v[1][2];
        ret[2] = v[2][2];
    }

    int Project(uchar* rgb, int start, const std::vector<double>& comp) {
        return static_cast<int>(rgb[start] * comp[0] + rgb[start + 1] * comp[1] + rgb[start + 2] * comp[2]);
    }

    // Split an entry using PCA and Otsu thresholding.
    // We find the principal component of variance in RGB space.
    // Then we apply Otsu thresholding along that axis, and cut.
    // We partition using one pass of quick sort.
    void SplitPCA(PaletteEntry& entry, PaletteEntry& split, uchar* buff) {
        int low = 0;
        int high = entry.nCount - 1;
        int cut;
        std::vector<double> comp(3);

        PCA(comp, buff, entry.start * 3, entry.nCount);
        cut = GetOtsuThreshold2(buff, entry.start * 3, entry.nCount, comp);

        while (low < high) {
            while (low < high && Project(buff, (entry.start + low) * 3, comp) < cut) {
                ++low;
            }
            while (low < high && Project(buff, (entry.start + high) * 3, comp) >= cut) {
                --high;
            }
            if (low < high) {
                for (int i = 0; i < 3; ++i) {
                    std::swap(buff[(entry.start + low) * 3 + i], buff[(entry.start + high) * 3 + i]);
                }
            }
            ++low;
            --high;
        }

        split.start = entry.start + low;
        split.nCount = entry.nCount - low;
        entry.nCount = low;

        CalcError(entry, buff);
        CalcError(split, buff);
    }

    // Get the Otsu threshold for image segmentation
    // rgb: The RGB image data
    // N: Total number of pixels
    // remap: Remapping values for RGB channels
    // Returns: Threshold at which to split pixels into foreground and background
    int GetOtsuThreshold2(uchar* rgb, int start, int N, const std::vector<double>& remap) {
        std::vector<int> hist(1024, 0);
        int wB = 0;
        int wF;
        float mB, mF;
        float sum = 0.0f;
        float sumB = 0.0f;
        float varBetween;
        float varMax = 0.0f;
        int answer = 0;

        for (int i = 0; i < N; ++i) {
            int nc = static_cast<int>(rgb[start + i * 3] * remap[0] + rgb[start + i * 3 + 1] * remap[1] + rgb[start + i * 3 + 2] * remap[2]);
            ++hist[512 + nc];
        }

        // Sum of all (for means)
        for (int k = 0; k < 1024; ++k) {
            sum += k * hist[k];
        }

        for (int k = 0; k < 1024; ++k) {
            wB += hist[k];
            if (wB == 0) {
                continue;
            }

            wF = N - wB;
            if (wF == 0) {
                break;
            }

            sumB += static_cast<float>(k * hist[k]);

            mB = sumB / wB;            // Mean Background
            mF = (sum - sumB) / wF;    // Mean Foreground

            // Calculate Between Class Variance
            varBetween = static_cast<float>(wB) * static_cast<float>(wF) * (mB - mF) * (mB - mF);

            // Check if new maximum found
            if (varBetween > varMax) {
                varMax = varBetween;
                answer = k;
            }
        }

        return answer - 512;
    }

    void EigenDecomposition(const std::vector<std::vector<double>>& A, std::vector<std::vector<double>>& V, std::vector<double>& d) {
        int bufLen = (int) A.size();
        std::vector<double> e(bufLen);

        for (int i = 0; i < bufLen; ++i) {
            for (int j = 0; j < bufLen; ++j) {
                V[i][j] = A[i][j];
            }
        }

        Tred2(V, d, e);
        Tql2(V, d, e);
    }
        // Symmetric tridiagonal QL algorithm.
    double Hypot(double a, double b) {
        return std::sqrt(a * a + b * b);
    }

    
public:
    Mat src, dst, palette;
    std::vector<uint8_t> paletteImage;
    PCA_NColor(){}
    void RunCPP(int desiredNcolors, bool heartBeat) {
        if (heartBeat) palette = MakePalette((uchar *) src.data, src.cols, src.rows, desiredNcolors);
        paletteImage = RgbToIndex((uchar*)src.data, src.cols, src.rows, palette.data, desiredNcolors);
    }
};

extern "C" __declspec(dllexport)
PCA_NColor *PCA_NColorOriginal_Open() {
    PCA_NColor *cPtr = new PCA_NColor();
    return cPtr;
}

extern "C" __declspec(dllexport)
void PCA_NColorOriginal_Close(PCA_NColor *cPtr)
{
    delete cPtr;
}

extern "C" __declspec(dllexport)
int* PCA_NColorOriginal_RunCPP(PCA_NColor* cPtr, int* dataPtr, int rows, int cols, int desiredColors, bool heartBeat)
{
    cPtr->src = Mat(rows, cols, CV_8UC3, dataPtr);
    cPtr->RunCPP(desiredColors, heartBeat);
    return (int*)&cPtr->paletteImage[0];
}

extern "C" __declspec(dllexport)
int* PCA_NColorOriginal_Palette(PCA_NColor* cPtr)
{
    return (int*)cPtr->palette.data;
}