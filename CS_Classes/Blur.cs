using cv = OpenCvSharp;
namespace CS_Classes
{
    public class CS_BlurGaussian
    {
        public void RunCS(cv.Mat color, cv.Mat result1, int kernelSize)
        {
            cv.Cv2.GaussianBlur(color, result1, new cv.Size(kernelSize, kernelSize), 0, 0);
        }
    }
    public class CS_BlurMedian
    {
        public void RunCS(cv.Mat color, cv.Mat result1, int kernelSize)
        {
            cv.Cv2.GaussianBlur(color, result1, new cv.Size(kernelSize, kernelSize), 0, 0);
        }
    }

}