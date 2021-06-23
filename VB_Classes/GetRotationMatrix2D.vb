Imports cv = OpenCvSharp
Imports System.Windows.Forms

' https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
Public Class GetRotationMatrix2D_Basics : Inherits VBparent
    Public M As cv.Mat
    Public Mflip As cv.Mat
    Public rotateOptions As New Resize_Options
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
        dst2 = src.WarpAffine(M, src.Size(), rotateOptions.warpFlag)
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
        dst3 = dst2.Clone()

        Dim r = task.drawRect
        dst2 = src.Clone()
        dst2.Rectangle(r, cv.Scalar.White, 1)

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
            dst3.Line(p1, p2, cv.Scalar.White, task.lineWidth + 1, task.lineType)
        Next
    End Sub
End Class


