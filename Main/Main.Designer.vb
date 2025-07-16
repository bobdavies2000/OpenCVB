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
        components = New ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Main))
        ToolStrip1 = New ToolStrip()
        PausePlayButton = New ToolStripButton()
        OptionsButton = New ToolStripButton()
        TestAllButton = New ToolStripButton()
        MagnifyButton = New ToolStripButton()
        PixelViewerButton = New ToolStripButton()
        RecentList = New ToolStripDropDownButton()
        AvailableAlgorithms = New ToolStripComboBox()
        AtoZButton = New ToolStripButton()
        GroupComboBox = New ToolStripComboBox()
        AlgDescription = New ToolStripLabel()
        XYLoc = New Label()
        CameraSwitching = New Label()
        CamSwitchProgress = New PictureBox()
        fpsTimer = New Timer(components)
        TestAllTimer = New Timer(components)
        CamSwitchTimer = New Timer(components)
        ComplexityTimer = New Timer(components)
        RefreshTimer = New Timer(components)
        MagnifyTimer = New Timer(components)
        ToolTip1 = New ToolTip(components)
        ToolStrip1.SuspendLayout()
        CType(CamSwitchProgress, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.ImageScalingSize = New Size(24, 24)
        ToolStrip1.Items.AddRange(New ToolStripItem() {PausePlayButton, OptionsButton, TestAllButton, MagnifyButton, PixelViewerButton, RecentList, AvailableAlgorithms, AtoZButton, GroupComboBox, AlgDescription})
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
        ' OptionsButton
        ' 
        OptionsButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        OptionsButton.Image = CType(resources.GetObject("OptionsButton.Image"), Image)
        OptionsButton.ImageTransparentColor = Color.Magenta
        OptionsButton.Name = "OptionsButton"
        OptionsButton.Size = New Size(34, 29)
        OptionsButton.Text = "ToolStripButton1"
        ' 
        ' TestAllButton
        ' 
        TestAllButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        TestAllButton.Image = CType(resources.GetObject("TestAllButton.Image"), Image)
        TestAllButton.ImageTransparentColor = Color.Magenta
        TestAllButton.Name = "TestAllButton"
        TestAllButton.Size = New Size(34, 29)
        TestAllButton.Text = "ToolStripButton2"
        ' 
        ' MagnifyButton
        ' 
        MagnifyButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        MagnifyButton.Image = CType(resources.GetObject("MagnifyButton.Image"), Image)
        MagnifyButton.ImageTransparentColor = Color.Magenta
        MagnifyButton.Name = "MagnifyButton"
        MagnifyButton.Size = New Size(34, 29)
        MagnifyButton.Text = "ToolStripButton3"
        ' 
        ' PixelViewerButton
        ' 
        PixelViewerButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        PixelViewerButton.Image = CType(resources.GetObject("PixelViewerButton.Image"), Image)
        PixelViewerButton.ImageTransparentColor = Color.Magenta
        PixelViewerButton.Name = "PixelViewerButton"
        PixelViewerButton.Size = New Size(34, 29)
        PixelViewerButton.Text = "ToolStripButton4"
        ' 
        ' RecentList
        ' 
        RecentList.DisplayStyle = ToolStripItemDisplayStyle.Text
        RecentList.Image = CType(resources.GetObject("RecentList.Image"), Image)
        RecentList.ImageTransparentColor = Color.Magenta
        RecentList.Name = "RecentList"
        RecentList.Size = New Size(82, 29)
        RecentList.Text = "Recent"
        ' 
        ' AvailableAlgorithms
        ' 
        AvailableAlgorithms.Name = "AvailableAlgorithms"
        AvailableAlgorithms.Size = New Size(300, 34)
        ' 
        ' AtoZButton
        ' 
        AtoZButton.DisplayStyle = ToolStripItemDisplayStyle.Text
        AtoZButton.Image = CType(resources.GetObject("AtoZButton.Image"), Image)
        AtoZButton.ImageTransparentColor = Color.Magenta
        AtoZButton.Name = "AtoZButton"
        AtoZButton.Size = New Size(45, 29)
        AtoZButton.Text = "A-Z"
        ' 
        ' GroupComboBox
        ' 
        GroupComboBox.Name = "GroupComboBox"
        GroupComboBox.Size = New Size(200, 34)
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
        ' fpsTimer
        ' 
        ' 
        ' CamSwitchTimer
        ' 
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
    Friend WithEvents OptionsButton As ToolStripButton
    Friend WithEvents TestAllButton As ToolStripButton
    Friend WithEvents MagnifyButton As ToolStripButton
    Friend WithEvents PixelViewerButton As ToolStripButton
    Friend WithEvents AvailableAlgorithms As ToolStripComboBox
    Friend WithEvents AtoZButton As ToolStripButton
    Friend WithEvents GroupComboBox As ToolStripComboBox
    Friend WithEvents AlgDescription As ToolStripLabel
    Friend WithEvents XYLoc As Label
    Friend WithEvents CameraSwitching As Label
    Friend WithEvents CamSwitchProgress As PictureBox
    Friend WithEvents fpsTimer As Timer
    Friend WithEvents TestAllTimer As Timer
    Friend WithEvents CamSwitchTimer As Timer
    Friend WithEvents ComplexityTimer As Timer
    Friend WithEvents RefreshTimer As Timer
    Friend WithEvents MagnifyTimer As Timer
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents RecentList As ToolStripDropDownButton

End Class
