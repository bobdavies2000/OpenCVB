
Imports cv = OpenCvSharp
Public Class Keyboard_Basics : Inherits VBparent
    Public keyInput As New List(Of String)
    Dim flow As New Font_FlowText
    Public checkKeys As New OptionsKeyboardInput
    Public Sub New()
        checkKeys.Setup(caller)
        labels(2) = "Use the Options form to send in keystrokes"
        task.desc = "Test the keyboard interface available to all algorithms"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        keyInput = New List(Of String)(checkKeys.inputText)
        checkKeys.inputText.Clear()
        If standalone or task.intermediateActive Then
            Dim inputText As String = ""
            For i = 0 To keyInput.Count - 1
                inputText += keyInput(i).ToString()
            Next
            If inputText <> "" Then flow.msgs.Add(inputText)
            flow.RunClass(Nothing)
        End If
    End Sub
End Class


