Imports cv = OpenCvSharp
Imports CamZed
Public Class CameraZED2 : Inherits GenericCamera
    Dim zed As CamZed
    Public Sub New(workRes As cv.Size, captureRes As cv.Size, deviceName As String)
        zed = New CamZed(workRes, captureRes, deviceName)
    End Sub
    Public Sub GetNextFrame(workRes As cv.Size)
        zed.GetNextFrame(workRes)

        SyncLock cameraLock
            IMU_Acceleration = zed.IMU_Acceleration
            IMU_AngularVelocity = zed.IMU_AngularVelocity
            Static IMU_StartTime = zed.IMU_TimeStamp
            IMU_TimeStamp = (zed.IMU_TimeStamp - IMU_StartTime) / 4000000 ' crude conversion to milliseconds.
            If workRes <> captureRes Then
                uiColor = zed.color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiLeft = zed.leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiRight = zed.rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiPointCloud = zed.pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest).Clone
            Else
                uiColor = zed.color.Clone
                uiLeft = zed.leftView.Clone
                uiRight = zed.rightView.Clone
                uiPointCloud = zed.pointCloud.Clone
            End If
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        zed.StopCamera()
    End Sub
End Class
