Imports cvb = OpenCvSharp
Public Class Derivative_Basics : Inherits TaskParent
    Dim options As New Options_DerivativeBasics
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.removeZeroEntry = False
        task.gOptions.setMaxDepth(25)
        If standalone Then task.gOptions.setDisplay1()
        dst0 = New cvb.Mat(dst0.Size(), cvb.MatType.CV_32FC1, 0)
        dst3 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U, 0)
        desc = "Compute the gradient in the Z depth and maintain the units for depth."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        src = task.pcSplit(2)
        Dim offsetX As Integer = If(options.horizontalDerivative, 1, 0)
        Dim offsetY As Integer = If(options.verticalDerivative, 1, 0)
        If offsetX = 0 And offsetY = 0 Then
            offsetX = 1
            offsetY = 1
            SetTrueText("Switched to using both horizontal and vertical derivatives", 3)
        End If
        Dim r1 = New cvb.Rect(0, 0, dst2.Width - offsetX, dst2.Height - offsetY)
        Dim r2 = New cvb.Rect(offsetX, offsetY, dst2.Width - offsetX, dst2.Height - offsetY)

        dst0(r1) = src(r1).Subtract(src(r2))

        Dim ranges = {New cvb.Rangef(-options.mmThreshold - 0.00001, options.mmThreshold + 0.00001)}
        Dim histogram As New cvb.Mat
        cvb.Cv2.CalcHist({dst0}, {0}, New cvb.Mat, histogram, 1, {task.histogramBins}, ranges)

        plot.Run(histogram)
        dst2 = plot.dst2

        Dim proximityCount As Integer = plot.histogram.Sum
        Dim proximityPercent = proximityCount / src.CountNonZero

        Dim brickWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Truncate(task.mouseMovePoint.X / brickWidth)

        ' this is guided backprojection.
        Dim index As Integer = 1
        Dim center As Integer = task.histogramBins / 2
        Dim centerAdjust As Integer = If(task.histogramBins Mod 2 = 0, 1, 0)
        For i = 0 To plot.histArray.Count - 1
            If i >= center - options.histBars And i <= center + options.histBars + centerAdjust Then
                plot.histArray(i) = 1
            Else
                plot.histArray(i) = 0
            End If
        Next

        histogram = cvb.Mat.FromPixelData(plot.histArray.Count, 1, cvb.MatType.CV_32F, plot.histArray)

        Dim mask As New cvb.Mat
        cvb.Cv2.CalcBackProject({dst0(r1)}, {0}, histogram, mask, ranges)

        mask.ConvertTo(mask, cvb.MatType.CV_8U)
        mask = mask.InRange(1, 1)

        dst1 = task.color.Clone
        dst3.SetTo(0)
        dst3(r1).SetTo(white, mask)
        dst3.SetTo(0, task.noDepthMask)
        dst1.SetTo(0, dst3)



        Dim nonz = dst3.FindNonZero()




        dst2.Rectangle(New cvb.Rect(CInt((center - options.histBars) * brickWidth), 0,
                       brickWidth * (options.histBars * 2 + centerAdjust), dst2.Height),
                       task.HighlightColor, task.lineWidth)

        labels(2) = CStr(proximityCount) + " depth points were within " + CStr(options.mmThreshold * 1000) +
                    " mm's of their neighbor or " + Format(proximityPercent, "0%")

        Dim proxDistance = 1000 * (options.histBars * 2 + centerAdjust) * options.mmThreshold * 2 / task.histogramBins
        labels(3) = "Of the " + CStr(proximityCount) + " depth points, " + CStr(nonz.Rows) +
                    " were within " + Format(proxDistance, fmt1) + " mm's of their neighbor"
    End Sub
End Class






Public Class Derivative_Sobel : Inherits TaskParent
    Public options As New Options_Derivative
    Public plot As New Plot_Histogram
    Public Sub New()
        UpdateAdvice(traceName + ": gOptions histogram Bins and several local options are important.")
        desc = "Display a first or second derivative of the selected depth dimension and direction."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Type <> cvb.MatType.CV_32F Then src = task.pcSplit(options.channel)
        src = src.Sobel(cvb.MatType.CV_32F, 1, 1, options.kernelSize)

        Dim ranges = {New cvb.Rangef(-options.derivativeRange, options.derivativeRange)}
        Dim histogram As New cvb.Mat
        cvb.Cv2.CalcHist({src}, {0}, task.depthMask, histogram, 1, {task.histogramBins}, ranges)

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
        dst1 = cvb.Mat.FromPixelData(plot.histArray.Count, 1, cvb.MatType.CV_32F, plot.histArray)

        Dim brickWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Truncate(task.mouseMovePoint.X / brickWidth)

        Dim mask As New cvb.Mat
        cvb.Cv2.CalcBackProject({src}, {0}, dst1, mask, ranges)
        mask.ConvertTo(mask, cvb.MatType.CV_8U)
        dst0 = mask
        mask = mask.InRange(histIndex, histIndex)

        dst3 = task.color.Clone
        dst3.SetTo(white, mask)
        dst3.SetTo(0, task.noDepthMask)
        dst2.Rectangle(New cvb.Rect(CInt(histIndex * brickWidth), 0, brickWidth, dst2.Height), cvb.Scalar.Yellow, task.lineWidth)
        Dim deriv = Format(options.derivativeRange, fmt2)
        labels(2) = "Histogram of first or second derivatives.  Range -" + deriv + " to " + deriv
        labels(3) = "Backprojection into the image for the selected histogram entry - move mouse over dst2."
    End Sub
End Class






Public Class Derivative_Sobel1 : Inherits TaskParent
    Dim deriv As New Derivative_Sobel
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Display the derivative of the selected depth dimension."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim channel = deriv.options.channel
        Dim chanName As String = "X"
        If channel <> 0 Then
            If channel = 1 Then chanName = "Y" Else chanName = "Z"
        End If
        Dim kern = deriv.options.kernelSize
        src = task.pcSplit(channel).Sobel(cvb.MatType.CV_32F, 1, 0, kern)
        deriv.Run(src)
        dst0 = deriv.dst2.Clone
        dst1 = deriv.dst3.Clone
        labels(0) = "Horizontal derivatives for " + chanName + " dimension of the point cloud"
        labels(1) = "Backprojection of horizontal derivatives indicated - move mouse in the image at left"

        src = task.pcSplit(channel).Sobel(cvb.MatType.CV_32F, 0, 1, kern)
        deriv.Run(src)
        dst2 = deriv.dst2
        dst3 = deriv.dst3
        labels(2) = "Vertical derivatives for " + chanName + " dimension of the point cloud"
        labels(3) = "Backprojection of vertical derivatives indicated - move mouse in the image at left"
    End Sub
End Class








Public Class Derivative_Laplacian : Inherits TaskParent
    Dim options As New Options_LaplacianKernels
    Dim deriv As New Derivative_Sobel
    Public Sub New()
        desc = "Create a histogram and backprojection for the second derivative of depth in the selected dimension."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim channel = deriv.options.channel
        Dim gausskern = New cvb.Size(CInt(options.gaussiankernelSize), CInt(options.gaussiankernelSize))
        dst1 = task.pcSplit(channel).GaussianBlur(gausskern, 0, 0)
        dst1 = dst1.Laplacian(cvb.MatType.CV_32F, options.LaplaciankernelSize, 1, 0)

        deriv.Run(dst1)
        dst2 = deriv.dst2
        dst3 = deriv.dst3
        labels(2) = deriv.labels(2)
        labels(3) = deriv.labels(3)
    End Sub
End Class






Public Class Derivative_Classes : Inherits TaskParent
    Dim deriv As New Derivative_Sobel
    Public classCountX As Integer
    Public classCountY As Integer
    Public Sub New()
        desc = "Display the X and Y derivatives for the whole image."
    End Sub
    Private Function derivClassCount(ByRef dst As cvb.Mat) As Integer
        For i = 0 To deriv.plot.histArray.Count - 1
            If deriv.plot.histArray(i) > 0 Then derivClassCount += 1
        Next
        dst = ShowPalette(deriv.dst0 * 255 / derivClassCount)
        dst.SetTo(0, task.noDepthMask)
        Return derivClassCount
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        deriv.Run(task.pcSplit(deriv.options.channel).Sobel(cvb.MatType.CV_32F, 1, 0, deriv.options.kernelSize))
        classCountX = derivClassCount(dst2)
        labels(2) = $"Backprojection of X dimension of task.pcSplit({deriv.options.channel})"

        deriv.Run(task.pcSplit(deriv.options.channel).Sobel(cvb.MatType.CV_32F, 0, 1, deriv.options.kernelSize))
        classCountY = derivClassCount(dst3)
        labels(3) = $"Backprojection of Y dimension of task.pcSplit({deriv.options.channel})"
    End Sub
End Class
