Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module MyntD_Interface
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDOpen(width As integer, height As integer, fps As integer) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDWaitFrame(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDLeftImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDRightImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDImageDepth(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDImageRGBdepth(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDintrinsicsLeft(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDintrinsicsRight(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDRotationMatrix(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDProjectionMatrix(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDPointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDRawDepth(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub MyntDtaskIMU(cPtr As IntPtr)
    End Sub



    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDExtrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDAcceleration(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDGyro(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDIMU_Temperature(cPtr As IntPtr) As Single
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDIMU_TimeStamp(cPtr As IntPtr) As Double
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDIMU_Barometer(cPtr As IntPtr) As Single
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDOrientation(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDIMU_Magnetometer(cPtr As IntPtr) As IntPtr
    End Function
End Module
Public Class CameraMyntD
    Inherits Camera
    Public cameraName As String
    Public cPtr As IntPtr
    Structure MyntIntrinsics
        Dim width As UShort
        Dim height As UShort
        Dim fx As Double ' Focal length x */
        Dim fy As Double ' Focal length y */
        Dim cx As Double ' Principal point In image, x */
        Dim cy As Double ' Principal point In image, y */
        Dim k1 As Double ' Distortion factor :  [ k1, k2, p1, p2, k3 ]. Radial (k1,k2,k3) And Tangential (p1,p2) distortion
        Dim k2 As Double
        Dim p1 As Double
        Dim p2 As Double
        Dim k3 As Double
    End Structure

    ' 3x4 projection matrix in the (rectified) coordinate systems 
    '  left: fx' cx' fy' cy' 1
    '  right: fx' cx' tx fy' cy' 1
    Public projectionMatrix(12 - 1) As Double ' 
    Public rotationMatrix(9 - 1) As Double ' 3x3 rectification transform (rotation matrix) for the left camera.
    Dim IMUtask As Task
    Private Sub IMUdataCollection()
        MyntDtaskIMU(cPtr)
    End Sub
    Public Sub initialize(_width As Integer, _height As Integer, fps As Integer)
        If frameCount = 10 Then
            width = _width
            height = _height
            cPtr = MyntDOpen(width, height, fps) ' can't get it to initialize depth without starting at 1280x720 first.  Then after a few frames, 640x480...
        Else
            width = 1280
            height = 720
            cPtr = MyntDOpen(1280, 720, fps) ' always open first at full resolution
        End If
        deviceName = "MyntEyeD 1000"
        cameraName = deviceName
        If cPtr <> 0 Then
            deviceCount = 1

            Dim ptr = MyntDExtrinsics(cPtr)
            Dim rotationTranslation(12) As Double ' Mynt is using doubles but the VB copy will be a single.
            Marshal.Copy(ptr, rotationTranslation, 0, rotationTranslation.Length)
            ReDim Extrinsics_VB.rotation(9 - 1)
            ReDim Extrinsics_VB.translation(3 - 1)
            For i = 0 To Extrinsics_VB.rotation.Length - 1
                Extrinsics_VB.rotation(i) = rotationTranslation(i)
            Next
            For i = 0 To Extrinsics_VB.translation.Length - 1
                Extrinsics_VB.translation(i) = rotationTranslation(i + Extrinsics_VB.rotation.Length)
            Next

            ptr = MyntDintrinsicsLeft(cPtr)
            Dim intrinsics = Marshal.PtrToStructure(Of MyntIntrinsics)(ptr)
            intrinsicsLeft_VB.ppx = intrinsics.cx
            intrinsicsLeft_VB.ppy = intrinsics.cy
            intrinsicsLeft_VB.fx = intrinsics.fx
            intrinsicsLeft_VB.fy = intrinsics.fy
            ReDim intrinsicsLeft_VB.coeffs(5)
            intrinsicsLeft_VB.coeffs(0) = intrinsics.k1
            intrinsicsLeft_VB.coeffs(1) = intrinsics.k2
            intrinsicsLeft_VB.coeffs(2) = intrinsics.p1
            intrinsicsLeft_VB.coeffs(3) = intrinsics.p2
            intrinsicsLeft_VB.coeffs(4) = intrinsics.k3

            ptr = MyntDRotationMatrix(cPtr)
            Marshal.Copy(ptr, rotationMatrix, 0, rotationMatrix.Length)

            ptr = MyntDProjectionMatrix(cPtr)
            Marshal.Copy(ptr, projectionMatrix, 0, projectionMatrix.Length)

            ptr = MyntDintrinsicsRight(cPtr)
            intrinsics = Marshal.PtrToStructure(Of MyntIntrinsics)(ptr)
            intrinsicsRight_VB.ppx = intrinsics.cx
            intrinsicsRight_VB.ppy = intrinsics.cy
            intrinsicsRight_VB.fx = intrinsics.fx
            intrinsicsRight_VB.fy = intrinsics.fy
            ReDim intrinsicsRight_VB.coeffs(5)
            intrinsicsRight_VB.coeffs(0) = intrinsics.k1
            intrinsicsRight_VB.coeffs(1) = intrinsics.k2
            intrinsicsRight_VB.coeffs(2) = intrinsics.p1
            intrinsicsRight_VB.coeffs(3) = intrinsics.p2
            intrinsicsRight_VB.coeffs(4) = intrinsics.k3

            IMUtask = New Task(Sub() IMUdataCollection())
            IMUtask.Start()
        End If
    End Sub

    Public Sub GetNextFrame()
        If frameCount = 10 And width = 640 Then initialize(640, 480, 30)
        If cPtr = 0 Then Exit Sub
        Dim imagePtr = MyntDWaitFrame(cPtr)
        Dim acc = MyntDAcceleration(cPtr)
        IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(acc)
        IMU_Acceleration.Y *= -1 ' make it consistent with the other cameras.

        Dim ang = MyntDGyro(cPtr)
        IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(ang)
        IMU_AngularVelocity *= 0.0174533 ' MyntD gyro is in degrees/sec

        IMU_Temperature = MyntDIMU_Temperature(cPtr)

        Static startTime = MyntDIMU_TimeStamp(cPtr)
        IMU_TimeStamp = MyntDIMU_TimeStamp(cPtr) - startTime

        Dim depthRGBPtr = MyntDImageRGBdepth(cPtr)
        Dim depth16Ptr = MyntDRawDepth(cPtr)
        Dim rightPtr = MyntDRightImage(cPtr)
        Dim pcPtr = MyntDPointCloud(cPtr)
        If imagePtr <> 0 And depthRGBPtr <> 0 And rightPtr <> 0 And depth16Ptr <> 0 And pcPtr <> 0 Then
            color = New cv.Mat(height, width, cv.MatType.CV_8UC3, imagePtr).Clone()
            leftView = color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            rightView = New cv.Mat(height, width, cv.MatType.CV_8UC3, rightPtr).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, depthRGBPtr).Clone()
            pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3, pcPtr).Clone()
            depth16 = New cv.Mat(height, width, cv.MatType.CV_16U, depth16Ptr).Clone()
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End If
    End Sub
    Public Sub stopCamera()
        Application.DoEvents()
        frameCount = 0
        cPtr = 0
    End Sub
End Class
