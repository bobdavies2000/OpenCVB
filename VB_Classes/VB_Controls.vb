Imports cv = OpenCvSharp
Imports System.Windows.Forms
Public Class VB_Controls_CSharp : Inherits VB_Parent
    Public Sub New()
        desc = "Access form controls from C#"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        SetTrueText("The Controls_Basics algorithm is to support FindSlider/FindCheckBox/FindRadio from C#" + vbCrLf +
                    "It has no output...")
    End Sub
    Public Function buildCallStack(traceName As String, callStack As String) As Boolean
        standalone = callTrace(0) = traceName + "\" ' only the first is standalone (the primary algorithm.)
        If standalone Then
            algorithm_ms.Clear()
            algorithmNames.Clear()
        End If
        If standalone = False And callTrace.Contains(callStack) = False Then callTrace.Add(callStack)
        Return standalone
    End Function
    Public Sub RunFromVB(src As cv.Mat, csCode As Object)
        If task.testAllRunning = False Then measureStartRun(csCode.traceName)

        trueData.Clear()
        If task.paused = False Then csCode.RunCS(src)
        If task.testAllRunning = False Then measureEndRun(csCode.traceName)
    End Sub
    Public Sub CS_SetSlider(opt As String, val As Integer)
        FindSlider(opt).Value = val
    End Sub
    Public Function CS_GetSlider(opt As String) As TrackBar
        Return FindSlider(opt)
    End Function
    Public Function CS_FindCheckBox(opt As String) As CheckBox
        Return FindCheckBox(opt)
    End Function
    Public Function CS_FindRadio(opt As String) As RadioButton
        Return FindRadio(opt)
    End Function
End Class
