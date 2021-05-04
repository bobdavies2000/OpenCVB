Imports cv = OpenCvSharp
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class ORB_Basics : Inherits VBparent
    Public keypoints() As cv.KeyPoint
    Dim orb As cv.ORB
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "ORB - desired point count", 10, 2000, 100)
        End If
        task.desc = "Find keypoints using ORB - Oriented Fast and Rotated BRIEF"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        orb = cv.ORB.Create(sliders.trackbar(0).Value)
        keypoints = orb.Detect(src)
        If standalone or task.intermediateReview = caller Then
            dst1 = src.Clone().CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each kpt In keypoints
                dst1.Circle(kpt.Pt, task.dotSize + 1, cv.Scalar.Yellow, -1, task.lineType)
            Next
            label1 = CStr(keypoints.Count) + " key points were identified"
        End If
    End Sub
End Class


