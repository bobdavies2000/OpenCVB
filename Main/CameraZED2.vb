Imports cv = OpenCvSharp
Imports CamZed
Public Class CameraZed2 : Inherits GenericCamera
    Dim zed As CamZed
    Dim ratio As Single
    Public Sub New(workRes As cv.Size, captureRes As cv.Size, deviceName As String)
        ratio = CInt(captureRes.Width / workRes.Width)
        zed = New CamZed(workRes, captureRes, deviceName)
    End Sub
    Public Sub GetNextFrame(workRes As cv.Size)
        zed.GetNextFrame(workRes)

        SyncLock cameraLock
            IMU_Acceleration = zed.IMU_Acceleration
            IMU_AngularVelocity = zed.IMU_AngularVelocity
            Static IMU_StartTime = zed.IMU_TimeStamp
            IMU_TimeStamp = (zed.IMU_TimeStamp - IMU_StartTime) / 4000000 ' crude conversion to milliseconds.

            If calibData.baseline = 0 Then
                calibData.baseline = zed.baseline

                calibData.rgbIntrinsics.fx = zed.rgbIntrinsics.fx / ratio
                calibData.rgbIntrinsics.fy = zed.rgbIntrinsics.fy / ratio
                calibData.rgbIntrinsics.ppx = zed.rgbIntrinsics.ppx / ratio
                calibData.rgbIntrinsics.ppy = zed.rgbIntrinsics.ppy / ratio

                calibData.leftIntrinsics = calibData.rgbIntrinsics

                calibData.rightIntrinsics.fx = zed.rightIntrinsics.fx / ratio
                calibData.rightIntrinsics.fy = zed.rightIntrinsics.fy / ratio
                calibData.rightIntrinsics.ppx = zed.rightIntrinsics.ppx / ratio
                calibData.rightIntrinsics.ppy = zed.rightIntrinsics.ppy / ratio
            End If

            If workRes <> captureRes Then
                uiColor = zed.color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiLeft = zed.leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiRight = zed.rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiPointCloud = zed.pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest).Clone
            Else
                uiColor = zed.color.Clone
                uiLeft = uiColor
                uiRight = zed.rightView.Clone
                uiPointCloud = zed.pointCloud.Clone
            End If
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        zed.stopCamera()
    End Sub
End Class
