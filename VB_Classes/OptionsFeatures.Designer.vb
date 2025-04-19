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
        Me.numLinesLabel = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.NumberLinesSlider = New System.Windows.Forms.TrackBar()
        Me.DistanceLabel = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.DistanceSlider = New System.Windows.Forms.TrackBar()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.FeatureMethod = New System.Windows.Forms.ComboBox()
        Me.FeatureCorrelationLabel = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.FCorrSlider = New System.Windows.Forms.TrackBar()
        Me.LineGroup.SuspendLayout()
        CType(Me.NumberLinesSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.DistanceSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.FCorrSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'LineGroup
        '
        Me.LineGroup.Controls.Add(Me.HorizRadio)
        Me.LineGroup.Controls.Add(Me.verticalRadio)
        Me.LineGroup.Location = New System.Drawing.Point(796, 12)
        Me.LineGroup.Name = "LineGroup"
        Me.LineGroup.Size = New System.Drawing.Size(328, 568)
        Me.LineGroup.TabIndex = 1
        Me.LineGroup.TabStop = False
        Me.LineGroup.Text = "Lines"
        '
        'HorizRadio
        '
        Me.HorizRadio.AutoSize = True
        Me.HorizRadio.Location = New System.Drawing.Point(27, 80)
        Me.HorizRadio.Name = "HorizRadio"
        Me.HorizRadio.Size = New System.Drawing.Size(148, 24)
        Me.HorizRadio.TabIndex = 1
        Me.HorizRadio.TabStop = True
        Me.HorizRadio.Text = "Horizontal Lines"
        Me.HorizRadio.UseVisualStyleBackColor = True
        '
        'verticalRadio
        '
        Me.verticalRadio.AutoSize = True
        Me.verticalRadio.Location = New System.Drawing.Point(27, 49)
        Me.verticalRadio.Name = "verticalRadio"
        Me.verticalRadio.Size = New System.Drawing.Size(129, 24)
        Me.verticalRadio.TabIndex = 0
        Me.verticalRadio.TabStop = True
        Me.verticalRadio.Text = "Vertical Lines"
        Me.verticalRadio.UseVisualStyleBackColor = True
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
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(32, 68)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(105, 20)
        Me.Label3.TabIndex = 23
        Me.Label3.Text = "Edge Method"
        '
        'EdgeMethods
        '
        Me.EdgeMethods.FormattingEnabled = True
        Me.EdgeMethods.Location = New System.Drawing.Point(28, 92)
        Me.EdgeMethods.Name = "EdgeMethods"
        Me.EdgeMethods.Size = New System.Drawing.Size(246, 28)
        Me.EdgeMethods.TabIndex = 22
        '
        'numLinesLabel
        '
        Me.numLinesLabel.AutoSize = True
        Me.numLinesLabel.Location = New System.Drawing.Point(534, 238)
        Me.numLinesLabel.Name = "numLinesLabel"
        Me.numLinesLabel.Size = New System.Drawing.Size(125, 20)
        Me.numLinesLabel.TabIndex = 21
        Me.numLinesLabel.Text = "Number of Lines"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(32, 215)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(125, 20)
        Me.Label4.TabIndex = 20
        Me.Label4.Text = "Number of Lines"
        '
        'NumberLinesSlider
        '
        Me.NumberLinesSlider.Location = New System.Drawing.Point(28, 238)
        Me.NumberLinesSlider.Maximum = 100
        Me.NumberLinesSlider.Minimum = 1
        Me.NumberLinesSlider.Name = "NumberLinesSlider"
        Me.NumberLinesSlider.Size = New System.Drawing.Size(476, 69)
        Me.NumberLinesSlider.TabIndex = 19
        Me.NumberLinesSlider.Value = 75
        '
        'DistanceLabel
        '
        Me.DistanceLabel.AutoSize = True
        Me.DistanceLabel.Location = New System.Drawing.Point(534, 163)
        Me.DistanceLabel.Name = "DistanceLabel"
        Me.DistanceLabel.Size = New System.Drawing.Size(72, 20)
        Me.DistanceLabel.TabIndex = 18
        Me.DistanceLabel.Text = "Distance"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(32, 140)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(229, 20)
        Me.Label2.TabIndex = 17
        Me.Label2.Text = "Min Distance between features"
        '
        'DistanceSlider
        '
        Me.DistanceSlider.Location = New System.Drawing.Point(28, 163)
        Me.DistanceSlider.Maximum = 100
        Me.DistanceSlider.Minimum = 1
        Me.DistanceSlider.Name = "DistanceSlider"
        Me.DistanceSlider.Size = New System.Drawing.Size(476, 69)
        Me.DistanceSlider.TabIndex = 16
        Me.DistanceSlider.Value = 25
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(32, 11)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(123, 20)
        Me.Label1.TabIndex = 15
        Me.Label1.Text = "Feature Method"
        '
        'FeatureMethod
        '
        Me.FeatureMethod.FormattingEnabled = True
        Me.FeatureMethod.Location = New System.Drawing.Point(28, 35)
        Me.FeatureMethod.Name = "FeatureMethod"
        Me.FeatureMethod.Size = New System.Drawing.Size(246, 28)
        Me.FeatureMethod.TabIndex = 14
        '
        'FeatureCorrelationLabel
        '
        Me.FeatureCorrelationLabel.AutoSize = True
        Me.FeatureCorrelationLabel.Location = New System.Drawing.Point(534, 323)
        Me.FeatureCorrelationLabel.Name = "FeatureCorrelationLabel"
        Me.FeatureCorrelationLabel.Size = New System.Drawing.Size(146, 20)
        Me.FeatureCorrelationLabel.TabIndex = 26
        Me.FeatureCorrelationLabel.Text = "Feature Correlation"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(32, 300)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(220, 20)
        Me.Label6.TabIndex = 25
        Me.Label6.Text = "Feature Correlation Threshold"
        '
        'FCorrSlider
        '
        Me.FCorrSlider.Location = New System.Drawing.Point(28, 323)
        Me.FCorrSlider.Maximum = 100
        Me.FCorrSlider.Minimum = 1
        Me.FCorrSlider.Name = "FCorrSlider"
        Me.FCorrSlider.Size = New System.Drawing.Size(476, 69)
        Me.FCorrSlider.TabIndex = 24
        Me.FCorrSlider.Value = 75
        '
        'OptionsFeatures
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1419, 615)
        Me.Controls.Add(Me.FeatureCorrelationLabel)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.FCorrSlider)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.EdgeMethods)
        Me.Controls.Add(Me.numLinesLabel)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.NumberLinesSlider)
        Me.Controls.Add(Me.DistanceLabel)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.DistanceSlider)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.FeatureMethod)
        Me.Controls.Add(Me.MaskGroup)
        Me.Controls.Add(Me.LineGroup)
        Me.Name = "OptionsFeatures"
        Me.Text = "Important Options for Features, Edges, Lines, and Masks"
        Me.LineGroup.ResumeLayout(False)
        Me.LineGroup.PerformLayout()
        CType(Me.NumberLinesSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.DistanceSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.FCorrSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents LineGroup As Windows.Forms.GroupBox
    Friend WithEvents MaskGroup As Windows.Forms.GroupBox
    Friend WithEvents HorizRadio As Windows.Forms.RadioButton
    Friend WithEvents verticalRadio As Windows.Forms.RadioButton
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents EdgeMethods As Windows.Forms.ComboBox
    Friend WithEvents numLinesLabel As Windows.Forms.Label
    Friend WithEvents Label4 As Windows.Forms.Label
    Friend WithEvents NumberLinesSlider As Windows.Forms.TrackBar
    Friend WithEvents DistanceLabel As Windows.Forms.Label
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents DistanceSlider As Windows.Forms.TrackBar
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents FeatureMethod As Windows.Forms.ComboBox
    Friend WithEvents FeatureCorrelationLabel As Windows.Forms.Label
    Friend WithEvents Label6 As Windows.Forms.Label
    Friend WithEvents FCorrSlider As Windows.Forms.TrackBar
End Class
