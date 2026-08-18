Imports System.Buffers
Imports System.Runtime.InteropServices
Imports System.Windows.Forms.Design.AxImporter
Imports OpenCvSharp
Imports OpenCvSharp.Cv2
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedC_Basics : Inherits TaskParent
        Public rcMap As New Mat(dst2.Size, MatType.CV_32S, 0)
        Public rcList As New List(Of rcData) ' includes cloud data.
        Public maxDStableList As New List(Of cv.Point)
        Public flood As New Flood_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Create the rcData representation of the image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rcMapLast = rcMap.Clone
            Dim rcListLast = New List(Of rcData)(rcList)
            Dim maxDLast = New List(Of cv.Point)(maxDStableList)

            If src.Channels <> 1 Then
                Static color8u As New Color8U_Basics
                color8u.Run(task.gray)
                src = color8u.dst2
            End If

            flood.Run(src)
            dst2 = flood.dst2
            rcList.Clear()
            rcList.Add(New rcData)
            rcMap.SetTo(0)
            For i = 0 To flood.rectList.Count - 1
                Dim index = flood.indexList(i)
                Dim r = flood.rectList(i)

                Dim rc As New rcData(flood.mask(r), r, index) With {.index = rcList.Count}
                rc.mapID = flood.dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                rcMap(r).SetTo(rc.index, rc.mask)
                rcList.Add(rc)
            Next

            maxDStableList.Clear()
            For Each rc In rcList
                Dim rcIndex = rcMapLast.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
                If rcIndex > 0 Then
                    Dim rcLast = rcListLast(rcIndex)
                    Dim index = maxDLast.IndexOf(rcLast.maxDStable)
                    If index >= 0 Then
                        rc.age = rcLast.age + 1
                        If rc.age >= 1000 Then rc.age = 10
                        rc.maxDStable = rcLast.maxDStable
                    End If
                End If
                maxDStableList.Add(rc.maxDStable)
            Next

            ' verify each maxDStable is within the cell.
            For Each rc In rcList
                Dim val1 = flood.dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                Dim val2 = flood.dst2.Get(Of cv.Vec3b)(rc.maxDStable.Y, rc.maxDStable.X)
                If val1 <> val2 Or rc.rect.Contains(rc.maxDStable) = False Then rc.maxDStable = rc.maxDist
                dst1(rc.rect).SetTo(rc.mapID, rc.mask)
            Next

            dst3 = Palettize(dst1, 0)
            dst1.SetTo(0)

            Static clickPoint As cv.Point
            If task.mouseClickFlag Then clickPoint = task.clickPoint
            Dim clickIndex = rcMap.Get(Of Integer)(clickPoint.Y, clickPoint.X)
            If clickIndex <= 0 Then
                SetTrueText("There is no cell defined for that point.", 1)
            Else
                task.rcD = rcList(clickIndex)
                task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
                Circle(dst2, task.rcD.maxDist, task.DotSize + 1, white, -1)
                Circle(dst2, task.rcD.maxDStable, task.DotSize + 1, black, -1)
                Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
                SetTrueText(task.rcD.displayCell, 1)
                SetTrueText(CStr(task.rcD.age), task.rcD.maxDist)
            End If

            labels(2) = CStr(rcList.Count) + " cells were found."
        End Sub
    End Class






    Public Class XR_RedC_BasicsOld : Inherits TaskParent
        Dim color8u As New Color8U_Basics
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcList As New List(Of rcData) ' includes cloud data.
        Dim rcListLast As New List(Of rcData)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32S, 0)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels(3) = "The tracked cell.  The dot is the maxDStable for the tracked cell."
            desc = "FloodFill each color8U output and create an rclist"
        End Sub
        Public Shared Function displayCell(rcIndexMap As cv.Mat, rcList As List(Of rcData)) As String
            Dim clickIndex = rcIndexMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            task.rcD = rcList(clickIndex)
            task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
            If clickIndex = 0 Then Return task.rcD.displayCell() + vbCrLf + vbCrLf + "Unmapped region.  No cell present" + vbCrLf
            Return task.rcD.displayCell()
        End Function
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
            Dim mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim floodMap = rcMap.Clone
            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted) From {{0, New rcData}}
            For y = 0 To floodMap.Height - 1
                For x = 0 To floodMap.Width - 1
                    If mask.Get(Of Byte)(y, x) = 0 Then
                        Dim index As Integer = sortList.Count
                        Dim mapID As Integer = rcMap.Get(Of Byte)(y, x)
                        Dim flags = FloodFillFlags.FixedRange Or (index << 8) ' Or FloodFillFlags.MaskOnly
                        Dim count = FloodFill(floodMap, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                        If count > CInt(src.Total * 0.001) Then
                            Dim rc As New rcData(floodMap(rect), rect, index) With {.mapID = mapID}
                            If rc.pixels >= 10 Then sortList.Add(rc.pixels, rc)
                        End If
                    End If
                Next
            Next

            dst2 = Palettize(rcMap, 0)

            rcIndexMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData)
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

                If indexLast < rcListLast.Count Then
                    rc.maxDStable = If(mapIDCurr = mapIDLast, rcListLast(indexLast).maxDStable, rc.maxDist)
                    Dim color = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                    Dim colorLast = dst2.Get(Of cv.Vec3b)(rc.maxDStable.Y, rc.maxDStable.X)
                    If color <> colorLast Then rc.maxDStable = rc.maxDist
                Else
                    If task.firstPass Then rc.maxDStable = rc.maxDist
                End If
            Next

            strOut = displayCell(rcIndexMap, rcList)
            SetTrueText(strOut, 1)

            If task.heartBeat Then labels(2) = CStr(rcList.Count) + " RedColor cells were found."
        End Sub
    End Class





    Public Class XR_RedC_BasicsList : Inherits TaskParent
        Dim color8u As New Color8U_Basics
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcList As New List(Of rcData) ' includes cloud data.
        Dim rcListLast As New List(Of rcData)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32S, 0)
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels(3) = "The tracked cell.  The dot is the maxDStable for the tracked cell."
            desc = "FloodFill each color8U output and create an rclist"
        End Sub
        Public Shared Function displayCell(rcIndexMap As cv.Mat, rcList As List(Of rcData)) As String
            Dim clickIndex = rcIndexMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
            task.rcD = rcList(clickIndex)
            task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
            If clickIndex = 0 Then Return task.rcD.displayCell() + vbCrLf + vbCrLf + "Unmapped region.  No cell present" + vbCrLf
            Return task.rcD.displayCell()
        End Function
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
            Dim mask As New Mat(New Size(dst2.Width + 2, dst2.Height + 2), MatType.CV_8U, 0)
            Dim floodMap = rcMap.Clone
            Dim sortList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted) From {{0, New rcData}}
            For y = 0 To floodMap.Height - 1
                For x = 0 To floodMap.Width - 1
                    If mask.Get(Of Byte)(y, x) = 0 Then
                        Dim index As Integer = sortList.Count
                        Dim mapID As Integer = rcMap.Get(Of Byte)(y, x)
                        Dim flags = FloodFillFlags.FixedRange Or (index << 8) ' Or FloodFillFlags.MaskOnly
                        Dim count = FloodFill(floodMap, mask, New cv.Point(x, y), index, rect, 0, 0, flags)
                        If count > CInt(src.Total * 0.001) Then
                            Dim rc As New rcData(floodMap(rect), rect, index) With {.mapID = mapID}
                            If rc.pixels >= 10 Then sortList.Add(rc.pixels, rc)
                        End If
                    End If
                Next
            Next

            dst2 = Palettize(rcMap, 0)

            rcIndexMap.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData)
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

                If indexLast < rcListLast.Count Then
                    rc.maxDStable = If(mapIDCurr = mapIDLast, rcListLast(indexLast).maxDStable, rc.maxDist)
                    Dim color = dst2.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                    Dim colorLast = dst2.Get(Of cv.Vec3b)(rc.maxDStable.Y, rc.maxDStable.X)
                    If color <> colorLast Then rc.maxDStable = rc.maxDist
                Else
                    If task.firstPass Then rc.maxDStable = rc.maxDist
                End If
            Next

            strOut = displayCell(rcIndexMap, rcList)
            SetTrueText(strOut, 1)

            If task.heartBeat Then labels(2) = CStr(rcList.Count) + " RedColor cells were found."
        End Sub
    End Class






    Public Class XR_RedC_Reliable : Inherits TaskParent
        Dim redC As New RedC_Basics
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
                    dst3(rc.rect).SetTo(task.scalarColors(rc.index Mod 255), rc.mask)
                    count += 1
                End If
            Next
            labels(3) = CStr(count) + " were consistently present."
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







    Public Class XR_RedC_Hulls : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public rcList As New List(Of rcData)
        Public Sub New()
            dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Display the hull for each cell."
        End Sub
        Public Overrides Sub RunAlg(src As Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            For i = 0 To redC.rcList.Count - 1
                Dim rc = redC.rcList(i)
                If rc.hull IsNot Nothing Then FillPoly(dst1(rc.rect), {rc.hull}, rc.mapID)
            Next

            rcList = New List(Of rcData)(redC.rcList)
            dst3 = Palettize(dst1)
            labels(3) = CStr(redC.rcList.Count) + " hulls with the smallest on top."
        End Sub
    End Class






    Public Class XR_RedC_TrackHull : Inherits TaskParent
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
                If rc.hull IsNot Nothing Then FillPoly(dst0(rc.rect), {rc.hull}, rc.index)
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





    Public Class XR_RedC_NeighborHulls : Inherits TaskParent
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
                If rc.hull IsNot Nothing Then FillPoly(dst0(rc.rect), {rc.hull}, rc.index)
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

                For i = 0 To pixels.Length - 1
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
            index = redC.rcMap.Get(Of Integer)(lastCenter.Y, lastCenter.X)

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
            redC.rcMap(r).ConvertTo(tmp, MatType.CV_8U)
            ' why did I need to add 1 to tmp?!!!
            CalcHist({tmp + 1}, {0}, New Mat, histogram, 1, {redC.rcList.Count}, ranges)

            Dim histArray(histogram.Rows - 1) As Single
            histogram.GetArray(Of Single)(histArray)

            neighbors.Clear()

            For i = 1 To histArray.Length - 1
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
        Private Shared Function cellDepth(rc As rcData) As Single
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

            merged = New rcData() With {.rect = unionRect, .mask = fullMask(unionRect).Clone(), .mapID = rcD.mapID,
                                        .index = rcD.index, .maxDStable = merged.maxDist}
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

            Dim clickIndex = redC.rcMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
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






    Public Class XR_RedC_SteadyCam : Inherits TaskParent
        Dim steady As New SteadyCam_Basics
        Dim redC As New RedC_Basics
        Dim color8U As New Color8U_Basics
        Public Sub New()
            desc = "Build the RedC cells using the GravityRGB_SteadyXY output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            steady.Run(task.grayOriginal)
            dst3 = steady.dst3

            Threshold(dst3, dst1, 0, 255, cv.ThresholdTypes.BinaryInv)

            color8U.Run(dst3)
            color8U.dst2.SetTo(0, dst1)
            redC.Run(color8U.dst2)
            dst2 = redC.dst2
            dst2.SetTo(0, dst1)
            labels = redC.labels
        End Sub
    End Class





    Public Class RedC_CellLines : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            desc = "Find any lines connected to a cell contour."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim histogram As New Mat
            Dim bins = task.lines.lpList.Count
            Dim ranges = {New Rangef(0, bins)}
            Dim histArray(bins) As Single
            dst3 = dst2.Clone
            For Each rc In redC.rcList
                If rc.index = 0 Then Continue For
                Dim tmp = task.lines.dst1(rc.rect).Clone
                CalcHist({tmp}, {0}, rc.mask, histogram, 1, {bins}, ranges)
                histogram.GetArray(Of Single)(histArray)
                For i = 1 To bins - 1
                    If histArray(i) > 0 Then rc.lpList.Add(i)
                Next

                For Each index In rc.lpList
                    Dim lp = task.lines.lpList(index - 1)
                    Line(dst3, lp.p1, lp.p2, task.highlight, task.lineWidth + 1, task.lineType)
                Next
            Next
        End Sub
    End Class





    Public Class RedC_TrackCell : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            task.gOptions.displayDst1.Checked = True
            desc = "Track the selected cell even after maxDStable goes beyond the edge of the cell."
        End Sub
        Private Function rcDFindCell(rcLast As rcData) As rcData
            Dim rcD As rcData = Nothing
            Dim candidates As New List(Of (index As Integer, rc As rcData))
            For Each rc In redC.rcList
                If rcLast.mapID = rc.mapID Then candidates.Add((rc.index, rc))
            Next

            If candidates.Count > 0 Then
                Dim pixelsSorted As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
                For i = 0 To candidates.Count - 1
                    Dim rc = candidates(i).rc
                    Dim rect = rc.rect.Intersect(rcLast.rect)
                    pixelsSorted.Add(rect.Width * rect.Height, rc)
                Next
                rcD = redC.rcList(candidates(0).index)
                dst1.SetTo(0)
                dst1(rcD.rect).SetTo(task.scalarColors(rcD.mapID), rcD.mask)
            End If
            Return rcD
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            If task.rcD Is Nothing Then Exit Sub

            Dim mapID = redC.flood.dst1.Get(Of Byte)(task.rcD.maxDStable.Y, task.rcD.maxDStable.X)
            If mapID <> task.rcD.mapID Then
                task.rcD = rcDFindCell(task.rcD)
                task.rcD.maxDStable = task.rcD.maxDist
            End If

            Dim index = redC.maxDStableList.IndexOf(task.rcD.maxDStable)
            dst3.SetTo(0)
            If index > 0 Then
                task.rcD = redC.rcList(index)
            Else
                Dim rcD = rcDFindCell(task.rcD)
                If rcD IsNot Nothing Then task.rcD = rcD
            End If

            task.clickPoint = task.rcD.maxDStable

            task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
            dst3(task.rcD.rect).SetTo(task.scalarColors(task.rcD.mapID), task.rcD.mask)
            Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            Circle(dst3, task.rcD.maxDStable, task.DotSize + 1, white, -1)
            Circle(dst3, task.rcD.maxDist, task.DotSize + 2, task.highlight, -1)

            strOut = task.rcD.displayCell() + vbCrLf + vbCrLf + "Track point " + task.clickPoint.ToString + vbCrLf
            SetTrueText(strOut, 1)
        End Sub
    End Class




    Public Class RedC_Smoothing : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Reduce the rc.contours points if the distance to the next is < X"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2.Clone
            labels(2) = redC.labels(2)

            dst1.SetTo(0)
            For Each rc In redC.rcList
                If rc.pixels > 100 Then
                    Dim epsilon = 0.01 * Cv2.ArcLength(rc.contour, True)
                    Dim simplified() As Point = Cv2.ApproxPolyDP(rc.contour.ToArray, epsilon, True)
                    rc.contour = simplified.ToList
                End If
                DrawContours(dst2(rc.rect), {rc.contour.ToArray}, 0, task.highlight, task.lineWidth, task.lineType)
            Next
        End Sub
    End Class
End Namespace