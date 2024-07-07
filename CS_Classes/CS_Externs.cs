using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
namespace CS_Classes
{
    public class CS_Externs
    {




        //public static Mat Show_HSV_Hist(Mat hist, VBtask task)
        //{
        //    Mat img = new Mat(task.WorkingRes, MatType.CV_8UC3, Scalar.All(0));
        //    int binCount = hist.Height;
        //    int binWidth = img.Width / hist.Height;
        //    mmData mm = vbMinMax(hist);
        //    img.SetTo(Scalar.All(0));
        //    if (mm.maxVal > 0)
        //    {
        //        for (int i = 0; i < binCount - 1; i++)
        //        {
        //            double h = img.Height * (hist.At<float>(i, 0)) / mm.maxVal;
        //            if (h == 0) h = 5; // show the color range in the plot
        //            Cv2.Rectangle(img, new Rect(i * binWidth, img.Height - (int)h, binWidth, (int)h),
        //                          new Scalar(180.0 * i / binCount, 255, 255), -1);
        //        }
        //    }
        //    return img;
        //}




        //public static cv.Rangef[] vbHist2Dminmax(cv.Mat input, int chan1, int chan2)
        //{
        //    if (input.Type == cv.MatType.CV_8UC3)
        //    {
        //        // ranges are exclusive in OpenCV 
        //        return new cv.Rangef[] {
        //        new cv.Rangef(-histDelta, 256),
        //        new cv.Rangef(-histDelta, 256)
        //    };
        //    }

        //    var xInput = input.ExtractChannel(chan1);
        //    var yInput = input.ExtractChannel(chan2);

        //    var mmX = vbMinMax(xInput);
        //    var mmY = vbMinMax(yInput);

        //    // ranges are exclusive in OpenCV 
        //    return new cv.Rangef[] {
        //    new cv.Rangef(mmX.minVal - histDelta, mmX.maxVal + histDelta),
        //    new cv.Rangef(mmY.minVal - histDelta, mmY.maxVal + histDelta)
        //};
        //}



        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SemiGlobalMatching_Open(int rows, int cols, int disparityRange);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SemiGlobalMatching_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SemiGlobalMatching_Run(IntPtr SemiGlobalMatchingPtr, IntPtr leftPtr, IntPtr rightPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Annealing_Basics_Open(IntPtr cityPositions, int numberOfCities);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Annealing_Basics_Close(IntPtr saPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Annealing_Basics_Run(IntPtr saPtr, IntPtr cityOrder, int numberOfCities);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void MinTriangle_Run(IntPtr inputPtr, int numberOfPoints, IntPtr outputTriangle);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BGRPattern_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void BGRPattern_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BGRPattern_ClassCount(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BGRPattern_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BGSubtract_BGFG_Open(int currMethod);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BGSubtract_BGFG_Close(IntPtr bgfs);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BGSubtract_BGFG_Run(IntPtr bgfs, IntPtr bgrPtr, int rows, int cols, int channels, double learnRate);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BGSubtract_Synthetic_Open(IntPtr bgrPtr, int rows, int cols, string fgFilename, double amplitude, double magnitude, double wavespeed, double objectspeed);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BGSubtract_Synthetic_Close(IntPtr synthPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BGSubtract_Synthetic_Run(IntPtr synthPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Corners_ShiTomasi(IntPtr grayPtr, int rows, int cols, int blocksize, int aperture);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Features_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Features_Close(IntPtr Harris_FeaturesPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Features_Run(IntPtr Harris_FeaturesPtr, IntPtr inputPtr, int rows, int cols, float threshold, short neighborhood, short aperture, float HarrisParm);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Harris_Detector_Count(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Detector_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Detector_Close(IntPtr Harris_FeaturesPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Detector_Run(IntPtr Harris_FeaturesPtr, IntPtr inputPtr, int rows, int cols, double qualityLevel);





        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr cppTask_Open(int cppFunction, int rows, int cols,
                                         bool heartBeat, float addWeighted, int lineWidth,
                                         int lineType, int DotSize, int gridSize,
                                         int histogramBins, bool ocvheartBeat, bool gravityPointCloud,
                                         int pixelDiffThreshold, bool UseKalman, int paletteIndex,
                                         int frameHistory, bool displayDst0,
                                         bool displayDst1);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr cppTask_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr cppTask_RunCPP(IntPtr cPtr, IntPtr dataPtr, int channels, int frameCount,
                                                   int rows, int cols, float x, float y, float z,
                                                   bool optionsChanged, bool heartBeat, bool displayDst0,
                                                   bool displayDst1, bool debugCheckBox);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr cppTask_PointCloud(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr cppTask_DepthLeftRight(IntPtr cPtr, IntPtr dataPtr, IntPtr leftPtr,
                                                           IntPtr rightPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr cppTask_GetDst(IntPtr cPtr, int index, ref int channels);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void cppTask_OptionsCPPtoVB(IntPtr cPtr, ref int gridSize,
                                                         ref int histogramBins,
                                                         ref int pixelDiffThreshold,
                                                         ref bool UseKalman, ref int frameHistory,
                                                         ref int rectX, ref int rectY, ref int rectWidth,
                                                         ref int rectHeight,
                                                         [MarshalAs(UnmanagedType.LPStr)] StringBuilder labels,
                                                         [MarshalAs(UnmanagedType.LPStr)] StringBuilder descBuffer,
                                                         [MarshalAs(UnmanagedType.LPStr)] StringBuilder adviceBuffer);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void cppTask_OptionsVBtoCPP(IntPtr cPtr, int gridSize,
                                                         int histogramBins,
                                                         int pixelDiffThreshold,
                                                         bool UseKalman, int frameHistory,
                                                         int rectX, int rectY, int rectWidth,
                                                         int rectHeight, int lineWidth,
                                                         int lineType, int DotSize, int lowResWidth,
                                                         int lowResHeight, float MaxZmeters,
                                                         int PCReduction, float fontSize,
                                                         int fontThickness, int clickX,
                                                         int clickY, bool clickFlag, int picTag,
                                                         int moveX, int moveY, int paletteIndex,
                                                         int desiredCells, bool midHeartBeat,
                                                         bool quarterBeat, int colorInputIndex, int depthInputIndex,
                                                         float xRangeDefault, float yRangeDefault);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Denoise_Basics_Open(int frameCount);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Denoise_Basics_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Denoise_Basics_Run(IntPtr cPtr, IntPtr bgrPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Denoise_Pixels_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Denoise_Pixels_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Denoise_Pixels_EdgeCountBefore(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Denoise_Pixels_EdgeCountAfter(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Denoise_Pixels_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);





        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Density_2D_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Density_2D_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Density_2D_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, float distance);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Density_Count_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Density_Count_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Density_Count_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, int zCount);

        public int minLengthContour = 4; // use any contour with enough points to make a contour!

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Depth_Colorizer_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Depth_Colorizer_Close(IntPtr Depth_ColorizerPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Depth_Colorizer_Run(IntPtr Depth_ColorizerPtr, IntPtr bgrPtr, int rows, int cols, float maxDepth);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer16(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer8(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer4(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer3(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer2(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgbNbpp(IntPtr pixels, int width, int height, int nColors);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb3bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb6bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb9bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb12bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb15bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb18bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgbNbpp(IntPtr pixels, int width, int height, int nColors);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFS(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb3bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb6bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb9bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb12bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb15bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb18bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherSierraLiteRgbNbpp(IntPtr pixels, int width, int height, int nColors);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherSierraLite(IntPtr pixels, int width, int height);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherSierraRgbNbpp(IntPtr pixels, int width, int height, int nColors);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherSierra(IntPtr pixels, int width, int height);





        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_RandomForest_Open(string modelFileName);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_RandomForest_Close(IntPtr Edges_RandomForestPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_RandomForest_Run(IntPtr Edges_RandomForestPtr, IntPtr inputPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_Deriche_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_Deriche_Close(IntPtr Edges_DerichePtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_Deriche_Run(IntPtr Edges_DerichePtr, IntPtr bgrPtr, int rows, int cols, float alpha, float omega);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_ColorGap_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_ColorGap_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_ColorGap_Run(IntPtr cPtr, IntPtr bgrPtr, int rows, int cols, int distance, int diff);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_DepthGap_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_DepthGap_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Edge_DepthGap_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, float minDiff);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EdgeDraw_Basics_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EdgeDraw_Basics_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EdgeDraw_Basics_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, int lineWidth);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EdgeDraw_Edges_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EdgeDraw_Edges_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EdgeDraw_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, int lineWidth);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EdgeDraw_Lines_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int EdgeDraw_Lines_Count(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EdgeDraw_Lines_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EdgeDraw_Lines_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, int lineWidth);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EMax_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EMax_Close(IntPtr EMax_RawPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EMax_Run(IntPtr EMax_RawPtr, IntPtr samplesPtr, IntPtr labelsPtr, int inputCount, int dimension, int rows, int cols, int clusters, int stepSize, int covarianceMatrixType);






        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Agast_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Agast_Count(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Agast_Close(IntPtr Harris_FeaturesPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Agast_Run(IntPtr Harris_FeaturesPtr, IntPtr inputPtr, int rows, int cols, int threshold);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FitEllipse_AMS(IntPtr inputPtr, int numberOfPoints);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FitEllipse_Direct(IntPtr inputPtr, int numberOfPoints);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Fuzzy_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Fuzzy_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Fuzzy_Run(IntPtr cPtr, IntPtr bgrPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Guess_Depth_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Guess_Depth_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Guess_Depth_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Guess_ImageEdges_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Guess_ImageEdges_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Guess_ImageEdges_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, int maxDistanceToEdge);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Hist_1D_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Hist_1D_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Hist_1D_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, int bins);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern float Hist_1D_Sum(IntPtr cPtr);

        public static float histDelta = 0.00001f;


        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Hist3Dcolor_Run(IntPtr bgrPtr, int rows, int cols, int bins);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BackProjectBGR_Run(IntPtr bgrPtr, int rows, int cols, int bins, float threshold);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Hist3Dcloud_Run(IntPtr pcPtr, int rows, int cols, int bins, float minX, float minY, float minZ, float maxX, float maxY, float maxZ);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr BackProjectCloud_Run(IntPtr pcPtr, int rows, int cols, int bins, float threshold, float minX, float minY, float minZ, float maxX, float maxY, float maxZ);


        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr HMM_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr HMM_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr HMM_Run(IntPtr HMMPtr, IntPtr bgrPtr, int rows, int cols, int channels);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr KMeans_MultiGaussian_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr KMeans_MultiGaussian_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr KMeans_MultiGaussian_RunCPP(IntPtr cPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Kmeans_Simple_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Kmeans_Simple_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Kmeans_Simple_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, float minVal, float maxVal);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ML_RemoveDups_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ML_RemoveDups_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ML_RemoveDups_GetCount(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ML_RemoveDups_Run(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, int type);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr MSER_Open(int delta, int minArea, int maxArea, float maxVariation, float minDiversity,
                                              int maxEvolution, float areaThreshold, float minMargin, int edgeBlurSize,
                                              int pass2Setting);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void MSER_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr MSER_Rects(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr MSER_FloodPoints(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr MSER_MaskCounts(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MSER_Count(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr MSER_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, int channels);

        public static bool removeZeroNeighbors = true;

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Neighbors1_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Neighbors1_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Neighbors1_CellData(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Neighbors1_Points(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Neighbors1_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Neighbor2_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Neighbor2_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Neighbor2_Points(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Neighbor2_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);


        public Mat opticalFlow_Dense(Mat oldGray, Mat gray, float pyrScale, int levels, int winSize, int iterations,
                        float polyN, float polySigma, OpticalFlowFlags OpticalFlowFlags)
        {
            Mat flow = new Mat();
            if (pyrScale >= 1) pyrScale = 0.99f;

            if (oldGray.Size() != gray.Size()) oldGray = gray.Clone();

            Cv2.CalcOpticalFlowFarneback(oldGray, gray, flow, pyrScale, levels, winSize, iterations, (int)polyN, polySigma, OpticalFlowFlags);
            Mat[] flowVec = flow.Split();

            Mat hsv = new Mat();
            Mat hsv0 = new Mat();
            Mat hsv1 = new Mat(gray.Rows, gray.Cols, MatType.CV_8UC1, new Scalar(255));
            Mat hsv2 = new Mat();

            Mat magnitude = new Mat();
            Mat angle = new Mat();
            Cv2.CartToPolar(flowVec[0], flowVec[1], magnitude, angle);
            angle.ConvertTo(hsv0, MatType.CV_8UC1, 180 / Math.PI / 2);
            Cv2.Normalize(magnitude, hsv2, 0, 255, NormTypes.MinMax, MatType.CV_8UC1);

            Mat[] hsvVec = { hsv0, hsv1, hsv2 };
            Cv2.Merge(hsvVec, hsv);
            return hsv;
        }

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr OpticalFlow_CPP_Open();

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr OpticalFlow_CPP_Close(IntPtr sPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr OpticalFlow_CPP_Run(IntPtr sPtr, IntPtr bgrPtr, int rows, int cols);

        public void calcOpticalFlowPyrLK_Native(Mat gray1, Mat gray2, Mat features1, Mat features2)
        {
            GCHandle hGray1;
            GCHandle hGray2;
            GCHandle hF1;
            GCHandle hF2;

            byte[] grayData1 = new byte[gray1.Total()];
            byte[] grayData2 = new byte[gray2.Total()];
            byte[] fData1 = new byte[features1.Total() * features1.ElemSize()];
            byte[] fData2 = new byte[features2.Total() * features2.ElemSize()];
            hGray1 = GCHandle.Alloc(grayData1, GCHandleType.Pinned);
            hGray2 = GCHandle.Alloc(grayData2, GCHandleType.Pinned);
            hF1 = GCHandle.Alloc(fData1, GCHandleType.Pinned);
            hF2 = GCHandle.Alloc(fData2, GCHandleType.Pinned);
        }

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ParticleFilterTest_Open(string matlabFileName, int rows, int cols);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ParticleFilterTest_Close(IntPtr cPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ParticleFilterTest_Run(IntPtr pfPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PCA_Prep_Open();

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern void PCA_Prep_Close(IntPtr cPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern int PCA_Prep_GetCount(IntPtr cPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PCA_Prep_Run(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WhiteBalance_Open();

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WhiteBalance_Close(IntPtr cPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WhiteBalance_Run(IntPtr wPtr, IntPtr rgb, int rows, int cols, float thresholdVal);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SimpleProjectionRun(IntPtr cPtr, IntPtr depth, float desiredMin, float desiredMax, int rows, int cols);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SimpleProjectionSide(IntPtr cPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SimpleProjectionOpen();

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern void SimpleProjectionClose(IntPtr cPtr);

        public class compareAllowIdenticalDoubleInverted : IComparer<double>
        {
            public int Compare(double a, double b)
            {
                if (a <= b) return 1;
                return -1;
            }
        }

        public class compareAllowIdenticalDouble : IComparer<double>
        {
            public int Compare(double a, double b)
            {
                if (a >= b) return 1;
                return -1;
            }
        }

        public class compareAllowIdenticalSingleInverted : IComparer<float>
        {
            public int Compare(float a, float b)
            {
                if (a <= b) return 1;
                return -1;
            }
        }

        public class compareAllowIdenticalSingle : IComparer<float>
        {
            public int Compare(float a, float b)
            {
                if (a >= b) return 1;
                return -1;
            }
        }

        public class compareAllowIdenticalIntegerInverted : IComparer<int>
        {
            public int Compare(int a, int b)
            {
                if (a <= b) return 1;
                return -1;
            }
        }

        public class compareByte : IComparer<byte>
        {
            public int Compare(byte a, byte b)
            {
                if (a <= b) return -1;
                return 1;
            }
        }

        public class compareAllowIdenticalInteger : IComparer<int>
        {
            public int Compare(int a, int b)
            {
                if (a >= b) return 1;
                return -1;
            }
        }

        public class CompareMaskSize : IComparer<int>
        {
            public int Compare(int a, int b)
            {
                if (a <= b) return 1;
                return -1;
            }
        }

        public int findNearestCentroid(Point detailPoint, List<Point> centroids)
        {
            int minIndex = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < centroids.Count; i++)
            {
                Point pt = centroids[i];
                float distance = (float)Math.Sqrt((detailPoint.X - pt.X) * (detailPoint.X - pt.X) + (detailPoint.Y - pt.Y) * (detailPoint.Y - pt.Y));
                if (distance < minDistance)
                {
                    minIndex = i;
                    minDistance = distance;
                }
            }
            return minIndex;
        }

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Random_PatternGenerator_Open();

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Random_PatternGenerator_Close(IntPtr cPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Random_PatternGenerator_Run(IntPtr Random_PatternGeneratorPtr, int rows, int cols);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Random_DiscreteDistribution_Open();

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Random_DiscreteDistribution_Close(IntPtr cPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Random_DiscreteDistribution_Run(IntPtr rPtr, int rows, int cols, int channels);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RecursiveBilateralFilter_Open();

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RecursiveBilateralFilter_Close(IntPtr cPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RecursiveBilateralFilter_Run(IntPtr cPtr, IntPtr inputPtr, int rows, int cols, int recursions);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Neighbors_Open();

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern void Neighbors_Close(IntPtr cPtr);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern int Neighbors_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);

        [DllImport(("CPP_Classes.dll"), CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Neighbors_NabList(IntPtr cPtr);
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloud_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloud_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloud_Rects(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloud_Sizes(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloud_FloodPoints(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RedCloud_Count(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloud_Run(IntPtr cPtr, IntPtr dataPtr, IntPtr maskPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloudMaxDist_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloudMaxDist_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloudMaxDist_Rects(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloudMaxDist_Sizes(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloudMaxDist_FloodPoints(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RedCloudMaxDist_SetPoints(IntPtr cPtr, int count, IntPtr maxList);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RedCloudMaxDist_Count(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloudMaxDist_Run(IntPtr cPtr, IntPtr dataPtr, IntPtr maskPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PlotOpenCV_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PlotOpenCV_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PlotOpenCV_Run(IntPtr cPtr, IntPtr inX, IntPtr inY, int inLen, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloud_FindCells_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RedCloud_FindCells_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int RedCloud_FindCells_TotalCount(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr RedCloud_FindCells_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Pixels_Vector_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Pixels_Vector_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pixels_Vector_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Pixels_Vector_Pixels(IntPtr cPtr);

        public static float shapeCorrelation(List<Point> points)
        {
            Mat pts = new Mat(points.Count, 1, MatType.CV_32SC2, points.ToArray());
            Mat pts32f = new Mat();
            pts.ConvertTo(pts32f, MatType.CV_32FC2);
            Mat[] split = pts32f.Split();
            Mat correlationMat = new Mat();
            Cv2.MatchTemplate(split[0], split[1], correlationMat, TemplateMatchModes.CCoeffNormed);
            return correlationMat.Get<float>(0, 0);
        }

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Retina_Basics_Open(int rows, int cols, bool useLogSampling, float samplingFactor);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Retina_Basics_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Retina_Basics_Run(IntPtr RetinaPtr, IntPtr bgrPtr, int rows, int cols, IntPtr magno, int useLogSampling);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Salience_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Salience_Run(IntPtr classPtr, int numScales, IntPtr grayInput, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Salience_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Sort_MLPrepTest_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Sort_MLPrepTest_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Sort_MLPrepTest_Run(IntPtr cPtr, IntPtr bgrPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FLess_Range_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int FLess_Range_Count(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FLess_Range_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FLess_Range_Run(IntPtr cPtr, IntPtr bgrPtr, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SuperPixel_Open(int width, int height, int num_superpixels, int num_levels, int prior);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SuperPixel_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SuperPixel_GetLabels(IntPtr spPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SuperPixel_Run(IntPtr spPtr, IntPtr bgrPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Tracker_Basics_Open(int trackType);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Tracker_Basics_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Tracker_Basics_Run(IntPtr cPtr, IntPtr bgrPtr, int rows, int cols, int x, int y, int w, int h);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Vignetting_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Vignetting_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Vignetting_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols, double radius, double centerX, double centerY, bool removeal);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WarpModel_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WarpModel_Close(IntPtr WarpModelPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr WarpModel_Run(IntPtr WarpModelPtr, IntPtr src1Ptr, IntPtr src2Ptr, int rows, int cols, int channels, int warpMode);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr xPhoto_OilPaint_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr xPhoto_OilPaint_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr xPhoto_OilPaint_Run(IntPtr xPhoto_OilPaint_Ptr, IntPtr bgrPtr, int rows, int cols, int size, int dynRatio, int colorCode);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr xPhoto_Inpaint_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr xPhoto_Inpaint_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr xPhoto_Inpaint_Run(IntPtr xPhoto_Inpaint_Ptr, IntPtr bgrPtr, IntPtr maskPtr, int rows, int cols, int iType);

    }
}
