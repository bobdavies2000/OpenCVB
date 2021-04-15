<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OptionsGlobal
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
        Me.maxCount = New System.Windows.Forms.Label()
        Me.MaxRange = New System.Windows.Forms.TrackBar()
        Me.InrangeMaxLabel = New System.Windows.Forms.Label()
        Me.minCount = New System.Windows.Forms.Label()
        Me.MinRange = New System.Windows.Forms.TrackBar()
        Me.InrangeMinLabel = New System.Windows.Forms.Label()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.HistogramSettings = New System.Windows.Forms.GroupBox()
        Me.ProjectionThreshold = New System.Windows.Forms.Label()
        Me.ProjectionSlider = New System.Windows.Forms.TrackBar()
        Me.ProjectionThresh = New System.Windows.Forms.Label()
        Me.IMUmotion = New System.Windows.Forms.Label()
        Me.IMUmotionSlider = New System.Windows.Forms.TrackBar()
        Me.ThresholdLabel = New System.Windows.Forms.Label()
        Me.HistBinsCount = New System.Windows.Forms.Label()
        Me.HistBinSlider = New System.Windows.Forms.TrackBar()
        Me.HistBins = New System.Windows.Forms.Label()
        Me.resetToDefaults = New System.Windows.Forms.CheckBox()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.UseKalmanWhenStable = New System.Windows.Forms.CheckBox()
        Me.UseKalman = New System.Windows.Forms.CheckBox()
        Me.PaletteGroup = New System.Windows.Forms.GroupBox()
        Me.FlowLayoutPanel1 = New System.Windows.Forms.FlowLayoutPanel()
        Me.MinMaxDepth.SuspendLayout()
        CType(Me.MaxRange, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.MinRange, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.HistogramSettings.SuspendLayout()
        CType(Me.ProjectionSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.IMUmotionSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        Me.PaletteGroup.SuspendLayout()
        Me.SuspendLayout()
        '
        'MinMaxDepth
        '
        Me.MinMaxDepth.Controls.Add(Me.maxCount)
        Me.MinMaxDepth.Controls.Add(Me.MaxRange)
        Me.MinMaxDepth.Controls.Add(Me.InrangeMaxLabel)
        Me.MinMaxDepth.Controls.Add(Me.minCount)
        Me.MinMaxDepth.Controls.Add(Me.MinRange)
        Me.MinMaxDepth.Controls.Add(Me.InrangeMinLabel)
        Me.MinMaxDepth.Location = New System.Drawing.Point(12, 63)
        Me.MinMaxDepth.Name = "MinMaxDepth"
        Me.MinMaxDepth.Size = New System.Drawing.Size(829, 169)
        Me.MinMaxDepth.TabIndex = 0
        Me.MinMaxDepth.TabStop = False
        Me.MinMaxDepth.Text = "Global Depth Min/Max"
        '
        'maxCount
        '
        Me.maxCount.AutoSize = True
        Me.maxCount.Location = New System.Drawing.Point(736, 112)
        Me.maxCount.Name = "maxCount"
        Me.maxCount.Size = New System.Drawing.Size(81, 20)
        Me.maxCount.TabIndex = 5
        Me.maxCount.Text = "maxCount"
        '
        'MaxRange
        '
        Me.MaxRange.Location = New System.Drawing.Point(212, 105)
        Me.MaxRange.Maximum = 15000
        Me.MaxRange.Minimum = 200
        Me.MaxRange.Name = "MaxRange"
        Me.MaxRange.Size = New System.Drawing.Size(505, 69)
        Me.MaxRange.TabIndex = 4
        Me.MaxRange.TickStyle = System.Windows.Forms.TickStyle.None
        Me.MaxRange.Value = 200
        '
        'InrangeMaxLabel
        '
        Me.InrangeMaxLabel.AutoSize = True
        Me.InrangeMaxLabel.Location = New System.Drawing.Point(18, 112)
        Me.InrangeMaxLabel.Name = "InrangeMaxLabel"
        Me.InrangeMaxLabel.Size = New System.Drawing.Size(188, 20)
        Me.InrangeMaxLabel.TabIndex = 3
        Me.InrangeMaxLabel.Text = "InRange Min Depth (mm)"
        '
        'minCount
        '
        Me.minCount.AutoSize = True
        Me.minCount.Location = New System.Drawing.Point(737, 36)
        Me.minCount.Name = "minCount"
        Me.minCount.Size = New System.Drawing.Size(77, 20)
        Me.minCount.TabIndex = 2
        Me.minCount.Text = "minCount"
        '
        'MinRange
        '
        Me.MinRange.Location = New System.Drawing.Point(213, 29)
        Me.MinRange.Maximum = 2000
        Me.MinRange.Minimum = 1
        Me.MinRange.Name = "MinRange"
        Me.MinRange.Size = New System.Drawing.Size(505, 69)
        Me.MinRange.TabIndex = 1
        Me.MinRange.TickStyle = System.Windows.Forms.TickStyle.None
        Me.MinRange.Value = 1
        '
        'InrangeMinLabel
        '
        Me.InrangeMinLabel.AutoSize = True
        Me.InrangeMinLabel.Location = New System.Drawing.Point(19, 40)
        Me.InrangeMinLabel.Name = "InrangeMinLabel"
        Me.InrangeMinLabel.Size = New System.Drawing.Size(188, 20)
        Me.InrangeMinLabel.TabIndex = 0
        Me.InrangeMinLabel.Text = "InRange Min Depth (mm)"
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'HistogramSettings
        '
        Me.HistogramSettings.Controls.Add(Me.ProjectionThreshold)
        Me.HistogramSettings.Controls.Add(Me.ProjectionSlider)
        Me.HistogramSettings.Controls.Add(Me.ProjectionThresh)
        Me.HistogramSettings.Controls.Add(Me.IMUmotion)
        Me.HistogramSettings.Controls.Add(Me.IMUmotionSlider)
        Me.HistogramSettings.Controls.Add(Me.ThresholdLabel)
        Me.HistogramSettings.Controls.Add(Me.HistBinsCount)
        Me.HistogramSettings.Controls.Add(Me.HistBinSlider)
        Me.HistogramSettings.Controls.Add(Me.HistBins)
        Me.HistogramSettings.Location = New System.Drawing.Point(12, 243)
        Me.HistogramSettings.Name = "HistogramSettings"
        Me.HistogramSettings.Size = New System.Drawing.Size(829, 285)
        Me.HistogramSettings.TabIndex = 1
        Me.HistogramSettings.TabStop = False
        Me.HistogramSettings.Text = "Histogram Options"
        '
        'ProjectionThreshold
        '
        Me.ProjectionThreshold.AutoSize = True
        Me.ProjectionThreshold.Location = New System.Drawing.Point(737, 112)
        Me.ProjectionThreshold.Name = "ProjectionThreshold"
        Me.ProjectionThreshold.Size = New System.Drawing.Size(57, 20)
        Me.ProjectionThreshold.TabIndex = 8
        Me.ProjectionThreshold.Text = "Label1"
        '
        'ProjectionSlider
        '
        Me.ProjectionSlider.Location = New System.Drawing.Point(213, 105)
        Me.ProjectionSlider.Maximum = 20
        Me.ProjectionSlider.Minimum = 1
        Me.ProjectionSlider.Name = "ProjectionSlider"
        Me.ProjectionSlider.Size = New System.Drawing.Size(505, 69)
        Me.ProjectionSlider.TabIndex = 7
        Me.ProjectionSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.ProjectionSlider.Value = 1
        '
        'ProjectionThresh
        '
        Me.ProjectionThresh.Location = New System.Drawing.Point(42, 105)
        Me.ProjectionThresh.Name = "ProjectionThresh"
        Me.ProjectionThresh.Size = New System.Drawing.Size(173, 47)
        Me.ProjectionThresh.TabIndex = 6
        Me.ProjectionThresh.Text = "Projection threshold"
        Me.ProjectionThresh.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'IMUmotion
        '
        Me.IMUmotion.AutoSize = True
        Me.IMUmotion.Location = New System.Drawing.Point(737, 175)
        Me.IMUmotion.Name = "IMUmotion"
        Me.IMUmotion.Size = New System.Drawing.Size(87, 20)
        Me.IMUmotion.TabIndex = 5
        Me.IMUmotion.Text = "IMUmotion"
        '
        'IMUmotionSlider
        '
        Me.IMUmotionSlider.Location = New System.Drawing.Point(213, 168)
        Me.IMUmotionSlider.Maximum = 20
        Me.IMUmotionSlider.Name = "IMUmotionSlider"
        Me.IMUmotionSlider.Size = New System.Drawing.Size(505, 69)
        Me.IMUmotionSlider.TabIndex = 4
        Me.IMUmotionSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.IMUmotionSlider.Value = 1
        '
        'ThresholdLabel
        '
        Me.ThresholdLabel.Location = New System.Drawing.Point(19, 168)
        Me.ThresholdLabel.Name = "ThresholdLabel"
        Me.ThresholdLabel.Size = New System.Drawing.Size(196, 44)
        Me.ThresholdLabel.TabIndex = 3
        Me.ThresholdLabel.Text = "Threshold in IMU motion in radians X100"
        Me.ThresholdLabel.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'HistBinsCount
        '
        Me.HistBinsCount.AutoSize = True
        Me.HistBinsCount.Location = New System.Drawing.Point(737, 36)
        Me.HistBinsCount.Name = "HistBinsCount"
        Me.HistBinsCount.Size = New System.Drawing.Size(68, 20)
        Me.HistBinsCount.TabIndex = 2
        Me.HistBinsCount.Text = "HistBins"
        '
        'HistBinSlider
        '
        Me.HistBinSlider.Location = New System.Drawing.Point(213, 29)
        Me.HistBinSlider.Maximum = 300
        Me.HistBinSlider.Minimum = 1
        Me.HistBinSlider.Name = "HistBinSlider"
        Me.HistBinSlider.Size = New System.Drawing.Size(505, 69)
        Me.HistBinSlider.TabIndex = 1
        Me.HistBinSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.HistBinSlider.Value = 1
        '
        'HistBins
        '
        Me.HistBins.Location = New System.Drawing.Point(42, 29)
        Me.HistBins.Name = "HistBins"
        Me.HistBins.Size = New System.Drawing.Size(173, 47)
        Me.HistBins.TabIndex = 0
        Me.HistBins.Text = "Bins"
        Me.HistBins.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'resetToDefaults
        '
        Me.resetToDefaults.AutoSize = True
        Me.resetToDefaults.Location = New System.Drawing.Point(12, 8)
        Me.resetToDefaults.Name = "resetToDefaults"
        Me.resetToDefaults.Size = New System.Drawing.Size(837, 24)
        Me.resetToDefaults.TabIndex = 2
        Me.resetToDefaults.Text = "All options on this form are remembered from the previous session.  To reset to t" &
    "heir default values, check this box."
        Me.resetToDefaults.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.UseKalmanWhenStable)
        Me.GroupBox2.Controls.Add(Me.UseKalman)
        Me.GroupBox2.Location = New System.Drawing.Point(854, 17)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(839, 281)
        Me.GroupBox2.TabIndex = 3
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Miscelaneous Globals"
        '
        'UseKalmanWhenStable
        '
        Me.UseKalmanWhenStable.AutoSize = True
        Me.UseKalmanWhenStable.Location = New System.Drawing.Point(23, 60)
        Me.UseKalmanWhenStable.Name = "UseKalmanWhenStable"
        Me.UseKalmanWhenStable.Size = New System.Drawing.Size(341, 24)
        Me.UseKalmanWhenStable.TabIndex = 1
        Me.UseKalmanWhenStable.Text = "Only use Kalman when the camera is stable"
        Me.UseKalmanWhenStable.UseVisualStyleBackColor = True
        '
        'UseKalman
        '
        Me.UseKalman.AutoSize = True
        Me.UseKalman.Location = New System.Drawing.Point(23, 30)
        Me.UseKalman.Name = "UseKalman"
        Me.UseKalman.Size = New System.Drawing.Size(201, 24)
        Me.UseKalman.TabIndex = 0
        Me.UseKalman.Text = "Turn Kalman filtering on"
        Me.UseKalman.UseVisualStyleBackColor = True
        '
        'PaletteGroup
        '
        Me.PaletteGroup.Controls.Add(Me.FlowLayoutPanel1)
        Me.PaletteGroup.Location = New System.Drawing.Point(854, 300)
        Me.PaletteGroup.Name = "PaletteGroup"
        Me.PaletteGroup.Size = New System.Drawing.Size(838, 153)
        Me.PaletteGroup.TabIndex = 5
        Me.PaletteGroup.TabStop = False
        Me.PaletteGroup.Text = "Palette Setting"
        '
        'FlowLayoutPanel1
        '
        Me.FlowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown
        Me.FlowLayoutPanel1.Location = New System.Drawing.Point(6, 25)
        Me.FlowLayoutPanel1.Name = "FlowLayoutPanel1"
        Me.FlowLayoutPanel1.Size = New System.Drawing.Size(826, 113)
        Me.FlowLayoutPanel1.TabIndex = 5
        '
        'OptionsGlobal
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1700, 822)
        Me.Controls.Add(Me.PaletteGroup)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.resetToDefaults)
        Me.Controls.Add(Me.HistogramSettings)
        Me.Controls.Add(Me.MinMaxDepth)
        Me.Name = "OptionsGlobal"
        Me.Text = "Options Available to all Algorithms"
        Me.MinMaxDepth.ResumeLayout(False)
        Me.MinMaxDepth.PerformLayout()
        CType(Me.MaxRange, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.MinRange, System.ComponentModel.ISupportInitialize).EndInit()
        Me.HistogramSettings.ResumeLayout(False)
        Me.HistogramSettings.PerformLayout()
        CType(Me.ProjectionSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.IMUmotionSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.PaletteGroup.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents MinMaxDepth As Windows.Forms.GroupBox
    Friend WithEvents InrangeMinLabel As Windows.Forms.Label
    Friend WithEvents MinRange As Windows.Forms.TrackBar
    Friend WithEvents maxCount As Windows.Forms.Label
    Friend WithEvents MaxRange As Windows.Forms.TrackBar
    Friend WithEvents InrangeMaxLabel As Windows.Forms.Label
    Friend WithEvents minCount As Windows.Forms.Label
    Friend WithEvents OpenFileDialog1 As Windows.Forms.OpenFileDialog
    Friend WithEvents HistogramSettings As Windows.Forms.GroupBox
    Friend WithEvents IMUmotion As Windows.Forms.Label
    Friend WithEvents IMUmotionSlider As Windows.Forms.TrackBar
    Friend WithEvents ThresholdLabel As Windows.Forms.Label
    Friend WithEvents HistBinsCount As Windows.Forms.Label
    Friend WithEvents HistBinSlider As Windows.Forms.TrackBar
    Friend WithEvents HistBins As Windows.Forms.Label
    Friend WithEvents resetToDefaults As Windows.Forms.CheckBox
    Friend WithEvents GroupBox2 As Windows.Forms.GroupBox
    Friend WithEvents UseKalman As Windows.Forms.CheckBox
    Friend WithEvents UseKalmanWhenStable As Windows.Forms.CheckBox
    Friend WithEvents PaletteGroup As Windows.Forms.GroupBox
    Friend WithEvents FlowLayoutPanel1 As Windows.Forms.FlowLayoutPanel
    Friend WithEvents ProjectionThreshold As Windows.Forms.Label
    Friend WithEvents ProjectionSlider As Windows.Forms.TrackBar
    Friend WithEvents ProjectionThresh As Windows.Forms.Label
End Class
