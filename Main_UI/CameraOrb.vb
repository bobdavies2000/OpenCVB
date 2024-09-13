Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Controls
Imports cvb = OpenCvSharp
Imports Orbbec
Imports System.Runtime
Public Class CameraORB : Inherits GenericCamera
    Dim pipe As New Pipeline()
    Dim accelSensor As Sensor
    Dim gyroSensor As Sensor
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes

        Dim ctx As New Context
        Dim devList = ctx.QueryDeviceList()
        Dim dev = devList.GetDevice(0)

        Dim w = captureRes.Width, h = captureRes.Height
        Dim colorProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_COLOR).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_BGR, 30)
        Dim depthProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_DEPTH).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y16, 30)
        Dim leftProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_LEFT).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y8, 30)
        Dim rightProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_RIGHT).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y8, 30)
        Dim config As New Config()
        config.EnableStream(colorProfile)
        config.EnableStream(depthProfile)
        config.EnableStream(leftProfile)
        config.EnableStream(rightProfile)
        config.SetAlignMode(AlignMode.ALIGN_D2C_SW_MODE)

        gyroSensor = dev.GetSensorList.GetSensor(SensorType.OB_SENSOR_GYRO)
        accelSensor = dev.GetSensorList.GetSensor(SensorType.OB_SENSOR_ACCEL)

        Dim myIntrinsics = dev.GetCalibrationCameraParamList()
        Dim param As CameraParam = myIntrinsics.GetCameraParam(0)
        cameraInfo.fx = param.rgbIntrinsic.fx
        cameraInfo.fy = param.rgbIntrinsic.fy
        cameraInfo.ppx = param.rgbIntrinsic.cx
        cameraInfo.ppy = param.rgbIntrinsic.cy

        pipe.EnableFrameSync()
        pipe.Start(config)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        Dim rows = captureRes.Height, cols = captureRes.Width
        Static PointCloud As New PointCloudFilter
        Static cameraParams As CameraParam = pipe.GetCameraParam()
        Static orbMutex As New Mutex(True, "orbMutex")
        Static acceleration As cvb.Point3f, angularVelocity As cvb.Point3f, timeStamp As Int64

        Dim frames = pipe.WaitForFrames(2000)
        If cameraFrameCount = 0 Then
            PointCloud.SetCameraParam(cameraParams)

            Dim gProfiles = gyroSensor.GetStreamProfileList()
            Dim gProfile = gProfiles.GetProfile(0)
            gyroSensor.Start(gProfile, Sub(frame As Orbbec.Frame)
                                           SyncLock orbMutex
                                               angularVelocity = Marshal.PtrToStructure(Of cvb.Point3f)(frame.GetDataPtr)
                                               timeStamp = frame.GetTimeStamp
                                           End SyncLock
                                       End Sub)

            Dim accProfiles = accelSensor.GetStreamProfileList()
            Dim accProfile = accProfiles.GetProfile(0)
            accelSensor.Start(accProfile, Sub(frame As Orbbec.Frame)
                                              SyncLock orbMutex
                                                  acceleration = Marshal.PtrToStructure(Of cvb.Point3f)(frame.GetDataPtr)
                                                  timeStamp = frame.GetTimeStamp
                                              End SyncLock
                                          End Sub)
        End If

        If frames Is Nothing Then Exit Sub

        Dim cFrame = frames.GetColorFrame
        Dim dFrame = frames.GetDepthFrame
        Dim lFrame = frames.GetFrame(FrameType.OB_FRAME_IR_LEFT)
        Dim rFrame = frames.GetFrame(FrameType.OB_FRAME_IR_RIGHT)
        Dim needResize = captureRes.Width <> WorkingRes.Width Or captureRes.Height <> WorkingRes.Height

        SyncLock cameraLock
            If cFrame IsNot Nothing Then
                uiColor = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, cFrame.GetDataPtr)
                If needResize Then uiColor = uiColor.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            End If

            If lFrame IsNot Nothing Then
                uiLeft = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, lFrame.GetDataPtr)
                If needResize Then uiLeft = uiLeft.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            End If

            If rFrame IsNot Nothing Then
                uiRight = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, rFrame.GetDataPtr)
                If needResize Then uiRight = uiRight.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            End If

            If dFrame IsNot Nothing Then
                Dim depthValueScale As Single = dFrame.GetValueScale()
                PointCloud.SetPositionDataScaled(depthValueScale)
                PointCloud.SetPointFormat(Format.OB_FORMAT_POINT)
                Dim pcData = PointCloud.Process(dFrame)
                uiPointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC3, pcData.GetDataPtr) / 1000
                If needResize Then uiPointCloud = uiPointCloud.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            End If

            SyncLock orbMutex
                IMU_Acceleration = acceleration
                IMU_AngularVelocity = angularVelocity
                IMU_TimeStamp = timeStamp
            End SyncLock

            If uiColor Is Nothing Then uiColor = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3)
            If uiLeft Is Nothing Then uiLeft = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3)
            If uiRight Is Nothing Then uiRight = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3)
            If uiPointCloud Is Nothing Then uiPointCloud = New cvb.Mat(WorkingRes, cvb.MatType.CV_32FC3)
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        pipe.Stop()
        pipe.Dispose()
        Application.DoEvents()
    End Sub
End Class
