Imports cv = OpenCvSharp
Public Class OptionsCombo
    Public Sub Setup(caller As String, label As String, comboList As List(Of String))
        Me.MdiParent = aOptions
        Me.Text = caller + " ComboBox Options"
        aOptions.addTitle(Me)
        ComboLabel.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
    End Sub
End Class
