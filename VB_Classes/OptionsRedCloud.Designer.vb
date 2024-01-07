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
        Me.TopLabel = New System.Windows.Forms.Label()
        Me.TopViewThreshold = New System.Windows.Forms.TrackBar()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.SideLabel = New System.Windows.Forms.Label()
        Me.SideViewThreshold = New System.Windows.Forms.TrackBar()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.YLabel = New System.Windows.Forms.Label()
        Me.YRangeSlider = New System.Windows.Forms.TrackBar()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.XLabel = New System.Windows.Forms.Label()
        Me.XRangeSlider = New System.Windows.Forms.TrackBar()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.RGBSource = New System.Windows.Forms.GroupBox()
        Me.FeatureLessRadio = New System.Windows.Forms.RadioButton()
        Me.BackProject3D = New System.Windows.Forms.RadioButton()
        Me.KMeans_Basics = New System.Windows.Forms.RadioButton()
        Me.LUT_Basics = New System.Windows.Forms.RadioButton()
        Me.Reduction_Basics = New System.Windows.Forms.RadioButton()
        Me.BackProject_Full = New System.Windows.Forms.RadioButton()
        Me.ReductionTypeGroup = New System.Windows.Forms.GroupBox()
        Me.NoReduction = New System.Windows.Forms.RadioButton()
        Me.BitwiseReduction = New System.Windows.Forms.RadioButton()
        Me.SimpleReduction = New System.Windows.Forms.RadioButton()
        Me.ReductionSliders = New System.Windows.Forms.GroupBox()
        Me.bitwiseLabel = New System.Windows.Forms.Label()
        Me.BitwiseReductionSlider = New System.Windows.Forms.TrackBar()
        Me.reduceXbits = New System.Windows.Forms.Label()
        Me.ColorLabel = New System.Windows.Forms.Label()
        Me.SimpleReductionSlider = New System.Windows.Forms.TrackBar()
        Me.ColorReduce = New System.Windows.Forms.Label()
        Me.RedCloudOnly = New System.Windows.Forms.GroupBox()
        Me.XYZReduction = New System.Windows.Forms.RadioButton()
        Me.YZReduction = New System.Windows.Forms.RadioButton()
        Me.XZReduction = New System.Windows.Forms.RadioButton()
        Me.XYReduction = New System.Windows.Forms.RadioButton()
        Me.ZReduction = New System.Windows.Forms.RadioButton()
        Me.YReduction = New System.Windows.Forms.RadioButton()
        Me.XReduction = New System.Windows.Forms.RadioButton()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.RedCloud_Core = New System.Windows.Forms.RadioButton()
        Me.GuidedBP_Depth = New System.Windows.Forms.RadioButton()
        Me.RedCloudType = New System.Windows.Forms.GroupBox()
        Me.UseDepthAndColor = New System.Windows.Forms.RadioButton()
        Me.UseDepth = New System.Windows.Forms.RadioButton()
        Me.UseColor = New System.Windows.Forms.RadioButton()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.LabelHistogramBins = New System.Windows.Forms.Label()
        Me.HistBinSlider = New System.Windows.Forms.TrackBar()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.LabelDesiredCell = New System.Windows.Forms.Label()
        Me.DesiredCellSlider = New System.Windows.Forms.TrackBar()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.LabelimageSizePercent = New System.Windows.Forms.Label()
        Me.imageSizeThresholdSlider = New System.Windows.Forms.TrackBar()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.RedCloudSliders.SuspendLayout()
        CType(Me.TopViewThreshold, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.SideViewThreshold, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.YRangeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.XRangeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.RGBSource.SuspendLayout()
        Me.ReductionTypeGroup.SuspendLayout()
        Me.ReductionSliders.SuspendLayout()
        CType(Me.BitwiseReductionSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.SimpleReductionSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.RedCloudOnly.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.RedCloudType.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DesiredCellSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.imageSizeThresholdSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'RedCloudSliders
        '
        Me.RedCloudSliders.Controls.Add(Me.TopLabel)
        Me.RedCloudSliders.Controls.Add(Me.TopViewThreshold)
        Me.RedCloudSliders.Controls.Add(Me.Label6)
        Me.RedCloudSliders.Controls.Add(Me.SideLabel)
        Me.RedCloudSliders.Controls.Add(Me.SideViewThreshold)
        Me.RedCloudSliders.Controls.Add(Me.Label8)
        Me.RedCloudSliders.Controls.Add(Me.YLabel)
        Me.RedCloudSliders.Controls.Add(Me.YRangeSlider)
        Me.RedCloudSliders.Controls.Add(Me.Label4)
        Me.RedCloudSliders.Controls.Add(Me.XLabel)
        Me.RedCloudSliders.Controls.Add(Me.XRangeSlider)
        Me.RedCloudSliders.Controls.Add(Me.Label2)
        Me.RedCloudSliders.Location = New System.Drawing.Point(12, 257)
        Me.RedCloudSliders.Name = "RedCloudSliders"
        Me.RedCloudSliders.Size = New System.Drawing.Size(763, 300)
        Me.RedCloudSliders.TabIndex = 2
        Me.RedCloudSliders.TabStop = False
        '
        'TopLabel
        '
        Me.TopLabel.AutoSize = True
        Me.TopLabel.Location = New System.Drawing.Point(668, 245)
        Me.TopLabel.Name = "TopLabel"
        Me.TopLabel.Size = New System.Drawing.Size(57, 20)
        Me.TopLabel.TabIndex = 20
        Me.TopLabel.Text = "Label5"
        '
        'TopViewThreshold
        '
        Me.TopViewThreshold.Location = New System.Drawing.Point(156, 238)
        Me.TopViewThreshold.Maximum = 200
        Me.TopViewThreshold.Minimum = 3
        Me.TopViewThreshold.Name = "TopViewThreshold"
        Me.TopViewThreshold.Size = New System.Drawing.Size(506, 69)
        Me.TopViewThreshold.TabIndex = 19
        Me.TopViewThreshold.TickStyle = System.Windows.Forms.TickStyle.None
        Me.TopViewThreshold.Value = 10
        '
        'Label6
        '
        Me.Label6.Location = New System.Drawing.Point(8, 245)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(152, 48)
        Me.Label6.TabIndex = 18
        Me.Label6.Text = "Top View Red Threshold"
        '
        'SideLabel
        '
        Me.SideLabel.AutoSize = True
        Me.SideLabel.Location = New System.Drawing.Point(668, 169)
        Me.SideLabel.Name = "SideLabel"
        Me.SideLabel.Size = New System.Drawing.Size(57, 20)
        Me.SideLabel.TabIndex = 17
        Me.SideLabel.Text = "Label7"
        '
        'SideViewThreshold
        '
        Me.SideViewThreshold.Location = New System.Drawing.Point(156, 163)
        Me.SideViewThreshold.Maximum = 200
        Me.SideViewThreshold.Minimum = 3
        Me.SideViewThreshold.Name = "SideViewThreshold"
        Me.SideViewThreshold.Size = New System.Drawing.Size(506, 69)
        Me.SideViewThreshold.TabIndex = 16
        Me.SideViewThreshold.TickStyle = System.Windows.Forms.TickStyle.None
        Me.SideViewThreshold.Value = 10
        '
        'Label8
        '
        Me.Label8.Location = New System.Drawing.Point(8, 169)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(152, 43)
        Me.Label8.TabIndex = 15
        Me.Label8.Text = "Side View Red Threshold"
        '
        'YLabel
        '
        Me.YLabel.AutoSize = True
        Me.YLabel.Location = New System.Drawing.Point(668, 105)
        Me.YLabel.Name = "YLabel"
        Me.YLabel.Size = New System.Drawing.Size(57, 20)
        Me.YLabel.TabIndex = 14
        Me.YLabel.Text = "Label3"
        '
        'YRangeSlider
        '
        Me.YRangeSlider.Location = New System.Drawing.Point(156, 98)
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
        Me.Label4.Location = New System.Drawing.Point(8, 105)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(115, 20)
        Me.Label4.TabIndex = 12
        Me.Label4.Text = "Y-Range X100"
        '
        'XLabel
        '
        Me.XLabel.AutoSize = True
        Me.XLabel.Location = New System.Drawing.Point(668, 31)
        Me.XLabel.Name = "XLabel"
        Me.XLabel.Size = New System.Drawing.Size(57, 20)
        Me.XLabel.TabIndex = 11
        Me.XLabel.Text = "Label1"
        '
        'XRangeSlider
        '
        Me.XRangeSlider.Location = New System.Drawing.Point(156, 25)
        Me.XRangeSlider.Maximum = 1000
        Me.XRangeSlider.Minimum = 3
        Me.XRangeSlider.Name = "XRangeSlider"
        Me.XRangeSlider.Size = New System.Drawing.Size(506, 69)
        Me.XRangeSlider.TabIndex = 10
        Me.XRangeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.XRangeSlider.Value = 5
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(8, 31)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(115, 20)
        Me.Label2.TabIndex = 9
        Me.Label2.Text = "X-Range X100"
        '
        'RGBSource
        '
        Me.RGBSource.Controls.Add(Me.FeatureLessRadio)
        Me.RGBSource.Controls.Add(Me.BackProject3D)
        Me.RGBSource.Controls.Add(Me.KMeans_Basics)
        Me.RGBSource.Controls.Add(Me.LUT_Basics)
        Me.RGBSource.Controls.Add(Me.Reduction_Basics)
        Me.RGBSource.Controls.Add(Me.BackProject_Full)
        Me.RGBSource.Location = New System.Drawing.Point(1019, 31)
        Me.RGBSource.Name = "RGBSource"
        Me.RGBSource.Size = New System.Drawing.Size(250, 220)
        Me.RGBSource.TabIndex = 3
        Me.RGBSource.TabStop = False
        Me.RGBSource.Text = "Color Source"
        '
        'FeatureLessRadio
        '
        Me.FeatureLessRadio.AutoSize = True
        Me.FeatureLessRadio.Location = New System.Drawing.Point(28, 100)
        Me.FeatureLessRadio.Name = "FeatureLessRadio"
        Me.FeatureLessRadio.Size = New System.Drawing.Size(124, 24)
        Me.FeatureLessRadio.TabIndex = 6
        Me.FeatureLessRadio.TabStop = True
        Me.FeatureLessRadio.Text = "FeatureLess"
        Me.FeatureLessRadio.UseVisualStyleBackColor = True
        '
        'BackProject3D
        '
        Me.BackProject3D.AutoSize = True
        Me.BackProject3D.Location = New System.Drawing.Point(28, 29)
        Me.BackProject3D.Name = "BackProject3D"
        Me.BackProject3D.Size = New System.Drawing.Size(165, 24)
        Me.BackProject3D.TabIndex = 5
        Me.BackProject3D.TabStop = True
        Me.BackProject3D.Text = "3D BackProjection"
        Me.BackProject3D.UseVisualStyleBackColor = True
        '
        'KMeans_Basics
        '
        Me.KMeans_Basics.AutoSize = True
        Me.KMeans_Basics.Location = New System.Drawing.Point(28, 129)
        Me.KMeans_Basics.Name = "KMeans_Basics"
        Me.KMeans_Basics.Size = New System.Drawing.Size(148, 24)
        Me.KMeans_Basics.TabIndex = 3
        Me.KMeans_Basics.TabStop = True
        Me.KMeans_Basics.Text = "KMeans_Basics"
        Me.KMeans_Basics.UseVisualStyleBackColor = True
        '
        'LUT_Basics
        '
        Me.LUT_Basics.AutoSize = True
        Me.LUT_Basics.Location = New System.Drawing.Point(28, 159)
        Me.LUT_Basics.Name = "LUT_Basics"
        Me.LUT_Basics.Size = New System.Drawing.Size(120, 24)
        Me.LUT_Basics.TabIndex = 2
        Me.LUT_Basics.TabStop = True
        Me.LUT_Basics.Text = "LUT_Basics"
        Me.LUT_Basics.UseVisualStyleBackColor = True
        '
        'Reduction_Basics
        '
        Me.Reduction_Basics.AutoSize = True
        Me.Reduction_Basics.Location = New System.Drawing.Point(28, 189)
        Me.Reduction_Basics.Name = "Reduction_Basics"
        Me.Reduction_Basics.Size = New System.Drawing.Size(163, 24)
        Me.Reduction_Basics.TabIndex = 1
        Me.Reduction_Basics.TabStop = True
        Me.Reduction_Basics.Text = "Reduction_Basics"
        Me.Reduction_Basics.UseVisualStyleBackColor = True
        '
        'BackProject_Full
        '
        Me.BackProject_Full.AutoSize = True
        Me.BackProject_Full.Location = New System.Drawing.Point(28, 64)
        Me.BackProject_Full.Name = "BackProject_Full"
        Me.BackProject_Full.Size = New System.Drawing.Size(153, 24)
        Me.BackProject_Full.TabIndex = 0
        Me.BackProject_Full.TabStop = True
        Me.BackProject_Full.Text = "BackProject_Full"
        Me.BackProject_Full.UseVisualStyleBackColor = True
        '
        'ReductionTypeGroup
        '
        Me.ReductionTypeGroup.Controls.Add(Me.NoReduction)
        Me.ReductionTypeGroup.Controls.Add(Me.BitwiseReduction)
        Me.ReductionTypeGroup.Controls.Add(Me.SimpleReduction)
        Me.ReductionTypeGroup.Location = New System.Drawing.Point(1019, 257)
        Me.ReductionTypeGroup.Name = "ReductionTypeGroup"
        Me.ReductionTypeGroup.Size = New System.Drawing.Size(250, 129)
        Me.ReductionTypeGroup.TabIndex = 5
        Me.ReductionTypeGroup.TabStop = False
        Me.ReductionTypeGroup.Text = "Reduction Options"
        '
        'NoReduction
        '
        Me.NoReduction.AutoSize = True
        Me.NoReduction.Location = New System.Drawing.Point(28, 97)
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
        Me.BitwiseReduction.Location = New System.Drawing.Point(28, 68)
        Me.BitwiseReduction.Name = "BitwiseReduction"
        Me.BitwiseReduction.Size = New System.Drawing.Size(194, 24)
        Me.BitwiseReduction.TabIndex = 3
        Me.BitwiseReduction.TabStop = True
        Me.BitwiseReduction.Text = "Use Bitwise Reduction"
        Me.BitwiseReduction.UseVisualStyleBackColor = True
        '
        'SimpleReduction
        '
        Me.SimpleReduction.AutoSize = True
        Me.SimpleReduction.Location = New System.Drawing.Point(28, 37)
        Me.SimpleReduction.Name = "SimpleReduction"
        Me.SimpleReduction.Size = New System.Drawing.Size(192, 24)
        Me.SimpleReduction.TabIndex = 0
        Me.SimpleReduction.TabStop = True
        Me.SimpleReduction.Text = "Use Simple Reduction"
        Me.SimpleReduction.UseVisualStyleBackColor = True
        '
        'ReductionSliders
        '
        Me.ReductionSliders.Controls.Add(Me.bitwiseLabel)
        Me.ReductionSliders.Controls.Add(Me.BitwiseReductionSlider)
        Me.ReductionSliders.Controls.Add(Me.reduceXbits)
        Me.ReductionSliders.Controls.Add(Me.ColorLabel)
        Me.ReductionSliders.Controls.Add(Me.SimpleReductionSlider)
        Me.ReductionSliders.Controls.Add(Me.ColorReduce)
        Me.ReductionSliders.Location = New System.Drawing.Point(792, 490)
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
        'BitwiseReductionSlider
        '
        Me.BitwiseReductionSlider.Location = New System.Drawing.Point(156, 85)
        Me.BitwiseReductionSlider.Maximum = 7
        Me.BitwiseReductionSlider.Name = "BitwiseReductionSlider"
        Me.BitwiseReductionSlider.Size = New System.Drawing.Size(506, 69)
        Me.BitwiseReductionSlider.TabIndex = 10
        Me.BitwiseReductionSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.BitwiseReductionSlider.Value = 5
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
        'SimpleReductionSlider
        '
        Me.SimpleReductionSlider.Location = New System.Drawing.Point(156, 18)
        Me.SimpleReductionSlider.Maximum = 255
        Me.SimpleReductionSlider.Minimum = 1
        Me.SimpleReductionSlider.Name = "SimpleReductionSlider"
        Me.SimpleReductionSlider.Size = New System.Drawing.Size(506, 69)
        Me.SimpleReductionSlider.TabIndex = 7
        Me.SimpleReductionSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.SimpleReductionSlider.Value = 80
        '
        'ColorReduce
        '
        Me.ColorReduce.Location = New System.Drawing.Point(8, 25)
        Me.ColorReduce.Name = "ColorReduce"
        Me.ColorReduce.Size = New System.Drawing.Size(152, 45)
        Me.ColorReduce.TabIndex = 6
        Me.ColorReduce.Text = "Simple Reduction"
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
        Me.RedCloudOnly.Text = "Histogram Channels"
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
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.RedCloud_Core)
        Me.GroupBox1.Controls.Add(Me.GuidedBP_Depth)
        Me.GroupBox1.Location = New System.Drawing.Point(792, 340)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(220, 112)
        Me.GroupBox1.TabIndex = 5
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "RedCloud Data Source"
        '
        'RedCloud_Core
        '
        Me.RedCloud_Core.AutoSize = True
        Me.RedCloud_Core.Location = New System.Drawing.Point(28, 68)
        Me.RedCloud_Core.Name = "RedCloud_Core"
        Me.RedCloud_Core.Size = New System.Drawing.Size(148, 24)
        Me.RedCloud_Core.TabIndex = 3
        Me.RedCloud_Core.TabStop = True
        Me.RedCloud_Core.Text = "RedCloud_Core"
        Me.RedCloud_Core.UseVisualStyleBackColor = True
        '
        'GuidedBP_Depth
        '
        Me.GuidedBP_Depth.AutoSize = True
        Me.GuidedBP_Depth.Location = New System.Drawing.Point(28, 37)
        Me.GuidedBP_Depth.Name = "GuidedBP_Depth"
        Me.GuidedBP_Depth.Size = New System.Drawing.Size(160, 24)
        Me.GuidedBP_Depth.TabIndex = 0
        Me.GuidedBP_Depth.TabStop = True
        Me.GuidedBP_Depth.Text = "GuidedBP_Depth"
        Me.GuidedBP_Depth.UseVisualStyleBackColor = True
        '
        'RedCloudType
        '
        Me.RedCloudType.Controls.Add(Me.UseDepthAndColor)
        Me.RedCloudType.Controls.Add(Me.UseDepth)
        Me.RedCloudType.Controls.Add(Me.UseColor)
        Me.RedCloudType.Location = New System.Drawing.Point(1297, 38)
        Me.RedCloudType.Name = "RedCloudType"
        Me.RedCloudType.Size = New System.Drawing.Size(220, 135)
        Me.RedCloudType.TabIndex = 8
        Me.RedCloudType.TabStop = False
        Me.RedCloudType.Text = "RedCloud Run Type"
        '
        'UseDepthAndColor
        '
        Me.UseDepthAndColor.AutoSize = True
        Me.UseDepthAndColor.Location = New System.Drawing.Point(28, 100)
        Me.UseDepthAndColor.Name = "UseDepthAndColor"
        Me.UseDepthAndColor.Size = New System.Drawing.Size(183, 24)
        Me.UseDepthAndColor.TabIndex = 4
        Me.UseDepthAndColor.TabStop = True
        Me.UseDepthAndColor.Text = "Use Depth and Color"
        Me.UseDepthAndColor.UseVisualStyleBackColor = True
        '
        'UseDepth
        '
        Me.UseDepth.AutoSize = True
        Me.UseDepth.Location = New System.Drawing.Point(28, 68)
        Me.UseDepth.Name = "UseDepth"
        Me.UseDepth.Size = New System.Drawing.Size(152, 24)
        Me.UseDepth.TabIndex = 3
        Me.UseDepth.TabStop = True
        Me.UseDepth.Text = "Use Depth Input"
        Me.UseDepth.UseVisualStyleBackColor = True
        '
        'UseColor
        '
        Me.UseColor.AutoSize = True
        Me.UseColor.Location = New System.Drawing.Point(28, 37)
        Me.UseColor.Name = "UseColor"
        Me.UseColor.Size = New System.Drawing.Size(145, 24)
        Me.UseColor.TabIndex = 0
        Me.UseColor.TabStop = True
        Me.UseColor.Text = "Use Color Input"
        Me.UseColor.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.LabelHistogramBins)
        Me.GroupBox2.Controls.Add(Me.HistBinSlider)
        Me.GroupBox2.Controls.Add(Me.Label7)
        Me.GroupBox2.Controls.Add(Me.LabelDesiredCell)
        Me.GroupBox2.Controls.Add(Me.DesiredCellSlider)
        Me.GroupBox2.Controls.Add(Me.Label5)
        Me.GroupBox2.Controls.Add(Me.LabelimageSizePercent)
        Me.GroupBox2.Controls.Add(Me.imageSizeThresholdSlider)
        Me.GroupBox2.Controls.Add(Me.Label3)
        Me.GroupBox2.Location = New System.Drawing.Point(12, 4)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(763, 255)
        Me.GroupBox2.TabIndex = 9
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Histogram 3D Options"
        '
        'LabelHistogramBins
        '
        Me.LabelHistogramBins.AutoSize = True
        Me.LabelHistogramBins.Location = New System.Drawing.Point(668, 163)
        Me.LabelHistogramBins.Name = "LabelHistogramBins"
        Me.LabelHistogramBins.Size = New System.Drawing.Size(57, 20)
        Me.LabelHistogramBins.TabIndex = 32
        Me.LabelHistogramBins.Text = "Label5"
        '
        'HistBinSlider
        '
        Me.HistBinSlider.Location = New System.Drawing.Point(156, 156)
        Me.HistBinSlider.Maximum = 16
        Me.HistBinSlider.Minimum = 2
        Me.HistBinSlider.Name = "HistBinSlider"
        Me.HistBinSlider.Size = New System.Drawing.Size(506, 69)
        Me.HistBinSlider.TabIndex = 31
        Me.HistBinSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.HistBinSlider.Value = 4
        '
        'Label7
        '
        Me.Label7.Location = New System.Drawing.Point(8, 163)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(152, 48)
        Me.Label7.TabIndex = 30
        Me.Label7.Text = "3D Histogram Bins"
        '
        'LabelDesiredCell
        '
        Me.LabelDesiredCell.AutoSize = True
        Me.LabelDesiredCell.Location = New System.Drawing.Point(668, 98)
        Me.LabelDesiredCell.Name = "LabelDesiredCell"
        Me.LabelDesiredCell.Size = New System.Drawing.Size(57, 20)
        Me.LabelDesiredCell.TabIndex = 29
        Me.LabelDesiredCell.Text = "Label5"
        '
        'DesiredCellSlider
        '
        Me.DesiredCellSlider.Location = New System.Drawing.Point(156, 91)
        Me.DesiredCellSlider.Maximum = 100
        Me.DesiredCellSlider.Minimum = 1
        Me.DesiredCellSlider.Name = "DesiredCellSlider"
        Me.DesiredCellSlider.Size = New System.Drawing.Size(506, 69)
        Me.DesiredCellSlider.TabIndex = 28
        Me.DesiredCellSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.DesiredCellSlider.Value = 8
        '
        'Label5
        '
        Me.Label5.Location = New System.Drawing.Point(8, 98)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(152, 48)
        Me.Label5.TabIndex = 27
        Me.Label5.Text = "Desired RedMin Cells"
        '
        'LabelimageSizePercent
        '
        Me.LabelimageSizePercent.AutoSize = True
        Me.LabelimageSizePercent.Location = New System.Drawing.Point(668, 34)
        Me.LabelimageSizePercent.Name = "LabelimageSizePercent"
        Me.LabelimageSizePercent.Size = New System.Drawing.Size(57, 20)
        Me.LabelimageSizePercent.TabIndex = 26
        Me.LabelimageSizePercent.Text = "Label5"
        '
        'imageSizeThresholdSlider
        '
        Me.imageSizeThresholdSlider.Location = New System.Drawing.Point(156, 27)
        Me.imageSizeThresholdSlider.Maximum = 100
        Me.imageSizeThresholdSlider.Minimum = 1
        Me.imageSizeThresholdSlider.Name = "imageSizeThresholdSlider"
        Me.imageSizeThresholdSlider.Size = New System.Drawing.Size(506, 69)
        Me.imageSizeThresholdSlider.TabIndex = 25
        Me.imageSizeThresholdSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.imageSizeThresholdSlider.Value = 95
        '
        'Label3
        '
        Me.Label3.Location = New System.Drawing.Point(8, 34)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(152, 48)
        Me.Label3.TabIndex = 24
        Me.Label3.Text = "RedMin Image %"
        '
        'OptionsRedCloud
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1585, 642)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.RedCloudType)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.RedCloudOnly)
        Me.Controls.Add(Me.ReductionSliders)
        Me.Controls.Add(Me.ReductionTypeGroup)
        Me.Controls.Add(Me.RGBSource)
        Me.Controls.Add(Me.RedCloudSliders)
        Me.Name = "OptionsRedCloud"
        Me.Text = "OptionsRedCloud"
        Me.RedCloudSliders.ResumeLayout(False)
        Me.RedCloudSliders.PerformLayout()
        CType(Me.TopViewThreshold, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.SideViewThreshold, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.YRangeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.XRangeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.RGBSource.ResumeLayout(False)
        Me.RGBSource.PerformLayout()
        Me.ReductionTypeGroup.ResumeLayout(False)
        Me.ReductionTypeGroup.PerformLayout()
        Me.ReductionSliders.ResumeLayout(False)
        Me.ReductionSliders.PerformLayout()
        CType(Me.BitwiseReductionSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.SimpleReductionSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.RedCloudOnly.ResumeLayout(False)
        Me.RedCloudOnly.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.RedCloudType.ResumeLayout(False)
        Me.RedCloudType.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DesiredCellSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.imageSizeThresholdSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents RedCloudSliders As Windows.Forms.GroupBox
    Friend WithEvents RGBSource As Windows.Forms.GroupBox
    Friend WithEvents KMeans_Basics As Windows.Forms.RadioButton
    Friend WithEvents LUT_Basics As Windows.Forms.RadioButton
    Friend WithEvents Reduction_Basics As Windows.Forms.RadioButton
    Friend WithEvents BackProject_Full As Windows.Forms.RadioButton
    Friend WithEvents YLabel As Windows.Forms.Label
    Friend WithEvents YRangeSlider As Windows.Forms.TrackBar
    Friend WithEvents Label4 As Windows.Forms.Label
    Friend WithEvents XLabel As Windows.Forms.Label
    Friend WithEvents XRangeSlider As Windows.Forms.TrackBar
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents TopLabel As Windows.Forms.Label
    Friend WithEvents TopViewThreshold As Windows.Forms.TrackBar
    Friend WithEvents Label6 As Windows.Forms.Label
    Friend WithEvents SideLabel As Windows.Forms.Label
    Friend WithEvents SideViewThreshold As Windows.Forms.TrackBar
    Friend WithEvents Label8 As Windows.Forms.Label
    Friend WithEvents ReductionTypeGroup As Windows.Forms.GroupBox
    Friend WithEvents BitwiseReduction As Windows.Forms.RadioButton
    Friend WithEvents SimpleReduction As Windows.Forms.RadioButton
    Friend WithEvents ReductionSliders As Windows.Forms.GroupBox
    Friend WithEvents bitwiseLabel As Windows.Forms.Label
    Friend WithEvents BitwiseReductionSlider As Windows.Forms.TrackBar
    Friend WithEvents reduceXbits As Windows.Forms.Label
    Friend WithEvents ColorLabel As Windows.Forms.Label
    Friend WithEvents SimpleReductionSlider As Windows.Forms.TrackBar
    Friend WithEvents ColorReduce As Windows.Forms.Label
    Friend WithEvents RedCloudOnly As Windows.Forms.GroupBox
    Friend WithEvents XZReduction As Windows.Forms.RadioButton
    Friend WithEvents XYReduction As Windows.Forms.RadioButton
    Friend WithEvents ZReduction As Windows.Forms.RadioButton
    Friend WithEvents YReduction As Windows.Forms.RadioButton
    Friend WithEvents XReduction As Windows.Forms.RadioButton
    Friend WithEvents XYZReduction As Windows.Forms.RadioButton
    Friend WithEvents YZReduction As Windows.Forms.RadioButton
    Friend WithEvents NoReduction As Windows.Forms.RadioButton
    Friend WithEvents GroupBox1 As Windows.Forms.GroupBox
    Friend WithEvents RedCloud_Core As Windows.Forms.RadioButton
    Friend WithEvents GuidedBP_Depth As Windows.Forms.RadioButton
    Friend WithEvents BackProject3D As Windows.Forms.RadioButton
    Friend WithEvents RedCloudType As Windows.Forms.GroupBox
    Friend WithEvents UseDepth As Windows.Forms.RadioButton
    Friend WithEvents UseColor As Windows.Forms.RadioButton
    Friend WithEvents UseDepthAndColor As Windows.Forms.RadioButton
    Friend WithEvents FeatureLessRadio As Windows.Forms.RadioButton
    Friend WithEvents GroupBox2 As Windows.Forms.GroupBox
    Friend WithEvents LabelDesiredCell As Windows.Forms.Label
    Friend WithEvents DesiredCellSlider As Windows.Forms.TrackBar
    Friend WithEvents Label5 As Windows.Forms.Label
    Friend WithEvents LabelimageSizePercent As Windows.Forms.Label
    Friend WithEvents imageSizeThresholdSlider As Windows.Forms.TrackBar
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents LabelHistogramBins As Windows.Forms.Label
    Friend WithEvents HistBinSlider As Windows.Forms.TrackBar
    Friend WithEvents Label7 As Windows.Forms.Label
End Class
