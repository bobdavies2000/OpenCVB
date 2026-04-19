Imports cv = OpenCvSharp
Public Class Foreground_Basics_TA : Inherits TaskParent
    Dim hist As New Histogram_Depth
    Public foregroundMaxDepth As Single
    Public Sub New()
        task.gOptions.MaxDepthBar.Value = 5
        task.gOptions.HistBinBar.Value = task.gOptions.HistBinBar.Maximum
        desc = "Create a histogram of depth and find foreground as X% of points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            dst1 = task.pcSplit(2).Threshold(task.MaxZmeters, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs
            dst1.SetTo(0, task.noDepthMask)
            dst0 = task.pcSplit(2).Clone
            dst0.SetTo(0, Not dst1)

            hist.Run(dst0)
            dst2 = hist.dst2

            Dim histArray(hist.histogram.Total - 1) As Single
            hist.histogram.GetArray(Of Single)(histArray)

            Dim totalSamples = task.pcSplit(2).CountNonZero
            Dim accum As Single = 0
            Dim incr As Single = task.MaxZmeters / task.histogramBins
            foregroundMaxDepth = 0
            For i = 0 To histArray.Length - 1
                accum += histArray(i)
                foregroundMaxDepth += incr
                If accum >= totalSamples * 0.25 Then Exit For
            Next
        End If

        task.foregroundMask = task.pcSplit(2).Threshold(foregroundMaxDepth, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs
        task.foregroundMask.SetTo(0, task.noDepthMask)

        If standaloneTest() Then dst3 = task.foregroundMask
        labels(2) = "Foreground is defined as anything closer that " + Format(foregroundMaxDepth, fmt1) + " meters"
    End Sub
End Class






Public Class NR_Foreground_KMeansDepth : Inherits TaskParent
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
        dst2 = Palettize(dst1)
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






Public Class NR_Foreground_KMeans : Inherits TaskParent
    Dim km As New KMeans_Image
    Public Sub New()
        OptionParent.FindSlider("KMeans k").Value = 2
        labels = {"", "", "Foreground Mask", "Background Mask"}
        dst2 = New cv.Mat(New cv.Size(task.workRes.Width, task.workRes.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(New cv.Size(task.workRes.Width, task.workRes.Height), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Separate foreground and background using Kmeans with k=2."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.optionsChanged = True

        src = task.pcSplit(2).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        km.Run(src)

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







Public Class NR_Foreground_Hist3D : Inherits TaskParent
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
