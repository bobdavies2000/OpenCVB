<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Main
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Main))
        ToolStrip1 = New ToolStrip()
        PausePlayButton = New ToolStripButton()
        ToolStripButton1 = New ToolStripButton()
        ToolStripButton2 = New ToolStripButton()
        ToolStripButton3 = New ToolStripButton()
        ToolStripButton4 = New ToolStripButton()
        ToolStripButton5 = New ToolStripButton()
        ToolStripComboBox1 = New ToolStripComboBox()
        ToolStripButton6 = New ToolStripButton()
        ToolStripComboBox2 = New ToolStripComboBox()
        AlgDescription = New ToolStripLabel()
        XYLoc = New Label()
        CameraSwitching = New Label()
        CamSwitchProgress = New PictureBox()
        ToolStrip1.SuspendLayout()
        CType(CamSwitchProgress, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.ImageScalingSize = New Size(24, 24)
        ToolStrip1.Items.AddRange(New ToolStripItem() {PausePlayButton, ToolStripButton1, ToolStripButton2, ToolStripButton3, ToolStripButton4, ToolStripButton5, ToolStripComboBox1, ToolStripButton6, ToolStripComboBox2, AlgDescription})
        ToolStrip1.Location = New Point(0, 0)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.Size = New Size(1582, 34)
        ToolStrip1.TabIndex = 0
        ToolStrip1.Text = "ToolStrip1"
        ' 
        ' PausePlayButton
        ' 
        PausePlayButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        PausePlayButton.Image = CType(resources.GetObject("PausePlayButton.Image"), Image)
        PausePlayButton.ImageTransparentColor = Color.Magenta
        PausePlayButton.Name = "PausePlayButton"
        PausePlayButton.Size = New Size(34, 29)
        PausePlayButton.Text = "Run Pause Button"
        ' 
        ' ToolStripButton1
        ' 
        ToolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Image
        ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), Image)
        ToolStripButton1.ImageTransparentColor = Color.Magenta
        ToolStripButton1.Name = "ToolStripButton1"
        ToolStripButton1.Size = New Size(34, 29)
        ToolStripButton1.Text = "ToolStripButton1"
        ' 
        ' ToolStripButton2
        ' 
        ToolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Image
        ToolStripButton2.Image = CType(resources.GetObject("ToolStripButton2.Image"), Image)
        ToolStripButton2.ImageTransparentColor = Color.Magenta
        ToolStripButton2.Name = "ToolStripButton2"
        ToolStripButton2.Size = New Size(34, 29)
        ToolStripButton2.Text = "ToolStripButton2"
        ' 
        ' ToolStripButton3
        ' 
        ToolStripButton3.DisplayStyle = ToolStripItemDisplayStyle.Image
        ToolStripButton3.Image = CType(resources.GetObject("ToolStripButton3.Image"), Image)
        ToolStripButton3.ImageTransparentColor = Color.Magenta
        ToolStripButton3.Name = "ToolStripButton3"
        ToolStripButton3.Size = New Size(34, 29)
        ToolStripButton3.Text = "ToolStripButton3"
        ' 
        ' ToolStripButton4
        ' 
        ToolStripButton4.DisplayStyle = ToolStripItemDisplayStyle.Image
        ToolStripButton4.Image = CType(resources.GetObject("ToolStripButton4.Image"), Image)
        ToolStripButton4.ImageTransparentColor = Color.Magenta
        ToolStripButton4.Name = "ToolStripButton4"
        ToolStripButton4.Size = New Size(34, 29)
        ToolStripButton4.Text = "ToolStripButton4"
        ' 
        ' ToolStripButton5
        ' 
        ToolStripButton5.DisplayStyle = ToolStripItemDisplayStyle.Text
        ToolStripButton5.Image = CType(resources.GetObject("ToolStripButton5.Image"), Image)
        ToolStripButton5.ImageTransparentColor = Color.Magenta
        ToolStripButton5.Name = "ToolStripButton5"
        ToolStripButton5.Size = New Size(68, 29)
        ToolStripButton5.Text = "Recent"
        ' 
        ' ToolStripComboBox1
        ' 
        ToolStripComboBox1.Name = "ToolStripComboBox1"
        ToolStripComboBox1.Size = New Size(300, 34)
        ' 
        ' ToolStripButton6
        ' 
        ToolStripButton6.DisplayStyle = ToolStripItemDisplayStyle.Text
        ToolStripButton6.Image = CType(resources.GetObject("ToolStripButton6.Image"), Image)
        ToolStripButton6.ImageTransparentColor = Color.Magenta
        ToolStripButton6.Name = "ToolStripButton6"
        ToolStripButton6.Size = New Size(45, 29)
        ToolStripButton6.Text = "A-Z"
        ' 
        ' ToolStripComboBox2
        ' 
        ToolStripComboBox2.Name = "ToolStripComboBox2"
        ToolStripComboBox2.Size = New Size(200, 34)
        ' 
        ' AlgDescription
        ' 
        AlgDescription.Name = "AlgDescription"
        AlgDescription.Size = New Size(102, 29)
        AlgDescription.Text = "Description"
        ' 
        ' XYLoc
        ' 
        XYLoc.AutoSize = True
        XYLoc.Location = New Point(5, 718)
        XYLoc.Name = "XYLoc"
        XYLoc.Size = New Size(60, 25)
        XYLoc.TabIndex = 1
        XYLoc.Text = "XYLoc"
        ' 
        ' CameraSwitching
        ' 
        CameraSwitching.AutoSize = True
        CameraSwitching.Font = New Font("Microsoft Sans Serif", 12F)
        CameraSwitching.Location = New Point(33, 66)
        CameraSwitching.Name = "CameraSwitching"
        CameraSwitching.Size = New Size(202, 29)
        CameraSwitching.TabIndex = 2
        CameraSwitching.Text = "CameraSwitching"
        ' 
        ' CamSwitchProgress
        ' 
        CamSwitchProgress.BackColor = SystemColors.MenuHighlight
        CamSwitchProgress.Location = New Point(33, 98)
        CamSwitchProgress.Name = "CamSwitchProgress"
        CamSwitchProgress.Size = New Size(202, 27)
        CamSwitchProgress.TabIndex = 3
        CamSwitchProgress.TabStop = False
        ' 
        ' Main
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1582, 748)
        Controls.Add(CamSwitchProgress)
        Controls.Add(CameraSwitching)
        Controls.Add(XYLoc)
        Controls.Add(ToolStrip1)
        Name = "Main"
        Text = "OpenCVB Main Form"
        ToolStrip1.ResumeLayout(False)
        ToolStrip1.PerformLayout()
        CType(CamSwitchProgress, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents PausePlayButton As ToolStripButton
    Friend WithEvents ToolStripButton1 As ToolStripButton
    Friend WithEvents ToolStripButton2 As ToolStripButton
    Friend WithEvents ToolStripButton3 As ToolStripButton
    Friend WithEvents ToolStripButton4 As ToolStripButton
    Friend WithEvents ToolStripButton5 As ToolStripButton
    Friend WithEvents ToolStripComboBox1 As ToolStripComboBox
    Friend WithEvents ToolStripButton6 As ToolStripButton
    Friend WithEvents ToolStripComboBox2 As ToolStripComboBox
    Friend WithEvents AlgDescription As ToolStripLabel
    Friend WithEvents XYLoc As Label
    Friend WithEvents CameraSwitching As Label
    Friend WithEvents CamSwitchProgress As PictureBox

End Class
