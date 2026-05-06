Imports cv = OpenCvSharp
Public Class Stabilizer_Basics : Inherits TaskParent
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





Public Class Stabilizer_BasicsFail : Inherits TaskParent
    Public Sub New()
        desc = "Use task.lines.lplist(0) to find the angle needed to stabilize the image."
    End Sub
    Public Function GetAngleBetweenLinesBySlopes(ByVal slope1 As Double, ByVal slope2 As Double) As Double
        Const EPSILON As Double = 0.000000001

        ' --- Handle Vertical Lines (Infinite Slope) ---
        Dim isSlope1Vertical As Boolean = Double.IsInfinity(slope1)
        Dim isSlope2Vertical As Boolean = Double.IsInfinity(slope2)

        If isSlope1Vertical AndAlso isSlope2Vertical Then
            ' Both lines are vertical, so they are parallel.
            Return 0.0 ' Angle is 0 degrees
        ElseIf isSlope1Vertical Then
            ' Line 1 is vertical (angle 90 degrees).
            ' Angle of line 2 is Atan(slope2).
            Dim angle2Degrees As Double = Math.Atan(slope2) * 180 / cv.Cv2.PI
            Dim angleDiff As Double = Math.Abs(90.0 - angle2Degrees)
            Return angleDiff
        ElseIf isSlope2Vertical Then
            ' Line 2 is vertical (angle 90 degrees).
            ' Angle of line 1 is Atan(slope1).
            Dim angle1Degrees As Double = Math.Atan(slope1) * 180 / cv.Cv2.PI
            Dim angleDiff As Double = Math.Abs(90.0 - angle1Degrees)
            Return angleDiff
        End If

        ' --- Handle Perpendicular Lines (Product of slopes is -1) ---
        ' Check if 1 + m1*m2 is very close to zero, indicating perpendicularity.
        If Math.Abs(1 + slope1 * slope2) < EPSILON Then
            Return 90.0 ' Lines are perpendicular (90 degrees)
        End If

        ' --- General Case: Use the tangent formula ---
        Dim tanTheta As Double = (slope2 - slope1) / (1 + slope1 * slope2)
        Dim angleRadians As Double = Math.Atan(tanTheta) ' Result is in (-PI/2, PI/2)
        Dim angleDegrees As Double = angleRadians * 180 / cv.Cv2.PI

        Return angleDegrees
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lpLast As lpData = task.lines.lpList(0)

        Dim lp = task.lines.lpList(0)
        If lp.pE1 = lpLast.pE1 And lp.pE2 = lpLast.pE2 Or task.lineLongestChanged Then
            dst2 = src
            If task.lineLongestChanged Then lpLast = task.lines.lpList(0)
        Else
            Dim rotateAngle = GetAngleBetweenLinesBySlopes(lp.slope, lpLast.slope)

            Dim rotateCenter = Line_Intersection.IntersectTest(lp, lpLast)
            Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
            dst2 = src.WarpAffine(M, src.Size(), cv.InterpolationFlags.Cubic)

            labels(2) = "Image after rotation by " + Format(rotateAngle, fmt3) + " degrees"
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
