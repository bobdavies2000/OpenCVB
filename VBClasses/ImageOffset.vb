Imports cv = OpenCvSharp
Public Class ImageOffset_Basics : Inherits TaskParent
    Public options As New Options_ImageOffset
    Dim options1 As New Options_Diff
    Public masks(2) As cv.Mat
    Public dst(2) As cv.Mat
    Public pcFiltered(2) As cv.Mat
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32FC1, New cv.Scalar(0))
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, New cv.Scalar(0))
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC1, New cv.Scalar(0))
        desc = "Compute various differences between neighboring pixels"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        options1.Run()

        Dim r1 = New cv.Rect(1, 1, task.cols - 2, task.rows - 2)
        Dim r2 As cv.Rect
        Select Case options.offsetDirection
            Case "Upper Left"
                r2 = New cv.Rect(0, 0, r1.Width, r1.Height)
            Case "Above"
                r2 = New cv.Rect(1, 0, r1.Width, r1.Height)
            Case "Upper Right"
                r2 = New cv.Rect(2, 0, r1.Width, r1.Height)
            Case "Left"
                r2 = New cv.Rect(0, 1, r1.Width, r1.Height)
            Case "Right"
                r2 = New cv.Rect(2, 1, r1.Width, r1.Height)
            Case "Lower Left"
                r2 = New cv.Rect(0, 2, r1.Width, r1.Height)
            Case "Below"
                r2 = New cv.Rect(1, 2, r1.Width, r1.Height)
            Case "Below Right"
                r2 = New cv.Rect(2, 2, r1.Width, r1.Height)
        End Select

        Dim r3 = New cv.Rect(1, 1, r1.Width, r1.Height)

        cv.Cv2.Absdiff(task.pcSplit(0)(r1), task.pcSplit(0)(r2), dst1(r3))
        cv.Cv2.Absdiff(task.pcSplit(1)(r1), task.pcSplit(1)(r2), dst2(r3))
        cv.Cv2.Absdiff(task.pcSplit(2)(r1), task.pcSplit(2)(r2), dst3(r3))

        dst = {dst1, dst2, dst3}
        For i = 0 To dst.Count - 1
            masks(i) = dst(i).Threshold(options1.pixelDiffThreshold, 255,
                                        cv.ThresholdTypes.BinaryInv).ConvertScaleAbs
            pcFiltered(i) = New cv.Mat(src.Size, cv.MatType.CV_32FC1, New cv.Scalar(0))
            task.pcSplit(i).CopyTo(pcFiltered(i), masks(i))
        Next
    End Sub
End Class






Public Class ImageOffset_SliceH : Inherits TaskParent
    Dim iOff As New ImageOffset_Basics
    Dim plot As New Plot_Points
    Dim options As New Options_SLR
    Dim slr As New SLR
    Dim mats As New Mat_4to1
    Public Sub New()
        labels(2) = "Upper left is pointcloud X, upper right pointcloud Y, bottom left pointcloud Z"
        desc = "Visualize a slice through the ImageOffsets_Basics images"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        iOff.Run(src)

        Dim pt = task.mouseMovePoint
        If standalone And task.mouseMovePoint.X = 0 And task.mouseMovePoint.Y = 0 Then
            pt = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        End If

        Dim slice As cv.Mat
        For i = 0 To 2
            slice = iOff.pcFiltered(i).Row(pt.Y)
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
                plot.input.Add(New cv.Point2d(CDbl(outputX(j)), CDbl(outputY(j))))
            Next

            plot.minY = Choose(i + 1, -task.xRange, -task.yRange, 0)
            plot.maxY = Choose(i + 1, task.xRange, task.yRange, task.MaxZmeters)
            plot.Run(src)

            mats.mat(i) = plot.dst2.Clone
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2

        Dim p1 = New cv.Point(0, pt.Y), p2 = New cv.Point(dst2.Width, pt.Y)
        task.color.Line(p1, p2, task.highlight, task.lineWidth)
        task.depthRGB.Line(p1, p2, task.highlight, task.lineWidth)
    End Sub
End Class







Public Class ImageOffset_SliceV : Inherits TaskParent
    Dim iOff As New ImageOffset_Basics
    Dim plot As New Plot_Points
    Dim options As New Options_SLR
    Dim slr As New SLR
    Dim mats As New Mat_4to1
    Public Sub New()
        labels(2) = "Upper left is pointcloud X, upper right pointcloud Y, bottom left pointcloud Z"
        desc = "Visualize a slice through the ImageOffsets_Basics images"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        iOff.Run(src)

        Dim pt = task.mouseMovePoint
        If standalone And task.mouseMovePoint.X = 0 And task.mouseMovePoint.Y = 0 Then
            pt = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        End If

        Dim slice As cv.Mat
        For i = 0 To 2
            slice = iOff.pcFiltered(i).Col(pt.X)
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
                plot.input.Add(New cv.Point2d(CDbl(outputX(j)), CDbl(outputY(j))))
            Next

            plot.minY = Choose(i + 1, -task.xRange, -task.yRange, 0)
            plot.maxy = Choose(i + 1, task.xRange, task.yRange, task.MaxZmeters)
            plot.Run(src)
            mats.mat(i) = plot.dst2.Clone
        Next

        mats.Run(emptyMat)
        dst2 = mats.dst2

        Dim p1 = New cv.Point(pt.X, 0), p2 = New cv.Point(pt.X, dst2.Height)
        task.color.Line(p1, p2, task.highlight, task.lineWidth)
        task.depthRGB.Line(p1, p2, task.highlight, task.lineWidth)
    End Sub
End Class





Public Class ImageOffset_Cloud : Inherits TaskParent
    Public Sub New()
        desc = "Create a pointcloud with the results of the imageOffset slices"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
    End Sub
End Class
