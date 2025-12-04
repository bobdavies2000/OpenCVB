Imports cv = OpenCvSharp
Imports Intel.RealSense

Namespace CVB
    Public Class CameraRS2 : Inherits GenericCamera
        Dim rs2 As CameraRS2

        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            captureRes = _captureRes
            workRes = _workRes
            ratio = CInt(captureRes.Width / workRes.Width)
            rs2 = New CameraRS2(workRes, captureRes, deviceName)

            calibData.rgbIntrinsics.fx = rs2.calibData.rgbIntrinsics.fx
            CalibData.rgbIntrinsics.fy = rs2.calibData.rgbIntrinsics.fy
            CalibData.rgbIntrinsics.ppx = rs2.calibData.rgbIntrinsics.ppx
            CalibData.rgbIntrinsics.ppy = rs2.calibData.rgbIntrinsics.ppy

            CalibData.leftIntrinsics.fx = rs2.calibData.leftIntrinsics.fx
            CalibData.leftIntrinsics.fy = rs2.calibData.leftIntrinsics.fy
            CalibData.leftIntrinsics.ppx = rs2.calibData.leftIntrinsics.ppx
            CalibData.leftIntrinsics.ppy = rs2.calibData.leftIntrinsics.ppy

            CalibData.rightIntrinsics.fx = rs2.calibData.rightIntrinsics.fx
            CalibData.rightIntrinsics.fy = rs2.calibData.rightIntrinsics.fy
            CalibData.rightIntrinsics.ppx = rs2.calibData.rightIntrinsics.ppx
            CalibData.rightIntrinsics.ppy = rs2.calibData.rightIntrinsics.ppy

            CalibData.baseline = rs2.calibData.baseline
        End Sub

        Public Sub GetNextFrame()
            rs2.GetNextFrame()

            IMU_Acceleration = rs2.IMU_Acceleration
            IMU_AngularVelocity = rs2.IMU_AngularVelocity
            Static IMU_StartTime = rs2.IMU_TimeStamp
            IMU_TimeStamp = (rs2.IMU_TimeStamp - IMU_StartTime) / 4000000 ' crude conversion to milliseconds.

            ' Main CameraRS2 already resizes images to workRes, so just copy them directly
            camImages.color = rs2.camImages.color
            camImages.left = rs2.camImages.left
            camImages.right = rs2.camImages.right
            camImages.pointCloud = rs2.camImages.pointCloud

            If cameraFrameCount Mod 10 = 0 Then GC.Collect()

            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End Sub

        Public Sub StopCamera()
            If rs2 IsNot Nothing AndAlso camImages IsNot Nothing AndAlso camImages.pointCloud.Width > 0 Then
                rs2.stopCamera()
            End If
        End Sub
    End Class
End Namespace

