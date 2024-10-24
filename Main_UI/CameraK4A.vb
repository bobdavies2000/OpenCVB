﻿Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Imports Microsoft.Kinect.KinectSensor
Public Class CameraK4A : Inherits GenericCamera
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes

        cPtr = A4KOpen(captureRes.Width, captureRes.Height)
        cameraName = deviceName
        If cPtr <> 0 Then
            deviceCount = A4KDeviceCount(cPtr)
            Dim strPtr = A4KDeviceName(cPtr) ' The width and height of the image are set in the constructor.
            serialNumber = Marshal.PtrToStringAnsi(strPtr)

            Dim ptr = A4KIntrinsics(cPtr)
            Dim intrinsicsLeftOutput = Marshal.PtrToStructure(Of intrinsicsData)(ptr)
            cameraInfo.ppx = intrinsicsLeftOutput.cx
            cameraInfo.ppy = intrinsicsLeftOutput.cy
            cameraInfo.fx = intrinsicsLeftOutput.fx
            cameraInfo.fy = intrinsicsLeftOutput.fy
        End If
    End Sub
    Structure imuData
        Dim placeholderForTemperature As Single
        Dim imuAccel As cvb.Point3f
        Dim accelTimeStamp As Long
        Dim imu_Gyro As cvb.Point3f
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
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
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
                uiColor = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width,
                                                cvb.MatType.CV_8UC3, A4KColor(cPtr)).Clone

                ' so depth data fits into 0-255 (approximately)
                uiLeft = (cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width,
                                                cvb.MatType.CV_16U, A4KLeftView(cPtr)) * 0.06).ToMat.
                                                ConvertScaleAbs().
                                                CvtColor(cvb.ColorConversionCodes.GRAY2BGR).Clone
                uiRight = uiLeft.Clone
                If captureRes <> WorkingRes Then
                    Dim ptr = A4KPointCloud(cPtr)
                    Dim tmp = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width,
                                                    cvb.MatType.CV_16SC3, ptr).
                                                    Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                    tmp.ConvertTo(uiPointCloud, cvb.MatType.CV_32FC3, 0.001) ' convert to meters...
                Else
                    Dim tmp = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width,
                                                    cvb.MatType.CV_16SC3, A4KPointCloud(cPtr))
                    tmp.ConvertTo(uiPointCloud, cvb.MatType.CV_32FC3, 0.001) ' convert to meters...
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
