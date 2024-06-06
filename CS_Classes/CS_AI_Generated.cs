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

namespace CS_Classes
{ 
    public class CSharp_AddWeighted_Basics : CS_Parent
    {
        public Mat src2;
        public Options_AddWeighted options = new Options_AddWeighted();

        public CSharp_AddWeighted_Basics(VBtask task) : base(task) 
        {
            //AddAdvice(traceName + ": use the local option slider 'Add Weighted %'");
            desc = "Add 2 images with specified weights.";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();

            Mat srcPlus = src2;
            // algorithm user normally provides src2! 
            if (standaloneTest() || src2 == null) srcPlus = task.depthRGB;
            if (srcPlus.Type() != src.Type())
            {
                if (src.Type() != MatType.CV_8UC3 || srcPlus.Type() != MatType.CV_8UC3)
                {
                    //if (src.Type() == MatType.CV_32FC1) src = vbNormalize32f(src);
                    //if (srcPlus.Type() == MatType.CV_32FC1) srcPlus = vbNormalize32f(srcPlus);
                    if (src.Type() != MatType.CV_8UC3) src = src.CvtColor(ColorConversionCodes.GRAY2BGR);
                    if (srcPlus.Type() != MatType.CV_8UC3) srcPlus = srcPlus.CvtColor(ColorConversionCodes.GRAY2BGR);
                }
            }
            Cv2.AddWeighted(src, options.addWeighted, srcPlus, 1.0 - options.addWeighted, 0, dst2);
            labels[2] = $"Depth %: {100 - options.addWeighted * 100} BGR %: {(int)(options.addWeighted * 100)}";
        }
    }







    public class CSharp_AddWeighted_Edges : CS_Parent
    {
        private Edge_All edges = new Edge_All();
        private CSharp_AddWeighted_Basics addw;

        public CSharp_AddWeighted_Edges(VBtask task) : base(task)
        {
            addw = new CSharp_AddWeighted_Basics(task);
            labels = new string[] { "", "", "Edges_BinarizedSobel output", "AddWeighted edges and BGR image" };
            desc = "Add in the edges separating light and dark to the color image";
        }

        public void RunCS(Mat src)
        {
            edges.Run(src);
            dst2 = edges.dst2;
            labels[2] = edges.labels[2];

            addw.src2 = edges.dst2.CvtColor(ColorConversionCodes.GRAY2BGR);
            addw.RunCS(src);
            dst3 = addw.dst2;
        }
    }






    public class CSharp_AddWeighted_ImageAccumulate : CS_Parent
    {
        private Options_AddWeightedAccum options = new Options_AddWeightedAccum();
        public CSharp_AddWeighted_ImageAccumulate(VBtask task) : base(task)
        {
            desc = "Update a running average of the image";
        }
        public void RunVB(cv.Mat src)
        {
            options.RunVB();

            if (task.optionsChanged)
            {
                dst2 = task.pcSplit[2] * 1000;
            }
            cv.Cv2.AccumulateWeighted(task.pcSplit[2] * 1000, dst2, options.addWeighted, new cv.Mat());
        }
    }







    public class CSharp_AddWeighted_InfraRed : CS_Parent
    {
        private AddWeighted_Basics addw = new AddWeighted_Basics();
        private Mat src2 = new Mat();

        public CSharp_AddWeighted_InfraRed(VBtask task) : base(task)
        {
            desc = "Align the depth data with the left or right view. Oak-D is aligned with the right image. Some cameras are not close to aligned.";
        }

        public void RunVB(Mat src)
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
            addw.Run(task.depthRGB);
            dst2 = addw.dst2.Clone();
        }
    }





    public class CSharp_AlphaChannel_Basics : CS_Parent
    {
        private Form alpha = new Form();

        public CSharp_AlphaChannel_Basics(VBtask task) : base(task)
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
public class CSharp_ApproxPoly_Basics : CS_Parent
    {
        private Contour_Largest contour = new Contour_Largest();
        private Rectangle_Rotated rotatedRect = new Rectangle_Rotated();
        private Options_ApproxPoly options = new Options_ApproxPoly();

        public CSharp_ApproxPoly_Basics(VBtask task) : base(task)
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
                Point[] nextContour;
                nextContour = Cv2.ApproxPolyDP(contour.bestContour, options.epsilon, options.closedPoly);
                dst3.SetTo(Scalar.Black);
                drawContour(dst3, new List<Point>(nextContour), Scalar.Yellow);
            }
            else
            {
                labels[2] = "No contours found";
            }
        }
    }







    public class CSharp_ApproxPoly_FindandDraw : CS_Parent
    {
        private Rectangle_Rotated rotatedRect = new Rectangle_Rotated();
        public Point[][] allContours;
        public CSharp_ApproxPoly_FindandDraw(VBtask task) : base(task)
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

            List<Point[]> contours = new List<Point[]>();
            for (int i = 0; i < allContours.Length; i++)
            {
                Point[] nextContour = Cv2.ApproxPolyDP(allContours[i], 3, true);
                if (nextContour.Length > 2)
                {
                    contours.Add(nextContour);
                }
            }

            Cv2.DrawContours(dst3, contours, -1, new Scalar(0, 255, 255), task.lineWidth, task.lineType);
        }
    }




public class CSharp_ApproxPoly_Hull : CS_Parent
    {
        private Hull_Basics hull = new Hull_Basics();
        private ApproxPoly_Basics aPoly = new ApproxPoly_Basics();
        public CSharp_ApproxPoly_Hull(VBtask task) : base(task)
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




    public class CSharp_Area_MinTriangle_CPP : CS_Parent
    {
        public Mat triangle;
        public Options_MinArea options = new Options_MinArea();
        public List<Point2f> srcPoints;

        [DllImport("CPP_Classes.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void MinTriangle_Run(IntPtr inputPtr, int numberOfPoints, IntPtr outputTriangle);

        public CSharp_Area_MinTriangle_CPP(VBtask task) : base(task)
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
                Point2f pt = triangle.At<Point2f>(i);
                Point p1 = new Point(pt.X, pt.Y);
                pt = triangle.At<Point2f>((i + 1) % 3);
                Point p2 = new Point(pt.X, pt.Y);
                drawLine(dst2, p1, p2, Scalar.Black);
            }

            foreach (var ptSrc in srcPoints)
            {
                var pt = new cv.Point(ptSrc.X, ptSrc.Y);
                drawCircle(dst2, pt, task.dotSize + 1, Scalar.Red);
            }
        }
    }





    public class CSharp_Annealing_Basics_CPP : CS_Parent
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
                drawCircle(dst2, cityPositions[i], task.dotSize, Scalar.White);
                drawLine(dst2, cityPositions[i], cityPositions[cityOrder[i]], Scalar.White);
            }
            setTrueText("Energy" + Environment.NewLine + energy.ToString(fmt0), new Point(10, 100), 2);
        }

        public void setup()
        {
            cityOrder = new int[numberOfCities];

            double radius = dst2.Rows * 0.45;
            Point center = new Point(dst2.Cols / 2, dst2.Rows / 2);
            if (circularPattern)
            {
                cityPositions = new Point2f[numberOfCities];
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

        public CSharp_Annealing_Basics_CPP(VBtask task) : base(task)
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










}


