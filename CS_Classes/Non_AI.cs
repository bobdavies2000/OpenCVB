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

namespace CS_Classes
{
    public class Bitmap_ToMat_CS : CS_Parent
    {
        public Bitmap_ToMat_CS(VBtask task) : base(task)
        {
            labels[2] = "Convert color bitmap to Mat";
            labels[3] = "Convert Mat to bitmap and then back to Mat";
            desc = "Convert a color and grayscale bitmap to a cv.Mat";
        }

        public void RunCS(Mat src)
        {
            Bitmap bitmap = new Bitmap(task.HomeDir + "opencv/Samples/Data/lena.jpg");
            dst2 = BitmapConverter.ToMat(bitmap).Resize(src.Size());

            bitmap = BitmapConverter.ToBitmap(src);
            dst3 = BitmapConverter.ToMat(bitmap);
        }
    }




    public class Blur_Gaussian_CS : CS_Parent
    {
        public Options_Blur options = new Options_Blur();
        public Blur_Gaussian_CS(VBtask task) : base(task)
        {
            desc = "Smooth each pixel with a Gaussian kernel of different sizes.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            cv.Cv2.GaussianBlur(src, dst2, new cv.Size(options.kernelSize, options.kernelSize), 0, 0);
        }
    }





    public class Feature_Kaze_CS : CS_Parent
    {
        public KeyPoint[] kazeKeyPoints = null;
        public Feature_Kaze_CS(VBtask task) : base(task)
        {
            labels[2] = "KAZE key points";
            desc = "Find keypoints using KAZE algorithm.";
        }
        public void RunCS(Mat src)
        {
            dst2 = src.Clone();
            if (src.Channels() != 1) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            var kaze = KAZE.Create();
            var kazeDescriptors = new Mat();
            kaze.DetectAndCompute(src, null, out kazeKeyPoints, kazeDescriptors);

            for (int i = 0; i < kazeKeyPoints.Length; i++)
            {
                DrawCircle(dst2, kazeKeyPoints[i].Pt, task.DotSize, task.HighlightColor);
            }
        }
    }





    public class Feature_AKaze_CS : CS_Parent
    {
        KeyPoint[] kazeKeyPoints = null;
        public Feature_AKaze_CS(VBtask task) : base(task)
        {
            labels[2] = "AKAZE key points";
            desc = "Find keypoints using AKAZE algorithm.";
        }
        public void RunCS(Mat src)
        {
            dst2 = src.Clone();
            if (src.Channels() != 1) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            var kaze = AKAZE.Create();
            var kazeDescriptors = new Mat();
            kaze.DetectAndCompute(src, null, out kazeKeyPoints, kazeDescriptors);

            for (int i = 0; i < kazeKeyPoints.Length; i++)
            {
                DrawCircle(dst2, kazeKeyPoints[i].Pt, task.DotSize, task.HighlightColor);
            }
        }
    }






    public class Feature_LeftRight_CS : CS_Parent
    {
        Options_Kaze options;
        Feature_Kaze_CS KazeLeft;
        Feature_Kaze_CS KazeRight;
        public Feature_LeftRight_CS(VBtask task) : base(task)
        {
            options = new Options_Kaze();
            KazeLeft = new Feature_Kaze_CS(task);
            KazeRight = new Feature_Kaze_CS(task);
            labels = new string[] { "", "", "Left Image", "Right image with matches shown.  Blue is left view and Red is right view." };
            desc = "Find keypoints in the left and right images using KAZE algorithm.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = task.leftView.Channels() == 1 ? task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR) : task.leftView;
            dst3 = task.rightView.Channels() == 1 ? task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR) : task.rightView;

            KazeLeft.RunAndMeasure(dst2, KazeLeft);
            KazeRight.RunAndMeasure(dst3, KazeRight);

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
                    DrawCircle(dst3, pt1.Pt, task.DotSize + 2, cv.Scalar.Blue);
                    DrawCircle(dst2, pt1.Pt, task.DotSize + 2, cv.Scalar.Blue);
                    DrawCircle(dst3, KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, task.DotSize + 2, cv.Scalar.Red);
                    DrawLine(dst3, pt1.Pt, KazeLeft.kazeKeyPoints.ElementAt(minIndex).Pt, cv.Scalar.Yellow, task.lineWidth);
                }
            }
        }
    }




    public class Blob_Basics_CS : CS_Parent
    {
        Blob_Input input;
        Options_Blob options;

        public Blob_Basics_CS(VBtask task) : base(task)
        {
            options = new Options_Blob();
            input = new Blob_Input();
            UpdateAdvice(traceName + ": click 'Show All' to see all the available options.");
            desc = "Isolate and list blobs with specified options";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

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



    public class Feature_SiftLeftRight_CS : CS_Parent
    {
        public Options_Sift options;
        KeyPoint[] keypoints1, keypoints2;
        public Feature_SiftLeftRight_CS(VBtask task) : base(task)
        {
            options = new Options_Sift();
            desc = "Compare 2 images to get a homography.  We will use left and right images.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat doubleSize = new Mat(dst2.Rows, dst2.Cols * 2, MatType.CV_8UC3);

            dst0 = task.leftView.Channels() == 3 ? task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY) : task.leftView;
            dst1 = task.rightView.Channels() == 3 ? task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY) : task.rightView;

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




    public class Feature_Sift_CS : CS_Parent
    {
        Options_Sift options;
        public List<cv.Point> stablePoints = new List<cv.Point>();
        List<List<cv.Point>> history = new List<List<cv.Point>>();
        public KeyPoint[] keypoints;
        public Feature_Sift_CS(VBtask task) : base(task)
        {
            options = new Options_Sift();
            desc = "Keypoints found in SIFT";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = src.Clone();
            var sift = SIFT.Create(options.pointCount);
            var descriptors1 = new Mat();
            sift.DetectAndCompute(src.CvtColor(ColorConversionCodes.BGR2GRAY), null, out keypoints, descriptors1);

            List<cv.Point> newPoints = new List<cv.Point>();
            for (int i = 0; i < keypoints.Length; i++)
            {
                var pt = keypoints[i].Pt;
                DrawCircle(dst2, pt, task.DotSize, Scalar.Yellow);
                newPoints.Add(new cv.Point((int)pt.X, (int)pt.Y));
            }

            dst3 = src.Clone();
            if (task.optionsChanged) history.Clear();
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
                    DrawCircle(dst3, pt, task.DotSize, Scalar.Yellow);
                    stablePoints.Add(pt);
                }
            }
            if (history.Count >= task.frameHistoryCount) history.RemoveAt(0);
            labels[3] = "Sift keypoints that are present in the last " + task.frameHistoryCount.ToString() + " frames.";
        }
    }




    // https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
    public class Feature_SiftSlices_CS : CS_Parent
    {
        Options_Sift options;

        public Feature_SiftSlices_CS(VBtask task) : base(task)
        {
            options = new Options_Sift();
            FindSlider("Points to Match").Value = 1;
            desc = "Compare 2 images to get a homography but limit the search to a slice of the image.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

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

                sift.DetectAndCompute(task.leftView[r1], null, out keypoints1, descriptors1);
                sift.DetectAndCompute(task.rightView[r1], null, out keypoints2, descriptors2);

                if (options.useBFMatcher)
                {
                    var bfMatcher = new BFMatcher(NormTypes.L2, false);
                    DMatch[] bfMatches = bfMatcher.Match(descriptors1, descriptors2);
                    Cv2.DrawMatches(task.leftView[r1], keypoints1, task.rightView[r1], keypoints2, bfMatches, doubleSize[r2]);
                }
                else
                {
                    var flannMatcher = new FlannBasedMatcher();
                    DMatch[] flannMatches = flannMatcher.Match(descriptors1, descriptors2);
                    Cv2.DrawMatches(task.leftView[r1], keypoints1, task.rightView[r1], keypoints2, flannMatches, doubleSize[r2]);
                }
            }

            doubleSize[new Rect(0, 0, dst2.Width, dst2.Height)].CopyTo(dst2);
            doubleSize[new Rect(dst2.Width, 0, dst2.Width, dst2.Height)].CopyTo(dst3);

            labels[2] = options.useBFMatcher ? "BF Matcher output" : "Flann Matcher output";
        }
    }
    // https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
    public class Feature_SURF_CS : CS_Parent
    {
        public Options_SURF options;
        public bool drawPoints = true;
        public List<cv.Point> stablePoints = new List<cv.Point>();
        List<List<cv.Point>> history = new List<List<cv.Point>>();
        public KeyPoint[] keypoints1, keypoints2;
        public Feature_SURF_CS(VBtask task) : base(task)
        {
            options = new Options_SURF();
            desc = "Keypoints found in SIFT";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            dst0 = task.leftView.Channels() == 3 ? task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY) : task.leftView;
            dst1 = task.rightView.Channels() == 3 ? task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY) : task.rightView;

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
    public class Feature_SURF_Draw_CS : CS_Parent
    {
        Feature_SURF_CS surf;

        public Feature_SURF_Draw_CS(VBtask task) : base(task)
        {
            surf = new Feature_SURF_CS(task);
            surf.drawPoints = false;
            desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices.";
        }

        public void RunCS(Mat src)
        {
            dst2 = task.leftView.Channels() == 1 ? task.leftView.CvtColor(ColorConversionCodes.GRAY2BGR) : task.leftView;
            dst3 = task.rightView.Channels() == 1 ? task.rightView.CvtColor(ColorConversionCodes.GRAY2BGR) : task.rightView;

            surf.RunAndMeasure(src, surf);

            KeyPoint[] keys1 = surf.keypoints1;
            KeyPoint[] keys2 = surf.keypoints2;

            for (int i = 0; i < keys1.Length; i++)
            {
                var pt = new cv.Point(keys1[i].Pt.X, keys1[i].Pt.Y);
                Cv2.Circle(dst2, pt, task.DotSize + 2, Scalar.Red);
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
                        Cv2.Circle(dst3, pt, task.DotSize + 2, task.HighlightColor);
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
                    Cv2.Circle(dst3, pt, task.DotSize + 2, Scalar.Red);
                }
            }

            labels[3] = "Yellow matched left to right = " + matchCount.ToString() + ". Red is unmatched.";
        }
    }




    public class OilPaint_Manual_CS : CS_Parent
    {
        CS_Classes.OilPaintManual oilPaint = new CS_Classes.OilPaintManual();
        public Options_OilPaint options = new Options_OilPaint();

        public OilPaint_Manual_CS(VBtask task) : base(task)
        {
            task.drawRect = new Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8);
            labels[3] = "Selected area only";
            desc = "Alter an image so it appears painted by a pointilist. Select a region of interest to paint.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            Rect roi = ValidateRect(task.drawRect);
            src.CopyTo(dst2);
            oilPaint.Start(new Mat(src, roi), new Mat(dst2, roi), options.kernelSize, options.intensity);
            dst3 = src.EmptyClone().SetTo(Scalar.All(0));
            int factor = Math.Min((int)Math.Floor((double)dst3.Width / roi.Width), (int)Math.Floor((double)dst3.Height / roi.Height));
            cv.Size s = new cv.Size(roi.Width * factor, roi.Height * factor);
            Cv2.Resize(new Mat(dst2, roi), new Mat(dst3, new Rect(0, 0, s.Width, s.Height)), s);
        }
    }



    public class OilPaint_Cartoon_CS : CS_Parent
    {
        OilPaint_Manual_CS oil;
        Edge_Laplacian Laplacian = new Edge_Laplacian();

        public OilPaint_Cartoon_CS(VBtask task) : base(task)
        {
            oil = new OilPaint_Manual_CS(task);
            task.drawRect = new Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8);
            labels[2] = "OilPaint_Cartoon";
            labels[3] = "Laplacian Edges";
            desc = "Alter an image so it appears more like a cartoon";
        }

        public void RunCS(Mat src)
        {
            var roi = ValidateRect(task.drawRect);
            Laplacian.Run(src);
            dst3 = Laplacian.dst2;

            oil.RunAndMeasure(src, oil);
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



    public class SLR_Basics_CS : CS_Parent
    {
        public SLR_Data slrInput = new SLR_Data();
        SLR slr = new SLR();
        Plot_Basics_CPP_VB plot = new Plot_Basics_CPP_VB();
        Options_SLR options = new Options_SLR();
        public SLR_Basics_CS(VBtask task) : base(task)
        {
            desc = "Segmented Linear Regression example";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.FirstPass && standalone)
            {
                slrInput.RunVB(dst2);
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



    public class SLR_DepthHist_CS : CS_Parent
    {
        public SLR_Basics_CS slr;
        public Hist_Kalman kalman;

        public SLR_DepthHist_CS(VBtask task) : base(task)
        {
            task.gOptions.setHistogramBins(32);
            slr = new SLR_Basics_CS(task);
            kalman = new Hist_Kalman();
            labels = new string[] { "", "", "Original data", "Segmented Linear Regression (SLR) version of the same data.  Red line is zero." };
            desc = "Run Segmented Linear Regression on depth data";
        }

        public void RunCS(Mat src)
        {
            kalman.Run(src);
            kalman.hist.histogram.Set<float>(0, 0, 0);
            dst2 = kalman.dst2;
            for (int i = 0; i < kalman.hist.histogram.Rows; i++)
            {
                slr.slrInput.dataX.Add(i);
                slr.slrInput.dataY.Add(kalman.hist.histogram.Get<float>(i, 0));
            }
            slr.RunAndMeasure(src, slr);
            dst3 = slr.dst3;
        }
    }



    public class OEX_Sobel_Demo_CS : CS_Parent
    {
        Edge_Sobel_CS sobel;

        public OEX_Sobel_Demo_CS(VBtask task) : base(task)
        {
            sobel = new Edge_Sobel_CS(task);
            desc = "OpenCV Example Sobel_Demo became Edge_Sobel algorithm.";
        }

        public void RunCS(Mat src)
        {
            sobel.RunAndMeasure(src, sobel);
            dst2 = sobel.dst2;
            dst3 = sobel.dst3;
            labels = sobel.labels;
        }
    }



    // https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
    public class Voronoi_Basics_CS : CS_Parent
    {
        public VoronoiDemo vDemo = new VoronoiDemo();
        public Random_Basics_CS random;

        public Voronoi_Basics_CS(VBtask task) : base(task)
        {
            random = new Random_Basics_CS(task);
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
                Cv2.Circle(dst, (cv.Point)pt, task.DotSize, color, -1);
            }
        }

        public void RunCS(Mat src)
        {
            if (task.heartBeat) random.RunAndMeasure(empty, random);
            vDemo.RunCS(ref dst2, random.PointList);
            vDisplay(ref dst2, random.PointList, Scalar.Yellow);
        }
    }

    // https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
    public class Voronoi_Compare_CS : CS_Parent
    {
        Voronoi_Basics_CS basics;
        public Random_Basics random = new Random_Basics();

        public Voronoi_Compare_CS(VBtask task) : base(task)
        {
            basics = new Voronoi_Basics_CS(task);
            FindSlider("Random Pixel Count").Maximum = 150;
            FindSlider("Random Pixel Count").Value = 150;
            labels = new string[] { "", "", "Brute Force method - check log timings", "Ordered List method - check log for timing" };
            desc = "C# implementations of the BruteForce and OrderedList Voronoi algorithms";
        }

        public void RunCS(Mat src)
        {
            random.Run(empty);
            basics.vDemo.RunCS(ref dst2, random.PointList, true);
            basics.vDisplay(ref dst2, random.PointList, Scalar.Yellow);

            basics.vDemo.RunCS(ref dst3, random.PointList, false);
            basics.vDisplay(ref dst3, random.PointList, Scalar.Yellow);
        }
    }

    // https://www.codeproject.com/Articles/882739/Simple-approach-to-Voronoi-diagrams
    public class Voronoi_CS : CS_Parent
    {
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr VoronoiDemo_Open(string matlabFileName, int rows, int cols);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr VoronoiDemo_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr VoronoiDemo_Run(IntPtr pfPtr, IntPtr Input, int pointCount, int width, int height);

        Voronoi_Basics_CS vDemo;
        public Voronoi_CS(VBtask task) : base(task)
        {
            vDemo = new Voronoi_Basics_CS(task);
            cPtr = VoronoiDemo_Open(task.HomeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols);
            desc = "Use the C++ version of the Voronoi code";
        }

        public void RunCS(Mat src)
        {
            var countSlider = FindSlider("Random Pixel Count");
            if (task.heartBeat) vDemo.random.RunAndMeasure(empty, vDemo.random);
            List<cv.Point> ptList = new List<cv.Point>();
            for (int i = 0; i < vDemo.random.PointList.Count; i++)
            {
                var pt = vDemo.random.PointList[i];
                ptList.Add(new cv.Point(pt.X, pt.Y));
            }
            var handleSrc = GCHandle.Alloc(ptList.ToArray(), GCHandleType.Pinned);
            var imagePtr = VoronoiDemo_Run(cPtr, handleSrc.AddrOfPinnedObject(), countSlider.Value, dst2.Width, dst2.Height);
            handleSrc.Free();

            dst2 = Mat.FromPixelData(dst2.Rows, dst2.Cols, MatType.CV_32F, imagePtr).Clone();
            vDemo.vDisplay(ref dst2, vDemo.random.PointList, Scalar.Yellow);
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = VoronoiDemo_Close(cPtr);
        }
    }

    public class Edge_Motion_CS : CS_Parent
    {
        Diff_Basics diff = new Diff_Basics();
        Edge_Sobel_CS edges;

        public Edge_Motion_CS(VBtask task) : base(task)
        {
            edges = new Edge_Sobel_CS(task);
            labels[2] = "Wave at camera to see impact or move camera.";
            desc = "Measure camera motion using Sobel and Diff from last frame.";
        }

        public void RunCS(Mat src)
        {
            edges.RunAndMeasure(src, edges);
            diff.Run(edges.dst2);

            dst2 = diff.dst2;
            dst3 = dst2 & edges.dst2;
            if (task.quarterBeat) labels[3] = $"{dst3.CountNonZero()} pixels overlap between Sobel edges and diff with last Sobel edges.";
        }
    }


    public class Edge_NoDepth_CS : CS_Parent
    {
        Edge_Sobel_CS edges;
        Blur_Basics_CS blur;
        public Edge_NoDepth_CS(VBtask task) : base(task)
        {
            edges = new Edge_Sobel_CS(task);
            blur = new Blur_Basics_CS(task);
            labels[2] = "Edges found in the regions with no depth";
            labels[3] = "Mask of regions with no depth - blurred to expand slightly.";
            desc = "Find the edges in regions without depth.";
        }
        public void RunCS(Mat src)
        {
            edges.RunAndMeasure(src, edges);
            dst2 = edges.dst2;

            blur.RunAndMeasure(task.noDepthMask, blur);
            dst3.SetTo(0);
            dst2.CopyTo(dst3, blur.dst2);
            dst3 = dst3.Threshold(0, 255, ThresholdTypes.Binary);
            dst2.SetTo(0, ~dst3);
        }
    }









    public class Sieve_Basics_CS : CS_Parent
    {
        Sieve_BasicsVB printer = new Sieve_BasicsVB();
        Sieve sieve = new Sieve();

        public Sieve_Basics_CS(VBtask task) : base(task)
        {
            desc = "Implement the Sieve of Eratothenes in C#";
        }

        public void RunCS(Mat src)
        {
            var countSlider = FindSlider("Count of desired primes");
            SetTrueText(printer.shareResults(sieve.GetPrimeNumbers(countSlider.Value)));
        }
    }



    // https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
    public class SLR_Image_CS : CS_Parent
    {
        public SLR_Basics_CS slr;
        public Hist_Basics hist = new Hist_Basics();

        public SLR_Image_CS(VBtask task) : base(task)
        {
            task.gOptions.setHistogramBins(32);
            slr = new SLR_Basics_CS(task);
            labels[2] = "Original data";
            desc = "Run Segmented Linear Regression on grayscale image data - just an experiment";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            hist.Run(src);
            dst2 = hist.dst2;
            for (int i = 0; i < hist.histogram.Rows; i++)
            {
                slr.slrInput.dataX.Add(i);
                slr.slrInput.dataY.Add(hist.histogram.Get<float>(i, 0));
            }
            slr.RunAndMeasure(src, slr);
            dst3 = slr.dst3;
        }
    }


    public class SLR_TrendCompare_CS : CS_Parent
    {
        public SLR_Image_CS slr;
        List<float> valList = new List<float>();
        int barMidPoint;
        Point2f lastPoint;
        public List<Point2f> resultingPoints = new List<Point2f>();

        public SLR_TrendCompare_CS(VBtask task) : base(task)
        {
            slr = new SLR_Image_CS(task);
            desc = "Find trends by filling in short histogram gaps in the given image's histogram.";
        }

        void connectLine(int i)
        {
            var p1 = new Point2f(barMidPoint + dst2.Width * i / valList.Count, dst2.Height - dst2.Height * valList[i] / slr.hist.plot.maxValue);
            resultingPoints.Add(p1);
            Cv2.Line(dst2, (int)lastPoint.X, (int)lastPoint.Y, (int)p1.X, (int)p1.Y, Scalar.Yellow, task.lineWidth + 1, task.lineType);
            lastPoint = p1;
        }

        public void RunCS(Mat src)
        {
            labels[2] = "Histogram with Yellow line showing the trends";
            slr.hist.plot.backColor = Scalar.Red;
            slr.RunAndMeasure(src, slr);
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
            slr.hist.plot.maxValue = valList.Max();
            lastPoint = new Point2f(barMidPoint, dst2.Height - dst2.Height * valList[0] / slr.hist.plot.maxValue);
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


    public class Feature_SURFMatch_CS : CS_Parent
    {
        Feature_SURF_CS surf;
        public Feature_SURFMatch_CS(VBtask task) : base(task)
        {
            surf = new Feature_SURF_CS(task);
            surf.drawPoints = false;
            desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices.";
        }

        public void RunCS(Mat src)
        {
            surf.RunAndMeasure(src, surf);

            Mat doublesize = new Mat();
            Cv2.HConcat(new List<Mat> { task.leftView, task.rightView }, doublesize);

            var keys1 = surf.keypoints1;
            var keys2 = surf.keypoints2;

            for (int i = 0; i < keys1.Length; i++)
            {
                DrawCircle(dst2, keys1[i].Pt, task.DotSize + 3, Scalar.Red);
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
                        doublesize.Line(pt1, pt2, task.HighlightColor, task.lineWidth);
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

    public class DNN_Caffe_CS : CS_Parent
    {
        DNN caffeCS = new DNN();
        public DNN_Caffe_CS(VBtask task) : base(task)
        {
            labels[3] = "Input Image";
            desc = "Download and use a Caffe database";

            string protoTxt = task.HomeDir + "Data/bvlc_googlenet.prototxt";
            string modelFile = task.HomeDir + "Data/bvlc_googlenet.caffemodel";
            string synsetWords = task.HomeDir + "Data/synset_words.txt";
            caffeCS.initialize(protoTxt, modelFile, synsetWords);
        }

        public void RunCS(Mat src)
        {
            Mat image = Cv2.ImRead(task.HomeDir + "Data/space_shuttle.jpg");
            string str = caffeCS.RunCS(image);
            dst3 = image.Resize(dst3.Size());
            SetTrueText(str);
        }
    }

    public class Dither_Basics_CS : CS_Parent
    {
        Options_Dither options = new Options_Dither();

        public Dither_Basics_CS(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Dither applied to the BGR image", "Dither applied to the Depth image" };
            UpdateAdvice(traceName + ": use local options to control which method is used.");
            desc = "Explore all the varieties of dithering";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            int w = dst2.Width;
            int h = dst2.Height;
            int nColors = new int[] { 1, 3, 7, 15, 31 }[options.bppIndex]; // indicate 3, 6, 9, 12, 15 bits per pixel.
            byte[] pixels = new byte[dst2.Total() * dst2.ElemSize()];
            GCHandle hpixels = GCHandle.Alloc(pixels, GCHandleType.Pinned);

            for (int i = 0; i < 2; i++)
            {
                Mat copySrc = (i == 0) ? src : task.depthRGB;
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
                    dst2 = new Mat(src.Height, src.Width, MatType.CV_8UC3, pixels).Clone();
                }
                else
                {
                    dst3 = new Mat(src.Height, src.Width, MatType.CV_8UC3, pixels).Clone();
                }
            }

            hpixels.Free();
        }
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
    }



    public class DNN_Test_CS : CS_Parent
    {
        Net net;
        string[] classnames;
        public DNN_Test_CS(VBtask task) : base(task)
        {
            var modelFile = new FileInfo(task.HomeDir + "Data/bvlc_googlenet.caffemodel");
            if (!File.Exists(modelFile.FullName))
            {
                var client = WebRequest.CreateHttp("http://dl.caffe.berkeleyvision.org/bvlc_googlenet.caffemodel");
                var response = client.GetResponse();
                var responseStream = response.GetResponseStream();
                var memory = new MemoryStream();
                responseStream.CopyTo(memory);
                File.WriteAllBytes(modelFile.FullName, memory.ToArray());
            }
            var protoTxt = task.HomeDir + "Data/bvlc_googlenet.prototxt";
            net = CvDnn.ReadNetFromCaffe(protoTxt, modelFile.FullName);
            var synsetWords = task.HomeDir + "Data/synset_words.txt";
            classnames = File.ReadAllLines(synsetWords);
            for (int i = 0; i < classnames.Length; i++)
            {
                classnames[i] = classnames[i].Split(' ').Last();
            }
            labels[3] = "Input Image";
            desc = "Download and use a Caffe database";
        }
        public void RunCS(Mat src)
        {
            var image = Cv2.ImRead(task.HomeDir + "Data/space_shuttle.jpg");
            dst3 = image.Resize(dst3.Size());
            var inputBlob = CvDnn.BlobFromImage(image, 1, new OpenCvSharp.Size(224, 224), new Scalar(104, 117, 123));
            net.SetInput(inputBlob, "data");
            var prob = net.Forward("prob");
            var mm = GetMinMax(prob.Reshape(1, 1));
            SetTrueText("Best classification: index = " + mm.maxLoc.X + " which is for '" + classnames[mm.maxLoc.X] + "' with Probability " +
                        $"{mm.maxVal:0.00%}", new cv.Point(40, 200));
        }
    }
    public class DNN_Basics_CS : CS_Parent
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
        public DNN_Basics_CS(VBtask task) : base(task)
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
            var infoText = new FileInfo(task.HomeDir + "Data/MobileNetSSD_deploy.prototxt");
            if (infoText.Exists)
            {
                var infoModel = new FileInfo(task.HomeDir + "Data/MobileNetSSD_deploy.caffemodel");
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
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (dnnPrepared)
            {
                var inScaleFactor = options.ScaleFactor / options.scaleMax; // should be 0.0078 by default...
                var inputBlob = CvDnn.BlobFromImage(src, inScaleFactor, new OpenCvSharp.Size(300, 300), options.meanValue, false);
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
                //            dst3.Rectangle(rect, Scalar.Yellow, task.lineWidth + 2, task.lineType);
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
    public class DNN_SuperRes_CS : CS_Parent
    {
        public Options_DNN options = new Options_DNN();
        public DnnSuperResImpl dnn;
        string saveModelFile;
        int multiplier;
        public DNN_SuperRes_CS(VBtask task) : base(task)
        {
            task.drawRect = new Rect(10, 10, 20, 20);
            labels[2] = "Output of a resize using OpenCV";
            desc = "Get better super-resolution through a DNN";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (saveModelFile != options.superResModelFileName)
            {
                saveModelFile = options.superResModelFileName;
                multiplier = options.superResMultiplier;
                dnn = new DnnSuperResImpl(options.shortModelName, multiplier);
                dnn.ReadModel(saveModelFile);
            }
            var r = task.drawRect;
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
    //public class DNN_SuperRes_CSize : CS_Parent
    //{
    //    DNN_SuperRes super;
    //    public DNN_SuperRes_CSize(VBtask task) : base(task)
    //    {
    //        labels[2] = "Super Res resized back to original size";
    //        labels[3] = "dst3 = dst2 - src or no difference - honors original";
    //        desc = "Compare superRes reduced to original size";
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        super.Run(src);
    //        var r = new Rect(0, 0, dst2.Width, dst2.Height);
    //        var tmp = new Mat();
    //        super.dnn.Upsample(src, tmp);
    //        dst2 = tmp.Resize(dst2.Size());
    //        dst3 = dst2 - src;
    //    }
    //}





    public class Feature_Sample_CS : CS_Parent
    {
        public Mat img1, img2;
        KeyPoint[] keypoints1, keypoints2;
        public Feature_Sample_CS(VBtask task) : base(task)
        {
            img1 = cv.Cv2.ImRead(task.HomeDir + "opencv/Samples/Data/box.png", cv.ImreadModes.Color);
            img2 = cv.Cv2.ImRead(task.HomeDir + "opencv/Samples/Data/box_in_scene.png", cv.ImreadModes.Color);
            desc = "Match keypoints in 2 photos using KAZE.";
        }
        public static Point2d Point2fToPoint2d(Point2f pf)
        {
            return new Point2d(((int)pf.X), ((int)pf.Y));
        }
        static Point2d[] MyPerspectiveTransform1(Point2f[] yourData, Mat transformationMatrix)
        {
            using (Mat src = new Mat(yourData.Length, 1, MatType.CV_32FC2, yourData))
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
            using (Mat m = new Mat(mask.Rows, 1, MatType.CV_8U, maskHandle.AddrOfPinnedObject()))
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

                using (var scalesMat = new Mat<float>(rows: logScale.Count, cols: 1, data: logScale.ToArray()))
                using (var rotationsMat = new Mat<float>(rows: rotations.Count, cols: 1, data: rotations.ToArray()))
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
            using (Mat m = new Mat(matches.Length, 1, MatType.CV_8U, maskHandle.AddrOfPinnedObject()))
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
        public void RunCS(Mat src)
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
    public class MeanSubtraction_Basics_CS : CS_Parent
    {
        Options_MeanSubtraction options = new Options_MeanSubtraction();
        public MeanSubtraction_Basics_CS(VBtask task) : base(task)
        {
            desc = "Subtract the mean from the image with a scaling factor";
        }
        public void RunCS(Mat src)
        {
            Scalar mean = Cv2.Mean(src);
            Cv2.Subtract(mean, src, dst2);
            dst2 *= (float)(100 / options.scaleVal);
        }
    }

}

