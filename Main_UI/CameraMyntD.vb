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
Public Class CameraMyntD : Inherits GenericCamera
    Public Sub New(workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        cPtr = MyntDOpen(captureRes.Width, captureRes.Height)
        cameraName = deviceName
        If cPtr <> 0 Then
            deviceCount = 1
            'IMUtask = New Task(Sub() IMUdataCollection())
            'IMUtask.Start()
        End If
    End Sub
    'Dim IMUtask As Task
    Private Sub IMUdataCollection()
        MyntDtaskIMU(cPtr)
    End Sub
    Public Sub GetNextFrame(workRes As cv.Size)
        If cPtr = 0 Then Exit Sub

        Dim imagePtr = MyntDWaitFrame(cPtr, workRes.Width, workRes.Height)
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
        '    color = New cv.Mat(workRes.Height, workRes.Width, cv.MatType.CV_8UC3, imagePtr)
        '    leftView = color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        '    rightView = New cv.Mat(workRes.Height, workRes.Width, cv.MatType.CV_8UC3, rightPtr).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        '    pointCloud = New cv.Mat(workRes.Height, workRes.Width, cv.MatType.CV_32FC3, pcPtr)
        'End If

        'MyBase.GetNextFrameCounts(IMU_FrameTime)

        If imagePtr <> 0 And rightPtr <> 0 And pcPtr <> 0 Then
            SyncLock cameraLock
                If captureRes <> workRes Then
                    Dim tmp As cv.Mat
                    tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, imagePtr)
                    uiColor = tmp.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    uiLeft = uiColor.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

                    tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, rightPtr).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                    uiRight = tmp.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)

                    tmp = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, pcPtr)
                    uiPointCloud = tmp.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                Else
                    uiColor = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, imagePtr).Clone
                    uiLeft = uiColor.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                    uiRight = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, rightPtr)
                    uiRight = uiRight.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                    uiPointCloud = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, pcPtr).Clone
                End If
            End SyncLock
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End If
    End Sub
    Public Sub stopCamera()
        cPtr = 0
    End Sub
End Class
