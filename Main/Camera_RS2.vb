Imports System.Runtime.InteropServices
Imports System.Threading
Imports Intel.RealSense
Imports cv = OpenCvSharp

Namespace MainForm
    Public Class Camera_RS2 : Inherits GenericCamera
        Dim captureThread As Thread = Nothing
        Dim isCapturing As Boolean = False
        Dim pipe As New Pipeline()
        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, devName As String, Optional fps As Integer = 30)
            captureRes = _captureRes
            workRes = _workRes
            Dim serialNumber As String = ""
            Dim ctx As New Context()
            Dim searchName As String = If(devName.EndsWith("455"), "D455", "D435i")
            For Each dev In ctx.QueryDevices()
                If dev.Info.Item(0).Contains(searchName) Then serialNumber = dev.Info.Item(1)
            Next

            Dim cfg As New Config()
            cfg.EnableDevice(serialNumber)

            cfg.EnableStream(Stream.Color, captureRes.Width, captureRes.Height, Format.Bgr8, fps)
            cfg.EnableStream(Stream.Infrared, 1, captureRes.Width, captureRes.Height, Format.Y8, fps)
            cfg.EnableStream(Stream.Infrared, 2, captureRes.Width, captureRes.Height, Format.Y8, fps)
            cfg.EnableStream(Stream.Depth, captureRes.Width, captureRes.Height, Format.Z16, fps)
            cfg.EnableStream(Stream.Accel, Format.MotionXyz32f, 63)
            cfg.EnableStream(Stream.Gyro, Format.MotionXyz32f, 200)

            Dim profiles = pipe.Start(cfg)
            Dim streamLeft = profiles.GetStream(Stream.Infrared, 1)
            Dim streamRight = profiles.GetStream(Stream.Infrared, 2)
            Dim StreamColor = profiles.GetStream(Stream.Color)
            Dim rgb As Intrinsics = StreamColor.As(Of VideoStreamProfile)().GetIntrinsics()
            Dim rgbExtrinsics As Extrinsics = StreamColor.As(Of VideoStreamProfile)().GetExtrinsicsTo(streamLeft)

            Dim ratio = CInt(captureRes.Width / workRes.Width)
            calibData.rgbIntrinsics.ppx = rgb.ppx / ratio
            calibData.rgbIntrinsics.ppy = rgb.ppy / ratio
            calibData.rgbIntrinsics.fx = rgb.fx / ratio
            calibData.rgbIntrinsics.fy = rgb.fy / ratio

            Dim leftIntrinsics As Intrinsics = streamLeft.As(Of VideoStreamProfile)().GetIntrinsics()
            Dim leftExtrinsics As Extrinsics = streamLeft.As(Of VideoStreamProfile)().GetExtrinsicsTo(streamRight)
            calibData.leftIntrinsics.ppx = rgb.ppx / ratio
            calibData.leftIntrinsics.ppy = rgb.ppy / ratio
            calibData.leftIntrinsics.fx = rgb.fx / ratio
            calibData.leftIntrinsics.fy = rgb.fy / ratio

            ReDim calibData.LtoR_translation(3 - 1)
            ReDim calibData.LtoR_rotation(9 - 1)

            ReDim calibData.ColorToLeft_translation(3 - 1)
            ReDim calibData.ColorToLeft_rotation(9 - 1)

            For i = 0 To 3 - 1
                calibData.LtoR_translation(i) = leftExtrinsics.translation(i)
            Next
            For i = 0 To 9 - 1
                calibData.LtoR_rotation(i) = leftExtrinsics.rotation(i)
            Next

            For i = 0 To 3 - 1
                calibData.ColorToLeft_translation(i) = rgbExtrinsics.translation(i)
            Next
            For i = 0 To 9 - 1
                calibData.ColorToLeft_rotation(i) = rgbExtrinsics.rotation(i)
            Next

            calibData.baseline = System.Math.Sqrt(System.Math.Pow(calibData.ColorToLeft_translation(0), 2) +
                                              System.Math.Pow(calibData.ColorToLeft_translation(1), 2) +
                                              System.Math.Pow(calibData.ColorToLeft_translation(2), 2))
            ' Start background thread to capture frames
            isCapturing = True
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "RS2_CaptureThread"
            camImages = New CameraImages(workRes)
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
            ' Check if pipe is still valid (might be cleared during stop)
            If pipe Is Nothing Then Return

            Dim alignToColor = New Align(Stream.Color)
            Dim ptcloud = New PointCloud()
            Dim cols = captureRes.Width, rows = captureRes.Height
            Static color As cv.Mat, leftView As cv.Mat, rightView As cv.Mat, pointCloud As cv.Mat

            Using frames As FrameSet = pipe.WaitForFrames(5000)
                For Each frame As Intel.RealSense.Frame In frames
                    If frame.Profile.Stream = Stream.Infrared AndAlso frame.Profile.Index = 1 Then
                        leftView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, frame.Data)
                    End If
                    If frame.Profile.Stream = Stream.Infrared AndAlso frame.Profile.Index = 2 Then
                        rightView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, frame.Data)
                    End If
                    If frame.Profile.Stream = Stream.Accel Then
                        IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(frame.Data)
                    End If
                    If frame.Profile.Stream = Stream.Gyro Then
                        IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(frame.Data)
                        Dim mFrame = frame.As(Of MotionFrame)
                        Static initialTime As Int64 = mFrame.Timestamp
                        IMU_FrameTime = mFrame.Timestamp - initialTime
                    End If
                Next

                Dim alignedFrames As FrameSet = alignToColor.Process(frames).As(Of FrameSet)()

                color = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC3, alignedFrames.ColorFrame.Data)

                Dim pcFrame = ptcloud.Process(alignedFrames.DepthFrame)
                pointCloud = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_32FC3, pcFrame.Data)

                If color Is Nothing Then color = New cv.Mat(workRes, cv.MatType.CV_8UC3)
                If leftView Is Nothing Then leftView = New cv.Mat(workRes, cv.MatType.CV_8UC3)
                If rightView Is Nothing Then rightView = New cv.Mat(workRes, cv.MatType.CV_8UC3)
                If pointCloud Is Nothing Then pointCloud = New cv.Mat(workRes, cv.MatType.CV_32FC3)

                camImages.images(0) = color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                camImages.images(1) = pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                camImages.images(2) = leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest) * 2 ' improve brightness
                camImages.images(3) = rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest) * 2 ' improve brightness

                GC.Collect() ' do you think this is unnecessary?  Remove it and check...
                MyBase.GetNextFrameCounts(IMU_FrameTime)
            End Using
        End Sub
        Public Overrides Sub StopCamera()
            isCapturing = False
            If captureThread IsNot Nothing Then
                captureThread.Join(1000) ' Wait up to 1 second for thread to finish
                captureThread = Nothing
            End If

            ' Stop the pipeline asynchronously so it doesn't block the UI
            If pipe IsNot Nothing Then
                Dim pipeToStop = pipe
                pipe = Nothing ' Clear reference so GetNextFrame won't try to use it

                ' Run pipe.Stop() on a background thread so it doesn't block
                ThreadPool.QueueUserWorkItem(Sub(state)
                                                 Try
                                                     pipeToStop.Stop()
                                                     pipeToStop.Dispose()
                                                 Catch ex As Exception
                                                     Debug.WriteLine("Error stopping pipeline: " + ex.Message)
                                                 End Try
                                             End Sub)
            End If
        End Sub
    End Class
End Namespace

