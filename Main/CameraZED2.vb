Imports cv = OpenCvSharp
Imports CamZed
Public Class CameraZed2 : Inherits GenericCamera
    Dim zed As CamZed
    Dim ratio As Single
    Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        workRes = _workRes
        camImages = New CameraImages.images(workRes)
        cameraFrameCount = 0
        ratio = CInt(captureRes.Width / workRes.Width)
        zed = New CamZed(workRes, captureRes, deviceName)
    End Sub
    Public Sub GetNextFrame()
        zed.GetNextFrame()

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
            camImages.color = zed.color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            camImages.left = zed.leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            camImages.right = zed.rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            camImages.pointCloud = zed.pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
        Else
            camImages.color = zed.color
            camImages.left = camImages.color
            camImages.right = zed.rightView
            camImages.pointCloud = zed.pointCloud
        End If

        If cameraFrameCount < 100 Then GC.Collect()

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        If zed.pointCloud.Width > 0 Then zed.StopCamera()
    End Sub
End Class
