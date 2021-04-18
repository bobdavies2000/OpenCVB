Imports cv = OpenCvSharp
Public Class OptionsCommon : Inherits VBparent
    Public gOptions As New OptionsGlobal
    Public Sub New()
        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone.
        standalone = False

        gOptions = New OptionsGlobal
        gOptions.Show()

        task.palette = New Palette_Basics
        task.cameraStableSlider = gOptions.IMUmotionSlider
        task.histogramBins = gOptions.HistBinSlider.Value
        updateSettings()
        label1 = "Depth values that are in-range"
        label2 = "Depth values that are out of range (and < 8m)"
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

        task.minDepth = gOptions.MinRange.Value
        task.maxDepth = gOptions.MaxRange.Value
        If task.minDepth >= task.maxDepth Then task.maxDepth = task.minDepth + 1

        task.maxZ = task.maxDepth / 1000
        task.maxXY = task.maxZ * dst1.Height / dst1.Width / 2
    End Sub
    Public Sub Run(src As cv.Mat)
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

        If task.depth32f.Size <> src.Size Then task.depth32f = task.depth32f.Resize(src.Size, 0, 0, cv.InterpolationFlags.Nearest)
        If task.pointCloud.Size <> src.Size Then task.pointCloud = task.pointCloud.Resize(src.Size, 0, 0, cv.InterpolationFlags.Nearest)
        cv.Cv2.InRange(task.depth32f, task.minDepth, task.maxDepth, task.depthMask)
        cv.Cv2.BitwiseNot(task.depthMask, task.noDepthMask)
        dst1 = task.depth32f.SetTo(0, task.noDepthMask)
        If task.pointCloud.Width = task.noDepthMask.Width Then task.pointCloud.SetTo(0, task.noDepthMask) ' reflect the range bounds into the task.pointcloud as well.
    End Sub
End Class







Public Class OptionsCommon_Histogram : Inherits VBparent
    Public Sub New()
        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone.

        Dim sideFrustrumSetting = 57
        Dim topFrustrumSetting = 57
        Dim cameraYSetting = 0
        Dim cameraXSetting = 0

        ' The specification for each camera spells out the FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the spec.
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
            Case VB_Classes.ActiveTask.algParms.camNames.D435i
                If dst1.Width = 640 Then
                    sideFrustrumSetting = 75
                    topFrustrumSetting = 101
                    cameraXSetting = 0
                    cameraYSetting = 0
                Else
                    sideFrustrumSetting = 57
                    topFrustrumSetting = 175
                    cameraXSetting = 0
                    cameraYSetting = 0
                End If
            Case VB_Classes.ActiveTask.algParms.camNames.D455
                If dst1.Width = 640 Then
                    sideFrustrumSetting = 86
                    topFrustrumSetting = 113
                    cameraXSetting = 1
                    cameraYSetting = -1
                Else
                    sideFrustrumSetting = 58
                    topFrustrumSetting = 184
                    cameraXSetting = 0
                    cameraYSetting = -3
                End If
        End Select

        task.sideFrustrumAdjust = task.maxZ * sideFrustrumSetting / 100 / 2
        task.topFrustrumAdjust = task.maxZ * topFrustrumSetting / 100 / 2
        task.sideCameraPoint = New cv.Point(0, CInt(dst1.Height / 2 + cameraYSetting))
        task.topCameraPoint = New cv.Point(CInt(dst1.Width / 2 + cameraXSetting), CInt(dst1.Height))
        task.desc = "The options for the side view are shared with this algorithm"
    End Sub
    Public Sub Run(src as cv.Mat)
    End Sub
End Class
