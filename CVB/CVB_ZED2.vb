Imports cv = OpenCvSharp
Imports System.Threading

Namespace CVB
    Public Class CVB_ZED2 : Inherits CVB_Camera
        Dim zed As CamZed
        Dim captureThread As Thread = Nothing
        Dim isCapturing As Boolean = False

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

            ' Start background thread to capture frames
            isCapturing = True
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "ZED2_CaptureThread"
            captureThread.Start()
        End Sub

        Private Sub CaptureFrames()
            While isCapturing
                Try
                    GetNextFrame()
                Catch ex As Exception
                    ' Continue capturing even if one frame fails
                    Thread.Sleep(10)
                End Try
            End While
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

        Public Overrides Sub StopCamera()
            isCapturing = False
            If captureThread IsNot Nothing Then
                captureThread.Join(1000) ' Wait up to 1 second for thread to finish
                captureThread = Nothing
            End If
            If zed IsNot Nothing AndAlso camImages IsNot Nothing AndAlso camImages.pointCloud.Width > 0 Then
                zed.StopCamera()
            End If
        End Sub
    End Class
End Namespace

