Imports cv = OpenCvSharp
Public Class Find_PolyLines : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Dim polyLine As New PolyLine_Basics
    Public Sub New()
        desc = "Find lines using the brick points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(src)
        labels(2) = ptBrick.labels(2)

        polyLine.Run(src)
        dst2 = polyLine.dst3
        labels(2) = polyLine.labels(3)

        For Each pt In ptBrick.bpList
            dst2.Circle(pt, task.DotSize, task.highlight, -1, task.lineType)
        Next
    End Sub
End Class



Public Class Find_EdgeLine : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Public classCount As Integer
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find lines using the brick points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        ptBrick.Run(src)
        labels(2) = ptBrick.labels(2)

        dst2 = task.edges.dst2
        dst3 = ShowPalette(task.edges.dst2)

        Dim segments(task.edges.classCount) As List(Of cv.Point2f)
        Dim brickCount As Integer, segmentCount
        For Each pt In ptBrick.bpList
            Dim val = task.edges.dst2.Get(Of Byte)(pt.Y, pt.X)
            If val > 0 And val < 255 Then
                If segments(val) Is Nothing Then
                    segments(val) = New List(Of cv.Point2f)
                    segmentCount += 1
                End If
                segments(val).Add(pt)
                brickCount += 1
            End If
        Next

        labels(3) = CStr(task.edges.classCount) + " segments were found and " + CStr(segmentCount) + " contained brick points"
        labels(3) += " " + CStr(brickCount) + " bricks were part of a segment"

        classCount = 0
        For Each segment In segments
            If segment Is Nothing Then Continue For
            classCount += 1
            Dim p1 = segment(0)
            For Each p2 In segment
                dst3.Circle(p2, task.DotSize, task.highlight, -1, task.lineType)
                'dst3.Line(p1, p2, task.highlight, task.lineWidth, task.lineType)
                p1 = p2
            Next
        Next

        If standaloneTest() Then EdgeLine_Basics.showSegment(dst1)
    End Sub
End Class






Public Class Find_Segment : Inherits TaskParent
    Public segments As New List(Of List(Of cv.Point))
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "Highlighting the individual segments one by one."
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Break up any edgeline segments that cross depth boundaries."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2

        segments.Clear()
        For Each seg In task.edges.sortedSegments.Values
            Dim nextSeg As New List(Of cv.Point)
            Dim lastDepth = -1
            For Each pt In seg
                Dim depth = task.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
                If lastDepth > 0 And Math.Abs(lastDepth - depth) > 1 Then
                    If nextSeg.Count > 0 Then
                        segments.Add(nextSeg)
                        nextSeg.Clear()
                    End If
                End If

                If depth > 0 Then nextSeg.Add(pt)
                lastDepth = depth
            Next
            If nextSeg.Count > 0 Then segments.Add(nextSeg)
        Next

        dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        If task.toggleOn Then
            dst3.SetTo(0, task.noDepthMask)
            SetTrueText("Pixels without depth are removed.", 3)
        End If

        dst1.SetTo(0)
        For Each seg In segments
            If seg.Count < 2 Then Continue For
            Dim p1 = seg(0)
            For Each p2 In seg
                dst1.Line(p1, p2, 255, task.lineWidth, task.lineType)
                p1 = p2
            Next
        Next
        labels(3) = "After using depth to isolate segments there are " + CStr(segments.Count) + " segments"
    End Sub
End Class






Public Class Find_BuildList : Inherits TaskParent
    Public nrclist As New List(Of nrcData)
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Create an entry for each segment"
    End Sub
    Public Shared Function buildRect(seg As List(Of cv.Point)) As cv.Rect
        Dim minX As Single = seg.Min(Function(p) p.X)
        Dim maxX As Single = seg.Max(Function(p) p.X)
        Dim minY As Single = seg.Min(Function(p) p.Y)
        Dim maxY As Single = seg.Max(Function(p) p.Y)
        Return TaskParent.ValidateRect(New cv.Rect(minX, minY, maxX - minX, maxY - minY))
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = task.edges.labels(2)

        dst1.SetTo(0)
        nrclist.Clear()
        Dim nrcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For Each seg In task.edges.sortedSegments.Values
            Dim nrc = New nrcData
            nrc.ID = task.grid.gridMap.Get(Of Single)(seg(0).Y, seg(0).X)
            Dim takenFlag = nrcMap.Get(Of Byte)(seg(0).Y, seg(0).X)
            If takenFlag <> 0 Then Continue For ' this id is already taken by a larger segment
            nrcMap(task.gridRects(nrc.ID)).SetTo(255)
            nrc.rect = buildRect(seg)
            nrc.mask = dst2(nrc.rect)
            nrc.pixels = seg.Count
            nrc.segment = seg
            dst1(nrc.rect).SetTo(nrc.ID Mod 255, nrc.mask)
            nrclist.Add(nrc)
            If nrclist.Count > task.gOptions.DebugSlider.Value Then Exit For
        Next

        dst3 = ShowPalette(dst1)
        dst0 = ShowPalette(dst1)

        For i = 0 To Math.Min(task.gOptions.DebugSlider.Value, nrclist.Count) - 1
            Dim nrc = nrclist(i)
            Dim seg = nrc.segment
            SetTrueText(CStr(i) + " " + CStr(seg.Count), seg(0), 3)
            dst3.Rectangle(nrc.rect, task.highlight, task.lineWidth)
            dst2(nrc.rect).SetTo(0, nrc.mask)
        Next

        labels(3) = CStr(nrclist.Count) + " segments are present.  " + CStr(task.edges.sortedSegments.Values.Count - nrclist.Count) +
                    " segments hit an already occupied grid cell.  Using " + CStr(dst1.CountNonZero) + " pixels."
    End Sub
End Class


