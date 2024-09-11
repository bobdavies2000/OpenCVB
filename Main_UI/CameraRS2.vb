Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Imports Intel.RealSense
Imports System.Text
Public Class CameraRS2 : Inherits Camera
    Dim pipe As New Pipeline()
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, devName As String, Optional fps As Integer = 30)
        Dim serialNumber As String = ""
        Dim ctx As New Context()
        Dim searchName As String = devName

        For Each dev In ctx.QueryDevices()
            Dim deviceName As String = dev.Info.Item(0)
            If String.Compare(deviceName, searchName) = 0 Then
                serialNumber = dev.Info.Item(1)
            End If
        Next

        Dim cfg As New Config()
        cfg.EnableDevice(serialNumber)

        captureRes = _captureRes
        cfg.EnableStream(Stream.Color, captureRes.Width, captureRes.Height, Format.Bgr8, fps)
        cfg.EnableStream(Stream.Infrared, 1, captureRes.Width, captureRes.Height, Format.Y8, fps)
        cfg.EnableStream(Stream.Infrared, 2, captureRes.Width, captureRes.Height, Format.Y8, fps)
        cfg.EnableStream(Stream.Depth, captureRes.Width, captureRes.Height, Format.Z16, fps)
        cfg.EnableStream(Stream.Accel, Format.MotionXyz32f, 63)
        cfg.EnableStream(Stream.Gyro, Format.MotionXyz32f, 200)

        Dim profiles = pipe.Start(cfg)
        Dim StreamColor = profiles.GetStream(Stream.Color)
        Dim myIntrinsics = StreamColor.As(Of VideoStreamProfile)().GetIntrinsics()
        cameraInfo.ppx = myIntrinsics.ppx
        cameraInfo.ppy = myIntrinsics.ppy
        cameraInfo.fx = myIntrinsics.fx
        cameraInfo.fy = myIntrinsics.fy
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        Dim alignToColor = New Align(Stream.Color)
        Dim pointcloud = New PointCloud()
        Dim cols = captureRes.Width, rows = captureRes.Height

        Using frames As FrameSet = pipe.WaitForFrames(5000)
            For Each frame As Intel.RealSense.Frame In frames
                If frame.Profile.Stream = Stream.Infrared AndAlso frame.Profile.Index = 1 Then
                    mbuf(mbIndex).leftView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, frame.Data)
                End If
                If frame.Profile.Stream = Stream.Infrared AndAlso frame.Profile.Index = 2 Then
                    mbuf(mbIndex).rightView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, frame.Data)
                End If
                If frame.Profile.Stream = Stream.Accel Then
                    IMU_Acceleration = Marshal.PtrToStructure(Of cvb.Point3f)(frame.Data)
                End If
                If frame.Profile.Stream = Stream.Gyro Then
                    IMU_AngularVelocity = Marshal.PtrToStructure(Of cvb.Point3f)(frame.Data)
                    Dim mFrame = frame.As(Of MotionFrame)
                    IMU_FrameTime = mFrame.Timestamp
                End If
            Next

            Dim alignedFrames As FrameSet = alignToColor.Process(frames).As(Of FrameSet)()

            mbuf(mbIndex).color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, alignedFrames.ColorFrame.Data)

            Dim pcFrame = pointcloud.Process(alignedFrames.DepthFrame)
            mbuf(mbIndex).pointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC3, pcFrame.Data)

            If captureRes.Width <> WorkingRes.Width Or captureRes.Height <> WorkingRes.Height Then
                mbuf(mbIndex).color = mbuf(mbIndex).color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                mbuf(mbIndex).leftView = mbuf(mbIndex).leftView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                mbuf(mbIndex).rightView = mbuf(mbIndex).rightView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                mbuf(mbIndex).pointCloud = mbuf(mbIndex).pointCloud.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            End If

            GC.Collect() ' do you think this is unnecessary?  Remove it and check...
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End Using
    End Sub
    Public Sub stopCamera()
        pipe.Stop()
    End Sub
End Class