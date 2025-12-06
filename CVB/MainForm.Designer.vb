Namespace CVB
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Public Class MainForm
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainForm))
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
            campicRGB = New PictureBox()
            campicPointCloud = New PictureBox()
            campicLeft = New PictureBox()
            campicRight = New PictureBox()
            labelRGB = New Label()
            labelPointCloud = New Label()
            labelLeft = New Label()
            labelRight = New Label()
            StatusLabel = New Label()
            StartUpTimer = New Timer(components)
            CameraSwitching = New Label()
            Timer1 = New Timer(components)
            CamSwitchTimer = New Timer(components)
            MainToolStrip.SuspendLayout()
            CType(campicRGB, ComponentModel.ISupportInitialize).BeginInit()
            CType(campicPointCloud, ComponentModel.ISupportInitialize).BeginInit()
            CType(campicLeft, ComponentModel.ISupportInitialize).BeginInit()
            CType(campicRight, ComponentModel.ISupportInitialize).BeginInit()
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
            TestAllButton.Text = "Open"
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
            RecentList.ToolTipText = "List of Recent Algorithms"
            ' 
            ' AvailableAlgorithms
            ' 
            AvailableAlgorithms.MaxDropDownItems = 50
            AvailableAlgorithms.MaxLength = 100
            AvailableAlgorithms.Name = "AvailableAlgorithms"
            AvailableAlgorithms.Size = New Size(350, 39)
            AvailableAlgorithms.Text = "Available Algorithms"
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
            AlgDescription.Text = "Description"
            AlgDescription.ToolTipText = "Description"
            ' 
            ' campicRGB
            ' 
            campicRGB.Anchor = AnchorStyles.None
            campicRGB.BackColor = Color.Black
            campicRGB.Location = New Point(-296, 62)
            campicRGB.Margin = New Padding(0)
            campicRGB.Name = "campicRGB"
            campicRGB.Size = New Size(933, 528)
            campicRGB.SizeMode = PictureBoxSizeMode.StretchImage
            campicRGB.TabIndex = 1
            campicRGB.TabStop = False
            campicRGB.Tag = "0"
            ' 
            ' campicPointCloud
            ' 
            campicPointCloud.Anchor = AnchorStyles.None
            campicPointCloud.BackColor = Color.Black
            campicPointCloud.Location = New Point(644, 58)
            campicPointCloud.Margin = New Padding(0)
            campicPointCloud.Name = "campicPointCloud"
            campicPointCloud.Size = New Size(920, 536)
            campicPointCloud.SizeMode = PictureBoxSizeMode.StretchImage
            campicPointCloud.TabIndex = 2
            campicPointCloud.TabStop = False
            campicPointCloud.Tag = "1"
            ' 
            ' campicLeft
            ' 
            campicLeft.Anchor = AnchorStyles.None
            campicLeft.BackColor = Color.Black
            campicLeft.Location = New Point(-289, 606)
            campicLeft.Margin = New Padding(0)
            campicLeft.Name = "campicLeft"
            campicLeft.Size = New Size(919, 535)
            campicLeft.SizeMode = PictureBoxSizeMode.StretchImage
            campicLeft.TabIndex = 3
            campicLeft.TabStop = False
            campicLeft.Tag = "2"
            ' 
            ' campicRight
            ' 
            campicRight.Anchor = AnchorStyles.None
            campicRight.BackColor = Color.Black
            campicRight.Location = New Point(644, 606)
            campicRight.Margin = New Padding(0)
            campicRight.Name = "campicRight"
            campicRight.Size = New Size(920, 535)
            campicRight.SizeMode = PictureBoxSizeMode.StretchImage
            campicRight.TabIndex = 4
            campicRight.TabStop = False
            campicRight.Tag = "3"
            ' 
            ' labelRGB
            ' 
            labelRGB.AutoSize = True
            labelRGB.Location = New Point(0, 39)
            labelRGB.Name = "labelRGB"
            labelRGB.Size = New Size(54, 30)
            labelRGB.TabIndex = 8
            labelRGB.Text = "RGB"
            labelRGB.Visible = False
            ' 
            ' labelPointCloud
            ' 
            labelPointCloud.AutoSize = True
            labelPointCloud.Location = New Point(933, 39)
            labelPointCloud.Name = "labelPointCloud"
            labelPointCloud.Size = New Size(124, 30)
            labelPointCloud.TabIndex = 7
            labelPointCloud.Text = "Point Cloud"
            labelPointCloud.Visible = False
            ' 
            ' labelLeft
            ' 
            labelLeft.AutoSize = True
            labelLeft.Location = New Point(0, 587)
            labelLeft.Name = "labelLeft"
            labelLeft.Size = New Size(49, 30)
            labelLeft.TabIndex = 6
            labelLeft.Text = "Left"
            labelLeft.Visible = False
            ' 
            ' labelRight
            ' 
            labelRight.AutoSize = True
            labelRight.Location = New Point(933, 587)
            labelRight.Name = "labelRight"
            labelRight.Size = New Size(63, 30)
            labelRight.TabIndex = 5
            labelRight.Text = "Right"
            labelRight.Visible = False
            ' 
            ' StatusLabel
            ' 
            StatusLabel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            StatusLabel.BackColor = SystemColors.Control
            StatusLabel.BorderStyle = BorderStyle.FixedSingle
            StatusLabel.Location = New Point(0, 1141)
            StatusLabel.Margin = New Padding(0)
            StatusLabel.Name = "StatusLabel"
            StatusLabel.Size = New Size(1275, 30)
            StatusLabel.TabIndex = 2
            StatusLabel.TextAlign = ContentAlignment.MiddleLeft
            ' 
            ' StartUpTimer
            ' 
            StartUpTimer.Interval = 500
            ' 
            ' CameraSwitching
            ' 
            CameraSwitching.AutoSize = True
            CameraSwitching.Font = New Font("Microsoft Sans Serif", 12F)
            CameraSwitching.Location = New Point(61, 122)
            CameraSwitching.Name = "CameraSwitching"
            CameraSwitching.Size = New Size(202, 29)
            CameraSwitching.TabIndex = 9
            CameraSwitching.Text = "CameraSwitching"
            ' 
            ' Timer1
            ' 
            Timer1.Enabled = True
            Timer1.Interval = 10
            ' 
            ' CamSwitchTimer
            ' 
            CamSwitchTimer.Interval = 10
            ' 
            ' MainForm
            ' 
            AutoScaleDimensions = New SizeF(12F, 30F)
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(1275, 1171)
            Controls.Add(CameraSwitching)
            Controls.Add(StatusLabel)
            Controls.Add(campicRight)
            Controls.Add(campicLeft)
            Controls.Add(campicPointCloud)
            Controls.Add(campicRGB)
            Controls.Add(labelRight)
            Controls.Add(labelLeft)
            Controls.Add(labelPointCloud)
            Controls.Add(labelRGB)
            Controls.Add(MainToolStrip)
            Icon = CType(resources.GetObject("$this.Icon"), Icon)
            Margin = New Padding(3, 4, 3, 4)
            Name = "MainForm"
            Text = "CVB Application"
            MainToolStrip.ResumeLayout(False)
            MainToolStrip.PerformLayout()
            CType(campicRGB, ComponentModel.ISupportInitialize).EndInit()
            CType(campicPointCloud, ComponentModel.ISupportInitialize).EndInit()
            CType(campicLeft, ComponentModel.ISupportInitialize).EndInit()
            CType(campicRight, ComponentModel.ISupportInitialize).EndInit()
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
        Friend WithEvents campicRGB As PictureBox
        Friend WithEvents campicPointCloud As PictureBox
        Friend WithEvents campicLeft As PictureBox
        Friend WithEvents campicRight As PictureBox
        Friend WithEvents labelRGB As Label
        Friend WithEvents labelPointCloud As Label
        Friend WithEvents labelLeft As Label
        Friend WithEvents labelRight As Label
        Friend WithEvents StatusLabel As Label
        Friend WithEvents StartUpTimer As Timer
        Friend WithEvents CameraSwitching As Label
        Friend WithEvents Timer1 As Timer
        Friend WithEvents CamSwitchTimer As Timer

    End Class
End Namespace
