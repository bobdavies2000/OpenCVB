Imports cv = OpenCvSharp
Public Class LogicalDepth_Basics : Inherits TaskParent
    Dim structured As New Structured_Basics
    Dim gcUpdates As New List(Of Tuple(Of Integer, Single))
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Use the lp.cellList of grid cells to build logical depth values for each cell."
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
            Dim halfCount As Integer = Math.Floor(If(lp.cellList.Count Mod 2 = 0, lp.cellList.Count, lp.cellList.Count - 1) / 2)
            Dim depthValues As New List(Of Single)
            For i = 0 To halfCount - 1
                Dim gc1 = task.gcList(lp.cellList(i))
                Dim gc2 = task.gcList(lp.cellList(lp.cellList.Count - i - 1))

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

            Dim incr = 255 / lp.cellList.Count
            Dim offset As Integer
            avg1 = If(halfSum1.Count > 0, halfSum1.Average, 0)
            avg2 = If(halfsum2.Count > 0, halfsum2.Average, 0)

            If avg1 < avg2 Then offset = lp.cellList.Count
            If Math.Abs(avg1 - avg2) < 0.01 Then ' task.depthDiffMeters Then
                For Each index In lp.cellList
                    Dim gc = task.gcList(index)
                    dst1(gc.rect).SetTo(1)
                    If debugMode Then dst2.Rectangle(gc.rect, task.highlight, task.lineWidth)
                    gcUpdates.Add(New Tuple(Of Integer, Single)(index, (avg1 + avg2) / 2))
                Next
            Else
                Dim min = If(depthValues.Count, depthValues.Min, 0)
                Dim max = If(depthValues.Count, depthValues.Max, 0)
                Dim depthIncr = (max - min) / lp.cellList.Count
                For i = 0 To lp.cellList.Count - 1
                    Dim index = lp.cellList(i)
                    Dim gc = task.gcList(index)
                    If offset > 0 Then
                        dst1(gc.rect).SetTo((offset - i + 1) * incr)
                        gcUpdates.Add(New Tuple(Of Integer, Single)(index, min + (offset - i) * depthIncr))
                    Else
                        dst1(gc.rect).SetTo(i * incr + 1)
                        gcUpdates.Add(New Tuple(Of Integer, Single)(index, min + i * depthIncr))
                    End If
                    If debugMode Then dst2.Rectangle(gc.rect, task.highlight, task.lineWidth)
                Next
            End If

            Exit For
        Next

        For Each tuple In gcUpdates
            task.gcList(tuple.Item1).depth = tuple.Item2
        Next


        dst1.SetTo(0)
        For Each gc In task.gcList
            dst1(gc.rect).SetTo(gc.depth * 255 / task.MaxZmeters)
        Next
        dst1.ConvertTo(dst0, cv.MatType.CV_8U)
        cv.Cv2.ApplyColorMap(dst0, dst2, task.depthColorMap)

        Dim lp1 = structured.lpListX(1)
        dst2.Line(lp1.p1, lp1.p2, task.highlight, task.lineWidth)
    End Sub
End Class

