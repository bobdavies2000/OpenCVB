Imports MS.Internal
Imports cvb = OpenCvSharp

Public Class LowRes_Basics : Inherits VB_Parent
    Dim options As New Options_Resize
    Public dst As New cvb.Mat
    Public dstDepth As New cvb.Mat
    Dim mapCells As New LowRes_Map
    Public Sub New()
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpInverseMap").Enabled = False
        labels(3) = "Low resolution version of the depthRGB image."
        desc = "Build the low-res image and accompanying map, rect list, and mask."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst = src.Resize(New cvb.Size(task.lowResPercent * src.Width, task.lowResPercent * src.Height), 0, 0, options.warpFlag)
        dst2 = dst.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        dstDepth = task.depthRGB.Resize(New cvb.Size(task.lowResPercent * src.Width, task.lowResPercent * src.Height),
                                        0, 0, options.warpFlag)
        dst3 = dstDepth.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        mapCells.Run(dst2)
    End Sub
End Class







Public Class LowRes_Map : Inherits VB_Parent
    Dim flood As New Grid_Basics
    Public Sub New()
        labels(3) = "Cell Map - CV_32S"
        desc = "Map the individual pixels in the lowRes image to the full size image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            Static options As New Options_Resize
            options.RunOpt()
            dst3 = src.Resize(New cvb.Size(task.lowResPercent * src.Width, task.lowResPercent * src.Height), 0, 0, options.warpFlag)
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






Public Class LowRes_FromReduction : Inherits VB_Parent
    Dim lowRes As New LowRes_Basics
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
        labels(2) = CStr(task.FeatureRects.Count) + " cells had features while " + CStr(task.FeaturelessRects.Count) + " had none"
    End Sub
End Class







Public Class Artifact_Edges : Inherits VB_Parent
    Public feat As New LowRes_Features
    Dim edges As New Edge_Basics
    Public Sub New()
        dst1 = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U)
        labels = {"", "", "Low Res overlaid with edges", "Featureless spaces - no edges or features"}
        desc = "Add edges to features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        If task.heartBeat Then labels(2) = feat.labels(2)

        edges.Run(src)
        dst2.SetTo(cvb.Scalar.Black, edges.dst2)

        Dim newFless As New List(Of cvb.Rect)
        dst1.SetTo(0)
        For Each r In task.FeaturelessRects
            Dim test = edges.dst2(r).CountNonZero
            If test > 0 Then
                task.FeatureRects.Add(r)
                ' DrawCircle(dst2, New cvb.Point(r.X, r.Y), task.DotSize, task.HighlightColor)
            Else
                newFless.Add(r)
                dst1(r).SetTo(255)
            End If
        Next

        dst3.SetTo(0)
        src.CopyTo(dst3, dst1)
    End Sub
End Class
