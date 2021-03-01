Imports cv = OpenCvSharp
Public Class OptionsCommon_Depth
    Inherits VBparent
    Public depthMask As New cv.Mat
    Public noDepthMask As New cv.Mat
    Public minVal As Single
    Public maxVal As Single
    Public bins As Integer
    Public Sub New()
        initParent()
        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone.
        standalone = False

        sliders.Setup(caller, 6)
        sliders.setupTrackBar(0, "InRange Min Depth (mm)", 1, 2000, 200)
        sliders.setupTrackBar(1, "InRange Max Depth (mm)", 200, 15000, 4000)
        sliders.setupTrackBar(2, "Top and Side Views Histogram threshold", 0, 200, 2)
        sliders.setupTrackBar(3, "Amount to rotate pointcloud around X-axis (degrees)", -90, 90, 0)
        sliders.setupTrackBar(4, "Amount to rotate pointcloud around Y-axis (degrees)", -90, 90, 0)
        sliders.setupTrackBar(5, "Amount to rotate pointcloud around Z-axis (degrees)", -90, 90, 0)
        'sliders.setupTrackBar(6, "Number of depth frames to fuse (eliminates noise)", 1, 10, 2)

        task.minRangeSlider = sliders.trackbar(0) ' one of the few places we can be certain there is only one...
        task.maxRangeSlider = sliders.trackbar(1)
        task.thresholdSlider = sliders.trackbar(2)
        task.xRotateSlider = sliders.trackbar(3)
        task.yRotateSlider = sliders.trackbar(4)
        task.zRotateSlider = sliders.trackbar(5)
        ' task.fuseSlider = sliders.trackbar(6)

        label1 = "Depth values that are in-range"
        label2 = "Depth values that are out of range (and < 8m)"
        task.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        minVal = task.minRangeSlider.Value
        maxVal = task.maxRangeSlider.Value
        ocvb.maxZ = maxVal / 1000
        bins = task.thresholdSlider.Value
        If minVal >= maxVal Then maxVal = minVal + 1

        Static saveMaxVal As Integer
        Static saveMinVal As Integer
        Static saveYRotate As Integer
        If saveMaxVal <> maxVal Or saveMinVal <> minVal Or saveYRotate <> task.yRotateSlider.Value Then
            task.depthOptionsChanged = True
            saveMaxVal = maxVal
            saveMinVal = minVal
            saveYRotate = task.yRotateSlider.Value
        Else
            task.depthOptionsChanged = False
        End If

#If 0 Then
        Dim fuseCount = task.fuseSlider.Value
        Static saveFuseCount = fuseCount
        Static fuseFrames As New List(Of cv.Mat)
        If saveFuseCount <> fuseCount Then
            fuseFrames.clear()
            saveFuseCount = fuseCount
        End If

        fuseFrames.Add(task.depth32f.Clone)
        If fuseFrames.Count > fuseCount Then fuseFrames.RemoveAt(0)
        For i = 1 To fuseFrames.Count - 1
            cv.Cv2.Max(fuseFrames(i), task.depth32f, task.depth32f)
        Next
#End If

        cv.Cv2.InRange(task.depth32f, minVal, maxVal, depthMask)
        cv.Cv2.BitwiseNot(depthMask, noDepthMask)
        dst1 = task.depth32f.SetTo(0, noDepthMask)
        If task.pointCloud.Width = noDepthMask.Width Then task.pointCloud.SetTo(0, noDepthMask) ' reflect the range bounds into the task.pointcloud as well.
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
        Select Case ocvb.parms.cameraName
            Case VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
                sideFrustrumSlider.Value = 58
                topFrustrumSlider.Value = 180
                cameraXSlider.Value = 0
                cameraYSlider.Value = If(ocvb.resolutionIndex = 1, -1, -2)
            Case VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2
                sideFrustrumSlider.Value = 53
                topFrustrumSlider.Value = 162
                cameraXSlider.Value = If(ocvb.resolutionIndex = 3, 38, 13)
                cameraYSlider.Value = -3
            Case VB_Classes.ActiveTask.algParms.camNames.MyntD1000
                sideFrustrumSlider.Value = 50
                topFrustrumSlider.Value = 105
                cameraXSlider.Value = If(ocvb.resolutionIndex = 1, 4, 8)
                cameraYSlider.Value = If(ocvb.resolutionIndex = 3, -8, -3)
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

        ocvb.sideFrustrumAdjust = ocvb.maxZ * sideFrustrumSlider.Value / 100 / 2
        ocvb.topFrustrumAdjust = ocvb.maxZ * topFrustrumSlider.Value / 100 / 2
        ocvb.sideCameraPoint = New cv.Point(0, CInt(src.Height / 2 + cameraYSlider.Value))
        ocvb.topCameraPoint = New cv.Point(CInt(src.Width / 2 + cameraXSlider.Value), CInt(src.Height))
        sliders.Hide()
        task.desc = "The options for the side view are shared with this algorithm"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        ocvb.sideFrustrumAdjust = ocvb.maxZ * sideFrustrumSlider.Value / 100 / 2
        ocvb.topFrustrumAdjust = ocvb.maxZ * topFrustrumSlider.Value / 100 / 2
        ocvb.sideCameraPoint = New cv.Point(0, CInt(src.Height / 2 + cameraYSlider.Value))
        ocvb.topCameraPoint = New cv.Point(CInt(src.Width / 2 + cameraXSlider.Value), CInt(src.Height))

        If sliders.Visible = False Then
            ocvb.trueText("This algorithm was created to tune the frustrum and camera locations." + vbCrLf +
                          "Without these tuning parameters the side and top views would not be correct." + vbCrLf +
                          "To see how these adjustments work and to add a new camera, " + vbCrLf +
                          "use the Histogram_TopView2D or Histogram_SideView2D algorithms." + vbCrLf +
                          "For new cameras, make the adjustments needed, note the value, and update " + vbCrLf +
                          "the Select statement in the constructor for OptionsCommon_Histogram.")
        End If
    End Sub
End Class