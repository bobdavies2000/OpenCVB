Imports MS.Internal
Imports cvb = OpenCvSharp

Public Class Artifacts_LowRes : Inherits VB_Parent
    Dim options As New Options_Resize
    Public dst As New cvb.Mat
    Public dstDepth As New cvb.Mat
    Public Sub New()
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpInverseMap").Enabled = False
        desc = "Build a low-res image to start the process of finding artifacts."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim pct = options.resizePercent
        dst = src.Resize(New cvb.Size(pct * src.Width, pct * src.Height), 0, 0, options.warpFlag)
        dst2 = dst.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        dstDepth = task.depthRGB.Resize(New cvb.Size(pct * src.Width, pct * src.Height), 0, 0, options.warpFlag)
        dst3 = dstDepth.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)
    End Sub
End Class







Public Class Artifacts_Reduction : Inherits VB_Parent
    Dim lowRes As New Artifacts_LowRes
    Dim color8U As New Color8U_Basics
    Public Sub New()
        desc = "Build a lowRes image after reduction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        color8U.Run(src)

        lowRes.Run(color8U.dst3)
        dst2 = lowRes.dst3
    End Sub
End Class






Public Class Artifacts_Features : Inherits VB_Parent
    Dim lowRes As New Artifacts_LowRes
    Dim feat As New Feature_Basics
    Public Sub New()
        task.redOptions.ColorSource.SelectedItem() = "Color8U_Grayscale"
        FindSlider("Resize Percentage (%)").Value = 20
        labels = {"", "", "LowRes image with highlighted features", "Same features highlighted in the original image."}
        desc = "Find features in a low res image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lowRes.Run(src)
        feat.Run(lowRes.dst2)
        dst2 = feat.dst2

        dst3 = src
        For Each pt In task.features
            DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class






Public Class Artifacts_CellSize : Inherits VB_Parent
    Public lowRes As New Artifacts_LowRes
    Dim recompute As Boolean = True
    Public distance As Integer
    Public Sub New()
        desc = "Identify the cell size from the Low Res image features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Then recompute = True

        lowRes.Run(src)
        dst2 = lowRes.dst2

        If recompute Then
            Dim OffsetX As New List(Of Integer)
            For y = 0 To dst2.Height - 2 Step 10
                For x = 0 To dst2.Width - 2
                    Dim v1 = dst2.Get(Of cvb.Vec3b)(y, x)
                    Dim v2 = dst2.Get(Of cvb.Vec3b)(y, x + 1)
                    If v1 <> v2 Then OffsetX.Add(x + 1)
                Next
            Next

            Dim OffsetY As New List(Of Integer)
            For y = 0 To dst2.Height - 2
                For x = 0 To dst2.Width - 2 Step 10
                    Dim v1 = dst2.Get(Of cvb.Vec3b)(y, x)
                    Dim v2 = dst2.Get(Of cvb.Vec3b)(y + 1, x)
                    If v1 <> v2 Then OffsetY.Add(y + 1)
                Next
            Next

            Dim lastOffset As Integer
            Dim offsets As New List(Of Integer)
            For Each offset In OffsetX
                If offset - lastOffset > 0 Then
                    offsets.Add(offset - lastOffset)
                    lastOffset = offset
                End If
            Next

            lastOffset = 0
            For Each offset In OffsetY
                If offset - lastOffset > 0 Then
                    offsets.Add(offset - lastOffset)
                    lastOffset = offset
                End If
            Next

            If offsets.Count = 0 Then Exit Sub ' try again later...
            distance = offsets.Average
            recompute = False
            If distance < 3 Then distance = distance Or 3
            task.gOptions.setGridSize(distance)
            task.grid.Run(src)
        End If
        SetTrueText(CStr(CInt(distance)) + " is the cell size (square) ", 3)
    End Sub
End Class






Public Class Artifacts_FeatureCells1 : Inherits VB_Parent
    Dim cellSize As New Artifacts_CellSize
    Dim feat As New Feature_Basics
    Public Sub New()
        FindSlider("Min Distance to next").Value = 3
        desc = "Identify the cells with features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Or cellSize.distance = 0 Then cellSize.Run(src)

        If cellSize.distance <> 0 Then
            feat.Run(cellSize.lowRes.dst2)
            dst2 = cellSize.lowRes.dst2.Clone

            task.featurePoints.Clear()
            For Each pt In task.features
                Dim p1 = New cvb.Point2f(pt.X - (pt.X Mod cellSize.distance), pt.Y - (pt.Y Mod cellSize.distance))
                DrawCircle(dst2, p1, task.DotSize, task.HighlightColor)
                task.featurePoints.Add(p1)
            Next
            If standaloneTest() Then
                feat.Run(src)
                For Each pt In task.features
                    DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Black)
                Next
            End If
        End If
    End Sub
End Class







Public Class Artifacts_FeatureCells : Inherits VB_Parent
    Dim feat As New Feature_Basics
    Dim cellSize As New Artifacts_CellSize
    Public Sub New()
        FindSlider("Min Distance to next").Value = 3
        desc = "Identify the cells with features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged Or cellSize.distance = 0 Then cellSize.Run(src)

        If cellSize.distance <> 0 Then
            feat.Run(src)
            dst2 = cellSize.lowRes.dst2.Clone

            Dim gridIndex As New List(Of Integer)
            Dim gridCounts As New List(Of Integer)

            task.featurePoints.Clear()
            For Each pt In task.features
                Dim tile = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim test = gridIndex.IndexOf(tile)
                If test < 0 Then
                    Dim r = task.gridRects(tile)
                    gridIndex.Add(tile)
                    gridCounts.Add(1)
                    Dim p1 = New cvb.Point(r.X, r.Y)
                    DrawCircle(dst2, p1, task.DotSize, task.HighlightColor)
                    task.featurePoints.Add(p1)
                Else
                    gridCounts(test) += 1
                End If
            Next

            If standaloneTest() Then
                dst3.SetTo(0)
                For Each pt In task.features
                    DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Black)
                    DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
                Next
            End If
        End If
    End Sub
End Class







Public Class Artifacts_CellMap : Inherits VB_Parent
    Dim flood As New Flood_Simple
    Dim lowRes As New Artifacts_LowRes
    Public Sub New()
        labels(3) = "Cell Map - CV_32S"
        desc = "Create the map of the artifacts."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.optionsChanged = False Then Exit Sub

        lowRes.Run(src)
        dst2 = lowRes.dst2

        flood.Run(dst2)
        dst3 = flood.dst2
        task.artifactMap = dst3
        labels(2) = "There were " + CStr(flood.count) + " cells found"
    End Sub
End Class
