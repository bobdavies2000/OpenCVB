﻿Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Threading

Module OakD_Module_CPP
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDOpen(width As Integer, height As Integer) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub OakDWaitForFrame(cPtr As IntPtr)
    End Sub
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDRightImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDColor(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDLeftImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDintrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDRawDepth(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDGyro(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDIMUTimeStamp(cPtr As IntPtr) As Double
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function OakDAccel(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub OakDStop(cPtr As IntPtr)
    End Sub
End Module
Structure OakDIMUdata
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
End Structure
Public Class CameraOakD : Inherits Camera
    Public deviceNum As Integer
    Public accel As cv.Point3f
    Public gyro As cv.Point3f
    Dim templateX As cv.Mat
    Dim templateY As cv.Mat
    Public Sub New(workingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        MyBase.setupMats(workingRes)
        If templateX IsNot Nothing Then Return ' we have already been initialized.
        cameraName = deviceName
        cPtr = OakDOpen(captureRes.Width, captureRes.Height)
    End Sub
    Public Sub GetNextFrame(workingRes As cv.Size)
        If cPtr = 0 Then Exit Sub
        ' if it breaks here, it is because you are restarting the Oak-D camera.
        ' I can't get that to work.  Restart OpenCVB and it will work fine.
        ' Switching to another camera and then switching back to Oak-D will crash here.
        OakDWaitForFrame(cPtr)

        Static firstPass = True
        If (firstPass) Then
            firstPass = False
            captureRes = New cv.Size(1280, 720)
            Dim intrin = OakDintrinsics(cPtr)
            Dim intrinsicsArray(9 - 1) As Single
            Marshal.Copy(intrin, intrinsicsArray, 0, intrinsicsArray.Length - 1)
            cameraInfo.ppx = intrinsicsArray(2)
            cameraInfo.ppy = intrinsicsArray(5)
            cameraInfo.fx = intrinsicsArray(0)
            cameraInfo.fy = intrinsicsArray(4)

            templateX = New cv.Mat(captureRes, cv.MatType.CV_32F)
            templateY = New cv.Mat(captureRes, cv.MatType.CV_32F)
            For i = 0 To templateX.Width - 1
                templateX.Set(Of Single)(0, i, i)
            Next

            For i = 1 To templateX.Height - 1
                templateX.Row(0).CopyTo(templateX.Row(i))
                templateY.Set(Of Single)(i, 0, i)
            Next

            For i = 1 To templateY.Width - 1
                templateY.Col(0).CopyTo(templateY.Col(i))
            Next
            templateX -= cameraInfo.ppx
            templateY -= cameraInfo.ppy
        End If

        Dim accelFrame = OakDAccel(cPtr)
        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.
        Dim yComponent = IMU_Acceleration.X
        IMU_Acceleration.X = IMU_Acceleration.Y
        IMU_Acceleration.Y = yComponent

        Dim gyroFrame = OakDGyro(cPtr)
        If accelFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

        Static imuStartTime = OakDIMUTimeStamp(cPtr)
        IMU_TimeStamp = OakDIMUTimeStamp(cPtr) - imuStartTime

        Dim depth32f As New cv.Mat
        Dim depth16 = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_16U, OakDRawDepth(cPtr))
        depth16.ConvertTo(depth32f, cv.MatType.CV_32F)

        SyncLock cameraLock
            If captureRes <> workingRes Then
                Dim tmp As cv.Mat
                tmp = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, OakDColor(cPtr))
                mbuf(mbIndex).color = tmp.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)

                tmp = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, OakDLeftImage(cPtr))
                mbuf(mbIndex).leftView = tmp.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

                tmp = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, OakDRightImage(cPtr))
                mbuf(mbIndex).rightView = tmp.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            Else
                mbuf(mbIndex).color = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, OakDColor(cPtr)).Clone
                mbuf(mbIndex).leftView = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, OakDLeftImage(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                mbuf(mbIndex).rightView = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, OakDRightImage(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End If
            ' the Oak-D cameras do not produce a point cloud - update if that changes.
            Dim d32f As cv.Mat = depth32f * 0.001
            Dim worldX As New cv.Mat, worldY As New cv.Mat

            cv.Cv2.Multiply(templateX, d32f, worldX)
            worldX *= 1 / cameraInfo.fx

            cv.Cv2.Multiply(templateY, d32f, worldY)
            worldY *= 1 / cameraInfo.fy

            cv.Cv2.Merge({worldX, worldY, d32f}, mbuf(mbIndex).pointCloud)
            If workingRes <> captureRes Then
                mbuf(mbIndex).pointCloud = mbuf(mbIndex).pointCloud.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
            End If
        End SyncLock
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        If cPtr <> 0 Then
            Try
                OakDStop(cPtr)
            Catch ex As Exception
                MsgBox("OakD failure " + ex.Message)
            End Try
            cPtr = 0
            Thread.Sleep(100)
        End If
    End Sub
End Class