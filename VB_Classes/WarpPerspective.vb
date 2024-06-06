Imports cv = OpenCvSharp
' https://stackoverflow.com/questions/6606891/opencv-virtually-camera-rotating-translating-for-birds-eye-view/6667784#6667784
Public Class WarpPerspective_Basics : Inherits VB_Parent
    Public options As New Options_Warp
    Public Sub New()
        desc = "Essentials of the rotation matrix of WarpPerspective"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        dst2 = src.EmptyClone
        cv.Cv2.WarpPerspective(src, dst2, options.transformMatrix, dst2.Size, cv.InterpolationFlags.Cubic Or cv.InterpolationFlags.WarpInverseMap)
        setTrueText("Use sliders to understand impact of WarpPerspective", 3)
    End Sub
End Class







' http://opencvexamples.blogspot.com/
Public Class WarpPerspective_WidthHeight : Inherits VB_Parent
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Warped Width", 0, dst2.Cols, dst2.Cols - 50)
            sliders.setupTrackBar("Warped Height", 0, dst2.Rows, dst2.Rows - 50)
            sliders.setupTrackBar("Warped Angle", 0, 360, 0)
        End If
        desc = "Use WarpPerspective to transform input images."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static wSlider = FindSlider("Warped Width")
        Static hSlider = FindSlider("Warped Height")
        Static angleSlider = FindSlider("Warped Angle")

        Dim srcPt() = {New cv.Point2f(0, 0), New cv.Point2f(0, src.Height), New cv.Point2f(src.Width, 0), New cv.Point2f(src.Width, src.Height)}
        Dim pts() = {New cv.Point2f(0, 0), New cv.Point2f(0, src.Height), New cv.Point2f(src.Width, 0),
                     New cv.Point2f(wSlider.Value, hSlider.Value)}

        Dim perpectiveTranx = cv.Cv2.GetPerspectiveTransform(srcPt, pts)
        cv.Cv2.WarpPerspective(src, dst2, perpectiveTranx, New cv.Size(src.Cols, src.Rows), cv.InterpolationFlags.Cubic, cv.BorderTypes.Constant, cv.Scalar.White)

        Dim center = New cv.Point2f(src.Cols / 2, src.Rows / 2)
        Dim rotationMatrix = cv.Cv2.GetRotationMatrix2D(center, angleSlider.Value, 1.0)
        cv.Cv2.WarpAffine(dst2, dst3, rotationMatrix, src.Size(), cv.InterpolationFlags.Nearest)
    End Sub
End Class