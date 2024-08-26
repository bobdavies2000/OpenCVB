Imports cvb = OpenCvSharp
Imports OpenCvSharp.XFeatures2D
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class FREAK_Basics : Inherits VB_Parent
    Dim orb As New ORB_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Find keypoints using FREAK algorithm"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        orb.Run(src)
        dst1 = orb.dst2

        Dim freak = cvb.XFeatures2D.FREAK.Create()
        Dim fdesc = New cvb.Mat
        Dim keypoints = orb.keypoints.ToList
        freak.Compute(src.CvtColor(cvb.ColorConversionCodes.BGR2Gray), orb.keypoints, fDesc)

        dst2 = src.Clone()

        For Each kpt In keypoints
            Dim r = kpt.Size / 2
            DrawCircle(dst2,kpt.Pt, r, cvb.Scalar.Green)
            DrawLine(dst2,New cvb.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cvb.Point(kpt.Pt.X - r, kpt.Pt.Y - r), cvb.Scalar.Green)
            DrawLine(dst2,New cvb.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cvb.Point(kpt.Pt.X - r, kpt.Pt.Y + r), cvb.Scalar.Green)
        Next
        labels(2) = CStr(orb.keypoints.Count) + " key points were identified"
        labels(3) = CStr(orb.keypoints.Count) + " FREAK Descriptors (resized) One row = keypoint"
        If fdesc.Width > 0 And fdesc.Height > 0 Then dst3 = fdesc.Resize(dst3.Size())
    End Sub
End Class