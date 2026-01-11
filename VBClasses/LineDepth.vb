Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class LineDepth_Basics : Inherits TaskParent
        Dim edges As New Edge_Basics
        Public Sub New()
            desc = "Find all the edges that are not lines"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)
            dst2 = edges.dst2
            dst3 = task.lines.dst2
        End Sub
    End Class
End Namespace

