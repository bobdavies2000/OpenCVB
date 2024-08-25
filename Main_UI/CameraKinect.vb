Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module K4A_Interface
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function K4AOpen(width As Integer, height As Integer) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function K4ADeviceCount(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function K4ADeviceName(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function K4AWaitFrame(cPtr As IntPtr, w As Integer, h As Integer) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function K4AIntrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function K4APointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function K4AColor(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function K4ALeftView(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_K4A.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub K4AClose(cPtr As IntPtr)
    End Sub
End Module
Public Class CameraKinect : Inherits Camera
    Public Sub New(WorkingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        MyBase.setupMats(WorkingRes)
        cPtr = K4AOpen(captureRes.Width, captureRes.Height)
        cameraName = deviceName
        If cPtr <> 0 Then
            deviceCount = K4ADeviceCount(cPtr)
            Dim strPtr = K4ADeviceName(cPtr) ' The width and height of the image are set in the constructor.
            serialNumber = Marshal.PtrToStringAnsi(strPtr)

            Dim ptr = K4AIntrinsics(cPtr)
            Dim intrinsicsLeftOutput = Marshal.PtrToStructure(Of intrinsicsData)(ptr)
            cameraInfo.ppx = intrinsicsLeftOutput.cx
            cameraInfo.ppy = intrinsicsLeftOutput.cy
            cameraInfo.fx = intrinsicsLeftOutput.fx
            cameraInfo.fy = intrinsicsLeftOutput.fy
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
            imuFrame = K4AWaitFrame(cPtr, WorkingRes.Width, WorkingRes.Height)
            If imuFrame = 0 Then
                Console.WriteLine("KinectWaitFrame has returned without any image.")
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
                mbuf(mbIndex).color = cv.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cv.MatType.CV_8UC3, K4AColor(cPtr)).Clone

                ' so depth data fits into 0-255 (approximately)
                mbuf(mbIndex).leftView = (cv.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cv.MatType.CV_16U,
                                          K4ALeftView(cPtr)) * 0.06).ToMat.ConvertScaleAbs().CvtColor(cv.ColorConversionCodes.GRAY2BGR).Clone
                mbuf(mbIndex).rightView = mbuf(mbIndex).leftView
                If captureRes <> WorkingRes Then
                    Dim tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_16SC3,
                                     K4APointCloud(cPtr)).Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
                    tmp.ConvertTo(mbuf(mbIndex).pointCloud, cv.MatType.CV_32FC3, 0.001) ' convert to meters...
                Else
                    Dim tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_16SC3, K4APointCloud(cPtr))
                    tmp.ConvertTo(mbuf(mbIndex).pointCloud, cv.MatType.CV_32FC3, 0.001) ' convert to meters...
                End If
            End SyncLock
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        Catch ex As Exception

        End Try
    End Sub
    Public Sub stopCamera()
        If cPtr <> 0 Then K4AClose(cPtr)
        cPtr = 0
    End Sub
End Class
