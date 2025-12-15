Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Keyboard_Basics : Inherits TaskParent
        Public keyInput As New List(Of String)
        Dim flow As New Font_FlowText
        Public checkKeys As New OptionsKeyboardInput
        Public Sub New()
            flow.parentData = Me
            checkKeys.Setup(traceName)
            labels(2) = "Use the Options form to send in keystrokes"
            desc = "Test the keyboard interface available to all algorithms"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() And checkKeys.inputText.Count > 0 Then
                For Each txt In checkKeys.inputText
                    flow.nextMsg += txt.ToString()
                Next
                flow.Run(src)
            End If
            checkKeys.inputText.Clear()
        End Sub
    End Class
End Namespace