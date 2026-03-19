Imports System.Runtime.InteropServices
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

            redLeft.Run(reduction.dst2 + 1)
            dst2 = Palettize(redLeft.dst2, 0)
            labels(2) = redLeft.labels(2) + " in the left image"

            reduction.Run(task.rightView)

            redRight.Run(reduction.dst2 + 1)
            dst3 = Palettize(redRight.dst2, 0)
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
            desc = "Attach an color8u class to each gRect."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.run(src)
            dst0 = task.leftView
            color8u.Run(task.leftView)

            redC.Run(color8u.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            labels(2) = redC.labels(3)
            dst2 = redC.dst2

            Dim count As Integer
            dst1.SetTo(0)
            For Each gRect As brickData In bricks.brickList
                If redC.rcMap(gRect.lRect).CountNonZero And gRect.rRect.Width > 0 Then
                    dst2(gRect.lRect).CopyTo(dst1(gRect.rRect))
                    gRect.colorClass = color8u.dst2.Get(Of Integer)
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
                Dim gRect = task.gridRects(i)

                Dim center = New cv.Point(CInt(gRect.X + gRect.Width / 2), CInt(gRect.Y + gRect.Height / 2))
                Dim index = redC.rcMap.Get(Of Integer)(center.Y, center.X) - 1
                If index >= redC.rcList.Count Or index < 0 Then Continue For
                Dim rc = redC.rcList(index)
                dst3(gRect).SetTo(rc.color)
                rcGridMap(gRect).SetTo(rc.index)
            Next
            strOut = RedUtil_Basics.selectCell(redC.rcMap, redC.rcList)
        End Sub
    End Class
End Namespace