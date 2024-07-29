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
using static CS_Classes.CS_Externs;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Windows.Media.Animation;

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
            List<cv.Point> pointList = new List<cv.Point>();
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
                List<cv.Point> inputPoints = new List<cv.Point>();
                for (int i = 0; i < nonzeros.Rows; i++)
                {
                    cv.Point pt = nonzeros.Get<cv.Point>(i, 0);
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
        public List<cv.Point> floodPoints = new List<cv.Point>();
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
            floodPoints = new List<cv.Point>(detect.floodPoints);
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
        public cv.Point[][] regions;
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
            cv.Point[][] regions;
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
            var floodPoints = new List<cv.Point>(detect.floodPoints);
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
        public List<cv.Point> floodPoints = new List<cv.Point>();
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
                floodPoints.Add(ptData.Get<cv.Point>(index, 0));
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
        public List<cv.Point> nPoints = new List<cv.Point>();
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
                    cv.Point[] pts = new cv.Point[3];
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
                    List<cv.Point> ifacet = new List<cv.Point>();
                    List<List<cv.Point>> ifacets = new List<List<cv.Point>> { ifacet };
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
            cv.Point[][] contours;
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
            List<cv.Point> vert = new List<cv.Point>
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
    public class CS_OilPaint_Pointilism : CS_Parent
    {
        Mat randomMask;
        RNG myRNG = new RNG();
        Options_Pointilism options = new Options_Pointilism();
        cv.Rect saveDrawRect = new cv.Rect();
        public CS_OilPaint_Pointilism(VBtask task) : base(task)
        {
            task.drawRect = new cv.Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8);
            desc = "Alter the image to effect the pointilism style";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2 = src;
            var img = src[task.drawRect];
            if (saveDrawRect != task.drawRect)
            {
                saveDrawRect = task.drawRect;
                randomMask = new Mat(img.Size(), MatType.CV_32SC2);
                cv.Point nPt = new cv.Point();
                for (int y = 0; y < randomMask.Height; y++)
                {
                    for (int x = 0; x < randomMask.Width; x++)
                    {
                        nPt.X = (msRNG.Next(-1, 1) + x) % (randomMask.Width - 1);
                        nPt.Y = (msRNG.Next(-1, 1) + y) % (randomMask.Height - 1);
                        if (nPt.X < 0) nPt.X = 0;
                        if (nPt.Y < 0) nPt.Y = 0;
                        randomMask.Set<cv.Point>(y, x, nPt);
                    }
                }
                Cv2.RandShuffle(randomMask, 1.0, ref myRNG);
            }
            var rand = randomMask.Resize(img.Size());
            var gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat fieldx = new Mat(), fieldy = new Mat();
            Cv2.Scharr(gray, fieldx, MatType.CV_32FC1, 1, 0, 1 / 15.36);
            Cv2.Scharr(gray, fieldy, MatType.CV_32FC1, 0, 1, 1 / 15.36);
            Cv2.GaussianBlur(fieldx, fieldx, new cv.Size(options.smoothingRadius, options.smoothingRadius), 0, 0);
            Cv2.GaussianBlur(fieldy, fieldy, new cv.Size(options.smoothingRadius, options.smoothingRadius), 0, 0);
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var nPt = rand.Get<cv.Point>(y, x);
                    var nextColor = src.Get<Vec3b>(saveDrawRect.Y + nPt.Y, saveDrawRect.X + nPt.X);
                    var fx = fieldx[saveDrawRect].Get<float>(nPt.Y, nPt.X);
                    var fy = fieldy[saveDrawRect].Get<float>(nPt.Y, nPt.X);
                    var nPoint = new Point2f(nPt.X, nPt.Y);
                    var gradient_magnitude = Math.Sqrt(fx * fx + fy * fy);
                    var slen = Math.Round(options.strokeSize + options.strokeSize * Math.Sqrt(gradient_magnitude));
                    var eSize = new Size2f(slen, options.strokeSize);
                    var direction = Math.Atan2(fx, fy);
                    var angle = direction * 180.0 / Math.PI + 90;
                    var rotatedRect = new RotatedRect(nPoint, eSize, (float)angle);
                    if (options.useElliptical)
                    {
                        dst2[saveDrawRect].Ellipse(rotatedRect, vecToScalar(nextColor));
                    }
                    else
                    {
                        DrawCircle(dst2[saveDrawRect], nPoint, (int)(slen / 4), vecToScalar(nextColor));
                    }
                }
            }
        }
    }
    public class CS_OilPaint_ManualVB : CS_Parent
    {
        public Options_OilPaint options = new Options_OilPaint();
        public CS_OilPaint_ManualVB(VBtask task) : base(task)
        {
            task.drawRect = new cv.Rect(dst2.Cols * 3 / 8, dst2.Rows * 3 / 8, dst2.Cols * 2 / 8, dst2.Rows * 2 / 8);
            desc = "Alter an image so it appears more like an oil painting.  Select a region of interest.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            int filterKern = options.filterSize | 1;
            var roi = task.drawRect;
            src.CopyTo(dst2);
            var color = src[roi];
            var result1 = color.Clone();
            for (int y = filterKern; y < roi.Height - filterKern - 1; y++)
            {
                for (int x = filterKern; x < roi.Width - filterKern - 1; x++)
                {
                    int[] intensitybins = new int[options.intensity + 1];
                    int[] bluebin = new int[options.intensity + 1];
                    int[] greenbin = new int[options.intensity + 1];
                    int[] redbin = new int[options.intensity + 1];
                    int maxIntensity = 0;
                    int maxIndex = 0;
                    Vec3b vec = new Vec3b();
                    for (int yy = y - filterKern; yy <= y + filterKern - 1; yy++)
                    {
                        for (int xx = x - filterKern; xx <= x + filterKern - 1; xx++)
                        {
                            vec = color.Get<Vec3b>(yy, xx);
                            int currentIntensity = (int)(Math.Round((double)(vec[0] + vec[1] + vec[2]) * options.intensity / (255 * 3)));
                            intensitybins[currentIntensity] += 1;
                            bluebin[currentIntensity] += vec[0];
                            greenbin[currentIntensity] += vec[1];
                            redbin[currentIntensity] += vec[2];
                            if (intensitybins[currentIntensity] > maxIntensity)
                            {
                                maxIndex = currentIntensity;
                                maxIntensity = intensitybins[currentIntensity];
                            }
                        }
                    }
                    vec[0] = (byte)((bluebin[maxIndex] / maxIntensity) > 255 ? 255 : bluebin[maxIndex] / maxIntensity);
                    vec[1] = (byte)((greenbin[maxIndex] / maxIntensity) > 255 ? 255 : greenbin[maxIndex] / maxIntensity);
                    vec[2] = (byte)((redbin[maxIndex] / maxIntensity) > 255 ? 255 : redbin[maxIndex] / maxIntensity);
                    result1.Set<Vec3b>(y, x, vec);
                }
            }
            result1.CopyTo(dst2[roi]);
        }
    }
    public class CS_OpAuto_XRange : CS_Parent
    {
        public Mat histogram = new Mat();
        int adjustedCount = 0;
        public CS_OpAuto_XRange(VBtask task) : base(task)
        {
            labels[2] = "Optimized top view to show as many samples as possible.";
            desc = "Automatically adjust the X-Range option of the pointcloud to maximize visible pixels";
        }
        public void RunCS(Mat src)
        {
            int expectedCount = task.depthMask.CountNonZero();
            int diff = Math.Abs(expectedCount - adjustedCount);
            // the input is a histogram.  If standaloneTest(), go get one...
            if (standaloneTest())
            {
                Cv2.CalcHist(new Mat[] { task.pointCloud }, task.channelsTop, new Mat(), histogram, 2, task.bins2D, task.rangesTop);
                histogram.Row(0).SetTo(0);
                dst2 = histogram.Threshold(0, 255, ThresholdTypes.Binary).ConvertScaleAbs();
                dst3 = histogram.Threshold(task.projectionThreshold, 255, ThresholdTypes.Binary).ConvertScaleAbs();
                src = histogram;
            }
            histogram = src;
            adjustedCount = (int)histogram.Sum()[0];
            strOut = "Adjusted = " + "\t" + adjustedCount + "k" + "\n" +
                     "Expected = " + "\t" + expectedCount + "k" + "\n" +
                     "Diff = " + "\t\t" + diff + "\n" +
                     "xRange = " + "\t" + string.Format("{0:F3}", task.xRange);
            if (task.useXYRange)
            {
                bool saveOptionState = task.optionsChanged; // the xRange and yRange change frequently.  It is safe to ignore it.
                int leftGap = histogram.Col(0).CountNonZero();
                int rightGap = histogram.Col(histogram.Width - 1).CountNonZero();
                if (leftGap == 0 && rightGap == 0 && task.redOptions.getXRangeSlider() > 3)
                {
                    task.redOptions.setXRangeSlider(task.redOptions.getXRangeSlider() - 1);
                }
                else
                {
                    task.redOptions.setXRangeSlider (task.redOptions.getXRangeSlider() + (adjustedCount < expectedCount ? 1 : -1));
                }
                task.optionsChanged = saveOptionState;
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_OpAuto_YRange : CS_Parent
    {
        public Mat histogram = new Mat();
        int adjustedCount = 0;
        public CS_OpAuto_YRange(VBtask task) : base(task)
        {
            labels[2] = "Optimized side view to show as much as possible.";
            desc = "Automatically adjust the Y-Range option of the pointcloud to maximize visible pixels";
        }
        public void RunCS(Mat src)
        {
            int expectedCount = task.depthMask.CountNonZero();
            int diff = Math.Abs(expectedCount - adjustedCount);
            // the input is a histogram.  If standaloneTest(), go get one...
            if (standaloneTest())
            {
                Cv2.CalcHist(new Mat[] { task.pointCloud }, task.channelsSide, new Mat(), histogram, 2, task.bins2D, task.rangesSide);
                histogram.Col(0).SetTo(0);
                dst2 = histogram.Threshold(0, 255, ThresholdTypes.Binary).ConvertScaleAbs();
                dst3 = histogram.Threshold(task.projectionThreshold, 255, ThresholdTypes.Binary).ConvertScaleAbs();
                src = histogram;
            }
            histogram = src;
            adjustedCount = (int)histogram.Sum()[0];
            strOut = "Adjusted = " + "\t" + adjustedCount + "k" + "\n" +
                     "Expected = " + "\t" + expectedCount + "k" + "\n" +
                     "Diff = " + "\t\t" + diff + "\n" +
                     "yRange = " + "\t" + string.Format("{0:F3}", task.yRange);
            if (task.useXYRange)
            {
                bool saveOptionState = task.optionsChanged; // the xRange and yRange change frequently.  It is safe to ignore it.
                int topGap = histogram.Row(0).CountNonZero();
                int botGap = histogram.Row(histogram.Height - 1).CountNonZero();
                if (topGap == 0 && botGap == 0 && task.redOptions.getYRangeSlider() > 3)
                {
                    task.redOptions.setYRangeSlider(task.redOptions.getYRangeSlider() - 1);
                }
                else
                {
                    task.redOptions.setYRangeSlider(task.redOptions.getYRangeSlider() + ((adjustedCount < expectedCount) ? 1 : -1));
                }
                task.optionsChanged = saveOptionState;
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_OpAuto_FloorCeiling : CS_Parent
    {
        public BackProject_LineSide bpLine = new BackProject_LineSide();
        public List<float> yList = new List<float>();
        public float floorY;
        public float ceilingY;
        public CS_OpAuto_FloorCeiling(VBtask task) : base(task)
        {
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            desc = "Automatically find the Y values that best describes the floor and ceiling (if present)";
        }
        void rebuildMask(string maskLabel, float min, float max)
        {
            Mat mask = task.pcSplit[1].InRange(min, max).ConvertScaleAbs();
            Scalar mean, stdev;
            Cv2.MeanStdDev(task.pointCloud, out mean, out stdev, mask);
            strOut += "The " + maskLabel + " mask has Y mean and stdev are:" + "\n";
            strOut += maskLabel + " Y Mean = " + string.Format("{0:F3}", mean[1]) + "\n";
            strOut += maskLabel + " Y Stdev = " + string.Format("{0:F3}", stdev[1]) + "\n" + "\n";
            if (Math.Abs(mean[1]) > task.yRange / 4) dst1 = mask | dst1;
        }
        public void RunCS(Mat src)
        {
            float pad = 0.05f; // pad the estimate by X cm's
            dst2 = src.Clone();
            bpLine.Run(src);
            if (bpLine.lpList.Count() > 0)
            {
                strOut = "Y range = " + string.Format("{0:F3}", task.yRange) + "\n" + "\n";
                if (task.heartBeat) yList.Clear();
                if (task.heartBeat) dst1.SetTo(0);
                int h = dst2.Height / 2;
                foreach (var mp in bpLine.lpList)
                {
                    float nextY = task.yRange * (mp.p1.Y - h) / h;
                    if (Math.Abs(nextY) > task.yRange / 4) yList.Add(nextY);
                }
                if (yList.Count() > 0)
                {
                    if (yList.Max() > 0) rebuildMask("floor", yList.Max() - pad, task.yRange);
                    if (yList.Min() < 0) rebuildMask("ceiling", -task.yRange, yList.Min() + pad);
                }
                dst2.SetTo(Scalar.White, dst1);
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_OpAuto_Valley : CS_Parent
    {
        public SortedList<int, int> valleyOrder = new SortedList<int, int>(new CompareAllowIdenticalInteger());
        public Options_Boundary options = new Options_Boundary();
        Hist_Kalman kalmanHist = new Hist_Kalman();
        public CS_OpAuto_Valley(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setHistogramBins(256);
            desc = "Get the top X highest quality valley points in the histogram.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            int desiredBoundaries = options.desiredBoundaries;
            // input should be a histogram.  If not, get one...
            if (standaloneTest())
            {
                kalmanHist.Run(src);
                dst2 = kalmanHist.dst2;
                src = kalmanHist.hist.histogram.Clone();
            }
            float[] histArray = new float[src.Total()];
            Marshal.Copy(src.Data, histArray, 0, histArray.Length);
            List<float> histList = new List<float>(histArray);
            List<float> valleys = new List<float>();
            int incr = histList.Count() / desiredBoundaries;
            for (int i = 0; i < desiredBoundaries; i++)
            {
                List<float> nextList = new List<float>();
                for (int j = i * incr; j < (i + 1) * incr; j++)
                {
                    if (i == 0 && j < 5)
                    {
                        nextList.Add(dst2.Total()); // there are typically some gaps near zero.
                    }
                    else
                    {
                        nextList.Add(histList[j] == 0 ? dst2.Total() : histList[j]);
                    }
                }
                int index = nextList.IndexOf(nextList.Min());
                valleys.Add(index + i * incr);
            }
            valleyOrder.Clear();
            int lastEntry = 0;
            for (int i = 0; i < desiredBoundaries; i++)
            {
                valleyOrder.Add(lastEntry, (int)(valleys[i] - 1));
                lastEntry = (int)valleys[i];
            }
            if (valleys[desiredBoundaries - 1] != histList.Count() - 1)
            {
                valleyOrder.Add((int)valleys[desiredBoundaries - 1], 256);
            }
            if (standaloneTest())
            {
                foreach (var entry in valleyOrder)
                {
                    int col = entry.Value * dst2.Width / task.histogramBins;
                    DrawLine(dst2, new cv.Point(col, 0), new cv.Point(col, dst2.Height), Scalar.White);
                }
                SetTrueText(valleys.Count() + " valleys in histogram", 3);
            }
        }
    }
    public class CS_OpAuto_Peaks2D : CS_Parent
    {
        public Options_Boundary options = new Options_Boundary();
        public List<Point2f> clusterPoints = new List<Point2f>();
        HeatMap_Basics heatmap = new HeatMap_Basics();
        public CS_OpAuto_Peaks2D(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setHistogramBins(256);
            labels = new string[] { "", "", "2D Histogram view with highlighted peaks", "" };
            desc = "Find the peaks in a 2D histogram";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            int desiredBoundaries = options.desiredBoundaries;
            int peakDistance = options.peakDistance;
            // input should be a 2D histogram.  If standaloneTest(), get one...
            if (standaloneTest())
            {
                heatmap.Run(src);
                dst2 = task.toggleOnOff ? heatmap.dst2 : heatmap.dst3;
                src = task.toggleOnOff ? heatmap.dst0.Clone() : heatmap.dst1.Clone();
            }
            clusterPoints.Clear();
            clusterPoints.Add(new cv.Point(0, 0));
            for (int i = 0; i < desiredBoundaries; i++)
            {
                var mm = GetMinMax(src);
                if (!clusterPoints.Contains(mm.maxLoc)) clusterPoints.Add(mm.maxLoc);
                DrawCircle(src, mm.maxLoc, peakDistance, 0);
            }
            if (!standaloneTest()) dst2.SetTo(0);
            for (int i = 0; i < clusterPoints.Count(); i++)
            {
                cv.Point pt = new cv.Point(clusterPoints[i].X, clusterPoints[i].Y);
                DrawCircle(dst2, pt, task.DotSize * 3, Scalar.White);
            }
        }
    }
    public class CS_OpAuto_Peaks2DGrid : CS_Parent
    {
        public List<Point2f> clusterPoints = new List<Point2f>();
        Options_Boundary options = new Options_Boundary();
        Hist2D_Basics hist2d = new Hist2D_Basics();
        TrackBar boundarySlider;
        public CS_OpAuto_Peaks2DGrid(VBtask task) : base(task)
        {
            boundarySlider = FindSlider("Desired boundary count");
            if (standaloneTest()) task.gOptions.setHistogramBins(256);
            labels = new string[] { "", "", "2D Histogram view with highlighted peaks", "" };
            desc = "Find the peaks in a 2D histogram";
        }
        public void RunCS(Mat src)
        {
            var desiredBoundaries = boundarySlider.Value;
            // input should be a 2D histogram.  If standaloneTest() or src is not a histogram, get one...
            if (standaloneTest() || src.Type() == MatType.CV_8UC3)
            {
                hist2d.Run(src);
                src = hist2d.histogram;
                dst2.SetTo(0);
            }
            var pointPop = new SortedList<float, cv.Point>(new CompareAllowIdenticalSingleInverted());
            foreach (var roi in task.gridList)
            {
                var mm = GetMinMax(src[roi]);
                if (mm.maxVal == 0) continue;
                cv.Point wPt = new cv.Point(roi.X + mm.maxLoc.X, roi.Y + mm.maxLoc.Y);
                pointPop.Add((float)mm.maxVal, wPt);
            }
            clusterPoints.Clear();
            clusterPoints.Add(new cv.Point(0, 0));
            foreach (var entry in pointPop)
            {
                clusterPoints.Add(entry.Value);
                if (desiredBoundaries <= clusterPoints.Count()) break;
            }
            if (!standaloneTest()) dst2.SetTo(0);
            for (int i = 0; i < clusterPoints.Count(); i++)
            {
                var pt = clusterPoints[i];
                DrawCircle(dst2, pt, task.DotSize * 3, Scalar.White);
            }
            dst2.SetTo(Scalar.White, task.gridMask);
            labels[3] = pointPop.Count().ToString() + " grid samples trimmed to " + clusterPoints.Count().ToString();
        }
    }
    public class CS_OpAuto_PixelDifference : CS_Parent
    {
        Diff_Basics diff = new Diff_Basics();
        public CS_OpAuto_PixelDifference(VBtask task) : base(task)
        {
            task.gOptions.pixelDiffThreshold = 2; // set it low so it will move up to the right value.
            labels = new string[] { "", "", "2D Histogram view with highlighted peaks", "" };
            desc = "Find the peaks in a 2D histogram";
        }
        public void RunCS(Mat src)
        {
            if (!task.heartBeat && task.frameCount > 10) return;
            if (standaloneTest())
            {
                diff.Run(src.CvtColor(ColorConversionCodes.BGR2GRAY));
                src = diff.dst2;
            }
            int gridCount = 0;
            foreach (var roi in task.gridList)
            {
                if (src[roi].CountNonZero() > 0) gridCount++;
            }
            if (task.gOptions.pixelDiffThreshold < task.gOptions.getPixelDifferenceMax())
            {
                if (gridCount > task.gridList.Count() / 10) task.gOptions.pixelDiffThreshold++;
            }
            if (gridCount == 0 && task.gOptions.pixelDiffThreshold > 1) task.gOptions.pixelDiffThreshold--;
            SetTrueText("Pixel difference threshold is at " + task.gOptions.getPixelDifference().ToString());
            dst2 = src;
        }
    }
    public class CS_OpAuto_MSER : CS_Parent
    {
        MSER_Basics mBase = new MSER_Basics();
        public int classCount;
        bool checkOften = true;
        TrackBar minSlider;
        TrackBar maxSlider;
        public CS_OpAuto_MSER(VBtask task) : base(task)
        {
            minSlider = FindSlider("MSER Min Area");
            maxSlider = FindSlider("MSER Max Area");
            desc = "Option Automation: find the best MSER max and min area values";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                mBase.Run(src);
                src = mBase.dst3;
                classCount = mBase.mserCells.Count();
            }
            dst2 = src.Clone();
            if (task.heartBeat || checkOften)
            {
                if (src.Channels() != 1) dst1 = src.CvtColor(ColorConversionCodes.BGR2GRAY); else dst1 = src;
                int count = dst1.CountNonZero();
                int desired = (int)(dst2.Total() * 0.6);
                if (count < desired)
                {
                    if (maxSlider.Value < maxSlider.Maximum - 1000) maxSlider.Value += 1000;
                }
                if (classCount > 35)
                {
                    if (minSlider.Value < minSlider.Maximum - 100) minSlider.Value += 100;
                }
                else
                {
                    if (classCount > 0) checkOften = false;
                    if (classCount < 25)
                    {
                        if (minSlider.Value > 100) minSlider.Value -= 100;
                    }
                }
                strOut = "NonZero pixel count = " + count.ToString() + "\n" +
                         "Desired pixel count (60% of total) = " + desired.ToString() + "\n";
                strOut += "maxSlider Value = " + maxSlider.Value.ToString() + "\n";
                strOut += "Cells identified = " + classCount.ToString() + "\n";
                strOut += "minSlider value = " + minSlider.Value.ToString() + "\n";
                strOut += "checkOften variable is " + checkOften.ToString();
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_OpenGL_Basics : CS_Parent
    {
        MemoryMappedViewAccessor memMapWriter;
        readonly ProcessStartInfo startInfo = new ProcessStartInfo();
        IntPtr memMapPtr;
        public Mat dataInput = new Mat();
        public Mat pointCloudInput = new Mat();
        public int oglFunction = 0; // the default function is to display a point cloud.
        public Options_OpenGLFunctions options = new Options_OpenGLFunctions();
        byte[] rgbBuffer = new byte[1];
        byte[] dataBuffer = new byte[1];
        byte[] pointCloudBuffer = new byte[1];
        public CS_OpenGL_Basics(VBtask task) : base(task)
        {
            task.OpenGLTitle = "OpenGL_Functions";
            UpdateAdvice(traceName + ": 'Show All' to see all the OpenGL options.");
            pointCloudInput = new Mat(dst2.Size(), MatType.CV_32FC3, 0);
            desc = "Create an OpenGL window and update it with images";
        }
        double[] memMapFill()
        {
            double timeConversionUnits = 1000;
            double imuAlphaFactor = 0.98; // theta is a mix of acceleration data and gyro data.
            if (task.cameraName != "Intel(R) RealSense(TM) Depth Camera 435i")
            {
                timeConversionUnits = 1000 * 1000;
                imuAlphaFactor = 0.99;
            }
            int rgbBufferSize = rgbBuffer.Length > 1 ? rgbBuffer.Length : 0;
            int dataBufferSize = dataBuffer.Length > 1 ? dataBuffer.Length : 0;
            double showXYZaxis = 1;
            double activateTask = task.activateTaskRequest ? 1 : 0;
            double[] memMapValues = {
                task.frameCount, dst2.Width, dst2.Height, rgbBufferSize,
                dataBufferSize, options.FOV, options.yaw, options.pitch, options.roll,
                options.zNear, options.zFar, options.PointSizeSlider.Value, dataInput.Width, dataInput.Height,
                task.IMU_AngularVelocity.X, task.IMU_AngularVelocity.Y, task.IMU_AngularVelocity.Z,
                task.IMU_Acceleration.X, task.IMU_Acceleration.Y, task.IMU_Acceleration.Z, task.IMU_TimeStamp,
                1, options.eye[0] / 100, options.eye[1] / 100, options.eye[2] / 100, options.zTrans,
                options.scaleXYZ[0] / 10, options.scaleXYZ[1] / 10, options.scaleXYZ[2] / 10, timeConversionUnits, imuAlphaFactor,
                task.OpenGLTitle.Length, pointCloudInput.Width, pointCloudInput.Height, oglFunction,
                activateTask, showXYZaxis
            };
            return memMapValues;
        }
        void MemMapUpdate()
        {
            double[] memMap = memMapFill();
            Marshal.Copy(memMap, 0, memMapPtr, memMap.Length);
            memMapWriter.WriteArray<double>(0, memMap, 0, memMap.Length);
        }
        void StartOpenGLWindow()
        {
            task.pipeName = "OpenCVBImages" + task.pipeCount.ToString();
            try
            {
                task.openGLPipe = new NamedPipeServerStream(task.pipeName, PipeDirection.InOut, 1);
            }
            catch (Exception) { }
            task.pipeCount++;
            double[] memMap = memMapFill();
            int memMapbufferSize = 8 * memMap.Length;
            startInfo.FileName = task.OpenGLTitle + ".exe";
            int windowWidth = 720;
            int windowHeight = 720 * 240 / 320;
            startInfo.Arguments = windowWidth.ToString() + " " + windowHeight.ToString() + " " + memMapbufferSize.ToString() + " " + task.pipeName;
            if (!task.showConsoleLog) startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(startInfo);
            memMapPtr = Marshal.AllocHGlobal(memMapbufferSize);
            MemoryMappedFile memMapFile = MemoryMappedFile.CreateOrOpen("OpenCVBControl", memMapbufferSize);
            memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize);
            task.openGLPipe.WaitForConnection();
            while (true)
            {
                task.openGL_hwnd = FindWindow(null, task.OpenGLTitle);
                if (task.openGL_hwnd != IntPtr.Zero) break;
            }
            task.oglRect = new cv.Rect(task.OpenGL_Left, task.OpenGL_Top, windowWidth, windowHeight);
            MoveWindow(task.openGL_hwnd, task.OpenGL_Left, task.OpenGL_Top, task.oglRect.Width, task.oglRect.Height, true);
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest()) pointCloudInput = task.pointCloud;
            // adjust the point cloud if present and the 'move' sliders are non-zero
            options.RunVB();
            cv.Scalar ptM = options.moveAmount;
            cv.Point3f shift = new cv.Point3f((float)ptM[0], (float)ptM[1], (float)ptM[2]);
            if (pointCloudInput.Width != 0 && (shift.X != 0 ||
                shift.Y != 0 || shift.Z != 0)) pointCloudInput -= ptM;
            if (src.Width > 0)
            {
                src = src.CvtColor(ColorConversionCodes.BGR2RGB); // OpenGL needs RGB, not BGR
                if (rgbBuffer.Length != src.Total() * src.ElemSize()) Array.Resize(ref rgbBuffer, (int)(src.Total() * src.ElemSize()));
            }
            if (dataInput.Width > 0)
            {
                if (dataBuffer.Length != dataInput.Total() * dataInput.ElemSize()) Array.Resize(ref dataBuffer, (int)(dataInput.Total() * dataInput.ElemSize()));
            }
            else
            {
                Array.Resize(ref dataBuffer, 1);
            }
            if (pointCloudInput.Width > 0)
            {
                if (pointCloudBuffer.Length != pointCloudInput.Total() * pointCloudInput.ElemSize()) Array.Resize(ref pointCloudBuffer, (int)(pointCloudInput.Total() * pointCloudInput.ElemSize()));
            }
            if (memMapPtr == IntPtr.Zero)
            {
                StartOpenGLWindow();
            }
            else
            {
                byte[] readPipe = new byte[4]; // we read 4 bytes because that is the signal that the other end of the named pipe wrote 4 bytes to indicate iteration complete.
                if (task.openGLPipe != null)
                {
                    int bytesRead = task.openGLPipe.Read(readPipe, 0, 4);
                    if (bytesRead == 0) SetTrueText("The OpenGL process appears to have stopped.", new cv.Point(20, 100));
                }
            }
            MemMapUpdate();
            if (src.Width > 0) Marshal.Copy(src.Data, rgbBuffer, 0, rgbBuffer.Length);
            if (dataInput.Width > 0) Marshal.Copy(dataInput.Data, dataBuffer, 0, dataBuffer.Length);
            if (pointCloudInput.Width > 0) 
                Marshal.Copy(pointCloudInput.Data, pointCloudBuffer, 0, 
                    (int)(pointCloudInput.Total() * pointCloudInput.ElemSize()));
            try
            {
                if (src.Width > 0) task.openGLPipe.Write(rgbBuffer, 0, rgbBuffer.Length);
                if (dataInput.Width > 0) task.openGLPipe.Write(dataBuffer, 0, dataBuffer.Length);
                if (pointCloudInput.Width > 0) task.openGLPipe.Write(pointCloudBuffer, 0, pointCloudBuffer.Length);
                byte[] buff = System.Text.Encoding.UTF8.GetBytes(task.OpenGLTitle);
                task.openGLPipe.Write(buff, 0, task.OpenGLTitle.Length);
                // lose a lot of performance doing this!
                if (task.gOptions.getOpenGLCapture())
                {
                    Bitmap snapshot = GetWindowImage(task.openGL_hwnd, new cv.Rect(0, 0, (int)(task.oglRect.Width * 1.4), 
                                        (int)(task.oglRect.Height * 1.4)));
                    Mat snap = BitmapConverter.ToMat(snapshot);
                    snap = snap.CvtColor(ColorConversionCodes.BGRA2BGR);
                    dst3 = snap.Resize(new cv.Size(dst2.Width, dst2.Height), 0, 0, InterpolationFlags.Nearest);
                }
            }
            catch (Exception)
            {
                // OpenGL window was likely closed.  
            }
            // If standaloneTest() Then SetTrueText(task.gMat.strout, 3)
            if (standaloneTest()) SetTrueText(task.gMat.strOut, 3);
        }
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
    }







  
    public class CS_OpenGL_BasicsSliders : CS_Parent
    {
        Options_OpenGL options = new Options_OpenGL();
        public Mat pointCloudInput;
        public CS_OpenGL_BasicsSliders(VBtask task) : base(task)
        {
            task.OpenGLTitle = "OpenGL_Basics";
            FindSlider("OpenGL FOV").Value = 150;
            desc = "Show the OpenGL point cloud with sliders support.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            task.ogl.pointCloudInput = standaloneTest() ? task.pointCloud : pointCloudInput;
            // update all the options from the slider values.
            task.ogl.options.FOV = options.FOV;
            task.ogl.options.yaw = options.yaw;
            task.ogl.options.pitch = options.pitch;
            task.ogl.options.roll = options.roll;
            task.ogl.options.zNear = options.zNear;
            task.ogl.options.zFar = options.zFar;
            task.ogl.options.PointSizeSlider.Value = options.pointSize;
            task.ogl.options.zTrans = options.zTrans;
            task.ogl.options.eye = options.eye;
            task.ogl.options.scaleXYZ = options.scaleXYZ;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_BasicsMouse : CS_Parent
    {
        public CS_OpenGL_BasicsMouse(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Show the OpenGL point cloud with mouse support.";
        }
        public void RunCS(Mat src)
        {
            if (task.testAllRunning) return; // seems to not like it when running overnight but it runs fine.
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_ReducedXYZ : CS_Parent
    {
        readonly Reduction_XYZ reduction = new Reduction_XYZ();
        public CS_OpenGL_ReducedXYZ(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Display the pointCloud after reduction in X, Y, or Z dimensions.";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            dst2 = reduction.dst3;
            task.ogl.pointCloudInput = reduction.dst3;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_Reduction : CS_Parent
    {
        readonly Reduction_PointCloud reduction;
        public CS_OpenGL_Reduction(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            reduction = new Reduction_PointCloud();
            desc = "Use the reduced depth pointcloud in OpenGL";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            dst2 = reduction.dst2;
            task.ogl.pointCloudInput = reduction.dst3;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_ReducedSideView : CS_Parent
    {
        readonly PointCloud_ReducedSideView sideView = new PointCloud_ReducedSideView();
        public CS_OpenGL_ReducedSideView(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Use the reduced depth pointcloud in 3D but allow it to be rotated in Options_Common";
        }
        public void RunCS(Mat src)
        {
            sideView.Run(src);
            dst2 = sideView.dst2;
            task.ogl.pointCloudInput = sideView.dst3;
            task.ogl.Run(task.color);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            labels[2] = sideView.labels[2];
        }
    }

    public class CS_OpenGL_Rebuilt : CS_Parent
    {
        readonly Structured_Rebuild rebuild = new Structured_Rebuild();
        public CS_OpenGL_Rebuilt(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Review the rebuilt point cloud from Structured_Rebuild";
        }
        public void RunCS(Mat src)
        {
            rebuild.Run(src);
            dst2 = rebuild.dst2;
            task.ogl.pointCloudInput = rebuild.pointcloud;
            task.ogl.Run(task.color);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_VerticalSingle : CS_Parent
    {
        readonly FeatureLine_LongestV_Tutorial2 vLine = new FeatureLine_LongestV_Tutorial2();
        public CS_OpenGL_VerticalSingle(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.verticalLines;
            desc = "Visualize the vertical line found with FeatureLine_LongestV_Tutorial";
        }
        public void RunCS(Mat src)
        {
            vLine.Run(src);
            dst2 = vLine.dst2;
            dst3 = vLine.dst3;
            var pt1 = vLine.pt1;
            var pt2 = vLine.pt2;
            var linePairs3D = new List<Point3f>
                {
                    new Point3f((pt1.X + pt2.X) / 2, pt1.Y, (pt1.Z + pt2.Z) / 2),
                    new Point3f(pt1.X, pt2.Y, pt1.Z)
                };
            task.ogl.dataInput = new Mat(linePairs3D.Count(), 1, MatType.CV_32FC3, linePairs3D.ToArray());
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(task.color);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }

    public class CS_OpenGL_Pyramid : CS_Parent
    {
        public CS_OpenGL_Pyramid(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.drawPyramid; // all the work is done inside the switch statement in OpenGL_Functions.
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Draw the traditional OpenGL pyramid";
        }
        public void RunCS(Mat src)
        {
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_DrawCube : CS_Parent
    {
        public CS_OpenGL_DrawCube(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.drawCube;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Draw the traditional OpenGL cube";
        }
        public void RunCS(Mat src)
        {
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_QuadSimple : CS_Parent
    {
        readonly Tessallate_QuadSimple tess = new Tessallate_QuadSimple();
        public CS_OpenGL_QuadSimple(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.simplePlane;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Create a simple plane in each roi of the RedCloud data";
        }
        public void RunCS(Mat src)
        {
            tess.Run(src);
            dst2 = tess.dst2;
            dst3 = tess.dst3;
            labels = tess.labels;
            task.ogl.dataInput = new Mat(tess.oglData.Count(), 1, MatType.CV_32FC3, tess.oglData.ToArray());
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(dst3);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_QuadHulls : CS_Parent
    {
        readonly Tessallate_QuadHulls tess = new Tessallate_QuadHulls();
        public CS_OpenGL_QuadHulls(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.simplePlane;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Create a simple plane in each roi of the RedCloud data";
        }
        public void RunCS(Mat src)
        {
            tess.Run(src);
            dst2 = tess.dst2;
            dst3 = tess.dst3;
            labels = tess.labels;
            task.ogl.dataInput = new Mat(tess.oglData.Count(), 1, MatType.CV_32FC3, tess.oglData.ToArray());
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(dst3);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_QuadMinMax : CS_Parent
    {
        readonly Tessallate_QuadMinMax tess = new Tessallate_QuadMinMax();
        public CS_OpenGL_QuadMinMax(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.simplePlane;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Reflect the min and max for each roi of the RedCloud data";
        }
        public void RunCS(Mat src)
        {
            tess.Run(src);
            dst2 = tess.dst2;
            dst3 = tess.dst3;
            labels = tess.labels;
            task.ogl.dataInput = new Mat(tess.oglData.Count(), 1, MatType.CV_32FC3, tess.oglData.ToArray());
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(dst3);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_Bricks : CS_Parent
    {
        readonly Tessallate_Bricks tess = new Tessallate_Bricks();
        public CS_OpenGL_Bricks(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.minMaxBlocks;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Create blocks in each roi using the min and max depth values";
        }
        public void RunCS(Mat src)
        {
            tess.Run(src);
            task.ogl.dataInput = new Mat(tess.oglData.Count(), 1, MatType.CV_32FC3, tess.oglData.ToArray());
            dst2 = tess.dst3;
            dst3 = tess.hulls.dst3;
            int index = 0;
            foreach (var roi in task.gridList)
            {
                if (index < tess.depths.Count())
                {
                    SetTrueText(tess.depths[index].ToString(fmt1) + "\n" + tess.depths[index + 1].ToString(fmt1), 
                                new cv.Point(roi.X, roi.Y), 2);
                }
                index += 2;
            }
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_StructuredCloud : CS_Parent
    {
        readonly Structured_Cloud sCloud = new Structured_Cloud();
        readonly RedCloud_Basics redC = new RedCloud_Basics();
        public CS_OpenGL_StructuredCloud(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            labels[2] = "Structured cloud 32fC3 data";
            desc = "Visualize the Structured_Cloud";
        }
        public void RunCS(Mat src)
        {
            sCloud.Run(src);
            redC.Run(src);
            dst2 = redC.dst2;
            labels = redC.labels;
            task.ogl.pointCloudInput = sCloud.dst2;
            task.ogl.Run(dst2);
            task.ogl.options.PointSizeSlider.Value = task.gridSize;
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_Tiles : CS_Parent
    {
        Structured_Tiles sCloud = new Structured_Tiles();
        public CS_OpenGL_Tiles(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.drawTiles;
            task.OpenGLTitle = "OpenGL_Functions";
            labels = new string[] { "", "", "Input from Structured_Tiles", "" };
            desc = "Display the quads built by Structured_Tiles in OpenGL - uses OpenGL's point size";
        }
        public void RunCS(Mat src)
        {
            sCloud.Run(src);
            dst2 = sCloud.dst2;
            dst3 = sCloud.dst3;
            task.ogl.dataInput = new Mat(sCloud.oglData.Count(), 1, MatType.CV_32FC3, sCloud.oglData.ToArray());
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            task.ogl.options.PointSizeSlider.Value = task.gridSize;
        }
    }
    public class CS_OpenGL_TilesQuad : CS_Parent
    {
        Structured_TilesQuad sCloud = new Structured_TilesQuad();
        public CS_OpenGL_TilesQuad(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.simplePlane;
            task.OpenGLTitle = "OpenGL_Functions";
            labels = new string[] { "", "", "Input from Structured_Tiles", "" };
            desc = "Display the quads built by Structured_TilesQuad in OpenGL - does NOT use OpenGL's point size";
        }
        public void RunCS(Mat src)
        {
            sCloud.Run(src);
            dst2 = sCloud.dst2;
            task.ogl.dataInput = new Mat(sCloud.oglData.Count(), 1, MatType.CV_32FC3, sCloud.oglData.ToArray());
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_OnlyPlanes : CS_Parent
    {
        readonly Plane_OnlyPlanes planes = new Plane_OnlyPlanes();
        public CS_OpenGL_OnlyPlanes(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            labels = new string[] { "", "", "RedCloud Cells", "Planes built in the point cloud" };
            desc = "Display the pointCloud as a set of RedCloud cell planes";
        }
        public void RunCS(Mat src)
        {
            planes.Run(src);
            dst2 = planes.dst2;
            dst3 = planes.dst3;
            task.ogl.pointCloudInput = planes.dst3;
            task.ogl.Run(task.color);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_FlatStudy1 : CS_Parent
    {
        readonly Structured_LinearizeFloor plane = new Structured_LinearizeFloor();
        public CS_OpenGL_FlatStudy1(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            labels = new string[] { "", "", "Side view of point cloud - use mouse to highlight the floor", "Highlight the floor in BGR image" };
            desc = "Convert depth cloud floor to a plane and visualize it with OpenGL";
        }
        public void RunCS(Mat src)
        {
            plane.Run(src);
            dst2 = plane.dst3;
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(plane.dst2);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_FlatStudy2 : CS_Parent
    {
        public Structured_LinearizeFloor plane = new Structured_LinearizeFloor();
        public CS_OpenGL_FlatStudy2(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.drawFloor;
            desc = "Show the floor in the pointcloud as a plane";
        }
        public void RunCS(Mat src)
        {
            plane.Run(src);
            dst2 = plane.dst3;
            List<float> oglData = new List<float>();
            var floorColor = task.color.Mean(plane.sliceMask);
            oglData.Add((float)floorColor[0]);
            oglData.Add((float)floorColor[1]);
            oglData.Add((float)floorColor[2]);
            oglData.Add(plane.floorYPlane);
            task.ogl.dataInput = new Mat(4, 1, MatType.CV_32F, oglData.ToArray());
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(plane.dst2);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_FlatStudy3 : CS_Parent
    {
        Plane_FloorStudy plane = new Plane_FloorStudy();
        TrackBar cushionSlider;
        public CS_OpenGL_FlatStudy3(VBtask task) : base(task)
        {
            cushionSlider = FindSlider("Structured Depth slice thickness in pixels");
            task.ogl.oglFunction = (int)oCase.floorStudy;
            task.OpenGLTitle = "OpenGL_Functions";
            labels = new string[] { "", "", "", "" };
            desc = "Create an OpenGL display where the floor is built as a quad";
        }
        public void RunCS(Mat src)
        {
            plane.Run(src);
            dst2 = plane.dst2;
            labels[2] = plane.labels[2];
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.dataInput = new Mat(1, 1, MatType.CV_32F, new float[] { plane.planeY });
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_FlatFloor : CS_Parent
    {
        Model_FlatSurfaces flatness = new Model_FlatSurfaces();
        public CS_OpenGL_FlatFloor(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.floorStudy;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Using minimal cost, create an OpenGL display where the floor is built as a quad";
        }
        public void RunCS(Mat src)
        {
            flatness.Run(src);
            SetTrueText(flatness.labels[2], 3);
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.dataInput = new Mat(1, 1, MatType.CV_32F, new float[] { task.pcFloor });
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            labels[2] = flatness.labels[2];
            labels[3] = flatness.labels[3];
        }
    }
    public class CS_OpenGL_FlatCeiling : CS_Parent
    {
        Model_FlatSurfaces flatness = new Model_FlatSurfaces();
        public CS_OpenGL_FlatCeiling(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.floorStudy;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Using minimal cost, create an OpenGL display where the ceiling is built as a quad";
        }
        public void RunCS(Mat src)
        {
            flatness.Run(src);
            SetTrueText(flatness.labels[2], 3);
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.dataInput = new Mat(1, 1, MatType.CV_32F, new float[] { task.pcCeiling });
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            labels[2] = flatness.labels[2];
            labels[3] = flatness.labels[3];
        }
    }
    public class CS_OpenGL_PeakFlat : CS_Parent
    {
        Plane_Histogram peak = new Plane_Histogram();
        Kalman_Basics kalman = new Kalman_Basics();
        public CS_OpenGL_PeakFlat(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.floorStudy;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Display the peak flat level in OpenGL";
        }
        public void RunCS(Mat src)
        {
            peak.Run(src);
            dst2 = peak.dst2;
            labels[2] = peak.labels[3];
            kalman.kInput = new float[] { peak.peakFloor, peak.peakCeiling };
            kalman.Run(empty);
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.dataInput = new Mat(2, 1, MatType.CV_32F, new float[] { kalman.kOutput[0], kalman.kOutput[1] });
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_DrawHull : CS_Parent
    {
        RedCloud_Hulls hulls = new RedCloud_Hulls();
        public CS_OpenGL_DrawHull(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.drawCell;
            task.OpenGLTitle = "OpenGL_Functions";
            labels = new string[] { "", "", "RedCloud output", "" };
            desc = "Select a cell and display its hull in OpenGL as a polygon.";
        }
        public void RunCS(Mat src)
        {
            hulls.Run(src);
            dst2 = hulls.dst2;
            List<Point3f> oglData = new List<Point3f>();
            var rc = task.rc;
            List<Point3f> hull = new List<Point3f>();
            if (rc.hull != null)
            {
                foreach (var pt in rc.hull)
                {
                    hull.Add(task.pointCloud[rc.rect].Get<Point3f>(pt.Y, pt.X));
                }
                for (int i = 0; i < hull.Count(); i += 3)
                {
                    oglData.Add(hull[i % hull.Count()]);
                    oglData.Add(hull[(i + 1) % hull.Count()]);
                    oglData.Add(hull[(i + 2) % hull.Count()]);
                }
            }
            task.ogl.dataInput = new Mat(oglData.Count(), 1, MatType.CV_32FC3, oglData.ToArray());
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_FPolyCloud : CS_Parent
    {
        FeaturePoly_PointCloud fpolyPC = new FeaturePoly_PointCloud();
        public CS_OpenGL_FPolyCloud(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            if (standaloneTest()) task.gOptions.setDisplay1();
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Display the pointcloud after FeaturePoly_PointCloud identifies the changes depth pixels";
        }
        public void RunCS(Mat src)
        {
            fpolyPC.Run(src);
            dst1 = fpolyPC.dst1;
            dst2 = fpolyPC.dst2;
            dst3 = fpolyPC.dst3;
            SetTrueText(fpolyPC.fMask.fImage.strOut, 1);
            labels = fpolyPC.labels;
            task.ogl.pointCloudInput = fpolyPC.fPolyCloud;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_Sierpinski : CS_Parent
    {
        public CS_OpenGL_Sierpinski(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.sierpinski;
            task.OpenGLTitle = "OpenGL_Functions";
            FindSlider("OpenGL Point Size").Value = 3;
            desc = "Draw the Sierpinski triangle pattern in OpenGL";
        }
        public void RunCS(Mat src)
        {
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_DrawHulls : CS_Parent
    {
        public Options_OpenGLFunctions options = new Options_OpenGLFunctions();
        public RedCloud_Hulls hulls = new RedCloud_Hulls();
        public CS_OpenGL_DrawHulls(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.drawCells;
            task.OpenGLTitle = "OpenGL_Functions";
            labels = new string[] { "", "", "", "" };
            desc = "Draw all the hulls in OpenGL";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            var ptM = options.moveAmount;
            var shift = new Point3f((float)ptM[0], (float)ptM[1], (float)ptM[2]);
            hulls.Run(src);
            dst2 = hulls.dst2;
            var rcx = task.rc;
            List<Point3f> oglData = new List<Point3f>();
            oglData.Add(new Point3f());
            int polygonCount = 0;
            foreach (var rc in task.redCells)
            {
                if (rc.hull == null) continue;
                int hullIndex = oglData.Count();
                oglData.Add(new Point3f(rc.hull.Count(), 0, 0));
                if (rc.index == rcx.index)
                {
                    oglData.Add(new Point3f(1, 1, 1));
                }
                else
                {
                    oglData.Add(new Point3f(rc.color[2] / 255f, rc.color[1] / 255f, rc.color[0] / 255f));
                }
                int hullPoints = 0;
                foreach (var pt in rc.hull)
                {
                    var ptNew = pt;
                    if (pt.X > rc.rect.Width) ptNew.X = rc.rect.Width - 1;
                    if (pt.Y > rc.rect.Height) ptNew.Y = rc.rect.Height - 1;
                    var v = task.pointCloud[rc.rect].Get<Point3f>(ptNew.Y, ptNew.X);
                    if (v.Z > 0)
                    {
                        hullPoints++;
                        oglData.Add(new Point3f(v.X + shift.X, v.Y + shift.Y, v.Z + shift.Z));
                    }
                }
                oglData[hullIndex] = new Point3f(hullPoints, 0, 0);
                polygonCount++;
            }
            oglData[0] = new Point3f(polygonCount, 0, 0);
            task.ogl.dataInput = new Mat(oglData.Count(), 1, MatType.CV_32FC3, oglData.ToArray());
            task.ogl.Run(dst2);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst2;
            SetTrueText(polygonCount.ToString() + " polygons were sent to OpenGL", 2);
        }
    }
    public class CS_OpenGL_Contours : CS_Parent
    {
        Options_OpenGL_Contours options2 = new Options_OpenGL_Contours();
        public Options_OpenGLFunctions options = new Options_OpenGLFunctions();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_OpenGL_Contours(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.drawCells;
            task.OpenGLTitle = "OpenGL_Functions";
            FindSlider("OpenGL shift fwd/back (Z-axis)").Value = -150;
            labels = new string[] { "", "", "Output of RedCloud", "OpenGL snapshot" };
            desc = "Draw all the RedCloud contours in OpenGL with various settings.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            var ptM = options.moveAmount;
            var shift = new Point3f((float)ptM[0], (float)ptM[1], (float)ptM[2]);
            options2.RunVB();
            redC.Run(src);
            dst2 = redC.dst2;
            var rcx = task.rc;
            int polygonCount = 0;
            var oglData = new List<Point3f>();
            Scalar lastDepth;
            oglData.Add(new Point3f());
            foreach (var rc in task.redCells)
            {
                var d = rc.depthMean[2];
                if (d == 0) continue;
                int dataIndex = oglData.Count();
                oglData.Add(new Point3f(rc.contour.Count(), 0, 0));
                if (rc.index == rcx.index)
                {
                    oglData.Add(new Point3f(1, 1, 1));
                }
                else
                {
                    oglData.Add(new Point3f(rc.color[2] / 255f, rc.color[1] / 255f, rc.color[0] / 255f));
                }
                lastDepth = rc.depthMean;
                foreach (var pt in rc.contour)
                {
                    var ptNew = pt;
                    if (pt.X > rc.rect.Width) ptNew.X = rc.rect.Width - 1;
                    if (pt.Y > rc.rect.Height) ptNew.Y = rc.rect.Height - 1;
                    var v = task.pointCloud[rc.rect].Get<Point3f>(ptNew.Y, ptNew.X);
                    if (options2.depthPointStyle == (int)pointStyle.flattened || 
                        options2.depthPointStyle == (int)pointStyle.flattenedAndFiltered) v.Z = (float)d;
                    if (options2.depthPointStyle == (int)pointStyle.filtered || 
                        options2.depthPointStyle == (int)pointStyle.flattenedAndFiltered)
                    {
                        if (Math.Abs(v.X - lastDepth[0]) > options2.filterThreshold) v.X = (float)lastDepth[0];
                        if (Math.Abs(v.Y - lastDepth[1]) > options2.filterThreshold) v.Y = (float)lastDepth[1];
                        if (Math.Abs(v.Z - lastDepth[2]) > options2.filterThreshold) v.Z = (float)lastDepth[2];
                    }
                    oglData.Add(new Point3f(v.X + shift.X, v.Y + shift.Y, v.Z + shift.Z));
                    lastDepth = new cv.Scalar(v.X, v.Y, v.Z);
                }
                oglData[dataIndex] = new Point3f(rc.contour.Count(), 0, 0);
                polygonCount++;
            }
            oglData[0] = new Point3f(polygonCount, 0, 0);
            task.ogl.dataInput = new Mat(oglData.Count(), 1, MatType.CV_32FC3, oglData.ToArray());
            task.ogl.Run(new Mat());
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_PCLineCandidates : CS_Parent
    {
        PointCloud_Basics pts = new PointCloud_Basics();
        public CS_OpenGL_PCLineCandidates(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pcPointsAlone;
            task.OpenGLTitle = "OpenGL_Functions";
            FindSlider("OpenGL Point Size").Value = 10;
            desc = "Display the output of the PointCloud_Basics";
        }
        public void RunCS(Mat src)
        {
            pts.Run(src);
            dst2 = pts.dst2;
            task.ogl.dataInput = new Mat(pts.allPointsH.Count(), 1, MatType.CV_32FC3, pts.allPointsH.ToArray());
            task.ogl.Run(new Mat());
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            labels[2] = "Point cloud points found = " + (pts.actualCount / 2).ToString();
        }
    }
    public class CS_OpenGL_PClinesFirstLast : CS_Parent
    {
        Line3D_CandidatesFirstLast lines = new Line3D_CandidatesFirstLast();
        public CS_OpenGL_PClinesFirstLast(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pcLines;
            task.OpenGLTitle = "OpenGL_Functions";
            FindSlider("OpenGL Point Size").Value = 10;
            desc = "Draw the 3D lines found from the PCpoints";
        }
        public void RunCS(Mat src)
        {
            lines.Run(src);
            dst2 = lines.dst2;
            task.ogl.dataInput = lines.pcLinesMat.Rows == 0 ? new Mat() : lines.pcLinesMat;
            task.ogl.Run(new Mat());
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            labels[2] = "OpenGL_PClines found " + (lines.pcLinesMat.Rows / 3).ToString() + " lines";
        }
    }
    public class CS_OpenGL_PClinesAll : CS_Parent
    {
        Line3D_CandidatesAll lines = new Line3D_CandidatesAll();
        public CS_OpenGL_PClinesAll(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pcLines;
            task.OpenGLTitle = "OpenGL_Functions";
            FindSlider("OpenGL Point Size").Value = 10;
            desc = "Draw the 3D lines found from the PCpoints";
        }
        public void RunCS(Mat src)
        {
            lines.Run(src);
            dst2 = lines.dst2;
            task.ogl.dataInput = lines.pcLinesMat.Rows == 0 ? new Mat() : lines.pcLinesMat;
            task.ogl.Run(new Mat());
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            labels[2] = "OpenGL_PClines found " + (lines.pcLinesMat.Rows / 3).ToString() + " lines";
        }
    }
    public class CS_OpenGL_PatchHorizontal : CS_Parent
    {
        Pixel_NeighborsPatchNeighbors patch = new Pixel_NeighborsPatchNeighbors();
        public CS_OpenGL_PatchHorizontal(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Draw the point cloud after patching z-values that are similar";
        }
        public void RunCS(Mat src)
        {
            patch.Run(src);
            dst2 = patch.dst3;
            task.ogl.pointCloudInput = dst2;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_PCpoints : CS_Parent
    {
        PointCloud_PCPoints pts = new PointCloud_PCPoints();
        public CS_OpenGL_PCpoints(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pcPoints;
            task.OpenGLTitle = "OpenGL_Functions";
            FindSlider("OpenGL Point Size").Value = 10;
            desc = "Display the output of the PointCloud_Points";
        }
        public void RunCS(Mat src)
        {
            pts.Run(src);
            dst2 = pts.dst2;
            task.ogl.dataInput = new Mat(pts.pcPoints.Count(), 1, MatType.CV_32FC3, pts.pcPoints.ToArray());
            task.ogl.Run(new Mat());
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            labels[2] = "Point cloud points found = " + (pts.pcPoints.Count() / 2).ToString();
        }
    }
    public class CS_OpenGL_PCpointsPlane : CS_Parent
    {
        PointCloud_PCPointsPlane pts = new PointCloud_PCPointsPlane();
        public CS_OpenGL_PCpointsPlane(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pcPoints;
            task.OpenGLTitle = "OpenGL_Functions";
            FindSlider("OpenGL Point Size").Value = 10;
            desc = "Display the points that are likely to be in a plane - found by both the vertical and horizontal searches";
        }
        public void RunCS(Mat src)
        {
            pts.Run(src);
            task.ogl.dataInput = new Mat(pts.pcPoints.Count(), 1, MatType.CV_32FC3, pts.pcPoints.ToArray());
            task.ogl.Run(new Mat());
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            labels[2] = "Point cloud points found = " + pts.pcPoints.Count() / 2;
        }
    }
    public class CS_OpenGL_PlaneClusters3D : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Plane_Equation eq = new Plane_Equation();
        public CS_OpenGL_PlaneClusters3D(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pcPoints;
            FindSlider("OpenGL Point Size").Value = 10;
            labels[3] = "Only the cells with a high probability plane are presented - blue on X-axis, green on Y-axis, red on Z-axis";
            desc = "Cluster the plane equations to find major planes in the image and display the clusters in OpenGL";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            dst3 = redC.dst3;
            List<Point3f> pcPoints = new List<Point3f>();
            Point3f blue = new Point3f(0, 0, 1), red = new Point3f(1, 0, 0), green = new Point3f(0, 1, 0);
            foreach (var rc in task.redCells)
            {
                rcData rcNew = rc;
                if (rc.maxVec.Z > 0)
                {
                    eq.rc = rc;
                    eq.Run(src);
                    rcNew = eq.rc;
                }
                if (rcNew.eq == new Vec4f()) continue;
                if (rcNew.eq.Item0 > rcNew.eq.Item1 && rcNew.eq.Item0 > rcNew.eq.Item2) pcPoints.Add(red);
                if (rcNew.eq.Item1 > rcNew.eq.Item0 && rcNew.eq.Item1 > rcNew.eq.Item2) pcPoints.Add(green);
                if (rcNew.eq.Item2 > rcNew.eq.Item0 && rcNew.eq.Item2 > rcNew.eq.Item1) pcPoints.Add(blue);
                pcPoints.Add(new Point3f(rcNew.eq.Item0 * 0.5f, rcNew.eq.Item1 * 0.5f, rcNew.eq.Item2 * 0.5f));
            }
            task.ogl.dataInput = new Mat(pcPoints.Count(), 1, MatType.CV_32FC3, pcPoints.ToArray());
            task.ogl.Run(new Mat());
        }
    }
    public class CS_OpenGL_Profile : CS_Parent
    {
        public Profile_Basics sides = new Profile_Basics();
        public Profile_Rotation rotate = new Profile_Rotation();
        HeatMap_Basics heat = new HeatMap_Basics();
        public CS_OpenGL_Profile(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            if (standaloneTest()) task.gOptions.setGravityUsage(false);
            task.ogl.oglFunction = (int)oCase.pcPointsAlone;
            labels[3] = "Contour of selected cell is shown below.  Blue dot represents the minimum X (leftmost) point and red the maximum X (rightmost)";
            desc = "Visualize a RedCloud Cell and rotate it using the Options_IMU Sliders";
        }
        public void RunCS(Mat src)
        {
            sides.Run(src);
            dst2 = sides.dst2;
            var rc = task.rc;
            var contourMat = new Mat(rc.contour.Count(), 1, MatType.CV_32SC2, rc.contour.ToArray());
            if (rc.contour.Count() == 0) return;
            var split = contourMat.Split();
            var mm = GetMinMax(split[0]);
            var p1 = rc.contour.ElementAt(mm.minLoc.Y);
            var p2 = rc.contour.ElementAt(mm.maxLoc.Y);
            dst3.SetTo(0);
            DrawContour(dst3[rc.rect], rc.contour, Scalar.Yellow);
            DrawCircle(dst3, new cv.Point(p1.X + rc.rect.X, p1.Y + rc.rect.Y), task.DotSize + 2, Scalar.Blue);
            DrawCircle(dst3, new cv.Point(p2.X + rc.rect.X, p2.Y + rc.rect.Y), task.DotSize + 2, Scalar.Red);
            if (rc.contour3D.Count() > 0)
            {
                var vecMat = new Mat(rc.contour3D.Count(), 1, MatType.CV_32FC3, rc.contour3D.ToArray());
                rotate.Run(empty);
                Mat output = vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * rotate.gMat.gMatrix;
                vecMat = output.Reshape(3, vecMat.Rows);
                task.ogl.pointCloudInput = new Mat();
                task.ogl.dataInput = vecMat;
                heat.Run(vecMat);
                dst1 = heat.dst0.Threshold(0, 255, ThresholdTypes.Binary);
            } 
            task.ogl.Run(new Mat());
        }
    }
    public class CS_OpenGL_ProfileSweep : CS_Parent
    {
        OpenGL_Profile visuals = new OpenGL_Profile();
        Options_IMU options = new Options_IMU();
        int testCase = 0;
        TrackBar xRotateSlider;
        TrackBar yRotateSlider;
        TrackBar zRotateSlider;
        public CS_OpenGL_ProfileSweep(VBtask task) : base(task)
        {
            xRotateSlider = FindSlider("Rotate pointcloud around X-axis (degrees)");
            yRotateSlider = FindSlider("Rotate pointcloud around Y-axis (degrees)");
            zRotateSlider = FindSlider("Rotate pointcloud around Z-axis (degrees)");
            if (standaloneTest()) task.gOptions.setDisplay1();
            desc = "Test the X-, Y-, and Z-axis rotation in sequence";
        }
        public void RunCS(Mat src)
        {
            task.gOptions.setGravityUsage(false);
            if (task.frameCount % 100 == 0)
            {
                testCase++;
                if (testCase >= 3) testCase = 0;
                options.RunVB();
                options.rotateX = -45;
                options.rotateY = -45;
                options.rotateZ = -45;
            }
            int bump = 1;
            switch (testCase)
            {
                case 0:
                    zRotateSlider.Value += bump;
                    if (zRotateSlider.Value >= 45) zRotateSlider.Value = -45;
                    labels[3] = "Rotating around X-axis with " + zRotateSlider.Value + " degrees";
                    break;
                case 1:
                    yRotateSlider.Value += bump;
                    if (yRotateSlider.Value >= 45) yRotateSlider.Value = -45;
                    labels[3] = "Rotating around Y-axis with " + yRotateSlider.Value + " degrees";
                    break;
                case 2:
                    xRotateSlider.Value += bump;
                    if (xRotateSlider.Value >= 45) xRotateSlider.Value = -45;
                    labels[3] = "Rotating around Z-axis with " + xRotateSlider.Value + " degrees";
                    break;
            }
            SetTrueText("Top down view: " + labels[3], 1);
            visuals.Run(src);
            dst1 = visuals.dst1;
            dst2 = visuals.dst2;
            dst3 = visuals.dst3;
        }
    }
    public class CS_OpenGL_FlatSurfaces : CS_Parent
    {
        RedCloud_LikelyFlatSurfaces flat = new RedCloud_LikelyFlatSurfaces();
        public CS_OpenGL_FlatSurfaces(VBtask task) : base(task)
        {
            labels[2] = "Display the point cloud pixels that appear to be vertical and horizontal regions.";
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Review the vertical and horizontal regions from Plane_Basics.";
        }
        public void RunCS(Mat src)
        {
            flat.Run(src);
            task.pointCloud.CopyTo(dst2, flat.dst2);
            task.ogl.pointCloudInput = dst2;
            task.ogl.Run(src);
        }
    }
    public class CS_OpenGL_GradientPhase : CS_Parent
    {
        Gradient_Depth gradient = new Gradient_Depth();
        public CS_OpenGL_GradientPhase(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Show the depth gradient Phase in OpenGL";
        }
        public void RunCS(Mat src)
        {
            gradient.Run(src);
            dst2 = gradient.dst2;
            dst3 = gradient.dst3;
            dst1 = GetNormalize32f(gradient.dst3);
            labels = gradient.labels;
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(dst1);
        }
    }
    public class CS_OpenGL_GravityTransform : CS_Parent
    {
        public CS_OpenGL_GravityTransform(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Use the IMU's acceleration values to build the transformation matrix of an OpenGL viewer";
        }
        public void RunCS(Mat src)
        {
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_GravityAverage : CS_Parent
    {
        readonly IMU_Average imuAvg = new IMU_Average();
        readonly IMU_Basics imu = new IMU_Basics();
        public CS_OpenGL_GravityAverage(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Build the GMatrix with the Average IMU acceleration (not the raw or filtered values) and use the resulting GMatrix to stabilize the point cloud in OpenGL";
        }
        public void RunCS(Mat src)
        {
            strOut = "To remove the point cloud averaging, set the global option 'Frame History' to 1.\n" +
                        "Or, even alternatively, run the 'OpenGL_GravityTransform' algorithm.\n\n" +
                        "Before Averaging: Average IMU acceleration: X = " + string.Format(fmt3, task.IMU_RawAcceleration.X) + ", Y = " + string.Format(fmt3, task.IMU_RawAcceleration.Y) +
                        ", Z = " + string.Format(fmt3, task.IMU_RawAcceleration.Z) + "\n";
            imuAvg.Run(src);
            task.IMU_RawAcceleration = task.IMU_AverageAcceleration;
            imu.Run(src);
            task.accRadians.Z += (float)Cv2.PI / 2;
            strOut += "After Averaging: Average IMU accerlation: X = " + string.Format(fmt3, task.IMU_Acceleration.X) + ", Y = " + string.Format(fmt3, task.IMU_Acceleration.Y) +
                        ", Z = " + string.Format(fmt3, task.IMU_Acceleration.Z) + "\n";
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            SetTrueText(strOut, 3);
        }
    }
    public class CS_OpenGL_GravityKalman : CS_Parent
    {
        readonly IMU_Kalman imuKalman = new IMU_Kalman();
        readonly IMU_Basics imu = new IMU_Basics();
        public CS_OpenGL_GravityKalman(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Build the GMatrix with the Average IMU acceleration (not the raw or filtered values) and use the resulting GMatrix to stabilize the point cloud in OpenGL";
        }
        public void RunCS(Mat src)
        {
            strOut = "To remove the point cloud averaging, set the global option 'Frame History' to 1.\n" +
                        "Or, even alternatively, run the 'OpenGL_GravityTransform' algorithm.\n\n" +
                        "Before Kalman: IMU acceleration: X = " + string.Format(fmt3, task.IMU_RawAcceleration.X) + ", Y = " + string.Format(fmt3, task.IMU_RawAcceleration.Y) +
                        ", Z = " + string.Format(fmt3, task.IMU_RawAcceleration.Z) + "\n";
            imuKalman.Run(src);
            task.IMU_RawAcceleration = task.IMU_Acceleration;
            imu.Run(src);
            task.accRadians.Z += (float)Cv2.PI / 2;
            strOut += "After Kalman: IMU acceleration: X = " + string.Format(fmt3, task.IMU_Acceleration.X) + ", Y = " + string.Format(fmt3, task.IMU_Acceleration.Y) +
                        ", Z = " + string.Format(fmt3, task.IMU_Acceleration.Z) + "\n";
            task.IMU_Acceleration = task.kalmanIMUacc;
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            SetTrueText(strOut, 3);
        }
    }
    public class CS_OpenGL_StableMinMax : CS_Parent
    {
        readonly Depth_MinMaxNone minmax = new Depth_MinMaxNone();
        public CS_OpenGL_StableMinMax(VBtask task) : base(task)
        {
            task.gOptions.setUnfiltered(true);
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            labels = new string[] { "", "", "Pointcloud Max", "Pointcloud Min" };
            desc = "display the Pointcloud Min or Max in OpenGL";
        }
        public void RunCS(Mat src)
        {
            minmax.Run(task.pointCloud);
            dst2 = minmax.dst2;
            if (minmax.options.useMax || minmax.options.useMin) task.ogl.pointCloudInput = dst2;
            else task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(task.color);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            labels[2] = minmax.labels[2];
        }
    }
    public class CS_OpenGL_DiffDepth : CS_Parent
    {
        Diff_Depth32S diff = new Diff_Depth32S();
        public CS_OpenGL_DiffDepth(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            labels = new string[] { "", "", "Point cloud after filtering for consistent depth", "" };
            desc = "Run OpenGL with a point cloud with consistent depth data (defined with slider in Motion_PixelDiff)";
        }
        public void RunCS(Mat src)
        {
            diff.Run(src);
            dst2 = diff.dst2;
            if (!task.gOptions.debugChecked) task.pointCloud.SetTo(0, dst2);
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(src);
            labels = diff.labels;
        }
    }
    public class CS_OpenGL_CloudMisses : CS_Parent
    {
        History_Basics frames = new History_Basics();
        public CS_OpenGL_CloudMisses(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            labels = new string[] { "", "", "Point cloud after over the last X frames", "" };
            desc = "Run OpenGL removing all pixels not present for all X frames";
        }
        public void RunCS(Mat src)
        {
            frames.Run(task.depthMask / 255);
            dst2 = frames.dst2;
            dst2 = dst2.Threshold(frames.saveFrames.Count() - 1, 255, ThresholdTypes.Binary);
            task.ogl.pointCloudInput.SetTo(0);
            task.pointCloud.CopyTo(task.ogl.pointCloudInput, dst2);
            task.ogl.Run(src);
        }
    }
    public class CS_OpenGL_CloudHistory : CS_Parent
    {
        History_Cloud hCloud = new History_Cloud();
        public CS_OpenGL_CloudHistory(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            labels = new string[] { "", "", "Point cloud after over the last X frames", "Mask to remove partially missing pixels" };
            desc = "Run OpenGL with a masked point cloud averaged over the last X frames.";
        }
        public void RunCS(Mat src)
        {
            hCloud.Run(task.pointCloud);
            dst2 = hCloud.dst2;
            task.ogl.pointCloudInput = dst2;
            task.ogl.Run(src);
        }
    }
    public class CS_OpenGL_TessellateCell : CS_Parent
    {
        Triangle_Basics tess = new Triangle_Basics();
        public CS_OpenGL_TessellateCell(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.tessalateTriangles;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Display a tessellated representation of the point cloud";
        }
        public void RunCS(Mat src)
        {
            tess.Run(src);
            dst2 = tess.dst2;
            dst3 = tess.dst3;
            task.ogl.dataInput = new Mat(tess.triangles.Count(), 1, MatType.CV_32FC3, tess.triangles.ToArray());
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(tess.dst2);
            labels = tess.labels;
        }
    }
    public class CS_OpenGL_Tessellate : CS_Parent
    {
        Triangle_RedCloud tess = new Triangle_RedCloud();
        public CS_OpenGL_Tessellate(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.tessalateTriangles;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Display a tessellated representation of the point cloud";
        }
        public void RunCS(Mat src)
        {
            tess.Run(src);
            dst2 = tess.dst2;
            dst3 = tess.dst3;
            task.ogl.dataInput = new Mat(tess.triangles.Count(), 1, MatType.CV_32FC3, tess.triangles.ToArray());
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(tess.dst2);
            labels = tess.labels;
        }
    }
    public class CS_OpenGL_TessellateRGB : CS_Parent
    {
        Triangle_RedCloud tess = new Triangle_RedCloud();
        public CS_OpenGL_TessellateRGB(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.tessalateTriangles;
            task.OpenGLTitle = "OpenGL_Functions";
            desc = "Display a tessellated representation of the point cloud";
        }
        public void RunCS(Mat src)
        {
            tess.Run(src);
            dst2 = tess.dst2;
            dst3 = tess.dst3;
            task.ogl.dataInput = new Mat(tess.triangles.Count(), 1, MatType.CV_32FC3, tess.triangles.ToArray());
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(src);
            labels = tess.labels;
        }
    }
    public class CS_OpenGL_RedTrack : CS_Parent
    {
        RedTrack_Basics redCC = new RedTrack_Basics();
        public CS_OpenGL_RedTrack(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Display all the RedCC cells in OpenGL";
        }
        public void RunCS(Mat src)
        {
            redCC.Run(src);
            dst2 = redCC.dst2;
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(dst2);
            SetTrueText(redCC.strOut, 3);
        }
    }
    public class CS_OpenGL_Density2D : CS_Parent
    {
        Density_Basics dense = new Density_Basics();
        public CS_OpenGL_Density2D(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            dst2 = new Mat(dst2.Size(), MatType.CV_32FC3, 0);
            desc = "Create a mask showing which pixels are close to each other and display the results.";
        }
        public void RunCS(Mat src)
        {
            dense.Run(src);
            dst2.SetTo(0);
            task.pointCloud.CopyTo(dst2, dense.dst2);
            task.ogl.pointCloudInput = dst2;
            task.ogl.Run(new Mat(dst2.Size(), MatType.CV_8UC3, Scalar.White));
        }
    }
    public class CS_OpenGL_ViewObjects : CS_Parent
    {
        GuidedBP_Points bpDoctor = new GuidedBP_Points();
        public CS_OpenGL_ViewObjects(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Identify the objects in the scene and display them in OpenGL with their respective colors.";
        }
        public void RunCS(Mat src)
        {
            dst1 = task.pointCloud.Clone();
            bpDoctor.Run(src);
            dst2 = bpDoctor.dst2;
            dst0 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(0, 255, ThresholdTypes.Binary);
            dst1.SetTo(0, ~dst0);
            task.ogl.pointCloudInput = dst1;
            task.ogl.Run(dst2);
        }
    }
    public class CS_OpenGL_NoSolo : CS_Parent
    {
        BackProject_SoloTop hotTop = new BackProject_SoloTop();
        BackProject_SoloSide hotSide = new BackProject_SoloSide();
        public CS_OpenGL_NoSolo(VBtask task) : base(task)
        {
            task.useXYRange = false;
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            labels[2] = "The points below were identified as solo points in the point cloud";
            desc = "Display point cloud without solo points";
        }
        public void RunCS(Mat src)
        {
            hotTop.Run(src);
            dst2 = hotTop.dst3;
            hotSide.Run(src);
            dst2 = dst2 | hotSide.dst3;
            if (!task.gOptions.debugChecked)
                task.pointCloud.SetTo(0, dst2);
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(src);
            SetTrueText("Toggle the solo points on and off using the 'DebugCheckBox' global option.", 3);
        }
    }
    public class CS_OpenGL_RedCloud : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_OpenGL_RedCloud(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Display all the RedCloud cells in OpenGL";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(dst2);
        }
    }
    public class CS_OpenGL_RedCloudSpectrum : CS_Parent
    {
        Spectrum_RedCloud redS = new Spectrum_RedCloud();
        public CS_OpenGL_RedCloudSpectrum(VBtask task) : base(task)
        {
            task.redOptions.setUseDepth(true);
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Display all the RedCloud cells after Spectrum filtering.";
        }
        public void RunCS(Mat src)
        {
            redS.Run(src);
            dst2 = redS.dst3;
            task.pointCloud.SetTo(0, dst2.InRange(0, 0));
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(dst2);
        }
    }
    public class CS_OpenGL_RedCloudCell : CS_Parent
    {
        Spectrum_Z specZ = new Spectrum_Z();
        Spectrum_Breakdown breakdown = new Spectrum_Breakdown();
        public CS_OpenGL_RedCloudCell(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Isolate a RedCloud cell - after filtering by Spectrum_Depth - in an OpenGL display";
        }
        public void RunCS(Mat src)
        {
            dst2 = specZ.options.runRedCloud(ref labels[2]);
            specZ.Run(src);
            SetTrueText(specZ.strOut, 3);
            if (task.ClickPoint == new cv.Point() && task.redCells.Count() > 1)
            {
                task.rc = task.redCells[1]; // pick the largest cell
                task.ClickPoint = task.rc.maxDist;
            }
            breakdown.Run(src);
            task.ogl.pointCloudInput.SetTo(0);
            task.pointCloud[task.rc.rect].CopyTo(task.ogl.pointCloudInput[task.rc.rect], task.rc.mask);
            task.ogl.Run(dst2);
            if (task.gOptions.getOpenGLCapture())
                dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_FilteredSideView : CS_Parent
    {
        BackProject2D_FilterSide filter = new BackProject2D_FilterSide();
        public CS_OpenGL_FilteredSideView(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Use the BackProject2D_FilterSide to remove low sample bins and trim the loose fragments in 3D";
        }
        public void RunCS(Mat src)
        {
            filter.Run(src);
            dst2 = filter.dst2;
            task.ogl.pointCloudInput = dst2;
            task.ogl.Run(src);
        }
    }
    public class CS_OpenGL_FilteredTopView : CS_Parent
    {
        BackProject2D_FilterTop filter = new BackProject2D_FilterTop();
        public CS_OpenGL_FilteredTopView(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Use the BackProject2D_FilterSide to remove low sample bins and trim the loose fragments in 3D";
        }
        public void RunCS(Mat src)
        {
            filter.Run(src);
            dst2 = filter.dst2;
            task.ogl.pointCloudInput = dst2;
            task.ogl.Run(src);
        }
    }
    public class CS_OpenGL_FilteredBoth : CS_Parent
    {
        BackProject2D_FilterBoth filter = new BackProject2D_FilterBoth();
        public CS_OpenGL_FilteredBoth(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Use the BackProject2D_FilterSide/Top to remove low sample bins and trim the loose fragments in 3D";
        }
        public void RunCS(Mat src)
        {
            filter.Run(src);
            dst2 = filter.dst2;
            task.ogl.pointCloudInput = dst2;
            task.ogl.Run(src);
        }
    }
    public class CS_OpenGL_Filtered3D : CS_Parent
    {
        Hist3Dcloud_BP_Filter filter = new Hist3Dcloud_BP_Filter();
        public CS_OpenGL_Filtered3D(VBtask task) : base(task)
        {
            task.gOptions.setOpenGLCapture(true);
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Use the BackProject2D_FilterSide/Top to remove low sample bins and trim the loose fragments in 3D";
        }
        public void RunCS(Mat src)
        {
            filter.Run(src);
            dst2 = filter.dst3;
            task.ogl.pointCloudInput = dst2;
            task.ogl.Run(src);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_HistNorm3D : CS_Parent
    {
        public CS_OpenGL_HistNorm3D(VBtask task) : base(task)
        {
            task.OpenGLTitle = "OpenGL_Functions";
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Create an OpenGL plot using the BGR data normalized to between 0 and 1.";
        }
        public void RunCS(Mat src)
        {
            src.ConvertTo(src, MatType.CV_32FC3);
            task.ogl.pointCloudInput = src.Normalize(0, 1, NormTypes.MinMax);
            task.ogl.Run(new Mat());
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_HistDepth3D : CS_Parent
    {
        Hist3Dcloud_Basics hcloud = new Hist3Dcloud_Basics();
        public CS_OpenGL_HistDepth3D(VBtask task) : base(task)
        {
            task.ogl.oglFunction = (int)oCase.Histogram3D;
            task.OpenGLTitle = "OpenGL_Functions";
            task.ogl.options.PointSizeSlider.Value = 10;
            desc = "Display the 3D histogram of the depth in OpenGL";
        }
        public void RunCS(Mat src)
        {
            hcloud.Run(src);
            Mat histogram = new Mat(task.redOptions.histBins3D, 1, MatType.CV_32F, hcloud.histogram.Data);
            task.ogl.dataInput = histogram;
            task.ogl.pointCloudInput = new Mat();
            task.ogl.Run(new Mat());
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
            SetTrueText("Use the sliders for X/Y/Z histogram bins to add more points");
        }
    }
    public class CS_OpenGL_SoloPointsRemoved : CS_Parent
    {
        Area_SoloPoints solos = new Area_SoloPoints();
        public CS_OpenGL_SoloPointsRemoved(VBtask task) : base(task)
        {
            task.gOptions.setUnfiltered(true); // show all the unfiltered points so removing the points is obvious.
            task.OpenGLTitle = "OpenGL_Functions";
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            desc = "Remove the solo points and display the pointcloud";
        }
        public void RunCS(Mat src)
        {
            if (task.toggleOnOff)
            {
                solos.Run(src);
                dst2 = solos.dst2;
                task.pointCloud.SetTo(0, dst2);
            }
            else
            {
                dst2.SetTo(0);
            }
            task.ogl.pointCloudInput = task.pointCloud;
            task.ogl.Run(src);
            SetTrueText("You should see the difference in the OpenGL window as the solo points are toggled on an off.", 3);
        }
    }

    public class CS_OpenGL_Duster : CS_Parent
    {
        Duster_Basics duster = new Duster_Basics();
        Options_OpenGL_Duster options = new Options_OpenGL_Duster();
        public CS_OpenGL_Duster(VBtask task) : base(task)
        {
            desc = "Show a dusted version point cloud";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            duster.Run(src);
            dst2 = duster.dst3;
            task.ogl.pointCloudInput = options.useTaskPointCloud ? task.pointCloud : duster.dst2;
            task.ogl.Run(options.useClusterColors == false ? task.color : dst2);
        }
    }
    public class CS_OpenGL_DusterY : CS_Parent
    {
        Duster_BasicsY duster = new Duster_BasicsY();
        Options_OpenGL_Duster options = new Options_OpenGL_Duster();
        public CS_OpenGL_DusterY(VBtask task) : base(task)
        {
            desc = "Show a dusted version point cloud";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            duster.Run(src);
            dst2 = duster.dst3;
            task.ogl.pointCloudInput = options.useTaskPointCloud ? task.pointCloud : duster.dst2;
            task.ogl.Run(options.useClusterColors == false ? task.color : dst2);
        }
    }
    public class CS_OpenGL_Color3D : CS_Parent
    {
        Hist3Dcolor_Basics hColor = new Hist3Dcolor_Basics();
        public CS_OpenGL_Color3D(VBtask task) : base(task)
        {
            task.OpenGLTitle = "OpenGL_Functions";
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            task.ogl.options.PointSizeSlider.Value = 10;
            desc = "Plot the results of a 3D histogram of the BGR data ";
        }
        public void RunCS(Mat src)
        {
            hColor.Run(src);
            dst2 = hColor.dst3;
            labels[2] = hColor.labels[2];
            dst2.ConvertTo(dst1, MatType.CV_32FC3);
            dst1 = dst1.Normalize(0, 1, NormTypes.MinMax);
            var split = dst1.Split();
            split[1] *= -1;
            Cv2.Merge(split, task.ogl.pointCloudInput);
            task.ogl.Run(dst2);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_ColorReduced3D : CS_Parent
    {
        Color8U_Basics colorClass = new Color8U_Basics();
        public CS_OpenGL_ColorReduced3D(VBtask task) : base(task)
        {
            task.OpenGLTitle = "OpenGL_Functions";
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            FindSlider("OpenGL Point Size").Value = 20;
            desc = "Connect the 3D representation of the different color formats with colors in that format (see dst2)";
        }
        public void RunCS(Mat src)
        {
            colorClass.Run(src);
            dst2 = colorClass.dst3;
            dst2.ConvertTo(dst1, MatType.CV_32FC3);
            labels[2] = "There are " + colorClass.classCount.ToString() + " classes for " + task.redOptions.colorInputName;
            dst1 = dst1.Normalize(0, 1, NormTypes.MinMax);
            var split = dst1.Split();
            split[1] *= -1;
            Cv2.Merge(split, task.ogl.pointCloudInput);
            task.ogl.Run(dst2);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_ColorRaw : CS_Parent
    {
        public CS_OpenGL_ColorRaw(VBtask task) : base(task)
        {
            task.OpenGLTitle = "OpenGL_Functions";
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            task.ogl.options.PointSizeSlider.Value = 10;
            desc = "Plot the results of a 3D histogram of the BGR data";
        }
        public void RunCS(Mat src)
        {
            dst2 = src;
            src.ConvertTo(dst1, MatType.CV_32FC3);
            dst1 = dst1.Normalize(0, 1, NormTypes.MinMax);
            var split = dst1.Split();
            split[1] *= -1;
            Cv2.Merge(split, task.ogl.pointCloudInput);
            task.ogl.Run(dst2);
            if (task.gOptions.getOpenGLCapture()) dst3 = task.ogl.dst3;
        }
    }
    public class CS_OpenGL_ColorBin4Way : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_OpenGL_ColorBin4Way(VBtask task) : base(task)
        {
            task.OpenGLTitle = "OpenGL_Functions";
            task.ogl.oglFunction = (int)oCase.pointCloudAndRGB;
            task.ogl.options.PointSizeSlider.Value = 10;
            dst0 = new Mat(dst0.Size(), MatType.CV_8UC3, Scalar.White);
            desc = "Plot the results of a 3D histogram of the lightest and darkest BGR data";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            dst1.SetTo(0);
            task.color[task.rc.rect].CopyTo(dst1[task.rc.rect], task.rc.mask);
            dst1.ConvertTo(dst3, MatType.CV_32FC3);
            dst3 = dst3.Normalize(0, 1, NormTypes.MinMax);
            var split = dst3.Split();
            split[1] *= -1;
            Cv2.Merge(split, task.ogl.pointCloudInput);
            task.ogl.Run(dst0);
        }
    }
    public class CS_ORB_Basics : CS_Parent
    {
        public KeyPoint[] keypoints;
        ORB orb;
        Options_ORB options = new Options_ORB();    
        public CS_ORB_Basics(VBtask task) : base(task)
        {
            desc = "Find keypoints using ORB - Oriented Fast and Rotated BRIEF";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (src.Channels() == 3)
                src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            orb = ORB.Create(options.desiredCount);
            keypoints = orb.Detect(src);
            dst2 = src.Clone();
            foreach (KeyPoint kpt in keypoints)
            {
                DrawCircle(dst2, kpt.Pt, task.DotSize + 1, Scalar.Yellow);
            }
            labels[2] = keypoints.Length + " key points were identified";
        }
    }
    public class CS_Palette_Basics : CS_Parent
    {
        public bool whitebackground;
        public CS_Palette_Basics(VBtask task) : base(task)
        {
            desc = "Apply the different color maps in OpenCV";
        }
        public void RunCS(Mat src)
        {
            labels[2] = "ColorMap = " + task.gOptions.getPalette();
            if (src.Type() == MatType.CV_32F)
            {
                src = GetNormalize32f(src);
                src.ConvertTo(src, MatType.CV_8U);
            }
            var mapIndex = ColormapTypes.Autumn;
            if (task.paletteIndex == 1) mapIndex = ColormapTypes.Bone;
            if (task.paletteIndex == 2) mapIndex = ColormapTypes.Cividis;
            if (task.paletteIndex == 3) mapIndex = ColormapTypes.Cool;
            if (task.paletteIndex == 4) mapIndex = ColormapTypes.Hot;
            if (task.paletteIndex == 5) mapIndex = ColormapTypes.Hsv;
            if (task.paletteIndex == 6) mapIndex = ColormapTypes.Inferno;
            if (task.paletteIndex == 7) mapIndex = ColormapTypes.Jet;
            if (task.paletteIndex == 8) mapIndex = ColormapTypes.Magma;
            if (task.paletteIndex == 9) mapIndex = ColormapTypes.Ocean;
            if (task.paletteIndex == 10) mapIndex = ColormapTypes.Parula;
            if (task.paletteIndex == 11) mapIndex = ColormapTypes.Pink;
            if (task.paletteIndex == 12) mapIndex = ColormapTypes.Plasma;
            if (task.paletteIndex == 13) mapIndex = ColormapTypes.Rainbow;
            if (task.paletteIndex == 14) mapIndex = ColormapTypes.Spring;
            if (task.paletteIndex == 15) mapIndex = ColormapTypes.Summer;
            if (task.paletteIndex == 16) mapIndex = ColormapTypes.Twilight;
            if (task.paletteIndex == 17) mapIndex = ColormapTypes.TwilightShifted;
            if (task.paletteIndex == 18) mapIndex = ColormapTypes.Viridis;
            if (task.paletteIndex == 19) mapIndex = ColormapTypes.Winter;
            Cv2.ApplyColorMap(src, dst2, mapIndex);
        }
    }
    public class CS_Palette_Color : CS_Parent
    {
        Options_Colors options = new Options_Colors();
        public CS_Palette_Color(VBtask task) : base(task)
        {
            desc = "Define a color Using sliders.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2.SetTo(new Scalar(options.blueS, options.greenS, options.redS));
            dst3.SetTo(new Scalar(255 - options.blueS, 255 - options.greenS, 255 - options.redS));
            labels[2] = "Color (RGB) = " + options.blueS + " " + options.greenS + " " + options.redS;
            labels[3] = "Color (255 - RGB) = " + (255 - options.blueS) + " " + (255 - options.greenS) + " " +
                         (255 - options.redS);
        }
    }
    public class CS_Palette_LinearPolar : CS_Parent
    {
        public Options_Resize rotateOptions = new Options_Resize();
        Point2f pt;
        TrackBar radiusSlider;
        Options_Palette options = new Options_Palette();    
        public CS_Palette_LinearPolar(VBtask task) : base(task)
        {
            radiusSlider = FindSlider("LinearPolar radius");
            pt = new Point2f(msRNG.Next(0, dst2.Cols - 1), msRNG.Next(0, dst2.Rows - 1));
            desc = "Use LinearPolar To create gradient image";
        }
        public void RunCS(Mat src)
        {
            dst2.SetTo(0);
            for (int i = 0; i < dst2.Rows; i++)
            {
                var c = i * 255 / dst2.Rows;
                dst2.Row(i).SetTo(new Scalar(c, c, c));
            }
            rotateOptions.RunVB();
            dst3.SetTo(0);
            if (rotateOptions.warpFlag == InterpolationFlags.WarpInverseMap) 
                radiusSlider.Value = radiusSlider.Maximum;
            Cv2.LinearPolar(dst2, dst2, pt, options.radius, rotateOptions.warpFlag);
            Cv2.LinearPolar(src, dst3, pt, options.radius, rotateOptions.warpFlag);
        }
    }
    public class CS_Palette_Reduction : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        public CS_Palette_Reduction(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": redOptions 'Reduction' to control results.");
            desc = "Map colors To different palette";
            labels[2] = "Reduced Colors";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            dst3 = reduction.dst2;
            dst2 = ShowPalette(dst3 * 255 / reduction.classCount);
        }
    }
    public class CS_Palette_DrawTest : CS_Parent
    {
        Draw_Shapes draw = new Draw_Shapes();
        public CS_Palette_DrawTest(VBtask task) : base(task)
        {
            desc = "Experiment With palette Using a drawn image";
        }
        public void RunCS(Mat src)
        {
            draw.Run(src);
            dst2 = ShowPalette(draw.dst2);
        }
    }
    public class CS_Palette_Gradient : CS_Parent
    {
        public Scalar color1;
        public Scalar color2;
        public CS_Palette_Gradient(VBtask task) : base(task)
        {
            labels[3] = "From And To colors";
            desc = "Create gradient image";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                if (standaloneTest())
                {
                    color1 = new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255));
                    color2 = new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255));
                    dst3.SetTo(color1);
                    dst3[new cv.Rect(0, 0, dst3.Width, dst3.Height / 2)].SetTo(color2);
                }
                var dst1 = new Mat(255, 1, MatType.CV_8UC3);
                double f = 1.0;
                for (int i = 0; i < dst1.Rows; i++)
                {
                    dst1.Set<Vec3b>(i, 0, new Vec3b((byte)(f * color2[0] + (1 - f) * color1[0]), 
                                                    (byte)(f * color2[1] + (1 - f) * color1[1]), 
                                                    (byte)(f * color2[2] + (1 - f) * color1[2])));
                    f -= 1 / (double)dst1.Rows;
                }
            }
            if (standaloneTest()) dst2 = dst1.Resize(dst2.Size());
        }
    }
    public class CS_Palette_DepthColorMap : CS_Parent
    {
        public Mat gradientColorMap = new Mat();
        Gradient_Color gColor = new Gradient_Color();
        Options_Palette options = new Options_Palette();        
        public CS_Palette_DepthColorMap(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": adjust color with 'Convert and Scale' slider");
            labels[3] = "Palette used To color left image";
            desc = "Build a colormap that best shows the depth.  NOTE: custom color maps need to use C++ ApplyColorMap.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
            {
                gColor.color1 = Scalar.Yellow;
                gColor.color2 = Scalar.Red;
                var gradMat = new Mat();
                gColor.gradientWidth = dst1.Width;
                gColor.Run(empty);
                gradientColorMap = gColor.gradient;
                gColor.color2 = gColor.color1;
                gColor.color1 = Scalar.Blue;
                gColor.Run(empty);
                Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap);
                gradientColorMap = gradientColorMap.Resize(new cv.Size(255, 1));
                if (standaloneTest())
                {
                    if (dst3.Width < 255) dst3 = new Mat(dst3.Height, 255, MatType.CV_8UC3, 0);
                    var r = new cv.Rect(0, 0, 255, 1);
                    for (int i = 0; i < dst3.Height; i++)
                    {
                        r.Y = i;
                        dst3[r] = gradientColorMap;
                    }
                }
            }
            var depth8u = task.pcSplit[2].ConvertScaleAbs(options.convertScale);
            var ColorMap = new Mat(256, 1, MatType.CV_8UC3, gradientColorMap.Data);
            Cv2.ApplyColorMap(depth8u, dst2, ColorMap);
            dst2.SetTo(0, task.noDepthMask);
        }
    }
    public class CS_Palette_RGBDepth : CS_Parent
    {
        Mat gradientColorMap = new Mat();
        Gradient_Color gColor = new Gradient_Color();
        public CS_Palette_RGBDepth(VBtask task) : base(task)
        {
            desc = "Build a colormap that best shows the depth.  NOTE: duplicate of Palette_DepthColorMap but no slider.";
        }
        public void RunCS(Mat src)
        {
            if (task.optionsChanged)
            {
                gColor.color1 = Scalar.Yellow;
                gColor.color2 = Scalar.Red;
                var gradMat = new Mat();
                gColor.gradientWidth = dst1.Width;
                gColor.Run(empty);
                gradientColorMap = gColor.gradient;
                gColor.color2 = gColor.color1;
                gColor.color1 = Scalar.Blue;
                gColor.Run(empty);
                Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap);
                gradientColorMap = gradientColorMap.Resize(new cv.Size(255, 1));
            }
            var sliderVal = (task.cameraName == "Intel(R) RealSense(TM) Depth Camera 435i") ? 50 : 80;
            var depth8u = task.pcSplit[2].ConvertScaleAbs(sliderVal);
            var ColorMap = new Mat(256, 1, MatType.CV_8UC3, gradientColorMap.Data);
            Cv2.ApplyColorMap(depth8u, dst2, ColorMap);
        }
    }
    public class CS_Palette_Layout2D : CS_Parent
    {
        public CS_Palette_Layout2D(VBtask task) : base(task)
        {
            desc = "Layout the available colors in a 2D grid";
        }
        public void RunCS(Mat src)
        {
            int index = 0;
            foreach (var r in task.gridList)
            {
                dst2[r].SetTo(task.scalarColors[index % 256]);
                index++;
            }
            labels[2] = "CS_Palette_Layout2D - " + task.gridList.Count().ToString() + " regions";
        }
    }
    public class CS_Palette_LeftRightImages : CS_Parent
    {
        public CS_Palette_LeftRightImages(VBtask task) : base(task)
        {
            desc = "Use a palette with the left and right images.";
        }
        public void RunCS(Mat src)
        {
            dst2 = ShowPalette(task.leftView.ConvertScaleAbs());
            dst3 = ShowPalette(task.rightView.ConvertScaleAbs());
        }
    }
    public class CS_Palette_TaskColors : CS_Parent
    {
        int direction = 1;
        public CS_Palette_TaskColors(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "ScalarColors", "VecColors" };
            desc = "Display that task.scalarColors and task.vecColors";
        }
        public void RunCS(Mat src)
        {
            if (task.gridSize <= 10) direction *= -1;
            if (task.gridSize >= 100) direction *= -1;
            task.gridSize -= direction * 1;
            task.grid.Run(src);
            for (int i = 0; i < task.gridList.Count(); i++)
            {
                var roi = task.gridList[i];
                dst2[roi].SetTo(task.scalarColors[i % 256]);
                dst3[roi].SetTo(task.vecColors[i % 256]);
            }
        }
    }
    public class CS_Palette_Create : CS_Parent
    {
        Mat colorGrad = new Mat();
        string activeSchemeName = "";
        int saveColorTransitionCount = -1;
        Options_Palette options = new Options_Palette();
        public CS_Palette_Create(VBtask task) : base(task)
        {
            desc = "Create a new palette";
        }
        Mat colorTransition(Scalar color1, Scalar color2, int width)
        {
            double f = 1.0;
            var gradientColors = new Mat(1, width, MatType.CV_64FC3);
            for (int i = 0; i < width; i++)
            {
                gradientColors.Set(0, i, new Scalar(f * color2[0] + (1 - f) * color1[0], f * color2[1] + (1 - f) * color1[1],
                    f * color2[2] + (1 - f) * color1[2]));
                f -= 1.0 / width;
            }
            var result = new Mat(1, width, MatType.CV_8UC3);
            for (int i = 0; i < width; i++)
            {
                result.Col(i).SetTo(gradientColors.Get<Scalar>(0, i));
            }
            return result;
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (activeSchemeName !=options.schemeName || options.transitions != saveColorTransitionCount)
            {
                activeSchemeName = options.schemeName;
                saveColorTransitionCount = options.transitions;
                if (activeSchemeName == "schemeRandom")
                {
                    var msRNG = new Random();
                    var color1 = new Scalar(0, 0, 0);
                    var color2 = new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255));
                    Mat gradMat = new Mat();
                    for (int i = 0; i <= options.transitions; i++)
                    {
                        gradMat = colorTransition(color1, color2, 255);
                        color1 = color2;
                        color2 = new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255));
                        if (i == 0) colorGrad = gradMat; else Cv2.HConcat(colorGrad, gradMat, colorGrad);
                    }
                    colorGrad = colorGrad.Resize(new cv.Size(256, 1));
                    Cv2.ImWrite(task.HomeDir + "data\\nextScheme.jpg", colorGrad); // use this to create new color schemes.
                }
                else
                {
                    colorGrad = Cv2.ImRead(options.schemeName).Row(0).Clone();
                }
            }
            SetTrueText("Use the 'Color Transitions' slider and radio buttons to change the color ranges.", 3);
            var depth8u = task.pcSplit[2].ConvertScaleAbs(options.transitions);
            var colorMap = new Mat(256, 1, MatType.CV_8UC3, colorGrad.Data);
            Cv2.ApplyColorMap(depth8u, dst2, colorMap);
            dst2.SetTo(0, task.noDepthMask);
        }
    }
    public class CS_Palette_Random : CS_Parent
    {
        public Mat colorMap;
        public CS_Palette_Random(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": There are no options\nJust produces a colorMap filled with random vec3b's.");
            colorMap = new Mat(256, 1, MatType.CV_8UC3, 0);
            for (int i = 0; i <= 255; i++)
            {
                colorMap.Set<Vec3b>(i, 0, randomCellColor());
            }
            desc = "Build a random colorGrad - no smooth transitions.";
        }
        public void RunCS(Mat src)
        {
            Cv2.ApplyColorMap(src, dst2, colorMap);
        }
    }
    public class CS_Palette_Variable : CS_Parent
    {
        public Mat colorGrad;
        public Mat originalColorMap;
        public List<Vec3b> colors = new List<Vec3b>();
        public CS_Palette_Variable(VBtask task) : base(task)
        {
            colorGrad = new Mat(1, 256, MatType.CV_8UC3, 0);
            for (int i = 0; i <= 255; i++)
            {
                colorGrad.Set<Vec3b>(0, i, randomCellColor());
            }
            originalColorMap = colorGrad.Clone();
            desc = "Build a new palette for every frame.";
        }
        public void RunCS(Mat src)
        {
            for (int i = 0; i < colors.Count(); i++)
            {
                colorGrad.Set<Vec3b>(0, i, colors[i]);
            }
            var colorMap = new Mat(256, 1, MatType.CV_8UC3, colorGrad.Data);
            Cv2.ApplyColorMap(src, dst2, colorMap);
        }
    }
    public class CS_Palette_RandomColorMap : CS_Parent
    {
        public Mat gradientColorMap = new Mat();
        public int transitionCount = -1;
        Gradient_Color gColor = new Gradient_Color();
        Options_Palette options = new Options_Palette();
        public CS_Palette_RandomColorMap(VBtask task) : base(task)
        {
            labels[3] = "Generated colormap";
            desc = "Build a random colormap that smoothly transitions colors";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
          
            if (transitionCount != options.transitions)
            {
                transitionCount = options.transitions;
                gColor.color1 = new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255));
                gColor.color2 = new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255));
                for (int i = 0; i < transitionCount; i++)
                {
                    gColor.gradientWidth = dst2.Width;
                    gColor.Run(empty);
                    gColor.color2 = gColor.color1;
                    gColor.color1 = new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255));
                    if (i == 0) gradientColorMap = gColor.gradient; else Cv2.HConcat(gradientColorMap, gColor.gradient, gradientColorMap);
                }
                gradientColorMap = gradientColorMap.Resize(new cv.Size(256, 1));
                if (standaloneTest()) dst3 = gradientColorMap;
                gradientColorMap.Set<Vec3b>(0, 0, new Vec3b()); // black is black!
            }
            var ColorMap = new Mat(256, 1, MatType.CV_8UC3, gradientColorMap.Data);
            Cv2.ApplyColorMap(src, dst2, ColorMap);
        }
    }
    public class CS_Palette_LoadColorMap : CS_Parent
    {
        public bool whitebackground;
        public Mat colorMap = new Mat();
        DirectoryInfo cMapDir;
        public CS_Palette_LoadColorMap(VBtask task) : base(task)
        {
            cMapDir = new DirectoryInfo(task.HomeDir + "opencv/modules/imgproc/doc/pics/colormaps");
            desc = "Apply the different color maps in OpenCV";
        }
        public void RunCS(Mat src)
        {
            if (task.optionsChanged || colorMap.Rows != 256)
            {
                labels[2] = "ColorMap = " + task.gOptions.getPalette();
                var str = cMapDir.FullName + "/colorscale_" + task.gOptions.getPalette() + ".jpg";
                var mapFile = new FileInfo(str);
                var tmp = Cv2.ImRead(mapFile.FullName);
                tmp.Col(0).SetTo(whitebackground ? Scalar.White : Scalar.Black);
                tmp = tmp.Row(0);
                colorMap = new Mat(256, 1, MatType.CV_8UC3, tmp.Data).Clone();
            }
            if (src.Type() == MatType.CV_32F)
            {
                src = GetNormalize32f(src);
                src.ConvertTo(src, MatType.CV_8U);
            }
            Cv2.ApplyColorMap(src, dst2, colorMap);
            if (standalone) dst3 = colorMap.Resize(dst3.Size());
        }
    }
    public class CS_Palette_CustomColorMap : CS_Parent
    {
        public Mat colorMap;
        public CS_Palette_CustomColorMap(VBtask task) : base(task)
        {
            labels[2] = "ColorMap = " + task.gOptions.getPalette();
            if (standalone)
            {
                var cMapDir = new DirectoryInfo(task.HomeDir + "opencv/modules/imgproc/doc/pics/colormaps");
                var str = cMapDir.FullName + "/colorscale_" + task.gOptions.getPalette() + ".jpg";
                var mapFile = new FileInfo(str);
                var tmp = Cv2.ImRead(mapFile.FullName);
                colorMap = new Mat(256, 1, MatType.CV_8UC3, tmp.Data).Clone();
            }
            desc = "Apply the provided color map to the input image.";
        }
        public void RunCS(Mat src)
        {
            if (colorMap == null)
            {
                SetTrueText("With " + traceName + " the colorMap must be provided.  Update the ColorMap Mat and then call Run(src)...");
                return;
            }
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (src.Type() == MatType.CV_32F)
            {
                src = GetNormalize32f(src);
                src.ConvertTo(src, MatType.CV_8U);
            }
            Cv2.ApplyColorMap(src, dst2, colorMap);
            if (standalone) dst3 = colorMap.Resize(dst3.Size());
        }
    }
    public class CS_Palette_GrayToColor : CS_Parent
    {
        public CS_Palette_GrayToColor(VBtask task) : base(task)
        {
            desc = "Build a palette for the current image using samples from each gray level.  Everything turns out sepia-like.";
        }
        public void RunCS(Mat src)
        {
            dst2 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            var pixels = new List<byte>();
            var colors = new SortedList<byte, Vec3b>();
            for (int y = 0; y < dst2.Height; y++)
            {
                for (int x = 0; x < dst2.Width; x++)
                {
                    var val = dst2.Get<byte>(y, x);
                    var color = src.Get<Vec3b>(y, x);
                    if (!pixels.Contains(val))
                    {
                        pixels.Add(val);
                        colors.Add(val, color);
                    }
                    else
                    {
                        var sum = color[0] + color[1] + color[2];
                        var index = colors.Keys.IndexOf(val);
                        var lastColor = colors.ElementAt(index).Value;
                        var lastSum = lastColor[0] + lastColor[1] + lastColor[2];
                        if (sum > lastSum)
                        {
                            colors.RemoveAt(index);
                            colors.Add(val, color);
                        }
                    }
                }
            }
            var ColorMap = new Mat(256, 1, MatType.CV_8UC3, colors.Values.ToArray());
            Cv2.ApplyColorMap(src, dst2, ColorMap);
        }
    }
    public class CS_ParticleFilter_Example : CS_Parent
    {
        int imageFrame = 12;
        public CS_ParticleFilter_Example(VBtask task) : base(task)
        {
            cPtr = ParticleFilterTest_Open(task.HomeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols);
            desc = "Particle Filter example downloaded from github - hyperlink in the code shows URL.";
        }
        public void RunCS(Mat src)
        {
            imageFrame += 1;
            if (imageFrame % 45 == 0)
            {
                imageFrame = 13;
                ParticleFilterTest_Close(cPtr);
                cPtr = ParticleFilterTest_Open(task.HomeDir + "/Data/ballSequence/", dst2.Rows, dst2.Cols);
            }
            var nextFile = new FileInfo(task.HomeDir + "Data/ballSequence/color_" + imageFrame.ToString() + ".png");
            dst3 = Cv2.ImRead(nextFile.FullName).Resize(dst2.Size());
            IntPtr imagePtr = ParticleFilterTest_Run(cPtr);
            dst2 = new Mat(dst2.Rows, dst2.Cols, MatType.CV_8UC3, imagePtr).Clone();
        }
        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = ParticleFilterTest_Close(cPtr);
        }
    }
    public class CS_PCA_Prep_CPP : CS_Parent
    {
        public Mat inputData = new Mat();
        public CS_PCA_Prep_CPP(VBtask task) : base(task)
        {
            cPtr = PCA_Prep_Open();
            desc = "Take some pointcloud data and return the non-zero points in a point3f vector";
        }
        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32FC3) src = task.pointCloud;
            byte[] cppData = new byte[src.Total() * src.ElemSize()];
            Marshal.Copy(src.Data, cppData, 0, cppData.Length);
            GCHandle handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned);
            IntPtr imagePtr = PCA_Prep_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols);
            handleSrc.Free();
            int count = PCA_Prep_GetCount(cPtr);
            inputData = new Mat(count, 3, MatType.CV_32F, imagePtr).Clone();
            SetTrueText("Data has been prepared and resides in inputData public");
        }
        public void Close()
        {
            PCA_Prep_Close(cPtr);
        }
    }
    public class CS_PCA_Palettize : CS_Parent
    {
        public byte[] palette;
        public byte[] rgb;
        public byte[] buff;
        Palette_CustomColorMap custom = new Palette_CustomColorMap();
        public byte[] paletteImage;
        public CS_PCA_NColor nColor;
        public Options_PCA_NColor options = new Options_PCA_NColor();
        public CS_PCA_Palettize(VBtask task) : base(task)
        {
            nColor = new CS_PCA_NColor(task);
            FindSlider("Desired number of colors").Value = 256;
            desc = "Create a palette for the input image but don't use it.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            rgb = new byte[src.Total() * src.ElemSize()];
            buff = new byte[rgb.Length];
            Marshal.Copy(src.Data, rgb, 0, rgb.Length);
            Marshal.Copy(src.Data, buff, 0, buff.Length);
            palette = nColor.MakePalette(rgb, dst2.Width, dst2.Height, options.desiredNcolors);
            if (standaloneTest())
            {
                paletteImage = nColor.RgbToIndex(rgb, dst1.Width, dst1.Height, palette, options.desiredNcolors);
                Mat img8u = new Mat(dst2.Size(), MatType.CV_8U, 0);
                Marshal.Copy(paletteImage, 0, img8u.Data, paletteImage.Length);
                custom.colorMap = new Mat(256, 1, MatType.CV_8UC3, palette);
                custom.Run(img8u);
                dst2 = custom.dst2;
            }
            labels[2] = "The palette found from the current image (repeated across the image) with " + options.desiredNcolors.ToString() + " entries";
        }
    }
    public class CS_PCA_Basics : CS_Parent
    {
        PCA_Prep_CPP prep = new PCA_Prep_CPP();
        public PCA pca_analysis = new PCA();
        public bool runRedCloud;
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_PCA_Basics(VBtask task) : base(task)
        {
            desc = "Find the Principal Component Analysis vector for the 3D points in a RedCloud cell contour.";
        }
        public string displayResults()
        {
            string pcaStr = "EigenVector 3X3 matrix from PCA_Analysis of cell point cloud data at contour points:\n";
            for (int y = 0; y < pca_analysis.Eigenvectors.Rows; y++)
            {
                for (int x = 0; x < pca_analysis.Eigenvectors.Cols; x++)
                {
                    float val = pca_analysis.Eigenvectors.Get<float>(y, x);
                    pcaStr += string.Format("{0}\t", val.ToString("F3"));
                }
                pcaStr += "\n";
            }
            List<float> valList = new List<float>();
            pcaStr += "EigenValues (PCA)\t";
            for (int i = 0; i < pca_analysis.Eigenvalues.Rows; i++)
            {
                float val = pca_analysis.Eigenvalues.Get<float>(i, 0);
                pcaStr += string.Format("{0}\t", val.ToString("F3"));
                valList.Add(val);
            }
            if (valList.Count() == 0) return pcaStr;
            float best = valList.Min();
            int index = valList.IndexOf(best);
            pcaStr += "Min EigenValue = " + best.ToString("F3") + " at index = " + index.ToString() + "\n";
            pcaStr += "Principal Component Vector\t";
            for (int j = 0; j < pca_analysis.Eigenvectors.Cols; j++)
            {
                float val = pca_analysis.Eigenvectors.Get<float>(index, j);
                pcaStr += string.Format("{0}\t", val.ToString("F3"));
            }
            pcaStr += "\n";
            return pcaStr;
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest() || runRedCloud)
            {
                if (task.FirstPass) task.redOptions.setUseColorOnly(true);
                redC.Run(src);
                dst2 = redC.dst2;
                labels[2] = redC.labels[2];
            }
            var rc = task.rc;
            List<Point3f> inputPoints = new List<Point3f>();
            foreach (var pt in rc.contour)
            {
                var vec = task.pointCloud[rc.rect].Get<Point3f>(pt.Y, pt.X);
                if (vec.Z > 0) inputPoints.Add(vec);
            }
            if (inputPoints.Count() > 0)
            {
                Mat inputMat = new Mat(inputPoints.Count(), 3, MatType.CV_32F, inputPoints.ToArray());
                pca_analysis = new PCA(inputMat, new Mat(), PCA.Flags.DataAsRow);
                strOut = displayResults();
                SetTrueText(strOut, 3);
            }
            else
            {
                SetTrueText("Select a cell to compute the eigenvector");
            }
        }
    }
    public class CS_PCA_CellMask : CS_Parent
    {
        PCA_Basics pca = new PCA_Basics();
        PCA_Prep_CPP pcaPrep = new PCA_Prep_CPP();
        public CS_PCA_CellMask(VBtask task) : base(task)
        {
            pca.runRedCloud = true;
            desc = "Find the Principal Component Analysis vector for all the 3D points in a RedCloud cell.";
        }
        public void RunCS(Mat src)
        {
            pca.Run(src);
            dst2 = pca.dst2;
            labels[2] = pca.labels[2];
            var rc = task.rc;
            if (rc.maxVec.Z > 0)
            {
                pcaPrep.Run(task.pointCloud[rc.rect].Clone());
                if (pcaPrep.inputData.Rows > 0)
                {
                    pca.pca_analysis = new PCA(pcaPrep.inputData, new Mat(), PCA.Flags.DataAsRow);
                    strOut = pca.displayResults();
                }
            }
            else
            {
                strOut = "Selected cell has no 3D data.";
                pca.pca_analysis = null;
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_PCA_Reconstruct : CS_Parent
    {
        Mat[] images = new Mat[8];
        Mat[] images32f = new Mat[8];
        Options_PCA options = new Options_PCA();
        public CS_PCA_Reconstruct(VBtask task) : base(task)
        {
            desc = "Reconstruct a video stream as a composite of X images.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            int index = task.frameCount % images.Length;
            images[index] = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat gray32f = new Mat();
            images[index].ConvertTo(gray32f, MatType.CV_32F);
            gray32f = gray32f.Normalize(0, 255, NormTypes.MinMax);
            images32f[index] = gray32f.Reshape(1, 1);
            if (task.frameCount >= images.Length)
            {
                Mat data = new Mat(images.Length, src.Rows * src.Cols, MatType.CV_32F);
                for (int i = 0; i < images.Length; i++)
                {
                    images32f[i].CopyTo(data.Row(i));
                }
                PCA pca = new PCA(data, new Mat(), PCA.Flags.DataAsRow, options.retainedVariance);
                Mat point = pca.Project(data.Row(0));
                Mat reconstruction = pca.BackProject(point);
                reconstruction = reconstruction.Reshape(images[0].Channels(), images[0].Rows);
                reconstruction.ConvertTo(dst2, MatType.CV_8UC1);
            }
        }
    }
    public class CS_PCA_Depth : CS_Parent
    {
        PCA_Reconstruct pca = new PCA_Reconstruct();
        public CS_PCA_Depth(VBtask task) : base(task)
        {
            desc = "Reconstruct a depth stream as a composite of X images.";
        }
        public void RunCS(Mat src)
        {
            pca.Run(task.depthRGB);
            dst2 = pca.dst2;
        }
    }
    public class CS_PCA_DrawImage : CS_Parent
    {
        PCA_Reconstruct pca = new PCA_Reconstruct();
        Mat image = new Mat();
        public CS_PCA_DrawImage(VBtask task) : base(task)
        {
            image = Cv2.ImRead(task.HomeDir + "opencv/Samples/Data/pca_test1.jpg");
            desc = "Use PCA to find the principal direction of an object.";
            labels[2] = "Original image";
            labels[3] = "PCA Output";
        }
        void drawAxis(Mat img, cv.Point p, cv.Point q, Scalar color, float scale)
        {
            double angle = Math.Atan2(p.Y - q.Y, p.X - q.X);
            double hypotenuse = Math.Sqrt((p.Y - q.Y) * (p.Y - q.Y) + (p.X - q.X) * (p.X - q.X));
            q.X = (int)(p.X - scale * hypotenuse * Math.Cos(angle));
            q.Y = (int)(p.Y - scale * hypotenuse * Math.Sin(angle));
            img.Line(p, q, color, task.lineWidth, task.lineType);
            p.X = (int)(q.X + 9 * Math.Cos(angle + Math.PI / 4));
            p.Y = (int)(q.Y + 9 * Math.Sin(angle + Math.PI / 4));
            img.Line(p, q, color, task.lineWidth, task.lineType);
            p.X = (int)(q.X + 9 * Math.Cos(angle - Math.PI / 4));
            p.Y = (int)(q.Y + 9 * Math.Sin(angle - Math.PI / 4));
            img.Line(p, q, color, task.lineWidth, task.lineType);
        }
        public void RunCS(Mat src)
        {
            dst2 = image.Resize(dst2.Size());
            Mat gray = dst2.CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(50, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            HierarchyIndex[] hierarchy;
            cv.Point[][] contours;
            Cv2.FindContours(gray, out contours, out hierarchy, RetrievalModes.List, ContourApproximationModes.ApproxNone);
            dst3.SetTo(0);
            for (int i = 0; i < contours.Length; i++)
            {
                double area = Cv2.ContourArea(contours[i]);
                if (area < 100 || area > 100000) continue;
                Cv2.DrawContours(dst3, contours, i, Scalar.Red, task.lineWidth, task.lineType);
                int sz = contours[i].Length;
                Mat data_pts = new Mat(sz, 2, MatType.CV_64FC1);
                for (int j = 0; j < data_pts.Rows; j++)
                {
                    data_pts.Set<double>(j, 0, contours[i][j].X);
                    data_pts.Set<double>(j, 1, contours[i][j].Y);
                }
                PCA pca_analysis = new PCA(data_pts, new Mat(), PCA.Flags.DataAsRow);
                cv.Point cntr = new cv.Point((int)pca_analysis.Mean.Get<double>(0, 0), (int)pca_analysis.Mean.Get<double>(0, 1));
                Point2d[] eigen_vecs = new Point2d[2];
                double[] eigen_val = new double[2];
                for (int j = 0; j < 2; j++)
                {
                    eigen_vecs[j] = new Point2d(pca_analysis.Eigenvectors.Get<double>(j, 0), pca_analysis.Eigenvectors.Get<double>(j, 1));
                    eigen_val[j] = pca_analysis.Eigenvalues.Get<double>(0, j);
                }
                DrawCircle(dst3, cntr, task.DotSize + 1, Scalar.BlueViolet);
                float factor = 0.02f;
                cv.Point ept1 = new cv.Point(cntr.X + (int)(factor * eigen_vecs[0].X * eigen_val[0]), cntr.Y + (int)(factor * eigen_vecs[0].Y * eigen_val[0]));
                cv.Point ept2 = new cv.Point(cntr.X - (int)(factor * eigen_vecs[1].X * eigen_val[1]), cntr.Y - (int)(factor * eigen_vecs[1].Y * eigen_val[1]));
                drawAxis(dst3, cntr, ept1, Scalar.Red, 1);
                drawAxis(dst3, cntr, ept2, Scalar.BlueViolet, 5);
            }
        }
    }
    public class CS_PCA_NColor : CS_Parent
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct paletteEntry
        {
            public int start;
            public int nCount;
            public byte red;
            public byte green;
            public byte blue;
            public double ErrorVal;
        }
        public double CDiff(byte[] a, int start, byte[] b, int startPal)
        {
            return (a[start + 0] - b[startPal + 0]) * (a[start + 0] - b[startPal + 0]) * 5 +
                   (a[start + 1] - b[startPal + 1]) * (a[start + 1] - b[startPal + 1]) * 8 +
                   (a[start + 2] - b[startPal + 2]) * (a[start + 2] - b[startPal + 2]) * 2;
        }
        public byte[] RgbToIndex(byte[] rgb, int width, int height, byte[] pal, int nColor)
        {
            byte[] answer = new byte[width * height];
            for (int i = 0; i < width * height; i++)
            {
                double best = CDiff(rgb, i * 3, pal, 0);
                int bestii = 0;
                for (int ii = 1; ii < nColor; ii++)
                {
                    double nextError = CDiff(rgb, i * 3, pal, ii * 3);
                    if (nextError < best)
                    {
                        best = nextError;
                        bestii = ii;
                    }
                }
                answer[i] = (byte)bestii;
            }
            return answer;
        }
        public byte[] MakePalette(byte[] rgb, int width, int height, int nColors)
        {
            byte[] buff = new byte[width * height * 3];
            paletteEntry[] entry = new paletteEntry[nColors];
            double best;
            int bestii;
            int i, ii;
            byte[] pal = new byte[256 * 3];
            Array.Copy(rgb, buff, width * height * 3);
            entry[0].start = 0;
            entry[0].nCount = width * height;
            CalcError(ref entry[0], ref buff);
            for (i = 1; i < nColors; i++)
            {
                best = entry[0].ErrorVal;
                bestii = 0;
                for (ii = 0; ii < i; ii++)
                {
                    if (entry[ii].ErrorVal > best)
                    {
                        best = entry[ii].ErrorVal;
                        bestii = ii;
                    }
                }
                SplitPCA(ref entry[bestii], ref entry[i], ref buff);
            }
            for (i = 0; i < nColors; i++)
            {
                pal[i * 3] = entry[i].red;
                pal[i * 3 + 1] = entry[i].green;
                pal[i * 3 + 2] = entry[i].blue;
            }
            return pal;
        }
        public void CalcError(ref paletteEntry entry, ref byte[] buff)
        {
            entry.red = (byte)MeanColor(buff, entry.start * 3, entry.nCount, 0);
            entry.green = (byte)MeanColor(buff, entry.start * 3, entry.nCount, 1);
            entry.blue = (byte)MeanColor(buff, entry.start * 3, entry.nCount, 2);
            entry.ErrorVal = 0;
            for (int i = 0; i < entry.nCount; i++)
            {
                entry.ErrorVal += Math.Abs(buff[(entry.start + i) * 3] - entry.red);
                entry.ErrorVal += Math.Abs(buff[(entry.start + i) * 3 + 1] - entry.green);
                entry.ErrorVal += Math.Abs(buff[(entry.start + i) * 3 + 2] - entry.blue);
            }
        }
        public double MeanColor(byte[] rgb, int start, int nnCount, int index)
        {
            if (nnCount == 0) return 0;
            double answer = 0;
            for (int i = 0; i < nnCount; i++)
            {
                answer += rgb[start + i * 3 + index];
            }
            return answer / nnCount;
        }
        public void PCA(ref double[] ret, byte[] pixels, int start, int nnCount)
        {
            double[,] cov = new double[3, 3];
            double[] mu = new double[3];
            int i, j, k;
            double var;
            double[] d = new double[3];
            double[,] v = new double[3, 3];
            for (i = 0; i < 3; i++)
            {
                mu[i] = MeanColor(pixels, start, nnCount, i);
            }
            for (i = 0; i < 3; i++)
            {
                for (j = 0; j <= i; j++)
                {
                    var = 0;
                    for (k = 0; k < nnCount; k++)
                    {
                        var += (pixels[start + k * 3 + i] - mu[i]) * (pixels[start + k * 3 + j] - mu[j]);
                    }
                    cov[i, j] = var / nnCount;
                    cov[j, i] = var / nnCount;
                }
            }
            EigenDecomposition(cov, ref v, ref d);
            ret[0] = v[0, 2];
            ret[1] = v[1, 2];
            ret[2] = v[2, 2];
        }
        public int Project(byte[] rgb, int start, double[] comp)
        {
            return (int)(rgb[start] * comp[0] + rgb[start + 1] * comp[1] + rgb[start + 2] * comp[2]);
        }
        public void SplitPCA(ref paletteEntry entry, ref paletteEntry split, ref byte[] buff)
        {
            int low = 0;
            int high = entry.nCount - 1;
            int cut;
            double[] comp = new double[3];
            byte temp;
            int i;
            PCA(ref comp, buff, (entry.start * 3), entry.nCount);
            cut = GetOtsuThreshold2(buff, (entry.start * 3), entry.nCount, comp);
            while (low < high)
            {
                while (low < high && Project(buff, ((entry.start + low) * 3), comp) < cut)
                {
                    low += 1;
                }
                while (low < high && Project(buff, ((entry.start + high) * 3), comp) >= cut)
                {
                    high -= 1;
                }
                if (low < high)
                {
                    for (i = 0; i < 3; i++)
                    {
                        temp = buff[(entry.start + low) * 3 + i];
                        buff[(entry.start + low) * 3 + i] = buff[(entry.start + high) * 3 + i];
                        buff[(entry.start + high) * 3 + i] = temp;
                    }
                }
                low += 1;
                high -= 1;
            }
            split.start = entry.start + low;
            split.nCount = entry.nCount - low;
            entry.nCount = low;
            CalcError(ref entry, ref buff);
            CalcError(ref split, ref buff);
        }
        public int GetOtsuThreshold2(byte[] rgb, int start, int N, double[] remap)
        {
            int[] hist = new int[1024];
            int wB = 0;
            int wF;
            float mB, mF;
            float sum = 0;
            float sumB = 0;
            float varBetween;
            float varMax = 0.0F;
            int answer = 0;
            for (int i = 0; i < N; i++)
            {
                int nc = (int)(rgb[start + i * 3] * remap[0] + rgb[start + i * 3 + 1] * remap[1] + rgb[start + i * 3 + 2] * remap[2]);
                hist[512 + nc] += 1;
            }
            for (int k = 0; k < 1024; k++)
            {
                sum += k * hist[k];
            }
            for (int k = 0; k < 1024; k++)
            {
                wB += hist[k];
                if (wB == 0)
                {
                    continue;
                }
                wF = N - wB;
                if (wF == 0)
                {
                    break;
                }
                sumB += k * hist[k];
                mB = sumB / wB;
                mF = (sum - sumB) / wF;
                varBetween = wB * wF * (mB - mF) * (mB - mF);
                if (varBetween > varMax)
                {
                    varMax = varBetween;
                    answer = k;
                }
            }
            return answer - 512;
        }
        public void EigenDecomposition(double[,] A, ref double[,] V, ref double[] d)
        {
            int bufLen = A.GetLength(0);
            double[] e = new double[bufLen];
            for (int i = 0; i < bufLen; i++)
            {
                for (int j = 0; j < bufLen; j++)
                {
                    V[i, j] = A[i, j];
                }
            }
            Tred2(ref V, ref d, ref e);
            Tql2(ref V, ref d, ref e);
        }
        public void Tred2(ref double[,] V, ref double[] d, ref double[] e)
        {
            int dLen = d.Length;
            int i, j, k;
            for (j = 0; j < dLen; j++)
            {
                d[j] = V[dLen - 1, j];
            }
            for (i = dLen - 1; i > 0; i--)
            {
                double scale = 0.0;
                double h = 0.0;
                for (k = 0; k < i; k++)
                {
                    scale += Math.Abs(d[k]);
                }
                if (scale == 0.0)
                {
                    e[i] = d[i - 1];
                    for (j = 0; j < i; j++)
                    {
                        d[j] = V[i - 1, j];
                        V[i, j] = 0.0;
                        V[j, i] = 0.0;
                    }
                }
                else
                {
                    double f, g;
                    double hh;
                    for (k = 0; k < i; k++)
                    {
                        d[k] /= scale;
                        h += d[k] * d[k];
                    }
                    f = d[i - 1];
                    g = Math.Sqrt(h);
                    if (f > 0)
                    {
                        g = -g;
                    }
                    e[i] = scale * g;
                    h = h - f * g;
                    d[i - 1] = f - g;
                    for (j = 0; j < i; j++)
                    {
                        e[j] = 0.0;
                    }
                    for (j = 0; j < i; j++)
                    {
                        f = d[j];
                        V[j, i] = f;
                        g = e[j] + V[j, j] * f;
                        for (k = j + 1; k < i; k++)
                        {
                            g += V[k, j] * d[k];
                            e[k] += V[k, j] * f;
                        }
                        e[j] = g;
                    }
                    f = 0.0;
                    for (j = 0; j < i; j++)
                    {
                        e[j] /= h;
                        f += e[j] * d[j];
                    }
                    hh = f / (h + h);
                    for (j = 0; j < i; j++)
                    {
                        e[j] -= hh * d[j];
                    }
                    for (j = 0; j < i; j++)
                    {
                        f = d[j];
                        g = e[j];
                        for (k = j; k < i; k++)
                        {
                            V[k, j] -= (f * e[k] + g * d[k]);
                        }
                        d[j] = V[i - 1, j];
                        V[i, j] = 0.0;
                    }
                }
                d[i] = h;
            }
            for (i = 0; i < dLen - 1; i++)
            {
                double h = d[i + 1];
                V[dLen - 1, i] = V[i, i];
                V[i, i] = 1.0;
                if (h != 0.0)
                {
                    for (k = 0; k <= i; k++)
                    {
                        d[k] = V[k, i + 1] / h;
                    }
                    for (j = 0; j <= i; j++)
                    {
                        double g = 0.0;
                        for (k = 0; k <= i; k++)
                        {
                            g += V[k, i + 1] * V[k, j];
                        }
                        for (k = 0; k <= i; k++)
                        {
                            V[k, j] -= g * d[k];
                        }
                    }
                }
                for (k = 0; k <= i; k++)
                {
                    V[k, i + 1] = 0.0;
                }
            }
            for (j = 0; j < dLen; j++)
            {
                d[j] = V[dLen - 1, j];
                V[dLen - 1, j] = 0.0;
            }
            V[dLen - 1, dLen - 1] = 1.0;
            e[0] = 0.0;
        }
        public void Tql2(ref double[,] V, ref double[] d, ref double[] e)
        {
            int dLen = d.Length;
            int i, j, k, l;
            double f, tst1, eps;
            for (i = 1; i < dLen; i++)
            {
                e[i - 1] = e[i];
            }
            e[dLen - 1] = 0.0;
            f = 0.0;
            tst1 = 0.0;
            eps = Math.Pow(2.0, -52.0);
            for (l = 0; l < dLen; l++)
            {
                tst1 = Math.Max(tst1, Math.Abs(d[l]) + Math.Abs(e[l]));
                int m = l;
                while (m < dLen)
                {
                    if (Math.Abs(e[m]) <= eps * tst1)
                    {
                        break;
                    }
                    m++;
                }
                if (m > l)
                {
                    int iter = 0;
                    do
                    {
                        double g, p, r;
                        double dl1;
                        double h;
                        double c;
                        double c2;
                        double c3;
                        double el1;
                        double s;
                        double s2;
                        iter++;
                        g = d[l];
                        p = (d[l + 1] - g) / (2.0 * e[l]);
                        r = Hypot(p, 1.0);
                        if (p < 0)
                        {
                            r = -r;
                        }
                        d[l] = e[l] / (p + r);
                        d[l + 1] = e[l] * (p + r);
                        dl1 = d[l + 1];
                        h = g - d[l];
                        for (i = l + 2; i < dLen; i++)
                        {
                            d[i] -= h;
                        }
                        f += h;
                        p = d[m];
                        c = 1.0;
                        c2 = c;
                        c3 = c;
                        el1 = e[l + 1];
                        s = 0.0;
                        s2 = 0.0;
                        for (i = m - 1; i >= l; i--)
                        {
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
                            for (k = 0; k < dLen; k++)
                            {
                                h = V[k, i + 1];
                                V[k, i + 1] = s * V[k, i] + c * h;
                                V[k, i] = c * V[k, i] - s * h;
                            }
                        }
                        p = -s * s2 * c3 * el1 * e[l] / dl1;
                        e[l] = s * p;
                        d[l] = c * p;
                    } while (Math.Abs(e[l]) > eps * tst1);
                }
                d[l] += f;
                e[l] = 0.0;
            }
            for (i = 0; i < dLen - 1; i++)
            {
                int k1 = i;
                double p = d[i];
                for (j = i + 1; j < dLen; j++)
                {
                    if (d[j] < p)
                    {
                        k1 = j;
                        p = d[j];
                    }
                }
                if (k1 != i)
                {
                    d[k1] = d[i];
                    d[i] = p;
                    for (j = 0; j < dLen; j++)
                    {
                        p = V[j, i];
                        V[j, i] = V[j, k1];
                        V[j, k1] = p;
                    }
                }
            }
        }
        public double Hypot(double a, double b)
        {
            return Math.Sqrt(a * a + b * b);
        }
        public Palette_CustomColorMap custom = new Palette_CustomColorMap();
        public Options_PCA_NColor options = new Options_PCA_NColor();
        public byte[] palette = new byte[256 * 3];
        public byte[] rgb;
        public byte[] buff;
        public byte[] answer;
        public CS_PCA_NColor(VBtask task) : base(task)
        {
            rgb = new byte[dst2.Total() * dst2.ElemSize()];
            buff = new byte[rgb.Length];
            answer = new byte[rgb.Length];
            custom.colorMap = new Mat(256, 1, MatType.CV_8UC3);
            desc = "Use PCA to build a palettized CV_8U image from the input using a palette.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Marshal.Copy(src.Data, rgb, 0, rgb.Length);
            Marshal.Copy(src.Data, buff, 0, buff.Length);
            palette = MakePalette(rgb, dst2.Width, dst2.Height, options.desiredNcolors);
            byte[] paletteImage = RgbToIndex(rgb, dst1.Width, dst1.Height, palette, options.desiredNcolors);
            Mat img8u = new Mat(dst2.Size(), MatType.CV_8U, 0);
            Marshal.Copy(paletteImage, 0, img8u.Data, paletteImage.Length);
            Marshal.Copy(palette, 0, custom.colorMap.Data, palette.Length);
            custom.Run(img8u);
            dst2 = custom.dst2;
            Mat tmp = new Mat(256, 1, MatType.CV_8UC3, palette);
            int paletteCount = tmp.CvtColor(ColorConversionCodes.BGR2GRAY).CountNonZero();
            if (standaloneTest())
            {
                task.palette.Run(img8u * 256 / options.desiredNcolors);
                dst3 = task.palette.dst2;
                labels[3] = "dst2 is palettized using global palette option: " + task.gOptions.getPalette();
            }
            labels[2] = "The image above is mapped to " + paletteCount.ToString() + " colors below.  ";
        }
    }
    public class CS_PCA_NColor_CPP : CS_Parent
    {
        Palette_CustomColorMap custom = new Palette_CustomColorMap();
        PCA_Palettize palettize = new PCA_Palettize();
        public byte[] rgb;
        public int classCount;
        public CS_PCA_NColor_CPP(VBtask task) : base(task)
        {
            cPtr = PCA_NColor_Open();
            FindSlider("Desired number of colors").Value = 8;
            UpdateAdvice(traceName + ": Adjust the 'Desired number of colors' between 1 and 256");
            labels = new string[] { "", "", "Palettized (CV_8U) version of color image.", "" };
            desc = "Create a faster version of the PCA_NColor algorithm.";
            rgb = new byte[dst1.Total() * dst1.ElemSize()];
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat) palettize.Run(src); // get the palette in C#
            Marshal.Copy(src.Data, rgb, 0, rgb.Length);
            classCount = palettize.options.desiredNcolors;
            GCHandle handleSrc = GCHandle.Alloc(rgb, GCHandleType.Pinned);
            GCHandle handlePalette = GCHandle.Alloc(palettize.palette, GCHandleType.Pinned);
            IntPtr imagePtr = PCA_NColor_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), handlePalette.AddrOfPinnedObject(), src.Rows, src.Cols, classCount);
            handlePalette.Free();
            handleSrc.Free();
            dst2 = new Mat(dst2.Height, dst2.Width, MatType.CV_8U, imagePtr);
            custom.colorMap = new Mat(256, 1, MatType.CV_8UC3, palettize.palette);
            custom.Run(dst2);
            dst3 = custom.dst2;
            labels[2] = "The CV_8U image is below.  Values range from 0 to " + classCount.ToString();
            labels[3] = "The upper left image is mapped to " + classCount.ToString() + " colors below.";
        }
        public void Close()
        {
            PCA_NColor_Close(cPtr);
        }
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PCA_NColor_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void PCA_NColor_Close(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PCA_NColor_RunCPP(IntPtr cPtr, IntPtr imagePtr, IntPtr palettePtr, int rows, int cols, int desiredNcolors);
    }
    public class CS_PCA_NColorPalettize : CS_Parent
    {
        Palette_CustomColorMap custom = new Palette_CustomColorMap();
        PCA_Palettize palettize = new PCA_Palettize();
        public byte[] answer;
        CS_PCA_NColor nColor;
        public byte[] rgb;
        public CS_PCA_NColorPalettize(VBtask task) : base(task)
        {
            nColor = new CS_PCA_NColor(task);
            FindSlider("Desired number of colors").Value = 8;
            desc = "Create a faster version of the PCA_NColor algorithm.";
            answer = new byte[dst2.Width * dst2.Height];
            rgb = new byte[dst1.Total() * dst1.ElemSize()];
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat) palettize.Run(src); // get the palette in C# which is very fast.
            Marshal.Copy(src.Data, rgb, 0, rgb.Length);
            var paletteImage = nColor.RgbToIndex(rgb, dst1.Width, dst1.Height, palettize.palette, palettize.options.desiredNcolors);
            Mat img8u = new Mat(dst2.Size(), MatType.CV_8U, 0);
            Marshal.Copy(paletteImage, 0, img8u.Data, paletteImage.Length);
            custom.colorMap = new Mat(256, 1, MatType.CV_8UC3, palettize.palette);
            custom.Run(img8u);
            dst2 = custom.dst2;
        }
    }
    public class CS_Pendulum_Basics : CS_Parent
    {
        float l1 = 150, l2 = 150, m1 = 10, m2 = 10;
        float o1 = (float)(2 * Cv2.PI / 2);
        float o2 = (float)(2 * Cv2.PI / 3);
        float w1, w2;
        float g = 9.81f;
        float dw = 2, dh = 4;
        Point2f center;
        float fps = 300;
        Options_Pendulum options = new Options_Pendulum();
    public CS_Pendulum_Basics(VBtask task) : base(task)
        {
            center = new Point2f(dst2.Width / 2, 0);
            labels = new string[] { "", "", "A double pendulum representation", "Trace of the pendulum end points (p1 and p2)" };
            desc = "Build a double pendulum";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            float accumulator = 0;
            if (task.frameCount % 1000 == 0 || task.optionsChanged)
            {
                dst2.SetTo(0);
                dst3.SetTo(0);
            }
            if (options.initialize)
            {
                l1 = msRNG.Next(50, 300);
                l2 = msRNG.Next(50, 300);
                dw = msRNG.Next(2, 4);
                dh = 2 * dw;
            }
            fps = options.fps;
            float dt = 1 / fps;
            float alfa1 = (float)((-g * (2 * m1 + m2) * Math.Sin(o1) - g * m2 * Math.Sin(o1 - 2 * o2) - 2 * m2 * Math.Sin(o1 - o2) * (w2 * w2 * l2 + w1 * w1 * l1 * Math.Cos(o1 - o2))) / (l1 * (2 * m1 + m2 - m2 * Math.Cos(2 * o1 - 2 * o2))));
            float alfa2 = (float)((2 * Math.Sin(o1 - o2)) * (w1 * w1 * l1 * (m1 + m2) + g * (m1 + m2) * Math.Cos(o1) + w2 * w2 * l2 * m2 * Math.Cos(o1 - o2)) / l2 / (2 * m1 + m2 - m2 * Math.Cos(2 * o1 - 2 * o2)));
            w1 += 10 * dt * alfa1;
            w2 += 10 * dt * alfa2;
            o1 += 10 * dt * w1;
            o2 += 10 * dt * w2;
            accumulator += dt;
            Point2f p1 = new Point2f((float)(dst2.Width / 2 + Math.Sin(o1) * l1 + dw * 0.5f) / dw, 
                                     (float)(dst2.Height - (Math.Cos(o1) * l1 + dh * 0.5f) / dh + dst2.Height / dh / 2));
            // adjust to fit in the image better
            p1 = new Point2f(p1.X * 2, p1.Y * 0.5f);
            Point2f p2 = new Point2f((float)(p1.X + (Math.Sin(o2) * l2 + dw * 0.5f) / dw), 
                                     (float)(p1.Y - (Math.Cos(o2) * l2 + dh * 0.5f) / dh));
            DrawLine(dst2, center, p1, task.scalarColors[task.frameCount % 255]);
            DrawLine(dst2, p1, p2, task.scalarColors[task.frameCount % 255]);
            DrawCircle(dst3, p1, task.DotSize, task.scalarColors[task.frameCount % 255]);
            DrawCircle(dst3, p2, task.DotSize, task.scalarColors[task.frameCount % 255]);
        }
    }

    public class CS_PhaseCorrelate_Basics : CS_Parent
    {
        Mat hanning = new Mat();
        public cv.Rect stableRect;
        public cv.Rect srcRect;
        public cv.Point center;
        public float radius;
        public Point2d shift;
        public Mat lastFrame;
        public double response;
        public bool resetLastFrame;
        Options_PhaseCorrelate options = new Options_PhaseCorrelate();  
        public CS_PhaseCorrelate_Basics(VBtask task) : base(task)
        {
            Cv2.CreateHanningWindow(hanning, dst2.Size(), MatType.CV_64F);
            desc = "Look for a shift between the current frame and the previous";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            Mat input = src;
            if (input.Channels() != 1) input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            Mat input64 = new Mat();
            input.ConvertTo(input64, MatType.CV_64F);
            if (lastFrame == null) lastFrame = input64.Clone();
            shift = Cv2.PhaseCorrelate(lastFrame, input64, hanning, out response);
            if (double.IsNaN(response))
            {
                SetTrueText("CS_PhaseCorrelate_Basics has detected NaN's in the input image.", 3);
            }
            else
            {
                radius = (float)Math.Sqrt(shift.X * shift.X + shift.Y * shift.Y);
                resetLastFrame = false;
                if (options.shiftThreshold < radius) resetLastFrame = true;
                int x1 = shift.X < 0 ? Math.Abs((int)shift.X) : 0;
                int y1 = shift.Y < 0 ? Math.Abs((int)shift.Y) : 0;
                stableRect = new cv.Rect(x1, y1, src.Width - Math.Abs((int)shift.X), src.Height - Math.Abs((int)shift.Y));
                stableRect = ValidateRect(stableRect);
                if (stableRect.Width > 0 && stableRect.Height > 0)
                {
                    int x2 = shift.X < 0 ? 0 : (int)shift.X;
                    int y2 = shift.Y < 0 ? 0 : (int)shift.Y;
                    srcRect = ValidateRect(new cv.Rect(x2, y2, stableRect.Width, stableRect.Height));
                    center = new cv.Point(input64.Cols / 2, input64.Rows / 2);
                    if (src.Channels() == 1) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
                    dst2 = src.Clone();
                    DrawCircle(dst2, center, (int)radius, Scalar.Yellow, task.lineWidth + 2);
                    DrawLine(dst2, center, new cv.Point(center.X + shift.X, center.Y + shift.Y), Scalar.Red, task.lineWidth + 1);
                    src[srcRect].CopyTo(dst3[stableRect]);
                    if (radius > 5)
                    {
                        DrawCircle(dst3, center, (int) radius, Scalar.Yellow, task.lineWidth + 2);
                        DrawLine(dst3, center, new cv.Point(center.X + shift.X, center.Y + shift.Y), Scalar.Red, task.lineWidth + 1);
                    }
                }
                else
                {
                    resetLastFrame = true;
                }
            }
            labels[3] = resetLastFrame ? "lastFrame Reset" : "Restored lastFrame";
            if (resetLastFrame) lastFrame = input64;
            labels[2] = "Shift = (" + shift.X.ToString(fmt2) + "," + shift.Y.ToString(fmt2) + ") with radius = " + radius.ToString(fmt2);
        }
    }
    public class CS_PhaseCorrelate_BasicsTest : CS_Parent
    {
        Stabilizer_BasicsRandomInput random = new Stabilizer_BasicsRandomInput();
        PhaseCorrelate_Basics stable = new PhaseCorrelate_Basics();
        public CS_PhaseCorrelate_BasicsTest(VBtask task) : base(task)
        {
            labels[2] = "Unstable input to PhaseCorrelate_Basics";
            labels[3] = "Stabilized output from Phase_Correlate_Basics";
            desc = "Test the PhaseCorrelate_Basics with random movement";
        }
        public void RunCS(Mat src)
        {
            random.Run(src);
            stable.Run(random.dst3.Clone());
            dst2 = stable.dst2;
            dst3 = stable.dst3;
            labels[3] = stable.labels[3];
        }
    }
    public class CS_PhaseCorrelate_Depth : CS_Parent
    {
        PhaseCorrelate_Basics phaseC = new PhaseCorrelate_Basics();
        Mat lastFrame;
        public CS_PhaseCorrelate_Depth(VBtask task) : base(task)
        {
            desc = "Use phase correlation on the depth data";
        }
        public void RunCS(Mat src)
        {
            if (task.FirstPass) lastFrame = task.pcSplit[2].Clone();
            phaseC.Run(task.pcSplit[2]);
            dst2 = task.pcSplit[2];
            Mat tmp = new Mat(dst2.Size(), MatType.CV_32F, 0);
            if (phaseC.resetLastFrame) task.pcSplit[2].CopyTo(lastFrame);
            if (double.IsNaN(phaseC.response))
            {
                SetTrueText("PhaseCorrelate_Basics has detected NaN's in the input image.", 3);
            }
            if (phaseC.srcRect.Width == phaseC.stableRect.Width && phaseC.srcRect.Width != 0)
            {
                lastFrame[phaseC.srcRect].CopyTo(tmp[phaseC.stableRect]);
                labels[2] = phaseC.labels[2];
                labels[3] = phaseC.labels[3];
                tmp = tmp.Normalize(0, 255, NormTypes.MinMax);
                tmp.ConvertTo(dst3, MatType.CV_8UC1);
                DrawCircle(dst3, phaseC.center, (int)phaseC.radius, Scalar.Yellow, task.lineWidth + 2);
                DrawLine(dst3, phaseC.center, new cv.Point(phaseC.center.X + phaseC.shift.X, phaseC.center.Y + phaseC.shift.Y), Scalar.Red, task.lineWidth + 1);
            }
            lastFrame = task.pcSplit[2].Clone();
        }
    }
    public class CS_PhaseCorrelate_HanningWindow : CS_Parent
    {
        public CS_PhaseCorrelate_HanningWindow(VBtask task) : base(task)
        {
            labels[2] = "Looking down on a bell curve in 2 dimensions";
            desc = "Show what a Hanning window looks like";
        }
        public void RunCS(Mat src)
        {
            Cv2.CreateHanningWindow(dst2, src.Size(), MatType.CV_32F);
        }
    }
    public class CS_Photon_Basics : CS_Parent
    {
        Hist_Basics hist = new Hist_Basics();
        Mat lastImage = new cv.Mat();
        public CS_Photon_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Points where B, G, or R differ from the previous image", "Histogram showing distribution of absolute value of differences" };
            desc = "With no motion the camera values will show the random photon differences.  Are they random?";
        }
        public void RunCS(Mat src)
        {
            if (task.FirstPass) lastImage = src;
            Cv2.Absdiff(src, lastImage, dst1);
            dst0 = dst1.Reshape(1, dst1.Rows * 3);
            dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            dst1 = dst1.Threshold(0, 255, ThresholdTypes.Binary);
            if (Cv2.CountNonZero(dst0) > 0)
            {
                dst2 = dst1.Clone();
                hist.Run(dst0);
                dst3 = hist.dst2;
            }
            lastImage = src;
        }
    }
    public class CS_Photon_Test : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        List<int>[] counts = new List<int>[4];
        Mat_4to1 mats = new Mat_4to1();
        public CS_Photon_Test(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            for (int i = 0; i < counts.Length; i++)
            {
                counts[i] = new List<int>();
            }
            labels = new string[] { "", "", "5 color levels from reduction (black not shown)", "Selected distribution" };
            desc = "";
        }
        public void RunCS(Mat src)
        {
            task.redOptions.setSimpleReductionBar(64); // for now...
            int reduce = 64;
            reduction.Run(src);
            dst1 = reduction.dst2;
            int testCount = dst2.Width - 1;
            string strOut = "";
            for (int i = 0; i < counts.Length; i++)
            {
                mats.mat[i] = dst1.InRange(new Scalar(reduce * i), new Scalar(reduce * i));
                counts[i].Add(Cv2.CountNonZero(mats.mat[i]));
                if (counts[i].Count() > testCount) counts[i].RemoveAt(0);
                strOut += "for " + (i * reduce).ToString() + " average = " + counts[i].Average().ToString("###,##0") + " min = " + counts[i].Min().ToString("###,##0.0") + " max = " +
                          counts[i].Max().ToString("###,##0.0") + "\n";
            }
            SetTrueText(strOut, 3);
            mats.Run(empty);
            dst2 = mats.dst2;
            int colWidth = dst2.Width / testCount;
            dst3.SetTo(0);
            for (int i = 0; i < counts[0].Count(); i++)
            {
                int colTop = 0;
                for (int j = 0; j < counts.Length; j++)
                {
                    int h = (int)((dst2.Height - 1) * (counts[j][i] / dst2.Total())); // extra parens to avoid overflow at high res.
                    cv.Rect r = new cv.Rect(colWidth * i, colTop, colWidth, h);
                    if (h > 0)
                    {
                        if (j == 0) dst3[r].SetTo(Scalar.Red);
                        if (j == 1) dst3[r].SetTo(Scalar.LightGreen);
                        if (j == 2) dst3[r].SetTo(Scalar.Blue);
                        if (j == 3) dst3[r].SetTo(Scalar.Yellow);
                    }
                    colTop += h;
                }
            }
        }
    }
    public class CS_Photon_Subtraction : CS_Parent
    {
        Hist_Basics hist = new Hist_Basics();
        Mat lastImage = new cv.Mat();
        public CS_Photon_Subtraction(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Points where B, G, or R differ", "Histogram showing distribution of differences" };
            desc = "Same as Photon_Basics but without ignoring sign.";
        }
        public void RunCS(Mat src)
        {
            src = src.Reshape(1, src.Rows * 3);
            src.ConvertTo(src, MatType.CV_32F);
            if (task.FirstPass) lastImage = src.Clone();
            Mat subOutput = new Mat();
            Cv2.Subtract(src, lastImage, subOutput);
            Mat histInput = subOutput.Add(new Scalar(100)).ToMat();
            hist.Run(histInput);
            dst2 = hist.dst2;
            subOutput = subOutput.Reshape(3, dst2.Height);
            dst1 = subOutput.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, ThresholdTypes.Binary);
            if (Cv2.CountNonZero(dst1) > 0) dst3 = dst1.Clone(); // occasionally the image returned is identical to the last.  hmmm...
            lastImage = src.Clone();
        }
    }
    public class CS_Plane_Basics : CS_Parent
    {
        History_Basics frames = new History_Basics();
        public CS_Plane_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "Top down mask after after thresholding heatmap", "Vertical regions", "Horizontal regions" };
            desc = "Find the regions that are mostly vertical and mostly horizontal.";
        }
        public void RunCS(Mat src)
        {
            Mat topHist = new Mat(), sideHist = new Mat(), topBackP = new Mat(), sideBackP = new Mat();
            Cv2.CalcHist(new Mat[] { task.pointCloud }, task.channelsTop, new Mat(), topHist, 2,
                          new int[] { dst2.Height, dst2.Width }, task.rangesTop);
            topHist.Row(0).SetTo(0);
            Cv2.InRange(topHist, task.projectionThreshold, topHist.Total(), dst1);
            dst1.ConvertTo(dst1, MatType.CV_32F);
            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsTop, dst1, topBackP, task.rangesTop);
            frames.Run(topBackP);
            frames.dst2.ConvertTo(dst2, MatType.CV_8U);
            dst3 = ~dst2;
            dst3.SetTo(0, task.noDepthMask);
        }
    }
    public class CS_Plane_From3Points : CS_Parent
    {
        public Point3f[] input = new Point3f[3];
        public bool showWork = true;
        public Point3f cross;
        public float k;
        public CS_Plane_From3Points(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Plane Equation", "" };
            input = new Point3f[] { new Point3f(2, 1, -1), new Point3f(0, -2, 0), new Point3f(1, -1, 2) };
            desc = "Build a plane equation from 3 points in 3-dimensional space";
        }
        public string vbFormatEquation(Vec4f eq)
        {
            string s1 = eq.Item1 < 0 ? " - " : " +";
            string s2 = eq.Item2 < 0 ? " - " : " +";
            return (eq.Item0 < 0 ? "-" : " ") + Math.Abs(eq.Item0).ToString(fmt3) + "*x " + s1 +
                   Math.Abs(eq.Item1).ToString(fmt3) + "*y " + s2 +
                   Math.Abs(eq.Item2).ToString(fmt3) + "*z = " +
                   eq.Item3.ToString(fmt3) + "\n";
        }
        public void RunCS(Mat src)
        {
            Point3f v1 = input[1] - input[0];
            Point3f v2 = input[1] - input[2];
            cross = crossProduct(v1, v2);
            k = -cross.X * input[0].X - cross.Y * input[0].Y - cross.Z * input[0].Z;
            strOut = "Input: " + "\n";
            for (int i = 0; i < input.Length; i++)
            {
                strOut += "p" + i + " = " + input[i].X.ToString(fmt3) + ", " + input[i].Y.ToString(fmt3) + ", " + input[i].Z.ToString(fmt3) + "\n";
            }
            strOut += "First " + "\t" + "difference = " + v1.X.ToString(fmt3) + ", " + v1.Y.ToString(fmt3) + ", " + v1.Z.ToString(fmt3) + "\n";
            strOut += "Second " + "\t" + "difference = " + v2.X.ToString(fmt3) + ", " + v2.Y.ToString(fmt3) + ", " + v2.Z.ToString(fmt3) + "\n";
            strOut += "Cross Product = " + cross.X.ToString(fmt3) + ", " + cross.Y.ToString(fmt3) + ", " + cross.Z.ToString(fmt3) + "\n";
            strOut += "k = " + k.ToString() + "\n";
            strOut += vbFormatEquation(new Vec4f(cross.X, cross.Y, cross.Z, k));
            string s1 = cross.Y < 0 ? " - " : " + ";
            string s2 = cross.Z < 0 ? " - " : " + ";
            strOut += "Plane equation: " + cross.X.ToString(fmt3) + "x" + s1 + Math.Abs(cross.Y).ToString(fmt3) + "y" + s2 +
                       Math.Abs(cross.Z).ToString(fmt3) + "z + " + (-k).ToString(fmt3) + "\n";
            if (showWork) SetTrueText(strOut, 2);
        }
    }
    public class CS_Plane_FlatSurfaces : CS_Parent
    {
        AddWeighted_Basics addW = new AddWeighted_Basics();
        Plane_CellColor plane = new Plane_CellColor();
        public CS_Plane_FlatSurfaces(VBtask task) : base(task)
        {
            labels = new string[] { "RedCloud Cell contours", "", "RedCloud cells", "" };
            addW.src2 = dst2.Clone();
            desc = "Find all the cells from a RedCloud_Basics output that are likely to be flat";
        }
        public void RunCS(Mat src)
        {
            plane.Run(src);
            dst2 = plane.dst2;
            if (!task.cameraStable || task.heartBeat) addW.src2.SetTo(0);
            int flatCount = 0;
            foreach (var rc in task.redCells)
            {
                if (rc.depthMean[2] < 1.0) continue; // close objects look like planes.
                double RMSerror = 0;
                int pixelCount = 0;
                for (int y = 0; y < rc.rect.Height; y++)
                {
                    for (int x = 0; x < rc.rect.Width; x++)
                    {
                        byte val = rc.mask.Get<byte>(y, x);
                        if (val > 0)
                        {
                            if (msRNG.Next(100) < 10)
                            {
                                Point3f pt = task.pointCloud[rc.rect].Get<Point3f>(y, x);
                                // a*x + b*y + c*z + k = 0 ---> z = -(k + a*x + b*y) / c
                                double depth = -(rc.eq[0] * pt.X + rc.eq[1] * pt.Y + rc.eq[3]) / rc.eq[2];
                                RMSerror += Math.Abs(pt.Z - depth);
                                pt.Z = (float)depth;
                                pixelCount++;
                            }
                        }
                    }
                }
                if (RMSerror / pixelCount <= plane.options.rmsThreshold)
                {
                    addW.src2[rc.rect].SetTo(Scalar.White, rc.mask);
                    flatCount++;
                }
            }
            addW.Run(task.color);
            dst3 = addW.dst2;
            labels[3] = "There were " + flatCount + " RedCloud Cells with an average RMSerror per pixel less than " + (plane.options.rmsThreshold * 100).ToString(fmt0) + " cm";
        }
    }
    public class CS_Plane_OnlyPlanes : CS_Parent
    {
        public Plane_CellColor plane = new Plane_CellColor();
        public List<cv.Point> contours;
        public CS_Plane_OnlyPlanes(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_32FC3, 0);
            labels = new string[] { "", "", "RedCloud Cells", "gCloud reworked with planes instead of depth data" };
            desc = "Replace the gCloud with planes in every RedCloud cell";
        }
        public void buildCloudPlane(rcData rc)
        {
            for (int y = 0; y < rc.rect.Height; y++)
            {
                for (int x = 0; x < rc.rect.Width; x++)
                {
                    if (rc.mask.Get<byte>(y, x) > 0)
                    {
                        Point3f pt = task.pointCloud[rc.rect].Get<Point3f>(y, x);
                        // a*x + b*y + c*z + k = 0 ---> z = -(k + a*x + b*y) / c
                        pt.Z = -(rc.eq[0] * pt.X + rc.eq[1] * pt.Y + rc.eq[3]) / rc.eq[2];
                        if (rc.minVec.Z <= pt.Z && rc.maxVec.Z >= pt.Z) dst3[rc.rect].Set<Point3f>(y, x, pt);
                    }
                }
            }
        }
        public void RunCS(Mat src)
        {
            plane.Run(src);
            dst2 = plane.dst2;
            dst3.SetTo(0);
            foreach (var rc in task.redCells)
            {
                if (!plane.options.reuseRawDepthData) buildCloudPlane(rc);
            }
            if (plane.options.reuseRawDepthData) dst3 = task.pointCloud;
            var rcX = task.rc;
        }
    }
    public class CS_Plane_EqCorrelation : CS_Parent
    {
        Plane_Points plane = new Plane_Points();
        public List<float> correlations = new List<float>();
        public List<Vec4f> equations = new List<Vec4f>();
        public List<List<cv.Point>> ptList2D = new List<List<cv.Point>>();
        Kalman_Basics kalman = new Kalman_Basics();
        public CS_Plane_EqCorrelation(VBtask task) : base(task)
        {
            desc = "Classify equations based on the correlation of their coefficients";
        }
        public void RunCS(Mat src)
        {
            plane.Run(src);
            dst2 = plane.dst2;
            if (plane.equations.Count() == 0)
            {
                dst0 = src;
                SetTrueText("Select a RedCloud cell to analyze.", 3);
                return;
            }
            equations = new List<Vec4f>(plane.equations);
            ptList2D = new List<List<cv.Point>>(plane.ptList2D);
            correlations.Clear();
            Mat correlationMat = new Mat();
            int[] count = new int[plane.equations.Count()];
            for (int i = 0; i < equations.Count(); i++)
            {
                Vec4f p1 = equations[i];
                Mat data1 = new Mat(4, 1, MatType.CV_32F, new float[] { p1.Item0, p1.Item1, p1.Item2, p1.Item3 });
                for (int j = i + 1; j < equations.Count(); j++)
                {
                    Vec4f p2 = equations[j];
                    Mat data2 = new Mat(4, 1, MatType.CV_32F, new float[] { p2.Item0, p2.Item1, p2.Item2, p2.Item3 });
                    Cv2.MatchTemplate(data1, data2, correlationMat, TemplateMatchModes.CCoeffNormed);
                    float correlation = correlationMat.At<float>(0, 0);
                    correlations.Add(correlation);
                    if (correlation >= 0.999) count[i]++;
                }
            }
            List<int> countList = new List<int>(count);
            int index = countList.IndexOf(countList.Max());
            Vec4f pt = equations[index];
            string s1 = pt.Item1 < 0 ? " - " : " + ";
            string s2 = pt.Item2 < 0 ? " - " : " + ";
            if (count[index] > plane.equations.Count() / 4)
            {
                kalman.kInput = new float[] { pt.Item0, pt.Item1, pt.Item2, pt.Item3 };
                kalman.Run(empty);
                strOut = "Normalized Plane equation: " + string.Format(fmt3, kalman.kOutput[0]) + "x" + s1 + string.Format(fmt3, Math.Abs(kalman.kOutput[1])) + "y" + s2 +
                         string.Format(fmt3, Math.Abs(kalman.kOutput[2])) + "z = " + string.Format(fmt3, -kalman.kOutput[3]) + " with " + count[index] +
                         " closely matching plane equations." + "\n";
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Plane_CellColor : CS_Parent
    {
        public Options_Plane options = new Options_Plane();
        public RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Plane_CellColor(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "RedCloud Cells", "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis" };
            desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal";
        }
        public List<Point3f> buildContourPoints(rcData rc)
        {
            List<Point3f> fitPoints = new List<Point3f>();
            foreach (var pt in rc.contour)
            {
                if (pt.X >= rc.rect.Width || pt.Y >= rc.rect.Height) continue;
                if (rc.mask.At<byte>(pt.Y, pt.X) == 0) continue;
                fitPoints.Add(task.pointCloud[rc.rect].At<Point3f>(pt.Y, pt.X)); // each contour point is guaranteed to be in the mask and have depth.
            }
            return fitPoints;
        }
        public List<Point3f> buildMaskPointEq(rcData rc)
        {
            List<Point3f> fitPoints = new List<Point3f>();
            for (int y = 0; y < rc.rect.Height; y++)
            {
                for (int x = 0; x < rc.rect.Width; x++)
                {
                    if (rc.mask.At<byte>(y, x) != 0) fitPoints.Add(task.pointCloud[rc.rect].At<Point3f>(y, x));
                }
            }
            return fitPoints;
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            redC.Run(src);
            dst2 = redC.dst2;
            dst3.SetTo(0);
            List<rcData> newCells = new List<rcData>();
            var rcX = task.rc;
            foreach (var rc in task.redCells)
            {
                rc.eq = new Vec4f();
                if (options.useMaskPoints)
                {
                    rc.eq = fitDepthPlane(buildMaskPointEq(rc));
                }
                else if (options.useContourPoints)
                {
                    rc.eq = fitDepthPlane(buildContourPoints(rc));
                }
                else if (options.use3Points)
                {
                    rc.eq = build3PointEquation(rc);
                }
                newCells.Add(rc);
                dst3[rc.rect].SetTo(new Scalar(Math.Abs(255 * rc.eq.Item0),
                                                Math.Abs(255 * rc.eq.Item1),
                                                Math.Abs(255 * rc.eq.Item2)), rc.mask);
            }
            task.redCells = new List<rcData>(newCells);
        }
    }
    public class CS_Plane_Points : CS_Parent
    {
        Plane_From3Points plane = new Plane_From3Points();
        public List<Vec4f> equations = new List<Vec4f>();
        public List<Point3f> ptList = new List<Point3f>();
        public List<List<cv.Point>> ptList2D = new List<List<cv.Point>>();
        RedCloud_Basics redC = new RedCloud_Basics();
        bool needOutput = false;
        public CS_Plane_Points(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "RedCloud Basics output - click to highlight a cell", "" };
            desc = "Detect if a some or all points in a RedCloud cell are in a plane.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            var rc = task.rc;
            labels[2] = "Selected cell has " + rc.contour.Count() + " points.";
            // this contour will have more depth data behind it.  Simplified contours will lose lots of depth data.
            rc.contour = contourBuild(rc.mask, ContourApproximationModes.ApproxNone);
            Point3f pt;
            List<cv.Point> list2D = new List<cv.Point>();
            ptList.Clear();
            for (int i = 0; i < rc.contour.Count(); i++)
            {
                pt = task.pointCloud.Get<Point3f>(rc.contour[i].Y, rc.contour[i].X);
                if (pt.Z > 0)
                {
                    ptList.Add(pt);
                    list2D.Add(rc.contour[i]);
                    if (ptList.Count() > 100) break;
                }
            }
            if (task.heartBeat || needOutput)
            {
                ptList2D.Clear();
                equations.Clear();
                needOutput = false;
                strOut = "";
                if (ptList.Count() < 3)
                {
                    needOutput = true;
                    strOut = "There weren't enough points in that cell contour with depth.  Select another cell.";
                }
                else
                {
                    int c = ptList.Count();
                    for (int i = 0; i < ptList.Count(); i++)
                    {
                        List<cv.Point> list2Dinput = new List<cv.Point>();
                        for (int j = 0; j < 3; j++)
                        {
                            int ptIndex = i;
                            if (j == 1) ptIndex = (i + c / 3) % c;
                            if (j == 2) ptIndex = (i + 2 * c / 3) % c;
                            plane.input[j] = ptList[ptIndex];
                            list2Dinput.Add(list2D[ptIndex]);
                        }
                        plane.Run(empty);
                        strOut += plane.vbFormatEquation(new Vec4f(plane.cross.X, plane.cross.Y, plane.cross.Z, plane.k));
                        equations.Add(new Vec4f(plane.cross.X, plane.cross.Y, plane.cross.Z, plane.k));
                        ptList2D.Add(list2Dinput);
                    }
                }
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Plane_Histogram : CS_Parent
    {
        PointCloud_Solo solo = new PointCloud_Solo();
        Hist_Basics hist = new Hist_Basics();
        public double peakCeiling;
        public double peakFloor;
        public double ceilingPop;
        public double floorPop;
        public CS_Plane_Histogram(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Histogram of Y-Values of the point cloud after masking", "Mask used to isolate histogram input" };
            desc = "Create a histogram plot of the Y-values in the backprojection of solo points.";
        }
        public void RunCS(Mat src)
        {
            solo.Run(src);
            dst3 = solo.dst3;
            Mat points = dst3.FindNonZero();
            List<float> yList = new List<float>();
            for (int i = 0; i < points.Rows; i++)
            {
                cv.Point pt = points.At<cv.Point>(i, 0);
                float yVal = task.pcSplit[1].At<float>(pt.Y, pt.X);
                if (yVal != 0) yList.Add(yVal);
            }
            if (yList.Count() == 0) return;
            hist.mm.minVal = yList.Min();
            hist.mm.maxVal = yList.Max();
            hist.Run(new Mat(yList.Count(), 1, MatType.CV_32F, yList.ToArray()));
            dst2 = hist.dst2;
            double binWidth = dst2.Width / task.histogramBins;
            double rangePerBin = (hist.mm.maxVal - hist.mm.minVal) / task.histogramBins;
            int midHist = task.histogramBins / 2;
            mmData mm = GetMinMax(hist.histogram[new cv.Rect(0, midHist, 1, midHist)]);
            floorPop = mm.maxVal;
            double peak = hist.mm.minVal + (midHist + mm.maxLoc.Y + 1) * rangePerBin;
            int rX = (midHist + mm.maxLoc.Y) * (int)binWidth;
            dst2.Rectangle(new cv.Rect(rX, 0, (int)binWidth, dst2.Height), Scalar.Black, task.lineWidth);
            if (Math.Abs(peak - peakCeiling) > rangePerBin) peakCeiling = peak;
            mm = GetMinMax(hist.histogram[new cv.Rect(0, 0, 1, midHist)]);
            ceilingPop = mm.maxVal;
            peak = hist.mm.minVal + (mm.maxLoc.Y + 1) * rangePerBin;
            rX = mm.maxLoc.Y * (int)binWidth;
            dst2.Rectangle(new cv.Rect(rX, 0, (int)binWidth, dst2.Height), Scalar.Yellow, task.lineWidth);
            if (Math.Abs(peak - peakFloor) > rangePerBin * 2) peakFloor = peak;
            labels[3] = "Peak Ceiling = " + string.Format(fmt3, peakCeiling) + " and Peak Floor = " + string.Format(fmt3, peakFloor);
            SetTrueText("Yellow rectangle is likely floor and black is likely ceiling.");
        }
    }
    public class CS_Plane_Equation : CS_Parent
    {
        public rcData rc = new rcData();
        public string justEquation;
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Plane_Equation(VBtask task) : base(task)
        {
            desc = "Compute the coefficients for an estimated plane equation given the rc contour";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                redC.Run(src);
                dst2 = redC.dst2;
                rc = task.rc;
                if (rc.index == 0) SetTrueText("Select a cell in the image at left.");
            }
            int offset = (int)(rc.contour.Count() / 4) - 1;
            List<float> xList = new List<float>();
            List<float> yList = new List<float>();
            List<float> zList = new List<float>();
            List<float> kList = new List<float>();
            List<float> dotlist = new List<float>();
            for (int j = 0; j <= offset - 1; j++)
            {
                var p1 = rc.contour[j + offset * 0];
                var p2 = rc.contour[j + offset * 1];
                var p3 = rc.contour[j + offset * 2];
                var p4 = rc.contour[j + offset * 3];
                var v1 = task.pointCloud[rc.rect].Get<cv.Point3f>(p1.Y, p1.X);
                var v2 = task.pointCloud[rc.rect].Get<cv.Point3f>(p2.Y, p2.X);
                var v3 = task.pointCloud[rc.rect].Get<cv.Point3f>(p3.Y, p3.X);
                var v4 = task.pointCloud[rc.rect].Get<cv.Point3f>(p4.Y, p4.X);
                var cross1 = crossProduct(v1 - v2, v2 - v3);
                var cross2 = crossProduct(v1 - v4, v4 - v3);
                float dot = dotProduct3D(cross1, cross2);
                dotlist.Add(dot);
                float k = -cross1.X * v1.X - cross1.Y * v1.Y - cross1.Z * v1.Z;
                xList.Add(cross1.X);
                yList.Add(cross1.Y);
                zList.Add(cross1.Z);
                kList.Add(k);
            }
            if (dotlist.Count() > 0)
            {
                int dotIndex = dotlist.IndexOf(dotlist.Max());
                rc.eq = new Vec4f(xList[dotIndex], yList[dotIndex], zList[dotIndex], kList[dotIndex]);
            }
            if (dotlist.Count() > 0)
            {
                if (task.heartBeat)
                {
                    justEquation = string.Format("{0}*X + {1}*Y + {2}*Z + {3}\n",
                        rc.eq.Item0.ToString("F3"), rc.eq.Item1.ToString("F3"),
                        rc.eq.Item2.ToString("F3"), rc.eq.Item3.ToString("F3"));
                    if (xList.Count() > 0)
                    {
                        strOut = "The rc.contour has " + rc.contour.Count() + " points\n";
                        strOut += "Estimated 3D plane equation:\n";
                        strOut += justEquation + "\n";
                    }
                    else
                    {
                        if (!strOut.Contains("Insufficient points"))
                        {
                            strOut += "\nInsufficient points or best dot product too low at " + dotlist.Max().ToString("0.00");
                        }
                    }
                    strOut += xList.Count() + " 3D plane equations were tested with an average dot product = " +
                              dotlist.Average().ToString("0.00");
                }
            }
            if (standaloneTest())
            {
                SetTrueText(strOut, 3);
                dst3.SetTo(0);
                DrawContour(dst3[rc.rect], rc.contour, vecToScalar(rc.color), -1);
            }
        }
    }
    public class CS_Plane_Verticals : CS_Parent
    {
        PointCloud_Solo solo = new PointCloud_Solo();
        History_Basics frames = new History_Basics();
        public CS_Plane_Verticals(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] {
            "RGB image with highlights for likely vertical surfaces over X frames.",
            "Heatmap top view", "Single frame backprojection of red areas in the heatmap",
            "Thresholded heatmap top view mask"
        };
            desc = "Use a heatmap to isolate vertical walls - incomplete!";
        }
        public void RunCS(Mat src)
        {
            solo.Run(src);
            dst3 = solo.heat.topframes.dst2.InRange(task.projectionThreshold * task.frameHistoryCount, dst2.Total());
            dst1 = new Mat(dst1.Size(), MatType.CV_32FC1, 0);
            solo.heat.dst0.CopyTo(dst1, dst3);
            dst1.ConvertTo(dst1, MatType.CV_32FC1);
            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsTop, dst1, dst2, task.rangesTop);
            frames.Run(dst2);
            frames.dst2.ConvertTo(dst2, MatType.CV_8U);
            dst2 = frames.dst2.Threshold(0, 255, ThresholdTypes.Binary);
            dst2.ConvertTo(dst0, MatType.CV_8U);
            task.color.SetTo(Scalar.White, dst0);
        }
    }
    public class CS_Plane_Horizontals : CS_Parent
    {
        PointCloud_Solo solo = new PointCloud_Solo();
        History_Basics frames = new History_Basics();
        public CS_Plane_Horizontals(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] {
            "RGB image with highlights for likely floor or ceiling over X frames.",
            "Heatmap side view", "Single frame backprojection areas in the heatmap",
            "Thresholded heatmap side view mask"
        };
            desc = "Use the solo points to isolate horizontal surfaces - floor or ceiling or table tops.";
        }
        public void RunCS(Mat src)
        {
            solo.Run(src);
            dst3 = solo.heat.sideframes.dst2.InRange(task.projectionThreshold * task.frameHistoryCount, dst2.Total());
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            solo.heat.dst1.CopyTo(dst1, dst3);
            dst1.ConvertTo(dst1, MatType.CV_32FC1);
            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsSide, dst1, dst2, task.rangesSide);
            frames.Run(dst2);
            frames.dst2.ConvertTo(dst2, MatType.CV_8U);
            dst2 = frames.dst2.Threshold(0, 255, ThresholdTypes.Binary);
            dst2.ConvertTo(dst0, MatType.CV_8U);
            task.color.SetTo(Scalar.White, dst0);
        }
    }
    public class CS_Plane_FloorStudy : CS_Parent
    {
        public Structured_SliceH slice = new Structured_SliceH();
        List<float> yList = new List<float>();
        public float planeY;
        Options_PlaneFloor options = new Options_PlaneFloor();
        public CS_Plane_FloorStudy(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "", "", "" };
            desc = "Find the floor plane (if present)";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            slice.Run(src);
            dst1 = slice.dst3;
            dst0 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            float thicknessCMs = task.metersPerPixel * 1000 / 100, nextY = 0;
            cv.Rect rect = new cv.Rect();
            for (int y = dst0.Height - 2; y >= 0; y--)
            {
                rect = new cv.Rect(0, y, dst0.Width - 1, 1);
                int count = dst0[rect].CountNonZero();
                if (count > options.countThreshold)
                {
                    nextY = -task.yRange * (task.sideCameraPoint.Y - y) / task.sideCameraPoint.Y - thicknessCMs / 2.5f; // narrow it down to about 1 cm
                    labels[2] = "Y = " + string.Format(fmt3, planeY) + " separates the floor.";
                    SetTrueText(labels[2], 3);
                    Mat sliceMask = task.pcSplit[1].InRange(new Scalar(planeY), new Scalar(3.0));
                    dst2 = src;
                    dst2.SetTo(new Scalar(255, 255, 255), sliceMask);
                    break;
                }
            }
            yList.Add(nextY);
            planeY = yList.Average();
            if (yList.Count() > 20) yList.RemoveAt(0);
            dst1.Line(new cv.Point(0, rect.Y), new cv.Point(dst2.Width, rect.Y), Scalar.Yellow, slice.options.sliceSize, task.lineType);
        }
    }

    public class CS_Plot_Basics : CS_Parent
    {
        CS_Plot_Basics_CPP plot;
        Hist_Graph hist = new Hist_Graph();
        public int plotCount = 3;
        public CS_Plot_Basics(VBtask task) : base(task)
        {
            plot = new CS_Plot_Basics_CPP(task);
            hist.plotRequested = true;
            labels[2] = "Plot of grayscale histogram";
            labels[3] = "Same Data but using OpenCV C++ plot";
            desc = "Plot data provided in src Mat";
        }
        public void RunCS(Mat src)
        {
            hist.plotColors[0] = Scalar.White;
            hist.Run(src);
            dst2 = hist.dst2;
            for (int i = 0; i < hist.histRaw[0].Rows; i++)
            {
                plot.srcX.Add(i);
                plot.srcY.Add(hist.histRaw[0].At<float>(i, 0));
            }
            plot.RunCS(empty);
            dst3 = plot.dst2;
            labels[2] = hist.labels[2];
        }
    }
    public class CS_Plot_Histogram : CS_Parent
    {
        public Mat histogram = new Mat();
        public float[] histArray;
        public float minRange = 0;
        public float maxRange = 255;
        public Scalar backColor = Scalar.Red;
        public float maxValue;
        public float minValue;
        public float plotCenter;
        public float barWidth;
        public bool addLabels = true;
        public bool removeZeroEntry = true;
        public bool createHistogram = false;
        public mmData mm;
        public CS_Plot_Histogram(VBtask task) : base(task)
        {
            desc = "Plot histogram data with a stable scale at the left of the image.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest() || createHistogram)
            {
                if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
                Rangef[] ranges = new Rangef[1] { new Rangef(minRange, maxRange) };
                Cv2.CalcHist(new Mat[] { src }, new int[] { 0 }, new Mat(), histogram, 1, 
                             new int[] { task.histogramBins }, ranges);
            }
            else
            {
                histogram = src;
            }
            if (removeZeroEntry) histogram.Set<float>(0, 0, 0); // let's not plot the values at zero...
            dst2.SetTo(backColor);
            barWidth = dst2.Width / histogram.Rows;
            plotCenter = barWidth * histogram.Rows / 2 + barWidth / 2;
            histArray = new float[histogram.Rows];
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length);
            mm = GetMinMax(histogram);
            if (mm.maxVal > 0 && histogram.Rows > 0)
            {
                float incr = 255f / histogram.Rows;
                for (int i = 0; i < histArray.Length; i++)
                {
                    if (float.IsNaN(histArray[i])) histArray[i] = 0;
                    if (histArray[i] > 0)
                    {
                        int h = (int)(histArray[i] * dst2.Height / mm.maxVal);
                        float sIncr = (i % 256) * incr;
                        Scalar color = new Scalar(sIncr, sIncr, sIncr);
                        if (histogram.Rows > 255) color = Scalar.Black;
                        Cv2.Rectangle(dst2, new cv.Rect((int)(i * barWidth), dst2.Height - h, (int)Math.Max(1, barWidth), h), 
                                      color, -1);
                    }
                }
                if (addLabels) AddPlotScale(dst2, mm.minVal, mm.maxVal);
            }
        }
    }
    public class CS_Plot_Depth : CS_Parent
    {
        Plot_Basics_CPP plotDepth = new Plot_Basics_CPP();
        Hist_Basics hist = new Hist_Basics();
        public CS_Plot_Depth(VBtask task) : base(task)
        {
            desc = "Show depth using OpenCV's plot format with variable bins.";
        }
        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32F) src = task.pcSplit[2];
            hist.Run(src);
            plotDepth.srcX.Clear();
            plotDepth.srcY.Clear();
            for (int i = 0; i < task.histogramBins; i++)
            {
                plotDepth.srcX.Add(i * task.MaxZmeters / task.histogramBins);
                plotDepth.srcY.Add(hist.histogram.At<float>(i, 0));
            }
            plotDepth.Run(empty);
            dst2 = plotDepth.dst2;
            if (task.heartBeat) labels[2] = plotDepth.labels[2];
            var split = Regex.Split(labels[2], @"\W+");
            int lineCount = int.Parse(split[4]);
            if (lineCount > 0)
            {
                int meterDepth = src.Width / lineCount;
                for (int i = 1; i <= lineCount; i++)
                {
                    int x = i * meterDepth;
                    DrawLine(dst2, new cv.Point(x, 0), new cv.Point(x, src.Height), Scalar.White);
                    SetTrueText($"{i}m", new cv.Point(x + 4, src.Height - 10));
                }
            }
        }
    }
    public class CS_Plot_Histogram2D : CS_Parent
    {
        public Color_Basics colorFmt = new Color_Basics();
        public CS_Plot_Histogram2D(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "2D Histogram", "" };
            desc = "Plot a 2D histogram from the input Mat";
        }
        public void RunCS(Mat src)
        {
            Mat histogram = src.Clone();
            if (standaloneTest())
            {
                colorFmt.Run(src);
                src = colorFmt.dst2;
                int bins = task.histogramBins;
                Cv2.CalcHist(new Mat[] { src }, new int[] { 0, 1 }, new Mat(), histogram, 2, new int[] { bins, bins }, task.redOptions.rangesBGR);
            }
            dst2 = histogram.Resize(dst2.Size(), 0, 0, InterpolationFlags.Nearest);
            if (standaloneTest()) dst3 = dst2.Threshold(0, 255, ThresholdTypes.Binary);
        }
    }
    public class CS_Plot_OverTimeSingle : CS_Parent
    {
        public float plotData;
        public Scalar backColor = Scalar.DarkGray;
        public float max, min, avg;
        public string fmt;
        public Scalar plotColor = Scalar.Blue;
        List<float> inputList = new List<float>();
        public CS_Plot_OverTimeSingle(VBtask task) : base(task)
        {
            labels[2] = "Plot_OverTime ";
            desc = "Plot an input variable over time";
        }
        public void RunCS(Mat src)
        {
            dst2 = dst2.Resize(task.quarterRes);
            if (standaloneTest()) plotData = (float)task.color.Mean(task.depthMask)[0];
            if (inputList.Count() >= dst2.Width) inputList.RemoveAt(0);
            inputList.Add(plotData);
            dst2.ColRange(new Range(0, inputList.Count())).SetTo(backColor);
            max = inputList.Max();
            min = inputList.Min();
            for (int i = 0; i < inputList.Count(); i++)
            {
                float y = 1 - (inputList[i] - min) / (max - min);
                y *= dst2.Height - 1;
                cv.Point c = new cv.Point(i, y);
                if (c.X < 1) c.X = 1;
                DrawCircle(dst2, c, 1, plotColor);
            }
            if (inputList.Count() > dst2.Width / 8)
            {
                float diff = max - min;
                fmt = diff > 10 ? fmt0 : (diff > 2 ? fmt1 : (diff > 0.5 ? fmt2 : fmt3));
                for (int i = 0; i < 3; i++)
                {
                    string nextText = max.ToString(fmt);
                    if (i == 1) nextText = inputList.Average().ToString(fmt);
                    if (i == 2) nextText = min.ToString(fmt);
                    cv.Point pt = new cv.Point(0, 10);
                    if (i == 1) pt = new cv.Point(0, dst2.Height / 2 - 5);
                    if (i == 2) pt = new cv.Point(0, dst2.Height - 3);
                    Cv2.PutText(dst2, nextText, pt, HersheyFonts.HersheyPlain, 0.7, Scalar.White, 1, task.lineType);
                }
            }
            cv.Point p1 = new cv.Point(0, dst2.Height / 2);
            cv.Point p2 = new cv.Point(dst2.Width, dst2.Height / 2);
            dst2.Line(p1, p2, Scalar.White, task.cvFontThickness);
            if (standaloneTest()) SetTrueText("standaloneTest() test is with the blue channel mean of the color image.", 3);
        }
    }
    public class CS_Plot_OverTimeScalar : CS_Parent
    {
        public Scalar plotData;
        public int plotCount = 3;
        public List<Plot_OverTimeSingle> plotList = new List<Plot_OverTimeSingle>();
        Mat_4Click mats = new Mat_4Click();
        public CS_Plot_OverTimeScalar(VBtask task) : base(task)
        {
            for (int i = 0; i < 4; i++)
            {
                plotList.Add(new Plot_OverTimeSingle());
                if (i == 0) plotList[i].plotColor = Scalar.Blue;
                if (i == 1) plotList[i].plotColor = Scalar.Green;
                if (i == 2) plotList[i].plotColor = Scalar.Red;
                if (i == 3) plotList[i].plotColor = Scalar.Yellow;
            }
            desc = "Plot the requested number of entries in the cv.scalar input";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest()) plotData = task.color.Mean();
            for (int i = 0; i < Math.Min(plotCount, 4); i++)
            {
                plotList[i].plotData = (float)plotData[i];
                plotList[i].Run(src);
                mats.mat[i] = plotList[i].dst2;
            }
            mats.Run(empty);
            dst2 = mats.dst2;
            dst3 = mats.dst3;
        }
    }
    public class CS_Plot_OverTime : CS_Parent
    {
        public Scalar plotData;
        public int plotCount = 3;
        public Scalar[] plotColors = { Scalar.Blue, Scalar.LawnGreen, Scalar.Red, Scalar.White };
        public Scalar backColor = Scalar.DarkGray;
        public int minScale = 50;
        public int maxScale = 200;
        public int plotTriggerRescale = 50;
        public int columnIndex;
        public int offChartCount;
        public List<Scalar> lastXdelta = new List<Scalar>();
        public bool controlScale; // Use this to programmatically control the scale (rather than let the automated way below keep the scale.)
        public CS_Plot_OverTime(VBtask task) : base(task)
        {
            desc = "Plot an input variable over time";
            switch (task.WorkingRes.Width)
            {
                case 1920:
                    task.gOptions.setLineWidth(10);
                    break;
                case 1280:
                    task.gOptions.setLineWidth(7);
                    break;
                case 640:
                    task.gOptions.setLineWidth(4);
                    break;
                case 320:
                    task.gOptions.setLineWidth(2);
                    break;
                default:
                    task.gOptions.setLineWidth(1);
                    break;
            }
            task.gOptions.SetDotSize(task.lineWidth);
        }
        public void RunCS(Mat src)
        {
            const int plotSeriesCount = 100;
            lastXdelta.Add(plotData);
            if (columnIndex + task.DotSize >= dst2.Width)
            {
                dst2.ColRange(columnIndex, dst2.Width).SetTo(backColor);
                columnIndex = 1;
            }
            dst2.ColRange(columnIndex, columnIndex + task.DotSize).SetTo(backColor);
            if (standaloneTest()) plotData = task.color.Mean();
            for (int i = 0; i < plotCount; i++)
            {
                if (Math.Floor(plotData[i]) < minScale || Math.Ceiling(plotData[i]) > maxScale)
                {
                    offChartCount++;
                    break;
                }
            }
            // if enough points are off the charted area or if manually requested, then redo the scale.
            if (offChartCount > plotTriggerRescale && lastXdelta.Count() >= plotSeriesCount && !controlScale)
            {
                if (!task.FirstPass)
                {
                    maxScale = int.MinValue;
                    minScale = int.MaxValue;
                    for (int i = 0; i < lastXdelta.Count(); i++)
                    {
                        var nextVal = lastXdelta[i];
                        for (int j = 0; j < plotCount; j++)
                        {
                            if (Math.Floor(nextVal[j]) < minScale) minScale = (int)Math.Floor(nextVal[j]);
                            if (Math.Floor(nextVal[j]) > maxScale) maxScale = (int)Math.Ceiling(nextVal[j]);
                        }
                    }
                }
                lastXdelta.Clear();
                offChartCount = 0;
                columnIndex = 1; // restart at the left side of the chart
            }
            if (lastXdelta.Count() >= plotSeriesCount) lastXdelta.RemoveAt(0);
            for (int i = 0; i < plotCount; i++)
            {
                var y = 1 - (plotData[i] - minScale) / (maxScale - minScale);
                y *= dst2.Height - 1;
                var c = new cv.Point(columnIndex - task.DotSize, y - task.DotSize);
                if (c.X < 1) c.X = 1;
                DrawCircle(dst2, c, task.DotSize, plotColors[i]);
            }
            if (task.heartBeat)
            {
                dst2.Line(new cv.Point(columnIndex, 0), new cv.Point(columnIndex, dst2.Height), Scalar.White, 1);
            }
            columnIndex += task.DotSize;
            dst2.Col(columnIndex).SetTo(0);
            if (standaloneTest()) labels[2] = "RGB Means: blue = " + string.Format(fmt1, plotData[0]) + " green = " + string.Format(fmt1, plotData[1]) + " red = " + string.Format(fmt1, plotData[2]);
            var lineCount = (int)(maxScale - minScale - 1);
            if (lineCount > 3 || lineCount < 0) lineCount = 3;
            AddPlotScale(dst2, minScale, maxScale, lineCount);
        }
    }
    public class CS_Plot_OverTimeFixedScale : CS_Parent
    {
        public Scalar plotData;
        public int plotCount = 3;
        public Scalar[] plotColors = { Scalar.Blue, Scalar.Green, Scalar.Red, Scalar.White };
        public Scalar backColor = Scalar.DarkGray;
        public int minScale = 50;
        public int maxScale = 200;
        public int plotTriggerRescale = 50;
        public int columnIndex;
        public int offChartCount;
        public List<Scalar> lastXdelta = new List<Scalar>();
        public bool controlScale; // Use this to programmatically control the scale (rather than let the automated way below keep the scale.)
        public bool showScale = true;
        public bool fixedScale;
        Mat plotOutput;
        public CS_Plot_OverTimeFixedScale(VBtask task) : base(task)
        {
            plotOutput = new Mat(new cv.Size(320, 180), MatType.CV_8UC3, 0);
            desc = "Plot an input variable over time";
            task.gOptions.setLineWidth(1);
            task.gOptions.SetDotSize(2);
        }
        public void RunCS(Mat src)
        {
            const int plotSeriesCount = 100;
            lastXdelta.Add(plotData);
            if (columnIndex + task.DotSize >= plotOutput.Width)
            {
                plotOutput.ColRange(columnIndex, plotOutput.Width).SetTo(backColor);
                columnIndex = 1;
            }
            plotOutput.ColRange(columnIndex, columnIndex + task.DotSize).SetTo(backColor);
            if (standaloneTest()) plotData = task.color.Mean();
            for (int i = 0; i < plotCount; i++)
            {
                if (Math.Floor(plotData[i]) < minScale || Math.Ceiling(plotData[i]) > maxScale)
                {
                    offChartCount++;
                    break;
                }
            }
            if (!fixedScale)
            {
                // if enough points are off the charted area or if manually requested, then redo the scale.
                if (offChartCount > plotTriggerRescale && lastXdelta.Count() >= plotSeriesCount && !controlScale)
                {
                    if (!task.FirstPass)
                    {
                        maxScale = int.MinValue;
                        minScale = int.MaxValue;
                        for (int i = 0; i < lastXdelta.Count(); i++)
                        {
                            var nextVal = lastXdelta[i];
                            for (int j = 0; j < plotCount; j++)
                            {
                                if (Math.Floor(nextVal[j]) < minScale) minScale = (int)Math.Floor(nextVal[j]);
                                if (Math.Floor(nextVal[j]) > maxScale) maxScale = (int)Math.Ceiling(nextVal[j]);
                            }
                        }
                    }
                    lastXdelta.Clear();
                    offChartCount = 0;
                    columnIndex = 1; // restart at the left side of the chart
                }
            }
            if (lastXdelta.Count() >= plotSeriesCount) lastXdelta.RemoveAt(0);
            if (task.heartBeat)
            {
                plotOutput.Line(new cv.Point(columnIndex, 0), new cv.Point(columnIndex, plotOutput.Height), Scalar.White, task.lineWidth);
            }
            for (int i = 0; i < plotCount; i++)
            {
                if (plotData[i] != 0)
                {
                    var y = 1 - (plotData[i] - minScale) / (maxScale - minScale);
                    y *= plotOutput.Height - 1;
                    var c = new cv.Point(columnIndex - task.DotSize, y - task.DotSize);
                    if (c.X < 1) c.X = 1;
                    DrawCircle(plotOutput, c, task.DotSize, plotColors[i]);
                }
            }
            columnIndex += 1;
            plotOutput.Col(columnIndex).SetTo(0);
            labels[2] = "Blue = " + string.Format(fmt1, plotData[0]) + " Green = " + string.Format(fmt1, plotData[1]) +
                        " Red = " + string.Format(fmt1, plotData[2]) + " Yellow = " + string.Format(fmt1, plotData[3]);
            strOut = "Blue = " + string.Format(fmt1, plotData[0]) + "\n";
            strOut += "Green = " + string.Format(fmt1, plotData[1]) + "\n";
            strOut += "Red = " + string.Format(fmt1, plotData[2]) + "\n";
            strOut += "White = " + string.Format(fmt1, plotData[3]) + "\n";
            SetTrueText(strOut, 3);
            var lineCount = (int)(maxScale - minScale - 1);
            if (lineCount > 3 || lineCount < 0) lineCount = 3;
            if (showScale) AddPlotScale(plotOutput, minScale, maxScale, lineCount);
            dst2 = plotOutput.Resize(task.WorkingRes);
        }
    }
    public class CS_Plot_Beats : CS_Parent
    {
        Plot_OverTimeFixedScale plot = new Plot_OverTimeFixedScale();
        public CS_Plot_Beats(VBtask task) : base(task)
        {
            plot.plotCount = 4;
            plot.showScale = false;
            plot.fixedScale = true;
            plot.minScale = 0;
            plot.maxScale = 5;
            desc = "Plot the beats.";
        }
        public void RunCS(Mat src)
        {
            plot.plotData[0] = task.heartBeat ? 1 : -1;
            plot.plotData[1] = task.midHeartBeat ? 2 : -1;
            plot.plotData[2] = task.quarterBeat ? 3 : -1;
            plot.plotData[3] = task.almostHeartBeat ? 4 : -1;
            plot.Run(src);
            dst2 = plot.dst2;
            strOut = "task.heartBeat (blue) = " + plot.plotData[0] + "\n";
            strOut += "task.midHeartBeat (green) = " + plot.plotData[1] + "\n";
            strOut += "task.quarterBeat (red) = " + plot.plotData[2] + "\n";
            strOut += "task.almostHeartBeat (white) = " + plot.plotData[3] + "\n";
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Plot_Basics_CPP : CS_Parent
    {
        public List<double> srcX = new List<double>();
        public List<double> srcY = new List<double>();
        public CS_Plot_Basics_CPP(VBtask task) : base(task)
        {
            for (int i = 0; i <= (int)task.MaxZmeters; i++) // something to plot if standaloneTest().
            {
                srcX.Add(i);
                srcY.Add(i * i * i);
            }
            cPtr = PlotOpenCV_Open();
            desc = "Demo the use of the integrated 2D plot available in OpenCV (only accessible in C++)";
        }
        public void RunCS(Mat src)
        {
            GCHandle handleX = GCHandle.Alloc(srcX.ToArray(), GCHandleType.Pinned);
            GCHandle handleY = GCHandle.Alloc(srcY.ToArray(), GCHandleType.Pinned);
            var imagePtr = PlotOpenCV_Run(cPtr, handleX.AddrOfPinnedObject(), handleY.AddrOfPinnedObject(), srcX.Count(),
                                          dst2.Rows, dst2.Cols);
            handleX.Free();
            handleY.Free();

            dst2 = new Mat(dst2.Rows, dst2.Cols, MatType.CV_8UC3, imagePtr);
            var maxX = srcX.Max();
            var minX = srcX.Min();
            var maxY = srcY.Max();
            var minY = srcY.Min();
            labels[2] = "x-Axis: " + minX + " to " + maxX + ", y-axis: " + minY + " to " + maxY;
        }
        public void Close()
        {
            if (cPtr != (IntPtr)0) cPtr = PlotOpenCV_Close(cPtr);
        }
    }
    public class CS_Plot_Dots : CS_Parent
    {
        public List<double> srcX = new List<double>();
        public List<double> srcY = new List<double>();
        public Scalar plotColor = Scalar.Yellow;
        public bool wipeSlate = true;
        public CS_Plot_Dots(VBtask task) : base(task)
        {
            for (int i = 0; i <= 50; i++) // something to plot if standaloneTest().
            {
                srcX.Add(i);
                srcY.Add(i * i * i);
            }
            desc = "Plot the requested points...";
        }
        public void RunCS(Mat src)
        {
            var maxX = srcX.Max();
            var minX = srcX.Min();
            var maxY = srcY.Max();
            var minY = srcY.Min();
            if (wipeSlate) dst2.SetTo(0);
            for (int i = 0; i < srcX.Count(); i++)
            {
                var pt = new cv.Point(dst2.Width * srcX[i] / maxX, dst2.Height - dst2.Height * srcY[i] / maxY);
                DrawCircle(dst2, pt, task.DotSize, plotColor);
            }
            labels[2] = "x-Axis: " + minX + " to " + maxX + ", y-axis: " + minY + " to " + maxY;
        }
    }
    public class CS_PlyFormat_Basics : CS_Parent
    {
        public Options_PlyFormat options = new Options_PlyFormat();
        string saveFileName;
        public CS_PlyFormat_Basics(VBtask task) : base(task)
        {
            desc = "Create a .ply format file with the pointcloud.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.fileName.Length != 0)
            {
                var fileInfo = new FileInfo(options.fileName);
                if (saveFileName != fileInfo.FullName)
                {
                    using (var sw = new StreamWriter(fileInfo.FullName))
                    {
                        saveFileName = fileInfo.FullName;
                        sw.WriteLine("ply");
                        sw.WriteLine("format ascii 1.0");
                        sw.WriteLine("element vertex " + task.pointCloud.Total());
                        sw.WriteLine("property float x");
                        sw.WriteLine("property float y");
                        sw.WriteLine("property float z");
                        sw.WriteLine("end_header");
                        for (int y = 0; y < task.pointCloud.Height; y++)
                        {
                            for (int x = 0; x < task.pointCloud.Width; x++)
                            {
                                var vec = task.pointCloud.Get<Vec3f>(y, x);
                                sw.WriteLine($"{vec[0]:F3} {vec[1]:F3} {vec[2]:F3}");
                            }
                        }
                    }
                }
                SetTrueText(".ply format file saved in " + options.fileName);
            }
        }
    }
    public class CS_PlyFormat_PlusRGB : CS_Parent
    {
        public Options_PlyFormat options = new Options_PlyFormat();
        string saveFileName;
        public CS_PlyFormat_PlusRGB(VBtask task) : base(task)
        {
            desc = "Save the pointcloud in .ply format and include the RGB data.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.fileName.Length != 0)
            {
                var fileInfo = new FileInfo(options.fileName);
                if (saveFileName != fileInfo.FullName)
                {
                    using (var sw = new StreamWriter(fileInfo.FullName))
                    {
                        saveFileName = fileInfo.FullName;
                        sw.WriteLine("ply");
                        sw.WriteLine("format ascii 1.0");
                        sw.WriteLine("element vertex " + task.pointCloud.Total());
                        sw.WriteLine("property float x");
                        sw.WriteLine("property float y");
                        sw.WriteLine("property float z");
                        sw.WriteLine("property uchar red");
                        sw.WriteLine("property uchar green");
                        sw.WriteLine("property uchar blue");
                        sw.WriteLine("end_header");
                        for (int y = 0; y < task.pointCloud.Height; y++)
                        {
                            for (int x = 0; x < task.pointCloud.Width; x++)
                            {
                                var vec = task.pointCloud.Get<Vec3f>(y, x);
                                var bgr = src.Get<Vec3b>(y, x);
                                sw.WriteLine($"{vec[0]:F3} {vec[1]:F3} {vec[2]:F3} {bgr[2]} {bgr[1]} {bgr[0]}");
                            }
                        }
                    }
                }
                SetTrueText(".ply format file saved in " + options.fileName);
            }
        }
    }
    public class CS_PointCloud_Basics : CS_Parent
    {
        Options_PointCloud options = new Options_PointCloud();
        public int actualCount;
        public List<cv.Point3f> allPointsH = new List<cv.Point3f>();
        public List<cv.Point3f> allPointsV = new List<cv.Point3f>();
        public List<List<cv.Point3f>> hList = new List<List<cv.Point3f>>();
        public List<List<cv.Point>> xyHList = new List<List<cv.Point>>();
        public List<List<cv.Point3f>> vList = new List<List<cv.Point3f>>();
        public List<List<cv.Point>> xyVList = new List<List<cv.Point>>();
        public CS_PointCloud_Basics(VBtask task) : base(task)
        {
            setPointCloudGrid();
            desc = "Reduce the point cloud to a manageable number points in 3D";
        }
        public List<List<cv.Point3f>> findHorizontalPoints(ref List<List<cv.Point>> xyList)
        {
            var ptlist = new List<List<cv.Point3f>>();
            var lastVec = new cv.Point3f();
            for (int y = 0; y < task.pointCloud.Height; y += task.gridList[0].Height - 1)
            {
                var vecList = new List<cv.Point3f>();
                var xyVec = new List<cv.Point>();
                for (int x = 0; x < task.pointCloud.Width; x += task.gridList[0].Width - 1)
                {
                    var vec = task.pointCloud.Get<cv.Point3f>(y, x);
                    bool jumpZ = false;
                    if (vec.Z > 0)
                    {
                        if ((Math.Abs(lastVec.Z - vec.Z) < options.deltaThreshold && lastVec.X < vec.X) || lastVec.Z == 0)
                        {
                            actualCount++;
                            DrawCircle(dst2, new cv.Point(x, y), task.DotSize, Scalar.White);
                            vecList.Add(vec);
                            xyVec.Add(new cv.Point(x, y));
                        }
                        else
                        {
                            jumpZ = true;
                        }
                    }
                    if (vec.Z == 0 || jumpZ)
                    {
                        if (vecList.Count() > 1)
                        {
                            ptlist.Add(new List<cv.Point3f>(vecList));
                            xyList.Add(new List<cv.Point>(xyVec));
                        }
                        vecList.Clear();
                        xyVec.Clear();
                    }
                    lastVec = vec;
                }
            }
            return ptlist;
        }
        public List<List<cv.Point3f>> findVerticalPoints(ref List<List<cv.Point>> xyList)
        {
            var ptlist = new List<List<Point3f>>();
            var lastVec = new Point3f();
            for (int x = 0; x < task.pointCloud.Width; x += task.gridList[0].Width - 1)
            {
                var vecList = new List<Point3f>();
                var xyVec = new List<cv.Point>();
                for (int y = 0; y < task.pointCloud.Height; y += task.gridList[0].Height - 1)
                {
                    var vec = task.pointCloud.Get<Point3f>(y, x);
                    bool jumpZ = false;
                    if (vec.Z > 0)
                    {
                        if ((Math.Abs(lastVec.Z - vec.Z) < options.deltaThreshold && lastVec.Y < vec.Y) || lastVec.Z == 0)
                        {
                            actualCount++;
                            DrawCircle(dst2, new cv.Point(x, y), task.DotSize, Scalar.White);
                            vecList.Add(vec);
                            xyVec.Add(new cv.Point(x, y));
                        }
                        else
                        {
                            jumpZ = true;
                        }
                    }
                    if (vec.Z == 0 || jumpZ)
                    {
                        if (vecList.Count() > 1)
                        {
                            ptlist.Add(new List<cv.Point3f>(vecList));
                            xyList.Add(new List<cv.Point>(xyVec));
                        }
                        vecList.Clear();
                        xyVec.Clear();
                    }
                    lastVec = vec;
                }
            }
            return ptlist;
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = src;
            actualCount = 0;
            xyHList.Clear();
            hList = findHorizontalPoints(ref xyHList);
            allPointsH.Clear();
            foreach (var h in hList)
            {
                foreach (var pt in h)
                {
                    allPointsH.Add(pt);
                }
            }
            xyVList.Clear();
            vList = findVerticalPoints(ref xyVList);
            allPointsV.Clear();
            foreach (var v in vList)
            {
                foreach (var pt in v)
                {
                    allPointsV.Add(pt);
                }
            }
            labels[2] = "Point series found = " + (hList.Count() + vList.Count()).ToString();
        }
    }
    public class CS_PointCloud_Point3f : CS_Parent
    {
        public CS_PointCloud_Point3f(VBtask task) : base(task)
        {
            desc = "Display the point cloud CV_32FC3 format";
        }
        public void RunCS(Mat src)
        {
            dst2 = task.pointCloud;
        }
    }
    public class CS_PointCloud_Spin2 : CS_Parent
    {
        PointCloud_Spin spin = new PointCloud_Spin();
        RedCloud_Basics redC = new RedCloud_Basics();
        RedCloud_Basics redCSpin = new RedCloud_Basics();
        public CS_PointCloud_Spin2(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "RedCloud output", "Spinning RedCloud output - use options to spin on different axes." };
            desc = "Spin the RedCloud output exercise";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            spin.Run(src);
            task.pointCloud = spin.dst2;
            redCSpin.Run(src);
            dst3 = redCSpin.dst2;
        }
    }
    public class CS_PointCloud_SetupSide : CS_Parent
    {
        int arcSize;
        public CS_PointCloud_SetupSide(VBtask task) : base(task)
        {
            arcSize = dst2.Width / 15;
            labels[2] = "Layout markers for side view";
            desc = "Create the colorized mat used for side projections";
        }
        public void RunCS(Mat src)
        {
            float distanceRatio = 1;
            if (src.Channels() != 3) src = src.CvtColor(ColorConversionCodes.GRAY2BGR);
            if (standaloneTest()) dst2.SetTo(0); else src.CopyTo(dst2);
            DrawCircle(dst2, task.sideCameraPoint, task.DotSize, Scalar.BlueViolet);
            for (int i = 1; i <= task.MaxZmeters; i++)
            {
                int xmeter = (int)(dst2.Width * i / task.MaxZmeters * distanceRatio);
                dst2.Line(new cv.Point(xmeter, 0), new cv.Point(xmeter, dst2.Height), Scalar.AliceBlue, 1);
                SetTrueText(i.ToString() + "m", new cv.Point(xmeter - src.Width / 24, dst2.Height - 10));
            }
            var cam = task.sideCameraPoint;
            var marker = new Point2f(dst2.Width / (task.MaxZmeters * distanceRatio), 0);
            marker.Y = (float)(marker.X * Math.Tan((task.vFov / 2) * Cv2.PI / 180));
            var markerLeft = new cv.Point(marker.X, cam.Y - marker.Y);
            var markerRight = new cv.Point(marker.X, cam.Y + marker.Y);
            int offset = (int)(Math.Sin(task.accRadians.X) * marker.Y);
            if (task.useGravityPointcloud)
            {
                if (task.accRadians.X > 0)
                {
                    markerLeft.Y -= offset;
                    markerRight.Y += offset;
                }
                else
                {
                    markerLeft.Y += offset;
                    markerRight.Y -= offset;
                }
                markerLeft = new cv.Point(markerLeft.X - cam.X, markerLeft.Y - cam.Y);
                markerLeft = new cv.Point(markerLeft.X * Math.Cos(task.accRadians.Z) - markerLeft.Y * Math.Sin(task.accRadians.Z),
                                        markerLeft.Y * Math.Cos(task.accRadians.Z) + markerLeft.X * Math.Sin(task.accRadians.Z));
                markerLeft = new cv.Point(markerLeft.X + cam.X, markerLeft.Y + cam.Y);
                markerRight = new cv.Point((markerRight.X - cam.X) * Math.Cos(task.accRadians.Z) - (markerRight.Y - cam.Y) * Math.Sin(task.accRadians.Z) + cam.X,
                                        (markerRight.Y - cam.Y) * Math.Cos(task.accRadians.Z) + (markerRight.X - cam.X) * Math.Sin(task.accRadians.Z) + cam.Y);
            }
            if (!standaloneTest())
            {
                DrawCircle(dst2, markerLeft, task.DotSize, Scalar.Red);
                DrawCircle(dst2, markerRight, task.DotSize, Scalar.Red);
            }
            float startAngle = (180 - task.vFov) / 2;
            float y = (float)(dst2.Width / Math.Tan(startAngle * Cv2.PI / 180));
            var fovTop = new cv.Point(dst2.Width, cam.Y - y);
            var fovBot = new cv.Point(dst2.Width, cam.Y + y);
            dst2.Line(cam, fovTop, Scalar.White, 1, task.lineType);
            dst2.Line(cam, fovBot, Scalar.White, 1, task.lineType);
            DrawCircle(dst2, markerLeft, task.DotSize + 3, Scalar.Red);
            DrawCircle(dst2, markerRight, task.DotSize + 3, Scalar.Red);
            dst2.Line(cam, markerLeft, Scalar.Red, 1, task.lineType);
            dst2.Line(cam, markerRight, Scalar.Red, 1, task.lineType);
            var labelLocation = new cv.Point(src.Width * 0.02, src.Height * 7 / 8);
            SetTrueText("vFOV=" + string.Format("{0:0.0}", 180 - startAngle * 2) + " deg.", new cv.Point(4, dst2.Height * 3 / 4));
        }
    }
    public class CS_PointCloud_SetupTop : CS_Parent
    {
        int arcSize;
        public CS_PointCloud_SetupTop(VBtask task) : base(task)
        {
            arcSize = dst2.Width / 15;
            labels[2] = "Layout markers for top view";
            desc = "Create the colorize the mat for a topdown projections";
        }
        public void RunCS(Mat src)
        {
            float distanceRatio = 1;
            if (src.Channels() != 3) src = src.CvtColor(ColorConversionCodes.GRAY2BGR);
            if (standaloneTest()) dst2.SetTo(0); else src.CopyTo(dst2);
            DrawCircle(dst2, task.topCameraPoint, task.DotSize, Scalar.BlueViolet);
            for (int i = 1; i <= task.MaxZmeters; i++)
            {
                int ymeter = (int)(dst2.Height - dst2.Height * i / (task.MaxZmeters * distanceRatio));
                dst2.Line(new cv.Point(0, ymeter), new cv.Point(dst2.Width, ymeter), Scalar.AliceBlue, 1);
                SetTrueText(i.ToString() + "m", new cv.Point(10, ymeter));
            }
            var cam = task.topCameraPoint;
            var marker = new Point2f(cam.X, dst2.Height / task.MaxZmeters);
            float topLen = (float)(marker.Y * Math.Tan((task.hFov / 2) * Cv2.PI / 180));
            float sideLen = (float)(marker.Y * Math.Tan((task.vFov / 2) * Cv2.PI / 180));
            var markerLeft = new cv.Point(cam.X - topLen, marker.Y);
            var markerRight = new cv.Point(cam.X + topLen, marker.Y);
            float offset = (float)Math.Sin(task.accRadians.Z) * topLen;
            if (task.useGravityPointcloud)
            {
                if (task.accRadians.Z > 0)
                {
                    markerLeft.X -= (int)offset;
                    markerRight.X += (int)offset;
                }
                else
                {
                    markerLeft.X += (int)offset;
                    markerRight.X -= (int)offset;
                }
            }
            float startAngle = (180 - task.hFov) / 2;
            float x = (float)(dst2.Height / Math.Tan(startAngle * Cv2.PI / 180));
            var fovRight = new cv.Point(task.topCameraPoint.X + x, 0);
            var fovLeft = new cv.Point(task.topCameraPoint.X - x, fovRight.Y);
            dst2.Line(task.topCameraPoint, fovLeft, Scalar.White, 1, task.lineType);
            DrawCircle(dst2, markerLeft, task.DotSize + 3, Scalar.Red);
            DrawCircle(dst2, markerRight, task.DotSize + 3, Scalar.Red);
            dst2.Line(cam, markerLeft, Scalar.Red, 1, task.lineType);
            dst2.Line(cam, markerRight, Scalar.Red, 1, task.lineType);
            float shift = (src.Width - src.Height) / 2;
            var labelLocation = new cv.Point(dst2.Width / 2 + shift, dst2.Height * 15 / 16);
            SetTrueText("hFOV=" + string.Format("{0:0.0}", 180 - startAngle * 2) + " deg.", new cv.Point(4, dst2.Height * 7 / 8));
            DrawLine(dst2, task.topCameraPoint, fovRight, Scalar.White);
        }
    }
    public class CS_PointCloud_Raw_CPP : CS_Parent
    {
        byte[] depthBytes;
        public CS_PointCloud_Raw_CPP(VBtask task) : base(task)
        {
            labels[2] = "Top View";
            labels[3] = "Side View";
            desc = "Project the depth data onto a top view And side view.";
            cPtr = SimpleProjectionOpen();
        }
        public void RunCS(Mat src)
        {
            if (task.FirstPass) Array.Resize(ref depthBytes, (int)(task.pcSplit[2].Total() * task.pcSplit[2].ElemSize()));
            Marshal.Copy(task.pcSplit[2].Data, depthBytes, 0, depthBytes.Length);
            var handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned);
            IntPtr imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject(), 0, task.MaxZmeters, task.pcSplit[2].Height, task.pcSplit[2].Width);
            dst2 = new Mat(task.pcSplit[2].Rows, task.pcSplit[2].Cols, MatType.CV_8U, imagePtr).CvtColor(ColorConversionCodes.GRAY2BGR);
            dst3 = new Mat(task.pcSplit[2].Rows, task.pcSplit[2].Cols, MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(ColorConversionCodes.GRAY2BGR);
            handleDepth.Free();
            labels[2] = "Top View (looking down)";
            labels[3] = "Side View";
        }
        public void Close()
        {
            SimpleProjectionClose(cPtr);
        }
    }
    public class CS_PointCloud_Raw : CS_Parent
    {
        public CS_PointCloud_Raw(VBtask task) : base(task)
        {
            labels[2] = "Top View";
            labels[3] = "Side View";
            desc = "Project the depth data onto a top view And side view - Using only VB code (too slow.)";
            cPtr = SimpleProjectionOpen();
        }
        public void RunCS(Mat src)
        {
            float range = task.MaxZmeters;
            dst2 = src.EmptyClone().SetTo(Scalar.White);
            dst3 = dst2.Clone();
            var black = new Vec3b(0, 0, 0);
            Parallel.ForEach(task.gridList, roi =>
            {
                for (int y = roi.Y; y < roi.Y + roi.Height; y++)
                {
                    for (int x = roi.X; x < roi.X + roi.Width; x++)
                    {
                        byte m = task.depthMask.Get<byte>(y, x);
                        if (m > 0)
                        {
                            float depth = task.pcSplit[2].Get<float>(y, x);
                            int dy = (int)(src.Height * depth / range);
                            if (dy < src.Height && dy > 0) dst2.Set<Vec3b>(src.Height - dy, x, black);
                            int dx = (int)(src.Width * depth / range);
                            if (dx < src.Width && dx > 0) dst3.Set<Vec3b>(y, dx, black);
                        }
                    }
                }
            });
            labels[2] = "Top View (looking down)";
            labels[3] = "Side View";
        }
        public void Close()
        {
            SimpleProjectionClose(cPtr);
        }
    }
    public class CS_PointCloud_Solo : CS_Parent
    {
        public HeatMap_Basics heat = new HeatMap_Basics();
        public CS_PointCloud_Solo(VBtask task) : base(task)
        {
            FindCheckBox("Top View (Unchecked Side View)").Checked = true;
            labels[2] = "Top down view after inrange sampling";
            labels[3] = "Histogram after filtering For Single-only histogram bins";
            desc = "Find floor And ceiling Using gravity aligned top-down view And selecting bins With exactly 1 sample";
        }
        public void RunCS(Mat src)
        {
            heat.Run(src);
            dst2 = heat.dst0.InRange(task.frameHistoryCount, task.frameHistoryCount).ConvertScaleAbs();
            dst3 = heat.dst1.InRange(task.frameHistoryCount, task.frameHistoryCount).ConvertScaleAbs();
        }
    }
    public class CS_PointCloud_SoloRegions : CS_Parent
    {
        public PointCloud_Solo solo = new PointCloud_Solo();
        Dilate_Basics dilate = new Dilate_Basics();
        public CS_PointCloud_SoloRegions(VBtask task) : base(task)
        {
            labels[2] = "Top down view before inrange sampling";
            labels[3] = "Histogram after filtering For Single-only histogram bins";
            desc = "Find floor And ceiling Using gravity aligned top-down view And selecting bins With exactly 1 sample";
        }
        public void RunCS(Mat src)
        {
            solo.Run(src);
            dst2 = solo.dst2;
            dst3 = solo.dst3;
            dilate.Run(dst3.Clone());
            dst3 = dilate.dst2;
        }
    }
    public class CS_PointCloud_SurfaceH_CPP : CS_Parent
    {
        public HeatMap_Basics heat = new HeatMap_Basics();
        public Plot_Basics_CPP plot = new Plot_Basics_CPP();
        public int topRow;
        public int botRow;
        public int peakRow;
        public CS_PointCloud_SurfaceH_CPP(VBtask task) : base(task)
        {
            desc = "Find the horizontal surfaces With a projects Of the SideView histogram.";
        }
        public void RunCS(Mat src)
        {
            heat.Run(src);
            dst2 = heat.dst3;
            topRow = 0;
            botRow = 0;
            peakRow = 0;
            int peakVal = 0;
            for (int i = 0; i < dst2.Height; i++)
            {
                plot.srcX.Add(i);
                if (dst2.Channels() == 1) plot.srcY.Add(dst2.Row(i).CountNonZero());
                else plot.srcY.Add(dst2.Row(i).CvtColor(ColorConversionCodes.BGR2GRAY).CountNonZero());
                if (peakVal < plot.srcY[i])
                {
                    peakVal = (int)plot.srcY[i];
                    peakRow = i;
                }
                if (topRow == 0 && plot.srcY[i] > 10) topRow = i;
            }
            for (int i = plot.srcY.Count() - 1; i >= 0; i--)
            {
                if (botRow == 0 && plot.srcY[i] > 10) botRow = i;
            }
            plot.Run(empty);
            dst3 = plot.dst2.Transpose();
            dst3 = dst3.Flip(FlipMode.Y);
            labels[2] = "Top row = " + topRow.ToString() + " peak row = " + peakRow.ToString() + " bottom row = " + botRow.ToString();
        }
    }
    public class CS_PointCloud_SurfaceH : CS_Parent
    {
        public HeatMap_Basics heat = new HeatMap_Basics();
        public Plot_Histogram plot = new Plot_Histogram();
        public int topRow;
        public int botRow;
        public int peakRow;
        public CS_PointCloud_SurfaceH(VBtask task) : base(task)
        {
            FindCheckBox("Top View (Unchecked Side View)").Checked = true;
            labels[3] = "Histogram Of Each Of " + task.histogramBins.ToString() + " bins aligned With the sideview";
            desc = "Find the horizontal surfaces With a projects Of the SideView histogram.";
        }
        public void RunCS(Mat src)
        {
            heat.Run(src);
            dst2 = heat.dst2;
            var hist = new Mat(dst2.Height, 1, MatType.CV_32F, 0);
            var indexer = hist.GetGenericIndexer<float>();
            topRow = 0;
            botRow = 0;
            peakRow = 0;
            int peakVal = 0;
            if (dst2.Channels() != 1) dst1 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            for (int i = 0; i < dst1.Height; i++)
            {
                indexer[i] = dst1.Row(i).CountNonZero();
                if (peakVal < indexer[i])
                {
                    peakVal = (int)indexer[i];
                    peakRow = i;
                }
                if (topRow == 0 && indexer[i] > 10) topRow = i;
            }
            plot.maxValue = (float)(Math.Floor((double)(peakVal / 100 + 1) * 100));
            for (int i = hist.Rows - 1; i >= 0; i--)
            {
                if (botRow == 0 && indexer[i] > 10) botRow = i;
            }
            plot.Run(hist);
            dst3 = plot.dst2.Transpose();
            dst3 = dst3.Flip(FlipMode.Y)[new cv.Rect(0, 0, dst0.Height, dst0.Height)].Resize(dst0.Size());
            labels[2] = "Top row = " + topRow.ToString() + " peak row = " + peakRow.ToString() + " bottom row = " + botRow.ToString();
            var ratio = task.mouseMovePoint.Y / (double)dst2.Height;
            var offset = ratio * dst3.Height;
            DrawLine(dst2, new cv.Point(0, task.mouseMovePoint.Y), new cv.Point(dst2.Width, task.mouseMovePoint.Y), Scalar.Yellow);
            dst3.Line(new cv.Point(0, offset), new cv.Point(dst3.Width, offset), Scalar.Yellow, task.lineWidth);
        }
    }
    public class CS_PointCloud_NeighborV : CS_Parent
    {
        Options_Neighbors options = new Options_Neighbors();
        public CS_PointCloud_NeighborV(VBtask task) : base(task)
        {
            desc = "Show where vertical neighbor depth values are within Y mm's";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (src.Type() != MatType.CV_32F) src = task.pcSplit[2];
            var tmp32f = new Mat(dst2.Size(), MatType.CV_32F, 0);
            var r1 = new cv.Rect(options.pixels, 0, dst2.Width - options.pixels, dst2.Height);
            var r2 = new cv.Rect(0, 0, dst2.Width - options.pixels, dst2.Height);
            Cv2.Absdiff(src[r1], src[r2], tmp32f[r1]);
            tmp32f = tmp32f.Threshold(options.threshold, 255, ThresholdTypes.BinaryInv);
            dst2 = tmp32f.ConvertScaleAbs(255);
            dst2.SetTo(0, task.noDepthMask);
            dst2[new cv.Rect(0, dst2.Height - options.pixels, dst2.Width, options.pixels)].SetTo(0);
            labels[2] = "White: z is within " + (options.threshold * 1000).ToString(fmt0) + " mm's with Y pixel offset " + options.pixels.ToString();
        }
    }
    public class CS_PointCloud_Visualize : CS_Parent
    {
        public CS_PointCloud_Visualize(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Pointcloud visualized", "" };
            desc = "Display the pointcloud as a BGR image.";
        }
        public void RunCS(Mat src)
        {
            var pcSplit = new Mat[] { task.pcSplit[0].ConvertScaleAbs(255), task.pcSplit[1].ConvertScaleAbs(255), task.pcSplit[2].ConvertScaleAbs(255) };
            Cv2.Merge(pcSplit, dst2);
        }
    }
    public class CS_PointCloud_PCpointsMask : CS_Parent
    {
        public Mat pcPoints;
        public int actualCount;
        public CS_PointCloud_PCpointsMask(VBtask task) : base(task)
        {
            setPointCloudGrid();
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Reduce the point cloud to a manageable number points in 3D representing the averages of X, Y, and Z in that roi.";
        }
        public void RunCS(Mat src)
        {
            if (task.optionsChanged) pcPoints = new Mat(task.gridRows, task.gridCols, MatType.CV_32FC3, 0);
            dst2.SetTo(0);
            actualCount = 0;
            float lastMeanZ = 0;
            for (int y = 0; y < task.gridRows; y++)
            {
                for (int x = 0; x < task.gridCols; x++)
                {
                    var roi = task.gridList[y * task.gridCols + x];
                    var mean = task.pointCloud[roi].Mean(task.depthMask[roi]);
                    bool depthPresent = task.depthMask[roi].CountNonZero() > roi.Width * roi.Height / 2;
                    if ((depthPresent && mean[2] > 0 && Math.Abs(lastMeanZ - mean[2]) < 0.2 && mean[2] < task.MaxZmeters) || (lastMeanZ == 0 && mean[2] > 0))
                    {
                        pcPoints.Set<Point3f>(y, x, new Point3f((float)mean[0], (float)mean[1], (float)mean[2]));
                        actualCount++;
                        DrawCircle(dst2, new cv.Point(roi.X, roi.Y),  (int)(task.DotSize * Math.Max(mean[2], 1)), 
                                    Scalar.White);
                    }
                    lastMeanZ = (float)mean[2];
                }
            }
            labels[2] = "PointCloud cv.Point Points found = " + actualCount.ToString();
        }
    }
    public class CS_PointCloud_PCPoints : CS_Parent
    {
        public List<Point3f> pcPoints = new List<Point3f>();
        public CS_PointCloud_PCPoints(VBtask task) : base(task)
        {
            setPointCloudGrid();
            desc = "Reduce the point cloud to a manageable number points in 3D using the mean value";
        }
        public void RunCS(Mat src)
        {
            int rw = task.gridList[0].Width / 2, rh = task.gridList[0].Height / 2;
            cv.Scalar red32 = new cv.Scalar(0, 0, 1);
            cv.Scalar blue32 = new cv.Scalar(1, 0, 0);
            cv.Scalar white32 = new cv.Scalar(1, 1, 1);
            cv.Scalar red = Scalar.Red;
            cv.Scalar blue = Scalar.Blue;
            cv.Scalar white = Scalar.White;
            pcPoints.Clear();
            dst2 = src;
            foreach (var roi in task.gridList)
            {
                var pt = new cv.Point(roi.X + rw, roi.Y + rh);
                var mean = task.pointCloud[roi].Mean(task.depthMask[roi]);
                if (mean[2] > 0)
                {
                    if (pt.Y % 3 == 0) pcPoints.Add(new cv.Point3f((float)red32[0], (float) red32[1], (float)red32[2]));
                    if (pt.Y % 3 == 1) pcPoints.Add(new cv.Point3f((float)blue32[0], (float)blue32[1], (float)blue32[2]));
                    if (pt.Y % 3 == 2) pcPoints.Add(new cv.Point3f((float)white32[0], (float)white32[1], (float)white32[2]));
                    pcPoints.Add(new cv.Point3f((float)mean[0], (float)mean[1], (float)mean[2]));
                    if (pt.Y % 3 == 0) DrawCircle(dst2, pt, task.DotSize, red);
                    if (pt.Y % 3 == 1) DrawCircle(dst2, pt, task.DotSize, blue);
                    if (pt.Y % 3 == 2) DrawCircle(dst2, pt, task.DotSize, white);
                }
            }
            labels[2] = "PointCloud cv.Point Points found = " + (pcPoints.Count() / 2).ToString();
        }
    }
    public class CS_PointCloud_PCPointsPlane : CS_Parent
    {
        PointCloud_Basics pcBasics = new PointCloud_Basics();
        public List<cv.Point3f> pcPoints = new List<cv.Point3f>();
        public List<cv.Point> xyList = new List<cv.Point>();
        cv.Point3f white32 = new cv.Point3f(1, 1, 1);
        public CS_PointCloud_PCPointsPlane(VBtask task) : base(task)
        {
            setPointCloudGrid();
            desc = "Find planes using a reduced set of 3D points and the intersection of vertical and horizontal lines through those points.";
        }
        public void RunCS(Mat src)
        {
            pcBasics.Run(src);
            pcPoints.Clear();
            // points in both the vertical and horizontal lists are likely to designate a plane
            foreach (var pt in pcBasics.allPointsH)
            {
                if (pcBasics.allPointsV.Contains(pt))
                {
                    pcPoints.Add(white32);
                    pcPoints.Add(pt);
                }
            }
            labels[2] = "Point series found = " + (pcPoints.Count() / 2).ToString();
        }
    }
    public class CS_PointCloud_Inspector : CS_Parent
    {
        public CS_PointCloud_Inspector(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            task.mouseMovePoint.X = dst2.Width / 2;
            desc = "Inspect x, y, and z values in a row or column";
        }
        public void RunCS(Mat src)
        {
            int yLines = 20;
            int cLine = task.mouseMovePoint.X;
            Mat input = src;
            if (input.Type() != MatType.CV_32F) input = task.pcSplit[2];
            Point2f topPt = new Point2f(cLine, 0);
            Point2f botPt = new Point2f(cLine, dst2.Height);
            dst2 = task.depthRGB;
            DrawLine(dst2, topPt, botPt, 255);
            double stepY = dst2.Height / yLines;
            SetTrueText("\t   X\t  Y\t  Z", 3);
            for (int i = 1; i < yLines - 1; i++)
            {
                Point2f pt1 = new Point2f(dst2.Width, (float)(i * stepY));
                Point2f pt2 = new Point2f(0, (float)(i * stepY));
                DrawLine(dst2, pt1, pt2, Scalar.White);
                Point2f pt = new Point2f(cLine, (float)(i * stepY));
                Vec3f xyz = task.pointCloud.Get<Vec3f>((int) pt.Y, (int) pt.X);
                SetTrueText("Row " + i.ToString() + "\t" + xyz[0].ToString(fmt2) + "\t" + xyz[1].ToString(fmt2) + 
                            "\t" + xyz[2].ToString(fmt2), new cv.Point(5, (int)pt.Y), 3);
            }
            labels[2] = "Values displayed are the point cloud X, Y, and Z values for column " + cLine.ToString();
            labels[3] = "Move mouse in the image at left to see the point cloud X, Y, and Z values.";
        }
    }
    public class CS_PointCloud_Average : CS_Parent
    {
        List<Mat> pcHistory = new List<Mat>();
        public CS_PointCloud_Average(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_32FC3, 0);
            desc = "Average all 3 elements of the point cloud - not just depth.";
        }
        public void RunCS(Mat src)
        {
            pcHistory.Add(task.pointCloud);
            if (pcHistory.Count() >= task.frameHistoryCount) pcHistory.RemoveAt(0);
            dst3.SetTo(0);
            foreach (var m in pcHistory)
            {
                dst3 += m;
            }
            dst3 *= 1.0 / pcHistory.Count();
        }
    }
    public class CS_PointCloud_FrustrumTop : CS_Parent
    {
        Draw_Frustrum frustrum = new Draw_Frustrum();
        HeatMap_Basics heat = new HeatMap_Basics();
        PointCloud_SetupTop setupTop = new PointCloud_SetupTop();
        public CS_PointCloud_FrustrumTop(VBtask task) : base(task)
        {
            task.gOptions.setGravityUsage(false);
            FindCheckBox("Top View (Unchecked Side View)").Checked = true;
            labels[3] = "Draw the frustrum from the top view";
            desc = "Draw the top view of the frustrum";
        }
        public void RunCS(Mat src)
        {
            frustrum.Run(src);
            heat.Run(frustrum.dst3.Resize(dst2.Size()));
            setupTop.Run(heat.dst2);
            dst2 = setupTop.dst2;
        }
    }
    public class CS_PointCloud_FrustrumSide : CS_Parent
    {
        Draw_Frustrum frustrum = new Draw_Frustrum();
        HeatMap_Basics heat = new HeatMap_Basics();
        PointCloud_SetupSide setupSide = new PointCloud_SetupSide();
        public CS_PointCloud_FrustrumSide(VBtask task) : base(task)
        {
            task.gOptions.setGravityUsage(false);
            FindCheckBox("Top View (Unchecked Side View)").Checked = false;
            labels[2] = "Draw the frustrum from the side view";
            desc = "Draw the side view of the frustrum";
        }
        public void RunCS(Mat src)
        {
            frustrum.Run(src);
            heat.Run(frustrum.dst3.Resize(dst2.Size()));
            setupSide.Run(heat.dst3);
            dst2 = setupSide.dst2;
        }
    }
    public class CS_PointCloud_Histograms : CS_Parent
    {
        Plot_Histogram2D plot2D = new Plot_Histogram2D();
        Plot_Histogram plot = new Plot_Histogram();
        Hist3Dcloud_Basics hcloud = new Hist3Dcloud_Basics();
        Grid_Basics grid = new Grid_Basics();
        public Mat histogram = new Mat();
        public CS_PointCloud_Histograms(VBtask task) : base(task)
        {
            task.gOptions.setHistogramBins(9);
            task.redOptions.setXYReduction(true);
            labels = new string[] { "", "", "Plot of 2D histogram", "All non-zero entries in the 2D histogram" };
            desc = "Create a 2D histogram of the point cloud data - which 2D inputs is in options.";
        }
        public void RunCS(Mat src)
        {
            task.redOptions.Sync(); // make sure settings are consistent
            Cv2.CalcHist(new Mat[] { task.pointCloud }, task.redOptions.channels, new Mat(), histogram, task.redOptions.channelCount,
                            task.redOptions.histBinList, task.redOptions.ranges);
            switch (task.redOptions.PointCloudReduction)
            {
                case 0:
                case 1:
                case 2: // "X Reduction", "Y Reduction", "Z Reduction"
                    plot.Run(histogram);
                    dst2 = plot.histogram;
                    labels[2] = "2D plot of 1D histogram.";
                    break;
                case 3:
                case 4:
                case 5: // "XY Reduction", "XZ Reduction", "YZ Reduction"
                    plot2D.Run(histogram);
                    dst2 = plot2D.dst2;
                    labels[2] = "2D plot of 2D histogram.";
                    break;
                case 6: // "XYZ Reduction"
                    if (dst2.Type() != MatType.CV_8U) dst2 = new Mat(dst2.Size(), MatType.CV_8U);
                    hcloud.Run(task.pointCloud);
                    histogram = hcloud.histogram;
                    float[] histData = new float[histogram.Total()];
                    Marshal.Copy(histogram.Data, histData, 0, histData.Length);
                    if (histData.Length > 255 && task.histogramBins > 3)
                    {
                        task.histogramBins -= 1;
                    }
                    if (histData.Length < 128 && task.histogramBins < task.gOptions.getHistBinBarMax())
                    {
                        task.histogramBins += 1;
                    }
                    if (task.gridList.Count() < histData.Length && task.gridSize > 2)
                    {
                        task.gridSize -= 1;
                        grid.Run(src);
                        dst2.SetTo(0);
                    }
                    histData[0] = 0; // count of zero pixels - distorts results..
                    float maxVal = histData.Max();
                    for (int i = 0; i < task.gridList.Count(); i++)
                    {
                        var roi = task.gridList[i];
                        if (i >= histData.Length)
                        {
                            dst2[roi].SetTo(0);
                        }
                        else
                        {
                            dst2[roi].SetTo(255 * histData[i] / maxVal);
                        }
                    }
                    labels[2] = "2D plot of the resulting 3D histogram.";
                    break;
            }
            var mm = GetMinMax(dst2);
            dst3 = ShowPalette(dst2 * 255 / mm.maxVal);
        }
    }
    public class CS_PointCloud_ReduceSplit2 : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        public CS_PointCloud_ReduceSplit2(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": redOptions 'X/Y-Range X100' sliders to test further.");
            desc = "Reduce the task.pcSplit[2] for use in several algorithms.";
        }
        public void RunCS(Mat src)
        {
            dst2 = task.pcSplit[2] * 1000;
            dst2.ConvertTo(dst2, MatType.CV_32S);
            reduction.Run(dst2);
            reduction.dst2.ConvertTo(dst1, MatType.CV_32F);
            dst1 *= 0.001;
            if (standaloneTest())
            {
                dst3 = task.pointCloud;
            }
            else
            {
                var mm = GetMinMax(dst1);
                dst1 *= task.MaxZmeters / mm.maxVal;
                Cv2.Merge(new Mat[] { task.pcSplit[0], task.pcSplit[1], dst1 }, dst3);
            }
        }
    }
    public class CS_PointCloud_ReducedTopView : CS_Parent
    {
        PointCloud_ReduceSplit2 split2 = new PointCloud_ReduceSplit2();
        public CS_PointCloud_ReducedTopView(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": redOptions 'Reduction Sliders' have high impact.");
            desc = "Create a stable side view of the point cloud";
        }
        public void RunCS(Mat src)
        {
            split2.Run(task.pointCloud);
            Cv2.CalcHist(new Mat[] { split2.dst3 }, task.channelsTop, new Mat(), dst1, 2, task.bins2D, task.rangesTop);
            dst1 = dst1.Flip(FlipMode.X);
            dst1 = dst1.Threshold(0, 255, ThresholdTypes.Binary);
            dst1.ConvertTo(dst2, MatType.CV_8UC1);
        }
    }
    public class CS_PointCloud_ReducedSideView : CS_Parent
    {
        PointCloud_ReduceSplit2 split2 = new PointCloud_ReduceSplit2();
        public CS_PointCloud_ReducedSideView(VBtask task) : base(task)
        {
            desc = "Show where vertical neighbor depth values are within X mm's";
        }
        public void RunCS(Mat src)
        {
            split2.Run(null);
            Cv2.CalcHist(new Mat[] { split2.dst3 }, task.channelsSide, new Mat(), dst1, 2, task.bins2D, task.rangesSide);
            dst1 = dst1.Threshold(0, 255, ThresholdTypes.Binary);
            dst1 = dst1.Threshold(0, 255, ThresholdTypes.Binary);
            dst1.ConvertTo(dst2, MatType.CV_8UC1);
        }
    }
    public class CS_PointCloud_ReducedViews : CS_Parent
    {
        PointCloud_ReduceSplit2 split2 = new PointCloud_ReduceSplit2();
        public CS_PointCloud_ReducedViews(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Reduced side view", "Reduced top view" };
            desc = "Show where vertical neighbor depth values are within X mm's";
        }
        public void RunCS(Mat src)
        {
            split2.Run(null);
            Cv2.CalcHist(new Mat[] { split2.dst3 }, task.channelsSide, new Mat(), dst1, 2, task.bins2D, task.rangesSide);
            dst1 = dst1.Threshold(0, 255, ThresholdTypes.Binary);
            dst1.ConvertTo(dst2, MatType.CV_8UC1);
            Cv2.CalcHist(new Mat[] { split2.dst3 }, task.channelsTop, new Mat(), dst1, 2, task.bins2D, task.rangesTop);
            dst1 = dst1.Flip(FlipMode.X);
            dst1 = dst1.Threshold(0, 255, ThresholdTypes.Binary);
            dst1.ConvertTo(dst3, MatType.CV_8UC1);
        }
    }
    public class CS_PointCloud_XRangeTest : CS_Parent
    {
        PointCloud_ReduceSplit2 split2 = new PointCloud_ReduceSplit2();
        public CS_PointCloud_XRangeTest(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": redOptions 'X-Range X100' slider has high impact.");
            desc = "Test adjusting the X-Range value to squeeze a histogram into dst2.";
        }
        public void RunCS(Mat src)
        {
            split2.Run(src);
            Cv2.CalcHist(new Mat[] { split2.dst3 }, task.channelsTop, new Mat(), dst1, 2, task.bins2D, task.rangesTop);
            dst1 = dst1.Threshold(0, 255, ThresholdTypes.Binary);
            dst1 = dst1.Flip(FlipMode.X);
            dst1.ConvertTo(dst2, MatType.CV_8UC1);
        }
    }
    public class CS_PointCloud_YRangeTest : CS_Parent
    {
        PointCloud_ReduceSplit2 split2 = new PointCloud_ReduceSplit2();
        public CS_PointCloud_YRangeTest(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": redOptions 'Y-Range X100' slider has high impact.");
            desc = "Test adjusting the Y-Range value to squeeze a histogram into dst2.";
        }
        public void RunCS(Mat src)
        {
            split2.Run(src);
            Cv2.CalcHist(new Mat[] { split2.dst3 }, task.channelsSide, new Mat(), dst1, 2, task.bins2D, task.rangesSide);
            dst1 = dst1.Threshold(0, 255, ThresholdTypes.Binary);
            dst1.ConvertTo(dst2, MatType.CV_8UC1);
        }
    }
    public class CS_Polylines_IEnumerableExample : CS_Parent
    {
        Options_PolyLines options = new Options_PolyLines();
        public CS_Polylines_IEnumerableExample(VBtask task) : base(task)
        {
            desc = "Manually create an IEnumerable<IEnumerable<cv.Point>>.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            var points = Enumerable.Range(0, options.polyCount).Select(i =>
                new cv.Point(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))).ToList();
            var pts = new List<List<cv.Point>> { points };
            dst2 = new Mat(src.Size(), MatType.CV_8U, 0);
            // NOTE: when there are 2 points, there will be 1 line.
            Cv2.Polylines(dst2, pts, options.polyClosed, Scalar.White, task.lineWidth, task.lineType);
        }
    }
    // C# implementation of the browse example in OpenCV.
    // https://github.com/opencv/opencv/blob/master/samples/python/browse.py
    public class CS_Polylines_Random : CS_Parent
    {
        Pixel_Zoom zoom = new Pixel_Zoom();
        public CS_Polylines_Random(VBtask task) : base(task)
        {
            labels[2] = "To zoom move the mouse over the image";
            desc = "Create a random procedural image";
        }
        public void RunCS(Mat src)
        {
            if (task.frameCount % (task.fpsRate * 3) == 0) // every x frames.
            {
                int h = src.Height, w = src.Width;
                Random autorand = new Random();
                Point2f[] points2f = new Point2f[10000];
                var pts = new List<List<cv.Point>>();
                var points = new List<cv.Point>();
                points2f[0] = new Point2f((float)(autorand.NextDouble() - 0.5), (float)(autorand.NextDouble() - 0.5));
                for (int i = 1; i < points2f.Length; i++)
                {
                    points2f[i] = new Point2f((float)(autorand.NextDouble() - 0.5 + points2f[i - 1].X),
                                               (float)(autorand.NextDouble() - 0.5 + points2f[i - 1].Y));
                    points.Add(new cv.Point((int)(points2f[i].X * 10 + w / 2), (int)(points2f[i].Y * 10 + h / 2)));
                }
                pts.Add(points);
                dst2 = new Mat(src.Size(), MatType.CV_8U, 0);
                Cv2.Polylines(dst2, pts, false, Scalar.White, task.lineWidth, task.lineType);
                dst2 = dst2.CvtColor(ColorConversionCodes.GRAY2BGR);
            }
            zoom.Run(dst2);
            dst3 = zoom.dst2;
        }
    }
    public class CS_PongWars_Basics : CS_Parent
    {
        int sqWidth = 25;
        int sqHeight;
        int numSquaresX;
        int numSquaresY;
        const int DAY_COLOR = 1, DAY_BALL_COLOR = 2, NIGHT_COLOR = 3, NIGHT_BALL_COLOR = 4;
        int[,] squares;
        cv.Point p1;
        Point2f d1;
        cv.Point p2;
        Point2f d2;
        int iteration = 0;
        cv.Point p1Last = new cv.Point();
        cv.Point p2Last = new cv.Point();
        public CS_PongWars_Basics(VBtask task) : base(task)
        {
            sqHeight = 25 * task.WorkingRes.Height / task.WorkingRes.Width;
            numSquaresX = task.WorkingRes.Width / sqWidth;
            numSquaresY = task.WorkingRes.Height / sqHeight;
            squares = new int[numSquaresX, numSquaresY];
            p1 = new cv.Point(task.WorkingRes.Width / 4, task.WorkingRes.Height / 2);
            d1 = new Point2f(12.5f, -12.5f);
            p2 = new cv.Point((task.WorkingRes.Width / 4) * 3, task.WorkingRes.Height / 2);
            d2 = new Point2f(-12.5f, 12.5f);
            for (int i = 0; i < numSquaresX; i++)
            {
                for (int j = 0; j < numSquaresY; j++)
                {
                    squares[i, j] = (i < numSquaresX / 2) ? DAY_COLOR : NIGHT_COLOR;
                }
            }
            p1 = new cv.Point(msRNG.Next(0, dst2.Width / 4), msRNG.Next(0, dst2.Height / 2));
            p2 = new cv.Point(msRNG.Next(dst2.Width / 2, dst2.Width), msRNG.Next(dst2.Height / 4, dst2.Height));
            UpdateAdvice(traceName + ": <place advice here on any options that are useful>");
            desc = "Pong as war between the forces of light and darkness.";
        }
        Point2f UpdateSquareAndBounce(cv.Point pt, Point2f dxy, int sqClass)
        {
            for (double angle = 0; angle <= Math.PI * 2; angle += Math.PI / 4)
            {
                double checkX = pt.X + Math.Cos(angle) * (sqWidth / 2);
                double checkY = pt.Y + Math.Sin(angle) * (sqHeight / 2);
                int i = (int)Math.Floor(checkX / sqWidth);
                int j = (int)Math.Floor(checkY / sqHeight);
                if (i >= 0 && i < numSquaresX && j >= 0 && j < numSquaresY)
                {
                    if (squares[i, j] != sqClass)
                    {
                        squares[i, j] = sqClass;
                        if (Math.Abs(Math.Cos(angle)) > Math.Abs(Math.Sin(angle)))
                        {
                            dxy.X = -dxy.X;
                        }
                        else
                        {
                            dxy.Y = -dxy.Y;
                        }
                    }
                }
            }
            return dxy;
        }
        Point2f CheckBoundaryCollision(cv.Point pt, Point2f dxy)
        {
            if (pt.X + dxy.X > dst2.Width - sqWidth / 2 || pt.X + dxy.X < sqWidth / 2) dxy.X = -dxy.X;
            if (pt.Y + dxy.Y > dst2.Height - sqHeight / 2 || pt.Y + dxy.Y < sqHeight / 2) dxy.Y = -dxy.Y;
            return dxy;
        }
        void UpdateScoreElement()
        {
            int dayScore = 0;
            int nightScore = 0;
            for (int i = 0; i < numSquaresX; i++)
            {
                for (int j = 0; j < numSquaresY; j++)
                {
                    if (squares[i, j] == DAY_COLOR)
                    {
                        dayScore += 1;
                    }
                    else if (squares[i, j] == NIGHT_COLOR)
                    {
                        nightScore += 1;
                    }
                }
            }
            if (task.heartBeat) labels[2] = $"Pong War: day {dayScore} | night {nightScore}";
        }
        public void RunCS(Mat src)
        {
            iteration += 1;
            if (iteration % 1000 == 0)
            {
                Console.WriteLine("iteration " + iteration);
            }
            d1 = UpdateSquareAndBounce(p1, d1, DAY_COLOR);
            d2 = UpdateSquareAndBounce(p2, d2, NIGHT_COLOR);
            d1 = CheckBoundaryCollision(p1, d1);
            d2 = CheckBoundaryCollision(p2, d2);
            p1.X += (int) d1.X;
            p1.Y += (int) d1.Y;
            p2.X += (int) d2.X;
            p2.Y += (int) d2.Y;
            if (p1Last == p1) p1 = new cv.Point(msRNG.Next(0, dst2.Width / 2), msRNG.Next(0, dst2.Height / 2));
            p1Last = p1;
            if (p2Last == p2) p2 = new cv.Point(msRNG.Next(0, dst2.Width / 2), msRNG.Next(0, dst2.Height / 2));
            p2Last = p2;
            UpdateScoreElement();
            dst2.SetTo(0);
            for (int i = 0; i < numSquaresX; i++)
            {
                for (int j = 0; j < numSquaresY; j++)
                {
                    var rect = new cv.Rect(i * sqWidth, j * sqHeight, sqWidth, sqHeight);
                    int index = squares[i, j];
                    dst2.Rectangle(rect, task.scalarColors[index], -1);
                }
            }
            var pt = new cv.Point((int)(p1.X - sqWidth / 2), (int)(p1.Y - sqHeight / 2));
            DrawCircle(dst2, pt, task.DotSize + 5, task.scalarColors[DAY_BALL_COLOR]);
            pt = new cv.Point((int)(p2.X - sqWidth / 2), (int)(p2.Y - sqHeight / 2));
            DrawCircle(dst2, pt, task.DotSize + 5, task.scalarColors[NIGHT_BALL_COLOR]);
        }
    }
    public class CS_PongWars_Two : CS_Parent
    {
        PongWars_Basics pong1 = new PongWars_Basics();
        PongWars_Basics pong2 = new PongWars_Basics();
        public CS_PongWars_Two(VBtask task) : base(task)
        {
            desc = "Running 2 pong wars at once.  Randomness inserted with starting location.";
        }
        public void RunCS(Mat src)
        {
            pong1.Run(src);
            dst2 = pong1.dst2.Clone();
            labels[2] = pong1.labels[2];
            pong2.Run(src);
            dst3 = pong2.dst2.Clone();
            labels[3] = pong2.labels[2];
        }
    }
    public class CS_ProCon_Basics : CS_Parent
    {
        readonly object _lockObject = new object();
        public Thread p;
        public Thread c;
        public int head = -1;
        public int tail = -1;
        public int frameCount = 1;
        public Font_FlowText flow = new Font_FlowText();
        public bool terminateConsumer;
        public bool terminateProducer;
        public Options_ProCon options = new Options_ProCon();
        public CS_ProCon_Basics(VBtask task) : base(task)
        {
            flow.parentData = this;
            p = new Thread(Producer);
            p.Name = "Producer";
            p.Start();
            c = new Thread(Consumer);
            c.Name = "Consumer";
            c.Start();
            desc = "DijKstra's Producer/Consumer 'Cooperating Sequential Process'.  Consumer must see every item produced.";
        }
        public int success(int index)
        {
            return (index + 1) % options.buffer.Length;
        }
        public void Consumer()
        {
            while (true)
            {
                lock (_lockObject)
                {
                    head = success(head);
                    var item = options.buffer[head];
                    if (item != -1)
                    {
                        flow.nextMsg = "Consumer: = " + item.ToString();
                        options.buffer[head] = -1;
                    }
                }
                if (terminateConsumer) break;
                cv.Cv2.WaitKey();
            }
        }
        void Producer()
        {
            while (true)
            {
                lock (_lockObject)
                {
                    tail = success(tail);
                    if (options.buffer[tail] == -1)
                    {
                        flow.nextMsg = "producer: = " + frameCount.ToString();
                        options.buffer[tail] = frameCount;
                        frameCount += 1;
                    }
                }
                if (terminateProducer) break;
                cv.Cv2.WaitKey();
            }
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.buffer.Length != options.bufferSize)
            {
                lock (_lockObject)
                {
                    options.buffer = new int[options.bufferSize];
                    options.buffer = Enumerable.Repeat(-1, options.buffer.Length).ToArray();
                    frameCount = 0;
                    head = -1;
                    tail = -1;
                };
            }
            lock (_lockObject)
            {
                flow.Run(empty);
            }
        }
        public void Close()
        {
            terminateProducer = true;
            terminateConsumer = true;
        }
    }
    public class CS_ProCon_Variation : CS_Parent
    {
        readonly object _lockObject = new object();
        ProCon_Basics procon;
        int frameCount;
        public CS_ProCon_Variation(VBtask task) : base(task)
        {
            procon = new ProCon_Basics();
            procon.terminateProducer = true; // we don't need a 2 producer task.  RunVB below provides the second thread.
            desc = "DijKstra's Producer/Consumer - similar to Basics above but producer is the algorithm thread.";
        }
        public void RunCS(Mat src)
        {
            lock (_lockObject)
            {
                procon.tail = procon.success((int)procon.tail);
                if (procon.options.buffer[(int)procon.tail] == -1)
                {
                    procon.flow.nextMsg = "producer: = " + frameCount.ToString();
                    procon.options.buffer[(int)procon.tail] = frameCount;
                    frameCount += 1;
                }
            }

            procon.Run(src);
        }
        public void Close()
        {
            procon.terminateConsumer = true;
            procon.terminateProducer = true;
        }
    }
    public class CS_Profile_Basics : CS_Parent
    {
        public Point3f ptLeft, ptRight, ptTop, ptBot, ptFront, ptBack;
        public List<string> cornerNames = new List<string> { "   First (white)", "   Left (light blue)", "   Right (red)", "   Top (green)",
                                                           "   Bottom (white)", "   Front (yellow)", "   Back (blue)" };
        public List<Scalar> cornerColors = new List<Scalar> { Scalar.White, Scalar.LightBlue, Scalar.Red, Scalar.Green,
                                                             Scalar.White, Scalar.Yellow, Scalar.Blue };
        public List<cv.Point3f> corners3D = new List<cv.Point3f>();
        public List<cv.Point> corners = new List<cv.Point>();
        public List<cv.Point> cornersRaw = new List<cv.Point>();
        public RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Profile_Basics(VBtask task) : base(task)
        {
            desc = "Find the left/right, top/bottom, and near/far sides of a cell";
        }
        string point3fToString(Point3f v)
        {
            return string.Format("{0}\t{1}\t{2}", v.X.ToString(fmt3), v.Y.ToString(fmt3), v.Z.ToString(fmt3));
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            var rc = task.rc;
            if (rc.depthPixels == 0)
            {
                strOut = "There is no depth data for that cell.";
                return;
            }
            if (rc.contour.Count() < 4) return;
            dst3.SetTo(0);
            DrawContour(dst3[rc.rect], rc.contour, Scalar.Yellow);
            var sortLeft = new SortedList<int, int>(new compareAllowIdenticalInteger());
            var sortTop = new SortedList<int, int>(new compareAllowIdenticalInteger());
            var sortFront = new SortedList<int, int>(new compareAllowIdenticalInteger());
            var sort2Dleft = new SortedList<int, int>(new compareAllowIdenticalInteger());
            var sort2Dtop = new SortedList<int, int>(new compareAllowIdenticalInteger());
            rc.contour3D = new List<cv.Point3f>();
            for (int i = 0; i < rc.contour.Count(); i++)
            {
                var pt = rc.contour[i];
                var vec = task.pointCloud[rc.rect].Get<cv.Point3f>(pt.Y, pt.X);
                if (float.IsNaN(vec.Z) || float.IsInfinity(vec.Z)) continue;
                if (vec.Z > 0)
                {
                    sortLeft.Add(pt.X, i);
                    sortTop.Add(pt.Y, i);
                    sortFront.Add((int)(vec.Z * 1000), i);
                    rc.contour3D.Add(vec);
                }
                else
                {
                    sort2Dleft.Add(pt.X, i);
                    sort2Dtop.Add(pt.Y, i);
                }
            }
            if (sortLeft.Count() == 0)
            {
                sortLeft = sort2Dleft;
                sortTop = sort2Dtop;
            }
            corners3D.Clear();
            corners.Clear();
            cornersRaw.Clear();
            corners.Add(new cv.Point(rc.rect.X + rc.contour[0].X, rc.rect.Y + rc.contour[0].Y)); // show the first contour point...
            cornersRaw.Add(rc.contour[0]); // show the first contour point...
            corners3D.Add(task.pointCloud.Get<cv.Point3f>(rc.rect.Y + rc.contour[0].Y, rc.rect.X + rc.contour[0].X));
            for (int i = 0; i < 6; i++)
            {
                int index = 0;
                if (i == 1) index = sortLeft.Count() - 1;
                if (i == 2) index = 0;
                if (i == 3) index = sortFront.Count() - 1;
                var ptList = sortLeft;
                if (i == 1) ptList = sortLeft;
                if (i == 2) ptList = sortTop;
                if (i == 3) ptList = sortTop;
                if (i == 4) ptList = sortFront;
                if (i == 5) ptList = sortFront;
                if (ptList.Count() > 0)
                {
                    var pt = rc.contour[ptList.ElementAt(index).Value];
                    cornersRaw.Add(pt);
                    corners.Add(new cv.Point(rc.rect.X + pt.X, rc.rect.Y + pt.Y));
                    corners3D.Add(task.pointCloud[rc.rect].Get<cv.Point3f>(pt.Y, pt.X));
                }
            }
            for (int i = 0; i < corners.Count(); i++)
            {
                DrawCircle(dst3, corners[i], task.DotSize + 2, cornerColors[i]);
            }
            if (task.heartBeat)
            {
                strOut = "X\tY\tZ\tunits=meters\n";
                var w = task.gridSize;
                for (int i = 0; i < corners.Count(); i++)
                {
                    strOut += point3fToString(corners3D[i]) + "\t" + cornerNames[i] + "\n";
                }
                strOut += "\nThe contour may show points further away but they don't have depth.";
                if (sortFront.Count() == 0) strOut += "\nNone of the contour points had depth.";
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Profile_Rotation : CS_Parent
    {
        public IMU_GMatrix gMat = new IMU_GMatrix();
        public string strMsg = "Then use the 'Options_IMU' sliders to rotate the cell\n" +
                               "It is a common mistake to the OpenGL sliders to try to move cell but they don't - use 'Options_IMU' sliders";
        Options_IMU options = new Options_IMU();
        TrackBar ySlider;
        public CS_Profile_Rotation(VBtask task) : base(task)
        {
            ySlider = FindSlider("Rotate pointcloud around Y-axis");
            if (standaloneTest()) task.gOptions.setGravityUsage(false);
            labels[2] = "Top matrix is the current gMatrix while the bottom one includes the Y-axis rotation.";
            desc = "Build the rotation matrix around the Y-axis";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                ySlider.Value += 1;
                if (ySlider.Value == ySlider.Maximum) ySlider.Value = ySlider.Minimum;
                SetTrueText("When running standaloneTest(), the Y-axis slider is rotating from -90 to 90.", 3);
            }
            gMat.Run(src);
            if (standaloneTest())
            {
                options.RunVB();
                strOut = "Gravity-oriented gMatrix\n";
                strOut += task.gMat.strOut + "\n";
                strOut += "\nNew gMatrix from sliders\n";
                strOut += gMatrixToStr(gMat.gMatrix) + "\n\n";
                strOut += "Angle X = " + options.rotateX.ToString(fmt1) + "\n";
                strOut += "Angle Y = " + options.rotateY.ToString(fmt1) + "\n";
                strOut += "Angle Z = " + options.rotateZ.ToString(fmt1) + "\n";
                SetTrueText(strOut + "\n\n" + strMsg);
            }
        }
    }
    public class CS_Profile_Derivative : CS_Parent
    {
        public Profile_Basics sides = new Profile_Basics();
        List<trueText> saveTrueText = new List<trueText>();
        public CS_Profile_Derivative(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "", "Select a cell to analyze its contour", "Selected cell:  yellow = closer, blue = farther, white = no depth" };
            desc = "Visualize the derivative of X, Y, and Z in the contour of a RedCloud cell";
        }
        public void RunCS(Mat src)
        {
            sides.Run(src);
            dst2 = sides.dst2;
            var rc = task.rc;
            int offset = 30;
            float rsizeX = (dst2.Width - offset * 2) / (float)rc.rect.Width;
            float rsizeY = (dst2.Height - offset * 2) / (float)rc.rect.Height;
            saveTrueText.Clear();
            task.trueData.Clear();
            dst3.SetTo(0);
            Scalar color, near = Scalar.Yellow, far = Scalar.Blue;
            if (rc.index > 0)
            {
                for (int i = 0; i < rc.contour.Count(); i++)
                {
                    var pt = rc.contour[i];
                    var vec = task.pointCloud[rc.rect].Get<cv.Point3f>(pt.Y, pt.X);
                    pt = new cv.Point(pt.X * rsizeX + offset, pt.Y * rsizeY + offset);
                    float t = (rc.maxVec.Z == 0) ? 0 : (vec.Z - rc.minVec.Z) / (rc.maxVec.Z - rc.minVec.Z);
                    if (vec.Z > 0 && t > 0)
                    {
                        byte b = (byte)((1 - t) * near[0] + t * far[0]);
                        byte g = (byte)((1 - t) * near[1] + t * far[1]);
                        byte r = (byte)((1 - t) * near[2] + t * far[2]);
                        color = new Scalar(b, g, r);
                    }
                    else
                    {
                        color = Scalar.White;
                    }
                    DrawCircle(dst3, pt, task.DotSize, color);
                    if (sides.cornersRaw.Contains(rc.contour[i]))
                    {
                        int index = sides.cornersRaw.IndexOf(rc.contour[i]);
                        DrawCircle(dst1, pt, task.DotSize + 5, Scalar.White);
                        DrawCircle(dst1, pt, task.DotSize + 3, sides.cornerColors[index]);
                        SetTrueText(sides.cornerNames[index], pt, 3);
                    }
                }
            }
            strOut = "Points are presented clockwise starting at White dot (leftmost top point)\n" +
                     "yellow = closer, blue = farther,\n\n" + sides.strOut;
            dst1 = sides.dst3.Clone();
            for (int i = 0; i < sides.corners.Count(); i++)
            {
                color = sides.cornerColors[i];
                SetTrueText(sides.cornerNames[i], sides.corners[i], 1);
                DrawCircle(dst1, sides.corners[i], task.DotSize, color);
            }
            SetTrueText(strOut, 1);
            saveTrueText = new List<trueText>(trueData);
            if (saveTrueText != null) trueData = new List<trueText>(saveTrueText);
        }
    }
    public class CS_Profile_ConcentrationSide : CS_Parent
    {
        Profile_ConcentrationTop profile = new Profile_ConcentrationTop();
        public CS_Profile_ConcentrationSide(VBtask task) : base(task)
        {
            FindCheckBox("Top View (Unchecked Side View)").Checked = false;
            labels = new string[] { "", "The outline of the selected RedCloud cell", traceName + " - click any RedCloud cell to visualize it's side view in the upper right image.", "" };
            desc = "Rotate around Y-axis to find peaks - this algorithm fails to find the optimal rotation to find walls";
        }
        public void RunCS(Mat src)
        {
            profile.Run(src);
            dst1 = profile.dst1;
            dst2 = profile.dst2;
            dst3 = profile.dst3;
            labels[3] = profile.labels[3];
        }
    }
    public class CS_Profile_ConcentrationTop : CS_Parent
    {
        Plot_OverTimeSingle plot = new Plot_OverTimeSingle();
        Profile_Rotation rotate = new Profile_Rotation();
        public Profile_Basics sides = new Profile_Basics();
        HeatMap_Basics heat = new HeatMap_Basics();
        Options_HeatMap options = new Options_HeatMap();
        float maxAverage;
        int peakRotation;
        TrackBar ySlider;
        public CS_Profile_ConcentrationTop(VBtask task) : base(task)
        {
            ySlider = FindSlider("Rotate pointcloud around Y-axis (degrees)");
            task.gOptions.setGravityUsage(false);
            task.gOptions.setDisplay1();
            desc = "Rotate around Y-axis to find peaks - this algorithm fails to find the optimal rotation to find walls";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            sides.Run(src);
            dst2 = sides.dst2;
            var rc = task.rc;
            if (rc.contour3D.Count() == 0)
            {
                SetTrueText("The selected cell has no 3D data.  The 3D data can only be computed from cells with depth data.", 1);
                return;
            }
            var vecMat = new Mat(rc.contour3D.Count(), 1, MatType.CV_32FC3, rc.contour3D.ToArray());
            ySlider.Value += 1;
            rotate.Run(empty);
            Mat output = (vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * rotate.gMat.gMatrix);  // <<< this is the XYZ-axis rotation...
            vecMat = output.Reshape(3, vecMat.Rows);
            heat.Run(vecMat);
            if (options.topView)
            {
                dst1 = heat.dst0.Threshold(0, 255, ThresholdTypes.Binary);
            }
            else
            {
                dst1 = heat.dst1.Threshold(0, 255, ThresholdTypes.Binary);
            }
            var count = dst1.CountNonZero();
            if (maxAverage < count)
            {
                maxAverage = count;
                peakRotation = ySlider.Value;
            }
            plot.plotData = count;
            plot.Run(empty);
            dst3 = plot.dst2;
            if (ySlider.Value >= 45)
            {
                maxAverage = 0;
                peakRotation = -45;
                ySlider.Value = -45;
            }
            labels[3] = "Peak cell concentration in the histogram = " + ((int)maxAverage).ToString() + " at " + peakRotation.ToString() + " degrees";
        }
    }
    public class CS_Profile_OpenGL : CS_Parent
    {
        Profile_Basics sides = new Profile_Basics();
        public Profile_Rotation rotate = new Profile_Rotation();
        HeatMap_Basics heat = new HeatMap_Basics();
        public CS_Profile_OpenGL(VBtask task) : base(task)
        {
            dst0 = new Mat(dst0.Size(), MatType.CV_32FC3, 0);
            if (standaloneTest()) task.gOptions.setGravityUsage(false);
            task.ogl.options.PointSizeSlider.Value = 10;
            task.ogl.oglFunction = (int)oCase.pcPointsAlone;
            desc = "Visualize just the RedCloud cell contour in OpenGL";
        }
        public void RunCS(Mat src)
        {
            sides.Run(src);
            dst2 = sides.dst2;
            dst3 = sides.dst3;
            var rc = task.rc;
            if (rc.contour3D.Count() > 0)
            {
                Mat vecMat = new Mat(rc.contour3D.Count(), 1, MatType.CV_32FC3, rc.contour3D.ToArray());
                rotate.Run(empty);
                Mat output = vecMat.Reshape(1, vecMat.Rows * vecMat.Cols) * rotate.gMat.gMatrix;  // <<<<<<<<<<<<<<<<<<<<<<< this is the XYZ-axis rotation...
                task.ogl.dataInput = output.Reshape(3, vecMat.Rows);
                task.ogl.pointCloudInput = new Mat();
                task.ogl.Run(new Mat());
                heat.Run(vecMat);
                dst1 = heat.dst0.Threshold(0, 255, ThresholdTypes.Binary);
            }
            SetTrueText("Select a RedCloud Cell to display the contour in OpenGL." + "\n" + rotate.strMsg, 3);
        }
    }
    public class CS_Profile_Kalman : CS_Parent
    {
        Profile_Basics sides = new Profile_Basics();
        Kalman_Basics kalman = new Kalman_Basics();
        public CS_Profile_Kalman(VBtask task) : base(task)
        {
            kalman.kInput = new float[12];
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "", "Profile_Basics output without Kalman", "Profile_Basics output with Kalman" };
            desc = "Use Kalman to smooth the results of the contour key points";
        }
        public void RunCS(Mat src)
        {
            sides.Run(src);
            dst0 = sides.redC.dst0;
            dst1 = sides.dst2;
            dst2 = sides.dst3;
            var rc = task.rc;
            if (kalman.kInput.Length != sides.corners.Count() * 2) Array.Resize(ref kalman.kInput, sides.corners.Count() * 2);
            for (int i = 0; i < sides.corners.Count(); i++)
            {
                kalman.kInput[i * 2] = sides.corners[i].X;
                kalman.kInput[i * 2 + 1] = sides.corners[i].Y;
            }
            kalman.Run(empty);
            if (rc.index > 0)
            {
                dst3.SetTo(0);
                DrawContour(dst3[rc.rect], rc.contour, Scalar.Yellow);
                for (int i = 0; i < sides.corners.Count(); i++)
                {
                    var pt = new cv.Point((int)kalman.kOutput[i * 2], (int)kalman.kOutput[i * 2 + 1]);
                    DrawCircle(dst3, pt, task.DotSize + 2, sides.cornerColors[i]);
                }
            }
            SetTrueText(sides.strOut, 3);
            SetTrueText("Select a cell in the upper right image", 2);
        }
    }
    public class CS_Puzzle_Basics : CS_Parent
    {
        public List<cv.Rect> scrambled = new List<cv.Rect>(); // this is every roi regardless of size.
        public List<cv.Rect> unscrambled = new List<cv.Rect>(); // this is every roi regardless of size.
        public Mat image = new Mat();
        public CS_Puzzle_Basics(VBtask task) : base(task)
        {
            desc = "Create the puzzle pieces to solve with correlation.";
        }
        public List<T> Shuffle<T>(IEnumerable<T> collection)
        {
            Random r = new Random();
            return collection.OrderBy(a => r.Next()).ToList();
        }
        public void RunCS(Mat src)
        {
            unscrambled.Clear();
            List<cv.Rect> inputROI = new List<cv.Rect>();
            for (int j = 0; j < task.gridList.Count(); j++)
            {
                var roi = task.gridList[j];
                if (roi.Width == task.gridSize && roi.Height == task.gridSize)
                    inputROI.Add(task.gridList[j]);
            }
            scrambled = Shuffle(inputROI);
            image = src.Clone();
            // display image with shuffled roi's
            for (int i = 0; i < scrambled.Count(); i++)
            {
                var roi = task.gridList[i];
                var roi2 = scrambled[i];
                if (roi.Width == task.gridSize && roi.Height == task.gridSize &&
                    roi2.Width == task.gridSize && roi2.Height == task.gridSize)
                    dst2[roi2] = src[roi];
            }
        }
    }
    public class CS_Puzzle_Solver : CS_Parent
    {
        public Puzzle_Basics puzzle = new Puzzle_Basics();
        List<cv.Rect> solution = new List<cv.Rect>();
        Match_Basics match = new Match_Basics();
        public Mat grayMat;
        int puzzleIndex;
        Options_Puzzle options = new Options_Puzzle();
        public CS_Puzzle_Solver(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setGridSize(8);
            labels = new string[] { "", "", "Puzzle Input", "Puzzle Solver Output - missing pieces can result from identical cells (usually bright white)" };
            desc = "Solve the puzzle using matchTemplate";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.optionsChanged || options.startPuzzle)
            {
                puzzle.Run(src);
                dst2 = puzzle.dst2;
                dst3.SetTo(0);
                grayMat = puzzle.image.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
                puzzleIndex = 0;
            }
            if (puzzle.scrambled.Count() > puzzle.unscrambled.Count())
            {
                // find one piece of the puzzle on each iteration.
                var rect = puzzle.scrambled[puzzleIndex];
                match.template = grayMat[rect];
                match.Run(grayMat);
                var bestRect = ValidateRect(new cv.Rect(match.matchCenter.X, match.matchCenter.Y, rect.Width, rect.Height));
                puzzle.unscrambled.Add(bestRect);
                puzzleIndex++;
                dst3[bestRect] = puzzle.image[bestRect];
            }
        }
    }
    public class CS_Puzzle_SolverDynamic : CS_Parent
    {
        Puzzle_Solver puzzle = new Puzzle_Solver();
        public CS_Puzzle_SolverDynamic(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setGridSize(8);
            labels = new string[] { "", "", "Latest Puzzle input image", "Puzzle Solver Output - missing pieces can occur because of motion or when cells are identical." };
            desc = "Instead of matching the original image as Puzzle_Solver, match the latest image from the camera.";
        }
        public void RunCS(Mat src)
        {
            puzzle.puzzle.image = src.Clone();
            puzzle.grayMat = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            puzzle.Run(src);
            dst2 = puzzle.dst2;
            dst3 = puzzle.dst3;
        }
    }
    public class CS_Pyramid_Basics : CS_Parent
    {
        Options_Pyramid options = new Options_Pyramid();
        public CS_Pyramid_Basics(VBtask task) : base(task)
        {
            desc = "Use pyrup and pyrdown to zoom in and out of an image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.zoom != 0)
            {
                if (options.zoom < 0)
                {
                    var tmp = src.PyrDown(new cv.Size(src.Cols / 2, src.Rows / 2));
                    var roi = new cv.Rect((src.Cols - tmp.Cols) / 2, (src.Rows - tmp.Rows) / 2, tmp.Width, tmp.Height);
                    dst2[roi] = tmp;
                }
                else
                {
                    var tmp = src.PyrUp(new cv.Size(src.Cols * 2, src.Rows * 2));
                    var roi = new cv.Rect((tmp.Cols - src.Cols) / 2, (tmp.Rows - src.Rows) / 2, src.Width, src.Height);
                    dst2 = tmp[roi];
                }
            }
            else
            {
                src.CopyTo(dst2);
            }
        }
    }
    public class CS_Pyramid_Filter : CS_Parent
    {
        Laplacian_PyramidFilter laplace = new Laplacian_PyramidFilter();
        public CS_Pyramid_Filter(VBtask task) : base(task)
        {
            desc = "Link to Laplacian_PyramidFilter that uses pyrUp and pyrDown extensively";
        }
        public void RunCS(Mat src)
        {
            laplace.Run(src);
            dst2 = laplace.dst2;
        }
    }
    public class CS_PyrFilter_Basics : CS_Parent
    {
        Options_PyrFilter options = new Options_PyrFilter();
        public CS_PyrFilter_Basics(VBtask task) : base(task)
        {
            desc = "Use PyrMeanShiftFiltering to segment an image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Cv2.PyrMeanShiftFiltering(src, dst2, options.radius, options.color, options.maxPyramid);
        }
    }
    public class CS_PyrFilter_RedCloud : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Reduction_Basics reduction = new Reduction_Basics();
        PyrFilter_Basics pyr = new PyrFilter_Basics();
        public CS_PyrFilter_RedCloud(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "RedCloud_Basics output", "PyrFilter output before reduction" };
            desc = "Use RedColor to segment the output of PyrFilter";
        }
        public void RunCS(Mat src)
        {
            pyr.Run(src);
            dst3 = pyr.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            reduction.Run(dst3);
            redC.Run(reduction.dst2);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
        }
    }
    public class CS_Python_Basics : CS_Parent
    {
        public bool StartPython(string arguments)
        {
            var pythonApp = new FileInfo(task.pythonTaskName);
            if (pythonApp.Exists)
            {
                task.pythonProcess = new Process();
                task.pythonProcess.StartInfo.FileName = "python";
                task.pythonProcess.StartInfo.WorkingDirectory = pythonApp.DirectoryName;
                if (string.IsNullOrEmpty(arguments))
                {
                    task.pythonProcess.StartInfo.Arguments = "\"" + pythonApp.Name + "\"";
                }
                else
                {
                    task.pythonProcess.StartInfo.Arguments = "\"" + pythonApp.Name + "\" " + arguments;
                }
                Console.WriteLine("Starting Python with the following command:\n" + task.pythonProcess.StartInfo.Arguments + "\n");
                if (!task.showConsoleLog) task.pythonProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                try
                {
                    task.pythonProcess.Start();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("The python algorithm " + pythonApp.Name + 
                                                         " failed with " + ex.Message + ".  Is python in the path?");
                }
            }
            else
            {
                if (pythonApp.Name.EndsWith("Python_MemMap") || pythonApp.Name.EndsWith("Python_Run"))
                {
                    strOut = pythonApp.Name + " is a support algorithm for PyStream apps.";
                }
                else
                {
                    strOut = pythonApp.FullName + " is missing.";
                }
                return false;
            }
            return true;
        }
        public CS_Python_Basics(VBtask task) : base(task)
        {
            desc = "Access Python from OpenCVB - contains the startPython interface";
        }
        public void RunCS(Mat src)
        {
            SetTrueText("There is no output from " + traceName + ".  It contains the interface to python.");
        }
    }
    public class CS_Python_Run : CS_Parent
    {
        Python_Basics python = new Python_Basics();
        public Python_Stream pyStream;
        FileInfo pythonApp;
        bool testPyStreamOakD = false; // set this to true to test the PyStream problem with the OakD Python camera
        public void OakDPipeIssue()
        {
            SetTrueText("Python Stream ('_PS.py') algorithms don't work reliably when using the Oak-D Python camera interface.\n" +
                        "They both use named pipes to communicate between OpenCVB and the external processes (a camera and a Python algorithm.)\n" +
                        "To experiment with Python Stream algorithms, any of the other supported cameras work fine.\n" +
                        "To see the problem: comment out the camera test in RunVB below to test any '_PS.py' algorithm.  It may work but\n" +
                        "if you move the algorithm window (separate from OpenCVB), the algorithm will hang.  More importantly,\n" +
                        "several of the algorithms just hang without moving the window.  Any suggestions would be gratefully received.\n" +
                        "Using another camera is the best option to observe all the Python Stream algorithms.");
        }
        public CS_Python_Run(VBtask task) : base(task)
        {
            pythonApp = new FileInfo(task.pythonTaskName);
            if (pythonApp.Name.EndsWith("_PS.py"))
            {
                if (testPyStreamOakD)
                {
                    pyStream = new Python_Stream();
                }
                else
                {
                    if (task.cameraName != "Oak-D camera") pyStream = new Python_Stream();
                }
            }
            else
            {
                python.StartPython("");
                if (!string.IsNullOrEmpty(python.strOut)) SetTrueText(python.strOut);
            }
            desc = "Run Python app: " + pythonApp.Name;
        }
        public void RunCS(Mat src)
        {
            if (task.cameraName == "Oak-D camera" && pythonApp.Name.EndsWith("_PS.py") && !testPyStreamOakD)
            {
                OakDPipeIssue();
            }
            else
            {
                if (pyStream != null)
                {
                    pyStream.Run(src);
                    dst2 = pyStream.dst2;
                    dst3 = pyStream.dst3;
                    labels[2] = "Output of Python Backend";
                    labels[3] = "Second Output of Python Backend";
                }
                else
                {
                    if (pythonApp.Name == "PyStream.py")
                    {
                        SetTrueText("The PyStream.py algorithm is used by a wide variety of apps but has no output when run by itself.");
                    }
                }
            }
        }
    }
    public class CS_Python_MemMap : CS_Parent
    {
        Python_Basics python = new Python_Basics();
        MemoryMappedViewAccessor memMapWriter;
        MemoryMappedFile memMapFile;
        IntPtr memMapPtr;
        public double[] memMapValues = new double[50]; // more than we need - buffer for growth.  PyStream assumes 400 bytes length!  Do not change without changing everywhere.
        public int memMapbufferSize;
        public CS_Python_MemMap(VBtask task) : base(task)
        {
            memMapbufferSize = Marshal.SizeOf(typeof(double)) * memMapValues.Length;
            memMapPtr = Marshal.AllocHGlobal(memMapbufferSize);
            memMapFile = MemoryMappedFile.CreateOrOpen("CS_Python_MemMap", memMapbufferSize);
            memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize);
            Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length);
            memMapWriter.WriteArray(0, memMapValues, 0, memMapValues.Length);
            if (standaloneTest())
            {
                if (!task.externalPythonInvocation)
                {
                    python.StartPython("--MemMapLength=" + memMapbufferSize.ToString());
                    if (!string.IsNullOrEmpty(python.strOut)) SetTrueText(python.strOut);
                }
                var pythonApp = new FileInfo(task.pythonTaskName);
                SetTrueText("No output for CS_Python_MemMap - see Python console log (see Options/'Show Console Log for external processes' in the main form)");
                desc = "Run Python app: " + pythonApp.Name + " to share memory with OpenCVB and Python.";
            }
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                SetTrueText(traceName + " has no output when run standaloneTest().");
                return;
            }
            memMapValues[0] = task.frameCount;
            Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length);
            memMapWriter.WriteArray(0, memMapValues, 0, memMapValues.Length);
        }
    }
    public class CS_Python_Stream : CS_Parent
    {
        Python_Basics python = new Python_Basics();
        byte[] rgbBuffer = new byte[2];
        byte[] depthBuffer = new byte[2];
        byte[] dst1Buffer = new byte[2];
        byte[] dst2Buffer = new byte[2];
        Python_MemMap memMap;
        public CS_Python_Stream(VBtask task) : base(task)
        {
            task.pipeName = "PyStream2Way" + task.pythonPipeIndex.ToString();
            task.pythonPipeIndex++;
            try
            {
                task.pythonPipeOut = new NamedPipeServerStream(task.pipeName, PipeDirection.Out);
            }
            catch (Exception ex)
            {
                SetTrueText("CS_Python_Stream: pipeOut NamedPipeServerStream failed to open.  Error: " + ex.Message);
                return;
            }
            task.pythonPipeIn = new NamedPipeServerStream(task.pipeName + "Results", PipeDirection.In);
            // Was this class invoked standaloneTest()?  Then just run something that works with BGR and depth...
            if (task.pythonTaskName.EndsWith("CS_Python_Stream"))
            {
                task.pythonTaskName = task.HomeDir + "Python_Classes/CS_Python_Stream_PS.py";
            }
            memMap = new Python_MemMap();
            if (task.externalPythonInvocation)
            {
                task.pythonReady = true; // python was already running and invoked OpenCVB.
            }
            else
            {
                task.pythonReady = python.StartPython("--MemMapLength=" + memMap.memMapbufferSize + " --pipeName=" + task.pipeName);
                if (!string.IsNullOrEmpty(python.strOut)) SetTrueText(python.strOut);
            }
            if (task.pythonReady)
            {
                task.pythonPipeOut.WaitForConnection();
                task.pythonPipeIn.WaitForConnection();
            }
            labels[2] = "Output of Python Backend";
            desc = "General purpose class to pipe BGR and Depth to Python scripts.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                SetTrueText(traceName + " has no output when run standaloneTest().");
                return;
            }
            if (task.pythonReady && task.pcSplit[2].Width > 0)
            {
                Mat depth32f = task.pcSplit[2] * 1000;
                double[] vals = new double[] {task.frameCount, src.Total() * src.ElemSize(),
                                              (double)(depth32f.Total() * depth32f.ElemSize()), src.Rows, src.Cols,
                                              task.drawRect.X, task.drawRect.Y, task.drawRect.Width,
                                              task.drawRect.Height};
                    for (int i = 0; i < memMap.memMapValues.Length; i++)
                {
                    memMap.memMapValues[i] = vals[i];
                }
                memMap.Run(src);
                if (rgbBuffer.Length != src.Total() * src.ElemSize()) Array.Resize(ref rgbBuffer, (int)(src.Total() * src.ElemSize()));
                if (depthBuffer.Length != depth32f.Total() * depth32f.ElemSize()) Array.Resize(ref depthBuffer, (int)(depth32f.Total() * depth32f.ElemSize()));
                if (dst1Buffer.Length != dst2.Total() * dst2.ElemSize()) Array.Resize(ref dst1Buffer, (int)(dst2.Total() * dst2.ElemSize()));
                if (dst2Buffer.Length != dst3.Total() * dst3.ElemSize()) Array.Resize(ref dst2Buffer, (int)(dst3.Total() * dst3.ElemSize()));
                Marshal.Copy(src.Data, rgbBuffer, 0, (int)(src.Total() * src.ElemSize()));
                Marshal.Copy(depth32f.Data, depthBuffer, 0, depthBuffer.Length);
                if (task.pythonPipeOut.IsConnected)
                {
                    try
                    {
                        task.pythonPipeOut.Write(rgbBuffer, 0, rgbBuffer.Length);
                        task.pythonPipeOut.Write(depthBuffer, 0, depthBuffer.Length);
                        task.pythonPipeIn.Read(dst1Buffer, 0, dst1Buffer.Length);
                        task.pythonPipeIn.Read(dst2Buffer, 0, dst2Buffer.Length);
                        Marshal.Copy(dst1Buffer, 0, dst2.Data, dst1Buffer.Length);
                        Marshal.Copy(dst2Buffer, 0, dst3.Data, dst2Buffer.Length);
                    }
                    catch { }
                }
            }
        }
        public void Close()
        {
            if (task.pythonPipeOut != null) task.pythonPipeOut.Close();
            if (task.pythonPipeIn != null) task.pythonPipeIn.Close();
        } 
    }
    public class CS_QRcode_Basics : CS_Parent
    {
        QRCodeDetector qrDecoder = new QRCodeDetector();
        Mat qrInput1 = new Mat();
        Mat qrInput2 = new Mat();
        public CS_QRcode_Basics(VBtask task) : base(task)
        {
            var fileInfo = new FileInfo(task.HomeDir + "data/QRcode1.png");
            if (fileInfo.Exists) qrInput1 = Cv2.ImRead(fileInfo.FullName);
            fileInfo = new FileInfo(task.HomeDir + "Data/QRCode2.png");
            if (fileInfo.Exists) qrInput2 = Cv2.ImRead(fileInfo.FullName);
            if (dst2.Width < 480) // for the smallest configurations the default size can be too big!
            {
                qrInput1 = qrInput1.Resize(new cv.Size(120, 160));
                qrInput2 = qrInput2.Resize(new cv.Size(120, 160));
            }
            desc = "Read a QR code";
        }
        public void RunCS(Mat src)
        {
            if (src.Height < 240)
            {
                SetTrueText("This QR Code test does not run at low resolutions");
                return;
            }
            var x = msRNG.Next(0, src.Width - Math.Max(qrInput1.Width, qrInput2.Width));
            var y = msRNG.Next(0, src.Height - Math.Max(qrInput1.Height, qrInput2.Height));
            if ((task.frameCount / 50) % 2 == 0)
            {
                var roi = new cv.Rect(x, y, qrInput1.Width, qrInput1.Height);
                src[roi] = qrInput1;
            }
            else
            {
                var roi = new cv.Rect(x, y, qrInput2.Width, qrInput2.Height);
                src[roi] = qrInput2;
            }
            Point2f[] box;
            var rectifiedImage = new Mat();
            var refersTo = qrDecoder.DetectAndDecode(src, out box, rectifiedImage);
            src.CopyTo(dst2);
            for (int i = 0; i < box.Length; i++)
            {
                DrawLine(dst2, box[i], box[(i + 1) % 4], Scalar.Red, task.lineWidth + 2);
            }
            if (!string.IsNullOrEmpty(refersTo)) labels[2] = refersTo;
        }
    }
    public class CS_Quadrant_Basics : CS_Parent
    {
        cv.Point p1 = new cv.Point();
        cv.Point p2;
        cv.Point p3;
        cv.Point p4;
        cv.Rect rect = new cv.Rect();
        Mat mask = new Mat();
        public CS_Quadrant_Basics(VBtask task) : base(task)
        {
            p2 = new cv.Point(dst2.Width - 1, 0);
            p3 = new cv.Point(0, dst2.Height - 1);
            p4 = new cv.Point(dst2.Width - 1, dst2.Height - 1);
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            labels[2] = "dst1 contains a map defining the quadrant value for each pixel";
            desc = "Divide the color and depth images into 4 quadrants based on the horizon and gravity vectors";
        }
        public void RunCS(Mat src)
        {
            dst1.SetTo(0);
            DrawLine(dst1, task.gravityVec.p1, task.gravityVec.p2, 255, 1);
            DrawLine(dst1, task.horizonVec.p1, task.horizonVec.p2, 255, 1);
            var flags = FloodFillFlags.FixedRange | (FloodFillFlags)(255 << 8);
            if (dst1.At<byte>(p1.Y, p1.X) == 0) Cv2.FloodFill(dst1, new Mat(), p1, 1 * 255 / 4, out rect, 0, 0, flags);
            if (dst1.At<byte>(p2.Y, p2.X) == 0) Cv2.FloodFill(dst1, new Mat(), p2, 2 * 255 / 4, out rect, 0, 0, flags);
            if (dst1.At<byte>(p3.Y, p3.X) == 0) Cv2.FloodFill(dst1, new Mat(), p3, 3 * 255 / 4, out rect, 0, 0, flags);
            if (dst1.At<byte>(p4.Y, p4.X) == 0) Cv2.FloodFill(dst1, new Mat(), p4, 4 * 255 / 4, out rect, 0, 0, flags);
            dst2 = ShowPalette(dst1);
        }
    }
    public class CS_Quaterion_Basics : CS_Parent
    {
        Options_Quaternion options = new Options_Quaternion();
        public CS_Quaterion_Basics(VBtask task) : base(task)
        {
            desc = "Use the quaternion values to multiply and compute conjugate";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            var quatmul = Quaternion.Multiply(options.q1, options.q2);
            SetTrueText("q1 = " + options.q1.ToString() + "\n" + "q2 = " + options.q2.ToString() + "\n" +
                        "Multiply q1 * q2" + quatmul.ToString());
        }
    }
    public class CS_Quaterion_IMUPrediction : CS_Parent
    {
        IMU_PlotHostFrameTimes host = new IMU_PlotHostFrameTimes();
        public CS_Quaterion_IMUPrediction(VBtask task) : base(task)
        {
            labels[2] = "Quaternion_IMUPrediction";
            labels[3] = "";
            desc = "IMU data arrives at the CPU after a delay.  Predict changes to the image based on delay and motion data.";
        }
        public Quaternion quaternion_exp(Point3f v)
        {
            v *= 0.5f;
            var theta2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z;
            var theta = Math.Sqrt(theta2);
            var c = Math.Cos(theta);
            var s = (theta2 < Math.Sqrt(120 * Single.Epsilon)) ? 1 - theta2 / 6 : Math.Sin(theta) / theta2;
            return new Quaternion((float)(s * v.X), (float)(s * v.Y), (float)(s * v.Z), (float)c);
        }
        public void RunCS(Mat src)
        {
            host.Run(src);
            var dt = host.HostInterruptDelayEstimate;
            var t = task.IMU_Translation;
            var predictedTranslation = new Point3f(
                (float)(dt * (dt / 2 * task.IMU_Acceleration.X + task.IMU_AngularVelocity.X) + t.X),
                (float)(dt * (dt / 2 * task.IMU_Acceleration.Y + task.IMU_AngularVelocity.Y) + t.Y),
                (float)(dt * (dt / 2 * task.IMU_Acceleration.Z + task.IMU_AngularVelocity.Z) + t.Z));
            var predictedW = new Point3f(
                (float)(dt * (dt / 2 * task.IMU_AngularAcceleration.X + task.IMU_AngularVelocity.X)),
                (float)(dt * (dt / 2 * task.IMU_AngularAcceleration.Y + task.IMU_AngularVelocity.Y)),
                (float)(dt * (dt / 2 * task.IMU_AngularAcceleration.Z + task.IMU_AngularVelocity.Z)));
            Quaternion predictedRotation = Quaternion.Multiply(quaternion_exp(predictedW), task.IMU_Rotation);
            var diffq = Quaternion.Subtract(task.IMU_Rotation, predictedRotation);
            SetTrueText("IMU_Acceleration = " + "\t" +
                         string.Format("{0:F3}", task.IMU_Acceleration.X) + "\t" +
                         string.Format("{0:F3}", task.IMU_Acceleration.Y) + "\t" +
                         string.Format("{0:F3}", task.IMU_Acceleration.Z) + "\t" + "\n" +
                         "IMU_AngularAccel. = " + "\t" +
                         string.Format("{0:F3}", task.IMU_AngularAcceleration.X) + "\t" +
                         string.Format("{0:F3}", task.IMU_AngularAcceleration.Y) + "\t" +
                         string.Format("{0:F3}", task.IMU_AngularAcceleration.Z) + "\t" + "\n" +
                         "IMU_AngularVelocity = " + "\t" +
                         string.Format("{0:F3}", task.IMU_AngularVelocity.X) + "\t" +
                         string.Format("{0:F3}", task.IMU_AngularVelocity.Y) + "\t" +
                         string.Format("{0:F3}", task.IMU_AngularVelocity.Z) + "\t" + "\n" + "\n" +
                         "dt = " + dt.ToString() + "\n" + "\n" +
                         "Pose quaternion = " + "\t" +
                         string.Format("{0:F3}", task.IMU_Rotation.X) + "\t" +
                         string.Format("{0:F3}", task.IMU_Rotation.Y) + "\t" +
                         string.Format("{0:F3}", task.IMU_Rotation.Z) + "\t" + "\n" +
                         "Prediction Rotation = " + "\t" +
                         string.Format("{0:F3}", predictedRotation.X) + "\t" +
                         string.Format("{0:F3}", predictedRotation.Y) + "\t" +
                         string.Format("{0:F3}", predictedRotation.Z) + "\t" + "\n" +
                         "difference = " + "\t" + "\t" +
                         string.Format("{0:F3}", diffq.X) + "\t" +
                         string.Format("{0:F3}", diffq.Y) + "\t" +
                         string.Format("{0:F3}", diffq.Z) + "\t");
        }
    }
    public class CS_Random_Basics : CS_Parent
    {
        public List<cv.Point2f> PointList = new List<cv.Point2f>();
        public cv.Rect range;
        public Options_Random options = new Options_Random();

        public CS_Random_Basics(VBtask task) : base(task)
        {
            range = new cv.Rect(0, 0, dst2.Cols, dst2.Rows);
            desc = "Create a uniform random mask with a specified number of pixels.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            int sizeRequest = options.count;
            if (!task.paused)
            {
                PointList.Clear();
                Random msRNG = new Random();
                while (PointList.Count < sizeRequest)
                {
                    PointList.Add(new cv.Point2f(msRNG.Next(range.X, range.X + range.Width),
                                              msRNG.Next(range.Y, range.Y + range.Height)));
                }
                if (standaloneTest())
                {
                    dst2.SetTo(0);
                    foreach (var pt in PointList)
                    {
                        DrawCircle(dst2, pt, task.DotSize, Scalar.Yellow);
                    }
                }
            }
        }
    }
    public class CS_Random_Point2d : CS_Parent
    {
        public List<cv.Point2d> PointList { get; } = new List<cv.Point2d>();
        public cv.Rect range;
        Options_Random options = new Options_Random();
        public CS_Random_Point2d(VBtask task) : base(task)
        {
            range = new cv.Rect(0, 0, dst2.Cols, dst2.Rows);
            desc = "Create a uniform random mask with a specificied number of pixels.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            PointList.Clear();
            if (!task.paused)
            {
                for (int i = 0; i < options.count; i++)
                {
                    PointList.Add(new Point2d(msRNG.Next(range.X, range.X + range.Width), msRNG.Next(range.Y, range.Y + range.Height)));
                }
                if (standaloneTest())
                {
                    dst2.SetTo(0);
                    foreach (var pt in PointList)
                    {
                        DrawCircle(dst2, new cv.Point2f((float)pt.X, (float)pt.Y), task.DotSize, Scalar.Yellow, -1);
                    }
                }
            }
        }
    }
    public class CS_Random_Enumerable : CS_Parent
    {
        public Options_Random options = new Options_Random();
        public Point2f[] points;
        public CS_Random_Enumerable(VBtask task) : base(task)
        {
            FindSlider("Random Pixel Count").Value = 100;
            desc = "Create an enumerable list of points using a lambda function";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            points = Enumerable.Range(0, options.count).Select(i =>
                new Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))).ToArray();
            dst2.SetTo(0);
            foreach (var pt in points)
            {
                DrawCircle(dst2, pt, task.DotSize, Scalar.Yellow);
            }
        }
    }
    public class CS_Random_Basics3D : CS_Parent
    {
        public Point3f[] Points3f;
        Options_Random options = new Options_Random();
        public List<cv.Point3f> PointList { get; } = new List<cv.Point3f>();
        public float[] ranges;
        public CS_Random_Basics3D(VBtask task) : base(task)
        {
            ranges = new float[] { 0, dst2.Width, 0, dst2.Height, 0, task.MaxZmeters, 0, task.MaxZmeters };
            FindSlider("Random Pixel Count").Value = 20;
            FindSlider("Random Pixel Count").Maximum = dst2.Cols * dst2.Rows;
            desc = "Create a uniform random mask with a specificied number of pixels.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            PointList.Clear();
            if (!task.paused)
            {
                for (int i = 0; i < options.count; i++)
                {
                    PointList.Add(new Point3f(msRNG.Next((int)ranges[0], (int)ranges[1]),
                                                          msRNG.Next((int)ranges[2], (int)ranges[3]),
                                                          msRNG.Next((int)ranges[4], (int)ranges[5])));
                }
                if (standaloneTest())
                {
                    dst2.SetTo(0);
                    foreach (var pt in PointList)
                    {
                        DrawCircle(dst2, new Point2f(pt.X, pt.Y), task.DotSize, Scalar.Yellow);
                    }
                }
                Points3f = PointList.ToArray();
            }
        }
    }
    public class CS_Random_Basics4D : CS_Parent
    {
        public Vec4f[] vec4f;
        public List<Vec4f> PointList { get; } = new List<Vec4f>();
        public float[] ranges;
        Options_Random options = new Options_Random();
        System.Windows.Forms.TrackBar countSlider;
        public CS_Random_Basics4D(VBtask task) : base(task)
        {
            ranges = new float[] { 0, dst2.Width, 0, dst2.Height, 0, task.MaxZmeters, 0, task.MaxZmeters };
            desc = "Create a uniform random mask with a specificied number of pixels.";
            countSlider = FindSlider("Random Pixel Count");
        }
        public void RunCS(Mat src)
        {
            PointList.Clear();
            var count = countSlider.Value;
            if (!task.paused)
            {
                for (int i = 0; i < count; i++)
                {
                    PointList.Add(new Vec4f(msRNG.Next((int)ranges[0], (int)ranges[1]),
                                                 msRNG.Next((int)ranges[2], (int)ranges[3]),
                                                 msRNG.Next((int)ranges[4], (int)ranges[5]),
                                                 msRNG.Next((int)ranges[6], (int)ranges[7])));
                }
                if (standaloneTest())
                {
                    dst2.SetTo(0);
                    foreach (var v in PointList)
                    {
                        DrawCircle(dst2, new Point2f(v[0], v[1]), task.DotSize, Scalar.Yellow);
                    }
                }
                vec4f = PointList.ToArray();
            }
        }
    }
    public class CS_Random_Shuffle : CS_Parent
    {
        RNG myRNG = new RNG();
        public CS_Random_Shuffle(VBtask task) : base(task)
        {
            desc = "Use randomShuffle to reorder an image.";
        }
        public void RunCS(Mat src)
        {
            src.CopyTo(dst2);
            Cv2.RandShuffle(dst2, 1.0, ref myRNG); // don't remove that myRNG!  It will fail in RandShuffle.
            labels[2] = "Random_shuffle - wave at camera";
        }
    }
    public class CS_Random_LUTMask : CS_Parent
    {
        Random_Basics random = new Random_Basics();
        KMeans_Image km = new KMeans_Image();
        Mat lutMat;
        public CS_Random_LUTMask(VBtask task) : base(task)
        {
            desc = "Use a random Look-Up-Table to modify few colors in a kmeans image.";
            labels[3] = "kmeans run to get colors";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat || task.frameCount < 10)
            {
                random.Run(empty);
                lutMat = new Mat(new cv.Size(1, 256), MatType.CV_8UC3, 0);
                int lutIndex = 0;
                km.Run(src);
                dst2 = km.dst2;
                foreach (var pt in random.PointList)
                {
                    lutMat.Set(lutIndex, 0, dst2.Get<Vec3b>((int)pt.Y, (int)pt.X));
                    lutIndex++;
                    if (lutIndex >= lutMat.Rows) break;
                }
            }
            dst3 = src.LUT(lutMat);
            labels[2] = "Using kmeans colors with interpolation";
        }
    }
    public class CS_Random_UniformDist : CS_Parent
    {
        double minVal = 0, maxVal = 255;
        public CS_Random_UniformDist(VBtask task) : base(task)
        {
            desc = "Create a uniform distribution.";
        }
        public void RunCS(Mat src)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            Cv2.Randu(dst2, minVal, maxVal);
        }
    }
    public class CS_Random_NormalDist : CS_Parent
    {
        Options_NormalDist options = new Options_NormalDist();
        public CS_Random_NormalDist(VBtask task) : base(task)
        {
            desc = "Create a normal distribution in all 3 colors with a variable standard deviation.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.grayChecked && dst2.Channels() != 1) dst2 = new Mat(dst2.Size(), MatType.CV_8U);
            Cv2.Randn(dst2, new Scalar(options.blueVal, options.greenVal, options.redVal), Scalar.All(options.stdev));
        }
    }
    public class CS_Random_CheckUniformSmoothed : CS_Parent
    {
        Hist_Basics histogram = new Hist_Basics();
        Random_UniformDist rUniform = new Random_UniformDist();
        public CS_Random_CheckUniformSmoothed(VBtask task) : base(task)
        {
            desc = "Display the smoothed histogram for a uniform distribution.";
        }
        public void RunCS(Mat src)
        {
            rUniform.Run(src);
            dst2 = rUniform.dst2;
            histogram.plot.maxRange = 255;
            histogram.Run(dst2);
            dst3 = histogram.dst2;
        }
    }
    public class CS_Random_CheckUniformDist : CS_Parent
    {
        Hist_Graph histogram = new Hist_Graph();
        Random_UniformDist rUniform = new Random_UniformDist();
        public CS_Random_CheckUniformDist(VBtask task) : base(task)
        {
            desc = "Display the histogram for a uniform distribution.";
        }
        public void RunCS(Mat src)
        {
            rUniform.Run(src);
            dst2 = rUniform.dst2;
            histogram.plotRequested = true;
            histogram.Run(dst2);
            dst3 = histogram.dst2;
        }
    }
    public class CS_Random_CheckNormalDist : CS_Parent
    {
        Hist_Graph histogram = new Hist_Graph();
        Random_NormalDist normalDist = new Random_NormalDist();
        public CS_Random_CheckNormalDist(VBtask task) : base(task)
        {
            desc = "Display the histogram for a Normal distribution.";
        }
        public void RunCS(Mat src)
        {
            normalDist.Run(src);
            dst3 = normalDist.dst2;
            histogram.plotRequested = true;
            histogram.Run(dst3);
            dst2 = histogram.dst2;
        }
    }
    public class CS_Random_CheckNormalDistSmoothed : CS_Parent
    {
        Hist_Basics histogram = new Hist_Basics();
        Random_NormalDist normalDist = new Random_NormalDist();
        public CS_Random_CheckNormalDistSmoothed(VBtask task) : base(task)
        {
            histogram.plot.minRange = 1;
            desc = "Display the histogram for a Normal distribution.";
        }
        public void RunCS(Mat src)
        {
            normalDist.Run(src);
            dst3 = normalDist.dst2;
            histogram.Run(dst3);
            dst2 = histogram.dst2;
        }
    }
    public class CS_Random_PatternGenerator_CPP : CS_Parent
    {
        public CS_Random_PatternGenerator_CPP(VBtask task) : base(task)
        {
            cPtr = Random_PatternGenerator_Open();
            desc = "Generate random patterns for use with 'Random Pattern Calibration'";
        }
        public void RunCS(Mat src)
        {
            byte[] dataSrc = new byte[src.Total() * src.ElemSize()];
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
            IntPtr imagePtr = Random_PatternGenerator_Run(cPtr, src.Rows, src.Cols);
            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr).Clone();
        }
        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = Random_PatternGenerator_Close(cPtr);
        }
    }
    public class CS_Random_CustomDistribution : CS_Parent
    {
        public Mat inputCDF; // place a cumulative distribution function here (or just put the histogram that reflects the desired random number distribution)
        public Mat outputRandom = new Mat(10000, 1, MatType.CV_32S, 0); // allocate the desired number of random numbers - size can be just one to get the next random value
        public Mat outputHistogram;
        public Plot_Histogram plot = new Plot_Histogram();
        public CS_Random_CustomDistribution(VBtask task) : base(task)
        {
            float[] loadedDice = { 1, 3, 0.5f, 0.5f, 0.75f, 0.25f };
            inputCDF = new Mat(loadedDice.Length, 1, MatType.CV_32F, loadedDice);
            desc = "Create a custom random number distribution from any histogram";
        }
        public void RunCS(Mat src)
        {
            float lastValue = inputCDF.At<float>(inputCDF.Rows - 1, 0);
            if (!(lastValue > 0.99 && lastValue <= 1.0)) // convert the input histogram to a cdf.
            {
                inputCDF *= 1 / (inputCDF.Sum()[0]);
                for (int i = 1; i < inputCDF.Rows; i++)
                {
                    inputCDF.Set<float>(i, 0, inputCDF.At<float>(i - 1, 0) + inputCDF.At<float>(i, 0));
                }
            }
            outputHistogram = new Mat(inputCDF.Size(), MatType.CV_32F, 0);
            int size = outputHistogram.Rows;
            for (int i = 0; i < outputRandom.Rows; i++)
            {
                double uniformR1 = msRNG.NextDouble();
                for (int j = 0; j < size; j++)
                {
                    if (uniformR1 < inputCDF.At<float>(j, 0))
                    {
                        outputHistogram.Set<float>(j, 0, outputHistogram.At<float>(j, 0) + 1);
                        outputRandom.Set<int>(i, 0, j); // the output is an integer reflecting a bin in the histogram.
                        break;
                    }
                }
            }
            plot.Run(outputHistogram);
            dst2 = plot.dst2;
        }
    }
    public class CS_Random_MonteCarlo : CS_Parent
    {
        public Plot_Histogram plot = new Plot_Histogram();
        Options_MonteCarlo options = new Options_MonteCarlo();
        public Mat outputRandom = new Mat(new cv.Size(1, 4000), MatType.CV_32S, 0); // allocate the desired number of random numbers - size can be just one to get the next random value
        public CS_Random_MonteCarlo(VBtask task) : base(task)
        {
            plot.maxValue = 100;
            desc = "Generate random numbers but prefer higher values - a linearly increasing random distribution";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat histogram = new Mat(options.dimension, 1, MatType.CV_32F, 0);
            for (int i = 0; i < outputRandom.Rows; i++)
            {
                while (true)
                {
                    double r1 = msRNG.NextDouble();
                    double r2 = msRNG.NextDouble();
                    if (r2 < r1)
                    {
                        int index = (int)(options.dimension * r1);
                        histogram.Set<float>(index, 0, histogram.At<float>(index, 0) + 1);
                        outputRandom.Set<int>(i, 0, index);
                        break;
                    }
                }
            }
            if (standaloneTest())
            {
                plot.Run(histogram);
                dst2 = plot.dst2;
            }
        }
    }
    public class CS_Random_CustomHistogram : CS_Parent
    {
        public Random_CustomDistribution random = new Random_CustomDistribution();
        public Hist_Simple hist = new Hist_Simple();
        public Mat saveHist;
        public CS_Random_CustomHistogram(VBtask task) : base(task)
        {
            random.outputRandom = new Mat(1000, 1, MatType.CV_32S, 0);
            labels[2] = "Histogram of the grayscale image";
            labels[3] = "Custom random distribution that reflects dst2 image";
            desc = "Create a random number distribution that reflects histogram of a grayscale image";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            hist.plot.maxValue = 0; // we are sharing the plot with the code below...
            hist.Run(src);
            dst2 = hist.dst2.Clone();
            saveHist = hist.plot.histogram.Clone();
            random.inputCDF = saveHist; // it will convert the histogram into a cdf where the last value must be near one.
            random.Run(src);
            if (standaloneTest())
            {
                hist.plot.maxValue = 100;
                hist.plot.Run(random.outputHistogram);
                dst3 = hist.plot.dst2;
            }
        }
    }
    public class CS_Random_StaticTV : CS_Parent
    {
        Options_StaticTV options = new Options_StaticTV();
        public CS_Random_StaticTV(VBtask task) : base(task)
        {
            task.drawRect = new cv.Rect(10, 10, 50, 50);
            labels[2] = "Draw anywhere to select a test region";
            labels[3] = "Resized selection rectangle in dst2";
            desc = "Imitate an old TV appearance using randomness.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            dst3 = dst2[task.drawRect];
            for (int y = 0; y < dst3.Height; y++)
            {
                for (int x = 0; x < dst3.Width; x++)
                {
                    if (255 * msRNG.NextDouble() <= options.thresh)
                    {
                        byte v = dst3.At<byte>(y, x);
                        dst3.Set<byte>(y, x, (byte)(msRNG.NextDouble() * 2 == 0 ? Math.Min(v + 
                             (options.val + 1) * msRNG.NextDouble(), 255) : Math.Max(v - (options.val + 1) * msRNG.NextDouble(), 0)));
                    }
                }
            }
        }
    }
    public class CS_Random_StaticTVFaster : CS_Parent
    {
        Random_UniformDist random = new Random_UniformDist();
        Mat_4to1 mats = new Mat_4to1();
        Random_StaticTV options = new Random_StaticTV();
        TrackBar valSlider;
        TrackBar percentSlider;
        public CS_Random_StaticTVFaster(VBtask task) : base(task)
        {
            valSlider = FindSlider("Range of noise to apply (from 0 to this value)");
            percentSlider = FindSlider("Percentage of pixels to include noise");
            labels[3] = "Changed pixels, add/sub mask, plusMask, minusMask";
            desc = "A faster way to apply noise to imitate an old TV appearance using randomness and thresholding.";
        }
        public void RunCS(Mat src)
        {
            dst2 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            random.Run(src);
            mats.mat[0] = random.dst2.Threshold(255 - percentSlider.Value * 255 / 100, 255, ThresholdTypes.Binary);
            Mat nochangeMask = random.dst2.Threshold(255 - percentSlider.Value * 255 / 100, 255, ThresholdTypes.BinaryInv);
            Mat valMat = new Mat(dst2.Size(), MatType.CV_8U, 0);
            Cv2.Randu(valMat, 0, valSlider.Value);
            valMat.SetTo(0, nochangeMask);
            random.Run(src);
            Mat plusMask = random.dst2.Threshold(128, 255, ThresholdTypes.Binary);
            Mat minusMask = random.dst2.Threshold(128, 255, ThresholdTypes.BinaryInv);
            mats.mat[2] = plusMask;
            mats.mat[3] = minusMask;
            mats.mat[1] = (plusMask + minusMask).ToMat().SetTo(0, nochangeMask);
            Cv2.Add(dst2, valMat, dst2, plusMask);
            Cv2.Subtract(dst2, valMat, dst2, minusMask);
            mats.Run(empty);
            dst3 = mats.dst2;
        }
    }
    public class CS_Random_StaticTVFastSimple : CS_Parent
    {
        Random_UniformDist random = new Random_UniformDist();
        Random_StaticTV options = new Random_StaticTV();
        TrackBar valSlider;
        TrackBar percentSlider;
        public CS_Random_StaticTVFastSimple(VBtask task) : base(task)
        {
            valSlider = FindSlider("Range of noise to apply (from 0 to this value)");
            percentSlider = FindSlider("Percentage of pixels to include noise");
            desc = "Remove diagnostics from the faster algorithm to simplify code.";
        }
        public void RunCS(Mat src)
        {
            dst2 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            random.Run(src);
            var nochangeMask = random.dst2.Threshold(255 - percentSlider.Value * 255 / 100, 255, ThresholdTypes.BinaryInv);
            dst3 = new Mat(dst2.Size(), MatType.CV_8U);
            Cv2.Randu(dst3, 0, valSlider.Value);
            dst3.SetTo(0, nochangeMask);
            var tmp = new Mat(dst2.Size(), MatType.CV_8U);
            Cv2.Randu(tmp, 0, 255);
            var plusMask = tmp.Threshold(128, 255, ThresholdTypes.Binary);
            var minusMask = tmp.Threshold(128, 255, ThresholdTypes.BinaryInv);
            Cv2.Add(dst2, dst3, dst2, plusMask);
            Cv2.Subtract(dst2, dst3, dst2, minusMask);
            labels[3] = "Mat of random values < " + valSlider.Value.ToString();
        }
    }
    public class CS_Random_KalmanPoints : CS_Parent
    {
        Random_Basics random = new Random_Basics();
        Kalman_Basics kalman = new Kalman_Basics();
        List<cv.Point2f> targetSet = new List<cv.Point2f>();
        List<cv.Point2f> currSet = new List<cv.Point2f>();
        bool refreshPoints = true;
        public CS_Random_KalmanPoints(VBtask task) : base(task)
        {
            var offset = dst2.Width / 5;
            random.range = new cv.Rect(offset, offset, Math.Abs(dst2.Width - offset * 2), Math.Abs(dst2.Height - offset * 2));
            FindSlider("Random Pixel Count").Value = 10;
            desc = "Smoothly transition a random point from location to location.";
        }
        public void RunCS(Mat src)
        {
            if (refreshPoints)
            {
                random.Run(empty);
                targetSet = new List<cv.Point2f>(random.PointList);
                currSet = new List<cv.Point2f>(random.PointList); // just to get the updated size
                refreshPoints = false;
                if (targetSet.Count() * 2 != kalman.kInput.Length)
                    Array.Resize(ref kalman.kInput, targetSet.Count() * 2);
            }
            for (int i = 0; i < targetSet.Count(); i++)
            {
                var pt = targetSet[i];
                kalman.kInput[i * 2] = pt.X;
                kalman.kInput[i * 2 + 1] = pt.Y;
            }
            kalman.Run(src);
            for (int i = 0; i < kalman.kOutput.Count(); i += 2)
            {
                currSet[i / 2] = new cv.Point(kalman.kOutput[i], kalman.kOutput[i + 1]);
            }
            dst2.SetTo(0);
            for (int i = 0; i < currSet.Count(); i++)
            {
                DrawCircle(dst2, currSet[i], task.DotSize + 2, Scalar.Yellow);
                DrawCircle(dst2, targetSet[i], task.DotSize + 2, Scalar.Red);
            }
            bool noChanges = true;
            for (int i = 0; i < currSet.Count(); i++)
            {
                var pt = currSet[i];
                if (Math.Abs(targetSet[i].X - pt.X) > 1 && Math.Abs(targetSet[i].Y - pt.Y) > 1)
                    noChanges = false;
            }
            if (noChanges) refreshPoints = true;
        }
    }
    public class CS_Random_Clusters : CS_Parent
    {
        public List<List<int>> clusterLabels = new List<List<int>>();
        public List<List<cv.Point2f>> clusters = new List<List<cv.Point2f>>();
        Options_Clusters options = new Options_Clusters();
        public CS_Random_Clusters(VBtask task) : base(task)
        {
            task.scalarColors[0] = Scalar.Yellow;
            task.scalarColors[1] = Scalar.Blue;
            task.scalarColors[2] = Scalar.Red;
            labels = new string[] { "", "", "Colorized sets", "" };
            desc = "Use OpenCV's randN API to create a cluster around a random mean with a requested stdev";
        }
        public void RunCS(Mat src)
        {
            if (!task.heartBeat) return;
            options.RunVB();
            var ptMat = new Mat(1, 1, MatType.CV_32FC2);
            dst2.SetTo(0);
            clusters.Clear();
            clusterLabels.Clear();
            for (int i = 0; i < options.numClusters; i++)
            {
                var mean = new Scalar(msRNG.Next(dst2.Width / 8, dst2.Width * 7 / 8), msRNG.Next(dst2.Height / 8, dst2.Height * 7 / 8), 0);
                var cList = new List<cv.Point2f>();
                var labelList = new List<int>();
                for (int j = 0; j < options.numPoints; j++)
                {
                    Cv2.Randn(ptMat, mean, Scalar.All(options.stdev));
                    var pt = ptMat.Get<cv.Point2f>(0, 0);
                    if (pt.X < 0) pt.X = 0;
                    if (pt.X >= dst2.Width) pt.X = dst2.Width - 1;
                    if (pt.Y < 0) pt.Y = 0;
                    if (pt.Y >= dst2.Height) pt.Y = dst2.Height - 1;
                    DrawCircle(dst2, pt, task.DotSize, task.scalarColors[i % 256]);
                    cList.Add(pt);
                    labelList.Add(i);
                }
                clusterLabels.Add(labelList);
                clusters.Add(cList);
            }
        }
    }
    public class CS_Rectangle_Basics : CS_Parent
    {
        public List<cv.Rect> rectangles = new List<cv.Rect>();
        public List<RotatedRect> rotatedRectangles = new List<RotatedRect>();
        public Options_Draw options = new Options_Draw();
        public CS_Rectangle_Basics(VBtask task) : base(task)
        {
            desc = "Draw the requested number of rectangles.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.heartBeat)
            {
                dst2.SetTo(Scalar.Black);
                rectangles.Clear();
                rotatedRectangles.Clear();
                for (int i = 0; i < options.drawCount; i++)
                {
                    var nPoint = new Point2f(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height));
                    var width = msRNG.Next(0, src.Cols - (int)nPoint.X - 1);
                    var height = msRNG.Next(0, src.Rows - (int)nPoint.Y - 1);
                    var eSize = new Size2f((float)msRNG.Next(0, src.Cols - (int)nPoint.X - 1), (float)msRNG.Next(0, src.Rows - (int)nPoint.Y - 1));
                    var angle = 180.0f * (float)(msRNG.Next(0, 1000) / 1000.0);
                    var nextColor = new Scalar(task.vecColors[i][0], task.vecColors[i][1], task.vecColors[i][2]);
                    var rr = new RotatedRect(nPoint, eSize, angle);
                    var r = new cv.Rect((int)nPoint.X, (int)nPoint.Y, width, height);
                    if (options.drawRotated)
                    {
                        DrawRotatedRect(rr, dst2, nextColor);
                    }
                    else
                    {
                        Cv2.Rectangle(dst2, r, nextColor, options.drawFilled);
                    }
                    rotatedRectangles.Add(rr);
                    rectangles.Add(r);
                }
            }
        }
    }
    public class CS_Rectangle_Rotated : CS_Parent
    {
        public Rectangle_Basics rectangle = new Rectangle_Basics();
        public CS_Rectangle_Rotated(VBtask task) : base(task)
        {
            FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)").Checked = true;
            desc = "Draw the requested number of rectangles.";
        }
        public void RunCS(Mat src)
        {
            rectangle.Run(src);
            dst2 = rectangle.dst2;
        }
    }
    public class CS_Rectangle_Overlap : CS_Parent
    {
        public cv.Rect rect1;
        public cv.Rect rect2;
        public cv.Rect enclosingRect;
        Rectangle_Basics draw = new Rectangle_Basics();
        public CS_Rectangle_Overlap(VBtask task) : base(task)
        {
            FindSlider("DrawCount").Value = 2;
            desc = "Test if 2 rectangles overlap";
        }
        public void RunCS(Mat src)
        {
            if (!task.heartBeat) return;
            if (standaloneTest())
            {
                draw.Run(src);
                dst2 = draw.dst2;
            }
            dst3.SetTo(0);
            if (draw.options.drawRotated)
            {
                var r1 = draw.rotatedRectangles[0];
                var r2 = draw.rotatedRectangles[1];
                rect1 = r1.BoundingRect();
                rect2 = r2.BoundingRect();
                DrawRotatedOutline(r1, dst3, Scalar.Yellow);
                DrawRotatedOutline(r2, dst3, Scalar.Yellow);
            }
            else
            {
                rect1 = draw.rectangles[0];
                rect2 = draw.rectangles[1];
            }
            enclosingRect = new cv.Rect();
            if (rect1.IntersectsWith(rect2))
            {
                enclosingRect = rect1.Union(rect2);
                dst3.Rectangle(enclosingRect, Scalar.White, 4);
                labels[3] = "Rectangles intersect - red marks overlapping rectangle";
                dst3.Rectangle(rect1.Intersect(rect2), Scalar.Red, -1);
            }
            else
            {
                labels[3] = "Rectangles don't intersect";
            }
            dst3.Rectangle(rect1, Scalar.Yellow, 2);
            dst3.Rectangle(rect2, Scalar.Yellow, 2);
        }
    }

    public class CS_Rectangle_Union : CS_Parent
    {
        Rectangle_Basics draw = new Rectangle_Basics();
        public List<cv.Rect> inputRects = new List<cv.Rect>();
        public cv.Rect allRect; // a rectangle covering all the input
        public CS_Rectangle_Union(VBtask task) : base(task)
        {
            desc = "Create a rectangle that contains all the input rectangles";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                var countSlider = FindSlider("DrawCount");
                var rotatedCheck = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)");
                rotatedCheck.Enabled = false;
                countSlider.Value = msRNG.Next(2, 10);
                labels[2] = "Input rectangles = " + draw.rectangles.Count();
                draw.Run(src);
                dst2 = draw.dst2;
                inputRects = new List<cv.Rect>(draw.rectangles);
            }
            else
            {
                dst2.SetTo(0);
                foreach (var r in inputRects)
                {
                    dst2.Rectangle(r, Scalar.Yellow, 1);
                }
                labels[2] = "Input rectangles = " + inputRects.Count();
            }
            if (inputRects.Count() == 0) return;
            allRect = inputRects[0];
            for (int i = 1; i < inputRects.Count(); i++)
            {
                var r = inputRects[i];
                if (r.X < 0) r.X = 0;
                if (r.Y < 0) r.Y = 0;
                if (allRect.Width > 0 && allRect.Height > 0)
                {
                    allRect = r.Union(allRect);
                    if (allRect.X + allRect.Width >= dst2.Width) allRect.Width = dst2.Width - allRect.X;
                    if (allRect.Height >= dst2.Height) allRect.Height = dst2.Height - allRect.Y;
                }
            }
            if (allRect.X + allRect.Width >= dst2.Width) allRect.Width = dst2.Width - allRect.X;
            if (allRect.Y + allRect.Height >= dst2.Height) allRect.Height = dst2.Height - allRect.Y;
            dst2.Rectangle(allRect, Scalar.Red, 2);
        }
    }
    public class CS_Rectangle_MultiOverlap : CS_Parent
    {
        public List<cv.Rect> inputRects = new List<cv.Rect>();
        public List<cv.Rect> outputRects = new List<cv.Rect>();
        Rectangle_Basics draw = new Rectangle_Basics();
        System.Windows.Forms.CheckBox rotatedCheck;
        TrackBar countSlider;
        public CS_Rectangle_MultiOverlap(VBtask task) : base(task)
        {
            rotatedCheck = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)");
            countSlider = FindSlider("DrawCount");
            desc = "Given a group of rectangles, merge all the rectangles that overlap";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                rotatedCheck.Enabled = task.toggleOnOff;
                countSlider.Value = msRNG.Next(2, 10);
                labels[2] = "Input rectangles = " + countSlider.Value.ToString();
                draw.Run(src);
                dst2 = draw.dst2;
                inputRects = draw.rectangles;
            }
            bool unionAdded;
            do
            {
                unionAdded = false;
                for (int i = 0; i < inputRects.Count(); i++)
                {
                    var r1 = inputRects[i];
                    int rectCount = inputRects.Count();
                    for (int j = i + 1; j < inputRects.Count(); j++)
                    {
                        var r2 = inputRects[j];
                        if (r1.IntersectsWith(r2))
                        {
                            inputRects.RemoveAt(j);
                            inputRects.RemoveAt(i);
                            inputRects.Add(r1.Union(r2));
                            unionAdded = true;
                            break;
                        }
                    }
                    if (rectCount != inputRects.Count()) break;
                }
            } while (unionAdded);
            outputRects = inputRects;
            if (standaloneTest())
            {
                dst3.SetTo(0);
                foreach (var r in outputRects)
                {
                    dst3.Rectangle(r, Scalar.Yellow, 2);
                }
                dst3 = dst2 * 0.5 + dst3;
                labels[3] = outputRects.Count().ToString() + " output rectangles";
            }
        }
    }
    public class CS_Rectangle_EnclosingPoints : CS_Parent
    {
        public List<cv.Point2f> pointList = new List<cv.Point2f>();
        public CS_Rectangle_EnclosingPoints(VBtask task) : base(task)
        {
            desc = "Build an enclosing rectangle for the supplied pointlist";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                pointList = QuickRandomPoints(20);
                dst2.SetTo(0);
                foreach (var pt in pointList)
                {
                    DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
                }
            }
            var minRect = Cv2.MinAreaRect(pointList.ToArray());
            DrawRotatedOutline(minRect, dst2, Scalar.Yellow);
        }
    }
    public class CS_Rectangle_Intersection : CS_Parent
    {
        public List<cv.Rect> inputRects = new List<cv.Rect>();
        Rectangle_Basics draw = new Rectangle_Basics();
        public List<cv.Rect> enclosingRects = new List<cv.Rect>();
        List<cv.Rect> otherRects = new List<cv.Rect>();
        System.Windows.Forms.CheckBox rotatedCheck;
        TrackBar countSlider;
        public CS_Rectangle_Intersection(VBtask task) : base(task)
        {
            rotatedCheck = FindCheckBox("Draw Rotated Rectangles - unchecked will draw ordinary rectangles (unrotated)");
            countSlider = FindSlider("DrawCount");
            desc = "Test if any number of rectangles intersect.";
        }
        cv.Rect findEnclosingRect(List<cv.Rect> rects, int proximity)
        {
            cv.Rect enclosing = rects[0];
            List<cv.Rect> newOther = new List<cv.Rect>();
            for (int i = 1; i < rects.Count(); i++)
            {
                cv.Rect r1 = rects[i];
                if (enclosing.IntersectsWith(r1) || Math.Abs(r1.X - enclosing.X) < proximity)
                {
                    enclosing = enclosing.Union(r1);
                }
                else
                {
                    newOther.Add(r1);
                }
            }
            otherRects = new List<cv.Rect>(newOther);
            return enclosing;
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                if (task.heartBeat)
                {
                    rotatedCheck.Checked = task.toggleOnOff;
                    countSlider.Value = msRNG.Next(2, 10);
                    labels[2] = "Input rectangles = " + countSlider.Value.ToString();
                    draw.Run(src);
                    dst2 = draw.dst2;
                    inputRects = new List<cv.Rect>(draw.rectangles);
                }
            }
            else
            {
                dst2.SetTo(0);
                foreach (Rect r in inputRects)
                {
                    dst2.Rectangle(r, Scalar.Yellow, 1);
                }
            }
            SortedList<float, cv.Rect> sortedRect = new SortedList<float, cv.Rect>(new compareAllowIdenticalSingleInverted());
            foreach (Rect r in inputRects)
            {
                sortedRect.Add(r.Width * r.Height, r);
            }
            otherRects = new List<cv.Rect>(sortedRect.Values);
            enclosingRects.Clear();
            while (otherRects.Count() > 0)
            {
                cv.Rect enclosing = findEnclosingRect(otherRects, draw.options.proximity);
                enclosingRects.Add(enclosing);
            }
            labels[3] = enclosingRects.Count().ToString() + " enclosing rectangles were found";
            dst3.SetTo(0);
            foreach (Rect r in enclosingRects)
            {
                dst3.Rectangle(r, Scalar.Yellow, 2);
            }
            dst3 = dst2 * 0.5 + dst3;
        }
    }
    public class CS_RecursiveBilateralFilter_CPP : CS_Parent
    {
        byte[] dataSrc = new byte[1];
        Options_RBF options = new Options_RBF();
        public CS_RecursiveBilateralFilter_CPP(VBtask task) : base(task)
        {
            cPtr = RecursiveBilateralFilter_Open();
            desc = "Apply the recursive bilateral filter - edge-preserving but faster.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (dataSrc.Length != src.Total() * src.ElemSize())
                Array.Resize(ref dataSrc, (int)(src.Total() * src.ElemSize()));
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
            GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            IntPtr imagePtr = RecursiveBilateralFilter_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                                            options.RBFCount);
            handleSrc.Free();
            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, imagePtr).Clone();
        }
        public void Close()
        {
            if (cPtr != IntPtr.Zero)
                cPtr = RecursiveBilateralFilter_Close(cPtr);
        }
    }
    public class CS_RedCloud_Basics : CS_Parent
    {
        public Cell_Generate genCells = new Cell_Generate();
        RedCloud_CPP redCPP = new RedCloud_CPP();
        public Mat inputMask = new Mat();
        Color8U_Basics color;
        public CS_RedCloud_Basics(VBtask task) : base(task)
        {
            task.redOptions.setIdentifyCells(true);
            inputMask = new Mat(dst2.Size(), MatType.CV_8U, 0);
            UpdateAdvice(traceName + ": there is dedicated panel for RedCloud algorithms." + "\n" +
                         "It is behind the global options (which affect most algorithms.)");
            desc = "Find cells and then match them to the previous generation with minimum boundary";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
            {
                if (color == null) color = new Color8U_Basics();
                color.Run(src);
                src = color.dst2;
            }
            redCPP.inputMask = inputMask;
            redCPP.Run(src);
            if (redCPP.classCount == 0) return; // no data to process.
            genCells.classCount = redCPP.classCount;
            genCells.rectList = redCPP.rectList;
            genCells.floodPoints = redCPP.floodPoints;
            genCells.Run(redCPP.dst2);
            dst2 = genCells.dst2;
            labels[2] = genCells.labels[2];
            dst3.SetTo(0);
            var smallCellThreshold = src.Total() / 1000;
            int cellCount = 0;
            foreach (var rc in task.redCells)
            {
                if (rc.pixels > smallCellThreshold)
                {
                    DrawCircle(dst3, rc.maxDist, task.DotSize, task.HighlightColor);
                    cellCount++;
                }
            }
            labels[3] = cellCount.ToString() + " RedCloud cells with more than " + smallCellThreshold + " pixels.  " + task.redCells.Count() + " cells present.";
        }
    }
    public class CS_RedCloud_Reduction : CS_Parent
    {
        public RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_Reduction(VBtask task) : base(task)
        {
            task.redOptions.setUseColorOnly(true);
            task.redOptions.setColorSource("Reduction_Basics");
            task.gOptions.setHistogramBins(20);
            desc = "Segment the image based on both the reduced color";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst3 = task.cellMap;
            dst2 = redC.dst2;
            labels = redC.labels;
        }
    }
    public class CS_RedCloud_Hulls : CS_Parent
    {
        Convex_RedCloudDefects convex = new Convex_RedCloudDefects();
        public RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_Hulls(VBtask task) : base(task)
        {
            labels = new string[] { "", "Cells where convexity defects failed", "", "Improved contour results using OpenCV's ConvexityDefects" };
            desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            dst3.SetTo(0);
            int defectCount = 0;
            task.cellMap.SetTo(0);
            var redCells = new List<rcData>();
            foreach (var rc in task.redCells)
            {
                if (rc.contour.Count() >= 5)
                {
                    rc.hull = Cv2.ConvexHull(rc.contour.ToArray(), true).ToList();
                    var hullIndices = Cv2.ConvexHullIndices(rc.hull.ToArray(), false);
                    try
                    {
                        var defects = Cv2.ConvexityDefects(rc.contour, hullIndices);
                        rc.contour = convex.betterContour(rc.contour, defects);
                    }
                    catch (Exception)
                    {
                        defectCount++;
                    }
                    DrawContour(dst3[rc.rect], rc.hull, vecToScalar(rc.color), -1);
                    DrawContour(task.cellMap[rc.rect], rc.hull, rc.index, -1);
                }
                redCells.Add(rc);
            }
            task.redCells = new List<rcData>(redCells);
            labels[2] = task.redCells.Count() + " hulls identified below.  " + defectCount + " hulls failed to build the defect list.";
        }
    }
    public class CS_RedCloud_FindCells : CS_Parent
    {
        public List<int> cellList = new List<int>();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_FindCells(VBtask task) : base(task)
        {
            task.redOptions.setIdentifyCells(true);
            task.gOptions.pixelDiffThreshold = 25;
            cPtr = RedCloud_FindCells_Open();
            desc = "Find all the RedCloud cells touched by the mask created by the Motion_History rectangle";
        }
        public void RunCS(Mat src)
        {
            cellList = new List<int>();
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            int count = 0;
            dst3.SetTo(0);
            if (task.motionDetected)
            {
                dst1 = task.cellMap[task.motionRect].Clone();
                var cppData = new byte[dst1.Total() - 1];
                Marshal.Copy(dst1.Data, cppData, 0, cppData.Length);
                var handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned);
                var imagePtr = RedCloud_FindCells_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols);
                handleSrc.Free();
                count = RedCloud_FindCells_TotalCount(cPtr);
                if (count == 0) return;
                var cellsFound = new int[count];
                Marshal.Copy(imagePtr, cellsFound, 0, cellsFound.Length);
                cellList = new List<int>(cellsFound);
                dst0 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
                dst0 = dst0.Threshold(0, 255, ThresholdTypes.BinaryInv);
                foreach (var index in cellList)
                {
                    if (task.redCells.Count() <= index) continue;
                    var rc = task.redCells[index];
                    DrawContour(dst3[rc.rect], rc.contour, vecToScalar(rc.color), -1);
                    if (task.redOptions.getNaturalColor())
                        dst3[rc.rect].SetTo(rc.naturalColor, rc.mask);
                    else
                        dst3[rc.rect].SetTo(Scalar.White, rc.mask);
                }
                dst2.Rectangle(task.motionRect, Scalar.White, task.lineWidth);
            }
            labels[3] = count + " cells were found using the motion mask";
        }
        public void Close()
        {
            RedCloud_FindCells_Close(cPtr);
        }
    }
    public class CS_RedCloud_Planes : CS_Parent
    {
        public RedCloud_PlaneColor planes = new RedCloud_PlaneColor();
        public CS_RedCloud_Planes(VBtask task) : base(task)
        {
            desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal";
        }
        public void RunCS(Mat src)
        {
            planes.Run(src);
            dst2 = planes.dst2;
            dst3 = planes.dst3;
            labels = planes.labels;
        }
    }
    public class CS_RedCloud_Equations : CS_Parent
    {
        Plane_Equation eq = new Plane_Equation();
        public List<rcData> redCells = new List<rcData>();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_Equations(VBtask task) : base(task)
        {
            labels[3] = "The estimated plane equations for the largest 20 RedCloud cells.";
            desc = "Show the estimated plane equations for all the cells.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                redC.Run(src);
                dst2 = redC.dst2;
                redCells = new List<rcData>(task.redCells);
            }
            var newCells = new List<rcData>();
            foreach (var rc in redCells)
            {
                if (rc.contour.Count() > 4)
                {
                    eq.rc = rc;
                    eq.Run(empty);
                    newCells.Add(eq.rc);
                }
            }
            redCells = new List<rcData>(newCells);
            if (task.heartBeat)
            {
                int index = 0;
                strOut = "";
                foreach (var rc in redCells)
                {
                    if (rc.contour.Count() > 4)
                    {
                        var justEquation = $"{rc.eq[0]:fmt3}*X + {rc.eq[1]:fmt3}*Y + " +
                                           $"{rc.eq[2]:fmt3}*Z + {rc.eq[3]:fmt3}" + "\n";
                        strOut += justEquation;
                        index++;
                        if (index >= 20) break;
                    }
                }
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_RedCloud_CellsAtDepth : CS_Parent
    {
        Plot_Histogram plot = new Plot_Histogram();
        Kalman_Basics kalman = new Kalman_Basics();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_CellsAtDepth(VBtask task) : base(task)
        {
            plot.removeZeroEntry = false;
            labels[3] = "Histogram of depth weighted by the size of the cell.";
            desc = "Create a histogram of depth using RedCloud cells";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            int histBins = task.histogramBins;
            List<int>[] slotList = new List<int>[histBins];
            for (int i = 0; i < slotList.Length; i++)
            {
                slotList[i] = new List<int>();
            }
            float[] hist = new float[histBins];
            foreach (var rc in task.redCells)
            {
                int slot;
                if (rc.depthMean[2] > task.MaxZmeters) rc.depthMean[2] = task.MaxZmeters;
                slot = (int)((rc.depthMean[2] / task.MaxZmeters) * histBins);
                if (slot >= hist.Length) slot = hist.Length - 1;
                slotList[slot].Add(rc.index);
                hist[slot] += rc.pixels;
            }
            kalman.kInput = hist;
            kalman.Run(src);
            Mat histMat = new Mat(histBins, 1, MatType.CV_32F, kalman.kOutput);
            plot.Run(histMat);
            dst3 = plot.dst2;
            float barWidth = dst3.Width / histBins;
            int histIndex = (int)Math.Floor(task.mouseMovePoint.X / barWidth);
            dst3.Rectangle(new cv.Rect((int)(histIndex * barWidth), 0, (int)barWidth, dst3.Height), Scalar.Yellow, task.lineWidth);
            for (int i = 0; i < slotList[histIndex].Count(); i++)
            {
                var rc = task.redCells[slotList[histIndex][i]];
                DrawContour(dst2[rc.rect], rc.contour, Scalar.Yellow);
                DrawContour(task.color[rc.rect], rc.contour, Scalar.Yellow);
            }
        }
    }
    public class CS_RedCloud_ShapeCorrelation : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_ShapeCorrelation(VBtask task) : base(task)
        {
            desc = "A shape correlation is between each x and y in list of contours points.  It allows classification based on angle and shape.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            var rc = task.rc;
            if (rc.contour.Count() > 0)
            {
                float shape = shapeCorrelation(rc.contour);
                strOut = "Contour correlation for selected cell contour X to Y = " + shape.ToString("F3") + "\n" + "\n" +
                         "Select different cells and notice the pattern for the correlation of the contour.X to contour.Y values:" + "\n" +
                         "(The contour correlation - contour.x to contour.y - Is computed above.)" + "\n" + "\n" +
                         "If shape leans left, correlation Is positive And proportional to the lean." + "\n" +
                         "If shape leans right, correlation Is negative And proportional to the lean. " + "\n" +
                         "If shape Is symmetric (i.e. rectangle Or circle), correlation Is near zero." + "\n" +
                         "(Remember that Y increases from the top of the image to the bottom.)";
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_RedCloud_FPS : CS_Parent
    {
        Grid_FPS fps = new Grid_FPS();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_FPS(VBtask task) : base(task)
        {
            task.gOptions.setDisplay1();
            task.gOptions.setDisplay1();
            desc = "Display RedCloud output at a fixed frame rate";
        }
        public void RunCS(Mat src)
        {
            fps.Run(empty);
            if (fps.heartBeat)
            {
                redC.Run(src);
                dst0 = task.color.Clone();
                dst1 = task.depthRGB.Clone();
                dst2 = redC.dst2.Clone();
            }
            labels[2] = redC.labels[2] + " " + fps.strOut;
        }
    }
    public class CS_RedCloud_PlaneColor : CS_Parent
    {
        public Options_Plane options = new Options_Plane();
        public RedCloud_Basics redC = new RedCloud_Basics();
        RedCloud_PlaneFromMask planeMask = new RedCloud_PlaneFromMask();
        RedCloud_PlaneFromContour planeContour = new RedCloud_PlaneFromContour();
        Plane_CellColor planeCells = new Plane_CellColor();
        public CS_RedCloud_PlaneColor(VBtask task) : base(task)
        {
            labels[3] = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis";
            desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal";
        }
        public void RunCS(Mat src)
        {
            if (!task.motionDetected) return;
            options.RunVB();
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            dst3.SetTo(0);
            List<cv.Point3f> fitPoints = new List<cv.Point3f>();
            foreach (var rc in task.redCells)
            {
                if (rc.eq == new Vec4f())
                {
                    rc.eq = new Vec4f();
                    if (options.useMaskPoints)
                    {
                        rc.eq = fitDepthPlane(planeCells.buildMaskPointEq(rc));
                    }
                    else if (options.useContourPoints)
                    {
                        rc.eq = fitDepthPlane(planeCells.buildContourPoints(rc));
                    }
                    else if (options.use3Points)
                    {
                        rc.eq = build3PointEquation(rc);
                    }
                }
                dst3[rc.rect].SetTo(new Scalar(Math.Abs(255 * rc.eq.Item0),
                                                Math.Abs(255 * rc.eq.Item1),
                                                Math.Abs(255 * rc.eq.Item2)), rc.mask);
            }
        }
    }
    public class CS_RedCloud_PlaneFromContour : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_PlaneFromContour(VBtask task) : base(task)
        {
            labels[3] = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis";
            desc = "Create a plane equation each cell's contour";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                redC.Run(src);
                dst2 = redC.dst2;
                labels[2] = redC.labels[2];
            }
            var rc = task.rc;
            List<cv.Point3f> fitPoints = new List<cv.Point3f>();
            foreach (var pt in rc.contour)
            {
                if (pt.X >= rc.rect.Width || pt.Y >= rc.rect.Height) continue;
                if (rc.mask.Get<byte>(pt.Y, pt.X) == 0) continue;
                fitPoints.Add(task.pointCloud[rc.rect].Get<cv.Point3f>(pt.Y, pt.X));
            }
            rc.eq = fitDepthPlane(fitPoints);
            if (standaloneTest())
            {
                dst3.SetTo(0);
                dst3[rc.rect].SetTo(new Scalar(Math.Abs(255 * rc.eq.Item0), Math.Abs(255 * rc.eq.Item1), Math.Abs(255 * rc.eq.Item2)), rc.mask);
            }
        }
    }
    public class CS_RedCloud_PlaneFromMask : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_PlaneFromMask(VBtask task) : base(task)
        {
            labels[3] = "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis";
            desc = "Create a plane equation from the pointcloud samples in a RedCloud cell";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                redC.Run(src);
                dst2 = redC.dst2;
                labels[2] = redC.labels[2];
            }
            var rc = task.rc;
            var fitPoints = new List<cv.Point3f>();
            for (int y = 0; y < rc.rect.Height; y++)
            {
                for (int x = 0; x < rc.rect.Width; x++)
                {
                    if (rc.mask.Get<byte>(y, x) != 0)
                    {
                        fitPoints.Add(task.pointCloud[rc.rect].Get<cv.Point3f>(y, x));
                    }
                }
            }
            rc.eq = fitDepthPlane(fitPoints);
            if (standaloneTest())
            {
                dst3.SetTo(0);
                dst3[rc.rect].SetTo(new Scalar(Math.Abs(255 * rc.eq[0]), Math.Abs(255 * rc.eq[1]), Math.Abs(255 * rc.eq[2])), rc.mask);
            }
        }
    }
    public class CS_RedCloud_BProject3D : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Hist3Dcloud_Basics hcloud = new Hist3Dcloud_Basics();
        public CS_RedCloud_BProject3D(VBtask task) : base(task)
        {
            desc = "Run RedCloud_Basics on the output of the RGB 3D backprojection";
        }
        public void RunCS(Mat src)
        {
            hcloud.Run(src);
            dst3 = hcloud.dst2;
            dst3.ConvertTo(dst0, MatType.CV_8U);
            redC.Run(dst0);
            dst2 = redC.dst2;
        }
    }
    public class CS_RedCloud_YZ : CS_Parent
    {
        Cell_Basics stats = new Cell_Basics();
        public CS_RedCloud_YZ(VBtask task) : base(task)
        {
            stats.runRedCloud = true;
            desc = "Build horizontal RedCloud cells";
        }
        public void RunCS(Mat src)
        {
            task.redOptions.setYZReduction(true);
            stats.Run(src);
            dst0 = stats.dst0;
            dst1 = stats.dst1;
            dst2 = stats.dst2;
            SetTrueText(stats.strOut, 3);
        }
    }
    public class CS_RedCloud_XZ : CS_Parent
    {
        Cell_Basics stats = new Cell_Basics();
        public CS_RedCloud_XZ(VBtask task) : base(task)
        {
            stats.runRedCloud = true;
            desc = "Build vertical RedCloud cells.";
        }
        public void RunCS(Mat src)
        {
            task.redOptions.setXZReduction(true);
            stats.Run(src);
            dst0 = stats.dst0;
            dst1 = stats.dst1;
            dst2 = stats.dst2;
            SetTrueText(stats.strOut, 3);
        }
    }
    public class CS_RedCloud_World : CS_Parent
    {
        RedCloud_Reduce redC = new RedCloud_Reduce();
        Depth_World world = new Depth_World();
        public CS_RedCloud_World(VBtask task) : base(task)
        {
            labels[3] = "Generated pointcloud";
            desc = "Display the output of a generated pointcloud as RedCloud cells";
        }
        public void RunCS(Mat src)
        {
            world.Run(src);
            task.pointCloud = world.dst2;
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            if (task.FirstPass) FindSlider("RedCloud_Reduce Reduction").Value = 1000;
        }
    }
    public class CS_RedCloud_KMeans : CS_Parent
    {
        KMeans_MultiChannel km = new KMeans_MultiChannel();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_KMeans(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "KMeans_MultiChannel output", "RedCloud_Basics output" };
            desc = "Use RedCloud to identify the regions created by kMeans";
        }
        public void RunCS(Mat src)
        {
            km.Run(src);
            dst3 = km.dst2;
            redC.Run(km.dst3);
            dst2 = redC.dst2;
        }
    }
    public class CS_RedCloud_Diff : CS_Parent
    {
        Diff_RGBAccum diff = new Diff_RGBAccum();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_Diff(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Diff output, RedCloud input", "RedCloud output" };
            desc = "Isolate blobs in the diff output with RedCloud";
        }
        public void RunCS(Mat src)
        {
            SetTrueText("Wave at the camera to see the segmentation of the motion.", 3);
            diff.Run(src);
            dst3 = diff.dst2;
            redC.Run(dst3);
            dst2.SetTo(0);
            redC.dst2.CopyTo(dst2, dst3);
            labels[3] = task.redCells.Count() + " objects identified in the diff output";
        }
    }
    public class CS_RedCloud_ProjectCell : CS_Parent
    {
        Hist_ShapeTop topView = new Hist_ShapeTop();
        Hist_ShapeSide sideView = new Hist_ShapeSide();
        Mat_4Click mats = new Mat_4Click();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_ProjectCell(VBtask task) : base(task)
        {
            task.gOptions.setDisplay1();
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            labels[3] = "Top: XZ values and mask, Bottom: ZY values and mask";
            desc = "Visualize the top and side projection of a RedCloud cell";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            // The commented code is omitted for clarity
        }
    }
    public class CS_RedCloud_LikelyFlatSurfaces : CS_Parent
    {
        Plane_Basics verts = new Plane_Basics();
        RedCloud_Basics redC = new RedCloud_Basics();
        public List<rcData> vCells = new List<rcData>();
        public List<rcData> hCells = new List<rcData>();
        public CS_RedCloud_LikelyFlatSurfaces(VBtask task) : base(task)
        {
            labels[1] = "RedCloud output";
            desc = "Use the mask for vertical surfaces to identify RedCloud cells that appear to be flat.";
        }
        public void RunCS(Mat src)
        {
            verts.Run(src);
            redC.Run(src);
            dst2.SetTo(0);
            dst3.SetTo(0);
            vCells.Clear();
            hCells.Clear();
            foreach (var rc in task.redCells)
            {
                if (rc.depthMean[2] >= task.MaxZmeters) continue;
                Mat tmp = verts.dst2[rc.rect] & rc.mask;
                if (rc.pixels == 0) continue;
                if (tmp.CountNonZero() / rc.pixels > 0.5)
                {
                    DrawContour(dst2[rc.rect], rc.contour, vecToScalar(rc.color), -1);
                    vCells.Add(rc);
                }
                tmp = verts.dst3[rc.rect] & rc.mask;
                int count = tmp.CountNonZero();
                if (count / rc.pixels > 0.5)
                {
                    DrawContour(dst3[rc.rect], rc.contour, vecToScalar(rc.color), -1);
                    hCells.Add(rc);
                }
            }
            var rcX = task.rc;
            SetTrueText("mean depth = " + rcX.depthMean[2].ToString("0.0"), 3);
            labels[2] = redC.labels[2];
        }
    }
    public class CS_RedCloud_PlaneEq3D : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Plane_Equation eq = new Plane_Equation();
        public CS_RedCloud_PlaneEq3D(VBtask task) : base(task)
        {
            desc = "If a RedColor cell contains depth then build a plane equation";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            var rc = task.rc;
            if (rc.maxVec.Z != 0)
            {
                eq.rc = rc;
                eq.Run(empty);
                rc = eq.rc;
            }
            dst3.SetTo(0);
            DrawContour(dst3[rc.rect], rc.contour, vecToScalar(rc.color), -1);
            SetTrueText(eq.strOut, 3);
        }
    }
    public class CS_RedCloud_DelaunayGuidedFeatures : CS_Parent
    {
        Feature_Delaunay features = new Feature_Delaunay();
        RedCloud_Basics redC = new RedCloud_Basics();
        List<List<cv.Point2f>> goodList = new List<List<cv.Point2f>>();
        public CS_RedCloud_DelaunayGuidedFeatures(VBtask task) : base(task)
        {
            labels = new[] { "", "Format CV_8U of Delaunay data", "RedCloud output", "RedCloud Output of GoodFeature points" };
            desc = "Track the GoodFeatures points using RedCloud.";
        }
        public void RunCS(Mat src)
        {
            features.Run(src);
            dst1 = features.dst3;
            redC.Run(dst1);
            dst2 = redC.dst2;
            if (task.heartBeat) goodList.Clear();
            var nextGood = new List<cv.Point2f>(task.features);
            goodList.Add(nextGood);
            if (goodList.Count() >= task.frameHistoryCount) goodList.RemoveAt(0);
            dst3.SetTo(0);
            foreach (var ptList in goodList)
            {
                foreach (var pt in ptList)
                {
                    var c = dst2.Get<Vec3b>((int)pt.Y, (int)pt.X);
                    DrawCircle(dst3, pt, task.DotSize, vecToScalar(c));
                }
            }
        }
    }
    public class CS_RedCloud_UnstableCells : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        List<cv.Point> prevList = new List<cv.Point>();
        public CS_RedCloud_UnstableCells(VBtask task) : base(task)
        {
            labels = new[] { "", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing" };
            desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            if (task.heartBeat || task.frameCount == 2)
            {
                dst1 = dst2.Clone();
                dst3.SetTo(0);
            }
            var currList = new List<cv.Point>();
            foreach (var rc in task.redCells)
            {
                if (!prevList.Contains(rc.maxDStable))
                {
                    DrawContour(dst1[rc.rect], rc.contour, Scalar.White, -1);
                    DrawContour(dst1[rc.rect], rc.contour, Scalar.Black);
                    DrawContour(dst3[rc.rect], rc.contour, Scalar.White, -1);
                }
                currList.Add(rc.maxDStable);
            }
            prevList = new List<cv.Point>(currList);
        }
    }
    public class CS_RedCloud_UnstableHulls : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        List<cv.Point> prevList = new List<cv.Point>();
        public CS_RedCloud_UnstableHulls(VBtask task) : base(task)
        {
            labels = new[] { "", "", "Current generation of cells", "Recently changed cells highlighted - indicated by rc.maxDStable changing" };
            desc = "Use maxDStable to identify unstable cells - cells which were NOT present in the previous generation.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            if (task.heartBeat || task.frameCount == 2)
            {
                dst1 = dst2.Clone();
                dst3.SetTo(0);
            }
            var currList = new List<cv.Point>();
            foreach (var rc in task.redCells)
            {
                rc.hull = Cv2.ConvexHull(rc.contour.ToArray(), true).ToList();
                if (!prevList.Contains(rc.maxDStable))
                {
                    DrawContour(dst1[rc.rect], rc.hull, Scalar.White, -1);
                    DrawContour(dst1[rc.rect], rc.hull, Scalar.Black);
                    DrawContour(dst3[rc.rect], rc.hull, Scalar.White, -1);
                }
                currList.Add(rc.maxDStable);
            }
            prevList = new List<cv.Point>(currList);
        }
    }
    public class CS_RedCloud_CellChanges : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Mat dst2Last;
        public CS_RedCloud_CellChanges(VBtask task) : base(task)
        {
            dst2Last = dst2.Clone();
            if (standaloneTest()) redC = new RedCloud_Basics();
            desc = "Count the cells that have changed in a RedCloud generation";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            dst3 = (dst2 - dst2Last).ToMat();
            int changedPixels = dst3.CvtColor(ColorConversionCodes.BGR2GRAY).CountNonZero();
            int changedCells = 0;
            foreach (var rc in task.redCells)
            {
                if (rc.indexLast == 0) changedCells++;
            }
            dst2Last = dst2.Clone();
            if (task.heartBeat)
            {
                labels[2] = "Changed cells = " + changedCells.ToString("000") + " cells or " + (changedCells / task.redCells.Count()).ToString("0%");
                labels[3] = "Changed pixel total = " + (changedPixels / 1000.0).ToString("0.0") + "k or " + (changedPixels / dst2.Total()).ToString("0%");
            }
        }
    }
    public class CS_RedCloud_FloodPoint : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Cell_Basics stats = new Cell_Basics();
        public CS_RedCloud_FloodPoint(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            desc = "Verify that floodpoints correctly determine if depth is present.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            dst1 = task.depthRGB;
            foreach (var rc in task.redCells)
            {
                DrawCircle(dst1, rc.floodPoint, task.DotSize, Scalar.White);
                DrawCircle(dst2, rc.floodPoint, task.DotSize, Scalar.Yellow);
            }
            stats.Run(src);
            SetTrueText(stats.strOut, 3);
        }
    }
    public class CS_RedCloud_CellStatsPlot : CS_Parent
    {
        Cell_BasicsPlot cells = new Cell_BasicsPlot();
        public CS_RedCloud_CellStatsPlot(VBtask task) : base(task)
        {
            task.redOptions.setIdentifyCells(true);
            if (standaloneTest()) task.gOptions.setDisplay1();
            cells.runRedCloud = true;
            desc = "Display the stats for the requested cell";
        }
        public void RunCS(Mat src)
        {
            cells.Run(src);
            dst1 = cells.dst1;
            dst2 = cells.dst2;
            labels[2] = cells.labels[2];
            SetTrueText(cells.strOut, 3);
        }
    }
    public class CS_RedCloud_MostlyColor : CS_Parent
    {
        public RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_MostlyColor(VBtask task) : base(task)
        {
            labels[3] = "Cells that have no depth data.";
            desc = "Identify cells that have no depth";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            dst3.SetTo(0);
            foreach (var rc in task.redCells)
            {
                if (rc.depthPixels > 0) dst3[rc.rect].SetTo(rc.color, rc.mask);
            }
        }
    }
    public class CS_RedCloud_OutlineColor : CS_Parent
    {
        Depth_Outline outline = new Depth_Outline();
        RedCloud_Basics redC = new RedCloud_Basics();
        Color8U_Basics colorClass = new Color8U_Basics();
        public CS_RedCloud_OutlineColor(VBtask task) : base(task)
        {
            labels[3] = "Color input to RedCloud_Basics with depth boundary blocking color connections.";
            desc = "Use the depth outline as input to RedCloud_Basics";
        }
        public void RunCS(Mat src)
        {
            outline.Run(task.depthMask);
            colorClass.Run(src);
            dst1 = colorClass.dst2 + 1;
            dst1.SetTo(0, outline.dst2);
            dst3 = ShowPalette(dst1 * 255 / colorClass.classCount);
            redC.Run(dst1);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
        }
    }
    public class CS_RedCloud_DepthOutline : CS_Parent
    {
        Depth_Outline outline = new Depth_Outline();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_DepthOutline(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            task.redOptions.setUseColorOnly(true);
            desc = "Use the Depth_Outline output over time to isolate high quality cells";
        }
        public void RunCS(Mat src)
        {
            outline.Run(task.depthMask);
            if (task.heartBeat) dst3.SetTo(0);
            dst3 = dst3 | outline.dst2;
            dst1.SetTo(0);
            src.CopyTo(dst1, ~dst3);
            redC.Run(dst1);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
        }
    }
    public class CS_RedCloud_MeterByMeter : CS_Parent
    {
        BackProject_MeterByMeter meter = new BackProject_MeterByMeter();
        public CS_RedCloud_MeterByMeter(VBtask task) : base(task)
        {
            desc = "Run RedCloud meter by meter";
        }
        public void RunCS(Mat src)
        {
            meter.Run(src);
            dst2 = meter.dst3;
            labels[2] = meter.labels[3];
            for (int i = 0; i <= task.MaxZmeters; i++)
            {
            }
        }
    }
    public class CS_RedCloud_FourColor : CS_Parent
    {
        Bin4Way_Regions binar4 = new Bin4Way_Regions();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_FourColor(VBtask task) : base(task)
        {
            task.redOptions.setIdentifyCells(true);
            task.redOptions.setUseColorOnly(true);
            labels[3] = "A 4-way split of the input grayscale image based on brightness";
            desc = "Use RedCloud on a 4-way split based on light to dark in the image.";
        }
        public void RunCS(Mat src)
        {
            binar4.Run(src);
            dst3 = ShowPalette(binar4.dst2 * 255 / 5);
            redC.Run(binar4.dst2);
            dst2 = redC.dst2;
            labels[2] = redC.labels[3];
        }
    }
    public class CS_RedCloud_CCompColor : CS_Parent
    {
        CComp_Both ccomp = new CComp_Both();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_CCompColor(VBtask task) : base(task)
        {
            task.redOptions.setUseColorOnly(true);
            desc = "Identify each Connected component as a RedCloud Cell.";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            ccomp.Run(src);
            dst3 = GetNormalize32f(ccomp.dst1);
            labels[3] = ccomp.labels[2];
            redC.Run(dst3);
            dst2 = redC.dst2;
            labels[2] = redC.labels[3];
        }
    }
    public class CS_RedCloud_Cells : CS_Parent
    {
        public RedCloud_Basics redC = new RedCloud_Basics();
        public Mat cellmap = new Mat();
        public List<rcData> redCells = new List<rcData>();
        public CS_RedCloud_Cells(VBtask task) : base(task)
        {
            task.redOptions.setUseColorOnly(true);
            desc = "Create RedCloud output using only color";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            cellmap = task.cellMap;
            redCells = task.redCells;
        }
    }
    public class CS_RedCloud_Flippers : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Mat lastMap;
        public CS_RedCloud_Flippers(VBtask task) : base(task)
        {
            lastMap = task.cellMap.Clone();
            task.redOptions.setIdentifyCells(true);
            task.redOptions.setUseColorOnly(true);
            labels[3] = "Highlighted below are the cells which flipped in color from the previous frame.";
            desc = "Identify the 4-way split cells that are flipping between brightness boundaries.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst3 = redC.dst3;
            labels[3] = redC.labels[2];
            dst2.SetTo(0);
            int unMatched = 0;
            int unMatchedPixels = 0;
            foreach (var cell in task.redCells)
            {
                var lastColor = lastMap.Get<Vec3b>(cell.maxDist.Y, cell.maxDist.X);
                if (lastColor != cell.color)
                {
                    dst2[cell.rect].SetTo(cell.color, cell.mask);
                    unMatched++;
                    unMatchedPixels += cell.pixels;
                }
            }
            lastMap = redC.dst3.Clone();
            if (task.heartBeat)
            {
                labels[3] = "Unmatched to previous frame: " + unMatched + " totaling " + unMatchedPixels + " pixels.";
            }
        }
    }
    public class CS_RedCloud_Overlaps : CS_Parent
    {
        public List<rcData> redCells = new List<rcData>();
        public Mat cellMap;
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_Overlaps(VBtask task) : base(task)
        {
            cellMap = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Remove the overlapping cells.  Keep the largest.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels = redC.labels;
            List<int> overlappingCells = new List<int>();
            cellMap.SetTo(0);
            redCells.Clear();
            foreach (var rc in task.redCells)
            {
                var valMap = cellMap.Get<byte>(rc.maxDist.Y, rc.maxDist.X);
                cellMap[rc.rect].SetTo(rc.index, rc.mask);
                redCells.Add(rc);
            }
            dst3.SetTo(0);
            for (int i = overlappingCells.Count() - 1; i >= 0; i--)
            {
                var rc = redCells[overlappingCells[i]];
                dst3[rc.rect].SetTo(rc.color, rc.mask);
                redCells.RemoveAt(overlappingCells[i]);
            }
            labels[3] = "Before removing overlapping cells: " + task.redCells.Count().ToString() + ". After: " + redCells.Count().ToString();
        }
    }
    public class CS_RedCloud_OnlyColorHist3D : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Hist3Dcolor_Basics hColor = new Hist3Dcolor_Basics();
        public CS_RedCloud_OnlyColorHist3D(VBtask task) : base(task)
        {
            desc = "Use the backprojection of the 3D RGB histogram as input to RedCloud_Basics.";
        }
        public void RunCS(Mat src)
        {
            hColor.Run(src);
            dst2 = hColor.dst3;
            labels[2] = hColor.labels[3];
            redC.Run(dst2);
            dst3 = task.cellMap;
            dst3.SetTo(0, task.noDepthMask);
            labels[3] = redC.labels[2];
        }
    }
    public class CS_RedCloud_OnlyColorAlt : CS_Parent
    {
        public RedCloud_Basics redMasks = new RedCloud_Basics();
        public CS_RedCloud_OnlyColorAlt(VBtask task) : base(task)
        {
            desc = "Track the color cells from floodfill - trying a minimalist approach to build cells.";
        }
        public void RunCS(Mat src)
        {
            redMasks.Run(src);
            List<rcData> lastCells = new List<rcData>(task.redCells);
            Mat lastMap = task.cellMap.Clone();
            Mat lastColors = dst3.Clone();
            List<rcData> newCells = new List<rcData>();
            task.cellMap.SetTo(0);
            dst3.SetTo(0);
            List<Vec3b> usedColors = new List<Vec3b> { black };
            int unmatched = 0;
            foreach (var cell in task.redCells)
            {
                int index = lastMap.Get<byte>(cell.maxDist.Y, cell.maxDist.X);
                if (index < lastCells.Count())
                {
                    cell.color = lastColors.Get<Vec3b>(cell.maxDist.Y, cell.maxDist.X);
                }
                else
                {
                    unmatched++;
                }
                if (usedColors.Contains(cell.color))
                {
                    unmatched++;
                    cell.color = randomCellColor();
                }
                usedColors.Add(cell.color);
                if (task.cellMap.Get<byte>(cell.maxDist.Y, cell.maxDist.X) == 0)
                {
                    cell.index = task.redCells.Count();
                    newCells.Add(cell);
                    task.cellMap[cell.rect].SetTo(cell.index, cell.mask);
                    dst3[cell.rect].SetTo(cell.color, cell.mask);
                }
            }
            task.redCells = new List<rcData>(newCells);
            labels[3] = task.redCells.Count().ToString() + " cells were identified.  The top " + 
                task.redOptions.getIdentifyCount().ToString() + " are numbered";
            labels[2] = redMasks.labels[3] + " " + unmatched.ToString() + " cells were not matched to previous frame.";
            if (task.redCells.Count() > 0) dst2 = ShowPalette(lastMap * 255 / task.redCells.Count());
        }
    }
    public class CS_RedCloud_Gaps : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        History_Basics frames = new History_Basics();
        public CS_RedCloud_Gaps(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            desc = "Find the gaps that are different in the RedCloud_Basics results.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[3];
            frames.Run(task.cellMap.InRange(0, 0));
            dst3 = frames.dst2;
            if (task.redCells.Count() > 0)
            {
                dst2[task.rc.rect].SetTo(Scalar.White, task.rc.mask);
            }
            if (task.redCells.Count() > 0)
            {
                var rc = task.redCells[0]; // index can now be zero.
                dst3[rc.rect].SetTo(0, rc.mask);
            }
            int count = dst3.CountNonZero();
            labels[3] = "Unclassified pixel count = " + count.ToString() + " or " + (count / src.Total()).ToString("0%");
        }
    }
    public class CS_RedCloud_SizeOrder : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_SizeOrder(VBtask task) : base(task)
        {
            task.redOptions.setUseColorOnly(true);
            UpdateAdvice(traceName + ": Use the goptions 'DebugSlider' to select which cell is isolated.");
            task.gOptions.setDebugSlider(0);
            desc = "Select blobs by size using the DebugSlider in the global options";
        }
        public void RunCS(Mat src)
        {
            SetTrueText("Use the goptions 'DebugSlider' to select cells by size." + "\n" + "Size order changes frequently.", 3);
            redC.Run(src);
            dst2 = redC.dst3;
            labels[2] = redC.labels[3];
            int index = task.gOptions.getDebugSlider();
            if (index < task.redCells.Count())
            {
                dst3.SetTo(0);
                var cell = task.redCells[index];
                dst3[cell.rect].SetTo(cell.color, cell.mask);
            }
        }
    }
    public class CS_RedCloud_StructuredH : CS_Parent
    {
        RedCloud_MotionBGsubtract motion = new RedCloud_MotionBGsubtract();
        Structured_TransformH transform = new Structured_TransformH();
        Projection_HistTop histTop = new Projection_HistTop();
        public CS_RedCloud_StructuredH(VBtask task) : base(task)
        {
            if (standalone)
            {
                task.redOptions.setIdentifyCells(false);
                task.gOptions.setDisplay1();
                task.gOptions.setDisplay1();
            }
            desc = "Display the RedCloud cells found with a horizontal slice through the cellMap.";
        }
        public void RunCS(Mat src)
        {
            Mat sliceMask = transform.createSliceMaskH();
            dst0 = src;
            motion.Run(sliceMask.Clone());
            if (task.heartBeat) dst1.SetTo(0);
            dst1.SetTo(Scalar.White, sliceMask);
            labels = motion.labels;
            dst2.SetTo(0);
            foreach (var rc in motion.redCells)
            {
                if (rc.motionFlag) DrawContour(dst2[rc.rect], rc.contour, vecToScalar(rc.color), -1);
            }
            Mat pc = new Mat(task.pointCloud.Size(), MatType.CV_32FC3, 0);
            task.pointCloud.CopyTo(pc, dst2.CvtColor(ColorConversionCodes.BGR2GRAY));
            histTop.Run(pc);
            dst3 = histTop.dst2;
            dst2.SetTo(Scalar.White, sliceMask);
            dst0.SetTo(Scalar.White, sliceMask);
        }
    }
    public class CS_RedCloud_StructuredV : CS_Parent
    {
        RedCloud_MotionBGsubtract motion = new RedCloud_MotionBGsubtract();
        Structured_TransformV transform = new Structured_TransformV();
        Projection_HistSide histSide = new Projection_HistSide();
        public CS_RedCloud_StructuredV(VBtask task) : base(task)
        {
            if (standalone)
            {
                task.redOptions.setIdentifyCells(false);
                task.gOptions.setDisplay1();
                task.gOptions.setDisplay1();
            }
            desc = "Display the RedCloud cells found with a vertical slice through the cellMap.";
        }
        public void RunCS(Mat src)
        {
            Mat sliceMask = transform.createSliceMaskV();
            dst0 = src;
            motion.Run(sliceMask.Clone());
            if (task.heartBeat) dst1.SetTo(0);
            dst1.SetTo(Scalar.White, sliceMask);
            labels = motion.labels;
            SetTrueText("Move mouse in image to see impact.", 3);
            dst2.SetTo(0);
            foreach (var rc in motion.redCells)
            {
                if (rc.motionFlag) DrawContour(dst2[rc.rect], rc.contour, vecToScalar(rc.color), -1);
            }
            Mat pc = new Mat(task.pointCloud.Size(), MatType.CV_32FC3, 0);
            task.pointCloud.CopyTo(pc, dst2.CvtColor(ColorConversionCodes.BGR2GRAY));
            histSide.Run(pc);
            dst3 = histSide.dst2;
            dst2.SetTo(Scalar.White, sliceMask);
            dst0.SetTo(Scalar.White, sliceMask);
        }
    }
    public class CS_RedCloud_MotionBasics : CS_Parent
    {
        public RedCloud_Basics redMasks = new RedCloud_Basics();
        public List<rcData> redCells = new List<rcData>();
        public RedCloud_MotionBGsubtract rMotion = new RedCloud_MotionBGsubtract();
        Mat lastColors;
        Mat lastMap;
        public CS_RedCloud_MotionBasics(VBtask task) : base(task)
        {
            lastColors = dst3.Clone();
            lastMap = dst2.Clone();
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            labels = new string[] { "", "Mask of active RedCloud cells", "CV_8U representation of redCells", "" };
            desc = "Track the color cells from floodfill - trying a minimalist approach to build cells.";
        }
        public void RunCS(Mat src)
        {
            redMasks.Run(src);
            rMotion.Run(task.color.Clone());
            List<rcData> lastCells = new List<rcData>(redCells);
            redCells.Clear();
            dst2.SetTo(0);
            dst3.SetTo(0);
            List<Vec3b> usedColors = new List<Vec3b> { black };
            int motionCount = 0;
            foreach (var nextCell in rMotion.redCells)
            {
                rcData cell = nextCell;
                int index = lastMap.At<byte>(cell.maxDist.Y, cell.maxDist.X);
                if (!cell.motionFlag)
                {
                    if (index > 0 && index < lastCells.Count()) cell = lastCells[index - 1];
                }
                else
                {
                    motionCount++;
                }
                if (index > 0 && index < lastCells.Count())
                {
                    cell.color = lastColors.At<Vec3b>(cell.maxDist.Y, cell.maxDist.X);
                }
                if (usedColors.Contains(cell.color)) cell.color = randomCellColor();
                usedColors.Add(cell.color);
                if (dst2.At<byte>(cell.maxDist.Y, cell.maxDist.X) == 0)
                {
                    cell.index = redCells.Count() + 1;
                    redCells.Add(cell);
                    dst2[cell.rect].SetTo(cell.index, cell.mask);
                    dst3[cell.rect].SetTo(cell.color, cell.mask);
                    SetTrueText(cell.index.ToString(), cell.maxDist, 2);
                    SetTrueText(cell.index.ToString(), cell.maxDist, 3);
                }
            }
            labels[3] = "There were " + redCells.Count() + " collected cells and " + motionCount +
                        " cells removed because of motion.  ";
            lastColors = dst3.Clone();
            lastMap = dst2.Clone();
            if (redCells.Count() > 0) dst1 = ShowPalette(lastMap * 255 / redCells.Count());
        }
    }
    public class CS_RedCloud_ContourVsFeatureLess : CS_Parent
    {
        RedCloud_Basics redMasks = new RedCloud_Basics();
        Contour_WholeImage contour = new Contour_WholeImage();
        FeatureLess_Basics fLess = new FeatureLess_Basics();
        System.Windows.Forms.RadioButton useContours;
        public CS_RedCloud_ContourVsFeatureLess(VBtask task) : base(task)
        {
            useContours = FindRadio("Use Contour_WholeImage");
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "Contour_WholeImage Input", "RedCloud_Basics - toggling between Contour and Featureless inputs", "FeatureLess_Basics Input" };
            desc = "Compare Contour_WholeImage and FeatureLess_Basics as input to RedCloud_Basics";
        }
        public void RunCS(Mat src)
        {
            contour.Run(src);
            dst1 = contour.dst2;
            fLess.Run(src);
            dst3 = fLess.dst2;
            if (task.toggleOnOff) redMasks.Run(dst3);
            else redMasks.Run(dst1);
            dst2 = redMasks.dst3;
        }
    }
    public class CS_RedCloud_UnmatchedCount : CS_Parent
    {
        public List<rcData> redCells = new List<rcData>();
        int myFrameCount;
        List<int> changedCellCounts = new List<int>();
        List<int> framecounts = new List<int>();
        List<cv.Point> frameLoc = new List<cv.Point>();
        public CS_RedCloud_UnmatchedCount(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            desc = "Count the unmatched cells and display them.";
        }
        public void RunCS(Mat src)
        {
            myFrameCount++;
            if (standaloneTest())
            {
                SetTrueText("CS_RedCloud_UnmatchedCount has no output when run standaloneTest()." + "\n" +
                            "It requires redCells and RedCloud_Basics is the only way to create redCells." + "\n" +
                            "Since RedCloud_Basics calls CS_RedCloud_UnmatchedCount, it would be circular and never finish the initialize.");
                return;
            }
            int unMatchedCells = 0;
            int mostlyColor = 0;
            for (int i = 0; i < redCells.Count(); i++)
            {
                var rc = redCells[i];
                if (redCells[i].depthPixels / redCells[i].pixels < 0.5) mostlyColor++;
                if (rc.indexLast != 0)
                {
                    byte val = dst3.At<byte>(rc.maxDist.Y, rc.maxDist.X);
                    if (val == 0)
                    {
                        dst3[rc.rect].SetTo(255, rc.mask);
                        unMatchedCells++;
                        frameLoc.Add(rc.maxDist);
                        framecounts.Add(myFrameCount);
                    }
                }
            }
            if (ShowIntermediate())
            {
                for (int i = 0; i < framecounts.Count(); i++)
                {
                    SetTrueText(framecounts[i].ToString(), frameLoc[i], 2);
                }
            }
            changedCellCounts.Add(unMatchedCells);
            if (task.heartBeat)
            {
                dst3.SetTo(0);
                framecounts.Clear();
                frameLoc.Clear();
                myFrameCount = 0;
                int sum = changedCellCounts.Sum();
                double avg = changedCellCounts.Count() > 0 ? changedCellCounts.Average() : 0;
                labels[3] = sum + " new/moved cells in the last second " + string.Format(fmt1, avg) + " changed per frame";
                labels[2] = redCells.Count() + " cells, unmatched cells = " + unMatchedCells + "   " +
                            mostlyColor + " cells were mostly color and " + (redCells.Count() - mostlyColor) + " had depth.";
                changedCellCounts.Clear();
            }
        }
    }
    public class CS_RedCloud_ContourUpdate : CS_Parent
    {
        public List<rcData> redCells = new List<rcData>();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_ContourUpdate(VBtask task) : base(task)
        {
            desc = "For each cell, add a contour if its count is zero.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                redC.Run(src);
                dst2 = redC.dst2;
                labels = redC.labels;
                redCells = task.redCells;
            }
            dst3.SetTo(0);
            for (int i = 1; i < redCells.Count(); i++)
            {
                var rc = redCells[i];
                rc.contour = contourBuild(rc.mask, ContourApproximationModes.ApproxNone);
                DrawContour(rc.mask, rc.contour, 255, -1);
                redCells[i] = rc;
                DrawContour(dst3[rc.rect], rc.contour, vecToScalar(rc.color), -1);
            }
        }
    }
    public class CS_RedCloud_MaxDist : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        RedCloud_ContourUpdate addTour = new RedCloud_ContourUpdate();
        public CS_RedCloud_MaxDist(VBtask task) : base(task)
        {
            desc = "Show the maxdist before and after updating the mask with the contour.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels = redC.labels;
            foreach (var rc in task.redCells)
            {
                DrawCircle(dst2, rc.maxDist, task.DotSize, task.HighlightColor);
            }
            addTour.redCells = task.redCells;
            addTour.Run(src);
            dst3 = addTour.dst3;
            for (int i = 1; i < addTour.redCells.Count(); i++)
            {
                var rc = addTour.redCells[i];
                rc.maxDist = GetMaxDist(ref rc);
                DrawCircle(dst3, rc.maxDist, task.DotSize, task.HighlightColor);
            }
        }
    }
    public class CS_RedCloud_Tiers : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Depth_TiersZ tiers = new Depth_TiersZ();
        Bin4Way_Regions binar4 = new Bin4Way_Regions();
        public CS_RedCloud_Tiers(VBtask task) : base(task)
        {
            task.redOptions.setUseColorOnly(true);
            desc = "Use the Depth_TiersZ algorithm to create a color-based RedCloud";
        }
        public void RunCS(Mat src)
        {
            binar4.Run(src);
            dst1 = ShowPalette((binar4.dst2 * 255 / binar4.classCount).ToMat());
            tiers.Run(src);
            dst3 = tiers.dst3;
            dst0 = tiers.dst2 + binar4.dst2;
            redC.Run(dst0);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
        }
    }
    public class CS_RedCloud_TiersBinarize : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Depth_TiersZ tiers = new Depth_TiersZ();
        Bin4Way_Regions binar4 = new Bin4Way_Regions();
        public CS_RedCloud_TiersBinarize(VBtask task) : base(task)
        {
            task.redOptions.setUseColorOnly(true);
            desc = "Use the Depth_TiersZ with Bin4Way_Regions algorithm to create a color-based RedCloud";
        }
        public void RunCS(Mat src)
        {
            binar4.Run(src);
            tiers.Run(src);
            dst2 = tiers.dst2 + binar4.dst2;
            redC.Run(dst2);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
        }
    }
    public class CS_RedCloud_Combine : CS_Parent
    {
        public Color8U_Basics colorClass = new Color8U_Basics();
        public GuidedBP_Depth guided = new GuidedBP_Depth();
        public RedCloud_Basics redMasks = new RedCloud_Basics();
        public List<rcData> combinedCells = new List<rcData>();
        Depth_MaxMask maxDepth = new Depth_MaxMask();
        RedCloud_Reduce prep = new RedCloud_Reduce();
        public CS_RedCloud_Combine(VBtask task) : base(task)
        {
            desc = "Combined the color and cloud as indicated in the RedOptions panel.";
        }
        public void RunCS(Mat src)
        {
            maxDepth.Run(src);
            if (task.redOptions.getUseColorOnly() || task.redOptions.getUseGuidedProjection())
            {
                redMasks.inputMask.SetTo(0);
                if (src.Channels() == 3)
                {
                    colorClass.Run(src);
                    dst2 = colorClass.dst2.Clone();
                }
                else
                {
                    dst2 = src;
                }
            }
            else
            {
                redMasks.inputMask = task.noDepthMask;
                dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            }
            if (task.redOptions.getUseDepth() || task.redOptions.getUseGuidedProjection())
            {
                switch (task.redOptions.depthInputIndex)
                {
                    case 0: // "GuidedBP_Depth"
                        guided.Run(src);
                        if (colorClass.classCount > 0) guided.dst2 += colorClass.classCount;
                        guided.dst2.CopyTo(dst2, task.depthMask);
                        break;
                    case 1: // "RedCloud_Reduce"
                        prep.Run(task.pointCloud);
                        if (colorClass.classCount > 0) prep.dst2 += colorClass.classCount;
                        prep.dst2.CopyTo(dst2, task.depthMask);
                        break;
                }
            }
            redMasks.Run(dst2);
            dst2 = redMasks.dst2;
            dst3 = redMasks.dst3;
            combinedCells.Clear();
            bool drawRectOnlyRun = task.drawRect.Width * task.drawRect.Height > 10;
            foreach (var rc in task.redCells)
            {
                if (drawRectOnlyRun && !task.drawRect.Contains(rc.floodPoint)) continue;
                combinedCells.Add(rc);
            }
        }
    }

    public class CS_RedCloud_TopX : CS_Parent
    {
        public RedCloud_Basics redC = new RedCloud_Basics();
        public Options_TopX options = new Options_TopX();
        public CS_RedCloud_TopX(VBtask task) : base(task)
        {
            desc = "Show only the top X cells";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            redC.Run(src);
            dst2.SetTo(0);
            foreach (var rc in task.redCells)
            {
                dst2[rc.rect].SetTo(rc.color, rc.mask);
                if (rc.index > options.topX) break;
            }
            labels[2] = $"The top {options.topX} RedCloud cells by size.";
        }
    }
    public class CS_RedCloud_TopXNeighbors : CS_Parent
    {
        Options_TopX options = new Options_TopX();
        Neighbors_Precise nab = new Neighbors_Precise();
        public CS_RedCloud_TopXNeighbors(VBtask task) : base(task)
        {
            nab.runRedCloud = true;
            desc = "Add unused neighbors to each of the top X cells";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            nab.Run(src);
            SetTrueText("Review the neighbors_Precise algorithm");
            // The commented code has been omitted for brevity
        }
    }
    public class CS_RedCloud_TopXHulls : CS_Parent
    {
        RedCloud_TopX topX = new RedCloud_TopX();
        public CS_RedCloud_TopXHulls(VBtask task) : base(task)
        {
            desc = "Build the hulls for the top X RedCloud cells";
        }
        public void RunCS(Mat src)
        {
            topX.Run(src);
            labels = topX.redC.labels;
            var newCells = new List<rcData>();
            task.cellMap.SetTo(0);
            dst2.SetTo(0);
            foreach (var rc in task.redCells)
            {
                if (rc.contour.Count() >= 5)
                {
                    rc.hull = Cv2.ConvexHull(rc.contour.ToArray(), true).ToList();
                    DrawContour(dst2[rc.rect], rc.hull, vecToScalar(rc.color), -1);
                    DrawContour(rc.mask, rc.hull, 255, -1);
                    task.cellMap[rc.rect].SetTo(rc.index, rc.mask);
                }
                newCells.Add(rc);
                if (rc.index > topX.options.topX) break;
            }
            task.redCells = new List<rcData>(newCells);
            task.setSelectedContour();
        }
    }
    public class CS_RedCloud_Hue : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Color8U_Hue hue = new Color8U_Hue();
        public CS_RedCloud_Hue(VBtask task) : base(task)
        {
            task.redOptions.setUseColorOnly(true);
            desc = "Run RedCloud on just the red hue regions.";
        }
        public void RunCS(Mat src)
        {
            hue.Run(src);
            dst3 = hue.dst2;
            redC.inputMask = ~dst3;
            redC.Run(src);
            dst2 = redC.dst2;
        }
    }
    public class CS_RedCloud_GenCellContains : CS_Parent
    {
        Flood_Basics flood = new Flood_Basics();
        Flood_ContainedCells contains = new Flood_ContainedCells();
        public CS_RedCloud_GenCellContains(VBtask task) : base(task)
        {
            task.redOptions.setIdentifyCells(true);
            desc = "Merge cells contained in the top X cells and remove all other cells.";
        }
        public void RunCS(Mat src)
        {
            flood.Run(src);
            dst3 = flood.dst2;
            if (task.heartBeat) return;
            labels[2] = flood.labels[2];
            contains.Run(src);
            dst2.SetTo(0);
            int count = Math.Min(task.redOptions.identifyCount, task.redCells.Count());
            for (int i = 0; i < count; i++)
            {
                var rc = task.redCells[i];
                dst2[rc.rect].SetTo(rc.color, rc.mask);
                dst2.Rectangle(rc.rect, task.HighlightColor, task.lineWidth);
            }
            for (int i = task.redOptions.identifyCount; i < task.redCells.Count(); i++)
            {
                var rc = task.redCells[i];
                dst2[rc.rect].SetTo(task.redCells[rc.container].color, rc.mask);
            }
        }
    }
    public class CS_RedCloud_PlusTiers : CS_Parent
    {
        Depth_TiersZ tiers = new Depth_TiersZ();
        Bin4Way_Regions binar4 = new Bin4Way_Regions();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_PlusTiers(VBtask task) : base(task)
        {
            desc = "Add the depth tiers to the input for RedCloud_Basics.";
        }
        public void RunCS(Mat src)
        {
            tiers.Run(src);
            binar4.Run(src);
            redC.Run(binar4.dst2 + tiers.dst2);
            dst2 = redC.dst2;
            labels = redC.labels;
        }
    }

    public class CS_RedCloud_Depth : CS_Parent
    {
        Flood_Basics flood = new Flood_Basics();
        public CS_RedCloud_Depth(VBtask task) : base(task)
        {
            task.redOptions.setUseDepth(true);
            desc = "Create RedCloud output using only depth.";
        }
        public void RunCS(Mat src)
        {
            flood.Run(src);
            dst2 = flood.dst2;
            labels[2] = flood.labels[2];
        }
    }
    public class CS_RedCloud_Consistent1 : CS_Parent
    {
        Bin3Way_RedCloud redC = new Bin3Way_RedCloud();
        Diff_Basics diff = new Diff_Basics();
        List<Mat> cellmaps = new List<Mat>();
        List<List<rcData>> cellLists = new List<List<rcData>>();
        List<Mat> diffs = new List<Mat>();
        public CS_RedCloud_Consistent1(VBtask task) : base(task)
        {
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            task.gOptions.pixelDiffThreshold = 1;
            desc = "Remove RedCloud results that are inconsistent with the previous frame.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            diff.Run(task.cellMap);
            dst1 = diff.dst2;
            cellLists.Add(new List<rcData>(task.redCells));
            cellmaps.Add(task.cellMap & ~dst1);
            diffs.Add(dst1.Clone());
            task.redCells.Clear();
            task.redCells.Add(new rcData());
            for (int i = 0; i < cellLists.Count(); i++)
            {
                foreach (var rc in cellLists[i])
                {
                    bool present = true;
                    for (int j = 0; j < cellmaps.Count(); j++)
                    {
                        var val = cellmaps[i].At<byte>(rc.maxDist.Y, rc.maxDist.X);
                        if (val == 0)
                        {
                            present = false;
                            break;
                        }
                    }
                    if (present)
                    {
                        rc.index = task.redCells.Count();
                        task.redCells.Add(rc);
                    }
                }
            }
            dst2.SetTo(0);
            task.cellMap.SetTo(0);
            foreach (var rc in task.redCells)
            {
                dst2[rc.rect].SetTo(rc.color, rc.mask);
                task.cellMap[rc.rect].SetTo(rc.index, rc.mask);
            }
            foreach (var mat in diffs)
            {
                dst2.SetTo(0, mat);
            }
            if (cellmaps.Count() > task.frameHistoryCount)
            {
                cellmaps.RemoveAt(0);
                cellLists.RemoveAt(0);
                diffs.RemoveAt(0);
            }
        }
    }
    public class CS_RedCloud_Consistent2 : CS_Parent
    {
        Bin3Way_RedCloud redC = new Bin3Way_RedCloud();
        Diff_Basics diff = new Diff_Basics();
        List<Mat> cellmaps = new List<Mat>();
        List<List<rcData>> cellLists = new List<List<rcData>>();
        List<Mat> diffs = new List<Mat>();
        public CS_RedCloud_Consistent2(VBtask task) : base(task)
        {
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            task.gOptions.pixelDiffThreshold = 1;
            desc = "Remove RedCloud results that are inconsistent with the previous frame.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            diff.Run(task.cellMap);
            dst1 = diff.dst2;
            cellLists.Add(new List<rcData>(task.redCells));
            cellmaps.Add(task.cellMap & ~dst1);
            diffs.Add(dst1.Clone());
            task.redCells.Clear();
            task.redCells.Add(new rcData());
            for (int i = 0; i < cellLists.Count(); i++)
            {
                foreach (var rc in cellLists[i])
                {
                    bool present = true;
                    for (int j = 0; j < cellmaps.Count(); j++)
                    {
                        var val = cellmaps[i].At<byte>(rc.maxDist.Y, rc.maxDist.X);
                        if (val == 0)
                        {
                            present = false;
                            break;
                        }
                    }
                    if (present)
                    {
                        rc.index = task.redCells.Count();
                        task.redCells.Add(rc);
                    }
                }
            }
            dst2.SetTo(0);
            task.cellMap.SetTo(0);
            foreach (var rc in task.redCells)
            {
                dst2[rc.rect].SetTo(rc.color, rc.mask);
                task.cellMap[rc.rect].SetTo(rc.index, rc.mask);
            }
            foreach (var mat in diffs)
            {
                dst2.SetTo(0, mat);
            }
            if (cellmaps.Count() > task.frameHistoryCount)
            {
                cellmaps.RemoveAt(0);
                cellLists.RemoveAt(0);
                diffs.RemoveAt(0);
            }
        }
    }
    public class CS_RedCloud_Consistent : CS_Parent
    {
        Bin3Way_RedCloud redC = new Bin3Way_RedCloud();
        List<Mat> cellmaps = new List<Mat>();
        List<List<rcData>> cellLists = new List<List<rcData>>();
        Mat lastImage;
        public CS_RedCloud_Consistent(VBtask task) : base(task)
        {
            lastImage = redC.dst2.Clone();
            desc = "Remove RedCloud results that are inconsistent with the previous frame(s).";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            cellLists.Add(new List<rcData>(task.redCells));
            cellmaps.Add(task.cellMap.Clone());
            List<rcData> newCells = new List<rcData>();
            newCells.Add(new rcData());
            foreach (var rc in task.redCells)
            {
                var maxDStable = rc.maxDStable;
                int count = 0;
                List<int> sizes = new List<int>();
                List<rcData> redData = new List<rcData>();
                for (int i = 0; i < cellmaps.Count(); i++)
                {
                    int index = cellmaps[i].Get<Byte>(rc.maxDStable.Y, rc.maxDStable.X);
                    if (cellLists[i][index].maxDStable == maxDStable)
                    {
                        count++;
                        sizes.Add(cellLists[i][index].pixels);
                        redData.Add(cellLists[i][index]);
                    }
                    else
                    {
                        break;
                    }
                }
                if (count == cellmaps.Count())
                {
                    int index = sizes.IndexOf(sizes.Max());
                    rcData rcNext = rc;
                    rcNext = redData[index];
                    var color = lastImage.Get<Vec3b>(rcNext.maxDStable.Y, rcNext.maxDStable.X);
                    if (color != black) rcNext.color = color;
                    rcNext.index = newCells.Count();
                    newCells.Add(rcNext);
                }
            }
            task.redCells = new List<rcData>(newCells);
            dst2 = DisplayCells();
            lastImage = dst2.Clone();
            if (cellmaps.Count() > task.frameHistoryCount)
            {
                cellmaps.RemoveAt(0);
                cellLists.RemoveAt(0);
            }
        }
    }
    public class CS_RedCloud_NaturalColor : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_NaturalColor(VBtask task) : base(task)
        {
            desc = "Display the RedCloud results with the mean color of the cell";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            labels[2] = redC.labels[2];
            dst2 = DisplayCells();
        }
    }

    public class CS_RedCloud_MotionBGsubtract : CS_Parent
    {
        public BGSubtract_Basics bgSub = new BGSubtract_Basics();
        public List<rcData> redCells = new List<rcData>();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedCloud_MotionBGsubtract(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            task.gOptions.pixelDiffThreshold = 25;
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            desc = "Use absDiff to build a mask of cells that changed.";
        }
        public void RunCS(Mat src)
        {
            bgSub.Run(src);
            dst3 = bgSub.dst2;
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[3];
            redCells.Clear();
            dst1.SetTo(0);
            foreach (var rc in task.redCells)
            {
                Mat tmp = rc.mask & bgSub.dst2[rc.rect];
                if (tmp.CountNonZero() > 0)
                {
                    dst1[rc.rect].SetTo(rc.color, rc.mask);
                    rc.motionFlag = true;
                }
                redCells.Add(rc);
            }
        }
    }
    public class CS_RedCloud_JoinCells : CS_Parent
    {
        FeatureLess_RedCloud fLess = new FeatureLess_RedCloud();
        public CS_RedCloud_JoinCells(VBtask task) : base(task)
        {
            task.gOptions.setHistogramBins(20);
            labels = new string[] { "", "FeatureLess_RedCloud output.", "RedCloud_Basics output", "RedCloud_Basics cells joined by using the color from the FeatureLess_RedCloud cellMap" };
            desc = "Run RedCloud_Basics and use FeatureLess_RedCloud to join cells that are in the same featureless regions.";
        }
        public void RunCS(Mat src)
        {
            fLess.Run(src);
            dst2 = fLess.dst2;
            labels[2] = fLess.labels[2];
            dst3.SetTo(0);
            foreach (var rc in task.redCells)
            {
                var color = fLess.dst2.Get<Vec3b>(rc.maxDist.Y, rc.maxDist.X);
                dst3[rc.rect].SetTo(color, rc.mask);
            }
        }
    }
    public class CS_RedCloud_LeftRight : CS_Parent
    {
        Flood_LeftRight redC = new Flood_LeftRight();
        public CS_RedCloud_LeftRight(VBtask task) : base(task)
        {
            if (standalone) task.gOptions.setDisplay1();
            desc = "Placeholder to make it easier to find where left and right images are floodfilled.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst1 = redC.dst1;
            dst2 = redC.dst2;
            dst3 = redC.dst3;
            labels = redC.labels;
        }
    }
    public class CS_RedCloud_ColorAndDepth : CS_Parent
    {
        Flood_Basics flood = new Flood_Basics();
        Flood_Basics floodPC = new Flood_Basics();
        List<rcData> colorCells = new List<rcData>();
        Mat colorMap;
        List<rcData> depthCells = new List<rcData>();
        Mat depthMap;
        int mousePicTag;
        public CS_RedCloud_ColorAndDepth(VBtask task) : base(task)
        {
            colorMap = new Mat(dst2.Size(), MatType.CV_8U, 0);
            depthMap = new Mat(dst2.Size(), MatType.CV_8U, 0);
            mousePicTag = task.mousePicTag;
            task.redOptions.setIdentifyCells(false);
            desc = "Run Flood_Basics and use the cells to map the depth cells";
        }
        public void RunCS(Mat src)
        {
            task.redOptions.setUseColorOnly(true);
            task.redCells = new List<rcData>(colorCells);
            task.cellMap = colorMap.Clone();
            flood.Run(src);
            dst2 = flood.dst2;
            colorCells = new List<rcData>(task.redCells);
            colorMap = task.cellMap.Clone();
            labels[2] = flood.labels[2];
            task.redOptions.setUseDepth(true);
            task.redCells = new List<rcData>(depthCells);
            task.cellMap = depthMap.Clone();
            floodPC.Run(src);
            dst3 = floodPC.dst2;
            depthCells = new List<rcData>(task.redCells);
            depthMap = task.cellMap.Clone();
            labels[3] = floodPC.labels[2];
            if (task.mouseClickFlag) mousePicTag = task.mousePicTag;
            switch (mousePicTag)
            {
                case 1:
                    // setSelectedContour();
                    break;
                case 2:
                    task.setSelectedContour(ref colorCells, ref colorMap);
                    break;
                case 3:
                    task.setSelectedContour(ref depthCells, ref depthMap);
                    break;
            }
            dst2.Rectangle(task.rc.rect, task.HighlightColor, task.lineWidth);
            dst3[task.rc.rect].SetTo(Scalar.White, task.rc.mask);
        }
    }
    public class CS_RedCloud_Delaunay : CS_Parent
    {
        RedCloud_CPP redCPP = new RedCloud_CPP();
        Feature_Delaunay delaunay = new Feature_Delaunay();
        Color8U_Basics color;
        public CS_RedCloud_Delaunay(VBtask task) : base(task)
        {
            desc = "Test Feature_Delaunay points after Delaunay contours have been added.";
        }
        public void RunCS(Mat src)
        {
            delaunay.Run(src);
            dst1 = delaunay.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (src.Channels() != 1)
            {
                if (color == null) color = new Color8U_Basics();
                color.Run(src);
                src = color.dst2;
            }
            redCPP.inputMask = dst1;
            redCPP.Run(src);
            dst2 = redCPP.dst2;
            labels[2] = redCPP.labels[2];
        }
    }
    public class CS_RedCloud_CPP : CS_Parent
    {
        public Mat inputMask;
        public int classCount;
        public List<cv.Rect> rectList = new List<cv.Rect>();
        public List<cv.Point> floodPoints = new List<cv.Point>();
        Color8U_Basics color;
        public CS_RedCloud_CPP(VBtask task) : base(task)
        {
            inputMask = new Mat(dst2.Size(), MatType.CV_8U, 0);
            cPtr = RedCloud_Open();
            desc = "Run the C++ RedCloud interface with or without a mask";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
            {
                if (color == null) color = new Color8U_Basics();
                color.Run(src);
                src = color.dst2;
            }
            IntPtr imagePtr;
            byte[] inputData = new byte[src.Total()];
            Marshal.Copy(src.Data, inputData, 0, inputData.Length);
            GCHandle handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned);
            byte[] maskData = new byte[inputMask.Total()];
            Marshal.Copy(inputMask.Data, maskData, 0, maskData.Length);
            GCHandle handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned);
            imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols);
            handleMask.Free();
            handleInput.Free();
            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8U, imagePtr).Clone();
            classCount = RedCloud_Count(cPtr);
            if (classCount == 0) return; // no data to process.
            Mat rectData = new Mat(classCount, 1, MatType.CV_32SC4, RedCloud_Rects(cPtr));
            Mat floodPointData = new Mat(classCount, 1, MatType.CV_32SC2, RedCloud_FloodPoints(cPtr));
            int[] rects = new int[classCount * 4];
            Marshal.Copy(rectData.Data, rects, 0, rects.Length);
            int[] ptList = new int[classCount * 2];
            Marshal.Copy(floodPointData.Data, ptList, 0, ptList.Length);
            rectList.Clear();
            for (int i = 0; i < rects.Length - 4; i += 4)
            {
                rectList.Add(new cv.Rect(rects[i], rects[i + 1], rects[i + 2], rects[i + 3]));
            }
            floodPoints.Clear();
            for (int i = 0; i < ptList.Length - 2; i += 2)
            {
                floodPoints.Add(new cv.Point(ptList[i], ptList[i + 1]));
            }
            if (standalone) dst3 = ShowPalette(dst2 * 255 / classCount);
            if (task.heartBeat) labels[2] = "CV_8U result with " + classCount.ToString() + " regions.";
            if (task.heartBeat) labels[3] = "Palette version of the data in dst2 with " + classCount.ToString() + " regions.";
        }
        public void Close()
        {
            if (cPtr != (IntPtr)0) cPtr = RedCloud_Close(cPtr);
        }
    }
    public class CS_RedCloud_MaxDist_CPP : CS_Parent
    {
        public int classCount;
        public List<cv.Rect> RectList = new List<cv.Rect>();
        public List<cv.Point> floodPoints = new List<cv.Point>();
        public List<int> maxList = new List<int>();
        Color8U_Basics color = new Color8U_Basics();
        public CS_RedCloud_MaxDist_CPP(VBtask task) : base(task)
        {
            cPtr = RedCloudMaxDist_Open();
            desc = "Run the C++ RedCloudMaxDist interface without a mask";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
            {
                color.Run(src);
                src = color.dst2;
            }
            if (task.heartBeat) maxList.Clear(); // reevaluate all cells.
            int[] maxArray = maxList.ToArray();
            GCHandle handleMaxList = GCHandle.Alloc(maxArray, GCHandleType.Pinned);
            RedCloudMaxDist_SetPoints(cPtr, maxList.Count() / 2, handleMaxList.AddrOfPinnedObject());
            handleMaxList.Free();
            IntPtr imagePtr;
            byte[] inputData = new byte[src.Total()];
            Marshal.Copy(src.Data, inputData, 0, inputData.Length);
            GCHandle handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned);
            imagePtr = RedCloudMaxDist_Run(cPtr, handleInput.AddrOfPinnedObject(), (IntPtr)0, src.Rows, src.Cols);
            handleInput.Free();
            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8U, imagePtr).Clone();
            dst3 = ShowPalette(dst2);
            classCount = RedCloudMaxDist_Count(cPtr);
            labels[2] = "CV_8U version with " + classCount.ToString() + " cells.";
            if (classCount == 0) return; // no data to process.
            Mat rectData = new Mat(classCount, 1, MatType.CV_32SC4, RedCloudMaxDist_Rects(cPtr));
            Mat floodPointData = new Mat(classCount, 1, MatType.CV_32SC2, RedCloudMaxDist_FloodPoints(cPtr));
            int[] rects = new int[classCount * 4];
            Marshal.Copy(rectData.Data, rects, 0, rects.Length);
            int[] ptList = new int[classCount * 2];
            Marshal.Copy(floodPointData.Data, ptList, 0, ptList.Length);
            for (int i = 0; i < rects.Length - 4; i += 4)
            {
                RectList.Add(new cv.Rect(rects[i], rects[i + 1], rects[i + 2], rects[i + 3]));
            }
            for (int i = 0; i < ptList.Length - 2; i += 2)
            {
                floodPoints.Add(new cv.Point(ptList[i], ptList[i + 1]));
            }
        }
        public void Close()
        {
            if (cPtr != (IntPtr)0) cPtr = RedCloudMaxDist_Close(cPtr);
        }
    }

    public class CS_RedCloud_Reduce : CS_Parent
    {
        public int classCount;
        Options_RedCloudOther options = new Options_RedCloudOther();
        public CS_RedCloud_Reduce(VBtask task) : base(task)
        {
            desc = "Reduction transform for the point cloud";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            task.pointCloud.ConvertTo(dst0, MatType.CV_32S, 1000 / options.reduceAmt);
            var split = dst0.Split();
            switch (task.redOptions.PointCloudReduction)
            {
                case 0: // "X Reduction"
                    dst0 = (split[0] * options.reduceAmt).ToMat();
                    break;
                case 1: // "Y Reduction"
                    dst0 = (split[1] * options.reduceAmt).ToMat();
                    break;
                case 2: // "Z Reduction"
                    dst0 = (split[2] * options.reduceAmt).ToMat();
                    break;
                case 3: // "XY Reduction"
                    dst0 = (split[0] * options.reduceAmt + split[1] * options.reduceAmt).ToMat();
                    break;
                case 4: // "XZ Reduction"
                    dst0 = (split[0] * options.reduceAmt + split[2] * options.reduceAmt).ToMat();
                    break;
                case 5: // "YZ Reduction"
                    dst0 = (split[1] * options.reduceAmt + split[2] * options.reduceAmt).ToMat();
                    break;
                case 6: // "XYZ Reduction"
                    dst0 = (split[0] * options.reduceAmt + split[1] * options.reduceAmt + split[2] * options.reduceAmt).ToMat();
                    break;
            }
            var mm = GetMinMax(dst0);
            dst2 = (dst0 - mm.minVal);
            dst2 = dst2 * 255 / (mm.maxVal - mm.minVal);
            dst2.ConvertTo(dst2, MatType.CV_8U);
            labels[2] = "Reduced Pointcloud - reduction factor = " + options.reduceAmt.ToString() + " produced " + classCount.ToString() + " regions";
        }
    }
    public class CS_RedCloud_NaturalGray : CS_Parent
    {
        RedCloud_Consistent redC = new RedCloud_Consistent();
        Options_RedCloudOther options = new Options_RedCloudOther();
        public CS_RedCloud_NaturalGray(VBtask task) : base(task)
        {
            desc = "Display the RedCloud results with the mean grayscale value of the cell +- delta";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            var rc = task.rc;
            var val = (int)(0.299 * rc.colorMean[0] + 0.587 * rc.colorMean[1] + 0.114 * rc.colorMean[2]);
            dst1 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            dst0 = dst1.InRange(val - options.range, val + options.range);
            var color = new Vec3b((byte)rc.colorMean[0], (byte)rc.colorMean[1], (byte)rc.colorMean[2]);
            dst3.SetTo(0);
            dst3.SetTo(Scalar.White, dst0);
        }
    }
    public class CS_RedCloud_FeatureLessReduce : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        FeatureROI_Basics devGrid = new FeatureROI_Basics();
        public List<rcData> redCells = new List<rcData>();
        public Mat cellMap;
        AddWeighted_Basics addw = new AddWeighted_Basics();
        Options_RedCloudOther options = new Options_RedCloudOther();
        public CS_RedCloud_FeatureLessReduce(VBtask task) : base(task)
        {
            cellMap = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Remove any cells which are in a featureless region - they are part of the neighboring (and often surrounding) region.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            devGrid.Run(src);
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            dst3.SetTo(0);
            redCells.Clear();
            foreach (var rc in task.redCells)
            {
                var tmp = new Mat(rc.mask.Size(), MatType.CV_8U, 0);
                devGrid.dst3[rc.rect].CopyTo(tmp, rc.mask);
                var count = tmp.CountNonZero();
                if (count == 0 || rc.pixels == 0) continue;
                if (count / rc.pixels < options.threshold)
                {
                    dst3[rc.rect].SetTo(rc.color, rc.mask);
                    rc.index = redCells.Count();
                    redCells.Add(rc);
                    cellMap[rc.rect].SetTo(rc.index, rc.mask);
                }
            }
            addw.src2 = devGrid.dst3.CvtColor(ColorConversionCodes.GRAY2BGR);
            addw.Run(dst2);
            dst2 = addw.dst2;
            labels[3] = $"{redCells.Count()} cells after removing featureless cells that were part of their surrounding.  " +
                        $"{task.redCells.Count() - redCells.Count()} were removed.";
            task.setSelectedContour();
        }
    }
    public class CS_RedCloud_Features : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Options_RedCloudFeatures options = new Options_RedCloudFeatures();
        public CS_RedCloud_Features(VBtask task) : base(task)
        {
            desc = "Display And validate the keyPoints for each RedCloud cell";
        }
        Vec3b vbNearFar(float factor)
        {
            var nearYellow = new Vec3b(255, 0, 0);
            var farBlue = new Vec3b(0, 255, 255);
            if (float.IsNaN(factor)) return new Vec3b();
            if (factor > 1) factor = 1;
            if (factor < 0) factor = 0;
            return new Vec3b((byte)((1 - factor) * farBlue.Item0 + factor * nearYellow.Item0),
                             (byte)((1 - factor) * farBlue.Item1 + factor * nearYellow.Item1),
                             (byte)((1 - factor) * farBlue.Item2 + factor * nearYellow.Item2));
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            redC.Run(src);
            dst2 = redC.dst2;
            var rc = task.rc;
            dst0 = task.color;
            var correlationMat = new Mat();
            float correlationXtoZ = 0.0f, correlationYtoZ = 0.0f;
            dst3.SetTo(0);
            switch (options.selection)
            {
                case 0:
                    var pt = rc.maxDist;
                    dst2.Circle(pt, task.DotSize, task.HighlightColor, -1, LineTypes.AntiAlias);
                    labels[3] = "maxDist Is at (" + pt.X + ", " + pt.Y + ")";
                    break;
                case 1:
                    dst3[rc.rect].SetTo(vbNearFar((float)((rc.depthMean[2]) / task.MaxZmeters)), rc.mask);
                    labels[3] = "rc.depthMean(2) Is highlighted in dst2";
                    labels[3] = "Mean depth for the cell Is " + rc.depthMean[2].ToString("F3");
                    break;
                case 2:
                    Cv2.MatchTemplate(task.pcSplit[0][rc.rect], task.pcSplit[2][rc.rect], correlationMat, TemplateMatchModes.CCoeffNormed, rc.mask);
                    correlationXtoZ = correlationMat.Get<float>(0, 0);
                    labels[3] = "High correlation X to Z Is yellow, low correlation X to Z Is blue";
                    break;
                case 3:
                    Cv2.MatchTemplate(task.pcSplit[1][rc.rect], task.pcSplit[2][rc.rect], correlationMat, TemplateMatchModes.CCoeffNormed, rc.mask);
                    correlationYtoZ = correlationMat.Get<float>(0, 0);
                    labels[3] = "High correlation Y to Z Is yellow, low correlation Y to Z Is blue";
                    break;
            }
            if (options.selection == 2 || options.selection == 3)
            {
                dst3[rc.rect].SetTo(vbNearFar((options.selection == 2 ? correlationXtoZ : correlationYtoZ) + 1), rc.mask);
                SetTrueText("(" + correlationXtoZ.ToString("F3") + ", " + correlationYtoZ.ToString("F3") + ")", new cv.Point(rc.rect.X, rc.rect.Y), 3);
            }
            DrawContour(dst0[rc.rect], rc.contour, Scalar.Yellow);
            SetTrueText(labels[3], 3);
            labels[2] = "Highlighted feature = " + options.labelName;
        }
    }
    public class CS_RedTrack_Basics : CS_Parent
    {
        Cell_Basics stats = new Cell_Basics();
        public RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedTrack_Basics(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            if (task.WorkingRes != new cv.Size(168, 94)) task.frameHistoryCount = 1;
            desc = "Get stats on each RedCloud cell.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            stats.Run(src);
            labels = stats.labels;
            dst2.SetTo(0);
            foreach (rcData rc in task.redCells)
            {
                DrawContour(dst2[rc.rect], rc.contour, vecToScalar(rc.color), -1);
                if (rc.index == task.rc.index) DrawContour(dst2[rc.rect], rc.contour, Scalar.White, -1);
            }
            strOut = stats.strOut;
            SetTrueText(strOut, 3);
        }
    }
    public class CS_RedTrack_Lines : CS_Parent
    {
        Line_Basics lines = new Line_Basics();
        RedTrack_Basics track = new RedTrack_Basics();
        public CS_RedTrack_Lines(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            desc = "Identify and track the lines in an image as RedCloud Cells";
        }
        public void RunCS(Mat src)
        {
            lines.Run(src);
            if (task.heartBeat || task.motionFlag) dst3.SetTo(0);
            int index = 0;
            foreach (var lp in lines.lpList)
            {
                DrawLine(dst3, lp.p1, lp.p2, 255);
                index++;
                if (index > 10) break;
            }
            track.Run(dst3.Clone());
            dst0 = track.redC.dst0;
            dst1 = track.redC.dst1;
            dst2 = track.dst2;
        }
    }
    public class CS_RedTrack_LineSingle : CS_Parent
    {
        RedTrack_Basics track = new RedTrack_Basics();
        int leftMost, rightmost;
        cv.Point leftCenter, rightCenter;
        public CS_RedTrack_LineSingle(VBtask task) : base(task)
        {
            desc = "Create a line between the rightmost and leftmost good feature to show camera motion";
        }
        int findNearest(cv.Point pt)
        {
            float bestDistance = float.MaxValue;
            int bestIndex = 0;
            foreach (var rc in task.redCells)
            {
                float d = (float)pt.DistanceTo(rc.maxDist);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    bestIndex = rc.index;
                }
            }
            return bestIndex;
        }
        public void RunCS(Mat src)
        {
            track.Run(src);
            dst0 = track.redC.dst0;
            dst1 = track.redC.dst1;
            dst2 = track.dst2;
            if (task.redCells.Count() == 0)
            {
                SetTrueText("No lines found to track.", 3);
                return;
            }
            var xList = new SortedList<int, int>(new CompareAllowIdenticalIntegerInverted());
            foreach (var rc in task.redCells)
            {
                if (rc.index == 0) continue;
                xList.Add(rc.rect.X, rc.index);
            }
            int minLeft = xList.Count() / 4;
            int minRight = (xList.Count() - minLeft);
            if (leftMost == 0 || rightmost == 0 || leftMost == rightmost)
            {
                leftCenter = rightCenter; // force iteration...
                int iterations = 0;
                while (leftCenter.DistanceTo(rightCenter) < dst2.Width / 4)
                {
                    leftMost = msRNG.Next(minLeft, minRight);
                    rightmost = msRNG.Next(minLeft, minRight);
                    leftCenter = task.redCells[leftMost].maxDist;
                    rightCenter = task.redCells[rightmost].maxDist;
                    iterations++;
                    if (iterations > 10) return;
                }
            }
            leftMost = findNearest(leftCenter);
            leftCenter = task.redCells[leftMost].maxDist;
            rightmost = findNearest(rightCenter);
            rightCenter = task.redCells[rightmost].maxDist;
            DrawLine(dst2, leftCenter, rightCenter, Scalar.White);
            labels[2] = track.redC.labels[2];
        }
    }
    public class CS_RedTrack_FeaturesKNN : CS_Parent
    {
        public KNN_Core knn = new KNN_Core();
        public Feature_Basics feat = new Feature_Basics();
        public CS_RedTrack_FeaturesKNN(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Output of Feature_Basics", "Grid of points to measure motion." };
            desc = "Use KNN with the good features in the image to create a grid of points";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            dst2 = feat.dst2;
            knn.queries = new List<cv.Point2f>(task.features);
            knn.Run(empty);
            dst3 = src.Clone();
            for (int i = 0; i < knn.neighbors.Count(); i++)
            {
                Point2f p1 = knn.queries[i];
                int index = knn.neighbors[i][knn.neighbors[i].Count() - 1];
                Point2f p2 = knn.trainInput[index];
                DrawCircle(dst3, p1, task.DotSize, Scalar.Yellow);
                DrawCircle(dst3, p2, task.DotSize, Scalar.Yellow);
                DrawLine(dst3, p1, p2, Scalar.White);
            }
            knn.trainInput = new List<cv.Point2f>(knn.queries);
        }
    }
    public class CS_RedTrack_GoodCell : CS_Parent
    {
        RedTrack_GoodCellInput good = new RedTrack_GoodCellInput();
        RedCloud_Hulls hulls = new RedCloud_Hulls();
        public CS_RedTrack_GoodCell(VBtask task) : base(task)
        {
            FindSlider("Feature Sample Size").Value = 100;
            desc = "Track the cells that have good features";
        }
        public void RunCS(Mat src)
        {
            hulls.Run(src);
            dst2 = hulls.dst2;
            good.Run(src);
            dst3.SetTo(0);
            foreach (var pt in good.featureList)
            {
                DrawCircle(dst3, pt, task.DotSize, Scalar.White);
            }
        }
    }
    public class CS_RedTrack_GoodCells : CS_Parent
    {
        RedTrack_GoodCellInput good = new RedTrack_GoodCellInput();
        RedCloud_Hulls hulls = new RedCloud_Hulls();
        public CS_RedTrack_GoodCells(VBtask task) : base(task)
        {
            desc = "Track the cells that have good features";
        }
        public void RunCS(Mat src)
        {
            hulls.Run(src);
            dst2 = hulls.dst2.Clone();
            good.Run(src);
            dst3.SetTo(0);
            dst0 = src;
            var trackCells = new List<rcData>();
            var trackIndex = new List<int>();
            foreach (var pt in good.featureList)
            {
                int index = task.cellMap.Get<byte>((int)pt.Y, (int)pt.X);
                if (!trackIndex.Contains(index))
                {
                    var rc = task.redCells[index];
                    if (rc.hull == null) continue;
                    DrawContour(dst2[rc.rect], rc.hull, Scalar.White, -1);
                    trackIndex.Add(index);
                    DrawCircle(dst0, pt, task.DotSize, task.HighlightColor);
                    DrawCircle(dst3, pt, task.DotSize, Scalar.White);
                    trackCells.Add(rc);
                }
            }
            labels[3] = "There were " + trackCells.Count() + " cells that could be tracked.";
        }
    }
    public class CS_RedTrack_GoodCellInput : CS_Parent
    {
        public KNN_Core knn = new KNN_Core();
        public Feature_Basics feat = new Feature_Basics();
        public List<cv.Point2f> featureList = new List<cv.Point2f>();
        Options_RedTrack options = new Options_RedTrack();
        public CS_RedTrack_GoodCellInput(VBtask task) : base(task)
        {
            desc = "Use KNN to find good features to track";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            feat.Run(src);
            dst2 = feat.dst2;
            knn.queries = new List<cv.Point2f>(task.features);
            knn.Run(empty);
            featureList.Clear();
            for (int i = 0; i < knn.neighbors.Count(); i++)
            {
                var p1 = knn.queries[i];
                var index = knn.neighbors[i][0]; // find nearest
                var p2 = knn.trainInput[index];
                if (p1.DistanceTo(p2) < options.maxDistance) featureList.Add(p1);
            }
            knn.trainInput = new List<cv.Point2f>(knn.queries);
        }
    }
    public class CS_RedTrack_Points : CS_Parent
    {
        Line_Basics lines = new Line_Basics();
        RedTrack_Basics track = new RedTrack_Basics();
        public CS_RedTrack_Points(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            labels = new string[] { "", "", "RedCloudX_Track output", "Input to RedCloudX_Track" };
            desc = "Identify and track the end points of lines in an image of RedCloud Cells";
        }
        public void RunCS(Mat src)
        {
            lines.Run(src);
            dst3.SetTo(0);
            int index = 0;
            foreach (var lp in lines.lpList)
            {
                DrawCircle(dst3, lp.p1, task.DotSize, 255);
                DrawCircle(dst3, lp.p2, task.DotSize, 255);
                index++;
                if (index >= 10) break;
            }
            track.Run(dst3);
            dst0 = track.redC.dst0;
            dst1 = track.redC.dst1;
            dst2 = track.dst2;
        }
    }
    public class CS_RedTrack_Features : CS_Parent
    {
        Options_Flood options = new Options_Flood();
        Feature_Basics feat = new Feature_Basics();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_RedTrack_Features(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            labels = new string[] { "", "", "Output of Feature_Basics - input to RedCloud",
                                "Value Is correlation of x to y in contour points (0 indicates circular.)" };
            desc = "Similar to RedTrack_KNNPoints";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            if (task.heartBeat) dst2.SetTo(0);
            foreach (var pt in task.features)
            {
                DrawCircle(dst2, pt, task.DotSize, 255);
            }
            redC.Run(dst2);
            dst3.SetTo(0);
            foreach (var rc in task.redCells)
            {
                if (rc.rect.X == 0 && rc.rect.Y == 0) continue;
                DrawContour(dst3[rc.rect], rc.contour, vecToScalar(rc.color), -1);
                if (rc.contour.Count() > 0) SetTrueText(shapeCorrelation(rc.contour).ToString(fmt3), new cv.Point(rc.rect.X, rc.rect.Y), 3);
            }
            SetTrueText("Move camera to see the value of this algorithm", 2);
            SetTrueText("Values are correlation of x to y.  Leans left (negative) or right (positive) or circular (neutral correlation.)", 3);
        }
    }
    public class CS_Reduction_Basics : CS_Parent
    {
        public int classCount;
        public CS_Reduction_Basics(VBtask task) : base(task)
        {
            task.redOptions.enableReductionTypeGroup(true);
            task.redOptions.enableReductionSliders(true);
            desc = "Reduction: a simpler way to KMeans by reducing color resolution";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1) 
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (task.redOptions.reductionType == "Use Bitwise Reduction")
            {
                var bits = task.redOptions.getBitReductionBar();
                classCount = (int)(255 / Math.Pow(2, bits));
                var zeroBits = Math.Pow(2, bits) - 1;
                dst2 = src & new Mat(src.Size(), src.Type(), Scalar.All(255 - zeroBits));
                dst2 = dst2 / zeroBits;
            }
            else if (task.redOptions.reductionType == "Use Simple Reduction")
            {
                var reductionVal = task.redOptions.getSimpleReductionBar();
                classCount = (int)Math.Ceiling((double)(255 / reductionVal));
                dst2 = src / reductionVal;
                labels[2] = "Reduced image - factor = " + task.redOptions.getSimpleReductionBar().ToString();
            }
            else
            {
                dst2 = src;
                labels[2] = "No reduction requested";
            }
            dst3 = ShowPalette(dst2 * 255 / classCount);
            labels[2] = classCount.ToString() + " colors after reduction";
        }
    }
    public class CS_Reduction_Floodfill : CS_Parent
    {
        public Reduction_Basics reduction = new Reduction_Basics();
        public RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Reduction_Floodfill(VBtask task) : base(task)
        {
            task.redOptions.setIdentifyCells(true);
            task.redOptions.setUseColorOnly(true);
            labels[2] = "Reduced input to floodfill";
            task.redOptions.setBitReductionBar(32);
            desc = "Use the reduction output as input to floodfill to get masks of cells.";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            dst2 = ShowPalette(reduction.dst2 * 255 / reduction.classCount);
            redC.Run(reduction.dst2);
            dst3 = redC.dst2;
            labels[3] = redC.labels[3];
        }
    }
    public class CS_Reduction_HeatMapLines : CS_Parent
    {
        HeatMap_Basics heat = new HeatMap_Basics();
        public Line_Basics lines = new Line_Basics();
        public PointCloud_SetupSide setupSide = new PointCloud_SetupSide();
        public PointCloud_SetupTop setupTop = new PointCloud_SetupTop();
        Reduction_PointCloud reduction = new Reduction_PointCloud();
        public CS_Reduction_HeatMapLines(VBtask task) : base(task)
        {
            labels[2] = "Gravity rotated Side View with detected lines";
            labels[3] = "Gravity rotated Top View width detected lines";
            desc = "Present both the top and side view to minimize pixel counts.";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            heat.Run(src);
            lines.Run(heat.dst2);
            setupTop.Run(heat.dst2);
            dst2 = setupTop.dst2;
            dst2.SetTo(Scalar.White, lines.dst3);
            lines.Run(heat.dst3);
            setupSide.Run(heat.dst3);
            dst3 = setupSide.dst2;
            dst3.SetTo(Scalar.White, lines.dst3);
        }
    }
    public class CS_Reduction_PointCloud : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        public CS_Reduction_PointCloud(VBtask task) : base(task)
        {
            task.redOptions.checkSimpleReduction(true);;
            task.redOptions.setBitReductionBar(20);
            labels = new string[] { "", "", "8-bit reduced depth", "Palettized output of the different depth levels found" };
            desc = "Use reduction to smooth depth data";
        }
        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32FC3) src = task.pcSplit[2];
            src *= 255 / task.MaxZmeters;
            src.ConvertTo(dst0, MatType.CV_32S);
            reduction.Run(dst0);
            reduction.dst2.ConvertTo(dst2, MatType.CV_32F);
            dst2.ConvertTo(dst2, MatType.CV_8U);
            dst3 = ShowPalette(dst2 * 255 / reduction.classCount);
        }
    }
    public class CS_Reduction_XYZ : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        Options_Reduction options = new Options_Reduction(); 
        public CS_Reduction_XYZ(VBtask task) : base(task)
        {
            task.redOptions.setSimpleReductionBarMax(1000);
            task.redOptions.setBitReductionBar(400);
            desc = "Use reduction to slice the point cloud in 3 dimensions";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (src.Type() != MatType.CV_32FC3) src = task.pointCloud;
            Mat[] split = src.Split();
            for (int i = 0; i < split.Length; i++)
            {
                if (options.reduceXYZ[i])
                {
                    split[i] *= 1000;
                    split[i].ConvertTo(dst0, MatType.CV_32S);
                    reduction.Run(dst0);
                    mmData mm = GetMinMax(reduction.dst2);
                    reduction.dst2.ConvertTo(split[i], MatType.CV_32F);
                }
            }
            Cv2.Merge(split, dst3);
            dst3.SetTo(0, task.noDepthMask);
            SetTrueText("Task.PointCloud (or 32fc3 input) has been reduced and is in dst3");
        }
    }
    public class CS_Reduction_Edges : CS_Parent
    {
        Edge_Laplacian edges = new Edge_Laplacian();
        Reduction_Basics reduction = new Reduction_Basics();
        public CS_Reduction_Edges(VBtask task) : base(task)
        {
            task.redOptions.checkSimpleReduction(true);;
            desc = "Get the edges after reducing the image.";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            dst2 = reduction.dst2 * 255 / reduction.classCount;
            bool reductionRequested = true;
            if (task.redOptions.reductionType == "No Reduction") reductionRequested = false;
            labels[2] = reductionRequested ? "Reduced image" : "Original image";
            labels[3] = reductionRequested ? "Laplacian edges of reduced image" : "Laplacian edges of original image";
            edges.Run(dst2);
            dst3 = edges.dst2;
        }
    }
    public class CS_Reduction_Histogram : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        Plot_Histogram plot = new Plot_Histogram();
        public CS_Reduction_Histogram(VBtask task) : base(task)
        {
            plot.createHistogram = true;
            plot.removeZeroEntry = false;
            labels = new string[] { "", "", "Reduction image", "Histogram of the reduction" };
            desc = "Visualize a reduction with a histogram";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            dst2 = reduction.dst2 * 255 / reduction.classCount;
            plot.Run(dst2);
            dst3 = plot.dst2;
            labels[2] = "ClassCount = " + reduction.classCount.ToString();
        }
    }
    public class CS_Reduction_BGR : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        Mat_4Click mats = new Mat_4Click();
        public CS_Reduction_BGR(VBtask task) : base(task)
        {
            desc = "Reduce BGR image in parallel";
        }
        public void RunCS(Mat src)
        {
            Mat[] split = src.Split();
            for (int i = 0; i <= 2; i++)
            {
                reduction.Run(split[i]);
                if (standaloneTest()) mats.mat[i] = ShowPalette(reduction.dst2 * 255 / reduction.classCount);
                split[0] = reduction.dst2.Clone();
            }
            if (standaloneTest())
            {
                mats.mat[3] = (mats.mat[0] + mats.mat[1] + mats.mat[2]);
                mats.Run(empty);
                dst3 = mats.dst2;
            }
            Cv2.Merge(split, dst2);
        }
    }
    public class CS_Remap_Basics : CS_Parent
    {
        public int direction = 3; // default to remap horizontally and vertically
        Mat mapx1, mapx2, mapx3;
        Mat mapy1, mapy2, mapy3;
        public CS_Remap_Basics(VBtask task) : base(task)
        {
            mapx1 = new Mat(dst2.Size(), MatType.CV_32F);
            mapy1 = new Mat(dst2.Size(), MatType.CV_32F);
            mapx2 = new Mat(dst2.Size(), MatType.CV_32F);
            mapy2 = new Mat(dst2.Size(), MatType.CV_32F);
            mapx3 = new Mat(dst2.Size(), MatType.CV_32F);
            mapy3 = new Mat(dst2.Size(), MatType.CV_32F);
            for (int j = 0; j < mapx1.Rows; j++)
            {
                for (int i = 0; i < mapx1.Cols; i++)
                {
                    mapx1.Set<float>(j, i, i);
                    mapy1.Set<float>(j, i, dst2.Rows - j);
                    mapx2.Set<float>(j, i, dst2.Cols - i);
                    mapy2.Set<float>(j, i, j);
                    mapx3.Set<float>(j, i, dst2.Cols - i);
                    mapy3.Set<float>(j, i, dst2.Rows - j);
                }
            }
            desc = "Use remap to reflect an image in 4 directions.";
        }
        public void RunCS(Mat src)
        {
            labels[2] = new[] { "CS_Remap_Basics - original", "Remap vertically", "Remap horizontally", "Remap horizontally and vertically" }[direction];
            switch (direction)
            {
                case 0:
                    dst2 = src;
                    break;
                case 1:
                    Cv2.Remap(src, dst2, mapx1, mapy1, InterpolationFlags.Nearest);
                    break;
                case 2:
                    Cv2.Remap(src, dst2, mapx2, mapy2, InterpolationFlags.Nearest);
                    break;
                case 3:
                    Cv2.Remap(src, dst2, mapx3, mapy3, InterpolationFlags.Nearest);
                    break;
            }
            if (task.heartBeat)
            {
                direction += 1;
                direction %= 4;
            }
        }
    }
    public class CS_Remap_Flip : CS_Parent
    {
        public int direction = 0;
        public CS_Remap_Flip(VBtask task) : base(task)
        {
            desc = "Use flip to remap an image.";
        }
        public void RunCS(Mat src)
        {
            labels[2] = new[] { "CS_Remap_Flip - original", "CS_Remap_Flip - flip horizontal", "CS_Remap_Flip - flip vertical", "CS_Remap_Flip - flip horizontal and vertical" }[direction];
            switch (direction)
            {
                case 0: // do nothing!
                    src.CopyTo(dst2);
                    break;
                case 1: // flip vertically
                    Cv2.Flip(src, dst2, FlipMode.Y);
                    break;
                case 2: // flip horizontally
                    Cv2.Flip(src, dst2, FlipMode.X);
                    break;
                case 3: // flip horizontally and vertically
                    Cv2.Flip(src, dst2, FlipMode.XY);
                    break;
            }
            if (task.heartBeat)
            {
                direction += 1;
                direction %= 4;
            }
        }
    }
    public class CS_Flip_Basics : CS_Parent
    {
        Remap_Flip flip = new Remap_Flip();
        public CS_Flip_Basics(VBtask task) : base(task)
        {
            desc = "Placeholder to make it easy to remember 'Remap'.";
        }
        public void RunCS(Mat src)
        {
            flip.RunVB(src);
            dst2 = flip.dst2;
            labels = flip.labels;
        }
    }
    public class CS_Resize_Basics : CS_Parent
    {
        public cv.Size newSize;
        public Options_Resize options = new Options_Resize();
        public CS_Resize_Basics(VBtask task) : base(task)
        {
            if (standaloneTest())
                task.drawRect = new cv.Rect(dst2.Width / 4, dst2.Height / 4, dst2.Width / 2, dst2.Height / 2);
            desc = "Resize with different options and compare them";
            labels[2] = "Rectangle highlight above resized";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.drawRect.Width != 0)
            {
                src = src[task.drawRect];
                newSize = task.drawRect.Size;
            }
            dst2 = src.Resize(newSize, 0, 0, options.warpFlag);
        }
    }
    public class CS_Resize_Smaller : CS_Parent
    {
        public Options_Resize options = new Options_Resize();
        public cv.Size newSize;
        public CS_Resize_Smaller(VBtask task) : base(task)
        {
            desc = "Resize by a percentage of the image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            newSize = new cv.Size((int)Math.Ceiling(src.Width * options.resizePercent), (int)Math.Ceiling(src.Height * options.resizePercent));
            dst2 = src.Resize(newSize, 0, 0, options.warpFlag);
            labels[2] = "Image after resizing to: " + newSize.Width + "X" + newSize.Height;
        }
    }
    public class CS_Resize_Preserve : CS_Parent
    {
        public Options_Resize options = new Options_Resize();
        public cv.Size newSize;
        public CS_Resize_Preserve(VBtask task) : base(task)
        {
            FindSlider("Resize Percentage (%)").Maximum = 200;
            FindSlider("Resize Percentage (%)").Value = 120;
            FindSlider("Resize Percentage (%)").Minimum = 100;
            desc = "Decrease the size but preserve the full image size.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            newSize = new cv.Size((int)Math.Ceiling(src.Width * options.resizePercent), (int)Math.Ceiling(src.Height * options.resizePercent));
            dst0 = src.Resize(newSize, 0, 0, InterpolationFlags.Nearest).SetTo(0);
            var rect = new cv.Rect(options.topLeftOffset, options.topLeftOffset, dst2.Width, dst2.Height);
            src.CopyTo(dst0[rect]);
            dst2 = dst0.Resize(dst2.Size(), 0, 0, options.warpFlag);
            labels[2] = "Image after resizing to: " + newSize.Width + "X" + newSize.Height;
        }
    }
    public class CS_Resize_Proportional : CS_Parent
    {
        Options_Spectrum options = new Options_Spectrum();
        public CS_Resize_Proportional(VBtask task) : base(task)
        {
            desc = "Resize the input but keep the results proportional to the original.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                options.RunVB();
                dst2 = options.runRedCloud(ref labels[2]);
                src = src[task.rc.rect];
                Cv2.ImShow("src", src);
            }
            cv.Size newSize;
            if (dst0.Width / (double)dst0.Height < src.Width / (double)src.Height)
            {
                newSize = new cv.Size(dst2.Width, dst2.Height * dst0.Height / dst0.Width);
            }
            else
            {
                newSize = new cv.Size(dst2.Width * dst0.Height / dst0.Width, dst2.Height);
            }
            src = src.Resize(newSize, 0, 0, InterpolationFlags.Nearest);
            var newRect = new cv.Rect(0, 0, newSize.Width, newSize.Height);
            dst3.SetTo(0);
            src.CopyTo(dst3[newRect]);
        }
    }
    public class CS_Retina_Basics_CPP : CS_Parent
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        byte[] magnoData = new byte[1];
        byte[] dataSrc = new byte[1];
        float samplingFactor = -1; // force open
        Options_Retina options = new Options_Retina();
        bool saveUseLogSampling;
        public CS_Retina_Basics_CPP(VBtask task) : base(task)
        {
            labels[2] = "Retina Parvo";
            labels[3] = "Retina Magno";
            desc = "Use the bio-inspired retina algorithm to adjust color and monitor motion.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.xmlCheck)
            {
                var fileinfo = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "RetinaDefaultParameters.xml"));
                if (fileinfo.Exists)
                {
                    File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "RetinaDefaultParameters.xml"), Path.Combine(task.HomeDir, "data/RetinaDefaultParameters.xml"), true);
                    startInfo.FileName = "wordpad.exe";
                    startInfo.Arguments = Path.Combine(task.HomeDir, "Data/RetinaDefaultParameters.xml");
                    Process.Start(startInfo);
                }
                else
                {
                    MessageBox.Show("RetinaDefaultParameters.xml should have been created but was not found.  OpenCV error?");
                }
            }
            if (saveUseLogSampling != options.useLogSampling || samplingFactor != options.sampleCount)
            {
                if (cPtr != (IntPtr) 0) Retina_Basics_Close(cPtr);
                Array.Resize(ref magnoData, (int)(src.Total() - 1));
                Array.Resize(ref dataSrc, (int)(src.Total() * src.ElemSize() - 1));
                saveUseLogSampling = options.useLogSampling;
                samplingFactor = options.sampleCount;
                if (!task.testAllRunning) cPtr = Retina_Basics_Open(src.Rows, src.Cols, options.useLogSampling, samplingFactor);
            }
            GCHandle handleMagno = GCHandle.Alloc(magnoData, GCHandleType.Pinned);
            GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            IntPtr imagePtr = IntPtr.Zero;
            if (!task.testAllRunning)
            {
                Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
                int logSampling = options.useLogSampling ? 1 : 0;   
                imagePtr = Retina_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                             handleMagno.AddrOfPinnedObject(), logSampling); 
            }
            else
            {
                SetTrueText("CS_Retina_Basics_CPP runs fine but during 'Test All' it is not run because it can oversubscribe OpenCL memory.");
                dst3 = new Mat(dst2.Size(), MatType.CV_8UC1, 0);
            }
            handleSrc.Free();
            handleMagno.Free();
            if (imagePtr != IntPtr.Zero)
            {
                float nextFactor = samplingFactor;
                if (!options.useLogSampling) nextFactor = 1;
                dst2 = new Mat(src.Rows / (int)nextFactor, src.Cols / (int)nextFactor, MatType.CV_8UC3, imagePtr).Resize(src.Size()).Clone();
                dst3 = new Mat(src.Rows / (int)nextFactor, src.Cols / (int)nextFactor, MatType.CV_8U, magnoData).Resize(src.Size());
            }
        }
        public void Close()
        {
            if (cPtr != (IntPtr) 0) cPtr = Retina_Basics_Close(cPtr);
        }
    }
    public class CS_Retina_Depth : CS_Parent
    {
        Retina_Basics_CPP retina = new Retina_Basics_CPP();
        Mat lastMotion = new Mat();
        public CS_Retina_Depth(VBtask task) : base(task)
        {
            desc = "Use the bio-inspired retina algorithm with the depth data.";
            labels[2] = "Last result || current result";
            labels[3] = "Current depth motion result";
        }
        public void RunCS(Mat src)
        {
            retina.Run(task.depthRGB);
            dst3 = retina.dst3;
            if (lastMotion.Width == 0) lastMotion = retina.dst3;
            dst2 = lastMotion | retina.dst3;
            lastMotion = retina.dst3;
        }
    }
    public class CS_ROI_Basics : CS_Parent
    {
        public Diff_Basics diff = new Diff_Basics();
        public cv.Rect aoiRect;
        public CS_ROI_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Enclosing rectangle of all pixels that have changed", "" };
            dst1 = new Mat(dst2.Size(), MatType.CV_8UC1, 0);
            task.gOptions.pixelDiffThreshold = 30;
            desc = "Find the motion ROI in the latest image.";
        }
        public void RunCS(Mat src)
        {
            diff.Run(src);
            dst2 = diff.dst2;
            var split = diff.dst2.FindNonZero().Split();
            if (split.Length == 0) return;
            var mm0 = GetMinMax(split[0]);
            var mm1 = GetMinMax(split[1]);
            aoiRect = new cv.Rect((int)mm0.minVal, (int)mm1.minVal, (int)(mm0.maxVal - mm0.minVal), (int)(mm1.maxVal - mm1.minVal));
            if (aoiRect.Width > 0 && aoiRect.Height > 0)
            {
                task.color.Rectangle(aoiRect, Scalar.Yellow, task.lineWidth);
                dst2.Rectangle(aoiRect, Scalar.White, task.lineWidth);
            }
        }
    }
    public class CS_ROI_FindNonZeroNoSingle : CS_Parent
    {
        public Diff_Basics diff = new Diff_Basics();
        public cv.Rect aoiRect;
        public CS_ROI_FindNonZeroNoSingle(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Enclosing rectangle of all changed pixels (after removing single pixels)", "" };
            dst1 = new Mat(dst2.Size(), MatType.CV_8UC1, 0);
            task.gOptions.pixelDiffThreshold = 30;
            desc = "Find the motion ROI in just the latest image - eliminate single pixels";
        }
        public void RunCS(Mat src)
        {
            diff.Run(src);
            dst2 = diff.dst2;
            var tmp = diff.dst2.FindNonZero();
            if (tmp.Rows == 0) return;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (int i = 0; i < tmp.Rows; i++)
            {
                var pt = tmp.Get<cv.Point>(i, 0);
                // eliminate single pixel differences.
                var r = new cv.Rect(pt.X - 1, pt.Y - 1, 3, 3);
                if (r.X < 0) r.X = 0;
                if (r.Y < 0) r.Y = 0;
                if (r.X + r.Width < dst2.Width && r.Y + r.Height < dst2.Height)
                {
                    if (dst2[r].CountNonZero() > 1)
                    {
                        if (minX > pt.X) minX = pt.X;
                        if (maxX < pt.X) maxX = pt.X;
                        if (minY > pt.Y) minY = pt.Y;
                        if (maxY < pt.Y) maxY = pt.Y;
                    }
                }
            }
            if (minX != int.MaxValue)
            {
                aoiRect = new cv.Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
                task.color.Rectangle(aoiRect, Scalar.Yellow, task.lineWidth);
                dst2.Rectangle(aoiRect, Scalar.White, task.lineWidth);
            }
        }
    }
    public class CS_ROI_AccumulateOld : CS_Parent
    {
        public Diff_Basics diff = new Diff_Basics();
        public cv.Rect aoiRect;
        public int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        Options_ROI options = new Options_ROI();
        public CS_ROI_AccumulateOld(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "", "Area of Interest", "" };
            dst1 = new Mat(dst2.Size(), MatType.CV_8UC1, 0);
            task.gOptions.pixelDiffThreshold = 30;
            desc = "Accumulate pixels in a motion ROI - all pixels that are different by X";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (aoiRect.Width * aoiRect.Height > src.Total() * options.roiPercent || task.optionsChanged)
            {
                dst0 = task.color;
                dst1.SetTo(0);
                aoiRect = new cv.Rect();
                minX = int.MaxValue;
                maxX = int.MinValue;
                minY = int.MaxValue;
                maxY = int.MinValue;
            }
            diff.Run(src);
            dst3 = diff.dst2;
            Cv2.BitwiseOr(dst3, dst1, dst1);
            var tmp = dst3.FindNonZero();
            if (aoiRect != new cv.Rect())
            {
                task.color[aoiRect].CopyTo(dst0[aoiRect]);
                dst0.Rectangle(aoiRect, Scalar.Yellow, task.lineWidth);
                dst2.Rectangle(aoiRect, Scalar.White, task.lineWidth);
            }
            if (tmp.Rows == 0) return;
            for (int i = 0; i < tmp.Rows; i++)
            {
                var pt = tmp.Get<cv.Point>(i, 0);
                if (minX > pt.X) minX = pt.X;
                if (maxX < pt.X) maxX = pt.X;
                if (minY > pt.Y) minY = pt.Y;
                if (maxY < pt.Y) maxY = pt.Y;
            }
            aoiRect = new cv.Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            dst1.CopyTo(dst2);
            dst2.Rectangle(aoiRect, Scalar.White, task.lineWidth);
        }
    }
    public class CS_ROI_Accumulate : CS_Parent
    {
        public Diff_Basics diff = new Diff_Basics();
        cv.Rect roiRect;
        Options_ROI options = new Options_ROI();
        public CS_ROI_Accumulate(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Area of Interest", "" };
            dst2 = new Mat(dst2.Size(), MatType.CV_8UC1, 0);
            task.gOptions.pixelDiffThreshold = 30;
            desc = "Accumulate pixels in a motion ROI until the size is x% of the total image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            SetTrueText(traceName + " is the same as CS_ROI_AccumulateOld but simpler.", 3);
            if (roiRect.Width * roiRect.Height > src.Total() * options.roiPercent || task.optionsChanged)
            {
                dst2.SetTo(0);
                roiRect = new cv.Rect();
            }
            diff.Run(src);
            var split = diff.dst2.FindNonZero().Split();
            if (split.Length > 0)
            {
                var mm0 = GetMinMax(split[0]);
                var mm1 = GetMinMax(split[1]);
                var motionRect = new cv.Rect((int)mm0.minVal, (int)mm1.minVal, (int)(mm0.maxVal - mm0.minVal),
                                             (int)(mm1.maxVal - mm1.minVal));
                if (motionRect.Width != 0 && motionRect.Height != 0)
                {
                    if (roiRect.X > 0 || roiRect.Y > 0) roiRect = motionRect.Union(roiRect);
                    else roiRect = motionRect;
                    Cv2.BitwiseOr(diff.dst2, dst2, dst2);
                }
            }
            dst2.Rectangle(roiRect, Scalar.White, task.lineWidth);
            task.color.Rectangle(roiRect, task.HighlightColor, task.lineWidth);
        }
    }
    public class CS_Rotate_Basics : CS_Parent
    {
        public Mat M;
        public Mat Mflip;
        public Options_Resize options = new Options_Resize();
        public float rotateAngle = 1000;
        public Point2f rotateCenter;
        Options_Rotate optionsRotate = new Options_Rotate();
        public CS_Rotate_Basics(VBtask task) : base(task)
        {
            rotateCenter = new Point2f(dst2.Width / 2, dst2.Height / 2);
            desc = "Rotate a rectangle by a specified angle";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            optionsRotate.RunVB();
            rotateAngle = optionsRotate.rotateAngle;
            M = Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1);
            dst2 = src.WarpAffine(M, src.Size(), options.warpFlag);
            if (options.warpFlag == InterpolationFlags.WarpInverseMap)
            {
                Mflip = Cv2.GetRotationMatrix2D(rotateCenter, rotateAngle, 1);
            }
        }
    }

    public class CS_Rotate_BasicsQT : CS_Parent
    {
        public float rotateAngle = 24;
        public Point2f rotateCenter;
        public CS_Rotate_BasicsQT(VBtask task) : base(task)
        {
            rotateCenter = new Point2f(dst2.Width / 2, dst2.Height / 2);
            desc = "Rotate a rectangle by a specified angle";
        }
        public void RunCS(Mat src)
        {
            var M = Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1);
            dst2 = src.WarpAffine(M, src.Size(), InterpolationFlags.Nearest);
        }
    }
    public class CS_Rotate_Box : CS_Parent
    {
        readonly Rotate_Basics rotation = new Rotate_Basics();
        public CS_Rotate_Box(VBtask task) : base(task)
        {
            task.drawRect = new cv.Rect(100, 100, 100, 100);
            labels[2] = "Original Rectangle in the original perspective";
            labels[3] = "Same Rectangle in the new warped perspective";
            desc = "Track a rectangle no matter how the perspective is warped.  Draw a rectangle anywhere.";
        }
        public void RunCS(Mat src)
        {
            rotation.Run(src);
            dst3 = dst2.Clone();
            var r = task.drawRect;
            dst2 = src.Clone();
            dst2.Rectangle(r, Scalar.White, 1);
            var center = new Point2f(r.X + r.Width / 2, r.Y + r.Height / 2);
            var drawBox = new RotatedRect(center, new Size2f(r.Width, r.Height), 0);
            var boxPoints = Cv2.BoxPoints(drawBox);
            var srcPoints = new Mat(1, 4, MatType.CV_32FC2, boxPoints);
            var dstpoints = new Mat();
            if (rotation.options.warpFlag != InterpolationFlags.WarpInverseMap)
            {
                Cv2.Transform(srcPoints, dstpoints, rotation.M);
            }
            else
            {
                Cv2.Transform(srcPoints, dstpoints, rotation.Mflip);
            }
            for (int i = 0; i < dstpoints.Width - 1; i++)
            {
                var p1 = dstpoints.Get<cv.Point2f>(0, i);
                var p2 = dstpoints.Get<cv.Point2f>(0, (i + 1) % 4);
                DrawLine(dst3, p1, p2, Scalar.White, task.lineWidth + 1);
            }
        }
    }
    public class CS_Rotate_Poly : CS_Parent
    {
        Options_FPoly optionsFPoly = new Options_FPoly();
        public Options_RotatePoly options = new Options_RotatePoly();
        public Rotate_PolyQT rotateQT = new Rotate_PolyQT();
        List<cv.Point2f> rPoly = new List<cv.Point2f>();
        public CS_Rotate_Poly(VBtask task) : base(task)
        {
            labels = new[] { "", "", "Triangle before rotation", "Triangle after rotation" };
            desc = "Rotate a triangle around a center of rotation";
        }
        public void RunCS(Mat src)
        {
            optionsFPoly.RunVB();
            if (options.changeCheck.Checked || task.FirstPass)
            {
                rPoly.Clear();
                for (int i = 0; i < task.polyCount; i++)
                {
                    rPoly.Add(new Point2f(msRNG.Next(dst2.Width / 4, dst2.Width * 3 / 4), msRNG.Next(dst2.Height / 4, dst2.Height * 3 / 4)));
                }
                rotateQT.rotateCenter = new Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height));
                options.changeCheck.Checked = false;
            }
            rotateQT.poly = new List<cv.Point2f>(rPoly);
            rotateQT.rotateAngle = options.angleSlider.Value;
            rotateQT.Run(src);
            dst2 = rotateQT.dst3;
            DrawCircle(dst2, rotateQT.rotateCenter, task.DotSize + 2, Scalar.Yellow);
            SetTrueText("center of rotation", rotateQT.rotateCenter);
            labels[3] = rotateQT.labels[3];
        }
    }
    public class CS_Rotate_PolyQT : CS_Parent
    {
        public List<cv.Point2f> poly = new List<cv.Point2f>();
        public Point2f rotateCenter;
        public float rotateAngle;
        public CS_Rotate_PolyQT(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Polygon before rotation", "" };
            desc = "Rotate a triangle around a center of rotation";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                dst2.SetTo(0);
                dst3.SetTo(0);
            }
            if (standaloneTest())
            {
                SetTrueText(traceName + " has no output when run standaloneTest().");
                return;
            }
            DrawFPoly(ref dst2, poly, Scalar.Red);
            labels[3] = "White is the original polygon, yellow has been rotated " + (rotateAngle * 57.2958).ToString() + " degrees";
            // translate so the center of rotation is 0,0
            List<cv.Point2f> translated = new List<cv.Point2f>();
            for (int i = 0; i < poly.Count(); i++)
            {
                Point2f pt = poly[i];
                translated.Add(new Point2f(poly[i].X - rotateCenter.X, poly[i].Y - rotateCenter.Y));
            }
            List<cv.Point2f> rotated = new List<cv.Point2f>();
            for (int i = 0; i < poly.Count(); i++)
            {
                Point2f pt = translated[i];
                float x = pt.X * (float)Math.Cos(rotateAngle) - pt.Y * (float)Math.Sin(rotateAngle);
                float y = pt.Y * (float)Math.Cos(rotateAngle) + pt.X * (float)Math.Sin(rotateAngle);
                rotated.Add(new Point2f(x, y));
            }
            DrawFPoly(ref dst3, poly, Scalar.White);
            poly.Clear();
            foreach (Point2f pt in rotated)
            {
                poly.Add(new Point2f(pt.X + rotateCenter.X, pt.Y + rotateCenter.Y));
            }
            DrawFPoly(ref dst3, poly, Scalar.Yellow);
        }
    }
    public class CS_Rotate_Example : CS_Parent
    {
        Rotate_Basics rotate = new Rotate_Basics();
        public CS_Rotate_Example(VBtask task) : base(task)
        {
            rotate.rotateCenter = new cv.Point(dst2.Height / 2, dst2.Height / 2);
            rotate.rotateAngle = -90;
            desc = "Reminder on how to rotate an image and keep all the pixels.";
        }
        public void RunCS(Mat src)
        {
            cv.Rect r = new cv.Rect(0, 0, src.Height, src.Height);
            dst2[r] = src.Resize(new cv.Size(src.Height, src.Height));
            rotate.Run(dst2);
            dst3[r] = rotate.dst2[new cv.Rect(0, 0, src.Height, src.Height)];
        }
    }
    public class CS_Rotate_Horizon : CS_Parent
    {
        Rotate_Basics rotate = new Rotate_Basics();
        CameraMotion_WithRotation edges = new CameraMotion_WithRotation();
        public CS_Rotate_Horizon(VBtask task) : base(task)
        {
            FindSlider("Rotation Angle in degrees").Value = 3;
            labels[2] = "White is the current horizon vector of the camera.  Highlighted color is the rotated horizon vector.";
            desc = "Rotate the horizon independently from the rotation of the image to validate the Edge_CameraMotion algorithm.";
        }
        Point2f RotatePoint(Point2f point, Point2f center, double angle)
        {
            double radians = angle * (Math.PI / 180.0);
            double sinAngle = Math.Sin(radians);
            double cosAngle = Math.Cos(radians);
            double x = point.X - center.X;
            double y = point.Y - center.Y;
            double xNew = x * cosAngle - y * sinAngle;
            double yNew = x * sinAngle + y * cosAngle;
            xNew += center.X;
            yNew += center.Y;
            return new Point2f((float)xNew, (float)yNew);
        }
        public void RunCS(Mat src)
        {
            rotate.Run(src);
            dst2 = rotate.dst2.Clone();
            dst1 = dst2.Clone();
            PointPair horizonVec = new PointPair(task.horizonVec.p1, task.horizonVec.p2);
            horizonVec.p1 = RotatePoint(task.horizonVec.p1, rotate.rotateCenter, -rotate.rotateAngle);
            horizonVec.p2 = RotatePoint(task.horizonVec.p2, rotate.rotateCenter, -rotate.rotateAngle);
            DrawLine(dst2, horizonVec.p1, horizonVec.p2, task.HighlightColor);
            DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, Scalar.White);
            double y1 = horizonVec.p1.Y - task.horizonVec.p1.Y;
            double y2 = horizonVec.p2.Y - task.horizonVec.p2.Y;
            edges.translateRotateY((int)y1, (int)y2);
            rotate.rotateAngle = edges.rotationY;
            rotate.rotateCenter = new cv.Point(edges.centerY.X, edges.centerY.Y);
            rotate.Run(dst1);
            dst3 = rotate.dst2.Clone();
            strOut = edges.strOut;
        }
    }
    public class CS_Salience_Basics_CPP : CS_Parent
    {
        byte[] grayData = new byte[1];
        public Options_Salience options = new Options_Salience();
        public CS_Salience_Basics_CPP(VBtask task) : base(task)
        {
            cPtr = Salience_Open();
            desc = "Show results of Salience algorithm when using C++";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (src.Total() != grayData.Length) Array.Resize(ref grayData, (int)src.Total());
            GCHandle grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned);
            Marshal.Copy(src.Data, grayData, 0, grayData.Length);
            IntPtr imagePtr = Salience_Run(cPtr, options.numScales, grayHandle.AddrOfPinnedObject(), src.Height, src.Width);
            grayHandle.Free();
            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8U, imagePtr).Clone();
        }
        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = Salience_Close(cPtr);
        }
    }
    public class CS_Salience_Basics_MT : CS_Parent
    {
        Salience_Basics_CPP salience = new Salience_Basics_CPP();
        public CS_Salience_Basics_MT(VBtask task) : base(task)
        {
            FindSlider("Salience numScales").Value = 2;
            desc = "Show results of multi-threaded Salience algorithm when using C++.  NOTE: salience is relative.";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            int threads = 32;
            int h = src.Height / threads;
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            Parallel.For(0, threads, i =>
            {
                cv.Rect roi = new cv.Rect(0, i * h, src.Width, Math.Min(h, src.Height - i * h));
                if (roi.Height <= 0) return;
                IntPtr cPtr = Salience_Open();
                Mat input = src[roi].Clone();
                byte[] grayData = new byte[input.Total()];
                GCHandle grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned);
                Marshal.Copy(input.Data, grayData, 0, grayData.Length);
                IntPtr imagePtr = Salience_Run(cPtr, salience.options.numScales, grayHandle.AddrOfPinnedObject(), roi.Height, roi.Width);
                grayHandle.Free();
                dst2[roi] = new Mat(roi.Height, roi.Width, MatType.CV_8U, imagePtr).Clone();
                if (cPtr != IntPtr.Zero) cPtr = Salience_Close(cPtr);
            });
        }
    }
    public class CS_Sides_Basics : CS_Parent
    {
        public Profile_Basics sides = new Profile_Basics();
        public Contour_RedCloudCorners corners = new Contour_RedCloudCorners();
        public CS_Sides_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "RedCloud output", "Selected Cell showing the various extrema." };
            desc = "Find the 6 extrema and the 4 farthest points in each quadrant for the selected RedCloud cell";
        }
        public void RunCS(Mat src)
        {
            sides.Run(src);
            dst2 = sides.dst2;
            dst3 = sides.dst3;
            var cornersList = sides.corners.ToList();
            for (int i = 0; i < cornersList.Count(); i++)
            {
                var nextColor = sides.cornerColors[i];
                var nextLabel = sides.cornerNames[i];
                DrawLine(dst3, task.rc.maxDist, cornersList[i], Scalar.White);
                SetTrueText(nextLabel, new cv.Point(cornersList[i].X, cornersList[i].Y), 3);
            }
            if (cornersList.Count() > 0)
                SetTrueText(sides.strOut, 3);
            else
                SetTrueText(strOut, 3);
        }
    }
    public class CS_Sides_Profile : CS_Parent
    {
        Contour_SidePoints sides = new Contour_SidePoints();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Sides_Profile(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "RedCloud_Basics Output", "Selected Cell" };
            desc = "Find the 6 corners - left/right, top/bottom, front/back - of a RedCloud cell";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            sides.Run(src);
            dst3 = sides.dst3;
            SetTrueText(sides.strOut, 3);
        }
    }
    public class CS_Sides_Corner : CS_Parent
    {
        Contour_RedCloudCorners sides = new Contour_RedCloudCorners();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Sides_Corner(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "RedCloud_Basics output", "" };
            desc = "Find the 4 points farthest from the center in each quadrant of the selected RedCloud cell";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            sides.Run(src);
            dst3 = sides.dst3;
            SetTrueText("Center point is rcSelect.maxDist", 3);
        }
    }
    public class CS_Sides_ColorC : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Sides_Basics sides = new Sides_Basics();
        public CS_Sides_ColorC(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "RedColor Output", "Cell Extrema" };
            desc = "Find the extrema - top/bottom, left/right, near/far - points for a RedColor Cell";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            sides.Run(src);
            dst3 = sides.dst3;
        }
    }
    public class CS_Sieve_Image : CS_Parent
    {
        Pixel_Zoom zoom = new Pixel_Zoom();
        byte[] numArray;
        Dictionary<int, int> referenceResults = new Dictionary<int, int>
        {
            {10, 4}, {100, 25}, {1000, 168}, {10000, 1229}, {100000, 9592}, {1000000, 78498}, 
            {10000000, 664579}, {100000000, 5761455}
        };
        public CS_Sieve_Image(VBtask task) : base(task)
        {
            numArray = new byte[dst2.Total() - 1];
            labels[2] = "NonZero pixels are primes";
            labels[3] = "Zoom output";
            desc = "Create an image marking primes";
        }
        public void RunCS(Mat src)
        {
            int numCeiling = numArray.Length - 1;
            Array.Resize(ref numArray, numCeiling + 1);
            numArray[0] = 255;
            numArray[1] = 255;
            for (int i = 2; i <= numCeiling / 2 - 1; i++)
            {
                for (int j = i + i; j <= numCeiling; j += i)
                {
                    if (numArray[j] != 255) numArray[j] = 255;
                }
            }
            int countPrimes = 0;
            for (int i = 2; i <= numCeiling; i++)
            {
                if (numArray[i] == 0) countPrimes++;
            }
            if (referenceResults.ContainsKey(numCeiling))
            {
                if (referenceResults[numCeiling] != countPrimes) SetTrueText("Invalid prime count - check this...");
            }
            dst2 = new Mat(dst2.Rows, dst2.Cols, MatType.CV_8U, numArray);
            dst2 = ~dst2;
            zoom.Run(dst2);
            dst3 = zoom.dst2;
        }
    }
    public class CS_SLR_Data : CS_Parent
    {
        Plot_Basics_CPP plot = new Plot_Basics_CPP();
        public List<double> dataX = new List<double>();
        public List<double> dataY = new List<double>();
        public CS_SLR_Data(VBtask task) : base(task)
        {
            using (var sr = new StreamReader(task.HomeDir + "/Data/real_data.txt"))
            {
                string code = sr.ReadToEnd();
                var lines = code.Split('\n');
                foreach (var line in lines)
                {
                    var split = line.Split(' ');
                    if (split.Length > 1)
                    {
                        dataX.Add(double.Parse(split[0]));
                        dataY.Add(double.Parse(split[1]));
                    }
                }
            }
            desc = "Plot the data used in SLR_Basics";
        }
        public void RunCS(Mat src)
        {
            plot.srcX = dataX;
            plot.srcY = dataY;
            plot.Run(src);
            dst2 = plot.dst2;
        }
    }
    public class CS_SLR_SurfaceH : CS_Parent
    {
        PointCloud_SurfaceH surface = new PointCloud_SurfaceH();
        public CS_SLR_SurfaceH(VBtask task) : base(task)
        {
            desc = "Use the PointCloud_SurfaceH data to indicate valleys and peaks.";
        }
        public void RunCS(Mat src)
        {
            surface.Run(src);
            dst2 = surface.dst3;
        }
    }
    public class CS_SLR_Trends : CS_Parent
    {
        public Hist_KalmanAuto hist = new Hist_KalmanAuto();
        List<float> valList = new List<float>();
        float barMidPoint;
        Point2f lastPoint;
        public List<cv.Point2f> resultingPoints = new List<cv.Point2f>();
        public List<float> resultingValues = new List<float>();
        public CS_SLR_Trends(VBtask task) : base(task)
        {
            desc = "Find trends by filling in short histogram gaps in the given image's histogram.";
        }
        public void connectLine(int i, cv.Mat dst)
        {
            float x = barMidPoint + dst.Width * i / valList.Count();
            float y = dst.Height - dst.Height * valList[i] / hist.plot.maxValue;
            Point2f p1 = new Point2f(x, y);
            resultingPoints.Add(p1);
            resultingValues.Add(p1.Y);
            DrawLine(dst, lastPoint, p1, Scalar.Yellow, task.lineWidth + 1);
            lastPoint = p1;
        }
        public void RunCS(Mat src)
        {
            labels[2] = "Grayscale histogram - yellow line shows trend";
            hist.plot.backColor = Scalar.Red;
            hist.Run(src);
            dst2 = hist.dst2;
            var indexer = hist.histogram.GetGenericIndexer<float>();
            valList = new List<float>();
            for (int i = 0; i < hist.histogram.Rows; i++)
            {
                valList.Add(indexer[i]);
            }
            barMidPoint = dst2.Width / valList.Count() / 2;
            if (valList.Count() < 2) return;
            hist.plot.maxValue = valList.Max();
            lastPoint = new Point2f(barMidPoint, dst2.Height - dst2.Height * valList[0] / hist.plot.maxValue);
            resultingPoints.Clear();
            resultingValues.Clear();
            resultingPoints.Add(lastPoint);
            resultingValues.Add(lastPoint.Y);
            for (int i = 1; i < valList.Count() - 1; i++)
            {
                if (valList[i - 1] > valList[i] && valList[i + 1] > valList[i])
                {
                    valList[i] = (valList[i - 1] + valList[i + 1]) / 2;
                }
                connectLine(i, dst2);
            }
            connectLine(valList.Count() - 1, dst2);
        }
    }
    public class CS_SLR_TrendImages : CS_Parent
    {
        SLR_Trends trends = new SLR_Trends();
        Options_SLRImages options = new Options_SLRImages();
        public CS_SLR_TrendImages(VBtask task) : base(task)
        {
            desc = "Find trends by filling in short histogram gaps for depth or 1-channel images";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat[] split = src.Split();
            trends.hist.plot.maxRange = 255;
            trends.hist.plot.removeZeroEntry = false; // default is to look at element 0....
            int splitIndex = 0;
            switch (options.radioText)
            {
                case "pcSplit(2) input":
                    trends.hist.plot.maxRange = task.MaxZmeters;
                    trends.hist.plot.removeZeroEntry = true; // not interested in the undefined depth areas...
                    trends.Run(task.pcSplit[2]);
                    labels[2] = "CS_SLR_TrendImages - pcSplit(2)";
                    break;
                case "Grayscale input":
                    trends.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY));
                    labels[2] = "CS_SLR_TrendImages - grayscale";
                    break;
                case "Blue input":
                    labels[2] = "CS_SLR_TrendImages - Blue channel";
                    splitIndex = 0;
                    break;
                case "Green input":
                    labels[2] = "CS_SLR_TrendImages - Green channel";
                    splitIndex = 1;
                    break;
                case "Red input":
                    labels[2] = "CS_SLR_TrendImages - Red channel";
                    splitIndex = 2;
                    break;
            }
            trends.Run(split[splitIndex]);
            dst2 = trends.dst2;
        }
    }
    public class CS_Smoothing_Exterior : CS_Parent
    {
        Convex_Basics hull = new Convex_Basics();
        public List<cv.Point> inputPoints { get; set; }
        public List<cv.Point> smoothPoints { get; set; }
        public Scalar plotColor = Scalar.Yellow;
        Options_Smoothing smOptions = new Options_Smoothing();
        List<cv.Point> getSplineInterpolationCatmullRom(List<cv.Point> points, int nrOfInterpolatedPoints)
        {
            List<cv.Point> spline = new List<cv.Point>();
            List<cv.Point> spoints = new List<cv.Point>(points);
            cv.Point startPt = (spoints[1] + spoints[0]) * 0.5;
            spoints.Insert(0, startPt);
            cv.Point endPt = (spoints[spoints.Count() - 1] + spoints[spoints.Count() - 2]) * 0.5;
            spoints.Add(endPt);
            double t;
            cv.Point spoint;
            for (int i = 0; i <= spoints.Count() - 4; i++)
            {
                spoint = new cv.Point();
                for (int j = 0; j < nrOfInterpolatedPoints; j++)
                {
                    cv.Point x0 = spoints[i % spoints.Count()];
                    cv.Point x1 = spoints[(i + 1) % spoints.Count()];
                    cv.Point x2 = spoints[(i + 2) % spoints.Count()];
                    cv.Point x3 = spoints[(i + 3) % spoints.Count()];
                    t = 1.0 / nrOfInterpolatedPoints * j;
                    spoint.X = (int)(0.5 * (2 * x1.X + (-1 * x0.X + x2.X) * t + (2 * x0.X - 5 * x1.X + 4 * x2.X - x3.X) * t * t +
                                      (-1 * x0.X + 3 * x1.X - 3 * x2.X + x3.X) * t * t * t));
                    spoint.Y = (int)(0.5 * (2 * x1.Y + (-1 * x0.Y + x2.Y) * t + (2 * x0.Y - 5 * x1.Y + 4 * x2.Y - x3.Y) * t * t +
                                      (-1 * x0.Y + 3 * x1.Y - 3 * x2.Y + x3.Y) * t * t * t));
                    spline.Add(spoint);
                }
            }
            spline.Add(spoints[spoints.Count() - 2]);
            return spline;
        }
        public CS_Smoothing_Exterior(VBtask task) : base(task)
        {
            labels[2] = "Original Points (white) Smoothed (yellow)";
            labels[3] = "";
            desc = "Smoothing the line connecting a series of points.";
        }
        public void RunCS(Mat src)
        {
            smOptions.RunVB();
            if (standaloneTest())
            {
                if (task.heartBeat && !task.paused)
                {
                    List<cv.Point> hullList = hull.buildRandomHullPoints();
                    dst2.SetTo(0);
                    hull.Run(src);
                    cv.Point[] nextHull = Cv2.ConvexHull(hullList.ToArray(), true);
                    inputPoints = new List<cv.Point>(nextHull);
                    DrawPoly(dst2, inputPoints, Scalar.White);
                }
                else
                {
                    return;
                }
            }
            else
            {
                dst2.SetTo(0);
            }
            if (inputPoints.Count() > 1)
            {
                smoothPoints = getSplineInterpolationCatmullRom(inputPoints, smOptions.iterations);
                DrawPoly(dst2, smoothPoints, plotColor);
            }
        }
    }
    public class CS_Smoothing_Interior : CS_Parent
    {
        Convex_Basics hull = new Convex_Basics();
        public List<cv.Point> inputPoints { get; set; }
        public List<cv.Point> smoothPoints { get; set; }
        public Scalar plotColor = Scalar.Yellow;
        Options_Smoothing smOptions = new Options_Smoothing();
        List<cv.Point2d> getCurveSmoothingChaikin(List<cv.Point> points, double tension, int nrOfIterations)
        {
            double cutdist = 0.05 + (tension * 0.4);
            List<cv.Point2d> nl = new List<cv.Point2d>();
            for (int i = 0; i < points.Count(); i++)
            {
                nl.Add(new Point2d(points[i].X, points[i].Y));
            }
            for (int i = 1; i <= nrOfIterations; i++)
            {
                if (nl.Count() > 0) nl = getSmootherChaikin(nl, cutdist);
            }
            return nl;
        }
        List<cv.Point2d> getSmootherChaikin(List<cv.Point2d> points, double cuttingDist)
        {
            List<cv.Point2d> nl = new List<cv.Point2d>();
            nl.Add(points[0]);
            for (int i = 0; i < points.Count() - 1; i++)
            {
                Point2d pt1 = new Point2d((1 - cuttingDist) * points[i].X, (1 - cuttingDist) * points[i].Y);
                Point2d pt2 = new Point2d(cuttingDist * points[i + 1].X, cuttingDist * points[i + 1].Y);
                nl.Add(pt1 + pt2);
                pt1 = new Point2d(cuttingDist * points[i].X, cuttingDist * points[i].Y);
                pt2 = new Point2d((1 - cuttingDist) * points[i + 1].X, (1 - cuttingDist) * points[i + 1].Y);
                nl.Add(pt1 + pt2);
            }
            nl.Add(points[points.Count() - 1]);
            return nl;
        }
        public CS_Smoothing_Interior(VBtask task) : base(task)
        {
            if (standaloneTest()) FindSlider("Hull random points").Value = 16;
            labels[2] = "Original Points (white) Smoothed (yellow)";
            labels[3] = "";
            desc = "Smoothing the line connecting a series of points staying inside the outline.";
        }
        public void RunCS(Mat src)
        {
            smOptions.RunVB();
            if (standaloneTest())
            {
                if (task.heartBeat && !task.paused)
                {
                    List<cv.Point> hullList = hull.buildRandomHullPoints();
                    dst2.SetTo(0);
                    hull.Run(src);
                    cv.Point[] nextHull = Cv2.ConvexHull(hullList.ToArray(), true);
                    inputPoints = new List<cv.Point>(nextHull);
                    DrawPoly(dst2, nextHull.ToList(), Scalar.White);
                }
                else
                {
                    return;
                }
            }
            else
            {
                dst2.SetTo(0);
            }
            List<cv.Point2d> smoothPoints2d = getCurveSmoothingChaikin(inputPoints, smOptions.interiorTension, smOptions.iterations);
            smoothPoints = new List<cv.Point>();
            for (int i = 0; i < smoothPoints2d.Count(); i += smOptions.stepSize)
            {
                smoothPoints.Add(new cv.Point((int)smoothPoints2d[i].X, (int)smoothPoints2d[i].Y));
            }
            if (smoothPoints.Count() > 0) DrawPoly(dst2, smoothPoints, plotColor);
        }
    }

    public class CS_Solve_ByMat : CS_Parent
    {
        public CS_Solve_ByMat(VBtask task) : base(task)
        {
            desc = "Solve a set of equations with OpenCV's Solve API.";
        }
        public void RunCS(Mat src)
        {
            // x + y = 10
            // 2x + 3y = 26
            // (x=4, y=6)
            double[,] av = { { 1, 1 }, { 2, 3 } };
            double[] yv = { 10, 26 };
            Mat a = new Mat(2, 2, MatType.CV_64FC1, av);
            Mat y = new Mat(2, 1, MatType.CV_64FC1, yv);
            Mat x = new Mat();
            Cv2.Solve(a, y, x, DecompTypes.LU);
            SetTrueText("Solution ByMat: X1 = " + x.At<double>(0, 0) + "\tX2 = " + x.At<double>(0, 1), new cv.Point(10, 125));
        }
    }
    public class CS_Solve_ByArray : CS_Parent
    {
        public CS_Solve_ByArray(VBtask task) : base(task)
        {
            desc = "Solve a set of equations with OpenCV's Solve API with a normal array as input  ";
        }
        public void RunCS(Mat src)
        {
            // x + y = 10
            // 2x + 3y = 26
            // (x=4, y=6)
            double[,] av = { { 1, 1 }, { 2, 3 } };
            double[] yv = { 10, 26 };
            Mat x = new Mat();
            Cv2.Solve(InputArray.Create(av), InputArray.Create(yv), x, DecompTypes.LU);
            SetTrueText("Solution ByArray: X1 = " + x.At<double>(0, 0) + "\tX2 = " + x.At<double>(0, 1), new cv.Point(10, 125));
        }
    }
    public class CS_Sort_Basics : CS_Parent
    {
        Options_Sort options = new Options_Sort();
        public CS_Sort_Basics(VBtask task) : base(task)
        {
            desc = "Sort the pixels of a grayscale image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.radio5.Checked)
            {
                src = src.Reshape(1, src.Rows * src.Cols);
                options.sortOption = SortFlags.EveryColumn | SortFlags.Descending;
            }
            if (options.radio4.Checked)
            {
                src = src.Reshape(1, src.Rows * src.Cols);
                options.sortOption = SortFlags.EveryColumn | SortFlags.Ascending;
            }
            if (src.Channels() == 3) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            dst2 = src.Sort(options.sortOption);
            if (options.radio4.Checked || options.radio5.Checked) dst2 = dst2.Reshape(1, dst0.Rows);
        }
    }
    public class CS_Sort_RectAndMask : CS_Parent
    {
        Sort_Basics sort = new Sort_Basics();
        public Mat mask;
        public cv.Rect rect;
        public CS_Sort_RectAndMask(VBtask task) : base(task)
        {
            labels[3] = "Original input to sort";
            if (standaloneTest()) task.drawRect = new cv.Rect(10, 10, 50, 5);
            desc = "Sort the grayscale image portion in a rect while allowing for a mask.";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() == 3) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            var tmpRect = rect == new cv.Rect() ? task.drawRect : rect;
            dst1 = src[tmpRect].Clone();
            if (mask != null)
            {
                mask = mask.Threshold(0, 255, ThresholdTypes.BinaryInv);
                dst1.SetTo(0, mask);
            }
            sort.Run(dst1);
            dst2 = sort.dst2.Reshape(1, dst1.Rows);
            dst2 = dst2.Resize(dst3.Size());
            if (standaloneTest()) dst3 = src[tmpRect].Resize(dst3.Size());
        }
    }
    public class CS_Sort_MLPrepTest_CPP : CS_Parent
    {
        public Reduction_Basics reduction = new Reduction_Basics();
        public Mat MLTestData = new Mat();
        public CS_Sort_MLPrepTest_CPP(VBtask task) : base(task)
        {
            cPtr = Sort_MLPrepTest_Open();
            desc = "Prepare the grayscale image and row to predict depth";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            reduction.Run(src);
            byte[] dataSrc = new byte[reduction.dst2.Total() * reduction.dst2.ElemSize()];
            Marshal.Copy(reduction.dst2.Data, dataSrc, 0, dataSrc.Length);
            var handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            var imagePtr = Sort_MLPrepTest_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols);
            handleSrc.Free();
            MLTestData = new Mat(src.Rows, src.Cols, MatType.CV_32FC2, imagePtr).Clone();
            var split = MLTestData.Split();
            dst2 = split[0];
            dst3 = split[1];
        }
        public void Close()
        {
            if (cPtr != (IntPtr)0) cPtr = Sort_MLPrepTest_Close(cPtr);
        }
    }
    public class CS_Sort_1Channel : CS_Parent
    {
        Sort_Basics sort = new Sort_Basics();
        ML_RemoveDups_CPP dups = new ML_RemoveDups_CPP();
        public List<int> rangeStart = new List<int>();
        public List<int> rangeEnd = new List<int>();
        TrackBar thresholdSlider;
        public CS_Sort_1Channel(VBtask task) : base(task)
        {
            thresholdSlider = FindSlider("Threshold for sort input");
            if (standaloneTest()) task.gOptions.setDisplay1();
            FindRadio("Sort all pixels descending").Checked = true;
            if (standaloneTest()) task.gOptions.setGridSize(10);
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            labels = new[] { "", "Mask used to isolate the gray scale input to sort", "Sorted thresholded data", "Output of sort - no duplicates" };
            desc = "Take some 1-channel input, sort it, and provide the list of unique elements";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            dst1 = src.Threshold(thresholdSlider.Value, 255, ThresholdTypes.Binary);
            dst2.SetTo(0);
            src.CopyTo(dst2, dst1);
            sort.Run(dst2);
            var pixelsPerBlock = (int)(dst3.Total() / dst2.Rows);
            var sq = Math.Sqrt(pixelsPerBlock);
            task.gOptions.setGridSize((int)Math.Min(sq, 10));
            dst0 = sort.dst2.Reshape(1, dst2.Rows);
            dups.Run(dst0);
            dst3.SetTo(255);
            var inputCount = dups.dst3.CountNonZero();
            var testVals = new List<int>();
            for (int i = 0; i < Math.Min(inputCount, task.gridList.Count()); i++)
            {
                var roi = task.gridList[i];
                var val = (int)dups.dst3.Get<byte>(0, i);
                testVals.Add(val);
                dst3[roi].SetTo(val);
            }
            if (testVals.Count() == 0) return;
            rangeStart.Clear();
            rangeEnd.Clear();
            rangeStart.Add(testVals[0]);
            for (int i = 0; i < testVals.Count() - 1; i++)
            {
                if (Math.Abs(testVals[i] - testVals[i + 1]) > 1)
                {
                    rangeEnd.Add(testVals[i]);
                    rangeStart.Add(testVals[i + 1]);
                }
            }
            rangeEnd.Add(testVals[testVals.Count() - 1]);
            labels[3] = " The number of unique entries = " + inputCount + " were spread across " + rangeStart.Count() + " ranges";
        }
    }
    public class CS_Sort_3Channel : CS_Parent
    {
        Sort_Basics sort = new Sort_Basics();
        ML_RemoveDups_CPP dups = new ML_RemoveDups_CPP();
        Mat bgra;
        TrackBar thresholdSlider;
        public CS_Sort_3Channel(VBtask task) : base(task)
        {
            thresholdSlider = FindSlider("Threshold for sort input");
            if (standaloneTest()) task.gOptions.setDisplay1();
            FindRadio("Sort all pixels descending").Checked = true;
            labels = new[] { "", "The BGRA input to sort - shown here as 1-channel CV_32S format", "Output of sort - no duplicates", "Input before removing the dups - use slider to increase/decrease the amount of data" };
            desc = "Take some 3-channel input, convert it to BGRA, sort it as integers, and provide the list of unique elements";
        }
        public void RunCS(Mat src)
        {
            var inputMask = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            if (standaloneTest()) inputMask = inputMask.Threshold(thresholdSlider.Value, 255, ThresholdTypes.Binary);
            bgra = src.CvtColor(cv.ColorConversionCodes.BGR2BGRA);
            dst1 = new Mat(dst1.Rows, dst1.Cols, MatType.CV_32S, bgra.Data);
            dst0 = new Mat(dst0.Size(), MatType.CV_32S, 0);
            dst1.CopyTo(dst0, inputMask);
            sort.Run(dst0);
            dst2 = sort.dst2.Reshape(1, dst2.Rows);
            var tmp = new Mat(src.Rows, src.Cols, MatType.CV_8UC4, dst2.Data);
            dst3 = tmp.CvtColor(cv.ColorConversionCodes.BGRA2BGR);
            //dups.Run(dst2);
            //dst2 = dups.dst2;
        }
    }
    public class CS_Sort_FeatureLess : CS_Parent
    {
        public FeatureROI_Basics devGrid = new FeatureROI_Basics();
        public Sort_Basics sort = new Sort_Basics();
        Plot_Histogram plot = new Plot_Histogram();
        public CS_Sort_FeatureLess(VBtask task) : base(task)
        {
            plot.createHistogram = true;
            task.gOptions.setHistogramBins(256);
            task.gOptions.setGridSize(8);
            desc = "Sort all the featureless grayscale pixels.";
        }
        public void RunCS(Mat src)
        {
            devGrid.Run(src);
            dst2 = devGrid.dst2;
            dst1 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            dst1.SetTo(0, ~devGrid.dst3);
            sort.Run(dst1);
            // dst3 = sort.dst2;
            byte[] samples = new byte[sort.dst2.Total()];
            Marshal.Copy(sort.dst2.Data, samples, 0, samples.Length);
            plot.Run(sort.dst2);
            dst3 = plot.dst2;
        }
    }
    public class CS_Sort_Integer : CS_Parent
    {
        Sort_Basics sort = new Sort_Basics();
        public int[] data;
        public List<int> vecList = new List<int>();
        public CS_Sort_Integer(VBtask task) : base(task)
        {
            data = new int[dst2.Total()];
            FindRadio("Sort all pixels ascending").Checked = true;
            labels = new string[] { "", "Mask used to isolate the gray scale input to sort", "Sorted thresholded data", "Output of sort - no duplicates" };
            desc = "Take some 1-channel input, sort it, and provide the list of unique elements";
        }
        public void RunCS(Mat src)
        {
            if (standalone)
            {
                Mat[] split = src.Split();
                Mat zero = new Mat(split[0].Size(), MatType.CV_8U, 0);
                Cv2.Merge(new Mat[] { split[0], split[1], split[2], zero }, src);
                Marshal.Copy(src.Data, data, 0, data.Length);
                src = new Mat(src.Size(), MatType.CV_32S, 0);
                Marshal.Copy(data, 0, src.Data, data.Length);
            }
            sort.Run(src);
            Marshal.Copy(sort.dst2.Data, data, 0, data.Length);
            vecList.Clear();
            vecList.Add(data[0]);
            for (int i = 1; i < data.Length; i++)
            {
                if (data[i - 1] != data[i]) vecList.Add(data[i]);
            }
            labels[2] = "There were " + vecList.Count().ToString() + " unique 8UC3 pixels in the input.";
        }
    }
    public class CS_Sort_GrayScale1 : CS_Parent
    {
        Sort_Integer sort = new Sort_Integer();
        byte[][] pixels = new byte[3][];
        public CS_Sort_GrayScale1(VBtask task) : base(task)
        {
            desc = "Sort the grayscale image but keep the 8uc3 pixels with each gray entry.";
        }
        public void RunCS(Mat src)
        {
            dst1 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            byte[] gray = new byte[dst1.Total()];
            Marshal.Copy(dst1.Data, gray, 0, gray.Length);
            Mat[] split = src.Split();
            for (int i = 0; i < 3; i++)
            {
                if (task.FirstPass) pixels[i] = new byte[src.Total()];
                Marshal.Copy(split[i].Data, pixels[i], 0, pixels[i].Length);
            }
            uint[] input = new uint[gray.Length];
            for (int i = 0; i < gray.Length; i++)
            {
                input[i] = (uint)(pixels[0][i] * 65536 + pixels[1][i] * 256 + pixels[2][i]);
            }
            sort.Run(new Mat(gray.Length, 1, MatType.CV_32S, input));
            List<uint> unique = new List<uint>();
            unique.Add((uint)sort.data[0]);
            for (int i = 1; i < sort.data.Length; i++)
            {
                if (sort.data[i - 1] != sort.data[i]) unique.Add((uint)sort.data[i]);
            }
            labels[2] = "There were " + unique.Count().ToString() + " distinct pixels in the image.";
        }
    }
    public class CS_Sort_GrayScale : CS_Parent
    {
        Plot_Histogram plot = new Plot_Histogram();
        byte[][] pixels = new byte[3][];
        public CS_Sort_GrayScale(VBtask task) : base(task)
        {
            desc = "Sort the grayscale image but keep the 8uc3 pixels with each gray entry.";
        }
        public void RunCS(Mat src)
        {
            Mat[] split = src.Split();
            for (int i = 0; i < 3; i++)
            {
                if (task.FirstPass) pixels[i] = new byte[src.Total()];
                Marshal.Copy(split[i].Data, pixels[i], 0, pixels[i].Length);
            }
            float[] totals = new float[256];
            Vec3b[] lut = new Vec3b[256];
            for (int i = 0; i < src.Total(); i++)
            {
                int index = (int)(0.299 * pixels[2][i] + 0.587 * pixels[1][i] + 0.114 * pixels[0][i]);
                totals[index] += 1;
                if (totals[index] == 1) lut[index] = new Vec3b(pixels[0][i], pixels[1][i], pixels[2][i]);
            }
            Mat histogram = new Mat(256, 1, MatType.CV_32F, totals);
            plot.Run(histogram);
            dst2 = plot.dst2;
        }
    }
    public class CS_Spectrum_Basics : CS_Parent
    {
        Spectrum_Z dSpec = new Spectrum_Z();
        Spectrum_Gray gSpec = new Spectrum_Gray();
        public Options_Spectrum options = new Options_Spectrum();
        public CS_Spectrum_Basics(VBtask task) : base(task)
        {
            desc = "Given a RedCloud cell, create a spectrum that contains the ranges of the depth and color.";
        }
        public void RunCS(Mat src)
        {
            dst2 = options.runRedCloud(ref labels[2]);
            dSpec.Run(src);
            gSpec.Run(src);
            if (task.heartBeat && task.rc.index > 0)
            {
                strOut = dSpec.strOut + "\n\n" + gSpec.strOut;
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Spectrum_X : CS_Parent
    {
        public Options_Spectrum options = new Options_Spectrum();
        public CS_Spectrum_X(VBtask task) : base(task)
        {
            desc = "Given a RedCloud cell, create a spectrum that contains the depth ranges.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (standaloneTest()) dst2 = options.runRedCloud(ref labels[2]);
            if (task.heartBeat && task.rc.index > 0)
            {
                var ranges = options.buildDepthRanges(task.pcSplit[0][task.rc.rect].Clone(), " pointcloud X ");
                strOut = options.strOut;
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Spectrum_Y : CS_Parent
    {
        public Options_Spectrum options = new Options_Spectrum();
        public CS_Spectrum_Y(VBtask task) : base(task)
        {
            desc = "Given a RedCloud cell, create a spectrum that contains the depth ranges.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (standaloneTest()) dst2 = options.runRedCloud(ref labels[2]);
            if (task.heartBeat && task.rc.index > 0)
            {
                var ranges = options.buildDepthRanges(task.pcSplit[1][task.rc.rect].Clone(), " pointcloud Y ");
                strOut = options.strOut;
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Spectrum_Z : CS_Parent
    {
        public Options_Spectrum options = new Options_Spectrum();
        public CS_Spectrum_Z(VBtask task) : base(task)
        {
            desc = "Given a RedCloud cell, create a spectrum that contains the depth ranges.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (standaloneTest()) dst2 = options.runRedCloud(ref labels[2]);
            if (task.heartBeat && task.rc.index > 0)
            {
                var ranges = options.buildDepthRanges(task.pcSplit[2][task.rc.rect].Clone(), " pointcloud Z ");
                strOut = options.strOut;
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Spectrum_Cloud : CS_Parent
    {
        public Options_Spectrum options = new Options_Spectrum();
        Spectrum_X specX = new Spectrum_X();
        Spectrum_Y specY = new Spectrum_Y();
        Spectrum_Z specZ = new Spectrum_Z();
        public CS_Spectrum_Cloud(VBtask task) : base(task)
        {
            desc = "Given a RedCloud cell, create a spectrum that contains the ranges for X, Y, and Z in the point cloud.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (standaloneTest()) dst2 = options.runRedCloud(ref labels[2]);
            if (task.heartBeat)
            {
                specX.Run(src);
                strOut = specX.strOut + "\n";
                specY.Run(src);
                strOut += specY.strOut + "\n";
                specZ.Run(src);
                strOut += specZ.strOut;
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Spectrum_GrayAndCloud : CS_Parent
    {
        Options_Spectrum options = new Options_Spectrum();
        Spectrum_Gray gSpec = new Spectrum_Gray();
        Spectrum_Cloud sCloud = new Spectrum_Cloud();
        public CS_Spectrum_GrayAndCloud(VBtask task) : base(task)
        {
            desc = "Given a RedCloud cell, create a spectrum that contains the ranges for X, Y, and Z in the point cloud.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (standaloneTest()) dst2 = options.runRedCloud(ref labels[2]);
            if (task.heartBeat)
            {
                sCloud.Run(src);
                strOut = sCloud.strOut + "\n";
                gSpec.Run(src);
                strOut += gSpec.strOut;
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Spectrum_RGB : CS_Parent
    {
        Options_Spectrum options = new Options_Spectrum();
        Spectrum_Gray gSpec = new Spectrum_Gray();
        public CS_Spectrum_RGB(VBtask task) : base(task)
        {
            desc = "Create a spectrum of the RGB values for a given RedCloud cell.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (standaloneTest()) dst2 = options.runRedCloud(ref labels[2]);
            var split = src.Split();
            gSpec.typeSpec = " blue ";
            gSpec.Run(split[0]);
            if (task.heartBeat) strOut = gSpec.strOut + "\n";
            gSpec.typeSpec = " green ";
            gSpec.Run(split[1]);
            if (task.heartBeat) strOut += gSpec.strOut + "\n";
            gSpec.typeSpec = " red ";
            gSpec.Run(split[2]);
            if (task.heartBeat) strOut += gSpec.strOut;
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Spectrum_CellZoom : CS_Parent
    {
        Resize_Proportional proportion = new Resize_Proportional();
        Spectrum_Breakdown breakdown = new Spectrum_Breakdown();
        public CS_Spectrum_CellZoom(VBtask task) : base(task)
        {
            labels = new string[] { "", "Cell trimming information", "", "White is after trimming, gray is before trim, black is outside the cell mask." };
            if (standaloneTest()) task.gOptions.setDisplay1();
            desc = "Zoom in on the selected RedCloud cell before and after Spectrum filtering.";
        }
        public void RunCS(Mat src)
        {
            breakdown.options.RunVB();
            dst2 = breakdown.options.runRedCloud(ref labels[2]);
            if (task.heartBeat)
            {
                breakdown.Run(src);
                SetTrueText(breakdown.strOut, 1);
                proportion.Run(breakdown.dst3);
                dst3 = proportion.dst2;
                strOut = breakdown.options.strOut;
            }
            SetTrueText(strOut, 1);
        }
    }
    public class CS_Spectrum_Breakdown : CS_Parent
    {
        public Options_Spectrum options = new Options_Spectrum();
        public bool buildMaskOnly;
        Resize_Proportional proportion = new Resize_Proportional();
        public CS_Spectrum_Breakdown(VBtask task) : base(task)
        {
            desc = "Breakdown a cell if possible.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                options.RunVB();
                dst2 = options.runRedCloud(ref labels[2]);
            }
            var rc = task.rc;
            List<rangeData> ranges;
            Mat input;
            if (rc.pixels == 0) return;
            if (rc.depthPixels / rc.pixels < 0.5)
            {
                input = new Mat(rc.mask.Size(), MatType.CV_8U, 0);
                src[rc.rect].CopyTo(input, rc.mask);
                input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                input = new Mat(rc.mask.Size(), MatType.CV_32F, 0);
                task.pcSplit[2][rc.rect].CopyTo(input, rc.mask);
            }
            ranges = options.buildColorRanges(input, "GrayScale");
            if (ranges.Count() == 0) return; // all the counts were too small - rare but it happens.
            rangeData maxRange = null;
            int maxPixels = 0;
            foreach (var r in ranges)
            {
                if (r.pixels > maxPixels)
                {
                    maxPixels = r.pixels;
                    maxRange = r;
                }
            }
            Mat rangeClip = new Mat(input.Size(), MatType.CV_8U, 0);
            if (input.Type() == MatType.CV_8U)
            {
                rangeClip = input.InRange(maxRange.start, maxRange.ending);
                rangeClip = rangeClip.Threshold(0, 255, ThresholdTypes.Binary).ConvertScaleAbs();
            }
            else
            {
                rangeClip = new Mat(rc.mask.Size(), MatType.CV_32F, 0);
                input.CopyTo(rangeClip, rc.mask);
                rangeClip = rangeClip.InRange(maxRange.start / 100, maxRange.ending / 100);
                rangeClip = rangeClip.Threshold(0, 255, ThresholdTypes.Binary).ConvertScaleAbs();
            }
            if (!buildMaskOnly)
            {
                dst3 = rc.mask.Threshold(0, 128, ThresholdTypes.Binary);
                dst3.SetTo(255, rangeClip);
            }
            if (standaloneTest())
            {
                proportion.Run(dst3);
                dst3 = proportion.dst2;
            }
            rc.mask = rc.mask.Threshold(0, 255, ThresholdTypes.Binary);
            task.rc = rc;
        }
    }
    public class CS_Spectrum_RedCloud : CS_Parent
    {
        Spectrum_Breakdown breakdown = new Spectrum_Breakdown();
        public List<rcData> redCells = new List<rcData>();
        public CS_Spectrum_RedCloud(VBtask task) : base(task)
        {
            desc = "Breakdown each cell in redCells.";
        }
        public void RunCS(Mat src)
        {
            breakdown.options.RunVB();
            dst2 = breakdown.options.runRedCloud(ref labels[2]);
            redCells.Clear();
            dst3.SetTo(0);
            foreach (var rc in task.redCells)
            {
                task.rc = rc;
                breakdown.Run(src);
                var rcNew = task.rc;
                redCells.Add(rcNew);
                dst3[rcNew.rect].SetTo(rcNew.color, rcNew.mask);
            }
            breakdown.Run(src);
        }
    }
    public class CS_Spectrum_Mask : CS_Parent
    {
        Spectrum_Gray gSpec = new Spectrum_Gray();
        public CS_Spectrum_Mask(VBtask task) : base(task)
        {
            if (standaloneTest()) strOut = "Select a cell to see its depth spectrum";
            if (standaloneTest()) task.gOptions.setDisplay1();
            desc = "Create a mask from the Spectrum ranges";
        }
        public void RunCS(Mat src)
        {
            gSpec.Run(src);
            dst1 = gSpec.dst2;
            labels[2] = gSpec.labels[2];
            if (task.heartBeat) strOut = gSpec.strOut;
        }
    }
    public class CS_Spectrum_Gray : CS_Parent
    {
        Options_Spectrum options = new Options_Spectrum();
        public string typeSpec = "GrayScale";
        public CS_Spectrum_Gray(VBtask task) : base(task)
        {
            desc = "Given a RedCloud cell, create a spectrum that contains the color ranges.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (standaloneTest()) dst2 = options.runRedCloud(ref labels[2]);
            var input = src[task.rc.rect];
            if (input.Type() != MatType.CV_8U) input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            var ranges = options.buildColorRanges(input, typeSpec);
            strOut = options.strOut;
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Stabilizer_Basics : CS_Parent
    {
        Match_Basics match = new Match_Basics();
        public int shiftX;
        public int shiftY;
        public cv.Rect templateRect;
        public cv.Rect searchRect;
        public cv.Rect stableRect;
        Options_Stabilizer options = new Options_Stabilizer();
        Mat lastFrame;
        public CS_Stabilizer_Basics(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            labels[2] = "Current frame - rectangle input to matchTemplate";
            desc = "if reasonable stdev and no motion in correlation rectangle, stabilize image across frames";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            bool resetImage = false;
            templateRect = new cv.Rect(src.Width / 2 - options.width / 2, src.Height / 2 - options.height / 2,
                                       options.width, options.height);
            if (src.Channels() != 1) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            if (task.FirstPass) lastFrame = src.Clone();
            dst2 = src.Clone();
            Scalar mean;
            Scalar stdev;
            Cv2.MeanStdDev(dst2[templateRect], out mean, out stdev);
            if (stdev[0] > options.minStdev)
            {
                var t = templateRect;
                int w = t.Width + options.pad * 2;
                int h = t.Height + options.pad * 2;
                int x = Math.Abs(t.X - options.pad);
                int y = Math.Abs(t.Y - options.pad);
                searchRect = new cv.Rect(x, y, Math.Min(w, lastFrame.Width - x - 1), Math.Min(h, lastFrame.Height - y - 1));
                match.template = lastFrame[searchRect];
                match.Run(src[templateRect]);
                if (match.correlation > options.corrThreshold)
                {
                    var maxLoc = new cv.Point(match.matchCenter.X, match.matchCenter.Y);
                    shiftX = templateRect.X - maxLoc.X - searchRect.X;
                    shiftY = templateRect.Y - maxLoc.Y - searchRect.Y;
                    int x1 = shiftX < 0 ? Math.Abs(shiftX) : 0;
                    int y1 = shiftY < 0 ? Math.Abs(shiftY) : 0;
                    dst3.SetTo(0);
                    int x2 = shiftX < 0 ? 0 : shiftX;
                    int y2 = shiftY < 0 ? 0 : shiftY;
                    stableRect = new cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY));
                    var srcRect = new cv.Rect(x2, y2, stableRect.Width, stableRect.Height);
                    stableRect = new cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY));
                    src[srcRect].CopyTo(dst3[stableRect]);
                    double nonZero = Cv2.CountNonZero(dst3) / (dst3.Width * dst3.Height);
                    if (nonZero < (1 - options.lostMax))
                    {
                        labels[3] = "Lost pixels = " + string.Format("{0:00%}", 1 - nonZero);
                        resetImage = true;
                    }
                    labels[3] = "Offset (x, y) = (" + shiftX + "," + shiftY + "), " + string.Format("{0:00%}", nonZero) + " preserved, cc=" + string.Format("{0}", match.correlation);
                }
                else
                {
                    labels[3] = "Below correlation threshold " + string.Format("{0}", options.corrThreshold) + " with " +
                                string.Format("{0}", match.correlation);
                    resetImage = true;
                }
            }
            else
            {
                labels[3] = "Correlation rectangle stdev is " + string.Format("{0:00}", stdev[0]) + " - too low";
                resetImage = true;
            }
            if (resetImage)
            {
                src.CopyTo(lastFrame);
                dst3 = lastFrame.Clone();
            }
            if (standaloneTest()) dst3.Rectangle(templateRect, Scalar.White, 1); // when not standaloneTest(), traceName doesn't want artificial rectangle.
        }
    }


    public class CS_Stabilizer_BasicsTest : CS_Parent
    {
        Stabilizer_BasicsRandomInput random = new Stabilizer_BasicsRandomInput();
        Stabilizer_Basics stable = new Stabilizer_Basics();
        public CS_Stabilizer_BasicsTest(VBtask task) : base(task)
        {
            labels[2] = "Unstable input to Stabilizer_Basics";
            desc = "Test the Stabilizer_Basics with random movement";
        }
        public void RunCS(Mat src)
        {
            random.Run(src);
            stable.Run(random.dst3.Clone());
            dst2 = stable.dst2;
            dst3 = stable.dst3;
            if (standaloneTest()) dst3.Rectangle(stable.templateRect, Scalar.White, 1);
            labels[3] = stable.labels[3];
        }
    }
    public class CS_Stabilizer_OpticalFlow : CS_Parent
    {
        public Feature_Basics feat = new Feature_Basics();
        public List<cv.Point2f> inputFeat = new List<cv.Point2f>();
        public int borderCrop = 30;
        Mat sumScale, sScale, features1;
        Mat errScale, qScale, rScale;
        Mat lastFrame;

        public CS_Stabilizer_OpticalFlow(VBtask task) : base(task)
        {
            desc = "Stabilize video with a Kalman filter.  Shake camera to see image edges appear.  This is not really working!";
            labels[2] = "Stabilized Image";
        }
        public void RunCS(Mat src)
        {
            double vert_Border = borderCrop * src.Rows / src.Cols;
            if (task.optionsChanged)
            {
                errScale = new Mat(new cv.Size(1, 5), MatType.CV_64F, 1);
                qScale = new Mat(new cv.Size(1, 5), MatType.CV_64F, 0.004);
                rScale = new Mat(new cv.Size(1, 5), MatType.CV_64F, 0.5);
                sumScale = new Mat(new cv.Size(1, 5), MatType.CV_64F, 0);
                sScale = new Mat(new cv.Size(1, 5), MatType.CV_64F, 0);
            }
            dst2 = src;
            if (src.Channels() == 3) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            if (task.FirstPass) lastFrame = src.Clone();
            feat.Run(src);
            inputFeat = new List<cv.Point2f>(task.features);
            features1 = new Mat(inputFeat.Count(), 1, MatType.CV_32FC2, inputFeat.ToArray());
            if (task.frameCount > 0)
            {
                Mat features2 = new Mat();
                Mat status = new Mat();
                Mat err = new Mat();
                cv.Size winSize = new cv.Size(3, 3);
                TermCriteria term = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.Count, 10, 1.0);
                Cv2.CalcOpticalFlowPyrLK(src, lastFrame, features1, features2, status, err, winSize, 3, term, OpticalFlowFlags.None);
                lastFrame = src.Clone();
                List<cv.Point2f> commonPoints = new List<cv.Point2f>();
                List<cv.Point2f> lastFeatures = new List<cv.Point2f>();
                for (int i = 0; i < status.Rows; i++)
                {
                    if (status.Get<byte>(i, 0) != 0)
                    {
                        Point2f pt1 = features1.Get<cv.Point2f>(i, 0);
                        Point2f pt2 = features2.Get<cv.Point2f>(i, 0);
                        double length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
                        if (length < 10)
                        {
                            commonPoints.Add(pt1);
                            lastFeatures.Add(pt2);
                        }
                    }
                }
                Mat affine = Cv2.GetAffineTransform(commonPoints.ToArray(), lastFeatures.ToArray());
                double dx = affine.Get<double>(0, 2);
                double dy = affine.Get<double>(1, 2);
                double da = Math.Atan2(affine.Get<double>(1, 0), affine.Get<double>(0, 0));
                double ds_x = affine.Get<double>(0, 0) / Math.Cos(da);
                double ds_y = affine.Get<double>(1, 1) / Math.Cos(da);
                double saveDX = dx, saveDY = dy, saveDA = da;
                string text = "Original dx = " + dx.ToString(fmt2) + "\n" + " dy = " + dy.ToString(fmt2) + "\n" + " da = " + da.ToString(fmt2);
                SetTrueText(text);
                double sx = ds_x, sy = ds_y;
                Mat delta = new Mat(5, 1, MatType.CV_64F, new double[] { ds_x, ds_y, da, dx, dy });
                Cv2.Add(sumScale, delta, sumScale);
                Mat diff = new Mat();
                Cv2.Subtract(sScale, sumScale, diff);
                da += diff.Get<double>(2, 0);
                dx += diff.Get<double>(3, 0);
                dy += diff.Get<double>(4, 0);
                if (Math.Abs(dx) > 50) dx = saveDX;
                if (Math.Abs(dy) > 50) dy = saveDY;
                if (Math.Abs(da) > 50) da = saveDA;
                text = "dx = " + dx.ToString(fmt2) + "\n" + " dy = " + dy.ToString(fmt2) + "\n" + " da = " + da.ToString(fmt2);
                SetTrueText(text, new cv.Point(10, 100));
                Mat smoothedMat = new Mat(2, 3, MatType.CV_64F);
                smoothedMat.Set<double>(0, 0, sx * Math.Cos(da));
                smoothedMat.Set<double>(0, 1, sx * -Math.Sin(da));
                smoothedMat.Set<double>(1, 0, sy * Math.Sin(da));
                smoothedMat.Set<double>(1, 1, sy * Math.Cos(da));
                smoothedMat.Set<double>(0, 2, dx);
                smoothedMat.Set<double>(1, 2, dy);
                Mat smoothedFrame = task.color.WarpAffine(smoothedMat, src.Size());
                smoothedFrame = smoothedFrame[new Range((int)vert_Border, (int)(smoothedFrame.Rows - vert_Border)), 
                                              new Range(borderCrop, smoothedFrame.Cols - borderCrop)];
                dst3 = smoothedFrame.Resize(src.Size());
                for (int i = 0; i < commonPoints.Count(); i++)
                {
                    DrawCircle(dst2, commonPoints[i], task.DotSize + 3, Scalar.Red);
                    DrawCircle(dst2, lastFeatures[i], task.DotSize + 1, Scalar.Blue);
                }
            }
            inputFeat = null; // show that we consumed the current set of features.
        }
    }
    public class CS_Stabilizer_VerticalIMU : CS_Parent
    {
        public bool stableTest;
        public string stableStr;
        List<float> angleXValue = new List<float>();
        List<float> angleYValue = new List<float>();
        List<int> stableCount = new List<int>();
        float lastAngleX, lastAngleY;
        public CS_Stabilizer_VerticalIMU(VBtask task) : base(task)
        {
            desc = "Use the IMU angular velocity to determine if the camera is moving or stable.";
        }
        public void RunCS(Mat src)
        {
            angleXValue.Add(task.accRadians.X);
            angleYValue.Add(task.accRadians.Y);
            strOut = "IMU X" + "\t" + "IMU Y" + "\t" + "IMU Z" + "\n";
            strOut += (task.accRadians.X * 57.2958).ToString(fmt3) + "\t" + (task.accRadians.Y * 57.2958).ToString(fmt3) + "\t" +
                      (task.accRadians.Z * 57.2958).ToString(fmt3) + "\n";
            float avgX = angleXValue.Average();
            float avgY = angleYValue.Average();
            if (task.FirstPass)
            {
                lastAngleX = avgX;
                lastAngleY = avgY;
            }
            strOut += "Angle X" + "\t" + "Angle Y" + "\n";
            strOut += avgX.ToString(fmt3) + "\t" + avgY.ToString(fmt3) + "\n";
            float angle = 90 - avgY * 57.2958f;
            if (avgX < 0) angle *= -1;
            labels[2] = "stabilizer_Vertical Angle = " + angle.ToString(fmt1);
            stableTest = Math.Abs(lastAngleX - avgX) < 0.001f && Math.Abs(lastAngleY - avgY) < 0.01f;
            stableCount.Add(stableTest ? 1 : 0);
            if (task.heartBeat)
            {
                float avgStable = (float)stableCount.Average();
                stableStr = "IMU stable = " + avgStable.ToString("0.0%") + " of the time";
                stableCount.Clear();
            }
            SetTrueText(strOut + "\n" + stableStr, 2);
            lastAngleX = avgX;
            lastAngleY = avgY;
            if (angleXValue.Count() >= task.frameHistoryCount) angleXValue.RemoveAt(0);
            if (angleYValue.Count() >= task.frameHistoryCount) angleYValue.RemoveAt(0);
        }
    }
    public class CS_Stabilizer_CornerPoints : CS_Parent
    {
        public Stable_Basics basics = new Stable_Basics();
        public List<cv.Point2f> features = new List<cv.Point2f>();
        cv.Rect ul, ur, ll, lr;
        Options_StabilizerOther options = new Options_StabilizerOther();  
        public CS_Stabilizer_CornerPoints(VBtask task) : base(task)
        {
            desc = "Track the FAST feature points found in the corners of the BGR image.";
        }
        void getKeyPoints(Mat src, cv.Rect r)
        {
            KeyPoint[] kpoints = Cv2.FAST(src[r], options.fastThreshold, true);
            foreach (var kp in kpoints)
            {
                features.Add(new Point2f(kp.Pt.X + r.X, kp.Pt.Y + r.Y));
            }
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
            {
                int size = task.gridSize;
                ul = new cv.Rect(0, 0, size, size);
                ur = new cv.Rect(dst2.Width - size, 0, size, size);
                ll = new cv.Rect(0, dst2.Height - size, size, size);
                lr = new cv.Rect(dst2.Width - size, dst2.Height - size, size, size);
            }
            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            features.Clear();
            getKeyPoints(src, ul);
            getKeyPoints(src, ur);
            getKeyPoints(src, ll);
            getKeyPoints(src, lr);
            dst2.SetTo(0);
            foreach (var pt in features)
            {
                DrawCircle(dst2, pt, task.DotSize, Scalar.Yellow);
            }
            labels[2] = "There were " + features.Count().ToString() + " key points detected";
        }
    }
    public class CS_Stabilizer_BasicsRandomInput : CS_Parent
    {
        Options_StabilizerOther options = new Options_StabilizerOther();
        int lastShiftX;
        int lastShiftY;
        public CS_Stabilizer_BasicsRandomInput(VBtask task) : base(task)
        {
            labels[2] = "Current frame (before)";
            labels[3] = "Image after shift";
            desc = "Generate images that have been arbitrarily shifted";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat input = src;
            if (input.Channels() != 1) input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            int shiftX = msRNG.Next(-options.range, options.range);
            int shiftY = msRNG.Next(-options.range, options.range);
            if (task.FirstPass)
            {
                lastShiftX = shiftX;
                lastShiftY = shiftY;
            }
            if (task.frameCount % 2 == 0)
            {
                shiftX = lastShiftX;
                shiftY = lastShiftY;
            }
            lastShiftX = shiftX;
            lastShiftY = shiftY;
            dst2 = input.Clone();
            if (shiftX != 0 || shiftY != 0)
            {
                int x = shiftX < 0 ? Math.Abs(shiftX) : 0;
                int y = shiftY < 0 ? Math.Abs(shiftY) : 0;
                int x2 = shiftX < 0 ? 0 : shiftX;
                int y2 = shiftY < 0 ? 0 : shiftY;
                cv.Rect srcRect = new cv.Rect(x, y, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY));
                cv.Rect dstRect = new cv.Rect(x2, y2, srcRect.Width, srcRect.Height);
                dst2[srcRect].CopyTo(input[dstRect]);
            }
            dst3 = input;
        }
    }
    public class CS_Stable_Basics : CS_Parent
    {
        public Delaunay_Generations facetGen = new Delaunay_Generations();
        public List<cv.Point2f> ptList = new List<cv.Point2f>();
        public Point2f anchorPoint;
        Feature_KNN good = new Feature_KNN();
        public CS_Stable_Basics(VBtask task) : base(task)
        {
            desc = "Maintain the generation counts around the feature points.";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                good.Run(src);
                facetGen.inputPoints = new List<cv.Point2f>(good.featurePoints);
            }
            facetGen.Run(src);
            if (facetGen.inputPoints.Count() == 0) return; // nothing to work on ...
            ptList.Clear();
            List<int> generations = new List<int>();
            foreach (var pt in facetGen.inputPoints)
            {
                int fIndex = facetGen.facet.facet32s.Get<int>((int)pt.Y, (int)pt.X);
                if (fIndex >= facetGen.facet.facetList.Count()) continue; // new point
                int g = facetGen.dst0.Get<int>((int)pt.Y, (int)pt.X);
                generations.Add(g);
                ptList.Add(pt);
                SetTrueText(g.ToString(), pt);
            }
            int maxGens = generations.Max();
            int index = generations.IndexOf(maxGens);
            anchorPoint = ptList[index];
            if (index < facetGen.facet.facetList.Count())
            {
                var bestFacet = facetGen.facet.facetList[index];
                dst2.FillConvexPoly(bestFacet, Scalar.Black, task.lineType);
                DrawContour(dst2, bestFacet, task.HighlightColor);
            }
            dst2 = facetGen.dst2;
            dst3 = src.Clone();
            for (int i = 0; i < ptList.Count(); i++)
            {
                var pt = ptList[i];
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
                DrawCircle(dst3, pt, task.DotSize, task.HighlightColor);
            }
            labels[2] = $"{ptList.Count()} stable points were identified with {maxGens} generations at the anchor point";
        }
    }
    public class CS_Stable_BasicsCount : CS_Parent
    {
        public Stable_Basics basics = new Stable_Basics();
        public Feature_Basics feat = new Feature_Basics();
        public SortedList<int, int> goodCounts = new SortedList<int, int>(new compareAllowIdenticalIntegerInverted());
        public CS_Stable_BasicsCount(VBtask task) : base(task)
        {
            desc = "Track the stable good features found in the BGR image.";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            basics.facetGen.inputPoints = new List<cv.Point2f>(task.features);
            basics.Run(src);
            dst2 = basics.dst2;
            dst3 = basics.dst3;
            goodCounts.Clear();
            int g;
            for (int i = 0; i < basics.ptList.Count(); i++)
            {
                var pt = basics.ptList[i];
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
                g = basics.facetGen.dst0.Get<int>((int)pt.Y, (int)pt.X);
                goodCounts.Add(g, i);
                SetTrueText(g.ToString(), pt);
            }
            labels[2] = $"{task.features.Count()} good features were found and {basics.ptList.Count()} were stable";
        }
    }
    public class CS_Stable_Lines : CS_Parent
    {
        public Stable_Basics basics = new Stable_Basics();
        Line_Basics lines = new Line_Basics();
        public CS_Stable_Lines(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            desc = "Track the line end points found in the BGR image and keep those that are stable.";
        }
        public void RunCS(Mat src)
        {
            lines.Run(src);
            basics.facetGen.inputPoints.Clear();
            dst1 = src.Clone();
            foreach (var lp in lines.lpList)
            {
                basics.facetGen.inputPoints.Add(lp.p1);
                basics.facetGen.inputPoints.Add(lp.p2);
                DrawLine(dst1, lp.p1, lp.p2, task.HighlightColor);
            }
            basics.Run(src);
            dst2 = basics.dst2;
            dst3 = basics.dst3;
            foreach (var pt in basics.ptList)
            {
                DrawCircle(dst2, pt, task.DotSize + 1, task.HighlightColor);
                if (standaloneTest())
                {
                    int g = basics.facetGen.dst0.Get<int>((int)pt.Y, (int)pt.X);
                    SetTrueText(g.ToString(), pt);
                }
            }
            labels[2] = basics.labels[2];
            labels[3] = $"{lines.lpList.Count()} line end points were found and {basics.ptList.Count()} were stable";
        }
    }
    public class CS_Stable_FAST : CS_Parent
    {
        public Stable_Basics basics = new Stable_Basics();
        readonly Corners_Basics fast = new Corners_Basics();
        public CS_Stable_FAST(VBtask task) : base(task)
        {
            FindSlider("FAST Threshold").Value = 100;
            desc = "Track the FAST feature points found in the BGR image and track those that appear stable.";
        }
        public void RunCS(Mat src)
        {
            fast.Run(src);
            basics.facetGen.inputPoints.Clear();
            basics.facetGen.inputPoints = new List<cv.Point2f>(fast.features);
            basics.Run(src);
            dst3 = basics.dst3;
            dst2 = basics.dst2;
            foreach (var pt in basics.ptList)
            {
                DrawCircle(dst2, pt, task.DotSize + 1, task.HighlightColor);
                if (standaloneTest())
                {
                    int g = basics.facetGen.dst0.Get<int>((int)pt.Y, (int)pt.X);
                    SetTrueText(g.ToString(), pt);
                }
            }
            labels[2] = basics.labels[2];
            labels[3] = $"{fast.features.Count()} features were found and {basics.ptList.Count()} were stable";
        }
    }
    public class CS_Stable_GoodFeatures : CS_Parent
    {
        public Stable_Basics basics = new Stable_Basics();
        public Feature_Basics feat = new Feature_Basics();
        public SortedList<int, int> genSorted = new SortedList<int, int>(new compareAllowIdenticalIntegerInverted());
        public CS_Stable_GoodFeatures(VBtask task) : base(task)
        {
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            desc = "Track the stable good features found in the BGR image.";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            dst3 = basics.dst3;
            if (task.features.Count() == 0) return; // nothing to work on...
            basics.facetGen.inputPoints = new List<cv.Point2f>(task.features);
            basics.Run(src);
            dst2 = basics.dst2;
            dst1.SetTo(0);
            genSorted.Clear();
            for (int i = 0; i < basics.ptList.Count(); i++)
            {
                var pt = basics.ptList[i];
                if (standaloneTest()) DrawCircle(dst2, pt, task.DotSize + 1, Scalar.Yellow);
                dst1.Set<byte>((int)pt.Y, (int)pt.X, 255);
                int g = basics.facetGen.dst0.Get<int>((int)pt.Y, (int)pt.X);
                genSorted.Add(g, i);
                SetTrueText(g.ToString(), pt);
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
            }
            labels[2] = basics.labels[2];
            labels[3] = $"{task.features.Count()} good features were found and {basics.ptList.Count()} were stable";
        }
    }
    public class CS_Stitch_Basics : CS_Parent
    {
        Options_Stitch options = new Options_Stitch();
        public CS_Stitch_Basics(VBtask task) : base(task)
        {
            desc = "Stitch together random parts of a color image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            List<Mat> mats = new List<Mat>();
            dst2 = src.Clone();
            for (int i = 0; i < options.imageCount; i++)
            {
                int x1 = (int)msRNG.Next(0, src.Width - options.width);
                int x2 = (int)msRNG.Next(0, src.Height - options.height);
                cv.Rect rect = new cv.Rect(x1, x2, options.width, options.height);
                dst2.Rectangle(rect, Scalar.Red, 2);
                mats.Add(src[rect].Clone());
            }
            if (task.testAllRunning)
            {
                // It runs fine but after several runs during 'Test All', it will fail with an external exception.  Only happens on 'Test All' runs.
                SetTrueText("CS_Stitch_Basics only fails when running 'Test All'.\n" +
                            "Skipping it during a 'Test All' just so all the other tests can be exercised.", new cv.Point(10, 100), 3);
                return;
            }
            var stitcher = Stitcher.Create(Stitcher.Mode.Scans);
            Mat pano = new Mat();
            // stitcher may fail with an external exception if you make width and height too small.
            if (pano.Cols == 0) return;
            var status = stitcher.Stitch(mats, pano);
            dst3.SetTo(0);
            if (status == Stitcher.Status.OK)
            {
                int w = pano.Width, h = pano.Height;
                if (w > dst2.Width) w = dst2.Width;
                if (h > dst2.Height) h = dst2.Height;
                pano.CopyTo(dst3[new cv.Rect(0, 0, w, h)]);
            }
            else
            {
                if (status == Stitcher.Status.ErrorNeedMoreImgs) SetTrueText("Need more images", 3);
            }
        }
    }
    public class CS_Structured_LinearizeFloor : CS_Parent
    {
        public Structured_FloorCeiling floor = new Structured_FloorCeiling();
        Kalman_VB_Basics kalman = new Kalman_VB_Basics();
        public Mat sliceMask;
        public float floorYPlane;
        Options_StructuredFloor options = new Options_StructuredFloor();
        public CS_Structured_LinearizeFloor(VBtask task) : base(task)
        {
            desc = "Using the mask for the floor create a better representation of the floor plane";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            floor.Run(src);
            dst2 = floor.dst2;
            dst3 = floor.dst3;
            sliceMask = floor.slice.sliceMask;
            Mat imuPC = task.pointCloud.Clone();
            imuPC.SetTo(0, ~sliceMask);
            if (Cv2.CountNonZero(sliceMask) > 0)
            {
                Mat[] split = imuPC.Split();
                if (options.xCheck)
                {
                    mmData mm = GetMinMax(split[0], sliceMask);
                    int firstCol = 0, lastCol = 0;
                    for (firstCol = 0; firstCol < sliceMask.Width; firstCol++)
                    {
                        if (Cv2.CountNonZero(sliceMask.Col(firstCol)) > 0) break;
                    }
                    for (lastCol = sliceMask.Width - 1; lastCol >= 0; lastCol--)
                    {
                        if (Cv2.CountNonZero(sliceMask.Col(lastCol)) > 0) break;
                    }
                    float xIncr = (float)((mm.maxVal - mm.minVal) / (lastCol - firstCol));
                    for (int i = firstCol; i <= lastCol; i++)
                    {
                        Mat maskCol = sliceMask.Col(i);
                        if (Cv2.CountNonZero(maskCol) > 0)
                            split[0].Col(i).SetTo(mm.minVal + xIncr * i, maskCol);
                    }
                }
                if (options.yCheck)
                {
                    mmData mm = GetMinMax(split[1], sliceMask);
                    kalman.kInput = (float)((mm.minVal + mm.maxVal) / 2);
                    kalman.Run(src);
                    floorYPlane = kalman.kAverage;
                    split[1].SetTo(floorYPlane, sliceMask);
                }
                if (options.zCheck)
                {
                    int firstRow = 0, lastRow = 0;
                    for (firstRow = 0; firstRow < sliceMask.Height; firstRow++)
                    {
                        if (Cv2.CountNonZero(sliceMask.Row(firstRow)) > 20) break;
                    }
                    for (lastRow = sliceMask.Height - 1; lastRow >= 0; lastRow--)
                    {
                        if (Cv2.CountNonZero(sliceMask.Row(lastRow)) > 20) break;
                    }
                    if (lastRow >= 0 && firstRow < sliceMask.Height)
                    {
                        Scalar meanMin = split[2].Row(lastRow).Mean(sliceMask.Row(lastRow));
                        Scalar meanMax = split[2].Row(firstRow).Mean(sliceMask.Row(firstRow));
                        float zIncr = (float)(meanMax[0] - meanMin[0]) / Math.Abs(lastRow - firstRow);
                        for (int i = firstRow; i <= lastRow; i++)
                        {
                            Mat maskRow = sliceMask.Row(i);
                            Scalar mean = split[2].Row(i).Mean(maskRow);
                            if (Cv2.CountNonZero(maskRow) > 0)
                            {
                                split[2].Row(i).SetTo(mean[0]);
                            }
                        }
                        DrawLine(dst2, new cv.Point(0, firstRow), new cv.Point(dst2.Width, firstRow), Scalar.Yellow, task.lineWidth + 1);
                        DrawLine(dst2, new cv.Point(0, lastRow), new cv.Point(dst2.Width, lastRow), Scalar.Yellow, task.lineWidth + 1);
                    }
                }
                Cv2.Merge(split, imuPC);
                imuPC.CopyTo(task.pointCloud, sliceMask);
            }
        }
    }
    public class CS_Structured_MultiSlice : CS_Parent
    {
        public HeatMap_Basics heat = new HeatMap_Basics();
        public Mat sliceMask;
        public Mat[] split;
        public Options_Structured options = new Options_Structured();
        public CS_Structured_MultiSlice(VBtask task) : base(task)
        {
            desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            int stepSize = options.stepSize;
            heat.Run(src);
            split = task.pointCloud.Split();
            dst3 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            for (int xCoordinate = 0; xCoordinate < src.Width; xCoordinate += stepSize)
            {
                float planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X;
                if (xCoordinate > task.topCameraPoint.X) planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X);
                Mat depthMask = new Mat();
                double minVal, maxVal;
                minVal = planeX - task.metersPerPixel;
                maxVal = planeX + task.metersPerPixel;
                Cv2.InRange(split[0].Clone(), minVal, maxVal, depthMask);
                sliceMask = depthMask;
                if (minVal < 0 && maxVal > 0) sliceMask.SetTo(0, task.noDepthMask);
                dst3.SetTo(255, sliceMask);
            }
            for (int yCoordinate = 0; yCoordinate < src.Height; yCoordinate += stepSize)
            {
                float planeY = -task.yRange * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y;
                if (yCoordinate > task.sideCameraPoint.Y) planeY = task.yRange * (yCoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y);
                Mat depthMask = new Mat();
                double minVal, maxVal;
                minVal = planeY - task.metersPerPixel;
                maxVal = planeY + task.metersPerPixel;
                Cv2.InRange(split[1].Clone(), minVal, maxVal, depthMask);
                Mat tmp = depthMask;
                sliceMask = tmp | sliceMask;
                dst3.SetTo(255, sliceMask);
            }
            dst2 = task.color.Clone();
            dst2.SetTo(Scalar.White, dst3);
        }
    }
    public class CS_Structured_MultiSliceLines : CS_Parent
    {
        Structured_MultiSlice multi = new Structured_MultiSlice();
        public Line_Basics lines = new Line_Basics();
        public CS_Structured_MultiSliceLines(VBtask task) : base(task)
        {
            desc = "Detect lines in the multiSlice output";
        }
        public void RunCS(Mat src)
        {
            multi.Run(src);
            dst3 = multi.dst3;
            lines.Run(dst3);
            dst2 = lines.dst2;
        }
    }
    public class CS_Structured_Depth : CS_Parent
    {
        Structured_SliceH sliceH = new Structured_SliceH();
        public CS_Structured_Depth(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "", "Use mouse to explore slices", "Top down view of the highlighted slice (at left)" };
            desc = "Use the structured depth to enhance the depth away from the centerline.";
        }
        public void RunCS(Mat src)
        {
            sliceH.Run(src);
            dst0 = sliceH.dst3;
            dst2 = sliceH.dst2;
            Mat mask = sliceH.sliceMask;
            float perMeter = dst3.Height / task.MaxZmeters;
            dst3.SetTo(0);
            Vec3b white = new Vec3b(255, 255, 255);
            for (int y = 0; y < mask.Height; y++)
            {
                for (int x = 0; x < mask.Width; x++)
                {
                    byte val = mask.Get<byte>(y, x);
                    if (val > 0)
                    {
                        float depth = task.pcSplit[2].Get<float>(y, x);
                        int row = dst1.Height - (int)(depth * perMeter);
                        dst3.Set<Vec3b>(row < 0 ? 0 : row, x, white);
                    }
                }
            }
        }
    }
    public class CS_Structured_Rebuild : CS_Parent
    {
        HeatMap_Basics heat = new HeatMap_Basics();
        Options_Structured options = new Options_Structured();
        float thickness;
        public Mat pointcloud = new Mat();
        public CS_Structured_Rebuild(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "X values in point cloud", "Y values in point cloud" };
            desc = "Rebuild the point cloud using inrange - not useful yet";
        }
        Mat rebuildX(Mat viewX)
        {
            Mat output = new Mat(task.pcSplit[1].Size(), MatType.CV_32F, 0);
            int firstCol;
            for (firstCol = 0; firstCol < viewX.Width; firstCol++)
            {
                if (viewX.Col(firstCol).CountNonZero() > 0) break;
            }
            int lastCol;
            for (lastCol = viewX.Height - 1; lastCol >= 0; lastCol--)
            {
                if (viewX.Row(lastCol).CountNonZero() > 0) break;
            }
            Mat sliceMask = new Mat();
            for (int i = firstCol; i <= lastCol; i++)
            {
                float planeX = -task.xRange * (task.topCameraPoint.X - i) / task.topCameraPoint.X;
                if (i > task.topCameraPoint.X) planeX = task.xRange * (i - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X);
                Cv2.InRange(task.pcSplit[0], planeX - thickness, planeX + thickness, sliceMask);
                output.SetTo(planeX, sliceMask);
            }
            return output;
        }
        Mat rebuildY(Mat viewY)
        {
            Mat output = new Mat(task.pcSplit[1].Size(), MatType.CV_32F, 0);
            int firstLine;
            for (firstLine = 0; firstLine < viewY.Height; firstLine++)
            {
                if (viewY.Row(firstLine).CountNonZero() > 0) break;
            }
            int lastLine;
            for (lastLine = viewY.Height - 1; lastLine >= 0; lastLine--)
            {
                if (viewY.Row(lastLine).CountNonZero() > 0) break;
            }
            Mat sliceMask = new Mat();
            for (int i = firstLine; i <= lastLine; i++)
            {
                float planeY = -task.yRange * (task.sideCameraPoint.Y - i) / task.sideCameraPoint.Y;
                if (i > task.sideCameraPoint.Y) planeY = task.yRange * (i - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y);
                Cv2.InRange(task.pcSplit[1], planeY - thickness, planeY + thickness, sliceMask);
                output.SetTo(planeY, sliceMask);
            }
            return output;
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            float metersPerPixel = task.MaxZmeters / dst3.Height;
            thickness = options.sliceSize * metersPerPixel;
            heat.Run(src);
            if (options.rebuilt)
            {
                task.pcSplit[0] = rebuildX(heat.dst3.CvtColor(ColorConversionCodes.BGR2GRAY));
                task.pcSplit[1] = rebuildY(heat.dst2.CvtColor(ColorConversionCodes.BGR2GRAY));
                Cv2.Merge(task.pcSplit, pointcloud);
            }
            else
            {
                task.pcSplit = task.pointCloud.Split();
                pointcloud = task.pointCloud;
            }
            dst2 = GetNormalize32f(task.pcSplit[0]);
            dst3 = GetNormalize32f(task.pcSplit[1]);
            dst2.SetTo(0, task.noDepthMask);
            dst3.SetTo(0, task.noDepthMask);
        }
    }
    public class CS_Structured_Cloud2 : CS_Parent
    {
        Pixel_Measure mmPixel = new Pixel_Measure();
        Options_StructuredCloud options = new Options_StructuredCloud();
        public CS_Structured_Cloud2(VBtask task) : base(task)
        {
            desc = "Attempt to impose a structure on the point cloud data.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat input = src;
            if (input.Type() != MatType.CV_32F) input = task.pcSplit[2];
            float stepX = dst2.Width / options.xLines;
            float stepY = dst2.Height / options.yLines;
            dst3 = new Mat(dst2.Size(), MatType.CV_32FC3, 0);
            float midX = dst2.Width / 2;
            float midY = dst2.Height / 2;
            float halfStepX = stepX / 2;
            float halfStepy = stepY / 2;
            for (int y = 1; y < options.yLines - 1; y++)
            {
                for (int x = 1; x < options.xLines - 1; x++)
                {
                    Point2f p1 = new Point2f(x * stepX, y * stepY);
                    Point2f p2 = new Point2f((x + 1) * stepX, y * stepY);
                    float d1 = task.pcSplit[2].Get<float>((int)p1.Y, (int)p1.X);
                    float d2 = task.pcSplit[2].Get<float>((int)p2.Y, (int)p2.X);
                    if (stepX * options.threshold > Math.Abs(d1 - d2) && d1 > 0 && d2 > 0)
                    {
                        Vec3f p = task.pointCloud.Get<Vec3f>((int)p1.Y, (int)p1.X);
                        float mmPP = mmPixel.Compute(d1);
                        if (options.xConstraint)
                        {
                            p[0] = (p1.X - midX) * mmPP;
                            if (p1.X == midX) p[0] = mmPP;
                        }
                        if (options.yConstraint)
                        {
                            p[1] = (p1.Y - midY) * mmPP;
                            if (p1.Y == midY) p[1] = mmPP;
                        }
                        cv.Rect r = new cv.Rect((int)(p1.X - halfStepX), (int)(p1.Y - halfStepy), (int)stepX, (int)stepY);
                        Scalar meanVal = Cv2.Mean(task.pcSplit[2][r], task.depthMask[r]);
                        p[2] = (d1 + d2) / 2;
                        dst3.Set<Vec3f>(y, x, p);
                    }
                }
            }
            dst2 = dst3[new cv.Rect(0, 0, options.xLines, options.yLines)].Resize(dst2.Size(), 0, 0, InterpolationFlags.Nearest);
        }
    }
    public class CS_Structured_Cloud : CS_Parent
    {
        public Options_StructuredCloud options = new Options_StructuredCloud();
        public CS_Structured_Cloud(VBtask task) : base(task)
        {
            task.gOptions.setGridSize(10);
            desc = "Attempt to impose a linear structure on the pointcloud.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            int yLines = (int)(options.xLines * dst2.Height / dst2.Width);
            float stepX = dst3.Width / options.xLines;
            float stepY = dst3.Height / yLines;
            dst2 = new Mat(dst3.Size(), MatType.CV_32FC3, 0);
            for (int y = 0; y < yLines; y++)
            {
                for (int x = 0; x < options.xLines; x++)
                {
                    cv.Rect r = new cv.Rect((int)(x * stepX), (int)(y * stepY), (int)(stepX - 1), (int)(stepY - 1));
                    cv.Point p1 = new cv.Point(r.X, r.Y);
                    cv.Point p2 = new cv.Point(r.X + r.Width, r.Y + r.Height);
                    Vec3f vec1 = task.pointCloud.Get<Vec3f>(p1.Y, p1.X);
                    Vec3f vec2 = task.pointCloud.Get<Vec3f>(p2.Y, p2.X);
                    if (vec1[2] > 0 && vec2[2] > 0) dst2[r].SetTo(vec1);
                }
            }
            labels[2] = "CS_Structured_Cloud with " + yLines.ToString() + " rows " + options.xLines.ToString() + " columns";
        }
    }
    public class CS_Structured_ROI : CS_Parent
    {
        public Mat data = new Mat();
        public List<cv.Point3f> oglData = new List<cv.Point3f>();
        public CS_Structured_ROI(VBtask task) : base(task)
        {
            task.gOptions.setGridSize(10);
            desc = "Simplify the point cloud so it can be represented as quads in OpenGL";
        }
        public void RunCS(Mat src)
        {
            dst2 = new Mat(dst3.Size(), MatType.CV_32FC3, 0);
            foreach (var roi in task.gridList)
            {
                Scalar d = task.pointCloud[roi].Mean(task.depthMask[roi]);
                Vec3f depth = new Vec3f((float)d.Val0, (float)d.Val1, (float)d.Val2);
                cv.Point pt = new cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2);
                Vec3f vec = task.pointCloud.Get<Vec3f>(pt.Y, pt.X);
                if (vec[2] > 0) dst2[roi].SetTo(depth);
            }
            labels[2] = traceName + " with " + task.gridList.Count().ToString() + " regions was created";
        }
    }
    public class CS_Structured_Tiles : CS_Parent
    {
        public List<Vec3f> oglData = new List<Vec3f>();
        RedCloud_Hulls hulls = new RedCloud_Hulls();
        public CS_Structured_Tiles(VBtask task) : base(task)
        {
            task.gOptions.setGridSize(10);
            desc = "Use the OpenGL point size to represent the point cloud as data";
        }
        public void RunCS(Mat src)
        {
            hulls.Run(src);
            dst2 = hulls.dst3;
            dst3.SetTo(0);
            oglData.Clear();
            foreach (var roi in task.gridList)
            {
                Vec3b c = dst2.Get<Vec3b>(roi.Y, roi.X);
                if (c == black) continue;
                oglData.Add(new Vec3f(c[2] / 255f, c[1] / 255f, c[0] / 255f));
                Scalar v = task.pointCloud[roi].Mean(task.depthMask[roi]);
                oglData.Add(new Vec3f((float)v.Val0, (float)v.Val1, (float)v.Val2));
                dst3[roi].SetTo(c);
            }
            labels[2] = traceName + " with " + task.gridList.Count().ToString() + " regions was created";
        }
    }
    public class CS_Structured_CountTop : CS_Parent
    {
        Structured_SliceV slice = new Structured_SliceV();
        Plot_Histogram plot = new Plot_Histogram();
        List<float> counts = new List<float>();
        public CS_Structured_CountTop(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "Structured Slice heatmap input - red line is max", "Max Slice output - likely vertical surface", "Histogram of pixel counts in each slice" };
            desc = "Count the number of pixels found in each slice of the point cloud data.";
        }
        Mat makeXSlice(int index)
        {
            Mat sliceMask = new Mat();
            double planeX = -task.xRange * (task.topCameraPoint.X - index) / task.topCameraPoint.X;
            if (index > task.topCameraPoint.X) planeX = task.xRange * (index - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X);
            double minVal = planeX - task.metersPerPixel;
            double maxVal = planeX + task.metersPerPixel;
            Cv2.InRange(task.pcSplit[0].Clone(), minVal, maxVal, sliceMask);
            if (minVal < 0 && maxVal > 0) sliceMask.SetTo(0, task.noDepthMask); // don't include zero depth locations
            counts.Add(sliceMask.CountNonZero());
            return sliceMask;
        }
        public void RunCS(Mat src)
        {
            slice.Run(src);
            dst1 = slice.dst3.Clone();
            counts.Clear();
            for (int i = 0; i <= dst2.Width - 1; i++)
            {
                makeXSlice(i);
            }
            float max = counts.Max();
            int index = counts.IndexOf(max);
            dst0 = makeXSlice(index);
            dst2 = task.color.Clone();
            dst2.SetTo(Scalar.White, dst0);
            dst1.Line(new cv.Point(index, 0), new cv.Point(index, dst1.Height), Scalar.Red, slice.options.sliceSize);
            Mat hist = new Mat(dst0.Width, 1, MatType.CV_32F, counts.ToArray());
            plot.Run(hist);
            dst3 = plot.dst2;
        }
    }

    public class CS_Structured_FeatureLines : CS_Parent
    {
        Structured_MultiSlice mStruct = new Structured_MultiSlice();
        FeatureLine_Finder lines = new FeatureLine_Finder();
        public CS_Structured_FeatureLines(VBtask task) : base(task)
        {
            desc = "Find the lines in the Structured_MultiSlice algorithm output";
        }
        public void RunCS(Mat src)
        {
            mStruct.Run(src);
            dst2 = mStruct.dst2;
            lines.Run(mStruct.dst2);
            dst3 = src.Clone();
            for (int i = 0; i <= lines.lines2D.Count() - 1; i += 2)
            {
                cv.Point2f p1 = lines.lines2D[i];
                cv.Point2f p2 = lines.lines2D[i + 1];
                DrawLine(dst3, p1, p2, Scalar.Yellow, task.lineWidth);
            }
        }
    }
    public class CS_Structured_FloorCeiling : CS_Parent
    {
        public Structured_SliceEither slice = new Structured_SliceEither();
        Kalman_Basics kalman = new Kalman_Basics();
        public CS_Structured_FloorCeiling(VBtask task) : base(task)
        {
            Array.Resize(ref kalman.kInput, 2);
            FindCheckBox("Top View (Unchecked Side View)").Checked = false;
            desc = "Find the floor or ceiling plane";
        }
        public void RunCS(Mat src)
        {
            slice.Run(src);
            dst2 = slice.heat.dst3;
            double floorMax = 0;
            int floorY = 0;
            int floorBuffer = dst2.Height / 4;
            for (int i = dst2.Height - 1; i >= 0; i--)
            {
                double nextSum = slice.heat.dst3.Row(i).Sum()[0];
                if (nextSum > 0) floorBuffer -= 1;
                if (floorBuffer == 0) break;
                if (nextSum > floorMax)
                {
                    floorMax = nextSum;
                    floorY = i;
                }
            }
            double ceilingMax = 0;
            int ceilingY = 0;
            int ceilingBuffer = dst2.Height / 4;
            for (int i = 0; i < dst3.Height; i++)
            {
                double nextSum = slice.heat.dst3.Row(i).Sum()[0];
                if (nextSum > 0) ceilingBuffer -= 1;
                if (ceilingBuffer == 0) break;
                if (nextSum > ceilingMax)
                {
                    ceilingMax = nextSum;
                    ceilingY = i;
                }
            }
            kalman.kInput[0] = floorY;
            kalman.kInput[1] = ceilingY;
            kalman.Run(src);
            labels[2] = "Current slice is at row =" + task.mouseMovePoint.Y.ToString();
            labels[3] = "Ceiling is at row =" + ((int)kalman.kOutput[1]).ToString() + " floor at y=" + ((int)kalman.kOutput[0]).ToString();
            DrawLine(dst2, new cv.Point(0, floorY), new cv.Point(dst2.Width, floorY), Scalar.Yellow);
            SetTrueText("floor", new cv.Point(10, floorY + task.DotSize), 3);
            cv.Rect rect = new cv.Rect(0, Math.Max(ceilingY - 5, 0), dst2.Width, 10);
            Mat mask = slice.heat.dst3[rect];
            Scalar mean, stdev;
            Cv2.MeanStdDev(mask, out mean, out stdev);
            if (mean[0] < mean[2])
            {
                DrawLine(dst2, new cv.Point(0, ceilingY), new cv.Point(dst2.Width, ceilingY), Scalar.Yellow);
                SetTrueText("ceiling", new cv.Point(10, ceilingY + task.DotSize), 3);
            }
            else
            {
                SetTrueText("Ceiling does not appear to be present", 3);
            }
        }
    }
    public class CS_Structured_MultiSliceH : CS_Parent
    {
        public HeatMap_Basics heat = new HeatMap_Basics();
        public Mat sliceMask;
        Options_Structured options = new Options_Structured();
        public CS_Structured_MultiSliceH(VBtask task) : base(task)
        {
            FindCheckBox("Top View (Unchecked Side View)").Checked = false;
            desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            int stepsize = options.stepSize;
            heat.Run(src);
            dst3 = heat.dst3;
            sliceMask = new Mat(dst2.Size(), MatType.CV_8U, 0);
            for (int yCoordinate = 0; yCoordinate <= src.Height - 1; yCoordinate += stepsize)
            {
                double planeY = -task.yRange * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y;
                if (yCoordinate > task.sideCameraPoint.Y) planeY = task.yRange * (yCoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y);
                Mat depthMask = new Mat();
                double minVal, maxVal;
                minVal = planeY - task.metersPerPixel;
                maxVal = planeY + task.metersPerPixel;
                Cv2.InRange(task.pcSplit[1].Clone(), minVal, maxVal, depthMask);
                sliceMask.SetTo(255, depthMask);
                if (minVal < 0 && maxVal > 0) sliceMask.SetTo(0, task.noDepthMask);
            }
            dst2 = task.color.Clone();
            dst2.SetTo(Scalar.White, sliceMask);
            labels[3] = heat.labels[3];
        }
    }
    public class CS_Structured_MultiSliceV : CS_Parent
    {
        public HeatMap_Basics heat = new HeatMap_Basics();
        Options_Structured options = new Options_Structured();
        public CS_Structured_MultiSliceV(VBtask task) : base(task)
        {
            FindCheckBox("Top View (Unchecked Side View)").Checked = true;
            desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            int stepsize = options.stepSize;
            heat.Run(src);
            dst3 = heat.dst2;
            Mat sliceMask = new Mat(dst2.Size(), MatType.CV_8U, 0);
            for (int xCoordinate = 0; xCoordinate <= src.Width - 1; xCoordinate += stepsize)
            {
                double planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X;
                if (xCoordinate > task.topCameraPoint.X) planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X);
                Mat depthMask = new Mat();
                double minVal, maxVal;
                minVal = planeX - task.metersPerPixel;
                maxVal = planeX + task.metersPerPixel;
                Cv2.InRange(task.pcSplit[0].Clone(), minVal, maxVal, depthMask);
                sliceMask.SetTo(255, depthMask);
                if (minVal < 0 && maxVal > 0) sliceMask.SetTo(0, task.noDepthMask);
            }
            dst2 = task.color.Clone();
            dst2.SetTo(Scalar.White, sliceMask);
            labels[3] = heat.labels[3];
        }
    }
    public class CS_Structured_SliceXPlot : CS_Parent
    {
        Structured_MultiSlice multi = new Structured_MultiSlice();
        Options_Structured options = new Options_Structured();
        public CS_Structured_SliceXPlot(VBtask task) : base(task)
        {
            desc = "Find any plane around a peak value in the top-down histogram";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            multi.Run(src);
            dst3 = multi.heat.dst2;
            int col = (task.mouseMovePoint.X == 0) ? dst2.Width / 2 : task.mouseMovePoint.X;
            cv.Rect rect = new cv.Rect(col, 0, (col + options.sliceSize >= dst3.Width) ? dst3.Width - col : options.sliceSize, dst3.Height - 1);
            mmData mm = GetMinMax(multi.heat.topframes.dst2[rect]);
            DrawCircle(dst3, new cv.Point(col, mm.maxLoc.Y), task.DotSize + 3, Scalar.Yellow);
            dst2 = task.color.Clone();
            double filterZ = (dst3.Height - mm.maxLoc.Y) / dst3.Height * task.MaxZmeters;
            if (filterZ > 0)
            {
                Mat depthMask = multi.split[2].InRange(filterZ - 0.05, filterZ + 0.05); // a 10 cm buffer surrounding the z value
                depthMask = multi.sliceMask & depthMask;
                dst2.SetTo(Scalar.White, depthMask);
            }
            labels[3] = "Peak histogram count (" + mm.maxVal.ToString("F0") + ") at " + filterZ.ToString("F2") + " meters +-" + (5 / dst2.Height / task.MaxZmeters).ToString("F2") + " m";
            SetTrueText("Use the mouse to move the yellow dot above.", new cv.Point(10, dst2.Height * 7 / 8), 3);
        }
    }
    public class CS_Structured_SliceYPlot : CS_Parent
    {
        Structured_MultiSlice multi = new Structured_MultiSlice();
        Options_Structured options = new Options_Structured();
        public CS_Structured_SliceYPlot(VBtask task) : base(task)
        {
            desc = "Find any plane around a peak value in the side view histogram";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            multi.Run(src);
            dst3 = multi.heat.dst3;
            int row = task.mouseMovePoint.Y == 0 ? dst2.Height / 2 : task.mouseMovePoint.Y;
            cv.Rect rect = new cv.Rect(0, row, dst3.Width - 1, row + options.sliceSize >= dst3.Height ? dst3.Height - row : options.sliceSize);
            mmData mm = GetMinMax(multi.heat.sideframes.dst2[rect]);
            if (mm.maxVal > 0)
            {
                DrawCircle(dst3, new cv.Point(mm.maxLoc.X, row), task.DotSize + 3, Scalar.Yellow);
                // dst3.Line(new cv.Point(mm.maxLoc.X, 0), new cv.Point(mm.maxLoc.X, dst3.Height), task.HighlightColor, task.lineWidth, task.lineType);
                double filterZ = mm.maxLoc.X / (double)dst3.Width * task.MaxZmeters;
                Mat depthMask = multi.split[2].InRange(filterZ - 0.05, filterZ + 0.05); // a 10 cm buffer surrounding the z value
                dst2 = task.color.Clone();
                dst2.SetTo(Scalar.White, depthMask);
                double pixelsPerMeter = (double)dst2.Width / task.MaxZmeters;
                labels[3] = $"Peak histogram count ({mm.maxVal.ToString(fmt0)}) at {filterZ.ToString(fmt2)} meters ±{(5 / pixelsPerMeter).ToString(fmt2)} m";
            }
            SetTrueText("Use the mouse to move the yellow dot above.", new cv.Point(10, dst2.Height * 7 / 8), 3);
        }
    }
    public class CS_Structured_MouseSlice : CS_Parent
    {
        Structured_SliceEither slice = new Structured_SliceEither();
        Line_Basics lines = new Line_Basics();
        public CS_Structured_MouseSlice(VBtask task) : base(task)
        {
            labels[2] = "Center Slice in yellow";
            labels[3] = "White = SliceV output, Red Dot is avgPt";
            desc = "Find the vertical center line with accurate depth data.";
        }
        public void RunCS(Mat src)
        {
            if (task.mouseMovePoint == new cv.Point()) task.mouseMovePoint = new cv.Point(dst2.Width / 2, dst2.Height);
            slice.Run(src);
            lines.Run(slice.sliceMask);
            List<int> tops = new List<int>();
            List<int> bots = new List<int>();
            List<cv.Point> topsList = new List<cv.Point>();
            List<cv.Point> botsList = new List<cv.Point>();
            if (lines.lpList.Count() > 0)
            {
                dst3 = lines.dst2;
                foreach (var lp in lines.lpList)
                {
                    DrawLine(dst3, lp.p1, lp.p2, task.HighlightColor, task.lineWidth + 3);
                    if (lp.p1.Y < lp.p2.Y) tops.Add((int)lp.p1.Y); else tops.Add((int)lp.p2.Y);
                    if (lp.p1.Y > lp.p2.Y) bots.Add((int)lp.p1.Y); else bots.Add((int)lp.p2.Y);
                    topsList.Add(new cv.Point(lp.p1.X, lp.p1.Y));
                    botsList.Add(new cv.Point(lp.p2.X, lp.p2.Y));
                }
            }
            if (standaloneTest())
            {
                dst2 = src;
                dst2.SetTo(Scalar.White, dst3);
            }
        }
    }
    public class CS_Structured_SliceEither : CS_Parent
    {
        public HeatMap_Basics heat = new HeatMap_Basics();
        public Mat sliceMask = new Mat();
        Options_Structured options = new Options_Structured();
        public CS_Structured_SliceEither(VBtask task) : base(task)
        {
            FindCheckBox("Top View (Unchecked Side View)").Checked = false;
            desc = "Create slices in top and side views";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            bool topView = FindCheckBox("Top View (Unchecked Side View)").Checked;
            int sliceVal = topView ? task.mouseMovePoint.X : task.mouseMovePoint.Y;
            heat.Run(src);
            double minVal, maxVal;
            if (topView)
            {
                double planeX = -task.xRange * (task.topCameraPoint.X - sliceVal) / task.topCameraPoint.X;
                if (sliceVal > task.topCameraPoint.X) planeX = task.xRange * (sliceVal - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X);
                minVal = planeX - task.metersPerPixel;
                maxVal = planeX + task.metersPerPixel;
                sliceMask = task.pcSplit[0].InRange(minVal, maxVal);
            }
            else
            {
                double planeY = -task.yRange * (task.sideCameraPoint.Y - sliceVal) / task.sideCameraPoint.Y;
                if (sliceVal > task.sideCameraPoint.Y) planeY = task.yRange * (sliceVal - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y);
                minVal = planeY - task.metersPerPixel;
                maxVal = planeY + task.metersPerPixel;
                sliceMask = task.pcSplit[1].InRange(minVal, maxVal);
            }
            if (minVal < 0 && maxVal > 0) sliceMask.SetTo(0, task.noDepthMask);
            double w = Math.Abs(maxVal - minVal) * 100;
            labels[2] = $"At offset {sliceVal} x = {((maxVal + minVal) / 2).ToString(fmt2)} with {w.ToString(fmt2)} cm width";
            labels[3] = heat.labels[3];
            dst3 = heat.dst3;
            DrawCircle(dst3, new cv.Point(task.topCameraPoint.X, dst3.Height), task.DotSize, Scalar.Yellow);
            if (topView)
            {
                dst3.Line(new cv.Point(sliceVal, 0), new cv.Point(sliceVal, dst3.Height), Scalar.Yellow, task.lineWidth);
            }
            else
            {
                int yPlaneOffset = sliceVal < dst3.Height - options.sliceSize ? sliceVal : dst3.Height - options.sliceSize - 1;
                dst3.Line(new cv.Point(0, yPlaneOffset), new cv.Point(dst3.Width, yPlaneOffset), Scalar.Yellow, options.sliceSize);
            }
            if (standaloneTest())
            {
                dst2 = src;
                dst2.SetTo(Scalar.White, sliceMask);
            }
        }
    }
    public class CS_Structured_TransformH : CS_Parent
    {
        Options_Structured options = new Options_Structured();
        Projection_HistTop histTop = new Projection_HistTop();
        public CS_Structured_TransformH(VBtask task) : base(task)
        {
            labels[3] = "Top down view of the slice of the point cloud";
            desc = "Find and isolate planes (floor and ceiling) in a TopView or SideView histogram.";
        }
        public Mat createSliceMaskH()
        {
            options.RunVB();
            Mat sliceMask = new Mat();
            int ycoordinate = task.mouseMovePoint.Y == 0 ? dst2.Height / 2 : task.mouseMovePoint.Y;
            double planeY = -task.yRange * (task.sideCameraPoint.Y - ycoordinate) / task.sideCameraPoint.Y;
            if (ycoordinate > task.sideCameraPoint.Y) planeY = task.yRange * (ycoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y);
            double thicknessMeters = options.sliceSize * task.metersPerPixel;
            double minVal = planeY - thicknessMeters;
            double maxVal = planeY + thicknessMeters;
            Cv2.InRange(task.pcSplit[1], minVal, maxVal, sliceMask);
            double w = Math.Abs(maxVal - minVal) * 100;
            labels[2] = $"At offset {ycoordinate} y = {((maxVal + minVal) / 2).ToString(fmt2)} with {w.ToString(fmt2)} cm width";
            if (minVal < 0 && maxVal > 0) sliceMask.SetTo(0, task.noDepthMask);
            return sliceMask;
        }
        public void RunCS(Mat src)
        {
            Mat sliceMask = createSliceMaskH();
            histTop.Run(task.pointCloud.SetTo(0, ~sliceMask));
            dst3 = histTop.dst2;
            if (standaloneTest())
            {
                dst2 = src;
                dst2.SetTo(Scalar.White, sliceMask);
            }
        }
    }
    public class CS_Structured_TransformV : CS_Parent
    {
        Options_Structured options = new Options_Structured();
        Projection_HistSide histSide = new Projection_HistSide();
        public CS_Structured_TransformV(VBtask task) : base(task)
        {
            labels[3] = "Side view of the slice of the point cloud";
            desc = "Find and isolate planes using the top view histogram data";
        }
        public Mat createSliceMaskV()
        {
            options.RunVB();
            Mat sliceMask = new Mat();
            if (task.mouseMovePoint == new cv.Point()) task.mouseMovePoint = new cv.Point(dst2.Width / 2, dst2.Height);
            int xCoordinate = task.mouseMovePoint.X == 0 ? dst2.Width / 2 : task.mouseMovePoint.X;
            double planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X;
            if (xCoordinate > task.topCameraPoint.X) planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X);
            double thicknessMeters = options.sliceSize * task.metersPerPixel;
            double minVal = planeX - thicknessMeters;
            double maxVal = planeX + thicknessMeters;
            Cv2.InRange(task.pcSplit[0], minVal, maxVal, sliceMask);
            double w = Math.Abs(maxVal - minVal) * 100;
            labels[2] = $"At offset {xCoordinate} x = {((maxVal + minVal) / 2).ToString(fmt2)} with {w.ToString(fmt2)} cm width";
            if (minVal < 0 && maxVal > 0) sliceMask.SetTo(0, task.noDepthMask);
            return sliceMask;
        }
        public void RunCS(Mat src)
        {
            Mat sliceMask = createSliceMaskV();
            histSide.Run(task.pointCloud.SetTo(0, ~sliceMask));
            dst3 = histSide.dst2;
            if (standaloneTest())
            {
                dst2 = src;
                dst2.SetTo(Scalar.White, sliceMask);
            }
        }
    }
    public class CS_Structured_CountSide : CS_Parent
    {
        Structured_SliceH slice = new Structured_SliceH();
        Plot_Histogram plot = new Plot_Histogram();
        Rotate_Basics rotate = new Rotate_Basics();
        public List<float> counts = new List<float>();
        public int maxCountIndex;
        public List<float> yValues = new List<float>();
        public CS_Structured_CountSide(VBtask task) : base(task)
        {
            rotate.rotateCenter = new cv.Point((int)(dst2.Width / 2), (int)(dst2.Width / 2));
            rotate.rotateAngle = -90;
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "Max Slice output - likely flat surface", "Structured Slice heatmap input - red line is max", "Histogram of pixel counts in each slice" };
            desc = "Count the number of pixels found in each slice of the point cloud data.";
        }
        public void RunCS(Mat src)
        {
            slice.Run(src);
            dst2 = slice.dst3;
            counts.Clear();
            yValues.Clear();
            for (int i = 0; i <= dst2.Height - 1; i++)
            {
                float planeY = task.yRange * (i - task.sideCameraPoint.Y) / task.sideCameraPoint.Y;
                float minVal = planeY - task.metersPerPixel, maxVal = planeY + task.metersPerPixel;
                Mat sliceMask = task.pcSplit[1].InRange(minVal, maxVal);
                if (minVal < 0 && maxVal > 0) sliceMask.SetTo(0, task.noDepthMask); // don't include zero depth locations
                counts.Add(sliceMask.CountNonZero());
                yValues.Add(planeY);
            }
            float max = counts.Max();
            maxCountIndex = counts.IndexOf(max);
            dst2.Line(new cv.Point(0, maxCountIndex), new cv.Point(dst2.Width, maxCountIndex), Scalar.Red, slice.options.sliceSize);
            Mat hist = new Mat(dst0.Height, 1, MatType.CV_32F, counts.ToArray());
            plot.dst2 = new Mat(dst2.Height, dst2.Height, MatType.CV_8UC3, 0);
            plot.Run(hist);
            dst3 = plot.dst2;
            dst3 = dst3.Resize(new cv.Size(dst2.Width, dst2.Width));
            rotate.Run(dst3);
            dst3 = rotate.dst2;
            SetTrueText("Max flat surface at: " + "\n" + string.Format(fmt3, yValues[maxCountIndex]), 2);
        }
    }
    public class CS_Structured_CountSideSum : CS_Parent
    {
        public List<float> counts = new List<float>();
        public int maxCountIndex;
        public List<float> yValues = new List<float>();
        public CS_Structured_CountSideSum(VBtask task) : base(task)
        {
            task.redOptions.setProjection(task.redOptions.getProjection() + 50); // to get the point cloud into the histogram.
            labels = new string[] { "", "Max Slice output - likely flat surface", "Structured Slice heatmap input - red line is max", "Histogram of pixel counts in each slice" };
            desc = "Count the number of points found in each slice of the point cloud data.";
        }
        public void RunCS(Mat src)
        {
            Cv2.CalcHist(new Mat[] { task.pointCloud }, task.channelsSide, new Mat(), dst2, 2, task.bins2D, task.rangesSide);
            dst2.Col(0).SetTo(0);
            counts.Clear();
            yValues.Clear();
            float ratio = task.yRange / task.yRangeDefault;
            for (int i = 0; i <= dst2.Height - 1; i++)
            {
                float planeY = task.yRange * (i - task.sideCameraPoint.Y) / task.sideCameraPoint.Y;
                counts.Add((float)dst2.Row(i).Sum()[0]);
                yValues.Add(planeY * ratio);
            }
            dst2 = dst2.Threshold(0, 255, ThresholdTypes.Binary);
            float max = counts.Max();
            if (max == 0) return;
            List<float> surfaces = new List<float>();
            for (int i = 0; i < counts.Count(); i++)
            {
                if (counts[i] >= max / 2)
                {
                    DrawLine(dst2, new cv.Point(0, i), new cv.Point(dst2.Width, i), Scalar.White);
                    surfaces.Add(yValues[i]);
                }
            }
            if (task.heartBeat)
            {
                strOut = "Flat surface at: ";
                for (int i = 0; i < surfaces.Count(); i++)
                {
                    strOut += string.Format(fmt3, surfaces[i]) + ", ";
                    if (i % 10 == 0 && i > 0) strOut += "\n";
                }
            }
            SetTrueText(strOut, 2);
            dst3.SetTo(Scalar.Red);
            float barHeight = dst2.Height / counts.Count();
            for (int i = 0; i < counts.Count(); i++)
            {
                float w = dst2.Width * counts[i] / max;
                Cv2.Rectangle(dst3, new cv.Rect(0, (int)(i * barHeight), (int)w, (int)barHeight), Scalar.Black, -1);
            }
        }
    }
    public class CS_Structured_SliceV : CS_Parent
    {
        public HeatMap_Basics heat = new HeatMap_Basics();
        public Mat sliceMask = new Mat();
        public Options_Structured options = new Options_Structured();
        public CS_Structured_SliceV(VBtask task) : base(task)
        {
            FindCheckBox("Top View (Unchecked Side View)").Checked = true;
            desc = "Find and isolate planes using the top view histogram data";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.mouseMovePoint == new cv.Point()) task.mouseMovePoint = new cv.Point(dst2.Width / 2, dst2.Height);
            int xCoordinate = (task.mouseMovePoint.X == 0) ? dst2.Width / 2 : task.mouseMovePoint.X;
            heat.Run(src);
            float planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X;
            if (xCoordinate > task.topCameraPoint.X) planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X);
            float thicknessMeters = options.sliceSize * task.metersPerPixel;
            float minVal = planeX - thicknessMeters;
            float maxVal = planeX + thicknessMeters;
            Cv2.InRange(task.pcSplit[0], minVal, maxVal, sliceMask);
            if (minVal < 0 && maxVal > 0) sliceMask.SetTo(0, task.noDepthMask);
            labels[2] = "At offset " + xCoordinate + " x = " + string.Format(fmt2, (maxVal + minVal) / 2) +
                        " with " + string.Format(fmt2, Math.Abs(maxVal - minVal) * 100) + " cm width";
            labels[3] = heat.labels[3];
            dst3 = heat.dst2;
            DrawCircle(dst3, new cv.Point(task.topCameraPoint.X, 0), task.DotSize, task.HighlightColor);
            dst3.Line(new cv.Point(xCoordinate, 0), new cv.Point(xCoordinate, dst3.Height), task.HighlightColor, options.sliceSize);
            if (standaloneTest())
            {
                dst2 = src;
                dst2.SetTo(Scalar.White, sliceMask);
            }
        }
    }
    public class CS_Structured_SliceH : CS_Parent
    {
        public HeatMap_Basics heat = new HeatMap_Basics();
        public Mat sliceMask = new Mat();
        public Options_Structured options = new Options_Structured();
        public int ycoordinate;
        public CS_Structured_SliceH(VBtask task) : base(task)
        {
            desc = "Find and isolate planes (floor and ceiling) in a TopView or SideView histogram.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            heat.Run(src);
            if (standaloneTest()) ycoordinate = (task.mouseMovePoint.Y == 0) ? dst2.Height / 2 : task.mouseMovePoint.Y;
            float sliceY = -task.yRange * (task.sideCameraPoint.Y - ycoordinate) / task.sideCameraPoint.Y;
            if (ycoordinate > task.sideCameraPoint.Y) sliceY = task.yRange * (ycoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y);
            float thicknessMeters = options.sliceSize * task.metersPerPixel;
            float minVal = sliceY - thicknessMeters;
            float maxVal = sliceY + thicknessMeters;
            Cv2.InRange(task.pcSplit[1], minVal, maxVal, sliceMask);
            labels[2] = "At offset " + ycoordinate + " y = " + string.Format(fmt2, (maxVal + minVal) / 2) +
                        " with " + string.Format(fmt2, Math.Abs(maxVal - minVal) * 100) + " cm width";
            if (minVal <= 0 && maxVal >= 0) sliceMask.SetTo(0, task.noDepthMask);
            labels[3] = heat.labels[2];
            dst3 = heat.dst3;
            int yPlaneOffset = (ycoordinate < dst3.Height - options.sliceSize) ? ycoordinate : dst3.Height - options.sliceSize - 1;
            DrawCircle(dst3, new cv.Point(0, task.sideCameraPoint.Y), task.DotSize, task.HighlightColor);
            dst3.Line(new cv.Point(0, yPlaneOffset), new cv.Point(dst3.Width, yPlaneOffset), task.HighlightColor, options.sliceSize);
            if (standaloneTest())
            {
                dst2 = src;
                dst2.SetTo(Scalar.White, sliceMask);
            }
        }
    }
    public class CS_Structured_SurveyH : CS_Parent
    {
        public CS_Structured_SurveyH(VBtask task) : base(task)
        {
            task.redOptions.setYRangeSlider(300);
            UpdateAdvice(traceName + ": use Y-Range slider in RedCloud options.");
            labels[2] = "Each slice represents point cloud pixels with the same Y-Range";
            labels[3] = "Y-Range - compressed to increase the size of each slice.  Use Y-range slider to adjust the size of each slice.";
            desc = "Mark each horizontal slice with a separate color.  Y-Range determines how thick the slice is.";
        }
        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32FC3) src = task.pointCloud;
            Cv2.CalcHist(new Mat[] { src }, task.channelsSide, new Mat(), dst3, 2, task.bins2D, task.rangesSide);
            dst3.Col(0).SetTo(0);
            dst3 = dst3.Threshold(0, 255, ThresholdTypes.Binary);
            dst3.ConvertTo(dst3, MatType.CV_8U);
            int topRow;
            for (topRow = 0; topRow <= dst2.Height - 1; topRow++)
            {
                if (dst3.Row(topRow).CountNonZero() > 0) break;
            }
            int botRow;
            for (botRow = dst2.Height - 1; botRow >= 0; botRow--)
            {
                if (dst3.Row(botRow).CountNonZero() > 0) break;
            }
            int index = 0;
            dst2.SetTo(0);
            for (int y = topRow; y <= botRow; y++)
            {
                float sliceY = -task.yRange * (task.sideCameraPoint.Y - y) / task.sideCameraPoint.Y;
                if (y > task.sideCameraPoint.Y) sliceY = task.yRange * (y - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y);
                float minVal = sliceY - task.metersPerPixel;
                float maxVal = sliceY + task.metersPerPixel;
                if (minVal < 0 && maxVal > 0) continue;
                dst0 = task.pcSplit[1].InRange(minVal, maxVal);
                dst2.SetTo(task.scalarColors[index % 256], dst0);
                index++;
            }
        }
    }
    public class CS_Structured_SurveyV : CS_Parent
    {
        public CS_Structured_SurveyV(VBtask task) : base(task)
        {
            task.redOptions.setXRangeSlider(250);
            UpdateAdvice(traceName + ": use X-Range slider in RedCloud options.");
            labels[2] = "Each slice represents point cloud pixels with the same X-Range";
            labels[3] = "X-Range - compressed to increase the size of each slice.  Use X-range slider to adjust the size of each slice.";
            desc = "Mark each vertical slice with a separate color.  X-Range determines how thick the slice is.";
        }
        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32FC3) src = task.pointCloud;
            Cv2.CalcHist(new Mat[] { src }, task.channelsTop, new Mat(), dst3, 2, task.bins2D, task.rangesTop);
            dst3.Row(0).SetTo(0);
            dst3 = dst3.Threshold(0, 255, ThresholdTypes.Binary);
            dst3.ConvertTo(dst3, MatType.CV_8U);
            int column;
            for (column = 0; column < dst2.Width; column++)
            {
                if (dst3.Col(column).CountNonZero() > 0) break;
            }
            int lastColumn;
            for (lastColumn = dst2.Width - 1; lastColumn >= 0; lastColumn--)
            {
                if (dst3.Col(lastColumn).CountNonZero() > 0) break;
            }
            int index = 0;
            dst2.SetTo(0);
            for (int x = column; x <= lastColumn; x++)
            {
                float sliceX = -task.xRange * (task.topCameraPoint.X - x) / task.topCameraPoint.X;
                if (x > task.topCameraPoint.X) sliceX = task.xRange * (x - task.topCameraPoint.X) / (dst3.Height - task.topCameraPoint.X);
                float minVal = sliceX - task.metersPerPixel;
                float maxVal = sliceX + task.metersPerPixel;
                if (minVal < 0 && maxVal > 0) continue;
                dst0 = task.pcSplit[0].InRange(minVal, maxVal);
                dst2.SetTo(task.scalarColors[index % 256], dst0);
                index++;
            }
        }
    }
    public class CS_Structured_MultiSlicePolygon : CS_Parent
    {
        Structured_MultiSlice multi = new Structured_MultiSlice();
        Options_StructuredMulti options = new Options_StructuredMulti();
        public CS_Structured_MultiSlicePolygon(VBtask task) : base(task)
        {
            labels[2] = "Input to FindContours";
            labels[3] = "ApproxPolyDP 4-corner object from FindContours input";
            desc = "Detect polygons in the multiSlice output";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            multi.Run(src);
            dst2 = ~multi.dst3;
            cv.Point[][] rawContours = Cv2.FindContoursAsArray(dst2, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
            cv.Point[][] contours = new cv.Point[rawContours.Length][];
            for (int j = 0; j < rawContours.Length; j++)
            {
                contours[j] = Cv2.ApproxPolyDP(rawContours[j], 3, true);
            }
            dst3.SetTo(0);
            for (int i = 0; i < contours.Length; i++)
            {
                if (contours[i].Length == 2) continue;
                if (contours[i].Length <= options.maxSides)
                {
                    Cv2.DrawContours(dst3, contours, i, new Scalar(0, 255, 255), task.lineWidth + 1, task.lineType);
                }
            }
        }
    }
    public class CS_Structured_Crosshairs : CS_Parent
    {
        Structured_Cloud sCloud = new Structured_Cloud();
        double minX, maxX, minY, maxY;
        public CS_Structured_Crosshairs(VBtask task) : base(task)
        {
            desc = "Connect vertical and horizontal dots that are in the same column and row.";
        }
        public void RunCS(Mat src)
        {
            int xLines = sCloud.options.indexX;
            int yLines = (int)(xLines * dst2.Width / dst2.Height);
            if (sCloud.options.indexX > xLines) sCloud.options.indexX = xLines - 1;
            if (sCloud.options.indexY > yLines) sCloud.options.indexY = yLines - 1;
            sCloud.Run(src);
            Mat[] split = Cv2.Split(sCloud.dst2);
            var mmX = GetMinMax(split[0]);
            var mmY = GetMinMax(split[1]);
            minX = Math.Min(minX, mmX.minVal);
            minY = Math.Min(minY, mmY.minVal);
            maxX = Math.Max(maxX, mmX.maxVal);
            maxY = Math.Max(maxY, mmY.maxVal);
            SetTrueText("mmx min/max = " + minX.ToString("0.00") + "/" + maxX.ToString("0.00") + " mmy min/max " + minY.ToString("0.00") +
                        "/" + maxY.ToString("0.00"), 3);
            dst2.SetTo(0);
            Vec3b white = new Vec3b(255, 255, 255);
            Mat pointX = new Mat(sCloud.dst2.Size(), MatType.CV_32S, 0);
            Mat pointY = new Mat(sCloud.dst2.Size(), MatType.CV_32S, 0);
            int yy, xx;
            for (int y = 1; y < sCloud.dst2.Height - 1; y++)
            {
                for (int x = 1; x < sCloud.dst2.Width - 1; x++)
                {
                    Vec3f p = sCloud.dst2.Get<Vec3f>(y, x);
                    if (p[2] > 0)
                    {
                        if (float.IsNaN(p[0]) || float.IsNaN(p[1]) || float.IsNaN(p[2])) continue;
                        xx = (int)(dst2.Width * (maxX - p[0]) / (maxX - minX));
                        yy = (int)(dst2.Height * (maxY - p[1]) / (maxY - minY));
                        if (xx < 0) xx = 0;
                        if (yy < 0) yy = 0;
                        if (xx >= dst2.Width) xx = dst2.Width - 1;
                        if (yy >= dst2.Height) yy = dst2.Height - 1;
                        yy = dst2.Height - yy - 1;
                        xx = dst2.Width - xx - 1;
                        dst2.Set<Vec3b>(yy, xx, white);
                        pointX.Set<int>(y, x, xx);
                        pointY.Set<int>(y, x, yy);
                        if (x == sCloud.options.indexX)
                        {
                            cv.Point p1 = new cv.Point(pointX.Get<int>(y - 1, x), pointY.Get<int>(y - 1, x));
                            if (p1.X > 0)
                            {
                                cv.Point p2 = new cv.Point(xx, yy);
                                dst2.Line(p1, p2, task.HighlightColor, task.lineWidth + 1, task.lineType);
                            }
                        }
                        if (y == sCloud.options.indexY)
                        {
                            cv.Point p1 = new cv.Point(pointX.Get<int>(y, x - 1), pointY.Get<int>(y, x - 1));
                            if (p1.X > 0)
                            {
                                cv.Point p2 = new cv.Point(xx, yy);
                                dst2.Line(p1, p2, task.HighlightColor, task.lineWidth + 1, task.lineType);
                            }
                        }
                    }
                }
            }
        }
    }




}
