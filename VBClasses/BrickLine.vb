Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class BrickLine_Basics : Inherits TaskParent
        Dim hist As New Histogram_GridCell
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

            dst2 = PaletteBlackZero(src)
            dst3 = dst2.Clone

            Dim mm = GetMinMax(src)
            Dim featList As New List(Of List(Of Integer))
            For i = 0 To mm.maxVal ' the 0th entry is a placeholder for the background and will have 0 entries.
                featList.Add(New List(Of Integer))
            Next

            For Each gr In task.bricks.brickList
                hist.Run(src(gr.rect))
                For i = 1 To hist.histarray.Count - 1
                    If hist.histarray(i) > 0 Then
                        featList(i).Add(gr.index)
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
                    Dim gr = task.bricks.brickList(index)
                    dst2.Rectangle(gr.rect, task.highlight, task.lineWidth)
                Next
            End If

            For i = 0 To task.featList.Count - 1
                If i <> Math.Abs(task.gOptions.DebugSlider.Value) Then Continue For
                Dim depthSorted As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingle)
                For Each index In task.featList(i)
                    Dim gr = task.bricks.brickList(index)
                    depthSorted.Add(gr.depth, index)
                Next

                Dim lastDepth = depthSorted.ElementAt(0).Key
                For Each ele In depthSorted
                    If Math.Abs(ele.Key - lastDepth) > task.depthDiffMeters Then
                        Dim gr = task.bricks.brickList(ele.Value)
                        dst2.Rectangle(gr.rect, red, task.lineWidth + 1)
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
            If task.bricks Is Nothing Then task.bricks = New Brick_Basics
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
                    Dim gr = task.bricks.brickList(index)
                    depthSorted.Add(gr.depth, index)
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
                    Dim gr = task.bricks.brickList(gapCells(i))
                    dst2.Rectangle(gr.rect, task.highlight, task.lineWidth)
                    If i = Math.Abs(task.gOptions.DebugSlider.Value) Then
                        SetTrueText(Format(gr.depth, fmt1), gr.rect.BottomRight)
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
            desc = "Define each gr according to whether it has edges or not.  Ignore peripheral bricks..."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edgeline.Run(task.grayStable)
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





    Public Class BrickLine_LeftRight : Inherits TaskParent
        Dim edges As New EdgeLine_LeftRight
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
                Dim gr As New brickData
                gr.rect = task.gridRects(index)

                ' too close to the edges of the image
                If task.gridNabeRects(index).Width + gr.rect.X + task.brickSize * 2 > dst2.Width Then Continue For
                If task.gridNabeRects(index).Height + gr.rect.Y + task.brickSize * 2 > dst2.Height Then Continue For

                gr.lRect = gr.rect
                gr.depth = task.pcSplit(2)(gr.rect).Mean()(0)
                If gr.depth > 0 Then
                    gr.rRect = gr.rect
                    gr.rRect.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / gr.depth
                    If gr.rRect.X < 0 Or gr.rRect.X + gr.rRect.Width >= dst2.Width Then Continue For

                    cv.Cv2.MatchTemplate(task.leftView(gr.lRect), task.rightView(gr.rRect), correlationMat,
                                     cv.TemplateMatchModes.CCoeffNormed)

                    gr.correlation = correlationMat.Get(Of Single)(0, 0)
                    If gr.correlation >= task.fCorrThreshold Then
                        DrawRect(dst2, gr.rect, white)
                        DrawRect(dst3, gr.rRect, red)
                        bestBricks.Add(index)
                    End If
                End If
            Next

            labels(3) = CStr(bestBricks.Count) + " bricks had lines and correlation >" + Format(task.fCorrThreshold, fmt2) + ") or " +
                  Format(bestBricks.Count / task.gridRects.Count, "00%") + " of all the bricks"
        End Sub
    End Class
End Namespace