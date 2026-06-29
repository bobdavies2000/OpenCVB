Imports cv = OpenCvSharp
''' <summary>Compute the gravity vector using the complementary filter: fuse gyro (fast, drifts) with accelerometer (slow, stable).</summary>
Public Class Gravity_Basics_TA : Inherits TaskParent
    Dim lastTimeStamp As Double
    Dim optionsIMU As New Options_IMU
    ''' <summary>Unit gravity vector in body/sensor frame (points down).</summary>
    Public GravityVector As New cv.Point3f(0, 0, -1)
    Public Sub New()
        If standalone Then task.gOptions.CrossHairs.Checked = True
        desc = "Compute the gravity vector using the complementary filter: fuse gyroscope (fast, drifts) with accelerometer (slow, stable)."
        labels(2) = "Complementary-filter gravity: angles and unit gravity vector"
    End Sub
    Public Shared Sub showVectors(dst As cv.Mat)
        dst.Line(task.lpGravity.ptE1, task.lpGravity.ptE2, white, task.lineWidth, task.lineType)
        dst.Line(task.lpHorizon.ptE1, task.lpHorizon.ptE2, white, task.lineWidth, task.lineType)
    End Sub
    ''' <summary>Compute two image points for the line through (cx,cy) in direction of gravity projection (gx,gy), extended to rect [0,w] x [0,h].</summary>
    Public Function GravityVectorToLineEndpoints(gravityVec As cv.Point3f, width As Integer, height As Integer) As (p1 As cv.Point2f, p2 As cv.Point2f)
        Dim cx = width / 2.0F
        Dim cy = height / 2.0F
        Dim dx = gravityVec.X
        Dim dy = gravityVec.Y
        Dim len = CSng(Math.Sqrt(dx * dx + dy * dy))
        If len < 0.0001F Then
            dx = 0.0F
            dy = 1.0F
        Else
            dx /= len
            dy /= len
        End If
        Dim tList As New List(Of Single)
        If Math.Abs(dx) > 0.0001F Then
            Dim t0 = -cx / dx
            Dim y0 = cy + t0 * dy
            If y0 >= 0 And y0 <= height Then tList.Add(t0)
            Dim t1 = (width - cx) / dx
            Dim y1 = cy + t1 * dy
            If y1 >= 0 And y1 <= height Then tList.Add(t1)
        End If
        If Math.Abs(dy) > 0.0001F Then
            Dim t0 = -cy / dy
            Dim x0 = cx + t0 * dx
            If x0 >= 0 And x0 <= width Then tList.Add(t0)
            Dim t1 = (height - cy) / dy
            Dim x1 = cx + t1 * dx
            If x1 >= 0 And x1 <= width Then tList.Add(t1)
        End If
        If tList.Count < 2 Then
            Return (New cv.Point2f(cx, 0), New cv.Point2f(cx, height))
        End If
        tList.Sort()
        Dim tMin = tList(0)
        Dim tMax = tList(tList.Count - 1)
        Dim p1 = New cv.Point2f(cx + tMin * dx, cy + tMin * dy)
        Dim p2 = New cv.Point2f(cx + tMax * dx, cy + tMax * dy)
        Return (p1, p2)
    End Function

    ''' <summary>Unit gravity in body frame from tilt angles (same convention as IMU_GMatrix_TA: roll=X, pitch=Y, yaw=Z).</summary>
    Public Function AnglesToGravityVector(accRadians As cv.Point3f) As cv.Point3f
        Dim cx = CSng(Math.Cos(accRadians.X))
        Dim sx = CSng(Math.Sin(accRadians.X))
        Dim cy = CSng(Math.Cos(accRadians.Y))
        Dim sy = CSng(Math.Sin(accRadians.Y))
        ' R = Ry(pitch)*Rx(roll); world down = (0,0,-1). Rx*(0,0,-1) = (0, sx, -cx). Ry* that = (sy*cx, sx, -cy*cx)
        Dim gx = sy * cx
        Dim gy = sx
        Dim gz = -cy * cx
        Dim n = CSng(Math.Sqrt(gx * gx + gy * gy + gz * gz))
        If n < 0.0001F Then n = 1.0F
        Return New cv.Point3f(gx / n, gy / n, gz / n)
    End Function

    Public Overrides Sub RunAlg(src As cv.Mat)
        optionsIMU.Run()

        Dim gyro = task.IMU_AngularVelocity
        If task.optionsChanged Then
            lastTimeStamp = task.IMU_TimeStamp
        Else
            Dim dt = (task.IMU_TimeStamp - lastTimeStamp) / 1000.0
            If task.Settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then dt /= 1000.0
            dt = Math.Max(0.000001, Math.Min(1.0, dt))
            task.theta += New cv.Point3f(-gyro.Z * dt, -gyro.Y * dt, gyro.X * dt)
            lastTimeStamp = task.IMU_TimeStamp
        End If

        ' Tilt angles from accelerometer (low-pass source)
        Dim g = task.IMU_Acceleration
        task.accRadians = New cv.Point3f(
                    CSng(Math.Atan2(g.X, Math.Sqrt(g.Y * g.Y + g.Z * g.Z))),
                    CSng(Math.Abs(Math.Atan2(g.X, g.Y))),
                    CSng(Math.Atan2(g.Y, g.Z)))

        ' Complementary filter: angle = alpha * (gyro-integrated) + (1-alpha) * (accel-derived)
        If task.optionsChanged Then
            task.theta = task.accRadians
        Else
            Dim a = task.IMU_AlphaFilter
            task.theta.X = a * task.theta.X + (1.0F - a) * task.accRadians.X
            task.theta.Y = task.accRadians.Y
            task.theta.Z = a * task.theta.Z + (1.0F - a) * task.accRadians.Z
        End If

        task.accRadians = task.theta
        If task.accRadians.Y > cv.Cv2.PI / 2 Then task.accRadians.Y -= cv.Cv2.PI / 2
        task.accRadians.Z += cv.Cv2.PI / 2

        Dim y1 = task.accRadians.Y - cv.Cv2.PI
        If task.accRadians.X < 0 Then y1 *= -1
        task.verticalizeAngle = y1 * RadToDeg

        ' Unit gravity vector in body frame (points down)
        GravityVector = AnglesToGravityVector(task.accRadians)

        ' Line through image center in gravity direction (lpData extends to image edges)
        Dim endpoints = GravityVectorToLineEndpoints(GravityVector, task.workRes.Width, task.workRes.Height)
        task.lpHorizon = New lpData(endpoints.p1, endpoints.p2)
        task.lpGravity = Line_Perpendicular.computePerp(task.lpHorizon)

        If standaloneTest() Then
            strOut = "Complementary filter gravity" + vbCrLf +
                         "Tilt (rad): X=" + Format(task.accRadians.X, fmt3) + " Y=" + Format(task.accRadians.Y, fmt3) + " Z=" + Format(task.accRadians.Z, fmt3) + vbCrLf +
                         "Gravity unit vector (body): " + Format(GravityVector.X, fmt3) + ", " + Format(GravityVector.Y, fmt3) + ", " + Format(GravityVector.Z, fmt3)
            SetTrueText(strOut)
        End If
    End Sub
End Class





Public Class Gravity_CloudMethod : Inherits TaskParent
    Public options As New Options_Features
    Public xTop As Single, xBot As Single
    Dim sampleSize As Integer = 25
    Dim ptList As New List(Of Integer)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(2) = "Horizon and Gravity Vectors"
        If standalone Then task.gOptions.gravityPointCloud.Checked = False
        desc = "Method to find gravity and horizon vectors from the IMU"
    End Sub
    Public Shared Sub showVectors(dst As cv.Mat)
        dst.Line(task.lpGravity.ptE1, task.lpGravity.ptE2, white, task.lineWidth, task.lineType)
        dst.Line(task.lpHorizon.ptE1, task.lpHorizon.ptE2, white, task.lineWidth, task.lineType)
        'If task.lpGravity Is Nothing Then
        '    dst.Line(task.lines.lpList(0).p1, task.lines.lpList(0).p2, task.highlight, task.lineWidth * 2, task.lineType)
        '    dst.Line(task.lines.lpList(0).ptE1, task.lines.lpList(0).ptE2, white, task.lineWidth, task.lineType)
        'End If
    End Sub
    Private Function findFirst(points As cv.Mat) As Single
        ptList.Clear()

        For i = 0 To Math.Min(sampleSize, points.Rows / 2)
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 0 Or pt.Y < 0 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            ptList.Add(pt.X)
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Private Function findLast(points As cv.Mat) As Single
        ptList.Clear()

        For i = points.Rows To Math.Max(points.Rows - sampleSize, points.Rows / 2) Step -1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            If pt.X <= 5 Or pt.Y <= 5 Then Continue For
            If pt.X > dst2.Width Or pt.Y > dst2.Height Then Continue For
            ptList.Add(pt.X)
        Next

        If ptList.Count = 0 Then Return 0
        Return ptList.Average()
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim threshold As Single = 0.015F ' surround zero by 15 cm's

        dst3 = task.pcSplit(0).InRange(-threshold, threshold)
        dst3.SetTo(0, task.noDepthMask)
        Dim gPoints = dst3.FindNonZero()
        If gPoints.Rows = 0 Then
            ' build a fake gravity vector when we don't have anything so task.lines.lplist has 1 entry.
            ' It will be updated in the next frame.  This is a startup issue.
            task.lpGravity = New lpData(New cv.Point2f(dst2.Width / 2, 0),
                                                New cv.Point2f(dst2.Width / 2, dst2.Height))

            Exit Sub ' no point cloud data to get the gravity line in the image coordinates.
        End If
        xTop = findFirst(gPoints)
        xBot = findLast(gPoints)
        task.lpGravity = New lpData(New cv.Point2f(xTop, 0), New cv.Point2f(xBot, dst2.Height))
        If standaloneTest() Then
            dst2 = task.color
            dst2.Line(task.lpGravity.p1, task.lpGravity.p2, task.highlight, task.lineWidth, task.lineType)
        End If
        task.lpHorizon = Line_Perpendicular.computePerp(task.lpGravity)
    End Sub
End Class






Public Class Gravity_LineTrackStabilize : Inherits TaskParent
    Private Const LineAngleWeight As Single = 0.6F
    Private Const GravityAngleWeight As Single = 0.4F
    Private Const LineShiftWeight As Single = 0.8F
    Private Const GravityShiftWeight As Single = 0.2F

    Private refLine As lpData
    Private refGravity As lpData
    Private refReady As Boolean

    Private frameHistory As New Queue(Of cv.Mat)
    Private maskHistory As New Queue(Of cv.Mat)

    Public Sub New()
        desc = "Cursor.ai: Fuse gravity vector and LineTrack_Basics_TA.lpCurr to stabilize grayscale."
    End Sub

    Private Shared Function WarpPoint(pt As cv.Point2f, M As cv.Mat) As cv.Point2f
        Dim x = pt.X
        Dim y = pt.Y
        Dim xOut = M.Get(Of Double)(0, 0) * x + M.Get(Of Double)(0, 1) * y + M.Get(Of Double)(0, 2)
        Dim yOut = M.Get(Of Double)(1, 0) * x + M.Get(Of Double)(1, 1) * y + M.Get(Of Double)(1, 2)
        Return New cv.Point2f(CSng(xOut), CSng(yOut))
    End Function

    Private Sub ResetState()
        refReady = False
        frameHistory.Clear()
        maskHistory.Clear()
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim graySrc = If(src.Channels = 1, src, task.gray)
        If graySrc.Empty Then Exit Sub
        If task.optionsChanged Or task.firstPass Then ResetState()

        Dim lpCurr = task.longestLine
        Dim lpGravity = task.lpGravity

        If Not refReady Then
            refLine = lpCurr
            refGravity = lpGravity
            refReady = True
        End If

        Dim dAngleLine = lpCurr.angle - refLine.angle
        Dim dAngleGravity = lpGravity.angle - refGravity.angle
        Dim fusedAngle = LineAngleWeight * dAngleLine + GravityAngleWeight * dAngleGravity

        Dim dxLine = refLine.ptE1.X - lpCurr.ptE1.X
        Dim dyLine = refLine.ptE1.Y - lpCurr.ptE1.Y
        Dim dxGravity = refGravity.ptE1.X - lpGravity.ptE1.X
        Dim dyGravity = refGravity.ptE1.Y - lpGravity.ptE1.Y
        Dim tx = LineShiftWeight * dxLine + GravityShiftWeight * dxGravity
        Dim ty = LineShiftWeight * dyLine + GravityShiftWeight * dyGravity

        Dim M = cv.Cv2.GetRotationMatrix2D(lpCurr.ptCenter, -fusedAngle, 1.0)
        M.Set(Of Double)(0, 2, M.Get(Of Double)(0, 2) + tx)
        M.Set(Of Double)(1, 2, M.Get(Of Double)(1, 2) + ty)

        Dim stabilized = graySrc.WarpAffine(M, graySrc.Size, cv.InterpolationFlags.Linear, cv.BorderTypes.Constant, cv.Scalar.Black)
        Dim srcMask As New cv.Mat(graySrc.Size, cv.MatType.CV_8U, 255)
        Dim validMask = srcMask.WarpAffine(M, graySrc.Size, cv.InterpolationFlags.Nearest, cv.BorderTypes.Constant, cv.Scalar.Black)

        frameHistory.Enqueue(stabilized)
        maskHistory.Enqueue(validMask)
        While frameHistory.Count > task.fOptions.FrameHistoryCount.Value
            frameHistory.Dequeue()
            maskHistory.Dequeue()
        End While

        Dim sumImg As New cv.Mat(graySrc.Size, cv.MatType.CV_32F, 0)
        Dim sumCnt As New cv.Mat(graySrc.Size, cv.MatType.CV_32F, 0)
        Dim frames = frameHistory.ToArray()
        Dim masks = maskHistory.ToArray()
        For i = 0 To frames.Length - 1
            Dim f32 As New cv.Mat
            Dim m32 As New cv.Mat
            frames(i).ConvertTo(f32, cv.MatType.CV_32F)
            masks(i).ConvertTo(m32, cv.MatType.CV_32F, 1.0 / 255.0)
            cv.Cv2.Accumulate(f32, sumImg, masks(i))
            cv.Cv2.Add(sumCnt, m32, sumCnt)
        Next

        Dim denom As New cv.Mat
        cv.Cv2.Max(sumCnt, 1.0, denom)
        Dim avg32 As New cv.Mat
        cv.Cv2.Divide(sumImg, denom, avg32)
        avg32.ConvertTo(dst2, cv.MatType.CV_8U)

        Dim hasData = sumCnt.Threshold(0.5, 255, cv.ThresholdTypes.Binary)
        hasData.ConvertTo(hasData, cv.MatType.CV_8U)
        dst2.SetTo(0, Not hasData)

        dst3 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim currP1 = WarpPoint(lpCurr.p1, M)
        Dim currP2 = WarpPoint(lpCurr.p2, M)
        Dim gravP1 = WarpPoint(lpGravity.p1, M)
        Dim gravP2 = WarpPoint(lpGravity.p2, M)
        dst3.Line(currP1, currP2, cv.Scalar.Red, task.lineWidth + 1, task.lineType)
        dst3.Line(gravP1, gravP2, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)

        labels(2) = "Stabilized accumulation of last " + CStr(frameHistory.Count) + " frames."
        labels(3) = "fusedAngle=" + Format(fusedAngle, fmt2) + " deg tx=" + Format(tx, fmt2) + " ty=" + Format(ty, fmt2)
    End Sub
End Class





Public Class XR_Gravity_RGB : Inherits TaskParent
    Dim survey As New XR_BrickPoint_PopulationSurvey
    Public Sub New()
        desc = "Rotate the RGB image using the offset from gravity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static rotateAngle As Double = task.verticalizeAngle - 2
        Static rotateCenter As cv.Point2f = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)

        rotateAngle += 0.1
        If rotateAngle >= task.verticalizeAngle + 2 Then rotateAngle = task.verticalizeAngle - 2

        Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
        dst3 = src.WarpAffine(M, src.Size(), cv.InterpolationFlags.Nearest)

        survey.Run(dst3)
        dst2 = survey.dst2

        Dim incrX = dst1.Width / task.gridWH
        Dim incrY = dst1.Height / task.gridWH
        For y = 0 To task.gridWH - 1
            For x = 0 To task.gridWH - 1
                SetTrueText(CStr(survey.results(x, y)), New cv.Point(x * incrX, y * incrY), 2)
            Next
        Next
    End Sub
End Class






Public Class XR_Gravity_BrickRotate : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Dim survey As New XR_BrickPoint_PopulationSurvey
    Public Sub New()
        desc = "Rotate the grid point using the offset from gravity."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        Dim angle = Math.Abs(task.verticalizeAngle)
        Static rotateAngle As Double = -angle
        Static rotateCenter As cv.Point2f = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)

        rotateAngle += 0.1
        If rotateAngle >= angle Then rotateAngle = -angle

        dst1 = src
        For Each brick In bricks.brickList
            Dim pt = New cv.Point(brick.mm.maxLoc.X + brick.rect.X, brick.mm.maxLoc.Y + brick.rect.Y)
            If pt.Y = brick.rect.Y Then
                dst1.Circle(pt, task.DotSize, task.highlight, -1, task.lineType)
            End If
        Next

        Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
        dst3 = dst1.WarpAffine(M, dst1.Size(), cv.InterpolationFlags.Nearest)

        survey.Run(dst3)
        dst2 = survey.dst2

        Dim incrX = dst1.Width / task.gridWH
        Dim incrY = dst1.Height / task.gridWH
        For y = 0 To task.gridWH - 1
            For x = 0 To task.gridWH - 1
                SetTrueText(CStr(survey.results(x, y)), New cv.Point(x * incrX, y * incrY), 2)
            Next
        Next
    End Sub
End Class





Public Class XR_Gravity_Basics_TAOld : Inherits TaskParent
    Public points As New List(Of cv.Point2f)
    Public autoDisplay As Boolean
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find all the points where depth X-component transitions from positive to negative"
    End Sub
    Public Sub displayResults(p1 As cv.Point, p2 As cv.Point)
        If task.heartBeat Then
            If p1.Y >= 1 And p1.Y <= dst2.Height - 1 Then strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf
        End If

        dst2.SetTo(0)
        dst3.SetTo(0)
        For Each pt In points
            dst2.Circle(pt, task.DotSize, white, -1, task.lineType)
        Next

        dst2.Line(task.lpGravity.p1, task.lpGravity.p2, white, task.lineWidth, task.lineType)
        dst3.Line(task.lpGravity.p1, task.lpGravity.p2, white, task.lineWidth, task.lineType)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.pcSplit(0) Else dst0 = src

        dst0 = dst0.Abs()
        dst1 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        dst0.SetTo(task.MaxZmeters, Not dst1)

        points.Clear()
        For i = dst0.Height / 3 To dst0.Height * 2 / 3 - 1
            Dim mm1 = GetMinMax(dst0.Row(i))
            If mm1.minVal > 0 And mm1.minVal < 0.005 Then
                dst0.Row(i).Set(Of Single)(mm1.minLoc.Y, mm1.minLoc.X, 10)
                Dim mm2 = GetMinMax(dst0.Row(i))
                If mm2.minVal > 0 And Math.Abs(mm1.minLoc.X - mm2.minLoc.X) <= 1 Then points.Add(New cv.Point(mm1.minLoc.X, i))
            End If
        Next

        labels(2) = CStr(points.Count) + " points found. "
        Dim p1 As cv.Point2f
        Dim p2 As cv.Point2f
        If points.Count >= 2 Then
            p1 = New cv.Point2f(points(points.Count - 1).X, points(points.Count - 1).Y)
            p2 = New cv.Point2f(points(0).X, points(0).Y)
        End If

        Dim distance = p1.DistanceTo(p2)
        If distance < 10 Then ' enough to get a line with some credibility
            strOut = "Gravity vector not found " + vbCrLf + "The distance of p1 to p2 is " +
                         CStr(CInt(distance)) + " pixels." + vbCrLf
            strOut += "Using the previous value for the gravity vector."
        Else
            Dim lp = New lpData(p1, p2)
            task.lpGravity = New lpData(lp.ptE1, lp.ptE2)
            If standaloneTest() Or autoDisplay Then displayResults(p1, p2)
        End If

        task.lpHorizon = Line_Perpendicular.computePerp(task.lpGravity)
        SetTrueText(strOut, 3)
    End Sub
End Class





Public Class XR_Gravity_Basics_Original : Inherits TaskParent
    Public vec As New lpData
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Search for the transition from positive to negative to find the gravity vector."
    End Sub
    Public Shared Function PrepareDepthInput(index As Integer) As cv.Mat
        If task.gOptions.gravityPointCloud.Checked Then Return task.pcSplit(index) ' already oriented to gravity

        ' rebuild the pointcloud so it is oriented to gravity.
        Dim rc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat.Reshape(3, task.pointCloud.Rows)
        Dim split = rc.Split()
        Return split(index)
    End Function
    Private Function findTransition(startRow As Integer, stopRow As Integer, stepRow As Integer) As cv.Point2f
        Dim val As Single, lastVal As Single
        Dim ptX As New List(Of Single)
        Dim ptY As New List(Of Single)
        For y = startRow To stopRow Step stepRow
            For x = 0 To dst0.Cols - 1
                lastVal = val
                val = dst0.Get(Of Single)(y, x)
                If val > 0 And lastVal < 0 Then
                    ' change to sub-pixel accuracy here 
                    Dim pt = New cv.Point2f(x + Math.Abs(val) / Math.Abs(val - lastVal), y)
                    ptX.Add(pt.X)
                    ptY.Add(pt.Y)
                    If ptX.Count >= task.fOptions.FrameHistoryCount.Value Then
                        Return New cv.Point2f(ptX.Average, ptY.Average)
                    End If
                End If
            Next
        Next
        Return New cv.Point
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then dst0 = task.pcSplit(0) Else dst0 = src

        Dim p1 = findTransition(0, dst0.Height - 1, 1)
        Dim p2 = findTransition(dst0.Height - 1, 0, -1)
        Dim lp = New lpData(p1, p2)
        vec = New lpData(lp.ptE1, lp.ptE2)

        If p1.X >= 1 Then
            strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf + "      val =  " +
                          Format(dst0.Get(Of Single)(p1.Y, p1.X)) + vbCrLf + "lastVal = " + Format(dst0.Get(Of Single)(p1.Y, p1.X - 1))
        End If
        SetTrueText(strOut, 3)

        If standaloneTest() Then
            dst2.SetTo(0)
            dst2.Line(vec.p1, vec.p2, 255, task.lineWidth, task.lineType)
        End If
    End Sub
End Class




Public Class Gravity_Jitter : Inherits TaskParent
    Dim plotX As New PlotTime_Single
    Dim plotY As New PlotTime_Single
    Dim jitterHistory As New List(Of Single)
    Dim lastGravity As lpData
    Dim lastHorizon As lpData
    Public xDelta As Single
    Public yDelta As Single
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        task.fOptions.FrameHistoryCount.Value = task.fOptions.FrameHistoryCount.Maximum
        desc = "Cursor.ai: Measure gravity-vector jitter over time and plot it. Control jitter with IMU alpha filtering and stable mounting."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.firstPass Then
            lastGravity = task.lpGravity
            lastHorizon = task.lpHorizon
        End If

        dst2 = task.color.Clone
        Gravity_Basics_TA.showVectors(dst2)

        xDelta = task.lpGravity.ptE1.DistanceTo(lastGravity.ptE1)
        yDelta = task.lpHorizon.ptE1.DistanceTo(lastHorizon.ptE1)

        jitterHistory.Add(xDelta)
        Dim histCount = task.fOptions.FrameHistoryCount.Value
        If jitterHistory.Count > histCount Then jitterHistory.RemoveAt(0)
        Dim jitterAvg = If(jitterHistory.Count > 0, jitterHistory.Average(), 0)

        Dim jitterMat = cv.Mat.FromPixelData(jitterHistory.Count, 1, cv.MatType.CV_32F, jitterHistory.ToArray)

        plotX.plotData = xDelta
        plotX.Run(src)
        dst1 = plotX.dst2.Clone

        plotY.plotData = yDelta
        plotY.Run(src)
        dst3 = plotY.dst2.Clone

        If task.heartBeat Then
            labels(1) = "xDelta (pixels) =" + Format(xDelta, fmt3)
            labels(3) = "yDelta (pixels) =" + Format(yDelta, fmt3)
        End If
        labels(2) = "Jitter controls: increase IMU alpha and improve camera mounting"

        lastGravity = task.lpGravity
        lastHorizon = task.lpHorizon
    End Sub
End Class