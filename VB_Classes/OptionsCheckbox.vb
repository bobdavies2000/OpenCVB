Imports System.Windows.Forms
Public Class OptionsCheckbox
    Public Box() As CheckBox
    Public Function Setup(caller As String, count As Integer) As Boolean
        If findfrm(caller + " CheckBox Options") IsNot Nothing Then Return False
        Me.MdiParent = aOptions
        ReDim Box(count - 1)
        Me.Text = caller + " CheckBox Options"
        aOptions.addTitle(Me)
        For i = 0 To Box.Count - 1
            Box(i) = New CheckBox
            Box(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(Box(i))
        Next
        Me.Show()
        Return True
    End Function
End Class
