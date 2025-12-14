Imports cv = OpenCvSharp
' https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
Public Class Rotate_Basics : Inherits TaskParent
    Public M As cv.Mat
    Public Mflip As cv.Mat
    Public options As New Options_Resize
    Public rotateAngle As Single = 1000
    Public rotateCenter As cv.Point
    Public optionsRotate As New Options_Rotate
    Public Sub New()
        rotateCenter = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)
        desc = "Rotate a rectangle by a specified angle"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        optionsRotate.Run()

        rotateAngle = optionsRotate.rotateAngle
        M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
        dst2 = src.WarpAffine(M, src.Size(), options.warpFlag)
        If options.warpFlag = cv.InterpolationFlags.WarpInverseMap Then
            Mflip = cv.Cv2.GetRotationMatrix2D(rotateCenter, rotateAngle, 1)
        End If
        labels(2) = "Image after rotation by " + Format(rotateAngle, fmt3)
    End Sub
End Class







Public Class Rotate_Box : Inherits TaskParent
    Dim rotation As New Rotate_Basics
    Public Sub New()
        algTask.drawRect = New cv.Rect(100, 100, 100, 100)
        labels(2) = "Original Rectangle in the original perspective"
        labels(3) = "Same Rectangle in the new warped perspective"
        desc = "Track a rectangle no matter how the perspective is warped.  Draw a rectangle anywhere."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        rotation.Run(src)
        dst3 = dst2.Clone()

        Dim r = algTask.drawRect
        dst2 = src.Clone()
        dst2.Rectangle(r, white, 1)

        Dim center = New cv.Point2f(r.X + r.Width / 2, r.Y + r.Height / 2)
        Dim drawBox = New cv.RotatedRect(center, New cv.Size2f(r.Width, r.Height), 0)
        Dim boxPoints = cv.Cv2.BoxPoints(drawBox)
        Dim srcPoints = cv.Mat.FromPixelData(1, 4, cv.MatType.CV_32FC2, boxPoints)
        Dim dstpoints As New cv.Mat

        If rotation.options.warpFlag <> cv.InterpolationFlags.WarpInverseMap Then
            cv.Cv2.Transform(srcPoints, dstpoints, rotation.M)
        Else
            cv.Cv2.Transform(srcPoints, dstpoints, rotation.Mflip)
        End If
        For i = 0 To dstpoints.Width - 1
            Dim p1 = dstpoints.Get(Of cv.Point2f)(0, i)
            Dim p2 = dstpoints.Get(Of cv.Point2f)(0, (i + 1) Mod 4)
            dst3.Line(p1, p2, white, algTask.lineWidth + 1, algTask.lineType)
        Next
    End Sub
End Class








Public Class Rotate_Example : Inherits TaskParent
    Dim rotate As New Rotate_Basics
    Public Sub New()
        rotate.rotateCenter = New cv.Point(dst2.Height / 2, dst2.Height / 2)
        rotate.rotateAngle = -90
        desc = "Reminder on how to rotate an image and keep all the pixels."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim r = New cv.Rect(0, 0, src.Height, src.Height)
        dst2(r) = src.Resize(New cv.Size(src.Height, src.Height))
        rotate.Run(dst2)
        dst3(r) = rotate.dst2(New cv.Rect(0, 0, src.Height, src.Height))
    End Sub
End Class








' https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
Public Class Rotate_BasicsQT : Inherits TaskParent
    Public rotateAngle As Double
    Public rotateCenter As cv.Point2f
    Public Sub New()
        rotateCenter = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)
        desc = "Rotate a rectangle by a specified angle"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
        dst2 = src.WarpAffine(M, src.Size(), cv.InterpolationFlags.Nearest)
    End Sub
End Class





Public Class Rotate_Verticalize : Inherits TaskParent
    Dim rotate As New Rotate_Basics
    Public angleSlider As New System.Windows.Forms.TrackBar
    Public Sub New()
        angleSlider = OptionParent.FindSlider("Rotation Angle in degrees X100")
        angleSlider.Value = algTask.verticalizeAngle / 100
        OptionParent.findRadio("Nearest (preserves pixel values best)").Checked = True
        desc = "Use gravity vector to rotate the image to be vertical"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then angleSlider.Value = algTask.verticalizeAngle * 100
        rotate.Run(src)
        dst2 = rotate.dst2
        SetTrueText("Angle offset from gravity = " + Format(angleSlider.Value / 100, fmt2))
    End Sub
End Class
