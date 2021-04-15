
Imports cv = OpenCvSharp
Public Class Keyboard_Basics : Inherits VBparent
    Public keyInput As New List(Of String)
    Dim flow As Font_FlowText
    Public checkKeys As New OptionsKeyboardInput
    Public Sub New()
        checkKeys.Setup(caller)
        label1 = "Use the Options form to send in keystrokes"
        task.desc = "Test the keyboard interface available to all algorithms"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        keyInput = New List(Of String)(checkKeys.inputText)
        checkKeys.inputText.Clear()
        If standalone or task.intermediateReview = caller Then
            If flow Is Nothing Then flow = New Font_FlowText()
            Dim inputText As String = ""
            For i = 0 To keyInput.Count - 1
                inputText += keyInput(i).ToString()
            Next
            If inputText <> "" Then flow.msgs.Add(inputText)
            flow.Run(Nothing)
        End If
    End Sub
End Class


