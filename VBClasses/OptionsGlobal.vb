Imports cv = OpenCvSharp
Imports VBClasses
Public Class OptionsGlobal
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public trackingLabel As String
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = taskAlg.allOptions

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

        ShowAllOptions.Checked = taskAlg.Settings.ShowAllOptions

        taskAlg.DotSize = 1
        taskAlg.cvFontThickness = 1
        taskAlg.brickSize = 8
        taskAlg.reductionTarget = 400
        taskAlg.DotSize = 1
        taskAlg.lineWidth = 1
        Select Case taskAlg.workRes.Width
            Case 1920
                taskAlg.cvFontSize = 3.5
                taskAlg.cvFontThickness = 4
                taskAlg.DotSize = 5
                taskAlg.lineWidth = 5
                taskAlg.brickSize = 36
            Case 1280
                taskAlg.cvFontSize = 2.5
                taskAlg.cvFontThickness = 2
                taskAlg.DotSize = 5
                taskAlg.lineWidth = 4
                taskAlg.brickSize = 24
            Case 960
                taskAlg.cvFontSize = 2.0
                taskAlg.cvFontThickness = 2
                taskAlg.DotSize = 2
                taskAlg.lineWidth = 3
                taskAlg.brickSize = 16
            Case 672
                taskAlg.cvFontSize = 1.5
                taskAlg.DotSize = 2
                taskAlg.lineWidth = 2
                taskAlg.brickSize = 16
            Case 640
                taskAlg.cvFontSize = 1.5
                taskAlg.lineWidth = 2
                taskAlg.DotSize = 2
                taskAlg.brickSize = 16
            Case 480
                taskAlg.cvFontSize = 1.2
            Case 240
                taskAlg.cvFontSize = 1.2
            Case 336
                taskAlg.cvFontSize = 1.0
            Case 320
                taskAlg.cvFontSize = 1.0
            Case 168
                taskAlg.cvFontSize = 0.5
            Case 160
                taskAlg.cvFontSize = 1.0
        End Select

        GridSlider.Value = taskAlg.brickSize
        DotSizeSlider.Value = taskAlg.DotSize
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
        FPSDisplayLabel.Text = CStr(TargetDisplaySlider.Value)
        LineWidth.Value = taskAlg.lineWidth
        HistBinBar.Value = 16
        labelBinsCount.Text = CStr(HistBinBar.Value)

        DebugSliderLabel.Text = CStr(DebugSlider.Value)

        Me.Left = 0
        Me.Top = 30
        maxCount.Text = CStr(MaxDepthBar.Value)
    End Sub
    Private Sub LineType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineType.SelectedIndexChanged
        taskAlg.optionsChanged = True
        Select Case LineType.Text
            Case "AntiAlias"
                taskAlg.lineType = cv.LineTypes.AntiAlias
            Case "Link4"
                taskAlg.lineType = cv.LineTypes.Link4
            Case "Link8"
                taskAlg.lineType = cv.LineTypes.Link8
        End Select
    End Sub
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        taskAlg.lineWidth = LineWidth.Value
        taskAlg.optionsChanged = True
    End Sub
    Private Sub DotSizeSlider_ValueChanged(sender As Object, e As EventArgs) Handles DotSizeSlider.ValueChanged
        taskAlg.DotSize = DotSizeSlider.Value
        DotSizeLabel.Text = CStr(taskAlg.DotSize)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub UseKalman_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub displayDst0_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst0.CheckedChanged
        taskAlg.optionsChanged = True
    End Sub
    Private Sub displayDst1_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst1.CheckedChanged
        taskAlg.optionsChanged = True
    End Sub
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepthBar.ValueChanged
        maxCount.Text = CStr(MaxDepthBar.Value)
        ' why add anything?  Because histograms are exclusive on ranges.
        taskAlg.MaxZmeters = MaxDepthBar.Value + 0.01
        taskAlg.optionsChanged = True
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        GridSizeLabel.Text = CStr(GridSlider.Value)
        taskAlg.brickSize = GridSlider.Value
        taskAlg.optionsChanged = True
    End Sub
    Private Sub HistBinBar_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        taskAlg.histogramBins = HistBinBar.Value
        labelBinsCount.Text = CStr(taskAlg.histogramBins)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub DisplayFPSSlider_ValueChanged(sender As Object, e As EventArgs) Handles TargetDisplaySlider.ValueChanged
        taskAlg.optionsChanged = True
        taskAlg.Settings.FPSPaintTarget = TargetDisplaySlider.Value
        FPSDisplayLabel.Text = CStr(TargetDisplaySlider.Value)
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        taskAlg.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        taskAlg.optionsChanged = True
        taskAlg.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        taskAlg.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        taskAlg.Settings.ShowAllOptions = ShowAllOptions.Checked
    End Sub
    Private Sub DebugSliderSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        DebugSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub



    Private Sub unFiltered_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub MotionFilteredCloudOnly_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorOnly_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorAndCloud_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub UseHistoryCloud_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub DustFree_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub highlight_SelectedIndexChanged(sender As Object, e As EventArgs) Handles highlight.SelectedIndexChanged
        Select Case highlight.Text
            Case "Yellow"
                taskAlg.highlight = cv.Scalar.Yellow
            Case "Black"
                taskAlg.highlight = cv.Scalar.Black
            Case "White"
                taskAlg.highlight = cv.Scalar.White
            Case "Red"
                taskAlg.highlight = cv.Scalar.Red
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
        taskAlg.optionsChanged = True
    End Sub
    Private Sub ShowQuadDepth_CheckedChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
    Private Sub UseMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles UseMotionMask.CheckedChanged
        taskAlg.optionsChanged = True
    End Sub
    Private Sub showMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles showMotionMask.CheckedChanged
        taskAlg.optionsChanged = True
    End Sub
    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs)
        taskAlg.optionsChanged = True
    End Sub
End Class