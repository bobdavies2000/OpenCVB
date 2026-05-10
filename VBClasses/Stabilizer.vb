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
    Public baselineRoll As Single
    Public baselinePitch As Single
    Dim warper As New WarpAffine_Basics
    Public Sub New()
        desc = "Use IMU tilt deltas to stabilize grayscale, then use AddWeighted_Accumulate."
        labels(2) = "IMU-stabilized grayscale accumulated (~last 10 frames)"
        labels(3) = "Current IMU-stabilized grayscale frame"
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim graySrc = If(src.Channels = 1, src, task.gray)

        If task.optionsChanged Or task.heartBeat Then
            baselineRoll = task.accRadians.Z
            baselinePitch = task.accRadians.X
        End If

        warper.baselineRoll = baselineRoll
        warper.baselinePitch = baselinePitch
        warper.Run(task.gray)
        dst2 = warper.dst2
        dst3 = warper.dst3
        labels = warper.labels
    End Sub
End Class