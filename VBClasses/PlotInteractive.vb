Imports cv = OpenCvSharp
Imports VBClasses
Public Class PlotInteractive_Basics : Inherits TaskParent
    Public plotHist As New PlotBars_Basics
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
        Dim colWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / colWidth)
        dst0 = dst1.InRange(histIndex, histIndex)
        If ranges(histIndex) IsNot Nothing Then
            labels(2) = "For bin " + CStr(histIndex) + " " + Format(ranges(histIndex).Average, fmt1) +
                    " average range and min/max " + Format(ranges(histIndex).Min, fmt1) + "/" +
                    Format(ranges(histIndex).Max, fmt1)
        End If

        Dim actualCount = dst0.CountNonZero
        dst3 = task.color.Clone
        dst3.SetTo(cv.Scalar.Yellow, dst0)
        dst2.Rectangle(New cv.Rect(CInt(histIndex) * colWidth, 0, colWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class




Public Class PlotInteractive_Correlation : Inherits TaskParent
    Public plotHist As New PlotBars_Basics
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
        Dim colWidth = dst2.Width / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / colWidth)
        dst0 = dst1.InRange(histIndex, histIndex)
        If ranges(histIndex) IsNot Nothing Then
            labels(2) = "For bin " + CStr(histIndex) + " " + Format(ranges(histIndex).Average, fmt1) +
                    " average range and min/max " + Format(ranges(histIndex).Min, fmt1) + "/" +
                    Format(ranges(histIndex).Max, fmt1)
        End If

        Dim actualCount = dst0.CountNonZero
        dst3 = task.color.Clone
        dst3.SetTo(cv.Scalar.Yellow, dst0)
        dst2.Rectangle(New cv.Rect(CInt(histIndex) * colWidth, 0, colWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
    End Sub
End Class