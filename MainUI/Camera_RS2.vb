Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.JavaScript.JSType
Imports System.Threading
Imports Intel.RealSense
Imports cv = OpenCvSharp

Namespace MainApp
    Public Class Camera_RS2 : Inherits GenericCamera
        Dim pipe As New Pipeline()
        Dim motionSensor As Sensor
        Dim imuStartTime As Double
        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, devName As String, emitterOn As Integer,
                       Optional fps As Integer = 30)
            captureRes = _captureRes
            workRes = _workRes
            Dim isD455 = devName.EndsWith("455")
            Dim serialNumber As String = ""
            Dim ctx As New Context()
            Dim searchName As String = If(isD455, "D455", "D435i")
            For Each dev In ctx.QueryDevices()
                If dev.Info.Item(0).Contains(searchName) Then serialNumber = dev.Info.Item(1)
            Next

            Dim cfg As New Config()
            cfg.EnableDevice(serialNumber)

            cfg.EnableStream(Stream.Color, captureRes.Width, captureRes.Height, Format.Bgr8, fps)
            cfg.EnableStream(Stream.Infrared, 1, captureRes.Width, captureRes.Height, Format.Y8, fps)
            cfg.EnableStream(Stream.Infrared, 2, captureRes.Width, captureRes.Height, Format.Y8, fps)
            cfg.EnableStream(Stream.Depth, captureRes.Width, captureRes.Height, Format.Z16, fps)
            ' D435i accel is 63 Hz. A D455 made after mid-2022 has no 63 Hz profile
            ' (minimum is 100). Leaving motion in this frameset makes WaitForFrames
            ' wait for a sample that never arrives and time out at 5 seconds.
            If Not isD455 Then
                cfg.EnableStream(Stream.Accel, Format.MotionXyz32f, 63)
                cfg.EnableStream(Stream.Gyro, Format.MotionXyz32f, 200)
            End If

            Dim profiles = pipe.Start(cfg)

            Dim device As Device = profiles.Device
            For Each s In device.Sensors
                ' The default queue holds seconds of frames, so the picture lags the scene.
                ' Two slots is the depth-plus-color size. Global time asks the device clock
                ' on every frame, and that query is the long hardware delay.
                TrySetOption(s, Intel.RealSense.Option.FramesQueueSize, 2)
                TrySetOption(s, Intel.RealSense.Option.GlobalTimeEnabled, 0)
                If s.Info.Item(0).Contains("Stereo Module") Then
                    s.Options(Intel.RealSense.Option.EmitterEnabled).Value = emitterOn
                    ' s.Options(Intel.RealSense.Option.LaserPower).Value = 13
                End If
            Next

            Dim StreamColor = profiles.GetStream(Stream.Color)
            Dim streamLeft = profiles.GetStream(Stream.Infrared, 1)
            Dim streamRight = profiles.GetStream(Stream.Infrared, 2)

            Dim leftIntrin As Intrinsics = streamLeft.As(Of VideoStreamProfile)().GetIntrinsics()
            Dim leftExtrinsics As Extrinsics = streamLeft.As(Of VideoStreamProfile)().GetExtrinsicsTo(streamRight)

            Dim rgb As Intrinsics = StreamColor.As(Of VideoStreamProfile)().GetIntrinsics()
            Dim rgbExtrinsics As Extrinsics = StreamColor.As(Of VideoStreamProfile)().GetExtrinsicsTo(streamLeft)

            Dim ratio = captureRes.Width \ workRes.Width
            calibData.rgbIntrinsics.ppx = rgb.ppx / ratio
            calibData.rgbIntrinsics.ppy = rgb.ppy / ratio
            calibData.rgbIntrinsics.fx = rgb.fx / ratio
            calibData.rgbIntrinsics.fy = rgb.fy / ratio

            calibData.leftIntrinsics.fx = leftIntrin.fx / ratio
            calibData.leftIntrinsics.fy = leftIntrin.fy / ratio
            calibData.leftIntrinsics.ppx = leftIntrin.ppx / ratio
            calibData.leftIntrinsics.ppy = leftIntrin.ppy / ratio

            ReDim calibData.LtoR_translation(3 - 1)
            ReDim calibData.LtoR_rotation(9 - 1)

            ReDim calibData.ColorToLeft_translation(3 - 1)
            ReDim calibData.ColorToLeft_rotation(9 - 1)

            For i = 0 To 3 - 1
                calibData.LtoR_translation(i) = leftExtrinsics.translation(i)
            Next
            For i = 0 To 9 - 1
                calibData.LtoR_rotation(i) = leftExtrinsics.rotation(i)
            Next

            For i = 0 To 3 - 1
                calibData.ColorToLeft_translation(i) = rgbExtrinsics.translation(i)
            Next
            For i = 0 To 9 - 1
                calibData.ColorToLeft_rotation(i) = rgbExtrinsics.rotation(i)
            Next

            calibData.baseline = System.Math.Sqrt(System.Math.Pow(calibData.LtoR_translation(0), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(1), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(2), 2))
            MyBase.prepImages()
            If isD455 Then StartD455Motion(profiles.Device)

            ' Start background thread to capture frames
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "CaptureThread_RS2"
            captureThread.Start()
        End Sub
        Private Sub CaptureFrames()
            While isCapturing
                GetNextFrame()
            End While
        End Sub
        ''' <summary>
        ''' D455 motion stays on its own sensor. The video pipeline then waits only
        ''' for color, depth, and infrared, which do arrive.
        ''' </summary>
        Private Sub StartD455Motion(device As Device)
            Try
                For Each s In device.Sensors
                    If Not s.Info.Item(0).Contains("Motion") Then Continue For
                    Dim profileList = s.StreamProfiles
                    Dim accelProfile As StreamProfile = Nothing
                    Dim gyroProfile As StreamProfile = Nothing
                    For i = 0 To profileList.Count - 1
                        Dim p = profileList(i)
                        If p.Stream = Stream.Accel AndAlso p.Framerate = 100 Then accelProfile = p
                        If p.Stream = Stream.Gyro AndAlso p.Framerate = 200 Then gyroProfile = p
                    Next
                    If accelProfile Is Nothing OrElse gyroProfile Is Nothing Then
                        For i = 0 To profileList.Count - 1
                            Dim p = profileList(i)
                            If accelProfile Is Nothing AndAlso p.Stream = Stream.Accel Then accelProfile = p
                            If gyroProfile Is Nothing AndAlso p.Stream = Stream.Gyro Then gyroProfile = p
                        Next
                    End If
                    If accelProfile Is Nothing OrElse gyroProfile Is Nothing Then Exit Sub
                    s.Open(New StreamProfile() {accelProfile, gyroProfile})
                    s.Start(AddressOf OnMotion)
                    motionSensor = s
                    Exit Sub
                Next
            Catch ex As Exception
                Debug.WriteLine("D455 motion start: " + ex.Message)
            End Try
        End Sub
        Private Sub TrySetOption(sensor As Sensor, opt As Intel.RealSense.Option, value As Single)
            Try
                sensor.Options(opt).Value = value
            Catch
                ' This sensor does not have the option.
            End Try
        End Sub
        Private Sub OnMotion(frame As Frame)
            If frame Is Nothing OrElse frame.Profile Is Nothing Then Return
            If frame.Profile.Stream = Stream.Accel Then
                IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(frame.Data)
            ElseIf frame.Profile.Stream = Stream.Gyro Then
                IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(frame.Data)
                Dim mFrame = frame.As(Of MotionFrame)()
                If imuStartTime = 0 Then imuStartTime = mFrame.Timestamp
                IMU_FrameTime = mFrame.Timestamp - imuStartTime
            End If
        End Sub
        Private Sub CopyToWorkRes(data As IntPtr, srcRows As Integer, srcCols As Integer, matType As cv.MatType, dst As cv.Mat)
            If data = IntPtr.Zero OrElse dst Is Nothing OrElse srcRows <= 0 OrElse srcCols <= 0 Then Return
            Dim src = cv.Mat.FromPixelData(srcRows, srcCols, matType, data)
            cv.Cv2.Resize(src, dst, workRes, 0, 0, cv.InterpolationFlags.Nearest)
        End Sub
        Public Sub GetNextFrame()
            ' Check if pipe is still valid (might be cleared during stop)
            If pipe Is Nothing Then Return

            ' WaitForFrames blocks for the whole timeout when color and stereo are
            ' a frame apart. Poll, then wait only one frame time, and keep the newest set.
            Dim frames As FrameSet = Nothing
            Try
                If Not pipe.PollForFrames(frames) Then
                    If Not pipe.TryWaitForFrames(frames, 33) Then Return
                End If
                Dim newer As FrameSet = Nothing
                While pipe IsNot Nothing AndAlso pipe.PollForFrames(newer)
                    frames.Dispose()
                    frames = newer
                    newer = Nothing
                End While
            Catch ex As Exception
                Debug.WriteLine("RealSense frame wait: " + ex.Message)
                If frames IsNot Nothing Then frames.Dispose()
                Return
            End Try
            If frames Is Nothing Then Return

            Dim cols = captureRes.Width, rows = captureRes.Height
            Dim sawDepth As Boolean = False
            Using frames
                ' Copy out of the SDK buffers before they are released. Holding the
                ' frameset through the point-cloud build fills the camera queue and
                ' the next wait times out.
                SyncLock cameraMutex
                    For Each frame As Intel.RealSense.Frame In frames
                        If frame.Profile.Stream = Stream.Color And frame.Profile.Index = 0 Then
                            CopyToWorkRes(frame.Data, rows, cols, cv.MatType.CV_8UC3, color)
                        End If
                        If frame.Profile.Stream = Stream.Infrared And frame.Profile.Index = 1 Then
                            CopyToWorkRes(frame.Data, rows, cols, cv.MatType.CV_8UC1, leftView)
                        End If
                        If frame.Profile.Stream = Stream.Infrared And frame.Profile.Index = 2 Then
                            CopyToWorkRes(frame.Data, rows, cols, cv.MatType.CV_8UC1, rightView)
                        End If
                        If frame.Profile.Stream = Stream.Depth Then
                            CopyToWorkRes(frame.Data, rows, cols, cv.MatType.CV_16UC1, depth16u)
                            sawDepth = True
                        End If
                        If frame.Profile.Stream = Stream.Accel Then
                            IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(frame.Data)
                        End If
                        If frame.Profile.Stream = Stream.Gyro Then
                            IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(frame.Data)
                            Dim mFrame = frame.As(Of MotionFrame)
                            If imuStartTime = 0 Then imuStartTime = mFrame.Timestamp
                            IMU_FrameTime = mFrame.Timestamp - imuStartTime
                        End If
                    Next
                End SyncLock
            End Using

            If sawDepth Then
                Dim cloud = ComputePointCloud(depth16u, calibData.leftIntrinsics)
                SyncLock cameraMutex
                    pointCloud = cloud
                End SyncLock
            End If

            MyBase.GetNextFrameCounts()
        End Sub
        Public Overrides Sub StopCamera()
            ' Stop the pipeline asynchronously so it doesn't block the UI
            Dim pipeToStop = pipe
            Dim motionToStop = motionSensor
            pipe = Nothing ' Clear reference so GetNextFrame won't try to use it
            motionSensor = Nothing

            ' Run pipe.Stop() on a background thread so it doesn't block
            ThreadPool.QueueUserWorkItem(Sub(state)
                                             Try
                                                 If motionToStop IsNot Nothing Then
                                                     motionToStop.Stop()
                                                     motionToStop.Close()
                                                 End If
                                                 pipeToStop.Stop()
                                                 pipeToStop.Dispose()
                                             Catch ex As Exception
                                                 Debug.WriteLine("Error stopping pipeline: " + ex.Message)
                                             End Try
                                         End Sub)
        End Sub
    End Class
End Namespace

