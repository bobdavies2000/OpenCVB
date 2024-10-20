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
        labels = {"", "", "Grid of mean color values", "Resized task.lowResColor"}
        desc = "The bare minimum needed to make the LowRes image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone
        For Each roi In task.gridRects
            Dim mean = src(roi).Mean()
            dst2(roi).SetTo(mean)
        Next
    End Sub
End Class







Public Class LowRes_Depth : Inherits VB_Parent
    Public Sub New()
        labels = {"", "", "Grid of mean depth values", "Resized task.lowResDepth"}
        desc = "The bare minimum needed to make the LowRes image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If src.Type <> cvb.MatType.CV_32F Then src = task.pcSplit(2).Clone
        dst2 = src.Clone
        Dim index As Integer
        For y = 0 To task.gridRows - 1
            For x = 0 To task.gridCols - 1
                Dim roi = task.gridRects(index)
                index += 1
                Dim mean = src(roi).Mean()
                dst2(roi).SetTo(mean)
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
    Public edges As New Edge_Basics
    Public Sub New()
        task.featureMask = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U)
        task.fLessMask = New cvb.Mat(dst3.Size, cvb.MatType.CV_8U)
        FindRadio("Laplacian").Checked = True
        desc = "Add edges to features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static stateList As New List(Of Single)

        lowRes.Run(src)
        dst2 = lowRes.dst2.Clone

        Static lastDepth As cvb.Mat = task.lowResDepth.Clone

        edges.Run(src)
        dst2.SetTo(0, edges.dst2)

        task.featureRects.Clear()
        task.fLessRects.Clear()
        task.featureMask.SetTo(0)
        task.fLessMask.SetTo(0)
        Dim flist As New List(Of Single)
        For Each r In task.gridRects
            flist.Add(If(edges.dst2(r).CountNonZero <= 1, 1, 2))
        Next

        If task.optionsChanged Or stateList.Count = 0 Then
            stateList.Clear()
            For Each n In flist
                stateList.Add(n)
            Next
        End If

        Dim flipRects As New List(Of cvb.Rect)
        For i = 0 To task.gridRects.Count - 1
            stateList(i) = (stateList(i) + flist(i)) / 2
            Dim r = task.gridRects(i)
            If stateList(i) >= 1.95 Then
                DrawCircle(dst2, New cvb.Point(r.X, r.Y), task.DotSize, task.HighlightColor)
                task.featureRects.Add(r)
                task.featureMask(r).SetTo(255)
            ElseIf stateList(i) <= 1.05 Then
                task.fLessRects.Add(r)
                task.fLessMask(r).SetTo(255)
            Else
                flipRects.Add(r)
                'task.fLessRects.Add(r)
                'task.fLessMask(r).SetTo(255)
                'task.featureRects.Add(r)
                'task.featureMask(r).SetTo(255)
            End If
        Next

        dst3.SetTo(0)
        src.CopyTo(dst3, task.featureMask)

        For Each r In flipRects
            dst3.Rectangle(r, task.HighlightColor, task.lineWidth)
        Next

        For Each r In task.fLessRects
            Dim x = CInt(r.X / task.gridSize)
            Dim y = CInt(r.Y / task.gridSize)
            task.lowResDepth.Set(Of Single)(y, x, lastDepth.Get(Of Single)(y, x))
        Next
        lastDepth = task.lowResDepth.Clone
        If task.heartBeat Then
            labels(2) = CStr(task.featureRects.Count) + "/" + CStr(task.fLessRects.Count) + "/" +
                        CStr(flipRects.Count) + " Features/FeatureLess/Flipper cells."
            labels(3) = CStr(task.fLessRects.Count) + " cells without features were found.  " +
                        "Cells that are flipping are highlighted"
        End If
    End Sub
End Class






Public Class LowRes_Boundaries : Inherits VB_Parent
    Public feat As New LowRes_Edges
    Public boundaryCells As New List(Of List(Of Integer))
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        desc = "Find the boundary cells between feature and featureless cells."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst1 = task.featureMask.Clone
        dst3 = feat.dst2

        boundaryCells.Clear()
        For Each nList In task.gridNeighbors
            Dim roiA = task.gridRects(nList(0))
            Dim centerType = task.featureMask.Get(Of Byte)(roiA.Y, roiA.X)
            If centerType <> 0 Then
                Dim boundList = New List(Of Integer)
                Dim addFirst As Boolean = True
                For i = 1 To nList.Count - 1
                    Dim roiB = task.gridRects(nList(i))
                    Dim val = task.featureMask.Get(Of Byte)(roiB.Y, roiB.X)
                    If centerType <> val Then
                        If addFirst Then boundList.Add(nList(0)) ' first element is the center point (has features)
                        addFirst = False
                        boundList.Add(nList(i))
                    End If
                Next
                If boundList.Count > 0 Then boundaryCells.Add(boundList)
            End If
        Next

        dst2.SetTo(0)
        For Each nlist In boundaryCells
            For Each n In nlist
                Dim mytoggle As Integer
                Dim roi = task.gridRects(n)
                Dim val = task.featureMask.Get(Of Byte)(roi.Y, roi.X)
                If val > 0 Then mytoggle = 255 Else mytoggle = 128
                dst2(task.gridRects(n)).SetTo(mytoggle)
            Next
        Next
    End Sub
End Class






Public Class LowRes_MLColor : Inherits VB_Parent
    Dim ml As New ML_Basics
    Dim bounds As New LowRes_Boundaries
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        ml.buildEveryPass = True
        dst1 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        desc = "Train an ML tree to predict each pixel of the boundary cells using color and depth from boundary neighbors."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        bounds.Run(src)
        Dim edgeMask = bounds.feat.edges.dst2

        Dim rgb32f As New cvb.Mat, tmp As New cvb.Mat
        src.ConvertTo(rgb32f, cvb.MatType.CV_32FC3)

        dst1 = task.fLessMask
        Dim trainRGB As cvb.Mat
        For i = 0 To bounds.boundaryCells.Count - 1
            Dim nList = bounds.boundaryCells(i)

            ' the first roi is the center one and the only roi with edges.  The rest are featureless.
            Dim roi = task.gridRects(nList(0))
            Dim edgePixels = edgeMask(roi).FindNonZero()

            ' mark the edge pixels as class 2 - others will be updated next
            ml.trainResponse = New cvb.Mat(nList.Count + edgePixels.Rows - 1, 1,
                                           cvb.MatType.CV_32F, New cvb.Scalar(2))
            trainRGB = New cvb.Mat(ml.trainResponse.Rows, 1, cvb.MatType.CV_32FC3)

            For j = 1 To nList.Count - 1
                Dim roiA = task.gridRects(nList(j))
                Dim x As Integer = Math.Floor(roiA.X * task.gridCols / task.cols)
                Dim y As Integer = Math.Floor(roiA.Y * task.gridRows / task.rows)
                Dim val = task.lowResColor.Get(Of cvb.Vec3f)(y, x)
                trainRGB.Set(Of cvb.Vec3f)(j - 1, 0, val)
                ml.trainResponse.Set(Of Single)(j - 1, 0, 1)
            Next

            ' next, add the edge pixels in the target cell - they are the feature identifiers.
            Dim index = nList.Count - 1
            For j = 0 To edgePixels.Rows - 1
                Dim pt = edgePixels.Get(Of cvb.Point)(j, 0)
                Dim val = rgb32f.Get(Of cvb.Vec3f)(roi.Y + pt.Y, roi.X + pt.X)
                trainRGB.Set(Of cvb.Vec3f)(index + j, 0, val) ' ml.trainResponse already set to 2
            Next

            ml.trainMats = {trainRGB}

            Dim roiB = task.gridRects(nList(0))
            ml.testMats = {rgb32f(roiB)}
            ml.Run(empty)

            dst1(roiB) = ml.predictions.Threshold(1.5, 255, cvb.ThresholdTypes.BinaryInv).
                                        ConvertScaleAbs.Reshape(1, roiB.Height)
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, dst1)

        dst3.SetTo(0)
        src.CopyTo(dst3, Not dst1)

        labels = {"Src image with edges.", "Src featureless regions", ml.options.ML_Name +
                  " found FeatureLess Regions", ml.options.ML_Name + " found these regions had features"}
    End Sub
End Class






Public Class LowRes_MLColorDepth : Inherits VB_Parent
    Dim ml As New ML_Basics
    Dim bounds As New LowRes_Boundaries
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        ml.buildEveryPass = True
        dst1 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        desc = "Train an ML tree to predict each pixel of the boundary cells using color and depth from boundary neighbors."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        bounds.Run(src)
        Dim edgeMask = bounds.feat.edges.dst2

        Dim rgb32f As New cvb.Mat, tmp As New cvb.Mat
        src.ConvertTo(rgb32f, cvb.MatType.CV_32FC3)

        dst1 = task.fLessMask
        Dim trainRGB As cvb.Mat, trainDepth As cvb.Mat
        For i = 0 To bounds.boundaryCells.Count - 1
            Dim nList = bounds.boundaryCells(i)

            ' the first roi is the center one and the only roi with edges.  The rest are featureless.
            Dim roi = task.gridRects(nList(0))
            Dim edgePixels = edgeMask(roi).FindNonZero()

            ' mark the edge pixels as class 2 - others will be updated next
            ml.trainResponse = New cvb.Mat(nList.Count + edgePixels.Rows - 1, 1,
                                           cvb.MatType.CV_32F, New cvb.Scalar(2))
            trainRGB = New cvb.Mat(ml.trainResponse.Rows, 1, cvb.MatType.CV_32FC3)
            trainDepth = New cvb.Mat(ml.trainResponse.Rows, 1, cvb.MatType.CV_32F)

            For j = 1 To nList.Count - 1
                Dim roiA = task.gridRects(nList(j))
                Dim x As Integer = Math.Floor(roiA.X * task.gridCols / task.cols)
                Dim y As Integer = Math.Floor(roiA.Y * task.gridRows / task.rows)
                Dim val = task.lowResColor.Get(Of cvb.Vec3f)(y, x)
                trainRGB.Set(Of cvb.Vec3f)(j - 1, 0, val)
                trainDepth.Set(Of Single)(j - 1, 0, task.lowResDepth.Get(Of Single)(y, x))
                ml.trainResponse.Set(Of Single)(j - 1, 0, 1)
            Next

            ' next, add the edge pixels in the target cell - they are the feature identifiers.
            Dim index = nList.Count - 1
            For j = 0 To edgePixels.Rows - 1
                Dim pt = edgePixels.Get(Of cvb.Point)(j, 0)
                Dim val = rgb32f(roi).Get(Of cvb.Vec3f)(pt.Y, pt.X)
                trainRGB.Set(Of cvb.Vec3f)(index + j, 0, val) ' ml.trainResponse already set to 2
                Dim depth = task.pcSplit(2)(roi).Get(Of Single)(pt.Y, pt.X)
                trainDepth.Set(Of Single)(index + j, 0, depth)
            Next

            ml.trainMats = {trainRGB, trainDepth}

            Dim roiB = task.gridRects(nList(0))
            ml.testMats = {rgb32f(roiB), task.pcSplit(2)(roiB)}
            ml.Run(empty)

            dst1(roiB) = ml.predictions.Threshold(1.5, 255, cvb.ThresholdTypes.BinaryInv).
                                        ConvertScaleAbs.Reshape(1, roiB.Height)
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, dst1)

        dst3.SetTo(0)
        src.CopyTo(dst3, Not dst1)

        labels = {"Src image with edges.", "Src featureless regions", ml.options.ML_Name +
                  " found FeatureLess Regions", ml.options.ML_Name + " found these regions had features"}
    End Sub
End Class





Public Class LowRes_DepthMask : Inherits VB_Parent
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U)
        desc = "Create a mask of the cells that are mostly depth - remove speckles in no depth regions"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2.SetTo(0)
        For Each roi In task.gridRects
            Dim count = task.pcSplit(2)(roi).CountNonZero()
            If count >= task.gridSize * task.gridSize / 2 Then dst2(roi).SetTo(255)
        Next
    End Sub
End Class





Public Class LowRes_MeasureColor : Inherits VB_Parent
    Dim lowRes As New LowRes_Color
    Public colors(0) As cvb.Vec3b
    Public distances() As Single
    Public options As New Options_LowRes
    Public motionList As New List(Of Integer)
    Public Sub New()
        desc = "Measure how much color changes with and without motion."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        lowRes.Run(src)
        dst2 = lowRes.dst2

        If task.optionsChanged Or colors.Length <> task.gridRects.Count Then
            ReDim colors(task.gridRects.Count - 1)
            ReDim distances(task.gridRects.Count - 1)
        End If

        If standaloneTest() Then trueData.Clear()
        motionList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim vec = dst2.Get(Of cvb.Vec3b)(roi.Y, roi.X)
            distances(i) = distance3D(colors(i), vec)
            If distances(i) > options.colorDifferenceThreshold Then
                If standaloneTest() Then
                    SetTrueText(Format(distances(i), fmt1), New cvb.Point(roi.X, roi.Y), 3)
                End If
                colors(i) = vec
                For Each index In task.gridNeighbors(i)
                    If motionList.Contains(index) = False Then
                        motionList.Add(index)
                    End If
                Next
            End If
        Next

        If task.heartBeat Or task.optionsChanged Then
            Static percentList As New List(Of Single)
            percentList.Add(motionList.Count / task.gridRects.Count)
            If percentList.Count > 3 Then percentList.RemoveAt(0)
            task.motionPercent = percentList.Average
            If task.gOptions.UseMotionConstructed.Checked = False Then
                labels(3) = "100% of each image has motion."
            Else
                labels(3) = Format(task.motionPercent, "0%") + " each image contained motion (on average)."
            End If
            task.MotionLabel = labels(3)
        End If
    End Sub
End Class





Public Class LowRes_MeasureMotion : Inherits VB_Parent
    Dim measure As New LowRes_MeasureColor
    Public motionDetected As Boolean
    Public motionRects As New List(Of cvb.Rect)
    Public Sub New()
        If standalone Then task.gOptions.setDisplay0()
        labels(3) = "A composite of an earlier image and the motion from the latest input"
        desc = "Show all the grid cells above the motionless value (an option)."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then dst0 = src.Clone

        If task.optionsChanged Then motionRects = New List(Of cvb.Rect)

        measure.Run(src)
        dst2 = measure.dst2
        labels(2) = measure.labels(3)

        Dim threshold = measure.options.colorDifferenceThreshold

        motionRects.Clear()
        Dim indexList As New List(Of Integer)
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            If measure.distances(i) > threshold Then
                For Each index In task.gridNeighbors(i)
                    If indexList.Contains(index) = False Then
                        indexList.Add(index)
                        motionRects.Add(task.gridRects(index))
                    End If
                Next
            End If
        Next

        motionDetected = False
        If task.frameCount < 10 Then ' some of the grid configurations are not compatible between cameras.
            src.CopyTo(dst3)
        Else
            If motionRects.Count > 0 Then
                For Each roi In motionRects
                    src(roi).CopyTo(dst3(roi))
                    If standaloneTest() Then dst0.Rectangle(roi, cvb.Scalar.White, task.lineWidth)
                Next
                motionDetected = True
            End If
        End If
    End Sub
End Class






Public Class LowRes_MeasureValidate : Inherits VB_Parent
    Dim measure As New LowRes_MeasureMotion
    Dim diff As New Diff_Basics
    Public Sub New()
        task.gOptions.setPixelDifference(50)
        task.gOptions.setDisplay1()
        labels(1) = "Every pixel is slightly different except where motion is detected."
        labels(3) = "Differences are individual pixels - not significant. " +
                    "Contrast this with BGSubtract."
        desc = "Validate the image provided by LowRes_MeasureMotion"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst0 = src.Clone
        measure.Run(src)
        dst2 = measure.dst3.Clone
        labels(2) = measure.labels(2)
        labels(2) = labels(2).Replace("Values shown are above average", "")

        Dim curr = dst0.Reshape(1, dst0.Rows * 3)
        Dim motion = dst2.Reshape(1, dst2.Rows * 3)

        cvb.Cv2.Absdiff(curr, motion, dst0)

        If task.heartBeat = False Then
            Static diff As New Diff_Basics
            diff.lastFrame = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
            diff.Run(src)
            dst3 = diff.dst2
        End If
    End Sub
End Class




