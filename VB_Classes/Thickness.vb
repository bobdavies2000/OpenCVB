Imports cvb = OpenCvSharp
Public Class Thickness_Basics : Inherits TaskParent
    Public rc As New rcData
    Public volZ As New Volume_Basics
    Dim redC As New RedCloud_Core
    Public Sub New()
        desc = "Determine the thickness of a RedCloud cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            redC.Run(src)
            dst2 = redC.dst2
            rc = task.rc
        End If

        volZ.rc = rc
        volZ.Run(src)
        dst3 = volZ.dst3
        SetTrueText(volZ.strOut, 3)
    End Sub
End Class