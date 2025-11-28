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
            MainToolStrip.SuspendLayout()
            SuspendLayout()
            ' 
            ' MainToolStrip
            ' 
            MainToolStrip.ImageScalingSize = New Size(24, 24)
            MainToolStrip.Items.AddRange(New ToolStripItem() {PausePlayButton, SettingsToolStripButton, TestAllButton, Magnifier, ToolStripSeparator1, PixelViewer, RecentList, AvailableAlgorithms, ToolStripButton1, ToolStripSeparator2, GroupComboBox, AlgDescription})
            MainToolStrip.Location = New Point(0, 0)
            MainToolStrip.Name = "MainToolStrip"
            MainToolStrip.Padding = New Padding(0, 0, 2, 0)
            MainToolStrip.Size = New Size(1089, 31)
            MainToolStrip.TabIndex = 0
            MainToolStrip.Text = "MainToolStrip"
            ' 
            ' PausePlayButton
            ' 
            PausePlayButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            PausePlayButton.Image = CType(resources.GetObject("PausePlayButton.Image"), Image)
            PausePlayButton.ImageTransparentColor = Color.Magenta
            PausePlayButton.Name = "PausePlayButton"
            PausePlayButton.Size = New Size(28, 28)
            PausePlayButton.Text = "Pause/Play"
            PausePlayButton.ToolTipText = "Play/Pause"
            ' 
            ' SettingsToolStripButton
            ' 
            SettingsToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            SettingsToolStripButton.Image = CType(resources.GetObject("SettingsToolStripButton.Image"), Image)
            SettingsToolStripButton.ImageTransparentColor = Color.Magenta
            SettingsToolStripButton.Name = "SettingsToolStripButton"
            SettingsToolStripButton.Size = New Size(28, 28)
            SettingsToolStripButton.Text = "Settings"
            SettingsToolStripButton.ToolTipText = "Open Settings"
            ' 
            ' TestAllButton
            ' 
            TestAllButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            TestAllButton.Image = CType(resources.GetObject("TestAllButton.Image"), Image)
            TestAllButton.ImageTransparentColor = Color.Magenta
            TestAllButton.Name = "TestAllButton"
            TestAllButton.Size = New Size(28, 28)
            TestAllButton.Text = "Open"
            TestAllButton.ToolTipText = "Test All Algorithms"
            ' 
            ' Magnifier
            ' 
            Magnifier.DisplayStyle = ToolStripItemDisplayStyle.Image
            Magnifier.Image = CType(resources.GetObject("Magnifier.Image"), Image)
            Magnifier.Name = "Magnifier"
            Magnifier.Size = New Size(28, 28)
            Magnifier.ToolTipText = "Magnifier"
            ' 
            ' ToolStripSeparator1
            ' 
            ToolStripSeparator1.Name = "ToolStripSeparator1"
            ToolStripSeparator1.Size = New Size(6, 31)
            ' 
            ' PixelViewer
            ' 
            PixelViewer.DisplayStyle = ToolStripItemDisplayStyle.Image
            PixelViewer.Image = CType(resources.GetObject("PixelViewer.Image"), Image)
            PixelViewer.Name = "PixelViewer"
            PixelViewer.Size = New Size(28, 28)
            PixelViewer.Text = "Pixel Viewer"
            PixelViewer.ToolTipText = "Pixel Viewer"
            ' 
            ' RecentList
            ' 
            RecentList.DisplayStyle = ToolStripItemDisplayStyle.Text
            RecentList.Name = "RecentList"
            RecentList.Size = New Size(56, 28)
            RecentList.Text = "Recent"
            RecentList.ToolTipText = "Copy selected text"
            ' 
            ' AvailableAlgorithms
            ' 
            AvailableAlgorithms.MaxDropDownItems = 50
            AvailableAlgorithms.MaxLength = 100
            AvailableAlgorithms.Name = "AvailableAlgorithms"
            AvailableAlgorithms.Size = New Size(206, 31)
            AvailableAlgorithms.Text = "Available Algorithms"
            AvailableAlgorithms.ToolTipText = "Available Algorithms"
            ' 
            ' ToolStripButton1
            ' 
            ToolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Text
            ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), Image)
            ToolStripButton1.ImageTransparentColor = Color.Magenta
            ToolStripButton1.Name = "ToolStripButton1"
            ToolStripButton1.Size = New Size(31, 28)
            ToolStripButton1.Text = "A-Z"
            ' 
            ' ToolStripSeparator2
            ' 
            ToolStripSeparator2.Name = "ToolStripSeparator2"
            ToolStripSeparator2.Size = New Size(6, 31)
            ' 
            ' GroupComboBox
            ' 
            GroupComboBox.Name = "GroupComboBox"
            GroupComboBox.Size = New Size(177, 31)
            GroupComboBox.Text = "Algorithm Groups"
            GroupComboBox.ToolTipText = "Algorithm Groups"
            ' 
            ' AlgDescription
            ' 
            AlgDescription.AutoSize = False
            AlgDescription.MaxLength = 200
            AlgDescription.Name = "AlgDescription"
            AlgDescription.Size = New Size(293, 31)
            AlgDescription.Text = "Description"
            AlgDescription.ToolTipText = "Description"
            ' 
            ' MainForm
            ' 
            AutoScaleDimensions = New SizeF(7F, 15F)
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(1089, 567)
            Controls.Add(MainToolStrip)
            Icon = CType(resources.GetObject("$this.Icon"), Icon)
            Margin = New Padding(2)
            Name = "MainForm"
            Text = "CVB Application"
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
        Friend WithEvents SettingsToolStripButton As ToolStripButton
        Friend WithEvents RecentList As ToolStripDropDownButton
        Friend WithEvents AvailableAlgorithms As ToolStripComboBox
        Friend WithEvents GroupComboBox As ToolStripComboBox
        Friend WithEvents ToolStripButton1 As ToolStripButton
        Friend WithEvents AlgDescription As ToolStripTextBox

    End Class
End Namespace
