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
        private CSharp_AddWeighted_Basics addw;
        private Mat src2 = new Mat();

        public CSharp_AddWeighted_InfraRed(VBtask task) : base(task)
        {
            addw = new CSharp_AddWeighted_Basics(task);
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
                        drawCircle(dst2, pt, task.dotSize, Scalar.Yellow);
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
                    drawCircle(dst2, pt, task.dotSize + 2, Scalar.Red);
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
            //csAddAdvice(traceName + ": use the local options for height and width.");
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
            //vbAddAdvice(traceName + ": use goption 'Pixel Difference Threshold' to control changed pixels.");
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
            //vbAddAdvice(traceName + ": the global option 'Histogram Bins' controls the histogram.");
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
                drawLine(dst2, lp.p1, lp.p2, lineColor);
                drawLine(dst3, lp.p1, lp.p2, 255);
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
                    drawLine(dst1, lp.p1, lp.p2, Scalar.White);
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








}


