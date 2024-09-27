Imports MS.Internal
Imports cvb = OpenCvSharp

Public Class Artifact_LowRes : Inherits VB_Parent
    Dim options As New Options_Resize
    Public dst As New cvb.Mat
    Public dstDepth As New cvb.Mat
    Dim mapCells As New Artifact_MapCells
    Public Sub New()
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpInverseMap").Enabled = False
        labels(3) = "Low resolution version of the depthRGB image."
        desc = "Build a low-res image to start the process of finding artifacts."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim pct = options.resizePercent
        dst = src.Resize(New cvb.Size(pct * src.Width, pct * src.Height), 0, 0, options.warpFlag)
        dst2 = dst.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        dstDepth = task.depthRGB.Resize(New cvb.Size(pct * src.Width, pct * src.Height), 0, 0, options.warpFlag)
        dst3 = dstDepth.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        mapCells.Run(dst2)
    End Sub
End Class







Public Class Artifact_MapCells : Inherits VB_Parent
    Dim flood As New Flood_Artifacts
    Public Sub New()
        labels(3) = "Cell Map - CV_32S"
        desc = "Create the map of the artifacts."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            Static options As New Options_Resize
            options.RunOpt()
            Dim pct = options.resizePercent
            dst3 = src.Resize(New cvb.Size(pct * src.Width, pct * src.Height), 0, 0, options.warpFlag)
            dst2 = dst3.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)
        End If

        If task.optionsChanged Then
            flood.Run(src)
            dst3 = flood.dst2
            task.artifactMap = dst3
            task.artifactRects = New List(Of cvb.Rect)(flood.rectList)
            task.artifactMask = flood.dst3
        End If
        labels(2) = "There were " + CStr(flood.count) + " cells found"
    End Sub
End Class






Public Class Artifact_Reduction : Inherits VB_Parent
    Dim lowRes As New Artifact_LowRes
    Dim color8U As New Color8U_Basics
    Public Sub New()
        FindSlider("Resize Percentage (%)").Value = 40
        desc = "Build a lowRes image after reduction"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        color8U.Run(src)

        lowRes.Run(color8U.dst3)
        dst2 = lowRes.dst2
    End Sub
End Class







Public Class Artifact_CellSize : Inherits VB_Parent
    Public lowRes As New Artifact_LowRes
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






Public Class Artifact_FeatureCells1 : Inherits VB_Parent
    Dim cellSize As New Artifact_CellSize
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







Public Class Artifact_FeatureCells2 : Inherits VB_Parent
    Dim feat As New Feature_Basics
    Dim cellSize As New Artifact_CellSize
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








Public Class Artifact_Features : Inherits VB_Parent
    Dim feat As New Feature_Basics
    Dim lowRes As New Artifact_LowRes
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
        Dim featureRects As New List(Of cvb.Rect)
        For Each pt In task.features
            Dim tile = task.artifactMap.Get(Of Integer)(pt.Y, pt.X)
            Dim test = gridIndex.IndexOf(tile)
            If test < 0 Then
                Dim r = task.artifactRects(tile)
                featureRects.Add(r)
                gridIndex.Add(tile)
                gridCounts.Add(1)
                Dim p1 = New cvb.Point(r.X, r.Y)
                DrawCircle(dst2, p1, task.DotSize, task.HighlightColor)
                task.featurePoints.Add(p1)
            Else
                gridCounts(test) += 1
            End If
        Next

        task.FeatureRects.Clear()
        task.FeaturelessRects.Clear()
        For Each r In task.artifactRects
            If featureRects.Contains(r) Then task.FeatureRects.Add(r) Else task.FeaturelessRects.Add(r)
        Next

        If standaloneTest() Then
            For Each pt In task.features
                DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.Black)
            Next
            If task.gOptions.ShowGrid.Checked Then dst2.SetTo(cvb.Scalar.White, task.artifactMask)

            dst3.SetTo(0)
            For Each r In featureRects
                dst3.Rectangle(r, cvb.Scalar.White, -1)
            Next
            dst3 = Not dst3
        End If
    End Sub
End Class






Public Class Artifact_Edges : Inherits VB_Parent
    Public feat As New Artifact_Features
    Dim edges As New Edge_Basics
    Public Sub New()
        desc = "Add edges to features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst2 = feat.dst2


    End Sub
End Class
