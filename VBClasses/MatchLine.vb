﻿Imports cv = OpenCvSharp







Public Class MatchLine_BasicsAll : Inherits TaskParent
    Public cameraMotionProxy As New lpData
    Dim match As New XO_MatchLine_Basics
    Public correlations As New List(Of Single)
    Public Sub New()
        task.featureOptions.MatchCorrSlider.Value = 90
        desc = "Track each of the lines found in Line_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        correlations.Clear()
        For Each lp In task.lines.lpList
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

        dst3 = task.lines.dst3
        labels(3) = task.lines.labels(3)
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
            If standalone Then lpInput = task.lines.lpList(0)

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
        'If matchLine.match.correlation < matchLine.match.options.correlationThreshold Then matchLine.lpInput = task.lineHorizon
        If task.quarterBeat Then matchLine.lpInput = task.lineHorizon
        matchLine.Run(src)
        dst2 = matchLine.dst2
        DrawLine(dst2, task.lineHorizon.p1, task.lineHorizon.p2, cv.Scalar.Red)
        labels(2) = "MatchLine correlation = " + Format(matchLine.match.correlation, fmt3) + " - Red = current horizon, yellow is matchLine output"
    End Sub
End Class




Public Class MatchLine_Gravity : Inherits TaskParent
    Dim matchLine As New MatchLine_BasicsOriginal
    Public Sub New()
        desc = "Verify the gravity vector using MatchTemplate."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        matchLine.lpInput = task.lineGravity
        matchLine.Run(src)
        dst2 = matchLine.dst2
        DrawLine(dst2, task.lineGravity.p1, task.lineGravity.p2, cv.Scalar.Red)
        labels(2) = "MatchLine correlation = " + Format(matchLine.match.correlation, fmt3) +
                    " - Red = current gravity vector, yellow is matchLine output"
    End Sub
End Class







Public Class MatchLine_VH : Inherits TaskParent
    Public brickCells As New List(Of gravityLine)
    Dim match As New Match_tCell
    Dim gLines As New Line_GCloud
    Public Sub New()
        labels(3) = "More readable than dst1 - index, correlation, length (meters), and ArcY"
        desc = "Find and track all the horizontal or vertical lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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
            Dim threshold = task.fCorrThreshold
            If match.tCells(0).correlation >= threshold And match.tCells(1).correlation >= threshold Then
                brick.tc1 = match.tCells(0)
                brick.tc2 = match.tCells(1)
                brick = gLines.updateGLine(src, brick, brick.tc1.center, brick.tc2.center)
                If brick.len3D > 0 Then brickCells.Add(brick)
            End If
        Next

        dst2 = src
        dst3.SetTo(0)

        For i = 0 To brickCells.Count - 1
            Dim tc As New tCell
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




Public Class MatchLine_EndPoints : Inherits TaskParent
    Public lpInput As lpData
    Dim match As New Match_Basics
    Public correlation As Single
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "Correlation measures how similar the previous template is to the current one."
        desc = "Concatenate the end point templates to return a single correlation to the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            dst2.SetTo(0)
            dst3.SetTo(0)
        End If

        If standalone Then lpInput = task.lineLongest

        Dim nabeRect1 = task.gridNabeRects(task.grid.gridMap.Get(Of Single)(lpInput.p1.Y, lpInput.p1.X))
        Dim nabeRect2 = task.gridNabeRects(task.grid.gridMap.Get(Of Single)(lpInput.p2.Y, lpInput.p2.X))
        cv.Cv2.HConcat(src(nabeRect1), src(nabeRect2), match.template)
        Static templateLast = match.template.Clone

        match.Run(templateLast)
        correlation = match.correlation

        If standaloneTest() Then
            Static rFit As New Rectangle_Fit
            rFit.Run(match.template)
            dst2 = rFit.dst2.Clone
            labels(2) = "Correlation = " + Format(correlation, fmt3)

            rFit.Run(templateLast)
            dst3 = rFit.dst2

            dst1 = src.Clone
            DrawRect(dst1, nabeRect1)
            DrawRect(dst1, nabeRect2)
        End If

        templateLast = match.template.Clone
    End Sub
End Class
