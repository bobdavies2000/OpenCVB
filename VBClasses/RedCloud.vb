Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Enum causes
        lastCellFound
        indexLastGood
        indexLastBelowZero
        indexLastAboveCount
        intersectLastRectFailed
        optionsChange
        maxDistOutsideOfLastRect
        colorSync
        wGridNotInLastList
    End Enum
    Public Class RedCloud_Basics : Inherits TaskParent
        Public redCore As New RedCloud_Core
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public options As New Options_RedCloud
        Public keyColors As New KeyColor_Reduction_TA
        Public Sub New()
            If standalone Then
                task.gOptions.displayDst1.Checked = True
                labels(1) = "The key colors used to set colors in the RedCloud output."
            End If
            desc = "Build contours for each cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If task.quarterBeat Then keyColors.Run(task.gray)

            redCore.Run(src)
            labels(3) = redCore.labels(3)

            Dim rcListLast As New List(Of rcData)(rcList)
            Dim rcMapLast As cv.Mat = rcMap.Clone

            rcList.Clear()
            rcMap.SetTo(0)
            dst2.SetTo(0)
            Dim matchCount As Integer
            Dim unMatched As Integer
            Dim matchAverage As Single
            Dim blackVec As New cv.Vec3b
            For Each rc In redCore.rcList
                rc = RedUtil_Basics.rcDataMatch(rc, rcListLast, rcMapLast)

                If rc.age = 1 Then unMatched += 1 Else matchCount += 1
                matchAverage += rc.age
                rc.index = rcList.Count + 1
                rcMap(rc.rect).SetTo(rc.index, rc.mask)

                rcList.Add(rc)

                If task.heartBeat Then
                    Dim color = keyColors.dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                    If color <> blackVec Then rc.color = color
                End If
                dst2(rc.rect).SetTo(rc.color, rc.mask)
            Next

            strOut = RedUtil_Basics.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)

            dst1 = keyColors.dst2

            labels(2) = CStr(unMatched) + " were new cells and " + CStr(matchCount) + " were matched, " +
                            "average age: " + Format(matchAverage / rcList.Count, fmt1)
            labels(3) = redCore.labels(3)
        End Sub
    End Class






    Public Class RedCloud_Core : Inherits TaskParent
        Public prepEdges As New RedPrep_Basics
        Public rcList As New List(Of rcData)
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Find the biggest chunks of consistent depth data "
        End Sub
        Public Shared Function sweepImage(input As cv.Mat) As List(Of rcData)
            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim mask = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8U, 0)
            '  Or cv.FloodFillFlags.MaskOnly - maskonly is expensive but why?
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4
            Dim minSize As Integer = input.Total * 0.0001
            Dim rc As rcData = Nothing
            Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            For y = 0 To input.Height - 1
                For x = 0 To input.Width - 1
                    Dim pt = New cv.Point(x, y)
                    ' skip the regions with no depth or those that were already floodfilled.
                    If input.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                        Dim count = cv.Cv2.FloodFill(input, mask, pt, index, rect, 0, 0, flags)
                        If rect.Width > 0 And rect.Height > 0 Then
                            If count >= minSize Then
                                rc = New rcData(input(rect), rect, index)
                                If rc.index < 0 Then Continue For
                                newList.Add(rc.pixels, rc)
                                index += 1
                                rc.index = newList.Count
                            End If
                        End If
                    End If
                    If index = 254 Then index = 1
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

            dst2.SetTo(0)
            For Each rc In rcList
                dst2(rc.rect).SetTo(rc.index Mod 254, rc.mask)
            Next
            dst3 = Palettize(dst2, 0)
            labels(2) = "RedCloud cells identified: " + CStr(rcList.Count)
        End Sub
    End Class




    Public Class RedCloud_BasicsFlood : Inherits TaskParent
        Dim prepXY As New RedPrep_XY_Add
        Public redC As New RedCloud_Flood_CPP
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat
        Public Sub New()
            task.fOptions.ReductionSlider.Value = 200
            desc = "Use the more complete RedPrep_XY_Add as input to RedCloud."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepXY.Run(src)
            prepXY.dst2.ConvertTo(dst2, cv.MatType.CV_8U)

            redC.Run(dst2)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            If task.rcD IsNot Nothing Then dst2.Rectangle(task.rcD.rect, task.highlight, task.lineWidth)
            If strOut <> "" Then SetTrueText(redC.strOut, 3) Else SetTrueText("Click on any cell", 3)

            Dim causeLabel = RedUtil_Basics.findCause(redC.rcMap, redC.rcList)
            If task.mouseClickFlag Then
                causeLabel = ""
                labels(3) = ""
            End If

            If causeLabel <> "" Then
                If labels(3) = "" Then labels(3) = causeLabel Else labels(3) += ", " + causeLabel
                If labels(3).Length > 80 Then labels(3) = causeLabel
            End If

            rcList = New List(Of rcData)(redC.rcList)
            rcMap = redC.rcMap.Clone
        End Sub
    End Class






    Public Class NR_RedCloud_Basics : Inherits TaskParent
        Public redC As New RedCloud_Flood_CPP
        Public rcList As New List(Of rcData)
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

            Dim causeLabel = RedUtil_Basics.findCause(redC.rcMap, redC.rcList)
            If task.mouseClickFlag Then
                causeLabel = ""
                labels(3) = ""
            End If

            If causeLabel <> "" Then
                If labels(3) = "" Then labels(3) = causeLabel Else labels(3) += ", " + causeLabel
                If labels(3).Length > 80 Then labels(3) = causeLabel
            End If

            rcList = New List(Of rcData)(redC.rcList)
            rcMap = redC.rcMap.Clone
        End Sub
    End Class








    Public Class NR_RedCloud_CellDepthHistogram : Inherits TaskParent
        Dim plot As New Plot_Histogram
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

            If task.rcD Is Nothing Then
                task.rcD = redC.rcList(0)
                task.clickPoint = task.rcD.maxDist
            End If

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
                vbc.DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
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
            For Each r As brickData In bricks.brickList
                If redC.rcMap(r.lRect).CountNonZero And r.rRect.Width > 0 Then
                    dst2(r.lRect).CopyTo(dst1(r.rRect))
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
        Public rcList As New List(Of rcData)
        Public Sub New()
            task.fOptions.ReductionSlider.Value = 120
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
        Public rcList As New List(Of rcData)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            task.fOptions.ReductionSlider.Value = 120
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






    Public Class NR_RedCloud_ColorChangeCause : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public Sub New()
            desc = "Click on a cell to determine why it is changing colors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            dst2.SetTo(0, task.noDepthMask)

            labels(3) = RedUtil_Basics.findCause(redC.rcMap, redC.rcList)
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
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public wGridList As New List(Of cv.Point3d)
        Public options As New Options_RedCloud
        Public keyColors As New KeyColor_Reduction_TA
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

            Dim rcListLast = New List(Of rcData)(rcList)
            Dim rcMapLast As cv.Mat = rcMap.Clone

            Dim index As Integer = 1
            Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            For i = 0 To rects.Count - 1
                Dim rc = New rcData(dst0(rects(i)), rects(i), index)
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
            For Each rc In newList.Values
                Dim maxDist = rc.maxDist
                If maxDist = New cv.Point(315, 19) Then Dim k = 0
                rc = RedUtil_Basics.rcMatch(rc, rcListLast, wGridList, rcMapLast)

                If rc.age = 1 Then unMatched += 1 Else matchCount += 1
                matchAverage += rc.age

                rc.index = rcList.Count + 1

                If task.heartBeat Then
                    Dim color = keyColors.dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                    If color <> blackVec Then rc.color = color
                End If

                rcList.Add(rc)

                If rc.pixels < 5 Then
                    ' when the cell is very small, pixels can be zero - buildcontours failed.  No mask.
                    dst2(rc.rect).SetTo(rc.color)
                    rcMap(rc.rect).SetTo(rc.index)
                Else
                    dst2(rc.rect).SetTo(rc.color, rc.mask)
                    rcMap(rc.rect).SetTo(rc.index, rc.mask)
                End If
            Next

            strOut = RedUtil_Basics.selectCell(rcMap, rcList)
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





    Public Class RedCloud_FeatureLess : Inherits TaskParent
        Dim redC As New FeatureLess_RedColor
        Public Sub New()
            desc = "Use the FeatureLess_Basics output as input to RedCloud - identical to FeatureLess_RedColor (now)"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(task.gray)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            SetTrueText(redC.strOut, 3)
        End Sub
    End Class
End Namespace
