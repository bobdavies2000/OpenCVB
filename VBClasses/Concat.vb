Imports cv = OpenCvSharp
Imports VBClasses
Public Class XR_Concat_Basics : Inherits TaskParent
    Public Sub New()
        labels(2) = "Horizontal concatenation"
        labels(3) = "Vertical concatenation"
        desc = "Concatenate 2 images - horizontally and vertically"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim tmp As New cv.Mat
        cv.Cv2.HConcat(src, task.depthRGB, tmp)
        cv.Cv2.Resize(tmp, dst2, src.Size())
        cv.Cv2.VConcat(src, task.depthRGB, tmp)
        cv.Cv2.Resize(tmp, dst3, src.Size())
    End Sub
End Class




Public Class XR_Concat_4way : Inherits TaskParent
    Public img(3) As cv.Mat
    Public Sub New()
        For i = 0 To img.Length - 1
            img(i) = New cv.Mat
        Next
        labels(2) = "Color/RGBDepth/Left/Right views"
        desc = "Concatenate 4 images - horizontally and vertically"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            img(0) = src
            img(1) = task.depthRGB
            Dim _cvtLeft As New cv.Mat
            cv.Cv2.CvtColor(task.leftView, _cvtLeft, cv.ColorConversionCodes.GRAY2BGR)
            img(2) = If(task.leftView.Channels() = 1, _cvtLeft, task.leftView)
            Dim _cvtRight As New cv.Mat
            cv.Cv2.CvtColor(task.rightView, _cvtRight, cv.ColorConversionCodes.GRAY2BGR)
            img(3) = If(task.rightView.Channels() = 1, _cvtRight, task.rightView)
        End If
        Dim tmp1 As New cv.Mat, tmp2 As New cv.Mat, tmp3 As New cv.Mat
        cv.Cv2.HConcat(img(0), img(1), tmp1)
        cv.Cv2.HConcat(img(2), img(3), tmp2)
        cv.Cv2.VConcat(tmp1, tmp2, tmp3)
        cv.Cv2.Resize(tmp3, dst2, src.Size())
    End Sub
End Class
