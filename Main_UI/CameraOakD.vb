Imports System.Runtime.InteropServices
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
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDintrinsics(cPtr As IntPtr, camera As Integer) As IntPtr
    End Function
    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDExtrinsicsRGBtoLeft(cPtr As IntPtr) As IntPtr
    End Function

    <DllImport(("Cam_Oak-D.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function OakDExtrinsicsLeftToRight(cPtr As IntPtr) As IntPtr
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





Public Class CameraOakD_CPP : Inherits GenericCamera
    Public deviceNum As Integer
    Public accel As cv.Point3f
    Public gyro As cv.Point3f
    Dim templateX As cv.Mat
    Dim templateY As cv.Mat
    Dim fxTemplate As Single
    Dim fyTemplate As Single
    Private Function copyOakIntrinsics(input() As Single, ratio As Single) As VB_Classes.VBtask.intrinsicData
        Dim output As New VB_Classes.VBtask.intrinsicData
        output.ppx = input(2) / ratio
        output.ppy = input(5) / ratio
        output.fx = input(0) / ratio
        output.fy = input(4) / ratio
        Return output
    End Function
    Public Sub New(workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        If templateX IsNot Nothing Then Return ' we have already been initialized.
        cameraName = deviceName
        cPtr = OakDOpen(captureRes.Width, captureRes.Height)

        Dim intrin = OakDintrinsics(cPtr, 2)
        Dim leftIntrinsicsArray(9 - 1) As Single
        Dim ratio = captureRes.Width / workRes.Width
        Marshal.Copy(intrin, leftIntrinsicsArray, 0, leftIntrinsicsArray.Length)
        calibData.leftIntrinsics = copyOakIntrinsics(leftIntrinsicsArray, ratio)

        intrin = OakDintrinsics(cPtr, 1)
        Dim rgbIntrinsicsArray(9 - 1) As Single
        Marshal.Copy(intrin, rgbIntrinsicsArray, 0, rgbIntrinsicsArray.Length)
        calibData.rgbIntrinsics = copyOakIntrinsics(rgbIntrinsicsArray, ratio) ' will not be used because RGB <> Left image

        Dim rgbExtrin = OakDExtrinsicsRGBtoLeft(cPtr)
        Dim rgbExtrinsicsArray(12 - 1) As Single
        Marshal.Copy(rgbExtrin, rgbExtrinsicsArray, 0, rgbExtrinsicsArray.Length)

        Dim leftExtrin = OakDExtrinsicsLeftToRight(cPtr)
        Dim leftExtrinsicsArray(12 - 1) As Single
        Marshal.Copy(leftExtrin, leftExtrinsicsArray, 0, leftExtrinsicsArray.Length)

        ReDim calibData.LtoR_translation(3 - 1)
        ReDim calibData.LtoR_rotation(9 - 1)

        ReDim calibData.ColorToLeft_translation(3 - 1)
        ReDim calibData.ColorToLeft_rotation(9 - 1)

        For i = 0 To 3 - 1
            calibData.LtoR_translation(i) = rgbExtrinsicsArray(i)
        Next
        For i = 0 To 9 - 1
            calibData.LtoR_rotation(i) = rgbExtrinsicsArray(i + 3)
        Next

        Dim translation(3 - 1) As Single
        For i = 0 To 3 - 1
            translation(i) = rgbExtrinsicsArray(i)
        Next
        calibData.baseline = System.Math.Sqrt(System.Math.Pow(translation(0), 2) +
                                              System.Math.Pow(translation(1), 2) +
                                              System.Math.Pow(translation(2), 2))

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
        templateX -= calibData.rgbIntrinsics.ppx * ratio
        templateY -= calibData.rgbIntrinsics.ppy * ratio
        fxTemplate = calibData.rgbIntrinsics.fx * ratio
        fyTemplate = calibData.rgbIntrinsics.fy * ratio
    End Sub
    Public Sub GetNextFrame(workRes As cv.Size)
        If cPtr = 0 Then Exit Sub
        OakDWaitForFrame(cPtr)

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
        Dim depth16 = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_16U, OakDRawDepth(cPtr))
        depth16.ConvertTo(depth32f, cv.MatType.CV_32F)

        SyncLock cameraLock
            If captureRes <> workRes Then
                Dim tmp As cv.Mat
                tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, OakDColor(cPtr))
                uiColor = tmp.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)

                tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, OakDLeftImage(cPtr))
                uiLeft = tmp.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)

                tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, OakDRightImage(cPtr))
                uiRight = tmp.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
            Else
                uiColor = cv.Mat.FromPixelData(workRes.Height, workRes.Width, cv.MatType.CV_8UC3, OakDColor(cPtr)).Clone
                uiLeft = cv.Mat.FromPixelData(workRes.Height, workRes.Width, cv.MatType.CV_8U, OakDLeftImage(cPtr))
                uiRight = cv.Mat.FromPixelData(workRes.Height, workRes.Width, cv.MatType.CV_8U, OakDRightImage(cPtr))
            End If

            ' the Oak-D cameras do not produce a point cloud - update if that changes.
            Dim d32f As cv.Mat = depth32f * 0.001
            Dim worldX As New cv.Mat, worldY As New cv.Mat

            cv.Cv2.Multiply(templateX, d32f, worldX)
            worldX *= 1 / fxTemplate

            cv.Cv2.Multiply(templateY, d32f, worldY)
            worldY *= 1 / fyTemplate

            Dim pc As New cv.Mat
            cv.Cv2.Merge({worldX, worldY, d32f}, pc)
            If workRes = captureRes Then
                uiPointCloud = pc
            Else
                uiPointCloud = pc.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
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
