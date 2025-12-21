Imports cv = OpenCvSharp
Imports System.Threading

Namespace MainUI
    Public Class Camera_ZED2 : Inherits GenericCamera
        Dim zed As CamZed
        Dim captureThread As Thread = Nothing

        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            captureRes = _captureRes
            workRes = _workRes
            ratio = captureRes.Width \ workRes.Width
            zed = New CamZed(workRes, captureRes, deviceName)

            calibData.rgbIntrinsics.fx = zed.rgbIntrinsics.fx / ratio
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
        Private Sub CaptureFrames()
            While isCapturing
                GetNextFrame()
            End While
        End Sub
        Public Sub GetNextFrame()
            zed.GetNextFrame()

            IMU_Acceleration = zed.IMU_Acceleration
            IMU_AngularVelocity = zed.IMU_AngularVelocity
            Static IMU_StartTime = zed.IMU_TimeStamp
            IMU_TimeStamp = (zed.IMU_TimeStamp - IMU_StartTime) / 4000000 ' crude conversion to milliseconds.

            If workRes <> captureRes Then
                color = zed.color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest).Clone
                pointCloud = zed.pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest).Clone
                leftView = zed.leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest).Clone
                rightView = zed.rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest).Clone
            Else
                color = zed.color.Clone
                pointCloud = zed.pointCloud.Clone
                leftView = zed.leftView.Clone
                rightView = zed.rightView.Clone
            End If
            GC.Collect()
        End Sub
        Public Overrides Sub StopCamera()
            If zed IsNot Nothing Then zed.StopCamera()
        End Sub
    End Class
End Namespace

