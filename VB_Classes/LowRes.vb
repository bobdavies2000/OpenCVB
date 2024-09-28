Imports MS.Internal
Imports cvb = OpenCvSharp

Public Class LowRes_Basics : Inherits VB_Parent
    Dim options As New Options_Resize
    Dim mapCells As New LowRes_Map
    Public Sub New()
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpInverseMap").Enabled = False
        labels(3) = "Low resolution version of the depthRGB image."
        desc = "Build the low-res image and accompanying map, rect list, and mask."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        task.lowResColor = src.Resize(New cvb.Size(task.lowResPercent * src.Width, task.lowResPercent * src.Height), 0, 0, options.warpFlag)
        dst2 = task.lowResColor.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        task.lowResDepth = task.depthRGB.Resize(New cvb.Size(task.lowResPercent * src.Width, task.lowResPercent * src.Height),
                                                0, 0, options.warpFlag)
        dst3 = task.lowResDepth.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        mapCells.Run(dst2)
        labels(2) = "There were " + CStr(task.lowRects.Count) + " cells found"
    End Sub
End Class





Public Class LowRes_Core : Inherits VB_Parent
    Dim options As New Options_Resize
    Public Sub New()
        desc = "The bare minimum needed to make the LowRes image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        dst3 = src.Resize(New cvb.Size(task.lowResPercent * src.Width, task.lowResPercent * src.Height), 0, 0, options.warpFlag)
        dst2 = dst3.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)
    End Sub
End Class








Public Class LowRes_Map : Inherits VB_Parent
    Dim lowGrid As New Grid_Basics
    Public Sub New()
        labels(2) = "Palettized version of task.lowGridMap - a map to translate points to grid rectangles."
        desc = "Map the individual pixels in the lowRes image to the full size image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            Static lowCore As New LowRes_Core
            lowCore.Run(src)
            src = lowCore.dst2
        End If

        If task.optionsChanged Then
            lowGrid.Run(src)
            dst2 = lowGrid.dst2
        End If
        SetTrueText("There were " + CStr(task.lowRects.Count) + " cells found", 3)
    End Sub
End Class






Public Class LowRes_Features : Inherits VB_Parent
    Dim feat As New Feature_Basics
    Dim lowRes As New LowRes_Basics
    Public Sub New()
        FindSlider("Min Distance to next").Value = 3
        dst3 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U, 0)
        labels(3) = "Featureless areas"
        desc = "Identify the cells with features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lowRes.Run(src)
        dst2 = lowRes.dst2.Clone

        feat.Run(src)

        Dim gridIndex As New List(Of Integer)
        Dim gridCounts As New List(Of Integer)

        task.featurePoints.Clear()
        Dim rects As New List(Of cvb.Rect)
        For Each pt In task.features
            Dim tile = task.lowGridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim test = gridIndex.IndexOf(tile)
            If test < 0 Then
                Dim r = task.lowRects(tile)
                rects.Add(r)
                gridIndex.Add(tile)
                gridCounts.Add(1)
                Dim p1 = New cvb.Point(r.X, r.Y)
                DrawCircle(dst2, p1, task.DotSize, task.HighlightColor)
                task.featurePoints.Add(p1)
            Else
                gridCounts(test) += 1
            End If
        Next

        task.featureRects.Clear()
        task.fLessRects.Clear()
        For Each r In task.lowRects
            If rects.Contains(r) Then task.featureRects.Add(r) Else task.fLessRects.Add(r)
        Next

        If task.gOptions.debugChecked Then
            For Each pt In task.features
                DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Black)
            Next
        End If
        If standaloneTest() Then
            dst3.SetTo(0)
            For Each r In rects
                dst3.Rectangle(r, cvb.Scalar.White, -1)
            Next
            dst3 = Not dst3
        End If
        If task.heartBeat Then
            labels(2) = CStr(task.featureRects.Count) + " cells had features while " + CStr(task.fLessRects.Count) + " had none"
        End If
    End Sub
End Class







Public Class LowRes_Edges : Inherits VB_Parent
    Public lowRes As New LowRes_Basics
    Dim edges As New Edge_Basics
    Public Sub New()
        FindRadio("Depth Region Boundaries").Enabled = False
        dst1 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U)
        labels = {"", "", "Low Res overlaid with edges", "Featureless spaces - no edges or features"}
        desc = "Add edges to features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lowRes.Run(src)
        dst2 = lowRes.dst2.Clone
        If task.heartBeat Then labels(2) = lowRes.labels(2)

        edges.Run(src)
        dst2.SetTo(cvb.Scalar.Black, edges.dst2)

        task.featureRects.Clear()
        task.fLessRects.Clear()
        dst1.SetTo(0)
        For Each r In task.lowRects
            If edges.dst2(r).CountNonZero = 0 Then
                task.fLessRects.Add(r)
                dst1(r).SetTo(255)
            Else
                task.featureRects.Add(r)
                DrawCircle(dst2, New cvb.Point(r.X, r.Y), task.DotSize, task.HighlightColor)
            End If
        Next

        dst3.SetTo(0)
        src.CopyTo(dst3, dst1)
    End Sub
End Class





Public Class LowRes_FeatureLess : Inherits VB_Parent
    Dim feat As New LowRes_Edges
    Public Sub New()
        task.gOptions.setDisplay1()
        desc = "Use ML to isolate featureless pixels."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Then

        End If
        feat.Run(src)
        dst2 = feat.dst2
        dst1 = feat.dst3

        dst3 = task.lowResColor.Clone
        dst3.SetTo(0)
        For Each r In task.fLessRects
            Dim index = task.lowGridMap.Get(Of Integer)(r.Y, r.X)
            Dim pt = task.ptPixel(index)
            Dim vec = task.lowResColor.Get(Of cvb.Vec3b)(pt.Y, pt.X)
            dst3.Set(Of cvb.Vec3b)(pt.Y, pt.X, vec)
        Next
    End Sub
End Class
