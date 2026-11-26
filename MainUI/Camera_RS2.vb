Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.JavaScript.JSType
Imports System.Threading
Imports Intel.RealSense
Imports cv = OpenCvSharp

Namespace MainApp
    Public Class Camera_RS2 : Inherits GenericCamera
        Dim pipe As Pipeline
        Dim sdkContext As Context
        Dim colorSensor As Sensor
        Dim stereoSensor As Sensor
        Dim motionSensor As Sensor
        Dim frameArrived As New AutoResetEvent(False)
        Dim directVideo As Boolean
        Dim colorRaw As cv.Mat
        Dim leftRaw As cv.Mat
        Dim rightRaw As cv.Mat
        Dim depthRaw As cv.Mat
        Dim imuStartTime As Double
        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, devName As String, emitterOn As Integer,
                       Optional fps As Integer = 30)
            captureRes = _captureRes
            workRes = _workRes
            Dim isD455 = devName.EndsWith("455")
            Dim serialNumber As String = ""
            sdkContext = New Context()
            Dim searchName As String = If(isD455, "D455", "D435i")
            Dim device As Device = Nothing
            For Each dev In sdkContext.QueryDevices()
                If dev.Info.Item(0).Contains(searchName) Then
                    serialNumber = dev.Info.Item(1)
                    device = dev
                End If
            Next

            If isD455 Then
                ' WaitForFrames throws "Frame didn't arrive within 100" when the four
                ' streams have not formed one frameset yet. Take each sensor directly
                ' instead, and pick a size that can actually hold 30 FPS on this link.
                captureRes = D455CaptureSize(device, workRes)
                fps = 30
                directVideo = StartD455Video(device, captureRes, fps, emitterOn)
            End If

            Dim leftIntrin As Intrinsics
            Dim leftExtrinsics As Extrinsics
            Dim rgb As Intrinsics
            Dim rgbExtrinsics As Extrinsics

            If directVideo Then
                leftIntrin = stereoLeft.GetIntrinsics()
                leftExtrinsics = stereoLeft.GetExtrinsicsTo(stereoRight)
                rgb = colorProfile.GetIntrinsics()
                rgbExtrinsics = colorProfile.GetExtrinsicsTo(stereoLeft)
            ElseIf Not isD455 Then
                pipe = New Pipeline()
                Dim cfg As New Config()
                cfg.EnableDevice(serialNumber)
                cfg.EnableStream(Stream.Color, captureRes.Width, captureRes.Height, Format.Bgr8, fps)
                cfg.EnableStream(Stream.Infrared, 1, captureRes.Width, captureRes.Height, Format.Y8, fps)
                cfg.EnableStream(Stream.Infrared, 2, captureRes.Width, captureRes.Height, Format.Y8, fps)
                cfg.EnableStream(Stream.Depth, captureRes.Width, captureRes.Height, Format.Z16, fps)
                ' D435i accel is 63 Hz. A D455 made after mid-2022 has no 63 Hz profile
                ' (minimum is 100). Leaving motion in this frameset makes the pipeline
                ' wait for a sample that never arrives.
                cfg.EnableStream(Stream.Accel, Format.MotionXyz32f, 63)
                cfg.EnableStream(Stream.Gyro, Format.MotionXyz32f, 200)

                Dim profiles = pipe.Start(cfg)
                Dim started As Device = profiles.Device
                For Each s In started.Sensors
                    TrySetOption(s, Intel.RealSense.Option.FramesQueueSize, 32)
                    If s.Info.Item(0).Contains("Stereo Module") Then
                        s.Options(Intel.RealSense.Option.EmitterEnabled).Value = emitterOn
                    End If
                Next

                Dim streamColor = profiles.GetStream(Stream.Color).As(Of VideoStreamProfile)()
                Dim streamLeft = profiles.GetStream(Stream.Infrared, 1).As(Of VideoStreamProfile)()
                Dim streamRight = profiles.GetStream(Stream.Infrared, 2).As(Of VideoStreamProfile)()
                leftIntrin = streamLeft.GetIntrinsics()
                leftExtrinsics = streamLeft.GetExtrinsicsTo(streamRight)
                rgb = streamColor.GetIntrinsics()
                rgbExtrinsics = streamColor.GetExtrinsicsTo(streamLeft)
            End If

            captureRes = New cv.Size(rgb.width, rgb.height)
            ScaleToWork(rgb, calibData.rgbIntrinsics)
            ScaleToWork(leftIntrin, calibData.leftIntrinsics)

            ReDim calibData.LtoR_translation(3 - 1)
            ReDim calibData.LtoR_rotation(9 - 1)

            ReDim calibData.ColorToLeft_translation(3 - 1)
            ReDim calibData.ColorToLeft_rotation(9 - 1)

            If leftExtrinsics.translation IsNot Nothing Then
                For i = 0 To 3 - 1
                    calibData.LtoR_translation(i) = leftExtrinsics.translation(i)
                Next
            End If
            If leftExtrinsics.rotation IsNot Nothing Then
                For i = 0 To 9 - 1
                    calibData.LtoR_rotation(i) = leftExtrinsics.rotation(i)
                Next
            End If

            If rgbExtrinsics.translation IsNot Nothing Then
                For i = 0 To 3 - 1
                    calibData.ColorToLeft_translation(i) = rgbExtrinsics.translation(i)
                Next
            End If
            If rgbExtrinsics.rotation IsNot Nothing Then
                For i = 0 To 9 - 1
                    calibData.ColorToLeft_rotation(i) = rgbExtrinsics.rotation(i)
                Next
            End If

            calibData.baseline = System.Math.Sqrt(System.Math.Pow(calibData.LtoR_translation(0), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(1), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(2), 2))
            MyBase.prepImages()
            If isD455 AndAlso device IsNot Nothing Then StartD455Motion(device)

            ' Start background thread to capture frames
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "CaptureThread_RS2"
            captureThread.Start()
        End Sub
        Private colorProfile As VideoStreamProfile
        Private stereoLeft As VideoStreamProfile
        Private stereoRight As VideoStreamProfile
        Private Sub CaptureFrames()
            While isCapturing
                If directVideo Then
                    PublishLatest()
                ElseIf pipe IsNot Nothing Then
                    GetNextFrame()
                Else
                    Thread.Sleep(50)
                End If
            End While
        End Sub
        ''' <summary>
        ''' USB 2 cannot carry four 1280x720 or 848x480 streams at 30 FPS.
        ''' The pipeline then sits past 100 ms and throws instead of delivering a frame.
        ''' </summary>
        Private Function D455CaptureSize(device As Device, requested As cv.Size) As cv.Size
            Dim usb As String = Nothing
            Try
                If device IsNot Nothing Then usb = device.Info(CameraInfo.UsbTypeDescriptor)
            Catch
            End Try
            Dim usb2 = usb IsNot Nothing AndAlso usb.StartsWith("2")
            If usb2 Then Return New cv.Size(424, 240)
            If requested.Width <= 640 AndAlso requested.Height <= 360 Then Return New cv.Size(640, 360)
            Return New cv.Size(848, 480)
        End Function
        Private Function StartD455Video(device As Device, size As cv.Size, rate As Integer, emitterOn As Integer) As Boolean
            If device Is Nothing Then Return False
            Dim sizes As cv.Size() = {
                size,
                New cv.Size(848, 480),
                New cv.Size(640, 480),
                New cv.Size(640, 360),
                New cv.Size(480, 270),
                New cv.Size(424, 240)
            }
            For Each s In device.Sensors
                Dim sensorName = s.Info.Item(0)
                If sensorName.Contains("Stereo") Then stereoSensor = s
                If sensorName.Contains("RGB") Then colorSensor = s
            Next
            If stereoSensor Is Nothing OrElse colorSensor Is Nothing Then Return

            For Each candidate In sizes
                colorProfile = FindVideoProfile(colorSensor, Stream.Color, 0, Format.Bgr8, candidate.Width, candidate.Height, rate)
                stereoLeft = FindVideoProfile(stereoSensor, Stream.Infrared, 1, Format.Y8, candidate.Width, candidate.Height, rate)
                stereoRight = FindVideoProfile(stereoSensor, Stream.Infrared, 2, Format.Y8, candidate.Width, candidate.Height, rate)
                Dim depthProfile = FindVideoProfile(stereoSensor, Stream.Depth, 0, Format.Z16, candidate.Width, candidate.Height, rate)
                If colorProfile Is Nothing OrElse stereoLeft Is Nothing OrElse stereoRight Is Nothing OrElse depthProfile Is Nothing Then Continue For

                captureRes = New cv.Size(candidate.Width, candidate.Height)
                ' The device-clock read was stalling each frame. These callbacks do not
                ' need one shared clock, so leave global time off.
                TrySetOption(colorSensor, Intel.RealSense.Option.GlobalTimeEnabled, 0)
                TrySetOption(stereoSensor, Intel.RealSense.Option.GlobalTimeEnabled, 0)
                TrySetOption(colorSensor, Intel.RealSense.Option.FramesQueueSize, 16)
                TrySetOption(stereoSensor, Intel.RealSense.Option.FramesQueueSize, 16)
                TrySetOption(stereoSensor, Intel.RealSense.Option.EmitterEnabled, emitterOn)

                Try
                    colorSensor.Open(colorProfile)
                    colorSensor.Start(AddressOf OnVideo)
                    stereoSensor.Open(depthProfile, stereoLeft, stereoRight)
                    stereoSensor.Start(AddressOf OnVideo)
                    Return True
                Catch ex As Exception
                    Debug.WriteLine("D455 video start: " + ex.Message)
                    Try
                        colorSensor.Stop()
                    Catch
                    End Try
                    Try
                        colorSensor.Close()
                    Catch
                    End Try
                    Try
                        stereoSensor.Stop()
                    Catch
                    End Try
                    Try
                        stereoSensor.Close()
                    Catch
                    End Try
                End Try
            Next
            Return False
        End Function
        Private Function FindVideoProfile(sensor As Sensor, kind As Stream, index As Integer, fmt As Format,
                                          width As Integer, height As Integer, rate As Integer) As VideoStreamProfile
            For Each p In sensor.StreamProfiles
                If p.Stream <> kind OrElse p.Index <> index OrElse p.Format <> fmt OrElse p.Framerate <> rate Then Continue For
                Dim video = p.As(Of VideoStreamProfile)()
                If video.Width = width AndAlso video.Height = height Then Return video
            Next
            Return Nothing
        End Function
        Private Sub OnVideo(frame As Frame)
            Try
                If frame Is Nothing OrElse frame.Profile Is Nothing OrElse Not isCapturing Then Return
                Dim video = frame.As(Of VideoFrame)()
                Dim kind = frame.Profile.Stream
                Dim index = frame.Profile.Index
                SyncLock cameraMutex
                    If kind = Stream.Color AndAlso index = 0 Then
                        CopyRaw(video.Data, video.Height, video.Width, cv.MatType.CV_8UC3, colorRaw)
                    ElseIf kind = Stream.Infrared AndAlso index = 1 Then
                        CopyRaw(video.Data, video.Height, video.Width, cv.MatType.CV_8UC1, leftRaw)
                    ElseIf kind = Stream.Infrared AndAlso index = 2 Then
                        CopyRaw(video.Data, video.Height, video.Width, cv.MatType.CV_8UC1, rightRaw)
                    ElseIf kind = Stream.Depth Then
                        CopyRaw(video.Data, video.Height, video.Width, cv.MatType.CV_16UC1, depthRaw)
                    End If
                End SyncLock
                If kind = Stream.Depth Then frameArrived.Set()
            Catch ex As Exception
                Debug.WriteLine("RealSense video callback: " + ex.Message)
            End Try
        End Sub
        Private Sub PublishLatest()
            ' Wake on the next depth frame. This wait does not throw when the camera is late.
            If Not frameArrived.WaitOne(200) Then Return
            If Not isCapturing Then Return

            Dim depthSnap As cv.Mat = Nothing
            SyncLock cameraMutex
                If colorRaw IsNot Nothing Then cv.Cv2.Resize(colorRaw, color, workRes, 0, 0, cv.InterpolationFlags.Nearest)
                If leftRaw IsNot Nothing Then cv.Cv2.Resize(leftRaw, leftView, workRes, 0, 0, cv.InterpolationFlags.Nearest)
                If rightRaw IsNot Nothing Then cv.Cv2.Resize(rightRaw, rightView, workRes, 0, 0, cv.InterpolationFlags.Nearest)
                If depthRaw IsNot Nothing Then
                    cv.Cv2.Resize(depthRaw, depth16u, workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    depthSnap = depth16u.Clone()
                End If
            End SyncLock
            If depthSnap Is Nothing Then Return

            Dim cloud = ComputePointCloud(depthSnap, calibData.leftIntrinsics)
            depthSnap.Dispose()
            SyncLock cameraMutex
                pointCloud = cloud
            End SyncLock
            MyBase.GetNextFrameCounts()
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
                Dim setting = sensor.Options(opt)
                If value > setting.Max Then value = setting.Max
                If value < setting.Min Then value = setting.Min
                setting.Value = value
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
        Private Sub ScaleToWork(src As Intrinsics, ByRef dst As GenericCamera.intrinsicData)
            Dim scaleX As Single = 1.0F
            Dim scaleY As Single = 1.0F
            If src.width > 0 AndAlso workRes.Width > 0 Then scaleX = CSng(src.width) / workRes.Width
            If src.height > 0 AndAlso workRes.Height > 0 Then scaleY = CSng(src.height) / workRes.Height
            dst.ppx = src.ppx / scaleX
            dst.ppy = src.ppy / scaleY
            dst.fx = src.fx / scaleX
            dst.fy = src.fy / scaleY
        End Sub
        Private Sub CopyRaw(data As IntPtr, rows As Integer, cols As Integer, matType As cv.MatType, ByRef dst As cv.Mat)
            If data = IntPtr.Zero OrElse rows <= 0 OrElse cols <= 0 Then Return
            If dst Is Nothing OrElse dst.Rows <> rows OrElse dst.Cols <> cols OrElse dst.Type() <> matType Then
                If dst IsNot Nothing Then dst.Dispose()
                dst = New cv.Mat(rows, cols, matType)
            End If
            Using src = cv.Mat.FromPixelData(rows, cols, matType, data)
                src.CopyTo(dst)
            End Using
        End Sub
        Private Sub CopyToWorkRes(data As IntPtr, srcRows As Integer, srcCols As Integer, matType As cv.MatType, dst As cv.Mat)
            If data = IntPtr.Zero OrElse dst Is Nothing OrElse srcRows <= 0 OrElse srcCols <= 0 Then Return
            Dim src = cv.Mat.FromPixelData(srcRows, srcCols, matType, data)
            cv.Cv2.Resize(src, dst, workRes, 0, 0, cv.InterpolationFlags.Nearest)
        End Sub
        Public Sub GetNextFrame()
            ' Check if pipe is still valid (might be cleared during stop)
            If pipe Is Nothing Then Return

            ' TryWaitForFrames returns false on a timeout. WaitForFrames throws
            ' "Frame didn't arrive within 100" and that exception was reaching the UI.
            Dim frames As FrameSet = Nothing
            Try
                If Not pipe.PollForFrames(frames) Then
                    If Not pipe.TryWaitForFrames(frames, 1000) Then Return
                End If
            Catch ex As Exception
                Debug.WriteLine("RealSense frame wait: " + ex.Message)
                If frames IsNot Nothing Then frames.Dispose()
                Return
            End Try
            If frames Is Nothing Then Return

            Dim sawDepth As Boolean = False
            Using frames
                ' Copy out of the SDK buffers before they are released. Holding the
                ' frameset through the point-cloud build fills the camera queue and
                ' the next wait times out.
                SyncLock cameraMutex
                    For Each frame As Intel.RealSense.Frame In frames
                        Dim streamKind = frame.Profile.Stream
                        If streamKind = Stream.Color OrElse streamKind = Stream.Infrared OrElse streamKind = Stream.Depth Then
                            Dim video = frame.As(Of VideoFrame)()
                            If streamKind = Stream.Color And frame.Profile.Index = 0 Then
                                CopyToWorkRes(frame.Data, video.Height, video.Width, cv.MatType.CV_8UC3, color)
                            ElseIf streamKind = Stream.Infrared And frame.Profile.Index = 1 Then
                                CopyToWorkRes(frame.Data, video.Height, video.Width, cv.MatType.CV_8UC1, leftView)
                            ElseIf streamKind = Stream.Infrared And frame.Profile.Index = 2 Then
                                CopyToWorkRes(frame.Data, video.Height, video.Width, cv.MatType.CV_8UC1, rightView)
                            ElseIf streamKind = Stream.Depth Then
                                CopyToWorkRes(frame.Data, video.Height, video.Width, cv.MatType.CV_16UC1, depth16u)
                                sawDepth = True
                            End If
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
            isCapturing = False
            frameArrived.Set()
            Dim pipeToStop = pipe
            Dim motionToStop = motionSensor
            Dim colorToStop = colorSensor
            Dim stereoToStop = stereoSensor
            pipe = Nothing
            motionSensor = Nothing
            colorSensor = Nothing
            stereoSensor = Nothing

            ThreadPool.QueueUserWorkItem(Sub(state)
                                             Try
                                                 If motionToStop IsNot Nothing Then
                                                     motionToStop.Stop()
                                                     motionToStop.Close()
                                                 End If
                                                 If stereoToStop IsNot Nothing Then
                                                     stereoToStop.Stop()
                                                     stereoToStop.Close()
                                                 End If
                                                 If colorToStop IsNot Nothing Then
                                                     colorToStop.Stop()
                                                     colorToStop.Close()
                                                 End If
                                                 If pipeToStop IsNot Nothing Then
                                                     pipeToStop.Stop()
                                                     pipeToStop.Dispose()
                                                 End If
                                             Catch ex As Exception
                                                 Debug.WriteLine("Error stopping RealSense: " + ex.Message)
                                             End Try
                                         End Sub)
        End Sub
    End Class
End Namespace

