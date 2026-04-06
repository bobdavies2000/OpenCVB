Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Public Class Plot_Basics : Inherits TaskParent
    Dim plot As New Plot_Basics_CPP
    Dim hist As New Histogram_Graph
    Public plotCount As Integer = 3
    Public Sub New()
        hist.plotRequested = True
        labels(3) = "Same Data but using OpenCV C++ plot"
        desc = "Plot data provided in src Mat"
    End Sub
    Public Shared Sub AddPlotScale(dst As cv.Mat, minVal As Double, maxVal As Double, Optional lineCount As Integer = 3)
        Dim spacer = dst.Height \ (lineCount + 1)
        Dim spaceVal = (maxVal - minVal) \ (lineCount + 1)
        If lineCount > 1 Then If spaceVal < 1 Then spaceVal = 1
        If spaceVal > 10 Then spaceVal += spaceVal Mod 10
        For i = 0 To lineCount
            Dim p1 = New cv.Point(0, spacer * i)
            Dim p2 = New cv.Point(dst.Width, spacer * i)
            dst.Line(p1, p2, white, task.cvFontThickness)
            Dim nextVal = (maxVal - spaceVal * i)
            Dim nextText = If(maxVal > 1000, Format(nextVal / 1000, "###,##0.0") + "k", Format(nextVal, fmt2))
            Dim p3 = New cv.Point(0, p1.Y + 12)
            cv.Cv2.PutText(dst, nextText, p3, cv.HersheyFonts.HersheyPlain, task.cvFontSize,
                                white, task.cvFontThickness, task.lineType)
        Next
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
    Dim hist As New Histogram_Basics
    Public Sub New()
        desc = "Show depth using OpenCV's plot format with variable bins."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        'src.SetTo(task.MaxZmeters, task.maxDepthMask)

        hist.Run(src)
        plotDepth.srcX.Clear()
        plotDepth.srcY.Clear()
        For i = 0 To task.histogramBins - 1
            plotDepth.srcX.Add(i * task.MaxZmeters / task.histogramBins)
            plotDepth.srcY.Add(hist.histogram.Get(Of Single)(i, 0))
        Next
        plotDepth.Run(src)
        dst2 = plotDepth.dst2

        labels(2) = plotDepth.labels(2)
        Dim Split = Regex.Split(labels(2), "\W+")
        Dim lineCount = CInt(Split(5))
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





Public Class NR_Plot_Beats : Inherits TaskParent
    Dim plot As New PlotTime_FixedScale
    Public Sub New()
        plot.plotCount = 4
        plot.showScale = False
        plot.fixedScale = True
        plot.minScale = 0
        plot.maxScale = 5
        desc = "Plot the beats to validate things are working."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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
        SetTrueText(strOut, 3)
    End Sub
End Class





' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class Plot_Basics_CPP : Inherits TaskParent
    Implements IDisposable
    Public srcX As New List(Of Double)
    Public srcY As New List(Of Double)
    Public Sub New()
        For i = 0 To CInt(task.MaxZmeters) ' something to plot if standaloneTest().
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
        labels(2) = "x-Axis: " + Format(minX, fmt2) + " to " + Format(maxX, fmt2) +
                          ", y-axis: " + Format(minY, fmt2) + " to " + Format(maxY, fmt2)
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = PlotOpenCV_Close(cPtr)
    End Sub
End Class






' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class NR_Plot_Dots : Inherits TaskParent
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
            DrawCircle(dst2, pt, task.DotSize, plotColor)
        Next
        labels(2) = "x-Axis: " + Format(minX, fmt2) + " to " + Format(maxX, fmt2) +
                          ", y-axis: " + Format(minY, fmt2) + " to " + Format(maxY, fmt2)
    End Sub
End Class




Public Class Plot_Points : Inherits TaskParent
    Public input As New List(Of cv.Point2d)
    Public output As New List(Of cv.Point)
    Public minX As Double = 0, maxX As Double = dst2.Width
    Public minY As Double = -task.xRange, maxY As Double = task.xRange
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
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
                output.Add(pt)
            Else
                output.Add(newPoint)
            End If
        Next

        labels(2) = "x-Axis: " + Format(minX, fmt2) + " to " + Format(maxX, fmt2) +
                          ", y-axis: " + Format(minY, fmt2) + " to " + Format(maxY, fmt2)
    End Sub
End Class









Public Class Plot_RedPrepData : Inherits TaskParent
    Dim prep As New RedPrep_Depth
    Dim plot As New PlotBars_Basics
    Public Sub New()
        plot.createHistogram = True
        desc = "Plot the RedCloud prep data to see if any patterns emerge."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        prep.Run(task.pcSplit(2))
        dst2 = prep.dst3
        labels(2) = prep.labels(2) + "  (task.histogramBins = " + CStr(task.histogramBins) + ")"

        plot.minRange = 1
        plot.maxRange = plot.minRange + task.MaxZmeters * 1000
        plot.Run(prep.dst2)
        dst3 = plot.dst2
        labels(3) = "Min/Max values " + Format(plot.mm.minVal / 1000, fmt2) + "/" +
                                                    Format(plot.mm.maxVal / 1000, fmt2) + " meters"
    End Sub
End Class

