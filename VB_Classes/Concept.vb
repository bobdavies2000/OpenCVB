Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Concept_Parallel : Inherits TaskParent
    Dim lrLine As New Line_LeftRight
    Public Sub New()
        desc = "Use depth and lines to identify parallel lines in the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lrLine.Run(src)
        dst2 = lrLine.dst2
        dst3 = lrLine.dst3
    End Sub
End Class
