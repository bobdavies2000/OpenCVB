Imports cv = OpenCvSharp
Public Class Bin2Way_Basics : Inherits VB_Parent
    Public hist As New Hist_Basics
    Public mats As New Mat_4Click
    Public fraction As Single
    Dim halfSplit As Integer
    Public Sub New()
        fraction = dst2.Total / 2
        task.gOptions.setHistogramBins(256)
        labels = {"", "", "Image separated into 2 segments from darkest and lightest", "Histogram Of grayscale image"}
        desc = "Split an image into 2 parts - darkest and lightest,"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = task.histogramBins
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.Run(src)
        dst3 = hist.dst2

        Dim histArray = hist.histArray
        Dim accum As Single
        For i = 0 To histArray.Count - 1
            accum += histArray(i)
            If accum > fraction Then
                halfSplit = i
                Exit For
            End If
        Next

        Dim offset = halfSplit / bins * dst3.Width
        DrawLine(dst3, New cv.Point(offset, 0), New cv.Point(offset, dst3.Height), cv.Scalar.White)

        mats.mat(0) = src.InRange(0, halfSplit - 1)         ' darkest
        mats.mat(1) = src.InRange(halfSplit, 255)            ' lightest

        If standaloneTest() Then
            mats.Run(empty)
            dst2 = mats.dst2
        End If
    End Sub
End Class






Public Class Bin2Way_KMeans : Inherits VB_Parent
    Public bin2 As New Bin2Way_Basics
    Dim kmeans As New KMeans_Dimensions
    Dim mats As New Mat_4Click
    Public Sub New()
        FindSlider("KMeans k").Value = 2
        labels = {"", "", "Darkest (upper left),lightest (upper right)", "Selected image from dst2"}
        desc = "Use kmeans with each of the 2-way split images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        bin2.Run(src)

        kmeans.Run(src)
        For i = 0 To 2
            mats.mat(i).SetTo(0)
            kmeans.dst3.CopyTo(mats.mat(i), bin2.mats.mat(i))
        Next

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class







Public Class Bin2Way_RedCloudDarkest : Inherits VB_Parent
    Dim bin2 As New Bin2Way_RecurseOnce
    Dim flood As New Flood_BasicsMask
    Public Sub New()
        desc = "Use RedCloud with the darkest regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then bin2.Run(src)

        flood.inputMask = Not bin2.mats.mat(0)
        flood.Run(bin2.mats.mat(0))
        dst2 = flood.dst2
        If task.heartBeat Then labels(2) = CStr(task.redCells.Count) + " cells were identified"
    End Sub
End Class







Public Class Bin2Way_RedCloudLightest : Inherits VB_Parent
    Dim bin2 As New Bin2Way_RecurseOnce
    Dim flood As New Flood_BasicsMask
    Public Sub New()
        desc = "Use RedCloud with the lightest regions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then bin2.Run(src)

        flood.inputMask = Not bin2.mats.mat(3)
        flood.Run(bin2.mats.mat(3))
        dst2 = flood.dst2
        If task.heartBeat Then labels(2) = CStr(task.redCells.Count) + " cells were identified"
    End Sub
End Class




Public Class Bin2Way_RecurseOnce : Inherits VB_Parent
    Dim bin2 As New Bin2Way_Basics
    Public mats As New Mat_4Click
    Public Sub New()
        desc = "Keep splitting an image between light and dark"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        bin2.fraction = src.Total / 2
        bin2.hist.inputMask = New cv.Mat
        bin2.Run(src)
        Dim darkestMask = bin2.mats.mat(0).Clone
        Dim lightestMask = bin2.mats.mat(1).Clone

        bin2.fraction = src.Total / 4
        bin2.hist.inputMask = darkestMask
        bin2.Run(src)

        mats.mat(0) = bin2.mats.mat(0)
        mats.mat(1) = bin2.mats.mat(1) And Not lightestMask

        bin2.fraction = src.Total / 4
        bin2.hist.inputMask = lightestMask
        bin2.Run(src)
        mats.mat(2) = bin2.mats.mat(0) And Not darkestMask
        mats.mat(3) = bin2.mats.mat(1)

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class






Public Class Bin2Way_RedCloud : Inherits VB_Parent
    Dim bin2 As New Bin2Way_RecurseOnce
    Dim flood As New Flood_BasicsMask
    Dim cellMaps(3) As cv.Mat, redCells(3) As List(Of rcData)
    Dim options As New Options_Bin2WayRedCloud
    Public Sub New()
        flood.showSelected = False
        desc = "Identify the lightest, darkest, and other regions separately and then combine the rcData."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.optionsChanged Then
            For i = 0 To redCells.Count - 1
                redCells(i) = New List(Of rcData)
                cellMaps(i) = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, 0)
            Next
        End If

        bin2.Run(src)

        Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = options.startRegion To options.endRegion
            task.cellMap = cellMaps(i)
            task.redCells = redCells(i)
            flood.inputMask = Not bin2.mats.mat(i)
            flood.Run(bin2.mats.mat(i))
            cellMaps(i) = task.cellMap.Clone
            redCells(i) = New List(Of rcData)(task.redCells)
            For Each rc In task.redCells
                If rc.index = 0 Then Continue For
                sortedCells.Add(rc.pixels, rc)
            Next
        Next

        dst2 = RebuildCells(sortedCells)

        If task.heartBeat Then labels(2) = CStr(task.redCells.Count) + " cells were identified and matched to the previous image"
    End Sub
End Class