Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports rs = Intel.RealSense
Module Kinect_Interface
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectOpen(width As Integer, height As Integer) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDeviceCount(cPtr As IntPtr) As integer
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDeviceName(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectWaitFrame(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectExtrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectintrinsicsLeft(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectPointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectRGBdepth(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectRGBA(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectLeftView(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDepthInColor(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectRawDepth(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub KinectClose(cPtr As IntPtr)
    End Sub
End Module
Public Class CameraKinect : Inherits Camera
    Public cameraName As String
    Public cPtr As IntPtr
    Structure imuData
        Dim temperature As Single
        Dim imuAccel As cv.Point3f
        Dim accelTimeStamp As Long
        Dim imu_Gyro As cv.Point3f
        Dim gyroTimeStamp As Long
    End Structure
    Structure intrinsicsLeftData
        Dim cx As Single            ' Principal point In image, x */
        Dim cy As Single            ' Principal point In image, y */
        Dim fx As Single            ' Focal length x */
        Dim fy As Single            ' Focal length y */
        Dim k1 As Single            ' k1 radial distortion coefficient */
        Dim k2 As Single            ' k2 radial distortion coefficient */
        Dim k3 As Single            ' k3 radial distortion coefficient */
        Dim k4 As Single            ' k4 radial distortion coefficient */
        Dim k5 As Single            ' k5 radial distortion coefficient */
        Dim k6 As Single            ' k6 radial distortion coefficient */
        Dim codx As Single          ' Center Of distortion In Z=1 plane, x (only used For Rational6KT) */
        Dim cody As Single          ' Center Of distortion In Z=1 plane, y (only used For Rational6KT) */
        Dim p2 As Single            ' Tangential distortion coefficient 2 */
        Dim p1 As Single            ' Tangential distortion coefficient 1 */
        Dim metric_radius As Single ' Metric radius */
    End Structure

    Public Sub initialize(_width As Integer, _height As Integer, fps As Integer)
        width = _width
        height = _height
        cPtr = KinectOpen(width, height)
        deviceName = "Kinect for Azure"
        cameraName = deviceName
        If cPtr <> 0 Then
            deviceCount = KinectDeviceCount(cPtr)
            Dim strPtr = KinectDeviceName(cPtr) ' The width and height of the image are set in the constructor.
            serialNumber = Marshal.PtrToStringAnsi(strPtr)

            Dim ptr = KinectExtrinsics(cPtr)
            Dim extrinsics As rs.Extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(ptr)
            Extrinsics_VB.rotation = extrinsics.rotation
            Extrinsics_VB.translation = extrinsics.translation

            ptr = KinectintrinsicsLeft(cPtr)
            Dim intrinsicsLeftOutput = Marshal.PtrToStructure(Of intrinsicsLeftData)(ptr)
            intrinsicsLeft_VB.ppx = intrinsicsLeftOutput.cx
            intrinsicsLeft_VB.ppy = intrinsicsLeftOutput.cy
            intrinsicsLeft_VB.fx = intrinsicsLeftOutput.fx
            intrinsicsLeft_VB.fy = intrinsicsLeftOutput.fy
            ReDim intrinsicsLeft_VB.FOV(3 - 1)
            ReDim intrinsicsLeft_VB.coeffs(6 - 1)
            intrinsicsLeft_VB.coeffs(0) = intrinsicsLeftOutput.k1
            intrinsicsLeft_VB.coeffs(1) = intrinsicsLeftOutput.k2
            intrinsicsLeft_VB.coeffs(2) = intrinsicsLeftOutput.k3
            intrinsicsLeft_VB.coeffs(3) = intrinsicsLeftOutput.k4
            intrinsicsLeft_VB.coeffs(4) = intrinsicsLeftOutput.k5
            intrinsicsLeft_VB.coeffs(5) = intrinsicsLeftOutput.k6

            intrinsicsRight_VB = intrinsicsLeft_VB ' there is no right lens - just copy for compatibility.

            color = New cv.Mat(height, width, cv.MatType.CV_8UC3)
            depth16 = New cv.Mat(height, width, cv.MatType.CV_16U)
            RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3)
            leftView = New cv.Mat(height, width, cv.MatType.CV_8UC1)
            rightView = New cv.Mat(height, width, cv.MatType.CV_8UC1)
            pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3)

            ReDim RGBDepthBytes(width * height * 3 - 1)
        End If
    End Sub

    Public Sub GetNextFrame()
        Dim imuFrame As IntPtr
        If cPtr = 0 Then Exit Sub
        imuFrame = KinectWaitFrame(cPtr)
        If imuFrame = 0 Then
            Console.WriteLine("KinectWaitFrame has returned without any image.")
            failedImageCount += 1
            Exit Sub ' just process the existing images again?  
        Else
            Dim imuOutput = Marshal.PtrToStructure(Of imuData)(imuFrame)
            IMU_AngularVelocity = imuOutput.imu_Gyro
            IMU_Acceleration = imuOutput.imuAccel

            ' make the imu data consistent with the Intel IMU...
            Dim tmpVal = IMU_Acceleration.Z
            IMU_Acceleration.Z = IMU_Acceleration.X
            IMU_Acceleration.X = -IMU_Acceleration.Y
            IMU_Acceleration.Y = tmpVal

            tmpVal = IMU_AngularVelocity.Z
            IMU_AngularVelocity.Z = -IMU_AngularVelocity.X
            IMU_AngularVelocity.X = -IMU_AngularVelocity.Y
            IMU_AngularVelocity.Y = tmpVal

            IMU_TimeStamp = imuOutput.accelTimeStamp / 1000
        End If

        Dim colorBuffer = KinectRGBA(cPtr)
        If colorBuffer <> 0 Then ' it can be zero on startup...
            Dim colorRGBA = New cv.Mat(height, width, cv.MatType.CV_8UC4, colorBuffer)
            color = colorRGBA.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
            depth16 = New cv.Mat(height, width, cv.MatType.CV_16U, KinectRawDepth(cPtr))
            RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, KinectRGBdepth(cPtr))
            ' if you normalize here instead of just a fixed multiply, the image will pulse with different brightness values.  Not pretty.
            leftView = (New cv.Mat(height, width, cv.MatType.CV_16U, KinectLeftView(cPtr)) * 0.06).ToMat.ConvertScaleAbs() ' so depth data fits into 0-255 (approximately)
            rightView = leftView.Clone

            Dim pc = New cv.Mat(height, width, cv.MatType.CV_16SC3, KinectPointCloud(cPtr))
            ' This is less efficient than using 16-bit pixels but consistent with the other cameras
            pc.ConvertTo(pointCloud, cv.MatType.CV_32FC3, 0.001) ' convert to meters...
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End If
    End Sub
    Public Sub stopCamera()
        If cPtr <> 0 Then KinectClose(cPtr)
        cPtr = 0
    End Sub
End Class
