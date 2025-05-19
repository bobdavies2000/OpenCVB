Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class OpAuto_XRange : Inherits TaskParent
    Public histogram As New cv.Mat
    Dim adjustedCount As Integer = 0
    Public Sub New()
        labels(2) = "Optimized top view to show as many samples as possible."
        desc = "Automatically adjust the X-Range option of the pointcloud to maximize visible pixels"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim expectedCount = task.depthMask.CountNonZero

        Dim diff = Math.Abs(expectedCount - adjustedCount)

        ' the input is a histogram.  If standaloneTest(), go get one...
        If standaloneTest() Then
            cv.Cv2.CalcHist({task.pointCloud}, task.channelsTop, New cv.Mat, histogram, 2, task.bins2D, task.rangesTop)
            histogram.Row(0).SetTo(0)
            dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            dst3 = histogram.Threshold(task.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            src = histogram
        End If

        histogram = src
        adjustedCount = histogram.Sum()(0)

        strOut = "Adjusted = " + vbTab + CStr(adjustedCount) + "k" + vbCrLf +
                 "Expected = " + vbTab + CStr(expectedCount) + "k" + vbCrLf +
                 "Diff = " + vbTab + vbTab + CStr(diff) + vbCrLf +
                 "xRange = " + vbTab + Format(task.xRange, fmt3)

        If task.useXYRange Then
            Dim saveOptionState = task.optionsChanged ' the xRange and yRange change frequently.  It is safe to ignore it.
            Dim leftGap = histogram.Col(0).CountNonZero
            Dim rightGap = histogram.Col(histogram.Width - 1).CountNonZero
            If leftGap = 0 And rightGap = 0 And task.redOptions.XRangeBar.Value > 3 Then
                task.redOptions.XRangeBar.Value -= 1
            Else
                If adjustedCount < expectedCount Then task.redOptions.XRangeBar.Value += 1 Else task.redOptions.XRangeBar.Value -= 1
            End If
            task.optionsChanged = saveOptionState
        End If

        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class OpAuto_YRange : Inherits TaskParent
    Public histogram As New cv.Mat
    Dim adjustedCount As Integer = 0
    Public Sub New()
        labels(2) = "Optimized side view to show as much as possible."
        desc = "Automatically adjust the Y-Range option of the pointcloud to maximize visible pixels"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim expectedCount = task.depthMask.CountNonZero

        Dim diff = Math.Abs(expectedCount - adjustedCount)

        ' the input is a histogram.  If standaloneTest(), go get one...
        If standaloneTest() Then
            cv.Cv2.CalcHist({task.pointCloud}, task.channelsSide, New cv.Mat, histogram, 2, task.bins2D, task.rangesSide)
            histogram.Col(0).SetTo(0)
            dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            dst3 = histogram.Threshold(task.projectionThreshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            src = histogram
        End If

        histogram = src
        adjustedCount = histogram.Sum()(0)

        strOut = "Adjusted = " + vbTab + CStr(adjustedCount) + "k" + vbCrLf +
                 "Expected = " + vbTab + CStr(expectedCount) + "k" + vbCrLf +
                 "Diff = " + vbTab + vbTab + CStr(diff) + vbCrLf +
                 "yRange = " + vbTab + Format(task.yRange, fmt3)

        If task.useXYRange Then
            Dim saveOptionState = task.optionsChanged ' the xRange and yRange change frequently.  It is safe to ignore it.
            Dim topGap = histogram.Row(0).CountNonZero
            Dim botGap = histogram.Row(histogram.Height - 1).CountNonZero
            If topGap = 0 And botGap = 0 And task.redOptions.YRangeSlider.Value > 3 Then
                task.redOptions.YRangeSlider.Value -= 1
            Else
                If adjustedCount < expectedCount Then task.redOptions.YRangeSlider.Value += 1 Else task.redOptions.YRangeSlider.Value -= 1
            End If
            task.optionsChanged = saveOptionState
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class OpAuto_Valley : Inherits TaskParent
    Public valleyOrder As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public options As New Options_Boundary
    Dim kalmanHist As New Hist_Kalman
    Public Sub New()
        If standalone Then task.gOptions.setHistogramBins(256)
        desc = "Get the top X highest quality valley points in the histogram."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        Dim desiredBoundaries = options.desiredBoundaries

        ' input should be a histogram.  If not, get one...
        If standaloneTest() Then
            kalmanHist.Run(src)
            dst2 = kalmanHist.dst2
            src = kalmanHist.hist.histogram.Clone
        End If

        Dim histArray(src.Total - 1) As Single
        Marshal.Copy(src.Data, histArray, 0, histArray.Length)

        Dim histList = histArray.ToList

        Dim valleys As New List(Of Single)
        Dim incr = histList.Count / desiredBoundaries
        For i = 0 To desiredBoundaries - 1
            Dim nextList As New List(Of Single)
            For j = i * incr To (i + 1) * incr - 1
                If i = 0 And j < 5 Then
                    nextList.Add(dst2.Total) ' there are typically some gaps near zero.
                Else
                    If histList(j) = 0 Then nextList.Add(dst2.Total) Else nextList.Add(histList(j))
                End If
            Next
            Dim index = nextList.IndexOf(nextList.Min())
            valleys.Add(index + i * incr)
        Next

        valleyOrder.Clear()
        Dim lastEntry As Integer
        For i = 0 To desiredBoundaries - 1
            valleyOrder.Add(lastEntry, valleys(i))
            lastEntry = valleys(i)
        Next
        If valleys(desiredBoundaries - 1) <> histList.Count - 1 Then
            valleyOrder.Add(valleys(desiredBoundaries - 1), 256)
        End If

        If standaloneTest() Then
            For Each entry In valleyOrder
                Dim col = entry.Value * dst2.Width / task.histogramBins
                DrawLine(dst2, New cv.Point(col, 0), New cv.Point(col, dst2.Height), white)
            Next
            SetTrueText(CStr(valleys.Count) + " valleys in histogram", 3)
        End If
    End Sub
End Class





Public Class OpAuto_Peaks2D : Inherits TaskParent
    Public options As New Options_Boundary
    Public clusterPoints As New List(Of cv.Point2f)
    Dim heatmap As New HeatMap_Basics
    Public Sub New()
        If standalone Then task.gOptions.setHistogramBins(256)
        labels = {"", "", "2D Histogram view with highlighted peaks", ""}
        desc = "Find the peaks in a 2D histogram"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        Dim desiredBoundaries = options.desiredBoundaries
        Dim peakDistance = options.peakDistance

        ' input should be a 2D histogram.  If standaloneTest(), get one...
        If standaloneTest() Then
            heatmap.Run(src)
            dst2 = If(task.toggleOn, heatmap.dst2, heatmap.dst3)
            src = If(task.toggleOn, heatmap.dst0.Clone, heatmap.dst1.Clone)
        End If

        clusterPoints.Clear()
        clusterPoints.Add(New cv.Point(0, 0))
        For i = 0 To desiredBoundaries - 1
            Dim mm as mmData = GetMinMax(src)
            If clusterPoints.Contains(mm.maxLoc) = False Then clusterPoints.Add(mm.maxLoc)
            DrawCircle(src, mm.maxLoc, peakDistance, 0)
        Next

        If Not standaloneTest() Then dst2.SetTo(0)
        For i = 0 To clusterPoints.Count - 1
            Dim pt = clusterPoints(i)
            DrawCircle(dst2,pt, task.DotSize * 3, white)
        Next
    End Sub
End Class





Public Class OpAuto_Peaks2DGrid : Inherits TaskParent
    Public clusterPoints As New List(Of cv.Point2f)
    Dim options As New Options_Boundary
    Dim hist2d As New Hist2D_Basics
    Public Sub New()
        If standalone Then task.gOptions.setHistogramBins(256)
        labels = {"", "", "2D Histogram view with highlighted peaks", ""}
        desc = "Find the peaks in a 2D histogram"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Static boundarySlider =OptionParent.FindSlider("Desired boundary count")
        Dim desiredBoundaries = boundarySlider.value

        ' input should be a 2D histogram.  If standaloneTest() or src is not a histogram, get one...
        If standaloneTest() Or src.Type = cv.MatType.CV_8UC3 Then
            hist2d.Run(src)
            src = hist2d.histogram
            dst2.SetTo(0)
        End If

        Dim pointPop As New SortedList(Of Single, cv.Point)(New compareAllowIdenticalSingleInverted)
        For Each roi In task.gridRects
            Dim mm as mmData = GetMinMax(src(roi))
            If mm.maxVal = 0 Then Continue For
            pointPop.Add(mm.maxVal, New cv.Point(roi.X + mm.maxLoc.X, roi.Y + mm.maxLoc.Y))
        Next

        clusterPoints.Clear()
        clusterPoints.Add(New cv.Point(0, 0))
        For Each entry In pointPop
            clusterPoints.Add(entry.Value)
            If desiredBoundaries <= clusterPoints.Count Then Exit For
        Next

        If Not standaloneTest() Then dst2.SetTo(0)
        For i = 0 To clusterPoints.Count - 1
            Dim pt = clusterPoints(i)
            DrawCircle(dst2,pt, task.DotSize * 3, white)
        Next

        dst2.SetTo(white, task.gridMask)
        labels(3) = CStr(pointPop.Count) + " grid samples trimmed to " + CStr(clusterPoints.Count)
    End Sub
End Class











Public Class OpAuto_PixelDifference : Inherits TaskParent
    Dim diff As New Diff_Basics
    Public Sub New()
        task.gOptions.pixelDiffThreshold = 2 ' set it low so it will move up to the right value.
        labels = {"", "", "2D Histogram view with highlighted peaks", ""}
        desc = "Find the peaks in a 2D histogram"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If not task.heartBeat And task.frameCount > 10 Then Exit Sub
        If standaloneTest() Then
            diff.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            src = diff.dst2
        End If

        Dim gridCount As Integer
        For Each roi In task.gridRects
            If src(roi).CountNonZero Then gridCount += 1
        Next

        If task.gOptions.pixelDiffThreshold < task.gOptions.PixelDiffBar.Maximum Then
            If gridCount > task.gridRects.Count / 10 Then task.gOptions.pixelDiffThreshold += 1
        End If
        If gridCount = 0 And task.gOptions.pixelDiffThreshold > 1 Then task.gOptions.pixelDiffThreshold -= 1
        SetTrueText("Color difference threshold is at " + CStr(task.gOptions.pixelDiffThreshold))
        dst2 = src
    End Sub
End Class