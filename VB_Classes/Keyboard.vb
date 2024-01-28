
Imports cv = OpenCvSharp
Public Class Keyboard_Basics : Inherits VB_Algorithm
    Public keyInput As New List(Of String)
    Dim flow As New Font_FlowText
    Public checkKeys As New OptionsKeyboardInput
    Public Sub New()
        checkKeys.Setup(traceName)
        labels(2) = "Use the Options form to send in keystrokes"
        desc = "Test the keyboard interface available to all algorithms"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        keyInput = New List(Of String)(checkKeys.inputText)
        checkKeys.inputText.Clear()
        If standaloneTest() Then
            Dim inputText As String = ""
            For i = 0 To keyInput.Count - 1
                inputText += keyInput(i).ToString()
            Next
            If inputText <> "" Then flow.msgs.Add(inputText)
            flow.Run(empty)
        End If
    End Sub
End Class


