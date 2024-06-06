Imports cv = OpenCvSharp
Imports System.Math
Imports System.Transactions
Imports System.Windows
Imports System.Windows.Media.Imaging
' https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
Public Class Rotate_Basics : Inherits VB_Parent
    Public M As cv.Mat
    Public Mflip As cv.Mat
    Public options As New Options_Resize
    Public rotateAngle As Single = 1000
    Public rotateCenter As cv.Point
    Public Sub New()
        rotateCenter = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Rotation Angle in degrees", -180, 180, 24)
        desc = "Rotate a rectangle by a specified angle"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static angleSlider = findSlider("Rotation Angle in degrees")
        rotateAngle = angleSlider.Value
        options.RunVB()

        M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
        dst2 = src.WarpAffine(M, src.Size(), options.warpFlag)
        If options.warpFlag = cv.InterpolationFlags.WarpInverseMap Then
            Mflip = cv.Cv2.GetRotationMatrix2D(rotateCenter, rotateAngle, 1)
        End If
    End Sub
End Class







' https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
Public Class Rotate_BasicsQT : Inherits VB_Parent
    Public rotateAngle As Single = 24
    Public rotateCenter As cv.Point2f
    Public Sub New()
        rotateCenter = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)
        desc = "Rotate a rectangle by a specified angle"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
        dst2 = src.WarpAffine(M, src.Size(), cv.InterpolationFlags.Nearest)
    End Sub
End Class






Public Class Rotate_Box : Inherits VB_Parent
    ReadOnly rotation As New Rotate_Basics
    Public Sub New()
        task.drawRect = New cv.Rect(100, 100, 100, 100)
        labels(2) = "Original Rectangle in the original perspective"
        labels(3) = "Same Rectangle in the new warped perspective"
        desc = "Track a rectangle no matter how the perspective is warped.  Draw a rectangle anywhere."
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
Public Class Rotate_Poly : Inherits VB_Parent
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

        drawCircle(dst2,rotateQT.rotateCenter, task.dotSize + 2, cv.Scalar.Yellow)
        setTrueText("center of rotation", rotateQT.rotateCenter)
        labels(3) = rotateQT.labels(3)
    End Sub
End Class








' https://academo.org/demos/rotation-about-point/
Public Class Rotate_PolyQT : Inherits VB_Parent
    Public poly As New List(Of cv.Point2f)
    Public rotateCenter As cv.Point2f
    Public rotateAngle As Single
    Public Sub New()
        labels = {"", "", "Polygon before rotation", ""}
        desc = "Rotate a triangle around a center of rotation"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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







Public Class Rotate_Example : Inherits VB_Parent
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






Public Class Rotate_Horizon : Inherits VB_Parent
    Dim rotate As New Rotate_Basics
    Dim edges As New CameraMotion_WithRotation
    Public Sub New()
        findSlider("Rotation Angle in degrees").Value = 3
        labels(2) = "White is the current horizon vector of the camera.  Highlighted color is the rotated horizon vector."
        desc = "Rotate the horizon independently from the rotation of the image to validate the Edge_CameraMotion algorithm."
    End Sub
    Function RotatePoint(point As cv.Point2f, center As cv.Point2f, angle As Double) As cv.Point2f
        Dim radians As Double = angle * (PI / 180.0)

        Dim sinAngle As Double = Sin(radians)
        Dim cosAngle As Double = Cos(radians)

        Dim x As Double = point.X - center.X
        Dim y As Double = point.Y - center.Y

        Dim xNew As Double = x * cosAngle - y * sinAngle
        Dim yNew As Double = x * sinAngle + y * cosAngle

        xNew += center.X
        yNew += center.Y

        Return New cv.Point2f(xNew, yNew)
    End Function
    Public Sub RunVB(src As cv.Mat)
        Static angleSlider = findSlider("Rotation Angle in degrees")
        rotate.rotateAngle = angleSlider.Value

        rotate.Run(src)
        dst2 = rotate.dst2.Clone
        dst1 = dst2.Clone

        Dim horizonVec = New pointPair(task.horizonVec.p1, task.horizonVec.p2)

        horizonVec.p1 = RotatePoint(task.horizonVec.p1, rotate.rotateCenter, -rotate.rotateAngle)
        horizonVec.p2 = RotatePoint(task.horizonVec.p2, rotate.rotateCenter, -rotate.rotateAngle)

        drawLine(dst2, horizonVec.p1, horizonVec.p2, task.highlightColor)
        drawLine(dst2, task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.White)

        Dim y1 = horizonVec.p1.Y - task.horizonVec.p1.Y
        Dim y2 = horizonVec.p2.Y - task.horizonVec.p2.Y
        edges.translateRotateY(y1, y2)

        rotate.rotateAngle = edges.rotationY
        rotate.rotateCenter = edges.centerY
        rotate.Run(dst1)
        dst3 = rotate.dst2.Clone

        strOut = edges.strOut
    End Sub
End Class