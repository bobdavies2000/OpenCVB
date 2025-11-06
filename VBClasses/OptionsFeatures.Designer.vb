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
        FeatureSampleSizeLabel = New Label()
        FeatureSampleSize = New TrackBar()
        Label5 = New Label()
        FeatureCorrelationLabel = New Label()
        MatchCorrSlider = New TrackBar()
        Label9 = New Label()
        GrayGroup = New GroupBox()
        ColorGroup = New GroupBox()
        Label4 = New Label()
        ColorSource = New ComboBox()
        CType(FeatureSampleSize, ComponentModel.ISupportInitialize).BeginInit()
        CType(MatchCorrSlider, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' FeatureMethod
        ' 
        FeatureMethod.FormattingEnabled = True
        FeatureMethod.Location = New Point(31, 54)
        FeatureMethod.Margin = New Padding(4)
        FeatureMethod.Name = "FeatureMethod"
        FeatureMethod.Size = New Size(294, 38)
        FeatureMethod.TabIndex = 0
        ' 
        ' EdgeMethods
        ' 
        EdgeMethods.FormattingEnabled = True
        EdgeMethods.Location = New Point(364, 54)
        EdgeMethods.Margin = New Padding(4)
        EdgeMethods.Name = "EdgeMethods"
        EdgeMethods.Size = New Size(294, 38)
        EdgeMethods.TabIndex = 1
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(364, 11)
        Label1.Margin = New Padding(4, 0, 4, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(145, 30)
        Label1.TabIndex = 2
        Label1.Text = "Edge Method"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(31, 11)
        Label2.Margin = New Padding(4, 0, 4, 0)
        Label2.Name = "Label2"
        Label2.Size = New Size(169, 30)
        Label2.TabIndex = 3
        Label2.Text = "Feature Method"
        ' 
        ' FeatureSampleSizeLabel
        ' 
        FeatureSampleSizeLabel.AutoSize = True
        FeatureSampleSizeLabel.Location = New Point(666, 215)
        FeatureSampleSizeLabel.Margin = New Padding(4, 0, 4, 0)
        FeatureSampleSizeLabel.Name = "FeatureSampleSizeLabel"
        FeatureSampleSizeLabel.Size = New Size(130, 30)
        FeatureSampleSizeLabel.TabIndex = 9
        FeatureSampleSizeLabel.Text = "Sample Size"
        ' 
        ' FeatureSampleSize
        ' 
        FeatureSampleSize.Location = New Point(31, 215)
        FeatureSampleSize.Margin = New Padding(4)
        FeatureSampleSize.Maximum = 400
        FeatureSampleSize.Minimum = 1
        FeatureSampleSize.Name = "FeatureSampleSize"
        FeatureSampleSize.Size = New Size(628, 69)
        FeatureSampleSize.TabIndex = 8
        FeatureSampleSize.Value = 100
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(31, 181)
        Label5.Margin = New Padding(4, 0, 4, 0)
        Label5.Name = "Label5"
        Label5.Size = New Size(209, 30)
        Label5.TabIndex = 7
        Label5.Text = "Feature Sample Size"
        ' 
        ' FeatureCorrelationLabel
        ' 
        FeatureCorrelationLabel.AutoSize = True
        FeatureCorrelationLabel.Location = New Point(666, 406)
        FeatureCorrelationLabel.Margin = New Padding(4, 0, 4, 0)
        FeatureCorrelationLabel.Name = "FeatureCorrelationLabel"
        FeatureCorrelationLabel.Size = New Size(200, 30)
        FeatureCorrelationLabel.TabIndex = 15
        FeatureCorrelationLabel.Text = "Feature Correlation"
        ' 
        ' MatchCorrSlider
        ' 
        MatchCorrSlider.Location = New Point(31, 406)
        MatchCorrSlider.Margin = New Padding(4)
        MatchCorrSlider.Maximum = 100
        MatchCorrSlider.Name = "MatchCorrSlider"
        MatchCorrSlider.Size = New Size(628, 69)
        MatchCorrSlider.TabIndex = 14
        MatchCorrSlider.Value = 90
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.Location = New Point(31, 372)
        Label9.Margin = New Padding(4, 0, 4, 0)
        Label9.Name = "Label9"
        Label9.Size = New Size(289, 30)
        Label9.TabIndex = 13
        Label9.Text = "Match Correlation Threshold"
        ' 
        ' GrayGroup
        ' 
        GrayGroup.Location = New Point(868, 11)
        GrayGroup.Margin = New Padding(4)
        GrayGroup.Name = "GrayGroup"
        GrayGroup.Padding = New Padding(4)
        GrayGroup.Size = New Size(340, 511)
        GrayGroup.TabIndex = 16
        GrayGroup.TabStop = False
        GrayGroup.Text = "Grayscale source inputs"
        ' 
        ' ColorGroup
        ' 
        ColorGroup.Location = New Point(1235, 17)
        ColorGroup.Margin = New Padding(4)
        ColorGroup.Name = "ColorGroup"
        ColorGroup.Padding = New Padding(4)
        ColorGroup.Size = New Size(347, 498)
        ColorGroup.TabIndex = 17
        ColorGroup.TabStop = False
        ColorGroup.Text = "Color source inputs"
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(364, 538)
        Label4.Margin = New Padding(4, 0, 4, 0)
        Label4.Name = "Label4"
        Label4.Size = New Size(176, 30)
        Label4.TabIndex = 23
        Label4.Text = "RedColor Source"
        ' 
        ' ColorSource
        ' 
        ColorSource.FormattingEnabled = True
        ColorSource.Location = New Point(40, 530)
        ColorSource.Margin = New Padding(4)
        ColorSource.Name = "ColorSource"
        ColorSource.Size = New Size(285, 38)
        ColorSource.TabIndex = 22
        ' 
        ' OptionsFeatures
        ' 
        AutoScaleDimensions = New SizeF(12F, 30F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1704, 690)
        Controls.Add(Label4)
        Controls.Add(ColorSource)
        Controls.Add(ColorGroup)
        Controls.Add(GrayGroup)
        Controls.Add(FeatureCorrelationLabel)
        Controls.Add(MatchCorrSlider)
        Controls.Add(Label9)
        Controls.Add(FeatureSampleSizeLabel)
        Controls.Add(FeatureSampleSize)
        Controls.Add(Label5)
        Controls.Add(Label2)
        Controls.Add(Label1)
        Controls.Add(EdgeMethods)
        Controls.Add(FeatureMethod)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(4)
        Name = "OptionsFeatures"
        Text = "Important Options for Features, Edges, Lines, and Masks"
        CType(FeatureSampleSize, ComponentModel.ISupportInitialize).EndInit()
        CType(MatchCorrSlider, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents FeatureMethod As ComboBox
    Friend WithEvents EdgeMethods As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents FeatureSampleSizeLabel As Label
    Friend WithEvents FeatureSampleSize As TrackBar
    Friend WithEvents Label5 As Label
    Friend WithEvents FeatureCorrelationLabel As Label
    Friend WithEvents MatchCorrSlider As TrackBar
    Friend WithEvents Label9 As Label
    Friend WithEvents GrayGroup As GroupBox
    Friend WithEvents ColorGroup As GroupBox
    Friend WithEvents Label4 As Label
    Friend WithEvents ColorSource As ComboBox
End Class
