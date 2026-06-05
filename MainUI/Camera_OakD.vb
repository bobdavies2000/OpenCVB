Imports System.Runtime.InteropServices
Imports System.Threading
Imports cv = OpenCvSharp

Namespace MainApp
    Public Class Camera_OakD : Inherits GenericCamera
        Private cPtr As IntPtr
        Private deviceClass As Integer = 3 ' 3D or 4D.

        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            OakDNative.EnsureLoaded()

            captureRes = _captureRes
            workRes = _workRes

            Dim deviceIndex = If(deviceName.Contains("Oak-4D"), settings.OakIndex4D, settings.OakIndex3D)
            deviceClass = If(deviceName.Contains("Oak-4D"), 4, 3)
            cPtr = OakDNative.OakDOpen(captureRes.Width, captureRes.Height, deviceIndex)

            If cPtr = IntPtr.Zero Then
                Throw New Exception("Failed to open Oak-D camera")
            End If

            Dim ratio = captureRes.Width \ workRes.Width
            Dim intrinsicsPtr = OakDNative.OakDintrinsics(cPtr, 1) ' RGB camera
            If intrinsicsPtr <> IntPtr.Zero Then
                Dim intrinsics(8) As Single
                Marshal.Copy(intrinsicsPtr, intrinsics, 0, 9)
                calibData.rgbIntrinsics.fx = intrinsics(0) / ratio
                calibData.rgbIntrinsics.ppx = intrinsics(2) / ratio
                calibData.rgbIntrinsics.fy = intrinsics(4) / ratio
                calibData.rgbIntrinsics.ppy = intrinsics(5) / ratio
            End If

            Dim leftIntrinsicsPtr = OakDNative.OakDintrinsics(cPtr, 2) ' Left camera
            If leftIntrinsicsPtr <> IntPtr.Zero Then
                Dim intrinsics(8) As Single
                Marshal.Copy(leftIntrinsicsPtr, intrinsics, 0, 9)
                calibData.leftIntrinsics.fx = intrinsics(0) / ratio
                calibData.leftIntrinsics.ppx = intrinsics(2) / ratio
                calibData.leftIntrinsics.fy = intrinsics(4) / ratio
                calibData.leftIntrinsics.ppy = intrinsics(5) / ratio
            End If

            Dim rightIntrinsicsPtr = OakDNative.OakDintrinsics(cPtr, 3) ' Right camera
            If rightIntrinsicsPtr <> IntPtr.Zero Then
                Dim intrinsics(8) As Single
                Marshal.Copy(rightIntrinsicsPtr, intrinsics, 0, 9)
                calibData.rightIntrinsics.fx = intrinsics(0) / ratio
                calibData.rightIntrinsics.ppx = intrinsics(2) / ratio
                calibData.rightIntrinsics.fy = intrinsics(4) / ratio
                calibData.rightIntrinsics.ppy = intrinsics(5) / ratio
            End If

            Dim rgbToLeftPtr = OakDNative.OakDExtrinsicsRGBtoLeft(cPtr)
            Dim extrinsics(11) As Single
            If rgbToLeftPtr <> IntPtr.Zero Then
                Marshal.Copy(rgbToLeftPtr, extrinsics, 0, 12)
                ReDim calibData.ColorToLeft_translation(2)
                ReDim calibData.ColorToLeft_rotation(9 - 1)

                calibData.ColorToLeft_translation(0) = extrinsics(0) / ratio
                calibData.ColorToLeft_translation(1) = extrinsics(4) / ratio
                calibData.ColorToLeft_translation(2) = extrinsics(8) / ratio

                calibData.ColorToLeft_rotation(0) = extrinsics(0)
                calibData.ColorToLeft_rotation(1) = extrinsics(1)
                calibData.ColorToLeft_rotation(2) = extrinsics(2)

                calibData.ColorToLeft_rotation(3) = extrinsics(4)
                calibData.ColorToLeft_rotation(4) = extrinsics(5)
                calibData.ColorToLeft_rotation(5) = extrinsics(6)

                calibData.ColorToLeft_rotation(6) = extrinsics(8)
                calibData.ColorToLeft_rotation(7) = extrinsics(9)
                calibData.ColorToLeft_rotation(8) = extrinsics(10)
            End If

            Dim leftToRightPtr = OakDNative.OakDExtrinsicsLeftToRight(cPtr)
            If leftToRightPtr <> IntPtr.Zero Then
                Marshal.Copy(leftToRightPtr, extrinsics, 0, 12)
                ReDim calibData.LtoR_translation(2)
                ReDim calibData.LtoR_rotation(9 - 1)
                calibData.LtoR_translation(0) = extrinsics(0)
                calibData.LtoR_translation(1) = extrinsics(4)
                calibData.LtoR_translation(2) = extrinsics(8)

                calibData.LtoR_rotation(0) = extrinsics(0)
                calibData.LtoR_rotation(1) = extrinsics(1)
                calibData.LtoR_rotation(2) = extrinsics(2)

                calibData.LtoR_rotation(3) = extrinsics(4)
                calibData.LtoR_rotation(4) = extrinsics(5)
                calibData.LtoR_rotation(5) = extrinsics(6)

                calibData.LtoR_rotation(6) = extrinsics(8)
                calibData.LtoR_rotation(7) = extrinsics(9)
                calibData.LtoR_rotation(8) = extrinsics(10)

                calibData.baseline = System.Math.Sqrt(System.Math.Pow(calibData.LtoR_translation(0), 2) +
                                                      System.Math.Pow(calibData.LtoR_translation(1), 2) +
                                                      System.Math.Pow(calibData.LtoR_translation(2), 2))
            End If

            MyBase.prepImages()

            isCapturing = True
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "CaptureThread_OakD"
            captureThread.Start()
        End Sub

        Private Sub CaptureFrames()
            While isCapturing
                Try
                    GetNextFrame()
                Catch ex As Exception
                    Debug.WriteLine("OakD capture failed: " & ex.Message)
                    isCapturing = False
                End Try
            End While
        End Sub

        Public Sub GetNextFrame()
            If cPtr = IntPtr.Zero Then Return

            Dim rows = captureRes.Height
            Dim cols = captureRes.Width

            OakDNative.OakDWaitForFrame(cPtr)

            SyncLock cameraMutex
                Dim leftPtr = OakDNative.OakDLeftImage(cPtr)
                If leftPtr <> IntPtr.Zero Then
                    leftView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, leftPtr).Clone()
                    leftView = leftView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                End If

                Dim rightPtr = OakDNative.OakDRightImage(cPtr)
                If rightPtr <> IntPtr.Zero Then
                    rightView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, rightPtr).Clone()
                    rightView = rightView.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                End If

                Dim depthPtr = If(deviceClass = 3, OakDNative.OakDRawDepth3D(cPtr), OakDNative.OakDRawDepth4D(cPtr))
                If depthPtr <> IntPtr.Zero Then
                    depth16u = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_16UC1, depthPtr).Clone()
                    depth16u = depth16u.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    pointCloud = ComputePointCloud(depth16u, calibData.leftIntrinsics)
                End If

                Dim rgbPtr = OakDNative.OakDColor(cPtr)
                If rgbPtr <> IntPtr.Zero Then
                    color = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC3, rgbPtr).Clone()
                    color = color.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                End If

                If deviceClass = 4 Then
                    Dim disparityPtr = OakDNative.OakDDisparity(cPtr)
                    If disparityPtr <> IntPtr.Zero Then
                        disparity = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, disparityPtr).Clone()
                        disparity = disparity.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    End If
                End If

                Dim accelPtr = OakDNative.OakDAccel(cPtr)
                If accelPtr <> IntPtr.Zero Then
                    IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelPtr)
                End If

                Dim gyroPtr = OakDNative.OakDGyro(cPtr)
                If gyroPtr <> IntPtr.Zero Then
                    IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroPtr)
                End If

                IMU_FrameTime = OakDNative.OakDIMUTimeStamp(cPtr)
            End SyncLock

            MyBase.GetNextFrameCounts()
        End Sub

        Public Overrides Sub StopCamera()
            If cPtr <> IntPtr.Zero Then
                OakDNative.OakDStop(cPtr)
                cPtr = IntPtr.Zero
            End If
        End Sub
    End Class
End Namespace
