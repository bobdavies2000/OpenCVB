using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VB_Classes;
using cv = OpenCvSharp;
using OpenCvSharp;
using System.Windows.Forms;

namespace CS_Classes
{
    public class CS_Parent
    {
        public VBtask task;
        public IntPtr cPtr;
        public bool standalone;
        public string desc = "";
        public Mat dst0, dst1, dst2, dst3, empty;
        public string traceName;
        public string[] labels = new string[4];
        public List<trueText> trueData = new List<trueText>();
        public const string fmt0 = "0";
        public const string fmt1 = "0.0";
        public const string fmt2 = "0.00";
        public const string fmt3 = "0.000";
        public System.Random msRNG = new System.Random();
        public string strOut;
        public const string fmt4 = "0.0000";
        public Controls_Basics controls;
        public CS_Parent(VBtask _task)
        {
            this.task = _task;
            traceName = this.GetType().Name;

            standalone = task.callTrace[0] == traceName + "\\"; // only the first is standaloneTest() (the primary algorithm.)
            //if (!standalone && !task.callTrace.Contains(callStack))
            //{
            //    task.callTrace.Add(callStack);
            //}

            dst0 = new Mat(task.workingRes, MatType.CV_8UC3, Scalar.All(0));
            dst1 = new Mat(task.workingRes, MatType.CV_8UC3, Scalar.All(0));
            dst2 = new Mat(task.workingRes, MatType.CV_8UC3, Scalar.All(0));
            dst3 = new Mat(task.workingRes, MatType.CV_8UC3, Scalar.All(0));

            controls = new Controls_Basics();
        }
        public Mat GetNormalize32f(Mat input)
        {
            Mat outMat = new Mat();
            Cv2.Normalize(input, outMat, 0, 255, NormTypes.MinMax);
            if (input.Channels() == 1)
            {
                outMat.ConvertTo(outMat, MatType.CV_8U);
                return outMat.CvtColor(ColorConversionCodes.GRAY2BGR);
            }
            outMat.ConvertTo(outMat, MatType.CV_8UC3);
            return outMat;
        }
        public Mat RebuildCells(SortedList<int, rcData> sortedCells)
        {
            task.redCells.Clear();
            task.redCells.Add(new rcData());
            foreach (var rc in sortedCells.Values)
            {
                rc.index = task.redCells.Count;
                task.redCells.Add(rc);
                if (rc.index >= 255) break;
            }

            return DisplayCells();
        }
        public Mat DisplayCells()
        {
            Mat dst = new Mat(task.workingRes, MatType.CV_8UC3, Scalar.All(0));
            task.cellMap.SetTo(Scalar.All(0));
            foreach (var rc in task.redCells)
            {
                bool natural = task.redOptions.useNaturalColor;
                dst[rc.rect].SetTo(natural ? rc.naturalColor : rc.color, rc.mask);
                task.cellMap[rc.rect].SetTo(rc.index, rc.mask);
            }
            return dst;
        }

        public bool standaloneTest()
        {
            if (standalone || ShowIntermediate()) return true;
            return false;
        }
        public bool ShowIntermediate()
        {
            //if (task.IntermediateObject == null) return false;
            //if (task.IntermediateObject.TraceName == traceName) return true;
            return false;
        }

        public void UpdateAdvice(string advice)
        {
            if (task.advice.StartsWith("No advice for "))
            {
                task.advice = string.Empty;
            }

            var split = advice.Split(':');
            if (task.advice.Contains(split[0] + ":"))
            {
                return;
            }

            task.advice += advice + Environment.NewLine + Environment.NewLine;
        }
        public mmData GetMinMax(Mat mat, Mat mask = null)
        {
            mmData mm = new mmData();
            if (mask == null)
            {
                Cv2.MinMaxLoc(mat, out mm.minVal, out mm.maxVal, out mm.minLoc, out mm.maxLoc);
            }
            else
            {
                Cv2.MinMaxLoc(mat, out mm.minVal, out mm.maxVal, out mm.minLoc, out mm.maxLoc, mask);
            }
            return mm;
        }

        public void DrawContour(Mat dst, List<Point> contour, Scalar color, int lineWidth = -10)
        {
            if (lineWidth == -10)
            {
                lineWidth = task.lineWidth; // Assuming 'task' is a predefined object with 'lineWidth' property
            }
            if (contour.Count < 3) return; // this is not enough to draw.

            var listOfPoints = new List<List<Point>>();
            listOfPoints.Add(contour);

            Cv2.DrawContours(dst, listOfPoints, -1, color, lineWidth, task.lineType); // Assuming 'task' has 'lineType' property
        }

        public void DrawLine(Mat dst, Point2f p1, Point2f p2, Scalar color)
        {
            var pt1 = new cv.Point(p1.X, p1.Y);
            var pt2 = new cv.Point(p2.X, p2.Y);
            dst.Line(pt1, pt2, color, task.lineWidth, task.lineType);
        }

        public void DrawCircle(Mat dst, Point2f p1, int radius, Scalar color)
        {
            var pt = new cv.Point(p1.X, p1.Y);
            dst.Circle(pt, radius, color, -1, task.lineType);
        }

        public void DrawRotatedRectangle(RotatedRect rotatedRect, Mat dst, Scalar color)
        {
            Point2f[] vertices2f = rotatedRect.Points();
            Point[] vertices = new Point[vertices2f.Length];
            for (int j = 0; j < vertices2f.Length; j++)
            {
                vertices[j] = new Point((int)vertices2f[j].X, (int)vertices2f[j].Y);
            }
            dst.FillConvexPoly(vertices, color, LineTypes.Link8);
        }
        public void DrawRotatedOutline(RotatedRect rotatedRect, Mat dst2, Scalar color)
        {
            Point2f[] pts = rotatedRect.Points();
            Point lastPt = new Point((int)pts[0].X, (int)pts[0].Y);
            for (int i = 1; i <= pts.Length; i++)
            {
                int index = i % pts.Length;
                Point pt = new Point((int)pts[index].X, (int)pts[index].Y);
                Cv2.Line(dst2, pt, lastPt, color);
                lastPt = pt;
            }
        }
        public List<Point2f> QuickRandomPoints(int howMany)
        {
            List<Point2f> srcPoints = new List<Point2f>();
            int w = task.workingRes.Width;
            int h = task.workingRes.Height;
            Random msRNG = new Random();

            for (int i = 0; i < howMany; i++)
            {
                Point2f pt = new Point2f(msRNG.Next(0, w), msRNG.Next(0, h));
                srcPoints.Add(pt);
            }

            return srcPoints;
        }

        public void setTrueText(string text, Point pt, int picTag = 2)
        {
            trueText str = new trueText(text, pt, picTag);
            trueData.Add(str);
        }

        public void setTrueText(string text)
        {
            Point pt = new Point(0, 0);
            int picTag = 2;
            trueText str = new trueText(text, pt, picTag);
            trueData.Add(str);
        }

        public void setTrueText(string text, int picTag)
        {
            Point pt = new Point(0, 0);
            trueText str = new trueText(text, pt, picTag);
            trueData.Add(str);
        }
        public void FindSlider(string opt, int val)
        {
            controls.CS_FindSlider(opt, val);
        }
        public TrackBar FindSlider(string opt)
        {
            return controls.CS_GetSlider(opt);
        }
        public void FindCheckBox(string opt, bool val)
        {
            controls.CS_FindCheckBox(opt, val); 
        }
        public cv.Mat ShowPalette(cv.Mat input)
        {
            if (input.Type() == cv.MatType.CV_32SC1)
            {
                input.ConvertTo(input, cv.MatType.CV_8U);
            }
            task.palette.RunVB(input);
            return task.palette.dst2.Clone();
        }
    }
}




