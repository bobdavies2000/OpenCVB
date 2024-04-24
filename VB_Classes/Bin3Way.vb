Imports OpenCvSharp
Imports cv = OpenCvSharp

Public Class Bin3Way_Basics : Inherits VB_Algorithm
    Dim hist As New Histogram_Basics
    Public mats As New Mat_4Click
    Public Sub New()
        gOptions.HistBinSlider.Value = 256
        labels = {"", "", "Image separated into three segments from darkest To lightest", "Histogram Of grayscale image"}
        desc = "Split an image into 3 parts - darkest to lightest"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = gOptions.HistBinSlider.Value
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.Run(src)
        dst3 = hist.dst2

        Dim histogram = hist.histArray
        Dim third = src.Total / 3, accum As Single
        Dim firstThird As Integer, secondThird As Integer
        For i = 0 To histogram.Count - 1
            accum += histogram(i)
            If accum > third Then
                If firstThird = 0 Then
                    firstThird = i
                    accum = 0
                Else
                    secondThird = i
                    Exit For
                End If
            End If
        Next

        Dim offset = firstThird / bins * dst3.Width
        dst3.Line(New cv.Point(offset, 0), New cv.Point(offset, dst3.Height), cv.Scalar.White, task.lineWidth)
        offset = secondThird / bins * dst3.Width
        dst3.Line(New cv.Point(offset, 0), New cv.Point(offset, dst3.Height), cv.Scalar.White, task.lineWidth)

        mats.mat(0) = src.InRange(0, firstThird - 1)
        mats.mat(1) = src.InRange(firstThird, secondThird - 1)
        mats.mat(2) = src.InRange(secondThird, 255)

        mats.Run(empty)
        dst2 = mats.dst2
    End Sub
End Class







Public Class Bin3Way_KMeans : Inherits VB_Algorithm
    Dim bin3 As New Bin3Way_Basics
    Dim kmeans As New KMeans_Dimensions
    Dim mats As New Mat_4Click
    Public Sub New()
        findSlider("KMeans k").Value = 2
        labels = {"", "", "Darkest (upper left), mixed (upper right), lightest (bottom left)", "Selected image from dst2"}
        desc = "Use kmeans with each of the 3-way split images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        bin3.Run(src)

        kmeans.Run(src)
        For i = 0 To 2
            mats.mat(i).SetTo(0)
            kmeans.dst3.CopyTo(mats.mat(i), bin3.mats.mat(i))
        Next

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class
