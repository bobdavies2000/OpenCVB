Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module OakD_Module_CPP
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDOpen(width As Integer, height As Integer) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub OakDWaitForFrame(tp As IntPtr)
    End Sub
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDRightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDColor(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDLeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDDisparity(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDintrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDintrinsicsRight(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDExtrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDPointCloud(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDRGBDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDRawDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDGyro(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDIMUTimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDAccel(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDDepthScale(tp As IntPtr) As Single
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub OakDStop(tp As IntPtr)
    End Sub
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDMaxDisparity(tp As IntPtr) As Single
    End Function
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
    Public deviceNum As Integer
    Public accel As cv.Point3f
    Public gyro As cv.Point3f
    Dim intrinsicsLeft(9 - 1) As Single
    Dim intrinsicsRight(9 - 1) As Single
    Dim intrinsicsRGB(9 - 1) As Single
    Public cameraName = "OakD"
    Public cPtr As IntPtr
    Dim maxDisparity As Single
    Dim myLut As New cv.Mat(1, 256, cv.MatType.CV_8U)
    Dim colorMap As New cv.Mat
    Public palette As cv.ColormapTypes
    Public Sub New()
    End Sub
    Public Function queryDeviceCount() As Integer
        Return 1
    End Function
    Public Function queryDevice(index As Integer) As String
        Return "OakD"
    End Function
    Public Function querySerialNumber(index As Integer) As String
        Return "Serial1"
    End Function
    Private Sub setupLUT()
        Dim msRNG As New System.Random
        Dim c1 = cv.Scalar.Yellow
        Dim c2 = cv.Scalar.Blue
        Dim gradMat As New cv.Mat
        Dim gradientColorMap As New cv.Mat
        Dim prevColor = cv.Scalar.Yellow
        For i = 0 To 256 - 1
            Dim t = 255 / (i + 1)
            Dim nextColor As cv.Scalar = New cv.Scalar(c1.Item(0) * (1 - t) + c2.Item(0) * t, c1.Item(1) * (1 - t) + c2.Item(1) * t, c1.Item(2) * (1 - t) + c2.Item(2) * t)
            gradMat = colorTransition(prevColor, nextColor, width)
            If i = 0 Then gradientColorMap = gradMat Else cv.Cv2.HConcat(gradientColorMap, gradMat, gradientColorMap)
        Next
        gradientColorMap = gradientColorMap.Resize(New cv.Size(256, 1))
        colorMap = gradientColorMap.Flip(cv.FlipMode.X)
    End Sub
    Public Sub initialize(_width As Integer, _height As Integer, fps As Integer)
        width = _width
        height = _height
        deviceName = cameraName
        cPtr = OakDOpen(width, height)

        Dim intrin = OakDintrinsicsLeft(cPtr)
        Marshal.Copy(intrin, intrinsicsLeft, 0, intrinsicsLeft.Length - 1)

        intrin = OakDintrinsicsRight(cPtr)
        Marshal.Copy(intrin, intrinsicsRight, 0, intrinsicsRight.Length - 1)
        intrinsicsLeft_VB.ppx = intrinsicsLeft(2) ' ppx
        intrinsicsLeft_VB.ppy = intrinsicsLeft(5) ' ppy
        intrinsicsLeft_VB.fx = intrinsicsLeft(0) ' fx
        intrinsicsLeft_VB.fy = intrinsicsLeft(4) ' fy
        intrinsicsLeft_VB.FOV = {72, 81, 0}
        intrinsicsLeft_VB.coeffs = {0, 0, 0, 0, 0, 0}

        'intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        'intrinsicsLeft_VB = setintrinsics(intrinsicsLeft)
        'intrinsicsRight_VB = intrinsicsLeft_VB ' need to get the Right lens intrinsics?

        'Dim extrin = OakDExtrinsics(cPtr)
        'Dim extrinsics As rs.Extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin) ' they are both float's
        'Extrinsics_VB.rotation = extrinsics.rotation
        'Extrinsics_VB.translation = extrinsics.translation
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U, 0)
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U, 0)
        maxDisparity = OakDMaxDisparity(cPtr)
        setupLUT()
    End Sub
    Public Sub GetNextFrame()
        If cPtr = 0 Then Exit Sub
        OakDWaitForFrame(cPtr)

        Dim accelFrame = OakDAccel(cPtr)
        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

        Dim gyroFrame = OakDGyro(cPtr)
        If accelFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

        Static imuStartTime = OakDIMUTimeStamp(cPtr)
        IMU_TimeStamp = OakDIMUTimeStamp(cPtr) - imuStartTime

        'Dim disparity = New cv.Mat(height, width, cv.MatType.CV_8UC1, OakDDisparity(cPtr))
        'disparity = disparity.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        'RGBDepth = disparity.LUT(colorMap)

        color = New cv.Mat(height, width, cv.MatType.CV_8UC3, OakDColor(cPtr))
        RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, OakDRGBDepth(cPtr))
        depth16 = New cv.Mat(height, width, cv.MatType.CV_8U, OakDRawDepth(cPtr))
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U, OakDLeftRaw(cPtr))
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U, OakDRightRaw(cPtr))
        pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3, 0)
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        If cPtr <> 0 Then OakDStop(cPtr)
        cPtr = 0
    End Sub
End Class