Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class NotLine_Basics : Inherits TaskParent
        Dim edges As New edge_Basics
        Public Sub New()
            desc = "Find all the edges that are not lines"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst2 = edges.dst2
            dst3 = task.lines.dst2
            For Each lp In task.lines.lpList
                dst2.Line(lp.p1, lp.p2, black, task.lineWidth, task.lineType)
            Next
        End Sub
    End Class
End Namespace

