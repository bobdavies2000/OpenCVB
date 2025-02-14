Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports Intel.RealSense
Imports System.Text
Public Class CameraRS2 : Inherits GenericCamera
    Dim pipe As New Pipeline()
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
        Dim StreamColor = profiles.GetStream(Stream.Color)
        Dim myIntrinsics = StreamColor.As(Of VideoStreamProfile)().GetIntrinsics()
        Dim ratio = CInt(captureRes.Width / WorkingRes.Width)
        calibData.ppx = myIntrinsics.ppx / ratio
        calibData.ppy = myIntrinsics.ppy / ratio
        calibData.fx = myIntrinsics.fx / ratio
        calibData.fy = myIntrinsics.fy / ratio

        Dim streamLeft = profiles.GetStream(Stream.Infrared, 1)
        Dim streamRight = profiles.GetStream(Stream.Infrared, 2)

        Dim leftIntrinsics = streamLeft.As(Of VideoStreamProfile)().GetIntrinsics()
        Dim leftExtrinsics = streamLeft.As(Of VideoStreamProfile)().GetExtrinsicsTo(StreamColor)

        Dim rightIntrinsics = streamRight.As(Of VideoStreamProfile)().GetIntrinsics()
        Dim rightExtrinsics = streamRight.As(Of VideoStreamProfile)().GetExtrinsicsTo(StreamColor)

        ' Calculate the baseline (distance between the left and RGB cameras) using the translation vector
        Dim baselineLeftToRGB As Single = System.Math.Sqrt(System.Math.Pow(leftExtrinsics.translation(0), 2) +
                                                           System.Math.Pow(leftExtrinsics.translation(1), 2) +
                                                           System.Math.Pow(leftExtrinsics.translation(2), 2))

        ' Calculate the baseline (distance between the right and RGB cameras) using the translation vector
        Dim baselineRightToRGB As Single = System.Math.Sqrt(System.Math.Pow(rightExtrinsics.translation(0), 2) +
                                                            System.Math.Pow(rightExtrinsics.translation(1), 2) +
                                                            System.Math.Pow(rightExtrinsics.translation(2), 2))
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
                uiLeft = leftView.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiRight = rightView.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
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
Public Class CameraRS2_CPP : Inherits GenericCamera
    Public deviceNum As Integer
    Public deviceName As String
    Public cPtrOpen As IntPtr
    Public Sub New(WorkingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes

        Dim devName As StringBuilder = New StringBuilder(deviceName)
        cPtr = RS2Open(devName, captureRes.Width, captureRes.Height)

        Dim intrin = RS2intrinsics(cPtr)
        Dim intrinInfo(4 - 1) As Single
        Marshal.Copy(intrin, intrinInfo, 0, intrinInfo.Length)

        calibData.baseline = 0.052 ' where can I get this for this camera?
        calibData.ppx = intrinInfo(0)
        calibData.ppy = intrinInfo(1)
        calibData.fx = intrinInfo(2)
        calibData.fy = intrinInfo(3)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cv.Size)
        If cPtr = 0 Then Exit Sub

        ' if OpenCVB fails here, just unplug and plug in the RealSense camera.
        RS2WaitForFrame(cPtr, WorkingRes.Width, WorkingRes.Height)
        Dim accelFrame = RS2Accel(cPtr)
        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

        Dim gyroFrame = RS2Gyro(cPtr)
        If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

        Static imuStartTime = RS2IMUTimeStamp(cPtr)
        IMU_TimeStamp = RS2IMUTimeStamp(cPtr) - imuStartTime

        SyncLock cameraLock
            Dim w = WorkingRes.Width, h = WorkingRes.Height
            uiColor = cv.Mat.FromPixelData(h, w, cv.MatType.CV_8UC3, RS2Color(cPtr)).Clone
            Dim tmp As cv.Mat = cv.Mat.FromPixelData(h, w, cv.MatType.CV_8U, RS2LeftRaw(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            uiLeft = tmp * 4 - 35 ' improved brightness specific to RealSense
            tmp = cv.Mat.FromPixelData(h, w, cv.MatType.CV_8U, RS2RightRaw(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            uiRight = tmp * 4 - 35 ' improved brightness specific to RealSense
            Dim pc = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, RS2PointCloud(cPtr))
            uiPointCloud = pc.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
        End SyncLock
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        Application.DoEvents()
        Try
            RS2Stop(cPtr)
        Catch ex As Exception
        End Try
        cPtr = 0
    End Sub
End Class