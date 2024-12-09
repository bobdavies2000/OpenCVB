Imports cvb = OpenCvSharp
' https://stackoverflow.com/questions/6606891/opencv-virtually-camera-rotating-translating-for-birds-eye-view/6667784#6667784
Public Class WarpPerspective_Basics : Inherits TaskParent
    Public options As New Options_Warp
    Public Sub New()
        desc = "Essentials of the rotation matrix of WarpPerspective"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Options.RunOpt()
        dst2 = src.EmptyClone
        cvb.Cv2.WarpPerspective(src, dst2, options.transformMatrix, dst2.Size(), cvb.InterpolationFlags.Cubic Or cvb.InterpolationFlags.WarpInverseMap)
        SetTrueText("Use sliders to understand impact of WarpPerspective", 3)
    End Sub
End Class







' http://opencvexamples.blogspot.com/
Public Class WarpPerspective_WidthHeight : Inherits TaskParent
    Dim options As New Options_WarpPerspective
    Public Sub New()
        desc = "Use WarpPerspective to transform input images."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim srcPt() = {New cvb.Point2f(0, 0), New cvb.Point2f(0, src.Height), New cvb.Point2f(src.Width, 0), New cvb.Point2f(src.Width, src.Height)}
        Dim pts() = {New cvb.Point2f(0, 0), New cvb.Point2f(0, src.Height), New cvb.Point2f(src.Width, 0),
                     New cvb.Point2f(options.width, options.height)}

        Dim perpectiveTranx = cvb.Cv2.GetPerspectiveTransform(srcPt, pts)
        cvb.Cv2.WarpPerspective(src, dst2, perpectiveTranx, New cvb.Size(src.Cols, src.Rows), cvb.InterpolationFlags.Cubic, cvb.BorderTypes.Constant, white)

        Dim center = New cvb.Point2f(src.Cols / 2, src.Rows / 2)
        Dim rotationMatrix = cvb.Cv2.GetRotationMatrix2D(center, options.angle, 1.0)
        cvb.Cv2.WarpAffine(dst2, dst3, rotationMatrix, src.Size(), cvb.InterpolationFlags.Nearest)
    End Sub
End Class