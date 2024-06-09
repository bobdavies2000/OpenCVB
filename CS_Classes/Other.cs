using System.Drawing;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using VB_Classes;

namespace CS_Classes
{ 
    public class CSharp_BitmapToMat : CS_Parent
    {
        public CSharp_BitmapToMat(VBtask task) : base(task)
        {
            labels[2] = "Convert color bitmap to Mat";
            labels[3] = "Convert Mat to bitmap and then back to Mat";
            desc = "Convert a color and grayscale bitmap to a cv.Mat";
        }

        public void Run(Mat src)
        {
            Bitmap bitmap = new Bitmap(task.homeDir + "opencv/Samples/Data/lena.jpg");
            dst2 = BitmapConverter.ToMat(bitmap).Resize(src.Size());

            bitmap = BitmapConverter.ToBitmap(src);
            dst3 = BitmapConverter.ToMat(bitmap);
        }
    }
}
