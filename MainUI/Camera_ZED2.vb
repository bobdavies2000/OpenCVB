Imports cv = OpenCvSharp
Imports System.Threading

Namespace MainUI
    Public Class Camera_ZED2 : Inherits GenericCamera
        Dim zed As CamZed
        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            captureRes = _captureRes
            workRes = _workRes
            ratio = captureRes.Width \ workRes.Width
            zed = New CamZed(workRes, captureRes, deviceName)

            calibData.rgbIntrinsics.fx = zed.rgbIntrinsics.fx / ratio
            calibData.rgbIntrinsics.fy = zed.rgbIntrinsics.fy / ratio
            calibData.rgbIntrinsics.ppx = zed.rgbIntrinsics.ppx / ratio
            calibData.rgbIntrinsics.ppy = zed.rgbIntrinsics.ppy / ratio

            calibData.leftIntrinsics = calibData.rgbIntrinsics

            calibData.rightIntrinsics.fx = zed.rightIntrinsics.fx / ratio
            calibData.rightIntrinsics.fy = zed.rightIntrinsics.fy / ratio
            calibData.rightIntrinsics.ppx = zed.rightIntrinsics.ppx / ratio
            calibData.rightIntrinsics.ppy = zed.rightIntrinsics.ppy / ratio

            calibData.baseline = zed.baseline

            MyBase.prepImages()

            ' Start background thread to capture frames
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "ZED2_CaptureThread"
            captureThread.Start()
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

            color = zed.color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            pointCloud = zed.pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            leftView = zed.leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            rightView = zed.rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)

            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End Sub
        Public Overrides Sub StopCamera()
            If captureThread IsNot Nothing Then
                captureThread.Join(1000) ' Wait up to 1 second for thread to finish
                captureThread = Nothing
            End If
            If zed IsNot Nothing Then zed.StopCamera()
        End Sub
    End Class
End Namespace

