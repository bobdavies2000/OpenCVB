<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsGlobal
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
        Me.MinMaxDepth = New System.Windows.Forms.GroupBox()
        Me.maxCount = New System.Windows.Forms.Label()
        Me.MaxRange = New System.Windows.Forms.TrackBar()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.minCount = New System.Windows.Forms.Label()
        Me.MinRange = New System.Windows.Forms.TrackBar()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.TrackBar1 = New System.Windows.Forms.TrackBar()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.threshold = New System.Windows.Forms.Label()
        Me.thresholdSlider = New System.Windows.Forms.TrackBar()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.MinMaxDepth.SuspendLayout()
        CType(Me.MaxRange, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.MinRange, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox1.SuspendLayout()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.thresholdSlider, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'MinMaxDepth
        '
        Me.MinMaxDepth.Controls.Add(Me.maxCount)
        Me.MinMaxDepth.Controls.Add(Me.MaxRange)
        Me.MinMaxDepth.Controls.Add(Me.Label4)
        Me.MinMaxDepth.Controls.Add(Me.minCount)
        Me.MinMaxDepth.Controls.Add(Me.MinRange)
        Me.MinMaxDepth.Controls.Add(Me.Label1)
        Me.MinMaxDepth.Location = New System.Drawing.Point(12, 13)
        Me.MinMaxDepth.Name = "MinMaxDepth"
        Me.MinMaxDepth.Size = New System.Drawing.Size(829, 195)
        Me.MinMaxDepth.TabIndex = 0
        Me.MinMaxDepth.TabStop = False
        Me.MinMaxDepth.Text = "Global Depth Min/Max"
        '
        'maxCount
        '
        Me.maxCount.AutoSize = True
        Me.maxCount.Location = New System.Drawing.Point(736, 112)
        Me.maxCount.Name = "maxCount"
        Me.maxCount.Size = New System.Drawing.Size(81, 20)
        Me.maxCount.TabIndex = 5
        Me.maxCount.Text = "maxCount"
        '
        'MaxRange
        '
        Me.MaxRange.Location = New System.Drawing.Point(212, 105)
        Me.MaxRange.Maximum = 15000
        Me.MaxRange.Minimum = 200
        Me.MaxRange.Name = "MaxRange"
        Me.MaxRange.Size = New System.Drawing.Size(505, 69)
        Me.MaxRange.TabIndex = 4
        Me.MaxRange.TickStyle = System.Windows.Forms.TickStyle.None
        Me.MaxRange.Value = 200
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(18, 112)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(188, 20)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "InRange Min Depth (mm)"
        '
        'minCount
        '
        Me.minCount.AutoSize = True
        Me.minCount.Location = New System.Drawing.Point(737, 36)
        Me.minCount.Name = "minCount"
        Me.minCount.Size = New System.Drawing.Size(77, 20)
        Me.minCount.TabIndex = 2
        Me.minCount.Text = "minCount"
        '
        'MinRange
        '
        Me.MinRange.Location = New System.Drawing.Point(213, 29)
        Me.MinRange.Maximum = 2000
        Me.MinRange.Minimum = 1
        Me.MinRange.Name = "MinRange"
        Me.MinRange.Size = New System.Drawing.Size(505, 69)
        Me.MinRange.TabIndex = 1
        Me.MinRange.TickStyle = System.Windows.Forms.TickStyle.None
        Me.MinRange.Value = 1
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(19, 40)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(188, 20)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "InRange Min Depth (mm)"
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.Label2)
        Me.GroupBox1.Controls.Add(Me.TrackBar1)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Controls.Add(Me.threshold)
        Me.GroupBox1.Controls.Add(Me.thresholdSlider)
        Me.GroupBox1.Controls.Add(Me.Label6)
        Me.GroupBox1.Location = New System.Drawing.Point(12, 214)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(829, 195)
        Me.GroupBox1.TabIndex = 1
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Histogram Projection Options"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(737, 114)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(57, 20)
        Me.Label2.TabIndex = 5
        Me.Label2.Text = "Label2"
        '
        'TrackBar1
        '
        Me.TrackBar1.Location = New System.Drawing.Point(213, 107)
        Me.TrackBar1.Maximum = 15000
        Me.TrackBar1.Minimum = 200
        Me.TrackBar1.Name = "TrackBar1"
        Me.TrackBar1.Size = New System.Drawing.Size(505, 69)
        Me.TrackBar1.TabIndex = 4
        Me.TrackBar1.TickStyle = System.Windows.Forms.TickStyle.None
        Me.TrackBar1.Value = 200
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(19, 114)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(188, 20)
        Me.Label3.TabIndex = 3
        Me.Label3.Text = "InRange Min Depth (mm)"
        '
        'threshold
        '
        Me.threshold.AutoSize = True
        Me.threshold.Location = New System.Drawing.Point(737, 36)
        Me.threshold.Name = "threshold"
        Me.threshold.Size = New System.Drawing.Size(75, 20)
        Me.threshold.TabIndex = 2
        Me.threshold.Text = "threshold"
        '
        'thresholdSlider
        '
        Me.thresholdSlider.Location = New System.Drawing.Point(213, 29)
        Me.thresholdSlider.Maximum = 2000
        Me.thresholdSlider.Minimum = 1
        Me.thresholdSlider.Name = "thresholdSlider"
        Me.thresholdSlider.Size = New System.Drawing.Size(505, 69)
        Me.thresholdSlider.TabIndex = 1
        Me.thresholdSlider.TickStyle = System.Windows.Forms.TickStyle.None
        Me.thresholdSlider.Value = 1
        '
        'Label6
        '
        Me.Label6.Location = New System.Drawing.Point(42, 29)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(173, 47)
        Me.Label6.TabIndex = 0
        Me.Label6.Text = "Top and Side Views Histogram threshold"
        '
        'OptionsGlobal
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(853, 822)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.MinMaxDepth)
        Me.Name = "OptionsGlobal"
        Me.Text = "Options Available to all Algorithms"
        Me.MinMaxDepth.ResumeLayout(False)
        Me.MinMaxDepth.PerformLayout()
        CType(Me.MaxRange, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.MinRange, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.thresholdSlider, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents MinMaxDepth As Windows.Forms.GroupBox
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents MinRange As Windows.Forms.TrackBar
    Friend WithEvents maxCount As Windows.Forms.Label
    Friend WithEvents MaxRange As Windows.Forms.TrackBar
    Friend WithEvents Label4 As Windows.Forms.Label
    Friend WithEvents minCount As Windows.Forms.Label
    Friend WithEvents OpenFileDialog1 As Windows.Forms.OpenFileDialog
    Friend WithEvents GroupBox1 As Windows.Forms.GroupBox
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents TrackBar1 As Windows.Forms.TrackBar
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents threshold As Windows.Forms.Label
    Friend WithEvents thresholdSlider As Windows.Forms.TrackBar
    Friend WithEvents Label6 As Windows.Forms.Label
End Class
