using OpenCvSharp;
  
namespace CS_Classes
{
    public class PCA_NColor
    {
        public Mat RunCS(Mat img1)
        {
            return img1.CvtColor(ColorConversionCodes.BGR2GRAY);
        }
    }
}
