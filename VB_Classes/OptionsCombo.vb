Imports cv = OpenCvSharp
Public Class OptionsCombo
    Public Sub Setup(traceName As String, label As String, comboList As List(Of String))
        Me.MdiParent = allOptions
        Me.Text = traceName + " ComboBox Options"
        allOptions.addTitle(Me)
        ComboLabel.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
    End Sub
    Private Sub OptionsCombo_Click(sender As Object, e As EventArgs) Handles Me.Click
        Me.BringToFront()
    End Sub
End Class
