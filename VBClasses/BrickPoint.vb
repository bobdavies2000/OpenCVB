Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class BrickPoint_Basics : Inherits TaskParent
        Public sobel As New Edge_Sobel
        Public bpCore As New BrickPoint_Core
        Public ptList As New List(Of cv.Point)
        Public Sub New()
            labels(3) = "Sobel input to BrickPoint_Basics"
            desc = "Find the max Sobel point in each gr"
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
        Public ptList As New List(Of cv.Point)
        Public threshold As Single = 150
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            desc = "Identify the highest intensity point in each gr given the input image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static sobel As New Edge_Sobel
                sobel.Run(src)
                src = sobel.dst2
                threshold = 255 ' thresholdSlider.value
            End If

            dst2 = atask.color.Clone
            ptList.Clear()
            For Each gr In atask.bricks.brickList
                Dim mm = GetMinMax(src(gr.rect))
                Dim pt = New cv.Point(mm.maxLoc.X + gr.rect.X, mm.maxLoc.Y + gr.rect.Y)
                If mm.maxVal >= threshold Then ptList.Add(New cv.Point(mm.maxLoc.X + gr.rect.X, mm.maxLoc.Y + gr.rect.Y))
            Next

            For Each pt In ptList
                DrawCircle(dst2, pt)
            Next

            labels(2) = "Of the " + CStr(atask.gridRects.Count) + " candidates, " + CStr(ptList.Count) +
                    " had brickpoint intensity >= " + CStr(threshold)
        End Sub
    End Class








    Public Class NR_BrickPoint_Plot : Inherits TaskParent
        Dim plotHist As New Plot_Histogram
        Dim bPoint As New BrickPoint_Basics
        Public Sub New()
            atask.gOptions.setHistogramBins(3)
            plotHist.maxRange = 255
            plotHist.minRange = 0
            plotHist.removeZeroEntry = False
            plotHist.createHistogram = True
            desc = "Plot the distribution of Sobel values for each ptBrick cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(atask.gray)

            Dim sobelValues As New List(Of Byte)
            For Each gr In atask.bricks.brickList
                sobelValues.Add(gr.mm.maxVal)
            Next
            plotHist.Run(cv.Mat.FromPixelData(sobelValues.Count, 1, cv.MatType.CV_8U, sobelValues.ToArray))
            dst2 = plotHist.dst2

            Dim incr = (plotHist.maxRange - plotHist.minRange) / atask.histogramBins
            Dim histIndex = Math.Floor(atask.mouseMovePoint.X / (dst2.Width / atask.histogramBins))
            Dim minVal = CInt(histIndex * incr)
            Dim maxVal = CInt((histIndex + 1) * incr)
            labels(3) = "Sobel peak values from " + CStr(minVal) + " to " + CStr(maxVal)

            dst3 = src
            For Each gr In atask.bricks.brickList
                If gr.mm.maxVal <= maxVal And gr.mm.maxVal >= minVal Then
                    DrawCircle(dst3, New cv.Point(gr.mm.maxLoc.X + gr.rect.X, gr.mm.maxLoc.Y + gr.rect.Y))
                End If
            Next
            labels(2) = "There were " + CStr(sobelValues.Count) + " points found.  Cursor over each bar to see where they originated from"
        End Sub
    End Class






    Public Class NR_BrickPoint_MaskRedColor : Inherits TaskParent
        Dim fLess As New BrickPoint_FeatureLess
        Public Sub New()
            desc = "Run RedColor with the featureless mask from BrickPoint_FeatureLess"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(atask.grayStable)
            dst2 = runRedList(src, labels(2), fLess.dst1)
        End Sub
    End Class







    Public Class NR_BrickPoint_TopRow : Inherits TaskParent
        Dim bPoint As New BrickPoint_Basics
        Public Sub New()
            labels(3) = "BrickPoint_Basics output of intensity = 255 - not necessarily in the top row of the gr."
            desc = "BackProject the top row of the survey results into the RGB image - might help identify vertical lines (see dst3)."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(src)
            dst3 = src.Clone
            dst2 = src.Clone

            Dim count As Integer
            For Each gr In atask.bricks.brickList
                If gr.mm.maxLoc = newPoint Then Continue For
                If gr.mm.maxVal <> 255 Then Continue For
                If gr.mm.maxLoc.Y = gr.rect.Y Then
                    DrawCircle(dst2, gr.mm.maxLoc)
                    DrawCircle(dst3, gr.rect.TopLeft)
                    count += 1
                End If
            Next

            labels(2) = "Of the " + CStr(bPoint.ptList.Count) + " max intensity bricks " + CStr(count) +
                    " had max intensity in the top row of the gr."
        End Sub
    End Class






    Public Class NR_BrickPoint_DistanceAbove : Inherits TaskParent
        Dim plotHist As New Plot_Histogram
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            plotHist.createHistogram = True
            plotHist.removeZeroEntry = False
            desc = "Show grid points based on their distance to the grid point above."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lpList As New List(Of lpData)

            Dim lpZero As New lpData(New cv.Point, New cv.Point)
            For Each gr In atask.bricks.brickList
                If gr.rect.Y = 0 Then
                    lpList.Add(lpZero)
                Else
                    Dim gc1 = atask.bricks.brickList(gr.index - atask.bricksPerRow)
                    Dim pt = New cv.Point(gr.mm.maxLoc.X + gr.rect.X, gr.mm.maxLoc.Y + gr.rect.Y)
                    Dim ptGc1 = New cv.Point(gc1.mm.maxLoc.X + gc1.rect.X, gc1.mm.maxLoc.Y + gc1.rect.Y)
                    Dim lp = New lpData(pt, ptGc1)
                    lpList.Add(lp)
                End If
            Next

            Dim lengths As New List(Of Single)
            For Each lp In lpList
                lengths.Add(lp.length)
            Next

            Dim minLen = lengths.Min, maxLen = lengths.Max
            If maxLen = atask.brickSize And minLen = atask.brickSize Then Exit Sub

            plotHist.Run(cv.Mat.FromPixelData(lengths.Count, 1, cv.MatType.CV_32F, lengths.ToArray))
            dst2 = plotHist.dst2

            Dim brickRange = (maxLen - minLen) / atask.histogramBins
            Dim histList = plotHist.histArray.ToList
            Dim histindex = histList.IndexOf(histList.Max)
            histList(histindex) = 0
            Dim histindex1 = histList.IndexOf(histList.Max)
            Dim min = Math.Min(CInt((histindex) * brickRange), CInt((histindex1) * brickRange))
            Dim max = Math.Max(CInt((histindex + 1) * brickRange), CInt((histindex1 + 1) * brickRange))

            dst3 = src
            For Each gr In atask.bricks.brickList
                Dim lp = lpList(gr.index)
                If lp.length < min Or lp.length > max Then Continue For
                dst3.Line(lp.p1, lp.p2, atask.highlight, atask.lineWidth, atask.lineWidth)
            Next
        End Sub
    End Class







    Public Class BrickPoint_Best : Inherits TaskParent
        Dim bPoint As New BrickPoint_Basics
        Public bestBricks As New List(Of cv.Point)
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Display the grid points that have the highest possible max val - indicating the quality of the point."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(atask.gray)
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
        Dim bPoint As New BrickPoint_Basics
        Public bestBricks As New List(Of cv.Point)
        Public sortedBricks As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
        Public Sub New()
            desc = "Identify the bricks with the best edge counts - indicating the quality of the gr."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(atask.gray)

            dst2 = src.Clone
            dst3.SetTo(0)
            bestBricks.Clear()
            sortedBricks.Clear()
            For Each pt In bPoint.ptList
                Dim index = atask.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim gr = atask.bricks.brickList(index)
                If gr.correlation > 0.9 And gr.depth < atask.MaxZmeters Then sortedBricks.Add(bPoint.sobel.dst2(gr.rect).CountNonZero, gr.rect)
            Next

            dst3 = bPoint.sobel.dst2
            For i = 0 To sortedBricks.Count - 1
                Dim ele = sortedBricks.ElementAt(i)
                dst2.Rectangle(ele.Value, atask.highlight, atask.lineWidth)
                dst3.Rectangle(ele.Value, 255, atask.lineWidth)
            Next
            labels(2) = CStr(sortedBricks.Count) + " bricks had max Sobel values with high left/right correlation and depth < " + CStr(CInt(atask.MaxZmeters)) + "m"
        End Sub
    End Class








    Public Class BrickPoint_PopulationSurvey : Inherits TaskParent
        Dim bPoint As New BrickPoint_Basics
        Public results(,) As Single
        Public Sub New()
            labels(2) = "Cursor over each gr to see where the grid points are."
            atask.mouseMovePoint = New cv.Point(0, 0) ' this gr is often the most populated.
            desc = "Monitor the location of each gr point in a gr."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(atask.gray)

            dst1 = bPoint.dst2
            dst3 = src

            ReDim results(atask.brickSize - 1, atask.brickSize - 1)
            For Each pt In bPoint.ptList
                Dim index = atask.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim gr = atask.bricks.brickList(index)
                results(gr.mm.maxLoc.X, gr.mm.maxLoc.Y) += 1
            Next

            Dim incrX = dst1.Width / atask.brickSize
            Dim incrY = dst1.Height / atask.brickSize
            Dim row = Math.Floor(atask.mouseMovePoint.Y / incrY)
            Dim col = Math.Floor(atask.mouseMovePoint.X / incrX)

            dst2 = cv.Mat.FromPixelData(atask.brickSize, atask.brickSize, cv.MatType.CV_32F, results)

            For Each gr In atask.bricks.brickList
                If gr.mm.maxLoc.X = col And gr.mm.maxLoc.Y = row Then
                    Dim ptfeat = New cv.Point(gr.mm.maxLoc.X + gr.rect.X, gr.mm.maxLoc.Y + gr.rect.Y)
                    DrawCircle(dst3, ptfeat)
                End If
            Next

            For y = 0 To atask.brickSize - 1
                For x = 0 To atask.brickSize - 1
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
        Dim fLess As New BrickPoint_FeatureLess
        Dim contours As New Contour_Basics
        Public Sub New()
            desc = "Compare Contour_Basics to BrickPoint_FeatureLess"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(src)

            contours.Run(src)
            dst2 = ShowAddweighted(contours.dst2, fLess.dst3, labels(2)).Clone
            dst3 = ShowAddweighted(src, fLess.dst3, labels(2))
        End Sub
    End Class








    Public Class BrickPoint_FeatureLess : Inherits TaskParent
        Public classCount As Integer
        Public contours As New Contour_Basics
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)  ' mask for the featureless regions.
            desc = "Identify each gr as part of a contour or not."
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





    Public Class BrickPoint_KNN : Inherits TaskParent
        Public bPoint As New BrickPoint_Basics
        Dim knn As New KNN_Basics
        Public lplist As New List(Of lpData)
        Public Sub New()
            desc = "Join the 2 nearest points to each gr point to help find lines."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(atask.gray)
            dst3 = bPoint.dst3
            If bPoint.ptList.Count < 3 Then Exit Sub

            knn.ptListTrain = New List(Of cv.Point)(bPoint.ptList)
            knn.ptListQuery = New List(Of cv.Point)(bPoint.ptList)
            knn.Run(emptyMat)

            lplist.Clear()
            For i = 0 To knn.neighbors.Count - 1
                Dim p1 = knn.trainInput(i)
                Dim p2 = knn.trainInput(knn.neighbors(i)(1))
                dst3.Line(p1, p2, 255, atask.lineWidth, atask.lineWidth)
                lplist.Add(New lpData(p1, p2))
            Next

            dst2 = src.Clone
            For Each lp In atask.lines.lpList
                dst2.Line(lp.p1, lp.p2, atask.highlight, atask.lineWidth, atask.lineWidth)
            Next
        End Sub
    End Class




    Public Class NR_BrickPoint_EndPoints : Inherits TaskParent
        Dim brickKNN As New BrickPoint_KNN
        Public Sub New()
            If standalone Then atask.gOptions.displayDst1.Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
            desc = "Use the lp end points to find lines in the gr points"
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
                dst2.Line(lp.pE1, lp.pE2, atask.highlight, atask.lineWidth, atask.lineWidth)
            Next
        End Sub
    End Class





    Public Class BrickPoint_Minimum : Inherits TaskParent
        Public sobel As New Edge_Sobel
        Public features As New List(Of cv.Point)
        Public Sub New()
            labels(3) = "Sobel input to BrickPoint_Basics"
            desc = "Find the max Sobel point in each gr"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src
            sobel.Run(atask.gray)
            dst3 = sobel.dst2

            features.Clear()
            For Each rect In atask.gridRects
                Dim mm = GetMinMax(sobel.dst2(rect))
                If mm.maxVal >= sobel.options.sobelThreshold Then
                    Dim pt = New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y)
                    features.Add(pt)
                    DrawCircle(dst2, pt)
                End If
            Next

            labels(2) = "Of the " + CStr(atask.gridRects.Count) + " candidates, " + CStr(features.Count) +
                    " had brickpoint intensity >= " + CStr(sobel.options.sobelThreshold)
        End Sub
    End Class





    Public Class BrickPoint_Vertical : Inherits TaskParent
        Dim vertical As New Edge_SobelVertical
        Public bpCore As New BrickPoint_Core
        Public ptList As New List(Of cv.Point)
        Public Sub New()
            desc = "Use the vertical Sobel to build gr points"
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
            desc = "Use the horizontal Sobel to build gr points"
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

            dst2 = atask.color.Clone
            For Each rect In atask.gridRects
                Dim mm = GetMinMax(src(rect))
                Dim pt = New cv.Point(mm.maxLoc.X + rect.X, mm.maxLoc.Y + rect.Y)
                If mm.maxVal >= threshold Then DrawRect(dst2, rect)
            Next
        End Sub
    End Class
End Namespace