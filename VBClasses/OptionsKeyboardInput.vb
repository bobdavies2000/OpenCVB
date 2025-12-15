Imports VBClasses.VBClasses
Public Class OptionsKeyboardInput
    Public inputText As New List(Of String)
    Dim keyboardLastInput As String
    Public Sub Setup(traceName As String)
        Me.MdiParent = algTask.allOptions
        Me.Text = traceName + " Keyboard Options"
        algTask.allOptions.addTitle(Me)
    End Sub
    Private Sub TextBox1_KeyUp(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyUp
        inputText.Add(e.KeyCode.ToString)
    End Sub
    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        keyboardLastInput = e.KeyCode.ToString
    End Sub
    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        TextBox1.Text = ""
    End Sub
End Class
