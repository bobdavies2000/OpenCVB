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







Public Class RedC_Hulls : Inherits TaskParent
    Dim redC As New RedC_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Display the hull for each cell."
    End Sub
    Public Overrides Sub RunAlg(src As Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        For i = redC.rcList.Count - 1 To 0 Step -1
            Dim rc = redC.rcList(i)
            FillPoly(dst1(rc.rect), {rc.hull}, rc.mapID)
        Next

        dst3 = Palettize(dst1)
    End Sub
End Class






Public Class RedC_TrackCell : Inherits TaskParent
    Dim redC As New RedC_Basics
    Dim rcLast As rcData = Nothing
    Dim lastClickPoint As cv.Point
    Dim strCause As New List(Of String)
    Public Sub New()
        task.gOptions.displayDst1.Checked = True
        desc = "Track the selected cell even after maxDStable goes beyond the edge of the cell."
    End Sub
    Private Function rcDFindCell(rcLast As rcData) As rcData
        Dim rcD As rcData = Nothing
        Dim candidates As New List(Of (index As Integer, mapID As Byte))
        For Each rc In redC.rcList
            If rcLast.rect.IntersectsWith(rc.rect) And rcLast.mapID = rc.mapID Then candidates.Add((rc.index, rc.mapID))
        Next

        strCause.Add(CStr(candidates.Count) + " candidates were found")
        If candidates.Count > 0 Then rcD = redC.rcList(candidates(0).index)
        Return rcD
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then dst1.SetTo(0)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim rcD As rcData = rcLast
        If task.mouseClickFlag Then
            strCause.Clear()
            strCause.Add("Clicked on a new cell")
            Dim clickIndex = redC.rcIndexMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            rcD = redC.rcList(clickIndex)
            If rcD.maxDStable = newPoint Then rcD.maxDStable = rcD.maxDist
            rcLast = rcD
            lastClickPoint = task.clickPoint
        End If

        Dim stablePoints As New List(Of cv.Point)
        For Each rc In redC.rcList
            stablePoints.Add(rc.maxDStable)
        Next

        If strCause.Count > 30 Then strCause.RemoveAt(0)
        Dim index = stablePoints.IndexOf(rcD.maxDStable)
        dst3.SetTo(0)
        If index >= 0 Then
            rcD = redC.rcList(index)
        Else
            rcD = rcDFindCell(rcLast)
            If rcD Is Nothing Then
                strCause.Add("Found using the last clickpoint")
                Dim clickIndex = redC.rcIndexMap.Get(Of Integer)(lastClickPoint.Y, lastClickPoint.X)
                rcD = redC.rcList(clickIndex)
            End If
        End If

        rcLast = rcD

        task.color(rcD.rect).SetTo(white, rcD.mask)
        dst3(rcD.rect).SetTo(task.scalarColors(rcD.mapID), rcD.mask)
        Rectangle(dst2, rcD.rect, task.highlight, task.lineWidth)
        Polylines(dst3(rcD.rect), {rcD.hull}, True, Scalar.Red, 2)
        Circle(dst3, rcD.maxDStable, task.DotSize + 1, task.highlight, -1)
        Circle(dst1, rcLast.maxDist, task.DotSize + 1, task.highlight, -1)

        strOut = rcD.displayCell() + vbCrLf + rcD.maxDist.ToString + vbCrLf + "No change: maxDStable" + vbCrLf + strCause(0)
        For i = 0 To strCause.Count - 1
            strOut += ", " + strCause(i)
            If i Mod 3 = 0 Then strOut += vbCrLf
        Next
        SetTrueText(strOut, 1)

        task.rcD = rcD
    End Sub
End Class







Public Class RedC_TrackHull : Inherits TaskParent
    Dim redC As New RedC_Basics
    Dim lastCenter As cv.Point
    Dim lastMapID As Byte
    Dim lastRect As cv.Rect
    Public Sub New()
        task.gOptions.displayDst1.Checked = True
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
        desc = "Track the selected cell even after maxDStable goes beyond the edge of the cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then dst1.SetTo(0)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst0.SetTo(0)
        For i = redC.rcList.Count - 1 To 0 Step -1
            Dim rc = redC.rcList(i)
            FillPoly(dst0(rc.rect), {rc.hull}, rc.index)
        Next

        Dim index As Integer
        If task.mouseClickFlag Then
            index = dst0.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        Else
            index = dst0.Get(Of Integer)(lastCenter.Y, lastCenter.X)
            For Each rc In redC.rcList
                If rc.rect.IntersectsWith(lastRect) And rc.mapID = lastMapID Then
                    index = rc.index
                    Exit For ' find the largest
                End If
            Next
        End If

        Dim rcD = redC.rcList(index)

        dst3.SetTo(0)
        task.color(rcD.rect).SetTo(white, rcD.mask)
        FillPoly(dst3(rcD.rect), {rcD.hull}, task.scalarColors(rcD.mapID + 1))
        dst3(rcD.rect).SetTo(task.scalarColors(rcD.mapID), rcD.mask)
        Rectangle(dst2, rcD.rect, task.highlight, task.lineWidth)
        Circle(dst1, lastCenter, task.DotSize + 1, task.highlight, -1)
        SetTrueText(rcD.displayCell() + vbCrLf, 1)

        task.rcD = rcD
        lastCenter = Utility_Basics.ComputeHullCentroid(rcD.hull.ToArray, rcD)
        lastMapID = rcD.mapID
        lastRect = rcD.rect
    End Sub
End Class