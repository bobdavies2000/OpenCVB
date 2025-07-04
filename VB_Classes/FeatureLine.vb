﻿Imports cv = OpenCvSharp
Public Class FeatureLine_Basics : Inherits TaskParent
    Dim match As New Match_Basics
    Public correlation1 As Single
    Public correlation2 As Single
    Public doubleCheckLine As Boolean
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find and track the longest line by matching endpoint bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            labels(3) = "Currently available lines."
            dst3.SetTo(0)
            For Each lp In task.lineRGB.lpList
                dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            Next
        End If

        dst2 = src.Clone
        If task.lineRGB.lpList.Count > 1 Then
            task.lpD = task.lineRGB.lpList(0)
            dst2.Line(task.lpD.ep1, task.lpD.ep2, task.highlight, task.lineWidth + 1, task.lineType)
            dst2.Line(task.gravityVec.ep1, task.gravityVec.ep2, task.highlight, task.lineWidth + 1, task.lineType)

            If doubleCheckLine Then
                Dim index = task.lpD.bricks(0)
                match.template = src(task.gridRects(index))

                Dim searchRect = task.gridNabeRects(index)
                match.Run(src(searchRect))
                correlation1 = match.correlation

                searchRect = task.gridNabeRects(task.lpD.bricks.Last)
                match.Run(src(searchRect))
                correlation2 = match.correlation
                labels(2) = "Line end point correlations: " + Format(correlation1, fmt3) + " / " + Format(correlation2, fmt3)
            Else
                labels(2) = task.gravityHorizon.labels(2)
            End If
        End If
    End Sub
End Class










Public Class FeatureLine_VH : Inherits TaskParent
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
            If match.tCells(0).correlation >= task.fCorrThreshold And match.tCells(1).correlation >= task.fCorrThreshold Then
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