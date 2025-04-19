Imports cv = OpenCvSharp
Public Class HighVis_Basics : Inherits TaskParent
    Dim info As New GridCell_Info
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Display all the grid cells that have good visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        For Each gc In task.gcList
            If gc.highlyVisible Then
                If gc.correlation > 0 And gc.corrHistory.Count = task.historyCount Then
                    dst1(gc.rect).SetTo((gc.correlation + 1) * 127)
                Else
                    dst1(gc.rect).SetTo(0)
                End If
            End If
        Next

        dst0 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        Dim mm = GetMinMax(dst1, dst0)
        dst2 = ShowPaletteDepth((dst1 - mm.minVal) * mm.maxVal / (mm.maxVal - mm.minVal))
        labels(2) = task.gCell.labels(2)

        info.Run(src)
        SetTrueText(info.strOut, 3)
    End Sub
End Class





Public Class HighVis_LineBasics : Inherits TaskParent
    Dim LRViews As New LeftRight_Lines
    Public leftLines As New List(Of lpData)
    Public Sub New()
        desc = "Find lines that are visible in both the left and right images"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        LRViews.Run(src)
        dst2 = LRViews.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        leftLines.Clear()
        For Each lp In LRViews.leftLines
            Dim gc = task.gcList(task.gcMap.Get(Of Single)(lp.center.Y, lp.center.X))
            If gc.depth = 0 Then Continue For
            If lp.highlyVisible Then
                leftLines.Add(lp)
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            End If
        Next
        If task.heartBeat Then
            labels(2) = CStr(LRViews.leftLines.Count) + " lines are present and " + CStr(leftLines.Count) +
                        " were marked highly visible in the left and right images."
        End If
    End Sub
End Class












