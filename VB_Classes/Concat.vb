Imports cv = OpenCvSharp
Public Class Concat_Basics : Inherits VBparent
    Public Sub New()
        labels(2) = "Horizontal concatenation"
        labels(3) = "Vertical concatenation"
        task.desc = "Concatenate 2 images - horizontally and vertically"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim tmp As New cv.Mat
        cv.Cv2.HConcat(src, task.RGBDepth, tmp)
        dst2 = tmp.Resize(src.Size())
        cv.Cv2.VConcat(src, task.RGBDepth, tmp)
        dst3 = tmp.Resize(src.Size())
    End Sub
End Class




Public Class Concat_4way : Inherits VBparent
    Public img(3) As cv.Mat
    Public Sub New()
        For i = 0 To img.Length - 1
            img(i) = New cv.Mat
        Next
        labels(2) = "Color/RGBDepth/Left/Right views"
        task.desc = "Concatenate 4 images - horizontally and vertically"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone or task.intermediateActive Then
            img(0) = src
            img(1) = task.RGBDepth
            img(2) = task.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(src.Size())
            img(3) = task.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(src.Size())
        End If
        Dim tmp1 As New cv.Mat, tmp2 As New cv.Mat, tmp3 As New cv.Mat
        cv.Cv2.HConcat(img(0), img(1), tmp1)
        cv.Cv2.HConcat(img(2), img(3), tmp2)
        cv.Cv2.VConcat(tmp1, tmp2, tmp3)
        dst2 = tmp3.Resize(src.Size())
    End Sub
End Class


