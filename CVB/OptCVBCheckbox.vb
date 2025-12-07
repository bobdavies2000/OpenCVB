Imports System.Windows.Forms
Public Class OptCVBCheckbox
    Public Box As New List(Of CheckBox)
    Public Function Setup(traceName As String) As Boolean
        If OptCVBParent.FindFrm(traceName + " CheckBoxes") IsNot Nothing Then Return False
        Me.MdiParent = myTask.allOptions
        Me.Text = traceName + " CheckBoxes"
        myTask.allOptions.addTitle(Me)
        Me.Show()
        Return True
    End Function
    Public Sub addCheckBox(labelStr As String)
        Dim index = Box.Count
        Box.Add(New CheckBox)
        AddHandler Box(index).CheckedChanged, AddressOf Box_CheckChanged
        Box(index).AutoSize = True
        Box(index).Text = labelStr
        FlowLayoutPanel1.Controls.Add(Box(index))
    End Sub
    Private Sub Box_CheckChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
End Class