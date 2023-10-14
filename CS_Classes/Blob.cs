using cv = OpenCvSharp;

namespace CS_Classes
{
    public class Blob_Basics
    {
        // public cv.KeyPoint[] keypoint;
        public void RunCS(cv.Mat input, cv.Mat output, cv.SimpleBlobDetector.Params detectorParams)
        {
            var binaryImage = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY);
            cv.Cv2.Threshold(binaryImage, binaryImage, thresh: 0, maxval: 255, type: cv.ThresholdTypes.Binary);

            var simpleBlobDetector = cv.SimpleBlobDetector.Create(detectorParams);
            //var mask = new cv.Mat(input.Size(), cv.MatType.CV_8UC1, 0);
            //var descriptors = new cv.Mat(input.Size(), cv.MatType.CV_8UC1, 0);
            //simpleBlobDetector.DetectAndCompute(binaryImage, mask, out keypoint, descriptors);

            var keypoint = simpleBlobDetector.Detect(input);

            cv.Cv2.DrawKeypoints(
                    image: binaryImage,
                    keypoints: keypoint,
                    outImage: output,
                    color: cv.Scalar.FromRgb(255, 0, 0),
                    flags: cv.DrawMatchesFlags.DrawRichKeypoints);
        }
    }
}
