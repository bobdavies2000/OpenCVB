Imports NAudio.Utils
Imports cv = OpenCvSharp
Public Class GridPoint_Basics : Inherits TaskParent
    Dim sobel As New Edge_SobelQT
    Public sortedPoints As New SortedList(Of Integer, cv.Point2f)(New compareAllowIdenticalIntegerInverted)
    Public options As New Options_GridPoint
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 255)
        desc = "Find the max Sobel point in each grid cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.algorithmPrep = False Then Exit Sub ' we have already been run as part of the algorithm setup.

        options.Run()

        dst2 = src

        sobel.Run(task.grayStable.Clone)
        dst3 = sobel.dst2

        sortedPoints.Clear()
        For Each gc In task.gcList
            Dim mm = GetMinMax(dst3(gc.rect))
            gc.pt = New cv.Point(mm.maxLoc.X + gc.rect.X, mm.maxLoc.Y + gc.rect.Y)
            gc.feature = mm.maxLoc
            Dim val = dst3.Get(Of Byte)(gc.pt.Y, gc.pt.X)
            gc.intensity = val
            sortedPoints.Add(val, gc.pt)
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
        labels(3) = If(task.toggleOn, "Toggle On", "Toggle Off")
    End Sub
End Class






Public Class GridPoint_PeakThreshold : Inherits TaskParent
    Public Sub New()
        labels(3) = "All the points found by GridPoint_Basics"
        desc = "Thresold the Sobel max values from GridPoint_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static peakSlider = optiBase.FindSlider("Sobel Threshold")
        Dim peak = peakSlider.value
        dst3 = task.gridPoint.dst2

        dst2 = src
        Dim hitCount As Integer
        For Each ele In task.gridPoint.sortedPoints
            If ele.Key >= peak Then
                dst2.Circle(ele.Value, task.DotSize, task.highlight, -1)
                hitCount += 1
            End If
        Next
        labels(2) = traceName + " found " + CStr(hitCount) + " points with peak  value greater than " + CStr(peak)
    End Sub
End Class







Public Class GridPoint_Plot : Inherits TaskParent
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        task.gOptions.HistBinBar.Value = 3
        plotHist.maxRange = 255
        plotHist.minRange = 0
        plotHist.removeZeroEntry = False
        plotHist.createHistogram = True
        desc = "Plot the distribution of Sobel values for each gridPoint cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim sobelValues As New List(Of Byte)
        For Each gc In task.gcList
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
        For Each ele In task.gridPoint.sortedPoints
            If ele.Key <= maxVal And ele.Key >= minVal Then dst3.Circle(ele.Value, task.DotSize, task.highlight, -1)
        Next
        labels(2) = "There were " + CStr(sobelValues.Count) + " points found.  Cursor over each bar to see where they originated from"
    End Sub
End Class







Public Class GridPoint_Lines : Inherits TaskParent
    Public Sub New()
        desc = "Find lines in the grid points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static peakSlider = optiBase.FindSlider("Sobel Threshold")
        Dim peak = peakSlider.value

        dst2 = src
        dst3.SetTo(0)
        For Each gc In task.gcList
            If gc.intensity <= peak Then Continue For
            For Each index In task.gridNeighbors(gc.index)
                Dim gcNabe = task.gcList(index)
                If gcNabe.intensity <= peak Then Continue For
                If gcNabe.feature.X = gc.feature.X And gcNabe.feature.Y = gc.feature.Y Then
                    If gc.pt <> task.gcList(index).pt Then
                        dst2.Line(gc.pt, task.gcList(index).pt, task.highlight, task.lineWidth)
                        dst3.Line(gc.pt, task.gcList(index).pt, task.highlight, task.lineWidth)
                    End If
                End If
            Next
        Next
    End Sub
End Class







Public Class GridPoint_FeatureLessCompare : Inherits TaskParent
    Dim fLess As New FeatureLess_Basics
    Dim gpLess As New GridPoint_FeatureLess
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





Public Class GridPoint_MaskRedColor : Inherits TaskParent
    Dim fLess As New GridPoint_FeatureLess
    Public Sub New()
        task.redC = New RedColor_Basics
        desc = "Run RedColor with the featureless mask from GridPoint_FeatureLess"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(src)

        task.redC.inputRemoved = fLess.dst3
        dst2 = runRedC(src, labels(2))
    End Sub
End Class





Public Class GridPoint_FeatureLess : Inherits TaskParent
    Public edges As New EdgeLine_Basics
    Public classCount As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)  ' mask for the featureless regions.
        desc = "Isolate the featureless regions using the sobel intensity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.grayStable.Clone)

        dst2.SetTo(0)
        For Each gc In task.gcList
            If gc.rect.X = 0 Or gc.rect.Y = 0 Then Continue For
            If edges.dst2(gc.rect).CountNonZero = 0 Then
                gc.fLessIndex = 255
                dst2(gc.rect).SetTo(255)
            End If
        Next

        classCount = 1
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each gc In task.gcList
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






Public Class GridPoint_FLessContours : Inherits TaskParent
    Dim hist As New Hist_GridPointRegions
    Dim contour As New Contour_Basics
    Public Sub New()
        desc = "Build contours for the featureless regions fleshed out by Hist_GridPointRegions"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hist.Run(src)
        dst3 = hist.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)


    End Sub
End Class






Public Class GridPoint_PopulationSurvey : Inherits TaskParent
    Public results(,) As Single
    Public Sub New()
        labels(2) = "Cursor over each brick to see where the grid points are."
        task.mouseMovePoint = New cv.Point(0, 0) ' this brick is often the most populated.
        desc = "Monitor the location of each grid point in the grid cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.gridPoint.dst2
        dst3 = src

        ReDim results(task.cellSize - 1, task.cellSize - 1)

        For Each gc In task.gcList
            results(gc.feature.X, gc.feature.Y) += 1
        Next

        Dim incrX = dst1.Width / task.cellSize
        Dim incrY = dst1.Height / task.cellSize
        Dim row = Math.Floor(task.mouseMovePoint.Y / incrY)
        Dim col = Math.Floor(task.mouseMovePoint.X / incrX)

        dst2 = cv.Mat.FromPixelData(task.cellSize, task.cellSize, cv.MatType.CV_32F, results)

        For Each gc In task.gcList
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





Public Class GridPoint_TopRow : Inherits TaskParent
    Public results(,) As Single
    Public Sub New()
        desc = "BackProject the top row of the survey results into the RGB image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        ReDim results(task.cellSize - 1, task.cellSize - 1)

        For Each gc In task.gcList
            results(gc.feature.X, gc.feature.Y) += 1
        Next

        For Each gc In task.gcList
            If gc.rect.Height <> task.cellSize Or gc.rect.Width <> task.cellSize Then Continue For
            If gc.feature.Y = 0 Then dst2(gc.rect).Circle(gc.feature, task.DotSize, task.highlight, -1, task.lineType)
        Next
    End Sub
End Class






Public Class GridPoint_DistanceAbove : Inherits TaskParent
    Dim plotHist As New Plot_Histogram
    Public Sub New()
        plotHist.createHistogram = True
        plotHist.removeZeroEntry = False
        desc = "Show grid points based on their distance to the grid point above."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lpList As New List(Of lpData)

        Dim lpZero As New lpData(New cv.Point, New cv.Point)
        For Each gc In task.gcList
            If gc.rect.Y = 0 Then
                lpList.Add(lpZero)
            Else
                Dim gc1 = task.gcList(gc.index - task.grid.tilesPerRow)
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
        For Each gc In task.gcList
            Dim lp = lpList(gc.index)
            If lp.length < min Or lp.length > max Then Continue For
            dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
    End Sub
End Class
