Imports cv = OpenCvSharp
Public Class ROI_Basics : Inherits VB_Algorithm
    Public diff As New Diff_Basics
    Public aoiRect As cv.Rect
    Public Sub New()
        labels = {"", "", "Enclosing rectangle of all pixels that have changed", ""}
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC1, 0)
        gOptions.PixelDiffThreshold.Value = 30
        desc = "Find the motion ROI in the latest image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        diff.Run(src)
        dst2 = diff.dst3

        Dim split = diff.dst3.FindNonZero().Split()
        If split.Length = 0 Then Exit Sub
        Dim mm0 = vbMinMax(split(0))
        Dim mm1 = vbMinMax(split(1))

        aoiRect = New cv.Rect(mm0.minVal, mm1.minVal, mm0.maxVal - mm0.minVal, mm1.maxVal - mm1.minVal)

        If aoiRect.Width > 0 And aoiRect.Height > 0 Then
            task.color.Rectangle(aoiRect, cv.Scalar.Yellow, task.lineWidth)
            dst2.Rectangle(aoiRect, cv.Scalar.White, task.lineWidth)
        End If
    End Sub
End Class






Public Class ROI_FindNonZeroNoSingle : Inherits VB_Algorithm
    Public diff As New Diff_Basics
    Public aoiRect As cv.Rect
    Public Sub New()
        labels = {"", "", "Enclosing rectangle of all changed pixels (after removing single pixels)", ""}
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC1, 0)
        gOptions.PixelDiffThreshold.Value = 30
        desc = "Find the motion ROI in just the latest image - eliminate single pixels"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        diff.Run(src)
        dst2 = diff.dst3
        Dim tmp = diff.dst3.FindNonZero()
        If tmp.Rows = 0 Then Exit Sub

        Dim minX = Integer.MaxValue, maxX = Integer.MinValue, minY = Integer.MaxValue, maxY = Integer.MinValue
        For i = 0 To tmp.Rows - 1
            Dim pt = tmp.Get(Of cv.Point)(i, 0)
            ' eliminate single pixel differences.
            Dim r = New cv.Rect(pt.X - 1, pt.Y - 1, 3, 3)
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
            aoiRect = New cv.Rect(minX, minY, maxX - minX + 1, maxY - minY + 1)
            task.color.Rectangle(aoiRect, cv.Scalar.Yellow, task.lineWidth)
            dst2.Rectangle(aoiRect, cv.Scalar.White, task.lineWidth)
        End If
    End Sub
End Class






Public Class ROI_AccumulateOld : Inherits VB_Algorithm
    Public diff As New Diff_Basics
    Public aoiRect As cv.Rect
    Public minX = Integer.MaxValue, maxX = Integer.MinValue, minY = Integer.MaxValue, maxY = Integer.MinValue
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        labels = {"", "", "Area of Interest", ""}
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC1, 0)
        gOptions.PixelDiffThreshold.Value = 30
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max size area of interest %", 0, 100, 25)
        desc = "Accumulate pixels in a motion ROI - all pixels that are different by X"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim aoiSlider = findSlider("Max size area of interest %")
        Dim aoiPercent = aoiSlider.Value / 100
        If aoiRect.Width * aoiRect.Height > src.Total * aoiPercent Or task.optionsChanged Then
            dst0 = task.color
            dst1.SetTo(0)
            aoiRect = New cv.Rect
            minX = Integer.MaxValue
            maxX = Integer.MinValue
            minY = Integer.MaxValue
            maxY = Integer.MinValue
        End If

        diff.Run(src)
        dst3 = diff.dst3
        cv.Cv2.BitwiseOr(dst3, dst1, dst1)
        Dim tmp = dst3.FindNonZero()
        If aoiRect <> New cv.Rect Then
            task.color(aoiRect).CopyTo(dst0(aoiRect))
            dst0.Rectangle(aoiRect, cv.Scalar.Yellow, task.lineWidth)
            dst2.Rectangle(aoiRect, cv.Scalar.White, task.lineWidth)
        End If
        If tmp.Rows = 0 Then Exit Sub
        For i = 0 To tmp.Rows - 1
            Dim pt = tmp.Get(Of cv.Point)(i, 0)
            If minX > pt.X Then minX = pt.X
            If maxX < pt.X Then maxX = pt.X
            If minY > pt.Y Then minY = pt.Y
            If maxY < pt.Y Then maxY = pt.Y
        Next
        aoiRect = New cv.Rect(minX, minY, maxX - minX + 1, maxY - minY + 1)
        dst1.CopyTo(dst2)
        dst2.Rectangle(aoiRect, cv.Scalar.White, task.lineWidth)
    End Sub
End Class







Public Class ROI_Accumulate : Inherits VB_Algorithm
    Public diff As New Diff_Basics
    Dim roiRect As cv.Rect
    Public Sub New()
        labels = {"", "", "Area of Interest", ""}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC1, 0)
        gOptions.PixelDiffThreshold.Value = 30
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max size area of interest %", 0, 100, 25)
        desc = "Accumulate pixels in a motion ROI until the size is x% of the total image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        setTrueText(traceName + " is the same as ROI_AccumulateOld but simpler.", 3)
        Dim roiSlider = findSlider("Max size area of interest %")
        Dim roiPercent = roiSlider.Value / 100
        If roiRect.Width * roiRect.Height > src.Total * roiPercent Or task.optionsChanged Then
            dst2.SetTo(0)
            roiRect = New cv.Rect
        End If

        diff.Run(src)

        Dim split = diff.dst3.FindNonZero().Split()
        If split.Length > 0 Then
            Dim mm0 = vbMinMax(split(0))
            Dim mm1 = vbMinMax(split(1))

            Dim motionRect = New cv.Rect(mm0.minVal, mm1.minVal, mm0.maxVal - mm0.minVal, mm1.maxVal - mm1.minVal)
            If motionRect.Width <> 0 And motionRect.Height <> 0 Then
                If roiRect.X > 0 Or roiRect.Y > 0 Then roiRect = motionRect.Union(roiRect) Else roiRect = motionRect
                cv.Cv2.BitwiseOr(diff.dst3, dst2, dst2)
            End If
        End If
        dst2.Rectangle(roiRect, cv.Scalar.White, task.lineWidth)
        task.color.Rectangle(roiRect, task.highlightColor, task.lineWidth)
    End Sub
End Class