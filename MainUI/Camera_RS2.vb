Imports System.Runtime.InteropServices
Imports System.Threading
Imports Intel.RealSense
Imports cv = OpenCvSharp

Namespace MainApp
    Public Class Camera_RS2 : Inherits GenericCamera
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
            Dim StreamColor = profiles.GetStream(Stream.Color)
            Dim streamLeft = profiles.GetStream(Stream.Infrared, 1)
            Dim streamRight = profiles.GetStream(Stream.Infrared, 2)

            Dim leftIntrin As Intrinsics = streamLeft.As(Of VideoStreamProfile)().GetIntrinsics()
            Dim leftExtrinsics As Extrinsics = streamLeft.As(Of VideoStreamProfile)().GetExtrinsicsTo(streamRight)

            Dim ratio As Single = captureRes.Width \ workRes.Width
            calibData.leftIntrinsics.fx = leftIntrin.fx / ratio
            calibData.leftIntrinsics.fy = leftIntrin.fy / ratio
            calibData.leftIntrinsics.ppx = leftIntrin.ppx / ratio
            calibData.leftIntrinsics.ppy = leftIntrin.ppy / ratio

            ReDim calibData.LtoR_translation(3 - 1)
            For i = 0 To 3 - 1
                calibData.LtoR_translation(i) = leftExtrinsics.translation(i)
            Next

            calibData.baseline = System.Math.Sqrt(System.Math.Pow(calibData.LtoR_translation(0), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(1), 2) +
                                                  System.Math.Pow(calibData.LtoR_translation(2), 2))

            MyBase.prepImages()

            ' Start background thread to capture frames
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "CaptureThread_RS2"
            captureThread.Start()
        End Sub
        Private Sub CaptureFrames()
            While isCapturing
                GetNextFrame()
            End While
        End Sub
        Public Sub GetNextFrame()
            ' Check if pipe is still valid (might be cleared during stop)
            If pipe Is Nothing Then Return

            Static ptcloud As PointCloud = New PointCloud()
            Dim cols = captureRes.Width, rows = captureRes.Height

            Dim depth16u As cv.Mat = Nothing
            Using frames As FrameSet = pipe.WaitForFrames(5000)
                SyncLock cameraMutex
                    For Each frame As Intel.RealSense.Frame In frames
                        If frame.Profile.Stream = Stream.Color AndAlso frame.Profile.Index = 0 Then
                            color = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC3, frame.Data)
                            color = color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                        End If
                        If frame.Profile.Stream = Stream.Infrared AndAlso frame.Profile.Index = 1 Then
                            leftView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, frame.Data)
                            leftView = leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                        End If
                        If frame.Profile.Stream = Stream.Infrared AndAlso frame.Profile.Index = 2 Then
                            rightView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, frame.Data)
                            rightView = rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                        End If
                        If frame.Profile.Stream = Stream.Depth Then
                            depth16u = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_16UC1, frame.Data)
                            depth16u = depth16u.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                            pointCloud = ComputePointCloud(depth16u, calibData.leftIntrinsics)
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
                End SyncLock

                MyBase.GetNextFrameCounts(IMU_FrameTime)
            End Using
        End Sub
        Public Overrides Sub StopCamera()
            ' Stop the pipeline asynchronously so it doesn't block the UI
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
        End Sub
    End Class
End Namespace

