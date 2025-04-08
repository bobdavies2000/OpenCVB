<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OptionsFeatures
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
        Me.FeaturesGroup = New System.Windows.Forms.GroupBox()
        Me.DistanceLabel = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.DistanceSlider = New System.Windows.Forms.TrackBar()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.FeatureMethod = New System.Windows.Forms.ComboBox()
        Me.LineGroup = New System.Windows.Forms.GroupBox()
        Me.MaskGroup = New System.Windows.Forms.GroupBox()
        Me.FeaturesGroup.SuspendLayout()
        CType(Me.DistanceSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'FeaturesGroup
        '
        Me.FeaturesGroup.Controls.Add(Me.DistanceLabel)
        Me.FeaturesGroup.Controls.Add(Me.Label2)
        Me.FeaturesGroup.Controls.Add(Me.DistanceSlider)
        Me.FeaturesGroup.Controls.Add(Me.Label1)
        Me.FeaturesGroup.Controls.Add(Me.FeatureMethod)
        Me.FeaturesGroup.Location = New System.Drawing.Point(28, 12)
        Me.FeaturesGroup.Name = "FeaturesGroup"
        Me.FeaturesGroup.Size = New System.Drawing.Size(762, 568)
        Me.FeaturesGroup.TabIndex = 0
        Me.FeaturesGroup.TabStop = False
        Me.FeaturesGroup.Text = "Features"
        '
        'DistanceLabel
        '
        Me.DistanceLabel.AutoSize = True
        Me.DistanceLabel.Location = New System.Drawing.Point(526, 133)
        Me.DistanceLabel.Name = "DistanceLabel"
        Me.DistanceLabel.Size = New System.Drawing.Size(72, 20)
        Me.DistanceLabel.TabIndex = 8
        Me.DistanceLabel.Text = "Distance"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(24, 110)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(229, 20)
        Me.Label2.TabIndex = 7
        Me.Label2.Text = "Min Distance between features"
        '
        'DistanceSlider
        '
        Me.DistanceSlider.Location = New System.Drawing.Point(20, 133)
        Me.DistanceSlider.Maximum = 100
        Me.DistanceSlider.Minimum = 1
        Me.DistanceSlider.Name = "DistanceSlider"
        Me.DistanceSlider.Size = New System.Drawing.Size(475, 69)
        Me.DistanceSlider.TabIndex = 6
        Me.DistanceSlider.Value = 25
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(24, 37)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(63, 20)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "Method"
        '
        'FeatureMethod
        '
        Me.FeatureMethod.FormattingEnabled = True
        Me.FeatureMethod.Location = New System.Drawing.Point(20, 62)
        Me.FeatureMethod.Name = "FeatureMethod"
        Me.FeatureMethod.Size = New System.Drawing.Size(246, 28)
        Me.FeatureMethod.TabIndex = 4
        '
        'LineGroup
        '
        Me.LineGroup.Location = New System.Drawing.Point(796, 12)
        Me.LineGroup.Name = "LineGroup"
        Me.LineGroup.Size = New System.Drawing.Size(328, 568)
        Me.LineGroup.TabIndex = 1
        Me.LineGroup.TabStop = False
        Me.LineGroup.Text = "Lines"
        '
        'MaskGroup
        '
        Me.MaskGroup.Location = New System.Drawing.Point(1038, 12)
        Me.MaskGroup.Name = "MaskGroup"
        Me.MaskGroup.Size = New System.Drawing.Size(328, 568)
        Me.MaskGroup.TabIndex = 2
        Me.MaskGroup.TabStop = False
        Me.MaskGroup.Text = "Masks"
        '
        'OptionsFeatures
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1419, 616)
        Me.Controls.Add(Me.MaskGroup)
        Me.Controls.Add(Me.LineGroup)
        Me.Controls.Add(Me.FeaturesGroup)
        Me.Name = "OptionsFeatures"
        Me.Text = "OptionsFeatures"
        Me.FeaturesGroup.ResumeLayout(False)
        Me.FeaturesGroup.PerformLayout()
        CType(Me.DistanceSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents FeaturesGroup As Windows.Forms.GroupBox
    Friend WithEvents LineGroup As Windows.Forms.GroupBox
    Friend WithEvents MaskGroup As Windows.Forms.GroupBox
    Friend WithEvents FeatureMethod As Windows.Forms.ComboBox
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents DistanceSlider As Windows.Forms.TrackBar
    Friend WithEvents DistanceLabel As Windows.Forms.Label
End Class
