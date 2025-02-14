Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports Intel.RealSense
Imports System.Dynamic
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
    Public Sub New(WorkingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
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
        calibData.baseline = 0.119
        calibData.ppx = intrinsics.cx
        calibData.ppy = intrinsics.cy
        calibData.fx = intrinsics.fx
        calibData.fy = intrinsics.fy
        calibData.v_fov = intrinsics.v_fov
        calibData.h_fov = intrinsics.h_fov
        calibData.d_fov = intrinsics.d_fov
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
    Public Sub GetNextFrame(WorkingRes As cv.Size)
        Zed2WaitForFrame(cPtr)

        If cPtr = 0 Then Exit Sub
        Zed2GetData(cPtr, WorkingRes.Width, WorkingRes.Height)

        SyncLock cameraLock
            uiColor = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC4,
                                            Zed2Color(cPtr)).Clone
            uiColor = uiColor.CvtColor(cv.ColorConversionCodes.BGRA2BGR).Resize(WorkingRes, 0, 0,
                                       cv.InterpolationFlags.Nearest)

            uiRight = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC4,
                                            Zed2RightView(cPtr)).Clone

            uiRight = uiColor.CvtColor(cv.ColorConversionCodes.BGRA2BGR).Resize(WorkingRes, 0, 0,
                                       cv.InterpolationFlags.Nearest)

            uiRight = uiRight.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            uiLeft = uiColor.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            uiPointCloud = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width,
                           cv.MatType.CV_32FC4, Zed2PointCloud(cPtr)).CvtColor(cv.ColorConversionCodes.BGRA2BGR)

            uiPointCloud = uiPointCloud.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)

            Dim tmp = uiPointCloud.Reshape(1)
            cv.Cv2.PatchNaNs(tmp, 0)
            uiPointCloud = tmp.Reshape(3)

            Dim accPtr = Zed2Acceleration(cPtr)
            Dim accel = Marshal.PtrToStructure(Of cv.Point3f)(accPtr)
            IMU_Acceleration = New cv.Point3f(accel.X, accel.Y, -accel.Z)

            Dim angPtr = Zed2AngularVelocity(cPtr)
            IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(angPtr)
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