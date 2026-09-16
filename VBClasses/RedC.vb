Imports System.Runtime.InteropServices : Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedC_Basics : Inherits TaskParent
        Public rcMapIDs As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Public rcList As New List(Of rcData) ' includes cloud data.
        Dim flood As New Flood_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(3) = "rcIndexMap version of cells.  Age is shown for the largest cells."
            desc = "Segment the image based on color."
        End Sub
        Public Shared Function displayCell(rclist As List(Of rcData), clickIndex As Integer) As String
            Dim displayStr As String = "There is no cell defined for that point."
            For Each rc In rclist
                If rc.index = clickIndex Or clickIndex < 0 Then
                    task.rcD = rc
                    task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
                    displayStr = task.rcD.displayCell
                    Exit For
                End If
            Next
            Return displayStr
        End Function
        Public Shared Function rcIndexFind(rclistLast As List(Of rcData), rcIndex As Integer) As rcData
            For Each rc In rclistLast
                If rc.index = rcIndex Then Return rclistLast(rclistLast.IndexOf(rc))
            Next
            Return Nothing
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rcListLast = New List(Of rcData)(rcList)
            Dim rcIndexMapLast = rcIndexMap.Clone
            Dim rcMapIDsLast = flood.dst1

            If src.Channels <> 1 Then
                Static color8u As New Color8U_Basics
                color8u.Run(task.gray)
                src = color8u.dst2
            End If

            flood.Run(src)
            dst2 = flood.dst2
            rcList.Clear()
            Dim usedList As New List(Of Single)({0})
            Dim reusedIndex As Integer
            For i = 0 To flood.rectList.Count - 1
                Dim floodVal = flood.indexList(i)
                Dim r = flood.rectList(i)
                Dim rc As New rcData(flood.mask(r), r, floodVal)
                rc.mapID = flood.dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)

                Dim oldMapID = rcMapIDsLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If rc.mapID = oldMapID Then
                    Dim previousIndex = rcIndexMapLast.Get(Of Single)(rc.maxDist.Y, rc.maxDist.X)
                    If usedList.Contains(previousIndex) = False And previousIndex <> 0 Then
                        rc.index = previousIndex
                        usedList.Add(rc.index)
                        reusedIndex += 1
                    End If
                End If
                rcList.Add(rc)
            Next

            Dim nextIndex As Integer
            rcIndexMap.SetTo(0)
            For i = rcList.Count - 1 To 0 Step -1
                Dim rc = rcList(i)
                If rc.index = 0 Then
                    While usedList.Contains(nextIndex)
                        nextIndex += 1
                    End While

                    rc.index = nextIndex
                    usedList.Add(nextIndex)
                    rc.age = 1
                Else
                    Dim rclast = rcIndexFind(rcListLast, rc.index)
                    If rclast IsNot Nothing Then
                        rc.age = rclast.age + 1
                        If rc.age >= 1000 Then rc.age = 100
                    Else
                        rc.age = 1
                    End If
                End If
                rcIndexMap(rc.rect).SetTo(rc.index Mod 255, rc.mask)
            Next

            SetTrueText(displayCell(rcList, rcIndexMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)), 1)

            If task.rcD IsNot Nothing Then
                SetTrueText(CStr(task.rcD.age), task.rcD.maxDist)
                Circle(dst2, task.rcD.maxDist, task.DotSize + 1, white, -1)
                Circle(dst2, task.rcD.maxDStable, task.DotSize + 1, black, -1)
                Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            End If

            For i = 0 To Math.Min(rcList.Count, 10) - 1
                SetTrueText(CStr(rcList(i).age), rcList(i).maxDStable, 3)
            Next

            dst3 = Palettize(rcIndexMap, 0)

            labels(2) = CStr(rcList.Count) + " cells were found and " + CStr(reusedIndex) + " were able to reuse the index."
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






    Public Class RedC_TrackHull : Inherits TaskParent
        Dim redC As New RedC_Basics
        Dim lastCenter As cv.Point
        Dim lastMapID As Byte
        Dim lastRect As cv.Rect
        Public Sub New()
            task.gOptions.showMyDst1.Checked = True
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

            SetTrueText(RedC_Basics.displayCell(redC.rcList, redC.rcIndexMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)), 1)

            dst3.SetTo(0)
            task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
            FillPoly(dst3(task.rcD.rect), {task.rcD.hull}, task.scalarColors(task.rcD.mapID + 1))
            dst3(task.rcD.rect).SetTo(task.scalarColors(task.rcD.mapID), task.rcD.mask)
            Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            Circle(dst1, lastCenter, task.DotSize + 1, task.highlight, -1)
            SetTrueText(task.rcD.displayCell() + vbCrLf, 1)

            lastCenter = Utility_Basics.ComputeHullCentroid(task.rcD.hull.ToArray, task.rcD)
            lastMapID = task.rcD.mapID
            lastRect = task.rcD.rect
        End Sub
    End Class





    Public Class XR_RedC_NeighborHulls : Inherits TaskParent
        Dim redC As New RedC_Basics
        Dim clickPoint As cv.Point
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
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







    Public Class RedC_MergeCells : Inherits TaskParent
        Dim nabe As New RedC_NeighborHist
        Public merged As New rcData
        Public mergeList As New List(Of rcData)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
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
            Dim rcD = task.rcD
            If rcD Is Nothing OrElse nabe.redC.rcList.Count <= 1 Then
                dst3 = nabe.dst3
                labels(3) = "No selected cell to merge."
                Exit Sub
            End If

            Dim depth0 = cellDepth(rcD)
            mergeList.Add(rcD)
            For Each rc In nabe.neighbors
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
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst0 = New Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0))
            desc = "Cursor.ai: Display the depth of each cell using the same colors as the DepthColorizer_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            dst0.SetTo(0)
            Dim depthCount As Integer
            For Each rc In redC.rcList
                Dim depth8u = CByte(Math.Min(255, rc.depth * 255.0 / task.MaxZmeters))
                dst0(rc.rect).SetTo(depth8u, rc.mask)
                depthCount += 1
            Next

            ApplyColorMap(dst0, dst3, task.colorMapDepth)
            dst3.SetTo(0, task.noDepthMask)

            SetTrueText(RedC_Basics.displayCell(redC.rcList, redC.rcIndexMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)), 1)

            SetTrueText(task.rcD.displayCell() + vbCrLf + "Mean depth = " + task.rcD.depth.ToString(fmt2) + "m", 1)
            task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
            dst2(task.rcD.rect).SetTo(task.highlight, task.rcD.mask)
            Rectangle(dst3, task.rcD.rect, task.highlight, task.lineWidth)

            labels(3) = CStr(depthCount) + " cells colored by mean depth (0-" + task.MaxZmeters.ToString(fmt0) + "m DepthColorizer palette)"
        End Sub
    End Class







    Public Class XR_RedC_SteadyCam : Inherits TaskParent
        Dim steady As New SteadyCam_Basics_TA
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




    Public Class XR_RedC_Smoothing : Inherits TaskParent
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




    Public Class RedC_CellLines : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            desc = "Find any lines connected to a cell contour."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.lines.lpList.Count = 0 Then Exit Sub

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim histogram As New Mat
            Dim bins = task.lines.lpList.Count
            Dim ranges = {New Rangef(0, bins)}
            Dim histArray(bins) As Single
            dst3 = dst2.Clone
            For Each rc In redC.rcList
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







    Public Class RedC_DepthMerge : Inherits TaskParent
        Public redC As New RedC_Basics
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(3) = "rcIndexMap version of cells"
            desc = "Cursor.ai: Merge neighboring RedC color cells when their min/max depths overlap."
        End Sub
        Private Shared Function cellDepthRange(rc As rcData) As mmData
            Dim mm As mmData
            If rc Is Nothing OrElse rc.mask.Width <= 1 OrElse rc.pixels = 0 Then Return mm
            Dim depthMask As New Mat
            BitwiseAnd(rc.mask, task.depthmask(rc.rect), depthMask)
            If CountNonZero(depthMask) = 0 Then Return mm
            Return GetMinMax(task.pcSplit(2)(rc.rect), depthMask)
        End Function
        Private Shared Function findRoot(parent() As Integer, i As Integer) As Integer
            If parent(i) <> i Then parent(i) = findRoot(parent, parent(i))
            Return parent(i)
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            dst3 = redC.dst3
            labels(2) = redC.labels(2)
            labels(3) = redC.labels(3)
            Dim n = redC.rcList.Count
            If n = 0 Then Exit Sub

            rcIndexMap = redC.rcIndexMap
            dst3 = dst2.Clone
            Exit Sub

            Dim minZ(n - 1) As Single, maxZ(n - 1) As Single
            For i = 1 To n - 1
                Dim mmZ = cellDepthRange(redC.rcList(i))
                minZ(i) = CSng(mmZ.minVal)
                maxZ(i) = CSng(mmZ.maxVal)
            Next

            Dim nabes(n - 1) As HashSet(Of Integer)
            For i = 0 To n - 1
                nabes(i) = New HashSet(Of Integer)
            Next
            Dim w = redC.rcIndexMap.Width, h = redC.rcIndexMap.Height
            Dim mapData(w * h - 1) As Single
            redC.rcIndexMap.GetArray(Of Single)(mapData)
            For y = 0 To h - 1
                Dim row = y * w
                For x = 0 To w - 1
                    Dim a = CInt(mapData(row + x))
                    If a <= 0 OrElse a >= n Then Continue For
                    If x + 1 < w Then
                        Dim b = CInt(mapData(row + x + 1))
                        If b > 0 AndAlso b < n AndAlso a <> b Then
                            nabes(a).Add(b)
                            nabes(b).Add(a)
                        End If
                    End If
                    If y + 1 < h Then
                        Dim b = CInt(mapData(row + w + x))
                        If b > 0 AndAlso b < n AndAlso a <> b Then
                            nabes(a).Add(b)
                            nabes(b).Add(a)
                        End If
                    End If
                Next
            Next

            Dim parent(n - 1) As Integer
            For i = 0 To n - 1
                parent(i) = i
            Next
            Dim slack = task.depthDiffMeters
            For i = 1 To n - 1
                If maxZ(i) <= 0 Then Continue For
                For Each j In nabes(i)
                    If j <= i OrElse maxZ(j) <= 0 Then Continue For
                    If minZ(i) <= maxZ(j) + slack AndAlso minZ(j) <= maxZ(i) + slack Then
                        Dim ra = findRoot(parent, i)
                        Dim rb = findRoot(parent, j)
                        If ra <> rb Then parent(ra) = rb
                    End If
                Next
            Next

            Dim groups As New Dictionary(Of Integer, List(Of rcData))
            For i = 1 To n - 1
                Dim root = findRoot(parent, i)
                If groups.ContainsKey(root) = False Then groups(root) = New List(Of rcData)
                groups(root).Add(redC.rcList(i))
            Next

            Dim sorted As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            Dim fullMask As New Mat(dst2.Size, MatType.CV_8U, 0)
            For Each members In groups.Values
                fullMask.SetTo(0)
                Dim unionRect = members(0).rect
                Dim biggest = members(0)
                For Each rc In members
                    unionRect = unionRect.Union(rc.rect)
                    fullMask(rc.rect).SetTo(255, rc.mask)
                    If rc.pixels > biggest.pixels Then biggest = rc
                Next
                unionRect = ValidateRect(unionRect)
                Dim merged As New rcData(fullMask(unionRect), unionRect, -1) With {.mapID = biggest.mapID}
                If merged.pixels > 0 Then sorted.Add(merged.pixels, merged)
            Next

            Dim mm = cellDepthRange(task.rcD)
            strOut = RedC_Basics.displayCell(redC.rcList, redC.rcIndexMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X))
            SetTrueText(strOut + "Mean depth = " + task.rcD.depth.ToString(fmt2) + "m" + vbCrLf +
                        "Depth range = " + mm.minVal.ToString(fmt2) + " to " +
                        mm.maxVal.ToString(fmt2) + "m", 1)

            Rectangle(dst3, task.rcD.rect, task.highlight, task.lineWidth)
            Circle(dst3, task.rcD.maxDist, task.DotSize + 1, white, -1)

            labels(3) = CStr(redC.rcList.Count) + " cells after merging neighbors of " +
                    CStr(n) + " color cells with overlapping depth"
        End Sub
    End Class





    Public Class XR_RedC_TrackCellOld : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            task.gOptions.showMyDst1.Checked = True
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
            dst2 = redC.dst3
            labels(2) = redC.labels(2)
            If task.rcD Is Nothing Then Exit Sub

            'Dim mapID = redC.flood.dst1.Get(Of Byte)(task.rcD.maxDStable.Y, task.rcD.maxDStable.X)
            'If mapID <> task.rcD.mapID Then
            '    task.rcD = rcDFindCell(task.rcD)
            '    task.rcD.maxDStable = task.rcD.maxDist
            'End If

            'Dim index = redC.maxDStableList.IndexOf(task.rcD.maxDStable)
            'dst3.SetTo(0)
            'If index > 0 Then
            '    task.rcD = redC.rcList(index)
            'Else
            '    Dim rcD = rcDFindCell(task.rcD)
            '    If rcD IsNot Nothing Then task.rcD = rcD
            'End If

            'task.clickPoint = task.rcD.maxDStable

            'task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
            'dst3(task.rcD.rect).SetTo(task.scalarColors(task.rcD.mapID), task.rcD.mask)
            'Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            'Circle(dst3, task.rcD.maxDStable, task.DotSize + 1, white, -1)
            'Circle(dst3, task.rcD.maxDist, task.DotSize + 2, task.highlight, -1)

            'strOut = task.rcD.displayCell() + vbCrLf + vbCrLf + "Track point " + task.clickPoint.ToString + vbCrLf
            'SetTrueText(strOut, 1)
        End Sub
    End Class





    Public Class RedC_Features : Inherits TaskParent
        Dim feat As New Feature_Basics
        Dim redC As New RedC_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(1) = "Ages for the top X lines..."
            desc = "Find the features in a RedC cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            feat.Run(src)
            dst3 = feat.dst2
            labels(3) = feat.labels(2)

            For Each lp In task.lines.lpList
                Line(dst3, lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
                If lp.index < 10 Then SetTrueText(CStr(lp.age), lp.ptCenter, 1)
            Next
        End Sub
    End Class






    Public Class XR_RedC_NeighborHist : Inherits TaskParent
        Public redC As New RedC_Basics
        Dim lastCenter As cv.Point
        Public rcD As rcData
        Public neighbors As New List(Of rcData)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Use a histogram to find the neighbors.  Not working..."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rcListLast = New List(Of rcData)(redC.rcList)

            If task.heartBeatLT Then dst1.SetTo(0)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            If task.mouseClickFlag Then lastCenter = task.clickPoint
            Dim index As Integer = redC.rcIndexMap.Get(Of Single)(lastCenter.Y, lastCenter.X)

            If index >= 0 Then
                rcD = RedC_Basics.rcIndexFind(rcListLast, index)
            Else
                Dim rect As New cv.Rect(lastCenter.X, lastCenter.Y, task.gridWH, task.gridWH)
                Dim myMapID As Integer = redC.rcMapIDs.Get(Of Single)(lastCenter.Y, lastCenter.X)
                For Each rc In redC.rcList
                    If rc.mapID = myMapID And rc.rect.IntersectsWith(rect) Then
                        rcD = rc
                        Exit For
                    End If
                Next
            End If
            If rcD Is Nothing Then rcD = redC.rcList(0)
            SetTrueText(rcD.displayCell() + vbCrLf, 1)

            Dim histogram As New Mat, tmp As New cv.Mat
            Dim ranges() As Rangef = New Rangef() {New Rangef(0, redC.rcList.Count + 1)}
            Dim delta = task.gridWH / 2
            Dim r = New cv.Rect(rcD.rect.X - delta, rcD.rect.Y - delta, rcD.rect.Width + task.gridWH, rcD.rect.Height + task.gridWH)
            r = ValidateRect(r)
            CalcHist({redC.rcIndexMap(r)}, {0}, New Mat, histogram, 1, {redC.rcList.Count}, ranges)

            Dim histArray(histogram.Rows - 1) As Single
            histogram.GetArray(Of Single)(histArray)

            neighbors.Clear()

            For i = 1 To histArray.Length - 1
                If histArray(i) > 0 Then neighbors.Add(redC.rcList(i))
            Next

            dst3.SetTo(0)
            For i = 0 To neighbors.Count - 1
                dst3(neighbors(i).rect).SetTo(task.scalarColors(neighbors(i).mapID), neighbors(i).mask)
            Next

            dst3(rcD.rect).SetTo(task.highlight, rcD.mask)
            Rectangle(dst3, r, task.highlight, task.lineWidth)
            Rectangle(dst2, r, task.highlight, task.lineWidth)
            labels(3) = CStr(neighbors.Count) + " neighbors were present."

            lastCenter = rcD.maxDStable
            Circle(dst1, lastCenter, task.DotSize + 1, task.highlight, -1)
        End Sub
    End Class






    Public Class RedC_NeighborHist : Inherits TaskParent
        Public redC As New RedC_Basics
        Public neighbors As New List(Of rcData)
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Use rect intersections to find the neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rcListLast = New List(Of rcData)(redC.rcList)

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            If redC.rcList.Count = 0 Then Exit Sub
            If task.rcD Is Nothing Then task.rcD = redC.rcList(0)

            Dim index As Integer = redC.rcIndexMap.Get(Of Single)(task.rcD.maxDist.Y, task.rcD.maxDist.X)

            neighbors.Clear()
            For Each rc In redC.rcList
                If task.rcD.rect.IntersectsWith(rc.rect) Then neighbors.Add(rc)
            Next
            SetTrueText(task.rcD.displayCell() + vbCrLf, 1)

            dst3.SetTo(0)
            For i = 0 To neighbors.Count - 1
                dst3(neighbors(i).rect).SetTo(task.scalarColors(neighbors(i).mapID), neighbors(i).mask)
            Next

            dst3(task.rcD.rect).SetTo(task.highlight, task.rcD.mask)
            Rectangle(dst3, task.rcD.rect, task.highlight, task.lineWidth)
            Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            labels(3) = CStr(neighbors.Count) + " neighbors were present."
        End Sub
    End Class





    Public Class RedC_FeatureLess1 : Inherits TaskParent
        Dim redC As New RedC_Basics
        Dim fLess As New FeatureLess_Core
        Public merged As New rcData
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Cursor.ai: Merge RedC cells under the largest FeatureLess_ToList cell using CalcHist on rcIndexMap."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            fLess.Run(src)
            dst3 = dst2.Clone

            If fLess.rcList.Count = 0 OrElse redC.rcList.Count = 0 Then
                labels(3) = "No FeatureLess or RedC cells to merge."
                Exit Sub
            End If

            Dim flRc = fLess.rcList(0)
            Dim histogram As New Mat
            Dim ranges() As Rangef = {New Rangef(0, 256)}
            CalcHist({redC.rcIndexMap(flRc.rect)}, {0}, flRc.mask, histogram, 1, {256}, ranges)
            Dim histArray(histogram.Rows - 1) As Single
            histogram.GetArray(Of Single)(histArray)

            Dim members As New List(Of rcData)
            For Each rc In redC.rcList
                Dim bin = rc.index Mod 255
                If bin > 0 AndAlso bin < histArray.Length AndAlso histArray(bin) > 0 Then members.Add(rc)
            Next

            If members.Count = 0 Then
                labels(3) = "No RedC cells under the largest FeatureLess cell."
                Exit Sub
            End If

            Dim unionRect = members(0).rect
            Dim fullMask As New Mat(dst2.Size, MatType.CV_8U, 0)
            For Each rc In members
                unionRect = unionRect.Union(rc.rect)
                fullMask(rc.rect).SetTo(255, rc.mask)
            Next
            unionRect = ValidateRect(unionRect)
            merged = New rcData(fullMask(unionRect), unionRect, 255)

            dst3(merged.rect).SetTo(task.highlight, merged.mask)
            Rectangle(dst3, merged.rect, task.highlight, task.lineWidth)
            Circle(dst3, merged.maxDist, task.DotSize + 1, white, -1)

            labels(3) = CStr(members.Count) + " RedC cells merged from the largest FeatureLess cell"
            SetTrueText(merged.displayCell, 1)
        End Sub
    End Class
End Namespace