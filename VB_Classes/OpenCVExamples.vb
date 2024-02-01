Imports OpenCvSharp
Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' all examples in this file are from https://github.com/opencv/opencv/tree/4.x/samples
Public Class OpenCVExample_CalcBackProject_Demo1 : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public classCount As Integer
    Public Sub New()
        labels = {"", "", "BackProjection of Hue channel", "Plot of Hue histogram"}
        vbAddAdvice(traceName + ": <place advice here on any options that are useful>")
        desc = "OpenCV Sample CalcBackProject_Demo1"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180)}

        Dim hsv As cv.Mat = task.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        cv.Cv2.CalcHist({hsv}, {0}, New cv.Mat, histogram, 1, {task.histogramBins}, ranges)
        classCount = histogram.CountNonZero
        dst0 = histogram.Normalize(0, classCount, cv.NormTypes.MinMax) ' for the backprojection.

        Dim histArray(histogram.Total - 1) As Single
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        Dim peakValue = histArray.ToList.Max

        histogram = histogram.Normalize(0, 1, cv.NormTypes.MinMax)
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        cv.Cv2.CalcBackProject({hsv}, {0}, dst0, dst2, ranges)

        dst3.SetTo(cv.Scalar.Red)
        Dim binW = dst2.Width / task.histogramBins
        Dim bins = dst2.Width / binW
        For i = 0 To bins - 1
            Dim h = dst2.Height * histArray(i)
            Dim r = New cv.Rect(i * binW, dst2.Height - h, binW, h)
            dst3.Rectangle(r, cv.Scalar.Black, -1)
        Next
        If task.heartBeat Then labels(3) = $"The max value below is {peakValue}"
    End Sub
End Class
