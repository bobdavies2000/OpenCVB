Imports System.Windows.Forms
Imports cvb = OpenCvSharp
Public Class Foreground_Basics : Inherits VB_Parent
    Dim simK As New KMeans_Depth
    Public fgDepth As Single
    Public fg As New cvb.Mat, bg As New cvb.Mat, classCount As Integer
    Public Sub New()
        labels(3) = "Foreground - all the KMeans classes up to and including the first class over 1 meter."
        dst1 = New cvb.Mat(dst1.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Find the first KMeans class with depth over 1 meter and use it to define foreground"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        simK.Run(src)
        classCount = simK.classCount

        ' Order the KMeans classes from foreground to background using depth data.
        Dim depthMats As New List(Of cvb.Mat)
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
        dst2 = ShowPalette(dst1 * 255 / depthMats.Count)
        fg = task.pcSplit(2).Threshold(fgDepth, 255, cvb.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        dst0 = fg

        fg.SetTo(0, task.noDepthMask)
        bg = Not fg

        dst3.SetTo(0)
        src.CopyTo(dst3, fg)
        SetTrueText("KMeans classes are in dst1 - ordered by depth" + vbCrLf + "fg = foreground mask", 3)
        labels(2) = "KMeans output defining the " + CStr(classCount) + " classes"
    End Sub
End Class






Public Class Foreground_KMeans : Inherits VB_Parent
    Dim km As New KMeans_Image
    Public Sub New()
        FindSlider("KMeans k").Value = 2
        labels = {"", "", "Foreground Mask", "Background Mask"}
        dst2 = New cvb.Mat(task.WorkingRes, cvb.MatType.CV_8U, cvb.Scalar.All(0))
        dst3 = New cvb.Mat(task.WorkingRes, cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Separate foreground and background using Kmeans with k=2."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
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







Public Class Foreground_Contours : Inherits VB_Parent
    Public fore As New Foreground_Hist3D
    Dim contours As New Contour_General
    Public Sub New()
        desc = "Create contours for the foreground mask"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        fore.Run(src)

        contours.Run(fore.dst2)
        dst2 = contours.dst2
    End Sub
End Class






Public Class Foreground_Hist3D : Inherits VB_Parent
    Dim hcloud As New Hist3Dcloud_Basics
    Public Sub New()
        hcloud.maskInput = task.noDepthMask
        labels = {"", "", "Foreground", "Background"}
        desc = "Use the first class of hist3Dcloud_Basics as the definition of foreground"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        hcloud.Run(src)

        dst2.SetTo(0)
        dst2 = hcloud.dst2.InRange(1, 1) Or task.noDepthMask
        dst3 = Not dst2
    End Sub
End Class






Public Class Foreground_RedCloud : Inherits VB_Parent
    Dim fore As New Foreground_CellsFore
    Dim back As New Foreground_CellsBack
    Public Sub New()
        desc = "Isolate foreground from background, then segment each with RedCloud"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        fore.Run(src)
        dst2 = fore.dst2
        labels(2) = fore.labels(2)

        back.Run(src)
        dst3 = back.dst2
        labels(3) = back.labels(2)
        If task.redCells.Count > 0 Then
            dst2(task.rc.rect).SetTo(cvb.Scalar.White, task.rc.mask)
        End If
    End Sub
End Class





Public Class Foreground_CellsFore : Inherits VB_Parent
    Dim fore As New Foreground_Hist3D
    Public redC As New RedCloud_Basics
    Public redCells As New List(Of rcData)
    Public Sub New()
        task.redOptions.setIdentifyCells(False)
        task.redOptions.setUseColorOnly(True)
        desc = "Get the foreground cells"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        redC.Run(src)

        fore.Run(src)
        dst3 = fore.dst2 And task.depthMask
        dst2.SetTo(0)
        For Each rc In task.redCells
            Dim tmp As cvb.Mat = dst3(rc.rect) And rc.mask
            If tmp.CountNonZero Then dst2(rc.rect).SetTo(rc.color, rc.mask)
        Next
    End Sub
End Class




Public Class Foreground_CellsBack : Inherits VB_Parent
    Dim fore As New Foreground_Hist3D
    Public redC As New RedCloud_Basics
    Public redCells As New List(Of rcData)
    Public Sub New()
        task.redOptions.setIdentifyCells(False)
        task.redOptions.setUseColorOnly(True)
        desc = "Get the background cells"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        redC.Run(src)

        fore.Run(src)
        dst3 = Not fore.dst2 And task.depthMask
        dst2.SetTo(0)
        For Each rc In task.redCells
            Dim tmp As cvb.Mat = dst3(rc.rect) And rc.mask
            If tmp.CountNonZero Then dst2(rc.rect).SetTo(rc.color, rc.mask)
        Next
    End Sub
End Class
