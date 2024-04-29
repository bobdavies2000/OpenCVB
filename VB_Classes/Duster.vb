Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Duster_Basics : Inherits VB_Algorithm
    Dim dust As New Duster_MaskZ
    Public Sub New()
        desc = "Removed blowback in the pointcloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dust.Run(src)

        For i = 1 To dust.classCount
            Dim mask = dust.dst2.InRange(i, i)
            Dim depth = task.pcSplit(2).Mean(mask)
            task.pcSplit(2).SetTo(depth(0), mask)
        Next

        cv.Cv2.Merge(task.pcSplit, dst2)
        dst2.SetTo(0, Not dust.dst0)
        dst2.SetTo(0, task.maxDepthMask)

        dst3 = dust.dst3
    End Sub
End Class




Public Class Duster_BasicsY : Inherits VB_Algorithm
    Dim dust As New Duster_MaskZ
    Public Sub New()
        desc = "Removed blowback in the pointcloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dust.Run(src)

        For i = 1 To dust.classCount
            Dim mask = dust.dst2.InRange(i, i)
            Dim pcY = task.pcSplit(1).Mean(mask)
            task.pcSplit(1).SetTo(pcY(0), mask)
        Next

        cv.Cv2.Merge(task.pcSplit, dst2)
        dst2.SetTo(0, Not dust.dst0)
        dst2.SetTo(0, task.maxDepthMask)

        dst3 = dust.dst3
    End Sub
End Class





Public Class Duster_MaskZ : Inherits VB_Algorithm
    Public hist As New Histogram_Basics
    Public classCount As Integer
    Public options As New Options_GuidedBPDepth
    Public Sub New()
        labels(3) = "Any flickering below is from changes in the sorted order of the clusters.  It should not be a problem."
        desc = "Build a histogram that finds the clusters of depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        hist.bins = options.bins

        Dim src32f = task.pcSplit(2)
        task.maxDepthMask = src32f.InRange(task.maxZmeters, task.maxZmeters).ConvertScaleAbs()
        src32f.SetTo(task.maxZmeters, task.maxDepthMask)

        hist.fixedRanges = {New cv.Rangef(0.001, task.maxZmeters)}
        hist.Run(src32f)

        Dim histArray = hist.histArray

        ' this insures that the maxDepthMask is separate from any previous cluster
        histArray(histArray.Count - 1) = 0

        Dim start As Integer
        Dim clusters As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
        Dim lastEntry As Single
        Dim sampleCount As Integer

        For i = 0 To histArray.Count - 1
            If histArray(i) > 0 And lastEntry = 0 Then start = i
            If histArray(i) = 0 And lastEntry > 0 Then
                clusters.Add(sampleCount, New cv.Vec2i(start, i))
                sampleCount = 0
            End If
            lastEntry = histArray(i)
            sampleCount += histArray(i)
        Next

        Dim incr = task.maxZmeters / options.bins
        classCount = 0
        ReDim histArray(hist.histogram.Rows - 1)
        For i = 0 To Math.Min(clusters.Count, options.maxClusters) - 1
            Dim vec = clusters.ElementAt(i).Value
            classCount += 1
            For j = vec(0) To vec(1)
                histArray(j) = classCount
            Next
        Next

        Marshal.Copy(histArray, 0, hist.histogram.Data, histArray.Length)
        cv.Cv2.CalcBackProject({src32f}, {0}, hist.histogram, dst1, hist.ranges)
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)

        classCount += 1
        dst2.SetTo(classCount, task.maxDepthMask)

        dst3 = vbPalette(dst2 * 255 / classCount)
        If task.heartBeat Then labels(2) = "dst2 = CV_8U version of depth segmented into " + CStr(classCount) + " clusters."
        dst0 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Duster_MaskY : Inherits VB_Algorithm
    Public hist As New Histogram_Basics
    Public classCount As Integer
    Public options As New Options_GuidedBPDepth
    Public Sub New()
        hist.removeMax = True
        labels(3) = "Any flickering below is from changes in the sorted order of the clusters.  It should not be a problem."
        desc = "Build a histogram that finds the clusters of depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        hist.bins = options.bins

        Dim src32f = task.pcSplit(1)

        Dim mm = vbMinMax(src32f)
        Static maxY = mm.maxVal
        If maxY < mm.maxVal Then maxY = mm.maxVal
        hist.fixedRanges = {New cv.Rangef(-maxY, maxY)}
        hist.Run(src32f)

        Dim histArray = hist.histArray

        Dim start As Integer
        Dim clusters As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
        Dim lastEntry As Single
        Dim sampleCount As Integer
        For i = 0 To histArray.Count - 1
            If histArray(i) > 0 And lastEntry = 0 Then start = i
            If histArray(i) = 0 And lastEntry > 0 Then
                clusters.Add(sampleCount, New cv.Vec2i(start, i))
                sampleCount = 0
            End If
            lastEntry = histArray(i)
            sampleCount += histArray(i)
        Next

        Dim incr = task.maxZmeters / options.bins
        classCount = 0
        ReDim histArray(hist.histogram.Rows - 1)
        For i = 0 To Math.Min(clusters.Count, options.maxClusters) - 1
            Dim vec = clusters.ElementAt(i).Value
            classCount += 1
            For j = vec(0) To vec(1)
                histArray(j) = classCount
            Next
        Next

        Marshal.Copy(histArray, 0, hist.histogram.Data, histArray.Length)
        cv.Cv2.CalcBackProject({src32f}, {0}, hist.histogram, dst1, hist.ranges)
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)

        dst3 = vbPalette(dst2 * 255 / classCount)
        If task.heartBeat Then labels(2) = "dst2 = CV_8U version of depth segmented into " + CStr(classCount) + " clusters."
    End Sub
End Class
