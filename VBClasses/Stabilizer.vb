Imports cv = OpenCvSharp
Public Class Stabilizer_Basics : Inherits TaskParent
    Private refLine As lpData
    Public Sub New()
        desc = "Cursor.ai: Use lpCurr to stabilize the grayscale image (rotation + translation to a reference line)."
        labels(2) = "Stabilized grayscale image from tracked line (lpCurr)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.grayOriginal

        If task.lineTrack.reSyncImage Then
            dst2 = src.Clone
            Exit Sub
        End If

        Dim lpCurr = task.lineTrack.lpCurr
        If task.heartBeatLT Or task.optionsChanged Then
            refLine = New lpData(lpCurr.p1, lpCurr.p2)
        End If

        Dim angleDelta = lpCurr.angle - refLine.angle
        Dim tx = refLine.ptE1.X - lpCurr.ptE1.X
        Dim ty = 0 ' lpPerpRef.ptE1.Y - lpPerpCurr.ptE1.Y

        Dim M = cv.Cv2.GetRotationMatrix2D(lpCurr.ptCenter, -angleDelta, 1.0)
        M.Set(Of Double)(0, 2, M.Get(Of Double)(0, 2) + tx)
        M.Set(Of Double)(1, 2, M.Get(Of Double)(1, 2) + ty)

        dst2 = src.WarpAffine(M, src.Size, cv.InterpolationFlags.Linear, cv.BorderTypes.Constant)
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3.Line(refLine.ptE1, refLine.ptE2, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        dst3.Line(lpCurr.ptE1, lpCurr.ptE2, cv.Scalar.Red, task.lineWidth + 1, task.lineType)

        labels(3) = "Delta Angle=" + Format(angleDelta, fmt2) + " deg, tx=" + Format(tx, fmt2) + ", ty=" + Format(ty, fmt2)
    End Sub
End Class




Public Class NR_Stabilizer_Basics : Inherits TaskParent
    Dim feat As New Feature_Basics
    Dim knn As New KNN_Basics
    Public Sub New()
        desc = "Reset the image on every heartbeat"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then
            feat.Run(task.gray)
            dst2 = feat.dst2
            labels(2) = feat.labels(2)

            knn.ptListQuery = feat.features
            knn.ptListTrain = feat.lastFeatures
            If feat.lastFeatures.Count = 0 Then knn.ptListTrain = feat.features
            If feat.features.Count > 0 And feat.lastFeatures.Count > 0 Then
                knn.Run(emptyMat)

                dst3.SetTo(0)
                For i = 0 To Math.Min(knn.ptListTrain.Count, knn.ptListQuery.Count) - 1
                    Dim p1 = knn.ptListQuery(i)
                    Dim p2 = knn.ptListTrain(knn.result(i, 0))
                    dst3.Line(p1, p2, task.highlight, task.lineWidth)
                Next
            End If
        End If
    End Sub
End Class





Public Class Stabilizer_IMU : Inherits TaskParent
    Dim warper As New WarpAffine_Basics
    Public Sub New()
        desc = "Use IMU tilt deltas to stabilize grayscale, then use AddWeighted_Accumulate."
        labels(2) = "IMU-stabilized grayscale accumulated (~last 10 frames)"
        labels(3) = "Current IMU-stabilized grayscale frame"
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim graySrc = If(src.Channels = 1, src, task.gray)

        If task.optionsChanged Or task.heartBeat Then
            warper.baselineRoll = task.accRadians.Z
            warper.baselinePitch = task.accRadians.X
        End If

        warper.Run(task.gray)
        dst2 = warper.dst2
        dst3 = warper.dst3
        labels = warper.labels
    End Sub
End Class






''' <summary>
''' Estimate camera pitch, roll, and yaw while tracking the longest line (task.lineTrack.lpCurr).
''' Pitch and roll come from IMU/gravity (task.accRadians); yaw is not observable from gravity alone,
''' so this task integrates gyro Y (rad/s) over time for yaw. Roll is also shown from the gravity
''' line in the image (lpGravity.angle) for image-plane context vs. the tracked line.
''' </summary>
Public Class Stabilizer_OrientationPRY : Inherits TaskParent
    Private Const RadToDeg As Double = 180.0 / Math.PI
    Private lastImuTs As Double
    Private yawRad As Single

    Public Sub New()
        desc = "Cursor.ai Pitch/roll from IMU gravity (accRadians); yaw from gyro integration; overlay longest tracked line (lpCurr)."
        labels(2) = "Pitch, roll, yaw (see labels(3) for line + sync state)"
        labels(3) = "lpCurr vs gravity; reSyncImage when longest line is lost"
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.color.Clone

        If task.lines.lpList.Count = 0 OrElse task.lineTrack Is Nothing Then
            labels(2) = "No lines or line tracker unavailable."
            labels(3) = ""
            Exit Sub
        End If

        Dim lpCurr = task.lineTrack.lpCurr
        If lpCurr.length <= 0 Then
            labels(2) = "Longest line not ready (lpCurr invalid)."
            labels(3) = ""
            Exit Sub
        End If

        If task.optionsChanged Or task.firstPass Then
            lastImuTs = task.IMU_TimeStamp
            yawRad = 0.0F
        Else
            Dim dt = (task.IMU_TimeStamp - lastImuTs) / 1000.0
            If task.Settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then dt /= 1000.0
            dt = Math.Max(0.000001, Math.Min(1.0, dt))
            yawRad += CSng(task.IMU_AngularVelocity.Y * dt)
            lastImuTs = task.IMU_TimeStamp
        End If

        Dim pitchDeg = task.accRadians.X * RadToDeg
        Dim rollDeg = task.accRadians.Z * RadToDeg
        Dim yawDeg = yawRad * RadToDeg

        Dim rollImageDeg As Single = 0.0F
        If task.lpGravity IsNot Nothing AndAlso task.lpGravity.length > 0 Then
            rollImageDeg = task.lpGravity.angle
        End If

        dst2.Line(lpCurr.ptE1, lpCurr.ptE2, cv.Scalar.Red, task.lineWidth + 1, task.lineType)
        If task.lpGravity IsNot Nothing AndAlso task.lpGravity.length > 0 Then
            dst2.Line(task.lpGravity.ptE1, task.lpGravity.ptE2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        End If

        labels(2) = "Pitch=" + Format(pitchDeg, fmt2) + " deg  Roll(IMU Z)=" + Format(rollDeg, fmt2) +
                    " deg  Yaw(gyro int)=" + Format(yawDeg, fmt2) + " deg"
        labels(3) = "lpCurr angle=" + Format(lpCurr.angle, fmt2) + " deg, roll(image lpGravity)=" + Format(rollImageDeg, fmt2) +
                    " deg, reSyncImage=" + CStr(task.lineTrack.reSyncImage)

        strOut = "Pitch/roll use task.accRadians (X,Z) from gravity/IMU pipeline (radians converted to degrees)." + vbCrLf +
                 "Yaw uses integrated IMU_AngularVelocity.Y (rad/s); reset on options change — it drifts without magnetometer." + vbCrLf +
                 "Red = longest tracked line (lpCurr); yellow = gravity line (lpGravity)." + vbCrLf +
                 "To reduce jitter: raise IMU alpha filter, mount the camera rigidly, and move smoothly."
        SetTrueText(strOut, 3)
    End Sub
End Class




''' <summary>
''' Estimate pitch, roll, and yaw from task.longestLine and task.lpGravity in the image plane.
''' Roll: tilt of projected gravity vs image-down (atan2 of gravity unit vector).
''' Pitch: acute angle (deg) between the longest line and the gravity line in the image (0 when parallel in the plane).
''' Yaw: signed angle of the longest line vs in-image "horizontal" (perpendicular to projected gravity); useful when the tracked line is horizontal in the world.
''' </summary>
Public Class Stabilizer_PRY : Inherits TaskParent
    Public Sub New()
        desc = "Cursor.ai: Pitch, roll, yaw from task.longestLine and task.lpGravity (image-plane geometry; yaw assumes scene line is horizontal in world)."
        labels(2) = "PRY (deg) from longest line + gravity"
        labels(3) = "Red = longestLine, yellow = lpGravity"
    End Sub
    Private Shared Function Unit2D(p1 As cv.Point2f, p2 As cv.Point2f) As cv.Point2f
        Dim dx = p2.X - p1.X
        Dim dy = p2.Y - p1.Y
        Dim n = CSng(Math.Sqrt(dx * dx + dy * dy))
        If n < 0.000001F Then Return New cv.Point2f(0.0F, 1.0F)
        Return New cv.Point2f(dx / n, dy / n)
    End Function
    Private Shared Function WrapDeg(d As Double) As Double
        While d > 180.0
            d -= 360.0
        End While
        While d < -180.0
            d += 360.0
        End While
        Return d
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        If task.longestLine Is Nothing Then
            labels(2) = "task.longestLine not available (no lines in the image?)."
            Exit Sub
        End If

        Dim g = Unit2D(task.lpGravity.ptE1, task.lpGravity.ptE2)
        Dim l = Unit2D(task.longestLine.ptE1, task.longestLine.ptE2)

        Dim rollDeg = WrapDeg(Math.Atan2(g.X, g.Y) * RadToDeg)

        Dim dotGL = l.X * g.X + l.Y * g.Y
        Dim crossGL = l.X * g.Y - l.Y * g.X
        task.pitchDeg = Math.Atan2(Math.Abs(crossGL), Math.Abs(dotGL) + 0.000001F) * RadToDeg
        If task.pitchDeg > 90.0 Then task.pitchDeg = 180.0 - task.pitchDeg

        Dim hx = -g.Y
        Dim hy = g.X
        Dim hn = CSng(Math.Sqrt(hx * hx + hy * hy))
        If hn > 0.000001F Then
            hx /= hn
            hy /= hn
        Else
            hx = 1.0F
            hy = 0.0F
        End If
        Dim yawDeg = WrapDeg(Math.Atan2(l.X * hy - l.Y * hx, l.X * hx + l.Y * hy) * RadToDeg)

        dst2.Line(task.longestLine.ptE1, task.longestLine.ptE2, cv.Scalar.Red, task.lineWidth + 1, task.lineType)
        dst2.Line(task.lpGravity.ptE1, task.lpGravity.ptE2, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)

        labels(2) = "Pitch=" + Format(task.pitchDeg, fmt2) + "  Roll=" + Format(rollDeg, fmt2) + "  Yaw=" + Format(yawDeg, fmt2)
        labels(3) = "longestLine angle=" + Format(task.longestLine.angle, fmt2) + " deg, lpGravity angle=" + Format(task.lpGravity.angle, fmt2) + " deg"

        strOut = "Roll: atan2(gx, gy) for unit gravity direction in the image (0 when gravity aligns with image +Y / down)." + vbCrLf +
                 "Pitch: acute angle between longestLine and lpGravity in the image (both edge directions)." + vbCrLf +
                 "Yaw: signed angle of longestLine vs horizontal perpendicular to gravity (best when the scene line is horizontal in world coordinates)."
        SetTrueText(strOut, 3)
    End Sub
End Class