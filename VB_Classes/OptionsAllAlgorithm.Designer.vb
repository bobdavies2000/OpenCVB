﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OptionsAllAlgorithm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.MinMaxDepth = New System.Windows.Forms.GroupBox()
        Me.MinPixels = New System.Windows.Forms.Label()
        Me.minPixelsSlider = New System.Windows.Forms.TrackBar()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.TempSliderLabel = New System.Windows.Forms.Label()
        Me.DebugSlider = New System.Windows.Forms.TrackBar()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.fHist = New System.Windows.Forms.Label()
        Me.FrameHistory = New System.Windows.Forms.TrackBar()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.PixelDiff = New System.Windows.Forms.Label()
        Me.PixelDiffThreshold = New System.Windows.Forms.TrackBar()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.DebugCheckBox = New System.Windows.Forms.CheckBox()
        Me.labelBinsCount = New System.Windows.Forms.Label()
        Me.HistBinSlider = New System.Windows.Forms.TrackBar()
        Me.labelbins = New System.Windows.Forms.Label()
        Me.AddWeighted = New System.Windows.Forms.Label()
        Me.AddWeightedSlider = New System.Windows.Forms.TrackBar()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.ThreadGridSize = New System.Windows.Forms.Label()
        Me.GridSize = New System.Windows.Forms.TrackBar()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.maxCount = New System.Windows.Forms.Label()
        Me.MaxDepth = New System.Windows.Forms.TrackBar()
        Me.InrangeMaxLabel = New System.Windows.Forms.Label()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.useHistoryCloud = New System.Windows.Forms.CheckBox()
        Me.ShowGrid = New System.Windows.Forms.CheckBox()
        Me.SyncOutput = New System.Windows.Forms.CheckBox()
        Me.UseMultiThreading = New System.Windows.Forms.CheckBox()
        Me.ShowAllOptions = New System.Windows.Forms.CheckBox()
        Me.useMotion = New System.Windows.Forms.CheckBox()
        Me.OpenGLCapture = New System.Windows.Forms.CheckBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.CreateGif = New System.Windows.Forms.CheckBox()
        Me.Palettes = New System.Windows.Forms.ComboBox()
        Me.gravityPointCloud = New System.Windows.Forms.CheckBox()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.RGBFilterList = New System.Windows.Forms.ComboBox()
        Me.useFilter = New System.Windows.Forms.CheckBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.LineType = New System.Windows.Forms.ComboBox()
        Me.displayDst1 = New System.Windows.Forms.CheckBox()
        Me.displayDst0 = New System.Windows.Forms.CheckBox()
        Me.GeometrySettings = New System.Windows.Forms.GroupBox()
        Me.DotSizeLabel = New System.Windows.Forms.Label()
        Me.dotSizeSlider = New System.Windows.Forms.TrackBar()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.LineThicknessAmount = New System.Windows.Forms.Label()
        Me.LineWidth = New System.Windows.Forms.TrackBar()
        Me.LineSizeLabel = New System.Windows.Forms.Label()
        Me.UseKalman = New System.Windows.Forms.CheckBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.MinMaxDepth.SuspendLayout()
        CType(Me.minPixelsSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DebugSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.FrameHistory, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PixelDiffThreshold, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.AddWeightedSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.GridSize, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.MaxDepth, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        Me.GeometrySettings.SuspendLayout()
        CType(Me.dotSizeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.LineWidth, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'MinMaxDepth
        '
        Me.MinMaxDepth.Controls.Add(Me.MinPixels)
        Me.MinMaxDepth.Controls.Add(Me.minPixelsSlider)
        Me.MinMaxDepth.Controls.Add(Me.Label13)
        Me.MinMaxDepth.Controls.Add(Me.TempSliderLabel)
        Me.MinMaxDepth.Controls.Add(Me.DebugSlider)
        Me.MinMaxDepth.Controls.Add(Me.Label11)
        Me.MinMaxDepth.Controls.Add(Me.fHist)
        Me.MinMaxDepth.Controls.Add(Me.FrameHistory)
        Me.MinMaxDepth.Controls.Add(Me.Label12)
        Me.MinMaxDepth.Controls.Add(Me.PixelDiff)
        Me.MinMaxDepth.Controls.Add(Me.PixelDiffThreshold)
        Me.MinMaxDepth.Controls.Add(Me.Label7)
        Me.MinMaxDepth.Controls.Add(Me.DebugCheckBox)
        Me.MinMaxDepth.Controls.Add(Me.labelBinsCount)
        Me.MinMaxDepth.Controls.Add(Me.HistBinSlider)
        Me.MinMaxDepth.Controls.Add(Me.labelbins)
        Me.MinMaxDepth.Controls.Add(Me.AddWeighted)
        Me.MinMaxDepth.Controls.Add(Me.AddWeightedSlider)
        Me.MinMaxDepth.Controls.Add(Me.Label6)
        Me.MinMaxDepth.Controls.Add(Me.ThreadGridSize)
        Me.MinMaxDepth.Controls.Add(Me.GridSize)
        Me.MinMaxDepth.Controls.Add(Me.Label9)
        Me.MinMaxDepth.Controls.Add(Me.maxCount)
        Me.MinMaxDepth.Controls.Add(Me.MaxDepth)
        Me.MinMaxDepth.Controls.Add(Me.InrangeMaxLabel)
        Me.MinMaxDepth.Location = New System.Drawing.Point(12, 49)
        Me.MinMaxDepth.Name = "MinMaxDepth"
        Me.MinMaxDepth.Size = New System.Drawing.Size(840, 609)
        Me.MinMaxDepth.TabIndex = 0
        Me.MinMaxDepth.TabStop = False
        Me.MinMaxDepth.Text = "Global Sliders"
        '
        'MinPixels
        '
        Me.MinPixels.AutoSize = True
        Me.MinPixels.Location = New System.Drawing.Point(723, 423)
        Me.MinPixels.Name = "MinPixels"
        Me.MinPixels.Size = New System.Drawing.Size(74, 20)
        Me.MinPixels.TabIndex = 59
        Me.MinPixels.Text = "MinPixels"
        '
        'minPixelsSlider
        '
        Me.minPixelsSlider.Location = New System.Drawing.Point(212, 423)
        Me.minPixelsSlider.Maximum = 2000
        Me.minPixelsSlider.Minimum = 1
        Me.minPixelsSlider.Name = "minPixelsSlider"
        Me.minPixelsSlider.Size = New System.Drawing.Size(506, 69)
        Me.minPixelsSlider.TabIndex = 58
        Me.minPixelsSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.minPixelsSlider.Value = 5
        '
        'Label13
        '
        Me.Label13.Location = New System.Drawing.Point(56, 423)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(136, 55)
        Me.Label13.TabIndex = 57
        Me.Label13.Text = "Min Pixels"
        Me.Label13.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'TempSliderLabel
        '
        Me.TempSliderLabel.AutoSize = True
        Me.TempSliderLabel.Location = New System.Drawing.Point(723, 504)
        Me.TempSliderLabel.Name = "TempSliderLabel"
        Me.TempSliderLabel.Size = New System.Drawing.Size(128, 20)
        Me.TempSliderLabel.TabIndex = 56
        Me.TempSliderLabel.Text = "TempSliderLabel"
        '
        'DebugSlider
        '
        Me.DebugSlider.Location = New System.Drawing.Point(212, 504)
        Me.DebugSlider.Name = "DebugSlider"
        Me.DebugSlider.Size = New System.Drawing.Size(506, 69)
        Me.DebugSlider.TabIndex = 55
        Me.DebugSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.DebugSlider.Value = 5
        '
        'Label11
        '
        Me.Label11.Location = New System.Drawing.Point(56, 504)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(136, 55)
        Me.Label11.TabIndex = 54
        Me.Label11.Text = "DebugSlider"
        Me.Label11.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'fHist
        '
        Me.fHist.AutoSize = True
        Me.fHist.Location = New System.Drawing.Point(723, 348)
        Me.fHist.Name = "fHist"
        Me.fHist.Size = New System.Drawing.Size(47, 20)
        Me.fHist.TabIndex = 53
        Me.fHist.Text = "FHist"
        '
        'FrameHistory
        '
        Me.FrameHistory.Location = New System.Drawing.Point(212, 348)
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
        Me.Label12.Location = New System.Drawing.Point(56, 348)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(136, 55)
        Me.Label12.TabIndex = 51
        Me.Label12.Text = "Frame History"
        Me.Label12.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'PixelDiff
        '
        Me.PixelDiff.AutoSize = True
        Me.PixelDiff.Location = New System.Drawing.Point(723, 272)
        Me.PixelDiff.Name = "PixelDiff"
        Me.PixelDiff.Size = New System.Drawing.Size(59, 20)
        Me.PixelDiff.TabIndex = 50
        Me.PixelDiff.Text = "BPbins"
        '
        'PixelDiffThreshold
        '
        Me.PixelDiffThreshold.Location = New System.Drawing.Point(212, 272)
        Me.PixelDiffThreshold.Maximum = 50
        Me.PixelDiffThreshold.Name = "PixelDiffThreshold"
        Me.PixelDiffThreshold.Size = New System.Drawing.Size(506, 69)
        Me.PixelDiffThreshold.TabIndex = 49
        Me.PixelDiffThreshold.TickStyle = System.Windows.Forms.TickStyle.None
        Me.PixelDiffThreshold.Value = 25
        '
        'Label7
        '
        Me.Label7.Location = New System.Drawing.Point(56, 272)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(136, 55)
        Me.Label7.TabIndex = 48
        Me.Label7.Text = "Pixel Difference Threshold"
        Me.Label7.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'DebugCheckBox
        '
        Me.DebugCheckBox.Location = New System.Drawing.Point(104, 574)
        Me.DebugCheckBox.Name = "DebugCheckBox"
        Me.DebugCheckBox.Size = New System.Drawing.Size(659, 35)
        Me.DebugCheckBox.TabIndex = 56
        Me.DebugCheckBox.Text = "DebugCheckbox - gOptions.DebugCheckBox.checked - use anywhere as a toggle"
        Me.DebugCheckBox.UseVisualStyleBackColor = True
        '
        'labelBinsCount
        '
        Me.labelBinsCount.AutoSize = True
        Me.labelBinsCount.Location = New System.Drawing.Point(723, 206)
        Me.labelBinsCount.Name = "labelBinsCount"
        Me.labelBinsCount.Size = New System.Drawing.Size(68, 20)
        Me.labelBinsCount.TabIndex = 44
        Me.labelBinsCount.Text = "HistBins"
        '
        'HistBinSlider
        '
        Me.HistBinSlider.Location = New System.Drawing.Point(212, 206)
        Me.HistBinSlider.Maximum = 1000
        Me.HistBinSlider.Minimum = 3
        Me.HistBinSlider.Name = "HistBinSlider"
        Me.HistBinSlider.Size = New System.Drawing.Size(506, 69)
        Me.HistBinSlider.TabIndex = 43
        Me.HistBinSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.HistBinSlider.Value = 40
        '
        'labelbins
        '
        Me.labelbins.AutoSize = True
        Me.labelbins.Location = New System.Drawing.Point(76, 206)
        Me.labelbins.Name = "labelbins"
        Me.labelbins.Size = New System.Drawing.Size(117, 20)
        Me.labelbins.TabIndex = 42
        Me.labelbins.Text = "Histogram Bins"
        Me.labelbins.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'AddWeighted
        '
        Me.AddWeighted.AutoSize = True
        Me.AddWeighted.Location = New System.Drawing.Point(723, 154)
        Me.AddWeighted.Name = "AddWeighted"
        Me.AddWeighted.Size = New System.Drawing.Size(88, 20)
        Me.AddWeighted.TabIndex = 41
        Me.AddWeighted.Text = "AddWeight"
        '
        'AddWeightedSlider
        '
        Me.AddWeightedSlider.Location = New System.Drawing.Point(212, 138)
        Me.AddWeightedSlider.Maximum = 100
        Me.AddWeightedSlider.Name = "AddWeightedSlider"
        Me.AddWeightedSlider.Size = New System.Drawing.Size(506, 69)
        Me.AddWeightedSlider.TabIndex = 40
        Me.AddWeightedSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.AddWeightedSlider.Value = 100
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(63, 154)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(128, 20)
        Me.Label6.TabIndex = 39
        Me.Label6.Text = "Add Weighted %"
        '
        'ThreadGridSize
        '
        Me.ThreadGridSize.AutoSize = True
        Me.ThreadGridSize.Location = New System.Drawing.Point(723, 89)
        Me.ThreadGridSize.Name = "ThreadGridSize"
        Me.ThreadGridSize.Size = New System.Drawing.Size(40, 20)
        Me.ThreadGridSize.TabIndex = 32
        Me.ThreadGridSize.Text = "Size"
        '
        'GridSize
        '
        Me.GridSize.Location = New System.Drawing.Point(212, 89)
        Me.GridSize.Maximum = 300
        Me.GridSize.Minimum = 2
        Me.GridSize.Name = "GridSize"
        Me.GridSize.Size = New System.Drawing.Size(506, 69)
        Me.GridSize.TabIndex = 31
        Me.GridSize.TickStyle = System.Windows.Forms.TickStyle.None
        Me.GridSize.Value = 3
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(76, 89)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(130, 20)
        Me.Label9.TabIndex = 30
        Me.Label9.Text = "Grid Square Size"
        Me.Label9.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'maxCount
        '
        Me.maxCount.AutoSize = True
        Me.maxCount.Location = New System.Drawing.Point(723, 35)
        Me.maxCount.Name = "maxCount"
        Me.maxCount.Size = New System.Drawing.Size(81, 20)
        Me.maxCount.TabIndex = 5
        Me.maxCount.Text = "maxCount"
        '
        'MaxDepth
        '
        Me.MaxDepth.Location = New System.Drawing.Point(212, 29)
        Me.MaxDepth.Maximum = 15
        Me.MaxDepth.Minimum = 1
        Me.MaxDepth.Name = "MaxDepth"
        Me.MaxDepth.Size = New System.Drawing.Size(506, 69)
        Me.MaxDepth.TabIndex = 4
        Me.MaxDepth.TickStyle = System.Windows.Forms.TickStyle.None
        Me.MaxDepth.Value = 5
        '
        'InrangeMaxLabel
        '
        Me.InrangeMaxLabel.AutoSize = True
        Me.InrangeMaxLabel.Location = New System.Drawing.Point(64, 35)
        Me.InrangeMaxLabel.Name = "InrangeMaxLabel"
        Me.InrangeMaxLabel.Size = New System.Drawing.Size(149, 20)
        Me.InrangeMaxLabel.TabIndex = 3
        Me.InrangeMaxLabel.Text = "Max Depth (meters)"
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.useHistoryCloud)
        Me.GroupBox2.Controls.Add(Me.ShowGrid)
        Me.GroupBox2.Controls.Add(Me.SyncOutput)
        Me.GroupBox2.Controls.Add(Me.UseMultiThreading)
        Me.GroupBox2.Controls.Add(Me.ShowAllOptions)
        Me.GroupBox2.Controls.Add(Me.useMotion)
        Me.GroupBox2.Controls.Add(Me.OpenGLCapture)
        Me.GroupBox2.Controls.Add(Me.Label2)
        Me.GroupBox2.Controls.Add(Me.CreateGif)
        Me.GroupBox2.Controls.Add(Me.Palettes)
        Me.GroupBox2.Controls.Add(Me.gravityPointCloud)
        Me.GroupBox2.Controls.Add(Me.Label10)
        Me.GroupBox2.Controls.Add(Me.RGBFilterList)
        Me.GroupBox2.Controls.Add(Me.useFilter)
        Me.GroupBox2.Controls.Add(Me.Label5)
        Me.GroupBox2.Controls.Add(Me.LineType)
        Me.GroupBox2.Controls.Add(Me.displayDst1)
        Me.GroupBox2.Controls.Add(Me.displayDst0)
        Me.GroupBox2.Controls.Add(Me.GeometrySettings)
        Me.GroupBox2.Controls.Add(Me.UseKalman)
        Me.GroupBox2.Location = New System.Drawing.Point(854, 49)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(838, 609)
        Me.GroupBox2.TabIndex = 3
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Miscelaneous Globals"
        '
        'useHistoryCloud
        '
        Me.useHistoryCloud.AutoSize = True
        Me.useHistoryCloud.Location = New System.Drawing.Point(24, 235)
        Me.useHistoryCloud.Name = "useHistoryCloud"
        Me.useHistoryCloud.Size = New System.Drawing.Size(388, 24)
        Me.useHistoryCloud.TabIndex = 68
        Me.useHistoryCloud.Text = "Use history point cloud (unchecked use raw cloud)"
        Me.useHistoryCloud.UseVisualStyleBackColor = True
        '
        'ShowGrid
        '
        Me.ShowGrid.AutoSize = True
        Me.ShowGrid.Location = New System.Drawing.Point(24, 118)
        Me.ShowGrid.Name = "ShowGrid"
        Me.ShowGrid.Size = New System.Drawing.Size(105, 24)
        Me.ShowGrid.TabIndex = 67
        Me.ShowGrid.Text = "Show grid"
        Me.ShowGrid.UseVisualStyleBackColor = True
        '
        'SyncOutput
        '
        Me.SyncOutput.AutoSize = True
        Me.SyncOutput.Location = New System.Drawing.Point(24, 176)
        Me.SyncOutput.Name = "SyncOutput"
        Me.SyncOutput.Size = New System.Drawing.Size(259, 24)
        Me.SyncOutput.TabIndex = 66
        Me.SyncOutput.Text = "Synchronize Debug with Output"
        Me.SyncOutput.UseVisualStyleBackColor = True
        '
        'UseMultiThreading
        '
        Me.UseMultiThreading.AutoSize = True
        Me.UseMultiThreading.Location = New System.Drawing.Point(544, 215)
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
        Me.ShowAllOptions.Location = New System.Drawing.Point(24, 89)
        Me.ShowAllOptions.Name = "ShowAllOptions"
        Me.ShowAllOptions.Size = New System.Drawing.Size(220, 24)
        Me.ShowAllOptions.TabIndex = 64
        Me.ShowAllOptions.Text = "Show All Options on Open"
        Me.ShowAllOptions.UseVisualStyleBackColor = True
        '
        'useMotion
        '
        Me.useMotion.AutoSize = True
        Me.useMotion.Location = New System.Drawing.Point(544, 151)
        Me.useMotion.Name = "useMotion"
        Me.useMotion.Size = New System.Drawing.Size(374, 24)
        Me.useMotion.TabIndex = 63
        Me.useMotion.Text = "Use Motion-Filtered Images (MFI) for all images."
        Me.useMotion.UseVisualStyleBackColor = True
        Me.useMotion.Visible = False
        '
        'OpenGLCapture
        '
        Me.OpenGLCapture.AutoSize = True
        Me.OpenGLCapture.Location = New System.Drawing.Point(544, 245)
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
        Me.Label2.Location = New System.Drawing.Point(166, 374)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(59, 20)
        Me.Label2.TabIndex = 55
        Me.Label2.Text = "Palette"
        '
        'CreateGif
        '
        Me.CreateGif.AutoSize = True
        Me.CreateGif.Location = New System.Drawing.Point(24, 205)
        Me.CreateGif.Name = "CreateGif"
        Me.CreateGif.Size = New System.Drawing.Size(256, 24)
        Me.CreateGif.TabIndex = 24
        Me.CreateGif.Text = "Create GIF of current algorithm"
        Me.CreateGif.UseVisualStyleBackColor = True
        '
        'Palettes
        '
        Me.Palettes.FormattingEnabled = True
        Me.Palettes.Location = New System.Drawing.Point(171, 397)
        Me.Palettes.Name = "Palettes"
        Me.Palettes.Size = New System.Drawing.Size(146, 28)
        Me.Palettes.TabIndex = 54
        '
        'gravityPointCloud
        '
        Me.gravityPointCloud.AutoSize = True
        Me.gravityPointCloud.Checked = True
        Me.gravityPointCloud.CheckState = System.Windows.Forms.CheckState.Checked
        Me.gravityPointCloud.Location = New System.Drawing.Point(24, 147)
        Me.gravityPointCloud.Name = "gravityPointCloud"
        Me.gravityPointCloud.Size = New System.Drawing.Size(294, 24)
        Me.gravityPointCloud.TabIndex = 23
        Me.gravityPointCloud.Text = "Apply gravity transform to point cloud"
        Me.gravityPointCloud.UseVisualStyleBackColor = True
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(540, 28)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(92, 20)
        Me.Label10.TabIndex = 20
        Me.Label10.Text = "RGB Filters"
        '
        'RGBFilterList
        '
        Me.RGBFilterList.FormattingEnabled = True
        Me.RGBFilterList.Location = New System.Drawing.Point(544, 52)
        Me.RGBFilterList.Name = "RGBFilterList"
        Me.RGBFilterList.Size = New System.Drawing.Size(272, 28)
        Me.RGBFilterList.TabIndex = 19
        '
        'useFilter
        '
        Me.useFilter.AutoSize = True
        Me.useFilter.Location = New System.Drawing.Point(382, 55)
        Me.useFilter.Name = "useFilter"
        Me.useFilter.Size = New System.Drawing.Size(138, 24)
        Me.useFilter.TabIndex = 16
        Me.useFilter.Text = "Use RGB filter"
        Me.useFilter.UseVisualStyleBackColor = True
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(20, 374)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(77, 20)
        Me.Label5.TabIndex = 15
        Me.Label5.Text = "Line Type"
        '
        'LineType
        '
        Me.LineType.FormattingEnabled = True
        Me.LineType.Location = New System.Drawing.Point(22, 397)
        Me.LineType.Name = "LineType"
        Me.LineType.Size = New System.Drawing.Size(120, 28)
        Me.LineType.TabIndex = 14
        '
        'displayDst1
        '
        Me.displayDst1.AutoSize = True
        Me.displayDst1.Location = New System.Drawing.Point(24, 60)
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
        Me.GeometrySettings.Controls.Add(Me.dotSizeSlider)
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
        'dotSizeSlider
        '
        Me.dotSizeSlider.Location = New System.Drawing.Point(207, 98)
        Me.dotSizeSlider.Minimum = 1
        Me.dotSizeSlider.Name = "dotSizeSlider"
        Me.dotSizeSlider.Size = New System.Drawing.Size(506, 69)
        Me.dotSizeSlider.TabIndex = 7
        Me.dotSizeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.dotSizeSlider.Value = 1
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
        Me.UseKalman.Location = New System.Drawing.Point(544, 275)
        Me.UseKalman.Name = "UseKalman"
        Me.UseKalman.Size = New System.Drawing.Size(176, 24)
        Me.UseKalman.TabIndex = 0
        Me.UseKalman.Text = "Use Kalman filtering"
        Me.UseKalman.UseVisualStyleBackColor = True
        Me.UseKalman.Visible = False
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
        'OptionsAllAlgorithm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSize = True
        Me.ClientSize = New System.Drawing.Size(1700, 675)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.MinMaxDepth)
        Me.Name = "OptionsAllAlgorithm"
        Me.Text = "All Algorithm Options - Use gOptions variable to access"
        Me.MinMaxDepth.ResumeLayout(False)
        Me.MinMaxDepth.PerformLayout()
        CType(Me.minPixelsSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DebugSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.FrameHistory, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PixelDiffThreshold, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.AddWeightedSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.GridSize, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.MaxDepth, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GeometrySettings.ResumeLayout(False)
        Me.GeometrySettings.PerformLayout()
        CType(Me.dotSizeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.LineWidth, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents MinMaxDepth As Windows.Forms.GroupBox
    Friend WithEvents maxCount As Windows.Forms.Label
    Friend WithEvents MaxDepth As Windows.Forms.TrackBar
    Friend WithEvents InrangeMaxLabel As Windows.Forms.Label
    Friend WithEvents OpenFileDialog1 As Windows.Forms.OpenFileDialog
    Friend WithEvents GroupBox2 As Windows.Forms.GroupBox
    Friend WithEvents UseKalman As Windows.Forms.CheckBox
    Friend WithEvents GeometrySettings As Windows.Forms.GroupBox
    Friend WithEvents LineThicknessAmount As Windows.Forms.Label
    Friend WithEvents LineWidth As Windows.Forms.TrackBar
    Friend WithEvents LineSizeLabel As Windows.Forms.Label
    Friend WithEvents DotSizeLabel As Windows.Forms.Label
    Friend WithEvents dotSizeSlider As Windows.Forms.TrackBar
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents displayDst1 As Windows.Forms.CheckBox
    Friend WithEvents displayDst0 As Windows.Forms.CheckBox
    Friend WithEvents Label5 As Windows.Forms.Label
    Friend WithEvents LineType As Windows.Forms.ComboBox
    Friend WithEvents labelBinsCount As Windows.Forms.Label
    Friend WithEvents HistBinSlider As Windows.Forms.TrackBar
    Friend WithEvents labelbins As Windows.Forms.Label
    Friend WithEvents AddWeighted As Windows.Forms.Label
    Friend WithEvents AddWeightedSlider As Windows.Forms.TrackBar
    Friend WithEvents Label6 As Windows.Forms.Label
    Friend WithEvents ThreadGridSize As Windows.Forms.Label
    Friend WithEvents GridSize As Windows.Forms.TrackBar
    Friend WithEvents Label9 As Windows.Forms.Label
    Friend WithEvents Label8 As Windows.Forms.Label
    Friend WithEvents useFilter As Windows.Forms.CheckBox
    Friend WithEvents Label10 As Windows.Forms.Label
    Friend WithEvents RGBFilterList As Windows.Forms.ComboBox
    Friend WithEvents gravityPointCloud As Windows.Forms.CheckBox
    Friend WithEvents CreateGif As Windows.Forms.CheckBox
    Friend WithEvents PixelDiff As Windows.Forms.Label
    Friend WithEvents PixelDiffThreshold As Windows.Forms.TrackBar
    Friend WithEvents Label7 As Windows.Forms.Label
    Friend WithEvents fHist As Windows.Forms.Label
    Friend WithEvents FrameHistory As Windows.Forms.TrackBar
    Friend WithEvents Label12 As Windows.Forms.Label
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents Palettes As Windows.Forms.ComboBox
    Friend WithEvents DebugCheckBox As Windows.Forms.CheckBox
    Friend WithEvents OpenGLCapture As Windows.Forms.CheckBox
    Friend WithEvents useMotion As Windows.Forms.CheckBox
    Friend WithEvents ShowAllOptions As Windows.Forms.CheckBox
    Friend WithEvents UseMultiThreading As Windows.Forms.CheckBox
    Friend WithEvents TempSliderLabel As Windows.Forms.Label
    Friend WithEvents DebugSlider As Windows.Forms.TrackBar
    Friend WithEvents Label11 As Windows.Forms.Label
    Friend WithEvents SyncOutput As Windows.Forms.CheckBox
    Friend WithEvents MinPixels As Windows.Forms.Label
    Friend WithEvents minPixelsSlider As Windows.Forms.TrackBar
    Friend WithEvents Label13 As Windows.Forms.Label
    Friend WithEvents ShowGrid As Windows.Forms.CheckBox
    Friend WithEvents useHistoryCloud As Windows.Forms.CheckBox
End Class