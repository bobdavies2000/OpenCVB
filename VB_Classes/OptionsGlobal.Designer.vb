﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OptionsGlobal
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing And components IsNot Nothing Then
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.MinMaxDepth = New System.Windows.Forms.GroupBox()
        Me.DepthDiffLabel = New System.Windows.Forms.Label()
        Me.DepthDiffSlider = New System.Windows.Forms.TrackBar()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.DebugSliderLabel = New System.Windows.Forms.Label()
        Me.DebugSlider = New System.Windows.Forms.TrackBar()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.fHist = New System.Windows.Forms.Label()
        Me.FrameHistory = New System.Windows.Forms.TrackBar()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.PixelDiff = New System.Windows.Forms.Label()
        Me.PixelDiffBar = New System.Windows.Forms.TrackBar()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.DebugCheckBox = New System.Windows.Forms.CheckBox()
        Me.labelBinsCount = New System.Windows.Forms.Label()
        Me.HistBinBar = New System.Windows.Forms.TrackBar()
        Me.labelbins = New System.Windows.Forms.Label()
        Me.GridSizeLabel = New System.Windows.Forms.Label()
        Me.GridSlider = New System.Windows.Forms.TrackBar()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.maxCount = New System.Windows.Forms.Label()
        Me.MaxDepthBar = New System.Windows.Forms.TrackBar()
        Me.InrangeMaxLabel = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.ColorSource = New System.Windows.Forms.ComboBox()
        Me.ColorSourceLabel = New System.Windows.Forms.Label()
        Me.DepthGroupBox = New System.Windows.Forms.GroupBox()
        Me.DepthCorrelations = New System.Windows.Forms.RadioButton()
        Me.ColorizedDepth = New System.Windows.Forms.RadioButton()
        Me.TruncateDepth = New System.Windows.Forms.CheckBox()
        Me.MotionBox = New System.Windows.Forms.GroupBox()
        Me.showMotionMask = New System.Windows.Forms.CheckBox()
        Me.UseMotionMask = New System.Windows.Forms.CheckBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.highlight = New System.Windows.Forms.ComboBox()
        Me.CrossHairs = New System.Windows.Forms.CheckBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.MotionFilteredColorOnly = New System.Windows.Forms.RadioButton()
        Me.MotionFilteredColorAndCloud = New System.Windows.Forms.RadioButton()
        Me.UseHistoryCloud = New System.Windows.Forms.RadioButton()
        Me.MotionFilteredCloudOnly = New System.Windows.Forms.RadioButton()
        Me.unFiltered = New System.Windows.Forms.RadioButton()
        Me.ShowGrid = New System.Windows.Forms.CheckBox()
        Me.debugSyncUI = New System.Windows.Forms.CheckBox()
        Me.UseMultiThreading = New System.Windows.Forms.CheckBox()
        Me.ShowAllOptions = New System.Windows.Forms.CheckBox()
        Me.OpenGLCapture = New System.Windows.Forms.CheckBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.CreateGif = New System.Windows.Forms.CheckBox()
        Me.Palettes = New System.Windows.Forms.ComboBox()
        Me.gravityPointCloud = New System.Windows.Forms.CheckBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.LineType = New System.Windows.Forms.ComboBox()
        Me.displayDst1 = New System.Windows.Forms.CheckBox()
        Me.displayDst0 = New System.Windows.Forms.CheckBox()
        Me.GeometrySettings = New System.Windows.Forms.GroupBox()
        Me.DotSizeLabel = New System.Windows.Forms.Label()
        Me.DotSizeSlider = New System.Windows.Forms.TrackBar()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.LineThicknessAmount = New System.Windows.Forms.Label()
        Me.LineWidth = New System.Windows.Forms.TrackBar()
        Me.LineSizeLabel = New System.Windows.Forms.Label()
        Me.UseKalman = New System.Windows.Forms.CheckBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.ColoringGroup = New System.Windows.Forms.GroupBox()
        Me.TrackingColor = New System.Windows.Forms.RadioButton()
        Me.TrackingMeanColor = New System.Windows.Forms.RadioButton()
        Me.MinMaxDepth.SuspendLayout()
        CType(Me.DepthDiffSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DebugSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.FrameHistory, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PixelDiffBar, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.HistBinBar, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.GridSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.MaxDepthBar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        Me.DepthGroupBox.SuspendLayout()
        Me.MotionBox.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.GeometrySettings.SuspendLayout()
        CType(Me.DotSizeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.LineWidth, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.ColoringGroup.SuspendLayout()
        Me.SuspendLayout()
        '
        'MinMaxDepth
        '
        Me.MinMaxDepth.Controls.Add(Me.DepthDiffLabel)
        Me.MinMaxDepth.Controls.Add(Me.DepthDiffSlider)
        Me.MinMaxDepth.Controls.Add(Me.Label6)
        Me.MinMaxDepth.Controls.Add(Me.DebugSliderLabel)
        Me.MinMaxDepth.Controls.Add(Me.DebugSlider)
        Me.MinMaxDepth.Controls.Add(Me.Label11)
        Me.MinMaxDepth.Controls.Add(Me.fHist)
        Me.MinMaxDepth.Controls.Add(Me.FrameHistory)
        Me.MinMaxDepth.Controls.Add(Me.Label12)
        Me.MinMaxDepth.Controls.Add(Me.PixelDiff)
        Me.MinMaxDepth.Controls.Add(Me.PixelDiffBar)
        Me.MinMaxDepth.Controls.Add(Me.Label7)
        Me.MinMaxDepth.Controls.Add(Me.DebugCheckBox)
        Me.MinMaxDepth.Controls.Add(Me.labelBinsCount)
        Me.MinMaxDepth.Controls.Add(Me.HistBinBar)
        Me.MinMaxDepth.Controls.Add(Me.labelbins)
        Me.MinMaxDepth.Controls.Add(Me.GridSizeLabel)
        Me.MinMaxDepth.Controls.Add(Me.GridSlider)
        Me.MinMaxDepth.Controls.Add(Me.Label9)
        Me.MinMaxDepth.Controls.Add(Me.maxCount)
        Me.MinMaxDepth.Controls.Add(Me.MaxDepthBar)
        Me.MinMaxDepth.Controls.Add(Me.InrangeMaxLabel)
        Me.MinMaxDepth.Location = New System.Drawing.Point(12, 49)
        Me.MinMaxDepth.Name = "MinMaxDepth"
        Me.MinMaxDepth.Size = New System.Drawing.Size(811, 609)
        Me.MinMaxDepth.TabIndex = 0
        Me.MinMaxDepth.TabStop = False
        Me.MinMaxDepth.Text = "Global Sliders"
        '
        'DepthDiffLabel
        '
        Me.DepthDiffLabel.AutoSize = True
        Me.DepthDiffLabel.Location = New System.Drawing.Point(696, 297)
        Me.DepthDiffLabel.Name = "DepthDiffLabel"
        Me.DepthDiffLabel.Size = New System.Drawing.Size(78, 20)
        Me.DepthDiffLabel.TabIndex = 59
        Me.DepthDiffLabel.Text = "DepthDiff"
        '
        'DepthDiffSlider
        '
        Me.DepthDiffSlider.Location = New System.Drawing.Point(185, 297)
        Me.DepthDiffSlider.Maximum = 1000
        Me.DepthDiffSlider.Minimum = 1
        Me.DepthDiffSlider.Name = "DepthDiffSlider"
        Me.DepthDiffSlider.Size = New System.Drawing.Size(506, 69)
        Me.DepthDiffSlider.TabIndex = 58
        Me.DepthDiffSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.DepthDiffSlider.Value = 100
        '
        'Label6
        '
        Me.Label6.Location = New System.Drawing.Point(26, 297)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(136, 55)
        Me.Label6.TabIndex = 57
        Me.Label6.Text = "Depth Difference Threshold (mm's)"
        Me.Label6.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'DebugSliderLabel
        '
        Me.DebugSliderLabel.AutoSize = True
        Me.DebugSliderLabel.Location = New System.Drawing.Point(695, 515)
        Me.DebugSliderLabel.Name = "DebugSliderLabel"
        Me.DebugSliderLabel.Size = New System.Drawing.Size(99, 20)
        Me.DebugSliderLabel.TabIndex = 56
        Me.DebugSliderLabel.Text = "debug Value"
        '
        'DebugSlider
        '
        Me.DebugSlider.Location = New System.Drawing.Point(185, 515)
        Me.DebugSlider.Maximum = 100
        Me.DebugSlider.Minimum = -100
        Me.DebugSlider.Name = "DebugSlider"
        Me.DebugSlider.Size = New System.Drawing.Size(506, 69)
        Me.DebugSlider.TabIndex = 55
        Me.DebugSlider.TickStyle = System.Windows.Forms.TickStyle.None
        '
        'Label11
        '
        Me.Label11.Location = New System.Drawing.Point(26, 515)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(136, 55)
        Me.Label11.TabIndex = 54
        Me.Label11.Text = "DebugSlider"
        Me.Label11.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'fHist
        '
        Me.fHist.AutoSize = True
        Me.fHist.Location = New System.Drawing.Point(696, 363)
        Me.fHist.Name = "fHist"
        Me.fHist.Size = New System.Drawing.Size(47, 20)
        Me.fHist.TabIndex = 53
        Me.fHist.Text = "FHist"
        '
        'FrameHistory
        '
        Me.FrameHistory.Location = New System.Drawing.Point(185, 363)
        Me.FrameHistory.Maximum = 30
        Me.FrameHistory.Minimum = 1
        Me.FrameHistory.Name = "FrameHistory"
        Me.FrameHistory.Size = New System.Drawing.Size(506, 69)
        Me.FrameHistory.TabIndex = 52
        Me.FrameHistory.TickStyle = System.Windows.Forms.TickStyle.None
        Me.FrameHistory.Value = 5
        '
        'Label12
        '
        Me.Label12.Location = New System.Drawing.Point(26, 363)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(136, 55)
        Me.Label12.TabIndex = 51
        Me.Label12.Text = "Frame History"
        Me.Label12.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'PixelDiff
        '
        Me.PixelDiff.AutoSize = True
        Me.PixelDiff.Location = New System.Drawing.Point(695, 240)
        Me.PixelDiff.Name = "PixelDiff"
        Me.PixelDiff.Size = New System.Drawing.Size(59, 20)
        Me.PixelDiff.TabIndex = 50
        Me.PixelDiff.Text = "BPbins"
        '
        'PixelDiffBar
        '
        Me.PixelDiffBar.Location = New System.Drawing.Point(185, 240)
        Me.PixelDiffBar.Maximum = 50
        Me.PixelDiffBar.Name = "PixelDiffBar"
        Me.PixelDiffBar.Size = New System.Drawing.Size(506, 69)
        Me.PixelDiffBar.TabIndex = 49
        Me.PixelDiffBar.TickStyle = System.Windows.Forms.TickStyle.None
        Me.PixelDiffBar.Value = 25
        '
        'Label7
        '
        Me.Label7.Location = New System.Drawing.Point(26, 219)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(136, 55)
        Me.Label7.TabIndex = 48
        Me.Label7.Text = "Color Difference Threshold"
        Me.Label7.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'DebugCheckBox
        '
        Me.DebugCheckBox.Location = New System.Drawing.Point(49, 449)
        Me.DebugCheckBox.Name = "DebugCheckBox"
        Me.DebugCheckBox.Size = New System.Drawing.Size(658, 35)
        Me.DebugCheckBox.TabIndex = 56
        Me.DebugCheckBox.Text = "DebugCheckbox - task.gOptions.DebugChecked - use anywhere as a toggle"
        Me.DebugCheckBox.UseVisualStyleBackColor = True
        '
        'labelBinsCount
        '
        Me.labelBinsCount.AutoSize = True
        Me.labelBinsCount.Location = New System.Drawing.Point(695, 171)
        Me.labelBinsCount.Name = "labelBinsCount"
        Me.labelBinsCount.Size = New System.Drawing.Size(68, 20)
        Me.labelBinsCount.TabIndex = 44
        Me.labelBinsCount.Text = "HistBins"
        '
        'HistBinBar
        '
        Me.HistBinBar.Location = New System.Drawing.Point(185, 171)
        Me.HistBinBar.Maximum = 1000
        Me.HistBinBar.Minimum = 3
        Me.HistBinBar.Name = "HistBinBar"
        Me.HistBinBar.Size = New System.Drawing.Size(506, 69)
        Me.HistBinBar.TabIndex = 43
        Me.HistBinBar.TickStyle = System.Windows.Forms.TickStyle.None
        Me.HistBinBar.Value = 16
        '
        'labelbins
        '
        Me.labelbins.AutoSize = True
        Me.labelbins.Location = New System.Drawing.Point(45, 166)
        Me.labelbins.Name = "labelbins"
        Me.labelbins.Size = New System.Drawing.Size(117, 20)
        Me.labelbins.TabIndex = 42
        Me.labelbins.Text = "Histogram Bins"
        Me.labelbins.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'GridSizeLabel
        '
        Me.GridSizeLabel.AutoSize = True
        Me.GridSizeLabel.Location = New System.Drawing.Point(696, 99)
        Me.GridSizeLabel.Name = "GridSizeLabel"
        Me.GridSizeLabel.Size = New System.Drawing.Size(40, 20)
        Me.GridSizeLabel.TabIndex = 32
        Me.GridSizeLabel.Text = "Size"
        '
        'GridSlider
        '
        Me.GridSlider.Location = New System.Drawing.Point(185, 98)
        Me.GridSlider.Maximum = 128
        Me.GridSlider.Minimum = 2
        Me.GridSlider.Name = "GridSlider"
        Me.GridSlider.Size = New System.Drawing.Size(506, 69)
        Me.GridSlider.TabIndex = 31
        Me.GridSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.GridSlider.Value = 8
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(11, 99)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(130, 20)
        Me.Label9.TabIndex = 30
        Me.Label9.Text = "Grid Square Size"
        Me.Label9.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'maxCount
        '
        Me.maxCount.AutoSize = True
        Me.maxCount.Location = New System.Drawing.Point(695, 35)
        Me.maxCount.Name = "maxCount"
        Me.maxCount.Size = New System.Drawing.Size(81, 20)
        Me.maxCount.TabIndex = 5
        Me.maxCount.Text = "maxCount"
        '
        'MaxDepthBar
        '
        Me.MaxDepthBar.Location = New System.Drawing.Point(185, 29)
        Me.MaxDepthBar.Maximum = 25
        Me.MaxDepthBar.Minimum = 1
        Me.MaxDepthBar.Name = "MaxDepthBar"
        Me.MaxDepthBar.Size = New System.Drawing.Size(506, 69)
        Me.MaxDepthBar.TabIndex = 4
        Me.MaxDepthBar.TickStyle = System.Windows.Forms.TickStyle.None
        Me.MaxDepthBar.Value = 5
        '
        'InrangeMaxLabel
        '
        Me.InrangeMaxLabel.AutoSize = True
        Me.InrangeMaxLabel.Location = New System.Drawing.Point(13, 35)
        Me.InrangeMaxLabel.Name = "InrangeMaxLabel"
        Me.InrangeMaxLabel.Size = New System.Drawing.Size(149, 20)
        Me.InrangeMaxLabel.TabIndex = 3
        Me.InrangeMaxLabel.Text = "Max Depth (meters)"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.ColoringGroup)
        Me.GroupBox2.Controls.Add(Me.ColorSource)
        Me.GroupBox2.Controls.Add(Me.ColorSourceLabel)
        Me.GroupBox2.Controls.Add(Me.DepthGroupBox)
        Me.GroupBox2.Controls.Add(Me.TruncateDepth)
        Me.GroupBox2.Controls.Add(Me.MotionBox)
        Me.GroupBox2.Controls.Add(Me.Label1)
        Me.GroupBox2.Controls.Add(Me.highlight)
        Me.GroupBox2.Controls.Add(Me.CrossHairs)
        Me.GroupBox2.Controls.Add(Me.GroupBox1)
        Me.GroupBox2.Controls.Add(Me.ShowGrid)
        Me.GroupBox2.Controls.Add(Me.debugSyncUI)
        Me.GroupBox2.Controls.Add(Me.UseMultiThreading)
        Me.GroupBox2.Controls.Add(Me.ShowAllOptions)
        Me.GroupBox2.Controls.Add(Me.OpenGLCapture)
        Me.GroupBox2.Controls.Add(Me.Label2)
        Me.GroupBox2.Controls.Add(Me.CreateGif)
        Me.GroupBox2.Controls.Add(Me.Palettes)
        Me.GroupBox2.Controls.Add(Me.gravityPointCloud)
        Me.GroupBox2.Controls.Add(Me.Label5)
        Me.GroupBox2.Controls.Add(Me.LineType)
        Me.GroupBox2.Controls.Add(Me.displayDst1)
        Me.GroupBox2.Controls.Add(Me.displayDst0)
        Me.GroupBox2.Controls.Add(Me.GeometrySettings)
        Me.GroupBox2.Controls.Add(Me.UseKalman)
        Me.GroupBox2.Location = New System.Drawing.Point(829, 49)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(838, 609)
        Me.GroupBox2.TabIndex = 3
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Miscelaneous Globals"
        '
        'ColorSource
        '
        Me.ColorSource.FormattingEnabled = True
        Me.ColorSource.Location = New System.Drawing.Point(528, 55)
        Me.ColorSource.Name = "ColorSource"
        Me.ColorSource.Size = New System.Drawing.Size(222, 28)
        Me.ColorSource.TabIndex = 80
        '
        'ColorSourceLabel
        '
        Me.ColorSourceLabel.AutoSize = True
        Me.ColorSourceLabel.Location = New System.Drawing.Point(384, 58)
        Me.ColorSourceLabel.Name = "ColorSourceLabel"
        Me.ColorSourceLabel.Size = New System.Drawing.Size(131, 20)
        Me.ColorSourceLabel.TabIndex = 79
        Me.ColorSourceLabel.Text = "RedColor Source"
        '
        'DepthGroupBox
        '
        Me.DepthGroupBox.Controls.Add(Me.DepthCorrelations)
        Me.DepthGroupBox.Controls.Add(Me.ColorizedDepth)
        Me.DepthGroupBox.Location = New System.Drawing.Point(332, 219)
        Me.DepthGroupBox.Name = "DepthGroupBox"
        Me.DepthGroupBox.Size = New System.Drawing.Size(194, 118)
        Me.DepthGroupBox.TabIndex = 78
        Me.DepthGroupBox.TabStop = False
        Me.DepthGroupBox.Text = "Depth Display"
        '
        'DepthCorrelations
        '
        Me.DepthCorrelations.AutoSize = True
        Me.DepthCorrelations.Location = New System.Drawing.Point(16, 69)
        Me.DepthCorrelations.Name = "DepthCorrelations"
        Me.DepthCorrelations.Size = New System.Drawing.Size(167, 24)
        Me.DepthCorrelations.TabIndex = 2
        Me.DepthCorrelations.Text = "Depth Correlations"
        Me.DepthCorrelations.UseVisualStyleBackColor = True
        '
        'ColorizedDepth
        '
        Me.ColorizedDepth.AutoSize = True
        Me.ColorizedDepth.Checked = True
        Me.ColorizedDepth.Location = New System.Drawing.Point(16, 35)
        Me.ColorizedDepth.Name = "ColorizedDepth"
        Me.ColorizedDepth.Size = New System.Drawing.Size(148, 24)
        Me.ColorizedDepth.TabIndex = 0
        Me.ColorizedDepth.TabStop = True
        Me.ColorizedDepth.Text = "Colorized Depth"
        Me.ColorizedDepth.UseVisualStyleBackColor = True
        '
        'TruncateDepth
        '
        Me.TruncateDepth.AutoSize = True
        Me.TruncateDepth.Location = New System.Drawing.Point(24, 292)
        Me.TruncateDepth.Name = "TruncateDepth"
        Me.TruncateDepth.Size = New System.Drawing.Size(238, 24)
        Me.TruncateDepth.TabIndex = 77
        Me.TruncateDepth.Text = "Truncate depth at MaxDepth"
        Me.TruncateDepth.UseVisualStyleBackColor = True
        '
        'MotionBox
        '
        Me.MotionBox.Controls.Add(Me.showMotionMask)
        Me.MotionBox.Controls.Add(Me.UseMotionMask)
        Me.MotionBox.Location = New System.Drawing.Point(6, 352)
        Me.MotionBox.Name = "MotionBox"
        Me.MotionBox.Size = New System.Drawing.Size(245, 89)
        Me.MotionBox.TabIndex = 76
        Me.MotionBox.TabStop = False
        Me.MotionBox.Text = "Motion"
        '
        'showMotionMask
        '
        Me.showMotionMask.AutoSize = True
        Me.showMotionMask.Location = New System.Drawing.Point(14, 28)
        Me.showMotionMask.Name = "showMotionMask"
        Me.showMotionMask.Size = New System.Drawing.Size(162, 24)
        Me.showMotionMask.TabIndex = 70
        Me.showMotionMask.Text = "Show motion cells"
        Me.showMotionMask.UseVisualStyleBackColor = True
        '
        'UseMotionMask
        '
        Me.UseMotionMask.AutoSize = True
        Me.UseMotionMask.Checked = True
        Me.UseMotionMask.CheckState = System.Windows.Forms.CheckState.Checked
        Me.UseMotionMask.Location = New System.Drawing.Point(14, 56)
        Me.UseMotionMask.Name = "UseMotionMask"
        Me.UseMotionMask.Size = New System.Drawing.Size(154, 24)
        Me.UseMotionMask.TabIndex = 76
        Me.UseMotionMask.Text = "Use MotionMask"
        Me.UseMotionMask.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(408, 172)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(112, 20)
        Me.Label1.TabIndex = 73
        Me.Label1.Text = "Highlight Color"
        '
        'highlight
        '
        Me.highlight.FormattingEnabled = True
        Me.highlight.Location = New System.Drawing.Point(528, 168)
        Me.highlight.Name = "highlight"
        Me.highlight.Size = New System.Drawing.Size(120, 28)
        Me.highlight.TabIndex = 72
        '
        'CrossHairs
        '
        Me.CrossHairs.AutoSize = True
        Me.CrossHairs.Checked = True
        Me.CrossHairs.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CrossHairs.Location = New System.Drawing.Point(24, 202)
        Me.CrossHairs.Name = "CrossHairs"
        Me.CrossHairs.Size = New System.Drawing.Size(151, 24)
        Me.CrossHairs.TabIndex = 71
        Me.CrossHairs.Text = "Show crosshairs"
        Me.CrossHairs.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.MotionFilteredColorOnly)
        Me.GroupBox1.Controls.Add(Me.MotionFilteredColorAndCloud)
        Me.GroupBox1.Controls.Add(Me.UseHistoryCloud)
        Me.GroupBox1.Controls.Add(Me.MotionFilteredCloudOnly)
        Me.GroupBox1.Controls.Add(Me.unFiltered)
        Me.GroupBox1.Enabled = False
        Me.GroupBox1.Location = New System.Drawing.Point(532, 256)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(306, 186)
        Me.GroupBox1.TabIndex = 69
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Color and PointCloud Input"
        '
        'MotionFilteredColorOnly
        '
        Me.MotionFilteredColorOnly.AutoSize = True
        Me.MotionFilteredColorOnly.Location = New System.Drawing.Point(22, 83)
        Me.MotionFilteredColorOnly.Name = "MotionFilteredColorOnly"
        Me.MotionFilteredColorOnly.Size = New System.Drawing.Size(215, 24)
        Me.MotionFilteredColorOnly.TabIndex = 4
        Me.MotionFilteredColorOnly.TabStop = True
        Me.MotionFilteredColorOnly.Text = "Motion Filtered Color Only"
        Me.MotionFilteredColorOnly.UseVisualStyleBackColor = True
        '
        'MotionFilteredColorAndCloud
        '
        Me.MotionFilteredColorAndCloud.AutoSize = True
        Me.MotionFilteredColorAndCloud.Location = New System.Drawing.Point(22, 112)
        Me.MotionFilteredColorAndCloud.Name = "MotionFilteredColorAndCloud"
        Me.MotionFilteredColorAndCloud.Size = New System.Drawing.Size(256, 24)
        Me.MotionFilteredColorAndCloud.TabIndex = 3
        Me.MotionFilteredColorAndCloud.TabStop = True
        Me.MotionFilteredColorAndCloud.Text = "Motion Filtered Color and Cloud"
        Me.MotionFilteredColorAndCloud.UseVisualStyleBackColor = True
        '
        'UseHistoryCloud
        '
        Me.UseHistoryCloud.AutoSize = True
        Me.UseHistoryCloud.Location = New System.Drawing.Point(22, 145)
        Me.UseHistoryCloud.Name = "UseHistoryCloud"
        Me.UseHistoryCloud.Size = New System.Drawing.Size(244, 24)
        Me.UseHistoryCloud.TabIndex = 2
        Me.UseHistoryCloud.TabStop = True
        Me.UseHistoryCloud.Text = "History PointCloud (averaged)"
        Me.UseHistoryCloud.UseVisualStyleBackColor = True
        '
        'MotionFilteredCloudOnly
        '
        Me.MotionFilteredCloudOnly.AutoSize = True
        Me.MotionFilteredCloudOnly.Location = New System.Drawing.Point(21, 52)
        Me.MotionFilteredCloudOnly.Name = "MotionFilteredCloudOnly"
        Me.MotionFilteredCloudOnly.Size = New System.Drawing.Size(219, 24)
        Me.MotionFilteredCloudOnly.TabIndex = 1
        Me.MotionFilteredCloudOnly.TabStop = True
        Me.MotionFilteredCloudOnly.Text = "Motion Filtered Cloud Only"
        Me.MotionFilteredCloudOnly.UseVisualStyleBackColor = True
        '
        'unFiltered
        '
        Me.unFiltered.AutoSize = True
        Me.unFiltered.Location = New System.Drawing.Point(21, 23)
        Me.unFiltered.Name = "unFiltered"
        Me.unFiltered.Size = New System.Drawing.Size(142, 24)
        Me.unFiltered.TabIndex = 0
        Me.unFiltered.TabStop = True
        Me.unFiltered.Text = "Unfiltered (raw)"
        Me.unFiltered.UseVisualStyleBackColor = True
        '
        'ShowGrid
        '
        Me.ShowGrid.AutoSize = True
        Me.ShowGrid.Location = New System.Drawing.Point(24, 115)
        Me.ShowGrid.Name = "ShowGrid"
        Me.ShowGrid.Size = New System.Drawing.Size(200, 24)
        Me.ShowGrid.TabIndex = 67
        Me.ShowGrid.Text = "Show grid mask overlay"
        Me.ShowGrid.UseVisualStyleBackColor = True
        '
        'debugSyncUI
        '
        Me.debugSyncUI.AutoSize = True
        Me.debugSyncUI.Location = New System.Drawing.Point(24, 143)
        Me.debugSyncUI.Name = "debugSyncUI"
        Me.debugSyncUI.Size = New System.Drawing.Size(261, 24)
        Me.debugSyncUI.TabIndex = 66
        Me.debugSyncUI.Text = "Synchronize Debug with Display"
        Me.debugSyncUI.UseVisualStyleBackColor = True
        '
        'UseMultiThreading
        '
        Me.UseMultiThreading.AutoSize = True
        Me.UseMultiThreading.Location = New System.Drawing.Point(748, 172)
        Me.UseMultiThreading.Name = "UseMultiThreading"
        Me.UseMultiThreading.Size = New System.Drawing.Size(253, 24)
        Me.UseMultiThreading.TabIndex = 65
        Me.UseMultiThreading.Text = "Use Multi-threading (if present)"
        Me.UseMultiThreading.UseVisualStyleBackColor = True
        Me.UseMultiThreading.Visible = False
        '
        'ShowAllOptions
        '
        Me.ShowAllOptions.AutoSize = True
        Me.ShowAllOptions.Location = New System.Drawing.Point(24, 87)
        Me.ShowAllOptions.Name = "ShowAllOptions"
        Me.ShowAllOptions.Size = New System.Drawing.Size(220, 24)
        Me.ShowAllOptions.TabIndex = 64
        Me.ShowAllOptions.Text = "Show All Options on Open"
        Me.ShowAllOptions.UseVisualStyleBackColor = True
        '
        'OpenGLCapture
        '
        Me.OpenGLCapture.AutoSize = True
        Me.OpenGLCapture.Location = New System.Drawing.Point(748, 202)
        Me.OpenGLCapture.Name = "OpenGLCapture"
        Me.OpenGLCapture.Size = New System.Drawing.Size(211, 24)
        Me.OpenGLCapture.TabIndex = 58
        Me.OpenGLCapture.Text = "Capture OpenGL output "
        Me.OpenGLCapture.UseVisualStyleBackColor = True
        Me.OpenGLCapture.Visible = False
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(460, 95)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(59, 20)
        Me.Label2.TabIndex = 55
        Me.Label2.Text = "Palette"
        '
        'CreateGif
        '
        Me.CreateGif.AutoSize = True
        Me.CreateGif.Location = New System.Drawing.Point(24, 171)
        Me.CreateGif.Name = "CreateGif"
        Me.CreateGif.Size = New System.Drawing.Size(256, 24)
        Me.CreateGif.TabIndex = 24
        Me.CreateGif.Text = "Create GIF of current algorithm"
        Me.CreateGif.UseVisualStyleBackColor = True
        '
        'Palettes
        '
        Me.Palettes.FormattingEnabled = True
        Me.Palettes.Location = New System.Drawing.Point(528, 91)
        Me.Palettes.Name = "Palettes"
        Me.Palettes.Size = New System.Drawing.Size(146, 28)
        Me.Palettes.TabIndex = 54
        '
        'gravityPointCloud
        '
        Me.gravityPointCloud.AutoSize = True
        Me.gravityPointCloud.Location = New System.Drawing.Point(24, 262)
        Me.gravityPointCloud.Name = "gravityPointCloud"
        Me.gravityPointCloud.Size = New System.Drawing.Size(294, 24)
        Me.gravityPointCloud.TabIndex = 23
        Me.gravityPointCloud.Text = "Apply gravity transform to point cloud"
        Me.gravityPointCloud.UseVisualStyleBackColor = True
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(441, 134)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(77, 20)
        Me.Label5.TabIndex = 15
        Me.Label5.Text = "Line Type"
        '
        'LineType
        '
        Me.LineType.FormattingEnabled = True
        Me.LineType.Location = New System.Drawing.Point(528, 129)
        Me.LineType.Name = "LineType"
        Me.LineType.Size = New System.Drawing.Size(120, 28)
        Me.LineType.TabIndex = 14
        '
        'displayDst1
        '
        Me.displayDst1.AutoSize = True
        Me.displayDst1.Location = New System.Drawing.Point(24, 59)
        Me.displayDst1.Name = "displayDst1"
        Me.displayDst1.Size = New System.Drawing.Size(160, 24)
        Me.displayDst1.TabIndex = 4
        Me.displayDst1.Text = "Show dst1 output"
        Me.displayDst1.UseVisualStyleBackColor = True
        '
        'displayDst0
        '
        Me.displayDst0.AutoSize = True
        Me.displayDst0.Location = New System.Drawing.Point(24, 31)
        Me.displayDst0.Name = "displayDst0"
        Me.displayDst0.Size = New System.Drawing.Size(160, 24)
        Me.displayDst0.TabIndex = 3
        Me.displayDst0.Text = "Show dst0 output"
        Me.displayDst0.UseVisualStyleBackColor = True
        '
        'GeometrySettings
        '
        Me.GeometrySettings.Controls.Add(Me.DotSizeLabel)
        Me.GeometrySettings.Controls.Add(Me.DotSizeSlider)
        Me.GeometrySettings.Controls.Add(Me.Label3)
        Me.GeometrySettings.Controls.Add(Me.LineThicknessAmount)
        Me.GeometrySettings.Controls.Add(Me.LineWidth)
        Me.GeometrySettings.Controls.Add(Me.LineSizeLabel)
        Me.GeometrySettings.Location = New System.Drawing.Point(4, 448)
        Me.GeometrySettings.Name = "GeometrySettings"
        Me.GeometrySettings.Size = New System.Drawing.Size(816, 155)
        Me.GeometrySettings.TabIndex = 2
        Me.GeometrySettings.TabStop = False
        Me.GeometrySettings.Text = "Geometry"
        '
        'DotSizeLabel
        '
        Me.DotSizeLabel.AutoSize = True
        Me.DotSizeLabel.Location = New System.Drawing.Point(714, 102)
        Me.DotSizeLabel.Name = "DotSizeLabel"
        Me.DotSizeLabel.Size = New System.Drawing.Size(105, 20)
        Me.DotSizeLabel.TabIndex = 8
        Me.DotSizeLabel.Text = "DotSizeLabel"
        '
        'DotSizeSlider
        '
        Me.DotSizeSlider.Location = New System.Drawing.Point(207, 98)
        Me.DotSizeSlider.Maximum = 16
        Me.DotSizeSlider.Minimum = 1
        Me.DotSizeSlider.Name = "DotSizeSlider"
        Me.DotSizeSlider.Size = New System.Drawing.Size(506, 69)
        Me.DotSizeSlider.TabIndex = 7
        Me.DotSizeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.DotSizeSlider.Value = 1
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(14, 105)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(129, 20)
        Me.Label3.TabIndex = 6
        Me.Label3.Text = "Dot Size in pixels"
        '
        'LineThicknessAmount
        '
        Me.LineThicknessAmount.AutoSize = True
        Me.LineThicknessAmount.Location = New System.Drawing.Point(714, 35)
        Me.LineThicknessAmount.Name = "LineThicknessAmount"
        Me.LineThicknessAmount.Size = New System.Drawing.Size(166, 20)
        Me.LineThicknessAmount.TabIndex = 5
        Me.LineThicknessAmount.Text = "LineThicknessAmount"
        '
        'LineWidth
        '
        Me.LineWidth.Location = New System.Drawing.Point(207, 25)
        Me.LineWidth.Minimum = 1
        Me.LineWidth.Name = "LineWidth"
        Me.LineWidth.Size = New System.Drawing.Size(506, 69)
        Me.LineWidth.TabIndex = 4
        Me.LineWidth.TickStyle = System.Windows.Forms.TickStyle.None
        Me.LineWidth.Value = 1
        '
        'LineSizeLabel
        '
        Me.LineSizeLabel.AutoSize = True
        Me.LineSizeLabel.Location = New System.Drawing.Point(14, 35)
        Me.LineSizeLabel.Name = "LineSizeLabel"
        Me.LineSizeLabel.Size = New System.Drawing.Size(169, 20)
        Me.LineSizeLabel.TabIndex = 3
        Me.LineSizeLabel.Text = "Line thickness in pixels"
        '
        'UseKalman
        '
        Me.UseKalman.AutoSize = True
        Me.UseKalman.Location = New System.Drawing.Point(24, 232)
        Me.UseKalman.Name = "UseKalman"
        Me.UseKalman.Size = New System.Drawing.Size(176, 24)
        Me.UseKalman.TabIndex = 0
        Me.UseKalman.Text = "Use Kalman filtering"
        Me.UseKalman.UseVisualStyleBackColor = True
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(18, 9)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(895, 20)
        Me.Label8.TabIndex = 39
        Me.Label8.Text = "All values are restored to their default values at the start of each algorithm.  " &
    "See OptionsGlobal.vb to change any default value."
        Me.Label8.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'ColoringGroup
        '
        Me.ColoringGroup.Controls.Add(Me.TrackingColor)
        Me.ColoringGroup.Controls.Add(Me.TrackingMeanColor)
        Me.ColoringGroup.Location = New System.Drawing.Point(332, 363)
        Me.ColoringGroup.Name = "ColoringGroup"
        Me.ColoringGroup.Size = New System.Drawing.Size(205, 103)
        Me.ColoringGroup.TabIndex = 81
        Me.ColoringGroup.TabStop = False
        Me.ColoringGroup.Text = "RedCloud Output Color"
        '
        'TrackingColor
        '
        Me.TrackingColor.AutoSize = True
        Me.TrackingColor.Location = New System.Drawing.Point(15, 59)
        Me.TrackingColor.Name = "TrackingColor"
        Me.TrackingColor.Size = New System.Drawing.Size(135, 24)
        Me.TrackingColor.TabIndex = 3
        Me.TrackingColor.TabStop = True
        Me.TrackingColor.Text = "Tracking Color"
        Me.TrackingColor.UseVisualStyleBackColor = True
        '
        'TrackingMeanColor
        '
        Me.TrackingMeanColor.AutoSize = True
        Me.TrackingMeanColor.Location = New System.Drawing.Point(15, 29)
        Me.TrackingMeanColor.Name = "TrackingMeanColor"
        Me.TrackingMeanColor.Size = New System.Drawing.Size(115, 24)
        Me.TrackingMeanColor.TabIndex = 0
        Me.TrackingMeanColor.TabStop = True
        Me.TrackingMeanColor.Text = "Mean Color"
        Me.TrackingMeanColor.UseVisualStyleBackColor = True
        '
        'OptionsGlobal
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSize = True
        Me.ClientSize = New System.Drawing.Size(1700, 675)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.MinMaxDepth)
        Me.Name = "OptionsGlobal"
        Me.Text = "All Algorithm Options - Use gOptions variable to access"
        Me.MinMaxDepth.ResumeLayout(False)
        Me.MinMaxDepth.PerformLayout()
        CType(Me.DepthDiffSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DebugSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.FrameHistory, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PixelDiffBar, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.HistBinBar, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.GridSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.MaxDepthBar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.DepthGroupBox.ResumeLayout(False)
        Me.DepthGroupBox.PerformLayout()
        Me.MotionBox.ResumeLayout(False)
        Me.MotionBox.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GeometrySettings.ResumeLayout(False)
        Me.GeometrySettings.PerformLayout()
        CType(Me.DotSizeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.LineWidth, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ColoringGroup.ResumeLayout(False)
        Me.ColoringGroup.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents MinMaxDepth As System.Windows.Forms.GroupBox
    Friend WithEvents maxCount As System.Windows.Forms.Label
    Friend WithEvents MaxDepthBar As System.Windows.Forms.TrackBar
    Friend WithEvents InrangeMaxLabel As System.Windows.Forms.Label
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents UseKalman As System.Windows.Forms.CheckBox
    Friend WithEvents GeometrySettings As System.Windows.Forms.GroupBox
    Friend WithEvents LineThicknessAmount As System.Windows.Forms.Label
    Friend WithEvents LineWidth As System.Windows.Forms.TrackBar
    Friend WithEvents LineSizeLabel As System.Windows.Forms.Label
    Friend WithEvents DotSizeLabel As System.Windows.Forms.Label
    Friend WithEvents DotSizeSlider As System.Windows.Forms.TrackBar
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents displayDst1 As System.Windows.Forms.CheckBox
    Friend WithEvents displayDst0 As System.Windows.Forms.CheckBox
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents LineType As System.Windows.Forms.ComboBox
    Friend WithEvents labelBinsCount As System.Windows.Forms.Label
    Friend WithEvents HistBinBar As System.Windows.Forms.TrackBar
    Friend WithEvents labelbins As System.Windows.Forms.Label
    Friend WithEvents GridSizeLabel As System.Windows.Forms.Label
    Friend WithEvents GridSlider As System.Windows.Forms.TrackBar
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents gravityPointCloud As System.Windows.Forms.CheckBox
    Friend WithEvents CreateGif As System.Windows.Forms.CheckBox
    Friend WithEvents PixelDiff As System.Windows.Forms.Label
    Friend WithEvents PixelDiffBar As System.Windows.Forms.TrackBar
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents fHist As System.Windows.Forms.Label
    Friend WithEvents FrameHistory As System.Windows.Forms.TrackBar
    Friend WithEvents Label12 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Palettes As System.Windows.Forms.ComboBox
    Friend WithEvents DebugCheckBox As System.Windows.Forms.CheckBox
    Friend WithEvents OpenGLCapture As System.Windows.Forms.CheckBox
    Friend WithEvents ShowAllOptions As System.Windows.Forms.CheckBox
    Friend WithEvents UseMultiThreading As System.Windows.Forms.CheckBox
    Friend WithEvents DebugSliderLabel As System.Windows.Forms.Label
    Friend WithEvents DebugSlider As System.Windows.Forms.TrackBar
    Friend WithEvents Label11 As System.Windows.Forms.Label
    Friend WithEvents debugSyncUI As System.Windows.Forms.CheckBox
    Friend WithEvents ShowGrid As System.Windows.Forms.CheckBox
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents UseHistoryCloud As System.Windows.Forms.RadioButton
    Friend WithEvents MotionFilteredCloudOnly As System.Windows.Forms.RadioButton
    Friend WithEvents unFiltered As System.Windows.Forms.RadioButton
    Friend WithEvents MotionFilteredColorOnly As System.Windows.Forms.RadioButton
    Friend WithEvents MotionFilteredColorAndCloud As System.Windows.Forms.RadioButton
    Friend WithEvents showMotionMask As System.Windows.Forms.CheckBox
    Friend WithEvents CrossHairs As System.Windows.Forms.CheckBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents highlight As System.Windows.Forms.ComboBox
    Friend WithEvents UseMotionMask As Windows.Forms.CheckBox
    Friend WithEvents MotionBox As Windows.Forms.GroupBox
    Friend WithEvents TruncateDepth As Windows.Forms.CheckBox
    Friend WithEvents DepthDiffLabel As Windows.Forms.Label
    Friend WithEvents DepthDiffSlider As Windows.Forms.TrackBar
    Friend WithEvents Label6 As Windows.Forms.Label
    Friend WithEvents DepthGroupBox As Windows.Forms.GroupBox
    Friend WithEvents DepthCorrelations As Windows.Forms.RadioButton
    Friend WithEvents ColorizedDepth As Windows.Forms.RadioButton
    Friend WithEvents ColorSource As Windows.Forms.ComboBox
    Friend WithEvents ColorSourceLabel As Windows.Forms.Label
    Friend WithEvents ColoringGroup As Windows.Forms.GroupBox
    Friend WithEvents TrackingColor As Windows.Forms.RadioButton
    Friend WithEvents TrackingMeanColor As Windows.Forms.RadioButton
End Class
