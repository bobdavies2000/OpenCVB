﻿Imports cv = OpenCvSharp
Public Class OptionsAllAlgorithm
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public heartBeatSeconds = 1
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = allOptions

        ThreadGridSize.Text = CStr(GridSize.Value)

        dotSizeSlider.Value = 1
        LineWidth.Value = 1
        If task.workingRes.Width <= 320 Then
            dotSizeSlider.Value = 1
            LineWidth.Value = 1
        ElseIf task.workingRes.Width = 640 Then
            dotSizeSlider.Value = 2
            LineWidth.Value = 2
        End If
        AddWeightedSlider.Value = 50
        FrameHistory.Value = 10
        useHistoryCloud.Checked = True

        maxCount.Text = CStr(MaxDepth.Value)
        labelBinsCount.Text = CStr(HistBinSlider.Value)
        PixelDiff.Text = CStr(PixelDiffThreshold.Value)
        fHist.Text = CStr(FrameHistory.Value)
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        DotSizeLabel.Text = CStr(dotSizeSlider.Value)
        AddWeighted.Text = CStr(AddWeightedSlider.Value)

        UseKalman.Checked = True
        UseMultiThreading.Checked = False ' too many times it is just not faster.

        Palettes.Items.Clear()
        For Each mapName In mapNames
            Palettes.Items.Add(mapName)
        Next
        Palettes.SelectedIndex = mapNames.IndexOf("Jet")

        LineType.Items.Add("AntiAlias")
        LineType.Items.Add("Link4")
        LineType.Items.Add("Link8")
        LineType.SelectedIndex = 0

        RGBFilterList.Items.Add("Blur_Basics")
        RGBFilterList.Items.Add("Contrast_Basics")
        RGBFilterList.Items.Add("Dilate_Basics")
        RGBFilterList.Items.Add("Erode_Basics")
        RGBFilterList.Items.Add("Filter_Laplacian")
        RGBFilterList.Items.Add("PhotoShop_SharpenDetail")
        RGBFilterList.Items.Add("PhotoShop_WhiteBalance")
        RGBFilterList.SelectedIndex = 0

        ShowAllOptions.Checked = GetSetting("OpenCVB1", "ShowAllOptions", "ShowAllOptions", False)

        task.colorReductionDefault = 40
        Select Case task.cameraName
            Case "Azure Kinect 4K"
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
            Case "Intel(R) RealSense(TM) Depth Camera 455"
            Case "Oak-D camera"
                task.colorReductionDefault = 80
            Case "StereoLabs ZED 2/2i"
            Case "MYNT-EYE-D1000"
        End Select

        task.dotSize = 1
        task.cvFontThickness = 1
        Select Case task.workingRes.Width
            Case 1920
                GridSize.Value = 192
                task.cvFontSize = 3.5
                task.cvFontThickness = 4
                task.dotSize = 4
                task.disparityAdjustment = 1.1
                task.minRes = New cv.Size(240, 135)
                task.quarterRes = New cv.Size(480, 270)
                task.densityMetric = 40
            Case 960
                GridSize.Value = 96
                task.cvFontSize = 2.0
                task.cvFontThickness = 2
                task.dotSize = 2
                task.disparityAdjustment = 2.2
                task.minRes = New cv.Size(240, 135)
                task.quarterRes = New cv.Size(480, 270)
                task.densityMetric = 200
            Case 480
                GridSize.Value = 48
                task.cvFontSize = 1.2
                task.disparityAdjustment = 4.4
                task.minRes = New cv.Size(240, 135)
                task.quarterRes = New cv.Size(480, 270)
                task.densityMetric = 650
            Case 1280
                GridSize.Value = 128
                task.cvFontSize = 2.5
                task.cvFontThickness = 2
                task.dotSize = 3
                task.disparityAdjustment = 2.2
                task.minRes = New cv.Size(320, 180)
                task.quarterRes = New cv.Size(320, 180)
                task.densityMetric = 150
            Case 640
                GridSize.Value = 64
                task.cvFontSize = 1.5
                task.dotSize = 2
                task.disparityAdjustment = 4.2
                task.minRes = New cv.Size(320, task.workingRes.Height / 2)
                task.quarterRes = New cv.Size(320, 180)
                task.densityMetric = 200
            Case 320
                GridSize.Value = 32
                task.cvFontSize = 1.0
                task.disparityAdjustment = 8.4
                task.minRes = New cv.Size(320, 180)
                task.quarterRes = New cv.Size(320, 180)
                If task.workingRes.Height = 240 Then task.minRes = New cv.Size(160, 120)
                task.densityMetric = 500
            Case 160
                GridSize.Value = 16
                task.cvFontSize = 1.0
                task.disparityAdjustment = 4.4
                task.minRes = New cv.Size(160, 120)
                task.quarterRes = New cv.Size(320, 180)
                task.densityMetric = 100
            Case 672
                GridSize.Value = 64
                task.cvFontSize = 1.5
                task.dotSize = 1
                task.disparityAdjustment = 4.4
                task.minRes = New cv.Size(168, 94)
                task.quarterRes = New cv.Size(336, 188)
                task.densityMetric = 300
            Case 336
                GridSize.Value = 32
                task.cvFontSize = 1.0
                task.dotSize = 1
                task.disparityAdjustment = 8.8
                task.minRes = New cv.Size(168, 94)
                task.quarterRes = New cv.Size(336, 188)
                task.densityMetric = 700
            Case 168
                GridSize.Value = 16
                task.cvFontSize = 0.5
                task.disparityAdjustment = 20.0
                task.minRes = New cv.Size(168, 94)
                task.quarterRes = New cv.Size(336, 188)
                task.densityMetric = 1700
        End Select

        Dim min = task.workingRes.Width * task.workingRes.Height * 0.0005
        Dim modVal = If(min > 200, 100, 10)
        If min < 10 Then min = 10
        minPixelsSlider.Value = min - (min Mod modVal)
        MinPixels.Text = CStr(minPixelsSlider.Value)

        task.depthThresholdPercent = 0.01
        gOptions.dotSizeSlider.Value = task.dotSize
        gOptions.LineWidth.Value = task.dotSize
        DotSizeLabel.Text = CStr(dotSizeSlider.Value)

        Me.Left = 0
        Me.Top = 30
    End Sub
    Private Sub LineType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineType.SelectedIndexChanged
        task.optionsChanged = True
        Select Case LineType.Text
            Case "AntiAlias"
                task.lineType = cv.LineTypes.AntiAlias
            Case "Link4"
                task.lineType = cv.LineTypes.Link4
            Case "Link8"
                task.lineType = cv.LineTypes.Link8
        End Select
    End Sub
    Private Sub LineWidth_Scroll(sender As Object, e As EventArgs) Handles LineWidth.Scroll
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        LineThicknessAmount.Text = CStr(LineWidth.Value)
    End Sub
    Private Sub dotSizeSlider_Scroll(sender As Object, e As EventArgs) Handles dotSizeSlider.Scroll
        DotSizeLabel.Text = CStr(dotSizeSlider.Value)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub UseKalman_CheckedChanged(sender As Object, e As EventArgs) Handles UseKalman.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub UseMultiThreading_CheckedChanged(sender As Object, e As EventArgs) Handles UseMultiThreading.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub displayDst0_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst0.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub displayDst1_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst1.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub MaxRange_Scroll(sender As Object, e As EventArgs) Handles MaxDepth.Scroll
        maxCount.Text = CStr(MaxDepth.Value)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub OptionsAllAlgorithm_Click(sender As Object, e As EventArgs) Handles Me.Click
        Me.BringToFront()
    End Sub
    Private Sub MinMaxDepth_Click(sender As Object, e As EventArgs) Handles MinMaxDepth.Click
        Me.BringToFront()
    End Sub
    Private Sub GroupBox2_Click(sender As Object, e As EventArgs) Handles GroupBox2.Click
        Me.BringToFront()
    End Sub
    Private Sub GridWidthSlider_Scroll(sender As Object, e As EventArgs) Handles GridSize.Scroll
        If task IsNot Nothing Then task.optionsChanged = True
        ThreadGridSize.Text = CStr(GridSize.Value)
    End Sub
    Private Sub GridWidthSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSize.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        ThreadGridSize.Text = CStr(GridSize.Value)
    End Sub
    Private Sub HistBinSlider_ValueChanged(sender As Object, e As EventArgs) Handles HistBinSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        labelBinsCount.Text = CStr(HistBinSlider.Value)
    End Sub
    Private Sub AddWeightedSlider_Scroll_1(sender As Object, e As EventArgs) Handles AddWeightedSlider.Scroll
        AddWeighted.Text = CStr(AddWeightedSlider.Value)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs) Handles useFilter.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub PixelDiffThreshold_ValueChanged(sender As Object, e As EventArgs) Handles PixelDiffThreshold.ValueChanged
        PixelDiff.Text = CStr(PixelDiffThreshold.Value)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub FrameHistory_ValueChanged(sender As Object, e As EventArgs) Handles FrameHistory.ValueChanged
        fHist.Text = CStr(FrameHistory.Value)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs) Handles OpenGLCapture.Click
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs) Handles useMotion.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        SaveSetting("OpenCVB1", "showAllOptions", "showAllOptions", ShowAllOptions.Checked)
    End Sub
    Private Sub tempSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        TempSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub
    Private Sub SyncOutput_CheckedChanged(sender As Object, e As EventArgs) Handles SyncOutput.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub minPixelsSlider_Scroll(sender As Object, e As EventArgs) Handles minPixelsSlider.Scroll
        If task IsNot Nothing Then task.optionsChanged = True
        MinPixels.Text = CStr(minPixelsSlider.Value)
    End Sub
    Private Sub minPixelsSlider_ValueChanged(sender As Object, e As EventArgs) Handles minPixelsSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        MinPixels.Text = CStr(minPixelsSlider.Value)
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs) Handles useHistoryCloud.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
End Class