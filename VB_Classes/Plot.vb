Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports Accord.Math

Public Class Plot_Basics : Inherits VB_Algorithm
    Dim plot As New Plot_Basics_CPP
    Dim hist As New Histogram_Graph
    Public plotCount As Integer = 3
    Public Sub New()
        hist.plotRequested = True
        labels(2) = "Plot of grayscale histogram"
        labels(3) = "Same Data but using OpenCV C++ plot"
        desc = "Plot data provided in src Mat"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist.plotColors(0) = cv.Scalar.White
        hist.Run(src)
        dst2 = hist.dst2

        ReDim plot.srcX(hist.histRaw(0).Rows - 1)
        ReDim plot.srcY(hist.histRaw(0).Rows - 1)
        For i = 0 To plot.srcX.Length - 1
            plot.srcX(i) = i
            plot.srcY(i) = hist.histRaw(0).Get(Of Single)(i, 0)
        Next
        plot.Run(Nothing)
        dst3 = plot.dst2
        labels(2) = hist.labels(2)
    End Sub
End Class




' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class Plot_Basics_CPP : Inherits VB_Algorithm
    Public srcX() As Double
    Public srcY() As Double
    Public Sub New()
        desc = "Demo the use of the integrated 2D plot available in OpenCV (only accessible in C++)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            ReDim srcX(50 - 1)
            ReDim srcY(50 - 1)
            For i = 0 To srcX.Length - 1
                srcX(i) = i
                srcY(i) = i * i * i
            Next
        End If
        Dim maxX As Double = Double.MinValue
        Dim minX As Double = Double.MaxValue
        Dim maxY As Double = Double.MinValue
        Dim minY As Double = Double.MaxValue
        For i = 0 To srcX.Length - 1
            If srcX(i) > maxX Then maxX = CInt(srcX(i))
            If srcX(i) < minX Then minX = CInt(srcX(i))
            If srcY(i) > maxY Then maxY = CInt(srcY(i))
            If srcY(i) < minY Then minY = CInt(srcY(i))
        Next

        Dim plotData(dst2.Total * dst2.ElemSize - 1) As Byte
        Dim handlePlot = GCHandle.Alloc(plotData, GCHandleType.Pinned)
        Dim handleX = GCHandle.Alloc(srcX, GCHandleType.Pinned)
        Dim handleY = GCHandle.Alloc(srcY, GCHandleType.Pinned)

        Plot_OpenCVBasics(handleX.AddrOfPinnedObject, handleY.AddrOfPinnedObject, srcX.Length - 1, handlePlot.AddrOfPinnedObject, dst2.Rows, dst2.Cols)

        Marshal.Copy(plotData, 0, dst2.Data, plotData.Length)
        handlePlot.Free()
        handleX.Free()
        handleY.Free()
        labels(2) = "x-Axis: " + CStr(minX) + " to " + CStr(maxX) + ", y-axis: " + CStr(minY) + " to " + CStr(maxY)
    End Sub
End Class






Public Class Plot_Histogram : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public minRange As Single = 0
    Public maxRange As Single = 255
    Public backColor As cv.Scalar = cv.Scalar.Red
    Public maxValue As Single
    Public minValue As Single
    Public plotCenter As Single
    Public barWidth As Single
    Public addLabels As Boolean = True
    Public noZeroEntry As Boolean = True
    Public createHistogram As Boolean = False
    Public Sub New()
        desc = "Plot histogram data with a stable scale at the left of the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Or createHistogram Then
            If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            cv.Cv2.CalcHist({src}, {0}, New cv.Mat(), histogram, 1, {task.histogramBins}, {New cv.Rangef(minRange, maxRange)})
        Else
            histogram = src
        End If

        If noZeroEntry Then histogram.Set(Of Single)(0, 0, 0) ' let's not plot the values at zero...i.e. Depth at 0, for instance, needs to be removed.

        dst2.SetTo(backColor)
        barWidth = dst2.Width / histogram.Rows
        plotCenter = barWidth * histogram.Rows / 2 + barWidth / 2
        Dim mm = vbMinMax(histogram)

        If mm.maxVal > 0 And histogram.Rows > 0 Then
            Dim count As Integer
            Dim incr = CInt(255 / histogram.Rows)
            For i = 0 To histogram.Rows - 1
                Dim offset = histogram.Get(Of Single)(i)
                If Single.IsNaN(offset) Then offset = 0
                If offset > 0 Then
                    Dim h = CInt(offset * dst2.Height / mm.maxVal)
                    Dim sIncr = CInt((i Mod 256) * incr)
                    Dim color = New cv.Scalar(sIncr, sIncr, sIncr)
                    If histogram.Rows > 255 Then color = cv.Scalar.Black
                    cv.Cv2.Rectangle(dst2, New cv.Rect(i * barWidth, dst2.Height - h, barWidth, h), color, -1)
                    count += 1
                End If
            Next
            If addLabels Then AddPlotScale(dst2, mm.minVal, mm.maxVal)
        End If
    End Sub
End Class





Module Plot_OpenCV_Module
    Public backColor = cv.Scalar.DarkGray
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Plot_OpenCVBasics(inX As IntPtr, inY As IntPtr, inLen As Integer, dstptr As IntPtr, rows As Integer, cols As Integer)
    End Sub
End Module






Public Class Plot_Depth : Inherits VB_Algorithm
    Dim plotDepth As New Plot_Basics_CPP
    Dim hist As New Histogram_Basics
    Public Sub New()
        desc = "Show depth using OpenCV's plot format with variable bins."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        src.SetTo(task.maxZmeters, task.maxDepthMask)

        hist.Run(src)
        ReDim plotDepth.srcX(hist.histogram.Rows - 1)
        ReDim plotDepth.srcY(hist.histogram.Rows - 1)
        For i = 0 To task.histogramBins - 1
            plotDepth.srcX(i) = i * task.maxZmeters / task.histogramBins
            plotDepth.srcY(i) = hist.histogram.Get(Of Single)(i, 0)
        Next
        plotDepth.Run(Nothing)
        dst2 = plotDepth.dst2

        If heartBeat() Then labels(2) = plotDepth.labels(2)
        Dim Split = Regex.Split(labels(2), "\W+")
        Dim lineCount = CInt(Split(4))
        If lineCount > 0 Then
            Dim meterDepth = CInt(src.Width / lineCount)
            For i = 1 To lineCount
                Dim x = i * meterDepth
                dst2.Line(New cv.Point(x, 0), New cv.Point(x, src.Height), cv.Scalar.White, task.lineWidth)
                setTrueText(Format(i, "0") + "m", New cv.Point(x + 4, src.Height - 10))
            Next
        End If
    End Sub
End Class






Public Class Plot_Histogram2D : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "2D Histogram", ""}
        desc = "Plot a 2D histgram from the input Mat"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim histogram = src.Clone
        If standalone Then
            Static options As New Options_ColorFormat
            options.RunVB()
            src = options.dst2
            Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 255), New cv.Rangef(0, 255)}
            cv.Cv2.CalcHist({src}, {0, 1}, New cv.Mat(), histogram, 2,
                            {task.histogramBins, task.histogramBins}, ranges)
        End If

        Dim mm = vbMinMax(histogram)
        histogram = 255 * (histogram - mm.minVal) / (mm.maxVal - mm.minVal)
        dst2 = histogram.Resize(dst2.Size, 0, 0,
                                cv.InterpolationFlags.Nearest).ConvertScaleAbs().CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standalone Then dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Plot_OverTime : Inherits VB_Algorithm
    Public plotData As cv.Scalar
    Public plotCount As Integer = 3
    Public plotColors() As cv.Scalar = {cv.Scalar.Blue, cv.Scalar.LawnGreen, cv.Scalar.Red, cv.Scalar.White}
    Public backColor = cv.Scalar.DarkGray
    Public minScale As Integer = 50
    Public maxScale As Integer = 200
    Public plotTriggerRescale = 50
    Public columnIndex As Integer
    Public offChartCount As Integer
    Public lastXdelta As New List(Of cv.Scalar)
    Public controlScale As Boolean ' Use this to programmatically control the scale (rather than let the automated way below keep the scale.)
    Dim myStopWatch As Stopwatch
    Public Sub New()
        desc = "Plot an input variable over time"
        Select Case task.workingRes.Width
            Case 1280
                gOptions.LineWidth.Value = 7
            Case 640
                gOptions.LineWidth.Value = 4
            Case 320
                gOptions.LineWidth.Value = 2
        End Select
        myStopWatch = Stopwatch.StartNew()
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static spreadTop As Single = Single.MinValue, spreadBot As Single = Single.MaxValue
        Const plotSeriesCount = 100
        lastXdelta.Add(plotData)

        If columnIndex + task.dotSize >= dst2.Width Then
            dst2.ColRange(columnIndex, dst2.Width).SetTo(backColor)
            columnIndex = 1
        End If
        dst2.ColRange(columnIndex, columnIndex + task.dotSize).SetTo(backColor)
        If standalone Then plotData = task.color.Mean()

        For i = 0 To plotCount - 1
            If Math.Floor(plotData(i)) < minScale Or Math.Ceiling(plotData(i)) > maxScale Then
                offChartCount += 1
                Exit For
            End If
        Next

        ' if enough points are off the charted area or if manually requested, then redo the scale.
        If (offChartCount > plotTriggerRescale And lastXdelta.Count >= plotSeriesCount And controlScale = False) Then
            If firstPass = False Then
                maxScale = Integer.MinValue
                minScale = Integer.MaxValue
                For i = 0 To lastXdelta.Count - 1
                    Dim nextVal = lastXdelta(i)
                    For j = 0 To plotCount - 1
                        If Math.Floor(nextVal(j)) < minScale Then minScale = Math.Floor(nextVal(j))
                        If Math.Floor(nextVal(j)) > maxScale Then maxScale = Math.Ceiling(nextVal(j))
                    Next
                Next
            End If
            lastXdelta.Clear()
            offChartCount = 0
            columnIndex = 1 ' restart at the left side of the chart
        End If

        If lastXdelta.Count >= plotSeriesCount Then lastXdelta.RemoveAt(0)

        For i = 0 To plotCount - 1
            Dim y = 1 - (plotData(i) - minScale) / (maxScale - minScale)
            y *= dst2.Height - 1
            If spreadTop < y Then spreadTop = y
            If spreadBot > y Then spreadBot = y
            Dim c As New cv.Point(columnIndex - task.dotSize, y - task.dotSize)
            If c.X < 1 Then c.X = 1
            dst2.Circle(c, task.dotSize, plotColors(i), -1, task.lineType)
        Next


        Static lastSeconds As Double
        Dim nextWatchVal = myStopWatch.ElapsedMilliseconds
        If nextWatchVal - lastSeconds > 1000 Then
            lastSeconds = nextWatchVal
            dst2.Line(New cv.Point(columnIndex, 0), New cv.Point(columnIndex, dst2.Height), cv.Scalar.White, 1)
        End If

        columnIndex += task.dotSize
        dst2.Col(columnIndex).SetTo(0)
        If standalone Then labels(2) = "RGB Means: blue = " + Format(plotData(0), fmt1) + " green = " + Format(plotData(1), fmt1) + " red = " + Format(plotData(2), fmt1)
        Dim lineCount = CInt(maxScale - minScale - 1)
        If lineCount > 3 Or lineCount < 0 Then lineCount = 3
        AddPlotScale(dst2, minScale, maxScale, lineCount)
    End Sub
End Class







Public Class Plot_OverTimeSingle : Inherits VB_Algorithm
    Public plotData As Single
    Public backColor = cv.Scalar.DarkGray
    Public max As Single, min As Single, avg, fmt As String
    Public plotColor = cv.Scalar.Blue
    Public Sub New()
        labels(2) = "Plot_OverTime "
        desc = "Plot an input variable over time"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static inputList As New List(Of Single)
        dst2 = dst2.Resize(task.quarterRes)
        If standalone Then plotData = task.color.Mean(task.depthMask)(0)

        If inputList.Count >= dst2.Width Then inputList.RemoveAt(0)
        inputList.Add(plotData)
        dst2.ColRange(New cv.Range(0, inputList.Count)).SetTo(backColor)

        max = inputList.Max
        min = inputList.Min
        For i = 0 To inputList.Count - 1
            Dim y = 1 - (inputList(i) - min) / (max - min)
            y *= dst2.Height - 1
            Dim c As New cv.Point(i, y)
            If c.X < 1 Then c.X = 1
            dst2.Circle(c, 1, plotColor, -1, task.lineType)
        Next

        If inputList.Count > dst2.Width / 8 Then
            Dim diff = max - min
            Dim fmt = If(diff > 10, fmt0, If(diff > 2, fmt1, If(diff > 0.5, fmt2, fmt3)))
            For i = 0 To 2
                Dim nextText = Format(Choose(i + 1, max, inputList.Average, min), fmt)
                Dim pt = Choose(i + 1, New cv.Point(0, 10), New cv.Point(0, dst2.Height / 2 - 5), New cv.Point(0, dst2.Height - 3))
                cv.Cv2.PutText(dst2, nextText, pt, cv.HersheyFonts.HersheyPlain, 0.7, cv.Scalar.White, 1, task.lineType)
            Next
        End If

        Dim p1 = New cv.Point(0, dst2.Height / 2)
        Dim p2 = New cv.Point(dst2.Width, dst2.Height / 2)
        dst2.Line(p1, p2, cv.Scalar.White, task.cvFontThickness)
        If standalone Then setTrueText("Standalone test is with the blue channel mean of the color image.", 3)
    End Sub
End Class








Public Class Plot_OverTimeScalar : Inherits VB_Algorithm
    Public plotData As cv.Scalar
    Public plotCount As Integer = 3
    Public plotList As New List(Of Plot_OverTimeSingle)
    Dim mats As New Mat_4Click
    Public Sub New()
        For i = 0 To 3
            plotList.Add(New Plot_OverTimeSingle)
            plotList(i).plotColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.Yellow)
        Next
        desc = "Plot the requested number of entries in the cv.scalar input"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then plotData = task.color.Mean()

        For i = 0 To Math.Min(plotCount, 4) - 1
            plotList(i).plotData = plotData(i)
            plotList(i).Run(src)
            mats.mat(i) = plotList(i).dst2
        Next

        mats.Run(Nothing)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class
