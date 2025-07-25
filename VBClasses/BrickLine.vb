Imports cv = OpenCvSharp
Public Class BrickLine_Basics : Inherits TaskParent
    Dim hist As New Hist_GridCell
    Public edgeRequest As Boolean
    Public options As New Options_Features
    Public Sub New()
        task.brickRunFlag = True
        labels(2) = "Use 'Selected Feature' in 'Options_Features' to highlight different edges."
        desc = "Given lines or edges, build a grid of cells that cover them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standalone Or edgeRequest Then
            Static contour As New Contour_RotateRect
            contour.Run(task.grayStable)
            src = contour.dst1.Clone
        End If

        dst2 = ShowPaletteNoZero(src)
        dst3 = dst2.Clone

        Dim mm = GetMinMax(src)
        Dim featList As New List(Of List(Of Integer))
        For i = 0 To mm.maxVal ' the 0th entry is a placeholder for the background and will have 0 entries.
            featList.Add(New List(Of Integer))
        Next

        For Each brick In task.bricks.brickList
            hist.Run(src(brick.rect))
            For i = 1 To hist.histarray.Count - 1
                If hist.histarray(i) > 0 Then
                    featList(i).Add(brick.index)
                End If
            Next
        Next

        Dim edgeSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To mm.maxVal
            edgeSorted.Add(featList(i).Count, i)
        Next

        task.featList.Clear()
        For Each index In edgeSorted.Values
            If featList(index).Count > 0 Then task.featList.Add(featList(index))
        Next

        Dim edgeIndex = Math.Abs(task.gOptions.DebugSlider.Value)
        If edgeIndex <> 0 And edgeIndex < task.featList.Count Then
            For Each index In task.featList(edgeIndex)
                Dim brick = task.bricks.brickList(index)
                dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
            Next
        End If

        For i = 0 To task.featList.Count - 1
            If i <> Math.Abs(task.gOptions.DebugSlider.Value) Then Continue For
            Dim depthSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            For Each index In task.featList(i)
                Dim brick = task.bricks.brickList(index)
                depthSorted.Add(brick.depth, index)
            Next

            Dim lastDepth = depthSorted.ElementAt(0).Key
            For Each ele In depthSorted
                If Math.Abs(ele.Key - lastDepth) > task.depthDiffMeters Then
                    Dim brick = task.bricks.brickList(ele.Value)
                    dst2.Rectangle(brick.rect, red, task.lineWidth + 1)
                End If
                lastDepth = ele.Key
            Next
        Next
        labels(3) = CStr(task.featList.Count) + " features are present in the input lines or edges"
    End Sub
End Class







Public Class BrickLine_Edges : Inherits TaskParent
    Dim findCells As New BrickLine_Basics
    Public Sub New()
        findCells.edgeRequest = True
        labels(3) = "Use the 'Feature' option 'Selected Feature' to highlight different edges."
        desc = "Find the cells containing edges."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        findCells.Run(task.grayStable)
        dst2 = findCells.dst2
        dst3 = findCells.dst3
    End Sub
End Class








Public Class BrickLine_DepthGap : Inherits TaskParent
    Dim findCells As New BrickLine_Basics
    Public Sub New()
        labels(2) = "Cells highlighted below have a significant gap in depth from their neighbors."
        desc = "Find cells mapping the edges/lines which are not near any other cell - they are neighboring edges/lines but not in depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            findCells.edgeRequest = True
            findCells.Run(task.grayStable)
        Else
            findCells.Run(src)
        End If

        Dim gapCells As New List(Of Integer)
        For i = 0 To task.featList.Count - 1
            If task.featList(i).Count = 0 Then Exit For
            Dim depthSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            For Each index In task.featList(i)
                Dim brick = task.bricks.brickList(index)
                depthSorted.Add(brick.depth, index)
            Next

            Dim lastDepth = depthSorted.ElementAt(0).Key
            For Each ele In depthSorted
                If Math.Abs(ele.Key - lastDepth) > task.depthDiffMeters Then gapCells.Add(task.bricks.brickList(ele.Value).index)
                lastDepth = ele.Key
            Next
        Next

        If task.heartBeat Then
            dst2 = findCells.dst2.Clone
            Dim debugMode = task.gOptions.DebugSlider.Value <> 0
            For i = 0 To gapCells.Count - 1
                If debugMode Then If i <> Math.Abs(task.gOptions.DebugSlider.Value) Then Continue For
                Dim brick = task.bricks.brickList(gapCells(i))
                dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
                If i = Math.Abs(task.gOptions.DebugSlider.Value) Then
                    SetTrueText(Format(brick.depth, fmt1), brick.rect.BottomRight)
                End If
            Next
        End If
        labels(3) = CStr(task.featList.Count) + " features are present in the input lines or edges"
    End Sub
End Class






Public Class BrickLine_DepthGaps : Inherits TaskParent
    Dim findCells As New BrickLine_DepthGap
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Use the 'Feature' option 'Selected Feature' to highlight different lines."
        desc = "Find cells that have a gap in depth from their neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub

        dst1.SetTo(0)
        For Each lp In task.lines.lpList
            dst1.Line(lp.p1, lp.p2, lp.index, task.lineWidth, cv.LineTypes.Link8)
        Next

        findCells.Run(dst1)
        dst2 = findCells.dst2
        dst3 = findCells.dst3
        labels = findCells.labels
    End Sub
End Class






Public Class BrickLine_Lines : Inherits TaskParent
    Dim findCells As New BrickLine_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Use the 'Feature' option 'Selected Feature' to highlight different lines."
        desc = "Find the cells containing lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub

        dst1.SetTo(0)
        For Each lp In task.lines.lpList
            dst1.Line(lp.p1, lp.p2, lp.index, task.lineWidth, cv.LineTypes.Link8)
        Next

        findCells.Run(dst1)
        dst2 = findCells.dst2
        dst3 = findCells.dst3
    End Sub
End Class






Public Class BrickLine_LeftRight : Inherits TaskParent
    Public Sub New()
        desc = "Display a line in both the left and right images using the bricks that contain the line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        'For i = 0 To task.featList.Count - 1
        '    Dim color = task.scalarColors(i)
        '    If task.gOptions.DebugSlider.Value = i Then
        '        For Each index In task.featList(i)
        '            Dim brick = task.bricks.brickList(index)
        '            dst2.Rectangle(brick.rect, color, task.lineWidth)
        '        Next
        '        Exit For
        '    End If
        'Next
    End Sub
End Class








Public Class BrickLine_FeatureLess : Inherits TaskParent
    Dim findCells As New BrickLine_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        findCells.edgeRequest = True
        labels(3) = "Mask of featureless regions."
        desc = "Use the edge/line cells to isolate the featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        findCells.Run(src)
        labels = findCells.labels

        dst3.SetTo(0)
        For i = 0 To task.featList.Count - 1
            For Each index In task.featList(i)
                Dim brick = task.bricks.brickList(index)
                dst3.Rectangle(brick.rect, 255, -1)
            Next
        Next

        Dim regionCount As Integer = 1
        For Each brick In task.bricks.brickList
            If dst3.Get(Of Byte)(brick.pt.Y, brick.pt.X) = 0 Then
                dst3.FloodFill(New cv.Point(brick.pt.X, brick.pt.Y), regionCount)
                regionCount += 1
            End If
        Next

        Dim fless As New List(Of List(Of Integer))
        For i = 0 To regionCount - 1
            fless.Add(New List(Of Integer))
        Next

        dst1.SetTo(0)
        For Each brick In task.bricks.brickList
            Dim val = dst3.Get(Of Byte)(brick.pt.Y, brick.pt.X)
            If val = 255 Then Continue For
            If val Then
                fless(val).Add(brick.index)
                dst1(brick.rect).SetTo(val)
            End If
        Next

        Dim regionSorted As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To fless.Count - 1
            If fless(i).Count = 1 Then
                Dim brick = task.bricks.brickList(fless(i)(0))
                dst1(brick.rect).SetTo(0)
            Else
                regionSorted.Add(fless(i).Count, i)
            End If
        Next

        task.fLess.Clear()
        For Each ele In regionSorted
            If ele.Key > 1 Then task.fLess.Add(fless(ele.Value))
        Next

        dst1.SetTo(0)
        For i = 0 To task.fLess.Count - 1
            For Each index In task.fLess(i)
                Dim brick = task.bricks.brickList(index)
                dst1(brick.rect).SetTo(i + 1)
            Next
        Next

        dst2 = ShowPalette(dst1)

        For i = 0 To task.fLess.Count - 2
            Dim brick = task.bricks.brickList(task.fLess(i)(0))
            SetTrueText(CStr(task.fLess(i).Count), brick.pt)
        Next
    End Sub
End Class