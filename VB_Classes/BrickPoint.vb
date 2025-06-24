Imports cv = OpenCvSharp
Public Class BrickPoint_Basics : Inherits TaskParent
    Public sobel As New Edge_SobelQT
    Public intensityFeatures As New List(Of cv.Point2f)
    Public Sub New()
        task.brickRunFlag = True
        labels(3) = "Sobel input to BrickPoint_Basics"
        desc = "Find the max Sobel point in each brick"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        sobel.Run(task.grayStable.Clone)
        dst3 = sobel.dst2

        intensityFeatures.Clear()
        For Each brick In task.bricks.brickList
            Dim mm = GetMinMax(dst3(brick.rect))
            brick.pt = New cv.Point(mm.maxLoc.X + brick.rect.X, mm.maxLoc.Y + brick.rect.Y)
            brick.feature = New cv.Point(mm.maxLoc.X + brick.rect.X, mm.maxLoc.Y + brick.rect.Y)
            brick.intensity = mm.maxVal
            If brick.intensity = 255 Then intensityFeatures.Add(brick.feature)
        Next

        For Each pt In intensityFeatures
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
        Next

        labels(2) = "Of the " + CStr(task.gridRects.Count) + " candidates, " + CStr(intensityFeatures.Count) +
                    " had the maximum intensity (255). "
    End Sub
End Class








Public Class BrickPoint_Plot : Inherits TaskParent
    Dim plotHist As New Plot_Histogram
    Dim ptBrick As New BrickPoint_Basics
    Public Sub New()
        task.gOptions.setHistogramBins(3)
        plotHist.maxRange = 255
        plotHist.minRange = 0
        plotHist.removeZeroEntry = False
        plotHist.createHistogram = True
        desc = "Plot the distribution of Sobel values for each ptBrick cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(task.grayStable)

        Dim sobelValues As New List(Of Byte)
        For Each brick In task.bricks.brickList
            sobelValues.Add(brick.intensity)
        Next
        plotHist.Run(cv.Mat.FromPixelData(sobelValues.Count, 1, cv.MatType.CV_8U, sobelValues.ToArray))
        dst2 = plotHist.dst2

        Dim incr = (plotHist.maxRange - plotHist.minRange) / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / (dst2.Width / task.histogramBins))
        Dim minVal = CInt(histIndex * incr)
        Dim maxVal = CInt((histIndex + 1) * incr)
        labels(3) = "Sobel peak values from " + CStr(minVal) + " to " + CStr(maxVal)

        dst3 = src
        For Each brick In task.bricks.brickList
            If brick.intensity <= maxVal And brick.intensity >= minVal Then dst3.Circle(brick.feature, task.DotSize, task.highlight, -1)
        Next
        labels(2) = "There were " + CStr(sobelValues.Count) + " points found.  Cursor over each bar to see where they originated from"
    End Sub
End Class






Public Class BrickPoint_MaskRedColor : Inherits TaskParent
    Dim fLess As New BrickPoint_FeatureLess
    Public Sub New()
        desc = "Run RedColor with the featureless mask from BrickPoint_FeatureLess"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        dst2 = runRedC(src, labels(2), fLess.dst2)
    End Sub
End Class







Public Class BrickPoint_TopRow : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Public Sub New()
        labels(3) = "BrickPoint_Basics output of intensity = 255 - not necessarily in the top row of the brick."
        desc = "BackProject the top row of the survey results into the RGB image - might help identify vertical lines (see dst3)."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(src)
        dst3 = src.Clone
        dst2 = src.Clone

        Dim count As Integer
        For Each brick In task.bricks.brickList
            If brick.feature = newPoint Then Continue For
            If brick.intensity <> 255 Then Continue For
            If brick.feature.Y = brick.rect.Y Then
                dst2.Circle(brick.feature, task.DotSize, task.highlight, -1, task.lineType)
                dst3.Circle(brick.rect.TopLeft, task.DotSize, task.highlight, -1, task.lineType)
                count += 1
            End If
        Next

        labels(2) = "Of the " + CStr(ptBrick.intensityFeatures.Count) + " max intensity bricks " + CStr(count) + " had max intensity in the top row of the brick."
    End Sub
End Class






Public Class BrickPoint_DistanceAbove : Inherits TaskParent
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        task.brickRunFlag = True
        plotHist.createHistogram = True
        plotHist.removeZeroEntry = False
        desc = "Show grid points based on their distance to the grid point above."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lpList As New List(Of lpData)

        Dim lpZero As New lpData(New cv.Point, New cv.Point)
        For Each brick In task.bricks.brickList
            If brick.rect.Y = 0 Then
                lpList.Add(lpZero)
            Else
                Dim gc1 = task.bricks.brickList(brick.index - task.cellsPerRow)
                Dim lp = New lpData(brick.pt, gc1.pt)
                lpList.Add(lp)
            End If
        Next

        Dim lengths As New List(Of Single)
        For Each lp In lpList
            lengths.Add(lp.length)
        Next

        Dim minLen = lengths.Min, maxLen = lengths.Max
        If maxLen = task.cellSize And minLen = task.cellSize Then Exit Sub

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
        For Each brick In task.bricks.brickList
            Dim lp = lpList(brick.index)
            If lp.length < min Or lp.length > max Then Continue For
            dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
    End Sub
End Class







Public Class BrickPoint_KNN : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Best
    Dim knn As New KNN_Basics
    Dim lines As New LineRGB_Basics
    Public Sub New()
        desc = "Join the 2 nearest points to each grid point to help find lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(task.grayStable)
        dst3 = ptBrick.dst3

        knn.trainInput.Clear()
        For Each pt In ptBrick.bestBricks
            knn.trainInput.Add(New cv.Point2f(pt.X, pt.Y))
        Next
        knn.queries = New List(Of cv.Point2f)(knn.trainInput)
        knn.Run(emptyMat)

        For i = 0 To knn.neighbors.Count - 1
            dst3.Line(knn.trainInput(i), knn.trainInput(knn.neighbors(i)(1)), 255, task.lineWidth, task.lineType)
            dst3.Line(knn.trainInput(i), knn.trainInput(knn.neighbors(i)(2)), 255, task.lineWidth, task.lineType)
        Next

        lines.Run(dst3)
        dst2 = src.Clone
        For Each lp In lines.lpList
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
    End Sub
End Class








Public Class BrickPoint_Best : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Public bestBricks As New List(Of cv.Point)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Display the grid points that have the highest possible max val - indicating the quality of the point."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(task.grayStable)
        labels(2) = ptBrick.labels(2)

        dst2 = src.Clone
        dst3.SetTo(0)
        bestBricks.Clear()
        For Each pt In ptBrick.intensityFeatures
            bestBricks.Add(pt)
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
            dst3.Circle(pt, task.DotSize, 255, -1)
        Next
    End Sub
End Class






Public Class BrickPoint_Busiest : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Public bestBricks As New List(Of cv.Point)
    Public sortedBricks As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        desc = "Identify the bricks with the best edge counts - indicating the quality of the brick."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(task.grayStable)

        dst2 = src.Clone
        dst3.SetTo(0)
        bestBricks.Clear()
        sortedBricks.Clear()
        For Each pt In ptBrick.intensityFeatures
            Dim index = task.grid.gridMap.Get(Of Single)(pt.Y, pt.X)
            Dim brick = task.bricks.brickList(index)
            If brick.correlation > 0.9 And brick.depth < task.MaxZmeters Then sortedBricks.Add(ptBrick.sobel.dst2(brick.rect).CountNonZero, brick.rect)
        Next

        dst3 = ptBrick.sobel.dst2
        For i = 0 To sortedBricks.Count - 1
            Dim ele = sortedBricks.ElementAt(i)
            dst2.Rectangle(ele.Value, task.highlight, task.lineWidth)
            dst3.Rectangle(ele.Value, 255, task.lineWidth)
        Next
        labels(2) = CStr(sortedBricks.Count) + " bricks had max Sobel values with high left/right correlation and depth < " + CStr(CInt(task.MaxZmeters)) + "m"
    End Sub
End Class








Public Class BrickPoint_PopulationSurvey : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Public results(,) As Single
    Public Sub New()
        labels(2) = "Cursor over each brick to see where the grid points are."
        task.mouseMovePoint = New cv.Point(0, 0) ' this brick is often the most populated.
        desc = "Monitor the location of each brick point in a brick."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(task.grayStable)

        dst1 = ptBrick.dst2
        dst3 = src

        ReDim results(task.cellSize - 1, task.cellSize - 1)
        For Each pt In ptBrick.intensityFeatures
            Dim index = task.grid.gridMap.Get(Of Single)(pt.Y, pt.X)
            Dim brick = task.bricks.brickList(index)
            results(brick.feature.X - brick.rect.X, brick.feature.Y - brick.rect.Y) += 1
        Next

        Dim incrX = dst1.Width / task.cellSize
        Dim incrY = dst1.Height / task.cellSize
        Dim row = Math.Floor(task.mouseMovePoint.Y / incrY)
        Dim col = Math.Floor(task.mouseMovePoint.X / incrX)

        dst2 = cv.Mat.FromPixelData(task.cellSize, task.cellSize, cv.MatType.CV_32F, results)

        For Each brick In task.bricks.brickList
            If brick.feature.X = col And brick.feature.Y = row Then dst3.Circle(brick.pt, task.DotSize, task.highlight, -1)
        Next

        For y = 0 To task.cellSize - 1
            For x = 0 To task.cellSize - 1
                SetTrueText(CStr(results(x, y)), New cv.Point(x * incrX, y * incrY), 2)
            Next
        Next

        dst2 = dst2.Resize(dst0.Size, 0, 0, cv.InterpolationFlags.Nearest).ConvertScaleAbs
        Dim mm = GetMinMax(dst2)
        dst2 *= 255 / mm.maxVal
        labels(3) = "There were " + CStr(results(col, row)) + " features at row/col " + CStr(row) + "/" + CStr(col)
    End Sub
End Class







Public Class BrickPoint_RedCloud : Inherits TaskParent
    Public Sub New()
        desc = "Run RedCloud to find the cells not already identified as contours."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.contours.dst2
        labels(2) = task.contours.labels(2)

        task.contours.contourMap.ConvertTo(dst1, cv.MatType.CV_8U)
        dst3 = runRedC(src, labels(3), dst1)
    End Sub
End Class









Public Class BrickPoint_ContourCompare : Inherits TaskParent
    Dim gpLess As New BrickPoint_FeatureLess
    Public Sub New()
        desc = "Compare Contour_Basics to BrickPoint_FeatureLess"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        gpLess.Run(src)

        dst2 = ShowAddweighted(task.contours.dst2, gpLess.dst3, labels(2)).Clone
        dst3 = ShowAddweighted(src, gpLess.dst3, labels(2))
    End Sub
End Class








Public Class BrickPoint_FeatureLess : Inherits TaskParent
    Public classCount As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)  ' mask for the featureless regions.
        desc = "Identify each brick as part of a contour or not."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2.SetTo(0)

        dst3 = ShowAddweighted(dst2, task.contours.dst2, labels(3))
        classCount = task.contours.contourList.Count
        labels(2) = task.contours.labels(2)
        labels(3) = "Of the " + CStr(task.contours.contourList.Count) + " contours " + CStr(classCount) +
                    " have complete bricks inside them."
    End Sub
End Class







Public Class BrickPoint_FLessRegions : Inherits TaskParent
    Public hist As New Hist_BrickRegions
    Public Sub New()
        desc = "Build a mask for the featureless regions fleshed out by Hist_BrickRegions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hist.Run(src)
        dst2 = ShowPalette(hist.dst1)
        dst3 = hist.dst3
        labels = hist.labels
    End Sub
End Class







Public Class BrickPoint_Contours : Inherits TaskParent
    Dim fLess As New BrickPoint_FLessRegions
    Dim contours As New Contour_Regions
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Create contours for the featureless regions "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayStable)

        contours.Run(fLess.hist.dst1)

        dst3.SetTo(0)
        For Each tour In contours.contourList.ToArray
            DrawContour(dst3, tour.ToList, 255, -1)
            DrawContour(dst3, tour.ToList, 0, task.lineWidth)
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, dst3)

        labels(2) = contours.labels(3)
    End Sub
End Class
