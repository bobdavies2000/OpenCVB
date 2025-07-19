<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsGlobal
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Label1 = New Label()
        GroupBox1 = New GroupBox()
        DebugSlider = New TrackBar()
        DebugSliderLabel = New Label()
        DebugSliderText = New Label()
        DebugCheckBox = New CheckBox()
        FrameHistory = New TrackBar()
        fHist = New Label()
        Label12 = New Label()
        DepthDiffSlider = New TrackBar()
        DepthDiffLabel = New Label()
        Label10 = New Label()
        PixelDiffBar = New TrackBar()
        PixelDiff = New Label()
        Label8 = New Label()
        HistBinBar = New TrackBar()
        labelBinsCount = New Label()
        Label6 = New Label()
        GridSlider = New TrackBar()
        GridSizeLabel = New Label()
        Label4 = New Label()
        MaxDepthBar = New TrackBar()
        maxCount = New Label()
        Label2 = New Label()
        GroupBox2 = New GroupBox()
        GroupBox3 = New GroupBox()
        CheckBox1 = New CheckBox()
        showMotionMask = New CheckBox()
        TruncateDepth = New CheckBox()
        gravityPointCloud = New CheckBox()
        UseKalman = New CheckBox()
        CrossHairs = New CheckBox()
        CreateGif = New CheckBox()
        debugSyncUI = New CheckBox()
        ShowGrid = New CheckBox()
        ShowAllOptions = New CheckBox()
        displayDst1 = New CheckBox()
        displayDst0 = New CheckBox()
        Geometry = New GroupBox()
        DotSizeSlider = New TrackBar()
        DotSizeLabel = New Label()
        Label7 = New Label()
        LineWidth = New TrackBar()
        LineThicknessAmount = New Label()
        Label5 = New Label()
        ColorSource = New ComboBox()
        Palettes = New ComboBox()
        LineType = New ComboBox()
        highlight = New ComboBox()
        Label9 = New Label()
        Label11 = New Label()
        Label13 = New Label()
        Label14 = New Label()
        DepthGroupBox = New GroupBox()
        DepthCorrelations = New RadioButton()
        ColorizedDepth = New RadioButton()
        ColoringGroup = New GroupBox()
        TrackingColor = New RadioButton()
        TrackingMeanColor = New RadioButton()
        GroupBox1.SuspendLayout()
        CType(DebugSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(FrameHistory, ComponentModel.ISupportInitialize).BeginInit()
        CType(DepthDiffSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(PixelDiffBar, ComponentModel.ISupportInitialize).BeginInit()
        CType(HistBinBar, ComponentModel.ISupportInitialize).BeginInit()
        CType(GridSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(MaxDepthBar, ComponentModel.ISupportInitialize).BeginInit()
        GroupBox2.SuspendLayout()
        GroupBox3.SuspendLayout()
        Geometry.SuspendLayout()
        CType(DotSizeSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(LineWidth, ComponentModel.ISupportInitialize).BeginInit()
        DepthGroupBox.SuspendLayout()
        ColoringGroup.SuspendLayout()
        SuspendLayout()
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(12, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(1010, 25)
        Label1.TabIndex = 0
        Label1.Text = "All values are restored to their default values at the start of each algorithm.  See OptionsGlobal.vb to change any default value."
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(DebugSlider)
        GroupBox1.Controls.Add(DebugSliderLabel)
        GroupBox1.Controls.Add(DebugSliderText)
        GroupBox1.Controls.Add(DebugCheckBox)
        GroupBox1.Controls.Add(FrameHistory)
        GroupBox1.Controls.Add(fHist)
        GroupBox1.Controls.Add(Label12)
        GroupBox1.Controls.Add(DepthDiffSlider)
        GroupBox1.Controls.Add(DepthDiffLabel)
        GroupBox1.Controls.Add(Label10)
        GroupBox1.Controls.Add(PixelDiffBar)
        GroupBox1.Controls.Add(PixelDiff)
        GroupBox1.Controls.Add(Label8)
        GroupBox1.Controls.Add(HistBinBar)
        GroupBox1.Controls.Add(labelBinsCount)
        GroupBox1.Controls.Add(Label6)
        GroupBox1.Controls.Add(GridSlider)
        GroupBox1.Controls.Add(GridSizeLabel)
        GroupBox1.Controls.Add(Label4)
        GroupBox1.Controls.Add(MaxDepthBar)
        GroupBox1.Controls.Add(maxCount)
        GroupBox1.Controls.Add(Label2)
        GroupBox1.Location = New Point(12, 28)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(829, 609)
        GroupBox1.TabIndex = 1
        GroupBox1.TabStop = False
        ' 
        ' DebugSlider
        ' 
        DebugSlider.Location = New Point(191, 527)
        DebugSlider.Maximum = 100
        DebugSlider.Minimum = -100
        DebugSlider.Name = "DebugSlider"
        DebugSlider.Size = New Size(457, 69)
        DebugSlider.TabIndex = 22
        ' 
        ' DebugSliderLabel
        ' 
        DebugSliderLabel.AutoSize = True
        DebugSliderLabel.Location = New Point(668, 527)
        DebugSliderLabel.Name = "DebugSliderLabel"
        DebugSliderLabel.Size = New Size(151, 25)
        DebugSliderLabel.TabIndex = 21
        DebugSliderLabel.Text = "DebugSliderLabel"
        ' 
        ' DebugSliderText
        ' 
        DebugSliderText.AutoSize = True
        DebugSliderText.Location = New Point(62, 517)
        DebugSliderText.Name = "DebugSliderText"
        DebugSliderText.Size = New Size(140, 25)
        DebugSliderText.TabIndex = 20
        DebugSliderText.Text = "DebugSliderText"
        ' 
        ' DebugCheckBox
        ' 
        DebugCheckBox.AutoSize = True
        DebugCheckBox.Location = New Point(17, 457)
        DebugCheckBox.Name = "DebugCheckBox"
        DebugCheckBox.Size = New Size(639, 29)
        DebugCheckBox.TabIndex = 19
        DebugCheckBox.Text = "DebugCheckbox - task.gOptions.DebugChecked - use anywhere as a toggle"
        DebugCheckBox.UseVisualStyleBackColor = True
        ' 
        ' FrameHistory
        ' 
        FrameHistory.Location = New Point(191, 372)
        FrameHistory.Maximum = 25
        FrameHistory.Minimum = 1
        FrameHistory.Name = "FrameHistory"
        FrameHistory.Size = New Size(457, 69)
        FrameHistory.TabIndex = 18
        FrameHistory.Value = 5
        ' 
        ' fHist
        ' 
        fHist.AutoSize = True
        fHist.Location = New Point(668, 372)
        fHist.Name = "fHist"
        fHist.Size = New Size(49, 25)
        fHist.TabIndex = 17
        fHist.Text = "fHist"
        ' 
        ' Label12
        ' 
        Label12.AutoSize = True
        Label12.Location = New Point(62, 362)
        Label12.Name = "Label12"
        Label12.Size = New Size(123, 25)
        Label12.TabIndex = 16
        Label12.Text = "Frame History"
        ' 
        ' DepthDiffSlider
        ' 
        DepthDiffSlider.Location = New Point(191, 303)
        DepthDiffSlider.Maximum = 1000
        DepthDiffSlider.Minimum = 1
        DepthDiffSlider.Name = "DepthDiffSlider"
        DepthDiffSlider.Size = New Size(457, 69)
        DepthDiffSlider.TabIndex = 15
        DepthDiffSlider.Value = 5
        ' 
        ' DepthDiffLabel
        ' 
        DepthDiffLabel.AutoSize = True
        DepthDiffLabel.Location = New Point(668, 314)
        DepthDiffLabel.Name = "DepthDiffLabel"
        DepthDiffLabel.Size = New Size(131, 25)
        DepthDiffLabel.TabIndex = 14
        DepthDiffLabel.Text = "DepthDiffLabel"
        ' 
        ' Label10
        ' 
        Label10.Location = New Point(17, 303)
        Label10.Name = "Label10"
        Label10.Size = New Size(168, 59)
        Label10.TabIndex = 13
        Label10.Text = "Depth Difference Threshold (mm's)"
        ' 
        ' PixelDiffBar
        ' 
        PixelDiffBar.Location = New Point(191, 234)
        PixelDiffBar.Maximum = 50
        PixelDiffBar.Name = "PixelDiffBar"
        PixelDiffBar.Size = New Size(457, 69)
        PixelDiffBar.TabIndex = 12
        PixelDiffBar.Value = 5
        ' 
        ' PixelDiff
        ' 
        PixelDiff.AutoSize = True
        PixelDiff.Location = New Point(668, 234)
        PixelDiff.Name = "PixelDiff"
        PixelDiff.Size = New Size(76, 25)
        PixelDiff.TabIndex = 11
        PixelDiff.Text = "PixelDiff"
        ' 
        ' Label8
        ' 
        Label8.Location = New Point(17, 234)
        Label8.Name = "Label8"
        Label8.Size = New Size(168, 55)
        Label8.TabIndex = 10
        Label8.Text = "Color Difference Threshold"
        ' 
        ' HistBinBar
        ' 
        HistBinBar.Location = New Point(191, 165)
        HistBinBar.Maximum = 32
        HistBinBar.Minimum = 3
        HistBinBar.Name = "HistBinBar"
        HistBinBar.Size = New Size(457, 69)
        HistBinBar.TabIndex = 9
        HistBinBar.Value = 5
        ' 
        ' labelBinsCount
        ' 
        labelBinsCount.AutoSize = True
        labelBinsCount.Location = New Point(668, 165)
        labelBinsCount.Name = "labelBinsCount"
        labelBinsCount.Size = New Size(129, 25)
        labelBinsCount.TabIndex = 8
        labelBinsCount.Text = "labelBinsCount"
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(52, 165)
        Label6.Name = "Label6"
        Label6.Size = New Size(133, 25)
        Label6.TabIndex = 7
        Label6.Text = "Histogram Bins"
        ' 
        ' GridSlider
        ' 
        GridSlider.Location = New Point(191, 96)
        GridSlider.Maximum = 64
        GridSlider.Minimum = 2
        GridSlider.Name = "GridSlider"
        GridSlider.Size = New Size(457, 69)
        GridSlider.TabIndex = 6
        GridSlider.Value = 5
        ' 
        ' GridSizeLabel
        ' 
        GridSizeLabel.AutoSize = True
        GridSizeLabel.Location = New Point(668, 96)
        GridSizeLabel.Name = "GridSizeLabel"
        GridSizeLabel.Size = New Size(117, 25)
        GridSizeLabel.TabIndex = 5
        GridSizeLabel.Text = "GridSizeLabel"
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(44, 96)
        Label4.Name = "Label4"
        Label4.Size = New Size(141, 25)
        Label4.TabIndex = 4
        Label4.Text = "Grid Square Size"
        ' 
        ' MaxDepthBar
        ' 
        MaxDepthBar.Location = New Point(191, 27)
        MaxDepthBar.Maximum = 25
        MaxDepthBar.Minimum = 1
        MaxDepthBar.Name = "MaxDepthBar"
        MaxDepthBar.Size = New Size(457, 69)
        MaxDepthBar.TabIndex = 3
        MaxDepthBar.Value = 5
        ' 
        ' maxCount
        ' 
        maxCount.AutoSize = True
        maxCount.Location = New Point(668, 27)
        maxCount.Name = "maxCount"
        maxCount.Size = New Size(93, 25)
        maxCount.TabIndex = 2
        maxCount.Text = "maxCount"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(17, 27)
        Label2.Name = "Label2"
        Label2.Size = New Size(168, 25)
        Label2.TabIndex = 1
        Label2.Text = "Max Depth (meters)"
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Controls.Add(GroupBox3)
        GroupBox2.Controls.Add(TruncateDepth)
        GroupBox2.Controls.Add(gravityPointCloud)
        GroupBox2.Controls.Add(UseKalman)
        GroupBox2.Controls.Add(CrossHairs)
        GroupBox2.Controls.Add(CreateGif)
        GroupBox2.Controls.Add(debugSyncUI)
        GroupBox2.Controls.Add(ShowGrid)
        GroupBox2.Controls.Add(ShowAllOptions)
        GroupBox2.Controls.Add(displayDst1)
        GroupBox2.Controls.Add(displayDst0)
        GroupBox2.Location = New Point(852, 40)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Size = New Size(357, 429)
        GroupBox2.TabIndex = 2
        GroupBox2.TabStop = False
        GroupBox2.Text = "Miscellaneous Globals"
        ' 
        ' GroupBox3
        ' 
        GroupBox3.Controls.Add(CheckBox1)
        GroupBox3.Controls.Add(showMotionMask)
        GroupBox3.Location = New Point(15, 342)
        GroupBox3.Name = "GroupBox3"
        GroupBox3.Size = New Size(248, 87)
        GroupBox3.TabIndex = 10
        GroupBox3.TabStop = False
        GroupBox3.Text = "Motion"
        ' 
        ' CheckBox1
        ' 
        CheckBox1.AutoSize = True
        CheckBox1.Checked = True
        CheckBox1.CheckState = CheckState.Checked
        CheckBox1.Location = New Point(18, 52)
        CheckBox1.Name = "CheckBox1"
        CheckBox1.Size = New Size(172, 29)
        CheckBox1.TabIndex = 1
        CheckBox1.Text = "Use MotionMask"
        CheckBox1.UseVisualStyleBackColor = True
        ' 
        ' showMotionMask
        ' 
        showMotionMask.AutoSize = True
        showMotionMask.Location = New Point(18, 25)
        showMotionMask.Name = "showMotionMask"
        showMotionMask.Size = New Size(183, 29)
        showMotionMask.TabIndex = 0
        showMotionMask.Text = "Show motion cells"
        showMotionMask.UseVisualStyleBackColor = True
        ' 
        ' TruncateDepth
        ' 
        TruncateDepth.AutoSize = True
        TruncateDepth.Location = New Point(13, 307)
        TruncateDepth.Name = "TruncateDepth"
        TruncateDepth.Size = New Size(262, 29)
        TruncateDepth.TabIndex = 9
        TruncateDepth.Text = "Truncate depth at MaxDepth"
        TruncateDepth.UseVisualStyleBackColor = True
        ' 
        ' gravityPointCloud
        ' 
        gravityPointCloud.AutoSize = True
        gravityPointCloud.Location = New Point(12, 274)
        gravityPointCloud.Name = "gravityPointCloud"
        gravityPointCloud.Size = New Size(345, 29)
        gravityPointCloud.TabIndex = 8
        gravityPointCloud.Text = "Apply gravity transform to point cloud"
        gravityPointCloud.UseVisualStyleBackColor = True
        ' 
        ' UseKalman
        ' 
        UseKalman.AutoSize = True
        UseKalman.Location = New Point(12, 244)
        UseKalman.Name = "UseKalman"
        UseKalman.Size = New Size(195, 29)
        UseKalman.TabIndex = 7
        UseKalman.Text = "Use Kalman filtering"
        UseKalman.UseVisualStyleBackColor = True
        ' 
        ' CrossHairs
        ' 
        CrossHairs.AutoSize = True
        CrossHairs.Checked = True
        CrossHairs.CheckState = CheckState.Checked
        CrossHairs.Location = New Point(12, 214)
        CrossHairs.Name = "CrossHairs"
        CrossHairs.Size = New Size(165, 29)
        CrossHairs.TabIndex = 6
        CrossHairs.Text = "Show crosshairs"
        CrossHairs.UseVisualStyleBackColor = True
        ' 
        ' CreateGif
        ' 
        CreateGif.AutoSize = True
        CreateGif.Location = New Point(12, 184)
        CreateGif.Name = "CreateGif"
        CreateGif.Size = New Size(283, 29)
        CreateGif.TabIndex = 5
        CreateGif.Text = "Create GIF of current algorithm"
        CreateGif.UseVisualStyleBackColor = True
        ' 
        ' debugSyncUI
        ' 
        debugSyncUI.AutoSize = True
        debugSyncUI.Location = New Point(12, 154)
        debugSyncUI.Name = "debugSyncUI"
        debugSyncUI.Size = New Size(292, 29)
        debugSyncUI.TabIndex = 4
        debugSyncUI.Text = "Synchronize Debug with Display"
        debugSyncUI.UseVisualStyleBackColor = True
        ' 
        ' ShowGrid
        ' 
        ShowGrid.AutoSize = True
        ShowGrid.Location = New Point(12, 124)
        ShowGrid.Name = "ShowGrid"
        ShowGrid.Size = New Size(228, 29)
        ShowGrid.TabIndex = 3
        ShowGrid.Text = "Show grid mask overlay"
        ShowGrid.UseVisualStyleBackColor = True
        ' 
        ' ShowAllOptions
        ' 
        ShowAllOptions.AutoSize = True
        ShowAllOptions.Location = New Point(12, 94)
        ShowAllOptions.Name = "ShowAllOptions"
        ShowAllOptions.Size = New Size(251, 29)
        ShowAllOptions.TabIndex = 2
        ShowAllOptions.Text = "Show All Options on Open"
        ShowAllOptions.UseVisualStyleBackColor = True
        ' 
        ' displayDst1
        ' 
        displayDst1.AutoSize = True
        displayDst1.Location = New Point(12, 64)
        displayDst1.Name = "displayDst1"
        displayDst1.Size = New Size(181, 29)
        displayDst1.TabIndex = 1
        displayDst1.Text = "Show dst1 output"
        displayDst1.UseVisualStyleBackColor = True
        ' 
        ' displayDst0
        ' 
        displayDst0.AutoSize = True
        displayDst0.Location = New Point(12, 34)
        displayDst0.Name = "displayDst0"
        displayDst0.Size = New Size(181, 29)
        displayDst0.TabIndex = 0
        displayDst0.Text = "Show dst0 output"
        displayDst0.UseVisualStyleBackColor = True
        ' 
        ' Geometry
        ' 
        Geometry.Controls.Add(DotSizeSlider)
        Geometry.Controls.Add(DotSizeLabel)
        Geometry.Controls.Add(Label7)
        Geometry.Controls.Add(LineWidth)
        Geometry.Controls.Add(LineThicknessAmount)
        Geometry.Controls.Add(Label5)
        Geometry.Location = New Point(850, 469)
        Geometry.Name = "Geometry"
        Geometry.Size = New Size(767, 160)
        Geometry.TabIndex = 3
        Geometry.TabStop = False
        Geometry.Text = "GroupBox4"
        ' 
        ' DotSizeSlider
        ' 
        DotSizeSlider.Location = New Point(207, 91)
        DotSizeSlider.Minimum = 1
        DotSizeSlider.Name = "DotSizeSlider"
        DotSizeSlider.Size = New Size(457, 69)
        DotSizeSlider.TabIndex = 9
        DotSizeSlider.Value = 5
        ' 
        ' DotSizeLabel
        ' 
        DotSizeLabel.AutoSize = True
        DotSizeLabel.Location = New Point(664, 91)
        DotSizeLabel.Name = "DotSizeLabel"
        DotSizeLabel.Size = New Size(114, 25)
        DotSizeLabel.TabIndex = 8
        DotSizeLabel.Text = "DotSizeLabel"
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(13, 91)
        Label7.Name = "Label7"
        Label7.Size = New Size(146, 25)
        Label7.TabIndex = 7
        Label7.Text = "Dot Size in pixels"
        ' 
        ' LineWidth
        ' 
        LineWidth.Location = New Point(207, 30)
        LineWidth.Minimum = 1
        LineWidth.Name = "LineWidth"
        LineWidth.Size = New Size(457, 69)
        LineWidth.TabIndex = 6
        LineWidth.Value = 5
        ' 
        ' LineThicknessAmount
        ' 
        LineThicknessAmount.AutoSize = True
        LineThicknessAmount.Location = New Point(664, 30)
        LineThicknessAmount.Name = "LineThicknessAmount"
        LineThicknessAmount.Size = New Size(183, 25)
        LineThicknessAmount.TabIndex = 5
        LineThicknessAmount.Text = "LineThicknessAmount"
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(13, 30)
        Label5.Name = "Label5"
        Label5.Size = New Size(188, 25)
        Label5.TabIndex = 4
        Label5.Text = "Line thickness in pixels"
        ' 
        ' ColorSource
        ' 
        ColorSource.FormattingEnabled = True
        ColorSource.Location = New Point(1379, 50)
        ColorSource.Name = "ColorSource"
        ColorSource.Size = New Size(211, 33)
        ColorSource.TabIndex = 4
        ' 
        ' Palettes
        ' 
        Palettes.FormattingEnabled = True
        Palettes.Location = New Point(1379, 89)
        Palettes.Name = "Palettes"
        Palettes.Size = New Size(211, 33)
        Palettes.TabIndex = 5
        ' 
        ' LineType
        ' 
        LineType.FormattingEnabled = True
        LineType.Location = New Point(1379, 129)
        LineType.Name = "LineType"
        LineType.Size = New Size(211, 33)
        LineType.TabIndex = 6
        ' 
        ' highlight
        ' 
        highlight.FormattingEnabled = True
        highlight.Location = New Point(1379, 168)
        highlight.Name = "highlight"
        highlight.Size = New Size(211, 33)
        highlight.TabIndex = 7
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.Location = New Point(1220, 50)
        Label9.Name = "Label9"
        Label9.Size = New Size(144, 25)
        Label9.TabIndex = 21
        Label9.Text = "RedColor Source"
        ' 
        ' Label11
        ' 
        Label11.AutoSize = True
        Label11.Location = New Point(1300, 89)
        Label11.Name = "Label11"
        Label11.Size = New Size(64, 25)
        Label11.TabIndex = 22
        Label11.Text = "Palette"
        ' 
        ' Label13
        ' 
        Label13.AutoSize = True
        Label13.Location = New Point(1288, 129)
        Label13.Name = "Label13"
        Label13.Size = New Size(85, 25)
        Label13.TabIndex = 23
        Label13.Text = "Line Type"
        ' 
        ' Label14
        ' 
        Label14.AutoSize = True
        Label14.Location = New Point(1231, 165)
        Label14.Name = "Label14"
        Label14.Size = New Size(133, 25)
        Label14.TabIndex = 24
        Label14.Text = "Highlight Color"
        ' 
        ' DepthGroupBox
        ' 
        DepthGroupBox.Controls.Add(DepthCorrelations)
        DepthGroupBox.Controls.Add(ColorizedDepth)
        DepthGroupBox.Location = New Point(1223, 220)
        DepthGroupBox.Name = "DepthGroupBox"
        DepthGroupBox.Size = New Size(199, 107)
        DepthGroupBox.TabIndex = 25
        DepthGroupBox.TabStop = False
        DepthGroupBox.Text = "Depth Display"
        ' 
        ' DepthCorrelations
        ' 
        DepthCorrelations.AutoSize = True
        DepthCorrelations.Location = New Point(8, 70)
        DepthCorrelations.Name = "DepthCorrelations"
        DepthCorrelations.Size = New Size(186, 29)
        DepthCorrelations.TabIndex = 1
        DepthCorrelations.TabStop = True
        DepthCorrelations.Text = "Depth Correlations"
        DepthCorrelations.UseVisualStyleBackColor = True
        ' 
        ' ColorizedDepth
        ' 
        ColorizedDepth.AutoSize = True
        ColorizedDepth.Location = New Point(8, 35)
        ColorizedDepth.Name = "ColorizedDepth"
        ColorizedDepth.Size = New Size(166, 29)
        ColorizedDepth.TabIndex = 0
        ColorizedDepth.TabStop = True
        ColorizedDepth.Text = "Colorized Depth"
        ColorizedDepth.UseVisualStyleBackColor = True
        ' 
        ' ColoringGroup
        ' 
        ColoringGroup.Controls.Add(TrackingColor)
        ColoringGroup.Controls.Add(TrackingMeanColor)
        ColoringGroup.Location = New Point(1436, 217)
        ColoringGroup.Name = "ColoringGroup"
        ColoringGroup.Size = New Size(186, 108)
        ColoringGroup.TabIndex = 26
        ColoringGroup.TabStop = False
        ColoringGroup.Text = "RedCloud Display"
        ' 
        ' TrackingColor
        ' 
        TrackingColor.AutoSize = True
        TrackingColor.Location = New Point(20, 66)
        TrackingColor.Name = "TrackingColor"
        TrackingColor.Size = New Size(149, 29)
        TrackingColor.TabIndex = 2
        TrackingColor.TabStop = True
        TrackingColor.Text = "Tracking Color"
        TrackingColor.UseVisualStyleBackColor = True
        ' 
        ' TrackingMeanColor
        ' 
        TrackingMeanColor.AutoSize = True
        TrackingMeanColor.Location = New Point(20, 30)
        TrackingMeanColor.Name = "TrackingMeanColor"
        TrackingMeanColor.Size = New Size(129, 29)
        TrackingMeanColor.TabIndex = 1
        TrackingMeanColor.TabStop = True
        TrackingMeanColor.Text = "Mean Color"
        TrackingMeanColor.UseVisualStyleBackColor = True
        ' 
        ' OptionsGlobal
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1629, 643)
        Controls.Add(ColoringGroup)
        Controls.Add(DepthGroupBox)
        Controls.Add(Label14)
        Controls.Add(Label13)
        Controls.Add(Label11)
        Controls.Add(Label9)
        Controls.Add(highlight)
        Controls.Add(LineType)
        Controls.Add(Palettes)
        Controls.Add(ColorSource)
        Controls.Add(Geometry)
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        Controls.Add(Label1)
        Name = "OptionsGlobal"
        Text = "OptionsGlobal"
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        CType(DebugSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(FrameHistory, ComponentModel.ISupportInitialize).EndInit()
        CType(DepthDiffSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(PixelDiffBar, ComponentModel.ISupportInitialize).EndInit()
        CType(HistBinBar, ComponentModel.ISupportInitialize).EndInit()
        CType(GridSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(MaxDepthBar, ComponentModel.ISupportInitialize).EndInit()
        GroupBox2.ResumeLayout(False)
        GroupBox2.PerformLayout()
        GroupBox3.ResumeLayout(False)
        GroupBox3.PerformLayout()
        Geometry.ResumeLayout(False)
        Geometry.PerformLayout()
        CType(DotSizeSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(LineWidth, ComponentModel.ISupportInitialize).EndInit()
        DepthGroupBox.ResumeLayout(False)
        DepthGroupBox.PerformLayout()
        ColoringGroup.ResumeLayout(False)
        ColoringGroup.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents Label2 As Label
    Friend WithEvents maxCount As Label
    Friend WithEvents MaxDepthBar As TrackBar
    Friend WithEvents HistBinBar As TrackBar
    Friend WithEvents labelBinsCount As Label
    Friend WithEvents Label6 As Label
    Friend WithEvents GridSlider As TrackBar
    Friend WithEvents GridSizeLabel As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents FrameHistory As TrackBar
    Friend WithEvents fHist As Label
    Friend WithEvents Label12 As Label
    Friend WithEvents DepthDiffSlider As TrackBar
    Friend WithEvents DepthDiffLabel As Label
    Friend WithEvents Label10 As Label
    Friend WithEvents PixelDiffBar As TrackBar
    Friend WithEvents PixelDiff As Label
    Friend WithEvents Label8 As Label
    Friend WithEvents DebugCheckBox As CheckBox
    Friend WithEvents DebugSlider As TrackBar
    Friend WithEvents DebugSliderLabel As Label
    Friend WithEvents DebugSliderText As Label
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents ShowGrid As CheckBox
    Friend WithEvents ShowAllOptions As CheckBox
    Friend WithEvents displayDst1 As CheckBox
    Friend WithEvents displayDst0 As CheckBox
    Friend WithEvents TruncateDepth As CheckBox
    Friend WithEvents gravityPointCloud As CheckBox
    Friend WithEvents UseKalman As CheckBox
    Friend WithEvents CrossHairs As CheckBox
    Friend WithEvents CreateGif As CheckBox
    Friend WithEvents debugSyncUI As CheckBox
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents showMotionMask As CheckBox
    Friend WithEvents CheckBox1 As CheckBox
    Friend WithEvents Geometry As GroupBox
    Friend WithEvents LineWidth As TrackBar
    Friend WithEvents LineThicknessAmount As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents DotSizeSlider As TrackBar
    Friend WithEvents DotSizeLabel As Label
    Friend WithEvents Label7 As Label
    Friend WithEvents ColorSource As ComboBox
    Friend WithEvents Palettes As ComboBox
    Friend WithEvents LineType As ComboBox
    Friend WithEvents highlight As ComboBox
    Friend WithEvents Label9 As Label
    Friend WithEvents Label11 As Label
    Friend WithEvents Label13 As Label
    Friend WithEvents Label14 As Label
    Friend WithEvents DepthGroupBox As GroupBox
    Friend WithEvents ColoringGroup As GroupBox
    Friend WithEvents DepthCorrelations As RadioButton
    Friend WithEvents ColorizedDepth As RadioButton
    Friend WithEvents TrackingColor As RadioButton
    Friend WithEvents TrackingMeanColor As RadioButton
End Class
