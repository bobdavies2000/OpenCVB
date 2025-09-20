<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsFeatures
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OptionsFeatures))
        FeatureMethod = New ComboBox()
        EdgeMethods = New ComboBox()
        Label1 = New Label()
        Label2 = New Label()
        Label3 = New Label()
        DistanceSlider = New TrackBar()
        DistanceLabel = New Label()
        FeatureSampleSizeLabel = New Label()
        FeatureSampleSize = New TrackBar()
        Label5 = New Label()
        ColorDiffLabel = New Label()
        ColorDiffSlider = New TrackBar()
        Label7 = New Label()
        FeatureCorrelationLabel = New Label()
        MatchCorrSlider = New TrackBar()
        Label9 = New Label()
        GrayGroup = New GroupBox()
        ColorGroup = New GroupBox()
        CType(DistanceSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(FeatureSampleSize, ComponentModel.ISupportInitialize).BeginInit()
        CType(ColorDiffSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(MatchCorrSlider, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' FeatureMethod
        ' 
        FeatureMethod.FormattingEnabled = True
        FeatureMethod.Location = New Point(26, 45)
        FeatureMethod.Name = "FeatureMethod"
        FeatureMethod.Size = New Size(246, 33)
        FeatureMethod.TabIndex = 0
        ' 
        ' EdgeMethods
        ' 
        EdgeMethods.FormattingEnabled = True
        EdgeMethods.Location = New Point(303, 45)
        EdgeMethods.Name = "EdgeMethods"
        EdgeMethods.Size = New Size(246, 33)
        EdgeMethods.TabIndex = 1
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(303, 9)
        Label1.Name = "Label1"
        Label1.Size = New Size(120, 25)
        Label1.TabIndex = 2
        Label1.Text = "Edge Method"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(26, 9)
        Label2.Name = "Label2"
        Label2.Size = New Size(138, 25)
        Label2.TabIndex = 3
        Label2.Text = "Feature Method"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(26, 85)
        Label3.Name = "Label3"
        Label3.Size = New Size(254, 25)
        Label3.TabIndex = 4
        Label3.Text = "Min Distance between features"
        ' 
        ' DistanceSlider
        ' 
        DistanceSlider.Location = New Point(26, 113)
        DistanceSlider.Maximum = 100
        DistanceSlider.Name = "DistanceSlider"
        DistanceSlider.Size = New Size(523, 69)
        DistanceSlider.TabIndex = 5
        DistanceSlider.Value = 25
        ' 
        ' DistanceLabel
        ' 
        DistanceLabel.AutoSize = True
        DistanceLabel.Location = New Point(555, 113)
        DistanceLabel.Name = "DistanceLabel"
        DistanceLabel.Size = New Size(79, 25)
        DistanceLabel.TabIndex = 6
        DistanceLabel.Text = "Distance"
        ' 
        ' FeatureSampleSizeLabel
        ' 
        FeatureSampleSizeLabel.AutoSize = True
        FeatureSampleSizeLabel.Location = New Point(555, 179)
        FeatureSampleSizeLabel.Name = "FeatureSampleSizeLabel"
        FeatureSampleSizeLabel.Size = New Size(107, 25)
        FeatureSampleSizeLabel.TabIndex = 9
        FeatureSampleSizeLabel.Text = "Sample Size"
        ' 
        ' FeatureSampleSize
        ' 
        FeatureSampleSize.Location = New Point(26, 179)
        FeatureSampleSize.Maximum = 400
        FeatureSampleSize.Minimum = 1
        FeatureSampleSize.Name = "FeatureSampleSize"
        FeatureSampleSize.Size = New Size(523, 69)
        FeatureSampleSize.TabIndex = 8
        FeatureSampleSize.Value = 50
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(26, 151)
        Label5.Name = "Label5"
        Label5.Size = New Size(170, 25)
        Label5.TabIndex = 7
        Label5.Text = "Feature Sample Size"
        ' 
        ' ColorDiffLabel
        ' 
        ColorDiffLabel.AutoSize = True
        ColorDiffLabel.Location = New Point(555, 263)
        ColorDiffLabel.Name = "ColorDiffLabel"
        ColorDiffLabel.Size = New Size(125, 25)
        ColorDiffLabel.TabIndex = 12
        ColorDiffLabel.Text = "ColorDiffLabel"
        ' 
        ' ColorDiffSlider
        ' 
        ColorDiffSlider.Location = New Point(26, 263)
        ColorDiffSlider.Maximum = 100
        ColorDiffSlider.Minimum = 1
        ColorDiffSlider.Name = "ColorDiffSlider"
        ColorDiffSlider.Size = New Size(523, 69)
        ColorDiffSlider.TabIndex = 11
        ColorDiffSlider.Value = 75
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(26, 235)
        Label7.Name = "Label7"
        Label7.Size = New Size(287, 25)
        Label7.TabIndex = 10
        Label7.Text = "LowRes Color Difference Threshold"
        ' 
        ' FeatureCorrelationLabel
        ' 
        FeatureCorrelationLabel.AutoSize = True
        FeatureCorrelationLabel.Location = New Point(555, 338)
        FeatureCorrelationLabel.Name = "FeatureCorrelationLabel"
        FeatureCorrelationLabel.Size = New Size(162, 25)
        FeatureCorrelationLabel.TabIndex = 15
        FeatureCorrelationLabel.Text = "Feature Correlation"
        ' 
        ' MatchCorrSlider
        ' 
        MatchCorrSlider.Location = New Point(26, 338)
        MatchCorrSlider.Maximum = 100
        MatchCorrSlider.Name = "MatchCorrSlider"
        MatchCorrSlider.Size = New Size(523, 69)
        MatchCorrSlider.TabIndex = 14
        MatchCorrSlider.Value = 90
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.Location = New Point(26, 310)
        Label9.Name = "Label9"
        Label9.Size = New Size(236, 25)
        Label9.TabIndex = 13
        Label9.Text = "Match Correlation Threshold"
        ' 
        ' GrayGroup
        ' 
        GrayGroup.Location = New Point(723, 9)
        GrayGroup.Name = "GrayGroup"
        GrayGroup.Size = New Size(283, 426)
        GrayGroup.TabIndex = 16
        GrayGroup.TabStop = False
        GrayGroup.Text = "Grayscale source inputs"
        ' 
        ' ColorGroup
        ' 
        ColorGroup.Location = New Point(1029, 14)
        ColorGroup.Name = "ColorGroup"
        ColorGroup.Size = New Size(289, 415)
        ColorGroup.TabIndex = 17
        ColorGroup.TabStop = False
        ColorGroup.Text = "Color source inputs"
        ' 
        ' OptionsFeatures
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1420, 575)
        Controls.Add(ColorGroup)
        Controls.Add(GrayGroup)
        Controls.Add(FeatureCorrelationLabel)
        Controls.Add(MatchCorrSlider)
        Controls.Add(Label9)
        Controls.Add(ColorDiffLabel)
        Controls.Add(ColorDiffSlider)
        Controls.Add(Label7)
        Controls.Add(FeatureSampleSizeLabel)
        Controls.Add(FeatureSampleSize)
        Controls.Add(Label5)
        Controls.Add(DistanceLabel)
        Controls.Add(DistanceSlider)
        Controls.Add(Label3)
        Controls.Add(Label2)
        Controls.Add(Label1)
        Controls.Add(EdgeMethods)
        Controls.Add(FeatureMethod)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "OptionsFeatures"
        Text = "Important Options for Features, Edges, Lines, and Masks"
        CType(DistanceSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(FeatureSampleSize, ComponentModel.ISupportInitialize).EndInit()
        CType(ColorDiffSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(MatchCorrSlider, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents FeatureMethod As ComboBox
    Friend WithEvents EdgeMethods As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents DistanceSlider As TrackBar
    Friend WithEvents DistanceLabel As Label
    Friend WithEvents FeatureSampleSizeLabel As Label
    Friend WithEvents FeatureSampleSize As TrackBar
    Friend WithEvents Label5 As Label
    Friend WithEvents ColorDiffLabel As Label
    Friend WithEvents ColorDiffSlider As TrackBar
    Friend WithEvents Label7 As Label
    Friend WithEvents FeatureCorrelationLabel As Label
    Friend WithEvents MatchCorrSlider As TrackBar
    Friend WithEvents Label9 As Label
    Friend WithEvents GrayGroup As GroupBox
    Friend WithEvents ColorGroup As GroupBox
End Class
