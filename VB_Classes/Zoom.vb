Imports cv = OpenCvSharp
Public Class Zoom_Basics : Inherits VB_Algorithm
    Public Sub New()
        task.drawRect = New cv.Rect(10, 10, 50, 50)
        desc = "Magnify the drawn rectangle on dst2 and put it in dst3."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src
        dst3 = dst2(task.drawRect).Resize(dst3.Size)
    End Sub
End Class
