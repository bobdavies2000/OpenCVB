Imports cv = OpenCvSharp
Public Class FindCells_Basics : Inherits TaskParent
    Dim hist As New Hist_GridCell
    Public edgeRequest As Boolean
    Public Sub New()
        If standalone Then task.featureOptions.SelectedFeature.Value = 1
        labels(2) = "Use the 'Feature' option 'Selected Feature' to highlight different edges."
        desc = "Given lines or edges, build a grid of cells that cover them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Or edgeRequest Then
            Static contour As New FindContours_Basics
            contour.Run(task.grayStable)
            src = contour.dst1.Clone
        End If

        dst2 = ShowPalette(src)
        dst3 = dst2.Clone

        Dim debugMode = task.selectedFeature > 0

        Dim mm = GetMinMax(src)
        Dim featList As New List(Of List(Of Integer))
        For i = 0 To mm.maxVal ' the 0th entry is a placeholder for the background and will have 0 entries.
            featList.Add(New List(Of Integer))
        Next

        For Each brick In task.brickList
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

        Dim edgeIndex = task.selectedFeature
        If edgeIndex <> 0 And edgeIndex < task.featList.Count Then
            For Each index In task.featList(edgeIndex)
                Dim brick = task.brickList(index)
                dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
            Next
        End If

        If debugMode Then
            For i = 0 To task.featList.Count - 1
                If debugMode Then If i <> task.selectedFeature Then Continue For
                Dim depthSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
                For Each index In task.featList(i)
                    Dim brick = task.brickList(index)
                    depthSorted.Add(brick.depth, index)
                Next

                Dim lastDepth = depthSorted.ElementAt(0).Key
                For Each ele In depthSorted
                    If Math.Abs(ele.Key - lastDepth) > task.depthDiffMeters Then
                        Dim brick = task.brickList(ele.Value)
                        dst2.Rectangle(brick.rect, red, task.lineWidth + 1)
                    End If
                    lastDepth = ele.Key
                Next
            Next
        End If
        labels(3) = CStr(task.featList.Count) + " features are present in the input lines or edges"
    End Sub
End Class







Public Class FindCells_Edges : Inherits TaskParent
    Dim findCells As New FindCells_Basics
    Public Sub New()
        If standalone Then task.featureOptions.SelectedFeature.Value = 1
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







Public Class FindCells_Lines : Inherits TaskParent
    Dim findCells As New FindCells_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.featureOptions.SelectedFeature.Value = 1
        labels(3) = "Use the 'Feature' option 'Selected Feature' to highlight different lines."
        desc = "Find the cells containing lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lineRGB.lpList.Count = 0 Then Exit Sub

        dst1.SetTo(0)
        For Each lp In task.lineRGB.lpList
            dst1.Line(lp.p1, lp.p2, lp.index, task.lineWidth, cv.LineTypes.Link8)
        Next

        findCells.Run(dst1)
        dst2 = findCells.dst2
        dst3 = findCells.dst3
    End Sub
End Class







Public Class FindCells_Gaps : Inherits TaskParent
    Dim findCells As New FindCells_Basics
    Public Sub New()
        task.featureOptions.SelectedFeature.Value = 0
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
                Dim brick = task.brickList(index)
                depthSorted.Add(brick.depth, index)
            Next

            Dim lastDepth = depthSorted.ElementAt(0).Key
            For Each ele In depthSorted
                If Math.Abs(ele.Key - lastDepth) > task.depthDiffMeters Then gapCells.Add(task.brickList(ele.Value).index)
                lastDepth = ele.Key
            Next
        Next

        If task.heartBeat Then
            dst2 = findCells.dst2.Clone
            Dim debugMode = task.selectedFeature <> 0
            For i = 0 To gapCells.Count - 1
                If debugMode Then If i <> task.selectedFeature Then Continue For
                Dim brick = task.brickList(gapCells(i))
                dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
                If i = task.selectedFeature Then
                    SetTrueText(Format(brick.depth, fmt1), brick.rect.BottomRight)
                End If
            Next
        End If
        labels(3) = CStr(task.featList.Count) + " features are present in the input lines or edges"
    End Sub
End Class






Public Class FindCells_LineGaps : Inherits TaskParent
    Dim findCells As New FindCells_Gaps
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Use the 'Feature' option 'Selected Feature' to highlight different lines."
        desc = "Find cells that have a gap in depth from their neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lineRGB.lpList.Count = 0 Then Exit Sub

        dst1.SetTo(0)
        For Each lp In task.lineRGB.lpList
            dst1.Line(lp.p1, lp.p2, lp.index, task.lineWidth, cv.LineTypes.Link8)
        Next

        findCells.Run(dst1)
        dst2 = findCells.dst2
        dst3 = findCells.dst3
        labels = findCells.labels
    End Sub
End Class







Public Class FindCells_FeatureLess : Inherits TaskParent
    Dim findCells As New FindCells_Basics
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
                Dim brick = task.brickList(index)
                dst3.Rectangle(brick.rect, 255, -1)
            Next
        Next

        Dim regionCount As Integer = 1
        For Each brick In task.brickList
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
        For Each brick In task.brickList
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
                Dim brick = task.brickList(fless(i)(0))
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
                Dim brick = task.brickList(index)
                dst1(brick.rect).SetTo(i + 1)
            Next
        Next

        dst2 = ShowPalette(dst1)

        For i = 0 To task.fLess.Count - 2
            Dim brick = task.brickList(task.fLess(i)(0))
            SetTrueText(CStr(task.fLess(i).Count), brick.pt)
        Next
    End Sub
End Class







Public Class FindCells_RedCloud : Inherits TaskParent
    Dim findCells As New FindCells_FeatureLess
    Public Sub New()
        desc = "Run RedCloud after identifying all the featureless regions."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        findCells.Run(task.grayStable)

        dst2 = runRedC(src, labels(2), findCells.dst1)

    End Sub
End Class





