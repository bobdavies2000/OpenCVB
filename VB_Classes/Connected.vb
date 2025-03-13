Imports cv = OpenCvSharp
Public Class Connected_Basics : Inherits TaskParent
    Public hTuples As New List(Of Tuple(Of Integer, Integer))
    Public vTuples As New List(Of Tuple(Of Integer, Integer))
    Public width As Integer, height As Integer
    Dim colStart As Integer, colEnd As Integer, colorIndex As Integer
    Dim rowStart As Integer, bottomRight As cv.Point, topLeft As cv.Point
    Public Sub New()
        desc = "Connect cells that are close in depth"
    End Sub
    Private Sub hTestRect(idd1 As gridCell, idd2 As gridCell, nextStart As Integer)
        If Math.Abs(idd1.depth - idd2.depth) > task.depthDiffMeters Or nextStart = -1 Then
            Dim p1 = task.iddList(colStart).cRect.TopLeft
            Dim p2 = task.iddList(colEnd).cRect.BottomRight
            dst2.Rectangle(p1, p2, task.scalarColors(colorIndex Mod 256), -1)
            colorIndex += 1
            hTuples.Add(New Tuple(Of Integer, Integer)(colStart, colEnd))
            colStart = nextStart
            colEnd = colStart
        Else
            colEnd += 1
        End If
    End Sub
    Private Sub vTestRect(idd1 As gridCell, idd2 As gridCell, iddNext As Integer, nextStart As Integer)
        If Math.Abs(idd1.depth - idd2.depth) > task.depthDiffMeters Or nextStart = -1 Then
            bottomRight = task.iddList(iddNext).cRect.BottomRight
            dst3.Rectangle(topLeft, bottomRight, task.scalarColors(colorIndex Mod 256), -1)
            colorIndex += 1
            vTuples.Add(New Tuple(Of Integer, Integer)(rowStart, iddNext))
            rowStart = nextStart
            If nextStart >= 0 Then topLeft = task.iddList(rowStart).cRect.TopLeft
        End If
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2.SetTo(0)
        dst3.SetTo(0)

        width = dst2.Width / task.cellSize
        If width * task.cellSize <> dst2.Width Then width += 1
        height = Math.Floor(dst2.Height / task.cellSize)
        If height * task.cellSize <> dst2.Height Then height += 1
        hTuples.Clear()
        colorIndex = 0
        For i = 0 To height - 1
            colStart = i * width
            colEnd = colStart
            For j = 0 To width - 2
                hTestRect(task.iddList(i * width + j), task.iddList(i * width + j + 1), i * width + j + 1)
            Next
            hTestRect(task.iddList(i * width + height - 1), task.iddList(i * width + height - 1), -1)
        Next
        labels(2) = CStr(colorIndex) + " horizontal slices were connected because cell depth difference < " +
                    CStr(task.depthDiffMeters) + " meters"

        vTuples.Clear()
        Dim index As Integer
        colorIndex = 0
        For i = 0 To width - 1
            rowStart = i
            topLeft = task.iddList(i).cRect.TopLeft
            bottomRight = task.iddList(i + width).cRect.TopLeft
            For j = 0 To height - 2
                index = i + (j + 1) * width
                If index >= task.iddList.Count Then index = task.iddList.Count - 1
                vTestRect(task.iddList(i + j * width), task.iddList(index), i + j * width, index)
            Next
            Dim iddNext = i + (height - 1) * width
            If iddNext >= task.iddList.Count Then iddNext = task.iddList.Count - 1
            vTestRect(task.iddList(iddNext), task.iddList(index), iddNext, -1)
        Next

        labels(3) = CStr(colorIndex) + " vertical slices were connected because cell depth difference < " +
                    CStr(task.depthDiffMeters) + " meters"
    End Sub
End Class





Public Class Connected_Gaps : Inherits TaskParent
    Dim connect As New Connected_Basics
    Public Sub New()
        labels(2) = "Grid cells with single cells removed for both vertical and horizontal connected cells."
        labels(3) = "Vertical cells with single cells removed."
        desc = "Use the horizontal/vertical connected cells to find gaps in depth and the like featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)
        dst2 = connect.dst2
        dst3 = connect.dst3

        For Each tup In connect.hTuples
            If tup.Item2 - tup.Item1 = 0 Then
                Dim idd = task.iddList(tup.Item1)
                dst2(idd.cRect).SetTo(0)
            End If
        Next

        For Each tup In connect.vTuples
            Dim idd1 = task.iddList(tup.Item1)
            Dim idd2 = task.iddList(tup.Item2)
            If idd2.cRect.TopLeft.Y - idd1.cRect.TopLeft.Y = 0 Then
                dst2(idd1.cRect).SetTo(0)
                dst3(idd1.cRect).SetTo(0)
            End If
        Next
    End Sub
End Class







Public Class Connected_Palette : Inherits TaskParent
    Dim hRects As New Connected_RectsH
    Dim vRects As New Connected_RectsV
    Dim mats As New Mat_4Click
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Assign an index to each of vertical and horizontal rects in Connected_Rects"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hRects.Run(src)

        Dim indexH As Integer
        dst1.SetTo(0)
        For Each r In hRects.hRects
            If r.Y = 0 Then
                indexH += 1
                dst1(r).SetTo(indexH)
            Else
                Dim foundLast As Boolean
                For x = r.X To r.X + r.Width - 1
                    Dim lastIndex = dst1.Get(Of Byte)(r.Y - 1, x)
                    If lastIndex <> 0 Then
                        dst1(r).SetTo(lastIndex)
                        foundLast = True
                        Exit For
                    End If
                Next
                If foundLast = False Then
                    indexH += 1
                    dst1(r).SetTo(indexH)
                End If
            End If
        Next
        mats.mat(0) = ShowPalette(dst1)

        mats.mat(1) = ShowAddweighted(src, mats.mat(0), labels(3))

        vRects.Run(src)
        Dim indexV As Integer
        dst1.SetTo(0)
        For Each r In vRects.vRects
            If r.X = 0 Then
                indexV += 1
                dst1(r).SetTo(indexV)
            Else
                Dim foundLast As Boolean
                For y = r.Y To r.Y + r.Height - 1
                    Dim lastIndex = dst1.Get(Of Byte)(y, r.X - 1)
                    If lastIndex <> 0 Then
                        dst1(r).SetTo(lastIndex)
                        foundLast = True
                        Exit For
                    End If
                Next
                If foundLast = False Then
                    indexV += 1
                    dst1(r).SetTo(indexV)
                End If
            End If
        Next
        mats.mat(2) = ShowPalette(dst1)

        mats.mat(3) = ShowAddweighted(src, mats.mat(2), labels(3))
        If task.heartBeat Then labels(2) = CStr(indexV + indexH) + " regions were found that were connected in depth."

        mats.Run(src)
        dst2 = mats.dst2
        dst3 = mats.dst3
    End Sub
End Class







Public Class Connected_Contours : Inherits TaskParent
    Public redM As New RedMask_Basics
    Public connect As New Connected_Rects
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.gOptions.TruncateDepth.Checked = True
        desc = "Find the main regions connected in depth and build a contour for each."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src.Clone)
        task.rcPixelThreshold = task.cellSize * task.cellSize ' eliminate singles...
        redM.Run(Not connect.dst2)

        dst1.SetTo(0)
        For Each md In redM.mdList
            md.contour = ContourBuild(md.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            dst1(md.rect).SetTo(md.index, md.mask)
        Next

        dst2 = ShowPalette(dst1)
        dst2.SetTo(0, connect.dst2)
        dst3 = ShowAddweighted(src, dst2, labels(3))
        If task.heartBeat Then labels(2) = "There were " + CStr(redM.mdList.Count) + " connected contours found."
    End Sub
End Class





Public Class Connected_RedColor : Inherits TaskParent
    Dim connect As New Connected_Contours
    Public Sub New()
        desc = "Color each redCell with the color of the nearest grid cell region."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        dst3 = runRedC(src, labels(3))
        For Each rc In task.rcList
            Dim index = connect.dst1.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            dst2(rc.rect).SetTo(task.scalarColors(index), rc.mask)
        Next
    End Sub
End Class






Public Class Connected_BasicsNewBad : Inherits TaskParent
    Public hTuples As New List(Of Tuple(Of Integer, Integer))
    Public width As Integer, height As Integer
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32F, 0)
        desc = "Connect cells that are close in depth"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim gCells As New List(Of gridCellNew)
        For Each idd In task.iddList
            Dim gc As New gridCellNew
            gc.cRect = idd.cRect
            gc.lRect = idd.lRect
            gc.rRect = idd.rRect

            gc.depth = idd.depth
            gc.pixels = idd.pixels
            gc.mm = idd.mm
            gc.correlation = idd.correlation
            gc.index = gCells.Count
            gCells.Add(gc)
        Next

        width = dst2.Width / task.cellSize
        If width * task.cellSize <> dst2.Width Then width += 1
        height = Math.Floor(dst2.Height / task.cellSize)
        If height * task.cellSize <> dst2.Height Then height += 1

        Dim newCount(gCells.Count - 1) As Integer
        For i = 0 To height - 2
            For j = 1 To width - 1
                Dim gc1 = gCells(i * width + j - 1)
                Dim gc2 = gCells(i * width + j)
                Dim gc3 = gCells((i + 1) * width + j - 1)
                Dim gc4 = gCells((i + 1) * width + j)
                If gc1.index <> gc2.index Then
                    If Math.Abs(gc1.depth - gc2.depth) < task.depthDiffMeters Then
                        gc2.index = gc1.index
                        gCells(i * width + j) = gc2
                        newCount(gc2.index) += 1
                    End If
                End If

                If gc3.index <> gc1.index Then
                    If Math.Abs(gc1.depth - gc3.depth) < task.depthDiffMeters Then
                        gc3.index = gc1.index
                        gCells((i + 1) * width + j - 1) = gc3
                        newCount(gc3.index) += 1
                    End If
                End If

                If gc4.index <> gc2.index Then
                    If Math.Abs(gc2.depth - gc4.depth) < task.depthDiffMeters Then
                        gc4.index = gc2.index
                        gCells((i + 1) * width + j) = gc4
                        newCount(gc4.index) += 1
                    End If
                End If
            Next
        Next

        Dim sortedCounts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To newCount.Count - 1
            sortedCounts.Add(newCount(i), i)
        Next

        Dim indexList As New List(Of Integer)
        For i = 0 To 10 - 1
            Dim index = sortedCounts.ElementAt(i).Value
            indexList.Add(index)
        Next

        dst3.SetTo(0)
        For Each gc In gCells
            If indexList.Contains(gc.index) Then
                dst3(gc.cRect).SetTo(gc.index)
            End If
        Next

        dst2 = ShowPalette(dst3)

        labels(2) = CStr(gCells.Count) + " grid cells consolidated into the top 10 cells."
    End Sub
End Class






Public Class Connected_RectsH : Inherits TaskParent
    Public hRects As New List(Of cv.Rect)
    Dim connect As New Connected_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Connect grid cells with similar depth - horizontally scanning."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        hRects.Clear()
        Dim index As Integer
        For Each tup In connect.hTuples
            If tup.Item1 = tup.Item2 Then Continue For
            Dim idd1 = task.iddList(tup.Item1)
            Dim idd2 = task.iddList(tup.Item2)

            Dim w = idd2.cRect.BottomRight.X - idd1.cRect.TopLeft.X
            Dim h = idd1.cRect.Height

            Dim r = New cv.Rect(idd1.cRect.TopLeft.X + 1, idd1.cRect.TopLeft.Y, w - 1, h)

            hRects.Add(r)
            dst2(r).SetTo(255)

            index += 1
            dst3(r).SetTo(task.scalarColors(index Mod 256))
        Next
    End Sub
End Class






Public Class Connected_RectsV : Inherits TaskParent
    Public vRects As New List(Of cv.Rect)
    Dim connect As New Connected_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Connect grid cells with similar depth - vertically scanning."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        vRects.Clear()
        Dim index As Integer
        For Each tup In connect.vTuples
            If tup.Item1 = tup.Item2 Then Continue For
            Dim idd1 = task.iddList(tup.Item1)
            Dim idd2 = task.iddList(tup.Item2)

            Dim w = idd1.cRect.Width
            Dim h = idd2.cRect.BottomRight.Y - idd1.cRect.TopLeft.Y

            Dim r = New cv.Rect(idd1.cRect.TopLeft.X, idd1.cRect.TopLeft.Y + 1, w, h - 1)
            vRects.Add(r)
            dst2(r).SetTo(255)

            index += 1
            dst3(r).SetTo(task.scalarColors(index Mod 256))
        Next
    End Sub
End Class






Public Class Connected_Rects : Inherits TaskParent
    Dim hConn As New Connected_RectsH
    Dim vConn As New Connected_RectsV
    Public Sub New()
        desc = "Isolate the connected depth grid cells both vertically and horizontally."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hConn.Run(src)
        vConn.Run(src)

        dst2 = (Not vConn.dst2).ToMat Or (Not hConn.dst2).ToMat

        dst3 = src
        dst3.SetTo(0, dst2)
    End Sub
End Class






Public Class Connected_Regions : Inherits TaskParent
    Public redM As New RedMask_Basics
    Public connect As New Connected_Rects
    Public mdLargest As New List(Of maskData)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.gOptions.TruncateDepth.Checked = True
        desc = "Find the main regions connected in depth and build a contour for each."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        connect.Run(src.Clone)
        task.rcPixelThreshold = task.cellSize * task.cellSize ' eliminate singles...
        redM.Run(Not connect.dst2)

        dst1.SetTo(0)
        For Each md In redM.mdList
            dst1(md.rect).SetTo(md.index, md.mask)
        Next

        Dim minSize As Integer = src.Total / 25
        dst2.SetTo(0)
        mdLargest.Clear()
        For Each idd In task.iddList
            Dim index = dst1.Get(Of Byte)(idd.center.Y, idd.center.X)
            Dim md = redM.mdList(index)
            If index = 0 Then
                dst2(idd.cRect).SetTo(black)
            Else
                If md.pixels > minSize Then
                    dst2(idd.cRect).SetTo(task.scalarColors(index))
                    mdLargest.Add(md)
                End If
            End If
        Next

        dst3 = ShowAddweighted(src, dst2, labels(3))
        If task.heartBeat Then labels(2) = "There were " + CStr(redM.mdList.Count) + " connected contours found."
    End Sub
End Class





Public Class Connected_RegionsNew : Inherits TaskParent
    Dim connect As New Connected_Basics
    Dim redM As New RedMask_Basics
    Public Sub New()
        desc = "Modify each grid cell to indicate which group it is in.  Single cells are not in any group."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim gCells As New List(Of gridCellNew)
        For Each idd In task.iddList
            Dim gc As New gridCellNew
            gc.cRect = idd.cRect
            gc.lRect = idd.lRect
            gc.rRect = idd.rRect

            gc.depth = idd.depth
            gc.pixels = idd.pixels
            gc.mm = idd.mm
            gc.correlation = idd.correlation
            gc.index = gCells.Count
            gCells.Add(gc)
        Next

        connect.Run(src.Clone)
        task.rcPixelThreshold = task.cellSize * task.cellSize ' eliminate singles...
        redM.Run(Not connect.dst2)

        'Dim counts(gCells.Count - 1) As Integer
        'For Each gc In gCells
        '    Dim index = dst1.Get(Of Byte)(gc.center.Y, gc.center.X)
        '    Dim md = redM.mdList(index)
        '    If index = 0 Then
        '        dst2(idd.cRect).SetTo(black)
        '    Else
        '        If md.pixels > minSize Then
        '            dst2(idd.cRect).SetTo(task.scalarColors(index))
        '            mdLargest.Add(md)
        '        End If
        '    End If
        'Next

        'Dim sortedCounts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        'For i = 0 To newCount.Count - 1
        '    sortedCounts.Add(newCount(i), i)
        'Next

        'Dim indexList As New List(Of Integer)
        'For i = 0 To 10 - 1
        '    Dim index = sortedCounts.ElementAt(i).Value
        '    indexList.Add(index)
        'Next

        'dst3.SetTo(0)
        'For Each gc In gCells
        '    If indexList.Contains(gc.index) Then
        '        dst3(gc.cRect).SetTo(gc.index)
        '    End If
        'Next

        'dst1.SetTo(0)
        'For Each md In redM.mdList
        '    dst1(md.rect).SetTo(md.index, md.mask)
        'Next
    End Sub
End Class
