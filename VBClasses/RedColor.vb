Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class RedColor_Basics : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public reduction As New Reduction_Basics
    Public runSelectCell As Boolean = True
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.fOptions.ReductionSlider.Value = 32
        desc = "Use the FeatureLess regions to improve the RedColor output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then
            reduction.Run(src)
            src = reduction.dst2
        End If

        Dim rect As cv.Rect
        Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
        Dim rectSorted As New SortedList(Of Integer, (count As Integer, r As cv.Rect))(New compareAllowIdenticalInteger)
        For Each r In task.fLess.brickList
            Dim val = mask(r).Get(Of Byte)(0, 0)
            If val = 0 Then
                Dim index As Integer = task.fLess.dst3(r).Get(Of Byte)(0, 0)
                If index > 0 Then
                    Dim flags = cv.FloodFillFlags.FixedRange Or cv.FloodFillFlags.Link4 Or (index << 8)
                    Dim count = cv.Cv2.FloodFill(src, mask, r.TopLeft, index, rect, 0, 0, flags)
                    rectSorted.Add(index, (count, ValidateRect(rect)))
                End If
            End If
        Next

        Dim rcSizeSort As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To rectSorted.Count - 2
            Dim r1 = rectSorted.ElementAt(i).Value.r
            If rectSorted.ElementAt(i).Key = rectSorted.ElementAt(i + 1).Key Then
                For j = i To rectSorted.Count - 2
                    Dim r2 = rectSorted.ElementAt(j + 1).Value.r
                    If rectSorted.ElementAt(j).Key = rectSorted.ElementAt(j + 1).Key Then
                        r1 = r1.Union(r2)
                    Else
                        Dim rc = New rcData(src(r1), r1, rectSorted.ElementAt(j).Key)
                        rc.index = rectSorted.ElementAt(j).Key
                        rcSizeSort.Add(rectSorted.ElementAt(i).Value.count, rc)
                        i = j
                        Exit For
                    End If
                Next
            Else
                Dim rc = New rcData(src(r1), r1, rectSorted.ElementAt(i).Key)
                rc.index = rectSorted.ElementAt(i).Key
                rcSizeSort.Add(rectSorted.ElementAt(i).Value.count, rc)
            End If
        Next

        rcMap.SetTo(0)
        rcList.Clear()
        For Each rc In rcSizeSort.Values
            rcList.Add(rc)
            rcMap(rc.rect).SetTo(rcList.Count, rc.mask)
        Next

        If runSelectCell Then
            strOut = Utility_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 1)
        End If

        dst3 = task.fLess.dst2

        labels(2) = CStr(rcList.Count) + " cells were identified."
    End Sub
End Class





Public Class RedColor_BrickList : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public reduction As New Reduction_Basics
    Public runSelectCell As Boolean = True
    Dim bricks As New FeatureLess_BrickList
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.fOptions.ReductionSlider.Value = 32
        desc = "Use the FeatureLess regions to improve the RedColor output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then
            reduction.Run(src)
            src = reduction.dst2
        End If

        bricks.Run(src)

        Dim rect As cv.Rect
        Dim mask = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
        Dim rectSorted As New SortedList(Of Integer, (index As Integer, r As cv.Rect))(New compareAllowIdenticalInteger)
        For Each r In bricks.brickList
            Dim val = mask.Get(Of Byte)(r.Y, r.X)
            If val = 0 Then
                Dim index As Integer = task.fLess.dst3.Get(Of Byte)(r.Y, r.X)
                If index > 0 Then
                    Dim flags = cv.FloodFillFlags.FixedRange Or cv.FloodFillFlags.Link4 Or (index << 8)
                    Dim count = cv.Cv2.FloodFill(src, mask, r.TopLeft, index, rect, 0, 0, flags)
                    rect = ValidateRect(rect)
                    If mask(rect).CountNonZero > 0 Then rectSorted.Add(index, (index, rect))
                End If
            End If
        Next

        Dim rcSizeSort As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To rectSorted.Count - 2
            Dim r1 = rectSorted.ElementAt(i).Value.r
            Dim r2 = rectSorted.ElementAt(i + 1).Value.r
            If rectSorted.ElementAt(i).Value.index = rectSorted.ElementAt(i + 1).Value.index Then
                For j = i To rectSorted.Count - 2
                    r2 = rectSorted.ElementAt(j + 1).Value.r
                    If rectSorted.ElementAt(j).Value.index = rectSorted.ElementAt(j + 1).Value.index Then
                        r1 = r1.Union(r2)
                    Else
                        Dim rc = New rcData(src(r1), r1, rectSorted.ElementAt(j).Value.index)
                        rcSizeSort.Add(rc.pixels, rc)
                        i = j
                        Exit For
                    End If
                Next
            Else
                Dim rc = New rcData(src(r1), r1, rectSorted.ElementAt(i).Value.index)
                rcSizeSort.Add(rc.pixels, rc)
            End If
        Next

        rcMap.SetTo(0)
        rcList.Clear()
        For Each rc In rcSizeSort.Values
            rc.index = rcList.Count + 1
            rcMap(rc.rect).SetTo(rc.index, rc.mask)
            rcList.Add(rc)
        Next

        If runSelectCell Then
            strOut = Utility_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 1)
        End If

        dst2 = Palettize(rcMap, 0)

        labels(2) = CStr(rcList.Count) + " cells were identified."
    End Sub
End Class





Public Class RedColor_BasicsOld : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public redFlood As New RedCloud_Flood_CPP
    Public runSelectCell As Boolean = True
    Dim tiers As New Depth_Tiers
    Dim color8U As New Color8U_Basics
    Public Sub New()
        desc = "Run the C++ RedCloud interface without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        color8U.Run(src)
        dst3 = color8U.dst2

        tiers.Run(src)

        Dim input As cv.Mat = dst3 + tiers.dst2 + 1
        input.SetTo(0, task.edges.dst2)
        redFlood.Run(input)
        dst2 = redFlood.dst2
        labels(2) = redFlood.labels(2)

        rcMap = redFlood.rcMap.Clone
        rcList = New List(Of rcData)(redFlood.rcList)

        If runSelectCell Then
            strOut = Utility_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)
        End If
    End Sub
End Class





Public Class NR_RedColor_Basics : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public options As New Options_RedCloud
    Public redFlood As New RedColor_Basics
    Public runSelectCell As Boolean = True
    Public Sub New()
        desc = "Run the C++ RedCloud interface without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        redFlood.Run(Mat_Basics.srcMustBe8U(src) + 1)
        dst2 = redFlood.dst2
        labels(2) = redFlood.labels(2)

        rcMap = redFlood.rcMap.Clone
        rcList = New List(Of rcData)(redFlood.rcList)

        If runSelectCell Then
            strOut = Utility_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)
        End If
    End Sub
End Class




Public Class NR_RedColor_CPP : Inherits TaskParent
    Implements IDisposable
    Public classCount As Integer
    Public rcList As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        cPtr = RedCloudLined_Open()
        desc = "Run the C++ RedCloud interface without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = Mat_Basics.srcMustBe8U(src)

        Dim imagePtr As IntPtr
        Dim inputData(dst1.Total - 1) As Byte
        dst1.GetArray(Of Byte)(inputData)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        imagePtr = RedCloudLined_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleInput.Free()
        dst0 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone

        classCount = RedCloudLined_Count(cPtr)

        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloudLined_Rects(cPtr))
        Dim rects(classCount - 1) As cv.Rect
        rectData.GetArray(Of cv.Rect)(rects)

        Dim rcListLast = New List(Of rcData)(rcList)
        Dim rcMapLast As cv.Mat = rcMap.Clone

        Dim minPixels As Integer = dst2.Total * 0.001
        Dim index As Integer = 1
        Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To rects.Count - 1
            Dim rc = New rcData(dst0(rects(i)), rects(i), index)
            If rc.pixels < minPixels Then Continue For
            newList.Add(rc.pixels, rc)
            index += 1
        Next

        Dim r2 As cv.Rect
        Dim count As Integer
        rcList.Clear()
        Dim usedColor As New List(Of cv.Scalar)
        For Each rc In newList.Values
            Dim r1 = rc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
            Dim indexLast As Integer = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If indexLast > 0 And indexLast < rcListLast.Count Then
                indexLast -= 1 ' index is 1 less than the rcMap value
                r2 = rcListLast(indexLast).rect
            Else
                indexLast = -1
            End If
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                rc.age = rcListLast(indexLast).age + 1
                rc.color = rcListLast(indexLast).color
                If rc.age >= 1000 Then rc.age = 2
                count += 1
            End If

            If usedColor.Contains(rc.color) Then
                rc.color = Palette_Basics.randomCellColor()
                rc.age = 1
            End If
            usedColor.Add(rc.color)

            rc.index = rcList.Count + 1
            rcList.Add(rc)
            rcMap(rc.rect).SetTo(rc.index, rc.mask)
            SetTrueText(CStr(rc.age), rc.maxDist)
        Next

        dst2.SetTo(0)
        For Each rc In rcList
            rc.mask = rcMap(rc.rect).InRange(rc.index, rc.index)
            rc.buildMaxDist()
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
        Next

        If standaloneTest() Then
            strOut = Utility_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)
        End If

        labels(2) = CStr(classCount) + " cells. " + CStr(rcList.Count) + " cells >" +
                        " minpixels.  " + CStr(count) + " matched to previous generation"
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = RedCloudLined_Close(cPtr)
    End Sub
End Class






Public Class RedColor_LeftRight : Inherits TaskParent
    Dim redLeft As New RedColor_Basics
    Dim redRight As New RedColor_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Display the RedColor_Basics output for both the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(task.leftView)

        redLeft.Run(reduction.dst2)
        dst2 = Palettize(redLeft.dst2)
        labels(2) = redLeft.labels(2) + " in the left image"

        reduction.Run(task.rightView)

        redRight.Run(reduction.dst2)
        dst3 = Palettize(redRight.dst2)
        labels(3) = redRight.labels(2) + " in the right image"
    End Sub
End Class





Public Class NR_RedColor_NWay : Inherits TaskParent
    Dim binN As New BinNWay_Basics
    Dim redC As New RedColor_Basics
    Public Sub New()
        desc = "Run RedColor on the output of the BinNWay_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binN.Run(src)
        dst3 = binN.dst3
        labels(3) = binN.labels(3)

        redC.Run(binN.dst2)
        labels(2) = redC.labels(2)
        dst2 = redC.dst2
    End Sub
End Class





Public Class RedColor_Bricks : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim color8u As New Color8U_Basics
    Public brickList As New List(Of brickData)
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst0.Checked = True
        desc = "Attach an color8u class to each r."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst0 = task.leftView
        color8u.Run(task.leftView)

        redC.Run(color8u.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        labels(2) = redC.labels(3)
        dst2 = redC.dst2

        Dim count As Integer
        dst1.SetTo(0)
        For Each brick As brickData In bricks.brickList
            If redC.rcMap(brick.lRect).CountNonZero And brick.rRect.Width > 0 Then
                dst2(brick.lRect).CopyTo(dst1(brick.rRect))
                brick.colorClass = color8u.dst2.Get(Of Integer)
                count += 1
            End If
        Next

        dst3 = ShowAddweighted(dst1, task.rightView, labels(3))
        labels(3) += " " + CStr(count) + " bricks mapped into the right image."
    End Sub
End Class






Public Class RedColor_Hulls : Inherits TaskParent
    Public rclist As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Dim redC As New RedColor_Basics
    Public Sub New()
        labels = {"", "Cells where convexity defects failed", "", "Improved contour results Using OpenCV's ConvexityDefects"}
        desc = "Add hulls and improved contours using ConvexityDefects to each RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim defectCount As Integer
        rcMap.SetTo(0)
        rclist.Clear()
        For Each rc In redC.rcList
            If rc.contour.Count >= 3 Then
                rc.hull = cv.Cv2.ConvexHull(rc.contour.ToArray, True).ToList
                Dim hullIndices = cv.Cv2.ConvexHullIndices(rc.hull.ToArray, False)
                Try
                    Dim defects = cv.Cv2.ConvexityDefects(rc.contour, hullIndices)
                    rc.contour = Convex_RedColorDefects.betterContour(rc.contour, defects)
                Catch ex As Exception
                    defectCount += 1
                End Try
                DrawTour(rcMap(rc.rect), rc.hull, rc.index, -1)
                rclist.Add(rc)
            End If
        Next
        dst3 = Palettize(rcMap)
        labels(3) = CStr(rclist.Count) + " hulls identified below.  " + CStr(defectCount) +
                        " hulls failed to build the defect list."
    End Sub
End Class





Public Class RedColor_GridRects : Inherits TaskParent
    Dim redC As New RedColor_Basics
    Public rcGridMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0) ' map of rc data to grid map
    Public Sub New()
        labels(3) = "RedColor output mapped into the gridRects."
        desc = "Create a triangle representation of the point cloud with RedCloud data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        rcGridMap.SetTo(0)
        dst3.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)

            Dim center = New cv.Point(CInt(r.X + r.Width / 2), CInt(r.Y + r.Height / 2))
            Dim index = redC.rcMap.Get(Of Integer)(center.Y, center.X) - 1
            If index >= redC.rcList.Count Or index < 0 Then Continue For
            Dim rc = redC.rcList(index)
            dst3(r).SetTo(rc.color)
            rcGridMap(r).SetTo(rc.index)
        Next
        strOut = Utility_Basics.selectCell(redC.rcMap, redC.rcList)
    End Sub
End Class






Public Class NR_RedColor_List : Inherits TaskParent
    Public inputRemoved As cv.Mat
    Public cellGen As New RedFlood_ToRedColor
    Public redMask As New NR_RedFlood_Basics
    Public rclist As New List(Of rcData)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public contours As New Contour_Basics
    Public Sub New()
        desc = "Find cells and then match them to the previous generation with minimum boundary"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        If src.Type <> cv.MatType.CV_8U Then
            dst1 = contours.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            dst1 = src
        End If

        If inputRemoved IsNot Nothing Then dst1.SetTo(0, inputRemoved)
        redMask.Run(dst1)

        If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
        cellGen.mdList = redMask.mdList
        cellGen.Run(redMask.dst2)

        rclist = New List(Of rcData)(cellGen.rcList)
        rcMap = cellGen.rcMap
        dst2 = Palettize(rcMap)

        labels(2) = cellGen.labels(2)
    End Sub
End Class








Public Class NR_RedColor_Lines : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0)
        desc = "Identify and track the lines in an image as RedCloud Cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then dst3.SetTo(0)
        Dim index As Integer
        For Each lp In task.lines.lpList
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            index += 1
            If index > 10 Then Exit For
        Next

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 3)
    End Sub
End Class










Public Class NR_RedColor_LineSingle : Inherits TaskParent
    Dim track As New RedColor_Contour
    Dim leftMost As Integer, rightmost As Integer
    Dim leftCenter As cv.Point, rightCenter As cv.Point
    Public Sub New()
        desc = "Create a line between the rightmost and leftmost good feature to show camera motion"
    End Sub
    Private Function findNearest(pt As cv.Point) As Integer
        Dim bestDistance As Single = Single.MaxValue
        Dim bestIndex As Integer
        For Each rc In track.redC.rcList
            Dim d = pt.DistanceTo(rc.maxDist)
            If d < bestDistance Then
                bestDistance = d
                bestIndex = rc.index
            End If
        Next
        Return bestIndex
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        track.Run(src)
        dst2 = track.dst2
        If track.redC.rcList.Count = 0 Then
            SetTrueText("No lines found to track.", 3)
            Exit Sub
        End If
        Dim xList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For Each rc In track.redC.rcList
            If rc.index = 0 Then Continue For
            xList.Add(rc.rect.X, rc.index)
        Next

        Dim minLeft As Integer = xList.Count / 4
        Dim minRight As Integer = (xList.Count - minLeft)

        If leftMost = 0 Or rightmost = 0 Or leftMost = rightmost Then
            leftCenter = rightCenter ' force iteration...
            Dim iterations As Integer
            While leftCenter.DistanceTo(rightCenter) < dst2.Width / 4
                leftMost = msRNG.Next(minLeft, minRight)
                rightmost = msRNG.Next(minLeft, minRight)
                leftCenter = track.redC.rcList(leftMost).maxDist
                rightCenter = track.redC.rcList(rightmost).maxDist
                iterations += 1
                If iterations > 10 Then Exit Sub
            End While
        End If

        leftMost = findNearest(leftCenter) - 1
        rightmost = findNearest(rightCenter) - 1
        If leftMost >= 0 And leftMost < track.redC.rcList.Count And
                    rightmost >= 0 And rightmost < track.redC.rcList.Count Then
            leftCenter = track.redC.rcList(leftMost).maxDist
            rightCenter = track.redC.rcList(rightmost).maxDist

            dst2.Line(leftCenter, rightCenter, white, task.lineWidth, task.lineType)
        End If
        labels(2) = track.redC.labels(2)
    End Sub
End Class







Public Class NR_RedColor_FeaturesKNN : Inherits TaskParent
    Public knn As New KNN_Basics
    Dim feat As New Feature_Basics
    Public Sub New()
        labels = {"", "", "Output of Feature_Stable", "Grid of points to measure motion."}
        desc = "Use KNN with the good features in the image to create a grid of points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.gray)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)

        knn.ptListQuery = New List(Of cv.Point)(feat.features)
        knn.ptListTrain = New List(Of cv.Point)(feat.features)
        knn.Run(src)

        dst3 = src.Clone
        For i = 0 To knn.queries.Count - 1
            Dim p1 = knn.ptListQuery(i)
            Dim index = knn.result(i, knn.trainInput.Count - 1)
            If index >= 0 And index < knn.ptListTrain.Count Then
                Dim p2 = knn.ptListTrain(index)
                dst3.Circle(p1, task.DotSize, cv.Scalar.Yellow, -1, task.lineType)
                dst3.Circle(p2, task.DotSize, cv.Scalar.Yellow, -1, task.lineType)
                dst3.Line(p1, p2, white, task.lineWidth, task.lineType)
            End If
        Next
        knn.ptListTrain = New List(Of cv.Point)(knn.ptListQuery)
    End Sub
End Class







Public Class NR_RedColor_GoodCellInput : Inherits TaskParent
    Public knn As New KNN_Basics
    Public featureList As New List(Of cv.Point2f)
    Dim feat As New Feature_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max feature travel distance", 0, 100, 10)
        desc = "Use KNN to find good features to track"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.gray)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)

        Static distSlider = OptionParent.FindSlider("Max feature travel distance")
        Dim maxDistance = distSlider.Value

        knn.ptListQuery = New List(Of cv.Point)(feat.features)
        knn.ptListTrain = New List(Of cv.Point)(feat.features)
        knn.Run(src)

        featureList.Clear()
        For i = 0 To knn.queries.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.result(i, 0) ' find nearest
            If index >= 0 And index < knn.trainInput.Count Then
                Dim p2 = knn.trainInput(index)
                If p1.DistanceTo(p2) < maxDistance Then featureList.Add(p1)
            End If
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class







Public Class NR_RedColor_Points : Inherits TaskParent
    Dim track As New RedColor_Contour
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "RedCloudX_Track output", "Input to RedCloudX_Track"}
        desc = "Identify and track the end points of lines in an image of RedCloud Cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3.SetTo(0)
        Dim index As Integer
        For Each lp In task.lines.lpList
            dst3.Circle(lp.p1, task.DotSize, 255, -1, task.lineType)
            dst3.Circle(lp.p2, task.DotSize, 255, -1, task.lineType)
            index += 1
            If index >= 10 Then Exit For
        Next

        track.Run(dst3)
        dst2 = track.dst2
    End Sub
End Class





Public Class RedColor_Contours : Inherits TaskParent
    Public contours As New Contour_Basics
    Public redC As New RedColor_Basics
    Public Sub New()
        labels(3) = "Contour_Basics output that is input to RedColor_Basics."
        desc = "Use the contour output as input to RedColor_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        contours.Run(src)
        dst1 = contours.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        redC.Run(dst1)
        dst2 = contours.dst2
        labels(2) = redC.labels(2)
    End Sub
End Class







Public Class RedColor_DelaunayMap : Inherits TaskParent
    Public dMap As New Delaunay_Map
    Dim redC As New RedColor_Basics
    Public Sub New()
        redC.runSelectCell = False
        desc = "Run RedColor as usual but use the Delaunay map to select cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dMap.rcList = New List(Of rcData)(redC.rcList)
        dMap.Run(emptyMat)

        strOut = Utility_Basics.DelaunaySelect(dMap.rcMap, dMap.rcList)
        SetTrueText(strOut, 3)
    End Sub
End Class







''' <summary>
''' Isolate a main subject from the scene (similar intent to iPhone "Copy Subject"): run RedColor segmentation,
''' pick a salient cell at the image center that is not the dominant background, then composite that region onto a neutral backdrop.
''' Click a cell (task.rcD) when available to override the auto-picked subject.
''' </summary>
Public Class RedColor_Isolate : Inherits TaskParent
    Dim redC As New RedColor_Basics
    Public Sub New()
        redC.runSelectCell = True
        desc = "Isolate subject via RedColor cells: auto-pick center cell (non-background size); use selected cell if set."
    End Sub
    Private Shared Function Clip(v As Integer, lo As Integer, hi As Integer) As Integer
        If v < lo Then Return lo
        If v > hi Then Return hi
        Return v
    End Function
    Private Shared Function CellMaskFull(rcMap As cv.Mat, rc As rcData) As cv.Mat
        Dim m As New cv.Mat(rcMap.Size, cv.MatType.CV_8U, 0)
        Using roi = rcMap(rc.rect)
            Using part = roi.InRange(rc.index, rc.index)
                part.CopyTo(m(rc.rect))
            End Using
        End Using
        Return m
    End Function
    Private Shared Sub MorphClean(mask As cv.Mat)
        Dim k = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(3, 3))
        cv.Cv2.MorphologyEx(mask, mask, cv.MorphTypes.Open, k)
    End Sub
    Private Function PickSubject(rcMap As cv.Mat, rcList As List(Of rcData)) As rcData
        Dim total = rcMap.Rows * rcMap.Cols
        Dim minPx = CInt(total * 0.003)
        Dim maxPx = CInt(total * 0.62)

        If task.rcD IsNot Nothing And task.rcD.pixels > 0 Then
            For Each rc In rcList
                If rc.index = task.rcD.index Then Return rc
            Next
        End If

        Dim cx = task.workRes.Width \ 2
        Dim cy = task.workRes.Height \ 2
        Dim idxCenter = rcMap.Get(Of Integer)(cy, cx) - 1
        If idxCenter >= 0 And idxCenter < rcList.Count Then
            Dim rc0 = rcList(idxCenter)
            If rc0.pixels >= minPx And rc0.pixels <= maxPx Then Return rc0
        End If

        Dim bestVotes As Integer = -1
        Dim bestRc As rcData = Nothing
        Dim freq As New Dictionary(Of Integer, Integer)
        For dy = -4 To 4
            For dx = -4 To 4
                Dim y = Clip(cy + dy, 0, rcMap.Rows - 1)
                Dim x = Clip(cx + dx, 0, rcMap.Cols - 1)
                Dim ix = rcMap.Get(Of Integer)(y, x)
                If ix <= 0 OrElse ix > rcList.Count Then Continue For
                Dim rc = rcList(ix - 1)
                If rc.pixels < minPx OrElse rc.pixels > maxPx Then Continue For
                If Not freq.ContainsKey(ix) Then freq(ix) = 0
                freq(ix) += 1
            Next
        Next
        For Each kv In freq
            If kv.Value > bestVotes Then
                bestVotes = kv.Value
                bestRc = rcList(kv.Key - 1)
            End If
        Next
        If bestRc IsNot Nothing Then Return bestRc

        Dim bestScore As Integer = -1
        For Each rc In rcList
            If rc.pixels < minPx OrElse rc.pixels > maxPx Then Continue For
            If rc.rect.Contains(New cv.Point(cx, cy)) = False Then Continue For
            If rc.pixels > bestScore Then
                bestScore = rc.pixels
                bestRc = rc
            End If
        Next
        Return bestRc
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim bgr = If(src IsNot Nothing And src.Channels() = 3, src, task.color)
        redC.Run(bgr)

        Dim subject = PickSubject(redC.rcMap, redC.rcList)
        If subject Is Nothing Then
            dst2 = bgr.Clone
            labels(2) = "Could not auto-pick a subject; try clicking a RedColor cell or adjust scene."
            dst3.SetTo(0)
            Exit Sub
        End If

        Dim mask = CellMaskFull(redC.rcMap, subject)
        MorphClean(mask)

        dst2 = New cv.Mat(bgr.Size(), cv.MatType.CV_8UC3, New cv.Scalar(245, 245, 245))
        bgr.CopyTo(dst2, mask)

        dst3 = New cv.Mat(mask.Size(), cv.MatType.CV_8UC3, New cv.Scalar(0, 0, 0))
        dst3.SetTo(New cv.Scalar(255, 255, 255), mask)

        labels(2) = "Subject index=" + CStr(subject.index) + ", pixels=" + CStr(subject.pixels) +
                    " (RedColor cell cutout; not ML portrait matting)."
        labels(3) = "White = kept region. Select another cell with RedColor UI to retarget."
        strOut = "Uses RedCloud color flood cells (RedColor_Basics). Auto-pick avoids cells covering most of the frame." + vbCrLf +
                 "For iPhone-like quality you would need a learned segmenter; this is a fast geometric proxy."
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class RedColor_FLessCorrelation : Inherits TaskParent
    Public redC As New RedCloud_Flood_CPP
    Dim corr As New Correlation_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "Contour_Basics output that is input to RedColor_Basics."
        desc = "Use the output of the Correlation_Basics as input the RedColor_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        corr.Run(src)
        dst3 = corr.dst2

        redC.Run(dst3)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 1)
    End Sub
End Class




Public Class RedColor_FLessMinMaxRange : Inherits TaskParent
    Public redC As New RedCloud_Flood_CPP
    Dim corrRange As New Correlation_MinMaxRange
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "Contour_Basics output that is input to RedColor_Basics."
        desc = "Use the output of the Correlation_Basics as input the RedColor_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        corrRange.Run(src)
        dst3 = corrRange.dst2

        redC.Run(dst3)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 1)
    End Sub
End Class





Public Class RedColor_FeatureLess : Inherits TaskParent
    Public redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use the output of the FeatureLess_Basics as input the RedColor_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = task.fLess.dst2
        labels(3) = task.fLess.labels(2)

        redC.Run(dst3)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 1)
    End Sub
End Class





Public Class RedColor_Contour : Inherits TaskParent
    Public redC As New RedColor_Basics
    Public Sub New()
        If New cv.Size(task.workRes.Width, task.workRes.Height) <> New cv.Size(168, 94) Then task.fOptions.FrameHistoryCount.Value = 1
        desc = "Get stats on each RedColor cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst3 = redC.dst2
        labels(3) = redC.labels(2)

        dst2.SetTo(0)
        For Each rc In redC.rcList
            DrawTour(dst2(rc.rect), rc.contour, rc.color, -1)
            If task.rcD IsNot Nothing Then
                If rc.index = task.rcD.index Then DrawTour(dst2(rc.rect), rc.contour, white, -1)
            End If
        Next
    End Sub
End Class