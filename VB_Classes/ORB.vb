Imports cv = OpenCvSharp
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class ORB_Basics : Inherits VB_Algorithm
    Public keypoints() As cv.KeyPoint
    Dim orb As cv.ORB
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("ORB - desired point count", 10, 2000, 100)
        desc = "Find keypoints using ORB - Oriented Fast and Rotated BRIEF"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static countSlider = findSlider("ORB - desired point count")
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        orb = cv.ORB.Create(countSlider.Value)
        keypoints = orb.Detect(src)
        If standaloneTest() Then
            dst2 = src.Clone().CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each kpt In keypoints
                dst2.Circle(kpt.Pt, task.dotSize + 1, cv.Scalar.Yellow, -1, task.lineType)
            Next
            labels(2) = CStr(keypoints.Count) + " key points were identified"
        End If
    End Sub
End Class








Public Class ORB_Match : Inherits VB_Algorithm
    Public Sub New()
        desc = "Find ORB keypoints and match with a previous frame"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class
