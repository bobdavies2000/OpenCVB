Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedC_Basics : Inherits TaskParent
    Dim color8u As New Color8U_Basics
    Public rcMap As Mat = New Mat(dst2.Size, MatType.CV_8U, 0)
    Public rcList As New List(Of rcData) ' includes cloud data.
    Dim rcListLast As New List(Of rcData)
    Public rcIndexMap As Mat = New Mat(dst2.Size, MatType.CV_32S, 0)
    Public Sub New()
        labels(3) = "The tracked cell.  The dot is the maxDStable for the tracked cell."
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
                        If rc.pixels >= 10 Then sortList.Add(rc.pixels, rc)
                    End If
                End If
            Next
        Next
        dst2 = Palettize(rcMap, 0)

        rcIndexMap.SetTo(0)
        rcList.Clear()
        rcList.Add(New rcData)
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

        Dim index = stablePoints.IndexOf(rcD.maxDStable)
        dst3.SetTo(0)
        If index >= 0 Then
            rcD = redC.rcList(index)
        Else
            rcD = rcDFindCell(rcLast)
            If rcD Is Nothing Then
                Dim clickIndex = redC.rcIndexMap.Get(Of Integer)(lastClickPoint.Y, lastClickPoint.X)
                rcD = redC.rcList(clickIndex)
            End If
        End If

        If rcD.index <> 0 Then rcLast = rcD Else rcD = redC.rcList(1)

        task.color(rcD.rect).SetTo(white, rcD.mask)
        dst3(rcD.rect).SetTo(task.scalarColors(rcD.mapID), rcD.mask)
        Rectangle(dst2, rcD.rect, task.highlight, task.lineWidth)
        Circle(dst3, rcD.maxDStable, task.DotSize + 1, task.highlight, -1)
        Circle(dst1, rcLast.maxDist, task.DotSize + 1, task.highlight, -1)

        strOut = rcD.displayCell() + vbCrLf + rcD.maxDist.ToString + vbCrLf
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





Public Class RedC_NeighborHulls : Inherits TaskParent
    Dim redC As New RedC_Basics
    Dim clickPoint As cv.Point
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
        desc = "Find the neighbors for the selected cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst0.SetTo(0)
        For i = redC.rcList.Count - 1 To 0 Step -1
            Dim rc = redC.rcList(i)
            FillPoly(dst0(rc.rect), {rc.hull}, rc.index)
        Next

        Dim index As Integer
        If task.mouseClickFlag Then clickPoint = task.clickPoint
        index = dst0.Get(Of Integer)(clickPoint.Y, clickPoint.X)

        Dim rcD = redC.rcList(index)
        SetTrueText(rcD.displayCell() + vbCrLf, 1)

        Dim neighbors As New List(Of Integer)
        For Each pt In rcD.contour
            pt.X += rcD.rect.X
            pt.Y += rcD.rect.Y
            Dim rect = ValidateRect(New cv.Rect(pt.X - task.gridWH / 2, pt.Y - task.gridWH / 2, task.gridWH, task.gridWH))
            Dim pixels(rect.Width * rect.Height - 1) As Integer
            Dim tmp = dst0(rect).Clone
            Marshal.Copy(tmp.Data, pixels, 0, pixels.Length)

            For i = 0 To pixels.Count - 1
                If pixels(i) = 0 Then Continue For
                If neighbors.Contains(pixels(i)) = False Then neighbors.Add(pixels(i))
            Next
        Next

        dst3.SetTo(0)
        dst3(rcD.rect).SetTo(task.scalarColors(rcD.mapID), rcD.mask)
        For i = 0 To neighbors.Count - 1
            Dim rc = redC.rcList(neighbors(i))
            dst3(rc.rect).SetTo(task.scalarColors(rc.mapID), rc.mask)
        Next

        dst3(rcD.rect).SetTo(task.highlight, rcD.mask)
        Circle(dst3, clickPoint, task.DotSize + 2, white, -1)
        labels(3) = CStr(neighbors.Count) + " neighbors were present."
    End Sub
End Class






Public Class RedC_NeighborHist : Inherits TaskParent
    Public redC As New RedC_Basics
    Dim lastCenter As cv.Point
    Public rcD As rcData
    Public neighbors As New List(Of Integer)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Use a histogram to find the neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then dst1.SetTo(0)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim index As Integer
        If task.mouseClickFlag Then lastCenter = task.clickPoint
        index = redC.rcIndexMap.Get(Of Integer)(lastCenter.Y, lastCenter.X)

        If index > 0 Then
            rcD = redC.rcList(index)
        Else
            Dim rect As New cv.Rect(lastCenter.X, lastCenter.Y, task.gridWH, task.gridWH)
            Dim myMapID = redC.rcMap.Get(Of Byte)(lastCenter.Y, lastCenter.X)
            For Each rc In redC.rcList
                If rc.mapID = myMapID And rc.rect.IntersectsWith(rect) Then
                    rcD = rc
                    Exit For
                End If
            Next
            If rcD Is Nothing Then rcD = redC.rcList(1)
        End If
        SetTrueText(rcD.displayCell() + vbCrLf, 1)

        Dim histogram As New Mat, tmp As New cv.Mat
        Dim ranges() As Rangef = New Rangef() {New Rangef(0, redC.rcList.Count + 1)}
        Dim delta = task.gridWH / 2
        Dim r = New cv.Rect(rcD.rect.X - delta, rcD.rect.Y - delta, rcD.rect.Width + task.gridWH, rcD.rect.Height + task.gridWH)
        r = ValidateRect(r)
        redC.rcIndexMap(r).ConvertTo(tmp, MatType.CV_8U)
        ' why did I need to add 1 to tmp?!!!
        CalcHist({tmp + 1}, {0}, New Mat, histogram, 1, {redC.rcList.Count}, ranges)

        Dim histArray(histogram.Rows - 1) As Single
        histogram.GetArray(Of Single)(histArray)

        neighbors.Clear()

        For i = 1 To histArray.Count - 1
            If histArray(i) > 0 Then neighbors.Add(i)
        Next

        dst3.SetTo(0)
        For i = 0 To neighbors.Count - 1
            Dim rc = redC.rcList(neighbors(i))
            dst3(rc.rect).SetTo(task.scalarColors(rc.mapID), rc.mask)
        Next

        dst3(rcD.rect).SetTo(task.highlight, rcD.mask)
        Rectangle(dst3, r, task.highlight, task.lineWidth)
        Rectangle(dst2, r, task.highlight, task.lineWidth)
        labels(3) = CStr(neighbors.Count) + " neighbors were present."

        lastCenter = rcD.maxDStable
        Circle(dst1, lastCenter, task.DotSize + 1, task.highlight, -1)
    End Sub
End Class






Public Class RedC_MergeCells : Inherits TaskParent
    Dim nabe As New RedC_NeighborHist
    Public merged As New rcData
    Public mergeList As New List(Of rcData)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Merge the selected cell with neighbors that are at about the same depth."
    End Sub
    Private Function cellDepth(rc As rcData) As Single
        If rc Is Nothing OrElse rc.mask.Width <= 1 OrElse rc.pixels = 0 Then Return 0
        Dim depthMask As New Mat
        BitwiseAnd(rc.mask, task.depthmask(rc.rect), depthMask)
        If CountNonZero(depthMask) = 0 Then Return 0
        Return CSng(Mean(task.pcSplit(2)(rc.rect), depthMask)(0))
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        nabe.Run(src)
        dst1 = nabe.dst1
        dst2 = nabe.dst2
        labels(2) = nabe.labels(2)

        mergeList.Clear()
        Dim rcD = nabe.rcD
        If rcD Is Nothing OrElse nabe.redC.rcList.Count <= 1 Then
            dst3 = nabe.dst3
            labels(3) = "No selected cell to merge."
            Exit Sub
        End If

        Dim depth0 = cellDepth(rcD)
        mergeList.Add(rcD)
        For Each idx In nabe.neighbors
            If idx = rcD.index OrElse idx <= 0 OrElse idx >= nabe.redC.rcList.Count Then Continue For
            Dim rc = nabe.redC.rcList(idx)
            Dim depth = cellDepth(rc)
            If depth0 > 0 AndAlso depth > 0 AndAlso Math.Abs(depth - depth0) <= task.depthDiffMeters Then
                mergeList.Add(rc)
            End If
        Next

        Dim unionRect = mergeList(0).rect
        For i = 1 To mergeList.Count - 1
            unionRect = unionRect.Union(mergeList(i).rect)
        Next
        unionRect = ValidateRect(unionRect)

        Dim fullMask As New Mat(dst2.Size(), MatType.CV_8U, Scalar.All(0))
        For Each rc In mergeList
            fullMask(rc.rect).SetTo(255, rc.mask)
        Next

        merged = New rcData()
        merged.rect = unionRect
        merged.mask = fullMask(unionRect).Clone()
        merged.mapID = rcD.mapID
        merged.index = rcD.index
        merged.contourHull()
        merged.maxDStable = merged.maxDist

        dst3.SetTo(0)
        For Each rc In mergeList
            dst3(rc.rect).SetTo(task.scalarColors(rc.mapID), rc.mask)
        Next
        dst3(merged.rect).SetTo(task.highlight, merged.mask)
        Rectangle(dst2, merged.rect, task.highlight, task.lineWidth)
        Rectangle(dst3, merged.rect, task.highlight, task.lineWidth)
        Circle(dst3, merged.maxDist, task.DotSize + 1, white, -1)

        SetTrueText(merged.displayCell() + vbCrLf +
                    "Selected depth = " + depth0.ToString(fmt2) + "m" + vbCrLf +
                    "Merged " + CStr(mergeList.Count) + " cells within " +
                    task.depthDiffMeters.ToString(fmt2) + "m", 1)

        labels(3) = CStr(mergeList.Count) + " of " + CStr(nabe.neighbors.Count) +
                    " neighbors merged (depth within " + task.depthDiffMeters.ToString(fmt2) + "m)"
    End Sub
End Class





Public Class RedC_Depth : Inherits TaskParent
    Dim redC As New RedC_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst0 = New Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0))
        desc = "cursor.ai: Display the depth of each cell using the same colors as the DepthColorizer_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst0.SetTo(0)
        Dim depthCount As Integer
        For Each rc In redC.rcList
            If rc.index = 0 Then Continue For
            Dim depth8u = CByte(Math.Min(255, rc.depth * 255.0 / task.MaxZmeters))
            dst0(rc.rect).SetTo(depth8u, rc.mask)
            depthCount += 1
        Next

        ApplyColorMap(dst0, dst3, task.colorMapDepth)
        dst3.SetTo(0, task.noDepthMask)

        Dim clickIndex = redC.rcIndexMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
        If clickIndex > 0 AndAlso clickIndex < redC.rcList.Count Then
            Dim rc = redC.rcList(clickIndex)
            SetTrueText(rc.displayCell() + vbCrLf + "Mean depth = " + rc.depth.ToString(fmt2) + "m", 1)
            task.color(rc.rect).SetTo(white, rc.mask)
            dst2(rc.rect).SetTo(task.highlight, rc.mask)
            Rectangle(dst3, rc.rect, task.highlight, task.lineWidth)
        End If

        labels(3) = CStr(depthCount) + " cells colored by mean depth (0-" +
                    task.MaxZmeters.ToString(fmt0) + "m DepthColorizer palette)"
    End Sub
End Class
