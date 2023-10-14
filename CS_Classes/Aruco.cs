using OpenCvSharp.Aruco;
using System;
using System.Collections.Generic;
using cv = OpenCvSharp;

// https://github.com/shimat/opencvsharp_samples/blob/master/SamplesCS/Samples/ArucoSample.cs
namespace CS_Classes
{
    public class Aruco_Detect
    {
        public cv.Mat detectedMarkers;
        public cv.Mat normalizedImage;
        public void RunCS(cv.Mat src)
        {
            // The locations of the markers in the image at FilePath.Image.Aruco.
            //const int upperLeftMarkerId = 160;
            //const int upperRightMarkerId = 268;
            //const int lowerRightMarkerId = 176;
            //const int lowerLeftMarkerId = 168;

            //var detectorParameters = Aruco_Detect();
            //detectorParameters.CornerRefinementMethod = CornerRefineMethod.Subpix;
            //detectorParameters.CornerRefinementWinSize = 9;

            //var dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_1000);

            //CvAruco.DetectMarkers(src, dictionary, out var corners, out var ids, detectorParameters, out var rejectedPoints);
            //detectedMarkers = src.Clone();
            //CvAruco.DrawDetectedMarkers(detectedMarkers, corners, ids, cv.Scalar.White);

            //// Find the index of the four markers in the ids array. We'll use this same index into the
            //// corners array to find the corners of each marker.
            //var upperLeftCornerIndex = Array.FindIndex(ids, id => id == upperLeftMarkerId);
            //var upperRightCornerIndex = Array.FindIndex(ids, id => id == upperRightMarkerId);
            //var lowerRightCornerIndex = Array.FindIndex(ids, id => id == lowerRightMarkerId);
            //var lowerLeftCornerIndex = Array.FindIndex(ids, id => id == lowerLeftMarkerId);

            //// Make sure we found all four markers.
            //if (upperLeftCornerIndex < 0 || upperRightCornerIndex < 0 || lowerRightCornerIndex < 0 || lowerLeftCornerIndex < 0)
            //{
            //    return;
            //}

            //// Marker corners are stored clockwise beginning with the upper-left corner.
            //// Get the first (upper-left) corner of the upper-left marker.
            //var upperLeftPixel = corners[upperLeftCornerIndex][0];
            //// Get the second (upper-right) corner of the upper-right marker.
            //var upperRightPixel = corners[upperRightCornerIndex][1];
            //// Get the third (lower-right) corner of the lower-right marker.
            //var lowerRightPixel = corners[lowerRightCornerIndex][2];
            //// Get the fourth (lower-left) corner of the lower-left marker.
            //var lowerLeftPixel = corners[lowerLeftCornerIndex][3];

            //// Create coordinates for passing to GetPerspectiveTransform
            //var sourceCoordinates = new List<cv.Point2f>
            //{
            //    upperLeftPixel, upperRightPixel, lowerRightPixel, lowerLeftPixel
            //};
            //var destinationCoordinates = new List<cv.Point2f>
            //{
            //    new cv.Point2f(0, 0),
            //    new cv.Point2f(1024, 0),
            //    new cv.Point2f(1024, 1024),
            //    new cv.Point2f(0, 1024),
            //};

            //var transform = cv.Cv2.GetPerspectiveTransform(sourceCoordinates, destinationCoordinates);
            //normalizedImage = new cv.Mat();
            //cv.Cv2.WarpPerspective(src, normalizedImage, transform, new cv.Size(1024, 1024));
        }
    }
}
