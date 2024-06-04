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
        public bool standalone = true;
        public string desc = "";
        public Mat dst0, dst1, dst2, dst3;
        public string traceName;
        public string[] labels = new string[4]; 
        public CS_Parent(VBtask _task)
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
        public bool StandaloneTest()
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

        public void NextFrame(Mat src)
        {
            task.labels = labels;

            // make sure that any outputs from the algorithm are the right size.nearest
            if (dst0.Width != task.workingRes.Width && dst0.Width > 0) dst0 = dst0.Resize(task.workingRes);
            if (dst1.Width != task.workingRes.Width && dst1.Width > 0) dst1 = dst1.Resize(task.workingRes);
            if (dst2.Width != task.workingRes.Width && dst2.Width > 0) dst2 = dst2.Resize(task.workingRes);
            if (dst3.Width != task.workingRes.Width && dst3.Width > 0) dst3 = dst3.Resize(task.workingRes);

            if (task.pixelViewerOn)
            {
                if (task.intermediateObject != null)
                {
                    task.dst0 = task.intermediateObject.dst0;
                    task.dst1 = task.intermediateObject.dst1;
                    task.dst2 = task.intermediateObject.dst2;
                    task.dst3 = task.intermediateObject.dst3;
                }
                else
                {
                    //task.dst0 = gOptions.displayDst0.Checked ? dst0 : task.color;
                    //task.dst1 = gOptions.displayDst1.Checked ? dst1 : task.depthRGB;
                    task.dst2 = dst2;
                    task.dst3 = dst3;
                }
                task.PixelViewer.viewerForm.Show();
                task.PixelViewer.Run(src);
            }
            else
            {
                if (task.PixelViewer != null && task.PixelViewer.viewerForm.Visible)
                {
                    task.PixelViewer.viewerForm.Hide();
                }
            }

            // var obj = checkIntermediateResults();
            //task.intermediateObject = obj;
            //task.trueData = new List<trueText>(trueData);
            //if (obj != null)
            //{
            //task.dst0 = gOptions.displayDst0.Checked ? MakeSureImage8uC3(obj.dst0) : task.color;
            //task.dst1 = gOptions.displayDst1.Checked ? MakeSureImage8uC3(obj.dst1) : task.depthRGB;
            //task.dst2 = obj.dst2.Type() == MatType.CV_8UC3 ? obj.dst2 : MakeSureImage8uC3(obj.dst2);
            //task.dst3 = obj.dst3.Type() == MatType.CV_8UC3 ? obj.dst3 : MakeSureImage8uC3(obj.dst3);
            //task.labels = obj.labels;
            //task.trueData = new List<trueText>(obj.trueData);
            //}
            //else
            //{
            //task.dst0 = gOptions.displayDst0.Checked ? MakeSureImage8uC3(dst0) : task.color;
            //task.dst1 = gOptions.displayDst1.Checked ? MakeSureImage8uC3(dst1) : task.depthRGB;
            //task.dst2 = MakeSureImage8uC3(dst2);
            //task.dst3 = MakeSureImage8uC3(dst3);
            //}





            task.dst0 = task.color;
            task.dst1 = task.depthRGB;
            task.dst2 = dst2;
            task.dst3 = dst3;

            
            
            
            
            
            if (task.gifCreator != null)
            {
                task.gifCreator.createNextGifImage();
            }

            //if (task.dst2.Width == task.workingRes.Width && task.dst2.Height == task.workingRes.Height)
            //{
            //    if (gOptions.ShowGrid.Checked)
            //    {
            //        task.dst2.SetTo(Scalar.White, task.gridMask);
            //    }
            //    if (task.dst2.Width != task.workingRes.Width || task.dst2.Height != task.workingRes.Height)
            //    {
            //        task.dst2 = task.dst2.Resize(task.workingRes, InterpolationFlags.Nearest);
            //    }
            //    if (task.dst3.Width != task.workingRes.Width || task.dst3.Height != task.workingRes.Height)
            //    {
            //        task.dst3 = task.dst3.Resize(task.workingRes, InterpolationFlags.Nearest);
            //    }
            //}



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
}




