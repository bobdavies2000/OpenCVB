Imports  cv = OpenCvSharp
Public Class NoDepth_Basics : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        findSlider("Reduction factor").Value = 32
        labels = {"", "", "NoDepth Mask", "Reduction of RGB where depth is missing - in CV_32FC3 format"}
        task.desc = "Use reduction to create RedCloud cells in areas with no depth"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2.SetTo(0)
        src.CopyTo(dst2, task.noDepthMask)

        reduction.Run(dst2)
        reduction.dst2.ConvertTo(dst3, cv.MatType.CV_32FC1)
        dst3 *= 0.01
        Dim split() As cv.Mat = {dst3, dst3, New cv.Mat(dst3.Size, cv.MatType.CV_32FC1, 0)}
        cv.Cv2.Merge(split, dst3)
    End Sub
End Class
