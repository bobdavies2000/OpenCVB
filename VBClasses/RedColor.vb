Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedColor_Basics : Inherits TaskParent
        Dim color8U As New Color8U_Basics
        Public tracker As New FeatureLess_Tracker
        Public rcList As New List(Of rcData)
        Public indexMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Build a list of cells for each floodfill region in the Color8U input."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            color8U.Run(task.gray)
            labels(2) = color8U.labels(2)

            tracker.Run(color8U.dst2)
            dst2 = color8U.dst3.Clone

            Dim mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim rect As cv.Rect
            Dim filled As New cv.Mat
            rcList.Clear()
            rcList.Add(New rcData)
            dst1 = color8U.dst2.Clone
            indexMap.SetTo(0)
            For y = 0 To dst2.Height - 1
                For x = 0 To dst2.Width - 1
                    If mask.Get(Of Byte)(y, x) = 0 Then
                        Dim index = rcList.Count
                        Dim flags = FloodFillFlags.FixedRange Or (index << 8)
                        Dim count = FloodFill(dst1, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                        If count >= task.minCellSize Then
                            Dim rc = New rcData(dst1(rect), rect, index) With {.index = index}
                            indexMap(rc.rect).SetTo(index, rc.mask)
                            rcList.Add(rc)
                        Else
                            If count > 0 Then mask(rect).SetTo(0, dst1(rect)) ' add the small fragments back to the pool.
                        End If
                    End If
                Next
            Next

            dst3 = Palettize(indexMap, 0)

            If standaloneTest() Then
                Dim clickIndex = dst1.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
                Dim rc = rcList(clickIndex)
                Rectangle(dst2, rc.rect, task.highlight, task.lineWidth)
                dst2(rc.rect).SetTo(white, rc.mask)
                SetTrueText(CStr(clickIndex), rc.maxDist)
                SetTrueText(rc.displayCell, 1)
            End If

            labels(1) = tracker.labels(2)
            labels(3) = CStr(rcList.Count) + " cells in the Color8U output."
            If rcList.Count > 255 Then
                MsgBox("Flood_RectMats needs to increase the minimum cell size - too many to fit in CV_8U!")
            End If
        End Sub
    End Class





    Public Class RedColor_FloodToID : Inherits TaskParent
        Public indexMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcList As New List(Of rcData) ' includes cloud data.
        Dim flood As New Flood_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Build an rcList from the flood data and present smalles to largest."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastMap = indexMap.Clone
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
            indexMap.SetTo(0)
            For Each rc In sortList.Values
                rc.index = rcList.Count
                indexMap(rc.rect).SetTo(rc.index, rc.mask)
                rcList.Add(rc)
            Next

            SetTrueText(RedC_Basics.displayCell(rcList, indexMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)), 1)

            For Each rc In rcList
                rc.id = lastMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If rc.id = 0 Then rc.id = rc.index
                indexMap(rc.rect).SetTo(rc.id, rc.mask)
            Next
            labels(2) = CStr(sortList.Values.Count) + " cells were found "

            If task.rcD IsNot Nothing Then
                Circle(dst2, task.rcD.maxDist, task.DotSize + 1, white, -1)
                Circle(dst2, task.rcD.maxDStable, task.DotSize + 1, black, -1)
                ' Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            End If

            dst3 = Palettize(indexMap, 0)
            labels(3) = CStr(rcList.Count - 1) + " cells found reordered and assigned rc.ID's."
        End Sub
    End Class




    Public Class RedColor_BasicsOld : Inherits TaskParent
        Public rcList As New List(Of rcDataOld)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Dim fLess As New FeatureLess_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(3) = "The output of FeatureLess_BasicsOld.  Note that cell colors match the RedColor output."
            desc = "Use the FeatureLess regions to improve the RedColor output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then
                Static color8U As New Color8U_Basics
                color8U.Run(task.gray)
                src = color8U.dst2
            End If

            fLess.Run(task.gray)

            Dim rect As cv.Rect
            Dim mask = New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim rectSorted As New SortedList(Of Integer, (count As Integer, r As cv.Rect))(New compareAllowIdenticalInteger)
            For Each r In fLess.brickList
                Dim val = mask(r).Get(Of Byte)(0, 0)
                If val = 0 Then
                    Dim index As Integer = fLess.dst1(r).Get(Of Byte)(0, 0)
                    If index > 0 Then
                        Dim flags = FloodFillFlags.FixedRange Or FloodFillFlags.Link4 Or (index << 8)
                        Dim count = FloodFill(src, mask, r.TopLeft, index, rect, 0, 0, flags)
                        rectSorted.Add(index, (count, ValidateRect(rect)))
                    End If
                End If
            Next

            Dim rcSizeSort As New SortedList(Of Integer, rcDataOld)(New compareAllowIdenticalIntegerInverted)
            For i = 0 To rectSorted.Count - 2
                Dim r1 = rectSorted.ElementAt(i).Value.r
                If rectSorted.ElementAt(i).Key = rectSorted.ElementAt(i + 1).Key Then
                    For j = i To rectSorted.Count - 2
                        Dim r2 = rectSorted.ElementAt(j + 1).Value.r
                        If rectSorted.ElementAt(j).Key = rectSorted.ElementAt(j + 1).Key Then
                            r1 = r1.Union(r2)
                        Else
                            Dim rc As New rcDataOld(src(r1), r1, rectSorted.ElementAt(j).Key)
                            rcSizeSort.Add(rectSorted.ElementAt(i).Value.count, rc)
                            i = j
                            Exit For
                        End If
                    Next
                Else
                    Dim rc As New rcDataOld(src(r1), r1, rectSorted.ElementAt(i).Key)
                    rcSizeSort.Add(rectSorted.ElementAt(i).Value.count, rc)
                End If
            Next

            rcIndexMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcDataOld)
            For Each rc In rcSizeSort.Values
                rc.index = rcList.Count
                rcList.Add(rc)
                rcIndexMap(rc.rect).SetTo(rc.index, rc.mask)
            Next

            dst2 = Palettize(rcIndexMap, 0)
            dst3 = fLess.dst2

            labels(2) = CStr(rcList.Count) + " cells were identified."
        End Sub
    End Class






    Public Class RedColor_GridRects : Inherits TaskParent
        Dim redC As New RedColor_BasicsOld
        Public rcGridMap As New Mat(dst2.Size, MatType.CV_8U, 0) ' map of rc data to grid map
        Public Sub New()
            labels(3) = "RedColor output mapped into the gridRects."
            desc = "Create a triangle representation of the cv.Point cloud with RedCloud data"
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
                Dim index As Integer = redC.rcIndexMap.Get(Of Single)(center.Y, center.X) - 1
                If index >= redC.rcList.Count Or index < 0 Then Continue For
                Dim rc = redC.rcList(index)
                dst3(r).SetTo(task.scalarColors(rc.index Mod 255))
                rcGridMap(r).SetTo(rc.mapID)
            Next
        End Sub
    End Class





    Public Class RedColor_InRange : Inherits TaskParent
        Public inputRemoved As New Mat
        Public showSelected As Boolean = True
        Public redC As New RedColor_BasicsOld
        Dim color8U As New Color8U_Basics
        Public Sub New()
            labels(3) = "The inputRemoved mask is used to limit how much of the image is processed."
            desc = "Use InRange to prepare an 8U input image for RedColor_BasicsOld"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            color8U.Run(src)
            InRange(task.pcSplit(2), task.MaxZmeters, 1000, inputRemoved)
            ConvertScaleAbs(inputRemoved, inputRemoved)
            src = color8U.dst2

            src.SetTo(0, inputRemoved)

            redC.Run(src)
            labels(2) = redC.labels(2)
            dst2 = redC.dst2.SetTo(0, inputRemoved)

            labels(2) = $"{redC.rcList.Count} cells identified"

            If showSelected Then SetTrueText(redC.strOut, 3)
        End Sub
    End Class
End Namespace