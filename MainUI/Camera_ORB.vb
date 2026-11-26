Imports System.Runtime.InteropServices
Imports System.Threading
Imports Orbbec
Imports cv = OpenCvSharp

Namespace MainApp
    Public Class Camera_ORB : Inherits GenericCamera
        Dim pipe As Pipeline

        Dim accelSensor As Sensor
        Dim gyroSensor As Sensor

        Dim acceleration As cv.Point3f, angularVelocity As cv.Point3f, timeStamp As Int64
        Dim PtCloud As New PointCloudFilter
        Dim initialTime As Int64 = timeStamp
        ''' <summary>Get video stream profile; if requested w/h/format/fps is not supported, use device default (avoids "No matched video stream profile" error).</summary>
        Private Shared Function GetOrbVideoProfile(profileList As StreamProfileList, w As Integer, h As Integer, format As Format, fps As Integer) As StreamProfile
            Try
                Return profileList.GetVideoStreamProfile(w, h, format, fps)
            Catch ex As NativeException
                If profileList.ProfileCount() = 0 Then Throw
                Return profileList.GetProfile(0)
            End Try
        End Function

        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            captureRes = _captureRes
            workRes = _workRes
            Dim ctx As New Context
            Dim list = ctx.QueryDeviceList()
            Dim dev = list.GetDevice(0)

            Dim firmwareVersion = dev.GetDeviceInfo().FirmwareVersion
            Debug.WriteLine("ORB Firmware Version: " & firmwareVersion)

            Dim fps = 0
            Dim w = captureRes.Width, h = captureRes.Height
            pipe = New Pipeline()
            ' Try requested resolution/format; fall back to device default if "No matched video stream profile" (unsupported combo).
            Dim colorProfile As StreamProfile = GetOrbVideoProfile(pipe.GetStreamProfileList(SensorType.OB_SENSOR_COLOR), w, h, Format.OB_FORMAT_BGR, fps)
            Dim depthProfile As StreamProfile = GetOrbVideoProfile(pipe.GetStreamProfileList(SensorType.OB_SENSOR_DEPTH), w, h, Format.OB_FORMAT_Y16, fps)
            Dim leftProfile As StreamProfile = GetOrbVideoProfile(pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_LEFT), w, h, Format.OB_FORMAT_Y8, fps)
            Dim rightProfile As StreamProfile = GetOrbVideoProfile(pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_RIGHT), w, h, Format.OB_FORMAT_Y8, fps)
            ' Use actual color stream size (e.g. when default profile was used)
            Dim colorVideo = colorProfile.As(Of VideoStreamProfile)()
            If colorVideo IsNot Nothing Then
                captureRes = New cv.Size(CInt(colorVideo.GetWidth()), CInt(colorVideo.GetHeight()))
            End If
            Dim config As New Config()
            config.EnableStream(colorProfile)
            config.EnableStream(depthProfile)
            config.EnableStream(leftProfile)
            config.EnableStream(rightProfile)
            ' Depth and color are separate cameras. Software D2C resamples depth
            ' into the color image for the Gemini 335, 335L, and 336L, including
            ' while the IR streams stay enabled. Hardware D2C only covers a few
            ' resolution pairs and drops out when those IR streams are on.
            config.SetAlignMode(AlignMode.ALIGN_D2C_SW_MODE)

            gyroSensor = dev.GetSensorList.GetSensor(SensorType.OB_SENSOR_GYRO)
            accelSensor = dev.GetSensorList.GetSensor(SensorType.OB_SENSOR_ACCEL)

            Dim gProfiles = gyroSensor.GetStreamProfileList()
            Dim gProfile = gProfiles.GetProfile(0)
            gyroSensor.Start(gProfile, Sub(frame As Orbbec.Frame)
                                           angularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(frame.GetDataPtr)
                                           timeStamp = frame.GetTimeStamp
                                       End Sub)

            Dim accProfiles = accelSensor.GetStreamProfileList()
            Dim accProfile = accProfiles.GetProfile(0)
            accelSensor.Start(accProfile, Sub(frame As Orbbec.Frame)
                                              acceleration = Marshal.PtrToStructure(Of cv.Point3f)(frame.GetDataPtr)
                                              timeStamp = frame.GetTimeStamp
                                          End Sub)
            pipe.EnableFrameSync()

            MyBase.prepImages()

            pipe.Start(config)

            Dim param As CameraParam = pipe.GetCameraParam()
            ' Intrinsics are for the resolution stored in the calibration. Depth is
            ' registered to color and then resized to workRes, so the cloud uses
            ' the color camera scaled to that resolution.
            ScaleIntrinsics(param.rgbIntrinsic, calibData.rgbIntrinsics)
            ScaleIntrinsics(param.depthIntrinsic, calibData.leftIntrinsics)

            calibData.ColorToLeft_rotation = param.transform.rot
            calibData.ColorToLeft_translation = param.transform.trans

            calibData.LtoR_rotation = param.transform.rot
            calibData.LtoR_translation = param.transform.trans

            calibData.baseline = System.Math.Sqrt(System.Math.Pow(calibData.LtoR_translation(0), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(1), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(2), 2))

            ' Gemini stereo baseline, in meters. Depth-to-color registration is
            ' handled by the pipeline align mode, not by this baseline.
            calibData.baseline = 0.095
            PtCloud.SetCameraParam(param)
            'PtCloud.SetAlignState(True)

            ' Start background thread to capture frames
            isCapturing = True
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "CaptureThread_ORB"
            captureThread.Start()
        End Sub

        ''' <summary>
        ''' Scale factory intrinsics down to workRes. fx/fy/cx/cy belong to
        ''' intrinsic.width x intrinsic.height, which is not always the capture size.
        ''' </summary>
        Private Sub ScaleIntrinsics(intrinsic As CameraIntrinsic, ByRef dst As GenericCamera.intrinsicData)
            Dim scaleX As Single = 1.0F
            Dim scaleY As Single = 1.0F
            If workRes.Width <= 0 OrElse workRes.Height <= 0 Then
                scaleX = 1.0F
                scaleY = 1.0F
            ElseIf intrinsic.width > 0 AndAlso intrinsic.height > 0 Then
                scaleX = CSng(intrinsic.width) / workRes.Width
                scaleY = CSng(intrinsic.height) / workRes.Height
            ElseIf workRes.Width > 0 Then
                Dim ratio = captureRes.Width \ workRes.Width
                If ratio < 1 Then ratio = 1
                scaleX = ratio
                scaleY = ratio
            End If
            dst.ppx = intrinsic.cx / scaleX
            dst.ppy = intrinsic.cy / scaleY
            dst.fx = intrinsic.fx / scaleX
            dst.fy = intrinsic.fy / scaleY
        End Sub

        ''' <summary>Copy a camera frame into the owned workRes image. The SDK buffer is only valid for this call.</summary>
        Private Sub CopyToWorkRes(frame As VideoFrame, matType As cv.MatType, dst As cv.Mat)
            If frame Is Nothing OrElse dst Is Nothing Then Return
            Dim srcRows = CInt(frame.GetHeight())
            Dim srcCols = CInt(frame.GetWidth())
            If srcRows <= 0 OrElse srcCols <= 0 OrElse frame.GetDataPtr() = IntPtr.Zero Then Return
            Dim src = cv.Mat.FromPixelData(srcRows, srcCols, matType, frame.GetDataPtr())
            cv.Cv2.Resize(src, dst, workRes, 0, 0, cv.InterpolationFlags.Nearest)
        End Sub

        Private Sub CaptureFrames()
            While isCapturing
                GetNextFrame()
            End While
        End Sub
        Public Sub GetNextFrame()
            If pipe Is Nothing Then Return

            Dim frames As Frameset = Nothing
            While frames Is Nothing AndAlso isCapturing AndAlso pipe IsNot Nothing
                frames = pipe.WaitForFrames(100)
            End While
            If frames Is Nothing Then Return

            Dim cFrame = frames.GetColorFrame
            Dim dFrame = frames.GetDepthFrame
            Dim lFrame = frames.GetFrame(FrameType.OB_FRAME_IR_LEFT)
            Dim rFrame = frames.GetFrame(FrameType.OB_FRAME_IR_RIGHT)

            SyncLock cameraMutex
                If cFrame IsNot Nothing Then
                    CopyToWorkRes(cFrame, cv.MatType.CV_8UC3, color)
                End If

                If lFrame IsNot Nothing Then
                    Dim leftVideo = lFrame.As(Of VideoFrame)()
                    CopyToWorkRes(leftVideo, cv.MatType.CV_8UC1, leftView)
                    If leftVideo IsNot Nothing Then leftVideo.Dispose()
                End If

                If rFrame IsNot Nothing Then
                    Dim rightVideo = rFrame.As(Of VideoFrame)()
                    CopyToWorkRes(rightVideo, cv.MatType.CV_8UC1, rightView)
                    If rightVideo IsNot Nothing Then rightVideo.Dispose()
                End If

                If dFrame IsNot Nothing Then
                    ' Pipeline D2C has already placed this depth on the color pixels.
                    CopyToWorkRes(dFrame, cv.MatType.CV_16UC1, depth16u)
                    Dim scale = dFrame.GetValueScale()
                    If scale > 0.0F AndAlso Math.Abs(scale - 1.0F) > 0.001F Then
                        depth16u.ConvertTo(depth16u, depth16u.Type(), scale)
                    End If
                    pointCloud = ComputePointCloud(depth16u, calibData.rgbIntrinsics)
                End If

                IMU_AngularVelocity = angularVelocity
                IMU_Acceleration = acceleration
                IMU_Acceleration.Z *= -1
                IMU_FrameTime = timeStamp - initialTime
            End SyncLock

            MyBase.GetNextFrameCounts()
        End Sub

        Public Overrides Sub StopCamera()
            ' Stop the pipeline asynchronously so it doesn't block the UI
            If pipe IsNot Nothing Then
                Dim pipeToStop = pipe
                pipe = Nothing ' Clear reference so GetNextFrame won't try to use it

                ' Run pipe.Stop() on a background thread so it doesn't block
                ThreadPool.QueueUserWorkItem(Sub(state)
                                                 Try
                                                     pipeToStop.Stop()
                                                     pipeToStop.Dispose()
                                                 Catch ex As Exception
                                                     Debug.WriteLine("Error stopping pipeline: " + ex.Message)
                                                 End Try
                                             End Sub)
            End If
        End Sub
    End Class
End Namespace

