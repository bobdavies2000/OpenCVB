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
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.HistogramSettings = New System.Windows.Forms.GroupBox()
        Me.ProjectionThreshold = New System.Windows.Forms.Label()
        Me.ProjectionSlider = New System.Windows.Forms.TrackBar()
        Me.LabelProjection = New System.Windows.Forms.Label()
        Me.HistBinsCount = New System.Windows.Forms.Label()
        Me.HistBinSlider = New System.Windows.Forms.TrackBar()
        Me.HistBins = New System.Windows.Forms.Label()
        Me.resetToDefaults = New System.Windows.Forms.CheckBox()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.GeometrySettings = New System.Windows.Forms.GroupBox()
        Me.LineThicknessAmount = New System.Windows.Forms.Label()
        Me.LineThickness = New System.Windows.Forms.TrackBar()
        Me.LineSizeLabel = New System.Windows.Forms.Label()
        Me.UseKalmanWhenStable = New System.Windows.Forms.CheckBox()
        Me.UseKalman = New System.Windows.Forms.CheckBox()
        Me.PaletteGroup = New System.Windows.Forms.GroupBox()
        Me.FlowLayoutPanel1 = New System.Windows.Forms.FlowLayoutPanel()
        Me.CameraOptions = New System.Windows.Forms.GroupBox()
        Me.LevelThresholdValue = New System.Windows.Forms.Label()
        Me.IMUlevelSlider = New System.Windows.Forms.TrackBar()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.MotionThresholdValue = New System.Windows.Forms.Label()
        Me.IMUmotionSlider = New System.Windows.Forms.TrackBar()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.DotSizeLabel = New System.Windows.Forms.Label()
        Me.dotSizeSlider = New System.Windows.Forms.TrackBar()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.FontSizeLabel = New System.Windows.Forms.Label()
        Me.fontSizeSlider = New System.Windows.Forms.TrackBar()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.MinMaxDepth.SuspendLayout()
        CType(Me.MaxRange, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.HistogramSettings.SuspendLayout()
        CType(Me.ProjectionSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        Me.GeometrySettings.SuspendLayout()
        CType(Me.LineThickness, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.PaletteGroup.SuspendLayout()
        Me.CameraOptions.SuspendLayout()
        CType(Me.IMUlevelSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.IMUmotionSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dotSizeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.fontSizeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'MinMaxDepth
        '
        Me.MinMaxDepth.Controls.Add(Me.maxCount)
        Me.MinMaxDepth.Controls.Add(Me.MaxRange)
        Me.MinMaxDepth.Controls.Add(Me.InrangeMaxLabel)
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
        Me.maxCount.Location = New System.Drawing.Point(736, 37)
        Me.maxCount.Name = "maxCount"
        Me.maxCount.Size = New System.Drawing.Size(81, 20)
        Me.maxCount.TabIndex = 5
        Me.maxCount.Text = "maxCount"
        '
        'MaxRange
        '
        Me.MaxRange.Location = New System.Drawing.Point(212, 30)
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
        Me.InrangeMaxLabel.Location = New System.Drawing.Point(18, 37)
        Me.InrangeMaxLabel.Name = "InrangeMaxLabel"
        Me.InrangeMaxLabel.Size = New System.Drawing.Size(192, 20)
        Me.InrangeMaxLabel.TabIndex = 3
        Me.InrangeMaxLabel.Text = "InRange Max Depth (mm)"
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'HistogramSettings
        '
        Me.HistogramSettings.Controls.Add(Me.ProjectionThreshold)
        Me.HistogramSettings.Controls.Add(Me.ProjectionSlider)
        Me.HistogramSettings.Controls.Add(Me.LabelProjection)
        Me.HistogramSettings.Controls.Add(Me.HistBinsCount)
        Me.HistogramSettings.Controls.Add(Me.HistBinSlider)
        Me.HistogramSettings.Controls.Add(Me.HistBins)
        Me.HistogramSettings.Location = New System.Drawing.Point(12, 243)
        Me.HistogramSettings.Name = "HistogramSettings"
        Me.HistogramSettings.Size = New System.Drawing.Size(829, 195)
        Me.HistogramSettings.TabIndex = 1
        Me.HistogramSettings.TabStop = False
        Me.HistogramSettings.Text = "Histogram Options"
        '
        'ProjectionThreshold
        '
        Me.ProjectionThreshold.AutoSize = True
        Me.ProjectionThreshold.Location = New System.Drawing.Point(737, 112)
        Me.ProjectionThreshold.Name = "ProjectionThreshold"
        Me.ProjectionThreshold.Size = New System.Drawing.Size(149, 20)
        Me.ProjectionThreshold.TabIndex = 8
        Me.ProjectionThreshold.Text = "ProjectionThreshold"
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
        'LabelProjection
        '
        Me.LabelProjection.Location = New System.Drawing.Point(42, 105)
        Me.LabelProjection.Name = "LabelProjection"
        Me.LabelProjection.Size = New System.Drawing.Size(173, 47)
        Me.LabelProjection.TabIndex = 6
        Me.LabelProjection.Text = "Projection threshold"
        Me.LabelProjection.TextAlign = System.Drawing.ContentAlignment.TopRight
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
        Me.HistBinSlider.Minimum = 3
        Me.HistBinSlider.Name = "HistBinSlider"
        Me.HistBinSlider.Size = New System.Drawing.Size(505, 69)
        Me.HistBinSlider.TabIndex = 1
        Me.HistBinSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.HistBinSlider.Value = 3
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
        Me.GroupBox2.Controls.Add(Me.GeometrySettings)
        Me.GroupBox2.Controls.Add(Me.UseKalmanWhenStable)
        Me.GroupBox2.Controls.Add(Me.UseKalman)
        Me.GroupBox2.Location = New System.Drawing.Point(854, 17)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(839, 436)
        Me.GroupBox2.TabIndex = 3
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Miscelaneous Globals"
        '
        'GeometrySettings
        '
        Me.GeometrySettings.Controls.Add(Me.FontSizeLabel)
        Me.GeometrySettings.Controls.Add(Me.fontSizeSlider)
        Me.GeometrySettings.Controls.Add(Me.Label5)
        Me.GeometrySettings.Controls.Add(Me.DotSizeLabel)
        Me.GeometrySettings.Controls.Add(Me.dotSizeSlider)
        Me.GeometrySettings.Controls.Add(Me.Label3)
        Me.GeometrySettings.Controls.Add(Me.LineThicknessAmount)
        Me.GeometrySettings.Controls.Add(Me.LineThickness)
        Me.GeometrySettings.Controls.Add(Me.LineSizeLabel)
        Me.GeometrySettings.Location = New System.Drawing.Point(15, 104)
        Me.GeometrySettings.Name = "GeometrySettings"
        Me.GeometrySettings.Size = New System.Drawing.Size(816, 254)
        Me.GeometrySettings.TabIndex = 2
        Me.GeometrySettings.TabStop = False
        Me.GeometrySettings.Text = "Geometry"
        '
        'LineThicknessAmount
        '
        Me.LineThicknessAmount.AutoSize = True
        Me.LineThicknessAmount.Location = New System.Drawing.Point(714, 36)
        Me.LineThicknessAmount.Name = "LineThicknessAmount"
        Me.LineThicknessAmount.Size = New System.Drawing.Size(166, 20)
        Me.LineThicknessAmount.TabIndex = 5
        Me.LineThicknessAmount.Text = "LineThicknessAmount"
        '
        'LineThickness
        '
        Me.LineThickness.Location = New System.Drawing.Point(207, 25)
        Me.LineThickness.Minimum = 1
        Me.LineThickness.Name = "LineThickness"
        Me.LineThickness.Size = New System.Drawing.Size(505, 69)
        Me.LineThickness.TabIndex = 4
        Me.LineThickness.TickStyle = System.Windows.Forms.TickStyle.None
        Me.LineThickness.Value = 1
        '
        'LineSizeLabel
        '
        Me.LineSizeLabel.AutoSize = True
        Me.LineSizeLabel.Location = New System.Drawing.Point(13, 36)
        Me.LineSizeLabel.Name = "LineSizeLabel"
        Me.LineSizeLabel.Size = New System.Drawing.Size(169, 20)
        Me.LineSizeLabel.TabIndex = 3
        Me.LineSizeLabel.Text = "Line thickness in pixels"
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
        Me.PaletteGroup.Location = New System.Drawing.Point(12, 459)
        Me.PaletteGroup.Name = "PaletteGroup"
        Me.PaletteGroup.Size = New System.Drawing.Size(829, 153)
        Me.PaletteGroup.TabIndex = 5
        Me.PaletteGroup.TabStop = False
        Me.PaletteGroup.Text = "Palette Setting"
        '
        'FlowLayoutPanel1
        '
        Me.FlowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown
        Me.FlowLayoutPanel1.Location = New System.Drawing.Point(6, 25)
        Me.FlowLayoutPanel1.Name = "FlowLayoutPanel1"
        Me.FlowLayoutPanel1.Size = New System.Drawing.Size(817, 113)
        Me.FlowLayoutPanel1.TabIndex = 5
        '
        'CameraOptions
        '
        Me.CameraOptions.Controls.Add(Me.LevelThresholdValue)
        Me.CameraOptions.Controls.Add(Me.IMUlevelSlider)
        Me.CameraOptions.Controls.Add(Me.Label2)
        Me.CameraOptions.Controls.Add(Me.MotionThresholdValue)
        Me.CameraOptions.Controls.Add(Me.IMUmotionSlider)
        Me.CameraOptions.Controls.Add(Me.Label4)
        Me.CameraOptions.Location = New System.Drawing.Point(857, 459)
        Me.CameraOptions.Name = "CameraOptions"
        Me.CameraOptions.Size = New System.Drawing.Size(829, 188)
        Me.CameraOptions.TabIndex = 6
        Me.CameraOptions.TabStop = False
        Me.CameraOptions.Text = "Camera Settings"
        '
        'LevelThresholdValue
        '
        Me.LevelThresholdValue.AutoSize = True
        Me.LevelThresholdValue.Location = New System.Drawing.Point(722, 107)
        Me.LevelThresholdValue.Name = "LevelThresholdValue"
        Me.LevelThresholdValue.Size = New System.Drawing.Size(116, 20)
        Me.LevelThresholdValue.TabIndex = 8
        Me.LevelThresholdValue.Text = "LevelThreshold"
        '
        'IMUlevelSlider
        '
        Me.IMUlevelSlider.Location = New System.Drawing.Point(198, 100)
        Me.IMUlevelSlider.Maximum = 100
        Me.IMUlevelSlider.Name = "IMUlevelSlider"
        Me.IMUlevelSlider.Size = New System.Drawing.Size(505, 69)
        Me.IMUlevelSlider.TabIndex = 7
        Me.IMUlevelSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.IMUlevelSlider.Value = 20
        '
        'Label2
        '
        Me.Label2.Location = New System.Drawing.Point(4, 100)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(196, 44)
        Me.Label2.TabIndex = 6
        Me.Label2.Text = "Level Threshold in IMU (degrees X10)"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'MotionThresholdValue
        '
        Me.MotionThresholdValue.AutoSize = True
        Me.MotionThresholdValue.Location = New System.Drawing.Point(722, 32)
        Me.MotionThresholdValue.Name = "MotionThresholdValue"
        Me.MotionThresholdValue.Size = New System.Drawing.Size(127, 20)
        Me.MotionThresholdValue.TabIndex = 5
        Me.MotionThresholdValue.Text = "MotionThreshold"
        '
        'IMUmotionSlider
        '
        Me.IMUmotionSlider.Location = New System.Drawing.Point(198, 25)
        Me.IMUmotionSlider.Maximum = 20
        Me.IMUmotionSlider.Name = "IMUmotionSlider"
        Me.IMUmotionSlider.Size = New System.Drawing.Size(505, 69)
        Me.IMUmotionSlider.TabIndex = 4
        Me.IMUmotionSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.IMUmotionSlider.Value = 1
        '
        'Label4
        '
        Me.Label4.Location = New System.Drawing.Point(4, 25)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(196, 44)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "Motion Threshold in IMU (radians X100)"
        Me.Label4.TextAlign = System.Drawing.ContentAlignment.TopRight
        '
        'DotSizeLabel
        '
        Me.DotSizeLabel.AutoSize = True
        Me.DotSizeLabel.Location = New System.Drawing.Point(714, 91)
        Me.DotSizeLabel.Name = "DotSizeLabel"
        Me.DotSizeLabel.Size = New System.Drawing.Size(105, 20)
        Me.DotSizeLabel.TabIndex = 8
        Me.DotSizeLabel.Text = "DotSizeLabel"
        '
        'dotSizeSlider
        '
        Me.dotSizeSlider.Location = New System.Drawing.Point(207, 89)
        Me.dotSizeSlider.Minimum = 1
        Me.dotSizeSlider.Name = "dotSizeSlider"
        Me.dotSizeSlider.Size = New System.Drawing.Size(505, 69)
        Me.dotSizeSlider.TabIndex = 7
        Me.dotSizeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.dotSizeSlider.Value = 1
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(13, 94)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(129, 20)
        Me.Label3.TabIndex = 6
        Me.Label3.Text = "Dot Size in pixels"
        '
        'FontSizeLabel
        '
        Me.FontSizeLabel.AutoSize = True
        Me.FontSizeLabel.Location = New System.Drawing.Point(714, 151)
        Me.FontSizeLabel.Name = "FontSizeLabel"
        Me.FontSizeLabel.Size = New System.Drawing.Size(112, 20)
        Me.FontSizeLabel.TabIndex = 11
        Me.FontSizeLabel.Text = "FontSizeLabel"
        '
        'fontSizeSlider
        '
        Me.fontSizeSlider.Location = New System.Drawing.Point(207, 155)
        Me.fontSizeSlider.Maximum = 50
        Me.fontSizeSlider.Minimum = 1
        Me.fontSizeSlider.Name = "fontSizeSlider"
        Me.fontSizeSlider.Size = New System.Drawing.Size(505, 69)
        Me.fontSizeSlider.TabIndex = 10
        Me.fontSizeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.fontSizeSlider.Value = 1
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(13, 160)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(110, 20)
        Me.Label5.TabIndex = 9
        Me.Label5.Text = "Font Size X10"
        '
        'OptionsGlobal
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1700, 822)
        Me.Controls.Add(Me.CameraOptions)
        Me.Controls.Add(Me.PaletteGroup)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.resetToDefaults)
        Me.Controls.Add(Me.HistogramSettings)
        Me.Controls.Add(Me.MinMaxDepth)
        Me.Name = "OptionsGlobal"
        Me.Text = "Options Available to all Algorithms - changes will be remembered across sessions"
        Me.MinMaxDepth.ResumeLayout(False)
        Me.MinMaxDepth.PerformLayout()
        CType(Me.MaxRange, System.ComponentModel.ISupportInitialize).EndInit()
        Me.HistogramSettings.ResumeLayout(False)
        Me.HistogramSettings.PerformLayout()
        CType(Me.ProjectionSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GeometrySettings.ResumeLayout(False)
        Me.GeometrySettings.PerformLayout()
        CType(Me.LineThickness, System.ComponentModel.ISupportInitialize).EndInit()
        Me.PaletteGroup.ResumeLayout(False)
        Me.CameraOptions.ResumeLayout(False)
        Me.CameraOptions.PerformLayout()
        CType(Me.IMUlevelSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.IMUmotionSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dotSizeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.fontSizeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents MinMaxDepth As Windows.Forms.GroupBox
    Friend WithEvents maxCount As Windows.Forms.Label
    Friend WithEvents MaxRange As Windows.Forms.TrackBar
    Friend WithEvents InrangeMaxLabel As Windows.Forms.Label
    Friend WithEvents OpenFileDialog1 As Windows.Forms.OpenFileDialog
    Friend WithEvents HistogramSettings As Windows.Forms.GroupBox
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
    Friend WithEvents LabelProjection As Windows.Forms.Label
    Friend WithEvents CameraOptions As Windows.Forms.GroupBox
    Friend WithEvents LevelThresholdValue As Windows.Forms.Label
    Friend WithEvents IMUlevelSlider As Windows.Forms.TrackBar
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents MotionThresholdValue As Windows.Forms.Label
    Friend WithEvents IMUmotionSlider As Windows.Forms.TrackBar
    Friend WithEvents Label4 As Windows.Forms.Label
    Friend WithEvents GeometrySettings As Windows.Forms.GroupBox
    Friend WithEvents LineThicknessAmount As Windows.Forms.Label
    Friend WithEvents LineThickness As Windows.Forms.TrackBar
    Friend WithEvents LineSizeLabel As Windows.Forms.Label
    Friend WithEvents DotSizeLabel As Windows.Forms.Label
    Friend WithEvents dotSizeSlider As Windows.Forms.TrackBar
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents FontSizeLabel As Windows.Forms.Label
    Friend WithEvents fontSizeSlider As Windows.Forms.TrackBar
    Friend WithEvents Label5 As Windows.Forms.Label
End Class
