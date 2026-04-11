Imports cv = OpenCvSharp
Public Class PlotTime_Basics : Inherits TaskParent
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
        Select Case task.workRes.Width
            Case 1920
                task.gOptions.LineWidth.Value = 10
            Case 1280
                task.gOptions.LineWidth.Value = 7
            Case 640
                task.gOptions.LineWidth.Value = 4
            Case 320
                task.gOptions.LineWidth.Value = 2
            Case Else
                task.gOptions.LineWidth.Value = 1
        End Select
        task.gOptions.DotSizeSlider.Value = task.gOptions.LineWidth.Value
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Const plotSeriesCount = 100
        lastXdelta.Add(plotData)

        If columnIndex + task.DotSize >= dst2.Width Then
            dst2.ColRange(columnIndex, dst2.Width).SetTo(backColor)
            columnIndex = 1
        End If
        dst2.ColRange(columnIndex, columnIndex + task.DotSize).SetTo(backColor)
        If standaloneTest() Then plotData = task.color.Mean()

        For i = 0 To plotCount - 1
            If Math.Floor(plotData(i)) < minScale Or Math.Ceiling(plotData(i)) > maxScale Then
                offChartCount += 1
                Exit For
            End If
        Next

        ' if enough points are off the charted area or if manually requested, then redo the scale.
        If (offChartCount > plotTriggerRescale And lastXdelta.Count >= plotSeriesCount And controlScale = False) Then
            If Not task.firstPass Then
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
            y *= (dst2.Height - 1)
            Dim c As New cv.Point(columnIndex - task.DotSize, y - task.DotSize)
            If c.X < 1 Then c.X = 1
            DrawCircle(dst2, c, task.DotSize, plotColors(i))
        Next

        If task.heartBeat Then
            dst2.Line(New cv.Point(columnIndex, 0), New cv.Point(columnIndex, dst2.Height), white, 1)
        End If

        columnIndex += task.DotSize
        dst2.Col(columnIndex).SetTo(0)
        If standaloneTest() Then labels(2) = "RGB Means: blue = " + Format(plotData(0), fmt1) + " green = " + Format(plotData(1), fmt1) + " red = " + Format(plotData(2), fmt1)
        Dim lineCount = CInt(maxScale - minScale - 1)
        If lineCount > 3 Or lineCount < 0 Then lineCount = 3
        PlotOpenCV_Basics.AddPlotScale(dst2, minScale, maxScale, lineCount)
    End Sub
End Class






Public Class PlotTime_Single : Inherits TaskParent
    Public plotData As Single
    Public backColor = cv.Scalar.DarkGray
    Public max As Single, min As Single, avg, fmt As String
    Public useFixedRange As Boolean
    Public plotColor = cv.Scalar.Blue
    Dim inputList As New List(Of Single)
    Public Sub New()
        labels(2) = "PlotTime_Basics "
        desc = "Plot an input variable over time"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then plotData = task.color.Mean(task.depthmask)(0)

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
            dst2.Circle(c, task.DotSize, blue, -1, task.lineType)
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
                cv.Cv2.PutText(dst2, nextText, pt, cv.HersheyFonts.HersheyPlain, 0.7, white, 1, task.lineType)
            Next
        End If

        Dim p1 = New cv.Point(0, dst2.Height / 2)
        Dim p2 = New cv.Point(dst2.Width, dst2.Height / 2)
        dst2.Line(p1, p2, white, task.cvFontThickness)
        If standaloneTest() Then SetTrueText("standaloneTest() test is with the blue channel mean of the color image.", 3)
    End Sub
End Class








Public Class PlotTime_Scalar : Inherits TaskParent
    Public plotData As cv.Scalar
    Public plotCount As Integer = 3
    Public plotList As New List(Of PlotTime_Single)
    Dim mats As New Mat_4Click
    Public Sub New()
        For i = 0 To 3
            plotList.Add(New PlotTime_Single)
            plotList(i).plotColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.Yellow)
        Next
        desc = "Plot the requested number of entries in the cv.scalar input"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then plotData = task.color.Mean()

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






Public Class PlotTime_FixedScale : Inherits TaskParent
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
        task.gOptions.LineWidth.Value = 1
        task.gOptions.DotSizeSlider.Value = 2
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Const plotSeriesCount = 100
        lastXdelta.Add(plotData)

        If columnIndex + task.DotSize >= dst2.Width Then
            dst2.ColRange(columnIndex, dst2.Width).SetTo(backColor)
            columnIndex = 1
        End If
        dst2.ColRange(columnIndex, columnIndex + task.DotSize).SetTo(backColor)
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
                If Not task.firstPass Then
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
            dst2.Line(New cv.Point(columnIndex, 0), New cv.Point(columnIndex, dst2.Height), white, task.lineWidth)
        End If

        For i = 0 To plotCount - 1
            If plotData(i) <> 0 Then
                Dim y = 1 - (plotData(i) - minScale) / (maxScale - minScale)
                y *= dst2.Height - 1
                Dim c As New cv.Point(columnIndex - task.DotSize, y - task.DotSize)
                If c.X < 1 Then c.X = 1
                DrawCircle(dst2, c, task.DotSize, plotColors(i))
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
        If showScale Then PlotOpenCV_Basics.AddPlotScale(dst2, minScale, maxScale, lineCount)
    End Sub
End Class