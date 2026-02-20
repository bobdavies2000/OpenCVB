Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class HighVis_Basics : Inherits TaskParent
        Dim info As New Brick_Info
        Public Sub New()
            If task.bricks Is Nothing Then task.bricks = New Brick_Basics
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            desc = "Display all the bricks that have good visibility"
        End Sub
        Public Function ShowPaletteDepth(input As cv.Mat) As cv.Mat
            Dim output As New cv.Mat
            cv.Cv2.ApplyColorMap(input, output, task.depthColorMap)
            output.SetTo(0, task.noDepthMask)
            Return output
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1.SetTo(0)
            For Each gr In task.bricks.brickList
                dst1(gr.rect).SetTo((gr.correlation + 1) * 127)
            Next

            dst0 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
            Dim mm = GetMinMax(dst1, dst0)
            dst2 = ShowPaletteDepth((dst1 - mm.minVal) * mm.maxVal / (mm.maxVal - mm.minVal))
            labels(2) = task.bricks.labels(2)

            info.Run(src)
            SetTrueText(info.strOut, 3)
        End Sub
    End Class
End Namespace