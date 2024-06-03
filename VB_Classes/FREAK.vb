Imports cv = OpenCvSharp
Imports OpenCvSharp.XFeatures2D
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class FREAK_Basics : Inherits VB_Parent
    Dim orb As New ORB_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Find keypoints using FREAK algorithm"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        orb.Run(src)
        dst1 = orb.dst2

        Dim freak = cv.XFeatures2D.FREAK.Create()
        Dim fdesc = New cv.Mat
        Dim keypoints = orb.keypoints.ToList
        freak.Compute(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), orb.keypoints, fDesc)

        dst2 = src.Clone()

        For Each kpt In keypoints
            Dim r = kpt.Size / 2
            dst2.Circle(kpt.Pt, r, cv.Scalar.Green, -1, task.lineType)
            dst2.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y - r), cv.Scalar.Green)
            dst2.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y + r), cv.Scalar.Green)
        Next
        labels(2) = CStr(orb.keypoints.Count) + " key points were identified"
        labels(3) = CStr(orb.keypoints.Count) + " FREAK Descriptors (resized) One row = keypoint"
        If fdesc.Width > 0 And fdesc.Height > 0 Then dst3 = fdesc.Resize(dst3.Size())
    End Sub
End Class