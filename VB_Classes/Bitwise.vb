Imports cv = OpenCvSharp
Public Class Bitwise_Not
    Inherits VBparent
    Public Sub New()
        initParent()
        label1 = "Color BitwiseNot"
        label2 = "Gray BitwiseNot"
        task.desc = "Gray and color bitwise_not"
		' task.rank = 1
    End Sub
    Public Sub Run(ByVal src As cv.Mat)
        cv.Cv2.BitwiseNot(src, dst1)
        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.BitwiseNot(src, dst2)
    End Sub
End Class


