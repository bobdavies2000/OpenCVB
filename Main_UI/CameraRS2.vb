Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Imports Intel.RealSense
Imports System.Text
#If 1 Then
' VB.Net version of the Realsense interface.  It works but is not stable.
Public Class CameraRS2 : Inherits Camera
    Dim pipe As New Pipeline()
    Dim cfg As New Config()
    Dim profiles As PipelineProfile
    Public myIntrinsics As Intrinsics
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

        cfg.EnableDevice(serialNumber)

        captureRes = _captureRes
        cfg.EnableStream(Stream.Color, captureRes.Width, captureRes.Height, Format.Rgb8, fps)
        cfg.EnableStream(Stream.Infrared, 1, captureRes.Width, captureRes.Height, Format.Y8, fps)
        cfg.EnableStream(Stream.Infrared, 2, captureRes.Width, captureRes.Height, Format.Y8, fps)
        cfg.EnableStream(Stream.Depth, captureRes.Width, captureRes.Height, Format.Z16, fps)
        cfg.EnableStream(Stream.Accel, Format.MotionXyz32f, 63)
        cfg.EnableStream(Stream.Gyro, Format.MotionXyz32f, 200)

        profiles = pipe.Start(cfg)
        Dim StreamColor = profiles.GetStream(Stream.Color)
        myIntrinsics = StreamColor.As(Of VideoStreamProfile)().GetIntrinsics()
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

            mbuf(mbIndex).color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, alignedFrames.ColorFrame.Data).
                                  CvtColor(cvb.ColorConversionCodes.RGB2BGR)

            Dim pcFrame = pointcloud.Process(alignedFrames.DepthFrame)
            mbuf(mbIndex).pointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC3, pcFrame.Data)

            If captureRes.Width <> WorkingRes.Width Or captureRes.Height <> WorkingRes.Height Then
                mbuf(mbIndex).color = mbuf(mbIndex).color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                mbuf(mbIndex).leftView = mbuf(mbIndex).leftView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                mbuf(mbIndex).rightView = mbuf(mbIndex).rightView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                mbuf(mbIndex).pointCloud = mbuf(mbIndex).pointCloud.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
            End If

            GC.Collect() ' do you think this is necessary?  Remove it and check...
            MyBase.GetNextFrameCounts(IMU_FrameTime)
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





'Module RS2_Module_CPP
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
'    Public Sub RS2WaitForFrame(cPtr As IntPtr, w As Integer, h As Integer)
'    End Sub
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2RightRaw(cPtr As IntPtr) As IntPtr
'    End Function
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2Color(cPtr As IntPtr) As IntPtr
'    End Function
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2LeftRaw(cPtr As IntPtr) As IntPtr
'    End Function
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2intrinsics(cPtr As IntPtr) As IntPtr
'    End Function
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2PointCloud(cPtr As IntPtr) As IntPtr
'    End Function
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2Gyro(cPtr As IntPtr) As IntPtr
'    End Function
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2IMUTimeStamp(cPtr As IntPtr) As Double
'    End Function
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2Accel(cPtr As IntPtr) As IntPtr
'    End Function
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub RS2Stop(cPtr As IntPtr)
'    End Sub
'    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function RS2Open(
'                                                   <MarshalAs(UnmanagedType.LPStr)> ByVal deviceName As StringBuilder,
'                                                   width As Integer, height As Integer) As IntPtr
'    End Function
'End Module

'Structure RS2IMUdata
'    Public acceleration As cvb.Point3f
'    Public velocity As cvb.Point3f
'    Public angularVelocity As cvb.Point3f
'    Public angularAcceleration As cvb.Point3f
'End Structure
'Public Class CameraRS2 : Inherits Camera
'    Public deviceNum As Integer
'    Public deviceName As String
'    Public cPtrOpen As IntPtr
'    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, devName As String)
'        captureRes = _captureRes
'        MyBase.setupMats(WorkingRes)

'        'Dim serialNumber As String = ""
'        'Dim ctx As New Context()
'        'Dim searchName As String = devName

'        'For Each dev In ctx.QueryDevices()
'        '    Dim deviceName As String = dev.Info.Item(0)
'        '    If String.Compare(deviceName, searchName) = 0 Then
'        '        serialNumber = dev.Info.Item(1)
'        '    End If
'        'Next

'        'Dim sNumber As StringBuilder = New StringBuilder(serialNumber)

'        'cPtr = RS2Open(sNumber, captureRes.Width, captureRes.Height)
'        Dim deviceName As StringBuilder = New StringBuilder(serialNumber)
'        cPtr = RS2Open(deviceName, captureRes.Width, captureRes.Height)

'        Dim intrin = RS2intrinsics(cPtr)
'        Dim intrinInfo(4 - 1) As Single
'        Marshal.Copy(intrin, intrinInfo, 0, intrinInfo.Length)
'        cameraInfo.ppx = intrinInfo(0)
'        cameraInfo.ppy = intrinInfo(1)
'        cameraInfo.fx = intrinInfo(2)
'        cameraInfo.fy = intrinInfo(3)
'    End Sub
'    Public Sub GetNextFrame(WorkingRes As cvb.Size)
'        If cPtr = 0 Then Exit Sub

'        ' if OpenCVB fails here, just unplug and plug in the RealSense camera.
'        RS2WaitForFrame(cPtr, WorkingRes.Width, WorkingRes.Height)

'        Dim accelFrame = RS2Accel(cPtr)
'        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cvb.Point3f)(accelFrame)
'        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

'        Dim gyroFrame = RS2Gyro(cPtr)
'        If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cvb.Point3f)(gyroFrame)

'        Static imuStartTime = RS2IMUTimeStamp(cPtr)
'        IMU_TimeStamp = RS2IMUTimeStamp(cPtr) - imuStartTime

'        SyncLock cameraLock
'            Dim cols = WorkingRes.Width, rows = WorkingRes.Height
'            mbuf(mbIndex).color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC3, RS2Color(cPtr))
'            mbuf(mbIndex).leftView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8U, RS2LeftRaw(cPtr)).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
'            mbuf(mbIndex).rightView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8U, RS2RightRaw(cPtr)).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
'            mbuf(mbIndex).pointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC3, RS2PointCloud(cPtr))
'        End SyncLock
'        MyBase.GetNextFrameCounts(IMU_FrameTime)
'    End Sub
'    Public Sub stopCamera()
'        Application.DoEvents()
'        Try
'            RS2Stop(cPtr)
'        Catch ex As Exception
'        End Try
'        cPtr = 0
'    End Sub
'End Class
#End If