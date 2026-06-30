Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Public Class PlotOpenCV_Basics : Inherits TaskParent
    Dim plot As New PlotOpenCV_CPP
    Dim hist As New Histogram_Graph
    Public plotCount As Integer = 3
    Public Sub New()
        hist.plotRequested = True
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







Public Class PlotOpenCV_Depth : Inherits TaskParent
    Dim plotDepth As New PlotOpenCV_CPP
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
                dst2.Line(New cv.Point(x, 0), New cv.Point(x, src.Height), white, task.lineWidth, task.lineType)
                SetTrueText(Format(i, "0") + "m", New cv.Point(x + 4, src.Height - 10))
            Next
        End If
    End Sub
End Class






' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class PlotOpenCV_CPP : Inherits TaskParent
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




Public Class PlotOpenCV_Points : Inherits TaskParent
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
                dst2.Circle(pt, task.DotSize, task.highlight, -1, task.lineType)
                output.Add(pt)
            Else
                output.Add(newPoint)
            End If
        Next

        labels(2) = "x-Axis: " + Format(minX, fmt2) + " to " + Format(maxX, fmt2) +
                          ", y-axis: " + Format(minY, fmt2) + " to " + Format(maxY, fmt2)
    End Sub
End Class


