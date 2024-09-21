Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Imports System.Threading
Imports Orbbec
' switch between the VB.Net version of the Orbbec camera interface and the C++ one.
#If 1 Then
Public Class CameraORB : Inherits GenericCamera
    Dim pipe As New Pipeline()
    Dim accelSensor As Sensor
    Dim gyroSensor As Sensor
    Dim orbMutex As New Mutex(True, "orbMutex")
    Dim acceleration As cvb.Point3f, angularVelocity As cvb.Point3f, timeStamp As Int64
    Dim timeStampList As New List(Of Int64)
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes
        Dim ctx As New Context
        Dim devList = ctx.QueryDeviceList()
        Dim dev = devList.GetDevice(0)

        Dim fps = 5
        Dim w = captureRes.Width, h = captureRes.Height
        Dim colorProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_COLOR).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_BGR, fps)
        Dim depthProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_DEPTH).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y16, fps)
        Dim leftProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_LEFT).
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y8, fps)
        Dim rightProfile As StreamProfile = pipe.GetStreamProfileList(SensorType.OB_SENSOR_IR_RIGHT). ' USE_RIGHT_IMAGE
                                            GetVideoStreamProfile(w, h, Format.OB_FORMAT_Y8, fps)
        Dim config As New Config()
        config.EnableStream(colorProfile)
        config.EnableStream(depthProfile)
        config.EnableStream(leftProfile)
        config.EnableStream(rightProfile) ' USE_RIGHT_IMAGE
        config.SetAlignMode(AlignMode.ALIGN_D2C_SW_MODE)

        gyroSensor = dev.GetSensorList.GetSensor(SensorType.OB_SENSOR_GYRO)
        accelSensor = dev.GetSensorList.GetSensor(SensorType.OB_SENSOR_ACCEL)

        Dim myIntrinsics = dev.GetCalibrationCameraParamList()
        Dim param As CameraParam = myIntrinsics.GetCameraParam(0)
        cameraInfo.fx = param.rgbIntrinsic.fx
        cameraInfo.fy = param.rgbIntrinsic.fy
        cameraInfo.ppx = param.rgbIntrinsic.cx
        cameraInfo.ppy = param.rgbIntrinsic.cy

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
        pipe.EnableFrameSync()
        pipe.Start(config)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        Dim rows = captureRes.Height, cols = captureRes.Width
        Static PtCloud As New PointCloudFilter
        ' turning on the right view overworks the camera processor.  Reduce the work and get 30 fps reliably.  Otherwise 5 fps.
        Static color As cvb.Mat, leftView As cvb.Mat, pointCloud As cvb.Mat, rightView As cvb.Mat ' USE_RIGHT_IMAGE

        Dim frames As Frameset = Nothing
        While frames Is Nothing
            frames = pipe.WaitForFrames(100)
        End While

        If cameraFrameCount = 0 Then
            Dim cameraParams As CameraParam = pipe.GetCameraParam()
            PtCloud.SetCameraParam(cameraParams)
        End If

        Dim cFrame = frames.GetColorFrame
        Dim dFrame = frames.GetDepthFrame
        Dim lFrame = frames.GetFrame(FrameType.OB_FRAME_IR_LEFT)
        Dim rFrame = frames.GetFrame(FrameType.OB_FRAME_IR_RIGHT)

        If cFrame IsNot Nothing Then
            color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, cFrame.GetDataPtr)
        End If

        If lFrame IsNot Nothing Then
            leftView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, lFrame.GetDataPtr)
        End If

        If rFrame IsNot Nothing Then ' USE_RIGHT_IMAGE
            rightView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, rFrame.GetDataPtr)
        End If

        If dFrame IsNot Nothing Then
            Dim depthValueScale As Single = dFrame.GetValueScale()
            PtCloud.SetPositionDataScaled(depthValueScale)
            PtCloud.SetPointFormat(Format.OB_FORMAT_POINT)
            Dim pcData = PtCloud.Process(dFrame)
            pointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC3, pcData.GetDataPtr) / 1000
        End If

        SyncLock orbMutex
            IMU_AngularVelocity = angularVelocity
            IMU_Acceleration = acceleration
            Static initialTime As Int64 = timeStamp
            IMU_FrameTime = timeStamp - initialTime
        End SyncLock

        If color Is Nothing Then color = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3)
        If leftView Is Nothing Then leftView = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3)
        If rightView Is Nothing Then rightView = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3) ' USE_RIGHT_IMAGE
        If pointCloud Is Nothing Then pointCloud = New cvb.Mat(WorkingRes, cvb.MatType.CV_32FC3)

        SyncLock cameraLock
            uiColor = color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            uiLeft = leftView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            uiRight = rightView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest) ' USE_RIGHT_IMAGE
            'uiRight = color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest) '  note duplicate of color for right view - less stress on camera.
            uiPointCloud = pointCloud.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
        End SyncLock

        GC.Collect()
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        accelSensor.Stop()
        gyroSensor.Stop()
        pipe.Stop()
    End Sub
End Class
#Else
Module ORB_Module
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBWaitForFrame(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBRightImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBColor(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBLeftImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBIntrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBPointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBGyro(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBIMUTimeStamp(cPtr As IntPtr) As Double
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBAccel(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub ORBClose(cPtr As IntPtr)
    End Sub
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBOpen(
                                                   width As Integer, height As Integer) As IntPtr
    End Function
End Module
Public Class CameraORB : Inherits GenericCamera
    Public deviceNum As Integer
    Public deviceName As String
    Public cPtrOpen As IntPtr
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes

        cPtr = ORBOpen(captureRes.Width, captureRes.Height)
        Dim intrin = ORBIntrinsics(cPtr)
        Dim intrinInfo(4 - 1) As Single
        Marshal.Copy(intrin, intrinInfo, 0, intrinInfo.Length)
        cameraInfo.ppx = intrinInfo(0)
        cameraInfo.ppy = intrinInfo(1)
        cameraInfo.fx = intrinInfo(2)
        cameraInfo.fy = intrinInfo(3)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        Static color As cvb.Mat, leftView As cvb.Mat, rightView As cvb.Mat, pointCloud As cvb.Mat

        If cPtr = 0 Then Exit Sub
        Dim cols = captureRes.Width, rows = captureRes.Height

        Dim colorData = ORBWaitForFrame(cPtr)

        If colorData <> 0 Then
            color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, colorData).Clone
        Else
            color = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3, New cvb.Scalar(0))
        End If

        Dim pcData = ORBPointCloud(cPtr)
        If pcData <> 0 Then
            pointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC3, pcData) * 0.001
        Else
            pointCloud = New cvb.Mat(WorkingRes, cvb.MatType.CV_32FC3, New cvb.Scalar(0))
        End If

        Dim leftData = ORBLeftImage(cPtr)
        If leftData <> 0 Then
            leftView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8U, leftData).
                                             CvtColor(cvb.ColorConversionCodes.GRAY2BGR) * 3
        Else
            leftView = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3, New cvb.Scalar(0))

        End If

        Dim rightData = ORBRightImage(cPtr)
        If rightData <> 0 Then
            rightView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8U, rightData).
                                              CvtColor(cvb.ColorConversionCodes.GRAY2BGR) * 3
        Else
            rightView = New cvb.Mat(WorkingRes, cvb.MatType.CV_8UC3, New cvb.Scalar(0))
        End If

        Dim accelFrame = ORBAccel(cPtr)
        Dim gyroFrame = ORBGyro(cPtr)
        SyncLock cameraLock
            Static imuStartTime = ORBIMUTimeStamp(cPtr)
            If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cvb.Point3f)(accelFrame)
            If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cvb.Point3f)(gyroFrame)
            IMU_TimeStamp = ORBIMUTimeStamp(cPtr) - imuStartTime

            uiColor = color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            uiLeft = leftView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            uiRight = rightView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            uiPointCloud = pointCloud.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        Application.DoEvents()
        Try
            ORBClose(cPtr)
        Catch ex As Exception
        End Try
        cPtr = 0
    End Sub
End Class
#End If