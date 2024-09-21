Imports cvb = OpenCvSharp
Imports  System.IO
' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
Public Class SLR_Data : Inherits VB_Parent
    Dim plot As New Plot_Basics_CPP_VB
    Public dataX As New List(Of Double)
    Public dataY As New List(Of Double)
    Public Sub New()
        Dim sr = New StreamReader(task.HomeDir + "/Data/real_data.txt")
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
    Public Sub RunAlg(src As cvb.Mat)
        plot.srcX = dataX
        plot.srcY = dataY
        plot.Run(src)
        dst2 = plot.dst2
    End Sub
End Class






Public Class SLR_TrendImages : Inherits VB_Parent
    Dim trends As New SLR_Trends
    Dim options As New Options_SLRImages
    Public Sub New()
        desc = "Find trends by filling in short histogram gaps for depth or 1-channel images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim split = src.Split()
        trends.hist.plot.maxRange = 255
        trends.hist.plot.removeZeroEntry = False ' default is to look at element 0....

        Dim splitIndex = 0
        Select Case options.radioText
            Case "pcSplit(2) input"
                trends.hist.plot.maxRange = task.MaxZmeters
                trends.hist.plot.removeZeroEntry = True ' not interested in the undefined depth areas...
                trends.Run(task.pcSplit(2))
                labels(2) = "SLR_TrendImages - pcSplit(2)"
            Case "Grayscale input"
                trends.Run(src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
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










Public Class SLR_SurfaceH : Inherits VB_Parent
    Dim surface As New PointCloud_SurfaceH
    Public Sub New()
        desc = "Use the PointCloud_SurfaceH data to indicate valleys and peaks."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        surface.Run(src)
        dst2 = surface.dst3
    End Sub
End Class









Public Class SLR_Trends : Inherits VB_Parent
    Public hist As New Hist_KalmanAuto
    Dim valList As New List(Of Single)
    Dim barMidPoint As Single
    Dim lastPoint As cvb.Point2f
    Public resultingPoints As New List(Of cvb.Point2f)
    Public resultingValues As New List(Of Single)
    Public Sub New()
        desc = "Find trends by filling in short histogram gaps in the given image's histogram."
    End Sub
    Public Sub connectLine(i As Integer, dst As cvb.Mat)
        Dim x = barMidPoint + dst.Width * i / valList.Count
        Dim y = dst.Height - dst.Height * valList(i) / hist.plot.maxRange
        Dim p1 = New cvb.Point2f(x, y)
        resultingPoints.Add(p1)
        resultingValues.Add(p1.Y)
        dst.Line(lastPoint, p1, cvb.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        lastPoint = p1
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        labels(2) = "Grayscale histogram - yellow line shows trend"
        hist.plot.backColor = cvb.Scalar.Red
        hist.Run(src)
        dst2 = hist.dst2

        Dim indexer = hist.histogram.GetGenericIndexer(Of Single)()
        valList = New List(Of Single)
        For i = 0 To hist.histogram.Rows - 1
            valList.Add(indexer(i))
        Next
        barMidPoint = dst2.Width / valList.Count / 2

        If valList.Count < 2 Then Exit Sub
        hist.plot.maxRange = valList.Max
        lastPoint = New cvb.Point2f(barMidPoint, dst2.Height - dst2.Height * valList(0) / hist.plot.maxRange)
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