Imports cv = OpenCvSharp
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Namespace VBClasses
    Public Class ORB_Basics : Inherits TaskParent
        Public keypoints() As cv.KeyPoint
        Dim orb As cv.ORB
        Dim options As New Options_ORB
        Public Sub New()
            desc = "Find keypoints using ORB - Oriented Fast and Rotated BRIEF"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            orb = cv.ORB.Create(options.desiredCount)
            keypoints = orb.Detect(src)
            dst2 = src.Clone()
            For Each kpt In keypoints
                DrawCircle(dst2, kpt.Pt, task.DotSize + 1, cv.Scalar.Yellow)
            Next
            labels(2) = CStr(keypoints.Count) + " key points were identified"
        End Sub
        Public Sub Close()
            If orb IsNot Nothing Then orb.Dispose()
        End Sub
    End Class


End Namespace