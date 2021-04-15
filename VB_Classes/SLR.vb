Imports cv = OpenCvSharp
Imports System.IO
' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
Public Class SLR_Basics : Inherits VBparent
    Public input As SLR_Data
    Dim slr As New CS_Classes.SLR
    Dim plot As Plot_Basics_CPP
    Public Sub New()
        plot = New Plot_Basics_CPP()
        input = New SLR_Data()
        If standalone Then
            input.Run(dst1)
            label1 = "Sample data input"
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Approximate accuracy (tolerance) X100", 1, 1000, 30)
            sliders.setupTrackBar(1, "Simple moving average window size", 1, 100, 20)
        End If
        task.desc = "Segmented Linear Regression example"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)

        Dim resultX As New List(Of Double)
        Dim resultY As New List(Of Double)

        Static toleranceSlider = findSlider("Approximate accuracy (tolerance) X100")
        Dim tolerance = toleranceSlider.value / 100
        Static movingAvgSlider = findSlider("Simple moving average window size")
        Dim halfLength = movingAvgSlider.value
        slr.SegmentedRegressionFast(input.dataX, input.dataY, tolerance, halfLength, resultX, resultY)

        label1 = "Tolerance = " + CStr(tolerance) + " and moving average window = " + CStr(halfLength)
        If resultX.Count > 0 Then
            plot.srcX = input.dataX.ToArray
            plot.srcY = input.dataY.ToArray
            plot.Run(src)
            dst1 = plot.dst1.Clone

            plot.srcX = resultX.ToArray
            plot.srcY = resultY.ToArray
            plot.Run(src)
            dst2 = plot.dst1
        Else
            dst1.SetTo(0)
            dst2.SetTo(0)
            task.trueText(label1 + " yielded no results...")
        End If
        If standalone = False Then
            input.dataX.Clear()
            input.dataY.Clear()
        End If
    End Sub
End Class






' https://www.codeproject.com/Articles/5282014/Segmented-Linear-Regression
Public Class SLR_Data : Inherits VBparent
    Dim plot As Plot_Basics_CPP
    Public dataX As New List(Of Double)
    Public dataY As New List(Of Double)
    Public Sub New()
        plot = New Plot_Basics_CPP()

        Dim sr = New StreamReader(task.parms.homeDir + "/Data/real_data.txt")
        Dim code As String = sr.ReadToEnd
        sr.Close()

        Dim lines = code.Split(vbCrLf)
        For Each line In lines
            Dim split = line.Split(" ")
            If split.Length > 1 Then
                dataX.Add(CDbl(split(0)))
                dataY.Add(CDbl(split(1)))
            End If
        Next
        task.desc = "Plot the data used in SLR_Basics"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        plot.srcX = dataX.ToArray
        plot.srcY = dataY.ToArray
        plot.Run(src)
        dst1 = plot.dst1
    End Sub
End Class







Public Class SLR_Image : Inherits VBparent
    Dim slr As SLR_Basics
    Dim hist As Histogram_Graph
    Public Sub New()
        hist = New Histogram_Graph()
        hist.plotRequested = True
        slr = New SLR_Basics()
        label1 = "Original data"
        task.desc = "Run Segmented Linear Regression on grayscale image data - just an experiment"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.plotColors(0) = cv.Scalar.White
        hist.Run(src)
        dst1 = hist.dst1
        For i = 0 To hist.histRaw(0).Rows - 1
            For j = 0 To 3 - 1
                slr.input.dataX.Add(i)
                slr.input.dataY.Add(hist.histRaw(j).Get(Of Single)(i, 0))
            Next
        Next
        slr.Run(src)
        dst2 = slr.dst2
    End Sub
End Class 
