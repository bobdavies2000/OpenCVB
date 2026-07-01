Imports System.Runtime.InteropServices
Imports VBClasses
Imports cv = OpenCvSharp
Public Class RedCloud_Basics : Inherits TaskParent
    Public redCore As New RedCloud_Core
    Public rcList As New List(Of rcDataOld)
    Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public options As New Options_RedCloud
    Public keyColors As New KeyColor_Reduction
    Public runSelectCell As Boolean = True
    Public Sub New()
        task.gOptions.stableDepthRGB.Checked = True
        desc = "Build contours for each cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If task.quarterBeat Then keyColors.Run(task.gray)

        redCore.Run(src)
        labels(3) = redCore.labels(3)

        Dim rcListLast As New List(Of rcDataOld)(rcList)
        Dim rcMapLast As cv.Mat = rcMap.Clone

        rcList.Clear()
        rcMap.SetTo(0)
        dst2.SetTo(0)
        Dim matchCount As Integer
        Dim unMatched As Integer
        Dim matchAverage As Single
        Dim blackVec As New cv.Vec3b
        For Each rc In redCore.rcList
            rc = Utility_Basics.rcDataMatch(rc, rcListLast, rcMapLast)

            If rc.age = 1 Then unMatched += 1 Else matchCount += 1
            matchAverage += rc.age
            rc.mapID = rcList.Count + 1
            rcMap(rc.rect).SetTo(rc.mapID, rc.mask)

            rcList.Add(rc)

            If task.heartBeat Then
                Dim color = keyColors.dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                If color <> blackVec Then rc.color = color
            End If
            dst2(rc.rect).SetTo(rc.color, rc.mask)
        Next

        If runSelectCell Then
            strOut = Utility_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)
        End If

        dst1 = keyColors.dst2

        labels(2) = CStr(unMatched) + " were new cells and " + CStr(matchCount) + " were matched, " +
                                "average age: " + Format(matchAverage / rcList.Count, fmt1)
        labels(3) = redCore.labels(3)

        'If task.heartbeatFrame + task.gOptions.DebugSlider.Value = task.frameCount Then
        '    dst3 = dst2.Clone
        'End If
    End Sub
End Class






Public Class RedCloud_Core : Inherits TaskParent
    Public prepEdges As New RedPrep_Basics
    Public rcList As New List(Of rcDataOld)
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Find the biggest chunks of consistent depth data "
    End Sub
    Public Shared Function sweepImage(input As cv.Mat, minSize As Integer) As List(Of rcDataOld)
        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim mask = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4
        Dim rc As rcDataOld = Nothing
        Dim newList As New SortedList(Of Integer, rcDataOld)(New compareAllowIdenticalIntegerInverted)
        For y = 0 To input.Height - 1
            For x = 0 To input.Width - 1
                Dim pt = New cv.Point(x, y)
                ' skip the regions with no depth or those that were already floodfilled.
                If input.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                    Dim count = cv.Cv2.FloodFill(input, mask, pt, index, rect, 0, 0, flags)
                    If rect.Width > 0 And rect.Height > 0 Then
                        If count >= minSize Then
                            rc = New rcDataOld(input(rect), rect, index)
                            If rc.mapID < 0 Then Continue For
                            newList.Add(rc.pixels, rc)
                            index += 1
                            rc.mapID = newList.Count
                        End If
                    End If
                End If
                If index = 254 Then index = 1
            Next
        Next
        Return New List(Of rcDataOld)(newList.Values)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then
            prepEdges.Run(src)
            src = prepEdges.dst2.Clone
        End If

        rcList = sweepImage(src, src.Total * 0.0001)
        If rcList.Count = 0 Then
            rcList.Add(New rcDataOld(src, New cv.Rect(0, 0, src.Width, src.Height), 1))
            task.rcD = rcList(0)
        End If
        dst2.SetTo(0)
        For Each rc In rcList
            dst2(rc.rect).SetTo(rc.mapID Mod 254, rc.mask)
        Next
        dst3 = Palettize(dst2, 0)
        labels(2) = "RedCloud cells identified: " + CStr(rcList.Count)
    End Sub
End Class







Public Class XR_RedCloud_Basics : Inherits TaskParent
    Public redC As New RedColor_Basics
    Public rcList As New List(Of rcDataOld)
    Public rcMap As cv.Mat
    Public Sub New()
        desc = "Assign abstract world coordinates to each RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If task.rcD IsNot Nothing Then dst2.Rectangle(task.rcD.rect, task.highlight, task.lineWidth)
        If strOut <> "" Then SetTrueText(redC.strOut, 3) Else SetTrueText("Click on any cell", 3)

        Dim causeLabel = Utility_Basics.findCause(redC.rcMap, redC.rcList)
        If task.mouseClickFlag Then
            causeLabel = ""
            labels(3) = ""
        End If

        If causeLabel <> "" Then
            If labels(3) = "" Then labels(3) = causeLabel Else labels(3) += ", " + causeLabel
            If labels(3).Length > 80 Then labels(3) = causeLabel
        End If

        rcList = New List(Of rcDataOld)(redC.rcList)
        rcMap = redC.rcMap.Clone
    End Sub
End Class








Public Class XR_RedCloud_CellDepthHistogram : Inherits TaskParent
    Dim plot As New PlotBar_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        task.gOptions.setHistogramBins(100)
        If standalone Then task.gOptions.displayDst1.Checked = True
        plot.createHistogram = True
        desc = "Display the histogram of a selected RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        SetTrueText(redC.strOut, 1)

        labels(3) = "Select a RedCloud cell to see the histogram"

        Dim depth As cv.Mat = task.pcSplit(2)(task.rcD.rect)
        depth.SetTo(0, task.noDepthMask(task.rcD.rect))
        cv.Cv2.ImShow("depth", depth)
        plot.minRange = 0
        plot.maxRange = task.MaxZmeters
        plot.Run(depth)
        labels(3) = "0 meters to " + Format(task.MaxZmeters, fmt0) + " meters - vertical lines every meter"

        Dim incr = dst2.Width / task.MaxZmeters
        For i = 1 To CInt(task.MaxZmeters - 1)
            Dim x = incr * i
            dst3.Line(New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White, task.lineWidth, task.lineType)
        Next
        dst3 = plot.dst2
    End Sub
End Class




Public Class RedCloud_LeftRight : Inherits TaskParent
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
            If redC.rcMap(brick.lRect).CountNonZero And brick.rRect.Width > 0 Then
                dst2(brick.lRect).CopyTo(dst1(brick.rRect))
                count += 1
            End If
        Next

        dst3 = ShowAddweighted(dst1, task.rightView, labels(3))
        labels(3) += " " + CStr(count) + " bricks mapped into the right image."
    End Sub
End Class





Public Class RedCloud_KNN : Inherits TaskParent
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
            knn.trainInput.Clear()
            For Each pt In rc.contour
                knn.trainInput.Add(pt)
            Next
            knn.queries = New List(Of cv.Point2f)({New cv.Point2f(0, 0), New cv.Point2f(rc.rect.Width, 0),
                                  New cv.Point2f(rc.rect.Width, rc.rect.Height), New cv.Point2f(0, rc.rect.Height)})
            knn.Run(Nothing)

            listOfPoints.Clear()
            Dim hullList As New List(Of cv.Point)
            For i = 0 To 3
                Dim pt = knn.trainInput(knn.result(i, 0))
                hullList.Add(New cv.Point(rc.rect.X + pt.X, rc.rect.Y + pt.Y))
            Next
            listOfPoints.Add(hullList)
            dst3.FillPoly(listOfPoints, rc.color)
        Next
    End Sub
End Class




Public Class RedCloud_RGB : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Display the RGB data rather than the rc.color"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        For Each rc In redC.rcList
            src(rc.rect).CopyTo(dst3(rc.rect), rc.mask)
        Next
    End Sub
End Class




Public Class RedCloud_Matches : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public rcList As New List(Of rcDataOld)
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
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                rcList.Add(rc)
            Else
                dst2(rc.rect).SetTo(white, rc.mask)
            End If
        Next

        If task.rcD IsNot Nothing Then dst2.Rectangle(task.rcD.rect, task.highlight, task.lineWidth)
        SetTrueText(redC.strOut, 3)
        labels(3) = CStr(rcList.Count) + " matched cells below with > " + CStr(redC.options.ageThreshold) + " age"
    End Sub
End Class





Public Class RedCloud_Matched : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public rcList As New List(Of rcDataOld)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.fOptions.ReductionColor.Value = 120
        desc = "Use the first cell when age > 1"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        labels(2) = redC.labels(2).Replace("new cells", "new cells (white)")

        dst2.SetTo(0)
        rcList.Clear()
        For Each rc In redC.rcList
            If rc.age >= redC.options.ageThreshold Or rc.age = task.frameCount Then
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                rcList.Add(rc)
            Else
                dst2(rc.rect).SetTo(white, rc.mask)
            End If
        Next

        If task.rcD IsNot Nothing Then dst2.Rectangle(task.rcD.rect, task.highlight, task.lineWidth)
        SetTrueText(redC.strOut, 1)
        labels(3) = CStr(rcList.Count) + " matched cells below with > " + CStr(redC.options.ageThreshold) + " age"
    End Sub
End Class






Public Class XR_RedCloud_ColorChangeCause : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Click on a cell to determine why it is changing colors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
        dst2.SetTo(0, task.noDepthMask)

        labels(3) = Utility_Basics.findCause(redC.rcMap, redC.rcList)
    End Sub
End Class






Public Class RedCloud_Reliable : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Display only those cells that are consistently present since the last heartbeat."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim count As Integer
        For Each rc In redC.rcList
            If rc.age > Math.Min(10, task.frameCount) Then
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                count += 1
            End If
        Next
        labels(3) = CStr(count) + " were consistently present."
    End Sub
End Class






Public Class RedCloud_Flood_CPP : Inherits TaskParent
    Implements IDisposable
    Public classCount As Integer
    Public rcList As New List(Of rcDataOld)
    Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public wGridList As New List(Of cv.Point3d)
    Public options As New Options_RedCloud
    Public keyColors As New KeyColor_Reduction
    Public Sub New()
        cPtr = RedCloudFill_Open()
        desc = "This is before matching to previous generation."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        keyColors.Run(emptyMat)

        If src.Channels <> 1 Then
            Static prepData As New RedPrep_Core
            prepData.Run(src)
            dst1 = prepData.reduced32f.Normalize(255, 0, cv.NormTypes.MinMax)
            dst1.ConvertTo(dst1, cv.MatType.CV_8U)
        Else
            dst1 = src
        End If

        Dim imagePtr As IntPtr
        Dim inputData(dst1.Total - 1) As Byte
        dst1.GetArray(Of Byte)(inputData)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        imagePtr = RedCloudFill_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleInput.Free()

        Dim rMask = New cv.Rect(1, 1, dst1.Width, dst1.Height)
        Dim mask = cv.Mat.FromPixelData(dst1.Rows + 2, dst1.Cols + 2, cv.MatType.CV_8U, imagePtr)
        dst0 = mask(rMask).Clone

        classCount = RedCloudFill_Count(cPtr)
        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloudFill_Rects(cPtr))
        Dim rects(classCount - 1) As cv.Rect
        rectData.GetArray(Of cv.Rect)(rects)

        Dim rcListLast = New List(Of rcDataOld)(rcList)
        Dim rcMapLast As cv.Mat = rcMap.Clone

        Dim index As Integer = 1
        Dim newList As New SortedList(Of Integer, rcDataOld)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To rects.Count - 1
            Dim rc = New rcDataOld(dst0(rects(i)), rects(i), index)
            newList.Add(rc.pixels, rc)
            index += 1
        Next

        rcList.Clear()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC3, 0)
        Dim matchCount As Integer
        Dim unMatched As Integer
        Dim matchAverage As Single
        dst3.SetTo(0)
        Dim blackVec As New cv.Vec3b
        rcMap.SetTo(0)
        For i = 0 To newList.Values.Count - 1
            Dim rc = newList.Values(i)
            Dim maxDist = rc.maxDist
            rc = Utility_Basics.rcMatch(rc, rcListLast, wGridList, rcMapLast)

            If rc.age = 1 Then unMatched += 1 Else matchCount += 1
            matchAverage += rc.age

            rc.mapID = rcList.Count + 1

            If task.heartBeat Then
                Dim color = keyColors.dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                If color <> blackVec Then rc.color = color
            End If

            Dim testIfClaimed = rcMap.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
            If testIfClaimed <> 0 Then Continue For

            rcList.Add(rc)

            dst2(rc.rect).SetTo(rc.color, rc.mask)
            rcMap(rc.rect).SetTo(rc.mapID, rc.mask)
        Next



        'For Each rc In rcList
        '    Dim test = rcMap.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
        '    If rc.mapID <> test Then Dim k = 0
        'Next




        strOut = Utility_Basics.selectCell(rcMap, rcList)
        SetTrueText(strOut, 3)

        wGridList.Clear()
        For Each rc In rcList
            wGridList.Add(rc.wGrid)
        Next

        labels(2) = CStr(unMatched) + " were new cells and " + CStr(matchCount) + " were matched, " +
                                "average age: " + Format(matchAverage / rcList.Count, fmt1)
    End Sub
    Protected Overrides Sub Finalize()
        If cPtr <> 0 Then cPtr = RedCloudFill_Close(cPtr)
    End Sub
End Class





Public Class RedCloud_MotionFilter : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public rcList As New List(Of rcDataOld)
    Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Dim pcMotion As New Motion_CloudPixel
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Filter changes to the RedCloud cells with motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pcMotion.Run(emptyMat)

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        redC.rcMap.ConvertTo(dst0, cv.MatType.CV_8U)
        dst1.SetTo(0)
        dst0.CopyTo(dst1, pcMotion.dst2)

        Dim histogram As New cv.Mat
        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, redC.rcList.Count + 1)}
        cv.Cv2.CalcHist({dst1}, {0}, New cv.Mat, histogram, 1, {redC.rcList.Count}, ranges)

        Dim count = histogram.CountNonZero()
        SetTrueText(CStr(count) + " cells had motion.", 3)
        If count = 0 Then Exit Sub
        histogram.Set(Of Single)(0, 0, 0) ' remove the count for cell 0 - no cell information.

        Dim histArray(histogram.Rows - 1) As Single
        histogram.GetArray(Of Single)(histArray)

        Dim rcMotionCells As New List(Of Integer)
        For i = 1 To histArray.Count - 1
            Dim rc = redC.rcList(i - 1)
            If histArray(i) > rc.pixels / 10 Then rcMotionCells.Add(i)
        Next

        dst3.SetTo(0)
        rcMap.SetTo(0)
        rcList.Clear()
        For Each rc In redC.rcList
            If rc.age > 1 Then
                If rcMotionCells.Contains(rc.mapID) = False Then
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    rcMap(rc.rect).SetTo(rc.mapID, rc.mask)
                    rcList.Add(rc)
                End If
            End If
        Next
    End Sub
End Class





Public Class RedCloud_Motion : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Dim addw As New AddWeighted_Basics
    Dim pcMotion As New Motion_CloudPixel
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
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






Public Class RedCloud_Foreground : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Find and monitor the cells in the foreground"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim count As Integer
        Dim maxDepth = task.foreground.foregroundMaxDepth
        For Each rc In redC.rcList
            If rc.wcMean(2) < maxDepth Then
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                count += 1
            End If
        Next
        labels(3) = CStr(count) + " RedCloud cells were in the foreground (< " + Format(maxDepth, fmt1) + " meters)"
    End Sub
End Class







Public Class RedCloud_Min : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Public rcMap As New cv.Mat
    Public rcList As New List(Of rcData) ' includes cloud data.
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "RedCloud cells with depth."
        desc = "FloodFill each color8U output and create an rclist"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then color8u.Run(task.gray) Else color8u.Run(src)
        src = color8u.dst2 + 1

        rcMap = src.Clone
        Dim minList As New List(Of rcData)
        Dim rect As cv.Rect
        Dim mask As cv.Mat = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8U, 0)
        For Each r In task.gridRects
            If mask(r).Get(Of Byte)(0, 0) = 0 Then
                Dim mapID As Integer = rcMap(r).Get(Of Byte)(0, 0)
                Dim flags = cv.FloodFillFlags.FixedRange Or cv.FloodFillFlags.MaskOnly Or (255 << 8)
                Dim count = cv.Cv2.FloodFill(rcMap, mask, r.TopLeft, mapID, rect, 0, 0, flags)
                If count > 0 Then minList.Add(New rcData(rcMap(rect), rect, mapID))
            End If
        Next
        dst2 = Palettize(rcMap)

        If task.rcMinD IsNot Nothing And standaloneTest() Then dst2.Rectangle(task.rcMinD.rect, task.highlight, task.lineWidth)

        dst3.SetTo(0)
        Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To minList.Count - 1
            Dim rc = minList(i)
            rc.maxDist = rc.buildMaxDist(rc.mask)

            rc.depth = task.pcSplit(2)(rc.rect).Mean(rc.mask)
            rc.maskDepth = rc.mask.Clone
            rc.maskDepth.SetTo(0, task.noDepthMask(rc.rect))
            rc.pixelsDepth = rc.maskDepth.CountNonZero
            rc.maxDistDepth = rc.buildMaxDist(rc.maskDepth)

            sortList.Add(rc.pixels, rc)
        Next

        rcList = New List(Of rcData)(sortList.Values)
        Dim rcIndex As Integer
        For Each rc In rcList
            rc.index = rcIndex
            dst0(rc.rect).SetTo(rc.mapID, rc.mask)
            rcIndex += 1
        Next

        Static picTag = task.mousePicTag
        If task.mouseClickFlag Then picTag = task.mousePicTag
        strOut = Utility_Basics.selectMinCell(rcMap, rcList, picTag)
        SetTrueText(strOut, 1)

        dst3 = Palettize(dst0)
        dst3.SetTo(0, task.noDepthMask)
        labels(2) = CStr(rcList.Count) + " RedColor cells were found."
    End Sub
End Class