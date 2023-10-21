<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsRedCloud
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
        Me.RedCloudSliders = New System.Windows.Forms.GroupBox()
        Me.TopLabel = New System.Windows.Forms.Label()
        Me.TopViewThreshold = New System.Windows.Forms.TrackBar()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.SideLabel = New System.Windows.Forms.Label()
        Me.SideViewThreshold = New System.Windows.Forms.TrackBar()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.YLabel = New System.Windows.Forms.Label()
        Me.YRangeSlider = New System.Windows.Forms.TrackBar()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.XLabel = New System.Windows.Forms.Label()
        Me.XRangeSlider = New System.Windows.Forms.TrackBar()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.histBins = New System.Windows.Forms.Label()
        Me.HistBinSlider = New System.Windows.Forms.TrackBar()
        Me.RedCloudHistBins = New System.Windows.Forms.Label()
        Me.RGBSource = New System.Windows.Forms.GroupBox()
        Me.RadioButton4 = New System.Windows.Forms.RadioButton()
        Me.RadioButton3 = New System.Windows.Forms.RadioButton()
        Me.RadioButton2 = New System.Windows.Forms.RadioButton()
        Me.RadioButton1 = New System.Windows.Forms.RadioButton()
        Me.RedCloudSliders.SuspendLayout()
        CType(Me.TopViewThreshold, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.SideViewThreshold, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.YRangeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.XRangeSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.RGBSource.SuspendLayout()
        Me.SuspendLayout()
        '
        'RedCloudSliders
        '
        Me.RedCloudSliders.Controls.Add(Me.TopLabel)
        Me.RedCloudSliders.Controls.Add(Me.TopViewThreshold)
        Me.RedCloudSliders.Controls.Add(Me.Label6)
        Me.RedCloudSliders.Controls.Add(Me.SideLabel)
        Me.RedCloudSliders.Controls.Add(Me.SideViewThreshold)
        Me.RedCloudSliders.Controls.Add(Me.Label8)
        Me.RedCloudSliders.Controls.Add(Me.YLabel)
        Me.RedCloudSliders.Controls.Add(Me.YRangeSlider)
        Me.RedCloudSliders.Controls.Add(Me.Label4)
        Me.RedCloudSliders.Controls.Add(Me.XLabel)
        Me.RedCloudSliders.Controls.Add(Me.XRangeSlider)
        Me.RedCloudSliders.Controls.Add(Me.Label2)
        Me.RedCloudSliders.Controls.Add(Me.histBins)
        Me.RedCloudSliders.Controls.Add(Me.HistBinSlider)
        Me.RedCloudSliders.Controls.Add(Me.RedCloudHistBins)
        Me.RedCloudSliders.Location = New System.Drawing.Point(12, 12)
        Me.RedCloudSliders.Name = "RedCloudSliders"
        Me.RedCloudSliders.Size = New System.Drawing.Size(831, 493)
        Me.RedCloudSliders.TabIndex = 2
        Me.RedCloudSliders.TabStop = False
        '
        'TopLabel
        '
        Me.TopLabel.AutoSize = True
        Me.TopLabel.Location = New System.Drawing.Point(667, 305)
        Me.TopLabel.Name = "TopLabel"
        Me.TopLabel.Size = New System.Drawing.Size(57, 20)
        Me.TopLabel.TabIndex = 20
        Me.TopLabel.Text = "Label5"
        '
        'TopViewThreshold
        '
        Me.TopViewThreshold.Location = New System.Drawing.Point(156, 299)
        Me.TopViewThreshold.Maximum = 200
        Me.TopViewThreshold.Minimum = 3
        Me.TopViewThreshold.Name = "TopViewThreshold"
        Me.TopViewThreshold.Size = New System.Drawing.Size(506, 69)
        Me.TopViewThreshold.TabIndex = 19
        Me.TopViewThreshold.TickStyle = System.Windows.Forms.TickStyle.None
        Me.TopViewThreshold.Value = 5
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(8, 305)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(182, 20)
        Me.Label6.TabIndex = 18
        Me.Label6.Text = "Top View Red Threshold"
        '
        'SideLabel
        '
        Me.SideLabel.AutoSize = True
        Me.SideLabel.Location = New System.Drawing.Point(667, 230)
        Me.SideLabel.Name = "SideLabel"
        Me.SideLabel.Size = New System.Drawing.Size(57, 20)
        Me.SideLabel.TabIndex = 17
        Me.SideLabel.Text = "Label7"
        '
        'SideViewThreshold
        '
        Me.SideViewThreshold.Location = New System.Drawing.Point(156, 224)
        Me.SideViewThreshold.Maximum = 200
        Me.SideViewThreshold.Minimum = 3
        Me.SideViewThreshold.Name = "SideViewThreshold"
        Me.SideViewThreshold.Size = New System.Drawing.Size(506, 69)
        Me.SideViewThreshold.TabIndex = 16
        Me.SideViewThreshold.TickStyle = System.Windows.Forms.TickStyle.None
        Me.SideViewThreshold.Value = 5
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(8, 230)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(187, 20)
        Me.Label8.TabIndex = 15
        Me.Label8.Text = "Side View Red Threshold"
        '
        'YLabel
        '
        Me.YLabel.AutoSize = True
        Me.YLabel.Location = New System.Drawing.Point(667, 166)
        Me.YLabel.Name = "YLabel"
        Me.YLabel.Size = New System.Drawing.Size(57, 20)
        Me.YLabel.TabIndex = 14
        Me.YLabel.Text = "Label3"
        '
        'YRangeSlider
        '
        Me.YRangeSlider.Location = New System.Drawing.Point(156, 160)
        Me.YRangeSlider.Maximum = 1000
        Me.YRangeSlider.Minimum = 3
        Me.YRangeSlider.Name = "YRangeSlider"
        Me.YRangeSlider.Size = New System.Drawing.Size(506, 69)
        Me.YRangeSlider.TabIndex = 13
        Me.YRangeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.YRangeSlider.Value = 5
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(8, 166)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(115, 20)
        Me.Label4.TabIndex = 12
        Me.Label4.Text = "Y-Range X100"
        '
        'XLabel
        '
        Me.XLabel.AutoSize = True
        Me.XLabel.Location = New System.Drawing.Point(667, 91)
        Me.XLabel.Name = "XLabel"
        Me.XLabel.Size = New System.Drawing.Size(57, 20)
        Me.XLabel.TabIndex = 11
        Me.XLabel.Text = "Label1"
        '
        'XRangeSlider
        '
        Me.XRangeSlider.Location = New System.Drawing.Point(156, 85)
        Me.XRangeSlider.Maximum = 1000
        Me.XRangeSlider.Minimum = 3
        Me.XRangeSlider.Name = "XRangeSlider"
        Me.XRangeSlider.Size = New System.Drawing.Size(506, 69)
        Me.XRangeSlider.TabIndex = 10
        Me.XRangeSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.XRangeSlider.Value = 5
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(8, 91)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(115, 20)
        Me.Label2.TabIndex = 9
        Me.Label2.Text = "X-Range X100"
        '
        'histBins
        '
        Me.histBins.AutoSize = True
        Me.histBins.Location = New System.Drawing.Point(667, 24)
        Me.histBins.Name = "histBins"
        Me.histBins.Size = New System.Drawing.Size(65, 20)
        Me.histBins.TabIndex = 8
        Me.histBins.Text = "histBins"
        '
        'HistBinSlider
        '
        Me.HistBinSlider.Location = New System.Drawing.Point(156, 18)
        Me.HistBinSlider.Maximum = 200
        Me.HistBinSlider.Minimum = 3
        Me.HistBinSlider.Name = "HistBinSlider"
        Me.HistBinSlider.Size = New System.Drawing.Size(506, 69)
        Me.HistBinSlider.TabIndex = 7
        Me.HistBinSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.HistBinSlider.Value = 5
        '
        'RedCloudHistBins
        '
        Me.RedCloudHistBins.AutoSize = True
        Me.RedCloudHistBins.Location = New System.Drawing.Point(8, 24)
        Me.RedCloudHistBins.Name = "RedCloudHistBins"
        Me.RedCloudHistBins.Size = New System.Drawing.Size(147, 20)
        Me.RedCloudHistBins.TabIndex = 6
        Me.RedCloudHistBins.Text = "RedCloud Hist Bins"
        '
        'RGBSource
        '
        Me.RGBSource.Controls.Add(Me.RadioButton4)
        Me.RGBSource.Controls.Add(Me.RadioButton3)
        Me.RGBSource.Controls.Add(Me.RadioButton2)
        Me.RGBSource.Controls.Add(Me.RadioButton1)
        Me.RGBSource.Location = New System.Drawing.Point(849, 30)
        Me.RGBSource.Name = "RGBSource"
        Me.RGBSource.Size = New System.Drawing.Size(250, 175)
        Me.RGBSource.TabIndex = 3
        Me.RGBSource.TabStop = False
        Me.RGBSource.Text = "RGB Color Source"
        '
        'RadioButton4
        '
        Me.RadioButton4.AutoSize = True
        Me.RadioButton4.Location = New System.Drawing.Point(28, 67)
        Me.RadioButton4.Name = "RadioButton4"
        Me.RadioButton4.Size = New System.Drawing.Size(148, 24)
        Me.RadioButton4.TabIndex = 3
        Me.RadioButton4.TabStop = True
        Me.RadioButton4.Text = "KMeans_Basics"
        Me.RadioButton4.UseVisualStyleBackColor = True
        '
        'RadioButton3
        '
        Me.RadioButton3.AutoSize = True
        Me.RadioButton3.Location = New System.Drawing.Point(28, 97)
        Me.RadioButton3.Name = "RadioButton3"
        Me.RadioButton3.Size = New System.Drawing.Size(120, 24)
        Me.RadioButton3.TabIndex = 2
        Me.RadioButton3.TabStop = True
        Me.RadioButton3.Text = "LUT_Basics"
        Me.RadioButton3.UseVisualStyleBackColor = True
        '
        'RadioButton2
        '
        Me.RadioButton2.AutoSize = True
        Me.RadioButton2.Location = New System.Drawing.Point(28, 127)
        Me.RadioButton2.Name = "RadioButton2"
        Me.RadioButton2.Size = New System.Drawing.Size(163, 24)
        Me.RadioButton2.TabIndex = 1
        Me.RadioButton2.TabStop = True
        Me.RadioButton2.Text = "Reduction_Basics"
        Me.RadioButton2.UseVisualStyleBackColor = True
        '
        'RadioButton1
        '
        Me.RadioButton1.AutoSize = True
        Me.RadioButton1.Location = New System.Drawing.Point(28, 37)
        Me.RadioButton1.Name = "RadioButton1"
        Me.RadioButton1.Size = New System.Drawing.Size(153, 24)
        Me.RadioButton1.TabIndex = 0
        Me.RadioButton1.TabStop = True
        Me.RadioButton1.Text = "BackProject_Full"
        Me.RadioButton1.UseVisualStyleBackColor = True
        '
        'OptionsRedCloud
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1670, 556)
        Me.Controls.Add(Me.RGBSource)
        Me.Controls.Add(Me.RedCloudSliders)
        Me.Name = "OptionsRedCloud"
        Me.Text = "OptionsRedCloud"
        Me.RedCloudSliders.ResumeLayout(False)
        Me.RedCloudSliders.PerformLayout()
        CType(Me.TopViewThreshold, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.SideViewThreshold, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.YRangeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.XRangeSlider, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.HistBinSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.RGBSource.ResumeLayout(False)
        Me.RGBSource.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents RedCloudSliders As Windows.Forms.GroupBox
    Friend WithEvents histBins As Windows.Forms.Label
    Friend WithEvents HistBinSlider As Windows.Forms.TrackBar
    Friend WithEvents RedCloudHistBins As Windows.Forms.Label
    Friend WithEvents RGBSource As Windows.Forms.GroupBox
    Friend WithEvents RadioButton4 As Windows.Forms.RadioButton
    Friend WithEvents RadioButton3 As Windows.Forms.RadioButton
    Friend WithEvents RadioButton2 As Windows.Forms.RadioButton
    Friend WithEvents RadioButton1 As Windows.Forms.RadioButton
    Friend WithEvents YLabel As Windows.Forms.Label
    Friend WithEvents YRangeSlider As Windows.Forms.TrackBar
    Friend WithEvents Label4 As Windows.Forms.Label
    Friend WithEvents XLabel As Windows.Forms.Label
    Friend WithEvents XRangeSlider As Windows.Forms.TrackBar
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents TopLabel As Windows.Forms.Label
    Friend WithEvents TopViewThreshold As Windows.Forms.TrackBar
    Friend WithEvents Label6 As Windows.Forms.Label
    Friend WithEvents SideLabel As Windows.Forms.Label
    Friend WithEvents SideViewThreshold As Windows.Forms.TrackBar
    Friend WithEvents Label8 As Windows.Forms.Label
End Class
