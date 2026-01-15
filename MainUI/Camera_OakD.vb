Imports System.Runtime.InteropServices
Imports System.Threading
Imports cv = OpenCvSharp

Namespace MainApp
#Region "Externs"
    Public Class Camera_OakD : Inherits GenericCamera
        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDOpen(w As Integer, h As Integer) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Sub OakDWaitForFrame(cPtr As IntPtr)
        End Sub

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDColor(cPtr As IntPtr) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Sub OakDLaserOff(cPtr As IntPtr)
        End Sub

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDLeftImage(cPtr As IntPtr) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDRightImage(cPtr As IntPtr) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDRawDepth(cPtr As IntPtr) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDGyro(cPtr As IntPtr) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDAccel(cPtr As IntPtr) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDIMUTimeStamp(cPtr As IntPtr) As Double
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDintrinsics(cPtr As IntPtr, camera As Integer) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDExtrinsicsRGBtoLeft(cPtr As IntPtr) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDExtrinsicsLeftToRight(cPtr As IntPtr) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Sub OakDStop(cPtr As IntPtr)
        End Sub

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDDisparity(cPtr As IntPtr) As IntPtr
        End Function

        <DllImport("Cam_Oak-D.dll", CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function OakDDisparityFactor(cPtr As IntPtr) As Single
        End Function

        Private cPtr As IntPtr
        Private initialTime As Double = 0
#End Region

        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            captureRes = _captureRes
            workRes = _workRes

            ' Open the Oak-D camera
            cPtr = OakDOpen(captureRes.Width, captureRes.Height)

            If cPtr = IntPtr.Zero Then
                Throw New Exception("Failed to open Oak-D camera")
            End If

            Dim ratio = captureRes.Width \ workRes.Width
            Dim leftIntrinsicsPtr = OakDintrinsics(cPtr, 2) ' Left camera
            If leftIntrinsicsPtr <> IntPtr.Zero Then
                Dim intrinsics(8) As Single
                Marshal.Copy(leftIntrinsicsPtr, intrinsics, 0, 9)
                calibData.leftIntrinsics.fx = intrinsics(0) / ratio
                calibData.leftIntrinsics.ppx = intrinsics(2) / ratio
                calibData.leftIntrinsics.fy = intrinsics(4) / ratio
                calibData.leftIntrinsics.ppy = intrinsics(5) / ratio
            End If

            Dim leftToRightPtr = OakDExtrinsicsLeftToRight(cPtr)
            If leftToRightPtr <> IntPtr.Zero Then
                Dim extrinsics(11) As Single
                Marshal.Copy(leftToRightPtr, extrinsics, 0, 12)
                ReDim calibData.LtoR_translation(2)
                calibData.LtoR_translation(0) = extrinsics(0)
                calibData.LtoR_translation(1) = extrinsics(4)
                calibData.LtoR_translation(2) = extrinsics(8)

                calibData.baseline = System.Math.Sqrt(System.Math.Pow(calibData.LtoR_translation(0), 2) +
                                                      System.Math.Pow(calibData.LtoR_translation(1), 2) +
                                                      System.Math.Pow(calibData.LtoR_translation(2), 2))
            End If

            MyBase.prepImages()

            ' Start background thread to capture frames
            isCapturing = True
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "CaptureThread_OakD"
            captureThread.Start()
        End Sub

        Private Sub CaptureFrames()
            While isCapturing
                GetNextFrame()
            End While
        End Sub

        Public Sub GetNextFrame()
            If cPtr = IntPtr.Zero Then Return

            Dim rows = captureRes.Height
            Dim cols = captureRes.Width

            OakDWaitForFrame(cPtr)

            SyncLock cameraMutex
                Dim leftPtr = OakDLeftImage(cPtr)
                If leftPtr <> IntPtr.Zero Then
                    leftView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, leftPtr).Clone()
                    leftView = leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                End If

                Dim rightPtr = OakDRightImage(cPtr)
                If rightPtr <> IntPtr.Zero Then
                    rightView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, rightPtr).Clone()
                    rightView = rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                End If

                Dim depthPtr = OakDRawDepth(cPtr)
                If depthPtr <> IntPtr.Zero Then
                    depth16u = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_16UC1, depthPtr).Clone()
                    depth16u = depth16u.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    pointCloud = ComputePointCloud(depth16u, calibData.leftIntrinsics)
                End If

                Dim rgbPtr = OakDColor(cPtr)
                If rgbPtr <> IntPtr.Zero Then
                    color = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC3, rgbPtr).Clone()
                    color = color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                End If
                ' Get IMU data
                Dim accelPtr = OakDAccel(cPtr)
                If accelPtr <> IntPtr.Zero Then
                    IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelPtr)
                End If

                Dim gyroPtr = OakDGyro(cPtr)
                If gyroPtr <> IntPtr.Zero Then
                    IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroPtr)
                End If

                IMU_FrameTime = OakDIMUTimeStamp(cPtr)
            End SyncLock

            ' OakDLaserOff(cPtr)
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End Sub
        Public Overrides Sub StopCamera()
            OakDStop(cPtr)
        End Sub
    End Class
End Namespace

