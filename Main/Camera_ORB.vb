Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Orbbec
Imports cv = OpenCvSharp

Namespace MainForm
    Public Class Camera_ORB : Inherits GenericCamera
        Dim captureThread As Thread = Nothing
        Dim isCapturing As Boolean = False
        Dim pipe As Pipeline

        Dim accelSensor As Sensor
        Dim gyroSensor As Sensor

        Dim acceleration As cv.Point3f, angularVelocity As cv.Point3f, timeStamp As Int64
        Dim color As cv.Mat, leftView As cv.Mat, pointCloud As cv.Mat, rightView As cv.Mat
        Dim PtCloud As New PointCloudFilter
        Dim initialTime As Int64 = timeStamp
        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            captureRes = _captureRes
            workRes = _workRes
            Dim ctx As New Context
            Dim devList = ctx.QueryDeviceList()
            Dim dev = devList.GetDevice(0)

            Dim fps = 0
            Dim w = captureRes.Width, h = captureRes.Height
            pipe = New Pipeline()
            Dim colorProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_COLOR).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_BGR, fps)
            Dim depthProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_DEPTH).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y16, fps)
            Dim leftProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_LEFT).
                                           GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y8, fps)
            Dim rightProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_RIGHT).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y8, fps)
            Dim config As New Config()
            config.EnableStream(colorProfile)
            config.EnableStream(depthProfile)
            config.EnableStream(leftProfile)
            config.EnableStream(rightProfile) ' USE_RIGHT_IMAGE
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
            Try
                pipe.Start(config)
            Catch ex As Exception
            End Try

            ' Start background thread to capture frames
            isCapturing = True
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "ORB_CaptureThread"
            captureThread.Start()
        End Sub

        Private Sub CaptureFrames()
            While isCapturing
                Try
                    GetNextFrame()
                Catch ex As Exception
                    ' Continue capturing even if one frame fails
                    Thread.Sleep(10)
                End Try
            End While
        End Sub

        Public Sub GetNextFrame()
            Dim rows = captureRes.Height, cols = captureRes.Width

            Dim frames As Frameset = Nothing
            While frames Is Nothing
                frames = pipe.WaitForFrames(100)
            End While

            If calibData.baseline = 0 Then
                Dim param As CameraParam = pipe.GetCameraParam()
                Dim ratio = CInt(captureRes.Width / workRes.Width)
                calibData.rgbIntrinsics.ppx = param.rgbIntrinsic.cx / ratio
                calibData.rgbIntrinsics.ppy = param.rgbIntrinsic.cy / ratio
                calibData.rgbIntrinsics.fx = param.rgbIntrinsic.fx / ratio
                calibData.rgbIntrinsics.fy = param.rgbIntrinsic.fy / ratio
                calibData.baseline = 0.095 ' the RGB and left image provided are aligned so depth is easily found.
                PtCloud.SetCameraParam(param)
            End If

            Dim cFrame = frames.GetColorFrame
            Dim dFrame = frames.GetDepthFrame
            Dim lFrame = frames.GetFrame(FrameType.OB_FRAME_IR_LEFT)
            Dim rFrame = frames.GetFrame(FrameType.OB_FRAME_IR_RIGHT)

            If cFrame IsNot Nothing Then
                color = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC3, cFrame.GetDataPtr)
            End If

            If lFrame IsNot Nothing Then
                leftView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, lFrame.GetDataPtr)
            End If

            If rFrame IsNot Nothing Then
                rightView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, rFrame.GetDataPtr)
            End If

            If dFrame IsNot Nothing Then
                Dim depthValueScale As Single = dFrame.GetValueScale()
                PtCloud.SetPositionDataScaled(depthValueScale)
                PtCloud.SetPointFormat(Format.OB_FORMAT_POINT)
                Dim pcData = PtCloud.Process(dFrame)
                If pcData IsNot Nothing Then
                    pointCloud = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_32FC3, pcData.GetDataPtr) / 1000
                End If
            End If

            IMU_AngularVelocity = angularVelocity
            IMU_Acceleration = acceleration
            IMU_Acceleration.Z *= -1
            IMU_FrameTime = timeStamp - initialTime

            If color Is Nothing Then color = New cv.Mat(workRes, cv.MatType.CV_8UC3, 0)
            If leftView Is Nothing Then leftView = New cv.Mat(workRes, cv.MatType.CV_8UC1, 0)
            If rightView Is Nothing Then rightView = New cv.Mat(workRes, cv.MatType.CV_8UC1, 0)
            If pointCloud Is Nothing Then pointCloud = New cv.Mat(workRes, cv.MatType.CV_32FC3, 0)

            If workRes.Width = captureRes.Width Then
                camImages.images(0) = color.Clone
                camImages.images(1) = pointCloud.Clone
                camImages.images(2) = leftView * 4 ' brighten it to help with correlations.
                camImages.images(3) = rightView * 4
            Else
                camImages.images(0) = color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                camImages.images(1) = pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                camImages.images(2) = leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest) * 4
                camImages.images(3) = rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest) * 4
            End If
            ' without this GC.Collect, there are occasional memory footprint problems.  
            If cameraFrameCount Mod 10 = 0 Then GC.Collect()
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End Sub

        Public Overrides Sub StopCamera()
            isCapturing = False
            If captureThread IsNot Nothing Then
                captureThread.Join(1000) ' Wait up to 1 second for thread to finish
                captureThread = Nothing
            End If

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

