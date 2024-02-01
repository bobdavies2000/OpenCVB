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







Public Class OpenCVExample_CalcBackProject_Demo2 : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public classCount As Integer = 10 ' initial value is just a guess.  It is refined after the first pass.
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        gOptions.HistBinSlider.Value = 6
        labels = {"", "Mask for isolated region", "Backprojection of the hsv 2D histogram", "Mask in image context"}
        desc = "OpenCV Sample CalcBackProject_Demo2"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim count As Integer

        If task.clickPoint <> New cv.Point Then

            Dim connectivity As Integer = 8
            Dim flags = connectivity Or (255 << 8) Or cv.FloodFillFlags.FixedRange Or cv.FloodFillFlags.MaskOnly
            Dim mask2 As New Mat(src.Rows + 2, src.Cols + 2, cv.MatType.CV_8U, 0)

            ' the delta between each regions value is 255 / classcount. no low or high bound needed.
            Dim delta = CInt(255 / classCount) - 1
            Dim bounds = New cv.Scalar(delta, delta, delta)
            count = cv.Cv2.FloodFill(dst2, mask2, task.clickPoint, 255, Nothing, bounds, bounds, flags)

            If count <> src.Total Then dst1 = mask2(New cv.Range(1, mask2.Rows - 1), New cv.Range(1, mask2.Cols - 1))
        End If
        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256)}

        Dim hsv As cv.Mat = task.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        cv.Cv2.CalcHist({hsv}, {0, 1}, New cv.Mat, histogram, 2, {task.histogramBins, task.histogramBins}, ranges)
        classCount = histogram.CountNonZero
        histogram = histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        cv.Cv2.CalcBackProject({hsv}, {0, 1}, histogram, dst2, ranges)

        dst3 = src
        dst3.SetTo(cv.Scalar.White, dst1)

        setTrueText("Click anywhere to isolate that region.", 3)
    End Sub
End Class
