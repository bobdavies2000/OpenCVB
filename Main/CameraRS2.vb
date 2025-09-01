Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports Intel.RealSense
Imports System.Text
Public Class CameraRS2 : Inherits GenericCamera
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
    End Sub
    Public Sub GetNextFrame()
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

            camImages.color = color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            camImages.left = leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest) * 2 ' improve brightness
            camImages.right = rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest) * 2 ' improve brightness
            camImages.pointCloud = pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)

            GC.Collect() ' do you think this is unnecessary?  Remove it and check...
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End Using
    End Sub
    Public Sub stopCamera()
        pipe.Stop()
    End Sub
End Class






Module RS2_Module_CPP
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub RS2WaitForFrame(cPtr As IntPtr, w As Integer, h As Integer)
    End Sub
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2RightRaw(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2Color(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2LeftRaw(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2intrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2PointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2Gyro(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2IMUTimeStamp(cPtr As IntPtr) As Double
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2Accel(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub RS2Stop(cPtr As IntPtr)
    End Sub
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2Open(
                                                   <MarshalAs(UnmanagedType.LPStr)> ByVal deviceName As StringBuilder,
                                                   width As Integer, height As Integer) As IntPtr
    End Function
End Module

Structure RS2IMUdata
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
End Structure