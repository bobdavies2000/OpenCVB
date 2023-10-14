Imports cv = OpenCvSharp
Public Class Thickness_Basics : Inherits VB_Algorithm
    Public rc As New rcData
    Public volZ As New Volume_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Determine the thickness of a RedCloud cell"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standalone Then
            redC.Run(src)
            dst2 = redC.dst2
            rc = task.rcSelect
        End If

        volZ.rc = rc
        volZ.Run(src)
        dst3 = volZ.dst3
        setTrueText(volZ.strOut, 3)
    End Sub
End Class