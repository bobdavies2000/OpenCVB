﻿Imports OpenCvSharp.Flann
Imports cv = OpenCvSharp
Public Class Brick_Basics : Inherits TaskParent
    Public instantUpdate As Boolean
    Public ptCursor As New cv.Point
    Public ptTextLoc As New cv.Point
    Public ptTopLeft As New cv.Point
    Public depthAndCorrelationText As String
    Public Sub New()
        task.brickMap = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
        If task.cameraName.StartsWith("Orbbec Gemini") Then task.rgbLeftAligned = True
        If task.cameraName.StartsWith("StereoLabs") Then task.rgbLeftAligned = True
        desc = "Create the grid of bricks that reduce depth volatility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.algorithmPrep = False Then Exit Sub ' when standalone or called from another algorithm is unnecessary - already been run...

        If task.brickList.Count <> task.gridRects.Count Then task.brickList.Clear()

        Dim correlationMat As New cv.Mat
        Dim leftview = If(task.gOptions.LRMeanSubtraction.Checked, task.LRMeanSub.dst2, task.leftView)
        Dim rightView = If(task.gOptions.LRMeanSubtraction.Checked, task.LRMeanSub.dst3, task.rightView)

        Dim brickLast As New List(Of brickData)(task.brickList)
        Dim unchangedCount As Integer

        Dim maxPixels = task.cellSize * task.cellSize
        task.brickList.Clear()
        Dim depthCount As Integer
        For i = 0 To task.gridRects.Count - 1
            Dim brick As New brickData
            If brick.index = 70 Then Dim k = 0
            If brick.depth > 0 Then
                ' motion mask does not include depth shadow so if there is depth shadow, we must recompute brick.
                Dim lastCorrelation = If(i < brickLast.Count, brickLast(i).correlation, 0)
                If brick.age > 1 And lastCorrelation > task.fCorrThreshold And instantUpdate = False Then
                    ' no need to recompute everything when there is no motion in the cell.
                    brick = brickLast(i)
                    brick.age = task.motionBasics.cellAge(i)
                    unchangedCount += 1
                Else
                    If task.rgbLeftAligned Then
                        brick.rRect = brick.lRect
                        brick.rRect.X -= task.calibData.baseline * task.calibData.rgbIntrinsics.fx / brick.depth
                        If brick.rRect.X < 0 Or brick.rRect.X + brick.rRect.Width >= dst2.Width Then
                            brick.depth = 0 ' off the image
                            brick.lRect = emptyRect
                            brick.rRect = emptyRect
                        End If
                    Else
                        Dim irPt = Intrinsics_Basics.translate_ColorToLeft(task.pointCloud.Get(Of cv.Point3f)(brick.rect.Y, brick.rect.X))
                        If irPt.X < 0 Or (irPt.X = 0 And irPt.Y = 0 And i > 0) Or (irPt.X >= dst2.Width Or irPt.Y >= dst2.Height) Then
                            brick.depth = 0 ' off the image
                            brick.lRect = emptyRect
                            brick.rRect = emptyRect
                        Else
                            brick.lRect = New cv.Rect(irPt.X, irPt.Y, brick.rect.Width, brick.rect.Height)
                            brick.lRect = ValidateRect(brick.lRect)

                            Dim LtoR_Pt = Intrinsics_Basics.translate_LeftToRight(task.pointCloud.Get(Of cv.Point3f)(brick.lRect.Y,
                                                                                                                     brick.lRect.X))
                            If LtoR_Pt.X < 0 Or (LtoR_Pt.X = 0 And LtoR_Pt.Y = 0 And i > 0) Or
                                (LtoR_Pt.X >= dst2.Width Or LtoR_Pt.Y >= dst2.Height) Then

                                brick.depth = 0 ' off the image
                                brick.lRect = emptyRect
                                brick.rRect = emptyRect
                            Else
                                brick.rRect = New cv.Rect(LtoR_Pt.X, LtoR_Pt.Y, brick.rect.Width, brick.rect.Height)
                                brick.rRect = ValidateRect(brick.rRect)
                            End If
                        End If
                    End If

                    If brick.depth > 0 Then ' depth can be zero if the translation of color to left fails or left to right fails.
                        cv.Cv2.MatchTemplate(leftview(brick.lRect), rightView(brick.rRect), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                        brick.correlation = correlationMat.Get(Of Single)(0, 0)

                        Dim p0 = getWorldCoordinates(brick.rect.TopLeft, brick.depth)
                        Dim p1 = getWorldCoordinates(brick.rect.BottomRight, brick.depth)

                        ' clockwise around starting in upper left.
                        brick.corners.Add(New cv.Point3f(p0.X, p0.Y, brick.depth))
                        brick.corners.Add(New cv.Point3f(p1.X, p0.Y, brick.depth))
                        brick.corners.Add(New cv.Point3f(p1.X, p1.Y, brick.depth))
                        brick.corners.Add(New cv.Point3f(p0.X, p1.Y, brick.depth))
                    End If
                End If
                brick.depthRanges.Add(brick.mm.range)
                brick.corrHistory.Add(brick.correlation)
            Else
                Dim k = 0
            End If

            dst2(brick.rect).SetTo(brick.color)

            If brick.depth > 0 Then depthCount += 1
            If brick.depthRanges.Count > task.historyCount Then
                brick.depthRanges.RemoveAt(0)
                brick.corrHistory.RemoveAt(0)
            End If

            task.brickList.Add(brick)
        Next

        If task.heartBeat Then labels(2) = "Of " + CStr(task.brickList.Count) + " bricks, " + CStr(depthCount) +
                                           " have useful depth data and " + CStr(unchangedCount) + " were unchanged"

        If task.mouseMovePoint.X < 0 Or task.mouseMovePoint.X >= dst2.Width Then Exit Sub
        If task.mouseMovePoint.Y < 0 Or task.mouseMovePoint.Y >= dst2.Height Then Exit Sub
        Dim index As Integer = task.brickMap.Get(Of Single)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        task.gcD = task.brickList(index)

        Dim pt = task.gcD.rect.TopLeft
        If pt.X > dst2.Width * 0.85 Or (pt.Y < dst2.Height * 0.15 And pt.X > dst2.Width * 0.15) Then
            pt.X -= dst2.Width * 0.15
        Else
            pt.Y -= task.gcD.rect.Height * 3
        End If

        depthAndCorrelationText = Format(task.gcD.depth, fmt3) + " ID=" +
                                  CStr(task.gcD.index) + vbCrLf + "depth " + Format(task.gcD.mm.minVal, fmt1) + "-" +
                                  Format(task.gcD.mm.maxVal, fmt1) + "m, age = " + CStr(task.gcD.age) + vbCrLf +
                                  "correlation = " + Format(task.gcD.correlation, fmt3)
        ptCursor = validatePoint(task.mouseMovePoint)
        ptTextLoc = pt
        ptTopLeft = ptCursor ' task.gcD.rect.TopLeft ' in case it needs to switch back...
    End Sub
End Class






Public Class Brick_Plot : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        plot.addLabels = False
        labels(2) = "Click anywhere In the image To the histogram Of that the depth In that cell."
        desc = "Select any cell To plot a histogram Of that cell's depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.brickBasics.dst2

        Dim index As Integer = task.brickMap.Get(Of Single)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If task.brickList.Count = 0 Or task.optionsChanged Then Exit Sub

        Dim brick As brickData
        If index < 0 Or index >= task.brickList.Count Then
            brick = task.brickList(task.brickList.Count / 2)
            task.mouseMovePoint = New cv.Point(brick.rect.X + brick.rect.Width / 2, brick.rect.Y + brick.rect.Height / 2)
        Else
            brick = task.brickList(index)
        End If

        Dim split() = task.pointCloud(brick.rect).Split()
        Dim mm = GetMinMax(split(2))
        If Single.IsInfinity(mm.maxVal) Then Exit Sub

        If Math.Abs(mm.maxVal - mm.minVal) > 0 Then
            plot.minRange = mm.minVal
            plot.maxRange = mm.maxVal
            plot.Run(split(2))
            dst3 = plot.dst2
            labels(3) = "Depth values vary from " + Format(plot.minRange, fmt3) +
                            " to " + Format(plot.maxRange, fmt3)
        End If
    End Sub
End Class






Public Class Brick_FullDepth : Inherits TaskParent
    Public Sub New()
        labels(2) = "Left image bricks - no overlap.  Click in any column to highlight that column."
        labels(3) = "Right image: corresponding bricks.  Overlap indicates uncertainty about depth."
        desc = "Display the bricks for all cells with depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.rgbLeftAligned Then
            dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Else
            dst2 = src
        End If
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim col As Integer, cellsPerRow = task.cellsPerRow
        Static whiteCol As Integer = cellsPerRow / 2
        If task.mouseClickFlag Then
            whiteCol = Math.Round(cellsPerRow * (task.ClickPoint.X - task.cellSize / 2) / dst2.Width)
        End If
        For Each brick In task.brickList
            If brick.depth > 0 Then
                Dim color = If(col = whiteCol, cv.Scalar.Black, task.scalarColors(255 * (col / cellsPerRow)))
                dst2.Rectangle(brick.rect, color, task.lineWidth)
                dst3.Rectangle(brick.rRect, color, task.lineWidth)
            End If
            col += 1
            If col >= cellsPerRow Then col = 0
        Next
    End Sub
End Class








Public Class Brick_InstantUpdate : Inherits TaskParent
    Public Sub New()
        task.brickBasics.instantUpdate = True
        labels(3) = "Pointcloud image for cells with good visibility"
        desc = "Create the grid of bricks with good visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then labels(2) = CStr(task.brickList.Count) + " bricks have reasonable depth."

        dst2 = task.brickBasics.dst2
        dst3 = task.brickBasics.dst3

        labels(2) = task.brickBasics.labels(2)
    End Sub
End Class







Public Class Brick_Edges : Inherits TaskParent
    Public edges As New Edge_Basics
    Public Sub New()
        task.featureMask = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        task.fLessMask = New cv.Mat(dst3.Size, cv.MatType.CV_8U)
        task.featureOptions.EdgeMethods.SelectedItem() = "Laplacian"
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
        dst3.SetTo(0)
        src.CopyTo(dst2, task.featureMask)
        src.CopyTo(dst3, task.featureMask)

        For Each r In flipRects
            dst2.Rectangle(r, task.highlight, task.lineWidth)
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







Public Class Brick_MLColor : Inherits TaskParent
    Dim ml As New ML_Basics
    Dim bounds As New Brick_FeaturesAndEdges
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        ml.buildEveryPass = True
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        desc = "Train an ML tree to predict each pixel of the boundary cells using only color from boundary neighbors."
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
                Dim x As Integer = Math.Floor(roiA.X * task.cellsPerRow / task.cols)
                Dim y As Integer = Math.Floor(roiA.Y * task.cellsPerCol / task.rows)
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






Public Class Brick_MLColorDepth : Inherits TaskParent
    Dim ml As New ML_Basics
    Dim bounds As New Brick_FeaturesAndEdges
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
                Dim x As Integer = Math.Floor(roiA.X * task.cellsPerRow / task.cols)
                Dim y As Integer = Math.Floor(roiA.Y * task.cellsPerCol / task.rows)
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








Public Class Brick_FeaturesAndEdges : Inherits TaskParent
    Public feat As New Brick_Edges
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




Public Class Brick_Features : Inherits TaskParent
    Dim feat As New Feature_Basics
    Public Sub New()
        task.featureOptions.DistanceSlider.Value = 3
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Featureless areas"
        desc = "Identify the cells with features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)

        dst2 = task.brickBasics.dst2

        For i = 0 To task.brickList.Count - 1
            Dim brick = task.brickList(i)
            brick.features.Clear()
            task.brickList(i) = brick
        Next

        task.featurePoints.Clear()
        Dim rects As New List(Of cv.Rect)
        For Each pt In task.features
            Dim index As Integer = task.brickMap.Get(Of Single)(pt.Y, pt.X)
            Dim brick = task.brickList(index)
            brick.features.Add(pt)
            DrawCircle(dst2, brick.rect.TopLeft, task.DotSize, task.highlight)

            rects.Add(brick.rect)
            task.brickList(index) = brick
        Next

        task.featureRects.Clear()
        task.fLessRects.Clear()
        For Each brick In task.brickList
            If brick.features.Count > 0 Then task.featureRects.Add(brick.rect) Else task.fLessRects.Add(brick.rect)
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








Public Class Brick_EdgeDraw : Inherits TaskParent
    Dim regions As New Region_Contours
    Public Sub New()
        desc = "Lines can mean cells are connected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        regions.Run(src)
        dst2 = regions.dst3
        labels(2) = regions.labels(2)

        dst2.SetTo(cv.Scalar.White, task.edges.dst2) ' edgeLine_Basics has already been run as part of fcsBasics.
    End Sub
End Class







Public Class Brick_RegionLines : Inherits TaskParent
    Dim regions As New Region_Contours
    Public Sub New()
        desc = "Lines can mean cells are connected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        regions.Run(src)
        dst2 = regions.dst2
        dst3 = regions.dst3
        labels = regions.labels

        For Each lp In task.lpList
            Dim c1 = dst2.Get(Of cv.Vec3b)(lp.p1.Y, lp.p1.X)
            Dim c2 = dst2.Get(Of cv.Vec3b)(lp.p2.Y, lp.p2.X)
            If c1 <> c2 Then
                dst3.Line(lp.p1, lp.p2, cv.Scalar.White, task.lineWidth)
            Else
                dst2.Line(lp.p1, lp.p2, cv.Scalar.White, task.lineWidth)
            End If
        Next
    End Sub
End Class










Public Class Brick_Correlation : Inherits TaskParent
    Public Sub New()
        desc = "Given a left image cell, find it's match in the right image, and display their correlation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then Exit Sub ' settle down first...

        If task.gOptions.LRMeanSubtraction.Checked Then
            dst2 = task.LRMeanSub.dst2
            dst3 = task.LRMeanSub.dst3
        Else
            dst2 = task.leftView
            dst3 = task.rightView
        End If

        Dim index As Integer = task.brickMap.Get(Of Single)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If index < 0 Or index > task.brickList.Count Then Exit Sub

        Dim brick = task.brickList(index)
        Dim pt = task.brickBasics.ptCursor
        Dim corr = brick.correlation
        dst2.Circle(brick.lRect.TopLeft, task.DotSize, 255, -1)
        SetTrueText("Corr. " + Format(corr, fmt3) + vbCrLf, pt, 2)
        labels(3) = "Correlation of the left grid cell to the right is " + Format(corr, fmt3)

        Dim grayScale As Integer = If(task.gOptions.LRMeanSubtraction.Checked, 128, 255)
        dst2.Rectangle(brick.lRect, grayScale, task.lineWidth)
        dst3.Rectangle(brick.rRect, grayScale, task.lineWidth)

        dst2.Rectangle(brick.hoodRect, grayScale, task.lineWidth)
        dst3.Rectangle(brick.rHoodRect, grayScale, task.lineWidth)

        labels(2) = "The correlation coefficient at " + pt.ToString + " is " + Format(corr, fmt3)
    End Sub
End Class




Public Class Brick_Info : Inherits TaskParent
    Public Sub New()
        task.ClickPoint = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        desc = "Display the info about the select grid cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        labels(2) = task.brickBasics.labels(2)

        Dim index As Integer = task.brickMap.Get(Of Single)(task.mouseMovePoint.Y, task.mouseMovePoint.X)

        Dim brick As brickData = task.brickList(index)
        dst2 = task.brickBasics.dst2

        strOut = labels(2) + vbCrLf + vbCrLf

        dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
        dst2.Rectangle(brick.hoodRect, task.highlight, task.lineWidth)

        strOut += CStr(index) + vbTab + "Grid ID" + vbCrLf
        strOut += CStr(brick.age) + vbTab + "Age" + vbTab + vbCrLf
        strOut += Format(brick.correlation, fmt3) + vbTab + "Correlation to right image" + vbCrLf
        strOut += Format(brick.depth, fmt3) + vbTab + "Depth" + vbCrLf
        strOut += Format(brick.mm.minVal, fmt3) + vbTab + "Depth mm.minval" + vbCrLf
        strOut += Format(brick.mm.maxVal, fmt3) + vbTab + "Depth mm.maxval" + vbCrLf
        strOut += Format(brick.mm.range, fmt3) + vbTab + "Depth mm.range" + vbCrLf

        strOut += "Depth range history: " + vbTab
        For Each ele In brick.depthRanges
            strOut += Format(ele, fmt3) + vbTab
        Next

        strOut += vbCrLf + vbCrLf + "Correlation history: " + vbTab
        For Each ele In brick.corrHistory
            strOut += Format(ele, fmt3) + vbTab
        Next

        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class Brick_LeftToColor : Inherits TaskParent
    Public Sub New()
        If task.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then task.gOptions.gravityPointCloud.Checked = False
        desc = "Align grid cell left rectangles in color with the left image.  StereoLabs and Orbbec already match."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3.SetTo(0)
        Dim count As Integer
        For Each brick In task.brickList
            If brick.depth > 0 Then
                count += 1
                task.color.Circle(brick.rect.TopLeft, task.DotSize, task.highlight, -1)
                dst2.Circle(brick.lRect.TopLeft, task.DotSize, task.highlight, -1)
                dst3.Circle(brick.lRect.TopLeft, task.DotSize, task.highlight, -1)
            End If
        Next
        labels(2) = CStr(count) + " bricks have depth and therefore an equivalent in the left and right views."
    End Sub
End Class






Public Class Brick_FitLeftInColor : Inherits TaskParent
    Public Sub New()
        desc = "Translate the left image into the same coordinates as the color image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim correlationMat As New cv.Mat

        Dim p1 = task.brickList(0).lRect.TopLeft
        Dim p2 = task.brickList(task.brickList.Count - 1).lRect.BottomRight

        Dim rect = ValidateRect(New cv.Rect(p1.X - task.cellSize, p1.Y - task.cellSize, task.cellSize * 2, task.cellSize * 2))
        cv.Cv2.MatchTemplate(task.gray(New cv.Rect(0, 0, dst1.Width / 2, dst1.Height / 2)), task.leftView, dst2,
                                       cv.TemplateMatchModes.CCoeffNormed)
        Dim mm = GetMinMax(dst2)
        dst3 = src(ValidateRect(New cv.Rect(mm.maxLoc.X / 2, mm.maxLoc.Y / 2, dst2.Width, dst2.Height)))
        labels(2) = "Correlation coefficient peak = " + Format(mm.maxVal, fmt3)
    End Sub
End Class








Public Class Brick_Lines : Inherits TaskParent
    Dim info As New LineRGB_Info
    Public Sub New()
        desc = "Lines can mean cells are connected - click on any highlighted grid cell to see info on that line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.lineRGB.dst2

        dst3.SetTo(0)
        If task.heartBeat Then info.Run(emptyMat)
        For Each index In task.lpD.bricks
            Dim brick = task.brickList(index)
            If brick.index <> 0 Then dst3.Rectangle(brick.rect, task.highlight, 1, task.lineType)
        Next
        dst3.Line(task.lpD.p1, task.lpD.p2, white, task.lineWidth, task.lineType)

        SetTrueText(info.strOut, 3)

        dst2 = ShowPalette(task.lpMap.ConvertScaleAbs())
        labels(2) = task.lineRGB.labels(2) + " - Click on any line in below to get details on that line."
    End Sub
End Class









Public Class Brick_CorrelationMap : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Public Sub New()
        labels(3) = "The map to identify each grid cell."
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Display a heatmap of the correlation of the left and right images for each grid cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        For Each brick In task.brickList
            If brick.depth > 0 Then dst1(brick.rect).SetTo((brick.correlation + 1) * 255 / 2)
        Next

        dst2 = ShowPaletteDepth(dst1)
        labels(2) = task.brickBasics.labels(2)

        ptBrick.Run(src)
    End Sub
End Class






Public Class Brick_LeftRightMouse : Inherits TaskParent
    Public means As New List(Of Single)
    Public Sub New()
        labels(2) = "Move the mouse in the color image to see the matches in left and right images. Click to clear the rectangles."
        labels(3) = "Right view with the translated trace of bricks under the mouse."
        If task.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then task.gOptions.gravityPointCloud.Checked = False
        If standalone Then task.gOptions.displayDst0.Checked = True
        desc = "Map the grid cells from the color image into the left view and the right view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0 = src
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Static myBricks As New List(Of Integer)
        If standalone And task.testAllRunning Then
            Dim index As Integer = task.brickMap.Get(Of Single)(task.ClickPoint.Y, task.ClickPoint.X)
            For i = index To index + 10
                If myBricks.Contains(i) = False Then myBricks.Add(i)
            Next
        Else
            Dim index As Integer = task.brickMap.Get(Of Single)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
            If myBricks.Contains(index) = False Then myBricks.Add(index)
        End If

        For Each index In myBricks
            Dim brick = task.brickList(index)
            dst0.Rectangle(brick.rect, task.highlight, task.lineWidth)
            dst2.Rectangle(brick.lRect, task.highlight, task.lineWidth)
            dst3.Rectangle(brick.rRect, task.highlight, task.lineWidth)
        Next
        If task.mouseClickFlag Then myBricks.Clear()
    End Sub
End Class






Public Class Brick_RGBtoLeft : Inherits TaskParent
    Public Sub New()
        labels(3) = "Right camera image..."
        If task.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then task.gOptions.gravityPointCloud.Checked = False
        desc = "Translate the RGB to left view - only needed for the Intel RealSense cameras."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim camInfo = task.calibData, correlationMat As New cv.Mat
        Dim index As Integer = task.brickMap.Get(Of Single)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        Dim brick As brickData
        If index > 0 And index < task.brickList.Count Then
            brick = task.brickList(index)
        Else
            brick = task.brickList(task.brickList.Count / 2)
        End If

        Dim irPt As cv.Point = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        Dim rgbTop = brick.rect.TopLeft, ir3D As cv.Point3f
        ' stereolabs and orbbec already aligned the RGB and left images so depth in the left image
        ' can be found.  For Intel, the left image and RGB need to be aligned first.
        ' With depth the correlation between the left and right for that grid cell will be accurate (if there is depth.)
        If task.rgbLeftAligned Then
            irPt = brick.rect.TopLeft ' the above cameras are already have RGB aligned to the left image.
        Else
            Dim pcTop = task.pointCloud.Get(Of cv.Point3f)(rgbTop.Y, rgbTop.X)
            If pcTop.Z > 0 Then
                ir3D.X = camInfo.ColorToLeft_rotation(0) * pcTop.X +
                         camInfo.ColorToLeft_rotation(1) * pcTop.Y +
                         camInfo.ColorToLeft_rotation(2) * pcTop.Z + camInfo.ColorToLeft_translation(0)
                ir3D.Y = camInfo.ColorToLeft_rotation(3) * pcTop.X +
                         camInfo.ColorToLeft_rotation(4) * pcTop.Y +
                         camInfo.ColorToLeft_rotation(5) * pcTop.Z + camInfo.ColorToLeft_translation(1)
                ir3D.Z = camInfo.ColorToLeft_rotation(6) * pcTop.X +
                         camInfo.ColorToLeft_rotation(7) * pcTop.Y +
                         camInfo.ColorToLeft_rotation(8) * pcTop.Z + camInfo.ColorToLeft_translation(2)
                irPt.X = camInfo.leftIntrinsics.fx * ir3D.X / ir3D.Z + camInfo.leftIntrinsics.ppx
                irPt.Y = camInfo.leftIntrinsics.fy * ir3D.Y / ir3D.Z + camInfo.leftIntrinsics.ppy
            End If
        End If
        labels(2) = "RGB point at " + rgbTop.ToString + " is at " + irPt.ToString + " in the left view "

        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        dst2.Rectangle(New cv.Rect(irPt.X, irPt.Y, brick.rect.Width, brick.rect.Height), task.highlight, task.lineWidth)
        dst3.Rectangle(brick.rRect, task.highlight, task.lineWidth)
    End Sub
End Class









Public Class Brick_LeftRight : Inherits TaskParent
    Public means As New List(Of Single)
    Public Sub New()
        labels(2) = "Only every other colum is shown to make it clear which bricks are being translated (can get crowded otherwise.)"
        labels(3) = "Right view with the translated bricks shown at left."
        If task.cameraName.StartsWith("Intel(R) RealSense(TM) Depth Camera") Then task.gOptions.gravityPointCloud.Checked = False
        desc = "Map the column of bricks in the color image into the left view and then to the right view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        For i = 0 To task.cellsPerRow - 1 Step 2
            For j = i To task.gridRects.Count - task.cellsPerRow - 1 Step task.cellsPerRow
                Dim brick = task.brickList(j)
                If brick.depth > 0 Then
                    dst2.Rectangle(brick.lRect, task.highlight, task.lineWidth)
                    dst3.Rectangle(brick.rRect, task.highlight, task.lineWidth)
                End If
            Next
        Next
    End Sub
End Class
