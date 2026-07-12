Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class Derivative_Basics : Inherits TaskParent
    Dim subD As New Derivative_Subtract
    Dim plotHist As New PlotBar_Basics
    Public Sub New()
        plotHist.removeZeroEntry = False
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst3 = New Mat(dst3.Size, MatType.CV_8U, 0)
        desc = "Compute the gradient in the Z depth and maintain the units for depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        subD.Run(task.pcSplit(2))

        Dim ranges = {New Rangef(-subD.options.mmThreshold - 0.00001, subD.options.mmThreshold + 0.00001)}
        Dim histogram As New Mat
        CalcHist({subD.dst2}, {0}, New Mat, histogram, 1, {task.histogramBins}, ranges)

        plotHist.Run(histogram)
        dst2 = plotHist.dst2

        Dim proximityCount = Sum(plotHist.histogram)
        Dim proximityPercent = proximityCount(0) / dst2.Total

        Dim barWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Truncate(task.mouseMovePoint.X / barWidth)

        Dim index As Integer = 1
        Dim bars = subD.options.histBars
        Dim center As Integer = task.histogramBins / 2
        Dim centerAdjust As Integer = If(task.histogramBins Mod 2 = 0, 1, 0)
        ' this is a variation of guided backprojection.
        For i = 0 To plotHist.histArray.Count - 1
            If i >= center - bars And i <= center + bars + centerAdjust Then
                plotHist.histArray(i) = 0
            Else
                plotHist.histArray(i) = 1
            End If
        Next

        histogram = Mat.FromPixelData(plotHist.histArray.Count, 1, MatType.CV_32F, plotHist.histArray)

        Dim mask As New Mat
        CalcBackProject({subD.dst2(subD.options.rect1)}, {0}, histogram, mask, ranges)

        mask.ConvertTo(mask, MatType.CV_8U)
                  InRange(mask, 1, 1, mask)

        dst1 = task.color.Clone
        dst3.SetTo(0)
        dst3(subD.options.rect1).SetTo(white, mask)
        dst3.SetTo(0, task.noDepthMask)
        dst1.SetTo(0, dst3)

        Dim nonz As New Mat
        FindNonZero(dst3, nonz)

        Rectangle(dst2, New cv.Rect(CInt((center - bars) * barWidth), 0,
                               barWidth * (bars * 2 + centerAdjust), dst2.Height),
                               task.highlight, task.lineWidth)

        labels(2) = CStr(proximityCount) + " depth points were within " +
                            CStr(subD.options.mmThreshold * 1000) + " mm's of their neighbor or " +
                            proximityPercent.ToString("0%")

        Dim proxDistance = 1000 * (bars * 2 + centerAdjust) * subD.options.mmThreshold * 2 /
                                   task.histogramBins
        labels(3) = "Of the " + CStr(proximityCount) + " depth points, " + CStr(nonz.Rows) +
                            " were within " + proxDistance.ToString(fmt1) + " mm's of their neighbor"
    End Sub
End Class





Public Class Derivative_Subtract : Inherits TaskParent
    Public options As New Options_DerivativeBasics
    Public Sub New()
        dst2 = New Mat(dst2.Size(), MatType.CV_32FC1, 0)
        desc = "Subtract neighboring cells in the cv.Point cloud depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> MatType.CV_32F Then src = task.pcSplit(2)

        dst2(options.rect1) = src(options.rect1).Subtract(src(options.rect2))
        If standaloneTest() Then
            Dim mm = GetMinMax(dst2)
            ConvertScaleAbs(dst2, dst2, 255, -mm.minVal)
        End If
    End Sub
End Class






Public Class Derivative_Sobel : Inherits TaskParent
    Public options As New Options_Derivative
    Public plotHist As New PlotBar_Basics
    Public Sub New()
        desc = "Display a first or second derivative of the selected depth dimension and direction."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> MatType.CV_32F Then src = task.pcSplit(options.channel)
        Sobel(src, src, MatType.CV_32F, 1, 1, options.kernelSize)

        Dim ranges = {New Rangef(-options.derivativeRange, options.derivativeRange)}
        Dim histogram As New Mat
        CalcHist({src}, {0}, task.depthmask, histogram, 1, {task.histogramBins}, ranges)

        plotHist.Run(histogram)
        histogram = plotHist.histogram ' reflect any updates to the 0 entry...
        dst2 = plotHist.dst2

        Dim index As Integer = 1
        For i = 0 To plotHist.histArray.Count - 1
            If plotHist.histArray(i) <> 0 Then
                plotHist.histArray(i) = index
                index += 1
            End If
        Next
        dst1 = Mat.FromPixelData(plotHist.histArray.Count, 1, MatType.CV_32F, plotHist.histArray)

        Dim barWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Truncate(task.mouseMovePoint.X / barWidth)

        Dim mask As New Mat
        CalcBackProject({src}, {0}, dst1, mask, ranges)
        mask.ConvertTo(mask, MatType.CV_8U)
        dst0 = mask
                  InRange(mask, histIndex, histIndex, mask)

        dst3 = task.color.Clone
        dst3.SetTo(white, mask)
        dst3.SetTo(0, task.noDepthMask)
        Rectangle(dst2, New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst2.Height), Scalar.Yellow, task.lineWidth)
        Dim deriv = options.derivativeRange.ToString(fmt2)
        labels(2) = "Histogram of first or second derivatives.  Range -" + deriv + " to " + deriv
        labels(3) = "Backprojection into the image for the selected histogram entry - move mouse over dst2."
    End Sub
End Class






Public Class XR_Derivative_Sobel1 : Inherits TaskParent
    Dim deriv As New Derivative_Sobel
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Display the derivative of the selected depth dimension."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim channel = deriv.options.channel
        Dim chanName As String = Choose(channel + 1, "X", "Y", "Z")
        Dim kern = deriv.options.kernelSize
        Sobel(task.pcSplit(channel), src, MatType.CV_32F, 1, 0, kern)
        deriv.Run(src)
        dst0 = deriv.dst2.Clone
        dst1 = deriv.dst3.Clone
        labels(0) = "Horizontal derivatives for " + chanName + " dimension of the cv.Point cloud"
        labels(1) = "Backprojection of horizontal derivatives indicated - move mouse in the image at left"

        Sobel(task.pcSplit(channel), src, MatType.CV_32F, 0, 1, kern)
        deriv.Run(src)
        dst2 = deriv.dst2
        dst3 = deriv.dst3
        labels(2) = "Vertical derivatives for " + chanName + " dimension of the cv.Point cloud"
        labels(3) = "Backprojection of vertical derivatives indicated - move mouse in the image at left"
    End Sub
End Class








Public Class XR_Derivative_Laplacian : Inherits TaskParent
    Dim options As New Options_LaplacianKernels
    Dim deriv As New Derivative_Sobel
    Public Sub New()
        desc = "Create a histogram and backprojection for the second derivative of depth in the selected dimension."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim channel = deriv.options.channel
        Dim gausskern = New Size(CInt(options.gaussiankernelSize), CInt(options.gaussiankernelSize))
        GaussianBlur(task.pcSplit(channel), dst1, gausskern, 0, 0)
        Laplacian(dst1, dst1, MatType.CV_32F, options.LaplaciankernelSize, 1, 0)

        deriv.Run(dst1)
        dst2 = deriv.dst2
        dst3 = deriv.dst3
        labels(2) = deriv.labels(2)
        labels(3) = deriv.labels(3)
    End Sub
End Class






Public Class XR_Derivative_Classes : Inherits TaskParent
    Dim deriv As New Derivative_Sobel
    Public classCountX As Integer
    Public classCountY As Integer
    Public Sub New()
        desc = "Display the X and Y derivatives for the whole image."
    End Sub
    Private Function derivClassCount(ByRef dst As Mat) As Integer
        For i = 0 To deriv.plotHist.histArray.Count - 1
            If deriv.plotHist.histArray(i) > 0 Then derivClassCount += 1
        Next
        dst = Palettize(deriv.dst0)
        dst.SetTo(0, task.noDepthMask)
        Return derivClassCount
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim tmp As New Mat
        Sobel(task.pcSplit(deriv.options.channel), tmp, MatType.CV_32F, 1, 0, deriv.options.kernelSize)
        deriv.Run(tmp)
        classCountX = derivClassCount(dst2)
        labels(2) = $"Backprojection of X dimension of task.pcSplit({deriv.options.channel})"

        Sobel(task.pcSplit(deriv.options.channel), tmp, MatType.CV_32F, 0, 1, deriv.options.kernelSize)
        deriv.Run(tmp)
        classCountY = derivClassCount(dst3)
        labels(3) = $"Backprojection of Y dimension of task.pcSplit({deriv.options.channel})"
    End Sub
End Class
