Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp

Public Class LowRes_Basics : Inherits VB_Parent
    Dim options As New Options_Resize
    Dim mapCells As New LowRes_Map
    Public Sub New()
        FindRadio("WarpFillOutliers").Enabled = False
        FindRadio("WarpInverseMap").Enabled = False
        FindRadio("Area").Checked = True
        labels(3) = "Low resolution version of the depth data."
        desc = "Build the low-res image and accompanying map, rect list, and mask."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        task.lowResColor = src.Resize(New cvb.Size(task.lowResPercent * src.Width, task.lowResPercent * src.Height), 0, 0, options.warpFlag)
        dst2 = task.lowResColor.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        task.lowResDepth = task.pcSplit(2).Resize(New cvb.Size(task.lowResPercent * src.Width,
                                                               task.lowResPercent * src.Height), 0, 0, options.warpFlag)
        dst3 = task.lowResDepth.Resize(New cvb.Size(src.Width, src.Height), 0, 0, options.warpFlag)

        mapCells.Run(dst2)
        labels(2) = "There were " + CStr(task.lowRects.Count) + " cells found"
    End Sub
End Class





Public Class LowRes_Core : Inherits VB_Parent
    Dim options As New Options_Resize
    Public Sub New()
        FindRadio("Area").Checked = True
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
    Public lowResMap As New cvb.Mat
    Public featureCellCount As Integer
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
        dst2.SetTo(cvb.Scalar.Black, edges.dst2)

        task.featureRects.Clear()
        task.fLessRects.Clear()
        dst1.SetTo(0)
        Dim flist As New List(Of Single)
        For Each r In task.lowRects
            If edges.dst2(r).CountNonZero = 0 Then
                task.fLessRects.Add(r)
                dst1(r).SetTo(255)
                flist.Add(0)
            Else
                task.featureRects.Add(r)
                DrawCircle(dst2, New cvb.Point(r.X, r.Y), task.DotSize, task.HighlightColor)
                flist.Add(1)
            End If
        Next

        lowResMap = cvb.Mat.FromPixelData(task.lowResColor.Width, task.lowResColor.Height,
                                          cvb.MatType.CV_32F, flist.ToArray)

        dst3.SetTo(0)
        src.CopyTo(dst3, dst1)
        If task.heartBeat Then
            featureCellCount = lowResMap.CountNonZero
            labels(2) = CStr(featureCellCount) + " cells with features were found"
            labels(3) = CStr(task.lowRects.Count - featureCellCount) + " cells without features were found"
        End If
    End Sub
End Class





Public Class LowRes_MLDepth : Inherits VB_Parent
    Dim feat As New LowRes_Edges
    Dim indexLowRes As New cvb.Mat
    Dim indexHighRes As New cvb.Mat
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Train an ML tree to predict each pixel of the full size image"
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
        Dim trainMat As New cvb.Mat
        cvb.Cv2.Merge({lowResRGB32f, indexLowRes, task.lowResDepth}, trainMat)
        trainMat = cvb.Mat.FromPixelData(trainMat.Width * trainMat.Height, 6, cvb.MatType.CV_32F, trainMat.Data)

        Dim rtree = cvb.ML.RTrees.Create()
        Dim responseMat As cvb.Mat = cvb.Mat.FromPixelData(feat.lowResMap.Total, 1, cvb.MatType.CV_32F,
                                                           feat.lowResMap.Data)
        rtree.Train(trainMat, cvb.ML.SampleTypes.RowSample, responseMat)

        Dim rgb32f As New cvb.Mat
        src.ConvertTo(rgb32f, cvb.MatType.CV_32F)

        Dim testMat As New cvb.Mat
        cvb.Cv2.Merge({rgb32f, indexHighRes, task.pcSplit(2)}, testMat)

        Dim predictions As New cvb.Mat
        testMat = cvb.Mat.FromPixelData(testMat.Total, 6, cvb.MatType.CV_32F, testMat.Data)
        rtree.Predict(testMat, predictions)


        Dim samples(predictions.Total - 1) As Single
        Marshal.Copy(predictions.Data, samples, 0, samples.Length)


        Dim fLessMask = predictions.Threshold(0.5, 255, cvb.ThresholdTypes.Binary).
                                    ConvertScaleAbs.Reshape(1, dst2.Rows)
        Dim featureMask = predictions.Threshold(0.5, 255, cvb.ThresholdTypes.BinaryInv).
                                      ConvertScaleAbs.Reshape(1, dst2.Rows)

        fLessMask.SetTo(0, task.noDepthMask)
        featureMask.SetTo(0, task.noDepthMask)

        dst2.SetTo(0)
        dst3.SetTo(0)
        src.CopyTo(dst2, featureMask)
        src.CopyTo(dst3, fLessMask)
    End Sub
End Class





Public Class LowRes_MLNoDepth : Inherits VB_Parent
    Dim feat As New LowRes_Edges
    Dim indexLowRes As New cvb.Mat
    Dim indexHighRes As New cvb.Mat
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Train an ML tree to predict each pixel of the full size image"
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
        Dim trainMat As New cvb.Mat
        cvb.Cv2.Merge({lowResRGB32f, indexLowRes, task.lowResDepth}, trainMat)
        trainMat = cvb.Mat.FromPixelData(trainMat.Width * trainMat.Height, 6, cvb.MatType.CV_32F, trainMat.Data)

        Dim rtree = cvb.ML.RTrees.Create()
        Dim responseMat As cvb.Mat = cvb.Mat.FromPixelData(feat.lowResMap.Total, 1, cvb.MatType.CV_32F,
                                                           feat.lowResMap.Data)
        rtree.Train(trainMat, cvb.ML.SampleTypes.RowSample, responseMat)

        Dim rgb32f As New cvb.Mat
        src.ConvertTo(rgb32f, cvb.MatType.CV_32F)

        Dim testMat As New cvb.Mat
        cvb.Cv2.Merge({rgb32f, indexHighRes, task.pcSplit(2)}, testMat)

        Dim predictions As New cvb.Mat
        testMat = cvb.Mat.FromPixelData(testMat.Total, 6, cvb.MatType.CV_32F, testMat.Data)
        rtree.Predict(testMat, predictions)


        Dim samples(predictions.Total - 1) As Single
        Marshal.Copy(predictions.Data, samples, 0, samples.Length)


        Dim fLessMask = predictions.Threshold(0.5, 255, cvb.ThresholdTypes.Binary).
                                    ConvertScaleAbs.Reshape(1, dst2.Rows)
        Dim featureMask = predictions.Threshold(0.5, 255, cvb.ThresholdTypes.BinaryInv).
                                      ConvertScaleAbs.Reshape(1, dst2.Rows)

        fLessMask.SetTo(0, task.noDepthMask)
        featureMask.SetTo(0, task.noDepthMask)

        dst2.SetTo(0)
        dst3.SetTo(0)
        src.CopyTo(dst2, featureMask)
        src.CopyTo(dst3, fLessMask)
    End Sub
End Class
