Imports cv = OpenCvSharp
Public Class OptionsCommon_Depth : Inherits VBparent
    Public gOptions As New OptionsGlobal
    Public Sub New()
        gOptions = New OptionsGlobal
        gOptions.Show()

        task.palette = New Palette_Basics
        task.histogramBins = gOptions.HistBinSlider.Value
        updateSettings()
        labels(2) = "Depth values that are in-range"
        labels(3) = "Depth values that are out of range (and < 8m)"
        task.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Private Sub updateSettings()
        task.hist3DThreshold = gOptions.ProjectionSlider.Value
        gOptions.ProjectionThreshold.Text = CStr(task.hist3DThreshold)
        task.useKalman = gOptions.UseKalman.Checked
        task.useKalmanWhenStable = gOptions.UseKalmanWhenStable.Checked
        task.paletteScheme = gOptions.scheme
        task.paletteSchemeName = gOptions.schemeName

        task.histogramBins = gOptions.HistBinSlider.Value
        task.cameraMotionLimit = gOptions.IMUmotionSlider.Value / 100
        task.cameraLevelLimit = gOptions.IMUlevelSlider.Value / 10
        task.lineWidth = gOptions.LineThickness.Value
        task.dotSize = gOptions.dotSizeSlider.Value
        task.fontSize = gOptions.fontSizeSlider.Value / 10
        task.AddWeighted = gOptions.AddWeightedSlider.Value / 100

        task.minDepth = 1
        task.maxDepth = gOptions.MaxRange.Value
        If task.minDepth >= task.maxDepth Then task.maxDepth = task.minDepth + 1
        task.maxZ = task.maxDepth / 1000
        task.maxY = task.maxZ * task.viewOptions.sideFrustrumSetting / 100 / 2
        task.maxX = task.maxZ * task.viewOptions.topFrustrumSetting / 100 / 2
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        updateSettings()

        Static saveMaxVal As Integer
        Static saveMinVal As Integer
        If saveMaxVal <> task.maxDepth Or saveMinVal <> task.minDepth Then
            task.depthOptionsChanged = True
            saveMaxVal = task.maxDepth
            saveMinVal = task.minDepth
        Else
            task.depthOptionsChanged = False
        End If

        task.lineType = cv.LineTypes.AntiAlias ' cv.LineTypes.Link4 or cv.LineTypes.Link8

        If task.depth32f.Size <> task.color.Size Then task.depth32f = task.depth32f.Resize(task.color.Size, 0, 0, cv.InterpolationFlags.Nearest)
        If task.pointCloud.Size <> task.color.Size Then task.pointCloud = task.pointCloud.Resize(task.color.Size, 0, 0, cv.InterpolationFlags.Nearest)
        cv.Cv2.InRange(task.depth32f, task.minDepth, task.maxDepth, task.depthMask)
        cv.Cv2.BitwiseNot(task.depthMask, task.noDepthMask)
        task.depth32f.SetTo(0, task.noDepthMask)
        If task.pointCloud.Width = task.noDepthMask.Width Then task.pointCloud.SetTo(0, task.noDepthMask) ' reflect the range bounds into the task.pointcloud as well.
    End Sub
End Class







Public Class OptionsCommon_Histogram : Inherits VBparent
    Public sideFrustrumSetting = 57
    Public topFrustrumSetting = 57
    Public cameraYSetting = 0
    Public cameraXSetting = 0
    Dim tweakSettings As Boolean = False ' set to true to add a new camera.
    Public Sub New()
        ' The specification for each camera spells out the FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the specification FOV
        Select Case task.parms.cameraName
            Case VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
                sideFrustrumSetting = 58
                topFrustrumSetting = 180
                cameraXSetting = 0
                cameraYSetting = If(task.resolutionIndex = 1, -1, -2)
            Case VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2
                sideFrustrumSetting = 53
                topFrustrumSetting = 162
                cameraXSetting = If(task.resolutionIndex = 3, 38, 13)
                cameraYSetting = -3
            Case VB_Classes.ActiveTask.algParms.camNames.MyntD1000
                sideFrustrumSetting = 50
                topFrustrumSetting = 105
                cameraXSetting = If(task.resolutionIndex = 1, 4, 8)
                cameraYSetting = If(task.resolutionIndex = 3, -8, -3)
            Case VB_Classes.ActiveTask.algParms.camNames.OakDCamera
                sideFrustrumSetting = 50
                topFrustrumSetting = 105
                cameraXSetting = If(task.resolutionIndex = 1, 4, 8)
                cameraYSetting = If(task.resolutionIndex = 3, -8, -3)
            Case VB_Classes.ActiveTask.algParms.camNames.D435i
                If dst2.Width = 640 Then
                    sideFrustrumSetting = 76
                    topFrustrumSetting = 132
                    cameraXSetting = -3
                    cameraYSetting = 2
                Else
                    sideFrustrumSetting = 56
                    topFrustrumSetting = 175
                    cameraXSetting = 0
                    cameraYSetting = 4
                End If
            Case VB_Classes.ActiveTask.algParms.camNames.D455
                If dst2.Width = 640 Then
                    sideFrustrumSetting = 74
                    topFrustrumSetting = 134
                    cameraXSetting = 3
                    cameraYSetting = -4
                Else
                    sideFrustrumSetting = 55
                    topFrustrumSetting = 180
                    cameraXSetting = 0
                    cameraYSetting = -3
                End If
        End Select

        If standalone Or tweakSettings Then
            If sliders.Setup(caller) Then
                sliders.setupTrackBar(0, "Adjust the top frustrum angle (hFOV)", 0, 200, topFrustrumSetting)
                sliders.setupTrackBar(1, "Adjust the side frustrum angle (vFOV)", 0, 100, sideFrustrumSetting)
                sliders.setupTrackBar(2, "Adjust the top camera X-coordinate", -100, 100, cameraXSetting)
                sliders.setupTrackBar(3, "Adjust the side camera Y-coordinate", -100, 100, cameraYSetting)
            End If
        End If

        task.sideCameraPoint = New cv.Point(0, CInt(dst2.Height / 2 + cameraYSetting))
        task.topCameraPoint = New cv.Point(CInt(dst2.Width / 2 + cameraXSetting), CInt(dst2.Height))
        task.desc = "The options for the side view are shared with this algorithm"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone Or tweakSettings Then
            Static topSlider = findSlider("Adjust the top frustrum angle (hFOV)")
            Static sideSlider = findSlider("Adjust the side frustrum angle (vFOV)")
            Static xSlider = findSlider("Adjust the top camera X-coordinate")
            Static ySlider = findSlider("Adjust the side camera Y-coordinate")
            sideFrustrumSetting = sideSlider.value
            topFrustrumSetting = topSlider.value
            cameraXSetting = xSlider.value
            cameraYSetting = ySlider.value
        End If
        task.sideCameraPoint = New cv.Point(0, CInt(dst2.Height / 2 + cameraYSetting))
        task.topCameraPoint = New cv.Point(CInt(dst2.Width / 2 + cameraXSetting), CInt(dst2.Height))
    End Sub
End Class
