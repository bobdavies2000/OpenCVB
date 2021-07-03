Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Public Class Plot_Basics : Inherits VBparent
    Dim plot As New Plot_Basics_CPP
    Dim hist As New Histogram_Graph
    Public plotCount As Integer = 3
    Public Sub New()
        hist.plotRequested = True
        labels(2) = "Plot of grayscale histogram"
        labels(3) = "Same Data but using OpenCV C++ plot"
        task.desc = "Plot data provided in src Mat"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hist.plotColors(0) = cv.Scalar.White
        hist.RunClass(src)
        dst2 = hist.dst2

        ReDim plot.srcX(hist.histRaw(0).Rows - 1)
        ReDim plot.srcY(hist.histRaw(0).Rows - 1)
        For i = 0 To plot.srcX.Length - 1
            plot.srcX(i) = i
            plot.srcY(i) = hist.histRaw(0).Get(Of Single)(i, 0)
        Next
        plot.RunClass(Nothing)
        dst3 = plot.dst2
        labels(2) = hist.labels(2)
    End Sub
End Class




' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class Plot_Basics_CPP : Inherits VBparent
    Public srcX() As Double
    Public srcY() As Double
    Public Sub New()
        task.desc = "Demo the use of the integrated 2D plot available in OpenCV (only accessible in C++)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateName = caller Then
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






Public Class Plot_OverTime : Inherits VBparent
    Public plotData As cv.Scalar
    Public plotCount As Integer = 3
    Public plotColors() As cv.Scalar = {cv.Scalar.Blue, cv.Scalar.LawnGreen, cv.Scalar.Red, cv.Scalar.White}
    Public backColor = cv.Scalar.LightGray
    Public minScale As Integer = 50
    Public maxScale As Integer = 200
    Public plotTriggerRescale = 50
    Public columnIndex As Integer
    Public offChartCount As Integer
    Public lastXdelta As New List(Of cv.Scalar)
    Public topBottomPad As Integer
    Public controlScale As Boolean ' Use this to programmatically control the scale (rather than let the automated way below keep the scale.)
    Dim myStopWatch As Stopwatch
    Public Sub New()
        If check.Setup(caller, 1) Then
            check.Box(0).Text = "Reset the plot scale"
            check.Box(0).Checked = True
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Plot Pixel Height", 1, 40, 4)
            sliders.setupTrackBar(1, "Plot Pixel Width", 1, 40, 4)
        End If
        task.desc = "Plot an input variable over time"
        myStopWatch = Stopwatch.StartNew()
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static widthSlider = findSlider("Plot Pixel Width")
        Static heightSlider = findSlider("Plot Pixel Height")
        Static resetCheck = findCheckBox("Reset the plot scale")
        Const plotSeriesCount = 100
        lastXdelta.Add(plotData)

        Dim pixelHeight = CInt(heightSlider.Value)
        Dim pixelWidth = CInt(widthSlider.Value)

        If task.frameCount = 0 Then dst2.SetTo(0)
        If columnIndex + pixelWidth >= dst2.Width Then
            dst2.ColRange(columnIndex, dst2.Width).SetTo(backColor)
            columnIndex = 0
        End If
        dst2.ColRange(columnIndex, columnIndex + pixelWidth).SetTo(backColor)
        If standalone Or task.intermediateName = caller Then plotData = task.color.Mean()

        For i = 0 To plotCount - 1
            If Math.Floor(plotData.Item(i)) < minScale Or Math.Ceiling(plotData.Item(i)) > maxScale Then
                offChartCount += 1
                Exit For
            End If
        Next

        ' if enough points are off the charted area or if manually requested, then redo the scale.
        If (offChartCount > plotTriggerRescale And lastXdelta.Count >= plotSeriesCount And controlScale = False) Or resetCheck.Checked Then
            resetCheck.Checked = False
            dst2.SetTo(0)
            maxScale = Integer.MinValue
            minScale = Integer.MaxValue
            For i = 0 To lastXdelta.Count - 1
                Dim nextVal = lastXdelta.Item(i)
                For j = 0 To plotCount - 1
                    If Math.Floor(nextVal.Item(j)) < minScale Then minScale = Math.Floor(nextVal.Item(j))
                    If Math.Floor(nextVal.Item(j)) > maxScale Then maxScale = Math.Ceiling(nextVal.Item(j))
                Next
            Next
            If minScale < 5 And minScale > 0 Then minScale = 0 ' nice location...
            minScale -= topBottomPad
            maxScale += topBottomPad

            lastXdelta.Clear()
            offChartCount = 0
            columnIndex = 0 ' restart at the left side of the chart
        End If
        If lastXdelta.Count >= plotSeriesCount Then lastXdelta.RemoveAt(0)

        Dim rectSize = New cv.Size2f(pixelWidth, pixelHeight)
        Dim ellipseSize = New cv.Size(pixelWidth, pixelHeight * 2)
        For i = 0 To plotCount - 1
            Dim y = 1 - (plotData.Item(i) - minScale) / (maxScale - minScale)
            y *= dst2.Height - 1
            Dim c As New cv.Point(columnIndex - pixelWidth, y - pixelHeight)
            Dim rect = New cv.Rect(c.X, c.Y, pixelWidth * 2, pixelHeight * 2)
            Select Case i
                Case 0
                    dst2.Circle(c, pixelWidth, plotColors(i), -1, task.lineType)
                Case 1
                    dst2.Rectangle(rect, plotColors(i), -1)
                Case 2
                    dst2.Ellipse(c, ellipseSize, 0, 0, 360, plotColors(i), -1)
                Case 3
                    Dim rotatedRect = New cv.RotatedRect(c, rectSize, 45)
                    drawRotatedRectangle(rotatedRect, dst3, plotColors(i))
            End Select
        Next

        Static lastSeconds As Double
        Dim nextWatchVal = myStopWatch.ElapsedMilliseconds
        If nextWatchVal - lastSeconds > 1000 Then
            lastSeconds = nextWatchVal
            dst2.Line(New cv.Point(columnIndex, 0), New cv.Point(columnIndex, dst2.Height), cv.Scalar.White, 1)
        End If

        columnIndex += pixelWidth
        dst2.Col(columnIndex).SetTo(0)
        If standalone Or task.intermediateName = caller Then labels(2) = "RGB Means: blue = " + Format(plotData.Item(0), "#0.0") + " green = " + Format(plotData.Item(1), "#0.0") + " red = " + Format(plotData.Item(2), "#0.0")
        AddPlotScale(dst2, minScale - topBottomPad, maxScale + topBottomPad, task.fontSize * 2)
    End Sub
End Class





Public Class Plot_Histogram : Inherits VBparent
    Public hist As New cv.Mat
    Public minRange As Integer = 0
    Public maxRange As Integer = 255
    Public backColor As cv.Scalar = cv.Scalar.Red
    Public plotMaxValue As Integer
    Public Sub New()
        task.desc = "Plot histogram data with a stable scale at the left of the image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or task.intermediateName = caller Then
            Dim gray = If(src.Channels = 1, src, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            Dim dimensions() = New Integer() {task.histogramBins}
            Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
            cv.Cv2.CalcHist(New cv.Mat() {gray}, New Integer() {0}, New cv.Mat(), hist, 1, dimensions, ranges)
        End If
        dst2.SetTo(backColor)
        Dim barWidth = dst2.Width / hist.Rows
        hist.MinMaxLoc(minVal, maxVal)

        If plotMaxValue = 0 Then
            Static savedMaxVal = maxVal
            maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000
            If maxVal < 0 Then maxVal = savedMaxVal
            If Math.Abs((maxVal - savedMaxVal)) / maxVal < 0.5 Then maxVal = savedMaxVal Else savedMaxVal = maxVal
        Else
            maxVal = plotMaxValue
        End If

        If maxVal > 0 And hist.Rows > 0 Then
            Dim incr = CInt(255 / hist.Rows)
            For i = 0 To hist.Rows - 1
                Dim offset = hist.Get(Of Single)(i)
                If Single.IsNaN(offset) Then offset = 0
                Dim h = CInt(offset * dst2.Height / maxVal)
                Dim color As cv.Scalar = cv.Scalar.Black
                If hist.Rows <= 255 Then color = cv.Scalar.All((i Mod 255) * incr)
                cv.Cv2.Rectangle(dst2, New cv.Rect(i * barWidth, dst2.Height - h, barWidth, h), color, -1)
            Next
            AddPlotScale(dst2, 0, maxVal, task.fontSize * 2)
        End If
    End Sub
End Class





Module Plot_OpenCV_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Plot_OpenCVBasics(inX As IntPtr, inY As IntPtr, inLen As Integer, dstptr As IntPtr, rows As Integer, cols As Integer)
    End Sub

    Public Sub AddPlotScale(dst2 As cv.Mat, minVal As Double, maxVal As Double, fontSize As Double)
        ' draw a scale along the side
        Dim spacer = CInt(dst2.Height / 4)
        Dim spaceVal = CInt((maxVal - minVal) / 4)
        If spaceVal < 1 Then spaceVal = 1
        For i = 0 To 4 - 1
            Dim pt1 = New cv.Point(0, spacer * i)
            Dim pt2 = New cv.Point(dst2.Width, spacer * i)
            dst2.Line(pt1, pt2, cv.Scalar.White, 1)
            If i = 0 Then pt2.Y += 10
            Dim nextVal = (maxVal - spaceVal * i)
            If maxVal > 1000 Then
                cv.Cv2.PutText(dst2, Format(nextVal / 1000, "###,###,##0.0") + "k", New cv.Point(pt1.X + 5, pt1.Y - 4),
                           cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.Beige, 2)
            Else
                cv.Cv2.PutText(dst2, Format(nextVal, "##0"), New cv.Point(pt1.X + 5, pt1.Y - 4),
                           cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.Beige, 2)
            End If
        Next
    End Sub
End Module






Public Class Plot_Depth : Inherits VBparent
    Dim plotDepth As New Plot_Basics_CPP
    Dim hist As New Histogram_Depth
    Public Sub New()
        task.desc = "Show depth using OpenCV's plot format with variable bins."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        hist.RunClass(src)
        ReDim plotDepth.srcX(hist.plotHist.hist.Rows - 1)
        ReDim plotDepth.srcY(hist.plotHist.hist.Rows - 1)
        For i = 0 To plotDepth.srcX.Length - 1
            plotDepth.srcX(i) = i * (task.maxDepth - task.minDepth) / plotDepth.srcX.Length
            plotDepth.srcY(i) = hist.plotHist.hist.Get(Of Single)(i, 0)
        Next
        plotDepth.RunClass(Nothing)
        dst2 = plotDepth.dst2

        labels(2) = plotDepth.labels(2)
        Dim Split = Regex.Split(labels(2), "\W+")
        Dim lineCount = CInt(Split(4) / 1000)
        If lineCount > 0 Then
            Dim meterDepth = CInt(src.Width / lineCount)
            For i = 1 To lineCount
                Dim x = i * meterDepth
                dst2.Line(New cv.Point(x, 0), New cv.Point(x, src.Height), cv.Scalar.White, task.lineWidth)
                cv.Cv2.PutText(dst2, Format(i, "0") + "m", New cv.Point(x + 5, src.Height - 10), cv.HersheyFonts.HersheyComplexSmall, 0.7, cv.Scalar.White, 2)
            Next
        End If
    End Sub
End Class


