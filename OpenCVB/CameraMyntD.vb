Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module MyntD_Interface
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function MyntDOpen(width As Integer, height As Integer) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function MyntDWaitFrame(cPtr As IntPtr, w As Integer, h As Integer) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function MyntDRightImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function MyntDIntrinsicsLeft(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function MyntDPointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Sub MyntDtaskIMU(cPtr As IntPtr)
    End Sub
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function MyntDAcceleration(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function MyntDGyro(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)> Public Function MyntDIMU_TimeStamp(cPtr As IntPtr) As Double
    End Function
End Module
Public Class CameraMyntD : Inherits Camera
    Public Sub New(workingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        MyBase.setupMats(workingRes)
        cPtr = MyntDOpen(captureRes.Width, captureRes.Height)
        cameraName = deviceName
        If cPtr <> 0 Then
            deviceCount = 1
            IMUtask = New Task(Sub() IMUdataCollection())
            IMUtask.Start()
        End If
    End Sub
    Dim IMUtask As Task
    Private Sub IMUdataCollection()
        MyntDtaskIMU(cPtr)
    End Sub
    Public Sub GetNextFrame(workingRes As cv.Size)
        If cPtr = 0 Then Exit Sub

        Dim imagePtr = MyntDWaitFrame(cPtr, workingRes.Width, workingRes.Height)
        Dim acc = MyntDAcceleration(cPtr)
        IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(acc)
        IMU_Acceleration.Y *= -1 ' make it consistent with the other cameras.

        Dim ang = MyntDGyro(cPtr)
        IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(ang)
        IMU_AngularVelocity *= 0.0174533 ' MyntD gyro is in degrees/sec

        Static startTime = MyntDIMU_TimeStamp(cPtr)
        IMU_TimeStamp = MyntDIMU_TimeStamp(cPtr) - startTime

        Dim rightPtr = MyntDRightImage(cPtr)
        Dim pcPtr = MyntDPointCloud(cPtr)

        'If imagePtr <> 0 And rightPtr <> 0 And pcPtr <> 0 Then
        '    color = New cv.Mat(workingRes.Height, workingRes.Width, cv.MatType.CV_8UC3, imagePtr)
        '    leftView = color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        '    rightView = New cv.Mat(workingRes.Height, workingRes.Width, cv.MatType.CV_8UC3, rightPtr).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        '    pointCloud = New cv.Mat(workingRes.Height, workingRes.Width, cv.MatType.CV_32FC3, pcPtr)
        'End If

        'MyBase.GetNextFrameCounts(IMU_FrameTime)

        If imagePtr <> 0 And rightPtr <> 0 And pcPtr <> 0 Then
            SyncLock cameraLock
                If captureRes <> workingRes Then
                    Dim tmp As cv.Mat
                    tmp = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, imagePtr)
                    mbuf(mbIndex).color = tmp.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
                    mbuf(mbIndex).leftView = mbuf(mbIndex).color

                    tmp = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, rightPtr).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                    mbuf(mbIndex).rightView = tmp.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)

                    tmp = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, pcPtr)
                    mbuf(mbIndex).pointCloud = tmp.Resize(workingRes, 0, 0, cv.InterpolationFlags.Nearest)
                Else
                    mbuf(mbIndex).color = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, imagePtr).Clone
                    mbuf(mbIndex).leftView = mbuf(mbIndex).color
                    mbuf(mbIndex).rightView = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, rightPtr).Clone
                    mbuf(mbIndex).pointCloud = New cv.Mat(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, pcPtr).Clone
                End If
            End SyncLock
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End If
    End Sub
    Public Sub stopCamera()
        cPtr = 0
    End Sub
End Class
