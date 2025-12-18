Imports cv = OpenCvSharp
Imports VBClasses
Public Class OptionsGlobal
    Public pixelDiffThreshold As Integer
    Public mapNames As New List(Of String)({"Autumn", "Bone", "Cividis", "Cool", "Hot", "Hsv", "Inferno", "Jet", "Magma", "Ocean", "Parula", "Pink",
                                "Plasma", "Rainbow", "Spring", "Summer", "Twilight", "Twilight_Shifted", "Viridis", "Winter"})
    Public trackingLabel As String
    Private Sub OptionsGlobal_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MdiParent = Task.allOptions

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

        ShowAllOptions.Checked = Task.settings.ShowAllOptions

        Task.DotSize = 1
        Task.cvFontThickness = 1
        Task.brickSize = 8
        Task.reductionTarget = 400
        Task.DotSize = 1
        Task.lineWidth = 1
        Select Case Task.workRes.Width
            Case 1920
                Task.cvFontSize = 3.5
                Task.cvFontThickness = 4
                Task.DotSize = 5
                Task.lineWidth = 5
                Task.brickSize = 36
            Case 1280
                Task.cvFontSize = 2.5
                Task.cvFontThickness = 2
                Task.DotSize = 5
                Task.lineWidth = 4
                Task.brickSize = 24
            Case 960
                Task.cvFontSize = 2.0
                Task.cvFontThickness = 2
                Task.DotSize = 2
                Task.lineWidth = 3
                Task.brickSize = 16
            Case 672
                Task.cvFontSize = 1.5
                Task.DotSize = 2
                Task.lineWidth = 2
                Task.brickSize = 16
            Case 640
                Task.cvFontSize = 1.5
                Task.lineWidth = 2
                Task.DotSize = 2
                Task.brickSize = 16
            Case 480
                Task.cvFontSize = 1.2
                Task.brickSize = 8
            Case 240
                Task.cvFontSize = 1.2
            Case 336
                Task.cvFontSize = 1.0
            Case 320
                Task.cvFontSize = 1.0
                Task.brickSize = 3
            Case 168
                Task.cvFontSize = 0.5
            Case 160
                Task.cvFontSize = 1.0
        End Select

        GridSlider.Value = Task.brickSize
        DotSizeSlider.Value = Task.DotSize
        LineWidth.Value = Task.lineWidth
        HistBinBar.Value = 16
        labelBinsCount.Text = CStr(HistBinBar.Value)

        DebugSliderLabel.Text = CStr(DebugSlider.Value)

        Me.Left = 0
        Me.Top = 30
        maxCount.Text = CStr(MaxDepthBar.Value)
    End Sub
    Private Sub LineType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LineType.SelectedIndexChanged
        Task.optionsChanged = True
        Select Case LineType.Text
            Case "AntiAlias"
                Task.lineType = cv.LineTypes.AntiAlias
            Case "Link4"
                Task.lineType = cv.LineTypes.Link4
            Case "Link8"
                Task.lineType = cv.LineTypes.Link8
        End Select
    End Sub
    Private Sub LineWidth_ValueChanged(sender As Object, e As EventArgs) Handles LineWidth.ValueChanged
        LineThicknessAmount.Text = CStr(LineWidth.Value)
        Task.lineWidth = LineWidth.Value
        Task.optionsChanged = True
    End Sub
    Private Sub DotSizeSlider_ValueChanged(sender As Object, e As EventArgs) Handles DotSizeSlider.ValueChanged
        Task.DotSize = DotSizeSlider.Value
        DotSizeLabel.Text = CStr(Task.DotSize)
        Task.optionsChanged = True
    End Sub
    Private Sub UseKalman_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub displayDst0_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst0.CheckedChanged
        Task.optionsChanged = True
    End Sub
    Private Sub displayDst1_CheckedChanged(sender As Object, e As EventArgs) Handles displayDst1.CheckedChanged
        Task.optionsChanged = True
    End Sub
    Private Sub MaxDepth_ValueChanged(sender As Object, e As EventArgs) Handles MaxDepthBar.ValueChanged
        maxCount.Text = CStr(MaxDepthBar.Value)
        ' why add anything?  Because histograms are exclusive on ranges.
        Task.MaxZmeters = MaxDepthBar.Value + 0.01
        Task.optionsChanged = True
    End Sub
    Private Sub GridSlider_ValueChanged(sender As Object, e As EventArgs) Handles GridSlider.ValueChanged
        GridSizeLabel.Text = CStr(GridSlider.Value)
        Task.brickSize = GridSlider.Value
        Task.optionsChanged = True
    End Sub
    Private Sub HistBinBar_ValueChanged(sender As Object, e As EventArgs) Handles HistBinBar.ValueChanged
        Task.histogramBins = HistBinBar.Value
        labelBinsCount.Text = CStr(Task.histogramBins)
        Task.optionsChanged = True
    End Sub
    Private Sub DisplayFPSSlider_ValueChanged(sender As Object, e As EventArgs) Handles DisplayFPSSlider.ValueChanged
        Task.optionsChanged = True
        Dim fps = DisplayFPSSlider.Value
        Task.Settings.FPSdisplay = fps
        FPSDisplayLabel.Text = CStr(fps)

        ' tick count is in milliseconds
        If fps = 0 Then
            Task.refreshTimerTickCount = 1000
        ElseIf fps < 0 Then
            Task.refreshTimerTickCount = Math.Abs(fps) * 1000
        Else
            Task.refreshTimerTickCount = CInt(1000 / fps)
        End If
    End Sub
    Private Sub gravityPointCloud_CheckedChanged(sender As Object, e As EventArgs) Handles gravityPointCloud.CheckedChanged
        Task.optionsChanged = True
    End Sub
    Private Sub Palettes_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles Palettes.SelectedIndexChanged
        Task.optionsChanged = True
        Task.paletteIndex = mapNames.IndexOf(Palettes.Text)
    End Sub
    Private Sub DebugCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles DebugCheckBox.CheckedChanged
        Task.optionsChanged = True
    End Sub
    Private Sub OpenGLCapture_Click(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub useMotion_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub ShowAllByDefault_CheckedChanged(sender As Object, e As EventArgs) Handles ShowAllOptions.CheckedChanged
        Task.settings.showAllOptions = ShowAllOptions.Checked
    End Sub
    Private Sub DebugSliderSlider_ValueChanged(sender As Object, e As EventArgs) Handles DebugSlider.ValueChanged
        DebugSliderLabel.Text = CStr(DebugSlider.Value)
    End Sub
    Private Sub useCloudHistory_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub



    Private Sub unFiltered_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredCloudOnly_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorOnly_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub MotionFilteredColorAndCloud_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub UseHistoryCloud_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub DustFree_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub useFilter_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub highlight_SelectedIndexChanged(sender As Object, e As EventArgs) Handles highlight.SelectedIndexChanged
        Select Case highlight.Text
            Case "Yellow"
                Task.highlight = cv.Scalar.Yellow
            Case "Black"
                Task.highlight = cv.Scalar.Black
            Case "White"
                Task.highlight = cv.Scalar.White
            Case "Red"
                Task.highlight = cv.Scalar.Red
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
        Task.optionsChanged = True
    End Sub
    Private Sub ShowQuadDepth_CheckedChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
    Private Sub UseMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles UseMotionMask.CheckedChanged
        Task.optionsChanged = True
    End Sub
    Private Sub showMotionMask_CheckedChanged(sender As Object, e As EventArgs) Handles showMotionMask.CheckedChanged
        Task.optionsChanged = True
    End Sub
    Private Sub ColorSource_SelectedIndexChanged(sender As Object, e As EventArgs)
        Task.optionsChanged = True
    End Sub
End Class