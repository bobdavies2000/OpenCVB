Imports cv = OpenCvSharp
Imports System.Threading

Namespace MainApp
    Public Class Camera_ZED2 : Inherits GenericCamera
        Dim zed As CamZed
        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            captureRes = _captureRes
            workRes = _workRes
            zed = New CamZed(workRes, captureRes, deviceName)

            Dim ratio = captureRes.Width \ workRes.Width
            calibData.leftIntrinsics.fx = zed.leftIntrinsics.fx / ratio
            calibData.leftIntrinsics.fy = zed.leftIntrinsics.fy / ratio
            calibData.leftIntrinsics.ppx = zed.leftIntrinsics.ppx / ratio
            calibData.leftIntrinsics.ppy = zed.leftIntrinsics.ppy / ratio

            calibData.baseline = zed.baseline

            MyBase.prepImages()

            ' Start background thread to capture frames
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "CaptureThread_ZED2"
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
            Static IMU_StartTime As Double = zed.IMU_TimeStamp
            IMU_TimeStamp = (zed.IMU_TimeStamp - IMU_StartTime) / 4000000 ' crude conversion to milliseconds.

            SyncLock cameraMutex
                color = zed.color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                pointCloud = zed.pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                leftView = zed.leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                rightView = zed.rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            End SyncLock

            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End Sub
        Public Overrides Sub StopCamera()
            If zed IsNot Nothing Then zed.StopCamera()
        End Sub
    End Class
End Namespace

