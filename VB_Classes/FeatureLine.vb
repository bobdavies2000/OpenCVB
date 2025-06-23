Imports cv = OpenCvSharp
Public Class FeatureLine_Basics : Inherits TaskParent
    Dim match As New Match_Basics
    Public gravityProxy As New lpData
    Dim firstRect As cv.Rect, lastRect As cv.Rect
    Dim matchRuns As Integer, lineRuns As Integer, totalLineRuns As Integer
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find and track the longest line by matching line bricks."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then task.lineRGB.lpList.Clear()

        If matchRuns > 500 Then
            Dim percent = lineRuns / matchRuns
            lineRuns = 10
            matchRuns = lineRuns / percent
        End If

        dst2 = src.Clone
        If task.lineRGB.lpList.Count > 0 Then
            matchRuns += 1
            gravityProxy = task.lineRGB.lpList(0)

            Dim matchInput As New cv.Mat
            cv.Cv2.HConcat(src(firstRect), src(lastRect), matchInput)

            match.Run(matchInput)

            labels(2) = "Line correlations (first/last): " + Format(match.correlation, fmt3) + " / " +
                        " with " + Format(lineRuns / matchRuns, "0%") + " requiring line detection.  " +
                        "line detection runs = " + CStr(totalLineRuns)
        End If

        If task.heartBeat Or task.lineRGB.lpList.Count = 0 Or match.correlation < 0.98 Then
            task.lineRGB.Run(src.Clone)
            If task.lineRGB.lpList.Count = 0 Then Exit Sub

            gravityProxy = task.lineRGB.lpList(0)
            lineRuns += 1
            totalLineRuns += 1

            firstRect = task.gridNabeRects(gravityProxy.bricks(0))
            lastRect = task.gridNabeRects(gravityProxy.bricks.Last)
            If firstRect.Width <> lastRect.Width Or firstRect.Height <> lastRect.Height Then
                Dim w = Math.Min(firstRect.Width, lastRect.Width)
                Dim h = Math.Min(firstRect.Height, lastRect.Height)
                firstRect = New cv.Rect(firstRect.X, firstRect.Y, w, h)
                lastRect = New cv.Rect(lastRect.X, lastRect.Y, w, h)
            End If

            Dim matchTemplate As New cv.Mat
            cv.Cv2.HConcat(src(firstRect), src(lastRect), matchTemplate)
            match.template = matchTemplate
        End If

        If standaloneTest() Then
            labels(3) = "Currently available lines."
            dst3.SetTo(0)
            For Each lp In task.lineRGB.lpList
                dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
            Next
        End If

        dst2.Rectangle(firstRect, task.highlight, task.lineWidth)
        dst2.Rectangle(lastRect, task.highlight, task.lineWidth)
        dst2.Line(gravityProxy.p1, gravityProxy.p2, task.highlight, task.lineWidth + 1, task.lineType)
        dst2.Line(task.gravityVec.ep1, task.gravityVec.ep2, task.highlight, task.lineWidth + 1, task.lineType)
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