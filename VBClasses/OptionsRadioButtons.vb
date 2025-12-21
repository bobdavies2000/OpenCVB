Imports VBClasses
Public Class OptionsRadioButtons
    Public check As New List(Of RadioButton)
    Public Function Setup(traceName As String) As Boolean
        If OptionParent.FindFrm(traceName + " Radio Buttons") IsNot Nothing Then Return False
        Me.MdiParent = taskAlg.allOptions
        Me.Text = traceName + " Radio Buttons"
        taskAlg.allOptions.addTitle(Me)
        Return True
    End Function
    Public Sub addRadio(labelStr As String)
        Dim index = check.Count
        check.Add(New RadioButton)
        check(index).Text = labelStr
        AddHandler check(index).CheckedChanged, AddressOf radio_CheckChanged
        check(index).AutoSize = True
        FlowLayoutPanel1.Controls.Add(check(index))
    End Sub
    Private Sub radio_CheckChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            ' Remove event handlers
            For Each radioButton In check
                If radioButton IsNot Nothing Then
                    RemoveHandler radioButton.CheckedChanged, AddressOf radio_CheckChanged
                End If
            Next

            ' Dispose all dynamically created controls
            For Each radioButton In check
                If radioButton IsNot Nothing Then
                    radioButton.Dispose()
                End If
            Next

            ' Clear the list
            check.Clear()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
