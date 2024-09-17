Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Imports System.Threading
Imports VB_Classes

#If 1 Then
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
    Public acceleration As cvb.Point3f
    Public velocity As cvb.Point3f
    Public angularVelocity As cvb.Point3f
    Public angularAcceleration As cvb.Point3f
End Structure
Public Class CameraOakD : Inherits GenericCamera
    Public deviceNum As Integer
    Public accel As cvb.Point3f
    Public gyro As cvb.Point3f
    Dim templateX As cvb.Mat
    Dim templateY As cvb.Mat
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes
        If templateX IsNot Nothing Then Return ' we have already been initialized.
        cameraName = deviceName
        cPtr = OakDOpen(captureRes.Width, captureRes.Height)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        If cPtr = 0 Then Exit Sub
        ' if it breaks here, it is because you are restarting the Oak-D camera.
        ' I can't get that to work.  Restart OpenCVB and it will work fine.
        ' Switching to another camera and then switching back to Oak-D will crash here.
        OakDWaitForFrame(cPtr)

        Static FirstPass = True
        If (FirstPass) Then
            FirstPass = False
            captureRes = New cvb.Size(1280, 720)
            Dim intrin = OakDintrinsics(cPtr)
            Dim intrinsicsArray(9 - 1) As Single
            Marshal.Copy(intrin, intrinsicsArray, 0, intrinsicsArray.Length)
            cameraInfo.ppx = intrinsicsArray(2)
            cameraInfo.ppy = intrinsicsArray(5)
            cameraInfo.fx = intrinsicsArray(0)
            cameraInfo.fy = intrinsicsArray(4)

            templateX = New cvb.Mat(captureRes, cvb.MatType.CV_32F)
            templateY = New cvb.Mat(captureRes, cvb.MatType.CV_32F)
            For i = 0 To templateX.Width - 1
                templateX.Set(Of Single)(0, i, i)
            Next

            For i = 1 To templateX.Height - 1
                templateX.Row(0).CopyTo(templateX.Row(i))
                templateY.Set(Of Single)(i, 0, i)
            Next

            For i = 0 To templateX.Height - 1
            Next

            For i = 1 To templateY.Width - 1
                templateY.Col(0).CopyTo(templateY.Col(i))
            Next
            templateX -= cameraInfo.ppx
            templateY -= cameraInfo.ppy
        End If

        Dim accelFrame = OakDAccel(cPtr)
        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cvb.Point3f)(accelFrame)
        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.
        Dim yComponent = IMU_Acceleration.X
        IMU_Acceleration.X = IMU_Acceleration.Y
        IMU_Acceleration.Y = yComponent

        Dim gyroFrame = OakDGyro(cPtr)
        If accelFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cvb.Point3f)(gyroFrame)

        Static imuStartTime = OakDIMUTimeStamp(cPtr)
        IMU_TimeStamp = OakDIMUTimeStamp(cPtr) - imuStartTime

        Dim depth32f As New cvb.Mat
        Dim depth16 = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_16U, OakDRawDepth(cPtr))
        depth16.ConvertTo(depth32f, cvb.MatType.CV_32F)

        SyncLock cameraLock
            If captureRes <> WorkingRes Then
                Dim tmp As cvb.Mat
                tmp = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_8UC3, OakDColor(cPtr))
                uiColor = tmp.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)

                tmp = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_8U, OakDLeftImage(cPtr))
                uiLeft = tmp.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

                tmp = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_8U, OakDRightImage(cPtr))
                uiRight = tmp.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            Else
                uiColor = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8UC3, OakDColor(cPtr)).Clone
                uiLeft = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8U, OakDLeftImage(cPtr)).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
                uiRight = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8U, OakDRightImage(cPtr)).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            End If

            ' the Oak-D cameras do not produce a point cloud - update if that changes.
            Dim d32f As cvb.Mat = depth32f * 0.001
            Dim worldX As New cvb.Mat, worldY As New cvb.Mat

            cvb.Cv2.Multiply(templateX, d32f, worldX)
            worldX *= 1 / cameraInfo.fx

            cvb.Cv2.Multiply(templateY, d32f, worldY)
            worldY *= 1 / cameraInfo.fy

            Dim pc As New cvb.Mat
            cvb.Cv2.Merge({worldX, worldY, d32f}, pc)
            If WorkingRes = captureRes Then
                uiPointCloud = pc
            Else
                uiPointCloud = pc.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
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

#Else

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
    Public acceleration As cvb.Point3f
    Public velocity As cvb.Point3f
    Public angularVelocity As cvb.Point3f
    Public angularAcceleration As cvb.Point3f
End Structure
Public Class CameraOakD : Inherits GenericCamera
    Public deviceNum As Integer
    Public accel As cvb.Point3f
    Public gyro As cvb.Point3f
    Dim templateX As cvb.Mat
    Dim templateY As cvb.Mat
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes
        If templateX IsNot Nothing Then Return ' we have already been initialized.
        cameraName = deviceName
        cPtr = OakDOpen(captureRes.Width, captureRes.Height)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        If cPtr = 0 Then Exit Sub
        ' if it breaks here, it is because you are restarting the Oak-D camera.
        ' I can't get that to work.  Restart OpenCVB and it will work fine.
        ' Switching to another camera and then switching back to Oak-D will crash here.
        OakDWaitForFrame(cPtr)

        Static FirstPass = True
        If (FirstPass) Then
            FirstPass = False
            Dim intrin = OakDintrinsics(cPtr)
            Dim intrinsicsArray(9 - 1) As Single
            Marshal.Copy(intrin, intrinsicsArray, 0, intrinsicsArray.Length)
            cameraInfo.ppx = intrinsicsArray(2)
            cameraInfo.ppy = intrinsicsArray(5)
            cameraInfo.fx = intrinsicsArray(0)
            cameraInfo.fy = intrinsicsArray(4)

            templateX = New cvb.Mat(captureRes, cvb.MatType.CV_32F)
            templateY = New cvb.Mat(captureRes, cvb.MatType.CV_32F)
            For i = 0 To templateX.Width - 1
                templateX.Set(Of Single)(0, i, i)
            Next

            For i = 1 To templateX.Height - 1
                templateX.Row(0).CopyTo(templateX.Row(i))
                templateY.Set(Of Single)(i, 0, i)
            Next

            For i = 0 To templateX.Height - 1
            Next

            For i = 1 To templateY.Width - 1
                templateY.Col(0).CopyTo(templateY.Col(i))
            Next
            templateX -= cameraInfo.ppx
            templateY -= cameraInfo.ppy
        End If

        Dim accelFrame = OakDAccel(cPtr)
        If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cvb.Point3f)(accelFrame)
        IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.
        Dim yComponent = IMU_Acceleration.X
        IMU_Acceleration.X = IMU_Acceleration.Y
        IMU_Acceleration.Y = yComponent

        Dim gyroFrame = OakDGyro(cPtr)
        If accelFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cvb.Point3f)(gyroFrame)

        Static imuStartTime = OakDIMUTimeStamp(cPtr)
        IMU_TimeStamp = OakDIMUTimeStamp(cPtr) - imuStartTime

        Dim depth32f As New cvb.Mat
        Dim depth16 = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_16U, OakDRawDepth(cPtr))
        depth16.ConvertTo(depth32f, cvb.MatType.CV_32F)

        SyncLock cameraLock
            If captureRes <> WorkingRes Then
                Dim tmp As cvb.Mat
                tmp = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_8UC3, OakDColor(cPtr))
                uiColor = tmp.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)

                tmp = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_8U, OakDLeftImage(cPtr))
                uiLeft = tmp.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)

                tmp = cvb.Mat.FromPixelData(captureRes.Height, captureRes.Width, cvb.MatType.CV_8U, OakDRightImage(cPtr))
                uiRight = tmp.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            Else
                uiColor = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8UC3, OakDColor(cPtr)).Clone
                uiLeft = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8U, OakDLeftImage(cPtr)).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
                uiRight = cvb.Mat.FromPixelData(WorkingRes.Height, WorkingRes.Width, cvb.MatType.CV_8U, OakDRightImage(cPtr)).CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            End If

            ' the Oak-D cameras do not produce a point cloud - update if that changes.
            Dim d32f As cvb.Mat = depth32f * 0.001
            Dim worldX As New cvb.Mat, worldY As New cvb.Mat

            cvb.Cv2.Multiply(templateX, d32f, worldX)
            worldX *= 1 / cameraInfo.fx

            cvb.Cv2.Multiply(templateY, d32f, worldY)
            worldY *= 1 / cameraInfo.fy

            Dim pc As New cvb.Mat
            cvb.Cv2.Merge({worldX, worldY, d32f}, pc)
            If WorkingRes = captureRes Then
                uiPointCloud = pc
            Else
                uiPointCloud = pc.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
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
#End If