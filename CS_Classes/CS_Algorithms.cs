using OpenCvSharp;
namespace CS_Classes
{
    public class AddWeighted_Basics_CS : CS_Parent
    {
        public Mat dst2 = null;
        public bool standalone = true;
        public Mat srcPlus;
        public Mat src2;
        public string desc;
        void constructor()
        {
            this.src2 = null;
            this.desc = "Add 2 images with specified weights.";
        }

        void RunCS(Mat src)
        {
            srcPlus = this.src2;
            // algorithm user normally provides src2! 
            if (standalone || this.src2 == null)
            {
                //srcPlus = task.depthRGB;
            }
            if (srcPlus.Type() != src.Type())
            {
                //if (src.type() !== cv.CV_8UC3 || srcPlus.type() !== cv.CV_8UC3)
                //{
                //    if (src.type() === cv.CV_32FC1) src = this.vbNormalize32f(src);
                //    if (srcPlus.type() === cv.CV_32FC1) srcPlus = this.vbNormalize32f(srcPlus);
                //    if (src.type() !== cv.CV_8UC3) src = src.cvtColor(cv.COLOR_GRAY2BGR);
                //    if (srcPlus.type() !== cv.CV_8UC3) srcPlus = srcPlus.cvtColor(cv.COLOR_GRAY2BGR);
                //}
            }
            Cv2.AddWeighted(src, 0.5, src2, 0.5, 0, dst2);
        }
    }
}
