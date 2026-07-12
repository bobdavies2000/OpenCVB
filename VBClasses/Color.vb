Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Color_Basics : Inherits TaskParent
    Public options As New Options_Color
    Public Sub New()
        desc = "Choose a color source"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If options.colorFormat Is Nothing Then options.colorFormat = "BGR" ' multiple invocations cause this to be necessary but how to fix?
        Select Case options.colorFormat
            Case "BGR"
                dst2 = src.Clone
            Case "LAB"
                CvtColor(src, dst2, ColorConversionCodes.BGR2Lab)
            Case "HSV"
                CvtColor(src, dst2, ColorConversionCodes.BGR2HSV)
            Case "XYZ"
                CvtColor(src, dst2, ColorConversionCodes.BGR2XYZ)
            Case "HLS"
                CvtColor(src, dst2, ColorConversionCodes.BGR2HLS)
            Case "YUV"
                CvtColor(src, dst2, ColorConversionCodes.BGR2YUV)
            Case "YCrCb"
                CvtColor(src, dst2, ColorConversionCodes.BGR2YCrCb)
        End Select
    End Sub
End Class




Public Class Color_Measure : Inherits TaskParent
    Public Sub New()
        labels(2) = "Input image with any motion rectangles.  Adjust with 'colorDiffSlider.value'."
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        desc = "Measure the range of values color can have when no motion is present."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.gray

        Static lastSrc As Mat = dst2.Clone

        Dim threshold = task.fOptions.ColorDiffSlider.Value
        dst3.SetTo(0)
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim valSrc As Integer = dst2.At(Of Byte)(y, x)
                Dim valLast As Integer = lastSrc.At(Of Byte)(y, x)
                If Math.Abs(valSrc - valLast) >= threshold Then dst3.At(Of Byte)(y, x) = 255
            Next
        Next

        For Each index In task.motion.motionSort
            Rectangle(dst2, task.gridRects(index), Scalar.All(255), task.lineWidth)
            Rectangle(dst3, task.gridRects(index), Scalar.All(255), task.lineWidth)
        Next
        lastSrc = dst2.Clone

        Dim count = CountNonZero(dst3)
        labels(3) = "At " + " color diff threshold " + CStr(threshold) + ", differences: " + CStr(count)
    End Sub
End Class
