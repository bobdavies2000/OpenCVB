using OpenCvSharp;
  
namespace CS_Classes
{
    public class AnyName
    {
        public Mat RunCS(Mat img1)
        {
            return img1.CvtColor(ColorConversionCodes.BGR2GRAY);
        }
    }
}
