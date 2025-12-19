Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Namespace VBClasses
    Public Class Plot_Basics : Inherits TaskParent
        Dim plot As New Plot_Basics_CPP
        Dim hist As New Hist_Graph
        Public plotCount As Integer = 3
        Public Sub New()
            hist.plotRequested = True
            labels(2) = "Plot of grayscale histogram"
            labels(3) = "Same Data but using OpenCV C++ plot"
            desc = "Plot data provided in src Mat"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            hist.plotColors(0) = cv.Scalar.White
            hist.Run(src)
            dst2 = hist.dst2

            For i = 0 To hist.histRaw(0).Rows - 1
                plot.srcX.Add(i)
                plot.srcY.Add(hist.histRaw(0).Get(Of Single)(i, 0))
            Next
            plot.Run(src)
            dst3 = plot.dst2
            labels(2) = hist.labels(2)
        End Sub
    End Class







    Public Class Plot_Depth : Inherits TaskParent
        Dim plotDepth As New Plot_Basics_CPP
        Dim hist As New Hist_Basics
        Public Sub New()
            desc = "Show depth using OpenCV's plot format with variable bins."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Type <> cv.MatType.CV_32F Then src = taskAlg.pcSplit(2)
            'src.SetTo(taskAlg.MaxZmeters, taskAlg.maxDepthMask)

            hist.Run(src)
            plotDepth.srcX.Clear()
            plotDepth.srcY.Clear()
            For i = 0 To taskAlg.histogramBins - 1
                plotDepth.srcX.Add(i * taskAlg.MaxZmeters / taskAlg.histogramBins)
                plotDepth.srcY.Add(hist.histogram.Get(Of Single)(i, 0))
            Next
            plotDepth.Run(src)
            dst2 = plotDepth.dst2

            If taskAlg.heartBeat Then labels(2) = plotDepth.labels(2)
            Dim Split = Regex.Split(labels(2), "\W+")
            Dim lineCount = CInt(Split(4))
            If lineCount > 0 Then
                Dim meterDepth = src.Width \ lineCount
                For i = 1 To lineCount
                    Dim x = i * meterDepth
                    vbc.DrawLine(dst2, New cv.Point(x, 0), New cv.Point(x, src.Height), white)
                    SetTrueText(Format(i, "0") + "m", New cv.Point(x + 4, src.Height - 10))
                Next
            End If
        End Sub
    End Class






    Public Class Plot_Histogram2D : Inherits TaskParent
        Public colorFmt As New Color_Basics
        Public Sub New()
            labels = {"", "", "2D Histogram", ""}
            desc = "Plot a 2D histgram from the input Mat"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim histogram = src.Clone
            If standaloneTest() Then
                colorFmt.Run(src)
                src = colorFmt.dst2
                Dim bins = taskAlg.histogramBins
                cv.Cv2.CalcHist({src}, {0, 1}, New cv.Mat(), histogram, 2, {bins, bins}, taskAlg.rangesBGR)
            End If

            dst2 = histogram.Resize(dst2.Size(), 0, 0, cv.InterpolationFlags.Nearest)

            If standaloneTest() Then dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        End Sub
    End Class








    Public Class Plot_OverTimeSingle : Inherits TaskParent
        Public plotData As Single
        Public backColor = cv.Scalar.DarkGray
        Public max As Single, min As Single, avg, fmt As String
        Public useFixedRange As Boolean
        Public plotColor = cv.Scalar.Blue
        Dim inputList As New List(Of Single)
        Public Sub New()
            labels(2) = "Plot_OverTime "
            desc = "Plot an input variable over time"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then plotData = taskAlg.color.Mean(taskAlg.depthmask)(0)

            If inputList.Count >= dst2.Width Then inputList.RemoveAt(0)
            inputList.Add(plotData)
            dst2.ColRange(New cv.Range(0, inputList.Count)).SetTo(backColor)

            If useFixedRange = False Then
                max = inputList.Max
                min = inputList.Min
            End If
            Dim y As Single
            For i = 0 To inputList.Count - 1
                y = 1 - (inputList(i) - min) / (max - min)
                y *= dst2.Height - 1
                Dim c As New cv.Point2f(i, y)
                If c.X < 1 Then c.X = 1
                dst2.Circle(c, taskAlg.DotSize, blue, -1, taskAlg.lineType)
            Next

            If inputList.Count > dst2.Width / 8 Then
                Dim diff = max - min
                Dim fmt = If(diff > 10, fmt0, If(diff > 2, fmt1, If(diff > 0.5, fmt2, fmt3)))
                Dim nextText As String
                For i = 0 To 2
                    If useFixedRange Then
                        nextText = Choose(i + 1, CStr(max), CStr((max + min) \ 2), CStr(min))
                    Else
                        nextText = Format(Choose(i + 1, max, inputList.Average, min), fmt)
                    End If
                    Dim pt = Choose(i + 1, New cv.Point(0, 10), New cv.Point(0, dst2.Height \ 2 - 5),
                                New cv.Point(0, dst2.Height - 3))
                    cv.Cv2.PutText(dst2, nextText, pt, cv.HersheyFonts.HersheyPlain, 0.7, white, 1, taskAlg.lineType)
                Next
            End If

            Dim p1 = New cv.Point(0, dst2.Height / 2)
            Dim p2 = New cv.Point(dst2.Width, dst2.Height / 2)
            dst2.Line(p1, p2, white, taskAlg.cvFontThickness)
            If standaloneTest() Then SetTrueText("standaloneTest() test is with the blue channel mean of the color image.", 3)
        End Sub
    End Class








    Public Class Plot_OverTimeScalar : Inherits TaskParent
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then plotData = taskAlg.color.Mean()

            For i = 0 To Math.Min(plotCount, 4) - 1
                plotList(i).plotData = plotData(i)
                plotList(i).Run(src)
                mats.mat(i) = plotList(i).dst2
            Next

            mats.Run(emptyMat)
            dst2 = mats.dst2
            dst3 = mats.dst3
        End Sub
    End Class






    Public Class Plot_OverTime : Inherits TaskParent
        Public plotData As cv.Scalar
        Public plotCount As Integer = 3
        Public plotColors() As cv.Scalar = {cv.Scalar.Blue, cv.Scalar.LawnGreen, cv.Scalar.Red, white}
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
            Select Case taskAlg.workRes.Width
                Case 1920
                    taskAlg.gOptions.LineWidth.Value = 10
                Case 1280
                    taskAlg.gOptions.LineWidth.Value = 7
                Case 640
                    taskAlg.gOptions.LineWidth.Value = 4
                Case 320
                    taskAlg.gOptions.LineWidth.Value = 2
                Case Else
                    taskAlg.gOptions.LineWidth.Value = 1
            End Select
            taskAlg.gOptions.DotSizeSlider.Value = taskAlg.gOptions.LineWidth.Value
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Const plotSeriesCount = 100
            lastXdelta.Add(plotData)

            If columnIndex + taskAlg.DotSize >= dst2.Width Then
                dst2.ColRange(columnIndex, dst2.Width).SetTo(backColor)
                columnIndex = 1
            End If
            dst2.ColRange(columnIndex, columnIndex + taskAlg.DotSize).SetTo(backColor)
            If standaloneTest() Then plotData = taskAlg.color.Mean()

            For i = 0 To plotCount - 1
                If Math.Floor(plotData(i)) < minScale Or Math.Ceiling(plotData(i)) > maxScale Then
                    offChartCount += 1
                    Exit For
                End If
            Next

            ' if enough points are off the charted area or if manually requested, then redo the scale.
            If (offChartCount > plotTriggerRescale And lastXdelta.Count >= plotSeriesCount And controlScale = False) Then
                If Not taskAlg.firstPass Then
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
                Dim c As New cv.Point(columnIndex - taskAlg.DotSize, y - taskAlg.DotSize)
                If c.X < 1 Then c.X = 1
                DrawCircle(dst2, c, taskAlg.DotSize, plotColors(i))
            Next


            If taskAlg.heartBeat Then
                dst2.Line(New cv.Point(columnIndex, 0), New cv.Point(columnIndex, dst2.Height), white, 1)
            End If

            columnIndex += taskAlg.DotSize
            dst2.Col(columnIndex).SetTo(0)
            If standaloneTest() Then labels(2) = "RGB Means: blue = " + Format(plotData(0), fmt1) + " green = " + Format(plotData(1), fmt1) + " red = " + Format(plotData(2), fmt1)
            Dim lineCount = CInt(maxScale - minScale - 1)
            If lineCount > 3 Or lineCount < 0 Then lineCount = 3
            AddPlotScale(dst2, minScale, maxScale, lineCount)
        End Sub
    End Class






    Public Class Plot_OverTimeFixedScale : Inherits TaskParent
        Public plotData As cv.Scalar
        Public plotCount As Integer = 3
        Public plotColors() As cv.Scalar = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, white}
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
        Public Sub New()
            desc = "Plot an input variable over time"
            taskAlg.gOptions.LineWidth.Value = 1
            taskAlg.gOptions.DotSizeSlider.Value = 2
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Const plotSeriesCount = 100
            lastXdelta.Add(plotData)

            If columnIndex + taskAlg.DotSize >= dst2.Width Then
                dst2.ColRange(columnIndex, dst2.Width).SetTo(backColor)
                columnIndex = 1
            End If
            dst2.ColRange(columnIndex, columnIndex + taskAlg.DotSize).SetTo(backColor)
            If standaloneTest() Then plotData = taskAlg.color.Mean()

            For i = 0 To plotCount - 1
                If Math.Floor(plotData(i)) < minScale Or Math.Ceiling(plotData(i)) > maxScale Then
                    offChartCount += 1
                    Exit For
                End If
            Next

            If fixedScale = False Then
                ' if enough points are off the charted area or if manually requested, then redo the scale.
                If (offChartCount > plotTriggerRescale And lastXdelta.Count >= plotSeriesCount And controlScale = False) Then
                    If Not taskAlg.firstPass Then
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

            If taskAlg.heartBeat Then
                dst2.Line(New cv.Point(columnIndex, 0), New cv.Point(columnIndex, dst2.Height), white, taskAlg.lineWidth)
            End If

            For i = 0 To plotCount - 1
                If plotData(i) <> 0 Then
                    Dim y = 1 - (plotData(i) - minScale) / (maxScale - minScale)
                    y *= dst2.Height - 1
                    Dim c As New cv.Point(columnIndex - taskAlg.DotSize, y - taskAlg.DotSize)
                    If c.X < 1 Then c.X = 1
                    DrawCircle(dst2, c, taskAlg.DotSize, plotColors(i))
                End If
            Next

            columnIndex += 1
            dst2.Col(columnIndex).SetTo(0)
            labels(2) = "Blue = " + Format(plotData(0), fmt1) + " Green = " + Format(plotData(1), fmt1) +
                    " Red = " + Format(plotData(2), fmt1) + " Yellow = " + Format(plotData(3), fmt1)
            strOut = "Blue = " + Format(plotData(0), fmt1) + vbCrLf
            strOut += "Green = " + Format(plotData(1), fmt1) + vbCrLf
            strOut += "Red = " + Format(plotData(2), fmt1) + vbCrLf
            strOut += "White = " + Format(plotData(3), fmt1) + vbCrLf
            SetTrueText(strOut, 3)
            Dim lineCount = CInt(maxScale - minScale - 1)
            If lineCount > 3 Or lineCount < 0 Then lineCount = 3
            If showScale Then AddPlotScale(dst2, minScale, maxScale, lineCount)
        End Sub
    End Class





    Public Class Plot_Beats : Inherits TaskParent
        Dim plot As New Plot_OverTimeFixedScale
        Public Sub New()
            plot.plotCount = 4
            plot.showScale = False
            plot.fixedScale = True
            plot.minScale = 0
            plot.maxScale = 5
            desc = "Plot the beats to validate things are working."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            plot.plotData(0) = If(taskAlg.heartBeat, 1, -1)
            plot.plotData(1) = If(taskAlg.midHeartBeat, 2, -1)
            plot.plotData(2) = If(taskAlg.quarterBeat, 3, -1)
            plot.plotData(3) = If(taskAlg.almostHeartBeat, 4, -1)
            plot.Run(src)
            dst2 = plot.dst2

            strOut = "taskAlg.heartBeat (blue) = " + CStr(plot.plotData(0)) + vbCrLf
            strOut += "taskAlg.midHeartBeat (green) = " + CStr(plot.plotData(1)) + vbCrLf
            strOut += "taskAlg.quarterBeat (red) = " + CStr(plot.plotData(2)) + vbCrLf
            strOut += "taskAlg.almostHeartBeat (white) = " + CStr(plot.plotData(3)) + vbCrLf
            SetTrueText(strOut, 3)
        End Sub
    End Class





    ' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
    Public Class Plot_Basics_CPP : Inherits TaskParent
        Public srcX As New List(Of Double)
        Public srcY As New List(Of Double)
        Public Sub New()
            For i = 0 To CInt(taskAlg.MaxZmeters) ' something to plot if standaloneTest().
                srcX.Add(i)
                srcY.Add(i * i * i)
            Next
            cPtr = PlotOpenCV_Open()
            desc = "Demo the use of the integrated 2D plot available in OpenCV (only accessible in C++)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim handleX = GCHandle.Alloc(srcX.ToArray, GCHandleType.Pinned)
            Dim handleY = GCHandle.Alloc(srcY.ToArray, GCHandleType.Pinned)

            Dim imagePtr = PlotOpenCV_Run(cPtr, handleX.AddrOfPinnedObject, handleY.AddrOfPinnedObject, srcX.Count,
                                      dst2.Rows, dst2.Cols)

            dst2 = cv.Mat.FromPixelData(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr)
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
    Public Class Plot_Dots : Inherits TaskParent
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim maxX = srcX.Max, minX = srcX.Min, maxY = srcY.Max, minY = srcY.Min
            If wipeSlate Then dst2.SetTo(0)
            For i = 0 To srcX.Count - 1
                Dim pt = New cv.Point(dst2.Width * srcX(i) / maxX, dst2.Height - dst2.Height * srcY(i) / maxY)
                DrawCircle(dst2, pt, taskAlg.DotSize, plotColor)
            Next
            labels(2) = "x-Axis: " + CStr(minX) + " to " + CStr(maxX) + ", y-axis: " + CStr(minY) + " to " + CStr(maxY)
        End Sub
    End Class






    Public Class Plot_Histogram : Inherits TaskParent
        Public histogram As New cv.Mat
        Public histArray() As Single
        Public minRange As Single
        Public maxRange As Single
        Public ranges() As cv.Rangef
        Public backColor As cv.Scalar = cv.Scalar.Red
        Public plotCenter As Single
        Public barWidth As Single
        Public addLabels As Boolean = True
        Public removeZeroEntry As Boolean = True
        Public createHistogram As Boolean = False
        Public histMask As New cv.Mat
        Public mm As mmData
        Public Sub New()
            desc = "Plot histogram data with a stable scale at the left of the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim min = minRange
            Dim max = maxRange
            If standaloneTest() Or createHistogram Then
                If src.Channels() <> 1 Then src = taskAlg.grayStable.Clone
                If minRange = 0 And maxRange = 0 Then
                    Dim mm = GetMinMax(src)
                    min = mm.minVal
                    max = mm.maxVal
                    If min = 0 And max = 0 Then
                        If src.Type = cv.MatType.CV_32F Then
                            max = taskAlg.MaxZmeters
                        Else
                            max = 255
                        End If
                    End If
                End If
                If Single.IsNaN(min) Or Single.IsInfinity(min) Then min = Single.MinValue
                If Single.IsNaN(max) Or Single.IsInfinity(max) Then max = Single.MaxValue
                ranges = {New cv.Rangef(min, max)}
                cv.Cv2.CalcHist({src}, {0}, histMask, histogram, 1, {taskAlg.histogramBins}, ranges)
            Else
                histogram = src
            End If

            If histogram Is Nothing Then
                createHistogram = True
                SetTrueText("The histogram is empty.", 3)
                Exit Sub
            End If

            If removeZeroEntry Then histogram.Set(Of Single)(0, 0, 0) ' let's not plot the values at zero...i.e. Depth at 0, for instance, needs to be removed.
            ReDim histArray(histogram.Rows - 1)
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            dst2.SetTo(backColor)
            barWidth = dst2.Width / histogram.Rows
            plotCenter = barWidth * histogram.Rows / 2 + barWidth / 2

            mm = GetMinMax(histogram)

            If Math.Abs(mm.maxVal - mm.minVal) > 0 And histogram.Rows > 0 Then
                Dim incr = 255 / histogram.Rows
                For i = 0 To histArray.Count - 1
                    If Single.IsNaN(histArray(i)) Then histArray(i) = 0
                    If histArray(i) > 0 Then
                        Dim h = histArray(i) * dst2.Height \ mm.maxVal
                        Dim sIncr = (i Mod 256) * incr
                        Dim color = New cv.Scalar(sIncr, sIncr, sIncr)
                        If histogram.Rows > 255 Then color = cv.Scalar.Black
                        cv.Cv2.Rectangle(dst2, New cv.Rect(i * barWidth, dst2.Height - h, Math.Max(1, barWidth), h), color, -1)
                    End If
                Next
                If addLabels Then AddPlotScale(dst2, mm.minVal, mm.maxVal)
            End If
            If taskAlg.heartBeat Then labels(2) = CStr(CInt(mm.maxVal)) + " max value " + CStr(CInt(mm.minVal)) + " min value"
        End Sub
    End Class




    Public Class Plot_Points : Inherits TaskParent
        Public input As New List(Of cv.Point2d)
        Public output As New List(Of cv.Point)
        Public minX As Double = 0, maxX As Double = dst2.Width
        Public minY As Double = -taskAlg.xRange, maxY As Double = taskAlg.xRange
        Public Sub New()
            For i = 0 To 50 ' something to plot if standaloneTest().
                input.Add(New cv.Point2d(i, i * i * i))
            Next
            desc = "Plot the requested points..."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2.SetTo(0)
            output.Clear()
            For i = 0 To input.Count - 1
                Dim y = input(i).Y
                If Single.IsNaN(y) Then y = 0
                Dim pt = New cv.Point(dst2.Width * (input(i).X - minX) \ (maxX - minX),
                                  dst2.Height - dst2.Height * (y - minY) \ (maxY - minY))
                If pt.Y <> dst2.Height / 2 Then
                    DrawCircle(dst2, pt, taskAlg.DotSize, taskAlg.highlight)
                    output.Add(pt)
                Else
                    output.Add(newPoint)
                End If
            Next

            labels(2) = "x-Axis: " + CStr(minX) + " to " + CStr(maxX) + ", y-axis: " + CStr(minY) + " to " + CStr(maxY)
        End Sub
    End Class
End Namespace