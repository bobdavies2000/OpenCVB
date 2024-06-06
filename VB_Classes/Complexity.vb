Imports cv = OpenCvSharp
Imports System.IO
' https://www.codeproject.com/Articles/5373108/Understanding-Time-Complexity-on-Simple-Examples
Public Class Complexity_Basics : Inherits VB_Parent
    Dim complex As New Complexity_Dots
    Public Sub New()
        desc = "Plot all the available complexity runs."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        complex.options.RunVB()

        Dim saveLatestFile = complex.options.filename.FullName

        complex.maxTime = 0
        For i = 0 To complex.options.filenames.Count - 1
            complex.fileName = complex.options.filenames(i)
            complex.Run(src)
        Next

        complex.initialize = True
        For i = 0 To complex.options.filenames.Count - 1
            complex.fileName = complex.options.filenames(i)
            complex.plotColor = complex.options.setPlotColor()
            complex.Run(src)
            complex.initialize = False
        Next

        dst3 = complex.dst2.Clone

        setTrueText(">>>>>> Increasing input data >>>>>>" + vbCrLf + "All available complexity runs",
                    New cv.Point(dst2.Width / 4, 10), 3)
        setTrueText(" TIME " + "(Max = " + Format(complex.maxTime, fmt0) + ")", New cv.Point(0, dst2.Height / 2), 3)

        complex.initialize = True
        complex.fileName = saveLatestFile
        complex.plotColor = complex.options.setPlotColor()
        complex.Run(src)
        dst2 = complex.dst2

        setTrueText(" >>>>>> Increasing input data >>>>>>" + vbCrLf + complex.options.filename.Name,
                    New cv.Point(dst2.Width / 4, 10))
        setTrueText(" TIME " + "(Max = " + Format(complex.maxTime, fmt0) + ")", New cv.Point(0, dst2.Height / 2))
        labels(2) = complex.labels(2)
        labels(3) = "Plots For all available complexity runs"
    End Sub
End Class






' https://www.codeproject.com/Articles/5373108/Understanding-Time-Complexity-on-Simple-Examples
Public Class Complexity_PlotOpenCV : Inherits VB_Parent
    Public plot As New Plot_Basics_CPP
    Public maxFrameCount As Integer
    Public sortData As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public options As New Options_Complexity
    Public sessionTime As Single
    Public Sub New()
        desc = "Plot the algorithm's input data rate (X) vs. time to complete work on that input (Y)."
    End Sub
    Public Sub prepareSortedData(filename As String)
        Dim contents = My.Computer.FileSystem.ReadAllText(filename)
        Dim lines = contents.Split(vbCrLf)
        Dim split() As String, nextSize As Integer, myFrameCount As Integer
        Dim times As New List(Of Single)
        sortData.Clear()
        For Each line In lines
            line = line.Trim()
            If line.StartsWith("Image") Then
                split = line.Split(vbTab)
                nextSize = split(2) * split(1)
            ElseIf line.StartsWith("Ending") Then
                split = line.Split(vbTab)
                myFrameCount = split(1)
                If myFrameCount > maxFrameCount Then maxFrameCount = myFrameCount
                split = split(2).Split()
                times.Add(split(0))
            End If
            If line.StartsWith("-") And nextSize > 0 Then
                sortData.Add(nextSize, myFrameCount)
            End If
        Next

        sessionTime = times.Average
    End Sub
    Public Function plotData(ByVal maxTime As Single) As Single
        For Each el In sortData
            plot.srcX.Add(el.Key)
            Dim nextTime = sessionTime * maxFrameCount / el.Value
            plot.srcY.Add(nextTime)
            If nextTime > maxTime Then maxTime = nextTime
        Next
        plot.Run(empty)
        dst2 = plot.dst2.Clone
        Return maxTime
    End Function
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        maxFrameCount = 0
        plot.srcX.Clear()
        plot.srcY.Clear()
        prepareSortedData(options.filename.FullName)

        Dim maxTime = plotData(0)

        setTrueText(">>>>>> Increasing input data >>>>>>", New cv.Point(dst2.Width / 4, 10))
        setTrueText(" TIME", New cv.Point(0, dst2.Height / 2))
        setTrueText("Max Time = " + Format(maxTime, fmt0), New cv.Point(10, 10))
        labels(2) = "Complexity plot for " + options.filename.Name.Substring(0, Len(options.filename.Name) - 4)
    End Sub
End Class






' https://www.codeproject.com/Articles/5373108/Understanding-Time-Complexity-on-Simple-Examples
Public Class Complexity_Dots : Inherits VB_Parent
    Public options As New Options_Complexity
    Public initialize As Boolean = True, maxTime As Single, fileName As String
    Public plotColor As cv.Scalar
    Public Sub New()
        desc = "Plot the results of multiple runs at various resolutions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If fileName <> "" Then options.filename = New FileInfo(fileName)
        Dim contents = My.Computer.FileSystem.ReadAllText(options.filename.FullName)
        Dim lines = contents.Split(vbCrLf)

        Dim sortData As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        Dim split() As String, nextSize As Integer, myFrameCount As Integer
        Dim times As New List(Of Single)
        Dim maxFrameCount As Single, srcX As New List(Of Double), srcY As New List(Of Double)
        For Each line In lines
            line = line.Trim()
            If line.StartsWith("Image") Then
                split = line.Split(vbTab)
                nextSize = split(2) * split(1)
            ElseIf line.StartsWith("Ending") Then
                split = line.Split(vbTab)
                myFrameCount = split(1)
                If myFrameCount > maxFrameCount Then maxFrameCount = myFrameCount
                split = split(2).Split()
                times.Add(split(0))
            End If
            If line.StartsWith("-") And nextSize > 0 Then
                If srcX.Contains(nextSize) Then
                    Dim index = srcX.IndexOf(nextSize)
                    srcY(index) = (myFrameCount + srcY(index)) / 2
                Else
                    srcX.Add(nextSize)
                    srcY.Add(myFrameCount)
                End If
            End If
        Next

        Dim sessionTime = times.Average

        For i = 0 To srcX.Count - 1
            Dim nextTime = sessionTime * maxFrameCount / srcY(i)
            If maxTime < nextTime Then maxTime = nextTime
            sortData.Add(srcX(i), nextTime)
        Next

        Dim maxX = srcX.Max
        Dim pointSet As New List(Of cv.Point)
        Static dst As New cv.Mat(New cv.Size(task.lowRes.Width * 2, task.lowRes.Height * 2), cv.MatType.CV_8UC3, 0)
        If initialize Then dst.SetTo(0)
        For i = 0 To sortData.Count - 1
            Dim pt = New cv.Point(dst.Width * sortData.ElementAt(i).Key / maxX,
                                  dst.Height - dst.Height * sortData.ElementAt(i).Value / maxTime)
            drawCircle(dst, pt, task.dotSize, plotColor)
            pointSet.Add(pt)
        Next

        For i = 1 To pointSet.Count - 1
            drawLine(dst, pointSet(i - 1), pointSet(i), plotColor)
        Next

        setTrueText(">>>>>> Increasing input data >>>>>>" + vbCrLf + options.filename.Name,
                    New cv.Point(dst2.Width / 4, 10))
        setTrueText(" TIME " + "(Max = " + Format(maxTime, fmt0) + ")", New cv.Point(0, dst2.Height / 2))
        labels(2) = "Complexity plot for " + options.filename.Name.Substring(0, Len(options.filename.Name) - 4)
        dst2 = dst.Resize(dst2.Size)
    End Sub
End Class