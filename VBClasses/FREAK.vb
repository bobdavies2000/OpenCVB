Imports cv = OpenCvSharp
Imports OpenCvSharp.XFeatures2D
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Namespace VBClasses
    Public Class FREAK_Basics : Inherits TaskParent
        Dim orb As New ORB_Basics
        Dim freak As FREAK
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Find keypoints using FREAK algorithm"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            orb.Run(src)
            dst1 = orb.dst2

            If freak Is Nothing Then freak = cv.XFeatures2D.FREAK.Create()
            Dim fdesc = New cv.Mat
            Dim keypoints = orb.keypoints.ToList
            freak.Compute(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), orb.keypoints, fdesc)

            dst2 = src.Clone()

            For Each kpt In keypoints
                Dim r = kpt.Size / 2
                DrawCircle(dst2, kpt.Pt, r, cv.Scalar.Green)
                vbc.DrawLine(dst2, New cv.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y - r), cv.Scalar.Green)
                vbc.DrawLine(dst2, New cv.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y + r), cv.Scalar.Green)
            Next
            labels(2) = CStr(orb.keypoints.Count) + " key points were identified"
            labels(3) = CStr(orb.keypoints.Count) + " FREAK Descriptors (resized) One row = keypoint"
            If fdesc.Width > 0 And fdesc.Height > 0 Then dst3 = fdesc.Resize(dst3.Size())
        End Sub
        Public Sub Close()
            If freak IsNot Nothing Then freak.Dispose()
        End Sub
    End Class
End Namespace