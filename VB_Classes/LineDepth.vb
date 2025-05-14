Imports cv = OpenCvSharp
Public Class LineDepth_Basics : Inherits TaskParent
    Dim structured As New Structured_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Collect all the depth lines to make them accessible to all algorithms."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        structured.Run(src)
        dst2 = src.Clone

        Dim lines As New SortedList(Of Single, lpData)(New compareAllowIdenticalSingleInverted)
        For Each lp In structured.lpListX
            lines.Add(lp.length, lp)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
        For Each lp In structured.lpListY
            lines.Add(lp.length, lp)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next

        dst1.SetTo(0)
        Dim brickCount As Integer
        task.logicalLines.Clear()
        For Each lp In lines.Values
            If lp.bricks.Count < 3 Then Continue For ' must have 3 bricks in the list or the line is not big enough...
            lp.index = task.logicalLines.Count + 1
            task.logicalLines.Add(lp)
            For Each index In lp.bricks
                Dim brick = task.brickList(index)
                Dim val = dst0.Get(Of Byte)(brick.pt.Y, brick.pt.X)
                If val = 0 Then
                    dst1(brick.rect).SetTo(lp.index)
                    brickCount += 1
                End If
            Next
        Next

        dst3 = ShowPalette(dst1)
        If standaloneTest() Then
            For Each lp In task.logicalLines
                dst3.Line(lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.Link8)
            Next
        End If

        labels(2) = "Found " + CStr(task.logicalLines.Count) + " lines in the depth data."
        labels(3) = CStr(brickCount) + " bricks were updated with logical depth (" + Format(brickCount / task.gridRects.Count, "0%") + ")"
    End Sub
End Class