Imports cv = OpenCvSharp
' http://answers.opencv.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics : Inherits VB_Algorithm
    Public rectangleEdgeWidth As Integer = 2
    Public trackbox As New cv.Rect
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim roi = If(task.drawRect.Width > 0, task.drawRect, New cv.Rect(0, 0, dst2.Width, dst2.Height))
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim ch() As Integer = {0, 1, 2}
        Dim hsize() As Integer = {16, 16, 16}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180)}
        Static histogram As New cv.Mat
        If task.optionsChanged Then
            trackbox = task.drawRect
            Dim maskROI = hsv(roi).InRange(New cv.Scalar(0, 60, 32), New cv.Scalar(180, 255, 255))
            cv.Cv2.CalcHist({hsv(roi)}, ch, maskROI, histogram, 1, hsize, ranges)
            histogram = histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        End If
        cv.Cv2.CalcBackProject({hsv}, ch, histogram, dst1, ranges)
        dst2 = src
        If trackbox.Width <> 0 Then
            cv.Cv2.MeanShift(dst1, trackbox, cv.TermCriteria.Both(10, 1))
            dst2.Rectangle(trackbox, cv.Scalar.Red, rectangleEdgeWidth, task.lineType)
            dst3 = Show_HSV_Hist(histogram)
            dst3 = dst3.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        End If
    End Sub
End Class




Public Class MeanShift_Depth : Inherits VB_Algorithm
    Dim meanShift As New MeanShift_Basics
    Public Sub New()
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Use depth to start mean shift algorithm."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        meanShift.Run(task.depthRGB)
        dst2 = meanShift.dst2
        dst3 = meanShift.dst1
    End Sub
End Class
