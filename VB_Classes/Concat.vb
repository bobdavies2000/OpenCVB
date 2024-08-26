Imports cvb = OpenCvSharp
Public Class Concat_Basics : Inherits VB_Parent
    Public Sub New()
        labels(2) = "Horizontal concatenation"
        labels(3) = "Vertical concatenation"
        desc = "Concatenate 2 images - horizontally and vertically"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim tmp As New cvb.Mat
        cvb.Cv2.HConcat(src, task.depthRGB, tmp)
        dst2 = tmp.Resize(src.Size())
        cvb.Cv2.VConcat(src, task.depthRGB, tmp)
        dst3 = tmp.Resize(src.Size())
    End Sub
End Class




Public Class Concat_4way : Inherits VB_Parent
    Public img(3) As cvb.Mat
    Public Sub New()
        For i = 0 To img.Length - 1
            img(i) = New cvb.Mat
        Next
        labels(2) = "Color/RGBDepth/Left/Right views"
        desc = "Concatenate 4 images - horizontally and vertically"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            img(0) = src
            img(1) = task.depthRGB
            img(2) = If(task.leftView.Channels() = 1, task.leftView.CvtColor(cvb.ColorConversionCodes.GRAY2BGR), task.leftView)
            img(3) = If(task.rightView.Channels() = 1, task.rightView.CvtColor(cvb.ColorConversionCodes.GRAY2BGR), task.rightView)
        End If
        Dim tmp1 As New cvb.Mat, tmp2 As New cvb.Mat, tmp3 As New cvb.Mat
        cvb.Cv2.HConcat(img(0), img(1), tmp1)
        cvb.Cv2.HConcat(img(2), img(3), tmp2)
        cvb.Cv2.VConcat(tmp1, tmp2, tmp3)
        dst2 = tmp3.Resize(src.Size())
    End Sub
End Class


