Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Imports System.Runtime
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports sl

'Public Class CameraZED2 : Inherits GenericCamera
'    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
'        captureRes = _captureRes
'        Dim init_params As New InitParameters()
'        init_params.camera_resolution = sl.Resolution.HD720
'        init_params.resolution = sl.Resolution.HD720


'        Dim zedCamera As New Camera(0)

'        Dim mWidth As UInteger = CUInt(zedCamera.ImageWidth)
'        Dim mHeight As UInteger = CUInt(zedCamera.ImageHeight)

'        Dim RuntimeParameters = New RuntimeParameters()
'        'Dim intrinsics = Marshal.PtrToStructure(Of intrinsicsZed)(Zed2Intrinsics(cPtr))
'        'cameraInfo.ppx = intrinsics.cx
'        'cameraInfo.ppy = intrinsics.cy
'        'cameraInfo.fx = intrinsics.fx
'        'cameraInfo.fy = intrinsics.fy
'        'cameraInfo.v_fov = intrinsics.v_fov
'        'cameraInfo.h_fov = intrinsics.h_fov
'        'cameraInfo.d_fov = intrinsics.d_fov
'    End Sub
'    Public Sub GetNextFrame(WorkingRes As cvb.Size)
'        Dim rows = captureRes.Height, cols = captureRes.Width
'        With mbuf(mbIndex)
'            .color = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC3, New cvb.Scalar(0))
'            .leftView = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC3, New cvb.Scalar(0))
'            .rightView = New cvb.Mat(rows, cols, cvb.MatType.CV_8UC3, New cvb.Scalar(0))
'            .pointCloud = New cvb.Mat(rows, cols, cvb.MatType.CV_32FC3, New cvb.Scalar(0))
'        End With
'        MyBase.GetNextFrameCounts(IMU_FrameTime)
'    End Sub
'    Public Sub stopCamera()
'    End Sub
'End Class




#If 0 Then
Dim init_params As New InitParameters()
init_params.resolution = RESOLUTION.HD1080

Dim zedCamera As New Camera(0)
' Open the camera
Dim err As ERROR_CODE = zedCamera.Open(init_params)
If err <> ERROR_CODE.SUCCESS Then
    Environment.Exit(-1)
End If

' Get resolution of camera
Dim mWidth As UInteger = CUInt(zedCamera.ImageWidth)
Dim mHeight As UInteger = CUInt(zedCamera.ImageHeight)

' Initialize the Mat that will contain the left image
Dim image As New Mat()
image.Create(mWidth, mHeight, MAT_TYPE.MAT_8U_C4, MEM.CPU) ' Mat need to be created before use.

' Define default Runtime parameters
Dim runtimeParameters As New RuntimeParameters()

' Initialize runtime parameters and frame counter
Dim i As Integer = 0
While i < 1000
    If zedCamera.Grab(runtimeParameters) = ERROR_CODE.SUCCESS Then
        zedCamera.RetrieveImage(image, VIEW.LEFT) ' Get the left image
        Dim timestamp As ULong = zedCamera.GetCameraTimeStamp() ' Get image timestamp
        Console.WriteLine("Image resolution: " & image.GetWidth() & "x" & image.GetHeight() & "|| Image timestamp: " & timestamp)
        ' increment frame count
        i += 1
    End If
End While

' Disable positional tracking and close the camera
zedCamera.DisablePositionalTracking("")
zedCamera.Close()
#End If
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
            mbuf(mbIndex).color = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8UC3,
                                             Zed2Color(cPtr)).Clone
            mbuf(mbIndex).rightView = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8UC3,
                                                 Zed2RightView(cPtr)).Clone
            mbuf(mbIndex).leftView = mbuf(mbIndex).color.Clone

            mbuf(mbIndex).pointCloud = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_32FC3,
                                                      Zed2PointCloud(cPtr)).Clone
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

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        Zed2Close(cPtr)
        cPtr = 0
    End Sub
End Class