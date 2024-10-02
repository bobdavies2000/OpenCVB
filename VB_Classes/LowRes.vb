Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp

Public Class LowRes_Basics : Inherits VB_Parent
    Dim options As New Options_Resize
    Dim mapCells As New LowRes_Map
    Dim optGrid As New Options_GridFromResize
    Public Sub New()
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpInverseMap").Enabled = False
        FindRadio("Area").Checked = True
        labels(3) = "Low resolution version of the depth data."
        desc = "Build the low-res image and accompanying map, rect list, and mask."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        optGrid.RunOpt()

        task.lowResColor = src.Resize(New cvb.Size(optGrid.lowResPercent * src.Width, optGrid.lowResPercent * src.Height), 0, 0, options.warpFlag)
        dst2 = task.lowResColor.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        task.lowResDepth = task.pcSplit(2).Resize(New cvb.Size(optGrid.lowResPercent * src.Width,
                                                               optGrid.lowResPercent * src.Height), 0, 0, options.warpFlag)
        dst3 = task.lowResDepth.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        mapCells.Run(dst2)
        labels(2) = "There were " + CStr(task.lrRectsByRow.Count) + " cells found"
    End Sub
End Class





Public Class LowRes_Core : Inherits VB_Parent
    Dim options As New Options_Resize
    Dim optGrid As New Options_GridFromResize
    Public Sub New()
        FindRadio("Area").Checked = True
        desc = "The bare minimum needed to make the LowRes image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        optGrid.RunOpt()

        dst3 = src.Resize(New cvb.Size(optGrid.lowResPercent * src.Width, optGrid.lowResPercent * src.Height), 0, 0, options.warpFlag)
        dst2 = dst3.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)
    End Sub
End Class








Public Class LowRes_Map : Inherits VB_Parent
    Dim lowGrid As New Grid_Basics
    Public Sub New()
        labels(2) = "task.lrFullSizeMap - a CV_32S map to translate points in a full-size image to an index in task.lrAllRects"
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
        SetTrueText("There were " + CStr(task.lrAllRects.Count) + " cells found", 3)
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
            Dim tile = task.lrFullSizeMap.Get(Of Integer)(pt.Y, pt.X)
            Dim test = gridIndex.IndexOf(tile)
            If test < 0 Then
                Dim r = task.lrAllRects(tile)
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
        For Each r In task.lrAllRects
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

        edges.Run(src)
        dst2.SetTo(0, edges.dst2)

        task.featureRects.Clear()
        task.fLessRects.Clear()
        dst1.SetTo(0)
        Dim flist As New List(Of Single)
        For Each rectlist In task.lrRectsByRow
            For Each r In rectlist
                If edges.dst2(r).CountNonZero = 0 Then
                    task.fLessRects.Add(r)
                    dst1(r).SetTo(255)
                    flist.Add(1)
                Else
                    task.featureRects.Add(r)
                    DrawCircle(dst2, New cvb.Point(r.X, r.Y), task.DotSize, task.HighlightColor)
                    flist.Add(2)
                End If
            Next
        Next

        task.lrSmallMap = cvb.Mat.FromPixelData(task.lowResColor.Height, task.lowResColor.Width,
                                                cvb.MatType.CV_32F, flist.ToArray)

        dst3.SetTo(0)
        src.CopyTo(dst3, dst1)
        If task.heartBeat Then
            labels(2) = CStr(task.featureRects.Count) + " cells with features were found"
            labels(3) = CStr(task.fLessRects.Count) + " cells without features were found"
        End If
    End Sub
End Class






Public Class LowRes_MLNoDepth : Inherits VB_Parent
    Dim feat As New LowRes_Edges
    Dim indexLowRes As New cvb.Mat
    Dim indexHighRes As New cvb.Mat
    Dim ml As New ML_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Train an ML tree to predict each pixel of the full size image using only color and position."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst0 = feat.dst2
        dst1 = feat.dst3

        If task.optionsChanged Then
            indexLowRes = New cvb.Mat(task.lowResColor.Size, cvb.MatType.CV_32FC2)
            For y = 0 To task.lowResColor.Height - 1
                For x = 0 To task.lowResColor.Width - 1
                    indexLowRes.Set(Of cvb.Vec2f)(y, x, New cvb.Vec2f(CSng(x), CSng(y)))
                Next
            Next
            indexHighRes = indexLowRes.Resize(dst2.Size, 0, 0, cvb.InterpolationFlags.Nearest)
        End If

        Dim lowResRGB32f As New cvb.Mat
        task.lowResColor.ConvertTo(lowResRGB32f, cvb.MatType.CV_32F)
        ml.trainMats = {lowResRGB32f, indexLowRes}
        ml.trainResponse = task.lrSmallMap

        Dim rgb32f As New cvb.Mat
        src.ConvertTo(rgb32f, cvb.MatType.CV_32F)
        ml.testMats = {rgb32f, indexHighRes}
        ml.Run(empty)


        Dim fLessMask = ml.predictions.Threshold(1.5, 255, cvb.ThresholdTypes.Binary).
                                       ConvertScaleAbs.Reshape(1, dst2.Rows)
        Dim featureMask = ml.predictions.Threshold(1.5, 255, cvb.ThresholdTypes.BinaryInv).
                                         ConvertScaleAbs.Reshape(1, dst2.Rows)
        dst2.SetTo(0)
        dst3.SetTo(0)

        src.CopyTo(dst2, featureMask)
        src.CopyTo(dst3, fLessMask)
        labels = {"Src image with edges.", "Src featureless regions", ml.options.ML_Name +
                  " found FeatureLess Regions", ml.options.ML_Name + " found these regions had features"}
    End Sub
End Class





Public Class LowRes_MLDepth : Inherits VB_Parent
    Dim feat As New LowRes_Edges
    Dim indexLowRes As New cvb.Mat
    Dim indexHighRes As New cvb.Mat
    Dim ml As New ML_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Train an ML tree to predict each pixel of the full size image using only color and position."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst0 = feat.dst2
        dst1 = feat.dst3

        If task.optionsChanged Then
            indexLowRes = New cvb.Mat(task.lowResColor.Size, cvb.MatType.CV_32FC2)
            For y = 0 To task.lowResColor.Height - 1
                For x = 0 To task.lowResColor.Width - 1
                    indexLowRes.Set(Of cvb.Vec2f)(y, x, New cvb.Vec2f(CSng(x), CSng(y)))
                Next
            Next
            indexHighRes = indexLowRes.Resize(dst2.Size, 0, 0, cvb.InterpolationFlags.Nearest)
        End If

        Dim lowResRGB32f As New cvb.Mat
        task.lowResColor.ConvertTo(lowResRGB32f, cvb.MatType.CV_32F)
        ml.trainMats = {lowResRGB32f, indexLowRes, task.lowResDepth}
        ml.trainResponse = task.lrSmallMap

        Dim rgb32f As New cvb.Mat
        src.ConvertTo(rgb32f, cvb.MatType.CV_32F)
        ml.testMats = {rgb32f, indexHighRes, task.pcSplit(2)}
        ml.Run(empty)

        Dim fLessMask = ml.predictions.Threshold(1.5, 255, cvb.ThresholdTypes.Binary).
                                       ConvertScaleAbs.Reshape(1, dst2.Rows)
        Dim featureMask = ml.predictions.Threshold(1.5, 255, cvb.ThresholdTypes.BinaryInv).
                                         ConvertScaleAbs.Reshape(1, dst2.Rows)
        dst2.SetTo(0)
        dst3.SetTo(0)

        src.CopyTo(dst2, featureMask)
        src.CopyTo(dst3, fLessMask)
        labels = {"Src image with edges.", "Src featureless regions", ml.options.ML_Name +
                  " found FeatureLess Regions", ml.options.ML_Name + " found these regions had features"}
    End Sub
End Class




Public Class LowRes_Boundaries : Inherits VB_Parent
    Dim feat As New LowRes_Edges
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        desc = "Find every non-featureless cell next to a featureless cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst1 = feat.dst1
        dst3 = feat.dst2

        dst2.SetTo(0)
        For Each rectList In task.lrRectsByRow
            For i = 0 To rectList.Count - 2
                Dim r1 = rectList(i)
                Dim r2 = rectList(i + 1)



                If (r1.X = 80) And (r1.Y = 20) Then Dim k = 0
                If (r2.X = 80) And (r2.Y = 20) Then Dim k = 0



                Dim v1 = dst1.Get(Of Byte)(r1.Y, r1.X)
                Dim v2 = dst1.Get(Of Byte)(r2.Y, r2.X)
                If v1 = 0 And v2 > 0 Then dst2(r1).SetTo(255)
                If v1 > 0 And v2 = 0 Then dst2(r2).SetTo(255)
            Next
        Next
    End Sub
End Class
