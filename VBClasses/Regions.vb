Imports System.Security.Cryptography
Imports cv = OpenCvSharp
Public Class Region_Basics : Inherits TaskParent
    Dim regions As New Region_Core
    Public hRects As New List(Of cv.Rect)
    Public vRects As New List(Of cv.Rect)
    Public Sub New()
        task.brickRunFlag = True
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32S, 0)
        labels(2) = "Move mouse over a line to see the depth values.  Results will be in Labels(3)"
        desc = "Display bricks that are connected by depth vertically and horizontally."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        regions.Run(src)

        hRects.Clear()
        dst0.SetTo(0)
        dst2.SetTo(0)
        For Each tuple In regions.hTuples
            Dim brick1 = task.bricks.brickList(tuple.Item1)
            Dim brick2 = task.bricks.brickList(tuple.Item2)
            If brick1.depth = 0 Or brick2.depth = 0 Then Continue For
            If brick1.center.DistanceTo(brick2.center) > task.cellSize Then
                Dim r = brick1.rect
                For i = brick1.index + 1 To brick2.index - 1
                    r = r.Union(task.bricks.brickList(i).rect)
                Next
                hRects.Add(r)
                dst0(r).SetTo(hRects.Count)

                Dim color = task.scalarColors(CInt(task.cellsPerCol * r.Y / dst2.Height) Mod 255)
                dst2(r).SetTo(color)
            End If
        Next

        vRects.Clear()
        dst1.SetTo(0)
        dst3.SetTo(0)
        For Each tuple In regions.vTuples
            Dim brick1 = task.bricks.brickList(tuple.Item1)
            Dim brick2 = task.bricks.brickList(tuple.Item2)
            If brick1.depth = 0 Or brick2.depth = 0 Then Continue For
            If brick1.center.DistanceTo(brick2.center) > task.cellSize Then
                Dim r = brick1.rect
                For i = brick1.index + task.cellsPerRow To brick2.index - 1 Step task.cellsPerRow
                    r = r.Union(task.bricks.brickList(i).rect)
                Next
                vRects.Add(r)
                dst1(r).SetTo(vRects.Count)

                Dim color = task.scalarColors(CInt(task.cellsPerRow * r.X / dst2.Width) Mod 255)
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

        Dim brickIndex = task.grid.gridMap.Get(Of Single)(rect.Y, rect.X)
        If brickIndex > 0 Then
            labels(3) = "Depth = " + Format(task.bricks.brickList(brickIndex).depth, fmt1) + "m"
            brickIndex = task.grid.gridMap.Get(Of Single)(rect.BottomRight.Y, rect.BottomRight.X)
            labels(3) += " to " + Format(task.bricks.brickList(brickIndex).depth, fmt1) + "m"
        Else
            labels(3) = "No depth region present..."
        End If
    End Sub
End Class






Public Class Region_Quads : Inherits TaskParent
    Public quadMat As New cv.Mat
    Public inputRects As New List(Of cv.Rect)
    Public Sub New()
        task.brickRunFlag = True
        desc = "Build Quads for each rectangle in the list horizontal rectangles."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            Static regions As New Region_Basics
            regions.Run(src)
            dst2 = regions.dst2
            dst3 = regions.dst3
            inputRects = regions.hRects
        End If

        Dim quadData As New List(Of cv.Point3f)
        For Each rect In inputRects
            Dim index1 = task.grid.gridMap.Get(Of Single)(rect.Y, rect.X)
            Dim index2 = task.grid.gridMap.Get(Of Single)(rect.BottomRight.Y - 1, rect.BottomRight.X - 1)
            If index1 = 0 Or index2 = 0 Then Continue For

            Dim brick1 = task.bricks.brickList(index1)
            Dim brick2 = task.bricks.brickList(index2)

            quadData.Add(brick1.color)

            Dim p0 = getWorldCoordinates(rect.TopLeft, brick1.depth)
            Dim p1 = getWorldCoordinates(rect.BottomRight, brick2.depth)

            quadData.Add(New cv.Point3f(p0.X, p0.Y, brick1.depth))
            quadData.Add(New cv.Point3f(p1.X, p0.Y, brick2.depth))
            quadData.Add(New cv.Point3f(p1.X, p1.Y, brick2.depth))
            quadData.Add(New cv.Point3f(p0.X, p1.Y, brick1.depth))
        Next

        quadMat = cv.Mat.FromPixelData(quadData.Count, 1, cv.MatType.CV_32FC3, quadData.ToArray)
    End Sub
End Class







Public Class Region_Core : Inherits TaskParent
    Public hTuples As New List(Of Tuple(Of Integer, Integer))
    Public vTuples As New List(Of Tuple(Of Integer, Integer))
    Public width As Integer, height As Integer
    Dim colStart As Integer, colEnd As Integer, colorIndex As Integer
    Dim rowStart As Integer, bottomRight As cv.Point, topLeft As cv.Point
    Public Sub New()
        task.brickRunFlag = True
        desc = "Connect cells that are close in depth"
    End Sub
    Private Sub hTestRect(brick1 As brickData, brick2 As brickData, nextStart As Integer)
        If Math.Abs(brick1.depth - brick2.depth) > task.depthDiffMeters Or nextStart = -1 Then
            Dim p1 = task.bricks.brickList(colStart).rect.TopLeft
            Dim p2 = task.bricks.brickList(colEnd).rect.BottomRight
            dst2.Rectangle(p1, p2, task.scalarColors(colorIndex Mod 256), -1)
            colorIndex += 1
            hTuples.Add(New Tuple(Of Integer, Integer)(colStart, colEnd))
            colStart = nextStart
            colEnd = colStart
        Else
            colEnd += 1
        End If
    End Sub
    Private Sub vTestRect(brick1 As brickData, brick2 As brickData, brickNext As Integer, nextStart As Integer)
        If Math.Abs(brick1.depth - brick2.depth) > task.depthDiffMeters Or nextStart = -1 Then
            bottomRight = task.bricks.brickList(brickNext).rect.BottomRight
            dst3.Rectangle(topLeft, bottomRight, task.scalarColors(colorIndex Mod 256), -1)
            colorIndex += 1
            vTuples.Add(New Tuple(Of Integer, Integer)(rowStart, brickNext))
            rowStart = nextStart
            If nextStart >= 0 Then topLeft = task.bricks.brickList(rowStart).rect.TopLeft
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
                hTestRect(task.bricks.brickList(i * width + j), task.bricks.brickList(i * width + j + 1), i * width + j + 1)
            Next
            hTestRect(task.bricks.brickList(i * width + height - 1), task.bricks.brickList(i * width + height - 1), -1)
        Next
        labels(2) = CStr(colorIndex) + " horizontal slices were connected because cell depth difference < " +
                    CStr(task.depthDiffMeters) + " meters"

        vTuples.Clear()
        Dim index As Integer
        colorIndex = 0
        For i = 0 To width - 1
            rowStart = i
            topLeft = task.bricks.brickList(i).rect.TopLeft
            bottomRight = task.bricks.brickList(i + width).rect.TopLeft
            For j = 0 To height - 2
                index = i + (j + 1) * width
                If index >= task.bricks.brickList.Count Then index = task.bricks.brickList.Count - 1
                vTestRect(task.bricks.brickList(i + j * width), task.bricks.brickList(index), i + j * width, index)
            Next
            Dim brickNext = i + (height - 1) * width
            If brickNext >= task.bricks.brickList.Count Then brickNext = task.bricks.brickList.Count - 1
            vTestRect(task.bricks.brickList(brickNext), task.bricks.brickList(index), brickNext, -1)
        Next

        labels(3) = CStr(colorIndex) + " vertical slices were connected because cell depth difference < " +
                    CStr(task.depthDiffMeters) + " meters"
    End Sub
End Class









Public Class Region_Contours : Inherits TaskParent
    Public redM As New RedMask_Basics
    Public connect As New XO_Region_Rects
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






Public Class Region_Depth : Inherits TaskParent
    Public redM As New RedMask_Basics
    Public connect As New XO_Region_Rects
    Public mdLargest As New List(Of maskData)
    Public Sub New()
        task.brickRunFlag = True
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
        For Each brick In task.bricks.brickList
            Dim index = dst1.Get(Of Byte)(brick.center.Y, brick.center.X)
            Dim md = redM.mdList(index)
            If index = 0 Then
                dst2(brick.rect).SetTo(black)
            Else
                If md.pixels > minSize Then
                    dst2(brick.rect).SetTo(task.scalarColors(index))
                    mdLargest.Add(md)
                End If
            End If
        Next

        dst3 = ShowAddweighted(src, dst2, labels(3))
        If task.heartBeat Then labels(2) = "There were " + CStr(redM.mdList.Count) + " connected contours found."
    End Sub
End Class





Public Class Region_DepthCorrelation : Inherits TaskParent
    Public Sub New()
        task.brickRunFlag = True
        dst0 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "The matching bricks in the right view that were used in the correlation computation"
        desc = "Create depth region markers using a correlation threshold"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst0.SetTo(0)
        dst1.SetTo(0)
        Dim count As Integer
        For Each brick In task.bricks.brickList
            If brick.correlation > task.bricks.options.correlationThreshold Then
                dst0.Rectangle(brick.rRect, 255, -1)
                dst1.Rectangle(brick.rect, 255, -1)
                count += 1
            End If
        Next

        dst2.SetTo(0)
        src.CopyTo(dst2, dst1)

        dst3.SetTo(0)
        task.rightView.CopyTo(dst3, dst0)

        labels(2) = Format(count / task.bricks.brickList.Count, "0%") + " of bricks had color correlation of " +
                    Format(task.bricks.options.correlationThreshold, "0.0%") + " or better"
    End Sub
End Class