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
        Me.LineGroup = New System.Windows.Forms.GroupBox()
        Me.HorizRadio = New System.Windows.Forms.RadioButton()
        Me.verticalRadio = New System.Windows.Forms.RadioButton()
        Me.MaskGroup = New System.Windows.Forms.GroupBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.EdgeMethods = New System.Windows.Forms.ComboBox()
        Me.FeatureCorrelationLabel = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.FCorrSlider = New System.Windows.Forms.TrackBar()
        Me.DistanceLabel = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.DistanceSlider = New System.Windows.Forms.TrackBar()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.FeatureMethod = New System.Windows.Forms.ComboBox()
        Me.LineGroup.SuspendLayout()
        CType(Me.FCorrSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DistanceSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'LineGroup
        '
        Me.LineGroup.Controls.Add(Me.HorizRadio)
        Me.LineGroup.Controls.Add(Me.verticalRadio)
        Me.LineGroup.Location = New System.Drawing.Point(531, 8)
        Me.LineGroup.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.LineGroup.Name = "LineGroup"
        Me.LineGroup.Padding = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.LineGroup.Size = New System.Drawing.Size(219, 369)
        Me.LineGroup.TabIndex = 1
        Me.LineGroup.TabStop = False
        Me.LineGroup.Text = "Lines"
        '
        'HorizRadio
        '
        Me.HorizRadio.AutoSize = True
        Me.HorizRadio.Location = New System.Drawing.Point(18, 52)
        Me.HorizRadio.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.HorizRadio.Name = "HorizRadio"
        Me.HorizRadio.Size = New System.Drawing.Size(100, 17)
        Me.HorizRadio.TabIndex = 1
        Me.HorizRadio.TabStop = True
        Me.HorizRadio.Text = "Horizontal Lines"
        Me.HorizRadio.UseVisualStyleBackColor = True
        '
        'verticalRadio
        '
        Me.verticalRadio.AutoSize = True
        Me.verticalRadio.Location = New System.Drawing.Point(18, 32)
        Me.verticalRadio.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.verticalRadio.Name = "verticalRadio"
        Me.verticalRadio.Size = New System.Drawing.Size(88, 17)
        Me.verticalRadio.TabIndex = 0
        Me.verticalRadio.TabStop = True
        Me.verticalRadio.Text = "Vertical Lines"
        Me.verticalRadio.UseVisualStyleBackColor = True
        '
        'MaskGroup
        '
        Me.MaskGroup.Location = New System.Drawing.Point(692, 8)
        Me.MaskGroup.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.MaskGroup.Name = "MaskGroup"
        Me.MaskGroup.Padding = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.MaskGroup.Size = New System.Drawing.Size(219, 369)
        Me.MaskGroup.TabIndex = 2
        Me.MaskGroup.TabStop = False
        Me.MaskGroup.Text = "Masks"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(21, 44)
        Me.Label3.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(71, 13)
        Me.Label3.TabIndex = 23
        Me.Label3.Text = "Edge Method"
        '
        'EdgeMethods
        '
        Me.EdgeMethods.FormattingEnabled = True
        Me.EdgeMethods.Location = New System.Drawing.Point(19, 60)
        Me.EdgeMethods.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.EdgeMethods.Name = "EdgeMethods"
        Me.EdgeMethods.Size = New System.Drawing.Size(165, 21)
        Me.EdgeMethods.TabIndex = 22
        '
        'FeatureCorrelationLabel
        '
        Me.FeatureCorrelationLabel.AutoSize = True
        Me.FeatureCorrelationLabel.Location = New System.Drawing.Point(356, 161)
        Me.FeatureCorrelationLabel.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.FeatureCorrelationLabel.Name = "FeatureCorrelationLabel"
        Me.FeatureCorrelationLabel.Size = New System.Drawing.Size(96, 13)
        Me.FeatureCorrelationLabel.TabIndex = 21
        Me.FeatureCorrelationLabel.Text = "Feature Correlation"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(21, 146)
        Me.Label4.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(146, 13)
        Me.Label4.TabIndex = 20
        Me.Label4.Text = "Feature Correlation Threshold"
        '
        'FCorrSlider
        '
        Me.FCorrSlider.Location = New System.Drawing.Point(19, 161)
        Me.FCorrSlider.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.FCorrSlider.Maximum = 100
        Me.FCorrSlider.Minimum = 1
        Me.FCorrSlider.Name = "FCorrSlider"
        Me.FCorrSlider.Size = New System.Drawing.Size(317, 45)
        Me.FCorrSlider.TabIndex = 19
        Me.FCorrSlider.Value = 75
        '
        'DistanceLabel
        '
        Me.DistanceLabel.AutoSize = True
        Me.DistanceLabel.Location = New System.Drawing.Point(356, 106)
        Me.DistanceLabel.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.DistanceLabel.Name = "DistanceLabel"
        Me.DistanceLabel.Size = New System.Drawing.Size(49, 13)
        Me.DistanceLabel.TabIndex = 18
        Me.DistanceLabel.Text = "Distance"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(21, 91)
        Me.Label2.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(154, 13)
        Me.Label2.TabIndex = 17
        Me.Label2.Text = "Min Distance between features"
        '
        'DistanceSlider
        '
        Me.DistanceSlider.Location = New System.Drawing.Point(19, 106)
        Me.DistanceSlider.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.DistanceSlider.Maximum = 100
        Me.DistanceSlider.Minimum = 1
        Me.DistanceSlider.Name = "DistanceSlider"
        Me.DistanceSlider.Size = New System.Drawing.Size(317, 45)
        Me.DistanceSlider.TabIndex = 16
        Me.DistanceSlider.Value = 25
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(21, 7)
        Me.Label1.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(82, 13)
        Me.Label1.TabIndex = 15
        Me.Label1.Text = "Feature Method"
        '
        'FeatureMethod
        '
        Me.FeatureMethod.FormattingEnabled = True
        Me.FeatureMethod.Location = New System.Drawing.Point(19, 23)
        Me.FeatureMethod.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.FeatureMethod.Name = "FeatureMethod"
        Me.FeatureMethod.Size = New System.Drawing.Size(165, 21)
        Me.FeatureMethod.TabIndex = 14
        '
        'OptionsFeatures
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(946, 400)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.EdgeMethods)
        Me.Controls.Add(Me.FeatureCorrelationLabel)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.FCorrSlider)
        Me.Controls.Add(Me.DistanceLabel)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.DistanceSlider)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.FeatureMethod)
        Me.Controls.Add(Me.MaskGroup)
        Me.Controls.Add(Me.LineGroup)
        Me.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.Name = "OptionsFeatures"
        Me.Text = "Important Options for Features, Edges, Lines, and Masks"
        Me.LineGroup.ResumeLayout(False)
        Me.LineGroup.PerformLayout()
        CType(Me.FCorrSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DistanceSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents LineGroup As Windows.Forms.GroupBox
    Friend WithEvents MaskGroup As Windows.Forms.GroupBox
    Friend WithEvents HorizRadio As Windows.Forms.RadioButton
    Friend WithEvents verticalRadio As Windows.Forms.RadioButton
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents EdgeMethods As Windows.Forms.ComboBox
    Friend WithEvents FeatureCorrelationLabel As Windows.Forms.Label
    Friend WithEvents Label4 As Windows.Forms.Label
    Friend WithEvents FCorrSlider As Windows.Forms.TrackBar
    Friend WithEvents DistanceLabel As Windows.Forms.Label
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents DistanceSlider As Windows.Forms.TrackBar
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents FeatureMethod As Windows.Forms.ComboBox
End Class
