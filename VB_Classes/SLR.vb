Imports cv = OpenCvSharp
Imports System.IO
' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
Public Class SLR_Basics : Inherits VBparent
    Public input As New SLR_Data
    Dim slr As New CS_Classes.SLR
    Dim plot As New Plot_Basics_CPP
    Public Sub New()
        If standalone Then
            input.Run(dst1)
            label1 = "Sample data input"
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Approximate accuracy (tolerance) X100", 1, 1000, 30)
            sliders.setupTrackBar(1, "Simple moving average window size", 1, 100, 20)
            sliders.setupTrackBar(2, "Desired number of segments (for SLR_Trends)", 1, 80, 20)
        End If
        task.desc = "Segmented Linear Regression example"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static toleranceSlider = findSlider("Approximate accuracy (tolerance) X100")
        Static movingAvgSlider = findSlider("Simple moving average window size")
        Dim tolerance = toleranceSlider.value / 100
        Dim halfLength = movingAvgSlider.value

        Dim resultX As New List(Of Double)
        Dim resultY As New List(Of Double)

        slr.SegmentedRegressionFast(input.dataX, input.dataY, tolerance, halfLength, resultX, resultY)

        label1 = "Tolerance = " + CStr(tolerance) + " and moving average window = " + CStr(halfLength)
        If resultX.Count > 0 Then
            plot.srcX = input.dataX.ToArray
            plot.srcY = input.dataY.ToArray
            plot.Run(src)
            dst1 = plot.dst1.Clone

            plot.srcX = resultX.ToArray
            plot.srcY = resultY.ToArray
            plot.Run(src)
            dst2 = plot.dst1
        Else
            dst1.SetTo(0)
            dst2.SetTo(0)
            setTrueText(label1 + " yielded no results...")
        End If
        If standalone = False Then
            input.dataX.Clear()
            input.dataY.Clear()
        End If
    End Sub
End Class






' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
Public Class SLR_Data : Inherits VBparent
    Dim plot As New Plot_Basics_CPP
    Public dataX As New List(Of Double)
    Public dataY As New List(Of Double)
    Public Sub New()
        Dim sr = New StreamReader(task.parms.homeDir + "/Data/real_data.txt")
        Dim code As String = sr.ReadToEnd
        sr.Close()

        Dim lines = code.Split(vbLf)
        For Each line In lines
            Dim split = line.Split(" ")
            If split.Length > 1 Then
                dataX.Add(CDbl(split(0)))
                dataY.Add(CDbl(split(1)))
            End If
        Next
        task.desc = "Plot the data used in SLR_Basics"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        plot.srcX = dataX.ToArray
        plot.srcY = dataY.ToArray
        plot.Run(src)
        dst1 = plot.dst1
    End Sub
End Class







Public Class SLR_Image : Inherits VBparent
    Public slr As New SLR_Basics
    Public hist As New Histogram_Basics
    Public Sub New()
        label1 = "Original data"
        task.desc = "Run Segmented Linear Regression on grayscale image data - just an experiment"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hist.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst1 = hist.dst1
        For i = 0 To hist.histogram.Rows - 1
            slr.input.dataX.Add(i)
            slr.input.dataY.Add(hist.histogram.Get(Of Single)(i, 0))
        Next
        slr.Run(src)
        dst2 = slr.dst2
    End Sub
End Class








Public Class SLR_Trends : Inherits VBparent
    Dim slr As New SLR_Image
    Public Sub New()
        task.desc = "Manual SLR - just find the trends of the plot data within the each segment"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static segSlider = findSlider("Desired number of segments (for SLR_Trends)")
        Dim segs = segSlider.value

        task.histogramBins = 256
        label1 = "There are " + CStr(segs) + " with " + CStr(task.histogramBins) + " histogram bins."
        slr.Run(src)
        dst1 = slr.dst1

        Dim indexer = slr.hist.histogram.GetGenericIndexer(Of Single)()
        Dim valList As New List(Of Single)
        For i = 0 To slr.hist.histogram.Rows - 1
            valList.Add(indexer(i))
        Next
        slr.hist.plotHist.plotMaxValue = valList.Max

        Dim incr = valList.Count / segs
        Dim spacer = dst1.Width / segs
        Dim pixelsPerUnit = dst1.Height / slr.hist.plotHist.plotMaxValue
        Dim accum As Single, lastI As Integer, offset As Single, segIndex As Integer
        For i = 0 To valList.Count - 1
            offset = CInt(segIndex * spacer)
            If i >= segIndex * incr Then
                If segIndex > 0 Then
                    Dim p0 = New cv.Point2f(offset - spacer / 2, dst1.Height - accum * pixelsPerUnit / (i - lastI))
                    dst1.Circle(p0, task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
                End If
                accum = 0
                segIndex += 1
                lastI = i
            End If
            Dim p1 = New cv.Point2f(offset, 0)
            Dim p2 = New cv.Point2f(offset, dst1.Height)
            dst1.Line(p1, p2, cv.Scalar.Black, task.lineWidth)
            accum += valList(i)
        Next
        Dim pt = New cv.Point2f(offset - spacer / 2, dst1.Height - accum * pixelsPerUnit / (valList.Count - lastI - 1))
        dst1.Circle(pt, task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
    End Sub
End Class








Public Class SLR_TrendSplitter : Inherits VBparent
    Dim slr As New SLR_Image
    Public Sub New()
        task.desc = "Find trends by finding peak/valley and splitting data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static segSlider = findSlider("Desired number of segments (for SLR_Trends)")
        Dim segs = segSlider.value

        'task.histogramBins = 256
        label1 = ""
        slr.Run(src)
        dst1 = slr.dst1

        Dim indexer = slr.hist.histogram.GetGenericIndexer(Of Single)()
        Dim valList As New List(Of Single)
        Dim spacer = dst1.Width / segs
        Dim offset As Single, segIndex As Integer
        Dim incr = CInt(slr.hist.histogram.Rows / segs / 2)
        For i = 0 To slr.hist.histogram.Rows - 1
            offset = CInt(segIndex * spacer)
            If i > segIndex * incr * 2 Then
                Dim p1 = New cv.Point2f(offset, 0)
                Dim p2 = New cv.Point2f(offset, dst1.Height)
                dst1.Line(p1, p2, cv.Scalar.Black, task.lineWidth)
                segIndex += 1
            End If
            valList.Add(indexer(i))
        Next

        Dim sortList As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
        Dim avg = valList.Average()
        For i = 0 To valList.Count - 1
            If valList(i) > avg / 4 Then sortList.Add(valList(i), i) Else sortList.Add(avg, i)
        Next

        Dim used(valList.Count - 1) As Boolean
        Dim usedCount As Integer
        Dim toggle As Boolean
        Dim backSide As Integer
        slr.hist.plotHist.plotMaxValue = valList.Max
        For i = 0 To valList.Count - 1
            Dim index = sortList.ElementAt(i).Value
            If used(index) = False Then
                If toggle Then
                    index = sortList.ElementAt(sortList.Count - backSide - 1).Value
                    backSide += 1
                End If
                toggle = Not toggle
                Dim pt = New cv.Point2f(dst1.Width * index / valList.Count, dst1.Height - dst1.Height * valList(index) / slr.hist.plotHist.plotMaxValue)
                dst1.Circle(pt, task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)

                For j = Math.Max(0, index - incr) To Math.Min(valList.Count, index + incr) - 1
                    used(j) = True
                Next
                usedCount += 1
                If usedCount >= segs Then Exit For
            End If
        Next
    End Sub
End Class








Public Class SLR_TrendFusion : Inherits VBparent
    Dim slr As New SLR_Image
    Public Sub New()
        task.desc = "Find trends by filling in short gaps."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static segSlider = findSlider("Desired number of segments (for SLR_Trends)")
        Dim segs = segSlider.value

        label1 = ""
        slr.Run(src)
        dst1 = slr.dst1

        Dim indexer = slr.hist.histogram.GetGenericIndexer(Of Single)()
        Dim valList As New List(Of Single)
        Dim incr = CInt(slr.hist.histogram.Rows / segs / 2)
        For i = 0 To slr.hist.histogram.Rows - 1
            valList.Add(indexer(i))
        Next

        If valList.Count < 2 Then Exit Sub
        slr.hist.plotHist.plotMaxValue = valList.Max
        For i = 1 To valList.Count - 2
            Dim prevVal = valList(i - 1)
            Dim currVal = valList(i)
            Dim nextVal = valList(i + 1)
            If prevVal > currVal And nextVal > currVal Then valList(i) = (prevVal + nextVal) / 2
            Dim p1 = New cv.Point2f(dst1.Width * (i - 1) / valList.Count, dst1.Height - dst1.Height * valList(i - 1) / slr.hist.plotHist.plotMaxValue)
            Dim p2 = New cv.Point2f(dst1.Width * i / valList.Count, dst1.Height - dst1.Height * valList(i) / slr.hist.plotHist.plotMaxValue)
            dst1.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        Next
    End Sub
End Class