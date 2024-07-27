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

}
