Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module OakD_Module_CPP
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDOpen(width As Integer, height As Integer) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OakDWaitForFrame(tp As IntPtr)
    End Sub
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDRightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDColor(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDLeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDDisparity(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDintrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDExtrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDPointCloud(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDRGBDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDRawDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDGyro(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDIMUTimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDAccel(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDDepthScale(tp As IntPtr) As Single
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OakDStop(tp As IntPtr)
    End Sub
End Module

Structure OakDIMUdata
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Integer
    Public mapperConfidence As Integer
End Structure
Public Class CameraOakD : Inherits Camera
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
        cPtr = OakDOpen(width, height, deviceIndex)
        depthScale = OakDDepthScale(cPtr) * 1000
        Dim intrin = OakDintrinsicsLeft(cPtr)
        intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        intrinsicsLeft_VB = setintrinsics(intrinsicsLeft)
        intrinsicsRight_VB = intrinsicsLeft_VB ' need to get the Right lens intrinsics?

        Dim extrin = OakDExtrinsics(cPtr)
        Dim extrinsics As rs.Extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin) ' they are both float's
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U, 0)
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U, 0)
    End Sub
    Public Sub GetNextFrame()
        If cPtr = 0 Then Exit Sub
        OakDWaitForFrame(cPtr)
        color = New cv.Mat(height, width, cv.MatType.CV_8UC3, OakDColor(cPtr))

        Dim accelFrame = OakDAccel(cPtr)
        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

        Dim gyroFrame = OakDGyro(cPtr)
        If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

        Static imuStartTime = OakDIMUTimeStamp(cPtr)
        IMU_TimeStamp = OakDIMUTimeStamp(cPtr) - imuStartTime

        RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, OakDRGBDepth(cPtr))
        depth16 = New cv.Mat(height, width, cv.MatType.CV_16U, OakDRawDepth(cPtr)) * depthScale
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U, OakDLeftRaw(cPtr))
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U, OakDRightRaw(cPtr))
        pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3, OakDPointCloud(cPtr))
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        Application.DoEvents()
        If cPtr <> 0 Then OakDStop(cPtr)
        cPtr = 0
    End Sub
End Class