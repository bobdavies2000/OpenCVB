﻿Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Text

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
Public Class CameraRS2 : Inherits Camera
    Public deviceNum As Integer
    Public deviceName As String
    Public cPtrOpen As IntPtr
    Public Sub New(workingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        MyBase.setupMats(workingRes)

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
    Public Sub GetNextFrame(workingRes As cv.Size)
        If cPtr = 0 Then Exit Sub

        ' if OpenCVB fails here, just unplug and plug in the RealSense camera.
        RS2WaitForFrame(cPtr, workingRes.Width, workingRes.Height)

        Dim accelFrame = RS2Accel(cPtr)
        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

        Dim gyroFrame = RS2Gyro(cPtr)
        If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

        Static imuStartTime = RS2IMUTimeStamp(cPtr)
        IMU_TimeStamp = RS2IMUTimeStamp(cPtr) - imuStartTime

        SyncLock cameraLock
            Dim cols = workingRes.Width, rows = workingRes.Height
            mbuf(mbIndex).color = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, RS2Color(cPtr)).Clone
            mbuf(mbIndex).leftView = New cv.Mat(rows, cols, cv.MatType.CV_8U, RS2LeftRaw(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            mbuf(mbIndex).rightView = New cv.Mat(rows, cols, cv.MatType.CV_8U, RS2RightRaw(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            If captureRes <> workingRes Then
                Dim pc = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, RS2PointCloud(cPtr))
                mbuf(mbIndex).pointCloud = pc.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            Else
                mbuf(mbIndex).pointCloud = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, RS2PointCloud(cPtr)).Clone
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