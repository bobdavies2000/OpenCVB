Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

' https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
Public Class Rotate_Basics : Inherits VB_Algorithm
    Public M As cv.Mat
    Public Mflip As cv.Mat
    Public options As New Options_Resize
    Public rotateAngle As Single = 1000
    Public rotateCenter As cv.Point
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Rotation Angle in degrees", -180, 180, 24)
        desc = "Rotate a rectangle by a specified angle"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static angleSlider = findSlider("Rotation Angle in degrees")
        If rotateAngle = 1000 Then
            rotateCenter = New cv.Point2f(src.Width / 2, src.Height / 2)
            rotateAngle = angleSlider.Value
        End If
        options.RunVB()

        M = cv.Cv2.GetRotationMatrix2D(rotateCenter, rotateAngle, 1)
        dst2 = src.WarpAffine(M, src.Size(), options.warpFlag)
        If options.warpFlag = cv.InterpolationFlags.WarpInverseMap Then
            Mflip = cv.Cv2.GetRotationMatrix2D(New cv.Point2f(src.Width / 2, src.Height / 2), -rotateAngle, 1)
        End If
    End Sub
End Class







' https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
Public Class Rotate_BasicsQT : Inherits VB_Algorithm
    Public rotateAngle As Single = 1000
    Public rotateCenter As cv.Point2f
    Public Sub New()
        desc = "Rotate a rectangle by a specified angle"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If standaloneTest() Then
            setTrueText(traceName + " has no output when run standaloneTest()")
            Exit Sub
        End If

        Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, rotateAngle * 57.2958, 1)
        dst2 = src.WarpAffine(M, src.Size(), cv.InterpolationFlags.Linear)
    End Sub
End Class






Public Class Rotate_Box : Inherits VB_Algorithm
    ReadOnly rotation As New Rotate_Basics
    Public Sub New()
        task.drawRect = New cv.Rect(100, 100, 100, 100)
        labels(2) = "Original Rectangle in the original perspective"
        labels(3) = "Same Rectangle in the new warped perspective"
        desc = "Track a rectangle no matter how the perspective is warped.  Draw a rectangle anywhere."
    End Sub
    Public Sub RunVB(src as cv.Mat)
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

        If rotation.options.warpFlag <> cv.InterpolationFlags.WarpInverseMap Then
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







' https://academo.org/demos/rotation-about-point/
Public Class Rotate_Poly : Inherits VB_Algorithm
    Dim optionsFPoly As New Options_FPoly
    Public options As New Options_RotatePoly
    Public rotateQT As New Rotate_PolyQT
    Public Sub New()
        labels = {"", "", "Triangle before rotation", "Triangle after rotation"}
        desc = "Rotate a triangle around a center of rotation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        optionsFPoly.RunVB()

        Static rPoly As New List(Of cv.Point2f)
        If options.changeCheck.Checked Or firstPass Then
            rPoly.Clear()
            For i = 0 To task.polyCount - 1
                rPoly.Add(New cv.Point2f(msRNG.Next(dst2.Width / 4, dst2.Width * 3 / 4), msRNG.Next(dst2.Height / 4, dst2.Height * 3 / 4)))
            Next
            rotateQT.rotateCenter = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            options.changeCheck.Checked = False
        End If

        rotateQT.poly = New List(Of cv.Point2f)(rPoly)
        rotateQT.rotateAngle = options.angleSlider.Value
        rotateQT.Run(src)
        dst2 = rotateQT.dst3

        dst2.Circle(rotateQT.rotateCenter, task.dotSize + 2, cv.Scalar.Yellow, -1, task.lineType)
        setTrueText("center of rotation", rotateQT.rotateCenter)
        labels(3) = rotateQT.labels(3)
    End Sub
End Class








' https://academo.org/demos/rotation-about-point/
Public Class Rotate_PolyQT : Inherits VB_Algorithm
    Public poly As New List(Of cv.Point2f)
    Public rotateCenter As cv.Point2f
    Public rotateAngle As Single
    Public Sub New()
        labels = {"", "", "Polygon before rotation", ""}
        desc = "Rotate a triangle around a center of rotation"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.heartBeat Then
            dst2.SetTo(0)
            dst3.SetTo(0)
        End If

        If standaloneTest() Then
            setTrueText(traceName + " has no output when run standaloneTest().")
            Exit Sub
        End If

        vbDrawFPoly(dst2, poly, cv.Scalar.Red)

        labels(3) = "White is the original polygon, yellow has been rotated " + Format(rotateAngle * 57.2958) + " degrees"

        ' translate so the center of rotation is 0,0
        Dim translated As New List(Of cv.Point2f)
        For i = 0 To poly.Count - 1
            Dim pt = poly(i)
            translated.Add(New cv.Point2f(poly(i).X - rotateCenter.X, poly(i).Y - rotateCenter.Y))
        Next

        Dim rotated As New List(Of cv.Point2f)
        For i = 0 To poly.Count - 1
            Dim pt = translated(i)
            Dim x = pt.X * Math.Cos(rotateAngle) - pt.Y * Math.Sin(rotateAngle)
            Dim y = pt.Y * Math.Cos(rotateAngle) + pt.X * Math.Sin(rotateAngle)
            rotated.Add(New cv.Point2f(x, y))
        Next

        vbDrawFPoly(dst3, poly, white)
        poly.Clear()
        For Each pt In rotated
            poly.Add(New cv.Point2f(pt.X + rotateCenter.X, pt.Y + rotateCenter.Y))
        Next

        vbDrawFPoly(dst3, poly, cv.Scalar.Yellow)
    End Sub
End Class







Public Class Rotate_Example : Inherits VB_Algorithm
    Dim rotate As New Rotate_Basics
    Public Sub New()
        rotate.rotateCenter = New cv.Point2f(dst2.Height / 2, dst2.Height / 2)
        rotate.rotateAngle = -90
        desc = "Reminder on how to rotate an image and keep all the pixels."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim r = New cv.Rect(0, 0, src.Height, src.Height)
        dst2(r) = src.Resize(New cv.Size(src.Height, src.Height))
        rotate.Run(dst2)
        dst3(r) = rotate.dst2(New cv.Rect(0, 0, src.Height, src.Height))
    End Sub
End Class
