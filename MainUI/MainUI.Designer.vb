Namespace MainUI
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Public Class MainUI
        Inherits Form

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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainUI))
            MainToolStrip = New ToolStrip()
            PausePlayButton = New ToolStripButton()
            OptionsButton = New ToolStripButton()
            TestAllButton = New ToolStripButton()
            Magnifier = New ToolStripButton()
            ToolStripSeparator1 = New ToolStripSeparator()
            PixelViewer = New ToolStripButton()
            RecentList = New ToolStripDropDownButton()
            AvailableAlgorithms = New ToolStripComboBox()
            AtoZ = New ToolStripButton()
            ToolStripSeparator2 = New ToolStripSeparator()
            AlgDescription = New ToolStripTextBox()
            labelRGB = New Label()
            labelPointCloud = New Label()
            labelLeft = New Label()
            labelRight = New Label()
            StatusLabel = New Label()
            CameraSwitching = New Label()
            CamSwitchTimer = New Timer(components)
            fpsTimer = New Timer(components)
            TestAllTimer = New Timer(components)
            MagnifyTimer = New Timer(components)
            MainToolStrip.SuspendLayout()
            SuspendLayout()
            ' 
            ' MainToolStrip
            ' 
            MainToolStrip.ImageScalingSize = New Size(24, 24)
            MainToolStrip.Items.AddRange(New ToolStripItem() {PausePlayButton, OptionsButton, TestAllButton, Magnifier, ToolStripSeparator1, PixelViewer, RecentList, AvailableAlgorithms, AtoZ, ToolStripSeparator2, AlgDescription})
            MainToolStrip.Location = New Point(0, 0)
            MainToolStrip.Name = "MainToolStrip"
            MainToolStrip.Padding = New Padding(0, 0, 3, 0)
            MainToolStrip.Size = New Size(1275, 39)
            MainToolStrip.TabIndex = 0
            MainToolStrip.Text = "MainToolStrip"
            ' 
            ' PausePlayButton
            ' 
            PausePlayButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            PausePlayButton.Image = CType(resources.GetObject("PausePlayButton.Image"), Image)
            PausePlayButton.ImageTransparentColor = Color.Magenta
            PausePlayButton.Name = "PausePlayButton"
            PausePlayButton.Size = New Size(34, 34)
            PausePlayButton.Text = "Pause/Play"
            PausePlayButton.ToolTipText = "Play/Pause"
            ' 
            ' OptionsButton
            ' 
            OptionsButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            OptionsButton.Image = CType(resources.GetObject("OptionsButton.Image"), Image)
            OptionsButton.ImageTransparentColor = Color.Magenta
            OptionsButton.Name = "OptionsButton"
            OptionsButton.Size = New Size(34, 34)
            OptionsButton.Text = "Settings"
            OptionsButton.ToolTipText = "Open OpenCVB Settings"
            ' 
            ' TestAllButton
            ' 
            TestAllButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            TestAllButton.Image = CType(resources.GetObject("TestAllButton.Image"), Image)
            TestAllButton.ImageTransparentColor = Color.Magenta
            TestAllButton.Name = "TestAllButton"
            TestAllButton.Size = New Size(34, 34)
            TestAllButton.Text = "Start"
            TestAllButton.ToolTipText = "Test All Algorithms"
            ' 
            ' Magnifier
            ' 
            Magnifier.DisplayStyle = ToolStripItemDisplayStyle.Image
            Magnifier.Image = CType(resources.GetObject("Magnifier.Image"), Image)
            Magnifier.Name = "Magnifier"
            Magnifier.Size = New Size(34, 34)
            Magnifier.ToolTipText = "Magnifier"
            ' 
            ' ToolStripSeparator1
            ' 
            ToolStripSeparator1.Name = "ToolStripSeparator1"
            ToolStripSeparator1.Size = New Size(6, 39)
            ' 
            ' PixelViewer
            ' 
            PixelViewer.DisplayStyle = ToolStripItemDisplayStyle.Image
            PixelViewer.Image = CType(resources.GetObject("PixelViewer.Image"), Image)
            PixelViewer.Name = "PixelViewer"
            PixelViewer.Size = New Size(34, 34)
            PixelViewer.Text = "Pixel Viewer"
            PixelViewer.ToolTipText = "Pixel Viewer"
            ' 
            ' RecentList
            ' 
            RecentList.DisplayStyle = ToolStripItemDisplayStyle.Text
            RecentList.Name = "RecentList"
            RecentList.Size = New Size(96, 34)
            RecentList.Text = "Recent"
            RecentList.ToolTipText = " "
            ' 
            ' AvailableAlgorithms
            ' 
            AvailableAlgorithms.MaxDropDownItems = 50
            AvailableAlgorithms.MaxLength = 100
            AvailableAlgorithms.Name = "AvailableAlgorithms"
            AvailableAlgorithms.Size = New Size(350, 39)
            ' 
            ' AtoZ
            ' 
            AtoZ.DisplayStyle = ToolStripItemDisplayStyle.Text
            AtoZ.Image = CType(resources.GetObject("AtoZ.Image"), Image)
            AtoZ.ImageTransparentColor = Color.Magenta
            AtoZ.Name = "AtoZ"
            AtoZ.Size = New Size(53, 34)
            AtoZ.Text = "A-Z"
            AtoZ.ToolTipText = "Show all the Algorithm Groups"
            ' 
            ' ToolStripSeparator2
            ' 
            ToolStripSeparator2.Name = "ToolStripSeparator2"
            ToolStripSeparator2.Size = New Size(6, 39)
            ' 
            ' AlgDescription
            ' 
            AlgDescription.AutoSize = False
            AlgDescription.MaxLength = 200
            AlgDescription.Name = "AlgDescription"
            AlgDescription.Size = New Size(499, 37)
            AlgDescription.Text = "Description of algorithm"
            AlgDescription.ToolTipText = "Description"
            ' 
            ' labelRGB
            ' 
            labelRGB.AutoSize = True
            labelRGB.Location = New Point(0, 39)
            labelRGB.Name = "labelRGB"
            labelRGB.Size = New Size(54, 30)
            labelRGB.TabIndex = 8
            labelRGB.Text = "RGB"
            ' 
            ' labelPointCloud
            ' 
            labelPointCloud.AutoSize = True
            labelPointCloud.Location = New Point(933, 39)
            labelPointCloud.Name = "labelPointCloud"
            labelPointCloud.Size = New Size(124, 30)
            labelPointCloud.TabIndex = 7
            labelPointCloud.Text = "Point Cloud"
            ' 
            ' labelLeft
            ' 
            labelLeft.AutoSize = True
            labelLeft.Location = New Point(0, 587)
            labelLeft.Name = "labelLeft"
            labelLeft.Size = New Size(49, 30)
            labelLeft.TabIndex = 6
            labelLeft.Text = "Left"
            ' 
            ' labelRight
            ' 
            labelRight.AutoSize = True
            labelRight.Location = New Point(933, 587)
            labelRight.Name = "labelRight"
            labelRight.Size = New Size(63, 30)
            labelRight.TabIndex = 5
            labelRight.Text = "Right"
            ' 
            ' StatusLabel
            ' 
            StatusLabel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            StatusLabel.BackColor = SystemColors.Control
            StatusLabel.Location = New Point(0, 1141)
            StatusLabel.Margin = New Padding(0)
            StatusLabel.Name = "StatusLabel"
            StatusLabel.Size = New Size(1275, 30)
            StatusLabel.TabIndex = 2
            StatusLabel.TextAlign = ContentAlignment.MiddleLeft
            ' 
            ' CameraSwitching
            ' 
            CameraSwitching.AutoSize = True
            CameraSwitching.Font = New Font("Microsoft Sans Serif", 12F)
            CameraSwitching.Location = New Point(40, 118)
            CameraSwitching.Name = "CameraSwitching"
            CameraSwitching.Size = New Size(202, 29)
            CameraSwitching.TabIndex = 9
            CameraSwitching.Text = "CameraSwitching"
            ' 
            ' CamSwitchTimer
            ' 
            CamSwitchTimer.Interval = 1
            ' 
            ' fpsTimer
            ' 
            fpsTimer.Interval = 3000
            ' 
            ' TestAllTimer
            ' 
            TestAllTimer.Interval = 1
            ' 
            ' MagnifyTimer
            ' 
            ' 
            ' MainUI
            ' 
            AutoScaleDimensions = New SizeF(12F, 30F)
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(1275, 1171)
            Controls.Add(CameraSwitching)
            Controls.Add(StatusLabel)
            Controls.Add(labelRight)
            Controls.Add(labelLeft)
            Controls.Add(labelPointCloud)
            Controls.Add(labelRGB)
            Controls.Add(MainToolStrip)
            Icon = CType(resources.GetObject("$this.Icon"), Icon)
            Margin = New Padding(3, 4, 3, 4)
            Name = "MainUI"
            Text = "Main Application"
            MainToolStrip.ResumeLayout(False)
            MainToolStrip.PerformLayout()
            ResumeLayout(False)
            PerformLayout()
        End Sub

        Friend WithEvents MainToolStrip As ToolStrip
        Friend WithEvents PausePlayButton As ToolStripButton
        Friend WithEvents TestAllButton As ToolStripButton
        Friend WithEvents Magnifier As ToolStripButton
        Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
        Friend WithEvents PixelViewer As ToolStripButton
        Friend WithEvents ToolStripSeparator2 As ToolStripSeparator
        Friend WithEvents OptionsButton As ToolStripButton
        Friend WithEvents RecentList As ToolStripDropDownButton
        Friend WithEvents AvailableAlgorithms As ToolStripComboBox
        Friend WithEvents AtoZ As ToolStripButton
        Friend WithEvents AlgDescription As ToolStripTextBox
        Friend WithEvents labelRGB As Label
        Friend WithEvents labelPointCloud As Label
        Friend WithEvents labelLeft As Label
        Friend WithEvents labelRight As Label
        Friend WithEvents StatusLabel As Label
        Friend WithEvents CameraSwitching As Label
        Friend WithEvents CamSwitchTimer As Timer
        Friend WithEvents fpsTimer As Timer
        Friend WithEvents TestAllTimer As Timer
        Friend WithEvents MagnifyTimer As Timer

    End Class
End Namespace
