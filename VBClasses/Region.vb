Imports System.Security.Cryptography
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Region_Basics : Inherits TaskParent
        Dim regions As New Region_Core
        Public hRects As New List(Of cv.Rect)
        Public vRects As New List(Of cv.Rect)
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
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
                Dim brick1 = atask.bricks.brickList(tuple.Item1)
                Dim brick2 = atask.bricks.brickList(tuple.Item2)
                If brick1.depth = 0 Or brick2.depth = 0 Then Continue For
                If brick1.center.DistanceTo(brick2.center) > atask.brickSize Then
                    Dim r = brick1.rect
                    For i = brick1.index + 1 To brick2.index - 1
                        r = r.Union(atask.bricks.brickList(i).rect)
                    Next
                    hRects.Add(r)
                    dst0(r).SetTo(hRects.Count)

                    Dim color = atask.scalarColors(atask.bricksPerCol * r.Y \ dst2.Height Mod 255)
                    dst2(r).SetTo(color)
                End If
            Next

            vRects.Clear()
            dst1.SetTo(0)
            dst3.SetTo(0)
            For Each tuple In regions.vTuples
                Dim brick1 = atask.bricks.brickList(tuple.Item1)
                Dim brick2 = atask.bricks.brickList(tuple.Item2)
                If brick1.depth = 0 Or brick2.depth = 0 Then Continue For
                If brick1.center.DistanceTo(brick2.center) > atask.brickSize Then
                    Dim r = brick1.rect
                    For i = brick1.index + atask.bricksPerRow To brick2.index - 1 Step atask.bricksPerRow
                        r = r.Union(atask.bricks.brickList(i).rect)
                    Next
                    vRects.Add(r)
                    dst1(r).SetTo(vRects.Count)

                    Dim color = atask.scalarColors(atask.bricksPerRow * r.X \ dst2.Width Mod 255)
                    dst3(r).SetTo(color)
                End If
            Next

            Dim rect As cv.Rect
            If atask.mousePicTag = 2 Then
                Dim index = dst0.Get(Of Integer)(atask.mouseMovePoint.Y, atask.mouseMovePoint.X)
                If index = 0 Then Exit Sub
                rect = hRects(index - 1)
            Else
                Dim index = dst1.Get(Of Integer)(atask.mouseMovePoint.Y, atask.mouseMovePoint.X)
                If index = 0 Then Exit Sub
                rect = vRects(index - 1)
            End If

            Dim brickIndex = atask.gridMap.Get(Of Integer)(rect.Y, rect.X)
            If brickIndex > 0 Then
                labels(3) = "Depth = " + Format(atask.bricks.brickList(brickIndex).depth, fmt1) + "m"
                brickIndex = atask.gridMap.Get(Of Integer)(rect.BottomRight.Y, rect.BottomRight.X)
                labels(3) += " to " + Format(atask.bricks.brickList(brickIndex).depth, fmt1) + "m"
            Else
                labels(3) = "No depth region present..."
            End If
        End Sub
    End Class






    Public Class NR_Region_Quads : Inherits TaskParent
        Public quadMat As New cv.Mat
        Public inputRects As New List(Of cv.Rect)
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
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
                Dim index1 = atask.gridMap.Get(Of Integer)(rect.Y, rect.X)
                Dim index2 = atask.gridMap.Get(Of Integer)(rect.BottomRight.Y - 1, rect.BottomRight.X - 1)
                If index1 = 0 Or index2 = 0 Then Continue For

                Dim brick1 = atask.bricks.brickList(index1)
                Dim brick2 = atask.bricks.brickList(index2)

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
        Public width As Integer, height As Integer
        Dim colStart As Integer, colEnd As Integer, colorIndex As Integer
        Dim rowStart As Integer, bottomRight As cv.Point, topLeft As cv.Point
        Dim options As New Options_Features
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            desc = "Connect cells that are close in depth"
        End Sub
        Private Sub hTestRect(brick1 As brickData, brick2 As brickData, nextStart As Integer)
            If Math.Abs(brick1.depth - brick2.depth) > atask.depthDiffMeters Or nextStart = -1 Then
                Dim p1 = atask.bricks.brickList(colStart).rect.TopLeft
                Dim p2 = atask.bricks.brickList(colEnd).rect.BottomRight
                dst2.Rectangle(p1, p2, atask.scalarColors(colorIndex Mod 256), -1)
                colorIndex += 1
                hTuples.Add(New Tuple(Of Integer, Integer)(colStart, colEnd))
                colStart = nextStart
                colEnd = colStart
            Else
                colEnd += 1
            End If
        End Sub
        Private Sub vTestRect(brick1 As brickData, brick2 As brickData, brickNext As Integer, nextStart As Integer)
            If Math.Abs(brick1.depth - brick2.depth) > atask.depthDiffMeters Or nextStart = -1 Then
                bottomRight = atask.bricks.brickList(brickNext).rect.BottomRight
                dst3.Rectangle(topLeft, bottomRight, atask.scalarColors(colorIndex Mod 256), -1)
                colorIndex += 1
                vTuples.Add(New Tuple(Of Integer, Integer)(rowStart, brickNext))
                rowStart = nextStart
                If nextStart >= 0 Then topLeft = atask.bricks.brickList(rowStart).rect.TopLeft
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2.SetTo(0)
            dst3.SetTo(0)

            width = dst2.Width / atask.brickSize
            If width * atask.brickSize <> dst2.Width Then width += 1
            height = Math.Floor(dst2.Height / atask.brickSize)
            If height * atask.brickSize <> dst2.Height Then height += 1
            hTuples.Clear()
            colorIndex = 0
            For i = 0 To height - 1
                colStart = i * width
                colEnd = colStart
                For j = 0 To width - 2
                    hTestRect(atask.bricks.brickList(i * width + j), atask.bricks.brickList(i * width + j + 1), i * width + j + 1)
                Next
                hTestRect(atask.bricks.brickList(i * width + height - 1), atask.bricks.brickList(i * width + height - 1), -1)
            Next
            labels(2) = CStr(colorIndex) + " horizontal slices were connected because cell depth difference < " +
                    CStr(atask.depthDiffMeters) + " meters"

            vTuples.Clear()
            Dim index As Integer
            colorIndex = 0
            For i = 0 To width - 1
                rowStart = i
                topLeft = atask.bricks.brickList(i).rect.TopLeft
                bottomRight = atask.bricks.brickList(i + width).rect.TopLeft
                For j = 0 To height - 2
                    index = i + (j + 1) * width
                    If index >= atask.bricks.brickList.Count Then index = atask.bricks.brickList.Count - 1
                    vTestRect(atask.bricks.brickList(i + j * width), atask.bricks.brickList(index), i + j * width, index)
                Next
                Dim brickNext = i + (height - 1) * width
                If brickNext >= atask.bricks.brickList.Count Then brickNext = atask.bricks.brickList.Count - 1
                vTestRect(atask.bricks.brickList(brickNext), atask.bricks.brickList(index), brickNext, -1)
            Next

            labels(3) = CStr(colorIndex) + " vertical slices were connected because cell depth difference < " +
                    CStr(atask.depthDiffMeters) + " meters"
        End Sub
    End Class









    Public Class Region_Contours : Inherits TaskParent
        Public redM As New RedMask_Basics
        Public connect As New XO_Region_Rects
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            atask.gOptions.TruncateDepth.Checked = True
            desc = "Find the main regions connected in depth and build a contour for each."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            connect.Run(src.Clone)
            redM.Run(Not connect.dst2)

            dst1.SetTo(0)
            For Each md In redM.mdList
                md.contour = ContourBuild(md.mask)
                dst1(md.rect).SetTo(md.index, md.mask)
            Next

            dst2 = PaletteFull(dst1)
            dst2.SetTo(0, connect.dst2)
            dst3 = ShowAddweighted(src, dst2, labels(3))
            If atask.heartBeat Then labels(2) = "There were " + CStr(redM.mdList.Count) + " connected contours found."
        End Sub
    End Class






    Public Class Region_Depth : Inherits TaskParent
        Public redM As New RedMask_Basics
        Public connect As New XO_Region_Rects
        Public mdLargest As New List(Of maskData)
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            atask.gOptions.TruncateDepth.Checked = True
            desc = "Find the main regions connected in depth and build a contour for each."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
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
            For Each gr In atask.bricks.brickList
                Dim index = dst1.Get(Of Byte)(gr.center.Y, gr.center.X)
                Dim md = redM.mdList(index)
                If index = 0 Then
                    dst2(gr.rect).SetTo(black)
                Else
                    If md.pixels > minSize Then
                        dst2(gr.rect).SetTo(atask.scalarColors(index))
                        mdLargest.Add(md)
                    End If
                End If
            Next

            dst3 = ShowAddweighted(src, dst2, labels(3))
            If atask.heartBeat Then labels(2) = "There were " + CStr(redM.mdList.Count) + " connected contours found."
        End Sub
    End Class





    Public Class Region_DepthCorrelation : Inherits TaskParent
        Public Sub New()
            If atask.bricks Is Nothing Then atask.bricks = New Brick_Basics
            dst0 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            labels(3) = "The matching bricks in the right view that were used in the correlation computation"
            desc = "Create depth region markers using a correlation threshold"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst0.SetTo(0)
            dst1.SetTo(0)
            Dim count As Integer
            For Each gr In atask.bricks.brickList
                If gr.correlation > atask.fCorrThreshold Then
                    dst0.Rectangle(gr.rRect, 255, -1)
                    dst1.Rectangle(gr.rect, 255, -1)
                    count += 1
                End If
            Next

            dst2.SetTo(0)
            src.CopyTo(dst2, dst1)

            dst3.SetTo(0)
            atask.rightView.CopyTo(dst3, dst0)

            labels(2) = Format(count / atask.bricks.brickList.Count, "0%") + " of bricks had color correlation of " +
                    Format(atask.fCorrThreshold, "0.0%") + " or better"
        End Sub
    End Class
End Namespace