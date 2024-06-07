Imports cv = OpenCvSharp
Public Class CameraMotion_Basics : Inherits VB_Parent
    Public translationX As Integer
    Public translationY As Integer
    Dim gravity As New Gravity_Horizon
    Public secondOpinion As Boolean
    Public Sub New()
        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.gOptions.DebugSlider.Value = 3
        desc = "Merge with previous image using just translation of the gravity vector and horizon vector (if present)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gravity.Run(src)

        Static gravityVec As pointPair = New pointPair(task.gravityVec.p1, task.gravityVec.p2)
        Static horizonVec As pointPair = New pointPair(task.horizonVec.p1, task.horizonVec.p2)

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        translationX = task.gOptions.DebugSlider.Value ' Math.Round(gravityVec.p1.X - task.gravityVec.p1.X)
        translationY = task.gOptions.DebugSlider.Value ' Math.Round(horizonVec.p1.Y - task.horizonVec.p1.Y)
        If Math.Abs(translationX) >= dst2.Width / 2 Then translationX = 0
        If horizonVec.p1.Y >= dst2.Height Or horizonVec.p2.Y >= dst2.Height Or Math.Abs(translationY) >= dst2.Height / 2 Then
            horizonVec = New pointPair(New cv.Point2f, New cv.Point2f(336, 0))
            translationY = 0
        End If

        Dim r1 As cv.Rect, r2 As cv.Rect
        If translationX = 0 And translationY = 0 Then
            dst2 = src
            task.camMotionPixels = 0
            task.camDirection = 0
        Else
            ' dst2.SetTo(0)
            r1 = New cv.Rect(translationX, translationY, Math.Min(dst2.Width - translationX * 2, dst2.Width),
                                                         Math.Min(dst2.Height - translationY * 2, dst2.Height))
            If r1.X < 0 Then
                r1.X = -r1.X
                r1.Width += translationX * 2
            End If
            If r1.Y < 0 Then
                r1.Y = -r1.Y
                r1.Height += translationY * 2
            End If

            r2 = New cv.Rect(Math.Abs(translationX), Math.Abs(translationY), r1.Width, r1.Height)

            task.camMotionPixels = Math.Sqrt(translationX * translationX + translationY * translationY)
            If translationX = 0 Then
                If translationY < 0 Then task.camDirection = Math.PI / 4 Else task.camDirection = Math.PI * 3 / 4
            Else
                task.camDirection = Math.Atan(translationY / translationX)
            End If

            If secondOpinion Then
                Static feat As New Swarm_Basics

                dst3.SetTo(0)
                ' the point cloud contributes one set of camera motion distance and direction.  Now confirm it with feature points
                feat.Run(src)
                strOut = "Swarm distance = " + Format(feat.distanceAvg, fmt1) + " when camMotionPixels = " + Format(task.camMotionPixels, fmt1)
                If (feat.distanceAvg < task.camMotionPixels / 2) Or task.heartBeat Then
                    task.camMotionPixels = 0
                    src.CopyTo(dst2)
                End If
                dst3 = (src - dst2).ToMat.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
            End If
        End If

        gravityVec = New pointPair(task.gravityVec.p1, task.gravityVec.p2)
        horizonVec = New pointPair(task.horizonVec.p1, task.horizonVec.p2)
        setTrueText(strOut, 3)

        labels(2) = "Translation (X, Y) = (" + CStr(translationX) + ", " + CStr(translationY) + ")" +
                    If(horizonVec.p1.Y = 0 And horizonVec.p2.Y = 0, " there is no horizon present", "")
        labels(3) = "Camera direction (radians) = " + Format(task.camDirection, fmt1) + " with distance = " + Format(task.camMotionPixels, fmt1)
    End Sub
End Class







Public Class CameraMotion_WithRotation : Inherits VB_Parent
    Public translationX As Single
    Public rotationX As Single
    Public centerX As cv.Point2f
    Public translationY As Single
    Public rotationY As Single
    Public centerY As cv.Point2f
    Public rotate As New Rotate_BasicsQT
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Merge with previous image using rotation AND translation of the camera motion - not as good as translation alone."
    End Sub
    Public Sub translateRotateX(x1 As Integer, x2 As Integer)
        rotationX = Math.Atan(Math.Abs((x1 - x2)) / dst2.Height) * 57.2958
        centerX = New cv.Point2f((task.gravityVec.p1.X + task.gravityVec.p2.X) / 2, (task.gravityVec.p1.Y + task.gravityVec.p2.Y) / 2)
        If x1 >= 0 And x2 > 0 Then
            translationX = If(x1 > x2, x1 - x2, x2 - x1)
            centerX = task.gravityVec.p2
        ElseIf x1 <= 0 And x2 < 0 Then
            translationX = If(x1 > x2, x1 - x2, x2 - x1)
            centerX = task.gravityVec.p1
        ElseIf x1 < 0 And x2 > 0 Then
            translationX = 0
        Else
            translationX = 0
            rotationX *= -1
        End If
    End Sub
    Public Sub translateRotateY(y1 As Integer, y2 As Integer)
        rotationY = Math.Atan(Math.Abs((y1 - y2)) / dst2.Width) * 57.2958
        centerY = New cv.Point2f((task.horizonVec.p1.X + task.horizonVec.p2.X) / 2, (task.horizonVec.p1.Y + task.horizonVec.p2.Y) / 2)
        If y1 > 0 And y2 > 0 Then
            translationY = If(y1 > y2, y1 - y2, y2 - y1)
            centerY = task.horizonVec.p2
        ElseIf y1 < 0 And y2 < 0 Then
            translationY = If(y1 > y2, y1 - y2, y2 - y1)
            centerY = task.horizonVec.p1
        ElseIf y1 < 0 And y2 > 0 Then
            translationY = 0
        Else
            translationY = 0
            rotationY *= -1
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static gravityVec As pointPair = task.gravityVec
        Static horizonVec As pointPair = task.horizonVec
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim x1 = gravityVec.p1.X - task.gravityVec.p1.X
        Dim x2 = gravityVec.p2.X - task.gravityVec.p2.X

        Dim y1 = horizonVec.p1.Y - task.horizonVec.p1.Y
        Dim y2 = horizonVec.p2.Y - task.horizonVec.p2.Y

        translateRotateX(x1, x2)
        translateRotateY(y1, y2)

        dst1.SetTo(0)
        dst3.SetTo(0)
        If Math.Abs(x1 - x2) > 0.5 Or Math.Abs(y1 - y2) > 0.5 Then
            Dim r1 = New cv.Rect(translationX, translationY, dst2.Width - translationX, dst2.Height - translationY)
            Dim r2 = New cv.Rect(0, 0, r1.Width, r1.Height)
            dst1(r2) = src(r1)
            rotate.rotateAngle = rotationY
            rotate.rotateCenter = centerY
            rotate.Run(dst1)
            dst2 = rotate.dst2
            dst3 = (src - dst2).ToMat.Threshold(task.gOptions.pixelDiffThreshold, 255, cv.ThresholdTypes.Binary)
        Else
            dst2 = src
        End If

        gravityVec = task.gravityVec
        horizonVec = task.horizonVec

        labels(2) = "Translation X = " + Format(translationX, fmt1) + " rotation X = " + Format(rotationX, fmt1) + " degrees " +
                    " center of rotation X = " + Format(centerX.X, fmt0) + ", " + Format(centerX.Y, fmt0)
        labels(3) = "Translation Y = " + Format(translationY, fmt1) + " rotation Y = " + Format(rotationY, fmt1) + " degrees " +
                    " center of rotation Y = " + Format(centerY.X, fmt0) + ", " + Format(centerY.Y, fmt0)
    End Sub
End Class







Public Class CameraMotion_SceneMotion : Inherits VB_Parent
    Dim cMotion As New CameraMotion_Basics
    Dim motion As New Motion_Basics
    Public Sub New()
        labels(2) = "Image after adjusting for camera motion."
        desc = "Display both camera motion (on heartbeats) and scene motion."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        cMotion.Run(src)
        dst2 = cMotion.dst3

        motion.Run(src)
        dst3 = motion.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class

