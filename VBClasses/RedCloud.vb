Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedCloud_Basics : Inherits TaskParent
        Public redSweep As New RedCloud_Sweep
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public percentImage As Single
        Public Sub New()
            taskA.redCloud = Me
            desc = "Build contours for each cell"
        End Sub
        Public Shared Function rcDataMatch(rc As rcData, rcListLast As List(Of rcData), rcMapLast As cv.Mat) As rcData
            Dim r1 = rc.rect
            Dim r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
            Dim indexLast = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)

            If indexLast > 0 And indexLast < rcListLast.Count Then
                indexLast -= 1 ' index is 1 less than the rcMap value
                r2 = rcListLast(indexLast).rect
            End If

            If indexLast >= 0 And indexLast < rcListLast.Count And r1.IntersectsWith(r2) And taskA.optionsChanged = False Then
                Dim lrc = rcListLast(indexLast)
                If rc.rect.Contains(lrc.maxDist) Then
                    Dim row = lrc.maxDist.Y - lrc.rect.Y
                    Dim col = lrc.maxDist.X - lrc.rect.X
                    If row < rc.mask.Height And col < rc.mask.Width And
                       row >= .0 And col >= 0 Then
                        If rc.mask.Get(Of Byte)(row, col) Then ' more doublechecking...
                            rc.maxDist = lrc.maxDist
                            rc.depth = lrc.depth
                        End If
                    End If
                End If

                rc.age = lrc.age + 1
                If rc.age > 1000 Then rc.age = 2

                rc.color = lrc.color
            End If
            Return rc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            redSweep.Run(src)
            labels(3) = redSweep.labels(3)
            labels(2) = redSweep.labels(2)

            Dim rcListLast As New List(Of rcData)(rcList)
            Dim rcMapLast As cv.Mat = rcMap.Clone

            rcList.Clear()
            rcMap.SetTo(0)
            dst2.SetTo(0)
            For Each rc In redSweep.rcList
                rc = rcDataMatch(rc, rcListLast, rcMapLast)

                rc.index = rcList.Count + 1
                rcMap(rc.rect).SetTo(rc.index, rc.mask)

                rcList.Add(rc)

                dst2(rc.rect).SetTo(rc.color, rc.mask)
                dst2.Circle(rc.maxDist, taskA.DotSize, taskA.highlight, -1)
                SetTrueText(CStr(rc.age), rc.maxDist)
            Next

            RedCloud_Cell.selectCell(rcMap, rcList)
            strOut = taskA.rcD.displayCell()
            SetTrueText(strOut, 3)
        End Sub
    End Class




    Public Class RedCloud_Sweep : Inherits TaskParent
        Public prepEdges As New RedPrep_Basics
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            desc = "Find the biggest chunks of consistent depth data "
        End Sub
        Public Shared Function sweepImage(input As cv.Mat) As List(Of rcData)
            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim mask = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
            Dim minSize As Integer = input.Total * 0.001
            Dim rc As rcData = Nothing
            Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim minCount As Integer
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
                            Else
                                minCount += 1
                            End If
                        End If
                    End If
                Next
            Next
            Return New List(Of rcData)(newList.Values)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            prepEdges.Run(src)
            dst3 = prepEdges.dst2.Clone

            rcList = sweepImage(dst3)

            Dim index As Integer
            dst2.SetTo(0)
            rcMap.SetTo(0)
            For Each rc In rcList
                index += 1
                rc.index = index
                rc.color = taskA.vecColors(rc.index Mod 255)
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                rcMap(rc.rect).SetTo(rc.index, rc.mask)
                dst2.Circle(rc.maxDist, taskA.DotSize, taskA.highlight, -1)
            Next

            labels(2) = "RedCloud cells identified: " + CStr(rcList.Count)

            Static unchanged As Integer
            If taskA.motionRGB.motionList.Count = 0 Then
                unchanged += 1
                labels(3) = "The rcMap was unchanged " + CStr(unchanged) + " times since last heartBeatLT"
            End If
            If taskA.heartBeatLT Then unchanged = 0
        End Sub
    End Class







    Public Class NR_RedCloud_CellDepthHistogram : Inherits TaskParent
        Dim plot As New Plot_Histogram
        Public Sub New()
            taskA.gOptions.setHistogramBins(100)
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            plot.createHistogram = True
            desc = "Display the histogram of a selected RedCloud cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))

            RedCloud_Cell.selectCell(taskA.redCloud.rcMap, taskA.redCloud.rcList)
            If taskA.rcD IsNot Nothing Then strOut = taskA.rcD.displayCell
            SetTrueText(strOut, 1)

            labels(3) = "Select a RedCloud cell to see the histogram"

            Dim depth As cv.Mat = taskA.pcSplit(2)(taskA.rcD.rect)
            depth.SetTo(0, taskA.noDepthMask(taskA.rcD.rect))
            plot.minRange = 0
            plot.maxRange = taskA.MaxZmeters
            plot.Run(depth)
            labels(3) = "0 meters to " + Format(taskA.MaxZmeters, fmt0) + " meters - vertical lines every meter"

            Dim incr = dst2.Width / taskA.MaxZmeters
            For i = 1 To CInt(taskA.MaxZmeters - 1)
                Dim x = incr * i
                vbc.DrawLine(dst3, New cv.Point(x, 0), New cv.Point(x, dst2.Height), cv.Scalar.White)
            Next
            dst3 = plot.dst2
        End Sub
    End Class





    Public Class RedCloud_CellMask : Inherits TaskParent
        Dim redMotion As New XO_RedCloud_Motion
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Create a mask that outlines all the RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redMotion.Run(src)

            dst3.SetTo(0)
            For Each rc In taskA.redCloud.rcList
                Dim listOfPoints = New List(Of List(Of cv.Point))({rc.contour})
                cv.Cv2.DrawContours(dst3(rc.rect), listOfPoints, 0, white, taskA.lineWidth, cv.LineTypes.Link8)
            Next

            dst2 = taskA.redCloud.redSweep.dst1
        End Sub
    End Class





    Public Class RedCloud_Cell : Inherits TaskParent
        Public Sub New()
            desc = "Display the output of a RedCloud cell."
        End Sub
        Public Shared Sub selectCell(rcMap As cv.Mat, rcList As List(Of rcData))
            If rcList.Count > 0 Then
                Dim clickIndex = rcMap.Get(Of Byte)(taskA.clickPoint.Y, taskA.clickPoint.X) - 1
                If clickIndex >= 0 And clickIndex < rcList.Count Then
                    taskA.rcD = rcList(clickIndex)
                Else
                    If taskA.rcD Is Nothing And rcList.Count > 0 Then
                        taskA.rcD = rcList(0)
                    Else
                        taskA.rcD = Nothing
                    End If
                End If
            End If
            If taskA.rcD Is Nothing Then
                ' placeholder rcData to avoid errors downstream.
                taskA.rcD = New rcData(New cv.Mat(New cv.Size(1, 1), cv.MatType.CV_8U, 255),
                                              New cv.Rect(0, 0, 1, 1), 0)
            End If
            If taskA.rcD.rect.Contains(taskA.clickPoint) Then
                taskA.color(taskA.rcD.rect).SetTo(white, taskA.rcD.mask)
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then dst2 = runRedCloud(src, labels(2))

            selectCell(taskA.redCloud.rcMap, taskA.redCloud.rcList)
            If taskA.rcD IsNot Nothing Then strOut = taskA.rcD.displayCell()
            SetTrueText(strOut, 3)
        End Sub
    End Class



    Public Class RedCloud_LeftRight : Inherits TaskParent
        Public Sub New()
            If taskA.bricks Is Nothing Then taskA.bricks = New Brick_Basics
            desc = "Map the RedCloud output into the right view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))

            Dim count As Integer
            dst1.SetTo(0)
            For Each gr As brickData In taskA.bricks.brickList
                If taskA.redCloud.rcMap(gr.lRect).CountNonZero And gr.rRect.Width > 0 Then
                    dst2(gr.lRect).CopyTo(dst1(gr.rRect))
                    count += 1
                End If
            Next

            dst3 = ShowAddweighted(dst1, taskA.rightView, labels(3))
            labels(3) += " " + CStr(count) + " bricks mapped into the right image."
        End Sub
    End Class
End Namespace