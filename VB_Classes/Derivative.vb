Imports cv = OpenCvSharp
Public Class Derivative_Basics : Inherits VB_Parent
    Public options As New Options_Derivative
    Dim backp As New BackProject_Image
    Public plot As New Plot_Histogram
    Public Sub New()
        backp.hist.plot.removeZeroEntry = False
        UpdateAdvice(traceName + ": gOptions histogram Bins and several local options are important.")
        desc = "Display a first or second derivative of the selected depth dimension and direction."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Type <> cv.MatType.CV_32F Then
            src = task.pcSplit(options.channel).Sobel(cv.MatType.CV_32F, 1, 0, options.kernelSize)
        End If

        Dim ranges = {New cv.Rangef(-options.derivativeRange, options.derivativeRange)}
        Dim histogram As New cv.Mat
        cv.Cv2.CalcHist({src}, {0}, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

        plot.Run(histogram)
        histogram = plot.histogram ' reflect any updates to the 0 entry...
        dst2 = plot.dst2

        Dim index As Integer = 1
        For i = 0 To plot.histArray.Count - 1
            If plot.histArray(i) <> 0 Then
                plot.histArray(i) = index
                index += 1
            End If
        Next
        histogram = New cv.Mat(plot.histArray.Count, 1, cv.MatType.CV_32F, plot.histArray)

        Dim brickWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Truncate(task.mouseMovePoint.X / brickWidth)

        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject({src}, {0}, histogram, mask, ranges)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        dst0 = mask
        mask = mask.InRange(histIndex, histIndex)

        dst3 = task.color.Clone
        dst3.SetTo(cv.Scalar.White, mask)
        dst3.SetTo(0, task.noDepthMask)
        dst2.Rectangle(New cv.Rect(CInt(histIndex * brickWidth), 0, brickWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
        Dim deriv = Format(options.derivativeRange, fmt2)
        labels(2) = "Histogram of first or second derivatives.  Range -" + deriv + " to " + deriv
        labels(3) = "Backprojection into the image for the selected histogram entry - move mouse over dst2."
    End Sub
End Class






Public Class Derivative_Sobel : Inherits VB_Parent
    Dim deriv As New Derivative_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Display the derivative of the selected depth dimension."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim channel = deriv.options.channel
        Dim chanName As String = "X"
        If channel <> 0 Then
            If channel = 1 Then chanName = "Y" Else chanName = "Z"
        End If
        Dim kern = deriv.options.kernelSize
        src = task.pcSplit(channel).Sobel(cv.MatType.CV_32F, 1, 0, kern)
        deriv.Run(src)
        dst0 = deriv.dst2.Clone
        dst1 = deriv.dst3.Clone
        labels(0) = "Horizontal derivatives for " + chanName + " dimension of the point cloud"
        labels(1) = "Backprojection of horizontal derivatives indicated - move mouse in the image at left"

        src = task.pcSplit(channel).Sobel(cv.MatType.CV_32F, 0, 1, kern)
        deriv.Run(src)
        dst2 = deriv.dst2
        dst3 = deriv.dst3
        labels(2) = "Vertical derivatives for " + chanName + " dimension of the point cloud"
        labels(3) = "Backprojection of vertical derivatives indicated - move mouse in the image at left"
    End Sub
End Class








Public Class Derivative_Laplacian : Inherits VB_Parent
    Dim options As New Options_LaplacianKernels
    Dim deriv As New Derivative_Basics
    Public Sub New()
        desc = "Create a histogram and backprojection for the second derivative of depth in the selected dimension."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim channel = deriv.options.channel
        Dim gausskern = New cv.Size(CInt(options.gaussiankernelSize), CInt(options.gaussiankernelSize))
        dst1 = task.pcSplit(channel).GaussianBlur(gausskern, 0, 0)
        dst1 = dst1.Laplacian(cv.MatType.CV_32F, options.LaplaciankernelSize, 1, 0)

        deriv.Run(dst1)
        dst2 = deriv.dst2
        dst3 = deriv.dst3
        labels(2) = deriv.labels(2)
        labels(3) = deriv.labels(3)
    End Sub
End Class






Public Class Derivative_Classes : Inherits VB_Parent
    Dim deriv As New Derivative_Basics
    Public classCountX As Integer
    Public classCountY As Integer
    Public Sub New()
        desc = "Display the X and Y derivatives for the whole image."
    End Sub
    Private Function derivClassCount(ByRef dst As cv.Mat) As Integer
        For i = 0 To deriv.plot.histArray.Count - 1
            If deriv.plot.histArray(i) > 0 Then derivClassCount += 1
        Next
        dst = ShowPalette(deriv.dst0 * 255 / derivClassCount)
        dst.SetTo(0, task.noDepthMask)
        Return derivClassCount
    End Function
    Public Sub RunVB(src As cv.Mat)
        deriv.Run(task.pcSplit(deriv.options.channel).Sobel(cv.MatType.CV_32F, 1, 0, deriv.options.kernelSize))
        classCountX = derivClassCount(dst2)
        labels(2) = $"Backprojection of X dimension of task.pcSplit({deriv.options.channel})"

        deriv.Run(task.pcSplit(deriv.options.channel).Sobel(cv.MatType.CV_32F, 0, 1, deriv.options.kernelSize))
        classCountY = derivClassCount(dst3)
        labels(3) = $"Backprojection of Y dimension of task.pcSplit({deriv.options.channel})"
    End Sub
End Class
