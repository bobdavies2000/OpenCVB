Imports System.Runtime.InteropServices
Imports System.Windows.Documents
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedColor_Basics : Inherits TaskParent
        Implements IDisposable
        Public classCount As Integer
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public options As New Options_RedCloud
        Public redFlood As New RedCloud_Flood_CPP
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

            strOut = RedUtil_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)
            If task.rcD Is Nothing Then
                SetTrueText("Select any cell", 3)
                Exit Sub
            End If
        End Sub
    End Class




    Public Class NR_RedColor_CPP : Inherits TaskParent
        Implements IDisposable
        Public classCount As Integer
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
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
                strOut = RedUtil_Basics.selectCell(rcMap, rcList)
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
            For Each r As brickData In bricks.brickList
                If redC.rcMap(r.lRect).CountNonZero And r.rRect.Width > 0 Then
                    dst2(r.lRect).CopyTo(dst1(r.rRect))
                    r.colorClass = color8u.dst2.Get(Of Integer)
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
            strOut = RedUtil_Basics.selectCell(redC.rcMap, redC.rcList)
        End Sub
    End Class






    Public Class RedColor_List : Inherits TaskParent
        Public inputRemoved As cv.Mat
        Public cellGen As New RedMask_ToRedColor
        Public redMask As New RedMask_Basics
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






    Public Class RedColor_FeatureLess : Inherits TaskParent
        Public redC As New RedColor_Basics
        Dim corrRange As New Correlation_Range
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

            dst2.SetTo(0, corrRange.dst3)
            redC.rcMap.SetTo(0, corrRange.dst3)

            SetTrueText(redC.strOut, 1)
        End Sub
    End Class




    Public Class RedColor_StableRegions : Inherits TaskParent
        Public noMatch As New RedColor_NoMatching
        Public trackedMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Dim previousRegions As New List(Of RegionTrack)
        Dim colorIndex As Integer
        Dim fLess As New FeatureLess_Stabilized
        Private Class RegionTrack
            Public rect As cv.Rect
            Public centroid As cv.Point2f
            Public color As cv.Scalar
            Public age As Integer
        End Class
        Public Sub New()
            labels(3) = "Input from RedColor_NoMatching."
            desc = "Cursor: Track connected regions from RedColor_NoMatching and keep stable colors while regions persist."
        End Sub

        Public Overrides Sub RunAlg(src As cv.Mat)
            fLess.Run(task.gray)

            noMatch.Run(fLess.dst2)
            dst3 = noMatch.dst2

            Dim ccLabels As New cv.Mat
            Dim stats As New cv.Mat
            Dim centroids As New cv.Mat
            Dim ccCount = cv.Cv2.ConnectedComponentsWithStats(fLess.dst2, ccLabels, stats, centroids)

            trackedMap.SetTo(0)
            dst2.SetTo(0)
            Dim currentRegions As New List(Of RegionTrack)
            Dim usedPrevious As New HashSet(Of Integer)
            Dim matchedCount As Integer
            Dim minPixels = CInt(dst2.Total * 0.001)
            If minPixels < 25 Then minPixels = 25

            For ccIndex = 0 To ccCount - 1
                Dim x = stats.Get(Of Integer)(ccIndex, 0)
                Dim y = stats.Get(Of Integer)(ccIndex, 1)
                Dim w = stats.Get(Of Integer)(ccIndex, 2)
                Dim h = stats.Get(Of Integer)(ccIndex, 3)
                Dim area = stats.Get(Of Integer)(ccIndex, 4)
                If area < minPixels Or w <= 0 Or h <= 0 Then Continue For

                Dim rect = New cv.Rect(x, y, w, h)
                Dim cx = CSng(centroids.Get(Of Double)(ccIndex, 0))
                Dim cy = CSng(centroids.Get(Of Double)(ccIndex, 1))
                Dim region As New RegionTrack With {
                    .rect = rect,
                    .centroid = New cv.Point2f(cx, cy),
                    .age = 1
                }

                Dim bestMatch = FindBestPrevious(region, usedPrevious)
                If bestMatch >= 0 Then
                    region.color = previousRegions(bestMatch).color
                    region.age = previousRegions(bestMatch).age + 1
                    usedPrevious.Add(bestMatch)
                    matchedCount += 1
                Else
                    region.color = NextStableColor()
                End If

                currentRegions.Add(region)

                Dim roiLabels = ccLabels(rect)
                Dim roiMask = roiLabels.InRange(ccIndex, ccIndex)
                trackedMap(rect).SetTo(currentRegions.Count, roiMask)
                dst2(rect).SetTo(region.color, roiMask)
                SetTrueText(CStr(region.age), New cv.Point(CInt(region.centroid.X), CInt(region.centroid.Y)))
            Next

            previousRegions = currentRegions

            labels(2) = CStr(currentRegions.Count) + " stable regions found. " + CStr(matchedCount) + " matched to previous frame."
            labels(3) = "Input from RedColor_NoMatching (" + CStr(noMatch.rcList.Count) + " regions)."
        End Sub

        Private Function FindBestPrevious(curr As RegionTrack, usedPrevious As HashSet(Of Integer)) As Integer
            Dim bestIndex = -1
            Dim bestScore As Double = 0
            For i = 0 To previousRegions.Count - 1
                If usedPrevious.Contains(i) Then Continue For
                Dim prev = previousRegions(i)
                Dim overlap = RectOverlapScore(curr.rect, prev.rect)
                Dim d = curr.centroid.DistanceTo(prev.centroid)
                Dim distThreshold = Math.Max(curr.rect.Width, curr.rect.Height) * 1.25
                Dim score = overlap
                If d <= distThreshold Then score += 0.5 * (1.0 - d / Math.Max(1, distThreshold))
                If score > bestScore Then
                    bestScore = score
                    bestIndex = i
                End If
            Next
            If bestScore < 0.15 Then Return -1
            Return bestIndex
        End Function

        Private Function RectOverlapScore(a As cv.Rect, b As cv.Rect) As Double
            Dim inter = a.Intersect(b)
            If inter.Width <= 0 Or inter.Height <= 0 Then Return 0
            Dim interArea = CDbl(inter.Width) * inter.Height
            Dim unionArea = CDbl(a.Width) * a.Height + CDbl(b.Width) * b.Height - interArea
            If unionArea <= 0 Then Return 0
            Return interArea / unionArea
        End Function

        Private Function Distance(a As cv.Point2f, b As cv.Point2f) As Double
            Dim dx = a.X - b.X
            Dim dy = a.Y - b.Y
            Return Math.Sqrt(dx * dx + dy * dy)
        End Function

        Private Function NextStableColor() As cv.Scalar
            Dim vec = task.vecColors(colorIndex Mod task.vecColors.Length)
            colorIndex += 1
            Return New cv.Scalar(vec.Item0, vec.Item1, vec.Item2)
        End Function
    End Class







    Public Class RedColor_NoMatching : Inherits TaskParent
        Implements IDisposable
        Public classCount As Integer
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public wGridList As New List(Of cv.Point3d)
        Public options As New Options_RedCloud
        Dim myColors(255) As cv.Vec3b
        Public Sub New()
            myColors = task.vecColors
            cPtr = RedCloudFill_Open()
            desc = "This is before matching to previous generation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If standalone Then
                Static fLess As New FeatureLess_Stabilized
                fLess.Run(task.gray)
                src = fLess.dst2
            End If

            Dim imagePtr As IntPtr
            Dim inputData(src.Total - 1) As Byte
            src.GetArray(Of Byte)(inputData)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = RedCloudFill_Run(cPtr, handleInput.AddrOfPinnedObject(), src.Rows, src.Cols)
            handleInput.Free()

            Dim rMask = New cv.Rect(1, 1, src.Width, src.Height)
            Dim mask = cv.Mat.FromPixelData(src.Rows + 2, src.Cols + 2, cv.MatType.CV_8U, imagePtr)
            dst0 = mask(rMask).Clone

            classCount = RedCloudFill_Count(cPtr)
            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloudFill_Rects(cPtr))
            Dim rects(classCount - 1) As cv.Rect
            rectData.GetArray(Of cv.Rect)(rects)

            Dim rcMapLast = rcMap.Clone
            Dim rcLastList = New List(Of rcData)(rcList)

            rcList.Clear()
            rcMap.SetTo(0)
            Dim count As Integer
            Dim ages As New List(Of Integer)
            For Each r In rects
                Dim rc = New rcData(dst0(r), r, rcList.Count + 1)
                Dim val = rcMapLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
                If val <> 0 Then
                    Dim nextColor = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                    If nextColor = myColors(val) Then
                        count += 1
                        If val < rcLastList.Count Then rc.age = rcLastList(val).age + 1
                    Else
                        myColors(val) = nextColor
                    End If
                End If
                ages.Add(rc.age)
                rcList.Add(rc)
                rcMap(r).SetTo(rcList.Count, rc.mask)
            Next

            task.colorMap = cv.Mat.FromPixelData(256, 1, cv.MatType.CV_8UC3, myColors)
            dst2 = Palettize(rcMap, 0)

            strOut = RedUtil_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)

            labels(2) = CStr(rcList.Count) + " cells found and " + CStr(count) + " matched their previous color. "
            labels(3) = "Average age = " + Format(ages.Average, fmt1)
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = RedCloudFill_Close(cPtr)
        End Sub
    End Class
End Namespace