Imports cv = OpenCvSharp
Public Class OptionsGlobal
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "TwilightShifted", "Viridis", "Winter"})
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
        FrameHistory.Value = 3
        MotionFilteredColorAndCloud.Checked = True
        gravityPointCloud.Checked = True

        maxCount.Text = CStr(MaxDepth.Value)
        labelBinsCount.Text = CStr(HistBinSlider.Value)
        PixelDiff.Text = CStr(PixelDiffThreshold.Value)
        fHist.Text = CStr(FrameHistory.Value)
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        DotSizeLabel.Text = CStr(dotSizeSlider.Value)
        TempSliderLabel.Text = CStr(DebugSlider.Value)

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
        RGBFilterList.Items.Add("Brightness_Basics")
        RGBFilterList.Items.Add("Contrast_Basics")
        RGBFilterList.Items.Add("Dilate_Basics")
        RGBFilterList.Items.Add("Erode_Basics")
        RGBFilterList.Items.Add("Filter_Laplacian")
        RGBFilterList.Items.Add("PhotoShop_SharpenDetail")
        RGBFilterList.Items.Add("PhotoShop_WhiteBalance")
        RGBFilterList.SelectedIndex = 0

        HighlightColor.Items.Add("Yellow")
        HighlightColor.Items.Add("Black")
        HighlightColor.Items.Add("White")
        HighlightColor.Items.Add("Red")
        HighlightColor.SelectedIndex = 0

        ShowAllOptions.Checked = GetSetting("OpenCVB", "ShowAllOptions", "ShowAllOptions", False)

        task.dotSize = 1
        task.cvFontThickness = 1
        Select Case task.workingRes.Width
            Case 1920
                GridSize.Value = 192
                task.cvFontSize = 3.5
                task.cvFontThickness = 4
                task.dotSize = 4
                task.disparityAdjustment = 1.1
                task.lowRes = New cv.Size(240, 135)
                task.quarterRes = New cv.Size(480, 270)
                task.densityMetric = 40
                task.FASTthreshold = 25
                'gravityPointCloud.Checked = False ' too expensive at this resolution
            Case 960
                GridSize.Value = 96
                task.cvFontSize = 2.0
                task.cvFontThickness = 2
                task.dotSize = 2
                task.disparityAdjustment = 2.2
                task.lowRes = New cv.Size(240, 135)
                task.quarterRes = New cv.Size(480, 270)
                task.densityMetric = 200
                task.FASTthreshold = 20
            Case 480
                GridSize.Value = 48
                task.cvFontSize = 1.2
                task.disparityAdjustment = 4.4
                task.lowRes = New cv.Size(240, 135)
                task.quarterRes = New cv.Size(480, 270)
                task.densityMetric = 650
                task.FASTthreshold = 10
            Case 1280
                GridSize.Value = 128
                task.cvFontSize = 2.5
                task.cvFontThickness = 2
                task.dotSize = 3
                task.disparityAdjustment = 2.2
                task.lowRes = New cv.Size(320, 180)
                task.quarterRes = New cv.Size(320, 180)
                task.densityMetric = 150
                task.FASTthreshold = 20
                'gravityPointCloud.Checked = False ' too expensive at this resolution
            Case 640
                GridSize.Value = 64
                task.cvFontSize = 1.5
                task.dotSize = 2
                task.disparityAdjustment = 4.2
                task.lowRes = New cv.Size(320, task.workingRes.Height / 2)
                task.quarterRes = New cv.Size(320, 180)
                task.densityMetric = 200
                task.FASTthreshold = 10
            Case 320
                GridSize.Value = 32
                task.cvFontSize = 1.0
                task.disparityAdjustment = 8.4
                task.lowRes = New cv.Size(320, 180)
                task.quarterRes = New cv.Size(320, 180)
                If task.workingRes.Height = 240 Then task.lowRes = New cv.Size(160, 120)
                task.densityMetric = 500
                task.FASTthreshold = 5
            Case 160
                GridSize.Value = 16
                task.cvFontSize = 1.0
                task.disparityAdjustment = 4.4
                task.lowRes = New cv.Size(160, 120)
                task.quarterRes = New cv.Size(320, 180)
                task.densityMetric = 100
                task.FASTthreshold = 1
            Case 672
                GridSize.Value = 64
                task.cvFontSize = 1.5
                task.dotSize = 1
                task.disparityAdjustment = 4.4
                task.lowRes = New cv.Size(168, 94)
                task.quarterRes = New cv.Size(336, 188)
                task.densityMetric = 300
                task.FASTthreshold = 10
            Case 336
                GridSize.Value = 32
                task.cvFontSize = 1.0
                task.dotSize = 1
                task.disparityAdjustment = 8.8
                task.lowRes = New cv.Size(168, 94)
                task.quarterRes = New cv.Size(336, 188)
                task.densityMetric = 700
                task.FASTthreshold = 5
            Case 168
                GridSize.Value = 16
                task.cvFontSize = 0.5
                task.disparityAdjustment = 20.0
                task.lowRes = New cv.Size(168, 94)
                task.quarterRes = New cv.Size(336, 188)
                task.densityMetric = 1700
                task.FASTthreshold = 1
        End Select

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
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepth.ValueChanged
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
    Private Sub IMU_Alpha_ValueChanged(sender As Object, e As EventArgs) Handles IMU_Alpha.ValueChanged
        IMU_Label.Text = CStr(IMU_Alpha.Value)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        If task IsNot Nothing Then task.optionsChanged = True
        task.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs) Handles OpenGLCapture.Click
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        SaveSetting("OpenCVB", "showAllOptions", "showAllOptions", ShowAllOptions.Checked)
    End Sub
    Private Sub tempSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        TempSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub
    Private Sub debugSyncUI_CheckedChanged(sender As Object, e As EventArgs) Handles debugSyncUI.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub



    Private Sub unFiltered_CheckedChanged(sender As Object, e As EventArgs) Handles unFiltered.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredCloudOnly_CheckedChanged(sender As Object, e As EventArgs) Handles MotionFilteredCloudOnly.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorOnly_CheckedChanged(sender As Object, e As EventArgs) Handles MotionFilteredColorOnly.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorAndCloud_CheckedChanged(sender As Object, e As EventArgs) Handles MotionFilteredColorAndCloud.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub UseHistoryCloud_CheckedChanged(sender As Object, e As EventArgs) Handles UseHistoryCloud.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub DustFree_CheckedChanged(sender As Object, e As EventArgs)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs) Handles RGBFilterActive.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub HighlightColor_SelectedIndexChanged(sender As Object, e As EventArgs) Handles HighlightColor.SelectedIndexChanged
        Select Case HighlightColor.Text
            Case "Yellow"
                task.highlightColor = cv.Scalar.Yellow
            Case "Black"
                task.highlightColor = cv.Scalar.Black
            Case "White"
                task.highlightColor = cv.Scalar.White
            Case "Red"
                task.highlightColor = cv.Scalar.Red
        End Select
    End Sub
End Class