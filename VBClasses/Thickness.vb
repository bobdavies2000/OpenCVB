Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Thickness_Basics : Inherits TaskParent
        Public rc As New oldrcData
        Public volZ As New Volume_Basics
        Public Sub New()
            desc = "Determine the thickness of a RedCloud cell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                dst2 = runRedList(src, labels(2))
                rc = task.oldrcD
            End If

            volZ.rc = rc
            volZ.Run(src)
            dst3 = volZ.dst3
            SetTrueText(volZ.strOut, 3)
        End Sub
    End Class
End Namespace