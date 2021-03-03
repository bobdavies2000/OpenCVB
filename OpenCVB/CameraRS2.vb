Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module RS2_Module_CPP
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Open(width As Integer, height As Integer, serialNumber As Integer) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RS2WaitForFrame(tp As IntPtr)
    End Sub
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2RightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Color(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2LeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Disparity(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2intrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Extrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2PointCloud(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2RGBDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2RawDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Gyro(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2IMUTimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Accel(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2DepthScale(tp As IntPtr) As Single
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RS2Stop(tp As IntPtr)
    End Sub
End Module

Structure RS2IMUdata
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As integer
    Public mapperConfidence As integer
End Structure
Public Class CameraRS2
    Inherits Camera

    Dim ctx As New rs.Context
    Public deviceNum As Integer
    Dim intrinsicsLeft As rs.Intrinsics
    Public cameraName As String
    Dim depthScale As Single
    Public cPtr As IntPtr
    Public Sub New()
    End Sub
    Public Function queryDeviceCount() As Integer
        Dim Devices = ctx.QueryDevices()
        Return ctx.QueryDevices().Count
    End Function
    Public Function queryDevice(index As Integer) As String
        Dim Devices = ctx.QueryDevices()
        Return Devices(index).Info(0)
    End Function
    Public Function querySerialNumber(index As Integer) As String
        Dim Devices = ctx.QueryDevices()
        Console.WriteLine("Intel RealSense Firmware Version: " + Devices(index).Info(2))
        Return Devices(index).Info(1)
    End Function
    Public Sub initialize(_width As Integer, _height As Integer, fps As Integer)
        width = _width
        height = _height
        deviceName = cameraName
        cPtr = RS2Open(width, height, deviceIndex)
        depthScale = RS2DepthScale(cPtr) * 1000
        Dim intrin = RS2intrinsicsLeft(cPtr)
        intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        intrinsicsLeft_VB = setintrinsics(intrinsicsLeft)
        intrinsicsRight_VB = intrinsicsLeft_VB ' need to get the Right lens intrinsics?

        Dim extrin = RS2Extrinsics(cPtr)
        Dim extrinsics As rs.Extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin) ' they are both float's
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U, 0)
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U, 0)
    End Sub
    Public Sub GetNextFrame()
        If cPtr = 0 Then Exit Sub
        RS2WaitForFrame(cPtr)
        color = New cv.Mat(height, width, cv.MatType.CV_8UC3, RS2Color(cPtr)).Clone()

        Dim accelFrame = RS2Accel(cPtr)
        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

        Dim gyroFrame = RS2Gyro(cPtr)
        If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

        Static imuStartTime = RS2IMUTimeStamp(cPtr)
        IMU_TimeStamp = RS2IMUTimeStamp(cPtr) - imuStartTime

        RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, RS2RGBDepth(cPtr)).Clone()
        depth16 = New cv.Mat(height, width, cv.MatType.CV_16U, RS2RawDepth(cPtr)) * depthScale
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U, RS2LeftRaw(cPtr)).Clone()
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U, RS2RightRaw(cPtr)).Clone()
        pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3, RS2PointCloud(cPtr)).Clone()
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        Application.DoEvents()
        If cPtr <> 0 Then RS2Stop(cPtr)
        cPtr = 0
    End Sub
End Class