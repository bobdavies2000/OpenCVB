Imports cv = OpenCvSharp
Imports VBClasses
Public Class OptionsGlobal
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public trackingLabel As String
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = atask.allOptions

        Palettes.Items.Clear()
        For Each mapName In mapNames
            Palettes.Items.Add(mapName)
        Next
        Palettes.SelectedIndex = mapNames.IndexOf("Jet")

        LineType.Items.Add("AntiAlias")
        LineType.Items.Add("Link4")
        LineType.Items.Add("Link8")
        LineType.SelectedIndex = 0

        highlight.Items.Add("Yellow")
        highlight.Items.Add("Black")
        highlight.Items.Add("White")
        highlight.Items.Add("Red")
        highlight.SelectedIndex = 0

        ShowAllOptions.Checked = atask.Settings.ShowAllOptions

        atask.DotSize = 1
        atask.cvFontThickness = 1
        atask.brickSize = 8
        atask.DotSize = 1
        atask.lineWidth = 1
        Select Case atask.workRes.Width
            Case 1920
                atask.cvFontSize = 3.5
                atask.cvFontThickness = 4
                atask.DotSize = 5
                atask.lineWidth = 5
                atask.brickSize = 36
            Case 1280
                atask.cvFontSize = 2.5
                atask.cvFontThickness = 2
                atask.DotSize = 5
                atask.lineWidth = 4
                atask.brickSize = 24
            Case 960
                atask.cvFontSize = 2.0
                atask.cvFontThickness = 2
                atask.DotSize = 2
                atask.lineWidth = 3
                atask.brickSize = 16
            Case 672
                atask.cvFontSize = 1.5
                atask.DotSize = 2
                atask.lineWidth = 2
                atask.brickSize = 16
            Case 640
                atask.cvFontSize = 1.5
                atask.lineWidth = 2
                atask.DotSize = 2
                atask.brickSize = 16
            Case 480
                atask.cvFontSize = 1.2
            Case 240
                atask.cvFontSize = 1.2
            Case 336
                atask.cvFontSize = 1.0
            Case 320
                atask.cvFontSize = 1.0
            Case 168
                atask.cvFontSize = 0.5
            Case 160
                atask.cvFontSize = 1.0
        End Select

        GridSlider.Value = atask.brickSize
        DotSizeSlider.Value = atask.DotSize
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
        FPSDisplayLabel.Text = CStr(TargetDisplaySlider.Value)
        LineWidth.Value = atask.lineWidth
        HistBinBar.Value = 16
        labelBinsCount.Text = CStr(HistBinBar.Value)

        DebugSliderLabel.Text = CStr(DebugSlider.Value)

        ShowSplash.Checked = CBool(GetSetting("OpenCVB", "ShowSplash", "ShowSplash", True))

        Me.Left = 0
        Me.Top = 30
        maxCount.Text = CStr(MaxDepthBar.Value)
    End Sub
    Private Sub LineType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineType.SelectedIndexChanged
        atask.optionsChanged = True
        Select Case LineType.Text
            Case "AntiAlias"
                atask.lineType = cv.LineTypes.AntiAlias
            Case "Link4"
                atask.lineType = cv.LineTypes.Link4
            Case "Link8"
                atask.lineType = cv.LineTypes.Link8
        End Select
    End Sub
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        atask.lineWidth = LineWidth.Value
        atask.optionsChanged = True
    End Sub
    Private Sub DotSizeSlider_ValueChanged(sender As Object, e As EventArgs) Handles DotSizeSlider.ValueChanged
        atask.DotSize = DotSizeSlider.Value
        DotSizeLabel.Text = CStr(atask.DotSize)
        atask.optionsChanged = True
    End Sub
    Private Sub UseKalman_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub displayDst0_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst0.CheckedChanged
        atask.optionsChanged = True
    End Sub
    Private Sub displayDst1_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst1.CheckedChanged
        atask.optionsChanged = True
    End Sub
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepthBar.ValueChanged
        maxCount.Text = CStr(MaxDepthBar.Value)
        ' why add anything?  Because histograms are exclusive on ranges.
        atask.MaxZmeters = MaxDepthBar.Value + 0.01
        atask.optionsChanged = True
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        GridSizeLabel.Text = CStr(GridSlider.Value)
        atask.brickSize = GridSlider.Value
        atask.optionsChanged = True
    End Sub
    Private Sub HistBinBar_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        atask.histogramBins = HistBinBar.Value
        labelBinsCount.Text = CStr(atask.histogramBins)
        atask.optionsChanged = True
    End Sub
    Private Sub DisplayFPSSlider_ValueChanged(sender As Object, e As EventArgs) Handles TargetDisplaySlider.ValueChanged
        atask.optionsChanged = True
        atask.Settings.FPSPaintTarget = TargetDisplaySlider.Value
        FPSDisplayLabel.Text = CStr(TargetDisplaySlider.Value)
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        atask.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        atask.optionsChanged = True
        atask.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        atask.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        atask.Settings.ShowAllOptions = ShowAllOptions.Checked
    End Sub
    Private Sub DebugSliderSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        DebugSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub



    Private Sub unFiltered_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub MotionFilteredCloudOnly_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorOnly_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorAndCloud_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub UseHistoryCloud_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub DustFree_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub highlight_SelectedIndexChanged(sender As Object, e As EventArgs) Handles highlight.SelectedIndexChanged
        Select Case highlight.Text
            Case "Yellow"
                atask.highlight = cv.Scalar.Yellow
            Case "Black"
                atask.highlight = cv.Scalar.Black
            Case "White"
                atask.highlight = cv.Scalar.White
            Case "Red"
                atask.highlight = cv.Scalar.Red
        End Select
    End Sub
    Public Sub setMaxDepth(val As Integer)
        MaxDepthBar.Value = val
    End Sub
    Public Sub setHistogramBins(val As Integer)
        If HistBinBar.Maximum < val Then HistBinBar.Maximum = val * 2
        HistBinBar.Value = val
    End Sub
    Public Sub setShowGrid(val As Boolean)
        ShowGrid.Checked = val
    End Sub
    Public Function getShowGrid() As Boolean
        Return ShowGrid.Checked
    End Function
    Public Function getPalette() As String
        Return Palettes.Text
    End Function
    Public Sub setPalette(val As String)
        Palettes.SelectedItem() = val
    End Sub
    Public Sub setGravityUsage(val As Boolean)
        gravityPointCloud.Checked = val
    End Sub
    Public Sub setLineType(val As Integer)
        LineType.SelectedIndex = val
    End Sub
    Public Sub setLineWidth(val As Integer)
        LineWidth.Value = val
    End Sub
    Public Sub SetDotSize(val As Integer)
        DotSizeSlider.Value = val
    End Sub
    Private Sub TruncateDepth_CheckedChanged(sender As Object, e As EventArgs) Handles TruncateDepth.CheckedChanged
        atask.optionsChanged = True
    End Sub
    Private Sub ShowQuadDepth_CheckedChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub UseMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles UseMotionMask.CheckedChanged
        atask.optionsChanged = True
    End Sub
    Private Sub showMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles showMotionMask.CheckedChanged
        atask.optionsChanged = True
    End Sub
    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs)
        atask.optionsChanged = True
    End Sub
    Private Sub ShowSplash_CheckedChanged(sender As Object, e As EventArgs) Handles ShowSplash.CheckedChanged
        SaveSetting("OpenCVB", "ShowSplash", "ShowSplash", ShowSplash.Checked)
    End Sub
End Class