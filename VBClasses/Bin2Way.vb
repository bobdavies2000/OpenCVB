Imports cv = OpenCvSharp
Public Class Bin2Way_Basics : Inherits TaskParent
    Public hist As New Hist_Basics
    Public mats As New Mat_4Click
    Public fraction As Single
    Dim halfSplit As Integer
    Public Sub New()
        fraction = dst2.Total / 2
        task.gOptions.setHistogramBins(255)
        labels = {"", "", "Image separated into 2 segments from darkest and lightest", "Histogram Of grayscale image"}
        desc = "Split an image into 2 parts - darkest and lightest,"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim bins = task.histogramBins
        hist.Run(task.gray)
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
        DrawLine(dst3, New cv.Point(offset, 0), New cv.Point(offset, dst3.Height), white)

        mats.mat(0) = task.gray.InRange(0, halfSplit - 1)         ' darkest
        mats.mat(1) = task.gray.InRange(halfSplit, 255)            ' lightest

        If standaloneTest() Then
            mats.Run(emptyMat)
            dst2 = mats.dst2
        End If
    End Sub
End Class






Public Class Bin2Way_KMeans : Inherits TaskParent
    Public bin2 As New Bin2Way_Basics
    Dim kmeans As New KMeans_Dimensions
    Dim mats As New Mat_4Click
    Public Sub New()
        OptionParent.FindSlider("KMeans k").Value = 2
        labels = {"", "", "Darkest (upper left),lightest (upper right)", "Selected image from dst2"}
        desc = "Use kmeans with each of the 2-way split images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bin2.Run(task.gray)

        kmeans.Run(task.gray)
        For i = 0 To 2
            mats.mat(i).SetTo(0)
            kmeans.dst3.CopyTo(mats.mat(i), bin2.mats.mat(i))
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class







Public Class Bin2Way_RedCloudDarkest : Inherits TaskParent
    Dim bin2 As New Bin2Way_RecurseOnce
    Dim flood As New Flood_BasicsMask
    Public Sub New()
        desc = "Use RedCloud with the darkest regions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then bin2.Run(src)

        flood.inputRemoved = Not bin2.mats.mat(0)
        flood.Run(bin2.mats.mat(0))
        dst2 = flood.dst2
        If task.heartBeat Then labels(2) = CStr(task.redList.oldrclist.Count) + " cells were identified"
    End Sub
End Class







Public Class Bin2Way_RedCloudLightest : Inherits TaskParent
    Dim bin2 As New Bin2Way_RecurseOnce
    Dim flood As New Flood_BasicsMask
    Public Sub New()
        desc = "Use RedCloud with the lightest regions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then bin2.Run(src)

        flood.inputRemoved = Not bin2.mats.mat(3)
        flood.Run(bin2.mats.mat(3))
        dst2 = flood.dst2
        If task.heartBeat Then labels(2) = CStr(task.redList.oldrclist.Count) + " cells were identified"
    End Sub
End Class




Public Class Bin2Way_RecurseOnce : Inherits TaskParent
    Dim bin2 As New Bin2Way_Basics
    Public mats As New Mat_4Click
    Public Sub New()
        desc = "Keep splitting an image between light and dark"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bin2.fraction = task.gray.Total / 2
        bin2.hist.histMask = New cv.Mat
        bin2.Run(task.gray)
        Dim darkestMask = bin2.mats.mat(0).Clone
        Dim lightestMask = bin2.mats.mat(1).Clone

        bin2.fraction = task.gray.Total / 4
        bin2.hist.histMask = darkestMask
        bin2.Run(task.gray)

        mats.mat(0) = bin2.mats.mat(0)
        mats.mat(1) = bin2.mats.mat(1) And Not lightestMask

        bin2.fraction = task.gray.Total / 4
        bin2.hist.histMask = lightestMask
        bin2.Run(task.gray)
        mats.mat(2) = bin2.mats.mat(0) And Not darkestMask
        mats.mat(3) = bin2.mats.mat(1)

        mats.Run(emptyMat)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class






Public Class Bin2Way_RedCloud : Inherits TaskParent
    Dim bin2 As New Bin2Way_RecurseOnce
    Dim flood As New Flood_BasicsMask
    Dim cellMaps(3) As cv.Mat
    Dim pclist(3) As List(Of cloudData)
    Dim options As New Options_Bin2WayRedCloud
    Public Sub New()
        flood.showSelected = False
        desc = "Identify the lightest, darkest, and other regions separately and then combine the rcData."
    End Sub
    Public Shared Function rebuildMap(sortedCells As SortedList(Of Integer, cloudData)) As cv.Mat
        task.redCloud.pcList.Clear()
        task.redCloud.pcList.Add(New cloudData) ' placeholder cloudData so map is correct.
        task.redList.rcMap.SetTo(0)
        Static saveColorSetting = task.gOptions.trackingLabel
        For Each pc In sortedCells.Values
            pc.index = task.redCloud.pcList.Count

            If saveColorSetting <> task.gOptions.trackingLabel Then pc.color = New cv.Vec3b
            Select Case task.gOptions.trackingLabel
                Case "Mean Color"
                    Dim colorStdev As cv.Scalar
                    Dim color = New cv.Scalar(pc.color(0), pc.color(1), pc.color(2))
                    cv.Cv2.MeanStdDev(task.color(pc.rect), color, colorStdev, pc.mask)
                Case "Tracking Color"
                    If pc.color = black Then pc.color = task.vecColors(pc.index)
            End Select

            task.redCloud.pcList.Add(pc)
            task.redList.rcMap(pc.rect).SetTo(pc.index, pc.mask)
            DisplayCells.Circle(pc.maxDist, task.DotSize, task.highlight, -1)
            If pc.index >= 255 Then Exit For
        Next
        saveColorSetting = task.gOptions.trackingLabel
        task.redList.rcMap.SetTo(0, task.noDepthMask)
        Return DisplayCells()
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        dst3 = runRedCloud(src, labels(3))

        If task.optionsChanged Then
            For i = 0 To pclist.Count - 1
                pclist(i) = New List(Of cloudData)
                cellMaps(i) = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            Next
        End If

        bin2.Run(src)

        Dim sortedCells As New SortedList(Of Integer, cloudData)(New compareAllowIdenticalIntegerInverted)
        For i = options.startRegion To options.endRegion
            task.redCloud.pcMap = cellMaps(i)

            task.redCloud.pcList = pclist(i)
            flood.inputRemoved = Not bin2.mats.mat(i)
            flood.Run(bin2.mats.mat(i))
            cellMaps(i) = task.redList.rcMap.Clone
            pclist(i) = New List(Of cloudData)(task.redCloud.pcList)
            For Each pc In task.redCloud.pcList
                If pc.index = 0 Then Continue For
                sortedCells.Add(pc.pixels, pc)
            Next
        Next

        dst2 = rebuildMap(sortedCells)

        If task.heartBeat Then labels(2) = CStr(task.redCloud.pcList.Count) + " cells were identified and matched to the previous image"
    End Sub
End Class






Public Class Bin2Way_RedColor : Inherits TaskParent
    Dim bin2 As New Bin2Way_RecurseOnce
    Dim redC As New RedColor_Core
    Dim pclist(3) As List(Of cloudData)
    Public Sub New()
        For i = 0 To pclist.Count - 1
            pclist(i) = New List(Of cloudData)
        Next
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Identify the lightest, darkest, and other regions separately and then combine the rcData."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bin2.Run(src)

        For i = 0 To bin2.mats.mat.Count - 1
            Dim index = CInt(255 / (i + 1))
            dst1.SetTo(index, bin2.mats.mat(i))
        Next
        redC.Run(dst1)
        labels(2) = redC.labels(2)

        dst2.SetTo(0)
        For Each pc In redC.pcList
            dst2(pc.rect).SetTo(pc.index, pc.mask)
        Next

        dst3 = PaletteBlackZero(dst2)

        'Dim sortedCells As New SortedList(Of Integer, cloudData)(New compareAllowIdenticalIntegerInverted)
        'For i = options.startRegion To options.endRegion
        '    task.redColor.pcMap = cellMaps(i)

        '    task.redColor.pcList = pclist(i)
        '    flood.inputRemoved = Not bin2.mats.mat(i)
        '    flood.Run(bin2.mats.mat(i))
        '    cellMaps(i) = task.redList.rcMap.Clone
        '    pclist(i) = New List(Of cloudData)(task.redColor.pcList)
        '    For Each pc In task.redColor.pcList
        '        If pc.index = 0 Then Continue For
        '        sortedCells.Add(pc.pixels, pc)
        '    Next
        'Next

        'dst2 = rebuildMap(sortedCells)

        'If task.heartBeat Then labels(2) = CStr(task.redColor.pcList.Count) + " cells were identified and matched to the previous image"
    End Sub
End Class