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
using static CS_Classes.CSharp_Externs;
using OpenCvSharp.XImgProc;

namespace CS_Classes
{ 
    public class CSharp_AddWeighted_Basics : CS_Parent
    {
        public Single weight;
        public Mat src2;
        public Options_AddWeighted options = new Options_AddWeighted();

        public CSharp_AddWeighted_Basics(VBtask task) : base(task) 
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
                    //if (src.Type() == MatType.CV_32FC1) src = vbNormalize32f(src);
                    //if (srcPlus.Type() == MatType.CV_32FC1) srcPlus = vbNormalize32f(srcPlus);
                    if (src.Type() != MatType.CV_8UC3) src = src.CvtColor(ColorConversionCodes.GRAY2BGR);
                    if (srcPlus.Type() != MatType.CV_8UC3) srcPlus = srcPlus.CvtColor(ColorConversionCodes.GRAY2BGR);
                }
            }
            Cv2.AddWeighted(src, weight, srcPlus, 1.0 - weight, 0, dst2);
            labels[2] = $"Depth %: {100 - weight} BGR %: {(int)(weight )}";
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







    public class CSharp_AddWeighted_InfraRed : CS_Parent
    {
        private CSharp_AddWeighted_Basics addw;
        private Mat src2 = new Mat();

        public CSharp_AddWeighted_InfraRed(VBtask task) : base(task)
        {
            addw = new CSharp_AddWeighted_Basics(task);
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
            addw.RunCS(task.depthRGB);
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
                DrawContour(dst3, new List<Point>(nextContour), Scalar.Yellow);
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
                DrawLine(dst2, p1, p2, Scalar.Black);
            }

            foreach (var ptSrc in srcPoints)
            {
                var pt = new cv.Point(ptSrc.X, ptSrc.Y);
                DrawCircle(dst2, pt, task.dotSize + 1, Scalar.Red);
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
                DrawCircle(dst2, cityPositions[i], task.dotSize, Scalar.White);
                DrawLine(dst2, cityPositions[i], cityPositions[cityOrder[i]], Scalar.White);
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





    public class CSharp_Random_Basics : CS_Parent
    {
        public List<Point2f> pointList = new List<Point2f>();
        public Rect range;
        public Options_Random options = new Options_Random();

        public CSharp_Random_Basics(VBtask task) : base(task)
        {
            range = new Rect(0, 0, dst2.Cols, dst2.Rows);
            desc = "Create a uniform random mask with a specified number of pixels.";
        }

        public void RunCS(Mat src)
        {
            int sizeRequest = options.countSlider.Value;
            if (!task.paused)
            {
                pointList.Clear();
                Random msRNG = new Random();
                while (pointList.Count < sizeRequest)
                {
                    pointList.Add(new Point2f(msRNG.Next(range.X, range.X + range.Width),
                                              msRNG.Next(range.Y, range.Y + range.Height)));
                }
                if (standaloneTest())
                {
                    dst2.SetTo(0);
                    foreach (var pt in pointList)
                    {
                        DrawCircle(dst2, pt, task.dotSize, Scalar.Yellow);
                    }
                }
            }
        }
    }







    public class CSharp_Annealing_MultiThreaded_CPP : CS_Parent
    {
        private Options_Annealing options = new Options_Annealing();
        private CSharp_Random_Basics random;
        private CSharp_Annealing_Basics_CPP[] anneal;
        private Mat_4to1 mats = new Mat_4to1();
        private DateTime startTime;
        private void setup()
        {
            random.options.countSlider.Value = options.cityCount;
            random.RunCS(empty); // get the city positions (may or may not be used below.)

            for (int i = 0; i < anneal.Length; i++)
            {
                anneal[i] = new CSharp_Annealing_Basics_CPP(task);
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

        public CSharp_Annealing_MultiThreaded_CPP(VBtask task) : base(task)
        {
            random = new CSharp_Random_Basics(task);
            anneal = new CSharp_Annealing_Basics_CPP[Environment.ProcessorCount / 2];
            labels = new string[] { "", "", "Top 2 are best solutions, bottom 2 are worst.", "Log of Annealing progress" };
            desc = "Setup and control finding the optimal route for a traveling salesman";
        }

        public void RunCS(cv.Mat src)
        {
            options.RunVB();

            if (task.optionsChanged) setup();

            Parallel.For(0, anneal.Length, i =>
            {
                anneal[i].RunCS(src);
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
            setTrueText(strOut, new cv.Point(10, 10), 3);

            mats.mat[0] = anneal[bestList.ElementAt(0).Value].dst2;
            if (bestList.Count >= 2)
            {
                mats.mat[1] = anneal[bestList.ElementAt(1).Value].dst2;
                mats.mat[2] = anneal[bestList.ElementAt(bestList.Count - 2).Value].dst2;
                mats.mat[3] = anneal[bestList.ElementAt(bestList.Count - 1).Value].dst2;
            }
            mats.Run(empty);
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




    public class CSharp_Area_MinMotionRect : CS_Parent
    {
        private BGSubtract_Basics bgSub = new BGSubtract_Basics();

        public CSharp_Area_MinMotionRect(VBtask task) : base(task)
        {
            desc = "Use minRectArea to encompass detected motion";
            labels[2] = "MinRectArea of MOG motion";
        }

        private Mat motionRectangles(Mat gray, Vec3b[] colors)
        {
            Point[][] contours;
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





    public class CSharp_Area_FindNonZero : CS_Parent
    {
        public Mat nonZero;
        public CSharp_Area_FindNonZero(VBtask task) : base(task)
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
                Point[] srcPoints = new Point[100]; // doesn't really matter how many there are.
                Random msRNG = new Random();
                for (int i = 0; i < srcPoints.Length; i++)
                {
                    srcPoints[i].X = msRNG.Next(0, src.Width);
                    srcPoints[i].Y = msRNG.Next(0, src.Height);
                    src.Set<byte>(srcPoints[i].Y, srcPoints[i].X, 255);
                }
            }

            nonZero = src.FindNonZero();

            dst3 = new Mat(src.Size(), MatType.CV_8U, Scalar.All(0));
            // mark the points so they are visible...
            for (int i = 0; i < nonZero.Rows; i++)
            {
                Point pt = nonZero.At<Point>(i);
                Cv2.Circle(dst3, pt, task.dotSize, Scalar.White);
            }

            string outstr = "Coordinates of the non-zero points (ordered by row - top to bottom): \n\n";
            for (int i = 0; i < nonZero.Rows; i++)
            {
                Point pt = nonZero.At<Point>(i);
                outstr += "X = \t" + pt.X + "\t y = \t" + pt.Y + "\n";
                if (i > 100) break; // for when there are way too many points found...
            }
            setTrueText(outstr);
        }
    }





    public class CSharp_Area_SoloPoints : CS_Parent
    {
        private BackProject_SoloTop hotTop = new BackProject_SoloTop();
        private BackProject_SoloSide hotSide = new BackProject_SoloSide();
        private Area_FindNonZero nZero = new Area_FindNonZero();
        public List<Point> soloPoints = new List<Point>();

        public CSharp_Area_SoloPoints(VBtask task) : base(task)
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
                soloPoints.Add(nZero.nonZero.At<Point>(i, 0));
            }

            if (task.heartBeat)
            {
                labels[2] = $"There were {soloPoints.Count} points found";
            }
        }
    }




    public class CSharp_Area_MinRect : CS_Parent
    {
        public RotatedRect minRect;
        Options_MinArea options = new Options_MinArea();
        public List<Point2f> inputPoints = new List<Point2f>();

        public CSharp_Area_MinRect(VBtask task) : base(task)
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
                    DrawCircle(dst2, pt, task.dotSize + 2, Scalar.Red);
                }
                DrawRotatedOutline(minRect, dst2, Scalar.Yellow);
            }
        }
    }




    public class CSharp_AsciiArt_Basics : CS_Parent
    {
        string[] asciiChars = { "@", "%", "#", "*", "+", "=", "-", ":", ",", ".", " " };
        Options_AsciiArt options = new Options_AsciiArt();

        public CSharp_AsciiArt_Basics(VBtask task) : base(task)
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
                    setTrueText(asciiChar, new Point(x * options.wStep, y * options.hStep), 2);
                }
            }
            labels[2] = "Ascii version using " + (dst3.Height * dst3.Width).ToString("N0") + " characters";
        }
    }




    public class CSharp_AsciiArt_Color : CS_Parent
    {
        public CSharp_AsciiArt_Color(VBtask task) : base(task)
        {
            dst3 = new Mat(dst3.Size(), MatType.CV_8U, Scalar.All(0));
            desc = "A palette'd version of the ascii art data";
        }

        public void RunCS(Mat src)
        {
            int hStep = src.Height / 31 - 1;
            int wStep = src.Width / 55 - 1;
            Size size = new Size(55, 31);
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





    public class CSharp_Diff_Basics : CS_Parent
    {
        public int changedPixels;
        public Mat lastFrame;

        public CSharp_Diff_Basics(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "Unstable mask", "" };
            UpdateAdvice(traceName + ": use goption 'Pixel Difference Threshold' to control changed pixels.");
            desc = "Capture an image and compare it to previous frame using absDiff and threshold";
        }

        public void RunCS(Mat src)
        {
            if (src.Channels() != 1)
                src = src.CvtColor(ColorConversionCodes.BGR2GRAY);

            if (task.firstPass)
                lastFrame = src.Clone();

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

            setTrueText(strOut, 3);
        }
    }



    public class CSharp_AsciiArt_Diff : CS_Parent
    {
        private CSharp_AsciiArt_Color colorAA;
        private CSharp_Diff_Basics diff;

        public CSharp_AsciiArt_Diff(VBtask task) : base(task)
        {
            diff = new CSharp_Diff_Basics(task);
            colorAA = new CSharp_AsciiArt_Color(task);
            desc = "Display the instability in image pixels.";
        }

        public void RunCS(Mat src)
        {
            colorAA.RunCS(src);
            dst2 = colorAA.dst2;

            diff.RunCS(dst2.CvtColor(ColorConversionCodes.BGR2GRAY));
            dst3 = diff.dst2;
        }
    }




    public class CSharp_BackProject_Basics : CS_Parent
    {
        public Hist_Kalman histK = new Hist_Kalman();
        public Scalar minRange, maxRange;

        public CSharp_BackProject_Basics(VBtask task) : base(task)
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
                setTrueText("The input image is empty - mm.minVal and mm.maxVal are both zero...");
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



    public class CSharp_BackProject_Full : CS_Parent
    {
        public int classCount;

        public CSharp_BackProject_Full(VBtask task) : base(task)
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




    public class CSharp_BackProject_Reduction : CS_Parent
    {
        private Reduction_Basics reduction = new Reduction_Basics();
        private BackProject_Basics backP = new BackProject_Basics();

        public CSharp_BackProject_Reduction(VBtask task) : base(task)
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






    public class CSharp_BackProject_FeatureLess : CS_Parent
    {
        private BackProject_Basics backP = new BackProject_Basics();
        private Reduction_Basics reduction = new Reduction_Basics();
        private Edge_ColorGap_CPP edges = new Edge_ColorGap_CPP();

        public CSharp_BackProject_FeatureLess(VBtask task) : base(task)
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




    public class CSharp_BackProject_BasicsKeyboard : CS_Parent
    {
        private Keyboard_Basics keys = new Keyboard_Basics();
        private BackProject_Image backP = new BackProject_Image();
        public CSharp_BackProject_BasicsKeyboard(VBtask task) : base(task)
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
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }






    public class CSharp_BackProject_FullLines : CS_Parent
    {
        private BackProject_Full backP = new BackProject_Full();
        private Line_Basics lines = new Line_Basics();

        public CSharp_BackProject_FullLines(VBtask task) : base(task)
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





    public class CSharp_BackProject_PointCloud : CS_Parent
    {
        public Hist_PointCloud hist = new Hist_PointCloud();
        public CSharp_BackProject_PointCloud(VBtask task) : base(task)
        {
            dst2 = new Mat(dst2.Size(), MatType.CV_32FC3, Scalar.All(0));
            labels = new string[] { "", "", "Backprojection after histogram binning X and Z values", "Backprojection after histogram binning Y and Z values" };
            desc = "Explore Backprojection of the cloud histogram.";
        }

        public void RunCS(Mat src)
        {
            int threshold = hist.thresholdSlider.Value;
            hist.Run(src);

            dst0 = hist.dst2.Threshold(threshold, 255, ThresholdTypes.Binary);
            dst1 = hist.dst3.Threshold(threshold, 255, ThresholdTypes.Binary);

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





    public class CSharp_BackProject_Display : CS_Parent
    {
        private BackProject_Full backP = new BackProject_Full();
        public CSharp_BackProject_Display(VBtask task) : base(task)
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


    public class CSharp_BackProject_Unstable : CS_Parent
    {
        private BackProject_Full backP = new BackProject_Full();
        private Diff_Basics diff = new Diff_Basics();

        public CSharp_BackProject_Unstable(VBtask task) : base(task)
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






    public class CSharp_BackProject_FullEqualized : CS_Parent
    {
        private BackProject_Full backP = new BackProject_Full();
        private Hist_EqualizeColor equalize = new Hist_EqualizeColor();

        public CSharp_BackProject_FullEqualized(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "BackProject_Full output without equalization", "BackProject_Full with equalization" };
            desc = "Create a histogram from the equalized color and then backproject it.";
        }

        public void RunCS(Mat src)
        {
            backP.Run(src);
            backP.dst2.ConvertTo(dst2, MatType.CV_8U);
            dst2 = ShowPalette(dst2);

            equalize.Run(src);
            backP.Run(equalize.dst2);

            backP.dst2.ConvertTo(dst3, MatType.CV_8U);
            dst3 = ShowPalette(dst3);
        }
    }




    public class CSharp_Line_Basics : CS_Parent
    {
        private FastLineDetector ld;
        public List<pointPair> lpList = new List<pointPair>();
        public Scalar lineColor = Scalar.White;

        public CSharp_Line_Basics(VBtask task) : base(task)
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

            var sortByLen = new SortedList<float, pointPair>(new compareAllowIdenticalSingleInverted());
            foreach (var v in lines)
            {
                if (v[0] >= 0 && v[0] <= dst2.Cols && v[1] >= 0 && v[1] <= dst2.Rows &&
                    v[2] >= 0 && v[2] <= dst2.Cols && v[3] >= 0 && v[3] <= dst2.Rows)
                {
                    var p1 = new Point(v[0], v[1]);
                    var p2 = new Point(v[2], v[3]);
                    var lp = new pointPair(p1, p2);
                    sortByLen.Add(lp.length, lp);
                }
            }

            dst2 = src;
            dst3.SetTo(0);
            lpList.Clear();
            foreach (var lp in sortByLen.Values)
            {
                lpList.Add(lp);
                DrawLine(dst2, lp.p1, lp.p2, lineColor);
                DrawLine(dst3, lp.p1, lp.p2, 255);
            }
            labels[2] = lpList.Count + " lines were detected in the current frame";
        }
    }





    public class CSharp_BackProject_MaskLines : CS_Parent
    {
        BackProject_Masks masks = new BackProject_Masks();
        CSharp_Line_Basics lines;
        public CSharp_BackProject_MaskLines(VBtask task) : base(task)
        {
            lines = new CSharp_Line_Basics(task);
            if (standaloneTest()) task.gOptions.setDisplay1();
            dst1 = new Mat(dst1.Size(), MatType.CV_8U, Scalar.All(0));
            labels = new string[] { "", "lines detected in the backProjection mask", "Histogram of pixels in a grayscale image.  Move mouse to see lines detected in the backprojection mask",
                                "Yellow is backProjection, lines detected are highlighted" };
            desc = "Inspect the lines from individual backprojection masks from a histogram";
        }

        public void RunCS(Mat src)
        {
            masks.Run(src);
            dst2 = masks.dst2;
            dst3 = src.Clone();

            if (task.heartBeat)
                dst1.SetTo(Scalar.All(0));

            lines.RunCS(masks.mask);
            foreach (var lp in lines.lpList)
            {
                byte val = masks.dst3.At<byte>((int)lp.p1.Y, (int)lp.p1.X);
                if (val == 255)
                    DrawLine(dst1, lp.p1, lp.p2, Scalar.White);
            }
            dst3.SetTo(Scalar.Yellow, masks.mask);
            dst3.SetTo(task.highlightColor, dst1);
        }
    }





    public class CSharp_BackProject_Masks : CS_Parent
    {
        public Hist_Basics hist = new Hist_Basics();
        public int histIndex;
        public Mat mask = new Mat();

        public CSharp_BackProject_Masks(VBtask task) : base(task)
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
                setTrueText("Input data has no values - exit " + traceName);
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





    public class CSharp_BackProject_Side : CS_Parent
    {
        private OpAuto_YRange autoY = new OpAuto_YRange();
        private Projection_HistSide histSide = new Projection_HistSide();

        public CSharp_BackProject_Side(VBtask task) : base(task)
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



    public class CSharp_BackProject_Top : CS_Parent
    {
        private Projection_HistTop histTop = new Projection_HistTop();
        public CSharp_BackProject_Top(VBtask task) : base(task)
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





    public class CSharp_BackProject_Horizontal : CS_Parent
    {
        private BackProject_Top bpTop = new BackProject_Top();
        private BackProject_Side bpSide = new BackProject_Side();

        public CSharp_BackProject_Horizontal(VBtask task) : base(task)
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





    public class CSharp_BackProject_Vertical : CS_Parent
    {
        private BackProject_Top bpTop = new BackProject_Top();
        private BackProject_Side bpSide = new BackProject_Side();

        public CSharp_BackProject_Vertical(VBtask task) : base(task)
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




    public class CSharp_BackProject_SoloSide : CS_Parent
    {
        Projection_HistSide histSide = new Projection_HistSide();

        public CSharp_BackProject_SoloSide(VBtask task) : base(task)
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

    public class CSharp_BackProject_SoloTop : CS_Parent
    {
        Projection_HistTop histTop = new Projection_HistTop();

        public CSharp_BackProject_SoloTop(VBtask task) : base(task)
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



    public class CSharp_BackProject_LineTop : CS_Parent
    {
        Line_ViewTop line = new Line_ViewTop();
        public CSharp_BackProject_LineTop(VBtask task) : base(task)
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

    public class CSharp_BackProject_LineSide : CS_Parent
    {
        Line_ViewSide line = new Line_ViewSide();
        public List<pointPair> lpList = new List<pointPair>();

        public CSharp_BackProject_LineSide(VBtask task) : base(task)
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

    public class CSharp_BackProject_Image : CS_Parent
    {
        public Hist_Basics hist = new Hist_Basics();
        public Mat mask = new Mat();
        Kalman_Basics kalman = new Kalman_Basics();
        public bool useInrange;

        public CSharp_BackProject_Image(VBtask task) : base(task)
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
                setTrueText("The input image is empty - mm.minval and mm.maxVal are both zero...");
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

    public class CSharp_BackProject_Mouse : CS_Parent
    {
        BackProject_Image backP = new BackProject_Image();
        public CSharp_BackProject_Mouse(VBtask task) : base(task)
        {
            labels[2] = "Use the mouse to select what should be shown in the backprojection of the depth histogram";
            desc = "Use the mouse to select what should be shown in the backprojection of the depth histogram";
        }
        public void RunCS(Mat src)
        {
            backP.Run(src);
            dst2 = backP.dst2;
            dst3 = backP.dst3;
        }
    }

    public class CSharp_BackProject_Depth : CS_Parent
    {
        BackProject_Image backp = new BackProject_Image();
        public CSharp_BackProject_Depth(VBtask task) : base(task)
        {
            desc = "Allow review of the depth backprojection";
        }
        public void RunCS(Mat src)
        {
            var depth = task.pcSplit[2].Threshold(task.maxZmeters, 255, ThresholdTypes.TozeroInv);
            backp.Run(depth * 1000);
            dst2 = backp.dst2;
            dst3 = src;
            dst3.SetTo(Scalar.White, backp.mask);
        }
    }

    public class CSharp_BackProject_MeterByMeter : CS_Parent
    {
        Mat histogram = new Mat();
        public CSharp_BackProject_MeterByMeter(VBtask task) : base(task)
        {
            desc = "Backproject the depth data at 1 meter intervals WITHOUT A HISTOGRAM.";
        }
        public void RunCS(Mat src)
        {
            if (task.histogramBins < task.maxZmeters) task.gOptions.setHistogramBins( (int)task.maxZmeters + 1);
            if (task.optionsChanged)
            {
                var incr = task.maxZmeters / task.histogramBins;
                var histData = new List<float>();
                for (int i = 0; i < task.histogramBins; i++)
                {
                    histData.Add((float)Math.Round(i * incr));
                }

                histogram = new Mat(task.histogramBins, 1, MatType.CV_32F, histData.ToArray());
            }
            var ranges = new[] { new Rangef(0, task.maxZmeters) };
            Cv2.CalcBackProject(new[] { task.pcSplit[2] }, new[] { 0 }, histogram, dst1, ranges);

            //dst1.SetTo(task.maxZmeters, task.maxDepthMask);
            dst1.ConvertTo(dst2, MatType.CV_8U);
            dst3 = ShowPalette(dst1);
        }
    }

    public class CSharp_BackProject_Hue : CS_Parent
    {
        OEX_CalcBackProject_Demo1 hue = new OEX_CalcBackProject_Demo1();
        public int classCount;
        public CSharp_BackProject_Hue(VBtask task) : base(task)
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






    public class CSharp_Benford_Basics : CS_Parent
    {
        public float[] expectedDistribution = new float[10];
        public float[] counts;
        Plot_Histogram plot = new Plot_Histogram();
        CSharp_AddWeighted_Basics addW;
        bool use99;

        public CSharp_Benford_Basics(VBtask task) : base(task)
        {
            addW = new CSharp_AddWeighted_Basics(task);
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
            addW.RunCS(dst3);
            dst2 = addW.dst2;

            float wt = addW.weight;
            labels[2] = "AddWeighted: " + wt.ToString("0.0") + " actual vs. " + (1 - wt).ToString("0.0") + " Benford distribution";
        }
    }






    public class CSharp_Benford_NormalizedImage : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        public CSharp_Benford_NormalizedImage(VBtask task) : base(task)
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
    public class CSharp_Benford_NormalizedImage99 : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        public CSharp_Benford_NormalizedImage99(VBtask task) : base(task)
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



    public class CSharp_Benford_Depth : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        public CSharp_Benford_Depth(VBtask task) : base(task)
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



    public class CSharp_Benford_Primes : CS_Parent
    {
        Sieve_BasicsVB sieve = new Sieve_BasicsVB();
        Benford_Basics benford = new Benford_Basics();
        public CSharp_Benford_Primes(VBtask task) : base(task)
        {
            var countSlider = FindSlider("Count of desired primes");
            countSlider.Value = countSlider.Maximum;
            labels = new string[] { "", "", "Actual Distribution of input", "" };
            desc = "Apply Benford to a list of primes";
        }
        public void RunCS(Mat src)
        {
            if (task.optionsChanged)
                sieve.Run(src); // only need to compute this once...
            setTrueText($"Primes found: {sieve.primes.Count}", 3);

            var tmp = new Mat(sieve.primes.Count, 1, MatType.CV_32S, sieve.primes.ToArray());
            tmp.ConvertTo(tmp, MatType.CV_32F);
            benford.Run(tmp);
            dst2 = benford.dst2;
        }
    }






    public class CSharp_Bezier_Basics : CS_Parent
    {
        public Point[] points;

        public CSharp_Bezier_Basics(VBtask task) : base(task)
        {
            points = new Point[]
            {
            new Point(100, 100),
            new Point(150, 50),
            new Point(250, 150),
            new Point(300, 100),
            new Point(350, 150),
            new Point(450, 50)
            };
            UpdateAdvice(traceName + ": Update the public points array variable. No exposed options.");
            desc = "Use n points to draw a Bezier curve.";
        }

        public Point nextPoint(Point[] points, int i, float t)
        {
            double x = Math.Pow(1 - t, 3) * points[i].X +
                       3 * t * Math.Pow(1 - t, 2) * points[i + 1].X +
                       3 * Math.Pow(t, 2) * (1 - t) * points[i + 2].X +
                       Math.Pow(t, 3) * points[i + 3].X;

            double y = Math.Pow(1 - t, 3) * points[i].Y +
                       3 * t * Math.Pow(1 - t, 2) * points[i + 1].Y +
                       3 * Math.Pow(t, 2) * (1 - t) * points[i + 2].Y +
                       Math.Pow(t, 3) * points[i + 3].Y;

            return new Point((int)x, (int)y);
        }

        public void RunCS(Mat src)
        {
            Point p1 = new Point();
            for (int i = 0; i <= points.Length - 4; i += 3)
            {
                for (int j = 0; j <= 100; j++)
                {
                    Point p2 = nextPoint(points, i, j / 100f);
                    if (j > 0) DrawLine(dst2, p1, p2, task.highlightColor);
                    p1 = p2;
                }
            }
            labels[2] = "Bezier output";
        }
    }

    public class CSharp_Bezier_Example : CS_Parent
    {
        Bezier_Basics bezier = new Bezier_Basics();
        public Point[] points;

        public CSharp_Bezier_Example(VBtask task) : base(task)
        {
            points = new Point[] { new Point(task.dotSize, task.dotSize), new Point(dst2.Width / 6, dst2.Width / 6),
                       new Point(dst2.Width * 3 / 4, dst2.Height / 2), new Point(dst2.Width - task.dotSize * 2,
                       dst2.Height - task.dotSize * 2)};
            desc = "Draw a Bezier curve based with the 4 input points.";
        }

        public void RunCS(Mat src)
        {
            dst2.SetTo(Scalar.Black);
            Point p1 = new Point();
            for (int i = 0; i < 100; i++)
            {
                Point p2 = bezier.nextPoint(points, 0, i / 100f);
                if (i > 0) DrawLine(dst2, p1, p2, task.highlightColor);
                p1 = p2;
            }

            for (int i = 0; i < points.Length; i++)
            {
                DrawCircle(dst2, points[i], task.dotSize + 2, Scalar.White);
            }

            DrawLine(dst2, points[0], points[1], Scalar.White);
            DrawLine(dst2, points[2], points[3], Scalar.White);
        }
    }






    public class CSharp_BGRPattern_Basics : CS_Parent
    {
        Denoise_Pixels denoise = new Denoise_Pixels();
        Options_ColorFormat options = new Options_ColorFormat();
        public int classCount;

        public CSharp_BGRPattern_Basics(VBtask task) : base(task)
        {
            cPtr = BGRPattern_Open();
            UpdateAdvice(traceName + ": local options 'Options_ColorFormat' selects color.");
            desc = "Classify each 3-channel input pixel according to their relative values";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            src = options.dst2;

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






    public class CSharp_BGSubtract_Basics : CS_Parent
    {
        public Options_BGSubtract options = new Options_BGSubtract();

        public CSharp_BGSubtract_Basics(VBtask task) : base(task)
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
    public class CSharp_BGSubtract_Basics_QT : CS_Parent
    {
        private double learnRate;

        public CSharp_BGSubtract_Basics_QT(VBtask task) : base(task)
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

    public class CSharp_BGSubtract_MOG2 : CS_Parent
    {
        private BackgroundSubtractorMOG2 MOG2;
        private Options_BGSubtract options = new Options_BGSubtract();

        public CSharp_BGSubtract_MOG2(VBtask task) : base(task)
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

    public class CSharp_BGSubtract_MOG2_QT : CS_Parent
    {
        private BackgroundSubtractorMOG2 MOG2;

        public CSharp_BGSubtract_MOG2_QT(VBtask task) : base(task)
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


    public class CSharp_BGSubtract_MOG : CS_Parent
    {
        BackgroundSubtractorMOG MOG;
        Options_BGSubtract options = new Options_BGSubtract();
        public CSharp_BGSubtract_MOG(VBtask task) : base(task)
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

    public class CSharp_BGSubtract_GMG_KNN : CS_Parent
    {
        BackgroundSubtractorGMG gmg;
        BackgroundSubtractorKNN knn;
        Options_BGSubtract options = new Options_BGSubtract();
        public CSharp_BGSubtract_GMG_KNN(VBtask task) : base(task)
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
                setTrueText("Waiting to get sufficient frames to learn background.  frameCount = " + task.frameCount);
            }
            else
            {
                setTrueText("");
            }

            dst2 = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            gmg.Apply(dst2, dst2, options.learnRate);
            knn.Apply(dst2, dst2, options.learnRate);
        }
    }

    public class CSharp_BGSubtract_MOG_RGBDepth : CS_Parent
    {
        public Mat grayMat = new Mat();
        Options_BGSubtract options = new Options_BGSubtract();
        BackgroundSubtractorMOG MOGDepth;
        BackgroundSubtractorMOG MOGRGB;
        public CSharp_BGSubtract_MOG_RGBDepth(VBtask task) : base(task)
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








    public class CSharp_BGSubtract_MotionDetect : CS_Parent
    {
        private Options_MotionDetect options = new Options_MotionDetect();

        public CSharp_BGSubtract_MotionDetect(VBtask task) : base(task)
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
                setTrueText("No motion detected in any of the regions");
            }
        }
    }






    // https://www.codeproject.com/Articles/215620/Detecting-Manipulations-in-Data-with-Benford-s-Law
    public class CSharp_Benford_JPEG : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        Options_JpegQuality options = new Options_JpegQuality();

        public CSharp_Benford_JPEG(VBtask task) : base(task)
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
    public class CSharp_Benford_JPEG99 : CS_Parent
    {
        public Benford_Basics benford = new Benford_Basics();
        public Options_JpegQuality options = new Options_JpegQuality();

        public CSharp_Benford_JPEG99(VBtask task) : base(task)
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
    public class CSharp_Benford_PNG : CS_Parent
    {
        Options_PNGCompression options = new Options_PNGCompression();
        public Benford_Basics benford = new Benford_Basics();

        public CSharp_Benford_PNG(VBtask task) : base(task)
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



    public class CSharp_BGSubtract_MOG_Retina : CS_Parent
    {
        BGSubtract_MOG bgSub = new BGSubtract_MOG();
        Retina_Basics_CPP retina = new Retina_Basics_CPP();

        public CSharp_BGSubtract_MOG_Retina(VBtask task) : base(task)
        {
            labels = new string[] { "", "", "MOG results of depth motion", "Difference from retina depth motion." };
            desc = "Use the bio-inspired retina algorithm to create a background/foreground using depth.";
        }

        public void RunCS(Mat src)
        {
            retina.Run(task.depthRGB);
            bgSub.Run(retina.dst3.Clone());
            dst2 = bgSub.dst2;
            Cv2.Subtract(bgSub.dst2, retina.dst3, dst3);
        }
    }

    public class CSharp_BGSubtract_DepthOrColorMotion : CS_Parent
    {
        public Diff_UnstableDepthAndColor motion = new Diff_UnstableDepthAndColor();

        public CSharp_BGSubtract_DepthOrColorMotion(VBtask task) : base(task)
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

    public class CSharp_BGSubtract_Video : CS_Parent
    {
        BGSubtract_Basics bgSub = new BGSubtract_Basics();
        Video_Basics video = new Video_Basics();

        public CSharp_BGSubtract_Video(VBtask task) : base(task)
        {
            video.srcVideo = task.homeDir + "opencv/Samples/Data/vtest.avi";
            desc = "Demonstrate all background subtraction algorithms in OpenCV using a video instead of camera.";
        }

        public void RunCS(Mat src)
        {
            video.Run(src);
            dst3 = video.dst2;
            bgSub.Run(dst3);
            dst2 = bgSub.dst2;
        }
    }

    public class CSharp_BGSubtract_Synthetic_CPP : CS_Parent
    {
        Options_BGSubtractSynthetic options = new Options_BGSubtractSynthetic();

        public CSharp_BGSubtract_Synthetic_CPP(VBtask task) : base(task)
        {
            labels[2] = "Synthetic background/foreground image.";
            desc = "Generate a synthetic input to background subtraction method";
        }

        public void RunCS(Mat src)
        {
            options.RunVB();
            if (task.optionsChanged)
            {
                if (!task.firstPass) BGSubtract_Synthetic_Close(cPtr);

                byte[] dataSrc = new byte[src.Total() * src.ElemSize()];
                Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length);
                GCHandle handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned);

                cPtr = BGSubtract_Synthetic_Open(handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                                 task.homeDir + "opencv/Samples/Data/baboon.jpg",
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

    public class CSharp_BGSubtract_Synthetic : CS_Parent
    {
        BGSubtract_Basics bgSub = new BGSubtract_Basics();
        BGSubtract_Synthetic_CPP synth = new BGSubtract_Synthetic_CPP();

        public CSharp_BGSubtract_Synthetic(VBtask task) : base(task)
        {
            desc = "Demonstrate background subtraction algorithms with synthetic images";
        }

        public void RunCS(Mat src)
        {
            synth.Run(src);
            dst3 = synth.dst2;
            bgSub.Run(dst3);
            dst2 = bgSub.dst2;
        }
    }



    public class CSharp_BGSubtract_Reduction : CS_Parent
    {
        private Reduction_Basics reduction = new Reduction_Basics();
        private BGSubtract_Basics bgSub = new BGSubtract_Basics();

        public CSharp_BGSubtract_Reduction(VBtask task) : base(task)
        {
            desc = "Use BGSubtract with the output of a reduction";
        }

        public void RunCS(Mat src)
        {
            reduction.Run(src);

            dst2 = ShowPalette(reduction.dst2.Clone());

            bgSub.Run(dst2);
            dst3 = bgSub.dst2.Clone();

            labels[3] = "Count nonzero = " + dst3.CountNonZero().ToString();
        }
    }





    public class CSharp_Bin2Way_Basics : CS_Parent
    {
        public Hist_Basics hist = new Hist_Basics();
        public Mat_4Click mats = new Mat_4Click();
        public float fraction;

        public CSharp_Bin2Way_Basics(VBtask task) : base(task)
        {
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
            Cv2.Line(dst3, new Point((int)offset, 0), new Point((int)offset, dst3.Height), Scalar.White);

            mats.mat[0] = src.InRange(0, halfSplit - 1); // darkest
            mats.mat[1] = src.InRange(halfSplit, 255);   // lightest

            if (standaloneTest())
            {
                mats.Run(Mat.Zeros(src.Size(), MatType.CV_8UC1));
                dst2 = mats.dst2;
            }
        }
    }

    public class CSharp_Bin2Way_KMeans : CS_Parent
    {
        public Bin2Way_Basics bin2 = new Bin2Way_Basics();
        KMeans_Dimensions kmeans = new KMeans_Dimensions();
        Mat_4Click mats = new Mat_4Click();

        public CSharp_Bin2Way_KMeans(VBtask task) : base(task)
        {
            //FindSlider("KMeans k").Value = 2;
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

    public class CSharp_Bin2Way_RedCloudDarkest : CS_Parent
    {
        Bin2Way_RecurseOnce bin2 = new Bin2Way_RecurseOnce();
        Flood_BasicsMask flood = new Flood_BasicsMask();

        public CSharp_Bin2Way_RedCloudDarkest(VBtask task) : base(task)
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

    public class CSharp_Bin2Way_RedCloudLightest : CS_Parent
    {
        Bin2Way_RecurseOnce bin2 = new Bin2Way_RecurseOnce();
        Flood_BasicsMask flood = new Flood_BasicsMask();

        public CSharp_Bin2Way_RedCloudLightest(VBtask task) : base(task)
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


































































































}




