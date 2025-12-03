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
            Options = New ToolStripButton()
            TestAllButton = New ToolStripButton()
            Magnifier = New ToolStripButton()
            ToolStripSeparator1 = New ToolStripSeparator()
            PixelViewer = New ToolStripButton()
            RecentList = New ToolStripDropDownButton()
            AvailableAlgorithms = New ToolStripComboBox()
            AtoZ = New ToolStripButton()
            ToolStripSeparator2 = New ToolStripSeparator()
            AlgDescription = New ToolStripTextBox()
            campics = New PictureBox()
            StatusLabel = New Label()
            MainToolStrip.SuspendLayout()
            CType(campics, ComponentModel.ISupportInitialize).BeginInit()
            SuspendLayout()
            ' 
            ' MainToolStrip
            ' 
            MainToolStrip.ImageScalingSize = New Size(24, 24)
            MainToolStrip.Items.AddRange(New ToolStripItem() {PausePlayButton, Options, TestAllButton, Magnifier, ToolStripSeparator1, PixelViewer, RecentList, AvailableAlgorithms, AtoZ, ToolStripSeparator2, AlgDescription})
            MainToolStrip.Location = New Point(0, 0)
            MainToolStrip.Name = "MainToolStrip"
            MainToolStrip.Padding = New Padding(0, 0, 3, 0)
            MainToolStrip.Size = New Size(1867, 39)
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
            ' Options
            ' 
            Options.DisplayStyle = ToolStripItemDisplayStyle.Image
            Options.Image = CType(resources.GetObject("Options.Image"), Image)
            Options.ImageTransparentColor = Color.Magenta
            Options.Name = "Options"
            Options.Size = New Size(34, 34)
            Options.Text = "Settings"
            Options.ToolTipText = "Open OpenCVB Settings"
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
            ' campics
            ' 
            campics.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            campics.BackColor = Color.Black
            campics.Location = New Point(0, 39)
            campics.Margin = New Padding(0)
            campics.Name = "campics"
            campics.Size = New Size(1867, 1095)
            campics.SizeMode = PictureBoxSizeMode.Zoom
            campics.TabIndex = 1
            campics.TabStop = False
            ' 
            ' StatusLabel
            ' 
            StatusLabel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            StatusLabel.BackColor = SystemColors.Control
            StatusLabel.BorderStyle = BorderStyle.FixedSingle
            StatusLabel.Location = New Point(9, 1125)
            StatusLabel.Margin = New Padding(0)
            StatusLabel.Name = "StatusLabel"
            StatusLabel.Size = New Size(1867, 30)
            StatusLabel.TabIndex = 2
            StatusLabel.TextAlign = ContentAlignment.MiddleLeft
            ' 
            ' MainForm
            ' 
            AutoScaleDimensions = New SizeF(12.0F, 30.0F)
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(1867, 1164)
            Controls.Add(StatusLabel)
            Controls.Add(campics)
            Controls.Add(MainToolStrip)
            Icon = CType(resources.GetObject("$this.Icon"), Icon)
            Margin = New Padding(3, 4, 3, 4)
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
        Friend WithEvents Options As ToolStripButton
        Friend WithEvents RecentList As ToolStripDropDownButton
        Friend WithEvents AvailableAlgorithms As ToolStripComboBox
        Friend WithEvents AtoZ As ToolStripButton
        Friend WithEvents AlgDescription As ToolStripTextBox
        Friend WithEvents campics As PictureBox
        Friend WithEvents StatusLabel As Label

    End Class
End Namespace
