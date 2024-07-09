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
using System.Drawing;
using System.Windows.Controls;

namespace CS_Classes
{
    public class CompareAllowIdenticalDoubleInverted : IComparer<double>
    {
        public int Compare(double a, double b)
        {
            // why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            if (a <= b) return 1;
            return -1;
        }
    }

    public class CompareAllowIdenticalDouble : IComparer<double>
    {
        public int Compare(double a, double b)
        {
            // why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            if (a >= b) return 1;
            return -1;
        }
    }

    public class CompareAllowIdenticalSingleInverted : IComparer<float>
    {
        public int Compare(float a, float b)
        {
            // why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            if (a <= b) return 1;
            return -1;
        }
    }

    public class CompareAllowIdenticalSingle : IComparer<float>
    {
        public int Compare(float a, float b)
        {
            // why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            if (a >= b) return 1;
            return -1;
        }
    }

    public class CompareAllowIdenticalIntegerInverted : IComparer<int>
    {
        public int Compare(int a, int b)
        {
            // why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            if (a <= b) return 1;
            return -1;
        }
    }

    public class CompareByte : IComparer<byte>
    {
        public int Compare(byte a, byte b)
        {
            if (a <= b) return -1;
            return 1;
        }
    }

    public class CompareAllowIdenticalInteger : IComparer<int>
    {
        public int Compare(int a, int b)
        {
            // why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
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

    public class CS_AddWeighted_Basics : CS_Parent
    {
        public Single weight;
        public Mat src2;
        public Options_AddWeighted options = new Options_AddWeighted();

        public CS_AddWeighted_Basics(VBtask task) : base(task) 
        {
            UpdateAdvice(traceName + ": use the local option slider 'Add Weighted %'");
            desc = "Add 2 images with specified weights.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            weight = options.addWeighted;

            Mat srcPlus = src2;
            // algorithm user normally provides src2! 
            if (standaloneTest() || src2 == null) srcPlus = task.depthRGB;
            if (srcPlus.Type() != src.Type())
            {
                if (src.Type() != MatType.CV_8UC3 || srcPlus.Type() != MatType.CV_8UC3)
                {
                    if (src.Type() == MatType.CV_32FC1) src = GetNormalize32f(src);
                    if (srcPlus.Type() == MatType.CV_32FC1) srcPlus = GetNormalize32f(srcPlus);
                    if (src.Type() != MatType.CV_8UC3) src = src.CvtColor(ColorConversionCodes.GRAY2BGR);
                    if (srcPlus.Type() != MatType.CV_8UC3) srcPlus = srcPlus.CvtColor(ColorConversionCodes.GRAY2BGR);
                }
            }
            Cv2.AddWeighted(src, weight, srcPlus, 1.0 - weight, 0, dst2);
            labels[2] = $"Depth %: {100 - weight * 100} BGR %: {(int)(weight * 100)}";
        }
    }







    public class CS_AddWeighted_Edges : CS_Parent
    {
        Edge_All edges = new Edge_All();
        CS_AddWeighted_Basics addw;

        public CS_AddWeighted_Edges(VBtask task) : base(task)
        {
            addw = new CS_AddWeighted_Basics(task);
            labels = new string[] { "", "", "Edges_BinarizedSobel output", "AddWeighted edges and BGR image" };
            desc = "Add in the edges separating light and dark to the color image";
        }

        public void RunCS(Mat src)
        {
            edges.Run(src);
            dst2 = edges.dst2;
            labels[2] = edges.labels[2];

            addw.src2 = edges.dst2.CvtColor(ColorConversionCodes.GRAY2BGR);
            addw.RunAndMeasure(src, addw);
            dst3 = addw.dst2;
        }
    }






    public class CS_AddWeighted_ImageAccumulate : CS_Parent
    {
        Options_AddWeightedAccum options = new Options_AddWeightedAccum();
        public CS_AddWeighted_ImageAccumulate(VBtask task) : base(task)
        {
            desc = "Update a running average of the image";
        }
        public void RunCS(cv.Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
            {
                dst2 = task.pcSplit[2] * 1000;
            }
            cv.Cv2.AccumulateWeighted(task.pcSplit[2] * 1000, dst2, options.addWeighted, new cv.Mat());
        }
    }







    public class CS_AddWeighted_InfraRed : CS_Parent
    {
        CS_AddWeighted_Basics addw;
        Mat src2 = new Mat();

        public CS_AddWeighted_InfraRed(VBtask task) : base(task)
        {
            addw = new CS_AddWeighted_Basics(task);
            desc = "Align the depth data with the left or right view. Oak-D is aligned with the right image. Some cameras are not close to aligned.";
        }

        public void RunCS(Mat src)
        {
            if (task.toggleOnOff)
            {
                dst1 = task.leftView;
                labels[2] = "Left view combined with depthRGB";
            }
            else
            {
                dst1 = task.rightView;
                labels[2] = "Right view combined with depthRGB";
            }

            addw.src2 = dst1;
            addw.RunAndMeasure(task.depthRGB, addw);
            dst2 = addw.dst2.Clone();
        }
    }





    public class CS_AlphaChannel_Basics : CS_Parent
    {
        Form alpha = new Form();

        public CS_AlphaChannel_Basics(VBtask task) : base(task)
        {
            alpha.Show();
            alpha.Width = dst2.Width + 10;
            alpha.Height = dst2.Height + 10;
            desc = "Use the Windows alpha channel to separate foreground and background";
        }

        public void RunCS(Mat src)
        {
            src = src.CvtColor(ColorConversionCodes.BGR2BGRA);
            Mat[] split = Cv2.Split(src);
            split[3] = task.depthMask;
            Cv2.Merge(split, src);
            alpha.BackgroundImage = BitmapConverter.ToBitmap(src, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        }
    }






// https://stackoverflow.com/questions/22132510/opencv-approxpolydp-for-edge-maps-Not-contours
// https://docs.opencv.org/4.x/js_contour_features_approxPolyDP.html
public class CS_ApproxPoly_Basics : CS_Parent
    {
        Contour_Largest contour = new Contour_Largest();
        Rectangle_Rotated rotatedRect = new Rectangle_Rotated();
        Options_ApproxPoly options = new Options_ApproxPoly();

        public CS_ApproxPoly_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Input to the ApproxPolyDP", "Output of ApproxPolyDP" };
            desc = "Using the input contours, create ApproxPoly output";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (standaloneTest())
            {
                if (task.heartBeat)
                    rotatedRect.Run(src);
                dst2 = rotatedRect.dst2;
            }

            contour.Run(dst2);
            dst2 = contour.dst2;

            if (contour.allContours.Count() > 0)
            {
                cv.Point[] nextContour;
                nextContour = Cv2.ApproxPolyDP(contour.bestContour, options.epsilon, options.closedPoly);
                dst3.SetTo(Scalar.Black);
                DrawContour(dst3, new List<cv.Point>(nextContour), Scalar.Yellow);
            }
            else
            {
                labels[2] = "No contours found";
            }
        }
    }







    public class CS_ApproxPoly_FindandDraw : CS_Parent
    {
        Rectangle_Rotated rotatedRect = new Rectangle_Rotated();
        public cv.Point[][] allContours;
        public CS_ApproxPoly_FindandDraw(VBtask task) : base(task)
        {
            labels[2] = "FindandDraw input";
            labels[3] = "FindandDraw output - note the change in line width where ApproxPoly differs from DrawContours";
            desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours.";
        }

        public void RunCS(Mat src)
        {
            rotatedRect.Run(src);
            dst2 = rotatedRect.dst2;
            dst0 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            dst0 = dst0.Threshold(1, 255, ThresholdTypes.Binary);

            dst0.ConvertTo(dst1, MatType.CV_32SC1);
            Cv2.FindContours(dst1, out allContours, out _, RetrievalModes.FloodFill, ContourApproximationModes.ApproxSimple);
            dst3.SetTo(Scalar.Black);

            List<cv.Point[]> contours = new List<cv.Point[]>();
            for (int i = 0; i < allContours.Length; i++)
            {
                cv.Point[] nextContour = Cv2.ApproxPolyDP(allContours[i], 3, true);
                if (nextContour.Length > 2)
                {
                    contours.Add(nextContour);
                }
            }
            Cv2.DrawContours(dst3, contours, -1, new Scalar(0, 255, 255), task.lineWidth, task.lineType);
        }
    }




    public class CS_ApproxPoly_Hull : CS_Parent
    {
        Hull_Basics hull = new Hull_Basics();
        ApproxPoly_Basics aPoly = new ApproxPoly_Basics();
        public CS_ApproxPoly_Hull(VBtask task) : base(task)
        {
            hull.useRandomPoints = true;
            labels = new string[] { "", "", "Original Hull", "Hull after ApproxPoly" };
            desc = "Use ApproxPolyDP on a hull to show impact of options (which appears to be minimal - what is wrong?)";
        }
        public void RunCS(Mat src)
        {
            hull.Run(src);
            dst2 = hull.dst2;

            aPoly.Run(dst2);
            dst3 = aPoly.dst2;
        }
    }




    public class CS_Area_MinTriangle_CPP : CS_Parent
    {
        public Mat triangle;
        public Options_MinArea options = new Options_MinArea();
        public List<Point2f> srcPoints;

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void MinTriangle_Run(IntPtr inputPtr, int numberOfPoints, IntPtr outputTriangle);

        public CS_Area_MinTriangle_CPP(VBtask task) : base(task)
        {
            desc = "Find minimum containing triangle for a set of points.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.heartBeat)
            {
                srcPoints = new List<Point2f>(options.srcPoints);
            }
            else
            {
                if (srcPoints.Count < 3) return; // not enough points
            }

            float[] dataSrc = new float[srcPoints.Count * Marshal.SizeOf<int>() * 2];
            float[] dstData = new float[3 * Marshal.SizeOf<int>() * 2];

            dst2.SetTo(Scalar.White);

            Mat input = new Mat(srcPoints.Count, 1, MatType.CV_32FC2, srcPoints.ToArray());
            Marshal.Copy(input.Data, dataSrc, 0, dataSrc.Length);
            GCHandle srcHandle = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            GCHandle dstHandle = GCHandle.Alloc(dstData, GCHandleType.Pinned);
            MinTriangle_Run(srcHandle.AddrOfPinnedObject(), srcPoints.Count, dstHandle.AddrOfPinnedObject());
            srcHandle.Free();
            dstHandle.Free();
            triangle = new Mat(3, 1, MatType.CV_32FC2, dstData);

            for (int i = 0; i <= 2; i++)
            {
                cv.Point2f pt = triangle.At<Point2f>(i);
                cv.Point p1 = new cv.Point(pt.X, pt.Y);
                pt = triangle.At<cv.Point2f>((i + 1) % 3);
                cv.Point p2 = new cv.Point(pt.X, pt.Y);
                DrawLine(dst2, p1, p2, Scalar.Black, task.lineWidth);
            }

            foreach (var ptSrc in srcPoints)
            {
                var pt = new cv.Point(ptSrc.X, ptSrc.Y);
                DrawCircle(dst2, pt, task.DotSize + 1, Scalar.Red);
            }
        }
    }





    public class CS_Annealing_Basics_CPP : CS_Parent
    {
        public int numberOfCities = 25;
        public Point2f[] cityPositions;
        public int[] cityOrder;
        public float energy;
        public float energyLast;
        public bool circularPattern = true;

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Annealing_Basics_Open(IntPtr cityPositions, int numberOfCities);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Annealing_Basics_Close(IntPtr saPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Annealing_Basics_Run(IntPtr saPtr, IntPtr cityOrder, int numberOfCities);
        public void drawMap()
        {
            dst2.SetTo(Scalar.Black);
            for (int i = 0; i < cityOrder.Length; i++)
            {
                DrawCircle(dst2, cityPositions[i], task.DotSize, Scalar.White);
                DrawLine(dst2, cityPositions[i], cityPositions[cityOrder[i]], Scalar.White, task.lineWidth);
            }
            SetTrueText("Energy" + Environment.NewLine + energy.ToString(fmt0), new cv.Point(10, 100), 2);
        }

        public void setup()
        {
            cityOrder = new int[numberOfCities];

            double radius = dst2.Rows * 0.45;
            cv.Point center = new cv.Point(dst2.Cols / 2, dst2.Rows / 2);
            if (circularPattern)
            {
                cityPositions = new cv.Point2f[numberOfCities];
                for (int i = 0; i < cityPositions.Length; i++)
                {
                    double theta = msRNG.Next(0, 360);
                    cityPositions[i].X = (float)(radius * Math.Cos(theta) + center.X);
                    cityPositions[i].Y = (float)(radius * Math.Sin(theta) + center.Y);
                    cityOrder[i] = (i + 1) % numberOfCities;
                }
            }
            for (int i = 0; i < cityOrder.Length; i++)
            {
                cityOrder[i] = (i + 1) % numberOfCities;
            }
            dst2 = new Mat(dst2.Size(), MatType.CV_8UC3, Scalar.Black);
        }

        public void Open()
        {
            GCHandle hCityPosition = GCHandle.Alloc(cityPositions, GCHandleType.Pinned);
            cPtr = Annealing_Basics_Open(hCityPosition.AddrOfPinnedObject(), numberOfCities);
            hCityPosition.Free();
        }

        public CS_Annealing_Basics_CPP(VBtask task) : base(task)
        {
            energy = -1;
            setup();
            Open();
            desc = "Simulated annealing with traveling salesman.  NOTE: No guarantee simulated annealing will find the optimal solution.";
        }

        public void RunCS(Mat src)
        {
            var saveCityOrder = (int[])cityOrder.Clone();
            GCHandle hCityOrder = GCHandle.Alloc(cityOrder, GCHandleType.Pinned);
            IntPtr outPtr = Annealing_Basics_Run(cPtr, hCityOrder.AddrOfPinnedObject(), cityPositions.Length);
            hCityOrder.Free();

            string msg = Marshal.PtrToStringAnsi(outPtr);
            string[] split = Regex.Split(msg, @"\W+");
            energy = float.Parse(split[split.Length - 2] + "." + split[split.Length - 1]);
            if (standaloneTest())
            {
                if (energyLast == energy || task.optionsChanged)
                {
                    Annealing_Basics_Close(cPtr);
                    setup();
                    Open();
                }
                energyLast = energy;
            }

            drawMap();
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = Annealing_Basics_Close(cPtr);
        }
    }





    public class CS_Random_Basics : CS_Parent
    {
        public List<cv.Point2f> pointList = new List<cv.Point2f>();
        public Rect range;
        public Options_Random options = new Options_Random();

        public CS_Random_Basics(VBtask task) : base(task)
        {
            range = new Rect(0, 0, dst2.Cols, dst2.Rows);
            desc = "Create a uniform random mask with a specified number of pixels.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            int sizeRequest = options.count;
            if (!task.paused)
            {
                pointList.Clear();
                Random msRNG = new Random();
                while (pointList.Count < sizeRequest)
                {
                    pointList.Add(new cv.Point2f(msRNG.Next(range.X, range.X + range.Width),
                                              msRNG.Next(range.Y, range.Y + range.Height)));
                }
                if (standaloneTest())
                {
                    dst2.SetTo(0);
                    foreach (var pt in pointList)
                    {
                        DrawCircle(dst2, pt, task.DotSize, Scalar.Yellow);
                    }
                }
            }
        }
    }







    public class CS_Annealing_MultiThreaded_CPP : CS_Parent
    {
        Options_Annealing options = new Options_Annealing();
        CS_Random_Basics random;
        CS_Annealing_Basics_CPP[] anneal;
        CS_Mat_4to1 mats;
        DateTime startTime;
        void setup()
        {
            random.options.count = options.cityCount;
            random.RunAndMeasure(empty, random); // get the city positions (may or may not be used below.)

            for (int i = 0; i < anneal.Length; i++)
            {
                anneal[i] = new CS_Annealing_Basics_CPP(task);
                anneal[i].numberOfCities = options.cityCount;
                anneal[i].cityPositions = random.pointList.ToArray();
                anneal[i].circularPattern = options.circularFlag;
                anneal[i].setup();
                anneal[i].Open(); // this will initialize the C++ copy of the city positions.
            }

            TimeSpan timeSpent = DateTime.Now.Subtract(startTime);
            if (timeSpent.TotalSeconds < 10000)
            {
                Console.WriteLine("time spent on last problem = " + timeSpent.TotalSeconds.ToString("0.00") + " seconds.");
            }
            startTime = DateTime.Now;
        }

        public CS_Annealing_MultiThreaded_CPP(VBtask task) : base(task)
        {
            mats = new CS_Mat_4to1(task);
            random = new CS_Random_Basics(task);
            anneal = new CS_Annealing_Basics_CPP[Environment.ProcessorCount / 2];
            labels = new string[] { "", "", "Top 2 are best solutions, bottom 2 are worst.", "Log of Annealing progress" };
            desc = "Setup and control finding the optimal route for a traveling salesman";
        }

        public void RunCS(cv.Mat src)
        {
            options.RunVB();

            if (task.optionsChanged) setup();

            Parallel.For(0, anneal.Length, i =>
            {
                anneal[i].RunAndMeasure(src, anneal[i]);
            });

            // find the best result and start all the others with it.
            SortedList<float, int> bestList = new SortedList<float, int>(new compareAllowIdenticalSingle());
            strOut = "";
            for (int i = 0; i < anneal.Length; i++)
            {
                bestList.Add(anneal[i].energy, i);
                if (i % 2 == 0)
                {
                    strOut += "CPU=" + i.ToString("00") + " energy=" + anneal[i].energy.ToString("0") + "\t";
                }
                else
                {
                    strOut += "CPU=" + i.ToString("00") + " energy=" + anneal[i].energy.ToString("0") + "\n";
                }
            }
            SetTrueText(strOut, new cv.Point(10, 10), 3);

            mats.mat[0] = anneal[bestList.ElementAt(0).Value].dst2;
            if (bestList.Count >= 2)
            {
                mats.mat[1] = anneal[bestList.ElementAt(1).Value].dst2;
                mats.mat[2] = anneal[bestList.ElementAt(bestList.Count - 2).Value].dst2;
                mats.mat[3] = anneal[bestList.ElementAt(bestList.Count - 1).Value].dst2;
            }
            mats.RunAndMeasure(empty, mats);
            dst2 = mats.dst2;

            // copy the top half of the solutions to the bottom half (worst solutions)
            if (options.copyBestFlag)
            {
                for (int i = 0; i < anneal.Length / 2; i++)
                {
                    anneal[bestList.ElementAt(bestList.Count - 1 - i).Value].cityOrder = anneal[bestList.ElementAt(i).Value].cityOrder;
                }
            }

            // if the top X are all the same energy, then we are done.
            int workingCount = 0, successCounter = 0;
            for (int i = 0; i < anneal.Length; i++)
            {
                int index = bestList.ElementAt(i).Value;
                if (anneal[index].energy != anneal[index].energyLast)
                {
                    anneal[index].energyLast = anneal[index].energy;
                    workingCount++;
                }
                else
                {
                    successCounter++;
                }
            }
            labels[3] = $"There are {workingCount} threads working in parallel.";
            if (successCounter >= options.successCount) setup();
        }
    }




    public class CS_Area_MinMotionRect : CS_Parent
    {
        BGSubtract_Basics bgSub = new BGSubtract_Basics();

        public CS_Area_MinMotionRect(VBtask task) : base(task)
        {
            desc = "Use minRectArea to encompass detected motion";
            labels[2] = "MinRectArea of MOG motion";
        }

        Mat motionRectangles(Mat gray, Vec3b[] colors)
        {
            cv.Point[][] contours;
            contours = Cv2.FindContoursAsArray(gray, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            for (int i = 0; i < contours.Length; i++)
            {
                RotatedRect minRect = Cv2.MinAreaRect(contours[i]);
                Scalar nextColor = new Scalar(colors[i % 256].Item0, colors[i % 256].Item1, colors[i % 256].Item2);
                DrawRotatedRectangle(minRect, gray, nextColor);
            }
            return gray;
        }

        public void RunCS(Mat src)
        {
            bgSub.Run(src);
            Mat gray;
            if (bgSub.dst2.Channels() == 1)
                gray = bgSub.dst2;
            else
                gray = bgSub.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);

            dst2 = motionRectangles(gray, task.vecColors);
            dst2.SetTo(Scalar.All(255), gray);
        }
    }





    public class CS_Area_FindNonZero : CS_Parent
    {
        public Mat nonZero;
        public CS_Area_FindNonZero(VBtask task) : base(task)
        {
            labels[2] = "Coordinates of non-zero points";
            labels[3] = "Non-zero original points";
            desc = "Use FindNonZero API to get coordinates of non-zero points.";
        }

        public void RunCS(Mat src)
        {
            if (standalone)
            {
                src = new Mat(src.Size(), MatType.CV_8U, Scalar.All(0));
                cv.Point[] srcPoints = new cv.Point[100]; // doesn't really matter how many there are.
                Random msRNG = new Random();
                for (int i = 0; i < srcPoints.Length; i++)
                {
                    srcPoints[i].X = msRNG.Next(0, src.Width);
                    srcPoints[i].Y = msRNG.Next(0, src.Height);
                    src.Set<byte>(srcPoints[i].Y, srcPoints[i].X, 255);
                }
            }
            if (src.Channels() != 1) src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            nonZero = src.FindNonZero();

            dst3 = new Mat(src.Size(), MatType.CV_8U, Scalar.All(0));
            // mark the points so they are visible...
            for (int i = 0; i < nonZero.Rows; i++)
            {
                cv.Point pt = nonZero.At<cv.Point>(i);
                Cv2.Circle(dst3, pt, task.DotSize, Scalar.White);
            }

            string outstr = "Coordinates of the non-zero points (ordered by row - top to bottom): \n\n";
            for (int i = 0; i < nonZero.Rows; i++)
            {
                cv.Point pt = nonZero.At<cv.Point>(i);
                outstr += "X = \t" + pt.X + "\t y = \t" + pt.Y + "\n";
                if (i > 100) break; // for when there are way too many points found...
            }
            SetTrueText(outstr);
        }
    }





    public class CS_Area_SoloPoints : CS_Parent
    {
        BackProject_SoloTop hotTop = new BackProject_SoloTop();
        BackProject_SoloSide hotSide = new BackProject_SoloSide();
        Area_FindNonZero nZero = new Area_FindNonZero();
        public List<cv.Point> soloPoints = new List<cv.Point>();

        public CS_Area_SoloPoints(VBtask task) : base(task)
        {
            desc = "Find the solo points in the pointcloud histograms for top and side views.";
        }

        public void RunCS(Mat src)
        {
            hotTop.Run(src);
            dst2 = hotTop.dst3;

            hotSide.Run(src);
            dst2 = dst2 | hotSide.dst3;

            nZero.Run(dst2);
            soloPoints.Clear();
            for (int i = 0; i < nZero.nonZero.Rows; i++)
            {
                soloPoints.Add(nZero.nonZero.At<cv.Point>(i, 0));
            }

            if (task.heartBeat)
            {
                labels[2] = $"There were {soloPoints.Count} points found";
            }
        }
    }




    public class CS_Area_MinRect : CS_Parent
    {
        public RotatedRect minRect;
        Options_MinArea options = new Options_MinArea();
        public List<Point2f> inputPoints = new List<Point2f>();

        public CS_Area_MinRect(VBtask task) : base(task)
        {
            desc = "Find minimum containing rectangle for a set of points.";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                if (!task.heartBeat) return;
                options.RunVB();
                inputPoints = QuickRandomPoints(options.numPoints);
            }

            minRect = Cv2.MinAreaRect(inputPoints.ToArray());

            if (standaloneTest())
            {
                dst2.SetTo(Scalar.Black);
                foreach (var pt in inputPoints)
                {
                    DrawCircle(dst2, pt, task.DotSize + 2, Scalar.Red);
                }
                DrawRotatedOutline(minRect, dst2, Scalar.Yellow);
            }
        }
    }




    public class CS_AsciiArt_Basics : CS_Parent
    {
        string[] asciiChars = { "@", "%", "#", "*", "+", "=", "-", ":", ",", ".", " " };
        Options_AsciiArt options = new Options_AsciiArt();

        public CS_AsciiArt_Basics(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": use the local options for height and width.");
            labels = new string[] { "", "", "Ascii version", "Grayscale input to ascii art" };
            desc = "Build an ascii art representation of the input stream.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            dst3 = src.CvtColor(ColorConversionCodes.BGR2GRAY).Resize(options.size, 0, 0, InterpolationFlags.Nearest); 
            for (int y = 0; y < dst3.Height; y++)
            {
                for (int x = 0; x < dst3.Width; x++)
                {
                    byte grayValue = dst3.At<byte>(y, x);
                    string asciiChar = asciiChars[grayValue * (asciiChars.Length - 1) / 255];
                    SetTrueText(asciiChar, new cv.Point(x * options.wStep, y * options.hStep), 2);
                }
            }
            labels[2] = "Ascii version using " + (dst3.Height * dst3.Width).ToString("N0") + " characters";
        }
    }




    public class CS_AsciiArt_Color : CS_Parent
    {
        public CS_AsciiArt_Color(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, Scalar.All(0));
            desc = "A palette'd version of the ascii art data";
        }

        public void RunCS(Mat src)
        {
            int hStep = src.Height / 31 - 1;
            int wStep = src.Width / 55 - 1;
            cv.Size size = new cv.Size(55, 31);
            dst1 = src.CvtColor(ColorConversionCodes.BGR2GRAY).Resize(size, 0, 0, InterpolationFlags.Nearest);
            double grayRatio = 12.0 / 255;

            for (int y = 0; y < dst1.Height; y++)
            {
                for (int x = 0; x < dst1.Width; x++)
                {
                    Rect r = new Rect(x * wStep, y * hStep, wStep - 1, hStep - 1);
                    int asciiChar = (int)(dst1.At<byte>(y, x) * grayRatio);
                    dst3[r].SetTo(asciiChar);
                }
            }

            dst2 = ShowPalette(dst3 / grayRatio);
        }
    }




    public class CS_AsciiArt_Diff : CS_Parent
    {
        CS_AsciiArt_Color colorAA;
        CS_Diff_Basics diff;

        public CS_AsciiArt_Diff(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Ascii Art colorized", "Difference from previous frame"};
            diff = new CS_Diff_Basics(task);
            colorAA = new CS_AsciiArt_Color(task);
            desc = "Display the instability in image pixels.";
        }

        public void RunCS(Mat src)
        {
            colorAA.RunAndMeasure(src, colorAA);
            dst2 = colorAA.dst2;

            diff.RunAndMeasure(dst2.CvtColor(ColorConversionCodes.BGR2GRAY), diff);
            dst3 = diff.dst2;
        }
    }




    public class CS_BackProject_Basics : CS_Parent
    {
        public Hist_Kalman histK = new Hist_Kalman();
        public Scalar minRange, maxRange;

        public CS_BackProject_Basics(VBtask task) : base(task)
        {
            labels[2] = "Move mouse to backproject a histogram column";
            UpdateAdvice(traceName + ": the global option 'Histogram Bins' controls the histogram.");
            desc = "Mouse over any bin to see the histogram backprojected.";
        }

        public void RunCS(Mat src)
        {
            Mat input = src.Clone();
            if (input.Channels() != 1)
                input = input.CvtColor(ColorConversionCodes.BGR2GRAY);

            histK.Run(input);
            if (histK.hist.mm.minVal == histK.hist.mm.maxVal)
            {
                SetTrueText("The input image is empty - mm.minVal and mm.maxVal are both zero...");
                return;
            }

            dst2 = histK.dst2;

            long totalPixels = dst2.Total(); // assume we are including zeros.
            if (histK.hist.plot.removeZeroEntry)
                totalPixels = input.CountNonZero();

            double brickWidth = dst2.Width / task.histogramBins;
            double incr = (histK.hist.mm.maxVal - histK.hist.mm.minVal) / task.histogramBins;
            int histIndex = (int)Math.Floor(task.mouseMovePoint.X / brickWidth);

            minRange = new Scalar(histIndex * incr);
            maxRange = new Scalar((histIndex + 1) * incr);
            if (histIndex + 1 == task.histogramBins)
                maxRange = new Scalar(255);

            // For single dimension histograms, backprojection is the same as inRange (and this works for backproject_FeatureLess below)
            dst0 = input.InRange(minRange, maxRange);

            int actualCount = dst0.CountNonZero();
            dst3 = task.color.Clone();
            dst3.SetTo(Scalar.Yellow, dst0);
            float count = histK.hist.histogram.Get<float>(histIndex, 0);
            mmData histMax = GetMinMax(histK.hist.histogram);
            labels[3] = $"Backprojecting {minRange.Val0} to {maxRange.Val0} with {count} of {totalPixels} compared to " +
                        $"mask pixels = {actualCount}.  Histogram max count = {histMax.maxVal}";
            dst2.Rectangle(new Rect((int)(histIndex * brickWidth), 0, (int)brickWidth, dst2.Height), Scalar.Yellow, task.lineWidth);
        }
    }



    public class CS_BackProject_Full : CS_Parent
    {
        public int classCount;

        public CS_BackProject_Full(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "CV_8U format of the backprojection", "dst2 presented with a palette" };
            desc = "Create a color histogram, normalize it, and backproject it with a palette.";
        }

        public void RunCS(Mat src)
        {
            classCount = task.histogramBins;
            if (src.Channels() == 3)
            {
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            }
            src.ConvertTo(dst1, MatType.CV_32F);
            Mat histogram = new Mat();
            Rangef[] ranges = new Rangef[] { new Rangef(0, 255) };
            Cv2.CalcHist(new Mat[] { dst1 }, new int[] { 0 }, new Mat(), histogram, 1, new int[] { classCount }, ranges);
            histogram = histogram.Normalize(0, classCount, NormTypes.MinMax);

            Cv2.CalcBackProject(new Mat[] { dst1 }, new int[] { 0 }, histogram, dst2, ranges);

            dst2.ConvertTo(dst2, MatType.CV_8U);
            dst3 = ShowPalette(dst2 * 255 / classCount);
        }
    }




    public class CS_BackProject_Reduction : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        BackProject_Basics backP = new BackProject_Basics();

        public CS_BackProject_Reduction(VBtask task) : base(task)
        {
            task.redOptions.checkSimpleReduction(true);
            labels[3] = "Backprojection of highlighted histogram bin";
            desc = "Use the histogram of a reduced BGR image to isolate featureless portions of an image.";
        }

        public void RunCS(Mat src)
        {
            reduction.Run(src);

            backP.Run(reduction.dst2);
            dst2 = backP.dst2;
            dst3 = backP.dst3;
            int reductionValue = task.redOptions.SimpleReduction;
            labels[2] = "Reduction = " + reductionValue.ToString() + " and bins = " + task.histogramBins.ToString();
        }
    }






    public class CS_BackProject_FeatureLess : CS_Parent
    {
        BackProject_Basics backP = new BackProject_Basics();
        Reduction_Basics reduction = new Reduction_Basics();
        Edge_ColorGap_CPP edges = new Edge_ColorGap_CPP();

        public CS_BackProject_FeatureLess(VBtask task) : base(task)
        {
            task.redOptions.checkBitReduction(true);
            labels = new string[] { "", "", "Histogram of the grayscale image at right",
                                "Move mouse over the histogram to backproject a column" };
            desc = "Create a histogram of the featureless regions";
        }

        public void RunCS(Mat src)
        {
            edges.Run(src);
            reduction.Run(edges.dst3);
            backP.Run(reduction.dst2);
            dst2 = backP.dst2;
            dst3 = backP.dst3;
            int reductionValue = task.redOptions.SimpleReduction;
            labels[2] = "Reduction = " + reductionValue.ToString() + " and bins = " + task.histogramBins.ToString();
        }
    }




    public class CS_BackProject_BasicsKeyboard : CS_Parent
    {
        Keyboard_Basics keys = new Keyboard_Basics();
        BackProject_Image backP = new BackProject_Image();
        public CS_BackProject_BasicsKeyboard(VBtask task) : base(task)
        {
            labels[2] = "Move the mouse away from OpenCVB and use the left and right arrows to move between histogram bins.";
            desc = "Move the mouse off of OpenCVB and then use the left and right arrow keys move around in the backprojection histogram";
        }

        public void RunCS(Mat src)
        {
            keys.Run(src);
            List<string> keyIn = new List<string>(keys.keyInput);
            int incrX = dst1.Width / task.histogramBins;

            if (keyIn.Count > 0)
            {
                task.mouseMovePointUpdated = true;
                for (int i = 0; i < keyIn.Count; i++)
                {
                    switch (keyIn[i])
                    {
                        case "Left":
                            task.mouseMovePoint.X -= incrX;
                            break;
                        case "Right":
                            task.mouseMovePoint.X += incrX;
                            break;
                    }
                }
            }

            backP.Run(src);
            dst2 = backP.dst2;
            dst3 = backP.dst3;

            // this is intended to provide a natural behavior for the left and right arrow keys.  The Keyboard_Basics Keyboard Options text box must be active.
            if (task.heartBeat)
            {
                IntPtr hwnd = FindWindow(null, "OpenCVB Algorithm Options");
                SetForegroundWindow(hwnd);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
    }






    public class CS_BackProject_FullLines : CS_Parent
    {
        BackProject_Full backP = new BackProject_Full();
        Line_Basics lines = new Line_Basics();

        public CS_BackProject_FullLines(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Lines found in the back projection", "Backprojection results" };
            desc = "Find lines in the back projection";
        }

        public void RunCS(Mat src)
        {
            backP.Run(src);
            dst3 = backP.dst3;

            lines.Run(backP.dst2);
            dst2 = lines.dst2;
            labels[3] = lines.lpList.Count.ToString() + " lines were found";
        }
    }





    public class CS_BackProject_PointCloud : CS_Parent
    {
        public Hist_PointCloud hist = new Hist_PointCloud();
        public CS_BackProject_PointCloud(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_32FC3, Scalar.All(0));
            labels = new string[] { "", "", "Backprojection after histogram binning X and Z values", "Backprojection after histogram binning Y and Z values" };
            desc = "Explore Backprojection of the cloud histogram.";
        }

        public void RunCS(Mat src)
        {
            hist.Run(src);

            dst0 = hist.dst2.Threshold(hist.options.threshold, 255, ThresholdTypes.Binary);
            dst1 = hist.dst3.Threshold(hist.options.threshold, 255, ThresholdTypes.Binary);

            dst2 = new Mat(hist.dst2.Size(), MatType.CV_32F, Scalar.All(0));
            dst3 = new Mat(hist.dst3.Size(), MatType.CV_32F, Scalar.All(0));

            Mat mask = new Mat();
            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, new int[] { 0, 2 }, dst0, mask, hist.rangesX);
            mask.ConvertTo(mask, MatType.CV_8U);
            task.pointCloud.CopyTo(dst2, mask);

            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, new int[] { 1, 2 }, dst1, mask, hist.rangesY);
            mask.ConvertTo(mask, MatType.CV_8U);
            task.pointCloud.CopyTo(dst3, mask);
        }
    }





    public class CS_BackProject_Display : CS_Parent
    {
        BackProject_Full backP = new BackProject_Full();
        public CS_BackProject_Display(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Back projection", "" };
            desc = "Display the back projected color image";
        }

        public void RunCS(Mat src)
        {
            backP.Run(src);
            dst2 = backP.dst2;
            dst3 = backP.dst3;
        }
    }


    public class CS_BackProject_Unstable : CS_Parent
    {
        BackProject_Full backP = new BackProject_Full();
        Diff_Basics diff = new Diff_Basics();

        public CS_BackProject_Unstable(VBtask task) : base(task)
        {
            task.gOptions.pixelDiffThreshold = 6;
            labels = new string[] { "", "", "Backprojection output", "Unstable pixels in the backprojection. If flashing, set 'Pixel Difference Threshold' higher." };
            desc = "Highlight the unstable pixels in the backprojection.";
        }

        public void RunCS(Mat src)
        {
            backP.Run(src);
            dst2 = ShowPalette(backP.dst2 * 255 / backP.classCount);

            diff.Run(dst2);
            dst3 = diff.dst2;
        }
    }





    public class CS_BackProject_FullEqualized : CS_Parent
    {
        BackProject_Full backP = new BackProject_Full();
        Hist_EqualizeColor equalize = new Hist_EqualizeColor();

        public CS_BackProject_FullEqualized(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "BackProject_Full output without equalization", "BackProject_Full with equalization" };
            desc = "Create a histogram from the equalized color and then backproject it.";
        }

        public void RunCS(Mat src)
        {
            backP.Run(src);
            backP.dst2.ConvertTo(dst2, MatType.CV_8U);
            var mm = GetMinMax(dst2);
            dst2 = ShowPalette(dst2 * 255 / mm.maxVal);

            equalize.Run(src);
            backP.Run(equalize.dst2);

            backP.dst2.ConvertTo(dst3, MatType.CV_8U);
            mm = GetMinMax(dst3);
            dst3 = ShowPalette(dst3 * 255 / mm.maxVal);
        }
    }




    public class CS_Line_Basics : CS_Parent
    {
        FastLineDetector ld;
        public List<PointPair> lpList = new List<PointPair>();
        public Scalar lineColor = Scalar.White;

        public CS_Line_Basics(VBtask task) : base(task)
        {
            ld = CvXImgProc.CreateFastLineDetector();
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present.";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() == 3)
                dst2 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            else
                dst2 = src.Clone();

            if (dst2.Type() != MatType.CV_8U)
                dst2.ConvertTo(dst2, MatType.CV_8U);

            var lines = ld.Detect(dst2);

            var sortByLen = new SortedList<float, PointPair>(new compareAllowIdenticalSingleInverted());
            foreach (var v in lines)
            {
                if (v[0] >= 0 && v[0] <= dst2.Cols && v[1] >= 0 && v[1] <= dst2.Rows &&
                    v[2] >= 0 && v[2] <= dst2.Cols && v[3] >= 0 && v[3] <= dst2.Rows)
                {
                    var p1 = new cv.Point(v[0], v[1]);
                    var p2 = new cv.Point(v[2], v[3]);
                    var lp = new PointPair(p1, p2);
                    sortByLen.Add(lp.length, lp);
                }
            }

            dst2 = src;
            dst3.SetTo(0);
            lpList.Clear();
            foreach (var lp in sortByLen.Values)
            {
                lpList.Add(lp);
                DrawLine(dst2, lp.p1, lp.p2, lineColor, task.lineWidth);
                DrawLine(dst3, lp.p1, lp.p2, 255, task.lineWidth);
            }
            labels[2] = lpList.Count + " lines were detected in the current frame";
        }
    }





    public class CS_BackProject_MaskLines : CS_Parent
    {
        CS_BackProject_Masks masks;
        CS_Line_Basics lines;
        public CS_BackProject_MaskLines(VBtask task) : base(task)
        {
            masks = new CS_BackProject_Masks(task);
            lines = new CS_Line_Basics(task);
            if (standaloneTest()) task.gOptions.setDisplay1();
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, Scalar.All(0));
            labels = new string[] { "", "lines detected in the backProjection mask", "Histogram of pixels in a grayscale image.  Move mouse to see lines detected in the backprojection mask",
                                "Yellow is backProjection, lines detected are highlighted" };
            desc = "Inspect the lines from individual backprojection masks from a histogram";
        }

        public void RunCS(Mat src)
        {
            masks.RunAndMeasure(src, masks);
            dst2 = masks.dst2;
            dst3 = src.Clone();

            if (task.heartBeat)
                dst1.SetTo(Scalar.All(0));

            lines.RunAndMeasure(masks.mask, lines);
            foreach (var lp in lines.lpList)
            {
                byte val = masks.dst3.At<byte>((int)lp.p1.Y, (int)lp.p1.X);
                if (val == 255)
                    DrawLine(dst1, lp.p1, lp.p2, Scalar.White, task.lineWidth);
            }
            dst3.SetTo(Scalar.Yellow, masks.mask);
            dst3.SetTo(task.HighlightColor, dst1);
        }
    }





    public class CS_BackProject_Masks : CS_Parent
    {
        public Hist_Basics hist = new Hist_Basics();
        public int histIndex;
        public Mat mask = new Mat();

        public CS_BackProject_Masks(VBtask task) : base(task)
        {
            labels[2] = "Histogram for the gray scale image.  Move mouse to see backprojection of each grayscale mask.";
            desc = "Create all the backprojection masks from a grayscale histogram";
        }

        public Mat maskDetect(Mat gray, int histIndex)
        {
            int brickWidth = dst2.Width / hist.histogram.Rows;
            float brickRange = 255f / hist.histogram.Rows;

            float minRange = (histIndex == hist.histogram.Rows - 1) ? 255 - brickRange : histIndex * brickRange;
            float maxRange = (histIndex == hist.histogram.Rows - 1) ? 255 : (histIndex + 1) * brickRange;

            if (float.IsNaN(minRange) || float.IsInfinity(minRange) || float.IsNaN(maxRange) || float.IsInfinity(maxRange))
            {
                SetTrueText("Input data has no values - exit " + traceName);
                return new Mat();
            }

            Rangef[] ranges = { new Rangef(minRange, maxRange) };

            Cv2.CalcBackProject(new[] { gray }, new[] { 0 }, hist.histogram, mask, ranges);
            return mask;
        }

        public void RunCS(Mat src)
        {
            hist.Run(src);
            dst2 = hist.dst2;

            int brickWidth = dst2.Width / task.histogramBins;
            histIndex = (int)Math.Floor((double)(task.mouseMovePoint.X / brickWidth));

            Mat gray = (src.Channels() == 1) ? src : src.CvtColor(ColorConversionCodes.BGR2GRAY);
            dst3 = task.color.Clone();
            dst1 = maskDetect(gray, histIndex);
            if (dst1.Width == 0) return;
            dst3.SetTo(Scalar.White, dst1);
            dst2.Rectangle(new Rect(histIndex * brickWidth, 0, brickWidth, dst2.Height), Scalar.Yellow, task.lineWidth);
        }
    }





    public class CS_BackProject_Side : CS_Parent
    {
        OpAuto_YRange autoY = new OpAuto_YRange();
        Projection_HistSide histSide = new Projection_HistSide();

        public CS_BackProject_Side(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Hotspots in the Side View", "Back projection of the hotspots in the Side View" };
            desc = "Display the back projection of the hotspots in the Side View";
        }

        public void RunCS(Mat src)
        {
            histSide.Run(src);
            autoY.Run(histSide.histogram);

            dst2 = autoY.histogram.Threshold(task.projectionThreshold, 255, ThresholdTypes.Binary).ConvertScaleAbs();
            Mat histogram = autoY.histogram.SetTo(0, ~dst2);
            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsSide, histogram, dst3, task.rangesSide);
            dst3 = dst3.Threshold(0, 255, ThresholdTypes.Binary).ConvertScaleAbs();
        }
    }



    public class CS_BackProject_Top : CS_Parent
    {
        Projection_HistTop histTop = new Projection_HistTop();
        public CS_BackProject_Top(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Hotspots in the Top View", "Back projection of the hotspots in the Top View" };
            desc = "Display the back projection of the hotspots in the Top View";
        }

        public void RunCS(Mat src)
        {
            histTop.Run(src);
            dst2 = histTop.dst2;

            Mat histogram = histTop.histogram.SetTo(0, ~dst2);
            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsTop, histogram, dst3, task.rangesTop);
            dst3 = ShowPalette(dst3.ConvertScaleAbs());
        }
    }





    public class CS_BackProject_Horizontal : CS_Parent
    {
        BackProject_Top bpTop = new BackProject_Top();
        BackProject_Side bpSide = new BackProject_Side();

        public CS_BackProject_Horizontal(VBtask task) : base(task)
        {
            desc = "Use both the BackProject_Top to improve the results of the BackProject_Side for finding flat surfaces.";
        }

        public void RunCS(Mat src)
        {
            bpTop.Run(src);
            task.pointCloud.SetTo(0, bpTop.dst3);

            bpSide.Run(src);
            dst2 = bpSide.dst3;
        }
    }





    public class CS_BackProject_Vertical : CS_Parent
    {
        BackProject_Top bpTop = new BackProject_Top();
        BackProject_Side bpSide = new BackProject_Side();

        public CS_BackProject_Vertical(VBtask task) : base(task)
        {
            desc = "Use both the BackProject_Top to improve the results of the BackProject_Side for finding flat surfaces.";
        }

        public void RunCS(cv.Mat src)
        {
            bpSide.Run(src);
            task.pointCloud.SetTo(0, bpSide.dst3);

            bpTop.Run(src);
            dst2 = bpTop.dst3;
        }
    }




    public class CS_BackProject_SoloSide : CS_Parent
    {
        Projection_HistSide histSide = new Projection_HistSide();

        public CS_BackProject_SoloSide(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Solo samples in the Side View", "Back projection of the solo samples in the Side View" };
            desc = "Display the back projection of the solo samples in the Side View";
        }

        public void RunCS(Mat src)
        {
            histSide.Run(src);

            dst3 = histSide.histogram.Threshold(1, 255, ThresholdTypes.TozeroInv);
            dst3.ConvertTo(dst2, MatType.CV_8U, 255);

            histSide.histogram.SetTo(0, ~dst2);
            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsSide, histSide.histogram, dst3, task.rangesSide);
            dst3 = dst3.Threshold(0, 255, ThresholdTypes.Binary).ConvertScaleAbs();
        }
    }

    public class CS_BackProject_SoloTop : CS_Parent
    {
        Projection_HistTop histTop = new Projection_HistTop();

        public CS_BackProject_SoloTop(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Solo samples in the Top View", "Back projection of the solo samples in the Top View" };
            desc = "Display the back projection of the solo samples in the Top View";
        }

        public void RunCS(Mat src)
        {
            histTop.Run(src);

            dst3 = histTop.histogram.Threshold(1, 255, ThresholdTypes.TozeroInv);
            dst3.ConvertTo(dst2, MatType.CV_8U, 255);

            histTop.histogram.SetTo(0, ~dst2);
            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsTop, histTop.histogram, dst3, task.rangesTop);
            dst3 = dst3.Threshold(0, 255, ThresholdTypes.Binary).ConvertScaleAbs();
        }
    }



    public class CS_BackProject_LineTop : CS_Parent
    {
        Line_ViewTop line = new Line_ViewTop();
        public CS_BackProject_LineTop(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Backproject the lines found in the top view.";
        }

        public void RunCS(Mat src)
        {
            line.Run(src);

            dst2.SetTo(0);
            int w = task.lineWidth + 5;
            foreach (var lp in line.lines.lpList)
            {
                var lpNew = lp.edgeToEdgeLine(dst2.Size());
                cv.Point p1 = new cv.Point((int)lpNew.p1.X, (int)lpNew.p1.Y);
                cv.Point p2 = new cv.Point((int)lpNew.p2.X, (int)lpNew.p2.Y);
                dst2.Line(p1, p2, Scalar.White, w, task.lineType);
            }

            var histogram = line.autoX.histogram;
            histogram.SetTo(0, ~dst2);
            Cv2.CalcBackProject(new[] { task.pointCloud }, task.channelsTop, histogram, dst3, task.rangesTop);
            dst3 = dst3.Threshold(0, 255, ThresholdTypes.Binary).ConvertScaleAbs();
        }
    }

    public class CS_BackProject_LineSide : CS_Parent
    {
        Line_ViewSide line = new Line_ViewSide();
        public List<PointPair> lpList = new List<PointPair>();

        public CS_BackProject_LineSide(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Backproject the lines found in the side view.";
        }

        public void RunCS(Mat src)
        {
            line.Run(src);

            dst2.SetTo(0);
            int w = task.lineWidth + 5;
            lpList.Clear();
            foreach (var lp in line.lines.lpList)
            {
                if (Math.Abs(lp.slope) < 0.1)
                {
                    var lpNew = lp.edgeToEdgeLine(dst2.Size());
                    cv.Point p1 = new cv.Point((int)lpNew.p1.X, (int)lpNew.p1.Y);
                    cv.Point p2 = new cv.Point((int)lpNew.p2.X, (int)lpNew.p2.Y);
                    dst2.Line(p1, p2, Scalar.White, w, task.lineType);
                    lpList.Add(lp);
                }
            }

            var histogram = line.autoY.histogram;
            histogram.SetTo(0, ~dst2);
            Cv2.CalcBackProject(new[] { task.pointCloud }, task.channelsSide, histogram, dst1, task.rangesSide);
            dst1 = dst1.Threshold(0, 255, ThresholdTypes.Binary).ConvertScaleAbs();
            dst3 = src;
            dst3.SetTo(Scalar.White, dst1);
        }
    }

    public class CS_BackProject_Image : CS_Parent
    {
        public Hist_Basics hist = new Hist_Basics();
        public Mat mask = new Mat();
        Kalman_Basics kalman = new Kalman_Basics();
        public bool useInrange;

        public CS_BackProject_Image(VBtask task) : base(task)
        {
            labels[2] = "Move mouse to backproject each histogram column";
            desc = "Explore Backprojection of each element of a grayscale histogram.";
        }

        public void RunCS(Mat src)
        {
            Mat input = src;
            if (input.Channels() != 1)
                input = input.CvtColor(ColorConversionCodes.BGR2GRAY);
            hist.Run(input);
            if (hist.mm.minVal == hist.mm.maxVal)
            {
                SetTrueText("The input image is empty - mm.minval and mm.maxVal are both zero...");
                return; // the input image is empty...
            }
            dst2 = hist.dst2;

            if (kalman.kInput.Length != 2)
                Array.Resize(ref kalman.kInput, 2);
            kalman.kInput[0] = (float) hist.mm.minVal;
            kalman.kInput[1] = (float) hist.mm.maxVal;
            kalman.Run(empty);
            hist.mm.minVal = Math.Min(kalman.kOutput[0], kalman.kOutput[1]);
            hist.mm.maxVal = Math.Max(kalman.kOutput[0], kalman.kOutput[1]);

            double totalPixels = dst2.Total(); // assume we are including zeros.
            if (hist.plot.removeZeroEntry)
                totalPixels = input.CountNonZero();

            double brickWidth = dst2.Width / task.histogramBins;
            double incr = (hist.mm.maxVal - hist.mm.minVal) / task.histogramBins;
            int histIndex = (int)Math.Round(task.mouseMovePoint.X / brickWidth);

            Scalar minRange = new Scalar(histIndex * incr);
            Scalar maxRange = new Scalar((histIndex + 1) * incr + 1);
            if (histIndex + 1 == task.histogramBins)
            {
                minRange = new Scalar(254);
                maxRange = new Scalar(255);
            }
            if (useInrange)
            {
                if (histIndex == 0 && hist.plot.removeZeroEntry)
                    mask = new Mat(input.Size(), MatType.CV_8U, 0);
                else
                    mask = input.InRange(minRange, maxRange);
            }
            else
            {
                Rangef bRange = new Rangef((float)minRange.Val0, (float)maxRange.Val0);
                Rangef[] ranges = { bRange };
                Cv2.CalcBackProject(new[] { input }, new[] { 0 }, hist.histogram, mask, ranges);
            }
            dst3 = src;
            if (mask.Type() != MatType.CV_8U)
                mask.ConvertTo(mask, MatType.CV_8U);
            dst3.SetTo(Scalar.Yellow, mask);
            int actualCount = mask.CountNonZero();
            float count = hist.histogram.Get<float>(histIndex, 0);
            mmData histMax = GetMinMax(hist.histogram);
            labels[3] = "Backprojecting " + ((int)minRange.Val0).ToString() + " to " + ((int)maxRange.Val0).ToString() + " with " +
                         count.ToString() + " histogram samples and " + actualCount.ToString() + " mask count.  Histogram max count = " +
                         ((int)histMax.maxVal).ToString();
            dst2.Rectangle(new Rect((int)(histIndex * brickWidth), 0, (int)brickWidth, dst2.Height), Scalar.Yellow, task.lineWidth);
        }
    }

    public class CS_BackProject_Mouse : CS_Parent
    {
        CS_BackProject_Image backP;
        public CS_BackProject_Mouse(VBtask task) : base(task)
        {
            backP = new CS_BackProject_Image(task);
            labels[2] = "Use the mouse to select what should be shown in the backprojection of the depth histogram";
            desc = "Use the mouse to select what should be shown in the backprojection of the depth histogram";
        }
        public void RunCS(Mat src)
        {
            backP.RunAndMeasure(src, backP);
            dst2 = backP.dst2;
            dst3 = backP.dst3;
        }
    }

    public class CS_BackProject_Depth : CS_Parent
    {
        CS_BackProject_Image backp;
        public CS_BackProject_Depth(VBtask task) : base(task)
        {
            backp = new CS_BackProject_Image(task);
            desc = "Allow review of the depth backprojection";
        }
        public void RunCS(Mat src)
        {
            var depth = task.pcSplit[2].Threshold(task.MaxZmeters, 255, ThresholdTypes.TozeroInv);
            backp.RunAndMeasure(depth * 1000, backp);
            dst2 = backp.dst2;
            dst3 = src;
            dst3.SetTo(Scalar.White, backp.mask);
        }
    }

    public class CS_BackProject_MeterByMeter : CS_Parent
    {
        Mat histogram = new Mat();
        public CS_BackProject_MeterByMeter(VBtask task) : base(task)
        {
            desc = "Backproject the depth data at 1 meter intervals WITHOUT A HISTOGRAM.";
        }
        public void RunCS(Mat src)
        {
            if (task.histogramBins < task.MaxZmeters) task.gOptions.setHistogramBins( (int)task.MaxZmeters + 1);
            if (task.optionsChanged)
            {
                var incr = task.MaxZmeters / task.histogramBins;
                var histData = new List<float>();
                for (int i = 0; i < task.histogramBins; i++)
                {
                    histData.Add((float)Math.Round(i * incr));
                }

                histogram = new Mat(task.histogramBins, 1, MatType.CV_32F, histData.ToArray());
            }
            var ranges = new[] { new Rangef(0, task.MaxZmeters) };
            Cv2.CalcBackProject(new[] { task.pcSplit[2] }, new[] { 0 }, histogram, dst1, ranges);

            //dst1.SetTo(task.MaxZmeters, task.maxDepthMask);
            dst1.ConvertTo(dst2, MatType.CV_8U);
            dst3 = ShowPalette(dst1);
        }
    }

    public class CS_BackProject_Hue : CS_Parent
    {
        OEX_CalcBackProject_Demo1 hue = new OEX_CalcBackProject_Demo1();
        public int classCount;
        public CS_BackProject_Hue(VBtask task) : base(task)
        {
            desc = "Create an 8UC1 image with a backprojection of the hue.";
        }
        public void RunCS(Mat src)
        {
            hue.Run(src);
            classCount = hue.classCount;
            dst2 = hue.dst2;
            dst3 = ShowPalette(dst2 * 255 / classCount);
        }
    }






    public class CS_Benford_Basics : CS_Parent
    {
        public float[] expectedDistribution = new float[10];
        public float[] counts;
        Plot_Histogram plot = new Plot_Histogram();
        CS_AddWeighted_Basics addW;
        bool use99;

        public CS_Benford_Basics(VBtask task) : base(task)
        {
            addW = new CS_AddWeighted_Basics(task);
            for (int i = 1; i < expectedDistribution.Length; i++)
            {
                expectedDistribution[i] = (float)Math.Log10(1 + 1.0 / i); // get the precise expected values.
            }

            labels[3] = "Actual distribution of input";
            desc = "Build the capability to perform a Benford analysis.";
        }

        public void setup99()
        {
            expectedDistribution = new float[100];
            for (int i = 1; i < expectedDistribution.Length; i++)
            {
                expectedDistribution[i] = (float)Math.Log10(1 + 1.0 / i);
            }
            counts = new float[expectedDistribution.Length];
            use99 = true;
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                dst2 = src.Channels() == 1 ? src : src.CvtColor(ColorConversionCodes.BGR2GRAY);
                src = new Mat(dst2.Size(), MatType.CV_32F);
                dst2.ConvertTo(src, MatType.CV_32F);
            }

            src = src.Reshape(1, src.Width * src.Height);
            var indexer = src.GetGenericIndexer<float>();
            counts = new float[expectedDistribution.Length];

            if (!use99)
            {
                for (int i = 0; i < src.Rows; i++)
                {
                    string val = indexer[i].ToString();
                    if (val != "0" && !float.IsNaN(float.Parse(val)))
                    {
                        var firstInt = Regex.Match(val, "[1-9]{1}");
                        if (firstInt.Length > 0) counts[int.Parse(firstInt.Value)] += 1;
                    }
                }
            }
            else
            {
                // this is for the distribution 10-99
                for (int i = 0; i < src.Rows; i++)
                {
                    string val = indexer[i].ToString();
                    if (val != "0" && !float.IsNaN(float.Parse(val)))
                    {
                        var firstInt = Regex.Match(val, "[1-9]{1}").ToString();
                        int index = val.IndexOf(firstInt);
                        if (index < val.Length - 2 && index > 0)
                        {
                            string val99 = val.Substring(index + 1, 2);
                            if (int.TryParse(val99, out int result)) counts[result] += 1;
                        }
                    }
                }
            }

            Mat hist = new Mat(counts.Length, 1, MatType.CV_32F, counts);
            plot.backColor = Scalar.Blue;
            plot.Run(hist);
            dst3 = plot.dst2.Clone();
            for (int i = 0; i < counts.Length; i++)
            {
                counts[i] = src.Rows * expectedDistribution[i];
            }

            hist = new Mat(counts.Length, 1, MatType.CV_32F, counts);
            plot.backColor = Scalar.Gray;
            plot.Run(hist);

            addW.src2 = ~plot.dst2;
            addW.RunAndMeasure(dst3, addW);
            dst2 = addW.dst2;

            float wt = addW.weight;
            labels[2] = "AddWeighted: " + wt.ToString("0.0") + " actual vs. " + (1 - wt).ToString("0.0") + " Benford distribution";
        }
    }






    public class CS_Benford_NormalizedImage : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        public CS_Benford_NormalizedImage(VBtask task) : base(task)
        {
            desc = "Perform a Benford analysis of an image normalized to between 0 and 1";
        }
        public void RunCS(Mat src)
        {
            dst3 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat gray32f = new Mat();
            dst3.ConvertTo(gray32f, MatType.CV_32F);

            benford.Run(gray32f.Normalize(1));
            dst2 = benford.dst2;
            labels[2] = benford.labels[3];
            labels[3] = "Input image";
        }
    }

    // https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    public class CS_Benford_NormalizedImage99 : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        public CS_Benford_NormalizedImage99(VBtask task) : base(task)
        {
            benford.setup99();
            desc = "Perform a Benford analysis for 10-99, not 1-9, of an image normalized to between 0 and 1";
        }
        public void RunCS(Mat src)
        {
            dst3 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat gray32f = new Mat();
            dst3.ConvertTo(gray32f, MatType.CV_32F);

            benford.Run(gray32f.Normalize(1));
            dst2 = benford.dst2;
            labels[2] = benford.labels[3];
            labels[3] = "Input image";
        }
    }



    public class CS_Benford_Depth : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        public CS_Benford_Depth(VBtask task) : base(task)
        {
            desc = "Apply Benford to the depth data";
        }
        public void RunCS(Mat src)
        {
            benford.Run(task.pcSplit[2]);
            dst2 = benford.dst2;
            labels[2] = benford.labels[3];
        }
    }



    public class CS_Benford_Primes : CS_Parent
    {
       Sieve_BasicsVB sieve = new Sieve_BasicsVB();
        CS_Benford_Basics benford;
        public CS_Benford_Primes(VBtask task) : base(task)
        {
            benford = new CS_Benford_Basics(task);
            sieve.setMaxPrimes();
            labels = new string[] { "", "", "Actual Distribution of input", "" };
            desc = "Apply Benford to a list of primes";
        }
        public void RunCS(Mat src)
        {
            if (task.optionsChanged)
                sieve.Run(src); // only need to compute this once...
            SetTrueText($"Primes found: {sieve.primes.Count}", 3);

            var tmp = new Mat(sieve.primes.Count, 1, MatType.CV_32S, sieve.primes.ToArray());
            tmp.ConvertTo(tmp, MatType.CV_32F);
            benford.RunAndMeasure(tmp, benford);
            dst2 = benford.dst2;
        }
    }






    public class CS_Bezier_Basics : CS_Parent
    {
        public cv.Point[] points;

        public CS_Bezier_Basics(VBtask task) : base(task)
        {
            points = new cv.Point[]
            {
            new cv.Point(100, 100),
            new cv.Point(150, 50),
            new cv.Point(250, 150),
            new cv.Point(300, 100),
            new cv.Point(350, 150),
            new cv.Point(450, 50)
            };
            UpdateAdvice(traceName + ": Update the public points array variable. No exposed options.");
            desc = "Use n points to draw a Bezier curve.";
        }

        public cv.Point nextPoint(cv.Point[] points, int i, float t)
        {
            double x = Math.Pow(1 - t, 3) * points[i].X +
                       3 * t * Math.Pow(1 - t, 2) * points[i + 1].X +
                       3 * Math.Pow(t, 2) * (1 - t) * points[i + 2].X +
                       Math.Pow(t, 3) * points[i + 3].X;

            double y = Math.Pow(1 - t, 3) * points[i].Y +
                       3 * t * Math.Pow(1 - t, 2) * points[i + 1].Y +
                       3 * Math.Pow(t, 2) * (1 - t) * points[i + 2].Y +
                       Math.Pow(t, 3) * points[i + 3].Y;

            return new cv.Point((int)x, (int)y);
        }

        public void RunCS(Mat src)
        {
            cv.Point p1 = new cv.Point();
            for (int i = 0; i <= points.Length - 4; i += 3)
            {
                for (int j = 0; j <= 100; j++)
                {
                    cv.Point p2 = nextPoint(points, i, j / 100f);
                    if (j > 0) DrawLine(dst2, p1, p2, task.HighlightColor, task.lineWidth);
                    p1 = p2;
                }
            }
            labels[2] = "Bezier output";
        }
    }

    public class CS_Bezier_Example : CS_Parent
    {
        CS_Bezier_Basics bezier;
        public cv.Point[] points;

        public CS_Bezier_Example(VBtask task) : base(task)
        {
            bezier = new CS_Bezier_Basics(task);
            points = new cv.Point[] { new cv.Point(task.DotSize, task.DotSize), new cv.Point(dst2.Width / 6, dst2.Width / 6),
                       new cv.Point(dst2.Width * 3 / 4, dst2.Height / 2), new cv.Point(dst2.Width - task.DotSize * 2,
                       dst2.Height - task.DotSize * 2)};
            desc = "Draw a Bezier curve based with the 4 input points.";
        }

        public void RunCS(Mat src)
        {
            dst2.SetTo(Scalar.Black);
            cv.Point p1 = new cv.Point();
            for (int i = 0; i < 100; i++)
            {
                cv.Point p2 = bezier.nextPoint(points, 0, i / 100f);
                if (i > 0) DrawLine(dst2, p1, p2, task.HighlightColor, task.lineWidth);
                p1 = p2;
            }

            for (int i = 0; i < points.Length; i++)
            {
                DrawCircle(dst2, points[i], task.DotSize + 2, Scalar.White);
            }

            DrawLine(dst2, points[0], points[1], Scalar.White, task.lineWidth);
            DrawLine(dst2, points[2], points[3], Scalar.White, task.lineWidth);
        }
    }






    public class CS_BGRPattern_Basics : CS_Parent
    {
        Denoise_Pixels denoise = new Denoise_Pixels();
        Color_Basics colorFmt = new Color_Basics();
        public int classCount;

        public CS_BGRPattern_Basics(VBtask task) : base(task)
        {
            cPtr = BGRPattern_Open();
            UpdateAdvice(traceName + ": local options 'Options_ColorFormat' selects color.");
            desc = "Classify each 3-channel input pixel according to their relative values";
        }

        public void RunCS(Mat src)
        {
            colorFmt.Run(src);
            src = colorFmt.dst2;

            byte[] cppData = new byte[src.Total() * src.ElemSize()];
            Marshal.Copy(src.Data, cppData, 0, cppData.Length);
            GCHandle handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned);
            IntPtr imagePtr = BGRPattern_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols);
            handleSrc.Free();

            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr).Clone();

            classCount = BGRPattern_ClassCount(cPtr);
            denoise.classCount = classCount;
            denoise.Run(dst2);
            dst2 = denoise.dst2;

            if (standaloneTest())
            {
                dst2 = dst2 * 255 / classCount;
                dst3 = ShowPalette(dst2);
            }
        }

        public void Close()
        {
            BGRPattern_Close(cPtr);
        }
    }






    public class CS_BGSubtract_Basics : CS_Parent
    {
        public Options_BGSubtract options = new Options_BGSubtract();

        public CS_BGSubtract_Basics(VBtask task) : base(task)
        {
            cPtr = BGSubtract_BGFG_Open(options.currMethod);
            UpdateAdvice(traceName + ": local options 'Correlation Threshold' controls how well the image matches.");
            desc = "Detect motion using background subtraction algorithms in OpenCV - some only available in C++";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
            {
                BGSubtract_BGFG_Close(cPtr);
                cPtr = BGSubtract_BGFG_Open(options.currMethod);
            }

            byte[] dataSrc = new byte[src.Total() * src.ElemSize()];
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
            GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            IntPtr imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels(), options.learnRate);
            handleSrc.Free();

            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr);
            labels[2] = options.methodDesc;
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero)
            {
                cPtr = BGSubtract_BGFG_Close(cPtr);
            }
        }
    }

    // https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
    public class CS_BGSubtract_Basics_QT : CS_Parent
    {
        double learnRate;

        public CS_BGSubtract_Basics_QT(VBtask task) : base(task)
        {
            learnRate = (dst2.Width >= 1280) ? 0.5 : 0.1; // learn faster with large images (slower frame rate)
            cPtr = BGSubtract_BGFG_Open(4); // MOG2 is the default method when running in QT mode.
            desc = "Detect motion using background subtraction algorithms in OpenCV - some only available in C++";
        }

        public void RunCS(Mat src)
        {
            byte[] dataSrc = new byte[src.Total() * src.ElemSize()];
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
            GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            IntPtr imagePtr = BGSubtract_BGFG_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels(), learnRate);
            handleSrc.Free();

            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr);
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero)
            {
                cPtr = BGSubtract_BGFG_Close(cPtr);
            }
        }
    }

    public class CS_BGSubtract_MOG2 : CS_Parent
    {
        BackgroundSubtractorMOG2 MOG2;
        Options_BGSubtract options = new Options_BGSubtract();

        public CS_BGSubtract_MOG2(VBtask task) : base(task)
        {
            MOG2 = BackgroundSubtractorMOG2.Create();
            desc = "Subtract background using a mixture of Gaussians";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            if (src.Channels() == 3)
            {
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            }
            MOG2.Apply(src, dst2, options.learnRate);
        }
    }

    public class CS_BGSubtract_MOG2_QT : CS_Parent
    {
        BackgroundSubtractorMOG2 MOG2;

        public CS_BGSubtract_MOG2_QT(VBtask task) : base(task)
        {
            MOG2 = BackgroundSubtractorMOG2.Create();
            desc = "Subtract background using a mixture of Gaussians - the QT version";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() == 3)
            {
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            }
            double learnRate = (dst2.Width >= 1280) ? 0.5 : 0.1; // learn faster with large images (slower frame rate)
            MOG2.Apply(src, dst2, learnRate);
        }
    }


    public class CS_BGSubtract_MOG : CS_Parent
    {
        BackgroundSubtractorMOG MOG;
        Options_BGSubtract options = new Options_BGSubtract();
        public CS_BGSubtract_MOG(VBtask task) : base(task)
        {
            MOG = BackgroundSubtractorMOG.Create();
            desc = "Subtract background using a mixture of Gaussians";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            MOG.Apply(src, dst2, options.learnRate);
        }
    }

    public class CS_BGSubtract_GMG_KNN : CS_Parent
    {
        BackgroundSubtractorGMG gmg;
        BackgroundSubtractorKNN knn;
        Options_BGSubtract options = new Options_BGSubtract();
        public CS_BGSubtract_GMG_KNN(VBtask task) : base(task)
        {
            gmg = BackgroundSubtractorGMG.Create();
            knn = BackgroundSubtractorKNN.Create();
            desc = "GMG and KNN API's to subtract background";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.frameCount < 120)
            {
                SetTrueText("Waiting to get sufficient frames to learn background.  frameCount = " + task.frameCount);
            }
            else
            {
                SetTrueText("");
            }

            dst2 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            gmg.Apply(dst2, dst2, options.learnRate);
            knn.Apply(dst2, dst2, options.learnRate);
        }
    }

    public class CS_BGSubtract_MOG_RGBDepth : CS_Parent
    {
        public Mat grayMat = new Mat();
        Options_BGSubtract options = new Options_BGSubtract();
        BackgroundSubtractorMOG MOGDepth;
        BackgroundSubtractorMOG MOGRGB;
        public CS_BGSubtract_MOG_RGBDepth(VBtask task) : base(task)
        {
            MOGDepth = BackgroundSubtractorMOG.Create();
            MOGRGB = BackgroundSubtractorMOG.Create();
            labels = new string[] { "", "", "Unstable depth", "Unstable color (if there is motion)" };
            desc = "Isolate motion in both depth and color data using a mixture of Gaussians";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            grayMat = task.depthRGB.CvtColor(ColorConversionCodes.BGR2GRAY);
            MOGDepth.Apply(grayMat, grayMat, options.learnRate);
            dst2 = grayMat.CvtColor(ColorConversionCodes.GRAY2BGR);

            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            MOGRGB.Apply(src, dst3, options.learnRate);
        }
    }








    public class CS_BGSubtract_MotionDetect : CS_Parent
    {
        Options_MotionDetect options = new Options_MotionDetect();

        public CS_BGSubtract_MotionDetect(VBtask task) : base(task)
        {
            labels[3] = "Only Motion Added";
            desc = "Detect Motion for use with background subtraction";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged || task.frameCount < 10)
            {
                src.CopyTo(dst3);
            }

            int threadCount = options.threadData[0];
            int width = options.threadData[1], height = options.threadData[2];
            Task[] taskArray = new Task[threadCount];
            int xfactor = src.Width / width;
            int yfactor = Math.Max(src.Height / height, src.Width / width);
            dst2.SetTo(0);
            bool motionFound = false;

            for (int i = 0; i < threadCount; i++)
            {
                int section = i;
                taskArray[i] = Task.Factory.StartNew(() =>
                {
                    Rect roi = new Rect((section % xfactor) * width, height * (int)Math.Floor((double)section / yfactor), width, height);
                    Mat correlation = new Mat();
                    if (roi.X + roi.Width > dst3.Width) roi.Width = dst3.Width - roi.X - 1;
                    if (roi.Y + roi.Height > dst3.Height) roi.Height = dst3.Height - roi.Y - 1;
                    Cv2.MatchTemplate(src[roi], dst3[roi], correlation, TemplateMatchModes.CCoeffNormed);
                    if (options.CCthreshold > correlation.At<float>(0, 0))
                    {
                        src[roi].CopyTo(dst2[roi]);
                        src[roi].CopyTo(dst3[roi]);
                        motionFound = true;
                    }
                });
            }

            Task.WaitAll(taskArray);

            if (!motionFound)
            {
                SetTrueText("No motion detected in any of the regions");
            }
        }
    }






    // https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    public class CS_Benford_JPEG : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        Options_JpegQuality options = new Options_JpegQuality();

        public CS_Benford_JPEG(VBtask task) : base(task)
        {
            desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image.";
        }

        public void RunCS(OpenCvSharp.Mat src)
        {
            options.RunVB();

            byte[] jpeg = src.ImEncode(".jpg", new int[] { (int)OpenCvSharp.ImwriteFlags.JpegQuality, options.quality });
            var tmp = new OpenCvSharp.Mat(jpeg.Length, 1, OpenCvSharp.MatType.CV_8U, jpeg);
            dst3 = OpenCvSharp.Cv2.ImDecode(tmp, OpenCvSharp.ImreadModes.Color);
            benford.Run(tmp);
            dst2 = benford.dst2;
            labels[2] = benford.labels[3];
            labels[3] = "Input image";
        }
    }

    // https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    public class CS_Benford_JPEG99 : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        public Options_JpegQuality options = new Options_JpegQuality();

        public CS_Benford_JPEG99(VBtask task) : base(task)
        {
            benford.setup99();
            desc = "Perform a Benford analysis for 10-99, not 1-9, of a JPEG compressed image.";
        }

        public void RunCS(OpenCvSharp.Mat src)
        {
            options.RunVB();

            byte[] jpeg = src.ImEncode(".jpg", new int[] { (int)OpenCvSharp.ImwriteFlags.JpegQuality, options.quality });
            var tmp = new OpenCvSharp.Mat(jpeg.Length, 1, OpenCvSharp.MatType.CV_8U, jpeg);
            dst3 = OpenCvSharp.Cv2.ImDecode(tmp, OpenCvSharp.ImreadModes.Color);
            benford.Run(tmp);
            dst2 = benford.dst2;
            labels[2] = benford.labels[3];
            labels[3] = "Input image";
        }
    }

    // https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    public class CS_Benford_PNG : CS_Parent
    {
        Options_PNGCompression options = new Options_PNGCompression();
        public Benford_Basics benford = new Benford_Basics();

        public CS_Benford_PNG(VBtask task) : base(task)
        {
            desc = "Perform a Benford analysis for 1-9 of a JPEG compressed image.";
        }

        public void RunCS(OpenCvSharp.Mat src)
        {
            options.RunVB();

            byte[] png = src.ImEncode(".png", new int[] { (int)OpenCvSharp.ImwriteFlags.PngCompression, options.compression });
            var tmp = new OpenCvSharp.Mat(png.Length, 1, OpenCvSharp.MatType.CV_8U, png);
            dst3 = OpenCvSharp.Cv2.ImDecode(tmp, OpenCvSharp.ImreadModes.Color);
            benford.Run(tmp);
            dst2 = benford.dst2;
            labels[2] = benford.labels[3];
            labels[3] = "Input image";
        }
    }



    public class CS_BGSubtract_MOG_Retina : CS_Parent
    {
        CS_BGSubtract_MOG bgSub;
        Retina_Basics_CPP retina = new Retina_Basics_CPP();

        public CS_BGSubtract_MOG_Retina(VBtask task) : base(task)
        {
            bgSub = new CS_BGSubtract_MOG(task);
            labels = new string[] { "", "", "MOG results of depth motion", "Difference from retina depth motion." };
            desc = "Use the bio-inspired retina algorithm to create a background/foreground using depth.";
        }

        public void RunCS(Mat src)
        {
            retina.Run(task.depthRGB);
            bgSub.RunAndMeasure(retina.dst3.Clone(), bgSub);
            dst2 = bgSub.dst2;
            Cv2.Subtract(bgSub.dst2, retina.dst3, dst3);
        }
    }

    public class CS_BGSubtract_DepthOrColorMotion : CS_Parent
    {
        public Diff_UnstableDepthAndColor motion = new Diff_UnstableDepthAndColor();

        public CS_BGSubtract_DepthOrColorMotion(VBtask task) : base(task)
        {
            desc = "Detect motion with both depth and color changes";
        }

        public void RunCS(Mat src)
        {
            motion.Run(src);
            dst2 = motion.dst2;
            dst3 = motion.dst3;
            var mask = dst2.CvtColor(ColorConversionCodes.BGR2GRAY).ConvertScaleAbs();
            src.CopyTo(dst3, ~mask);
            labels[3] = "Image with instability filled with color data";
        }
    }

    public class CS_BGSubtract_Video : CS_Parent
    {
        CS_BGSubtract_Basics bgSub;
        Video_Basics video = new Video_Basics();

        public CS_BGSubtract_Video(VBtask task) : base(task)
        {
            bgSub = new CS_BGSubtract_Basics(task);
            video.srcVideo = task.HomeDir + "opencv/Samples/Data/vtest.avi";
            desc = "Demonstrate all background subtraction algorithms in OpenCV using a video instead of camera.";
        }

        public void RunCS(Mat src)
        {
            video.Run(src);
            dst3 = video.dst2;
            bgSub.RunAndMeasure(dst3, bgSub);
            dst2 = bgSub.dst2;
        }
    }

    public class CS_BGSubtract_Synthetic_CPP : CS_Parent
    {
        Options_BGSubtractSynthetic options = new Options_BGSubtractSynthetic();

        public CS_BGSubtract_Synthetic_CPP(VBtask task) : base(task)
        {
            labels[2] = "Synthetic background/foreground image.";
            desc = "Generate a synthetic input to background subtraction method";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.optionsChanged)
            {
                if (!task.FirstPass) BGSubtract_Synthetic_Close(cPtr);

                byte[] dataSrc = new byte[src.Total() * src.ElemSize()];
                Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
                GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);

                cPtr = BGSubtract_Synthetic_Open(handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                                 task.HomeDir + "opencv/Samples/Data/baboon.jpg",
                                                 options.amplitude / 100, options.magnitude, options.waveSpeed / 100, options.objectSpeed);
                handleSrc.Free();
            }
            IntPtr imagePtr = BGSubtract_Synthetic_Run(cPtr);
            if (imagePtr != IntPtr.Zero) dst2 = new Mat(dst2.Rows, dst2.Cols, MatType.CV_8UC3, imagePtr).Clone();
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = BGSubtract_Synthetic_Close(cPtr);
        }
    }

    public class CS_BGSubtract_Synthetic : CS_Parent
    {
        CS_BGSubtract_Basics bgSub;
        CS_BGSubtract_Synthetic_CPP synth;

        public CS_BGSubtract_Synthetic(VBtask task) : base(task)
        {
            synth = new CS_BGSubtract_Synthetic_CPP(task);
            bgSub = new CS_BGSubtract_Basics(task);
            desc = "Demonstrate background subtraction algorithms with synthetic images";
        }

        public void RunCS(Mat src)
        {
            synth.RunAndMeasure(src, synth);
            dst3 = synth.dst2;
            bgSub.RunAndMeasure(dst3, bgSub);
            dst2 = bgSub.dst2;
        }
    }



    public class CS_BGSubtract_Reduction : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        BGSubtract_Basics bgSub = new BGSubtract_Basics();

        public CS_BGSubtract_Reduction(VBtask task) : base(task)
        {
            desc = "Use BGSubtract with the output of a reduction";
        }

        public void RunCS(Mat src)
        {
            reduction.Run(src);
            var mm = GetMinMax(reduction.dst2);
            dst2 = ShowPalette(reduction.dst2 * 255 / mm.maxVal);

            bgSub.Run(dst2);
            dst3 = bgSub.dst2.Clone();

            labels[3] = "Count nonzero = " + dst3.CountNonZero().ToString();
        }
    }





    public class CS_Bin2Way_Basics : CS_Parent
    {
        public Hist_Basics hist = new Hist_Basics();
        public CS_Mat_4Click mats;
        public float fraction;

        public CS_Bin2Way_Basics(VBtask task) : base(task)
        {
            mats = new CS_Mat_4Click(task);
            fraction = dst2.Total() / 2;
            task.gOptions.setHistogramBins(256);
            labels = new string[] { "", "", "Image separated into 2 segments from darkest and lightest", "Histogram Of grayscale image" };
            desc = "Split an image into 2 parts - darkest and lightest,";
        }

        public void RunCS(Mat src)
        {
            int halfSplit = 0;
            int bins = task.histogramBins;
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            hist.Run(src);
            dst3 = hist.dst2;

            List<float> histArray = hist.histArray.ToList();
            float accum = 0;
            for (int i = 0; i < histArray.Count; i++)
            {
                accum += histArray[i];
                if (accum > fraction)
                {
                    halfSplit = i;
                    break;
                }
            }

            float offset = halfSplit / (float)bins * dst3.Width;
            Cv2.Line(dst3, new cv.Point((int)offset, 0), new cv.Point((int)offset, dst3.Height), Scalar.White);

            mats.mat[0] = src.InRange(0, halfSplit - 1); // darkest
            mats.mat[1] = src.InRange(halfSplit, 255);   // lightest

            if (standaloneTest())
            {
                mats.RunAndMeasure(Mat.Zeros(src.Size(), MatType.CV_8UC1), mats);
                dst2 = mats.dst2;
            }
        }
    }

    public class CS_Bin2Way_KMeans : CS_Parent
    {
        public Bin2Way_Basics bin2 = new Bin2Way_Basics();
        KMeans_Dimensions kmeans = new KMeans_Dimensions();
        Mat_4Click mats = new Mat_4Click();

        public CS_Bin2Way_KMeans(VBtask task) : base(task)
        {
            kmeans.km.options.setK(2);
            labels = new string[] { "", "", "Darkest (upper left), lightest (upper right)", "Selected image from dst2" };
            desc = "Use kmeans with each of the 2-way split images";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            bin2.Run(src);

            kmeans.Run(src);
            for (int i = 0; i < 2; i++)
            {
                mats.mat[i].SetTo(0);
                kmeans.dst3.CopyTo(mats.mat[i], bin2.mats.mat[i]);
            }

            mats.Run(Mat.Zeros(src.Size(), MatType.CV_8UC1));
            dst2 = mats.dst2;
            dst3 = mats.dst3;
        }
    }

    public class CS_Bin2Way_RedCloudDarkest : CS_Parent
    {
        Bin2Way_RecurseOnce bin2 = new Bin2Way_RecurseOnce();
        Flood_BasicsMask flood = new Flood_BasicsMask();

        public CS_Bin2Way_RedCloudDarkest(VBtask task) : base(task)
        {
            desc = "Use RedCloud with the darkest regions";
        }

        public void RunCS(Mat src)
        {
            if (standalone) bin2.Run(src);

            flood.inputMask = ~bin2.mats.mat[0];
            flood.Run(bin2.mats.mat[0]);
            dst2 = flood.dst2;
            if (task.heartBeat) labels[2] = task.redCells.Count + " cells were identified";
        }
    }

    public class CS_Bin2Way_RedCloudLightest : CS_Parent
    {
        Bin2Way_RecurseOnce bin2 = new Bin2Way_RecurseOnce();
        Flood_BasicsMask flood = new Flood_BasicsMask();

        public CS_Bin2Way_RedCloudLightest(VBtask task) : base(task)
        {
            desc = "Use RedCloud with the lightest regions";
        }

        public void RunCS(Mat src)
        {
            if (standalone) bin2.Run(src);

            flood.inputMask = ~bin2.mats.mat[3];
            flood.Run(bin2.mats.mat[3]);
            dst2 = flood.dst2;
            if (task.heartBeat) labels[2] = task.redCells.Count + " cells were identified";
        }
    }



    public class CS_Bin2Way_RecurseOnce : CS_Parent
    {
        Bin2Way_Basics bin2 = new Bin2Way_Basics();
        public Mat_4Click mats = new Mat_4Click();

        public CS_Bin2Way_RecurseOnce(VBtask task) : base(task)
        {
            desc = "Keep splitting an image between light and dark";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
            {
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            }

            bin2.fraction = src.Total() / 2;
            bin2.hist.inputMask = new Mat();
            bin2.Run(src);
            Mat darkestMask = bin2.mats.mat[0].Clone();
            Mat lightestMask = bin2.mats.mat[1].Clone();

            bin2.fraction = src.Total() / 4;
            bin2.hist.inputMask = darkestMask;
            bin2.Run(src);

            mats.mat[0] = bin2.mats.mat[0];
            mats.mat[1] = bin2.mats.mat[1] & ~lightestMask;

            bin2.fraction = src.Total() / 4;
            bin2.hist.inputMask = lightestMask;
            bin2.Run(src);
            mats.mat[2] = bin2.mats.mat[0] & ~darkestMask;
            mats.mat[3] = bin2.mats.mat[1];

            mats.Run(empty);
            dst2 = mats.dst2;
            dst3 = mats.dst3;
        }
    }

    public class CS_Bin2Way_RedCloud : CS_Parent
    {
        Bin2Way_RecurseOnce bin2 = new Bin2Way_RecurseOnce();
        Flood_BasicsMask flood = new Flood_BasicsMask();
        Color8U_Basics color = new Color8U_Basics();
        Mat[] cellMaps = new Mat[4];
        List<rcData>[] redCells = new List<rcData>[4];
        Options_Bin2WayRedCloud options = new Options_Bin2WayRedCloud();

        public CS_Bin2Way_RedCloud(VBtask task) : base(task)
        {
            flood.showSelected = false;
            desc = "Identify the lightest, darkest, and other regions separately and then combine the rcData.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
            {
                for (int i = 0; i < redCells.Length; i++)
                {
                    redCells[i] = new List<rcData>();
                    cellMaps[i] = new Mat(dst2.Size(), MatType.CV_8U, 0);
                }
            }

            bin2.Run(src);

            SortedList<int, rcData> sortedCells = new SortedList<int, rcData>(new compareAllowIdenticalIntegerInverted());
            for (int i = options.startRegion; i <= options.endRegion; i++)
            {
                task.cellMap = cellMaps[i];
                task.redCells = redCells[i];
                flood.inputMask = ~bin2.mats.mat[i];
                flood.Run(bin2.mats.mat[i]);
                cellMaps[i] = task.cellMap.Clone();
                redCells[i] = new List<rcData>(task.redCells);
                foreach (var rc in task.redCells)
                {
                    if (rc.index == 0) continue;
                    sortedCells.Add(rc.pixels, rc);
                }
            }

            dst2 = RebuildCells(sortedCells);

            if (task.heartBeat)
            {
                labels[2] = $"{task.redCells.Count} cells were identified and matched to the previous image";
            }
        }
    }



    public class CS_Bin3Way_Basics : CS_Parent
    {
        Hist_Basics hist = new Hist_Basics();
        public Mat_4Click mats = new Mat_4Click();
        int firstThird = 0, lastThird = 0;

        public CS_Bin3Way_Basics(VBtask task) : base(task)
        {
            task.gOptions.setHistogramBins(256);
            labels = new string[] { "", "", "Image separated into three segments from darkest to lightest and 'Other' (between)", "Histogram Of grayscale image" };
            desc = "Split an image into 3 parts - darkest, lightest, and in-between the 2";
        }

        public void RunCS(Mat src)
        {
            int bins = task.histogramBins;
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            if (task.heartBeat)
            {
                firstThird = 0;
                lastThird = 0;
                hist.Run(src);
                dst3 = hist.dst2;

                var histogram = hist.histArray.ToList();
                double third = src.Total() / 3;
                float accum = 0;
                for (int i = 0; i < histogram.Count; i++)
                {
                    accum += histogram[i];
                    if (accum > third)
                    {
                        if (firstThird == 0)
                        {
                            firstThird = i;
                            accum = 0;
                        }
                        else
                        {
                            lastThird = i;
                            break;
                        }
                    }
                }
            }

            double offset = firstThird / (double)bins * dst3.Width;
            Cv2.Line(dst3, new cv.Point(offset, 0), new cv.Point(offset, dst3.Height), Scalar.White);
            offset = lastThird / (double)bins * dst3.Width;
            Cv2.Line(dst3, new cv.Point(offset, 0), new cv.Point(offset, dst3.Height), Scalar.White);

            mats.mat[0] = src.InRange(0, firstThird - 1);         // darkest
            mats.mat[1] = src.InRange(lastThird, 255);            // lightest
            mats.mat[2] = src.InRange(firstThird, lastThird - 1); // other

            if (standaloneTest())
            {
                mats.Run(Mat.Zeros(src.Size(), MatType.CV_8U));
                dst2 = mats.dst2;
            }
        }
    }

    public class CS_Bin3Way_KMeans : CS_Parent
    {
        public Bin3Way_Basics bin3 = new Bin3Way_Basics();
        KMeans_Dimensions kmeans = new KMeans_Dimensions();
        Mat_4Click mats = new Mat_4Click();

        public CS_Bin3Way_KMeans(VBtask task) : base(task)
        {
            kmeans.km.options.setK(2);
            labels = new string[] { "", "", "Darkest (upper left), mixed (upper right), lightest (bottom left)", "Selected image from dst2" };
            desc = "Use kmeans with each of the 3-way split images";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            bin3.Run(src);

            kmeans.Run(src);
            for (int i = 0; i < 2; i++)
            {
                mats.mat[i].SetTo(0);
                kmeans.dst3.CopyTo(mats.mat[i], bin3.mats.mat[i]);
            }

            mats.Run(Mat.Zeros(src.Size(), MatType.CV_8U));
            dst2 = mats.dst2;
            dst3 = mats.dst3;
        }
    }

    public class CS_Bin3Way_Color : CS_Parent
    {
        Bin3Way_KMeans bin3 = new Bin3Way_KMeans();

        public CS_Bin3Way_Color(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "CV_8U format of the image", "showPalette output of dst2" };
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Build the palette input that best separates the light and dark regions of an image";
        }

        public void RunCS(Mat src)
        {
            bin3.Run(src);
            dst2.SetTo(4);
            dst2.SetTo(1, bin3.bin3.mats.mat[0]);
            dst2.SetTo(2, bin3.bin3.mats.mat[1]);
            dst2.SetTo(3, bin3.bin3.mats.mat[2]);
            dst3 = ShowPalette(dst2 * 255 / 3);
        }
    }



    public class CS_Bin3Way_RedCloudDarkest : CS_Parent
    {
        Bin3Way_KMeans bin3 = new Bin3Way_KMeans();
        Flood_BasicsMask flood = new Flood_BasicsMask();

        public CS_Bin3Way_RedCloudDarkest(VBtask task) : base(task)
        {
            desc = "Use RedCloud with the darkest regions";
        }

        public void RunCS(Mat src)
        {
            if (standalone) bin3.Run(src);

            flood.inputMask = ~bin3.bin3.mats.mat[0];
            flood.Run(bin3.bin3.mats.mat[0]);
            dst2 = flood.dst2;
        }
    }

    public class CS_Bin3Way_RedCloudLightest : CS_Parent
    {
        Bin3Way_KMeans bin3 = new Bin3Way_KMeans();
        Flood_BasicsMask flood = new Flood_BasicsMask();

        public CS_Bin3Way_RedCloudLightest(VBtask task) : base(task)
        {
            desc = "Use RedCloud with the lightest regions";
        }

        public void RunCS(Mat src)
        {
            if (standalone) bin3.Run(src);

            flood.inputMask = ~bin3.bin3.mats.mat[2];
            flood.Run(bin3.bin3.mats.mat[2]);
            dst2 = flood.dst2;
        }
    }

    public class CS_Bin3Way_RedCloudOther : CS_Parent
    {
        Bin3Way_KMeans bin3 = new Bin3Way_KMeans();
        Flood_BasicsMask flood = new Flood_BasicsMask();
        Color8U_Basics color = new Color8U_Basics();

        public CS_Bin3Way_RedCloudOther(VBtask task) : base(task)
        {
            flood.inputMask = new Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0));
            desc = "Use RedCloud with the regions that are neither lightest or darkest";
        }

        public void RunCS(Mat src)
        {
            if (standalone) bin3.Run(src);

            flood.inputMask = bin3.bin3.mats.mat[0] | bin3.bin3.mats.mat[1];

            color.Run(src);
            flood.Run(color.dst2);
            dst2 = flood.dst2;
        }
    }

    public class CS_Bin3Way_RedCloud1 : CS_Parent
    {
        Bin3Way_KMeans bin3 = new Bin3Way_KMeans();
        Flood_BasicsMask flood = new Flood_BasicsMask();
        Color8U_Basics color = new Color8U_Basics();
        Mat[] cellMaps = new Mat[3];
        List<rcData>[] redCells = new List<rcData>[3];
        Options_Bin3WayRedCloud options = new Options_Bin3WayRedCloud();

        public CS_Bin3Way_RedCloud1(VBtask task) : base(task)
        {
            desc = "Identify the lightest, darkest, and 'Other' regions separately and then combine the rcData.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
            {
                for (int i = 0; i < redCells.Length; i++)
                {
                    redCells[i] = new List<rcData>();
                    cellMaps[i] = new Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0));
                }
            }

            bin3.Run(src);

            for (int i = options.startRegion; i <= options.endRegion; i++)
            {
                task.cellMap = cellMaps[i];
                task.redCells = redCells[i];
                if (i == 2)
                {
                    flood.inputMask = bin3.bin3.mats.mat[0] | bin3.bin3.mats.mat[1];
                    color.Run(src);
                    flood.Run(color.dst2);
                }
                else
                {
                    flood.inputMask = ~bin3.bin3.mats.mat[i];
                    flood.Run(bin3.bin3.mats.mat[i]);
                }
                cellMaps[i] = task.cellMap.Clone();
                redCells[i] = new List<rcData>(task.redCells);
            }

            SortedList<int, rcData> sortedCells = new SortedList<int, rcData>(new compareAllowIdenticalIntegerInverted());
            for (int i = 0; i < 3; i++)
            {
                foreach (var rc in redCells[i])
                {
                    sortedCells.Add(rc.pixels, rc);
                }
            }

            dst2 = RebuildCells(sortedCells);

            if (task.heartBeat) labels[2] = task.redCells.Count + " cells were identified and matched to the previous image";
        }
    }

    public class CS_Bin3Way_RedCloud : CS_Parent
    {
        Bin3Way_KMeans bin3 = new Bin3Way_KMeans();
        Flood_BasicsMask flood = new Flood_BasicsMask();
        Color8U_Basics color = new Color8U_Basics();
        Mat[] cellMaps = new Mat[3];
        List<rcData>[] redCells = new List<rcData>[3];
        Options_Bin3WayRedCloud options = new Options_Bin3WayRedCloud();

        public CS_Bin3Way_RedCloud(VBtask task) : base(task)
        {
            flood.showSelected = false;
            desc = "Identify the lightest, darkest, and other regions separately and then combine the rcData.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
            {
                for (int i = 0; i < redCells.Length; i++)
                {
                    redCells[i] = new List<rcData>();
                    cellMaps[i] = new Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0));
                }
            }

            bin3.Run(src);

            SortedList<int, rcData> sortedCells = new SortedList<int, rcData>(new compareAllowIdenticalIntegerInverted());
            for (int i = options.startRegion; i <= options.endRegion; i++)
            {
                task.cellMap = cellMaps[i];
                task.redCells = redCells[i];
                flood.inputMask = ~bin3.bin3.mats.mat[i];
                flood.Run(bin3.bin3.mats.mat[i]);
                cellMaps[i] = task.cellMap.Clone();
                redCells[i] = new List<rcData>(task.redCells);
                foreach (var rc in redCells[i])
                {
                    if (rc.index == 0) continue;
                    sortedCells.Add(rc.pixels, rc);
                }
            }

            dst2 = RebuildCells(sortedCells);

            if (task.heartBeat) labels[2] = task.redCells.Count + " cells were identified and matched to the previous image";
        }
    }



    public class CS_Bin4Way_Basics : CS_Parent
    {
        CS_Mat_4to1 mats;
        CS_Bin4Way_SplitMean binary;
        Diff_Basics[] diff = new Diff_Basics[4];
        string[] labelStr = new string[4];
        cv.Point[] points = new cv.Point[4];
        int index = 0;
        public CS_Bin4Way_Basics(VBtask task) : base(task)
        {
            if (standalone) task.gOptions.setDisplay1();
            dst0 = new Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0));
            for (int i = 0; i < diff.Length; i++)
            {
                diff[i] = new Diff_Basics();
            }
            binary = new CS_Bin4Way_SplitMean(task);
            mats = new CS_Mat_4to1(task);
            labels = new string[] { "", "Quartiles for selected roi.  Click in dst1 to see different roi.", "4 brightness levels - darkest to lightest",
                      "Quartiles for the selected grid element, darkest to lightest" };
            desc = "Highlight the contours for each grid element with stats for each.";
        }

        public void RunCS(Mat src)
        {
            if (task.mousePicTag == 1) index = task.gridMap.At<int>(task.ClickPoint.Y, task.ClickPoint.X);
            Rect roiSave = index < task.gridList.Count ? task.gridList[index] : new Rect();

            if (task.optionsChanged) index = 0;

            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat[] matList = new Mat[4];
            for (int i = 0; i < matList.Length; i++)
            {
                mats.mat[i] = new Mat(mats.mat[i].Size(), MatType.CV_8U, Scalar.All(0));
                binary.mats.mat[i] = new Mat(binary.mats.mat[i].Size(), MatType.CV_8U, Scalar.All(0));
            }

            int quadrant;
            binary.RunAndMeasure(src, binary);
            binary.mats.RunAndMeasure(new Mat(), binary.mats);
            dst2 = binary.mats.dst2;
            dst1 = binary.mats.dst3 * 0.5;
            matList = binary.mats.mat;
            quadrant = binary.mats.quadrant;

            dst0.SetTo(Scalar.All(0));
            for (int i = 0; i < diff.Length; i++)
            {
                diff[i].Run(binary.mats.mat[i]);
                dst0 = dst0 | diff[i].dst2;
            }

            int[,] counts = new int[4, task.gridList.Count];
            List<List<int>> contourCounts = new List<List<int>>();
            List<List<float>> means = new List<List<float>>();

            cv.Point[][] allContours;
            for (int i = 0; i < counts.GetLength(0); i++)
            {
                for (int j = 0; j < task.gridList.Count; j++)
                {
                    Rect roi = task.gridList[j];
                    Mat tmp = new Mat(matList[i], roi);
                    Cv2.FindContours(tmp, out allContours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                    if (i == 0)
                    {
                        contourCounts.Add(new List<int>());
                        means.Add(new List<float>());
                    }
                    contourCounts[j].Add(allContours.Length);
                    means[j].Add((float)(src[roi].Mean(tmp)[0]));
                    if (i == quadrant) SetTrueText(allContours.Length.ToString(), roi.TopLeft, 1);
                    counts[i, j] = allContours.Length;
                }
            }

            int bump = 3;
            double ratio = (double)dst2.Height / task.gridList[0].Height;
            for (int i = 0; i < matList.Length; i++)
            {
                Mat tmp = new Mat(matList[i], roiSave) * 0.5;
                int nextCount = Cv2.CountNonZero(tmp);
                Mat tmpVolatile = new Mat(dst0, roiSave) & tmp;
                tmp.SetTo(Scalar.All(255), tmpVolatile);
                new Mat(dst0, roiSave).CopyTo(tmp, tmpVolatile);
                Rect r = new Rect(0, 0, (int)(tmp.Width * ratio), (int)(tmp.Height * ratio));
                mats.mat[i][r] = tmp.Resize(new cv.Size(r.Width, r.Height));

                if (task.heartBeat)
                {
                    int plus = mats.mat[i][r].Width / 2;

                    if (i == 0) points[i] = new cv.Point(bump + plus, bump);
                    if (i == 1) points[i] = new cv.Point(bump + dst2.Width / 2 + plus, bump);
                    if (i == 2) points[i] = new cv.Point(bump + plus, bump + dst2.Height / 2);
                    if (i == 3) points[i] = new cv.Point(bump + dst2.Width / 2 + plus, bump + dst2.Height / 2);
                }
            }

            for (int i = 0; i < labelStr.Length; i++)
            {
                SetTrueText(labelStr[i], points[i], 3);
            }

            mats.RunAndMeasure(src, mats);
            dst3 = mats.dst2;

            dst1.Rectangle(roiSave, Scalar.White, task.lineWidth);
            task.color.Rectangle(roiSave, Scalar.White, task.lineWidth);
        }
    }



    public class CS_Bin4Way_Canny : CS_Parent
    {
        Edge_Canny edges = new Edge_Canny();
        Bin4Way_SplitMean binary = new Bin4Way_SplitMean();
        Mat_4Click mats = new Mat_4Click();

        public CS_Bin4Way_Canny(VBtask task) : base(task)
        {
            labels[2] = "Edges between halves, lightest, darkest, and the combo";
            desc = "Find edges from each of the binarized images";
        }

        public void RunCS(Mat src)
        {
            binary.Run(src);

            edges.Run(binary.mats.mat[0]);  // the light and dark halves
            mats.mat[0] = edges.dst2.Threshold(0, 255, ThresholdTypes.Binary);
            mats.mat[3] = edges.dst2.Threshold(0, 255, ThresholdTypes.Binary);

            edges.Run(binary.mats.mat[1]);  // the lightest of the light half
            mats.mat[1] = edges.dst2.Threshold(0, 255, ThresholdTypes.Binary);
            mats.mat[3] = mats.mat[1] | mats.mat[3];

            edges.Run(binary.mats.mat[3]);  // the darkest of the dark half
            mats.mat[2] = edges.dst2.Threshold(0, 255, ThresholdTypes.Binary);
            mats.mat[3] = mats.mat[2] | mats.mat[3];

            mats.Run(Mat.Zeros(src.Size(), MatType.CV_8UC1));
            dst2 = mats.dst2;

            if (mats.dst3.Channels() == 3)
            {
                labels[3] = "Combo of first 3 below.  Click quadrants in dst2.";
                dst3 = mats.mat[3];
            }
            else
            {
                dst3 = mats.dst3;
            }
        }
    }

    public class CS_Bin4Way_Sobel : CS_Parent
    {
        Edge_Sobel_Old edges = new Edge_Sobel_Old();
        Bin4Way_SplitMean binary = new Bin4Way_SplitMean();
        public Mat_4Click mats = new Mat_4Click();

        public CS_Bin4Way_Sobel(VBtask task) : base(task)
        {
            SetSlider("Sobel kernel Size", 5);
            labels[2] = "Edges between halves, lightest, darkest, and the combo";
            labels[3] = "Click any quadrant in dst2 to view it in dst3";
            desc = "Collect Sobel edges from binarized images";
        }

        public void RunCS(Mat src)
        {
            binary.Run(src);

            edges.Run(binary.mats.mat[0]); // the light and dark halves
            mats.mat[0] = edges.dst2.Threshold(0, 255, ThresholdTypes.Binary);
            mats.mat[3] = edges.dst2.Threshold(0, 255, ThresholdTypes.Binary);

            edges.Run(binary.mats.mat[1]); // the lightest of the light half
            mats.mat[1] = edges.dst2.Threshold(0, 255, ThresholdTypes.Binary);
            mats.mat[3] = mats.mat[1] | mats.mat[3];

            edges.Run(binary.mats.mat[3]);  // the darkest of the dark half
            mats.mat[2] = edges.dst2.Threshold(0, 255, ThresholdTypes.Binary);
            mats.mat[3] = mats.mat[2] | mats.mat[3];

            mats.Run(Mat.Zeros(src.Size(), MatType.CV_8UC1));
            dst2 = mats.dst2;
            dst3 = mats.dst3;
        }
    }

    public class CS_Bin4Way_Unstable1 : CS_Parent
    {
        Bin4Way_SplitMean binary = new Bin4Way_SplitMean();
        Diff_Basics diff = new Diff_Basics();

        public CS_Bin4Way_Unstable1(VBtask task) : base(task)
        {
            desc = "Find the unstable pixels in the binary image";
        }

        public void RunCS(Mat src)
        {
            binary.Run(src);
            dst2 = binary.dst2;
            diff.Run(binary.dst3);
            dst3 = diff.dst2;

            if (task.heartBeat)
            {
                labels[3] = "There are " + dst3.CountNonZero().ToString() + " unstable pixels";
            }
        }
    }

    public class CS_Bin4Way_UnstableEdges : CS_Parent
    {
        Edge_Canny canny = new Edge_Canny();
        Blur_Basics blur = new Blur_Basics();
        Bin4Way_Unstable unstable = new Bin4Way_Unstable();

        public CS_Bin4Way_UnstableEdges(VBtask task) : base(task)
        {
            if (standalone)
            {
                task.gOptions.setDisplay1();
            }
            desc = "Find unstable pixels but remove those that are also edges.";
        }

        public void RunCS(Mat src)
        {
            canny.Run(src);
            blur.Run(canny.dst2);
            dst1 = blur.dst2.Threshold(0, 255, ThresholdTypes.Binary);

            unstable.Run(src);
            dst2 = unstable.dst2;
            dst3 = unstable.dst3;

            if (!task.gOptions.debugChecked)
            {
                dst3.SetTo(0, dst1);
            }
        }
    }


    public class CS_Bin4Way_UnstablePixels : CS_Parent
    {
        Bin4Way_UnstableEdges unstable = new Bin4Way_UnstableEdges();
        public List<byte> gapValues = new List<byte>();

        public CS_Bin4Way_UnstablePixels(VBtask task) : base(task)
        {
            desc = "Identify the unstable grayscale pixel values ";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            unstable.Run(src);
            dst2 = unstable.dst3;

            var points = dst2.FindNonZero();
            if (points.Rows == 0) return;

            int[] pts = new int[points.Rows * 2];
            Marshal.Copy(points.Data, pts, 0, pts.Length);

            List<byte> pixels = new List<byte>();
            SortedList<byte, int> pixelSort = new SortedList<byte, int>(new compareByte());
            for (int i = 0; i < pts.Length; i += 2)
            {
                byte val = src.At<byte>(pts[i + 1], pts[i]);
                if (!pixels.Contains(val))
                {
                    pixelSort.Add(val, 1);
                    pixels.Add(val);
                }
            }

            int gapThreshold = 2;
            gapValues.Clear();
            strOut = "These are the ranges of grayscale bytes where there is fuzziness.\n";
            int lastIndex = 0, lastGap = 0;
            foreach (var index in pixelSort.Keys)
            {
                if (Math.Abs(lastIndex - index) > gapThreshold)
                {
                    strOut += "\n";
                    gapValues.Add((byte)((index + lastGap) / 2));
                    lastGap = index;
                    for (int i = index + 1; i < pixelSort.Keys.Count; i++)
                    {
                        if (pixelSort.Keys.ElementAt(i) - lastGap > gapThreshold) break;
                        lastGap = i;
                    }
                }
                strOut += index.ToString() + "\t";
                lastIndex = index;
            }
            if (gapValues.Count < 4)
            {
                gapValues.Add((byte)((255 + lastGap) / 2));
            }

            strOut += "\n\nThe best thresholds for this image to avoid fuzziness are: \n";
            foreach (var index in gapValues)
            {
                strOut += index.ToString() + "\t";
            }
            SetTrueText(strOut, 3);
            if (task.heartBeat) labels[3] = "There are " + dst2.CountNonZero().ToString() + " unstable pixels";
        }
    }

    public class CS_Bin4Way_SplitValley : CS_Parent
    {
        Binarize_Simple binary = new Binarize_Simple();
        HistValley_Basics valley = new HistValley_Basics();
        public Mat_4Click mats = new Mat_4Click();

        public CS_Bin4Way_SplitValley(VBtask task) : base(task)
        {
            labels[2] = "A 4-way split - darkest (upper left) to lightest (lower right)";
            desc = "Binarize an image using the valleys provided by HistValley_Basics";
        }

        public void RunCS(Mat src)
        {
            Mat gray = src.Channels() == 1 ? src.Clone() : src.CvtColor(ColorConversionCodes.BGR2GRAY);

            binary.Run(gray);
            Mat mask = binary.dst2.Clone();

            if (task.heartBeat) valley.Run(gray);

            mats.mat[0] = gray.InRange(0, valley.valleys[1] - 1);
            mats.mat[1] = gray.InRange(valley.valleys[1], valley.valleys[2] - 1);
            mats.mat[2] = gray.InRange(valley.valleys[2], valley.valleys[3] - 1);
            mats.mat[3] = gray.InRange(valley.valleys[3], 255);

            mats.Run(Mat.Zeros(src.Size(), MatType.CV_8UC1));
            dst2 = mats.dst2;
            dst3 = mats.dst3;
            labels[3] = mats.labels[3];
        }
    }

    public class CS_Bin4Way_UnstablePixels1 : CS_Parent
    {
        Hist_Basics hist = new Hist_Basics();
        Bin4Way_UnstableEdges unstable = new Bin4Way_UnstableEdges();
        public List<byte> gapValues = new List<byte>();

        public CS_Bin4Way_UnstablePixels1(VBtask task) : base(task)
        {
            task.gOptions.setHistogramBins(256);
            desc = "Identify the unstable grayscale pixel values ";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            hist.Run(src);

            unstable.Run(src);
            dst2 = unstable.dst3;

            var points = dst2.FindNonZero();
            if (points.Rows == 0) return;

            int[] pts = new int[points.Rows * 2];
            Marshal.Copy(points.Data, pts, 0, pts.Length);

            List<byte> pixels = new List<byte>();
            SortedList<byte, int> pixelSort = new SortedList<byte, int>(new compareByte());
            for (int i = 0; i < pts.Length; i += 2)
            {
                byte val = src.At<byte>(pts[i + 1], pts[i]);
                if (!pixels.Contains(val))
                {
                    pixelSort.Add(val, 1);
                    pixels.Add(val);
                }
            }

            byte[] boundaries = new byte[5];
            boundaries[0] = (byte)(0 * 255 / 4);
            boundaries[1] = (byte)(1 * 255 / 4);
            boundaries[2] = (byte)(2 * 255 / 4);
            boundaries[3] = (byte)(3 * 255 / 4);
            boundaries[4] = 255;

            int gapThreshold = 2, lastIndex = 0, bIndex = 1;
            strOut = "These are the ranges of grayscale bytes where there is fuzziness.\n";
            for (int i = 0; i < pixelSort.Keys.Count; i++)
            {
                byte index = pixelSort.ElementAt(i).Key;
                if (Math.Abs(lastIndex - index) > gapThreshold)
                {
                    strOut += "\n";
                    if (bIndex < boundaries.Length)
                    {
                        boundaries[bIndex] = index;
                        bIndex++;
                    }
                }
                strOut += index.ToString() + "\t";
                lastIndex = index;
            }

            gapValues.Clear();
            for (int i = 1; i < boundaries.Length; i++)
            {
                byte minVal = byte.MaxValue;
                int minIndex = 0;
                for (int j = boundaries[i - 1]; j < boundaries[i]; j++)
                {
                    if (hist.histArray[j] < minVal)
                    {
                        minVal = (byte) hist.histArray[j];
                        minIndex = j;
                    }
                }
                gapValues.Add((byte)minIndex);
            }
            strOut += "\n\nThe best thresholds for this image to avoid fuzziness are: \n";
            foreach (var index in gapValues)
            {
                strOut += index.ToString() + "\t";
            }
            SetTrueText(strOut, 3);
            if (task.heartBeat) labels[3] = "There are " + dst2.CountNonZero().ToString() + " unstable pixels";
        }
    }



    public class CS_Bin4Way_Regions1 : CS_Parent
    {
        Binarize_Simple binary = new Binarize_Simple();
        public Mat_4Click mats = new Mat_4Click();
        public int classCount = 4; // 4-way split

        public CS_Bin4Way_Regions1(VBtask task) : base(task)
        {
            labels[2] = "A 4-way split - darkest (upper left) to lightest (lower right)";
            desc = "Binarize an image and split it into quartiles using peaks.";
        }

        public void RunCS(Mat src)
        {
            Mat gray = (src.Channels() == 1) ? src.Clone() : src.CvtColor(ColorConversionCodes.BGR2GRAY);

            binary.Run(gray);
            Mat mask = binary.dst2.Clone();

            double midColor = binary.meanScalar[0];
            double topColor = Cv2.Mean(gray, mask)[0];
            double botColor = Cv2.Mean(gray, ~mask)[0];
            mats.mat[0] = gray.InRange(0, botColor);
            mats.mat[1] = gray.InRange(botColor, midColor);
            mats.mat[2] = gray.InRange(midColor, topColor);
            mats.mat[3] = gray.InRange(topColor, 255);

            mats.Run(Mat.Zeros(dst1.Size(), MatType.CV_8U));
            dst2 = mats.dst2;
            dst3 = mats.dst3;
            labels[3] = mats.labels[3];
        }
    }


    public class CS_Bin4Way_SplitGaps : CS_Parent
    {
        Bin4Way_UnstablePixels unstable = new Bin4Way_UnstablePixels();
        public Mat_4Click mats = new Mat_4Click();
        Diff_Basics[] diff = new Diff_Basics[4];

        public CS_Bin4Way_SplitGaps(VBtask task) : base(task)
        {
            for (int i = 0; i < diff.Length; i++)
            {
                diff[i] = new Diff_Basics();
                mats.mat[i] = new Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0));
            }
            if (standalone) task.gOptions.setDisplay1();
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, Scalar.All(0));
            labels[2] = "A 4-way split - darkest (upper left) to lightest (lower right)";
            desc = "Separate the quartiles of the image using the fuzzy grayscale pixel values";
        }

        public void RunCS(Mat src)
        {
            Mat gray = (src.Channels() == 1) ? src.Clone() : src.CvtColor(ColorConversionCodes.BGR2GRAY);

            unstable.Run(gray);

            int lastVal = 255;
            for (int i = Math.Min(mats.mat.Length, unstable.gapValues.Count) - 1; i >= 0; i--)
            {
                mats.mat[i] = gray.InRange(unstable.gapValues[i], lastVal);
                lastVal = unstable.gapValues[i];
            }

            dst1.SetTo(Scalar.All(0));
            for (int i = 0; i < diff.Length; i++)
            {
                diff[i].Run(mats.mat[i]);
                dst1 = dst1 | diff[i].dst2;
            }
            mats.Run(Mat.Zeros(dst1.Size(), MatType.CV_8U));
            dst2 = mats.dst2;
            dst3 = mats.dst3;
            if (task.heartBeat) labels[1] = "There are " + dst1.CountNonZero().ToString() + " unstable pixels";
        }
    }

    public class CS_Bin4Way_RegionsLeftRight : CS_Parent
    {
        Bin4Way_SplitGaps binaryLeft = new Bin4Way_SplitGaps();
        Bin4Way_SplitGaps binaryRight = new Bin4Way_SplitGaps();
        public int classCount = 4; // 4-way split

        public CS_Bin4Way_RegionsLeftRight(VBtask task) : base(task)
        {
            dst0 = new Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0));
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, Scalar.All(0));
            labels = new string[] { "", "", "Left in 4 colors", "Right image in 4 colors" };
            desc = "Add the 4-way split of left and right views.";
        }

        public void RunCS(Mat src)
        {
            binaryLeft.Run(src);

            dst0.SetTo(Scalar.All(1), binaryLeft.mats.mat[0]);
            dst0.SetTo(Scalar.All(2), binaryLeft.mats.mat[1]);
            dst0.SetTo(Scalar.All(3), binaryLeft.mats.mat[2]);
            dst0.SetTo(Scalar.All(4), binaryLeft.mats.mat[3]);

            dst2 = ShowPalette((dst0 * 255 / classCount).ToMat());

            binaryRight.Run(task.rightView);

            dst1.SetTo(Scalar.All(1), binaryRight.mats.mat[0]);
            dst1.SetTo(Scalar.All(2), binaryRight.mats.mat[1]);
            dst1.SetTo(Scalar.All(3), binaryRight.mats.mat[2]);
            dst1.SetTo(Scalar.All(4), binaryRight.mats.mat[3]);

            dst3 = ShowPalette((dst1 * 255 / classCount).ToMat());
        }
    }


    public class CS_Bin4Way_RedCloud : CS_Parent
    {
        Bin4Way_BasicsRed bin2 = new Bin4Way_BasicsRed();
        Flood_BasicsMask flood = new Flood_BasicsMask();
        Mat[] cellMaps = new Mat[4];
        List<rcData>[] redCells = new List<rcData>[4];
        Options_Bin2WayRedCloud options = new Options_Bin2WayRedCloud();

        public CS_Bin4Way_RedCloud(VBtask task) : base(task)
        {
            flood.showSelected = false;
            desc = "Identify the lightest and darkest regions separately and then combine the rcData.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
            {
                for (int i = 0; i < redCells.Length; i++)
                {
                    redCells[i] = new List<rcData>();
                    cellMaps[i] = new Mat(dst2.Size(), MatType.CV_8U, 0);
                }
            }

            bin2.Run(src);

            var sortedCells = new SortedList<int, rcData>(new compareAllowIdenticalIntegerInverted());
            for (int i = options.startRegion; i <= options.endRegion; i++)
            {
                task.cellMap = cellMaps[i];
                task.redCells = redCells[i];
                flood.inputMask = ~bin2.mats.mat[i];
                flood.Run(bin2.mats.mat[i]);
                cellMaps[i] = task.cellMap.Clone();
                redCells[i] = new List<rcData>(task.redCells);
                foreach (var rc in task.redCells)
                {
                    if (rc.index == 0) continue;
                    sortedCells.Add(rc.pixels, rc);
                }
            }

            dst2 = RebuildCells(sortedCells);

            if (task.heartBeat) labels[2] = $"{task.redCells.Count} cells were identified and matched to the previous image";
        }
    }

    public class CS_Bin4Way_Regions : CS_Parent
    {
        Bin4Way_SplitMean binary = new Bin4Way_SplitMean();
        public int classCount = 4; // 4-way split 

        public CS_Bin4Way_Regions(VBtask task) : base(task)
        {
            rebuildMats();
            labels = new string[] { "", "", "CV_8U version of dst3 with values ranging from 1 to 4", "Palettized version of dst2" };
            desc = "Add the 4-way split of images to define the different regions.";
        }

        void rebuildMats()
        {
            dst2 = new Mat(task.WorkingRes, MatType.CV_8U, 0);
            for (int i = 0; i < binary.mats.mat.Count(); i++)
            {
                binary.mats.mat[i] = new Mat(task.WorkingRes, MatType.CV_8UC1, 0);
            }
        }

        public void RunCS(Mat src)
        {
            binary.Run(src);
            if (dst2.Width != binary.mats.mat[0].Width) rebuildMats();

            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            dst2.SetTo(1, binary.mats.mat[0]);
            dst2.SetTo(2, binary.mats.mat[1]);
            dst2.SetTo(3, binary.mats.mat[2]);
            dst2.SetTo(4, binary.mats.mat[3]);

            dst3 = ShowPalette((dst2 * 255 / classCount).ToMat());
        }
    }





    public class CS_Bin4Way_SplitMean : CS_Parent
    {
        public CS_Binarize_Simple binary;
        public CS_Mat_4Click mats;
        Scalar botColor, midColor, topColor;

        public CS_Bin4Way_SplitMean(VBtask task) : base(task)
        {
            mats = new CS_Mat_4Click(task);
            binary = new CS_Binarize_Simple(task);
            labels[2] = "A 4-way split - darkest (upper left) to lightest (lower right)";
            desc = "Binarize an image and split it into quartiles using peaks.";
        }

        public void RunCS(Mat src)
        {
            Mat gray = (src.Channels() == 1) ? src.Clone() : src.CvtColor(ColorConversionCodes.BGR2GRAY);

            binary.RunAndMeasure(gray, binary);
            Mat mask = binary.dst2.Clone();

            if (task.heartBeat)
            {
                midColor = binary.meanScalar[0];
                topColor = Cv2.Mean(gray, mask)[0];
                botColor = Cv2.Mean(gray, ~mask)[0];
            }

            mats.mat[0] = gray.InRange(new Scalar(0), botColor);
            mats.mat[1] = gray.InRange(botColor, midColor);
            mats.mat[2] = gray.InRange(midColor, topColor);
            mats.mat[3] = gray.InRange(topColor, new Scalar(255));

            mats.RunAndMeasure(Mat.Zeros(gray.Size(), MatType.CV_8UC1), mats);
            dst2 = mats.dst2;
            dst3 = mats.dst3;
            labels[3] = mats.labels[3];
        }
    }


    public class CS_Binarize_Basics : CS_Parent
    {
        public ThresholdTypes thresholdType = ThresholdTypes.Otsu;
        public Mat histogram = new Mat();
        public Scalar meanScalar;
        public Mat mask = new Mat();
        Blur_Basics blur = new Blur_Basics();
        public bool useBlur;

        public CS_Binarize_Basics(VBtask task) : base(task)
        {
            mask = new Mat(dst2.Size(), MatType.CV_8U, 255);
            UpdateAdvice(traceName + ": use local options to control the kernel size and sigma.");
            desc = "Binarize an image using Threshold with OTSU.";
        }

        public void RunCS(Mat src)
        {
            meanScalar = Cv2.Mean(src, mask);

            Mat input = src;
            if (input.Channels() == 3)
                input = input.CvtColor(ColorConversionCodes.BGR2GRAY);

            if (useBlur)
            {
                blur.Run(input);
                dst2 = blur.dst2.Threshold(meanScalar.Val0, 255, thresholdType);
            }
            else
            {
                dst2 = input.Threshold(meanScalar.Val0, 255, thresholdType);
            }
        }
    }

    // https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
    public class CS_Binarize_OTSU : CS_Parent
    {
        Binarize_Basics binarize;
        Options_Binarize options = new Options_Binarize();
        public CS_Binarize_OTSU(VBtask task) : base(task)
        {
            binarize = new Binarize_Basics();
            labels[2] = "Threshold 1) binary 2) Binary+OTSU 3) OTSU 4) OTSU+Blur";
            labels[3] = "Histograms correspond to images on the left";
            desc = "Binarize an image using Threshold with OTSU.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Mat input = src;
            if (input.Channels() == 3)
                input = input.CvtColor(ColorConversionCodes.BGR2GRAY);

            binarize.meanScalar = Cv2.Mean(input);

            binarize.useBlur = false;
            switch (labels[2])
            {
                case "Binary":
                    binarize.thresholdType = ThresholdTypes.Binary;
                    break;
                case "Binary + OTSU":
                    binarize.thresholdType = ThresholdTypes.Binary | ThresholdTypes.Otsu;
                    break;
                case "OTSU":
                    binarize.thresholdType = ThresholdTypes.Otsu;
                    break;
                case "OTSU + Blur":
                    binarize.useBlur = true;
                    binarize.thresholdType = ThresholdTypes.Binary | ThresholdTypes.Otsu;
                    break;
            }
            binarize.Run(input);
            dst2 = binarize.dst2;
        }
    }


    public class CS_Binarize_KMeansMasks : CS_Parent
    {
        KMeans_Image km = new KMeans_Image();
        Mat_4Click mats = new Mat_4Click();
        public CS_Binarize_KMeansMasks(VBtask task) : base(task)
        {
            labels[2] = "Ordered from dark to light, top left darkest, bottom right lightest ";
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            desc = "Display the top 4 masks from the BGR kmeans output";
        }
        public void RunCS(Mat src)
        {
            km.Run(src);
            for (int i = 0; i < km.masks.Count; i++)
            {
                mats.mat[i] = km.masks[i];
                dst1.SetTo(i + 1, km.masks[i]);
                if (i >= 3) break;
            }

            mats.Run(Mat.Zeros(src.Size(), MatType.CV_8U));
            dst2 = mats.dst2;
            dst3 = mats.dst3;
        }
    }



    public class CS_Binarize_KMeansRGB : CS_Parent
    {
        KMeans_Image km = new KMeans_Image();
        Mat_4Click mats = new Mat_4Click();

        public CS_Binarize_KMeansRGB(VBtask task) : base(task)
        {
            labels[2] = "Ordered from dark to light, top left darkest, bottom right lightest ";
            desc = "Display the top 4 masks from the BGR kmeans output";
        }

        public void RunCS(Mat src)
        {
            km.Run(src);
            dst1.SetTo(0);
            for (int i = 0; i < km.masks.Count; i++)
            {
                mats.mat[i] = new Mat(dst2.Size(), MatType.CV_8UC3, Scalar.All(0));
                src.CopyTo(mats.mat[i], km.masks[i]);
                if (i >= 3) break;
            }
            mats.Run(Mat.Zeros(src.Size(), MatType.CV_8UC3));
            dst2 = mats.dst2;
            dst3 = mats.dst3;
        }
    }

    public class CS_Binarize_FourPixelFlips : CS_Parent
    {
        Bin4Way_Regions binar4 = new Bin4Way_Regions();
        Mat lastSubD;
        public CS_Binarize_FourPixelFlips(VBtask task) : base(task)
        {
            desc = "Identify the marginal regions that flip between subdivisions based on brightness.";
        }

        public void RunCS(Mat src)
        {
            binar4.Run(src);
            dst2 = ShowPalette(binar4.dst2 * 255 / 5);

            if (task.FirstPass) lastSubD = binar4.dst2.Clone();
            dst3 = lastSubD - binar4.dst2;
            dst3 = dst3.Threshold(0, 255, ThresholdTypes.Binary);
            lastSubD = binar4.dst2.Clone();
        }
    }

    public class CS_Binarize_DepthTiers : CS_Parent
    {
        Depth_TiersZ tiers = new Depth_TiersZ();
        Bin4Way_Regions binar4 = new Bin4Way_Regions();
        public int classCount = 200; // 4-way split with 50 depth levels at 10 cm's each.

        public CS_Binarize_DepthTiers(VBtask task) : base(task)
        {
            task.redOptions.useColorOnlyChecked = true;
            desc = "Add the Depth_TiersZ and Bin4Way_Regions output in preparation for RedCloud";
        }

        public void RunCS(Mat src)
        {
            binar4.Run(src);
            tiers.Run(src);
            dst3 = tiers.dst3;

            dst0 = tiers.dst2 + binar4.dst2;

            if (task.heartBeat)
            {
                dst2 = dst0.Clone();
            }
            else if (task.motionDetected)
            {
                dst0[task.motionRect].CopyTo(dst2[task.motionRect]);
            }
            classCount = binar4.classCount + tiers.classCount;
        }
    }

    public class CS_Binarize_Simple : CS_Parent
    {
        public Scalar meanScalar;
        public int injectVal = 255;

        public CS_Binarize_Simple(VBtask task) : base(task)
        {
            desc = "Binarize an image using Threshold with OTSU.";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            meanScalar = Cv2.Mean(src);
            dst2 = src.Threshold(meanScalar[0], injectVal, ThresholdTypes.Binary);
        }
    }


    public class CS_Binarize_Niblack_Sauvola : CS_Parent
    {
        Options_BinarizeNiBlack options = new Options_BinarizeNiBlack();
        //[InlineData(LocalBinarizationMethods.Niblack)]
        //[InlineData(LocalBinarizationMethods.Sauvola)]
        //[InlineData(LocalBinarizationMethods.Wolf)]
        //[InlineData(LocalBinarizationMethods.Nick)]
        public CS_Binarize_Niblack_Sauvola(VBtask task) : base(task)
        {
            desc = "Binarize an image using Niblack and Sauvola";
            labels[2] = "Binarize Niblack";
            labels[3] = "Binarize Sauvola";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            if (src.Channels() == 3)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            CvXImgProc.NiblackThreshold(src, dst0, 255, ThresholdTypes.Binary, 5, 0.5, LocalBinarizationMethods.Niblack);
            dst2 = dst0.CvtColor(ColorConversionCodes.GRAY2BGR);
            CvXImgProc.NiblackThreshold(src, dst0, 255, ThresholdTypes.Binary, 5, 0.5, LocalBinarizationMethods.Sauvola);
            dst3 = dst0.CvtColor(ColorConversionCodes.GRAY2BGR);
        }
    }



    public class CS_Binarize_Wolf_Nick : CS_Parent
    {
        Options_BinarizeNiBlack options = new Options_BinarizeNiBlack();
        public CS_Binarize_Wolf_Nick(VBtask task) : base(task)
        {
            desc = "Binarize an image using Wolf and Nick";
            labels[2] = "Binarize Wolf";
            labels[3] = "Binarize Nick";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            CvXImgProc.NiblackThreshold(src, dst0, 255, ThresholdTypes.Binary, 5, 0.5, LocalBinarizationMethods.Wolf);
            dst2 = dst0.CvtColor(ColorConversionCodes.GRAY2BGR);
            CvXImgProc.NiblackThreshold(src, dst0, 255, ThresholdTypes.Binary, 5, 0.5, LocalBinarizationMethods.Nick);
            dst3 = dst0.CvtColor(ColorConversionCodes.GRAY2BGR);
        }
    }


    public class CS_Blob_Input : CS_Parent
    {
        Rectangle_Rotated rotatedRect = new Rectangle_Rotated();
        Draw_Circles circles = new Draw_Circles();
        Draw_Ellipses ellipses = new Draw_Ellipses();
        Draw_Polygon poly = new Draw_Polygon();
        public Mat_4Click Mats = new Mat_4Click();
        public int updateFrequency = 30;

        public CS_Blob_Input(VBtask task) : base(task)
        {
            SetSlider("DrawCount", 5);
            FindCheckBox("Draw filled (unchecked draw an outline)").Checked = true;

            Mats.mats.lineSeparators = false;

            labels[2] = "Click any quadrant below to view it on the right";
            labels[3] = "Click any quadrant at left to view it below";
            desc = "Generate data to test Blob Detector.";
        }

        public void RunCS(Mat src)
        {
            rotatedRect.Run(src);
            Mats.mat[0] = rotatedRect.dst2;

            circles.Run(src);
            Mats.mat[1] = circles.dst2;

            ellipses.Run(src);
            Mats.mat[2] = ellipses.dst2;

            poly.Run(src);
            Mats.mat[3] = poly.dst3;
            Mats.Run(empty);
            dst2 = Mats.dst2;
            dst3 = Mats.dst3;
        }
    }

    public class CS_Blob_RenderBlobs : CS_Parent
    {
        Blob_Input input = new Blob_Input();

        public CS_Blob_RenderBlobs(VBtask task) : base(task)
        {
            labels[2] = "Input blobs";
            labels[3] = "Largest blob, centroid in yellow";
            desc = "Use connected components to find blobs.";
        }

        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                input.Run(src);
                dst2 = input.dst2;
                var gray = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
                var binary = gray.Threshold(0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
                var labelView = dst2.EmptyClone();
                var stats = new Mat();
                var centroids = new Mat();
                var cc = Cv2.ConnectedComponentsEx(binary);
                var labelCount = Cv2.ConnectedComponentsWithStats(binary, labelView, stats, centroids);
                cc.RenderBlobs(labelView);

                foreach (var b in cc.Blobs.Skip(1))
                {
                    dst2.Rectangle(b.Rect, Scalar.Red, task.lineWidth + 1, task.lineType);
                }

                var maxBlob = cc.GetLargestBlob();
                dst3.SetTo(0);
                cc.FilterByBlob(dst2, dst3, maxBlob);

                dst3.Circle(new cv.Point(maxBlob.Centroid.X, maxBlob.Centroid.Y), task.DotSize + 3, Scalar.Blue, -1, task.lineType);
                DrawCircle(dst3, new cv.Point(maxBlob.Centroid.X, maxBlob.Centroid.Y), task.DotSize, Scalar.Yellow);
            }
        }
    }


    public class CS_BlockMatching_Basics : CS_Parent
    {
        Depth_Colorizer_CPP colorizer = new Depth_Colorizer_CPP();
        Options_BlockMatching options = new Options_BlockMatching();

        public CS_BlockMatching_Basics(VBtask task) : base(task)
        {
            if (standaloneTest())
            {
                task.gOptions.setDisplay1();
            }
            labels[2] = "Block matching disparity colorized like depth";
            labels[3] = "Right Image (used with left image)";
            UpdateAdvice(traceName + ": click 'Show All' to see all the available options.");
            desc = "Use OpenCV's block matching on left and right views";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.cameraName == "Azure Kinect 4K")
            {
                SetTrueText("For the K4A 4 Azure camera, the left and right views are the same.");
            }

            var blockMatch = StereoBM.Create();
            blockMatch.BlockSize = options.blockSize;
            blockMatch.MinDisparity = 0;
            blockMatch.ROI1 = new Rect(0, 0, task.leftView.Width, task.leftView.Height);
            blockMatch.ROI2 = new Rect(0, 0, task.leftView.Width, task.leftView.Height);
            blockMatch.PreFilterCap = 31;
            blockMatch.NumDisparities = options.numDisparity;
            blockMatch.TextureThreshold = 10;
            blockMatch.UniquenessRatio = 15;
            blockMatch.SpeckleWindowSize = 100;
            blockMatch.SpeckleRange = 32;
            blockMatch.Disp12MaxDiff = 1;

            Mat tmpLeft = task.leftView.Channels() == 3 ? task.leftView.CvtColor(ColorConversionCodes.BGR2GRAY) : task.leftView;
            Mat tmpRight = task.rightView.Channels() == 3 ? task.rightView.CvtColor(ColorConversionCodes.BGR2GRAY) : task.rightView;

            Mat disparity = new Mat();
            blockMatch.Compute(tmpLeft, tmpRight, disparity);
            disparity.ConvertTo(dst1, MatType.CV_32F, 1.0 / 16);
            dst1 = dst1.Threshold(0, 0, ThresholdTypes.Tozero);

            int topMargin = 10, sideMargin = 8;
            Rect rect = new Rect(options.numDisparity + sideMargin, topMargin, src.Width - options.numDisparity - sideMargin * 2, src.Height - topMargin * 2);
            Cv2.Divide(options.distance, dst1[rect], dst1[rect]); // this needs much more refinement. The trackbar value is just an approximation.
            dst1[rect] = dst1[rect].Threshold(10, 10, ThresholdTypes.Trunc);

            colorizer.Run(dst1);
            dst2[rect] = colorizer.dst2[rect];
            dst3 = task.rightView.Resize(src.Size());
        }
    }


    public class CS_Blur_Basics : CS_Parent
    {
        Options_Blur options = new Options_Blur();
        public CS_Blur_Basics(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": use local options to control the kernel size and sigma.");
            desc = "Smooth each pixel with a Gaussian kernel of different sizes.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Cv2.GaussianBlur(src, dst2, new cv.Size(options.kernelSize, options.kernelSize), options.sigma, options.sigma);
        }
    }

    public class CS_Blur_Homogeneous : CS_Parent
    {
        Blur_Basics blur = new Blur_Basics();
        public CS_Blur_Homogeneous(VBtask task) : base(task)
        {
            desc = "Smooth each pixel with a kernel of 1's of different sizes.";
        }
        public void RunCS(Mat src)
        {
            Cv2.Blur(src, dst2, new cv.Size(blur.Options.kernelSize, blur.Options.kernelSize), new cv.Point(-1, -1));
        }
    }

    public class CS_Blur_Median : CS_Parent
    {
        Blur_Basics blur = new Blur_Basics();
        public CS_Blur_Median(VBtask task) : base(task)
        {
            desc = "Replace each pixel with the median of neighborhood of varying sizes.";
        }
        public void RunCS(Mat src)
        {
            Cv2.MedianBlur(src, dst2, blur.Options.kernelSize);
        }
    }

    // https://docs.opencv.org/2.4/modules/imgproc/doc/filtering.html?highlight=bilateralfilter
    // https://www.tutorialspoint.com/opencv/opencv_bilateral_filter.htm
    public class CS_Blur_Bilateral : CS_Parent
    {
        Blur_Basics blur = new Blur_Basics();
        public CS_Blur_Bilateral(VBtask task) : base(task)
        {
            desc = "Smooth each pixel with a Gaussian kernel of different sizes but preserve edges";
        }
        public void RunCS(Mat src)
        {
            Cv2.BilateralFilter(src, dst2, blur.Options.kernelSize, blur.Options.kernelSize * 2, blur.Options.kernelSize / 2);
        }
    }



    public class CS_Blur_PlusHistogram : CS_Parent
    {
        Mat_2to1 mat2to1 = new Mat_2to1();
        Blur_Bilateral blur = new Blur_Bilateral();
        Hist_EqualizeGray myhist = new Hist_EqualizeGray();

        public CS_Blur_PlusHistogram(VBtask task) : base(task)
        {
            labels[2] = "Use Blur slider to see impact on histogram peak values";
            labels[3] = "Top is before equalize, Bottom is after Equalize";
            desc = "Compound algorithms Blur and Histogram";
        }

        public void RunCS(Mat src)
        {
            myhist.Run(src);
            mat2to1.mat[0] = myhist.dst2.Clone();

            blur.Run(src);
            dst2 = blur.dst2.Clone();

            myhist.Run(blur.dst2);
            mat2to1.mat[1] = myhist.dst2.Clone();
            mat2to1.Run(src);
            dst3 = mat2to1.dst2;
        }
    }


    public class CS_Blur_Detection : CS_Parent
    {
        Laplacian_Basics laplace = new Laplacian_Basics();
        Blur_Basics blur = new Blur_Basics();

        public CS_Blur_Detection(VBtask task) : base(task)
        {
            SetSlider("Laplacian Threshold", 50);
            SetSlider("Blur Kernel Size", 11);
            labels = new string[] { "", "", "Draw a rectangle to blur a region in alternating frames and test further", "Detected blur in the highlight regions - non-blur is white." };
            desc = "Detect blur in an image";
        }

        public void RunCS(Mat src)
        {
            Rect r = new Rect(dst2.Width / 2 - 25, dst2.Height / 2 - 25, 50, 50);
            if (standaloneTest())
            {
                if (task.drawRect != new Rect()) r = task.drawRect;
                if (task.frameCount % 2 == 1)
                {
                    blur.Run(src[r]);
                    src[r] = blur.dst2;
                }
            }

            dst2 = src;
            laplace.Run(src);
            dst3 = laplace.dst2;

            Scalar mean, stdev;
            Cv2.MeanStdDev(dst2, out mean, out stdev);
            SetTrueText("Blur variance is " + (stdev.Val0 * stdev.Val0).ToString("F3"), 3);

            if (standaloneTest()) dst2.Rectangle(r, Scalar.White, task.lineWidth);
        }
    }

    public class CS_Blur_Depth : CS_Parent
    {
        Blur_Basics blur = new Blur_Basics();

        public CS_Blur_Depth(VBtask task) : base(task)
        {
            desc = "Blur the depth results to help find the boundaries to large depth regions";
        }

        public void RunCS(Mat src)
        {
            dst3 = task.depthRGB.CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(0, 255, ThresholdTypes.Binary);

            blur.Run(dst3);
            dst2 = blur.dst2;
        }
    }

    public class CS_Blur_TopoMap : CS_Parent
    {
        Gradient_CartToPolar gradient = new Gradient_CartToPolar();
        AddWeighted_Basics addw = new AddWeighted_Basics();
        Options_BlurTopo options = new Options_BlurTopo();

        public CS_Blur_TopoMap(VBtask task) : base(task)
        {
            labels[2] = "Image Gradient";
            desc = "Create a topo map from the blurred image";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            gradient.Run(src);
            dst2 = gradient.magnitude;

            if (options.kernelSize > 1)
            {
                Cv2.GaussianBlur(dst2, dst3, new cv.Size(options.kernelSize, options.kernelSize), 0, 0);
            }
            dst3 = dst3.Normalize(255);
            dst3 = dst3.ConvertScaleAbs(255);

            dst3 = (dst3 * 1 / options.reduction).ToMat();
            dst3 = (dst3 * options.reduction).ToMat();

            addw.src2 = ShowPalette(dst3);
            addw.Run(task.color);
            dst3 = addw.dst2;

            labels[3] = "Blur = " + options.nextPercent.ToString() + "% Reduction Factor = " + options.reduction.ToString();
            if (task.frameCount % options.frameCycle == 0)
            {
                options.nextPercent -= 1;
            }
            if (options.nextPercent <= 0)
            {
                options.nextPercent = options.savePercent;
            }
        }
    }


    public class CS_BlurMotion_Basics : CS_Parent
    {
        public Mat kernel;
        public Options_MotionBlur options = new Options_MotionBlur();

        public CS_BlurMotion_Basics(VBtask task) : base(task)
        {
            desc = "Use Filter2D to create a motion blur";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (standaloneTest())
            {
                var blurSlider = FindSlider("Motion Blur Length");
                var blurAngleSlider = FindSlider("Motion Blur Angle");
                blurAngleSlider.Value = blurAngleSlider.Value < blurAngleSlider.Maximum ? blurAngleSlider.Value + 1 : blurAngleSlider.Minimum;
            }

            kernel = new Mat(options.kernelSize, options.kernelSize, MatType.CV_32F, Scalar.All(0));
            var pt1 = new cv.Point(0, (options.kernelSize - 1) / 2);
            var pt2 = new cv.Point(options.kernelSize * Math.Cos(options.theta) + pt1.X, options.kernelSize * Math.Sin(options.theta) + pt1.Y);
            kernel.Line(pt1, pt2, new Scalar(1.0 / options.kernelSize));
            dst2 = src.Filter2D(-1, kernel);

            pt1 += new cv.Point(src.Width / 2, src.Height / 2);
            pt2 += new cv.Point(src.Width / 2, src.Height / 2);

            if (options.showDirection)
            {
                dst2.Line(pt1, pt2, Scalar.Yellow, task.lineWidth + 3, task.lineType);
            }
        }
    }

    // https://docs.opencv.org/trunk/d1/dfd/tutorial_motion_deblur_filter.html
    public class CS_BlurMotion_Deblur : CS_Parent
    {
        CS_BlurMotion_Basics mblur;

        Mat calcPSF(cv.Size filterSize, int len, double theta)
        {
            var h = new Mat(filterSize, MatType.CV_32F, Scalar.All(0));
            var pt = new cv.Point(filterSize.Width / 2, filterSize.Height / 2);
            h.Ellipse(pt, new cv.Size(0, len / 2), 90 - theta, 0, 360, new Scalar(255), -1);
            var summa = Cv2.Sum(h);
            return h / summa[0];
        }

        Mat calcWeinerFilter(Mat input_h_PSF, double nsr)
        {
            var h_PSF_shifted = fftShift(input_h_PSF);
            var planes = new Mat[] { h_PSF_shifted.Clone(), new Mat(h_PSF_shifted.Size(), MatType.CV_32F, Scalar.All(0)) };
            var complexI = new Mat();
            Cv2.Merge(planes, complexI);
            Cv2.Dft(complexI, complexI);
            planes = Cv2.Split(complexI);
            var denom = new Mat();
            Cv2.Pow(Cv2.Abs(planes[0]), 2, denom);
            denom += nsr;
            var output_G = new Mat();
            Cv2.Divide(planes[0], denom, output_G);
            return output_G;
        }

        Mat fftShift(Mat inputImg)
        {
            var outputImg = inputImg.Clone();
            int cx = outputImg.Width / 2;
            int cy = outputImg.Height / 2;
            var q0 = new Mat(outputImg, new Rect(0, 0, cx, cy));
            var q1 = new Mat(outputImg, new Rect(cx, 0, cx, cy));
            var q2 = new Mat(outputImg, new Rect(0, cy, cx, cy));
            var q3 = new Mat(outputImg, new Rect(cx, cy, cx, cy));
            var tmp = q0.Clone();
            q3.CopyTo(q0);
            tmp.CopyTo(q3);
            q1.CopyTo(tmp);
            q2.CopyTo(q1);
            tmp.CopyTo(q2);
            return outputImg;
        }

        Mat edgeTaper(Mat inputImg, double gamma, double beta)
        {
            int nx = inputImg.Width;
            int ny = inputImg.Height;
            var w1 = new Mat(1, nx, MatType.CV_32F, Scalar.All(0));
            var w2 = new Mat(ny, 1, MatType.CV_32F, Scalar.All(0));

            float dx = (float)(2.0 * Math.PI / nx);
            float x = (float)-Math.PI;
            for (int i = 0; i < nx; i++)
            {
                w1.Set<float>(0, i, 0.5f * (float)(Math.Tanh((x + gamma / 2) / beta) - Math.Tanh((x - gamma / 2) / beta)));
                x += dx;
            }

            float dy = (float)(2.0 * Math.PI / ny);
            float y = (float)-Math.PI;
            for (int i = 0; i < ny; i++)
            {
                w2.Set<float>(i, 0, 0.5f * (float)(Math.Tanh((y + gamma / 2) / beta) - Math.Tanh((y - gamma / 2) / beta)));
                y += dy;
            }

            var w = w2 * w1;
            var outputImg = new Mat();
            Cv2.Multiply(inputImg, w, outputImg);
            return outputImg;
        }

        Mat filter2DFreq(Mat inputImg, Mat H)
        {
            var planes = new Mat[] { inputImg.Clone(), new Mat(inputImg.Size(), MatType.CV_32F, Scalar.All(0)) };
            var complexI = new Mat();
            Cv2.Merge(planes, complexI);
            Cv2.Dft(complexI, complexI, DftFlags.Scale);
            var planesH = new Mat[] { H.Clone(), new Mat(H.Size(), MatType.CV_32F, Scalar.All(0)) };
            var complexH = new Mat();
            Cv2.Merge(planesH, complexH);
            var complexIH = new Mat();
            Cv2.MulSpectrums(complexI, complexH, complexIH, 0);
            Cv2.Idft(complexIH, complexIH);
            planes = Cv2.Split(complexIH);
            return planes[0];
        }

        public CS_BlurMotion_Deblur(VBtask task) : base(task)
        {
            mblur = new CS_BlurMotion_Basics(task);
            desc = "Deblur a motion blurred image";
            labels[2] = "Blurred Image Input";
            labels[3] = "Deblurred Image Output";
        }

        public void RunCS(Mat src)
        {
            mblur.options.RunVB();

            if (task.heartBeat)
            {
                mblur.options.redoCheckBox.Checked = true;
            }

            if (mblur.options.redoCheckBox.Checked)
            {
                mblur.RunAndMeasure(src, mblur);
                mblur.options.showDirection = false;
                mblur.options.redoCheckBox.Checked = false;
            }
            else
            {
                mblur.RunAndMeasure(src, mblur);
            }

            dst2 = mblur.dst2;
            double beta = 0.2;

            int width = src.Width;
            int height = src.Height;
            var roi = new Rect(0, 0, width % 2 == 0 ? width : width - 1, height % 2 == 0 ? height : height - 1);

            var h = calcPSF(roi.Size, mblur.options.restoreLen, mblur.options.theta);
            var hW = calcWeinerFilter(h, 1.0 / mblur.options.SNR);

            var gray8u = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            var imgIn = new Mat();
            gray8u.ConvertTo(imgIn, MatType.CV_32F);
            imgIn = edgeTaper(imgIn, mblur.options.gamma, beta);

            var imgOut = filter2DFreq(imgIn[roi], hW);
            imgOut.ConvertTo(dst3, MatType.CV_8U);
            dst3.Normalize(0, 255, NormTypes.MinMax);
        }
    }



    public class CS_Boundary_Basics : CS_Parent
    {
        public RedCloud_CPP redCPP = new RedCloud_CPP();
        public List<Rect> rects = new List<Rect>();
        public List<Mat> masks = new List<Mat>();
        public List<List<cv.Point>> contours = new List<List<cv.Point>>();
        public bool runRedCPP = true;
        Color8U_Basics cvt;
        RedCloud_Reduce prep;
        GuidedBP_Depth guided;

        public CS_Boundary_Basics(VBtask task) : base(task)
        {
            cvt = new Color8U_Basics();
            prep = new RedCloud_Reduce();
            guided = new GuidedBP_Depth();
            task.redOptions.setColorSource("Bin4Way_Regions");
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Create a mask of the RedCloud cell boundaries";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
            {
                if (task.redOptions.useColorOnlyChecked)
                {
                    cvt.Run(src);
                    dst1 = cvt.dst2;
                }
                else if (task.redOptions.useDepthChecked)
                {
                    prep.Run(src);
                    dst1 = prep.dst2;
                }
                else
                {
                    guided.Run(src);
                    dst1 = guided.dst2;
                }
            }

            if (runRedCPP)
            {
                redCPP.Run(dst1);

                dst2.SetTo(0);
                rects.Clear();
                masks.Clear();
                contours.Clear();
                for (int i = 1; i < redCPP.classCount; i++)
                {
                    var rect = redCPP.rectList[i - 1];
                    var mask = redCPP.dst2[rect].InRange(i, i);
                    var contour = ContourBuild(mask, ContourApproximationModes.ApproxNone);
                    DrawContour(dst2[rect], contour, 255, task.lineWidth);
                    rects.Add(rect);
                    masks.Add(mask);
                    contours.Add(contour);
                }

                labels[2] = $"{redCPP.classCount} cells were found.";
            }
        }
    }

    public class CS_Boundary_Tiers : CS_Parent
    {
        Boundary_Basics cells = new Boundary_Basics();
        Contour_DepthTiers contours = new Contour_DepthTiers();

        public CS_Boundary_Tiers(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Add the depth tiers to the cell boundaries";
        }

        public void RunCS(Mat src)
        {
            cells.Run(src);
            dst3 = cells.dst2;

            contours.Run(src);
            dst2.SetTo(0);
            foreach (var tour in contours.contourlist)
            {
                DrawContour(dst2, tour.ToList(), 255, 2);
            }
            labels[2] = $"{contours.contourlist.Count} depth tiers were found.";
            labels[3] = cells.labels[2];
        }
    }

    public class CS_Boundary_Rectangles : CS_Parent
    {
        public Boundary_Basics bounds = new Boundary_Basics();
        public List<Rect> rects = new List<Rect>();
        public List<Rect> smallRects = new List<Rect>();
        public List<List<cv.Point>> smallContours = new List<List<cv.Point>>();
        public Options_BoundaryRect options = new Options_BoundaryRect();
        public CS_Boundary_Rectangles(VBtask task) : base(task)
        {
            desc = "Build the boundaries for redCells and remove interior rectangles";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            bounds.Run(src);

            dst2.SetTo(0);
            foreach (var r in bounds.rects)
            {
                dst2.Rectangle(r, task.HighlightColor, task.lineWidth);
            }
            labels[2] = $"{bounds.rects.Count} rectangles before contain test";

            rects.Clear();
            smallRects.Clear();
            smallContours.Clear();
            for (int i = 0; i < bounds.rects.Count * options.percentRect; i++)
            {
                rects.Add(bounds.rects[i]);
            }
            for (int i = bounds.rects.Count - 1; i >= (int)(bounds.rects.Count * options.percentRect); i--)
            {
                var r = bounds.rects[i];
                bool contained = false;
                foreach (var rect in bounds.rects)
                {
                    if (r == rect) continue;
                    if (rect.Contains(r))
                    {
                        contained = true;
                        break;
                    }
                }

                if (contained)
                {
                    smallContours.Add(bounds.contours[i]);
                    smallRects.Add(r);
                }
                else
                {
                    rects.Add(r);
                }
            }

            dst3.SetTo(0);
            foreach (var r in rects)
            {
                dst3.Rectangle(r, task.HighlightColor, task.lineWidth);
            }
            labels[3] = $"{rects.Count} rectangles after contain test";
        }
    }

    public class CS_Boundary_RemovedRects : CS_Parent
    {
        public Boundary_Rectangles bRects = new Boundary_Rectangles();

        public CS_Boundary_RemovedRects(VBtask task) : base(task)
        {
            if (standalone) task.gOptions.setDisplay1();
            desc = "Build the boundaries for redCells and remove interior rectangles";
        }

        public void RunCS(Mat src)
        {
            bRects.Run(src);
            dst2 = bRects.bounds.dst2.Clone();
            dst3 = bRects.dst2;
            dst1 = bRects.dst3;
            labels[3] = $"{bRects.bounds.rects.Count} cells before contain test";

            for (int i = 0; i < bRects.smallRects.Count; i++)
            {
                DrawContour(dst2[bRects.smallRects[i]], bRects.smallContours[i], Scalar.Black, task.lineWidth);
            }
            labels[1] = labels[2];
            labels[2] = $"{bRects.bounds.rects.Count - bRects.smallRects.Count} cells after contain test";
        }
    }

    public class CS_Boundary_Overlap : CS_Parent
    {
        Boundary_Basics bounds = new Boundary_Basics();

        public CS_Boundary_Overlap(VBtask task) : base(task)
        {
            dst2 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            desc = "Determine if 2 contours overlap";
        }

        public void RunCS(Mat src)
        {
            bounds.Run(src);
            dst3 = bounds.dst2;
            bool overlapping = false;
            for (int i = 0; i < bounds.contours.Count; i++)
            {
                var tour = bounds.contours[i];
                var rect = bounds.rects[i];
                for (int j = i + 1; j < bounds.contours.Count; j++)
                {
                    var r = bounds.rects[j];
                    if (r.IntersectsWith(rect))
                    {
                        dst2.SetTo(0);
                        int c1 = tour.Count;
                        int c2 = bounds.contours[j].Count;
                        DrawContour(dst2[rect], tour, 127, task.lineWidth);
                        DrawContour(dst2[r], bounds.contours[j], 255, task.lineWidth);
                        int count = dst2.CountNonZero();
                        if (count != c1 + c2)
                        {
                            overlapping = true;
                            break;
                        }
                    }
                }
                if (overlapping) break;
            }
        }
    }




    public class CS_Brightness_Basics : CS_Parent
    {
        Options_BrightnessContrast Options = new Options_BrightnessContrast();

        public CS_Brightness_Basics(VBtask task) : base(task)
        {
            desc = "Implement a brightness effect";
        }

        public void RunCS(Mat src)
        {
            Options.RunVB();

            dst2 = src.ConvertScaleAbs(Options.brightness, Options.contrast);
            labels[3] = "Brightness level = " + Options.contrast.ToString();
        }
    }

    // https://github.com/spmallick/learnopencv/blob/master/Photoshop-Filters-in-OpenCV/brightness.cpp
    public class CS_Brightness_HSV : CS_Parent
    {
        Options_BrightnessContrast Options = new Options_BrightnessContrast();

        public CS_Brightness_HSV(VBtask task) : base(task)
        {
            labels[3] = "HSV image";
            desc = "Implement the brightness effect for HSV images";
        }

        public void RunCS(Mat src)
        {
            Options.RunVB();

            dst3 = src.CvtColor(ColorConversionCodes.BGR2HSV);
            Mat hsv64 = new Mat();
            dst3.ConvertTo(hsv64, MatType.CV_64F);
            Mat[] split = hsv64.Split();

            split[1] *= Options.hsvBrightness;
            split[1] = split[1].Threshold(255, 255, ThresholdTypes.Trunc);

            split[2] *= Options.hsvBrightness;
            split[2] = split[2].Threshold(255, 255, ThresholdTypes.Trunc);

            Cv2.Merge(split, hsv64);
            hsv64.ConvertTo(dst2, MatType.CV_8UC3);
            dst2 = dst2.CvtColor(ColorConversionCodes.HSV2BGR);
            labels[2] = "Brightness level = " + Options.hsvBrightness.ToString();
        }
    }



    public class CS_BRISK_Basics : CS_Parent
    {
        BRISK brisk;
        public List<cv.Point2f> features = new List<cv.Point2f>();
        Options_Features options = new Options_Features();

        public CS_BRISK_Basics(VBtask task) : base(task)
        {
            brisk = BRISK.Create();
            UpdateAdvice(traceName + ": only the 'Min Distance' option affects the BRISK results.");
            desc = "Detect features with BRISK";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            src.CopyTo(dst2);

            if (src.Channels() == 3)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            KeyPoint[] keyPoints = brisk.Detect(src);

            features.Clear();
            foreach (var pt in keyPoints)
            {
                if (pt.Size > options.minDistance)
                {
                    features.Add(new cv.Point2f(pt.Pt.X, pt.Pt.Y));
                    DrawCircle(dst2, pt.Pt, task.DotSize + 1, task.HighlightColor);
                }
            }
            labels[2] = features.Count + " features found with BRISK";
        }
    }





    public class CS_BackProject2D_Basics : CS_Parent
    {
        public Hist2D_Basics hist2d = new Hist2D_Basics();
        public Color_Basics colorFmt = new Color_Basics();
        public bool backProjectByGrid;
        public int classCount;

        public CS_BackProject2D_Basics(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": the global option 'Histogram Bins' controls the histogram.");
            desc = "A 2D histogram is built from 2 channels of any 3-channel input and the results are displayed.";
        }

        public void RunCS(Mat src)
        {
            int index = task.gridMap.At<int>(task.mouseMovePoint.Y, task.mouseMovePoint.X);
            var roi = task.gridList[index];

            colorFmt.Run(src);
            hist2d.Run(colorFmt.dst2);
            dst2 = hist2d.dst2;

            if (standaloneTest())
            {
                dst2.Rectangle(roi, Scalar.White, task.lineWidth, task.lineType);
            }

            Mat histogram = new Mat();
            if (backProjectByGrid)
            {
                task.gridMap.ConvertTo(histogram, MatType.CV_32F);
            }
            else
            {
                histogram = new Mat(hist2d.histogram.Size(), MatType.CV_32F, 0);
                hist2d.histogram[roi].CopyTo(histogram[roi]);
            }
            Cv2.CalcBackProject(new[] { colorFmt.dst2 }, hist2d.channels, histogram, dst0, hist2d.ranges);

            int bpCount = hist2d.histogram[roi].CountNonZero();

            if (backProjectByGrid)
            {
                var mm = GetMinMax(dst0);
                classCount = (int)mm.maxVal;
                task.palette.Run(dst0 * 255 / classCount);
                dst3 = task.palette.dst2;
            }
            else
            {
                dst3.SetTo(Scalar.All(0));
                dst3.SetTo(Scalar.Yellow, dst0);
            }
            if (task.heartBeat)
            {
                labels[2] = colorFmt.options.colorFormat + " format " + (classCount > 0 ? classCount + " classes" : " ");
                int c1 = task.redOptions.channels[0], c2 = task.redOptions.channels[1];
                labels[3] = "That combination of channel " + c1 + "/" + c2 + " has " + bpCount +
                            " pixels while image total is " + dst0.Total().ToString("0");
            }
            SetTrueText("Use Global Algorithm Option 'Grid Square Size' to control the 2D backprojection",
                        new cv.Point(10, dst3.Height - 20), 3);
        }
    }





    public class CS_BackProject2D_Compare : CS_Parent
    {
        PhotoShop_Hue hueSat = new PhotoShop_Hue();
        CS_BackProject2D_Basics backP;
        Mat_4Click mats = new Mat_4Click();

        public CS_BackProject2D_Compare(VBtask task) : base(task)
        {
            backP = new CS_BackProject2D_Basics(task);
            labels[2] = "Hue (upper left), sat (upper right), highlighted backprojection (bottom left)";
            if (standaloneTest()) task.gOptions.setGridSize(10);
            desc = "Compare the hue and brightness images and the results of the Hist_backprojection2d";
        }

        public void RunCS(Mat src)
        {
            hueSat.Run(src.Clone());
            mats.mat[0] = hueSat.dst2;
            mats.mat[1] = hueSat.dst3;

            backP.RunAndMeasure(src, backP);
            mats.mat[2] = backP.dst3;

            if (task.FirstPass) mats.quadrant = RESULT_DST3;
            mats.Run(Mat.Zeros(src.Size(), MatType.CV_8UC3));
            dst2 = mats.dst2;
            dst3 = mats.dst3;

            labels[3] = backP.labels[3];

            SetTrueText("Use Global Algorithm Option 'Grid Square Size' to control this 2D histogram.\n" +
                        "Move mouse in 2D histogram to select a cell to backproject.\n" +
                        "Click any quadrant at left to display that quadrant here.\n",
                        new cv.Point(10, dst3.Height - dst3.Height / 4), 3);
        }
    }

    





    public class CS_BackProject2D_Top : CS_Parent
    {
        HeatMap_Basics heat = new HeatMap_Basics();

        public CS_BackProject2D_Top(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Top Down HeatMap", "BackProject2D for the top-down view" };
            desc = "Backproject the output of the Top View.";
        }

        public void RunCS(Mat src)
        {
            heat.Run(src);
            dst2 = heat.dst2;

            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsTop, heat.histogramTop, dst3, task.rangesTop);
            dst3 = GetNormalize32f(dst3);
            dst3 = ShowPalette(dst3);
        }
    }

    public class CS_BackProject2D_Side : CS_Parent
    {
        HeatMap_Basics heat = new HeatMap_Basics();

        public CS_BackProject2D_Side(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Side View HeatMap", "BackProject2D for the side view" };
            desc = "Backproject the output of the Side View.";
        }

        public void RunCS(Mat src)
        {
            heat.Run(src);
            dst2 = heat.dst3;

            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsSide, heat.histogramSide, dst3, task.rangesSide);
            dst3 = GetNormalize32f(dst3);
            dst3 = ShowPalette(dst3);
        }
    }

    public class CS_BackProject2D_Filter : CS_Parent
    {
        public int threshold;

        public CS_BackProject2D_Filter(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_32FC3, 0);
            task.gOptions.setHistogramBins(100); // extra bins to help isolate the stragglers.
            desc = "Filter a 2D histogram for the backprojection.";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                Cv2.CalcHist(new Mat[] { task.pointCloud }, task.channelsSide, new Mat(), dst1, 2, task.bins2D, task.rangesSide);
            }
            dst1.Col(0).SetTo(0);

            dst2 = dst1.Threshold(threshold, 255, cv.ThresholdTypes.Binary);
        }
    }





    public class CS_BackProject2D_FilterSide : CS_Parent
    {
        public BackProject2D_Filter filter = new BackProject2D_Filter();
        Options_HistXD options = new Options_HistXD();

        public CS_BackProject2D_FilterSide(VBtask task) : base(task)
        {
            desc = "Backproject the output of the Side View after removing low sample bins.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Mat histogram = new Mat();
            Cv2.CalcHist(new Mat[] { task.pointCloud }, task.channelsSide, new Mat(), histogram, 2, task.bins2D, task.rangesSide);

            filter.threshold = options.sideThreshold;
            filter.histogram = histogram;
            filter.Run(src);

            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsSide, filter.dst2, dst1, task.rangesSide);
            dst1.ConvertTo(dst1, MatType.CV_8U);

            dst2.SetTo(0);
            task.pointCloud.CopyTo(dst2, dst1);
        }
    }





    public class CS_BackProject2D_FilterTop : CS_Parent
    {
        BackProject2D_Filter filter = new BackProject2D_Filter();
        Options_HistXD options = new Options_HistXD();

        public CS_BackProject2D_FilterTop(VBtask task) : base(task)
        {
            desc = "Backproject the output of the Side View after removing low sample bins.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Mat histogram = new Mat();
            Cv2.CalcHist(new Mat[] { task.pointCloud }, task.channelsSide, new Mat(), histogram, 2, task.bins2D, task.rangesSide);

            filter.threshold = options.topThreshold;
            filter.histogram = histogram;
            filter.Run(src);

            Cv2.CalcBackProject(new Mat[] { task.pointCloud }, task.channelsTop, filter.dst2, dst1, task.rangesTop);
            dst1.ConvertTo(dst1, MatType.CV_8U);

            dst2.SetTo(0);
            task.pointCloud.CopyTo(dst2, dst1);
        }
    }

    public class CS_BackProject2D_FilterBoth : CS_Parent
    {
        BackProject2D_FilterSide filterSide = new BackProject2D_FilterSide();
        BackProject2D_FilterTop filterTop = new BackProject2D_FilterTop();

        public CS_BackProject2D_FilterBoth(VBtask task) : base(task)
        {
            desc = "Backproject the output of the both the top and side views after removing low sample bins.";
        }

        public void RunCS(Mat src)
        {
            filterSide.Run(src);
            filterTop.Run(src);

            dst2.SetTo(0);
            task.pointCloud.CopyTo(dst2, filterSide.dst1);
            task.pointCloud.CopyTo(dst3, filterTop.dst1);
        }
    }




    public class CS_BackProject2D_Full : CS_Parent
    {
        CS_BackProject2D_Basics backP;
        public int classCount;

        public CS_BackProject2D_Full(VBtask task) : base(task)
        {
            backP = new CS_BackProject2D_Basics(task);
            backP.backProjectByGrid = true;
            desc = "Backproject the 2D histogram marking each grid element's backprojection";
        }

        public void RunCS(Mat src)
        {
            backP.RunAndMeasure(src, backP);
            dst2 = backP.dst0;
            dst3 = backP.dst3;
            classCount = backP.classCount;
            labels = backP.labels;
        }
    }



    public class CS_CameraMotion_Basics : CS_Parent
    {
        public int translationX;
        public int translationY;
        Gravity_Horizon gravity = new Gravity_Horizon();
        public bool secondOpinion;
        Swarm_Basics feat = new Swarm_Basics();
        PointPair gravityVec;
        PointPair horizonVec;
        public CS_CameraMotion_Basics(VBtask task) : base(task)
        {
            dst2 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            dst3 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            task.gOptions.setDebugSlider(3);
            desc = "Merge with previous image using just translation of the gravity vector and horizon vector (if present)";
        }

        public void RunCS(Mat src)
        {
            gravity.Run(src);

            if (task.FirstPass)
            {
                gravityVec = new PointPair(task.gravityVec.p1, task.gravityVec.p2);
                horizonVec = new PointPair(task.horizonVec.p1, task.horizonVec.p2);
            }

            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            translationX = task.gOptions.DebugSliderValue;
            translationY = task.gOptions.DebugSliderValue;

            if (Math.Abs(translationX) >= dst2.Width / 2)
                translationX = 0;

            if (horizonVec.p1.Y >= dst2.Height || horizonVec.p2.Y >= dst2.Height || Math.Abs(translationY) >= dst2.Height / 2)
            {
                horizonVec = new PointPair(new Point2f(), new Point2f(336, 0));
                translationY = 0;
            }

            Rect r1, r2;
            if (translationX == 0 && translationY == 0)
            {
                dst2 = src;
                task.camMotionPixels = 0;
                task.camDirection = 0;
            }
            else
            {
                r1 = new Rect(translationX, translationY, Math.Min(dst2.Width - translationX * 2, dst2.Width),
                              Math.Min(dst2.Height - translationY * 2, dst2.Height));
                if (r1.X < 0)
                {
                    r1.X = -r1.X;
                    r1.Width += translationX * 2;
                }
                if (r1.Y < 0)
                {
                    r1.Y = -r1.Y;
                    r1.Height += translationY * 2;
                }

                r2 = new Rect(Math.Abs(translationX), Math.Abs(translationY), r1.Width, r1.Height);

                task.camMotionPixels = (float)Math.Sqrt(translationX * translationX + translationY * translationY);
                if (translationX == 0)
                {
                    if (translationY < 0)
                        task.camDirection = (float)Math.PI / 4;
                    else
                        task.camDirection = (float)Math.PI * 3 / 4;
                }
                else
                {
                    task.camDirection = (float)Math.Atan2(translationY, translationX);
                }

                if (secondOpinion)
                {
                    dst3.SetTo(0);
                    feat.Run(src);
                    strOut = "Swarm distance = " + feat.distanceAvg.ToString("F1") + " when camMotionPixels = " + task.camMotionPixels.ToString("F1");
                    if (feat.distanceAvg < task.camMotionPixels / 2 || task.heartBeat)
                    {
                        task.camMotionPixels = 0;
                        src.CopyTo(dst2);
                    }
                    dst3 = (src - dst2).ToMat().Threshold(task.gOptions.pixelDiffThreshold, 255, ThresholdTypes.Binary);
                }
            }

            gravityVec = new PointPair(task.gravityVec.p1, task.gravityVec.p2);
            horizonVec = new PointPair(task.horizonVec.p1, task.horizonVec.p2);
            SetTrueText(strOut, 3);

            labels[2] = "Translation (X, Y) = (" + translationX.ToString() + ", " + translationY.ToString() + ")" +
                        (horizonVec.p1.Y == 0 && horizonVec.p2.Y == 0 ? " there is no horizon present" : "");
            labels[3] = "Camera direction (radians) = " + task.camDirection.ToString("F1") + " with distance = " + task.camMotionPixels.ToString("F1");
        }
    }




    public class CS_CameraMotion_WithRotation : CS_Parent
    {
        public float translationX;
        public float rotationX;
        public Point2f centerX;
        public float translationY;
        public float rotationY;
        public Point2f centerY;
        public Rotate_BasicsQT rotate = new Rotate_BasicsQT();
        PointPair gravityVec;
        PointPair horizonVec;
        public CS_CameraMotion_WithRotation(VBtask task) : base(task)
        {
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, Scalar.All(0));
            dst3 = new Mat(dst1.Size(), MatType.CV_8U, Scalar.All(0));
            desc = "Merge with previous image using rotation AND translation of the camera motion - not as good as translation alone.";
        }
        public void translateRotateX(int x1, int x2)
        {
            rotationX = (float)(Math.Atan(Math.Abs(x1 - x2) / (double)dst2.Height) * 57.2958);
            centerX = new Point2f((task.gravityVec.p1.X + task.gravityVec.p2.X) / 2, (task.gravityVec.p1.Y + task.gravityVec.p2.Y) / 2);
            if (x1 >= 0 && x2 > 0)
            {
                translationX = x1 > x2 ? x1 - x2 : x2 - x1;
                centerX = task.gravityVec.p2;
            }
            else if (x1 <= 0 && x2 < 0)
            {
                translationX = x1 > x2 ? x1 - x2 : x2 - x1;
                centerX = task.gravityVec.p1;
            }
            else if (x1 < 0 && x2 > 0)
            {
                translationX = 0;
            }
            else
            {
                translationX = 0;
                rotationX *= -1;
            }
        }
        public void translateRotateY(int y1, int y2)
        {
            rotationY = (float)(Math.Atan(Math.Abs(y1 - y2) / (double)dst2.Width) * 57.2958);
            centerY = new Point2f((task.horizonVec.p1.X + task.horizonVec.p2.X) / 2, (task.horizonVec.p1.Y + task.horizonVec.p2.Y) / 2);
            if (y1 > 0 && y2 > 0)
            {
                translationY = y1 > y2 ? y1 - y2 : y2 - y1;
                centerY = task.horizonVec.p2;
            }
            else if (y1 < 0 && y2 < 0)
            {
                translationY = y1 > y2 ? y1 - y2 : y2 - y1;
                centerY = task.horizonVec.p1;
            }
            else if (y1 < 0 && y2 > 0)
            {
                translationY = 0;
            }
            else
            {
                translationY = 0;
                rotationY *= -1;
            }
        }
        public void RunCS(Mat src)
        {
            if (task.FirstPass)
            {
                gravityVec = task.gravityVec;
                horizonVec = task.horizonVec;
            }
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            int x1 = (int)(gravityVec.p1.X - task.gravityVec.p1.X);
            int x2 = (int)(gravityVec.p2.X - task.gravityVec.p2.X);
            int y1 = (int)(horizonVec.p1.Y - task.horizonVec.p1.Y);
            int y2 = (int)(horizonVec.p2.Y - task.horizonVec.p2.Y);
            translateRotateX(x1, x2);
            translateRotateY(y1, y2);
            dst1.SetTo(Scalar.All(0));
            dst3.SetTo(Scalar.All(0));
            if (Math.Abs(x1 - x2) > 0.5 || Math.Abs(y1 - y2) > 0.5)
            {
                Rect r1 = new Rect((int)translationX, (int)translationY, dst2.Width - (int)translationX, dst2.Height - (int)translationY);
                Rect r2 = new Rect(0, 0, r1.Width, r1.Height);
                src[r1].CopyTo(dst1[r2]);
                rotate.rotateAngle = rotationY;
                rotate.rotateCenter = centerY;
                rotate.Run(dst1);
                dst2 = rotate.dst2;
                dst3 = (src - dst2).ToMat().Threshold(task.gOptions.pixelDiffThreshold, 255, ThresholdTypes.Binary);
            }
            else
            {
                dst2 = src;
            }
            gravityVec = task.gravityVec;
            horizonVec = task.horizonVec;
            labels[2] = "Translation X = " + translationX.ToString(fmt1) + " rotation X = " + rotationX.ToString(fmt1) + " degrees " +
                        " center of rotation X = " + centerX.X.ToString(fmt0) + ", " + centerX.Y.ToString(fmt0);
            labels[3] = "Translation Y = " + translationY.ToString(fmt1) + " rotation Y = " + rotationY.ToString(fmt1) + " degrees " +
                        " center of rotation Y = " + centerY.X.ToString(fmt0) + ", " + centerY.Y.ToString(fmt0);
        }
    }
    public class CS_CameraMotion_SceneMotion : CS_Parent
    {
        CameraMotion_Basics cMotion = new CameraMotion_Basics();
        Motion_Basics motion = new Motion_Basics();
        public CS_CameraMotion_SceneMotion(VBtask task) : base(task)
        {
            labels[2] = "Image after adjusting for camera motion.";
            desc = "Display both camera motion (on heartbeats) and scene motion.";
        }
        public void RunCS(Mat src)
        {
            cMotion.Run(src);
            dst2 = cMotion.dst3;
            motion.Run(src);
            dst3 = motion.dst2.Threshold(0, 255, ThresholdTypes.Binary);
        }
    }




    public class CS_CamShift_Basics : CS_Parent
    {
        public RotatedRect trackBox = new RotatedRect();
        CamShift_RedHue redHue = new CamShift_RedHue();
        Rect roi = new Rect();
        Mat histogram = new Mat();
        public CS_CamShift_Basics(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": Draw on any available red hue area.");
            labels[2] = "Draw anywhere to create histogram and start camshift";
            labels[3] = "Histogram of targeted region (hue only)";
            UpdateAdvice(traceName + ": click 'Show All' to control camShift options.");
            desc = "CamShift Demo - draw on the images to define the object to track.";
        }
        public void RunCS(Mat src)
        {
            redHue.RunVB(src);
            dst2 = redHue.dst2;
            Mat hue = redHue.dst1;
            Mat mask = redHue.dst3;
            Rangef[] ranges = { new Rangef(0, 180) };
            int[] hsize = { task.histogramBins };
            task.drawRect = ValidateRect(task.drawRect);
            Cv2.CalcHist(new Mat[] { hue[task.drawRect] }, new int[] { 0 }, mask[task.drawRect], histogram, 1, hsize, ranges);
            histogram = histogram.Normalize(0, 255, NormTypes.MinMax);
            roi = task.drawRect;
            if (histogram.Rows != 0)
            {
                Cv2.CalcBackProject(new Mat[] { hue }, new int[] { 0 }, histogram, dst1, ranges);
                trackBox = Cv2.CamShift(dst1 & mask, ref roi, new TermCriteria(cv.CriteriaTypes.MaxIter, 10, 1));
                dst3 = Show_HSV_Hist(histogram);
                if (dst3.Channels() == 1) dst3 = src;
                dst3 = dst3.CvtColor(ColorConversionCodes.HSV2BGR);
            }
            if (trackBox.Size.Width > 0)
            {
                dst2.Ellipse(trackBox, Scalar.White, task.lineWidth + 1, task.lineType);
            }
        }
    }
    public class CS_CamShift_RedHue : CS_Parent
    {
        Options_CamShift options = new Options_CamShift();
        public CS_CamShift_RedHue(VBtask task) : base(task)
        {
            labels = new string[] { "", "Hue", "Image regions with red hue", "Mask for hue regions" };
            desc = "Find that portion of the image where red dominates";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            dst3 = hsv.InRange(options.camSBins, new Scalar(180, 255, options.camMax));
            dst2.SetTo(0);
            src.CopyTo(dst2, dst3);
        }
    }





    public class CS_Cartoonify_Basics : CS_Parent
    {
        Options_Cartoonify options = new Options_Cartoonify();
        public CS_Cartoonify_Basics(VBtask task) : base(task)
        {
            labels[2] = "Mask for Cartoon";
            labels[3] = "Cartoonify Result";
            UpdateAdvice(traceName + ": click 'Show All' to control cartoonify options.");
            desc = "Create a cartoon from a color image";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat gray8u = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            gray8u = gray8u.MedianBlur(options.medianBlur);
            Mat edges = gray8u.Laplacian(MatType.CV_8U, options.kernelSize);
            Mat mask = edges.Threshold(options.threshold, 255, ThresholdTypes.Binary);
            dst2 = mask.CvtColor(ColorConversionCodes.GRAY2BGR);
            dst3 = src.MedianBlur(options.medianBlur2).MedianBlur(options.medianBlur2);
            src.CopyTo(dst3, mask);
        }
    }






    public class CS_Pixel_Unstable : CS_Parent
    {
        KMeans_Basics km = new KMeans_Basics();
        List<int> pixelCounts = new List<int>();
        int k = -1;
        List<Mat> unstable = new List<Mat>();
        Mat lastImage;
        public Mat unstablePixels = new Mat();
        TrackBar kSlider;
        public CS_Pixel_Unstable(VBtask task) : base(task)
        {
            task.gOptions.setPixelDifference(2);
            kSlider = FindSlider("KMeans k");
            labels[2] = "KMeans_Basics output";
            desc = "Detect where pixels are unstable";
        }
        public void RunCS(Mat src)
        {
            k = kSlider.Value;
            km.Run(src);
            dst2 = km.dst2;
            dst2.ConvertTo(dst2, MatType.CV_32F);
            if (lastImage == null) lastImage = dst2.Clone();
            Cv2.Subtract(dst2, lastImage, dst3);
            dst3 = dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, ThresholdTypes.Binary);
            unstable.Add(dst3);
            if (unstable.Count > task.frameHistoryCount) unstable.RemoveAt(0);
            unstablePixels = unstable[0];
            for (int i = 1; i < unstable.Count; i++)
            {
                unstablePixels = unstablePixels | unstable[i];
            }
            dst3 = unstablePixels;
            int unstableCount = dst3.CountNonZero();
            pixelCounts.Add(unstableCount);
            if (pixelCounts.Count > 100) pixelCounts.RemoveAt(0);
            // compute stdev from the list
            double avg = pixelCounts.Average();
            double sum = pixelCounts.Sum(d => Math.Pow(d - avg, 2));
            double stdev = Math.Sqrt(sum / pixelCounts.Count);
            labels[3] = "Unstable pixel count = " + avg.ToString("###,##0") + "    stdev = " + stdev.ToString("0.0");
            lastImage = dst2.Clone();
        }
    }




    public class CS_CComp_Basics : CS_Parent
    {
        public ConnectedComponents connectedComponents;
        public List<Rect> rects = new List<Rect>();
        public List<Point2f> centroids = new List<Point2f>();
        Mat lastImage;
        Options_CComp options = new Options_CComp();
        public CS_CComp_Basics(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            UpdateAdvice(traceName + ": only the local options for threshold is used in CS_CComp_Basics.");
            labels[2] = "Input to ConnectedComponenetsEx";
            desc = "Draw bounding boxes around BGR binarized connected Components";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            rects.Clear();
            centroids.Clear();
            Mat input = src;
            if (input.Channels() == 3)
                input = input.CvtColor(ColorConversionCodes.BGR2GRAY);
            dst2 = input.Threshold(options.threshold, 255, ThresholdTypes.BinaryInv);
            connectedComponents = Cv2.ConnectedComponentsEx(dst2);
            connectedComponents.RenderBlobs(dst3);
            int count = 0;
            foreach (var blob in connectedComponents.Blobs)
            {
                var rect = ValidateRect(blob.Rect);
                var m = Cv2.Moments(dst2[rect], true);
                if (m.M00 == 0) continue; // avoid divide by zero...
                rects.Add(rect);
                centroids.Add(new Point2f((float)(m.M10 / m.M00 + rect.X), (float)(m.M01 / m.M00 + rect.Y)));
                count++;
            }
            lastImage = dst2;
            labels[3] = count + " items found ";
        }
    }




    public class CS_CComp_Shapes : CS_Parent
    {
        Mat shapes;
        Mat_4Click mats = new Mat_4Click();
        public CS_CComp_Shapes(VBtask task) : base(task)
        {
            shapes = new Mat(task.HomeDir + "Data/Shapes.png", ImreadModes.Color);
            labels[2] = "Largest connected component";
            labels[3] = "RectView, LabelView, Binary, grayscale";
            desc = "Use connected components to isolate objects in image.";
        }
        public void RunCS(Mat src)
        {
            var gray = shapes.CvtColor(ColorConversionCodes.BGR2GRAY);
            var binary = gray.Threshold(0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
            var labelview = shapes.EmptyClone();
            var rectView = binary.CvtColor(ColorConversionCodes.GRAY2BGR);
            var cc = Cv2.ConnectedComponentsEx(binary);
            if (cc.LabelCount <= 1) return;
            cc.RenderBlobs(labelview);
            foreach (var blob in cc.Blobs.Skip(1))
            {
                rectView.Rectangle(blob.Rect, Scalar.Red, 2);
            }
            var maxBlob = cc.GetLargestBlob();
            var filtered = new Mat();
            cc.FilterByBlob(shapes, filtered, maxBlob);
            mats.mat[0] = rectView;
            mats.mat[1] = labelview;
            mats.mat[2] = binary;
            mats.mat[3] = gray;
            mats.Run(empty);
            dst2 = mats.dst2;
            dst3 = mats.dst3;
        }
    }




    public class CS_CComp_Both : CS_Parent
    {
        CComp_Stats above = new CComp_Stats();
        CComp_Stats below = new CComp_Stats();
        public CS_CComp_Both(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Connected components in both the lighter and darker halves", "Connected components in the darker half of the image" };
            desc = "Prepare the connected components for both above and below the threshold";
        }
        public void RunCS(Mat src)
        {
            above.options.RunVB();
            var light = src.Threshold(above.options.light, 255, ThresholdTypes.Binary);
            below.Run(light);
            dst2 = below.dst3;
            dst1 = below.dst1;
            labels[3] = above.labels[3];
        }
    }



    public class CS_CComp_Hulls : CS_Parent
    {
        CComp_Both ccomp = new CComp_Both();
        RedCloud_Hulls hulls = new RedCloud_Hulls();
        public CS_CComp_Hulls(VBtask task) : base(task)
        {
            desc = "Create connected components using RedCloud Hulls";
        }
        public void RunCS(Mat src)
        {
            ccomp.Run(src.CvtColor(ColorConversionCodes.BGR2GRAY));
            dst2 = ccomp.dst3;
            ccomp.dst1.ConvertTo(dst1, MatType.CV_8U);
            hulls.Run(dst1);
            dst2 = hulls.dst3;
            labels[2] = hulls.labels[3];
        }
    }





    // https://docs.opencv.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
    public class CS_CComp_Stats : CS_Parent
    {
        public List<Mat> masks = new List<Mat>();
        public List<Rect> rects = new List<Rect>();
        public List<int> areas = new List<int>();
        public List<cv.Point> centroids = new List<cv.Point>();
        public int numberOfLabels;
        public Options_CComp options = new Options_CComp();
        public CS_CComp_Stats(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            desc = "Use a threshold slider on the CComp input";
        }
        public void RunCS(Mat src)
        {
            dst2 = src;
            options.RunVB();
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (standaloneTest())
                src = src.Threshold(options.light, 255, ThresholdTypes.BinaryInv);
            Mat stats = new Mat();
            Mat centroidRaw = new Mat();
            numberOfLabels = Cv2.ConnectedComponentsWithStats(src, dst1, stats, centroidRaw);
            rects.Clear();
            areas.Clear();
            centroids.Clear();
            List<Vec3b> colors = new List<Vec3b>();
            SortedList<float, int> maskOrder = new SortedList<float, int>(new compareAllowIdenticalSingleInverted());
            List<Mat> unsortedMasks = new List<Mat>();
            List<Rect> unsortedRects = new List<Rect>();
            List<cv.Point> unsortedCentroids = new List<cv.Point>();
            List<int> index = new List<int>();
            for (int i = 0; i < Math.Min(256, stats.Rows); i++)
            {
                int area = stats.Get<int>(i, 4);
                if (area < 10) continue;
                Rect r1 = ValidateRect(stats.Get<Rect>(i, 0));
                Rect r = ValidateRect(new Rect(r1.X, r1.Y, r1.Width, r1.Height));
                if ((r.Width == dst2.Width && r.Height == dst2.Height) || (r.Width == 1 && r.Height == 1)) continue;
                areas.Add(area);
                unsortedRects.Add(r);
                dst2.Rectangle(r, task.HighlightColor, task.lineWidth);
                index.Add(i);
                colors.Add(task.vecColors[colors.Count]);
                maskOrder.Add(area, unsortedMasks.Count);
                unsortedMasks.Add(dst1.InRange(i, i)[r]);
                cv.Point c = new cv.Point((int)centroidRaw.Get<double>(i, 0), (int)centroidRaw.Get<double>(i, 1));
                unsortedCentroids.Add(c);
            }
            masks.Clear();
            for (int i = 0; i < maskOrder.Count; i++)
            {
                int mIndex = maskOrder.ElementAt(i).Value;
                masks.Add(unsortedMasks[mIndex]);
                rects.Add(unsortedRects[mIndex]);
                centroids.Add(unsortedCentroids[mIndex]);
            }
            dst1.ConvertTo(dst0, MatType.CV_8U);
            dst3 = ShowPalette(dst0 * 255 / centroids.Count);
            labels[3] = masks.Count + " Connected Components";
        }
    }




    public class CS_Cell_Basics : CS_Parent
    {
        Hist_Depth plot = new Hist_Depth();
        PCA_Basics pca = new PCA_Basics();
        Plane_Equation eq = new Plane_Equation();
        public bool runRedCloud;
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Cell_Basics(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setHistogramBins(20);
            desc = "Display the statistics for the selected cell.";
        }
        public void statsString()
        {
            if (task.heartBeat)
            {
                var rc = task.rc;
                var gridID = task.gridMap.Get<int>(rc.maxDist.Y, rc.maxDist.X);
                strOut = "rc.index = " + rc.index.ToString() + "\t" + " gridID = " + gridID.ToString() + "\r\n";
                strOut += "rc.rect: " + rc.rect.X.ToString() + ", " + rc.rect.Y.ToString() + ", ";
                strOut += rc.rect.Width.ToString() + ", " + rc.rect.Height.ToString() + "\r\n" + "rc.color = " + rc.color.ToString() + "\r\n";
                strOut += "rc.maxDist = " + rc.maxDist.X.ToString() + "," + rc.maxDist.Y.ToString() + "\r\n";
                strOut += rc.depthPixels > 0 ? "Cell is marked as depthCell \r\n" : "";
                if (rc.depthPixels > 0)
                {
                    strOut += "depth pixels " + rc.pixels.ToString() + "\r\n" + "rc.depthPixels = " + rc.depthPixels.ToString() +
                          " or " + (rc.depthPixels / (float)rc.pixels).ToString("0%") + " depth \r\n";
                }
                else
                {
                    strOut += "depth pixels " + rc.pixels.ToString() + " - no depth data\r\n";
                }
                strOut += "Depth Min/Max/Range: X = " + rc.minVec.X.ToString(fmt1) + "/" + rc.maxVec.X.ToString(fmt1);
                strOut += "/" + (rc.maxVec.X - rc.minVec.X).ToString(fmt1) + "\t";
                strOut += "Y = " + rc.minVec.Y.ToString(fmt1) + "/" + rc.maxVec.Y.ToString(fmt1);
                strOut += "/" + (rc.maxVec.Y - rc.minVec.Y).ToString(fmt1) + "\t";
                strOut += "Z = " + rc.minVec.Z.ToString(fmt2) + "/" + rc.maxVec.Z.ToString(fmt2);
                strOut += "/" + (rc.maxVec.Z - rc.minVec.Z).ToString(fmt2) + "\r\n\r\n";
                strOut += "Cell Mean in 3D: x/y/z = \t" + rc.depthMean[0].ToString(fmt2) + "\t";
                strOut += rc.depthMean[1].ToString(fmt2) + "\t" + rc.depthMean[2].ToString(fmt2) + "\r\n";
                strOut += "Color Mean  RGB: \t" + rc.colorMean[0].ToString(fmt1) + "\t" + rc.colorMean[1].ToString(fmt1) + "\t";
                strOut += rc.colorMean[2].ToString(fmt1) + "\r\n";
                strOut += "Color Stdev RGB: \t" + rc.colorStdev[0].ToString(fmt1) + "\t" + rc.colorStdev[1].ToString(fmt1) + "\t";
                strOut += rc.colorStdev[2].ToString(fmt1) + "\r\n";
                var tmp = new Mat(task.rc.mask.Rows, task.rc.mask.Cols, MatType.CV_32F, 0);
                task.pcSplit[2][task.rc.rect].CopyTo(tmp, task.rc.mask);
                plot.rc = task.rc;
                plot.Run(tmp);
                dst1 = plot.dst2;
                // If rc.depthMean[2] == 0
                // {
                //     strOut += "\r\nNo depth data is available for that cell. ";
                // }
                // else
                // {
                //     eq.rc = rc;
                //     eq.Run(src);
                //     rc = eq.rc;
                //     strOut += "\r\n" + eq.strOut + "\r\n";
                //     pca.Run(empty);
                //     strOut += "\r\n" + pca.strOut;
                // }
            }
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest() || runRedCloud)
            {
                redC.Run(src);
                dst2 = redC.dst2;
                labels[2] = redC.labels[2];
            }

            statsString();
            SetTrueText(strOut, 3);
            labels[1] = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + ((int)task.MaxZmeters).ToString() + " meters";
        }
    }
    public class CS_Cell_PixelCountCompare : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Cell_PixelCountCompare(VBtask task) : base(task)
        {
            task.gOptions.setDebugCheckBox(true);
            desc = "The rc.mask is filled and may completely contain depth pixels.  This alg finds cells that contain depth islands.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            dst3.SetTo(0);
            int missCount = 0;
            foreach (var rc in task.redCells)
            {
                if (rc.depthPixels != 0)
                {
                    if (rc.pixels != rc.depthPixels)
                    {
                        dst3[rc.rect].SetTo(rc.color, rc.mask);
                        var pt = new cv.Point(rc.maxDist.X - 10, rc.maxDist.Y);
                        if (task.gOptions.getDebugCheckBox())
                        {
                            strOut = rc.pixels.ToString() + "/" + rc.depthPixels.ToString();
                        }
                        else
                        {
                            strOut = (rc.depthPixels / (float)rc.pixels).ToString("0%");
                        }
                        if (missCount < task.redOptions.identifyCount) SetTrueText(strOut, pt, 3);
                        missCount++;
                    }
                }
            }
            if (task.heartBeat) labels[3] = "There were " + missCount.ToString() + " cells containing depth - showing rc.pixels/rc.depthpixels";
        }
    }
    public class CS_Cell_ValidateColorCells : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Cell_ValidateColorCells(VBtask task) : base(task)
        {
            labels[3] = "Cells shown below have rc.depthPixels / rc.pixels < 50%";
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            desc = "Validate that all the depthCells are correctly identified.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            dst1.SetTo(0);
            dst3.SetTo(0);
            List<float> percentDepth = new List<float>();
            foreach (var rc in task.redCells)
            {
                if (rc.depthPixels > 0) dst1[rc.rect].SetTo(255, rc.mask);
                if (rc.depthPixels > 0 && rc.index > 0)
                {
                    float pc = rc.depthPixels / (float)rc.pixels;
                    percentDepth.Add(pc);
                    if (pc < 0.5f) dst3[rc.rect].SetTo(rc.color, rc.mask);
                }
            }
            int beforeCount = dst1.CountNonZero();
            dst1.SetTo(0, task.depthMask);
            int aftercount = dst1.CountNonZero();
            if (beforeCount != aftercount)
            {
                strOut = "There are color cells with depth in them - not good\r\n";
            }
            else
            {
                strOut = "There are no color cells with depth in them.\r\n";
            }
            if (percentDepth.Count > 0)
            {
                strOut += "Depth cell percentage average " + percentDepth.Average().ToString("0%") + "\r\n";
                strOut += "Depth cell percentage range " + percentDepth.Min().ToString("0%") + " to " + percentDepth.Max().ToString("0%");
            }
            SetTrueText(strOut, 3);
        }
    }




    public class CS_Cell_Distance : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Cell_Distance(VBtask task) : base(task)
        {
            if (standalone) task.gOptions.setDisplay1();
            if (standalone) task.gOptions.setDisplay1();
            dst1 = new cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0);
            dst3 = new cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0);
            labels = new string[] { "", "Depth distance to selected cell", "", "Color distance to selected cell" };
            desc = "Measure the color distance of each cell to the selected cell.";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat || task.quarterBeat)
            {
                redC.Run(src);
                dst0 = task.color;
                dst2 = redC.dst2;
                labels[2] = redC.labels[2];
                List<float> depthDistance = new List<float>();
                List<float> colorDistance = new List<float>();
                cv.Scalar selectedMean = src[task.rc.rect].Mean(task.rc.mask);
                foreach (var rc in task.redCells)
                {
                    colorDistance.Add(distance3D(selectedMean, new Mat(src, rc.rect).Mean(rc.mask)));
                    depthDistance.Add(distance3D(task.rc.depthMean, rc.depthMean));
                }
                dst1.SetTo(0);
                dst3.SetTo(0);
                float maxColorDistance = colorDistance.Max();
                for (int i = 0; i < task.redCells.Count; i++)
                {
                    var rc = task.redCells[i];
                    dst1[rc.rect].SetTo(new cv.Scalar(255 - depthDistance[i] * 255 / task.MaxZmeters), rc.mask);
                    dst3[rc.rect].SetTo(new cv.Scalar(255 - colorDistance[i] * 255 / maxColorDistance), rc.mask);
                }
            }
        }
    }
    public class CS_Cell_Binarize : CS_Parent
    {
        public RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Cell_Binarize(VBtask task) : base(task)
        {
            if (standaloneTest())
                task.gOptions.setDisplay1();
            if (standaloneTest())
                task.gOptions.setDisplay1();
            dst1 = new cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0);
            dst3 = new cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0);
            labels = new string[] { "", "Binarized image", "", "Relative gray image" };
            desc = "Separate the image into light and dark using RedCloud cells";
        }
        public void RunCS(Mat src)
        {
            dst0 = src;
            if (task.heartBeat || task.quarterBeat)
            {
                redC.Run(src);
                dst2 = redC.dst2;
                labels[2] = redC.labels[2];
                List<float> grayMeans = new List<float>();
                cv.Mat gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
                foreach (var rc in task.redCells)
                {
                    cv.Scalar grayMean, grayStdev;
                    cv.Cv2.MeanStdDev(gray[rc.rect], out grayMean, out grayStdev, rc.mask);
                    grayMeans.Add((float)grayMean[0]);
                }
                float min = grayMeans.Min();
                float max = grayMeans.Max();
                float avg = grayMeans.Average();
                dst3.SetTo(0);
                foreach (var rc in task.redCells)
                {
                    float color = (grayMeans[rc.index] - min) * 255 / (max - min);
                    dst3[rc.rect].SetTo(new cv.Scalar(color), rc.mask);
                    dst1[rc.rect].SetTo(grayMeans[rc.index] > avg ? new cv.Scalar(255) : new cv.Scalar(0), rc.mask);
                }
            }
        }
    }





    public class CS_Cell_Floodfill : CS_Parent
    {
        Flood_Basics flood = new Flood_Basics();
        Cell_Basics stats = new Cell_Basics();
        public CS_Cell_Floodfill(VBtask task) : base(task)
        {
            desc = "Provide cell stats on the flood_basics cells.";
        }
        public void RunCS(Mat src)
        {
            flood.Run(src);
            stats.Run(src);
            dst0 = stats.dst0;
            dst1 = stats.dst1;
            dst2 = flood.dst2;
            labels = flood.labels;
            SetTrueText(stats.strOut, 3);
        }
    }





    public class CS_Cell_BasicsPlot : CS_Parent
    {
        Hist_Depth plot = new Hist_Depth();
        public bool runRedCloud;
        Cell_Basics stats = new Cell_Basics();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Cell_BasicsPlot(VBtask task) : base(task)
        {
            task.redOptions.setIdentifyCells(true);
            if (standalone)
                task.gOptions.setDisplay1();
            if (standalone)
                task.gOptions.setHistogramBins(20);
            desc = "Display the statistics for the selected cell.";
        }
        public void statsString(cv.Mat src)
        {
            cv.Mat tmp = new cv.Mat(task.rc.mask.Rows, task.rc.mask.Cols, cv.MatType.CV_32F, 0);
            task.pcSplit[2][task.rc.rect].CopyTo(tmp, task.rc.mask);
            plot.rc = task.rc;
            plot.Run(tmp);
            dst1 = plot.dst2;
            stats.statsString();
            strOut = stats.strOut;
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest() || runRedCloud)
            {
                redC.Run(src);
                dst2 = redC.dst2;
                labels[2] = redC.labels[2];
                if (task.ClickPoint == new cv.Point())
                {
                    if (task.redCells.Count > 1)
                    {
                        task.rc = task.redCells[1];
                        task.ClickPoint = task.rc.maxDist;
                    }
                }
            }
            if (task.heartBeat)
                statsString(src);
            SetTrueText(strOut, 3);
            labels[1] = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + ((int)task.MaxZmeters).ToString() + " meters";
        }
    }




    public class CS_Cell_Stable : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Cell_Stable(VBtask task) : base(task)
        {
            labels[3] = "Below are cells that were not exact matches.";
            desc = "Identify cells which were NOT present in the previous generation.";
        }
        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
            if (task.heartBeat)
                return;
            int retained = 0;
            dst3.SetTo(0);
            foreach (var rc in task.redCells)
            {
                if (rc.exactMatch)
                    retained++;
                else
                    dst3[rc.rect].SetTo(rc.color, rc.mask);
            }
            labels[3] = (task.redCells.Count - retained).ToString() + " were not exact matches (shown below)";
        }
    }





    public class CS_Cell_Generate : CS_Parent
    {
        public int classCount;
        public List<cv.Rect> rectList = new List<cv.Rect>();
        public List<cv.Point> floodPoints = new List<cv.Point>();
        public bool removeContour;
        Diff_Basics diffLeft = new Diff_Basics();
        Diff_Basics diffRight = new Diff_Basics();
        public bool useLeftImage = true;
        Boundary_RemovedRects bounds = new Boundary_RemovedRects();
        RedCloud_CPP redCPP;
        int saveRetained = -1;
        public CS_Cell_Generate(VBtask task) : base(task)
        {
            task.cellMap = new cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0);
            task.redCells = new List<rcData>();
            desc = "Generate the RedCloud cells from the rects, mask, and pixel counts.";
        }
        public void RunCS(Mat src)
        {
            if (standalone)
            {
                bounds.Run(src);
                task.cellMap = bounds.bRects.bounds.dst2;
                src = task.cellMap.BitwiseOr(bounds.dst2);
                if (task.FirstPass)
                    task.cellMap.SetTo(0);
                redCPP = bounds.bRects.bounds.redCPP;
                if (redCPP.classCount == 0)
                    return; // no data to process.
                classCount = redCPP.classCount;
                rectList = redCPP.rectList;
                floodPoints = redCPP.floodPoints;
                removeContour = false;
                src = redCPP.dst2;
            }
            if (useLeftImage)
                diffLeft.Run(task.leftView);
            else
                diffRight.Run(task.rightView);
            SortedList<int, rcData> sortedCells = new SortedList<int, rcData>(new compareAllowIdenticalIntegerInverted());
            List<cv.Vec3b> usedColors = new List<cv.Vec3b> { black };
            int retained = 0;
            List<rcData> initList = new List<rcData> { new rcData() };
            for (int i = 1; i < classCount; i++)
            {
                rcData rc = new rcData();
                rc.rect = rectList[i - 1];
                if (rc.rect.Width == dst2.Width && rc.rect.Height == dst2.Height)
                    continue; // FeatureLess_RedCloud find a cell this big.  
                rc.floodPoint = floodPoints[i - 1];
                rc.mask = src[rc.rect].InRange(i, i);
                if (task.heartBeat || rc.indexLast == 0 || rc.indexLast >= task.redCells.Count)
                {
                    if (useLeftImage)
                        cv.Cv2.MeanStdDev(task.color[rc.rect], out rc.colorMean, out rc.colorStdev, rc.mask);
                    else
                        cv.Cv2.MeanStdDev(task.rightView[rc.rect], out rc.colorMean, out rc.colorStdev, rc.mask);
                }
                else
                {
                    rc.colorMean = task.redCells[rc.indexLast].colorMean;
                }
                rc.naturalColor = new cv.Vec3b((byte)rc.colorMean[0], (byte)rc.colorMean[1], (byte)rc.colorMean[2]);
                rc.naturalGray = (int)(rc.colorMean[2] * 0.299 + rc.colorMean[1] * 0.587 + rc.colorMean[0] * 0.114);
                rc.maxDist = GetMaxDist(ref rc);
                rc.indexLast = task.cellMap.Get<byte>(rc.maxDist.Y, rc.maxDist.X);
                if (useLeftImage)
                    rc.motionPixels = diffLeft.dst2[rc.rect].CountNonZero();
                else
                    rc.motionPixels = diffRight.dst2[rc.rect].CountNonZero();
                if (rc.indexLast > 0 && rc.indexLast < task.redCells.Count)
                {
                    var lrc = task.redCells[rc.indexLast];
                    if ((!task.heartBeat || task.FirstPass) && Math.Abs(lrc.naturalGray - rc.naturalGray) <= 1 && rc.motionPixels == 0)
                    {
                        rc = lrc;
                        rc.exactMatch = true;
                        retained++;
                    }
                }
                initList.Add(rc);
            }
            for (int i = 0; i < initList.Count; i++) {
                var rc = initList[i];
                if (!rc.exactMatch)
                {
                    rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone); // .ApproxTC89L1
                    DrawContour(rc.mask, rc.contour, 255, -1);
                    if (removeContour)
                        DrawContour(rc.mask, rc.contour, 0, 2); // no overlap with neighbors.
                    rc.maxDStable = rc.maxDist; // assume it has to use the latest.
                    rc.indexLast = task.cellMap.Get<byte>(rc.maxDist.Y, rc.maxDist.X);
                    if (rc.indexLast > 0 && rc.indexLast < task.redCells.Count)
                    {
                        var lrc = task.redCells[rc.indexLast];
                        if (!task.heartBeat && Math.Abs(lrc.naturalGray - rc.naturalGray) <= 1 && rc.motionPixels == 0)
                        {
                            rc = lrc;
                            rc.exactMatch = true;
                        }
                        else
                        {
                            rc.color = lrc.color;
                            byte stableCheck = task.cellMap.Get<byte>(lrc.maxDist.Y, lrc.maxDist.X);
                            if (stableCheck == rc.indexLast)
                                rc.maxDStable = lrc.maxDStable; // keep maxDStable if cell matched to previous
                            byte val = task.cellMap.Get<byte>(rc.maxDStable.Y, rc.maxDStable.X);
                            if (val != rc.indexLast)
                                rc.maxDStable = rc.maxDist; // maxDist has finally hit the edges of the cell.
                            rc.pointMatch = true;
                        }
                    }
                    if (!rc.pointMatch && !rc.exactMatch)
                        rc.color = new cv.Vec3b((byte)msRNG.Next(40, 220), (byte)msRNG.Next(40, 220), (byte)msRNG.Next(40, 220));
                    if (usedColors.Contains(rc.color))
                        rc.color = task.vecColors[sortedCells.Count + 1];
                    usedColors.Add(rc.color);
                    rc.pixels = rc.mask.CountNonZero(); // the number of pixels may have changed with the infill or contour.
                    if (rc.pixels == 0)
                        continue;
                    rc.depthMask = rc.mask.Clone();
                    rc.depthMask.SetTo(0, new Mat(task.noDepthMask, rc.rect));
                    rc.depthPixels = rc.depthMask.CountNonZero();
                    if (rc.depthPixels != 0)
                    {
                        double minVal, maxVal;
                        task.pcSplit[0][rc.rect].MinMaxLoc(out minVal, out maxVal, out rc.minLoc, out rc.maxLoc, rc.depthMask);
                        rc.minVec.X = (float)minVal;
                        rc.maxVec.X = (float)maxVal;
                        task.pcSplit[1][rc.rect].MinMaxLoc(out minVal, out maxVal, out rc.minLoc, out rc.maxLoc, rc.depthMask);
                        rc.minVec.Y = (float)minVal;
                        rc.maxVec.Y = (float)maxVal;
                        task.pcSplit[2][rc.rect].MinMaxLoc(out minVal, out maxVal, out rc.minLoc, out rc.maxLoc, rc.depthMask);
                        rc.minVec.Z = (float)minVal;
                        rc.maxVec.Z = (float)maxVal;
                        cv.Cv2.MeanStdDev(task.pointCloud[rc.rect], out rc.depthMean, out rc.depthStdev, rc.depthMask);
                    }
                }
                sortedCells.Add(rc.pixels, rc);
            }
            task.redCells = new List<rcData>(sortedCells.Values);
            dst2 = RebuildCells(sortedCells);
            if (saveRetained < 0) saveRetained = retained;
            if (retained > 0)
                saveRetained = retained;
            if (task.heartBeat)
                labels[2] = task.redCells.Count.ToString() + " total cells with " + saveRetained.ToString() + " exact matches";
        }
    }


    // http://ptgmedia.pearsoncmg.com/images/0672320665/downloads/The%20Game%20of%20Life.html
    public class CS_CellularAutomata_Life : CS_Parent
    {
        public int lastPopulation;
        CS_Random_Basics random;
        Mat grid;
        Mat nextgrid;
        int factor = 8;
        int generation;
        public int population;
        public Scalar nodeColor = Scalar.White;
        public Scalar backColor = Scalar.Black;
        int savePointCount;
        const int countInit = 200;
        int countdown = countInit;

        int CountNeighbors(int cellX, int cellY)
        {
            int count = 0;
            if (cellX > 0 && cellY > 0)
            {
                if (grid.At<byte>(cellY - 1, cellX - 1) != 0) count++;
                if (grid.At<byte>(cellY - 1, cellX) != 0) count++;
                if (grid.At<byte>(cellY, cellX - 1) != 0) count++;
            }
            if (cellX < grid.Width - 1 && cellY < grid.Height - 1)
            {
                if (grid.At<byte>(cellY + 1, cellX + 1) != 0) count++;
                if (grid.At<byte>(cellY + 1, cellX) != 0) count++;
                if (grid.At<byte>(cellY, cellX + 1) != 0) count++;
            }
            if (cellX > 0 && cellY < grid.Height - 1)
            {
                if (grid.At<byte>(cellY + 1, cellX - 1) != 0) count++;
            }
            if (cellX < grid.Width - 1 && cellY > 0)
            {
                if (grid.At<byte>(cellY - 1, cellX + 1) != 0) count++;
            }
            return count;
        }

        public CS_CellularAutomata_Life(VBtask task) : base(task)
        {
            random = new CS_Random_Basics(task);
            grid = new Mat(dst2.Height / factor, dst2.Width / factor, MatType.CV_8UC1, Scalar.All(0));
            nextgrid = grid.Clone();
            random.range = new Rect(0, 0, grid.Width, grid.Height);
            FindSlider("Random Pixel Count").Value = (int)(grid.Width * grid.Height * 0.3); // we want about 30% of cells filled.
            desc = "Use OpenCV to implement the Game of Life";
        }

        public void RunCS(Mat src)
        {
            if (random.options.count != savePointCount || generation == 0)
            {
                random.RunAndMeasure(empty, random);
                generation = 0;
                savePointCount = random.options.count;
                foreach (var point in random.pointList)
                {
                    grid.Set((int)point.Y, (int)point.X, 1);
                }
            }
            generation++;

            population = 0;
            dst2.SetTo(backColor);
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    int neighbors = CountNeighbors(x, y);
                    if (neighbors == 2 || neighbors == 3)
                    {
                        if (neighbors == 2)
                        {
                            nextgrid.Set(y, x, grid.At<byte>(y, x));
                        }
                        else
                        {
                            nextgrid.Set(y, x, 1);
                        }
                    }
                    else
                    {
                        nextgrid.Set(y, x, 0);
                    }
                    if (nextgrid.At<byte>(y, x) != 0)
                    {
                        cv.Point pt = new cv.Point(x, y) * factor;
                        Cv2.Circle(dst2, pt, factor / 2, nodeColor, -1);
                        population++;
                    }
                }
            }

            string countdownText = "";
            if (lastPopulation == population && countdown == -1) countdown = countInit;
            if (lastPopulation == population)
            {
                countdown--;
                countdownText = " Restart in " + countdown;
                if (countdown == 0)
                {
                    countdownText = "";
                    generation = 0;
                    countdown = countInit;
                }
            }
            else
            { 
                countdown = -1;
            }
            lastPopulation = population;
            labels[2] = "Population " + population + " Generation = " + generation + countdownText;
            grid = nextgrid.Clone();
        }
    }




    public class CS_CellularAutomata_LifeColor : CS_Parent
    {
        CS_CellularAutomata_Life game;
        public CS_CellularAutomata_LifeColor(VBtask task) : base(task)
        {
            game = new CS_CellularAutomata_Life(task);
            game.backColor = Scalar.White;
            game.nodeColor = Scalar.Black;

            labels[2] = "Births are blue, deaths are red";
            desc = "Game of Life but with color added";
        }

        public void RunCS(Mat src)
        {
            Mat lastBoard = game.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            game.RunAndMeasure(src, game);
            dst1 = game.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);

            Mat deaths = new Mat(), births = new Mat();

            Cv2.Subtract(dst1, lastBoard, births);
            Cv2.Subtract(lastBoard, dst1, deaths);
            births = births.Threshold(0, 255, ThresholdTypes.Binary);
            deaths = deaths.Threshold(0, 255, ThresholdTypes.Binary);
            dst2 = game.dst2.Clone();
            dst2.SetTo(Scalar.Blue, births);
            dst2.SetTo(Scalar.Red, deaths);
        }
    }




    // http://ptgmedia.pearsoncmg.com/images/0672320665/downloads/The%20Game%20of%20Life.html
    public class CS_CellularAutomata_LifePopulation : CS_Parent
    {
        Plot_OverTimeSingle plot = new Plot_OverTimeSingle();
        CS_CellularAutomata_Life game;
        public CS_CellularAutomata_LifePopulation(VBtask task) : base(task)
        {
            game = new CS_CellularAutomata_Life(task);
            desc = "Show Game of Life display with plot of population";
        }

        public void RunCS(Mat src)
        {
            game.RunAndMeasure(src, game);
            dst2 = game.dst2;

            plot.plotData = game.population;
            plot.Run(empty);
            dst3 = plot.dst2;
        }
    }



    // https://mathworld.wolfram.com/ElementaryCellularAutomaton.html
    public class CS_CellularAutomata_All256 : CS_Parent
    {
        CS_CellularAutomata_Basics cell;
        Options_CellAutomata options = new Options_CellAutomata();
        System.Windows.Forms.TrackBar ruleSlider;
        public CS_CellularAutomata_All256(VBtask task) : base(task)
        {
            ruleSlider = FindSlider("Current Rule");
            cell = new CS_CellularAutomata_Basics(task);
            desc = "Run through all 256 combinations of outcomes";
        }

        string createOutcome(int val)
        {
            string outstr = "";
            for (int i = 0; i < 8; i++)
            {
                outstr = (val % 2).ToString() + outstr;
                val = (int)Math.Floor(val / 2.0);
            }
            return outstr;
        }

        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                cell.input = new Mat(new cv.Size(src.Width / 4, src.Height / 4), MatType.CV_8UC1, 0);
                cell.input.Set<byte>(0, cell.input.Width / 2, 1);

                labels[2] = createOutcome(options.currentRule) + " options.currentRule = " + options.currentRule.ToString();
                dst2 = cell.createCells(labels[2]);

                options.RunVB();

                if (task.heartBeat)
                {
                    labels[3] = createOutcome(options.currentRule) + " current rule = " + options.currentRule.ToString();
                    dst3 = cell.createCells(labels[3]);
                }
                if (ruleSlider.Value < ruleSlider.Maximum - 1) ruleSlider.Value += 1; else ruleSlider.Value = 0;
            }
        }
    }

    public class CS_CellularAutomata_MultiPoint : CS_Parent
    {
        CS_CellularAutomata_Basics cell;
        Options_CellAutomata options = new Options_CellAutomata();
        int val1 = 0; int val2 = 0;

        public CS_CellularAutomata_MultiPoint(VBtask task) : base(task)
        {
            cell = new CS_CellularAutomata_Basics(task);
            val2 = dst2.Width / 2;
            FindSlider("Current Rule").Value = 4;
            desc = "All256 above starts with just one point. Here we start with multiple points.";
        }

        public void RunCS(Mat src)
        {
            Mat tmp = new Mat(new cv.Size(src.Width / 4, src.Height / 4), MatType.CV_8UC1, 0);
            tmp.Set(0, val1, 1);
            tmp.Set(0, val2, 1);
            cell.RunAndMeasure(tmp, cell);

            dst2 = cell.dst2;
            val1++;
            if (val1 > tmp.Width) val1 = 0;
            if (val2 >= src.Width) val2 = 0;
        }
    }




    public class CS_CellularAutomata_Basics : CS_Parent
    {
        string[] i18 = {"00011110 Rule 30 (chaotic)", "00110110 Rule 54", "00111100 Rule 60", "00111110 Rule 62",
                                  "01011010 Rule 90", "01011110 Rule 94", "01100110 Rule 102", "01101110 Rule 110",
                                  "01111010 Rule 122", "01111110 Rule 126", "10010110 Rule 150", "10011110 Rule 158",
                                  "10110110 Rule 182", "10111100 Rule 188", "10111110 Rule 190", "11011100 Rule 220",
                                  "11011110 Rule 222", "11111010 Rule 250"};
        string inputCombo = "111,110,101,100,011,010,001,000";
        int[,] cellInput = { { 1, 1, 1 }, { 1, 1, 0 }, { 1, 0, 1 }, { 1, 0, 0 }, { 0, 1, 1 }, { 0, 1, 0 }, { 0, 0, 1 }, { 0, 0, 0 } };
        public Mat input = new Mat();
        int myIndex = 0;
        public CS_CellularAutomata_Basics(VBtask task) : base(task)
        {
            string label = "The 18 most interesting automata from the first 256 in 'New Kind of Science'\nThe input combinations are: " + inputCombo;
            desc = "Visualize the 30 interesting examples from the first 256 in 'New Kind of Science'";
        }

        public Mat createCells(string outStr)
        {
            byte[] outcomes = new byte[8];
            for (int i = 0; i < outcomes.Length; i++)
            {
                outcomes[i] = byte.Parse(outStr.Substring(i, 1));
            }

            Mat dst = input.Clone();
            for (int y = 0; y < dst.Height - 2; y++)
            {
                for (int x = 0; x < dst.Width - 2; x++)
                {
                    byte x1 = dst.At<byte>(y, x - 1);
                    byte x2 = dst.At<byte>(y, x);
                    byte x3 = dst.At<byte>(y, x + 1);
                    for (int i = 0; i <= cellInput.GetUpperBound(0); i++)
                    {
                        if (x1 == cellInput[i, 0] && x2 == cellInput[i, 1] && x3 == cellInput[i, 2])
                        {
                            dst.Set(y + 1, x, outcomes[i]);
                            break;
                        }
                    }
                }
            }
            return dst.ConvertScaleAbs(255).CvtColor(ColorConversionCodes.GRAY2BGR);
        }

        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                labels[2] = i18[myIndex];
                myIndex += 1;
                if (myIndex >= i18.Length) myIndex = 0;
            }
            
            if (standalone)
            {
                input = new Mat(new cv.Size(src.Width, src.Height), MatType.CV_8UC1, Scalar.All(0));
                input.Set<byte>(0, src.Width / 2, 1);
                dst2 = createCells(labels[2]);
            }
            else
            {
                input = src.Clone();
                dst2 = createCells(labels[2]);
            }
        }
    }



    public class CS_Classifier_Basics : CS_Parent
    {
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr OEX_Points_Classifier_Open();
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void OEX_Points_Classifier_Close(IntPtr cPtr);
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr OEX_ShowPoints(IntPtr cPtr, int imgRows, int imgCols, int DotSize);
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr OEX_Points_Classifier_RunCPP(IntPtr cPtr, int count, int methodIndex, int imgRows, int imgCols, int resetInput);
        Options_Classifier options = new Options_Classifier();

        public CS_Classifier_Basics(VBtask task) : base(task)
        {
            cPtr = OEX_Points_Classifier_Open();
            desc = "OpenCV Example Points_Classifier";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged) task.gOptions.setDebugCheckBox(true);
            IntPtr imagePtr = OEX_Points_Classifier_RunCPP(cPtr, options.sampleCount, options.methodIndex, dst2.Rows, dst2.Cols,
                                                           task.gOptions.getDebugCheckBox() ? 1 : 0);
            task.gOptions.setDebugCheckBox(false);
            dst1 = new Mat(dst0.Rows, dst0.Cols, MatType.CV_32S, imagePtr);

            dst1.ConvertTo(dst0, MatType.CV_8U);
            dst2 = ShowPalette(dst0 * 255 / 2);
            imagePtr = OEX_ShowPoints(cPtr, dst2.Rows, dst2.Cols, task.DotSize);
            dst3 = new Mat(dst2.Rows, dst2.Cols, MatType.CV_8UC3, imagePtr);

            SetTrueText("Click the global DebugCheckBox to get another set of points.", 3);
        }

        public void Close()
        {
            OEX_Points_Classifier_Close(cPtr);
        }
    }

    public class CS_Classifier_Bayesian : CS_Parent
    {
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr OEX_Points_Classifier_Open();
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void OEX_Points_Classifier_Close(IntPtr cPtr);
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr OEX_ShowPoints(IntPtr cPtr, int imgRows, int imgCols, int DotSize);
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr OEX_Points_Classifier_RunCPP(IntPtr cPtr, int count, int methodIndex, int imgRows, int imgCols, int resetInput);
        Options_Classifier options = new Options_Classifier();

        public CS_Classifier_Bayesian(VBtask task) : base(task)
        {
            cPtr = OEX_Points_Classifier_Open();
            desc = "Run the Bayesian classifier with the input.";
        }

        public void RunCS(Mat src)
        {
            int sampleCount, methodIndex = 0;
            if (src.Type() != MatType.CV_32FC2)
            {
                options.RunVB();
                sampleCount = options.sampleCount;
                methodIndex = options.methodIndex;
            }
            else
            {
                sampleCount = src.Rows;
            }

            if (task.heartBeat) task.gOptions.setDebugCheckBox(true);
            IntPtr imagePtr = OEX_Points_Classifier_RunCPP(cPtr, sampleCount, methodIndex, dst2.Rows, dst2.Cols,
                                                           task.gOptions.getDebugCheckBox() ? 1 : 0);
            task.gOptions.setDebugCheckBox(false);
            dst1 = new Mat(dst1.Rows, dst1.Cols, MatType.CV_32S, imagePtr);
            dst1.ConvertTo(dst0, MatType.CV_8U);
            dst2 = ShowPalette(dst0 * 255 / 2);
            imagePtr = OEX_ShowPoints(cPtr, dst2.Rows, dst2.Cols, task.DotSize);
        }
    }

    public class CS_Classifier_BayesianTest : CS_Parent
    {
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr Classifier_Bayesian_Open();
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void Classifier_Bayesian_Close(IntPtr cPtr);
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void Classifier_Bayesian_Train(IntPtr cPtr, IntPtr trainInput, IntPtr response, int count);
        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr Classifier_Bayesian_RunCPP(IntPtr cPtr, IntPtr trainInput, int count);

        RedCloud_Basics redC = new RedCloud_Basics();
        Neighbors_Precise nabs = new Neighbors_Precise();

        public CS_Classifier_BayesianTest(VBtask task) : base(task)
        {
            task.redOptions.useColorOnlyChecked = true;
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, 0);
            labels = new string[] { "", "Mask of the neighbors to the selected cell", "RedCloud_Basics output", "Classifier_Bayesian output" };
            if (standalone) task.gOptions.setDisplay1();
            cPtr = Classifier_Bayesian_Open();
            desc = "Classify the neighbor cells to be similar to the selected cell or not.";
        }

        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;

            SetTrueText("Review the neighbors_Precise algorithm");
            // nabs.redCells = task.redCells;
            // nabs.Run(task.cellMap);

            // List<Scalar> trainList = new List<Scalar>();
            // List<int> responseList = new List<int>();
            // foreach (var rc in task.redCells)
            // {
            //     trainList.Add(rc.depthMean);
            //     responseList.Add(0);
            // }

            // dst1.SetTo(0);
            // foreach (var index in nabs.nabList(task.rc.index))
            // {
            //     var rc = task.redCells[index];
            //     dst1[rc.rect].SetTo(255, rc.mask);
            //     strOut += index + ",";
            //     responseList[index] = -1;
            // }

            // responseList[task.rc.index] = 1;

            // List<Scalar> queryList = new List<Scalar>();
            // List<int> maskList = new List<int>();
            // for (int i = responseList.Count - 1; i >= 0; i--)
            // {
            //     if (responseList[i] == -1)
            //     {
            //         responseList.RemoveAt(i);
            //         queryList.Add(trainList[i]);
            //         trainList.RemoveAt(i);
            //         maskList.Add(i);
            //     }
            // }

            // var vecs = trainList.ToArray();
            // var resp = responseList.ToArray();
            // var handleTrainInput = GCHandle.Alloc(vecs, GCHandleType.Pinned);
            // var handleResponse = GCHandle.Alloc(resp, GCHandleType.Pinned);
            // Classifier_Bayesian_Train(cPtr, handleTrainInput.AddrOfPinnedObject(), handleResponse.AddrOfPinnedObject(), responseList.Count);
            // handleResponse.Free();
            // handleTrainInput.Free();

            // int[] results = new int[queryList.Count];
            // if (queryList.Count > 0)
            // {
            //     var queries = queryList.ToArray();
            //     var handleQueryInput = GCHandle.Alloc(queries, GCHandleType.Pinned);
            //     IntPtr resultsPtr = Classifier_Bayesian_RunCPP(cPtr, handleQueryInput.AddrOfPinnedObject(), queries.Length);
            //     handleQueryInput.Free();

            //     Marshal.Copy(resultsPtr, results, 0, results.Length);
            // }

            // dst3.SetTo(0);
            // bool zeroOutput = true;
            // for (int i = 0; i < maskList.Count; i++)
            // {
            //     if (results[i] > 0)
            //     {
            //         var rc = task.redCells[maskList[i]];
            //         dst3[rc.rect].SetTo(rc.color, rc.mask);
            //         zeroOutput = false;
            //     }
            // }
            // if (zeroOutput) SetTrueText("None of the neighbors were as similar to the selected cell.", 3);
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero) Classifier_Bayesian_Close(cPtr);
        }
    }


    public class CS_Clone_Basics : CS_Parent
    {
        public Vec3f colorChangeValues;
        public Vec2f illuminationChangeValues;
        public Vec2f textureFlatteningValues;
        public int cloneSpec; // 0 is colorchange, 1 is illuminationchange, 2 is textureflattening
        public CS_Clone_Basics(VBtask task) : base(task)
        {
            labels[2] = "Clone result - draw anywhere to clone a region";
            labels[3] = "Clone Region Mask";
            desc = "Clone a portion of one image into another. Draw on any image to change selected area.";
            task.drawRect = new Rect(dst2.Width / 4, dst2.Height / 4, dst2.Width / 2, dst2.Height / 2);
        }

        public void RunCS(Mat src)
        {
            Mat mask = new Mat(src.Size(), MatType.CV_8U, Scalar.All(0));
            if (task.drawRect == new Rect())
            {
                mask.SetTo(Scalar.All(255));
            }
            else
            {
                Cv2.Rectangle(mask, task.drawRect, Scalar.White, -1);
            }
            dst3 = mask.CvtColor(ColorConversionCodes.GRAY2BGR);

            if (standaloneTest() && task.frameCount % 10 == 0) cloneSpec += 1;
            switch (cloneSpec % 3)
            {
                case 0:
                    Cv2.ColorChange(src, mask, dst2, colorChangeValues.Item0, colorChangeValues.Item1, colorChangeValues.Item2);
                    break;
                case 1:
                    Cv2.IlluminationChange(src, mask, dst2, illuminationChangeValues.Item0, illuminationChangeValues.Item1);
                    break;
                case 2:
                    Cv2.TextureFlattening(src, mask, dst2, textureFlatteningValues.Item0, textureFlatteningValues.Item1);
                    break;
            }
        }
    }

    public class CS_Clone_ColorChange : CS_Parent
    {
        Clone_Basics clone = new Clone_Basics();
        Options_Clone options = new Options_Clone();
        public CS_Clone_ColorChange(VBtask task) : base(task)
        {
            labels[2] = "Draw anywhere to select different clone region";
            labels[3] = "Mask used for clone";
            desc = "Clone a portion of one image into another controlling rgb. Draw on any image to change selected area.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            
            clone.cloneSpec = 0;
            clone.colorChangeValues = new Vec3f(options.blueChange, options.greenChange, options.redChange);
            clone.Run(src);
            dst2 = clone.dst2;
            dst3 = clone.dst3;
        }
    }

    public class CS_Clone_IlluminationChange : CS_Parent
    {
        Clone_Basics clone = new Clone_Basics();
        Options_Clone options = new Options_Clone();
        public CS_Clone_IlluminationChange(VBtask task) : base(task)
        {
            labels[2] = "Draw anywhere to select different clone region";
            labels[3] = "Mask used for clone";
            desc = "Clone a portion of one image into another controlling illumination. Draw on any image to change selected area.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            
            clone.cloneSpec = 1;
            clone.illuminationChangeValues = new Vec2f(options.alpha, options.beta);
            clone.Run(src);
            dst2 = clone.dst2;
            dst3 = clone.dst3;
        }
    }

    public class CS_Clone_TextureFlattening : CS_Parent
    {
        Clone_Basics clone = new Clone_Basics();
        Options_Clone options = new Options_Clone();
        public CS_Clone_TextureFlattening(VBtask task) : base(task)
        {
            labels[2] = "Draw anywhere to select different clone region";
            labels[3] = "mask used for clone";
            desc = "Clone a portion of one image into another controlling texture. Draw on any image to change selected area.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            clone.cloneSpec = 2;
            clone.textureFlatteningValues = new Vec2f(options.lowThreshold, options.highThreshold);
            clone.Run(src);
            dst2 = clone.dst2;
            dst3 = clone.dst3;
        }
    }



    public class CS_Clone_Eagle : CS_Parent
    {
        Mat sourceImage;
        Mat mask;
        Rect srcROI;
        Rect maskROI;
        cv.Point pt;
        Options_Clone options = new Options_Clone();

        public CS_Clone_Eagle(VBtask task) : base(task)
        {
            sourceImage = Cv2.ImRead(task.HomeDir + "Data/CloneSource.png");
            sourceImage = sourceImage.Resize(new cv.Size(sourceImage.Width * dst2.Width / 1280, sourceImage.Height * dst2.Height / 720));
            srcROI = new Rect(0, 40, sourceImage.Width, sourceImage.Height);

            mask = Cv2.ImRead(task.HomeDir + "Data/Clonemask.png");
            mask = mask.Resize(new cv.Size(mask.Width * dst2.Width / 1280, mask.Height * dst2.Height / 720));
            maskROI = new Rect(srcROI.Width, 40, mask.Width, mask.Height);

            dst3.SetTo(0);
            dst3[srcROI] = sourceImage;
            dst3[maskROI] = mask;

            pt = new cv.Point(dst2.Width / 2, dst2.Height / 2);
            labels[2] = "Move Eagle by clicking in any location.";
            labels[3] = "Source image and source mask.";
            desc = "Clone an eagle into the video stream.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = src.Clone();
            if (task.mouseClickFlag)
            {
                pt = task.ClickPoint;  // pt corresponds to the center of the source image.  Roi can't be outside image boundary.
                if (pt.X + srcROI.Width / 2 >= src.Width) pt.X = src.Width - srcROI.Width / 2;
                if (pt.X - srcROI.Width / 2 < 0) pt.X = srcROI.Width / 2;
                if (pt.Y + srcROI.Height >= src.Height) pt.Y = src.Height - srcROI.Height / 2;
                if (pt.Y - srcROI.Height < 0) pt.Y = srcROI.Height / 2;
            }

            Cv2.SeamlessClone(sourceImage, dst2, mask, pt, dst2, options.cloneFlag);
        }
    }

    // https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Cv2.SeamlessClone(OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.Point,%20OpenCvSharp.OutputArray,%20OpenCvSharp.SeamlessCloneMethods)/
    public class CS_Clone_Seamless : CS_Parent
    {
        Options_Clone options = new Options_Clone();
        public CS_Clone_Seamless(VBtask task) : base(task)
        {
            labels[2] = "Results for SeamlessClone";
            labels[3] = "Mask for Clone";
            desc = "Use the seamlessclone API to merge color and depth...";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            cv.Point center = new cv.Point(src.Width / 2, src.Height / 2);
            int radius = 100;
            if (task.drawRect == new Rect())
            {
                dst3.SetTo(0);
                DrawCircle(dst3, center, radius, Scalar.White);
            }
            else
            {
                Cv2.Rectangle(dst3, task.drawRect, Scalar.White, -1);
            }

            dst2 = src.Clone();
            Cv2.SeamlessClone(task.depthRGB, src, dst3, center, dst2, options.cloneFlag);
            DrawCircle(dst2, center, radius, Scalar.White);
        }
    }

    public class CS_Mat_4Click : CS_Parent
    {
        public CS_Mat_4to1 mats;
        public Mat[] mat;
        public int quadrant = RESULT_DST3;

        public CS_Mat_4Click(VBtask task) : base(task)
        {
            mats = new CS_Mat_4to1(task);
            mat = mats.mat;
            labels[3] = "Click a quadrant in dst2 to view it in dst3";
            desc = "Split an image into 4 segments and allow clicking on a quadrant to open it in dst3";
        }

        public void RunCS(Mat src)
        {
            mat = mats.mat;
            mats.RunAndMeasure(Mat.Zeros(src.Size(), MatType.CV_8UC3), mats);
            dst2 = mats.dst2.Clone();
            if (standalone) mats.defaultMats(src);
            if (task.FirstPass)
            {
                task.ClickPoint = new cv.Point(0, 0);
                task.mousePicTag = RESULT_DST2;
            }

            if (task.mouseClickFlag && task.mousePicTag == RESULT_DST2)
            {
                if (task.ClickPoint.Y < dst2.Rows / 2)
                {
                    quadrant = (task.ClickPoint.X < task.WorkingRes.Width / 2) ? RESULT_DST0 : RESULT_DST1;
                }
                else
                {
                    quadrant = (task.ClickPoint.X < task.WorkingRes.Width / 2) ? RESULT_DST2 : RESULT_DST3;
                }
            }
            mats.RunAndMeasure(Mat.Zeros(src.Size(), MatType.CV_8UC3), mats);
            dst2 = mats.dst2.Clone();
            dst3 = mats.mat[quadrant].Clone();
        }
    }


    public class CS_Mat_4to1 : CS_Parent
    {
        public Mat[] mat = new Mat[4];
        public bool lineSeparators = true; // if they want lines or not...
        public int quadrant = 0;

        public CS_Mat_4to1(VBtask task) : base(task)
        {
            for (int i = 0; i < mat.Length; i++)
            {
                mat[i] = dst2.Clone();
            }
            labels[2] = "Combining 4 images into one";
            labels[3] = "Click any quadrant at left to view it below";
            desc = "Use one Mat for up to 4 images";
        }

        public void defaultMats(Mat src)
        {
            Mat tmpLeft = (task.leftView.Channels() == 1) ? task.leftView.CvtColor(ColorConversionCodes.GRAY2BGR) : task.leftView;
            Mat tmpRight = (task.rightView.Channels() == 1) ? task.rightView.CvtColor(ColorConversionCodes.GRAY2BGR) : task.rightView;
            mat = new Mat[] { task.color.Clone(), task.depthRGB.Clone(), tmpLeft, tmpRight };
        }

        public void RunCS(Mat src)
        {
            cv.Size nSize = new cv.Size(dst2.Width / 2, dst2.Height / 2);
            Rect roiTopLeft = new Rect(0, 0, nSize.Width, nSize.Height);
            Rect roiTopRight = new Rect(nSize.Width, 0, nSize.Width, nSize.Height);
            Rect roibotLeft = new Rect(0, nSize.Height, nSize.Width, nSize.Height);
            Rect roibotRight = new Rect(nSize.Width, nSize.Height, nSize.Width, nSize.Height);
            if (standalone) defaultMats(src);

            dst2 = new Mat(dst2.Size(), MatType.CV_8UC3);
            Rect roi = new Rect(0, 0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                Mat tmp = mat[i].Clone();
                if (tmp.Channels() == 1) tmp = mat[i].CvtColor(ColorConversionCodes.GRAY2BGR);
                if (i == 0) roi = roiTopLeft;
                if (i == 1) roi = roiTopRight;
                if (i == 2) roi = roibotLeft;
                if (i == 3) roi = roibotRight;
                dst2[roi] = tmp.Resize(nSize);
            }
            if (lineSeparators)
            {
                dst2.Line(new cv.Point(0, dst2.Height / 2), new cv.Point(dst2.Width, dst2.Height / 2), Scalar.White, task.lineWidth + 1);
                dst2.Line(new cv.Point(dst2.Width / 2, 0), new cv.Point(dst2.Width / 2, dst2.Height), Scalar.White, task.lineWidth + 1);
            }
        }
    }



    public class CS_Cluster_Basics : CS_Parent
    {
        KNN_Core knn = new KNN_Core();
        public List<cv.Point> ptInput = new List<cv.Point>();
        public List<cv.Point> ptList = new List<cv.Point>();
        public List<int> clusterID = new List<int>();
        public SortedList<int, List<cv.Point>> clusters = new SortedList<int, List<cv.Point>>();
        Feature_Basics feat = new Feature_Basics();

        public CS_Cluster_Basics(VBtask task) : base(task)
        {
            FindSlider("Min Distance to next").Value = 10;
            desc = "Group the points based on their proximity to each other.";
        }

        public void RunCS(Mat src)
        {
            dst2 = src.Clone();
            if (standalone)
            {
                feat.Run(src);
                ptInput = task.featurePoints;
            }

            if (ptInput.Count <= 3) return;

            knn.queries = task.features;
            knn.trainInput = knn.queries;
            knn.Run(empty);

            ptList.Clear();
            clusterID.Clear();
            clusters.Clear();
            int groupID;
            for (int i = 0; i < knn.queries.Count; i++)
            {
                cv.Point p1 = new cv.Point(knn.queries[i].X, knn.queries[i].Y);
                cv.Point p2 = new cv.Point(knn.queries[knn.result[i, 1]].X, knn.queries[knn.result[i, 1]].Y);
                int index1 = ptList.IndexOf(p1);
                int index2 = ptList.IndexOf(p2);
                if (index1 >= 0 && index2 >= 0) continue;
                if (index1 < 0 && index2 < 0)
                {
                    ptList.Add(p1);
                    ptList.Add(p2);
                    groupID = clusters.Count;
                    List<cv.Point> newList = new List<cv.Point> { p1, p2 };
                    clusters.Add(groupID, newList);
                    clusterID.Add(groupID);
                    clusterID.Add(groupID);
                }
                else
                {
                    cv.Point pt = index1 < 0 ? p1 : p2;
                    int index = index1 < 0 ? index2 : index1;
                    groupID = clusterID[index];
                    ptList.Add(pt);
                    clusterID.Add(groupID);
                    clusters.ElementAt(groupID).Value.Add(pt);
                }
            }

            foreach (var group in clusters)
            {
                for (int i = 0; i < group.Value.Count; i++)
                {
                    for (int j = 0; j < group.Value.Count; j++)
                    {
                        Cv2.Line(dst2, group.Value[i], group.Value[j], Scalar.White);
                    }
                }
            }
            dst3.SetTo(0);
            for (int i = 0; i < knn.queries.Count; i++)
            {
                Cv2.Circle(dst2, new cv.Point(knn.queries[i].X, knn.queries[i].Y), task.DotSize, Scalar.Red);
                Cv2.Circle(dst3, new cv.Point(knn.queries[i].X, knn.queries[i].Y), task.DotSize, task.HighlightColor);
            }
            labels[2] = $"{clusters.Count} groups built from {ptInput.Count} by combining each input point and its nearest neighbor.";
        }
    }

    public class CS_Cluster_Hulls : CS_Parent
    {
        Cluster_Basics cluster = new Cluster_Basics();
        public List<List<cv.Point>> hulls = new List<List<cv.Point>>();
        Feature_Basics feat = new Feature_Basics();

        public CS_Cluster_Hulls(VBtask task) : base(task)
        {
            desc = "Create hulls for each cluster of feature points found in Cluster_Basics";
        }

        public void RunCS(Mat src)
        {
            dst2 = src.Clone();

            feat.Run(src);
            cluster.ptInput = task.featurePoints;
            cluster.Run(src);
            dst2 = cluster.dst2;
            dst3 = cluster.dst3;

            hulls.Clear();
            foreach (var group in cluster.clusters)
            {
                cv.Point[] hullPoints = Cv2.ConvexHull(group.Value.ToArray(), true);
                List<cv.Point> hull = new List<cv.Point>();
                if (hullPoints.Length > 2)
                {
                    hull.AddRange(hullPoints.Select(pt => new cv.Point(pt.X, pt.Y)));
                }
                else if (hullPoints.Length == 2)
                {
                    Cv2.Line(dst3, hullPoints[0], hullPoints[1], Scalar.White);
                }

                hulls.Add(hull);
                if (hull.Count > 0) Cv2.DrawContours(dst3, new[] { hull }, 0, Scalar.White, task.lineWidth);
            }
        }
    }



    public class CS_Coherence_Basics : CS_Parent
    {
        Options_Coherence options = new Options_Coherence();
        public CS_Coherence_Basics(VBtask task) : base(task)
        {
            labels[2] = "Coherence - draw rectangle to apply";
            desc = "Find lines that are artistically coherent in the image";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            int side;
            switch (src.Height)
            {
                case 120:
                case 180:
                    side = 64;
                    break;
                case 360:
                case 480:
                    side = 256;
                    break;
                case 720:
                    side = 512;
                    break;
                default:
                    side = 50;
                    break;
            }

            int xoffset = src.Width / 2 - side / 2;
            int yoffset = src.Height / 2 - side / 2;
            Rect srcRect = new Rect(xoffset, yoffset, side, side);
            if (task.drawRect.Width != 0) srcRect = task.drawRect;

            dst2 = src.Clone();
            src = new Mat(src, srcRect);

            Mat gray = new Mat();
            Mat eigen = new Mat();
            Mat[] split;

            for (int i = 0; i < 4; i++)
            {
                gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);
                eigen = gray.CornerEigenValsAndVecs(options.str_sigma, options.eigenkernelsize);
                split = eigen.Split();
                Mat x = split[2], y = split[3];

                Mat gxx = gray.Sobel(MatType.CV_32F, 2, 0, options.sigma);
                Mat gxy = gray.Sobel(MatType.CV_32F, 1, 1, options.sigma);
                Mat gyy = gray.Sobel(MatType.CV_32F, 0, 2, options.sigma);

                Mat tmpX = new Mat(), tmpXY = new Mat(), tmpY = new Mat();
                Cv2.Multiply(x, x, tmpX);
                Cv2.Multiply(tmpX, gxx, tmpX);
                Cv2.Multiply(x, y, tmpXY);
                Cv2.Multiply(tmpXY, gxy, tmpXY);
                tmpXY = tmpXY * 2;

                Cv2.Multiply(y, y, tmpY);
                Cv2.Multiply(tmpY, gyy, tmpY);

                Mat gvv = tmpX + tmpXY + tmpY;

                Mat mask = gvv.Threshold(0, 255, ThresholdTypes.BinaryInv).ConvertScaleAbs();

                Mat erode = src.Erode(new Mat());
                Mat dilate = src.Dilate(new Mat());

                Mat imgl = erode;
                dilate.CopyTo(imgl, mask);
                src = src * (1 - options.blend) + imgl * options.blend;
            }

            src.CopyTo(new Mat(dst2, srcRect));
            Cv2.Rectangle(dst2, srcRect, Scalar.Yellow, 2);
            dst3.SetTo(0);
        }
    }

    public class CS_Coherence_Depth : CS_Parent
    {
        Coherence_Basics coherent = new Coherence_Basics();
        public CS_Coherence_Depth(VBtask task) : base(task)
        {
            desc = "Find coherent lines in the depth image";
        }
        public void RunCS(Mat src)
        {
            coherent.Run(task.depthRGB);
            dst2 = coherent.dst2;
        }
    }


    public class CS_Color_Basics : CS_Parent
    {
        public Options_Color options = new Options_Color();
        public CS_Color_Basics(VBtask task) : base(task)
        {
            desc = "Choose a color source";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (options.colorFormat == null) options.colorFormat = "BGR"; // multiple invocations cause this to be necessary but how to fix?

            switch (options.colorFormat)
            {
                case "BGR":
                    dst2 = src.Clone();
                    break;
                case "LAB":
                    dst2 = src.CvtColor(ColorConversionCodes.BGR2Lab);
                    break;
                case "HSV":
                    dst2 = src.CvtColor(ColorConversionCodes.BGR2HSV);
                    break;
                case "XYZ":
                    dst2 = src.CvtColor(ColorConversionCodes.BGR2XYZ);
                    break;
                case "HLS":
                    dst2 = src.CvtColor(ColorConversionCodes.BGR2HLS);
                    break;
                case "YUV":
                    dst2 = src.CvtColor(ColorConversionCodes.BGR2YUV);
                    break;
                case "YCrCb":
                    dst2 = src.CvtColor(ColorConversionCodes.BGR2YCrCb);
                    break;
            }
        }
    }



    public class CS_Color8U_Basics : CS_Parent
    {
        public int classCount;
        public object classifier;
        object[] colorMethods = {new BackProject_Full(), new BackProject2D_Full(), new Bin4Way_Regions(), new Binarize_DepthTiers(), new FeatureLess_Groups(),
                                 new Hist3Dcolor_Basics(), new KMeans_Basics(), new LUT_Basics(), new Reduction_Basics() };
        public CS_Color8U_Basics(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U);
            labels[3] = "vbPalette output of dst2 at left";
            UpdateAdvice(traceName + ": redOptions 'Color Source' controls which color source is used.");
            desc = "Classify pixels by color using a variety of techniques";
        }
        public void RunCS(Mat src)
        {
            if (task.optionsChanged || classifier == null)
                classifier = colorMethods[task.redOptions.colorInputIndex];

            if (task.redOptions.colorInputName == "BackProject2D_Full")
            {
                ((dynamic)classifier).Run(src);
            }
            else
            {
                dst1 = src.Channels() == 3 ? src.CvtColor(ColorConversionCodes.BGR2GRAY) : src;
                ((dynamic)classifier).Run(dst1);
            }

            if (task.heartBeat)
            {
                dst2 = ((dynamic)classifier).dst2.Clone();
            }
            else if (task.motionDetected)
            {
                ((dynamic)classifier).dst2[task.motionRect].CopyTo(dst2[task.motionRect]);
            }

            classCount = ((dynamic)classifier).classCount;

            // Commented out as in original code
            // if (task.maxDepthMask.Rows > 0)
            // {
            //     classCount += 1;
            //     dst2.SetTo(classCount, task.maxDepthMask);
            // }

            dst3 = ((dynamic)classifier).dst3;
            labels[2] = $"Color_Basics: method = {traceName} produced {classCount} pixel classifications";
        }
    }

    public class CS_Color8U_Grayscale : CS_Parent
    {
        Options_Grayscale8U options = new Options_Grayscale8U();

        public CS_Color8U_Grayscale(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Color_Grayscale", "" };
            desc = "Manually create a grayscale image. The only reason for this example is to show how slow it can be to do the work manually in C#";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (options.useOpenCV)
            {
                dst2 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                dst2 = new Mat(src.Size(), MatType.CV_8U, 0);
                Parallel.For(0, src.Rows, y =>
                {
                    for (int x = 0; x < src.Cols; x++)
                    {
                        Vec3b cc = src.Get<Vec3b>(y, x);
                        dst2.Set<byte>(y, x, (byte)((cc[0] * 1140 + cc[1] * 5870 + cc[2] * 2989) / 10000));
                    }
                });
            }
        }
    }

    public class CS_Color8U_Depth : CS_Parent
    {
        public Reduction_Basics reduction = new Reduction_Basics();
        public CS_Depth_InRange depth;
        public int classCount;

        public CS_Color8U_Depth(VBtask task) : base(task)
        {
            depth = new CS_Depth_InRange(task);
            task.gOptions.setLineType(1); // linetype = link4
            labels = new string[] { "", "", "Color Reduction Edges", "Depth Range Edges" };
            desc = "Add depth regions edges to the color Reduction image.";
        }

        public void RunCS(Mat src)
        {
            reduction.Run(src);
            dst2 = reduction.dst2;
            classCount = reduction.classCount;

            depth.RunAndMeasure(src, depth);
            dst2.SetTo(0, depth.dst3);
            dst3.SetTo(0);
            dst3.SetTo(Scalar.White, depth.dst3);
        }
    }

    public class CS_Color8U_KMeans : CS_Parent
    {
        public KMeans_Basics km0 = new KMeans_Basics();
        public KMeans_Basics km1 = new KMeans_Basics();
        public KMeans_Basics km2 = new KMeans_Basics();
        public Color_Basics colorFmt = new Color_Basics();

        public CS_Color8U_KMeans(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels[0] = "Recombined channels in other images.";
            desc = "Run KMeans on each of the 3 color channels";
        }

        public void RunCS(Mat src)
        {
            colorFmt.Run(src);
            dst0 = colorFmt.dst2;

            Mat[] split = dst0.Split();

            km0.Run(split[0]);
            dst1 = km0.dst2 * 255 / km0.classCount;

            km1.Run(split[1]);
            dst2 = km1.dst2 * 255 / km0.classCount;

            km2.Run(split[2]);
            dst3 = km2.dst2 * 255 / km0.classCount;

            for (int i = 1; i <= 3; i++)
            {
                labels[i] = $"{colorFmt.options.colorFormat} channel {i - 1}";
            }
        }
    }



    public class CS_Color8U_RedHue : CS_Parent
    {
        Options_CamShift options = new Options_CamShift();

        public CS_Color8U_RedHue(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": This mask of red hue areas is available for use.");
            labels = new string[] { "", "", "Pixels with Red Hue", "" };
            desc = "Find all the reddish pixels in the image - indicate some life form.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Mat hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            Mat mask = hsv.InRange(options.camSBins, new Scalar(180, 255, options.camMax));
            dst2.SetTo(0);
            src.CopyTo(dst2, mask);
        }
    }

    public class CS_Color8U_Complementary : CS_Parent
    {
        public CS_Color8U_Complementary(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Current image in complementary colors", "HSV version of the current image but hue is flipped to complementary value." };
            desc = "Display the current image in complementary colors";
        }

        public void RunCS(Mat src)
        {
            Mat hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            Mat[] split = hsv.Split();
            split[0] += 90 % 180;
            Cv2.Merge(split, dst3);
            dst2 = dst3.CvtColor(ColorConversionCodes.HSV2BGR);
        }
    }

    public class CS_Color8U_ComplementaryTest : CS_Parent
    {
        Image_Basics images = new Image_Basics();
        Color8U_Complementary comp = new Color8U_Complementary();

        public CS_Color8U_ComplementaryTest(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Original Image", "Color_Complementary version looks identical to the correct version at the link above " };
            desc = "Create the complementary images for Gilles Tran's 'Glasses' image for comparison";
        }

        public void RunCS(Mat src)
        {
            images.fileNameForm.setFileName(task.HomeDir + "Data/Glasses by Gilles Tran.png");
            images.Run(new Mat());
            dst2 = images.dst2;

            comp.Run(dst2);
            dst3 = comp.dst2;
        }
    }

    public class CS_Color8U_InRange : CS_Parent
    {
        public CS_Color8U_InRange(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Original", "After InRange processing" };
            desc = "Use inRange to isolate colors from the background";
        }

        public void RunCS(Mat src)
        {
            dst2 = Cv2.ImRead(task.HomeDir + "Data/1.jpg", ImreadModes.Grayscale);
            dst1 = dst2.InRange(105, 165); // should make this a slider and experiment further...
            dst3 = dst2.Clone();
            dst3.SetTo(0, dst1);
        }
    }

    public class CS_Color8U_TopX : CS_Parent
    {
        Hist3Dcolor_TopXColors topX = new Hist3Dcolor_TopXColors();
        Options_Color8UTopX options = new Options_Color8UTopX();

        public CS_Color8U_TopX(VBtask task) : base(task)
        {
            desc = "Classify every BGR pixel into some common colors";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Mat input = src;
            input = input.Resize(task.lowRes, 0, 0, InterpolationFlags.Nearest);

            topX.mapTopX = options.topXcount;
            topX.Run(input);

            List<Vec3b> top = new List<Vec3b>();
            foreach (var pt in topX.topXPixels)
            {
                top.Add(new Vec3b((byte)pt.X, (byte)pt.Y, (byte)pt.Z));
            }

            dst2 = input.Clone();
            for (int y = 0; y < input.Rows; y++)
            {
                for (int x = 0; x < input.Cols; x++)
                {
                    List<float> distances = new List<float>();
                    Vec3b vec = input.Get<Vec3b>(y, x);
                    foreach (var pt in top)
                    {
                        distances.Add(distance3D(pt, new Vec3b(vec.Item0, vec.Item1, vec.Item2)));
                    }
                    Vec3b best = top[distances.IndexOf(distances.Min())];
                    dst2.Set(y, x, new Vec3b(best.Item0, best.Item1, best.Item2));
                }
            }
            labels[2] = "The BGR image mapped to " + topX.mapTopX + " colors";
        }
    }

    public class CS_Color8U_Common : CS_Parent
    {
        List<Vec3b> common = new List<Vec3b>();
        List<Scalar> commonScalar = new List<Scalar> { Scalar.Blue, Scalar.Green, Scalar.Red, Scalar.Yellow, Scalar.Pink, Scalar.Purple, Scalar.Brown,
                                                           Scalar.Gray, Scalar.Black, Scalar.White };

        public CS_Color8U_Common(VBtask task) : base(task)
        {
            foreach (var c in commonScalar)
            {
                common.Add(new Vec3b((byte)c[0], (byte)c[1], (byte)c[2]));
            }
            desc = "Classify every BGR pixel into some common colors";
        }

        public void RunCS(Mat src)
        {
            for (int y = 0; y < src.Rows; y++)
            {
                for (int x = 0; x < src.Cols; x++)
                {
                    List<float> distances = new List<float>();
                    Vec3b vec = src.Get<Vec3b>(y, x);
                    foreach (var pt in common)
                    {
                        distances.Add(distance3D(pt, new Vec3b(vec.Item0, vec.Item1, vec.Item2)));
                    }
                    Vec3b best = common[distances.IndexOf(distances.Min())];
                    dst2.Set(y, x, new Vec3b(best.Item0, best.Item1, best.Item2));
                }
            }
            labels[2] = "The BGR image mapped to " + common.Count + " common colors";
        }
    }

    public class CS_Color8U_Smoothing : CS_Parent
    {
        History_Basics frames = new History_Basics();

        public CS_Color8U_Smoothing(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Averaged BGR image over the last X frames", "" };
            dst0 = new Mat(dst0.Size(), MatType.CV_32FC3, 0);
            desc = "Merge that last X BGR frames to smooth out differences.";
        }

        public void RunCS(Mat src)
        {
            frames.Run(src);
            dst2 = frames.dst2;
            labels[2] = "The image below is the average of " + frames.saveFrames.Count + " the last BGR frames";
        }
    }

    public class CS_Color8U_Denoise : CS_Parent
    {
        Denoise_Pixels denoise = new Denoise_Pixels();

        public CS_Color8U_Denoise(VBtask task) : base(task)
        {
            denoise.standalone = true;
            desc = "Remove single pixels between identical pixels for all color classifiers.";
        }

        public void RunCS(Mat src)
        {
            denoise.Run(src);
            dst2 = denoise.dst2;
            dst3 = denoise.dst3;
            SetTrueText(denoise.strOut, 2);
        }
    }

    public class CS_Color8U_MotionFiltered : CS_Parent
    {
        Color8U_Basics colorClass = new Color8U_Basics();
        public int classCount;
        Motion_Color motionColor = new Motion_Color();

        public CS_Color8U_MotionFiltered(VBtask task) : base(task)
        {
            desc = "Prepare a Color8U_Basics image using the task.motionRect";
        }

        public void RunCS(Mat src)
        {
            motionColor.Run(src);

            dst3 = motionColor.dst2;
            colorClass.Run(motionColor.dst2);
            dst2 = colorClass.dst3;
            classCount = colorClass.classCount;
        }
    }

    public class CS_Color8U_Hue : CS_Parent
    {
        public CS_Color8U_Hue(VBtask task) : base(task)
        {
            desc = "Isolate those regions in the image that have a reddish hue.";
        }

        public void RunCS(Mat src)
        {
            Mat hsv = src.CvtColor(ColorConversionCodes.BGR2HSV);
            Scalar loBins = new Scalar(0, 40, 32);
            Scalar hiBins = new Scalar(180, 255, 255);
            dst2 = hsv.InRange(loBins, hiBins);
        }
    }

    public class CS_Color8U_BlackAndWhite : CS_Parent
    {
        Options_StdevGrid options = new Options_StdevGrid();

        public CS_Color8U_BlackAndWhite(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Mask to identify all 'black' regions", "Mask identifies all 'white' regions" };
            desc = "Create masks for black and white";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            dst1 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            dst2 = dst1.Threshold(options.minThreshold, 255, ThresholdTypes.BinaryInv);
            dst3 = dst1.Threshold(options.maxThreshold, 255, ThresholdTypes.Binary);
        }
    }



    public class CS_Complexity_Basics : CS_Parent
    {
        Complexity_Dots complex = new Complexity_Dots();
        public CS_Complexity_Basics(VBtask task) : base(task) 
        {
            desc = "Plot all the available complexity runs.";
        }
        public void RunCS(Mat src)
        {
            complex.options.RunVB();

            string saveLatestFile = complex.options.filename.FullName;

            complex.maxTime = 0;
            for (int i = 0; i < complex.options.filenames.Count; i++)
            {
                complex.fileName = complex.options.filenames[i];
                complex.Run(src);
            }

            complex.initialize = true;
            for (int i = 0; i < complex.options.filenames.Count; i++)
            {
                complex.fileName = complex.options.filenames[i];
                complex.plotColor = complex.options.setPlotColor();
                complex.Run(src);
                complex.initialize = false;
            }

            dst3 = complex.dst2.Clone();

            SetTrueText(">>>>>> Increasing input data >>>>>>" + Environment.NewLine + "All available complexity runs",
                        new cv.Point(dst2.Width / 4, 10), 3);
            SetTrueText(" TIME " + "(Max = " + complex.maxTime.ToString(fmt0) + ")", new cv.Point(0, dst2.Height / 2), 3);

            complex.initialize = true;
            complex.fileName = saveLatestFile;
            complex.plotColor = complex.options.setPlotColor();
            complex.Run(src);
            dst2 = complex.dst2;

            SetTrueText(" >>>>>> Increasing input data >>>>>>" + Environment.NewLine + complex.options.filename.Name,
                        new cv.Point(dst2.Width / 4, 10));
            SetTrueText(" TIME " + "(Max = " + complex.maxTime.ToString(fmt0) + ")", new cv.Point(0, dst2.Height / 2));
            labels[2] = complex.labels[2];
            labels[3] = "Plots For all available complexity runs";
        }
    }

    public class CS_Complexity_PlotOpenCV : CS_Parent
    {
        public Plot_Basics_CPP plot = new Plot_Basics_CPP();
        public int maxFrameCount;
        public SortedList<int, int> sortData = new SortedList<int, int>(new compareAllowIdenticalInteger());
        public Options_Complexity options = new Options_Complexity();
        public float sessionTime;
        public CS_Complexity_PlotOpenCV(VBtask task) : base(task)
        {
            desc = "Plot the algorithm's input data rate (X) vs. time to complete work on that input (Y).";
        }
        public void prepareSortedData(string filename)
        {
            string contents = File.ReadAllText(filename);
            string[] lines = contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            int nextSize = 0, myFrameCount = 0;
            List<float> times = new List<float>();
            sortData.Clear();

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("Image"))
                {
                    string[] split = trimmedLine.Split('\t');
                    nextSize = int.Parse(split[2]) * int.Parse(split[1]);
                }
                else if (trimmedLine.StartsWith("Ending"))
                {
                    string[] split = trimmedLine.Split('\t');
                    myFrameCount = int.Parse(split[1]);
                    if (myFrameCount > maxFrameCount) maxFrameCount = myFrameCount;
                    times.Add(float.Parse(split[2].Split()[0]));
                }

                if (trimmedLine.StartsWith("-") && nextSize > 0)
                {
                    sortData.Add(nextSize, myFrameCount);
                }
            }

            sessionTime = times.Average();
        }
        public float plotData(float maxTime)
        {
            foreach (var el in sortData)
            {
                plot.srcX.Add(el.Key);
                float nextTime = sessionTime * maxFrameCount / el.Value;
                plot.srcY.Add(nextTime);
                if (nextTime > maxTime) maxTime = nextTime;
            }
            plot.Run(new Mat());
            dst2 = plot.dst2.Clone();
            return maxTime;
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            maxFrameCount = 0;
            plot.srcX.Clear();
            plot.srcY.Clear();
            prepareSortedData(options.filename.FullName);

            float maxTime = plotData(0);

            SetTrueText(">>>>>> Increasing input data >>>>>>", new cv.Point(dst2.Width / 4, 10));
            SetTrueText(" TIME", new cv.Point(0, dst2.Height / 2));
            SetTrueText("Max Time = " + maxTime.ToString(fmt0), new cv.Point(10, 10));
            labels[2] = "Complexity plot for " + Path.GetFileNameWithoutExtension(options.filename.Name);
        }
    }

    public class CS_Complexity_Dots : CS_Parent
    {
        public Options_Complexity options = new Options_Complexity();
        public bool initialize = true;
        public float maxTime;
        public string fileName;
        public Scalar plotColor;
        Mat dst;
        public CS_Complexity_Dots(VBtask task) : base(task)
        {
            dst = new Mat(new cv.Size(task.lowRes.Width * 2, task.lowRes.Height * 2), MatType.CV_8UC3, Scalar.Black);
            desc = "Plot the results of multiple runs at various resolutions.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (!string.IsNullOrEmpty(fileName)) options.filename = new FileInfo(fileName);
            string contents = File.ReadAllText(options.filename.FullName);
            string[] lines = contents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            SortedList<int, int> sortData = new SortedList<int, int>(new compareAllowIdenticalInteger());
            int nextSize = 0, myFrameCount = 0;
            List<float> times = new List<float>();
            float maxFrameCount = 0;
            List<double> srcX = new List<double>(), srcY = new List<double>();

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("Image"))
                {
                    string[] split = trimmedLine.Split('\t');
                    nextSize = int.Parse(split[2]) * int.Parse(split[1]);
                }
                else if (trimmedLine.StartsWith("Ending"))
                {
                    string[] split = trimmedLine.Split('\t');
                    myFrameCount = int.Parse(split[1]);
                    if (myFrameCount > maxFrameCount) maxFrameCount = myFrameCount;
                    times.Add(float.Parse(split[2].Split()[0]));
                }

                if (trimmedLine.StartsWith("-") && nextSize > 0)
                {
                    int index = srcX.IndexOf(nextSize);
                    if (index != -1)
                    {
                        srcY[index] = (myFrameCount + srcY[index]) / 2;
                    }
                    else
                    {
                        srcX.Add(nextSize);
                        srcY.Add(myFrameCount);
                    }
                }
            }

            float sessionTime = times.Average();

            for (int i = 0; i < srcX.Count; i++)
            {
                float nextTime = sessionTime * maxFrameCount / (float)srcY[i];
                if (maxTime < nextTime) maxTime = nextTime;
                sortData.Add((int)srcX[i], (int) nextTime);
            }

            double maxX = srcX.Max();
            List<cv.Point> pointSet = new List<cv.Point>();
            if (initialize) dst.SetTo(Scalar.Black);

            for (int i = 0; i < sortData.Count; i++)
            {
                cv.Point pt = new cv.Point(dst.Width * sortData.ElementAt(i).Key / maxX,
                                     dst.Height - dst.Height * sortData.ElementAt(i).Value / maxTime);
                Cv2.Circle(dst, pt, task.DotSize, plotColor, -1);
                pointSet.Add(pt);
            }

            for (int i = 1; i < pointSet.Count; i++)
            {
                Cv2.Line(dst, pointSet[i - 1], pointSet[i], plotColor);
            }

            SetTrueText(">>>>>> Increasing input data >>>>>>" + Environment.NewLine + options.filename.Name,
                        new cv.Point(dst2.Width / 4, 10));
            SetTrueText(" TIME " + "(Max = " + maxTime.ToString(fmt0) + ")", new cv.Point(0, dst2.Height / 2));
            labels[2] = "Complexity plot for " + Path.GetFileNameWithoutExtension(options.filename.Name);
            dst2 = dst.Resize(dst2.Size());
        }
    }



    public class CS_Concat_Basics : CS_Parent
    {
        public CS_Concat_Basics(VBtask task) : base(task)
        {
            labels[2] = "Horizontal concatenation";
            labels[3] = "Vertical concatenation";
            desc = "Concatenate 2 images - horizontally and vertically";
        }
        public void RunCS(Mat src)
        {
            Mat tmp = new Mat();
            Cv2.HConcat(src, task.depthRGB, tmp);
            dst2 = tmp.Resize(src.Size());
            Cv2.VConcat(src, task.depthRGB, tmp);
            dst3 = tmp.Resize(src.Size());
        }
    }

    public class CS_Concat_4way : CS_Parent
    {
        public Mat[] img = new Mat[4];
        public CS_Concat_4way(VBtask task) : base(task)
        {
            for (int i = 0; i < img.Length; i++)
            {
                img[i] = new Mat();
            }
            labels[2] = "Color/RGBDepth/Left/Right views";
            desc = "Concatenate 4 images - horizontally and vertically";
        }
        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                img[0] = src;
                img[1] = task.depthRGB;
                img[2] = task.leftView.Channels() == 1 ? task.leftView.CvtColor(ColorConversionCodes.GRAY2BGR) : task.leftView;
                img[3] = task.rightView.Channels() == 1 ? task.rightView.CvtColor(ColorConversionCodes.GRAY2BGR) : task.rightView;
            }

            Mat tmp1 = new Mat();
            Mat tmp2 = new Mat();
            Mat tmp3 = new Mat();

            Cv2.HConcat(img[0], img[1], tmp1);
            Cv2.HConcat(img[2], img[3], tmp2);
            Cv2.VConcat(tmp1, tmp2, tmp3);
            dst2 = tmp3.Resize(src.Size());
        }
    }



    public class CS_Contour_Basics : CS_Parent
    {
        Color8U_Basics colorClass = new Color8U_Basics();
        public List<cv.Point[]> contourlist = new List<cv.Point[]>();
        public cv.Point[][] allContours;
        public Options_Contours options = new Options_Contours();
        public SortedList<int, int> sortedList = new SortedList<int, int>(new compareAllowIdenticalIntegerInverted());

        public CS_Contour_Basics(VBtask task) : base(task)
        {
            FindRadio("FloodFill").Checked = true;
            UpdateAdvice(traceName + ": redOptions color class determines the input. Use local options in 'Options_Contours' to further control output.");
            labels = new string[] { "", "", "FindContour input", "Draw contour output" };
            desc = "General purpose contour finder";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            colorClass.Run(src);
            dst2 = colorClass.dst2;

            if (options.retrievalMode == RetrievalModes.FloodFill)
            {
                dst2.ConvertTo(dst1, MatType.CV_32SC1);
                Cv2.FindContours(dst1, out allContours, out _, RetrievalModes.FloodFill, ContourApproximationModes.ApproxSimple);
            }
            else
            {
                Cv2.FindContours(dst2, out allContours, out _, options.retrievalMode, options.ApproximationMode);
            }
            if (allContours.Length <= 1) return;

            sortedList.Clear();
            for (int i = 0; i < allContours.Length; i++)
            {
                if (allContours[i].Length < 4) continue;
                double count = Cv2.ContourArea(allContours[i]);
                if (count > 2) sortedList.Add((int)count, i);
            }

            dst3.SetTo(0);
            contourlist.Clear();
            dst2 = colorClass.dst3;
            for (int i = 0; i < sortedList.Count; i++)
            {
                cv.Point[] tour = allContours[sortedList.ElementAt(i).Value];
                contourlist.Add(tour);
                Scalar color = vecToScalar(dst2.Get<Vec3b>(tour[0].Y, tour[0].X));
                DrawContour(dst3, tour.ToList(), color, -1);
            }
            labels[3] = $"Top {sortedList.Count} contours found";
        }
    }

    public class CS_Contour_General : CS_Parent
    {
        public List<cv.Point[]> contourlist = new List<cv.Point[]>();
        public cv.Point[][] allContours;
        public Options_Contours options = new Options_Contours();
        Rectangle_Rotated rotatedRect = new Rectangle_Rotated();
        int minLengthContour = 4; // use any contour With enough points To make a contour!

        public CS_Contour_General(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "FindContour input", "Draw contour output" };
            desc = "General purpose contour finder";
        }

        public void RunCS(Mat src)
        {
            if (standalone)
            {
                if (!task.heartBeat) return;
                rotatedRect.Run(src);
                dst2 = rotatedRect.dst2;
                dst2 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                dst2 = src.Channels() == 3 ? src.CvtColor(ColorConversionCodes.BGR2GRAY) : src;
            }

            if (dst2.Type() == MatType.CV_8U)
            {
                Cv2.FindContours(dst2, out allContours, out _, RetrievalModes.External, ContourApproximationModes.ApproxTC89KCOS);
            }
            else
            {
                if (dst2.Type() != MatType.CV_32S) dst2.ConvertTo(dst2, MatType.CV_32S);
                Cv2.FindContours(dst2, out allContours, out _, RetrievalModes.FloodFill, ContourApproximationModes.ApproxTC89KCOS);
            }

            contourlist.Clear();
            foreach (var c in allContours)
            {
                double area = Cv2.ContourArea(c);
                if (area >= options.minPixels && c.Length >= minLengthContour) contourlist.Add(c);
            }

            dst3.SetTo(0);
            foreach (var ctr in allContours)
            {
                DrawContour(dst3, ctr.ToList(), Scalar.Yellow);
            }
        }
    }

    public class CS_Contour_GeneralWithOptions : CS_Parent
    {
        public List<cv.Point[]> contourlist = new List<cv.Point[]>();
        public cv.Point[][] allContours;
        public Options_Contours options = new Options_Contours();
        Rectangle_Rotated rotatedRect = new Rectangle_Rotated();
        int minLengthContour = 4; // use any contour With enough points To make a contour!
        public CS_Contour_GeneralWithOptions(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "FindContour input", "Draw contour output" };
            desc = "General purpose contour finder";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (standaloneTest())
            {
                if (!task.heartBeat) return;
                rotatedRect.Run(src);
                dst2 = rotatedRect.dst2;
                if (dst2.Channels() == 3)
                {
                    dst2 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255);
                }
                else
                {
                    dst2 = dst2.ConvertScaleAbs(255);
                }
            }
            else
            {
                dst2 = src.Channels() == 3 ? src.CvtColor(ColorConversionCodes.BGR2GRAY) : src;
            }

            if (options.retrievalMode == RetrievalModes.FloodFill) dst2.ConvertTo(dst2, MatType.CV_32SC1);
            Cv2.FindContours(dst2, out allContours, out _, options.retrievalMode, options.ApproximationMode);

            contourlist.Clear();
            foreach (var c in allContours)
            {
                double area = Cv2.ContourArea(c);
                if (area >= options.minPixels && c.Length >= minLengthContour) contourlist.Add(c);
            }

            dst3.SetTo(0);
            foreach (var ctr in allContours)
            {
                DrawContour(dst3, ctr.ToList(), Scalar.Yellow);
            }
        }
    }



    public class CS_Contour_RotatedRects : CS_Parent
    {
        public Rectangle_Rotated rotatedRect = new Rectangle_Rotated();
        Contour_General basics = new Contour_General();

        public CS_Contour_RotatedRects(VBtask task) : base(task)
        {
            labels[3] = "Find contours of several rotated rects";
            desc = "Demo options on FindContours.";
        }

        public void RunCS(Mat src)
        {
            Mat imageInput = new Mat();
            rotatedRect.Run(src);
            imageInput = rotatedRect.dst2;
            if (imageInput.Channels() == 3)
            {
                dst2 = imageInput.CvtColor(ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255);
            }
            else
            {
                dst2 = imageInput.ConvertScaleAbs(255);
            }

            basics.Run(dst2);
            dst2 = basics.dst2;
            dst3 = basics.dst3;
        }
    }

    //public class CS_Contour_RemoveLines : CS_Parent
    //{
    //    Options_Morphology options = new Options_Morphology();
    //    public CS_Contour_RemoveLines(VBtask task) : base(task)
    //    {
    //        labels[2] = "Original image";
    //        labels[3] = "Original with horizontal/vertical lines removed";
    //        desc = "Remove the lines from an invoice image";
    //    }

    //    public void RunCS(Mat src)
    //    {
    //        options.RunVB();

    //        Mat input = FeatureSrc;
    //        Mat tmp = Cv2.ImRead(task.HomeDir + "Data/invoice.jpg");
    //        float height = src.Height;
    //        float factor = (float)(src.Height / tmp.Height);
    //        cv.Size dstSize = new cv.Size(factor * src.Width), src.Height);
    //        cv.Rect dstRect = new cv.Rect(0, 0, dstSize.Width, src.Height);
    //        tmp = tmp.Resize(dstSize);
    //        dst2 = tmp.Resize(dst2.Size());
    //        Mat gray = tmp.CvtColor(ColorConversionCodes.BGR2GRAY);
    //        Mat thresh = gray.Threshold(0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

    //        // remove horizontal lines
    //        Mat hkernel = Cv2.GetStructuringElement(MorphShapes.Rect, new cv.Size(options.widthHeight, 1));
    //        Mat removedH = new Mat();
    //        Cv2.MorphologyEx(thresh, removedH, MorphTypes.Open, hkernel, iterations: options.iterations);
    //        cv.Point[][] cnts = Cv2.FindContoursAsArray(removedH, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
    //        for (int i = 0; i < cnts.Length; i++)
    //        {
    //            Cv2.DrawContours(tmp, cnts, i, Scalar.White, task.lineWidth);
    //        }

    //        Mat vkernel = Cv2.GetStructuringElement(MorphShapes.Rect, new cv.Size(1, options.widthHeight));
    //        Mat removedV = new Mat();
    //        Cv2.MorphologyEx(thresh, removedV, MorphTypes.Open, vkernel, iterations: options.iterations);
    //        cnts = Cv2.FindContoursAsArray(removedV, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
    //        for (int i = 0; i < cnts.Length; i++)
    //        {
    //            Cv2.DrawContours(tmp, cnts, i, Scalar.White, task.lineWidth);
    //        }

    //        dst3 = tmp.Resize(dst3.Size());
    //        Cv2.ImShow("Altered image at original resolution", tmp);
    //    }
    //}

    public class CS_Contour_Edges : CS_Parent
    {
        Edge_ResizeAdd edges = new Edge_ResizeAdd();
        Contour_General contour = new Contour_General();
        Mat lastImage;
        int minLengthContour = 4; // use any contour With enough points To make a contour!
        public CS_Contour_Edges(VBtask task) : base(task)
        {
            lastImage = new Mat(task.WorkingRes, MatType.CV_8UC3, 0);
            desc = "Create contours for Edge_MotionAccum";
        }

        public void RunCS(Mat src)
        {
            edges.Run(src);
            dst2 = edges.dst2;

            contour.Run(dst2);

            dst3.SetTo(0);
            List<Vec3b> colors = new List<Vec3b>();
            Scalar color;
            foreach (var c in contour.allContours)
            {
                if (c.Length > minLengthContour)
                {
                    Vec3b vec = lastImage.Get<Vec3b>(c[0].Y, c[0].X);
                    if (vec == black || colors.Contains(vec))
                    {
                        color = new Scalar(msRNG.Next(10, 240), msRNG.Next(10, 240), msRNG.Next(10, 240)); // trying to avoid extreme colors... 
                    }
                    else
                    {
                        color = new Scalar(vec[0], vec[1], vec[2]);
                    }
                    colors.Add(vec);
                    DrawContour(dst3, c.ToList(), color, -1);
                }
            }
            lastImage = dst3.Clone();
        }
    }

    public class CS_Contour_SidePoints : CS_Parent
    {
        public Vec3f vecLeft, vecRight, vecTop, vecBot;
        public cv.Point ptLeft, ptRight, ptTop, ptBot;
        public Profile_Basics sides = new Profile_Basics();

        public CS_Contour_SidePoints(VBtask task) : base(task)
        {
            desc = "Find the left/right and top/bottom sides of a contour";
        }

        string Vec3fToString(Vec3f v)
        {
            return string.Format("{0:F3}\t{1:F3}\t{2:F3}", v[0], v[1], v[2]);
        }

        public void RunCS(Mat src)
        {
            sides.Run(src);
            dst2 = sides.dst2;
            var rc = task.rc;

            if (sides.corners.Count > 0 && task.heartBeat)
            {
                ptLeft = sides.corners[1];
                ptRight = sides.corners[2];
                ptTop = sides.corners[3];
                ptBot = sides.corners[4];

                vecLeft = sides.corners3D[1];
                vecRight = sides.corners3D[2];

                vecTop = sides.corners3D[3];
                vecBot = sides.corners3D[4];

                if (rc.contour.Count > 0)
                {
                    dst3.SetTo(0);
                    DrawContour(dst3[rc.rect], rc.contour, Scalar.Yellow);
                    Cv2.Line(dst3, ptLeft, ptRight, Scalar.White);
                    Cv2.Line(dst3, ptTop, ptBot, Scalar.White);
                }
                if (task.heartBeat)
                {
                    strOut = "X     \tY     \tZ \t 3D location (units=meters)\n";
                    strOut += Vec3fToString(vecLeft) + "\t Left side average (blue)\n";
                    strOut += Vec3fToString(vecRight) + "\t Right side average (red)\n";
                    strOut += Vec3fToString(vecTop) + "\t Top side average (green)\n";
                    strOut += Vec3fToString(vecBot) + "\t Bottom side average (white)\n\n";
                    strOut += "The contour may show points further away but they don't have depth.";
                }
            }
            SetTrueText(strOut, 3);
        }
    }


    public class CS_Contour_Foreground : CS_Parent
    {
        Foreground_KMeans2 km = new Foreground_KMeans2();
        Contour_General contour = new Contour_General();

        public CS_Contour_Foreground(VBtask task) : base(task)
        {
            dst3 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            labels = new string[] { "", "", "Kmeans foreground output", "Contour of foreground" };
            desc = "Build a contour for the foreground";
        }

        public void RunCS(Mat src)
        {
            km.Run(task.pcSplit[2]);
            dst2 = km.dst2;

            contour.Run(dst2);
            dst3.SetTo(0);
            foreach (var ctr in contour.contourlist)
            {
                Cv2.DrawContours(dst3, new[] { ctr }, 0, new Scalar(255), -1);
            }
        }
    }

    public class CS_Contour_Sorted : CS_Parent
    {
        Contour_GeneralWithOptions contours = new Contour_GeneralWithOptions();
        SortedList<int, cv.Point[]> sortedContours = new SortedList<int, cv.Point[]>(new compareAllowIdenticalIntegerInverted());
        SortedList<int, int> sortedByArea = new SortedList<int, int>(new compareAllowIdenticalIntegerInverted());
        Diff_Basics diff = new Diff_Basics();
        Erode_Basics erode = new Erode_Basics();
        Dilate_Basics dilate = new Dilate_Basics();
        Options_Contours options = new Options_Contours();
        int minLengthContour = 4; // use any contour With enough points To make a contour!
        public CS_Contour_Sorted(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "", "Contours in the detected motion", "Diff output - detected motion" };
            task.gOptions.pixelDiffThreshold = 25;
            desc = "Display the contours from largest to smallest in the motion output";
        }

        public void RunCS(Mat src)
        {
            diff.Run(src);
            erode.Run(diff.dst2); // remove solo points.

            contours.Run(diff.dst2);
            dst2 = contours.dst2;
            dst1 = contours.dst2.Clone();

            sortedByArea.Clear();
            sortedContours.Clear();
            dst3.SetTo(0);
            for (int i = 0; i < contours.contourlist.Count; i++)
            {
                double area = Cv2.ContourArea(contours.contourlist[i]);
                if (area > options.minPixels && contours.contourlist[i].Length > minLengthContour)
                {
                    sortedByArea.Add((int)area, i);
                    sortedContours.Add((int)area, Cv2.ApproxPolyDP(contours.contourlist[i], contours.options.epsilon, true));
                    Cv2.DrawContours(dst3, new[] { contours.contourlist[i] }, 0, Scalar.White, -1);
                }
            }

            dilate.Run(dst3);
            dst3 = dilate.dst2;

            int beforeCount = Cv2.CountNonZero(dst1);
            dst1.SetTo(0, dst3);
            int afterCount = Cv2.CountNonZero(dst1);
            SetTrueText($"Before dilate: {beforeCount}\nAfter dilate {afterCount}\nRemoved = {beforeCount - afterCount}", 1);

            SetTrueText($"The motion detected produced {sortedContours.Count} contours after filtering for length and area.", 3);
        }
    }

    public class CS_Contour_Outline : CS_Parent
    {
        public rcData rc = new rcData();
        RedCloud_Basics redC = new RedCloud_Basics();

        public CS_Contour_Outline(VBtask task) : base(task)
        {
            desc = "Create a simplified contour of the selected cell";
        }

        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            List<cv.Point> ptList = rc.contour;

            dst3.SetTo(0);

            List<cv.Point> newContour = new List<cv.Point>();
            rc = task.rc;
            if (rc.contour.Count == 0) return;
            cv.Point p1 = new cv.Point(0, 0), p2;
            newContour.Add(p1);
            for (int i = 0; i < rc.contour.Count - 1; i++)
            {
                p1 = rc.contour[i];
                p2 = rc.contour[i + 1];
                Cv2.Line(dst3[rc.rect], p1, p2, Scalar.White, task.lineWidth + 1);
                newContour.Add(p2);
            }
            rc.contour = new List<cv.Point>(newContour);
            Cv2.Line(dst3[rc.rect], rc.contour[rc.contour.Count - 1], rc.contour[0], Scalar.White, task.lineWidth + 1);

            labels[2] = $"Input points = {rc.contour.Count}";
        }
    }

    public class CS_Contour_SelfIntersect : CS_Parent
    {
        public rcData rc = new rcData();
        RedCloud_Basics redC = new RedCloud_Basics();

        public CS_Contour_SelfIntersect(VBtask task) : base(task)
        {
            desc = "Search the contour points for duplicates indicating the contour is self-intersecting.";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                redC.Run(src);
                dst2 = redC.dst2;
                rc = task.rc;
                DrawContour(dst2[rc.rect], rc.contour, cv.Scalar.White);
                labels[2] = redC.labels[2];
            }

            bool selfInt = false;
            HashSet<string> ptSet = new HashSet<string>();
            dst3 = rc.mask.CvtColor(ColorConversionCodes.GRAY2BGR);
            for (int i = 0; i < rc.contour.Count; i++)
            {
                cv.Point pt = rc.contour[i];
                string ptStr = $"{pt.X:0000}{pt.Y:0000}";
                if (ptSet.Contains(ptStr))
                {
                    double pct = (double)i / rc.contour.Count;
                    if (pct > 0.1 && pct < 0.9)
                    {
                        selfInt = true;
                        Cv2.Circle(dst3, pt, task.DotSize, Scalar.Red, -1);
                    }
                }
                ptSet.Add(ptStr);
            }
            labels[3] = selfInt ? "Self intersecting - red shows where" : "Not self-intersecting";
        }
    }




    public class CS_Contour_Largest : CS_Parent
    {
        public List<cv.Point> bestContour = new List<cv.Point>();
        public cv.Point[][] allContours;
        public Options_Contours options = new Options_Contours();
        Rectangle_Rotated rotatedRect = new Rectangle_Rotated();

        public CS_Contour_Largest(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": use the local options in 'Options_Contours'");
            labels = new string[] { "", "", "Input to FindContours", "Largest single contour in the input image." };
            desc = "Create a mask from the largest contour of the input.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (standaloneTest())
            {
                if (task.heartBeat)
                {
                    rotatedRect.Run(src);
                    dst2 = rotatedRect.dst2;
                }
            }
            else
            {
                dst2 = src;
            }

            if (dst2.Channels() != 1)
                dst2 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);

            if (options.retrievalMode == RetrievalModes.FloodFill)
            {
                dst2.ConvertTo(dst1, MatType.CV_32SC1);
                Cv2.FindContours(dst1, out allContours, out _, options.retrievalMode, options.ApproximationMode);
                dst1.ConvertTo(dst3, MatType.CV_8UC1);
            }
            else
            {
                Cv2.FindContours(dst2, out allContours, out _, options.retrievalMode, options.ApproximationMode);
            }

            int maxCount = 0, maxIndex = -1;
            if (allContours.Length == 0) return;

            for (int i = 0; i < allContours.Length; i++)
            {
                int len = allContours[i].Length;
                if (len > maxCount)
                {
                    maxCount = len;
                    maxIndex = i;
                }
            }

            bestContour = allContours[maxIndex].ToList();

            if (standaloneTest())
            {
                dst3.SetTo(0);
                if (maxIndex >= 0 && maxCount >= 2)
                {
                    DrawContour(dst3, allContours[maxIndex].ToList(), Scalar.White);
                }
            }
        }
    }

    public class CS_Contour_Compare : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        public Options_Contours options = new Options_Contours();

        public CS_Contour_Compare(VBtask task) : base(task)
        {
            desc = "Compare findContours options - ApproxSimple, ApproxNone, etc.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];

            Mat tmp = task.rc.mask.Clone();

            cv.Point[][] allContours;
            if (options.retrievalMode == RetrievalModes.FloodFill)
                tmp.ConvertTo(tmp, MatType.CV_32SC1);

            Cv2.FindContours(tmp, out allContours, out _, RetrievalModes.External, options.ApproximationMode);

            dst3.SetTo(0);
            Cv2.DrawContours(dst3[task.rc.rect], allContours, -1, Scalar.Yellow);
        }
    }

    public class CS_Contour_RedCloudCorners : CS_Parent
    {
        public cv.Point[] corners = new cv.Point[4];
        public rcData rc = new rcData();
        RedCloud_Basics redC = new RedCloud_Basics();

        public CS_Contour_RedCloudCorners(VBtask task) : base(task)
        {
            labels[2] = "The RedCloud Output with the highlighted contour to smooth";
            desc = "Find the point farthest from the center in each cell.";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                redC.Run(src);
                dst2 = redC.dst2;
                labels[2] = redC.labels[2];
                rc = task.rc;
            }

            dst3.SetTo(0);
            Cv2.Circle(dst3, rc.maxDist, task.DotSize, Scalar.White, -1);
            cv.Point center = new cv.Point(rc.maxDist.X - rc.rect.X, rc.maxDist.Y - rc.rect.Y);
            float[] maxDistance = new float[4];

            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = center; // default is the center - a triangle shape can omit a corner
            }

            if (rc.contour == null) return;

            foreach (cv.Point pt in rc.contour)
            {
                int quad;
                if (pt.X - center.X >= 0 && pt.Y - center.Y <= 0) quad = 0; // upper right quadrant
                else if (pt.X - center.X >= 0 && pt.Y - center.Y >= 0) quad = 1; // lower right quadrant
                else if (pt.X - center.X <= 0 && pt.Y - center.Y >= 0) quad = 2; // lower left quadrant
                else quad = 3; // upper left quadrant

                float dist = (float)Math.Sqrt(Math.Pow(center.X - pt.X, 2) + Math.Pow(center.Y - pt.Y, 2));
                if (dist > maxDistance[quad])
                {
                    maxDistance[quad] = dist;
                    corners[quad] = pt;
                }
            }

            DrawContour(dst3[rc.rect], rc.contour, Scalar.White);
            for (int i = 0; i < corners.Length; i++)
            {
                Cv2.Line(dst3[rc.rect], center, corners[i], Scalar.White);
            }
        }
    }



    public class CS_Contour_Gray : CS_Parent
    {
        public List<cv.Point> contour = new List<cv.Point>();
        public Options_Contours options = new Options_Contours();
        int myFrameCount;
        Reduction_Basics reduction = new Reduction_Basics();

        public CS_Contour_Gray(VBtask task) : base(task)
        {
            myFrameCount = task.frameCount;
            desc = "Find the contour for the src.";
        }

        public void RunCS(Mat src)
        {
            if (myFrameCount != task.frameCount)
            {
                options.RunVB(); // avoid running options more than once per frame.
                myFrameCount = task.frameCount;
            }

            if (standalone)
            {
                task.redOptions.setColorSource("Reduction_Basics");
                reduction.Run(src);
                src = reduction.dst2;
            }

            cv.Point[][] allContours;
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            Cv2.FindContours(src, out allContours, out _, RetrievalModes.External, options.ApproximationMode);
            if (allContours.Length == 0)
                return;

            dst2 = src;
            foreach (var tour in allContours)
            {
                DrawContour(dst2, tour.ToList(), Scalar.White, task.lineWidth);
            }
            labels[2] = $"There were {allContours.Length} contours found.";
        }
    }

    public class CS_Contour_WholeImage : CS_Parent
    {
        Contour_Basics contour = new Contour_Basics();

        public CS_Contour_WholeImage(VBtask task) : base(task)
        {
            FindSlider("Max contours").Value = 20;
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Find the top X contours by size and display them.";
        }

        public void RunCS(Mat src)
        {
            contour.Run(src);
            var sortedContours = new SortedList<int, List<cv.Point>>(new compareAllowIdenticalIntegerInverted());
            foreach (var tour in contour.contourlist)
            {
                sortedContours.Add(tour.Length, tour.ToList());
            }

            dst2.SetTo(0);
            for (int i = 0; i < sortedContours.Count; i++)
            {
                var tour = sortedContours.ElementAt(i).Value;
                DrawContour(dst2, tour, 255, task.lineWidth);
            }
        }
    }

    public class CS_Contour_DepthTiers : CS_Parent
    {
        public Options_Contours options = new Options_Contours();
        public int classCount;
        public List<cv.Point[]> contourlist = new List<cv.Point[]>();

        public CS_Contour_DepthTiers(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            FindRadio("FloodFill").Checked = true;
            UpdateAdvice(traceName + ": redOptions color class determines the input.  Use local options in 'Options_Contours' to further control output.");
            labels = new string[] { "", "", "FindContour input", "Draw contour output" };
            desc = "General purpose contour finder";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            task.pcSplit[2].ConvertTo(dst1, MatType.CV_32S, 100 / options.cmPerTier, 1);

            cv.Point[][] allContours;
            Cv2.FindContours(dst1, out allContours, out _, RetrievalModes.FloodFill, ContourApproximationModes.ApproxSimple);
            if (allContours.Length <= 1)
                return;

            var sortedList = new SortedList<int, int>(new compareAllowIdenticalIntegerInverted());
            for (int i = 0; i < allContours.Length; i++)
            {
                if (allContours[i].Length < 4)
                    continue;
                int count = (int)Cv2.ContourArea(allContours[i]);
                if (count < options.minPixels)
                    continue;
                if (count > 2)
                    sortedList.Add(count, i);
            }

            dst2.SetTo(0);
            contourlist.Clear();
            for (int i = 0; i < sortedList.Count; i++)
            {
                var tour = allContours[sortedList.ElementAt(i).Value];
                byte val = dst2.Get<byte>(tour[0].Y, tour[0].X);
                if (val == 0)
                {
                    int index = dst1.Get<int>(tour[0].Y, tour[0].X);
                    contourlist.Add(tour);
                    DrawContour(dst2, tour.ToList(), index, -1);
                }
            }

            dst2.SetTo(1, dst2.Threshold(0, 255, ThresholdTypes.BinaryInv));
            classCount = (int)(task.MaxZmeters * 100 / options.cmPerTier);

            if (standaloneTest())
                dst3 = ShowPalette(dst2 * 255 / classCount);
            labels[3] = $"All depth pixels are assigned a tier with {classCount} contours.";
        }
    }

    public class CS_Contour_FromPoints : CS_Parent
    {
        Contour_Basics contour = new Contour_Basics();
        Random_Basics random = new Random_Basics();

        public CS_Contour_FromPoints(VBtask task) : base(task)
        {
            FindSlider("Random Pixel Count").Value = 3;
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            desc = "Create a contour from some random points";
        }

        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                random.Run(src);
                dst2.SetTo(0);
                foreach (var p1 in random.pointList)
                {
                    foreach (var p2 in random.pointList)
                    {
                        DrawLine(dst2, p1, p2, Scalar.White, task.lineWidth);
                    }
                }
            }

            var hullPoints = Cv2.ConvexHull(random.pointList.ToArray(), true).ToList();

            var hull = new List<cv.Point>();
            foreach (var pt in hullPoints)
            {
                hull.Add(new cv.Point(pt.X, pt.Y));
            }

            dst3.SetTo(0);
            DrawContour(dst3, hull, Scalar.White, -1);
        }
    }



    public class CS_Contrast_POW : CS_Parent
    {
        Options_BrightnessContrast options = new Options_BrightnessContrast();
        public CS_Contrast_POW(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Original Image", "Contrast reduced with POW function" };
            desc = "Reduce contrast with POW function";
        }
         
        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            dst2.ConvertTo(dst3, MatType.CV_32FC3);
            dst3 = dst3.Normalize();
            dst3 = dst3.Pow(options.exponent);
        }
    }

    public class CS_Contrast_Basics : CS_Parent
    {
        Options_BrightnessContrast options = new Options_BrightnessContrast();

        public CS_Contrast_Basics(VBtask task) : base(task)
        {
            labels[2] = "Brightness/Contrast";
            UpdateAdvice(traceName + ": use the local options to control brightness and contrast.");
            desc = "Show image with varying contrast and brightness.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = src.ConvertScaleAbs(options.brightness, options.contrast);
        }
    }


    public class CS_Convex_Basics : CS_Parent
    {
        public cv.Point[] hull;
        Options_Convex options = new Options_Convex();

        public CS_Convex_Basics(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": use the local options to control the number of points.");
            desc = "Surround a set of random points with a convex hull";
            labels = new string[] { "", "", "Convex Hull - red dot is center and the black dots are the input points", "" };
        }

        public List<cv.Point> BuildRandomHullPoints()
        {
            int pad = 4;
            int w = dst2.Width - dst2.Width / pad;
            int h = dst2.Height - dst2.Height / pad;

            var hullList = new List<cv.Point>();
            for (int i = 0; i < options.hullCount; i++)
            {
                hullList.Add(new cv.Point(msRNG.Next(dst2.Width / pad, w), msRNG.Next(dst2.Height / pad, h)));
            }
            return hullList;
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            List<cv.Point> hullList = task.rc.contour;
            if (standaloneTest())
            {
                if (!task.heartBeat) return;
                hullList = BuildRandomHullPoints();
            }

            if (hullList.Count == 0)
            {
                SetTrueText("No points were provided. Update hullList before running.");
                return;
            }

            hull = Cv2.ConvexHull(hullList.ToArray(), true);

            dst2.SetTo(0);

            using (var pMat = new Mat(hull.Length, 1, MatType.CV_32SC2, hull))
            {
                Scalar sum = pMat.Sum();
                DrawContour(dst2, hullList, Scalar.White, -1);

                for (int i = 0; i < hull.Length; i++)
                {
                    Cv2.Line(dst2, hull[i], hull[(i + 1) % hull.Length], Scalar.White);
                }
            }
        }
    }

    public class CS_Convex_RedCloud : CS_Parent
    {
        CS_Convex_Basics convex;
        public RedCloud_Basics redC = new RedCloud_Basics();

        public CS_Convex_RedCloud(VBtask task) : base(task)
        {
            convex = new CS_Convex_Basics(task);
            labels = new string[] { "", "", "Selected contour - line shows hull with white is contour. Click to select another contour.", "RedCloud cells" };
            desc = "Get lots of odd shapes from the RedCloud_Basics output and use ConvexHull to simplify them.";
        }

        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;

            if (task.rc.contour != null)
            {
                convex.RunAndMeasure(src, convex);

                dst3.SetTo(0);
                convex.dst2[new Rect(0, 0, task.rc.rect.Width, task.rc.rect.Height)].CopyTo(dst3[task.rc.rect]);
                Cv2.Circle(dst3, task.rc.maxDist, task.DotSize, Scalar.White, -1);
            }
        }
    }

    public class CS_Convex_Defects : CS_Parent
    {
        CS_Contour_Largest contours;

        public CS_Convex_Defects(VBtask task) : base(task)
        {
            contours = new CS_Contour_Largest(task);
            dst2 = Cv2.ImRead(task.HomeDir + "Data/star2.png").Threshold(200, 255, ThresholdTypes.Binary).Resize(task.WorkingRes);
            dst2 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);

            labels = new string[] { "", "", "Input to the ConvexHull and ConvexityDefects", "Yellow = ConvexHull, Red = ConvexityDefects, Yellow dots are convexityDefect 'Far' points" };
            desc = "Find the convexityDefects in the image";
        }

        public void RunCS(Mat src)
        {
            contours.RunAndMeasure(dst2.Clone(), contours);
            cv.Point[] c = contours.bestContour.ToArray();
            dst3 = dst2.CvtColor(ColorConversionCodes.GRAY2BGR);
            cv.Point[] hull = Cv2.ConvexHull(c, false);
            int[] hullIndices = Cv2.ConvexHullIndices(c, false);
            DrawContour(dst3, hull.ToList(), task.HighlightColor);

            Vec4i[] defects = Cv2.ConvexityDefects(contours.bestContour, hullIndices.ToList());
            foreach (Vec4i v in defects)
            {
                Cv2.Line(dst3, c[v[0]], c[v[2]], Scalar.Red, task.lineWidth + 1, LineTypes.Link8);
                Cv2.Line(dst3, c[v[1]], c[v[2]], Scalar.Red, task.lineWidth + 1, LineTypes.Link8);
                Cv2.Circle(dst3, c[v[2]], task.DotSize + 2, task.HighlightColor, -1);
            }
        }
    }

    public class CS_Convex_RedCloudDefects : CS_Parent
    {
        CS_Convex_RedCloud convex;
        CS_Contour_Largest contours;

        public CS_Convex_RedCloudDefects(VBtask task) : base(task)
        {
            convex = new CS_Convex_RedCloud(task);
            contours = new CS_Contour_Largest(task);

            if (standaloneTest()) task.gOptions.setDisplay1();
            labels = new string[] { "", "", "Hull outline in green, lines show defects.", "Output of RedCloud_Basics" };
            desc = "Find the convexityDefects in the selected RedCloud cell";
        }

        public List<cv.Point> BetterContour(List<cv.Point> c, Vec4i[] defects)
        {
            int lastV = -1;
            var newC = new List<cv.Point>();
            foreach (Vec4i v in defects)
            {
                if (v[0] != lastV && lastV >= 0)
                {
                    for (int i = lastV; i < v[0]; i++)
                    {
                        newC.Add(c[i]);
                    }
                }
                newC.Add(c[v[0]]);
                newC.Add(c[v[2]]);
                newC.Add(c[v[1]]);
                lastV = v[1];
            }
            if (defects.Length > 0)
            {
                if (lastV != defects[0][0])
                {
                    for (int i = lastV; i < c.Count; i++)
                    {
                        newC.Add(c[i]);
                    }
                }
                newC.Add(c[defects[0][0]]);
            }
            return newC;
        }

        public void RunCS(Mat src)
        {
            convex.RunAndMeasure(src, convex);
            dst1 = convex.redC.dst2;
            labels[1] = convex.redC.labels[2];
            dst3 = convex.dst3;

            var rc = task.rc;
            if (rc.mask == null) return;

            dst2 = rc.mask.Resize(dst2.Size(), 0, 0, InterpolationFlags.Nearest);
            contours.RunAndMeasure(dst2, contours);
            var c = contours.bestContour;

            cv.Point[] hull = Cv2.ConvexHull(c, false);
            int[] hullIndices = Cv2.ConvexHullIndices(c, false);
            dst2.SetTo(0);
            DrawContour(dst2, hull.ToList(), vecToScalar(rc.color), -1);

            try
            {
                Vec4i[] defects = Cv2.ConvexityDefects(contours.bestContour, hullIndices.ToList());
                rc.contour = BetterContour(c, defects);
            }
            catch (Exception)
            {
                SetTrueText("Convexity defects failed due to self-intersection.", 3);
            }

            DrawContour(dst2, rc.contour, Scalar.Red);
        }
    }



    public class CS_Corners_Basics : CS_Parent
    {
        public List<Point2f> features = new List<Point2f>();
        public Options_Features options = new Options_Features();
        public Options_Corners optionCorner = new Options_Corners();

        public CS_Corners_Basics(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U);
            desc = "Find interesting points with the FAST (Features from Accelerated Segment Test) algorithm";
        }

        public void RunCS(Mat src)
        {
            optionCorner.RunVB();
            options.RunVB();

            dst2 = src.Clone();
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            KeyPoint[] kpoints = Cv2.FAST(src, task.FASTthreshold, optionCorner.useNonMax);

            features.Clear();
            foreach (KeyPoint kp in kpoints)
            {
                features.Add(new cv.Point2f(kp.Pt.X, kp.Pt.Y));
            }

            if (standaloneTest())
            {
                dst3.SetTo(new Scalar(0));
                foreach (KeyPoint kp in kpoints)
                {
                    DrawCircle(dst2, kp.Pt, task.DotSize, Scalar.Yellow, -1);
                    dst3.Set((int)kp.Pt.Y, (int)kp.Pt.X, (byte)255);
                }
            }
            labels[2] = $"There were {features.Count} key points detected using FAST";
        }
    }

    public class CS_Corners_Harris : CS_Parent
    {
        public Options_HarrisCorners options = new Options_HarrisCorners();
        public Mat gray, mc;
        public mmData mm;

        public CS_Corners_Harris(VBtask task) : base(task)
        {
            desc = "Find corners using Eigen values and vectors";
            labels[3] = "Corner Eigen values";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            mc = new Mat(gray.Size(), MatType.CV_32FC1, new Scalar(0));
            dst2 = new Mat(gray.Size(), MatType.CV_8U, new Scalar(0));
            Cv2.CornerEigenValsAndVecs(gray, dst2, options.blockSize, options.aperture, BorderTypes.Default);

            for (int y = 0; y < gray.Rows - 1; y++)
            {
                for (int x = 0; x < gray.Cols - 1; x++)
                {
                    float lambda_1 = dst2.Get<Vec6f>(y, x)[0];
                    float lambda_2 = dst2.Get<Vec6f>(y, x)[1];
                    mc.Set(y, x, (float)(lambda_1 * lambda_2 - 0.04f * Math.Pow(lambda_1 + lambda_2, 2)));
                }
            }

            mm = GetMinMax(mc);

            src.CopyTo(dst2);
            int count = 0;
            for (int y = 0; y < gray.Rows - 1; y++)
            {
                for (int x = 0; x < gray.Cols - 1; x++)
                {
                    if (mc.Get<float>(y, x) > mm.minVal + (mm.maxVal - mm.minVal) * options.quality / options.qualityMax)
                    {
                        Cv2.Circle(dst2, new cv.Point(x, y), task.DotSize, task.HighlightColor, -1);
                        count += 1;
                    }
                }
            }

            labels[2] = $"Corners_Harris found {count} corners in the image."; 
            Mat McNormal = new Mat();
            Cv2.Normalize(mc, McNormal, 127, 255, NormTypes.MinMax);
            McNormal.ConvertTo(dst3, MatType.CV_8U);
        }
    }

    public class CS_Corners_PreCornerDetect : CS_Parent
    {
        public Math_Median_CDF median = new Math_Median_CDF();
        public Options_PreCorners options = new Options_PreCorners();

        public CS_Corners_PreCornerDetect(VBtask task) : base(task)
        {
            desc = "Use PreCornerDetect to find features in the image.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Mat gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat prob = new Mat();
            Cv2.PreCornerDetect(gray, prob, options.kernelSize);

            Cv2.Normalize(prob, prob, 0, 255, NormTypes.MinMax);
            prob.ConvertTo(gray, MatType.CV_8U);
            median.Run(gray.Clone());
            dst2 = gray.CvtColor(ColorConversionCodes.GRAY2BGR);
            dst3 = gray.Threshold(160, 255, ThresholdTypes.BinaryInv).CvtColor(ColorConversionCodes.GRAY2BGR);
            labels[3] = $"median = {median.medianVal}";
        }
    }

    public class CS_Corners_ShiTomasi_CPP : CS_Parent
    {
        public Options_ShiTomasi options = new Options_ShiTomasi();

        public CS_Corners_ShiTomasi_CPP(VBtask task) : base(task)
    {
            desc = "Find corners using Eigen values and vectors";
            labels[3] = "Corner Eigen values using ShiTomasi which is also what is used in GoodFeatures.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            byte[] data = new byte[src.Total() * src.Channels()];
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            Marshal.Copy(src.Data, data, 0, data.Length);
            IntPtr imagePtr = Corners_ShiTomasi(handle.AddrOfPinnedObject(), src.Rows, src.Cols, options.blocksize, options.aperture);
            handle.Free();

            dst2 = new Mat(src.Rows, src.Cols, MatType.CV_32F, imagePtr).Clone();

            dst3 = GetNormalize32f(dst2);
            dst3 = dst3.Threshold(options.threshold, 255, ThresholdTypes.Binary);
        }

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Corners_ShiTomasi(IntPtr grayPtr, int rows, int cols, int blocksize, int aperture);
    }

    public class CS_Corners_BasicsCentroid : CS_Parent
    {
        public Corners_Basics fast = new Corners_Basics();
        public Kalman_Basics kalman = new Kalman_Basics();

        public CS_Corners_BasicsCentroid(VBtask task) : base(task)
    {
            kalman.kInput = new float[2];
            desc = "Find interesting points with the FAST and smooth the centroid with kalman";
        }

        public void RunCS(Mat src)
        {
            fast.Run(src);
            dst2 = fast.dst2;
            foreach (Point2f pt in fast.features)
            {
                DrawCircle(dst3, pt, task.DotSize + 2, Scalar.White, -1);
            }
            Mat gray = dst3.CvtColor(ColorConversionCodes.BGR2GRAY);
            Moments m = Cv2.Moments(gray, true);
            if (m.M00 > 5000)
            {
                kalman.kInput[0] = (float)(m.M10 / m.M00);
                kalman.kInput[1] = (float)(m.M01 / m.M00);
                kalman.Run(src);
                Cv2.Circle(dst3, new cv.Point((int)kalman.kOutput[0], (int)kalman.kOutput[1]), 10, Scalar.Red, -1);
            }
        }
    }

    public class CS_Corners_BasicsStablePoints : CS_Parent
    {
        public List<cv.Point> features = new List<cv.Point>();
        public Corners_Basics fast = new Corners_Basics();

        public CS_Corners_BasicsStablePoints(VBtask task) : base(task)
    {
            labels = new string[] { "", "", "", "FAST stable points without context" };
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, new Scalar(0));
            desc = "Find and save only the stable points in the FAST output.";
        }

        public void RunCS(Mat src)
        {
            fast.Run(src);

            if (task.motionFlag || task.optionsChanged)
            {
                foreach (Point2f pt in fast.features)
                {
                    features.Add(new cv.Point((int)pt.X, (int)pt.Y));
                }
            }
            List<cv.Point> newPts = new List<cv.Point>();
            dst2 = src.Clone();
            dst3.SetTo(new Scalar(0));
            foreach (Point2f pt in fast.features)
            {
                cv.Point test = new cv.Point((int)pt.X, (int)pt.Y);
                if (features.Contains(test))
                {
                    Cv2.Circle(dst2, test, task.DotSize, Scalar.Yellow, -1);
                    newPts.Add(test);
                    dst3.Set(test.Y, test.X, (byte)255);
                }
            }

            features = newPts;
            labels[2] = $"{features.Count.ToString("000")} identified FAST stable points - slider adjusts threshold";
        }
    }

    public class CS_Corners_BasicsCentroids : CS_Parent
    {
        public Corners_Basics fast = new Corners_Basics();
        public Point2f[] fastCenters;

        public CS_Corners_BasicsCentroids(VBtask task) : base(task)
    {
            if (standaloneTest()) task.gOptions.setGridSize(16);
            desc = "Use a thread grid to find the centroids in each grid element";
        }

        public void RunCS(Mat src)
        {
            dst2 = src.Clone();

            fast.Run(src);
            fastCenters = new Point2f[task.gridList.Count];
            for (int i = 0; i < task.gridList.Count; i++)
            {
                Rect roi = task.gridList[i];
                Mat tmp = fast.dst3[roi];
                var nonZero = tmp.FindNonZero();
                if (nonZero.Rows > 0)
                {
                    Scalar mean = Cv2.Mean(tmp, null);
                    fastCenters[i] = new Point2f((float)(roi.X + mean.Val0), (float)(roi.Y + mean.Val1));
                }
            }

            foreach (Point2f center in fastCenters)
            {
                DrawCircle(dst2, center, task.DotSize, Scalar.Yellow);
            }
            dst2.SetTo(new Scalar(255), task.gridMask);
        }
    }

    public class CS_Corners_Harris_CPP : CS_Parent
    {
        public AddWeighted_Basics addw = new AddWeighted_Basics();
        public Options_Harris options = new Options_Harris();

        public CS_Corners_Harris_CPP(VBtask task) : base(task)
    {
            cPtr = Harris_Features_Open();
            desc = "Use Harris feature detectors to identify interesting points.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            byte[] dataSrc = new byte[src.Total() * src.Channels()];
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
            GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            IntPtr imagePtr = Harris_Features_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.threshold,
                                                  (short) options.neighborhood, (short) options.aperture, options.harrisParm);
            handleSrc.Free();

            Mat gray32f = new Mat(src.Rows, src.Cols, MatType.CV_32F, imagePtr);
            // gray32f = GetNormalize32f(gray32f);
            gray32f.ConvertTo(dst2, MatType.CV_8U);
            addw.src2 = dst2.CvtColor(ColorConversionCodes.GRAY2BGR);
            addw.Run(task.color);
            dst3 = addw.dst2;
            labels[3] = "RGB overlaid with Harris result.";
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = Harris_Features_Close(cPtr);
        }

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Features_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Features_Close(IntPtr Harris_FeaturesPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Features_Run(IntPtr Harris_FeaturesPtr, IntPtr inputPtr, int rows, int cols, float threshold, short neighborhood, short aperture, float HarrisParm);
    }

    public class CS_Corners_HarrisDetector : CS_Parent
    {
        public List<Point2f> features = new List<Point2f>();
        public Options_Features options = new Options_Features();

        public CS_Corners_HarrisDetector(VBtask task) : base(task)
    {
            cPtr = Harris_Detector_Open();
            desc = "Use Harris detector to identify interesting points.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = src.Clone();

            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            byte[] dataSrc = new byte[src.Total() * src.Channels()];
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
            GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            IntPtr imagePtr = Harris_Detector_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.quality);
            handleSrc.Free();
            int ptCount = Harris_Detector_Count(cPtr);
            if (ptCount > 1)
            {
                Mat ptMat = new Mat(ptCount, 2, MatType.CV_32S, imagePtr).Clone();
                features.Clear();
                for (int i = 0; i < ptCount; i++)
                {
                    features.Add(new Point2f(ptMat.Get<int>(i, 0), ptMat.Get<int>(i, 1)));
                    DrawCircle(dst2, features[i], task.DotSize, Scalar.Yellow, -1);
                }
            }
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = Harris_Detector_Close(cPtr);
        }

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Harris_Detector_Count(IntPtr cPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Detector_Open();

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Detector_Close(IntPtr Harris_FeaturesPtr);

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Harris_Detector_Run(IntPtr Harris_FeaturesPtr, IntPtr inputPtr, int rows, int cols, double qualityLevel);
    }

    public class CS_Corners_RedCloud : CS_Parent
    {
        public RedCloud_Basics redC = new RedCloud_Basics();
        public Neighbors_Intersects corners = new Neighbors_Intersects();

        public CS_Corners_RedCloud(VBtask task) : base(task)
    {
            labels = new string[] { "", "", "Grayscale", "Highlighted points show where more than 2 cells intersect." };
            desc = "Find the corners for each RedCloud cell.";
        }

        public void RunCS(Mat src)
        {
            redC.Run(src);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];

            corners.Run(task.cellMap);

            dst3 = new Mat();
            src.CopyTo(dst3);
            foreach (Point2f pt in corners.nPoints)
            {
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor, -1);
                DrawCircle(dst3, pt, task.DotSize, Scalar.Yellow, -1);
            }
        }
    }

    public class CS_Corners_SubPix : CS_Parent
    {
        public Feature_Basics feat = new Feature_Basics();
        public Options_PreCorners options = new Options_PreCorners();

        public CS_Corners_SubPix(VBtask task) : base(task)
        {
            labels[2] = "Output of PreCornerDetect";
            desc = "Use PreCornerDetect to refine the feature points to sub-pixel accuracy.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = src.Clone();
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            feat.Run(src);
            cv.TermCriteria term = new cv.TermCriteria((cv.CriteriaTypes)((int)cv.CriteriaTypes.Eps + (int)cv.CriteriaTypes.Count), 10, 1.0);
            Cv2.CornerSubPix(src, task.features, new cv.Size(options.subpixSize, options.subpixSize), new cv.Size(-1, -1), term);

            List<cv.Point> featurePoints = new List<cv.Point>();
            for (int i = 0; i < task.features.Count; i++)
            {
                Point2f pt = task.features[i];
                featurePoints.Add(new cv.Point((int)pt.X, (int)pt.Y));
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor, -1);
            }
        }
    }


    public class CS_Correlation_Basics : CS_Parent
    {
        KMeans_Edges kFlood = new KMeans_Edges();
        Options_FeatureMatch options = new Options_FeatureMatch();

        public CS_Correlation_Basics(VBtask task) : base(task)
        {
            labels[3] = "Plot of z (vertical scale) to x with ranges shown on the plot.";
            UpdateAdvice(traceName + ": there are several local options panels.");
            desc = "Compute a correlation for src rows (See also: Match.cs";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            kFlood.Run(src);
            dst1 = kFlood.dst2;
            dst2 = kFlood.dst3;

            int row = task.mouseMovePoint.Y;
            if (row == 0)
                SetTrueText("Move mouse across image to see the relationship between X and Z\n" +
                            "A linear relationship is a useful correlation", new cv.Point(0, 10), 3);

            Mat dataX = new Mat(src.Size(), MatType.CV_32F, Scalar.All(0));
            Mat dataY = new Mat(src.Size(), MatType.CV_32F, Scalar.All(0));
            Mat dataZ = new Mat(src.Size(), MatType.CV_32F, Scalar.All(0));

            Mat mask = kFlood.dst3.CvtColor(ColorConversionCodes.BGR2GRAY);
            task.pcSplit[0].CopyTo(dataX, mask);
            task.pcSplit[1].CopyTo(dataY, mask);
            task.pcSplit[2].CopyTo(dataZ, mask);

            Mat row1 = dataX.Row(row);
            Mat row2 = dataZ.Row(row);
            Cv2.Line(dst2, new cv.Point(0, row), new cv.Point(dst2.Width, row), Scalar.Yellow, task.lineWidth + 1);

            Mat correlationMat = new Mat();
            Cv2.MatchTemplate(row1, row2, correlationMat, options.matchOption);
            float correlation = correlationMat.Get<float>(0, 0);
            labels[2] = $"Correlation of X to Z = {correlation:F2}";

            dst3.SetTo(Scalar.All(0));
            List<float> plotX = new List<float>();
            List<float> plotZ = new List<float>();
            for (int i = 0; i < row1.Cols; i++)
            {
                float x = row1.Get<float>(0, i);
                float z = row2.Get<float>(0, i);
                if (x != 0 && z != 0)
                {
                    plotX.Add(x);
                    plotZ.Add(z);
                }
            }

            if (plotX.Count > 0)
            {
                float minx = plotX.Min(), maxx = plotX.Max();
                float minZ = plotZ.Min(), maxZ = plotZ.Max();
                for (int i = 0; i < plotX.Count; i++)
                {
                    float x = dst3.Width * (plotX[i] - minx) / (maxx - minx);
                    float y = dst3.Height * (plotZ[i] - minZ) / (maxZ - minZ);
                    Cv2.Circle(dst3, new cv.Point(x, y), task.DotSize, Scalar.Yellow, -1);
                }
                SetTrueText($"Z-min {minZ:F2}", new cv.Point(10, 5), 3);
                SetTrueText($"Z-max {maxZ:F2}\n\tX-min {minx:F2}", new cv.Point(0, dst3.Height - 20), 3);
                SetTrueText($"X-max {maxx:F2}", new cv.Point(dst3.Width - 40, dst3.Height - 10), 3);
            }
        }
    }


    public class CS_Covariance_Basics : CS_Parent
    {
        CS_Random_Basics random;
        public Mat mean = new Mat();
        public Mat covariance = new Mat();
        cv.Point2f lastCenter;
        public CS_Covariance_Basics(VBtask task) : base(task)
        {
            random = new CS_Random_Basics(task);
            UpdateAdvice(traceName + ": use the local options to control the number of points.");
            desc = "Calculate the covariance of random depth data points.";
        }

        public void RunCS(Mat src)
        {
            dst3.SetTo(0);
            if (standaloneTest())
            {
                random.RunAndMeasure(empty, random);
                src = new Mat(random.pointList.Count, 2, MatType.CV_32F, random.pointList.ToArray());
                for (int i = 0; i < random.pointList.Count; i++)
                {
                    DrawCircle(dst3, random.pointList[i], 3, Scalar.White);
                }
            }

            Mat samples2 = src.Reshape(2);
            Cv2.CalcCovarMatrix(src, covariance, mean, CovarFlags.Cols);

            strOut = "The Covariance Mat:\n";
            for (int j = 0; j < covariance.Rows; j++)
            {
                for (int i = 0; i < covariance.Cols; i++)
                {
                    strOut += string.Format(fmt3, covariance.Get<double>(j, i)) + ", ";
                }
                strOut += "\n";
            }
            strOut += "\n";

            Scalar overallMean = Cv2.Mean(samples2);
            cv.Point2f center = new Point2f((float)overallMean[0], (float)overallMean[1]);
            strOut += $"Mean (img1, img2) = ({center.X.ToString(fmt0)}, {center.Y.ToString(fmt0)})\n";

            if (standaloneTest())
            {
                if (task.FirstPass) lastCenter = center;
                DrawCircle(dst3, center, 5, Scalar.Red);
                DrawCircle(dst3, lastCenter, 5, Scalar.Yellow, task.lineWidth + 1);
                DrawLine(dst3, center, lastCenter, Scalar.Red, task.lineWidth + 1);
                lastCenter = center;
                strOut += "Yellow is last center, red is the current center";
            }
            SetTrueText(strOut);
        }
    }

    public class CS_Covariance_Test : CS_Parent
    {
        Covariance_Basics covar = new Covariance_Basics();

        public CS_Covariance_Test(VBtask task) : base(task)
        {
            desc = "Test the covariance basics algorithm.";
        }

        public void RunCS(Mat src)
        {
            double[] testInput = { 1.5, 2.3, 3.0, 1.7, 1.2, 2.9, 2.1, 2.2, 3.1, 3.1, 1.3, 2.7, 2.0, 1.7, 1.0, 2.0, 0.5, 0.6, 1.0, 0.9 };
            Mat samples = new Mat(10, 2, MatType.CV_64F, testInput);
            covar.Run(samples);
            SetTrueText(covar.strOut, new cv.Point(20, 60));
            SetTrueText("Results should be a symmetric array with 2.1 and -2.1", new cv.Point(20, 150));
        }
    }

    public class CS_Covariance_Images : CS_Parent
    {
        Covariance_Basics covar = new Covariance_Basics();
        public Mat mean;
        public Mat covariance;
        Mat last32f = new Mat();

        public CS_Covariance_Images(VBtask task) : base(task)
        {
            desc = "Calculate the covariance of 2 images";
        }

        public void RunCS(Mat src)
        {
            Mat gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (task.optionsChanged) gray.ConvertTo(last32f, MatType.CV_32F);
            dst2 = gray;

            Mat gray32f = new Mat();
            gray.ConvertTo(gray32f, MatType.CV_32F);
            Cv2.Merge(new[] { gray32f, last32f }, dst0);
            Mat samples = dst0.Reshape(1, dst0.Rows * dst0.Cols);
            covar.Run(samples);

            last32f = gray32f;

            SetTrueText(covar.strOut, new cv.Point(10, 10), 3);

            mean = covar.mean;
            covariance = covar.covariance;
        }
    }


    public class CS_Crypto_Hash : CS_Parent
    {
        Font_FlowText flow = new Font_FlowText();
        List<Mat> images = new List<Mat>();
        List<string> guids = new List<string>();

        public CS_Crypto_Hash(VBtask task) : base(task)
        {
            desc = "Experiment with hashing algorithm and guid";
        }

        public void RunCS(Mat src)
        {
            int iSize = (int)(src.Total() * src.ElemSize());
            int maxImages = 10;
            images.Add(src);

            if (images.Count >= maxImages)
            {
                byte[] bytes = new byte[iSize * maxImages];
                images.RemoveAt(0);

                int index = 0;
                foreach (Mat mat in images)
                {
                    Marshal.Copy(mat.Data, bytes, iSize * index, iSize);
                    index++;
                }

                using (MD5 algorithm = MD5.Create())
                {
                    bytes = algorithm.ComputeHash(bytes);
                }

                guids.Add(new Guid(bytes).ToString());
                flow.msgs.Clear();

                for (int i = 0; i < guids.Count; i++)
                {
                    flow.msgs.Add(guids[i]);
                }

                if (guids.Count >= 25)
                {
                    guids.RemoveAt(0);
                }

                flow.Run(empty);
            }
        }
    }



    public class CS_CSV_Basics : CS_Parent
    {
        public string InputFile { get; set; }
        public string[,] Array { get; set; }
        public List<List<string>> ArrayList { get; set; }

        public CS_CSV_Basics(VBtask task) : base(task)
        {
            var fileInput = new FileInfo(Path.Combine(task.HomeDir, "Data/agaricus-lepiota.data"));
            InputFile = fileInput.FullName;
            desc = "Read and prepare a .csv file";
            ArrayList = new List<List<string>>();
        }

        public void RunCS(Mat src)
        {
            string[] readText = File.ReadAllLines(InputFile);
            string[] variables = readText[0].Split(',');
            Array = new string[readText.Length, variables.Length];

            for (int i = 0; i < Array.GetLength(0); i++)
            {
                variables = readText[i].Split(',');
                for (int j = 0; j < Array.GetLength(1); j++)
                {
                    Array[i, j] = variables[j];
                }
            }

            for (int i = 0; i < Array.GetLength(1); i++)
            {
                ArrayList.Add(new List<string>());
                for (int j = 0; j < Array.GetLength(0); j++)
                {
                    ArrayList[i].Add(Array[j, i]);
                }
            }

            if (standaloneTest())
            {
                SetTrueText($"{InputFile} is now loaded into the csv.array");
            }
        }
    }




    public class CS_DCT_Basics : CS_Parent
    {
        public Options_DCT options = new Options_DCT();

        public CS_DCT_Basics(VBtask task) : base(task)
        {
            labels[3] = "Difference from original";
            UpdateAdvice(traceName + ": local options control the Discrete Cosine Transform'");
            desc = "Apply OpenCV's Discrete Cosine Transform to a grayscale image and use slider to remove the highest frequencies.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (src.Channels() == 3)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            Mat src32f = new Mat();
            src.ConvertTo(src32f, MatType.CV_32F, 1.0 / 255);

            Mat frequencies = new Mat();
            Cv2.Dct(src32f, frequencies, (cv.DctFlags) options.removeFrequency);

            Rect roi = new Rect(0, 0, options.removeFrequency, src32f.Height);
            if (roi.Width > 0)
                frequencies[roi].SetTo(0);
            labels[2] = "Frequencies below " + options.removeFrequency.ToString() + " removed";

            Cv2.Dct(frequencies, src32f, DctFlags.Inverse);
            src32f.ConvertTo(dst2, MatType.CV_8UC1, 255);

            Cv2.Subtract(src, dst2, dst3);
        }
    }

    public class CS_DCT_RGB : CS_Parent
    {
        public DCT_Basics dct = new DCT_Basics();

        public CS_DCT_RGB(VBtask task) : base(task)
        {
            labels[3] = "Difference from original";
            desc = "Apply OpenCV's Discrete Cosine Transform to a BGR image and use slider to remove the highest frequencies.";
        }

        public void RunCS(Mat src)
        {
            dct.options.RunVB();

            Mat[] srcPlanes = Cv2.Split(src);

            Mat[] freqPlanes = new Mat[3];
            for (int i = 0; i < srcPlanes.Length; i++)
            {
                Mat src32f = new Mat();
                srcPlanes[i].ConvertTo(src32f, MatType.CV_32FC3, 1.0 / 255);
                freqPlanes[i] = new Mat();
                Cv2.Dct(src32f, freqPlanes[i], DctFlags.None);

                Rect roi = new Rect(0, 0, dct.options.removeFrequency, src32f.Height);
                if (roi.Width > 0)
                    freqPlanes[i][roi].SetTo(0);

                Cv2.Dct(freqPlanes[i], src32f, dct.options.dctFlag);
                src32f.ConvertTo(srcPlanes[i], MatType.CV_8UC1, 255);
            }
            labels[2] = dct.labels[2];

            Cv2.Merge(srcPlanes, dst2);

            Cv2.Subtract(src, dst2, dst3);
        }
    }

    public class CS_DCT_Depth : CS_Parent
    {
        DCT_Basics dct = new DCT_Basics();

        public CS_DCT_Depth(VBtask task) : base(task)
        {
            labels[3] = "Subtract DCT inverse from Grayscale depth";
            desc = "Find featureless surfaces in the depth data - expected to be useful only on the K4A for Azure camera.";
        }

        public void RunCS(Mat src)
        {
            Mat gray = task.depthRGB.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat frequencies = new Mat();
            Mat src32f = new Mat();
            gray.ConvertTo(src32f, MatType.CV_32F, 1.0 / 255);
            Cv2.Dct(src32f, frequencies, dct.options.dctFlag);

            Rect roi = new Rect(0, 0, dct.options.removeFrequency, src32f.Height);
            if (roi.Width > 0)
                frequencies[roi].SetTo(0);
            labels[2] = dct.labels[2];

            Cv2.Dct(frequencies, src32f, DctFlags.Inverse);
            src32f.ConvertTo(dst2, MatType.CV_8UC1, 255);

            Cv2.Subtract(gray, dst2, dst3);
        }
    }

    public class CS_DCT_FeatureLess : CS_Parent
    {
        public DCT_Basics dct = new DCT_Basics();

        public CS_DCT_FeatureLess(VBtask task) : base(task)
        {
            desc = "Find surfaces that lack any texture. Remove just the highest frequency from the DCT to get horizontal lines through the image.";
            labels[3] = "FeatureLess BGR regions";
        }

        public void RunCS(Mat src)
        {
            dct.Run(src);

            dst2.SetTo(0);
            for (int i = 0; i < dct.dst2.Rows; i++)
            {
                int runLen = 0;
                int runStart = 0;
                for (int j = 1; j < dct.dst2.Cols; j++)
                {
                    if (dct.dst2.Get<byte>(i, j) == dct.dst2.Get<byte>(i, j - 1))
                    {
                        runLen++;
                    }
                    else
                    {
                        if (runLen > dct.options.runLengthMin)
                        {
                            Rect roi = new Rect(runStart, i, runLen, 1);
                            dst2[roi].SetTo(255);
                        }
                        runStart = j;
                        runLen = 1;
                    }
                }
            }

            dst3.SetTo(0);
            if (dst2.Channels() == 3)
            {
                dst2 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY).Threshold(1, 255, ThresholdTypes.Binary);
            }
            else
            {
                dst2 = dst2.Threshold(1, 255, ThresholdTypes.Binary);
            }
            src.CopyTo(dst3, ~dst2);
            labels[2] = "Mask of DCT with highest frequency removed";
        }
    }

    public class CS_DCT_Surfaces_debug : CS_Parent
    {
        Mat_4to1 mats = new Mat_4to1();
        DCT_FeatureLess dct = new DCT_FeatureLess();
        Font_FlowText flow = new Font_FlowText();
        Plane_CellColor plane = new Plane_CellColor();

        public CS_DCT_Surfaces_debug(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Stats on the largest region below DCT threshold", "Various views of regions with DCT below threshold" };
            task.gOptions.setDisplay0();
            desc = "Find plane equation for a featureless surface - debugging one region for now.";
        }

        public void RunCS(Mat src)
        {
            mats.mat[0] = src.Clone();
            mats.mat[0].SetTo(Scalar.White, task.gridMask);

            dct.Run(src);
            mats.mat[1] = dct.dst2.CvtColor(ColorConversionCodes.GRAY2BGR).Clone();
            mats.mat[2] = dct.dst3.Clone();

            Mat mask = dct.dst2.Clone();
            task.pcSplit[2].SetTo(0, ~mask);

            int maxIndex = 0;
            int[] roiCounts = new int[task.gridList.Count];
            for (int i = 0; i < task.gridList.Count; i++)
            {
                roiCounts[i] = mask[task.gridList[i]].CountNonZero();
                if (roiCounts[i] > roiCounts[maxIndex])
                    maxIndex = i;
            }

            mats.mat[3] = new Mat(src.Size(), MatType.CV_8UC3, 0);
            src[task.gridList[maxIndex]].CopyTo(mats.mat[3][task.gridList[maxIndex]], mask[task.gridList[maxIndex]]);
            mats.Run(new Mat());
            dst3 = mats.dst2;

            Rect roi = task.gridList[maxIndex];
            if (roi.X == task.gridList[maxIndex].X && roi.Y == task.gridList[maxIndex].Y)
            {
                if (roiCounts[maxIndex] > roi.Width * roi.Height / 4)
                {
                    List<Point3f> fitPoints = new List<Point3f>();
                    float minDepth = float.MaxValue, maxDepth = float.MinValue;
                    for (int j = 0; j < roi.Height; j++)
                    {
                        for (int i = 0; i < roi.Width; i++)
                        {
                            float nextD = task.pcSplit[2][roi].Get<float>(j, i);
                            if (nextD != 0)
                            {
                                if (minDepth > nextD) minDepth = nextD;
                                if (maxDepth < nextD) maxDepth = nextD;
                                Point3f wpt = new Point3f(roi.X + i, roi.Y + j, nextD);
                                fitPoints.Add(getWorldCoordinates(wpt));
                            }
                        }
                    }
                    if (fitPoints.Count > 0)
                    {
                        var eq = fitDepthPlane(fitPoints);
                        if (!float.IsNaN(eq[0]))
                        {
                            flow.msgs.Add($"a={eq[0]:F2} b={eq[1]:F2} c={Math.Abs(eq[2]):F2}\t" +
                                          $"depth={-eq[3]:F2}m roi(x,y) = {roi.X:000},{roi.Y:000}\t" +
                                          $"Min={minDepth:F1}m Max={maxDepth:F1}m");
                        }
                    }
                }
            }
            flow.Run(new Mat());
        }
    }


    public class CS_Delaunay_Basics : CS_Parent
    {
        public List<Point2f> inputPoints;
        public List<List<cv.Point>> facetList = new List<List<cv.Point>>();
        public Mat facet32s;
        Random_Enumerable randEnum = new Random_Enumerable();
        Subdiv2D subdiv = new Subdiv2D();

        public CS_Delaunay_Basics(VBtask task) : base(task)
        {
            facet32s = new Mat(dst2.Size(), MatType.CV_32SC1, 0);
            labels[3] = "CV_8U map of Delaunay cells";
            desc = "Subdivide an image based on the points provided.";
        }

        public void RunCS(Mat src)
        {
            if (task.heartBeat && standalone)
            {
                randEnum.Run(null);
                inputPoints = randEnum.points.ToList();
            }

            subdiv.InitDelaunay(new Rect(0, 0, dst2.Width, dst2.Height));
            subdiv.Insert(inputPoints.ToArray());

            cv.Point2f[][] facets = null;
            var facetIndices = new List<int>();
            var facetCenters = new Point2f[1];
            subdiv.GetVoronoiFacetList(facetIndices, out facets, out facetCenters);

            facetList.Clear();
            for (int i = 0; i < facets.GetUpperBound(0); i++)
            {
                var ptList = new List<cv.Point>();
                for (int j = 0; j < facets[i].Length - 1; j++)
                {
                    ptList.Add(new cv.Point(facets[i][j].X, facets[i][j].Y));
                }

                facet32s.FillConvexPoly(ptList.ToArray(), i, task.lineType);
                facetList.Add(ptList);
            }
            facet32s.ConvertTo(dst3, MatType.CV_8U);
            dst2 = ShowPalette(dst3);
            labels[2] = traceName + ": " + inputPoints.Count.ToString("000") + " cells were present.";
        }
    }

    public class CS_Delaunay_SubDiv : CS_Parent
    {
        Random_Basics random = new Random_Basics();

        public CS_Delaunay_SubDiv(VBtask task) : base(task)
        {
            FindSlider("Random Pixel Count").Value = 100;
            desc = "Use Delaunay to subdivide an image into triangles.";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                if (!task.heartBeat) return;
            }

            var subdiv = new Subdiv2D(new Rect(0, 0, dst2.Width, dst2.Height));
            random.Run(null);
            dst2.SetTo(new Scalar(0));

            foreach (var pt in random.pointList)
            {
                subdiv.Insert(pt);
                var edgeList = subdiv.GetEdgeList();
                foreach (var e in edgeList)
                {
                    var p0 = new cv.Point(Math.Round(e[0]), Math.Round(e[1]));
                    var p1 = new cv.Point(Math.Round(e[2]), Math.Round(e[3]));
                    DrawLine(dst2, p0, p1, new Scalar(255), task.lineWidth);
                }
            }

            foreach (var pt in random.pointList)
            {
                DrawCircle(dst2, pt, task.DotSize + 1, new Scalar(255, 0, 0), -1);
            }

            cv.Point2f[][] facets = null;
            var centers = new Point2f[1];
            subdiv.GetVoronoiFacetList(null, out facets, out centers);

            var ifacet = new cv.Point[1];
            var ifacets = new cv.Point[1][];

            for (int i = 0; i < facets.GetUpperBound(0); i++)
            {
                Array.Resize(ref ifacet, facets[i].Length - 1);
                for (int j = 0; j < facets[i].Length - 1; j++)
                {
                    ifacet[j] = new cv.Point(Math.Round(facets[i][j].X), Math.Round(facets[i][j].Y));
                }
                ifacets[0] = ifacet;
                dst3.FillConvexPoly(ifacet, task.scalarColors[i % task.scalarColors.Length], task.lineType);
                Cv2.Polylines(dst3, ifacets, true, new Scalar(0, 0, 0), task.lineWidth, LineTypes.AntiAlias, 0);
            }
        }
    }

    public class CS_Delaunay_Subdiv2D : CS_Parent
    {
        public CS_Delaunay_Subdiv2D(VBtask task) : base(task)
        {
            labels[3] = "Voronoi facets for the same subdiv2D";
            desc = "Generate random points and divide the image around those points.";
        }

        public void RunCS(Mat src)
        {
            if (!task.heartBeat) return;
            dst2.SetTo(new Scalar(0));
            var points = Enumerable.Range(0, 100)
                .Select(i => new Point2f(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height)))
                .ToArray();

            foreach (var p in points)
            {
                DrawCircle(dst2, p, task.DotSize + 1, 255, -1);
            }
            dst3 = dst2.Clone();

            var subdiv = new Subdiv2D(new Rect(0, 0, dst3.Width, dst3.Height));
            subdiv.Insert(points);

            cv.Point2f[][] facets = null;
            var facetCenters = new Point2f[1];
            subdiv.GetVoronoiFacetList(null, out facets, out facetCenters);

            for (int i = 0; i < facets.GetUpperBound(0); i++)
            {
                var before = facets[i][facets[i].Length - 1];
                foreach (var p in facets[i])
                {
                    DrawLine(dst2, before, p, new Scalar(0, 255, 0), 1);
                    before = p;
                }
            }

            var edgelist = subdiv.GetEdgeList();
            foreach (var edge in edgelist)
            {
                var p1 = new Point2f(edge[0], edge[1]);
                var p2 = new Point2f(edge[2], edge[3]);
                DrawLine(dst2, p1, p2, new Scalar(0, 255, 0), 1);
            }
        }
    }

    public class CS_Delaunay_GenerationsNoKNN : CS_Parent
    {
        public List<Point2f> inputPoints;
        public Delaunay_Basics facet = new Delaunay_Basics();
        Random_Basics random = new Random_Basics();

        public CS_Delaunay_GenerationsNoKNN(VBtask task) : base(task)
        {
            FindSlider("Random Pixel Count").Value = 10;
            dst3 = new Mat(dst3.Size(), MatType.CV_32S, 0);
            labels = new string[] { "", "Mask of unmatched regions - generation set to 0", "Facet Image with index of each region", "Generation counts for each region." };
            desc = "Create a region in an image for each cv.Point provided without using KNN.";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest() && task.heartBeat)
            {
                random.Run(null);
                inputPoints = random.pointList.ToList();
            }

            facet.inputPoints = inputPoints;
            facet.Run(src);
            dst2 = facet.dst2;

            var generationMap = dst3.Clone();
            dst3.SetTo(new Scalar(0));
            var usedG = new List<int>();
            var g = 0;
            foreach (var pt in inputPoints)
            {
                var index = facet.facet32s.Get<int>((int)pt.Y, (int)pt.X);
                if (index >= facet.facetList.Count) continue;
                var nextFacet = facet.facetList[index];
                // insure that each facet has a unique generation number
                if (task.FirstPass)
                {
                    g = usedG.Count;
                }
                else
                {
                    g = generationMap.Get<int>((int)pt.Y, (int)pt.X) + 1;
                    while (usedG.Contains(g))
                    {
                        g++;
                    }
                }
                dst3.FillConvexPoly(nextFacet.ToArray(), g, task.lineType);
                usedG.Add(g);
                SetTrueText(g.ToString(), new cv.Point((int)pt.X, (int)pt.Y), 2);
            }
            generationMap = dst3.Clone();
        }
    }

    public class CS_Delaunay_Generations : CS_Parent
    {
        public List<Point2f> inputPoints;
        public Delaunay_Basics facet = new Delaunay_Basics();
        KNN_Basics knn = new KNN_Basics();
        Random_Basics random = new Random_Basics();

        public CS_Delaunay_Generations(VBtask task) : base(task)
        {
            dst0 = new Mat(dst0.Size(), MatType.CV_32S, 0);
            labels = new string[] { "", "Mask of unmatched regions - generation set to 0", "Facet Image with count for each region", "Generation counts in CV_32SC1 format" };
            FindSlider("Random Pixel Count").Value = 10;
            desc = "Create a region in an image for each cv.Point provided";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                if (task.heartBeat)
                {
                    random.Run(null);
                }
                inputPoints = random.pointList.ToList();
            }

            knn.queries = inputPoints;
            knn.Run(null);

            facet.inputPoints = inputPoints;
            facet.Run(src);
            dst2 = facet.dst2;

            var generationMap = dst0.Clone();
            dst0.SetTo(new Scalar(0));
            var usedG = new List<int>();
            var g = 0;
            foreach (var mp in knn.matches)
            {
                var index = facet.facet32s.Get<int>((int)mp.p2.Y, (int)mp.p2.X);
                if (index >= facet.facetList.Count) continue;
                var nextFacet = facet.facetList[index];
                // insure that each facet has a unique generation number
                if (task.FirstPass)
                {
                    g = usedG.Count;
                }
                else
                {
                    g = generationMap.Get<int>((int)mp.p2.Y, (int)mp.p2.X) + 1;
                    while (usedG.Contains(g))
                    {
                        g++;
                    }
                }
                dst0.FillConvexPoly(nextFacet.ToArray(), g, task.lineType);
                usedG.Add(g);
                SetTrueText(g.ToString(), new cv.Point(mp.p2.X, mp.p2.Y), 2);
            }
        }
    }

    public class CS_Delaunay_ConsistentColor : CS_Parent
    {
        public List<Point2f> inputPoints;
        public List<List<cv.Point>> facetList = new List<List<cv.Point>>();
        public Mat facet32s;
        Random_Enumerable randEnum = new Random_Enumerable();
        Subdiv2D subdiv = new Subdiv2D();

        public CS_Delaunay_ConsistentColor(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            facet32s = new Mat(dst2.Size(), MatType.CV_32SC1, 0);
            UpdateAdvice(traceName + ": use local options to control the number of points");
            labels[1] = "Input points to subdiv";
            labels[3] = "Inconsistent colors in dst2 are duplicate randomCellColor output.";
            desc = "Subdivide an image based on the points provided.";
        }

        public void RunCS(Mat src)
        {
            if (task.heartBeat && standalone)
            {
                randEnum.Run(null);
                inputPoints = randEnum.points.ToList();
            }

            subdiv.InitDelaunay(new Rect(0, 0, dst2.Width, dst2.Height));
            subdiv.Insert(inputPoints.ToArray());

            cv.Point2f[][] facets = null;
            var facetCenters = new Point2f[1];
            subdiv.GetVoronoiFacetList(new List<int>(), out facets, out facetCenters);

            var usedColors = new List<Vec3b>(); 
            usedColors.Add(new Vec3b(0, 0, 0));
            facetList.Clear();
            for (int i = 0; i < facets.GetUpperBound(0); i++)
            {
                var nextFacet = new List<cv.Point>();
                for (int j = 0; j < facets[i].Length - 1; j++)
                {
                    nextFacet.Add(new cv.Point(facets[i][j].X, facets[i][j].Y));
                }

                var pt = inputPoints[i];
                var nextColor = dst3.Get<Vec3b>((int)pt.Y, (int)pt.X);
                if (usedColors.Contains(nextColor))
                {
                    nextColor = randomCellColor();
                }
                usedColors.Add(nextColor);

                dst2.FillConvexPoly(nextFacet.ToArray(), vecToScalar(nextColor));
                facet32s.FillConvexPoly(nextFacet.ToArray(), i, task.lineType);
                facetList.Add(nextFacet);
            }
            dst3 = dst2.Clone();
            dst1.SetTo(0);
            foreach (var pt in inputPoints)
            {
                dst1.Circle(new cv.Point((int)pt.X, (int)pt.Y), task.DotSize, task.HighlightColor, -1, task.lineType);
            }
            dst1 = dst3.Clone();
            labels[2] = traceName + ": " + inputPoints.Count.ToString("000") + " cells were present.";
        }
    }

    public class CS_Delaunay_Contours : CS_Parent
    {
        public List<Point2f> inputPoints;
        Random_Enumerable randEnum = new Random_Enumerable();
        Subdiv2D subdiv = new Subdiv2D();

        public CS_Delaunay_Contours(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            labels[3] = "CV_8U map of Delaunay cells";
            desc = "Subdivide an image based on the points provided.";
        }

        public void RunCS(Mat src)
        {
            if (task.heartBeat && standalone)
            {
                randEnum.Run(null);
                inputPoints = randEnum.points.ToList();
            }

            subdiv.InitDelaunay(new Rect(0, 0, dst2.Width, dst2.Height));
            subdiv.Insert(inputPoints.ToArray());

            cv.Point2f[][] facets = null;
            cv.Point2f[] facetCenters = null;
            subdiv.GetVoronoiFacetList(new List<int>(), out facets, out facetCenters);
            dst2.SetTo(0);
            for (int i = 0; i < facets.GetUpperBound(0); i++)
            {
                var ptList = new List<cv.Point>();
                for (int j = 0; j < facets[i].Length; j++)
                {
                    ptList.Add(new cv.Point(facets[i][j].X, facets[i][j].Y));
                }

                DrawContour(dst2, ptList, 255, 1);
            }
            labels[2] = traceName + ": " + inputPoints.Count.ToString("000") + " cells were present.";
        }
    }


    public class CS_Denoise_Basics_CPP : CS_Parent
    {
        Diff_Basics diff = new Diff_Basics();

        public CS_Denoise_Basics_CPP(VBtask task) : base(task)
        {
            cPtr = Denoise_Basics_Open(3);
            labels = new string[] { "", "", "Input image", "Output: Use PixelViewer to see changes" };
            desc = "Denoise example.";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY) - 1;

            byte[] dataSrc = new byte[src.Total()];
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
            GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            IntPtr imagePtr = Denoise_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols);
            handleSrc.Free();

            if (imagePtr != IntPtr.Zero)
            {
                dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr).Clone();
                diff.Run(dst2);
                dst3 = diff.dst2;
            }
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero)
                cPtr = Denoise_Basics_Close(cPtr);
        }
    }

    public class CS_Denoise_Pixels : CS_Parent
    {
        public int classCount;
        Options_Denoise options = new Options_Denoise();
        Reduction_Basics reduction = new Reduction_Basics();

        public CS_Denoise_Pixels(VBtask task) : base(task)
        {
            cPtr = Denoise_Pixels_Open();
            labels = new string[] { "", "", "Before removing single pixels", "After removing single pixels" };
            desc = "Remove single pixels between identical pixels";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (standaloneTest())
            {
                reduction.Run(src);
                src = reduction.dst2;
                classCount = reduction.classCount;
            }

            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            if (options.removeSinglePixels)
            {
                byte[] cppData = new byte[src.Total()];
                Marshal.Copy(src.Data, cppData, 0, cppData.Length);
                GCHandle handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned);
                IntPtr imagePtr = Denoise_Pixels_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols);
                handleSrc.Free();
                dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr).Clone();
            }
            else
            {
                dst2 = src;
            }

            if (standaloneTest())
            {
                dst2 *= 255.0 / classCount;
                dst3 = ShowPalette(dst2);
            }

            if (task.heartBeat)
            {
                strOut = $"{classCount} pixel classes\n";
                strOut += $"{Denoise_Pixels_EdgeCountBefore(cPtr)} edges before\n";
                strOut += $"{Denoise_Pixels_EdgeCountAfter(cPtr)} edges after";
            }

            SetTrueText(strOut, 2);
        }

        public void Close()
        {
            Denoise_Pixels_Close(cPtr);
        }
    }



    public class CS_Depth_Basics : CS_Parent
    {
        Depth_Colorizer_CPP colorizer = new Depth_Colorizer_CPP();

        public CS_Depth_Basics(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": use global option to control 'Max Depth'.");
            desc = "Colorize the depth data into task.depthRGB";
        }

        public void RunCS(Mat src)
        {
            dst2 = task.pcSplit[2];

            task.pcSplit[2] = task.pcSplit[2].Threshold(task.MaxZmeters, task.MaxZmeters, ThresholdTypes.Trunc);
            if (task.FirstPass)
            {
                task.maxDepthMask = task.pcSplit[2].ConvertScaleAbs().InRange(task.MaxZmeters, task.MaxZmeters);
                task.maxDepthMask.SetTo(0);
            }
            if (standalone) dst3 = task.maxDepthMask;
            SetTrueText(task.gMat.strOut, 3);

            colorizer.Run(task.pcSplit[2]);
            task.depthRGB = colorizer.dst2;
        }
    }

    public class CS_Depth_Display : CS_Parent
    {
        public CS_Depth_Display(VBtask task) : base(task)
        {
            task.gOptions.setDisplay1();
            task.gOptions.setDisplay1();
            labels = new string[] { "task.pcSplit[2]", "task.pointcloud", "task.depthMask", "task.noDepthMask" };
            desc = "Display the task.pcSplit[2], task.pointcloud, task.depthMask, and task.noDepthMask";
        }

        public void RunCS(Mat src)
        {
            dst0 = task.pcSplit[2];
            dst1 = task.pointCloud;
            dst2 = task.depthMask;
            dst3 = task.noDepthMask;
        }
    }


    public class CS_Depth_FirstLastDistance : CS_Parent
    {
        public CS_Depth_FirstLastDistance(VBtask task) : base(task)
        {
            desc = "Monitor the first and last depth distances";
        }

        void identifyMinMax(cv.Point pt, string text)
        {
            Cv2.Circle(dst2, pt, task.DotSize, task.HighlightColor);
            SetTrueText(text, pt, 2);

            Cv2.Circle(dst3, pt, task.DotSize, task.HighlightColor);
            SetTrueText(text, pt, 3);
        }

        public void RunCS(Mat src)
        {
            var mm = GetMinMax(task.pcSplit[2], task.depthMask);
            task.depthRGB.CopyTo(dst2);

            if (task.heartBeat) dst3.SetTo(0);
            labels[2] = $"Min Depth {mm.minVal:F1}m";
            identifyMinMax(mm.minLoc, labels[2]);

            labels[3] = $"Max Depth {mm.maxVal:F1}m";
            identifyMinMax(mm.maxLoc, labels[3]);
        }
    }

    public class CS_Depth_HolesRect : CS_Parent
    {
        Depth_Holes shadow = new Depth_Holes();

        public CS_Depth_HolesRect(VBtask task) : base(task)
        {
            labels[2] = "The 10 largest contours in the depth holes.";
            desc = "Identify the minimum rectangles of contours of the depth shadow";
        }

        public void RunCS(Mat src)
        {
            shadow.Run(src);

            cv.Point[][] contours;
            if (shadow.dst3.Channels() == 3)
                shadow.dst3 = shadow.dst3.CvtColor(ColorConversionCodes.BGR2GRAY);
            Cv2.FindContours(shadow.dst3, out contours, out _, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            var sortContours = new SortedList<int, List<cv.Point>>(new compareAllowIdenticalIntegerInverted());
            foreach (var c in contours)
            {
                sortContours.Add(c.Length, c.ToList());
            }
            dst3.SetTo(0);
            for (int i = 0; i < Math.Min(sortContours.Count, 10); i++)
            {
                var contour = sortContours.ElementAt(i).Value;
                var minRect = Cv2.MinAreaRect(contour);
                var nextColor = new Scalar(task.vecColors[i % 256][0], task.vecColors[i % 256][1], task.vecColors[i % 256][2]);
                DrawRotatedRectangle(minRect, dst2, nextColor);
                Cv2.DrawContours(dst3, new[] { contour }, 0, Scalar.White, task.lineWidth);
            }
            Cv2.AddWeighted(dst2, 0.5, task.depthRGB, 0.5, 0, dst2);
        }
    }

    public class CS_Depth_MeanStdev_MT : CS_Parent
    {
        Mat meanSeries;
        float maxMeanVal, maxStdevVal;

        public CS_Depth_MeanStdev_MT(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Rows, dst2.Cols, MatType.CV_8U, 0);
            dst3 = new Mat(dst3.Rows, dst3.Cols, MatType.CV_8U, 0);
            desc = "Collect a time series of depth mean and stdev to highlight where depth is unstable.";
        }

        public void RunCS(Mat src)
        {
            if (task.optionsChanged)
                meanSeries = new Mat(task.gridList.Count, task.frameHistoryCount, MatType.CV_32F, 0);

            int index = task.frameCount % task.frameHistoryCount;
            float[] meanValues = new float[task.gridList.Count];
            float[] stdValues = new float[task.gridList.Count];

            Parallel.For(0, task.gridList.Count, i =>
            {
                var roi = task.gridList[i];
                Cv2.MeanStdDev(task.pcSplit[2][roi], out Scalar mean, out Scalar stdev, task.depthMask[roi]);
                meanSeries.Set(i, index, (float)mean.Val0);
                if (task.frameCount >= task.frameHistoryCount - 1)
                {
                    Cv2.MeanStdDev(meanSeries.Row(i), out mean, out stdev);
                    meanValues[i] = (float)mean.Val0;
                    stdValues[i] = (float)stdev.Val0;
                }
            });

            if (task.frameCount >= task.frameHistoryCount)
            {
                var means = new Mat(task.gridList.Count, 1, MatType.CV_32F, meanValues);
                var stdevs = new Mat(task.gridList.Count, 1, MatType.CV_32F, stdValues);
                var meanmask = means.Threshold(1, task.MaxZmeters, ThresholdTypes.Binary).ConvertScaleAbs();
                var mm = GetMinMax(means, meanmask);
                var stdMask = stdevs.Threshold(0.001, task.MaxZmeters, ThresholdTypes.Binary).ConvertScaleAbs();
                var mmStd = GetMinMax(stdevs, stdMask);

                maxMeanVal = Math.Max(maxMeanVal, (float)mm.maxVal);
                maxStdevVal = Math.Max(maxStdevVal, (float)mmStd.maxVal);

                Parallel.For(0, task.gridList.Count, i =>
                {
                    var roi = task.gridList[i];
                    dst3[roi].SetTo(255 * stdevs.Get<float>(i, 0) / maxStdevVal);
                    dst3[roi].SetTo(0, task.noDepthMask[roi]);

                    dst2[roi].SetTo(255 * means.Get<float>(i, 0) / maxMeanVal);
                    dst2[roi].SetTo(0, task.noDepthMask[roi]);
                });

                if (task.heartBeat)
                {
                    maxMeanVal = 0;
                    maxStdevVal = 0;
                }

                if (standaloneTest())
                {
                    for (int i = 0; i < task.gridList.Count; i++)
                    {
                        var roi = task.gridList[i];
                        SetTrueText($"{meanValues[i]:F3}\n{stdValues[i]:F3}", new cv.Point(roi.X, roi.Y), 3);
                    }
                }

                dst3 = dst3 | task.gridMask;
                labels[2] = $"The regions where the depth is volatile are brighter.  Stdev min {mmStd.minVal:F3} Stdev Max {mmStd.maxVal:F3}";
                labels[3] = $"Mean/stdev for each ROI: Min {mm.minVal:F3} Max {mm.maxVal:F3}";
            }
        }
    }



    public class CS_Depth_MeanStdevPlot : CS_Parent
    {
        Plot_OverTimeSingle plot1 = new Plot_OverTimeSingle();
        Plot_OverTimeSingle plot2 = new Plot_OverTimeSingle();

        public CS_Depth_MeanStdevPlot(VBtask task) : base(task)
        {
            desc = "Plot the mean and stdev of the depth image";
        }

        public void RunCS(Mat src)
        {
            Scalar mean, stdev;
            Mat depthMask = task.depthMask;
            Cv2.MeanStdDev(task.pcSplit[2], out mean, out stdev, depthMask);

            plot1.plotData = (float)mean[0];
            plot1.Run(src);
            dst2 = plot1.dst2;

            plot2.plotData = (float)stdev[0];
            plot2.Run(src);
            dst3 = plot2.dst2;

            labels[2] = $"Plot of mean depth = {mean[0].ToString(fmt1)} min = {plot1.min.ToString(fmt2)} max = {plot1.max.ToString(fmt2)}";
            labels[3] = $"Plot of depth stdev = {stdev[0].ToString(fmt1)} min = {plot2.min.ToString(fmt2)} max = {plot2.max.ToString(fmt2)}";
        }
    }

    public class CS_Depth_Uncertainty : CS_Parent
    {
        Retina_Basics_CPP retina = new Retina_Basics_CPP();
        Options_Uncertainty options = new Options_Uncertainty();

        public CS_Depth_Uncertainty(VBtask task) : base(task)
        {
            labels[3] = "Mask of areas with stable depth";
            desc = "Use the bio-inspired retina algorithm to determine depth uncertainty.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            retina.Run(task.depthRGB);
            dst2 = retina.dst2;
            Cv2.Threshold(retina.dst3, dst3, options.uncertaintyThreshold, 255, ThresholdTypes.Binary);
        }
    }

    public class CS_Depth_Palette : CS_Parent
    {
        Mat customColorMap = new Mat();
        Gradient_Color gColor = new Gradient_Color();

        public CS_Depth_Palette(VBtask task) : base(task)
        {
            desc = "Use a palette to display depth from the raw depth data.";
        }

        public void RunCS(Mat src)
        {
            gColor.gradientWidth = 255;
            gColor.Run(empty);
            customColorMap = gColor.gradient;

            double mult = 255 / task.MaxZmeters;
            Mat depthNorm = (task.pcSplit[2] * mult).ToMat();
            depthNorm.ConvertTo(depthNorm, MatType.CV_8U);
            Mat ColorMap = new Mat(256, 1, MatType.CV_8UC3, customColorMap.Data);
            Cv2.ApplyColorMap(src, dst2, ColorMap);
        }
    }

    public class CS_Depth_Colorizer_CPP : CS_Parent
    {
        public CS_Depth_Colorizer_CPP(VBtask task) : base(task)
        {
            cPtr = Depth_Colorizer_Open();
            desc = "Display depth data with InRange. Higher contrast than others - yellow to blue always present.";
        }

        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32F)
                src = task.pcSplit[2];

            byte[] depthData = new byte[src.Total() * src.ElemSize()];
            GCHandle handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned);
            Marshal.Copy(src.Data, depthData, 0, depthData.Length);
            IntPtr imagePtr = Depth_Colorizer_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.MaxZmeters);
            handleSrc.Free();

            if (imagePtr != IntPtr.Zero)
                dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, imagePtr);
        }

        public void Close()
        {
            if (cPtr != IntPtr.Zero)
                cPtr = Depth_Colorizer_Close(cPtr);
        }
    }

    public class CS_Depth_LocalMinMax_MT : CS_Parent
    {
        public Point2f[] minPoint = new Point2f[1];
        public Point2f[] maxPoint = new Point2f[1];

        public CS_Depth_LocalMinMax_MT(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Highlight (usually yellow) is min distance, red is max distance",
                                "Highlight is min, red is max. Lines would indicate planes are present." };
            desc = "Find min and max depth in each segment.";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                src.CopyTo(dst2);
                dst2.SetTo(Scalar.White, task.gridMask);
            }

            if (minPoint.Length != task.gridList.Count)
            {
                Array.Resize(ref minPoint, task.gridList.Count);
                Array.Resize(ref maxPoint, task.gridList.Count);
            }

            if (task.heartBeat)
                dst3.SetTo(0);

            Parallel.For(0, task.gridList.Count, i =>
            {
                Rect roi = task.gridList[i];
                mmData mm = GetMinMax(task.pcSplit[2][roi], task.depthMask[roi]);
                if (mm.minLoc.X < 0 || mm.minLoc.Y < 0)
                    mm.minLoc = new cv.Point(0, 0);
                minPoint[i] = new cv.Point(mm.minLoc.X + roi.X, mm.minLoc.Y + roi.Y);
                maxPoint[i] = new cv.Point(mm.maxLoc.X + roi.X, mm.maxLoc.Y + roi.Y);

                Cv2.Circle(dst2[roi], mm.minLoc, task.DotSize, task.HighlightColor);
                Cv2.Circle(dst2[roi], mm.maxLoc, task.DotSize, Scalar.Red);

                cv.Point p1 = new cv.Point(mm.minLoc.X + roi.X, mm.minLoc.Y + roi.Y);
                cv.Point p2 = new cv.Point(mm.maxLoc.X + roi.X, mm.maxLoc.Y + roi.Y);
                Cv2.Circle(dst3, p1, task.DotSize, task.HighlightColor);
                Cv2.Circle(dst3, p2, task.DotSize, Scalar.Red);
            });
        }
    }



    public class CS_Depth_Median : CS_Parent
    {
        Math_Median_CDF median;
        public CS_Depth_Median(VBtask task) : base(task)
        {
            median = new Math_Median_CDF();
            median.rangeMax = (int)task.MaxZmeters;
            median.rangeMin = 0;
            desc = "Divide the depth image ahead and behind the median.";
        }
        public void RunCS(Mat src)
        {
            median.Run(task.pcSplit[2]);

            Mat mask = task.pcSplit[2].LessThan(median.medianVal);
            task.pcSplit[2].CopyTo(dst2, mask);

            dst2.SetTo(0, task.noDepthMask);

            labels[2] = "Median Depth < " + median.medianVal.ToString("F1");

            dst3.SetTo(0);
            task.depthRGB.CopyTo(dst3, ~mask);
            dst3.SetTo(0, task.noDepthMask);
            labels[3] = "Median Depth > " + median.medianVal.ToString("F1");
        }
    }

    public class CS_Depth_SmoothingMat : CS_Parent
    {
        Options_Depth options = new Options_Depth();
        Mat lastDepth;
        public CS_Depth_SmoothingMat(VBtask task) : base(task)
        {
            labels[3] = "Depth pixels after smoothing";
            desc = "Use depth rate of change to smooth the depth values in close range";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            Rect rect = task.drawRect.Width != 0 ? task.drawRect : new Rect(0, 0, src.Width, src.Height);

            if (task.FirstPass) lastDepth = task.pcSplit[2].Clone();
            Cv2.Subtract(lastDepth, task.pcSplit[2], dst2);

            dst2 = dst2.Threshold(options.mmThreshold, 0, ThresholdTypes.TozeroInv).Threshold(-options.mmThreshold, 0, ThresholdTypes.Tozero);
            Cv2.Add(task.pcSplit[2], dst2, dst3);
            lastDepth = task.pcSplit[2];

            labels[2] = "Smoothing Mat: range to " + task.MaxZmeters.ToString() + " meters";
        }
    }

    public class CS_Depth_Smoothing : CS_Parent
    {
        Depth_SmoothingMat smooth = new Depth_SmoothingMat();
        Reduction_Basics reduction = new Reduction_Basics();
        public Mat reducedDepth = new Mat();
        Mat_4to1 mats = new Mat_4to1();
        Depth_ColorMap colorize = new Depth_ColorMap();
        public CS_Depth_Smoothing(VBtask task) : base(task)
        {
            task.redOptions.checkBitReduction(true);
            labels[3] = "Mask of depth that is smooth";
            desc = "This attempt to get the depth data to 'calm' down is not working well enough to be useful - needs more work";
        }
        public void RunCS(Mat src)
        {
            smooth.Run(task.pcSplit[2]);
            Mat input = smooth.dst2.Normalize(0, 255, NormTypes.MinMax);
            input.ConvertTo(mats.mat[0], MatType.CV_8UC1);
            Mat tmp = new Mat();
            Cv2.Add(smooth.dst3, smooth.dst2, tmp);
            mats.mat[1] = tmp.Normalize(0, 255, NormTypes.MinMax).ConvertScaleAbs();

            reduction.Run(task.pcSplit[2]);
            reduction.dst2.ConvertTo(reducedDepth, MatType.CV_32F);
            colorize.Run(reducedDepth);
            dst2 = colorize.dst2;
            mats.Run(new Mat());
            dst3 = mats.dst2;
            labels[2] = smooth.labels[2];
        }
    }

    public class CS_Depth_HolesOverTime : CS_Parent
    {
        List<Mat> images = new List<Mat>();
        public CS_Depth_HolesOverTime(VBtask task) : base(task)
        {
            dst0 = new Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0));
            dst1 = new Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0));
            labels[3] = "Latest hole mask";
            desc = "Integrate memory holes over time to identify unstable depth";
        }
        public void RunCS(Mat src)
        {
            if (task.optionsChanged)
            {
                images.Clear();
                dst0.SetTo(0);
            }

            dst3 = task.noDepthMask;
            dst1 = dst3.Threshold(0, 1, ThresholdTypes.Binary);
            images.Add(dst1);

            dst0 += dst1;
            dst2 = dst0.Threshold(0, 255, ThresholdTypes.Binary);

            labels[2] = "Depth holes integrated over the past " + images.Count.ToString() + " images";
            if (images.Count >= task.frameHistoryCount)
            {
                dst0 -= images[0];
                images.RemoveAt(0);
            }
        }
    }

    public class CS_Depth_Holes : CS_Parent
    {
        Mat element;
        Options_DepthHoles options = new Options_DepthHoles();
        public CS_Depth_Holes(VBtask task) : base(task)
        {
            labels[3] = "Shadow Edges (use sliders to expand)";
            element = Cv2.GetStructuringElement(MorphShapes.Rect, new cv.Size(5, 5));
            desc = "Identify holes in the depth image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = task.pcSplit[2].Threshold(0.01, 255, ThresholdTypes.BinaryInv).ConvertScaleAbs(255);
            dst2 = dst2.Dilate(element, null, options.holeDilation);
            dst3 = dst2.Dilate(element, null, options.borderDilation);
            dst3 = dst3.Xor(dst2);
            if (standaloneTest()) task.depthRGB.CopyTo(dst3, dst3);
        }
    }

    public class CS_Depth_Dilate : CS_Parent
    {
        Dilate_Basics dilate = new Dilate_Basics();
        public CS_Depth_Dilate(VBtask task) : base(task)
        {
            desc = "Dilate the depth data to fill holes.";
        }
        public void RunCS(Mat src)
        {
            dilate.Run(task.pcSplit[2]);
            dst2 = dilate.dst2;
        }
    }

    public class CS_Depth_ForegroundHead : CS_Parent
    {
        Depth_ForegroundBlob fgnd = new Depth_ForegroundBlob();
        Kalman_Basics kalman = new Kalman_Basics();
        //Rect trustedRect;
        public bool trustworthy = false;
        public CS_Depth_ForegroundHead(VBtask task) : base(task)
        {
            labels[2] = "Blue is current, red is kalman, green is trusted";
            desc = "Use Depth_ForeGround to find the foreground blob.  Then find the probable head of the person in front of the camera.";
        }
        public void RunCS(Mat src)
        {
            fgnd.Run(src);

            //trustworthy = false;
            //if (fgnd.dst2.CountNonZero() > 0 && fgnd.maxIndex >= 0)
            //{
            //    int rectSize = 50;
            //    if (src.Width > 1000) rectSize = 250;
            //    int xx = fgnd.blobLocation[fgnd.maxIndex].X - rectSize / 2;
            //    int yy = fgnd.blobLocation[fgnd.maxIndex].Y;
            //    if (xx < 0) xx = 0;
            //    if (xx + rectSize / 2 > src.Width) xx = src.Width - rectSize;
            //    dst2 = fgnd.dst2.CvtColor(ColorConversionCodes.GRAY2BGR);

            //    kalman.kInput = new float[] { xx, yy, rectSize, rectSize };
            //    kalman.Run(src);
            //    Rect nextRect = new Rect(xx, yy, rectSize, rectSize);
            //    Rect kRect = new Rect((int)kalman.kOutput[0], (int)kalman.kOutput[1], (int)kalman.kOutput[2], (int)kalman.kOutput[3]);
            //    dst2.Rectangle(kRect, Scalar.Red, 2);
            //    dst2.Rectangle(nextRect, Scalar.Blue, 2);
            //    if (Math.Abs(kRect.X - nextRect.X) < rectSize / 4 && Math.Abs(kRect.Y - nextRect.Y) < rectSize / 4)
            //    {
            //        trustedRect = ValidateRect(kRect);
            //        trustworthy = true;
            //        dst2.Rectangle(trustedRect, Scalar.Green, 5);
            //    }
            //}
        }
        Rect ValidateRect(Rect rect)
        {
            throw new NotImplementedException();
        }
    }

    public class CS_Depth_RGBShadow : CS_Parent
    {
        public CS_Depth_RGBShadow(VBtask task) : base(task)
        {
            desc = "Merge the BGR and Depth Shadow";
        }
        public void RunCS(Mat src)
        {
            dst2 = src;
            dst2.SetTo(0, task.noDepthMask);
        }
    }

    public class CS_Depth_BGSubtract : CS_Parent
    {
        BGSubtract_Basics bgSub = new BGSubtract_Basics();
        public CS_Depth_BGSubtract(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Latest task.noDepthMask", "BGSubtract output for the task.noDepthMask" };
            desc = "Create a mask for the missing depth across multiple frame";
        }
        public void RunCS(Mat src)
        {
            dst2 = task.noDepthMask;

            bgSub.Run(dst2);
            dst3 = bgSub.dst2;
        }
    }

    public class CS_Depth_Averaging : CS_Parent
    {
        Math_ImageAverage avg = new Math_ImageAverage();
        Depth_Colorizer_CPP colorize = new Depth_Colorizer_CPP();
        public CS_Depth_Averaging(VBtask task) : base(task)
        {
            labels[3] = "32-bit format depth data";
            desc = "Take the average depth at each pixel but eliminate any pixels that had zero depth.";
        }
        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32F) src = task.pcSplit[2];
            avg.Run(src);

            dst3 = avg.dst2;
            colorize.Run(dst3);
            dst2 = colorize.dst2;
        }
    }

    public class CS_Depth_MaxMask : CS_Parent
    {
        Contour_General contour = new Contour_General();
        public CS_Depth_MaxMask(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Depth that is too far", "Contour of depth that is too far..." };
            desc = "Display the task.maxDepthMask and its contour containing depth that is greater than maxdepth (global setting)";
        }
        public void RunCS(Mat src)
        {
            dst2 = src;

            //    If task.maxDepthMask.Width = 0 Then
            //    task.maxDepthMask = task.pcSplit(2).InRange(task.MaxZmeters, task.MaxZmeters).ConvertScaleAbs()
            //End If
            if (task.maxDepthMask.Width == 0) task.maxDepthMask = task.pcSplit[2].InRange(task.MaxZmeters, task.MaxZmeters).ConvertScaleAbs();
            dst2.SetTo(Scalar.White, task.maxDepthMask);
            contour.Run(task.maxDepthMask);
            dst3 = new Mat();
            dst3.SetTo(Scalar.All(0));
            foreach (var c in contour.allContours)
            {
                List<cv.Point> hull = Cv2.ConvexHull(c.ToArray(), true).ToList();
                DrawContour(dst3, hull, Scalar.White, -1);
            }
        }
    }

    public class CS_Depth_ForegroundOverTime : CS_Parent
    {
        Options_ForeGround options = new Options_ForeGround();
        Depth_Foreground fore = new Depth_Foreground();
        Contour_Largest contours = new Contour_Largest();
        List<Mat> lastFrames = new List<Mat>();
        public CS_Depth_ForegroundOverTime(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Foreground objects", "Edges for the Foreground Objects" };
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0));
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, Scalar.All(0));
            task.frameHistoryCount = 5;
            desc = "Create a fused foreground mask over x number of frames (task.frameHistoryCount)";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged) lastFrames.Clear();

            fore.Run(src);
            lastFrames.Add(fore.dst3);
            dst2.SetTo(Scalar.All(0));
            foreach (Mat m in lastFrames)
            {
                dst2 += m;
            }
            if (lastFrames.Count >= task.frameHistoryCount) lastFrames.RemoveAt(0);

            contours.Run(dst2);
            dst2.SetTo(Scalar.All(0));
            dst3.SetTo(Scalar.All(0));
            foreach (var ctr in contours.allContours)
            {
                if (ctr.Length >= options.minSizeContour)
                {
                    DrawContour(dst2, ctr.ToList(), Scalar.White, -1);
                    DrawContour(dst3, ctr.ToList(), Scalar.White, 1);
                }
            }
        }
    }




    public class CS_Depth_ForegroundBlob : CS_Parent
    {
        Options_ForeGround options = new Options_ForeGround();
        List<cv.Point> blobLocation = new List<cv.Point>();
        int maxIndex;

        public CS_Depth_ForegroundBlob(VBtask task) : base(task)
        {
            labels[2] = "Mask for the largest foreground blob";
            desc = "Use InRange to define foreground and find the largest blob in the foreground";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Cv2.InRange(task.pcSplit[2], 0.01, options.maxForegroundDepthInMeters, dst2);
            dst3 = dst2.Clone();

            List<int> blobSize = new List<int>();
            blobLocation.Clear();

            for (int y = 0; y < dst2.Rows; y++)
            {
                for (int x = 0; x < dst2.Cols; x++)
                {
                    byte nextByte = dst2.At<byte>(y, x);
                    if (nextByte != 0)
                    {
                        int count = Cv2.FloodFill(dst2, new cv.Point(x, y), cv.Scalar.All(0), out _, new Scalar(0), new Scalar(0));
                        if (count > 10)
                        {
                            blobSize.Add(count);
                            blobLocation.Add(new cv.Point(x, y));
                        }
                    }
                }
            }

            if (blobSize.Count > 0)
            {
                int maxBlob = blobSize.Max();
                maxIndex = blobSize.IndexOf(maxBlob);
                Cv2.FloodFill(dst3, blobLocation[maxIndex], cv.Scalar.All(250), out _, new Scalar(0), new Scalar(0));
                Cv2.InRange(dst3, 250, 250, dst2);
                Cv2.BitwiseAnd(dst2, task.noDepthMask, dst2);
                labels[3] = "Mask of all depth pixels < " + options.maxForegroundDepthInMeters.ToString("0.0") + "m";
            }
        }
    }

    public class CS_Depth_Foreground : CS_Parent
    {
        Options_ForeGround options = new Options_ForeGround();
        Contour_Largest contours = new Contour_Largest();

        public CS_Depth_Foreground(VBtask task) : base(task)
        {
            labels[2] = "Foreground objects";
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0));
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, Scalar.All(0));
            desc = "Create a mask for the objects in the foreground";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Cv2.Threshold(task.pcSplit[2], dst1, options.maxForegroundDepthInMeters, 255, cv.ThresholdTypes.BinaryInv);
            dst1 = dst1.ConvertScaleAbs();

            Cv2.BitwiseAnd(dst1, task.noDepthMask, dst1);

            contours.Run(dst1);
            dst2.SetTo(Scalar.All(0));
            dst3.SetTo(Scalar.All(0));

            foreach (var ctr in contours.allContours)
            {
                if (ctr.Length >= options.minSizeContour)
                {
                    DrawContour(dst2, ctr.ToList(), Scalar.White, -1);
                    DrawContour(dst3, ctr.ToList(), Scalar.White, -1);
                }
            }
        }
    }

    public class CS_Depth_Grid : CS_Parent
    {
        public CS_Depth_Grid(VBtask task) : base(task)
        {
            task.gOptions.setGridSize(4);
            labels = new string[] { "", "", "White regions below are likely depth edges where depth changes rapidly", "Depth 32f display" };
            desc = "Find boundaries in depth to separate featureless regions.";
        }

        public void RunCS(Mat src)
        {
            dst3 = task.pcSplit[2];
            dst2 = task.gridMask.Clone();

            foreach (Rect roi in task.gridList)
            {
                double minVal, maxVal;
                Cv2.MinMaxLoc(dst3[roi], out minVal, out maxVal);
                if (Math.Abs(minVal - maxVal) > 0.1)
                {
                    dst2[roi].SetTo(Scalar.White);
                }
            }
        }
    }

    public class CS_Depth_InRange : CS_Parent
    {
        Options_ForeGround options = new Options_ForeGround();
        Contour_Largest contours = new Contour_Largest();
        int classCount = 1;

        public CS_Depth_InRange(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Looks empty! But the values are there - 0 to classcount.  Run standaloneTest() to see the palette output for this", "Edges between the depth regions." };
            if (standaloneTest()) { task.gOptions.setDisplay1(); }
            dst3 = new Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0));
            desc = "Create the selected number of depth ranges ";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            List<Mat> regMats = new List<Mat>();
            for (int i = 0; i < options.numberOfRegions; i++)
            {
                double upperBound = (i + 1) * options.depthPerRegion;
                if (i == options.numberOfRegions - 1) { upperBound = 1000; }
                Mat tmp = new Mat();
                Cv2.InRange(task.pcSplit[2], i * options.depthPerRegion, upperBound, tmp);
                regMats.Add(tmp);
                if (i == 0) { regMats[0].SetTo(0, task.noDepthMask); }
            }

            dst2 = new Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0));
            dst3.SetTo(0);
            classCount = 1;
            foreach (Mat regMat in regMats)
            {
                contours.Run(regMat);
                foreach (var ctr in contours.allContours)
                {
                    if (ctr.Length >= options.minSizeContour)
                    {
                        DrawContour(dst2, ctr.ToList(), Scalar.White, -1);
                        classCount++;
                        DrawContour(dst3, ctr.ToList(), Scalar.White, -1);
                    }
                }
            }

            dst0 = src.Clone();
            dst0.SetTo(cv.Scalar.White, dst3);

            if (standaloneTest())
            {
                dst2 = ShowPalette(dst2 * 255 / classCount);
            }

            if (task.heartBeat) { labels[2] = classCount.ToString("000") + " regions were found"; }
        }
    }






    public class CS_Depth_Regions : CS_Parent
    {
        int classCount = 5;

        public CS_Depth_Regions(VBtask task) : base(task)
        {
            desc = "Separate the scene into a specified number of regions by depth";
        }

        public void RunCS(Mat src)
        {
            Cv2.Threshold(task.pcSplit[2], dst1, task.gOptions.maxDepth, 255, ThresholdTypes.Binary);
            dst0 = (task.pcSplit[2] / task.gOptions.maxDepth) * 255 / classCount;
            Cv2.ConvertScaleAbs(dst0, dst2);
            Cv2.BitwiseAnd(dst2, task.noDepthMask, dst2);

            if (standaloneTest()) { dst3 = ShowPalette(dst2); }
            labels[2] = classCount.ToString() + " regions defined in the depth data";
        }
    }


    public class CS_Depth_PunchIncreasing : CS_Parent
    {
        Depth_PunchDecreasing depth = new Depth_PunchDecreasing();

        public CS_Depth_PunchIncreasing(VBtask task) : base(task)
        {
            depth.Increasing = true;
            desc = "Identify where depth is increasing - retreating from the camera.";
        }

        public void RunCS(Mat src)
        {
            depth.Run(src);
            dst2 = depth.dst2;
        }
    }

    public class CS_Depth_PunchDecreasing : CS_Parent
    {
        public bool Increasing { get; set; }
        Depth_Foreground fore = new Depth_Foreground();
        Mat lastDepth;
        Options_Depth options = new Options_Depth();
        public CS_Depth_PunchDecreasing(VBtask task) : base(task)
        {
            dst1 = new Mat(dst1.Size(), MatType.CV_32F, Scalar.All(0));
            desc = "Identify where depth is decreasing - coming toward the camera.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            fore.Run(src);
            dst1.SetTo(0);
            Cv2.CopyTo(dst1, fore.dst2, task.noDepthMask);

            if (task.FirstPass) lastDepth = dst1.Clone();
            if (Increasing)
            {
                dst2 = new Mat();
                Cv2.Subtract(dst1, lastDepth, dst2);
            }
            else
            {
                dst2 = new Mat();
                Cv2.Subtract(lastDepth, dst1, dst2);
            }
            Cv2.Threshold(dst2, dst2, options.mmThreshold, 0, cv.ThresholdTypes.Tozero);
            Cv2.Threshold(dst2, dst2, 0, 255, cv.ThresholdTypes.Binary);
            lastDepth = dst1.Clone();
        }
    }

    public class CS_Depth_PunchBlob : CS_Parent
    {
        Depth_PunchDecreasing depthDec = new Depth_PunchDecreasing();
        Depth_PunchIncreasing depthInc = new Depth_PunchIncreasing();
        Contour_General contours = new Contour_General();
        int lastContoursCount;
        int punchCount;
        int showMessage;
        int showWarningInfo;

        public CS_Depth_PunchBlob(VBtask task) : base(task)
        {
            desc = "Identify the punch with a rectangle around the largest blob";
        }

        public void RunCS(Mat src)
        {
            depthInc.Run(src);
            dst1 = depthInc.dst2;

            double minVal, maxVal;
            Cv2.MinMaxLoc(dst1, out minVal, out maxVal);
            Cv2.ConvertScaleAbs(dst1, dst2);
            contours.Run(dst2);
            dst3 = contours.dst3;

            if (contours.contourlist.Count > 0) { showMessage = 30; }

            if (showMessage == 30 && lastContoursCount == 0) { punchCount++; }
            lastContoursCount = contours.contourlist.Count;
            labels[3] = punchCount.ToString() + " Punches Thrown";

            if (showMessage > 0)
            {
                SetTrueText("Punched!!!", new cv.Point(10, 100), 3);
                showMessage--;
            }

            if (contours.contourlist.Count > 3) { showWarningInfo = 100; }

            if (showWarningInfo > 0)
            {
                showWarningInfo--;
                SetTrueText("Too many contours!  Reduce the Max Depth.", new cv.Point(10, 130), 3);
            }
        }
    }

    public class CS_Depth_PunchBlobNew : CS_Parent
    {
        Depth_PunchDecreasing depthDec = new Depth_PunchDecreasing();
        Depth_PunchIncreasing depthInc = new Depth_PunchIncreasing();
        Contour_General contours = new Contour_General();
        Mat lastColor;
        Options_Depth options = new Options_Depth();
        public CS_Depth_PunchBlobNew(VBtask task) : base(task)
        {
            desc = "Identify a punch using both depth and color";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.FirstPass) lastColor = task.color.Clone();
            dst2 = task.color.Clone();

            Cv2.Absdiff(dst2, lastColor, dst2);
            Cv2.Threshold(dst2, dst3, 0, options.threshold, ThresholdTypes.Binary);
            Cv2.ConvertScaleAbs(dst3, dst3);

            Cv2.Threshold(dst2, dst2, 0, 255, ThresholdTypes.Binary);

            lastColor = task.color.Clone();
        }
    }

    public class CS_Depth_Contour : CS_Parent
    {
        Contour_General contour = new Contour_General();

        public CS_Depth_Contour(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0));
            labels[2] = "task.depthMask contour";
            desc = "Create and display the task.depthMask output as a contour.";
        }

        public void RunCS(Mat src)
        {
            contour.Run(task.depthMask);

            dst2.SetTo(0);
            foreach (var tour in contour.contourlist)
            {
                DrawContour(dst2, tour.ToList(), Scalar.All(255), -1);
            }
        }
    }

    public class CS_Depth_Outline : CS_Parent
    {
        Contour_General contour = new Contour_General();

        public CS_Depth_Outline(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0));
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, Scalar.All(0));
            labels[2] = "Contour separating depth from no depth";
            desc = "Provide a line that separates depth from no depth throughout the image.";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest()) { src = task.depthMask; }
            contour.Run(src);

            dst2.SetTo(0);
            foreach (var tour in contour.contourlist)
            {
                DrawContour(dst2, tour.ToList(), Scalar.All(255), task.lineWidth);
            }

            if (standaloneTest())
            {
                if (task.heartBeat) { dst3.SetTo(0); }
                Cv2.BitwiseOr(dst3, dst2, dst3);
            }
        }
    }

    public class CS_Depth_StableAverage : CS_Parent
    {
        Depth_Averaging dAvg = new Depth_Averaging();
        Depth_StableMinMax extrema = new Depth_StableMinMax();

        public CS_Depth_StableAverage(VBtask task) : base(task)
        {
            FindRadio("Use farthest distance").Checked = true;
            desc = "Use Depth_StableMax to remove the artifacts from the Depth_Averaging";
        }

        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32F) { src = task.pcSplit[2]; }
            extrema.Run(src);

            if (extrema.options.useNone)
            {
                dst2 = extrema.dst2;
                dst3 = extrema.dst3;
            }
            else
            {
                dAvg.Run(extrema.dst3);
                dst2 = dAvg.dst2;
                dst3 = dAvg.dst3;
            }
        }
    }

    public class CS_Depth_MinMaxNone : CS_Parent
    {
        Options_MinMaxNone options = new Options_MinMaxNone();
        int filtered;

        public CS_Depth_MinMaxNone(VBtask task) : base(task)
        {
            desc = "To reduce z-Jitter, use the closest or farthest point as long as the camera is stable";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat[] split = src.Type() == MatType.CV_32FC3 ? src.Split() : task.pcSplit;

            if (task.heartBeat)
            {
                dst3 = split[2];
                filtered = 0;
            }
            labels[2] = "Point cloud unchanged";
            if (options.useMax)
            {
                labels[2] = "Point cloud maximum values at each pixel";
                Cv2.Max(split[2], dst3, split[2]);
            }
            if (options.useMin)
            {
                labels[2] = "Point cloud minimum values at each pixel";
                Mat saveMat = split[2].Clone();
                Cv2.Min(split[2], dst3, split[2]);
                Mat mask = new Mat();
                Cv2.InRange(split[2], 0, 0.1, mask);
                Cv2.CopyTo(saveMat, split[2], mask);
            }
            Cv2.Merge(split, dst2);
            dst3 = split[2];
            filtered++;
            labels[2] += " after " + filtered.ToString() + " images";
        }
    }

    public class CS_Depth_StableMin : CS_Parent
    {
        Mat stableMin;
        Depth_Colorizer_CPP colorize = new Depth_Colorizer_CPP();

        public CS_Depth_StableMin(VBtask task) : base(task)
        {
            task.gOptions.setUnfiltered(true);
            labels = new string[] { "", "", "InRange depth with low quality depth removed.", "Motion in the BGR image. Depth updated in rectangle." };
            desc = "To reduce z-Jitter, use the closest depth value at each pixel as long as the camera is stable";
        }

        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32FC1) { src = task.pcSplit[2]; }

            if (task.heartBeat)
            {
                stableMin = src.Clone();
                dst3.SetTo(0);
            }
            else if (task.motionDetected)
            {
                Cv2.CopyTo(stableMin[task.motionRect], src[task.motionRect]);
                if (src.Type() != stableMin.Type()) { Cv2.ConvertScaleAbs(src, stableMin); }
                Cv2.CopyTo(src[task.motionRect], stableMin[task.motionRect], task.noDepthMask);
                Cv2.Min(src, stableMin, stableMin);
            }

            colorize.Run(stableMin);
            dst2 = colorize.dst2;
        }
    }

    public class CS_Depth_StableMax : CS_Parent
    {
        Mat stableMax;
        Depth_Colorizer_CPP colorize = new Depth_Colorizer_CPP();

        public CS_Depth_StableMax(VBtask task) : base(task)
        {
            task.gOptions.setUnfiltered(true);
            labels = new string[] { "", "", "InRange depth with low quality depth removed.", "Motion in the BGR image. Depth updated in rectangle." };
            desc = "To reduce z-Jitter, use the farthest depth value at each pixel as long as the camera is stable";
        }

        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32FC1) { src = task.pcSplit[2]; }

            if (task.heartBeat)
            {
                stableMax = src.Clone();
                dst3.SetTo(0);
            }
            else if (task.motionDetected)
            {
                Cv2.CopyTo(stableMax[task.motionRect], src[task.motionRect]);
                if (src.Type() != stableMax.Type()) { Cv2.ConvertScaleAbs(src, stableMax); }
                Cv2.CopyTo(src[task.motionRect], stableMax[task.motionRect], task.noDepthMask);
                Cv2.Max(src, stableMax, stableMax);
            }

            colorize.Run(stableMax);
            dst2 = colorize.dst2;
        }
    }

    public class CS_Depth_StableMinMax : CS_Parent
    {
        Depth_Colorizer_CPP colorize = new Depth_Colorizer_CPP();
        Depth_StableMin dMin = new Depth_StableMin();
        Depth_StableMax dMax = new Depth_StableMax();
        Options_MinMaxNone options = new Options_MinMaxNone();

        public CS_Depth_StableMinMax(VBtask task) : base(task)
        {
            task.gOptions.setUnfiltered(true);
            labels[2] = "Depth map colorized";
            labels[3] = "32-bit StableDepth";
            desc = "To reduce z-Jitter, use the closest or farthest point as long as the camera is stable";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (src.Type() != MatType.CV_32FC1) { src = task.pcSplit[2]; }
            if (task.optionsChanged) { dst3 = task.pcSplit[2]; }

            if (options.useMax)
            {
                dMax.Run(src);
                dst3 = dMax.stableMax;
                dst2 = dMax.dst2;
            }
            else if (options.useMin)
            {
                dMin.Run(src);
                dst3 = dMin.stableMin;
                dst2 = dMin.dst2;
            }
            else if (options.useNone)
            {
                dst3 = task.pcSplit[2];
                dst2 = task.depthRGB;
            }
        }
    }

    public class CS_Depth_WorldXYMT : CS_Parent
    {
        bool depthUnitsMeters = false;

        public CS_Depth_WorldXYMT(VBtask task) : base(task)
        {
            labels[3] = "dst3 = pointcloud";
            desc = "Create OpenGL point cloud from depth data (slow)";
        }

        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32FC1) { src = task.pcSplit[2]; }

            dst3 = new Mat(src.Size(), MatType.CV_32FC3, Scalar.All(0));
            if (!depthUnitsMeters) { src = src * 0.001; }
            double multX = task.pointCloud.Width / src.Width;
            double multY = task.pointCloud.Height / src.Height;

            Parallel.ForEach(task.gridList, roi =>
            {
                Point3f xy = new Point3f();
                for (int y = roi.Y; y < roi.Y + roi.Height; y++)
                {
                    for (int x = roi.X; x < roi.X + roi.Width; x++)
                    {
                        xy.X = x * (float)multX;
                        xy.Y = y * (float)multY;
                        xy.Z = src.At<float>(y, x);
                        if (xy.Z != 0)
                        {
                            Point3f xyz = getWorldCoordinates(xy);
                            dst3.Set<Point3f>(y, x, xyz);
                        }
                    }
                }
            });

            SetTrueText("OpenGL data prepared.");
        }
    }


    public class CS_Depth_WorldXYZ : CS_Parent
    {
        public bool depthUnitsMeters = false;

        public CS_Depth_WorldXYZ(VBtask task) : base(task)
        {
            labels[3] = "dst3 = pointcloud";
            desc = "Create 32-bit XYZ format from depth data (too slow to be useful.)";
        }

        public void RunCS(Mat src)
        {
            if (src.Type() != MatType.CV_32FC1)
                src = task.pcSplit[2];

            if (!depthUnitsMeters)
                src = (src * 0.001).ToMat();

            dst2 = new Mat(src.Size(), MatType.CV_32FC3, 0);
            Point3f xy = new Point3f();

            for (xy.Y = 0; xy.Y < dst2.Height; xy.Y++)
            {
                for (xy.X = 0; xy.X < dst2.Width; xy.X++)
                {
                    xy.Z = src.Get<float>((int)xy.Y, (int)xy.X);
                    if (xy.Z != 0)
                    {
                        Point3f xyz = getWorldCoordinates(xy);
                        dst2.Set((int)xy.Y, (int)xy.X, xyz);
                    }
                }
            }

            SetTrueText("OpenGL data prepared and in dst2.", 3);
        }
    }

    public class CS_Depth_World : CS_Parent
    {
        Math_Template template = new Math_Template();

        public CS_Depth_World(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Merged templates and depth32f - should be similar to upper right image", "" };
            desc = "Build the (approximate) point cloud using camera intrinsics - see CameraOakD.cs for comparable calculations";
        }

        public void RunCS(Mat src)
        {
            if (task.FirstPass)
                template.Run(empty);

            if (src.Type() != MatType.CV_32F)
                src = task.pcSplit[2];

            Cv2.Multiply(template.dst2, src, dst0);
            dst0 *= 1 / task.calibData.fx;

            Cv2.Multiply(template.dst3, src, dst1);
            dst1 *= 1 / task.calibData.fy;

            Cv2.Merge(new Mat[] { dst0, dst1, src }, dst2);

            if (standaloneTest())
            {
                var colorizer = new Depth_Colorizer_CPP();
                colorizer.Run(dst2);
                dst2 = colorizer.dst2;
            }
        }
    }

    public class CS_Depth_TiersZ : CS_Parent
    {
        public int classCount;
        Options_Contours options = new Options_Contours();

        public CS_Depth_TiersZ(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": gOptions 'Max Depth (meters)' and local options for cm's per tier.");
            desc = "Create a reduced image of the depth data to define tiers of similar values";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (src.Type() != MatType.CV_32F) src = task.pcSplit[2];

            dst1 = (src * 100 / options.cmPerTier).ToMat();
            dst1.ConvertTo(dst2, MatType.CV_8U);

            classCount = (int)(task.MaxZmeters * 100 / options.cmPerTier) + 1;

            dst3 = ShowPalette(dst2 * 255 / classCount);
            labels[2] = $"{classCount} regions found.";
        }
    }

    public class CS_Depth_TierCount : CS_Parent
    {
        public HistValley_Depth1 valley = new HistValley_Depth1();
        public int classCount;
        List<int> kValues = new List<int>();

        public CS_Depth_TierCount(VBtask task) : base(task)
        {
            labels = new string[] { "", "Histogram of the depth data with instantaneous valley lines", "", "" };
            desc = "Determine the 'K' value for the best number of clusters for the depth";
        }

        public void RunCS(Mat src)
        {
            valley.Run(src);
            dst2 = valley.dst2;

            kValues.Add(valley.valleyOrder.Count);

            classCount = (int)kValues.Average();
            if (kValues.Count > task.frameHistoryCount * 10)
                kValues.RemoveAt(0);

            SetTrueText($"'K' value = {classCount} after averaging. Instantaneous value = {valley.valleyOrder.Count}", 3);
            labels[2] = $"There are {classCount}";
        }
    }

    public class CS_Depth_Flatland : CS_Parent
    {
        Options_FlatLand options = new Options_FlatLand();

        public CS_Depth_Flatland(VBtask task) : base(task)
        {
            labels[3] = "Grayscale version";
            desc = "Attempt to stabilize the depth image.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = task.depthRGB / options.reductionFactor;
            dst2 *= options.reductionFactor;
            dst3 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            dst3 = dst3.CvtColor(ColorConversionCodes.GRAY2BGR);
        }
    }


    public class CS_Derivative_Basics : CS_Parent
    {
        public Options_Derivative options = new Options_Derivative();
        BackProject_Image backp = new BackProject_Image();
        public Plot_Histogram plot = new Plot_Histogram();

        public CS_Derivative_Basics(VBtask task) : base(task)
        {
            backp.hist.plot.removeZeroEntry = false;
            UpdateAdvice(traceName + ": gOptions histogram Bins and several local options are important.");
            desc = "Display a first or second derivative of the selected depth dimension and direction.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (src.Type() != MatType.CV_32F)
            {
                src = task.pcSplit[options.channel].Sobel(MatType.CV_32F, 1, 0, options.kernelSize);
            }

            Rangef[] ranges = { new Rangef(-options.derivativeRange, options.derivativeRange) };
            Mat histogram = new Mat();
            Cv2.CalcHist(new[] { src }, new[] { 0 }, task.depthMask, histogram, 1, new[] { task.histogramBins }, ranges);

            plot.Run(histogram);
            histogram = plot.histogram; // reflect any updates to the 0 entry...
            dst2 = plot.dst2;

            int index = 1;
            for (int i = 0; i < plot.histArray.Length; i++)
            {
                if (plot.histArray[i] != 0)
                {
                    plot.histArray[i] = index;
                    index++;
                }
            }
            histogram = new Mat(plot.histArray.Length, 1, MatType.CV_32F, plot.histArray.ToArray());

            int brickWidth = dst2.Width / task.histogramBins;
            int histIndex = (int)(task.mouseMovePoint.X / brickWidth);

            Mat mask = new Mat();
            Cv2.CalcBackProject(new[] { src }, new[] { 0 }, histogram, mask, ranges);
            mask.ConvertTo(mask, MatType.CV_8U);
            dst0 = mask;
            mask = mask.InRange(histIndex, histIndex);

            dst3 = task.color.Clone();
            dst3.SetTo(Scalar.White, mask);
            dst3.SetTo(0, task.noDepthMask);
            Cv2.Rectangle(dst2, new Rect(histIndex * brickWidth, 0, brickWidth, dst2.Height), Scalar.Yellow, task.lineWidth);
            string deriv = string.Format(fmt2, options.derivativeRange);
            labels[2] = "Histogram of first or second derivatives.  Range -" + deriv + " to " + deriv;
            labels[3] = "Backprojection into the image for the selected histogram entry - move mouse over dst2.";
        }
    }

    public class CS_Derivative_Sobel : CS_Parent
    {
        CS_Derivative_Basics deriv;

        public CS_Derivative_Sobel(VBtask task) : base(task)
        {
            deriv = new CS_Derivative_Basics(task);
            if (standalone) task.gOptions.setDisplay1();
            if (standalone) task.gOptions.setDisplay1();
            desc = "Display the derivative of the selected depth dimension.";
        }

        public void RunCS(Mat src)
        {
            int channel = deriv.options.channel;
            string chanName = "X";
            if (channel != 0)
            {
                chanName = channel == 1 ? "Y" : "Z";
            }
            int kern = deriv.options.kernelSize;
            src = task.pcSplit[channel].Sobel(MatType.CV_32F, 1, 0, kern);
            deriv.RunAndMeasure(src, deriv);
            dst0 = deriv.dst2.Clone();
            dst1 = deriv.dst3.Clone();
            labels[0] = "Horizontal derivatives for " + chanName + " dimension of the point cloud";
            labels[1] = "Backprojection of horizontal derivatives indicated - move mouse in the image at left";

            src = task.pcSplit[channel].Sobel(MatType.CV_32F, 0, 1, kern);
            deriv.RunAndMeasure(src, deriv);
            dst2 = deriv.dst2;
            dst3 = deriv.dst3;
            labels[2] = "Vertical derivatives for " + chanName + " dimension of the point cloud";
            labels[3] = "Backprojection of vertical derivatives indicated - move mouse in the image at left";
        }
    }

    public class CS_Derivative_Laplacian : CS_Parent
    {
        Options_LaplacianKernels options = new Options_LaplacianKernels();
        CS_Derivative_Basics deriv;

        public CS_Derivative_Laplacian(VBtask task) : base(task)
        {
            deriv = new CS_Derivative_Basics(task);
            desc = "Create a histogram and backprojection for the second derivative of depth in the selected dimension.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            int channel = deriv.options.channel;
            cv.Size gausskern = new cv.Size((int)options.gaussiankernelSize, (int)options.gaussiankernelSize);
            dst1 = task.pcSplit[channel].GaussianBlur(gausskern, 0, 0);
            dst1 = dst1.Laplacian(MatType.CV_32F, options.LaplaciankernelSize, 1, 0);

            deriv.RunAndMeasure(dst1, deriv);
            dst2 = deriv.dst2;
            dst3 = deriv.dst3;
            labels[2] = deriv.labels[2];
            labels[3] = deriv.labels[3];
        }
    }

    public class CS_Derivative_Classes : CS_Parent
    {
        CS_Derivative_Basics deriv;
        public int classCountX;
        public int classCountY;

        public CS_Derivative_Classes(VBtask task) : base(task)
        {
            deriv = new CS_Derivative_Basics(task);
            desc = "Display the X and Y derivatives for the whole image.";
        }

        int derivClassCount(ref Mat dst)
        {
            int count = 0;
            for (int i = 0; i < deriv.plot.histArray.Length; i++)
            {
                if (deriv.plot.histArray[i] > 0) count++;
            }
            dst = ShowPalette(deriv.dst0 * 255 / count);
            dst.SetTo(0, task.noDepthMask);
            return count;
        }

        public void RunCS(Mat src)
        {
            deriv.RunAndMeasure(task.pcSplit[deriv.options.channel].Sobel(MatType.CV_32F, 1, 0, deriv.options.kernelSize), deriv);
            classCountX = derivClassCount(ref dst2);
            labels[2] = $"Backprojection of X dimension of task.pcSplit({deriv.options.channel})";

            deriv.RunAndMeasure(task.pcSplit[deriv.options.channel].Sobel(MatType.CV_32F, 0, 1, deriv.options.kernelSize), deriv);
            classCountY = derivClassCount(ref dst3);
            labels[3] = $"Backprojection of Y dimension of task.pcSplit({deriv.options.channel})";
        }
    }


    public class CS_DFT_Basics : CS_Parent
    {
        Mat_4to1 mats = new Mat_4to1();
        public Mat magnitude = new Mat();
        public Mat spectrum = new Mat();
        public Mat complexImage = new Mat();
        public Mat grayMat;
        public int rows;
        public int cols;

        public CS_DFT_Basics(VBtask task) : base(task)
        {
            mats.lineSeparators = false;

            desc = "Explore the Discrete Fourier Transform.";
            labels[2] = "Image after inverse DFT";
            labels[3] = "DFT_Basics Spectrum Magnitude";
        }

        public Mat InverseDFT(Mat complexImage)
        {
            Mat invDFT = new Mat();
            Cv2.Dft(complexImage, invDFT, DftFlags.Inverse | DftFlags.RealOutput);
            invDFT = invDFT.Normalize(0, 255, NormTypes.MinMax);
            invDFT.ConvertTo(invDFT, MatType.CV_8U);
            return invDFT;
        }
        public void RunCS(Mat src)
        {
            grayMat = src;
            if (src.Channels() == 3)
                grayMat = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            rows = Cv2.GetOptimalDFTSize(grayMat.Rows);
            cols = Cv2.GetOptimalDFTSize(grayMat.Cols);
            Mat padded = new Mat(grayMat.Width, grayMat.Height, MatType.CV_8UC3);
            Cv2.CopyMakeBorder(grayMat, padded, 0, rows - grayMat.Rows, 0, cols - grayMat.Cols, BorderTypes.Constant, Scalar.All(0));
            Mat padded32 = new Mat();
            padded.ConvertTo(padded32, MatType.CV_32F);
            Mat[] planes = { padded32, new Mat(padded.Size(), MatType.CV_32F, 0) };
            Cv2.Merge(planes, complexImage);
            Cv2.Dft(complexImage, complexImage);

            planes = complexImage.Split();

            Cv2.Magnitude(planes[0], planes[1], magnitude);
            magnitude += Scalar.All(1);
            Cv2.Log(magnitude, magnitude);

            spectrum = magnitude[new Rect(0, 0, magnitude.Cols & -2, magnitude.Rows & -2)];
            spectrum = spectrum.Normalize(0, 255, NormTypes.MinMax);
            spectrum.ConvertTo(padded, MatType.CV_8U);

            int cx = padded.Cols / 2;
            int cy = padded.Rows / 2;

            mats.mat[3] = padded[new Rect(0, 0, cx, cy)].Clone();
            mats.mat[2] = padded[new Rect(cx, 0, cx, cy)].Clone();
            mats.mat[1] = padded[new Rect(0, cy, cx, cy)].Clone();
            mats.mat[0] = padded[new Rect(cx, cy, cx, cy)].Clone();
            mats.Run(empty);
            dst3 = mats.dst2;

            dst2 = InverseDFT(complexImage);
        }
    }

    public class CS_DFT_Inverse : CS_Parent
    {
        Mat_2to1 mats = new Mat_2to1();
        CS_DFT_Basics dft;

        public CS_DFT_Inverse(VBtask task) : base(task)
        {
            dft = new CS_DFT_Basics(task);
            labels[2] = "Image after Inverse DFT";
            desc = "Take the inverse of the Discrete Fourier Transform.";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() == 3)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat gray32f = new Mat();
            src.ConvertTo(gray32f, MatType.CV_32F);
            Mat[] planes = { gray32f, new Mat(gray32f.Size(), MatType.CV_32F, 0) };
            Mat complex = new Mat();
            Mat complexImage = new Mat();
            Cv2.Merge(planes, complex);
            Cv2.Dft(complex, complexImage);

            dst2 = dft.InverseDFT(complexImage);

            Mat diff = new Mat();
            Cv2.Absdiff(src, dst2, diff);
            mats.mat[0] = diff.Threshold(0, 255, ThresholdTypes.Binary);
            mats.mat[1] = (diff * 50).ToMat();
            mats.Run(empty);
            if (mats.mat[0].CountNonZero() > 0)
            {
                dst3 = mats.dst2;
                labels[3] = "Mask of difference (top) and relative diff (bot)";
            }
            else
            {
                labels[3] = "InverseDFT reproduced original";
                dst3.SetTo(0);
            }
        }
    }




    public class CS_DFT_ButterworthDepth : CS_Parent
    {
        DFT_ButterworthFilter_MT bfilter = new DFT_ButterworthFilter_MT();

        public CS_DFT_ButterworthDepth(VBtask task) : base(task)
        {
            desc = "Use the Butterworth filter on a DFT image - RGBDepth as input.";
            labels[2] = "Image with Butterworth Low Pass Filter Applied";
            labels[3] = "Same filter with radius / 2";
        }

        public void RunCS(Mat src)
        {
            bfilter.Run(task.depthRGB.CvtColor(ColorConversionCodes.BGR2GRAY));
            dst2 = bfilter.dst2;
            dst3 = bfilter.dst3;
        }
    }

    public class CS_DFT_Shapes : CS_Parent
    {
        CS_DFT_Basics dft;
        Draw_Circles circle = new Draw_Circles();
        Draw_Ellipses ellipse = new Draw_Ellipses();
        Draw_Polygon polygon = new Draw_Polygon();
        Rectangle_Basics rectangle = new Rectangle_Basics();
        Draw_Lines lines = new Draw_Lines();
        Draw_SymmetricalShapes symShapes = new Draw_SymmetricalShapes();
        Options_Draw options = new Options_Draw();
        Options_DFTShape optionsDFT = new Options_DFTShape();

        public CS_DFT_Shapes(VBtask task) : base(task)
        {
            dft = new CS_DFT_Basics(task);
            FindSlider("DrawCount").Value = 1;
            labels = new string[] { "Inverse of the DFT - the same grayscale input.", "", "Input to the DFT", "Discrete Fourier Transform Output" };
            desc = "Show the spectrum magnitude for some standard shapes";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            optionsDFT.RunVB();

            switch (optionsDFT.dftShape)
            {
                case "Draw Circle":
                    circle.Run(src);
                    dst2 = circle.dst2;
                    break;
                case "Draw Ellipse":
                    ellipse.Run(src);
                    dst2 = ellipse.dst2;
                    break;
                case "Draw Line":
                    lines.Run(src);
                    dst2 = lines.dst2;
                    break;
                case "Draw Rectangle":
                    rectangle.Run(src);
                    dst2 = rectangle.dst2;
                    break;
                case "Draw Polygon":
                    polygon.Run(src);
                    dst2 = polygon.dst2;
                    break;
                case "Draw Symmetrical Shapes":
                    symShapes.Run(src);
                    dst2 = symShapes.dst2;
                    break;
                case "Draw Point":
                    if (task.heartBeat)
                    {
                        dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
                        var pt1 = new cv.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10));
                        var pt2 = new cv.Point(msRNG.Next(0, dst2.Width / 10), msRNG.Next(0, dst2.Height / 10));
                        dst2.Set<byte>(pt1.Y, pt1.X, 255);
                        dst2.Set<byte>(pt2.Y, pt2.X, 255);
                        labels[2] = $"pt1 = ({pt1.X},{pt1.Y})  pt2 = ({pt2.X},{pt2.Y})";
                    }
                    break;
            }

            dft.RunAndMeasure(dst2, dft);
            dst3 = dft.dst3;

            // the following line to view the inverse of the DFT transform.
            // It is the grayscale image of the input - no surprise.  It works!
            dst2 = dft.InverseDFT(dft.complexImage);
        }
    }








    public class CS_Diff_Basics : CS_Parent
    {
        public int changedPixels;
        public Mat lastFrame;

        public CS_Diff_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Unstable mask", "" };
            UpdateAdvice(traceName + ": use goption 'Pixel Difference Threshold' to control changed pixels.");
            desc = "Capture an image and compare it to previous frame using absDiff and threshold";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            if (task.FirstPass) lastFrame = src.Clone();
            if (task.optionsChanged || lastFrame.Size() != src.Size())
                lastFrame = src.Clone();

            Cv2.Absdiff(src, lastFrame, dst0);
            dst2 = dst0.Threshold(task.gOptions.pixelDiffThreshold, 255, ThresholdTypes.Binary);
            changedPixels = dst2.CountNonZero();

            if (changedPixels > 0)
            {
                lastFrame = src.Clone();
                int pixelD = task.gOptions.pixelDiffThreshold;
                strOut = "Motion detected - " + changedPixels.ToString() + " pixels changed with threshold " + pixelD.ToString();
                if (task.heartBeat)
                    labels[3] = strOut;
            }
            else
            {
                strOut = "No motion detected";
            }

            SetTrueText(strOut, 3);
        }
    }

    public class CS_Diff_Color : CS_Parent
    {
        public CS_Diff_Basics diff;

        public CS_Diff_Color(VBtask task) : base(task)
        {
            diff = new CS_Diff_Basics(task);
            labels = new string[] { "", "", "Each channel displays the channel's difference", "Mask with all differences" };
            desc = "Use Diff_Basics with a color image.";
        }

        public void RunCS(Mat src)
        {
            if (task.FirstPass)
                diff.lastFrame = src.Reshape(1, src.Rows * 3);

            diff.RunAndMeasure(src.Reshape(1, src.Rows * 3), diff);
            dst2 = diff.dst2.Reshape(3, src.Rows);
            dst3 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
        }
    }

    public class CS_Diff_UnstableDepthAndColor : CS_Parent
    {
        public CS_Diff_Basics diff;
        public Depth_NotMissing depth = new Depth_NotMissing();

        public CS_Diff_UnstableDepthAndColor(VBtask task) : base(task)
        {
            diff = new CS_Diff_Basics(task);
            labels = new string[] { "", "", "Stable depth and color", "Unstable depth/color mask" };
            desc = "Build a mask for any pixels that have either unstable depth or color";
        }

        public void RunCS(Mat src)
        {
            diff.RunAndMeasure(src, diff);
            Mat unstableGray = diff.dst2.Clone();
            depth.Run(task.depthRGB);
            Mat unstableDepth = new Mat();
            Mat mask = new Mat();
            Cv2.BitwiseNot(depth.dst3, unstableDepth);

            if (unstableGray.Channels() == 3)
                unstableGray = unstableGray.CvtColor(ColorConversionCodes.BGR2GRAY);

            Cv2.BitwiseOr(unstableGray, unstableDepth, mask);
            dst2 = src.Clone();
            dst2.SetTo(Scalar.Black, mask);
            dst3 = mask;
        }
    }

    public class CS_Diff_RGBAccum : CS_Parent
    {
        public CS_Diff_Basics diff;
        List<Mat> history = new List<Mat>();

        public CS_Diff_RGBAccum(VBtask task) : base(task)
        {
            diff = new CS_Diff_Basics(task);
            labels = new string[] { "", "", "Accumulated BGR image", "Mask of changed pixels" };
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, Scalar.Black);
            desc = "Run Diff_Basics and accumulate BGR diff data.";
        }

        public void RunCS(Mat src)
        {
            diff.RunAndMeasure(src, diff);
            if (task.optionsChanged)
                history.Clear();

            history.Add(diff.dst2);
            if (history.Count > task.frameHistoryCount)
                history.RemoveAt(0);

            dst2.SetTo(Scalar.Black);
            foreach (Mat m in history)
            {
                Cv2.BitwiseOr(dst2, m, dst2);
            }
        }
    }

    public class CS_Diff_Lines : CS_Parent
    {
        Diff_RGBAccum diff = new Diff_RGBAccum();
        Line_Basics lines = new Line_Basics();

        public CS_Diff_Lines(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Add motion to see Diff output and lines input", "Lines output" };
            desc = "identify lines in the diff output";
        }

        public void RunCS(Mat src)
        {
            diff.Run(src);
            dst2 = diff.dst2;

            lines.Run(dst2);
            dst3 = src.Clone();
            foreach (var lp in lines.lpList)
            {
                DrawLine(dst3, lp.p1, lp.p2, Scalar.Yellow, task.lineWidth);
            }
        }
    }



    public class CS_Diff_Heartbeat : CS_Parent
    {
        public int cumulativePixels;

        public CS_Diff_Heartbeat(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, 0);
            labels = new string[] { "", "", "Unstable mask", "Pixel difference" };
            desc = "Diff an image with one from the last heartbeat.";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() == 3)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            if (task.heartBeat)
            {
                dst1 = src.Clone();
                dst2.SetTo(0);
            }

            Cv2.Absdiff(src, dst1, dst3);
            cumulativePixels = Cv2.CountNonZero(dst3);
            dst2 = dst2 | dst3.Threshold(task.gOptions.pixelDiffThreshold, 255, ThresholdTypes.Binary);
        }
    }

    public class CS_Diff_DepthAccum : CS_Parent
    {
        Diff_Depth32S diff = new Diff_Depth32S();
        History_Basics frames = new History_Basics();

        public CS_Diff_DepthAccum(VBtask task) : base(task)
        {
            desc = "Accumulate the mask of depth differences.";
        }

        public void RunCS(Mat src)
        {
            diff.Run(src);
            frames.Run(diff.dst2);
            dst2 = frames.dst2;
            labels = diff.labels;
        }
    }

    public class CS_Diff_Depth32S : CS_Parent
    {
        public Mat lastDepth32s;
        Options_Depth options = new Options_Depth();

        public CS_Diff_Depth32S(VBtask task) : base(task)
        {
            lastDepth32s = dst0.Clone();
            desc = "Where is the depth difference between frames greater than X millimeters.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Mat depth32f = 1000 * task.pcSplit[2];
            depth32f.ConvertTo(dst0, MatType.CV_32S);

            if (task.optionsChanged)
                lastDepth32s = dst0.Clone();

            Cv2.Absdiff(dst0, lastDepth32s, dst1);
            dst1 = dst1.ConvertScaleAbs();
            var mm = GetMinMax(dst1);

            dst2 = dst1.Threshold(options.millimeters - 1, 255, ThresholdTypes.Binary);

            lastDepth32s = dst0.Clone();
            if (task.heartBeat)
            {
                labels[2] = $"Mask where depth difference between frames is more than {options.millimeters} mm's";
                int count = Cv2.CountNonZero(dst2);
                labels[3] = $"{count} pixels ({(double)count / Cv2.CountNonZero(task.depthMask):P0} of all depth pixels) were different by more than {options.millimeters} mm's";
            }
        }
    }

    public class CS_Diff_Depth32f : CS_Parent
    {
        public Mat lastDepth32f;
        Options_Depth options = new Options_Depth();

        public CS_Diff_Depth32f(VBtask task) : base(task)
        {
            lastDepth32f = dst0.Clone();
            desc = "Where is the depth difference between frames greater than X centimeters.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
                lastDepth32f = task.pcSplit[2].Clone();

            Cv2.Absdiff(task.pcSplit[2], lastDepth32f, dst1);
            var mm = GetMinMax(dst1);

            dst2 = dst1.Threshold(options.mmThreshold, 255, ThresholdTypes.Binary);

            lastDepth32f = task.pcSplit[2].Clone();
            if (task.heartBeat)
            {
                labels[2] = $"Mask where depth difference between frames is more than {options.mmThreshold} mm's";
                int count = Cv2.CountNonZero(dst2);
                labels[3] = $"{count} pixels ({(double)count / Cv2.CountNonZero(task.depthMask):P0} of all depth pixels) were different by more than {options.mmThreshold} mm's";
            }
        }
    }


    public class CS_Dilate_Basics : CS_Parent
    {
        public Options_Dilate options = new Options_Dilate();

        public CS_Dilate_Basics(VBtask task) : base(task)
        {
            desc = "Dilate the image provided.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (options.noshape || options.iterations == 0)
                dst2 = src;
            else
                dst2 = src.Dilate(options.element, null, options.iterations);

            if (standaloneTest())
            {
                dst3 = task.depthRGB.Dilate(options.element, null, options.iterations);
                labels[3] = $"Dilated Depth {options.iterations} times";
            }
            labels[2] = $"Dilated BGR {options.iterations} times";
        }
    }

    public class CS_Dilate_OpenClose : CS_Parent
    {
        Options_Dilate options = new Options_Dilate();

        public CS_Dilate_OpenClose(VBtask task) : base(task)
        {
            desc = "Erode and dilate with MorphologyEx on the BGR and Depth image.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            MorphTypes openClose = options.iterations > 0 ? MorphTypes.Open : MorphTypes.Close;
            Cv2.MorphologyEx(task.depthRGB, dst3, openClose, options.element);
            Cv2.MorphologyEx(src, dst2, openClose, options.element);
        }
    }

    public class CS_Dilate_Erode : CS_Parent
    {
        Options_Dilate options = new Options_Dilate();

        public CS_Dilate_Erode(VBtask task) : base(task)
        {
            desc = "Erode and dilate with MorphologyEx on the input image.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            Cv2.MorphologyEx(src, dst2, MorphTypes.Open, options.element);
            Cv2.MorphologyEx(dst2, dst2, MorphTypes.Close, options.element);
        }
    }


    public class CS_DisparityFunction_Basics : CS_Parent
    {
        FeatureLeftRight_Basics match = new FeatureLeftRight_Basics();
        string depthStr;
        string dispStr;

        public CS_DisparityFunction_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "AddWeighted output: lines show disparity between left and right images",
                                "Disparity as a function of depth" };
            desc = "Using FeatureMatch results build a function for disparity given depth";
        }

        public int DisparityFormula(float depth)
        {
            if (depth == 0) return 0;
            return (int)(task.baseline * 1000 * task.focalLength / depth);
        }

        public void RunCS(Mat src)
        {
            if (task.cameraName == "Azure Kinect 4K")
            {
                SetTrueText("Kinect for Azure does not have a left and right view to compute disparities", 2);
                return;
            }

            match.Run(src);
            dst2 = match.dst1;
            if (match.mpList.Count == 0) return; // no data...

            var disparity = new SortedDictionary<int, float>(new CompareAllowIdenticalIntegerInverted());
            for (int i = 0; i < match.mpList.Count; i++)
            {
                var mp = match.mpList[i];
                disparity.Add((int)(mp.p1.X - mp.p2.X), match.mpCorrelation[i]);
            }

            if (task.heartBeat)
            {
                dispStr = "Disparity: \n";
                depthStr = "Depth: \n";
                int index = 0;
                foreach (var entry in disparity)
                {
                    dispStr += $"{entry.Key}, ";
                    depthStr += $"{entry.Value:F3}, ";
                    index++;
                    if (index % 20 == 0)
                    {
                        dispStr += "\n";
                        depthStr += "\n";
                    }
                }

                int testIndex = Math.Min(disparity.Count - 1, 10);
                float actualDisparity = task.disparityAdjustment * disparity.ElementAt(testIndex).Key;
                float actualDepth = disparity.ElementAt(testIndex).Value;

                strOut = "Computing disparity from depth: disparity = ";
                strOut += "baseline * focal length / actual depth\n";
                strOut += "A disparity adjustment that is dependent on working resolution is used here \n";
                strOut += "to adjust the observed disparity to match the formula.\n";
                strOut += $"At working resolution = {task.WorkingRes.Width}x{task.WorkingRes.Height}";
                strOut += $" the adjustment factor is {task.disparityAdjustment:F3}\n\n";

                int disparityformulaoutput = DisparityFormula(actualDepth);
                strOut += $"At actual depth {actualDepth:F3}\n";

                strOut += $"Disparity formula is: {task.baseline:F3}";
                strOut += $" * {task.focalLength:F3} * 1000 / {actualDepth:F3}\n";

                strOut += $"Disparity formula:\t{disparityformulaoutput:F3} pixels\n";
                strOut += $"Disparity actual:\t\t{actualDisparity:F3} pixels\n";
                strOut += "Predicted disparity = baseline * focal length * 1000 / actual depth / disparityAdjustment\n";
                strOut += $"Predicted disparity at {actualDepth:F3}m = {(int)(disparityformulaoutput / task.disparityAdjustment)} pixels";
            }

            SetTrueText(depthStr + "\n\n" + dispStr, 3);
            SetTrueText(strOut, new cv.Point(0, dst2.Height / 3), 3);
        }
    }



    public class CS_Distance_Basics : CS_Parent
    {
        Options_Distance options = new Options_Distance();

        public CS_Distance_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Distance transform - create a mask with threshold", "" };
            UpdateAdvice(traceName + ": use local options to control which method is used.");
            desc = "Distance algorithm basics.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (standaloneTest()) src = task.depthRGB;
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            dst0 = src.DistanceTransform(options.distanceType, 0);
            dst1 = GetNormalize32f(dst0);
            dst1.ConvertTo(dst2, MatType.CV_8UC1);
        }
    }

    public class CS_Distance_Labels : CS_Parent
    {
        Options_Distance options = new Options_Distance();

        public CS_Distance_Labels(VBtask task) : base(task)
        {
            labels[2] = "Distance results";
            labels[3] = "Input mask to distance transform";
            desc = "Distance algorithm basics.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            if (standaloneTest()) src = task.depthRGB;
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            // Commented out code
            //Mat labels;
            //Cv2.DistanceTransformWithLabels(src, dst0, out labels, DistanceTypes.L2, DistanceTransformMasks.Precise);
            //Mat dist32f = dst0.Normalize(0, 255, NormTypes.MinMax);
            //dist32f.ConvertTo(src, MatType.CV_8UC1);
            //dst2 = src.CvtColor(ColorConversionCodes.GRAY2BGR);
        }
    }

    public class CS_Distance_Foreground : CS_Parent
    {
        Distance_Basics dist = new Distance_Basics();
        Foreground_KMeans2 foreground = new Foreground_KMeans2();
        public bool useBackgroundAsInput;

        public CS_Distance_Foreground(VBtask task) : base(task)
        {
            labels[2] = "Distance results";
            labels[3] = "Input mask to distance transform";
            desc = "Distance algorithm basics.";
        }

        public void RunCS(Mat src)
        {
            var cRadio = FindRadio("C");
            var l1Radio = FindRadio("L1");

            foreground.Run(src);
            dst3 = useBackgroundAsInput ? foreground.dst2 : foreground.dst3;

            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            DistanceTypes DistanceType = DistanceTypes.L2;
            if (cRadio.Checked) DistanceType = DistanceTypes.C;
            if (l1Radio.Checked) DistanceType = DistanceTypes.L1;

            src = dst3 & src;
            Mat dist = src.DistanceTransform(DistanceType, cv.DistanceTransformMasks.Precise);
            Mat dist32f = dist.Normalize(0, 255, NormTypes.MinMax);
            dist32f.ConvertTo(src, MatType.CV_8UC1);
            dst2 = src.CvtColor(ColorConversionCodes.GRAY2BGR);
        }
    }

    public class CS_Distance_Background : CS_Parent
    {
        Distance_Foreground dist = new Distance_Foreground();

        public CS_Distance_Background(VBtask task) : base(task)
        {
            dist.useBackgroundAsInput = true;
            desc = "Use distance algorithm on the background";
        }

        public void RunCS(Mat src)
        {
            dist.Run(src);
            dst2 = dist.dst2;
            dst3 = dist.dst3;
            labels[2] = dist.labels[2];
            labels[3] = dist.labels[3];
        }
    }

    public class CS_Distance_Point3D : CS_Parent
    {
        public Point3f inPoint1;
        public Point3f inPoint2;
        public float distance;

        public CS_Distance_Point3D(VBtask task) : base(task)
        {
            desc = "Compute the distance in meters between 3D points in the point cloud";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest() && task.heartBeat)
            {
                inPoint1 = new Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000));
                inPoint2 = new Point3f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, 10000));

                dst2.SetTo(0);
                cv.Point p1 = new cv.Point((int)inPoint1.X, (int)inPoint1.Y);
                cv.Point p2 = new cv.Point((int)inPoint2.X, (int)inPoint2.Y);
                Cv2.Line(dst2, p1, p2, task.HighlightColor, task.lineWidth);

                Point3f vec1 = task.pointCloud.Get<Point3f>(p1.Y, p1.X);
                Point3f vec2 = task.pointCloud.Get<Point3f>(p2.Y, p2.X);
            }

            float x = inPoint1.X - inPoint2.X;
            float y = inPoint1.Y - inPoint2.Y;
            float z = inPoint1.Z - inPoint2.Z;
            distance = (float)Math.Sqrt(x * x + y * y + z * z);

            string strOut = $"{inPoint1.X:F3}, {inPoint1.Y:F3}, {inPoint1.Z:F3}\n";
            strOut += $"{inPoint2.X:F3}, {inPoint2.Y:F3}, {inPoint2.Z:F3}\n";
            strOut += $"Distance = {distance:F3}";
            SetTrueText(strOut, 3);
        }
    }

    public class CS_Distance_Point4D : CS_Parent
    {
        public Vec4f inPoint1;
        public Vec4f inPoint2;
        public float distance;

        public CS_Distance_Point4D(VBtask task) : base(task)
        {
            desc = "Compute the distance between 4D points";
        }

        public void RunCS(Mat src)
        {
            if (standaloneTest())
            {
                inPoint1 = new Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                     msRNG.Next(0, (int)task.MaxZmeters), msRNG.Next(0, (int)task.MaxZmeters));
                inPoint2 = new Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height),
                                     msRNG.Next(0, (int)task.MaxZmeters), msRNG.Next(0, (int)task.MaxZmeters));
            }

            float x = inPoint1[0] - inPoint2[0];
            float y = inPoint1[1] - inPoint2[1];
            float z = inPoint1[2] - inPoint2[2];
            float d = inPoint1[3] - inPoint2[3];
            distance = (float)Math.Sqrt(x * x + y * y + z * z + d * d);

            string strOut = $"{inPoint1}\n{inPoint2}\nDistance = {distance:F1}";
            SetTrueText(strOut, new cv.Point(10, 10), 2);
        }
    }

    public class CS_Distance_RedCloud : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        Hist3Dcolor_Basics hColor = new Hist3Dcolor_Basics();
        public List<List<float>> pixelVector = new List<List<float>>();
        SortedList<double, int> distances = new SortedList<double, int>(new compareAllowIdenticalDoubleInverted());
        SortedList<double, int> lastDistances = new SortedList<double, int>(new compareAllowIdenticalDoubleInverted());
        List<rcData> lastredCells = new List<rcData>();

        public CS_Distance_RedCloud(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setDisplay1();
            task.redOptions.setUseColorOnly(true);
            task.redOptions.setHistBinBar3D(5);
            labels[1] = "3D Histogram distance for each of the cells at left";
            desc = "Identify RedCloud cells using the cell's 3D histogram distance from zero";
        }

        double distanceFromZero(List<float> histlist)
        {
            double result = 0;
            foreach (var d in histlist)
            {
                result += d * d;
            }
            return Math.Sqrt(result);
        }

        public void RunCS(Mat src)
        {
            redC.Run(src);

            pixelVector.Clear();
            distances.Clear();
            for (int i = 0; i < task.redCells.Count; i++)
            {
                var rc = task.redCells[i];
                hColor.inputMask = rc.mask;
                hColor.Run(src.SubMat(rc.rect));

                double nextD = distanceFromZero(hColor.histArray.ToList());
                distances.Add(nextD, i);
            }

            if (task.heartBeat)
            {
                string strOut = "3D histogram distances from zero for each cell\n";
                int index = 0;
                foreach (var el in distances)
                {
                    strOut += $"({el.Value}) {el.Key:F1}\t";
                    if (index % 6 == 5) strOut += "\n";
                    index++;

                    var rc = task.redCells[el.Value];
                    SetTrueText(el.Value.ToString(), rc.maxDist);
                }

                strOut += "----------------------\n";
                index = 0;
                foreach (var el in lastDistances)
                {
                    strOut += $"({el.Value}) {el.Key:F1}\t";
                    if (index % 6 == 5) strOut += "\n";
                    index++;
                    var rc = lastredCells[el.Value];
                    SetTrueText(el.Value.ToString(), new cv.Point(rc.maxDist.X, rc.maxDist.Y + 10));
                }

                foreach (var el in distances)
                {
                    var rc = task.redCells[el.Value];
                    SetTrueText(el.Value.ToString(), rc.maxDist);
                }
            }

            foreach (var el in lastDistances)
            {
                var rp = lastredCells[el.Value];
                SetTrueText(el.Value.ToString(), new cv.Point(rp.maxDist.X, rp.maxDist.Y + 10));
            }

            SetTrueText(strOut, 1);

            dst2.SetTo(0);
            dst3.SetTo(0);
            for (int i = 0; i < distances.Count; i++)
            {
                var rp = task.redCells[distances.ElementAt(i).Value];
                task.color.SubMat(rp.rect).CopyTo(dst2.SubMat(rp.rect), rp.mask);
                dst3.SubMat(rp.rect).SetTo(task.scalarColors[i], rp.mask);
            }
            labels[2] = redC.labels[3];

            lastDistances.Clear();
            foreach (var el in distances)
            {
                lastDistances.Add(el.Key, el.Value);
            }

            lastredCells = new List<rcData>(task.redCells);
        }
    }



    public class CS_Distance_BinaryImage : CS_Parent
    {
        Binarize_Simple binary = new Binarize_Simple();
        Distance_Basics distance = new Distance_Basics();

        public CS_Distance_BinaryImage(VBtask task) : base(task)
        {
            if (standalone)
            {
                task.gOptions.setDisplay1();
            }
            desc = "Measure the fragmentation of a binary image by using the distance transform";
        }

        public void RunCS(Mat src)
        {
            binary.Run(src);
            dst2 = binary.dst2;
            labels[2] = binary.labels[2] + " Draw a rectangle to measure specific area.";

            if (task.drawRect.Width > 0)
            {
                distance.Run(dst2[task.drawRect]);
            }
            else
            {
                distance.Run(dst2);
            }
            dst3 = distance.dst2;
            dst1 = dst3.Threshold(task.gOptions.DebugSliderValue, 255, ThresholdTypes.Binary);
        }
    }


    public class CS_Draw_Noise : CS_Parent
    {
        public int maxNoiseWidth = 3;
        public bool addRandomColor;
        public Mat noiseMask;
        Options_DrawNoise options = new Options_DrawNoise();    
        public CS_Draw_Noise(VBtask task) : base(task)
        {
            desc = "Add Noise to the color image";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            src.CopyTo(dst2);
            noiseMask = new Mat(src.Size(), MatType.CV_8UC1, Scalar.Black);
            for (int n = 0; n < options.noiseCount; n++)
            {
                int i = msRNG.Next(0, src.Cols - 1);
                int j = msRNG.Next(0, src.Rows - 1);
                Point2f center = new Point2f(i, j);
                Scalar c = addRandomColor ? new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255)) : Scalar.Black;
                int noiseWidth = msRNG.Next(1, options.noiseWidth);
                DrawCircle(dst2, center, noiseWidth, c, -1);
                DrawCircle(noiseMask, center, noiseWidth, Scalar.White, -1);
            }
        }
    }
    public class CS_Draw_Ellipses : CS_Parent
    {
        Options_Draw options = new Options_Draw();
        public CS_Draw_Ellipses(VBtask task) : base(task)
        {
            desc = "Draw the requested number of ellipses.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.heartBeat)
            {
                dst2.SetTo(Scalar.Black);
                for (int i = 0; i < options.drawCount; i++)
                {
                    Point2f nPoint = new Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4));
                    Size2f eSize = new Size2f((float)msRNG.Next(0, src.Cols - (int)nPoint.X - 1), (float)msRNG.Next(0, src.Rows - (int)nPoint.Y - 1));
                    float angle = 180.0f * (float)msRNG.Next(0, 1000) / 1000.0f;
                    Scalar nextColor = new Scalar(task.vecColors[i][0], task.vecColors[i][1], task.vecColors[i][2]);
                    Cv2.Ellipse(dst2, new RotatedRect(nPoint, eSize, angle), nextColor, options.drawFilled);
                }
            }
        }
    }
    public class CS_Draw_Circles : CS_Parent
    {
        Options_Draw options = new Options_Draw();
        public CS_Draw_Circles(VBtask task) : base(task)
        {
            desc = "Draw the requested number of circles.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.heartBeat)
            {
                dst2.SetTo(Scalar.Black);
                for (int i = 0; i < options.drawCount; i++)
                {
                    Point2f nPoint = new Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4));
                    int radius = msRNG.Next(10, 10 + msRNG.Next(src.Cols / 4));
                    Scalar nextColor = new Scalar(task.vecColors[i][0], task.vecColors[i][1], task.vecColors[i][2]);
                    DrawCircle(dst2, nPoint, radius, nextColor, options.drawFilled);
                }
            }
        }
    }
    public class CS_Draw_Lines : CS_Parent
    {
        readonly Options_Draw options = new Options_Draw();
        public CS_Draw_Lines(VBtask task) : base(task)
        {
            desc = "Draw the requested number of Lines.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.heartBeat)
            {
                dst2.SetTo(Scalar.Black);
                for (int i = 0; i < options.drawCount; i++)
                {
                    Point2f nPoint1 = new Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4));
                    Point2f nPoint2 = new Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4));
                    Scalar nextColor = new Scalar(task.vecColors[i][0], task.vecColors[i][1], task.vecColors[i][2]);
                    DrawLine(dst2, nPoint1, nPoint2, nextColor, options.drawFilled);
                }
            }
        }
    }
    public class CS_Draw_Polygon : CS_Parent
    {
        readonly Options_Draw options = new Options_Draw();
        public CS_Draw_Polygon(VBtask task) : base(task)
        {
            desc = "Draw Polygon figures";
            labels = new string[] { "", "", "Convex Hull for the same points", "Polylines output" };
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (!task.heartBeat) return;
            int height = src.Height / 8;
            int width = src.Width / 8;
            Scalar polyColor = new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255));
            dst3.SetTo(Scalar.Black);
            for (int i = 0; i < options.drawCount; i++)
            {
                List<cv.Point> points = new List<cv.Point>();
                List<List<cv.Point>> listOfPoints = new List<List<cv.Point>>();
                for (int j = 0; j < 11; j++)
                {
                    points.Add(new cv.Point(msRNG.Next(width, width * 7), msRNG.Next(height, height * 7)));
                }
                listOfPoints.Add(points);
                if (options.drawFilled != -1)
                {
                    Cv2.Polylines(dst3, listOfPoints, true, polyColor, task.lineWidth + 1, task.lineType);
                }
                else
                {
                    Cv2.FillPoly(dst3, listOfPoints, new Scalar(0, 0, 255));
                }
                cv.Point[] hull = Cv2.ConvexHull(points, true);
                listOfPoints = new List<List<cv.Point>>();
                points = new List<cv.Point>();
                for (int j = 0; j < hull.Length; j++)
                {
                    points.Add(new cv.Point(hull[j].X, hull[j].Y));
                }
                listOfPoints.Add(points);
                dst2.SetTo(Scalar.Black);
                Cv2.DrawContours(dst2, listOfPoints, 0, polyColor, options.drawFilled);
            }
        }
    }
    public class CS_Draw_Shapes : CS_Parent
    {
        public CS_Draw_Shapes(VBtask task) : base(task)
        {
            desc = "Use RNG to draw the same set of shapes every time";
        }
        public void RunCS(Mat src)
        {
            int offsetX = 25, offsetY = 25, lineLength = 25, thickness = 2;
            dst2.SetTo(0);
            for (int i = 1; i <= 256; i++)
            {
                cv.Point p1 = new cv.Point(thickness * i + offsetX, offsetY);
                cv.Point p2 = new cv.Point(thickness * i + offsetX, offsetY + lineLength);
                Cv2.Line(dst2, p1, p2, new Scalar(i, i, i), thickness);
            }
            for (int i = 1; i <= 256; i++)
            {
                Scalar color = new Scalar(msRNG.Next(0, 255), msRNG.Next(0, 255), msRNG.Next(0, 255));
                switch (msRNG.Next(0, 3))
                {
                    case 0: // circle
                        cv.Point center = new cv.Point(msRNG.Next(offsetX, dst2.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst2.Rows - offsetY));
                        int radius = msRNG.Next(1, Math.Min(offsetX, offsetY));
                        Cv2.Circle(dst2, center, radius, color, -1);
                        break;
                    case 1: // Rectangle
                        center = new cv.Point(msRNG.Next(offsetX, dst2.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst2.Rows - offsetY));
                        int width = msRNG.Next(1, Math.Min(offsetX, offsetY));
                        int height = msRNG.Next(1, Math.Min(offsetX, offsetY));
                        Rect rcenter = new Rect(center.X - width, center.Y - height / 2, width, height);
                        Cv2.Rectangle(dst2, rcenter, color, -1, LineTypes.Link8);
                        break;
                    case 2: // Ellipse
                        center = new cv.Point(msRNG.Next(offsetX, dst2.Cols - offsetX), msRNG.Next(offsetY + lineLength, dst2.Rows - offsetY));
                        width = msRNG.Next(1, Math.Min(offsetX, offsetY));
                        height = msRNG.Next(1, Math.Min(offsetX, offsetY));
                        int angle = msRNG.Next(0, 180);
                        Cv2.Ellipse(dst2, center, new cv.Size(width / 2, height / 2), angle, 0, 360, color, -1, LineTypes.Link8);
                        break;
                }
            }
        }
    }
    public class CS_Draw_SymmetricalShapes : CS_Parent
    {
        Options_SymmetricalShapes options = new Options_SymmetricalShapes();
        public CS_Draw_SymmetricalShapes(VBtask task) : base(task)
        {
            desc = "Generate shapes programmatically";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.heartBeat)
            {
                dst2.SetTo(Scalar.Black);
                cv.Point pt = new cv.Point();
                cv.Point center = new cv.Point(src.Width / 2, src.Height / 2);
                List<cv.Point> points = new List<cv.Point>();
                for (int i = 0; i < options.numPoints; i++)
                {
                    double theta = i * options.dTheta;
                    double ripple = options.radius2 * Math.Cos(options.nGenPer * theta);
                    if (!options.symmetricRipple) ripple = Math.Abs(ripple);
                    if (options.reverseInOut) ripple = -ripple;
                    pt.X = (int)(center.X + (options.radius1 + ripple) * Math.Cos(theta + options.rotateAngle) + 0.5);
                    pt.Y = (int)(center.Y - (options.radius1 + ripple) * Math.Sin(theta + options.rotateAngle) + 0.5);
                    points.Add(pt);
                }
                for (int i = 0; i < options.numPoints; i++)
                {
                    cv.Point p1 = points[i];
                    cv.Point p2 = points[(i + 1) % options.numPoints];
                    Cv2.Line(dst2, p1, p2, task.scalarColors[i % task.scalarColors.Count()], task.lineWidth + 1, task.lineType);
                }
                if (options.fillRequest) Cv2.FloodFill(dst2, center, options.fillColor);
            }
        }
    }
    public class CS_Draw_Arc : CS_Parent
    {
        readonly Kalman_Basics kalman = new Kalman_Basics();
        Rect rect;
        float angle;
        float startAngle;
        float endAngle;
        int colorIndex;
        int thickness;
        Options_DrawArc options = new Options_DrawArc();
        public CS_Draw_Arc(VBtask task) : base(task)
        {
            desc = "Use OpenCV's ellipse function to draw an arc";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.heartBeat)
            {
                rect = InitRandomRect(options.saveMargin);
                angle = msRNG.Next(0, 360);
                colorIndex = msRNG.Next(0, 255);
                thickness = msRNG.Next(1, 5);
                startAngle = msRNG.Next(1, 360);
                endAngle = msRNG.Next(1, 360);
                kalman.kInput = new[] { rect.X, rect.Y, rect.Width, rect.Height, angle, startAngle, endAngle };
            }
            kalman.kInput = new[] { rect.X, rect.Y, rect.Width, rect.Height, angle, startAngle, endAngle };
            kalman.Run(src);
            Rect r = new Rect((int)kalman.kOutput[0], (int)kalman.kOutput[1], (int)kalman.kOutput[2], (int)kalman.kOutput[3]);
            if (r.Width <= 5) r.Width = 5;
            if (r.Height <= 5) r.Height = 5;
            RotatedRect rr = new RotatedRect(new Point2f(r.X, r.Y), new Size2f(r.Width, r.Height), angle);
            Scalar color = task.scalarColors[colorIndex];
            dst2.SetTo(Scalar.White);
            if (options.drawFull)
            {
                Cv2.Ellipse(dst2, rr, color, thickness, task.lineType);
                DrawRotatedOutline(rr, dst2, task.scalarColors[colorIndex]);
            }
            else
            {
                angle = kalman.kOutput[4];
                startAngle = kalman.kOutput[5];
                endAngle = kalman.kOutput[6];
                if (options.drawFill) thickness = -1;
                Rect r1 = rr.BoundingRect();
                Cv2.Ellipse(dst2, new cv.Point(rr.Center.X, rr.Center.Y), new cv.Size(r1.Width, r1.Height),
                            angle, startAngle, endAngle, color, thickness, task.lineType);  
            }
        }
    }
    public class CS_Draw_ClipLine : CS_Parent
    {
        Font_FlowText flow = new Font_FlowText();
        Kalman_Basics kalman = new Kalman_Basics();
        cv.Point pt1;
        cv.Point pt2;
        Rect rect;
        int linenum = 0;
        int hitCount = 0;
        void setup()
        {
            kalman.kInput = new float[9];
            Rect r = InitRandomRect(25);
            pt1 = new cv.Point(r.X, r.Y);
            pt2 = new cv.Point(r.X + r.Width, r.Y + r.Height);
            rect = InitRandomRect(25);
            if (task.gOptions.GetUseKalman()) flow.msgs.Add("--------------------------- setup ---------------------------");
        }
        public CS_Draw_ClipLine(VBtask task) : base(task)
        {
            setup();
            desc = "Demonstrate the use of the ClipLine function in OpenCV. NOTE: when clipline returns true, p1/p2 are clipped by the rectangle";
        }
        public void RunCS(Mat src)
        {
            dst3 = src;
            kalman.kInput = new float[] { pt1.X, pt1.Y, pt2.X, pt2.Y, rect.X, rect.Y, rect.Width, rect.Height };
            kalman.Run(src);
            cv.Point p1 = new cv.Point((int)kalman.kOutput[0], (int)kalman.kOutput[1]);
            cv.Point p2 = new cv.Point((int)kalman.kOutput[2], (int)kalman.kOutput[3]);
            if (kalman.kOutput[6] < 5) kalman.kOutput[6] = 5; // don't let the width/height get too small...
            if (kalman.kOutput[7] < 5) kalman.kOutput[7] = 5;
            Rect r = new Rect((int)kalman.kOutput[4], (int)kalman.kOutput[5], (int)kalman.kOutput[6], (int)kalman.kOutput[7]);
            bool clipped = Cv2.ClipLine(r, ref p1, ref p2); // Returns false when the line and the rectangle don't intersect.
            Cv2.Line(dst3, p1, p2, clipped ? Scalar.White : Scalar.Black, task.lineWidth + 1, task.lineType);
            Cv2.Rectangle(dst3, r, clipped ? Scalar.Yellow : Scalar.Red, task.lineWidth + 1, task.lineType);
            flow.msgs.Add($"({linenum}) line {(clipped ? "intersects rectangle" : "does not intersect rectangle")}");
            linenum++;
            hitCount += clipped ? 1 : 0;
            SetTrueText($"There were {hitCount:###,##0} intersects and {linenum - hitCount} misses",
                         new cv.Point(src.Width / 2, 200));
            if (r == rect) setup();
            flow.Run(empty);
        }
    }
    //public class CS_Draw_Hexagon : CS_Parent
    //{
    //    ImageForm alpha = new ImageForm();
    //    public CS_Draw_Hexagon(VBtask task) : base(task)
    //    {
    //        alpha.ImagePic.Image = Image.(task.HomeDir + "Data/GestaltCube.gif");
    //        alpha.Show();
    //        alpha.Size = new System.Drawing.Size(512, 512);
    //        alpha.Text = "Perception is the key";
    //        desc = "What it means to recognize a cube.  Zygmunt Pizlo - UC Irvine";
    //    }
    //    public void RunCS(Mat src)
    //    {
    //    }
    //}
    public class CS_Draw_Line : CS_Parent
    {
        public cv.Point p1, p2;
        public bool externalUse;
        public CS_Draw_Line(VBtask task) : base(task)
        {
            desc = "Draw a line between the selected p1 and p2 - either by clicking twice in the image or externally providing p1 and p2.";
        }
        public void RunCS(Mat src)
        {
            if (task.FirstPass) task.ClickPoint = new cv.Point();
            if (p1 != new cv.Point() && p2 != new cv.Point() && task.ClickPoint != new cv.Point())
            {
                p1 = new cv.Point();
                p2 = new cv.Point();
            }
            dst2 = src;
            if (task.ClickPoint != new cv.Point() || externalUse)
            {
                if (p1 == new cv.Point()) p1 = task.ClickPoint; else p2 = task.ClickPoint;
            }
            if (p1 != new cv.Point() && p2 == new cv.Point()) Cv2.Circle(dst2, p1, task.DotSize, task.HighlightColor);
            if (p1 != new cv.Point() && p2 != new cv.Point())
            {
                Cv2.Line(dst2, p1, p2, task.HighlightColor);
            }
            SetTrueText("Click twice in the image to provide the points below and they will be connected with a line\n" +
                        "P1 = " + p1.ToString() + "\nP2 = " + p2.ToString(), 3);
            task.ClickPoint = new cv.Point();
        }
    }
    public class CS_Draw_LineTest : CS_Parent
    {
        readonly Draw_Line line = new Draw_Line();
        public CS_Draw_LineTest(VBtask task) : base(task)
        {
            desc = "Test the external use of the Draw_Line algorithm - provide 2 points and draw the line...";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                line.p1 = new cv.Point(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height));
                line.p2 = new cv.Point(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height));
            }
            line.Run(src);
            dst2 = line.dst2;
        }
    }
    public class CS_Draw_Frustrum : CS_Parent
    {
        public Depth_WorldXYZ xyzDepth = new Depth_WorldXYZ();
        public CS_Draw_Frustrum(VBtask task) : base(task)
        {
            xyzDepth.depthUnitsMeters = true;
            labels[3] = "Frustrum 3D pointcloud";
            desc = "Draw a frustrum for a camera viewport";
        }
        public void RunCS(Mat src)
        {
            src = new Mat(task.WorkingRes, MatType.CV_32F, 0);
            int mid = src.Height / 2;
            float zIncr = task.MaxZmeters / mid;
            dst2 = src.Clone();
            Rect fRect = new Rect((src.Width - src.Height) / 2, 0, src.Height, src.Height);
            for (int i = 0; i <= src.Height / 2; i++)
            {
                Cv2.Rectangle(dst2[fRect], new Rect(mid - i, mid - i, i * 2, (i + 1) * 2), i * zIncr, 1);
            }
            xyzDepth.Run(dst2);
            dst3 = xyzDepth.dst2.Resize(task.WorkingRes);
        }
    }

    public class CS_Duster_Basics : CS_Parent
    {
        public Duster_MaskZ dust = new Duster_MaskZ();
        public CS_Duster_Basics(VBtask task) : base(task)
        {
            desc = "Removed blowback in the pointcloud";
        }
        public void RunCS(Mat src)
        {
            dust.Run(src);
            for (int i = 1; i <= dust.classCount; i++)
            {
                Mat mask = dust.dst2.InRange(i, i);
                Scalar depth = task.pcSplit[2].Mean(mask);
                task.pcSplit[2].SetTo(depth[0], mask);
            }
            Cv2.Merge(task.pcSplit, dst2);
            dst2.SetTo(0, ~dust.dst0);
            dst2.SetTo(0, task.maxDepthMask);
            dst3 = dust.dst3;
        }
    }
    public class CS_Duster_MaskZ : CS_Parent
    {
        public Hist_Basics hist = new Hist_Basics();
        public int classCount;
        public Options_GuidedBPDepth options = new Options_GuidedBPDepth();
        public CS_Duster_MaskZ(VBtask task) : base(task)
        {
            labels[3] = "Any flickering below is from changes in the sorted order of the clusters.  It should not be a problem.";
            desc = "Build a histogram that finds the clusters of depth data";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            hist.bins = options.bins;
            Mat src32f = task.pcSplit[2];
            task.maxDepthMask = src32f.InRange(task.MaxZmeters, task.MaxZmeters).ConvertScaleAbs();
            src32f.SetTo(task.MaxZmeters, task.maxDepthMask);
            hist.fixedRanges = new[] { new Rangef(0.001f, task.MaxZmeters) };
            hist.Run(src32f);
            List<float> histArray = hist.histArray.ToList();
            // This ensures that the maxDepthMask is separate from any previous cluster
            histArray[histArray.Count - 1] = 0;
            int start = 0;
            SortedList<int, Vec2i> clusters = new SortedList<int, Vec2i>(new compareAllowIdenticalIntegerInverted());
            float lastEntry = 0;
            int sampleCount = 0;
            for (int i = 0; i < histArray.Count; i++)
            {
                if (histArray[i] > 0 && lastEntry == 0) start = i;
                if (histArray[i] == 0 && lastEntry > 0)
                {
                    clusters.Add(sampleCount, new Vec2i(start, i));
                    sampleCount = 0;
                }
                lastEntry = histArray[i];
                sampleCount += (int)histArray[i];
            }
            float incr = task.MaxZmeters / options.bins;
            classCount = 0;
            for (int i = 0; i < Math.Min(clusters.Count, options.maxClusters); i++)
            {
                Vec2i vec = clusters.ElementAt(i).Value;
                classCount++;
                for (int j = vec[0]; j <= vec[1]; j++)
                {
                    histArray[j] = classCount;
                }
            }
            Marshal.Copy(histArray.ToArray(), 0, hist.histogram.Data, histArray.Count);
            Cv2.CalcBackProject(new[] { src32f }, new[] { 0 }, hist.histogram, dst1, hist.ranges);
            dst1.ConvertTo(dst2, MatType.CV_8U);
            classCount++;
            dst2.SetTo(classCount, task.maxDepthMask);
            dst3 = ShowPalette(dst2 * 255 / classCount);
            if (task.heartBeat) labels[2] = $"dst2 = CV_8U version of depth segmented into {classCount} clusters.";
            dst0 = dst2.Threshold(0, 255, ThresholdTypes.Binary);
        }
    }
    public class CS_Duster_BasicsY : CS_Parent
    {
        Duster_MaskZ dust = new Duster_MaskZ();
        public CS_Duster_BasicsY(VBtask task) : base(task)
        {
            desc = "Removed blowback in the pointcloud";
        }
        public void RunCS(Mat src)
        {
            dust.Run(src);
            for (int i = 1; i <= dust.classCount; i++)
            {
                Mat mask = dust.dst2.InRange(i, i);
                Scalar pcY = task.pcSplit[1].Mean(mask);
                task.pcSplit[1].SetTo(pcY[0], mask);
            }
            Cv2.Merge(task.pcSplit, dst2);
            dst2.SetTo(0, ~dust.dst0);
            dst2.SetTo(0, task.maxDepthMask);
            dst3 = dust.dst3;
        }
    }
    public class CS_Duster_RedCloud : CS_Parent
    {
        Duster_Basics duster = new Duster_Basics();
        RedCloud_Basics redC = new RedCloud_Basics();
        public CS_Duster_RedCloud(VBtask task) : base(task)
        {
            desc = "Run Bin3Way_RedCloud on the largest regions identified in Duster_Basics";
        }
        public void RunCS(Mat src)
        {
            duster.Run(src);
            dst1 = duster.dust.dst2.InRange(1, 1);
            dst3.SetTo(0);
            src.CopyTo(dst3, dst1);
            redC.inputMask = ~dst1;
            redC.Run(dst3);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
        }
    }


    public class CS_Edge_All : CS_Parent
    {
        Options_Edges_All options = new Options_Edges_All();
        public CS_Edge_All(VBtask task) : base(task)
        {
            desc = "Use Radio Buttons to select the different edge algorithms.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            options.RunEdges(src);
            dst2 = options.dst2.Channels() == 1 ? options.dst2 : options.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            labels[2] = traceName + " - selection = " + options.edgeSelection;
        }
    }
    public class CS_Edge_DepthAndColor : CS_Parent
    {
        Depth_Holes shadow = new Depth_Holes();
        Edge_Canny canny = new Edge_Canny();
        Dilate_Basics dilate = new Dilate_Basics();
        public CS_Edge_DepthAndColor(VBtask task) : base(task)
        {
            FindRadio("Dilate shape: Rect").Checked = true;
            FindSlider("Canny threshold1").Value = 100;
            FindSlider("Canny threshold2").Value = 100;
            desc = "Find all the edges in an image include Canny from the grayscale image and edges of depth shadow.";
            labels[2] = "Edges in color and depth after dilate";
            labels[3] = "Edges in color and depth no dilate";
        }
        public void RunCS(Mat src)
        {
            canny.Run(src);
            shadow.Run(src);
            dst3 = shadow.dst3.Channels() != 1 ? shadow.dst3.CvtColor(ColorConversionCodes.BGR2GRAY) : shadow.dst3;
            dst3 += canny.dst2.Threshold(1, 255, ThresholdTypes.Binary);
            dilate.Run(dst3);
            dilate.dst2.SetTo(0, shadow.dst2);
            dst2 = dilate.dst2;
        }
    }
    public class CS_Edge_Scharr : CS_Parent
    {
        Options_Edges options = new Options_Edges();
        public CS_Edge_Scharr(VBtask task) : base(task)
        {
            labels[3] = "x field + y field in CV_32F format";
            desc = "Scharr is most accurate with 3x3 kernel.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat xField = gray.Scharr(MatType.CV_32FC1, 1, 0);
            Mat yField = gray.Scharr(MatType.CV_32FC1, 0, 1);
            Cv2.Add(xField, yField, dst3);
            dst3.ConvertTo(dst2, MatType.CV_8U, options.scharrMultiplier);
        }
    }
    public class CS_Edge_Preserving : CS_Parent
    {
        Options_Edges options = new Options_Edges();
        public CS_Edge_Preserving(VBtask task) : base(task)
        {
            labels[3] = "Edge preserving blur for BGR depth image above";
            desc = "OpenCV's edge preserving filter.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.recurseCheck)
            {
                Cv2.EdgePreservingFilter(src, dst2, EdgePreservingMethods.RecursFilter, options.EP_Sigma_s, options.EP_Sigma_r);
            }
            else
            {
                Cv2.EdgePreservingFilter(src, dst2, EdgePreservingMethods.NormconvFilter, options.EP_Sigma_s, options.EP_Sigma_r);
            }
            if (options.recurseCheck)
            {
                Cv2.EdgePreservingFilter(task.depthRGB, dst3, EdgePreservingMethods.RecursFilter, options.EP_Sigma_s, options.EP_Sigma_r);
            }
            else
            {
                Cv2.EdgePreservingFilter(task.depthRGB, dst3, EdgePreservingMethods.NormconvFilter, options.EP_Sigma_s, options.EP_Sigma_r);
            }
        }
    }
    public class CS_Edge_RandomForest_CPP : CS_Parent
    {
        byte[] rgbData;
        Options_Edges2 options = new Options_Edges2();
        public CS_Edge_RandomForest_CPP(VBtask task) : base(task)
        {
            desc = "Detect edges using structured forests - Opencv Contrib";
            rgbData = new byte[dst2.Total() * dst2.ElemSize()];
            labels[3] = "Thresholded Edge Mask (use slider to adjust)";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.frameCount < 100)
                SetTrueText("On the first call only, it takes a few seconds to load the randomForest model.", new cv.Point(10, 100));
            if (task.frameCount == 5)
            {
                FileInfo modelInfo = new FileInfo(Path.Combine(task.HomeDir, "Data/model.yml.gz"));
                cPtr = Edge_RandomForest_Open(modelInfo.FullName);
            }
            if (task.frameCount > 5)
            {
                Marshal.Copy(src.Data, rgbData, 0, rgbData.Length);
                GCHandle handleRGB = GCHandle.Alloc(rgbData, GCHandleType.Pinned);
                IntPtr imagePtr = Edge_RandomForest_Run(cPtr, handleRGB.AddrOfPinnedObject(), src.Rows, src.Cols);
                handleRGB.Free();
                dst3 = new Mat(src.Rows, src.Cols, MatType.CV_8U, imagePtr).Threshold(options.edgeRFthreshold, 255, ThresholdTypes.Binary);
            }
        }
        public void Close()
        {
            if (cPtr != IntPtr.Zero)
                cPtr = Edge_RandomForest_Close(cPtr);
        }
    }
    public class CS_Edge_DCTfrequency : CS_Parent
    {
        Options_Edges2 options = new Options_Edges2();
        public CS_Edge_DCTfrequency(VBtask task) : base(task)
        {
            labels[3] = "Mask for the isolated frequencies";
            desc = "Find edges by removing all the highest frequencies.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Mat gray = task.depthRGB.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat frequencies = new Mat();
            Mat src32f = new Mat();
            gray.ConvertTo(src32f, MatType.CV_32F, 1.0 / 255);
            Cv2.Dct(src32f, frequencies, DctFlags.None);
            Rect roi = new Rect(0, 0, options.removeFrequencies, src32f.Height);
            if (roi.Width > 0)
                frequencies.SubMat(roi).SetTo(0);
            labels[2] = $"Highest {options.removeFrequencies} frequencies removed from RGBDepth";
            Cv2.Dct(frequencies, src32f, DctFlags.Inverse);
            src32f.ConvertTo(dst2, MatType.CV_8UC1, 255);
            dst3 = dst2.Threshold(options.dctThreshold, 255, ThresholdTypes.Binary);
        }
    }
    public class CS_Edge_Deriche_CPP : CS_Parent
    {
        Options_Edges3 options = new Options_Edges3();
        public CS_Edge_Deriche_CPP(VBtask task) : base(task)
        {
            cPtr = Edge_Deriche_Open();
            labels[3] = "Image enhanced with Deriche results";
            desc = "Edge detection using the Deriche X and Y gradients";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            byte[] dataSrc = new byte[src.Total() * src.ElemSize()];
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
            GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
            IntPtr imagePtr = Edge_Deriche_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.alpha, options.omega);
            handleSrc.Free();
            if (imagePtr != IntPtr.Zero)
                dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, imagePtr).Clone();
            dst3 = src | dst2;
        }
        public void Close()
        {
            if (cPtr != IntPtr.Zero)
                cPtr = Edge_Deriche_Close(cPtr);
        }
    }
    public class CS_Edge_DCTinput : CS_Parent
    {
        Edge_Canny edges = new Edge_Canny();
        DCT_FeatureLess dct = new DCT_FeatureLess();
        public CS_Edge_DCTinput(VBtask task) : base(task)
        {
            labels[2] = "Canny edges produced from original grayscale image";
            labels[3] = "Edges produced with featureless regions cleared";
            desc = "Use the featureless regions to enhance the edge detection";
        }
        public void RunCS(Mat src)
        {
            edges.Run(src);
            dst2 = edges.dst2.Clone();
            dct.Run(src);
            Mat tmp = src.SetTo(Scalar.White, dct.dst2);
            edges.Run(tmp);
            dst3 = edges.dst2;
        }
    }

    public class CS_EdgeDraw_Basics : CS_Parent
    {
        public CS_EdgeDraw_Basics(VBtask task) : base(task)
        {
            cPtr = EdgeDraw_Edges_Open();
            labels = new string[] { "", "", "CS_EdgeDraw_Basics output", "" };
            desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            byte[] cppData = new byte[src.Total()];
            Marshal.Copy(src.Data, cppData, 0, cppData.Length);
            GCHandle handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned);
            IntPtr imagePtr = EdgeDraw_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth);
            handleSrc.Free();
            if (imagePtr != IntPtr.Zero)
                dst2 = new Mat(src.Rows, src.Cols, MatType.CV_8UC1, imagePtr);
            Cv2.Rectangle(dst2, new Rect(0, 0, dst2.Width, dst2.Height), new Scalar(255), task.lineWidth);
        }
        public void Close()
        {
            EdgeDraw_Edges_Close(cPtr);
        }
    }
    public class CS_EdgeDraw_Segments : CS_Parent
    {
        public List<Point2f> segPoints = new List<Point2f>();
        public CS_EdgeDraw_Segments(VBtask task) : base(task)
        {
            cPtr = EdgeDraw_Lines_Open();
            labels = new string[] { "", "", "CS_EdgeDraw_Segments output", "" };
            dst2 = new Mat(dst2.Size(), MatType.CV_8U, new Scalar(0));
            dst3 = new Mat(dst2.Size(), MatType.CV_8U, new Scalar(0));
            desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient";
        }
        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            byte[] cppData = new byte[src.Total()];
            Marshal.Copy(src.Data, cppData, 0, cppData.Length);
            GCHandle handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned);
            IntPtr vecPtr = EdgeDraw_Lines_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth);
            handleSrc.Free();
            Mat ptData = new Mat(EdgeDraw_Lines_Count(cPtr), 2, MatType.CV_32FC2, vecPtr).Clone();
            dst2.SetTo(new Scalar(0));
            if (task.heartBeat)
                dst3.SetTo(new Scalar(0));
            segPoints.Clear();
            for (int i = 0; i < ptData.Rows; i += 2)
            {
                Point2f pt1 = ptData.Get<Point2f>(i, 0);
                Point2f pt2 = ptData.Get<Point2f>(i, 1);
                DrawLine(dst2, pt1, pt2, Scalar.White, task.lineWidth);
                Cv2.Add(dst3, dst2, dst3);
                segPoints.Add(pt1);
                segPoints.Add(pt2);
            }
        }
        public void Close()
        {
            EdgeDraw_Lines_Close(cPtr);
        }
    }



    public class CS_Eigen_Basics : CS_Parent
    {
        public CS_Eigen_Basics(VBtask task) : base(task)
        {
            desc = "Solve system of equations using OpenCV's EigenVV";
            labels[2] = "EigenVec (solution)";
            labels[3] = "Relationship between Eigen Vec and Vals";
        }
        public void RunCS(Mat src)
        {
            double[] a = { 1.96, -6.49, -0.47, -7.2, -0.65,
                       -6.49, 3.8, -6.39, 1.5, -6.34,
                       -0.47, -6.39, 4.17, -1.51, 2.67,
                       -7.2, 1.5, -1.51, 5.7, 1.8,
                       -0.65, -6.34, 2.67, 1.8, -7.1 };
            Mat mat = new Mat(5, 5, MatType.CV_64FC1, a);
            Mat eigenVal = new Mat();
            Mat eigenVec = new Mat();
            Cv2.Eigen(mat, eigenVal, eigenVec);
            double[] solution = new double[mat.Cols];
            string nextLine = "Eigen Vals\tEigen Vectors\t\t\t\t\tOriginal Matrix\n\n";
            Scalar scalar;
            for (int i = 0; i < eigenVal.Rows; i++)
            {
                scalar = eigenVal.Get<Scalar>(0, i);
                solution[i] = scalar.Val0;
                nextLine += string.Format("{0:F2}\t\t", scalar.Val0);
                for (int j = 0; j < eigenVec.Rows; j++)
                {
                    scalar = eigenVec.Get<Scalar>(i, j);
                    nextLine += string.Format("{0:F2}\t", scalar.Val0);
                }
                for (int j = 0; j < eigenVec.Rows; j++)
                {
                    nextLine += string.Format("\t{0:F2}", a[i * 5 + j]);
                }
                nextLine += "\n\n";
            }
            for (int i = 0; i < eigenVec.Rows; i++)
            {
                string plusSign = " + ";
                for (int j = 0; j < eigenVec.Cols; j++)
                {
                    scalar = eigenVec.Get<Scalar>(i, j);
                    if (j == eigenVec.Cols - 1) plusSign = "\t";
                    nextLine += string.Format("{0:F2} * {1:F2}{2}", scalar.Val0, solution[j], plusSign);
                }
                nextLine += " = \t0.0\n";
            }
            SetTrueText(nextLine);
        }
    }
    public class CS_Eigen_FitLineInput : CS_Parent
    {
        public List<Point2f> points = new List<Point2f>();
        public float m;
        public float bb;
        public Options_Eigen options = new Options_Eigen();
        public CS_Eigen_FitLineInput(VBtask task) : base(task)
        {
            labels[2] = "Use sliders to adjust the width and intensity of the line";
            desc = "Generate a noisy line in a field of random data.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.heartBeat)
            {
                if (!task.testAllRunning) options.recompute = false;
                dst2.SetTo(0);
                int width = src.Width;
                int height = src.Height;
                points.Clear();
                Random rand = new Random();
                for (int i = 0; i < options.randomCount; i++)
                {
                    Point2f pt = new Point2f((float)(rand.NextDouble() * width), (float)(rand.NextDouble() * height));
                    pt.X = Math.Max(0, Math.Min(pt.X, width));
                    pt.Y = Math.Max(0, Math.Min(pt.Y, height));
                    points.Add(pt);
                    Cv2.Circle(dst2, (cv.Point)points[i], task.DotSize, Scalar.White, -1);
                }
                Point2f p1, p2;
                if (rand.NextDouble() * 2 - 1 >= 0)
                {
                    p1 = new Point2f((float)(rand.NextDouble() * width), 0);
                    p2 = new Point2f((float)(rand.NextDouble() * width), height);
                }
                else
                {
                    p1 = new Point2f(0, (float)(rand.NextDouble() * height));
                    p2 = new Point2f(width, (float)(rand.NextDouble() * height));
                }
                if (p1.X == p2.X) p1.X += 1;
                if (p1.Y == p2.Y) p1.Y += 1;
                m = (p2.Y - p1.Y) / (p2.X - p1.X);
                bb = p2.Y - p2.X * m;
                float startx = Math.Min(p1.X, p2.X);
                float incr = (Math.Max(p1.X, p2.X) - startx) / options.linePairCount;
                Scalar highLight = options.highlight ? Scalar.Gray : Scalar.White;
                for (int i = 0; i < options.linePairCount; i++)
                {
                    float noiseOffsetX = (float)((rand.NextDouble() * 2 - 1) * options.noiseOffset);
                    float noiseOffsetY = (float)((rand.NextDouble() * 2 - 1) * options.noiseOffset);
                    cv.Point pt = new cv.Point(
                        startx + i * incr + noiseOffsetX,
                        Math.Max(0, Math.Min(m * (startx + i * incr) + bb + noiseOffsetY, height))
                    );
                    pt.X = Math.Max(0, Math.Min(pt.X, width));
                    pt.Y = Math.Max(0, Math.Min(pt.Y, height));
                    points.Add((Point2f)pt);
                    Cv2.Circle(dst2, pt, task.DotSize + 1, highLight, -1);
                }
            }
        }
    }
    public class CS_Eigen_Fitline : CS_Parent
    {
        Eigen_FitLineInput noisyLine = new Eigen_FitLineInput();
        Mat eigenVec = new Mat(2, 2, MatType.CV_32F, 0);
        Mat eigenVal = new Mat(2, 2, MatType.CV_32F, 0);
        float theta;
        float len;
        float m2;
        public CS_Eigen_Fitline(VBtask task) : base(task)
        {
            labels[2] = "blue is Ground Truth, red is fitline, yellow is EigenFit";
            labels[3] = "Raw input (use sliders below to explore)";
            desc = "Remove outliers when trying to fit a line. Fitline and the Eigen computation below produce the same result.";
        }
        public void RunCS(Mat src)
        {
            noisyLine.options.recompute = true;
            noisyLine.Run(src);
            dst3 = noisyLine.dst2.Clone();
            dst2.SetTo(0);
            noisyLine.options.recompute = false;
            int width = src.Width;
            var nLines = Cv2.FitLine(noisyLine.points, DistanceTypes.L2, 1, 0.01, 0.01); 
            Vec4f line = new Vec4f((float)nLines.Vx, (float)nLines.Vy, (float)nLines.X1, (float)nLines.Y1);
            float m = line[1] / line[0];
            float bb = line[3] - m * line[2];
            cv.Point p1 = new cv.Point(0, bb);
            cv.Point p2 = new cv.Point(width, m * width + bb);
            Cv2.Line(dst2, p1, p2, Scalar.Red, 20, LineTypes.Link8);
            Mat pointMat = new Mat(noisyLine.options.randomCount, 1, MatType.CV_32FC2, noisyLine.points.ToArray());
            Scalar mean = Cv2.Mean(pointMat);
            Mat[] split = Cv2.Split(pointMat);
            var mmX = GetMinMax(split[0]);
            var mmY = GetMinMax(split[1]);
            Vec4f eigenInput = new Vec4f();
            foreach (Point2f pt in noisyLine.points)
            {
                float x = pt.X - (float)mean.Val0;
                float y = pt.Y - (float)mean.Val1;
                eigenInput[0] += x * x;
                eigenInput[1] += x * y;
                eigenInput[3] += y * y;
            }
            eigenInput[2] = eigenInput[1];
            List<Point2f> vec4f = new List<Point2f>
        {
            new Point2f(eigenInput[0], eigenInput[1]),
            new Point2f(eigenInput[1], eigenInput[3])
        };
            Mat D = new Mat(2, 2, MatType.CV_32FC1, vec4f.ToArray());
            Cv2.Eigen(D, eigenVal, eigenVec);
            theta = (float)Math.Atan2(eigenVec.Get<float>(1, 0), eigenVec.Get<float>(0, 0));
            len = (float)Math.Sqrt(Math.Pow(mmX.maxVal - mmX.minVal, 2) + Math.Pow(mmY.maxVal - mmY.minVal, 2));
            p1 = new cv.Point((int)(mean.Val0 - Math.Cos(theta) * len / 2), (int)(mean.Val1 - Math.Sin(theta) * len / 2));
            p2 = new cv.Point((int)(mean.Val0 + Math.Cos(theta) * len / 2), (int)(mean.Val1 + Math.Sin(theta) * len / 2));
            m2 = (p2.Y - p1.Y) / (p2.X - p1.X);
            if (Math.Abs(m2) > 1.0)
            {
                Cv2.Line(dst2, (cv.Point)p1, (cv.Point)p2, task.HighlightColor, 10, LineTypes.Link8);
            }
            else
            {
                p1 = new cv.Point((int)(mean.Val0 - Math.Cos(-theta) * len / 2), (int)(mean.Val1 - Math.Sin(-theta) * len / 2));
                p2 = new cv.Point((int)(mean.Val0 + Math.Cos(-theta) * len / 2), (int)(mean.Val1 + Math.Sin(-theta) * len / 2));
                m2 = (p2.Y - p1.Y) / (p2.X - p1.X);
                Cv2.Line(dst2, (cv.Point)p1, (cv.Point)p2, Scalar.Yellow, 10, LineTypes.Link8);
            }
            p1 = new cv.Point(0, noisyLine.bb);
            p2 = new cv.Point(width, noisyLine.m * width + noisyLine.bb);
            Cv2.Line(dst2, p1, p2, Scalar.Blue, task.lineWidth + 2, LineTypes.Link8);
            SetTrueText($"Ground Truth m = {noisyLine.m:F2} eigen m = {m2:F2}    len = {(int)len}\n" +
                        $"Confidence = {eigenVal.Get<float>(0, 0) / eigenVal.Get<float>(1, 0):F1}\n" +
                        $"theta: atan2({eigenVec.Get<float>(1, 0):F1}, {eigenVec.Get<float>(0, 0):F1}) = {theta:F4}");
        }
    }


    public class CS_EMax_Basics : CS_Parent
    {
        public EMax_InputClusters emaxInput = new EMax_InputClusters();
        public List<int> eLabels = new List<int>();
        public List<Point2f> eSamples = new List<Point2f>();
        public int dimension = 2;
        public int regionCount;
        public List<Point2f> centers = new List<Point2f>();
        Options_Emax options = new Options_Emax();
        bool useInputClusters;
        Palette_Variable palette = new Palette_Variable();
        public CS_EMax_Basics(VBtask task) : base(task)
        {
            cPtr = EMax_Open();
            FindSlider("EMax Number of Samples per region").Value = 1;
            labels[3] = "Emax regions as integers";
            UpdateAdvice(traceName + ": use local options to control EMax.");
            desc = "Use EMax - Expectation Maximization - to classify the regions around a series of labeled points";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (eLabels.Count == 0 || useInputClusters)
            {
                useInputClusters = true;
                emaxInput.Run(empty);
                eLabels = new List<int>(emaxInput.eLabels);
                eSamples = new List<Point2f>(emaxInput.eSamples);
                regionCount = emaxInput.regionCount;
            }
            if (centers.Count == 0) centers = new List<Point2f>(emaxInput.centers);
            labels[2] = $"{eLabels.Count} samples provided in {regionCount} regions";
            GCHandle handleSrc = GCHandle.Alloc(eSamples.ToArray(), GCHandleType.Pinned);
            GCHandle handleLabels = GCHandle.Alloc(eLabels.ToArray(), GCHandleType.Pinned);
            IntPtr imagePtr = EMax_Run(cPtr, handleSrc.AddrOfPinnedObject(), handleLabels.AddrOfPinnedObject(), eLabels.Count, dimension,
                                       dst2.Rows, dst2.Cols, regionCount, options.predictionStepSize, (int)options.covarianceType);
            handleLabels.Free();
            handleSrc.Free();
            dst1 = new Mat(dst1.Rows, dst1.Cols, MatType.CV_32S, imagePtr).Clone();
            dst1.ConvertTo(dst0, MatType.CV_8U);
            if (options.consistentcolors)
            {
                palette.colors.Clear();
                Vec3b[] newLabels = new Vec3b[regionCount + 1];
                for (int i = 0; i < eLabels.Count; i++)
                {
                    Point2f pt = eSamples[i];
                    if (pt.X < 0 || pt.X >= dst2.Width || pt.Y < 0 || pt.Y >= dst2.Height) continue;
                    byte newLabel = dst0.Get<byte>((int)pt.Y, (int)pt.X);
                    int original = eLabels[i];
                    Vec3b c = palette.originalColorMap.Get<Vec3b>(0, original % 256);
                    if (!newLabels.Contains(c) && newLabel <= regionCount) newLabels[newLabel] = c;
                }
                palette.colors = new List<Vec3b>(newLabels);
                palette.Run(dst0);
                dst2 = palette.dst2;
            }
            else
            {
                dst0 *= 255 / regionCount;
                dst2 = ShowPalette(dst0);
            }
            centers = new List<Point2f>(emaxInput.centers);
        }
        public void Close()
        {
            if (cPtr != IntPtr.Zero) cPtr = EMax_Close(cPtr);
        }
    }
    public class CS_EMax_Centers : CS_Parent
    {
        EMax_Basics emax = new EMax_Basics();
        public CS_EMax_Centers(VBtask task) : base(task)
        {
            labels[2] = "Centers are highlighted, Previous centers are black";
            desc = "Display the Emax centers as they move";
        }
        public void RunCS(Mat src)
        {
            emax.Run(src);
            dst2 = emax.dst2;
            List<Point2f> lastCenters = new List<Point2f>(emax.centers);
            for (int i = 0; i < emax.centers.Count; i++)
            {
                Cv2.Circle(dst2, emax.centers[i].ToPoint(), task.DotSize + 1, task.HighlightColor);
                if (i < lastCenters.Count)
                {
                    Cv2.Circle(dst2, lastCenters[i].ToPoint(), task.DotSize + 2, Scalar.Black);
                }
            }
            lastCenters = new List<Point2f>(emax.centers);
        }
    }
    public class CS_EMax_InputClusters : CS_Parent
    {
        public int regionCount;
        public int[] eLabels;
        public List<Point2f> eSamples = new List<Point2f>();
        public List<Point2f> centers = new List<Point2f>();
        Options_EmaxInputClusters options = new Options_EmaxInputClusters();
        public CS_EMax_InputClusters(VBtask task) : base(task)
        {
            labels[2] = "EMax algorithms input samples";
            desc = "Options for EMax algorithms.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            if (task.FirstPass)
            {
                task.gOptions.setGridMaximum(dst2.Width);
                task.gOptions.setGridSize((int)(dst2.Width / 3));
                task.grid.Run(dst2);
            }
            if (regionCount != task.gridList.Count) task.optionsChanged = true;
            regionCount = task.gridList.Count;
            Mat samples = new Mat(regionCount * options.samplesPerRegion, 2, MatType.CV_32F).Reshape(2, 0);
            Mat eLabelMat = new Mat(regionCount * options.samplesPerRegion, 1, MatType.CV_32S);
            for (int i = 0; i < regionCount; i++)
            {
                Rect roi = task.gridList[i];
                eLabelMat.RowRange(i * options.samplesPerRegion, (i + 1) * options.samplesPerRegion).SetTo(i);
                Mat tmp = samples.RowRange(i * options.samplesPerRegion, (i + 1) * options.samplesPerRegion);
                Cv2.Randn(tmp, new Scalar(roi.X + task.gridSize / 2, roi.Y + task.gridSize / 2),
                          Scalar.All(options.sigma));
            }
            samples = samples.Reshape(1, 0);
            dst2.SetTo(0);
            eSamples.Clear();
            centers.Clear();
            for (int i = 0; i < regionCount * options.samplesPerRegion; i++)
            {
                Point2f pt = samples.Get<Point2f>(i, 0);
                centers.Add(pt);
                eSamples.Add(new Point2f((int)pt.X, (int)pt.Y));
                int label = eLabelMat.Get<int>(i);
                Cv2.Circle(dst2, pt.ToPoint(), task.DotSize + 2, task.HighlightColor, -1);
            }
            eLabels = new int[eLabelMat.Rows];
            Marshal.Copy(eLabelMat.Data, eLabels, 0, eLabels.Length);
        }
    }
    public class CS_EMax_VB_Failing : CS_Parent
    {
        public EMax_InputClusters emaxInput = new EMax_InputClusters();
        public List<int> eLabels = new List<int>();
        public List<Point2f> eSamples = new List<Point2f>();
        public int dimension = 2;
        public int regionCount;
        public CS_EMax_VB_Failing(VBtask task) : base(task)
        {
            desc = "OpenCV expectation maximization example.";
        }
        public void RunCS(Mat src)
        {
            emaxInput.Run(empty);
            eLabels = new List<int>(emaxInput.eLabels);
            eSamples = new List<Point2f>(emaxInput.eSamples);
            regionCount = emaxInput.regionCount;
            SetTrueText("The EMax algorithm fails as a result of a bug in em_model.Predict2.  See code for details." + Environment.NewLine +
                        "The C++ version works fine (EMax_RedCloud) and the 2 are functionally identical.", new cv.Point(20, 100));
            return; // Comment this line to see the bug in the C# version of this Predict2 below. Any answers would be gratefully received.
            //EM em_model = EM.Create();
            //em_model.ClustersNumber = regionCount;
            //em_model.CovarianceMatrixType = EMTypes.CovMatSpherical;
            //em_model.TermCriteria = new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.Count, 300, 1.0);
            //Mat samples = new Mat(eSamples.Count, 2, MatType.CV_32FC1, eSamples.ToArray());
            //Mat eLabelsMat = new Mat(eLabels.Count, 1, MatType.CV_32S, eLabels.ToArray());
            //em_model.TrainEM(samples, null, eLabelsMat, null);
            //Mat sample = new Mat(1, 2, MatType.CV_32FC1, 0);
            //for (int i = 0; i < dst2.Rows; i++)
            //{
            //    for (int j = 0; j < dst2.Cols; j++)
            //    {
            //        sample.Set<float>(0, 0, (float)j);
            //        sample.Set<float>(0, 1, (float)i);
            //        double response = Math.Round(em_model.Predict2(sample)[1]);
            //        Scalar c = task.vecColors[(int)response];
            //        Cv2.Circle(dst2, new cv.Point(j, i), task.DotSize, c);
            //    }
            //}
        }
    }


    public class CS_EMax_PointTracker : CS_Parent
    {
        KNN_Core knn = new KNN_Core();
        EMax_Basics emax = new EMax_Basics();
        public CS_EMax_PointTracker(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Output of EMax_RedCloud", "Emax centers tracked and smoothed." };
            desc = "Use KNN to track the EMax Centers";
        }
        public void RunCS(Mat src)
        {
            emax.Run(src);
            dst2 = emax.dst2;
            knn.queries = new List<Point2f>(emax.centers);
            knn.Run(empty);
            if (task.FirstPass)
            {
                knn.trainInput = new List<Point2f>(knn.queries);
                return;
            }
            dst3.SetTo(0);
            for (int i = 0; i < knn.queries.Count; i++)
            {
                Point2f p1 = knn.queries[i];
                Point2f p2 = knn.trainInput[knn.result[i, 0]];
                DrawCircle(dst3, p1, task.DotSize, task.HighlightColor, -1);
                DrawCircle(dst3, p2, task.DotSize, Scalar.Red, -1);
                DrawLine(dst3, p1, p2, Scalar.White, task.lineWidth);
            }
            knn.trainInput = new List<Point2f>(knn.queries);
            dst2 = dst2 | emax.emaxInput.dst2;
        }
    }
    public class CS_EMax_RandomClusters : CS_Parent
    {
        Random_Clusters clusters = new Random_Clusters();
        EMax_Basics emax = new EMax_Basics();
        public CS_EMax_RandomClusters(VBtask task) : base(task)
        {
            FindSlider("Number of points per cluster").Value = 1;
            labels = new string[] { "", "", "Random_Clusters output", "EMax layout for the random clusters supplied" };
            desc = "Build an EMax layout for random set of clusters (not a grid)";
        }
        public void RunCS(Mat src)
        {
            var regionSlider = FindSlider("Number of Clusters");
            emax.regionCount = regionSlider.Value;
            clusters.Run(empty);
            dst3 = clusters.dst2;
            emax.eLabels.Clear();
            emax.eSamples.Clear();
            for (int i = 0; i < emax.regionCount; i++)
            {
                var cList = clusters.clusters[i];
                var cLabels = clusters.clusterLabels[i];
                for (int j = 0; j < cList.Count; j++)
                {
                    emax.eSamples.Add(cList[j]);
                    emax.eLabels.Add(cLabels[j]);
                }
            }
            emax.Run(src);
            dst2 = emax.dst2;
        }
    }



    public class CS_Encode_Basics : CS_Parent
    {
        Options_Encode options = new Options_Encode();
        public CS_Encode_Basics(VBtask task) : base(task)
        {
            desc = "Error Level Analysis - to verify a jpg image has not been modified.";
            labels[2] = "absDiff with original";
            labels[3] = "Original decompressed";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.FirstPass) FindSlider("Encode Output Scaling").Value = 10;

            int[] encodeParams = { (int)options.encodeOption, options.qualityLevel };
            byte[] buf = src.ImEncode(".jpg", encodeParams);
            Mat image = new Mat(buf.Length, 1, MatType.CV_8U, buf);
            dst3 = Cv2.ImDecode(image, ImreadModes.AnyColor);
            Mat output = new Mat();
            Cv2.Absdiff(src, dst3, output);
            output.ConvertTo(dst2, MatType.CV_8UC3, options.scalingLevel);
            double compressionRatio = (double)buf.Length / (src.Rows * src.Cols * src.ElemSize());
            labels[3] = $"Original compressed to len={buf.Length} ({compressionRatio:P1})";
        }
    }
    public class CS_Encode_Scaling : CS_Parent
    {
        Options_Encode options = new Options_Encode();
        public CS_Encode_Scaling(VBtask task) : base(task)
        {
            desc = "JPEG Encoder";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.FirstPass) FindSlider("Encode Output Scaling").Value = 10;

            int[] encodeParams = { (int)options.encodeOption, options.qualityLevel };
            byte[] buf = src.ImEncode(".jpg", encodeParams);
            Mat image = new Mat(buf.Length, 1, MatType.CV_8U, buf);
            dst3 = Cv2.ImDecode(image, ImreadModes.AnyColor);
            Mat output = new Mat();
            Cv2.Absdiff(src, dst3, output);
            output.ConvertTo(dst2, MatType.CV_8UC3, options.scalingLevel);
            double compressionRatio = (double)buf.Length / (src.Rows * src.Cols * src.ElemSize());
        }
    }


    public class CS_Entropy_Basics : CS_Parent
    {
        Entropy_Rectangle entropy = new Entropy_Rectangle();
        public CS_Entropy_Basics(VBtask task) : base(task)
        {
            labels[2] = "Control entropy values with histogram bins slider";
            desc = "Compute the entropy in an image - a measure of contrast(iness)";
        }
        Rect ValidatePreserve(Rect r)
        {
            if (r.Width <= 0) r.Width = 1;
            if (r.Height <= 0) r.Height = 1;
            if (r.X < 0) r.X = 0;
            if (r.Y < 0) r.Y = 0;
            if (r.X + r.Width >= task.WorkingRes.Width) r.X = task.WorkingRes.Width - r.Width - 1;
            if (r.Y + r.Height >= task.WorkingRes.Height) r.Y = task.WorkingRes.Height - r.Height - 1;
            return r;
        }
        public void RunCS(Mat src)
        {
            int stdSize = 30;
            if (task.drawRect == new Rect())
            {
                task.drawRect = new Rect(30, 30, stdSize, stdSize); // arbitrary rectangle
            }
            if (task.mouseClickFlag)
            {
                task.drawRect = ValidatePreserve(new Rect(task.ClickPoint.X, task.ClickPoint.Y, stdSize, stdSize));
            }
            task.drawRect = ValidateRect(task.drawRect);
            if (src.Channels() == 3)
            {
                entropy.Run(src.CvtColor(ColorConversionCodes.BGR2GRAY)[task.drawRect]);
            }
            else
            {
                entropy.Run(src[task.drawRect]);
            }
            dst2 = entropy.dst2;
            Cv2.Rectangle(dst2, task.drawRect, Scalar.White, task.lineWidth);
            if (task.heartBeat)
            {
                strOut = $"Click anywhere to measure the entropy with rect(pt.x, pt.y, {stdSize}, {stdSize})\n\n" +
                         $"Total entropy = {entropy.entropyVal.ToString(fmt1)}\n{entropy.strOut}";
            }
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Entropy_Highest : CS_Parent
    {
        Entropy_Rectangle entropy = new Entropy_Rectangle();
        public Rect eMaxRect;
        AddWeighted_Basics addw = new AddWeighted_Basics();
        public CS_Entropy_Highest(VBtask task) : base(task)
        {
            if (standaloneTest()) task.gOptions.setGridSize((int)(dst2.Width / 10));
            labels[2] = "Highest entropy marked with red rectangle";
            desc = "Find the highest entropy section of the color image.";
        }
        public void RunCS(Mat src)
        {
            Mat entropyMap = new Mat(src.Size(), MatType.CV_32F);
            float[] entropyList = new float[task.gridList.Count];
            float maxEntropy = float.MinValue;
            float minEntropy = float.MaxValue;
            src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            for (int i = 0; i < task.gridList.Count; i++)
            {
                Rect roi = task.gridList[i];
                entropy.Run(src[roi]);
                entropyMap[roi].SetTo(entropy.entropyVal);
                if (entropy.entropyVal > maxEntropy || task.optionsChanged)
                {
                    maxEntropy = entropy.entropyVal;
                    eMaxRect = roi;
                }
                if (entropy.entropyVal < minEntropy) minEntropy = entropy.entropyVal;
                if (standaloneTest())
                {
                    cv.Point pt = new cv.Point(roi.X, roi.Y);
                    SetTrueText(entropy.entropyVal.ToString(fmt2), pt, 2);
                    SetTrueText(entropy.entropyVal.ToString(fmt2), pt, 3);
                }
            }
            dst2 = entropyMap.ConvertScaleAbs(255 / (maxEntropy - minEntropy), minEntropy);
            addw.src2 = src;
            addw.Run(dst2);
            dst2 = addw.dst2;
            if (standaloneTest())
            {
                Cv2.Rectangle(dst2, eMaxRect, new Scalar(255), task.lineWidth);
                dst3.SetTo(0);
                Cv2.Rectangle(dst3, eMaxRect, Scalar.White, task.lineWidth);
            }
            labels[2] = $"Lighter = higher entropy. Range: {minEntropy:0.0} to {maxEntropy:0.0}";
        }
    }
    public class CS_Entropy_FAST : CS_Parent
    {
        Corners_Basics fast = new Corners_Basics();
        Entropy_Highest entropy = new Entropy_Highest();
        public CS_Entropy_FAST(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Output of Corners_FAST, input to entropy calculation", "Lighter color is higher entropy, highlight shows highest" };
            desc = "Use FAST markings to add to entropy";
        }
        public void RunCS(Mat src)
        {
            fast.Run(src);
            entropy.Run(fast.dst2);
            dst2 = entropy.dst2;
            dst3 = entropy.dst2;
            Cv2.Rectangle(dst3, entropy.eMaxRect, task.HighlightColor, task.lineWidth);
        }
    }
    public class CS_Entropy_Rectangle : CS_Parent
    {
        public float entropyVal;
        public CS_Entropy_Rectangle(VBtask task) : base(task)
        {
            desc = "Calculate the entropy in the drawRect when run standalone";
        }
        public float ChannelEntropy(int total, Mat hist)
        {
            float channelEntropy = 0;
            for (int i = 0; i < hist.Rows; i++)
            {
                float hc = Math.Abs(hist.Get<float>(i));
                if (hc != 0) channelEntropy += -(hc / total) * (float)Math.Log10(hc / total);
            }
            return channelEntropy;
        }
        public void RunCS(Mat src)
        {
            int[] dimensions = new int[] { task.histogramBins };
            if (src.Channels() != 1) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            var mm = GetMinMax(src);
            Rangef[] ranges = new Rangef[] { new Rangef((float)mm.minVal, (float)mm.maxVal) };
            if (mm.minVal == mm.maxVal) ranges = new Rangef[] { new Rangef(0, 255) };
            if (standalone)
            {
                if (task.drawRect.Width == 0 || task.drawRect.Height == 0)
                {
                    task.drawRect = new Rect(10, 10, 50, 50); // arbitrary template to match
                }
                src = src[task.drawRect];
            }
            Mat hist = new Mat();
            Cv2.CalcHist(new Mat[] { src }, new int[] { 0 }, null, hist, 1, dimensions, ranges);
            Mat histNormalized = hist.Normalize(0, hist.Rows, NormTypes.MinMax);
            entropyVal = ChannelEntropy((int)src.Total(), histNormalized) * 1000;
            strOut = $"Entropy X1000 {entropyVal.ToString(fmt1)}\n";
            dst2 = src;
            Cv2.Rectangle(dst2, task.drawRect, Scalar.White, task.lineWidth);
            dst3 = src;
            SetTrueText(strOut, 3);
        }
    }
    public class CS_Entropy_SubDivisions : CS_Parent
    {
        Entropy_Rectangle entropy = new Entropy_Rectangle();
        List<List<float>> entropies = new List<List<float>>();
        List<List<Rect>> eROI = new List<List<Rect>>();
        public List<Rect> roiList = new List<Rect>();
        public CS_Entropy_SubDivisions(VBtask task) : base(task)
        {
            labels[2] = "The top entropy values in each subdivision";
            for (int i = 0; i < task.subDivisionCount; i++)
            {
                entropies.Add(new List<float>()); // 4 quadrants
                eROI.Add(new List<Rect>()); // 4 quadrants
            }
            desc = "Find the highest entropy in each quadrant";
        }
        public void RunCS(Mat src)
        {
            dst2 = task.color.Clone();
            for (int i = 0; i < task.subDivisionCount; i++)
            {
                entropies[i].Clear();
                eROI[i].Clear();
            }
            dst1 = src.Channels() == 1 ? src : src.CvtColor(ColorConversionCodes.BGR2GRAY);
            int[] dimensions = new int[] { task.histogramBins };
            Rangef[] ranges = new Rangef[] { new Rangef(0, 255) };
            Mat hist = new Mat();
            for (int i = 0; i < task.gridList.Count; i++)
            {
                Rect roi = task.gridList[i];
                Cv2.CalcHist(new Mat[] { dst1[roi] }, new int[] { 0 }, null, hist, 1, dimensions, ranges);
                hist = hist.Normalize(0, hist.Rows, NormTypes.MinMax);
                float nextEntropy = entropy.channelEntropy((int)dst1[roi].Total(), hist) * 1000;
                entropies[task.subDivisions[i]].Add(nextEntropy);
                eROI[task.subDivisions[i]].Add(roi);
                if (standaloneTest()) SetTrueText(nextEntropy.ToString(fmt2), new cv.Point(roi.X, roi.Y), 3);
            }
            roiList.Clear();
            for (int i = 0; i < task.subDivisionCount; i++)
            {
                var eList = entropies[i];
                float maxEntropy = eList.Max();
                Rect roi = eROI[i][eList.IndexOf(maxEntropy)];
                roiList.Add(roi);
                Cv2.Rectangle(dst2, roi, Scalar.White);
            }
            cv.Point p1 = new cv.Point(0, dst2.Height / 3);
            cv.Point p2 = new cv.Point(dst2.Width, dst2.Height / 3);
            DrawLine(dst2, p1, p2, Scalar.White, task.lineWidth);
            p1 = new cv.Point(0, dst2.Height * 2 / 3);
            p2 = new cv.Point(dst2.Width, dst2.Height * 2 / 3);
            DrawLine(dst2, p1, p2, Scalar.White, task.lineWidth);
            p1 = new cv.Point(dst2.Width / 3, 0);
            p2 = new cv.Point(dst2.Width / 3, dst2.Height);
            DrawLine(dst2, p1, p2, Scalar.White, task.lineWidth);
            p1 = new cv.Point(dst2.Width * 2 / 3, 0);
            p2 = new cv.Point(dst2.Width * 2 / 3, dst2.Height);
            DrawLine(dst2, p1, p2, Scalar.White, task.lineWidth);
        }
    }
    public class CS_Entropy_BinaryImage : CS_Parent
    {
        Binarize_Simple binary = new Binarize_Simple();
        Entropy_Basics entropy = new Entropy_Basics();
        public CS_Entropy_BinaryImage(VBtask task) : base(task)
        {
            desc = "Measure entropy in a binary image";
        }
        public void RunCS(Mat src)
        {
            binary.Run(src);
            dst2 = binary.dst2;
            labels[2] = binary.labels[2];
            entropy.Run(dst2);
            SetTrueText(entropy.strOut, 3);
        }
    }



    public class CS_Erode_Basics : CS_Parent
    {
        public Options_Erode options = new Options_Erode();
        public CS_Erode_Basics(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": use local options to control erosion.");
            desc = "Erode the image provided.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.noshape || options.iterations == 0)
                dst2 = src;
            else
                dst2 = src.Erode(options.element, null, options.iterations);
            if (standaloneTest())
            {
                dst3 = task.depthRGB.Erode(options.element, null, options.iterations);
                labels[3] = "Eroded Depth " + options.iterations.ToString() + " times";
            }
            labels[2] = "Eroded BGR " + (-options.iterations).ToString() + " times";
        }
    }
    public class CS_Erode_CloudXY : CS_Parent
    {
        Erode_Basics erode = new Erode_Basics();
        Dilate_Basics dilate = new Dilate_Basics();
        Erode_Basics erodeMask = new Erode_Basics();
        public CS_Erode_CloudXY(VBtask task) : base(task)
        {
            FindSlider("Dilate Iterations").Value = 2;
            FindRadio("Erode shape: Ellipse").Checked = true;
            labels = new string[] { "", "", "Eroded point cloud X", "Erode point cloud Y" };
            desc = "Erode depth and then find edges";
        }
        public void RunCS(Mat src)
        {
            var dilateSlider = FindSlider("Dilate Iterations");
            var erodeSlider = FindSlider("Erode Iterations");
            erodeMask.Run(task.depthMask);
            dst1 = ~erodeMask.dst2;
            dilate.Run(task.pcSplit[0]);
            var mm = GetMinMax(dilate.dst2, erodeMask.dst2);
            dst2 = (dilate.dst2 - mm.minVal) / (mm.maxVal - mm.minVal);
            dst2.SetTo(0, dst1);
            erode.Run(task.pcSplit[1]);
            mm = GetMinMax(dilate.dst2, erodeMask.dst2);
            dst3 = (erode.dst2 - mm.minVal) / (mm.maxVal - mm.minVal);
            dst3.SetTo(0, dst1);
        }
    }
    public class CS_Erode_DepthSeed : CS_Parent
    {
        Erode_Basics erode = new Erode_Basics();
        Options_Erode options = new Options_Erode();
        public CS_Erode_DepthSeed(VBtask task) : base(task)
        {
            desc = "Erode depth to build a depth mask for inrange data.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Cv2.Erode(task.pcSplit[2], dst0, erode.options.element);
            dst0 = task.pcSplit[2] - dst0;
            dst3 = dst0.LessThan(options.flatDepth).ToMat();
            dst1 = task.pcSplit[2].GreaterThan(0).ToMat();
            dst1.SetTo(0, task.pcSplit[2].GreaterThan(task.MaxZmeters));
            dst3 = dst3 & dst1;
            dst2.SetTo(0);
            task.depthRGB.CopyTo(dst2, dst3);
        }
    }

    public class CS_Erode_Dilate : CS_Parent
    {
        Options_Dilate options = new Options_Dilate();
        public CS_Erode_Dilate(VBtask task) : base(task)
        {
            desc = "Erode and then dilate with MorphologyEx on the input image.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            Cv2.MorphologyEx(src, dst2, MorphTypes.Close, options.element);
            Cv2.MorphologyEx(dst2, dst2, MorphTypes.Open, options.element);
        }
    }




    public class CS_Etch_ASketch : CS_Parent
    {
        Keyboard_Basics keys;
        Scalar slateColor = new Scalar(122, 122, 122);
        cv.Point cursor;
        Random ms_rng = new Random();
        Options_Etch_ASketch options = new Options_Etch_ASketch();
        cv.Point lastCursor;
        cv.Point RandomCursor()
        {
            cv.Point nextCursor = new cv.Point(ms_rng.Next(0, dst2.Width), ms_rng.Next(0, dst2.Height));
            lastCursor = nextCursor;
            return nextCursor;
        }
        public CS_Etch_ASketch(VBtask task) : base(task)
        {
            keys = new Keyboard_Basics();
            cursor = RandomCursor();
            dst2.SetTo(slateColor);
            desc = "Use OpenCV to simulate the Etch-a-Sketch Toy";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            keys.Run(src);
            List<string> keyIn = new List<string>(keys.keyInput);
            if (options.demoMode)
            {
                keyIn.Clear(); // ignore any keyboard input when in Demo mode.
                string nextKey = new[] { "Down", "Up", "Left", "Right" }[ms_rng.Next(0, 4)];
                labels[2] = "CS_Etch_ASketch demo mode - moving randomly";
                for (int i = 0; i < ms_rng.Next(10, 51); i++)
                {
                    keyIn.Add(nextKey);
                }
            }
            else
            {
                labels[2] = "Use Up/Down/Left/Right keys to create image";
            }
            if (options.cleanMode)
            {
                cursor = RandomCursor();
                dst2.SetTo(slateColor);
            }
            foreach (string key in keyIn)
            {
                switch (key)
                {
                    case "Down":
                        cursor.Y += 1;
                        break;
                    case "Up":
                        cursor.Y -= 1;
                        break;
                    case "Left":
                        cursor.X -= 1;
                        break;
                    case "Right":
                        cursor.X += 1;
                        break;
                }
                cursor.X = Math.Max(0, Math.Min(cursor.X, src.Width - 1));
                cursor.Y = Math.Max(0, Math.Min(cursor.Y, src.Height - 1));
                dst2.Set<Vec3b>(cursor.Y, cursor.X, black);
            }
            if (options.demoMode)
            {
                lastCursor = cursor;
            }
        }
    }
    public class CS_Extrinsics_Basics : CS_Parent
    {
        AddWeighted_Basics addw = new AddWeighted_Basics();
        public CS_Extrinsics_Basics(VBtask task) : base(task)
        {
            if (standalone) task.gOptions.SetDotSize(5);
            desc = "MatchShapes: Show the alignment of the BGR image to the left and right camera images.";
        }
        public void RunCS(Mat src)
        {
            dst2 = task.leftView;
            dst3 = task.rightView;
            Mat gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            if (task.drawRect.Width > 0)
            {
                dst2.Rectangle(task.drawRect, Scalar.White, task.lineWidth, task.lineType);
                addw.src2 = dst2[task.drawRect].Resize(dst2.Size());
                addw.Run(gray);
                dst1 = addw.dst2;
            }
            cv.Point pt = new cv.Point(dst2.Width / 2, dst2.Height / 2);
            if (standaloneTest())
            {
                DrawCircle(dst2, pt, task.DotSize, Scalar.White);
                DrawCircle(dst3, pt, task.DotSize, Scalar.White);
                DrawCircle(dst2, pt, task.DotSize - 2, Scalar.Black);
                DrawCircle(dst3, pt, task.DotSize - 2, Scalar.Black);
                DrawCircle(task.color, pt, task.DotSize, Scalar.White);
            }
        }
    }
    public class CS_Extrinsics_Display : CS_Parent
    {
        Options_Extrinsics options = new Options_Extrinsics();
        Options_Translation optTrans = new Options_Translation();
        AddWeighted_Basics addw = new AddWeighted_Basics();
        public CS_Extrinsics_Display(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Left Image", "Right Image" };
            desc = "MatchShapes: Build overlays for the left and right images on the BGR image";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            optTrans.RunVB();
            Rect rectLeft = new Rect(options.leftCorner - optTrans.leftTrans, options.topCorner, dst2.Width - 2 * options.leftCorner, dst2.Height - 2 * options.topCorner);
            Rect rectRight = new Rect(options.rightCorner - optTrans.rightTrans, options.topCorner, dst2.Width - 2 * options.rightCorner, dst2.Height - 2 * options.topCorner);
            addw.src2 = task.leftView[rectLeft].Resize(dst2.Size());
            addw.Run(src);
            dst2 = addw.dst2.Clone();
            addw.src2 = task.rightView[rectRight].Resize(dst2.Size());
            addw.Run(src);
            dst3 = addw.dst2.Clone();
        }
    }


    public class CS_Face_Haar_LBP : CS_Parent
    {
        CascadeClassifier haarCascade;
        CascadeClassifier lbpCascade;
        public CS_Face_Haar_LBP(VBtask task) : base(task)
        {
            haarCascade = new CascadeClassifier(task.HomeDir + "Data/haarcascade_frontalface_default.xml");
            lbpCascade = new CascadeClassifier(task.HomeDir + "Data/lbpcascade_frontalface.xml");
            desc = "Detect faces in the video stream.";
            labels[2] = "Faces detected with Haar";
            labels[3] = "Faces detected with LBP";
        }
        public void RunCS(Mat src)
        {
            dst2 = src.Clone();
            DetectFace(ref dst2, haarCascade);
            dst3 = src.Clone();
            DetectFace(ref dst3, lbpCascade);
        }
    }
    public class CS_Face_Haar_Alt : CS_Parent
    {
        CascadeClassifier haarCascade;
        public CS_Face_Haar_Alt(VBtask task) : base(task)
        {
            haarCascade = new CascadeClassifier(task.HomeDir + "Data/haarcascade_frontalface_alt.xml");
            desc = "Detect faces Haar_alt database.";
            labels[2] = "Faces detected with Haar_Alt";
        }
        public void RunCS(Mat src)
        {
            dst2 = src.Clone();
            DetectFace(ref dst2, haarCascade);
        }
    }


    public class CS_Feature_Basics : CS_Parent
    {
        List<Mat> matList = new List<Mat>();
        List<Point2f> ptList = new List<Point2f>();
        KNN_Core knn = new KNN_Core();
        List<Point2f> ptLost = new List<Point2f>();
        Feature_Gather gather = new Feature_Gather();
        List<Mat> featureMat = new List<Mat>();
        public Options_Features options = new Options_Features();
        public CS_Feature_Basics(VBtask task) : base(task)
        {
            task.features.Clear(); // in case it was previously in use...
            desc = "Identify features with GoodFeaturesToTrack but manage them with MatchTemplate";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2 = src.Clone();
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            gather.Run(src);
            if (task.optionsChanged)
            {
                task.features.Clear();
                featureMat.Clear();
            }
            matList.Clear();
            ptList.Clear();
            Mat correlationMat = new Mat();
            for (int i = 0; i < Math.Min(featureMat.Count, task.features.Count); i++)
            {
                Point2f pt = task.features[i];
                Rect rect = ValidateRect(new Rect((int)(pt.X - options.templatePad), (int)(pt.Y - options.templatePad), featureMat[i].Width, featureMat[i].Height));
                if (!gather.ptList.Contains(new cv.Point((int)pt.X, (int)pt.Y)))
                {
                    Cv2.MatchTemplate(src.SubMat(rect), featureMat[i], correlationMat, TemplateMatchModes.CCoeffNormed);
                    if (correlationMat.Get<float>(0, 0) < options.correlationMin)
                    {
                        Point2f ptNew = new Point2f((int)pt.X, (int)pt.Y);
                        if (!ptLost.Contains(ptNew)) ptLost.Add(ptNew);
                        continue;
                    }
                }
                matList.Add(featureMat[i]);
                ptList.Add(pt);
            }
            featureMat = new List<Mat>(matList);
            task.features = new List<Point2f>(ptList);
            float extra = 1 + (1 - options.resyncThreshold);
            task.featureMotion = true;
            if (task.features.Count < gather.features.Count * options.resyncThreshold || task.features.Count > extra * gather.features.Count)
            {
                ptLost.Clear();
                featureMat.Clear();
                task.features.Clear();
                foreach (Point2f pt in gather.features)
                {
                    Rect rect = ValidateRect(new Rect((int)(pt.X - options.templatePad), (int)(pt.Y - options.templatePad), options.templateSize, options.templateSize));
                    featureMat.Add(src.SubMat(rect));
                    task.features.Add(pt);
                }
            }
            else
            {
                if (ptLost.Count > 0)
                {
                    knn.queries = ptLost;
                    knn.trainInput = gather.features;
                    knn.Run(null);
                    for (int i = 0; i < knn.queries.Count; i++)
                    {
                        Point2f pt = knn.queries[i];
                        Rect rect = ValidateRect(new Rect((int)(pt.X - options.templatePad), (int)(pt.Y - options.templatePad), options.templateSize, options.templateSize));
                        featureMat.Add(src.SubMat(rect));
                        task.features.Add(knn.trainInput[knn.result[i, 0]]);
                    }
                }
                else
                {
                    task.featureMotion = false;
                }
            }
            task.featurePoints.Clear();
            foreach (Point2f pt in task.features)
            {
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
                task.featurePoints.Add(new cv.Point((int)pt.X, (int)pt.Y));
            }
            if (task.heartBeat)
            {
                labels[2] = $"{task.features.Count}/{matList.Count} features were matched to the previous frame using correlation and {ptLost.Count} features had to be relocated.";
            }
        }
    }
    public class CS_Feature_BasicsNoFrills : CS_Parent
    {
        public Options_Features options = new Options_Features();
        Feature_Gather gather = new Feature_Gather();
        public CS_Feature_BasicsNoFrills(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": Use 'Options_Features' to control output.");
            desc = "Find good features to track in a BGR image without using correlation coefficients which produce more consistent results.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2 = src.Clone();
            gather.Run(src);
            task.features.Clear();
            task.featurePoints.Clear();
            foreach (Point2f pt in gather.features)
            {
                task.features.Add(pt);
                task.featurePoints.Add(new cv.Point((int)pt.X, (int)pt.X));
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
            }
            labels[2] = gather.labels[2];
        }
    }
    public class CS_Feature_KNN : CS_Parent
    {
        KNN_Core knn = new KNN_Core();
        public List<Point2f> featurePoints = new List<Point2f>();
        public Feature_Basics feat = new Feature_Basics();
        public CS_Feature_KNN(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            desc = "Find good features to track in a BGR image but use the same point if closer than a threshold";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            knn.queries = new List<Point2f>(task.features);
            if (task.FirstPass) knn.trainInput = new List<Point2f>(knn.queries);
            knn.Run(null);
            for (int i = 0; i < knn.neighbors.Count; i++)
            {
                int trainIndex = knn.neighbors[i][0]; // index of the matched train input
                Point2f pt = knn.trainInput[trainIndex];
                Point2f qPt = task.features[i];
                if (pt.DistanceTo(qPt) > feat.options.minDistance) knn.trainInput[trainIndex] = task.features[i];
            }
            featurePoints = new List<Point2f>(knn.trainInput);
            src.CopyTo(dst2);
            dst3.SetTo(0);
            foreach (Point2f pt in featurePoints)
            {
                DrawCircle(dst2, pt, task.DotSize + 2, Scalar.White);
                DrawCircle(dst3, pt, task.DotSize + 2, Scalar.White);
            }
            labels[2] = feat.labels[2];
            labels[3] = feat.labels[2];
        }
    }
    public class CS_Feature_Reduction : CS_Parent
    {
        Reduction_Basics reduction = new Reduction_Basics();
        Feature_Basics feat = new Feature_Basics();
        public CS_Feature_Reduction(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Good features", "History of good features" };
            desc = "Get the features in a reduction grayscale image.";
        }
        public void RunCS(Mat src)
        {
            reduction.Run(src);
            dst2 = src;
            feat.Run(reduction.dst2);
            if (task.heartBeat) dst3.SetTo(0);
            foreach (Point2f pt in task.features)
            {
                DrawCircle(dst2, pt, task.DotSize, Scalar.White);
                DrawCircle(dst3, pt, task.DotSize, Scalar.White);
            }
        }
    }
    public class CS_Feature_MultiPass : CS_Parent
    {
        Feature_Basics feat = new Feature_Basics();
        public List<Point2f> featurePoints = new List<Point2f>();
        PhotoShop_SharpenDetail sharpen = new PhotoShop_SharpenDetail();
        public CS_Feature_MultiPass(VBtask task) : base(task)
        {
            task.gOptions.setRGBFilterActive(true);
            task.gOptions.setRGBFilterSelection("Filter_Laplacian");
            desc = "Run Feature_Basics twice and compare results.";
        }
        public void RunCS(Mat src)
        {
            feat.Run(task.color);
            dst2 = src.Clone();
            featurePoints = new List<Point2f>(task.features);
            string passCounts = $"{featurePoints.Count}/";
            feat.Run(src);
            foreach (var pt in task.features)
            {
                featurePoints.Add(pt);
            }
            passCounts += $"{task.features.Count}/";
            sharpen.Run(task.color);
            feat.Run(sharpen.dst2);
            foreach (var pt in task.features)
            {
                featurePoints.Add(pt);
            }
            passCounts += $"{task.features.Count}";
            foreach (var pt in featurePoints)
            {
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
            }
            if (task.heartBeat)
            {
                labels[2] = $"Total features = {featurePoints.Count}, pass counts = {passCounts}";
            }
        }
    }
    public class CS_Feature_PointTracker : CS_Parent
    {
        Font_FlowText flow = new Font_FlowText();
        public Feature_Basics feat = new Feature_Basics();
        Match_Points mPoints = new Match_Points();
        Options_Features options = new Options_Features();
        public CS_Feature_PointTracker(VBtask task) : base(task)
        {
            flow.dst = RESULT_DST3;
            labels[3] = "Correlation coefficients for each remaining cell";
            desc = "Use the top X goodFeatures and then use matchTemplate to find track them.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            double correlationMin = options.correlationMin;
            int templatePad = options.templatePad;
            int templateSize = options.templateSize;
            strOut = "";
            if (mPoints.ptx.Count <= 3)
            {
                mPoints.ptx.Clear();
                feat.Run(src);
                foreach (var pt in task.features)
                {
                    mPoints.ptx.Add(pt);
                    Rect rect = ValidateRect(new Rect((int)(pt.X - templatePad), (int)(pt.Y - templatePad), templateSize, templateSize));
                }
                strOut = "Restart tracking -----------------------------------------------------------------------------\n";
            }
            mPoints.Run(src);
            dst2 = src.Clone();
            for (int i = mPoints.ptx.Count - 1; i >= 0; i--)
            {
                if (mPoints.correlation[i] > correlationMin)
                {
                    DrawCircle(dst2, mPoints.ptx[i], task.DotSize, task.HighlightColor);
                    strOut += $"{mPoints.correlation[i]:F3}, ";
                }
                else
                {
                    mPoints.ptx.RemoveAt(i);
                }
            }
            if (standaloneTest())
            {
                flow.msgs.Add(strOut);
                flow.Run(empty);
            }
            labels[2] = $"Of the {task.features.Count} input points, {mPoints.ptx.Count} points were tracked with correlation above {correlationMin:F2}";
        }
    }
    public class CS_Feature_Delaunay : CS_Parent
    {
        Delaunay_Contours facet = new Delaunay_Contours();
        Feature_Basics feat = new Feature_Basics();
        public CS_Feature_Delaunay(VBtask task) : base(task)
        {
            FindSlider("Min Distance to next").Value = 10;
            desc = "Divide the image into contours with Delaunay using features";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            dst2 = feat.dst2;
            labels[2] = feat.labels[2];
            facet.inputPoints.Clear();
            foreach (var pt in task.features)
            {
                facet.inputPoints.Add(pt);
            }
            facet.Run(src);
            dst3 = facet.dst2;
            foreach (var pt in task.features)
            {
                DrawCircle(dst3, pt, task.DotSize, Scalar.White);
            }
            labels[3] = $"There were {task.features.Count} Delaunay contours";
        }
    }
    public class CS_Feature_LucasKanade : CS_Parent
    {
        FeatureFlow_LucasKanade pyr = new FeatureFlow_LucasKanade();
        public List<cv.Point> ptList = new List<cv.Point>();
        public List<cv.Point> ptLast = new List<cv.Point>();
        List<List<cv.Point>> ptHist = new List<List<cv.Point>>();
        public CS_Feature_LucasKanade(VBtask task) : base(task)
        {
            desc = "Provide a trace of the tracked features";
        }
        public void RunCS(Mat src)
        {
            pyr.Run(src);
            dst2 = src;
            labels[2] = pyr.labels[2];
            if (task.heartBeat) dst3.SetTo(0);
            ptList.Clear();
            int stationary = 0, motion = 0;
            for (int i = 0; i < pyr.features.Count; i++)
            {
                cv.Point pt = new cv.Point((int)pyr.features[i].X, (int)pyr.features[i].Y);
                ptList.Add(pt);
                if (ptLast.Contains(pt))
                {
                    Cv2.Circle(dst3, pt, task.DotSize, task.HighlightColor);
                    stationary++;
                }
                else
                {
                    DrawLine(dst3, pyr.lastFeatures[i], pyr.features[i], Scalar.White, task.lineWidth);
                    motion++;
                }
            }
            if (task.heartBeat) labels[3] = $"{stationary} features were stationary and {motion} features had some motion.";
            ptLast = new List<cv.Point>(ptList);
        }
    }
    public class CS_Feature_NearestCell : CS_Parent
    {
        RedCloud_Basics redC = new RedCloud_Basics();
        FeatureLeftRight_Basics feat = new FeatureLeftRight_Basics();
        KNN_Core knn = new KNN_Core();
        public CS_Feature_NearestCell(VBtask task) : base(task)
        {
            desc = "Find the nearest feature to every cell in task.redCells";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            redC.Run(src);
            dst2 = redC.dst2;
            dst3 = redC.dst2.Clone();
            labels[2] = redC.labels[2];
            knn.queries.Clear();
            foreach (var rc in task.redCells)
            {
                knn.queries.Add(rc.maxDStable);
            }
            knn.trainInput.Clear();
            foreach (var mp in feat.mpList)
            {
                knn.trainInput.Add(new Point2f(mp.p1.X, mp.p1.Y));
            }
            knn.Run(null);
            for (int i = 0; i < task.redCells.Count; i++)
            {
                var rc = task.redCells[i];
                rc.nearestFeature = knn.trainInput[knn.result[i, 0]];
                DrawLine(dst3, rc.nearestFeature, rc.maxDStable, task.HighlightColor, task.lineWidth);
            }
        }
    }
    public class CS_Feature_Points : CS_Parent
    {
        public Feature_Basics feat = new Feature_Basics();
        public CS_Feature_Points(VBtask task) : base(task)
        {
            labels[3] = "Features found in the image";
            desc = "Use the sorted list of Delaunay regions to find the top X points to track.";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            dst2 = feat.dst2;
            if (task.heartBeat) dst3.SetTo(0);
            foreach (var pt in task.features)
            {
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
                DrawCircle(dst3, pt, task.DotSize, task.HighlightColor);
            }
            labels[2] = $"{task.features.Count} targets were present with {feat.options.featurePoints} requested.";
        }
    }
    public class CS_Feature_Trace : CS_Parent
    {
        RedTrack_Features track = new RedTrack_Features();
        public CS_Feature_Trace(VBtask task) : base(task)
        {
            desc = "Placeholder to help find RedTrack_Features";
        }
        public void RunCS(Mat src)
        {
            track.Run(src);
            dst2 = track.dst2;
            labels = track.labels;
        }
    }
    public class CS_Feature_TraceDelaunay : CS_Parent
    {
        Feature_Delaunay features = new Feature_Delaunay();
        public List<List<Point2f>> goodList = new List<List<Point2f>>(); // stable points only
        public CS_Feature_TraceDelaunay(VBtask task) : base(task)
        {
            labels = new string[] { "Stable points highlighted", "", "", "Delaunay map of regions defined by the feature points" };
            desc = "Trace the GoodFeatures points using only Delaunay - no KNN or RedCloud or Matching.";
        }
        public void RunCS(Mat src)
        {
            features.Run(src);
            dst3 = features.dst2;
            if (task.optionsChanged)
                goodList.Clear();
            List<Point2f> ptList = new List<Point2f>(task.features);
            goodList.Add(ptList);
            if (goodList.Count >= task.frameHistoryCount)
                goodList.RemoveAt(0);
            dst2.SetTo(0);
            foreach (var pt_List in goodList)
            {
                foreach (var pt in pt_List)
                {
                    DrawCircle(task.color, pt, task.DotSize, task.HighlightColor);
                    Vec3b c = dst3.Get<Vec3b>((int)pt.Y, (int)pt.X);
                    DrawCircle(dst2, pt, task.DotSize + 1, vecToScalar(c));
                }
            }
            labels[2] = $"{task.features.Count} features were identified in the image.";
        }
    }
    public class CS_Feature_ShiTomasi : CS_Parent
    {
        Corners_HarrisDetector harris = new Corners_HarrisDetector();
        Corners_ShiTomasi_CPP shiTomasi = new Corners_ShiTomasi_CPP();
        Options_ShiTomasi options = new Options_ShiTomasi();
        public CS_Feature_ShiTomasi(VBtask task) : base(task)
        {
            FindSlider("Corner normalize threshold").Value = 15;
            labels = new string[] { "", "", "Features in the left camera image", "Features in the right camera image" };
            desc = "Identify feature points in the left and right views";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            if (options.useShiTomasi)
            {
                dst2 = task.leftView;
                dst3 = task.rightView;
                shiTomasi.Run(task.leftView);
                dst2.SetTo(Scalar.White, shiTomasi.dst3.CvtColor(ColorConversionCodes.BGR2GRAY));
                shiTomasi.Run(task.rightView);
                dst3.SetTo(task.HighlightColor, shiTomasi.dst3.CvtColor(ColorConversionCodes.BGR2GRAY));
            }
            else
            {
                harris.Run(task.leftView);
                dst2 = harris.dst2.Clone();
                harris.Run(task.rightView);
                dst3 = harris.dst2;
            }
        }
    }
    public class CS_Feature_Generations : CS_Parent
    {
        Feature_Basics feat = new Feature_Basics();
        List<cv.Point> features = new List<cv.Point>();
        List<int> gens = new List<int>();
        public CS_Feature_Generations(VBtask task) : base(task)
        {
            UpdateAdvice(traceName + ": Local options will determine how many features are present.");
            desc = "Find feature age maximum and average.";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            SortedList<int, cv.Point> newfeatures = new SortedList<int, cv.Point>(new compareAllowIdenticalIntegerInverted());
            foreach (var pt in task.featurePoints)
            {
                int index = features.IndexOf(pt);
                if (index >= 0)
                    newfeatures.Add(gens[index] + 1, pt);
                else
                    newfeatures.Add(1, pt);
            }
            if (task.heartBeat)
            {
                features.Clear();
                gens.Clear();
            }
            features = new List<cv.Point>(newfeatures.Values);
            gens = new List<int>(newfeatures.Keys);
            dst2 = src;
            for (int i = 0; i < features.Count; i++)
            {
                if (gens[i] == 1)
                    break;
                cv.Point pt = features[i];
                DrawCircle(dst2, pt, task.DotSize, Scalar.White);
            }
            if (task.heartBeat)
            {
                labels[2] = $"{features.Count} features found with max/average {gens[0]}/{gens.Average():F2} generations";
            }
        }
    }
    public class CS_Feature_History : CS_Parent
    {
        public List<cv.Point> features = new List<cv.Point>();
        public Feature_Basics feat = new Feature_Basics();
        List<List<cv.Point>> featureHistory = new List<List<cv.Point>>();
        List<int> gens = new List<int>();
        public CS_Feature_History(VBtask task) : base(task)
        {
            desc = "Find good features across multiple frames.";
        }
        public void RunCS(Mat src)
        {
            int histCount = task.frameHistoryCount;
            feat.Run(src);
            dst2 = src.Clone();
            featureHistory.Add(new List<cv.Point>(task.featurePoints));
            List<cv.Point> newFeatures = new List<cv.Point>();
            gens.Clear();
            foreach (var cList in featureHistory)
            {
                foreach (var pt in cList)
                {
                    int index = newFeatures.IndexOf(pt);
                    if (index >= 0)
                    {
                        gens[index]++;
                    }
                    else
                    {
                        newFeatures.Add(pt);
                        gens.Add(1);
                    }
                }
            }
            int threshold = histCount == 1 ? 0 : 1;
            features.Clear();
            int whiteCount = 0;
            for (int i = 0; i < newFeatures.Count; i++)
            {
                if (gens[i] > threshold)
                {
                    cv.Point pt = newFeatures[i];
                    features.Add(pt);
                    if (gens[i] < histCount)
                    {
                        DrawCircle(dst2, pt, task.DotSize + 2, Scalar.Red);
                    }
                    else
                    {
                        whiteCount++;
                        DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
                    }
                }
            }
            if (featureHistory.Count > histCount)
                featureHistory.RemoveAt(0);
            if (task.heartBeat)
            {
                labels[2] = $"{features.Count}/{whiteCount} present/present on every frame" +
                            $" Red is a recent addition, yellow is present on previous {histCount} frames";
            }
        }
    }
    public class CS_Feature_GridPopulation : CS_Parent
    {
        Feature_Basics feat = new Feature_Basics();
        public CS_Feature_GridPopulation(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, 0);
            labels[3] = "Click 'Show grid mask overlay' to see grid boundaries.";
            desc = "Find the feature population for each cell.";
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            dst2 = feat.dst2;
            labels[2] = feat.labels[2];
            dst3.SetTo(0);
            foreach (var pt in task.featurePoints)
            {
                dst3.Set<byte>((int)pt.Y, (int)pt.X, 255);
            }
            foreach (var roi in task.gridList)
            {
                Mat test = dst3.SubMat(roi).FindNonZero();
                SetTrueText(test.Rows.ToString(), roi.TopLeft, 3);
            }
        }
    }
    public class CS_Feature_Compare : CS_Parent
    {
        Feature_Basics feat = new Feature_Basics();
        Feature_BasicsNoFrills noFrill = new Feature_BasicsNoFrills();
        List<Point2f> saveLFeatures = new List<Point2f>();
        List<Point2f> saveRFeatures = new List<Point2f>();
        public CS_Feature_Compare(VBtask task) : base(task)
        {
            desc = "Prepare features for the left and right views";
        }
        public void RunCS(Mat src)
        {
            task.features = new List<Point2f>(saveLFeatures);
            feat.Run(src.Clone());
            dst2 = feat.dst2;
            labels[2] = feat.labels[2];
            saveLFeatures = new List<Point2f>(task.features);
            task.features = new List<Point2f>(saveRFeatures);
            noFrill.Run(src.Clone());
            dst3 = noFrill.dst2;
            labels[3] = "With no correlation coefficients " + noFrill.labels[2];
            saveRFeatures = new List<Point2f>(task.features);
        }
    }
    public class CS_Feature_Gather : CS_Parent
    {
        Corners_HarrisDetector harris = new Corners_HarrisDetector();
        Corners_Basics FAST = new Corners_Basics();
        Options_FeatureGather myOptions = new Options_FeatureGather();
        public List<Point2f> features = new List<Point2f>();
        public List<cv.Point> ptList = new List<cv.Point>();
        BRISK_Basics brisk = new BRISK_Basics();
        public Options_Features options = new Options_Features();
        public CS_Feature_Gather(VBtask task) : base(task)
        {
            FindSlider("Feature Sample Size").Value = 400;
            cPtr = Agast_Open();
            desc = "Gather features from a list of sources - GoodFeatures, Agast, Brisk.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            myOptions.RunVB();

            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            switch (myOptions.featureSource)
            {
                case FeatureSrc.goodFeaturesFull:
                    features = new List<Point2f>(Cv2.GoodFeaturesToTrack(src, options.featurePoints, options.quality, options.minDistance, null,
                                                          options.blockSize, true, options.k));
                    labels[2] = $"GoodFeatures produced {features.Count} features";
                    break;
                case FeatureSrc.goodFeaturesGrid:
                    options.featurePoints = 4;
                    features.Clear();
                    for (int i = 0; i < task.gridList.Count; i++)
                    {
                        var roi = task.gridList[i];
                        var tmpFeatures = new List<Point2f>(Cv2.GoodFeaturesToTrack(src.SubMat(roi), options.featurePoints, options.quality, options.minDistance, null,
                                                                     options.blockSize, true, options.k));
                        for (int j = 0; j < tmpFeatures.Count; j++)
                        {
                            features.Add(new Point2f(tmpFeatures[j].X + roi.X, tmpFeatures[j].Y + roi.Y));
                        }
                    }
                    labels[2] = $"GoodFeatures produced {features.Count} features";
                    break;
                case FeatureSrc.Agast:
                    src = task.color.Clone();
                    byte[] dataSrc = new byte[src.Total() * src.ElemSize()];
                    Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
                    GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);
                    IntPtr imagePtr = Agast_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.agastThreshold);
                    handleSrc.Free();
                    Mat ptMat = new Mat(Agast_Count(cPtr), 1, MatType.CV_32FC2, imagePtr).Clone();
                    features.Clear();
                    if (standaloneTest())
                        dst2 = src;
                    for (int i = 0; i < ptMat.Rows; i++)
                    {
                        Point2f pt = ptMat.Get<Point2f>(i, 0);
                        features.Add(pt);
                        if (standaloneTest())
                            DrawCircle(dst2, pt, task.DotSize, Scalar.White);
                    }
                    labels[2] = $"GoodFeatures produced {features.Count} features";
                    break;
                case FeatureSrc.BRISK:
                    brisk.Run(src);
                    features = brisk.features;
                    labels[2] = $"GoodFeatures produced {features.Count} features";
                    break;
                case FeatureSrc.Harris:
                    harris.Run(src);
                    features = harris.features;
                    labels[2] = $"Harris Detector produced {features.Count} features";
                    break;
                case FeatureSrc.FAST:
                    FAST.Run(src);
                    features = FAST.features;
                    labels[2] = $"FAST produced {features.Count} features";
                    break;
            }
            ptList.Clear();
            foreach (var pt in features)
            {
                ptList.Add(new cv.Point((int)pt.X, (int)pt.Y));
            }
            if (standaloneTest())
            {
                dst2 = task.color.Clone();
                foreach (var pt in features)
                {
                    DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
                }
            }
        }
        public void Close()
        {
            if (cPtr != IntPtr.Zero)
                cPtr = Agast_Close(cPtr);
        }
    }


    public class CS_FeatureFlow_Basics : CS_Parent
    {
        public Feature_Basics feat = new Feature_Basics();
        public List<PointPair> mpList = new List<PointPair>();
        public List<float> mpCorrelation = new List<float>();
        public CS_FeatureFlow_Basics(VBtask task) : base(task)
        {
            task.gOptions.setMaxDepth(20);
            if (standalone) task.gOptions.setDisplay1();
            labels[1] = "NOTE: matching right point is always to the left of the left point";
            desc = "Identify which feature in the left image corresponds to the feature in the right image.";
        }
        public void buildCorrelations(List<cv.Point> prevFeatures, List<cv.Point> currFeatures)
        {
            float correlationMin = feat.options.correlationMin;
            Mat correlationmat = new Mat();
            mpList.Clear();
            mpCorrelation.Clear();
            int pad = feat.options.templatePad, size = feat.options.templateSize;
            foreach (cv.Point p1 in prevFeatures)
            {
                Rect rect = ValidateRect(new Rect(p1.X - pad, p1.Y - pad, size, size));
                List<float> correlations = new List<float>();
                foreach (cv.Point p2 in currFeatures)
                {
                    Rect r = ValidateRect(new Rect(p2.X - pad, p2.Y - pad, Math.Min(rect.Width, size), Math.Min(size, rect.Height)));
                    Cv2.MatchTemplate(dst2[rect], dst3[r], correlationmat, TemplateMatchModes.CCoeffNormed);
                    correlations.Add(correlationmat.Get<float>(0, 0));
                }
                float maxCorrelation = correlations.Max();
                if (maxCorrelation >= correlationMin)
                {
                    int index = correlations.IndexOf(maxCorrelation);
                    mpList.Add(new PointPair(p1, currFeatures[index]));
                    mpCorrelation.Add(maxCorrelation);
                }
            }
        }
        public void RunCS(Mat src)
        {
            feat.Run(src);
            labels = feat.labels;
            dst3 = task.FirstPass ? src.Clone() : dst2.Clone();
            List<cv.Point> prevFeatures = new List<cv.Point>(task.featurePoints);
            buildCorrelations(prevFeatures, task.featurePoints);
            SetTrueText("Click near any feature to find the corresponding pair of features.", 1);
            dst2 = src.Clone();
            foreach (cv.Point pt in task.featurePoints)
            {
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor);
            }
            prevFeatures = new List<cv.Point>(task.featurePoints);
        }
    }
    public class CS_FeatureFlow_Dense : CS_Parent
    {
        public Options_OpticalFlow options = new Options_OpticalFlow();
        public CS_FeatureFlow_Dense(VBtask task) : base(task)
        {
            desc = "Use dense optical flow algorithm";
        }
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
        public void RunCS(Mat src)
        {
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            options.RunVB();
            Mat lastGray = src.Clone();
            Mat hsv = opticalFlow_Dense(lastGray, src, options.pyrScale, options.levels, options.winSize, options.iterations, options.polyN,
                                        options.polySigma, options.OpticalFlowFlags);
            dst2 = hsv.CvtColor(ColorConversionCodes.HSV2RGB);
            dst2 = dst2.ConvertScaleAbs(options.outputScaling);
            dst3 = dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            lastGray = src.Clone();
        }
    }
    public class CS_FeatureFlow_LucasKanade : CS_Parent
    {
        public List<Point2f> features = new List<Point2f>();
        public List<Point2f> lastFeatures = new List<Point2f>();
        public Feature_Basics feat = new Feature_Basics();
        public Options_OpticalFlowSparse options = new Options_OpticalFlowSparse();
        public CS_FeatureFlow_LucasKanade(VBtask task) : base(task)
        {
            desc = "Show the optical flow of a sparse matrix.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2 = src.Clone();
            dst3 = src.Clone();
            if (src.Channels() == 3) src = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat lastGray = src.Clone();
            feat.Run(src);
            features = task.features.ToList();
            Mat features1 = new Mat(features.Count, 1, MatType.CV_32FC2, features.ToArray());
            Mat features2 = new Mat();
            Mat status = new Mat();
            Mat err = new Mat();
            cv.Size winSize = new cv.Size(3, 3);
            cv.TermCriteria term = new cv.TermCriteria((cv.CriteriaTypes)((int)cv.CriteriaTypes.Eps + (int)cv.CriteriaTypes.Count), 10, 1.0);
            Cv2.CalcOpticalFlowPyrLK(src, lastGray, features1, features2, status, err, winSize, 3, term, options.OpticalFlowFlag);
            features = new List<Point2f>();
            lastFeatures.Clear();
            for (int i = 0; i < status.Rows; i++)
            {
                if (status.Get<byte>(i, 0) != 0)
                {
                    Point2f pt1 = features1.Get<Point2f>(i, 0);
                    Point2f pt2 = features2.Get<Point2f>(i, 0);
                    float length = (float)Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
                    if (length < 30)
                    {
                        features.Add(pt1);
                        lastFeatures.Add(pt2);
                        DrawLine(dst2, pt1, pt2, task.HighlightColor, task.lineWidth + task.lineWidth);
                        DrawCircle(dst3, pt1, task.DotSize + 3, Scalar.White, -1);
                        DrawCircle(dst3, pt2, task.DotSize + 1, Scalar.Red, -1);
                    }
                }
            }
            labels[2] = "Matched " + features.Count + " points";
            if (task.heartBeat) lastGray = src.Clone();
            lastGray = src.Clone();
        }
    }
    public class CS_FeatureFlow_LeftRight1 : CS_Parent
    {
        public FeatureFlow_LucasKanade pyrLeft = new FeatureFlow_LucasKanade();
        public FeatureFlow_LucasKanade pyrRight = new FeatureFlow_LucasKanade();
        public List<cv.Point> ptLeft = new List<cv.Point>();
        public List<cv.Point> ptRight = new List<cv.Point>();
        public List<cv.Point> ptlist = new List<cv.Point>();
        public CS_FeatureFlow_LeftRight1(VBtask task) : base(task)
        {
            if (standalone) task.gOptions.setDisplay1();
            desc = "Find features using optical flow in both the left and right images.";
        }
        public void RunCS(Mat src)
        {
            pyrLeft.Run(task.leftView);
            pyrRight.Run(task.rightView);
            List<int> leftY = new List<int>();
            ptLeft.Clear();
            dst2 = task.leftView.Clone();
            for (int i = 0; i < pyrLeft.features.Count; i++)
            {
                cv.Point pt = new cv.Point((int)pyrLeft.features[i].X, (int)pyrLeft.features[i].Y);
                ptLeft.Add(new cv.Point(pt.X, pt.Y));
                Cv2.Circle(dst2, pt, task.DotSize, task.HighlightColor, -1, task.lineType, 0);
                leftY.Add(pt.Y);
                pt = new cv.Point((int)pyrLeft.lastFeatures[i].X, (int)pyrLeft.lastFeatures[i].Y);
                ptLeft.Add(new cv.Point(pt.X, pt.Y));
                Cv2.Circle(dst2, pt, task.DotSize, task.HighlightColor, -1, task.lineType, 0);
                leftY.Add(pt.Y);
            }
            List<int> rightY = new List<int>();
            ptRight.Clear();
            dst3 = task.rightView.Clone();
            for (int i = 0; i < pyrRight.features.Count; i++)
            {
                cv.Point pt = new cv.Point((int)pyrRight.features[i].X, (int)pyrRight.features[i].Y); 
                ptRight.Add(new cv.Point(pt.X, pt.Y));
                Cv2.Circle(dst3, pt, task.DotSize, task.HighlightColor, -1, task.lineType, 0);
                rightY.Add(pt.Y);
                pt = new cv.Point((int)pyrRight.lastFeatures[i].X, (int)pyrRight.lastFeatures[i].Y); 
                ptRight.Add(new cv.Point(pt.X, pt.Y));
                Cv2.Circle(dst3, pt, task.DotSize, task.HighlightColor, -1, task.lineType, 0);
                rightY.Add(pt.Y);
            }
            List<PointPair> mpList = new List<PointPair>();
            ptlist.Clear();
            for (int i = 0; i < leftY.Count; i++)
            {
                int index = rightY.IndexOf(leftY[i]);
                if (index != -1) mpList.Add(new PointPair(ptLeft[i], ptRight[index]));
            }
            if (task.heartBeat)
            {
                labels[2] = ptLeft.Count + " features found in the left image, " + ptRight.Count + " features in the right and " +
                            ptlist.Count + " features are matched.";
            }
        }
    }
    public class CS_FeatureFlow_LeftRightHist : CS_Parent
    {
        public FeatureFlow_LucasKanade pyrLeft = new FeatureFlow_LucasKanade();
        public FeatureFlow_LucasKanade pyrRight = new FeatureFlow_LucasKanade();
        public List<cv.Point> leftFeatures = new List<cv.Point>();
        public List<cv.Point> rightFeatures = new List<cv.Point>();
        public CS_FeatureFlow_LeftRightHist(VBtask task) : base(task)
        {
            desc = "Keep only the features that have been around for the specified number of frames.";
        }
        public Mat displayFeatures(Mat dst, List<cv.Point> features)
        {
            foreach (cv.Point pt in features)
            {
                Cv2.Circle(dst, pt, task.DotSize, task.HighlightColor, -1, task.lineType, 0);
            }
            return dst;
        }
        public void RunCS(Mat src)
        {
            pyrLeft.Run(task.leftView);
            List<cv.Point> tmpLeft = new List<cv.Point>();
            for (int i = 0; i < pyrLeft.features.Count; i++)
            {
                cv.Point pt = new cv.Point(pyrLeft.features[i].X, pyrLeft.features[i].Y);
                tmpLeft.Add(new cv.Point(pt.X, pt.Y));
                pt = new cv.Point(pyrLeft.lastFeatures[i].X, pyrLeft.lastFeatures[i].Y);
                tmpLeft.Add(new cv.Point(pt.X, pt.Y));
            }
            pyrRight.Run(task.rightView);
            List<cv.Point> tmpRight = new List<cv.Point>();
            for (int i = 0; i < pyrRight.features.Count; i++)
            {
                cv.Point pt = new cv.Point(pyrRight.features[i].X, pyrRight.features[i].Y);
                tmpRight.Add(new cv.Point(pt.X, pt.Y));
                pt = new cv.Point(pyrRight.lastFeatures[i].X, pyrRight.lastFeatures[i].Y);
                tmpRight.Add(new cv.Point(pt.X, pt.Y));
            }
            List<List<cv.Point>> leftHist = new List<List<cv.Point>> { tmpLeft };
            List<List<cv.Point>> rightHist = new List<List<cv.Point>> { tmpRight };
            if (task.optionsChanged)
            {
                leftHist = new List<List<cv.Point>> { tmpLeft };
                rightHist = new List<List<cv.Point>> { tmpRight };
            }
            leftFeatures.Clear();
            foreach (cv.Point pt in tmpLeft)
            {
                int count = 0;
                foreach (List<cv.Point> hist in leftHist)
                {
                    if (hist.Contains(pt)) count++;
                    else break;
                }
                if (count == leftHist.Count) leftFeatures.Add(pt);
            }
            rightFeatures.Clear();
            foreach (cv.Point pt in tmpRight)
            {
                int count = 0;
                foreach (List<cv.Point> hist in rightHist)
                {
                    if (hist.Contains(pt)) count++;
                    else break;
                }
                if (count == rightHist.Count) rightFeatures.Add(pt);
            }
            int minPoints = 10; // just a guess - trying to keep things current.
            if (leftFeatures.Count < minPoints)
            {
                leftFeatures = tmpLeft;
                leftHist = new List<List<cv.Point>> { tmpLeft };
            }
            if (rightFeatures.Count < minPoints)
            {
                rightFeatures = tmpRight;
                rightHist = new List<List<cv.Point>> { tmpRight };
            }
            dst2 = displayFeatures(task.leftView.Clone(), leftFeatures);
            dst3 = displayFeatures(task.rightView.Clone(), rightFeatures);
            leftHist.Add(tmpLeft);
            rightHist.Add(tmpRight);
            int threshold = Math.Min(task.frameHistoryCount, leftHist.Count);
            if (leftHist.Count >= task.frameHistoryCount) leftHist.RemoveAt(0);
            if (rightHist.Count >= task.frameHistoryCount) rightHist.RemoveAt(0);
            if (task.heartBeat)
            {
                labels[2] = leftFeatures.Count + " detected in the left image that have matches in " + threshold + " previous left images";
                labels[3] = rightFeatures.Count + " detected in the right image that have matches in " + threshold + " previous right images";
            }
        }
    }
    public class CS_FeatureFlow_LeftRight : CS_Parent
    {
        CS_FeatureFlow_LeftRightHist flowHist;
        public List<List<cv.Point>> leftFeatures = new List<List<cv.Point>>();
        public List<List<cv.Point>> rightFeatures = new List<List<cv.Point>>();
        public CS_FeatureFlow_LeftRight(VBtask task) : base(task)
        {
            flowHist = new CS_FeatureFlow_LeftRightHist(task);
            desc = "Match features in the left and right images";
        }
        public Mat DisplayFeatures(Mat dst, List<List<cv.Point>> features)
        {
            foreach (var ptlist in features)
            {
                foreach (var pt in ptlist)
                {
                    Cv2.Circle(dst, pt, task.DotSize, task.HighlightColor);
                }
            }
            return dst;
        }
        public void RunCS(Mat src)
        {
            flowHist.RunAndMeasure(src, flowHist);
            var tmpLeft = new SortedList<int, List<cv.Point>>();
            var tmpRight = new SortedList<int, List<cv.Point>>();
            ProcessFeatures(flowHist.leftFeatures, tmpLeft);
            ProcessFeatures(flowHist.rightFeatures, tmpRight);
            leftFeatures.Clear();
            rightFeatures.Clear();
            foreach (var ele in tmpLeft)
            {
                int index = tmpRight.Keys.ToList().IndexOf(ele.Key);
                if (index >= 0)
                {
                    leftFeatures.Add(ele.Value);
                    rightFeatures.Add(tmpRight.ElementAt(index).Value);
                }
            }
            dst2 = DisplayFeatures(task.leftView.Clone(), leftFeatures);
            dst3 = DisplayFeatures(task.rightView.Clone(), rightFeatures);
            if (task.heartBeat)
            {
                labels[2] = $"{leftFeatures.Count} detected in the left image that match one or more Y-coordinates found in the right image";
                labels[3] = $"{rightFeatures.Count} detected in the right image that match one or more Y-coordinates found in the left image";
            }
        }
        void ProcessFeatures(List<cv.Point> features, SortedList<int, List<cv.Point>> tmp)
        {
            foreach (var pt in features)
            {
                if (tmp.ContainsKey(pt.Y))
                {
                    var index = tmp.Keys.ToList().IndexOf(pt.Y);
                    var ptlist = tmp.ElementAt(index).Value;
                    ptlist.Add(pt);
                    tmp.RemoveAt(index);
                    tmp.Add(pt.Y, ptlist);
                }
                else
                {
                    tmp.Add(pt.Y, new List<cv.Point> { pt });
                }
            }
        }
    }

    public class CS_FeatureLeftRight_Basics : CS_Parent
    {
        public FeatureLeftRight_LeftRightPrep prep = new FeatureLeftRight_LeftRightPrep();
        public List<PointPair> mpList = new List<PointPair>();
        public List<float> mpCorrelation = new List<float>();
        public cv.Point selectedPoint;
        public int mpIndex;
        public cv.Point ClickPoint;
        public int picTag;
        public Options_Features options = new Options_Features();
        public KNN_Core knn = new KNN_Core();
        public CS_FeatureLeftRight_Basics(VBtask task) : base(task)
        {
            labels[1] = "NOTE: matching right point is always to the left of the left point";
            if (standalone) task.gOptions.setDisplay1();
            FindSlider("Feature Correlation Threshold").Value = 75;
            FindSlider("Min Distance to next").Value = 1;
            task.gOptions.setMaxDepth(20); // up to 20 meters...
            labels[3] = "Click near any feature to get more details on the matched pair of points.";
            desc = "Match the left and right features and allow the user to select a point to get more details.";
        }
        public void setClickPoint(Point2f pt, int _pictag)
        {
            ClickPoint = new cv.Point((int)pt.X, (int)pt.Y);
            picTag = _pictag;
            task.drawRect = new Rect(ClickPoint.X - options.templatePad, ClickPoint.Y - options.templatePad, options.templateSize, options.templateSize);
            task.drawRectUpdated = true;
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            dst2 = task.leftView.Clone();
            dst3 = task.rightView.Clone();
            prep.Run(src);
            List<PointPair> prepList = new List<PointPair>();
            foreach (cv.Point p1 in prep.leftFeatures)
            {
                foreach (cv.Point p2 in prep.rightFeatures)
                {
                    if (p1.Y == p2.Y) prepList.Add(new PointPair(p1, p2));
                }
            }
            Mat correlationmat = new Mat();
            mpList.Clear();
            mpCorrelation.Clear();
            for (int i = 0; i < prepList.Count; i++)
            {
                PointPair mpBase = prepList[i];
                List<float> correlations = new List<float>();
                List<PointPair> tmpList = new List<PointPair>();
                for (int j = i; j < prepList.Count; j++)
                {
                    PointPair mp = prepList[j];
                    if (mp.p1.Y != mpBase.p1.Y)
                    {
                        i = j;
                        break;
                    }
                    Rect r1 = ValidateRect(new Rect((int)(mp.p1.X - options.templatePad), (int)(mp.p1.Y - options.templatePad), options.templateSize, options.templateSize));
                    Rect r2 = ValidateRect(new Rect((int)(mp.p2.X - options.templatePad), (int)(mp.p2.Y - options.templatePad), options.templateSize, options.templateSize));
                    Cv2.MatchTemplate(task.leftView[r1], task.rightView[r2], correlationmat, TemplateMatchModes.CCoeffNormed);
                    correlations.Add(correlationmat.Get<float>(0, 0));
                    tmpList.Add(mp);
                }
                float maxCorrelation = correlations.Max();
                if (maxCorrelation >= options.correlationMin)
                {
                    mpList.Add(tmpList[correlations.IndexOf(maxCorrelation)]);
                    mpCorrelation.Add(maxCorrelation);
                }
            }
            foreach (PointPair mp in mpList)
            {
                DrawCircle(dst2, mp.p1, task.DotSize, task.HighlightColor, -1);
                DrawCircle(dst3, mp.p2, task.DotSize, task.HighlightColor, -1);
            }
            if (task.mouseClickFlag) setClickPoint(task.ClickPoint, task.mousePicTag);
            SetTrueText("Click near any feature to find the corresponding pair of features." + Environment.NewLine +
                        "The correlation values in the lower left for the correlation of the left to the right views." + Environment.NewLine +
                        "The dst2 shows features for the left view, dst3 shows features for the right view.", 1);
            if (ClickPoint == new cv.Point() && mpList.Count > 0) setClickPoint(mpList[0].p1, 2);
            if (mpList.Count > 0)
            {
                knn.queries.Clear();
                knn.queries.Add(task.ClickPoint);
                PointPair mp;
                knn.trainInput.Clear();
                foreach (PointPair mpX in mpList)
                {
                    Point2f pt = (picTag == 2) ? mpX.p1 : mpX.p2;
                    knn.trainInput.Add(pt);
                }
                knn.Run(null);
                dst1.SetTo(Scalar.All(0));
                int mpIndex = knn.result[0, 0];
                mp = mpList[mpIndex];
                DrawCircle(dst2, mp.p1, task.DotSize + 4, Scalar.Red, -1);
                DrawCircle(dst3, mp.p2, task.DotSize + 4, Scalar.Red, -1);
                float dspDistance = task.pcSplit[2].Get<float>((int)mp.p1.Y, (int)mp.p1.X);
                int offset = (int)(mp.p1.X - mp.p2.X);
                string strOut = string.Format(fmt3, mpCorrelation[mpIndex]) + Environment.NewLine +
                                string.Format(fmt3, dspDistance) + "m (from camera)" + Environment.NewLine +
                                offset.ToString() + " Pixel difference";
                for (int i = 0; i < mpList.Count; i++)
                {
                    Point2f pt = mpList[i].p1;
                    SetTrueText(string.Format("{0:0%}", mpCorrelation[i]), new cv.Point((int)pt.X, (int)pt.Y));
                }
                if (task.heartBeat) dst1.SetTo(Scalar.All(0));
                DrawCircle(dst1, mp.p1, task.DotSize, task.HighlightColor, -1);
                DrawCircle(dst1, mp.p2, task.DotSize, task.HighlightColor, -1);
                selectedPoint = new cv.Point(mp.p1.X, mpList[mpIndex].p1.Y + 10);
                SetTrueText(strOut, selectedPoint, 1);
                if (task.heartBeat)
                {
                    labels[2] = mpList.Count + " features matched and confirmed with left/right image correlation coefficients";
                }
            }
            labels[2] = mpList.Count + " features were matched using correlation coefficients in the left and right images. White box is cell around click point.";
        }
    }
    public class CS_FeatureLeftRight_LeftRightPrep : CS_Parent
    {
        public Feature_Basics lFeat = new Feature_Basics();
        public Feature_Basics rFeat = new Feature_Basics();
        public List<cv.Point> leftFeatures = new List<cv.Point>();
        public List<cv.Point> rightFeatures = new List<cv.Point>();
        public List<Point2f> saveLFeatures = new List<Point2f>();
        public List<Point2f> saveRFeatures = new List<Point2f>();
        public CS_FeatureLeftRight_LeftRightPrep(VBtask task) : base(task)
        {
            desc = "Prepare features for the left and right views";
        }
        public void RunCS(Mat src)
        {
            task.features = new List<cv.Point2f> (saveLFeatures);
            lFeat.Run(task.leftView);
            dst2 = lFeat.dst2;
            labels[2] = lFeat.labels[2];
            leftFeatures = task.featurePoints.ToList();
            saveLFeatures = task.features.ToList();
            task.features = new List<cv.Point2f>(saveRFeatures);
            rFeat.Run(task.rightView);
            dst3 = rFeat.dst2;
            labels[3] = rFeat.labels[2];
            rightFeatures = task.featurePoints.ToList();
            saveRFeatures = task.features.ToList();
        }
    }
    public class CS_FeatureLeftRight_Grid : CS_Parent
    {
        public FeatureLeftRight_Basics match = new FeatureLeftRight_Basics();
        public CS_FeatureLeftRight_Grid(VBtask task) : base(task)
        {
            if (standalone) task.gOptions.setDisplay1();
            FindRadio("GoodFeatures (ShiTomasi) grid").Checked = true;
            desc = "Run FeatureLeftRight_Basics but with 'GoodFeatures grid' instead of 'GoodFeatures full image'";
        }
        public void RunCS(Mat src)
        {
            match.Run(src);
            dst1 = match.dst1.Clone();
            dst2 = match.dst2.Clone();
            dst3 = match.dst3.Clone();
            if (task.FirstPass) match.setClickPoint(match.mpList[0].p1, 2);
            SetTrueText(match.strOut, match.selectedPoint, 1);
            if (task.heartBeat) labels = match.labels;
        }
    }
    public class CS_FeatureLeftRight_Input : CS_Parent
    {
        public List<cv.Point> ptLeft = new List<cv.Point>();
        public List<cv.Point> ptRight = new List<cv.Point>();
        public List<PointPair> mpList = new List<PointPair>();
        public List<float> mpCorrelation = new List<float>();
        public cv.Point selectedPoint;
        public cv.Point ClickPoint;
        public int picTag;
        public Options_Features options = new Options_Features();
        public KNN_Core knn = new KNN_Core();
        public CS_FeatureLeftRight_Input(VBtask task) : base(task)
        {
            labels[1] = "NOTE: matching right point is always to the left of the left point";
            if (standalone) task.gOptions.setDisplay1();
            FindSlider("Feature Correlation Threshold").Value = 75;
            FindSlider("Min Distance to next").Value = 1;
            task.gOptions.setMaxDepth(20); // up to 20 meters...
            labels[3] = "Click near any feature to get more details on the matched pair of points.";
            desc = "Match the left and right features and allow the user to select a point to get more details.";
        }
        public void setClickPoint(Point2f pt, int _pictag)
        {
            ClickPoint = new cv.Point(pt.X, pt.Y);
            picTag = _pictag;
            task.drawRect = new Rect(ClickPoint.X - options.templatePad, ClickPoint.Y - options.templatePad, options.templateSize, options.templateSize);
            task.drawRectUpdated = true;
        }
        public void RunCS(Mat src)
        {
            if (ptLeft.Count == 0 || ptRight.Count == 0)
            {
                SetTrueText("Caller provides the ptLeft/ptRight points to use.", 1);
                return;
            }
            options.RunVB();
            List<PointPair> prepList = new List<PointPair>();
            foreach (cv.Point p1 in ptLeft)
            {
                foreach (cv.Point p2 in ptRight)
                {
                    if (p1.Y == p2.Y) prepList.Add(new PointPair(p1, p2));
                }
            }
            Mat correlationmat = new Mat();
            mpList.Clear();
            mpCorrelation.Clear();
            for (int i = 0; i < prepList.Count; i++)
            {
                PointPair mpBase = prepList[i];
                List<float> correlations = new List<float>();
                List<PointPair> tmpList = new List<PointPair>();
                for (int j = i; j < prepList.Count; j++)
                {
                    PointPair mp = prepList[j];
                    if (mp.p1.Y != mpBase.p1.Y)
                    {
                        i = j;
                        break;
                    }
                    Rect r1 = ValidateRect(new Rect((int)(mp.p1.X - options.templatePad), (int)(mp.p1.Y - options.templatePad), options.templateSize, options.templateSize));
                    Rect r2 = ValidateRect(new Rect((int)(mp.p2.X - options.templatePad), (int)(mp.p2.Y - options.templatePad), options.templateSize, options.templateSize));
                    Cv2.MatchTemplate(task.leftView[r1], task.rightView[r2], correlationmat, TemplateMatchModes.CCoeffNormed);
                    correlations.Add(correlationmat.Get<float>(0, 0));
                    tmpList.Add(mp);
                }
                float maxCorrelation = correlations.Max();
                if (maxCorrelation >= options.correlationMin)
                {
                    mpList.Add(tmpList[correlations.IndexOf(maxCorrelation)]);
                    mpCorrelation.Add(maxCorrelation);
                }
            }
            foreach (PointPair mp in mpList)
            {
                DrawCircle(dst2, mp.p1, task.DotSize, task.HighlightColor, -1);
                DrawCircle(dst3, mp.p2, task.DotSize, task.HighlightColor, -1);
            }
            if (task.mouseClickFlag) setClickPoint(task.ClickPoint, task.mousePicTag);
            SetTrueText("Click near any feature to find the corresponding pair of features." + Environment.NewLine +
                        "The correlation values in the lower left for the correlation of the left to the right views." + Environment.NewLine +
                        "The dst2 shows features for the left view, dst3 shows features for the right view.", 1);
            if (ClickPoint == new cv.Point() && mpList.Count > 0) setClickPoint(mpList[0].p1, 2);
            if (mpList.Count > 0)
            {
                knn.queries.Clear();
                knn.queries.Add(task.ClickPoint);
                PointPair mp;
                knn.trainInput.Clear();
                foreach (PointPair  mpX in mpList)
                {
                    cv.Point2f pt = (picTag == 2) ? mpX.p1 : mpX.p2;
                    knn.trainInput.Add(new Point2f(pt.X, pt.Y));
                }
                knn.Run(null);
                dst1.SetTo(Scalar.All(0));
                int mpIndex = knn.result[0, 0];
                mp = mpList[mpIndex];
                DrawCircle(dst2, mp.p1, task.DotSize + 4, Scalar.Red, -1);
                DrawCircle(dst3, mp.p2, task.DotSize + 4, Scalar.Red, -1);
                float dspDistance = task.pcSplit[2].Get<float>((int)mp.p1.Y, (int)mp.p1.X);
                int offset = (int)(mp.p1.X - mp.p2.X);
                string strOut = string.Format(fmt3, mpCorrelation[mpIndex]) + Environment.NewLine +
                                string.Format(fmt3, dspDistance) + "m (from camera)" + Environment.NewLine +
                                offset.ToString() + " Pixel difference";
                for (int i = 0; i < mpList.Count; i++)
                {
                    Point2f pt = mpList[i].p1;
                    SetTrueText(string.Format("{0:0%}", mpCorrelation[i]), new cv.Point((int)pt.X, (int)pt.Y));
                }
                if (task.heartBeat) dst1.SetTo(Scalar.All(0));
                DrawCircle(dst1, mp.p1, task.DotSize, task.HighlightColor, -1);
                DrawCircle(dst1, mp.p2, task.DotSize, task.HighlightColor, -1);
                selectedPoint = new cv.Point(mp.p1.X, mpList[mpIndex].p1.Y + 10);
                SetTrueText(strOut, selectedPoint, 1);
                if (task.heartBeat)
                {
                    labels[2] = mpList.Count + " features matched and confirmed with left/right image correlation coefficients";
                }
            }
            labels[2] = mpList.Count + " features were matched using correlation coefficients in the left and right images. White box is cell around click point.";
        }
    }


    public class CS_FeatureLess_Basics : CS_Parent
    {
        EdgeDraw_Basics edgeD = new EdgeDraw_Basics();
        public int classCount = 2;
        public CS_FeatureLess_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "EdgeDraw_Basics output", "" };
            desc = "Access the EdgeDraw_Basics algorithm directly rather than through the CPP_Basics interface - more efficient";
        }
        public void RunCS(Mat src)
        {
            edgeD.Run(src);
            dst2 = edgeD.dst2;
            if (standaloneTest())
            {
                dst3 = src.Clone();
                dst3.SetTo(Scalar.Yellow, dst2);
            }
        }
    }
    public class CS_FeatureLess_Canny : CS_Parent
    {
        Edge_Canny edges = new Edge_Canny();
        Options_Sobel options = new Options_Sobel();
        public CS_FeatureLess_Canny(VBtask task) : base(task)
        {
            desc = "Use Canny edges to define featureless regions.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            edges.Run(src);
            dst2 = ~edges.dst2.Threshold(options.distanceThreshold, 255, ThresholdTypes.Binary);
        }
    }
    public class CS_FeatureLess_Sobel : CS_Parent
    {
        Edge_Sobel_Old edges = new Edge_Sobel_Old();
        Options_Sobel options = new Options_Sobel();
        public CS_FeatureLess_Sobel(VBtask task) : base(task)
        {
            desc = "Use Sobel edges to define featureless regions.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            edges.Run(src);
            dst2 = ~edges.dst2.Threshold(options.distanceThreshold, 255, ThresholdTypes.Binary);
        }
    }
    public class CS_FeatureLess_UniquePixels : CS_Parent
    {
        Hough_FeatureLessTopX fless = new Hough_FeatureLessTopX();
        Sort_1Channel sort = new Sort_1Channel();
        public CS_FeatureLess_UniquePixels(VBtask task) : base(task)
        {
            if (standaloneTest())
                FindSlider("Threshold for sort input").Value = 0;
            labels = new string[] { "", "Gray scale input to sort/remove dups", "Unique pixels", "" };
            desc = "Find the unique gray pixels for the featureless regions";
        }
        public void RunCS(Mat src)
        {
            fless.Run(src);
            dst2 = fless.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            sort.Run(dst2);
            dst3 = sort.dst2;
        }
    }
    public class CS_FeatureLess_Unique3Pixels : CS_Parent
    {
        Hough_FeatureLessTopX fless = new Hough_FeatureLessTopX();
        Sort_3Channel sort3 = new Sort_3Channel();
        public CS_FeatureLess_Unique3Pixels(VBtask task) : base(task)
        {
            desc = "Find the unique 3-channel pixels for the featureless regions";
        }
        public void RunCS(Mat src)
        {
            fless.Run(src);
            dst2 = fless.dst2.CvtColor(ColorConversionCodes.BGR2GRAY);
            sort3.Run(fless.dst2);
            dst3 = sort3.dst2;
        }
    }
    public class CS_FeatureLess_Histogram : CS_Parent
    {
        BackProject_FeatureLess backP = new BackProject_FeatureLess();
        public CS_FeatureLess_Histogram(VBtask task) : base(task)
        {
            desc = "Create a histogram of the featureless regions";
        }
        public void RunCS(Mat src)
        {
            backP.Run(src);
            dst2 = backP.dst2;
            dst3 = backP.dst3;
            labels = backP.labels;
        }
    }
    public class CS_FeatureLess_DCT : CS_Parent
    {
        DCT_FeatureLess dct = new DCT_FeatureLess();
        public CS_FeatureLess_DCT(VBtask task) : base(task)
        {
            labels[3] = "Largest FeatureLess Region";
            desc = "Use DCT to find featureless regions.";
        }
        public void RunCS(Mat src)
        {
            dct.Run(src);
            dst2 = dct.dst2;
            dst3 = dct.dst3;
            Mat mask = dst2.Clone();
            List<int> objectSize = new List<int>();
            int regionCount = 1;
            for (int y = 0; y < mask.Rows; y++)
            {
                for (int x = 0; x < mask.Cols; x++)
                {
                    if (mask.Get<byte>(y, x) == 255)
                    {
                        cv.Point pt = new cv.Point(x, y);
                        int floodCount = mask.FloodFill(pt, regionCount);
                        objectSize.Add(floodCount);
                        regionCount++;
                    }
                }
            }
            int maxSize = 0, maxIndex = 0;
            for (int i = 0; i < objectSize.Count; i++)
            {
                if (maxSize < objectSize[i])
                {
                    maxSize = objectSize[i];
                    maxIndex = i;
                }
            }
            Mat label = mask.InRange(maxIndex + 1, maxIndex + 1);
            int nonZ = Cv2.CountNonZero(label);
            labels[3] = $"Largest FeatureLess Region ({nonZ} {(double)nonZ / label.Total():P1} pixels)";
            dst3.SetTo(Scalar.White, label);
        }
    }
    public class CS_FeatureLess_LeftRight : CS_Parent
    {
        FeatureLess_Basics fLess = new FeatureLess_Basics();
        public CS_FeatureLess_LeftRight(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "FeatureLess Left mask", "FeatureLess Right mask" };
            desc = "Find the featureless regions of the left and right images";
        }
        public void RunCS(Mat src)
        {
            fLess.Run(task.leftView);
            dst2 = fLess.dst2.Clone();
            fLess.Run(task.rightView);
            dst3 = fLess.dst2;
        }
    }

    public class CS_FeatureLess_History : CS_Parent
    {
        FeatureLess_Basics fLess = new FeatureLess_Basics();
        History_Basics frames = new History_Basics();
        public CS_FeatureLess_History(VBtask task) : base(task)
        {
            desc = "Accumulate the edges over a span of X images.";
        }
        public void RunCS(Mat src)
        {
            fLess.Run(src);
            dst2 = fLess.dst2;
            frames.Run(dst2);
            dst3 = frames.dst2;
        }
    }
    public class CS_FeatureLess_RedCloud : CS_Parent
    {
        public RedCloud_Basics redC = new RedCloud_Basics();
        FeatureLess_Basics fless = new FeatureLess_Basics();
        public CS_FeatureLess_RedCloud(VBtask task) : base(task)
        {
            desc = "Floodfill the FeatureLess output so each cell can be tracked.";
        }
        public void RunCS(Mat src)
        {
            fless.Run(src);
            redC.Run(fless.dst2);
            dst2 = redC.dst2;
            labels[2] = redC.labels[2];
        }
    }
    public class CS_FeatureLess_Groups : CS_Parent
    {
        RedCloud_CPP redCPP = new RedCloud_CPP();
        FeatureLess_Basics fless = new FeatureLess_Basics();
        public int classCount;
        public CS_FeatureLess_Groups(VBtask task) : base(task)
        {
            desc = "Group RedCloud cells by the value of their featureless maxDist";
        }
        public void RunCS(Mat src)
        {
            fless.Run(src);
            dst2 = fless.dst2;
            labels[2] = fless.labels[2];
            redCPP.Run(dst2);
            classCount = redCPP.classCount;
            dst3 = redCPP.dst2;
            labels[3] = $"{classCount} featureless regions were found.";
        }
    }


    //public class CS_FeatureLine_Basics : CS_Parent
    //{
    //    public Line_SubsetRect lines;
    //    public Line_DisplayInfo lineDisp;
    //    public Options_Features options
    //    public Match_tCell match;
    //    public List<tCell> tcells;
    //    public CS_FeatureLine_Basics(VBtask task) : base(task)
    //    {
    //        tcells = new List<tCell> { new tCell(), new tCell() };
    //        labels = new string[] { "", "", "Longest line present.", "" };
    //        desc = "Find and track a line using the end points";
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        options.RunVB();

    //        int distanceThreshold = 50; // pixels - arbitrary but realistically needs some value
    //        double linePercentThreshold = 0.7; // if less than 70% of the pixels in the line are edges, then find a better line.  Again, arbitrary but realistic.
    //        double correlationMin = options.correlationMin;
    //        bool correlationTest = tcells[0].correlation <= correlationMin || tcells[1].correlation <= correlationMin;
    //        lineDisp.distance = tcells[0].center.DistanceTo(tcells[1].center);
    //        if (task.optionsChanged || correlationTest || lineDisp.maskCount / lineDisp.distance < linePercentThreshold || lineDisp.distance < distanceThreshold)
    //        {
    //            int templatePad = options.templatePad;
    //            Rectangle subsetRect = new Rectangle(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6);
    //            lines.subsetRect = subsetRect;
    //            lines.Run(src.Clone());
    //            if (lines.mpList.Count == 0)
    //            {
    //                SetTrueText("No lines found.", 3);
    //                return;
    //            }
    //            Line_Point lp = lines.sortByLen.ElementAt(0).Value;
    //            tcells[0] = match.createCell(src, 0, lp.p1);
    //            tcells[1] = match.createCell(src, 0, lp.p2);
    //        }
    //        dst2 = src.Clone();
    //        for (int i = 0; i < tcells.Count; i++)
    //        {
    //            match.tCells[0] = tcells[i];
    //            match.Run(src);
    //            tcells[i] = match.tCells[0];
    //            SetTrueText(tcells[i].strOut, new cv.Point(tcells[i].rect.X, tcells[i].rect.Y));
    //            SetTrueText(tcells[i].strOut, new cv.Point(tcells[i].rect.X, tcells[i].rect.Y), 3);
    //        }
    //        lineDisp.tcells = new List<tCell>(tcells);
    //        lineDisp.Run(src);
    //        dst2 = lineDisp.dst2;
    //        SetTrueText(lineDisp.strOut, new cv.Point(10, 40), 3);
    //    }
    //}
    //public class CS_FeatureLine_VerticalVerify : CS_Parent
    //{
    //    public FeatureLine_VH linesVH;
    //    public IMU_VerticalVerify verify;
    //    public CS_FeatureLine_VerticalVerify(VBtask task) : base(task)
    //    {
    //        desc = "Select a line or group of lines and track the result";
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        linesVH.Run(src);
    //        verify.gCells = new List<gravityLine>(linesVH.gCells);
    //        verify.Run(src);
    //        dst2 = verify.dst2;
    //    }
    //}
    //public class CS_FeatureLine_VH : CS_Parent
    //{
    //    public List<gravityLine> gCells;
    //    public Match_tCell match;
    //    public Line_GCloud gLines;
    //    public Options_Features options
    //    public CS_FeatureLine_VH(VBtask task) : base(task)
    //    {
    //        if (FindFrm(traceName + " Radio Buttons") == null)
    //        {
    //            radio.Setup(traceName);
    //            radio.addRadio("Vertical lines");
    //            radio.addRadio("Horizontal lines");
    //            radio.check(0).Checked = true;
    //        }
    //        labels[3] = "More readable than dst1 - index, correlation, length (meters), and ArcY";
    //        desc = "Find and track all the horizontal or vertical lines";
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        options.RunVB();
    //        int templatePad = options.templatePad;
    //        // gLines.lines.subsetRect = new Rectangle(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6);
    //        gLines.Run(src);
    //        RadioButton vertRadio = FindRadio("Vertical lines");
    //        List<KeyValuePair<int, Line_Point>> sortedLines = vertRadio.Checked ? gLines.sortedVerticals : gLines.sortedHorizontals;
    //        if (sortedLines.Count == 0)
    //        {
    //            SetTrueText("There were no vertical lines found.", 3);
    //            return;
    //        }
    //        gravityLine gc;
    //        gCells.Clear();
    //        match.tCells.Clear();
    //        for (int i = 0; i < sortedLines.Count; i++)
    //        {
    //            gc = sortedLines[i].Value;
    //            if (i == 0)
    //            {
    //                dst1.SetTo(0);
    //                gc.tc1.template.CopyTo(dst1.ROI);
    //                gc.tc2.template.CopyTo(dst1.ROI);
    //            }
    //            match.tCells.Clear();
    //            match.tCells.Add(gc.tc1);
    //            match.tCells.Add(gc.tc2);
    //            match.Run(src);
    //            double correlationMin = options.correlationMin;
    //            if (match.tCells[0].correlation >= correlationMin && match.tCells[1].correlation >= correlationMin)
    //            {
    //                gc.tc1 = match.tCells[0];
    //                gc.tc2 = match.tCells[1];
    //                gc = gLines.updateGLine(src, gc, gc.tc1.center, gc.tc2.center);
    //                if (gc.len3D > 0) gCells.Add(gc);
    //            }
    //        }
    //        dst2 = src.Clone();
    //        dst3.SetTo(0);
    //        for (int i = 0; i < gCells.Count; i++)
    //        {
    //            tCell tc;
    //            gc = gCells[i];
    //            PointF p1, p2;
    //            for (int j = 0; j < 2; j++)
    //            {
    //                tc = j == 0 ? gc.tc1 : gc.tc2;
    //                if (j == 0) { p1 = tc.center; } else { p2 = tc.center; }
    //            }
    //            SetTrueText(i.ToString() + "\r\n" + tc.strOut + "\r\n" + gc.arcY.ToString("F3"), gc.tc1.center, 2);
    //            SetTrueText(i.ToString() + "\r\n" + tc.strOut + "\r\n" + gc.arcY.ToString("F3"), gc.tc1.center, 3);
    //            CvInvoke.Line(dst2, p1, p2, task.HighlightColor, 2);
    //            CvInvoke.Line(dst3, p1, p2, task.HighlightColor, 2);
    //        }
    //    }
    //}
    //public class CS_FeatureLine_Tutorial1 : CS_Parent
    //{
    //    public Line_Basics lines;
    //    public CS_FeatureLine_Tutorial1(VBtask task) : base(task)
    //    {
    //        labels[3] = "The highlighted lines are also lines in 3D.";
    //        desc = "Find all the lines in the image and determine which are in the depth data.";
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        lines.Run(src);
    //        dst2 = lines.dst2;
    //        List<PointPair> raw2D = new List<PointPair>();
    //        List<Point3f> raw3D = new List<Point3f>();
    //        foreach (Line_Point lp in lines.lpList)
    //        {
    //            if (task.pcSplit[2].Get<float>(lp.p1.Y, lp.p1.X) > 0 && task.pcSplit[2].Get<float>(lp.p2.Y, lp.p2.X) > 0)
    //            {
    //                raw2D.Add(lp);
    //                raw3D.Add(task.pointCloud.Get<Point3f>(lp.p1.Y, lp.p1.X));
    //                raw3D.Add(task.pointCloud.Get<Point3f>(lp.p2.Y, lp.p2.X));
    //            }
    //        }
    //        dst3 = src.Clone();
    //        for (int i = 0; i < raw2D.Count; i += 2)
    //        {
    //            CvInvoke.Line(dst3, raw2D[i].p1, raw2D[i].p2, task.HighlightColor, 2);
    //        }
    //        if (task.heartBeat) labels[2] = "Starting with " + lines.lpList.Count.ToString("000") + " lines, there are " + (raw3D.Count / 2).ToString("000") + " with depth data.";
    //    }
    //}
    //public class CS_FeatureLine_Tutorial2 : CS_Parent
    //{
    //    public Line_Basics lines;
    //    public IMU_GMatrix gMat;
    //    public CS_FeatureLine_Tutorial2(VBtask task) : base(task)
    //    {
    //        if (sliders.Setup(traceName)) sliders.setupTrackBar("Area kernel size for depth", 1, 10, 5);
    //        desc = "Find all the lines in the image and determine which are vertical and horizontal";
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        Slider kernelSlider = FindSlider("Area kernel size for depth");
    //        int k = kernelSlider.Value - 1;
    //        int kernel = kernelSlider.Value * 2 - 1;
    //        lines.Run(src);
    //        dst2 = lines.dst2;
    //        List<PointPair> raw2D = new List<PointPair>();
    //        List<Point3f> raw3D = new List<Point3f>();
    //        foreach (Line_Point lp in lines.lpList)
    //        {
    //            Point3f pt1 = new Point3f(), pt2 = new Point3f();
    //            for (int j = 0; j < 2; j++)
    //            {
    //                cv.Point pt = j == 0 ? lp.p1 : lp.p2;
    //                Rectangle rect = ValidateRect(new Rectangle(pt.X - k, pt.Y - k, kernel, kernel));
    //                VectorOfFloat val = task.pointCloud[rect].Mean(task.depthMask[rect]);
    //                if (j == 0) { pt1 = new Point3f(val[0], val[1], val[2]); } else { pt2 = new Point3f(val[0], val[1], val[2]); }
    //            }
    //            if (pt1.Z > 0 && pt2.Z > 0)
    //            {
    //                raw2D.Add(lp);
    //                raw3D.Add(task.pointCloud.Get<Point3f>(lp.p1.Y, lp.p1.X));
    //                raw3D.Add(task.pointCloud.Get<Point3f>(lp.p2.Y, lp.p2.X));
    //            }
    //        }
    //        dst3 = src.Clone();
    //        for (int i = 0; i < raw2D.Count; i += 2)
    //        {
    //            DrawLine(dst3, raw2D[i].p1, raw2D[i].p2, task.HighlightColor, 2);
    //        }
    //        if (task.heartBeat) labels[2] = "Starting with " + lines.lpList.Count.ToString("000") + " lines, there are " + raw3D.Count.ToString("000") + " with depth data.";
    //        if (raw3D.Count == 0)
    //        {
    //            SetTrueText("No vertical or horizontal lines were found");
    //        }
    //        else
    //        {
    //            gMat.Run(null);
    //            task.gMatrix = gMat.gMatrix;
    //            Mat matLines3D = new Mat(raw3D.Count, 3, MatType.CV_32F, raw3D.ToArray());
    //            cv.Gemm(matLines3D, task.gMatrix, 1, null, 0, matLines3D);
    //        }
    //    }
    //}
    //public class CS_FeatureLine_LongestVerticalKNN : CS_Parent
    //{
    //    public Line_GCloud gLines;
    //    public FeatureLine_Longest longest;
    //    public CS_FeatureLine_LongestVerticalKNN(VBtask task) : base(task)
    //    {
    //        labels[3] = "All vertical lines.  The numbers: index and Arc-Y for the longest X vertical lines.";
    //        desc = "Find all the vertical lines and then track the longest one with a lightweight KNN.";
    //    }
    //    bool testLastPair(PointPair lastPair, gravityLine gc)
    //    {
    //        double distance1 = lastPair.p1.DistanceTo(lastPair.p2);
    //        PointF p1 = gc.tc1.center;
    //        PointF p2 = gc.tc2.center;
    //        if (distance1 < 0.75 * p1.DistanceTo(p2)) return true; // it the longest vertical * 0.75 > current lastPair, then use the longest vertical...
    //        return false;
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        gLines.Run(src);
    //        if (gLines.sortedVerticals.Count == 0)
    //        {
    //            SetTrueText("No vertical lines were present", 3);
    //            return;
    //        }
    //        dst3 = src.Clone();
    //        int index = 0;
    //        if (testLastPair(longest.knn.lastPair, gLines.sortedVerticals.ElementAt(0).Value)) longest.knn.lastPair = new PointPair();
    //        foreach (gravityLine gc in gLines.sortedVerticals.Values)
    //        {
    //            if (index >= 10) break;
    //            cv.Point2f p1 = gc.tc1.center;
    //            cv.Point2f p2 = gc.tc2.center;
    //            if (longest.knn.lastPair.CompareTo(new PointPair()) == 0) longest.knn.lastPair = new PointPair(p1, p2);
    //            PointF pt = new PointF((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
    //            SetTrueText(index.ToString() + "\r\n" + gc.arcY.ToString("F3"), pt, 3);
    //            index++;
    //            DrawLine(dst3, p1, p2, task.HighlightColor, 2);
    //            longest.knn.trainInput.Add(p1);
    //            longest.knn.trainInput.Add(p2);
    //        }
    //        longest.Run(src);
    //        dst2 = longest.dst2;
    //    }
    //}
    //public class CS_FeatureLine_LongestV_Tutorial1 : CS_Parent
    //{
    //    public FeatureLine_Finder lines;
    //    public CS_FeatureLine_LongestV_Tutorial1(VBtask task) : base(task)
    //    {
    //        desc = "Use FeatureLine_Finder to find all the vertical lines and show the longest.";
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        dst2 = src.Clone();
    //        lines.Run(src);
    //        if (lines.sortedVerticals.Count == 0)
    //        {
    //            SetTrueText("No vertical lines were found", 3);
    //            return;
    //        }
    //        int index = lines.sortedVerticals.ElementAt(0).Value;
    //        cv.Point2f p1 = lines.lines2D[index];
    //        cv.Point2f p2 = lines.lines2D[index + 1];
    //        DrawLine(dst2, p1, p2, task.HighlightColor, 2);
    //        dst3.SetTo(0);
    //        DrawLine(dst3, p1, p2, task.HighlightColor, 2);
    //    }
    //}
    //public class CS_FeatureLine_LongestV_Tutorial2 : CS_Parent
    //{
    //    FeatureLine_Finder lines;
    //    KNN_Core4D knn;
    //    Point3f pt1, pt2;
    //    int lengthReject;
    //    float lastLength;
    //    public CS_FeatureLine_LongestV_Tutorial2(VBtask task) : base(task)
    //    {
    //        lines = new FeatureLine_Finder();
    //        knn = new KNN_Core4D();
    //        pt1 = new Point3f();
    //        pt2 = new Point3f();
    //        lengthReject = 0;
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        dst2 = src.Clone();
    //        lines.Run(src);
    //        dst1 = lines.dst3;
    //        if (lines.sortedVerticals.Count == 0)
    //        {
    //            SetTrueText("No vertical lines were found", 3);
    //            return;
    //        }
    //        List<Point3f> match3D = new List<Point3f>();
    //        knn.trainInput.Clear();
    //        for (int i = 0; i < lines.sortedVerticals.Count - 1; i++)
    //        {
    //            int sIndex = lines.sortedVerticals.ElementAt(i).Value;
    //            Point2f x1 = lines.lines2D[sIndex];
    //            Point2f x2 = lines.lines2D[sIndex + 1];
    //            Vec4f vec = (x1.Y < x2.Y) ? new Vec4f(x1.X, x1.Y, x2.X, x2.Y) : new Vec4f(x2.X, x2.Y, x1.X, x1.Y);
    //            if (knn.queries.Count == 0) knn.queries.Add(vec);
    //            knn.trainInput.Add(vec);
    //            match3D.Add(lines.lines3D[sIndex]);
    //            match3D.Add(lines.lines3D[sIndex + 1]);
    //        }
    //        Vec4f saveVec = knn.queries[0];
    //        knn.Run(new Mat());
    //        int index = knn.result[0, 0];
    //        Point2f p1 = new Point2f(knn.trainInput[index][0], knn.trainInput[index][1]);
    //        Point2f p2 = new Point2f(knn.trainInput[index][2], knn.trainInput[index][3]);
    //        pt1 = match3D[index * 2];
    //        pt2 = match3D[index * 2 + 1];
    //        DrawLine(dst2, p1, p2, task.HighlightColor, 2);
    //        dst3.Rectangle(new Rect(0, 0, dst3.Width, dst3.Height), new cv.Scalar(0, 0, 0), -1);
    //        DrawLine(dst3, p1, p2, task.HighlightColor, 2);
    //        if (task.FirstPass) lastLength = lines.sorted2DV.ElementAt(0).Key;
    //        float bestLength = lines.sorted2DV.ElementAt(0).Key;
    //        knn.queries.Clear();
    //        if (lastLength > 0.5f * bestLength)
    //        {
    //            knn.queries.Add(new Vec4f(p1.X, p1.Y, p2.X, p2.Y));
    //            lastLength =(float) p1.DistanceTo(p2);
    //        }
    //        else
    //        {
    //            lengthReject++;
    //            lastLength = bestLength;
    //        }
    //        labels[3] = "Length rejects = " + (lengthReject / task.frameCount).ToString("0%");
    //    }
    //}
    //public class CS_FeatureLine_Finder : CS_Parent
    //{
    //    Line_Basics lines = new Line_Basics();
    //    List<Point2f> lines2D = new List<Point2f>();
    //    List<Point3f> lines3D = new List<Point3f>();
    //    SortedList<float, int> sorted2DV;
    //    SortedList<float, int> sortedVerticals;
    //    SortedList<float, int> sortedHorizontals;
    //    public CS_FeatureLine_Finder(VBtask task) : base(task)
    //    {
    //        lines = new Line_Basics();
    //        lines2D = new List<Point2f>();
    //        lines3D = new List<Point3f>();
    //        sorted2DV = new SortedList<float, int>(new compareAllowIdenticalSingleInverted());
    //        sortedVerticals = new SortedList<float, int>(new compareAllowIdenticalSingleInverted());
    //        sortedHorizontals = new SortedList<float, int>(new compareAllowIdenticalSingleInverted());
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        int angleSlider = FindSlider("Angle tolerance in degrees");
    //        int kernelSlider = FindSlider("Area kernel size for depth");
    //        int tolerance = angleSlider.Value;
    //        int k = kernelSlider.Value - 1;
    //        int kernel = kernelSlider.Value * 2 - 1;
    //        dst3 = src.Clone();
    //        lines2D.Clear();
    //        lines3D.Clear();
    //        sorted2DV.Clear();
    //        sortedVerticals.Clear();
    //        sortedHorizontals.Clear();
    //        lines.Run(src);
    //        dst2 = lines.dst2;
    //        List<PointPair> raw2D = new List<PointPair>();
    //        List<Point3f> raw3D = new List<Point3f>();
    //        foreach (var lp in lines.lpList)
    //        {
    //            Point3f pt1 = new Point3f(), pt2 = new Point3f();
    //            for (int j = 0; j < 2; j++)
    //            {
    //                cv.Point2f pt = j == 0 ? lp.p1 : lp.p2;
    //                Rect rect = ValidateRect(new Rect((int)(pt.X - k), (int)(pt.Y - k), kernel, kernel));
    //                cv.Scalar val = task.pointCloud[rect].Mean(task.depthMask[rect]);
    //                if (j == 0) pt1 = new Point3f((float)val[0], (float)val[1], (float)val[2]);
    //                else pt2 = new Point3f((float)val[0], (float)val[1], (float)val[2]);
    //            }
    //            if (pt1.Z > 0 && pt2.Z > 0 && pt1.Z < 4 && pt2.Z < 4)
    //            {
    //                raw2D.Add(lp);
    //                raw3D.Add(pt1);
    //                raw3D.Add(pt2);
    //            }
    //        }
    //        if (raw3D.Count == 0)
    //        {
    //            SetTrueText("No vertical or horizontal lines were found");
    //            return;
    //        }
    //        Mat matLines3D = new Mat(raw3D.Count, 3, cv.MatType.CV_32F, raw3D.ToArray()).Mul(task.gMatrix);
    //        for (int i = 0; i < raw2D.Count - 1; i += 2)
    //        {
    //            Point3f pt1 = matLines3D.Get<Point3f>(i, 0);
    //            Point3f pt2 = matLines3D.Get<Point3f>(i + 1, 0);
    //            float len3D = distance3D(pt1, pt2);
    //            float arcY = (float)Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958f);
    //            if (Math.Abs(arcY - 90) < tolerance)
    //            {
    //                DrawLine(dst3, raw2D[i].p1, raw2D[i].p2, new cv.Scalar(255, 0, 0), 2);
    //                sortedVerticals.Add(len3D, lines3D.Count);
    //                sorted2DV.Add((float)raw2D[i].p1.DistanceTo(raw2D[i].p2), lines2D.Count);
    //                if (pt1.Y > pt2.Y)
    //                {
    //                    lines3D.Add(pt1);
    //                    lines3D.Add(pt2);
    //                    lines2D.Add(raw2D[i].p1);
    //                    lines2D.Add(raw2D[i].p2);
    //                }
    //                else
    //                {
    //                    lines3D.Add(pt2);
    //                    lines3D.Add(pt1);
    //                    lines2D.Add(raw2D[i].p2);
    //                    lines2D.Add(raw2D[i].p1);
    //                }
    //            }
    //            if (Math.Abs(arcY) < tolerance)
    //            {
    //                DrawLine(dst3, raw2D[i].p1, raw2D[i].p2, new cv.Scalar(0, 255, 255), 2);
    //                sortedHorizontals.Add(len3D, lines3D.Count);
    //                if (pt1.X < pt2.X)
    //                {
    //                    lines3D.Add(pt1);
    //                    lines3D.Add(pt2);
    //                    lines2D.Add(raw2D[i].p1);
    //                    lines2D.Add(raw2D[i].p2);
    //                }
    //                else
    //                {
    //                    lines3D.Add(pt2);
    //                    lines3D.Add(pt1);
    //                    lines2D.Add(raw2D[i].p2);
    //                    lines2D.Add(raw2D[i].p1);
    //                }
    //            }
    //        }
    //        labels[2] = "Starting with " + lines.lpList.Count.ToString("000") + " lines, there are " + (lines3D.Count / 2).ToString("000") + " with depth data.";
    //        labels[3] = "There were " + sortedVerticals.Count + " vertical lines (blue) and " + sortedHorizontals.Count + " horizontal lines (yellow)";
    //    }
    //}
    //public class CS_FeatureLine_VerticalLongLine : CS_Parent
    //{
    //    FeatureLine_Finder lines;
    //    public CS_FeatureLine_VerticalLongLine(VBtask task) : base(task)
    //    {
    //        lines = new FeatureLine_Finder();
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        if (task.heartBeat)
    //        {
    //            dst2 = src.Clone();
    //            lines.Run(src);
    //            if (lines.sortedVerticals.Count == 0)
    //            {
    //                SetTrueText("No vertical lines were found", 3);
    //                return;
    //            }
    //        }
    //        if (lines.sortedVerticals.Count == 0) return;
    //        int index = lines.sortedVerticals.ElementAt(0).Value;
    //        Point2f p1 = lines.lines2D[index];
    //        Point2f p2 = lines.lines2D[index + 1];
    //        DrawLine(dst2, p1, p2, task.HighlightColor, 2);
    //        dst3.Rectangle(new Rect(0, 0, dst3.Width, dst3.Height), new cv.Scalar(0, 0, 0), -1);
    //        DrawLine(dst3, p1, p2, task.HighlightColor, 2);
    //        Point3f pt1 = lines.lines3D[index];
    //        Point3f pt2 = lines.lines3D[index + 1];
    //        float len3D = distance3D(pt1, pt2);
    //        float arcY = (float) Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958f);
    //        SetTrueText(arcY.ToString("F3") + "\n" + len3D.ToString("F3") + "m len\n" + pt1.Z.ToString("F1") + "m dist", p1);
    //        SetTrueText(arcY.ToString("F3") + "\n" + len3D.ToString("F3") + "m len\n" + pt1.Z.ToString("F1") + "m dist", p1, 3);
    //    }
    //}
    //public class CS_FeatureLine_DetailsAll : CS_Parent
    //{
    //    FeatureLine_Finder lines;
    //    Font_FlowText flow;
    //    List<float> arcList;
    //    List<float> arcLongAverage;
    //    List<float> firstAverage;
    //    int firstBest;
    //    public CS_FeatureLine_DetailsAll(VBtask task) : base(task)
    //    {
    //        flow = new Font_FlowText();
    //        flow.dst = 3;
    //        lines = new FeatureLine_Finder();
    //        arcList = new List<float>();
    //        arcLongAverage = new List<float>();
    //        firstAverage = new List<float>();
    //        firstBest = 0;
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        if (task.heartBeat)
    //        {
    //            dst2 = src.Clone();
    //            lines.Run(src);
    //            if (lines.sortedVerticals.Count == 0)
    //            {
    //                SetTrueText("No vertical lines were found", 3);
    //                return;
    //            }
    //        }
    //        if (lines.sortedVerticals.Count == 0) return;
    //        dst3 = new Mat();
    //        dst3.Rectangle(new Rect(0, 0, dst3.Width, dst3.Height), new cv.Scalar(0, 0, 0), -1);
    //        arcList.Clear();
    //        flow.msgs.Clear();
    //        flow.msgs.Add("ID\tlength\tdistance");
    //        for (int i = 0; i < Math.Min(10, lines.sortedVerticals.Count) - 1; i++)
    //        {
    //            int index = lines.sortedVerticals.ElementAt(i).Value;
    //            Point2f p1 = lines.lines2D[index];
    //            Point2f p2 = lines.lines2D[index + 1];
    //            DrawLine(dst2, p1, p2, task.HighlightColor, 2);
    //            SetTrueText(i.ToString(), i % 2 == 0 ? new cv.Point(p1.X, p1.Y) : new cv.Point(p2.X, p2.Y), 2);
    //            DrawLine(dst3, p1, p2, task.HighlightColor, 2);
    //            Point3f pt1 = lines.lines3D[index];
    //            Point3f pt2 = lines.lines3D[index + 1];
    //            float len3D = distance3D(pt1, pt2);
    //            if (len3D > 0)
    //            {
    //                float arcY = (float)Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958f);
    //                arcList.Add(arcY);
    //                flow.msgs.Add(arcY.ToString("F3") + "\t" + len3D.ToString("F3") + "m \t" + pt1.Z.ToString("F1") + "m");
    //            }
    //        }
    //        flow.Run(new Mat());
    //        if (arcList.Count == 0) return;
    //        float mostAccurate = arcList[0];
    //        firstAverage.Add(mostAccurate);
    //        foreach (float arc in arcList)
    //        {
    //            if (arc > mostAccurate)
    //            {
    //                mostAccurate = arc;
    //                break;
    //            }
    //        }
    //        if (mostAccurate == arcList[0]) firstBest++;
    //        float avg = arcList.Average();
    //        arcLongAverage.Add(avg);
    //        labels[3] = "arcY avg = " + avg.ToString("F1") + ", long term average = " + arcLongAverage.Average().ToString("F1") +
    //                    ", first was best " + (firstBest / task.frameCount).ToString("0%") + " of the time, Avg of longest line " + firstAverage.Average().ToString("F1");
    //        if (arcLongAverage.Count > 1000)
    //        {
    //            arcLongAverage.RemoveAt(0);
    //            firstAverage.RemoveAt(0);
    //        }
    //    }
    //}
    //public class CS_FeatureLine_LongestKNN : CS_Parent
    //{
    //    Line_GCloud glines = new Line_GCloud();
    //    KNN_ClosestTracker knn = new KNN_ClosestTracker();
    //    Options_Features options = new Options_Features();
    //    gravityLine gline;
    //    Match_Basics match = new Match_Basics();
    //    cv.Point2f p1, p2;
    //    public CS_FeatureLine_LongestKNN(VBtask task) : base(task)
    //    {
    //        glines = new Line_GCloud();
    //        knn = new KNN_ClosestTracker();
    //        options = new Options_Features();
    //        gline = new gravityLine();
    //        match = new Match_Basics();
    //        p1 = new cv.Point();
    //        p2 = new cv.Point();
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        options.RunVB();
    //        dst2 = src;
    //        knn.Run(src.Clone());
    //        p1 = knn.lastPair.p1;
    //        p2 = knn.lastPair.p2;
    //        gline = glines.updateGLine(src, gline, new cv.Point(p1.X, p1.Y), new cv.Point(p2.X, p2.Y));
    //        Rect rect = ValidateRect(new Rect((int)Math.Min(p1.X, p2.X), (int)Math.Min(p1.Y, p2.Y), (int)Math.Abs(p1.X - p2.X) + 2, (int)Math.Abs(p1.Y - p2.Y)));
    //        match.template = src[rect];
    //        match.Run(src);
    //        if (match.correlation >= options.correlationMin)
    //        {
    //            dst3 = new Mat();
    //            dst3 = match.dst0.Resize(dst3.Size());
    //            DrawLine(dst2, p1, p2, task.HighlightColor, 2);
    //            DrawCircle(dst2, p1, task.DotSize, task.HighlightColor, -1);
    //            DrawCircle(dst2, p2, task.DotSize, task.HighlightColor, -1);
    //            rect = ValidateRect(new Rect((int)Math.Min(p1.X, p2.X), (int)Math.Min(p1.Y, p2.Y), (int)Math.Abs(p1.X - p2.X) + 2, (int)Math.Abs(p1.Y - p2.Y)));
    //            match.template = new Mat(src, rect).Clone();
    //        }
    //        else
    //        {
    //            task.HighlightColor = task.HighlightColor == new cv.Scalar(0, 255, 255) ? new cv.Scalar(255, 0, 0) : new cv.Scalar(0, 255, 255);
    //            knn.lastPair = new PointPair(new Point2f(), new Point2f());
    //        }
    //        labels[2] = "Longest line end points had correlation of " + match.correlation.ToString("F3") + " with the original longest line.";
    //    }
    //}
    //public class CS_FeatureLine_Longest : CS_Parent
    //{
    //    Line_GCloud glines = new Line_GCloud();
    //    KNN_ClosestTracker knn = new KNN_ClosestTracker();
    //    Options_Features options = new Options_Features();
    //    gravityLine gline;
    //    Match_Basics match1 = new Match_Basics(), match2 = new Match_Basics();;
    //    public CS_FeatureLine_Longest(VBtask task) : base(task)
    //    {
    //        labels[2] = "Longest line end points are highlighted ";
    //        glines = new Line_GCloud();
    //        knn = new KNN_ClosestTracker();
    //        options = new Options_Features();
    //        gline = new gravityLine();
    //        match1 = new Match_Basics();
    //        match2 = new Match_Basics();
    //    }
    //    public void RunCS(Mat src)
    //    {
    //        options.RunVB();
    //        dst2 = src.Clone();
    //        float correlationMin = match1.options.correlationMin;
    //        int templatePad = match1.options.templatePad;
    //        int templateSize = match1.options.templateSize;
    //        cv.Point2f p1, p2;
    //        if (task.heartBeat || match1.correlation < correlationMin && match2.correlation < correlationMin)
    //        {
    //            knn.Run(src.Clone());
    //            p1 = knn.lastPair.p1;
    //            Rect r1 = ValidateRect(new Rect((int)(p1.X - templatePad), (int)(p1.Y - templatePad), templateSize, templateSize));
    //            match1.template = new Mat(src, r1).Clone();
    //            p2 = knn.lastPair.p2;
    //            Rect r2 = ValidateRect(new Rect((int)(p2.X - templatePad), (int)(p2.Y - templatePad), templateSize, templateSize));
    //            match2.template = new Mat(src, r2).Clone();
    //        }
    //        match1.Run(src);
    //        p1 = match1.matchCenter;
    //        match2.Run(src);
    //        p2 = match2.matchCenter;
    //        gline = glines.updateGLine(src, gline, new cv.Point(p1.X, p1.Y), new cv.Point(p2.X, p2.Y));
    //        DrawLine(dst2, p1, p2, task.HighlightColor, 2);
    //        DrawCircle(dst2, p1, task.DotSize, task.HighlightColor, -1);
    //        DrawCircle(dst2, p2, task.DotSize, task.HighlightColor, -1);
    //        SetTrueText(match1.correlation.ToString("F3"), new cv.Point(p1.X, p1.Y));
    //        SetTrueText(match2.correlation.ToString("F3"), new cv.Point(p2.X, p2.Y));
    //    }
    //}
    //public class CS_compareAllowIdenticalSingleInverted : IComparer<float>
    //{
    //    public int Compare(float x, float y)
    //    {
    //        if (x > y) return -1;
    //        if (x < y) return 1;
    //        return 0;
    //    }
    //}
    //public static class Extensions
    //{
    //    public static float distance3D(this Point3f pt1, Point3f pt2)
    //    {
    //        return (float)Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2) + Math.Pow(pt1.Z - pt2.Z, 2));
    //    }
    //    public static Rectangle ValidateRect(this Rectangle rect)
    //    {
    //        int x = Math.Max(0, rect.X);
    //        int y = Math.Max(0, rect.Y);
    //        int width = Math.Min(rect.Width, rect.X + rect.Width > task.src.Width ? task.src.Width - rect.X : rect.Width);
    //        int height = Math.Min(rect.Height, rect.Y + rect.Height > task.src.Height ? task.src.Height - rect.Y : rect.Height);
    //        return new Rectangle(x, y, width, height);
    //    }
    //}


    public class CS_FeatureLine_Basics : CS_Parent
    {
        Line_SubsetRect lines = new Line_SubsetRect();
        Line_DisplayInfo lineDisp = new Line_DisplayInfo();
        Options_Features options = new Options_Features();
        Match_tCell match = new Match_tCell();
        public List<tCell> tcells;
        public CS_FeatureLine_Basics(VBtask task) : base(task)
        {
            tCell tc = new tCell();
            tcells = new List<tCell> { tc, tc };
            labels = new string[] { "", "", "Longest line present.", "" };
            desc = "Find and track a line using the end points";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            int distanceThreshold = 50; // pixels - arbitrary but realistically needs some value
            double linePercentThreshold = 0.7; // if less than 70% of the pixels in the line are edges, then find a better line.  Again, arbitrary but realistic.
            double correlationMin = options.correlationMin;
            bool correlationTest = tcells[0].correlation <= correlationMin || tcells[1].correlation <= correlationMin;
            lineDisp.distance = (int)tcells[0].center.DistanceTo(tcells[1].center);
            if (task.optionsChanged || correlationTest || lineDisp.maskCount / lineDisp.distance < linePercentThreshold || lineDisp.distance < distanceThreshold)
            {
                int templatePad = options.templatePad;
                lines.subsetRect = new Rect(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6);
                lines.Run(src.Clone());
                if (lines.mpList.Count == 0)
                {
                    SetTrueText("No lines found.", 3);
                    return;
                }
                var lp = lines.sortByLen.ElementAt(0).Value;
                tcells[0] = match.createCell(src, 0, lp.p1);
                tcells[1] = match.createCell(src, 0, lp.p2);
            }
            dst2 = src.Clone();
            for (int i = 0; i < tcells.Count; i++)
            {
                match.tCells[0] = tcells[i];
                match.Run(src);
                tcells[i] = match.tCells[0];
                SetTrueText(tcells[i].strOut, new cv.Point(tcells[i].rect.X, tcells[i].rect.Y));
                SetTrueText(tcells[i].strOut, new cv.Point(tcells[i].rect.X, tcells[i].rect.Y), 3);
            }
            lineDisp.tcells = new List<tCell>(tcells);
            lineDisp.Run(src);
            dst2 = lineDisp.dst2;
            SetTrueText(lineDisp.strOut, new cv.Point(10, 40), 3);
        }
    }
    public class CS_FeatureLine_VerticalVerify : CS_Parent
    {
        FeatureLine_VH linesVH = new FeatureLine_VH();
        public IMU_VerticalVerify verify = new IMU_VerticalVerify();
        public CS_FeatureLine_VerticalVerify(VBtask task) : base(task)
        {
            desc = "Select a line or group of lines and track the result";
        }
        public void RunCS(Mat src)
        {
            linesVH.Run(src);
            verify.gCells = new List<gravityLine>(linesVH.gCells);
            verify.Run(src);
            dst2 = verify.dst2;
        }
    }
    public class CS_FeatureLine_VH : CS_Parent
    {
        public List<gravityLine> gCells = new List<gravityLine>();
        Match_tCell match = new Match_tCell();
        Line_GCloud gLines = new Line_GCloud();
        Options_Features options = new Options_Features();
        public CS_FeatureLine_VH(VBtask task) : base(task)
        {
            labels[3] = "More readable than dst1 - index, correlation, length (meters), and ArcY";
            desc = "Find and track all the horizontal or vertical lines";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            int templatePad = options.templatePad;
            // gLines.lines.subsetRect = new Rect(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6);
            gLines.Run(src);
            var vertRadio = FindRadio("Vertical lines");
            var sortedLines = vertRadio.Checked ? gLines.sortedVerticals : gLines.sortedHorizontals;
            if (sortedLines.Count == 0)
            {
                SetTrueText("There were no vertical lines found.", 3);
                return;
            }
            gCells.Clear();
            match.tCells.Clear();
            for (int i = 0; i < sortedLines.Count; i++)
            {
                var gc = sortedLines.ElementAt(i).Value;
                if (i == 0)
                {
                    dst1.SetTo(0);
                    gc.tc1.template.CopyTo(dst1[gc.tc1.rect]);
                    gc.tc2.template.CopyTo(dst1[gc.tc2.rect]);
                }
                match.tCells.Clear();
                match.tCells.Add(gc.tc1);
                match.tCells.Add(gc.tc2);
                match.Run(src);
                double correlationMin = options.correlationMin;
                if (match.tCells[0].correlation >= correlationMin && match.tCells[1].correlation >= correlationMin)
                {
                    gc.tc1 = match.tCells[0];
                    gc.tc2 = match.tCells[1];
                    cv.Point gc1 = new cv.Point(gc.tc1.center.X, gc.tc1.center.Y);
                    cv.Point gc2 = new cv.Point(gc.tc2.center.X, gc.tc2.center.Y);
                    gc = gLines.updateGLine(src, gc, gc1, gc2);
                    if (gc.len3D > 0) gCells.Add(gc);
                }
            }
            dst2 = src;
            dst3.SetTo(0);
            for (int i = 0; i < gCells.Count; i++)
            {
                var gc = gCells[i];
                Point2f p1 = gc.tc1.center, p2 = gc.tc2.center;
                SetTrueText($"{i}\n{gc.tc1.strOut}\n{gc.arcY.ToString(fmt1)}", gc.tc1.center, 2);
                SetTrueText($"{i}\n{gc.tc1.strOut}\n{gc.arcY.ToString(fmt1)}", gc.tc1.center, 3);
                DrawLine(dst2, p1, p2, task.HighlightColor, task.lineWidth);
                DrawLine(dst3, p1, p2, task.HighlightColor, task.lineWidth);
            }
        }
    }
    public class CS_FeatureLine_Tutorial1 : CS_Parent
    {
        Line_Basics lines = new Line_Basics();
        public CS_FeatureLine_Tutorial1(VBtask task) : base(task)
        {
            labels[3] = "The highlighted lines are also lines in 3D.";
            desc = "Find all the lines in the image and determine which are in the depth data.";
        }
        public void RunCS(Mat src)
        {
            lines.Run(src);
            dst2 = lines.dst2;
            var raw2D = new List<PointPair>();
            var raw3D = new List<Point3f>();
            foreach (var lp in lines.lpList)
            {
                if (task.pcSplit[2].Get<float>((int)lp.p1.Y, (int)lp.p1.X) > 0 && task.pcSplit[2].Get<float>((int)lp.p2.Y, (int)lp.p2.X) > 0)
                {
                    raw2D.Add(lp);
                    raw3D.Add(task.pointCloud.Get<Point3f>((int)lp.p1.Y, (int)lp.p1.X));
                    raw3D.Add(task.pointCloud.Get<Point3f>((int)lp.p2.Y, (int)lp.p2.X));
                }
            }
            dst3 = src.Clone();
            for (int i = 0; i < raw2D.Count - 1; i += 2)
            {
                DrawLine(dst3, raw2D[i].p1, raw2D[i].p2, task.HighlightColor, task.lineWidth);
            }
            if (task.heartBeat)
            {
                labels[2] = $"Starting with {lines.lpList.Count:000} lines, there are {raw3D.Count / 2:000} with depth data.";
            }
        }
    }
    public class CS_FeatureLine_Tutorial2 : CS_Parent
    {
        Line_Basics lines = new Line_Basics();
        IMU_GMatrix gMat = new IMU_GMatrix();
        Options_LineFinder options = new Options_LineFinder();
        public CS_FeatureLine_Tutorial2(VBtask task) : base(task)
        {
            desc = "Find all the lines in the image and determine which are vertical and horizontal";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            lines.Run(src);
            dst2 = lines.dst2;
            var raw2D = new List<PointPair>();
            var raw3D = new List<Point3f>();
            foreach (var lp in lines.lpList)
            {
                Point3f pt1 = new cv.Point3f(), pt2 = new cv.Point3f();
                for (int j = 0; j < 2; j++)
                {
                    cv.Point pt = (j == 0) ? new cv.Point(lp.p1.X, lp.p1.Y) : new cv.Point(lp.p2.X, lp.p2.Y);
                    Rect rect = ValidateRect(new Rect(pt.X - options.kSize, pt.Y - options.kSize, options.kernelSize, options.kernelSize));
                    Scalar val = task.pointCloud.SubMat(rect).Mean(task.depthMask.SubMat(rect));
                    if (j == 0)
                        pt1 = new Point3f((float)val[0], (float)val[1], (float)val[2]);
                    else
                        pt2 = new Point3f((float)val[0], (float)val[1], (float)val[2]);
                }
                if (pt1.Z > 0 && pt2.Z > 0)
                {
                    raw2D.Add(lp);
                    raw3D.Add(task.pointCloud.Get<Point3f>((int)lp.p1.Y, (int)lp.p1.X));
                    raw3D.Add(task.pointCloud.Get<Point3f>((int)lp.p2.Y, (int)lp.p2.X));
                }
            }
            dst3 = src.Clone();
            for (int i = 0; i < raw2D.Count - 1; i += 2)
            {
                DrawLine(dst3, raw2D[i].p1, raw2D[i].p2, task.HighlightColor, task.lineWidth);
            }
            if (task.heartBeat)
            {
                labels[2] = $"Starting with {lines.lpList.Count:000} lines, there are {raw3D.Count:000} with depth data.";
            }
            if (raw3D.Count == 0)
            {
                SetTrueText("No vertical or horizontal lines were found");
            }
            else
            {
                gMat.Run(empty);
                task.gMatrix = gMat.gMatrix;
                Mat matLines3D = new Mat(raw3D.Count, 3, MatType.CV_32F, raw3D.ToArray()) * task.gMatrix;
            }
        }
    }
    public class CS_FeatureLine_LongestVerticalKNN : CS_Parent
    {
        Line_GCloud gLines = new Line_GCloud();
        FeatureLine_Longest longest = new FeatureLine_Longest();
        public CS_FeatureLine_LongestVerticalKNN(VBtask task) : base(task)
        {
            labels[3] = "All vertical lines.  The numbers: index and Arc-Y for the longest X vertical lines.";
            desc = "Find all the vertical lines and then track the longest one with a lightweight KNN.";
        }
        bool testLastPair(PointPair lastPair, gravityLine gc)
        {
            var distance1 = lastPair.p1.DistanceTo(lastPair.p2);
            var p1 = gc.tc1.center;
            var p2 = gc.tc2.center;
            if (distance1 < 0.75 * p1.DistanceTo(p2)) return true; // it the longest vertical * 0.75 > current lastPair, then use the longest vertical...
            return false;
        }
        public void RunCS(Mat src)
        {
            gLines.Run(src);
            if (gLines.sortedVerticals.Count == 0)
            {
                SetTrueText("No vertical lines were present", 3);
                return;
            }
            dst3 = src.Clone();
            var index = 0;
            if (testLastPair(longest.knn.lastPair, gLines.sortedVerticals.ElementAt(0).Value)) longest.knn.lastPair = new PointPair();
            foreach (var gc in gLines.sortedVerticals.Values)
            {
                if (index >= 10) break;
                var p1 = gc.tc1.center;
                var p2 = gc.tc2.center;
                if (longest.knn.lastPair.compare(new PointPair())) longest.knn.lastPair = new PointPair(p1, p2);
                var pt = new cv.Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                SetTrueText($"{index}\n{gc.arcY.ToString(fmt1)}", pt, 3); 
                index++;
                DrawLine(dst3, p1, p2, task.HighlightColor, task.lineWidth);
                longest.knn.trainInput.Add(p1);
                longest.knn.trainInput.Add(p2);
            }
            longest.Run(src);
            dst2 = longest.dst2;
        }
    }
    public class CS_FeatureLine_LongestV_Tutorial1 : CS_Parent
    {
        FeatureLine_Finder lines = new FeatureLine_Finder();
        public CS_FeatureLine_LongestV_Tutorial1(VBtask task) : base(task)
        {
            desc = "Use FeatureLine_Finder to find all the vertical lines and show the longest.";
        }
        public void RunCS(Mat src)
        {
            dst2 = src.Clone();
            lines.Run(src);
            if (lines.sortedVerticals.Count == 0)
            {
                SetTrueText("No vertical lines were found", 3);
                return;
            }
            var index = lines.sortedVerticals.ElementAt(0).Value;
            var p1 = lines.lines2D[index];
            var p2 = lines.lines2D[index + 1];
            DrawLine(dst2, p1, p2, task.HighlightColor, task.lineWidth);
            dst3.SetTo(0);
            DrawLine(dst3, p1, p2, task.HighlightColor, task.lineWidth);
        }
    }
    public class CS_FeatureLine_LongestV_Tutorial2 : CS_Parent
    {
        FeatureLine_Finder lines = new FeatureLine_Finder();
        KNN_Core4D knn = new KNN_Core4D();
        public cv.Point3f pt1 = new cv.Point3f();
        public cv.Point3f pt2 = new cv.Point3f();
        int lengthReject;
        public CS_FeatureLine_LongestV_Tutorial2(VBtask task) : base(task)
        {
            desc = "Use FeatureLine_Finder to find all the vertical lines.  Use KNN_Core4D to track each line.";
        }
        public void RunCS(Mat src)
        {
            dst2 = src.Clone();
            lines.Run(src);
            dst1 = lines.dst3;
            if (lines.sortedVerticals.Count == 0)
            {
                SetTrueText("No vertical lines were found", 3);
                return;
            }
            var match3D = new List<cv.Point3f>();
            knn.trainInput.Clear();
            for (var i = 0; i < lines.sortedVerticals.Count; i++)
            {
                var sIndex = lines.sortedVerticals.ElementAt(i).Value;
                var x1 = lines.lines2D[sIndex];
                var x2 = lines.lines2D[sIndex + 1];
                var vec = x1.Y < x2.Y ? new cv.Vec4f(x1.X, x1.Y, x2.X, x2.Y) : new cv.Vec4f(x2.X, x2.Y, x1.X, x1.Y);
                if (knn.queries.Count == 0) knn.queries.Add(vec);
                knn.trainInput.Add(vec);
                match3D.Add(lines.lines3D[sIndex]);
                match3D.Add(lines.lines3D[sIndex + 1]);
            }
            var saveVec = knn.queries[0];
            knn.Run(empty);
            var index = knn.result[0, 0];
            var p1 = new cv.Point2f(knn.trainInput[index][0], knn.trainInput[index][1]);
            var p2 = new cv.Point2f(knn.trainInput[index][2], knn.trainInput[index][3]);
            pt1 = match3D[index * 2];
            pt2 = match3D[index * 2 + 1];
            DrawLine(dst2, p1, p2, task.HighlightColor, task.lineWidth);
            dst3.SetTo(0);
            DrawLine(dst3, p1, p2, task.HighlightColor, task.lineWidth);
            var lastLength = lines.sorted2DV.ElementAt(0).Key;
            var bestLength = lines.sorted2DV.ElementAt(0).Key;
            knn.queries.Clear();
            if (lastLength > 0.5 * bestLength)
            {
                knn.queries.Add(new cv.Vec4f(p1.X, p1.Y, p2.X, p2.Y));
                lastLength = (float)p1.DistanceTo(p2);
            }
            else
            {
                lengthReject++;
                lastLength = bestLength;
            }
            labels[3] = "Length rejects = " + (lengthReject / task.frameCount).ToString("P0");
        }
    }




    public class CS_FeatureLine_Finder : CS_Parent
    {
        Line_Basics lines = new Line_Basics();
        public List<Point2f> lines2D = new List<Point2f>();
        public List<Point3f> lines3D = new List<Point3f>();
        public SortedList<float, int> sorted2DV = new SortedList<float, int>(new compareAllowIdenticalSingleInverted());
        public SortedList<float, int> sortedVerticals = new SortedList<float, int>(new compareAllowIdenticalSingleInverted());
        public SortedList<float, int> sortedHorizontals = new SortedList<float, int>(new compareAllowIdenticalSingleInverted());
        Options_LineFinder options = new Options_LineFinder();
        public CS_FeatureLine_Finder(VBtask task) : base(task)
        {
            desc = "Find all the lines in the image and determine which are vertical and horizontal";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();

            dst3 = src.Clone();
            lines2D.Clear();
            lines3D.Clear();
            sorted2DV.Clear();
            sortedVerticals.Clear();
            sortedHorizontals.Clear();
            lines.Run(src);
            dst2 = lines.dst2;
            List<PointPair> raw2D = new List<PointPair>();
            List<Point3f> raw3D = new List<Point3f>();
            foreach (var lp in lines.lpList)
            {
                Point3f pt1 = new Point3f(), pt2 = new Point3f();
                for (int j = 0; j < 2; j++)
                {
                    cv.Point2f pt = (j == 0) ? lp.p1 : lp.p2;
                    Rect rect = ValidateRect(new Rect((int)(pt.X - options.kSize), (int)(pt.Y - options.kSize), options.kernelSize, options.kernelSize));
                    Scalar val = task.pointCloud[rect].Mean(task.depthMask[rect]);
                    if (j == 0)
                        pt1 = new Point3f((float)val[0], (float)val[1], (float)val[2]);
                    else
                        pt2 = new Point3f((float)val[0], (float)val[1], (float)val[2]);
                }
                if (pt1.Z > 0 && pt2.Z > 0 && pt1.Z < 4 && pt2.Z < 4)
                {
                    raw2D.Add(lp);
                    raw3D.Add(pt1);
                    raw3D.Add(pt2);
                }
            }
            if (raw3D.Count == 0)
            {
                SetTrueText("No vertical or horizontal lines were found");
            }
            else
            {
                Mat matLines3D = new Mat(raw3D.Count, 3, MatType.CV_32F, raw3D.ToArray()) * task.gMatrix;
                for (int i = 0; i < raw2D.Count - 1; i += 2)
                {
                    Point3f pt1 = matLines3D.Get<Point3f>(i, 0);
                    Point3f pt2 = matLines3D.Get<Point3f>(i + 1, 0);
                    float len3D = distance3D(pt1, pt2);
                    double arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958);
                    if (Math.Abs(arcY - 90) < options.tolerance)
                    {
                        DrawLine(dst3, raw2D[i].p1, raw2D[i].p2, Scalar.Blue, task.lineWidth);
                        sortedVerticals.Add(len3D, lines3D.Count);
                        sorted2DV.Add((float)raw2D[i].p1.DistanceTo(raw2D[i].p2), lines2D.Count);
                        if (pt1.Y > pt2.Y)
                        {
                            lines3D.Add(pt1);
                            lines3D.Add(pt2);
                            lines2D.Add(raw2D[i].p1);
                            lines2D.Add(raw2D[i].p2);
                        }
                        else
                        {
                            lines3D.Add(pt2);
                            lines3D.Add(pt1);
                            lines2D.Add(raw2D[i].p2);
                            lines2D.Add(raw2D[i].p1);
                        }
                    }
                    if (Math.Abs(arcY) < options.tolerance)
                    {
                        DrawLine(dst3, raw2D[i].p1, raw2D[i].p2, Scalar.Yellow, task.lineWidth);
                        sortedHorizontals.Add(len3D, lines3D.Count);
                        if (pt1.X < pt2.X)
                        {
                            lines3D.Add(pt1);
                            lines3D.Add(pt2);
                            lines2D.Add(raw2D[i].p1);
                            lines2D.Add(raw2D[i].p2);
                        }
                        else
                        {
                            lines3D.Add(pt2);
                            lines3D.Add(pt1);
                            lines2D.Add(raw2D[i].p2);
                            lines2D.Add(raw2D[i].p1);
                        }
                    }
                }
            }
            labels[2] = $"Starting with {lines.lpList.Count:000} lines, there are {lines3D.Count / 2:000} with depth data.";
            labels[3] = $"There were {sortedVerticals.Count} vertical lines (blue) and {sortedHorizontals.Count} horizontal lines (yellow)";
        }
    }

    public class CS_FeatureLine_VerticalLongLine : CS_Parent
    {
        FeatureLine_Finder lines = new FeatureLine_Finder();
        public CS_FeatureLine_VerticalLongLine(VBtask task) : base(task)
        {
            desc = "Use FeatureLine_Finder data to identify the longest lines and show its angle.";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                dst2 = src.Clone();
                lines.Run(src);
                if (lines.sortedVerticals.Count == 0)
                {
                    SetTrueText("No vertical lines were found", 3);
                    return;
                }
            }
            if (lines.sortedVerticals.Count == 0) return; // nothing found...
            var index = lines.sortedVerticals.ElementAt(0).Value;
            var p1 = lines.lines2D[index];
            var p2 = lines.lines2D[index + 1];
            DrawLine(dst2, p1, p2, task.HighlightColor, task.lineWidth);
            dst3.SetTo(0);
            DrawLine(dst3, p1, p2, task.HighlightColor, task.lineWidth);
            var pt1 = lines.lines3D[index];
            var pt2 = lines.lines3D[index + 1];
            var len3D = distance3D(pt1, pt2);
            var arcY = Math.Abs(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958);
            SetTrueText($"{arcY.ToString(fmt3)}\n{len3D.ToString(fmt3)}m len\n{pt1.Z.ToString(fmt1)}m dist", new cv.Point(p1.X, p1.Y));
            SetTrueText($"{arcY.ToString(fmt3)}\n{len3D.ToString(fmt3)}m len\n{pt1.Z.ToString(fmt1)}m distant", new cv.Point(p1.X, p1.Y), 3);
        }
    }
    public class CS_FeatureLine_DetailsAll : CS_Parent
    {
        FeatureLine_Finder lines = new FeatureLine_Finder();
        Font_FlowText flow = new Font_FlowText();
        List<float> arcList = new List<float>();
        List<float> arcLongAverage = new List<float>();
        List<float> firstAverage = new List<float>();
        int firstBest;
        public CS_FeatureLine_DetailsAll(VBtask task) : base(task)
        {
            flow.dst = 3;
            desc = "Use FeatureLine_Finder data to collect vertical lines and measure accuracy of each.";
        }
        public void RunCS(Mat src)
        {
            if (task.heartBeat)
            {
                dst2 = src.Clone();
                lines.Run(src);
                if (lines.sortedVerticals.Count == 0)
                {
                    SetTrueText("No vertical lines were found", 3);
                    return;
                }
                dst3.SetTo(0);
                arcList.Clear();
                flow.msgs.Clear();
                flow.msgs.Add("ID\tlength\tdistance");
                for (int i = 0; i < Math.Min(10, lines.sortedVerticals.Count); i++)
                {
                    int index = lines.sortedVerticals.ElementAt(i).Value;
                    cv.Point2f p1 = lines.lines2D[index];
                    cv.Point2f p2 = lines.lines2D[index + 1];
                    DrawLine(dst2, p1, p2, task.HighlightColor, task.lineWidth);
                    SetTrueText(i.ToString(), i % 2 == 1 ? new cv.Point(p1.X, p1.Y) : new cv.Point(p2.X, p2.Y), 2);
                    DrawLine(dst3, p1, p2, task.HighlightColor, task.lineWidth);
                    Point3f pt1 = lines.lines3D[index];
                    Point3f pt2 = lines.lines3D[index + 1];
                    float len3D = distance3D(pt1, pt2);
                    if (len3D > 0)
                    {
                        float arcY = Math.Abs((float)(Math.Asin((pt1.Y - pt2.Y) / len3D) * 57.2958));
                        arcList.Add(arcY);
                        flow.msgs.Add($"{arcY:F3}\t{len3D:F3}m\t{pt1.Z:F1}m");
                    }
                }
            }
            flow.Run(null);
            if (arcList.Count == 0) return;
            float mostAccurate = arcList[0];
            firstAverage.Add(mostAccurate);
            foreach (float arc in arcList)
            {
                if (arc > mostAccurate)
                {
                    mostAccurate = arc;
                    break;
                }
            }
            if (mostAccurate == arcList[0]) firstBest++;
            float avg = arcList.Average();
            arcLongAverage.Add(avg);
            labels[3] = $"arcY avg = {avg:F1}, long term average = {arcLongAverage.Average():F1}, " +
                        $"first was best {(float)firstBest / task.frameCount:P0} of the time, " +
                        $"Avg of longest line {firstAverage.Average():F1}";
            if (arcLongAverage.Count > 1000)
            {
                arcLongAverage.RemoveAt(0);
                firstAverage.RemoveAt(0);
            }
        }
    }
    public class CS_FeatureLine_LongestKNN : CS_Parent
    {
        Line_GCloud glines = new Line_GCloud();
        public KNN_ClosestTracker knn = new KNN_ClosestTracker();
        public Options_Features options = new Options_Features();
        public gravityLine gline;
        public Match_Basics match = new Match_Basics();
        cv.Point2f p1, p2;
        public CS_FeatureLine_LongestKNN(VBtask task) : base(task)
        {
            desc = "Find and track the longest line in the BGR image with a lightweight KNN.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2 = src;
            knn.Run(src.Clone());
            p1 = knn.lastPair.p1;
            p2 = knn.lastPair.p2;
            gline = glines.updateGLine(src, gline, new cv.Point(p1.X, p1.Y), new cv.Point(p2.X, p2.Y));
            Rect rect = ValidateRect(new Rect((int)Math.Min(p1.X, p2.X), (int)Math.Min(p1.Y, p2.Y), (int)Math.Abs(p1.X - p2.X) + 2, (int)Math.Abs(p1.Y - p2.Y)));
            match.template = new Mat(src, rect);
            match.Run(src);
            if (match.correlation >= options.correlationMin)
            {
                dst3 = match.dst0.Resize(dst3.Size());
                DrawLine(dst2, p1, p2, task.HighlightColor, task.lineWidth);
                DrawCircle(dst2, p1, task.DotSize, task.HighlightColor);
                DrawCircle(dst2, p2, task.DotSize, task.HighlightColor);
                rect = ValidateRect(new Rect((int)(Math.Min(p1.X, p2.X)), (int)(Math.Min(p1.Y, p2.Y)), (int)(Math.Abs(p1.X - p2.X) + 2), (int)(Math.Abs(p1.Y - p2.Y))));
                match.template = new Mat(src, rect).Clone();
            }
            else
            {
                task.HighlightColor = task.HighlightColor == Scalar.Yellow ? Scalar.Blue : Scalar.Yellow;
                knn.lastPair = new PointPair(new Point2f(), new Point2f());
            }
            labels[2] = $"Longest line end points had correlation of {match.correlation:F3} with the original longest line.";
        }
    }
    public class CS_FeatureLine_Longest : CS_Parent
    {
        Line_GCloud glines = new Line_GCloud();
        public KNN_ClosestTracker knn = new KNN_ClosestTracker();
        public Options_Features options = new Options_Features();
        public gravityLine gline;
        public Match_Basics match1 = new Match_Basics();
        public Match_Basics match2 = new Match_Basics();
        public CS_FeatureLine_Longest(VBtask task) : base(task)
        {
            labels[2] = "Longest line end points are highlighted ";
            desc = "Find and track the longest line in the BGR image with a lightweight KNN.";
        }
        public void RunCS(Mat src)
        {
            options.RunVB();
            dst2 = src.Clone();
            float correlationMin = match1.options.correlationMin;
            int templatePad = match1.options.templatePad;
            int templateSize = match1.options.templateSize;
            cv.Point2f p1 = new cv.Point(), p2 = new cv.Point();
            if (task.heartBeat || (match1.correlation < correlationMin && match2.correlation < correlationMin))
            {
                knn.Run(src.Clone());
                p1 = knn.lastPair.p1;
                Rect r1 = ValidateRect(new Rect((int)(p1.X - templatePad), (int)(p1.Y - templatePad), templateSize, templateSize));
                match1.template = new Mat(src, r1).Clone();
                p2 = knn.lastPair.p2;
                Rect r2 = ValidateRect(new Rect((int)(p2.X - templatePad), (int)(p2.Y - templatePad), templateSize, templateSize));
                match2.template = new Mat(src, r2).Clone();
            }
            match1.Run(src);
            p1 = match1.matchCenter;
            match2.Run(src);
            p2 = match2.matchCenter;
            gline = glines.updateGLine(src, gline, new cv.Point(p1.X, p1.Y), new cv.Point(p2.X, p2.Y));
            DrawLine(dst2, p1, p2, task.HighlightColor, task.lineWidth);
            DrawCircle(dst2, p1, task.DotSize, task.HighlightColor);
            DrawCircle(dst2, p2, task.DotSize, task.HighlightColor);
            SetTrueText($"{match1.correlation:F3}", new cv.Point(p1.X, p1.Y));
            SetTrueText($"{match2.correlation:F3}", new cv.Point(p2.X, p2.Y));
        }
    }



}

