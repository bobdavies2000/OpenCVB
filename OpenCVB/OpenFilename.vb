Imports System.Windows.Forms
Public Class OpenFilename
    Public fileStarted As Boolean
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then filename.Text = OpenFileDialog1.FileName
    End Sub
    Private Sub PlayButton_Click(sender As Object, e As EventArgs) Handles PlayButton.Click
        If PlayButton.Text = "Start" Then
            PlayButton.Text = "Stop"
            fileStarted = True
        Else
            PlayButton.Text = "Start"
            fileStarted = False
        End If
    End Sub
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property
End Class
