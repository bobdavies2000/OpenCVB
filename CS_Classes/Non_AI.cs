using cv = OpenCvSharp;
using System.Drawing;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using OpenCvSharp.Features2D;
using OpenCvSharp.XFeatures2D;
using OpenCvSharp.Dnn;
using OpenCvSharp.DnnSuperres;
using System.IO;
using System.Net;
using VB_Classes;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CS_Classes
{
    public class Bitmap_ToMat_CS : VB_Parent
    {
        public Bitmap_ToMat_CS()
        {
            labels[2] = "Convert color bitmap to Mat";
            labels[3] = "Convert Mat to bitmap and then back to Mat";
            desc = "Convert a color and grayscale bitmap to a cv.Mat";
        }

        public void RunAlg(Mat src)
        {
            Bitmap bitmap = new Bitmap(vbc.task.HomeDir + "opencv/Samples/Data/lena.jpg");
            dst2 = BitmapConverter.ToMat(bitmap).Resize(src.Size());

            bitmap = BitmapConverter.ToBitmap(src);
            dst3 = BitmapConverter.ToMat(bitmap);
        }
    }




    public class Blur_Gaussian_CS : VB_Parent
    {
        public Options_Blur options = new Options_Blur();
        public Blur_Gaussian_CS()
        {
            desc = "Smooth each pixel with a Gaussian kernel of different sizes.";
        }
        public void RunAlg(Mat src)
        {
            options.RunOpt();
            cv.Cv2.GaussianBlur(src, dst2, new cv.Size(options.kernelSize, options.kernelSize), 0, 0);
        }
    }





    public class Feature_Kaze_CS : VB_Parent
    {
        public KeyPoint[] kazeKeyPoints = null;
        public Feature_Kaze_CS()
        {
            labels[2] = "KAZE key points";
            desc = "Find keypoints using KAZE algorithm.";
        }
        public void RunAlg(Mat src)
        {
            dst2 = src.Clone();
            if (src.Channels() != 1) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            var kaze = KAZE.Create();
            var kazeDescriptors = new Mat();
            kaze.DetectAndCompute(src, null, out kazeKeyPoints, kazeDescriptors);

            for (int i = 0; i < kazeKeyPoints.Length; i++)
            {
                DrawCircle(dst2, kazeKeyPoints[i].Pt, vbc.task.DotSize, vbc.task.HighlightColor);
            }
        }
    }





    public class Feature_AKaze_CS : VB_Parent
    {
        KeyPoint[] kazeKeyPoints = null;
        public Feature_AKaze_CS()
        {
            labels[2] = "AKAZE key points";
            desc = "Find keypoints using AKAZE algorithm.";
        }
        public void RunAlg(Mat src)
        {
            dst2 = src.Clone();
            if (src.Channels() != 1) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            var kaze = AKAZE.Create();
            var kazeDescriptors = new Mat();
            kaze.DetectAndCompute(src, null, out kazeKeyPoints, kazeDescriptors);

            for (int i = 0; i < kazeKeyPoints.Length; i++)
            {
                DrawCircle(dst2, kazeKeyPoints[i].Pt, vbc.task.DotSize, vbc.task.HighlightColor);
            }
        }
    }






    public class Feature_LeftRight_CS : VB_Parent
    {
        Options_Kaze options;
        Feature_Kaze_CS KazeLeft;
        Feature_Kaze_CS KazeRight;
        public Feature_LeftRight_CS()
        {
            options = new Options_Kaze();
            KazeLeft = new Feature_Kaze_CS();
            KazeRight = new Feature_Kaze_CS();
            labels = new string[] { "", "", "Left Image", "Right image with matches shown.  Blue is left view and Red is right view." };
            desc = "Find keypoints in the left and right images using KAZE algorithm.";
        }
        public void RunAlg(Mat src)
        {
            options.RunOpt();

            dst2 = vbc.task.leftView.Channels() == 1 ? vbc.task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR) : vbc.task.leftView;
            dst3 = vbc.task.rightView.Channels() == 1 ? vbc.task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR) : vbc.task.rightView;

            KazeLeft.Run(dst2);
            KazeRight.Run(dst3);

            if (KazeLeft.kazeKeyPoints == null || KazeRight.kazeKeyPoints == null) return;
            int maxCount = Math.Min(options.pointsToMatch, Math.Min(KazeLeft.kazeKeyPoints.Length, KazeRight.kazeKeyPoints.Length));

            for (int i = 0; i < maxCount; i++)
            {
                var pt1 = KazeRight.kazeKeyPoints.ElementAt(i);
                int minIndex = -1;
                float minDistance = float.MaxValue;

                for (int j = 0; j < KazeLeft.kazeKeyPoints.Length; j++)
                {
                    var pt2 = KazeLeft.kazeKeyPoints.ElementAt(j);
                    // the right image point must be to the right of the left image point (pt1 X is < pt2 X) and at about the same Y
                    if (Math.Abs(pt2.Pt.Y - pt1.Pt.Y) < 2 && pt1.Pt.X < pt2.Pt.X)
                    {
                        float distance = (float)Math.Sqrt((pt1.Pt.X - pt2.Pt.X) * (pt1.Pt.X - pt2.Pt.X) + (pt1.Pt.Y - pt2.Pt.Y) * (pt1.Pt.Y - pt2.Pt.Y));
                        // it is not enough to just be at the same height. Can't be too far away!
                        if (minDistance > distance && distance < options.maxDistance)
                        {
                            minIndex = j;
                            minDistance = distance;
                        }
                    }
                }

                if (minDistance < float.MaxValue)
                {
                    DrawCircle(dst3, pt1.Pt, vbc.task.DotSize + 2, cv.Scalar.Blue);
                    DrawCircle(dst2, pt1.Pt, vbc.task.DotSize + 2, cv.Scalar.Blue);
                    DrawCircle(dst3, KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, vbc.task.DotSize + 2, cv.Scalar.Red);
                    DrawLine(dst3, pt1.Pt, KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, cv.Scalar.Yellow, vbc.task.lineWidth);
                }
            }
        }
    }




    public class Blob_Basics_CS : VB_Parent
    {
        Blob_Input input;
        Options_Blob options;

        public Blob_Basics_CS()
        {
            options = new Options_Blob();
            input = new Blob_Input();
            UpdateAdvice(traceName + ": click 'Show All' to see all the available options.");
            desc = "Isolate and list blobs with specified options";
        }

        public void RunAlg(Mat src)
        {
            options.RunOpt();

            if (standaloneTest())
            {
                input.Run(src);
                dst2 = input.dst2;
            }
            else
            {
                dst2 = src;
            }

            var binaryImage = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            cv.Cv2.Threshold(binaryImage, binaryImage, thresh: 0, maxval: 255, type: cv.ThresholdTypes.Binary);

            var simpleBlobDetector = cv.SimpleBlobDetector.Create((cv.SimpleBlobDetector.Params)options.blobParams);
            var keypoint = simpleBlobDetector.Detect(dst2);

            cv.Cv2.DrawKeypoints(
                    image: binaryImage,
                    keypoints: keypoint,
                    outImage: dst3,
                    color: cv.Scalar.FromRgb(255, 0, 0),
                    flags: cv.DrawMatchesFlags.DrawRichKeypoints);
        }
    }



    public class Feature_SiftLeftRight_CS : VB_Parent
    {
        public Options_Sift options;
        KeyPoint[] keypoints1, keypoints2;
        public Feature_SiftLeftRight_CS()
        {
            options = new Options_Sift();
            desc = "Compare 2 images to get a homography.  We will use left and right images.";
        }

        public void RunAlg(Mat src)
        {
            options.RunOpt();
            Mat doubleSize = new Mat(dst2.Rows, dst2.Cols * 2, MatType.CV_8UC3);

            dst0 = vbc.task.leftView.Channels() == 3 ? vbc.task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY) : vbc.task.leftView;
            dst1 = vbc.task.rightView.Channels() == 3 ? vbc.task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY) : vbc.task.rightView;

            var sift = SIFT.Create(options.pointCount);

            var descriptors1 = new Mat();
            var descriptors2 = new Mat();
            sift.DetectAndCompute(dst0, null, out keypoints1, descriptors1);
            sift.DetectAndCompute(dst1, null, out keypoints2, descriptors2);

            if (options.useBFMatcher)
            {
                var bfMatcher = new BFMatcher(NormTypes.L2, false);
                DMatch[] bfMatches = bfMatcher.Match(descriptors1, descriptors2);
                Cv2.DrawMatches(dst0, keypoints1, dst1, keypoints2, bfMatches, doubleSize);
            }
            else
            {
                var flannMatcher = new FlannBasedMatcher();
                DMatch[] flannMatches = flannMatcher.Match(descriptors1, descriptors2);
                Cv2.DrawMatches(dst0, keypoints1, dst1, keypoints2, flannMatches, doubleSize);
            }

            doubleSize[new Rect(0, 0, dst0.Width, dst0.Height)].CopyTo(dst2);
            doubleSize[new Rect(dst0.Width, 0, dst0.Width, dst0.Height)].CopyTo(dst3);
        }
    }




    public class Feature_Sift_CS : VB_Parent
    {
        Options_Sift options;
        public List<cv.Point> stablePoints = new List<cv.Point>();
        List<List<cv.Point>> history = new List<List<cv.Point>>();
        public KeyPoint[] keypoints;
        public Feature_Sift_CS()
        {
            options = new Options_Sift();
            desc = "Keypoints found in SIFT";
        }

        public void RunAlg(Mat src)
        {
            options.RunOpt();

            dst2 = src.Clone();
            var sift = SIFT.Create(options.pointCount);
            var descriptors1 = new Mat();
            sift.DetectAndCompute(src.CvtColor(ColorConversionCodes.BGR2GRAY), null, out keypoints, descriptors1);

            List<cv.Point> newPoints = new List<cv.Point>();
            for (int i = 0; i < keypoints.Length; i++)
            {
                var pt = keypoints[i].Pt;
                DrawCircle(dst2, pt, vbc.task.DotSize, Scalar.Yellow);
                newPoints.Add(new cv.Point((int)pt.X, (int)pt.Y));
            }

            dst3 = src.Clone();
            if (vbc.task.optionsChanged) history.Clear();
            history.Add(newPoints);
            stablePoints.Clear();
            foreach (var pt in newPoints)
            {
                bool missing = false;
                foreach (var ptList in history)
                {
                    if (!ptList.Contains(pt))
                    {
                        missing = true;
                        break;
                    }
                }
                if (!missing)
                {
                    DrawCircle(dst3, pt, vbc.task.DotSize, Scalar.Yellow);
                    stablePoints.Add(pt);
                }
            }
            if (history.Count >= vbc.task.frameHistoryCount) history.RemoveAt(0);
            labels[3] = "Sift keypoints that are present in the last " + vbc.task.frameHistoryCount.ToString() + " frames.";
        }
    }




    // https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
    public class Feature_SiftSlices_CS : VB_Parent
    {
        Options_Sift options;

        public Feature_SiftSlices_CS()
        {
            options = new Options_Sift();
            FindSlider("Points to Match").Value = 1;
            desc = "Compare 2 images to get a homography but limit the search to a slice of the image.";
        }

        public void RunAlg(Mat src)
        {
            options.RunOpt();

            Mat doubleSize = new Mat(dst2.Rows, dst2.Cols * 2, MatType.CV_8UC3);
            int stepsize = options.stepSize;
            var descriptors1 = new Mat();
            var descriptors2 = new Mat();
            var sift = SIFT.Create(options.pointCount);
            KeyPoint[] keypoints1, keypoints2;
            for (int i = 0; i < dst2.Height; i += stepsize)
            {
                if (i + stepsize >= dst2.Height) stepsize = dst2.Height - i;
                Rect r1 = new Rect(0, i, dst2.Width, stepsize);
                Rect r2 = new Rect(0, i, dst2.Width * 2, stepsize);

                sift.DetectAndCompute(vbc.task.leftView[r1], null, out keypoints1, descriptors1);
                sift.DetectAndCompute(vbc.task.rightView[r1], null, out keypoints2, descriptors2);

                if (options.useBFMatcher)
                {
                    var bfMatcher = new BFMatcher(NormTypes.L2, false);
                    DMatch[] bfMatches = bfMatcher.Match(descriptors1, descriptors2);
                    Cv2.DrawMatches(vbc.task.leftView[r1], keypoints1, vbc.task.rightView[r1], keypoints2, bfMatches, doubleSize[r2]);
                }
                else
                {
                    var flannMatcher = new FlannBasedMatcher();
                    DMatch[] flannMatches = flannMatcher.Match(descriptors1, descriptors2);
                    Cv2.DrawMatches(vbc.task.leftView[r1], keypoints1, vbc.task.rightView[r1], keypoints2, flannMatches, doubleSize[r2]);
                }
            }

            doubleSize[new Rect(0, 0, dst2.Width, dst2.Height)].CopyTo(dst2);
            doubleSize[new Rect(dst2.Width, 0, dst2.Width, dst2.Height)].CopyTo(dst3);

            labels[2] = options.useBFMatcher ? "BF Matcher output" : "Flann Matcher output";
        }
    }
    // https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
    public class Feature_SURF_CS : VB_Parent
    {
        public Options_SURF options;
        public bool drawPoints = true;
        public List<cv.Point> stablePoints = new List<cv.Point>();
        List<List<cv.Point>> history = new List<List<cv.Point>>();
        public KeyPoint[] keypoints1, keypoints2;
        public Feature_SURF_CS()
        {
            options = new Options_SURF();
            desc = "Keypoints found in SIFT";
        }

        public void RunAlg(Mat src)
        {
            options.RunOpt();

            dst0 = vbc.task.leftView.Channels() == 3 ? vbc.task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY) : vbc.task.leftView;
            dst1 = vbc.task.rightView.Channels() == 3 ? vbc.task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY) : vbc.task.rightView;

            var surf = SURF.Create(options.hessianThreshold, 4, 2, true);
            var doubleSize = new Mat(dst0.Rows, dst0.Cols * 2, MatType.CV_8UC1);

            var descriptors1 = new Mat(); var descriptors2 = new Mat();
            surf.DetectAndCompute(dst0, null, out keypoints1, descriptors1);
            surf.DetectAndCompute(dst1, null, out keypoints2, descriptors2);

            if (options.useBFMatcher)
            {
                if (descriptors1.Rows > 0 && descriptors2.Rows > 0) // occasionally there is nothing to match!
                {
                    var bfMatcher = new BFMatcher(NormTypes.L2, false);
                    DMatch[] bfMatches = bfMatcher.Match(descriptors1, descriptors2);
                    if (drawPoints) Cv2.DrawMatches(dst0, keypoints1, dst1, keypoints2, bfMatches, doubleSize);
                }
            }
            else
            {
                var flannMatcher = new FlannBasedMatcher();
                if (descriptors1.Width > 0 && descriptors2.Width > 0)
                {
                    DMatch[] flannMatches = flannMatcher.Match(descriptors1, descriptors2);
                    if (drawPoints) Cv2.DrawMatches(dst0, keypoints1, dst1, keypoints2, flannMatches, doubleSize);
                }
            }
            doubleSize[new Rect(0, 0, dst0.Width, dst0.Height)].CopyTo(dst2);
            doubleSize[new Rect(dst0.Width, 0, dst0.Width, dst0.Height)].CopyTo(dst3);
        }
    }


    // https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
    public class Feature_SURF_Draw_CS : VB_Parent
    {
        Feature_SURF_CS surf;

        public Feature_SURF_Draw_CS()
        {
            surf = new Feature_SURF_CS();
            surf.drawPoints = false;
            desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices.";
        }

        public void RunAlg(Mat src)
        {
            dst2 = vbc.task.leftView.Channels() == 1 ? vbc.task.leftView.CvtColor(ColorConversionCodes.GRAY2BGR) : vbc.task.leftView;
            dst3 = vbc.task.rightView.Channels() == 1 ? vbc.task.rightView.CvtColor(ColorConversionCodes.GRAY2BGR) : vbc.task.rightView;

            surf.Run(src);

            KeyPoint[] keys1 = surf.keypoints1;
            KeyPoint[] keys2 = surf.keypoints2;

            for (int i = 0; i < keys1.Length; i++)
            {
                var pt = new cv.Point(keys1[i].Pt.X, keys1[i].Pt.Y);
                Cv2.Circle(dst2, pt, vbc.task.DotSize + 2, Scalar.Red);
            }

            int matchCount = 0;
            int range = surf.options.verticalRange;
            for (int i = 0; i < keys1.Length; i++)
            {
                for (int j = 0; j < keys2.Length; j++)
                {
                    var pt = new cv.Point(keys2[j].Pt.X, keys2[j].Pt.Y);
                    if (Math.Abs(keys2[j].Pt.Y - keys1[i].Pt.Y) < range)
                    {
                        Cv2.Circle(dst3, pt, vbc.task.DotSize + 2, vbc.task.HighlightColor);
                        keys2[j].Pt.Y = -1; // so we don't match it again.
                        matchCount++;
                    }
                }
            }

            // mark those that were not
            for (int i = 0; i < keys2.Length; i++)
            {
                var pt = new cv.Point(keys2[i].Pt.X, keys2[i].Pt.Y);
                if (pt.Y != -1)
                {
                    Cv2.Circle(dst3, pt, vbc.task.DotSize + 2, Scalar.Red);
                }
            }

            labels[3] = "Yellow matched left to right = " + matchCount.ToString() + ". Red is unmatched.";
        }
    }




    public class OilPaint_Manual_CS : VB_Parent
    {
        CS_Classes.OilPaintManual oilPaint = new CS_Classes.OilPaintManual();
        public Options_OilPaint options = new Options_OilPaint();

        public OilPaint_Manual_CS()
        {
            vbc.task.drawRect = new Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8);
            labels[3] = "Selected area only";
            desc = "Alter an image so it appears painted by a pointilist. Select a region of interest to paint.";
        }

        public void RunAlg(Mat src)
        {
            options.RunOpt();
            Rect roi = ValidateRect(vbc.task.drawRect);
            src.CopyTo(dst2);
            oilPaint.Start(new Mat(src, roi), new Mat(dst2, roi), options.kernelSize, options.intensity);
            dst3 = src.EmptyClone().SetTo(Scalar.All(0));
            int factor = Math.Min((int)Math.Floor((double)dst3.Width / roi.Width), (int)Math.Floor((double)dst3.Height / roi.Height));
            cv.Size s = new cv.Size(roi.Width * factor, roi.Height * factor);
            Cv2.Resize(new Mat(dst2, roi), new Mat(dst3, new Rect(0, 0, s.Width, s.Height)), s);
        }
    }



    public class OilPaint_Cartoon_CS : VB_Parent
    {
        OilPaint_Manual_CS oil;
        Edge_Laplacian Laplacian = new Edge_Laplacian();

        public OilPaint_Cartoon_CS()
        {
            oil = new OilPaint_Manual_CS();
            vbc.task.drawRect = new Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8);
            labels[2] = "OilPaint_Cartoon";
            labels[3] = "Laplacian Edges";
            desc = "Alter an image so it appears more like a cartoon";
        }

        public void RunAlg(Mat src)
        {
            var roi = ValidateRect(vbc.task.drawRect);
            Laplacian.Run(src);
            dst3 = Laplacian.dst2;

            oil.Run(src);
            dst2 = oil.dst2;

            var vec000 = new Vec3b(0, 0, 0);
            for (int y = 0; y < roi.Height; y++)
            {
                for (int x = 0; x < roi.Width; x++)
                {
                    if (dst3[roi].Get<byte>(y, x) >= oil.options.threshold)
                    {
                        dst2[roi].Set(y, x, vec000);
                    }
                }
            }
        }
    }



    public class SLR_Basics_CS : VB_Parent
    {
        public SLR_PlotTest slrInput = new SLR_PlotTest();
        SLR slr = new SLR();
        Plot_Basics_CPP_VB plot = new Plot_Basics_CPP_VB();
        Options_SLR options = new Options_SLR();
        public SLR_Basics_CS()
        {
            desc = "Segmented Linear Regression example";
        }
        public void RunAlg(Mat src)
        {
            options.RunOpt();
            if (vbc.task.FirstPass && standalone)
            {
                slrInput.RunAlg(dst2);
                labels[2] = "Sample data slrInput";
            }

            var resultX = new List<double>();
            var resultY = new List<double>();

            slr.SegmentedRegressionFast(slrInput.dataX, slrInput.dataY, options.tolerance, options.halfLength, resultX, resultY);

            labels[2] = "Tolerance = " + options.tolerance.ToString() + " and moving average window = " + options.halfLength.ToString();
            if (resultX.Count > 0)
            {
                plot.srcX = slrInput.dataX;
                plot.srcY = slrInput.dataY;
                plot.Run(src);
                dst2 = plot.dst2.Clone();

                plot.srcX = resultX;
                plot.srcY = resultY;
                plot.Run(src);
                dst3 = plot.dst2;
            }
            else
            {
                dst2.SetTo(0);
                dst3.SetTo(0);
                SetTrueText(labels[2] + " yielded no results...");
            }
            if (!standaloneTest())
            {
                slrInput.dataX.Clear();
                slrInput.dataY.Clear();
            }
        }
    }



    public class SLR_DepthHist_CS : VB_Parent
    {
        public SLR_Basics_CS slr;
        public Hist_Kalman kalman;

        public SLR_DepthHist_CS()
        {
            vbc.task.gOptions.setHistogramBins(32);
            slr = new SLR_Basics_CS();
            kalman = new Hist_Kalman();
            labels = new string[] { "", "", "Original data", "Segmented Linear Regression (SLR) version of the same data.  Red line is zero." };
            desc = "Run Segmented Linear Regression on depth data";
        }

        public void RunAlg(Mat src)
        {
            kalman.Run(src);
            kalman.hist.histogram.Set<float>(0, 0, 0);
            dst2 = kalman.dst2;
            for (int i = 0; i < kalman.hist.histogram.Rows; i++)
            {
                slr.slrInput.dataX.Add(i);
                slr.slrInput.dataY.Add(kalman.hist.histogram.Get<float>(i, 0));
            }
            slr.Run(src);
            dst3 = slr.dst3;
        }
    }



    public class OEX_Sobel_Demo_CS : VB_Parent
    {
        Edge_Sobel_CS sobel;

        public OEX_Sobel_Demo_CS()
        {
            sobel = new Edge_Sobel_CS();
            desc = "OpenCV Example Sobel_Demo became Edge_Sobel algorithm.";
        }

        public void RunAlg(Mat src)
        {
            sobel.Run(src);
            dst2 = sobel.dst2;
            dst3 = sobel.dst3;
            labels = sobel.labels;
        }
    }



    // https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
    public class Voronoi_Basics_CS : VB_Parent
    {
        public VoronoiDemo vDemo = new VoronoiDemo();
        public Random_Basics_CS random;

        public Voronoi_Basics_CS()
        {
            random = new Random_Basics_CS();
            labels[2] = "Ordered list output for Voronoi algorithm";
            FindSlider("Random Pixel Count").Maximum = 100;
            desc = "Use the ordered list method to find the Voronoi segments";
        }

        public void vDisplay(ref Mat dst, List<Point2f> points, Scalar color)
        {
            dst = dst.Normalize(255).ConvertScaleAbs(255);
            dst = dst.CvtColor(ColorConversionCodes.GRAY2BGR);

            foreach (var pt in points)
            {
                Cv2.Circle(dst, (cv.Point)pt, vbc.task.DotSize, color, -1);
            }
        }

        public void RunAlg(Mat src)
        {
            if (vbc.task.heartBeat) random.Run(empty);
            vDemo.RunAlg(ref dst2, random.PointList);
            vDisplay(ref dst2, random.PointList, Scalar.Yellow);
        }
    }

    // https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
    public class Voronoi_Compare_CS : VB_Parent
    {
        Voronoi_Basics_CS basics;
        public Random_Basics random = new Random_Basics();

        public Voronoi_Compare_CS()
        {
            basics = new Voronoi_Basics_CS();
            FindSlider("Random Pixel Count").Maximum = 150;
            FindSlider("Random Pixel Count").Value = 150;
            labels = new string[] { "", "", "Brute Force method - check log timings", "Ordered List method - check log for timing" };
            desc = "C# implementations of the BruteForce and OrderedList Voronoi algorithms";
        }

        public void RunAlg(Mat src)
        {
            random.Run(empty);
            basics.vDemo.RunAlg(ref dst2, random.PointList, true);
            basics.vDisplay(ref dst2, random.PointList, Scalar.Yellow);

            basics.vDemo.RunAlg(ref dst3, random.PointList, false);
            basics.vDisplay(ref dst3, random.PointList, Scalar.Yellow);
        }
    }

    // https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
    public class Voronoi_CS : VB_Parent
    {
        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr VoronoiDemo_Open(string matlabFileName, int rows, int cols);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr VoronoiDemo_Close(IntPtr cPtr);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr VoronoiDemo_Run(IntPtr pfPtr, IntPtr Input, int pointCount, int width, int height);

        Voronoi_Basics_CS vDemo;
        public Voronoi_CS()
        {
            vDemo = new Voronoi_Basics_CS();
            cPtr = VoronoiDemo_Open(vbc.task.HomeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols);
            desc = "Use the C++ version of the Voronoi code";
        }

        public void RunAlg(Mat src)
        {
            var countSlider = FindSlider("Random Pixel Count");
            if (vbc.task.heartBeat) vDemo.random.Run(empty);
            List<cv.Point> ptList = new List<cv.Point>();
            for (int i = 0; i < vDemo.random.PointList.Count; i++)
            {
                var pt = vDemo.random.PointList[i];
                ptList.Add(new cv.Point(pt.X, pt.Y));
            }
            var handleSrc = GCHandle.Alloc(ptList.ToArray(), GCHandleType.Pinned);
            var imagePtr = VoronoiDemo_Run(cPtr, handleSrc.AddrOfPinnedObject(), countSlider.Value, dst2.Width, dst2.Height);
            handleSrc.Free();

            dst2 = cv.Mat.FromPixelData(dst2.Rows, dst2.Cols, MatType.CV_32F, imagePtr).Clone();
            vDemo.vDisplay(ref dst2, vDemo.random.PointList, Scalar.Yellow);
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = VoronoiDemo_Close(cPtr);
        }
    }

    public class Edge_Motion_CS : VB_Parent
    {
        Diff_Basics diff = new Diff_Basics();
        Edge_Sobel_CS edges;

        public Edge_Motion_CS()
        {
            edges = new Edge_Sobel_CS();
            labels[2] = "Wave at camera to see impact or move camera.";
            desc = "Measure camera motion using Sobel and Diff from last frame.";
        }

        public void RunAlg(Mat src)
        {
            edges.Run(src);
            diff.Run(edges.dst2);

            dst2 = diff.dst2;
            dst3 = dst2 & edges.dst2;
            if (vbc.task.quarterBeat) labels[3] = $"{dst3.CountNonZero()} pixels overlap between Sobel edges and diff with last Sobel edges.";
        }
    }


    public class Edge_NoDepth_CS : VB_Parent
    {
        Edge_Sobel_CS edges;
        Blur_Basics_CS blur;
        public Edge_NoDepth_CS()
        {
            edges = new Edge_Sobel_CS();
            blur = new Blur_Basics_CS();
            labels[2] = "Edges found in the regions with no depth";
            labels[3] = "Mask of regions with no depth - blurred to expand slightly.";
            desc = "Find the edges in regions without depth.";
        }
        public void RunAlg(Mat src)
        {
            edges.Run(src);
            dst2 = edges.dst2;

            blur.Run(vbc.task.noDepthMask);
            dst3.SetTo(0);
            dst2.CopyTo(dst3, blur.dst2);
            dst3 = dst3.Threshold(0, 255, ThresholdTypes.Binary);
            dst2.SetTo(0, ~dst3);
        }
    }









    public class Sieve_Basics_CS : VB_Parent
    {
        Sieve_BasicsVB printer = new Sieve_BasicsVB();
        Sieve sieve = new Sieve();

        public Sieve_Basics_CS()
        {
            desc = "Implement the Sieve of Eratothenes in C#";
        }

        public void RunAlg(Mat src)
        {
            var countSlider = FindSlider("Count of desired primes");
            SetTrueText(printer.shareResults(sieve.GetPrimeNumbers(countSlider.Value)));
        }
    }



    // https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
    public class SLR_Image_CS : VB_Parent
    {
        public SLR_Basics_CS slr;
        public Hist_Basics hist = new Hist_Basics();

        public SLR_Image_CS()
        {
            vbc.task.gOptions.setHistogramBins(32);
            slr = new SLR_Basics_CS();
            labels[2] = "Original data";
            desc = "Run Segmented Linear Regression on grayscale image data - just an experiment";
        }

        public void RunAlg(Mat src)
        {
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            hist.Run(src);
            dst2 = hist.dst2;
            for (int i = 0; i < hist.histogram.Rows; i++)
            {
                slr.slrInput.dataX.Add(i);
                slr.slrInput.dataY.Add(hist.histogram.Get<float>(i, 0));
            }
            slr.Run(src);
            dst3 = slr.dst3;
        }
    }


    public class SLR_TrendCompare_CS : VB_Parent
    {
        public SLR_Image_CS slr;
        List<float> valList = new List<float>();
        int barMidPoint;
        Point2f lastPoint;
        public List<Point2f> resultingPoints = new List<Point2f>();

        public SLR_TrendCompare_CS()
        {
            slr = new SLR_Image_CS();
            desc = "Find trends by filling in short histogram gaps in the given image's histogram.";
        }

        void connectLine(int i)
        {
            var p1 = new Point2f(barMidPoint + dst2.Width * i / valList.Count, 
                                 dst2.Height - dst2.Height * valList[i] / slr.hist.plot.maxRange);
            resultingPoints.Add(p1);
            Cv2.Line(dst2, (int)lastPoint.X, (int)lastPoint.Y, (int)p1.X, (int)p1.Y, Scalar.Yellow, vbc.task.lineWidth + 1, vbc.task.lineType);
            lastPoint = p1;
        }

        public void RunAlg(Mat src)
        {
            labels[2] = "Histogram with Yellow line showing the trends";
            slr.hist.plot.backColor = Scalar.Red;
            slr.Run(src);
            dst2 = slr.dst2;
            dst3 = slr.dst3;

            var indexer = slr.hist.histogram.GetGenericIndexer<float>();
            valList = new List<float>();
            for (int i = 0; i < slr.hist.histogram.Rows; i++)
            {
                valList.Add(indexer[i]);
            }
            barMidPoint = dst2.Width / valList.Count / 2;

            if (valList.Count < 2) return;
            slr.hist.plot.maxRange = valList.Max();
            lastPoint = new Point2f(barMidPoint, dst2.Height - dst2.Height * valList[0] / slr.hist.plot.maxRange);
            resultingPoints.Clear();
            resultingPoints.Add(lastPoint);
            for (int i = 1; i < valList.Count - 1; i++)
            {
                if (valList[i - 1] > valList[i] && valList[i + 1] > valList[i])
                    valList[i] = (valList[i - 1] + valList[i + 1]) / 2;
                connectLine(i);
            }
            connectLine(valList.Count - 1);
        }
    }


    public class Feature_SURFMatch_CS : VB_Parent
    {
        Feature_SURF_CS surf;
        public Feature_SURFMatch_CS()
        {
            surf = new Feature_SURF_CS();
            surf.drawPoints = false;
            desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices.";
        }

        public void RunAlg(Mat src)
        {
            surf.Run(src);

            Mat doublesize = new Mat();
            Cv2.HConcat(new List<Mat> { vbc.task.leftView, vbc.task.rightView }, doublesize);

            var keys1 = surf.keypoints1;
            var keys2 = surf.keypoints2;

            for (int i = 0; i < keys1.Length; i++)
            {
                DrawCircle(dst2, keys1[i].Pt, vbc.task.DotSize + 3, Scalar.Red);
            }

            int matchCount = 0;
            float range = surf.options.verticalRange;
            for (int i = 0; i < keys1.Length; i++)
            {
                Point2f p1 = keys1[i].Pt;
                for (int j = 0; j < keys2.Length; j++)
                {
                    Point2f p2 = keys2[j].Pt;
                    p2 = new Point2f(p2.X + dst2.Width, p2.Y);
                    if (Math.Abs(keys2[j].Pt.Y - p1.Y) < range)
                    {
                        cv.Point pt1 = new cv.Point(p1.X, p1.Y);
                        cv.Point pt2 = new cv.Point(p2.X, p2.Y);
                        doublesize.Line(pt1, pt2, vbc.task.HighlightColor, vbc.task.lineWidth);
                        keys2[j].Pt = new Point2f(keys2[j].Pt.X, -1); // so we don't match it again.
                        matchCount++;
                    }
                }
            }

            doublesize[new Rect(0, 0, src.Width, src.Height)].CopyTo(dst2);
            doublesize[new Rect(src.Width, 0, src.Width, src.Height)].CopyTo(dst3);

            labels[2] = surf.keypoints1.Length + " key points found in the left view - " +
                        (surf.options.useBFMatcher ? "BF Matcher output " : "Flann Matcher output ");
            labels[3] = surf.keypoints2.Length + " key points found in the right view " + matchCount + " were matched.";
        }
    }

    public class DNN_Caffe_CS : VB_Parent
    {
        DNN caffeCS = new DNN();
        public DNN_Caffe_CS()
        {
            labels[3] = "Input Image";
            desc = "Download and use a Caffe database";

            string protoTxt = vbc.task.HomeDir + "Data/bvlc_googlenet.prototxt";
            string modelFile = vbc.task.HomeDir + "Data/bvlc_googlenet.caffemodel";
            string synsetWords = vbc.task.HomeDir + "Data/synset_words.txt";
            caffeCS.initialize(protoTxt, modelFile, synsetWords);
        }

        public void RunAlg(Mat src)
        {
            Mat image = Cv2.ImRead(vbc.task.HomeDir + "Data/space_shuttle.jpg");
            string str = caffeCS.RunAlg(image);
            dst3 = image.Resize(dst3.Size());
            SetTrueText(str);
        }
    }

    public class Dither_Basics_CS : VB_Parent
    {
        Options_Dither options = new Options_Dither();

        public Dither_Basics_CS()
        {
            labels = new string[] { "", "", "Dither applied to the BGR image", "Dither applied to the Depth image" };
            UpdateAdvice(traceName + ": use local options to control which method is used.");
            desc = "Explore all the varieties of dithering";
        }

        public void RunAlg(Mat src)
        {
            options.RunOpt();

            int w = dst2.Width;
            int h = dst2.Height;
            int nColors = new int[] { 1, 3, 7, 15, 31 }[options.bppIndex]; // indicate 3, 6, 9, 12, 15 bits per pixel.
            byte[] pixels = new byte[dst2.Total() * dst2.ElemSize()];
            GCHandle hpixels = GCHandle.Alloc(pixels, GCHandleType.Pinned);

            for (int i = 0; i < 2; i++)
            {
                Mat copySrc = (i == 0) ? src : vbc.task.depthRGB;
                Marshal.Copy(copySrc.Data, pixels, 0, pixels.Length);

                switch (options.radioIndex)
                {
                    case 0:
                        ditherBayer16(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 1:
                        ditherBayer8(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 2:
                        ditherBayer4(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 3:
                        ditherBayer3(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 4:
                        ditherBayer2(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 5:
                        ditherBayerRgbNbpp(hpixels.AddrOfPinnedObject(), w, h, nColors);
                        break;
                    case 6:
                        ditherBayerRgb3bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 7:
                        ditherBayerRgb6bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 8:
                        ditherBayerRgb9bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 9:
                        ditherBayerRgb12bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 10:
                        ditherBayerRgb15bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 11:
                        ditherBayerRgb18bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 12:
                        ditherFSRgbNbpp(hpixels.AddrOfPinnedObject(), w, h, nColors);
                        break;
                    case 13:
                        ditherFS(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 14:
                        ditherFSRgb3bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 15:
                        ditherFSRgb6bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 16:
                        ditherFSRgb9bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 17:
                        ditherFSRgb12bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 18:
                        ditherFSRgb15bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 19:
                        ditherFSRgb18bpp(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 20:
                        ditherSierraLiteRgbNbpp(hpixels.AddrOfPinnedObject(), w, h, nColors);
                        break;
                    case 21:
                        ditherSierraLite(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                    case 22:
                        ditherSierraRgbNbpp(hpixels.AddrOfPinnedObject(), w, h, nColors);
                        break;
                    case 23:
                        ditherSierra(hpixels.AddrOfPinnedObject(), w, h);
                        break;
                }

                if (i == 0)
                {
                    dst2 = cv.Mat.FromPixelData(src.Height, src.Width, MatType.CV_8UC3, pixels).Clone();
                }
                else
                {
                    dst3 = cv.Mat.FromPixelData(src.Height, src.Width, MatType.CV_8UC3, pixels).Clone();
                }
            }

            hpixels.Free();
        }
        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer16(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer8(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer4(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer3(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayer2(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgbNbpp(IntPtr pixels, int width, int height, int nColors);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb3bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb6bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb9bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb12bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb15bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherBayerRgb18bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgbNbpp(IntPtr pixels, int width, int height, int nColors);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFS(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb3bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb6bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb9bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb12bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb15bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherFSRgb18bpp(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherSierraLiteRgbNbpp(IntPtr pixels, int width, int height, int nColors);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherSierraLite(IntPtr pixels, int width, int height);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherSierraRgbNbpp(IntPtr pixels, int width, int height, int nColors);

        [DllImport("CPP_Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ditherSierra(IntPtr pixels, int width, int height);
    }



    public class DNN_Test_CS : VB_Parent
    {
        Net net;
        string[] classnames;
        public DNN_Test_CS()
        {
            var modelFile = new FileInfo(vbc.task.HomeDir + "Data/bvlc_googlenet.caffemodel");
            if (!File.Exists(modelFile.FullName))
            {
                var client = WebRequest.CreateHttp("http://dl.caffe.berkeleyvision.org/bvlc_googlenet.caffemodel");
                var response = client.GetResponse();
                var responseStream = response.GetResponseStream();
                var memory = new MemoryStream();
                responseStream.CopyTo(memory);
                File.WriteAllBytes(modelFile.FullName, memory.ToArray());
            }
            var protoTxt = vbc.task.HomeDir + "Data/bvlc_googlenet.prototxt";
            net = CvDnn.ReadNetFromCaffe(protoTxt, modelFile.FullName);
            var synsetWords = vbc.task.HomeDir + "Data/synset_words.txt";
            classnames = File.ReadAllLines(synsetWords);
            for (int i = 0; i < classnames.Length; i++)
            {
                classnames[i] = classnames[i].Split(' ').Last();
            }
            labels[3] = "Input Image";
            desc = "Download and use a Caffe database";
        }
        public void RunAlg(Mat src)
        {
            var image = Cv2.ImRead(vbc.task.HomeDir + "Data/space_shuttle.jpg");
            dst3 = image.Resize(dst3.Size());
            var inputBlob = CvDnn.BlobFromImage(image, 1, new OpenCvSharp.Size(224, 224), new Scalar(104, 117, 123));
            net.SetInput(inputBlob, "data");
            var prob = net.Forward("prob");
            var mm = GetMinMax(prob.Reshape(1, 1));
            SetTrueText("Best classification: index = " + mm.maxLoc.X + " which is for '" + classnames[mm.maxLoc.X] + "' with Probability " +
                        $"{mm.maxVal:0.00%}", new cv.Point(40, 200));
        }
    }
    public class DNN_Basics_CS : VB_Parent
    {

        Net net;
        bool dnnPrepared;
        Rect crop;
        int dnnWidth;
        int dnnHeight;
        Kalman_Basics[] kalman;
        public Rect rect;
        Options_DNN options = new Options_DNN();
        string[] classNames = { "background", "aeroplane", "bicycle", "bird", "boat", "bottle", "bus", "car", "cat", "chair", "cow", "diningtable", "dog", "horse",
                        "motorbike", "person", "pottedplant", "sheep", "sofa", "train", "tvmonitor" };
        public DNN_Basics_CS()
        {
            kalman = new Kalman_Basics[10];
            for (int i = 0; i < kalman.Length; i++)
            {
                kalman[i] = new Kalman_Basics();
                Array.Resize(ref kalman[i].kInput, 4);
                Array.Resize(ref kalman[i].kOutput, 4);
            }
            dnnWidth = dst2.Height; // height is always smaller than width...
            dnnHeight = dst2.Height;
            crop = new Rect(dst2.Width / 2 - dnnWidth / 2, dst2.Height / 2 - dnnHeight / 2, dnnWidth, dnnHeight);
            var infoText = new FileInfo(vbc.task.HomeDir + "Data/MobileNetSSD_deploy.prototxt");
            if (infoText.Exists)
            {
                var infoModel = new FileInfo(vbc.task.HomeDir + "Data/MobileNetSSD_deploy.caffemodel");
                if (infoModel.Exists)
                {
                    net = CvDnn.ReadNetFromCaffe(infoText.FullName, infoModel.FullName);
                    dnnPrepared = true;
                }
            }
            if (!dnnPrepared)
            {
                SetTrueText("Caffe databases not found.  It should be in <OpenCVB_HomeDir>/Data.");
            }
            desc = "Use OpenCV's dnn from Caffe file.";
            labels[2] = "Cropped Input Image - must be square!";
        }
        public void RunAlg(Mat src)
        {
            options.RunOpt();

            if (dnnPrepared)
            {
                var inScaleFactor = options.ScaleFactor / options.scaleMax; // should be 0.0078 by default...
                var inputBlob = CvDnn.BlobFromImage(src, inScaleFactor, new OpenCvSharp.Size(300, 300), 
                                                    cv.Scalar.All(options.meanValue), false);
                src.CopyTo(dst3);
                src[crop].CopyTo(dst2[crop]);
                net.SetInput(inputBlob, "data");
                Mat detection = net.Forward("detection_out");
                //    Mat detectionMat = new Mat(detection.Size[2], detection.Size[3], MatType.CV_32F, detection.Data);
                //    var confidenceThreshold = (float)confidenceSlider.Value / 100;
                //    var rows = src[crop].Rows;
                //    var cols = src[crop].Cols;
                //    labels[3] = "";
                //    var kPoints = new List<cv.Point>();
                //    for (int i = 0; i < detectionMat.Rows; i++)
                //    {
                //        var confidence = detectionMat.Get<float>(i, 2);
                //        if (confidence > options.confidenceThreshold)
                //        {
                //            var vec = detectionMat.Get<Vec4f>(i, 3);
                //            if (kalman[i].kInput[0] == 0 && kalman[i].kInput[1] == 0)
                //            {
                //                kPoints.Add(new cv.Point(vec[0] * cols + crop.Left, vec[1] * rows + crop.Top));
                //            }
                //            else
                //            {
                //                kPoints.Add(new cv.Point(kalman[i].kInput[0], kalman[i].kInput[1]));
                //            }
                //        }
                //    }
                //    if (kPoints.Count > activeKalman) activeKalman = kPoints.Count;
                //    for (int i = 0; i < detectionMat.Rows; i++)
                //    {
                //        var confidence = detectionMat.Get<float>(i, 2);
                //        if (confidence > options.confidenceThreshold)
                //        {
                //            var nextName = classNames[(int)detectionMat.Get<float>(i, 1)];
                //            labels[3] += nextName + " "; // display the name of what we found.
                //            var vec = detectionMat.Get<Vec4f>(i, 3);
                //            rect = new Rect(vec[0] * cols + crop.Left, vec[1] * rows + crop.Top, (vec[2] - vec[0]) * cols, (vec[3] - vec[1]) * rows);
                //            rect = new Rect(rect.X, rect.Y, Math.Min(dnnWidth, rect.Width), Math.Min(dnnHeight, rect.Height));
                //            var pt = new Point(rect.X, rect.Y);
                //            var minIndex = 0;
                //            var minDistance = float.MaxValue;
                //            for (int j = 0; j < kPoints.Count; j++)
                //            {
                //                var distance = Math.Sqrt((pt.X - kPoints[j].X) * (pt.X - kPoints[j].X) + (pt.Y - kPoints[j].Y) * (pt.Y - kPoints[j].Y));
                //                if (minDistance > distance)
                //                {
                //                    minIndex = j;
                //                    minDistance = distance;
                //                }
                //            }
                //            if (minIndex < kalman.Length)
                //            {
                //                kalman[minIndex].kInput = new float[] { rect.X, rect.Y, rect.Width, rect.Height };
                //                kalman[minIndex].Run(src);
                //                rect = new Rect(kalman[minIndex].kOutput[0], kalman[minIndex].kOutput[1], kalman[minIndex].kOutput[2], kalman[minIndex].kOutput[3]);
                //            }
                //            dst3.Rectangle(rect, Scalar.Yellow, vbc.task.lineWidth + 2, vbc.task.lineType);
                //            rect.Width = src.Width / 12;
                //            rect.Height = src.Height / 16;
                //            dst3.Rectangle(rect, Scalar.Black, -1);
                //            SetTrueText(nextName, new Point(rect.X, rect.Y), 3);
                //        }
                //    }
                //    // reinitialize any unused kalman filters.
                //    for (int i = kPoints.Count; i < activeKalman; i++)
                //    {
                //        if (i < kalman.Length)
                //        {
                //            kalman[i].kInput[0] = 0;
                //            kalman[i].kInput[1] = 0;
                //        }
                //    }
            }
        }
    }
    public class DNN_SuperRes_CS : VB_Parent
    {
        public Options_DNN options = new Options_DNN();
        public DnnSuperResImpl dnn;
        string saveModelFile;
        int multiplier;
        public DNN_SuperRes_CS()
        {
            vbc.task.drawRect = new Rect(10, 10, 20, 20);
            labels[2] = "Output of a resize using OpenCV";
            desc = "Get better super-resolution through a DNN";
        }
        public void RunAlg(Mat src)
        {
            options.RunOpt();
            if (saveModelFile != options.superResModelFileName)
            {
                saveModelFile = options.superResModelFileName;
                multiplier = options.superResMultiplier;
                dnn = new DnnSuperResImpl(options.shortModelName, multiplier);
                dnn.ReadModel(saveModelFile);
            }
            var r = vbc.task.drawRect;
            if (r.Width == 0 || r.Height == 0) return;
            var outRect = new Rect(0, 0, r.Width * multiplier, r.Height * multiplier);
            if (outRect.Width > dst3.Width)
            {
                r.Width = dst3.Width / multiplier;
                outRect.Width = dst3.Width;
            }
            if (outRect.Height > dst3.Height)
            {
                r.Height = dst3.Height / multiplier;
                outRect.Height = dst3.Height;
            }
            dst2.SetTo(0);
            dst3.SetTo(0);
            dst2[outRect] = src[r].Resize(new OpenCvSharp.Size(r.Width * multiplier, r.Height * multiplier));
            dnn.Upsample(src[r], dst3[outRect]);
            labels[3] = $"{multiplier}X resize of selected area using DNN super resolution";
        }
    }
    //public class DNN_SuperRes_CSize : VB_Parent
    //{
    //    DNN_SuperRes super;
    //    public DNN_SuperRes_CSize()
    //    {
    //        labels[2] = "Super Res resized back to original size";
    //        labels[3] = "dst3 = dst2 - src or no difference - honors original";
    //        desc = "Compare superRes reduced to original size";
    //    }
    //    public void RunAlg(Mat src)
    //    {
    //        super.Run(src);
    //        var r = new Rect(0, 0, dst2.Width, dst2.Height);
    //        var tmp = new Mat();
    //        super.dnn.Upsample(src, tmp);
    //        dst2 = tmp.Resize(dst2.Size());
    //        dst3 = dst2 - src;
    //    }
    //}





    public class Feature_Sample_CS : VB_Parent
    {
        public Mat img1, img2;
        KeyPoint[] keypoints1, keypoints2;
        public Feature_Sample_CS()
        {
            img1 = cv.Cv2.ImRead(vbc.task.HomeDir + "opencv/Samples/Data/box.png", cv.ImreadModes.Color);
            img2 = cv.Cv2.ImRead(vbc.task.HomeDir + "opencv/Samples/Data/box_in_scene.png", cv.ImreadModes.Color);
            desc = "Match keypoints in 2 photos using KAZE.";
        }
        public static Point2d Point2fToPoint2d(Point2f pf)
        {
            return new Point2d(((int)pf.X), ((int)pf.Y));
        }
        static Point2d[] MyPerspectiveTransform1(Point2f[] yourData, Mat transformationMatrix)
        {
            using (Mat src = cv.Mat.FromPixelData(yourData.Length, 1, MatType.CV_32FC2, yourData))
            using (Mat output = new Mat())
            {
                Cv2.PerspectiveTransform(src, output, transformationMatrix);
                Point2f[] dstArray = new Point2f[output.Rows * output.Cols];
                output.GetArray(out dstArray);
                Point2d[] result = Array.ConvertAll(dstArray, Point2fToPoint2d);
                return result;
            }
        }

        // fixed FromArray behavior
        static Point2d[] MyPerspectiveTransform2(Point2f[] yourData, Mat transformationMatrix)
        {
            using (var s = Mat<Point2f>.FromArray(yourData))
            using (var d = new Mat<Point2f>())
            {
                Cv2.PerspectiveTransform(s, d, transformationMatrix);
                Point2f[] f = d.ToArray();
                return f.Select(Point2fToPoint2d).ToArray();
            }
        }

        // new API
        private Point2d[] MyPerspectiveTransform3(Point2f[] yourData, Mat transformationMatrix)
        {
            Point2f[] ret = Cv2.PerspectiveTransform(yourData, transformationMatrix);
            return ret.Select(Point2fToPoint2d).ToArray();
        }

        private int VoteForSizeAndOrientation(KeyPoint[] modelKeyPoints, KeyPoint[] observedKeyPoints, DMatch[][] matches, Mat mask, float scaleIncrement, int rotationBins)
        {
            int idx = 0;
            int nonZeroCount = 0;
            byte[] maskMat = new byte[mask.Rows];
            GCHandle maskHandle = GCHandle.Alloc(maskMat, GCHandleType.Pinned);
            using (Mat m = cv.Mat.FromPixelData(mask.Rows, 1, MatType.CV_8U, maskHandle.AddrOfPinnedObject()))
            {
                mask.CopyTo(m);
                List<float> logScale = new List<float>();
                List<float> rotations = new List<float>();
                double s, maxS, minS, r;
                maxS = -1.0e-10f; minS = 1.0e10f;

                //if you get an exception here, it's because you're passing in the model and observed keypoints backwards.  Just switch the order.
                for (int i = 0; i < maskMat.Length; i++)
                {
                    if (maskMat[i] > 0)
                    {
                        KeyPoint observedKeyPoint = observedKeyPoints[i];
                        KeyPoint modelKeyPoint = modelKeyPoints[matches[i][0].TrainIdx];
                        s = Math.Log10(observedKeyPoint.Size / modelKeyPoint.Size);
                        logScale.Add((float)s);
                        maxS = s > maxS ? s : maxS;
                        minS = s < minS ? s : minS;

                        r = observedKeyPoint.Angle - modelKeyPoint.Angle;
                        r = r < 0.0f ? r + 360.0f : r;
                        rotations.Add((float)r);
                    }
                }

                int scaleBinSize = (int)Math.Ceiling((maxS - minS) / Math.Log10(scaleIncrement));
                if (scaleBinSize < 2)
                    scaleBinSize = 2;
                float[] scaleRanges = { (float)minS, (float)(minS + scaleBinSize + Math.Log10(scaleIncrement)) };

                using (var scalesMat = cv.Mat.FromPixelData(rows: logScale.Count, cols: 1, cv.MatType.CV_32F, data: logScale.ToArray()))
                using (var rotationsMat = cv.Mat.FromPixelData(rows: rotations.Count, cols: 1, cv.MatType.CV_32F, data: rotations.ToArray()))
                using (var flagsMat = new Mat<float>(logScale.Count, 1))
                using (Mat hist = new Mat())
                {
                    flagsMat.SetTo(new Scalar(0.0f));
                    float[] flagsMatFloat1 = flagsMat.ToArray();

                    int[] histSize = { scaleBinSize, rotationBins };
                    float[] rotationRanges = { 0.0f, 360.0f };
                    int[] channels = { 0, 1 };
                    // with infrared left and right, rotation max = min and calchist fails.  Adding 1 to max enables all this to work!
                    if (rotations.Count > 0)
                    {
                        Rangef[] ranges = { new Rangef(scaleRanges[0], scaleRanges[1]), new Rangef(rotations.Min(), rotations.Max() + 1) };
                        double minVal, maxVal;

                        Mat[] arrs = { scalesMat, rotationsMat };

                        Cv2.CalcHist(arrs, channels, null, hist, 2, histSize, ranges);
                        Cv2.MinMaxLoc(hist, out minVal, out maxVal);

                        Cv2.Threshold(hist, hist, maxVal * 0.5, 0, ThresholdTypes.Tozero);
                        Cv2.CalcBackProject(arrs, channels, hist, flagsMat, ranges);

                        MatIndexer<float> flagsMatIndexer = flagsMat.GetIndexer();

                        for (int i = 0; i < maskMat.Length; i++)
                        {
                            if (maskMat[i] > 0)
                            {
                                if (flagsMatIndexer[idx++] != 0.0f)
                                {
                                    nonZeroCount++;
                                }
                                else
                                    maskMat[i] = 0;
                            }
                        }
                        m.CopyTo(mask);
                    }
                }
            }
            maskHandle.Free();

            return nonZeroCount;
        }

        private void VoteForUniqueness(DMatch[][] matches, Mat mask, float uniqnessThreshold = 0.80f)
        {
            byte[] maskData = new byte[matches.Length];
            GCHandle maskHandle = GCHandle.Alloc(maskData, GCHandleType.Pinned);
            using (Mat m = cv.Mat.FromPixelData(matches.Length, 1, MatType.CV_8U, maskHandle.AddrOfPinnedObject()))
            {
                mask.CopyTo(m);
                for (int i = 0; i < matches.Length; i++)
                {
                    if (matches[i].Length > 1)
                    {
                        if (matches[i][1].Distance > 0)
                        {
                            //This is also known as NNDR Nearest Neighbor Distance Ratio
                            if ((matches[i][0].Distance / matches[i][1].Distance) <= uniqnessThreshold)
                                maskData[i] = 255;
                            else
                                maskData[i] = 0;
                        }
                    }
                }
                m.CopyTo(mask);
            }
            maskHandle.Free();
        }
        public void RunAlg(Mat src)
        {
            Mat img3 = new Mat(Math.Max(img1.Height, img2.Height), img2.Width + img1.Width, MatType.CV_8UC3).SetTo(0);
            using (var descriptors1 = new Mat())
            using (var descriptors2 = new Mat())
            using (var matcher = new BFMatcher(NormTypes.L2SQR))
            using (var kaze = KAZE.Create())
            {
                kaze.DetectAndCompute(img1, null, out keypoints1, descriptors1);
                kaze.DetectAndCompute(img2, null, out keypoints2, descriptors2);

                if (descriptors1.Width > 0 && descriptors2.Width > 0)
                {
                    DMatch[][] matches = matcher.KnnMatch(descriptors1, descriptors2, 2);
                    using (Mat mask = new Mat(matches.Length, 1, MatType.CV_8U))
                    {
                        mask.SetTo(Scalar.White);
                        int nonZero = Cv2.CountNonZero(mask);
                        VoteForUniqueness(matches, mask);
                        nonZero = Cv2.CountNonZero(mask);
                        nonZero = VoteForSizeAndOrientation(keypoints2, keypoints1, matches, mask, 1.5f, 10);

                        List<Point2f> obj = new List<Point2f>();
                        List<Point2f> scene = new List<Point2f>();
                        List<DMatch> goodMatchesList = new List<DMatch>();
                        //iterate through the mask only pulling out nonzero items because they're matches
                        MatIndexer<byte> maskIndexer = mask.GetGenericIndexer<byte>();
                        for (int i = 0; i < mask.Rows; i++)
                        {
                            if (maskIndexer[i] > 0)
                            {
                                obj.Add(keypoints1[matches[i][0].QueryIdx].Pt);
                                scene.Add(keypoints2[matches[i][0].TrainIdx].Pt);
                                goodMatchesList.Add(matches[i][0]);
                            }
                        }

                        List<Point2d> objPts = obj.ConvertAll(Point2fToPoint2d);
                        List<Point2d> scenePts = scene.ConvertAll(Point2fToPoint2d);
                        if (nonZero >= 4)
                        {
                            Mat homography = Cv2.FindHomography(objPts, scenePts, HomographyMethods.Ransac, 1.5, mask);
                            nonZero = Cv2.CountNonZero(mask);

                            if (homography != null && homography.Width > 0)
                            {
                                Point2f[] objCorners = { new Point2f(0, 0),
                                    new Point2f(img1.Cols, 0),
                                    new Point2f(img1.Cols, img1.Rows),
                                    new Point2f(0, img1.Rows) };

                                Point2d[] sceneCorners = MyPerspectiveTransform3(objCorners, homography);

                                //This is a good concat horizontal
                                using (Mat left = new Mat(img3, new Rect(0, 0, img1.Width, img1.Height)))
                                using (Mat right = new Mat(img3, new Rect(img1.Width, 0, img2.Width, img2.Height)))
                                {
                                    img1.CopyTo(left);
                                    img2.CopyTo(right);

                                    byte[] maskBytes = new byte[mask.Rows * mask.Cols];
                                    mask.GetArray(out maskBytes);
                                    Cv2.DrawMatches(img1, keypoints1, img2, keypoints2, goodMatchesList, img3, Scalar.All(-1), Scalar.All(-1), maskBytes, DrawMatchesFlags.NotDrawSinglePoints);

                                    List<List<cv.Point>> listOfListOfPoint2D = new List<List<cv.Point>>();
                                    List<cv.Point> listOfPoint2D = new List<cv.Point>();
                                    listOfPoint2D.Add(new cv.Point(sceneCorners[0].X + img1.Cols, sceneCorners[0].Y));
                                    listOfPoint2D.Add(new cv.Point(sceneCorners[1].X + img1.Cols, sceneCorners[1].Y));
                                    listOfPoint2D.Add(new cv.Point(sceneCorners[2].X + img1.Cols, sceneCorners[2].Y));
                                    listOfPoint2D.Add(new cv.Point(sceneCorners[3].X + img1.Cols, sceneCorners[3].Y));
                                    listOfListOfPoint2D.Add(listOfPoint2D);
                                    img3.Polylines(listOfListOfPoint2D, true, Scalar.LimeGreen, 2);

                                    //This works too
                                    //Cv2.Line(img3, scene_corners[0] + new Point2d(img1.Cols, 0), scene_corners[1] + new Point2d(img1.Cols, 0), Scalar.LimeGreen);
                                    //Cv2.Line(img3, scene_corners[1] + new Point2d(img1.Cols, 0), scene_corners[2] + new Point2d(img1.Cols, 0), Scalar.LimeGreen);
                                    //Cv2.Line(img3, scene_corners[2] + new Point2d(img1.Cols, 0), scene_corners[3] + new Point2d(img1.Cols, 0), Scalar.LimeGreen);
                                    //Cv2.Line(img3, scene_corners[3] + new Point2d(img1.Cols, 0), scene_corners[0] + new Point2d(img1.Cols, 0), Scalar.LimeGreen);
                                }
                            }
                        }
                    }
                }
            }
            dst2 = img3.Resize(dst2.Size());
        }
    }






    public class MeanSubtraction_Basics_CS : VB_Parent
    {
        Options_MeanSubtraction options = new Options_MeanSubtraction();
        public MeanSubtraction_Basics_CS()
        {
            desc = "Subtract the mean from the image with a scaling factor";
        }
        public void RunAlg(Mat src)
        {
            Scalar mean = Cv2.Mean(src);
            Cv2.Subtract(mean, src, dst2);
            dst2 *= (float)(100 / options.scaleVal);
        }
    }





    public class OilPaintManual
    {
        private static byte ClipByte(double colour)
        {
            return (byte)(colour > 255 ? 255 : (colour < 0 ? 0 : colour));
        }

        public void Start(cv.Mat color, cv.Mat result1, int filterSize, int levels)
        {
            int[] intensityBin = new int[levels];

            int filterOffset = (filterSize - 1) / 2;
            int currentIntensity = 0, maxIntensity = 0, maxIndex = 0;

            for (int offsetY = filterOffset; offsetY < color.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX < color.Width - filterOffset; offsetX++)
                {
                    maxIntensity = maxIndex = 0;

                    intensityBin = new int[levels];
                    cv.Vec3i[] bins = new cv.Vec3i[levels];

                    for (int y = offsetY - filterOffset; y < offsetY + filterOffset; y++)
                    {
                        for (int x = offsetX - filterOffset; x < offsetX + filterOffset; x++)
                        {
                            cv.Vec3b rgb = color.Get<cv.Vec3b>(y, x);
                            currentIntensity = (int)(Math.Round((Double)(rgb[0] + rgb[1] + rgb[2]) / 3.0 * (levels - 1)) / 255.0);

                            intensityBin[currentIntensity] += 1;
                            bins[currentIntensity][0] += rgb[0];
                            bins[currentIntensity][1] += rgb[1];
                            bins[currentIntensity][2] += rgb[2];

                            if (intensityBin[currentIntensity] > maxIntensity)
                            {
                                maxIntensity = intensityBin[currentIntensity];
                                maxIndex = currentIntensity;
                            }
                        }
                    }

                    if (maxIntensity == 0) maxIntensity = 1;
                    double blue = bins[maxIndex][0] / maxIntensity;
                    double green = bins[maxIndex][1] / maxIntensity;
                    double red = bins[maxIndex][2] / maxIntensity;

                    result1.Set<cv.Vec3b>(offsetY, offsetX, new cv.Vec3b(ClipByte(blue), ClipByte(green), ClipByte(red)));
                }
            }
        }
    }





    public class Sieve
    {
        public List<int> GetPrimeNumbers(int count)
        {
            var output = new List<int>();
            for (int n = 2; output.Count < count; n++)
            {
                if (output.All(x => n % x != 0)) output.Add(n);
            }
            return output;
        }
    }





    /////+////////////////////////////////////////////////////////////
    //                                                               
    //          Copyright Vadim Stadnik 2020.                        
    // Distributed under the Code Project Open License (CPOL) 1.02.  
    // (See or copy at http://www.codeproject.com/info/cpol10.aspx)  
    //                                                               
    /////////////////////////////////////////////////////////////////

    //                                                                      
    //  This file contains the demonstration C# code for the article        
    //  by V. Stadnik "Segmented Linear Regression";                        
    //                                                                      
    //  Note that the algorithms for segmented linear regression (SLR)      
    //  were originally written in C++. The C# variants of these algorithms 
    //  preserve the structure of the original C++ implementation.          
    //                                                                      
    //  The accuracy of the approximation is basically the same.            
    //  The differences are observed in the least significant digits of     
    //  type <double>.                                                      
    //                                                                      
    //  The C# implementation of the algorithms has comparable performance  
    //  with C++ implementation in terms of measured running time.          
    //  The asymptotical performance is identical.                          
    //                                                                      
    //  The implementation assumes that the attached sample datasets are    
    //  stored in the folder "C:\\SampleData\\" ;                           
    //    
    // https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression

    //  implemenation of algorithms          
    //  SLR == Segmented Linear Regression ; 
    public class SLR
    {
        //  type to store coefficients A and B of linear regression 
        public class LinearRegressionParams
        {
            public double coef_a;
            public double coef_b;

            public LinearRegressionParams(double _a, double _b)
            {
                coef_a = _a;
                coef_b = _b;
            }
        }


        //  struct RangeIndex represents a semi-open range of indices [ a, b ) 
        public class RangeIndex
        {
            public int idx_a;
            public int idx_b;

            public RangeIndex(int _a, int _b)
            {
                //  empty and reversed ranges are NOT allowed in this type 
                if (_b <= _a)
                    Debug.WriteLine("invalid range");

                //  a negative index will crash application 
                if (_a < 0)
                    Debug.WriteLine("invalide index");

                idx_a = _a;
                idx_b = _b;
            }

            public int Length()
            {
                return (idx_b - idx_a);
            }
        }

        //  RangeLengthMin() is the limit for minimum allowed length of a range ;  
        //  a range is indivisible if its length is less than 2*RANGE_LENGTH_MIN ; 
        static public int RangeLengthMin()
        {
            return 2;
        }

        //  the function to measure the accuracy of an approximation 
        static public double ApproximationErrorY
            (
                List<double> data_y_orig,
                List<double> data_y_approx
            )
        {
            if (data_y_orig.Count != data_y_approx.Count)
            {
                Debug.WriteLine("SLR: data size error");
                return double.MaxValue;
            }

            //  the result is max value of abs differences between two matching y values 
            double diff_max = 0.0;
            int n_values = data_y_orig.Count;

            if (n_values < 1)
                return diff_max;

            for (int i = 0; i < n_values; ++i)
            {
                double y_orig_i = data_y_orig[i];
                double y_aprox_i = data_y_approx[i];
                double diff_i = Math.Abs(y_orig_i - y_aprox_i);

                if (diff_i > diff_max)
                    diff_max = diff_i;
            }

            return diff_max;
        }


        //  the function LinearRegressionParameters() computes parameters of 
        //  linear regression using values of given sums ;                   
        //                                                                   
        //  this function returns <false> for special cases or invalid input 
        //  that should be processed in client code ;                        
        static public bool LinearRegressionParameters
            (
                double n_values,
                double sum_x,
                double sum_y,
                double sum_xx,
                double sum_xy,
                //  the results are                                                            
                //  coefficients a and b of linear function: y = a + b*x ;                     
                //                                                                             
                //  they are solution of the two equations:  a * N     + b * sum_x  = sum_y  ; 
                //                                           a * sum_x + b * sum_xx = sum_xy ; 
                //                                                                             
                LinearRegressionParams lin_regn_out
            )
        {
            //  result for special cases or invalid input parameters 
            lin_regn_out.coef_a = 0.0;
            lin_regn_out.coef_b = 0.0;

            const double TOLER = 1.0e-10;
            //  invalid input n_values:                       
            //      0 is UN-defined case;                     
            //      1 causes division by zero (denom ==0.0) ; 
            if (n_values < 1.0 + TOLER)
                return false;

            double denom = n_values * sum_xx - sum_x * sum_x;

            if (Math.Abs(denom) < TOLER)
            {
                //  the following special cases should be processed in client code:              
                //    1. user data represent a single point ;                                    
                //    2. regression line is vertical: coef_a==INFINITY , coeff_b is UN-defined ; 
                return false;
            }

            //  coefficients for the approximation line: y = a + b*x ;            
            lin_regn_out.coef_a = (sum_y * sum_xx - sum_x * sum_xy) / denom;
            lin_regn_out.coef_b = (n_values * sum_xy - sum_x * sum_y) / denom;
            return true;
        }

        //  the function ComputeLinearRegression() computes parameters of 
        //  linear regression and approximation error                     
        //  for a given range of a given dataset ;                        
        static public void ComputeLinearRegression
            (
                //  original dataset                                     
                List<double> data_x,
                List<double> data_y,
                //  semi-open range [ a , b )                            
                RangeIndex idx_range,
                //  coefficients of linear regression in the given range 
                LinearRegressionParams lin_regr_out,
                //  approximation error                                  
                ref double err_appr_out
            )
        {
            if (idx_range.Length() < RangeLengthMin())
            {
                Debug.WriteLine("SLR error: input range is too small");
                return;
            }

            int idx_a = idx_range.idx_a;
            int idx_b = idx_range.idx_b;
            double n_vals = idx_range.Length();
            double sum_x = 0.0;
            double sum_y = 0.0;
            double sum_xx = 0.0;
            double sum_xy = 0.0;

            //  compute the required sums: 
            for (int it = idx_a; it < idx_b; ++it)
            {
                double xi = data_x[it];
                double yi = data_y[it];
                sum_x += xi;
                sum_y += yi;
                sum_xx += xi * xi;
                sum_xy += xi * yi;
            }

            //  compute parameters of linear regression in the given range 
            if (!LinearRegressionParameters(n_vals, sum_x, sum_y, sum_xx, sum_xy, lin_regr_out))
            {
                //  this is a very unusual case for real data  
                //Debug.WriteLine("SLR: special case error");
                return;
            }

            double coef_a = lin_regr_out.coef_a;
            double coef_b = lin_regr_out.coef_b;

            //  use linear regression obtained to measure approximation error in the given range,          
            //  the error is the maximum of absolute differences between original and approximation values 
            double diff_max = 0.0;
            for (int it = idx_a; it < idx_b; ++it)
            {
                double xi = data_x[it];
                double yi_orig = data_y[it];
                double yi_appr = coef_a + coef_b * xi;

                double diff_i = Math.Abs(yi_orig - yi_appr);
                if (diff_i > diff_max)
                {
                    diff_max = diff_i;
                }
            }

            err_appr_out = diff_max;
        }

        //  implementation specific function-helper for better code re-use, 
        //  it enables us to measure approximations errors in results       
        static public void InterpolateSegments
            (
                List<RangeIndex> vec_ranges,
                List<LinearRegressionParams> vec_LR_params,
                List<double> data_x,
                //  results 
                List<double> data_x_interpol,
                List<double> data_y_interpol
            )
        {
            data_x_interpol.Clear();
            data_y_interpol.Clear();

            int n_ranges = vec_ranges.Count;
            for (int i_rng = 0; i_rng < n_ranges; ++i_rng)
            {
                //  in the current range we only need to interpolate y-data 
                //  using corresponding linear regression                   
                RangeIndex range_i = vec_ranges[i_rng];
                LinearRegressionParams lr_params_i = vec_LR_params[i_rng];

                double coef_a = lr_params_i.coef_a;
                double coef_b = lr_params_i.coef_b;
                int i_start = range_i.idx_a;
                int i_end = range_i.idx_b;
                for (int i = i_start; i < i_end; ++i)
                {
                    double x_i = data_x[i];
                    double y_i = coef_a + coef_b * x_i;

                    data_x_interpol.Add(x_i);
                    data_y_interpol.Add(y_i);
                }
            }
        }


        //  the function CanSplitRangeThorough()                          
        //  makes decision whether a given range should be split or not ; 
        //                                                                
        //  a given range is not subdivided if the specified accuracy of  
        //  linear regression has been achieved, otherwise, the function  
        //  searches for the best split point in the range ;              
        //                                                                
        static public bool CanSplitRangeThorough
            (
                //  original dataset                                                
                List<double> data_x,
                List<double> data_y,
                //  the limit for maximum allowed approximation error (tolerance)   
                double devn_max_user,
                //  input range to be split if linear regression is not acceptable  
                RangeIndex idx_range_in,
                //  the position of a split point, when the function returns <true> 
                ref int idx_split_out,
                //  the parameters of linear regression for the given range,        
                //  when the function returns <false>                               
                LinearRegressionParams lr_params_out
            )
        {
            //  compute linear regression and approximation error for input range 
            double error_range_in = double.MaxValue;
            ComputeLinearRegression(data_x, data_y, idx_range_in, lr_params_out, ref error_range_in);

            //  if the approximation is acceptable, input range is not subdivided 
            if (error_range_in < devn_max_user)
                return false;

            //  approximation error for a current split 
            double err_split = double.MaxValue;
            //  the position (index) of a current split 
            int idx_split = -1;
            int idx_a = idx_range_in.idx_a;
            int idx_b = idx_range_in.idx_b;
            int end_offset = RangeLengthMin();

            //  sequential search for the best split point in the input range 
            for (int idx = idx_a + end_offset; idx < idx_b - end_offset; ++idx)
            {
                //  sub-divided ranges 
                RangeIndex range_left = new RangeIndex(idx_a, idx);
                RangeIndex range_right = new RangeIndex(idx, idx_b);

                //  parameters of linear regression in sub-divided ranges 
                LinearRegressionParams lin_regr_left = new LinearRegressionParams(0.0, 0.0);
                LinearRegressionParams lin_regr_right = new LinearRegressionParams(0.0, 0.0);

                //  corresponding approximation errors 
                double err_left = double.MaxValue;
                double err_right = double.MaxValue;

                //  compute linear regression and approximation error in each range 
                ComputeLinearRegression(data_x, data_y, range_left, lin_regr_left, ref err_left);
                ComputeLinearRegression(data_x, data_y, range_right, lin_regr_right, ref err_right);

                //  we use the worst approximation error 
                double err_idx = Math.Max(err_left, err_right);
                //  the smaller error the better split   
                if (err_idx < err_split)
                {
                    err_split = err_idx;
                    idx_split = idx;
                }
            }

            //  check that sub-division is valid,                             
            //  the case of short segment: 2 or 3 data points ;               
            //  if (n==3) required approximation accuracy cannot be reached ; 
            if (idx_split < 0)
                return false;

            idx_split_out = idx_split;
            return true;
        }


        //  this function implements the smoothing method,           
        //  which is known as simple moving average ;                
        //                                                           
        //  the implementation uses symmetric window ;               
        //  the window length is variable in front and tail ranges ; 
        //  the first and last values are fixed ;                    
        static public void SimpleMovingAverage
            (
                List<double> data_io,
                int half_len
            )
        {
            int n_values = data_io.Count;

            //  no processing is required 
            if (half_len <= 0 || n_values < 3)
                return;

            //  smoothing window is too large 
            if ((2 * half_len + 1) > n_values)
                return;

            int ix = 0;
            double sum_y = 0.0;
            List<double> data_copy = new List<double>();
            data_copy.AddRange(data_io);

            //  for better readability, where relevant the code below shows   
            //  the symmetry of processing at a current data point,           
            //  for example: we use ( ix + 1 + ix ) instead of ( 2*ix + 1 ) ; 

            //  the first point is fixed 
            sum_y = data_copy[0];
            data_io[0] = sum_y / 1.0;

            //  the front range:                                                      
            //  processing accumulates sum_y using gradually increasing length window 
            for (ix = 1; ix <= half_len; ++ix)
            {
                sum_y = sum_y + data_copy[2 * ix - 1] + data_copy[2 * ix];
                data_io[ix] = sum_y / (double)(ix + 1 + ix);
            }

            //  in the middle range window length is constant 
            for (ix = (half_len + 1); ix <= ((n_values - 1) - half_len); ++ix)
            {
                //  add to window new data point and remove from window the oldest data point
                sum_y = sum_y + data_copy[ix + half_len] - data_copy[ix - half_len - 1];
                data_io[ix] = sum_y / (double)(half_len + 1 + half_len);
            }

            //  the tail range:                                    
            //  processing uses gradually decreasing length window 
            for (ix = (n_values - half_len); ix < (n_values - 1); ++ix)
            {
                sum_y = sum_y - data_copy[n_values - 1 - 2 * half_len + 2 * (ix - (n_values - 1 - half_len)) - 2]
                                - data_copy[n_values - 1 - 2 * half_len + 2 * (ix - (n_values - 1 - half_len)) - 1];

                data_io[ix] = sum_y / (double)(n_values - 1 - ix + 1 + n_values - 1 - ix);
            }

            //  the last point is fixed 
            data_io[n_values - 1] = data_copy[n_values - 1];
        }


        //                                                                           
        //  this function detects positions (indices) of local maxima                
        //  in a given dataset of values of type <double> ;                          
        //                                                                           
        //  limitations:                                                             
        //  the implementation is potentially sensitive to numerical error,          
        //  thus, it is not the best choice for processing perfect (no noise) data ; 
        //  it does not support finding maximum value in a plato ;                   
        //                                                                           
        static public void FindLocalMaxima
            (
                List<double> vec_data_in,
                List<int> vec_max_indices_res
            )
        {
            vec_max_indices_res.Clear();

            int n_values = vec_data_in.Count;

            if (n_values < 3)
                return;

            //  the last and first values are excluded from processing 
            for (int ix = 1; ix <= n_values - 2; ++ix)
            {
                double y_prev = vec_data_in[ix - 1];
                double y_curr = vec_data_in[ix];
                double y_next = vec_data_in[ix + 1];

                bool less_prev = (y_prev < y_curr);
                bool less_next = (y_next < y_curr);

                if (less_prev && less_next)
                {
                    vec_max_indices_res.Add(ix);
                    ++ix;
                }
            }
        }


        //  the function CanSplitRangeFast()                              
        //  makes decision whether a given range should be split or not ; 
        //                                                                
        //  a given range is not subdivided if the specified accuracy of  
        //  linear regression has been achieved, otherwise,               
        //  the function selects for the best split the position of       
        //  the greatest local maximum of absolute differences            
        //  between original and smoothed values in a given range ;       
        //                                                                
        static public bool CanSplitRangeFast
            (
                //  original dataset                                                
                List<double> data_x,
                List<double> data_y,
                //  absolute differences between original and smoothed values       
                List<double> vec_devns_in,
                //  positions (indices) of local maxima in vec_devns_in             
                List<int> vec_max_ind_in,
                //  the limit for maximum allowed approximation error (tolerance)   
                double devn_max_user,
                //  input range to be split if linear regression is not acceptable  
                RangeIndex idx_range_in,
                //  the position of a split point, when the function returns <true> 
                ref int idx_split_out,
                //  the parameters of linear regression for the given range,        
                //  when the function returns <false>                               
                LinearRegressionParams lr_params_out
            )
        {
            idx_split_out = -1;

            if (vec_devns_in.Count != data_x.Count)
            {
                Debug.WriteLine("SLR: size error");
                return false;
            }

            int end_offset = RangeLengthMin();
            int range_len = idx_range_in.Length();
            if (range_len < end_offset)
            {
                Debug.WriteLine("SLR: input range is too small");
                return false;
            }

            //  compute linear regression and approximation error for input range 
            double err_range_in = double.MaxValue;
            ComputeLinearRegression(data_x, data_y, idx_range_in, lr_params_out, ref err_range_in);

            //  if the approximation is acceptable, input range is not subdivided 
            if (err_range_in < devn_max_user)
                return false;

            //  check for indivisible range 
            if (range_len < 2 * RangeLengthMin())
                return false;

            if (vec_devns_in.Count == 0)
                return false;

            //  for the main criterion of splitting here we use                 
            //  the greatest local maximum of deviations inside the given range 
            int idx_split_local_max = -1;
            double devn_max = 0.0;
            double devn_cur = 0.0;
            int sloc_max = vec_max_ind_in.Count;

            //  find inside given range local maximum with the largest deviation 
            for (int k_max = 0; k_max < sloc_max; ++k_max)
            {
                int idx_max_cur = vec_max_ind_in[k_max];

                //  check if the current index is inside the given range and that  
                //  potential split will not create segment with 1 data point only 
                if ((idx_max_cur < idx_range_in.idx_a + end_offset) ||
                        (idx_max_cur >= idx_range_in.idx_b - end_offset))
                    continue;

                devn_cur = vec_devns_in[idx_max_cur];
                if (devn_cur > devn_max)
                {
                    devn_max = devn_cur;
                    idx_split_local_max = idx_max_cur;
                }
            }

            //  the case of no one local maximum inside the given range 
            if (idx_split_local_max < 0)
                return false;

            //  the case (idx_split_local_max==0) is not possible here due to (end_offset==RANGE_LENGTH_MIN), 
            //  this is a valid result ( idx_split_local_max > 0 )                                            
            idx_split_out = idx_split_local_max;

            return true;
        }


        //  the function SegmentedRegressionFast() implements                   
        //  algorithm for segmented linear (piecewise) regression,              
        //  which uses for range splitting local maxima of                      
        //  absolute differences between original and smoothed values ;         
        //  the method of smoothing is simple moving average;                   
        //                                                                      
        //  the average performance of this algorithm is O(N logM), where       
        //      N is the number of given values and                             
        //      M is the number of resulting line segments ;                    
        //  in the worst case the performace is quadratic ;                     
        //                                                                      
        //  return value <false> shows that the required approximation accuracy 
        //  has not been achieved ;                                             
        //                                                                      
        public bool SegmentedRegressionFast
            (
                //  input dataset:                                                     
                //  this function assumes that input x-data are equally spaced         
                List<double> data_x,
                List<double> data_y,
                //  user specified approximation accuracy (tolerance) ;                
                //  this parameter allows to control the total number                  
                //  and lengths of segments detected ;                                 
                double devn_max,
                //  this parameter represents half length of window ( h_len+1+h_len ), 
                //  which is used by simple moving average to create smoothed dataset  
                int sm_half_len,
                //  the resulting segmented linear regression                          
                //  is interpolated to match and compare against input values          
                List<double> data_x_res,
                List<double> data_y_res
            )
        {
            data_x_res.Clear();
            data_y_res.Clear();

            int size_x = data_x.Count;
            int size_y = data_y.Count;

            if (size_x != size_y)
                return false;

            //  check for indivisible range 
            if (size_x < 2 * RangeLengthMin())
                return false;

            //  vector of smoothed values 
            List<double> data_y_smooth = new List<double>();
            data_y_smooth.AddRange(data_y);
            SimpleMovingAverage(data_y_smooth, sm_half_len);

            //  vector of deviations (as absolute differences) between original and smoothed values 
            List<double> vec_deviations = new List<double>();
            for (int i = 0; i < size_y; ++i)
            {
                vec_deviations.Add(Math.Abs(data_y_smooth[i] - data_y[i]));
            }

            //  find positions of local maxima in the vector of deviations 
            List<int> vec_max_indices = new List<int>();
            FindLocalMaxima(vec_deviations, vec_max_indices);

            //  ranges (segments) of linear regression 
            List<RangeIndex> vec_ranges = new List<RangeIndex>();
            //  parameters of linear regression in each matching range 
            List<LinearRegressionParams> vec_LR_params = new List<LinearRegressionParams>();

            //  the stage of recursive top-down subvision:                    
            //  this processing starts from the entire range of given dataset 
            RangeIndex range_top = new RangeIndex(0, size_x);
            //  the position (index) of a current split point                 
            int idx_split = -1;
            //  parameters of linear regression in a current range (segment)  
            LinearRegressionParams lr_params = new LinearRegressionParams(0.0, 0.0);

            Stack<RangeIndex> stack_ranges = new Stack<RangeIndex>();
            stack_ranges.Push(range_top);

            while (stack_ranges.Count > 0)
            {
                range_top = stack_ranges.Pop();

                if (CanSplitRangeFast(data_x, data_y, vec_deviations, vec_max_indices,
                                        devn_max, range_top, ref idx_split, lr_params))
                {
                    //  reverse order of pushing onto stack eliminates re-ordering vec_ranges 
                    //  after this function is completed                                      
                    stack_ranges.Push(new RangeIndex(idx_split, range_top.idx_b));
                    stack_ranges.Push(new RangeIndex(range_top.idx_a, idx_split));
                }
                else
                {
                    //  the range is indivisible, we add it to the result 
                    vec_ranges.Add(new RangeIndex(range_top.idx_a, range_top.idx_b));
                    vec_LR_params.Add(new LinearRegressionParams(lr_params.coef_a, lr_params.coef_b));
                }
            }


            //  interpolate the resulting segmented linear regression 
            //  and verify the accuracy of the approximation          
            List<double> data_x_interpol = new List<double>();
            List<double> data_y_interpol = new List<double>();

            InterpolateSegments(vec_ranges, vec_LR_params, data_x,
                                    data_x_interpol, data_y_interpol);

            double appr_error = ApproximationErrorY(data_y, data_y_interpol);
            //if (appr_error > devn_max)
            //    return false;

            //  the result of this function when the required accuracy has been achieved 
            data_x_res.AddRange(data_x_interpol);
            data_y_res.AddRange(data_y_interpol);

            return true;
        }
    }



    /////////////////////////////////////////////////////////////////
    //                                                               
    //          Copyright Vadim Stadnik 2020.                        
    // Distributed under the Code Project Open License (CPOL) 1.02.  
    // (See or copy at http://www.codeproject.com/info/cpol10.aspx)  
    //                                                               
    //  this file contains the demonstration code for the article    
    //  by V. Stadnik "Simple approach to Voronoi diagrams";         
    //                                                               
    /////////////////////////////////////////////////////////////////

    //                                                                      
    //  the namespace VoronoiDemo provides a demonstration variant of       
    //  the nearest neighbor search in an ordered dataset of                
    //  two dimensional points;                                             
    //  the algorithm has square root computational complexity, on average; 
    //                                                                      
    //  the performance test emulates the computation of                    
    //  the distance transform; the performance of the developed            
    //  algorithm can be compared with the performance of                   
    //  the brute force algorithm;                                          
    //                                                                      
    //  the code of the namespace VoronoiDemo has been written              
    //  to avoid complications associated with numeric errors of            
    //  floating data types in comparison operations;                       
    //  the code uses integer type for X and Y coordinates of               
    //  two dimensional points; in addition to this, instead of             
    //  distance between two points, it calculates squared distance,        
    //  which also takes advantage of the exact integer value;              
    //                                                                      

    public class VoronoiDemo
    {

        //  class Point represents a two dimensional point 
        public class Point
        {
            protected int x;
            protected int y;

            public Point(int _x, int _y)
            {
                x = _x;
                y = _y;
            }

            public int X() { return x; }
            public int Y() { return y; }

            public bool IsEqual(Point that)
            {
                return (this.X() == that.X() &&
                         this.Y() == that.Y());
            }

            public bool NotEqual(Point that)
            {
                return (!this.IsEqual(that));
            }

            static public uint DistanceSquared(Point pnt_a, Point pnt_b)
            {
                int x = pnt_b.X() - pnt_a.X();
                int y = pnt_b.Y() - pnt_a.Y();
                uint d = (uint)(x * x + y * y);
                return d;
            }
        }


        //  the comparison operation to order a given list or an array for search, 
        //  which is more efficient than straightforward sequential search         
        public class PointComparer : IComparer<Point>
        {
            //  compare coordinates in X then Y order 
            public int Compare(Point pnt_a, Point pnt_b)
            {
                if (pnt_a.X().CompareTo(pnt_b.X()) != 0)
                {
                    return pnt_a.X().CompareTo(pnt_b.X());
                }
                else if (pnt_a.Y().CompareTo(pnt_b.Y()) != 0)
                {
                    return pnt_a.Y().CompareTo(pnt_b.Y());
                }
                else
                    return 0;
            }
        }

        //  the comparison operation to remove duplicates from a given dataset 
        class PointEquality : IEqualityComparer<Point>
        {
            public bool Equals(Point pnt_a, Point pnt_b)
            {
                if (pnt_a.IsEqual(pnt_b))
                    return true;
                else
                    return false;
            }

            public int GetHashCode(Point pnt)
            {
                int hCode = pnt.X() ^ pnt.Y();
                return hCode.GetHashCode();
            }
        }


        //  implementation of the algorithm using sequential search 
        class AlgoBruteForce
        {
            //  the function MinDistanceBruteForce() implements 
            //  the brute force sequential search algorithm;    
            //  a dataset can be either ordered or unordered;   
            //                                                  
            //  computational complexity - O(N),                
            //  where N is the number of points in a container; 
            static public uint MinDistanceBruteForce
                (
                    Point point_in,
                    List<Point> points
                )
            {
                uint dist_min = uint.MaxValue;
                uint dist_cur = dist_min;
                int n_points = points.Count;

                for (int i = 0; i < n_points; ++i)
                {
                    dist_cur = Point.DistanceSquared(point_in, points[i]);

                    if (dist_cur < dist_min)
                        dist_min = dist_cur;
                }

                return dist_min;
            }

            //  the function TestPerformance() measures the running time of  
            //  the nearest neighbor search in an ordered dataset of points; 
            //                                                               
            //  the test emulates the computation of the distance transform; 
            //  it calculates the minimum distance from each point in        
            //  the given rectangle to a point in the input dataset;         
            static public cv.Mat TestPerformance
                (
                    int rect_width,
                    int rect_height,
                    List<Point> test_points
                )
            {
                cv.Mat dist = new cv.Mat(rect_height, rect_width, cv.MatType.CV_32F);
                PointComparer pnt_comparer = new PointComparer();
                test_points.Sort(pnt_comparer);

                Stopwatch watch = new Stopwatch();
                watch.Start();

                for (int x = 0; x < rect_width; ++x)
                {
                    for (int y = 0; y < rect_height; ++y)
                    {
                        float nextVal = MinDistanceBruteForce(new Point(x, y), test_points);
                        dist.Set<float>(y, x, nextVal);
                    }
                }

                watch.Stop();
                Debug.WriteLine("execution time of AlgoBruteForce algorithm = {0} ms ;", watch.ElapsedMilliseconds);

                return dist;
            }

        }   //  class AlgoBruteForce ; 


        //  implementation of the algorithm using efficient search 
        //  on ordered dataset                                     
        class AlgoOrderedList
        {
            //  the function LowerBound() implements a binary search,   
            //  which in terms of operator < returns the first position 
            //  that satisfies the following condition:                 
            //      ! ( points_ordered[pos] < point_in ) == true ;      
            //                                                          
            //  the computational complexity is O(log N),               
            //  where N is the number of points in a dataset;           
            static protected int LowerBound
                (
                    List<Point> points_ordered,
                    PointComparer pnt_comparer_in,
                    Point point_in
                )
            {
                int i_low = 0;
                int i_high = points_ordered.Count;
                int i_mid = 0;

                while (i_low < i_high)
                {
                    i_mid = (i_low + i_high) / 2;

                    if (pnt_comparer_in.Compare(points_ordered[i_mid], point_in) < 0)
                    {
                        i_low = i_mid + 1;
                    }
                    else
                    {
                        i_high = i_mid;
                    }
                }

                return i_low;
            }

            //  the function FindForward() is a helper   
            //  for the function MinDistanceOrderedSet() 
            static protected void FindForward
                (
                    Point point_in,
                    int i_low_bound,
                    int i_end,
                    List<Point> points_ordered,
                    ref uint dist_min_io
                )
            {
                uint dist_cur = 0;
                uint dist_x = 0;

                for (int i = i_low_bound; i < i_end; ++i)
                {
                    dist_cur = Point.DistanceSquared(points_ordered[i], point_in);
                    dist_x = (uint)(points_ordered[i].X() - point_in.X()) *
                               (uint)(points_ordered[i].X() - point_in.X());

                    if (dist_cur < dist_min_io)
                        dist_min_io = dist_cur;

                    if (dist_x > dist_min_io)
                        break;
                }
            }

            //  the function FindBackward() is a helper  
            //  for the function MinDistanceOrderedSet() 
            static protected void FindBackward
                (
                    Point point_in,
                    int i_low_bound,
                    int i_start,
                    List<Point> points_ordered,
                    ref uint dist_min_io
                )
            {
                uint dist_cur = 0;
                uint dist_x = 0;

                for (int i = i_low_bound - 1; i >= 0; --i)
                {
                    dist_cur = Point.DistanceSquared(points_ordered[i], point_in);
                    dist_x = (uint)(points_ordered[i].X() - point_in.X()) *
                               (uint)(points_ordered[i].X() - point_in.X());

                    if (dist_cur < dist_min_io)
                        dist_min_io = dist_cur;

                    if (dist_x > dist_min_io)
                        break;
                }
            }


            //  the function MinDistanceOrderedSet() implements          
            //  the nearest neighbor search in an ordered set of points; 
            //  its average computational complexity - O ( sqrt(N) ) ,   
            //  where N is the number of points in the set;              
            static protected uint MinDistanceOrderedSet
                (
                    Point point_in,
                    PointComparer pnt_comparer_in,
                    List<Point> points_ordered
                )
            {
                uint dist_min = uint.MaxValue;
                int i_start = 0;
                int i_end = points_ordered.Count;
                int i_low_bound = 0;

                i_low_bound = LowerBound(points_ordered, pnt_comparer_in, point_in);

                FindForward(point_in, i_low_bound, i_end, points_ordered, ref dist_min);
                FindBackward(point_in, i_low_bound, i_start, points_ordered, ref dist_min);

                return dist_min;
            }


            //  the function TestPerformance() measures the running time of  
            //  the nearest neighbor search in an ordered dataset of points; 
            //                                                               
            //  the test emulates the computation of the distance transform; 
            //  it calculates the minimum distance from each point in        
            //  the given rectangle to a point in the input dataset;         
            static public cv.Mat TestPerformance
                (
                    int rect_width,
                    int rect_height,
                    List<Point> test_points
                )
            {
                cv.Mat dist = new cv.Mat(rect_height, rect_width, cv.MatType.CV_32F);
                PointComparer pnt_comparer = new PointComparer();
                test_points.Sort(pnt_comparer);

                Stopwatch watch = new Stopwatch();
                watch.Start();

                for (int x = 0; x < rect_width; ++x)
                {
                    for (int y = 0; y < rect_height; ++y)
                    {
                        float nextVal = (float)MinDistanceOrderedSet(new Point(x, y), pnt_comparer, test_points);
                        dist.Set<float>(y, x, nextVal);
                    }
                }

                watch.Stop();
                Debug.WriteLine("execution time of ordered dataset algorithm = {0} ms ;", watch.ElapsedMilliseconds);

                return dist;
            }

        }   //  class AlgoOrderedList ; 


        //  class to generate test datasets of random points 
        public class TestPoints
        {
            //  this function generates random points inside the    
            //  specified rectangle: [ 0, width ) x [ 0, height ) ; 
            //                                                      
            //  the result is sorted and contains no duplicates;    
            //  note also that  points_res.size() <= n_points  ;    
            static public void Generate
                (
                    //  rectangle area to fill in 
                    int width,
                    int height,
                    int n_points,
                    List<Point> points_out
                )
            {
                points_out.Clear();

                Random rand_x = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
                Thread.Sleep(20);
                Random rand_y = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

                HashSet<Point> hash_set = new HashSet<Point>(new PointEquality());
                int n_duplicates = 0;

                for (int i = 0; i < n_points; ++i)
                {
                    int xi = rand_x.Next(width);
                    int yi = rand_y.Next(height);
                    if (hash_set.Add(new Point(xi, yi)))
                    {
                        points_out.Add(new Point(xi, yi));
                    }
                    else
                        ++n_duplicates;
                }

                //  if ( n_duplicates > 0 ) 
                //      Console . WriteLine ( "test points: {0} duplicates removed;", n_duplicates ) ;

                points_out.Sort(new PointComparer());
            }
        }   //  class TestPoints ; 

        public void New() { }

        public void RunAlg(ref cv.Mat src, List<cv.Point2f> points, bool bruteForce)
        {
            List<Point> test_points = new List<Point>();
            foreach (cv.Point pt in points)
            {
                test_points.Add(new Point(pt.X, pt.Y));
            }

            if (bruteForce == false)
                src = AlgoOrderedList.TestPerformance(src.Width, src.Height, test_points);
            else
                src = AlgoBruteForce.TestPerformance(src.Width, src.Height, test_points);
        }
        public void RunAlg(ref cv.Mat src, List<cv.Point2f> points)
        {
            List<Point> test_points = new List<Point>();
            foreach (cv.Point pt in points)
            {
                test_points.Add(new Point(pt.X, pt.Y));
            }

            src = AlgoOrderedList.TestPerformance(src.Width, src.Height, test_points);
        }
    }





    public class DNN
    {
        private string[] classNames;
        Net net;
        public void initialize(string protoTxt, string caffeModel, string synsetWords)
        {
            classNames = File.ReadAllLines(synsetWords).Select(line => line.Split(' ').Last()).ToArray();

            PrepareModel(caffeModel);
            net = CvDnn.ReadNetFromCaffe(protoTxt, caffeModel);
            Debug.WriteLine("Layer names: {0}", string.Join(", ", net.GetLayerNames()));
            Debug.WriteLine("Preparation complete");
        }
        public string RunAlg(Mat image)
        {
            // Convert Mat to batch of images
            using (var inputBlob = CvDnn.BlobFromImage(image, 1, new cv.Size(224, 224), new Scalar(104, 117, 123)))
            {
                net.SetInput(inputBlob, "data");
                using (var prob = net.Forward("prob"))
                {
                    // find the best class
                    GetMaxClass(prob, out int classId, out double classProb);
                    return String.Format("Best class: #{0} '{1}' Probability: {2:P2}", classId, classNames[classId], classProb);
                }
            }
        }

        private static byte[] DownloadBytes(string url)
        {
            var client = WebRequest.CreateHttp(url);
            using (var response = client.GetResponse())
            using (var responseStream = response.GetResponseStream())
            {
                using (var memory = new MemoryStream())
                {
                    responseStream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        private static void PrepareModel(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Console.Write("Downloading Caffe Model...");
                var contents = DownloadBytes("http://dl.caffe.berkeleyvision.org/bvlc_googlenet.caffemodel");
                File.WriteAllBytes(fileName, contents);
            }
        }

        /// <summary>
        /// Find best class for the blob (i. e. class with maximal probability)
        /// </summary>
        /// <param name="probBlob"></param>
        /// <param name="classId"></param>
        /// <param name="classProb"></param>
        private static void GetMaxClass(Mat probBlob, out int classId, out double classProb)
        {
            // reshape the blob to 1x1000 matrix
            using (var probMat = probBlob.Reshape(1, 1))
            {
                Cv2.MinMaxLoc(probMat, out _, out classProb, out _, out var classNumber);
                classId = classNumber.X;
            }
        }
    }





    public class KNN
    {
        public List<float[]> trainingSetValues = new List<float[]>();
        public List<float[]> testSetValues = new List<float[]>();

        private int K;

        public void Classify(int neighborsNumber)
        {
            this.K = neighborsNumber;

            // create an array where we store the distance from our test data and the training data -> [0]
            // plus the index of the training data element -> [1]
            float[][] distances = new float[trainingSetValues.Count][];

            for (int i = 0; i < trainingSetValues.Count; i++)
                distances[i] = new float[2];

            Debug.WriteLine("[i] classifying...");

            // start computing
            for (var test = 0; test < this.testSetValues.Count; test++)
            {
                Parallel.For(0, trainingSetValues.Count, index =>
                {
                    var dist = EuclideanDistance(this.testSetValues[test], this.trainingSetValues[index]);
                    distances[index][0] = dist;
                    distances[index][1] = index;
                }
                );

                // sort and select first K of them
                var sortedDistances = distances.AsParallel().OrderBy(t => t[0]).Take(this.K);
            }
        }

        private static float EuclideanDistance(float[] sampleOne, float[] sampleTwo)
        {
            float d = 0.0f;

            for (int i = 0; i < sampleOne.Length; i++)
            {
                float temp = sampleOne[i] - sampleTwo[i];
                d += temp * temp;
            }
            return (float)Math.Sqrt(d);
        }
    }




    public class MatrixInverse
    {
        public double[] bVector;
        public double[] solution;

        cv.Mat inverse;
        public cv.Mat RunAlg(cv::Mat m)
        {
            bool ShowIntermediate = false; // turn this on if further detail is needed.
            double d = MatDeterminant(m);
            if (Math.Abs(d) < 1.0e-5)
                if (ShowIntermediate) Debug.WriteLine("\nMatrix has no inverse");
                else
                if (ShowIntermediate) Debug.WriteLine("\nDet(m) = " + d.ToString("F4"));

            inverse = MatInverse(m);

            cv.Mat prod = MatProduct(m, inverse);
            if (ShowIntermediate)
            {
                Debug.WriteLine("\nThe product of m * inv is ");
                MatShow(prod, 1, 6);
            }

            cv.Mat lum;
            int[] perm;
            int toggle = MatDecompose(m, out lum, out perm);
            if (ShowIntermediate)
            {
                Debug.WriteLine("\nThe combined lower-upper decomposition of m is");
                MatShow(lum, 4, 8);
            }

            cv.Mat lower = ExtractLower(lum);
            cv.Mat upper = ExtractUpper(lum);

            if (ShowIntermediate)
            {
                solution = MatVecProd(inverse, bVector);  // (1, 0, 2, 1)
                Debug.WriteLine("\nThe lower part of LUM is");
                MatShow(lower, 4, 8);

                Debug.WriteLine("\nThe upper part of LUM is");
                MatShow(upper, 4, 8);

                Debug.WriteLine("\nThe perm[] array is");
                VecShow(perm, 4);

                cv.Mat lowUp = MatProduct(lower, upper);
                Debug.WriteLine("\nThe product of lower * upper is ");
                MatShow(lowUp, 4, 8);

                Debug.WriteLine("\nVector b = ");
                VecShow(bVector, 1, 8);

                Debug.WriteLine("\nSolving m*x = b");

                Debug.WriteLine("\nSolution x = ");
                VecShow(solution, 1, 8);
            }
            return inverse;
        }

        static cv.Mat MatInverse(cv.Mat m)
        {
            // assumes determinant is not 0
            // that is, the matrix does have an inverse
            int n = m.Rows;
            cv.Mat result = m.Clone();

            cv.Mat lum; // combined lower & upper
            int[] perm;  // out parameter
            MatDecompose(m, out lum, out perm);  // ignore return

            double[] b = new double[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                    if (i == perm[j])
                        b[j] = 1.0;
                    else
                        b[j] = 0.0;

                double[] x = Reduce(lum, b); // 
                for (int j = 0; j < n; ++j)
                    result.Set<double>(j, i, x[j]);
            }
            return result;
        }

        static int MatDecompose(cv.Mat m, out cv.Mat lum, out int[] perm)
        {
            // Crout's LU decomposition for matrix determinant and inverse
            // stores combined lower & upper in lum[][]
            // stores row permuations into perm[]
            // returns +1 or -1 according to even or odd number of row permutations
            // lower gets dummy 1.0s on diagonal (0.0s above)
            // upper gets lum values on diagonal (0.0s below)

            int toggle = +1; // even (+1) or odd (-1) row permutatuions
            int n = m.Rows;

            // make a copy of m[][] into result lum[][]
            lum = m.Clone();

            // make perm[]
            perm = new int[n];
            for (int i = 0; i < n; ++i)
                perm[i] = i;

            for (int j = 0; j < n - 1; ++j) // process by column. note n-1 
            {
                double max = Math.Abs(lum.At<double>(j, j));
                int piv = j;

                for (int i = j + 1; i < n; ++i) // find pivot index
                {
                    double xij = Math.Abs(lum.At<double>(i, j));
                    if (xij > max)
                    {
                        max = xij;
                        piv = i;
                    }
                } // i

                if (piv != j)
                {
                    cv.Mat tmp = lum.Row(piv).Clone(); // swap rows j, piv
                    lum.Row(j).CopyTo(lum.Row(piv));
                    tmp.CopyTo(lum.Row(j));

                    int t = perm[piv]; // swap perm elements
                    perm[piv] = perm[j];
                    perm[j] = t;

                    toggle = -toggle;
                }

                double xjj = lum.At<double>(j, j);
                if (xjj != 0.0)
                {
                    for (int i = j + 1; i < n; ++i)
                    {
                        double xij = lum.At<double>(i, j) / xjj;
                        lum.Set<double>(i, j, xij);
                        for (int k = j + 1; k < n; ++k)
                            lum.Set<double>(i, k, lum.At<double>(i, k) - xij * lum.At<double>(j, k));
                    }
                }
            }

            return toggle;  // for determinant
        }

        static double[] Reduce(cv.Mat luMatrix, double[] b) // helper
        {
            int n = luMatrix.Rows;
            double[] x = new double[n];
            for (int i = 0; i < n; ++i)
                x[i] = b[i];

            for (int i = 1; i < n; ++i)
            {
                double sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix.At<double>(i, j) * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix.At<double>(n - 1, n - 1);
            for (int i = n - 2; i >= 0; --i)
            {
                double sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix.At<double>(i, j) * x[j];
                x[i] = sum / luMatrix.At<double>(i, i);
            }

            return x;
        }

        static double MatDeterminant(cv.Mat m)
        {
            cv.Mat lum;
            int[] perm;

            double result = MatDecompose(m, out lum, out perm);  // impl. cast
            for (int i = 0; i < lum.Rows; ++i)
                result *= lum.At<double>(i, i);
            return result;
        }

        static cv.Mat MatProduct(cv.Mat matA, cv.Mat matB)
        {
            int aRows = matA.Rows;
            int aCols = matA.Cols;
            int bRows = matB.Rows;
            int bCols = matB.Cols;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices");

            cv.Mat result = new cv.Mat(aRows, bCols, cv.MatType.CV_64F, cv.Scalar.All(0));

            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < bCols; ++j) // each col of B
                    for (int k = 0; k < aCols; ++k) // could use bRows
                        result.Set<double>(i, j, result.At<double>(i, j) + matA.At<double>(i, k) * matB.At<double>(k, j));

            return result;
        }

        static double[] MatVecProd(cv.Mat m, double[] v)
        {
            int n = v.Length;
            if (m.Cols != n)
                throw new Exception("non-comform in MatVecProd");

            double[] result = new double[n];

            for (int i = 0; i < m.Rows; ++i)
            {
                for (int j = 0; j < m.Cols; ++j)
                {
                    result[i] += m.At<double>(i, j) * v[j];
                }
            }
            return result;
        }

        static cv.Mat ExtractLower(cv.Mat lum)
        {
            // lower part of an LU Crout's decomposition
            // (dummy 1.0s on diagonal, 0.0s above)
            int n = lum.Rows;
            cv.Mat result = lum.Clone().SetTo(0);
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == j)
                        result.Set<double>(i, j, 1.0);
                    else if (i > j)
                        result.Set<double>(i, j, lum.At<double>(i, j));
                }
            }
            return result;
        }

        static cv.Mat ExtractUpper(cv.Mat lum)
        {
            // upper part of an LU (lu values on diagional and above, 0.0s below)
            int n = lum.Rows;
            cv.Mat result = lum.Clone().SetTo(0);
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i <= j)
                        result.Set<double>(i, j, lum.At<double>(i, j));
                }
            }
            return result;
        }

        static void MatShow(cv.Mat m, int dec, int wid)
        {
            for (int i = 0; i < m.Rows; ++i)
            {
                for (int j = 0; j < m.Cols; ++j)
                {
                    double v = m.At<double>(i, j);
                    if (Math.Abs(v) < 1.0e-5) v = 0.0;  // avoid "-0.00"
                    Console.Write(v.ToString("F" + dec).PadLeft(wid));
                }
                Debug.WriteLine("");
            }
        }

        static void VecShow(int[] vec, int wid)
        {
            for (int i = 0; i < vec.Length; ++i)
                Console.Write(vec[i].ToString().PadLeft(wid));
            Debug.WriteLine("");
        }

        static void VecShow(double[] vec, int dec, int wid)
        {
            for (int i = 0; i < vec.Length; ++i)
            {
                double x = vec[i];
                if (Math.Abs(x) < 1.0e-5) x = 0.0;  // avoid "-0.00"
                Console.Write(x.ToString("F" + dec).PadLeft(wid));
            }
            Debug.WriteLine("");
        }
    }

}

