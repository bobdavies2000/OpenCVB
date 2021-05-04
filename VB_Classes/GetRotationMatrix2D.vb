Imports cv = OpenCvSharp
Imports System.Windows.Forms
Public Class GetRotationMatrix2D_Options : Inherits VBparent
    Public warpFlag As cv.InterpolationFlags
    Public Sub New()

        If radio.Setup(caller, 7) Then
            radio.check(0).Text = "Area"
            radio.check(1).Text = "Cubic flag"
            radio.check(2).Text = "Lanczos4"
            radio.check(3).Text = "Linear"
            radio.check(4).Text = "Nearest"
            radio.check(5).Text = "WarpFillOutliers"
            radio.check(6).Text = "WarpInverseMap"
            radio.check(3).Checked = True
        End If

        task.desc = "Run to get the warpflag based on the current options"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static frm = findfrm(caller + " Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                warpFlag = Choose(i + 1, cv.InterpolationFlags.Area, cv.InterpolationFlags.Cubic, cv.InterpolationFlags.Lanczos4, cv.InterpolationFlags.Linear,
                                         cv.InterpolationFlags.Nearest, cv.InterpolationFlags.WarpFillOutliers, cv.InterpolationFlags.WarpInverseMap)
                Exit For
            End If
        Next
    End Sub
End Class








' https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
Public Class GetRotationMatrix2D_Basics : Inherits VBparent
    Public M As cv.Mat
    Public Mflip As cv.Mat
    Public rotateOptions As New GetRotationMatrix2D_Options
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "GetRotation Matrix2D Angle (deg)", 0, 360, 24)
        End If

        task.desc = "Rotate a rectangle of a specified angle"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        rotateOptions.Run(src)
        Dim angle = sliders.trackbar(0).Value
        M = cv.Cv2.GetRotationMatrix2D(New cv.Point2f(src.Width / 2, src.Height / 2), angle, 1)
        dst1 = src.WarpAffine(M, src.Size(), rotateOptions.warpFlag)
        If rotateOptions.warpFlag = cv.InterpolationFlags.WarpInverseMap Then Mflip = cv.Cv2.GetRotationMatrix2D(New cv.Point2f(src.Width / 2, src.Height / 2), -angle, 1)
    End Sub
End Class






Public Class GetRotationMatrix2D_Box : Inherits VBparent
    Dim rotation As New GetRotationMatrix2D_Basics
    Public Sub New()
        task.drawRect = New cv.Rect(100, 100, 100, 100)

        label1 = "Original Rectangle in the original perspective"
        label2 = "Same Rectangle in the new warped perspective"
        task.desc = "Track a rectangle no matter how the perspective is warped.  Draw a rectangle anywhere."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        rotation.Run(src)
        dst2 = dst1.Clone()

        Dim r = task.drawRect
        dst1 = src.Clone()
        dst1.Rectangle(r, cv.Scalar.White, 1)

        Dim center = New cv.Point2f(r.X + r.Width / 2, r.Y + r.Height / 2)
        Dim drawBox = New cv.RotatedRect(center, New cv.Size2f(r.Width, r.Height), 0)
        Dim boxPoints = cv.Cv2.BoxPoints(drawBox)
        Dim srcPoints = New cv.Mat(1, 4, cv.MatType.CV_32FC2, boxPoints)
        Dim dstpoints As New cv.Mat

        If rotation.rotateOptions.warpFlag <> cv.InterpolationFlags.WarpInverseMap Then
            cv.Cv2.Transform(srcPoints, dstpoints, rotation.M)
        Else
            cv.Cv2.Transform(srcPoints, dstpoints, rotation.Mflip)
        End If
        For i = 0 To dstpoints.Width - 1
            Dim p1 = dstpoints.Get(Of cv.Point2f)(0, i)
            Dim p2 = dstpoints.Get(Of cv.Point2f)(0, (i + 1) Mod 4)
            dst2.Line(p1, p2, cv.Scalar.White, task.lineThickness + 1, task.lineType)
        Next
    End Sub
End Class


