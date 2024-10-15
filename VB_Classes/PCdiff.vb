Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class PCdiff_Basics : Inherits VB_Parent
    Public options As New Options_ImageOffset
    Public masks(2) As cvb.Mat
    Public dst(2) As cvb.Mat
    Public pcFiltered(2) As cvb.Mat
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        dst1 = New cvb.Mat(dst1.Size, cvb.MatType.CV_32FC1, New cvb.Scalar(0))
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_32FC1, New cvb.Scalar(0))
        dst3 = New cvb.Mat(dst3.Size, cvb.MatType.CV_32FC1, New cvb.Scalar(0))
        desc = "Compute various differences between neighboring pixels"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim r1 = New cvb.Rect(1, 1, task.cols - 2, task.rows - 2)
        Dim r2 As cvb.Rect
        Select Case options.offsetDirection
            Case "Upper Left"
                r2 = New cvb.Rect(0, 0, r1.Width, r1.Height)
            Case "Above"
                r2 = New cvb.Rect(1, 0, r1.Width, r1.Height)
            Case "Upper Right"
                r2 = New cvb.Rect(2, 0, r1.Width, r1.Height)
            Case "Left"
                r2 = New cvb.Rect(0, 1, r1.Width, r1.Height)
            Case "Right"
                r2 = New cvb.Rect(2, 1, r1.Width, r1.Height)
            Case "Lower Left"
                r2 = New cvb.Rect(0, 2, r1.Width, r1.Height)
            Case "Below"
                r2 = New cvb.Rect(1, 2, r1.Width, r1.Height)
            Case "Below Right"
                r2 = New cvb.Rect(2, 2, r1.Width, r1.Height)
        End Select

        Dim r3 = New cvb.Rect(1, 1, r1.Width, r1.Height)

        cvb.Cv2.Absdiff(task.pcSplit(0)(r1), task.pcSplit(0)(r2), dst1(r3))
        cvb.Cv2.Absdiff(task.pcSplit(1)(r1), task.pcSplit(1)(r2), dst2(r3))
        cvb.Cv2.Absdiff(task.pcSplit(2)(r1), task.pcSplit(2)(r2), dst3(r3))

        dst = {dst1, dst2, dst3}
        For i = 0 To dst.Count - 1
            masks(i) = dst(i).Threshold(options.delta, 255, cvb.ThresholdTypes.BinaryInv).ConvertScaleAbs
            pcFiltered(i) = New cvb.Mat(src.Size, cvb.MatType.CV_32FC1, New cvb.Scalar(0))
            task.pcSplit(i).CopyTo(pcFiltered(i), masks(i))
        Next
    End Sub
End Class






Public Class PCdiff_Filter : Inherits VB_Parent
    Public pcDiff As New PCdiff_Basics
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, New cvb.Scalar(0))
        dst3 = New cvb.Mat(dst3.Size, cvb.MatType.CV_32FC3)
        desc = "Filter the pointcloud to isolate only pixels within X mm's of it neighbor"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        pcDiff.Run(src)

        Dim delta = pcDiff.options.delta
        dst2.SetTo(0)
        For y = 0 To dst2.Height - 1
            For x = 0 To dst2.Width - 1
                Dim xVal = pcDiff.dst1.Get(Of Single)(y, x)
                Dim yVal = pcDiff.dst2.Get(Of Single)(y, x)
                Dim zVal = pcDiff.dst3.Get(Of Single)(y, x)
                If xVal <= delta And yVal <= delta And zVal <= delta Then
                    dst2.Set(Of Byte)(y, x, 255)
                End If
            Next
        Next
        dst2.SetTo(0, task.noDepthMask)
        dst3.SetTo(0)
        task.pointCloud.CopyTo(dst3, dst2)
    End Sub
End Class






Public Class PCdiff_Points : Inherits VB_Parent
    Dim filter As New PCdiff_Filter
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        labels = {"", "", "Point cloud with clean breaks between objects",
                          "Pixels removed to make clean breaks in the depth data"}
        desc = "Review the filtered PCdiff output."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        filter.Run(src)
        task.pcSplit = filter.dst3.Split()

        dst2.SetTo(0)
        Dim countInf As Integer
        Dim delta = filter.pcDiff.options.delta
        For y = 0 To task.pcSplit(2).Rows - 1
            Dim slice = task.pcSplit(2).Row(y)
            Dim samples(slice.Total - 1) As Single
            Marshal.Copy(slice.Data, samples, 0, samples.Length)

            Dim lastVal As Single = 0
            For x = 0 To slice.Cols - 2
                Dim val = slice.Get(Of Single)(0, x)
                If Single.IsInfinity(val) Then
                    val = 10000000
                    countInf += 1
                End If
                If val <> 0 Then
                    If Math.Abs(val - lastVal) > delta Then
                        task.pcSplit(2).Set(Of Single)(y, x, 0) ' neighbors cannot have diff > delta.
                        dst2.Set(Of Byte)(y, x, 255)
                    End If
                End If
                lastVal = val
            Next
        Next

        task.noDepthMask = task.pcSplit(2).InRange(0, 0)
        cvb.Cv2.Merge(task.pcSplit, dst3)
    End Sub
End Class





Public Class PCdiff_GuidedBP : Inherits VB_Parent
    Dim points As New PCdiff_Points
    Dim backP As New GuidedBP_TopView
    Public Sub New()
        desc = "Isolate the objects found in the contiguous depth"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        points.Run(src)

        cvb.Cv2.Merge(task.pcSplit, task.pointCloud)
        backP.Run(task.pointCloud)
        dst2 = backP.dst2
        labels(2) = backP.labels(2)
    End Sub
End Class
