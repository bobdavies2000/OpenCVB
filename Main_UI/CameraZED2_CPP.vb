Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Imports sl
Imports Intel.RealSense
Module Zed2_CPP_Interface
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Open(width As Integer, height As Integer) As IntPtr
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
Public Class CameraZED2_CPP : Inherits GenericCamera
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes

        ' if OpenCVB fails here, it is likely because you have turned off the StereoLabs support.
        ' Open the CameraDefines.hpp file and uncomment the StereoLab
        cPtr = Zed2Open(captureRes.Width, captureRes.Height)
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
            uiColor = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8UC3,
                                            Zed2Color(cPtr)).Clone
            uiRight = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8UC3,
                                            Zed2RightView(cPtr)).Clone
            uiLeft = uiColor

            uiPointCloud = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width,
                                                 cvb.MatType.CV_32FC3, Zed2PointCloud(cPtr)).Clone

            uiPointCloud = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_32FC3,
                                           Zed2PointCloud(cPtr)).CvtColor(cvb.ColorConversionCodes.BGRA2BGR)


            Dim accPtr = Zed2Acceleration(cPtr)
            Dim accel = Marshal.PtrToStructure(Of cvb.Point3f)(accPtr)
            IMU_Acceleration = New cvb.Point3f(accel.X, accel.Y, -accel.Z)

            Dim angPtr = Zed2AngularVelocity(cPtr)
            IMU_AngularVelocity = Marshal.PtrToStructure(Of cvb.Point3f)(angPtr)
            IMU_AngularVelocity *= 0.0174533 ' Zed 2 gyro is in degrees/sec
            IMU_AngularVelocity.Z *= -1 ' make it consistent with the other cameras.

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