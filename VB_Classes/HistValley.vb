Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class HistValley_Basics : Inherits VB_Algorithm
    Dim hist As New Histogram_Basics
    Dim options As New Options_Boundary
    Public valleys(3) As Integer ' grayscale values for low points in the histogram.
    Public Sub New()
        gOptions.FrameHistory.Value = 30
        gOptions.HistBinSlider.Value = 256
        labels(2) = "Histogram of the grayscale image.  White lines mark local minimum above threshold.  Yellow horizontal = histogram mean."
        desc = "Find the histogram valleys for a grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim vCount = options.desiredBoundaries
        Dim minDistance = options.peakDistance

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        hist.Run(src)
        dst2 = hist.dst2

        Static scaleList As New List(Of Single)
        Dim avg = hist.histogram.Mean()
        scaleList.Add(dst2.Height - dst2.Height * avg(0) / hist.plot.mm.maxVal)
        Dim scale = scaleList.Average()
        setTrueText("Mean", New cv.Point(5, scale), 3)
        dst2.Line(New cv.Point(0, scale), New cv.Point(dst2.Width, scale), cv.Scalar.Yellow, task.lineWidth + 1)

        If scaleList.Count > gOptions.FrameHistory.Value Then scaleList.RemoveAt(0)

        Dim hArray = hist.histArray
        Dim quartile = Math.Floor(hArray.Count / 4) ' note we really just want quartiles 
        Dim threshold = avg(0) / 2
        ReDim valleys(3)
        For i = 0 To valleys.Count - 1
            valleys(i) = quartile * i
            Dim minVal = avg
            For j = quartile * i To quartile * (i + 1) - 1
                Dim nextVal = hArray(j)
                If nextVal < minVal And nextVal > threshold And (j - valleys(i)) >= minDistance Then
                    valleys(i) = j
                    minVal = nextVal
                End If
            Next
        Next

        Dim wPlot = dst2.Width / task.histogramBins
        For i = 0 To valleys.Count - 1
            Dim col = valleys(i) * wPlot
            dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.White, task.lineWidth + 1)
        Next
    End Sub
End Class







Public Class HistValley_FromPeaks : Inherits VB_Algorithm
    Public peak As New HistValley_Peaks
    Public peaks As New List(Of Integer)
    Public valleyIndex As New List(Of Integer)
    Public avgValley() As Single
    Public histList As New List(Of Single)
    Public Sub New()
        findSlider("Desired boundary count").Value = 10
        desc = "Use the peaks identified in HistValley_Peaks to find the valleys between the peaks."
    End Sub
    Public Sub updatePlot(dst As cv.Mat, bins As Integer)
        For Each valley In valleyIndex
            Dim col = dst.Width * valley / bins
            dst.Line(New cv.Point(col, dst.Height), New cv.Point(col, dst.Height * 9 / 10), cv.Scalar.White, task.lineWidth)
        Next
    End Sub
    Public Sub RunVB(src As cv.Mat)
        peak.Run(src)
        dst2 = peak.hist.dst2

        histList = peak.histArray.ToList
        peaks = New List(Of Integer)(peak.peaks)
        valleyIndex.Clear()
        For i = 0 To peaks.Count - 2
            Dim start = peaks(i)
            Dim finish = peaks(i + 1)

            Dim testList As New List(Of Single)
            For j = start To finish
                testList.Add(histList(j))
            Next

            valleyIndex.Add(start + testList.IndexOf(testList.Min))
        Next

        If task.optionsChanged Then ReDim avgValley(valleyIndex.Count - 1)

        Dim depthPerBin = task.maxZmeters / histList.Count
        For i = 0 To avgValley.Count - 1
            avgValley(i) = (avgValley(i) + valleyIndex(i) * depthPerBin) / 2
        Next

        If standaloneTest() Then
            updatePlot(dst2, task.histogramBins)
            setTrueText("Input data used by default is the depth data", 3)
        End If
        labels(2) = peak.labels(2) + " and " + CStr(valleyIndex.Count) + " valleys (marked at bottom)"
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

        If src.Type <> cv.MatType.CV_32FC1 Or standaloneTest() Then
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
        For i = 0 To histList.Count - 1
            If histList(i) <> 0 Then
                sortPeaks.Add(i, i)
                Exit For
            End If
        Next
        For i = histList.Count - 1 To 0 Step -1
            If histList(i) <> 0 Then
                sortPeaks.Add(i, i)
                Exit For
            End If
        Next
        For i = 0 To desiredBoundaries - 1
            Dim index = histList.IndexOf(histList.Max)
            Dim lastCount = histList(index)
            sortPeaks.Add(index, index)
            For j = index - 1 To 0 Step -1
                Dim count = histList(j)
                If lastCount > count Then histList(j) = 0 Else Exit For
                lastCount = count
            Next

            lastCount = histList(index)
            histList(index) = 0
            For j = index + 1 To histList.Count - 1
                Dim count = histList(j)
                If lastCount > count Then histList(j) = 0 Else Exit For
                lastCount = count
            Next
        Next

        Dim mm As mmData = vbMinMax(src)
        Dim incr = (mm.maxVal - mm.minVal) / task.histogramBins
        peaks.Clear()
        For Each index In sortPeaks.Keys
            Dim col = dst2.Width * index / task.histogramBins
            peaks.Add(index)
            dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height / 10), cv.Scalar.White, task.lineWidth)
        Next
        labels(2) = CStr(peaks.Count - 2) + " peaks (marked at top) were found in the histogram"
    End Sub
End Class






Public Class HistValley_Depth : Inherits VB_Algorithm
    Public valley As New HistValley_FromPeaks
    Public Sub New()
        labels(2) = "Top markerstop = peaks, bottom markers = valleys"
        desc = "Find the valleys in the depth histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static histogram As cv.Mat
        If task.heartBeat Then
            valley.Run(src)
            dst2 = valley.dst2

            Dim vList = New List(Of Single)(valley.valleyIndex)

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

        If standaloneTest() Then valley.updatePlot(dst2, task.histogramBins)
    End Sub
End Class






Public Class HistValley_Depth1 : Inherits VB_Algorithm
    Public valley As New HistValley_OptionsAuto
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
        If standaloneTest() Then gOptions.HistBinSlider.Value = 256
        desc = "Get the top X highest quality valley points in the histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim desiredBoundaries = options.desiredBoundaries

        ' input should be a histogram.  If not, get one...
        If standaloneTest() Then
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

        If standaloneTest() Then
            For Each entry In valleyOrder
                Dim col = entry.Value * dst2.Width / task.histogramBins
                dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.White, task.lineWidth)
            Next
            setTrueText(CStr(valleys.Count) + " valleys in histogram", 3)
        End If
    End Sub
End Class







Public Class HistValley_OptionsAuto : Inherits VB_Algorithm
    Dim kalman As New Histogram_Kalman
    Public histogram As New cv.Mat
    Public auto As New OpAuto_Valley
    Public Sub New()
        gOptions.HistBinSlider.Value = 256
        labels = {"", "", "Grayscale histogram - white lines are valleys", ""}
        desc = "Isolate the different levels of gray using the histogram valleys."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
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
    Dim valley As New HistValley_FromPeaks
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
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
    Dim valley As New HistValley_FromPeaks
    Dim edges As New EdgeDraw_Basics
    Public Sub New()
        desc = "Remove edge color in RGB before HistValley_FromPeaks"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)

        dst3 = src
        dst3.SetTo(cv.Scalar.Black, edges.dst2)

        valley.Run(dst3)
        dst2 = valley.dst2
    End Sub
End Class








Public Class HistValley_Simple : Inherits VB_Algorithm
    Dim trends As New SLR_Trends
    Public kalman As New Kalman_Basics
    Public depthRegions As New List(Of Integer)
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








Public Class HistValley_Tiers : Inherits VB_Algorithm
    Dim valleys As New HistValley_FromPeaks
    Public Sub New()
        labels = {"", "", "CV_8U tier map with values ranging from 0 to the desired valley count", "vbPalette output of dst2."}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Display the depth as tiers defined by the depth valleys in the histogram of depth."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat = False Then Exit Sub
        valleys.Run(src)

        dst2.SetTo(0)
        Dim marks = valleys.avgValley
        marks(0) = 0
        For i = 1 To marks.Count - 1
            dst2.SetTo(i + 1, task.pcSplit(2).InRange(marks(i - 1), marks(i)))
        Next
        dst2.SetTo(marks.Count, task.pcSplit(2).InRange(marks(marks.Count - 1), 100))

        dst3 = vbPalette(dst2 * 255 / (marks.Count + 1))
    End Sub
End Class







Public Class HistValley_Colors : Inherits VB_Algorithm
    Dim hist As New Histogram_Kalman
    Dim auto As New OpAuto_Valley
    Public Sub New()
        If standaloneTest() Then gOptions.HistBinSlider.Value = 256
        If standaloneTest() Then findSlider("Desired boundary count").Value = 10
        desc = "Find the histogram valleys for each of the colors."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static splitIndex As Integer
        If task.heartBeat Then splitIndex = (splitIndex + 1) Mod 3
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






Public Class HistValley_GrayKalman : Inherits VB_Algorithm
    Dim hist As New Histogram_Kalman
    Dim auto As New OpAuto_Valley
    Dim kalman As New Kalman_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.HistBinSlider.Value = 256
        If standaloneTest() Then findSlider("Desired boundary count").Value = 4
        desc = "Find the histogram valleys for a grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        hist.Run(src)
        dst2 = hist.dst2

        auto.Run(hist.hist.histogram)

        ReDim kalman.kInput(auto.valleyOrder.Count - 1)
        For i = 0 To auto.valleyOrder.Count - 1
            kalman.kInput(i) = auto.valleyOrder.ElementAt(i).Value
        Next
        kalman.Run(src)

        Dim lastEntry As Integer
        For i = 0 To kalman.kOutput.Count - 1
            Dim entry = auto.valleyOrder.ElementAt(i).Value
            For j = lastEntry To entry
                hist.hist.histogram.Set(Of Single)(j, 0, i)
            Next
            Dim col = dst2.Width * entry / task.histogramBins
            dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.White, task.lineWidth)
            lastEntry = entry
        Next
    End Sub
End Class






Public Class HistValley_GrayScale1 : Inherits VB_Algorithm
    Dim hist As New Histogram_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.HistBinSlider.Value = 256
        desc = "Find the histogram valleys for a grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        hist.Run(src)
        dst2 = hist.dst2

        Dim wquartile = dst2.Width / 4
        For i = 0 To 2
            Dim col = wquartile * (i + 1)
            dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.Yellow, task.lineWidth + 2)
        Next

        Dim start As Integer
        Dim lastentry As Integer
        Dim minEntries(3) As Integer
        Dim quartile = Math.Floor(hist.histogram.Rows / 4)
        For i = 0 To hist.histArray.Count - 1
            If hist.histArray(i) <> 0 And i > quartile / 4 Then
                lastentry = hist.histArray(i)
                minEntries(0) = i
                start = i
                Exit For
            End If
        Next

        For i = start To hist.histArray.Count - 1
            If hist.histArray(i) = 0 Then hist.histArray(i) = lastentry
            lastentry = hist.histArray(i)
        Next

        For i = 0 To minEntries.Count - 1
            minEntries(i) = quartile * i
            For j = quartile * i To quartile * (i + 1) - 1
                If hist.histArray(minEntries(i)) >= hist.histArray(j) Then minEntries(i) = j
            Next
        Next

        Dim wPlot = dst2.Width / task.histogramBins
        For i = 0 To minEntries.Count - 1
            Dim col = minEntries(i) * wPlot
            dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.White, task.lineWidth + 1)
        Next
    End Sub
End Class