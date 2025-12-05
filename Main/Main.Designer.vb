Namespace OpenCVB
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class MainUI : Inherits Form

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
            ToolStrip1 = New ToolStrip()
            PausePlayButton = New ToolStripButton()
            OptionsButton = New ToolStripButton()
            TestAllButton = New ToolStripButton()
            Magnify = New ToolStripButton()
            PixelViewerButton = New ToolStripButton()
            RecentList = New ToolStripDropDownButton()
            AvailableAlgorithms = New ToolStripComboBox()
            AtoZButton = New ToolStripButton()
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
            GLControl = New SharpGL.OpenGLControl()
            ToolStrip1.SuspendLayout()
            CType(CamSwitchProgress, ComponentModel.ISupportInitialize).BeginInit()
            CType(GLControl, ComponentModel.ISupportInitialize).BeginInit()
            SuspendLayout()
            ' 
            ' ToolStrip1
            ' 
            ToolStrip1.ImageScalingSize = New Size(24, 24)
            ToolStrip1.Items.AddRange(New ToolStripItem() {PausePlayButton, OptionsButton, TestAllButton, Magnify, PixelViewerButton, RecentList, AvailableAlgorithms, AtoZButton, AlgDescription})
            ToolStrip1.Location = New Point(0, 0)
            ToolStrip1.Name = "ToolStrip1"
            ToolStrip1.Padding = New Padding(0, 0, 3, 0)
            ToolStrip1.Size = New Size(1898, 39)
            ToolStrip1.TabIndex = 0
            ToolStrip1.Text = "ToolStrip1"
            ' 
            ' PausePlayButton
            ' 
            PausePlayButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            PausePlayButton.Image = My.Resources.Resources.PauseButtonRun
            PausePlayButton.ImageTransparentColor = Color.Magenta
            PausePlayButton.Name = "PausePlayButton"
            PausePlayButton.Size = New Size(34, 34)
            PausePlayButton.Text = "Run Pause Button"
            ' 
            ' OptionsButton
            ' 
            OptionsButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            OptionsButton.Image = My.Resources.Resources.settings
            OptionsButton.ImageTransparentColor = Color.Magenta
            OptionsButton.Name = "OptionsButton"
            OptionsButton.Size = New Size(34, 34)
            OptionsButton.Text = "OpenCVB Settings"
            ' 
            ' TestAllButton
            ' 
            TestAllButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            TestAllButton.Image = My.Resources.Resources.testall
            TestAllButton.ImageTransparentColor = Color.Magenta
            TestAllButton.Name = "TestAllButton"
            TestAllButton.Size = New Size(34, 34)
            TestAllButton.Text = "Test All (overnight testing)"
            ' 
            ' Magnify
            ' 
            Magnify.DisplayStyle = ToolStripItemDisplayStyle.Image
            Magnify.Image = My.Resources.Resources.magnify
            Magnify.ImageTransparentColor = Color.Magenta
            Magnify.Name = "Magnify"
            Magnify.Size = New Size(34, 34)
            Magnify.Text = "Magnify selected rectangle"
            ' 
            ' PixelViewerButton
            ' 
            PixelViewerButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            PixelViewerButton.Image = My.Resources.Resources.PixelViewer
            PixelViewerButton.ImageTransparentColor = Color.Magenta
            PixelViewerButton.Name = "PixelViewerButton"
            PixelViewerButton.Size = New Size(34, 34)
            PixelViewerButton.Text = "Display pixels as text"
            ' 
            ' RecentList
            ' 
            RecentList.DisplayStyle = ToolStripItemDisplayStyle.Text
            RecentList.ImageTransparentColor = Color.Magenta
            RecentList.Name = "RecentList"
            RecentList.Size = New Size(96, 34)
            RecentList.Text = "Recent"
            RecentList.ToolTipText = " "
            ' 
            ' AvailableAlgorithms
            ' 
            AvailableAlgorithms.Name = "AvailableAlgorithms"
            AvailableAlgorithms.Size = New Size(359, 39)
            ' 
            ' AtoZButton
            ' 
            AtoZButton.DisplayStyle = ToolStripItemDisplayStyle.Text
            AtoZButton.ImageTransparentColor = Color.Magenta
            AtoZButton.Name = "AtoZButton"
            AtoZButton.Size = New Size(53, 34)
            AtoZButton.Text = "A-Z"
            AtoZButton.ToolTipText = "Select an algorithm group"
            ' 
            ' AlgDescription
            ' 
            AlgDescription.Name = "AlgDescription"
            AlgDescription.Size = New Size(122, 34)
            AlgDescription.Text = "Description"
            ' 
            ' XYLoc
            ' 
            XYLoc.AutoSize = True
            XYLoc.Location = New Point(7, 862)
            XYLoc.Name = "XYLoc"
            XYLoc.Size = New Size(71, 30)
            XYLoc.TabIndex = 1
            XYLoc.Text = "XYLoc"
            XYLoc.Visible = False
            ' 
            ' CameraSwitching
            ' 
            CameraSwitching.AutoSize = True
            CameraSwitching.Font = New Font("Microsoft Sans Serif", 12F)
            CameraSwitching.Location = New Point(39, 160)
            CameraSwitching.Name = "CameraSwitching"
            CameraSwitching.Size = New Size(202, 29)
            CameraSwitching.TabIndex = 2
            CameraSwitching.Text = "CameraSwitching"
            ' 
            ' CamSwitchProgress
            ' 
            CamSwitchProgress.BackColor = SystemColors.MenuHighlight
            CamSwitchProgress.Location = New Point(39, 198)
            CamSwitchProgress.Margin = New Padding(3, 4, 3, 4)
            CamSwitchProgress.Name = "CamSwitchProgress"
            CamSwitchProgress.Size = New Size(242, 32)
            CamSwitchProgress.TabIndex = 3
            CamSwitchProgress.TabStop = False
            ' 
            ' fpsTimer
            ' 
            ' 
            ' TestAllTimer
            ' 
            ' 
            ' CamSwitchTimer
            ' 
            ' 
            ' RefreshTimer
            ' 
            RefreshTimer.Enabled = True
            ' 
            ' MagnifyTimer
            ' 
            ' 
            ' GLControl
            ' 
            GLControl.DrawFPS = False
            GLControl.Location = New Point(511, 218)
            GLControl.Margin = New Padding(7, 8, 7, 8)
            GLControl.Name = "GLControl"
            GLControl.OpenGLVersion = SharpGL.Version.OpenGLVersion.OpenGL2_1
            GLControl.RenderContextType = SharpGL.RenderContextType.DIBSection
            GLControl.RenderTrigger = SharpGL.RenderTrigger.TimerBased
            GLControl.Size = New Size(960, 440)
            GLControl.TabIndex = 4
            GLControl.Visible = False
            ' 
            ' MainUI
            ' 
            AutoScaleDimensions = New SizeF(12F, 30F)
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(1898, 898)
            Controls.Add(GLControl)
            Controls.Add(CamSwitchProgress)
            Controls.Add(CameraSwitching)
            Controls.Add(XYLoc)
            Controls.Add(ToolStrip1)
            Icon = CType(resources.GetObject("$this.Icon"), Icon)
            Margin = New Padding(3, 4, 3, 4)
            Name = "MainUI"
            Text = "OpenCVB Main Form"
            ToolStrip1.ResumeLayout(False)
            ToolStrip1.PerformLayout()
            CType(CamSwitchProgress, ComponentModel.ISupportInitialize).EndInit()
            CType(GLControl, ComponentModel.ISupportInitialize).EndInit()
            ResumeLayout(False)
            PerformLayout()
        End Sub

        Friend WithEvents ToolStrip1 As ToolStrip
        Friend WithEvents PausePlayButton As ToolStripButton
        Friend WithEvents OptionsButton As ToolStripButton
        Friend WithEvents TestAllButton As ToolStripButton
        Friend WithEvents Magnify As ToolStripButton
        Friend WithEvents PixelViewerButton As ToolStripButton
        Friend WithEvents AvailableAlgorithms As ToolStripComboBox
        Friend WithEvents AtoZButton As ToolStripButton
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
        Friend WithEvents GLControl As SharpGL.OpenGLControl

    End Class
End Namespace