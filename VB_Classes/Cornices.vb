Imports cv = OpenCvSharp
Public Class Cornice_Basics : Inherits VB_Algorithm
    Dim redC As New RedCloud_Core
    Dim sides As New Contour_SidePoints
    Public redCells As New List(Of rcData)
    Public Sub New()
        desc = "A cornice is basically a 3D corner.  Identify RedCloud cells as containing a cornice."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        For Each rc In task.redCells
            sides.rc = rc
            sides.Run(Nothing)
            rc.sideLeft = sides.ptLeft
            rc.sideRight = sides.ptRight
            rc.sideTop = sides.ptTop
            rc.sideBot = sides.ptBot
        Next
        showCell(dst0)
    End Sub
End Class

