Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.JavaScript.JSType
Imports System.Threading
Imports Intel.RealSense
Imports cv = OpenCvSharp

Namespace MainApp
    Public Class Camera_RS2 : Inherits GenericCamera
        Dim pipe As Pipeline
        Dim motionSensor As Sensor
        Dim imuStartTime As Double
        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, devName As String, emitterOn As Integer,
                       Optional fps As Integer = 30)
            captureRes = _captureRes
            workRes = _workRes
            Dim isD455 = devName.Contains("455")
            Dim serialNumber As String = ""
            Dim ctx As New Context()
            Dim searchName As String = If(isD455, "D455", "D435i")
            For Each dev In ctx.QueryDevices()
                If dev.Info.Item(0).Contains(searchName) Then serialNumber = dev.Info.Item(1)
            Next

            ' A D455 has no 63 Hz accelerometer. Requesting that rate, or a video
            ' size the firmware does not list, makes Start throw "Couldn't resolve requests".
            ' Ask for a set the device can resolve, and keep D455 motion off this frameset.
            pipe = New Pipeline()
            Dim profiles = StartResolvable(serialNumber, isD455, fps)
            If profiles Is Nothing Then Return

            Dim device As Device = profiles.Device
            For Each s In device.Sensors
                TrySetOption(s, Intel.RealSense.Option.FramesQueueSize, 16)
                If s.Info.Item(0).Contains("Stereo Module") Then
                    TrySetOption(s, Intel.RealSense.Option.EmitterEnabled, emitterOn)
                End If
            Next

            Dim streamColor = TryGetVideo(profiles, Stream.Color, 0)
            Dim streamDepth = TryGetVideo(profiles, Stream.Depth, 0)
            Dim streamLeft = TryGetVideo(profiles, Stream.Infrared, 1)
            Dim streamRight = TryGetVideo(profiles, Stream.Infrared, 2)
            If streamColor Is Nothing OrElse streamDepth Is Nothing Then Return
            Dim leftSource = If(streamLeft, streamDepth)
            Dim leftIntrin As Intrinsics = leftSource.GetIntrinsics()
            Dim leftExtrinsics As Extrinsics = Nothing
            If streamLeft IsNot Nothing AndAlso streamRight IsNot Nothing Then
                leftExtrinsics = streamLeft.GetExtrinsicsTo(streamRight)
            End If
            Dim rgb As Intrinsics = streamColor.GetIntrinsics()
            Dim rgbExtrinsics As Extrinsics = streamColor.GetExtrinsicsTo(leftSource)

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
            If isD455 Then StartD455Motion(device)

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
        Private Function StartResolvable(serialNumber As String, isD455 As Boolean, fps As Integer) As PipelineProfile
            Dim sizes As cv.Size() = {
                captureRes,
                New cv.Size(1280, 720),
                New cv.Size(848, 480),
                New cv.Size(640, 480),
                New cv.Size(640, 360)
            }
            Dim triedWidth As Integer = -1
            Dim triedHeight As Integer = -1
            For Each size In sizes
                If size.Width <= 0 OrElse size.Height <= 0 Then Continue For
                If size.Width = triedWidth AndAlso size.Height = triedHeight Then Continue For
                triedWidth = size.Width
                triedHeight = size.Height
                ' D435i can include its 63 Hz accel. A D455 cannot, so leave motion out.
                Dim withMotion = Not isD455
                Dim started = TryStart(serialNumber, size.Width, size.Height, fps, withMotion, True)
                If started IsNot Nothing Then Return started
                If withMotion Then
                    started = TryStart(serialNumber, size.Width, size.Height, fps, False, True)
                    If started IsNot Nothing Then Return started
                End If
            Next
            ' Let the device choose a size it actually has.
            Dim fallback = TryStart(serialNumber, 0, 0, fps, False, True)
            If fallback IsNot Nothing Then Return fallback
            Return TryStart(serialNumber, 0, 0, fps, False, False)
        End Function
        Private Function TryStart(serialNumber As String, width As Integer, height As Integer, fps As Integer,
                                  withMotion As Boolean, withInfrared As Boolean) As PipelineProfile
            Dim cfg As New Config()
            If serialNumber <> "" Then cfg.EnableDevice(serialNumber)
            If width > 0 AndAlso height > 0 Then
                cfg.EnableStream(Stream.Color, width, height, Format.Bgr8, fps)
                cfg.EnableStream(Stream.Depth, width, height, Format.Z16, fps)
                If withInfrared Then
                    cfg.EnableStream(Stream.Infrared, 1, width, height, Format.Y8, fps)
                    cfg.EnableStream(Stream.Infrared, 2, width, height, Format.Y8, fps)
                End If
            Else
                cfg.EnableStream(Stream.Color, Format.Bgr8, fps)
                cfg.EnableStream(Stream.Depth, Format.Z16, fps)
                If withInfrared Then
                    cfg.EnableStream(Stream.Infrared, 1, Format.Y8, fps)
                    cfg.EnableStream(Stream.Infrared, 2, Format.Y8, fps)
                End If
            End If
            If withMotion Then
                cfg.EnableStream(Stream.Accel, Format.MotionXyz32f, 63)
                cfg.EnableStream(Stream.Gyro, Format.MotionXyz32f, 200)
            End If
            Try
                If Not cfg.CanResolve(pipe) Then Return Nothing
                Return pipe.Start(cfg)
            Catch ex As Exception
                Debug.WriteLine("RealSense resolve: " + ex.Message)
                Try
                    pipe.Stop()
                Catch
                End Try
                pipe = New Pipeline()
                Return Nothing
            End Try
        End Function
        Private Function TryGetVideo(profiles As PipelineProfile, kind As Stream, index As Integer) As VideoStreamProfile
            Try
                Return profiles.GetStream(kind, index).As(Of VideoStreamProfile)()
            Catch
                Return Nothing
            End Try
        End Function
        ''' <summary>
        ''' D455 motion stays on its own sensor. Putting a 63 Hz accel request in the
        ''' video config is what throws "Couldn't resolve requests".
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
        Private Sub CopyToWorkRes(data As IntPtr, srcRows As Integer, srcCols As Integer, matType As cv.MatType, dst As cv.Mat)
            If data = IntPtr.Zero OrElse dst Is Nothing OrElse srcRows <= 0 OrElse srcCols <= 0 Then Return
            Dim src = cv.Mat.FromPixelData(srcRows, srcCols, matType, data)
            cv.Cv2.Resize(src, dst, workRes, 0, 0, cv.InterpolationFlags.Nearest)
        End Sub
        Public Sub GetNextFrame()
            If pipe Is Nothing Then Return

            ' TryWaitForFrames returns false when nothing is ready. WaitForFrames throws.
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
            Dim pipeToStop = pipe
            Dim motionToStop = motionSensor
            pipe = Nothing
            motionSensor = Nothing

            ThreadPool.QueueUserWorkItem(Sub(state)
                                             Try
                                                 If motionToStop IsNot Nothing Then
                                                     motionToStop.Stop()
                                                     motionToStop.Close()
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
