Imports VB_Classes.VBtask
Imports cv = OpenCvSharp
Public Class GridCell_Basics : Inherits TaskParent
    Public options As New Options_GridCells
    Public thresholdRangeZ As Single
    Public instantUpdate As Boolean = True
    Public mouseD As New GridCell_MouseDepth
    Public quad As New Quad_Basics
    Dim lastCorrelation() As Single
    Public Sub New()
        task.rgbLeftAligned = If(task.cameraName.StartsWith("StereoLabs") Or task.cameraName.StartsWith("Orbbec"), True, False)
        desc = "Create the grid of grid cells that reduce depth volatility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        If task.optionsChanged Then
            ReDim lastCorrelation(task.gridRects.Count - 1)
        End If

        task.iddList.Clear()
        Dim stdev As cv.Scalar, mean As cv.Scalar
        For Each rect In task.gridRects
            Dim idd As New gridCell
            idd.rect = ValidateRect(rect)
            idd.lRect = idd.rect ' for some cameras the color image and the left image are the same.
            idd.center = New cv.Point(rect.TopLeft.X + idd.rect.Width / 2, rect.TopLeft.Y + idd.rect.Height / 2)
            If task.depthMask(idd.rect).CountNonZero Then
                cv.Cv2.MeanStdDev(task.pcSplit(2)(idd.rect), mean, stdev, task.depthMask(idd.rect))
                idd.depth = mean(0)
                idd.depthStdev = stdev(0)
            End If
            idd.index = task.iddList.Count
            task.iddList.Add(idd)
        Next

        Dim emptyRect As New cv.Rect, correlationMat As New cv.Mat
        Dim leftview = If(task.gOptions.LRMeanSubtraction.Checked, task.LRMeanSub.dst2, task.leftView)
        Dim rightView = If(task.gOptions.LRMeanSubtraction.Checked, task.LRMeanSub.dst3, task.rightView)
        For i = 0 To task.iddList.Count - 1
            Dim idd = task.iddList(i)
            If task.motionBasics.motionFlag(i) = False Then
                idd.age += 1
                idd.correlation = lastCorrelation(i)
            Else
                idd.age = 1
                If idd.depth = 0 Then
                    idd.correlation = 0
                    idd.rRect = emptyRect
                Else
                    idd.mm = GetMinMax(task.pcSplit(2)(idd.rect), task.depthMask(idd.rect))
                    idd.depthErr = 0.02 * idd.depth / 2
                    If task.rgbLeftAligned Then
                        idd.lRect = idd.rect
                        idd.rRect = idd.lRect
                        idd.rRect.X -= task.calibData.baseline * task.calibData.rgbIntrinsics.fx / idd.depth
                        idd.rRect = ValidateRect(idd.rRect)
                        cv.Cv2.MatchTemplate(leftview(idd.lRect), rightView(idd.rRect), correlationMat,
                                                 cv.TemplateMatchModes.CCoeffNormed)

                        idd.correlation = correlationMat.Get(Of Single)(0, 0)
                    Else
                        Dim irPt = translateColorToLeft(idd.rect.TopLeft)
                        If irPt.X < 0 Or (irPt.X = 0 And irPt.Y = 0 And i > 0) Then
                            idd.depth = 0 ' off the grid.
                            idd.lRect = emptyRect
                            idd.rRect = emptyRect
                        Else
                            idd.lRect = New cv.Rect(irPt.X, irPt.Y, idd.rect.Width, idd.rect.Height)
                            idd.lRect = ValidateRect(idd.lRect)

                            idd.rRect = idd.lRect
                            idd.rRect.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / idd.depth
                            idd.rRect = ValidateRect(idd.rRect)
                            cv.Cv2.MatchTemplate(leftview(idd.lRect), rightView(idd.rRect), correlationMat,
                                                         cv.TemplateMatchModes.CCoeffNormed)

                            idd.correlation = correlationMat.Get(Of Single)(0, 0)
                        End If
                    End If
                End If
            End If

            lastCorrelation(i) = idd.correlation
            task.iddList(i) = idd
        Next

        quad.Run(src)
        dst2 = quad.dst2

        If task.heartBeat Then labels(2) = CStr(task.iddList.Count) + " grid cells have the useful depth values."
    End Sub
End Class





Public Class GridCell_MouseDepth : Inherits TaskParent
    Public ptTopLeft As New cv.Point
    Public depthAndCorrelationText As String
    Public Sub New()
        desc = "Provide the mouse depth at the mouse movement location."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.mouseMovePoint.X < 0 Or task.mouseMovePoint.X >= dst2.Width Then Exit Sub
        If task.mouseMovePoint.Y < 0 Or task.mouseMovePoint.Y >= dst2.Height Then Exit Sub
        Dim index = task.iddMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        task.iddC = task.iddList(index)
        dst2 = task.gCell.dst2

        Dim pt = task.iddC.rect.TopLeft
        If pt.X > dst2.Width * 0.85 Or (pt.Y < dst2.Height * 0.15 And pt.X > dst2.Width * 0.15) Then
            pt.X -= dst2.Width * 0.15
        Else
            pt.Y -= task.iddC.rect.Height * 3
        End If

        depthAndCorrelationText = Format(task.iddC.depth, fmt2) +
                                  "m " + " ID=" +
                                  CStr(task.iddC.index) + vbCrLf + "depth " + Format(task.iddC.mm.minVal, fmt3) + "m - " +
                                  Format(task.iddC.mm.maxVal, fmt3) + vbCrLf + "correlation = " + Format(task.iddC.correlation, fmt3)
        ptTopLeft = pt
        If standaloneTest() Then SetTrueText(depthAndCorrelationText, ptTopLeft, 2)
    End Sub
End Class







Public Class GridCell_Plot : Inherits TaskParent
    Dim plot As New Plot_Histogram
    Public Sub New()
        plot.createHistogram = True
        plot.addLabels = False
        labels(2) = "Click anywhere In the image To the histogram Of that the depth In that cell."
        desc = "Select any cell To plot a histogram Of that cell's depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.gCell.dst2

        Dim index = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If task.iddList.Count = 0 Or task.optionsChanged Then Exit Sub

        Dim idd As gridCell
        If index < 0 Or index >= task.iddList.Count Then
            idd = task.iddList(task.iddList.Count / 2)
            task.mouseMovePoint = New cv.Point(idd.rect.X + idd.rect.Width / 2, idd.rect.Y + idd.rect.Height / 2)
        Else
            idd = task.iddList(index)
        End If

        Dim split() = task.pointCloud(idd.rect).Split()
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






Public Class GridCell_FullDepth : Inherits TaskParent
    Public Sub New()
        labels(2) = "Left image grid cells - no overlap.  Click in any column to highlight that column."
        labels(3) = "Right image: corresponding grid cells.  Overlap indicates uncertainty about depth."
        desc = "Display the grid cells for all cells with depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.rgbLeftAligned Then
            dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Else
            dst2 = src
        End If
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim col As Integer, tilesPerRow = task.grid.tilesPerRow
        Static whiteCol As Integer = tilesPerRow / 2
        If task.mouseClickFlag Then
            whiteCol = Math.Round(tilesPerRow * (task.ClickPoint.X - task.cellSize / 2) / dst2.Width)
        End If
        For Each idd In task.iddList
            If idd.depth > 0 Then
                Dim color = If(col = whiteCol, cv.Scalar.Black, task.scalarColors(255 * (col / tilesPerRow)))
                dst2.Rectangle(idd.rect, color, task.lineWidth)
                dst3.Rectangle(idd.rRect, color, task.lineWidth)
            End If
            col += 1
            If col >= tilesPerRow Then col = 0
        Next
    End Sub
End Class








Public Class GridCell_InstantUpdate : Inherits TaskParent
    Public Sub New()
        task.gCell.instantUpdate = True
        labels(3) = "Pointcloud image for cells with good visibility"
        desc = "Create the grid of grid cells with good visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then labels(2) = CStr(task.iddList.Count) + " grid cells have reasonable depth."

        dst2 = task.gCell.dst2
        labels(2) = task.gCell.labels(2)
    End Sub
End Class





Public Class GridCell_CorrelationMask : Inherits TaskParent
    Dim corrMap As New GridCell_CorrelationMap
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        desc = "Isolate only the depth values under the grid cell correlation mask (see GridCell_CorrelatonMap"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        corrMap.Run(src)
        dst3 = corrMap.dst2

        dst2.SetTo(0)
        labels = corrMap.labels
        task.pointCloud.CopyTo(dst2, corrMap.dst3)
    End Sub
End Class






Public Class GridCell_RGBtoLeft : Inherits TaskParent
    Public Sub New()
        labels(3) = "Right camera image..."
        desc = "Translate the RGB to left view for all cameras except Stereolabs where left is RGB."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim camInfo = task.calibData, correlationMat As New cv.Mat
        Dim index = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        Dim idd As gridCell
        If index > 0 And index < task.iddList.Count Then
            idd = task.iddList(index)
        Else
            idd = task.iddList(task.iddList.Count / 2)
        End If

        Dim irPt As cv.Point = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        Dim rgbTop = idd.rect.TopLeft, ir3D As cv.Point3f
        ' stereolabs and orbbec already aligned the RGB and left images so depth in the left image
        ' can be found.  For Intel and the Oak-D, the left image and RGB need to be aligned to get accurate depth.
        ' With depth the correlation between the left and right for that grid cell will be accurate (if there is depth.)
        ' NOTE: the Intel camera is accurate in X but way off in Y.  Probably my problem...
        If task.cameraName.StartsWith("Intel") Or task.cameraName.StartsWith("Oak-D") Then
            Dim pcTop = task.pointCloud.Get(Of cv.Point3f)(rgbTop.Y, rgbTop.X)
            If pcTop.Z > 0 Then
                ir3D.X = camInfo.rotation(0) * pcTop.X +
                         camInfo.rotation(1) * pcTop.Y +
                         camInfo.rotation(2) * pcTop.Z + camInfo.translation(0)
                ir3D.Y = camInfo.rotation(3) * pcTop.X +
                         camInfo.rotation(4) * pcTop.Y +
                         camInfo.rotation(5) * pcTop.Z + camInfo.translation(1)
                ir3D.Z = camInfo.rotation(6) * pcTop.X +
                         camInfo.rotation(7) * pcTop.Y +
                         camInfo.rotation(8) * pcTop.Z + camInfo.translation(2)
                irPt.X = camInfo.leftIntrinsics.fx * ir3D.X / ir3D.Z + camInfo.leftIntrinsics.ppx
                irPt.Y = camInfo.leftIntrinsics.fy * ir3D.Y / ir3D.Z + camInfo.leftIntrinsics.ppy
            End If
        Else
            irPt = idd.rect.TopLeft ' the above cameras are already have RGB aligned to the left image.
        End If
        labels(2) = "RGB point at " + rgbTop.ToString + " is at " + irPt.ToString + " in the left view "

        dst2 = task.leftView
        dst3 = task.rightView
        Dim r = New cv.Rect(irPt.X, irPt.Y, idd.rect.Width, idd.rect.Height)
        dst2.Rectangle(r, 255, task.lineWidth)

        dst2.Circle(r.TopLeft, task.DotSize, 255, -1)
        ' SetTrueText("Correlation " + Format(idd.correlation, fmt3), task.gCell.mouseD.pt, 2)
    End Sub
End Class







Public Class GridCell_LeftToColor : Inherits TaskParent
    Public Sub New()
        desc = "Align grid cell left rectangles in color with the left image.  StereoLabs and Orbbec already match."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3.SetTo(0)
        Dim count As Integer
        For Each idd In task.iddList
            If idd.depth > 0 Then
                count += 1
                task.color.Circle(idd.rect.TopLeft, task.DotSize, task.HighlightColor, -1)
                dst2.Circle(idd.lRect.TopLeft, task.DotSize, task.HighlightColor, -1)
                dst3.Circle(idd.lRect.TopLeft, task.DotSize, task.HighlightColor, -1)
            End If
        Next
        labels(2) = CStr(count) + " grid cells have depth and therefore an equivalent in the left and right views."
    End Sub
End Class






Public Class GridCell_LeftRightSize : Inherits TaskParent
    Public Sub New()
        desc = "Resize the left image so it is about the same size as the color image.  This is just an approximation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim minX As Integer = Integer.MaxValue, minY As Integer = Integer.MaxValue
        Dim maxX As Integer = Integer.MinValue, maxY As Integer = Integer.MinValue
        For Each idd In task.iddList
            If idd.depth > 0 Then
                If idd.rect.X < minX Then minX = idd.rect.X
                If idd.rect.Y < minY Then minY = idd.rect.Y
                If idd.rect.BottomRight.X > maxX Then maxX = idd.rect.BottomRight.X
                If idd.rect.BottomRight.Y > maxY Then maxY = idd.rect.BottomRight.Y
            End If
        Next

        If minX >= 0 And minX < dst2.Width And minY >= 0 And minY < dst2.Height And maxX < dst2.Width And maxY < dst2.Height Then
            Dim rect = New cv.Rect(minX, minY, maxX - minX, maxY - minY)
            dst2 = task.leftView(rect).Resize(task.color.Size)
        End If
    End Sub
End Class








Public Class GridCell_Correlation : Inherits TaskParent
    Public Sub New()
        desc = "Given a left image cell, find it's match in the right image, and display their correlation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then Exit Sub ' settle down first...

        dst2 = task.leftView
        dst3 = task.rightView
        Dim index = task.gridMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        If index < 0 Or index > task.iddList.Count Then Exit Sub

        Dim idd = task.iddList(index)
        Dim pt = task.gCell.mouseD.ptTopLeft
        Dim corr = idd.correlation
        dst2.Circle(idd.lRect.TopLeft, task.DotSize, 255, -1)
        SetTrueText("Correlation " + Format(corr, fmt3), pt, 2)
        labels(3) = "Correlation of the left grid cell to the right is " + Format(corr, fmt3)

        dst2.Rectangle(idd.lRect, 255, task.lineWidth)
        dst3.Rectangle(idd.rRect, 255, task.lineWidth)
        labels(2) = "The correlation coefficient at " + pt.ToString + " is " + Format(corr, fmt3)
    End Sub
End Class






Public Class GridCell_GrayScaleTest : Inherits TaskParent
    Dim options As New Options_Stdev
    Public Sub New()
        labels(3) = "grid cells where grayscale stdev and average of the 3 color stdev's"
        desc = "Is the average of the color stdev's the same as the stdev of the grayscale?"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        Dim threshold = options.stdevThreshold

        Dim pt = task.gCell.mouseD.ptTopLeft
        Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
        Static saveTrueData As New List(Of TrueText)
        If task.heartBeat Then
            dst3.SetTo(0)
            dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim count As Integer
            For Each idd In task.iddList
                cv.Cv2.MeanStdDev(dst2(idd.rect), grayMean, grayStdev)
                Dim colorStdev = (idd.colorStdev(0) + idd.colorStdev(1) + idd.colorStdev(2)) / 3
                Dim diff = Math.Abs(grayStdev(0) - colorStdev)
                If diff > threshold Then
                    dst2.Rectangle(idd.rect, 255, task.lineWidth)
                    SetTrueText(Format(grayStdev(0), fmt1) + " " + Format(colorStdev, fmt1), idd.rect.TopLeft, 2)
                    dst3.Rectangle(idd.rect, task.HighlightColor, task.lineWidth)
                    SetTrueText(Format(diff, fmt1), idd.rect.TopLeft, 3)
                    count += 1
                End If
            Next
            labels(2) = "There were " + CStr(count) + " cells where the difference was greater than " + CStr(threshold)
        End If

        If trueData.Count > 0 Then saveTrueData = New List(Of TrueText)(trueData)
        trueData = New List(Of TrueText)(saveTrueData)
    End Sub
End Class








Public Class GridCell_Edges : Inherits TaskParent
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
        dst3.SetTo(0)
        src.CopyTo(dst2, task.featureMask)
        src.CopyTo(dst3, task.featureMask)

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







Public Class GridCell_MLColor : Inherits TaskParent
    Dim ml As New ML_Basics
    Dim bounds As New GridCell_FeaturesAndEdges
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






Public Class GridCell_MLColorDepth : Inherits TaskParent
    Dim ml As New ML_Basics
    Dim bounds As New GridCell_FeaturesAndEdges
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







Public Class GridCell_Validate : Inherits TaskParent
    Dim measure As New GridCell_MeasureMotion
    Public Sub New()
        If standalone Then task.gOptions.showMotionMask.Checked = True
        labels(1) = "Every pixel is slightly different except where motion is detected."
        labels(3) = "Differences are individual pixels and not many are connected to other pixels."
        desc = "Validate the image provided by GridCell_MeasureMotion"
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

        If Not task.heartBeat Then
            Static diff As New Diff_Basics
            diff.lastFrame = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            diff.Run(src)
            dst3 = diff.dst2
        End If
    End Sub
End Class





Public Class GridCell_FeatureGaps : Inherits TaskParent
    Dim feat As New GridCell_Features
    Dim gaps As New Connected_Gaps
    Public Sub New()
        labels(2) = "The output of GridCell_Gaps overlaid with the output of the GridCell_Features"
        desc = "Overlay the features on the image of the gaps"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)
        gaps.Run(src)
        dst2 = ShowAddweighted(feat.dst2, gaps.dst2, labels(3))
    End Sub
End Class







Public Class GridCell_FeaturesAndEdges : Inherits TaskParent
    Public feat As New GridCell_Edges
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




Public Class GridCell_Features : Inherits TaskParent
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

        dst2 = task.gCell.dst2

        For i = 0 To task.iddList.Count - 1
            Dim idd = task.iddList(i)
            idd.features.Clear()
            task.iddList(i) = idd
        Next

        task.featurePoints.Clear()
        Dim rects As New List(Of cv.Rect)
        For Each pt In task.features
            Dim index = task.iddMap.Get(Of Integer)(pt.Y, pt.X)
            Dim idd = task.iddList(index)
            idd.features.Add(pt)
            DrawCircle(dst2, idd.rect.TopLeft, task.DotSize, task.HighlightColor)

            rects.Add(idd.rect)
            task.iddList(index) = idd
        Next

        task.featureRects.Clear()
        task.fLessRects.Clear()
        For Each idd In task.iddList
            If idd.features.Count > 0 Then task.featureRects.Add(idd.rect) Else task.fLessRects.Add(idd.rect)
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








Public Class GridCell_Boundaries : Inherits TaskParent
    Public Sub New()
        desc = "Find cells that have high depth variability indicating that cell is a boundary."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.depthRGB.Clone
        dst1.SetTo(0)
        For Each idd In task.iddList
            If idd.correlation > 0 Then
                If (idd.mm.maxVal - idd.mm.minVal) - idd.depthErr > task.depthDiffMeters Then
                    dst2.Rectangle(idd.rect, 0, -1)
                    dst1.Rectangle(idd.rect, cv.Scalar.White, -1)
                End If
            End If
        Next

        dst3 = ShowAddweighted(dst1, src, labels(3))
    End Sub
End Class







Public Class GridCell_Lines : Inherits TaskParent
    Dim regions As New Connected_Contours
    Public Sub New()
        desc = "Lines can mean cells are connected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        regions.Run(src)
        dst2 = regions.dst3

        task.lines.Run(src)
        For Each lp In task.lpList
            dst2.Line(lp.p1, lp.p2, cv.Scalar.White, task.lineWidth)
        Next
    End Sub
End Class







Public Class GridCell_EdgeDraw : Inherits TaskParent
    Dim regions As New Connected_Contours
    Public edges As New EdgeLine_Basics
    Public Sub New()
        desc = "Lines can mean cells are connected."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        regions.Run(src)
        dst2 = regions.dst3

        edges.Run(src)
        dst2.SetTo(cv.Scalar.White, edges.dst2)
    End Sub
End Class





Public Class GridCell_ColorLines : Inherits TaskParent
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Remove lines which cross grid cells that have the same depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)
        dst2 = src.Clone
        dst3.SetTo(0)
        For Each lp In task.lpList
            Dim idd1 = task.iddList(task.iddMap.Get(Of Integer)(lp.p1.Y, lp.p1.X))
            Dim idd2 = task.iddList(task.iddMap.Get(Of Integer)(lp.p2.Y, lp.p2.X))
            dst3.Line(lp.p1, lp.p2, 128, task.lineWidth)
            If Math.Abs(idd1.depth - idd2.depth) >= task.depthDiffMeters Then
                dst2.Line(lp.p1, lp.p2, cv.Scalar.White, task.lineWidth)
                dst3.Line(lp.p1, lp.p2, 255, task.lineWidth)
            End If
        Next
    End Sub
End Class








Public Class GridCell_Stdev : Inherits TaskParent
    Dim options As New Options_GridStdev
    Public Sub New()
        desc = "Visualize the depth and color standard deviation for each grid cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2.SetTo(0)
        dst3.SetTo(0)
        For Each idd In task.iddList
            If idd.depthStdev > options.depthThreshold Then
                ' dst2(idd.rect).SetTo()
            End If
        Next
    End Sub
End Class






Public Class GridCell_MeanSubtraction : Inherits TaskParent
    Dim LRMeanSub As New MeanSubtraction_LeftRight
    Public Sub New()
        task.gCell.instantUpdate = True
        ' labels = {"", "", "This is the grid cell map using mean subtraction on the left and right images", ""}
        desc = "Use the mean subtraction output of the left and right images as input to the GridCell_Basics.  NOTE: instant update!"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        LRMeanSub.Run(src)

        task.leftView = LRMeanSub.dst2
        task.rightView = LRMeanSub.dst3

        task.gCell.Run(src)
        task.colorizer.Run(src)
        dst2 = task.colorizer.buildCorrMap.dst2

        labels(2) = task.gCell.labels(2)
        SetTrueText("dst2 is the grid cell map using mean subtraction on the left and right images." + vbCrLf +
                    "dst1 (above right) shows the correlation map produced with the original left and right images.", 3)
    End Sub
End Class






Public Class GridCell_LeftRight : Inherits TaskParent
    Public means As New List(Of Single)
    Public Sub New()
        labels(2) = "Draw above in the color image to see the matches in left and right images"
        labels(3) = "Right view with the translated drawrect."
        task.drawRect = New cv.Rect(dst2.Width / 2 - 20, dst2.Height / 2 - 20, 40, 40)
        desc = "Map Each grid cell into the right view."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView
        dst3 = task.rightView

        Dim indexTop = task.iddMap.Get(Of Integer)(task.drawRect.Y, task.drawRect.X)
        If indexTop < 0 Or indexTop >= task.iddList.Count Then Exit Sub
        Dim indexBot = task.iddMap.Get(Of Integer)(task.drawRect.BottomRight.Y, task.drawRect.BottomRight.X)
        If indexBot < 0 Or indexBot >= task.iddList.Count Then Exit Sub

        Dim idd1 = task.iddList(indexTop)
        Dim idd2 = task.iddList(indexBot)

        Dim w = idd2.lRect.BottomRight.X - idd1.lRect.TopLeft.X
        Dim h = idd2.lRect.BottomRight.Y - idd1.lRect.TopLeft.Y
        Dim rectLeft = New cv.Rect(idd1.lRect.X, idd1.lRect.Y, w, h)

        w = idd2.rRect.BottomRight.X - idd1.rRect.TopLeft.X
        h = idd2.rRect.BottomRight.Y - idd1.rRect.TopLeft.Y
        Dim rectRight = New cv.Rect(idd1.rRect.X, idd1.rRect.Y, w, h)

        dst2.Rectangle(rectLeft, 0, task.lineWidth)
        dst3.Rectangle(rectRight, 0, task.lineWidth)
    End Sub
End Class






Public Class GridCell_MeasureMotion : Inherits TaskParent
    Public motionRects As New List(Of cv.Rect)
    Dim percentList As New List(Of Single)
    Public motionList As New List(Of Integer)
    Public Sub New()
        labels(3) = "A composite of an earlier image and the motion since that input.  " +
                    "Any object boundaries are unlikely to be different."
        desc = "Show all the grid cells above the motionless value (an option)."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then dst0 = src.Clone

        If task.optionsChanged Then motionRects = New List(Of cv.Rect)

        dst2 = task.gCell.dst2
        labels(2) = task.gCell.labels(3)

        motionRects.Clear()
        Dim indexList As New List(Of Integer) ' avoid adding the same cell more than once.
        Dim motionFlags = task.motionBasics.motionFlag
        For Each idd In task.iddList
            If motionFlags(idd.index) Then
                For Each index In task.gridNeighbors(idd.index)
                    If indexList.Contains(index) = False Then
                        indexList.Add(index)
                        motionRects.Add(task.gridRects(index))
                    End If
                Next
            End If
        Next

        ' Use the whole image for the first few images as camera stabilizes.
        If task.frameCount < 3 Then
            src.CopyTo(dst3)
            motionRects.Clear()
            motionRects.Add(New cv.Rect(0, 0, dst2.Width, dst2.Height))
        Else
            If motionRects.Count > 0 Then
                For Each roi In motionRects
                    src(roi).CopyTo(dst3(roi))
                    If standaloneTest() Then dst0.Rectangle(roi, white, task.lineWidth)
                Next
            End If
        End If

        If task.heartBeat Or task.optionsChanged Then
            percentList.Add(motionList.Count / task.gridRects.Count)
            If percentList.Count > 10 Then percentList.RemoveAt(0)
            task.motionPercent = percentList.Average
            labels(3) = " Average motion per image: " + Format(task.motionPercent, "0%")
            task.motionLabel = labels(3)
        End If
    End Sub
End Class







Public Class GridCell_CorrelationMap : Inherits TaskParent
    Public Sub New()
        labels(3) = "The correlation values are in dst2"
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Display a heatmap of the correlation of the left and right images for each grid cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)

        Dim count As Integer
        For Each idd In task.iddList
            If idd.depth > 0 Then
                dst1(idd.rect).SetTo((idd.correlation + 1) * 255 / 2)
                If idd.correlation > 0 Then count += 1
            Else
                dst1(idd.rect).SetTo(0)
            End If
        Next

        dst2 = ShowPaletteDepth(dst1)
        labels(2) = task.gCell.labels(2) + " and " + CStr(count) + " had a correlation."
    End Sub
End Class