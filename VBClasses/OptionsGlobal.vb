Imports cv = OpenCvSharp
Imports VBClasses
Public Class OptionsGlobal
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public trackingLabel As String
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = taskA.allOptions

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

        ShowAllOptions.Checked = taskA.Settings.ShowAllOptions

        taskA.DotSize = 1
        taskA.cvFontThickness = 1
        taskA.brickSize = 8
        taskA.DotSize = 1
        taskA.lineWidth = 1
        Select Case taskA.workRes.Width
            Case 1920
                taskA.cvFontSize = 3.5
                taskA.cvFontThickness = 4
                taskA.DotSize = 5
                taskA.lineWidth = 5
                taskA.brickSize = 36
            Case 1280
                taskA.cvFontSize = 2.5
                taskA.cvFontThickness = 2
                taskA.DotSize = 5
                taskA.lineWidth = 4
                taskA.brickSize = 24
            Case 960
                taskA.cvFontSize = 2.0
                taskA.cvFontThickness = 2
                taskA.DotSize = 2
                taskA.lineWidth = 3
                taskA.brickSize = 16
            Case 672
                taskA.cvFontSize = 1.5
                taskA.DotSize = 2
                taskA.lineWidth = 2
                taskA.brickSize = 16
            Case 640
                taskA.cvFontSize = 1.5
                taskA.lineWidth = 2
                taskA.DotSize = 2
                taskA.brickSize = 16
            Case 480
                taskA.cvFontSize = 1.2
            Case 240
                taskA.cvFontSize = 1.2
            Case 336
                taskA.cvFontSize = 1.0
            Case 320
                taskA.cvFontSize = 1.0
            Case 168
                taskA.cvFontSize = 0.5
            Case 160
                taskA.cvFontSize = 1.0
        End Select

        GridSlider.Value = taskA.brickSize
        DotSizeSlider.Value = taskA.DotSize
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
        FPSDisplayLabel.Text = CStr(TargetDisplaySlider.Value)
        LineWidth.Value = taskA.lineWidth
        HistBinBar.Value = 16
        labelBinsCount.Text = CStr(HistBinBar.Value)

        DebugSliderLabel.Text = CStr(DebugSlider.Value)

        ShowSplash.Checked = CBool(GetSetting("OpenCVB", "ShowSplash", "ShowSplash", True))

        Me.Left = 0
        Me.Top = 30
        maxCount.Text = CStr(MaxDepthBar.Value)
    End Sub
    Private Sub LineType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineType.SelectedIndexChanged
        taskA.optionsChanged = True
        Select Case LineType.Text
            Case "AntiAlias"
                taskA.lineType = cv.LineTypes.AntiAlias
            Case "Link4"
                taskA.lineType = cv.LineTypes.Link4
            Case "Link8"
                taskA.lineType = cv.LineTypes.Link8
        End Select
    End Sub
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        taskA.lineWidth = LineWidth.Value
        taskA.optionsChanged = True
    End Sub
    Private Sub DotSizeSlider_ValueChanged(sender As Object, e As EventArgs) Handles DotSizeSlider.ValueChanged
        taskA.DotSize = DotSizeSlider.Value
        DotSizeLabel.Text = CStr(taskA.DotSize)
        taskA.optionsChanged = True
    End Sub
    Private Sub UseKalman_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub displayDst0_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst0.CheckedChanged
        taskA.optionsChanged = True
    End Sub
    Private Sub displayDst1_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst1.CheckedChanged
        taskA.optionsChanged = True
    End Sub
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepthBar.ValueChanged
        maxCount.Text = CStr(MaxDepthBar.Value)
        ' why add anything?  Because histograms are exclusive on ranges.
        taskA.MaxZmeters = MaxDepthBar.Value + 0.01
        taskA.optionsChanged = True
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        GridSizeLabel.Text = CStr(GridSlider.Value)
        taskA.brickSize = GridSlider.Value
        taskA.optionsChanged = True
    End Sub
    Private Sub HistBinBar_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        taskA.histogramBins = HistBinBar.Value
        labelBinsCount.Text = CStr(taskA.histogramBins)
        taskA.optionsChanged = True
    End Sub
    Private Sub DisplayFPSSlider_ValueChanged(sender As Object, e As EventArgs) Handles TargetDisplaySlider.ValueChanged
        taskA.optionsChanged = True
        taskA.Settings.FPSPaintTarget = TargetDisplaySlider.Value
        FPSDisplayLabel.Text = CStr(TargetDisplaySlider.Value)
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        taskA.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        taskA.optionsChanged = True
        taskA.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        taskA.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        taskA.Settings.ShowAllOptions = ShowAllOptions.Checked
    End Sub
    Private Sub DebugSliderSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        DebugSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub



    Private Sub unFiltered_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub MotionFilteredCloudOnly_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorOnly_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorAndCloud_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub UseHistoryCloud_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub DustFree_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub highlight_SelectedIndexChanged(sender As Object, e As EventArgs) Handles highlight.SelectedIndexChanged
        Select Case highlight.Text
            Case "Yellow"
                taskA.highlight = cv.Scalar.Yellow
            Case "Black"
                taskA.highlight = cv.Scalar.Black
            Case "White"
                taskA.highlight = cv.Scalar.White
            Case "Red"
                taskA.highlight = cv.Scalar.Red
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
        taskA.optionsChanged = True
    End Sub
    Private Sub ShowQuadDepth_CheckedChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub UseMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles UseMotionMask.CheckedChanged
        taskA.optionsChanged = True
    End Sub
    Private Sub showMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles showMotionMask.CheckedChanged
        taskA.optionsChanged = True
    End Sub
    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs)
        taskA.optionsChanged = True
    End Sub
    Private Sub ShowSplash_CheckedChanged(sender As Object, e As EventArgs) Handles ShowSplash.CheckedChanged
        SaveSetting("OpenCVB", "ShowSplash", "ShowSplash", ShowSplash.Checked)
    End Sub
End Class