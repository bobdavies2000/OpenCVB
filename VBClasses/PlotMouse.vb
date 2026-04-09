Imports SharpGL.SceneGraph.Raytracing
Imports cv = OpenCvSharp
Public Class PlotMouse_Basics : Inherits TaskParent
    Public plotHist As New PlotBar_Basics
    Public histogram As New cv.Mat
    Public mask As New cv.Mat
    Public Sub New()
        plotHist.removeZeroEntry = False
        plotHist.createHistogram = True
        task.gOptions.HistBinBar.Value = task.gOptions.HistBinBar.Maximum
        desc = "Show CV_8U data as a histogram and use mouse movement to control the backprojection."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.color.Clone

        If src.Channels <> 1 Then src = task.gray

        plotHist.histogram = histogram
        plotHist.Run(src)
        dst2 = plotHist.dst2

        Dim barWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / barWidth)

        Dim minRange = (plotHist.ranges(0).End - plotHist.ranges(0).Start) * histIndex / task.histogramBins
        Dim maxRange = (plotHist.ranges(0).End - plotHist.ranges(0).Start) * (histIndex + 1) / task.histogramBins
        Dim bpRanges = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
        cv.Cv2.CalcBackProject({src}, {0}, histogram, mask, bpRanges)
        mask.ConvertTo(mask, cv.MatType.CV_8U)

        If mask.Size = dst3.Size Then dst3.SetTo(task.highlight, mask)
        labels(3) = "BackProjected pixel (% of image) = " + Format(mask.CountNonZero / src.Total, "0%")

        labels(2) = "Histogram Depth to " + Format(task.MaxZmeters, "0.0") + " m"
        dst2.Rectangle(New cv.Rect(CInt(histIndex) * barWidth, 0, barWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class




Public Class PlotMouse_Basics32F : Inherits TaskParent
    Public plotHist As New PlotBar_Basics
    Public histogram As New cv.Mat
    Dim ranges() As cv.Rangef
    Public mask As New cv.Mat
    Public Sub New()
        plotHist.minRange = -0.01
        plotHist.maxRange = task.MaxZmeters
        plotHist.removeZeroEntry = False
        task.gOptions.MaxDepthBar.Value = 10
        desc = "Show depth32F data as a histogram and allow mouse movement to control backprojection."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.color.Clone

        If src.Channels <> 1 Then src = task.pcSplit(2)

        ranges = {New cv.Rangef(0, task.MaxZmeters)}
        cv.Cv2.CalcHist({src}, {0}, New cv.Mat, histogram, 1, {task.histogramBins}, ranges)

        plotHist.histogram = histogram
        plotHist.Run(plotHist.histogram)
        dst2 = plotHist.dst2

        Dim stepsize = dst2.Width / task.MaxZmeters
        For i = 1 To CInt(task.MaxZmeters) - 1
            dst2.Line(New cv.Point(stepsize * i, 0), New cv.Point(stepsize * i, dst2.Height), white, task.cvFontThickness)
        Next

        Dim barWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / barWidth)

        Dim minRange = (ranges(0).End - ranges(0).Start) * histIndex / task.histogramBins
        Dim maxRange = (ranges(0).End - ranges(0).Start) * (histIndex + 1) / task.histogramBins
        Dim bpRanges = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
        cv.Cv2.CalcBackProject({src}, {0}, histogram, mask, bpRanges)
        mask.ConvertTo(mask, cv.MatType.CV_8U)

        Dim maskCount = mask.CountNonZero
        If mask.Size = dst3.Size Then dst3.SetTo(task.highlight, mask)
        labels(3) = "BackProjected pixel (% of image) = " + Format(maskCount / src.Total, "0%")

        labels(2) = "Histogram Depth to " + Format(task.MaxZmeters, "0.0") + " m"
        dst2.Rectangle(New cv.Rect(CInt(histIndex) * barWidth, 0, barWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class




Public Class PlotMouse_Correlation : Inherits TaskParent
    Public plotHist As New PlotBar_Basics
    Dim corr As New Correlation_BasicsPlot
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        plotHist.minRange = 0
        plotHist.maxRange = 2
        plotHist.createHistogram = False
        plotHist.shadeValues = False
        labels(2) = "Move mouse to identify grid squares in the image."
        desc = "Mouse over any bin to see the grid squares in the selected range."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        corr.Run(src)

        Dim histogram(task.histogramBins - 1) As Single
        Dim ranges(task.histogramBins - 1) As List(Of Single)
        Dim incr = 2 / task.histogramBins
        dst1.SetTo(0)
        For i = 0 To corr.cList.Count - 1
            Dim bin = CInt(corr.cList(i) / incr) - 1
            If bin > 0 Then
                Dim r = task.gridRects(i)
                dst1(r).SetTo(bin)
                histogram(bin) += 1
                If ranges(bin) Is Nothing Then ranges(bin) = New List(Of Single)
                ranges(bin).Add(corr.mmRanges(i))
            End If
        Next

        Dim histInput As cv.Mat = cv.Mat.FromPixelData(histogram.Count, 1, cv.MatType.CV_32F, histogram)
        plotHist.Run(histInput)
        dst2 = plotHist.dst2
        labels(3) = plotHist.labels(2)

        Dim totalPixels = dst2.Total ' assume we are including zeros.
        Dim barWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / barWidth)
        dst0 = dst1.InRange(histIndex, histIndex)
        If ranges(histIndex) IsNot Nothing Then
            labels(2) = "For bin " + CStr(histIndex) + " " + Format(ranges(histIndex).Average, fmt1) +
                    " average range and min/max " + Format(ranges(histIndex).Min, fmt1) + "/" +
                    Format(ranges(histIndex).Max, fmt1)
        End If

        Dim actualCount = dst0.CountNonZero
        dst3 = task.color.Clone
        dst3.SetTo(cv.Scalar.Yellow, dst0)
        dst2.Rectangle(New cv.Rect(CInt(histIndex) * barWidth, 0, barWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class






Public Class PlotMouse_SobelDerivative : Inherits TaskParent
    Dim deriv As New Derivative_Sobel
    Public Sub New()
        desc = "Plot of derivative in depth in X, Y, or Z."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        deriv.Run(task.pcSplit(2))
        dst2 = deriv.dst2
        dst3 = deriv.dst3
        labels = deriv.labels
    End Sub
End Class






Public Class PlotMouse_StableGray : Inherits TaskParent
    Dim plot As New PlotMouse_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        desc = "Plot the stable grayscale image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.gOptions.stabilizeDepthRGB.Checked Then
            dst0 = task.stabilizeGray.dst2
            labels(0) = task.stabilizeGray.labels(2)
        Else
            dst0 = task.gray
            labels(0) = "Original task.gray - unchanged."
        End If

        plot.Run(dst0)
        dst2 = plot.dst2
        labels(2) = plot.labels(2)

        dst3 = plot.dst3
        labels(3) = plot.labels(3)
    End Sub
End Class






Public Class PlotMouse_MaskBackProject : Inherits TaskParent
    Public hist As New Histogram_Basics
    Public histIndex As Integer
    Public Sub New()
        hist.plotHist.removeZeroEntry = False
        labels(2) = "Histogram for the gray scale image.  Move mouse to see backprojection of each grayscale mask."
        desc = "Create all the backprojection masks from a grayscale histogram"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hist.Run(task.gray)
        dst2 = hist.dst2

        Dim barWidth = dst2.Width / task.histogramBins
        histIndex = Math.Floor(task.mouseMovePoint.X / barWidth)

        dst3 = task.color.Clone
        Dim barRange = 255 / hist.histogram.Rows

        Dim minRange = If(histIndex = hist.histogram.Rows - 1, 255 - barRange, histIndex * barRange)
        Dim maxRange = If(histIndex = hist.histogram.Rows - 1, 255, (histIndex + 1) * barRange)
        If Single.IsNaN(minRange) Or Single.IsInfinity(minRange) Or
           Single.IsNaN(maxRange) Or Single.IsInfinity(maxRange) Then
            SetTrueText("Input data has no values - exit " + traceName)
            Exit Sub
        End If

        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        cv.Cv2.CalcBackProject({task.gray}, {0}, hist.histogram, dst1, ranges)
        dst3.SetTo(task.highlight, dst1)
        dst2.Rectangle(New cv.Rect(CInt(histIndex * barWidth), 0, barWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class