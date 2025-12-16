Imports cv = OpenCvSharp
Imports VBClasses.VBClasses
Public Class OptionsGlobal
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public trackingLabel As String
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = algTask.allOptions

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

        ShowAllOptions.Checked = algTask.settings.ShowAllOptions

        algTask.DotSize = 1
        algTask.cvFontThickness = 1
        algTask.brickSize = 8
        algTask.reductionTarget = 400
        algTask.DotSize = 1
        algTask.lineWidth = 1
        Select Case algTask.workRes.Width
            Case 1920
                algTask.cvFontSize = 3.5
                algTask.cvFontThickness = 4
                algTask.DotSize = 5
                algTask.lineWidth = 5
                algTask.brickSize = 36
            Case 1280
                algTask.cvFontSize = 2.5
                algTask.cvFontThickness = 2
                algTask.DotSize = 5
                algTask.lineWidth = 4
                algTask.brickSize = 24
            Case 960
                algTask.cvFontSize = 2.0
                algTask.cvFontThickness = 2
                algTask.DotSize = 2
                algTask.lineWidth = 3
                algTask.brickSize = 16
            Case 672
                algTask.cvFontSize = 1.5
                algTask.DotSize = 2
                algTask.lineWidth = 2
                algTask.brickSize = 16
            Case 640
                algTask.cvFontSize = 1.5
                algTask.lineWidth = 2
                algTask.DotSize = 2
                algTask.brickSize = 16
            Case 480
                algTask.cvFontSize = 1.2
                algTask.brickSize = 8
            Case 240
                algTask.cvFontSize = 1.2
            Case 336
                algTask.cvFontSize = 1.0
            Case 320
                algTask.cvFontSize = 1.0
                algTask.brickSize = 3
            Case 168
                algTask.cvFontSize = 0.5
            Case 160
                algTask.cvFontSize = 1.0
        End Select

        GridSlider.Value = algTask.brickSize
        DotSizeSlider.Value = algTask.DotSize
        LineWidth.Value = algTask.lineWidth
        HistBinBar.Value = 16
        labelBinsCount.Text = CStr(HistBinBar.Value)

        DebugSliderLabel.Text = CStr(DebugSlider.Value)

        Me.Left = 0
        Me.Top = 30
        maxCount.Text = CStr(MaxDepthBar.Value)
    End Sub
    Private Sub LineType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineType.SelectedIndexChanged
        algTask.optionsChanged = True
        Select Case LineType.Text
            Case "AntiAlias"
                algTask.lineType = cv.LineTypes.AntiAlias
            Case "Link4"
                algTask.lineType = cv.LineTypes.Link4
            Case "Link8"
                algTask.lineType = cv.LineTypes.Link8
        End Select
    End Sub
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        algTask.lineWidth = LineWidth.Value
        algTask.optionsChanged = True
    End Sub
    Private Sub DotSizeSlider_ValueChanged(sender As Object, e As EventArgs) Handles DotSizeSlider.ValueChanged
        algTask.DotSize = DotSizeSlider.Value
        DotSizeLabel.Text = CStr(algTask.DotSize)
        algTask.optionsChanged = True
    End Sub
    Private Sub UseKalman_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub displayDst0_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst0.CheckedChanged
        algTask.optionsChanged = True
    End Sub
    Private Sub displayDst1_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst1.CheckedChanged
        algTask.optionsChanged = True
    End Sub
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepthBar.ValueChanged
        maxCount.Text = CStr(MaxDepthBar.Value)
        ' why add anything?  Because histograms are exclusive on ranges.
        algTask.MaxZmeters = MaxDepthBar.Value + 0.01
        algTask.optionsChanged = True
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        GridSizeLabel.Text = CStr(GridSlider.Value)
        algTask.brickSize = GridSlider.Value
        algTask.optionsChanged = True
    End Sub
    Private Sub HistBinBar_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        algTask.histogramBins = HistBinBar.Value
        labelBinsCount.Text = CStr(algTask.histogramBins)
        algTask.optionsChanged = True
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        algTask.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        algTask.optionsChanged = True
        algTask.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        algTask.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        algTask.settings.showAllOptions = ShowAllOptions.Checked
    End Sub
    Private Sub DebugSliderSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        DebugSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub



    Private Sub unFiltered_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub MotionFilteredCloudOnly_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorOnly_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorAndCloud_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub UseHistoryCloud_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub DustFree_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub highlight_SelectedIndexChanged(sender As Object, e As EventArgs) Handles highlight.SelectedIndexChanged
        Select Case highlight.Text
            Case "Yellow"
                algTask.highlight = cv.Scalar.Yellow
            Case "Black"
                algTask.highlight = cv.Scalar.Black
            Case "White"
                algTask.highlight = cv.Scalar.White
            Case "Red"
                algTask.highlight = cv.Scalar.Red
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
        algTask.optionsChanged = True
    End Sub
    Private Sub ShowQuadDepth_CheckedChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub UseMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles UseMotionMask.CheckedChanged
        algTask.optionsChanged = True
    End Sub
    Private Sub showMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles showMotionMask.CheckedChanged
        algTask.optionsChanged = True
    End Sub
    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs)
        algTask.optionsChanged = True
    End Sub
    Private Sub debugSyncUI_CheckedChanged(sender As Object, e As EventArgs) Handles debugSyncUI.CheckedChanged
        algTask.debugSyncUI = debugSyncUI.Checked
    End Sub
End Class