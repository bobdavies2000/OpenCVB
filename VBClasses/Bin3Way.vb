Imports cv = OpenCvSharp
Public Class Bin3Way_Basics : Inherits TaskParent
    Dim hist As New Hist_Basics
    Public mats As New Mat_4Click
    Dim firstThird As Integer, lastThird As Integer
    Public Sub New()
        task.gOptions.setHistogramBins(255)
        labels = {"", "", "Image separated into three segments from darkest to lightest and 'Other' (between)", "Histogram Of grayscale image"}
        desc = "Split an image into 3 parts - darkest, lightest, and in-between (2)"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim bins = task.histogramBins

        If task.heartBeat Then
            firstThird = 0
            lastThird = 0
            hist.Run(task.gray)
            dst3 = hist.dst2

            Dim histogram = hist.histArray
            Dim third = src.Total / 3, accum As Single
            For i = 0 To histogram.Count - 1
                accum += histogram(i)
                If accum > third Then
                    If firstThird = 0 Then
                        firstThird = i
                        accum = 0
                    Else
                        lastThird = i
                        Exit For
                    End If
                End If
            Next
        End If

        Dim offset = firstThird / bins * dst3.Width
        DrawLine(dst3, New cv.Point(offset, 0), New cv.Point(offset, dst3.Height), white)
        offset = lastThird / bins * dst3.Width
        DrawLine(dst3, New cv.Point(offset, 0), New cv.Point(offset, dst3.Height), white)

        mats.mat(0) = task.gray.InRange(0, firstThird - 1)         ' darkest
        mats.mat(1) = task.gray.InRange(lastThird, 255)            ' lightest
        mats.mat(2) = task.gray.InRange(firstThird, lastThird - 1) ' other

        If standaloneTest() Then
            mats.Run(emptyMat)
            dst2 = mats.dst2
        End If
    End Sub
End Class







Public Class Bin3Way_KMeans : Inherits TaskParent
    Public bin3 As New Bin3Way_Basics
    Dim kmeans As New KMeans_Dimensions
    Dim mats As New Mat_4Click
    Public Sub New()
        OptionParent.FindSlider("KMeans k").Value = 2
        labels = {"", "", "Darkest (upper left), mixed (upper right), lightest (bottom left)", "Selected image from dst2"}
        desc = "Use kmeans with each of the 3-way split images"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        bin3.Run(task.gray)

        kmeans.Run(task.gray)
        For i = 0 To 2
            mats.mat(i).SetTo(0)
            kmeans.dst3.CopyTo(mats.mat(i), bin3.mats.mat(i))
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class







Public Class Bin3Way_Color : Inherits TaskParent
    Dim bin3 As New Bin3Way_KMeans
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Build the palette input that best separates the light and dark regions of an image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        bin3.Run(src)
        dst2.SetTo(4)
        dst2.SetTo(1, bin3.bin3.mats.mat(0))
        dst2.SetTo(2, bin3.bin3.mats.mat(1))
        dst2.SetTo(3, bin3.bin3.mats.mat(2))
        dst3 = PaletteFull(dst2)
    End Sub
End Class







Public Class Bin3Way_RedColorDarkest : Inherits TaskParent
    Dim bin3 As New Bin3Way_KMeans
    Dim flood As New Flood_BasicsMask
    Public Sub New()
        desc = "Use RedColor with the darkest regions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then bin3.Run(src)

        flood.inputRemoved = Not bin3.bin3.mats.mat(0)
        flood.Run(bin3.bin3.mats.mat(0))
        dst2 = flood.dst2
    End Sub
End Class







Public Class Bin3Way_RedColorLightest : Inherits TaskParent
    Dim bin3 As New Bin3Way_KMeans
    Dim flood As New Flood_BasicsMask
    Public Sub New()
        desc = "Use RedColor with the lightest regions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then bin3.Run(src)

        flood.inputRemoved = Not bin3.bin3.mats.mat(2)
        flood.Run(bin3.bin3.mats.mat(2))
        dst2 = flood.dst2
    End Sub
End Class





Public Class Bin3Way_RedColorOther : Inherits TaskParent
    Dim bin3 As New Bin3Way_KMeans
    Dim flood As New Flood_BasicsMask
    Dim color8U As New Color8U_Basics
    Public Sub New()
        flood.inputRemoved = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Use RedColor with the regions that are neither lightest or darkest"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then bin3.Run(src)

        flood.inputRemoved = bin3.bin3.mats.mat(0) Or bin3.bin3.mats.mat(1)

        color8U.Run(src)
        flood.Run(color8U.dst2)
        dst2 = flood.dst2
    End Sub
End Class






Public Class Bin3Way_RedCloud : Inherits TaskParent
    Dim redC0 As New RedColor_Basics
    Dim redC1 As New RedColor_Basics
    Dim redC2 As New RedColor_Basics
    Dim bin3 As New Bin3Way_KMeans
    Dim mats As New Mat_4Click
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "The output of all 3 RedColor runs are combined below."
        desc = "Combine the results of the 3 different RedColor runs..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bin3.Run(src)
        labels(2) = bin3.labels(2)

        Dim mat = bin3.bin3.mats.mat
        Dim redC As Object = Nothing
        For i = 0 To 2
            If i = 0 Then redC = redC0
            If i = 1 Then redC = redC1
            If i = 2 Then redC = redC2
            redC.run(mat(i))
            mats.mat(i) = redC.dst2
            mats.mat(i).SetTo(0, Not mat(i))
            mats.mat(i).CopyTo(dst3, mat(i))
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2
    End Sub
End Class
