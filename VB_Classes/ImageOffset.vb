Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class ImageOffset_Basics : Inherits VB_Parent
    Public options As New Options_ImageOffset
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

        Dim maskX = dst1.Threshold(options.delta, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
        Dim maskY = dst2.Threshold(options.delta, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs
        Dim maskZ = dst3.Threshold(options.delta, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs

        dst1.SetTo(0, maskX)
        dst2.SetTo(0, maskY)
        dst3.SetTo(0, maskZ)
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
        If standalone And task.mouseMovePoint = New cvb.Point Then
            pt = New cvb.Point(dst2.Width / 2, dst2.Height / 2)
        End If

        Dim slice As cvb.Mat
        For i = 0 To 2
            slice = task.pcSplit(i).Row(pt.Y)
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
    Dim plotSLR As New SLR_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Visualize a slice through the ImageOffsets_Basics images"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        iOff.Run(src)

        Dim pt = task.mouseMovePoint
        If standalone And task.mouseMovePoint = New cvb.Point Then
            pt = New cvb.Point(dst2.Width / 2, dst2.Height / 2)
        End If

        Dim slice As cvb.Mat
        For i = 0 To 2
            slice = task.pcSplit(i).Col(pt.X)
            plotSLR.slrCore.inputX.Clear()
            plotSLR.slrCore.inputY.Clear()
            For j = 0 To dst2.Height - 1
                plotSLR.slrCore.inputX.Add(j)
                plotSLR.slrCore.inputY.Add(slice.Get(Of Single)(j, 0))
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
