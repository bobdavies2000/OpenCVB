Imports cvb = OpenCvSharp
' http://answers.opencvb.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics : Inherits TaskParent
    Public rectangleEdgeWidth As Integer = 2
    Public trackbox As New cvb.Rect
    Dim histogram As New cvb.Mat
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Dim roi = If(task.drawRect.Width > 0, task.drawRect, New cvb.Rect(0, 0, dst2.Width, dst2.Height))
        Dim hsv = src.CvtColor(cvb.ColorConversionCodes.BGR2HSV)
        Dim ch() As Integer = {0, 1, 2}
        Dim hsize() As Integer = {16, 16, 16}
        Dim ranges() = New cvb.Rangef() {New cvb.Rangef(0, 180)}
        If task.optionsChanged Then
            trackbox = task.drawRect
            Dim maskROI = hsv(roi).InRange(New cvb.Scalar(0, 60, 32), New cvb.Scalar(180, 255, 255))
            cvb.Cv2.CalcHist({hsv(roi)}, ch, maskROI, histogram, 1, hsize, ranges)
            histogram = histogram.Normalize(0, 255, cvb.NormTypes.MinMax)
        End If
        cvb.Cv2.CalcBackProject({hsv}, ch, histogram, dst1, ranges)
        dst2 = src
        If trackbox.Width <> 0 Then
            cvb.Cv2.MeanShift(dst1, trackbox, cvb.TermCriteria.Both(10, 1))
            dst2.Rectangle(trackbox, cvb.Scalar.Red, rectangleEdgeWidth, task.lineType)
            dst3 = Show_HSV_Hist(histogram)
            dst3 = dst3.CvtColor(cvb.ColorConversionCodes.HSV2BGR)
        End If
    End Sub
End Class




Public Class MeanShift_Depth : Inherits TaskParent
    Dim meanShift As New MeanShift_Basics
    Public Sub New()
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Use depth to start mean shift algorithm."
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        meanShift.Run(task.depthRGB)
        dst2 = meanShift.dst2
        dst3 = meanShift.dst1
    End Sub
End Class
