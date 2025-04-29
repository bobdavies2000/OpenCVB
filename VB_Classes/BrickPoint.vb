Imports NAudio.Utils
Imports cv = OpenCvSharp
Public Class BrickPoint_Basics : Inherits TaskParent
    Public sobel As New Edge_SobelQT
    Public sortedPoints As New SortedList(Of Integer, cv.Point2f)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 255)
        desc = "Find the max Sobel point in each grid cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        sobel.Run(task.grayStable.Clone)
        dst3 = sobel.dst2

        sortedPoints.Clear()
        For Each gc In task.brickList
            Dim mm = GetMinMax(dst3(gc.rect))
            gc.pt = New cv.Point(mm.maxLoc.X + gc.rect.X, mm.maxLoc.Y + gc.rect.Y)
            gc.feature = mm.maxLoc
            gc.intensity = mm.maxVal
            sortedPoints.Add(mm.maxVal, gc.pt)
        Next

        dst1.SetTo(255, task.motionMask)
        task.gcFeatures.Clear()
        For Each ele In sortedPoints
            Dim pt = ele.Value
            If dst1.Get(Of Byte)(pt.Y, pt.X) Then task.gcFeatures.Add(pt)
        Next

        dst1.SetTo(0)
        For Each pt In task.gcFeatures
            dst1.Circle(pt, task.DotSize, 255, -1, cv.LineTypes.Link8)
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
        Next

        labels(2) = "Of the " + CStr(sortedPoints.Count) + " candidates, " + CStr(task.gcFeatures.Count) +
                    " were retained from the previous image. "
    End Sub
End Class








Public Class BrickPoint_Plot : Inherits TaskParent
    Dim plotHist As New Plot_Histogram
    Dim ptBrick As New BrickPoint_Basics
    Public Sub New()
        task.gOptions.HistBinBar.Value = 3
        plotHist.maxRange = 255
        plotHist.minRange = 0
        plotHist.removeZeroEntry = False
        plotHist.createHistogram = True
        desc = "Plot the distribution of Sobel values for each ptBrick cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(task.grayStable)

        Dim sobelValues As New List(Of Byte)
        For Each gc In task.brickList
            sobelValues.Add(gc.intensity)
        Next
        plotHist.Run(cv.Mat.FromPixelData(sobelValues.Count, 1, cv.MatType.CV_8U, sobelValues.ToArray))
        dst2 = plotHist.dst2

        Dim incr = (plotHist.maxRange - plotHist.minRange) / task.histogramBins
        Dim histIndex = Math.Floor(task.mouseMovePoint.X / (dst2.Width / task.histogramBins))
        Dim minVal = CInt(histIndex * incr)
        Dim maxVal = CInt((histIndex + 1) * incr)
        labels(3) = "Sobel peak values from " + CStr(minVal) + " to " + CStr(maxVal)

        dst3 = src
        For Each ele In ptBrick.sortedPoints
            If ele.Key <= maxVal And ele.Key >= minVal Then dst3.Circle(ele.Value, task.DotSize, task.highlight, -1)
        Next
        labels(2) = "There were " + CStr(sobelValues.Count) + " points found.  Cursor over each bar to see where they originated from"
    End Sub
End Class







Public Class BrickPoint_FeatureLessCompare : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
    Dim gpLess As New BrickPoint_FeatureLess
    Public Sub New()
        desc = "Compare the grid point featureless output to the earlier version - FeatureLess_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)
        gpLess.Run(src)

        dst2 = ShowAddweighted(fLess.dst2, gpLess.dst3, labels(2)).Clone
        dst3 = ShowAddweighted(src, gpLess.dst3, labels(2))
    End Sub
End Class





Public Class BrickPoint_MaskRedColor : Inherits TaskParent
    Dim fLess As New BrickPoint_FeatureLess
    Public Sub New()
        task.redC = New RedColor_Basics
        desc = "Run RedColor with the featureless mask from BrickPoint_FeatureLess"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)

        task.redC.inputRemoved = fLess.dst3
        dst2 = runRedC(src, labels(2))
    End Sub
End Class





Public Class BrickPoint_FeatureLess : Inherits TaskParent
    Public edges As New EdgeLine_Basics
    Public classCount As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)  ' mask for the featureless regions.
        desc = "Isolate the featureless regions using the sobel intensity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.grayStable.Clone)

        dst2.SetTo(0)
        For Each gc In task.brickList
            If gc.rect.X = 0 Or gc.rect.Y = 0 Then Continue For
            If edges.dst2(gc.rect).CountNonZero = 0 Then
                gc.fLessIndex = 255
                dst2(gc.rect).SetTo(255)
            End If
        Next

        classCount = 1
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each gc In task.brickList
            Dim pt = gc.rect.TopLeft
            If dst2.Get(Of Byte)(pt.Y, pt.X) = 0 Then Continue For
            Dim val = dst2.Get(Of Byte)(pt.Y, pt.X)
            If val <> 255 Then
                gc.fLessIndex = val
                Continue For
            End If

            dst2.FloodFill(pt, gc.index Mod 255)
            gc.fLessIndex = gc.index Mod 255
            classCount += 1
        Next

        If standaloneTest() Then dst3 = ShowPalette(dst2)

        labels(2) = "CV_8U Mask for the " + CStr(classCount) + " featureless regions enumerated."
        labels(3) = CStr(classCount) + " featureless regions colored using the gc.index of the first grid cell member."
    End Sub
End Class






Public Class BrickPoint_TopRow : Inherits TaskParent
    Public results(,) As Single
    Public Sub New()
        desc = "BackProject the top row of the survey results into the RGB image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        ReDim results(task.cellSize - 1, task.cellSize - 1)

        For Each gc In task.brickList
            results(gc.feature.X, gc.feature.Y) += 1
        Next

        For Each gc In task.brickList
            If gc.rect.Height <> task.cellSize Or gc.rect.Width <> task.cellSize Then Continue For
            If gc.feature.Y = 0 Then dst2(gc.rect).Circle(gc.feature, task.DotSize, task.highlight, -1, task.lineType)
        Next
    End Sub
End Class






Public Class BrickPoint_DistanceAbove : Inherits TaskParent
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        plotHist.createHistogram = True
        plotHist.removeZeroEntry = False
        desc = "Show grid points based on their distance to the grid point above."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lpList As New List(Of lpData)

        Dim lpZero As New lpData(New cv.Point, New cv.Point)
        For Each gc In task.brickList
            If gc.rect.Y = 0 Then
                lpList.Add(lpZero)
            Else
                Dim gc1 = task.brickList(gc.index - task.grid.tilesPerRow)
                Dim lp = New lpData(gc.pt, gc1.pt)
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
        For Each gc In task.brickList
            Dim lp = lpList(gc.index)
            If lp.length < min Or lp.length > max Then Continue For
            dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
    End Sub
End Class







Public Class BrickPoint_KNN : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Best
    Dim knn As New KNN_Basics
    Dim lines As New Line_Basics
    Public Sub New()
        lines.nonTaskRequest = True
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
        For Each ele In ptBrick.sortedPoints
            If ele.Key = 255 Then
                bestBricks.Add(ele.Value)
                dst2.Circle(ele.Value, task.DotSize, task.highlight, -1)
                dst3.Circle(ele.Value, task.DotSize, 255, -1)
            Else
                Exit For
            End If
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
        For Each ele In ptBrick.sortedPoints
            If ele.Key = 255 Then
                Dim pt = ele.Value
                Dim index = task.brickMap.Get(Of Single)(pt.Y, pt.X)
                Dim gc = task.brickList(index)
                If gc.correlation > 0.9 And gc.depth < task.MaxZmeters Then sortedBricks.Add(ptBrick.sobel.dst2(gc.rect).CountNonZero, gc.rect)
            End If
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
        For Each ele In ptBrick.sortedPoints
            If ele.Key = 255 Then
                Dim pt = ele.Value
                Dim index = task.brickMap.Get(Of Single)(pt.Y, pt.X)
                Dim gc = task.brickList(index)
                results(gc.feature.X, gc.feature.Y) += 1
            End If
        Next

        Dim incrX = dst1.Width / task.cellSize
        Dim incrY = dst1.Height / task.cellSize
        Dim row = Math.Floor(task.mouseMovePoint.Y / incrY)
        Dim col = Math.Floor(task.mouseMovePoint.X / incrX)

        dst2 = cv.Mat.FromPixelData(task.cellSize, task.cellSize, cv.MatType.CV_32F, results)

        For Each gc In task.brickList
            If gc.feature.X = col And gc.feature.Y = row Then dst3.Circle(gc.pt, task.DotSize, task.highlight, -1)
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
    Dim fLess As New BrickPoint_FeatureLess
    Public histTop As New Projection_HistTop
    Public Sub New()
        desc = "Use RedCloud to identify the featureLess regions found in BrickPoint_FeatureLess"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayStable)

        dst1 = fLess.dst2.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        dst2 = runRedC(fLess.dst2, labels(2), dst1)

        dst0.SetTo(0)
        task.pointCloud.CopyTo(dst0, fLess.dst2)
        histTop.Run(fLess.dst2)
    End Sub
End Class







Public Class BrickPoint_FLessRegions : Inherits TaskParent
    Public hist As New Hist_BrickRegions
    Public Sub New()
        desc = "Build a mask for the featureless regions fleshed out by Hist_GridPointRegions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hist.Run(src)
        dst2 = ShowPalette(hist.dst1)
        dst3 = hist.dst3
        labels = hist.labels
    End Sub
End Class





Public Class BrickPoint_Test : Inherits TaskParent
    Dim fLess As New BrickPoint_FLessRegions
    Public histTop As New Projection_HistTop
    Public Sub New()
        desc = "Use RedCloud to identify the featureLess regions found in BrickPoint_FeatureLess"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayStable)
        labels = fLess.labels

        dst1 = fLess.hist.dst1.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        'dst2 = fLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst3 = dst1.Clone
        ' If task.firstPass Then dst2 = runRedC(fLess.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY), labels(2), dst1)

        'dst0.SetTo(0)
        'task.pointCloud.CopyTo(dst0, fLess.dst2)
        'histTop.Run(fLess.dst2)
    End Sub
End Class






Public Class BrickPoint_Contours : Inherits TaskParent
    Dim fLess As New BrickPoint_FLessRegions
    Dim contours As New Contour_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Create contours for the featureless regions "
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.grayStable)

        contours.Run(fLess.hist.dst1)

        dst3.SetTo(0)
        For Each tour In contours.contourlist.ToArray
            DrawContour(dst3, tour.ToList, 255, -1)
            DrawContour(dst3, tour.ToList, 0, task.lineWidth)
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, dst3)

        labels(2) = contours.labels(3)
    End Sub
End Class

