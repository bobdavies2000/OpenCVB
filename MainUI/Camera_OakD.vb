Imports System.Runtime.InteropServices
Imports System.Threading
Imports cv = OpenCvSharp

Namespace MainUI
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

        Private initialTime As Double = 0

        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size, deviceName As String)
            captureRes = _captureRes
            workRes = _workRes

            ' Open the Oak-D camera
            cPtr = OakDOpen(captureRes.Width, captureRes.Height)

            If cPtr = IntPtr.Zero Then
                Throw New Exception("Failed to open Oak-D camera")
            End If

            ' Get calibration data
            'Dim ratio = captureRes.Width \ workRes.Width
            'Dim intrinsicsPtr = OakDintrinsics(cPtr, 1) ' RGB camera
            'If intrinsicsPtr <> IntPtr.Zero Then
            '    Dim intrinsics(8) As Single
            '    Marshal.Copy(intrinsicsPtr, intrinsics, 0, 9)
            '    ' Intrinsics matrix: [fx, 0, cx; 0, fy, cy; 0, 0, 1]
            '    calibData.rgbIntrinsics.fx = intrinsics(0) / ratio
            '    calibData.rgbIntrinsics.ppx = intrinsics(2) / ratio
            '    calibData.rgbIntrinsics.fy = intrinsics(4) / ratio
            '    calibData.rgbIntrinsics.ppy = intrinsics(5) / ratio
            'End If

            'Dim leftIntrinsicsPtr = OakDintrinsics(cPtr, 2) ' Left camera
            'If leftIntrinsicsPtr <> IntPtr.Zero Then
            '    Dim intrinsics(8) As Single
            '    Marshal.Copy(leftIntrinsicsPtr, intrinsics, 0, 9)
            '    calibData.leftIntrinsics.fx = intrinsics(0) / ratio
            '    calibData.leftIntrinsics.ppx = intrinsics(2) / ratio
            '    calibData.leftIntrinsics.fy = intrinsics(4) / ratio
            '    calibData.leftIntrinsics.ppy = intrinsics(5) / ratio
            'End If

            'Dim rightIntrinsicsPtr = OakDintrinsics(cPtr, 3) ' Right camera
            'If rightIntrinsicsPtr <> IntPtr.Zero Then
            '    Dim intrinsics(8) As Single
            '    Marshal.Copy(rightIntrinsicsPtr, intrinsics, 0, 9)
            '    calibData.rightIntrinsics.fx = intrinsics(0) / ratio
            '    calibData.rightIntrinsics.ppx = intrinsics(2) / ratio
            '    calibData.rightIntrinsics.fy = intrinsics(4) / ratio
            '    calibData.rightIntrinsics.ppy = intrinsics(5) / ratio
            'End If

            '' Get extrinsics
            'Dim rgbToLeftPtr = OakDExtrinsicsRGBtoLeft(cPtr)
            'If rgbToLeftPtr <> IntPtr.Zero Then
            '    Dim extrinsics(11) As Single
            '    Marshal.Copy(rgbToLeftPtr, extrinsics, 0, 12)
            '    ReDim calibData.ColorToLeft_translation(2)
            '    ReDim calibData.ColorToLeft_rotation(8)
            '    For i = 0 To 2
            '        calibData.ColorToLeft_translation(i) = extrinsics(i)
            '    Next
            '    For i = 0 To 8
            '        calibData.ColorToLeft_rotation(i) = extrinsics(i + 3)
            '    Next
            'End If

            'Dim leftToRightPtr = OakDExtrinsicsLeftToRight(cPtr)
            'If leftToRightPtr <> IntPtr.Zero Then
            '    Dim extrinsics(11) As Single
            '    Marshal.Copy(leftToRightPtr, extrinsics, 0, 12)
            '    ReDim calibData.LtoR_translation(2)
            '    ReDim calibData.LtoR_rotation(8)
            '    For i = 0 To 2
            '        calibData.LtoR_translation(i) = extrinsics(i)
            '    Next
            '    For i = 0 To 8
            '        calibData.LtoR_rotation(i) = extrinsics(i + 3)
            '    Next

            '    ' Calculate baseline from translation vector
            '    calibData.baseline = CSng(Math.Sqrt(
            '        calibData.LtoR_translation(0) * calibData.LtoR_translation(0) +
            '        calibData.LtoR_translation(1) * calibData.LtoR_translation(1) +
            '        calibData.LtoR_translation(2) * calibData.LtoR_translation(2)))
            'End If

            'If calibData.baseline = 0 Then calibData.baseline = 0.075F ' Default Oak-D baseline ~7.5cm

            MyBase.prepImages()

            ' Start background thread to capture frames
            isCapturing = True
            captureThread = New Thread(AddressOf CaptureFrames)
            captureThread.IsBackground = True
            captureThread.Name = "OakD_CaptureThread"
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

            Try
                OakDWaitForFrame(cPtr)

                SyncLock cameraMutex
                    Dim colorPtr = OakDColor(cPtr)
                    If colorPtr <> IntPtr.Zero Then
                        Dim colorMat = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC3, colorPtr)
                        color = colorMat.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    End If

                    '' Get left image
                    Dim leftPtr = OakDLeftImage(cPtr)
                    If leftPtr <> IntPtr.Zero Then
                        Dim leftMat = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, leftPtr)
                        leftView = leftMat.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    End If

                    ' Get right image
                    Dim rightPtr = OakDRightImage(cPtr)
                    If rightPtr <> IntPtr.Zero Then
                        Dim rightMat = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC1, rightPtr)
                        rightView = rightMat.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    End If

                    '' Get depth and compute point cloud
                    'Dim depthPtr = OakDRawDepth(cPtr)
                    'If depthPtr <> IntPtr.Zero Then
                    '    Dim depth16 = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_16UC1, depthPtr)
                    '    pointCloud = ComputePointCloud(depth16, calibData.rgbIntrinsics)
                    '    pointCloud = pointCloud.Resize(workRes, 0, 0, cv.InterpolationFlags.Nearest)
                    'End If

                    '' Get IMU data
                    'Dim accelPtr = OakDAccel(cPtr)
                    'If accelPtr <> IntPtr.Zero Then
                    '    IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelPtr)
                    'End If

                    'Dim gyroPtr = OakDGyro(cPtr)
                    'If gyroPtr <> IntPtr.Zero Then
                    '    IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroPtr)
                    'End If

                    'Dim timeStamp = OakDIMUTimeStamp(cPtr)
                    'If initialTime = 0 Then initialTime = timeStamp
                    'IMU_FrameTime = timeStamp - initialTime
                End SyncLock

                MyBase.GetNextFrameCounts(IMU_FrameTime)

            Catch ex As Exception
                Debug.WriteLine("Error in GetNextFrame: " + ex.Message)
            End Try
        End Sub

        Private Function ComputePointCloud(depth16 As cv.Mat, intrinsics As intrinsicData) As cv.Mat
            ' Compute point cloud from depth image and camera intrinsics
            Dim rows = depth16.Rows
            Dim cols = depth16.Cols
            Dim pc = New cv.Mat(rows, cols, cv.MatType.CV_32FC3, New cv.Scalar(0, 0, 0))

            Dim fx = intrinsics.fx * (captureRes.Width / workRes.Width)
            Dim fy = intrinsics.fy * (captureRes.Height / workRes.Height)
            Dim cx = intrinsics.ppx * (captureRes.Width / workRes.Width)
            Dim cy = intrinsics.ppy * (captureRes.Height / workRes.Height)

            ' Use indexer for depth data
            Dim depthIndexer = depth16.GetGenericIndexer(Of UInt16)()
            Dim pcIndexer = pc.GetGenericIndexer(Of cv.Vec3f)()

            For y = 0 To rows - 1
                For x = 0 To cols - 1
                    Dim d = depthIndexer(y, x)
                    If d > 0 AndAlso d < 10000 Then ' Valid depth in mm
                        Dim z = d / 1000.0F ' Convert to meters
                        Dim px = (x - cx) * z / fx
                        Dim py = (y - cy) * z / fy
                        pcIndexer(y, x) = New cv.Vec3f(px, py, z)
                    End If
                Next
            Next

            Return pc
        End Function

        Public Overrides Sub StopCamera()
            If captureThread IsNot Nothing Then
                captureThread.Join(1000) ' Wait up to 1 second for thread to finish
                captureThread = Nothing
            End If

            ' Stop the camera asynchronously so it doesn't block the UI
            If cPtr <> IntPtr.Zero Then
                Dim ptrToStop = cPtr
                cPtr = IntPtr.Zero ' Clear reference so GetNextFrame won't try to use it

                ' Run OakDStop on a background thread so it doesn't block
                ThreadPool.QueueUserWorkItem(Sub(state)
                                                 Try
                                                     OakDStop(ptrToStop)
                                                 Catch ex As Exception
                                                     Debug.WriteLine("Error stopping Oak-D camera: " + ex.Message)
                                                 End Try
                                             End Sub)
            End If
        End Sub
    End Class
End Namespace

