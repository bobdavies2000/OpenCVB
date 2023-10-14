Imports cv = OpenCvSharp
' http://answers.opencv.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics : Inherits VB_Algorithm
    Public rectangleEdgeWidth As integer = 2
    Public inputRect As cv.Rect
    Public trackbox As New cv.Rect
    Public usingDrawRect As Boolean
    Public Sub New()
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standalone Then usingDrawRect = True
        If usingDrawRect Then inputRect = task.drawRect
        If inputRect.X + inputRect.Width > src.Width Then inputRect.Width = src.Width - inputRect.X
        If inputRect.Y + inputRect.Height > src.Height Then inputRect.Height = src.Height - inputRect.Y
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim ch() As Integer = {0, 1, 2}
        Dim hsize() As Integer = {16, 16, 16}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180)}
        Static histogram As New cv.Mat
        If inputRect.Width > 0 And inputRect.Height > 0 Then
            If usingDrawRect Then trackbox = task.drawRect Else trackbox = inputRect
            Dim maskROI = hsv(inputRect).InRange(New cv.Scalar(0, 60, 32), New cv.Scalar(180, 255, 255))
            cv.Cv2.CalcHist({hsv(inputRect)}, ch, maskROI, histogram, 1, hsize, ranges)
            histogram = histogram.Normalize(0, 255, cv.NormTypes.MinMax)
            If usingDrawRect Then task.drawRectClear = True
        End If
        If trackbox.Width <> 0 Then
            Dim backProj As New cv.Mat
            cv.Cv2.CalcBackProject({hsv}, ch, histogram, backProj, ranges)
            cv.Cv2.MeanShift(backProj, trackbox, cv.TermCriteria.Both(10, 1))
            dst2 = backProj.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst2.Rectangle(trackbox, cv.Scalar.Red, rectangleEdgeWidth, task.lineType)
            dst3 = Show_HSV_Hist(histogram)
            dst3 = dst3.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        Else
            dst2 = src
        End If
    End Sub
End Class




Public Class MeanShift_Depth : Inherits VB_Algorithm
    Dim ms As New MeanShift_Basics
    Dim blob As New Depth_ForegroundHead
    Public Sub New()
        labels(2) = "Draw anywhere to start mean shift tracking."
        desc = "Use depth to start mean shift algorithm."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.drawRect.Width > 0 Then
            ms.usingDrawRect = True
            ms.inputRect = New cv.Rect
        End If
        If ms.usingDrawRect Then
            ms.Run(src)
            dst2 = ms.dst2
            dst3 = ms.dst3
        Else
            blob.Run(src)
            dst2 = blob.dst2

            If blob.trustworthy Then
                ms.inputRect = blob.trustedRect
                ms.Run(src)
                dst3 = ms.dst3
                dst2 = ms.dst2
            Else
                dst3 = src
            End If
        End If
    End Sub
End Class