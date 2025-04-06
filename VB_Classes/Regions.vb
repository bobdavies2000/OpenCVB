Imports cv = OpenCvSharp
Public Class Regions_Basics : Inherits TaskParent
    Dim regions As New Regions_Core
    Dim hRects As New List(Of cv.Rect)
    Dim vRects As New List(Of cv.Rect)
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32S, 0)
        labels(2) = "Move mouse over a line to see the depth values.  Results will be in Labels(3)"
        desc = "Display grid cells that are connected by depth vertically and horizontally."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim tilesPerRow = task.grid.tilesPerRow
        Dim tilesPerCol = task.grid.tilesPerCol
        regions.Run(src)

        hRects.Clear()
        dst0.SetTo(0)
        dst2.SetTo(0)
        For Each tuple In regions.hTuples
            Dim gc1 = task.gcList(tuple.Item1)
            Dim gc2 = task.gcList(tuple.Item2)
            If gc1.depth = 0 Or gc2.depth = 0 Then Continue For
            If gc1.center.DistanceTo(gc2.center) > task.cellSize Then
                Dim r = gc1.rect
                For i = gc1.index + 1 To gc2.index - 1
                    r = r.Union(task.gcList(i).rect)
                Next
                hRects.Add(r)
                dst0(r).SetTo(hRects.Count)

                Dim color = task.scalarColors(CInt(tilesPerCol * r.Y / dst2.Height) Mod 255)
                dst2(r).SetTo(color)
            End If
        Next

        vRects.Clear()
        dst1.SetTo(0)
        dst3.SetTo(0)
        For Each tuple In regions.vTuples
            Dim gc1 = task.gcList(tuple.Item1)
            Dim gc2 = task.gcList(tuple.Item2)
            If gc1.depth = 0 Or gc2.depth = 0 Then Continue For
            If gc1.center.DistanceTo(gc2.center) > task.cellSize Then
                Dim r = gc1.rect
                For i = gc1.index + tilesPerRow To gc2.index - 1 Step tilesPerRow
                    r = r.Union(task.gcList(i).rect)
                Next
                vRects.Add(r)
                dst1(r).SetTo(vRects.Count)

                Dim color = task.scalarColors(CInt(tilesPerRow * r.X / dst2.Width) Mod 255)
                dst3(r).SetTo(color)
            End If
        Next

        Dim rect As cv.Rect
        If task.mousePicTag = 2 Then
            Dim index = dst0.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
            If index = 0 Then Exit Sub
            rect = hRects(index - 1)
        Else
            Dim index = dst1.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
            If index = 0 Then Exit Sub
            rect = vRects(index - 1)
        End If

        labels(3) = ""
    End Sub
End Class






Public Class Regions_Core : Inherits TaskParent
    Public hTuples As New List(Of Tuple(Of Integer, Integer))
    Public vTuples As New List(Of Tuple(Of Integer, Integer))
    Public width As Integer, height As Integer
    Dim colStart As Integer, colEnd As Integer, colorIndex As Integer
    Dim rowStart As Integer, bottomRight As cv.Point, topLeft As cv.Point
    Public Sub New()
        desc = "Connect cells that are close in depth"
    End Sub
    Private Sub hTestRect(idd1 As gcData, idd2 As gcData, nextStart As Integer)
        If Math.Abs(idd1.depth - idd2.depth) > task.depthDiffMeters Or nextStart = -1 Then
            Dim p1 = task.gcList(colStart).rect.TopLeft
            Dim p2 = task.gcList(colEnd).rect.BottomRight
            dst2.Rectangle(p1, p2, task.scalarColors(colorIndex Mod 256), -1)
            colorIndex += 1
            hTuples.Add(New Tuple(Of Integer, Integer)(colStart, colEnd))
            colStart = nextStart
            colEnd = colStart
        Else
            colEnd += 1
        End If
    End Sub
    Private Sub vTestRect(idd1 As gcData, idd2 As gcData, iddNext As Integer, nextStart As Integer)
        If Math.Abs(idd1.depth - idd2.depth) > task.depthDiffMeters Or nextStart = -1 Then
            bottomRight = task.gcList(iddNext).rect.BottomRight
            dst3.Rectangle(topLeft, bottomRight, task.scalarColors(colorIndex Mod 256), -1)
            colorIndex += 1
            vTuples.Add(New Tuple(Of Integer, Integer)(rowStart, iddNext))
            rowStart = nextStart
            If nextStart >= 0 Then topLeft = task.gcList(rowStart).rect.TopLeft
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
                hTestRect(task.gcList(i * width + j), task.gcList(i * width + j + 1), i * width + j + 1)
            Next
            hTestRect(task.gcList(i * width + height - 1), task.gcList(i * width + height - 1), -1)
        Next
        labels(2) = CStr(colorIndex) + " horizontal slices were connected because cell depth difference < " +
                    CStr(task.depthDiffMeters) + " meters"

        vTuples.Clear()
        Dim index As Integer
        colorIndex = 0
        For i = 0 To width - 1
            rowStart = i
            topLeft = task.gcList(i).rect.TopLeft
            bottomRight = task.gcList(i + width).rect.TopLeft
            For j = 0 To height - 2
                index = i + (j + 1) * width
                If index >= task.gcList.Count Then index = task.gcList.Count - 1
                vTestRect(task.gcList(i + j * width), task.gcList(index), i + j * width, index)
            Next
            Dim iddNext = i + (height - 1) * width
            If iddNext >= task.gcList.Count Then iddNext = task.gcList.Count - 1
            vTestRect(task.gcList(iddNext), task.gcList(index), iddNext, -1)
        Next

        labels(3) = CStr(colorIndex) + " vertical slices were connected because cell depth difference < " +
                    CStr(task.depthDiffMeters) + " meters"
    End Sub
End Class






Public Class Regions_Gaps : Inherits TaskParent
    Dim connect As New Regions_Core
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
                Dim idd = task.gcList(tup.Item1)
                dst2(idd.rect).SetTo(0)
            End If
        Next

        For Each tup In connect.vTuples
            Dim idd1 = task.gcList(tup.Item1)
            Dim idd2 = task.gcList(tup.Item2)
            If idd2.rect.TopLeft.Y - idd1.rect.TopLeft.Y = 0 Then
                dst2(idd1.rect).SetTo(0)
                dst3(idd1.rect).SetTo(0)
            End If
        Next
    End Sub
End Class








Public Class Regions_Contours : Inherits TaskParent
    Public redM As New RedMask_Basics
    Public connect As New Regions_Rects
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





Public Class Regions_RedColor : Inherits TaskParent
    Dim connect As New Regions_Contours
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







Public Class Regions_RectsH : Inherits TaskParent
    Public hRects As New List(Of cv.Rect)
    Dim connect As New Regions_Core
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
            Dim idd1 = task.gcList(tup.Item1)
            Dim idd2 = task.gcList(tup.Item2)

            Dim w = idd2.rect.BottomRight.X - idd1.rect.TopLeft.X
            Dim h = idd1.rect.Height

            Dim r = New cv.Rect(idd1.rect.TopLeft.X + 1, idd1.rect.TopLeft.Y, w - 1, h)

            hRects.Add(r)
            dst2(r).SetTo(255)

            index += 1
            dst3(r).SetTo(task.scalarColors(index Mod 256))
        Next
    End Sub
End Class






Public Class Regions_RectsV : Inherits TaskParent
    Public vRects As New List(Of cv.Rect)
    Dim connect As New Regions_Core
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
            Dim idd1 = task.gcList(tup.Item1)
            Dim idd2 = task.gcList(tup.Item2)

            Dim w = idd1.rect.Width
            Dim h = idd2.rect.BottomRight.Y - idd1.rect.TopLeft.Y

            Dim r = New cv.Rect(idd1.rect.TopLeft.X, idd1.rect.TopLeft.Y + 1, w, h - 1)
            vRects.Add(r)
            dst2(r).SetTo(255)

            index += 1
            dst3(r).SetTo(task.scalarColors(index Mod 256))
        Next
    End Sub
End Class






Public Class Regions_Rects : Inherits TaskParent
    Dim hConn As New Regions_RectsH
    Dim vConn As New Regions_RectsV
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






