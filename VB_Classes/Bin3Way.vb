Imports System.Security.Cryptography
Imports OpenCvSharp
Imports OpenCvSharp.Flann
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
    Public bin3 As New Bin3Way_Basics
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







Public Class Bin3Way_Color : Inherits VB_Algorithm
    Dim bin3 As New Bin3Way_KMeans
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Build the palette input that best separates the light and dark regions of an image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bin3.Run(src)
        dst2.SetTo(4)
        dst2.SetTo(1, bin3.bin3.mats.mat(0))
        dst2.SetTo(2, bin3.bin3.mats.mat(1))
        dst2.SetTo(3, bin3.bin3.mats.mat(2))
        dst3 = vbPalette(dst2 * 255 / 3)
    End Sub
End Class







Public Class Bin3Way_RedCloudDark : Inherits VB_Algorithm
    Dim bin3 As New Bin3Way_KMeans
    Dim flood As New Flood_BasicsMask
    Public Sub New()
        desc = "Use RedCloud with the darkest regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then bin3.Run(src)

        flood.inputMask = Not bin3.bin3.mats.mat(0)
        flood.Run(bin3.bin3.mats.mat(0))
        dst2 = flood.dst2
    End Sub
End Class







Public Class Bin3Way_RedCloudLite : Inherits VB_Algorithm
    Dim bin3 As New Bin3Way_KMeans
    Dim flood As New Flood_BasicsMask
    Public Sub New()
        desc = "Use RedCloud with the lightest regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then bin3.Run(src)

        flood.inputMask = Not bin3.bin3.mats.mat(2)
        flood.Run(bin3.bin3.mats.mat(2))
        dst2 = flood.dst2
    End Sub
End Class





Public Class Bin3Way_RedCloudOther : Inherits VB_Algorithm
    Dim bin3 As New Bin3Way_KMeans
    Dim flood As New Flood_BasicsMask
    Dim color As New Color_Basics
    Public Sub New()
        flood.inputMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Use RedCloud with the regions that are neither lightest or darkest"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then bin3.Run(src)

        flood.inputMask = bin3.bin3.mats.mat(0) Or bin3.bin3.mats.mat(2)

        color.Run(src)
        flood.Run(color.dst2)
        dst2 = flood.dst2
    End Sub
End Class







Public Class Bin3Way_RedCloud : Inherits VB_Algorithm
    Dim bin3 As New Bin3Way_KMeans
    Dim flood As New Flood_BasicsMask
    Dim color As New Color_Basics
    Dim cellMaps(2) As cv.Mat, redCells(2) As List(Of rcData)
    Dim options As New Options_Bin3WayRedCloud
    Public Sub New()
        redOptions.identifyCount = 100
        desc = "Identify the lightest, darkest, and other regions separately and then combine the rcData."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.optionsChanged Then
            For i = 0 To redCells.Count - 1
                redCells(i) = New List(Of rcData)
                cellMaps(i) = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            Next
        End If

        bin3.Run(src)

        For i = options.startRegion To options.endRegion
            task.cellMap = cellMaps(i)
            task.redCells = redCells(i)
            If i = 0 Or i = 2 Then
                flood.inputMask = Not bin3.bin3.mats.mat(i)
                flood.Run(bin3.bin3.mats.mat(i))
            Else
                flood.inputMask = bin3.bin3.mats.mat(0) Or bin3.bin3.mats.mat(2)
                color.Run(src)
                flood.Run(color.dst2)
            End If
            cellMaps(i) = task.cellMap.Clone
            redCells(i) = New List(Of rcData)(task.redCells)
        Next

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To 2
            For Each rc In redCells(i)
                sortedCells.Add(rc.pixels, rc)
            Next
        Next

        task.redCells.Clear()
        task.redCells.Add(New rcData)
        task.cellMap.SetTo(0)
        dst2.SetTo(0)
        Static lastImage As cv.Mat = dst2.Clone
        Dim ptMarks As New List(Of cv.Point)
        For Each rc In sortedCells.Values
            If rc.index = 0 Then Continue For
            If ptMarks.Contains(rc.maxDStable) Then
                Dim index = ptMarks.IndexOf(rc.maxDStable)
                rc.color = task.redCells(index).color
            End If
            rc.index = task.redCells.Count
            task.redCells.Add(rc)
            task.cellMap(rc.rect).SetTo(rc.index, rc.mask)

            dst2(rc.rect).SetTo(rc.color, rc.mask)
            ptMarks.Add(rc.maxDStable)
            If rc.index >= 255 Then Exit For
        Next

        If task.heartBeat Then labels(2) = CStr(task.redCells.Count) + " cells were identified and matched to the previous image"
        lastImage = dst2.Clone
    End Sub
End Class
