﻿Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class OpAuto_XRange : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public Sub New()
        labels(2) = "Optimized top view to show as many samples as possible."
        desc = "Automatically adjust the X-Range option of the pointcloud to maximize visible pixels"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static adjustedCount As Integer = 0
        Dim expectedCount = task.depthMask.CountNonZero

        Dim diff = Math.Abs(expectedCount - adjustedCount)

        ' the input is a histogram.  If standalone, go get one...
        If standalone Then
            cv.Cv2.CalcHist({task.pointCloud}, task.channelsTop, New cv.Mat, histogram, 2, task.bins2D, task.rangesTop)
            histogram.Row(0).SetTo(0)
            dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            dst3 = histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
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
            Static xRangeSlider = findSlider("X-Range X100")
            Dim leftGap = histogram.Col(0).CountNonZero
            Dim rightGap = histogram.Col(histogram.Width - 1).CountNonZero
            If leftGap = 0 And rightGap = 0 And xRangeSlider.value > 1 Then
                xRangeSlider.value -= 1
            Else
                If adjustedCount < expectedCount Then xRangeSlider.value += 1 Else xRangeSlider.value -= 1
            End If
            task.optionsChanged = saveOptionState
        End If

        setTrueText(strOut, 3)
    End Sub
End Class





Public Class OpAuto_YRange : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public Sub New()
        labels(2) = "Optimized side view to show as much as possible."
        desc = "Automatically adjust the Y-Range option of the pointcloud to maximize visible pixels"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static adjustedCount As Integer = 0
        Dim expectedCount = task.depthMask.CountNonZero

        Dim diff = Math.Abs(expectedCount - adjustedCount)

        ' the input is a histogram.  If standalone, go get one...
        If standalone Then
            cv.Cv2.CalcHist({task.pointCloud}, task.channelsSide, New cv.Mat, histogram, 2, task.bins2D, task.rangesSide)
            histogram.Col(0).SetTo(0)
            dst2 = histogram.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
            dst3 = histogram.Threshold(task.redThresholdSide, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs
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
            Static yRangeSlider = findSlider("Y-Range X100")
            Dim topGap = histogram.Row(0).CountNonZero
            Dim botGap = histogram.Row(histogram.Height - 1).CountNonZero
            If topGap = 0 And botGap = 0 And yRangeSlider.value > 1 Then
                yRangeSlider.value -= 1
            Else
                If adjustedCount < expectedCount Then yRangeSlider.value += 1 Else yRangeSlider.value -= 1
            End If
            task.optionsChanged = saveOptionState
        End If
        setTrueText(strOut, 3)
    End Sub
End Class






Public Class OpAuto_FloorCeiling : Inherits VB_Algorithm
    Public bpLine As New BackProject_LineSide
    Public yList As New List(Of Single)
    Public floorY As Single
    Public ceilingY As Single
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Automatically find the Y values that best describes the floor and ceiling (if present)"
    End Sub
    Private Sub rebuildMask(maskLabel As String, min As Single, max As Single)
        Dim mask = task.pcSplit(1).InRange(min, max).ConvertScaleAbs

        Dim mean As cv.Scalar, stdev As cv.Scalar
        cv.Cv2.MeanStdDev(task.pointCloud, mean, stdev, mask)

        strOut += "The " + maskLabel + " mask has Y mean and stdev are:" + vbCrLf
        strOut += maskLabel + " Y Mean = " + Format(mean(1), fmt3) + vbCrLf
        strOut += maskLabel + " Y Stdev = " + Format(stdev(1), fmt3) + vbCrLf + vbCrLf

        If Math.Abs(mean(1)) > task.yRange / 4 Then dst1 = mask Or dst1
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim pad As Single = 0.05 ' pad the estimate by X cm's

        dst2 = src.Clone
        bpLine.Run(src)

        If bpLine.mpList.Count > 0 Then
            strOut = "Y range = " + Format(task.yRange, fmt3) + vbCrLf + vbCrLf
            If heartBeat() Then yList.Clear()
            If heartBeat() Then dst1.SetTo(0)
            Dim h = dst2.Height / 2
            For Each mp In bpLine.mpList
                Dim nextY = task.yRange * (mp.p1.Y - h) / h
                If Math.Abs(nextY) > task.yRange / 4 Then yList.Add(nextY)
            Next

            If yList.Count > 0 Then
                If yList.Max > 0 Then rebuildMask("floor", yList.Max - pad, task.yRange)
                If yList.Min < 0 Then rebuildMask("ceiling", -task.yRange, yList.Min + pad)
            End If

            dst2.SetTo(cv.Scalar.White, dst1)
        End If
        setTrueText(strOut, 3)
    End Sub
End Class







Public Class OpAuto_Valley : Inherits VB_Algorithm
    Public valleyOrder As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public options As New Options_Boundary
    Public Sub New()
        If standalone Then gOptions.HistBinSlider.Value = 256
        desc = "Get the top X highest quality valley points in the histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim desiredBoundaries = options.desiredBoundaries

        ' input should be a histogram.  If not, get one...
        If standalone Then
            Static kalmanHist As New Histogram_Kalman
            kalmanHist.Run(src)
            dst2 = kalmanHist.dst2
            src = kalmanHist.hist.histogram.Clone
        End If

        Dim samples(src.Total - 1) As Single
        Marshal.Copy(src.Data, samples, 0, samples.Length)

        Dim histList = samples.ToList

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
            valleyOrder.Add(lastEntry, valleys(i) - 1)
            lastEntry = valleys(i)
        Next
        If valleys(desiredBoundaries - 1) <> histList.Count - 1 Then
            valleyOrder.Add(valleys(desiredBoundaries - 1), 256)
        End If

        If standalone Then
            For Each entry In valleyOrder
                Dim col = entry.Value * dst2.Width / task.histogramBins
                dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.White, task.lineWidth)
            Next
            setTrueText(CStr(valleys.Count) + " valleys in histogram", 3)
        End If
    End Sub
End Class





Public Class OpAuto_Peaks2D : Inherits VB_Algorithm
    Public options As New Options_Boundary
    Public clusterPoints As New List(Of cv.Point2f)
    Public Sub New()
        If standalone Then gOptions.HistBinSlider.Value = 256
        labels = {"", "", "2D Histogram view with highlighted peaks", ""}
        desc = "Find the peaks in a 2D histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim desiredBoundaries = options.desiredBoundaries
        Dim peakDistance = options.peakDistance

        ' input should be a 2D histogram.  If standalone, get one...
        If standalone Then
            Static heatmap As New HeatMap_Basics
            If firstPass Then findCheckBox("Show Frustrum").Checked = False
            heatmap.Run(src)
            dst2 = If(task.toggleEverySecond, heatmap.dst2, heatmap.dst3)
            src = If(task.toggleEverySecond, heatmap.dst0.Clone, heatmap.dst1.Clone)
        End If

        clusterPoints.Clear()
        clusterPoints.Add(New cv.Point(0, 0))
        For i = 0 To desiredBoundaries - 1
            Dim mm = vbMinMax(src)
            If clusterPoints.Contains(mm.maxLoc) = False Then clusterPoints.Add(mm.maxLoc)
            src.Circle(mm.maxLoc, peakDistance, 0, -1, task.lineType)
        Next

        If Not standalone Then dst2.SetTo(0)
        For i = 0 To clusterPoints.Count - 1
            Dim pt = clusterPoints(i)
            dst2.Circle(pt, task.dotSize * 3, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class





Public Class OpAuto_Peaks2DGrid : Inherits VB_Algorithm
    Public clusterPoints As New List(Of cv.Point2f)
    Dim options As New Options_Boundary
    Public Sub New()
        If standalone Then gOptions.HistBinSlider.Value = 256
        labels = {"", "", "2D Histogram view with highlighted peaks", ""}
        desc = "Find the peaks in a 2D histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static boundarySlider = findSlider("Desired boundary count")
        Dim desiredBoundaries = boundarySlider.value

        ' input should be a 2D histogram.  If standalone or src is not a histogram, get one...
        If standalone Or src.Type = cv.MatType.CV_8UC3 Then
            Static hist2d As New Histogram2D_Basics
            hist2d.Run(src)
            src = hist2d.histogram
            dst2.SetTo(0)
        End If

        Dim pointPop As New SortedList(Of Single, cv.Point)(New compareAllowIdenticalSingleInverted)
        For Each roi In task.gridList
            Dim mm = vbMinMax(src(roi))
            If mm.maxVal = 0 Then Continue For
            pointPop.Add(mm.maxVal, New cv.Point(roi.X + mm.maxLoc.X, roi.Y + mm.maxLoc.Y))
        Next

        clusterPoints.Clear()
        clusterPoints.Add(New cv.Point(0, 0))
        For Each entry In pointPop
            clusterPoints.Add(entry.Value)
            If desiredBoundaries <= clusterPoints.Count Then Exit For
        Next

        If Not standalone Then dst2.SetTo(0)
        For i = 0 To clusterPoints.Count - 1
            Dim pt = clusterPoints(i)
            dst2.Circle(pt, task.dotSize * 3, cv.Scalar.White, -1, task.lineType)
        Next

        dst2.SetTo(cv.Scalar.White, task.gridMask)
        labels(3) = CStr(pointPop.Count) + " grid samples trimmed to " + CStr(clusterPoints.Count)
    End Sub
End Class











Public Class OpAuto_PixelDifference : Inherits VB_Algorithm
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 2 ' set it low so it will move up to the right value.
        labels = {"", "", "2D Histogram view with highlighted peaks", ""}
        desc = "Find the peaks in a 2D histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() = False And task.frameCount > 10 Then Exit Sub
        If standalone Then
            Static diff As New Diff_Basics
            diff.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            src = diff.dst3
        End If

        Dim gridCount As Integer
        For Each roi In task.gridList
            If src(roi).CountNonZero Then gridCount += 1
        Next

        If gOptions.PixelDiffThreshold.Value < gOptions.PixelDiffThreshold.Maximum Then
            If gridCount > task.gridList.Count / 10 Then gOptions.PixelDiffThreshold.Value += 1
        End If
        If gridCount = 0 And gOptions.PixelDiffThreshold.Value > 1 Then gOptions.PixelDiffThreshold.Value -= 1
        setTrueText("Pixel difference threshold is at " + CStr(gOptions.PixelDiffThreshold.Value))
        dst2 = src
    End Sub
End Class







Public Class OpAuto_MSER : Inherits VB_Algorithm
    Dim mBase As New MSER_Basics
    Public classCount As Integer
    Public Sub New()
        If standalone Then mBase.useOpAuto = False
        desc = "Option Automation: find the best MSER max and min area values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static minSlider = findSlider("MSER Min Area")
        Static maxSlider = findSlider("MSER Max Area")
        If standalone Then
            mBase.Run(src)
            src = mBase.dst3
            classCount = mBase.mserCells.Count
        End If
        dst2 = src.Clone

        Static checkOften As Boolean = True
        If heartBeat() Or checkOften Then
            If src.Channels <> 1 Then dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst1 = src
            Dim count = dst1.CountNonZero
            Dim desired = CInt(dst2.Total * 0.6)
            If count < desired Then
                If maxSlider.value < maxSlider.maximum - 1000 Then maxSlider.value += 1000
            End If

            If classCount > 35 Then
                If minSlider.value < minSlider.maximum - 100 Then minSlider.value += 100
            Else
                If classCount > 0 Then checkOften = False
                If classCount < 25 Then
                    If minSlider.value > 100 Then minSlider.value -= 100
                End If
            End If

            strOut = "NonZero pixel count = " + CStr(count) + vbCrLf + "Desired pixel count (60% of total) = " + CStr(desired) + vbCrLf
            strOut += "maxSlider value = " + CStr(maxSlider.value) + vbCrLf
            strOut += "Cells identified = " + CStr(classCount) + vbCrLf
            strOut += "minSlider value = " + CStr(minSlider.value) + vbCrLf
            strOut += "checkOften variable is " + CStr(checkOften)
        End If
        setTrueText(strOut, 3)
    End Sub
End Class








Public Class OpAuto_GuidedBP : Inherits VB_Algorithm
    Public nonZeroSamples As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Upper limit of regions", 20, 500, 80)
            sliders.setupTrackBar("Lower limit of regions", 10, 500, 50)
        End If

        desc = "This algorihm is intended to control how many cells RedCloud will find with a 2D backprojection"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static upperSlider = findSlider("Upper limit of regions")
        Static lowerSlider = findSlider("Lower limit of regions")
        If lowerSlider.value > upperSlider.value - 10 Then lowerSlider.value = upperSlider.value - 10

        If nonZeroSamples = 0 Then Exit Sub

        ' A practical use of optionAutomation.  Any image with more regions is quite complex.
        Dim saveit = task.optionsChanged
        If nonZeroSamples > upperSlider.value Then redOptions.HistBinSlider.Value -= 1
        If nonZeroSamples < lowerSlider.value Then redOptions.HistBinSlider.Value += 1
        task.optionsChanged = saveit
    End Sub
End Class