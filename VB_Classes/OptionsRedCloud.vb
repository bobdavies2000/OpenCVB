Imports System.Windows.Controls
Imports cv = OpenCvSharp
Public Class OptionsRedCloud
    Private Sub OptionsRedCloud_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = allOptions
        Me.Text = "Options for RedCloud_Basics and related algorithms."

        ' The following lines control the pointcloud histograms for X and Y, and the camera location.

        ' The specification for each camera spells out the FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the specification FOV
        Select Case task.cameraName
            Case "Azure Kinect 4K"
                task.xRange = 4.4
                task.yRange = 1.5
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
                If task.workingRes.Height = 480 Or task.workingRes.Height = 240 Or task.workingRes.Height = 120 Then
                    task.xRange = 1.38
                    task.yRange = 1.0
                Else
                    task.xRange = 2.5
                    task.yRange = 0.8
                End If
            Case "Intel(R) RealSense(TM) Depth Camera 455"
                If task.workingRes.Height = 480 Or task.workingRes.Height = 240 Or task.workingRes.Height = 120 Then
                    task.xRange = 2.04
                    task.yRange = 2.14
                Else
                    task.xRange = 3.22
                    task.yRange = 1.39
                End If
            Case "Oak-D camera"
                task.xRange = 4.07
                task.yRange = 1.32
            Case "StereoLabs ZED 2/2i"
                task.xRange = 4
                task.yRange = 1.5
            Case "MYNT-EYE-D1000"
                task.xRange = 3.5
                task.yRange = 1.5
        End Select

        XRangeSlider.Value = task.xRange * 100
        YRangeSlider.Value = task.yRange * 100

        task.xRangeDefault = task.xRange
        task.yRangeDefault = task.yRange

        task.sideCameraPoint = New cv.Point(0, CInt(task.workingRes.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(task.workingRes.Width / 2), 0)

        task.channelsTop = {2, 0}
        task.channelsSide = {1, 2}

        Me.Left = 0
        Me.Top = 0
    End Sub
    Private Sub HistBinSlider_ValueChanged(sender As Object, e As EventArgs) Handles HistBinSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Public Sub Sync()
        histBins.Text = CStr(HistBinSlider.Value)

        task.maxZmeters = gOptions.MaxDepth.Value + 0.01 ' why add a cm?  Because histograms are exclusive on ranges.

        task.rangesTop = New cv.Rangef() {New cv.Rangef(0.1, task.maxZmeters), New cv.Rangef(-task.xRange, task.xRange)}
        task.rangesSide = New cv.Rangef() {New cv.Rangef(-task.yRange, task.yRange), New cv.Rangef(0.1, task.maxZmeters)}

        task.sideCameraPoint = New cv.Point(0, CInt(task.workingRes.Height / 2))
        task.topCameraPoint = New cv.Point(CInt(task.workingRes.Width / 2), 0)

        task.xRange = XRangeSlider.Value / 100 + 0.01 ' why add a cm?  Because histograms are exclusive on ranges.
        task.xRange = YRangeSlider.Value / 100 + 0.01 ' why add a cm?  Because histograms are exclusive on ranges.

        task.redThresholdSide = SideViewThreshold.Value
        task.redThresholdTop = TopViewThreshold.Value
    End Sub
    Private Sub XRangeSlider_ValueChanged(sender As Object, e As EventArgs) Handles XRangeSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub YRangeSlider_ValueChanged(sender As Object, e As EventArgs) Handles XRangeSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub SideViewThreshold_ValueChanged(sender As Object, e As EventArgs) Handles SideViewThreshold.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub TopViewThreshold_ValueChanged(sender As Object, e As EventArgs) Handles SideViewThreshold.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
End Class