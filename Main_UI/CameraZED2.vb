Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Imports sl
#If 1 Then
Public Class CameraZED2 : Inherits GenericCamera
    Dim zed As sl.Camera
    Dim init_params As New InitParameters()
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes
        init_params.cameraFPS = 100
        If captureRes.Width = 960 Then init_params.cameraFPS = 120
        If captureRes.Width = 1920 And captureRes.Height = 1080 Then init_params.cameraFPS = 30
        If captureRes.Width = 1920 And captureRes.Height = 1200 Then init_params.cameraFPS = 60
        If captureRes.Width = 1280 And captureRes.Height = 720 Then init_params.cameraFPS = 60

        init_params.sensorsRequired = True
        init_params.depthMode = sl.DEPTH_MODE.ULTRA
        init_params.coordinateSystem = sl.COORDINATE_SYSTEM.RIGHT_HANDED_Y_UP
        init_params.coordinateUnits = sl.UNIT.METER

        If captureRes.Height = 720 Then init_params.resolution = sl.RESOLUTION.HD720
        If captureRes.Height = 1080 Then init_params.resolution = sl.RESOLUTION.HD1080
        If captureRes.Height = 1200 Then init_params.resolution = sl.RESOLUTION.HD720
        If captureRes.Height = 376 Then init_params.resolution = sl.RESOLUTION.VGA

        zed = New sl.Camera(0)
        Dim errCode = zed.Open(init_params)

        Dim camInfo As sl.CameraInformation = zed.GetCameraInformation

        cameraInfo.ppx = camInfo.cameraConfiguration.calibrationParameters.leftCam.cx
        cameraInfo.ppy = camInfo.cameraConfiguration.calibrationParameters.leftCam.cy
        cameraInfo.fx = camInfo.cameraConfiguration.calibrationParameters.leftCam.fx
        cameraInfo.fy = camInfo.cameraConfiguration.calibrationParameters.leftCam.fy

        Dim posTrack As New sl.PositionalTrackingParameters
        posTrack.enableAreaMemory = True
        zed.EnablePositionalTracking(posTrack)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        Static RuntimeParameters = New RuntimeParameters()
        Dim rows = captureRes.Height, cols = captureRes.Width
        Dim w = WorkingRes.Width, h = WorkingRes.Height
        While 1
            Dim rc = zed.Grab(RuntimeParameters)
            If rc = 0 Then Exit While
        End While

        Dim color As New cvb.Mat, leftView As New cvb.Mat, rightView As New cvb.Mat, pointCloud As New cvb.Mat
        Static colorSL As New sl.Mat(New sl.ResolutionStruct(rows, cols), sl.MAT_TYPE.MAT_8U_C3)
        Static rightSL As New sl.Mat(New sl.ResolutionStruct(rows, cols), sl.MAT_TYPE.MAT_8U_C3)
        Static pointCloudSL As New sl.Mat(New sl.ResolutionStruct(rows, cols), sl.MAT_TYPE.MAT_8U_C3)

        zed.RetrieveImage(colorSL, sl.VIEW.LEFT)
        color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC4, colorSL.GetPtr).
                                      CvtColor(cvb.ColorConversionCodes.BGRA2BGR)

        zed.RetrieveImage(rightSL, sl.VIEW.RIGHT)
        rightView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC4, rightSL.GetPtr).CvtColor(cvb.ColorConversionCodes.BGRA2BGR)

        zed.RetrieveMeasure(pointCloudSL, sl.MEASURE.XYZ)
        pointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC4, pointCloudSL.GetPtr).CvtColor(cvb.ColorConversionCodes.BGRA2BGR)
        cvb.Cv2.PatchNaNs(pointCloud, 0)

        Dim zed_pose As New sl.Pose
        zed.GetPosition(zed_pose, REFERENCE_FRAME.WORLD)
        Dim sensordata As New sl.SensorsData
        zed.GetSensorsData(sensordata, TIME_REFERENCE.CURRENT)

        SyncLock cameraLock
            Dim acc = sensordata.imu.linearAcceleration
            IMU_Acceleration = New cvb.Point3f(-acc.X, acc.Y, -acc.Z)
            Dim gyro = sensordata.imu.angularVelocity
            IMU_AngularVelocity = New cvb.Point3f(gyro.X, gyro.Y, gyro.Z) * 0.0174533 ' Zed 2 gyro is in degrees/sec 
            Static IMU_StartTime = sensordata.imu.timestamp
            IMU_TimeStamp = (sensordata.imu.timestamp - IMU_StartTime) / 4000000 ' crude conversion to milliseconds.

            If WorkingRes <> captureRes Then
                uiColor = color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                uiLeft = color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                uiRight = rightView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                uiPointCloud = pointCloud.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest).Clone
            Else
                uiColor = color.Clone
                uiLeft = color.Clone
                uiRight = rightView.Clone
                uiPointCloud = pointCloud.Clone
            End If
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        zed.Close()
    End Sub
End Class


#Else

Module Zed2_Interface
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Open(width As Integer, height As Integer, fps As Integer) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub Zed2Close(cPtr As IntPtr)
    End Sub
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Zed2SerialNumber(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Zed2Acceleration(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Zed2AngularVelocity(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Zed2IMU_TimeStamp(cPtr As IntPtr) As Double
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub Zed2WaitForFrame(cPtr As IntPtr)
    End Sub
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub Zed2GetData(cPtr As IntPtr, w As Integer, h As Integer)
    End Sub
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Zed2Color(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Zed2PointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Zed2GetPoseData(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Zed2RightView(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function Zed2Intrinsics(cPtr As IntPtr) As IntPtr
    End Function
End Module
Public Class CameraZED2 : Inherits GenericCamera
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes
        Dim fps = 100
        If captureRes.Width = 960 Then fps = 120
        If captureRes.Width = 1920 And captureRes.Height = 1080 Then fps = 30
        If captureRes.Width = 1920 And captureRes.Height = 1200 Then fps = 60
        If captureRes.Width = 1280 And captureRes.Height = 720 Then fps = 60

        ' if OpenCVB fails here, it is likely because you have turned off the StereoLabs support.
        ' Open the CameraDefines.hpp file and uncomment the StereoLab
        cPtr = Zed2Open(captureRes.Width, captureRes.Height, fps)
        cameraName = deviceName
        If cPtr <> 0 Then
            deviceCount = 1
            Dim serialNumber = Zed2SerialNumber(cPtr)
        End If
        Dim intrinsics = Marshal.PtrToStructure(Of intrinsicsZed)(Zed2Intrinsics(cPtr))
        cameraInfo.ppx = intrinsics.cx
        cameraInfo.ppy = intrinsics.cy
        cameraInfo.fx = intrinsics.fx
        cameraInfo.fy = intrinsics.fy
        cameraInfo.v_fov = intrinsics.v_fov
        cameraInfo.h_fov = intrinsics.h_fov
        cameraInfo.d_fov = intrinsics.d_fov
    End Sub
    Structure intrinsicsZed
        Dim cx As Single ' Principal point In image, x */
        Dim cy As Single ' Principal point In image, y */
        Dim fx As Single ' Focal length x */
        Dim fy As Single ' Focal length y */
        Dim v_fov As Single ' vertical field of view in degrees.
        Dim h_fov As Single ' horizontal field of view in degrees.
        Dim d_fov As Single ' diagonal field of view in degrees.
    End Structure
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        Zed2WaitForFrame(cPtr)

        If cPtr = 0 Then Exit Sub
        Zed2GetData(cPtr, WorkingRes.Width, WorkingRes.Height)

        SyncLock cameraLock
            uiColor = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8UC3, Zed2Color(cPtr)).Clone
            uiRight = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8UC3, Zed2RightView(cPtr)).Clone
            uiLeft = uiColor.Clone

            uiPointCloud = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_32FC3,
                                                 Zed2PointCloud(cPtr)).Clone


            Dim samples(uiPointCloud.Total * 3 - 1) As Single
            Marshal.Copy(uiPointCloud.Data, samples, 0, samples.Length)


            Dim acc = Zed2Acceleration(cPtr)
            IMU_Acceleration = Marshal.PtrToStructure(Of cvb.Point3f)(acc)
            IMU_Acceleration.Y *= -1 ' make it consistent with the other cameras.

            Dim ang = Zed2AngularVelocity(cPtr)
            IMU_AngularVelocity = Marshal.PtrToStructure(Of cvb.Point3f)(ang)
            IMU_AngularVelocity *= 0.0174533 ' Zed 2 gyro is in degrees/sec
            IMU_AngularVelocity.Z *= -1 ' make it consistent with the other cameras.

            'Dim rt = Marshal.PtrToStructure(Of imuDataStruct)(imuFrame)
            'Dim t = New cvb.Point3f(rt.tx, rt.ty, rt.tz)
            'Dim mat() As Single = {-rt.r00, rt.r01, -rt.r02, 0.0,
            '                       -rt.r10, rt.r11, rt.r12, 0.0,
            '                       -rt.r20, rt.r21, -rt.r22, 0.0,
            '                       t.X, t.Y, t.Z, 1.0}
            'transformationMatrix = mat

            IMU_TimeStamp = Zed2IMU_TimeStamp(cPtr)
            Static imuStartTime = IMU_TimeStamp
            IMU_TimeStamp -= imuStartTime
        End SyncLock

        GC.Collect()
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        Zed2Close(cPtr)
        cPtr = 0
    End Sub
End Class
#End If