Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports NAudio.Gui
Imports System.Windows
Public Class ImageOffset_Basics : Inherits VB_Parent
    Public options As New Options_ImageOffset
    Public masks(2) As cvb.Mat
    Public dsts(2) As cvb.Mat
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

        dsts = {dst1, dst2, dst3}
        For i = 0 To dsts.Count - 1
            masks(i) = dsts(i).Threshold(options.delta, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
            dsts(i).SetTo(0, masks(i))
        Next
    End Sub
End Class






Public Class ImageOffset_SliceH : Inherits VB_Parent
    Dim iOff As New ImageOffset_Basics
    Dim plot As New Plot_PointsH
    Dim options As New Options_SLR
    Dim slr As New SLR
    Dim mats As New Mat_4to1
    Public Sub New()
        labels(2) = "Upper left is pointcloud X, upper right pointcloud Y, bottom left pointcloud Z"
        desc = "Visualize a slice through the ImageOffsets_Basics images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        iOff.Run(src)

        Dim pt = task.mouseMovePoint
        If standalone And task.mouseMovePoint.X = 0 And task.mouseMovePoint.Y = 0 Then
            pt = New cvb.Point(dst2.Width / 2, dst2.Height / 2)
        End If

        Dim pcSplit(2) As cvb.Mat
        For i = 0 To 2
            pcSplit(i) = task.pcSplit(i).Clone
            pcSplit(i).SetTo(0, iOff.masks(i))
        Next

        Dim slice As cvb.Mat
        For i = 0 To 2
            slice = pcSplit(i).Row(pt.Y)
            Dim inputX As New List(Of Double)
            Dim inputY As New List(Of Double)
            For j = 0 To dst2.Width - 1
                inputX.Add(j)
                inputY.Add(slice.Get(Of Single)(0, j))
            Next

            Dim outputX As New List(Of Double)
            Dim outputY As New List(Of Double)
            slr.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                        outputX, outputY)
            plot.input.Clear()
            For j = 0 To outputX.Count - 1
                plot.input.Add(New cvb.Point2d(CDbl(outputX(j)), CDbl(outputY(j))))
            Next

            plot.Run(src)
            mats.mat(i) = plot.dst2.Clone
        Next

        mats.Run(empty)
        dst2 = mats.dst2

        Dim p1 = New cvb.Point(0, pt.Y), p2 = New cvb.Point(dst2.Width, pt.Y)
        task.color.Line(p1, p2, task.HighlightColor, task.lineWidth)
        task.depthRGB.Line(p1, p2, task.HighlightColor, task.lineWidth)
    End Sub
End Class







Public Class ImageOffset_SliceV : Inherits VB_Parent
    Dim iOff As New ImageOffset_Basics
    Dim plot As New Plot_PointsV
    Dim options As New Options_SLR
    Dim slr As New SLR
    Dim mats As New Mat_4to1
    Public Sub New()
        labels(2) = "Upper left is pointcloud X, upper right pointcloud Y, bottom left pointcloud Z"
        desc = "Visualize a slice through the ImageOffsets_Basics images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        iOff.Run(src)

        Dim pt = task.mouseMovePoint
        If standalone And task.mouseMovePoint.X = 0 And task.mouseMovePoint.Y = 0 Then
            pt = New cvb.Point(dst2.Width / 2, dst2.Height / 2)
        End If

        Dim pcSplit(2) As cvb.Mat
        For i = 0 To 2
            pcSplit(i) = task.pcSplit(i).Clone
            pcSplit(i).SetTo(0, iOff.masks(i))
        Next

        Dim slice As cvb.Mat
        For i = 0 To 2
            slice = pcSplit(i).Col(pt.X)
            Dim inputX As New List(Of Double)
            Dim inputY As New List(Of Double)
            For j = 0 To dst2.Height - 1
                inputX.Add(CDbl(j))
                inputY.Add(CDbl(slice.Get(Of Single)(j, 0)))
            Next

            Dim outputX As New List(Of Double)
            Dim outputY As New List(Of Double)
            slr.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                        outputX, outputY)
            plot.input.Clear()
            For j = 0 To outputX.Count - 1
                plot.input.Add(New cvb.Point2d(CDbl(outputX(j)), CDbl(outputY(j))))
            Next

            plot.Run(src)
            mats.mat(i) = plot.dst2.Clone
        Next

        mats.Run(empty)
        dst2 = mats.dst2

        Dim p1 = New cvb.Point(pt.X, 0), p2 = New cvb.Point(pt.X, dst2.Height)
        task.color.Line(p1, p2, task.HighlightColor, task.lineWidth)
        task.depthRGB.Line(p1, p2, task.HighlightColor, task.lineWidth)
    End Sub
End Class





Public Class ImageOffset_Cloud : Inherits VB_Parent
    Public Sub New()
        desc = "Create a pointcloud with the results of the imageOffset slices"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
    End Sub
End Class
