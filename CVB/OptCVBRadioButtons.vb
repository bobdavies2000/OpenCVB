Imports System.Windows.Forms
Public Class OptCVBRadioButtons
    Public check As New List(Of RadioButton)
    Public Function Setup(traceName As String) As Boolean
        If OptCVBParent.FindFrm(traceName + " Radio Buttons") IsNot Nothing Then Return False
        Me.MdiParent = myTask.allOptions
        Me.Text = traceName + " Radio Buttons"
        myTask.allOptions.addTitle(Me)
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
        myTask.optionsChanged = True
    End Sub
End Class
