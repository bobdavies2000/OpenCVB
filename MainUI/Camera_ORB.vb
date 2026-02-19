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
            config.SetAlignMode(AlignMode.ALIGN_DISABLE)

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
            Dim ratio = captureRes.Width \ workRes.Width
            calibData.rgbIntrinsics.ppx = param.rgbIntrinsic.cx / ratio
            calibData.rgbIntrinsics.ppy = param.rgbIntrinsic.cy / ratio
            calibData.rgbIntrinsics.fx = param.rgbIntrinsic.fx / ratio
            calibData.rgbIntrinsics.fy = param.rgbIntrinsic.fy / ratio

            calibData.leftIntrinsics.ppx = param.depthIntrinsic.cx / ratio
            calibData.leftIntrinsics.ppy = param.depthIntrinsic.cy / ratio
            calibData.leftIntrinsics.fx = param.depthIntrinsic.fx / ratio
            calibData.leftIntrinsics.fy = param.depthIntrinsic.fy / ratio

            calibData.ColorToLeft_rotation = param.transform.rot
            calibData.ColorToLeft_translation = param.transform.trans

            calibData.LtoR_rotation = param.transform.rot
            calibData.LtoR_translation = param.transform.trans

            calibData.baseline = System.Math.Sqrt(System.Math.Pow(calibData.LtoR_translation(0), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(1), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(2), 2))

            calibData.baseline = 0.095 ' the RGB and left image provided are aligned so depth is easily found.
            PtCloud.SetCameraParam(param)

            ' Start background thread to capture frames
            isCapturing = True
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "CaptureThread_ORB"
            captureThread.Start()
        End Sub

        Private Sub CaptureFrames()
            While isCapturing
                GetNextFrame()
            End While
        End Sub
        Public Sub GetNextFrame()
            Dim rows = captureRes.Height, cols = captureRes.Width

            Dim frames As Frameset = Nothing
            While frames Is Nothing
                frames = pipe.WaitForFrames(100)
            End While

            Dim cFrame = frames.GetColorFrame
            Dim dFrame = frames.GetDepthFrame
            Dim lFrame = frames.GetFrame(FrameType.OB_FRAME_IR_LEFT)
            Dim rFrame = frames.GetFrame(FrameType.OB_FRAME_IR_RIGHT)

            SyncLock cameraMutex
                If cFrame IsNot Nothing Then
                    color = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC3, cFrame.GetDataPtr)
                    color = color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                End If

                If lFrame IsNot Nothing Then
                    leftView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, lFrame.GetDataPtr)
                    leftView = leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                End If

                If rFrame IsNot Nothing Then
                    rightView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, rFrame.GetDataPtr)
                    rightView = rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                End If

                If dFrame IsNot Nothing Then
                    depth16u = cv.Mat(Of UShort).FromPixelData(rows, cols, cv.MatType.CV_16UC1, dFrame.GetDataPtr)
                    depth16u = depth16u.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    pointCloud = ComputePointCloud(depth16u, calibData.leftIntrinsics)
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

