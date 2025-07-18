Imports System.Windows.Forms
Imports cv = OpenCvSharp

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





Public Class AddWeighted_Basics : Inherits TaskParent
    Public src2 As cv.Mat  ' user normally provides src2! 
    Public weight As Double
    Public Sub New()
        desc = "Add 2 images with specified weights."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then src2 = New cv.Mat(dst2.size, cv.MatType.CV_8UC3, 0)

        weight = 50
        cv.Cv2.AddWeighted(src, weight, src2, 1.0 - weight, 0, dst2)
        labels(2) = $"Depth %: {100 - weight * 100} BGR %: {CInt(weight * 100)}"
    End Sub
End Class
