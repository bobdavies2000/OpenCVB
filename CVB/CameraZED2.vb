Imports cv = OpenCvSharp

Namespace CVB
    Public Class CameraZED2 : Inherits GenericCamera
        Dim zed As CamZed

        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            captureRes = _captureRes
            workRes = _workRes
            ratio = CInt(captureRes.Width / workRes.Width)
            zed = New CamZed(workRes, captureRes, deviceName)

            CalibData.rgbIntrinsics.fx = zed.rgbIntrinsics.fx / ratio
            CalibData.rgbIntrinsics.fy = zed.rgbIntrinsics.fy / ratio
            CalibData.rgbIntrinsics.ppx = zed.rgbIntrinsics.ppx / ratio
            CalibData.rgbIntrinsics.ppy = zed.rgbIntrinsics.ppy / ratio

            CalibData.leftIntrinsics = CalibData.rgbIntrinsics

            CalibData.rightIntrinsics.fx = zed.rightIntrinsics.fx / ratio
            CalibData.rightIntrinsics.fy = zed.rightIntrinsics.fy / ratio
            CalibData.rightIntrinsics.ppx = zed.rightIntrinsics.ppx / ratio
            CalibData.rightIntrinsics.ppy = zed.rightIntrinsics.ppy / ratio

            CalibData.baseline = zed.baseline
        End Sub

        Public Sub GetNextFrame()
            zed.GetNextFrame()

            IMU_Acceleration = zed.IMU_Acceleration
            IMU_AngularVelocity = zed.IMU_AngularVelocity
            Static IMU_StartTime = zed.IMU_TimeStamp
            IMU_TimeStamp = (zed.IMU_TimeStamp - IMU_StartTime) / 4000000 ' crude conversion to milliseconds.

            If workRes <> captureRes Then
                camImages.color = zed.color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                camImages.left = zed.leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                camImages.right = zed.rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                camImages.pointCloud = zed.pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            Else
                camImages.color = zed.color
                camImages.left = zed.leftView
                camImages.right = zed.rightView
                camImages.pointCloud = zed.pointCloud
            End If

            If cameraFrameCount Mod 10 = 0 Then GC.Collect()

            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End Sub

        Public Sub StopCamera()
            If zed IsNot Nothing AndAlso camImages IsNot Nothing AndAlso camImages.pointCloud.Width > 0 Then
                zed.StopCamera()
            End If
        End Sub
    End Class
End Namespace

