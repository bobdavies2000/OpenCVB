Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedColor_Basics : Inherits VB_Algorithm
    Public colorClass As New Color_Basics
    Public Sub New()
        desc = "Combined the color and cloud as indicated in the RedOptions panel."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        colorClass.Run(src)
        dst2 = colorClass.dst2
        dst3 = colorClass.dst3
    End Sub
End Class