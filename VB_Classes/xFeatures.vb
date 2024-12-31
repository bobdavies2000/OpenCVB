Imports cvb = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/StarDetectorSample.vb
Public Class XFeatures2D_StarDetector : Inherits TaskParent
    Public Sub New()
        desc = "Basics of the StarDetector - a 2D feature detector.  FAILS IN COMPUTE.  Uncomment to investigate further."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        dst2 = src.Clone()
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2Gray)
        Dim detector = OpenCvSharp.XFeatures2D.StarDetector.Create()
        Dim keypoints() = detector.Detect(src)

        If keypoints IsNot Nothing Then
            For Each kpt As cvb.KeyPoint In keypoints
                Dim r As Single = kpt.Size / 2
                DrawCircle(dst2, kpt.Pt, CInt(Math.Truncate(r)), New cvb.Scalar(0, 255, 0))
                dst2.Line(New cvb.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cvb.Point(kpt.Pt.X - r, kpt.Pt.Y - r), New cvb.Scalar(0, 255, 0), task.lineWidth, cvb.LineTypes.Link8, 0)
                dst2.Line(New cvb.Point(kpt.Pt.X - r, kpt.Pt.Y + r), New cvb.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cvb.Scalar(0, 255, 0), task.lineWidth, cvb.LineTypes.Link8, 0)
            Next kpt
        End If
    End Sub
End Class



