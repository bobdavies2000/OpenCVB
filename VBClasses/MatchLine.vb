Imports cv = OpenCvSharp
Public Class MatchLine_Basics : Inherits TaskParent
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







Public Class MatchLine_VH : Inherits TaskParent
    Public brickCells As New List(Of gravityLine)
    Dim match As New Match_tCell
    Dim gLines As New XO_Line_GCloud
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

        Dim nabeRect1 = task.gridNabeRects(task.gridMap.Get(Of Integer)(lpInput.p1.Y, lpInput.p1.X))
        Dim nabeRect2 = task.gridNabeRects(task.gridMap.Get(Of Integer)(lpInput.p2.Y, lpInput.p2.X))
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
