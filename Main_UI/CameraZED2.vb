Imports cv = OpenCvSharp
Imports sl
Public Class CameraZED2 : Inherits GenericCamera
    Dim zed As sl.Camera
    Dim init_params As New InitParameters()
    Public Sub New(WorkingRes As cv.Size, _captureRes As cv.Size, deviceName As String)
        captureRes = _captureRes
        init_params.sensorsRequired = True
        init_params.depthMode = sl.DEPTH_MODE.ULTRA
        init_params.coordinateSystem = sl.COORDINATE_SYSTEM.IMAGE
        init_params.coordinateUnits = sl.UNIT.METER
        init_params.cameraFPS = 0

        If captureRes.Height = 720 Then init_params.resolution = sl.RESOLUTION.HD720
        If captureRes.Height = 1080 Then init_params.resolution = sl.RESOLUTION.HD1080
        If captureRes.Height = 1200 Then init_params.resolution = sl.RESOLUTION.HD720
        If captureRes.Height = 600 Then init_params.resolution = sl.RESOLUTION.HDSVGA
        If captureRes.Height = 376 Then init_params.resolution = sl.RESOLUTION.VGA

        zed = New sl.Camera(0)
        Dim errCode = zed.Open(init_params)

        Dim camInfo As sl.CameraInformation = zed.GetCameraInformation

        ' stereolabs left camera is the RGB camera so alignment to depth and left camera is already done.
        calibData.baseline = camInfo.cameraConfiguration.calibrationParameters.Trans.X
        calibData.rgbIntrinsics.fx = camInfo.cameraConfiguration.calibrationParameters.leftCam.fx
        calibData.rgbIntrinsics.fy = camInfo.cameraConfiguration.calibrationParameters.leftCam.fy
        calibData.rgbIntrinsics.ppx = camInfo.cameraConfiguration.calibrationParameters.leftCam.cx
        calibData.rgbIntrinsics.ppy = camInfo.cameraConfiguration.calibrationParameters.leftCam.cy
        calibData.h_fov = camInfo.cameraConfiguration.calibrationParameters.leftCam.hFOV
        calibData.v_fov = camInfo.cameraConfiguration.calibrationParameters.leftCam.vFOV

        Dim ratio = CInt(captureRes.Width / WorkingRes.Width)
        calibData.rgbIntrinsics.fx /= ratio
        calibData.rgbIntrinsics.fy /= ratio
        calibData.rgbIntrinsics.ppx /= ratio
        calibData.rgbIntrinsics.ppy /= ratio

        Dim posTrack As New sl.PositionalTrackingParameters
        posTrack.enableAreaMemory = True
        zed.EnablePositionalTracking(posTrack)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cv.Size)
        Static RuntimeParameters = New RuntimeParameters()
        Dim rows = captureRes.Height, cols = captureRes.Width
        Dim w = WorkingRes.Width, h = WorkingRes.Height
        While 1
            Dim rc = zed.Grab(RuntimeParameters)
            If rc = 0 Then Exit While
        End While

        Dim color As New cv.Mat, leftView As New cv.Mat, rightView As New cv.Mat, pointCloud As New cv.Mat
        Static colorSL As New sl.Mat(New sl.ResolutionStruct(rows, cols), sl.MAT_TYPE.MAT_8U_C3)
        Static rightSL As New sl.Mat(New sl.ResolutionStruct(rows, cols), sl.MAT_TYPE.MAT_8U_C3)
        Static pointCloudSL As New sl.Mat(New sl.ResolutionStruct(rows, cols), sl.MAT_TYPE.MAT_8U_C4)

        zed.RetrieveImage(colorSL, sl.VIEW.LEFT)
        color = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC4, colorSL.GetPtr).
                                      CvtColor(cv.ColorConversionCodes.BGRA2BGR)
        leftView = color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        zed.RetrieveImage(rightSL, sl.VIEW.RIGHT)
        rightView = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_8UC4, rightSL.GetPtr).
                                          CvtColor(cv.ColorConversionCodes.BGRA2BGR)
        rightView = rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        zed.RetrieveMeasure(pointCloudSL, sl.MEASURE.XYZBGRA) ' tried XYZ but it still comes with BGRA
        pointCloud = cv.Mat.FromPixelData(rows, cols, cv.MatType.CV_32FC4,
                                           pointCloudSL.GetPtr).CvtColor(cv.ColorConversionCodes.BGRA2BGR)
        cv.Cv2.PatchNaNs(pointCloud) ' This should not be necessary!  What is going on with StereoLabs interface?

        Dim zed_pose As New sl.Pose
        zed.GetPosition(zed_pose, REFERENCE_FRAME.WORLD)
        Dim sensordata As New sl.SensorsData
        zed.GetSensorsData(sensordata, TIME_REFERENCE.CURRENT)

        SyncLock cameraLock
            Dim acc = sensordata.imu.linearAcceleration
            If acc.X <> 0 And acc.Y <> 0 And acc.Z <> 0 Then
                IMU_Acceleration = New cv.Point3f(acc.X, acc.Y, -acc.Z)
                Dim gyro = sensordata.imu.angularVelocity
                IMU_AngularVelocity = New cv.Point3f(gyro.X, gyro.Y, gyro.Z) * 0.0174533 ' Zed 2 gyro is in degrees/sec 
                Static IMU_StartTime = sensordata.imu.timestamp
                IMU_TimeStamp = (sensordata.imu.timestamp - IMU_StartTime) / 4000000 ' crude conversion to milliseconds.
            End If
            If WorkingRes <> captureRes Then
                uiColor = color.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiLeft = leftView.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiRight = rightView.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest)
                uiPointCloud = pointCloud.Resize(WorkingRes, 0, 0, cv.InterpolationFlags.Nearest).Clone
            Else
                uiColor = color.Clone
                uiLeft = leftView.Clone
                uiRight = rightView.Clone
                uiPointCloud = pointCloud.Clone
            End If
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
    Public Sub stopCamera()
        zed.Close()
    End Sub
End Class
