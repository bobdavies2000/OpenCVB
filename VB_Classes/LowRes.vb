Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp

Public Class LowRes_Basics : Inherits VB_Parent
    Dim lrColor As New LowRes_Color
    Dim lrDepth As New LowRes_Depth
    Public Sub New()
        labels(2) = "Low resolution color image."
        labels(3) = "Low resolution version of the depth data."
        desc = "Build the low-res image and accompanying map, rect list, and mask."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lrColor.Run(src)
        dst2 = lrColor.dst2.Clone
        task.lowResColor = lrColor.dst3.Clone

        lrDepth.Run(task.pcSplit(2))
        dst3 = lrDepth.dst2
        task.lowResDepth = lrDepth.dst3.Clone
    End Sub
End Class





Public Class LowRes_Color : Inherits VB_Parent
    Public Sub New()
        task.gOptions.setGridSize(10)
        labels = {"", "", "Grid of mean color values", "Resized task.lowResColor"}
        desc = "The bare minimum needed to make the LowRes image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone
        If task.optionsChanged Then dst3 = New cvb.Mat(task.gridRows, task.gridCols, cvb.MatType.CV_8UC3)
        dst3.SetTo(0)
        Dim index As Integer
        For y = 0 To task.gridRows - 1
            For x = 0 To task.gridCols - 1
                Dim roi = task.gridRects(index)
                index += 1
                Dim mean = src(roi).Mean()
                dst2(roi).SetTo(mean)
                dst3.Set(Of cvb.Vec3b)(y, x, New cvb.Vec3b(mean(0), mean(1), mean(2)))
            Next
        Next
    End Sub
End Class






Public Class LowRes_Depth : Inherits VB_Parent
    Public Sub New()
        task.gOptions.setGridSize(10)
        labels = {"", "", "Grid of mean depth values", "Resized task.lowResDepth"}
        desc = "The bare minimum needed to make the LowRes image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Type <> cvb.MatType.CV_32F Then src = task.pcSplit(2).Clone
        dst2 = src.Clone
        If task.optionsChanged Then dst3 = New cvb.Mat(task.gridRows, task.gridCols, cvb.MatType.CV_32F)
        dst3.SetTo(0)
        Dim index As Integer
        For y = 0 To task.gridRows - 1
            For x = 0 To task.gridCols - 1
                Dim roi = task.gridRects(index)
                index += 1
                Dim mean = src(roi).Mean()
                dst2(roi).SetTo(mean)
                dst3.Set(Of Single)(y, x, mean(0))
            Next
        Next
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
            Dim tile = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim test = gridIndex.IndexOf(tile)
            If test < 0 Then
                Dim r = task.gridRects(tile)
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
        For Each r In task.gridRects
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
        task.featuresFullRes = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U)
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
        task.featuresFullRes.SetTo(0)
        Dim flist As New List(Of Single)
        For Each r In task.gridRects
            If edges.dst2(r).CountNonZero = 0 Then
                task.fLessRects.Add(r)
                task.featuresFullRes(r).SetTo(255)
                flist.Add(1)
            Else
                task.featureRects.Add(r)
                DrawCircle(dst2, New cvb.Point(r.X, r.Y), task.DotSize, task.HighlightColor)
                flist.Add(2)
            End If
        Next

        task.featuresLowRes = cvb.Mat.FromPixelData(task.lowResColor.Height, task.lowResColor.Width,
                                                cvb.MatType.CV_32F, flist.ToArray)

        dst3.SetTo(0)
        src.CopyTo(dst3, task.featuresFullRes)
        If task.heartBeat Then
            labels(2) = CStr(task.featureRects.Count) + " cells with features were found"
            labels(3) = CStr(task.fLessRects.Count) + " cells without features were found"
        End If
    End Sub
End Class






Public Class LowRes_MLDepth : Inherits VB_Parent
    Dim feat As New LowRes_Edges
    Dim ml As New ML_Basics
    Dim bounds As New LowRes_Boundaries
    Public Sub New()
        dst1 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        desc = "Train an ML tree to predict each pixel of the boundary cells using color and depth."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)

        bounds.Run(src)

        Dim lowResRGB32f As New cvb.Mat
        task.lowResColor.ConvertTo(lowResRGB32f, cvb.MatType.CV_32FC3)
        ml.trainMats = {lowResRGB32f, task.lowResDepth}
        ml.trainResponse = task.featuresLowRes

        Dim rgb32f As New cvb.Mat, tmp As New cvb.Mat
        dst1 = task.featuresFullRes
        For Each roi In bounds.boundaryCells
            src(roi).ConvertTo(rgb32f, cvb.MatType.CV_32FC3)
            ml.testMats = {rgb32f, task.pcSplit(2)(roi)}
            ml.Run(empty)
            tmp = ml.predictions.Threshold(1.5, 255, cvb.ThresholdTypes.Binary).ConvertScaleAbs.Reshape(1, roi.Height)
            dst1(roi) = Not tmp
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, dst1)

        dst3.SetTo(0)
        src.CopyTo(dst3, Not dst1)

        labels = {"Src image with edges.", "Src featureless regions", ml.options.ML_Name +
                  " found FeatureLess Regions", ml.options.ML_Name + " found these regions had features"}
    End Sub
End Class




Public Class LowRes_Boundaries : Inherits VB_Parent
    Public feat As New LowRes_Edges
    Public boundaryCells As New List(Of cvb.Rect)
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        desc = "Find every non-featureless cell next to a featureless cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            feat.Run(src)
            dst1 = task.featuresFullRes.Clone
            dst3 = feat.dst2
        End If

        boundaryCells.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim cell = task.gridRects(i)
            Dim cellType = task.featuresFullRes.Get(Of Byte)(cell.Y, cell.X)
            If cellType = 0 Then Continue For
            Dim neighbors = task.gridNeighbors(i)
            For Each index In neighbors
                Dim roi = task.gridRects(index)
                Dim nType = task.featuresFullRes.Get(Of Byte)(roi.Y, roi.X)
                If nType <> cellType Then
                    boundaryCells.Add(roi)
                    Exit For
                End If
            Next
        Next

        dst2.SetTo(0)
        For Each roi In boundaryCells
            dst2(roi).SetTo(255)
        Next
    End Sub
End Class