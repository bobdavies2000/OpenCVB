Imports cv = OpenCvSharp
Public Class LineTrack_Basics : Inherits TaskParent
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.redOptions.TrackingColor.Checked = True
        desc = "Track the line regions with RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)

        dst1.SetTo(0)
        For Each lp In task.lpList
            dst1.Line(lp.p1, lp.p2, 255, task.lineWidth + 1, cv.LineTypes.Link8)
        Next
        dst2 = runRedC(dst1, labels(2), Not dst1)

        dst3.SetTo(0)
        For Each lp In task.lpList
            DrawLine(dst3, lp.p1, lp.p2, white, task.lineWidth)
            DrawCircle(dst3, lp.center, task.DotSize, task.highlight, -1)
        Next
    End Sub
End Class