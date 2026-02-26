Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Enum causes
        indexLastGood
        indexLastBelowZero
        indexLastAboveCount
        intersectLastRectFailed
        optionsChange
        maxDistOutsideOfLastRect
    End Enum
    Public Class RedCloud_Basics : Inherits TaskParent
        Public indexer As New Indexer_Basics
        Public redC As New RedCloud_FloodFill
        Dim element As New cv.Mat
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat
        Public Sub New()
            element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(3, 3))
            desc = "Assign abstract world coordinates to each cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            indexer.Run(src)
            dst1 = indexer.dst3.Dilate(element, Nothing, 1)
            dst0 = Not indexer.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
            dst1.SetTo(255, dst0)

            redC.Run(dst1)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            dst2.SetTo(0, dst1)
            redC.rcMap.SetTo(0, dst1)

            strOut = RedCloud_Cell.selectCell(redC.rcMap, redC.rcList)
            If task.rcD IsNot Nothing Then dst2.Rectangle(task.rcD.rect, task.highlight, task.lineWidth)
            If strOut <> "" Then SetTrueText(strOut, 3) Else SetTrueText("Click on any cell", 3)

            Dim causeLabel = RedCloud_ColorChangeCause.findCause(redC.rcMap, redC.rcList)

            If causeLabel <> "" Then
                If labels(3) = "" Then labels(3) = causeLabel Else labels(3) += ", " + causeLabel
                If labels(3).Length > 80 Then labels(3) = causeLabel
            End If

            rcList = New List(Of rcData)(redC.rcList)
            rcMap = redC.rcMap.Clone
        End Sub
    End Class






    Public Class RedCloud_PrepEdges : Inherits TaskParent
        Public redCore As New RedCloud_Core
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public percentImage As Single
        Public options As New Options_RedCloud
        Public Sub New()
            task.redCloud = Me
            desc = "Build contours for each cell"
        End Sub
        Public Shared Function rcDataMatch(rc As rcData, rcListLast As List(Of rcData),
                                           rcMapLast As cv.Mat) As rcData
            Dim r1 = rc.rect
            Dim indexLast = rcMapLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)

            If indexLast = 0 Then
                rc.colorChange = causes.indexLastBelowZero
            Else
                If indexLast < rcListLast.Count Then
                    ' rcList index is 1 less than the rcMap value because a 0 rcMap value means not mapped.
                    ' All pixels are mapped with color but withh depth, rcMap has 0's where there is no depth.
                    indexLast -= 1
                    Dim r2 = rcListLast(indexLast).rect
                    If r1.IntersectsWith(r2) = False Then rc.colorChange = causes.intersectLastRectFailed
                Else
                    rc.colorChange = causes.indexLastAboveCount
                End If
            End If

            If task.optionsChanged Then rc.colorChange = causes.optionsChange

            If rc.colorChange = 0 And task.optionsChanged = False Then
                Dim lrc = rcListLast(indexLast)
                Dim rTest = rc.rect.Intersect(lrc.rect)
                Dim rTotal = rTest.Width * rTest.Height
                Dim lastTotal = lrc.rect.Width * lrc.rect.Height
                If rc.rect.Contains(lrc.maxDist) Then
                    rc.maxDist = lrc.maxDist
                    rc.depthDelta = Math.Abs(lrc.wcMean(2) - rc.wcMean(2))
                    If Single.IsInfinity(rc.depthDelta) Or rc.depthDelta < 0 Then
                        rc.depthDelta = 0
                        rc.wcMean(2) = 0
                    End If
                Else
                    rc.colorChange = causes.maxDistOutsideOfLastRect
                End If

                rc.age = lrc.age + 1
                If rc.age > 1000 Then rc.age = 2

                rc.color = lrc.color
            End If
            Return rc
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

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
            For Each rc In redCore.rcList
                rc = rcDataMatch(rc, rcListLast, rcMapLast)

                If rc.age = 1 Then unMatched += 1 Else matchCount += 1
                matchAverage += rc.age
                rc.index = rcList.Count + 1
                rcMap(rc.rect).SetTo(rc.index, rc.mask)

                rcList.Add(rc)

                dst2(rc.rect).SetTo(rc.color, rc.mask)
            Next

            strOut = RedCloud_Cell.selectCell(rcMap, rcList)
            SetTrueText(strOut, 3)

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
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
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
                dst2(rc.rect).SetTo(rc.index, rc.mask)
            Next
            dst3 = PaletteBlackZero(dst2)
            labels(2) = "RedCloud cells identified: " + CStr(rcList.Count)
        End Sub
    End Class







    Public Class NR_RedCloud_CellDepthHistogram : Inherits TaskParent
        Dim plot As New Plot_Histogram
        Public Sub New()
            task.gOptions.setHistogramBins(100)
            If standalone Then task.gOptions.displayDst1.Checked = True
            plot.createHistogram = True
            desc = "Display the histogram of a selected RedCloud cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))

            strOut = RedCloud_Cell.selectCell(task.redCloud.rcMap, task.redCloud.rcList)
            SetTrueText(strOut, 1)

            labels(3) = "Select a RedCloud cell to see the histogram"

            Dim depth As cv.Mat = task.pcSplit(2)(task.rcD.rect)
            depth.SetTo(0, task.noDepthMask(task.rcD.rect))
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





    Public Class RedCloud_CellMask : Inherits TaskParent
        Dim redMotion As New XO_RedCloud_Motion
        Public Sub New()
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Create a mask that outlines all the RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redMotion.Run(src)

            dst3.SetTo(0)
            For Each rc In task.redCloud.rcList
                Dim listOfPoints = New List(Of List(Of cv.Point))({rc.contour})
                cv.Cv2.DrawContours(dst3(rc.rect), listOfPoints, 0, white, task.lineWidth, cv.LineTypes.Link8)
            Next

            dst2 = task.redCloud.redCore.dst1
        End Sub
    End Class




    Public Class RedCloud_LeftRight : Inherits TaskParent
        Public Sub New()
            If task.bricks Is Nothing Then task.bricks = New Brick_Basics
            desc = "Map the RedCloud output into the right view."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = runRedCloud(src, labels(2))

            Dim count As Integer
            dst1.SetTo(0)
            For Each gr As brickData In task.bricks.brickList
                If task.redCloud.rcMap(gr.lRect).CountNonZero And gr.rRect.Width > 0 Then
                    dst2(gr.lRect).CopyTo(dst1(gr.rRect))
                    count += 1
                End If
            Next

            dst3 = ShowAddweighted(dst1, task.rightView, labels(3))
            labels(3) += " " + CStr(count) + " bricks mapped into the right image."
        End Sub
    End Class





    Public Class RedCloud_KNN : Inherits TaskParent
        Dim redC As New RedCloud_PrepEdges
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
        Dim redC As New RedCloud_PrepEdges
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
        Dim redC As New RedCloud_PrepEdges
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

            strOut = RedCloud_Cell.selectCell(redC.rcMap, redC.rcList)
            If task.rcD IsNot Nothing Then dst2.Rectangle(task.rcD.rect, task.highlight, task.lineWidth)
            SetTrueText(strOut, 3)
            labels(3) = CStr(rcList.Count) + " matched cells below with > " + CStr(redC.options.ageThreshold) + " age"
        End Sub
    End Class





    Public Class RedCloud_Matched : Inherits TaskParent
        Dim redC As New RedCloud_PrepEdges
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

            strOut = RedCloud_Cell.selectCell(redC.rcMap, redC.rcList)
            If task.rcD IsNot Nothing Then dst2.Rectangle(task.rcD.rect, task.highlight, task.lineWidth)
            SetTrueText(strOut, 1)
            labels(3) = CStr(rcList.Count) + " matched cells below with > " + CStr(redC.options.ageThreshold) + " age"
        End Sub
    End Class






    Public Class RedCloud_ColorChangeCause : Inherits TaskParent
        Dim redC As New RedWC_BasicsOld
        Public Sub New()
            desc = "Click on a cell to determine why it is changing colors."
        End Sub
        Public Shared Function findCause(rcMap As cv.Mat, rcList As List(Of rcData)) As String
            Dim clickIndex = rcMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            findCause = ""
            If clickIndex > 0 Then
                Dim rc = rcList(clickIndex - 1)
                Select Case rc.colorChange
                    Case causes.indexLastBelowZero
                        findCause = "indexLast = 0"
                    Case causes.indexLastAboveCount
                        findCause = "last index >= last rclist"
                    Case causes.intersectLastRectFailed
                        findCause = "Current/Last don't intersect"
                    Case causes.optionsChange
                        findCause = "task options changed"
                    Case causes.maxDistOutsideOfLastRect
                        findCause = "maxDist outside last rect"
                End Select
            End If
            Return findCause
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            dst2.SetTo(0, task.noDepthMask)

            labels(3) = findCause(redC.redC.rcMap, redC.rcList)
        End Sub
    End Class





    Public Class RedCloud_Cell : Inherits TaskParent
        Public Sub New()
            desc = "Display the output of a RedCloud cell."
        End Sub
        Public Shared Function selectCell(rcMap As cv.Mat, rcList As List(Of rcData)) As String
            Dim clickIndex As Integer = 0, strOut As String = ""
            If rcList.Count > 0 Then
                clickIndex = rcMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
                If clickIndex > 0 And clickIndex < rcList.Count Then
                    task.rcD = rcList(clickIndex - 1)
                    strOut = task.rcD.displayCell()
                    task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
                    task.color.Rectangle(task.rcD.rect, task.highlight, task.lineWidth)
                Else
                    If task.rcD IsNot Nothing Then task.rcD = Nothing
                End If
            End If
            Return strOut
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then dst2 = runRedCloud(src, labels(2))

            selectCell(task.redCloud.rcMap, task.redCloud.rcList)
            SetTrueText(strOut, 3)
        End Sub
    End Class





    Public Class RedCloud_FloodFill : Inherits TaskParent
        Implements IDisposable
        Public classCount As Integer
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public options As New Options_RedCloud
        Public Sub New()
            cPtr = RedCloud_Open()
            desc = "Run the C++ RedCloud interface without a mask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Channels <> 1 Then
                Static prepData As New RedPrep_Core
                prepData.Run(src)
                dst1 = prepData.dst2
            Else
                dst1 = src
            End If

            Dim imagePtr As IntPtr
            Dim inputData(dst1.Total - 1) As Byte
            dst1.GetArray(Of Byte)(inputData)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleInput.Free()
            dst0 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone

            classCount = RedCloud_Count(cPtr)
            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))
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

            rcList.Clear()
            dst2.SetTo(0)
            Dim changed As Integer
            Dim usedColor As New List(Of cv.Scalar)
            Dim matchCount As Integer
            Dim unMatched As Integer
            Dim matchAverage As Single
            For Each rc In newList.Values
                Dim maxDist = rc.maxDist
                rc = RedCloud_PrepEdges.rcDataMatch(rc, rcListLast, rcMapLast)

                If rc.age = 1 Then unMatched += 1 Else matchCount += 1
                matchAverage += rc.age

                rc.index = rcList.Count + 1

                ' The first cell often contains other cells completely within it.
                ' These often causes the maxdist to move around.
                ' So just fix the color here and create a stable image.
                ' The cells within the largest cell will switch colors but many cells are stable.
                If rc.index = 1 Then rc.color = blue
                If maxDist <> rc.maxDist Then changed += 1

                If usedColor.Contains(rc.color) Then
                    rc.color = Palette_Basics.randomCellColor()
                    rc.age = 1
                End If
                usedColor.Add(rc.color)

                rcList.Add(rc)

                dst2(rc.rect).SetTo(rc.color, rc.mask)
                rcMap(rc.rect).SetTo(rc.index, rc.mask)
            Next

            If standalone Then
                dst2.SetTo(0, task.noDepthMask)
                strOut = RedCloud_Cell.selectCell(rcMap, rcList)
                SetTrueText(strOut, 3)
            End If

            labels(2) = CStr(unMatched) + " were new cells and " + CStr(matchCount) + " were matched, " +
                            "average age: " + Format(matchAverage / rcList.Count, fmt1)
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
        End Sub
    End Class
End Namespace
