Imports VBClasses
Public Class OptionsFileName
    Public fileStarted As Boolean
    Public newFileName As Boolean
    Public Sub PlayButton_Click(sender As Object, e As EventArgs) Handles PlayButton.Click
        If PlayButton.Text = "Start" Then
            PlayButton.Text = "Stop"
            fileStarted = True
        Else
            PlayButton.Text = "Start"
            fileStarted = False
        End If
        atask.optionsChanged = True
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then
            filename.Text = OpenFileDialog1.FileName
            atask.optionsChanged = True
        End If
        newFileName = True
    End Sub
    Public Sub Setup(traceName As String)
        Me.MdiParent = atask.allOptions
        Me.Text = traceName + " OpenFile Options"
        atask.allOptions.addTitle(Me)
    End Sub
    Public Sub setFileName(filespec As String)
        filename.Text = filespec
    End Sub
    Public Function getFileName() As String
        Return filename.Text
    End Function
End Class
