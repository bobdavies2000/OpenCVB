Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
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
Public Class RedColor_Contour : Inherits TaskParent
    Public redC As New RedColor_Basics
    Public Sub New()
        If New cv.Size(task.workRes.Width, task.workRes.Height) <> New cv.Size(168, 94) Then task.frameHistoryCount = 1
        desc = "Get stats on each RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst3 = redC.dst2
        labels(3) = redC.labels(2)

        dst2.SetTo(0)
        For Each rc As rcData In redC.rcList
            DrawTour(dst2(rc.rect), rc.contour, rc.color, -1)
            If rc.index = task.rcD.index Then DrawTour(dst2(rc.rect), rc.contour, white, -1)
        Next
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
            vbc.DrawLine(dst3, lp.p1, lp.p2, 255)
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

            vbc.DrawLine(dst2, leftCenter, rightCenter, white)
        End If
        labels(2) = track.redC.labels(2)
    End Sub
End Class







Public Class NR_RedColor_FeaturesKNN : Inherits TaskParent
    Public knn As New KNN_Basics
    Public Sub New()
        labels = {"", "", "Output of Feature_Stable", "Grid of points to measure motion."}
        desc = "Use KNN with the good features in the image to create a grid of points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        knn.queries = New List(Of cv.Point2f)(task.features)
        knn.Run(src)

        dst3 = src.Clone
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.neighbors(i)(knn.neighbors(i).Count - 1)
            If index >= 0 And index < knn.trainInput.Count Then
                Dim p2 = knn.trainInput(index)
                DrawCircle(dst3, p1, task.DotSize, cv.Scalar.Yellow)
                DrawCircle(dst3, p2, task.DotSize, cv.Scalar.Yellow)
                vbc.DrawLine(dst3, p1, p2, white)
            End If
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class







Public Class NR_RedColor_GoodCellInput : Inherits TaskParent
    Public knn As New KNN_Basics
    Public featureList As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max feature travel distance", 0, 100, 10)
        desc = "Use KNN to find good features to track"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static distSlider = OptionParent.FindSlider("Max feature travel distance")
        Dim maxDistance = distSlider.Value

        knn.queries = New List(Of cv.Point2f)(task.features)
        knn.Run(src)

        featureList.Clear()
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.neighbors(i)(0) ' find nearest
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
            DrawCircle(dst3, lp.p1, task.DotSize, 255)
            DrawCircle(dst3, lp.p2, task.DotSize, 255)
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
