Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class HistValley_Basics : Inherits VB_Algorithm
    Public peak As New HistValley_Peaks
    Public peaks As New List(Of Integer)
    Public valleys As New List(Of Integer)
    Public histList As New List(Of Single)
    Public Sub New()
        findSlider("Desired boundary count").Value = 5
        desc = "Use the peaks identified in HistValley_Peaks to find the valleys between the peaks."
    End Sub
    Public Function updatePlot(dst As cv.Mat, bins As Integer) As cv.Mat
        For Each valley In valleys
            Dim col = dst.Width * valley / bins
            dst.Line(New cv.Point(col, dst.Height), New cv.Point(col, dst.Height * 9 / 10), cv.Scalar.White, task.lineWidth)
        Next
        Return dst
    End Function
    Public Sub RunVB(src As cv.Mat)
        peak.Run(src)
        dst2 = peak.hist.dst2
        labels(2) = peak.labels(2) + " and " + CStr(valleys.Count) + " valleys (marked at bottom)"
        histList = peak.histArray.ToList
        valleys.Clear()
        peaks = New List(Of Integer)(peak.peaks)
        For i = 0 To peaks.Count - 2
            Dim start = peaks(i)
            Dim finish = peaks(i + 1)

            Dim testList As New List(Of Single)
            For j = start To finish
                testList.Add(histList(j))
            Next

            Dim nextVal = start + testList.IndexOf(testList.Min)
            valleys.Add(nextVal)
        Next
        If standalone Then
            updatePlot(dst2, task.histogramBins)
            setTrueText("Input data used by default is the color image", 3)
        End If
    End Sub
End Class







Public Class HistValley_Depth : Inherits VB_Algorithm
    Public valley As New HistValley_Basics
    Public valleyOrder As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public Sub New()
        labels = {"", "", "Top markerstop = peaks, bottom markers = valleys", "Guided backprojection of kalmanized depth valleys"}
        desc = "Find the valleys in the depth histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static histogram As cv.Mat
        If heartBeat() Then
            valley.Run(src)
            dst2 = valley.dst2

            Dim vList = New List(Of Integer)(valley.valleys)

            Dim histArray(valley.histList.Count - 1) As Single
            For i = 0 To vList.Count - 2
                Dim start = vList(i)
                Dim finish = vList(i + 1)
                For j = start To finish
                    histArray(j) = i + 1
                Next
            Next

            histogram = valley.peak.hist.histogram

            Marshal.Copy(histArray, 0, histogram.Data, histArray.Length)
            histogram += 1 ' shift away from 0
        End If
        cv.Cv2.CalcBackProject({src}, {0}, histogram, dst1, valley.peak.hist.ranges)
        If dst1.Type <> cv.MatType.CV_8U Then
            dst1.SetTo(0, task.noDepthMask)
            dst1.ConvertTo(dst1, cv.MatType.CV_8U)
            dst3 = vbPalette(dst1 * 255 / valley.valleys.Count)
        End If
        If standalone Then valley.updatePlot(dst2, task.histogramBins)
    End Sub
End Class






Public Class HistValley_DepthOld : Inherits VB_Algorithm
    Public valley As New HistValley_BasicsOptionAuto
    Public valleyOrder As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public Sub New()
        desc = "Find the valleys in the depth histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        valley.Run(src)
        dst1 = valley.dst1
        dst2 = valley.dst2
        dst3 = valley.dst3
        valleyOrder = valley.auto.valleyOrder
    End Sub
End Class







Public Class HistValley_Test : Inherits VB_Algorithm
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







Public Class HistValley_BasicsOptionAuto : Inherits VB_Algorithm
    Dim kalman As New Histogram_Kalman
    Public histogram As New cv.Mat
    Public auto As New OpAuto_Valley
    Public Sub New()
        gOptions.HistBinSlider.Value = 256
        labels = {"", "", "Grayscale histogram - white lines are valleys", ""}
        desc = "Isolate the different levels of gray using the histogram valleys."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() Then
            kalman.Run(src)
            dst2 = kalman.dst2
            histogram = kalman.hist.histogram.Clone
            auto.Run(histogram)

            If auto.valleyOrder.Count = 0 Then Exit Sub

            For i = 0 To auto.valleyOrder.Count - 1
                Dim entry = auto.valleyOrder.ElementAt(i)
                Dim cClass = CSng(CInt(255 / (i + 1)))
                Dim index = If(i Mod 2, cClass, 255 - cClass)
                For j = entry.Key To entry.Value
                    histogram.Set(Of Single)(j, 0, index)
                Next
                Dim col = dst2.Width * entry.Value / task.histogramBins
                dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.White, task.lineWidth)
            Next
        End If

        If src.Type = cv.MatType.CV_32F Then histogram += 1

        cv.Cv2.CalcBackProject({src}, {0}, histogram, dst1, kalman.hist.ranges)
        If dst1.Type <> cv.MatType.CV_8U Then
            dst1.SetTo(0, task.noDepthMask)
            dst1.ConvertTo(dst1, cv.MatType.CV_8U)
        End If

        dst3 = vbPalette(dst1)
        labels(3) = CStr(auto.valleyOrder.Count + 1) + " colors in the back projection"
    End Sub
End Class







Public Class HistValley_Diff : Inherits VB_Algorithm
    Dim diff As New Diff_Basics
    Dim valley As New HistValley_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Compare frame to frame what has changed"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        valley.Run(src)
        dst2 = valley.dst2

        diff.Run(valley.dst2)
        dst3 = diff.dst2
        dst1 = diff.dst3
    End Sub
End Class









Public Class HistValley_EdgeDraw : Inherits VB_Algorithm
    Dim valley As New HistValley_Basics
    Dim edges As New EdgeDraw_Basics
    Public Sub New()
        desc = "Remove edge color in RGB before HistValley_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)

        dst3 = src
        dst3.SetTo(cv.Scalar.Black, edges.dst2)

        valley.Run(dst3)
        dst2 = valley.dst2
    End Sub
End Class






Public Class HistValley_Colors : Inherits VB_Algorithm
    Dim hist As New Histogram_Kalman
    Dim auto As New OpAuto_Valley
    Public Sub New()
        If standalone Then gOptions.HistBinSlider.Value = 256
        If standalone Then findSlider("Desired boundary count").Value = 10
        desc = "Find the histogram valleys for each of the colors."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static splitIndex As Integer
        If heartBeat() Then splitIndex = (splitIndex + 1) Mod 3
        src = src.ExtractChannel(splitIndex)
        hist.hist.plot.backColor = Choose(splitIndex + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red)

        hist.Run(src)
        dst2 = hist.dst2

        auto.Run(hist.hist.histogram)

        For i = 0 To auto.valleyOrder.Count - 1
            Dim entry = auto.valleyOrder.ElementAt(i)
            Dim cClass = CSng(CInt(255 / (i + 1)))
            Dim index = If(i Mod 2, cClass, 255 - cClass)
            For j = entry.Key To entry.Value
                hist.hist.histogram.Set(Of Single)(j, 0, index)
            Next
            Dim col = dst2.Width * entry.Value / task.histogramBins
            dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.White, task.lineWidth)
        Next
    End Sub
End Class







Public Class HistValley_Simple : Inherits VB_Algorithm
    Dim trends As New SLR_Trends
    Public kalman As New Kalman_Basics
    Public depthRegions As New List(Of Integer)
    Public plot As New Plot_Histogram
    Public Sub New()
        desc = "Identify ranges by marking the depth histogram entries from valley to valley"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        trends.Run(src)

        If kalman.kInput.Length <> task.histogramBins Then ReDim kalman.kInput(task.histogramBins - 1)
        kalman.kInput = trends.resultingValues.ToArray
        kalman.Run(src)

        dst2.SetTo(cv.Scalar.Black)
        Dim barWidth As Single = dst2.Width / trends.resultingValues.Count
        Dim colorIndex As Integer
        Dim color = task.scalarColors(colorIndex Mod 256)
        Dim vals() = {-1, -1, -1}
        For i = 0 To kalman.kOutput.Count - 1
            Dim h = dst2.Height - kalman.kOutput(i)
            vals(0) = vals(1)
            vals(1) = vals(2)
            vals(2) = h
            If vals(0) >= 0 Then
                If vals(0) > vals(1) And vals(2) > vals(1) Then
                    colorIndex += 1
                    color = task.scalarColors(colorIndex Mod 256)
                End If
            End If
            cv.Cv2.Rectangle(dst2, New cv.Rect(i * barWidth, dst2.Height - h, barWidth, h), color, -1)
            depthRegions.Add(colorIndex)
        Next

        Dim lastPoint As cv.Point = trends.resultingPoints(0)
        For i = 1 To trends.resultingPoints.Count - 1
            Dim p1 = trends.resultingPoints(i)
            dst2.Line(lastPoint, p1, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
            lastPoint = p1
        Next
        labels(2) = "Depth regions between 0 and " + CStr(CInt(task.maxZmeters + 1)) + " meters"
    End Sub
End Class






Public Class HistValley_Peaks : Inherits VB_Algorithm
    Public hist As New Histogram_Basics
    Public options As New Options_Boundary
    Public peaks As New List(Of Integer)
    Public histArray() As Single
    Public Sub New()
        gOptions.HistBinSlider.Value = 100
        findSlider("Desired boundary count").Value = 5
        labels(2) = "Histogram - white lines are peaks"
        desc = "Find the requested number of peaks in the histogram "
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim desiredBoundaries = options.desiredBoundaries

        If standalone Then
            src = task.pcSplit(2)
            setTrueText("Default input when running standalone is the depth data.", 3)
        End If
        If src.Type <> cv.MatType.CV_32FC1 Or standalone Then
            src = task.pcSplit(2)
            hist.Run(src)
            dst2 = hist.dst2
            ReDim histArray(hist.histogram.Rows - 1)
            Marshal.Copy(hist.histogram.Data, histArray, 0, histArray.Length)
        Else
            ReDim histArray(src.Rows - 1)
            Marshal.Copy(src.Data, histArray, 0, histArray.Length)
        End If

        Dim histList = histArray.ToList

        Dim sortPeaks As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        For i = 0 To histlist.Count - 1
            If histlist(i) <> 0 Then
                sortPeaks.Add(i, i)
                Exit For
            End If
        Next
        For i = histlist.Count - 1 To 0 Step -1
            If histlist(i) <> 0 Then
                sortPeaks.Add(i, i)
                Exit For
            End If
        Next
        For i = 0 To desiredBoundaries - 1
            Dim index = histlist.IndexOf(histlist.Max)
            Dim lastCount = histlist(index)
            sortPeaks.Add(index, index)
            For j = index - 1 To 0 Step -1
                Dim count = histlist(j)
                If lastCount > count Then histlist(j) = 0 Else Exit For
                lastCount = count
            Next

            lastCount = histlist(index)
            histlist(index) = 0
            For j = index + 1 To histlist.Count - 1
                Dim count = histlist(j)
                If lastCount > count Then histlist(j) = 0 Else Exit For
                lastCount = count
            Next
        Next

        Dim mm as mmData = vbMinMax(src)
        Dim incr = (mm.maxVal - mm.minVal) / task.histogramBins
        peaks.clear()
        For Each el In sortPeaks
            Dim index = el.Key
            Dim col = dst2.Width * index / task.histogramBins
            peaks.Add(index)
            dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height / 10), cv.Scalar.White, task.lineWidth)
        Next
        labels(2) = CStr(peaks.Count - 2) + " peaks (marked at top) were found in the histogram"
    End Sub
End Class

