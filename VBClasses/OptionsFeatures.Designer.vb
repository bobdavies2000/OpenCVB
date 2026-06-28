<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsFeatures
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OptionsFeatures))
        FeatureMethod = New ComboBox()
        EdgeMethods = New ComboBox()
        Label1 = New Label()
        Label2 = New Label()
        FeatureSampleSizeLabel = New Label()
        FrameHistoryCount = New TrackBar()
        Label5 = New Label()
        FeatureCorrelationLabel = New Label()
        MatchCorrSlider = New TrackBar()
        Label9 = New Label()
        GrayGroup = New GroupBox()
        ColorGroup = New GroupBox()
        Label4 = New Label()
        Color8USource = New ComboBox()
        ColorDiffLabel = New Label()
        ColorDiffSlider = New TrackBar()
        Label6 = New Label()
        MotionThreshold = New Label()
        MotionPixelLabel1 = New Label()
        MotionPixelSlider = New TrackBar()
        ReductionColor = New TrackBar()
        ReductionDepth = New TrackBar()
        Label8 = New Label()
        Lab1 = New Label()
        Label10 = New Label()
        LineCombo = New ComboBox()
        Lab9 = New Label()
        Label7 = New Label()
        CType(FrameHistoryCount, ComponentModel.ISupportInitialize).BeginInit()
        CType(MatchCorrSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(ColorDiffSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(MotionPixelSlider, ComponentModel.ISupportInitialize).BeginInit()
        CType(ReductionColor, ComponentModel.ISupportInitialize).BeginInit()
        CType(ReductionDepth, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' FeatureMethod
        ' 
        FeatureMethod.FormattingEnabled = True
        FeatureMethod.Location = New Point(208, 3)
        FeatureMethod.Margin = New Padding(4)
        FeatureMethod.Name = "FeatureMethod"
        FeatureMethod.Size = New Size(294, 38)
        FeatureMethod.TabIndex = 0
        ' 
        ' EdgeMethods
        ' 
        EdgeMethods.FormattingEnabled = True
        EdgeMethods.Location = New Point(208, 48)
        EdgeMethods.Margin = New Padding(4)
        EdgeMethods.Name = "EdgeMethods"
        EdgeMethods.Size = New Size(294, 38)
        EdgeMethods.TabIndex = 1
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(55, 53)
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
        FeatureSampleSizeLabel.Location = New Point(793, 330)
        FeatureSampleSizeLabel.Margin = New Padding(4, 0, 4, 0)
        FeatureSampleSizeLabel.Name = "FeatureSampleSizeLabel"
        FeatureSampleSizeLabel.Size = New Size(54, 30)
        FeatureSampleSizeLabel.TabIndex = 9
        FeatureSampleSizeLabel.Text = "lab2"
        ' 
        ' FrameHistoryCount
        ' 
        FrameHistoryCount.Location = New Point(235, 330)
        FrameHistoryCount.Margin = New Padding(4)
        FrameHistoryCount.Maximum = 30
        FrameHistoryCount.Minimum = 1
        FrameHistoryCount.Name = "FrameHistoryCount"
        FrameHistoryCount.Size = New Size(550, 69)
        FrameHistoryCount.TabIndex = 8
        FrameHistoryCount.Value = 3
        ' 
        ' Label5
        ' 
        Label5.Location = New Point(78, 330)
        Label5.Margin = New Padding(4, 0, 4, 0)
        Label5.Name = "Label5"
        Label5.Size = New Size(157, 30)
        Label5.TabIndex = 7
        Label5.Text = "Frame History"
        ' 
        ' FeatureCorrelationLabel
        ' 
        FeatureCorrelationLabel.AutoSize = True
        FeatureCorrelationLabel.Location = New Point(793, 574)
        FeatureCorrelationLabel.Margin = New Padding(4, 0, 4, 0)
        FeatureCorrelationLabel.Name = "FeatureCorrelationLabel"
        FeatureCorrelationLabel.Size = New Size(54, 30)
        FeatureCorrelationLabel.TabIndex = 15
        FeatureCorrelationLabel.Text = "lab5"
        ' 
        ' MatchCorrSlider
        ' 
        MatchCorrSlider.Location = New Point(235, 574)
        MatchCorrSlider.Margin = New Padding(4)
        MatchCorrSlider.Maximum = 100
        MatchCorrSlider.Name = "MatchCorrSlider"
        MatchCorrSlider.Size = New Size(550, 69)
        MatchCorrSlider.TabIndex = 14
        MatchCorrSlider.Value = 90
        ' 
        ' Label9
        ' 
        Label9.Location = New Point(34, 574)
        Label9.Margin = New Padding(4, 0, 4, 0)
        Label9.Name = "Label9"
        Label9.Size = New Size(193, 64)
        Label9.TabIndex = 13
        Label9.Text = "Match Correlation Threshold"
        ' 
        ' GrayGroup
        ' 
        GrayGroup.Location = New Point(845, 13)
        GrayGroup.Margin = New Padding(4)
        GrayGroup.Name = "GrayGroup"
        GrayGroup.Padding = New Padding(4)
        GrayGroup.Size = New Size(347, 294)
        GrayGroup.TabIndex = 16
        GrayGroup.TabStop = False
        GrayGroup.Text = "Grayscale source inputs"
        ' 
        ' ColorGroup
        ' 
        ColorGroup.Location = New Point(845, 315)
        ColorGroup.Margin = New Padding(4)
        ColorGroup.Name = "ColorGroup"
        ColorGroup.Padding = New Padding(4)
        ColorGroup.Size = New Size(347, 163)
        ColorGroup.TabIndex = 17
        ColorGroup.TabStop = False
        ColorGroup.Text = "Color source inputs"
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(34, 94)
        Label4.Margin = New Padding(4, 0, 4, 0)
        Label4.Name = "Label4"
        Label4.Size = New Size(166, 30)
        Label4.TabIndex = 23
        Label4.Text = "Color8U Source"
        ' 
        ' Color8USource
        ' 
        Color8USource.FormattingEnabled = True
        Color8USource.Location = New Point(208, 94)
        Color8USource.Margin = New Padding(4)
        Color8USource.Name = "Color8USource"
        Color8USource.Size = New Size(294, 38)
        Color8USource.TabIndex = 22
        ' 
        ' ColorDiffLabel
        ' 
        ColorDiffLabel.AutoSize = True
        ColorDiffLabel.Location = New Point(793, 408)
        ColorDiffLabel.Margin = New Padding(4, 0, 4, 0)
        ColorDiffLabel.Name = "ColorDiffLabel"
        ColorDiffLabel.Size = New Size(54, 30)
        ColorDiffLabel.TabIndex = 26
        ColorDiffLabel.Text = "lab3"
        ' 
        ' ColorDiffSlider
        ' 
        ColorDiffSlider.Location = New Point(235, 408)
        ColorDiffSlider.Margin = New Padding(4)
        ColorDiffSlider.Maximum = 50
        ColorDiffSlider.Name = "ColorDiffSlider"
        ColorDiffSlider.Size = New Size(550, 69)
        ColorDiffSlider.TabIndex = 25
        ColorDiffSlider.Value = 5
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(18, 408)
        Label6.Margin = New Padding(4, 0, 4, 0)
        Label6.Name = "Label6"
        Label6.Size = New Size(208, 30)
        Label6.TabIndex = 24
        Label6.Text = "Color Diff Threshold"
        ' 
        ' MotionThreshold
        ' 
        MotionThreshold.Location = New Point(84, 474)
        MotionThreshold.Margin = New Padding(4, 0, 4, 0)
        MotionThreshold.Name = "MotionThreshold"
        MotionThreshold.Size = New Size(143, 64)
        MotionThreshold.TabIndex = 27
        MotionThreshold.Text = "Motion pixel threshold"
        ' 
        ' MotionPixelLabel1
        ' 
        MotionPixelLabel1.AutoSize = True
        MotionPixelLabel1.Location = New Point(793, 485)
        MotionPixelLabel1.Margin = New Padding(4, 0, 4, 0)
        MotionPixelLabel1.Name = "MotionPixelLabel1"
        MotionPixelLabel1.Size = New Size(54, 30)
        MotionPixelLabel1.TabIndex = 29
        MotionPixelLabel1.Text = "lab4"
        ' 
        ' MotionPixelSlider
        ' 
        MotionPixelSlider.Location = New Point(235, 485)
        MotionPixelSlider.Margin = New Padding(4)
        MotionPixelSlider.Maximum = 100
        MotionPixelSlider.Name = "MotionPixelSlider"
        MotionPixelSlider.Size = New Size(550, 69)
        MotionPixelSlider.TabIndex = 28
        MotionPixelSlider.Value = 5
        ' 
        ' ReductionColor
        ' 
        ReductionColor.Location = New Point(235, 194)
        ReductionColor.Margin = New Padding(4)
        ReductionColor.Maximum = 255
        ReductionColor.Minimum = 1
        ReductionColor.Name = "ReductionColor"
        ReductionColor.Size = New Size(550, 69)
        ReductionColor.TabIndex = 1000
        ReductionColor.Value = 31
        ' 
        ' ReductionDepth
        ' 
        ReductionDepth.Location = New Point(235, 261)
        ReductionDepth.Margin = New Padding(4)
        ReductionDepth.Maximum = 1000
        ReductionDepth.Minimum = 1
        ReductionDepth.Name = "ReductionDepth"
        ReductionDepth.Size = New Size(550, 69)
        ReductionDepth.TabIndex = 1005
        ReductionDepth.Value = 100
        ' 
        ' Label8
        ' 
        Label8.Location = New Point(55, 203)
        Label8.Margin = New Padding(4, 0, 4, 0)
        Label8.Name = "Label8"
        Label8.Size = New Size(180, 30)
        Label8.TabIndex = 30
        Label8.Text = "Color Reduction"
        ' 
        ' Lab1
        ' 
        Lab1.AutoSize = True
        Lab1.Location = New Point(793, 193)
        Lab1.Margin = New Padding(4, 0, 4, 0)
        Lab1.Name = "Lab1"
        Lab1.Size = New Size(54, 30)
        Lab1.TabIndex = 1001
        Lab1.Text = "lab1"
        ' 
        ' Label10
        ' 
        Label10.AutoSize = True
        Label10.Location = New Point(34, 140)
        Label10.Margin = New Padding(4, 0, 4, 0)
        Label10.Name = "Label10"
        Label10.Size = New Size(154, 30)
        Label10.TabIndex = 1003
        Label10.Text = "Line Algorithm"
        ' 
        ' LineCombo
        ' 
        LineCombo.FormattingEnabled = True
        LineCombo.Location = New Point(208, 141)
        LineCombo.Margin = New Padding(4)
        LineCombo.Name = "LineCombo"
        LineCombo.Size = New Size(294, 38)
        LineCombo.TabIndex = 1002
        ' 
        ' Lab9
        ' 
        Lab9.AutoSize = True
        Lab9.Location = New Point(793, 260)
        Lab9.Margin = New Padding(4, 0, 4, 0)
        Lab9.Name = "Lab9"
        Lab9.Size = New Size(54, 30)
        Lab9.TabIndex = 1006
        Lab9.Text = "lab1"
        ' 
        ' Label7
        ' 
        Label7.Location = New Point(34, 275)
        Label7.Margin = New Padding(4, 0, 4, 0)
        Label7.Name = "Label7"
        Label7.Size = New Size(192, 30)
        Label7.TabIndex = 1004
        Label7.Text = "Depth Reduction"
        ' 
        ' OptionsFeatures
        ' 
        AutoScaleDimensions = New SizeF(12.0F, 30.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1205, 665)
        Controls.Add(Lab9)
        Controls.Add(Label7)
        Controls.Add(Label10)
        Controls.Add(LineCombo)
        Controls.Add(Lab1)
        Controls.Add(ReductionColor)
        Controls.Add(ReductionDepth)
        Controls.Add(Label8)
        Controls.Add(MotionPixelLabel1)
        Controls.Add(MotionPixelSlider)
        Controls.Add(MotionThreshold)
        Controls.Add(ColorDiffLabel)
        Controls.Add(ColorDiffSlider)
        Controls.Add(Label6)
        Controls.Add(Label4)
        Controls.Add(Color8USource)
        Controls.Add(ColorGroup)
        Controls.Add(GrayGroup)
        Controls.Add(FeatureCorrelationLabel)
        Controls.Add(MatchCorrSlider)
        Controls.Add(Label9)
        Controls.Add(FeatureSampleSizeLabel)
        Controls.Add(FrameHistoryCount)
        Controls.Add(Label5)
        Controls.Add(Label2)
        Controls.Add(Label1)
        Controls.Add(EdgeMethods)
        Controls.Add(FeatureMethod)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(4)
        Name = "OptionsFeatures"
        Text = "Important Options for Color, Features, Edges, Lines, and Masks"
        CType(FrameHistoryCount, ComponentModel.ISupportInitialize).EndInit()
        CType(MatchCorrSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(ColorDiffSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(MotionPixelSlider, ComponentModel.ISupportInitialize).EndInit()
        CType(ReductionColor, ComponentModel.ISupportInitialize).EndInit()
        CType(ReductionDepth, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents FeatureMethod As ComboBox
    Friend WithEvents EdgeMethods As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents FeatureSampleSizeLabel As Label
    Friend WithEvents FrameHistoryCount As TrackBar
    Friend WithEvents Label5 As Label
    Friend WithEvents FeatureCorrelationLabel As Label
    Friend WithEvents MatchCorrSlider As TrackBar
    Friend WithEvents Label9 As Label
    Friend WithEvents GrayGroup As GroupBox
    Friend WithEvents ColorGroup As GroupBox
    Friend WithEvents Label4 As Label
    Friend WithEvents Color8USource As ComboBox
    Friend WithEvents ColorDiffLabel As Label
    Friend WithEvents ColorDiffSlider As TrackBar
    Friend WithEvents Label6 As Label
    Friend WithEvents Lab9 As Label
    Friend WithEvents TrackBar1 As TrackBar
    Friend WithEvents MotionThreshold As Label
    Friend WithEvents MotionPixelLabel1 As Label
    Friend WithEvents MotionPixelSlider As TrackBar
    Friend WithEvents Label7 As Label
    Friend WithEvents ReductionColor As TrackBar
    Friend WithEvents ReductionDepth As TrackBar
    Friend WithEvents Label8 As Label
    Friend WithEvents Lab1 As Label
    Friend WithEvents Label10 As Label
    Friend WithEvents LineCombo As ComboBox
End Class
