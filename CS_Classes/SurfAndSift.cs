using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.XFeatures2D;

/// http://www.prism.gatech.edu/~ahuaman3/docs/OpenCV_Docs/tutorials/nonfree_1/nonfree_1.html
namespace CS_Classes
{
    public class CS_SurfBasics
    {
        public bool drawPoints = true;
        public KeyPoint[] keypoints1, keypoints2;
        public void RunCS(Mat gray1, Mat gray2, out Mat output, int hessianThreshold, bool useBFMatcher)
        {
            var surf = SURF.Create(hessianThreshold, 4, 2, true);
            output = new Mat(gray1.Rows, gray1.Cols * 2, MatType.CV_8UC3);

            var descriptors1 = new Mat();
            var descriptors2 = new Mat();
            surf.DetectAndCompute(gray1, null, out keypoints1, descriptors1);
            surf.DetectAndCompute(gray2, null, out keypoints2, descriptors2);

            if (useBFMatcher)
            {
                if (descriptors1.Rows > 0 && descriptors2.Rows > 0) // occasionally there is nothing to match!
                {
                    var bfMatcher = new BFMatcher(NormTypes.L2, false);
                    DMatch[] bfMatches = bfMatcher.Match(descriptors1, descriptors2);
                    if (drawPoints) Cv2.DrawMatches(gray1, keypoints1, gray2, keypoints2, bfMatches, output);
                }
            }
            else
            {
                var flannMatcher = new FlannBasedMatcher();
                if (descriptors1.Width > 0 && descriptors2.Width > 0)
                {
                    DMatch[] flannMatches = flannMatcher.Match(descriptors1, descriptors2);
                    if (drawPoints) Cv2.DrawMatches(gray1, keypoints1, gray2, keypoints2, flannMatches, output);
                }
            }
        }
    }


    public class CS_SiftBasics
    {
        public KeyPoint[] kp1, kp2;
        public void RunCS(Mat gray1, Mat gray2, Mat dst2, bool useBFMatcher, int pointsToMatch)
        {
            var sift = SIFT.Create(pointsToMatch);

            KeyPoint[] keypoints1, keypoints2;
            var descriptors1 = new Mat();
            var descriptors2 = new Mat();
            sift.DetectAndCompute(gray1, null, out keypoints1, descriptors1);
            sift.DetectAndCompute(gray2, null, out keypoints2, descriptors2);

            if (useBFMatcher)
            {
                var bfMatcher = new BFMatcher(NormTypes.L2, false);
                DMatch[] bfMatches = bfMatcher.Match(descriptors1, descriptors2);
                Cv2.DrawMatches(gray1, keypoints1, gray2, keypoints2, bfMatches, dst2);
            }
            else
            {
                var flannMatcher = new FlannBasedMatcher();
                DMatch[] flannMatches = flannMatcher.Match(descriptors1, descriptors2);
                Cv2.DrawMatches(gray1, keypoints1, gray2, keypoints2, flannMatches, dst2);
            }
            kp1 = keypoints1;
            kp2 = keypoints2;
        }
    }
    public class CS_SiftPoints
    {
        public KeyPoint[] keypoints;
        public void RunCS(Mat gray1, int pointsToMatch)
        {
            var sift = SIFT.Create(pointsToMatch);
            var descriptors1 = new Mat();
            sift.DetectAndCompute(gray1, null, out keypoints, descriptors1);
        }
    }
}
