Imports VBClasses
Public Class OptionsCheckbox
    Public Box As New List(Of CheckBox)
    Public Function Setup(traceName As String) As Boolean
        If OptionParent.FindFrm(traceName + " CheckBoxes") IsNot Nothing Then Return False
        Me.MdiParent = taskAlg.allOptions
        Me.Text = traceName + " CheckBoxes"
        taskAlg.allOptions.addTitle(Me)
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
        taskAlg.optionsChanged = True
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            ' Remove event handlers
            For Each checkbox In Box
                If checkbox IsNot Nothing Then
                    RemoveHandler checkbox.CheckedChanged, AddressOf Box_CheckChanged
                End If
            Next

            ' Dispose all dynamically created controls
            For Each checkbox In Box
                If checkbox IsNot Nothing Then
                    checkbox.Dispose()
                End If
            Next

            ' Clear the list
            Box.Clear()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
