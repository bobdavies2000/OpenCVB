Imports cv = OpenCvSharp
Public Class FindCells_Basics : Inherits TaskParent
    Dim hist As New Hist_GridCell
    Public edgeRequest As Boolean
    Public Sub New()
        If standalone Then task.featureOptions.SelectedFeature.Value = 1
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

        For Each gc In task.gcList
            hist.Run(src(gc.rect))
            For i = 1 To hist.histarray.Count - 1
                If hist.histarray(i) > 0 Then
                    featList(i).Add(gc.index)
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
                Dim gc = task.gcList(index)
                dst2.Rectangle(gc.rect, task.highlight, task.lineWidth)
            Next
        End If

        If debugMode Then
            For i = 0 To task.featList.Count - 1
                If debugMode Then If i <> task.selectedFeature Then Continue For
                Dim depthSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
                For Each index In task.featList(i)
                    Dim gc = task.gcList(index)
                    depthSorted.Add(gc.depth, index)
                Next

                Dim lastDepth = depthSorted.ElementAt(0).Key
                For Each ele In depthSorted
                    If Math.Abs(ele.Key - lastDepth) > task.depthDiffMeters Then
                        Dim gc = task.gcList(ele.Value)
                        dst2.Rectangle(gc.rect, red, task.lineWidth + 1)
                    End If
                    lastDepth = ele.Key
                Next
            Next
        End If
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
        If task.lpList.Count = 0 Then Exit Sub

        dst1.SetTo(0)
        For Each lp In task.lpList
            dst1.Line(lp.p1, lp.p2, lp.index, task.lineWidth, cv.LineTypes.Link8)
        Next

        findCells.Run(dst1)
        dst2 = findCells.dst2
        dst3 = findCells.dst3
    End Sub
End Class







Public Class FindCells_EdgeGaps : Inherits TaskParent
    Dim findCells As New FindCells_Basics
    Public Sub New()
        findCells.edgeRequest = True
        task.featureOptions.SelectedFeature.Value = 0
        labels(2) = "Cells highlighted below have a significant gap in depth from their edge neighbors."
        desc = "Find all the cells mapping the edges which are not near any other cell - they are neighboring edges but not in depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        findCells.Run(task.grayStable)

        Dim gapCells As New List(Of Integer)
        For i = 0 To task.featList.Count - 1
            If task.featList(i).Count = 0 Then Exit For
            Dim depthSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            For Each index In task.featList(i)
                Dim gc = task.gcList(index)
                depthSorted.Add(gc.depth, index)
            Next

            Dim lastDepth = depthSorted.ElementAt(0).Key
            For Each ele In depthSorted
                If Math.Abs(ele.Key - lastDepth) > task.depthDiffMeters Then gapCells.Add(task.gcList(ele.Value).index)
                lastDepth = ele.Key
            Next
        Next

        If task.heartBeat Then
            dst2 = findCells.dst2
            Dim debugMode = task.selectedFeature <> 0
            For i = 0 To gapCells.Count - 1
                If debugMode Then If i <> task.selectedFeature Then Continue For
                Dim gc = task.gcList(gapCells(i))
                dst2.Rectangle(gc.rect, task.highlight, task.lineWidth)
                If i = task.selectedFeature Then
                    SetTrueText(Format(gc.depth, fmt1), gc.rect.BottomRight)
                End If
            Next
        End If
    End Sub
End Class
