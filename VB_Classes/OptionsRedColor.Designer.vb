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
        Me.ColorSourceLabel = New System.Windows.Forms.Label()
        Me.ColorSource = New System.Windows.Forms.ComboBox()
        Me.ColoringGroup = New System.Windows.Forms.GroupBox()
        Me.TrackingColor = New System.Windows.Forms.RadioButton()
        Me.TrackingMeanColor = New System.Windows.Forms.RadioButton()
        Me.ReductionTypeGroup.SuspendLayout()
        Me.ReductionSliders.SuspendLayout()
        CType(Me.BitwiseReductionBar, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.SimpleReductionBar, System.ComponentModel.ISupportInitialize).BeginInit()
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
    Friend WithEvents NoReduction As System.Windows.Forms.RadioButton
    Friend WithEvents ColorSourceLabel As System.Windows.Forms.Label
    Friend WithEvents ColorSource As System.Windows.Forms.ComboBox
    Friend WithEvents ColoringGroup As Windows.Forms.GroupBox
    Friend WithEvents TrackingColor As Windows.Forms.RadioButton
    Friend WithEvents TrackingMeanColor As Windows.Forms.RadioButton
End Class
