Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Public Class Plot_Basics : Inherits VB_Parent
    Dim plot As New Plot_Basics_CPP
    Dim hist As New Hist_Graph
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

        For i = 0 To hist.histRaw(0).Rows - 1
            plot.srcX.Add(i)
            plot.srcY.Add(hist.histRaw(0).Get(Of Single)(i, 0))
        Next
        plot.Run(empty)
        dst3 = plot.dst2
        labels(2) = hist.labels(2)
    End Sub
End Class







Public Class Plot_Histogram : Inherits VB_Parent
    Public histogram As New cv.Mat
    Public histArray() As Single
    Public minRange As Single = 0
    Public maxRange As Single = 255
    Public backColor As cv.Scalar = cv.Scalar.Red
    Public maxValue As Single
    Public minValue As Single
    Public plotCenter As Single
    Public barWidth As Single
    Public addLabels As Boolean = True
    Public removeZeroEntry As Boolean = True
    Public createHistogram As Boolean = False
    Public mm As mmData
    Public Sub New()
        desc = "Plot histogram data with a stable scale at the left of the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Or createHistogram Then
            If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            cv.Cv2.CalcHist({src}, {0}, New cv.Mat(), histogram, 1, {task.histogramBins}, {New cv.Rangef(minRange, maxRange)})
        Else
            histogram = src
        End If

        If removeZeroEntry Then histogram.Set(Of Single)(0, 0, 0) ' let's not plot the values at zero...i.e. Depth at 0, for instance, needs to be removed.

        dst2.SetTo(backColor)
        barWidth = dst2.Width / histogram.Rows
        plotCenter = barWidth * histogram.Rows / 2 + barWidth / 2

        ReDim histArray(histogram.Rows - 1)
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

        mm = vbMinMax(histogram)

        If mm.maxVal > 0 And histogram.Rows > 0 Then
            Dim incr = 255 / histogram.Rows
            For i = 0 To histArray.Count - 1
                If Single.IsNaN(histArray(i)) Then histArray(i) = 0
                If histArray(i) > 0 Then
                    Dim h = CInt(histArray(i) * dst2.Height / mm.maxVal)
                    Dim sIncr = (i Mod 256) * incr
                    Dim color = New cv.Scalar(sIncr, sIncr, sIncr)
                    If histogram.Rows > 255 Then color = cv.Scalar.Black
                    cv.Cv2.Rectangle(dst2, New cv.Rect(i * barWidth, dst2.Height - h, Math.Max(1, barWidth), h), color, -1)
                End If
            Next
            If addLabels Then AddPlotScale(dst2, mm.minVal, mm.maxVal)
        End If
    End Sub
End Class








Public Class Plot_Depth : Inherits VB_Parent
    Dim plotDepth As New Plot_Basics_CPP
    Dim hist As New Hist_Basics
    Public Sub New()
        desc = "Show depth using OpenCV's plot format with variable bins."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        'src.SetTo(task.maxZmeters, task.maxDepthMask)

        hist.Run(src)
        plotDepth.srcX.Clear()
        plotDepth.srcY.Clear()
        For i = 0 To task.histogramBins - 1
            plotDepth.srcX.Add(i * task.maxZmeters / task.histogramBins)
            plotDepth.srcY.Add(hist.histogram.Get(Of Single)(i, 0))
        Next
        plotDepth.Run(empty)
        dst2 = plotDepth.dst2

        If task.heartBeat Then labels(2) = plotDepth.labels(2)
        Dim Split = Regex.Split(labels(2), "\W+")
        Dim lineCount = CInt(Split(4))
        If lineCount > 0 Then
            Dim meterDepth = CInt(src.Width / lineCount)
            For i = 1 To lineCount
                Dim x = i * meterDepth
                drawLine(dst2, New cv.Point(x, 0), New cv.Point(x, src.Height), cv.Scalar.White)
                setTrueText(Format(i, "0") + "m", New cv.Point(x + 4, src.Height - 10))
            Next
        End If
    End Sub
End Class






Public Class Plot_Histogram2D : Inherits VB_Parent
    Public Sub New()
        labels = {"", "", "2D Histogram", ""}
        desc = "Plot a 2D histgram from the input Mat"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim histogram = src.Clone
        If standaloneTest() Then
            Static options As New Options_ColorFormat
            options.RunVB()
            src = options.dst2
            Dim bins = task.histogramBins
            cv.Cv2.CalcHist({src}, {0, 1}, New cv.Mat(), histogram, 2, {bins, bins}, redOptions.rangesBGR)
        End If

        dst2 = histogram.Resize(dst2.Size, 0, 0, cv.InterpolationFlags.Nearest)

        If standaloneTest() Then dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class








Public Class Plot_OverTimeSingle : Inherits VB_Parent
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
        If standaloneTest() Then plotData = task.color.Mean(task.depthMask)(0)

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
            drawCircle(dst2,c, 1, plotColor)
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
        If standaloneTest() Then setTrueText("standaloneTest() test is with the blue channel mean of the color image.", 3)
    End Sub
End Class








Public Class Plot_OverTimeScalar : Inherits VB_Parent
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
        If standaloneTest() Then plotData = task.color.Mean()

        For i = 0 To Math.Min(plotCount, 4) - 1
            plotList(i).plotData = plotData(i)
            plotList(i).Run(src)
            mats.mat(i) = plotList(i).dst2
        Next

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class






Public Class Plot_OverTime : Inherits VB_Parent
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
    Public Sub New()
        desc = "Plot an input variable over time"
        Select Case task.workingRes.Width
            Case 1920
                gOptions.LineWidth.Value = 10
            Case 1280
                gOptions.LineWidth.Value = 7
            Case 640
                gOptions.LineWidth.Value = 4
            Case 320
                gOptions.LineWidth.Value = 2
            Case Else
                gOptions.LineWidth.Value = 1
        End Select
        gOptions.dotSizeSlider.Value = gOptions.LineWidth.Value
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Const plotSeriesCount = 100
        lastXdelta.Add(plotData)

        If columnIndex + task.dotSize >= dst2.Width Then
            dst2.ColRange(columnIndex, dst2.Width).SetTo(backColor)
            columnIndex = 1
        End If
        dst2.ColRange(columnIndex, columnIndex + task.dotSize).SetTo(backColor)
        If standaloneTest() Then plotData = task.color.Mean()

        For i = 0 To plotCount - 1
            If Math.Floor(plotData(i)) < minScale Or Math.Ceiling(plotData(i)) > maxScale Then
                offChartCount += 1
                Exit For
            End If
        Next

        ' if enough points are off the charted area or if manually requested, then redo the scale.
        If (offChartCount > plotTriggerRescale And lastXdelta.Count >= plotSeriesCount And controlScale = False) Then
            If Not firstPass Then
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
            Dim c As New cv.Point(columnIndex - task.dotSize, y - task.dotSize)
            If c.X < 1 Then c.X = 1
            drawCircle(dst2,c, task.dotSize, plotColors(i))
        Next


        If task.heartBeat Then
            dst2.Line(New cv.Point(columnIndex, 0), New cv.Point(columnIndex, dst2.Height), cv.Scalar.White, 1)
        End If

        columnIndex += task.dotSize
        dst2.Col(columnIndex).SetTo(0)
        If standaloneTest() Then labels(2) = "RGB Means: blue = " + Format(plotData(0), fmt1) + " green = " + Format(plotData(1), fmt1) + " red = " + Format(plotData(2), fmt1)
        Dim lineCount = CInt(maxScale - minScale - 1)
        If lineCount > 3 Or lineCount < 0 Then lineCount = 3
        AddPlotScale(dst2, minScale, maxScale, lineCount)
    End Sub
End Class






Public Class Plot_OverTimeFixedScale : Inherits VB_Parent
    Public plotData As cv.Scalar
    Public plotCount As Integer = 3
    Public plotColors() As cv.Scalar = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.White}
    Public backColor = cv.Scalar.DarkGray
    Public minScale As Integer = 50
    Public maxScale As Integer = 200
    Public plotTriggerRescale = 50
    Public columnIndex As Integer
    Public offChartCount As Integer
    Public lastXdelta As New List(Of cv.Scalar)
    Public controlScale As Boolean ' Use this to programmatically control the scale (rather than let the automated way below keep the scale.)
    Public showScale As Boolean = True
    Public fixedScale As Boolean
    Dim plotOutput As cv.Mat
    Public Sub New()
        plotOutput = New cv.Mat(New cv.Size(320, 180), cv.MatType.CV_8UC3, 0)
        desc = "Plot an input variable over time"
        gOptions.LineWidth.Value = 1
        gOptions.dotSizeSlider.Value = 2
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Const plotSeriesCount = 100
        lastXdelta.Add(plotData)

        If columnIndex + task.dotSize >= plotOutput.Width Then
            plotOutput.ColRange(columnIndex, plotOutput.Width).SetTo(backColor)
            columnIndex = 1
        End If
        plotOutput.ColRange(columnIndex, columnIndex + task.dotSize).SetTo(backColor)
        If standaloneTest() Then plotData = task.color.Mean()

        For i = 0 To plotCount - 1
            If Math.Floor(plotData(i)) < minScale Or Math.Ceiling(plotData(i)) > maxScale Then
                offChartCount += 1
                Exit For
            End If
        Next

        If fixedScale = False Then
            ' if enough points are off the charted area or if manually requested, then redo the scale.
            If (offChartCount > plotTriggerRescale And lastXdelta.Count >= plotSeriesCount And controlScale = False) Then
                If Not firstPass Then
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
        End If

        If lastXdelta.Count >= plotSeriesCount Then lastXdelta.RemoveAt(0)

        If task.heartBeat Then
            plotOutput.Line(New cv.Point(columnIndex, 0), New cv.Point(columnIndex, plotOutput.Height), cv.Scalar.White, task.lineWidth)
        End If

        For i = 0 To plotCount - 1
            If plotData(i) <> 0 Then
                Dim y = 1 - (plotData(i) - minScale) / (maxScale - minScale)
                y *= plotOutput.Height - 1
                Dim c As New cv.Point(columnIndex - task.dotSize, y - task.dotSize)
                If c.X < 1 Then c.X = 1
                drawCircle(plotOutput, c, task.dotSize, plotColors(i))
            End If
        Next

        columnIndex += 1
        plotOutput.Col(columnIndex).SetTo(0)
        labels(2) = "Blue = " + Format(plotData(0), fmt1) + " Green = " + Format(plotData(1), fmt1) +
                    " Red = " + Format(plotData(2), fmt1) + " Yellow = " + Format(plotData(3), fmt1)
        strOut = "Blue = " + Format(plotData(0), fmt1) + vbCrLf
        strOut += "Green = " + Format(plotData(1), fmt1) + vbCrLf
        strOut += "Red = " + Format(plotData(2), fmt1) + vbCrLf
        strOut += "White = " + Format(plotData(3), fmt1) + vbCrLf
        setTrueText(strOut, 3)
        Dim lineCount = CInt(maxScale - minScale - 1)
        If lineCount > 3 Or lineCount < 0 Then lineCount = 3
        If showScale Then AddPlotScale(plotOutput, minScale, maxScale, lineCount)
        dst2 = plotOutput.Resize(task.workingRes)
    End Sub
End Class




Public Class Plot_Beats : Inherits VB_Parent
    Dim plot As New Plot_OverTimeFixedScale
    Public Sub New()
        plot.plotCount = 4
        plot.showScale = False
        plot.fixedScale = True
        plot.minScale = 0
        plot.maxScale = 5
        desc = "Plot the beats."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        plot.plotData(0) = If(task.heartBeat, 1, -1)
        plot.plotData(1) = If(task.midHeartBeat, 2, -1)
        plot.plotData(2) = If(task.quarterBeat, 3, -1)
        plot.plotData(3) = If(task.almostHeartBeat, 4, -1)
        plot.Run(src)
        dst2 = plot.dst2

        strOut = "task.heartBeat (blue) = " + CStr(plot.plotData(0)) + vbCrLf
        strOut += "task.midHeartBeat (green) = " + CStr(plot.plotData(1)) + vbCrLf
        strOut += "task.quarterBeat (red) = " + CStr(plot.plotData(2)) + vbCrLf
        strOut += "task.almostHeartBeat (white) = " + CStr(plot.plotData(3)) + vbCrLf
        setTrueText(strOut, 3)
    End Sub
End Class





' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class Plot_Basics_CPP : Inherits VB_Parent
    Public srcX As New List(Of Double)
    Public srcY As New List(Of Double)
    Public Sub New()
        For i = 0 To CInt(task.maxZmeters) ' something to plot if standaloneTest().
            srcX.Add(i)
            srcY.Add(i * i * i)
        Next
        cPtr = PlotOpenCV_Open()
        desc = "Demo the use of the integrated 2D plot available in OpenCV (only accessible in C++)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim handleX = GCHandle.Alloc(srcX.ToArray, GCHandleType.Pinned)
        Dim handleY = GCHandle.Alloc(srcY.ToArray, GCHandleType.Pinned)

        Dim imagePtr = PlotOpenCV_Run(cPtr, handleX.AddrOfPinnedObject, handleY.AddrOfPinnedObject, srcX.Count,
                                      dst2.Rows, dst2.Cols)

        dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr)
        handleX.Free()
        handleY.Free()

        Dim maxX = srcX.Max, minX = srcX.Min, maxY = srcY.Max, minY = srcY.Min
        labels(2) = "x-Axis: " + CStr(minX) + " to " + CStr(maxX) + ", y-axis: " + CStr(minY) + " to " + CStr(maxY)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = PlotOpenCV_Close(cPtr)
    End Sub
End Class






' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class Plot_Dots : Inherits VB_Parent
    Public srcX As New List(Of Double)
    Public srcY As New List(Of Double)
    Public plotColor = cv.Scalar.Yellow
    Public wipeSlate As Boolean = True
    Public Sub New()
        For i = 0 To 50 ' something to plot if standaloneTest().
            srcX.Add(i)
            srcY.Add(i * i * i)
        Next
        desc = "Plot the requested points..."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim maxX = srcX.Max, minX = srcX.Min, maxY = srcY.Max, minY = srcY.Min
        If wipeSlate Then dst2.SetTo(0)
        For i = 0 To srcX.Count - 1
            Dim pt = New cv.Point(dst2.Width * srcX(i) / maxX, dst2.Height - dst2.Height * srcY(i) / maxY)
            drawCircle(dst2,pt, task.dotSize, plotColor)
        Next
        labels(2) = "x-Axis: " + CStr(minX) + " to " + CStr(maxX) + ", y-axis: " + CStr(minY) + " to " + CStr(maxY)
    End Sub
End Class