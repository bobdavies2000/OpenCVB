Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedCloud_Basics : Inherits TaskParent
        Public redCore As New RedCloud_Core
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public options As New Options_RedCloud
        Dim reduction As New Reduction_BasicsParmInput
        Public runSelectCell As Boolean = True
        Public Sub New()
            reduction.reductionFactor = 50
            desc = "Build contours for each cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            redCore.Run(src)
            labels(3) = redCore.labels(3)

            rcList.Clear()
            For Each rc In redCore.rcList
                rc.ID = task.delaunayFLess.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                rc.index = rcList.Count
                rcList.Add(rc)
            Next

            Dim tmp = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
            Dim tmpList As New List(Of rcData)
            For Each rc In rcList
                If rc.ID < 0 Then Continue For
                For i = rc.index + 1 To rcList.Count - 1
                    Dim rcNext = rcList(i)
                    If rcNext.index < 0 Then Continue For
                    If rc.ID = rcNext.ID Or rc.rect.Contains(rcNext.rect) Then
                        Dim ID = rc.ID
                        Dim rect = rc.rect.Union(rcNext.rect)
                        tmp(rect).SetTo(0)
                        rc.mask.CopyTo(tmp(rc.rect))
                        rcNext.mask.CopyTo(tmp(rcNext.rect))
                        rc = New rcData(tmp(rect), rect, 255) With {.ID = ID}
                        rcList(i).index = -1
                    End If
                Next
                tmpList.Add(rc)
            Next

            rcMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData)
            For Each rc In tmplist
                If rc.index < 0 Then Continue For
                rc.index = rcList.Count
                rcList.Add(rc)
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
            Next

            dst2 = ApplyPalette(rcMap, 0)

            If standalone Then
                Dim clickID = rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
                If clickID > 0 Then
                    Dim rctmp = RedCC_Basics.rcFinder(rcList, clickID)
                    If rctmp IsNot Nothing Then
                        Rectangle(dst2, rctmp.rect, task.highlight, task.lineWidth)
                        SetTrueText(rctmp.displayCell(), 3)
                    End If
                End If
            End If

            labels(2) = CStr(rcList.Count) + " cells found "
            labels(3) = redCore.labels(3)
        End Sub
    End Class





    Public Class XR_RedCloud_BasicsTest : Inherits TaskParent
        Public redCore As New RedCloud_Core
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public options As New Options_RedCloud
        Dim reduction As New Reduction_BasicsParmInput
        Public runSelectCell As Boolean = True
        Public Sub New()
            reduction.reductionFactor = 50
            desc = "Build contours for each cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If task.quarterBeat Then
                reduction.Run(task.gray)
                dst1 = reduction.dst2
            End If

            redCore.Run(src)
            labels(3) = redCore.labels(3)

            rcList.Clear()
            rcList.Add(New rcData)
            Dim matchCount As Integer
            Dim unMatched As Integer
            Dim matchAverage As Single
            Dim blackVec As New Vec3b
            For Each rc In redCore.rcList
                If rc.age = 1 Then unMatched += 1 Else matchCount += 1
                matchAverage += rc.age
                rc.ID = task.delaunayFLess.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                rc.index = rcList.Count
                If rc.pixels < task.minCellSize Then Continue For
                rcList.Add(rc)
            Next

            Dim depths As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            Dim maxDepth As Single
            For Each rc In rcList
                depths.Add(rc.depthMin, rc.index)
                If maxDepth < rc.depthMax Then maxDepth = rc.depthMax
            Next

            Dim usedTiers As New List(Of Integer)
            rcMap.SetTo(0)
            For Each index In depths.Values
                Dim rc = rcList(index)
                If rc.ID = 0 Then Continue For
                rc.ID = Math.Floor(task.histogramBins * rc.depthMin / Math.Ceiling(maxDepth)) + 1
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                rcList(index) = rc
                If usedTiers.Contains(rc.ID) = False Then usedTiers.Add(rc.ID)
            Next

            dst2 = ApplyPalette(rcMap, 0)

            If rcList.Count >= 255 Then
                MsgBox("RedCloud_Basics has exceeded the 8-bit max.  Increase task.minCellSize?")
            End If
            labels(2) = CStr(unMatched) + " were new cells and " + CStr(matchCount) + " were matched, " +
                                "average age: " + (matchAverage / rcList.Count).ToString(fmt1)
            labels(3) = redCore.labels(3)
        End Sub
    End Class






    Public Class XR_RedCloud_LeftRight : Inherits TaskParent
        Dim bricks As New Brick_Basics
        Dim redC As New RedCloud_Basics
        Public Sub New()
            desc = "Map the RedCloud output into the right view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim count As Integer
            dst1.SetTo(0)
            For Each brick As brickData In bricks.brickList
                If CountNonZero(redC.rcMap(brick.lRect)) And brick.rRect.Width > 0 Then
                    dst2(brick.lRect).CopyTo(dst1(brick.rRect))
                    count += 1
                End If
            Next

            dst3 = ShowAddweighted(dst1, task.rightView, labels(3))
            labels(3) += " " + CStr(count) + " bricks mapped into the right image."
        End Sub
    End Class





    Public Class XR_RedCloud_KNN : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim knn As New KNN_Basics
        Public hulls As New List(Of List(Of cv.Point))
        Public Sub New()
            desc = "Identify corners in contours using KNN with the rect corners."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst3.SetTo(0)
            hulls.Clear()
            Dim listOfPoints = New List(Of List(Of cv.Point))
            For Each rc In redC.rcList
                If rc.contour.Count = 0 Then Continue For
                knn.trainInput.Clear()
                For Each pt In rc.contour
                    knn.trainInput.Add(pt)
                Next
                knn.queries = New List(Of Point2f)({New Point2f(0, 0), New Point2f(rc.rect.Width, 0),
                                      New Point2f(rc.rect.Width, rc.rect.Height), New Point2f(0, rc.rect.Height)})
                knn.Run(Nothing)

                listOfPoints.Clear()
                Dim hullList As New List(Of cv.Point)
                For i = 0 To 3
                    Dim pt = knn.trainInput(knn.result(i, 0))
                    hullList.Add(pt + rc.rect.TopLeft)
                Next
                listOfPoints.Add(hullList)
                FillPoly(dst3, listOfPoints, task.scalarColors(rc.ID Mod 255))
            Next
        End Sub
    End Class




    Public Class XR_RedCloud_Matches : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public rcList As New List(Of rcData)
        Public Sub New()
            task.fOptions.ReductionColor.Value = 120
            desc = "Display the RedCloud cells that matched to the previous frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            labels(2) = redC.labels(2).Replace("new cells", "new cells (white)")

            dst3.SetTo(0)
            dst2.SetTo(0)
            rcList.Clear()
            For Each rc In redC.rcList
                If rc.age >= redC.options.ageThreshold Or rc.age = task.frameCount Then
                    dst2(rc.rect).SetTo(task.scalarColors(rc.ID Mod 255), rc.mask)
                    dst3(rc.rect).SetTo(task.scalarColors(rc.ID Mod 255), rc.mask)
                    rcList.Add(rc)
                Else
                    dst2(rc.rect).SetTo(white, rc.mask)
                End If
            Next

            If task.rcD IsNot Nothing Then Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            SetTrueText(redC.strOut, 3)
            labels(3) = CStr(rcList.Count) + " matched cells below with > " + CStr(redC.options.ageThreshold) + " age"
        End Sub
    End Class





    Public Class XR_RedCloud_MotionFilter : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Dim pcMotion As New Motion_CloudPixel
        Public Sub New()
            dst1 = New Mat(dst1.Size, MatType.CV_8U, 0)
            desc = "Filter changes to the RedCloud cells with motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            pcMotion.Run(emptyMat)

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst1.SetTo(0)
            dst0.CopyTo(dst1, pcMotion.dst2)

            Dim histogram As New Mat
            Dim ranges() As Rangef = New Rangef() {New Rangef(0, redC.rcList.Count + 1)}
            CalcHist({dst1}, {0}, New Mat, histogram, 1, {redC.rcList.Count}, ranges)

            Dim count = CountNonZero(histogram)
            SetTrueText(CStr(count) + " cells had motion.", 3)
            If count = 0 Then Exit Sub
            histogram.Set(Of Single)(0, 0, 0) ' remove the count for cell 0 - no cell information.

            Dim histArray(histogram.Rows - 1) As Single
            histogram.GetArray(Of Single)(histArray)

            Dim rcMotionCells As New List(Of Integer)
            For i = 1 To histArray.Length - 1
                Dim rc = redC.rcList(i - 1)
                If histArray(i) > rc.pixels / 10 Then rcMotionCells.Add(i)
            Next

            dst3.SetTo(0)
            rcMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData)
            For Each rc In redC.rcList
                If rc.age > 1 Then
                    If rcMotionCells.Contains(rc.ID) = False Then
                        dst3(rc.rect).SetTo(task.scalarColors(rc.ID), rc.mask)
                        rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                        rc.index = rcList.Count
                        rcList.Add(rc)
                    End If
                End If
            Next
        End Sub
    End Class





    Public Class XR_RedCloud_Motion : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim addw As New AddWeighted_Basics
        Dim pcMotion As New Motion_CloudPixel
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(3) = "Pixels with changes in depth that are larger than the expected error at that distance."
            desc = "Mix the cloud motion and RedCloud output with AddWeighted."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            pcMotion.Run(emptyMat)
            redC.Run(src)
            dst1 = redC.dst2
            labels(1) = redC.labels(2)

            dst3 = pcMotion.dst2

            addw.src2 = dst1
            addw.Run(dst3)
            dst2 = addw.dst2
        End Sub
    End Class




    Public Class RedCloud_Features : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst1.Size, MatType.CV_8U, 0)
        Dim rcListLast As New List(Of rcData)
        Public fLessBricks As New List(Of cv.Rect)
        Public Sub New()
            desc = "Identify regions by depth."
        End Sub
        Public Shared Function MyFinder(myList As List(Of rcData), id As Integer) As rcData
            If id = 0 Then Return Nothing
            For Each rc In myList
                If rc.ID = id Then Return myList(rc.index)
            Next
            Return Nothing
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            rcListLast = New List(Of rcData)(rcList)

            rcMap.SetTo(0)
            fLessBricks.Clear()
            For Each r In task.gridRects
                If CountNonZero(task.edges.dst2(r)) > 0 Then Continue For
                If r.Height <> task.gridWH Or r.Width <> task.gridWH Then Continue For ' odd sizes.
                rcMap(r).SetTo(255)
                fLessBricks.Add(r)
            Next

            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(rcMap.Width + 2, rcMap.Height + 2), MatType.CV_8U, 0)
            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim rc As rcData
            Dim index As Integer = 1
            Dim flags = FloodFillFlags.FixedRange Or (255 << 8)
            For Each r In fLessBricks
                If rcMap.Get(Of Byte)(r.Y, r.X) = 255 Then
                    Dim count = FloodFill(rcMap, mask, r.TopLeft, index, rect, 0, 0, flags)
                    rc = New rcData(rcMap(rect), rect, index)
                    sortList.Add(rc.pixels, rc)
                    index += 1
                End If
            Next

            rcList.Clear()
            rcList.Add(New rcData) ' placeholder for zero...
            rcMap.SetTo(0)
            For Each rc In sortList.Values
                rc.index = rcList.Count
                Dim pt = WarpAffine_Basics.WarpPoint(rc.maxDist, task.steadyCam.inverseM)
                pt = validatePoint(pt)
                rc.ID = task.delaunayFLess.rcMap.Get(Of Byte)(pt.Y, pt.X)
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                rc.pixels = CountNonZero(rc.mask)
                rcList.Add(rc)
            Next

            For Each rc In rcList
                If rc.index = 0 Then Continue For
                If rc.motionPixels = 0 Then
                    Dim rclast = MyFinder(rcListLast, rc.ID)
                    If rclast IsNot Nothing Then
                        rc.age = rclast.age + 1
                        If rc.age >= 1000 Then rc.age = 10
                        rc.maxDStable = rclast.maxDStable
                    End If
                End If
                ' SetTrueText(CStr(rc.age), rc.maxDStable)
            Next

            dst2 = ApplyPalette(rcMap, 0)

            For Each rc In rcList
                If rc.index = 0 Then Continue For
                SetTrueText(CStr(rc.ID), rc.maxDist)
            Next

            Dim rcNext = MyFinder(rcList, rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X))
            If rcNext IsNot Nothing Then
                task.rcD = rcNext
                Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
                SetTrueText(task.rcD.displayCell, 3)
            Else
                If rcList.Count > 1 Then task.rcD = rcList(1)
            End If

            labels(2) = CStr(rcList.Count - 1) + " featureless regions found.  Age is shown."
        End Sub
    End Class





    Public Class XR_RedCloud_Delaunay : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim ptList As New List(Of cv.Point2f)
        Dim facetList As New List(Of List(Of cv.Point))
        Public Sub New()
            task.MaxZmeters = 20
            task.gOptions.stableDepthRGB.Checked = True
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            desc = "Create a Delaunay map using the RedCloud maxDist points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static myHeartBeat As Boolean
            If heartbeatLTX() Then myHeartBeat = True
            If task.almostHeartBeat Or task.firstPass Then
                If myHeartBeat Then
                    myHeartBeat = False
                    redC.Run(src)
                    labels(2) = redC.labels(2)
                    rcList = New List(Of rcData)(redC.rcList)

                    ptList.Clear()
                    For Each rc In redC.rcList
                        ptList.Add(rc.maxDist)
                    Next

                    rcMap.SetTo(0)
                    facetList = Delaunay_Basics.buildFacets(ptList)
                    For i = 0 To facetList.Count - 1
                        Dim index = task.delaunayFLess.rcMap.Get(Of Byte)(ptList(i).Y, ptList(i).X)
                        FillConvexPoly(rcMap, facetList(i), index, LineTypes.Link4)
                    Next

                    For Each rc In redC.rcList
                        If rc.contour.Count = 0 Then Continue For
                        DrawTour(dst2(rc.rect), rc.contour, task.scalarColors(rc.ID), -1)
                        rc.ID = task.delaunayFLess.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                        DrawTour(rcMap(rc.rect), rc.contour, rc.ID, -1)
                        DrawTour(dst2(rc.rect), rc.contour, black, task.lineWidth)
                    Next
                    rcMap.SetTo(0, task.edges.dst2)

                    dst2 = ApplyPalette(rcMap, 0)

                    'For Each rc In redC.rcList
                    '    DrawTour(dst2(rc.rect), rc.contour, black, task.lineWidth)
                    'Next

                    For Each pt In ptList
                        Circle(dst2, pt, task.DotSize, white, -1)
                    Next

                    dst3 = redC.dst2
                End If
            End If
        End Sub
    End Class






    Public Class XR_RedCloud_CellHistogram2 : Inherits TaskParent
        Dim plot As New PlotBar_Basics
        Dim redC As New RedCC_Core
        Public Sub New()
            task.gOptions.setHistogramBins(100)
            plot.createHistogram = True
            plot.removeZeroEntry = True
            desc = "Display the histogram of the RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            If task.rcD Is Nothing Then Exit Sub

            SetTrueText(redC.strOut, 1)

            labels(3) = "Select a RedCloud cell to see the histogram"

            Dim depth As Mat = task.pcSplit(2)(task.rcD.rect)
            depth.SetTo(0, task.noDepthMask(task.rcD.rect))
            plot.minRange = 0
            plot.maxRange = task.MaxZmeters
            plot.Run(depth)
            labels(3) = "0 meters to " + task.MaxZmeters.ToString(fmt0) + " meters - vertical lines every meter"

            Dim incr = dst2.Width / task.MaxZmeters
            For i = 1 To CInt(task.MaxZmeters - 1)
                Dim x = incr * i
                Line(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), Scalar.White, task.lineWidth, task.lineType)
            Next
            dst3 = plot.dst2
        End Sub
    End Class






    Public Class XR_RedCloud_CellHistogram1 : Inherits TaskParent
        Dim plot As New PlotBar_Basics
        Dim redC As New RedCloud_Basics
        Public Sub New()
            desc = "Display the histogram of the RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            labels(2) = redC.labels(2)

            Dim depths(redC.rcList.Count - 1) As Single
            For i = 1 To redC.rcList.Count - 1
                depths(i) = redC.rcList(i).depthAvg
            Next
            Dim depthMat = Mat.FromPixelData(depths.Length, 1, MatType.CV_32F, depths)

            Dim histogram As New Mat
            Dim ranges = {New Rangef(0, task.MaxZmeters)}
            CalcHist({depthMat}, {0}, New Mat, histogram, 1, {task.histogramBins}, ranges)

            plot.minRange = 0
            plot.maxRange = task.MaxZmeters
            plot.Run(histogram)
            labels(3) = "0 meters to " + task.MaxZmeters.ToString(fmt0)

            dst3 = plot.dst2

            For Each rc In redC.rcList
                If rc.ID = 0 Then Continue For

            Next
        End Sub
    End Class






    Public Class XR_RedCloud_CellHistogram : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Dim redColor As New RedColor_Basics
        Public Sub New()
            desc = "Display the histogram of the RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If heartbeatLTX() Then
                redColor.Run(src)

                redC.Run(src)
                labels(2) = redC.labels(2)

                Dim depths As New List(Of Single)
                For Each rc In redC.rcList
                    depths.Add(rc.depthAvg)
                Next

                Dim maxval = depths.Max
                Static maxDepth = Math.Ceiling(maxval)
                maxDepth = Math.Ceiling(maxval)
                For Each rc In redC.rcList
                    rc.ID = Math.Floor(task.histogramBins * rc.depthAvg / maxDepth)
                Next

                redC.rcMap.SetTo(0)
                For Each rc In redC.rcList
                    If rc.ID = 0 Then Continue For
                    DrawTour(redC.rcMap(rc.rect), rc.contour, New cv.Scalar(rc.ID), -1)
                Next

                dst2 = ApplyPalette(redC.rcMap, 0)
            End If
        End Sub
    End Class





    Public Class RedCloud_DepthTiers : Inherits TaskParent
        Public rcMap As cv.Mat
        Public rcList = New List(Of rcData)
        Public redMap As New RedPrep_Map
        Public Sub New()
            task.gOptions.HistBinBar.Value = 32
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            desc = "Classify the redColor cells by their depth to create tiers."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim depths As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            Dim maxDepth As Single
            For Each rc In task.redColor.rcList
                depths.Add(rc.depthMin, rc.index)
                If maxDepth < rc.depthMin Then maxDepth = rc.depthMax
            Next
            maxDepth = task.MaxZmeters
            labels(2) = "Depth values range From 0 to " + CStr(Math.Ceiling(maxDepth)) + " meters"

            dst3 = task.redColor.dst2

            Dim usedTiers As New List(Of Integer)
            For Each index In depths.Values
                Dim rc = task.redColor.rcList(index)
                If rc.ID = 0 Then Continue For
                Dim ID = Math.Floor(task.histogramBins * rc.depthMin / Math.Ceiling(maxDepth)) + 1
                task.redColor.rcList(index).ID = ID
                If usedTiers.Contains(ID) = False Then usedTiers.Add(ID)
            Next

            task.redColor.rcMap.SetTo(0)
            For Each rc In task.redColor.rcList
                If rc.ID = 0 Then Continue For
                DrawTour(task.redColor.rcMap(rc.rect), rc.contour, New cv.Scalar(rc.ID), -1)
                SetTrueText(rc.depthMin.ToString(fmt3) + "m", rc.maxDStable, 2)
            Next

            dst2 = ApplyPalette(task.redColor.rcMap, 0)
            labels(3) = CStr(usedTiers.Count) + " depth tiers"
        End Sub
    End Class






    Public Class XR_RedCloud_TierRanges : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Dim redC As New RedCloud_Basics
        Dim tiers As New List(Of Integer)
        Public Sub New()
            task.gOptions.HistBinBar.Value = 32
            desc = "Identify the ranges of the different layers of depth data."
        End Sub
        Private Function adjustTiers(depths As List(Of Single), maxVal As Single) As List(Of Integer)
            Dim usedTiers As New List(Of Integer)
            For Each index In depths
                Dim rc = redC.rcList(index)
                If rc.ID = 0 Then Continue For
                Dim ID = Math.Floor(task.histogramBins * rc.depthAvg / maxVal) + 1
                redC.rcList(index).ID = ID
                If usedTiers.Contains(ID) = False Then usedTiers.Add(ID)
            Next
            Return usedTiers
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If heartbeatLTX() Then
                redC.Run(src)
                labels(2) = redC.labels(2)

                Dim depths As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
                Dim maxDepth As Single
                For Each rc In redC.rcList
                    depths.Add(rc.depthMin, rc.index)
                    If maxDepth < rc.depthMax Then maxDepth = rc.depthMax
                Next

                Dim depthValues = New List(Of Single)(depths.Values)
                maxDepth = Math.Ceiling(maxDepth)
                While 1
                    tiers = adjustTiers(depthValues, maxDepth)
                    If tiers.Count >= 10 Then Exit While
                    If maxDepth <= 3 Then Exit While
                    maxDepth -= 1
                End While

                redC.rcMap.SetTo(0)
                For Each rc In redC.rcList
                    If rc.ID = 0 Then Continue For
                    DrawTour(redC.rcMap(rc.rect), rc.contour, New cv.Scalar(rc.ID), -1)
                Next
                dst2 = ApplyPalette(redC.rcMap, 0)
                labels(3) = CStr(tiers.Count) + " depth tiers"
            End If
        End Sub
    End Class






    'Public Class XR_RedCloud_TierRanges : Inherits TaskParent
    '    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    '    Dim redC As New RedCloud_Basics
    '    Dim redColor As New RedColor_Basics
    '    Public Sub New()
    '        task.gOptions.HistBinBar.Value = 32
    '        desc = "Identify the ranges of the different layers of depth data."
    '    End Sub
    '    Public Overrides Sub RunAlg(src As cv.Mat)
    '        redC.Run(src)
    '        labels(2) = redC.labels(2)

    '        Dim depthSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
    '        Dim maxDepth As Single
    '        For Each rc In redC.rcList
    '            depthSorted.Add(rc.depthMin, rc.index)
    '            If maxDepth < rc.depthMax Then maxDepth = rc.depthMax
    '        Next

    '        Const desiredCount As Integer = 5
    '        Dim incr As Single = maxDepth / desiredCount
    '        Dim boundaryMin As New List(Of Single)
    '        Dim boundaryMax As New List(Of Single)
    '        Dim maxDesired = CInt(depthSorted.Keys.Count / 3)
    '        While boundaryMin.Count < desiredCount Or boundaryMin.Count > maxDesired
    '            boundaryMin.Clear()
    '            boundaryMax.Clear()
    '            Dim val1 = depthSorted.Keys(1) ' first value is always zero.
    '            For i = 0 To depthSorted.Keys.Count - 2
    '                Dim val2 = depthSorted.Keys(i + 1)
    '                If (val2 - val1) > incr Then
    '                    boundaryMin.Add(val1)
    '                    boundaryMax.Add(val2)
    '                    val1 = val2
    '                End If
    '            Next
    '            If boundaryMin.Count < desiredCount Then
    '                incr -= 0.05
    '            End If
    '            If boundaryMin.Count > maxDesired Then
    '                incr += 0.05
    '            End If
    '        End While

    '        strOut = "Ranges for tiers" + vbCrLf
    '        Dim minVal As Single, maxVal As Single

    '        rcMap.SetTo(0)
    '        Dim tmp As New cv.Mat
    '        For i = 0 To boundaryMin.Count - 2
    '            minVal = boundaryMin(i)
    '            maxVal = boundaryMax(i)
    '            Dim bestBound = maxVal + (boundaryMin(i + 1) - maxVal) / 2
    '            InRange(task.pcSplit(2), minVal, bestBound, tmp)
    '            rcMap.SetTo(i + 1, tmp)
    '        Next
    '        InRange(task.pcSplit(2), boundaryMax.Last, task.MaxZmeters, tmp)
    '        rcMap.SetTo(boundaryMin.Count, tmp)

    '        dst2 = ApplyPalette(rcMap, 0)

    '        For Each rc In redC.rcList
    '            If rc.ID = 0 Then Continue For
    '            SetTrueText(rc.depthMin.ToString(fmt3) + "m", rc.maxDStable, 2)
    '        Next

    '        labels(3) = CStr(boundaryMin.Count) + " depth tiers"
    '    End Sub
    'End Class






    Public Class RedCloud_Core : Inherits TaskParent
        Public prepEdges As New RedPrep_Basics
        Public rcList As New List(Of rcData)
        Public Sub New()
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
            desc = "Find the biggest chunks of consistent depth data "
        End Sub
        Public Shared Function sweepImage(input As Mat) As List(Of rcData)
            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim mask = New Mat(New Size(input.Width + 2, input.Height + 2), MatType.CV_8U, 0)
            Dim flags As FloodFillFlags = FloodFillFlags.Link4
            Dim rc As rcData
            Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            For y = 0 To input.Height - 1 Step task.gridWH
                For x = 0 To input.Width - 1 Step task.gridWH
                    Dim pt = New cv.Point(x, y)
                    ' skip the regions with no depth or those that were already floodfilled.
                    If input.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                        Dim count = FloodFill(input, mask, pt, index, rect, 0, 0, flags)
                        If rect.Width > 0 And rect.Height > 0 Then
                            If count >= task.minCellSize Then
                                rc = New rcData(input(rect), rect, index)
                                newList.Add(rc.pixels, rc)
                                index += 1
                                rc.ID = newList.Count
                            End If
                        End If
                    End If
                Next
            Next

            Return New List(Of rcData)(newList.Values)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                prepEdges.Run(src)
                src = prepEdges.dst2.Clone
            End If

            rcList = sweepImage(src)
            If rcList.Count = 0 Then
                rcList.Add(New rcData(src, New cv.Rect(0, 0, src.Width, src.Height), 1))
                task.rcD = rcList(0)
            End If
            dst2.SetTo(0)
            For Each rc In rcList
                If rc.pixels < task.minCellSize Then Continue For
                dst2(rc.rect).SetTo(rc.ID, rc.mask)
            Next
            If rcList.Count >= 255 Then MsgBox("RedCloud_Core has exceeded the 8-bit max.  Increase task.minCellSize?")
            dst3 = ApplyPalette(dst2, 0)
            labels(2) = "RedCloud cells identified: " + CStr(rcList.Count)
        End Sub
    End Class




    Public Class RedCloud_FeatureLess : Inherits TaskParent
        Dim redColor = New RedColor_Basics
        Dim minPixels As Integer = dst2.Total * 0.025
        Public rcList As New List(Of rcData)
        Public Sub New()
            desc = "Restrict the floodfill for depth data to the featureless regions."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = task.depthmask.Clone
            dst1.SetTo(0, task.delaunayFLess.dst3)
            InRange(dst1, 0, 254, dst1)
            redColor.run(dst1)
            dst2 = redColor.dst2
            dst2.SetTo(0, task.noDepthMask)

            For Each rc In redColor.rclist
                If rc.pixels >= minPixels Then
                    rcList.Add(rc)
                End If
            Next
        End Sub
    End Class

End Namespace