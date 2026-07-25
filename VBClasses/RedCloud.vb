Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedCloud_Basics : Inherits TaskParent
        Public redCore As New RedCloud_Core
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_32S, 0)
        Public options As New Options_RedCloud
        Dim reduction As New Reduction_BasicsParmInput
        Public runSelectCell As Boolean = True
        Public Sub New()
            reduction.reductionFactor = 50
            task.gOptions.stableDepthRGB.Checked = True
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

            Dim rcListLast As New List(Of rcData)(rcList)
            Dim rcMapLast As Mat = rcMap.Clone

            rcList.Clear()
            rcMap.SetTo(0)
            dst2.SetTo(0)
            Dim matchCount As Integer
            Dim unMatched As Integer
            Dim matchAverage As Single
            Dim blackVec As New Vec3b
            For Each rc In redCore.rcList
                rc = Utility_Basics.rcDataMatch(rc)

                If rc.age = 1 Then unMatched += 1 Else matchCount += 1
                matchAverage += rc.age
                rc.mapID = dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                rcMap(rc.rect).SetTo(rc.mapID, rc.mask)
                rc.index = rcList.Count
                rcList.Add(rc)

                dst2(rc.rect).SetTo(task.scalarColors(rc.mapID Mod 255), rc.mask)
            Next

            If runSelectCell Then
                strOut = Utility_Basics.selectCell(rcMap, rcList)
                SetTrueText(strOut, 3)
            End If

            labels(2) = CStr(unMatched) + " were new cells and " + CStr(matchCount) + " were matched, " +
                                "average age: " + (matchAverage / rcList.Count).ToString(fmt1)
            labels(3) = redCore.labels(3)
        End Sub
    End Class






    Public Class RedCloud_Core : Inherits TaskParent
        Public prepEdges As New RedPrep_Basics
        Public rcList As New List(Of rcData)
        Public Sub New()
            dst2 = New Mat(dst2.Size, MatType.CV_8U, 0)
            desc = "Find the biggest chunks of consistent depth data "
        End Sub
        Public Shared Function sweepImage(input As Mat, minSize As Integer) As List(Of rcData)
            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim mask = New Mat(New Size(input.Width + 2, input.Height + 2), MatType.CV_8U, 0)
            Dim flags As FloodFillFlags = FloodFillFlags.Link4
            Dim rc As rcData
            Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            For y = 0 To input.Height - 1
                For x = 0 To input.Width - 1
                    Dim pt = New cv.Point(x, y)
                    ' skip the regions with no depth or those that were already floodfilled.
                    If input.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                        Dim count = FloodFill(input, mask, pt, index, rect, 0, 0, flags)
                        If rect.Width > 0 And rect.Height > 0 Then
                            If count >= minSize Then
                                rc = New rcData(input(rect), rect, index)
                                newList.Add(rc.pixels, rc)
                                index += 1
                                rc.mapID = newList.Count
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

            rcList = sweepImage(src, src.Total * 0.0001)
            If rcList.Count = 0 Then
                rcList.Add(New rcData(src, New cv.Rect(0, 0, src.Width, src.Height), 1))
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
        Public rcList As New List(Of rcData)
        Public rcMap As Mat
        Public Sub New()
            desc = "Assign abstract world coordinates to each RedCloud cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            If task.rcD IsNot Nothing Then Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            If strOut <> "" Then SetTrueText(redC.strOut, 3) Else SetTrueText("Click on any cell", 3)

            Dim causeLabel = Utility_Basics.findCause(redC.rcMap)
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

            Dim depth As Mat = task.pcSplit(2)(task.rcD.rect)
            depth.SetTo(0, task.noDepthMask(task.rcD.rect))
            ImShow("depth", depth)
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
                    hullList.Add(New cv.Point(rc.rect.X + pt.X, rc.rect.Y + pt.Y))
                Next
                listOfPoints.Add(hullList)
                FillPoly(dst3, listOfPoints, task.scalarColors(rc.index Mod 255))
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
                    dst2(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
                    dst3(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
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

            labels(3) = Utility_Basics.findCause(redC.rcMap)
        End Sub
    End Class





    Public Class XR_RedCloud_MotionFilter : Inherits TaskParent
        Dim redC As New RedCloud_Basics
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_32S, 0)
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

            redC.rcMap.ConvertTo(dst0, MatType.CV_8U)
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
            For Each rc In redC.rcList
                If rc.age > 1 Then
                    If rcMotionCells.Contains(rc.mapID) = False Then
                        dst3(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
                        rcMap(rc.rect).SetTo(rc.mapID, rc.mask)
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






    'Public Class RedCloud_Foreground : Inherits TaskParent
    '    Dim redC As New RedCloud_Basics
    '    Public Sub New()
    '        desc = "Find and monitor the cells in the foreground"
    '    End Sub
    '    Public Overrides Sub RunAlg(src As cv.Mat)
    '        redC.Run(src)
    '        dst2 = redC.dst2
    '        labels(2) = redC.labels(2)

    '        dst3.SetTo(0)
    '        Dim count As Integer
    '        Dim maxDepth = task.foreground.splitValue
    '        For Each rc In redC.rcList
    '            If rc.wcMean(2) < maxDepth Then
    '                dst3(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
    '                count += 1
    '            End If
    '        Next
    '        labels(3) = CStr(count) + " RedCloud cells were in the foreground (< " + maxDepth.ToString(fmt1) + " meters)"
    '    End Sub
    'End Class
End Namespace