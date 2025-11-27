Namespace CVB
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class MainForm
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
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainForm))
            MainToolStrip = New ToolStrip()
            campics = New PictureBox()
            PausePlayButton = New ToolStripButton()
            SettingsToolStripButton = New ToolStripButton()
            TestAllButton = New ToolStripButton()
            Magnifier = New ToolStripButton()
            ToolStripSeparator1 = New ToolStripSeparator()
            PixelViewer = New ToolStripButton()
            RecentList = New ToolStripDropDownButton()
            AvailableAlgorithms = New ToolStripComboBox()
            ToolStripButton1 = New ToolStripButton()
            ToolStripSeparator2 = New ToolStripSeparator()
            GroupComboBox = New ToolStripComboBox()
            AlgDescription = New ToolStripTextBox()
            CType(campics, ComponentModel.ISupportInitialize).BeginInit()
            MainToolStrip.SuspendLayout()
            SuspendLayout()
            ' 
            ' MainToolStrip
            ' 
            MainToolStrip.ImageScalingSize = New Size(24, 24)
            MainToolStrip.Items.AddRange(New ToolStripItem() {PausePlayButton, SettingsToolStripButton, TestAllButton, Magnifier, ToolStripSeparator1, PixelViewer, RecentList, AvailableAlgorithms, ToolStripButton1, ToolStripSeparator2, GroupComboBox, AlgDescription})
            MainToolStrip.Location = New Point(0, 0)
            MainToolStrip.Name = "MainToolStrip"
            MainToolStrip.Padding = New Padding(0, 0, 3, 0)
            MainToolStrip.Size = New Size(1888, 39)
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
            ' SettingsToolStripButton
            ' 
            SettingsToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            SettingsToolStripButton.Image = CType(resources.GetObject("SettingsToolStripButton.Image"), Image)
            SettingsToolStripButton.ImageTransparentColor = Color.Magenta
            SettingsToolStripButton.Name = "SettingsToolStripButton"
            SettingsToolStripButton.Size = New Size(34, 34)
            SettingsToolStripButton.Text = "Settings"
            SettingsToolStripButton.ToolTipText = "Open Settings"
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
            RecentList.ToolTipText = "Copy selected text"
            ' 
            ' AvailableAlgorithms
            ' 
            AvailableAlgorithms.MaxDropDownItems = 50
            AvailableAlgorithms.MaxLength = 100
            AvailableAlgorithms.Name = "AvailableAlgorithms"
            AvailableAlgorithms.Size = New Size(350, 39)
            AvailableAlgorithms.Text = "Available Algorithms"
            AvailableAlgorithms.ToolTipText = "Available Algorithms"
            ' 
            ' ToolStripButton1
            ' 
            ToolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Text
            ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), Image)
            ToolStripButton1.ImageTransparentColor = Color.Magenta
            ToolStripButton1.Name = "ToolStripButton1"
            ToolStripButton1.Size = New Size(53, 34)
            ToolStripButton1.Text = "A-Z"
            ' 
            ' ToolStripSeparator2
            ' 
            ToolStripSeparator2.Name = "ToolStripSeparator2"
            ToolStripSeparator2.Size = New Size(6, 39)
            ' 
            ' GroupComboBox
            ' 
            GroupComboBox.Name = "GroupComboBox"
            GroupComboBox.Size = New Size(300, 39)
            GroupComboBox.Text = "Algorithm Groups"
            GroupComboBox.ToolTipText = "Algorithm Groups"
            ' 
            ' AlgDescription
            ' 
            AlgDescription.MaxLength = 200
            AlgDescription.Name = "AlgDescription"
            AlgDescription.Size = New Size(500, 39)
            AlgDescription.Text = "Description"
            AlgDescription.ToolTipText = "Description"
            ' 
            ' campics
            ' 
            campics.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            campics.BackColor = SystemColors.ControlLight
            campics.BorderStyle = BorderStyle.FixedSingle
            campics.Location = New Point(12, 54)
            campics.Name = "campics"
            campics.Size = New Size(1864, 609)
            campics.SizeMode = PictureBoxSizeMode.Zoom
            campics.TabIndex = 1
            campics.TabStop = False
            ' 
            ' MainForm
            ' 
            AutoScaleDimensions = New SizeF(12F, 30F)
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(1888, 675)
            Controls.Add(campics)
            Controls.Add(MainToolStrip)
            Icon = CType(resources.GetObject("$this.Icon"), Icon)
            Margin = New Padding(4)
            Name = "MainForm"
            Text = "CVB Application"
            MainToolStrip.ResumeLayout(False)
            MainToolStrip.PerformLayout()
            CType(campics, ComponentModel.ISupportInitialize).EndInit()
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
        Friend WithEvents SettingsToolStripButton As ToolStripButton
        Friend WithEvents RecentList As ToolStripDropDownButton
        Friend WithEvents AvailableAlgorithms As ToolStripComboBox
        Friend WithEvents GroupComboBox As ToolStripComboBox
        Friend WithEvents ToolStripButton1 As ToolStripButton
        Friend WithEvents AlgDescription As ToolStripTextBox
        Friend WithEvents campics As PictureBox

    End Class
End Namespace
