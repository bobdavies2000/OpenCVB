Imports System.ComponentModel
Imports cv = OpenCvSharp
Public Class OptionsGlobal
    Public imu_Alpha As Single
    Public maxDepth As Integer
    Public debugChecked As Boolean
    Public DebugSliderValue As Integer
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "TwilightShifted", "Viridis", "Winter"})
    Public heartBeatSeconds = 1
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = allOptions

        ThreadGridSize.Text = CStr(GridSlider.Value)

        DotSizeSlider.Value = 1
        LineWidth.Value = 1
        If task.WorkingRes.Width <= 320 Then
            DotSizeSlider.Value = 1
            LineWidth.Value = 1
        ElseIf task.WorkingRes.Width = 640 Then
            DotSizeSlider.Value = 2
            LineWidth.Value = 2
        End If
        FrameHistory.Value = 3
        MotionFilteredColorAndCloud.Checked = True
        gravityPointCloud.Checked = True

        maxCount.Text = CStr(MaxDepthBar.Value)
        labelBinsCount.Text = CStr(HistBinBar.Value)
        PixelDiff.Text = CStr(PixelDiffBar.Value)
        fHist.Text = CStr(FrameHistory.Value)
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
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

        task.DotSize = 1
        task.cvFontThickness = 1
        Select Case task.WorkingRes.Width
            Case 1920
                GridSlider.Value = 192
                task.cvFontSize = 3.5
                task.cvFontThickness = 4
                task.DotSize = 4
                task.disparityAdjustment = 1.1
                task.lowRes = New cv.Size(240, 135)
                task.quarterRes = New cv.Size(480, 270)
                task.densityMetric = 40
                task.FASTthreshold = 25
                'gravityPointCloud.Checked = False ' too expensive at this resolution
            Case 960
                GridSlider.Value = 96
                task.cvFontSize = 2.0
                task.cvFontThickness = 2
                task.DotSize = 2
                task.disparityAdjustment = 2.2
                task.lowRes = New cv.Size(240, 135)
                task.quarterRes = New cv.Size(480, 270)
                task.densityMetric = 200
                task.FASTthreshold = 40
            Case 480
                GridSlider.Value = 48
                task.cvFontSize = 1.2
                task.disparityAdjustment = 4.4
                task.lowRes = New cv.Size(240, 135)
                task.quarterRes = New cv.Size(480, 270)
                task.densityMetric = 650
                task.FASTthreshold = 10
            Case 1280
                GridSlider.Value = 128
                task.cvFontSize = 2.5
                task.cvFontThickness = 2
                task.DotSize = 5
                task.disparityAdjustment = 2.2
                task.lowRes = New cv.Size(320, 180)
                task.quarterRes = New cv.Size(320, 180)
                task.densityMetric = 150
                task.FASTthreshold = 40
                'gravityPointCloud.Checked = False ' too expensive at this resolution
            Case 640
                GridSlider.Value = 64
                task.cvFontSize = 1.5
                task.DotSize = 2
                task.disparityAdjustment = 4.2
                task.lowRes = New cv.Size(320, task.WorkingRes.Height / 2)
                task.quarterRes = New cv.Size(320, 180)
                task.densityMetric = 200
                task.FASTthreshold = 30
            Case 320
                GridSlider.Value = 32
                task.cvFontSize = 1.0
                task.disparityAdjustment = 8.4
                task.lowRes = New cv.Size(320, 180)
                task.quarterRes = New cv.Size(320, 180)
                If task.WorkingRes.Height = 240 Then task.lowRes = New cv.Size(160, 120)
                task.densityMetric = 500
                task.FASTthreshold = 10
            Case 160
                GridSlider.Value = 16
                task.cvFontSize = 1.0
                task.disparityAdjustment = 4.4
                task.lowRes = New cv.Size(160, 120)
                task.quarterRes = New cv.Size(320, 180)
                task.densityMetric = 100
                task.FASTthreshold = 10
            Case 672
                GridSlider.Value = 64
                task.cvFontSize = 1.5
                task.DotSize = 1
                task.disparityAdjustment = 4.4
                task.lowRes = New cv.Size(168, 94)
                task.quarterRes = New cv.Size(336, 188)
                task.densityMetric = 300
                task.FASTthreshold = 10
            Case 336
                GridSlider.Value = 32
                task.cvFontSize = 1.0
                task.DotSize = 1
                task.disparityAdjustment = 8.8
                task.lowRes = New cv.Size(168, 94)
                task.quarterRes = New cv.Size(336, 188)
                task.densityMetric = 700
                task.FASTthreshold = 10
            Case 168
                GridSlider.Value = 16
                task.cvFontSize = 0.5
                task.disparityAdjustment = 20.0
                task.lowRes = New cv.Size(168, 94)
                task.quarterRes = New cv.Size(336, 188)
                task.densityMetric = 1700
                task.FASTthreshold = 10
        End Select

        task.depthThresholdPercent = 0.01
        task.gOptions.DotSizeSlider.Value = task.DotSize
        task.gOptions.LineWidth.Value = task.DotSize
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)

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
    Private Sub DotSizeSlider_Scroll(sender As Object, e As EventArgs) Handles DotSizeSlider.Scroll
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
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
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepthBar.ValueChanged
        maxCount.Text = CStr(MaxDepthBar.Value)
        maxDepth = MaxDepthBar.Value
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub GridSlider_Scroll(sender As Object, e As EventArgs) Handles GridSlider.Scroll
        If task IsNot Nothing Then
            task.optionsChanged = True
            task.gridSize = GridSlider.Value
        End If
        ThreadGridSize.Text = CStr(GridSlider.Value)
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        ThreadGridSize.Text = CStr(GridSlider.Value)
    End Sub
    Private Sub HistBinSlider_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        If task IsNot Nothing Then task.optionsChanged = True
        labelBinsCount.Text = CStr(HistBinBar.Value)
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        If task IsNot Nothing Then
            task.optionsChanged = True
            task.useGravityPointcloud = gravityPointCloud.Checked
        End If
    End Sub
    Private Sub PixelDiffThreshold_ValueChanged(sender As Object, e As EventArgs) Handles PixelDiffBar.ValueChanged
        PixelDiff.Text = CStr(PixelDiffBar.Value)
        pixelDiffThreshold = PixelDiffBar.Value
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub FrameHistory_ValueChanged(sender As Object, e As EventArgs) Handles FrameHistory.ValueChanged
        fHist.Text = CStr(FrameHistory.Value)
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub IMU_Alpha_ValueChanged(sender As Object, e As EventArgs) Handles IMU_AlphaBar.ValueChanged
        IMU_Label.Text = CStr(IMU_AlphaBar.Value)
        imu_Alpha = IMU_AlphaBar.Value / 100
        If task IsNot Nothing Then task.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        If task IsNot Nothing Then task.optionsChanged = True
        task.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        If task IsNot Nothing Then task.optionsChanged = True
        debugChecked = DebugCheckBox.Checked
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
        DebugSliderValue = DebugSlider.Value
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
                task.HighlightColor = cv.Scalar.Yellow
            Case "Black"
                task.HighlightColor = cv.Scalar.Black
            Case "White"
                task.HighlightColor = cv.Scalar.White
            Case "Red"
                task.HighlightColor = cv.Scalar.Red
        End Select
    End Sub
    Public Sub setMaxDepth(val As Integer)
        MaxDepthBar.Value = val
    End Sub
    Public Function getMaxDepthBar() As Integer
        Return MaxDepthBar.Value
    End Function
    Public Sub setDisplay0()
        displayDst0.Checked = True
    End Sub
    Public Sub setDisplay1()
        displayDst1.Checked = True
    End Sub
    Public Function getDisplay0() As Boolean
        Return displayDst0.Checked
    End Function
    Public Function getDisplay1() As Boolean
        Return displayDst1.Checked
    End Function
    Public Sub setHistogramBins(val As Integer)
        HistBinBar.Value = val
    End Sub
    Public Sub setPixelDifference(val As Integer)
        PixelDiffBar.Value = val
    End Sub
    Public Sub setShowGrid(val As Boolean)
        ShowGrid.Checked = val
    End Sub
    Public Function getShowGrid() As Boolean
        Return ShowGrid.Checked
    End Function
    Public Sub setGridSize(val As Integer)
        GridSlider.Value = val
    End Sub
    Public Function getGridSize() As Integer
        Return GridSlider.Value
    End Function
    Public Sub setGridMaximum(val As Integer)
        GridSlider.Maximum = val
    End Sub
    Public Sub setGridMinimum(val As Integer)
        GridSlider.Minimum = val
    End Sub
    Public Sub setDebugSlider(val As Integer)
        DebugSlider.Value = val
    End Sub
    Public Sub setDebugCheckBox(val As Boolean)
        DebugCheckBox.Checked = val
    End Sub
    Public Function getDebugCheckBox() As Boolean
        Return DebugCheckBox.Checked
    End Function
    Public Sub setGravityUsage(val As Boolean)
        gravityPointCloud.Checked = val
    End Sub
    Public Sub setLineType(val As Integer)
        LineType.SelectedIndex = val
    End Sub
    Public Sub setLineWidth(val As Integer)
        LineWidth.Value = val
    End Sub
    Public Sub setUnfiltered(val As Boolean)
        unFiltered.Checked = val
    End Sub
    Public Sub SetUseKalman(val As Boolean)
        UseKalman.Checked = val
    End Sub
    Public Function GetUseKalman() As Boolean
        Return UseKalman.Checked
    End Function
    Public Function getMultiThreading() As Boolean
        Return UseMultiThreading.Checked
    End Function
    Public Sub setRGBFilterActive(val As Boolean)
        RGBFilterActive.Checked = val
    End Sub
    Public Sub setRGBFilterSelection(val As String)
        RGBFilterList.SelectedIndex = task.gOptions.RGBFilterList.Items.IndexOf(val)
    End Sub
    Public Sub SetDotSize(val As Integer)
        DotSizeSlider.Value = val
    End Sub
End Class