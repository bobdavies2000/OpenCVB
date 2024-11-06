Imports cvb = OpenCvSharp
Imports sl
Public Class CameraZED2 : Inherits GenericCamera
    Dim zed As sl.Camera
    Dim init_params As New InitParameters()
    Public Sub New(WorkingRes As cvb.Size, _captureRes As cvb.Size, deviceName As String)
        captureRes = _captureRes
        init_params.cameraFPS = 0

        init_params.sensorsRequired = True
        init_params.depthMode = sl.DEPTH_MODE.ULTRA
        init_params.coordinateSystem = sl.COORDINATE_SYSTEM.IMAGE
        init_params.coordinateUnits = sl.UNIT.METER

        If captureRes.Height = 720 Then init_params.resolution = sl.RESOLUTION.HD720
        If captureRes.Height = 1080 Then init_params.resolution = sl.RESOLUTION.HD1080
        If captureRes.Height = 1200 Then init_params.resolution = sl.RESOLUTION.HD720
        If captureRes.Height = 600 Then init_params.resolution = sl.RESOLUTION.HDSVGA
        If captureRes.Height = 376 Then init_params.resolution = sl.RESOLUTION.VGA

        zed = New sl.Camera(0)
        Dim errCode = zed.Open(init_params)

        Dim camInfo As sl.CameraInformation = zed.GetCameraInformation

        cameraInfo.ppx = camInfo.cameraConfiguration.calibrationParameters.leftCam.cx
        cameraInfo.ppy = camInfo.cameraConfiguration.calibrationParameters.leftCam.cy
        cameraInfo.fx = camInfo.cameraConfiguration.calibrationParameters.leftCam.fx
        cameraInfo.fy = camInfo.cameraConfiguration.calibrationParameters.leftCam.fy

        Dim posTrack As New sl.PositionalTrackingParameters
        posTrack.enableAreaMemory = True
        zed.EnablePositionalTracking(posTrack)
    End Sub
    Public Sub GetNextFrame(WorkingRes As cvb.Size)
        Static RuntimeParameters = New RuntimeParameters()
        Dim rows = captureRes.Height, cols = captureRes.Width
        Dim w = WorkingRes.Width, h = WorkingRes.Height
        While 1
            Dim rc = zed.Grab(RuntimeParameters)
            If rc = 0 Then Exit While
        End While

        Dim color As New cvb.Mat, leftView As New cvb.Mat, rightView As New cvb.Mat, pointCloud As New cvb.Mat
        Static colorSL As New sl.Mat(New sl.ResolutionStruct(rows, cols), sl.MAT_TYPE.MAT_8U_C3)
        Static rightSL As New sl.Mat(New sl.ResolutionStruct(rows, cols), sl.MAT_TYPE.MAT_8U_C3)
        Static pointCloudSL As New sl.Mat(New sl.ResolutionStruct(rows, cols), sl.MAT_TYPE.MAT_8U_C3)

        zed.RetrieveImage(colorSL, sl.VIEW.LEFT)
        color = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC4, colorSL.GetPtr).
                                      CvtColor(cvb.ColorConversionCodes.BGRA2BGR)

        zed.RetrieveImage(rightSL, sl.VIEW.RIGHT)
        rightView = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_8UC4, rightSL.GetPtr).
                                          CvtColor(cvb.ColorConversionCodes.BGRA2BGR)

        zed.RetrieveMeasure(pointCloudSL, sl.MEASURE.XYZ)
        pointCloud = cvb.Mat.FromPixelData(rows, cols, cvb.MatType.CV_32FC4,
                                           pointCloudSL.GetPtr).CvtColor(cvb.ColorConversionCodes.BGRA2BGR)
        cvb.Cv2.PatchNaNs(pointCloud, 0)
        Dim infNans As cvb.Mat = pointCloud.Reshape(1, pointCloud.Rows * pointCloud.Cols)
        Dim mask As cvb.Mat = infNans.Equals(Single.PositiveInfinity)
        mask = mask.Reshape(3, pointCloud.Rows)
        pointCloud.SetTo(0, mask)

        Dim zed_pose As New sl.Pose
        zed.GetPosition(zed_pose, REFERENCE_FRAME.WORLD)
        Dim sensordata As New sl.SensorsData
        zed.GetSensorsData(sensordata, TIME_REFERENCE.CURRENT)

        SyncLock cameraLock
            Dim acc = sensordata.imu.linearAcceleration
            If acc.X <> 0 And acc.Y <> 0 And acc.Z <> 0 Then
                IMU_Acceleration = New cvb.Point3f(acc.X, acc.Y, -acc.Z)
                Dim gyro = sensordata.imu.angularVelocity
                IMU_AngularVelocity = New cvb.Point3f(gyro.X, gyro.Y, gyro.Z) * 0.0174533 ' Zed 2 gyro is in degrees/sec 
                Static IMU_StartTime = sensordata.imu.timestamp
                IMU_TimeStamp = (sensordata.imu.timestamp - IMU_StartTime) / 4000000 ' crude conversion to milliseconds.
            End If
            If WorkingRes <> captureRes Then
                uiColor = color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                uiLeft = color.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                uiRight = rightView.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest)
                uiPointCloud = pointCloud.Resize(WorkingRes, 0, 0, cvb.InterpolationFlags.Nearest).Clone
            Else
                uiColor = color.Clone
                uiLeft = color.Clone
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
