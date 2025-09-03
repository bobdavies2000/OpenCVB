Imports cv = OpenCvSharp
Public Class BrickLine_Basics : Inherits TaskParent
    Dim hist As New Hist_GridCell
    Public edgeRequest As Boolean
    Public options As New Options_Features
    Public Sub New()
        If task.bricks Is Nothing Then task.bricks = New Brick_Basics
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






Public Class BrickLine_EdgesNoEdges : Inherits TaskParent
    Public edges As New List(Of Integer)
    Public noEdges As New List(Of Integer)
    Public Sub New()
        desc = "Define each brick according to whether it has edges or not.  Ignore peripheral bricks..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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
            If task.edges.dst2(r).CountNonZero Then edges.Add(i) Else noEdges.Add(i)
        Next

        If standaloneTest() Then
            For Each index In edges
                DrawRect(dst2, task.gridRects(index), white)
            Next
            For Each index In noEdges
                DrawRect(dst3, task.gridRects(index), white)
            Next
        End If

        labels(2) = CStr(edges.Count) + " bricks had edges"
        labels(3) = CStr(noEdges.Count) + " bricks were featureless"
    End Sub
End Class





Public Class BrickLine_LeftRight : Inherits TaskParent
    Dim edges As New EdgeLine_LeftRight
    Dim fLess As New BrickLine_EdgesNoEdges
    Dim mats As New Mat_4Click
    Public bestBricks As New List(Of Integer)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(1) = "Left edges, right edges, bricks with left image edges, bricks with right image edges"
        labels(2) = "The cells below have depth and good correlation left to right"
        desc = "Display a line in both the left and right images using the bricks that contain the line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(src)
        mats.mat(0) = edges.dst2
        mats.mat(1) = edges.dst3

        fLess.Run(edges.dst2)
        mats.mat(2) = fLess.dst2.Clone
        Dim leftEdges As New List(Of Integer)(fLess.edges)
        For Each index In leftEdges
            DrawRect(mats.mat(2), task.gridRects(index), white)
        Next

        task.edges.Run(edges.dst3)
        fLess.Run(task.edges.dst2)
        mats.mat(3) = fLess.dst2
        Dim rightEdges As New List(Of Integer)(fLess.edges)
        For Each index In rightEdges
            DrawRect(mats.mat(3), task.gridRects(index), white)
        Next

        mats.Run(emptyMat)
        dst1 = mats.dst2

        dst2 = task.leftView
        dst3 = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim correlationMat As New cv.Mat
        bestBricks.Clear()
        For Each index In leftEdges
            Dim brick As New brickData
            brick.rect = task.gridRects(index)

            ' too close to the edges of the image
            If task.gridNabeRects(index).Width + brick.rect.X + task.brickSize * 2 > dst2.Width Then Continue For
            If task.gridNabeRects(index).Height + brick.rect.Y + task.brickSize * 2 > dst2.Height Then Continue For

            brick.lRect = brick.rect
            brick.depth = task.pcSplit(2)(brick.rect).Mean()(0)
            brick.age = task.motionBasics.cellAge(index)
            If brick.depth > 0 Then
                brick.rRect = brick.rect
                brick.rRect.X -= task.calibData.baseline * task.calibData.rgbIntrinsics.fx / brick.depth
                If brick.rRect.X < 0 Or brick.rRect.X + brick.rRect.Width >= dst2.Width Then Continue For

                If task.rgbLeftAligned = False Then
                    brick = Brick_Basics.RealSenseAlign(brick)
                End If

                cv.Cv2.MatchTemplate(task.leftView(brick.lRect), task.rightView(brick.rRect), correlationMat,
                                     cv.TemplateMatchModes.CCoeffNormed)

                brick.correlation = correlationMat.Get(Of Single)(0, 0)
                If brick.correlation >= task.fCorrThreshold Then
                    DrawRect(dst2, brick.rect, white)
                    DrawRect(dst3, brick.rRect, red)
                    bestBricks.Add(index)
                End If
            End If
        Next

        labels(3) = CStr(bestBricks.Count) + " bricks had lines and correlation >" + Format(task.fCorrThreshold, fmt2) + ") or " +
                  Format(bestBricks.Count / task.gridRects.Count, "00%") + " of all the bricks"
    End Sub
End Class