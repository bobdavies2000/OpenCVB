Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedC_BasicsNew : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public Sub New()
            labels(3) = "The output of FeatureLess_Basics.  Note that cell colors match the RedColor output."
            desc = "Use the FeatureLess regions to improve the RedColor output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rcLastList As New List(Of rcData)(rcList)
            Dim rcIndexList As New List(Of Integer)
            For Each rc In rcLastList
                Dim tmp As New cv.Mat(rc.mask.Size, cv.MatType.CV_8U, 0)
                task.motion.motionMask(rc.rect).CopyTo(tmp, rc.mask)
                rc.motionPixels = CountNonZero(tmp)
                rcIndexList.Add(rc.ID)
            Next

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
                rc.ID = task.fless.dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If rc.motionPixels = 0 Then
                    Dim index = rcIndexList.IndexOf(rc.ID)
                    If index >= 0 Then rc = rcLastList(index)
                End If
                rc.index = rcList.Count
                rcList.Add(rc)
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
            Next

            dst2 = Palettize(rcMap, 0)
            dst3 = task.fless.dst2

            labels(2) = CStr(rcList.Count) + " cells were identified."
        End Sub
    End Class






    Public Class RedC_Basics : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            If task.redColor Is Nothing Then task.redColor = New RedColor_Basics
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Track the FeatureLess cells points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If heartbeatLTX() Then dst1 = task.delaunay.dst2

            rcMap.SetTo(0)
            dst3.SetTo(0)
            rcList.Clear()
            rcList.Add(New rcData) ' placeholder for zero...
            For Each rc In task.redColor.rcList
                Dim ptx = WarpAffine_Basics.WarpPoint(rc.maxDist, task.steadyCam.M)
                ptx = validatePoint(ptx)
                rc.ID = task.delaunay.idMap.Get(Of Byte)(ptx.Y, ptx.X)
                Line(dst3, rc.maxDist, ptx, white, task.lineWidth)
                SetTrueText(CStr(rc.age), rc.maxDist, 2)
                rcList.Add(rc)

                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
            Next

            dst2 = Palettize(rcMap, 0)
            labels(2) = CStr(task.redColor.rcList.Count) + " were mapped to " + CStr(task.delaunay.ptList.Count) + " cells"
            labels(3) = CStr(task.redColor.rcList.Count) + " cells were found"

            If task.firstPass And rcList.Count > 1 Then task.clickPoint = rcList(1).maxDist
            Dim rcNext = FeatureLess_Basics.rcFinder(rcMap, rcList)
            If rcNext IsNot Nothing Then task.rcD = rcNext
            Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            SetTrueText(task.rcD.displayCell, 3)
        End Sub
    End Class






    Public Class XR_RedC_Core : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Dim redColor As New RedColor_Basics
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            desc = "Use CalcHist on FeatureLess_Tracker cells to merge Color8U floodfill regions."
        End Sub
        Public Shared Function displayCell(rclist As List(Of rcData), clickIndex As Integer) As String
            Dim displayStr As String = "There is no cell defined for that point."
            For Each rc In rclist
                If rc.ID = clickIndex Or clickIndex < 0 Then
                    task.rcD = rc
                    task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
                    displayStr = task.rcD.displayCell
                    Exit For
                End If
            Next
            Return displayStr
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastMap = rcMap.Clone

            redColor.Run(src)

            Dim rc As rcData
            If standaloneTest() Then
                labels(3) = redColor.labels(3)
                dst3 = redColor.dst2.Clone
                For Each rc In redColor.tracker.rcList
                    If rc.ID = 0 Then Continue For
                    DrawContours(dst3(rc.rect), {rc.contour}, 0, task.highlight, task.lineWidth)
                Next
            End If

            Dim ranges() As Rangef = {New Rangef(0, redColor.rcList.Count)}
            Dim histogram As New Mat

            Dim rcFLess = New List(Of rcData)(redColor.tracker.rcList)
            Dim rcOwner(redColor.rcList.Count - 1) As Integer
            Dim histArray() As Single = Nothing

            For Each rc In rcFLess
                If rc.ID = 0 Then Continue For

                CalcHist({redColor.rcMap(rc.rect)}, {0}, rc.mask, histogram, 1, {redColor.rcList.Count}, ranges)
                histogram.GetArray(Of Single)(histArray)

                For i = 1 To histArray.Length - 1
                    If histArray(i) > 0 And rcOwner(i) = 0 Then
                        rcOwner(i) = rc.ID
                    End If
                Next
            Next

            rcMap.SetTo(0)
            For Each rc In rcFLess
                If rc.ID = 0 Then Continue For
                For i = 1 To redColor.rcList.Count - 1
                    If rcOwner(i) = 0 Then Continue For
                    If rcOwner(i) = rc.ID Then
                        Dim rcTuple = redColor.rcList(i)
                        rc.rect = rc.rect.Union(rcTuple.rect)
                        rcMap(rcTuple.rect).SetTo(rcOwner(i), rcTuple.mask)
                    End If
                Next
            Next

            ' update all the rc.mask's with the merged cells.
            rcList.Clear()
            For Each rc In rcFLess
                If rc.ID = 0 Then Continue For
                InRange(rcMap(rc.rect), rc.ID, rc.ID, rc.mask)
                rc.contour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxSimple)
                Dim val1 = rcMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                Dim val2 = rcMap.Get(Of Byte)(rc.maxDStable.Y, rc.maxDStable.X)
                If val1 <> val2 Then rc.maxDStable = rc.maxDist
                SetTrueText(CStr(rc.ID), rc.maxDist, 2)
                rc.index = rcList.Count
                rcList.Add(rc)
            Next

            dst2 = Palettize(rcMap, 0)

            labels(2) = CStr(redColor.rcList.Count) + " input cells merged into the " +
                        CStr(rcList.Count - 1) + " featureless cells."
        End Sub
    End Class





    Public Class XR_RedC_CoreOld : Inherits TaskParent
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcList As New List(Of rcData) ' includes cloud data.
        Dim flood As New Flood_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            If standalone Then task.gOptions.showMyDst1.Checked = True
            labels(3) = "rcMap version of cells."
            desc = "Segment the image based on color."
        End Sub
        Public Shared Function displayCell(rclist As List(Of rcData), clickIndex As Integer) As String
            Dim displayStr As String = "There is no cell defined for that point."
            For Each rc In rclist
                If rc.ID = clickIndex Or clickIndex < 0 Then
                    task.rcD = rc
                    task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
                    displayStr = task.rcD.displayCell
                    Exit For
                End If
            Next
            Return displayStr
        End Function
        Public Shared Function rcIndexFind(rclist As List(Of rcData), rcIndex As Integer) As rcData
            For Each rc In rclist
                If rc.ID = rcIndex Then Return rclist(rclist.IndexOf(rc))
            Next
            Return Nothing
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim rcListLast = New List(Of rcData)(rcList)
            Dim rcidMapLast = rcMap.Clone

            If src.Channels <> 1 Then
                Static color8u As New Color8U_Basics
                color8u.Run(task.gray)
                src = color8u.dst2
            End If

            flood.Run(src)
            dst2 = flood.dst2
            rcList.Clear()
            rcList.Add(New rcData) ' placeholder for zero...
            For i = 0 To flood.rectList.Count - 1
                Dim floodVal = flood.indexList(i)
                Dim r = flood.rectList(i)
                Dim rc As New rcData(flood.mask(r), r, floodVal Mod 256) With {.ID = i + 1}
                rc.ID = i
                rc.index = i
                rcList.Add(rc)
            Next

            rcMap.SetTo(0)
            For i = rcList.Count - 1 To 0 Step -1
                Dim rc = rcList(i)
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
            Next

            SetTrueText(displayCell(rcList, rcMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)), 1)

            If task.rcD IsNot Nothing Then
                Circle(dst2, task.rcD.maxDist, task.DotSize + 1, white, -1)
                Circle(dst2, task.rcD.maxDStable, task.DotSize + 1, black, -1)
                ' Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            End If

            dst3 = Palettize(rcMap, 0)

            labels(2) = CStr(rcList.Count) + " cells were found "
        End Sub
    End Class






    Public Class XR_RedC_MergeCells : Inherits TaskParent
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

            merged = New rcData() With {.rect = unionRect, .mask = fullMask(unionRect).Clone(),
                                        .maxDStable = merged.maxDist}
            dst3.SetTo(0)
            For Each rc In mergeList
                dst3(rc.rect).SetTo(task.scalarColors(rc.ID), rc.mask)
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
        Dim redColor As New RedColor_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            dst0 = New Mat(dst0.Size(), MatType.CV_8U, Scalar.All(0))
            desc = "Cursor.ai: Display the depth of each cell using the same colors as the DepthColorizer_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redColor.Run(src)
            dst2 = redColor.dst2
            labels(2) = redColor.labels(2)

            dst0.SetTo(0)
            Dim depthCount As Integer
            For Each rc In redColor.rcList
                Dim depth8u = CByte(Math.Min(255, rc.depth * 255.0 / task.MaxZmeters))
                dst0(rc.rect).SetTo(depth8u, rc.mask)
                depthCount += 1
            Next

            ApplyColorMap(dst0, dst3, task.colorMapDepth)
            dst3.SetTo(0, task.noDepthMask)

            If task.firstPass Then task.rcD = redColor.rcList(1)
            SetTrueText(XR_RedC_Core.displayCell(redColor.rcList, redColor.rcMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X)), 1)

            SetTrueText(task.rcD.displayCell() + vbCrLf + "Mean depth = " + task.rcD.depth.ToString(fmt2) + "m", 1)
            task.color(task.rcD.rect).SetTo(white, task.rcD.mask)
            dst2(task.rcD.rect).SetTo(task.highlight, task.rcD.mask)
            Rectangle(dst3, task.rcD.rect, task.highlight, task.lineWidth)

            labels(3) = CStr(depthCount) + " cells colored by mean depth (0-" + task.MaxZmeters.ToString(fmt0) + "m DepthColorizer palette)"
        End Sub
    End Class





    Public Class RedC_CellLines : Inherits TaskParent
        Dim redC As New XR_RedC_Core
        Public Sub New()
            If task.lines Is Nothing Then task.lines = New Line_Basics_TA
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
                Dim lpList As New List(Of Integer)
                For i = 1 To bins - 1
                    If histArray(i) > 0 Then lpList.Add(i)
                Next

                For Each index In lpList
                    Dim lp = task.lines.lpList(index - 1)
                    Line(dst3, lp.p1, lp.p2, task.highlight, task.lineWidth + 1, task.lineType)
                Next
            Next
        End Sub
    End Class







    Public Class XR_RedC_DepthMerge : Inherits TaskParent
        Public redC As New XR_RedC_Core
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public Sub New()
            labels(3) = "rcMap version of cells"
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

            rcMap = redC.rcMap
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
            Dim w = redC.rcMap.Width, h = redC.rcMap.Height
            Dim mapData(w * h - 1) As Byte
            redC.rcMap.GetArray(Of Byte)(mapData)
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
                Dim merged As New rcData(fullMask(unionRect), unionRect, -1)
                If merged.pixels > 0 Then sorted.Add(merged.pixels, merged)
            Next

            Dim mm = cellDepthRange(task.rcD)
            strOut = XR_RedC_Core.displayCell(redC.rcList, redC.rcMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X))
            SetTrueText(strOut + "Mean depth = " + task.rcD.depth.ToString(fmt2) + "m" + vbCrLf +
                        "Depth range = " + mm.minVal.ToString(fmt2) + " to " +
                        mm.maxVal.ToString(fmt2) + "m", 1)

            Rectangle(dst3, task.rcD.rect, task.highlight, task.lineWidth)
            Circle(dst3, task.rcD.maxDist, task.DotSize + 1, white, -1)

            labels(3) = CStr(redC.rcList.Count) + " cells after merging neighbors of " +
                    CStr(n) + " color cells with overlapping depth"
        End Sub
    End Class




    Public Class RedC_Features : Inherits TaskParent
        Dim feat As New Feature_Basics
        Dim redC As New XR_RedC_Core
        Public Sub New()
            If task.lines Is Nothing Then task.lines = New Line_Basics_TA
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

            Dim index As Integer = redC.rcMap.Get(Of Single)(task.rcD.maxDist.Y, task.rcD.maxDist.X)

            neighbors.Clear()
            For Each rc In redC.rcList
                If task.rcD.rect.IntersectsWith(rc.rect) Then neighbors.Add(rc)
            Next
            SetTrueText(task.rcD.displayCell() + vbCrLf, 1)

            dst3.SetTo(0)
            For i = 0 To neighbors.Count - 1
                dst3(neighbors(i).rect).SetTo(task.scalarColors(neighbors(i).index Mod 255), neighbors(i).mask)
            Next

            dst3(task.rcD.rect).SetTo(task.highlight, task.rcD.mask)
            Rectangle(dst3, task.rcD.rect, task.highlight, task.lineWidth)
            Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            labels(3) = CStr(neighbors.Count) + " neighbors were present."
        End Sub
    End Class





    Public Class XR_RedC_FeatureLessTestSingle : Inherits TaskParent
        Dim flood As New RedC_FloodToRC
        Dim redC As New RedC_Basics
        Public merged As New rcData
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Cursor.ai: Merge flood cells under the largest FeatureLess_ToList cell using CalcHist on rcMap."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            flood.Run(src)
            dst2 = flood.dst2
            labels(2) = flood.labels(2)

            redC.Run(src)
            dst3 = dst2.Clone

            If redC.rcList.Count = 0 OrElse flood.rcList.Count = 0 Then
                labels(3) = "No FeatureLess or flood cells to merge."
                Exit Sub
            End If

            Dim flRc = redC.rcList(1)
            Dim histogram As New Mat
            Dim ranges() As Rangef = {New Rangef(0, 256)}
            CalcHist({flood.rcMap(flRc.rect)}, {0}, flRc.mask, histogram, 1, {256}, ranges)
            Dim histArray(histogram.Rows - 1) As Single
            histogram.GetArray(Of Single)(histArray)

            Dim members As New List(Of rcData)
            For Each rc In flood.rcList
                Dim bin = rc.ID Mod 255
                If bin > 0 AndAlso bin < histArray.Length AndAlso histArray(bin) > 0 Then members.Add(rc)
            Next

            If members.Count = 0 Then
                labels(3) = "No flood cells under the largest FeatureLess cell."
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

            labels(3) = CStr(members.Count) + " flood cells merged from the largest FeatureLess cell"
            SetTrueText(merged.displayCell, 1)
        End Sub
    End Class





    Public Class RedC_FloodToRC : Inherits TaskParent
        Public rcMap As New Mat(dst2.Size, MatType.CV_8U, 0)
        Public rcList As New List(Of rcData) ' includes cloud data.
        Dim flood As New Flood_Basics
        Public Sub New()
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Build an rcList from the flood data and present smalles to largest."
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

            For Each rc In rcList
                If rc.ID = 0 Then Continue For
                rcMap(rc.rect).SetTo(rc.ID, rc.mask)
            Next
            labels(2) = CStr(sortList.Values.Count) + " cells were found "

            If task.rcD IsNot Nothing Then
                Circle(dst2, task.rcD.maxDist, task.DotSize + 1, white, -1)
                Circle(dst2, task.rcD.maxDStable, task.DotSize + 1, black, -1)
                ' Rectangle(dst2, task.rcD.rect, task.highlight, task.lineWidth)
            End If

            dst3 = Palettize(rcMap, 0)
            labels(3) = CStr(rcList.Count - 1) + " cells found reordered and assigned an index."
        End Sub
    End Class




    Public Class RedC_Cells : Inherits TaskParent
        Dim redC As New RedC_Basics
        Public Sub New()
            desc = "Break down the cells withing each RedC cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redC.Run(task.gray)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
        End Sub
    End Class

End Namespace
