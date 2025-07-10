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
        Me.ReductionSliders = New System.Windows.Forms.GroupBox()
        Me.bitwiseLabel = New System.Windows.Forms.Label()
        Me.BitwiseReductionBar = New System.Windows.Forms.TrackBar()
        Me.reduceXbits = New System.Windows.Forms.Label()
        Me.TrackingMeanColor = New System.Windows.Forms.RadioButton()
        Me.TrackingColor = New System.Windows.Forms.RadioButton()
        Me.ColoringGroup = New System.Windows.Forms.GroupBox()
        Me.ReductionSliders.SuspendLayout()
        CType(Me.BitwiseReductionBar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.ColoringGroup.SuspendLayout()
        Me.SuspendLayout()
        '
        'ReductionSliders
        '
        Me.ReductionSliders.Controls.Add(Me.bitwiseLabel)
        Me.ReductionSliders.Controls.Add(Me.BitwiseReductionBar)
        Me.ReductionSliders.Controls.Add(Me.reduceXbits)
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
        'OptionsRedColor
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(950, 604)
        Me.Controls.Add(Me.ColoringGroup)
        Me.Controls.Add(Me.ReductionSliders)
        Me.Name = "OptionsRedColor"
        Me.Text = "OptionsRedCloud"
        Me.ReductionSliders.ResumeLayout(False)
        Me.ReductionSliders.PerformLayout()
        CType(Me.BitwiseReductionBar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ColoringGroup.ResumeLayout(False)
        Me.ColoringGroup.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents ReductionSliders As System.Windows.Forms.GroupBox
    Friend WithEvents bitwiseLabel As System.Windows.Forms.Label
    Friend WithEvents BitwiseReductionBar As System.Windows.Forms.TrackBar
    Friend WithEvents reduceXbits As System.Windows.Forms.Label
    Friend WithEvents TrackingMeanColor As Windows.Forms.RadioButton
    Friend WithEvents TrackingColor As Windows.Forms.RadioButton
    Friend WithEvents ColoringGroup As Windows.Forms.GroupBox
End Class
