Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Threading
Imports cv = OpenCvSharp

Namespace MainApp
    ''' <summary>
    ''' Stereolabs ZED camera using the Stereolabs.zed NuGet package (sl.Camera).
    ''' VB cannot name sl.RESOLUTION vs sl.Resolution, so those values are set by reflection.
    ''' </summary>
    Public Class Camera_ZED : Inherits GenericCamera
        Private zed As sl.Camera
        Private runtimeParameters As sl.RuntimeParameters
        Private ReadOnly colorSL As sl.Mat
        Private ReadOnly leftGreySL As sl.Mat
        Private ReadOnly rightGreySL As sl.Mat
        Private ReadOnly pointCloudSL As sl.Mat
        Private ReadOnly captureRows As Integer
        Private ReadOnly captureCols As Integer
        Private imuStartTime As ULong
        Private opened As Boolean

        Shared Sub New()
            LoadZedNatives()
        End Sub

        Public Sub New(_workRes As cv.Size, _captureRes As cv.Size)
            LoadZedNatives()
            captureRes = _captureRes
            workRes = _workRes

            Dim initParams As New sl.InitParameters() With {
                .sensorsRequired = True,
                .depthMode = sl.DEPTH_MODE.ULTRA,
                .coordinateSystem = sl.COORDINATE_SYSTEM.IMAGE,
                .coordinateUnits = sl.UNIT.METER,
                .cameraFPS = 0
            }

            If captureRes.Height = 720 Then
                SetZedResolution(initParams, "HD720")
            ElseIf captureRes.Height = 1080 Then
                SetZedResolution(initParams, "HD1080")
            ElseIf captureRes.Height = 1200 Then
                SetZedResolution(initParams, "HD1200")
            ElseIf captureRes.Height = 600 Then
                SetZedResolution(initParams, "HDSVGA")
            ElseIf captureRes.Height = 376 Then
                initParams.cameraFPS = 100
                SetZedResolution(initParams, "VGA")
            End If

            zed = New sl.Camera(0)
            Dim err = zed.Open(initParams)
            If err <> sl.ERROR_CODE.SUCCESS Then
                Throw New InvalidOperationException("Failed to open Stereolabs ZED camera: " + err.ToString())
            End If
            opened = True

            captureCols = CInt(zed.ImageWidth)
            captureRows = CInt(zed.ImageHeight)

            Dim calib = zed.CalibrationParametersRectified

            Dim ratio = captureRes.Width \ workRes.Width
            If ratio < 1 Then ratio = 1

            calibData.rgbIntrinsics.fx = calib.leftCam.fx / ratio
            calibData.rgbIntrinsics.fy = calib.leftCam.fy / ratio
            calibData.rgbIntrinsics.ppx = calib.leftCam.cx / ratio
            calibData.rgbIntrinsics.ppy = calib.leftCam.cy / ratio
            calibData.leftIntrinsics = calibData.rgbIntrinsics

            calibData.rightIntrinsics.fx = calib.rightCam.fx / ratio
            calibData.rightIntrinsics.fy = calib.rightCam.fy / ratio
            calibData.rightIntrinsics.ppx = calib.rightCam.cx / ratio
            calibData.rightIntrinsics.ppy = calib.rightCam.cy / ratio

            baseline = calib.Trans.X
            calibData.baseline = baseline
            calibData.h_fov = calib.leftCam.hFOV
            calibData.v_fov = calib.leftCam.vFOV
            calibData.d_fov = calib.leftCam.dFOV

            ReDim calibData.LtoR_rotation(8)
            ReDim calibData.ColorToLeft_rotation(8)
            calibData.LtoR_rotation = {1, 0, 0, 0, 1, 0, 0, 0, 1}
            calibData.ColorToLeft_rotation = calibData.LtoR_rotation

            ReDim calibData.LtoR_translation(2)
            ReDim calibData.ColorToLeft_translation(2)
            calibData.LtoR_translation = {baseline, 0, 0}
            calibData.ColorToLeft_translation = {0, 0, 0}

            Dim posTrack As New sl.PositionalTrackingParameters() With {.enableAreaMemory = True}
            zed.EnablePositionalTracking(posTrack)

            colorSL = New sl.Mat()
            leftGreySL = New sl.Mat()
            rightGreySL = New sl.Mat()
            pointCloudSL = New sl.Mat()
            colorSL.Create(CUInt(captureCols), CUInt(captureRows), sl.MAT_TYPE.MAT_8U_C4, sl.MEM.CPU)
            leftGreySL.Create(CUInt(captureCols), CUInt(captureRows), sl.MAT_TYPE.MAT_8U_C1, sl.MEM.CPU)
            rightGreySL.Create(CUInt(captureCols), CUInt(captureRows), sl.MAT_TYPE.MAT_8U_C1, sl.MEM.CPU)
            pointCloudSL.Create(CUInt(captureCols), CUInt(captureRows), sl.MAT_TYPE.MAT_32F_C4, sl.MEM.CPU)

            runtimeParameters = New sl.RuntimeParameters()
            MyBase.prepImages()

            captureThread = New Thread(AddressOf CaptureFrames) With {.IsBackground = True, .Name = "CaptureThread_ZED"}
            captureThread.Start()
        End Sub

        Private Sub CaptureFrames()
            While isCapturing
                GetNextFrame()
            End While
        End Sub

        Public Sub GetNextFrame()
            If zed Is Nothing OrElse Not opened Then Return

            Dim rc As sl.ERROR_CODE
            Do
                If Not isCapturing Then Return
                rc = zed.Grab(runtimeParameters)
                If rc = sl.ERROR_CODE.SUCCESS Then Exit Do
                Thread.Sleep(1)
            Loop

            zed.RetrieveImage(colorSL, sl.VIEW.LEFT)
            zed.RetrieveImage(leftGreySL, sl.VIEW.LEFT_GREY)
            zed.RetrieveImage(rightGreySL, sl.VIEW.RIGHT_GREY)
            zed.RetrieveMeasure(pointCloudSL, sl.MEASURE.XYZ)

            Dim colorBgra = cv.Mat.FromPixelData(captureRows, captureCols, cv.MatType.CV_8UC4, colorSL.GetPtr(sl.MEM.CPU))
            Dim leftGray = cv.Mat.FromPixelData(captureRows, captureCols, cv.MatType.CV_8UC1, leftGreySL.GetPtr(sl.MEM.CPU))
            Dim rightGray = cv.Mat.FromPixelData(captureRows, captureCols, cv.MatType.CV_8UC1, rightGreySL.GetPtr(sl.MEM.CPU))
            Dim pc4 = cv.Mat.FromPixelData(captureRows, captureCols, cv.MatType.CV_32FC4, pointCloudSL.GetPtr(sl.MEM.CPU))

            Dim bgr As New cv.Mat()
            cv.Cv2.CvtColor(colorBgra, bgr, cv.ColorConversionCodes.BGRA2BGR)

            Dim pc3 As New cv.Mat()
            Dim xyz() As cv.Mat = cv.Cv2.Split(pc4)
            cv.Cv2.Merge({xyz(0), xyz(1), xyz(2)}, pc3)
            For Each ch In xyz
                ch.Dispose()
            Next

            Dim sensors As New sl.SensorsData()
            zed.GetSensorsData(sensors, sl.TIME_REFERENCE.CURRENT)
            Dim acc = sensors.imu.linearAcceleration
            If Not Single.IsNaN(acc.X) AndAlso acc.X <> 0 AndAlso
               Not Single.IsNaN(acc.Y) AndAlso acc.Y <> 0 AndAlso
               Not Single.IsNaN(acc.Z) AndAlso acc.Z <> 0 Then
                IMU_Acceleration = New cv.Point3f(acc.X, acc.Y, -acc.Z)
                Dim gyro = sensors.imu.angularVelocity
                IMU_AngularVelocity = New cv.Point3f(gyro.X, gyro.Y, gyro.Z) * 0.0174533F
                If imuStartTime = 0UL Then imuStartTime = sensors.imu.timestamp
                IMU_TimeStamp = CDbl(sensors.imu.timestamp - imuStartTime) / 4000000.0
                IMU_FrameTime = IMU_TimeStamp
            End If

            SyncLock cameraMutex
                cv.Cv2.Resize(bgr, color, workRes, 0, 0, cv.InterpolationFlags.Nearest)
                cv.Cv2.Resize(leftGray, leftView, workRes, 0, 0, cv.InterpolationFlags.Nearest)
                cv.Cv2.Resize(rightGray, rightView, workRes, 0, 0, cv.InterpolationFlags.Nearest)
                cv.Cv2.Resize(pc3, pointCloud, workRes, 0, 0, cv.InterpolationFlags.Nearest)
            End SyncLock

            bgr.Dispose()
            pc3.Dispose()
            colorBgra.Dispose()
            leftGray.Dispose()
            rightGray.Dispose()
            pc4.Dispose()

            MyBase.GetNextFrameCounts()
        End Sub

        Public Overrides Sub StopCamera()
            opened = False
            If zed IsNot Nothing Then
                Try
                    colorSL?.Free(sl.MEM.CPU)
                    leftGreySL?.Free(sl.MEM.CPU)
                    rightGreySL?.Free(sl.MEM.CPU)
                    pointCloudSL?.Free(sl.MEM.CPU)
                    zed.Close()
                Catch
                End Try
                zed = Nothing
            End If
        End Sub

        Private Shared Sub SetZedResolution(initParams As sl.InitParameters, enumName As String)
            Dim field = GetType(sl.InitParameters).GetField("resolution")
            field.SetValue(initParams, [Enum].Parse(field.FieldType, enumName))
        End Sub

        Private Shared nativesLoaded As Boolean
        Private Shared ReadOnly nativesLock As New Object()

        Private Shared Sub LoadZedNatives()
            If nativesLoaded Then Return
            SyncLock nativesLock
                If nativesLoaded Then Return

                Dim baseDir = AppContext.BaseDirectory
                Dim extras As New List(Of String) From {baseDir}
                Dim cuda = Environment.GetEnvironmentVariable("CUDA_PATH")
                If String.IsNullOrEmpty(cuda) Then
                    cuda = Environment.GetEnvironmentVariable("CUDA_PATH", EnvironmentVariableTarget.Machine)
                End If
                If Not String.IsNullOrEmpty(cuda) Then extras.Add(Path.Combine(cuda, "bin"))
                extras.Add("C:\Program Files (x86)\ZED SDK\bin")
                extras.Add("C:\Program Files\ZED SDK\bin")

                Dim sl64Path As String = Nothing
                For Each searchDir In extras
                    Dim candidate = Path.Combine(searchDir, "sl_zed64.dll")
                    If File.Exists(candidate) Then
                        sl64Path = candidate
                        Exit For
                    End If
                Next
                If sl64Path IsNot Nothing Then NativeLibrary.Load(sl64Path)

                Dim slc = "sl_zed_c.dll"
                NativeLibrary.Load(slc)
                nativesLoaded = True
            End SyncLock
        End Sub
    End Class
End Namespace
