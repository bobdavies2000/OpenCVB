Imports cv = OpenCvSharp
Imports System.Windows.Forms
' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Namespace VBClasses
    Public Class IMU_Basics : Inherits TaskParent
        Dim lastTimeStamp As Double
        Public Sub New()
            desc = "Read and display the IMU data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                labels(2) = "IMU_Basics gets the IMU data on every iteration."
            End If

            Dim gyroAngle As cv.Point3f
            If taskA.optionsChanged Then
                lastTimeStamp = taskA.IMU_TimeStamp
            Else
                gyroAngle = taskA.IMU_AngularVelocity
                Dim dt_gyro = (taskA.IMU_TimeStamp - lastTimeStamp) / 1000
                If taskA.settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then
                    dt_gyro /= 1000 ' different units in the timestamp?
                End If
                gyroAngle = gyroAngle * dt_gyro
                taskA.theta += New cv.Point3f(-gyroAngle.Z, -gyroAngle.Y, gyroAngle.X)
                lastTimeStamp = taskA.IMU_TimeStamp
            End If

            ' NOTE: Initialize the angle around the y-axis to zero.
            Dim g = taskA.IMU_Acceleration
            taskA.accRadians = New cv.Point3f(Math.Atan2(g.X, Math.Sqrt(g.Y * g.Y + g.Z * g.Z)),
                                             Math.Abs(Math.Atan2(g.X, g.Y)), Math.Atan2(g.Y, g.Z))
            If taskA.optionsChanged Then
                taskA.theta = taskA.accRadians
            Else
                ' Apply the Complementary Filter:
                '  - high-pass filter = theta * alpha: allows short-duration signals to pass while filtering steady signals (trying to cancel drift)
                '  - low-pass filter = accel * (1 - alpha): lets the long-term changes through, filtering out short term fluctuations
                taskA.theta.X = taskA.theta.X * taskA.IMU_AlphaFilter + taskA.accRadians.X * (1 - taskA.IMU_AlphaFilter)
                taskA.theta.Y = taskA.accRadians.Y
                taskA.theta.Z = taskA.theta.Z * taskA.IMU_AlphaFilter + taskA.accRadians.Z * (1 - taskA.IMU_AlphaFilter)
            End If

            Dim x1 = -(90 + taskA.accRadians.X * 57.2958)
            Dim x2 = -(90 + taskA.theta.X * 57.2958)
            Dim y1 = taskA.accRadians.Y - cv.Cv2.PI
            If taskA.accRadians.X < 0 Then y1 *= -1
            taskA.verticalizeAngle = y1 * 58.2958
            strOut = "Angles in degree to gravity (before velocity filter)" + vbCrLf +
                     Format(x1, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(taskA.accRadians.Z * 57.2958, fmt1) + vbCrLf +
                     "Velocity-Filtered Angles to gravity in degrees" + vbCrLf +
                     Format(x2, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(taskA.theta.Z * 57.2958, fmt1) + vbCrLf
            strOut += "cx = " + Format(taskA.gravityMatrix.cx, fmt3) + " sx = " + Format(taskA.gravityMatrix.sx, fmt3) + vbCrLf +
                      "cy = " + Format(taskA.gravityMatrix.cy, fmt3) + " sy = " + Format(taskA.gravityMatrix.sy, fmt3) + vbCrLf +
                      "cz = " + Format(taskA.gravityMatrix.cz, fmt3) + " sz = " + Format(taskA.gravityMatrix.sz, fmt3)

            taskA.accRadians = taskA.theta
            If taskA.accRadians.Y > cv.Cv2.PI / 2 Then taskA.accRadians.Y -= cv.Cv2.PI / 2
            taskA.accRadians.Z += cv.Cv2.PI / 2

            SetTrueText(strOut)
        End Sub
    End Class


    ''' <summary>Compute the gravity vector using the complementary filter: fuse gyro (fast, drifts) with accelerometer (slow, stable).</summary>
    Public Class IMU_GravityComplementary : Inherits TaskParent
        Dim lastTimeStamp As Double
        Dim options As New Options_IMU

        ''' <summary>Unit gravity vector in body/sensor frame (points down).</summary>
        Public GravityVector As New cv.Point3f(0, 0, -1)

        ''' <summary>Line through image center in the direction of gravity (extends to image edges).</summary>
        Public lpGravity As lpData

        Public Sub New()
            desc = "Compute the gravity vector using the complementary filter: fuse gyroscope (fast, drifts) with accelerometer (slow, stable)."
            labels(2) = "Complementary-filter gravity: angles and unit gravity vector"
        End Sub

        ''' <summary>Compute two image points for the line through (cx,cy) in direction of gravity projection (gx,gy), extended to rect [0,w] x [0,h].</summary>
        Public Shared Function GravityVectorToLineEndpoints(gravityVec As cv.Point3f, width As Integer, height As Integer) As (p1 As cv.Point2f, p2 As cv.Point2f)
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
        Public Shared Function AnglesToGravityVector(accRadians As cv.Point3f) As cv.Point3f
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
            options.Run()

            Dim gyro = taskA.IMU_AngularVelocity
            If taskA.optionsChanged Then
                lastTimeStamp = taskA.IMU_TimeStamp
            Else
                Dim dt = (taskA.IMU_TimeStamp - lastTimeStamp) / 1000.0
                If taskA.Settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then dt /= 1000.0
                dt = Math.Max(0.000001, Math.Min(1.0, dt))
                taskA.theta += New cv.Point3f(-gyro.Z * dt, -gyro.Y * dt, gyro.X * dt)
                lastTimeStamp = taskA.IMU_TimeStamp
            End If

            ' Tilt angles from accelerometer (low-pass source)
            Dim g = taskA.IMU_Acceleration
            taskA.accRadians = New cv.Point3f(
                CSng(Math.Atan2(g.X, Math.Sqrt(g.Y * g.Y + g.Z * g.Z))),
                CSng(Math.Abs(Math.Atan2(g.X, g.Y))),
                CSng(Math.Atan2(g.Y, g.Z)))

            ' Complementary filter: angle = alpha * (gyro-integrated) + (1-alpha) * (accel-derived)
            If taskA.optionsChanged Then
                taskA.theta = taskA.accRadians
            Else
                Dim a = taskA.IMU_AlphaFilter
                taskA.theta.X = a * taskA.theta.X + (1.0F - a) * taskA.accRadians.X
                taskA.theta.Y = taskA.accRadians.Y
                taskA.theta.Z = a * taskA.theta.Z + (1.0F - a) * taskA.accRadians.Z
            End If

            taskA.accRadians = taskA.theta
            If taskA.accRadians.Y > cv.Cv2.PI / 2 Then taskA.accRadians.Y -= cv.Cv2.PI / 2
            taskA.accRadians.Z += cv.Cv2.PI / 2

            Dim y1 = taskA.accRadians.Y - cv.Cv2.PI
            If taskA.accRadians.X < 0 Then y1 *= -1
            taskA.verticalizeAngle = y1 * 58.2958

            ' Unit gravity vector in body frame (points down)
            GravityVector = AnglesToGravityVector(taskA.accRadians)

            ' Line through image center in gravity direction (lpData extends to image edges)
            Dim endpoints = GravityVectorToLineEndpoints(GravityVector, taskA.workRes.Width, taskA.workRes.Height)
            lpGravity = New lpData(endpoints.p1, endpoints.p2)
            taskA.lpGravity = lpGravity

            strOut = "Complementary filter gravity" + vbCrLf +
                     "Tilt (rad): X=" + Format(taskA.accRadians.X, fmt3) + " Y=" + Format(taskA.accRadians.Y, fmt3) + " Z=" + Format(taskA.accRadians.Z, fmt3) + vbCrLf +
                     "Gravity unit vector (body): " + Format(GravityVector.X, fmt3) + ", " + Format(GravityVector.Y, fmt3) + ", " + Format(GravityVector.Z, fmt3)
            SetTrueText(strOut)
        End Sub
    End Class


    ' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
    Public Class NR_IMU_BasicsKalman : Inherits TaskParent
        Dim lastTimeStamp As Double
        Public Sub New()
            taskA.kalman = New Kalman_Basics
            desc = "Read and display the IMU coordinates"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gyroAngle As cv.Point3f
            If taskA.optionsChanged Then
                lastTimeStamp = taskA.IMU_TimeStamp
            Else
                gyroAngle = taskA.IMU_AngularVelocity
                Dim dt_gyro = (taskA.IMU_TimeStamp - lastTimeStamp) / 1000
                If taskA.Settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then
                    dt_gyro /= 1000 ' different units in the timestamp?
                End If
                gyroAngle = gyroAngle * dt_gyro
                lastTimeStamp = taskA.IMU_TimeStamp
            End If

            ' NOTE: Initialize the angle around the y-axis to zero.
            Dim g = taskA.IMU_Acceleration
            taskA.accRadians = New cv.Point3f(Math.Atan2(g.X, Math.Sqrt(g.Y * g.Y + g.Z * g.Z)),
                                         Math.Abs(Math.Atan2(g.X, g.Y)), Math.Atan2(g.Y, g.Z))

            taskA.kalman.kInput = {taskA.accRadians.X, taskA.accRadians.Y, taskA.accRadians.Z}
            taskA.kalman.Run(Nothing)

            taskA.accRadians = New cv.Point3f(taskA.kalman.kOutput(0), taskA.kalman.kOutput(1), taskA.kalman.kOutput(2))

            Dim x1 = -(90 + taskA.accRadians.X * 57.2958)
            Dim y1 = taskA.accRadians.Y - cv.Cv2.PI
            If taskA.accRadians.X < 0 Then y1 *= -1
            strOut = "Angles in degree to gravity (before velocity filter)" + vbCrLf +
                 Format(x1, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(taskA.accRadians.Z * 57.2958, fmt1) + vbCrLf
            strOut += "cx = " + Format(taskA.gravityMatrix.cx, fmt3) + " sx = " + Format(taskA.gravityMatrix.sx, fmt3) + vbCrLf +
                  "cy = " + Format(taskA.gravityMatrix.cy, fmt3) + " sy = " + Format(taskA.gravityMatrix.sy, fmt3) + vbCrLf +
                  "cz = " + Format(taskA.gravityMatrix.cz, fmt3) + " sz = " + Format(taskA.gravityMatrix.sz, fmt3)
            If taskA.accRadians.Y > cv.Cv2.PI / 2 Then taskA.accRadians.Y -= cv.Cv2.PI / 2
            taskA.accRadians.Z += cv.Cv2.PI / 2

            SetTrueText(strOut)
        End Sub
    End Class







    ' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
    Public Class NR_IMU_BasicsWithOptions : Inherits TaskParent
        Dim lastTimeStamp As Double
        Dim options As New Options_IMU
        Public Sub New()
            desc = "Read and display the IMU coordinates"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim gyroAngle As cv.Point3f
            If taskA.optionsChanged Then
                lastTimeStamp = taskA.IMU_TimeStamp
            Else
                gyroAngle = taskA.IMU_AngularVelocity
                Dim dt_gyro = (taskA.IMU_TimeStamp - lastTimeStamp) / 1000
                If taskA.Settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then
                    dt_gyro /= 1000 ' different units in the timestamp?
                End If
                gyroAngle = gyroAngle * dt_gyro
                taskA.theta += New cv.Point3f(-gyroAngle.Z, -gyroAngle.Y, gyroAngle.X)
                lastTimeStamp = taskA.IMU_TimeStamp
            End If

            ' NOTE: Initialize the angle around the y-axis to zero.
            Dim g = taskA.IMU_Acceleration
            taskA.accRadians = New cv.Point3f(Math.Atan2(g.X, Math.Sqrt(g.Y * g.Y + g.Z * g.Z)),
                                         Math.Abs(Math.Atan2(g.X, g.Y)), Math.Atan2(g.Y, g.Z))

            If taskA.optionsChanged Then
                taskA.theta = taskA.accRadians
            Else
                ' Apply the Complementary Filter:
                '  - high-pass filter = theta * alpha: allows short-duration signals to pass while filtering steady signals (trying to cancel drift)
                '  - low-pass filter = accel * (1 - alpha): lets the long-term changes through, filtering out short term fluctuations
                taskA.theta.X = taskA.theta.X * taskA.IMU_AlphaFilter + taskA.accRadians.X * (1 - taskA.IMU_AlphaFilter)
                taskA.theta.Y = taskA.accRadians.Y
                taskA.theta.Z = taskA.theta.Z * taskA.IMU_AlphaFilter + taskA.accRadians.Z * (1 - taskA.IMU_AlphaFilter)
            End If

            Dim x1 = -(90 + taskA.accRadians.X * 57.2958)
            Dim x2 = -(90 + taskA.theta.X * 57.2958)
            Dim y1 = taskA.accRadians.Y - cv.Cv2.PI
            If taskA.accRadians.X < 0 Then y1 *= -1
            strOut = "Angles in degree to gravity (before velocity filter)" + vbCrLf +
                 Format(x1, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(taskA.accRadians.Z * 57.2958, fmt1) + vbCrLf +
                 "Velocity-Filtered Angles to gravity in degrees" + vbCrLf +
                 Format(x2, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(taskA.theta.Z * 57.2958, fmt1) + vbCrLf
            SetTrueText(strOut)

            taskA.accRadians = taskA.theta
            If taskA.accRadians.Y > cv.Cv2.PI / 2 Then taskA.accRadians.Y -= cv.Cv2.PI / 2
            taskA.accRadians.Z += cv.Cv2.PI / 2

            SetTrueText(strOut)
        End Sub
    End Class






    Public Class IMU_Vertical : Inherits TaskParent
        Public stableTest As Boolean
        Public stableStr As String
        Dim angleXValue As New List(Of Single)
        Dim angleYValue As New List(Of Single)
        Dim stableCount As New List(Of Integer)
        Dim lastAngleX As Single, lastAngleY As Single
        Public Sub New()
            desc = "Use the IMU angular velocity to determine if the camera is moving or stable."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            angleXValue.Add(taskA.accRadians.X)
            angleYValue.Add(taskA.accRadians.Y)

            strOut = "IMU X" + vbTab + "IMU Y" + vbTab + "IMU Z" + vbCrLf
            strOut += Format(taskA.accRadians.X * 57.2958, fmt3) + vbTab + Format(taskA.accRadians.Y * 57.2958, fmt3) + vbTab +
                  Format(taskA.accRadians.Z * 57.2958, fmt3) + vbCrLf
            Dim avgX = angleXValue.Average
            Dim avgY = angleYValue.Average
            If taskA.firstPass Then
                lastAngleX = avgX
                lastAngleY = avgY
            End If
            strOut += "Angle X" + vbTab + "Angle Y" + vbCrLf
            strOut += Format(avgX, fmt3) + vbTab + Format(avgY, fmt3) + vbCrLf

            Dim angle = 90 - avgY * 57.2958
            If avgX < 0 Then angle *= -1
            labels(2) = "stabilizer_Vertical Angle = " + Format(angle, fmt1)

            stableTest = Math.Abs(lastAngleX - avgX) < 0.001 And Math.Abs(lastAngleY - avgY) < 0.01
            stableCount.Add(If(stableTest, 1, 0))
            If taskA.heartBeat Then
                Dim avgStable = stableCount.Average
                stableStr = "IMU stable = " + Format(avgStable, "0.0%") + " of the time"
                stableCount.Clear()
            End If
            SetTrueText(strOut + vbCrLf + stableStr, 2)

            lastAngleX = avgX
            lastAngleY = avgY

            If angleXValue.Count >= taskA.frameHistoryCount Then angleXValue.RemoveAt(0)
            If angleYValue.Count >= taskA.frameHistoryCount Then angleYValue.RemoveAt(0)
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/1247960/3D-graphics-engine-with-basic-math-on-CPU
    Public Class IMU_GMatrix : Inherits TaskParent
        Public cx As Single = 1, sx As Single = 0, cy As Single = 1, sy As Single = 0, cz As Single = 1, sz As Single = 0
        Public gMatrix As cv.Mat
        Public Sub New()
            desc = "Find the angle of tilt for the camera with respect to gravity."
        End Sub
        Public Shared Function gMatrixToStr(gMatrix As cv.Mat) As String
            Dim outStr = "Gravity transform matrix" + vbCrLf
            For i = 0 To gMatrix.Rows - 1
                For j = 0 To gMatrix.Cols - 1
                    outStr += Format(gMatrix.Get(Of Single)(j, i), fmt3) + vbTab
                Next
                outStr += vbCrLf
            Next

            Return outStr
        End Function
        Private Sub buildGmatrix()
            '[cx -sx    0]  [1  0   0 ] 
            '[sx  cx    0]  [0  cz -sz]
            '[0   0     1]  [0  sz  cz]
            Dim gArray As Single(,) = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                                   {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                                   {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}

            gMatrix = cv.Mat.FromPixelData(3, 3, cv.MatType.CV_32F, {
                  {gArray(0, 0) * cy + gArray(0, 1) * 0 + gArray(0, 2) * sy},
                  {gArray(0, 0) * 0 + gArray(0, 1) * 1 + gArray(0, 2) * 0},
                  {gArray(0, 0) * -sy + gArray(0, 1) * 0 + gArray(0, 2) * cy},
                  {gArray(1, 0) * cy + gArray(1, 1) * 0 + gArray(1, 2) * sy},
                  {gArray(1, 0) * 0 + gArray(1, 1) * 1 + gArray(1, 2) * 0},
                  {gArray(1, 0) * -sy + gArray(1, 1) * 0 + gArray(1, 2) * cy},
                  {gArray(2, 0) * cy + gArray(2, 1) * 0 + gArray(2, 2) * sy},
                  {gArray(2, 0) * 0 + gArray(2, 1) * 1 + gArray(2, 2) * 0},
                  {gArray(2, 0) * -sy + gArray(2, 1) * 0 + gArray(2, 2) * cy}})
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                labels(2) = "IMU_GMatrix builds the gMatrix (gravity matrix) on every iteration."
            End If

            '[cos(a) -sin(a)    0]
            '[sin(a)  cos(a)    0]
            '[0       0         1] rotate the point cloud around the x-axis.
            cz = Math.Cos(taskA.accRadians.Z)
            sz = Math.Sin(taskA.accRadians.Z)

            '[1       0         0      ] rotate the point cloud around the z-axis.
            '[0       cos(a)    -sin(a)]
            '[0       sin(a)    cos(a) ]
            cx = Math.Cos(taskA.accRadians.X)
            sx = Math.Sin(taskA.accRadians.X)

            buildGmatrix()

            Dim g = taskA.IMU_Acceleration
            Dim fmt = fmt3
            strOut = "IMU Acceleration in X-direction = " + vbTab + Format(g.X, fmt) + vbCrLf
            strOut += "IMU Acceleration in Y-direction = " + vbTab + Format(g.Y, fmt) + vbCrLf
            strOut += "IMU Acceleration in Z-direction = " + vbTab + Format(g.Z, fmt) + vbCrLf + vbCrLf
            strOut += vbCrLf + "sqrt (" + vbTab + Format(g.X, fmt) + "*" + Format(g.X, fmt) + vbTab +
                  Format(g.Y, fmt) + "*" + Format(g.Y, fmt) + vbTab +
                  Format(g.Z, fmt) + "*" + Format(g.Z, fmt) + " ) = " + vbTab +
                  Format(Math.Sqrt(g.X * g.X + g.Y * g.Y + g.Z * g.Z), fmt) + vbCrLf +
                  "Should be close to the earth's gravitational constant of 9.807 (or the camera was moving.)"

            strOut += vbCrLf + "Gravity-oriented gMatrix - move camera to test this:" + vbCrLf + gMatrixToStr(gMatrix)
            SetTrueText(strOut)
            taskA.gMatrix = gMatrix
        End Sub
    End Class







    Public Class NR_IMU_Stabilize : Inherits TaskParent
        Public Sub New()
            taskA.kalman = New Kalman_Basics
            ReDim taskA.kalman.kInput(3 - 1)
            desc = "Stabilize IMU acceleration data."
            labels = {"", "", "IMU Stabilize (move camera around)", "Difference from Color Image"}
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim borderCrop = 5
            Dim vert_Border = borderCrop * src.Rows / src.Cols
            Dim dx = taskA.IMU_AngularVelocity.X
            Dim dy = taskA.IMU_AngularVelocity.Y
            Dim dz = taskA.IMU_AngularVelocity.Z
            Dim sx = 1 ' assume no scaling is taking place.
            Dim sy = 1 ' assume no scaling is taking place.

            taskA.kalman.kInput = {dx, dy, dz}
            taskA.kalman.Run(emptyMat)
            dx = taskA.kalman.kOutput(0)
            dy = taskA.kalman.kOutput(1)
            dz = taskA.kalman.kOutput(2)

            Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(dz))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(dz))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(dz))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(dz))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = src.WarpAffine(smoothedMat, src.Size())
            smoothedFrame = smoothedFrame(New cv.Range(borderCrop, smoothedFrame.Rows - borderCrop), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            dst2 = smoothedFrame.Resize(src.Size())
            cv.Cv2.Subtract(src, dst2, dst3)

            Dim Text = "dx = " + Format(dx, fmt2) + vbCrLf + "dy = " + Format(dy, fmt2) + vbCrLf + "dz = " + Format(dz, fmt2)
            SetTrueText(Text, New cv.Point(10, 10), 3)
        End Sub
    End Class






    Public Class IMU_PlotIMUFrameTime : Inherits TaskParent
        Public plot As New Plot_OverTime
        Public CPUInterval As Double
        Public IMUtoCaptureEstimate As Double
        Dim options As New Options_IMUFrameTime
        Dim imuTotalTime As Double
        Dim allZeroCount As Integer
        Public Sub New()
            plot.dst2 = dst3
            plot.maxScale = 40
            plot.minScale = -10
            plot.plotCount = 4

            labels(3) = "IMU (blue) Host (green) Latency est. (red) - all in ms"
            desc = "Use the IMU timestamp to estimate the delay from IMU capture to image capture.  Just an estimate!"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Static IMUanchor As Integer = taskA.IMU_FrameTime Mod 4000000000
            Static histogramIMU(plot.maxScale) As Integer

            ' there can be some errant times at startup.
            If CInt(taskA.IMU_FrameTime) >= histogramIMU.Length Then taskA.IMU_FrameTime = plot.maxScale
            If taskA.IMU_FrameTime < 0 Then taskA.IMU_FrameTime = 0

            imuTotalTime += taskA.IMU_FrameTime
            If imuTotalTime = 0 Then
                allZeroCount += 1
                If allZeroCount > 20 Then
                    SetTrueText("Is IMU present?  No IMU FrameTimes")
                    allZeroCount = Integer.MinValue ' don't show message again.
                End If
                Exit Sub ' if the IMU frametime was 0, then no new IMU data was generated (or it is unsupported!)
            End If

            Dim maxval = Integer.MinValue
            For i = 0 To histogramIMU.Count - 1
                If maxval < histogramIMU(i) Then
                    maxval = histogramIMU(i)
                    IMUanchor = i
                End If
            Next

            Dim imuFrameTime = CInt(taskA.IMU_FrameTime)
            If IMUanchor <> 0 Then imuFrameTime = imuFrameTime Mod IMUanchor
            IMUtoCaptureEstimate = IMUanchor - imuFrameTime + options.minDelayIMU
            If IMUtoCaptureEstimate > IMUanchor Then IMUtoCaptureEstimate -= IMUanchor
            If IMUtoCaptureEstimate < options.minDelayIMU Then IMUtoCaptureEstimate = options.minDelayIMU

            Static sampledIMUFrameTime = taskA.IMU_FrameTime
            If taskA.heartBeat Then sampledIMUFrameTime = taskA.IMU_FrameTime

            histogramIMU(Math.Min(CInt(taskA.IMU_FrameTime), histogramIMU.Length - 1)) += 1

            If standaloneTest() Then
                Dim output = "IMU_TimeStamp (ms) " + Format(taskA.IMU_TimeStamp, "00") + vbCrLf +
                        "CPU TimeStamp (ms) " + Format(taskA.CPU_TimeStamp, "00") + vbCrLf +
                        "IMU Frametime (ms, sampled) " + Format(sampledIMUFrameTime, "000.00") +
                        " IMUanchor = " + Format(IMUanchor, "00") +
                        " latest = " + Format(taskA.IMU_FrameTime, "00.00") + vbCrLf +
                        "IMUtoCapture (ms, sampled, in red) " + Format(IMUtoCaptureEstimate, "00") + vbCrLf + vbCrLf +
                        "IMU Frame Time = Blue" + vbCrLf +
                        "Host Frame Time = Green" + vbCrLf +
                        "IMU Total Delay = Red" + vbCrLf +
                        "IMU Anchor Frame Time = White (IMU Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

                plot.plotData = New cv.Scalar(taskA.IMU_FrameTime, taskA.CPU_FrameTime, IMUtoCaptureEstimate, IMUanchor)
                plot.Run(src)

                If plot.maxScale - plot.minScale > histogramIMU.Count Then ReDim histogramIMU(plot.maxScale - plot.minScale)

                If plot.lastXdelta.Count > options.plotLastX Then
                    For i = 0 To plot.plotCount - 1
                        output += "Last " + CStr(options.plotLastX) + Choose(i + 1, " IMU FrameTime", " Host Frame Time", " IMUtoCapture ms", " IMU Center time") + vbTab
                        For j = plot.lastXdelta.Count - options.plotLastX - 1 To plot.lastXdelta.Count - 1
                            output += Format(plot.lastXdelta(j)(i), "00") + ", "
                        Next
                        output += vbCrLf
                    Next
                End If
                SetTrueText(output)
            End If
        End Sub
    End Class





    Public Class NR_IMU_PlotTotalDelay : Inherits TaskParent
        Dim host As New IMU_PlotHostFrameTimes
        Dim imu As New IMU_PlotIMUFrameTime
        Dim plot As New Plot_OverTime
        Dim kalman As New Kalman_Single
        Public Sub New()
            plot.dst2 = dst3
            plot.maxScale = 50
            plot.minScale = 0
            plot.plotCount = 4

            labels(2) = "Timing data - total (white) right image"
            labels(3) = "IMU (blue) Host (green) Latency est. (red) - all in ms"
            desc = "Estimate time from IMU capture to host processing to allow predicting effect of camera motion."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static countSlider = OptionParent.FindSlider("Number of Plot Values")
            Dim plotLastX = countSlider.Value
            host.Run(src)
            imu.Run(src)
            Dim totaldelay = host.HostInterruptDelayEstimate + imu.IMUtoCaptureEstimate

            kalman.inputReal = totaldelay
            kalman.Run(src)

            Static sampledCPUDelay = host.HostInterruptDelayEstimate
            Static sampledIMUDelay = imu.IMUtoCaptureEstimate
            Static sampledTotalDelay = totaldelay
            Static sampledSmooth = kalman.stateResult
            If taskA.heartBeat Then
                sampledCPUDelay = host.HostInterruptDelayEstimate
                sampledIMUDelay = imu.IMUtoCaptureEstimate
                sampledTotalDelay = totaldelay
                sampledSmooth = kalman.stateResult
            End If

            Dim output = "Estimated host delay (ms, sampled) " + Format(sampledCPUDelay, "00") + vbCrLf +
                     "Estimated IMU delay (ms, sampled) " + Format(sampledIMUDelay, "00") + vbCrLf +
                     "Estimated Total delay (ms, sampled) " + Format(sampledTotalDelay, "00") + vbCrLf +
                     "Estimated Total delay Smoothed (ms, sampled, in White) " + Format(sampledSmooth, "00") + vbCrLf + vbCrLf +
                     "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                     "Green" + vbTab + "Host Frame Time" + vbCrLf +
                     "Red" + vbTab + "Host+IMU Total Delay (latency)" + vbCrLf +
                     "White" + vbTab + "Host+IMU Anchor Frame Time (Host Frame Time that occurs most often)" + vbCrLf + vbCrLf + vbCrLf

            plot.plotData = New cv.Scalar(imu.IMUtoCaptureEstimate, host.HostInterruptDelayEstimate, totaldelay, kalman.stateResult)
            plot.Run(src)

            If plot.lastXdelta.Count > plotLastX Then
                For i = 0 To plot.plotCount - 1
                    output += "Last " + CStr(plotLastX) + Choose(i + 1, " IMU Delay ", " Host Delay", " Total Delay ms", " Smoothed Total") + vbTab
                    For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                        output += Format(plot.lastXdelta(j)(i), "00") + ", "
                    Next
                    output += vbCrLf
                Next
            End If
            SetTrueText(output)
        End Sub
    End Class









    Public Class NR_IMU_VerticalAngles : Inherits TaskParent
        Dim vert As New XO_Line_GCloud
        Public Sub New()
            labels = {"", "", "Highlighted vertical lines", "Line details"}
            desc = "Compare the IMU changes to the angle changes in the vertical lines."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone

            vert.Run(src)

            Dim cells = vert.sortedVerticals
            strOut = "ID" + vbTab + "len3D" + vbTab + "Depth" + vbTab + "Arc X" + vbTab + "Arc Y" + vbTab + "Arc Z" + vbTab + "IMU X" + vbTab + "IMU Y" + vbTab + "IMU Z" + vbCrLf
            dst3.SetTo(0)
            For i = 0 To cells.Count - 1
                Dim gr = cells.ElementAt(i).Value
                strOut += CStr(i) + vbTab + Format(gr.len3D, fmt1) + "m" + vbTab + Format(gr.tc1.depth, fmt1) + "m" + vbTab +
                      Format(gr.arcX, fmt1) + vbTab + Format(gr.arcY, fmt1) + vbTab + Format(gr.arcZ, fmt1) + vbTab
                strOut += Format(taskA.accRadians.X * 57.2958, fmt1) + vbTab + Format(taskA.accRadians.Y * 57.2958, fmt1) + vbTab + Format(taskA.accRadians.Z * 57.2958, fmt1) + vbTab + vbCrLf
                SetTrueText(CStr(i), gr.tc1.center, 2)
                SetTrueText(CStr(i), gr.tc1.center, 3)
                vbc.DrawLine(dst2, gr.tc1.center, gr.tc2.center, taskA.highlight)
                vbc.DrawLine(dst3, gr.tc1.center, gr.tc2.center, white)
            Next
            SetTrueText(strOut, 3)
        End Sub
    End Class








    Public Class NR_IMU_PlotGravityAngles : Inherits TaskParent
        Dim plot As New Plot_OverTimeScalar
        Public Sub New()
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            desc = "Plot the motion of the camera based on the IMU data in degrees"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("ts = " + Format(taskA.IMU_TimeStamp, fmt2) + vbCrLf + "X degrees = " + Format(taskA.accRadians.X * 57.2958, fmt3) + vbCrLf +
                    "Y degrees = " + Format(Math.Abs(taskA.accRadians.Y * 57.2958), fmt3) + vbCrLf + "Z degrees = " + Format(taskA.accRadians.Z * 57.2958, fmt2) + vbCrLf + vbCrLf +
                    "Motion (radians/sec) " + vbCrLf + "pitch = " + Format(taskA.IMU_AngularVelocity.X, fmt2) + vbCrLf +
                    "Yaw = " + Format(taskA.IMU_AngularVelocity.Y, fmt2) + vbCrLf + " Roll = " + Format(taskA.IMU_AngularVelocity.Z, fmt2), 1)

            plot.plotData = New cv.Scalar(taskA.accRadians.X * 57.2958, taskA.accRadians.Y * 57.2958, taskA.accRadians.Z * 57.2958)
            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3
        End Sub
    End Class









    Public Class NR_IMU_PlotAngularVelocity : Inherits TaskParent
        Dim plot As New Plot_OverTimeScalar
        Public Sub New()
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            desc = "Plot the IMU Velocity over time."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("ts = " + Format(taskA.IMU_TimeStamp, fmt2) + vbCrLf + "X m/sec^2 = " + Format(taskA.IMU_Acceleration.X, fmt2) + vbCrLf +
                    "Y m/sec^2 = " + Format(taskA.IMU_Acceleration.Y, fmt2) + vbCrLf + "Z m/sec^2 = " + Format(taskA.IMU_Acceleration.Z, fmt2) + vbCrLf + vbCrLf +
                    "Motion (radians/sec) " + vbCrLf + "X - Pitch = " + Format(taskA.IMU_AngularVelocity.X, fmt2) + vbCrLf +
                    "Y - Yaw = " + Format(taskA.IMU_AngularVelocity.Y, fmt2) + vbCrLf + "Z - Roll = " + Format(taskA.IMU_AngularVelocity.Z, fmt2) + vbCrLf + vbCrLf +
                    "Move the camera to move values off of zero...", 1)

            plot.plotData = New cv.Scalar(taskA.IMU_AngularVelocity.X, taskA.IMU_AngularVelocity.Y, taskA.IMU_AngularVelocity.Z)
            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3
        End Sub
    End Class








    Public Class NR_IMU_Lines : Inherits TaskParent
        Dim vert As New XO_Line_GCloud
        Dim lastGcell As gravityLine
        Public Sub New()
            taskA.kalman = New Kalman_Basics
            labels(2) = "Vertical lines in Blue and horizontal lines in Yellow"
            desc = "Find the vertical and horizontal lines"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            vert.Run(src)
            dst2 = vert.dst2
            If vert.sortedVerticals.Count = 0 Then Exit Sub ' nothing to work on ...

            Dim gcell As New gravityLine
            Dim cells = vert.sortedVerticals
            If cells.Count > 0 Then gcell = cells.ElementAt(0).Value Else gcell = lastGcell
            If gcell.len3D > 0 Then
                strOut = "ID" + vbTab + "len3D" + vbTab + "Depth" + vbTab + "Arc Y" + vbTab + "Image" + vbTab + "IMU Y" + vbTab + vbCrLf
                If taskA.heartBeat Then dst3.SetTo(0)
                Dim p1 = gcell.tc1.center
                Dim p2 = gcell.tc2.center
                Dim lastP1 = New cv.Point(taskA.kalman.kOutput(0), taskA.kalman.kOutput(1))
                Dim lastp2 = New cv.Point(taskA.kalman.kOutput(2), taskA.kalman.kOutput(3))

                taskA.kalman.kInput = {p1.X, p1.Y, p2.X, p2.Y}
                taskA.kalman.Run(emptyMat)

                p1 = New cv.Point(taskA.kalman.kOutput(0), taskA.kalman.kOutput(1))
                p2 = New cv.Point(taskA.kalman.kOutput(2), taskA.kalman.kOutput(3))
                DrawCircle(dst2, p1, taskA.DotSize, taskA.highlight)
                DrawCircle(dst2, p2, taskA.DotSize, taskA.highlight)
                DrawCircle(dst3, p1, taskA.DotSize, white)

                DrawCircle(dst3, p2, taskA.DotSize, white)
                lastGcell = gcell
                strOut += CStr(0) + vbTab + Format(gcell.len3D, fmt1) + "m" + vbTab +
                                                Format(gcell.tc1.depth, fmt1) + "m" + vbTab +
                                                Format(gcell.arcY, fmt1) + vbTab +
                                                Format(gcell.imageAngle, fmt1) + vbTab
                strOut += Format(taskA.accRadians.Y * 57.2958, fmt1) + vbCrLf

                SetTrueText(strOut, 3)
                labels(2) = vert.labels(3)
            End If
        End Sub
    End Class








    Public Class NR_IMU_PlotAcceleration : Inherits TaskParent
        Dim plot As New Plot_OverTimeScalar
        Public Sub New()
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            desc = "Plot the IMU Acceleration in m/Sec^2 over time."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("ts = " + Format(taskA.IMU_TimeStamp, fmt2) + vbCrLf + "X m/sec^2 = " + Format(taskA.IMU_Acceleration.X, fmt2) + vbCrLf +
                    "Y m/sec^2 = " + Format(taskA.IMU_Acceleration.Y, fmt2) + vbCrLf + "Z m/sec^2 = " + Format(taskA.IMU_Acceleration.Z, fmt2) + vbCrLf + vbCrLf +
                    "Motion (radians/sec) " + vbCrLf + "pitch = " + Format(taskA.IMU_AngularVelocity.X, fmt2) + vbCrLf +
                    "Yaw = " + Format(taskA.IMU_AngularVelocity.Y, fmt2) + vbCrLf + " Roll = " + Format(taskA.IMU_AngularVelocity.Z, fmt2), 1)

            plot.plotData = New cv.Scalar(taskA.IMU_Acceleration.X, taskA.IMU_Acceleration.Y, taskA.IMU_Acceleration.Z)
            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3
        End Sub
    End Class







    Public Class IMU_Average : Inherits TaskParent
        Dim accList As New List(Of cv.Scalar)
        Public Sub New()
            desc = "Average the IMU Acceleration values over the previous X images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskA.optionsChanged Then accList.Clear()
            accList.Add(taskA.IMU_Acceleration)
            Dim accMat = cv.Mat.FromPixelData(accList.Count, 1, cv.MatType.CV_64FC4, accList.ToArray)
            Dim imuMean = accMat.Mean()
            taskA.IMU_AverageAcceleration = New cv.Point3f(imuMean(0), imuMean(1), imuMean(2))
            If accList.Count >= taskA.frameHistoryCount Then accList.RemoveAt(0)
            strOut = "Average IMU acceleration: " + vbCrLf + Format(taskA.IMU_AverageAcceleration.X, fmt3) + vbTab + Format(taskA.IMU_AverageAcceleration.Y, fmt3) + vbTab +
                  Format(taskA.IMU_AverageAcceleration.Z, fmt3) + vbCrLf
            SetTrueText(strOut)
        End Sub
    End Class






    Public Class NR_IMU_PlotCompareIMU : Inherits TaskParent
        Dim plot(3 - 1) As Plot_OverTimeScalar
        Dim imuAll As New IMU_Methods
        Public Sub New()
            If standalone Then taskA.gOptions.displayDst1.Checked = True
            If standalone Then taskA.gOptions.displayDst1.Checked = True

            For i = 0 To plot.Count - 1
                plot(i) = New Plot_OverTimeScalar
                plot(i).plotCount = 4
            Next

            labels = {"IMU Acceleration in X", "IMU Acceleration in Y", "IMU Acceleration in Z", ""}
            desc = "Compare the results of the raw IMU data with the same values after Kalman"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            imuAll.Run(src)

            plot(0).plotData = New cv.Scalar(taskA.IMU_Acceleration.X, taskA.IMU_Acceleration.X, taskA.kalmanIMUacc.X, taskA.IMU_AverageAcceleration.X)
            plot(0).Run(src)
            dst0 = plot(0).dst2

            plot(1).plotData = New cv.Scalar(taskA.IMU_Acceleration.Y, taskA.IMU_Acceleration.Y, taskA.kalmanIMUacc.Y, taskA.IMU_AverageAcceleration.Y)
            plot(1).Run(src)
            dst1 = plot(1).dst2

            plot(2).plotData = New cv.Scalar(taskA.IMU_Acceleration.Z, taskA.IMU_Acceleration.Z, taskA.kalmanIMUacc.Z, taskA.IMU_AverageAcceleration.Z)
            plot(2).Run(src)
            dst2 = plot(2).dst2

            SetTrueText("Blue (usually hidden) is the raw signal" + vbCrLf + "Green (usually hidden) is the Velocity-filtered results" + vbCrLf +
                    "Red is the Kalman IMU data" + vbCrLf + "White is the IMU Averaging output (note delay from Kalman output)" + vbCrLf + vbCrLf +
                    "Move the camera around to see the impact on the IMU data." + vbCrLf +
                    "Adjust the global option 'Frame History' to see the impact." + vbCrLf + vbCrLf +
                    "Remember that IMU Data filtering only impacts the X and Z values." + vbCrLf +
                    "Averaging seems to track closer but is not as timely.", 3)
        End Sub
    End Class









    Public Class IMU_Kalman : Inherits TaskParent
        Public Sub New()
            desc = "Use Kalman Filter to stabilize the IMU acceleration and velocity"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If taskA.kalman Is Nothing Then taskA.kalman = New Kalman_Basics
            With taskA.kalman
                .kInput = {taskA.IMU_Acceleration.X, taskA.IMU_Acceleration.Y, taskA.IMU_Acceleration.Z,
                       taskA.IMU_AngularVelocity.X, taskA.IMU_AngularVelocity.Y, taskA.IMU_AngularVelocity.Z}
                .Run(src)
                taskA.kalmanIMUacc = New cv.Point3f(.kOutput(0), .kOutput(1), .kOutput(2))
                taskA.kalmanIMUvelocity = New cv.Point3f(.kOutput(3), .kOutput(4), .kOutput(5))
            End With
            strOut = "IMU Acceleration Raw" + vbTab + "IMU Velocity Raw" + vbCrLf +
                 Format(taskA.IMU_Acceleration.X, fmt3) + vbTab + Format(taskA.IMU_Acceleration.Y, fmt3) + vbTab +
                 Format(taskA.IMU_Acceleration.Z, fmt3) + vbTab + Format(taskA.IMU_AngularVelocity.X, fmt3) + vbTab +
                 Format(taskA.IMU_AngularVelocity.Y, fmt3) + vbTab + Format(taskA.IMU_AngularVelocity.Z, fmt3) + vbTab + vbCrLf + vbCrLf +
                 "kalmanIMUacc" + vbTab + vbTab + "kalmanIMUvelocity" + vbCrLf +
                 Format(taskA.kalmanIMUacc.X, fmt3) + vbTab + Format(taskA.kalmanIMUacc.Y, fmt3) + vbTab +
                 Format(taskA.kalmanIMUacc.Z, fmt3) + vbTab + Format(taskA.kalmanIMUvelocity.X, fmt3) + vbTab +
                 Format(taskA.kalmanIMUvelocity.Y, fmt3) + vbTab + Format(taskA.kalmanIMUvelocity.Z, fmt3) + vbTab
            SetTrueText(strOut)
        End Sub
    End Class







    Public Class IMU_Methods : Inherits TaskParent
        Dim basics As New IMU_Basics
        Dim imuAvg As New IMU_Average
        Dim kalman As New IMU_Kalman
        Public Sub New()
            desc = "Compute the IMU acceleration using all available methods - raw, Kalman, averaging, and velocity-filtered."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            basics.Run(src)
            kalman.Run(src)
            imuAvg.Run(src)

            SetTrueText(basics.strOut + vbCrLf + kalman.strOut + vbCrLf + vbCrLf + imuAvg.strOut, 2)
        End Sub
    End Class







    Public Class NR_IMU_VelocityPlot : Inherits TaskParent
        Dim plot As New IMU_Plot
        Public Sub New()
            If standalone Then taskA.gOptions.displaydst1.checked = True
            desc = "Plot the angular velocity"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            taskA.pitch = taskA.IMU_AngularVelocity.X
            taskA.yaw = taskA.IMU_AngularVelocity.Y
            taskA.roll = taskA.IMU_AngularVelocity.Z

            plot.blueA = taskA.pitch * 1000
            plot.greenA = taskA.yaw * 1000
            plot.redA = taskA.roll * 1000
            plot.labels(2) = "pitch X 1000 (blue), Yaw X 1000 (green), and roll X 1000 (red)"

            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3

            If taskA.heartBeat Then
                strOut = "Pitch X1000 (blue): " + vbTab + Format(taskA.pitch * 1000, fmt1) + vbCrLf +
                     "Yaw X1000 (green): " + vbTab + Format(taskA.yaw * 1000, fmt1) + vbCrLf +
                     "Roll X1000 (red): " + vbTab + Format(taskA.roll * 1000, fmt1)
            End If
            SetTrueText(strOut, 1)
        End Sub
    End Class







    Public Class NR_IMU_IscameraStable : Inherits TaskParent
        Dim plot As New IMU_Plot
        Dim options As New Options_IMU
        Public Sub New()
            desc = "Track the standard deviation of the angular velocities."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            taskA.pitch = taskA.IMU_AngularVelocity.X
            taskA.yaw = taskA.IMU_AngularVelocity.Y
            taskA.roll = taskA.IMU_AngularVelocity.Z
            If taskA.heartBeat Then
                strOut = "Pitch X1000 (blue): " + vbTab + Format(taskA.pitch * 1000, fmt1) + vbCrLf +
                     "Yaw X1000 (green): " + vbTab + Format(taskA.yaw * 1000, fmt1) + vbCrLf +
                     "Roll X1000 (red): " + vbTab + Format(taskA.roll * 1000, fmt1)
            End If
            SetTrueText(strOut, 3)
        End Sub
    End Class





    Public Class IMU_PlotHostFrameTimes : Inherits TaskParent
        Public plot As New Plot_OverTime
        Public CPUInterval As Double
        Public HostInterruptDelayEstimate As Double
        Dim options As New Options_IMUFrameTime
        Public Sub New()
            plot.dst2 = dst3
            plot.maxScale = 50
            plot.minScale = -10
            plot.plotCount = 4

            labels(3) = "IMU (blue) Host (green) Latency est. (red) - all in ms"
            desc = "Use the Host timestamp to estimate the delay from image capture to host interrupt.  Just an estimate!"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Static CPUanchor As Integer = taskA.CPU_FrameTime
            Static hist(plot.maxScale) As Integer

            ' there can be some errant times at startup.
            If taskA.CPU_FrameTime > plot.maxScale Then taskA.CPU_FrameTime = plot.maxScale
            If taskA.CPU_FrameTime < 0 Then taskA.CPU_FrameTime = 0

            Dim maxval = Integer.MinValue
            For i = 0 To hist.Count - 1
                If maxval < hist(i) Then
                    maxval = hist(i)
                    CPUanchor = i
                End If
            Next

            Dim cpuFrameTime = CInt(taskA.CPU_FrameTime)
            If CPUanchor <> 0 Then cpuFrameTime = cpuFrameTime Mod CPUanchor
            HostInterruptDelayEstimate = CPUanchor - cpuFrameTime + options.minDelayHost
            If HostInterruptDelayEstimate > CPUanchor Then HostInterruptDelayEstimate -= CPUanchor
            If HostInterruptDelayEstimate < 0 Then HostInterruptDelayEstimate = options.minDelayHost

            Static sampledCPUFrameTime = taskA.CPU_FrameTime
            If taskA.heartBeat Then sampledCPUFrameTime = taskA.CPU_FrameTime

            hist(Math.Min(CInt(taskA.CPU_FrameTime), hist.Length - 1)) += 1

            If standaloneTest() Then
                Dim output = "IMU_TimeStamp (ms) " + Format(taskA.IMU_TimeStamp, "00") + vbCrLf +
                         "CPU TimeStamp (ms) " + Format(taskA.CPU_TimeStamp, "00") + vbCrLf +
                         "Host Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                         " CPUanchor = " + Format(CPUanchor, "00") +
                         " latest = " + Format(taskA.CPU_FrameTime, "00.00") + vbCrLf +
                         "Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00") + vbCrLf + vbCrLf +
                         "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                         "Green" + vbTab + "Host Frame Time" + vbCrLf +
                         "Red" + vbTab + "Host Total Delay (latency)" + vbCrLf +
                         "White" + vbTab + "Host Anchor Frame Time (Host Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

                plot.plotData = New cv.Scalar(taskA.IMU_FrameTime, taskA.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
                plot.Run(src)

                If plot.maxScale - plot.minScale > hist.Count Then ReDim hist(plot.maxScale - plot.minScale)

                If plot.lastXdelta.Count > options.plotLastX Then
                    For i = 0 To plot.plotCount - 1
                        output += "Last " + CStr(options.plotLastX) + Choose(i + 1, " IMU FrameTime", " Host Frametime", " Host Delay ms", " CPUanchor FT") + vbTab
                        For j = plot.lastXdelta.Count - options.plotLastX - 1 To plot.lastXdelta.Count - 1
                            output += Format(plot.lastXdelta(j)(i), "00") + ", "
                        Next
                        output += vbCrLf
                    Next
                End If
                SetTrueText(output)
            End If
        End Sub
    End Class









    Public Class NR_IMU_PlotHostFrameScalar : Inherits TaskParent
        Public plot As New Plot_OverTimeScalar
        Public CPUInterval As Double
        Public HostInterruptDelayEstimate As Double
        Dim options As New Options_IMUFrameTime
        Public Sub New()
            If standalone Then taskA.gOptions.displaydst1.checked = True
            plot.plotCount = 4
            labels(3) = "IMU (blue) Host (green) Latency est. (red) - all in ms"
            desc = "Use the Host timestamp to estimate the delay from image capture to host interrupt.  Just an estimate!"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Static CPUanchor As Integer = taskA.CPU_FrameTime

            Dim cpuFrameTime = CInt(taskA.CPU_FrameTime)
            If CPUanchor <> 0 Then cpuFrameTime = cpuFrameTime Mod CPUanchor
            HostInterruptDelayEstimate = CPUanchor - cpuFrameTime + options.minDelayHost
            If HostInterruptDelayEstimate > CPUanchor Then HostInterruptDelayEstimate -= CPUanchor
            If HostInterruptDelayEstimate < 0 Then HostInterruptDelayEstimate = options.minDelayHost

            Static sampledCPUFrameTime = taskA.CPU_FrameTime
            If taskA.heartBeat Then sampledCPUFrameTime = taskA.CPU_FrameTime

            If standaloneTest() Then
                strOut = "IMU_TimeStamp (ms) " + Format(taskA.IMU_TimeStamp, "00") + vbCrLf +
                     "CPU TimeStamp (ms) " + Format(taskA.CPU_TimeStamp, "00") + vbCrLf +
                     "Host Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                     " CPUanchor = " + Format(CPUanchor, "00") +
                     " latest = " + Format(taskA.CPU_FrameTime, "00.00") + vbCrLf +
                     "Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00") + vbCrLf + vbCrLf +
                     "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                     "Green" + vbTab + "Host Frame Time" + vbCrLf +
                     "Red" + vbTab + "Host Total Delay (latency)" + vbCrLf +
                     "White" + vbTab + "Host Anchor Frame Time (Host Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

                plot.plotData = New cv.Scalar(taskA.IMU_FrameTime, taskA.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
                plot.Run(src)
                dst2 = plot.dst2
                dst3 = plot.dst3
                SetTrueText(strOut, 1)
            End If
        End Sub
    End Class







    ' https://www.codeproject.com/Articles/1247960/3D-graphics-engine-with-basic-math-on-CPU
    Public Class IMU_GMatrixWithOptions : Inherits TaskParent
        Public cx As Single = 1, sx As Single = 0, cy As Single = 1, sy As Single = 0, cz As Single = 1, sz As Single = 0
        Public gMatrix As cv.Mat
        Dim xSlider As TrackBar
        Dim ySlider As TrackBar
        Dim zSlider As TrackBar
        Dim options As New Options_IMU
        Public Sub New()
            desc = "Find the angle of tilt for the camera with respect to gravity."
        End Sub
        Private Sub getSliderValues()
            If xSlider Is Nothing Then xSlider = OptionParent.FindSlider("Rotate pointcloud around X-axis (degrees)")
            If ySlider Is Nothing Then ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")
            If zSlider Is Nothing Then zSlider = OptionParent.FindSlider("Rotate pointcloud around Z-axis (degrees)")
            cx = Math.Cos(xSlider.Value * cv.Cv2.PI / 180)
            sx = Math.Sin(xSlider.Value * cv.Cv2.PI / 180)

            cy = Math.Cos(ySlider.Value * cv.Cv2.PI / 180)
            sy = Math.Sin(ySlider.Value * cv.Cv2.PI / 180)

            cz = Math.Cos(zSlider.Value * cv.Cv2.PI / 180)
            sz = Math.Sin(zSlider.Value * cv.Cv2.PI / 180)
        End Sub
        Private Function buildGmatrix() As cv.Mat
            '[cx -sx    0]  [1  0   0 ] 
            '[sx  cx    0]  [0  cz -sz]
            '[0   0     1]  [0  sz  cz]
            Dim gArray As Single(,) = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                                   {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                                   {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}

            Dim tmpGMatrix = cv.Mat.FromPixelData(3, 3, cv.MatType.CV_32F, {
                  {gArray(0, 0) * cy + gArray(0, 1) * 0 + gArray(0, 2) * sy},
                  {gArray(0, 0) * 0 + gArray(0, 1) * 1 + gArray(0, 2) * 0},
                  {gArray(0, 0) * -sy + gArray(0, 1) * 0 + gArray(0, 2) * cy},
                  {gArray(1, 0) * cy + gArray(1, 1) * 0 + gArray(1, 2) * sy},
                  {gArray(1, 0) * 0 + gArray(1, 1) * 1 + gArray(1, 2) * 0},
                  {gArray(1, 0) * -sy + gArray(1, 1) * 0 + gArray(1, 2) * cy},
                  {gArray(2, 0) * cy + gArray(2, 1) * 0 + gArray(2, 2) * sy},
                  {gArray(2, 0) * 0 + gArray(2, 1) * 1 + gArray(2, 2) * 0},
                  {gArray(2, 0) * -sy + gArray(2, 1) * 0 + gArray(2, 2) * cy}})

            Return tmpGMatrix
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If xSlider Is Nothing Then xSlider = OptionParent.FindSlider("Rotate pointcloud around X-axis (degrees)")
            If ySlider Is Nothing Then ySlider = OptionParent.FindSlider("Rotate pointcloud around Y-axis (degrees)")
            If zSlider Is Nothing Then zSlider = OptionParent.FindSlider("Rotate pointcloud around Z-axis (degrees)")

            If taskA.gOptions.gravityPointCloud.Checked Then
                '[cos(a) -sin(a)    0]
                '[sin(a)  cos(a)    0]
                '[0       0         1] rotate the point cloud around the x-axis.
                cz = Math.Cos(taskA.accRadians.Z)
                sz = Math.Sin(taskA.accRadians.Z)

                '[1       0         0      ] rotate the point cloud around the z-axis.
                '[0       cos(a)    -sin(a)]
                '[0       sin(a)    cos(a) ]
                cx = Math.Cos(taskA.accRadians.X)
                sx = Math.Sin(taskA.accRadians.X)
            Else
                getSliderValues()
            End If

            gMatrix = buildGmatrix()

            If standaloneTest() Then
                Dim g = taskA.IMU_Acceleration
                strOut = "IMU Acceleration in X-direction = " + vbTab + vbTab + Format(g.X, fmt4) + vbCrLf
                strOut += "IMU Acceleration in Y-direction = " + vbTab + vbTab + Format(g.Y, fmt4) + vbCrLf
                strOut += "IMU Acceleration in Z-direction = " + vbTab + vbTab + Format(g.Z, fmt4) + vbCrLf + vbCrLf
                strOut += "Rotate around X-axis (in degrees) = " + vbTab + Format(xSlider.Value, fmt4) + vbCrLf
                strOut += "Rotate around Y-axis (in degrees) = " + vbTab + Format(ySlider.Value, fmt4) + vbCrLf
                strOut += "Rotate around Z-axis (in degrees) = " + vbTab + Format(zSlider.Value, fmt4) + vbCrLf
                strOut += vbCrLf + "sqrt (" + vbTab + Format(g.X, fmt4) + "*" + Format(g.X, fmt4) + vbTab +
                          vbTab + Format(g.Y, fmt4) + "*" + Format(g.Y, fmt4) + vbTab +
                          vbTab + Format(g.Z, fmt4) + "*" + Format(g.Z, fmt4) + " ) = " + vbTab +
                          vbTab + Format(Math.Sqrt(g.X * g.X + g.Y * g.Y + g.Z * g.Z), fmt4) + vbCrLf +
                          "Should be close to the earth's gravitational constant of 9.807 (or the camera was moving.)"

                Dim tmpGMat1 = buildGmatrix()
                strOut += vbCrLf + "Gravity-oriented gMatrix - move camera to test this:" + vbCrLf + IMU_GMatrix.gMatrixToStr(tmpGMat1)

                getSliderValues()
                Dim tmpGMat2 = buildGmatrix()
                strOut += vbCrLf + "gMatrix with slider input - use Options_IMU Sliders to change this:" + vbCrLf + IMU_GMatrix.gMatrixToStr(tmpGMat2)
            End If
            SetTrueText(strOut)
            taskA.gMatrix = gMatrix
        End Sub
    End Class








    Public Class IMU_VerticalVerify : Inherits TaskParent
        Public brickCells As New List(Of gravityLine)
        Dim linesVH As New LineEnds_VH
        Dim options As New Options_VerticalVerify
        Public Sub New()
            labels = {"", "", "Highlighted vertical lines", "Line details"}
            desc = "Use the Y-Arc to confirm which vertical lines are valid"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            dst2 = src.Clone

            If standaloneTest() Then
                linesVH.Run(src)
                brickCells = linesVH.brickCells
            End If

            strOut = "ID" + vbTab + "len3D" + vbTab + "Depth" + vbTab + "Arc Y" + vbTab + "Image" + vbTab + "IMU Y" + vbTab + vbCrLf
            dst3.SetTo(0)
            Dim index As Integer
            For i = brickCells.Count - 1 To 0 Step -1
                Dim gr = brickCells(i)
                If gr.arcY > options.angleThreshold Then
                    index = brickCells.Count - i
                    Dim p1 = gr.tc1.center
                    Dim p2 = gr.tc2.center
                    Dim xOffset = p1.X - p2.X
                    If p1.Y < p2.Y Then xOffset = p2.X - p1.X
                    Dim hypot = p1.DistanceTo(p2)
                    gr.imageAngle = -Math.Asin(xOffset / hypot) * 57.2958

                    strOut += CStr(index) + vbTab + Format(gr.len3D, fmt1) + "m" + vbTab +
                                                Format(gr.tc1.depth, fmt1) + "m" + vbTab +
                                                Format(gr.arcY, fmt1) + vbTab +
                                                Format(gr.imageAngle, fmt1) + vbTab
                    strOut += Format(taskA.accRadians.Y * 57.2958, fmt1) + vbCrLf

                    SetTrueText(CStr(index), gr.tc1.center, 2)
                    SetTrueText(CStr(index), gr.tc1.center, 3)
                    vbc.DrawLine(dst2, gr.tc1.center, gr.tc2.center, taskA.highlight)
                    vbc.DrawLine(dst3, gr.tc1.center, gr.tc2.center, white)
                    brickCells(i) = gr
                Else
                    brickCells.RemoveAt(i)
                End If
            Next
            SetTrueText(strOut, 3)
        End Sub
    End Class









    Public Class IMU_Plot : Inherits TaskParent
        Dim plot As New Plot_OverTimeScalar
        Public blueA As Single, greenA As Single, redA As Single
        Dim options As New Options_IMUPlot
        Public Sub New()
            plot.plotCount = 3
            desc = "Plot the angular velocity of the camera based on the IMU data"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If standaloneTest() Then
                blueA = taskA.IMU_AngularVelocity.X * 1000
                greenA = taskA.IMU_AngularVelocity.Y * 1000
                redA = taskA.IMU_AngularVelocity.Z * 1000
            End If

            Dim blueX As Single, greenX As Single, redX As Single

            If options.setBlue Then blueX = blueA
            If options.setGreen Then greenX = greenA
            If options.setRed Then redX = redA

            plot.plotData = New cv.Scalar(blueX, greenX, redX)
            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3
            labels(2) = "When run standaloneTest(), the default is to plot the angular velocity for X, Y, and Z"
        End Sub
    End Class
End Namespace