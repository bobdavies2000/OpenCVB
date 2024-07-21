using System;
using System.Collections.Generic;
using System.Linq;
using VB_Classes;
using cv = OpenCvSharp;
using OpenCvSharp;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CS_Classes
{
    public class CS_Parent
    {
        public VBtask task;
        public IntPtr cPtr;
        public bool standalone;
        public string desc = "";
        public cv.Mat dst0, dst1, dst2, dst3, empty;
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
        public VB_Controls_CSharp controls = new VB_Controls_CSharp();
        public const int RESULT_DST0 = 0;
        public const int RESULT_DST1 = 1;
        public const int RESULT_DST2 = 2;
        public const int RESULT_DST3 = 3;
        public Vec3b black = new Vec3b(0, 0, 0);
        public Vec3b white = new Vec3b(255, 255, 255);
        public Vec3b grayColor = new Vec3b(127, 127, 127);
        public Vec3b yellow = new Vec3b(0, 255, 255);
        public Vec3b purple = new Vec3b(255, 0, 255);
        public Vec3b teal = new Vec3b(255, 255, 0);
        public Vec3b red = new Vec3b(0, 0, 255);
        public Vec3b green = new Vec3b(0, 255, 0);
        public Vec3b blue = new Vec3b(255, 0, 0);
        string callStack = "";
        public CS_Parent(VBtask _task)
        {
            this.task = _task;
            traceName = this.GetType().Name;

            dst0 = new cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, Scalar.All(0));
            dst1 = new cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, Scalar.All(0));
            dst2 = new cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, Scalar.All(0));
            dst3 = new cv.Mat(task.WorkingRes, cv.MatType.CV_8UC3, Scalar.All(0));

            traceName = this.GetType().Name;
            string[] labels = { "", "", traceName, "" };
            string stackTrace = Environment.StackTrace;
            string[] lines = stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
                int offset = lines[i].IndexOf("CS_Classes.");
                if (offset > 0)
                {
                    string partLine = lines[i].Substring(offset + 11);
                    if (partLine.StartsWith("AlgorithmList.createCSAlgorithm")) break;
                    string[] split = partLine.Split('\\');
                    partLine = partLine.Substring(0, partLine.IndexOf('.'));
                    if (!(partLine.StartsWith("CS_Parent") || partLine.StartsWith("VBtask")))
                    {
                        callStack = partLine + "\\" + callStack;
                    }
                }
            }
            callStack = callStack.Replace("CSAlgorithmList\\", "");
            standalone = controls.buildCallStack(traceName, callStack);
        }
        [DllImport("gdi32.dll", EntryPoint = "BitBlt")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);
        public Bitmap GetWindowImage(IntPtr WindowHandle, cv.Rect rect)
        {
            Bitmap b = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics img = Graphics.FromImage(b))
            {
                IntPtr ImageHDC = img.GetHdc();
                using (Graphics window = Graphics.FromHwnd(WindowHandle))
                {
                    IntPtr WindowHDC = window.GetHdc();
                    BitBlt(ImageHDC, 0, 0, rect.Width, rect.Height, WindowHDC, rect.X, rect.Y, (uint)CopyPixelOperation.SourceCopy);
                    window.ReleaseHdc();
                }
                img.ReleaseHdc();
            }

            return b;
        }

        public cv.Scalar vecToScalar(cv.Vec3b v)
        {
            return new cv.Scalar(v.Item0, v.Item1, v.Item2);
        }

        public void DrawRotatedRectangle(cv.RotatedRect rotatedRect, cv.Mat dst, cv.Scalar color)
        {
            cv.Point2f[] vertices2f = rotatedRect.Points();
            cv.Point[] vertices = new cv.Point[vertices2f.Length];
            for (int j = 0; j < vertices2f.Length; j++)
            {
                vertices[j] = new cv.Point((int)vertices2f[j].X, (int)vertices2f[j].Y);
            }
            dst.FillConvexPoly(vertices, color, task.lineType);
        }

        public void AddPlotScale(cv.Mat dst, double minVal, double maxVal, int lineCount = 3)
        {
            // draw a scale along the side
            int spacer = (int)(dst.Height / (lineCount + 1));
            int spaceVal = (int)((maxVal - minVal) / (lineCount + 1));
            if (lineCount > 1)
            {
                if (spaceVal < 1)
                {
                    spaceVal = 1;
                }
            }
            if (spaceVal > 10)
            {
                spaceVal += spaceVal % 10;
            }
            for (int i = 0; i <= lineCount; i++)
            {
                cv.Point p1 = new cv.Point(0, spacer * i);
                cv.Point p2 = new cv.Point(dst.Width, spacer * i);
                dst.Line(p1, p2, cv.Scalar.White, task.cvFontThickness);
                double nextVal = (maxVal - spaceVal * i);
                string nextText = (maxVal > 1000) ? (nextVal / 1000).ToString() + "k" : nextVal.ToString(fmt2);
            cv.Cv2.PutText(dst, nextText, p1, cv.HersheyFonts.HersheyPlain, task.cvFontSize, cv.Scalar.White, task.cvFontThickness, task.lineType);
        }
    }

    public void DrawLine(cv.Mat dst, cv.Point2f p1, cv.Point2f p2, cv.Scalar color, int lineWidth)
    {
        cv.Point pt1 = new cv.Point((int)p1.X, (int)p1.Y);
        cv.Point pt2 = new cv.Point((int)p2.X, (int)p2.Y);
        dst.Line(pt1, pt2, color, task.lineWidth);
    }
    public Rangef[] GetHist2Dminmax(Mat input, int chan1, int chan2)
    {
        float histDelta = 0.00001f;
        if (input.Type() == MatType.CV_8UC3)
        {
            // ranges are exclusive in OpenCV 
            return new Rangef[]
            {
                new Rangef(-histDelta, 256),
                new Rangef(-histDelta, 256)
            };
        }

        var xInput = input.ExtractChannel(chan1);
        var yInput = input.ExtractChannel(chan2);

        var mmX = GetMinMax(xInput);
        var mmY = GetMinMax(yInput);

        // ranges are exclusive in OpenCV 
        return new Rangef[]
        {
            new Rangef((float)(mmX.minVal - histDelta), (float)(mmX.maxVal + histDelta)),
            new Rangef((float)(mmY.minVal - histDelta), (float)(mmY.maxVal + histDelta))
        };
    }

        public static OpenCvSharp.Point GetMaxDist(ref rcData rc)
    {
        using (var mask = rc.mask.Clone())
        {
            mask.Rectangle(new OpenCvSharp.Rect(0, 0, mask.Width, mask.Height), 0, 1);
            using (cv.Mat distance32f = mask.DistanceTransform(OpenCvSharp.DistanceTypes.L1, 0))
            {
                    mmData mm;
                    distance32f.MinMaxLoc(out mm.minVal, out mm.maxVal, out mm.minLoc, out mm.maxLoc);
                    mm.maxLoc.X += rc.rect.X;
                    mm.maxLoc.Y += rc.rect.Y;
                    return mm.maxLoc;
            }
        }
    }
    public mmData GetMinMax(Mat mat, cv.Mat mask = null)
    {
        mmData mm;
        if (mask == null)
        {
            mat.MinMaxLoc(out mm.minVal, out mm.maxVal, out mm.minLoc, out mm.maxLoc);
        }
        else
        {
            mat.MinMaxLoc(out mm.minVal, out mm.maxVal, out mm.minLoc, out mm.maxLoc, mask);
        }
        return mm;
    }

    public cv.Point2f IntersectTest(cv.Point2f p1, cv.Point2f p2, cv.Point2f p3, cv.Point2f p4, cv.Rect rect)
    {
        cv.Point2f x = p3 - p1;
        cv.Point2f d1 = p2 - p1;
        cv.Point2f d2 = p4 - p3;
        float cross = d1.X * d2.Y - d1.Y * d2.X;
        if (Math.Abs(cross) < 0.000001)
        {
            return new cv.Point2f();
        }
        float t1 = (x.X * d2.Y - x.Y * d2.X) / cross;
        cv.Point2f pt = p1 + d1 * t1;
        // If pt.X >= rect.Width Or pt.Y >= rect.Height Then Return New cv.Point2f
        return pt;
    }

    public cv.Mat PrepareDepthInput(int index)
    {
        if (task.useGravityPointcloud)
        {
            return task.pcSplit[index]; // already oriented to gravity
        }

        // rebuild the pointcloud so it is oriented to gravity.
        cv.Mat pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat().Reshape(3, task.pointCloud.Rows);
        cv.Mat[] split = pc.Split();
        return split[index];
    }

    public cv.Mat GetNormalize32f(cv.Mat Input)
    {
        cv.Mat outMat = Input.Normalize(0, 255, cv.NormTypes.MinMax);
        if (Input.Channels() == 1)
        {
            outMat.ConvertTo(outMat, cv.MatType.CV_8U);
            return outMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR);
        }
        outMat.ConvertTo(outMat, cv.MatType.CV_8UC3);
        return outMat;
    }

    public float distance3D(cv.Point3f p1, cv.Point3f p2)
    {
        return (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z));
    }

    public float distance3D(cv.Vec3b p1, cv.Vec3b p2)
    {
        return (float)Math.Sqrt((p1.Item0 - p2.Item0) * (p1.Item0 - p2.Item0) + (p1.Item1 - p2.Item1) * (p1.Item1 - p2.Item1) + (p1.Item2 - p2.Item2) * (p1.Item2 - p2.Item2));
    }

    public float distance3D(cv.Point3i p1, cv.Point3i p2)
    {
        return (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z));
    }

    public float distance3D(cv.Scalar p1, cv.Scalar p2)
    {
        return (float)Math.Sqrt((p1[0] - p2[0]) * (p1[0] - p2[0]) + (p1[1] - p2[1]) * (p1[1] - p2[1]) + (p1[2] - p2[2]) * (p1[2] - p2[2]));
    }

        public void DrawFatLine(Point2f p1, Point2f p2, Mat dst, Scalar fatColor)
        {
            int pad = 2;
            if (task.WorkingRes.Width >= 640) pad = 6;

            DrawLine(dst, p1, p2, fatColor, task.lineWidth + pad);
            DrawLine(dst, p1, p2, Scalar.Black, task.lineWidth);
        }

        public void DrawFPoly(ref cv.Mat dst, List<cv.Point2f> poly, cv.Scalar color)
    {
        int minMod = Math.Min(poly.Count, task.polyCount);
        for (int i = 0; i < minMod; i++)
        {
                var p1 = new cv.Point((int)poly[i].X, (int)poly[i].Y);
                var p2 = new cv.Point((int)poly[(i+1) % minMod].X, (int)poly[(i+1) % minMod].Y);
                DrawLine(dst, p1, poly[(i + 1) % minMod], color, task.lineWidth);
        }
    }

    public List<cv.Point> contourBuild(cv.Mat mask, cv.ContourApproximationModes approxMode)
    {
        cv.Point[][] allContours;
        cv.HierarchyIndex[] test;
        cv.Cv2.FindContours(mask, out allContours, out test, cv.RetrievalModes.External, approxMode);

        int maxCount = 0, maxIndex = 0;
        for (int i = 0; i < allContours.Length; i++)
        {
            int len = allContours[i].Length;
            if (len > maxCount)
            {
                maxCount = len;
                maxIndex = i;
            }
        }
        if (allContours.Length > 0)
        {
            return new List<cv.Point>(allContours[maxIndex].ToList());
        }
        return new List<cv.Point>();
    }

    public void setPointCloudGrid()
    {
        task.gOptions.setGridSize(8);
        if (task.WorkingRes.Width == 640)
        {
            task.gOptions.setGridSize(16);
        }
        else if (task.WorkingRes.Width == 1280)
        {
            task.gOptions.setGridSize(32);
        }
    }

    public string gMatrixToStr(cv.Mat gMatrix)
    {
        string outStr = "Gravity transform matrix" + "\n";
        for (int i = 0; i < gMatrix.Rows; i++)
        {
            for (int j = 0; j < gMatrix.Cols; j++)
            {
                outStr += gMatrix.Get<float>(j, i).ToString(fmt3) + "\t";
            }
            outStr += "\n";
        }

        return outStr;
    }

    public cv.Vec3b randomCellColor()
    {
        return new cv.Vec3b((byte)msRNG.Next(50, 240), (byte)msRNG.Next(50, 240), (byte)msRNG.Next(50, 240)); // trying to avoid extreme colors... 
    }

    public cv.Point validContourPoint(rcData rc, cv.Point pt, int offset)
    {
        if (pt.X < rc.rect.Width && pt.Y < rc.rect.Height)
        {
            return pt;
        }
        int count = rc.contour.Count;
        for (int i = offset + 1; i < rc.contour.Count; i++)
        {
            pt = rc.contour[i % count];
            if (pt.X < rc.rect.Width && pt.Y < rc.rect.Height)
            {
                return pt;
            }
        }
        return new cv.Point();
    }

    public cv.Vec4f build3PointEquation(rcData rc)
    {
        if (rc.contour.Count < 3)
        {
            return new cv.Vec4f();
        }
        int offset = rc.contour.Count / 3;
        cv.Point p1 = validContourPoint(rc, rc.contour[offset * 0], offset * 0);
        cv.Point p2 = validContourPoint(rc, rc.contour[offset * 1], offset * 1);
        cv.Point p3 = validContourPoint(rc, rc.contour[offset * 2], offset * 2);

        cv.Point3f v1 = task.pointCloud.Get<cv.Point3f>(rc.rect.Y + p1.Y, rc.rect.X + p1.X);
        cv.Point3f v2 = task.pointCloud.Get<cv.Point3f>(rc.rect.Y + p2.Y, rc.rect.X + p2.X);
        cv.Point3f v3 = task.pointCloud.Get<cv.Point3f>(rc.rect.Y + p3.Y, rc.rect.X + p3.X);

        cv.Point3f cross = crossProduct(v1 - v2, v2 - v3);
        float k = -(v1.X * cross.X + v1.Y * cross.Y + v1.Z * cross.Z);
        return new cv.Vec4f(cross.X, cross.Y, cross.Z, k);
    }

    public cv.Vec4f fitDepthPlane(List<cv.Point3f> fitDepth)
    {
        cv.Mat wDepth = new cv.Mat(fitDepth.Count, 1, cv.MatType.CV_32FC3, fitDepth.ToArray());
        cv.Scalar columnSum = wDepth.Sum();
        double count = (double)fitDepth.Count;
        cv.Vec4f plane = new cv.Vec4f();
        cv.Scalar centroid = new cv.Scalar(0);
        if (count > 0)
        {
            centroid = new cv.Scalar((float)(columnSum[0] / count), (float)(columnSum[1] / count), (float)(columnSum[2] / count));
            wDepth = wDepth.Subtract(centroid);
            double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
            for (int i = 0; i < wDepth.Rows; i++)
            {
                cv.Point3f tmp = wDepth.Get<cv.Point3f>(i, 0);
                xx += tmp.X * tmp.X;
                xy += tmp.X * tmp.Y;
                xz += tmp.X * tmp.Z;
                yy += tmp.Y * tmp.Y;
                yz += tmp.Y * tmp.Z;
                zz += tmp.Z * tmp.Z;
            }

            double det_x = yy * zz - yz * yz;
            double det_y = xx * zz - xz * xz;
            double det_z = xx * yy - xy * xy;

            double det_max = Math.Max(det_x, det_y);
            det_max = Math.Max(det_max, det_z);

            if (det_max == det_x)
            {
                plane[0] = 1;
                plane[1] = (float)((xz * yz - xy * zz) / det_x);
                plane[2] = (float)((xy * yz - xz * yy) / det_x);
            }
            else if (det_max == det_y)
            {
                plane[0] = (float)((yz * xz - xy * zz) / det_y);
                plane[1] = 1;
                plane[2] = (float)((xy * xz - yz * xx) / det_y);
            }
            else
            {
                plane[0] = (float)((yz * xy - xz * yy) / det_z);
                plane[1] = (float)((xz * xy - yz * xx) / det_z);
                plane[2] = 1;
            }
        }

        double magnitude = Math.Sqrt(plane[0] * plane[0] + plane[1] * plane[1] + plane[2] * plane[2]);
        cv.Scalar normal = new cv.Scalar((float)(plane[0] / magnitude), (float)(plane[1] / magnitude), (float)(plane[2] / magnitude));
        return new cv.Vec4f((float)normal[0], (float)normal[1], (float)normal[2], 
                            (float)-(normal[0] * centroid[0] + normal[1] * centroid[1] + normal[2] * centroid[2]));
    }

    // http://james-ramsden.com/calculate-the-cross-product-c-code/
    public cv.Point3f crossProduct(cv.Point3f v1, cv.Point3f v2)
    {
        cv.Point3f product = new cv.Point3f();
        product.X = v1.Y * v2.Z - v1.Z * v2.Y;
        product.Y = v1.Z * v2.X - v1.X * v2.Z;
        product.Z = v1.X * v2.Y - v1.Y * v2.X;

        if (float.IsNaN(product.X) || float.IsNaN(product.Y) || float.IsNaN(product.Z))
        {
            return new cv.Point3f(0, 0, 0);
        }
        double magnitude = Math.Sqrt(product.X * product.X + product.Y * product.Y + product.Z * product.Z);
        if (magnitude == 0)
        {
            return new cv.Point3f(0, 0, 0);
        }
        return new cv.Point3f((float)(product.X / magnitude), (float)(product.Y / magnitude), (float)(product.Z / magnitude));
    }

    public float dotProduct3D(cv.Point3f v1, cv.Point3f v2)
    {
        return Math.Abs(v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z);
    }

    public cv.Point3f getWorldCoordinates(cv.Point3f p)
    {
        float x = (p.X - task.calibData.ppx) / task.calibData.fx;
        float y = (p.Y - task.calibData.ppy) / task.calibData.fy;
        return new cv.Point3f(x * p.Z, y * p.Z, p.Z);
    }

    public cv.Vec6f getWorldCoordinatesD6(cv.Point3f p)
    {
        float x = (p.X - task.calibData.ppx) / task.calibData.fx;
        float y = (p.Y - task.calibData.ppy) / task.calibData.fy;
        return new cv.Vec6f(x * p.Z, y * p.Z, p.Z, p.X, p.Y, 0);
    }

    
    public List<cv.Point> ContourBuild(Mat mask, ContourApproximationModes approxMode)
        {
            cv.Point[][] allContours;
            Cv2.FindContours(mask, out allContours, out _, RetrievalModes.External, approxMode);

            int maxCount = 0, maxIndex = 0;
            for (int i = 0; i < allContours.Length; i++)
            {
                int len = allContours[i].Length;
                if (len > maxCount)
                {
                    maxCount = len;
                    maxIndex = i;
                }   
            }
            if (allContours.Length > 0)
                return new List<cv.Point>(allContours[maxIndex].ToList());
            return new List<cv.Point>();
        }
        public Mat Show_HSV_Hist(Mat hist)
        {
            Mat img = new Mat(task.WorkingRes, MatType.CV_8UC3, Scalar.All(0));
            int binCount = hist.Height;
            int binWidth = img.Width / hist.Height;
            mmData mm = GetMinMax(hist);
            img.SetTo(Scalar.All(0));

            if (mm.maxVal > 0)
            {
                for (int i = 0; i < binCount - 1; i++)
                {
                    double h = img.Height * hist.At<float>(i, 0) / mm.maxVal;
                    if (h == 0) h = 5; // show the color range in the plot
                    Cv2.Rectangle(img, new Rect(i * binWidth, img.Height - (int)h, binWidth, (int)h),
                                  new Scalar(180.0 * i / binCount, 255, 255), -1);
                }
            }
            return img;
        }

        public cv.Rect ValidateRect(cv.Rect r, int ratio = 1)
        {
            if (r.Width <= 0) r.Width = 1;
            if (r.Height <= 0) r.Height = 1;
            if (r.X < 0) r.X = 0;
            if (r.Y < 0) r.Y = 0;
            if (r.X > task.WorkingRes.Width * ratio) r.X = task.WorkingRes.Width * ratio - 1;
            if (r.Y > task.WorkingRes.Height * ratio) r.Y = task.WorkingRes.Height * ratio - 1;
            if (r.X + r.Width > task.WorkingRes.Width * ratio) r.Width = task.WorkingRes.Width * ratio - r.X;
            if (r.Y + r.Height > task.WorkingRes.Height * ratio) r.Height = task.WorkingRes.Height * ratio - r.Y;
            if (r.Width <= 0) r.Width = 1; // check again (it might have changed.)
            if (r.Height <= 0) r.Height = 1;
            if (r.X == task.WorkingRes.Width * ratio) r.X = r.X - 1;
            if (r.Y == task.WorkingRes.Height * ratio) r.Y = r.Y - 1;
            return r;
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
            Mat dst = new Mat(task.WorkingRes, MatType.CV_8UC3, Scalar.All(0));
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
        public cv.Rect InitRandomRect(int margin)
        {
            return new cv.Rect(
                msRNG.Next(margin, dst2.Width - 2 * margin),
                msRNG.Next(margin, dst2.Height - 2 * margin),
                msRNG.Next(margin, dst2.Width - 2 * margin),
                msRNG.Next(margin, dst2.Height - 2 * margin)
            );
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

        public void DrawContour(Mat dst, List<cv.Point> contour, Scalar color, int lineWidth = -10)
        {
            if (lineWidth == -10)
            {
                lineWidth = task.lineWidth; // Assuming 'task' is a predefined object with 'lineWidth' property
            }
            if (contour.Count < 3) return; // this is not enough to draw.

            var listOfPoints = new List<List<cv.Point>>();
            listOfPoints.Add(contour);

            Cv2.DrawContours(dst, listOfPoints, -1, color, lineWidth, task.lineType); // Assuming 'task' has 'lineType' property
        }

        public void DrawCircle(Mat dst, Point2f p1, int radius, Scalar color, int lineWidth = -1)
        {
            var pt = new cv.Point(p1.X, p1.Y);
            dst.Circle(pt, radius, color, lineWidth, task.lineType);
        }

        public void DrawRotatedOutline(RotatedRect rotatedRect, Mat dst2, Scalar color)
        {
            Point2f[] pts = rotatedRect.Points();
            cv.Point lastPt = new cv.Point((int)pts[0].X, (int)pts[0].Y);
            for (int i = 1; i <= pts.Length; i++)
            {
                int index = i % pts.Length;
                cv.Point pt = new cv.Point((int)pts[index].X, (int)pts[index].Y);
                Cv2.Line(dst2, pt, lastPt, color);
                lastPt = pt;
            }
        }
        public List<Point2f> QuickRandomPoints(int howMany)
        {
            List<Point2f> srcPoints = new List<Point2f>();
            int w = task.WorkingRes.Width;
            int h = task.WorkingRes.Height;
            Random msRNG = new Random();

            for (int i = 0; i < howMany; i++)
            {
                Point2f pt = new Point2f(msRNG.Next(0, w), msRNG.Next(0, h));
                srcPoints.Add(pt);
            }

            return srcPoints;
        }
        public void SetTrueText(string text, cv.Point pt, int picTag = 2)
        {
            trueText str = new trueText(text, pt, picTag);
            trueData.Add(str);
        }
        public void SetTrueText(string text, cv.Point2f pt, int picTag = 2)
        {
            trueText str = new trueText(text, new cv.Point(pt.X, pt.Y), picTag);
            trueData.Add(str);
        }

        public void SetTrueText(string text)
        {
            cv.Point pt = new cv.Point(0, 0);
            int picTag = 2;
            trueText str = new trueText(text, pt, picTag);
            trueData.Add(str);
        }

        public void SetTrueText(string text, int picTag)
        {
            cv.Point pt = new cv.Point(0, 0);
            trueText str = new trueText(text, pt, picTag);
            trueData.Add(str);
        }
        public void SetSlider(string opt, int val)
        {
            controls.CS_SetSlider(opt, val);
        }
        public System.Windows.Forms.TrackBar FindSlider(string opt)
        {
            return controls.CS_GetSlider(opt);
        }
        public System.Windows.Forms.CheckBox FindCheckBox(string opt)
        {
            return controls.CS_FindCheckBox(opt);
        }
        public System.Windows.Forms.RadioButton FindRadio(string opt)
        {
            return controls.CS_FindRadio(opt);
        }
        public void RunAndMeasure(Mat src, Object csCode)
        {
            controls.RunFromVB(src, csCode);
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

        public void DrawContour(ref Mat dst, List<cv.Point> contour, Scalar color, int lineWidth = -10)
        {
            if (lineWidth == -10) lineWidth = task.lineWidth;
            if (contour.Count < 3) return; // Not enough points to draw
            var listOfPoints = new List<List<cv.Point>> { contour };
            Cv2.DrawContours(dst, listOfPoints, -1, color, lineWidth, task.lineType);
        }
        public void DetectFace(ref Mat src, CascadeClassifier cascade)
        {
            Mat gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            Rect[] faces = cascade.DetectMultiScale(gray, 1.08, 3, HaarDetectionTypes.ScaleImage, new cv.Size(30, 30));

            foreach (Rect fface in faces)
            {
                Cv2.Rectangle(src, fface, Scalar.Red, task.lineWidth, task.lineType);
            }
        }
        public void HoughShowLines(ref Mat dst, LineSegmentPolar[] segments, int desiredCount)
        {
            for (int i = 0; i < Math.Min(segments.Length, desiredCount); i++)
            {
                float rho = segments[i].Rho;
                float theta = segments[i].Theta;

                double a = Math.Cos(theta);
                double b = Math.Sin(theta);
                double x = a * rho;
                double y = b * rho;

                cv.Point pt1 = new cv.Point(x + 1000 * -b, y + 1000 * a);
                cv.Point pt2 = new cv.Point(x - 1000 * -b, y - 1000 * a);
                dst.Line(pt1, pt2, Scalar.Red, task.lineWidth + 1, task.lineType, 0);
            }
        }

    }
}




