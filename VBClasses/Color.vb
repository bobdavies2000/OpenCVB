Imports cv = OpenCvSharp
Imports VBClasses
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
                    dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2Lab)
                Case "HSV"
                    dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
                Case "XYZ"
                    dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2XYZ)
                Case "HLS"
                    dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2HLS)
                Case "YUV"
                    dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2YUV)
                Case "YCrCb"
                    dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2YCrCb)
            End Select
        End Sub
    End Class




    Public Class Color_Measure : Inherits TaskParent
        Public Sub New()
            labels(2) = "Input image with any motion rectangles.  Adjust with 'colorDiffSlider.value'."
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Measure the range of values color can have when no motion is present."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.gray

            Static lastSrc As cv.Mat = dst2.Clone

            Dim threshold = task.fOptions.ColorDiffSlider.Value
            Dim indexSrc = dst2.GetGenericIndexer(Of Byte)()
            Dim indexLast = lastSrc.GetGenericIndexer(Of Byte)()
            Dim indexOut = dst3.GetGenericIndexer(Of Byte)()
            dst3.SetTo(0)
            For y = 0 To src.Rows - 1
                For x = 0 To src.Cols - 1
                    Dim valSrc As Integer = indexSrc(y, x)
                    Dim valLast As Integer = indexLast(y, x)
                    If Math.Abs(valSrc - valLast) >= threshold Then indexOut(y, x) = 255
                Next
            Next

            For Each index In task.motion.motionSort
                dst2.Rectangle(task.gridRects(index), 255, task.lineWidth)
                dst3.Rectangle(task.gridRects(index), 255, task.lineWidth)
            Next
            lastSrc = dst2.Clone

            Dim count = dst3.CountNonZero()
            labels(3) = "At " + " color diff threshold " + CStr(threshold) + ", differences: " + CStr(count)
        End Sub
    End Class
