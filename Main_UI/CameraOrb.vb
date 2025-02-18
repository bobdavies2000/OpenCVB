Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Threading
Imports Orbbec
Public Class CameraORB : Inherits GenericCamera
    Dim pipe As New Pipeline()
    Dim accelSensor As Sensor
    Dim gyroSensor As Sensor
    Dim orbMutex As New Mutex(True, "orbMutex")
    Dim acceleration As cv.Point3f, angularVelocity As cv.Point3f, timeStamp As Int64
    Public Sub New(WorkingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        Dim ctx As New Context
        Dim devList = ctx.QueryDeviceList()
        Dim dev = devList.GetDevice(0)

        Dim fps = 0
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

        Dim gProfiles = gyroSensor.GetStreamProfileList()
        Dim gProfile = gProfiles.GetProfile(0)
        gyroSensor.Start(gProfile, Sub(frame As Orbbec.Frame)
                                       SyncLock orbMutex
                                           angularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(frame.GetDataPtr)
                                           timeStamp = frame.GetTimeStamp
                                       End SyncLock
                                   End Sub)

        Dim accProfiles = accelSensor.GetStreamProfileList()
        Dim accProfile = accProfiles.GetProfile(0)
        accelSensor.Start(accProfile, Sub(frame As Orbbec.Frame)
                                          SyncLock orbMutex
                                              acceleration = Marshal.PtrToStructure(Of cv.Point3f)(frame.GetDataPtr)
                                              timeStamp = frame.GetTimeStamp
                                          End SyncLock
                                      End Sub)
        pipe.EnableFrameSync()
        pipe.Start(config)
    End Sub
    Private Function copyIntrinsics(input As CameraIntrinsic, ratio As Single) As VB_Classes.VBtask.intrinsicData
        Dim output As New VB_Classes.VBtask.intrinsicData
        output.ppx = input.cx / ratio
        output.ppy = input.cy / ratio
        output.fx = input.fx / ratio
        output.fy = input.fy / ratio
        Return output
    End Function
    Public Sub GetNextFrame(WorkingRes As cv.Size)
        Dim rows = captureRes.Height, cols = captureRes.Width
        Static PtCloud As New PointCloudFilter
        ' turning on the right view overworks the camera processor.  Reduce the work and get 30 fps reliably.  Otherwise 5 fps.
        Static color As cv.Mat, leftView As cv.Mat, pointCloud As cv.Mat, rightView As cv.Mat ' USE_RIGHT_IMAGE

        Dim frames As Frameset = Nothing
        While frames Is Nothing
            frames = pipe.WaitForFrames(100)
        End While

        If cameraFrameCount = 0 Then
            Dim param As CameraParam = pipe.GetCameraParam()
            Dim ratio = CInt(captureRes.Width / WorkingRes.Width)
            calibData.rgbIntrinsics = copyIntrinsics(param.rgbIntrinsic, ratio)
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

        If rFrame IsNot Nothing Then ' USE_RIGHT_IMAGE
            rightView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, rFrame.GetDataPtr)
        End If

        If dFrame IsNot Nothing Then
            Dim depthValueScale As Single = dFrame.GetValueScale()
            PtCloud.SetPositionDataScaled(depthValueScale)
            PtCloud.SetPointFormat(Format.OB_FORMAT_POINT)
            Dim pcData = PtCloud.Process(dFrame)
            pointCloud = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_32FC3, pcData.GetDataPtr) / 1000
        End If

        SyncLock orbMutex
            IMU_AngularVelocity = angularVelocity
            IMU_Acceleration = acceleration
            Static initialTime As Int64 = timeStamp
            IMU_FrameTime = timeStamp - initialTime
        End SyncLock

        If color Is Nothing Then color = New cv.Mat(WorkingRes, cv.MatType.CV_8UC3, 0)
        If leftView Is Nothing Then leftView = New cv.Mat(WorkingRes, cv.MatType.CV_8UC1, 0)
        If rightView Is Nothing Then rightView = New cv.Mat(WorkingRes, cv.MatType.CV_8UC1, 0)
        If pointCloud Is Nothing Then pointCloud = New cv.Mat(WorkingRes, cv.MatType.CV_32FC3, 0)

        SyncLock cameraLock
            uiColor = color.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
            uiLeft = leftView.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
            uiRight = rightView.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
            uiPointCloud = pointCloud.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
        End SyncLock

        GC.Collect() ' this GC seems to be necessary to get the color image in the VB.Net interface.
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        accelSensor.Stop()
        gyroSensor.Stop()
        pipe.Stop()
    End Sub
End Class

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
