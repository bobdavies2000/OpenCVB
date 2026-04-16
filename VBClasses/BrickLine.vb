Imports cv = OpenCvSharp
Public Class BrickLine_Basics : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim hist As New Histogram_GridCell
    Public edgeRequest As Boolean
    Public options As New Options_Features
    Public Sub New()
        labels(2) = "Use 'Selected Feature' in 'Options_Features' to highlight different edges."
        desc = "Given lines or edges, build a grid of cells that cover them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        options.Run()

        If standalone Or edgeRequest Then
            Static contour As New Contour_RotateRect
            contour.Run(task.gray)
            src = contour.dst1.Clone
        End If

        dst2 = Palettize(src, 0)
        dst3 = dst2.Clone

        Dim mm = GetMinMax(src)
        Dim featList As New List(Of List(Of Integer))
        For i = 0 To mm.maxVal ' the 0th entry is a placeholder for the background and will have 0 entries.
            featList.Add(New List(Of Integer))
        Next

        For Each brick In bricks.brickList
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
                Dim brick = bricks.brickList(index)
                dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
            Next
        End If

        For i = 0 To task.featList.Count - 1
            If i <> Math.Abs(task.gOptions.DebugSlider.Value) Then Continue For
            Dim depthSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            For Each index In task.featList(i)
                Dim brick = bricks.brickList(index)
                depthSorted.Add(brick.depth, index)
            Next

            Dim lastDepth = depthSorted.ElementAt(0).Key
            For Each ele In depthSorted
                If Math.Abs(ele.Key - lastDepth) > task.depthDiffMeters Then
                    Dim brick = bricks.brickList(ele.Value)
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
        findCells.Run(task.gray)
        dst2 = findCells.dst2
        dst3 = findCells.dst3
    End Sub
End Class








Public Class BrickLine_DepthGap : Inherits TaskParent
    Dim findCells As New BrickLine_Basics
    Dim bricks As New Brick_Basics
    Public Sub New()
        labels(2) = "Cells highlighted below have a significant gap in depth from their neighbors."
        desc = "Find cells mapping the edges/lines which are not near any other cell - they are neighboring edges/lines but not in depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        If standalone Then
            findCells.edgeRequest = True
            findCells.Run(task.gray)
        Else
            findCells.Run(src)
        End If

        Dim gapCells As New List(Of Integer)
        For i = 0 To task.featList.Count - 1
            If task.featList(i).Count = 0 Then Exit For
            Dim depthSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
            For Each index In task.featList(i)
                Dim brick = bricks.brickList(index)
                depthSorted.Add(brick.depth, index)
            Next

            Dim lastDepth = depthSorted.ElementAt(0).Key
            For Each ele In depthSorted
                If Math.Abs(ele.Key - lastDepth) > task.depthDiffMeters Then gapCells.Add(bricks.brickList(ele.Value).index)
                lastDepth = ele.Key
            Next
        Next

        If task.heartBeat Then
            dst2 = findCells.dst2.Clone
            Dim debugMode = task.gOptions.DebugSlider.Value <> 0
            For i = 0 To gapCells.Count - 1
                If debugMode Then If i <> Math.Abs(task.gOptions.DebugSlider.Value) Then Continue For
                Dim brick = bricks.brickList(gapCells(i))
                dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
                If i = Math.Abs(task.gOptions.DebugSlider.Value) Then
                    SetTrueText(Format(brick.depth, fmt1), brick.rect.BottomRight)
                End If
            Next
        End If
        labels(3) = CStr(task.featList.Count) + " features are present in the input lines or edges"
    End Sub
End Class






Public Class NR_BrickLine_DepthGaps : Inherits TaskParent
    Dim findCells As New BrickLine_DepthGap
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Use the 'Feature' option 'Selected Feature' to highlight different lines."
        desc = "Find cells that have a gap in depth from their neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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






Public Class NR_BrickLine_Lines : Inherits TaskParent
    Dim findCells As New BrickLine_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Use the 'Feature' option 'Selected Feature' to highlight different lines."
        desc = "Find the cells containing lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        For Each lp In task.lines.lpList
            dst1.Line(lp.p1, lp.p2, lp.index, task.lineWidth, cv.LineTypes.Link8)
        Next

        findCells.Run(dst1)
        dst2 = findCells.dst2
        dst3 = findCells.dst3
    End Sub
End Class






Public Class BrickLine_EdgesNoEdges : Inherits TaskParent
    Public edges As New List(Of Integer)
    Public noEdges As New List(Of Integer)
    Dim edgeline As New EdgeLine_Basics
    Public Sub New()
        desc = "Define each grid square according to whether it has edges or not.  Ignore peripheral bricks..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edgeline.Run(task.gray)
        dst2 = src.Clone
        dst3 = src.Clone
        edges.Clear()
        noEdges.Clear()
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            If r.X = 0 Then Continue For
            If r.X + r.Width = dst2.Width Then Continue For
            If r.Y = 0 Then Continue For
            If r.Y + r.Height = dst2.Height Then Continue For
            If edgeline.dst2(r).CountNonZero Then edges.Add(i) Else noEdges.Add(i)
        Next

        If standaloneTest() Then
            For Each index In edges
                DrawRect(dst2, task.gridRects(index), white)
            Next
            For Each index In noEdges
                DrawRect(dst3, task.gridRects(index), white)
            Next
        End If

        dst3.SetTo(white, edgeline.dst2)
        labels(2) = CStr(edges.Count) + " bricks had edges"
        labels(3) = CStr(noEdges.Count) + " bricks were featureless"
    End Sub
End Class





Public Class BrickLine_LeftRightMotion : Inherits TaskParent
    Dim edges As New EdgeLine_LeftRightMotion
    Dim fLess As New BrickLine_EdgesNoEdges
    Dim mats As New Mat_4Click
    Public bestBricks As New List(Of Integer)
    Dim edgeline As New EdgeLine_Basics
    Public Sub New()
        labels(1) = "Left edges, right edges, bricks with left image edges, bricks with right image edges"
        labels(2) = "The cells below have depth and good correlation left to right"
        desc = "Display a line in both the left and right images using the bricks that contain the line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(src)
        mats.mat(0) = edges.dst2.Clone
        mats.mat(1) = edges.dst3.Clone

        fLess.Run(edges.dst2)
        mats.mat(2) = fLess.dst2.Clone
        Dim leftEdges As New List(Of Integer)(fLess.edges)
        For Each index In leftEdges
            DrawRect(mats.mat(2), task.gridRects(index), white)
        Next

        edgeline.Run(edges.dst3)
        fLess.Run(edgeline.dst2)
        mats.mat(3) = fLess.dst2.Clone
        Dim rightEdges As New List(Of Integer)(fLess.edges)
        For Each index In rightEdges
            DrawRect(mats.mat(3), task.gridRects(index), white)
        Next

        '  mats.Run(emptyMat)

        dst2 = task.leftView
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim correlationMat As New cv.Mat
        bestBricks.Clear()
        For Each index In leftEdges
            Dim r As New brickData
            r.rect = task.gridRects(index)

            ' too close to the edges of the image
            If task.gridNabeRects(index).Width + r.rect.X + task.brickEdgeLen * 2 > dst2.Width Then Continue For
            If task.gridNabeRects(index).Height + r.rect.Y + task.brickEdgeLen * 2 > dst2.Height Then Continue For

            r.lRect = r.rect
            r.depth = task.pcSplit(2)(r.rect).Mean()(0)
            If r.depth > 0 Then
                r.rRect = r.rect
                r.rRect.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / r.depth
                If r.rRect.X < 0 Or r.rRect.X + r.rRect.Width >= dst2.Width Then Continue For

                cv.Cv2.MatchTemplate(task.leftView(r.lRect), task.rightView(r.rRect), correlationMat,
                                         cv.TemplateMatchModes.CCoeffNormed)

                r.correlation = correlationMat.Get(Of Single)(0, 0)
                If r.correlation >= task.fCorrThreshold Then
                    DrawRect(dst2, r.rect, white)
                    DrawRect(dst3, r.rRect, red)
                    bestBricks.Add(index)
                End If
            End If
        Next

        labels(3) = CStr(bestBricks.Count) + " bricks had lines and correlation >" + Format(task.fCorrThreshold, fmt2) + ") or " +
                      Format(bestBricks.Count / task.gridRects.Count, "00%") + " of all the bricks"
    End Sub
End Class





Public Class BrickLine_Find1 : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim side As Integer
    Dim pixels(side * side) As cv.Point
    Public Sub New()
        side = task.gOptions.GridSlider.Value
        ReDim pixels(side * side)
        desc = "Find lines within each brick."
    End Sub
    Public Shared Function testPixels(pixels() As cv.Point) As lpData
        Dim testX As Boolean = True
        Dim testY As Boolean = True

        For j = 1 To pixels.Count - 1
            If Math.Abs(pixels(j - 1).X - pixels(j).X) > 1 Then
                testX = False
                Exit For
            End If
        Next

        For j = 1 To pixels.Count - 1
            If Math.Abs(pixels(j - 1).Y - pixels(j).Y) > 1 Then
                testX = False
                Exit For
            End If
        Next
        If testX Or testY Then Return New lpData(pixels(0), pixels(pixels.Count - 1))
        Return Nothing
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.gray)
        dst2 = edges.dst2
        labels(2) = edges.labels(2)

        dst3.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)
            Dim pixelCount = dst2(r).CountNonZero
            If pixelCount = 0 Or pixelCount > 20 Then Continue For
            Dim pixelMat = dst2(r).FindNonZero

            pixelMat.GetArray(Of cv.Point)(pixels)
            If task.drawRect.Contains(r.TopLeft) Then Dim k = 0

            Dim lp = testPixels(pixels)
            If lp IsNot Nothing Then
                dst3(r).Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
            End If
        Next
    End Sub
End Class






Public Class BrickLine_Finder : Inherits TaskParent
    Dim find As New BrickLine_Find1
    Dim lines As New Line_Basics_TA
    Public Sub New()
        desc = "Find lines in the brickline_find output"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        find.Run(task.gray)
        dst2 = find.dst3

        lines.run(dst2)
        dst3 = lines.dst2
        labels(3) = lines.labels(2)
    End Sub
End Class





'Public Class BrickLine_KNN : Inherits TaskParent
'    Dim knn As KNN_Basics
'    Dim edges As New Edge_Basics
'    Dim side As Integer
'    Dim pixels(side * side) As cv.Point
'    Public Sub New()
'        desc = "Use KNN to find lines in each grid rect."
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        edges.Run(task.gray)
'        dst2 = edges.dst2
'        labels(2) = edges.labels(2)

'        dst3.SetTo(0)
'        For Each r In task.gridRects
'            Dim pixelCount = dst2(r).CountNonZero
'            If pixelCount = 0 Or pixelCount > 20 Then Continue For
'            Dim pixelMat = dst2(r).FindNonZero

'            pixelMat.GetArray(Of cv.Point)(pixels)
'            If task.drawRect.Contains(r.TopLeft) Then Dim k = 0

'            knn.queries.Clear()
'            For Each pt In pixels
'                knn.queries.Add(New cv.Point2f(pt.X, pt.Y))
'            Next
'            knn.trainInput = New List(Of cv.Point2f)(knn.queries)
'            knn.Run(emptyMat)


'            For i = 0 To knn.queries.Count - 1
'                Dim p1 = knn.queries(i)
'                While 1
'                    Dim index = knn.result(i, 1)
'                    Dim p2 = knn.queries(index)
'                    Dim distance = p1.DistanceTo(p2)
'                    If distance >= 2 Then Exit While

'                End While
'                Dim p2 = knn.queries(knn.result)
'            Next
'        Next
'    End Sub
'End Class





Public Class BrickLine_Find : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim side As Integer
    Dim pixels(side * side) As cv.Point
    Dim pNothing As New cv.Point(-1, -1)
    Public Sub New()
        side = task.gOptions.GridSlider.Value
        ReDim pixels(side * side)
        desc = "Find lines within each brick."
    End Sub
    Public Function testPixels(pixels() As cv.Point) As List(Of lpData)
        Dim lpList As New List(Of lpData)
        Dim sortX As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
        For Each pt In pixels
            sortX.Add(pt.X, pt)
        Next

        Dim xList As New List(Of cv.Point)(sortX.Values)
        Dim p1 As cv.Point = pNothing, p2 As cv.Point
        For i = 1 To xList.Count - 1
            If p1.X = -1 Or Math.Abs(xList(i).Y - xList(i - 1).Y) > 1 Then
                p1 = xList(i - 1)
            Else
                p2 = xList(i - 1)
                If Math.Abs(xList(i).X - p2.X) > 1 Or i = xList.Count - 1 Then
                    lpList.Add(New lpData(p1, p2))
                    p1 = pNothing
                End If
            End If
        Next
        Return lpList
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.gray)
        dst2 = edges.dst2
        labels(2) = edges.labels(2)

        dst3.SetTo(0)
        For i = 0 To task.gridRects.Count - 1
            Dim r = task.gridRects(i)

            Dim pixelCount = dst2(r).CountNonZero
            If pixelCount = 0 Or pixelCount > 20 Then Continue For
            If pixelCount < side Then Continue For

            Dim pixelMat = dst2(r).FindNonZero

            pixelMat.GetArray(Of cv.Point)(pixels)
            If task.drawRect.Contains(r.TopLeft) Then Dim k = 0

            Dim lpList = testPixels(pixels)
            For Each lp In lpList
                dst3(r).Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
            Next
        Next
    End Sub
End Class