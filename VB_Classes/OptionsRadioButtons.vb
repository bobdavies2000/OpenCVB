Imports System.Windows.Forms
Public Class OptionsRadioButtons
    Public check() As RadioButton
    Public Function Setup(caller As String, count As Integer) As Boolean
        If findfrm(caller + " Radio Options") IsNot Nothing Then Return False
        Me.MdiParent = allOptions
        ReDim check(count - 1)
        Me.Text = caller + " Radio Options"
        allOptions.addTitle(Me)
        For i = 0 To check.Count - 1
            check(i) = New RadioButton
            check(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(check(i))
        Next
        Return True
    End Function
End Class
