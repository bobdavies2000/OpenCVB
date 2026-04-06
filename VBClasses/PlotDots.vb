Imports cv = OpenCvSharp
' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class PlotDots_Basics : Inherits TaskParent
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









Public Class PlotDots_Heartbeats : Inherits TaskParent
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