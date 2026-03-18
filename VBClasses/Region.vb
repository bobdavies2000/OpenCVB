Imports System.Security.Cryptography
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Region_Basics : Inherits TaskParent
        Dim bricks As New Brick_Basics
        Dim regions As New Region_Core
        Public hRects As New List(Of cv.Rect)
        Public vRects As New List(Of cv.Rect)
        Public Sub New()
            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32S, 0)
            labels(2) = "Move mouse over a line to see the depth values.  Results will be in Labels(3)"
            desc = "Display bricks that are connected by depth vertically and horizontally."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)
            regions.Run(src)

            hRects.Clear()
            dst0.SetTo(0)
            dst2.SetTo(0)
            For Each tuple In regions.hTuples
                Dim brick1 = bricks.brickList(tuple.Item1)
                Dim brick2 = bricks.brickList(tuple.Item2)
                If brick1.depth = 0 Or brick2.depth = 0 Then Continue For
                If brick1.center.DistanceTo(brick2.center) > task.squareSize Then
                    Dim r = brick1.rect
                    For i = brick1.index + 1 To brick2.index - 1
                        r = r.Union(bricks.brickList(i).rect)
                    Next
                    hRects.Add(r)
                    dst0(r).SetTo(hRects.Count)

                    Dim color = task.scalarColors(task.bricksPerCol * r.Y \ dst2.Height Mod 255)
                    dst2(r).SetTo(color)
                End If
            Next

            vRects.Clear()
            dst1.SetTo(0)
            dst3.SetTo(0)
            For Each tuple In regions.vTuples
                Dim brick1 = bricks.brickList(tuple.Item1)
                Dim brick2 = bricks.brickList(tuple.Item2)
                If brick1.depth = 0 Or brick2.depth = 0 Then Continue For
                If brick1.center.DistanceTo(brick2.center) > task.squareSize Then
                    Dim r = brick1.rect
                    For i = brick1.index + task.bricksPerRow To brick2.index - 1 Step task.bricksPerRow
                        r = r.Union(bricks.brickList(i).rect)
                    Next
                    vRects.Add(r)
                    dst1(r).SetTo(vRects.Count)

                    Dim color = task.scalarColors(task.bricksPerRow * r.X \ dst2.Width Mod 255)
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

            Dim brickIndex = task.gridMap.Get(Of Integer)(rect.Y, rect.X)
            If brickIndex > 0 Then
                labels(3) = "Depth = " + Format(bricks.brickList(brickIndex).depth, fmt1) + "m"
                brickIndex = task.gridMap.Get(Of Integer)(rect.BottomRight.Y, rect.BottomRight.X)
                labels(3) += " to " + Format(bricks.brickList(brickIndex).depth, fmt1) + "m"
            Else
                labels(3) = "No depth region present..."
            End If
        End Sub
    End Class






    Public Class NR_Region_Quads : Inherits TaskParent
        Public quadMat As New cv.Mat
        Public inputRects As New List(Of cv.Rect)
        Dim bricks As New Brick_Basics
        Public Sub New()
            desc = "Build Quads for each rectangle in the list horizontal rectangles."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)

            If standalone Then
                Static regions As New Region_Basics
                regions.Run(src)
                dst2 = regions.dst2
                dst3 = regions.dst3
                inputRects = regions.hRects
            End If

            Dim quadData As New List(Of cv.Point3f)
            For Each rect In inputRects
                Dim index1 = task.gridMap.Get(Of Integer)(rect.Y, rect.X)
                Dim index2 = task.gridMap.Get(Of Integer)(rect.BottomRight.Y - 1, rect.BottomRight.X - 1)
                If index1 = 0 Or index2 = 0 Then Continue For

                Dim brick1 = bricks.brickList(index1)
                Dim brick2 = bricks.brickList(index2)

                quadData.Add(New cv.Point3f(brick1.color(0), brick1.color(1), brick1.color(2)))

                Dim p0 = Cloud_Basics.worldCoordinates(rect.TopLeft, brick1.depth)
                Dim p1 = Cloud_Basics.worldCoordinates(rect.BottomRight, brick2.depth)

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
        Dim colStart As Integer, colEnd As Integer, colorIndex As Integer
        Dim rowStart As Integer, bottomRight As cv.Point, topLeft As cv.Point
        Dim options As New Options_Features
        Dim bricks As New Brick_Basics
        Public Sub New()
            desc = "Connect cells that are close in depth"
        End Sub
        Private Sub hTestRect(brick1 As brickData, brick2 As brickData, nextStart As Integer)
            If Math.Abs(brick1.depth - brick2.depth) > task.depthDiffMeters Or nextStart = -1 Then
                Dim p1 = bricks.brickList(colStart).rect.TopLeft
                Dim p2 = bricks.brickList(colEnd).rect.BottomRight
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
                bottomRight = bricks.brickList(brickNext).rect.BottomRight
                dst3.Rectangle(topLeft, bottomRight, task.scalarColors(colorIndex Mod 256), -1)
                colorIndex += 1
                vTuples.Add(New Tuple(Of Integer, Integer)(rowStart, brickNext))
                rowStart = nextStart
                If nextStart >= 0 Then topLeft = bricks.brickList(rowStart).rect.TopLeft
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            bricks.Run(src)

            dst2.SetTo(0)
            dst3.SetTo(0)

            hTuples.Clear()
            colorIndex = 0
            For i = 0 To task.bricksPerCol - 1
                colStart = i * task.bricksPerRow
                colEnd = colStart
                For j = 0 To task.bricksPerRow - 2
                    hTestRect(bricks.brickList(i * task.bricksPerRow + j),
                              bricks.brickList(i * task.bricksPerRow + j + 1), i * task.bricksPerRow + j + 1)
                Next
                hTestRect(bricks.brickList(i * task.bricksPerRow + task.bricksPerCol - 1),
                          bricks.brickList(i * task.bricksPerRow + task.bricksPerCol - 1), -1)
            Next
            labels(2) = CStr(colorIndex) + " horizontal slices were connected because cell depth difference < " +
                    CStr(task.depthDiffMeters) + " meters"

            vTuples.Clear()
            Dim index As Integer
            colorIndex = 0
            For i = 0 To task.bricksPerRow - 1
                rowStart = i
                topLeft = bricks.brickList(i).rect.TopLeft
                bottomRight = bricks.brickList(i + task.bricksPerRow).rect.TopLeft
                For j = 0 To task.bricksPerCol - 2
                    index = i + (j + 1) * task.bricksPerRow
                    If index >= bricks.brickList.Count Then index = bricks.brickList.Count - 1
                    vTestRect(bricks.brickList(i + j * task.bricksPerRow),
                              bricks.brickList(index), i + j * task.bricksPerRow, index)
                Next
                Dim brickNext = i + (task.bricksPerCol - 1) * task.bricksPerRow
                If brickNext >= bricks.brickList.Count Then brickNext = bricks.brickList.Count - 1
                vTestRect(bricks.brickList(brickNext), bricks.brickList(index), brickNext, -1)
            Next

            labels(3) = CStr(colorIndex) + " vertical slices were connected because cell depth difference < " +
                    CStr(task.depthDiffMeters) + " meters"
        End Sub
    End Class





    Public Class Region_Depth : Inherits TaskParent
        Public redM As New RedMask_Basics
        Public connect As New Region_Rects
        Public mdLargest As New List(Of maskData)
        Dim bricks As New Brick_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            task.gOptions.TruncateDepth.Checked = True
            desc = "Find the main regions connected in depth and build a contour for each."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)

            connect.Run(src.Clone)
            redM.Run(Not connect.dst2)
            If redM.mdList.Count = 0 Then Exit Sub

            dst1.SetTo(0)
            For Each md In redM.mdList
                dst1(md.rect).SetTo(md.index, md.mask)
            Next

            Dim minSize As Integer = src.Total / 25
            dst2.SetTo(0)
            mdLargest.Clear()
            For Each gSq In bricks.brickList
                Dim index = dst1.Get(Of Byte)(gSq.center.Y, gSq.center.X)
                Dim md = redM.mdList(index)
                If index = 0 Then
                    dst2(gSq.rect).SetTo(black)
                Else
                    If md.pixels > minSize Then
                        dst2(gSq.rect).SetTo(task.scalarColors(index))
                        mdLargest.Add(md)
                    End If
                End If
            Next

            dst3 = ShowAddweighted(src, dst2, labels(3))
            labels(2) = "There were " + CStr(redM.mdList.Count) + " connected contours found."
        End Sub
    End Class







    Public Class Region_RectsH : Inherits TaskParent
        Dim bricks As New Brick_Basics
        Public hRects As New List(Of cv.Rect)
        Dim connect As New Region_Core
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Connect bricks with similar depth - horizontally scanning."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)
            connect.Run(src)

            dst2.SetTo(0)
            dst3.SetTo(0)
            hRects.Clear()
            Dim index As Integer
            For Each tup In connect.hTuples
                If tup.Item1 = tup.Item2 Then Continue For
                Dim brick1 = bricks.brickList(tup.Item1)
                Dim brick2 = bricks.brickList(tup.Item2)

                Dim w = brick2.rect.BottomRight.X - brick1.rect.X
                Dim h = brick1.rect.Height

                Dim r = New cv.Rect(brick1.rect.X + 1, brick1.rect.Y, w - 1, h)

                hRects.Add(r)
                dst2(r).SetTo(255)

                index += 1
                dst3(r).SetTo(task.scalarColors(index Mod 256))
            Next
        End Sub
    End Class






    Public Class Region_RectsV : Inherits TaskParent
        Dim bricks As New Brick_Basics
        Public vRects As New List(Of cv.Rect)
        Dim connect As New Region_Core
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            desc = "Connect bricks with similar depth - vertically scanning."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)

            connect.Run(src)

            dst2.SetTo(0)
            dst3.SetTo(0)
            vRects.Clear()
            Dim index As Integer
            For Each tup In connect.vTuples
                If tup.Item1 = tup.Item2 Then Continue For
                Dim brick1 = bricks.brickList(tup.Item1)
                Dim brick2 = bricks.brickList(tup.Item2)

                Dim w = brick1.rect.Width
                Dim h = brick2.rect.BottomRight.Y - brick1.rect.Y

                Dim r = New cv.Rect(brick1.rect.X, brick1.rect.Y + 1, w, h - 1)
                vRects.Add(r)
                dst2(r).SetTo(255)

                index += 1
                dst3(r).SetTo(task.scalarColors(index Mod 256))
            Next
        End Sub
    End Class






    Public Class Region_Rects : Inherits TaskParent
        Dim bricks As New Brick_Basics
        Dim hConn As New Region_RectsH
        Dim vConn As New Region_RectsV
        Public Sub New()
            desc = "Isolate the connected depth bricks both vertically and horizontally."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)
            hConn.Run(src)
            vConn.Run(src)

            dst2 = (Not vConn.dst2).ToMat Or (Not hConn.dst2).ToMat

            dst3 = src
            dst3.SetTo(0, dst2)
        End Sub
    End Class




    Public Class Region_DepthCorrelation : Inherits TaskParent
        Dim bricks As New Brick_Basics
        Public Sub New()
            dst0 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The matching bricks in the right view that were used in the correlation computation"
            desc = "Create depth region markers using a correlation threshold"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)

            dst0.SetTo(0)
            dst1.SetTo(0)
            Dim count As Integer
            For Each gSq In bricks.brickList
                If gSq.correlation > task.fCorrThreshold Then
                    dst0.Rectangle(gSq.rRect, 255, -1)
                    dst1.Rectangle(gSq.rect, 255, -1)
                    count += 1
                End If
            Next

            dst2.SetTo(0)
            src.CopyTo(dst2, dst1)

            dst3.SetTo(0)
            task.rightView.CopyTo(dst3, dst0)

            labels(2) = Format(count / bricks.brickList.Count, "0%") + " of bricks had color correlation of " +
                    Format(task.fCorrThreshold, "0.0%") + " or better"
        End Sub
    End Class
End Namespace