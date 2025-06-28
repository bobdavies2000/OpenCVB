<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OptionsRedColor
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
        Me.ColorSourceLabel = New System.Windows.Forms.Label()
        Me.ColorSource = New System.Windows.Forms.ComboBox()
        Me.ColoringGroup = New System.Windows.Forms.GroupBox()
        Me.TrackingColor = New System.Windows.Forms.RadioButton()
        Me.TrackingMeanColor = New System.Windows.Forms.RadioButton()
        Me.ReductionTypeGroup.SuspendLayout()
        Me.ReductionSliders.SuspendLayout()
        CType(Me.BitwiseReductionBar, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.SimpleReductionBar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.RedCloudOnly.SuspendLayout()
        Me.ColoringGroup.SuspendLayout()
        Me.SuspendLayout()
        '
        'ReductionTypeGroup
        '
        Me.ReductionTypeGroup.Controls.Add(Me.NoReduction)
        Me.ReductionTypeGroup.Controls.Add(Me.BitwiseReduction)
        Me.ReductionTypeGroup.Controls.Add(Me.UseSimpleReduction)
        Me.ReductionTypeGroup.Location = New System.Drawing.Point(36, 321)
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
        Me.ReductionSliders.Location = New System.Drawing.Point(36, 456)
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
        Me.SimpleReductionBar.Value = 200
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
        Me.RedCloudOnly.Location = New System.Drawing.Point(21, 12)
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
        'ColorSourceLabel
        '
        Me.ColorSourceLabel.AutoSize = True
        Me.ColorSourceLabel.Location = New System.Drawing.Point(259, 134)
        Me.ColorSourceLabel.Name = "ColorSourceLabel"
        Me.ColorSourceLabel.Size = New System.Drawing.Size(271, 20)
        Me.ColorSourceLabel.TabIndex = 10
        Me.ColorSourceLabel.Text = "Color Source when running RedColor"
        '
        'ColorSource
        '
        Me.ColorSource.FormattingEnabled = True
        Me.ColorSource.Location = New System.Drawing.Point(263, 157)
        Me.ColorSource.Name = "ColorSource"
        Me.ColorSource.Size = New System.Drawing.Size(222, 28)
        Me.ColorSource.TabIndex = 11
        '
        'ColoringGroup
        '
        Me.ColoringGroup.Controls.Add(Me.TrackingColor)
        Me.ColoringGroup.Controls.Add(Me.TrackingMeanColor)
        Me.ColoringGroup.Location = New System.Drawing.Point(263, 12)
        Me.ColoringGroup.Name = "ColoringGroup"
        Me.ColoringGroup.Size = New System.Drawing.Size(220, 103)
        Me.ColoringGroup.TabIndex = 78
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
        'OptionsRedColor
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(950, 604)
        Me.Controls.Add(Me.ColoringGroup)
        Me.Controls.Add(Me.ColorSource)
        Me.Controls.Add(Me.ColorSourceLabel)
        Me.Controls.Add(Me.RedCloudOnly)
        Me.Controls.Add(Me.ReductionSliders)
        Me.Controls.Add(Me.ReductionTypeGroup)
        Me.Name = "OptionsRedColor"
        Me.Text = "OptionsRedCloud"
        Me.ReductionTypeGroup.ResumeLayout(False)
        Me.ReductionTypeGroup.PerformLayout()
        Me.ReductionSliders.ResumeLayout(False)
        Me.ReductionSliders.PerformLayout()
        CType(Me.BitwiseReductionBar, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.SimpleReductionBar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.RedCloudOnly.ResumeLayout(False)
        Me.RedCloudOnly.PerformLayout()
        Me.ColoringGroup.ResumeLayout(False)
        Me.ColoringGroup.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
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
    Friend WithEvents ColorSourceLabel As System.Windows.Forms.Label
    Friend WithEvents ColorSource As System.Windows.Forms.ComboBox
    Friend WithEvents ColoringGroup As Windows.Forms.GroupBox
    Friend WithEvents TrackingColor As Windows.Forms.RadioButton
    Friend WithEvents TrackingMeanColor As Windows.Forms.RadioButton
End Class
