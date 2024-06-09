Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Controls_Basics : Inherits VB_Parent
    Public Sub New()
        desc = "Access form controls from C#"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("The Controls_Basics algorithm is to support FindSlider/FindCheckBox/FindRadio from C#" + vbCrLf +
                    "It has no output...")
    End Sub
    Public Sub CS_FindSlider(opt As String, val As Integer)
        FindSlider(opt).Value = val
    End Sub
End Class
