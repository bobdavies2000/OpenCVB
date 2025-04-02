Imports System.Runtime.CompilerServices
Imports cv = OpenCvSharp
Public Class OptionsGlobal
    Public maxDepth As Integer
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public heartBeatSeconds = 1
    Public RGBfilters As String() = {"Blur_Basics", "Brightness_Basics", "Contrast_Basics",
                                     "Dilate_Basics", "Erode_Basics", "Filter_Laplacian",
                                     "MeanSubtraction_Basics", "PhotoShop_SharpenDetail",
                                     "PhotoShop_WhiteBalance"}
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = allOptions

        Dim cellSize = 8
        If task.dst2.Width >= 640 Then cellSize = 16
        If task.dst2.Width >= 1280 Then cellSize = 32
        If task.dst2.Width >= 1920 Then cellSize = 48
        GridSlider.Value = cellSize
        GridSizeLabel.Text = CStr(cellSize)

        DotSizeSlider.Value = 1
        LineWidth.Value = 1
        If task.dst2.Width <= 320 Then
            DotSizeSlider.Value = 1
            LineWidth.Value = 1
        ElseIf task.dst2.Width = 640 Then
            DotSizeSlider.Value = 2
            LineWidth.Value = 2
        End If
        FrameHistory.Value = 5
        MotionFilteredColorAndCloud.Checked = True
        gravityPointCloud.Checked = True

        maxCount.Text = CStr(MaxDepthBar.Value)
        labelBinsCount.Text = CStr(HistBinBar.Value)
        PixelDiff.Text = CStr(PixelDiffBar.Value)
        fHist.Text = CStr(FrameHistory.Value)
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
        DebugSliderLabel.Text = CStr(DebugSlider.Value)

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

        For Each filt In RGBfilters
            RGBFilterList.Items.Add(filt)
        Next
        RGBFilterList.SelectedIndex = 0

        highlight.Items.Add("Yellow")
        highlight.Items.Add("Black")
        highlight.Items.Add("White")
        highlight.Items.Add("Red")
        highlight.SelectedIndex = 0

        ShowAllOptions.Checked = GetSetting("Opencv", "ShowAllOptions", "ShowAllOptions", False)

        task.DotSize = 1
        task.cvFontThickness = 1
        Select Case task.dst2.Width
            Case 1920
                task.cvFontSize = 3.5
                task.cvFontThickness = 4
                task.DotSize = 4
                task.disparityAdjustment = 1.1
                task.densityMetric = 40
                task.FASTthreshold = 25
            Case 960
                task.cvFontSize = 2.0
                task.cvFontThickness = 2
                task.DotSize = 2
                task.disparityAdjustment = 2.2
                task.densityMetric = 200
                task.FASTthreshold = 40
            Case 480
                task.cvFontSize = 1.2
                task.disparityAdjustment = 4.4
                task.densityMetric = 650
                task.FASTthreshold = 10
            Case 1280
                task.cvFontSize = 2.5
                task.cvFontThickness = 2
                task.DotSize = 5
                task.disparityAdjustment = 2.2
                task.densityMetric = 150
                task.FASTthreshold = 40
            Case 640
                task.cvFontSize = 1.5
                task.DotSize = 2
                task.disparityAdjustment = 4.2
                task.densityMetric = 200
                task.FASTthreshold = 30
            Case 320
                task.cvFontSize = 1.0
                task.disparityAdjustment = 8.4
                task.densityMetric = 500
                task.FASTthreshold = 10
            Case 160
                task.cvFontSize = 1.0
                task.disparityAdjustment = 4.4
                task.densityMetric = 100
                task.FASTthreshold = 10
            Case 672
                task.cvFontSize = 1.5
                task.DotSize = 1
                task.disparityAdjustment = 4.4
                task.densityMetric = 300
                task.FASTthreshold = 10
            Case 336
                task.cvFontSize = 1.0
                task.DotSize = 1
                task.disparityAdjustment = 8.8
                task.FASTthreshold = 10
            Case 168
                task.cvFontSize = 0.5
                task.disparityAdjustment = 20.0
                task.densityMetric = 1700
                task.FASTthreshold = 10
        End Select

        task.depthThresholdPercent = 0.01
        task.gOptions.DotSizeSlider.Value = task.DotSize
        task.gOptions.LineWidth.Value = task.DotSize
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
        DepthDiffSlider.Value = 10

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
        task.optionsChanged = True
    End Sub
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        task.optionsChanged = True
        LineThicknessAmount.Text = CStr(LineWidth.Value)
    End Sub
    Private Sub DotSizeSlider_Scroll(sender As Object, e As EventArgs) Handles DotSizeSlider.Scroll
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
        task.optionsChanged = True
    End Sub
    Private Sub UseKalman_CheckedChanged(sender As Object, e As EventArgs) Handles UseKalman.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub UseMultiThreading_CheckedChanged(sender As Object, e As EventArgs) Handles UseMultiThreading.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub displayDst0_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst0.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub displayDst1_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst1.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepthBar.ValueChanged
        maxCount.Text = CStr(MaxDepthBar.Value)
        maxDepth = MaxDepthBar.Value
        task.optionsChanged = True
    End Sub
    Private Sub GridSlider_Scroll(sender As Object, e As EventArgs) Handles GridSlider.Scroll
        task.optionsChanged = True
        task.cellSize = GridSlider.Value

        If task.cellSize <= 2 Then task.cellSize = 2
        If task.cellSize > 2 And task.cellSize <= 4 Then task.cellSize = 4
        If task.cellSize > 4 And task.cellSize <= 8 Then task.cellSize = 8
        If task.cellSize > 8 And task.cellSize <= 16 Then task.cellSize = 16
        If task.cellSize > 16 And task.cellSize <= 32 Then task.cellSize = 32
        If task.cellSize > 32 And task.cellSize <= 64 Then task.cellSize = 64
        If task.cellSize > 64 And task.cellSize <= 128 Then task.cellSize = 128
        GridSlider.Value = task.cellSize

        GridSizeLabel.Text = CStr(GridSlider.Value)
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        task.optionsChanged = True
        GridSizeLabel.Text = CStr(GridSlider.Value)
    End Sub
    Private Sub HistBinSlider_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        task.optionsChanged = True
        labelBinsCount.Text = CStr(HistBinBar.Value)
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        task.optionsChanged = True
        task.useGravityPointcloud = gravityPointCloud.Checked
    End Sub
    Private Sub PixelDiffThreshold_ValueChanged(sender As Object, e As EventArgs) Handles PixelDiffBar.ValueChanged
        PixelDiff.Text = CStr(PixelDiffBar.Value)
        pixelDiffThreshold = PixelDiffBar.Value
        task.optionsChanged = True
    End Sub
    Private Sub FrameHistory_ValueChanged(sender As Object, e As EventArgs) Handles FrameHistory.ValueChanged
        fHist.Text = CStr(FrameHistory.Value)
        task.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        task.optionsChanged = True
        task.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs) Handles OpenGLCapture.Click
        task.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        SaveSetting("Opencv", "showAllOptions", "showAllOptions", ShowAllOptions.Checked)
    End Sub
    Private Sub tempSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        DebugSliderLabel.Text = CStr(DebugSlider.Value)
        task.optionsChanged = True
    End Sub
    Private Sub DepthDiffSlider_ValueChanged(sender As Object, e As EventArgs) Handles DepthDiffSlider.ValueChanged
        task.optionsChanged = True
        DepthDiffLabel.Text = CStr(DepthDiffSlider.Value)
    End Sub
    Private Sub debugSyncUI_CheckedChanged(sender As Object, e As EventArgs) Handles debugSyncUI.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub



    Private Sub unFiltered_CheckedChanged(sender As Object, e As EventArgs) Handles unFiltered.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredCloudOnly_CheckedChanged(sender As Object, e As EventArgs) Handles MotionFilteredCloudOnly.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorOnly_CheckedChanged(sender As Object, e As EventArgs) Handles MotionFilteredColorOnly.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorAndCloud_CheckedChanged(sender As Object, e As EventArgs) Handles MotionFilteredColorAndCloud.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub UseHistoryCloud_CheckedChanged(sender As Object, e As EventArgs) Handles UseHistoryCloud.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub DustFree_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs) Handles RGBFilterActive.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub highlight_SelectedIndexChanged(sender As Object, e As EventArgs) Handles highlight.SelectedIndexChanged
        Select Case highlight.Text
            Case "Yellow"
                task.highlight = cv.Scalar.Yellow
            Case "Black"
                task.highlight = cv.Scalar.Black
            Case "White"
                task.highlight = cv.Scalar.White
            Case "Red"
                task.highlight = cv.Scalar.Red
        End Select
    End Sub
    Public Sub setMaxDepth(val As Integer)
        MaxDepthBar.Value = val
    End Sub
    Public Function getMaxDepthBar() As Integer
        Return MaxDepthBar.Value
    End Function
    Public Sub setHistogramBins(val As Integer)
        HistBinBar.Value = val
    End Sub
    Public Sub setHistBinBarMax(val As Integer)
        HistBinBar.Maximum = val
    End Sub
    Public Function getHistBinBarMax() As Integer
        Return HistBinBar.Maximum
    End Function

    Public Sub setPixelDifference(val As Integer)
        PixelDiffBar.Value = val
    End Sub
    Public Function getPixelDifference() As Integer
        Return PixelDiffBar.Value
    End Function
    Public Function getPixelDifferenceMax() As Integer
        Return PixelDiffBar.Maximum
    End Function
    Public Sub setShowGrid(val As Boolean)
        ShowGrid.Checked = val
    End Sub
    Public Function getShowGrid() As Boolean
        Return ShowGrid.Checked
    End Function
    Public Sub setDebugSlider(val As Integer)
        DebugSlider.Value = val
    End Sub
    Public Sub setDebugCheckBox(val As Boolean)
        DebugCheckBox.Checked = val
    End Sub
    Public Function getPalette() As String
        Return Palettes.Text
    End Function
    Public Sub setPalette(val As String)
        Palettes.SelectedItem() = val
    End Sub
    Public Function getOpenGLCapture() As Boolean
        Return OpenGLCapture.Checked
    End Function
    Public Sub setOpenGLCapture(val As Boolean)
        OpenGLCapture.Checked = val
    End Sub
    Public Function getDebugCheckBox() As Boolean
        Return DebugCheckBox.Checked
    End Function
    Public Function getDebugSlider() As Integer
        Return DebugSlider.Value
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
    Public Function getUnfiltered() As Boolean
        Return unFiltered.Checked
    End Function
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
    Private Sub TruncateDepth_CheckedChanged(sender As Object, e As EventArgs) Handles TruncateDepth.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub ShowQuadDepth_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub LRMeanSubtraction_CheckedChanged(sender As Object, e As EventArgs) Handles LRMeanSubtraction.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub UseMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles UseMotionMask.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub showMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles showMotionMask.CheckedChanged
        task.optionsChanged = True
    End Sub
End Class