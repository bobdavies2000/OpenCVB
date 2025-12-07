Imports cv = OpenCvSharp
Public Class OptCVBGlobal
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public heartBeatSeconds = 1
    Public trackingLabel As String
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = myTask.allOptions

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

        myTask.DotSize = 1
        myTask.cvFontThickness = 1
        myTask.brickSize = 8
        myTask.reductionTarget = 400
        myTask.lineWidth = 1
        Select Case myTask.workRes.Width
            Case 1920
                myTask.cvFontSize = 3.5
                myTask.cvFontThickness = 4
                myTask.DotSize = 5
                myTask.lineWidth = 5
                myTask.brickSize = 36
            Case 1280
                myTask.cvFontSize = 2.5
                myTask.cvFontThickness = 2
                myTask.DotSize = 5
                myTask.lineWidth = 4
                myTask.brickSize = 24
            Case 960
                myTask.cvFontSize = 2.0
                myTask.cvFontThickness = 2
                myTask.DotSize = 2
                myTask.lineWidth = 3
                myTask.brickSize = 16
            Case 672
                myTask.cvFontSize = 1.5
                myTask.DotSize = 2
                myTask.lineWidth = 2
                myTask.brickSize = 16
            Case 640
                myTask.cvFontSize = 1.5
                myTask.lineWidth = 2
                myTask.DotSize = 2
                myTask.brickSize = 16
            Case 480
                myTask.cvFontSize = 1.2
                myTask.brickSize = 8
            Case 240
                myTask.cvFontSize = 1.2
            Case 336
                myTask.cvFontSize = 1.0
            Case 320
                myTask.cvFontSize = 1.0
                myTask.brickSize = 3
            Case 168
                myTask.cvFontSize = 0.5
            Case 160
                myTask.cvFontSize = 1.0
        End Select

        GridSlider.Value = myTask.brickSize
        DotSizeSlider.Value = myTask.DotSize
        LineWidth.Value = myTask.lineWidth
        HistBinBar.Value = 16
        labelBinsCount.Text = CStr(HistBinBar.Value)

        DebugSliderLabel.Text = CStr(DebugSlider.Value)

        Me.Left = 0
        Me.Top = 30
        maxCount.Text = CStr(MaxDepthBar.Value)
    End Sub
    Private Sub LineType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineType.SelectedIndexChanged
        myTask.optionsChanged = True
        Select Case LineType.Text
            Case "AntiAlias"
                myTask.lineType = cv.LineTypes.AntiAlias
            Case "Link4"
                myTask.lineType = cv.LineTypes.Link4
            Case "Link8"
                myTask.lineType = cv.LineTypes.Link8
        End Select
    End Sub
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        myTask.lineWidth = LineWidth.Value
        myTask.optionsChanged = True
    End Sub
    Private Sub DotSizeSlider_ValueChanged(sender As Object, e As EventArgs) Handles DotSizeSlider.ValueChanged
        myTask.DotSize = DotSizeSlider.Value
        DotSizeLabel.Text = CStr(myTask.DotSize)
        myTask.optionsChanged = True
    End Sub
    Private Sub UseKalman_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub displayDst0_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst0.CheckedChanged
        myTask.optionsChanged = True
    End Sub
    Private Sub displayDst1_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst1.CheckedChanged
        myTask.optionsChanged = True
    End Sub
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepthBar.ValueChanged
        maxCount.Text = CStr(MaxDepthBar.Value)
        ' why add anything?  Because histograms are exclusive on ranges.
        myTask.MaxZmeters = MaxDepthBar.Value + 0.01
        myTask.optionsChanged = True
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        GridSizeLabel.Text = CStr(GridSlider.Value)
        myTask.brickSize = GridSlider.Value
        myTask.optionsChanged = True
    End Sub
    Private Sub HistBinBar_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        myTask.histogramBins = HistBinBar.Value
        labelBinsCount.Text = CStr(myTask.histogramBins)
        myTask.optionsChanged = True
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        myTask.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        myTask.optionsChanged = True
        myTask.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        myTask.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        SaveSetting("Opencv", "showAllOptions", "showAllOptions", ShowAllOptions.Checked)
    End Sub
    Private Sub DebugSliderSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        DebugSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub



    Private Sub unFiltered_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub MotionFilteredCloudOnly_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorOnly_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorAndCloud_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub UseHistoryCloud_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub DustFree_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub highlight_SelectedIndexChanged(sender As Object, e As EventArgs) Handles highlight.SelectedIndexChanged
        Select Case highlight.Text
            Case "Yellow"
                myTask.highlight = cv.Scalar.Yellow
            Case "Black"
                myTask.highlight = cv.Scalar.Black
            Case "White"
                myTask.highlight = cv.Scalar.White
            Case "Red"
                myTask.highlight = cv.Scalar.Red
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
        myTask.optionsChanged = True
    End Sub
    Private Sub ShowQuadDepth_CheckedChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
    Private Sub UseMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles UseMotionMask.CheckedChanged
        myTask.optionsChanged = True
    End Sub
    Private Sub showMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles showMotionMask.CheckedChanged
        myTask.optionsChanged = True
    End Sub

    Private Sub OptionsGlobal_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        myTask.closeRequest = True
    End Sub




    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs)
        myTask.optionsChanged = True
    End Sub
End Class