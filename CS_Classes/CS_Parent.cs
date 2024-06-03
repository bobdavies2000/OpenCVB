using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cv = OpenCvSharp;
namespace CS_Classes
{
    public class CS_Parent
    {
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

public class CS_Algorithm : IDisposable
{
    //    //public OptionsCheckbox check = new OptionsCheckbox();
    //    //public OptionsCombo combo = new OptionsCombo();
    //    //public OptionsRadioButtons radio = new OptionsRadioButtons();
    //    //public OptionsSliders sliders = new OptionsSliders();
    //    public bool standalone;
    //    public bool firstPass;
    //    public cv.Mat dst0, dst1, dst2, dst3, empty;
    //    public string[] labels = new string[4];
    //    public Random msRNG = new Random();
    //    public object algorithm;
    //    public string traceName;
    //    public string desc;
    //    public string callStack = "";
    //    public cv.Scalar nearColor = cv.Scalar.Yellow;
    //    public cv.Scalar farColor = cv.Scalar.Blue;
    //    public cv.Vec3b black;
    //    public cv.Vec3b white = new cv.Vec3b(255, 255, 255);
    //    public cv.Vec3b gray = new cv.Vec3b(127, 127, 127);
    //    public cv.Vec3b yellow = new cv.Vec3b(0, 255, 255);
    //    public cv.Vec3b purple = new cv.Vec3b(255, 0, 255);
    //    public cv.Vec3b teal = new cv.Vec3b(255, 255, 0);
    //    public cv.Vec3b red = new cv.Vec3b(0, 0, 255);
    //    public cv.Vec3b green = new cv.Vec3b(0, 255, 0);
    //    public cv.Vec3b blue = new cv.Vec3b(255, 0, 0);
    //    public cv.Point3f zero3f = new cv.Point3f(0, 0, 0);
    //    public cv.Vec4f newVec4f = new cv.Vec4f();
    //    public IntPtr cPtr;
    //    public List<trueText> trueData = new List<trueText>();
    //    public string strOut;

    //    public void initParent()
    //    {
    //        if (task.algName.StartsWith("CPP_"))
    //        {
    //            task.callTrace.Clear();
    //            task.callTrace.Add("CPP_Basics\\");
    //        }

    //        standalone = task.callTrace[0] == traceName + "\\"; // only the first is standaloneTest() (the primary algorithm.)
    //        if (traceName == "Python_Run") standalone = true;
    //        if (!standalone && !task.callTrace.Contains(callStack))
    //        {
    //            task.callTrace.Add(callStack);
    //        }

    //        dst0 = new cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0);
    //        dst1 = new cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0);
    //        dst2 = new cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0);
    //        dst3 = new cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0);
    //        task.activeObjects.Add(this);

    //        if (task.recordTimings)
    //        {
    //            if (standalone && !task.testAllRunning)
    //            {
    //                task.algorithmNames.Add("waitingForInput");
    //                algorithmTimes.Add(DateTime.Now);
    //                task.algorithm_ms.Add(0);

    //                task.algorithmNames.Add("inputBufferCopy");
    //                algorithmTimes.Add(DateTime.Now);
    //                task.algorithm_ms.Add(0);

    //                task.algorithmNames.Add("ReturnCopyTime");
    //                algorithmTimes.Add(DateTime.Now);
    //                task.algorithm_ms.Add(0);

    //                task.algorithmNames.Add(traceName);
    //                algorithmTimes.Add(DateTime.Now);
    //                task.algorithm_ms.Add(0);

    //                algorithmStack = new Stack<int>();
    //                algorithmStack.Push(0);
    //                algorithmStack.Push(1);
    //                algorithmStack.Push(2);
    //                algorithmStack.Push(3);
    //            }
    //        }
    //        firstPass = true;
    //    }

    //    public bool standaloneTest()
    //    {
    //        if (standalone || showIntermediate()) return true;
    //        return false;
    //    }

    //    public VB_Parent checkIntermediateResults()
    //    {
    //        if (task.algName.StartsWith("CPP_")) return null; // we don't currently support intermediate results for CPP_ algorithms.
    //        foreach (var obj in task.activeObjects)
    //        {
    //            if (obj.traceName == task.intermediateName && !obj.firstPass) return obj;
    //        }
    //        return null;
    //    }

    //    public void measureStartRun(string name)
    //    {
    //        if (!task.recordTimings) return;
    //        var nextTime = DateTime.Now;
    //        if (!task.algorithmNames.Contains(name))
    //        {
    //            task.algorithmNames.Add(name);
    //            task.algorithm_ms.Add(0);
    //            algorithmTimes.Add(nextTime);
    //        }

    //        var index = algorithmStack.Peek();
    //        var elapsedTicks = nextTime.Ticks - algorithmTimes[index].Ticks;
    //        var span = new TimeSpan(elapsedTicks);
    //        task.algorithm_ms[index] += span.Ticks / TimeSpan.TicksPerMillisecond;

    //        index = task.algorithmNames.IndexOf(name);
    //        algorithmTimes[index] = nextTime;
    //        algorithmStack.Push(index);
    //    }

    //    public void measureEndRun(string name)
    //    {
    //        if (!task.recordTimings) return;
    //        var nextTime = DateTime.Now;
    //        var index = algorithmStack.Peek();
    //        var elapsedTicks = nextTime.Ticks - algorithmTimes[index].Ticks;
    //        var span = new TimeSpan(elapsedTicks);
    //        task.algorithm_ms[index] += span.Ticks / TimeSpan.TicksPerMillisecond;
    //        algorithmStack.Pop();
    //        algorithmTimes[algorithmStack.Peek()] = nextTime;
    //    }

    //    public void Run(cv.Mat src)
    //    {
    //        if (!task.testAllRunning) measureStartRun(traceName);

    //        trueData.Clear();
    //        if (!task.paused)
    //        {
    //            if (!task.algName.StartsWith("Options_")) algorithm.RunVB(src);
    //        }
    //        firstPass = false;
    //        if (!task.testAllRunning) measureEndRun(traceName);
    //    }

    //    public void setTrueText(string text, cv.Point pt, int picTag = 2)
    //    {
    //        var str = new trueText(text, pt, picTag);
    //        trueData.Add(str);
    //    }

    //    public void setTrueText(string text)
    //    {
    //        var pt = new cv.Point(0, 0);
    //        var picTag = 2;
    //        var str = new trueText(text, pt, picTag);
    //        trueData.Add(str);
    //    }

    //    public void setTrueText(string text, int picTag)
    //    {
    //        var pt = new cv.Point(0, 0);
    //        var str = new trueText(text, pt, picTag);
    //        trueData.Add(str);
    //    }

    //    public bool showIntermediate()
    //    {
    //        if (task.intermediateObject == null) return false;
    //        if (task.intermediateObject.traceName == traceName) return true;
    //        return false;
    //    }

    //    public void setFlowText(string text, int picTag)
    //    {
    //        var pt = new cv.Point(0, 0);
    //        var str = new trueText(text, pt, picTag);
    //        task.flowData.Add(str);
    //    }

    //    public cv.Rect initRandomRect(int margin)
    //    {
    //        return new cv.Rect(msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin),
    //                           msRNG.Next(margin, dst2.Width - 2 * margin), msRNG.Next(margin, dst2.Height - 2 * margin));
    //    }

    //    public cv.Scalar vecToScalar(cv.Vec3b v)
    //    {
    //        return new cv.Scalar(v.Item0, v.Item1, v.Item2);
    //    }

    //    public void drawRotatedRectangle(cv.RotatedRect rotatedRect, cv.Mat dst2, cv.Scalar color)
    //    {
    //        var vertices2f = rotatedRect.Points();
    //        var vertices = new cv.Point[vertices2f.Length];
    //        for (var j = 0; j < vertices2f.Length; j++)
    //        {
    //            vertices[j] = new cv.Point((int)vertices2f[j].X, (int)vertices2f[j].Y);
    //        }
    //        dst2.FillConvexPoly(vertices, color, task.lineType);
    //    }

    //    public void drawRotatedOutline(cv.RotatedRect rotatedRect, cv.Mat dst2, cv.Scalar color)
    //    {
    //        var pts = rotatedRect.Points();
    //        var lastPt = pts[0];
    //        for (var i = 1; i <= pts.Length; i++)
    //        {
    //            var index = i % pts.Length;
    //            var pt = new cv.Point((int)pts[index].X, (int)pts[index].Y);
    //            dst2.Line(pt, lastPt, task.highlightColor, task.lineWidth, task.lineType);
    //            lastPt = pt;
    //        }
    //    }

    //    public List<cv.Point2f> quickRandomPoints(int howMany)
    //    {
    //        var srcPoints = new List<cv.Point2f>();
    //        var w = task.workingRes.Width;
    //        var h = task.workingRes.Height;
    //        for (var i = 0; i < howMany; i++)
    //        {
    //            var pt = new cv.Point2f(msRNG.Next(0, w), msRNG.Next(0, h));
    //            srcPoints.Add(pt);
    //        }
    //        return srcPoints;
    //    }

    //    public float findCorrelation(cv.Mat pts1, cv.Mat pts2)
    //    {
    //        var correlationMat = new cv.Mat();
    //        cv.Cv2.MatchTemplate(pts1, pts2, correlationMat, cv.TemplateMatchModes.CCoeffNormed);
    //        return correlationMat.Get<float>(0, 0);
    //    }

    //    public void AddPlotScale(cv.Mat dst, double minVal, double maxVal, int lineCount = 3)
    //    {
    //        // draw a scale along the side
    //        var spacer = dst.Height / (lineCount + 1);
    //        var spaceVal = (maxVal - minVal) / (lineCount + 1);
    //        if (lineCount > 1) if (spaceVal < 1) spaceVal = 1;
    //        if (spaceVal > 10) spaceVal += spaceVal % 10;
    //        for (var i = 0; i <= lineCount; i++)
    //        {
    //            var p1 = new cv.Point(0, spacer * i);
    //            var p2 = new cv.Point(dst.Width, spacer * i);
    //            dst.Line(p1, p2, cv.Scalar.White, task.cvFontThickness);
    //            var nextVal = (maxVal - spaceVal * i);
    //            var nextText = maxVal > 1000 ? $"{(nextVal / 1000).ToString("###,##0.0")}k" : nextVal.ToString(fmt2);
    //            cv.Cv2.PutText(dst, nextText, p1, cv.HersheyFonts.HersheyPlain, task.cvFontSize, cv.Scalar.White,
    //                           task.cvFontThickness, task.lineType);
    //        }
    //    }

    //    public void AddPlotScaleNew(cv.Mat dst, float minVal, float maxVal, float average)
    //    {
    //        var diff = maxVal - minVal;
    //        var fmt = diff > 10 ? fmt0 : diff > 2 ? fmt1 : diff > 0.5 ? fmt2 : fmt3;
    //        for (var i = 0; i < 3; i++)
    //        {
    //            var nextVal = i switch
    //            {
    //                0 => maxVal,
    //                1 => average,
    //                2 => minVal,
    //                _ => throw new ArgumentOutOfRangeException()
    //            };
    //            var nextText = maxVal > 1000 ? $"{(nextVal / 1000).ToString("###,##0.0")}k" : nextVal.ToString(fmt);
    //            var pt = i switch
    //            {
    //                0 => new cv.Point(0, 15),
    //                1 => new cv.Point(0, dst2.Height / 2),
    //                2 => new cv.Point(0, dst2.Height - 10),
    //                _ => throw new ArgumentOutOfRangeException()
    //            };
    //            cv.Cv2.PutText(dst, nextText, pt, cv.HersheyFonts.HersheyPlain, 1.0, cv.Scalar.White, 1, task.lineType);
    //        }
    //    }

    //    public bool rectContainsPt(cv.Rect r, cv.Point pt)
    //    {
    //        if (r.X <= pt.X && r.X + r.Width > pt.X && r.Y <= pt.Y && r.Y + r.Height > pt.Y) return true;
    //        return false;
    //    }

    //    public cv.Rect validateRect(cv.Rect r, int ratio = 1)
    //    {
    //        if (r.Width <= 0) r.Width = 1;
    //        if (r.Height <= 0) r.Height = 1;
    //        if (r.X < 0) r.X = 0;
    //        if (r.Y < 0) r.Y = 0;
    //        if (r.X > task.workingRes.Width * ratio) r.X = task.workingRes.Width * ratio - 1;
    //        if (r.Y > task.workingRes.Height * ratio) r.Y = task.workingRes.Height * ratio - 1;
    //        if (r.X + r.Width > task.workingRes.Width * ratio) r.Width = task.workingRes.Width * ratio - r.X;
    //        if (r.Y + r.Height > task.workingRes.Height * ratio) r.Height = task.workingRes.Height * ratio - r.Y;
    //        if (r.Width <= 0) r.Width = 1; // check again (it might have changed.)
    //        if (r.Height <= 0) r.Height = 1;
    //        if (r.X == task.workingRes.Width * ratio) r.X = r.X - 1;
    //        if (r.Y == task.workingRes.Height * ratio) r.Y = r.Y - 1;
    //        return r;
    //    }

    //    public cv.Rect validatePreserve(cv.Rect r)
    //    {
    //        if (r.Width <= 0) r.Width = 1;
    //        if (r.Height <= 0) r.Height = 1;
    //        if (r.X < 0) r.X = 0;
    //        if (r.Y < 0) r.Y = 0;
    //        if (r.X + r.Width >= task.workingRes.Width) r.X = dst2.Width - r.Width - 1;
    //        if (r.Y + r.Height >= task.workingRes.Height) r.Y = task.workingRes.Height - r.Height - 1;
    //        return r;
    //    }

    //    public cv.Point2f validatePoint2f(cv.Point2f p)
    //    {
    //        if (p.X < 0) p.X = 0;
    //        if (p.Y < 0) p.Y = 0;
    //        if (p.X >= dst2.Width) p.X = dst2.Width - 1;
    //        if (p.Y >= dst2.Height) p.Y = dst2.Height - 1;
    //        return p;
    //    }

    //    public VB_Parent()
    //    {
    //        algorithm = this;
    //        traceName = GetType().Name;
    //        var stackTrace = Environment.StackTrace;
    //        var lines = stackTrace.Split(Environment.NewLine);
    //        for (var i = 0; i < lines.Length; i++)
    //        {
    //            lines[i] = lines[i].Trim();
    //            var offset = lines[i].IndexOf("VB_Classes.");
    //            if (offset > 0)
    //            {
    //                var partLine = lines[i].Substring(offset + 11);
    //                if (partLine.StartsWith("AlgorithmList.createVBAlgorithm")) break;
    //                var split = partLine.Split("\\");
    //                partLine = partLine.Substring(0, partLine.IndexOf("."));
    //                if (!(partLine.StartsWith("VB_Parent") || partLine.StartsWith("VBtask")))
    //                {
    //                    callStack = partLine + "\\" + callStack;
    //                }
    //            }
    //        }
    //        initParent();
    //    }

    //    public void Dispose()
    //    {
    //        if (allOptions != null) allOptions.Close();
    //        if (task.pythonTaskName.EndsWith(".py"))
    //        {
    //            var proc = Process.GetProcesses();
    //            for (var i = 0; i < proc.Length; i++)
    //            {
    //                if (proc[i].ProcessName.ToLower().Contains("pythonw")) continue;
    //                if (proc[i].ProcessName.ToLower().Contains("python"))
    //                {
    //                    if (!proc[i].HasExited) proc[i].Kill();
    //                }
    //            }
    //        }
    //        foreach (var algorithm in task.activeObjects)
    //        {
    //            var type = algorithm.GetType();
    //            if (type.GetMethod("Close") != null) algorithm.Close(); // Close any unmanaged classes...
    //        }
    //        sliders.Dispose();
    //        check.Dispose();
    //        radio.Dispose();
    //        combo.Dispose();
    //    }

    //    public void NextFrame(cv.Mat src)
    //    {
    //        // If task.drawRect.Width <> 0 Then task.drawRect = validateRect(task.drawRect)
    //        algorithm.Run(src);

    //        task.labels = labels;

    //        // make sure that any outputs from the algorithm are the right size.nearest
    //        if (dst0.Size != task.workingRes && dst0.Width > 0) dst0 = dst0.Resize(task.workingRes, cv.InterpolationFlags.Nearest);
    //        if (dst1.Size != task.workingRes && dst1.Width > 0) dst1 = dst1.Resize(task.workingRes, cv.InterpolationFlags.Nearest);
    //        if (dst2.Size != task.workingRes && dst2.Width > 0) dst2 = dst2.Resize(task.workingRes, cv.InterpolationFlags.Nearest);
    //        if (dst3.Size != task.workingRes && dst3.Width > 0) dst3 = dst3.Resize(task.workingRes, cv.InterpolationFlags.Nearest);

    //        if (task.pixelViewerOn)
    //        {
    //            if (task.intermediateObject != null)
    //            {
    //                task.dst0 = task.intermediateObject.dst0;
    //                task.dst1 = task.intermediateObject.dst1;
    //                task.dst2 = task.intermediateObject.dst2;
    //                task.dst3 = task.intermediateObject.dst3;
    //            }
    //            else
    //            {
    //                task.dst0 = gOptions.displayDst0.Checked ? dst0 : task.color;
    //                task.dst1 = gOptions.displayDst1.Checked ? dst1 : task.depthRGB;
    //                task.dst2 = dst2;
    //                task.dst3 = dst3;
    //            }
    //            task.PixelViewer.viewerForm.Show();
    //            task.PixelViewer.Run(src);
    //        }
    //        else
    //        {
    //            if (task.PixelViewer != null) if (task.PixelViewer.viewerForm.Visible) task.PixelViewer.viewerForm.Hide();
    //        }

    //        var obj = checkIntermediateResults();
    //        task.intermediateObject = obj;
    //        task.trueData = new List<trueText>(trueData);
    //        if (obj != null)
    //        {
    //            if (gOptions.displayDst0.Checked) task.dst0 = MakeSureImage8uC3(obj.dst0); else task.dst0 = task.color;
    //            if (gOptions.displayDst1.Checked) task.dst1 = MakeSureImage8uC3(obj.dst1); else task.dst1 = task.depthRGB;
    //            task.dst2 = obj.dst2.Type == cv.MatType.CV_8UC3 ? obj.dst2 : MakeSureImage8uC3(obj.dst2);
    //            task.dst3 = obj.dst3.Type == cv.MatType.CV_8UC3 ? obj.dst3 : MakeSureImage8uC3(obj.dst3);
    //            task.labels = obj.labels;
    //            task.trueData = new List<trueText>(obj.trueData);
    //        }
    //        else
    //        {
    //            if (gOptions.displayDst0.Checked) task.dst0 = MakeSureImage8uC3(dst0); else task.dst0 = task.color;
    //            if (gOptions.displayDst1.Checked) task.dst1 = MakeSureImage8uC3(dst1); else task.dst1 = task.depthRGB;
    //            task.dst2 = MakeSureImage8uC3(dst2);
    //            task.dst3 = MakeSureImage8uC3(dst3);
    //        }

    //        if (task.gifCreator != null) task.gifCreator.createNextGifImage();

    //        if (task.dst2.Width == task.workingRes.Width && task.dst2.Height == task.workingRes.Height)
    //        {
    //            if (gOptions.ShowGrid.Checked) task.dst2.SetTo(cv.Scalar.White, task.gridMask);
    //            if (task.dst2.Width != task.workingRes.Width || task.dst2.Height != task.workingRes.Height)
    //            {
    //                task.dst2 = task.dst2.Resize(task.workingRes, cv.InterpolationFlags.Nearest);
    //            }
    //            if (task.dst3.Width != task.workingRes.Width || task.dst3.Height != task.workingRes.Height)
    //            {
    //                task.dst3 = task.dst3.Resize(task.workingRes, cv.InterpolationFlags.Nearest);
    //            }
    //        }
    //    }
    public void Dispose()
    {
       
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
        }
        // Clean up unmanaged resources (if any)
        // ...
    }
    ~CS_Algorithm()
    {
        Dispose(false);
    }
}


