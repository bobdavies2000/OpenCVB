﻿Imports cv = OpenCvSharp
Public Class GridPoint_Basics : Inherits TaskParent
    Dim sobel As New Edge_SobelQT
    Public features As New List(Of cv.Point2f)
    Public featurePoints As New List(Of cv.Point)
    Public sortedPoints As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 255)
        desc = "Find the max Sobel point in each grid cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src

        sobel.Run(task.grayStable)
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
        featurePoints.Clear()
        For Each ele In sortedPoints
            Dim pt = ele.Value
            If dst1.Get(Of Byte)(pt.Y, pt.X) Then featurePoints.Add(pt)
        Next

        dst1.SetTo(0)
        features.Clear()
        For Each pt In featurePoints
            dst1.Circle(pt, task.DotSize, 255, -1, cv.LineTypes.Link8)
            dst2.Circle(pt, task.DotSize, task.highlight, -1)
            features.Add(New cv.Point2f(pt.X, pt.Y))
        Next

        labels(2) = "Of the " + CStr(sortedPoints.Count) + " candidates, " + CStr(features.Count) + " were saved "
    End Sub
End Class






Public Class GridPoint_PeakOver100 : Inherits TaskParent
    Public Sub New()
        labels(3) = "All the points found by GridPoint_Basics"
        desc = "Thresold the Sobel max values from GridPoint_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.feat.gridPoint.dst2

        dst2 = src
        Dim hitCount As Integer, peak = 100
        For Each ele In task.feat.gridPoint.sortedPoints
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
        For Each ele In task.feat.gridPoint.sortedPoints
            If ele.Key <= maxVal And ele.Key >= minVal Then dst3.Circle(ele.Value, task.DotSize, task.highlight, -1)
        Next
        labels(2) = "There were " + CStr(sobelValues.Count) + " points found.  Cursor over each bar to see where they originated from"
    End Sub
End Class






Public Class GridPoint_PopulationSurvey : Inherits TaskParent
    Public Sub New()
        labels(2) = "Cursor over each brick to see where the grid points are."
        desc = "Monitor the location of each grid point in the grid cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = task.feat.gridPoint.dst2
        dst3 = src
        Dim survey(task.cellSize - 1, task.cellSize - 1) As Single
        For Each gc In task.gcList
            survey(gc.feature.X, gc.feature.Y) += 1
        Next

        Dim incrX = dst1.Width / task.cellSize
        Dim incrY = dst1.Height / task.cellSize
        Dim row = Math.Floor(task.mouseMovePoint.Y / incrY)
        Dim col = Math.Floor(task.mouseMovePoint.X / incrX)

        dst2 = cv.Mat.FromPixelData(task.cellSize, task.cellSize, cv.MatType.CV_32F, survey)

        For Each gc In task.gcList
            If gc.feature.X = col And gc.feature.Y = row Then dst3.Circle(gc.pt, task.DotSize, task.highlight, -1)
        Next

        For y = 0 To task.cellSize - 1
            For x = 0 To task.cellSize - 1
                SetTrueText(CStr(survey(x, y)), New cv.Point(x * incrX, y * incrY), 2)
            Next
        Next

        dst2 = dst2.Resize(dst0.Size, 0, 0, cv.InterpolationFlags.Nearest).ConvertScaleAbs
        Dim mm = GetMinMax(dst2)
        dst2 *= 255 / mm.maxVal
        labels(3) = "There were " + CStr(survey(col, row)) + " features at row/col " + CStr(row) + "/" + CStr(col)
    End Sub
End Class






Public Class GridPoint_Lines : Inherits TaskParent
    Public Sub New()
        desc = "Find lines in the grid points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src
        dst3.SetTo(0)
        For Each gc In task.gcList
            If gc.intensity <= 100 Then Continue For
            For Each index In task.gridNeighbors(gc.index)
                Dim gcNabe = task.gcList(index)
                If gcNabe.intensity <= 100 Then Continue For
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

        dst2 = ShowAddweighted(fLess.dst2, gpLess.dst3, labels(2))
        dst3 = ShowAddweighted(src, gpLess.dst3, labels(2))
    End Sub
End Class







Public Class GridPoint_FeatureLess : Inherits TaskParent
    Public Sub New()
        labels(3) = "Mask for the featureless regions"
        dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Isolate the featureless regions using the sobel intensity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3.SetTo(0)
        Dim gcPrev As gcData = task.gcList(0), gcPrevAbove = task.gcList(0)
        For Each gc In task.gcList
            If gc.intensity <= 100 And Math.Abs(gc.depth - gcPrev.depth) < task.depthDiffMeters And
                Math.Abs(gc.depth - gcPrevAbove.depth) < task.depthDiffMeters Then
                dst3(gc.rect).SetTo(255)
            End If
        Next

        If standaloneTest() Then dst2 = ShowAddweighted(src, dst3, labels(2))
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
