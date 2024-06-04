using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VB_Classes;
using OpenCvSharp;

using cv = OpenCvSharp;
namespace CS_Classes
{
    public class CS_Parent
    {
        public VBtask task;
        public bool standalone = true;
        public string desc = "";
        public Mat dst0, dst1, dst2, dst3;
        public CS_Parent(VBtask _task)
        {
            this.task = _task;
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




