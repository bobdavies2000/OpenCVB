Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module ORB_Module
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBWaitForFrame(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBRightImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBColor(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBLeftImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBIntrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBPointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBGyro(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBIMUTimeStamp(cPtr As IntPtr) As Double
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBAccel(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub ORBClose(cPtr As IntPtr)
    End Sub
    <DllImport(("Cam_ORB335L.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function ORBOpen(
                                                   width As Integer, height As Integer) As IntPtr
    End Function
End Module
Public Class CameraORB : Inherits Camera
    Public deviceNum As Integer
    Public deviceName As String
    Public cPtrOpen As IntPtr
    Public Sub New(workingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        MyBase.setupMats(workingRes)

        cPtr = ORBOpen(captureRes.Width, captureRes.Height)

        Dim intrin = ORBintrinsics(cPtr)
        Dim intrinInfo(4 - 1) As Single
        Marshal.Copy(intrin, intrinInfo, 0, intrinInfo.Length)
        cameraInfo.ppx = intrinInfo(0)
        cameraInfo.ppy = intrinInfo(1)
        cameraInfo.fx = intrinInfo(2)
        cameraInfo.fy = intrinInfo(3)
    End Sub
    Public Sub GetNextFrame(workingRes As cv.Size)

        If cPtr = 0 Then Exit Sub

        Dim colorData = ORBWaitForFrame(cPtr)

        'Dim accelFrame = ORBAccel(cPtr)
        'If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
        'IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

        'Dim gyroFrame = ORBGyro(cPtr)
        'If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

        'Static imuStartTime = ORBIMUTimeStamp(cPtr)
        'IMU_TimeStamp = ORBIMUTimeStamp(cPtr) - imuStartTime

        SyncLock cameraLock
            Dim cols = workingRes.Width, rows = workingRes.Height
            If colorData <> 0 Then mbuf(mbIndex).color = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, colorData).Clone

            Dim pcData = ORBPointCloud(cPtr)
            If pcData <> 0 Then mbuf(mbIndex).pointCloud = New cv.Mat(rows, cols, cv.MatType.CV_32FC3, pcData) * 0.001

            Dim leftData = ORBLeftImage(cPtr)
            mbuf(mbIndex).leftView = New cv.Mat(rows, cols, cv.MatType.CV_8U, leftData).Clone

            Dim rightData = ORBRightImage(cPtr)
            mbuf(mbIndex).rightView = New cv.Mat(rows, cols, cv.MatType.CV_8U, rightData).Clone
            'Else
            '    If colorData <> 0 Then
            '        mbuf(mbIndex).color = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, colorData).
            '                                         Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            '    End If

            '    Dim pcData = ORBPointCloud(cPtr)
            '    If pcData <> 0 Then
            '        mbuf(mbIndex).pointCloud = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, pcData).
            '                                              Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest) * 0.001
            '    End If

            '    Dim leftData = ORBLeftImage(cPtr)
            '    If leftData <> 0 Then
            '        mbuf(mbIndex).leftView = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, leftData).
            '                                              Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            '    End If

            '    Dim rightData = ORBRightImage(cPtr)
            '    If rightData <> 0 Then
            '        mbuf(mbIndex).rightView = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, rightData).
            '                                             Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            '    End If
            'End If
            'Dim tmp As cv.Mat = New cv.Mat(rows, cols, cv.MatType.CV_8U, ORBLeftRaw(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            'mbuf(mbIndex).leftView = tmp * 4 - 35 ' improved brightness specific to RealSense
            'tmp = New cv.Mat(rows, cols, cv.MatType.CV_8U, ORBRightRaw(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            'mbuf(mbIndex).rightView = tmp * 4 - 35 ' improved brightness specific to RealSense
            'If captureRes <> workingRes Then
            '    Dim pc = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, ORBPointCloud(cPtr))
            '    mbuf(mbIndex).pointCloud = pc.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            'Else
            '    mbuf(mbIndex).pointCloud = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, ORBPointCloud(cPtr)).Clone
            'End If
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        Application.DoEvents()
        Try
            ORBClose(cPtr)
        Catch ex As Exception
        End Try
        cPtr = 0
    End Sub
End Class