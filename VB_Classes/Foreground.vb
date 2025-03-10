Imports System.Windows.Forms
Imports cv = OpenCvSharp
Public Class Foreground_Basics : Inherits TaskParent
    Dim simK As New KMeans_Depth
    Public fgDepth As Single
    Public fg As New cv.Mat, bg As New cv.Mat, classCount As Integer
    Public Sub New()
        labels(3) = "Foreground - all the KMeans classes up to and including the first class over 1 meter."
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find the first KMeans class with depth over 1 meter and use it to define foreground"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        simK.Run(src)
        classCount = simK.classCount

        ' Order the KMeans classes from foreground to background using depth data.
        Dim depthMats As New List(Of cv.Mat)
        Dim sortedMats As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
        For i = 0 To classCount - 1
            Dim tmp = simK.dst2.InRange(i, i)
            depthMats.Add(tmp.Clone)
            Dim depth = task.pcSplit(2).Mean(tmp)(0)
            sortedMats.Add(depth, i)
        Next

        fgDepth = 0
        For Each fgDepth In sortedMats.Keys
            If fgDepth >= 1 Then Exit For ' find all the regions closer than a meter (inclusive)
        Next

        For Each index In sortedMats.Values
            Dim tmp = depthMats(index)
            dst1.SetTo(index + 1, tmp)
        Next
        dst2 = ShowPalette(dst1)
        fg = task.pcSplit(2).Threshold(fgDepth, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        dst0 = fg

        fg.SetTo(0, task.noDepthMask)
        bg = Not fg

        dst3.SetTo(0)
        src.CopyTo(dst3, fg)
        SetTrueText("KMeans classes are in dst1 - ordered by depth" + vbCrLf + "fg = foreground mask", 3)
        labels(2) = "KMeans output defining the " + CStr(classCount) + " classes"
    End Sub
End Class






Public Class Foreground_KMeans : Inherits TaskParent
    Dim km As New KMeans_Image
    Public Sub New()
        optiBase.FindSlider("KMeans k").Value = 2
        labels = {"", "", "Foreground Mask", "Background Mask"}
        dst2 = New cv.Mat(New cv.Size(task.dst2.Width, task.dst2.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(New cv.Size(task.dst2.Width, task.dst2.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Separate foreground and background using Kmeans with k=2."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.optionsChanged = True

        km.Run(task.pcSplit(2))

        Dim minDistance = Single.MaxValue
        Dim minIndex As Integer
        For i = 0 To km.km.colors.Rows - 1
            Dim distance = km.km.colors.Get(Of Single)(i, 0)
            If minDistance > distance And distance > 0 Then
                minDistance = distance
                minIndex = i
            End If
        Next
        dst2.SetTo(0)
        dst2.SetTo(255, km.masks(minIndex))
        dst2.SetTo(0, task.noDepthMask)

        dst3 = Not dst2
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class Foreground_Contours : Inherits TaskParent
    Public fore As New Foreground_Hist3D
    Dim contours As New Contour_General
    Public Sub New()
        desc = "Create contours for the foreground mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fore.Run(src)

        contours.Run(fore.dst2)
        dst2 = contours.dst2
    End Sub
End Class






Public Class Foreground_Hist3D : Inherits TaskParent
    Dim hcloud As New Hist3Dcloud_Basics
    Public Sub New()
        hcloud.maskInput = task.noDepthMask
        labels = {"", "", "Foreground", "Background"}
        desc = "Use the first class of hist3Dcloud_Basics as the definition of foreground"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hcloud.Run(src)

        dst2.SetTo(0)
        dst2 = hcloud.dst2.InRange(1, 1) Or task.noDepthMask
        dst3 = Not dst2
    End Sub
End Class






Public Class Foreground_RedCloud : Inherits TaskParent
    Dim fore As New Foreground_CellsFore
    Dim back As New Foreground_CellsBack
    Public Sub New()
        desc = "Isolate foreground from background, then segment each with RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fore.Run(src)
        dst2 = fore.dst2
        labels(2) = fore.labels(2)

        back.Run(src)
        dst3 = back.dst2
        labels(3) = back.labels(2)
        If task.rcList.Count > 0 Then
            dst2(task.rc.roi).SetTo(white, task.rc.mask)
        End If
    End Sub
End Class





Public Class Foreground_CellsFore : Inherits TaskParent
    Dim fore As New Foreground_Hist3D
    Public rcList As New List(Of rcData)
    Public Sub New()
        desc = "Get the foreground cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runRedC(src)

        fore.Run(src)
        dst3 = fore.dst3
        dst2.SetTo(0)
        For Each rc In task.rcList
            Dim tmp As cv.Mat = dst3(rc.roi) And rc.mask
            If tmp.CountNonZero Then dst2(rc.roi).SetTo(rc.color, rc.mask)
        Next
    End Sub
End Class




Public Class Foreground_CellsBack : Inherits TaskParent
    Dim fore As New Foreground_Hist3D
    Public rcList As New List(Of rcData)
    Public Sub New()
        desc = "Get the background cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runRedC(src)

        fore.Run(src)
        dst3 = Not fore.dst2 And task.depthMask
        dst2.SetTo(0)
        For Each rc In task.rcList
            Dim tmp As cv.Mat = dst3(rc.roi) And rc.mask
            If tmp.CountNonZero Then dst2(rc.roi).SetTo(rc.color, rc.mask)
        Next
    End Sub
End Class
