Imports cv = OpenCvSharp
Public Class BackY_Basics : Inherits VB_Algorithm
    Public singleTop As New View_SoloTop
    Public sideFull As New View_DynamicSide
    Public histogram As New cv.Mat
    Dim plot As New Plot_Histogram
    Public Sub New()
        labels = {"", "", "Side View histogram of Top View single counts", "Histogram of Y-values from image at left"}
        desc = "Use the single count histogram entries to find flat surfaces."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        singleTop.Run(src)
        dst2 = singleTop.dst3
        task.pointCloud.SetTo(0, Not dst0)

        'sideFull.Run(src)
        'dst2 = sideFull.dst2

        'dst1 = dst2.FindNonZero()
        'dst1.ConvertTo(dst1, cv.MatType.CV_32FC2)

        'If dst1.Width > 0 Then
        '    Dim ranges = New cv.Rangef() {New cv.Rangef(0, src.Height)}
        '    cv.Cv2.CalcHist({dst1}, {1}, New cv.Mat, histogram, 1, {src.Height}, ranges)
        '    plot.Run(histogram)
        '    dst3 = plot.dst2
        'End If
    End Sub
End Class








Public Class BackY_FlatSurfaces : Inherits VB_Algorithm
    Dim top As New BackY_SingleTop
    Dim surfaces As New List(Of Single)
    Public Sub New()
        findSlider("Y-Range X100").Value += 50 ' to get the point cloud into the histogram.
        labels = {"", "", "Side view histogram (thresholded) - singles from top view.", "Sum of each histogram row."}
        desc = "View the top view singles in a side view representation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        top.Run(src)

        'cv.Cv2.CalcHist({task.pointCloud}, task.channelsSide, top.maskSingles, dst2, 2, task.bins2D, task.rangesSide)
        'dst2.Col(0).SetTo(0)

        'Dim counts As New List(Of Single)
        'Dim yValues As New List(Of Single)
        'Dim ratio = task.yRange / task.yRangeDefault
        'For i = 0 To dst2.Height - 1
        '    Dim planeY = task.yRange * (i - task.sideCameraPoint.Y) / task.sideCameraPoint.Y
        '    counts.Add(dst2.Row(i).Sum.Item(0))
        '    yValues.Add(planeY * ratio)
        'Next

        'dst2 = dst2.Threshold(0, cv.Scalar.White, cv.ThresholdTypes.Binary)

        'Dim max = counts.Max
        'surfaces.Clear()
        'For i = 0 To counts.Count - 1
        '    If counts(i) >= max / 2 Then
        '        dst2.Line(New cv.Point(0, i), New cv.Point(dst2.Width, i), cv.Scalar.White, task.lineWidth)
        '        surfaces.Add(yValues(i))
        '        If i Mod 10 = 0 Then strOut += vbCrLf
        '    End If
        'Next

        'If heartBeat() Then
        '    strOut = "Flat surface at: "
        '    For i = 0 To surfaces.Count - 1
        '        strOut += Format(surfaces(i), fmt3) + ", "
        '        If i Mod 10 = 0 And i > 0 Then strOut += vbCrLf
        '    Next
        '    dst3.SetTo(cv.Scalar.Red)
        '    Dim barHeight = dst2.Height / counts.Count
        '    For i = 0 To counts.Count - 1
        '        Dim w = dst2.Width * counts(i) / max
        '        cv.Cv2.Rectangle(dst3, New cv.Rect(0, i * barHeight, w, barHeight), cv.Scalar.Black, -1)
        '    Next
        'End If
        'setTrueText(strOut, 2)
        'setTrueText("Camera at left", task.sideCameraPoint, 1)
    End Sub
End Class






Public Class BackY_Density2D : Inherits VB_Algorithm
    Dim flat As New BackY_FlatSurfaces
    Dim dense As New Density_Basics
    Public Sub New()
        desc = "Find flat surfaces using the low density metric"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dense.Run(src)

        flat.Run(task.pointCloud.SetTo(0, Not dense.dst2))
        dst2 = flat.dst2
        dst3 = flat.dst3
    End Sub
End Class







Public Class BackY_SingleTop : Inherits VB_Algorithm
    Dim sideFull As New View_DynamicSide
    Public maskSingles As New cv.Mat
    Public Sub New()
        labels = {"", "", "Top View histogram with single counts", "Backprojection mask of samples with a single count"}
        desc = "Find the Top View histogram entries with a single count"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim histogram As New cv.Mat
        If gOptions.gravityPointCloud.Checked Then
            sideFull.Run(src)
            histogram = sideFull.sideView.histogram
        Else
            If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
            cv.Cv2.CalcHist({src}, task.channelsTop, New cv.Mat, histogram, 2, task.bins2D, task.rangesTop)
        End If
        dst2 = histogram.InRange(1, 1)

        histogram.SetTo(0, Not dst2)
        cv.Cv2.CalcBackProject({src}, task.channelsTop, histogram, dst1, task.rangesTop)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        maskSingles = dst1.ConvertScaleAbs
        dst3 = task.color.Clone
        dst3.SetTo(cv.Scalar.White, maskSingles)
        setTrueText("camera at top", New cv.Point(dst2.Width / 2 + 5, 0), 2)
    End Sub
End Class