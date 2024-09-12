Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Controls
Imports cvb = OpenCvSharp
Imports Orbbec
Public Class CameraORB : Inherits Camera
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
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_BGR, 0)
        Dim depthProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_DEPTH).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y16, 0)
        Dim leftProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_LEFT).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y8, 0)
        Dim rightProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_RIGHT).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y8, 0)
        Dim config As New Config()
        config.EnableStream(colorProfile)
        config.EnableStream(depthProfile)
        config.EnableStream(leftProfile)
        config.EnableStream(rightProfile)
        config.SetAlignMode(AlignMode.ALIGN_D2C_SW_MODE)

        gyroSensor = dev.GetSensorList.GetSensor(SensorType.OB_SENSOR_GYRO)
        accelSensor = dev.GetSensorList.GetSensor(SensorType.OB_SENSOR_ACCEL)

        pipe.EnableFrameSync()
        pipe.Start(config)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        Dim rows = captureRes.Height, cols = captureRes.Width
        Static PointCloud As New PointCloudFilter
        Static cameraParams As CameraParam = pipe.GetCameraParam()
        Static orbMutex As New Mutex(True, "orbMutex")
        Static acceleration As cvb.Point3f, angularVelocity As cvb.Point3f, timeStamp As Int64
        With mbuf(mbIndex)
            Using frames = pipe.WaitForFrames(100)
                If frames Is Nothing Then Exit Sub
                If cameraFrameCount = 0 Then
                    .color = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC3, New cvb.Scalar(0))
                    .leftView = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC1, New cvb.Scalar(0))
                    .rightView = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC1, New cvb.Scalar(0))
                    .pointCloud = New cvb.Mat(rows, cols, cvb.MatType.CV_32FC3)
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

                Dim colorFrame = frames?.GetColorFrame
                Dim depthFrame = frames?.GetDepthFrame
                Dim leftFrame = frames?.GetFrame(FrameType.OB_FRAME_IR_LEFT)
                Dim rightFrame = frames?.GetFrame(FrameType.OB_FRAME_IR_RIGHT)

                If colorFrame IsNot Nothing Then
                    .color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, colorFrame.GetDataPtr)
                End If

                If leftFrame IsNot Nothing Then
                    .leftView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, leftFrame.GetDataPtr)
                End If

                If rightFrame IsNot Nothing Then
                    .rightView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, rightFrame.GetDataPtr)
                End If
                If depthFrame IsNot Nothing Then
                    Dim depthValueScale As Single = depthFrame.GetValueScale()
                    PointCloud.SetPositionDataScaled(depthValueScale)
                    PointCloud.SetPointFormat(Format.OB_FORMAT_POINT)
                    Dim pcData = PointCloud.Process(depthFrame)
                    .pointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC3, pcData.GetDataPtr) / 1000
                End If
            End Using

            If captureRes.Width <> WorkingRes.Width Or captureRes.Height <> WorkingRes.Height Then
                .color = .color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                .leftView = .leftView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                .rightView = .rightView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                .pointCloud = .pointCloud.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            End If
            SyncLock orbMutex
                IMU_Acceleration = acceleration
                IMU_AngularVelocity = angularVelocity
                IMU_TimeStamp = timeStamp
            End SyncLock
        End With
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        pipe.Stop()
    End Sub
End Class
