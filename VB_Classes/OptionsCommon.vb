Imports cv = OpenCvSharp
Public Class OptionsCommon_Depth
    Inherits VBparent
    Public gOptions As New OptionsGlobal
    Public Sub New()
        initParent()
        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone.
        standalone = False

        gOptions = New OptionsGlobal
        gOptions.Show()

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Threshold in camera motion in radians X100", 1, 15, 1) ' how much motion is reasonable?

        task.cameraStableSlider = sliders.trackbar(0)

        label1 = "Depth values that are in-range"
        label2 = "Depth values that are out of range (and < 8m)"
        task.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        task.task.hist3DThreshold = gOptions.thresholdSlider.Value

        task.minDepth = gOptions.MinRange.Value
        task.maxDepth = gOptions.MaxRange.Value
        If task.minDepth >= task.maxDepth Then task.maxDepth = task.minDepth + 1

        task.maxZ = task.maxDepth / 1000

        Static saveMaxVal As Integer
        Static saveMinVal As Integer
        If saveMaxVal <> task.maxDepth Or saveMinVal <> task.minDepth Then
            task.depthOptionsChanged = True
            saveMaxVal = task.maxDepth
            saveMinVal = task.minDepth
        Else
            task.depthOptionsChanged = False
        End If

        If task.depth32f.Size <> src.Size Then task.depth32f = task.depth32f.Resize(src.Size, 0, 0, cv.InterpolationFlags.Nearest)
        If task.pointCloud.Size <> src.Size Then task.pointCloud = task.pointCloud.Resize(src.Size, 0, 0, cv.InterpolationFlags.Nearest)
        cv.Cv2.InRange(task.depth32f, task.minDepth, task.maxDepth, task.depthMask)
        cv.Cv2.BitwiseNot(task.depthMask, task.noDepthMask)
        dst1 = task.depth32f.SetTo(0, task.noDepthMask)
        If task.pointCloud.Width = task.noDepthMask.Width Then task.pointCloud.SetTo(0, task.noDepthMask) ' reflect the range bounds into the task.pointcloud as well.
    End Sub
End Class







Public Class OptionsCommon_LineType
    Inherits VBparent
    Public Sub New()
        initParent()
        task.callTrace.Clear() ' special line to clear the tree view otherwise OptionsCommon_LineType is standalone.
        standalone = False

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "AntiAlias"
            radio.check(1).Text = "Link4"
            radio.check(2).Text = "Link8"
            radio.check(0).Checked = True
        End If
        task.desc = "Control the line type in use by all algorithms."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static AntiAliasRadio = findRadio("AntiAlias")
        Static Link4Radio = findRadio("Link4")
        Static Link8Radio = findRadio("Link8")
        If AntiAliasRadio.checked Then task.lineType = cv.LineTypes.AntiAlias
        If Link4Radio.checked Then task.lineType = cv.LineTypes.Link4
        If Link8Radio.checked Then task.lineType = cv.LineTypes.Link8
    End Sub
End Class







Public Class OptionsCommon_Histogram
    Inherits VBparent
    Dim sideFrustrumSlider As Windows.Forms.TrackBar
    Dim topFrustrumSlider As Windows.Forms.TrackBar
    Dim cameraYSlider As Windows.Forms.TrackBar
    Dim cameraXSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone.

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "SideView Frustrum adjustment", 1, 200, 57)
        sliders.setupTrackBar(1, "TopView Frustrum adjustment", 1, 200, 57)
        sliders.setupTrackBar(2, "SideCameraPoint adjustment", -100, 100, 0)
        sliders.setupTrackBar(3, "TopCameraPoint adjustment", -10, 10, 0)

        sideFrustrumSlider = sliders.trackbar(0) ' findSlider("SideView Frustrum adjustment")
        topFrustrumSlider = sliders.trackbar(1) 'findSlider("TopView Frustrum adjustment")
        cameraYSlider = sliders.trackbar(2) ' findSlider("SideCameraPoint adjustment")
        cameraXSlider = sliders.trackbar(3) 'findSlider("TopCameraPoint adjustment")

        ' The specification for each camera spells out the FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the spec.
        Select Case task.parms.cameraName
            Case VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
                sideFrustrumSlider.Value = 58
                topFrustrumSlider.Value = 180
                cameraXSlider.Value = 0
                cameraYSlider.Value = If(task.resolutionIndex = 1, -1, -2)
            Case VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2
                sideFrustrumSlider.Value = 53
                topFrustrumSlider.Value = 162
                cameraXSlider.Value = If(task.resolutionIndex = 3, 38, 13)
                cameraYSlider.Value = -3
            Case VB_Classes.ActiveTask.algParms.camNames.MyntD1000
                sideFrustrumSlider.Value = 50
                topFrustrumSlider.Value = 105
                cameraXSlider.Value = If(task.resolutionIndex = 1, 4, 8)
                cameraYSlider.Value = If(task.resolutionIndex = 3, -8, -3)
            Case VB_Classes.ActiveTask.algParms.camNames.D435i
                If src.Width = 640 Then
                    sideFrustrumSlider.Value = 75
                    topFrustrumSlider.Value = 101
                    cameraXSlider.Value = 0
                    cameraYSlider.Value = 0
                Else
                    sideFrustrumSlider.Value = 57
                    topFrustrumSlider.Value = 175
                    cameraXSlider.Value = 0
                    cameraYSlider.Value = 0
                End If
            Case VB_Classes.ActiveTask.algParms.camNames.D455
                If src.Width = 640 Then
                    sideFrustrumSlider.Value = 86
                    topFrustrumSlider.Value = 113
                    cameraXSlider.Value = 1
                    cameraYSlider.Value = -1
                Else
                    sideFrustrumSlider.Value = 58
                    topFrustrumSlider.Value = 184
                    cameraXSlider.Value = 0
                    cameraYSlider.Value = -3
                End If
        End Select

        task.sideFrustrumAdjust = task.maxZ * sideFrustrumSlider.Value / 100 / 2
        task.topFrustrumAdjust = task.maxZ * topFrustrumSlider.Value / 100 / 2
        task.sideCameraPoint = New cv.Point(0, CInt(src.Height / 2 + cameraYSlider.Value))
        task.topCameraPoint = New cv.Point(CInt(src.Width / 2 + cameraXSlider.Value), CInt(src.Height))
        sliders.Hide()
        task.desc = "The options for the side view are shared with this algorithm"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        task.sideFrustrumAdjust = task.maxZ * sideFrustrumSlider.Value / 100 / 2
        task.topFrustrumAdjust = task.maxZ * topFrustrumSlider.Value / 100 / 2
        task.sideCameraPoint = New cv.Point(0, CInt(src.Height / 2 + cameraYSlider.Value))
        task.topCameraPoint = New cv.Point(CInt(src.Width / 2 + cameraXSlider.Value), CInt(src.Height))

        If sliders.Visible = False Then
            task.trueText("This algorithm was created to tune the frustrum and camera locations." + vbCrLf +
                          "Without these tuning parameters the side and top views would not be correct." + vbCrLf +
                          "To see how these adjustments work and to add a new camera, " + vbCrLf +
                          "use the Histogram_TopView2D or Histogram_SideView2D algorithms." + vbCrLf +
                          "For new cameras, make the adjustments needed, note the value, and update " + vbCrLf +
                          "the Select statement in the constructor for OptionsCommon_Histogram.")
        End If
    End Sub
End Class