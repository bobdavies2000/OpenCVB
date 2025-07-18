Imports System.Windows.Forms

Public Class MyLibraryClass

    Public Function GetCheckBoxExample() As CheckBox
        Dim myCheckBox As New CheckBox()
        myCheckBox.Text = "Check Me"
        myCheckBox.Checked = True
        Return myCheckBox
    End Function

    ' You can even use MessageBox.Show from here now
    Public Sub ShowLibraryMessage()
        MessageBox.Show("This message box is created from the library!", "Library Function")
    End Sub

End Class