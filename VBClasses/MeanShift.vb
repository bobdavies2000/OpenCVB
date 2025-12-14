Imports cv = OpenCvSharp
' http://answers.opencvb.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics : Inherits TaskParent
    Public rectangleEdgeWidth As Integer = 2
    Public trackbox As New cv.Rect
    Dim histogram As New cv.Mat
    Public Sub New()
        If standalone Then algTask.gOptions.displaydst1.checked = true
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim roi = If(algTask.drawRect.Width > 0, algTask.drawRect, New cv.Rect(0, 0, dst2.Width, dst2.Height))
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim ch() As Integer = {0, 1, 2}
        Dim hsize() As Integer = {16, 16, 16}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180)}
        If algTask.optionsChanged Then
            trackbox = algTask.drawRect
            Dim maskROI = hsv(roi).InRange(New cv.Scalar(0, 60, 32), New cv.Scalar(180, 255, 255))
            cv.Cv2.CalcHist({hsv(roi)}, ch, maskROI, histogram, 1, hsize, ranges)
            histogram = histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        End If
        cv.Cv2.CalcBackProject({hsv}, ch, histogram, dst1, ranges)
        dst2 = src
        If trackbox.Width <> 0 Then
            cv.Cv2.MeanShift(dst1, trackbox, cv.TermCriteria.Both(10, 1))
            dst2.Rectangle(trackbox, cv.Scalar.Red, rectangleEdgeWidth, algTask.lineType)
            dst3 = Show_HSV_Hist(histogram)
            dst3 = dst3.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        End If
    End Sub
End Class




Public Class MeanShift_Depth : Inherits TaskParent
    Dim meanShift As New MeanShift_Basics
    Public Sub New()
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Use depth to start mean shift algorithm."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        meanShift.Run(algTask.depthRGB)
        dst2 = meanShift.dst2
        dst3 = meanShift.dst1
    End Sub
End Class
