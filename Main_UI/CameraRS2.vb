Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports Intel.RealSense
Imports System.Text
Public Class CameraRS2 : Inherits GenericCamera
    Dim pipe As New Pipeline()
    Private Function copyIntrinsics(input As Intrinsics, ratio As Single) As VB_Classes.VBtask.intrinsicData
        Dim output As New VB_Classes.VBtask.intrinsicData
        output.ppx = input.ppx / ratio
        output.ppy = input.ppy / ratio
        output.fx = input.fx / ratio
        output.fy = input.fy / ratio
        Return output
    End Function
    Public Sub New(WorkingRes As cv.Size, _captureRes As cv.Size, devName As String, Optional fps As Integer = 30)
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
        Dim streamLeft = profiles.GetStream(Stream.Infrared, 1)
        Dim StreamColor = profiles.GetStream(Stream.Color)
        Dim rgbIntrinsics = StreamColor.As(Of VideoStreamProfile)().GetIntrinsics()
        Dim rgbExtrinsics = StreamColor.As(Of VideoStreamProfile)().GetExtrinsicsTo(streamLeft)

        Dim ratio = CInt(captureRes.Width / WorkingRes.Width)
        calibData.rgbIntrinsics = copyIntrinsics(rgbIntrinsics, ratio)

        Dim leftIntrinsics = streamLeft.As(Of VideoStreamProfile)().GetIntrinsics()
        Dim leftExtrinsics = streamLeft.As(Of VideoStreamProfile)().GetExtrinsicsTo(StreamColor)
        calibData.leftIntrinsics = copyIntrinsics(leftIntrinsics, ratio)

        ReDim calibData.translation(3 - 1)
        ReDim calibData.rotation(9 - 1)

        For i = 0 To 3 - 1
            calibData.translation(i) = leftExtrinsics.translation(i)
        Next
        For i = 0 To 9 - 1
            calibData.rotation(i) = leftExtrinsics.rotation(i)
        Next

        ' Calculate the baseline (distance between the left and RGB cameras) using the translation vector
        'calibData.baseline = System.Math.Sqrt(System.Math.Pow(calibData.translation(0), 2) +
        '                                      System.Math.Pow(calibData.translation(1), 2) +
        '                                      System.Math.Pow(calibData.translation(2), 2))
        calibData.baseline = 0.65
    End Sub
    Public Sub GetNextFrame(WorkingRes As cv.Size)
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

            If color Is Nothing Then color = New cv.Mat(WorkingRes, cv.MatType.CV_8UC3)
            If leftView Is Nothing Then leftView = New cv.Mat(WorkingRes, cv.MatType.CV_8UC3)
            If rightView Is Nothing Then rightView = New cv.Mat(WorkingRes, cv.MatType.CV_8UC3)
            If pointCloud Is Nothing Then pointCloud = New cv.Mat(WorkingRes, cv.MatType.CV_32FC3)

            SyncLock cameraLock
                uiColor = color.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiLeft = leftView.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest) * 2 ' improve brightness
                uiRight = rightView.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest) * 2 ' improve brightness
                uiPointCloud = pointCloud.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End SyncLock

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