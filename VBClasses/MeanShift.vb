Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
' http://answers.opencvb.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics : Inherits TaskParent
    Public rectangleEdgeWidth As Integer = 2
    Public trackbox As New cv.Rect
    Dim histogram As New Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim roi = If(task.drawRect.Width > 0, task.drawRect, New cv.Rect(0, 0, dst2.Width, dst2.Height))
        Dim hsv As New Mat
        CvtColor(src, hsv, ColorConversionCodes.BGR2HSV)
        Dim ch() As Integer = {0, 1, 2}
        Dim hsize() As Integer = {16, 16, 16}
        Dim ranges() = New Rangef() {New Rangef(0, 180)}
        If task.optionsChanged Then
            trackbox = task.drawRect
            Dim maskROI As New Mat
            InRange(hsv(roi), New Scalar(0, 60, 32), New Scalar(180, 255, 255), maskROI)
            CalcHist({hsv(roi)}, ch, maskROI, histogram, 1, hsize, ranges)
            Normalize(histogram, histogram, 0, 255, NormTypes.MinMax)
        End If
        CalcBackProject({hsv}, ch, histogram, dst1, ranges)
        dst2 = src
        If trackbox.Width <> 0 Then
            MeanShift(dst1, trackbox, TermCriteria.Both(10, 1))
            Rectangle(dst2, trackbox, Scalar.Red, rectangleEdgeWidth, task.lineType)
            dst3 = CamShift_Basics.Show_HSV_Hist(histogram)
            CvtColor(dst3, dst3, ColorConversionCodes.HSV2BGR)
        End If
    End Sub
End Class




Public Class XR_MeanShift_Depth : Inherits TaskParent
    Dim meanShift As New MeanShift_Basics
    Public Sub New()
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Use depth to start mean shift algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        meanShift.Run(task.depthRGB)
        dst2 = meanShift.dst2
        dst3 = meanShift.dst1
    End Sub
End Class
