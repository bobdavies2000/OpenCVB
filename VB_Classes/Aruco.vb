Imports cv = OpenCvSharp
Imports OpenCvSharp.Aruco.CvAruco

' https://github.com/shimat/opencvsharp_samples/blob/master/SamplesCS/Samples/ArucoSample.cs
Public Class Aruco_Basics : Inherits VB_Algorithm
    Public Sub New()
        desc = "Show how to use the Aruco markers and rotate the image accordingly."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        'Dim tmp = cv.Cv2.ImRead(task.homeDir + "Data/aruco_markers_photo.jpg")
        ''Static detectorParameters = cv.Aruco.DetectorParameters.Create()
        'Static detectorParameters As cv.Aruco.DetectorParameters

        'detectorParameters.CornerRefinementMethod = cv.Aruco.CornerRefineMethod.Subpix
        'detectorParameters.CornerRefinementWinSize = 9
        'detectorParameters.MarkerBorderBits = 8
        'Dim dictionary = cv.Aruco.CvAruco.GetPredefinedDictionary(cv.Aruco.PredefinedDictionaryName.Dict4X4_1000)


        'Dim corners()() As cv.Point2f
        'Dim ids() As Integer
        'Dim rejectedPoints()() As cv.Point2f
        'cv.Aruco.CvAruco.DetectMarkers(tmp, dictionary, corners, ids, detectorParameters, rejectedPoints)
        'dst2 = src.Clone
        'For i = 0 To corners.Count - 1
        '    For Each pt In corners(i)
        '        dst2.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        '    Next
        'Next
        setTrueText("With the OpenCVSharp 4.7, the support for Aruco changed a lot and is not working." + vbCrLf +
                    "Both the C# and the VB.Net versions needed to be commented out.")
    End Sub
End Class




Public Class Aruco_Test : Inherits VB_Algorithm
    Dim aruco As New CS_Classes.Aruco_Detect
    Public Sub New()
        labels(2) = "Original Image with marker ID's"
        labels(3) = "Normalized image after WarpPerspective."
        desc = "Testing the Aruco marker detection in C#"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim tmp = cv.Cv2.ImRead(task.homeDir + "Data/aruco_markers_photo.jpg")
        'aruco.RunCS(tmp)
        'dst2 = aruco.detectedMarkers.Resize(src.Size())

        'dst3.SetTo(0)
        'dst3(New cv.Rect(0, 0, dst3.Height, dst3.Height)) = aruco.normalizedImage.Resize(New cv.Size(dst2.Height, dst2.Height))
        setTrueText("With the OpenCVSharp 4.7, the support for Aruco changed a lot and is not working." + vbCrLf +
                    "Both the C# and the VB.Net versions needed to be commented out.")
    End Sub
End Class