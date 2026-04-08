Imports cv = OpenCvSharp
Public Class HighVis_Basics : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim info As New NR_Brick_Info
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Display all the bricks that have good visibility"
    End Sub
    Public Function ShowPaletteDepth(input As cv.Mat) As cv.Mat
        Dim output As New cv.Mat
        cv.Cv2.ApplyColorMap(input, output, task.colorMapDepth)
        output.SetTo(0, task.noDepthMask)
        Return output
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        dst1.SetTo(0)
        For Each brick In bricks.brickList
            dst1(brick.rect).SetTo((brick.correlation + 1) * 127)
        Next

        dst0 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        Dim mm = GetMinMax(dst1, dst0)
        dst2 = ShowPaletteDepth((dst1 - mm.minVal) * mm.maxVal / (mm.maxVal - mm.minVal))
        labels(2) = bricks.labels(2)

        info.Run(src)
        SetTrueText(info.strOut, 3)
    End Sub
End Class
