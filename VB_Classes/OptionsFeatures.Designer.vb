﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
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
        Me.Label3 = New System.Windows.Forms.Label()
        Me.EdgeMethods = New System.Windows.Forms.ComboBox()
        Me.DistanceLabel = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.DistanceSlider = New System.Windows.Forms.TrackBar()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.FeatureMethod = New System.Windows.Forms.ComboBox()
        Me.FeatureCorrelationLabel = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.MatchCorrSlider = New System.Windows.Forms.TrackBar()
        Me.ColorDiffLabel = New System.Windows.Forms.Label()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.ColorDiffSlider = New System.Windows.Forms.TrackBar()
        Me.FeatureSampleSizeLabel = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.FeatureSampleSize = New System.Windows.Forms.TrackBar()
        Me.GrayGroup = New System.Windows.Forms.GroupBox()
        Me.ColorGroup = New System.Windows.Forms.GroupBox()
        CType(Me.DistanceSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.MatchCorrSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.ColorDiffSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.FeatureSampleSize, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(284, 11)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(105, 20)
        Me.Label3.TabIndex = 23
        Me.Label3.Text = "Edge Method"
        '
        'EdgeMethods
        '
        Me.EdgeMethods.FormattingEnabled = True
        Me.EdgeMethods.Location = New System.Drawing.Point(280, 35)
        Me.EdgeMethods.Name = "EdgeMethods"
        Me.EdgeMethods.Size = New System.Drawing.Size(246, 28)
        Me.EdgeMethods.TabIndex = 22
        '
        'DistanceLabel
        '
        Me.DistanceLabel.AutoSize = True
        Me.DistanceLabel.Location = New System.Drawing.Point(499, 108)
        Me.DistanceLabel.Name = "DistanceLabel"
        Me.DistanceLabel.Size = New System.Drawing.Size(72, 20)
        Me.DistanceLabel.TabIndex = 18
        Me.DistanceLabel.Text = "Distance"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(32, 75)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(229, 20)
        Me.Label2.TabIndex = 17
        Me.Label2.Text = "Min Distance between features"
        '
        'DistanceSlider
        '
        Me.DistanceSlider.Location = New System.Drawing.Point(28, 98)
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
        Me.FeatureCorrelationLabel.Location = New System.Drawing.Point(499, 441)
        Me.FeatureCorrelationLabel.Name = "FeatureCorrelationLabel"
        Me.FeatureCorrelationLabel.Size = New System.Drawing.Size(146, 20)
        Me.FeatureCorrelationLabel.TabIndex = 26
        Me.FeatureCorrelationLabel.Text = "Feature Correlation"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(32, 408)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(208, 20)
        Me.Label6.TabIndex = 25
        Me.Label6.Text = "Match Correlation Threshold"
        '
        'MatchCorrSlider
        '
        Me.MatchCorrSlider.Location = New System.Drawing.Point(28, 431)
        Me.MatchCorrSlider.Maximum = 100
        Me.MatchCorrSlider.Minimum = 1
        Me.MatchCorrSlider.Name = "MatchCorrSlider"
        Me.MatchCorrSlider.Size = New System.Drawing.Size(476, 69)
        Me.MatchCorrSlider.TabIndex = 24
        Me.MatchCorrSlider.Value = 75
        '
        'ColorDiffLabel
        '
        Me.ColorDiffLabel.AutoSize = True
        Me.ColorDiffLabel.Location = New System.Drawing.Point(499, 343)
        Me.ColorDiffLabel.Name = "ColorDiffLabel"
        Me.ColorDiffLabel.Size = New System.Drawing.Size(146, 20)
        Me.ColorDiffLabel.TabIndex = 29
        Me.ColorDiffLabel.Text = "Feature Correlation"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(32, 310)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(260, 20)
        Me.Label7.TabIndex = 28
        Me.Label7.Text = "LowRes Color Difference Threshold"
        '
        'ColorDiffSlider
        '
        Me.ColorDiffSlider.Location = New System.Drawing.Point(28, 336)
        Me.ColorDiffSlider.Maximum = 100
        Me.ColorDiffSlider.Minimum = 1
        Me.ColorDiffSlider.Name = "ColorDiffSlider"
        Me.ColorDiffSlider.Size = New System.Drawing.Size(476, 69)
        Me.ColorDiffSlider.TabIndex = 27
        Me.ColorDiffSlider.Value = 75
        '
        'FeatureSampleSizeLabel
        '
        Me.FeatureSampleSizeLabel.AutoSize = True
        Me.FeatureSampleSizeLabel.Location = New System.Drawing.Point(507, 160)
        Me.FeatureSampleSizeLabel.Name = "FeatureSampleSizeLabel"
        Me.FeatureSampleSizeLabel.Size = New System.Drawing.Size(175, 20)
        Me.FeatureSampleSizeLabel.TabIndex = 35
        Me.FeatureSampleSizeLabel.Text = "Selected Feature Index"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(32, 127)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(158, 20)
        Me.Label5.TabIndex = 34
        Me.Label5.Text = "Feature Sample Size"
        '
        'FeatureSampleSize
        '
        Me.FeatureSampleSize.Location = New System.Drawing.Point(28, 150)
        Me.FeatureSampleSize.Maximum = 400
        Me.FeatureSampleSize.Minimum = 1
        Me.FeatureSampleSize.Name = "FeatureSampleSize"
        Me.FeatureSampleSize.Size = New System.Drawing.Size(476, 69)
        Me.FeatureSampleSize.TabIndex = 33
        Me.FeatureSampleSize.Value = 50
        '
        'GrayGroup
        '
        Me.GrayGroup.Location = New System.Drawing.Point(749, 12)
        Me.GrayGroup.Name = "GrayGroup"
        Me.GrayGroup.Size = New System.Drawing.Size(284, 393)
        Me.GrayGroup.TabIndex = 2
        Me.GrayGroup.TabStop = False
        Me.GrayGroup.Text = "Grayscale source inputs"
        '
        'ColorGroup
        '
        Me.ColorGroup.Location = New System.Drawing.Point(1049, 11)
        Me.ColorGroup.Name = "ColorGroup"
        Me.ColorGroup.Size = New System.Drawing.Size(284, 393)
        Me.ColorGroup.TabIndex = 3
        Me.ColorGroup.TabStop = False
        Me.ColorGroup.Text = "Color source inputs"
        '
        'OptionsFeatures
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1419, 615)
        Me.Controls.Add(Me.ColorGroup)
        Me.Controls.Add(Me.FeatureSampleSizeLabel)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.FeatureSampleSize)
        Me.Controls.Add(Me.ColorDiffLabel)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.ColorDiffSlider)
        Me.Controls.Add(Me.FeatureCorrelationLabel)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.MatchCorrSlider)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.EdgeMethods)
        Me.Controls.Add(Me.DistanceLabel)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.DistanceSlider)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.FeatureMethod)
        Me.Controls.Add(Me.GrayGroup)
        Me.Name = "OptionsFeatures"
        Me.Text = "Important Options for Features, Edges, Lines, and Masks"
        CType(Me.DistanceSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.MatchCorrSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.ColorDiffSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.FeatureSampleSize, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents EdgeMethods As Windows.Forms.ComboBox
    Friend WithEvents DistanceLabel As Windows.Forms.Label
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents DistanceSlider As Windows.Forms.TrackBar
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents FeatureMethod As Windows.Forms.ComboBox
    Friend WithEvents FeatureCorrelationLabel As Windows.Forms.Label
    Friend WithEvents Label6 As Windows.Forms.Label
    Friend WithEvents MatchCorrSlider As Windows.Forms.TrackBar
    Friend WithEvents ColorDiffLabel As Windows.Forms.Label
    Friend WithEvents Label7 As Windows.Forms.Label
    Friend WithEvents ColorDiffSlider As Windows.Forms.TrackBar
    Friend WithEvents FeatureSampleSizeLabel As Windows.Forms.Label
    Friend WithEvents Label5 As Windows.Forms.Label
    Friend WithEvents FeatureSampleSize As Windows.Forms.TrackBar
    Friend WithEvents GrayGroup As Windows.Forms.GroupBox
    Friend WithEvents ColorGroup As Windows.Forms.GroupBox
End Class
