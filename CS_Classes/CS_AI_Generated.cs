using cv = OpenCvSharp;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using VB_Classes;
using OpenCvSharp;

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
            if (StandaloneTest() || src2 == null) srcPlus = task.depthRGB;
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
}



