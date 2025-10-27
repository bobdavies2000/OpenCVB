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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OptionsGlobal))
        Label1 = New Label()
        GroupBox1 = New GroupBox()
        GL_LinearMode = New CheckBox()
        Label3 = New Label()
        DebugSlider = New TrackBar()
        DebugSliderLabel = New Label()
        DebugCheckBox = New CheckBox()
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
        UseMotionMask = New CheckBox()
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
        LRCorrelations = New RadioButton()
        ColorizedDepth = New RadioButton()
        ColoringGroup = New GroupBox()
        TrackingColor = New RadioButton()
        TrackingMeanColor = New RadioButton()
        GroupBox1.SuspendLayout()
        CType(DebugSlider, ComponentModel.ISupportInitialize).BeginInit()
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
        Label1.Location = New Point(13, 0)
        Label1.Margin = New Padding(4, 0, 4, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(1231, 30)
        Label1.TabIndex = 0
        Label1.Text = "All values are restored to their default values at the start of each algorithm.  See OptionsGlobal.vb to change any default value."
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(GL_LinearMode)
        GroupBox1.Controls.Add(Label3)
        GroupBox1.Controls.Add(DebugSlider)
        GroupBox1.Controls.Add(DebugSliderLabel)
        GroupBox1.Controls.Add(DebugCheckBox)
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
        GroupBox1.Location = New Point(13, 34)
        GroupBox1.Margin = New Padding(4)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Padding = New Padding(4)
        GroupBox1.Size = New Size(995, 730)
        GroupBox1.TabIndex = 1
        GroupBox1.TabStop = False
        ' 
        ' GL_LinearMode
        ' 
        GL_LinearMode.AutoSize = True
        GL_LinearMode.Location = New Point(20, 517)
        GL_LinearMode.Margin = New Padding(4)
        GL_LinearMode.Name = "GL_LinearMode"
        GL_LinearMode.Size = New Size(313, 34)
        GL_LinearMode.TabIndex = 24
        GL_LinearMode.Text = "Run SharpGL in Linear mode"
        GL_LinearMode.UseVisualStyleBackColor = True
        GL_LinearMode.Visible = False
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(35, 632)
        Label3.Margin = New Padding(4, 0, 4, 0)
        Label3.Name = "Label3"
        Label3.Size = New Size(139, 30)
        Label3.TabIndex = 23
        Label3.Text = "Debug Slider"
        ' 
        ' DebugSlider
        ' 
        DebugSlider.Location = New Point(229, 632)
        DebugSlider.Margin = New Padding(4)
        DebugSlider.Maximum = 100
        DebugSlider.Minimum = -100
        DebugSlider.Name = "DebugSlider"
        DebugSlider.Size = New Size(548, 69)
        DebugSlider.TabIndex = 22
        ' 
        ' DebugSliderLabel
        ' 
        DebugSliderLabel.AutoSize = True
        DebugSliderLabel.Location = New Point(803, 632)
        DebugSliderLabel.Margin = New Padding(4, 0, 4, 0)
        DebugSliderLabel.Name = "DebugSliderLabel"
        DebugSliderLabel.Size = New Size(184, 30)
        DebugSliderLabel.TabIndex = 21
        DebugSliderLabel.Text = "DebugSliderLabel"
        ' 
        ' DebugCheckBox
        ' 
        DebugCheckBox.AutoSize = True
        DebugCheckBox.Location = New Point(20, 559)
        DebugCheckBox.Margin = New Padding(4)
        DebugCheckBox.Name = "DebugCheckBox"
        DebugCheckBox.Size = New Size(817, 34)
        DebugCheckBox.TabIndex = 19
        DebugCheckBox.Text = "DebugCheckbox - task.gOptions.DebugChecked - use anywhere to inject a value"
        DebugCheckBox.UseVisualStyleBackColor = True
        ' 
        ' PixelDiffBar
        ' 
        PixelDiffBar.Location = New Point(229, 280)
        PixelDiffBar.Margin = New Padding(4)
        PixelDiffBar.Maximum = 50
        PixelDiffBar.Name = "PixelDiffBar"
        PixelDiffBar.Size = New Size(548, 69)
        PixelDiffBar.TabIndex = 12
        PixelDiffBar.Value = 5
        ' 
        ' PixelDiff
        ' 
        PixelDiff.AutoSize = True
        PixelDiff.Location = New Point(803, 280)
        PixelDiff.Margin = New Padding(4, 0, 4, 0)
        PixelDiff.Name = "PixelDiff"
        PixelDiff.Size = New Size(91, 30)
        PixelDiff.TabIndex = 11
        PixelDiff.Text = "PixelDiff"
        ' 
        ' Label8
        ' 
        Label8.Location = New Point(20, 280)
        Label8.Margin = New Padding(4, 0, 4, 0)
        Label8.Name = "Label8"
        Label8.Size = New Size(203, 66)
        Label8.TabIndex = 10
        Label8.Text = "Color Difference Threshold"
        ' 
        ' HistBinBar
        ' 
        HistBinBar.Location = New Point(229, 198)
        HistBinBar.Margin = New Padding(4)
        HistBinBar.Maximum = 32
        HistBinBar.Minimum = 2
        HistBinBar.Name = "HistBinBar"
        HistBinBar.Size = New Size(548, 69)
        HistBinBar.TabIndex = 9
        HistBinBar.Value = 5
        ' 
        ' labelBinsCount
        ' 
        labelBinsCount.AutoSize = True
        labelBinsCount.Location = New Point(803, 198)
        labelBinsCount.Margin = New Padding(4, 0, 4, 0)
        labelBinsCount.Name = "labelBinsCount"
        labelBinsCount.Size = New Size(156, 30)
        labelBinsCount.TabIndex = 8
        labelBinsCount.Text = "labelBinsCount"
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(61, 198)
        Label6.Margin = New Padding(4, 0, 4, 0)
        Label6.Name = "Label6"
        Label6.Size = New Size(159, 30)
        Label6.TabIndex = 7
        Label6.Text = "Histogram Bins"
        ' 
        ' GridSlider
        ' 
        GridSlider.Location = New Point(229, 116)
        GridSlider.Margin = New Padding(4)
        GridSlider.Maximum = 64
        GridSlider.Minimum = 2
        GridSlider.Name = "GridSlider"
        GridSlider.Size = New Size(548, 69)
        GridSlider.TabIndex = 6
        GridSlider.Value = 5
        ' 
        ' GridSizeLabel
        ' 
        GridSizeLabel.AutoSize = True
        GridSizeLabel.Location = New Point(803, 116)
        GridSizeLabel.Margin = New Padding(4, 0, 4, 0)
        GridSizeLabel.Name = "GridSizeLabel"
        GridSizeLabel.Size = New Size(144, 30)
        GridSizeLabel.TabIndex = 5
        GridSizeLabel.Text = "GridSizeLabel"
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(53, 116)
        Label4.Margin = New Padding(4, 0, 4, 0)
        Label4.Name = "Label4"
        Label4.Size = New Size(173, 30)
        Label4.TabIndex = 4
        Label4.Text = "Grid Square Size"
        ' 
        ' MaxDepthBar
        ' 
        MaxDepthBar.Location = New Point(229, 32)
        MaxDepthBar.Margin = New Padding(4)
        MaxDepthBar.Maximum = 25
        MaxDepthBar.Minimum = 1
        MaxDepthBar.Name = "MaxDepthBar"
        MaxDepthBar.Size = New Size(548, 69)
        MaxDepthBar.TabIndex = 3
        MaxDepthBar.Value = 5
        ' 
        ' maxCount
        ' 
        maxCount.AutoSize = True
        maxCount.Location = New Point(803, 32)
        maxCount.Margin = New Padding(4, 0, 4, 0)
        maxCount.Name = "maxCount"
        maxCount.Size = New Size(111, 30)
        maxCount.TabIndex = 2
        maxCount.Text = "maxCount"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(20, 32)
        Label2.Margin = New Padding(4, 0, 4, 0)
        Label2.Name = "Label2"
        Label2.Size = New Size(206, 30)
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
        GroupBox2.Location = New Point(1021, 48)
        GroupBox2.Margin = New Padding(4)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Padding = New Padding(4)
        GroupBox2.Size = New Size(428, 514)
        GroupBox2.TabIndex = 2
        GroupBox2.TabStop = False
        GroupBox2.Text = "Miscellaneous Globals"
        ' 
        ' GroupBox3
        ' 
        GroupBox3.Controls.Add(UseMotionMask)
        GroupBox3.Controls.Add(showMotionMask)
        GroupBox3.Location = New Point(17, 410)
        GroupBox3.Margin = New Padding(4)
        GroupBox3.Name = "GroupBox3"
        GroupBox3.Padding = New Padding(4)
        GroupBox3.Size = New Size(299, 104)
        GroupBox3.TabIndex = 10
        GroupBox3.TabStop = False
        GroupBox3.Text = "Motion"
        ' 
        ' UseMotionMask
        ' 
        UseMotionMask.AutoSize = True
        UseMotionMask.Checked = True
        UseMotionMask.CheckState = CheckState.Checked
        UseMotionMask.Location = New Point(23, 62)
        UseMotionMask.Margin = New Padding(4)
        UseMotionMask.Name = "UseMotionMask"
        UseMotionMask.Size = New Size(202, 34)
        UseMotionMask.TabIndex = 1
        UseMotionMask.Text = "Use MotionMask"
        UseMotionMask.UseVisualStyleBackColor = True
        ' 
        ' showMotionMask
        ' 
        showMotionMask.AutoSize = True
        showMotionMask.Location = New Point(23, 30)
        showMotionMask.Margin = New Padding(4)
        showMotionMask.Name = "showMotionMask"
        showMotionMask.Size = New Size(214, 34)
        showMotionMask.TabIndex = 0
        showMotionMask.Text = "Show motion cells"
        showMotionMask.UseVisualStyleBackColor = True
        ' 
        ' TruncateDepth
        ' 
        TruncateDepth.AutoSize = True
        TruncateDepth.Location = New Point(16, 368)
        TruncateDepth.Margin = New Padding(4)
        TruncateDepth.Name = "TruncateDepth"
        TruncateDepth.Size = New Size(314, 34)
        TruncateDepth.TabIndex = 9
        TruncateDepth.Text = "Truncate depth at MaxDepth"
        TruncateDepth.UseVisualStyleBackColor = True
        ' 
        ' gravityPointCloud
        ' 
        gravityPointCloud.AutoSize = True
        gravityPointCloud.Location = New Point(13, 328)
        gravityPointCloud.Margin = New Padding(4)
        gravityPointCloud.Name = "gravityPointCloud"
        gravityPointCloud.Size = New Size(408, 34)
        gravityPointCloud.TabIndex = 8
        gravityPointCloud.Text = "Apply gravity transform to point cloud"
        gravityPointCloud.UseVisualStyleBackColor = True
        ' 
        ' UseKalman
        ' 
        UseKalman.AutoSize = True
        UseKalman.Location = New Point(13, 292)
        UseKalman.Margin = New Padding(4)
        UseKalman.Name = "UseKalman"
        UseKalman.Size = New Size(232, 34)
        UseKalman.TabIndex = 7
        UseKalman.Text = "Use Kalman filtering"
        UseKalman.UseVisualStyleBackColor = True
        ' 
        ' CrossHairs
        ' 
        CrossHairs.AutoSize = True
        CrossHairs.Location = New Point(13, 256)
        CrossHairs.Margin = New Padding(4)
        CrossHairs.Name = "CrossHairs"
        CrossHairs.Size = New Size(192, 34)
        CrossHairs.TabIndex = 6
        CrossHairs.Text = "Show crosshairs"
        CrossHairs.UseVisualStyleBackColor = True
        ' 
        ' CreateGif
        ' 
        CreateGif.AutoSize = True
        CreateGif.Location = New Point(13, 220)
        CreateGif.Margin = New Padding(4)
        CreateGif.Name = "CreateGif"
        CreateGif.Size = New Size(341, 34)
        CreateGif.TabIndex = 5
        CreateGif.Text = "Create GIF of current algorithm"
        CreateGif.UseVisualStyleBackColor = True
        ' 
        ' debugSyncUI
        ' 
        debugSyncUI.AutoSize = True
        debugSyncUI.Location = New Point(13, 184)
        debugSyncUI.Margin = New Padding(4)
        debugSyncUI.Name = "debugSyncUI"
        debugSyncUI.Size = New Size(347, 34)
        debugSyncUI.TabIndex = 4
        debugSyncUI.Text = "Synchronize Debug with Display"
        debugSyncUI.UseVisualStyleBackColor = True
        ' 
        ' ShowGrid
        ' 
        ShowGrid.AutoSize = True
        ShowGrid.Location = New Point(13, 148)
        ShowGrid.Margin = New Padding(4)
        ShowGrid.Name = "ShowGrid"
        ShowGrid.Size = New Size(270, 34)
        ShowGrid.TabIndex = 3
        ShowGrid.Text = "Show grid mask overlay"
        ShowGrid.UseVisualStyleBackColor = True
        ' 
        ' ShowAllOptions
        ' 
        ShowAllOptions.AutoSize = True
        ShowAllOptions.Location = New Point(13, 112)
        ShowAllOptions.Margin = New Padding(4)
        ShowAllOptions.Name = "ShowAllOptions"
        ShowAllOptions.Size = New Size(295, 34)
        ShowAllOptions.TabIndex = 2
        ShowAllOptions.Text = "Show All Options on Open"
        ShowAllOptions.UseVisualStyleBackColor = True
        ' 
        ' displayDst1
        ' 
        displayDst1.AutoSize = True
        displayDst1.Location = New Point(13, 76)
        displayDst1.Margin = New Padding(4)
        displayDst1.Name = "displayDst1"
        displayDst1.Size = New Size(209, 34)
        displayDst1.TabIndex = 1
        displayDst1.Text = "Show dst1 output"
        displayDst1.UseVisualStyleBackColor = True
        ' 
        ' displayDst0
        ' 
        displayDst0.AutoSize = True
        displayDst0.Location = New Point(13, 40)
        displayDst0.Margin = New Padding(4)
        displayDst0.Name = "displayDst0"
        displayDst0.Size = New Size(209, 34)
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
        Geometry.Location = New Point(1020, 562)
        Geometry.Margin = New Padding(4)
        Geometry.Name = "Geometry"
        Geometry.Padding = New Padding(4)
        Geometry.Size = New Size(920, 192)
        Geometry.TabIndex = 3
        Geometry.TabStop = False
        Geometry.Text = "GroupBox4"
        ' 
        ' DotSizeSlider
        ' 
        DotSizeSlider.Location = New Point(248, 110)
        DotSizeSlider.Margin = New Padding(4)
        DotSizeSlider.Minimum = 1
        DotSizeSlider.Name = "DotSizeSlider"
        DotSizeSlider.Size = New Size(548, 69)
        DotSizeSlider.TabIndex = 9
        DotSizeSlider.Value = 5
        ' 
        ' DotSizeLabel
        ' 
        DotSizeLabel.AutoSize = True
        DotSizeLabel.Location = New Point(797, 110)
        DotSizeLabel.Margin = New Padding(4, 0, 4, 0)
        DotSizeLabel.Name = "DotSizeLabel"
        DotSizeLabel.Size = New Size(138, 30)
        DotSizeLabel.TabIndex = 8
        DotSizeLabel.Text = "DotSizeLabel"
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(16, 110)
        Label7.Margin = New Padding(4, 0, 4, 0)
        Label7.Name = "Label7"
        Label7.Size = New Size(176, 30)
        Label7.TabIndex = 7
        Label7.Text = "Dot Size in pixels"
        ' 
        ' LineWidth
        ' 
        LineWidth.Location = New Point(248, 36)
        LineWidth.Margin = New Padding(4)
        LineWidth.Minimum = 1
        LineWidth.Name = "LineWidth"
        LineWidth.Size = New Size(548, 69)
        LineWidth.TabIndex = 6
        LineWidth.Value = 5
        ' 
        ' LineThicknessAmount
        ' 
        LineThicknessAmount.AutoSize = True
        LineThicknessAmount.Location = New Point(797, 36)
        LineThicknessAmount.Margin = New Padding(4, 0, 4, 0)
        LineThicknessAmount.Name = "LineThicknessAmount"
        LineThicknessAmount.Size = New Size(221, 30)
        LineThicknessAmount.TabIndex = 5
        LineThicknessAmount.Text = "LineThicknessAmount"
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(16, 36)
        Label5.Margin = New Padding(4, 0, 4, 0)
        Label5.Name = "Label5"
        Label5.Size = New Size(228, 30)
        Label5.TabIndex = 4
        Label5.Text = "Line thickness in pixels"
        ' 
        ' ColorSource
        ' 
        ColorSource.FormattingEnabled = True
        ColorSource.Location = New Point(1655, 60)
        ColorSource.Margin = New Padding(4)
        ColorSource.Name = "ColorSource"
        ColorSource.Size = New Size(188, 38)
        ColorSource.TabIndex = 4
        ' 
        ' Palettes
        ' 
        Palettes.FormattingEnabled = True
        Palettes.Location = New Point(1655, 106)
        Palettes.Margin = New Padding(4)
        Palettes.Name = "Palettes"
        Palettes.Size = New Size(188, 38)
        Palettes.TabIndex = 5
        ' 
        ' LineType
        ' 
        LineType.FormattingEnabled = True
        LineType.Location = New Point(1655, 154)
        LineType.Margin = New Padding(4)
        LineType.Name = "LineType"
        LineType.Size = New Size(188, 38)
        LineType.TabIndex = 6
        ' 
        ' highlight
        ' 
        highlight.FormattingEnabled = True
        highlight.Location = New Point(1655, 202)
        highlight.Margin = New Padding(4)
        highlight.Name = "highlight"
        highlight.Size = New Size(188, 38)
        highlight.TabIndex = 7
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.Location = New Point(1464, 60)
        Label9.Margin = New Padding(4, 0, 4, 0)
        Label9.Name = "Label9"
        Label9.Size = New Size(176, 30)
        Label9.TabIndex = 21
        Label9.Text = "RedColor Source"
        ' 
        ' Label11
        ' 
        Label11.AutoSize = True
        Label11.Location = New Point(1560, 106)
        Label11.Margin = New Padding(4, 0, 4, 0)
        Label11.Name = "Label11"
        Label11.Size = New Size(78, 30)
        Label11.TabIndex = 22
        Label11.Text = "Palette"
        ' 
        ' Label13
        ' 
        Label13.AutoSize = True
        Label13.Location = New Point(1547, 154)
        Label13.Margin = New Padding(4, 0, 4, 0)
        Label13.Name = "Label13"
        Label13.Size = New Size(105, 30)
        Label13.TabIndex = 23
        Label13.Text = "Line Type"
        ' 
        ' Label14
        ' 
        Label14.AutoSize = True
        Label14.Location = New Point(1477, 198)
        Label14.Margin = New Padding(4, 0, 4, 0)
        Label14.Name = "Label14"
        Label14.Size = New Size(160, 30)
        Label14.TabIndex = 24
        Label14.Text = "Highlight Color"
        ' 
        ' DepthGroupBox
        ' 
        DepthGroupBox.Controls.Add(LRCorrelations)
        DepthGroupBox.Controls.Add(ColorizedDepth)
        DepthGroupBox.Location = New Point(1468, 264)
        DepthGroupBox.Margin = New Padding(4)
        DepthGroupBox.Name = "DepthGroupBox"
        DepthGroupBox.Padding = New Padding(4)
        DepthGroupBox.Size = New Size(264, 128)
        DepthGroupBox.TabIndex = 25
        DepthGroupBox.TabStop = False
        DepthGroupBox.Text = "Depth Display"
        ' 
        ' LRCorrelations
        ' 
        LRCorrelations.AutoSize = True
        LRCorrelations.Location = New Point(11, 84)
        LRCorrelations.Margin = New Padding(4)
        LRCorrelations.Name = "LRCorrelations"
        LRCorrelations.Size = New Size(247, 34)
        LRCorrelations.TabIndex = 1
        LRCorrelations.TabStop = True
        LRCorrelations.Text = "Left/Right Correlation"
        LRCorrelations.UseVisualStyleBackColor = True
        ' 
        ' ColorizedDepth
        ' 
        ColorizedDepth.AutoSize = True
        ColorizedDepth.Location = New Point(11, 42)
        ColorizedDepth.Margin = New Padding(4)
        ColorizedDepth.Name = "ColorizedDepth"
        ColorizedDepth.Size = New Size(196, 34)
        ColorizedDepth.TabIndex = 0
        ColorizedDepth.TabStop = True
        ColorizedDepth.Text = "Colorized Depth"
        ColorizedDepth.UseVisualStyleBackColor = True
        ' 
        ' ColoringGroup
        ' 
        ColoringGroup.Controls.Add(TrackingColor)
        ColoringGroup.Controls.Add(TrackingMeanColor)
        ColoringGroup.Location = New Point(1464, 434)
        ColoringGroup.Margin = New Padding(4)
        ColoringGroup.Name = "ColoringGroup"
        ColoringGroup.Padding = New Padding(4)
        ColoringGroup.Size = New Size(268, 130)
        ColoringGroup.TabIndex = 26
        ColoringGroup.TabStop = False
        ColoringGroup.Text = "RedCloud Display"
        ' 
        ' TrackingColor
        ' 
        TrackingColor.AutoSize = True
        TrackingColor.Location = New Point(17, 80)
        TrackingColor.Margin = New Padding(4)
        TrackingColor.Name = "TrackingColor"
        TrackingColor.Size = New Size(177, 34)
        TrackingColor.TabIndex = 2
        TrackingColor.TabStop = True
        TrackingColor.Text = "Tracking Color"
        TrackingColor.UseVisualStyleBackColor = True
        ' 
        ' TrackingMeanColor
        ' 
        TrackingMeanColor.AutoSize = True
        TrackingMeanColor.Location = New Point(16, 36)
        TrackingMeanColor.Margin = New Padding(4)
        TrackingMeanColor.Name = "TrackingMeanColor"
        TrackingMeanColor.Size = New Size(152, 34)
        TrackingMeanColor.TabIndex = 1
        TrackingMeanColor.TabStop = True
        TrackingMeanColor.Text = "Mean Color"
        TrackingMeanColor.UseVisualStyleBackColor = True
        ' 
        ' OptionsGlobal
        ' 
        AutoScaleDimensions = New SizeF(12F, 30F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1849, 772)
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
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(4)
        Name = "OptionsGlobal"
        Text = "OptionsGlobal"
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        CType(DebugSlider, ComponentModel.ISupportInitialize).EndInit()
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
    Friend WithEvents PixelDiffBar As TrackBar
    Friend WithEvents PixelDiff As Label
    Friend WithEvents Label8 As Label
    Friend WithEvents DebugCheckBox As CheckBox
    Friend WithEvents DebugSlider As TrackBar
    Friend WithEvents DebugSliderLabel As Label
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
    Friend WithEvents UseMotionMask As CheckBox
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
    Friend WithEvents LRCorrelations As RadioButton
    Friend WithEvents ColorizedDepth As RadioButton
    Friend WithEvents TrackingColor As RadioButton
    Friend WithEvents TrackingMeanColor As RadioButton
    Friend WithEvents Label3 As Label
    Friend WithEvents GL_LinearMode As CheckBox
End Class
