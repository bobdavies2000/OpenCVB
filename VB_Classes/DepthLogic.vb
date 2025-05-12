Imports cv = OpenCvSharp
Public Class DepthLogic_Basics : Inherits TaskParent
    Dim structured As New Structured_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Collect all the depth lines to make them accessible to all algorithms."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        structured.Run(src)
        dst2 = src.Clone
        dst1.SetTo(0)
        task.logicalLines.Clear()
        For Each lp In task.lpList
            lp.index = task.logicalLines.Count + 1
            task.logicalLines.Add(lp)
        Next

        For Each lp In structured.lpListX
            lp.index = task.logicalLines.Count + 1
            task.logicalLines.Add(lp)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next
        For Each lp In structured.lpListY
            lp.index = task.logicalLines.Count + 1
            task.logicalLines.Add(lp)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
        Next

        For Each lp In task.logicalLines
            dst1.Line(lp.p1, lp.p2, lp.index, task.lineWidth, cv.LineTypes.Link8)
        Next


        If standaloneTest() Then dst3 = ShowPalette(dst1)
        labels(2) = "Found " + CStr(task.logicalLines.Count) + " lines in the depth data."
    End Sub
End Class






Public Class DepthLogic_Bricks : Inherits TaskParent
    Dim structured As New Structured_Basics
    Dim gcUpdates As New List(Of Tuple(Of Integer, Single))
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Use the lp.bricks of bricks to build logical depth values for each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        structured.Run(src)
        labels = structured.labels

        Dim debugMode = task.featureOptions.SelectedFeature.Value > 0

        Dim avg1 As Single, avg2 As Single
        gcUpdates.Clear()
        For Each lp In structured.lpListX
            If lp.index = 0 Then Continue For
            Dim halfSum1 As New List(Of Single), halfsum2 As New List(Of Single)
            Dim halfCount As Integer = Math.Floor(If(lp.bricks.Count Mod 2 = 0, lp.bricks.Count, lp.bricks.Count - 1) / 2)
            Dim depthValues As New List(Of Single)
            For i = 0 To halfCount - 1
                Dim gc1 = task.brickList(lp.bricks(i))
                Dim gc2 = task.brickList(lp.bricks(lp.bricks.Count - i - 1))

                Dim d1 = gc1.depth
                Dim d2 = gc2.depth

                Dim p1 = gc1.rect.TopLeft
                Dim p2 = gc2.rect.TopLeft

                If gc1.correlation >= task.fCorrThreshold Then
                    halfSum1.Add(d1)
                    depthValues.Add(d1)
                    If debugMode Then SetTrueText(Format(d1, fmt3), New cv.Point(p1.X + 20, p1.Y), 3)
                End If

                If gc2.correlation >= task.fCorrThreshold Then
                    halfsum2.Add(d2)
                    depthValues.Add(d2)
                    If debugMode Then SetTrueText(Format(d2, fmt3), New cv.Point(p2.X - 20, p2.Y), 3)
                End If
            Next

            Dim incr = 255 / lp.bricks.Count
            Dim offset As Integer
            avg1 = If(halfSum1.Count > 0, halfSum1.Average, 0)
            avg2 = If(halfsum2.Count > 0, halfsum2.Average, 0)

            If avg1 < avg2 Then offset = lp.bricks.Count
            If Math.Abs(avg1 - avg2) < 0.01 Then ' task.depthDiffMeters Then
                For Each index In lp.bricks
                    Dim brick = task.brickList(index)
                    dst1(brick.rect).SetTo(1)
                    If debugMode Then dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
                    gcUpdates.Add(New Tuple(Of Integer, Single)(index, (avg1 + avg2) / 2))
                Next
            Else
                Dim min = If(depthValues.Count, depthValues.Min, 0)
                Dim max = If(depthValues.Count, depthValues.Max, 0)
                Dim depthIncr = (max - min) / lp.bricks.Count
                For i = 0 To lp.bricks.Count - 1
                    Dim index = lp.bricks(i)
                    Dim brick = task.brickList(index)
                    If offset > 0 Then
                        dst1(brick.rect).SetTo((offset - i + 1) * incr)
                        gcUpdates.Add(New Tuple(Of Integer, Single)(index, min + (offset - i) * depthIncr))
                    Else
                        dst1(brick.rect).SetTo(i * incr + 1)
                        gcUpdates.Add(New Tuple(Of Integer, Single)(index, min + i * depthIncr))
                    End If
                    If debugMode Then dst2.Rectangle(brick.rect, task.highlight, task.lineWidth)
                Next
            End If

            Exit For
        Next

        For Each tuple In gcUpdates
            task.brickList(tuple.Item1).depth = tuple.Item2
        Next

        dst1.SetTo(0)
        For Each brick In task.brickList
            dst1(brick.rect).SetTo(brick.depth * 255 / task.MaxZmeters)
        Next
        dst1.ConvertTo(dst0, cv.MatType.CV_8U)
        cv.Cv2.ApplyColorMap(dst0, dst2, task.depthColorMap)

        Dim lp1 = structured.lpListX(1)
        dst2.Line(lp1.p1, lp1.p2, task.highlight, task.lineWidth)
    End Sub
End Class






Public Class DepthLogic_Correlations : Inherits TaskParent
    Public Sub New()
        desc = "Reconstruct depth data using depth lines with high correlation (typically indicating featureless.)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        For Each lp In task.logicalLines

        Next
    End Sub
End Class
