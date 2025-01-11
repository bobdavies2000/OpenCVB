<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OptionsRedCloud
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
        Me.RedCloudSliders = New System.Windows.Forms.GroupBox()
        Me.LabelHistogramBins = New System.Windows.Forms.Label()
        Me.HistBinBar3D = New System.Windows.Forms.TrackBar()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.YLabel = New System.Windows.Forms.Label()
        Me.YRangeSlider = New System.Windows.Forms.TrackBar()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.XLabel = New System.Windows.Forms.Label()
        Me.XRangeBar = New System.Windows.Forms.TrackBar()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.SideLabel = New System.Windows.Forms.Label()
        Me.ProjectionThresholdBar = New System.Windows.Forms.TrackBar()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.ReductionTypeGroup = New System.Windows.Forms.GroupBox()
        Me.NoReduction = New System.Windows.Forms.RadioButton()
        Me.BitwiseReduction = New System.Windows.Forms.RadioButton()
        Me.UseSimpleReduction = New System.Windows.Forms.RadioButton()
        Me.ReductionSliders = New System.Windows.Forms.GroupBox()
        Me.bitwiseLabel = New System.Windows.Forms.Label()
        Me.BitwiseReductionBar = New System.Windows.Forms.TrackBar()
        Me.reduceXbits = New System.Windows.Forms.Label()
        Me.ColorLabel = New System.Windows.Forms.Label()
        Me.SimpleReductionBar = New System.Windows.Forms.TrackBar()
        Me.SimpleReduceLabel = New System.Windows.Forms.Label()
        Me.RedCloudOnly = New System.Windows.Forms.GroupBox()
        Me.XYZReduction = New System.Windows.Forms.RadioButton()
        Me.YZReduction = New System.Windows.Forms.RadioButton()
        Me.XZReduction = New System.Windows.Forms.RadioButton()
        Me.XYReduction = New System.Windows.Forms.RadioButton()
        Me.ZReduction = New System.Windows.Forms.RadioButton()
        Me.YReduction = New System.Windows.Forms.RadioButton()
        Me.XReduction = New System.Windows.Forms.RadioButton()
        Me.RedCloudType = New System.Windows.Forms.GroupBox()
        Me.UseGuidedProjection = New System.Windows.Forms.RadioButton()
        Me.UseDepth = New System.Windows.Forms.RadioButton()
        Me.UseColorOnly = New System.Windows.Forms.RadioButton()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.minCellSizeSlider = New System.Windows.Forms.TrackBar()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.labelCellSize = New System.Windows.Forms.Label()
        Me.LabelIdentify = New System.Windows.Forms.Label()
        Me.IdentifyCountBar = New System.Windows.Forms.TrackBar()
        Me.IdentifyCountLabel = New System.Windows.Forms.Label()
        Me.ColorSourceLabel = New System.Windows.Forms.Label()
        Me.ColorSource = New System.Windows.Forms.ComboBox()
        Me.DisplayCellStats = New System.Windows.Forms.CheckBox()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.IdentifyCells = New System.Windows.Forms.CheckBox()
        Me.ColoringGroup = New System.Windows.Forms.GroupBox()
        Me.ColorTrackingDepth = New System.Windows.Forms.RadioButton()
        Me.ColorTracking = New System.Windows.Forms.RadioButton()
        Me.ColorMean = New System.Windows.Forms.RadioButton()
        Me.ColorDepth = New System.Windows.Forms.RadioButton()
        Me.RedCloudSliders.SuspendLayout()
        CType(Me.HistBinBar3D, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.YRangeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.XRangeBar, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.ProjectionThresholdBar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.ReductionTypeGroup.SuspendLayout()
        Me.ReductionSliders.SuspendLayout()
        CType(Me.BitwiseReductionBar, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.SimpleReductionBar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.RedCloudOnly.SuspendLayout()
        Me.RedCloudType.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        CType(Me.minCellSizeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.IdentifyCountBar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.ColoringGroup.SuspendLayout()
        Me.SuspendLayout()
        '
        'RedCloudSliders
        '
        Me.RedCloudSliders.Controls.Add(Me.LabelHistogramBins)
        Me.RedCloudSliders.Controls.Add(Me.HistBinBar3D)
        Me.RedCloudSliders.Controls.Add(Me.Label7)
        Me.RedCloudSliders.Controls.Add(Me.YLabel)
        Me.RedCloudSliders.Controls.Add(Me.YRangeSlider)
        Me.RedCloudSliders.Controls.Add(Me.Label4)
        Me.RedCloudSliders.Controls.Add(Me.XLabel)
        Me.RedCloudSliders.Controls.Add(Me.XRangeBar)
        Me.RedCloudSliders.Controls.Add(Me.Label2)
        Me.RedCloudSliders.Location = New System.Drawing.Point(12, 318)
        Me.RedCloudSliders.Name = "RedCloudSliders"
        Me.RedCloudSliders.Size = New System.Drawing.Size(763, 242)
        Me.RedCloudSliders.TabIndex = 2
        Me.RedCloudSliders.TabStop = False
        '
        'LabelHistogramBins
        '
        Me.LabelHistogramBins.AutoSize = True
        Me.LabelHistogramBins.Location = New System.Drawing.Point(668, 22)
        Me.LabelHistogramBins.Name = "LabelHistogramBins"
        Me.LabelHistogramBins.Size = New System.Drawing.Size(57, 20)
        Me.LabelHistogramBins.TabIndex = 32
        Me.LabelHistogramBins.Text = "Label5"
        '
        'HistBinBar3D
        '
        Me.HistBinBar3D.Location = New System.Drawing.Point(156, 15)
        Me.HistBinBar3D.Maximum = 16
        Me.HistBinBar3D.Minimum = 2
        Me.HistBinBar3D.Name = "HistBinBar3D"
        Me.HistBinBar3D.Size = New System.Drawing.Size(506, 69)
        Me.HistBinBar3D.TabIndex = 31
        Me.HistBinBar3D.TickStyle = System.Windows.Forms.TickStyle.None
        Me.HistBinBar3D.Value = 4
        '
        'Label7
        '
        Me.Label7.Location = New System.Drawing.Point(8, 22)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(152, 48)
        Me.Label7.TabIndex = 30
        Me.Label7.Text = "3D Histogram Bins"
        '
        'YLabel
        '
        Me.YLabel.AutoSize = True
        Me.YLabel.Location = New System.Drawing.Point(668, 168)
        Me.YLabel.Name = "YLabel"
        Me.YLabel.Size = New System.Drawing.Size(57, 20)
        Me.YLabel.TabIndex = 14
        Me.YLabel.Text = "Label3"
        '
        'YRangeSlider
        '
        Me.YRangeSlider.Location = New System.Drawing.Point(156, 161)
        Me.YRangeSlider.Maximum = 1000
        Me.YRangeSlider.Minimum = 3
        Me.YRangeSlider.Name = "YRangeSlider"
        Me.YRangeSlider.Size = New System.Drawing.Size(506, 69)
        Me.YRangeSlider.TabIndex = 13
        Me.YRangeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.YRangeSlider.Value = 5
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(8, 168)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(115, 20)
        Me.Label4.TabIndex = 12
        Me.Label4.Text = "Y-Range X100"
        '
        'XLabel
        '
        Me.XLabel.AutoSize = True
        Me.XLabel.Location = New System.Drawing.Point(668, 94)
        Me.XLabel.Name = "XLabel"
        Me.XLabel.Size = New System.Drawing.Size(57, 20)
        Me.XLabel.TabIndex = 11
        Me.XLabel.Text = "Label1"
        '
        'XRangeBar
        '
        Me.XRangeBar.Location = New System.Drawing.Point(156, 88)
        Me.XRangeBar.Maximum = 1000
        Me.XRangeBar.Minimum = 3
        Me.XRangeBar.Name = "XRangeBar"
        Me.XRangeBar.Size = New System.Drawing.Size(506, 69)
        Me.XRangeBar.TabIndex = 10
        Me.XRangeBar.TickStyle = System.Windows.Forms.TickStyle.None
        Me.XRangeBar.Value = 5
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(8, 94)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(115, 20)
        Me.Label2.TabIndex = 9
        Me.Label2.Text = "X-Range X100"
        '
        'SideLabel
        '
        Me.SideLabel.AutoSize = True
        Me.SideLabel.Location = New System.Drawing.Point(668, 102)
        Me.SideLabel.Name = "SideLabel"
        Me.SideLabel.Size = New System.Drawing.Size(57, 20)
        Me.SideLabel.TabIndex = 17
        Me.SideLabel.Text = "Label7"
        '
        'ProjectionThresholdBar
        '
        Me.ProjectionThresholdBar.Location = New System.Drawing.Point(180, 96)
        Me.ProjectionThresholdBar.Maximum = 200
        Me.ProjectionThresholdBar.Name = "ProjectionThresholdBar"
        Me.ProjectionThresholdBar.Size = New System.Drawing.Size(482, 69)
        Me.ProjectionThresholdBar.TabIndex = 16
        Me.ProjectionThresholdBar.TickStyle = System.Windows.Forms.TickStyle.None
        Me.ProjectionThresholdBar.Value = 10
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(21, 96)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(153, 20)
        Me.Label8.TabIndex = 15
        Me.Label8.Text = "Projection Threshold"
        '
        'ReductionTypeGroup
        '
        Me.ReductionTypeGroup.Controls.Add(Me.NoReduction)
        Me.ReductionTypeGroup.Controls.Add(Me.BitwiseReduction)
        Me.ReductionTypeGroup.Controls.Add(Me.UseSimpleReduction)
        Me.ReductionTypeGroup.Location = New System.Drawing.Point(792, 318)
        Me.ReductionTypeGroup.Name = "ReductionTypeGroup"
        Me.ReductionTypeGroup.Size = New System.Drawing.Size(220, 129)
        Me.ReductionTypeGroup.TabIndex = 5
        Me.ReductionTypeGroup.TabStop = False
        Me.ReductionTypeGroup.Text = "Reduction Options"
        '
        'NoReduction
        '
        Me.NoReduction.AutoSize = True
        Me.NoReduction.Location = New System.Drawing.Point(15, 89)
        Me.NoReduction.Name = "NoReduction"
        Me.NoReduction.Size = New System.Drawing.Size(131, 24)
        Me.NoReduction.TabIndex = 4
        Me.NoReduction.TabStop = True
        Me.NoReduction.Text = "No Reduction"
        Me.NoReduction.UseVisualStyleBackColor = True
        '
        'BitwiseReduction
        '
        Me.BitwiseReduction.AutoSize = True
        Me.BitwiseReduction.Location = New System.Drawing.Point(15, 60)
        Me.BitwiseReduction.Name = "BitwiseReduction"
        Me.BitwiseReduction.Size = New System.Drawing.Size(194, 24)
        Me.BitwiseReduction.TabIndex = 3
        Me.BitwiseReduction.TabStop = True
        Me.BitwiseReduction.Text = "Use Bitwise Reduction"
        Me.BitwiseReduction.UseVisualStyleBackColor = True
        '
        'UseSimpleReduction
        '
        Me.UseSimpleReduction.AutoSize = True
        Me.UseSimpleReduction.Location = New System.Drawing.Point(15, 29)
        Me.UseSimpleReduction.Name = "UseSimpleReduction"
        Me.UseSimpleReduction.Size = New System.Drawing.Size(192, 24)
        Me.UseSimpleReduction.TabIndex = 0
        Me.UseSimpleReduction.TabStop = True
        Me.UseSimpleReduction.Text = "Use Simple Reduction"
        Me.UseSimpleReduction.UseVisualStyleBackColor = True
        '
        'ReductionSliders
        '
        Me.ReductionSliders.Controls.Add(Me.bitwiseLabel)
        Me.ReductionSliders.Controls.Add(Me.BitwiseReductionBar)
        Me.ReductionSliders.Controls.Add(Me.reduceXbits)
        Me.ReductionSliders.Controls.Add(Me.ColorLabel)
        Me.ReductionSliders.Controls.Add(Me.SimpleReductionBar)
        Me.ReductionSliders.Controls.Add(Me.SimpleReduceLabel)
        Me.ReductionSliders.Location = New System.Drawing.Point(792, 453)
        Me.ReductionSliders.Name = "ReductionSliders"
        Me.ReductionSliders.Size = New System.Drawing.Size(779, 140)
        Me.ReductionSliders.TabIndex = 6
        Me.ReductionSliders.TabStop = False
        Me.ReductionSliders.Text = "Reduction Sliders"
        '
        'bitwiseLabel
        '
        Me.bitwiseLabel.AutoSize = True
        Me.bitwiseLabel.Location = New System.Drawing.Point(668, 91)
        Me.bitwiseLabel.Name = "bitwiseLabel"
        Me.bitwiseLabel.Size = New System.Drawing.Size(98, 20)
        Me.bitwiseLabel.TabIndex = 11
        Me.bitwiseLabel.Text = "BitwiseLabel"
        '
        'BitwiseReductionBar
        '
        Me.BitwiseReductionBar.Location = New System.Drawing.Point(156, 85)
        Me.BitwiseReductionBar.Maximum = 7
        Me.BitwiseReductionBar.Name = "BitwiseReductionBar"
        Me.BitwiseReductionBar.Size = New System.Drawing.Size(506, 69)
        Me.BitwiseReductionBar.TabIndex = 10
        Me.BitwiseReductionBar.TickStyle = System.Windows.Forms.TickStyle.None
        Me.BitwiseReductionBar.Value = 5
        '
        'reduceXbits
        '
        Me.reduceXbits.AutoSize = True
        Me.reduceXbits.Location = New System.Drawing.Point(8, 91)
        Me.reduceXbits.Name = "reduceXbits"
        Me.reduceXbits.Size = New System.Drawing.Size(109, 20)
        Me.reduceXbits.TabIndex = 9
        Me.reduceXbits.Text = "Reduce X bits"
        '
        'ColorLabel
        '
        Me.ColorLabel.AutoSize = True
        Me.ColorLabel.Location = New System.Drawing.Point(668, 25)
        Me.ColorLabel.Name = "ColorLabel"
        Me.ColorLabel.Size = New System.Drawing.Size(85, 20)
        Me.ColorLabel.TabIndex = 8
        Me.ColorLabel.Text = "ColorLabel"
        '
        'SimpleReductionBar
        '
        Me.SimpleReductionBar.Location = New System.Drawing.Point(156, 18)
        Me.SimpleReductionBar.Maximum = 255
        Me.SimpleReductionBar.Minimum = 1
        Me.SimpleReductionBar.Name = "SimpleReductionBar"
        Me.SimpleReductionBar.Size = New System.Drawing.Size(506, 69)
        Me.SimpleReductionBar.TabIndex = 7
        Me.SimpleReductionBar.TickStyle = System.Windows.Forms.TickStyle.None
        Me.SimpleReductionBar.Value = 80
        '
        'SimpleReduceLabel
        '
        Me.SimpleReduceLabel.Location = New System.Drawing.Point(8, 25)
        Me.SimpleReduceLabel.Name = "SimpleReduceLabel"
        Me.SimpleReduceLabel.Size = New System.Drawing.Size(152, 45)
        Me.SimpleReduceLabel.TabIndex = 6
        Me.SimpleReduceLabel.Text = "Simple Reduction"
        '
        'RedCloudOnly
        '
        Me.RedCloudOnly.Controls.Add(Me.XYZReduction)
        Me.RedCloudOnly.Controls.Add(Me.YZReduction)
        Me.RedCloudOnly.Controls.Add(Me.XZReduction)
        Me.RedCloudOnly.Controls.Add(Me.XYReduction)
        Me.RedCloudOnly.Controls.Add(Me.ZReduction)
        Me.RedCloudOnly.Controls.Add(Me.YReduction)
        Me.RedCloudOnly.Controls.Add(Me.XReduction)
        Me.RedCloudOnly.Location = New System.Drawing.Point(792, 23)
        Me.RedCloudOnly.Name = "RedCloudOnly"
        Me.RedCloudOnly.Size = New System.Drawing.Size(220, 289)
        Me.RedCloudOnly.TabIndex = 7
        Me.RedCloudOnly.TabStop = False
        Me.RedCloudOnly.Text = "RedCloud Reduction"
        '
        'XYZReduction
        '
        Me.XYZReduction.AutoSize = True
        Me.XYZReduction.Location = New System.Drawing.Point(28, 248)
        Me.XYZReduction.Name = "XYZReduction"
        Me.XYZReduction.Size = New System.Drawing.Size(143, 24)
        Me.XYZReduction.TabIndex = 8
        Me.XYZReduction.TabStop = True
        Me.XYZReduction.Tag = "6"
        Me.XYZReduction.Text = "XYZ Reduction"
        Me.XYZReduction.UseVisualStyleBackColor = True
        '
        'YZReduction
        '
        Me.YZReduction.AutoSize = True
        Me.YZReduction.Location = New System.Drawing.Point(28, 212)
        Me.YZReduction.Name = "YZReduction"
        Me.YZReduction.Size = New System.Drawing.Size(132, 24)
        Me.YZReduction.TabIndex = 7
        Me.YZReduction.TabStop = True
        Me.YZReduction.Tag = "5"
        Me.YZReduction.Text = "YZ Reduction"
        Me.YZReduction.UseVisualStyleBackColor = True
        '
        'XZReduction
        '
        Me.XZReduction.AutoSize = True
        Me.XZReduction.Location = New System.Drawing.Point(28, 177)
        Me.XZReduction.Name = "XZReduction"
        Me.XZReduction.Size = New System.Drawing.Size(132, 24)
        Me.XZReduction.TabIndex = 6
        Me.XZReduction.TabStop = True
        Me.XZReduction.Tag = "4"
        Me.XZReduction.Text = "XZ Reduction"
        Me.XZReduction.UseVisualStyleBackColor = True
        '
        'XYReduction
        '
        Me.XYReduction.AutoSize = True
        Me.XYReduction.Location = New System.Drawing.Point(28, 142)
        Me.XYReduction.Name = "XYReduction"
        Me.XYReduction.Size = New System.Drawing.Size(133, 24)
        Me.XYReduction.TabIndex = 5
        Me.XYReduction.TabStop = True
        Me.XYReduction.Tag = "3"
        Me.XYReduction.Text = "XY Reduction"
        Me.XYReduction.UseVisualStyleBackColor = True
        '
        'ZReduction
        '
        Me.ZReduction.AutoSize = True
        Me.ZReduction.Location = New System.Drawing.Point(28, 108)
        Me.ZReduction.Name = "ZReduction"
        Me.ZReduction.Size = New System.Drawing.Size(121, 24)
        Me.ZReduction.TabIndex = 4
        Me.ZReduction.TabStop = True
        Me.ZReduction.Tag = "2"
        Me.ZReduction.Text = "Z Reduction"
        Me.ZReduction.UseVisualStyleBackColor = True
        '
        'YReduction
        '
        Me.YReduction.AutoSize = True
        Me.YReduction.Location = New System.Drawing.Point(28, 72)
        Me.YReduction.Name = "YReduction"
        Me.YReduction.Size = New System.Drawing.Size(122, 24)
        Me.YReduction.TabIndex = 3
        Me.YReduction.TabStop = True
        Me.YReduction.Tag = "1"
        Me.YReduction.Text = "Y Reduction"
        Me.YReduction.UseVisualStyleBackColor = True
        '
        'XReduction
        '
        Me.XReduction.AutoSize = True
        Me.XReduction.Location = New System.Drawing.Point(28, 37)
        Me.XReduction.Name = "XReduction"
        Me.XReduction.Size = New System.Drawing.Size(122, 24)
        Me.XReduction.TabIndex = 0
        Me.XReduction.TabStop = True
        Me.XReduction.Tag = "0"
        Me.XReduction.Text = "X Reduction"
        Me.XReduction.UseVisualStyleBackColor = True
        '
        'RedCloudType
        '
        Me.RedCloudType.Controls.Add(Me.UseGuidedProjection)
        Me.RedCloudType.Controls.Add(Me.UseDepth)
        Me.RedCloudType.Controls.Add(Me.UseColorOnly)
        Me.RedCloudType.Location = New System.Drawing.Point(1020, 34)
        Me.RedCloudType.Name = "RedCloudType"
        Me.RedCloudType.Size = New System.Drawing.Size(302, 135)
        Me.RedCloudType.TabIndex = 8
        Me.RedCloudType.TabStop = False
        Me.RedCloudType.Text = "RedCloud Run Type"
        '
        'UseGuidedProjection
        '
        Me.UseGuidedProjection.AutoSize = True
        Me.UseGuidedProjection.Location = New System.Drawing.Point(28, 100)
        Me.UseGuidedProjection.Name = "UseGuidedProjection"
        Me.UseGuidedProjection.Size = New System.Drawing.Size(261, 24)
        Me.UseGuidedProjection.TabIndex = 4
        Me.UseGuidedProjection.TabStop = True
        Me.UseGuidedProjection.Text = "Use guided backprojection input"
        Me.UseGuidedProjection.UseVisualStyleBackColor = True
        '
        'UseDepth
        '
        Me.UseDepth.AutoSize = True
        Me.UseDepth.Location = New System.Drawing.Point(28, 68)
        Me.UseDepth.Name = "UseDepth"
        Me.UseDepth.Size = New System.Drawing.Size(241, 24)
        Me.UseDepth.TabIndex = 3
        Me.UseDepth.TabStop = True
        Me.UseDepth.Text = "Use reduced pointcloud input"
        Me.UseDepth.UseVisualStyleBackColor = True
        '
        'UseColorOnly
        '
        Me.UseColorOnly.AutoSize = True
        Me.UseColorOnly.Location = New System.Drawing.Point(28, 37)
        Me.UseColorOnly.Name = "UseColorOnly"
        Me.UseColorOnly.Size = New System.Drawing.Size(136, 24)
        Me.UseColorOnly.TabIndex = 0
        Me.UseColorOnly.TabStop = True
        Me.UseColorOnly.Text = "Use Color only"
        Me.UseColorOnly.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.minCellSizeSlider)
        Me.GroupBox2.Controls.Add(Me.Label1)
        Me.GroupBox2.Controls.Add(Me.labelCellSize)
        Me.GroupBox2.Controls.Add(Me.LabelIdentify)
        Me.GroupBox2.Controls.Add(Me.IdentifyCountBar)
        Me.GroupBox2.Controls.Add(Me.IdentifyCountLabel)
        Me.GroupBox2.Controls.Add(Me.ProjectionThresholdBar)
        Me.GroupBox2.Controls.Add(Me.Label8)
        Me.GroupBox2.Controls.Add(Me.SideLabel)
        Me.GroupBox2.Location = New System.Drawing.Point(12, 4)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(763, 255)
        Me.GroupBox2.TabIndex = 9
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "RedCloud Options"
        '
        'minCellSizeSlider
        '
        Me.minCellSizeSlider.Location = New System.Drawing.Point(180, 171)
        Me.minCellSizeSlider.Maximum = 10000
        Me.minCellSizeSlider.Name = "minCellSizeSlider"
        Me.minCellSizeSlider.Size = New System.Drawing.Size(482, 69)
        Me.minCellSizeSlider.TabIndex = 34
        Me.minCellSizeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.minCellSizeSlider.Value = 500
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(21, 171)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(159, 20)
        Me.Label1.TabIndex = 33
        Me.Label1.Text = "Min Cell Size in Pixels"
        '
        'labelCellSize
        '
        Me.labelCellSize.AutoSize = True
        Me.labelCellSize.Location = New System.Drawing.Point(668, 177)
        Me.labelCellSize.Name = "labelCellSize"
        Me.labelCellSize.Size = New System.Drawing.Size(57, 20)
        Me.labelCellSize.TabIndex = 35
        Me.labelCellSize.Text = "Label7"
        '
        'LabelIdentify
        '
        Me.LabelIdentify.AutoSize = True
        Me.LabelIdentify.Location = New System.Drawing.Point(668, 30)
        Me.LabelIdentify.Name = "LabelIdentify"
        Me.LabelIdentify.Size = New System.Drawing.Size(57, 20)
        Me.LabelIdentify.TabIndex = 32
        Me.LabelIdentify.Text = "Label1"
        '
        'IdentifyCountBar
        '
        Me.IdentifyCountBar.Location = New System.Drawing.Point(180, 30)
        Me.IdentifyCountBar.Maximum = 255
        Me.IdentifyCountBar.Minimum = 1
        Me.IdentifyCountBar.Name = "IdentifyCountBar"
        Me.IdentifyCountBar.Size = New System.Drawing.Size(482, 69)
        Me.IdentifyCountBar.TabIndex = 31
        Me.IdentifyCountBar.TickStyle = System.Windows.Forms.TickStyle.None
        Me.IdentifyCountBar.Value = 10
        '
        'IdentifyCountLabel
        '
        Me.IdentifyCountLabel.AutoSize = True
        Me.IdentifyCountLabel.Location = New System.Drawing.Point(70, 39)
        Me.IdentifyCountLabel.Name = "IdentifyCountLabel"
        Me.IdentifyCountLabel.Size = New System.Drawing.Size(104, 20)
        Me.IdentifyCountLabel.TabIndex = 30
        Me.IdentifyCountLabel.Text = "IdentifyCount"
        '
        'ColorSourceLabel
        '
        Me.ColorSourceLabel.AutoSize = True
        Me.ColorSourceLabel.Location = New System.Drawing.Point(1042, 208)
        Me.ColorSourceLabel.Name = "ColorSourceLabel"
        Me.ColorSourceLabel.Size = New System.Drawing.Size(105, 20)
        Me.ColorSourceLabel.TabIndex = 10
        Me.ColorSourceLabel.Text = "Color Source:"
        '
        'ColorSource
        '
        Me.ColorSource.FormattingEnabled = True
        Me.ColorSource.Location = New System.Drawing.Point(1170, 208)
        Me.ColorSource.Name = "ColorSource"
        Me.ColorSource.Size = New System.Drawing.Size(222, 28)
        Me.ColorSource.TabIndex = 11
        '
        'DisplayCellStats
        '
        Me.DisplayCellStats.AutoSize = True
        Me.DisplayCellStats.Location = New System.Drawing.Point(1048, 247)
        Me.DisplayCellStats.Name = "DisplayCellStats"
        Me.DisplayCellStats.Size = New System.Drawing.Size(158, 24)
        Me.DisplayCellStats.TabIndex = 74
        Me.DisplayCellStats.Text = "Display Cell Stats"
        Me.DisplayCellStats.UseVisualStyleBackColor = True
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Checked = True
        Me.CheckBox1.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBox1.Location = New System.Drawing.Point(1049, 307)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(172, 24)
        Me.CheckBox1.TabIndex = 77
        Me.CheckBox1.Text = "Exact matches only"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'IdentifyCells
        '
        Me.IdentifyCells.AutoSize = True
        Me.IdentifyCells.Location = New System.Drawing.Point(1049, 277)
        Me.IdentifyCells.Name = "IdentifyCells"
        Me.IdentifyCells.Size = New System.Drawing.Size(200, 24)
        Me.IdentifyCells.TabIndex = 75
        Me.IdentifyCells.Text = "Identify RedCloud Cells"
        Me.IdentifyCells.UseVisualStyleBackColor = True
        '
        'ColoringGroup
        '
        Me.ColoringGroup.Controls.Add(Me.ColorDepth)
        Me.ColoringGroup.Controls.Add(Me.ColorTrackingDepth)
        Me.ColoringGroup.Controls.Add(Me.ColorTracking)
        Me.ColoringGroup.Controls.Add(Me.ColorMean)
        Me.ColoringGroup.Location = New System.Drawing.Point(1338, 40)
        Me.ColoringGroup.Name = "ColoringGroup"
        Me.ColoringGroup.Size = New System.Drawing.Size(220, 155)
        Me.ColoringGroup.TabIndex = 78
        Me.ColoringGroup.TabStop = False
        Me.ColoringGroup.Text = "Coloring Options"
        '
        'ColorTrackingDepth
        '
        Me.ColorTrackingDepth.AutoSize = True
        Me.ColorTrackingDepth.Location = New System.Drawing.Point(15, 89)
        Me.ColorTrackingDepth.Name = "ColorTrackingDepth"
        Me.ColorTrackingDepth.Size = New System.Drawing.Size(183, 24)
        Me.ColorTrackingDepth.TabIndex = 4
        Me.ColorTrackingDepth.TabStop = True
        Me.ColorTrackingDepth.Text = "Depth Tracking Color"
        Me.ColorTrackingDepth.UseVisualStyleBackColor = True
        '
        'ColorTracking
        '
        Me.ColorTracking.AutoSize = True
        Me.ColorTracking.Location = New System.Drawing.Point(15, 59)
        Me.ColorTracking.Name = "ColorTracking"
        Me.ColorTracking.Size = New System.Drawing.Size(135, 24)
        Me.ColorTracking.TabIndex = 3
        Me.ColorTracking.TabStop = True
        Me.ColorTracking.Text = "Tracking Color"
        Me.ColorTracking.UseVisualStyleBackColor = True
        '
        'ColorMean
        '
        Me.ColorMean.AutoSize = True
        Me.ColorMean.Location = New System.Drawing.Point(15, 29)
        Me.ColorMean.Name = "ColorMean"
        Me.ColorMean.Size = New System.Drawing.Size(112, 24)
        Me.ColorMean.TabIndex = 0
        Me.ColorMean.TabStop = True
        Me.ColorMean.Text = "Mean color"
        Me.ColorMean.UseVisualStyleBackColor = True
        '
        'ColorDepth
        '
        Me.ColorDepth.AutoSize = True
        Me.ColorDepth.Location = New System.Drawing.Point(16, 122)
        Me.ColorDepth.Name = "ColorDepth"
        Me.ColorDepth.Size = New System.Drawing.Size(119, 24)
        Me.ColorDepth.TabIndex = 5
        Me.ColorDepth.TabStop = True
        Me.ColorDepth.Text = "Depth Color"
        Me.ColorDepth.UseVisualStyleBackColor = True
        '
        'OptionsRedCloud
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1585, 604)
        Me.Controls.Add(Me.ColoringGroup)
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.IdentifyCells)
        Me.Controls.Add(Me.DisplayCellStats)
        Me.Controls.Add(Me.ColorSource)
        Me.Controls.Add(Me.ColorSourceLabel)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.RedCloudType)
        Me.Controls.Add(Me.RedCloudOnly)
        Me.Controls.Add(Me.ReductionSliders)
        Me.Controls.Add(Me.ReductionTypeGroup)
        Me.Controls.Add(Me.RedCloudSliders)
        Me.Name = "OptionsRedCloud"
        Me.Text = "OptionsRedCloud"
        Me.RedCloudSliders.ResumeLayout(False)
        Me.RedCloudSliders.PerformLayout()
        CType(Me.HistBinBar3D, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.YRangeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.XRangeBar, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.ProjectionThresholdBar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ReductionTypeGroup.ResumeLayout(False)
        Me.ReductionTypeGroup.PerformLayout()
        Me.ReductionSliders.ResumeLayout(False)
        Me.ReductionSliders.PerformLayout()
        CType(Me.BitwiseReductionBar, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.SimpleReductionBar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.RedCloudOnly.ResumeLayout(False)
        Me.RedCloudOnly.PerformLayout()
        Me.RedCloudType.ResumeLayout(False)
        Me.RedCloudType.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        CType(Me.minCellSizeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.IdentifyCountBar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ColoringGroup.ResumeLayout(False)
        Me.ColoringGroup.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents RedCloudSliders As System.Windows.Forms.GroupBox
    Friend WithEvents YLabel As System.Windows.Forms.Label
    Friend WithEvents YRangeSlider As System.Windows.Forms.TrackBar
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents XLabel As System.Windows.Forms.Label
    Friend WithEvents XRangeBar As System.Windows.Forms.TrackBar
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents SideLabel As System.Windows.Forms.Label
    Friend WithEvents ProjectionThresholdBar As System.Windows.Forms.TrackBar
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents ReductionTypeGroup As System.Windows.Forms.GroupBox
    Friend WithEvents BitwiseReduction As System.Windows.Forms.RadioButton
    Friend WithEvents UseSimpleReduction As System.Windows.Forms.RadioButton
    Friend WithEvents ReductionSliders As System.Windows.Forms.GroupBox
    Friend WithEvents bitwiseLabel As System.Windows.Forms.Label
    Friend WithEvents BitwiseReductionBar As System.Windows.Forms.TrackBar
    Friend WithEvents reduceXbits As System.Windows.Forms.Label
    Friend WithEvents ColorLabel As System.Windows.Forms.Label
    Friend WithEvents SimpleReductionBar As System.Windows.Forms.TrackBar
    Friend WithEvents SimpleReduceLabel As System.Windows.Forms.Label
    Friend WithEvents RedCloudOnly As System.Windows.Forms.GroupBox
    Friend WithEvents XZReduction As System.Windows.Forms.RadioButton
    Friend WithEvents XYReduction As System.Windows.Forms.RadioButton
    Friend WithEvents ZReduction As System.Windows.Forms.RadioButton
    Friend WithEvents YReduction As System.Windows.Forms.RadioButton
    Friend WithEvents XReduction As System.Windows.Forms.RadioButton
    Friend WithEvents XYZReduction As System.Windows.Forms.RadioButton
    Friend WithEvents YZReduction As System.Windows.Forms.RadioButton
    Friend WithEvents NoReduction As System.Windows.Forms.RadioButton
    Friend WithEvents RedCloudType As System.Windows.Forms.GroupBox
    Friend WithEvents UseDepth As System.Windows.Forms.RadioButton
    Friend WithEvents UseColorOnly As System.Windows.Forms.RadioButton
    Friend WithEvents UseGuidedProjection As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents LabelHistogramBins As System.Windows.Forms.Label
    Friend WithEvents HistBinBar3D As System.Windows.Forms.TrackBar
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents ColorSourceLabel As System.Windows.Forms.Label
    Friend WithEvents ColorSource As System.Windows.Forms.ComboBox
    Friend WithEvents LabelIdentify As System.Windows.Forms.Label
    Friend WithEvents IdentifyCountBar As System.Windows.Forms.TrackBar
    Friend WithEvents IdentifyCountLabel As System.Windows.Forms.Label
    Friend WithEvents DisplayCellStats As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBox1 As System.Windows.Forms.CheckBox
    Friend WithEvents IdentifyCells As Windows.Forms.CheckBox
    Friend WithEvents minCellSizeSlider As Windows.Forms.TrackBar
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents labelCellSize As Windows.Forms.Label
    Friend WithEvents ColoringGroup As Windows.Forms.GroupBox
    Friend WithEvents ColorTrackingDepth As Windows.Forms.RadioButton
    Friend WithEvents ColorTracking As Windows.Forms.RadioButton
    Friend WithEvents ColorMean As Windows.Forms.RadioButton
    Friend WithEvents ColorDepth As Windows.Forms.RadioButton
End Class
