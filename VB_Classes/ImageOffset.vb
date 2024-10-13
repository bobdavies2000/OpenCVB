Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
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
    Dim plotSLR As New SLR_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Visualize a slice through the ImageOffsets_Basics images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
            plotSLR.slrCore.inputX.Clear()
            plotSLR.slrCore.inputY.Clear()
            For j = 0 To dst2.Width - 1
                plotSLR.slrCore.inputX.Add(j)
                plotSLR.slrCore.inputY.Add(slice.Get(Of Single)(0, j))
            Next
            plotSLR.Run(src)

            Select Case i
                Case 0
                    dst1 = plotSLR.dst2.Clone
                Case 1
                    dst2 = plotSLR.dst2.Clone
                Case 2
                    dst3 = plotSLR.dst2.Clone
            End Select
        Next

        task.color.Line(New cvb.Point(0, pt.Y), New cvb.Point(dst2.Width, pt.Y),
                        task.HighlightColor, task.lineWidth)
    End Sub
End Class







Public Class ImageOffset_SliceV : Inherits VB_Parent
    Dim iOff As New ImageOffset_Basics
    Dim plot As New Plot_PointsV
    Dim options As New Options_SLR
    Dim slr As New SLR
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Visualize a slice through the ImageOffsets_Basics images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
            SLR.SegmentedRegressionFast(inputX, inputY, options.tolerance, options.halfLength,
                                        outputX, outputY)
            plot.input.Clear()
            For j = 0 To outputX.Count - 1
                plot.input.Add(New cvb.Point2d(CDbl(outputX(j)), CDbl(outputY(j))))
            Next

            plot.Run(src)

            Select Case i
                Case 0
                    dst1 = plot.dst2.Clone
                Case 1
                    dst2 = plot.dst2.Clone
                Case 2
                    dst3 = plot.dst2.Clone
            End Select
        Next

        task.color.Line(New cvb.Point(pt.X, 0), New cvb.Point(pt.X, dst2.Height),
                        task.HighlightColor, task.lineWidth)
    End Sub
End Class





Public Class ImageOffset_Cloud : Inherits VB_Parent
    Public Sub New()
        desc = "Create a pointcloud with the results of the imageOffset slices"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
    End Sub
End Class
