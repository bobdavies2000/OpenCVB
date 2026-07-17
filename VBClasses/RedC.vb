Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Public Class RedC_Basics : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Public rcMap As Mat = New Mat(dst2.Size, MatType.CV_8U, 0)
    Public rcList As New List(Of rcData) ' includes cloud data.
    Dim rcListLast As New List(Of rcData)
    Public rcIndexMap As Mat = New Mat(dst2.Size, MatType.CV_32S, 0)
    Public Sub New()
        desc = "FloodFill each color8U output and create an rclist"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim rcMapLast As cv.Mat = rcMap.Clone
        Dim rcIndexMapLast As cv.Mat = rcIndexMap.Clone
        rcListLast = New List(Of rcData)(rcList)

        If task.optionsChanged Then
            rcMapLast.SetTo(0)
            rcIndexMap.SetTo(0)
            rcListLast.Clear()
        End If

        color8u.Run(task.gray)

        rcMap = color8u.dst2.Clone + 1
        Dim rect As cv.Rect
        Dim mask As Mat = New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
        Dim floodMap = rcMap.Clone
        Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        sortList.Add(0, New rcData)
        For y = 0 To floodMap.Height - 1
            For x = 0 To floodMap.Width - 1
                If mask.Get(Of Byte)(y, x) = 0 Then
                    Dim index As Integer = sortList.Count
                    Dim mapID As Integer = rcMap.Get(Of Byte)(y, x)
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8) ' Or FloodFillFlags.MaskOnly
                    Dim count = FloodFill(floodMap, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                    If count > 10 Then
                        Dim rc = New rcData(floodMap(rect), rect, index)
                        rc.mapID = mapID
                        rc.maxDist = rc.buildMaxDist(rc.mask)
                        rc.pixels = CountNonZero(rc.mask)
                        If rc.pixels >= 10 Then sortList.Add(rc.pixels, rc)
                    End If
                End If
            Next
        Next
        dst2 = Palettize(rcMap, 0)

        rcIndexMap.SetTo(0)
        rcList.Clear()
        Dim flag = FloodFillFlags.FixedRange Or (255 << 8)
        For Each rc In sortList.Values
            If rc.pixels > 0 Then
                rc.index = rcList.Count
                rcIndexMap(rc.rect).SetTo(rc.index, rc.mask)
                rcList.Add(rc)
            End If
        Next

        For Each rc In rcList
            Dim mapIDCurr = rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            Dim mapIDLast = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            Dim indexLast = rcIndexMapLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)

            If rc.index = 0 Then Dim k = 0

            If indexLast < rcListLast.Count Then
                rc.maxDStable = If(mapIDCurr = mapIDLast, rcListLast(indexLast).maxDStable, rc.maxDist)
                Dim color = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                Dim colorLast = dst2.Get(Of cv.Vec3b)(rc.maxDStable.Y, rc.maxDStable.X)
                If color <> colorLast Then rc.maxDStable = rc.maxDist
            Else
                If task.firstPass Then rc.maxDStable = rc.maxDist
            End If
        Next

        Dim clickIndex = rcIndexMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        SetTrueText(rcList(clickIndex).displayCell() + vbCrLf, 3)

        If task.heartBeat Then labels(2) = CStr(rcList.Count) + " RedColor cells were found."
    End Sub
End Class





Public Class RedC_BasicsFail : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Public rcMap As Mat = New Mat(dst2.Size, MatType.CV_8U, 0)
    Public rcList As New List(Of rcData) ' includes cloud data.
    Dim rcListLast As New List(Of rcData)
    Public rcIndexMap As Mat = New Mat(dst2.Size, MatType.CV_32S, 0)
    Public Sub New()
        desc = "FloodFill each color8U output and create an rclist"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim rcMapLast As cv.Mat = rcMap.Clone
        Dim rcIndexMapLast As cv.Mat = rcIndexMap.Clone
        rcListLast = New List(Of rcData)(rcList)

        If task.optionsChanged Then
            rcMapLast.SetTo(0)
            rcIndexMapLast.SetTo(0)
            rcListLast.Clear()
        End If

        color8u.Run(src)

        rcMap = color8u.dst2.Clone + 1
        Dim rect As cv.Rect
        Dim mask As Mat = New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
        Dim floodMap = rcMap.Clone
        Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        sortList.Add(0, New rcData)
        For y = 0 To floodMap.Height - 1
            For x = 0 To floodMap.Width - 1
                If mask.Get(Of Byte)(y, x) = 0 Then
                    Dim index As Integer = sortList.Count
                    Dim mapID As Integer = rcMap.Get(Of Byte)(y, x)
                    Dim flags = FloodFillFlags.FixedRange Or (index << 8) ' Or FloodFillFlags.MaskOnly
                    Dim count = FloodFill(floodMap, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                    If count > 1 Then
                        Dim rc = New rcData(floodMap(rect), rect, index)
                        rc.mapID = mapID
                        rc.pixels = count
                        sortList.Add(rc.pixels, rc)
                    End If
                End If
            Next
        Next

        dst2 = Palettize(rcMap, 0)

        rcIndexMap.SetTo(0)
        rcList.Clear()
        Dim flag = FloodFillFlags.FixedRange Or (255 << 8)
        For Each rc In sortList.Values
            rc.index = rcList.Count

            rc.maxDist = rc.buildMaxDist(rc.mask)
            rc.pixels = CountNonZero(rc.mask)
            If rc.pixels > 0 Then
                rcIndexMap(rc.rect).SetTo(rc.index, rc.mask)
                rcList.Add(rc)
            End If
        Next

        If task.firstPass Then
            For Each rc In rcList
                rc.maxDStable = rc.maxDist
            Next
        Else
            For Each rc In rcList
                Dim indexCurr = rcIndexMap.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
                Dim indexLast = rcIndexMapLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
                Dim rcCurr = rcList(indexCurr)
                rc.maxDStable = rc.maxDist
                If indexLast < rcListLast.Count Then
                    Dim rcLast = rcListLast(indexLast)
                    If rcCurr.mapID = rcLast.mapID Then
                        If rcCurr.rect.IntersectsWith(rcLast.rect) Then
                            Dim md1 = rcMap.Get(Of Integer)(rcLast.maxDStable.Y, rcLast.maxDStable.X)
                            Dim md2 = rcMapLast.Get(Of Integer)(rcLast.maxDStable.Y, rcLast.maxDStable.X)
                            If md1 = md2 Then rc.maxDStable = rcLast.maxDStable
                        End If
                    End If
                End If
            Next
        End If

        Dim clickIndex = rcIndexMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        strOut = rcList(clickIndex).displayCell() + vbCrLf
        SetTrueText(strOut, 3)

        'strOut = Utility_Basics.selectMinCell(rcIndexMap, rcMap, rcList)
        'SetTrueText(strOut, 3)

        If task.heartBeat Then labels(2) = CStr(rcList.Count) + " RedColor cells were found."
    End Sub
End Class





Public Class XR_RedC_Sizes : Inherits TaskParent
    Dim redC As New RedC_Basics
    Public Sub New()
        If standalone Then task.gOptions.DebugSlider.Value = 32
        desc = "Use the debug slider to display cells of X pixels or less."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If task.heartBeat Then dst3.SetTo(0)
        Dim count As Integer
        For Each rc In redC.rcList
            If rc.pixels <= task.gOptions.DebugSlider.Value Then
                Dim vec = dst2.Get(Of Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                dst3(rc.rect).SetTo(vec, rc.mask)
                count += 1
            End If
        Next

        labels(3) = CStr(count) + " cells smaller than " + CStr(task.gOptions.DebugSlider.Value) + " pixels."
    End Sub
End Class







Public Class RedC_TrackCell : Inherits TaskParent
    Dim redC As New RedC_Basics
    Dim lastStablePoint As cv.Point
    Dim stablePoint As cv.Point
    Public Sub New()
        task.gOptions.displayDst1.Checked = True
        task.gOptions.DebugSlider.Minimum = 0
        desc = "Track the selected cell even after maxDStable goes beyond the edge of the cell."
    End Sub
    Private Sub rcDFindCell()
        Dim mapID = redC.rcMap.Get(Of Byte)(lastStablePoint.Y, lastStablePoint.X)
        Dim rcD = redC.rcList(redC.rcIndexMap.Get(Of Integer)(lastStablePoint.Y, lastStablePoint.X))
        lastStablePoint = newPoint
        ' Dim sortOverlapRects As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For Each rc In redC.rcList
            If rc.mapID = mapID Then
                If rcD.rect.IntersectsWith(rc.rect) Then
                    'Dim overlapRect As cv.Rect = rc.rect.Intersect(task.rcD.rect)
                    'sortOverlapRects.Add(overlapRect.Width * overlapRect.Height, rc.index)
                    task.rcD = rc
                    lastStablePoint = task.rcD.maxDist
                    Exit For
                End If
            End If
        Next

        'Dim index = sortOverlapRects.Values(0)
        'task.rcD = redC.rcList(index)
        'lastStablePoint = task.rcD.maxDist

        If lastStablePoint = newPoint Then Dim k = 0
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then dst1.SetTo(0)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If task.mouseClickFlag Then
            strOut = "" + vbCrLf
            Dim clickIndex = redC.rcIndexMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            task.rcD = redC.rcList(clickIndex)
            If task.rcD.maxDStable = newPoint Then task.rcD.maxDStable = task.rcD.maxDist
        End If

        Dim stablePoints As New List(Of cv.Point)
        For Each rc In redC.rcList
            stablePoints.Add(rc.maxDStable)
        Next

        Dim index = stablePoints.IndexOf(task.rcD.maxDStable)
        dst3.SetTo(0)
        If index >= 0 Then
            If strOut.Length < 200 Then strOut += "Cell was found using MaxDStable..." + vbCrLf
            task.rcD = redC.rcList(index)
            lastStablePoint = task.rcD.maxDist
        Else
            stablePoint = lastStablePoint
            rcDFindCell()
        End If

        If lastStablePoint = newPoint Then
            lastStablePoint = stablePoint
            rcDFindCell()
        End If

        Dim rcD = task.rcD
        task.color(rcD.rect).SetTo(white, rcD.mask)
        dst3(rcD.rect).SetTo(task.scalarColors(rcD.mapID), rcD.mask)
        Rectangle(dst2, rcD.rect, task.highlight, task.lineWidth)
        Circle(dst3, rcD.maxDStable, task.DotSize + 1, task.highlight, -1)
        Circle(dst1, lastStablePoint, task.DotSize + 1, task.highlight, -1)

        SetTrueText(rcD.displayCell() + vbCrLf + lastStablePoint.ToString + vbCrLf, 1)
    End Sub
End Class
