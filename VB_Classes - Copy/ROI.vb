Imports cvb = OpenCvSharp
Public Class ROI_Basics : Inherits TaskParent
    Public diff As New Diff_Basics
    Public aoiRect As cvb.Rect
    Public Sub New()
        labels = {"", "", "Enclosing rectangle of all pixels that have changed", ""}
        dst1 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8UC1, 0)
        task.gOptions.pixelDiffThreshold = 30
        desc = "Find the motion ROI in the latest image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        diff.Run(src)
        dst2 = diff.dst2

        Dim split = diff.dst2.FindNonZero().Split()
        If split.Length = 0 Then Exit Sub
        Dim mm0 = GetMinMax(split(0))
        Dim mm1 = GetMinMax(split(1))

        aoiRect = New cvb.Rect(mm0.minVal, mm1.minVal, mm0.maxVal - mm0.minVal, mm1.maxVal - mm1.minVal)

        If aoiRect.Width > 0 And aoiRect.Height > 0 Then
            task.color.Rectangle(aoiRect, cvb.Scalar.Yellow, task.lineWidth)
            dst2.Rectangle(aoiRect, white, task.lineWidth)
        End If
    End Sub
End Class






Public Class ROI_FindNonZeroNoSingle : Inherits TaskParent
    Public diff As New Diff_Basics
    Public aoiRect As cvb.Rect
    Public Sub New()
        labels = {"", "", "Enclosing rectangle of all changed pixels (after removing single pixels)", ""}
        dst1 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8UC1, 0)
        task.gOptions.pixelDiffThreshold = 30
        desc = "Find the motion ROI in just the latest image - eliminate single pixels"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        diff.Run(src)
        dst2 = diff.dst2
        Dim tmp = diff.dst2.FindNonZero()
        If tmp.Rows = 0 Then Exit Sub

        Dim minX = Integer.MaxValue, maxX = Integer.MinValue, minY = Integer.MaxValue, maxY = Integer.MinValue
        For i = 0 To tmp.Rows - 1
            Dim pt = tmp.Get(Of cvb.Point)(i, 0)
            ' eliminate single pixel differences.
            Dim r = New cvb.Rect(pt.X - 1, pt.Y - 1, 3, 3)
            If r.X < 0 Then r.X = 0
            If r.Y < 0 Then r.Y = 0
            If r.X + r.Width < dst2.Width And r.Y + r.Height < dst2.Height Then
                If dst2(r).CountNonZero > 1 Then
                    If minX > pt.X Then minX = pt.X
                    If maxX < pt.X Then maxX = pt.X
                    If minY > pt.Y Then minY = pt.Y
                    If maxY < pt.Y Then maxY = pt.Y
                End If
            End If
        Next
        If minX <> Integer.MaxValue Then
            aoiRect = New cvb.Rect(minX, minY, maxX - minX + 1, maxY - minY + 1)
            task.color.Rectangle(aoiRect, cvb.Scalar.Yellow, task.lineWidth)
            dst2.Rectangle(aoiRect, white, task.lineWidth)
        End If
    End Sub
End Class






Public Class ROI_AccumulateOld : Inherits TaskParent
    Public diff As New Diff_Basics
    Public aoiRect As cvb.Rect
    Public minX = Integer.MaxValue, maxX = Integer.MinValue, minY = Integer.MaxValue, maxY = Integer.MinValue
    Dim options As New Options_ROI
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "", "Area of Interest", ""}
        dst1 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8UC1, 0)
        task.gOptions.pixelDiffThreshold = 30
        desc = "Accumulate pixels in a motion ROI - all pixels that are different by X"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If aoiRect.Width * aoiRect.Height > src.Total * options.roiPercent Or task.optionsChanged Then
            dst0 = task.color
            dst1.SetTo(0)
            aoiRect = New cvb.Rect
            minX = Integer.MaxValue
            maxX = Integer.MinValue
            minY = Integer.MaxValue
            maxY = Integer.MinValue
        End If

        diff.Run(src)
        dst3 = diff.dst2
        cvb.Cv2.BitwiseOr(dst3, dst1, dst1)
        Dim tmp = dst3.FindNonZero()
        If aoiRect <> New cvb.Rect Then
            task.color(aoiRect).CopyTo(dst0(aoiRect))
            dst0.Rectangle(aoiRect, cvb.Scalar.Yellow, task.lineWidth)
            dst2.Rectangle(aoiRect, white, task.lineWidth)
        End If
        If tmp.Rows = 0 Then Exit Sub
        For i = 0 To tmp.Rows - 1
            Dim pt = tmp.Get(Of cvb.Point)(i, 0)
            If minX > pt.X Then minX = pt.X
            If maxX < pt.X Then maxX = pt.X
            If minY > pt.Y Then minY = pt.Y
            If maxY < pt.Y Then maxY = pt.Y
        Next
        aoiRect = New cvb.Rect(minX, minY, maxX - minX + 1, maxY - minY + 1)
        dst1.CopyTo(dst2)
        dst2.Rectangle(aoiRect, white, task.lineWidth)
    End Sub
End Class







Public Class ROI_Accumulate : Inherits TaskParent
    Public diff As New Diff_Basics
    Dim roiRect As cvb.Rect
    Dim options As New Options_ROI
    Public Sub New()
        labels = {"", "", "Area of Interest", ""}
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8UC1, 0)
        task.gOptions.pixelDiffThreshold = 30
        desc = "Accumulate pixels in a motion ROI until the size is x% of the total image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        SetTrueText(traceName + " is the same as ROI_AccumulateOld but simpler.", 3)
        If roiRect.Width * roiRect.Height > src.Total * options.roiPercent Or task.optionsChanged Then
            dst2.SetTo(0)
            roiRect = New cvb.Rect
        End If

        diff.Run(src)

        Dim split = diff.dst2.FindNonZero().Split()
        If split.Length > 0 Then
            Dim mm0 = GetMinMax(split(0))
            Dim mm1 = GetMinMax(split(1))

            Dim motionRect = New cvb.Rect(mm0.minVal, mm1.minVal, mm0.maxVal - mm0.minVal, mm1.maxVal - mm1.minVal)
            If motionRect.Width <> 0 And motionRect.Height <> 0 Then
                If roiRect.X > 0 Or roiRect.Y > 0 Then roiRect = motionRect.Union(roiRect) Else roiRect = motionRect
                cvb.Cv2.BitwiseOr(diff.dst2, dst2, dst2)
            End If
        End If
        dst2.Rectangle(roiRect, white, task.lineWidth)
        task.color.Rectangle(roiRect, task.HighlightColor, task.lineWidth)
    End Sub
End Class