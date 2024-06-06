using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VB_Classes;
using cv = OpenCvSharp;
using OpenCvSharp;

namespace CS_Classes
{
    public class CS_Parent
    {
        public VBtask task;
        public IntPtr cPtr;
        public bool standalone = true;
        public string desc = "";
        public Mat dst0, dst1, dst2, dst3;
        public string traceName;
        public string[] labels = new string[4]; 
        private List<trueText> trueData = new List<trueText>();
        public const string fmt0 = "0";
        public const string fmt1 = "0.0";
        public const string fmt2 = "0.00";
        public const string fmt3 = "0.000";
        public System.Random msRNG = new System.Random();
        public const string fmt4 = "0.0000"; public CS_Parent(VBtask _task)
        {
            this.task = _task;
            traceName = this.GetType().Name;

            bool standalone = task.callTrace[0] == traceName + "\\"; // only the first is standaloneTest() (the primary algorithm.)
            //if (!standalone && !task.callTrace.Contains(callStack))
            //{
            //    task.callTrace.Add(callStack);
            //}

            dst0 = new Mat(task.workingRes, MatType.CV_8UC3, Scalar.All(0));
            dst1 = new Mat(task.workingRes, MatType.CV_8UC3, Scalar.All(0));
            dst2 = new Mat(task.workingRes, MatType.CV_8UC3, Scalar.All(0));
            dst3 = new Mat(task.workingRes, MatType.CV_8UC3, Scalar.All(0));
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

        public void AddAdvice(string advice)
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

        public void drawContour(Mat dst, List<Point> contour, Scalar color, int lineWidth = -10)
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

        public void drawLine(Mat dst, Point2f p1, Point2f p2, Scalar color)
        {
            var pt1 = new cv.Point(p1.X, p1.Y);
            var pt2 = new cv.Point(p2.X, p2.Y);
            dst.Line(pt1, pt2, color, task.lineWidth, task.lineType);
        }

        public void drawCircle(Mat dst, Point2f p1, int radius, Scalar color)
        {
            var pt = new cv.Point(p1.X, p1.Y);
            dst.Circle(pt, radius, color, -1, task.lineType);
        }

        public void processFrame(Mat src)
        {
            task.labels = labels;

            task.dst0 = task.color;
            task.dst1 = task.depthRGB;
            task.dst2 = dst2;
            task.dst3 = dst3;
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
    }

    public class trueText
    {
        public string Text { get; set; }
        public Point Pt { get; set; }
        public int PicTag { get; set; }

        public trueText(string text, Point pt, int picTag)
        {
            Text = text;
            Pt = pt;
            PicTag = picTag;
        }
    }
}


    public class trueText
    {
        public string text;
        public int picTag = 2;
        public cv.Point pt;

        private void setup(string _text, cv.Point _pt, int camPicIndex)
        {
            text = _text;
            pt = _pt;
            picTag = camPicIndex;
        }

        public trueText(string _text, cv.Point _pt, int camPicIndex)
        {
            setup(_text, _pt, camPicIndex);
        }

        public trueText(string _text, cv.Point _pt)
        {
            setup(_text, _pt, 2);
        }
    }




