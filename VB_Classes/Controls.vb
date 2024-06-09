Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Windows.Forms
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
    Public Function CS_GetSlider(opt As String) As TrackBar
        Return FindSlider(opt)
    End Function
    Public Sub CS_FindCheckBox(opt As String, val As Boolean)
        findCheckBox(opt).Checked = val
    End Sub
End Class
