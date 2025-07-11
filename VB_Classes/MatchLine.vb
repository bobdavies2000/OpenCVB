Imports cv = OpenCvSharp
Public Class MatchLine_Basics : Inherits TaskParent
    Public lpInput As lpData
    Public lpOutput As lpData
    Dim match As New Match_Basics
    Public correlation1 As Single
    Public correlation2 As Single
    Public Sub New()
        match.template = New cv.Mat
        desc = "Get the end points of the gravity RGB vector and compare them to the original template."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            lpInput = task.gravityBasics.gravityRGB
            If lpInput.template1.Width = 0 Then
                SetTrueText("lpInput template was not found.", 3)
                Exit Sub
            End If
        End If

        match.template = lpInput.template1.Clone
        Dim nabeRect1 = task.gridNabeRects(task.grid.gridMap.Get(Of Single)(lpInput.p1.Y, lpInput.p1.X))
        Dim nabeRect2 = task.gridNabeRects(task.grid.gridMap.Get(Of Single)(lpInput.p2.Y, lpInput.p2.X))

        match.Run(src(nabeRect1))
        correlation1 = match.correlation
        Dim offsetx1 = match.newRect.TopLeft.X - task.cellSize
        Dim offsety1 = match.newRect.TopLeft.Y - task.cellSize
        Dim p1 = New cv.Point(lpInput.p1.X + offsetx1, lpInput.p1.Y + offsety1)

        match.template = lpInput.template2.Clone
        match.Run(src(nabeRect2))
        correlation2 = match.correlation
        Dim offsetX2 = match.newRect.TopLeft.X - task.cellSize
        Dim offsetY2 = match.newRect.TopLeft.Y - task.cellSize
        Dim p2 = New cv.Point(lpInput.p2.X + offsetX2, lpInput.p2.Y + offsetY2)

        lpOutput = New lpData(p1, p2)

        If standaloneTest() Then
            Dim tmp As New cv.Mat
            cv.Cv2.HConcat(lpInput.template1, lpInput.template2, tmp)
            Dim sz = New cv.Size(dst2.Width, tmp.Height * dst2.Width / tmp.Width)
            tmp = tmp.Resize(sz)
            tmp.CopyTo(dst2(New cv.Rect(0, 0, sz.Width, sz.Height)))
            labels(2) = "Correlation1 = " + Format(correlation1, fmt3) + " and correlation2 = " + Format(correlation2, fmt3)

            dst3 = src
            DrawLine(dst3, lpInput.p1, lpInput.p2)
            labels(3) = "OffsetX1 = " + CStr(offsetx1) + "  OffsetY1 = " + CStr(offsety1) + "  " +
                        "OffsetX2 = " + CStr(offsetX2) + "  OffsetY2 = " + CStr(offsetY2)
        End If
    End Sub
End Class






Public Class MatchLine_BasicsAll : Inherits TaskParent
    Public cameraMotionProxy As New lpData
    Dim match As New MatchLine_Basics
    Public correlations As New List(Of Single)
    Public Sub New()
        task.featureOptions.MatchCorrSlider.Value = 90
        desc = "Track each of the lines found in LineRGB_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        correlations.Clear()
        For Each lp In task.lineRGB.lpList
            match.lpInput = lp
            match.Run(src)
            correlations.Add(match.correlation1)
            correlations.Add(match.correlation2)
            If match.correlation1 > task.fCorrThreshold And match.correlation2 > task.fCorrThreshold Then
                DrawLine(dst2, lp.p1, lp.p2)
            End If
            dst2.Rectangle(lp.rect, task.highlight, task.lineWidth)
            'dst2.Rectangle(lp.gridRect2, task.highlight, task.lineWidth)
            'dst2.Rectangle(lp.nabeRect1, task.highlight, task.lineWidth)
            'dst2.Rectangle(lp.nabeRect2, task.highlight, task.lineWidth)
            DrawLine(dst2, lp.p1, lp.p2)
            labels(2) = "Left rect has correlation " + Format(match.correlation1, fmt3) + " and " +
                        "Right rect has " + Format(match.correlation2, fmt3)
            Exit For
        Next

        dst3 = task.lineRGB.dst3
        labels(3) = task.lineRGB.labels(3)
    End Sub
End Class





Public Class MatchLine_BasicsOriginal : Inherits TaskParent
    Public match As New Match_Basics
    Public lpInput As New lpData
    Public lpOutput As lpData
    Public corner1 As Integer, corner2 As Integer
    Dim lpSave As New lpData
    Dim knn As New KNN_ClosestTracker
    Public Sub New()
        desc = "Find and track a line in the BGR image."
    End Sub
    Private Function cornerToPoint(whichCorner As Integer, r As cv.Rect) As cv.Point2f
        Select Case whichCorner
            Case 0
                Return r.TopLeft
            Case 1
                Return New cv.Point2f(r.BottomRight.X, r.TopLeft.Y)
            Case 2
                Return r.BottomRight
        End Select
        Return New cv.Point2f(r.TopLeft.X, r.BottomRight.Y)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        If match.correlation < task.fCorrThreshold Or lpSave.p1 <> lpInput.p1 Or lpSave.p2 <> lpInput.p2 Then
            lpSave = lpInput
            ' default to longest line
            If standalone Then lpInput = task.lineRGB.lpList(0)

            Dim r = ValidateRect(New cv.Rect(Math.Min(lpInput.p1.X, lpInput.p2.X), Math.Min(lpInput.p1.Y, lpInput.p2.Y),
                                             Math.Abs(lpInput.p1.X - lpInput.p2.X), Math.Abs(lpInput.p1.Y - lpInput.p2.Y)))
            match.template = src(r).Clone

            Dim p1 = New cv.Point(CInt(lpInput.p1.X), CInt(lpInput.p1.Y))
            ' Determine which corner - numbering topleft = 0 clockwise, 1, 2, 3
            If r.TopLeft.DistanceTo(p1) <= 2 Then
                corner1 = 0
                corner2 = 2
            ElseIf r.BottomRight.DistanceTo(p1) <= 2 Then
                corner1 = 2
                corner2 = 0
            ElseIf r.Y = p1.Y Then
                corner1 = 1
                corner2 = 3
            Else
                corner1 = 3
                corner2 = 1
            End If
        End If

        match.Run(src)
        If match.correlation >= task.fCorrThreshold Then
            If standaloneTest() Then dst3 = match.dst0.Resize(dst3.Size)
            Dim p1 = cornerToPoint(corner1, match.newRect)
            Dim p2 = cornerToPoint(corner2, match.newRect)
            dst2.Line(p1, p2, task.highlight, task.lineWidth + 2, task.lineType)
            lpOutput = New lpData(p1, p2)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class





Public Class MatchLine_Longest : Inherits TaskParent
    Public knn As New KNN_ClosestTracker
    Public matchLine As New MatchLine_BasicsOriginal
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        knn.Run(src.Clone)
        matchLine.lpInput = New lpData(knn.lastPair.p1, knn.lastPair.p2)

        matchLine.Run(src)
        dst2 = matchLine.dst2
        DrawLine(dst2, matchLine.lpOutput.p1, matchLine.lpOutput.p2, cv.Scalar.Red)

        labels(2) = "Longest line end points had correlation of " + Format(matchLine.match.correlation, fmt3) +
                    " with the original longest line."
    End Sub
End Class





Public Class MatchLine_Horizon : Inherits TaskParent
    Dim matchLine As New MatchLine_BasicsOriginal
    Public Sub New()
        desc = "Verify the horizon using MatchTemplate."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        'If matchLine.match.correlation < matchLine.match.options.correlationThreshold Then matchLine.lpInput = task.horizonVec
        If task.quarterBeat Then matchLine.lpInput = task.horizonVec
        matchLine.Run(src)
        dst2 = matchLine.dst2
        DrawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Red)
        labels(2) = "MatchLine correlation = " + Format(matchLine.match.correlation, fmt3) + " - Red = current horizon, yellow is matchLine output"
    End Sub
End Class




Public Class MatchLine_Gravity : Inherits TaskParent
    Dim matchLine As New MatchLine_BasicsOriginal
    Public Sub New()
        desc = "Verify the gravity vector using MatchTemplate."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        matchLine.lpInput = task.gravityVec
        matchLine.Run(src)
        dst2 = matchLine.dst2
        DrawLine(dst2, task.gravityVec.p1, task.gravityVec.p2, cv.Scalar.Red)
        labels(2) = "MatchLine correlation = " + Format(matchLine.match.correlation, fmt3) +
                    " - Red = current gravity vector, yellow is matchLine output"
    End Sub
End Class
Public Class MatchLine_Test : Inherits TaskParent
    Public cameraMotionProxy As New lpData
    Dim match As New MatchLine_Basics
    Public Sub New()
        desc = "Find and track the longest line by matching line bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then task.lineRGB.lpList.Clear()

        dst2 = src.Clone
        If task.lineRGB.lpList.Count > 0 Then
            cameraMotionProxy = task.lineRGB.lpList(0)
            match.lpInput = cameraMotionProxy
            match.Run(src)
            dst1 = match.dst2

            labels(2) = "EndPoint1 correlation:  " + Format(match.correlation1, fmt3) + vbTab +
                        "EndPoint2 correlation:  " + Format(match.correlation1, fmt3)

            If match.correlation1 < task.fCorrThreshold Or task.frameCount < 10 Or
               match.correlation2 < task.fCorrThreshold Then

                task.motionMask.SetTo(255) ' force a complete line detection
                task.lineRGB.Run(src.Clone)
                If task.lineRGB.lpList.Count = 0 Then Exit Sub

                match.lpInput = task.lineRGB.lpList(0)
                match.Run(src)
            End If
        End If

        dst3 = task.lineRGB.dst3
        labels(3) = task.lineRGB.labels(3)

        dst2.Line(cameraMotionProxy.p1, cameraMotionProxy.p2, task.highlight, task.lineWidth, task.lineType)
    End Sub
End Class






Public Class MatchLine_VH : Inherits TaskParent
    Public brickCells As New List(Of gravityLine)
    Dim match As New Match_tCell
    Dim gLines As New LineRGB_GCloud
    Dim options As New Options_Features
    Public Sub New()
        labels(3) = "More readable than dst1 - index, correlation, length (meters), and ArcY"
        desc = "Find and track all the horizontal or vertical lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim templatePad = options.templatePad
        ' gLines.lines.subsetRect = New cv.Rect(templatePad * 3, templatePad * 3, src.Width - templatePad * 6, src.Height - templatePad * 6)
        gLines.Run(src)

        Dim sortedLines = If(task.verticalLines, gLines.sortedVerticals, gLines.sortedHorizontals)
        If sortedLines.Count = 0 Then
            SetTrueText("There were no vertical lines found.", 3)
            Exit Sub
        End If

        Dim brick As gravityLine
        brickCells.Clear()
        match.tCells.Clear()
        For i = 0 To sortedLines.Count - 1
            brick = sortedLines.ElementAt(i).Value

            If i = 0 Then
                dst1.SetTo(0)
                brick.tc1.template.CopyTo(dst1(brick.tc1.rect))
                brick.tc2.template.CopyTo(dst1(brick.tc2.rect))
            End If

            match.tCells.Clear()
            match.tCells.Add(brick.tc1)
            match.tCells.Add(brick.tc2)

            match.Run(src)
            If match.tCells(0).correlation >= options.correlationThreshold And match.tCells(1).correlation >= options.correlationThreshold Then
                brick.tc1 = match.tCells(0)
                brick.tc2 = match.tCells(1)
                brick = gLines.updateGLine(src, brick, brick.tc1.center, brick.tc2.center)
                If brick.len3D > 0 Then brickCells.Add(brick)
            End If
        Next

        dst2 = src
        dst3.SetTo(0)
        For i = 0 To brickCells.Count - 1
            Dim tc As tCell
            brick = brickCells(i)
            Dim p1 As cv.Point2f, p2 As cv.Point2f
            For j = 0 To 2 - 1
                tc = Choose(j + 1, brick.tc1, brick.tc2)
                If j = 0 Then p1 = tc.center Else p2 = tc.center
            Next
            SetTrueText(CStr(i) + vbCrLf + tc.strOut + vbCrLf + Format(brick.arcY, fmt1), brick.tc1.center, 2)
            SetTrueText(CStr(i) + vbCrLf + tc.strOut + vbCrLf + Format(brick.arcY, fmt1), brick.tc1.center, 3)

            DrawLine(dst2, p1, p2, task.highlight)
            DrawLine(dst3, p1, p2, task.highlight)
        Next
    End Sub
End Class