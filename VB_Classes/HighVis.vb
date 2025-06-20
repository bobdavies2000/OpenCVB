Imports cv = OpenCvSharp
Public Class HighVis_Basics : Inherits TaskParent
    Dim info As New Brick_Info
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Display all the bricks that have good visibility"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1.SetTo(0)
        For Each brick In task.bricks.brickList
            If brick.correlation > task.fCorrThreshold Then
                If brick.correlation > 0 Then
                    dst1(brick.rect).SetTo((brick.correlation + 1) * 127)
                Else
                    dst1(brick.rect).SetTo(0)
                End If
            End If
        Next

        dst0 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        Dim mm = GetMinMax(dst1, dst0)
        dst2 = ShowPaletteDepth((dst1 - mm.minVal) * mm.maxVal / (mm.maxVal - mm.minVal))
        labels(2) = task.bricks.labels(2)

        info.Run(src)
        SetTrueText(info.strOut, 3)
    End Sub
End Class











