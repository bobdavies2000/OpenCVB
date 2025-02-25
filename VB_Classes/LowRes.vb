Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class LowRes_Features : Inherits TaskParent
    Dim options As New Options_Features
    Public Sub New()
        optiBase.FindSlider("Min Distance to next").Value = 3
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Featureless areas"
        task.feat = New Feature_Basics
        desc = "Identify the cells with features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        task.feat.Run(src)

        dst2 = task.dCell.dst2

        Dim gridIndex As New List(Of Integer)
        Dim gridCounts As New List(Of Integer)

        task.featurePoints.Clear()
        Dim rects As New List(Of cv.Rect)
        For Each pt In task.features
            Dim index = task.iddMap.Get(Of Integer)(pt.Y, pt.X)
            Dim idd = task.iddList(index)
            idd.features.Add(pt)
            DrawCircle(dst2, idd.cRect.TopLeft, task.DotSize, task.HighlightColor)

            rects.Add(idd.cRect)
            task.iddList(index) = idd
        Next

        task.featureRects.Clear()
        task.fLessRects.Clear()
        For Each idd In task.iddList
            If idd.features.Count > 0 Then task.featureRects.Add(idd.cRect) Else task.fLessRects.Add(idd.cRect)
        Next

        If task.gOptions.DebugCheckBox.Checked Then
            For Each pt In task.features
                DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Black)
            Next
        End If
        If standaloneTest() Then
            dst3.SetTo(0)
            For Each r In rects
                dst3.Rectangle(r, white, -1)
            Next
            dst3 = Not dst3
        End If
        If task.heartBeat Then
            labels(2) = CStr(task.featureRects.Count) + " cells had features while " + CStr(task.fLessRects.Count) + " had none"
        End If
    End Sub
End Class







Public Class LowRes_Edges : Inherits TaskParent
    Public edges As New Edge_Basics
    Public Sub New()
        task.featureMask = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        task.fLessMask = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        optiBase.findRadio("Laplacian").Checked = True
        desc = "Add edges to features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static stateList As New List(Of Single)
        Static lastDepth As cv.Mat = task.lowResDepth.Clone

        edges.Run(src)

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

        Dim flipRects As New List(Of cv.Rect)
        For i = 0 To task.gridRects.Count - 1
            stateList(i) = (stateList(i) + flist(i)) / 2
            Dim r = task.gridRects(i)
            If stateList(i) >= 1.95 Then
                task.featureRects.Add(r)
                task.featureMask(r).SetTo(255)
            ElseIf stateList(i) <= 1.05 Then
                task.fLessRects.Add(r)
                task.fLessMask(r).SetTo(255)
            Else
                flipRects.Add(r)
            End If
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, task.featureMask)

        For Each r In flipRects
            dst2.Rectangle(r, task.HighlightColor, task.lineWidth)
        Next

        For Each r In task.fLessRects
            Dim x = CInt(r.X / task.cellSize)
            Dim y = CInt(r.Y / task.cellSize)
            task.lowResDepth.Set(Of Single)(y, x, lastDepth.Get(Of Single)(y, x))
        Next
        lastDepth = task.lowResDepth.Clone
        If task.heartBeat Then
            labels(2) = CStr(task.fLessRects.Count) + " cells without features were found.  " +
                        "Cells that are flipping (with and without edges) are highlighted"
        End If
    End Sub
End Class






Public Class LowRes_Boundaries : Inherits TaskParent
    Public feat As New LowRes_Edges
    Public boundaryCells As New List(Of List(Of Integer))
    Public Sub New()
        labels(2) = "Gray and black regions are featureless while white has features..."
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Find the boundary cells between feature and featureless cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)
        dst1 = task.featureMask.Clone

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






Public Class LowRes_MLColor : Inherits TaskParent
    Dim ml As New ML_Basics
    Dim bounds As New LowRes_Boundaries
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        ml.buildEveryPass = True
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Train an ML tree to predict each pixel of the boundary cells using color and depth from boundary neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bounds.Run(src)
        Dim edgeMask = bounds.feat.edges.dst2

        Dim rgb32f As New cv.Mat, tmp As New cv.Mat
        src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)

        dst1 = task.fLessMask
        Dim trainRGB As cv.Mat
        For i = 0 To bounds.boundaryCells.Count - 1
            Dim nList = bounds.boundaryCells(i)

            ' the first roi is the center one and the only roi with edges.  The rest are featureless.
            Dim roi = task.gridRects(nList(0))
            Dim edgePixels = edgeMask(roi).FindNonZero()

            ' mark the edge pixels as class 2 - others will be updated next
            ml.trainResponse = New cv.Mat(nList.Count + edgePixels.Rows - 1, 1,
                                           cv.MatType.CV_32F, New cv.Scalar(2))
            trainRGB = New cv.Mat(ml.trainResponse.Rows, 1, cv.MatType.CV_32FC3)

            For j = 1 To nList.Count - 1
                Dim roiA = task.gridRects(nList(j))
                Dim x As Integer = Math.Floor(roiA.X * task.tilesPerRow / task.cols)
                Dim y As Integer = Math.Floor(roiA.Y * task.tilesPerCol / task.rows)
                Dim val = task.lowResColor.Get(Of cv.Vec3f)(y, x)
                trainRGB.Set(Of cv.Vec3f)(j - 1, 0, val)
                ml.trainResponse.Set(Of Single)(j - 1, 0, 1)
            Next

            ' next, add the edge pixels in the target cell - they are the feature identifiers.
            Dim index = nList.Count - 1
            For j = 0 To edgePixels.Rows - 1
                Dim pt = edgePixels.Get(Of cv.Point)(j, 0)
                Dim val = rgb32f.Get(Of cv.Vec3f)(roi.Y + pt.Y, roi.X + pt.X)
                trainRGB.Set(Of cv.Vec3f)(index + j, 0, val) ' ml.trainResponse already set to 2
            Next

            ml.trainMats = {trainRGB}

            Dim roiB = task.gridRects(nList(0))
            ml.testMats = {rgb32f(roiB)}
            ml.Run(src)

            dst1(roiB) = ml.predictions.Threshold(1.5, 255, cv.ThresholdTypes.BinaryInv).
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






Public Class LowRes_MLColorDepth : Inherits TaskParent
    Dim ml As New ML_Basics
    Dim bounds As New LowRes_Boundaries
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        ml.buildEveryPass = True
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Train an ML tree to predict each pixel of the boundary cells using color and depth from boundary neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bounds.Run(src)
        Dim edgeMask = bounds.feat.edges.dst2

        Dim rgb32f As New cv.Mat, tmp As New cv.Mat
        src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)

        dst1 = task.fLessMask
        Dim trainRGB As cv.Mat, trainDepth As cv.Mat
        For i = 0 To bounds.boundaryCells.Count - 1
            Dim nList = bounds.boundaryCells(i)

            ' the first roi is the center one and the only roi with edges.  The rest are featureless.
            Dim roi = task.gridRects(nList(0))
            Dim edgePixels = edgeMask(roi).FindNonZero()

            ' mark the edge pixels as class 2 - others will be updated next
            ml.trainResponse = New cv.Mat(nList.Count + edgePixels.Rows - 1, 1,
                                           cv.MatType.CV_32F, New cv.Scalar(2))
            trainRGB = New cv.Mat(ml.trainResponse.Rows, 1, cv.MatType.CV_32FC3)
            trainDepth = New cv.Mat(ml.trainResponse.Rows, 1, cv.MatType.CV_32F)

            For j = 1 To nList.Count - 1
                Dim roiA = task.gridRects(nList(j))
                Dim x As Integer = Math.Floor(roiA.X * task.tilesPerRow / task.cols)
                Dim y As Integer = Math.Floor(roiA.Y * task.tilesPerCol / task.rows)
                Dim val = task.lowResColor.Get(Of cv.Vec3f)(y, x)
                trainRGB.Set(Of cv.Vec3f)(j - 1, 0, val)
                trainDepth.Set(Of Single)(j - 1, 0, task.lowResDepth.Get(Of Single)(y, x))
                ml.trainResponse.Set(Of Single)(j - 1, 0, 1)
            Next

            ' next, add the edge pixels in the target cell - they are the feature identifiers.
            Dim index = nList.Count - 1
            For j = 0 To edgePixels.Rows - 1
                Dim pt = edgePixels.Get(Of cv.Point)(j, 0)
                Dim val = rgb32f(roi).Get(Of cv.Vec3f)(pt.Y, pt.X)
                trainRGB.Set(Of cv.Vec3f)(index + j, 0, val) ' ml.trainResponse already set to 2
                Dim depth = task.pcSplit(2)(roi).Get(Of Single)(pt.Y, pt.X)
                trainDepth.Set(Of Single)(index + j, 0, depth)
            Next

            ml.trainMats = {trainRGB, trainDepth}

            Dim roiB = task.gridRects(nList(0))
            ml.testMats = {rgb32f(roiB), task.pcSplit(2)(roiB)}
            ml.Run(src)

            dst1(roiB) = ml.predictions.Threshold(1.5, 255, cv.ThresholdTypes.BinaryInv).
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






Public Class LowRes_MeasureColor : Inherits TaskParent
    Public colors(0) As cv.Vec3b
    Public distances() As Single
    Public options As New Options_LowRes
    Public motionList As New List(Of Integer)
    Dim percentList As New List(Of Single)
    Public Sub New()
        desc = "Measure how much color changes with and without motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If task.optionsChanged Or colors.Length <> task.gridRects.Count Then
            ReDim colors(task.gridRects.Count - 1)
            ReDim distances(task.gridRects.Count - 1)
        End If

        If standaloneTest() Then trueData.Clear()
        motionList.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            Dim vec = dst2.Get(Of cv.Vec3b)(roi.Y, roi.X)
            distances(i) = distance3D(colors(i), vec)
            If distances(i) > options.colorDifferenceThreshold Then
                If standaloneTest() Then
                    SetTrueText(Format(distances(i), fmt1), roi.Location, 3)
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
            percentList.Add(motionList.Count / task.gridRects.Count)
            If percentList.Count > 3 Then percentList.RemoveAt(0)
            task.motionPercent = percentList.Average
            If task.gOptions.UseMotion.Checked = False Then
                labels(3) = "100% of each image has motion."
            Else
                labels(3) = " Average motion per image: " + Format(task.motionPercent, "0%")
            End If
            task.motionLabel = labels(3)
        End If
    End Sub
End Class





Public Class LowRes_MeasureMotion : Inherits TaskParent
    Dim measure As New LowRes_MeasureColor
    Public motionDetected As Boolean
    Public motionRects As New List(Of cv.Rect)
    Public Sub New()
        labels(3) = "A composite of an earlier image and the motion since that input"
        desc = "Show all the grid cells above the motionless value (an option)."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then dst0 = src.Clone

        If task.optionsChanged Then motionRects = New List(Of cv.Rect)

        measure.Run(src)
        dst2 = measure.dst2
        labels(2) = measure.labels(3)

        Dim threshold = If(task.heartBeat, measure.options.colorDifferenceThreshold - 1,
                                           measure.options.colorDifferenceThreshold)

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
        ' some configurations are not compatible when switching cameras.
        ' Use the whole image for the first few images.
        If task.frameCount < 3 Then
            src.CopyTo(dst3)
            motionRects.Clear()
            motionRects.Add(New cv.Rect(0, 0, dst2.Width, dst2.Height))
            motionDetected = True
        Else
            If motionRects.Count > 0 Then
                For Each roi In motionRects
                    src(roi).CopyTo(dst3(roi))
                    If standaloneTest() Then dst0.Rectangle(roi, white, task.lineWidth)
                Next
                motionDetected = True
            End If
        End If
    End Sub
End Class






Public Class LowRes_MeasureValidate : Inherits TaskParent
    Dim measure As New LowRes_MeasureMotion
    Public Sub New()
        task.gOptions.setPixelDifference(50)
        labels(1) = "Every pixel is slightly different except where motion is detected."
        labels(3) = "Differences are individual pixels - not significant. " +
                    "Contrast this with BGSubtract."
        desc = "Validate the image provided by LowRes_MeasureMotion"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0 = src.Clone
        measure.Run(src)
        dst2 = measure.dst3.Clone
        labels(2) = measure.labels(2)
        labels(2) = labels(2).Replace("Values shown are above average", "")

        Dim curr = dst0.Reshape(1, dst0.Rows * 3)
        Dim motion = dst2.Reshape(1, dst2.Rows * 3)

        cv.Cv2.Absdiff(curr, motion, dst0)

        If task.heartBeat = False Then
            Static diff As New Diff_Basics
            diff.lastFrame = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            diff.Run(src)
            dst3 = diff.dst2
        End If
    End Sub
End Class
