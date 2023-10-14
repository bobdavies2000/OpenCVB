Imports cv = OpenCvSharp
Imports OpenCvSharp.XFeatures2D
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class FREAK_Basics : Inherits VB_Algorithm
    ReadOnly orb As New ORB_Basics
    Public Sub New()
        desc = "Find keypoints using ORB and FREAK algorithm"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        orb.Run(src)

        Dim freak = cv.XFeatures2D.FREAK.Create()
        Dim fdesc = New cv.Mat
        freak.Compute(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), orb.keypoints, fDesc)

        dst2 = src.Clone()

        For Each kpt In orb.keypoints
            Dim r = kpt.Size / 2
            dst2.Circle(kpt.Pt, r, cv.Scalar.Green, -1, task.lineType)
            dst2.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y - r), cv.Scalar.Green)
            dst2.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y + r), cv.Scalar.Green)
        Next
        labels(2) = CStr(orb.keypoints.Count) + " key points were identified"
        labels(3) = CStr(orb.keypoints.Count) + " FREAK Descriptors (resized) One row = keypoint"
        If fDesc.Width > 0 And fDesc.Height > 0 Then dst3 = fDesc.Resize(dst3.Size())
    End Sub
End Class


