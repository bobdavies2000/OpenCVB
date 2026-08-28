Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Public Class OptionsGlobal
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public trackingLabel As String
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = vbc.task.allOptions

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
        highlight.Items.Add("white")
        highlight.Items.Add("red")
        highlight.SelectedIndex = 0

        ShowAllOptions.Checked = vbc.task.Settings.ShowAllOptions

        vbc.task.DotSize = 1
        vbc.task.gridWH = 8
        vbc.task.smallBrick = 8
        vbc.task.DotSize = 1
        vbc.task.lineWidth = 1
        vbc.task.smallRes = New Size(320, 240)
        Select Case vbc.task.workRes.Width
            Case 1920
                vbc.task.DotSize = 5
                vbc.task.lineWidth = 5
                vbc.task.gridWH = 48
                vbc.task.smallRes = New Size(240, 135)
            Case 1280
                vbc.task.DotSize = 5
                vbc.task.lineWidth = 4
                vbc.task.gridWH = 36
            Case 960
                vbc.task.DotSize = 2
                vbc.task.lineWidth = 2
                vbc.task.gridWH = 24
                vbc.task.smallRes = New Size(336, 188)
            Case 672
                vbc.task.DotSize = 2
                vbc.task.lineWidth = 2
                vbc.task.gridWH = 16
                vbc.task.smallRes = New Size(336, 188)
            Case 640
                vbc.task.lineWidth = 2
                vbc.task.DotSize = 2
                vbc.task.gridWH = 16
            Case 480
                vbc.task.smallRes = New Size(480, 270)
                vbc.task.gridWH = 12
            Case 240
                vbc.task.smallRes = New Size(240, 150)
            Case 336
                vbc.task.smallRes = New Size(336, 188)
            Case 320
            Case 168
                vbc.task.smallRes = New Size(168, 94)
                vbc.task.gridWH = 5
            Case 160
                vbc.task.smallRes = New Size(160, 120)
                vbc.task.gridWH = 5
        End Select

        GridSlider.Value = vbc.task.gridWH
        DotSizeSlider.Value = vbc.task.DotSize
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
        LineWidth.Value = vbc.task.lineWidth
        HistBinBar.Value = 16
        labelBinsCount.Text = CStr(HistBinBar.Value)

        DebugSliderLabel.Text = CStr(DebugSlider.Value)

        ShowSplash.Checked = CBool(GetSetting("OpenCVB", "ShowSplash", "ShowSplash", True))
        PaintFrequencyLabel.Text = vbc.task.Settings.paintFrequency

        Me.Left = 0
        Me.Top = 30
        maxCount.Text = CStr(MaxDepthBar.Value)
        GridSizeLabel.Text = CStr(GridSlider.Value)
    End Sub
    Private Sub LineType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineType.SelectedIndexChanged
        vbc.task.optionsChanged = True
        Select Case LineType.Text
            Case "AntiAlias"
                vbc.task.lineType = LineTypes.AntiAlias
            Case "Link4"
                vbc.task.lineType = LineTypes.Link4
            Case "Link8"
                vbc.task.lineType = LineTypes.Link8
        End Select
    End Sub
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        vbc.task.lineWidth = LineWidth.Value
        vbc.task.optionsChanged = True
    End Sub
    Private Sub DotSizeSlider_ValueChanged(sender As Object, e As EventArgs) Handles DotSizeSlider.ValueChanged
        vbc.task.DotSize = DotSizeSlider.Value
        DotSizeLabel.Text = CStr(vbc.task.DotSize)
        vbc.task.optionsChanged = True
    End Sub
    Private Sub showMyDst0_CheckedChanged(sender As Object, e As EventArgs) Handles showMyDst0.CheckedChanged
        vbc.task.optionsChanged = True
    End Sub
    Private Sub showMyDst1_CheckedChanged(sender As Object, e As EventArgs) Handles showMyDst1.CheckedChanged
        vbc.task.optionsChanged = True
    End Sub
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepthBar.ValueChanged
        maxCount.Text = CStr(MaxDepthBar.Value)
        ' why add anything?  Because histograms are exclusive on ranges.
        vbc.task.MaxZmeters = MaxDepthBar.Value + 0.01
        vbc.task.optionsChanged = True
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        GridSizeLabel.Text = CStr(GridSlider.Value)
        vbc.task.gridWH = GridSlider.Value
        vbc.task.optionsChanged = True
    End Sub
    Private Sub HistBinBar_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        vbc.task.histogramBins = HistBinBar.Value
        labelBinsCount.Text = CStr(vbc.task.histogramBins)
        vbc.task.optionsChanged = True
    End Sub
    Private Sub PaintFreqSlider_ValueChanged(sender As Object, e As EventArgs) Handles PaintFreqSlider.ValueChanged
        vbc.task.optionsChanged = True
        PaintFrequencyLabel.Text = CStr(PaintFreqSlider.Value)
        vbc.task.Settings.paintFrequency = PaintFreqSlider.Value
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        vbc.task.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        vbc.task.optionsChanged = True
        vbc.task.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        vbc.task.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        vbc.task.optionsChanged = True
        vbc.task.Settings.ShowAllOptions = ShowAllOptions.Checked
    End Sub
    Private Sub DebugSliderSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        vbc.task.optionsChanged = True
        DebugSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub



    Private Sub highlight_SelectedIndexChanged(sender As Object, e As EventArgs) Handles highlight.SelectedIndexChanged
        vbc.task.optionsChanged = True
        Select Case highlight.Text
            Case "Yellow"
                vbc.task.highlight = Scalar.Yellow
            Case "Black"
                vbc.task.highlight = Scalar.Black
            Case "white"
                vbc.task.highlight = Scalar.white
            Case "red"
                vbc.task.highlight = Scalar.red
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
    Private Sub TruncateDepth_CheckedChanged(sender As Object, e As EventArgs)
        vbc.task.optionsChanged = True
    End Sub
    Private Sub UseMotionMask_CheckedChanged(sender As Object, e As EventArgs)
        vbc.task.optionsChanged = True
    End Sub
    Private Sub showMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles showMotionMask.CheckedChanged
        vbc.task.optionsChanged = True
    End Sub
    Private Sub ShowSplash_CheckedChanged(sender As Object, e As EventArgs) Handles ShowSplash.CheckedChanged
        SaveSetting("OpenCVB", "ShowSplash", "ShowSplash", ShowSplash.Checked)
    End Sub
    Private Sub stableDepthRGB_CheckedChanged(sender As Object, e As EventArgs) Handles stableDepthRGB.CheckedChanged
        vbc.task.optionsChanged = True
    End Sub
    Private Sub CrossHairs_CheckedChanged(sender As Object, e As EventArgs) Handles CrossHairs.CheckedChanged
        vbc.task.optionsChanged = True
    End Sub
    Private Sub ShowGrid_CheckedChanged(sender As Object, e As EventArgs) Handles ShowGrid.CheckedChanged
        vbc.task.optionsChanged = True
    End Sub
End Class
