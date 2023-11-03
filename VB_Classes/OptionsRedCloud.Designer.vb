<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsRedCloud
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
        Me.RedCloudSliders = New System.Windows.Forms.GroupBox()
        Me.GridsizeLabel = New System.Windows.Forms.Label()
        Me.GridSizeSlider = New System.Windows.Forms.TrackBar()
        Me.Label3 = New System.Windows.Forms.Label()
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
        Me.histBins = New System.Windows.Forms.Label()
        Me.HistBinSlider = New System.Windows.Forms.TrackBar()
        Me.RedCloudHistBins = New System.Windows.Forms.Label()
        Me.RGBSource = New System.Windows.Forms.GroupBox()
        Me.noColor_Input = New System.Windows.Forms.RadioButton()
        Me.KMeans_Basics = New System.Windows.Forms.RadioButton()
        Me.LUT_Basics = New System.Windows.Forms.RadioButton()
        Me.Reduction_Basics = New System.Windows.Forms.RadioButton()
        Me.BackProject_Full = New System.Windows.Forms.RadioButton()
        Me.GroupReduction = New System.Windows.Forms.GroupBox()
        Me.NoReduction = New System.Windows.Forms.RadioButton()
        Me.BitwiseReduction = New System.Windows.Forms.RadioButton()
        Me.SimpleReduction = New System.Windows.Forms.RadioButton()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.PCreductionLabel = New System.Windows.Forms.Label()
        Me.PCreductionSlider = New System.Windows.Forms.TrackBar()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.bitwiseLabel = New System.Windows.Forms.Label()
        Me.BitwiseReductionSlider = New System.Windows.Forms.TrackBar()
        Me.reduceXbits = New System.Windows.Forms.Label()
        Me.ColorLabel = New System.Windows.Forms.Label()
        Me.ColorReductionSlider = New System.Windows.Forms.TrackBar()
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
        Me.NoPointcloudData = New System.Windows.Forms.RadioButton()
        Me.RedCloud_Core = New System.Windows.Forms.RadioButton()
        Me.GuidedBP_Depth = New System.Windows.Forms.RadioButton()
        Me.RedCloudSliders.SuspendLayout()
        CType(Me.GridSizeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.TopViewThreshold, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.SideViewThreshold, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.YRangeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.XRangeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.RGBSource.SuspendLayout()
        Me.GroupReduction.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        CType(Me.PCreductionSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.BitwiseReductionSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.ColorReductionSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.RedCloudOnly.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'RedCloudSliders
        '
        Me.RedCloudSliders.Controls.Add(Me.GridsizeLabel)
        Me.RedCloudSliders.Controls.Add(Me.GridSizeSlider)
        Me.RedCloudSliders.Controls.Add(Me.Label3)
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
        Me.RedCloudSliders.Controls.Add(Me.histBins)
        Me.RedCloudSliders.Controls.Add(Me.HistBinSlider)
        Me.RedCloudSliders.Controls.Add(Me.RedCloudHistBins)
        Me.RedCloudSliders.Location = New System.Drawing.Point(12, 12)
        Me.RedCloudSliders.Name = "RedCloudSliders"
        Me.RedCloudSliders.Size = New System.Drawing.Size(831, 432)
        Me.RedCloudSliders.TabIndex = 2
        Me.RedCloudSliders.TabStop = False
        '
        'GridsizeLabel
        '
        Me.GridsizeLabel.AutoSize = True
        Me.GridsizeLabel.Location = New System.Drawing.Point(667, 380)
        Me.GridsizeLabel.Name = "GridsizeLabel"
        Me.GridsizeLabel.Size = New System.Drawing.Size(57, 20)
        Me.GridsizeLabel.TabIndex = 23
        Me.GridsizeLabel.Text = "Label5"
        '
        'GridSizeSlider
        '
        Me.GridSizeSlider.Location = New System.Drawing.Point(156, 374)
        Me.GridSizeSlider.Maximum = 200
        Me.GridSizeSlider.Minimum = 3
        Me.GridSizeSlider.Name = "GridSizeSlider"
        Me.GridSizeSlider.Size = New System.Drawing.Size(506, 69)
        Me.GridSizeSlider.TabIndex = 22
        Me.GridSizeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.GridSizeSlider.Value = 10
        '
        'Label3
        '
        Me.Label3.Location = New System.Drawing.Point(8, 380)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(151, 48)
        Me.Label3.TabIndex = 21
        Me.Label3.Text = "BackProjection Grid Size"
        '
        'TopLabel
        '
        Me.TopLabel.AutoSize = True
        Me.TopLabel.Location = New System.Drawing.Point(667, 305)
        Me.TopLabel.Name = "TopLabel"
        Me.TopLabel.Size = New System.Drawing.Size(57, 20)
        Me.TopLabel.TabIndex = 20
        Me.TopLabel.Text = "Label5"
        '
        'TopViewThreshold
        '
        Me.TopViewThreshold.Location = New System.Drawing.Point(156, 299)
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
        Me.Label6.Location = New System.Drawing.Point(8, 305)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(151, 48)
        Me.Label6.TabIndex = 18
        Me.Label6.Text = "Top View Red Threshold"
        '
        'SideLabel
        '
        Me.SideLabel.AutoSize = True
        Me.SideLabel.Location = New System.Drawing.Point(667, 230)
        Me.SideLabel.Name = "SideLabel"
        Me.SideLabel.Size = New System.Drawing.Size(57, 20)
        Me.SideLabel.TabIndex = 17
        Me.SideLabel.Text = "Label7"
        '
        'SideViewThreshold
        '
        Me.SideViewThreshold.Location = New System.Drawing.Point(156, 224)
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
        Me.Label8.Location = New System.Drawing.Point(8, 230)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(151, 43)
        Me.Label8.TabIndex = 15
        Me.Label8.Text = "Side View Red Threshold"
        '
        'YLabel
        '
        Me.YLabel.AutoSize = True
        Me.YLabel.Location = New System.Drawing.Point(667, 166)
        Me.YLabel.Name = "YLabel"
        Me.YLabel.Size = New System.Drawing.Size(57, 20)
        Me.YLabel.TabIndex = 14
        Me.YLabel.Text = "Label3"
        '
        'YRangeSlider
        '
        Me.YRangeSlider.Location = New System.Drawing.Point(156, 160)
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
        Me.Label4.Location = New System.Drawing.Point(8, 166)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(115, 20)
        Me.Label4.TabIndex = 12
        Me.Label4.Text = "Y-Range X100"
        '
        'XLabel
        '
        Me.XLabel.AutoSize = True
        Me.XLabel.Location = New System.Drawing.Point(667, 91)
        Me.XLabel.Name = "XLabel"
        Me.XLabel.Size = New System.Drawing.Size(57, 20)
        Me.XLabel.TabIndex = 11
        Me.XLabel.Text = "Label1"
        '
        'XRangeSlider
        '
        Me.XRangeSlider.Location = New System.Drawing.Point(156, 85)
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
        Me.Label2.Location = New System.Drawing.Point(8, 91)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(115, 20)
        Me.Label2.TabIndex = 9
        Me.Label2.Text = "X-Range X100"
        '
        'histBins
        '
        Me.histBins.AutoSize = True
        Me.histBins.Location = New System.Drawing.Point(667, 24)
        Me.histBins.Name = "histBins"
        Me.histBins.Size = New System.Drawing.Size(65, 20)
        Me.histBins.TabIndex = 8
        Me.histBins.Text = "histBins"
        '
        'HistBinSlider
        '
        Me.HistBinSlider.Location = New System.Drawing.Point(156, 18)
        Me.HistBinSlider.Maximum = 200
        Me.HistBinSlider.Minimum = 3
        Me.HistBinSlider.Name = "HistBinSlider"
        Me.HistBinSlider.Size = New System.Drawing.Size(506, 69)
        Me.HistBinSlider.TabIndex = 7
        Me.HistBinSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.HistBinSlider.Value = 5
        '
        'RedCloudHistBins
        '
        Me.RedCloudHistBins.Location = New System.Drawing.Point(8, 24)
        Me.RedCloudHistBins.Name = "RedCloudHistBins"
        Me.RedCloudHistBins.Size = New System.Drawing.Size(151, 45)
        Me.RedCloudHistBins.TabIndex = 6
        Me.RedCloudHistBins.Text = "RedCloud Histogram Bins"
        '
        'RGBSource
        '
        Me.RGBSource.Controls.Add(Me.noColor_Input)
        Me.RGBSource.Controls.Add(Me.KMeans_Basics)
        Me.RGBSource.Controls.Add(Me.LUT_Basics)
        Me.RGBSource.Controls.Add(Me.Reduction_Basics)
        Me.RGBSource.Controls.Add(Me.BackProject_Full)
        Me.RGBSource.Location = New System.Drawing.Point(1088, 30)
        Me.RGBSource.Name = "RGBSource"
        Me.RGBSource.Size = New System.Drawing.Size(250, 200)
        Me.RGBSource.TabIndex = 3
        Me.RGBSource.TabStop = False
        Me.RGBSource.Text = "Color Source"
        '
        'noColor_Input
        '
        Me.noColor_Input.AutoSize = True
        Me.noColor_Input.Location = New System.Drawing.Point(28, 161)
        Me.noColor_Input.Name = "noColor_Input"
        Me.noColor_Input.Size = New System.Drawing.Size(136, 24)
        Me.noColor_Input.TabIndex = 4
        Me.noColor_Input.TabStop = True
        Me.noColor_Input.Text = "No Color Input"
        Me.noColor_Input.UseVisualStyleBackColor = True
        '
        'KMeans_Basics
        '
        Me.KMeans_Basics.AutoSize = True
        Me.KMeans_Basics.Location = New System.Drawing.Point(28, 67)
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
        Me.LUT_Basics.Location = New System.Drawing.Point(28, 97)
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
        Me.Reduction_Basics.Location = New System.Drawing.Point(28, 127)
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
        Me.BackProject_Full.Location = New System.Drawing.Point(28, 37)
        Me.BackProject_Full.Name = "BackProject_Full"
        Me.BackProject_Full.Size = New System.Drawing.Size(153, 24)
        Me.BackProject_Full.TabIndex = 0
        Me.BackProject_Full.TabStop = True
        Me.BackProject_Full.Text = "BackProject_Full"
        Me.BackProject_Full.UseVisualStyleBackColor = True
        '
        'GroupReduction
        '
        Me.GroupReduction.Controls.Add(Me.NoReduction)
        Me.GroupReduction.Controls.Add(Me.BitwiseReduction)
        Me.GroupReduction.Controls.Add(Me.SimpleReduction)
        Me.GroupReduction.Location = New System.Drawing.Point(1364, 44)
        Me.GroupReduction.Name = "GroupReduction"
        Me.GroupReduction.Size = New System.Drawing.Size(250, 130)
        Me.GroupReduction.TabIndex = 5
        Me.GroupReduction.TabStop = False
        Me.GroupReduction.Text = "Reduction Options"
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
        Me.BitwiseReduction.Location = New System.Drawing.Point(28, 67)
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
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.PCreductionLabel)
        Me.GroupBox3.Controls.Add(Me.PCreductionSlider)
        Me.GroupBox3.Controls.Add(Me.Label5)
        Me.GroupBox3.Controls.Add(Me.bitwiseLabel)
        Me.GroupBox3.Controls.Add(Me.BitwiseReductionSlider)
        Me.GroupBox3.Controls.Add(Me.reduceXbits)
        Me.GroupBox3.Controls.Add(Me.ColorLabel)
        Me.GroupBox3.Controls.Add(Me.ColorReductionSlider)
        Me.GroupBox3.Controls.Add(Me.ColorReduce)
        Me.GroupBox3.Location = New System.Drawing.Point(849, 384)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(831, 236)
        Me.GroupBox3.TabIndex = 6
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Reduction Sliders"
        '
        'PCreductionLabel
        '
        Me.PCreductionLabel.AutoSize = True
        Me.PCreductionLabel.Location = New System.Drawing.Point(667, 161)
        Me.PCreductionLabel.Name = "PCreductionLabel"
        Me.PCreductionLabel.Size = New System.Drawing.Size(98, 20)
        Me.PCreductionLabel.TabIndex = 14
        Me.PCreductionLabel.Text = "BitwiseLabel"
        '
        'PCreductionSlider
        '
        Me.PCreductionSlider.Location = New System.Drawing.Point(156, 155)
        Me.PCreductionSlider.Maximum = 2500
        Me.PCreductionSlider.Minimum = 1
        Me.PCreductionSlider.Name = "PCreductionSlider"
        Me.PCreductionSlider.Size = New System.Drawing.Size(506, 69)
        Me.PCreductionSlider.TabIndex = 13
        Me.PCreductionSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.PCreductionSlider.Value = 250
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(8, 161)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(153, 20)
        Me.Label5.TabIndex = 12
        Me.Label5.Text = "Pointcloud reduction"
        '
        'bitwiseLabel
        '
        Me.bitwiseLabel.AutoSize = True
        Me.bitwiseLabel.Location = New System.Drawing.Point(667, 91)
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
        Me.ColorLabel.Location = New System.Drawing.Point(667, 24)
        Me.ColorLabel.Name = "ColorLabel"
        Me.ColorLabel.Size = New System.Drawing.Size(85, 20)
        Me.ColorLabel.TabIndex = 8
        Me.ColorLabel.Text = "ColorLabel"
        '
        'ColorReductionSlider
        '
        Me.ColorReductionSlider.Location = New System.Drawing.Point(156, 18)
        Me.ColorReductionSlider.Maximum = 255
        Me.ColorReductionSlider.Minimum = 1
        Me.ColorReductionSlider.Name = "ColorReductionSlider"
        Me.ColorReductionSlider.Size = New System.Drawing.Size(506, 69)
        Me.ColorReductionSlider.TabIndex = 7
        Me.ColorReductionSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.ColorReductionSlider.Value = 80
        '
        'ColorReduce
        '
        Me.ColorReduce.Location = New System.Drawing.Point(8, 24)
        Me.ColorReduce.Name = "ColorReduce"
        Me.ColorReduce.Size = New System.Drawing.Size(151, 45)
        Me.ColorReduce.TabIndex = 6
        Me.ColorReduce.Text = "Color Reduction"
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
        Me.RedCloudOnly.Location = New System.Drawing.Point(861, 23)
        Me.RedCloudOnly.Name = "RedCloudOnly"
        Me.RedCloudOnly.Size = New System.Drawing.Size(221, 290)
        Me.RedCloudOnly.TabIndex = 7
        Me.RedCloudOnly.TabStop = False
        Me.RedCloudOnly.Text = "PC Histogram Inputs"
        '
        'XYZReduction
        '
        Me.XYZReduction.AutoSize = True
        Me.XYZReduction.Location = New System.Drawing.Point(28, 247)
        Me.XYZReduction.Name = "XYZReduction"
        Me.XYZReduction.Size = New System.Drawing.Size(143, 24)
        Me.XYZReduction.TabIndex = 8
        Me.XYZReduction.TabStop = True
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
        Me.YZReduction.Text = "YZ Reduction"
        Me.YZReduction.UseVisualStyleBackColor = True
        '
        'XZReduction
        '
        Me.XZReduction.AutoSize = True
        Me.XZReduction.Location = New System.Drawing.Point(29, 177)
        Me.XZReduction.Name = "XZReduction"
        Me.XZReduction.Size = New System.Drawing.Size(132, 24)
        Me.XZReduction.TabIndex = 6
        Me.XZReduction.TabStop = True
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
        Me.XYReduction.Text = "XY Reduction"
        Me.XYReduction.UseVisualStyleBackColor = True
        '
        'ZReduction
        '
        Me.ZReduction.AutoSize = True
        Me.ZReduction.Location = New System.Drawing.Point(28, 107)
        Me.ZReduction.Name = "ZReduction"
        Me.ZReduction.Size = New System.Drawing.Size(121, 24)
        Me.ZReduction.TabIndex = 4
        Me.ZReduction.TabStop = True
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
        Me.XReduction.Text = "X Reduction"
        Me.XReduction.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.NoPointcloudData)
        Me.GroupBox1.Controls.Add(Me.RedCloud_Core)
        Me.GroupBox1.Controls.Add(Me.GuidedBP_Depth)
        Me.GroupBox1.Location = New System.Drawing.Point(1088, 242)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(250, 144)
        Me.GroupBox1.TabIndex = 5
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "RedCloud Depth Source"
        '
        'NoPointcloudData
        '
        Me.NoPointcloudData.AutoSize = True
        Me.NoPointcloudData.Location = New System.Drawing.Point(28, 99)
        Me.NoPointcloudData.Name = "NoPointcloudData"
        Me.NoPointcloudData.Size = New System.Drawing.Size(171, 24)
        Me.NoPointcloudData.TabIndex = 10
        Me.NoPointcloudData.TabStop = True
        Me.NoPointcloudData.Text = "No Pointcloud Data"
        Me.NoPointcloudData.UseVisualStyleBackColor = True
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
        'OptionsRedCloud
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1690, 632)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.RedCloudOnly)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.GroupReduction)
        Me.Controls.Add(Me.RGBSource)
        Me.Controls.Add(Me.RedCloudSliders)
        Me.Name = "OptionsRedCloud"
        Me.Text = "OptionsRedCloud"
        Me.RedCloudSliders.ResumeLayout(False)
        Me.RedCloudSliders.PerformLayout()
        CType(Me.GridSizeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.TopViewThreshold, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.SideViewThreshold, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.YRangeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.XRangeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.RGBSource.ResumeLayout(False)
        Me.RGBSource.PerformLayout()
        Me.GroupReduction.ResumeLayout(False)
        Me.GroupReduction.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        CType(Me.PCreductionSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.BitwiseReductionSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.ColorReductionSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.RedCloudOnly.ResumeLayout(False)
        Me.RedCloudOnly.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents RedCloudSliders As Windows.Forms.GroupBox
    Friend WithEvents histBins As Windows.Forms.Label
    Friend WithEvents HistBinSlider As Windows.Forms.TrackBar
    Friend WithEvents RedCloudHistBins As Windows.Forms.Label
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
    Friend WithEvents GridsizeLabel As Windows.Forms.Label
    Friend WithEvents GridSizeSlider As Windows.Forms.TrackBar
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents GroupReduction As Windows.Forms.GroupBox
    Friend WithEvents BitwiseReduction As Windows.Forms.RadioButton
    Friend WithEvents SimpleReduction As Windows.Forms.RadioButton
    Friend WithEvents GroupBox3 As Windows.Forms.GroupBox
    Friend WithEvents bitwiseLabel As Windows.Forms.Label
    Friend WithEvents BitwiseReductionSlider As Windows.Forms.TrackBar
    Friend WithEvents reduceXbits As Windows.Forms.Label
    Friend WithEvents ColorLabel As Windows.Forms.Label
    Friend WithEvents ColorReductionSlider As Windows.Forms.TrackBar
    Friend WithEvents ColorReduce As Windows.Forms.Label
    Friend WithEvents PCreductionLabel As Windows.Forms.Label
    Friend WithEvents PCreductionSlider As Windows.Forms.TrackBar
    Friend WithEvents Label5 As Windows.Forms.Label
    Friend WithEvents RedCloudOnly As Windows.Forms.GroupBox
    Friend WithEvents XZReduction As Windows.Forms.RadioButton
    Friend WithEvents XYReduction As Windows.Forms.RadioButton
    Friend WithEvents ZReduction As Windows.Forms.RadioButton
    Friend WithEvents YReduction As Windows.Forms.RadioButton
    Friend WithEvents XReduction As Windows.Forms.RadioButton
    Friend WithEvents XYZReduction As Windows.Forms.RadioButton
    Friend WithEvents YZReduction As Windows.Forms.RadioButton
    Friend WithEvents NoReduction As Windows.Forms.RadioButton
    Friend WithEvents noColor_Input As Windows.Forms.RadioButton
    Friend WithEvents GroupBox1 As Windows.Forms.GroupBox
    Friend WithEvents RedCloud_Core As Windows.Forms.RadioButton
    Friend WithEvents GuidedBP_Depth As Windows.Forms.RadioButton
    Friend WithEvents NoPointcloudData As Windows.Forms.RadioButton
End Class
