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
        Dim tx = refLine.pE1.X - lpCurr.pE1.X
        Dim ty = 0 ' lpPerpRef.pE1.Y - lpPerpCurr.pE1.Y

        Dim M = cv.Cv2.GetRotationMatrix2D(lpCurr.ptCenter, -angleDelta, 1.0)
        M.Set(Of Double)(0, 2, M.Get(Of Double)(0, 2) + tx)
        M.Set(Of Double)(1, 2, M.Get(Of Double)(1, 2) + ty)

        dst2 = src.WarpAffine(M, src.Size, cv.InterpolationFlags.Linear, cv.BorderTypes.Constant)
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst3.Line(refLine.pE1, refLine.pE2, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
        dst3.Line(lpCurr.pE1, lpCurr.pE2, cv.Scalar.Red, task.lineWidth + 1, task.lineType)

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
            knn.Run(emptyMat)

            dst3.SetTo(0)
            For i = 0 To Math.Min(knn.ptListTrain.Count, knn.ptListQuery.Count) - 1
                Dim p1 = knn.ptListQuery(i)
                Dim p2 = knn.ptListTrain(knn.result(i, 0))
                dst3.Line(p1, p2, task.highlight, task.lineWidth)
            Next
        End If
    End Sub
End Class





Public Class Stabilizer_IMU : Inherits TaskParent
    Private Const RadToDeg As Double = 57.29577951308232
    Private Const PixelsPerRad As Single = 60.0F
    Private Const MaxShift As Single = 30.0F

    Private accum As New AddWeighted_Accumulate
    Private baselineRoll As Single
    Private baselinePitch As Single
    Private baselineSet As Boolean

    Public Sub New()
        desc = "Cursor.ai: Use IMU tilt deltas to stabilize grayscale, then use AddWeighted_Accumulate."
        labels(2) = "IMU-stabilized grayscale accumulated (~last 10 frames)"
        labels(3) = "Current IMU-stabilized grayscale frame"
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim graySrc = If(src.Channels = 1, src, task.gray)
        If graySrc.Empty Then Exit Sub

        If task.optionsChanged Or baselineSet = False Then
            baselineRoll = task.accRadians.Z
            baselinePitch = task.accRadians.X
            baselineSet = True
            accum.dst3.SetTo(0)
        End If

        Dim rollDelta = task.accRadians.Z - baselineRoll
        Dim pitchDelta = task.accRadians.X - baselinePitch

        Dim angleDeg = -rollDelta * RadToDeg
        Dim dx = CSng(-pitchDelta * PixelsPerRad - task.IMU_AngularVelocity.Y * 4.0F)
        Dim dy = CSng(task.IMU_AngularVelocity.X * 4.0F)
        dx = Math.Max(-MaxShift, Math.Min(MaxShift, dx))
        dy = Math.Max(-MaxShift, Math.Min(MaxShift, dy))

        Dim center = New cv.Point2f(graySrc.Cols / 2.0F, graySrc.Rows / 2.0F)
        Dim M = cv.Cv2.GetRotationMatrix2D(center, angleDeg, 1.0)
        M.Set(Of Double)(0, 2, M.Get(Of Double)(0, 2) + dx)
        M.Set(Of Double)(1, 2, M.Get(Of Double)(1, 2) + dy)

        dst3 = graySrc.WarpAffine(M, graySrc.Size, cv.InterpolationFlags.Linear, cv.BorderTypes.Reflect101)

        accum.Run(dst3)
        dst2 = accum.dst2.Clone

        labels(2) = "IMU stabilize + AddWeighted_Accumulate (weight 0.1 ~= last 10 frames)."
        labels(3) = "Angle=" + Format(angleDeg, fmt2) + " deg, dx=" + Format(dx, fmt2) + ", dy=" + Format(dy, fmt2)
    End Sub
End Class
