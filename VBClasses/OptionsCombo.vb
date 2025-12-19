Imports VBClasses
Public Class OptionsCombo
    Public Sub Setup(traceName As String, label As String, comboList As List(Of String))
        Me.MdiParent = taskAlg.allOptions
        Me.Text = traceName + " ComboBox Options"
        taskAlg.allOptions.addTitle(Me)
        ComboLabel.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
    End Sub
End Class
