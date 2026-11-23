Imports OpenCvSharp :
Imports OpenCvSharp.Cv2 :
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedColor_Basics : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcIndex As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public floodMask As cv.Mat
        Public Sub New()
            desc = "FloodFill cells in the Color8U input and track them if above a minimum size."
        End Sub
        Public Shared Function displayClick(rcList As List(Of rcData), rcMap As cv.Mat) As String
            Dim rctmp = RedCC_Basics.rcFinder(rcList, rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X))
            If rctmp IsNot Nothing Then
                task.clickPoint = rctmp.maxDist
                task.rcD = rctmp
                ' Rectangle(task.color, task.rcD.rect, task.highlight, task.lineWidth)
                task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
                Return task.rcD.displayCell()
            Else
                If rcList.Count > 1 Then task.rcD = rcList(1)
            End If
            Return ""
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.color8U.dst2

            Dim rc As rcData, rcNext As rcData
            Dim rcLastList = New List(Of rcData)(rcList)

            Dim mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            If floodMask IsNot Nothing Then floodMask.CopyTo(mask(task.maskRect))
            Dim rect As cv.Rect
            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim index As Integer = 1
            For y = 0 To dst2.Height - 1 Step task.gridWH
                For x = 0 To dst2.Width - 1 Step task.gridWH
                    Dim pt = New cv.Point(x, y)
                    If mask.Get(Of Byte)(y, x) = 0 Then

                        Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                        Dim count = FloodFill(src, mask, pt, index, rect, 0, 0, flags)

                        If count > task.minCellSize Then
                            rc = New rcData(mask(rect).Clone, rect, index) With {.index = index}

                            Dim ID = rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                            If ID > 0 Then
                                mask(rect).SetTo(ID, rc.mask)
                                rc.ID = ID
                            End If
                            sortList.Add(rc.pixels, rc)
                            index += 1
                        Else
                            If count > 0 Then mask(rect).SetTo(255, mask(rect))
                        End If
                    End If
                Next
            Next

            mask = mask(task.maskRect)
            If floodMask IsNot Nothing Then mask.SetTo(0, floodMask)
            dst3 = mask.Clone

            Dim ranges() As Rangef = {New Rangef(0, task.delaunayFLess.ptList.Count)}
            Dim histogram As New Mat
            Dim histArray() As Single = Nothing
            Dim colorList As New List(Of rcData)
            Dim matches As Integer
            With task.delaunayFLess
                For Each rc In sortList.Values
                    Dim motionFlag = task.motion.motionMask.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                    Dim ID = rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                    If ID = 0 Or motionFlag <> 0 Then
                        CalcHist({ .rcMap(rc.rect)}, {0}, rc.mask, histogram, 1, { .ptList.Count}, ranges)
                        histogram.GetArray(Of Single)(histArray)
                        Dim hList = histArray.ToList
                        rc.ID = hList.IndexOf(hList.Max) - 1
                    Else
                        Dim lastIndex = rcIndex.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                        Dim rcLast = rcLastList(lastIndex)
                        Dim lastID = rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                        If rcLast.ID = lastID Then
                            rc.age = rcLast.age + 1
                            rc.maxDStable = rcLast.maxDStable
                            rc.ID = ID
                            matches += 1
                        End If
                    End If
                    If rc.ID >= 0 Then colorList.Add(rc)
                Next
            End With

            rcMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData) ' placeholder for zero...
            For i = 0 To colorList.Count - 1
                rc = colorList(i)
                If rc.ID < 0 Then Continue For
                For j = i + 1 To colorList.Count - 1
                    rcNext = colorList(j)
                    If rc.ID = rcNext.ID Then
                        Dim r = ValidateRect(rc.rect.Union(rcNext.rect))
                        mask(r).SetTo(0)
                        mask(rc.rect).SetTo(255, rc.mask)
                        mask(rcNext.rect).SetTo(255, rcNext.mask)
                        rc = New rcData(mask(r), r, 255) With {.ID = rcNext.ID}
                        rcNext.ID = -1
                        rc.age = colorList(i).age
                        colorList(j) = rcNext
                    End If
                Next
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                rc.index = rcList.Count
                rcIndex(rc.rect).SetTo(rc.index, rc.mask)
                SetTrueText(CStr(rc.ID), rc.maxDStable, 2)
                rcList.Add(rc)
            Next

            dst2 = ApplyPal(rcMap, 0)

            If standalone Then SetTrueText(RedColor_Basics.displayClick(rcList, rcMap), 3)

            labels(2) = CStr(sortList.Count) + " cells merged into " + CStr(rcList.Count) + ". " + CStr(matches) +
                        " were matched to previous frame.  Click any cell to see details in dst3."
            If rcList.Count > 255 Then
                SetTrueText("Flood_RectMats needs to increase the minimum cell size - too many to fit in CV_8U!", 3)
            End If
        End Sub
    End Class





    Public Class XR_RedColor_FloodToID : Inherits TaskParent
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcList As New List(Of rcData) ' includes cloud data.
        Dim flood As New Flood_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Build an rcList from the flood data and present smallest to largest."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastMap = rcMap.Clone
            If src.Channels <> 1 Then src = task.color8U.dst2.Clone

            flood.Run(src)
            dst2 = flood.dst2
            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            For i = 0 To flood.rectList.Count - 1
                Dim floodVal = flood.indexList(i)
                Dim r = flood.rectList(i)
                Dim rc As New rcData(flood.mask(r), r, floodVal)
                sortList.Add(rc.pixels, rc)
            Next

            rcList.Clear()
            rcList.Add(New rcData())
            rcMap.SetTo(0)
            For Each rc In sortList.Values
                rc.ID = rcList.Count
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                rcList.Add(rc)
            Next

            SetTrueText(RedCC_Core.displayCell(rcList, rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)), 1)

            For i = 0 To rcList.Count - 1
                Dim rc = rcList(i)
                rc.ID = i
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
            Next
            labels(2) = CStr(sortList.Values.Count) + " cells were found "

            If task.rcD IsNot Nothing Then
                Circle(dst2, task.rcD.maxDist, task.DotSize + 1, white, -1)
                Circle(dst2, task.rcD.maxDStable, task.DotSize + 1, black, -1)
                ' Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            End If

            dst3 = ApplyPal(rcMap, 0)
            labels(3) = CStr(rcList.Count - 1) + " cells found reordered and assigned index."
        End Sub
    End Class





    Public Class XR_RedColor_GridRects : Inherits TaskParent
        Public rcGridMap As New Mat(dst2.Size, MatType.CV_8U, 0) ' map of rc data to grid map
        Public Sub New()
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            labels(3) = "RedColor output mapped into the gridRects."
            desc = "Create a triangle representation of the cv.Point cloud with RedCloud data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            task.redColor.Run(src)
            dst2 = task.redColor.dst2
            labels(2) = task.redColor.labels(2)

            rcGridMap.SetTo(0)
            dst3.SetTo(0)
            For i = 0 To task.gridRects.Count - 1
                Dim r = task.gridRects(i)

                Dim center = New cv.Point(CInt(r.X + r.Width / 2), CInt(r.Y + r.Height / 2))
                Dim index As Integer = task.redColor.rcMap.Get(Of Byte)(center.Y, center.X) - 1
                If index >= task.redColor.rcList.Count Or index < 0 Then Continue For
                Dim rc = task.redColor.rcList(index)
                dst3(r).SetTo(task.scalarColors(rc.ID Mod 255))
                rcGridMap(r).SetTo(rc.ID)
            Next
        End Sub
    End Class





    Public Class XR_RedColor_InRange : Inherits TaskParent
        Public inputRemoved As New Mat
        Public showSelected As Boolean = True
        Public Sub New()
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            labels(3) = "The inputRemoved mask is used to limit how much of the image is processed."
            desc = "Use InRange to prepare an 8U input image for RedColor_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            InRange(task.pcSplit(2), task.MaxZmeters, 1000, inputRemoved)
            ConvertScaleAbs(inputRemoved, inputRemoved)
            src = task.color8U.dst2

            src.SetTo(0, inputRemoved)

            task.redColor.Run(src)
            labels(2) = task.redColor.labels(2)
            dst2 = task.redColor.dst2.SetTo(0, inputRemoved)

            labels(2) = $"{task.redColor.rcList.Count} cells identified"

            If showSelected Then SetTrueText(task.redColor.strOut, 3)
        End Sub
    End Class





    Public Class XR_RedColor_Flippers : Inherits TaskParent
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            desc = "Find the cells which are flipping."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastList As New List(Of rcData)(task.redColor.rcList)
            dst2 = task.redColor.dst2
            labels(2) = task.redColor.labels(2)

            Dim usedList As New List(Of Byte)
            For Each rc In task.redColor.rcList
                If rc.index = 0 Then Continue For
                Circle(dst2, rc.maxDist, task.DotSize, task.highlight, -1)
                If usedList.Contains(rc.ID) = False Then usedList.Add(rc.ID)
            Next

            dst1.SetTo(0)
            For Each rc In task.redColor.rcList
                If rc.ID = 255 Then
                    dst1(rc.rect).SetTo(rc.ID, rc.mask)
                End If
            Next
            dst3 = ApplyPal(dst1, 0)
        End Sub
    End Class




    Public Class XR_RedColor_Missing : Inherits TaskParent
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            desc = "Find the cells which are missing."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastList As New List(Of rcData)(task.redColor.rcList)
            dst2 = task.redColor.dst2
            dst3 = task.delaunayFLess.dst2
            labels(2) = task.redColor.labels(2)

            Dim usedList As New SortedList(Of Integer, Byte)(New compareAllowIdenticalInteger)
            For Each rc In task.redColor.rcList
                If usedList.ContainsKey(rc.ID) = False Then usedList.Add(rc.ID, rc.index)
            Next

            dst0.SetTo(0)
            Dim lastIndex As Integer = -1
            For i = 0 To usedList.Keys.Count - 1
                If usedList.Keys(i) <> lastIndex + 1 Then
                    Dim rc = task.redColor.rcList(usedList.Values(i))
                    dst0(rc.rect).SetTo(rc.ID, rc.mask)
                    dst2(rc.rect).SetTo(rc.ID, rc.mask)
                End If
                lastIndex = usedList.Keys(i)
            Next

            dst1 = ApplyPal(dst0, 0)
        End Sub
    End Class




    Public Class XR_RedColor_FLessBasics : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(3) = "The output of FeatureLess_Basics.  Note that cell colors match the RedColor output."
            desc = "Use the FeatureLess regions to improve the RedColor output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.color8U.dst2.Clone

            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim rectSorted As New SortedList(Of Integer, (count As Integer, r As cv.Rect))(New compareAllowIdenticalInteger)
            For Each r In task.fless.fLessBricks
                Dim val = mask(r).Get(Of Byte)(0, 0)
                If val = 0 Then
                    Dim index As Integer = task.fless.dst1(r).Get(Of Byte)(0, 0)
                    If index > 0 Then
                        Dim flags = FloodFillFlags.FixedRange Or FloodFillFlags.Link4 Or (index << 8)
                        Dim count = FloodFill(src, mask, r.TopLeft, index, rect, 0, 0, flags)
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
                            Dim rc As New rcData(src(r1), r1, rectSorted.ElementAt(j).Key)
                            rcSizeSort.Add(rectSorted.ElementAt(i).Value.count, rc)
                            i = j
                            Exit For
                        End If
                    Next
                Else
                    Dim rc As New rcData(src(r1), r1, rectSorted.ElementAt(i).Key)
                    rcSizeSort.Add(rectSorted.ElementAt(i).Value.count, rc)
                End If
            Next

            rcMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData) ' placeholder for zero...
            For Each rc In rcSizeSort.Values
                rc.ID = rcList.Count
                rcList.Add(rc)
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
            Next

            dst2 = ApplyPal(rcMap, 0)
            dst3 = task.fless.dst2

            labels(2) = CStr(rcList.Count) + " cells were identified."
        End Sub
    End Class





    Public Class RedColor_MapHoles : Inherits TaskParent
        Public Sub New()
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            desc = "Display the holes in the RedColor rcMap output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = task.redColor.dst2
            labels(3) = task.redColor.labels(2)
            Threshold(task.redColor.rcMap, dst2, 0, 255, ThresholdTypes.BinaryInv)
        End Sub
    End Class






    Public Class RedColor_CellHistogram : Inherits TaskParent
        Dim redColor As New RedColor_Basics
        Public Sub New()
            desc = "Display the histogram of the RedCloud cells."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If heartbeatLTX() Then
                redColor.Run(src)

                Dim depths As New List(Of Single)
                For Each rc In redColor.rcList
                    depths.Add(rc.depthAvg)
                Next

                Dim maxval = depths.Max
                Static maxDepth = Math.Ceiling(maxval)
                maxDepth = Math.Ceiling(maxval)
                For Each rc In redColor.rcList
                    rc.ID = Math.Floor(task.histogramBins * rc.depthAvg / maxDepth) + 1
                Next

                redColor.rcMap.SetTo(0)
                For Each rc In redColor.rcList
                    If rc.contour.Count = 0 Then Continue For
                    DrawTour(redColor.rcMap(rc.rect), rc.contoursEx, New cv.Scalar(rc.ID), -1)
                    SetTrueText(CStr(rc.index), rc.maxDist)
                Next

                dst2 = ApplyPal(redColor.rcMap, 0)
            End If
        End Sub
    End Class





    Public Class RedColor_DepthBoundaries : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Dim redPrep As New RedPrep_Basics
        Dim redNoDepth As New RedColor_NoDepth
        Public Sub New()
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            desc = "Limit the floodfill of colors with the depth boundaries."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redPrep.Run(src)

            dst3 = redPrep.dst2
            dst1 = Not dst3
            redNoDepth.Run(dst1)

            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            redNoDepth.rcList.RemoveAt(0) ' remove the placeholder - it is copied in below.
            For Each rc In redNoDepth.rcList
                sortList.Add(rc.pixels, rc)
            Next

            For Each rc In task.redColor.rcList
                sortList.Add(rc.pixels, rc)

            Next

            rcList.Clear()

            For Each rc In sortList.Values
                rc.ID = rcList.Count
                rc.index = rcList.Count
                rcList.Add(rc)
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
            Next

            dst2 = ApplyPal(rcMap, 0)

            labels(2) = CStr(rcList.Count) + " cells found from " + CStr(redNoDepth.rcList.Count) + " and " +
                        CStr(task.redColor.rcList.Count) + " cells."
        End Sub
    End Class





    Public Class RedColor_NoDepth : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Dim redColor As New RedColor_Basics
        Public Sub New()
            task.gOptions.stableDepthRGB.Checked = True
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Limit the floodfill of colors to areas with no depth data."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redColor.floodMask = task.depthmask
            redColor.Run(task.color8U.dst2)
            rcList = New List(Of rcData)(redColor.rcList)
            rcMap = redColor.rcMap
            dst2 = redColor.dst2
            labels(2) = CStr(rcList.Count) + " cells were found where there was no depth."
        End Sub
    End Class






    Public Class RedColor_Phase1 : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
        Public Sub New()
            labels(3) = "CV_8U version of dst2."
            desc = "FloodFill cells in the Color8U input and track them if above a minimum size."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.color8U.dst2

            Dim rect As cv.Rect
            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim index As Integer = 1
            mask.SetTo(0)
            For y = 0 To dst2.Height - 1 Step task.gridWH
                For x = 0 To dst2.Width - 1 Step task.gridWH
                    Dim pt = New cv.Point(x, y)
                    If mask.Get(Of Byte)(y, x) = 0 Then

                        Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                        Dim count = FloodFill(src, mask, pt, index, rect, 0, 0, flags)

                        If count > task.minCellSize Then
                            Dim rc = New rcData(src(rect), rect, index) With {.ID = index, .index = index}
                            Dim ID = task.delaunayFLess.rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                            mask(rect).SetTo(ID, rc.mask)
                            rc.ID = ID
                            sortList.Add(rc.pixels, rc)
                            index += 1
                        End If
                    End If
                Next
            Next

            dst3 = mask(task.maskRect).Clone
            rcList = New List(Of rcData)(sortList.Values)
            rcMap = src.Clone

            If standaloneTest() Then
                dst2 = ApplyPal(dst3, 0)
                For Each rc In rcList
                    SetTrueText(CStr(rc.ID), rc.maxDist, 2)
                    SetTrueText(CStr(rc.ID), rc.maxDist, 3)
                Next
            End If

            labels(2) = CStr(sortList.Count) + " cells found in phase 1 of isolating RedColor cells."
            If rcList.Count > 255 Then
                SetTrueText("Flood_RectMats needs to increase the minimum cell size - too many to fit in CV_8U!", 3)
            End If
        End Sub
    End Class





    Public Class RedColor_Phase2 : Inherits TaskParent
        Public inputList As New List(Of rcData)
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcIndex As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public Sub New()
            desc = "FloodFill cells in the Color8U input and track them if above a minimum size."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static redC1 As New RedColor_Phase1
                redC1.Run(src)
                inputList = redC1.rcList
            End If

            Dim rcLastList = New List(Of rcData)(rcList)
            Dim rcMapLast = rcMap.Clone
            Dim rcIndexLast = rcIndex.Clone

            Dim ranges() As Rangef = {New Rangef(0, task.delaunayFLess.ptList.Count)}
            Dim histogram As New Mat
            Dim histArray() As Single = Nothing
            Dim matches As Integer
            rcList.Clear()
            rcMap.SetTo(0)
            rcIndex.SetTo(0)
            With task.delaunayFLess
                For Each rc In inputList
                    Dim motionFlag = task.motion.motionMask.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) > 0
                    Dim ID = .rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                    If ID = 0 Or motionFlag Then
                        CalcHist({ .rcMap(rc.rect)}, {0}, rc.mask, histogram, 1, { .ptList.Count}, ranges)
                        histogram.GetArray(Of Single)(histArray)
                        Dim hList = histArray.ToList
                        rc.ID = hList.IndexOf(hList.Max) - 1
                    Else
                        Dim lastIndex = rcIndexLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                        Dim rcLast = rcLastList(lastIndex)
                        Dim lastID = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                        If rcLast.ID = lastID Then
                            rc.age = rcLast.age + 1
                            rc.maxDStable = rcLast.maxDStable
                            rc.ID = ID
                            matches += 1
                        End If
                    End If
                    If rc.ID >= 0 Then
                        rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                        rc.index = rcList.Count
                        rcIndex(rc.rect).SetTo(rc.index, rc.mask)
                        SetTrueText(CStr(rc.ID), rc.maxDStable, 2)
                        rcList.Add(rc)
                    End If
                Next
            End With


            If standaloneTest() Then
                dst2 = ApplyPal(rcMap, 0)
                For Each rc In rcList
                    SetTrueText(CStr(rc.ID), rc.maxDist, 2)
                    SetTrueText(CStr(rc.ID), rc.maxDist, 3)
                Next
            End If

            labels(2) = CStr(inputList.Count) + " cells merged into " + CStr(rcList.Count) + " cells. Click any cell to see details."
            If rcList.Count >= 255 Then
                SetTrueText("Flood_RectMats needs to increase the minimum cell size - too many to fit in CV_8U!", 3)
            End If
        End Sub
    End Class






    Public Class RedColor_Phase3 : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcIndex As New Mat(dst2.Size, MatType.CV_8U, 0)
        Dim redC1 As New RedColor_Phase1
        Dim redC2 As New RedColor_Phase2
        Public Sub New()
            desc = "FloodFill cells in the Color8U input and track them if above a minimum size."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.color8U.dst2

            redC1.Run(src)
            Dim mask = redC1.mask(task.maskRect).Clone

            redC2.inputList = New List(Of rcData)(redC1.rcList)
            redC2.Run(src)

            Dim rc As rcData, rcNext As rcData

            rcMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData) ' placeholder for zero...
            Dim rcListC2 As New List(Of rcData)(redC2.rcList)
            For i = 0 To rcListC2.Count - 1
                rc = rcListC2(i)
                If rc.ID < 0 Then Continue For
                For j = i + 1 To rcListC2.Count - 1
                    rcNext = rcListC2(j)
                    If rc.ID = rcNext.ID Then
                        Dim r = ValidateRect(rc.rect.Union(rcNext.rect))
                        mask(r).SetTo(0)
                        mask(rc.rect).SetTo(255, rc.mask)
                        mask(rcNext.rect).SetTo(255, rcNext.mask)
                        rc = New rcData(mask(r), r, 255) With {.ID = rcNext.ID}
                        rcNext.ID = -1
                        rc.age = rcListC2(i).age
                        rcListC2(j) = rcNext
                    End If
                Next
                rc.index = rcList.Count
                rcIndex(rc.rect).SetTo(rc.index, rc.mask)
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                SetTrueText(CStr(rc.index), rc.maxDStable, 2)
                rcList.Add(rc)
            Next

            dst2 = ApplyPal(rcMap, 0)

            If standalone Then SetTrueText(RedColor_Basics.displayClick(rcList, rcMap), 3)

            labels(2) = CStr(rcListC2.Count) + " cells merged into " + CStr(rcList.Count) + ". "
            If rcList.Count > 255 Then
                SetTrueText("Flood_RectMats needs to increase the minimum cell size - too many to fit in CV_8U!", 3)
            End If
        End Sub
    End Class
End Namespace