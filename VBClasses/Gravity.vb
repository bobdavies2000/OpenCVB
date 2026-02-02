Imports cv = OpenCvSharp
Namespace VBClasses
    ''' <summary>Compute the gravity vector using the complementary filter: fuse gyro (fast, drifts) with accelerometer (slow, stable).</summary>
    Public Class Gravity_Basics : Inherits TaskParent
        Public options As New Options_Features
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
            dst.Line(task.lpGravity.pE1, task.lpGravity.pE2, white, task.lineWidth, task.lineType)
            dst.Line(task.lpHorizon.pE1, task.lpHorizon.pE2, white, task.lineWidth, task.lineType)
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
                If y0 >= 0 AndAlso y0 <= height Then tList.Add(t0)
                Dim t1 = (width - cx) / dx
                Dim y1 = cy + t1 * dy
                If y1 >= 0 AndAlso y1 <= height Then tList.Add(t1)
            End If
            If Math.Abs(dy) > 0.0001F Then
                Dim t0 = -cy / dy
                Dim x0 = cx + t0 * dx
                If x0 >= 0 AndAlso x0 <= width Then tList.Add(t0)
                Dim t1 = (height - cy) / dy
                Dim x1 = cx + t1 * dx
                If x1 >= 0 AndAlso x1 <= width Then tList.Add(t1)
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

        ''' <summary>Unit gravity in body frame from tilt angles (same convention as IMU_GMatrix: roll=X, pitch=Y, yaw=Z).</summary>
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
            task.verticalizeAngle = y1 * 58.2958

            ' Unit gravity vector in body frame (points down)
            GravityVector = AnglesToGravityVector(task.accRadians)

            ' Line through image center in gravity direction (lpData extends to image edges)
            Dim endpoints = GravityVectorToLineEndpoints(GravityVector, task.workRes.Width, task.workRes.Height)
            task.lpHorizon = New lpData(endpoints.p1, endpoints.p2)
            task.lpGravity = Line_PerpendicularTest.computePerp(task.lpHorizon)

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
            dst.Line(task.lpGravity.pE1, task.lpGravity.pE2, white, task.lineWidth, task.lineType)
            dst.Line(task.lpHorizon.pE1, task.lpHorizon.pE2, white, task.lineWidth, task.lineType)
            'If task.lpGravity Is Nothing Then
            '    dst.Line(task.lines.lpList(0).p1, task.lines.lpList(0).p2, task.highlight, task.lineWidth * 2, task.lineType)
            '    dst.Line(task.lines.lpList(0).pE1, task.lines.lpList(0).pE2, white, task.lineWidth, task.lineType)
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
                vbc.DrawLine(dst2, task.lpGravity.p1, task.lpGravity.p2, task.highlight)
            End If
            task.lpHorizon = Line_PerpendicularTest.computePerp(task.lpGravity)
        End Sub
    End Class






    Public Class NR_Gravity_RGB : Inherits TaskParent
        Dim survey As New BrickPoint_PopulationSurvey
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

            Dim incrX = dst1.Width / task.brickSize
            Dim incrY = dst1.Height / task.brickSize
            For y = 0 To task.brickSize - 1
                For x = 0 To task.brickSize - 1
                    SetTrueText(CStr(survey.results(x, y)), New cv.Point(x * incrX, y * incrY), 2)
                Next
            Next
        End Sub
    End Class






    Public Class NR_Gravity_BrickRotate : Inherits TaskParent
        Dim survey As New BrickPoint_PopulationSurvey
        Public Sub New()
            If task.bricks Is Nothing Then task.bricks = New Brick_Basics
            desc = "Rotate the grid point using the offset from gravity."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim angle = Math.Abs(task.verticalizeAngle)
            Static rotateAngle As Double = -angle
            Static rotateCenter As cv.Point2f = New cv.Point2f(dst2.Width / 2, dst2.Height / 2)

            rotateAngle += 0.1
            If rotateAngle >= angle Then rotateAngle = -angle

            dst1 = src
            For Each gr In task.bricks.brickList
                Dim pt = New cv.Point(gr.mm.maxLoc.X + gr.rect.X, gr.mm.maxLoc.Y + gr.rect.Y)
                If pt.Y = gr.rect.Y Then
                    DrawCircle(dst1, pt)
                End If
            Next

            Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
            dst3 = dst1.WarpAffine(M, dst1.Size(), cv.InterpolationFlags.Nearest)

            survey.Run(dst3)
            dst2 = survey.dst2

            Dim incrX = dst1.Width / task.brickSize
            Dim incrY = dst1.Height / task.brickSize
            For y = 0 To task.brickSize - 1
                For x = 0 To task.brickSize - 1
                    SetTrueText(CStr(survey.results(x, y)), New cv.Point(x * incrX, y * incrY), 2)
                Next
            Next
        End Sub
    End Class





    Public Class NR_Gravity_BasicsOld : Inherits TaskParent
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
                DrawCircle(dst2, pt, task.DotSize, white)
            Next

            vbc.DrawLine(dst2, task.lpGravity.p1, task.lpGravity.p2, white)
            vbc.DrawLine(dst3, task.lpGravity.p1, task.lpGravity.p2, white)
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
                task.lpGravity = New lpData(lp.pE1, lp.pE2)
                If standaloneTest() Or autoDisplay Then displayResults(p1, p2)
            End If

            task.lpHorizon = Line_PerpendicularTest.computePerp(task.lpGravity)
            SetTrueText(strOut, 3)
        End Sub
    End Class





    Public Class NR_Gravity_BasicsOriginal : Inherits TaskParent
        Public vec As New lpData
        Dim options As New Options_History
        Public Sub New()
            dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
            desc = "Search for the transition from positive to negative to find the gravity vector."
        End Sub
        Public Shared Function PrepareDepthInput(index As Integer) As cv.Mat
            If task.gOptions.gravityPointCloud.Checked Then Return task.pcSplit(index) ' already oriented to gravity

            ' rebuild the pointcloud so it is oriented to gravity.
            Dim pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat.Reshape(3, task.pointCloud.Rows)
            Dim split = pc.Split()
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
                        If ptX.Count >= task.frameHistoryCount Then
                            Return New cv.Point2f(ptX.Average, ptY.Average)
                        End If
                    End If
                Next
            Next
            Return New cv.Point
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Type <> cv.MatType.CV_32F Then dst0 = task.pcSplit(0) Else dst0 = src

            Dim p1 = findTransition(0, dst0.Height - 1, 1)
            Dim p2 = findTransition(dst0.Height - 1, 0, -1)
            Dim lp = New lpData(p1, p2)
            vec = New lpData(lp.pE1, lp.pE2)

            If p1.X >= 1 Then
                strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf + "      val =  " +
                      Format(dst0.Get(Of Single)(p1.Y, p1.X)) + vbCrLf + "lastVal = " + Format(dst0.Get(Of Single)(p1.Y, p1.X - 1))
            End If
            SetTrueText(strOut, 3)

            If standaloneTest() Then
                dst2.SetTo(0)
                vbc.DrawLine(dst2, vec.p1, vec.p2, 255)
            End If
        End Sub
    End Class





    'Module Gravity
    '    ' ==============================================================================
    '    ' VB.NET Math Structures: Quaternion and Vector3
    '    ' ==============================================================================

    '    <Serializable()>
    '    Public Structure Vector3
    '        Public X As Single
    '        Public Y As Single
    '        Public Z As Single

    '        Public Sub New(x As Single, y As Single, z As Single)
    '            Me.X = x
    '            Me.Y = y
    '            Me.Z = z
    '        End Sub

    '        Public Function Length() As Single
    '            Return CSng(Math.Sqrt(X * X + Y * Y + Z * Z))
    '        End Function

    '        Public Function Normalize() As Vector3
    '            Dim len As Single = Me.Length()
    '            If len = 0 Then Return New Vector3(0, 0, 0)
    '            Return New Vector3(X / len, Y / len, Z / len)
    '        End Function

    '        Public Shared Operator +(v1 As Vector3, v2 As Vector3) As Vector3
    '            Return New Vector3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z)
    '        End Operator

    '        Public Shared Operator -(v1 As Vector3, v2 As Vector3) As Vector3
    '            Return New Vector3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z)
    '        End Operator

    '        Public Shared Operator *(v As Vector3, scalar As Single) As Vector3
    '            Return New Vector3(v.X * scalar, v.Y * scalar, v.Z * scalar)
    '        End Operator

    '        Public Shared Operator *(scalar As Single, v As Vector3) As Vector3
    '            Return New Vector3(v.X * scalar, v.Y * scalar, v.Z * scalar)
    '        End Operator

    '        Public Shared Function Cross(v1 As Vector3, v2 As Vector3) As Vector3
    '            Return New Vector3(
    '                        v1.Y * v2.Z - v1.Z * v2.Y,
    '                        v1.Z * v2.X - v1.X * v2.Z,
    '                        v1.X * v2.Y - v1.Y * v2.X
    '                    )
    '        End Function

    '        Public Shared Function Dot(v1 As Vector3, v2 As Vector3) As Single
    '            Return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z
    '        End Function

    '        Public Overrides Function ToString() As String
    '            Return $"({X:F3}, {Y:F3}, {Z:F3})"
    '        End Function
    '    End Structure

    '    <Serializable()>
    '    Public Structure Quaternion
    '        Public W As Single
    '        Public X As Single
    '        Public Y As Single
    '        Public Z As Single

    '        Public Sub New(w As Single, x As Single, y As Single, z As Single)
    '            Me.W = w
    '            Me.X = x
    '            Me.Y = y
    '            Me.Z = z
    '        End Sub

    '        Public Shared ReadOnly Property Identity As Quaternion
    '            Get
    '                Return New Quaternion(1.0F, 0.0F, 0.0F, 0.0F)
    '            End Get
    '        End Property

    '        Public Function Normalize() As Quaternion
    '            Dim mag As Single = CSng(Math.Sqrt(W * W + X * X + Y * Y + Z * Z))
    '            If mag = 0 Then Return New Quaternion(0, 0, 0, 0) ' Or Identity
    '            Return New Quaternion(W / mag, X / mag, Y / mag, Z / mag)
    '        End Function

    '        Public Function Conjugate() As Quaternion
    '            Return New Quaternion(W, -X, -Y, -Z)
    '        End Function

    '        ' Quaternion multiplication (q1 * q2)
    '        Public Shared Operator *(q1 As Quaternion, q2 As Quaternion) As Quaternion
    '            Return New Quaternion(
    '                    q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z,
    '                        q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y,
    '                        q1.W * q2.Y - q1.X * q2.Z + q1.Y * q2.W + q1.Z * q2.X,
    '                        q1.W * q2.Z + q1.X * q2.Y - q1.Y * q2.X + q1.Z * q2.W
    '                    )
    '        End Operator

    '        ' Rotate a Vector3 by a Quaternion (q * v * q_conjugate)
    '        Public Function Rotate(v As Vector3) As Vector3
    '            Dim vq As New Quaternion(0, v.X, v.Y, v.Z)
    '            Dim rotatedVq As Quaternion = Me * vq * Me.Conjugate()
    '            Return New Vector3(rotatedVq.X, rotatedVq.Y, rotatedVq.Z)
    '        End Function

    '        Public Overrides Function ToString() As String
    '            Return $"({W:F3}, {X:F3}, {Y:F3}, {Z:F3})"
    '        End Function
    '    End Structure

    '    ' ==============================================================================
    '    ' MadgwickAHRS Filter Implementation
    '    ' ==============================================================================

    '    Public Class MadgwickAHRS
    '        Private _q As Quaternion ' Quaternion (w, x, y, z)
    '        Private _beta As Single ' Algorithm gain beta
    '        Private _sampleFreq As Single ' Sample frequency in Hz

    '        ''' <summary>
    '        ''' Initializes the MadgwickAHRS filter.
    '        ''' </summary>
    '        ''' <param name="sampleFrequency">The frequency at which IMU data is sampled (Hz).</param>
    '        ''' <param name="beta">The algorithm gain beta (default 0.1 for typical use, higher for faster convergence/more noise).</param>
    '        Public Sub New(sampleFrequency As Single, beta As Single)
    '            If sampleFrequency <= 0 Then Throw New ArgumentOutOfRangeException("sampleFrequency must be greater than 0.")
    '            If beta < 0 Then Throw New ArgumentOutOfRangeException("beta cannot be negative.")

    '            _sampleFreq = sampleFrequency
    '            _beta = beta
    '            _q = Quaternion.Identity ' Initialize orientation to straight up
    '        End Sub

    '        ''' <summary>
    '        ''' Updates the filter with new IMU data.
    '        ''' Call this at a consistent rate defined by sampleFrequency.
    '        ''' </summary>
    '        ''' <param name="gx">Gyroscope X-axis reading (radians/sec).</param>
    '        ''' <param name="gy">Gyroscope Y-axis reading (radians/sec).</param>
    '        ''' <param name="gz">Gyroscope Z-axis reading (radians/sec).</param>
    '        ''' <param name="ax">Accelerometer X-axis reading (g's or m/s^2).</param>
    '        ''' <param name="ay">Accelerometer Y-axis reading (g's or m/s^2).</param>
    '        ''' <param name="az">Accelerometer Z-axis reading (g's or m/s^2).</param>
    '        Public Sub Update(gx As Single, gy As Single, gz As Single, ax As Single, ay As Single, az As Single)
    '            ' Local variables for readability
    '            Dim q1 As Single = _q.W
    '            Dim q2 As Single = _q.X
    '            Dim q3 As Single = _q.Y
    '            Dim q4 As Single = _q.Z

    '            Dim norm As Single
    '            Dim halfvx As Single, halfvy As Single, halfvz As Single
    '            Dim halfex As Single, halfey As Single, halfez As Single

    '            Dim deltaT As Single = 1.0F / _sampleFreq

    '            ' Normalize accelerometer measurement
    '            norm = CSng(Math.Sqrt(ax * ax + ay * ay + az * az))
    '            If norm = 0 Then Return ' Handle NaN

    '            ax /= norm
    '            ay /= norm
    '            az /= norm

    '            ' Estimated direction of gravity and flux (v and w in Madgwick's paper)
    '            ' Calculate quaternion product with acceleration (0, ax, ay, az)
    '            ' (quaternion equivalent of multiplying by normalized accelerometer)
    '            halfvx = 2.0F * (q2 * q4 - q1 * q3)
    '            halfvy = 2.0F * (q1 * q2 + q3 * q4)
    '            halfvz = 2.0F * (q1 * q1 - 0.5F + q4 * q4) ' Simplified from 2 * (q1*q1 + q4*q4 - 0.5)

    '            ' Error is sum of cross product between estimated and measured direction of gravity
    '            halfex = (ay * halfvz - az * halfvy)
    '            halfey = (az * halfvx - ax * halfvz)
    '            halfez = (ax * halfvy - ay * halfvx)

    '            ' Apply proportional feedback (gradient descent)
    '            Dim Fx_dot As Single = _beta * halfex
    '            Dim Fy_dot As Single = _beta * halfey
    '            Dim Fz_dot As Single = _beta * halfez

    '            ' Integrate quaternion rate and normalize
    '            Dim qDot1 As Single = (-q2 * gx - q3 * gy - q4 * gz) * 0.5F + Fx_dot
    '            Dim qDot2 As Single = (q1 * gx + q3 * gz - q4 * gy) * 0.5F + Fy_dot
    '            Dim qDot3 As Single = (q1 * gy - q2 * gz + q4 * gx) * 0.5F + Fz_dot
    '            Dim qDot4 As Single = (q1 * gz + q2 * gy - q3 * gx) * 0.5F

    '            ' Update quaternion using Euler integration
    '            q1 += qDot1 * deltaT
    '            q2 += qDot2 * deltaT
    '            q3 += qDot3 * deltaT
    '            q4 += qDot4 * deltaT

    '            _q = New Quaternion(q1, q2, q3, q4).Normalize()
    '        End Sub

    '        ''' <summary>
    '        ''' Gets the current estimated orientation as a Quaternion.
    '        ''' </summary>
    '        Public ReadOnly Property Orientation As Quaternion
    '            Get
    '                Return _q
    '            End Get
    '        End Property

    '        ''' <summary>
    '        ''' Computes the gravity vector in the IMU's body frame based on the current orientation.
    '        ''' Assumes standard Earth-fixed frame where +Z is down (or -Z is up).
    '        ''' </summary>
    '        ''' <param name="gravityMagnitude">Magnitude of gravity, e.g., 9.81 m/s^2 or 1.0 g.</param>
    '        ''' <returns>The gravity vector in the IMU's body frame.</returns>
    '        Public Function GetGravityVector(gravityMagnitude As Single) As Vector3
    '            ' Gravity vector in Earth frame (assuming NED: X-North, Y-East, Z-Down)
    '            ' If your Earth frame Z is UP, use New Vector3(0, 0, -gravityMagnitude)
    '            Dim g_earth_ned As New Vector3(0, 0, gravityMagnitude)

    '            ' To rotate from Earth frame to Body frame, we apply the inverse rotation (conjugate of filter quaternion)
    '            ' (q_body_to_earth)^-1 * g_earth_vector * (q_body_to_earth)
    '            ' which is q_earth_to_body * g_earth_vector * q_body_to_earth
    '            ' Or simply rotate the (0,0,1) vector by the estimated quaternion, then scale.

    '            ' A more direct way is to transform the known gravity vector (0,0,1) from the
    '            ' world frame into the body frame using the rotation matrix derived from the quaternion.
    '            ' However, since the quaternion represents rotation from Earth to Body,
    '            ' we can rotate the (0,0,1) down vector.

    '            ' Gravity vector in Earth frame (world down direction, relative to IMU's coordinate system alignment)
    '            ' A common convention for the IMU's accelerometer is that when flat and stationary
    '            ' with Z-axis pointing UP, it reads (0,0,-1g). If Z-axis points DOWN, it reads (0,0,1g).
    '            ' The Madgwick filter outputs orientation from world to body.
    '            ' So, if world Z is down (0,0,1) and IMU Z is down when flat,
    '            ' gravity vector will be (0,0,1) rotated by the quaternion.

    '            ' Gravity direction vector in body frame derived from quaternion components
    '            ' (This is the -ve of the 'down' vector in the body frame)
    '            Dim gw As Single = _q.W
    '            Dim gx As Single = _q.X
    '            Dim gy As Single = _q.Y
    '            Dim gz As Single = _q.Z

    '            Dim gX_body As Single = 2 * (gx * gz - gw * gy)
    '            Dim gY_body As Single = 2 * (gw * gx + gy * gz)
    '            Dim gZ_body As Single = gw * gw - gx * gx - gy * gy + gz * gz

    '            ' This vector (gX_body, gY_body, gZ_body) points upwards relative to gravity.
    '            ' To get the gravity vector, you often want the one pointing downwards.
    '            Return New Vector3(-gX_body * gravityMagnitude, -gY_body * gravityMagnitude, -gZ_body * gravityMagnitude)
    '        End Function
    '    End Class
    'End Module


    'Imports System
    'Imports System.Threading
    'Imports System.Diagnostics ' For Stopwatch

    'Module MainModule

    '    Sub Main()
    '        Console.WriteLine("MadgwickAHRS Sensor Fusion Demo (VB.NET)")
    '        Console.WriteLine("======================================")

    '        ' --- IMU Parameters ---
    '        Const SampleFreq As Single = 100.0F ' Hz (e.g., IMU updates 100 times per second)
    '        Const Beta As Single = 0.1F      ' Madgwick filter gain (tune this)
    '        Const G_MAGNITUDE As Single = 9.81F ' Magnitude of gravity (m/s^2)

    '        Dim madgwickFilter As New MadgwickAHRS(SampleFreq, Beta)

    '        ' --- Simulate IMU Data (Conceptual - replace with real data) ---
    '        ' We'll simulate 3 states:
    '        ' 1. Stationary, Z-axis up (initial)
    '        ' 2. Rotated 90 degrees around X-axis (Y-axis up)
    '        ' 3. Rotated 90 degrees around Y-axis (X-axis up)
    '        ' 4. Moderate linear acceleration in X
    '        ' 5. Back to stationary

    '        Console.WriteLine("--- Initializing (Stationary, Z-axis Up) ---")
    '        ' Simulate stationary, Z-axis pointing up (accelerometer reads ~0,0,-1g or 0,0,-9.81 m/s^2)
    '        ' Gyro is 0 when stationary
    '        Dim simAx1 As Single = 0.0F
    '        Dim simAy1 As Single = 0.0F
    '        Dim simAz1 As Single = -G_MAGNITUDE ' Z-axis pointing 'up', gravity pulling 'down'

    '        Dim simGx As Single = 0.0F
    '        Dim simGy As Single = 0.0F
    '        Dim simGz As Single = 0.0F

    '        Dim stopwatch As New Stopwatch()
    '        stopwatch.Start()

    '        Dim sampleCount As Integer = 0
    '        While sampleCount < SampleFreq * 5 ' Simulate 5 seconds
    '            ' Update filter
    '            madgwickFilter.Update(simGx, simGy, simGz, simAx1, simAy1, simAz1)

    '            ' Print current orientation and gravity vector
    '            If sampleCount Mod CInt(SampleFreq / 10) = 0 Then ' Print ~10 times per second
    '                Console.WriteLine($"Sample: {sampleCount / SampleFreq:F1}s | Ori: {madgwickFilter.Orientation} | Grav: {madgwickFilter.GetGravityVector(G_MAGNITUDE)}")
    '            End If

    '            sampleCount += 1
    '            Thread.Sleep(CInt(1000.0F / SampleFreq)) ' Simulate real-time delay
    '        End While

    '        Console.WriteLine(Environment.NewLine & "--- Simulating 90-degree Rotation Around X-axis ---")
    '        ' Simulate a rotation around X-axis (e.g., pitch up)
    '        ' Accel still reads gravity, but its components change due to orientation
    '        ' (Stationary, Y-axis pointing up: ~0, 9.81, 0) - or - (0, 1g, 0)
    '        Dim simAx2 As Single = 0.0F
    '        Dim simAy2 As Single = G_MAGNITUDE ' Now Y-axis points towards Earth's up
    '        Dim simAz2 As Single = 0.0F

    '        ' Simulate a sustained angular velocity for rotation
    '        Dim rotationGx As Single = Math.PI / 2.0F ' 90 deg/sec around X-axis (radians)
    '        Dim rotationGy As Single = 0.0F
    '        Dim rotationGz As Single = 0.0F

    '        For i As Integer = 0 To CInt(SampleFreq * 2) ' Simulate 2 seconds of rotation
    '            madgwickFilter.Update(rotationGx, rotationGy, rotationGz, simAx2, simAy2, simAz2)
    '            If i Mod CInt(SampleFreq / 10) = 0 Then
    '                Console.WriteLine($"Sample: {i / SampleFreq:F1}s | Ori: {madgwickFilter.Orientation} | Grav: {madgwickFilter.GetGravityVector(G_MAGNITUDE)}")
    '            End If
    '            Thread.Sleep(CInt(1000.0F / SampleFreq))
    '        Next

    '        Console.WriteLine(Environment.NewLine & "--- Simulating Moderate Linear Acceleration in X ---")
    '        ' Now, simulate a period where there IS linear acceleration (e.g., moving forward fast)
    '        Dim linearAccelX As Single = 5.0F ' 5 m/s^2 linear acceleration in X
    '        For i As Integer = 0 To CInt(SampleFreq * 2) ' Simulate 2 seconds of acceleration
    '            ' Accel reads gravity component PLUS linear acceleration
    '            madgwickFilter.Update(0, 0, 0, simAx1 + linearAccelX, simAy1, simAz1) ' Add linear accel to initial state
    '            If i Mod CInt(SampleFreq / 10) = 0 Then
    '                Console.WriteLine($"Sample: {i / SampleFreq:F1}s | Ori: {madgwickFilter.Orientation} | Grav: {madgwickFilter.GetGravityVector(G_MAGNITUDE)}")
    '            End If
    '            Thread.Sleep(CInt(1000.0F / SampleFreq))
    '        Next

    '        Console.WriteLine(Environment.NewLine & "--- Back to Stationary (Recalibrating) ---")
    '        For i As Integer = 0 To CInt(SampleFreq * 3) ' Simulate 3 seconds to stabilize
    '            madgwickFilter.Update(0, 0, 0, simAx1, simAy1, simAz1)
    '            If i Mod CInt(SampleFreq / 10) = 0 Then
    '                Console.WriteLine($"Sample: {i / SampleFreq:F1}s | Ori: {madgwickFilter.Orientation} | Grav: {madgwickFilter.GetGravityVector(G_MAGNITUDE)}")
    '            End If
    '            Thread.Sleep(CInt(1000.0F / SampleFreq))
    '        Next


    '        stopwatch.Stop()
    '        Console.WriteLine(Environment.NewLine & $"Demo finished in {stopwatch.Elapsed.TotalSeconds:F2} seconds.")
    '        Console.WriteLine("Press any key to exit.")
    '        Console.ReadKey()

    '    End Sub

    'End Module



End Namespace