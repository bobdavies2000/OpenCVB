Imports cv = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/StarDetectorSample.vb
Public Class XFeatures2D_StarDetector : Inherits VBparent
    Public Sub New()
        task.desc = "Basics of the StarDetector - a 2D feature detector.  FAILS IN COMPUTE.  Uncomment to investigate further."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst2 = src.Clone()
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim detector = OpenCvSharp.XFeatures2D.StarDetector.Create()
        Dim keypoints() = detector.Detect(src)

        If keypoints IsNot Nothing Then
            For Each kpt As cv.KeyPoint In keypoints
                Dim r As Single = kpt.Size / 2
                dst2.Circle(kpt.Pt, CInt(Math.Truncate(r)), New cv.Scalar(0, 255, 0), 1, cv.LineTypes.Link8, 0)
                dst2.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y - r), New cv.Scalar(0, 255, 0), task.lineWidth, cv.LineTypes.Link8, 0)
                dst2.Line(New cv.Point(kpt.Pt.X - r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cv.Scalar(0, 255, 0), task.lineWidth, cv.LineTypes.Link8, 0)
            Next kpt
        End If
    End Sub
End Class



