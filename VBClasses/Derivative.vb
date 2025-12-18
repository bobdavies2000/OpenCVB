Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Derivative_Basics : Inherits TaskParent
        Dim subD As New Derivative_Subtract
        Dim plotHist As New Plot_Histogram
        Public Sub New()
            plotHist.removeZeroEntry = False
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Compute the gradient in the Z depth and maintain the units for depth."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            subD.Run(algTask.pcSplit(2))

            Dim ranges = {New cv.Rangef(-subD.options.mmThreshold - 0.00001, subD.options.mmThreshold + 0.00001)}
            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({subD.dst2}, {0}, New cv.Mat, histogram, 1, {algTask.histogramBins}, ranges)

            plotHist.Run(histogram)
            dst2 = plotHist.dst2

            Dim proximityCount As Integer = plotHist.histogram.Sum
            Dim proximityPercent = proximityCount / dst2.Total

            Dim brickWidth = dst2.Width / algTask.histogramBins
            Dim histIndex = Math.Truncate(algTask.mouseMovePoint.X / brickWidth)

            Dim index As Integer = 1
            Dim bars = subD.options.histBars
            Dim center As Integer = algTask.histogramBins / 2
            Dim centerAdjust As Integer = If(algTask.histogramBins Mod 2 = 0, 1, 0)
            ' this is a variation of guided backprojection.
            For i = 0 To plotHist.histArray.Count - 1
                If i >= center - bars And i <= center + bars + centerAdjust Then
                    plotHist.histArray(i) = 0
                Else
                    plotHist.histArray(i) = 1
                End If
            Next

            histogram = cv.Mat.FromPixelData(plotHist.histArray.Count, 1, cv.MatType.CV_32F, plotHist.histArray)

            Dim mask As New cv.Mat
            cv.Cv2.CalcBackProject({subD.dst2(subD.options.rect1)}, {0}, histogram, mask, ranges)

            mask.ConvertTo(mask, cv.MatType.CV_8U)
            mask = mask.InRange(1, 1)

            dst1 = algTask.color.Clone
            dst3.SetTo(0)
            dst3(subD.options.rect1).SetTo(white, mask)
            dst3.SetTo(0, algTask.noDepthMask)
            dst1.SetTo(0, dst3)

            Dim nonz = dst3.FindNonZero()

            dst2.Rectangle(New cv.Rect(CInt((center - bars) * brickWidth), 0,
                           brickWidth * (bars * 2 + centerAdjust), dst2.Height),
                           algTask.highlight, algTask.lineWidth)

            labels(2) = CStr(proximityCount) + " depth points were within " +
                        CStr(subD.options.mmThreshold * 1000) + " mm's of their neighbor or " +
                        Format(proximityPercent, "0%")

            Dim proxDistance = 1000 * (bars * 2 + centerAdjust) * subD.options.mmThreshold * 2 /
                               algTask.histogramBins
            labels(3) = "Of the " + CStr(proximityCount) + " depth points, " + CStr(nonz.Rows) +
                        " were within " + Format(proxDistance, fmt1) + " mm's of their neighbor"
        End Sub
    End Class





    Public Class Derivative_Subtract : Inherits TaskParent
        Public options As New Options_DerivativeBasics
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC1, 0)
            desc = "Subtract neighboring cells in the point cloud depth."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Type <> cv.MatType.CV_32F Then src = algTask.pcSplit(2)

            dst2(options.rect1) = src(options.rect1).Subtract(src(options.rect2))
            If standaloneTest() Then
                Dim mm = GetMinMax(dst2)
                dst2 = dst2.ConvertScaleAbs(255, -mm.minVal)
            End If
        End Sub
    End Class






    Public Class Derivative_Sobel : Inherits TaskParent
        Public options As New Options_Derivative
        Public plotHist As New Plot_Histogram
        Public Sub New()
            desc = "Display a first or second derivative of the selected depth dimension and direction."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Type <> cv.MatType.CV_32F Then src = algTask.pcSplit(options.channel)
            src = src.Sobel(cv.MatType.CV_32F, 1, 1, options.kernelSize)

            Dim ranges = {New cv.Rangef(-options.derivativeRange, options.derivativeRange)}
            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({src}, {0}, algTask.depthMask, histogram, 1, {algTask.histogramBins}, ranges)

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
            dst1 = cv.Mat.FromPixelData(plotHist.histArray.Count, 1, cv.MatType.CV_32F, plotHist.histArray)

            Dim brickWidth = dst2.Width / algTask.histogramBins
            Dim histIndex = Math.Truncate(algTask.mouseMovePoint.X / brickWidth)

            Dim mask As New cv.Mat
            cv.Cv2.CalcBackProject({src}, {0}, dst1, mask, ranges)
            mask.ConvertTo(mask, cv.MatType.CV_8U)
            dst0 = mask
            mask = mask.InRange(histIndex, histIndex)

            dst3 = algTask.color.Clone
            dst3.SetTo(white, mask)
            dst3.SetTo(0, algTask.noDepthMask)
            dst2.Rectangle(New cv.Rect(CInt(histIndex * brickWidth), 0, brickWidth, dst2.Height), cv.Scalar.Yellow, algTask.lineWidth)
            Dim deriv = Format(options.derivativeRange, fmt2)
            labels(2) = "Histogram of first or second derivatives.  Range -" + deriv + " to " + deriv
            labels(3) = "Backprojection into the image for the selected histogram entry - move mouse over dst2."
        End Sub
    End Class






    Public Class Derivative_Sobel1 : Inherits TaskParent
        Dim deriv As New Derivative_Sobel
        Public Sub New()
            If standalone Then algTask.gOptions.displaydst1.checked = True
            If standalone Then algTask.gOptions.displaydst1.checked = True
            desc = "Display the derivative of the selected depth dimension."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim channel = deriv.options.channel
            Dim chanName As String = "X"
            If channel <> 0 Then
                If channel = 1 Then chanName = "Y" Else chanName = "Z"
            End If
            Dim kern = deriv.options.kernelSize
            src = algTask.pcSplit(channel).Sobel(cv.MatType.CV_32F, 1, 0, kern)
            deriv.Run(src)
            dst0 = deriv.dst2.Clone
            dst1 = deriv.dst3.Clone
            labels(0) = "Horizontal derivatives for " + chanName + " dimension of the point cloud"
            labels(1) = "Backprojection of horizontal derivatives indicated - move mouse in the image at left"

            src = algTask.pcSplit(channel).Sobel(cv.MatType.CV_32F, 0, 1, kern)
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim channel = deriv.options.channel
            Dim gausskern = New cv.Size(CInt(options.gaussiankernelSize), CInt(options.gaussiankernelSize))
            dst1 = algTask.pcSplit(channel).GaussianBlur(gausskern, 0, 0)
            dst1 = dst1.Laplacian(cv.MatType.CV_32F, options.LaplaciankernelSize, 1, 0)

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
        Private Function derivClassCount(ByRef dst As cv.Mat) As Integer
            For i = 0 To deriv.plotHist.histArray.Count - 1
                If deriv.plotHist.histArray(i) > 0 Then derivClassCount += 1
            Next
            dst = PaletteFull(deriv.dst0)
            dst.SetTo(0, algTask.noDepthMask)
            Return derivClassCount
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            deriv.Run(algTask.pcSplit(deriv.options.channel).Sobel(cv.MatType.CV_32F, 1, 0, deriv.options.kernelSize))
            classCountX = derivClassCount(dst2)
            labels(2) = $"Backprojection of X dimension of algTask.pcSplit({deriv.options.channel})"

            deriv.Run(algTask.pcSplit(deriv.options.channel).Sobel(cv.MatType.CV_32F, 0, 1, deriv.options.kernelSize))
            classCountY = derivClassCount(dst3)
            labels(3) = $"Backprojection of Y dimension of algTask.pcSplit({deriv.options.channel})"
        End Sub
    End Class
End Namespace