Imports System.Windows.Forms
Imports cv = OpenCvSharp
Public Class Foreground_Basics : Inherits VB_Algorithm
    Dim simK As New KMeans_Depth
    Public fgDepth As Single
    Public fg As New cv.Mat, bg As New cv.Mat, classCount As Integer
    Public Sub New()
        labels(3) = "Foreground - all the KMeans classes up to and including the first class over 1 meter."
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Find the first KMeans class with depth over 1 meter and use it to define foreground"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
        For Each el In sortedMats
            fgDepth = el.Key
            If fgDepth >= 1 Then Exit For ' find all the regions closer than a meter (inclusive)
        Next

        For Each el In sortedMats
            Dim tmp = depthMats(el.Value)
            dst1.SetTo(el.Value + 1, tmp)
        Next
        dst2 = vbPalette(dst1 * 255 / depthMats.Count)
        fg = task.pcSplit(2).Threshold(fgDepth, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        dst0 = fg

        fg.SetTo(0, task.noDepthMask)
        bg = Not fg

        dst3.SetTo(0)
        src.CopyTo(dst3, fg)
        setTrueText("KMeans classes are in dst1 - ordered by depth" + vbCrLf + "fg = foreground mask", 3)
        labels(2) = "KMeans output defining the " + CStr(classCount) + " classes"
    End Sub
End Class






Public Class Foreground_KMeans2 : Inherits VB_Algorithm
    Dim km As New KMeans_Image
    Public Sub New()
        findSlider("KMeans k").Value = 2
        labels = {"", "", "Foreground Mask", "Background Mask"}
        dst2 = New cv.Mat(task.workingRes, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(task.workingRes, cv.MatType.CV_8U, 0)
        desc = "Separate foreground and background using Kmeans with k=2."
    End Sub
    Public Sub RunVB(src As cv.Mat)
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







Public Class Foreground_Contours : Inherits VB_Algorithm
    Public fore As New Foreground_Hist3D
    Dim contours As New Contour_Basics
    Public Sub New()
        desc = "Create contours for the foreground mask"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fore.Run(src)

        contours.Run(fore.dst2)
        dst2 = contours.dst2
    End Sub
End Class






Public Class Foreground_Hist3D : Inherits VB_Algorithm
    Dim hcloud As New Hist3Dcloud_Basics
    Public Sub New()
        hcloud.maskInput = task.noDepthMask
        labels = {"", "", "Foreground", "Background"}
        advice = hcloud.advice
        desc = "Use the first class of hist3Dcloud_Basics as the definition of foreground"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hcloud.Run(src)

        dst2.SetTo(0)
        dst2 = hcloud.dst2.InRange(1, 1) Or task.noDepthMask
        dst3 = Not dst2
    End Sub
End Class





Public Class Foreground_RedMinFront : Inherits VB_Algorithm
    Dim fore As New Foreground_Hist3D
    Public rMin As New RedMin_Basics
    Dim hist3D As New Hist3D_DepthWithMask
    Public minCells As New List(Of segCell)
    Public Sub New()
        redOptions.UseColor.Checked = True
        advice = "redOptions '3D Histogram Bins' " + vbCrLf + "redOptions other 'Histogram 3D Options'"
        desc = "Run the foreground through RedCloud_Basics "
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fore.Run(src)

        hist3D.depthMask = fore.dst2 Or task.noDepthMask
        hist3D.Run(src)

        rMin.minCore.inputMask = Not hist3D.depthMask
        rMin.Run(hist3D.dst2)

        dst2 = rMin.dst3.Clone
        labels(2) = rMin.labels(3)
        If task.cellSelect.index <> 0 Then dst2(task.cellSelect.rect).SetTo(cv.Scalar.White, task.cellSelect.mask)
    End Sub
End Class





Public Class Foreground_RedMinBack : Inherits VB_Algorithm
    Dim fore As New Foreground_Hist3D
    Public rMin As New RedMin_Basics
    Dim hist3D As New Hist3D_DepthWithMask
    Public minCells As New List(Of segCell)
    Public Sub New()
        redOptions.UseColor.Checked = True
        advice = "redOptions '3D Histogram Bins' " + vbCrLf + "redOptions other 'Histogram 3D Options'"
        desc = "Run the foreground through RedCloud_Basics "
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fore.Run(src)

        hist3D.depthMask = fore.dst3 Or task.noDepthMask
        hist3D.Run(src)

        rMin.minCore.inputMask = Not hist3D.depthMask
        rMin.Run(hist3D.dst2)

        dst2 = rMin.dst3.Clone
        dst2.SetTo(0, fore.dst2)

        labels(2) = rMin.labels(3)
        If task.cellSelect.index <> 0 Then dst2(task.cellSelect.rect).SetTo(cv.Scalar.White, task.cellSelect.mask)
    End Sub
End Class





Public Class Foreground_RedMin : Inherits VB_Algorithm
    Dim fore As New Foreground_RedMinFront
    Dim back As New Foreground_RedMinBack
    Public Sub New()
        advice = "redOptions '3D Histogram Bins' " + vbCrLf + "redOptions other 'Histogram 3D Options'"
        desc = "Isolate foreground from background, then segment each with RedMin"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fore.Run(src)
        dst2 = fore.dst2
        labels(2) = fore.labels(2)

        back.Run(src)
        dst3 = back.dst2
        labels(3) = back.labels(2)
        If task.cellSelect.index <> 0 Then dst2(task.cellSelect.rect).SetTo(cv.Scalar.White, task.cellSelect.mask)
    End Sub
End Class

