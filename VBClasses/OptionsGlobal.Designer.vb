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
        DotSizeSlider = New TrackBar()
        DotSizeLabel = New Label()
        Label7 = New Label()
        LineWidth = New TrackBar()
        LineThicknessAmount = New Label()
        Label5 = New Label()
        HistBinBar = New TrackBar()
        labelBinsCount = New Label()
        Label6 = New Label()
        GridSlider = New TrackBar()
        GridSizeLabel = New Label()
        Label4 = New Label()
        MaxDepthBar = New TrackBar()
        maxCount = New Label()
        Label2 = New Label()
        Label3 = New Label()
        DebugSlider = New TrackBar()
        DebugSliderLabel = New Label()
        DebugCheckBox = New CheckBox()
        GroupBox2 = New GroupBox()
        UseMotionMask = New CheckBox()
        TruncateDepth = New CheckBox()
        gravityPointCloud = New CheckBox()
        CreateGif = New CheckBox()
        debugSyncUI = New CheckBox()
        ShowAllOptions = New CheckBox()
        displayDst1 = New CheckBox()
        displayDst0 = New CheckBox()
        showMotionMask = New CheckBox()
        CrossHairs = New CheckBox()
        ShowGrid = New CheckBox()
        Palettes = New ComboBox()
        LineType = New ComboBox()
        highlight = New ComboBox()
        Label11 = New Label()
        Label13 = New Label()
        Label14 = New Label()
        DepthGroupBox = New GroupBox()
        GroupBox1.SuspendLayout()
        CType(DotSizeSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(LineWidth, ComponentModel.ISupportInitialize).BeginInit()
        CType(HistBinBar, ComponentModel.ISupportInitialize).BeginInit()
        CType(GridSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(MaxDepthBar, ComponentModel.ISupportInitialize).BeginInit()
        CType(DebugSlider, ComponentModel.ISupportInitialize).BeginInit()
        GroupBox2.SuspendLayout()
        DepthGroupBox.SuspendLayout()
        SuspendLayout()
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(14, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(1231, 30)
        Label1.TabIndex = 0
        Label1.Text = "All values are restored to their default values at the start of each algorithm.  See OptionsGlobal.vb to change any default value."
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(DotSizeSlider)
        GroupBox1.Controls.Add(DotSizeLabel)
        GroupBox1.Controls.Add(Label7)
        GroupBox1.Controls.Add(LineWidth)
        GroupBox1.Controls.Add(LineThicknessAmount)
        GroupBox1.Controls.Add(Label5)
        GroupBox1.Controls.Add(HistBinBar)
        GroupBox1.Controls.Add(labelBinsCount)
        GroupBox1.Controls.Add(Label6)
        GroupBox1.Controls.Add(GridSlider)
        GroupBox1.Controls.Add(GridSizeLabel)
        GroupBox1.Controls.Add(Label4)
        GroupBox1.Controls.Add(MaxDepthBar)
        GroupBox1.Controls.Add(maxCount)
        GroupBox1.Controls.Add(Label2)
        GroupBox1.Location = New Point(14, 34)
        GroupBox1.Margin = New Padding(3, 4, 3, 4)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Padding = New Padding(3, 4, 3, 4)
        GroupBox1.Size = New Size(994, 540)
        GroupBox1.TabIndex = 1
        GroupBox1.TabStop = False
        ' 
        ' DotSizeSlider
        ' 
        DotSizeSlider.Location = New Point(230, 104)
        DotSizeSlider.Margin = New Padding(3, 4, 3, 4)
        DotSizeSlider.Minimum = 1
        DotSizeSlider.Name = "DotSizeSlider"
        DotSizeSlider.Size = New Size(549, 69)
        DotSizeSlider.TabIndex = 9
        DotSizeSlider.TickStyle = TickStyle.None
        DotSizeSlider.Value = 5
        ' 
        ' DotSizeLabel
        ' 
        DotSizeLabel.AutoSize = True
        DotSizeLabel.Location = New Point(778, 108)
        DotSizeLabel.Name = "DotSizeLabel"
        DotSizeLabel.Size = New Size(138, 30)
        DotSizeLabel.TabIndex = 8
        DotSizeLabel.Text = "DotSizeLabel"
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(48, 104)
        Label7.Name = "Label7"
        Label7.Size = New Size(176, 30)
        Label7.TabIndex = 7
        Label7.Text = "Dot Size in pixels"
        ' 
        ' LineWidth
        ' 
        LineWidth.Location = New Point(230, 14)
        LineWidth.Margin = New Padding(3, 4, 3, 4)
        LineWidth.Minimum = 1
        LineWidth.Name = "LineWidth"
        LineWidth.Size = New Size(549, 69)
        LineWidth.TabIndex = 6
        LineWidth.TickStyle = TickStyle.None
        LineWidth.Value = 5
        ' 
        ' LineThicknessAmount
        ' 
        LineThicknessAmount.AutoSize = True
        LineThicknessAmount.Location = New Point(778, 14)
        LineThicknessAmount.Name = "LineThicknessAmount"
        LineThicknessAmount.Size = New Size(221, 30)
        LineThicknessAmount.TabIndex = 5
        LineThicknessAmount.Text = "LineThicknessAmount"
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(-7, 14)
        Label5.Name = "Label5"
        Label5.Size = New Size(228, 30)
        Label5.TabIndex = 4
        Label5.Text = "Line thickness in pixels"
        ' 
        ' HistBinBar
        ' 
        HistBinBar.Location = New Point(230, 374)
        HistBinBar.Margin = New Padding(3, 4, 3, 4)
        HistBinBar.Maximum = 32
        HistBinBar.Minimum = 2
        HistBinBar.Name = "HistBinBar"
        HistBinBar.Size = New Size(549, 69)
        HistBinBar.TabIndex = 9
        HistBinBar.TickStyle = TickStyle.None
        HistBinBar.Value = 16
        ' 
        ' labelBinsCount
        ' 
        labelBinsCount.AutoSize = True
        labelBinsCount.Location = New Point(778, 378)
        labelBinsCount.Name = "labelBinsCount"
        labelBinsCount.Size = New Size(156, 30)
        labelBinsCount.TabIndex = 8
        labelBinsCount.Text = "labelBinsCount"
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(58, 374)
        Label6.Name = "Label6"
        Label6.Size = New Size(159, 30)
        Label6.TabIndex = 7
        Label6.Text = "Histogram Bins"
        ' 
        ' GridSlider
        ' 
        GridSlider.Location = New Point(230, 284)
        GridSlider.Margin = New Padding(3, 4, 3, 4)
        GridSlider.Maximum = 64
        GridSlider.Minimum = 2
        GridSlider.Name = "GridSlider"
        GridSlider.Size = New Size(549, 69)
        GridSlider.TabIndex = 6
        GridSlider.TickStyle = TickStyle.None
        GridSlider.Value = 5
        ' 
        ' GridSizeLabel
        ' 
        GridSizeLabel.AutoSize = True
        GridSizeLabel.Location = New Point(778, 290)
        GridSizeLabel.Name = "GridSizeLabel"
        GridSizeLabel.Size = New Size(144, 30)
        GridSizeLabel.TabIndex = 5
        GridSizeLabel.Text = "GridSizeLabel"
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(57, 284)
        Label4.Name = "Label4"
        Label4.Size = New Size(173, 30)
        Label4.TabIndex = 4
        Label4.Text = "Grid Square Size"
        ' 
        ' MaxDepthBar
        ' 
        MaxDepthBar.Location = New Point(230, 194)
        MaxDepthBar.Margin = New Padding(3, 4, 3, 4)
        MaxDepthBar.Maximum = 25
        MaxDepthBar.Minimum = 1
        MaxDepthBar.Name = "MaxDepthBar"
        MaxDepthBar.Size = New Size(549, 69)
        MaxDepthBar.TabIndex = 3
        MaxDepthBar.TickStyle = TickStyle.None
        MaxDepthBar.Value = 5
        ' 
        ' maxCount
        ' 
        maxCount.AutoSize = True
        maxCount.Location = New Point(778, 194)
        maxCount.Name = "maxCount"
        maxCount.Size = New Size(111, 30)
        maxCount.TabIndex = 2
        maxCount.Text = "maxCount"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(22, 194)
        Label2.Name = "Label2"
        Label2.Size = New Size(206, 30)
        Label2.TabIndex = 1
        Label2.Text = "Max Depth (meters)"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(1034, 432)
        Label3.Name = "Label3"
        Label3.Size = New Size(78, 30)
        Label3.TabIndex = 23
        Label3.Text = "Debug"
        ' 
        ' DebugSlider
        ' 
        DebugSlider.Location = New Point(1130, 432)
        DebugSlider.Margin = New Padding(3, 4, 3, 4)
        DebugSlider.Maximum = 100
        DebugSlider.Minimum = -100
        DebugSlider.Name = "DebugSlider"
        DebugSlider.Size = New Size(549, 69)
        DebugSlider.TabIndex = 22
        DebugSlider.TickStyle = TickStyle.None
        ' 
        ' DebugSliderLabel
        ' 
        DebugSliderLabel.AutoSize = True
        DebugSliderLabel.Location = New Point(1685, 432)
        DebugSliderLabel.Name = "DebugSliderLabel"
        DebugSliderLabel.Size = New Size(78, 30)
        DebugSliderLabel.TabIndex = 21
        DebugSliderLabel.Text = "Debug"
        ' 
        ' DebugCheckBox
        ' 
        DebugCheckBox.AutoSize = True
        DebugCheckBox.Location = New Point(1034, 510)
        DebugCheckBox.Margin = New Padding(3, 4, 3, 4)
        DebugCheckBox.Name = "DebugCheckBox"
        DebugCheckBox.Size = New Size(665, 34)
        DebugCheckBox.TabIndex = 19
        DebugCheckBox.Text = "DebugCheckbox - task.gOptions.DebugChecked - use for testing"
        DebugCheckBox.UseVisualStyleBackColor = True
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Controls.Add(UseMotionMask)
        GroupBox2.Controls.Add(TruncateDepth)
        GroupBox2.Controls.Add(gravityPointCloud)
        GroupBox2.Controls.Add(CreateGif)
        GroupBox2.Controls.Add(debugSyncUI)
        GroupBox2.Controls.Add(ShowAllOptions)
        GroupBox2.Controls.Add(displayDst1)
        GroupBox2.Controls.Add(displayDst0)
        GroupBox2.Location = New Point(1022, 48)
        GroupBox2.Margin = New Padding(3, 4, 3, 4)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Padding = New Padding(3, 4, 3, 4)
        GroupBox2.Size = New Size(429, 352)
        GroupBox2.TabIndex = 2
        GroupBox2.TabStop = False
        GroupBox2.Text = "Miscellaneous Globals"
        ' 
        ' UseMotionMask
        ' 
        UseMotionMask.AutoSize = True
        UseMotionMask.Checked = True
        UseMotionMask.CheckState = CheckState.Checked
        UseMotionMask.Location = New Point(14, 304)
        UseMotionMask.Margin = New Padding(3, 4, 3, 4)
        UseMotionMask.Name = "UseMotionMask"
        UseMotionMask.Size = New Size(208, 34)
        UseMotionMask.TabIndex = 1
        UseMotionMask.Text = "Use Motion Mask"
        UseMotionMask.UseVisualStyleBackColor = True
        ' 
        ' TruncateDepth
        ' 
        TruncateDepth.AutoSize = True
        TruncateDepth.Location = New Point(14, 268)
        TruncateDepth.Margin = New Padding(3, 4, 3, 4)
        TruncateDepth.Name = "TruncateDepth"
        TruncateDepth.Size = New Size(314, 34)
        TruncateDepth.TabIndex = 9
        TruncateDepth.Text = "Truncate depth at MaxDepth"
        TruncateDepth.UseVisualStyleBackColor = True
        ' 
        ' gravityPointCloud
        ' 
        gravityPointCloud.AutoSize = True
        gravityPointCloud.Location = New Point(14, 228)
        gravityPointCloud.Margin = New Padding(3, 4, 3, 4)
        gravityPointCloud.Name = "gravityPointCloud"
        gravityPointCloud.Size = New Size(408, 34)
        gravityPointCloud.TabIndex = 8
        gravityPointCloud.Text = "Apply gravity transform to point cloud"
        gravityPointCloud.UseVisualStyleBackColor = True
        ' 
        ' CreateGif
        ' 
        CreateGif.AutoSize = True
        CreateGif.Location = New Point(14, 192)
        CreateGif.Margin = New Padding(3, 4, 3, 4)
        CreateGif.Name = "CreateGif"
        CreateGif.Size = New Size(341, 34)
        CreateGif.TabIndex = 5
        CreateGif.Text = "Create GIF of current algorithm"
        CreateGif.UseVisualStyleBackColor = True
        ' 
        ' debugSyncUI
        ' 
        debugSyncUI.AutoSize = True
        debugSyncUI.Location = New Point(14, 154)
        debugSyncUI.Margin = New Padding(3, 4, 3, 4)
        debugSyncUI.Name = "debugSyncUI"
        debugSyncUI.Size = New Size(347, 34)
        debugSyncUI.TabIndex = 4
        debugSyncUI.Text = "Synchronize Debug with Display"
        debugSyncUI.UseVisualStyleBackColor = True
        ' 
        ' ShowAllOptions
        ' 
        ShowAllOptions.AutoSize = True
        ShowAllOptions.Location = New Point(14, 116)
        ShowAllOptions.Margin = New Padding(3, 4, 3, 4)
        ShowAllOptions.Name = "ShowAllOptions"
        ShowAllOptions.Size = New Size(295, 34)
        ShowAllOptions.TabIndex = 2
        ShowAllOptions.Text = "Show All Options on Open"
        ShowAllOptions.UseVisualStyleBackColor = True
        ' 
        ' displayDst1
        ' 
        displayDst1.AutoSize = True
        displayDst1.Location = New Point(14, 78)
        displayDst1.Margin = New Padding(3, 4, 3, 4)
        displayDst1.Name = "displayDst1"
        displayDst1.Size = New Size(209, 34)
        displayDst1.TabIndex = 1
        displayDst1.Text = "Show dst1 output"
        displayDst1.UseVisualStyleBackColor = True
        ' 
        ' displayDst0
        ' 
        displayDst0.AutoSize = True
        displayDst0.Location = New Point(14, 40)
        displayDst0.Margin = New Padding(3, 4, 3, 4)
        displayDst0.Name = "displayDst0"
        displayDst0.Size = New Size(209, 34)
        displayDst0.TabIndex = 0
        displayDst0.Text = "Show dst0 output"
        displayDst0.UseVisualStyleBackColor = True
        ' 
        ' showMotionMask
        ' 
        showMotionMask.AutoSize = True
        showMotionMask.Location = New Point(9, 120)
        showMotionMask.Margin = New Padding(3, 4, 3, 4)
        showMotionMask.Name = "showMotionMask"
        showMotionMask.Size = New Size(214, 34)
        showMotionMask.TabIndex = 0
        showMotionMask.Text = "Show motion cells"
        showMotionMask.UseVisualStyleBackColor = True
        ' 
        ' CrossHairs
        ' 
        CrossHairs.AutoSize = True
        CrossHairs.Location = New Point(9, 80)
        CrossHairs.Margin = New Padding(3, 4, 3, 4)
        CrossHairs.Name = "CrossHairs"
        CrossHairs.Size = New Size(192, 34)
        CrossHairs.TabIndex = 6
        CrossHairs.Text = "Show crosshairs"
        CrossHairs.UseVisualStyleBackColor = True
        ' 
        ' ShowGrid
        ' 
        ShowGrid.AutoSize = True
        ShowGrid.Location = New Point(9, 36)
        ShowGrid.Margin = New Padding(3, 4, 3, 4)
        ShowGrid.Name = "ShowGrid"
        ShowGrid.Size = New Size(270, 34)
        ShowGrid.TabIndex = 3
        ShowGrid.Text = "Show grid mask overlay"
        ShowGrid.UseVisualStyleBackColor = True
        ' 
        ' Palettes
        ' 
        Palettes.FormattingEnabled = True
        Palettes.Location = New Point(1478, 36)
        Palettes.Margin = New Padding(3, 4, 3, 4)
        Palettes.Name = "Palettes"
        Palettes.Size = New Size(187, 38)
        Palettes.TabIndex = 5
        ' 
        ' LineType
        ' 
        LineType.FormattingEnabled = True
        LineType.Location = New Point(1478, 80)
        LineType.Margin = New Padding(3, 4, 3, 4)
        LineType.Name = "LineType"
        LineType.Size = New Size(187, 38)
        LineType.TabIndex = 6
        ' 
        ' highlight
        ' 
        highlight.FormattingEnabled = True
        highlight.Location = New Point(1478, 126)
        highlight.Margin = New Padding(3, 4, 3, 4)
        highlight.Name = "highlight"
        highlight.Size = New Size(187, 38)
        highlight.TabIndex = 7
        ' 
        ' Label11
        ' 
        Label11.AutoSize = True
        Label11.Location = New Point(1678, 36)
        Label11.Name = "Label11"
        Label11.Size = New Size(78, 30)
        Label11.TabIndex = 22
        Label11.Text = "Palette"
        ' 
        ' Label13
        ' 
        Label13.AutoSize = True
        Label13.Location = New Point(1678, 84)
        Label13.Name = "Label13"
        Label13.Size = New Size(105, 30)
        Label13.TabIndex = 23
        Label13.Text = "Line Type"
        ' 
        ' Label14
        ' 
        Label14.AutoSize = True
        Label14.Location = New Point(1678, 134)
        Label14.Name = "Label14"
        Label14.Size = New Size(160, 30)
        Label14.TabIndex = 24
        Label14.Text = "Highlight Color"
        ' 
        ' DepthGroupBox
        ' 
        DepthGroupBox.Controls.Add(ShowGrid)
        DepthGroupBox.Controls.Add(showMotionMask)
        DepthGroupBox.Controls.Add(CrossHairs)
        DepthGroupBox.Location = New Point(1478, 184)
        DepthGroupBox.Margin = New Padding(3, 4, 3, 4)
        DepthGroupBox.Name = "DepthGroupBox"
        DepthGroupBox.Padding = New Padding(3, 4, 3, 4)
        DepthGroupBox.Size = New Size(295, 170)
        DepthGroupBox.TabIndex = 25
        DepthGroupBox.TabStop = False
        DepthGroupBox.Text = "Display Masks"
        ' 
        ' OptionsGlobal
        ' 
        AutoScaleDimensions = New SizeF(12F, 30F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1850, 588)
        Controls.Add(DepthGroupBox)
        Controls.Add(Label14)
        Controls.Add(Label3)
        Controls.Add(Label13)
        Controls.Add(DebugSlider)
        Controls.Add(DebugCheckBox)
        Controls.Add(Label11)
        Controls.Add(DebugSliderLabel)
        Controls.Add(highlight)
        Controls.Add(LineType)
        Controls.Add(Palettes)
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        Controls.Add(Label1)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(3, 4, 3, 4)
        Name = "OptionsGlobal"
        Text = "OptionsGlobal"
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        CType(DotSizeSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(LineWidth, ComponentModel.ISupportInitialize).EndInit()
        CType(HistBinBar, ComponentModel.ISupportInitialize).EndInit()
        CType(GridSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(MaxDepthBar, ComponentModel.ISupportInitialize).EndInit()
        CType(DebugSlider, ComponentModel.ISupportInitialize).EndInit()
        GroupBox2.ResumeLayout(False)
        GroupBox2.PerformLayout()
        DepthGroupBox.ResumeLayout(False)
        DepthGroupBox.PerformLayout()
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
    Friend WithEvents CrossHairs As CheckBox
    Friend WithEvents CreateGif As CheckBox
    Friend WithEvents debugSyncUI As CheckBox
    Friend WithEvents showMotionMask As CheckBox
    Friend WithEvents UseMotionMask As CheckBox
    Friend WithEvents LineWidth As TrackBar
    Friend WithEvents LineThicknessAmount As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents DotSizeSlider As TrackBar
    Friend WithEvents DotSizeLabel As Label
    Friend WithEvents Label7 As Label
    Friend WithEvents Palettes As ComboBox
    Friend WithEvents LineType As ComboBox
    Friend WithEvents highlight As ComboBox
    Friend WithEvents Label11 As Label
    Friend WithEvents Label13 As Label
    Friend WithEvents Label14 As Label
    Friend WithEvents DepthGroupBox As GroupBox
    Friend WithEvents Label3 As Label
End Class
