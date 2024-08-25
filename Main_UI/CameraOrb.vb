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
    Public Sub New(WorkingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        MyBase.setupMats(WorkingRes)

        cPtr = ORBOpen(captureRes.Width, captureRes.Height)

        Dim intrin = ORBintrinsics(cPtr)
        Dim intrinInfo(4 - 1) As Single
        Marshal.Copy(intrin, intrinInfo, 0, intrinInfo.Length)
        cameraInfo.ppx = intrinInfo(0)
        cameraInfo.ppy = intrinInfo(1)
        cameraInfo.fx = intrinInfo(2)
        cameraInfo.fy = intrinInfo(3)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cv.Size)
        If cPtr = 0 Then Exit Sub

        Try
            Dim colorData As IntPtr
            While (1)
                colorData = ORBWaitForFrame(cPtr)
                If colorData <> 0 Then Exit While
                Application.DoEvents()
            End While

            Dim accelFrame = ORBAccel(cPtr)
            If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
            ' IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

            Dim gyroFrame = ORBGyro(cPtr)
            If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

            Static imuStartTime = ORBIMUTimeStamp(cPtr)
            IMU_TimeStamp = ORBIMUTimeStamp(cPtr) - imuStartTime

            SyncLock cameraLock
                Dim cols = WorkingRes.Width, rows = WorkingRes.Height
                If captureRes = WorkingRes Then
                    If colorData <> 0 Then mbuf(mbIndex).color = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC3, colorData).Clone

                    Dim pcData = ORBPointCloud(cPtr)
                    If pcData <> 0 Then mbuf(mbIndex).pointCloud = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_32FC3, pcData) * 0.001

                    Dim leftData = ORBLeftImage(cPtr)
                    If leftData <> 0 Then mbuf(mbIndex).leftView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8U, leftData).
                            CvtColor(cv.ColorConversionCodes.GRAY2BGR) * 3

                    Dim rightData = ORBRightImage(cPtr)
                    If rightData <> 0 Then mbuf(mbIndex).rightView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8U, rightData).
                            CvtColor(cv.ColorConversionCodes.GRAY2BGR) * 3
                Else
                    If colorData <> 0 Then
                        mbuf(mbIndex).color = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8UC3, colorData).
                                                             Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
                    End If

                    Dim pcData = ORBPointCloud(cPtr)
                    If pcData <> 0 Then
                        mbuf(mbIndex).pointCloud = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_32FC3, pcData).
                                                                  Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest) * 0.001
                    End If

                    Dim leftData = ORBLeftImage(cPtr)
                    If leftData <> 0 Then
                        mbuf(mbIndex).leftView = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, leftData).
                                                                  Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest).
                                                                  CvtColor(cv.ColorConversionCodes.GRAY2BGR) * 3
                    End If

                    Dim rightData = ORBRightImage(cPtr)
                    If rightData <> 0 Then
                        mbuf(mbIndex).rightView = cv.Mat.FromPixelData(captureRes.Height, captureRes.Width, cv.MatType.CV_8U, rightData).
                                                                 Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest).
                                                                 CvtColor(cv.ColorConversionCodes.GRAY2BGR) * 3
                    End If
                End If
            End SyncLock

            MyBase.GetNextFrameCounts(IMU_FrameTime)
        Catch ex As Exception
            Console.WriteLine("Orbec camera failure..." + ex.Message)
        End Try
    End Sub
    Public Sub stopCamera()
        Application.DoEvents()
        Try
            ORBClose(cPtr)
        Catch ex As Exception
            Console.WriteLine("Orbec camera shutdown failure..." + ex.Message)
        End Try
        cPtr = 0
    End Sub
End Class