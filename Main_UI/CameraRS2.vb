Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Imports System.Text
Imports Intel.RealSense
Imports OpenCvSharp
#If 1 Then
Public Class CameraRS2 : Inherits Camera
    Dim pipe As New Pipeline()
    Dim cfg As New Config()

    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String, Optional fps As Integer = 30)
        captureRes = _captureRes
        cfg.EnableStream(Stream.Color, _captureRes.Width, _captureRes.Height, Format.Rgb8, fps)
        cfg.EnableStream(Stream.Infrared, 1, _captureRes.Width, _captureRes.Height, Format.Y8, fps)
        cfg.EnableStream(Stream.Infrared, 2, _captureRes.Width, _captureRes.Height, Format.Y8, fps)
        cfg.EnableStream(Stream.Depth, _captureRes.Width, _captureRes.Height, Format.Z16, fps)
        'cfg.EnableStream(Stream.Accel, Format.MotionXyz32f, 63)
        'cfg.EnableStream(Stream.Gyro, Format.MotionXyz32f, 200)

        pipe.Start(cfg)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        Static alignToColor = New Align(Stream.Color)
        Static colorizer = New Colorizer
        Dim pointcloud = New PointCloud()
        Dim cols = captureRes.Width, rows = captureRes.Height

        Using frames As FrameSet = pipe.WaitForFrames()

            Dim alignedDepthFrame As Frame = alignToColor.Process(frames.DepthFrame)
            ' Dim processedFrames = colorizer.process(frames)

            Using depthFrame As DepthFrame = alignedDepthFrame.As(Of DepthFrame)()
                Dim pcFrame = pointcloud.Process(alignedDepthFrame)
                mbuf(mbIndex).pointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC3, pcFrame.Data).
                                                                 Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            End Using

            Dim colorFrame As Frame = alignToColor.Process(frames.ColorFrame)
            mbuf(mbIndex).color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, colorFrame.Data).
                 CvtColor(cvb.ColorConversionCodes.RGB2BGR).Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)

            For Each frame As Intel.RealSense.Frame In frames
                If frame.Profile.Stream = Stream.Infrared AndAlso frame.Profile.Index = 1 Then
                    mbuf(mbIndex).leftView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, frame.Data).
                                                                   Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                    Exit For
                End If
            Next

            For Each frame As Intel.RealSense.Frame In frames
                If frame.Profile.Stream = Stream.Infrared AndAlso frame.Profile.Index = 2 Then
                    mbuf(mbIndex).rightView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC1, frame.Data).
                                                                    Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                    Exit For
                End If
            Next

            'Using accelFrame As MotionFrame = frames.FirstOrDefault(Function(f) f.Profile.Stream = Stream.Accel)
            '    If accelFrame IsNot Nothing Then
            '        Dim accelData As MotionData = accelFrame.MotionData
            '        Console.WriteLine($"Accel: X={accelData.X}, Y={accelData.Y}, Z={accelData.Z}")
            '    End If
            'End Using

            'Using gyroFrame As MotionFrame = frames.FirstOrDefault(Function(f) f.Profile.Stream = Stream.Gyro)
            '    If gyroFrame IsNot Nothing Then
            '        Dim gyroData As MotionData = gyroFrame.MotionData
            '        Console.WriteLine($"Gyro: X={gyroData.X}, Y={gyroData.Y}, Z={gyroData.Z}")
            '    End If
            'End Using
        End Using
    End Sub
    Public Sub stopCamera()
        Application.DoEvents()
        Try
            pipe.Stop()
        Catch ex As Exception
        End Try
    End Sub
End Class




#Else





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
    Public acceleration As cvb.Point3f
    Public velocity As cvb.Point3f
    Public angularVelocity As cvb.Point3f
    Public angularAcceleration As cvb.Point3f
End Structure
Public Class CameraRS2 : Inherits Camera
    Public deviceNum As Integer
    Public deviceName As String
    Public cPtrOpen As IntPtr
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes
        MyBase.setupMats(WorkingRes)

        Dim devName As StringBuilder = New StringBuilder(deviceName)
        cPtr = RS2Open(devName, captureRes.Width, captureRes.Height)

        Dim intrin = RS2intrinsics(cPtr)
        Dim intrinInfo(4 - 1) As Single
        Marshal.Copy(intrin, intrinInfo, 0, intrinInfo.Length)
        cameraInfo.ppx = intrinInfo(0)
        cameraInfo.ppy = intrinInfo(1)
        cameraInfo.fx = intrinInfo(2)
        cameraInfo.fy = intrinInfo(3)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        If cPtr = 0 Then Exit Sub

        ' if OpenCVB fails here, just unplug and plug in the RealSense camera.
        RS2WaitForFrame(cPtr, WorkingRes.Width, WorkingRes.Height)

        Dim accelFrame = RS2Accel(cPtr)
        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cvb.Point3f)(accelFrame)
        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

        Dim gyroFrame = RS2Gyro(cPtr)
        If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cvb.Point3f)(gyroFrame)

        Static imuStartTime = RS2IMUTimeStamp(cPtr)
        IMU_TimeStamp = RS2IMUTimeStamp(cPtr) - imuStartTime

        SyncLock cameraLock
            Dim cols = WorkingRes.Width, rows = WorkingRes.Height
            mbuf(mbIndex).color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, RS2Color(cPtr)).Clone
            Dim tmp As cvb.Mat = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8U, RS2LeftRaw(cPtr)).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            mbuf(mbIndex).leftView = tmp * 4 - 35 ' improved brightness specific to RealSense
            tmp = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8U, RS2RightRaw(cPtr)).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            mbuf(mbIndex).rightView = tmp * 4 - 35 ' improved brightness specific to RealSense
            If captureRes <> WorkingRes Then
                Dim pc = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_32FC3, RS2PointCloud(cPtr))
                mbuf(mbIndex).pointCloud = pc.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            Else
                mbuf(mbIndex).pointCloud = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_32FC3, RS2PointCloud(cPtr)).Clone
            End If
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
#End If