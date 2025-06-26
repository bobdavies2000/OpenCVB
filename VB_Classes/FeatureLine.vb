Imports cv = OpenCvSharp
Public Class FeatureLine_Basics : Inherits TaskParent
    Public cameraMotionProxy As New lpData
    Dim matchRuns As Integer, lineRuns As Integer, totalLineRuns As Integer
    Public runOnEachFrame As Boolean
    Dim match As New Match_Line
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
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
            cameraMotionProxy = task.lineRGB.lpList(0)
            matchRuns += 1
            match.lpInput = cameraMotionProxy
            match.Run(src)
            dst1 = match.dst2

            labels(2) = "Line end point correlation:  " + Format(match.correlation, fmt3) + " / " +
                        " with " + Format(lineRuns / matchRuns, "0%") + " requiring line detection.  " +
                        "line detection runs = " + CStr(totalLineRuns)
        End If

        If task.heartBeatLT Or task.lineRGB.lpList.Count = 0 Or match.correlation < 0.98 Or runOnEachFrame Then
            task.motionMask.SetTo(255) ' force a complete line detection
            task.lineRGB.Run(src.Clone)
            If task.lineRGB.lpList.Count = 0 Then Exit Sub

            match.lpInput = task.lineRGB.lpList(0)
            lineRuns += 1
            totalLineRuns += 1
            match.Run(src)
        End If

        labels(3) = "Currently available lines."
        dst3 = task.lineRGB.dst3
        labels(3) = task.lineRGB.labels(3)

        dst2.Rectangle(match.lpInput.matchRect1, task.highlight, task.lineWidth)
        dst2.Rectangle(match.lpInput.matchRect2, task.highlight, task.lineWidth)
        dst2.Line(cameraMotionProxy.p1, cameraMotionProxy.p2, task.highlight, task.lineWidth, task.lineType)
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