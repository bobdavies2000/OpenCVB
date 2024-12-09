Imports cvb = OpenCvSharp
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class ORB_Basics : Inherits TaskParent
    Public keypoints() As cvb.KeyPoint
    Dim orb As cvb.ORB
    Dim options As New Options_ORB
    Public Sub New()
        desc = "Find keypoints using ORB - Oriented Fast and Rotated BRIEF"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        orb = cvb.ORB.Create(options.desiredCount)
        keypoints = orb.Detect(src)
        dst2 = src.Clone()
        For Each kpt In keypoints
            DrawCircle(dst2, kpt.Pt, task.DotSize + 1, cvb.Scalar.Yellow)
        Next
        labels(2) = CStr(keypoints.Count) + " key points were identified"
    End Sub
End Class

