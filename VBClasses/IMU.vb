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
            If algTask.optionsChanged Then
                lastTimeStamp = algTask.IMU_TimeStamp
            Else
                gyroAngle = algTask.IMU_AngularVelocity
                Dim dt_gyro = (algTask.IMU_TimeStamp - lastTimeStamp) / 1000
                If algTask.settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then
                    dt_gyro /= 1000 ' different units in the timestamp?
                End If
                gyroAngle = gyroAngle * dt_gyro
                algTask.theta += New cv.Point3f(-gyroAngle.Z, -gyroAngle.Y, gyroAngle.X)
                lastTimeStamp = algTask.IMU_TimeStamp
            End If

            ' NOTE: Initialize the angle around the y-axis to zero.
            Dim g = algTask.IMU_RawAcceleration
            algTask.accRadians = New cv.Point3f(Math.Atan2(g.X, Math.Sqrt(g.Y * g.Y + g.Z * g.Z)),
                                             Math.Abs(Math.Atan2(g.X, g.Y)), Math.Atan2(g.Y, g.Z))
            If algTask.optionsChanged Then
                algTask.theta = algTask.accRadians
            Else
                ' Apply the Complementary Filter:
                '  - high-pass filter = theta * alpha: allows short-duration signals to pass while filtering steady signals (trying to cancel drift)
                '  - low-pass filter = accel * (1 - alpha): lets the long-term changes through, filtering out short term fluctuations
                algTask.theta.X = algTask.theta.X * algTask.IMU_AlphaFilter + algTask.accRadians.X * (1 - algTask.IMU_AlphaFilter)
                algTask.theta.Y = algTask.accRadians.Y
                algTask.theta.Z = algTask.theta.Z * algTask.IMU_AlphaFilter + algTask.accRadians.Z * (1 - algTask.IMU_AlphaFilter)
            End If

            Dim x1 = -(90 + algTask.accRadians.X * 57.2958)
            Dim x2 = -(90 + algTask.theta.X * 57.2958)
            Dim y1 = algTask.accRadians.Y - cv.Cv2.PI
            If algTask.accRadians.X < 0 Then y1 *= -1
            algTask.verticalizeAngle = y1 * 58.2958
            strOut = "Angles in degree to gravity (before velocity filter)" + vbCrLf +
                     Format(x1, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(algTask.accRadians.Z * 57.2958, fmt1) + vbCrLf +
                     "Velocity-Filtered Angles to gravity in degrees" + vbCrLf +
                     Format(x2, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(algTask.theta.Z * 57.2958, fmt1) + vbCrLf
            strOut += "cx = " + Format(algTask.gmat.cx, fmt3) + " sx = " + Format(algTask.gmat.sx, fmt3) + vbCrLf +
                      "cy = " + Format(algTask.gmat.cy, fmt3) + " sy = " + Format(algTask.gmat.sy, fmt3) + vbCrLf +
                      "cz = " + Format(algTask.gmat.cz, fmt3) + " sz = " + Format(algTask.gmat.sz, fmt3)

            algTask.accRadians = algTask.theta
            If algTask.accRadians.Y > cv.Cv2.PI / 2 Then algTask.accRadians.Y -= cv.Cv2.PI / 2
            algTask.accRadians.Z += cv.Cv2.PI / 2

            SetTrueText(strOut)
        End Sub
    End Class








    ' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
    Public Class IMU_BasicsKalman : Inherits TaskParent
        Dim lastTimeStamp As Double
        Public Sub New()
            algTask.kalman = New Kalman_Basics
            desc = "Read and display the IMU coordinates"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim gyroAngle As cv.Point3f
            If algTask.optionsChanged Then
                lastTimeStamp = algTask.IMU_TimeStamp
            Else
                gyroAngle = algTask.IMU_AngularVelocity
                Dim dt_gyro = (algTask.IMU_TimeStamp - lastTimeStamp) / 1000
                If algTask.settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then
                    dt_gyro /= 1000 ' different units in the timestamp?
                End If
                gyroAngle = gyroAngle * dt_gyro
                lastTimeStamp = algTask.IMU_TimeStamp
            End If

            ' NOTE: Initialize the angle around the y-axis to zero.
            Dim g = algTask.IMU_RawAcceleration
            algTask.accRadians = New cv.Point3f(Math.Atan2(g.X, Math.Sqrt(g.Y * g.Y + g.Z * g.Z)),
                                         Math.Abs(Math.Atan2(g.X, g.Y)), Math.Atan2(g.Y, g.Z))

            algTask.kalman.kInput = {algTask.accRadians.X, algTask.accRadians.Y, algTask.accRadians.Z}
            algTask.kalman.Run(Nothing)

            algTask.accRadians = New cv.Point3f(algTask.kalman.kOutput(0), algTask.kalman.kOutput(1), algTask.kalman.kOutput(2))

            Dim x1 = -(90 + algTask.accRadians.X * 57.2958)
            Dim y1 = algTask.accRadians.Y - cv.Cv2.PI
            If algTask.accRadians.X < 0 Then y1 *= -1
            strOut = "Angles in degree to gravity (before velocity filter)" + vbCrLf +
                 Format(x1, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(algTask.accRadians.Z * 57.2958, fmt1) + vbCrLf
            strOut += "cx = " + Format(algTask.gmat.cx, fmt3) + " sx = " + Format(algTask.gmat.sx, fmt3) + vbCrLf +
                  "cy = " + Format(algTask.gmat.cy, fmt3) + " sy = " + Format(algTask.gmat.sy, fmt3) + vbCrLf +
                  "cz = " + Format(algTask.gmat.cz, fmt3) + " sz = " + Format(algTask.gmat.sz, fmt3)
            If algTask.accRadians.Y > cv.Cv2.PI / 2 Then algTask.accRadians.Y -= cv.Cv2.PI / 2
            algTask.accRadians.Z += cv.Cv2.PI / 2

            SetTrueText(strOut)
        End Sub
    End Class







    ' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
    Public Class IMU_BasicsWithOptions : Inherits TaskParent
        Dim lastTimeStamp As Double
        Dim options As New Options_IMU
        Public Sub New()
            desc = "Read and display the IMU coordinates"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim gyroAngle As cv.Point3f
            If algTask.optionsChanged Then
                lastTimeStamp = algTask.IMU_TimeStamp
            Else
                gyroAngle = algTask.IMU_AngularVelocity
                Dim dt_gyro = (algTask.IMU_TimeStamp - lastTimeStamp) / 1000
                If algTask.settings.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then
                    dt_gyro /= 1000 ' different units in the timestamp?
                End If
                gyroAngle = gyroAngle * dt_gyro
                algTask.theta += New cv.Point3f(-gyroAngle.Z, -gyroAngle.Y, gyroAngle.X)
                lastTimeStamp = algTask.IMU_TimeStamp
            End If

            ' NOTE: Initialize the angle around the y-axis to zero.
            Dim g = algTask.IMU_RawAcceleration
            algTask.accRadians = New cv.Point3f(Math.Atan2(g.X, Math.Sqrt(g.Y * g.Y + g.Z * g.Z)),
                                         Math.Abs(Math.Atan2(g.X, g.Y)), Math.Atan2(g.Y, g.Z))

            If algTask.optionsChanged Then
                algTask.theta = algTask.accRadians
            Else
                ' Apply the Complementary Filter:
                '  - high-pass filter = theta * alpha: allows short-duration signals to pass while filtering steady signals (trying to cancel drift)
                '  - low-pass filter = accel * (1 - alpha): lets the long-term changes through, filtering out short term fluctuations
                algTask.theta.X = algTask.theta.X * algTask.IMU_AlphaFilter + algTask.accRadians.X * (1 - algTask.IMU_AlphaFilter)
                algTask.theta.Y = algTask.accRadians.Y
                algTask.theta.Z = algTask.theta.Z * algTask.IMU_AlphaFilter + algTask.accRadians.Z * (1 - algTask.IMU_AlphaFilter)
            End If

            Dim x1 = -(90 + algTask.accRadians.X * 57.2958)
            Dim x2 = -(90 + algTask.theta.X * 57.2958)
            Dim y1 = algTask.accRadians.Y - cv.Cv2.PI
            If algTask.accRadians.X < 0 Then y1 *= -1
            strOut = "Angles in degree to gravity (before velocity filter)" + vbCrLf +
                 Format(x1, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(algTask.accRadians.Z * 57.2958, fmt1) + vbCrLf +
                 "Velocity-Filtered Angles to gravity in degrees" + vbCrLf +
                 Format(x2, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(algTask.theta.Z * 57.2958, fmt1) + vbCrLf
            SetTrueText(strOut)

            algTask.accRadians = algTask.theta
            If algTask.accRadians.Y > cv.Cv2.PI / 2 Then algTask.accRadians.Y -= cv.Cv2.PI / 2
            algTask.accRadians.Z += cv.Cv2.PI / 2

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
            angleXValue.Add(algTask.accRadians.X)
            angleYValue.Add(algTask.accRadians.Y)

            strOut = "IMU X" + vbTab + "IMU Y" + vbTab + "IMU Z" + vbCrLf
            strOut += Format(algTask.accRadians.X * 57.2958, fmt3) + vbTab + Format(algTask.accRadians.Y * 57.2958, fmt3) + vbTab +
                  Format(algTask.accRadians.Z * 57.2958, fmt3) + vbCrLf
            Dim avgX = angleXValue.Average
            Dim avgY = angleYValue.Average
            If algTask.firstPass Then
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
            If algTask.heartBeat Then
                Dim avgStable = stableCount.Average
                stableStr = "IMU stable = " + Format(avgStable, "0.0%") + " of the time"
                stableCount.Clear()
            End If
            SetTrueText(strOut + vbCrLf + stableStr, 2)

            lastAngleX = avgX
            lastAngleY = avgY

            If angleXValue.Count >= algTask.frameHistoryCount Then angleXValue.RemoveAt(0)
            If angleYValue.Count >= algTask.frameHistoryCount Then angleYValue.RemoveAt(0)
        End Sub
    End Class






    ' https://www.codeproject.com/Articles/1247960/3D-graphics-engine-with-basic-math-on-CPU
    Public Class IMU_GMatrix : Inherits TaskParent
        Public cx As Single = 1, sx As Single = 0, cy As Single = 1, sy As Single = 0, cz As Single = 1, sz As Single = 0
        Public gMatrix As cv.Mat
        Public Sub New()
            desc = "Find the angle of tilt for the camera with respect to gravity."
        End Sub
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
            If algTask.algorithmPrep = False Then Exit Sub ' a direct call from another algorithm is unnecessary - already been run...
            If standaloneTest() Then
                labels(2) = "IMU_GMatrix builds the gMatrix (gravity matrix) on every iteration."
            End If

            '[cos(a) -sin(a)    0]
            '[sin(a)  cos(a)    0]
            '[0       0         1] rotate the point cloud around the x-axis.
            cz = Math.Cos(algTask.accRadians.Z)
            sz = Math.Sin(algTask.accRadians.Z)

            '[1       0         0      ] rotate the point cloud around the z-axis.
            '[0       cos(a)    -sin(a)]
            '[0       sin(a)    cos(a) ]
            cx = Math.Cos(algTask.accRadians.X)
            sx = Math.Sin(algTask.accRadians.X)

            buildGmatrix()

            Dim g = algTask.IMU_Acceleration
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
            algTask.gMatrix = gMatrix
        End Sub
    End Class







    Public Class IMU_Stabilize : Inherits TaskParent
        Public Sub New()
            algTask.kalman = New Kalman_Basics
            ReDim algTask.kalman.kInput(3 - 1)
            desc = "Stabilize IMU acceleration data."
            labels = {"", "", "IMU Stabilize (move camera around)", "Difference from Color Image"}
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim borderCrop = 5
            Dim vert_Border = borderCrop * src.Rows / src.Cols
            Dim dx = algTask.IMU_AngularVelocity.X
            Dim dy = algTask.IMU_AngularVelocity.Y
            Dim dz = algTask.IMU_AngularVelocity.Z
            Dim sx = 1 ' assume no scaling is taking place.
            Dim sy = 1 ' assume no scaling is taking place.

            algTask.kalman.kInput = {dx, dy, dz}
            algTask.kalman.Run(emptyMat)
            dx = algTask.kalman.kOutput(0)
            dy = algTask.kalman.kOutput(1)
            dz = algTask.kalman.kOutput(2)

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

            Static IMUanchor As Integer = algTask.IMU_FrameTime Mod 4000000000
            Static histogramIMU(plot.maxScale) As Integer

            ' there can be some errant times at startup.
            If CInt(algTask.IMU_FrameTime) >= histogramIMU.Length Then algTask.IMU_FrameTime = plot.maxScale
            If algTask.IMU_FrameTime < 0 Then algTask.IMU_FrameTime = 0

            imuTotalTime += algTask.IMU_FrameTime
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

            Dim imuFrameTime = CInt(algTask.IMU_FrameTime)
            If IMUanchor <> 0 Then imuFrameTime = imuFrameTime Mod IMUanchor
            IMUtoCaptureEstimate = IMUanchor - imuFrameTime + options.minDelayIMU
            If IMUtoCaptureEstimate > IMUanchor Then IMUtoCaptureEstimate -= IMUanchor
            If IMUtoCaptureEstimate < options.minDelayIMU Then IMUtoCaptureEstimate = options.minDelayIMU

            Static sampledIMUFrameTime = algTask.IMU_FrameTime
            If algTask.heartBeat Then sampledIMUFrameTime = algTask.IMU_FrameTime

            histogramIMU(Math.Min(CInt(algTask.IMU_FrameTime), histogramIMU.Length - 1)) += 1

            If standaloneTest() Then
                Dim output = "IMU_TimeStamp (ms) " + Format(algTask.IMU_TimeStamp, "00") + vbCrLf +
                        "CPU TimeStamp (ms) " + Format(algTask.CPU_TimeStamp, "00") + vbCrLf +
                        "IMU Frametime (ms, sampled) " + Format(sampledIMUFrameTime, "000.00") +
                        " IMUanchor = " + Format(IMUanchor, "00") +
                        " latest = " + Format(algTask.IMU_FrameTime, "00.00") + vbCrLf +
                        "IMUtoCapture (ms, sampled, in red) " + Format(IMUtoCaptureEstimate, "00") + vbCrLf + vbCrLf +
                        "IMU Frame Time = Blue" + vbCrLf +
                        "Host Frame Time = Green" + vbCrLf +
                        "IMU Total Delay = Red" + vbCrLf +
                        "IMU Anchor Frame Time = White (IMU Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

                plot.plotData = New cv.Scalar(algTask.IMU_FrameTime, algTask.CPU_FrameTime, IMUtoCaptureEstimate, IMUanchor)
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





    Public Class IMU_PlotTotalDelay : Inherits TaskParent
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
            If algTask.heartBeat Then
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









    Public Class IMU_VerticalAngles : Inherits TaskParent
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
                Dim brick = cells.ElementAt(i).Value
                strOut += CStr(i) + vbTab + Format(brick.len3D, fmt1) + "m" + vbTab + Format(brick.tc1.depth, fmt1) + "m" + vbTab +
                      Format(brick.arcX, fmt1) + vbTab + Format(brick.arcY, fmt1) + vbTab + Format(brick.arcZ, fmt1) + vbTab
                strOut += Format(algTask.accRadians.X * 57.2958, fmt1) + vbTab + Format(algTask.accRadians.Y * 57.2958, fmt1) + vbTab + Format(algTask.accRadians.Z * 57.2958, fmt1) + vbTab + vbCrLf
                SetTrueText(CStr(i), brick.tc1.center, 2)
                SetTrueText(CStr(i), brick.tc1.center, 3)
                vbc.DrawLine(dst2, brick.tc1.center, brick.tc2.center, algTask.highlight)
                vbc.DrawLine(dst3, brick.tc1.center, brick.tc2.center, white)
            Next
            SetTrueText(strOut, 3)
        End Sub
    End Class








    Public Class IMU_PlotGravityAngles : Inherits TaskParent
        Dim plot As New Plot_OverTimeScalar
        Public Sub New()
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            desc = "Plot the motion of the camera based on the IMU data in degrees"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("ts = " + Format(algTask.IMU_TimeStamp, fmt2) + vbCrLf + "X degrees = " + Format(algTask.accRadians.X * 57.2958, fmt3) + vbCrLf +
                    "Y degrees = " + Format(Math.Abs(algTask.accRadians.Y * 57.2958), fmt3) + vbCrLf + "Z degrees = " + Format(algTask.accRadians.Z * 57.2958, fmt2) + vbCrLf + vbCrLf +
                    "Motion (radians/sec) " + vbCrLf + "pitch = " + Format(algTask.IMU_AngularVelocity.X, fmt2) + vbCrLf +
                    "Yaw = " + Format(algTask.IMU_AngularVelocity.Y, fmt2) + vbCrLf + " Roll = " + Format(algTask.IMU_AngularVelocity.Z, fmt2), 1)

            plot.plotData = New cv.Scalar(algTask.accRadians.X * 57.2958, algTask.accRadians.Y * 57.2958, algTask.accRadians.Z * 57.2958)
            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3
        End Sub
    End Class









    Public Class IMU_PlotAngularVelocity : Inherits TaskParent
        Dim plot As New Plot_OverTimeScalar
        Public Sub New()
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            desc = "Plot the IMU Velocity over time."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("ts = " + Format(algTask.IMU_TimeStamp, fmt2) + vbCrLf + "X m/sec^2 = " + Format(algTask.IMU_Acceleration.X, fmt2) + vbCrLf +
                    "Y m/sec^2 = " + Format(algTask.IMU_Acceleration.Y, fmt2) + vbCrLf + "Z m/sec^2 = " + Format(algTask.IMU_Acceleration.Z, fmt2) + vbCrLf + vbCrLf +
                    "Motion (radians/sec) " + vbCrLf + "X - Pitch = " + Format(algTask.IMU_AngularVelocity.X, fmt2) + vbCrLf +
                    "Y - Yaw = " + Format(algTask.IMU_AngularVelocity.Y, fmt2) + vbCrLf + "Z - Roll = " + Format(algTask.IMU_AngularVelocity.Z, fmt2) + vbCrLf + vbCrLf +
                    "Move the camera to move values off of zero...", 1)

            plot.plotData = New cv.Scalar(algTask.IMU_AngularVelocity.X, algTask.IMU_AngularVelocity.Y, algTask.IMU_AngularVelocity.Z)
            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3
        End Sub
    End Class








    Public Class IMU_Lines : Inherits TaskParent
        Dim vert As New XO_Line_GCloud
        Dim lastGcell As gravityLine
        Public Sub New()
            algTask.kalman = New Kalman_Basics
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
                If algTask.heartBeat Then dst3.SetTo(0)
                Dim p1 = gcell.tc1.center
                Dim p2 = gcell.tc2.center
                Dim lastP1 = New cv.Point(algTask.kalman.kOutput(0), algTask.kalman.kOutput(1))
                Dim lastp2 = New cv.Point(algTask.kalman.kOutput(2), algTask.kalman.kOutput(3))

                algTask.kalman.kInput = {p1.X, p1.Y, p2.X, p2.Y}
                algTask.kalman.Run(emptyMat)

                p1 = New cv.Point(algTask.kalman.kOutput(0), algTask.kalman.kOutput(1))
                p2 = New cv.Point(algTask.kalman.kOutput(2), algTask.kalman.kOutput(3))
                DrawCircle(dst2, p1, algTask.DotSize, algTask.highlight)
                DrawCircle(dst2, p2, algTask.DotSize, algTask.highlight)
                DrawCircle(dst3, p1, algTask.DotSize, white)

                DrawCircle(dst3, p2, algTask.DotSize, white)
                lastGcell = gcell
                strOut += CStr(0) + vbTab + Format(gcell.len3D, fmt1) + "m" + vbTab +
                                                Format(gcell.tc1.depth, fmt1) + "m" + vbTab +
                                                Format(gcell.arcY, fmt1) + vbTab +
                                                Format(gcell.imageAngle, fmt1) + vbTab
                strOut += Format(algTask.accRadians.Y * 57.2958, fmt1) + vbCrLf

                SetTrueText(strOut, 3)
                labels(2) = vert.labels(3)
            End If
        End Sub
    End Class








    Public Class IMU_PlotAcceleration : Inherits TaskParent
        Dim plot As New Plot_OverTimeScalar
        Public Sub New()
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            desc = "Plot the IMU Acceleration in m/Sec^2 over time."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            SetTrueText("ts = " + Format(algTask.IMU_TimeStamp, fmt2) + vbCrLf + "X m/sec^2 = " + Format(algTask.IMU_Acceleration.X, fmt2) + vbCrLf +
                    "Y m/sec^2 = " + Format(algTask.IMU_Acceleration.Y, fmt2) + vbCrLf + "Z m/sec^2 = " + Format(algTask.IMU_Acceleration.Z, fmt2) + vbCrLf + vbCrLf +
                    "Motion (radians/sec) " + vbCrLf + "pitch = " + Format(algTask.IMU_AngularVelocity.X, fmt2) + vbCrLf +
                    "Yaw = " + Format(algTask.IMU_AngularVelocity.Y, fmt2) + vbCrLf + " Roll = " + Format(algTask.IMU_AngularVelocity.Z, fmt2), 1)

            plot.plotData = New cv.Scalar(algTask.IMU_Acceleration.X, algTask.IMU_Acceleration.Y, algTask.IMU_Acceleration.Z)
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
            If algTask.optionsChanged Then accList.Clear()
            accList.Add(algTask.IMU_RawAcceleration)
            Dim accMat = cv.Mat.FromPixelData(accList.Count, 1, cv.MatType.CV_64FC4, accList.ToArray)
            Dim imuMean = accMat.Mean()
            algTask.IMU_AverageAcceleration = New cv.Point3f(imuMean(0), imuMean(1), imuMean(2))
            If accList.Count >= algTask.frameHistoryCount Then accList.RemoveAt(0)
            strOut = "Average IMU acceleration: " + vbCrLf + Format(algTask.IMU_AverageAcceleration.X, fmt3) + vbTab + Format(algTask.IMU_AverageAcceleration.Y, fmt3) + vbTab +
                  Format(algTask.IMU_AverageAcceleration.Z, fmt3) + vbCrLf
            SetTrueText(strOut)
        End Sub
    End Class






    Public Class IMU_PlotCompareIMU : Inherits TaskParent
        Dim plot(3 - 1) As Plot_OverTimeScalar
        Dim imuAll As New IMU_AllMethods
        Public Sub New()
            If standalone Then algTask.gOptions.displayDst1.Checked = True
            If standalone Then algTask.gOptions.displaydst1.checked = True

            For i = 0 To plot.Count - 1
                plot(i) = New Plot_OverTimeScalar
                plot(i).plotCount = 4
            Next

            labels = {"IMU Acceleration in X", "IMU Acceleration in Y", "IMU Acceleration in Z", ""}
            desc = "Compare the results of the raw IMU data with the same values after Kalman"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            imuAll.Run(src)

            plot(0).plotData = New cv.Scalar(algTask.IMU_RawAcceleration.X, algTask.IMU_Acceleration.X, algTask.kalmanIMUacc.X, algTask.IMU_AverageAcceleration.X)
            plot(0).Run(src)
            dst0 = plot(0).dst2

            plot(1).plotData = New cv.Scalar(algTask.IMU_RawAcceleration.Y, algTask.IMU_Acceleration.Y, algTask.kalmanIMUacc.Y, algTask.IMU_AverageAcceleration.Y)
            plot(1).Run(src)
            dst1 = plot(1).dst2

            plot(2).plotData = New cv.Scalar(algTask.IMU_RawAcceleration.Z, algTask.IMU_Acceleration.Z, algTask.kalmanIMUacc.Z, algTask.IMU_AverageAcceleration.Z)
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
            If algTask.kalman Is Nothing Then algTask.kalman = New Kalman_Basics
            With algTask.kalman
                .kInput = {algTask.IMU_RawAcceleration.X, algTask.IMU_RawAcceleration.Y, algTask.IMU_RawAcceleration.Z,
                       algTask.IMU_RawAngularVelocity.X, algTask.IMU_RawAngularVelocity.Y, algTask.IMU_RawAngularVelocity.Z}
                .Run(src)
                algTask.kalmanIMUacc = New cv.Point3f(.kOutput(0), .kOutput(1), .kOutput(2))
                algTask.kalmanIMUvelocity = New cv.Point3f(.kOutput(3), .kOutput(4), .kOutput(5))
            End With
            strOut = "IMU Acceleration Raw" + vbTab + "IMU Velocity Raw" + vbCrLf +
                 Format(algTask.IMU_RawAcceleration.X, fmt3) + vbTab + Format(algTask.IMU_RawAcceleration.Y, fmt3) + vbTab +
                 Format(algTask.IMU_RawAcceleration.Z, fmt3) + vbTab + Format(algTask.IMU_RawAngularVelocity.X, fmt3) + vbTab +
                 Format(algTask.IMU_RawAngularVelocity.Y, fmt3) + vbTab + Format(algTask.IMU_RawAngularVelocity.Z, fmt3) + vbTab + vbCrLf + vbCrLf +
                 "kalmanIMUacc" + vbTab + vbTab + "kalmanIMUvelocity" + vbCrLf +
                 Format(algTask.kalmanIMUacc.X, fmt3) + vbTab + Format(algTask.kalmanIMUacc.Y, fmt3) + vbTab +
                 Format(algTask.kalmanIMUacc.Z, fmt3) + vbTab + Format(algTask.kalmanIMUvelocity.X, fmt3) + vbTab +
                 Format(algTask.kalmanIMUvelocity.Y, fmt3) + vbTab + Format(algTask.kalmanIMUvelocity.Z, fmt3) + vbTab
            SetTrueText(strOut)
        End Sub
    End Class







    Public Class IMU_AllMethods : Inherits TaskParent
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







    Public Class IMU_VelocityPlot : Inherits TaskParent
        Dim plot As New IMU_Plot
        Public Sub New()
            If standalone Then algTask.gOptions.displaydst1.checked = True
            desc = "Plot the angular velocity"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            algTask.pitch = algTask.IMU_AngularVelocity.X
            algTask.yaw = algTask.IMU_AngularVelocity.Y
            algTask.roll = algTask.IMU_AngularVelocity.Z

            plot.blueA = algTask.pitch * 1000
            plot.greenA = algTask.yaw * 1000
            plot.redA = algTask.roll * 1000
            plot.labels(2) = "pitch X 1000 (blue), Yaw X 1000 (green), and roll X 1000 (red)"

            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3

            If algTask.heartBeat Then
                strOut = "Pitch X1000 (blue): " + vbTab + Format(algTask.pitch * 1000, fmt1) + vbCrLf +
                     "Yaw X1000 (green): " + vbTab + Format(algTask.yaw * 1000, fmt1) + vbCrLf +
                     "Roll X1000 (red): " + vbTab + Format(algTask.roll * 1000, fmt1)
            End If
            SetTrueText(strOut, 1)
        End Sub
    End Class







    Public Class IMU_IscameraStable : Inherits TaskParent
        Dim plot As New IMU_Plot
        Dim options As New Options_IMU
        Public Sub New()
            desc = "Track the standard deviation of the angular velocities."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            algTask.pitch = algTask.IMU_AngularVelocity.X
            algTask.yaw = algTask.IMU_AngularVelocity.Y
            algTask.roll = algTask.IMU_AngularVelocity.Z
            If algTask.heartBeat Then
                strOut = "Pitch X1000 (blue): " + vbTab + Format(algTask.pitch * 1000, fmt1) + vbCrLf +
                     "Yaw X1000 (green): " + vbTab + Format(algTask.yaw * 1000, fmt1) + vbCrLf +
                     "Roll X1000 (red): " + vbTab + Format(algTask.roll * 1000, fmt1)
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

            Static CPUanchor As Integer = algTask.CPU_FrameTime
            Static hist(plot.maxScale) As Integer

            ' there can be some errant times at startup.
            If algTask.CPU_FrameTime > plot.maxScale Then algTask.CPU_FrameTime = plot.maxScale
            If algTask.CPU_FrameTime < 0 Then algTask.CPU_FrameTime = 0

            Dim maxval = Integer.MinValue
            For i = 0 To hist.Count - 1
                If maxval < hist(i) Then
                    maxval = hist(i)
                    CPUanchor = i
                End If
            Next

            Dim cpuFrameTime = CInt(algTask.CPU_FrameTime)
            If CPUanchor <> 0 Then cpuFrameTime = cpuFrameTime Mod CPUanchor
            HostInterruptDelayEstimate = CPUanchor - cpuFrameTime + options.minDelayHost
            If HostInterruptDelayEstimate > CPUanchor Then HostInterruptDelayEstimate -= CPUanchor
            If HostInterruptDelayEstimate < 0 Then HostInterruptDelayEstimate = options.minDelayHost

            Static sampledCPUFrameTime = algTask.CPU_FrameTime
            If algTask.heartBeat Then sampledCPUFrameTime = algTask.CPU_FrameTime

            hist(Math.Min(CInt(algTask.CPU_FrameTime), hist.Length - 1)) += 1

            If standaloneTest() Then
                Dim output = "IMU_TimeStamp (ms) " + Format(algTask.IMU_TimeStamp, "00") + vbCrLf +
                         "CPU TimeStamp (ms) " + Format(algTask.CPU_TimeStamp, "00") + vbCrLf +
                         "Host Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                         " CPUanchor = " + Format(CPUanchor, "00") +
                         " latest = " + Format(algTask.CPU_FrameTime, "00.00") + vbCrLf +
                         "Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00") + vbCrLf + vbCrLf +
                         "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                         "Green" + vbTab + "Host Frame Time" + vbCrLf +
                         "Red" + vbTab + "Host Total Delay (latency)" + vbCrLf +
                         "White" + vbTab + "Host Anchor Frame Time (Host Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

                plot.plotData = New cv.Scalar(algTask.IMU_FrameTime, algTask.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
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









    Public Class IMU_PlotHostFrameScalar : Inherits TaskParent
        Public plot As New Plot_OverTimeScalar
        Public CPUInterval As Double
        Public HostInterruptDelayEstimate As Double
        Dim options As New Options_IMUFrameTime
        Public Sub New()
            If standalone Then algTask.gOptions.displaydst1.checked = True
            plot.plotCount = 4
            labels(3) = "IMU (blue) Host (green) Latency est. (red) - all in ms"
            desc = "Use the Host timestamp to estimate the delay from image capture to host interrupt.  Just an estimate!"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Static CPUanchor As Integer = algTask.CPU_FrameTime

            Dim cpuFrameTime = CInt(algTask.CPU_FrameTime)
            If CPUanchor <> 0 Then cpuFrameTime = cpuFrameTime Mod CPUanchor
            HostInterruptDelayEstimate = CPUanchor - cpuFrameTime + options.minDelayHost
            If HostInterruptDelayEstimate > CPUanchor Then HostInterruptDelayEstimate -= CPUanchor
            If HostInterruptDelayEstimate < 0 Then HostInterruptDelayEstimate = options.minDelayHost

            Static sampledCPUFrameTime = algTask.CPU_FrameTime
            If algTask.heartBeat Then sampledCPUFrameTime = algTask.CPU_FrameTime

            If standaloneTest() Then
                strOut = "IMU_TimeStamp (ms) " + Format(algTask.IMU_TimeStamp, "00") + vbCrLf +
                     "CPU TimeStamp (ms) " + Format(algTask.CPU_TimeStamp, "00") + vbCrLf +
                     "Host Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                     " CPUanchor = " + Format(CPUanchor, "00") +
                     " latest = " + Format(algTask.CPU_FrameTime, "00.00") + vbCrLf +
                     "Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00") + vbCrLf + vbCrLf +
                     "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                     "Green" + vbTab + "Host Frame Time" + vbCrLf +
                     "Red" + vbTab + "Host Total Delay (latency)" + vbCrLf +
                     "White" + vbTab + "Host Anchor Frame Time (Host Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

                plot.plotData = New cv.Scalar(algTask.IMU_FrameTime, algTask.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
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

            If algTask.gOptions.gravityPointCloud.Checked Then
                '[cos(a) -sin(a)    0]
                '[sin(a)  cos(a)    0]
                '[0       0         1] rotate the point cloud around the x-axis.
                cz = Math.Cos(algTask.accRadians.Z)
                sz = Math.Sin(algTask.accRadians.Z)

                '[1       0         0      ] rotate the point cloud around the z-axis.
                '[0       cos(a)    -sin(a)]
                '[0       sin(a)    cos(a) ]
                cx = Math.Cos(algTask.accRadians.X)
                sx = Math.Sin(algTask.accRadians.X)
            Else
                getSliderValues()
            End If

            gMatrix = buildGmatrix()

            If standaloneTest() Then
                Dim g = algTask.IMU_Acceleration
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
                strOut += vbCrLf + "Gravity-oriented gMatrix - move camera to test this:" + vbCrLf + gMatrixToStr(tmpGMat1)

                getSliderValues()
                Dim tmpGMat2 = buildGmatrix()
                strOut += vbCrLf + "gMatrix with slider input - use Options_IMU Sliders to change this:" + vbCrLf + gMatrixToStr(tmpGMat2)
            End If
            SetTrueText(strOut)
            algTask.gMatrix = gMatrix
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
                Dim brick = brickCells(i)
                If brick.arcY > options.angleThreshold Then
                    index = brickCells.Count - i
                    Dim p1 = brick.tc1.center
                    Dim p2 = brick.tc2.center
                    Dim xOffset = p1.X - p2.X
                    If p1.Y < p2.Y Then xOffset = p2.X - p1.X
                    Dim hypot = p1.DistanceTo(p2)
                    brick.imageAngle = -Math.Asin(xOffset / hypot) * 57.2958

                    strOut += CStr(index) + vbTab + Format(brick.len3D, fmt1) + "m" + vbTab +
                                                Format(brick.tc1.depth, fmt1) + "m" + vbTab +
                                                Format(brick.arcY, fmt1) + vbTab +
                                                Format(brick.imageAngle, fmt1) + vbTab
                    strOut += Format(algTask.accRadians.Y * 57.2958, fmt1) + vbCrLf

                    SetTrueText(CStr(index), brick.tc1.center, 2)
                    SetTrueText(CStr(index), brick.tc1.center, 3)
                    vbc.DrawLine(dst2, brick.tc1.center, brick.tc2.center, algTask.highlight)
                    vbc.DrawLine(dst3, brick.tc1.center, brick.tc2.center, white)
                    brickCells(i) = brick
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
                blueA = algTask.IMU_AngularVelocity.X * 1000
                greenA = algTask.IMU_AngularVelocity.Y * 1000
                redA = algTask.IMU_AngularVelocity.Z * 1000
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