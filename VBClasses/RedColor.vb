Imports OpenCvSharp :
Imports OpenCvSharp.Cv2 :
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedColor_Basics : Inherits TaskParent
        Dim color8U As New Color8U_Basics
        Public rcList As New List(Of rcData)
        Public colorList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public Sub New()
            labels(3) = "Select any cell in dst2 to see details below."
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            desc = "FloodFill cells in the Color8U input and track them if above a minimum size."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                color8U.Run(task.gray)
                src = color8U.dst2
            End If

            Dim rc As rcData, rcNext As rcData

            Dim mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim rect As cv.Rect
            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim index As Integer = 1
            For y = 0 To dst2.Height - 1 Step task.gridWH
                For x = 0 To dst2.Width - 1 Step task.gridWH
                    Dim pt = New cv.Point(x, y)
                    If mask.Get(Of Byte)(y, x) = 0 Then
                        Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                        Dim count = FloodFill(src, mask, pt, index, rect, 0, 0, flags)
                        If count >= task.minCellSize Then
                            rc = New rcData(mask(rect), rect, index) With {.index = index}
                            sortList.Add(rc.pixels, rc)
                            index += 1
                        End If
                    End If
                Next
            Next

            Dim ranges() As Rangef = {New Rangef(0, task.delaunayFLess.ptList.Count)}
            Dim histogram As New Mat
            Dim histArray() As Single = Nothing
            colorList.Clear()
            With task.delaunayFLess
                For Each rc In sortList.Values
                    CalcHist({ .rcMap(rc.rect)}, {0}, rc.mask, histogram, 1, { .ptList.Count}, ranges)
                    histogram.GetArray(Of Single)(histArray)
                    Dim hList = histArray.ToList
                    rc.ID = hList.IndexOf(hList.Max)
                    If rc.ID >= 0 Then colorList.Add(rc)
                Next
            End With

            rcMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData) ' placeholder for zero...
            mask = mask(New cv.Rect(1, 1, dst2.Width, dst2.Height))
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
                        colorList(j) = rcNext
                    End If
                Next
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
                rc.index = rcList.Count
                rcList.Add(rc)
            Next

            Dim usedList As New SortedList(Of Integer, Byte)(New compareAllowIdenticalInteger)
            For Each rc In colorList
                If usedList.ContainsKey(rc.ID) = False Then usedList.Add(rc.ID, rc.index)
            Next

            dst2 = ApplyPalette(rcMap, 0)

            If standalone Then
                Dim rctmp = RedC_Basics.rcFinder(rcList, rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X))
                If rctmp IsNot Nothing Then
                    task.rcD = rctmp
                    Rectangle(task.color, task.rcD.rect, task.highlight, task.lineWidth)
                    task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
                    SetTrueText(task.rcD.displayCell, 3)
                Else
                    If rcList.Count > 1 Then task.rcD = rcList(1)
                End If
            End If

            labels(2) = CStr(color8U.classCount) + " color8U classes created " + CStr(rcList.Count) + " cells"
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
            If src.Channels <> 1 Then
                Static color8u As New Color8U_Basics
                color8u.Run(task.gray)
                src = color8u.dst2
            End If

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

            SetTrueText(XR_RedC_Core.displayCell(rcList, rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)), 1)

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

            dst3 = ApplyPalette(rcMap, 0)
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
        Dim color8U As New Color8U_Basics
        Public Sub New()
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            labels(3) = "The inputRemoved mask is used to limit how much of the image is processed."
            desc = "Use InRange to prepare an 8U input image for RedColor_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            color8U.Run(src)
            InRange(task.pcSplit(2), task.MaxZmeters, 1000, inputRemoved)
            ConvertScaleAbs(inputRemoved, inputRemoved)
            src = color8U.dst2

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
            dst3 = ApplyPalette(dst1, 0)
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

            dst1 = ApplyPalette(dst0, 0)
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
            If src.Channels <> 1 Then
                Static color8U As New Color8U_Basics
                color8U.Run(task.gray)
                src = color8U.dst2
            End If

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

            dst2 = ApplyPalette(rcMap, 0)
            dst3 = task.fless.dst2

            labels(2) = CStr(rcList.Count) + " cells were identified."
        End Sub
    End Class
End Namespace