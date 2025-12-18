Imports OpenCvSharp.XFeatures2D
Imports cv = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/StarDetectorSample.vb
Namespace VBClasses
    Public Class XFeatures2D_StarDetector : Inherits TaskParent
        Dim detector As StarDetector
        Public Sub New()
            desc = "Basics of the StarDetector - a 2D feature detector.  FAILS IN COMPUTE.  Uncomment to investigate further."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone()
            If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If detector Is Nothing Then detector = OpenCvSharp.XFeatures2D.StarDetector.Create()
            Dim keypoints() = detector.Detect(src)

            If keypoints IsNot Nothing Then
                For Each kpt As cv.KeyPoint In keypoints
                    Dim r As Single = kpt.Size / 2
                    DrawCircle(dst2, kpt.Pt, CInt(Math.Truncate(r)), New cv.Scalar(0, 255, 0))
                    dst2.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y - r), New cv.Scalar(0, 255, 0), task.lineWidth, cv.LineTypes.Link8, 0)
                    dst2.Line(New cv.Point(kpt.Pt.X - r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cv.Scalar(0, 255, 0), task.lineWidth, cv.LineTypes.Link8, 0)
                Next kpt
            End If
        End Sub
        Public Sub Close()
            If detector IsNot Nothing Then detector.Dispose()
        End Sub
    End Class
End Namespace