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




Public Class Bin2Way_RedColor : Inherits TaskParent
    Dim bin2 As New Bin2Way_Gradation
    Dim redC As New RedColor_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Identify 4 gradations of light and combine them for input to RedColor"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bin2.Run(src)

        redC.Run(bin2.dst3)
        labels(2) = redC.labels(2)

        dst2.SetTo(0)
        For Each rc In redC.rcList
            dst2(rc.rect).SetTo(rc.index, rc.mask)
        Next

        dst3 = PaletteFull(dst2)
    End Sub
End Class




Public Class Bin2Way_Gradation : Inherits TaskParent
    Dim bin2 As New Bin2Way_Basics
    Public mats(3) As cv.Mat
    Public Sub New()
        dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For Each m In mats
            m = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Next

        labels(2) = "4 gradations of light to dark - 8uC3"
        labels(3) = "4 gradations of light to dark - 8uC1 - no zeros..."
        desc = "Build 4 gradations of light and combine them."
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

        mats(0) = bin2.mats.mat(0)
        mats(1) = bin2.mats.mat(1) And Not lightestMask

        bin2.fraction = task.gray.Total / 4
        bin2.hist.histMask = lightestMask
        bin2.Run(task.gray)
        mats(2) = bin2.mats.mat(0) And Not darkestMask
        mats(3) = bin2.mats.mat(1)

        For i = 0 To mats.Count - 1
            Dim index = CInt(255 / (i + 1))
            dst3.SetTo(index, mats(i))
        Next

        dst2 = PaletteFull(dst3)
    End Sub
End Class






Public Class Bin2Way_GradationEdges : Inherits TaskParent
    Dim grad As New Bin2Way_Gradation
    Dim edges As New Edge_Basics
    Public Sub New()
        labels(2) = "4-way gradation of the color image with edges added."
        desc = "Add edges to the 4-way gradation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.gray)

        grad.Run(src)
        dst2 = grad.dst2
        dst2.SetTo(0, edges.dst2)
    End Sub
End Class
