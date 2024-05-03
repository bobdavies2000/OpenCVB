Imports cv = OpenCvSharp
Public Class Bin3Way_Basics : Inherits VB_Algorithm
    Dim hist As New Hist_Basics
    Public mats As New Mat_4Click
    Public Sub New()
        gOptions.HistBinSlider.Value = 256
        labels = {"", "", "Image separated into three segments from darkest to lightest and 'Other' (between)", "Histogram Of grayscale image"}
        desc = "Split an image into 3 parts - darkest, lightest, and in-between the 2"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static firstThird As Integer, lastThird As Integer
        Dim bins = gOptions.HistBinSlider.Value
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If task.heartBeat Then
            firstThird = 0
            lastThird = 0
            hist.Run(src)
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
        dst3.Line(New cv.Point(offset, 0), New cv.Point(offset, dst3.Height), cv.Scalar.White, task.lineWidth)
        offset = lastThird / bins * dst3.Width
        dst3.Line(New cv.Point(offset, 0), New cv.Point(offset, dst3.Height), cv.Scalar.White, task.lineWidth)

        mats.mat(0) = src.InRange(0, firstThird - 1)         ' darkest
        mats.mat(1) = src.InRange(lastThird, 255)            ' lightest
        mats.mat(2) = src.InRange(firstThird, lastThird - 1) ' other

        If standaloneTest() Then
            mats.Run(empty)
            dst2 = mats.dst2
        End If
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







Public Class Bin3Way_RedCloudDarkest : Inherits VB_Algorithm
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







Public Class Bin3Way_RedCloudLightest : Inherits VB_Algorithm
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

        flood.inputMask = bin3.bin3.mats.mat(0) Or bin3.bin3.mats.mat(1)

        color.Run(src)
        flood.Run(color.dst2)
        dst2 = flood.dst2
    End Sub
End Class







Public Class Bin3Way_RedCloud1 : Inherits VB_Algorithm
    Dim bin3 As New Bin3Way_KMeans
    Dim flood As New Flood_BasicsMask
    Dim color As New Color_Basics
    Dim cellMaps(2) As cv.Mat, redCells(2) As List(Of rcData)
    Dim options As New Options_Bin3WayRedCloud
    Public Sub New()
        desc = "Identify the lightest, darkest, and 'Other' regions separately and then combine the rcData."
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
            If i = 2 Then
                flood.inputMask = bin3.bin3.mats.mat(0) Or bin3.bin3.mats.mat(1)
                color.Run(src)
                flood.Run(color.dst2)
            Else
                flood.inputMask = Not bin3.bin3.mats.mat(i)
                flood.Run(bin3.bin3.mats.mat(i))
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

        dst2 = vbRebuildCells(sortedCells)

        If task.heartBeat Then labels(2) = CStr(task.redCells.Count) + " cells were identified and matched to the previous image"
    End Sub
End Class








Public Class Bin3Way_RedCloud : Inherits VB_Algorithm
    Dim bin3 As New Bin3Way_KMeans
    Dim flood As New Flood_BasicsMask
    Dim color As New Color_Basics
    Dim cellMaps(2) As cv.Mat, redCells(2) As List(Of rcData)
    Dim options As New Options_Bin3WayRedCloud
    Public Sub New()
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

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = options.startRegion To options.endRegion
            task.cellMap = cellMaps(i)
            task.redCells = redCells(i)
            flood.inputMask = Not bin3.bin3.mats.mat(i)
            flood.Run(bin3.bin3.mats.mat(i))
            cellMaps(i) = task.cellMap.Clone
            redCells(i) = New List(Of rcData)(task.redCells)
            For Each rc In redCells(i)
                If rc.index = 0 Then Continue For
                sortedCells.Add(rc.pixels, rc)
            Next
        Next

        dst2 = vbRebuildCells(sortedCells)

        If task.heartBeat Then labels(2) = CStr(task.redCells.Count) + " cells were identified and matched to the previous image"
    End Sub
End Class

