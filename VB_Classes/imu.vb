Imports System.Windows.Controls
Imports System.Windows.Ink
Imports System.Windows.Media.Imaging
Imports OpenCvSharp
Imports cv = OpenCvSharp
' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Public Class IMU_Basics : Inherits VB_Algorithm
    Dim lastTimeStamp As Double
    Public alpha As Double = 0.5
    Dim options As New Options_IMU
    Public Sub New()
        desc = "Read and display the IMU coordinates"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim gyroAngle As cv.Point3f
        If task.optionsChanged Then
            lastTimeStamp = task.IMU_TimeStamp
        Else
            gyroAngle = task.IMU_AngularVelocity
            Dim dt_gyro = (task.IMU_TimeStamp - lastTimeStamp) / 1000
            If task.cameraName <> "Intel(R) RealSense(TM) Depth Camera 435i" Then dt_gyro /= 1000 ' different units in the timestamp?
            gyroAngle = gyroAngle * dt_gyro
            task.theta += New cv.Point3f(-gyroAngle.Z, -gyroAngle.Y, gyroAngle.X)
            lastTimeStamp = task.IMU_TimeStamp
        End If

        ' NOTE: Initialize the angle around the y-axis to zero.
        Dim g = task.IMU_RawAcceleration
        task.accRadians = New cv.Point3f(Math.Atan2(g.X, Math.Sqrt(g.Y * g.Y + g.Z * g.Z)),
                                         Math.Abs(Math.Atan2(g.X, g.Y)), Math.Atan2(g.Y, g.Z))

        If task.optionsChanged Then
            task.theta = task.accRadians
        Else
            ' Apply the Complementary Filter:
            '  - high-pass filter = theta * alpha: allows short-duration signals to pass while filtering steady signals (trying to cancel drift)
            '  - low-pass filter = accel * (1 - alpha): lets the long-term changes through, filtering out short term fluctuations
            task.theta.X = task.theta.X * options.alpha + task.accRadians.X * (1 - options.alpha)
            task.theta.Y = task.accRadians.Y
            task.theta.Z = task.theta.Z * options.alpha + task.accRadians.Z * (1 - options.alpha)
        End If

        Dim x1 = -(90 + task.accRadians.X * 57.2958)
        Dim x2 = -(90 + task.theta.X * 57.2958)
        Dim y1 = task.accRadians.Y - cv.Cv2.PI
        If task.accRadians.X < 0 Then y1 *= -1
        strOut = "Angles in degree to gravity (before velocity filter)" + vbCrLf +
                 Format(x1, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(task.accRadians.Z * 57.2958, fmt1) + vbCrLf +
                 "Velocity-Filtered Angles to gravity in degrees" + vbCrLf +
                 Format(x2, fmt1) + vbTab + Format(y1 * 57.2958, fmt1) + vbTab + Format(task.theta.Z * 57.2958, fmt1) + vbCrLf
        setTrueText(strOut)

        task.accRadians = task.theta
        If task.accRadians.Y > cv.Cv2.PI / 2 Then task.accRadians.Y -= cv.Cv2.PI / 2
        task.accRadians.Z += cv.Cv2.PI / 2

        setTrueText(strOut)
    End Sub
End Class







Public Class IMU_Stabilize : Inherits VB_Algorithm
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(3 - 1)
        desc = "Stabilize IMU acceleration data."
        labels = {"", "", "IMU Stabilize (move camera around)", "Difference from Color Image"}
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim borderCrop = 5
        Dim vert_Border = borderCrop * src.Rows / src.Cols
        Dim dx = task.IMU_AngularVelocity.X
        Dim dy = task.IMU_AngularVelocity.Y
        Dim da = task.IMU_AngularVelocity.Z
        Dim sx = 1 ' assume no scaling is taking place.
        Dim sy = 1 ' assume no scaling is taking place.

        kalman.kInput = {dx, dy, da}
        kalman.Run(src)
        dx = kalman.kOutput(0)
        dy = kalman.kOutput(1)
        da = kalman.kOutput(2)

        Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
        smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
        smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
        smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
        smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
        smoothedMat.Set(Of Double)(0, 2, dx)
        smoothedMat.Set(Of Double)(1, 2, dy)

        Dim smoothedFrame = src.WarpAffine(smoothedMat, src.Size())
        smoothedFrame = smoothedFrame(New cv.Range(borderCrop, smoothedFrame.Rows - borderCrop), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
        dst2 = smoothedFrame.Resize(src.Size())
        cv.Cv2.Subtract(src, dst2, dst3)

        Dim Text = "dx = " + Format(dx, fmt2) + vbNewLine + "dy = " + Format(dy, fmt2) + vbNewLine + "da = " + Format(da, fmt2)
        setTrueText(Text, New cv.Point(10, 10), 3)
    End Sub
End Class






Public Class IMU_PlotIMUFrameTime : Inherits VB_Algorithm
    Public plot As New Plot_OverTime
    Public CPUInterval As Double
    Public IMUtoCaptureEstimate As Double
    Dim options As New Options_IMUFrameTime
    Public Sub New()
        plot.dst2 = dst3
        plot.maxScale = 40
        plot.minScale = -10
        plot.plotCount = 4

        labels(3) = "IMU (blue) Host (green) Latency est. (red) - all in ms"
        desc = "Use the IMU timestamp to estimate the delay from IMU capture to image capture.  Just an estimate!"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Static IMUanchor As Integer = task.IMU_FrameTime
        Static histogramIMU(plot.maxScale) As Integer

        ' there can be some errant times at startup.
        If CInt(task.IMU_FrameTime) >= histogramIMU.Length Then task.IMU_FrameTime = plot.maxScale
        If task.IMU_FrameTime < 0 Then task.IMU_FrameTime = 0

        Static imuTotalTime As Double
        imuTotalTime += task.IMU_FrameTime
        If imuTotalTime = 0 Then
            Static allZeroCount As Integer
            allZeroCount += 1
            If allZeroCount > 20 Then
                setTrueText("Is IMU present?  No IMU FrameTimes")
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

        Dim imuFrameTime = CInt(task.IMU_FrameTime)
        If IMUanchor <> 0 Then imuFrameTime = imuFrameTime Mod IMUanchor
        IMUtoCaptureEstimate = IMUanchor - imuFrameTime + options.minDelayIMU
        If IMUtoCaptureEstimate > IMUanchor Then IMUtoCaptureEstimate -= IMUanchor
        If IMUtoCaptureEstimate < options.minDelayIMU Then IMUtoCaptureEstimate = options.minDelayIMU

        Static sampledIMUFrameTime = task.IMU_FrameTime
        If heartBeat() Then sampledIMUFrameTime = task.IMU_FrameTime

        histogramIMU(Math.Min(CInt(task.IMU_FrameTime), histogramIMU.Length - 1)) += 1

        If standalone Then
            Dim output = "IMU_TimeStamp (ms) " + Format(task.IMU_TimeStamp, "00") + vbCrLf +
                        "CPU TimeStamp (ms) " + Format(task.CPU_TimeStamp, "00") + vbCrLf +
                        "IMU Frametime (ms, sampled) " + Format(sampledIMUFrameTime, "000.00") +
                        " IMUanchor = " + Format(IMUanchor, "00") +
                        " latest = " + Format(task.IMU_FrameTime, "00.00") + vbCrLf +
                        "IMUtoCapture (ms, sampled, in red) " + Format(IMUtoCaptureEstimate, "00") + vbCrLf + vbCrLf +
                        "IMU Frame Time = Blue" + vbCrLf +
                        "Host Frame Time = Green" + vbCrLf +
                        "IMU Total Delay = Red" + vbCrLf +
                        "IMU Anchor Frame Time = White (IMU Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

            plot.plotData = New cv.Scalar(task.IMU_FrameTime, task.CPU_FrameTime, IMUtoCaptureEstimate, IMUanchor)
            plot.Run(Nothing)

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
            setTrueText(output)
        End If
    End Sub
End Class





Public Class IMU_PlotTotalDelay : Inherits VB_Algorithm
    ReadOnly host As New IMU_PlotHostFrameTimes
    ReadOnly imu As New IMU_PlotIMUFrameTime
    ReadOnly plot As New Plot_OverTime
    ReadOnly kalman As New Kalman_Single
    Public Sub New()
        plot.dst2 = dst3
        plot.maxScale = 50
        plot.minScale = 0
        plot.plotCount = 4

        labels(2) = "Timing data - total (white) right image"
        labels(3) = "IMU (blue) Host (green) Latency est. (red) - all in ms"
        desc = "Estimate time from IMU capture to host processing to allow predicting effect of camera motion."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static countSlider = findSlider("Number of Plot Values")
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
        If heartBeat() Then
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
        plot.Run(Nothing)

        If plot.lastXdelta.Count > plotLastX Then
            For i = 0 To plot.plotCount - 1
                output += "Last " + CStr(plotLastX) + Choose(i + 1, " IMU Delay ", " Host Delay", " Total Delay ms", " Smoothed Total") + vbTab
                For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                    output += Format(plot.lastXdelta(j)(i), "00") + ", "
                Next
                output += vbCrLf
            Next
        End If
        setTrueText(output)
    End Sub
End Class









Public Class IMU_VerticalAngles : Inherits VB_Algorithm
    ReadOnly vert As New Line_GCloud
    Public Sub New()
        labels = {"", "", "Highlighted vertical lines", "Line details"}
        desc = "Compare the IMU changes to the angle changes in the vertical lines."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone

        vert.Run(src)

        Dim cells = vert.sortedVerticals
        strOut = "ID" + vbTab + "len3D" + vbTab + "Depth" + vbTab + "Arc X" + vbTab + "Arc Y" + vbTab + "Arc Z" + vbTab + "IMU X" + vbTab + "IMU Y" + vbTab + "IMU Z" + vbCrLf
        dst3.SetTo(0)
        For i = 0 To cells.Count - 1
            Dim gc = cells.ElementAt(i).Value
            strOut += CStr(i) + vbTab + Format(gc.len3D, fmt1) + "m" + vbTab + Format(gc.tc1.depth, fmt1) + "m" + vbTab +
                      Format(gc.arcX, fmt1) + vbTab + Format(gc.arcY, fmt1) + vbTab + Format(gc.arcZ, fmt1) + vbTab
            strOut += Format(task.accRadians.X * 57.2958, fmt1) + vbTab + Format(task.accRadians.Y * 57.2958, fmt1) + vbTab + Format(task.accRadians.Z * 57.2958, fmt1) + vbTab + vbCrLf
            setTrueText(CStr(i), gc.tc1.center, 2)
            setTrueText(CStr(i), gc.tc1.center, 3)
            dst2.Line(gc.tc1.center, gc.tc2.center, task.highlightColor, task.lineWidth, task.lineType)
            dst3.Line(gc.tc1.center, gc.tc2.center, cv.Scalar.White, task.lineWidth, task.lineType)
        Next
        setTrueText(strOut, 3)
    End Sub
End Class








Public Class IMU_PlotGravityAngles : Inherits VB_Algorithm
    ReadOnly plot As New Plot_OverTimeScalar
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Plot the motion of the camera based on the IMU data in degrees"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("ts = " + Format(task.IMU_TimeStamp, fmt2) + vbCrLf + "X degrees = " + Format(task.accRadians.X * 57.2958, fmt3) + vbCrLf +
                    "Y degrees = " + Format(Math.Abs(task.accRadians.Y * 57.2958), fmt3) + vbCrLf + "Z degrees = " + Format(task.accRadians.Z * 57.2958, fmt2) + vbCrLf + vbCrLf +
                    "Motion (radians/sec) " + vbCrLf + "pitch = " + Format(task.IMU_AngularVelocity.X, fmt2) + vbCrLf +
                    "Yaw = " + Format(task.IMU_AngularVelocity.Y, fmt2) + vbCrLf + " Roll = " + Format(task.IMU_AngularVelocity.Z, fmt2), 1)

        plot.plotData = New cv.Scalar(task.accRadians.X * 57.2958, task.accRadians.Y * 57.2958, task.accRadians.Z * 57.2958)
        plot.Run(Nothing)
        dst2 = plot.dst2
        dst3 = plot.dst3
    End Sub
End Class









Public Class IMU_PlotAngularVelocity : Inherits VB_Algorithm
    ReadOnly plot As New Plot_OverTimeScalar
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Plot the IMU Velocity over time."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("ts = " + Format(task.IMU_TimeStamp, fmt2) + vbCrLf + "X m/sec^2 = " + Format(task.IMU_Acceleration.X, fmt2) + vbCrLf +
                    "Y m/sec^2 = " + Format(task.IMU_Acceleration.Y, fmt2) + vbCrLf + "Z m/sec^2 = " + Format(task.IMU_Acceleration.Z, fmt2) + vbCrLf + vbCrLf +
                    "Motion (radians/sec) " + vbCrLf + "X - Pitch = " + Format(task.IMU_AngularVelocity.X, fmt2) + vbCrLf +
                    "Y - Yaw = " + Format(task.IMU_AngularVelocity.Y, fmt2) + vbCrLf + "Z - Roll = " + Format(task.IMU_AngularVelocity.Z, fmt2) + vbCrLf + vbCrLf +
                    "Move the camera to move values off of zero...", 1)

        plot.plotData = New cv.Scalar(task.IMU_AngularVelocity.X, task.IMU_AngularVelocity.Y, task.IMU_AngularVelocity.Z)
        plot.Run(Nothing)
        dst2 = plot.dst2
        dst3 = plot.dst3
    End Sub
End Class








Public Class IMU_VerticalVerify : Inherits VB_Algorithm
    Public gCells As New List(Of gravityLine)
    Public Sub New()
        labels = {"", "", "Highlighted vertical lines", "Line details"}
        desc = "Use the Y-Arc to confirm which vertical lines are valid"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone

        If standalone Then
            Static linesVH As New Feature_LinesVH
            linesVH.Run(src)
            gCells = linesVH.gCells
        End If

        Static arcYslider = findSlider("Minimum Arc-Y threshold angle (degrees)")
        Dim angleThreshold = arcYslider.Value

        strOut = "ID" + vbTab + "len3D" + vbTab + "Depth" + vbTab + "Arc Y" + vbTab + "Image" + vbTab + "IMU Y" + vbTab + vbCrLf
        dst3.SetTo(0)
        Dim index As Integer
        For i = gCells.Count - 1 To 0 Step -1
            Dim gc = gCells(i)
            If gc.arcY > angleThreshold Then
                index = gCells.Count - i
                Dim p1 = gc.tc1.center
                Dim p2 = gc.tc2.center
                Dim xOffset = p1.X - p2.X
                If p1.Y < p2.Y Then xOffset = p2.X - p1.X
                Dim hypot = p1.DistanceTo(p2)
                gc.imageAngle = -Math.Asin(xOffset / hypot) * 57.2958

                strOut += CStr(index) + vbTab + Format(gc.len3D, fmt1) + "m" + vbTab +
                                                Format(gc.tc1.depth, fmt1) + "m" + vbTab +
                                                Format(gc.arcY, fmt1) + vbTab +
                                                Format(gc.imageAngle, fmt1) + vbTab
                strOut += Format(task.accRadians.Y * 57.2958, fmt1) + vbCrLf

                setTrueText(CStr(index), gc.tc1.center, 2)
                setTrueText(CStr(index), gc.tc1.center, 3)
                dst2.Line(gc.tc1.center, gc.tc2.center, task.highlightColor, task.lineWidth, task.lineType)
                dst3.Line(gc.tc1.center, gc.tc2.center, cv.Scalar.White, task.lineWidth, task.lineType)
                gCells(i) = gc
            Else
                gCells.RemoveAt(i)
            End If
        Next
        setTrueText(strOut, 3)
    End Sub
End Class







Public Class IMU_Lines : Inherits VB_Algorithm
    ReadOnly vert As New Line_GCloud
    ReadOnly kalman As New Kalman_Basics
    Public Sub New()
        labels(2) = "Vertical lines in Blue and horizontal lines in Yellow"
        desc = "Find the vertical and horizontal lines"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        vert.Run(src)
        dst2 = vert.dst2
        Static lastGcell As gravityLine
        Dim gcell As gravityLine
        Dim cells = vert.sortedVerticals
        If cells.Count > 0 Then gcell = cells.ElementAt(0).Value Else gcell = lastGcell
        If gcell.len3D > 0 Then
            strOut = "ID" + vbTab + "len3D" + vbTab + "Depth" + vbTab + "Arc Y" + vbTab + "Image" + vbTab + "IMU Y" + vbTab + vbCrLf
            If heartBeat() Then dst3.SetTo(0)
            Dim p1 = gcell.tc1.center
            Dim p2 = gcell.tc2.center
            Dim lastP1 = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
            Dim lastp2 = New cv.Point(kalman.kOutput(2), kalman.kOutput(3))

            kalman.kInput = {p1.X, p1.Y, p2.X, p2.Y}
            kalman.Run(Nothing)

            p1 = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
            p2 = New cv.Point(kalman.kOutput(2), kalman.kOutput(3))
            dst2.Circle(p1, task.dotSize, task.highlightColor, -1, task.lineType)
            dst2.Circle(p2, task.dotSize, task.highlightColor, -1, task.lineType)
            dst3.Circle(p1, task.dotSize, cv.Scalar.White, -1, task.lineType)

            dst3.Circle(p2, task.dotSize, cv.Scalar.White, -1, task.lineType)
            lastGcell = gcell
            strOut += CStr(0) + vbTab + Format(gcell.len3D, fmt1) + "m" + vbTab +
                                                Format(gcell.tc1.depth, fmt1) + "m" + vbTab +
                                                Format(gcell.arcY, fmt1) + vbTab +
                                                Format(gcell.imageAngle, fmt1) + vbTab
            strOut += Format(task.accRadians.Y * 57.2958, fmt1) + vbCrLf

            setTrueText(strOut, 3)
            labels(2) = vert.labels(3)
        End If
    End Sub
End Class








Public Class IMU_PlotAcceleration : Inherits VB_Algorithm
    ReadOnly plot As New Plot_OverTimeScalar
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Plot the IMU Acceleration in m/Sec^2 over time."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("ts = " + Format(task.IMU_TimeStamp, fmt2) + vbCrLf + "X m/sec^2 = " + Format(task.IMU_Acceleration.X, fmt2) + vbCrLf +
                    "Y m/sec^2 = " + Format(task.IMU_Acceleration.Y, fmt2) + vbCrLf + "Z m/sec^2 = " + Format(task.IMU_Acceleration.Z, fmt2) + vbCrLf + vbCrLf +
                    "Motion (radians/sec) " + vbCrLf + "pitch = " + Format(task.IMU_AngularVelocity.X, fmt2) + vbCrLf +
                    "Yaw = " + Format(task.IMU_AngularVelocity.Y, fmt2) + vbCrLf + " Roll = " + Format(task.IMU_AngularVelocity.Z, fmt2), 1)

        plot.plotData = New cv.Scalar(task.IMU_Acceleration.X, task.IMU_Acceleration.Y, task.IMU_Acceleration.Z)
        plot.Run(Nothing)
        dst2 = plot.dst2
        dst3 = plot.dst3
    End Sub
End Class







Public Class IMU_Average : Inherits VB_Algorithm
    Public Sub New()
        desc = "Average the IMU Acceleration values over the previous X images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static accList As New List(Of cv.Scalar)
        If task.optionsChanged Then accList.Clear()
        accList.Add(task.IMU_RawAcceleration)
        Dim accMat = New cv.Mat(accList.Count, 1, cv.MatType.CV_64FC4, accList.ToArray)
        Dim imuMean = accMat.Mean()
        task.IMU_AverageAcceleration = New cv.Point3f(imuMean(0), imuMean(1), imuMean(2))
        If accList.Count >= task.historyCount Then accList.RemoveAt(0)
        strOut = "Average IMU acceleration: " + vbCrLf + Format(task.IMU_AverageAcceleration.X, fmt3) + vbTab + Format(task.IMU_AverageAcceleration.Y, fmt3) + vbTab +
                  Format(task.IMU_AverageAcceleration.Z, fmt3) + vbCrLf
        setTrueText(strOut)
    End Sub
End Class






Public Class IMU_PlotCompareIMU : Inherits VB_Algorithm
    ReadOnly plot(3 - 1) As Plot_OverTimeScalar
    ReadOnly imuAll As New IMU_AllMethods
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True

        For i = 0 To plot.Count - 1
            plot(i) = New Plot_OverTimeScalar
            plot(i).plotCount = 4
        Next

        labels = {"IMU Acceleration in X", "IMU Acceleration in Y", "IMU Acceleration in Z", ""}
        desc = "Compare the results of the raw IMU data with the same values after Kalman"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        imuAll.Run(Nothing)

        plot(0).plotData = New cv.Scalar(task.IMU_RawAcceleration.X, task.IMU_Acceleration.X, task.kalmanIMUacc.X, task.IMU_AverageAcceleration.X)
        plot(0).Run(Nothing)
        dst0 = plot(0).dst2

        plot(1).plotData = New cv.Scalar(task.IMU_RawAcceleration.Y, task.IMU_Acceleration.Y, task.kalmanIMUacc.Y, task.IMU_AverageAcceleration.Y)
        plot(1).Run(Nothing)
        dst1 = plot(1).dst2

        plot(2).plotData = New cv.Scalar(task.IMU_RawAcceleration.Z, task.IMU_Acceleration.Z, task.kalmanIMUacc.Z, task.IMU_AverageAcceleration.Z)
        plot(2).Run(Nothing)
        dst2 = plot(2).dst2

        setTrueText("Blue (usually hidden) is the raw signal" + vbCrLf + "Green (usually hidden) is the Velocity-filtered results" + vbCrLf +
                    "Red is the Kalman IMU data" + vbCrLf + "White is the IMU Averaging output (note delay from Kalman output)" + vbCrLf + vbCrLf +
                    "Move the camera around to see the impact on the IMU data." + vbCrLf +
                    "Adjust the global option 'Frame History' to see the impact." + vbCrLf + vbCrLf +
                    "Remember that IMU Data filtering only impacts the X and Z values." + vbCrLf +
                    "Averaging seems to track closer but is not as timely.", 3)
    End Sub
End Class









Public Class IMU_Kalman : Inherits VB_Algorithm
    ReadOnly kalman As New Kalman_Basics
    Public Sub New()
        desc = "Use Kalman Filter to stabilize the IMU acceleration and velocity"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kalman.kInput = {task.IMU_RawAcceleration.X, task.IMU_RawAcceleration.Y, task.IMU_RawAcceleration.Z,
                         task.IMU_RawAngularVelocity.X, task.IMU_RawAngularVelocity.Y, task.IMU_RawAngularVelocity.Z}
        kalman.Run(Nothing)
        task.kalmanIMUacc = New cv.Point3f(kalman.kOutput(0), kalman.kOutput(1), kalman.kOutput(2))
        task.kalmanIMUvelocity = New cv.Point3f(kalman.kOutput(3), kalman.kOutput(4), kalman.kOutput(5))
        strOut = "IMU Acceleration Raw" + vbTab + "IMU Velocity Raw" + vbCrLf +
                 Format(task.IMU_RawAcceleration.X, fmt3) + vbTab + Format(task.IMU_RawAcceleration.Y, fmt3) + vbTab +
                 Format(task.IMU_RawAcceleration.Z, fmt3) + vbTab + Format(task.IMU_RawAngularVelocity.X, fmt3) + vbTab +
                 Format(task.IMU_RawAngularVelocity.Y, fmt3) + vbTab + Format(task.IMU_RawAngularVelocity.Z, fmt3) + vbTab + vbCrLf + vbCrLf +
                 "kalmanIMUacc" + vbTab + vbTab + "kalmanIMUvelocity" + vbCrLf +
                 Format(task.kalmanIMUacc.X, fmt3) + vbTab + Format(task.kalmanIMUacc.Y, fmt3) + vbTab +
                 Format(task.kalmanIMUacc.Z, fmt3) + vbTab + Format(task.kalmanIMUvelocity.X, fmt3) + vbTab +
                 Format(task.kalmanIMUvelocity.Y, fmt3) + vbTab + Format(task.kalmanIMUvelocity.Z, fmt3) + vbTab
        setTrueText(strOut)
    End Sub
End Class







Public Class IMU_AllMethods : Inherits VB_Algorithm
    Dim basics As New IMU_Basics
    ReadOnly imuAvg As New IMU_Average
    Dim kalman As New IMU_Kalman
    Public Sub New()
        desc = "Compute the IMU acceleration using all available methods - raw, Kalman, averaging, and velocity-filtered."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        basics.Run(Nothing)
        kalman.Run(Nothing)
        imuAvg.Run(Nothing)

        setTrueText(basics.strOut + vbCrLf + kalman.strOut + vbCrLf + vbCrLf + imuAvg.strOut, 2)
    End Sub
End Class






' https://www.codeproject.com/Articles/1247960/3D-graphics-engine-with-basic-math-on-CPU
Public Class IMU_GMatrix : Inherits VB_Algorithm
    Public cx As Single = 1, sx As Single = 0, cy As Single = 1, sy As Single = 0, cz As Single = 1, sz As Single = 0
    Public gMatrix As cv.Mat
    Public Sub New()
        desc = "Find the angle of tilt for the camera with respect to gravity."
    End Sub
    Private Sub getSliderValues()
        Static xSlider = findSlider("Rotate pointcloud around X-axis (degrees)")
        Static ySlider = findSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zSlider = findSlider("Rotate pointcloud around Z-axis (degrees)")
        cx = Math.Cos(xSlider.value * cv.Cv2.PI / 180)
        sx = Math.Sin(xSlider.value * cv.Cv2.PI / 180)

        cy = Math.Cos(ySlider.Value * cv.Cv2.PI / 180)
        sy = Math.Sin(ySlider.Value * cv.Cv2.PI / 180)

        cz = Math.Cos(zSlider.value * cv.Cv2.PI / 180)
        sz = Math.Sin(zSlider.value * cv.Cv2.PI / 180)
    End Sub
    Private Function buildGmatrix() As cv.Mat
        '[cx -sx    0]  [1  0   0 ] 
        '[sx  cx    0]  [0  cz -sz]
        '[0   0     1]  [0  sz  cz]
        Dim gArray As Single(,) = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                                   {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                                   {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}

        Dim tmpGMatrix = New cv.Mat(3, 3, cv.MatType.CV_32F, {
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
    Public Sub RunVB(src As cv.Mat)
        Static xSlider = findSlider("Rotate pointcloud around X-axis (degrees)")
        Static ySlider = findSlider("Rotate pointcloud around Y-axis (degrees)")
        Static zSlider = findSlider("Rotate pointcloud around Z-axis (degrees)")

        If gOptions.gravityPointCloud.Checked Then
            '[cos(a) -sin(a)    0]
            '[sin(a)  cos(a)    0]
            '[0       0         1] rotate the point cloud around the x-axis.
            cz = Math.Cos(task.accRadians.Z)
            sz = Math.Sin(task.accRadians.Z)

            '[1       0         0      ] rotate the point cloud around the z-axis.
            '[0       cos(a)    -sin(a)]
            '[0       sin(a)    cos(a) ]
            cx = Math.Cos(task.accRadians.X)
            sx = Math.Sin(task.accRadians.X)
        Else
            getSliderValues()
        End If

        gMatrix = buildGmatrix()

        If standalone Then
            Dim g = task.IMU_Acceleration
            strOut = "IMU Acceleration in X-direction = " + vbTab + vbTab + Format(g.X, fmt4) + vbCrLf
            strOut += "IMU Acceleration in Y-direction = " + vbTab + vbTab + Format(g.Y, fmt4) + vbCrLf
            strOut += "IMU Acceleration in Z-direction = " + vbTab + vbTab + Format(g.Z, fmt4) + vbCrLf + vbCrLf
            strOut += "Rotate around X-axis (in degrees) = " + vbTab + Format(xSlider.value, fmt4) + vbCrLf
            strOut += "Rotate around Y-axis (in degrees) = " + vbTab + Format(ySlider.value, fmt4) + vbCrLf
            strOut += "Rotate around Z-axis (in degrees) = " + vbTab + Format(zSlider.value, fmt4) + vbCrLf

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
        setTrueText(strOut)
        task.gMatrix = gMatrix
    End Sub
End Class









Public Class IMU_Plot : Inherits VB_Algorithm
    Dim plot As New Plot_OverTimeScalar
    Public blue As Single, green As Single, red As Single
    Public Sub New()
        If findfrm(traceName + " CheckBox Options") Is Nothing Then
            check.Setup(traceName)
            check.addCheckBox("Blue Variable")
            check.addCheckBox("Green Variable")
            check.addCheckBox("Red Variable")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
            check.Box(2).Checked = True
        End If

        plot.plotCount = 3
        desc = "Plot the angular velocity of the camera based on the IMU data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            blue = task.IMU_AngularVelocity.X * 1000
            green = task.IMU_AngularVelocity.Y * 1000
            red = task.IMU_AngularVelocity.Z * 1000
        End If

        Static blueCheck = findCheckBox("Blue Variable")
        Static greenCheck = findCheckBox("Green Variable")
        Static redCheck = findCheckBox("Red Variable")

        Dim blueX As Single, greenX As Single, redX As Single

        If blueCheck.checked Then blueX = blue
        If greenCheck.checked Then greenX = green
        If redCheck.checked Then redX = red

        plot.plotData = New cv.Scalar(blueX, greenX, redX)
        plot.Run(Nothing)
        dst2 = plot.dst2
        dst3 = plot.dst3
        labels(2) = "When run standalone, the default is to plot the angular velocity for X, Y, and Z"
    End Sub
End Class






Public Class IMU_VelocityPlot : Inherits VB_Algorithm
    Dim plot As New IMU_Plot
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Plot the angular velocity"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.pitch = task.IMU_AngularVelocity.X
        task.yaw = task.IMU_AngularVelocity.Y
        task.roll = task.IMU_AngularVelocity.Z

        plot.blue = task.pitch * 1000
        plot.green = task.yaw * 1000
        plot.red = task.roll * 1000
        plot.labels(2) = "pitch X 1000 (blue), Yaw X 1000 (green), and roll X 1000 (red)"

        plot.Run(Nothing)
        dst2 = plot.dst2
        dst3 = plot.dst3

        If heartBeat() Then
            strOut = "Pitch X1000 (blue): " + vbTab + Format(task.pitch * 1000, fmt1) + vbCrLf +
                     "Yaw X1000 (green): " + vbTab + Format(task.yaw * 1000, fmt1) + vbCrLf +
                     "Roll X1000 (red): " + vbTab + Format(task.roll * 1000, fmt1)
        End If
        setTrueText(strOut, 1)
    End Sub
End Class







Public Class IMU_IscameraStable : Inherits VB_Algorithm
    Dim plot As New IMU_Plot
    Dim options As New Options_IMU
    Public Sub New()
        desc = "Track the standard deviation of the angular velocities."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        task.pitch = task.IMU_AngularVelocity.X
        task.yaw = task.IMU_AngularVelocity.Y
        task.roll = task.IMU_AngularVelocity.Z
        If heartBeat() Then
            strOut = "Pitch X1000 (blue): " + vbTab + Format(task.pitch * 1000, fmt1) + vbCrLf +
                     "Yaw X1000 (green): " + vbTab + Format(task.yaw * 1000, fmt1) + vbCrLf +
                     "Roll X1000 (red): " + vbTab + Format(task.roll * 1000, fmt1)
        End If
        setTrueText(strOut, 3)
    End Sub
End Class





Public Class IMU_PlotHostFrameTimes : Inherits VB_Algorithm
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
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Static CPUanchor As Integer = task.CPU_FrameTime
        Static hist(plot.maxScale) As Integer

        ' there can be some errant times at startup.
        If task.CPU_FrameTime > plot.maxScale Then task.CPU_FrameTime = plot.maxScale
        If task.CPU_FrameTime < 0 Then task.CPU_FrameTime = 0

        Dim maxval = Integer.MinValue
        For i = 0 To hist.Count - 1
            If maxval < hist(i) Then
                maxval = hist(i)
                CPUanchor = i
            End If
        Next

        Dim cpuFrameTime = CInt(task.CPU_FrameTime)
        If CPUanchor <> 0 Then cpuFrameTime = cpuFrameTime Mod CPUanchor
        HostInterruptDelayEstimate = CPUanchor - cpuFrameTime + options.minDelayHost
        If HostInterruptDelayEstimate > CPUanchor Then HostInterruptDelayEstimate -= CPUanchor
        If HostInterruptDelayEstimate < 0 Then HostInterruptDelayEstimate = options.minDelayHost

        Static sampledCPUFrameTime = task.CPU_FrameTime
        If heartBeat() Then sampledCPUFrameTime = task.CPU_FrameTime

        hist(Math.Min(CInt(task.CPU_FrameTime), hist.Length - 1)) += 1

        If standalone Then
            Dim output = "IMU_TimeStamp (ms) " + Format(task.IMU_TimeStamp, "00") + vbCrLf +
                         "CPU TimeStamp (ms) " + Format(task.CPU_TimeStamp, "00") + vbCrLf +
                         "Host Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                         " CPUanchor = " + Format(CPUanchor, "00") +
                         " latest = " + Format(task.CPU_FrameTime, "00.00") + vbCrLf +
                         "Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00") + vbCrLf + vbCrLf +
                         "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                         "Green" + vbTab + "Host Frame Time" + vbCrLf +
                         "Red" + vbTab + "Host Total Delay (latency)" + vbCrLf +
                         "White" + vbTab + "Host Anchor Frame Time (Host Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

            plot.plotData = New cv.Scalar(task.IMU_FrameTime, task.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
            plot.Run(Nothing)

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
            setTrueText(output)
        End If
    End Sub
End Class









Public Class IMU_PlotHostFrameScalar : Inherits VB_Algorithm
    Public plot As New Plot_OverTimeScalar
    Public CPUInterval As Double
    Public HostInterruptDelayEstimate As Double
    Dim options As New Options_IMUFrameTime
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        plot.plotCount = 4
        labels(3) = "IMU (blue) Host (green) Latency est. (red) - all in ms"
        desc = "Use the Host timestamp to estimate the delay from image capture to host interrupt.  Just an estimate!"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Static CPUanchor As Integer = task.CPU_FrameTime
        Static histList As New List(Of Integer)

        Dim cpuFrameTime = CInt(task.CPU_FrameTime)
        If CPUanchor <> 0 Then cpuFrameTime = cpuFrameTime Mod CPUanchor
        HostInterruptDelayEstimate = CPUanchor - cpuFrameTime + options.minDelayHost
        If HostInterruptDelayEstimate > CPUanchor Then HostInterruptDelayEstimate -= CPUanchor
        If HostInterruptDelayEstimate < 0 Then HostInterruptDelayEstimate = options.minDelayHost

        Static sampledCPUFrameTime = task.CPU_FrameTime
        If heartBeat() Then sampledCPUFrameTime = task.CPU_FrameTime

        histList.Add(cpuFrameTime)

        If standalone Then
            strOut = "IMU_TimeStamp (ms) " + Format(task.IMU_TimeStamp, "00") + vbCrLf +
                     "CPU TimeStamp (ms) " + Format(task.CPU_TimeStamp, "00") + vbCrLf +
                     "Host Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                     " CPUanchor = " + Format(CPUanchor, "00") +
                     " latest = " + Format(task.CPU_FrameTime, "00.00") + vbCrLf +
                     "Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00") + vbCrLf + vbCrLf +
                     "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                     "Green" + vbTab + "Host Frame Time" + vbCrLf +
                     "Red" + vbTab + "Host Total Delay (latency)" + vbCrLf +
                     "White" + vbTab + "Host Anchor Frame Time (Host Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

            plot.plotData = New cv.Scalar(task.IMU_FrameTime, task.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
            plot.Run(Nothing)
            dst2 = plot.dst2
            dst3 = plot.dst3
            setTrueText(strOut, 1)
        End If
    End Sub
End Class