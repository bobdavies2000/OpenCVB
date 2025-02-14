Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports Microsoft.Kinect.KinectSensor
Public Class CameraK4A : Inherits GenericCamera
    Public Sub New(WorkingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes

        cPtr = A4KOpen(captureRes.Width, captureRes.Height)
        cameraName = deviceName
        If cPtr <> 0 Then
            deviceCount = A4KDeviceCount(cPtr)
            Dim strPtr = A4KDeviceName(cPtr) ' The width and height of the image are set in the constructor.
            serialNumber = Marshal.PtrToStringAnsi(strPtr)

            Dim ptr = A4KIntrinsics(cPtr)
            Dim intrinsicsLeftOutput = Marshal.PtrToStructure(Of intrinsicsData)(ptr)
            Dim ratio = CInt(captureRes.Width / WorkingRes.Width)
            calibData.ppx = intrinsicsLeftOutput.cx / ratio
            calibData.ppy = intrinsicsLeftOutput.cy / ratio
            calibData.fx = intrinsicsLeftOutput.fx / ratio
            calibData.fy = intrinsicsLeftOutput.fy / ratio
        End If
    End Sub
    Structure imuData
        Dim placeholderForTemperature As Single
        Dim imuAccel As cv.Point3f
        Dim accelTimeStamp As Long
        Dim imu_Gyro As cv.Point3f
    End Structure
    Structure intrinsicsData
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
    Public Sub GetNextFrame(WorkingRes As cv.Size)
        Try
            Dim imuFrame As IntPtr
            If cPtr = 0 Then Exit Sub
            imuFrame = A4KWaitFrame(cPtr, WorkingRes.Width, WorkingRes.Height)
            If imuFrame = 0 Then
                Debug.WriteLine("KinectWaitFrame has returned without any image.")
                failedImageCount += 1
                Exit Sub ' just process the existing images again?  
            Else
                Dim imuOutput = Marshal.PtrToStructure(Of imuData)(imuFrame)
                IMU_AngularVelocity = imuOutput.imu_Gyro
                IMU_Acceleration = imuOutput.imuAccel

                ' make the imu data consistent with the other IMU's...
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

            If cPtr = 0 Then Exit Sub

            SyncLock cameraLock
                uiColor = cv.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width,
                                               cv.MatType.CV_8UC3, A4KColor(cPtr)).Clone

                ' so depth data fits into 0-255 (approximately)
                uiLeft = (cv.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width,
                                               cv.MatType.CV_16U, A4KLeftView(cPtr)) * 0.06).ToMat.
                                               ConvertScaleAbs()
                uiRight = uiLeft.Clone
                If captureRes <> WorkingRes Then
                    Dim ptr = A4KPointCloud(cPtr)
                    Dim tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width,
                                                   cv.MatType.CV_16SC3, ptr).
                                                   Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
                    tmp.ConvertTo(uiPointCloud, cv.MatType.CV_32FC3, 0.001) ' convert to meters...
                Else
                    Dim tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width,
                                                   cv.MatType.CV_16SC3, A4KPointCloud(cPtr))
                    tmp.ConvertTo(uiPointCloud, cv.MatType.CV_32FC3, 0.001) ' convert to meters...
                End If
            End SyncLock
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        Catch ex As Exception

        End Try
    End Sub
    Public Sub stopCamera()
        If cPtr <> 0 Then A4KClose(cPtr)
        cPtr = 0
    End Sub
End Class





Module A4K_Interface
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function A4KOpen(width As Integer, height As Integer) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function A4KDeviceCount(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function A4KDeviceName(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function A4KWaitFrame(cPtr As IntPtr, w As Integer, h As Integer) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function A4KIntrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function A4KPointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function A4KColor(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function A4KLeftView(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub A4KClose(cPtr As IntPtr)
    End Sub
End Module

