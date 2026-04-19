Imports cv = OpenCvSharp
Public Class BrickPoint_Basics : Inherits TaskParent
    Public sobel As New Edge_Sobel
    Public bpCore As New BrickPoint_Core
    Public ptList As New List(Of cv.Point)
    Public Sub New()
        labels(3) = "Sobel input to BrickPoint_Basics"
        desc = "Find the max Sobel point in each r"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        sobel.Run(src)
        dst3 = sobel.dst2

        bpCore.Run(dst3)
        dst2 = bpCore.dst2
        ptList = New List(Of cv.Point)(bpCore.ptList)

        For Each pt In ptList
            DrawCircle(dst3, pt, 255)
        Next
        labels(2) = bpCore.labels(2)
    End Sub
End Class






Public Class BrickPoint_Core : Inherits TaskParent
    Public bricks As New Brick_Basics
    Public ptList As New List(Of cv.Point)
    Public options As New Options_BrickPoint
    Public Sub New()
        desc = "Identify the highest intensity point in each grid square given the input image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        bricks.Run(src)
        If standalone Then
            Static sobel As New Edge_Sobel
            sobel.Run(src)
            src = sobel.dst2
        End If

        dst2 = task.color.Clone
        ptList.Clear()
        For Each brick In bricks.brickList
            brick.mm = GetMinMax(src(brick.rect))
            Dim pt = New cv.Point(brick.mm.maxLoc.X + brick.rect.X, brick.mm.maxLoc.Y + brick.rect.Y)
            If brick.mm.maxVal >= options.threshold Then ptList.Add(pt)
        Next

        For Each pt In ptList
            DrawCircle(dst2, pt)
        Next

        labels(2) = "Of the " + CStr(task.gridRects.Count) + " candidates, " + CStr(ptList.Count) +
                            " had brickpoint intensity >= " + CStr(options.threshold)
    End Sub
End Class








Public Class BrickPoint_Plot : Inherits TaskParent
    Dim plotHist As New PlotBar_Basics
    Dim bPoint As New BrickPoint_Basics
    Public Sub New()
        task.gOptions.setHistogramBins(3)
        plotHist.maxRange = 255
        plotHist.minRange = 0
        plotHist.removeZeroEntry = False
        plotHist.createHistogram = True
        desc = "Plot the distribution of Sobel values for each ptBrick cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoint.Run(task.gray)

        Dim sobelValues As New List(Of Byte)
        For Each brick In bPoint.bpCore.bricks.brickList
            sobelValues.Add(brick.mm.maxVal)
        Next
        plotHist.Run(cv.Mat.FromPixelData(sobelValues.Count, 1, cv.MatType.CV_8U, sobelValues.ToArray))
        dst2 = plotHist.dst2

        Dim incr = (plotHist.maxRange - plotHist.minRange) / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / (dst2.Width / task.histogramBins))
        Dim minVal = CInt(histIndex * incr)
        Dim maxVal = CInt((histIndex + 1) * incr)
        labels(3) = "Sobel peak values from " + CStr(minVal) + " to " + CStr(maxVal)

        dst3 = src
        For Each brick In bPoint.bpCore.bricks.brickList
            If brick.mm.maxVal <= maxVal And brick.mm.maxVal >= minVal Then
                DrawCircle(dst3, New cv.Point(brick.mm.maxLoc.X + brick.rect.X, brick.mm.maxLoc.Y + brick.rect.Y))
            End If
        Next
        labels(2) = "There were " + CStr(sobelValues.Count) + " points found.  Cursor over each bar to see where they originated from"
    End Sub
End Class






Public Class NR_BrickPoint_TopRow : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim bPoint As New BrickPoint_Basics
    Public Sub New()
        labels(3) = "BrickPoint_Basics output of intensity = 255 - not necessarily in the top row of the r."
        desc = "BackProject the top row of the survey results into the RGB image - might help identify vertical lines (see dst3)."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        bPoint.Run(src)
        dst3 = src.Clone
        dst2 = src.Clone

        Dim count As Integer
        For Each brick In bricks.brickList
            If brick.mm.maxLoc = newPoint Then Continue For
            If brick.mm.maxVal <> 255 Then Continue For
            If brick.mm.maxLoc.Y = brick.rect.Y Then
                DrawCircle(dst2, brick.mm.maxLoc)
                DrawCircle(dst3, brick.rect.TopLeft)
                count += 1
            End If
        Next

        labels(2) = "Of the " + CStr(bPoint.ptList.Count) + " max intensity bricks " + CStr(count) +
                            " had max intensity in the top row of the brick."
    End Sub
End Class






Public Class NR_BrickPoint_DistanceAbove : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim plotHist As New PlotBar_Basics
    Public Sub New()
        plotHist.createHistogram = True
        plotHist.removeZeroEntry = False
        desc = "Show grid points based on their distance to the grid point above."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        Dim lpList As New List(Of lpData)

        Dim lpZero As New lpData(New cv.Point, New cv.Point)
        For Each brick In bricks.brickList
            If brick.rect.Y = 0 Then
                lpList.Add(lpZero)
            Else
                Dim brick1 = bricks.brickList(brick.index - task.bricksPerRow)
                Dim pt = New cv.Point(brick.mm.maxLoc.X + brick.rect.X, brick.mm.maxLoc.Y + brick.rect.Y)
                Dim ptbrick = New cv.Point(brick.mm.maxLoc.X + brick.rect.X, brick.mm.maxLoc.Y + brick.rect.Y)
                Dim lp = New lpData(pt, ptbrick)
                lpList.Add(lp)
            End If
        Next

        Dim lengths As New List(Of Single)
        For Each lp In lpList
            lengths.Add(lp.length)
        Next

        Dim minLen = lengths.Min, maxLen = lengths.Max
        If maxLen = task.brickEdgeLen And minLen = task.brickEdgeLen Then Exit Sub

        plotHist.Run(cv.Mat.FromPixelData(lengths.Count, 1, cv.MatType.CV_32F, lengths.ToArray))
        dst2 = plotHist.dst2

        Dim brickRange = (maxLen - minLen) / task.histogramBins
        Dim histList = plotHist.histArray.ToList
        Dim histindex = histList.IndexOf(histList.Max)
        histList(histindex) = 0
        Dim histindex1 = histList.IndexOf(histList.Max)
        Dim min = Math.Min(CInt((histindex) * brickRange), CInt((histindex1) * brickRange))
        Dim max = Math.Max(CInt((histindex + 1) * brickRange), CInt((histindex1 + 1) * brickRange))

        dst3 = src
        For Each brick In bricks.brickList
            Dim lp = lpList(brick.index)
            If lp.length < min Or lp.length > max Then Continue For
            dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineWidth)
        Next
    End Sub
End Class







Public Class NR_BrickPoint_Best : Inherits TaskParent
    Dim bPoint As New BrickPoint_Basics
    Public bestBricks As New List(Of cv.Point)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Display the grid points that have the highest possible max val - indicating the quality of the point."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoint.Run(task.gray)
        labels(2) = bPoint.labels(2)

        dst2 = src.Clone
        dst3.SetTo(0)
        bestBricks.Clear()
        For Each pt In bPoint.ptList
            bestBricks.Add(pt)
            DrawCircle(dst2, pt)
            DrawCircle(dst3, pt, 255)
        Next
    End Sub
End Class






Public Class NR_BrickPoint_Busiest : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim bPoint As New BrickPoint_Basics
    Public bestBricks As New List(Of cv.Point)
    Public sortedBricks As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        desc = "Identify the bricks with the best edge counts - indicating the quality of the brick."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        bPoint.Run(task.gray)

        dst2 = src.Clone
        dst3.SetTo(0)
        bestBricks.Clear()
        sortedBricks.Clear()
        For Each pt In bPoint.ptList
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim brick = bricks.brickList(index)
            If brick.correlation > 0.9 And brick.depth < task.MaxZmeters Then sortedBricks.Add(bPoint.sobel.dst2(brick.rect).CountNonZero, brick.rect)
        Next

        dst3 = bPoint.sobel.dst2
        For i = 0 To sortedBricks.Count - 1
            Dim ele = sortedBricks.ElementAt(i)
            dst2.Rectangle(ele.Value, task.highlight, task.lineWidth)
            dst3.Rectangle(ele.Value, 255, task.lineWidth)
        Next
        labels(2) = CStr(sortedBricks.Count) + " bricks had max Sobel values with high left/right correlation and depth < " + CStr(CInt(task.MaxZmeters)) + "m"
    End Sub
End Class








Public Class NR_BrickPoint_PopulationSurvey : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim bPoint As New BrickPoint_Basics
    Public results(,) As Single
    Public Sub New()
        labels(2) = "Cursor over each grid square to see where the grid squares are."
        task.mouseMovePoint = New cv.Point(0, 0) ' this grid square is often the most populated.
        desc = "Monitor the location of each grid square point in a grid square."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        bPoint.Run(task.gray)
        dst1 = bPoint.dst2
        dst3 = src

        ReDim results(task.brickEdgeLen - 1, task.brickEdgeLen - 1)
        For Each pt In bPoint.ptList
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim brick = bricks.brickList(index)
            results(brick.mm.maxLoc.X, brick.mm.maxLoc.Y) += 1
        Next

        Dim incrX = dst1.Width / task.brickEdgeLen
        Dim incrY = dst1.Height / task.brickEdgeLen
        Dim row = Math.Floor(task.mouseMovePoint.Y / incrY)
        Dim col = Math.Floor(task.mouseMovePoint.X / incrX)

        dst2 = cv.Mat.FromPixelData(task.brickEdgeLen, task.brickEdgeLen, cv.MatType.CV_32F, results)

        For Each brick In bricks.brickList
            If brick.mm.maxLoc.X = col And brick.mm.maxLoc.Y = row Then
                Dim ptfeat = New cv.Point(brick.mm.maxLoc.X + brick.rect.X, brick.mm.maxLoc.Y + brick.rect.Y)
                DrawCircle(dst3, ptfeat)
            End If
        Next

        For y = 0 To task.brickEdgeLen - 1
            For x = 0 To task.brickEdgeLen - 1
                SetTrueText(CStr(results(x, y)), New cv.Point(x * incrX, y * incrY), 2)
            Next
        Next

        dst2 = dst2.Resize(dst0.Size, 0, 0, cv.InterpolationFlags.Nearest).ConvertScaleAbs
        Dim mm = GetMinMax(dst2)
        dst2 *= 255 / mm.maxVal
        labels(3) = "There were " + CStr(results(col, row)) + " features at row/col " + CStr(row) + "/" + CStr(col)
    End Sub
End Class










Public Class NR_BrickPoint_ContourCompare : Inherits TaskParent
    Dim fLess As New NR_BrickPoint_FeatureLess
    Dim contours As New Contour_Basics
    Public Sub New()
        desc = "Compare Contour_Basics to NR_BrickPoint_FeatureLess"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)

        contours.Run(src)
        dst2 = ShowAddweighted(contours.dst2, fLess.dst3, labels(2)).Clone
        dst3 = ShowAddweighted(src, fLess.dst3, labels(2))
    End Sub
End Class








Public Class NR_BrickPoint_FeatureLess : Inherits TaskParent
    Public classCount As Integer
    Public contours As New Contour_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)  ' mask for the featureless regions.
        desc = "Identify each grid square as part of a contour or not."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        dst1 = contours.dst3
        dst2 = contours.dst2
        dst3 = ShowAddweighted(dst2, src, labels(3))
        classCount = contours.contourList.Count
        labels(2) = contours.labels(2)
        labels(3) = "Of the " + CStr(contours.contourList.Count) + " contours " + CStr(classCount) +
                            " have complete bricks inside them."
    End Sub
End Class





Public Class NR_BrickPoint_KNN : Inherits TaskParent
    Public bPoint As New BrickPoint_Basics
    Dim knn As New KNN_Basics
    Public lplist As New List(Of lpData)
    Public Sub New()
        desc = "Join the 2 nearest points to each grid square point to help find lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoint.Run(task.gray)
        dst3 = bPoint.dst3
        If bPoint.ptList.Count < 3 Then Exit Sub

        knn.ptListTrain = New List(Of cv.Point)(bPoint.ptList)
        knn.ptListQuery = New List(Of cv.Point)(bPoint.ptList)
        knn.Run(emptyMat)

        lplist.Clear()
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.trainInput(i)
            Dim p2 = knn.trainInput(knn.neighbors(i)(1))
            dst3.Line(p1, p2, 255, task.lineWidth, task.lineWidth)
            lplist.Add(New lpData(p1, p2))
        Next

        dst2 = src.Clone
        For Each lp In task.lines.lpList
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineWidth)
        Next
    End Sub
End Class




Public Class NR_BrickPoint_EndPoints : Inherits TaskParent
    Dim brickKNN As New NR_BrickPoint_KNN
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Use the lp end points to find lines in the grid square points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        brickKNN.Run(src)
        dst2 = src.Clone
        Dim lplist = brickKNN.lplist

        dst1.SetTo(0)
        Dim lineList As New List(Of Single)
        For Each lp In lplist
            Dim p1 = lpData.validatePoint(New cv.Point(CInt(lp.pE1.Y), CInt(lp.pE1.X)))
            Dim p2 = lpData.validatePoint(New cv.Point(CInt(lp.pE2.Y), CInt(lp.pE2.X)))
            Dim index1 = dst1.Get(Of Single)(p1.Y, p1.X)
            Dim index2 = dst1.Get(Of Single)(p2.Y, p2.X)
            If index1 = 0 And index2 = 0 Then
                dst1.Set(Of Single)(p1.Y, p1.X, lp.index + 1)
                dst1.Set(Of Single)(p2.Y, p2.X, lp.index + 1)
            Else
                If index1 = index2 Then
                    If lineList.Contains(lp.index) = False Then
                        lineList.Add(lp.index)
                        If lineList.Contains(index1 - 1) = False Then lineList.Add(index1 - 1)
                    End If
                End If
            End If
        Next

        For Each index In lineList
            Dim lp = lplist(index)
            dst2.Line(lp.pE1, lp.pE2, task.highlight, task.lineWidth, task.lineWidth)
        Next
    End Sub
End Class





Public Class BrickPoint_MaxSobel : Inherits TaskParent
    Public sobel As New Edge_Sobel
    Public features As New List(Of cv.Point)
    Public Sub New()
        labels(3) = "Sobel input to BrickPoint_Basics"
        desc = "Find the max Sobel point in each brick"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src
        sobel.Run(task.gray)
        dst3 = sobel.dst2

        features.Clear()
        For Each rect In task.gridRects
            Dim mm = GetMinMax(sobel.dst2(rect))
            If mm.maxVal >= sobel.options.sobelThreshold Then
                Dim pt = New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y)
                features.Add(pt)
                DrawCircle(dst2, pt)
            End If
        Next

        labels(2) = "Of the " + CStr(task.gridRects.Count) + " candidates, " + CStr(features.Count) +
                            " had brickpoint intensity >= " + CStr(sobel.options.sobelThreshold)
    End Sub
End Class





Public Class NR_BrickPoint_Vertical : Inherits TaskParent
    Dim vertical As New Edge_SobelVertical
    Public bpCore As New BrickPoint_Core
    Public ptList As New List(Of cv.Point)
    Public Sub New()
        desc = "Use the vertical Sobel to build grid square points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        vertical.Run(src)
        bpCore.Run(vertical.dst2)
        dst2 = bpCore.dst2
        ptList = New List(Of cv.Point)(bpCore.ptList)
        labels(2) = bpCore.labels(2)
    End Sub
End Class




Public Class NR_BrickPoint_Horizontal : Inherits TaskParent
    Dim horizontal As New Edge_SobelHorizontal
    Public bpCore As New BrickPoint_Core
    Public ptList As New List(Of cv.Point)
    Public Sub New()
        desc = "Use the horizontal Sobel to build grid square points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        horizontal.Run(src)
        bpCore.Run(horizontal.dst2)
        dst2 = bpCore.dst2
        ptList = New List(Of cv.Point)(bpCore.ptList)
        labels(2) = bpCore.labels(2)
    End Sub
End Class





Public Class NR_BrickPoint_Blocks : Inherits TaskParent
    Public threshold As Single
    Public Sub New()
        desc = "Use the bricks to portray the brickpoints"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static sobel As New Edge_Sobel
            sobel.Run(src)
            src = sobel.dst2
            Static thresholdSlider = OptionParent.FindSlider("Sobel Intensity Threshold")
            threshold = thresholdSlider.value
        End If

        dst2 = task.color.Clone
        For Each rect In task.gridRects
            Dim mm = GetMinMax(src(rect))
            Dim pt = New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y)
            If mm.maxVal >= threshold Then DrawRect(dst2, rect)
        Next
    End Sub
End Class






Public Class NR_BrickPoint_Features : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim feat As New Feature_Bricks
    Public featureBricks As New List(Of cv.Rect)
    Public Sub New()
        task.gOptions.LineWidth.Value = 3
        labels(3) = "Featureless areas"
        desc = "Identify the cells with features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2

        bricks.Run(src)

        featureBricks.Clear()
        Dim featList As New List(Of cv.Point)(feat.features)
        For Each pt In featList
            Dim index As Integer = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            featureBricks.Add(task.gridRects(index))
        Next

        If task.gOptions.DebugCheckBox.Checked Then
            For Each pt In featList
                DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Black)
            Next
        End If

        If standaloneTest() Then
            dst3.SetTo(0)
            For Each r In featureBricks
                dst3.Rectangle(r, white, -1)
            Next
            dst3 = Not dst3
        End If

        If task.heartBeat Then
            Dim flessCount = task.gridRects.Count - featureBricks.Count
            labels(2) = CStr(featureBricks.Count) + " cells had features while " + CStr(flessCount) + " had none"
        End If
    End Sub
End Class
