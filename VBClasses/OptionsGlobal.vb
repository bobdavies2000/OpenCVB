Imports cv = OpenCvSharp
Public Class OptionsGlobal
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public heartBeatSeconds = 1
    Public trackingLabel As String
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = allOptions

        DotSizeSlider.Value = 1
        LineWidth.Value = 1
        If task.workRes.Width <= 320 Then
            DotSizeSlider.Value = 1
            LineWidth.Value = 1
        ElseIf task.workRes.Width = 640 Then
            DotSizeSlider.Value = 2
            LineWidth.Value = 2
        End If

        labelBinsCount.Text = CStr(HistBinBar.Value)
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
        DebugSliderLabel.Text = CStr(DebugSlider.Value)

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

        ShowAllOptions.Checked = GetSetting("Opencv", "ShowAllOptions", "ShowAllOptions", False)

        task.DotSize = 1
        task.cvFontThickness = 1
        task.brickSize = 8
        task.reductionTarget = 400
        Select Case task.workRes.Width
            Case 1920
                task.cvFontSize = 3.5
                task.cvFontThickness = 4
                task.DotSize = 4
                task.brickSize = 24
                task.motionThreshold = 400
            Case 1280
                task.cvFontSize = 2.5
                task.cvFontThickness = 2
                task.DotSize = 5
                task.brickSize = 24
                task.motionThreshold = 100
            Case 960
                task.cvFontSize = 2.0
                task.cvFontThickness = 2
                task.DotSize = 2
                task.brickSize = 16
                task.motionThreshold = 100
            Case 672
                task.cvFontSize = 1.5
                task.DotSize = 2
                task.brickSize = 16
                task.motionThreshold = 100
            Case 640
                task.cvFontSize = 1.5
                task.DotSize = 2
                task.brickSize = 16
            Case 480
                task.cvFontSize = 1.2
                task.brickSize = 8
                task.motionThreshold = 20
            Case 240
                task.cvFontSize = 1.2
            Case 336
                task.cvFontSize = 1.0
            Case 320
                task.cvFontSize = 1.0
            Case 168
                task.cvFontSize = 0.5
            Case 160
                task.cvFontSize = 1.0
        End Select

        task.gOptions.MotionThreshold.Value = task.motionThreshold
        task.gOptions.GridSlider.Value = task.brickSize
        task.gOptions.DotSizeSlider.Value = task.DotSize
        task.gOptions.LineWidth.Value = task.DotSize

        Me.Left = 0
        Me.Top = 30
        maxCount.Text = CStr(MaxDepthBar.Value)
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
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        task.lineWidth = LineWidth.Value
        task.optionsChanged = True
    End Sub
    Private Sub DotSizeSlider_Scroll(sender As Object, e As EventArgs) Handles DotSizeSlider.Scroll
        DotSizeLabel.Text = CStr(DotSizeSlider.Value)
        task.DotSize = DotSizeSlider.Value
        task.optionsChanged = True
    End Sub
    Private Sub UseKalman_CheckedChanged(sender As Object, e As EventArgs)
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
        ' why add anything?  Because histograms are exclusive on ranges.
        task.MaxZmeters = MaxDepthBar.Value + 0.01
        task.optionsChanged = True
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        GridSizeLabel.Text = CStr(GridSlider.Value)
        task.brickSize = GridSlider.Value
        task.optionsChanged = True
    End Sub
    Private Sub HistBinSlider_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        task.optionsChanged = True
        labelBinsCount.Text = CStr(HistBinBar.Value)
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        task.optionsChanged = True
        task.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        SaveSetting("Opencv", "showAllOptions", "showAllOptions", ShowAllOptions.Checked)
    End Sub
    Private Sub DebugSliderSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        DebugSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub



    Private Sub unFiltered_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredCloudOnly_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorOnly_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorAndCloud_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub UseHistoryCloud_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub DustFree_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs)
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
        task.optionsChanged = True
    End Sub
    Private Sub ShowQuadDepth_CheckedChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub UseMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles UseMotionMask.CheckedChanged
        task.optionsChanged = True
    End Sub
    Private Sub showMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles showMotionMask.CheckedChanged
        task.optionsChanged = True
    End Sub

    Private Sub OptionsGlobal_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        task.closeRequest = True
    End Sub




    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs)
        task.optionsChanged = True
    End Sub
    Private Sub MotionThreshold_ValueChanged(sender As Object, e As EventArgs) Handles MotionThreshold.ValueChanged
        task.motionThreshold = MotionThreshold.Value
        task.optionsChanged = True
        motionThresholdLabel.Text = CStr(task.motionThreshold)
    End Sub
End Class