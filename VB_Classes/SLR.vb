Imports cv = OpenCvSharp
Imports System.IO
' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
Public Class SLR_Basics : Inherits VBparent
    Public input As New SLR_Data
    Dim slr As New CS_Classes.SLR
    Dim plot As New Plot_Basics_CPP
    Public Sub New()
        If standalone Then
            input.Run(dst2)
            label1 = "Sample data input"
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Approximate accuracy (tolerance) X100", 1, 1000, 30)
            sliders.setupTrackBar(1, "Simple moving average window size", 1, 100, 10)
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
            dst2 = plot.dst2.Clone

            plot.srcX = resultX.ToArray
            plot.srcY = resultY.ToArray
            plot.Run(src)
            dst3 = plot.dst2
        Else
            dst2.SetTo(0)
            dst3.SetTo(0)
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
        dst2 = plot.dst2
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
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.Run(src)
        dst2 = hist.dst2
        For i = 0 To hist.histogram.Rows - 1
            slr.input.dataX.Add(i)
            slr.input.dataY.Add(hist.histogram.Get(Of Single)(i, 0))
        Next
        slr.Run(src)
        dst3 = slr.dst3
    End Sub
End Class









Public Class SLR_TrendCompare : Inherits VBparent
    Public slr As Object = New SLR_Image
    Dim valList As New List(Of Single)
    Dim barMidPoint As Integer
    Dim lastPoint As cv.Point2f
    Public resultingPoints As New List(Of cv.Point2f)
    Public Sub New()
        task.desc = "Find trends by filling in short histogram gaps in the given image's histogram."
    End Sub
    Private Sub connectLine(i As Integer)
        Dim p1 = New cv.Point2f(barMidPoint + dst2.Width * i / valList.Count, dst2.Height - dst2.Height * valList(i) / slr.hist.plotHist.plotMaxValue)
        resultingPoints.Add(p1)
        dst2.Line(lastPoint, p1, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        lastPoint = p1
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        label1 = "Histogram with Yellow line showing the trends"
        slr.hist.plothist.backcolor = cv.Scalar.Red
        slr.Run(src)
        dst2 = slr.dst2
        dst3 = slr.dst3

        Dim indexer = slr.hist.histogram.GetGenericIndexer(Of Single)()
        valList = New List(Of Single)
        For i = 0 To slr.hist.histogram.Rows - 1
            valList.Add(indexer(i))
        Next
        barMidPoint = dst2.Width / valList.Count / 2

        If valList.Count < 2 Then Exit Sub
        slr.hist.plotHist.plotMaxValue = valList.Max
        lastPoint = New cv.Point2f(barMidPoint, dst2.Height - dst2.Height * valList(0) / slr.hist.plotHist.plotMaxValue)
        resultingPoints.Clear()
        resultingPoints.Add(lastPoint)
        For i = 1 To valList.Count - 2
            If valList(i - 1) > valList(i) And valList(i + 1) > valList(i) Then valList(i) = (valList(i - 1) + valList(i + 1)) / 2
            connectLine(i)
        Next
        connectLine(valList.Count - 1)
    End Sub
End Class









Public Class SLR_Trends : Inherits VBparent
    Public hist As New Histogram_Basics
    Dim valList As New List(Of Single)
    Dim barMidPoint As Integer
    Dim lastPoint As cv.Point2f
    Public resultingPoints As New List(Of cv.Point2f)
    Public resultingValues As New List(Of Single)
    Public Sub New()
        task.desc = "Find trends by filling in short histogram gaps in the given image's histogram."
    End Sub
    Private Sub connectLine(i As Integer)
        Dim p1 = New cv.Point2f(barMidPoint + dst2.Width * i / valList.Count, dst2.Height - dst2.Height * valList(i) / hist.plotHist.plotMaxValue)
        resultingPoints.Add(p1)
        resultingValues.Add(p1.Y)
        dst2.Line(lastPoint, p1, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        lastPoint = p1
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        label1 = "Grayscale histogram - yellow line shows trend"
        hist.plotHist.backColor = cv.Scalar.Red
        hist.Run(src)
        dst2 = hist.dst2

        Dim indexer = hist.histogram.GetGenericIndexer(Of Single)()
        valList = New List(Of Single)
        For i = 0 To hist.histogram.Rows - 1
            valList.Add(indexer(i))
        Next
        barMidPoint = dst2.Width / valList.Count / 2

        If valList.Count < 2 Then Exit Sub
        hist.plotHist.plotMaxValue = valList.Max
        lastPoint = New cv.Point2f(barMidPoint, dst2.Height - dst2.Height * valList(0) / hist.plotHist.plotMaxValue)
        resultingPoints.Clear()
        resultingValues.Clear()
        resultingPoints.Add(lastPoint)
        resultingValues.Add(lastPoint.Y)
        For i = 1 To valList.Count - 2
            If valList(i - 1) > valList(i) And valList(i + 1) > valList(i) Then valList(i) = (valList(i - 1) + valList(i + 1)) / 2
            connectLine(i)
        Next
        connectLine(valList.Count - 1)
    End Sub
End Class









Public Class SLR_TrendImages : Inherits VBparent
    Dim trends As New SLR_Trends
    Public Sub New()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 5)
            radio.check(0).Text = "Depth32f input"
            radio.check(1).Text = "Grayscale input"
            radio.check(2).Text = "Blue input"
            radio.check(3).Text = "Green input"
            radio.check(4).Text = "Red input"
            radio.check(1).Checked = True
        End If

        task.desc = "Find trends by filling in short histogram gaps for depth or 1-channel images"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim split = src.Split()
        trends.hist.plotHist.maxRange = 255
        trends.hist.depthNoZero = False ' default is to look at element 0....

        Dim splitIndex = 0
        For i = 0 To radio.check.Count - 1
            If radio.check(0).Checked Then
                trends.hist.plotHist.maxRange = task.maxZ * 1000
                trends.hist.depthNoZero = True ' not interested in the undefined depth areas...
                trends.Run(task.depth32f)
                label1 = "SLR_TrendImages - Depth32f"
                Exit For
            End If
            If radio.check(1).Checked Then
                trends.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
                label1 = "SLR_TrendImages - grayscale"
                Exit For
            End If
            If radio.check(2).Checked Then
                label1 = "SLR_TrendImages - Blue channel"
                splitIndex = 0
            Else
                splitIndex = If(radio.check(3).Checked, 1, 2)
                label1 = "SLR_TrendImages - " + If(radio.check(3).Checked, "Green", "Red") + " channel"
            End If
            trends.Run(split(splitIndex))
        Next

        dst2 = trends.dst2
    End Sub
End Class










Public Class SLR_SurfaceH : Inherits VBparent
    Dim surface As New PointCloud_SurfaceH
    Public Sub New()
        task.desc = "Use the PointCloud_SurfaceH data to indicate valleys and peaks."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        surface.Run(src)
        dst2 = surface.dst3
    End Sub
End Class