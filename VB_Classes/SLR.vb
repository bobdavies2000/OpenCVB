Imports cv = OpenCvSharp
Imports  System.IO
' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
Public Class SLR_Basics : Inherits VB_Algorithm
    Public input As New SLR_Data
    Dim slr As New CS_Classes.SLR
    Dim plot As New Plot_Basics_CPP
    Public Sub New()
        If standalone Then
            input.Run(dst2)
            labels(2) = "Sample data input"
        End If

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Approximate accuracy (tolerance) X100", 1, 1000, 30)
            sliders.setupTrackBar("Simple moving average window size", 1, 100, 10)
        End If
        desc = "Segmented Linear Regression example"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static toleranceSlider = findSlider("Approximate accuracy (tolerance) X100")
        Static movingAvgSlider = findSlider("Simple moving average window size")
        Dim tolerance = toleranceSlider.Value / 100
        Dim halfLength = movingAvgSlider.Value

        Dim resultX As New List(Of Double)
        Dim resultY As New List(Of Double)

        slr.SegmentedRegressionFast(input.dataX, input.dataY, tolerance, halfLength, resultX, resultY)

        labels(2) = "Tolerance = " + CStr(tolerance) + " and moving average window = " + CStr(halfLength)
        If resultX.Count > 0 Then
            plot.srcX = input.dataX
            plot.srcY = input.dataY
            plot.Run(src)
            dst2 = plot.dst2.Clone

            plot.srcX = resultX
            plot.srcY = resultY
            plot.Run(src)
            dst3 = plot.dst2
        Else
            dst2.SetTo(0)
            dst3.SetTo(0)
            setTrueText(labels(2) + " yielded no results...")
        End If
        If standalone = False Then
            input.dataX.Clear()
            input.dataY.Clear()
        End If
    End Sub
End Class






' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
Public Class SLR_Data : Inherits VB_Algorithm
    Dim plot As New Plot_Basics_CPP
    Public dataX As New List(Of Double)
    Public dataY As New List(Of Double)
    Public Sub New()
        Dim sr = New StreamReader(task.homeDir + "/Data/real_data.txt")
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
        desc = "Plot the data used in SLR_Basics"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        plot.srcX = dataX
        plot.srcY = dataY
        plot.Run(src)
        dst2 = plot.dst2
    End Sub
End Class







Public Class SLR_Image : Inherits VB_Algorithm
    Public slr As New SLR_Basics
    Public hist As New Histogram_Basics
    Public Sub New()
        labels(2) = "Original data"
        desc = "Run Segmented Linear Regression on grayscale image data - just an experiment"
    End Sub
    Public Sub RunVB(src as cv.Mat)
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









Public Class SLR_TrendCompare : Inherits VB_Algorithm
    Public slr As Object = New SLR_Image
    Dim valList As New List(Of Single)
    Dim barMidPoint As Integer
    Dim lastPoint As cv.Point2f
    Public resultingPoints As New List(Of cv.Point2f)
    Public Sub New()
        desc = "Find trends by filling in short histogram gaps in the given image's histogram."
    End Sub
    Private Sub connectLine(i As Integer)
        Dim p1 = New cv.Point2f(barMidPoint + dst2.Width * i / valList.Count, dst2.Height - dst2.Height * valList(i) / slr.hist.plot.maxValue)
        resultingPoints.Add(p1)
        dst2.Line(lastPoint, p1, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        lastPoint = p1
    End Sub
    Public Sub RunVB(src as cv.Mat)
        labels(2) = "Histogram with Yellow line showing the trends"
        slr.hist.plot.backcolor = cv.Scalar.Red
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
        slr.hist.plot.maxValue = valList.Max
        lastPoint = New cv.Point2f(barMidPoint, dst2.Height - dst2.Height * valList(0) / slr.hist.plot.maxValue)
        resultingPoints.Clear()
        resultingPoints.Add(lastPoint)
        For i = 1 To valList.Count - 2
            If valList(i - 1) > valList(i) And valList(i + 1) > valList(i) Then valList(i) = (valList(i - 1) + valList(i + 1)) / 2
            connectLine(i)
        Next
        connectLine(valList.Count - 1)
    End Sub
End Class









Public Class SLR_TrendImages : Inherits VB_Algorithm
    Dim trends As New SLR_Trends
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("pcSplit(2) input")
            radio.addRadio("Grayscale input")
            radio.addRadio("Blue input")
            radio.addRadio("Green input")
            radio.addRadio("Red input")
            radio.check(1).Checked = True
        End If

        desc = "Find trends by filling in short histogram gaps for depth or 1-channel images"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim split = src.Split()
        trends.hist.plot.maxRange = 255
        trends.hist.plot.removeZeroEntry = False ' default is to look at element 0....

        Dim splitIndex = 0
        Static frm = findfrm(traceName + " Radio Buttons")
        Select Case findRadioText(frm.check)
            Case "pcSplit(2) input"
                trends.hist.plot.maxRange = task.maxZmeters
                trends.hist.plot.removeZeroEntry = True ' not interested in the undefined depth areas...
                trends.Run(task.pcSplit(2))
                labels(2) = "SLR_TrendImages - pcSplit(2)"
            Case "Grayscale input"
                trends.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
                labels(2) = "SLR_TrendImages - grayscale"
            Case "Blue input"
                labels(2) = "SLR_TrendImages - Blue channel"
                splitIndex = 0
            Case "Green input"
                labels(2) = "SLR_TrendImages - Green channel"
                splitIndex = 1
            Case "Red input"
                labels(2) = "SLR_TrendImages - Red channel"
                splitIndex = 2
        End Select
        trends.Run(split(splitIndex))
        dst2 = trends.dst2
    End Sub
End Class










Public Class SLR_SurfaceH : Inherits VB_Algorithm
    Dim surface As New PointCloud_SurfaceH
    Public Sub New()
        desc = "Use the PointCloud_SurfaceH data to indicate valleys and peaks."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        surface.Run(src)
        dst2 = surface.dst3
    End Sub
End Class









Public Class SLR_Trends : Inherits VB_Algorithm
    Public hist As New Histogram_KalmanAuto
    Dim valList As New List(Of Single)
    Dim barMidPoint As Single
    Dim lastPoint As cv.Point2f
    Public resultingPoints As New List(Of cv.Point2f)
    Public resultingValues As New List(Of Single)
    Public Sub New()
        desc = "Find trends by filling in short histogram gaps in the given image's histogram."
    End Sub
    Public Sub connectLine(i As Integer, dst As cv.Mat)
        Dim x = barMidPoint + dst.Width * i / valList.Count
        Dim y = dst.Height - dst.Height * valList(i) / hist.plot.maxValue
        Dim p1 = New cv.Point2f(x, y)
        resultingPoints.Add(p1)
        resultingValues.Add(p1.Y)
        dst.Line(lastPoint, p1, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        lastPoint = p1
    End Sub
    Public Sub RunVB(src As cv.Mat)
        labels(2) = "Grayscale histogram - yellow line shows trend"
        hist.plot.backColor = cv.Scalar.Red
        hist.Run(src)
        dst2 = hist.dst2

        Dim indexer = hist.histogram.GetGenericIndexer(Of Single)()
        valList = New List(Of Single)
        For i = 0 To hist.histogram.Rows - 1
            valList.Add(indexer(i))
        Next
        barMidPoint = dst2.Width / valList.Count / 2

        If valList.Count < 2 Then Exit Sub
        hist.plot.maxValue = valList.Max
        lastPoint = New cv.Point2f(barMidPoint, dst2.Height - dst2.Height * valList(0) / hist.plot.maxValue)
        resultingPoints.Clear()
        resultingValues.Clear()
        resultingPoints.Add(lastPoint)
        resultingValues.Add(lastPoint.Y)
        For i = 1 To valList.Count - 2
            If valList(i - 1) > valList(i) And valList(i + 1) > valList(i) Then
                valList(i) = (valList(i - 1) + valList(i + 1)) / 2
            End If
            connectLine(i, dst2)
        Next
        connectLine(valList.Count - 1, dst2)
    End Sub
End Class
