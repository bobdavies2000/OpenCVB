using cv = OpenCvSharp;
using System;
using System.Windows.Forms;
using VB_Classes;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using static CS_Classes.CS_Externs;
using OpenCvSharp.XImgProc;
using System.IO;
using System.Security.Cryptography;
using System.Numerics;
using System.Diagnostics;
using OpenCvSharp.ML;
using System.Threading;
using System.Windows.Controls;

namespace CS_Classes
{
    public class CS_Moments_Basics : CS_Parent
    {
        public Point2f centroid;
        Foreground_KMeans2 foreground = new Foreground_KMeans2();
        public int scaleFactor = 1;
        public cv.Point offsetPt;
        public Kalman_Basics kalman = new Kalman_Basics();
        public CS_Moments_Basics(VBtask task) : base(task)
        {
            kalman.kInput = new float[2]; // 2 elements - cv.point
            labels[2] = "Red dot = Kalman smoothed centroid";
            desc = "Compute the centroid of the provided mask file.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                foreground.Run(src);
                dst2 = foreground.dst2.CvtColor(ColorConversionCodes.GRAY2BGR);
            }
            var m = Cv2.Moments(foreground.dst2, true);
            Point2f center;
            if (task.gOptions.GetUseKalman())
            {
                kalman.kInput[0] = (float)(m.M10 / m.M00);
                kalman.kInput[1] = (float)(m.M01 / m.M00);
                kalman.Run(src);
                center = new Point2f(kalman.kOutput[0], kalman.kOutput[1]);
            }
            else
            {
                center = new Point2f((float)(m.M10 / m.M00), (float)(m.M01 / m.M00));
            }
            if (standaloneTest()) DrawCircle(dst2, center, task.DotSize + 5, Scalar.Red);
            centroid = new Point2f(scaleFactor * (offsetPt.X + center.X), scaleFactor * (offsetPt.Y + center.Y));
        }
    }
    public class CS_Moments_CentroidKalman : CS_Parent
    {
        Foreground_KMeans2 foreground = new Foreground_KMeans2();
        Kalman_Basics kalman = new Kalman_Basics();
        public CS_Moments_CentroidKalman(VBtask task) : base(task)
        {
            kalman.kInput = new float[2]; // 2 elements - cv.point
            labels[2] = "Red dot = Kalman smoothed centroid";
            desc = "Compute the centroid of the foreground depth and smooth with Kalman filter.";
        }
        public void RunCS(Mat src)
        {
            foreground.Run(src);
            dst2 = foreground.dst2.CvtColor(ColorConversionCodes.GRAY2BGR);
            var m = Cv2.Moments(foreground.dst2, true);
            if (m.M00 > 5000) // if more than x pixels are present (avoiding a zero area!)
            {
                kalman.kInput[0] = (float)(m.M10 / m.M00);
                kalman.kInput[1] = (float)(m.M01 / m.M00);
                kalman.Run(src);
                DrawCircle(dst2, new cv.Point((int)kalman.kOutput[0], (int)kalman.kOutput[1]), task.DotSize + 5, Scalar.Red);
            }
        }
    }
    public class CS_Motion_Basics : CS_Parent
    {
        public BGSubtract_MOG2 bgSub = new BGSubtract_MOG2();
        CS_Motion_Basics_QT motion;
        public CS_Motion_Basics(VBtask task) : base(task)
        {
            motion = new CS_Motion_Basics_QT(task);
            UpdateAdvice(traceName + ": redOptions are used as well as BGSubtract options.");
            desc = "Use floodfill to find all the real motion in an image.";
        }
        public void RunCS(Mat src)
        {
            bgSub.Run(src);
            motion.RunCS(bgSub.dst2);
            dst2 = motion.dst2;
            labels[2] = motion.labels[2];
        }
    }
    public class CS_Motion_Simple : CS_Parent
    {
        public Diff_Basics diff = new Diff_Basics();
        public int cumulativePixels;
        public Options_Motion options = new Options_Motion();
        public CS_Motion_Simple(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            labels[3] = "Accumulated changed pixels from the last heartbeat";
            desc = "Accumulate differences from the previous BGR image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            diff.Run(src);
            dst2 = diff.dst2;
            if (task.heartBeat) cumulativePixels = 0;
            if (diff.changedPixels > 0 || task.heartBeat)
            {
                cumulativePixels += diff.changedPixels;
                if (cumulativePixels / src.Total() > options.cumulativePercentThreshold ||
                    diff.changedPixels > options.motionThreshold ||
                    task.optionsChanged)
                {
                    task.motionRect = new cv.Rect(0, 0, dst2.Width, dst2.Height);
                }
                if (task.motionRect.Width == dst2.Width || task.heartBeat)
                {
                    dst2.CopyTo(dst3);
                    cumulativePixels = 0;
                }
                else
                {
                    dst3.SetTo(255, dst2);
                }
            }
            var threshold = src.Total() * options.cumulativePercentThreshold;
            strOut = "Cumulative threshold = " + (threshold / 1000).ToString() + "k ";
            labels[2] = strOut + "Current cumulative pixels changed = " + (cumulativePixels / 1000).ToString() + "k";
        }
    }
    public class CS_Motion_ThruCorrelation : CS_Parent
    {
        Options_MotionDetect options = new Options_MotionDetect();
        Mat lastFrame = new cv.Mat();
        public CS_Motion_ThruCorrelation(VBtask task) : base(task)
        {
            dst3 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Detect motion through the correlation coefficient";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            var input = src.Clone();
            if (input.Channels() != 1) input = input.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (task.FirstPass) lastFrame = input.Clone();
            dst3.SetTo(0);
            Parallel.For(0, task.gridList.Count(), i =>
            {
                cv.Rect roi = task.gridList[i];
                Mat correlation = new Mat();
                cv.Scalar mean, stdev;
                Cv2.MeanStdDev(input[roi], out mean, out stdev);
                if (stdev[0] > options.stdevThreshold)
                {
                    Cv2.MatchTemplate(lastFrame[roi], input[roi], correlation, TemplateMatchModes.CCoeffNormed);
                    var mm = GetMinMax(correlation);
                    if (mm.maxVal < options.CCthreshold)
                    {
                        if ((i % task.gridRows) != 0) dst3[task.gridList[i - 1]].SetTo(255);
                        if ((i % task.gridRows) < task.gridRows && i < task.gridList.Count() - 1) dst3[task.gridList[i + 1]].SetTo(255);
                        if (i > task.gridRows)
                        {
                            dst3[task.gridList[i - task.gridRows]].SetTo(255);
                            dst3[task.gridList[i - task.gridRows + 1]].SetTo(255);
                        }
                        if (i < (task.gridList.Count() - task.gridRows - 1))
                        {
                            dst3[task.gridList[i + task.gridRows]].SetTo(255);
                            dst3[task.gridList[i + task.gridRows + 1]].SetTo(255);
                        }
                        dst3[roi].SetTo(255);
                    }
                }
            });
            lastFrame = input.Clone();
            if (task.heartBeat) dst2 = src.Clone();
            else src.CopyTo(dst2, dst3);
        }
    }
    public class CS_Motion_CCmerge : CS_Parent
    {
        Mat lastFrame = new cv.Mat();
        Motion_ThruCorrelation motionCC = new Motion_ThruCorrelation();
        public CS_Motion_CCmerge(VBtask task) : base(task)
        {
            desc = "Use the correlation coefficient to maintain an up-to-date image";
        }
        public void RunCS(Mat src)
        {
            if (task.frameCount < 10) dst2 = src.Clone();
            motionCC.Run(src);
            if (task.FirstPass) lastFrame = src.Clone();
            if (motionCC.dst3.CountNonZero() > src.Total() / 2)
            {
                dst2 = src.Clone();
                lastFrame = src.Clone();
            }
            src.CopyTo(dst2, motionCC.dst3);
            dst3 = motionCC.dst3;
        }
    }
    public class CS_Motion_PixelDiff : CS_Parent
    {
        public int changedPixels;
        int changeCount, frames;
        Mat lastFrame = new cv.Mat();
        public CS_Motion_PixelDiff(VBtask task) : base(task)
        {
            desc = "Count the number of changed pixels in the current frame and accumulate them.  If either exceeds thresholds, then set flag = true.  " +
                   "To get the Options Slider, use " + traceName + "QT";
        }
        public void RunCS(Mat src)
        {
            src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (task.FirstPass) lastFrame = src.Clone();
            Cv2.Absdiff(src, lastFrame, dst2);
            dst2 = dst2.Threshold(task.gOptions.pixelDiffThreshold, 255, ThresholdTypes.Binary);
            changedPixels = dst2.CountNonZero();
            task.motionFlag = changedPixels > 0;
            if (task.motionFlag) changeCount++;
            frames++;
            if (task.heartBeat)
            {
                strOut = "Pixels changed = " + changedPixels.ToString() + " at last heartbeat.  Since last heartbeat: " +
                         (changeCount / (float)frames).ToString("0%") + " of frames were different";
                changeCount = 0;
                frames = 0;
            }
            SetTrueText(strOut, 3);
            if (task.motionFlag) lastFrame = src.Clone();
        }
    }
    public class CS_Motion_DepthReconstructed : CS_Parent
    {
        public Motion_Basics motion = new Motion_Basics();
        public CS_Motion_DepthReconstructed(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            dst3 = new Mat(dst3.Size(), MatType.CV_32FC3, 0);
            labels[2] = "The yellow rectangle indicates where the motion is and only that portion of the point cloud and depth mask is updated.";
            desc = "Rebuild the point cloud based on the BGR motion history.";
        }
        public void RunCS(Mat src)
        {
            motion.Run(src);
            dst2 = src;
            if (task.motionFlag)
            {
                dst0 = src.Clone();
                dst1 = task.noDepthMask.Clone();
                dst3 = task.pointCloud.Clone();
                labels[3] = motion.labels[2];
            }
            if (!task.motionDetected) return;
            src[task.motionRect].CopyTo(dst0[task.motionRect]);
            task.noDepthMask[task.motionRect].CopyTo(dst1[task.motionRect]);
            task.pointCloud[task.motionRect].CopyTo(dst3[task.motionRect]);
        }
    }
    public class CS_Motion_Contours : CS_Parent
    {
        public Motion_MinRect motion = new Motion_MinRect();
        Contour_Largest contours = new Contour_Largest();
        public int cumulativePixels;
        public CS_Motion_Contours(VBtask task) : base(task)
        {
            labels[2] = "Enclosing rectangles are yellow in dst2 and dst3";
            desc = "Detect contours in the motion data and the resulting rectangles";
        }
        public void RunCS(Mat src)
        {
            dst2 = src;
            motion.Run(src);
            dst3 = motion.dst3;
            var changedPixels = Cv2.CountNonZero(dst3);
            if (task.heartBeat) cumulativePixels = changedPixels;
            else cumulativePixels += changedPixels;
            if (changedPixels > 0)
            {
                contours.Run(dst3);
                DrawContour(dst2, contours.bestContour, Scalar.Yellow);
            }
        }
    }
    public class CS_Motion_Grid_MP : CS_Parent
    {
        Options_MotionDetect options = new Options_MotionDetect();
        public CS_Motion_Grid_MP(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": local options 'Correlation Threshold' controls how well the image matches.");
            desc = "Detect Motion in the color image using multi-threading.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (task.heartBeat) dst3 = src.Clone();
            dst2 = src;
            int updateCount = 0;
            Parallel.ForEach(task.gridList, roi =>
            {
                Mat correlation = new Mat();
                Cv2.MatchTemplate(src[roi], dst3[roi], correlation, TemplateMatchModes.CCoeffNormed);
                if (correlation.At<float>(0, 0) < options.CCthreshold)
                {
                    Interlocked.Increment(ref updateCount);
                    src[roi].CopyTo(dst3[roi]);
                    dst2.Rectangle(roi, Scalar.White, task.lineWidth);
                }
            });
            labels[2] = "Motion added to dst3 for " + updateCount + " segments out of " + task.gridList.Count();
            labels[3] = (task.gridList.Count() - updateCount) + " segments out of " + task.gridList.Count() + " had > " +
                         (options.CCthreshold).ToString("0.0%") + " correlation.";
        }
    }
    public class CS_Motion_Grid : CS_Parent
    {
        Options_MotionDetect options = new Options_MotionDetect();  
        public CS_Motion_Grid(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": local options 'Correlation Threshold' controls how well the image matches.");
            desc = "Detect Motion in the color image";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            float CCthreshold = (float)(options.CCthreshold);
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (task.heartBeat) dst3 = src.Clone();
            List<cv.Rect> roiMotion = new List<cv.Rect>();
            foreach (var roi in task.gridList)
            {
                Mat correlation = new Mat();
                Cv2.MatchTemplate(src[roi], dst3[roi], correlation, TemplateMatchModes.CCoeffNormed);
                if (correlation.At<float>(0, 0) < CCthreshold)
                {
                    src[roi].CopyTo(dst3[roi]);
                    roiMotion.Add(roi);
                }
            }
            dst2 = src;
            foreach (var roi in roiMotion)
            {
                dst2.Rectangle(roi, Scalar.White, task.lineWidth);
            }
            labels[2] = "Motion added to dst3 for " + roiMotion.Count() + " segments out of " + task.gridList.Count();
            labels[3] = (task.gridList.Count() - roiMotion.Count()) + " segments out of " + task.gridList.Count() + " had > " +
                         (options.CCthreshold).ToString("0.0%") + " correlation.";
        }
    }
    public class CS_Motion_Intersect : CS_Parent
    {
        BGSubtract_Basics bgSub = new BGSubtract_Basics();
        int minCount = 4;
        int reconstructedRGB = 0;
        Mat color = new Mat();
        cv.Rect lastMotionRect = new cv.Rect();
        public CS_Motion_Intersect(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            if (dst2.Width == 1280 || dst2.Width == 640) minCount = 16;
            desc = "Track the max rectangle that covers all the motion until there is no motion in it.";
        }
        cv.Rect buildEnclosingRect(Mat tmp)
        {
            List<cv.Rect> rectList = new List<cv.Rect>();
            int[] dots = new int[tmp.Total() * 2];
            Marshal.Copy(tmp.Data, dots, 0, dots.Length);
            List<Point> pointList = new List<Point>();
            for (int i = 0; i < dots.Length; i += 2)
            {
                if (dots[i] >= 1 && dots[i] < dst2.Width - 2 && dots[i + 1] >= 1 && dots[i + 1] < dst2.Height - 2)
                {
                    pointList.Add(new cv.Point(dots[i], dots[i + 1]));
                }
            }
            FloodFillFlags flags = (FloodFillFlags) 4 | FloodFillFlags.MaskOnly | FloodFillFlags.FixedRange;
            cv.Rect rect = new cv.Rect();
            Mat motionMat = new Mat(dst2.Size(), MatType.CV_8U, 0);
            Mat matPoints = dst1[new cv.Rect(1, 1, motionMat.Width - 2, motionMat.Height - 2)];
            foreach (var pt in pointList)
            {
                if (motionMat.At<byte>(pt.Y, pt.X) == 0 && matPoints.At<byte>(pt.Y, pt.X) != 0)
                {
                    int count = matPoints.FloodFill(motionMat, pt, 255, out rect, 0, 0, flags | (FloodFillFlags)(255 << 8));
                    if (count <= minCount) continue;
                    rectList.Add(new cv.Rect(rect.X, rect.Y, rect.Width + 1, rect.Height + 1));
                }
            }
            labels[3] = "There were " + (dots.Length / 2) + " points collected";
            if (rectList.Count() == 0) return new cv.Rect();
            cv.Rect motionRect = rectList[0];
            foreach (var r in rectList)
            {
                motionRect = motionRect.Union(r);
            }
            return motionRect;
        }
        public void RunCS(Mat src)
        {
            if (task.FirstPass) color = src.Clone();
            if (task.FirstPass) lastMotionRect = task.motionRect;
            task.motionFlag = false;
            if (task.heartBeat || task.motionRect.Width * task.motionRect.Height > src.Total() / 2 || task.optionsChanged)
            {
                task.motionFlag = true;
            }
            else
            {
                bgSub.Run(src);
                dst1 = bgSub.dst2;
                Mat tmp = new Mat();
                Cv2.FindNonZero(dst1, tmp);
                if (tmp.Total() > src.Total() / 2)
                {
                    task.motionFlag = true;
                }
                else if (tmp.Total() > 0)
                {
                    reconstructedRGB += 1;
                    task.motionRect = buildEnclosingRect(tmp);
                    if (task.motionRect.IntersectsWith(lastMotionRect)) task.motionRect = task.motionRect.Union(lastMotionRect);
                    if (task.motionRect.Width * task.motionRect.Height > src.Total() / 2) task.motionFlag = true;
                }
            }
            dst3.SetTo(0);
            if (task.motionFlag)
            {
                labels[2] = reconstructedRGB + " frames since last full image";
                reconstructedRGB = 0;
                task.motionRect = new cv.Rect();
                dst2 = src.Clone();
            }
            if (standaloneTest())
            {
                dst2 = dst1;
                if (task.motionRect.Width > 0 && task.motionRect.Height > 0)
                {
                    dst3[task.motionRect].SetTo(255);
                    src[task.motionRect].CopyTo(dst2[task.motionRect]);
                }
            }
            if (standaloneTest())
            {
                if (task.motionRect.Width > 0 && task.motionRect.Height > 0)
                {
                    src[task.motionRect].CopyTo(dst0[task.motionRect]);
                    color.Rectangle(task.motionRect, Scalar.White, task.lineWidth, task.lineType);
                }
            }
            lastMotionRect = task.motionRect;
        }
    }
    public class CS_Motion_RectTest : CS_Parent
    {
        Motion_Enclosing motion = new Motion_Enclosing();
        Diff_Basics diff = new Diff_Basics();
        List<cv.Rect> lastRects = new List<cv.Rect>();
        public CS_Motion_RectTest(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": gOptions frame history slider will impact results.");
            labels[3] = "The white spots show the difference of the constructed image from the current image.";
            desc = "Track the RGB image using Motion_Enclosing to isolate the motion";
        }
        public void RunCS(Mat src)
        {
            motion.Run(src);
            cv.Rect r = motion.motionRect;
            if (task.heartBeat || r.Width * r.Height > src.Total() / 2 || task.frameCount < 50)
            {
                dst2 = src.Clone();
                lastRects.Clear();
            }
            else
            {
                if (r.Width > 0 && r.Height > 0)
                {
                    foreach (var rect in lastRects)
                    {
                        r = r.Union(rect);
                    }
                    src[r].CopyTo(dst2[r]);
                    lastRects.Add(r);
                    if (lastRects.Count() > task.frameHistoryCount) lastRects.RemoveAt(0);
                }
                else
                {
                    lastRects.Clear();
                }
            }
            if (standaloneTest())
            {
                diff.lastFrame = src.CvtColor(ColorConversionCodes.BGR2GRAY);
                diff.Run(dst2);
                dst3 = diff.dst2.CvtColor(ColorConversionCodes.GRAY2BGR);
                dst3.Rectangle(r, task.HighlightColor, task.lineWidth, task.lineType);
            }
        }
    }
    public class CS_Motion_HistoryTest : CS_Parent
    {
        Diff_Basics diff = new Diff_Basics();
        History_Basics frames = new History_Basics();
        public CS_Motion_HistoryTest(VBtask task) : base(task)
        {
            task.gOptions.pixelDiffThreshold = 10;
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Detect motion using the last X images";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            diff.Run(src);
            dst1 = diff.dst2.Threshold(0, 1, ThresholdTypes.Binary);
            frames.Run(dst1);
            dst2 = frames.dst2.Threshold(0, 255, ThresholdTypes.Binary);
            labels[2] = "Cumulative diff for the last " + task.frameHistoryCount + " frames";
        }
    }
    public class CS_Motion_History : CS_Parent
    {
        public Motion_Simple motionCore = new Motion_Simple();
        History_Basics frames = new History_Basics();
        public CS_Motion_History(VBtask task) : base(task)
        {
            task.frameHistoryCount = 10;
            desc = "Accumulate differences from the previous BGR images.";
        }
        public void RunCS(Mat src)
        {
            motionCore.Run(src);
            dst2 = motionCore.dst2;
            frames.Run(dst2);
            dst3 = frames.dst2;
        }
    }
    public class CS_Motion_Enclosing : CS_Parent
    {
        RedCloud_Basics redMasks = new RedCloud_Basics();
        double learnRate;
        public cv.Rect motionRect = new cv.Rect();
        public CS_Motion_Enclosing(VBtask task) : base(task)
        {
            if (dst2.Width >= 1280) learnRate = 0.5; else learnRate = 0.1; // learn faster with large images (slower frame rate)
            cPtr = BGSubtract_BGFG_Open(4);
            labels[2] = "MOG2 is the best option.  See BGSubtract_Basics to see more options.";
            desc = "Build an enclosing rectangle for the motion";
        }
        public void RunCS(Mat src)
        {
            byte[] dataSrc = new byte[src.Total() * src.ElemSize()];
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
            GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            IntPtr imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels(), learnRate);
            handleSrc.Free();
            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr).Threshold(0, 255, ThresholdTypes.Binary);
            redMasks.inputMask = ~dst2;
            redMasks.Run(dst2);
            motionRect = new cv.Rect();
            if (task.redCells.Count() < 2) return;
            motionRect = task.redCells[1].rect;
            for (int i = 2; i < task.redCells.Count(); i++)
            {
                var cell = task.redCells[i];
                motionRect = motionRect.Union(cell.rect);
            }
            if (motionRect.Width > dst2.Width / 2 && motionRect.Height > dst2.Height / 2)
            {
                motionRect = new cv.Rect(0, 0, dst2.Width, dst2.Height);
            }
            dst2.Rectangle(motionRect, 255, task.lineWidth, task.lineType);
        }
        public void Close()
        {
            if (cPtr != (IntPtr)0) cPtr = BGSubtract_BGFG_Close(cPtr);
        }
    }
    public class CS_Motion_Depth : CS_Parent
    {
        Diff_Depth32f diff = new Diff_Depth32f();
        public CS_Motion_Depth(VBtask task) : base(task)
        {
            labels = new string[] { "", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud" };
            desc = "Display the depth data after updating only the motion rectangle.  Resync every heartbeat.";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat) dst2 = task.pcSplit[2].Clone();
            if (task.motionDetected) task.pcSplit[2][task.motionRect].CopyTo(dst2[task.motionRect]);
            if (standaloneTest())
            {
                if (diff.lastDepth32f.Width == 0) diff.lastDepth32f = task.pcSplit[2].Clone();
                diff.Run(task.pcSplit[2]);
                dst3 = diff.dst2;
                dst3.Rectangle(task.motionRect, 255, task.lineWidth);
                diff.lastDepth32f = task.pcSplit[2];
            }
        }
    }
    public class CS_Motion_Grayscale : CS_Parent
    {
        Diff_Basics diff = new Diff_Basics();
        public CS_Motion_Grayscale(VBtask task) : base(task)
        {
            labels = new string[] { "", "MotionRect_Basics output showing motion and enclosing rectangle.", "MotionRect accumulated grayscale image", "Diff of input and latest accumulated grayscale image" };
            desc = "Display the grayscale image after updating only the motion rectangle.  Resync every heartbeat.";
        }
        public void RunCS(Mat src)
        {
            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            if (task.heartBeat)
            {
                dst2 = src.Clone();
            }
            else if (task.motionDetected)
            {
                src[task.motionRect].CopyTo(dst2[task.motionRect]);
            }
            else
            {
                dst2 = src.Clone();
            }
            if (standaloneTest())
            {
                if (diff.lastFrame == null) diff.lastFrame = dst2.Clone();
                if (diff.lastFrame.Width == 0) diff.lastFrame = dst2.Clone();
                diff.Run(src);
                dst3 = diff.dst2;
                dst3.Rectangle(task.motionRect, 255, task.lineWidth);
                diff.lastFrame = src.Clone();
            }
        }
    }
    public class CS_Motion_Basics_QT : CS_Parent
    {
        RedCloud_Basics redMasks = new RedCloud_Basics();
        public BGSubtract_MOG2 bgSub = new BGSubtract_MOG2();
        List<cv.Rect> rectList = new List<cv.Rect>();
        public CS_Motion_Basics_QT(VBtask task) : base(task)
        {
            task.redOptions.setIdentifyCells(false);
            desc = "The option-free version of Motion_Basics";
        }
        public void RunCS(Mat src)
        {
            task.motionDetected = true;
            if (task.heartBeat)
            {
                task.motionRect = new cv.Rect(0, 0, dst2.Width, dst2.Height);
                return;
            }
            task.motionRect = new cv.Rect();
            if (src.Channels() != 1)
            {
                bgSub.Run(src);
                src = bgSub.dst2;
            }
            dst2 = src;
            redMasks.Run(src.Threshold(0, 255, ThresholdTypes.Binary));
            if (task.redCells.Count() < 2)
            {
                task.motionDetected = false;
                rectList.Clear();
            }
            else
            {
                var nextRect = task.redCells[1].rect;
                for (int i = 2; i < task.redCells.Count(); i++)
                {
                    var rc = task.redCells[i];
                    nextRect = nextRect.Union(rc.rect);
                }
                rectList.Add(nextRect);
                foreach (var r in rectList)
                {
                    if (task.motionRect.Width == 0) task.motionRect = r; else task.motionRect = task.motionRect.Union(r);
                }
                if (rectList.Count() > task.frameHistoryCount) rectList.RemoveAt(0);
                if (task.motionRect.Width > dst2.Width / 2 && task.motionRect.Height > dst2.Height / 2)
                {
                    task.motionRect = new cv.Rect(0, 0, dst2.Width, dst2.Height);
                }
                else
                {
                    if (task.motionRect.Width == 0 || task.motionRect.Height == 0) task.motionDetected = false;
                }
            }
            if (standaloneTest())
            {
                dst2.Rectangle(task.motionRect, 255, task.lineWidth);
                if (task.redCells.Count() > 1)
                {
                    labels[2] = task.redCells.Count().ToString() + " RedMask cells had motion";
                }
                else
                {
                    labels[2] = "No motion detected";
                }
                labels[3] = "";
                if (task.motionRect.Width > 0)
                {
                    labels[3] = "Rect width = " + task.motionRect.Width + ", height = " + task.motionRect.Height;
                }
            }
        }
    }
    public class CS_Motion_PointCloud : CS_Parent
    {
        Diff_Depth32f diff = new Diff_Depth32f();
        public CS_Motion_PointCloud(VBtask task) : base(task)
        {
            labels = new string[] { "", "Output of MotionRect_Basics showing motion and enclosing rectangle.", "MotionRect point cloud", "Diff of MotionRect Pointcloud and latest pointcloud" };
            desc = "Display the pointcloud after updating only the motion rectangle.  Resync every heartbeat.";
        }
        public void RunCS(Mat src)
        {
            if (task.motionDetected) task.pointCloud[task.motionRect].CopyTo(dst2[task.motionRect]);
            if (standaloneTest())
            {
                if (diff.lastDepth32f.Width == 0) diff.lastDepth32f = task.pcSplit[2].Clone();
                diff.Run(task.pcSplit[2]);
                dst3 = diff.dst2;
                dst3.Rectangle(task.motionRect, 255, task.lineWidth);
                diff.lastDepth32f = task.pcSplit[2];
            }
        }
    }
    public class CS_Motion_Color : CS_Parent
    {
        public CS_Motion_Color(VBtask task) : base(task)
        {
            labels = new string[] { "", "MotionRect_Basics output showing motion and enclosing rectangle.", "MotionRect accumulated color image", "Diff of input and latest accumulated color image" };
            desc = "Display the color image after updating only the motion rectangle.  Resync every heartbeat.";
        }
        public void RunCS(Mat src)
        {
            if (task.motionDetected) src[task.motionRect].CopyTo(dst2[task.motionRect]);
            if (standaloneTest() && task.motionDetected) dst2.Rectangle(task.motionRect, Scalar.White, task.lineWidth);
        }
    }
    public class CS_Motion_BasicsQuarterRes : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public BGSubtract_MOG2_QT bgSub = new BGSubtract_MOG2_QT();
        List<cv.Rect> rectList = new List<cv.Rect>();
        public CS_Motion_BasicsQuarterRes(VBtask task) : base(task)
        {
            desc = "The option-free version of Motion_Basics";
        }
        public void RunCS(Mat src)
        {
            task.motionDetected = true;
            task.motionRect = new cv.Rect(0, 0, dst2.Width, dst2.Height);
            if (src.Channels() != 1)
            {
                bgSub.Run(src);
                dst2 = bgSub.dst2;
            }
            else
            {
                dst2 = src;
            }
            if (dst2.Size() != task.quarterRes)
            {
                dst2 = dst2.Resize(task.quarterRes).Threshold(0, 255, ThresholdTypes.Binary);
            }
            else
            {
                dst2 = src.Threshold(0, 255, ThresholdTypes.Binary);
            }
            redC.inputMask = ~dst2;
            redC.Run(dst2);
            if (task.redCells.Count() <= 2)
            {
                task.motionDetected = false;
            }
            else
            {
                cv.Rect nextRect = task.redCells[1].rect;
                for (int i = 2; i < task.redCells.Count(); i++)
                {
                    var rc = task.redCells[i];
                    nextRect = nextRect.Union(rc.rect);
                }
                rectList.Add(nextRect);
                task.motionRect = rectList[0];
                for (int i = 1; i < rectList.Count(); i++)
                {
                    task.motionRect = task.motionRect.Union(rectList[i]);
                }
                if (rectList.Count() > task.frameHistoryCount) rectList.RemoveAt(0);
                if (task.motionRect.Width > dst2.Width / 2 && task.motionRect.Height > dst2.Height / 2)
                {
                    task.motionRect = new cv.Rect(0, 0, dst2.Width, dst2.Height);
                }
                else
                {
                    if (task.motionRect.Width == 0 || task.motionRect.Height == 0) task.motionDetected = false;
                }
            }
            if (standaloneTest())
            {
                dst2.Rectangle(task.motionRect, 255, task.lineWidth);
                if (task.redCells.Count() > 1)
                {
                    labels[2] = task.redCells.Count().ToString() + " RedMask cells had motion";
                }
                else
                {
                    labels[2] = "No motion detected";
                }
                labels[3] = "";
                if (task.motionRect.Width > 0)
                {
                    labels[3] = "Rect width = " + task.motionRect.Width + ", height = " + task.motionRect.Height;
                }
            }
            int ratio = src.Width / dst2.Width;
            if (src.Size() != dst2.Size())
            {
                cv.Rect r = task.motionRect;
                task.motionRect = new cv.Rect(r.X * ratio, r.Y * ratio, r.Width * ratio, r.Height * ratio);
            }
            if (task.motionRect.Width < dst2.Width)
            {
                dst2.Rectangle(task.motionRect, 255, task.lineWidth);
                int pad = dst2.Width / 20;
                cv.Rect r = task.motionRect;
                r = new cv.Rect(r.X - pad, r.Y - pad, r.Width + pad * 2, r.Height + pad * 2);
                task.motionRect = ValidateRect(r, ratio);
                dst2.Rectangle(task.motionRect, 255, task.lineWidth + 1);
            }
        }
    }
    public class CS_Motion_Diff : CS_Parent
    {
        public CS_Motion_Diff(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            labels = new string[] { "", "", "Unstable mask", "Pixel difference" };
            desc = "Capture an image and use absDiff/threshold to compare it to the last snapshot";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (task.heartBeat)
            {
                dst1 = src.Clone();
                dst2.SetTo(0);
            }
            Cv2.Absdiff(src, dst1, dst3);
            dst2 = dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, ThresholdTypes.Binary);
        }
    }
    public class CS_Motion_MinRect : CS_Parent
    {
        public Motion_Diff motion = new Motion_Diff();
        Area_MinRect mRect = new Area_MinRect();
        public CS_Motion_MinRect(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            desc = "Find the nonzero points of motion and fit an rotated rectangle to them.";
        }
        public void RunCS(Mat src)
        {
            motion.Run(src);
            dst2 = motion.dst2;
            var nonzeros = dst2.FindNonZero();
            if (task.heartBeat) dst3.SetTo(0);
            if (nonzeros.Rows > 5)
            {
                List<int> ptx = new List<int>();
                List<int> pty = new List<int>();
                List<Point> inputPoints = new List<Point>();
                for (int i = 0; i < nonzeros.Rows; i++)
                {
                    cv.Point pt = nonzeros.Get<Point>(i, 0);
                    inputPoints.Add(pt);
                    ptx.Add(pt.X);
                    pty.Add(pt.Y);
                }
                cv.Point p1 = inputPoints[ptx.IndexOf(ptx.Max())];
                cv.Point p2 = inputPoints[ptx.IndexOf(ptx.Min())];
                cv.Point p3 = inputPoints[pty.IndexOf(pty.Max())];
                cv.Point p4 = inputPoints[pty.IndexOf(pty.Min())];
                mRect.inputPoints = new List<Point2f> { p1, p2, p3, p4 };
                mRect.Run(empty);
                DrawRotatedRect(mRect.minRect, dst3, Scalar.White);
            }
        }
    }
    public class CS_Motion_RedCloud : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Motion_RedCloud(VBtask task) : base(task)
        {
            labels[3] = "Motion detected in the cells below";
            desc = "Use RedCloud to define where there is motion";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            dst3.SetTo(0);
            foreach (var rc in task.redCells)
            {
                if (rc.motionPixels > 0) dst3[rc.rect].SetTo(rc.naturalColor, rc.mask);
            }
        }
    }
    public class CS_Mouse_Basics : CS_Parent
    {
        cv.Point lastPoint = new cv.Point();
        int colorIndex;
        public CS_Mouse_Basics(VBtask task) : base(task)
        {
            labels[2] = "Move the mouse below to show mouse tracking.";
            desc = "Test the mousePoint interface";
        }
        public void RunCS(Mat src)
        {
            // only display mouse movement in the lower left image (pic.tag = 2)
            if (lastPoint == task.mouseMovePoint || task.mousePicTag != 2) return;
            lastPoint = task.mouseMovePoint;
            Scalar nextColor = task.scalarColors[colorIndex];
            cv.Point nextPt = task.mouseMovePoint;
            DrawCircle(dst2, nextPt, task.DotSize + 3, nextColor);
            colorIndex++;
            if (colorIndex >= task.scalarColors.Count()) colorIndex = 0;
        }
    }
    public class CS_Mouse_LeftClickZoom : CS_Parent
    {
        public CS_Mouse_LeftClickZoom(VBtask task) : base(task)
        {
            labels[2] = "Left click and drag to draw a rectangle";
            desc = "Demonstrate what the left-click enables";
        }
        public void RunCS(Mat src)
        {
            SetTrueText("Left-click and drag to select a region in any of the images." + "\n" +
                        "The selected area is a rectangle that is saved in task.drawRect." + "\n" +
                        "In this example, the selected region from the BGR image will be resized to fit in the Result2 image to the right." + "\n" +
                        "Double-click an image to remove the selected region.");
            if (task.drawRect.Width != 0 && task.drawRect.Height != 0)
                dst3 = src[task.drawRect].Resize(dst3.Size());
        }
    }
    public class CS_Mouse_ClickPointUsage : CS_Parent
    {
        Feature_Basics feat = new Feature_Basics();
        public CS_Mouse_ClickPointUsage(VBtask task) : base(task)
        {
            desc = "This algorithm shows how to use task.ClickPoint to dynamically identify what to break on.";
        }
        public void RunCS(Mat src)
        {
            SetTrueText("Click on one of the feature points (carefully) to hit the breakpoint below.");
            feat.Run(src);
            dst2 = feat.dst2;
            foreach (var pt in task.features)
            {
                if (pt == task.ClickPoint)
                {
                    Console.WriteLine("Hit the point you selected.");
                }
            }
        }
    }
    public class CS_MSER_Basics : CS_Parent
    {
        MSER_CPP detect = new MSER_CPP();
        public List<rcData> mserCells = new List<rcData>();
        public List<Point> floodPoints = new List<Point>();
        public CS_MSER_Basics(VBtask task) : base(task)
        {
            desc = "Create cells for each region in MSER output";
        }
        public void RunCS(Mat src)
        {
            detect.Run(src);
            var boxInput = new List<cv.Rect>(detect.boxes);
            var boxes = new SortedList<int, int>(new compareAllowIdenticalIntegerInverted());
            for (int i = 0; i < boxInput.Count(); i++)
            {
                var r = boxInput[i];
                boxes.Add(r.Width * r.Height, i);
            }
            floodPoints = new List<Point>(detect.floodPoints);
            var sortedCells = new SortedList<int, rcData>(new compareAllowIdenticalIntegerInverted());
            var matched = new SortedList<int, int>(new compareAllowIdenticalIntegerInverted());
            dst0 = detect.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            for (int i = 0; i < boxes.Count(); i++)
            {
                var index = boxes.ElementAt(i).Value;
                var rc = new rcData();
                rc.rect = boxInput[index];
                var val = dst0.Get<byte>(floodPoints[index].Y, floodPoints[index].X);
                rc.mask = dst0[rc.rect].InRange(val, val);
                rc.pixels = detect.maskCounts[index];
                rc.contour = contourBuild(rc.mask, ContourApproximationModes.ApproxNone);
                DrawContour(rc.mask, rc.contour, 255, -1);
                rc.floodPoint = floodPoints[index];
                rc.maxDist = GetMaxDist(ref rc);
                rc.indexLast = task.cellMap.Get<byte>(rc.maxDist.Y, rc.maxDist.X);
                if (rc.indexLast != 0 && rc.indexLast < task.redCells.Count())
                {
                    var lrc = task.redCells[rc.indexLast];
                    rc.maxDStable = lrc.maxDStable;
                    rc.color = lrc.color;
                    matched.Add(rc.indexLast, rc.indexLast);
                }
                else
                {
                    rc.maxDStable = rc.maxDist;
                }
                cv.Scalar mean, stdev;
                Cv2.MeanStdDev(task.color[rc.rect], out mean, out stdev, rc.mask);
                rc.colorMean = mean;
                rc.colorStdev = stdev;
                rc.naturalColor = new Vec3b((byte)rc.colorMean[0], (byte)rc.colorMean[1], (byte)rc.colorMean[2]);
                if (rc.pixels > 0) sortedCells.Add(rc.pixels, rc);
            }
            dst2 = RebuildCells(sortedCells);
            labels[2] = task.redCells.Count().ToString() + " cells were identified and " + matched.Count().ToString() + " were matched.";
        }
    }
    public class CS_MSER_Detect : CS_Parent
    {
        public cv.Rect[] boxes;
        public Point[][] regions;
        public MSER mser = MSER.Create();
        public Options_MSER options = new Options_MSER();
        public int classCount;
        public CS_MSER_Detect(VBtask task) : base(task)
        {
            desc = "Run the core MSER (Maximally Stable Extremal Region) algorithm";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2 = src.Clone();
            if (task.optionsChanged)
            {
                mser = MSER.Create(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                                   options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize);
                mser.Pass2Only = options.pass2Setting != 0 ? true : false;
            }
            if (options.graySetting && src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            mser.DetectRegions(src, out regions, out boxes);
            classCount = boxes.Length;
            foreach (var z in boxes)
            {
                dst2.Rectangle(z, Scalar.Yellow, 1);
            }
        }
    }
    public class CS_MSER_SyntheticInput : CS_Parent
    {
        void addNestedRectangles(Mat img, cv.Point p0, int[] width, int[] color, int n)
        {
            for (int i = 0; i < n; i++)
            {
                img.Rectangle(new cv.Rect(p0.X, p0.Y, width[i], width[i]), color[i], 1);
                p0 += new cv.Point((width[i] - width[i + 1]) / 2, (width[i] - width[i + 1]) / 2);
                img.FloodFill(p0, color[i]);
            }
        }
        void addNestedCircles(Mat img, cv.Point p0, int[] width, int[] color, int n)
        {
            for (int i = 0; i < n; i++)
            {
                DrawCircle(img, p0, width[i] / 2, color[i]);
                img.FloodFill(p0, color[i]);
            }
        }
        public CS_MSER_SyntheticInput(VBtask task) : base(task)
        {
            desc = "Build a synthetic image for MSER (Maximal Stable Extremal Regions) testing";
        }
        public void RunCS(Mat src)
        {
            var img = new Mat(800, 800, MatType.CV_8U, 0);
            int[] width = { 390, 380, 300, 290, 280, 270, 260, 250, 210, 190, 150, 100, 80, 70 };
            int[] color1 = { 80, 180, 160, 140, 120, 100, 90, 110, 170, 150, 140, 100, 220 };
            int[] color2 = { 81, 181, 161, 141, 121, 101, 91, 111, 171, 151, 141, 101, 221 };
            int[] color3 = { 175, 75, 95, 115, 135, 155, 165, 145, 85, 105, 115, 155, 35 };
            int[] color4 = { 173, 73, 93, 113, 133, 153, 163, 143, 83, 103, 113, 153, 33 };
            addNestedRectangles(img, new cv.Point(10, 10), width, color1, 13);
            addNestedCircles(img, new cv.Point(200, 600), width, color2, 13);
            addNestedRectangles(img, new cv.Point(410, 10), width, color3, 13);
            addNestedCircles(img, new cv.Point(600, 600), width, color4, 13);
            img = img.Resize(new cv.Size(src.Rows, src.Rows));
            dst2[new cv.Rect(0, 0, src.Rows, src.Rows)] = img.CvtColor(ColorConversionCodes.GRAY2BGR);
        }
    }
    public class CS_MSER_LeftRight : CS_Parent
    {
        MSER_Left left = new MSER_Left();
        MSER_Right right = new MSER_Right();
        public CS_MSER_LeftRight(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "MSER_Basics output for left camera", "MSER_Basics output for right camera" };
            desc = "Test MSER (Maximally Stable Extremal Region) algorithm on the left and right views.";
        }
        public void RunCS(Mat src)
        {
            left.Run(task.leftView);
            dst2 = left.dst2;
            labels[2] = left.labels[2];
            right.Run(task.rightView);
            dst3 = right.dst2;
            labels[3] = right.labels[2];
        }
    }
    public class CS_MSER_Left : CS_Parent
    {
        MSER_Basics mBase = new MSER_Basics();
        public CS_MSER_Left(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "MSER_Basics output for left camera", "MSER_Basics rectangles found" };
            desc = "Test MSER (Maximally Stable Extremal Region) algorithm on the left and right views.";
        }
        public void RunCS(Mat src)
        {
            mBase.Run(task.leftView);
            dst2 = mBase.dst2;
            dst3 = mBase.dst3;
            labels[2] = mBase.labels[2];
        }
    }
    public class CS_MSER_Right : CS_Parent
    {
        MSER_Basics mBase = new MSER_Basics();
        public CS_MSER_Right(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "MSER_Basics output for right camera", "MSER_Basics rectangles found" };
            desc = "Test MSER (Maximally Stable Extremal Region) algorithm on the left and right views.";
        }
        public void RunCS(Mat src)
        {
            mBase.Run(task.rightView);
            dst2 = mBase.dst2;
            dst3 = mBase.dst3;
            labels[2] = mBase.labels[2];
        }
    }
    public class CS_MSER_Hulls : CS_Parent
    {
        Options_MSER options = new Options_MSER();
        MSER_Basics mBase = new MSER_Basics();
        public CS_MSER_Hulls(VBtask task) : base(task)
        {
            desc = "Use MSER (Maximally Stable Extremal Region) but show the contours of each region.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            mBase.Run(src);
            dst2 = mBase.dst2;
            int pixels = 0;
            dst3.SetTo(0);
            foreach (var rc in mBase.mserCells)
            {
                rc.hull = Cv2.ConvexHull(rc.contour.ToArray(), true).ToList();
                pixels += rc.pixels;
                DrawContour(dst3[rc.rect], rc.hull, vecToScalar(rc.color), -1);
            }
            if (task.heartBeat) labels[2] = mBase.mserCells.Count() + " Regions with average size " + (mBase.mserCells.Count() > 0 ?
                (pixels / mBase.mserCells.Count()).ToString() : "0");
        }
    }
    public class CS_MSER_TestSynthetic : CS_Parent
    {
        Options_MSER options = new Options_MSER();
        MSER_SyntheticInput synth = new MSER_SyntheticInput();
        MSER_Basics mBase = new MSER_Basics();
        public CS_MSER_TestSynthetic(VBtask task) : base(task)
        {
            FindCheckBox("Use grayscale input").Checked = true;
            labels = new string[] { "", "", "Synthetic input", "Output from MSER (Maximally Stable Extremal Region)" };
            desc = "Test MSER (Maximally Stable Extremal Region) with the synthetic image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            synth.Run(src);
            dst2 = synth.dst2.Clone();
            mBase.Run(dst2);
            dst3 = mBase.dst3;
        }
    }
    public class CS_MSER_Grayscale : CS_Parent
    {
        MSER_Basics mBase = new MSER_Basics();
        Reduction_Basics reduction = new Reduction_Basics();
        public CS_MSER_Grayscale(VBtask task) : base(task)
        {
            FindCheckBox("Use grayscale input").Checked = true;
            desc = "Run MSER (Maximally Stable Extremal Region) with grayscale input";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            mBase.Run(reduction.dst2);
            dst2 = mBase.dst3;
            labels[2] = mBase.labels[2];
        }
    }
    public class CS_MSER_ReducedRGB : CS_Parent
    {
        MSER_Basics mBase = new MSER_Basics();
        Reduction_BGR reduction = new Reduction_BGR();
        public CS_MSER_ReducedRGB(VBtask task) : base(task)
        {
            FindCheckBox("Use grayscale input").Checked = false;
            desc = "Run MSER (Maximally Stable Extremal Region) with a reduced RGB input";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            mBase.Run(reduction.dst2);
            dst2 = mBase.dst3;
            labels[2] = mBase.labels[2];
        }
    }
    public class CS_MSER_ROI : CS_Parent
    {
        public List<cv.Rect> containers = new List<cv.Rect>();
        Options_MSER options = new Options_MSER();
        MSER_Detect core = new MSER_Detect();
        public CS_MSER_ROI(VBtask task) : base(task)
        {
            desc = "Identify the main regions of interest with MSER (Maximally Stable Extremal Region)";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2 = src.Clone();
            dst3 = src.Clone();
            core.Run(src);
            var sortedBoxes = new SortedList<int, cv.Rect>(new CompareAllowIdenticalIntegerInverted());
            foreach (var box in core.boxes)
            {
                sortedBoxes.Add(box.Width * box.Height, box);
            }
            var boxList = new List<cv.Rect>();
            for (int i = 0; i < sortedBoxes.Count(); i++)
            {
                boxList.Add(sortedBoxes.ElementAt(i).Value);
            }
            containers.Clear();
            while (boxList.Count() > 0)
            {
                var box = boxList[0];
                containers.Add(box);
                var removeBoxes = new List<int>();
                for (int i = 0; i < boxList.Count(); i++)
                {
                    var b = boxList[i];
                    var center = new cv.Point((int)(b.X + b.Width / 2), (int)(b.Y + b.Height / 2));
                    if (center.X >= box.X && center.X <= (box.X + box.Width) && center.Y >= box.Y && center.Y <= (box.Y + box.Height))
                    {
                        removeBoxes.Add(i);
                        dst3.Rectangle(b, task.HighlightColor, task.lineWidth);
                    }
                }
                for (int i = removeBoxes.Count() - 1; i >= 0; i--)
                {
                    boxList.RemoveAt(removeBoxes[i]);
                }
            }
            foreach (var rect in containers)
            {
                dst2.Rectangle(rect, task.HighlightColor, task.lineWidth);
            }
            labels[2] = containers.Count().ToString() + " consolidated regions of interest located";
            labels[3] = sortedBoxes.Count().ToString() + " total rectangles found with MSER";
        }
    }
    public class CS_MSER_TestExample : CS_Parent
    {
        Mat image;
        MSER mser;
        Options_MSER options = new Options_MSER();
        public CS_MSER_TestExample(VBtask task) : base(task)
        {
            labels[2] = "Contour regions from MSER";
            labels[3] = "Box regions from MSER";
            if (standaloneTest()) task.gOptions.setDisplay1();
            desc = "Maximally Stable Extremal Regions example - still image";
            image = Cv2.ImRead(task.HomeDir + "Data/MSERtestfile.jpg", ImreadModes.Color);
            mser = MSER.Create();
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Point[][] regions;
            cv.Rect[] boxes;
            dst0 = image.Clone();
            dst2 = image.Clone();
            dst3 = image.Clone();
            if (task.optionsChanged)
            {
                mser = MSER.Create(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                                   options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize);
                mser.Pass2Only = options.pass2Setting != 0 ? true : false;
            }
            mser.DetectRegions(dst2, out regions, out boxes);
            int index = 0;
            foreach (var pts in regions)
            {
                var color = task.vecColors[index % 256];
                foreach (var pt in pts)
                {
                    dst2.Set<Vec3b>(pt.Y, pt.X, color);
                }
                index++;
            }
            foreach (var box in boxes)
            {
                dst3.Rectangle(box, task.HighlightColor, task.lineWidth + 1, task.lineType);
            }
            labels[2] = boxes.Length.ToString() + " regions were found using MSER";
        }
    }
    public class CS_MSER_RedCloud : CS_Parent
    {
        MSER_Basics mBase = new MSER_Basics();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_MSER_RedCloud(VBtask task) : base(task)
        {
            desc = "Use the MSER_Basics output as input to RedCloud_Basics";
        }
        public void RunCS(Mat src)
        {
            mBase.Run(src);
            redC.Run(mBase.dst2.CvtColor(ColorConversionCodes.BGR2GRAY));
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
        }
    }
    public class CS_MSER_Mask_CPP : CS_Parent
    {
        Options_MSER options = new Options_MSER();
        RedCloud_Cells redC = new RedCloud_Cells();
        public int classCount;
        public CS_MSER_Mask_CPP(VBtask task) : base(task)
        {
            task.redOptions.setUseColorOnly(true);
            FindCheckBox("Use grayscale input").Checked = false;
            options.RunVB();
            cPtr = MSER_Open(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                             options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize, 
                             options.pass2Setting);
            desc = "MSER in a nutshell: intensity threshold, stability, maximize region, adaptive threshold.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.optionsChanged)
            {
                MSER_Close(cPtr);
                cPtr = MSER_Open(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                                 options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize, 
                                 options.pass2Setting);
            }
            if (options.graySetting && src.Channels() == 3)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (task.heartBeat)
            {
                byte[] cppData = new byte[src.Total() * src.ElemSize()];
                Marshal.Copy(src.Data, cppData, 0, cppData.Length);
                var handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned);
                IntPtr imagePtr = MSER_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels());
                handleSrc.Free();
                classCount = MSER_Count(cPtr);
                if (classCount == 0) return;
                dst3 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr).InRange(255, 255);
            }
            labels[3] = classCount.ToString() + " regions identified";
            src.SetTo(Scalar.White, dst3);
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
        }
        public void Close()
        {
            MSER_Close(cPtr);
        }
    }
    public class CS_MSER_Binarize : CS_Parent
    {
        MSER_Basics mser = new MSER_Basics();
        Bin4Way_Regions bin4 = new Bin4Way_Regions();
        public CS_MSER_Binarize(VBtask task) : base(task)
        {
            desc = "Instead of a BGR src, try using the color output of Bin4Way_Regions";
        }
        public void RunCS(Mat src)
        {
            bin4.Run(src);
            dst2 = ShowPalette(bin4.dst2 * 255 / 4);
            mser.Run(dst2);
            dst3 = mser.dst2;
            labels[3] = mser.labels[2];
        }
    }
    public class CS_MSER_Basics1 : CS_Parent
    {
        MSER_CPP detect = new MSER_CPP();
        RedCloud_Basics flood = new RedCloud_Basics();
        public CS_MSER_Basics1(VBtask task) : base(task)
        {
            desc = "Create cells for each region in MSER output";
        }
        public void RunCS(Mat src)
        {
            detect.Run(src);
            dst3 = detect.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            flood.Run(dst3);
            dst2 = flood.dst2;
            labels[2] = flood.labels[2];
        }
    }
    public class CS_MSER_BasicsNew : CS_Parent
    {
        MSER_CPP detect = new MSER_CPP();
        int displaycount;
        public CS_MSER_BasicsNew(VBtask task) : base(task)
        {
            desc = "Create cells for each region in MSER output";
        }
        public void RunCS(Mat src)
        {
            detect.Run(src);
            var boxInput = new List<cv.Rect>(detect.boxes);
            var boxes = new SortedList<int, cv.Rect>(new compareAllowIdenticalIntegerInverted());
            for (int i = 0; i < boxInput.Count(); i++)
            {
                var r = boxInput[i];
                boxes.Add(r.Width * r.Height, r);
            }
            dst3 = src;
            for (int i = 0; i < boxes.Count(); i++)
            {
                var r = boxes.ElementAt(i).Value;
                dst3.Rectangle(r, task.HighlightColor, task.lineWidth);
                if (i >= displaycount) break;
            }
            if (task.heartBeat)
            {
                labels[2] = "Displaying the largest " + displaycount + " rectangles out of " + boxes.Count() + " found";
                if (displaycount >= boxes.Count()) displaycount = 0;
            }
        }
    }
    public class CS_MSER_Basics2 : CS_Parent
    {
        MSER_CPP detect = new MSER_CPP();
        Mat cellMap;
        public CS_MSER_Basics2(VBtask task) : base(task)
        {
            cellMap = new Mat(dst2.Size(), MatType.CV_8U, 0);
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            desc = "Create cells for each region in MSER output";
        }
        public void RunCS(Mat src)
        {
            detect.Run(src);
            dst3 = detect.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            var floodPoints = new List<Point>(detect.floodPoints);
            var boxInput = new List<cv.Rect>(detect.boxes);
            var boxes = new SortedList<int, int>(new compareAllowIdenticalIntegerInverted());
            for (int i = 0; i < boxInput.Count(); i++)
            {
                var r = boxInput[i];
                boxes.Add(r.Width * r.Height, i);
            }
            var redCells = new List<rcData> { new rcData() };
            dst1.SetTo(0);
            dst2.SetTo(0);
            var lastMap = cellMap.Clone();
            cellMap.SetTo(0);
            int matchCount = 0;
            for (int i = 0; i < floodPoints.Count(); i++)
            {
                var rc = new rcData
                {
                    index = redCells.Count(),
                    floodPoint = floodPoints[i]
                };
                var val = dst3.Get<byte>(rc.floodPoint.Y, rc.floodPoint.X);
                rc.rect = boxInput[boxes.ElementAt(i).Value];
                rc.mask = dst3[rc.rect].InRange(val, val);
                dst1[rc.rect].SetTo(rc.index, rc.mask);
                rc.pixels = detect.maskCounts[i];
                rc.maxDist = GetMaxDist(ref rc);
                rc.indexLast = lastMap.Get<byte>(rc.maxDist.Y, rc.maxDist.X);
                cv.Scalar mean, stdev;
                Cv2.MeanStdDev(task.color[rc.rect], out mean, out stdev, rc.mask);
                rc.colorMean = mean;
                rc.colorStdev = stdev;
                rc.color = new Vec3b((byte)rc.colorMean[0], (byte)rc.colorMean[1], (byte)rc.colorMean[2]);
                if (rc.indexLast != 0) matchCount++;
                redCells.Add(rc);
                cellMap[rc.rect].SetTo(rc.index, rc.mask);
                dst2[rc.rect].SetTo(rc.color, rc.mask);
            }
            if (task.heartBeat) labels[2] = detect.labels[2] + " and " + matchCount + " were matched to the previous frame";
        }
    }
    public class CS_MSER_CPP : CS_Parent
    {
        Options_MSER options = new Options_MSER();
        public List<cv.Rect> boxes = new List<cv.Rect>();
        public List<Point> floodPoints = new List<Point>();
        public List<int> maskCounts = new List<int>();
        public int classcount;
        public CS_MSER_CPP(VBtask task) : base(task)
        {
            FindCheckBox("Use grayscale input").Checked = false;
            options.RunVB();
            cPtr = MSER_Open(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                             options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize, 
                             (int)options.pass2Setting);
            desc = "C++ version of MSER basics.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.optionsChanged)
            {
                MSER_Close(cPtr);
                cPtr = MSER_Open(options.delta, options.minArea, options.maxArea, options.maxVariation, options.minDiversity,
                                 options.maxEvolution, options.areaThreshold, options.minMargin, options.edgeBlurSize, 
                                 options.pass2Setting);
            }
            if (options.graySetting && src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            byte[] cppData = new byte[src.Total() * src.ElemSize()];
            Marshal.Copy(src.Data, cppData, 0, cppData.Length);
            var handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned);
            var imagePtr = MSER_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels());
            handleSrc.Free();
            dst0 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr).Clone();
            classcount = MSER_Count(cPtr);
            if (classcount == 0) return;
            var ptData = new Mat(classcount, 1, MatType.CV_32SC2, MSER_FloodPoints(cPtr));
            var maskData = new Mat(classcount, 1, MatType.CV_32S, MSER_MaskCounts(cPtr));
            var rectData = new Mat(classcount, 1, MatType.CV_32SC4, MSER_Rects(cPtr));
            var sortedBoxes = new SortedList<int, int>(new compareAllowIdenticalIntegerInverted());
            var rects = new List<cv.Rect>();
            for (int i = 0; i < classcount; i++)
            {
                var r = rectData.Get<cv.Rect>(i, 0);
                if (rects.Contains(r)) continue;
                rects.Add(r);
                sortedBoxes.Add(r.Width * r.Height, i);
            }
            boxes.Clear();
            floodPoints.Clear();
            maskCounts.Clear();
            for (int i = 0; i < sortedBoxes.Count(); i++)
            {
                var index = sortedBoxes.ElementAt(i).Value;
                boxes.Add(rectData.Get<cv.Rect>(index, 0));
                floodPoints.Add(ptData.Get<Point>(index, 0));
                maskCounts.Add(maskData.Get<int>(index, 0));
            }
            dst2 = ShowPalette(dst0 * 255 / classcount);
            if (standaloneTest())
            {
                dst3 = src;
                for (int i = 0; i < boxes.Count(); i++)
                {
                    dst3.Rectangle(boxes[i], task.HighlightColor, task.lineWidth);
                    if (i < task.redOptions.identifyCount) SetTrueText((i + 1).ToString(), boxes[i].TopLeft, 3);
                }
            }
            labels[2] = classcount + " regions identified";
        }
        public void Close()
        {
            MSER_Close(cPtr);
        }
    }
    public class CS_MultiDimensionScaling_Cities : CS_Parent
    {
        double[] CityDistance = { // 10x10 array of distances for 10 cities
        0, 587, 1212, 701, 1936, 604, 748, 2139, 2182, 543,       // Atlanta
        587, 0, 920, 940, 1745, 1188, 713, 1858, 1737, 597,       // Chicago
        1212, 920, 0, 879, 831, 1726, 1631, 949, 1021, 1494,      // Denver
        701, 940, 879, 0, 1734, 968, 1420, 1645, 1891, 1220,      // Houston
        1936, 1745, 831, 1734, 0, 2339, 2451, 347, 959, 2300,     // Los Angeles
        604, 1188, 1726, 968, 2339, 0, 1092, 2594, 2734, 923,     // Miami
        748, 713, 1631, 1420, 2451, 1092, 0, 2571, 2408, 205,     // New York
        2139, 1858, 949, 1645, 347, 2594, 2571, 0, 678, 2442,     // San Francisco
        2182, 1737, 1021, 1891, 959, 2734, 2408, 678, 0, 2329,    // Seattle
        543, 597, 1494, 1220, 2300, 923, 205, 2442, 2329, 0};      // Washington D.C.
        public CS_MultiDimensionScaling_Cities(VBtask task) : base(task)
        {
            labels[2] = "Resulting solution using cv.Eigen";
            desc = "Use OpenCV's Eigen function to solve a system of equations";
        }
        double Torgerson(Mat src)
        {
            int rows = src.Rows;
            mmData mm = GetMinMax(src);
            double c1 = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    for (int k = 0; k < rows; k++)
                    {
                        double v = src.At<double>(i, k) - src.At<double>(i, j) - src.At<double>(j, k);
                        if (v > c1) c1 = v;
                    }
                }
            }
            return Math.Max(Math.Max(c1, mm.maxVal), 0);
        }
        Mat CenteringMatrix(int n)
        {
            return Mat.Eye(n, n, MatType.CV_64F) - 1.0 / n;
        }
        public void RunCS(Mat src)
        {
            int size = 10; // we are working with 10 cities.
            Mat cityMat = new Mat(size, size, MatType.CV_64FC1, CityDistance);
            cityMat += Torgerson(cityMat);
            cityMat = cityMat.Mul(cityMat);
            Mat g = CenteringMatrix(size);
            // calculates the inner product matrix b
            Mat b = g * cityMat * g.Transpose() * -0.5;
            Mat vectors = new Mat(size, size, MatType.CV_64F);
            Mat values = new Mat(size, 1, MatType.CV_64F);
            Cv2.Eigen(b, values, vectors);
            values.Threshold(0, 0, ThresholdTypes.Tozero);
            Mat result = vectors.RowRange(0, 2);
            var at = result.GetGenericIndexer<double>();
            for (int r = 0; r < result.Rows; r++)
            {
                for (int c = 0; c < result.Cols; c++)
                {
                    at[r, c] *= Math.Sqrt(values.At<double>(r));
                }
            }
            result.Normalize(0, 800, NormTypes.MinMax);
            at = result.GetGenericIndexer<double>();
            double maxX = 0, maxY = 0, minX = double.MaxValue, minY = double.MaxValue;
            for (int c = 0; c < size; c++)
            {
                double x = -at[0, c];
                double y = at[1, c];
                if (maxX < x) maxX = x;
                if (maxY < y) maxY = y;
                if (minX > x) minX = x;
                if (minY > y) minY = y;
            }
            int w = dst2.Width;
            int h = dst2.Height;
            dst2.SetTo(0);
            string cityName = "Atlanta";
            for (int c = 0; c < size; c++)
            {
                double x = -at[0, c];
                double y = at[1, c];
                x = w * 0.1 + 0.7 * w * (x - minX) / (maxX - minX);
                y = h * 0.1 + 0.7 * h * (y - minY) / (maxY - minY);
                DrawCircle(dst2, new cv.Point(x, y), task.DotSize + 3, Scalar.Red);
                cv.Point textPos = new cv.Point(x + 5, y + 10);
                if (c == 1) cityName = "Chicago";
                if (c == 2) cityName = "Denver";
                if (c == 3) cityName = "Houston";
                if (c == 4) cityName = "Los Angeles";
                if (c == 5) cityName = "Miami";
                if (c == 6) cityName = "New York";
                if (c == 7) cityName = "San Francisco";
                if (c == 8) cityName = "Seattle";
                if (c == 9) cityName = "Washington D.C.";
                SetTrueText(cityName, textPos, 2);
            }
        }
    }
    public class CS_Neighbors_Basics : CS_Parent
    {
        public RedCloud_Basics redC = new RedCloud_Basics();
        KNN_Core knn = new KNN_Core();
        public bool runRedCloud = false;
        public Options_XNeighbors options = new Options_XNeighbors();
        public CS_Neighbors_Basics(VBtask task) : base(task)
        {
            desc = "Find all the neighbors with KNN";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (standalone || runRedCloud)
            {
                redC.Run(src);
                dst2 = redC.dst2;
                labels = redC.labels;
            }
            knn.queries.Clear();
            foreach (var rc in task.redCells)
            {
                knn.queries.Add(rc.maxDStable);
            }
            knn.trainInput = new List<Point2f>(knn.queries);
            knn.Run(src);
            for (int i = 0; i < task.redCells.Count(); i++)
            {
                var rc = task.redCells[i];
                rc.nabs = knn.neighbors[i];
            }
            if (standalone)
            {
                task.setSelectedContour();
                dst3.SetTo(0);
                int ptCount = 0;
                foreach (var index in task.rc.nabs)
                {
                    var pt = task.redCells[index].maxDStable;
                    if (pt == task.rc.maxDStable)
                    {
                        DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Black);
                    }
                    else
                    {
                        DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
                        ptCount++;
                        if (ptCount > options.xNeighbors) break;
                    }
                }
            }
        }
    }
    public class CS_Neighbors_Intersects : CS_Parent
    {
        public List<Point> nPoints = new List<Point>();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Neighbors_Intersects(VBtask task) : base(task)
        {
            desc = "Find the corner points where multiple cells intersect.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest() || src.Type() != MatType.CV_8U)
            {
                redC.Run(src);
                dst2 = redC.dst2;
                src = task.cellMap;
                labels[2] = redC.labels[2];
            }
            byte[] samples = new byte[src.Total()];
            Marshal.Copy(src.Data, samples, 0, samples.Length);
            int w = dst2.Width;
            nPoints.Clear();
            int kSize = 2;
            for (int y = 0; y <= dst1.Height - kSize; y++)
            {
                for (int x = 0; x <= dst1.Width - kSize; x++)
                {
                    var nabs = new SortedList<byte, byte>();
                    for (int yy = y; yy < y + kSize; yy++)
                    {
                        for (int xx = x; xx < x + kSize; xx++)
                        {
                            byte val = samples[yy * w + xx];
                            if (val == 0 && removeZeroNeighbors) continue;
                            if (!nabs.ContainsKey(val)) nabs.Add(val, 0);
                        }
                    }
                    if (nabs.Count() > 2)
                    {
                        nPoints.Add(new cv.Point(x, y));
                    }
                }
            }
            if (standaloneTest())
            {
                dst3 = task.color.Clone();
                foreach (var pt in nPoints)
                {
                    DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
                    DrawCircle(dst3, pt, task.DotSize, Scalar.Yellow);
                }
            }
            labels[3] = nPoints.Count().ToString() + " intersections with 3 or more cells were found";
        }
    }
    public class CS_Neighbors_ColorOnly : CS_Parent
    {
        Neighbors_Intersects corners = new Neighbors_Intersects();
        RedCloud_Cells redC = new RedCloud_Cells();
        public CS_Neighbors_ColorOnly(VBtask task) : base(task)
        {
            desc = "Find neighbors in a color only RedCloud cellMap";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            corners.Run(task.cellMap.Clone());
            foreach (var pt in corners.nPoints)
            {
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
            }
            labels[2] = redC.labels[2] + " and " + corners.nPoints.Count().ToString() + " cell intersections";
        }
    }
    public class CS_Neighbors_Precise : CS_Parent
    {
        public List<List<int>> nabList = new List<List<int>>();
        Cell_Basics stats = new Cell_Basics();
        public List<rcData> redCells;
        public bool runRedCloud = false;
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Neighbors_Precise(VBtask task) : base(task)
        {
            cPtr = Neighbors_Open();
            if (standaloneTest()) task.gOptions.setDisplay1();
            desc = "Find the neighbors in a selected RedCloud cell";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest() || runRedCloud)
            {
                redC.Run(src);
                dst2 = redC.dst2;
                labels = redC.labels;
                src = task.cellMap;
                redCells = task.redCells;
            }
            byte[] mapData = new byte[src.Total()];
            Marshal.Copy(src.Data, mapData, 0, mapData.Length);
            GCHandle handleSrc = GCHandle.Alloc(mapData, GCHandleType.Pinned);
            int nabCount = Neighbors_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols);
            handleSrc.Free();
            SetTrueText("Review the neighbors_Precise algorithm");
            //if (nabCount > 0)
            //{
            //    var nabData = new Mat(nabCount, 1, MatType.CV_32SC2, Neighbors_NabList(cPtr));
            //    nabList.Clear();
            //    for (int i = 0; i < redCells.Count(); i++)
            //    {
            //        nabList.Add(new List<int>());
            //    }
            //    redCells[i].nab = nabList.Min();
            //    for (int i = 0; i < nabCount; i++)
            //    {
            //        var pt = nabData.Get<Point>(i, 0);
            //        if (!nabList[pt.X].Contains(pt.Y) && pt.Y != 0)
            //        {
            //            nabList[pt.X].Add(pt.Y);
            //            redCells[pt.X].nabs.Add(pt.Y);
            //        }
            //        if (!nabList[pt.Y].Contains(pt.X) && pt.X != 0)
            //        {
            //            nabList[pt.Y].Add(pt.X);
            //            redCells[pt.Y].nabs.Add(pt.X);
            //        }
            //    }
            //    nabList[0].Clear(); // neighbors to zero are not interesting (yet?)
            //    redCells[0].nabs.Clear(); // not interesting.
            //    if (task.heartBeat && standaloneTest())
            //    {
            //        stats.Run(task.color);
            //        strOut = stats.strOut;
            //        if (nabList[task.rc.index].Count() > 0)
            //        {
            //            strOut += "Neighbors: ";
            //            dst1.SetTo(0);
            //            dst1[task.rc.rect].SetTo(task.rc.color, task.rc.mask);
            //            foreach (var index in nabList[task.rc.index])
            //            {
            //                var rc = redCells[index];
            //                dst1[rc.rect].SetTo(rc.color, rc.mask);
            //                strOut += index.ToString() + ",";
            //            }
            //            strOut += "\n";
            //        }
            //    }
            //    SetTrueText(strOut, 3);
            //}
            labels[3] = nabCount.ToString() + " neighbor pairs were found.";
        }
        public void Close()
        {
            Neighbors_Close(cPtr);
        }
    }
    public class CS_OEX_CalcBackProject_Demo1 : CS_Parent
    {
        public Mat histogram = new Mat();
        public int classCount;
        public CS_OEX_CalcBackProject_Demo1(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "BackProjection of Hue channel", "Plot of Hue histogram" };
            UpdateAdvice(traceName + ": <place advice here on any options that are useful>");
            desc = "OpenCV Sample CalcBackProject_Demo1";
        }
        public void RunCS(Mat src)
        {
            Rangef[] ranges = new Rangef[] { new Rangef(0, 180) };
            Mat hsv = task.color.CvtColor(ColorConversionCodes.BGR2HSV);
            Cv2.CalcHist(new Mat[] { hsv }, new int[] { 0 }, new Mat(), histogram, 1, new int[] { task.histogramBins }, ranges);
            classCount = Cv2.CountNonZero(histogram);
            dst0 = histogram.Normalize(0, classCount, NormTypes.MinMax); // for the backprojection.
            float[] histArray = new float[histogram.Total()];
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length);
            float peakValue = histArray.ToList().Max();
            histogram = histogram.Normalize(0, 1, NormTypes.MinMax);
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length);
            Cv2.CalcBackProject(new Mat[] { hsv }, new int[] { 0 }, dst0, dst2, ranges);
            dst3.SetTo(Scalar.Red);
            int binW = dst2.Width / task.histogramBins;
            int bins = dst2.Width / binW;
            for (int i = 0; i < bins; i++)
            {
                int h = (int)(dst2.Height * histArray[i]);
                cv.Rect r = new cv.Rect(i * binW, dst2.Height - h, binW, h);
                dst3.Rectangle(r, Scalar.Black, -1);
            }
            if (task.heartBeat) labels[3] = $"The max value below is {peakValue}";
        }
    }
    public class CS_OEX_CalcBackProject_Demo2 : CS_Parent
    {
        public Mat histogram = new Mat();
        public int classCount = 10; // initial value is just a guess.  It is refined after the first pass.
        public CS_OEX_CalcBackProject_Demo2(VBtask task) : base(task)
        {
            if (standalone) task.gOptions.setDisplay1();
            task.gOptions.setHistogramBins(6);
            labels = new string[] { "", "Mask for isolated region", "Backprojection of the hsv 2D histogram", "Mask in image context" };
            desc = "OpenCV Sample CalcBackProject_Demo2";
        }
        public void RunCS(Mat src)
        {
            int count = 0;
            if (task.ClickPoint != new cv.Point())
            {
                int connectivity = 8;
                int flags = connectivity | (255 << 8) | (int)FloodFillFlags.FixedRange | (int)FloodFillFlags.MaskOnly;
                Mat mask2 = new Mat(src.Rows + 2, src.Cols + 2, MatType.CV_8U, 0);
                // the delta between each regions value is 255 / classcount. no low or high bound needed.
                int delta = (int)(255 / classCount) - 1;
                Scalar bounds = new Scalar(delta, delta, delta);
                count = Cv2.FloodFill(dst2, mask2, task.ClickPoint, 255, out _, bounds, bounds, (cv.FloodFillFlags)flags);
                if (count != src.Total()) dst1 = mask2[new Range(1, mask2.Rows - 1), new Range(1, mask2.Cols - 1)];
            }
            Rangef[] ranges = new Rangef[] { new Rangef(0, 180), new Rangef(0, 256) };
            Mat hsv = task.color.CvtColor(ColorConversionCodes.BGR2HSV);
            Cv2.CalcHist(new Mat[] { hsv }, new int[] { 0, 1 }, new Mat(), histogram, 2, new int[] { task.histogramBins, task.histogramBins }, ranges);
            classCount = Cv2.CountNonZero(histogram);
            histogram = histogram.Normalize(0, 255, NormTypes.MinMax);
            Cv2.CalcBackProject(new Mat[] { hsv }, new int[] { 0, 1 }, histogram, dst2, ranges);
            dst3 = src;
            dst3.SetTo(Scalar.White, dst1);
            SetTrueText("Click anywhere to isolate that region.", 1);
        }
    }
    public class CS_OEX_bgfg_segm : CS_Parent
    {
        BGSubtract_Basics bgSub = new BGSubtract_Basics();
        public CS_OEX_bgfg_segm(VBtask task) : base(task)
        {
            desc = "OpenCV example bgfg_segm - existing BGSubtract_Basics is the same.";
        }
        public void RunCS(Mat src)
        {
            bgSub.Run(src);
            dst2 = bgSub.dst2;
            labels[2] = bgSub.labels[2];
        }
    }
    public class CS_OEX_bgSub : CS_Parent
    {
        BackgroundSubtractor pBackSub;
        Options_BGSubtract options = new Options_BGSubtract();
        public CS_OEX_bgSub(VBtask task) : base(task)
        {
            desc = "OpenCV example bgSub";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.optionsChanged)
            {
                switch (options.methodDesc)
                {
                    case "GMG":
                        pBackSub = BackgroundSubtractorGMG.Create();
                        break;
                    case "KNN":
                        pBackSub = BackgroundSubtractorKNN.Create();
                        break;
                    case "MOG":
                        pBackSub = BackgroundSubtractorMOG.Create();
                        break;
                    default: // MOG2 is the default.  Other choices map to MOG2 because OpenCVSharp doesn't support them.
                        pBackSub = BackgroundSubtractorMOG2.Create();
                        break;
                }
            }
            pBackSub.Apply(src, dst2, options.learnRate);
        }
    }
    public class CS_OEX_BasicLinearTransforms : CS_Parent
    {
        Options_BrightnessContrast options = new Options_BrightnessContrast();
        public CS_OEX_BasicLinearTransforms(VBtask task) : base(task)
        {
            desc = "OpenCV Example BasicLinearTransforms - NOTE: much faster than BasicLinearTransformTrackBar";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            src.ConvertTo(dst2, -1, options.brightness, options.contrast);
        }
    }
    public class CS_OEX_BasicLinearTransformsTrackBar : CS_Parent
    {
        Options_BrightnessContrast options = new Options_BrightnessContrast();
        public CS_OEX_BasicLinearTransformsTrackBar(VBtask task) : base(task)
        {
            desc = "OpenCV Example BasicLinearTransformTrackBar - much slower than OEX_BasicLinearTransforms";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (src.Cols >= 640)
            {
                src = src.Resize(task.lowRes);
                dst2 = dst2.Resize(task.lowRes);
            }
            for (int y = 0; y < src.Rows; y++)
            {
                for (int x = 0; x < src.Cols; x++)
                {
                    Vec3b vec = src.Get<Vec3b>(y, x);
                    vec[0] = (byte)Math.Max(Math.Min(vec[0] * options.brightness + options.contrast, 255), 0);
                    vec[1] = (byte)Math.Max(Math.Min(vec[1] * options.brightness + options.contrast, 255), 0);
                    vec[2] = (byte)Math.Max(Math.Min(vec[2] * options.brightness + options.contrast, 255), 0);
                    dst2.Set<Vec3b>(y, x, vec);
                }
            }
        }
    }
    public class CS_OEX_delaunay2 : CS_Parent
    {
        Scalar active_facet_color = new Scalar(0, 0, 255);
        Scalar delaunay_color = new Scalar(255, 255, 255);
        List<Point2f> points = new List<Point2f>();
        Subdiv2D subdiv;
        public CS_OEX_delaunay2(VBtask task) : base(task)
       {
            subdiv = new Subdiv2D(new cv.Rect(0, 0, dst2.Width, dst2.Height));
            if (standalone) task.gOptions.setDisplay1();
            labels = new string[] { "", "", "Next triangle list being built.  Latest entry is in red.", "The completed voronoi facets" };
            desc = "OpenCV Example delaunay2";
        }
        public void locatePoint(Mat img, Subdiv2D subdiv, cv.Point pt, Scalar activeColor)
        {
            int e0 = 0;
            int vertex = 0;
            subdiv.Locate(pt, out e0, out vertex);
            if (e0 > 0)
            {
                int e = e0;
                do
                {
                    cv.Point2f org, dst;
                    if (subdiv.EdgeOrg(e, out org) > 0 && subdiv.EdgeDst(e, out dst) > 0)
                    {
                        DrawLine(img, org, dst, activeColor, task.lineWidth + 3);
                    }
                    e = subdiv.GetEdge(e, (cv.NextEdgeType)Subdiv2D.NEXT_AROUND_LEFT);
                } while (e != e0);
            }
            DrawCircle(img, pt, task.DotSize, activeColor);
        }
        public void RunCS(Mat src)
        {
            if (task.quarterBeat)
            {
                if (points.Count() < 10)
                {
                    dst2.SetTo(0);
                    Point2f pt = new Point2f(msRNG.Next(0, dst2.Width - 10) + 5, msRNG.Next(0, dst2.Height - 10) + 5);
                    points.Add(pt);
                    locatePoint(dst2, subdiv, new cv.Point((int)pt.X, (int)pt.Y), active_facet_color);
                    subdiv.Insert(pt);
                    var triangleList = subdiv.GetTriangleList();
                    Point[] pts = new Point[3];
                    foreach (var tri in triangleList)
                    {
                        pts[0] = new cv.Point(Math.Round(tri[0]), Math.Round(tri[1]));
                        pts[1] = new cv.Point(Math.Round(tri[2]), Math.Round(tri[3]));
                        pts[2] = new cv.Point(Math.Round(tri[4]), Math.Round(tri[5]));
                        DrawLine(dst2, pts[0], pts[1], delaunay_color);
                        DrawLine(dst2, pts[1], pts[2], delaunay_color);
                        DrawLine(dst2, pts[2], pts[0], delaunay_color);
                    }
                }
                else
                {
                    dst1 = dst2.Clone();
                    Point2f[][] facets = new Point2f[1][];
                    Point2f[] centers;
                    subdiv.GetVoronoiFacetList(new List<int>(), out facets, out centers);
                    List<Point> ifacet = new List<Point>();
                    List<List<Point>> ifacets = new List<List<Point>> { ifacet };
                    for (int i = 0; i < facets.Length; i++)
                    {
                        ifacet.Clear();
                        ifacet.AddRange(facets[i].Select(p => new cv.Point(p.X, p.Y)));
                        Scalar color = vecToScalar(task.vecColors[i % 256]);
                        dst3.FillConvexPoly(ifacet, color, cv.LineTypes.Link8, 0);
                        ifacets[0] = ifacet;
                        Cv2.Polylines(dst3, ifacets, true, new cv.Scalar(), task.lineWidth, task.lineType);
                        DrawCircle(dst3, centers[i], 3, new cv.Scalar());
                    }
                    points.Clear();
                    subdiv = new Subdiv2D(new cv.Rect(0, 0, dst2.Width, dst2.Height));
                }
            }
        }
    }
    public class CS_OEX_MeanShift : CS_Parent
    {
        TermCriteria term_crit = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.Count, 10, 1.0);
        Rangef[] ranges = new Rangef[] { new Rangef(0, 180) };
        public Mat histogram = new Mat();
        cv.Rect trackWindow;
        public CS_OEX_MeanShift(VBtask task) : base(task)
        {
            labels[3] = "Draw a rectangle around the region of interest";
            desc = "OpenCV Example MeanShift";
        }
        public void RunCS(Mat src)
        {
            cv.Rect roi = task.drawRect.Width > 0 ? task.drawRect : new cv.Rect(0, 0, dst2.Width, dst2.Height);
            Mat hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            dst2 = src;
            if (task.optionsChanged)
            {
                trackWindow = roi;
                Mat mask = new Mat();
                Cv2.InRange(hsv, new Scalar(0, 60, 32), new Scalar(180, 255, 255), mask);
                Cv2.CalcHist(new Mat[] { hsv[roi] }, new int[] { 0 }, new Mat(), histogram, 1, new int[] { task.histogramBins }, ranges);
                histogram = histogram.Normalize(0, 255, NormTypes.MinMax);
            }
            Cv2.CalcBackProject(new Mat[] { hsv }, new int[] { 0 }, histogram, dst3, ranges);
            if (trackWindow.Width != 0)
            {
                Cv2.MeanShift(dst3, ref trackWindow, TermCriteria.Both(10, 1));
                src.Rectangle(trackWindow, Scalar.White, task.lineWidth, task.lineType);
            }
        }
    }
    public class CS_OEX_PointPolygon : CS_Parent
    {
        Rectangle_Rotated rotatedRect = new Rectangle_Rotated();
        public CS_OEX_PointPolygon(VBtask task) : base(task)
        {
            desc = "PointPolygonTest will decide what is inside and what is outside.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                rotatedRect.Run(src);
                src = rotatedRect.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            }
            dst2 = src.Clone();
            Point[][] contours;
            Cv2.FindContours(src, out contours, out _, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
            dst1 = new Mat(dst1.Size(), MatType.CV_32F, 0);
            for (int i = 0; i < dst1.Rows; i++)
            {
                for (int j = 0; j < dst1.Cols; j++)
                {
                    double distance = Cv2.PointPolygonTest(contours[0], new cv.Point(j, i), true);
                    dst1.Set(i, j, distance);
                }
            }
            var mm = GetMinMax(dst1);
            mm.minVal = Math.Abs(mm.minVal);
            mm.maxVal = Math.Abs(mm.maxVal);
            Vec3b blue = new Vec3b(0, 0, 0);
            Vec3b red = new Vec3b(0, 0, 0);
            for (int i = 0; i < src.Rows; i++)
            {
                for (int j = 0; j < src.Cols; j++)
                {
                    float val = dst1.Get<float>(i, j);
                    if (val < 0)
                    {
                        blue[0] = (byte)(255 - Math.Abs(val) * 255 / mm.minVal);
                        dst3.Set(i, j, blue);
                    }
                    else if (val > 0)
                    {
                        red[2] = (byte)(255 - val * 255 / mm.maxVal);
                        dst3.Set(i, j, red);
                    }
                    else
                    {
                        dst3.Set(i, j, white);
                    }
                }
            }
        }
    }
    public class CS_OEX_PointPolygon_demo : CS_Parent
    {
        OEX_PointPolygon pointPoly = new OEX_PointPolygon();
        public CS_OEX_PointPolygon_demo(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "OpenCV Example PointPolygonTest_demo - it became PointPolygonTest_Basics.";
        }
        public void RunCS(Mat src)
        {
            int r = dst2.Height / 4;
            List<Point> vert = new List<Point>
        {
            new cv.Point(3 * r / 2 + dst2.Width / 4, (int)(1.34 * r)),
            new cv.Point(r + dst2.Width / 4, 2 * r),
            new cv.Point(3 * r / 2 + dst2.Width / 4, (int)(2.866 * r)),
            new cv.Point(5 * r / 2 + dst2.Width / 4, (int)(2.866 * r)),
            new cv.Point(3 * r + dst2.Width / 4, 2 * r),
            new cv.Point(5 * r / 2 + dst2.Width / 4, (int)(1.34 * r))
        };
            dst2.SetTo(0);
            for (int i = 0; i < vert.Count(); i++)
            {
                DrawLine(dst2, vert[i], vert[(i + 1) % 6], Scalar.White);
            }
            pointPoly.Run(dst2);
            dst3 = pointPoly.dst3;
        }
    }
    public class CS_OEX_Remap : CS_Parent
    {
        Remap_Basics remap = new Remap_Basics();
        public CS_OEX_Remap(VBtask task) : base(task)
        {
            desc = "The OpenCV Remap example became the Remap_Basics algorithm.";
        }
        public void RunCS(Mat src)
        {
            remap.Run(src);
            dst2 = remap.dst2;
            labels[2] = remap.labels[2];
        }
    }
    public class CS_OEX_Threshold : CS_Parent
    {
        Threshold_Basics threshold = new Threshold_Basics();
        public CS_OEX_Threshold(VBtask task) : base(task)
        {
            desc = "OpenCV Example Threshold became Threshold_Basics";
        }
        public void RunCS(Mat src)
        {
            threshold.Run(src);
            dst2 = threshold.dst2;
            dst3 = threshold.dst3;
            labels = threshold.labels;
        }
    }
    public class CS_OEX_Threshold_Inrange : CS_Parent
    {
        Options_OEX options = new Options_OEX();
        public CS_OEX_Threshold_Inrange(VBtask task) : base(task)
        {
            desc = "OpenCV Example Threshold_Inrange";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            dst2 = hsv.InRange(options.lows, options.highs);
        }
    }
    public class CS_OEX_Points_Classifier : CS_Parent
    {
        Classifier_Basics basics = new Classifier_Basics();
        public CS_OEX_Points_Classifier(VBtask task) : base(task)
        {
            desc = "OpenCV Example Points_Classifier became Classifier_Basics";
        }
        public void RunCS(Mat src)
        {
            basics.Run(src);
            dst2 = basics.dst2;
            dst3 = basics.dst3;
            labels = basics.labels;
            SetTrueText("Click the global DebugCheckBox to get another set of points.", 2);
        }
    }
    public class CS_OEX_GoodFeaturesToTrackDemo : CS_Parent
    {
        Feature_Basics feat = new Feature_Basics();
        public CS_OEX_GoodFeaturesToTrackDemo(VBtask task) : base(task)
        {
            desc = "OpenCV Example GoodFeaturesToTrackDemo - now Feature_Basics";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            dst2 = feat.dst2;
            labels[2] = feat.labels[2];
        }
    }
    public class CS_OEX_Core_Reduce : CS_Parent
    {
        public CS_OEX_Core_Reduce(VBtask task) : base(task)
        {
            desc = "Use OpenCV's reduce API to create row/col sums, averages, and min/max.";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                Mat m = new Mat(3, 2, MatType.CV_32F, new float[] { 1, 2, 3, 4, 5, 6 });
                Mat col_sum = new Mat(), row_sum = new Mat();
                Cv2.Reduce(m, col_sum, 0, ReduceTypes.Sum, MatType.CV_32F);
                Cv2.Reduce(m, row_sum, (cv.ReduceDimension) 1, ReduceTypes.Sum, MatType.CV_32F);
                strOut = "Original Mat" + "\n";
                for (int y = 0; y < m.Rows; y++)
                {
                    for (int x = 0; x < m.Cols; x++)
                    {
                        strOut += m.Get<float>(y, x) + ", ";
                    }
                    strOut += "\n";
                }
                strOut += "\n" + "col_sum" + "\n";
                for (int i = 0; i < m.Cols; i++)
                {
                    strOut += col_sum.Get<float>(0, i) + ", ";
                }
                strOut += "\n" + "row_sum" + "\n";
                for (int i = 0; i < m.Rows; i++)
                {
                    strOut += row_sum.Get<float>(0, i) + ", ";
                }
                Mat col_average = new Mat(), row_average = new Mat(), col_min = new Mat();
                Mat col_max = new Mat(), row_min = new Mat(), row_max = new Mat();
                Cv2.Reduce(m, col_average, 0, ReduceTypes.Avg, MatType.CV_32F);
                Cv2.Reduce(m, row_average, (cv.ReduceDimension) 1, ReduceTypes.Avg, MatType.CV_32F);
                Cv2.Reduce(m, col_min, 0, ReduceTypes.Min, MatType.CV_32F);
                Cv2.Reduce(m, row_min, (cv.ReduceDimension)1, ReduceTypes.Min, MatType.CV_32F);
                Cv2.Reduce(m, col_max, 0, ReduceTypes.Max, MatType.CV_32F);
                Cv2.Reduce(m, row_max, (cv.ReduceDimension)1, ReduceTypes.Max, MatType.CV_32F);
            }
            SetTrueText(strOut, 2);
        }
    }
    public class CS_OEX_Core_Split : CS_Parent
    {
        public CS_OEX_Core_Split(VBtask task) : base(task)
        {
            desc = "OpenCV Example Core_Split";
        }
        public void RunCS(Mat src)
        {
            var d = new Mat(2, 2, MatType.CV_8UC3, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
            var channels = d.Split();
            var samples = new byte[d.Total() * d.ElemSize()];
            Marshal.Copy(d.Data, samples, 0, samples.Length);
            strOut = "Original 2x2 Mat";
            for (int i = 0; i < samples.Length; i++)
            {
                strOut += samples[i].ToString() + ", ";
            }
            strOut += "\n";
            for (int i = 0; i < 3; i++)
            {
                strOut += "Channels " + i.ToString() + "\n";
                for (int y = 0; y < channels[i].Rows; y++)
                {
                    for (int x = 0; x < channels[i].Cols; x++)
                    {
                        strOut += channels[i].Get<byte>(y, x).ToString() + ", ";
                    }
                    strOut += "\n";
                }
            }
            SetTrueText(strOut, 2);
        }
    }
    public class CS_OEX_Filter2D : CS_Parent
    {
        MatType ddepth = MatType.CV_8UC3;
        cv.Point anchor = new cv.Point(-1, -1);
        int kernelSize = 3, ind = 0;
        public CS_OEX_Filter2D(VBtask task) : base(task)
        {
            desc = "OpenCV Example Filter2D demo - Use a varying kernel to show the impact.";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat) ind++;
            kernelSize = 3 + 2 * (ind % 5);
            var kernel = new Mat(kernelSize, kernelSize, MatType.CV_32F, 1.0 / (kernelSize * kernelSize));
            dst2 = src.Filter2D(ddepth, kernel, anchor, 0, BorderTypes.Default);
            SetTrueText("Kernel size = " + kernelSize.ToString(), 3);
        }
    }
    public class CS_OEX_FitEllipse : CS_Parent
    {
        Mat img;
        Options_FitEllipse options = new Options_FitEllipse();
        public CS_OEX_FitEllipse(VBtask task) : base(task)
        {
            var fileInputName = new FileInfo(task.HomeDir + "opencv/samples/data/ellipses.jpg");
            img = Cv2.ImRead(fileInputName.FullName).CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            cPtr = OEX_FitEllipse_Open();
            desc = "OEX Example fitellipse";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            var cppData = new byte[img.Total() * img.ElemSize()];
            Marshal.Copy(img.Data, cppData, 0, cppData.Length);
            var handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned);
            var imagePtr = OEX_FitEllipse_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), img.Rows, img.Cols,
                                                 options.threshold, options.fitType);
            handleSrc.Free();
            dst2 = new Mat(img.Rows + 4, img.Cols + 4, MatType.CV_8UC3, imagePtr).Clone();
        }
        public void Close()
        {
            OEX_FitEllipse_Close(cPtr);
        }
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr OEX_FitEllipse_Open();
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void OEX_FitEllipse_Close(IntPtr cPtr);
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr OEX_FitEllipse_RunCPP(IntPtr cPtr, IntPtr dataPtr, int rows, int cols,
                                                           int threshold, int fitType);
    }

}
