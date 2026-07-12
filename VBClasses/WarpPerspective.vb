Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
' https://stackoverflow.com/questions/6606891/opencv-virtually-camera-rotating-translating-for-birds-eye-view/6667784#6667784
Public Class WarpPerspective_Basics : Inherits TaskParent
    Public options As New Options_Warp
    Public Sub New()
        desc = "Essentials of the rotation matrix of WarpPerspective"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        dst2 = src.EmptyClone
        WarpPerspective(src, dst2, options.transformMatrix, dst2.Size(), cv.InterpolationFlags.Cubic Or cv.InterpolationFlags.WarpInverseMap)
        SetTrueText("Use sliders to understand impact of WarpPerspective", 3)
    End Sub
End Class







' http://opencvexamples.blogspot.com/
Public Class WarpPerspective_WidthHeight : Inherits TaskParent
    Dim options As New Options_WarpPerspective
    Public Sub New()
        desc = "Use WarpPerspective to transform input images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Dim srcPt() = {New cv.Point2f(0, 0), New cv.Point2f(0, src.Height), New cv.Point2f(src.Width, 0), New cv.Point2f(src.Width, src.Height)}
        Dim pts() = {New cv.Point2f(0, 0), New cv.Point2f(0, src.Height), New cv.Point2f(src.Width, 0),
                         New cv.Point2f(options.width, options.height)}

        Dim perpectiveTranx = GetPerspectiveTransform(srcPt, pts)
        WarpPerspective(src, dst2, perpectiveTranx, New cv.Size(src.Cols, src.Rows), cv.InterpolationFlags.Cubic, cv.BorderTypes.Constant, white)

        Dim center = New cv.Point2f(src.Cols / 2, src.Rows / 2)
        Dim rotationMatrix = GetRotationMatrix2D(center, options.angle, 1.0)
        WarpAffine(dst2, dst3, rotationMatrix, src.Size(), cv.InterpolationFlags.Nearest)
    End Sub
End Class
