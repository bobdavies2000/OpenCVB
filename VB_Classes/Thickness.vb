Imports cv = OpenCvSharp
Public Class Thickness_Basics : Inherits TaskParent
    Public rc As New rcData
    Public volZ As New Volume_Basics
    Public Sub New()
        desc = "Determine the thickness of a RedCloud cell"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If standaloneTest() Then
            task.redC.Run(src)
            dst2 = task.redC.dst2
            rc = task.rc
        End If

        volZ.rc = rc
        volZ.Run(src)
        dst3 = volZ.dst3
        SetTrueText(volZ.strOut, 3)
    End Sub
End Class