Namespace OpenCVB
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
            ToolStrip1 = New ToolStrip()
            PausePlayButton = New ToolStripButton()
            OptionsButton = New ToolStripButton()
            TestAllButton = New ToolStripButton()
            Magnify = New ToolStripButton()
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
            GLControl = New SharpGL.OpenGLControl()
            ToolStrip1.SuspendLayout()
            CType(CamSwitchProgress, ComponentModel.ISupportInitialize).BeginInit()
            CType(GLControl, ComponentModel.ISupportInitialize).BeginInit()
            SuspendLayout()
            ' 
            ' ToolStrip1
            ' 
            ToolStrip1.ImageScalingSize = New Size(24, 24)
            ToolStrip1.Items.AddRange(New ToolStripItem() {PausePlayButton, OptionsButton, TestAllButton, Magnify, PixelViewerButton, RecentList, AvailableAlgorithms, AtoZButton, GroupComboBox, AlgDescription})
            ToolStrip1.Location = New Point(0, 0)
            ToolStrip1.Name = "ToolStrip1"
            ToolStrip1.Size = New Size(1582, 34)
            ToolStrip1.TabIndex = 0
            ToolStrip1.Text = "ToolStrip1"
            ' 
            ' PausePlayButton
            ' 
            PausePlayButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            PausePlayButton.Image = Global.Main.My.Resources.Resources.PauseButtonRun
            PausePlayButton.ImageTransparentColor = Color.Magenta
            PausePlayButton.Name = "PausePlayButton"
            PausePlayButton.Size = New Size(34, 29)
            PausePlayButton.Text = "Run Pause Button"
            ' 
            ' OptionsButton
            ' 
            OptionsButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            OptionsButton.Image = Global.Main.My.Resources.Resources.settings
            OptionsButton.ImageTransparentColor = Color.Magenta
            OptionsButton.Name = "OptionsButton"
            OptionsButton.Size = New Size(34, 29)
            OptionsButton.Text = "OpenCVB Settings"
            ' 
            ' TestAllButton
            ' 
            TestAllButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            TestAllButton.Image = Global.Main.My.Resources.Resources.testall
            TestAllButton.ImageTransparentColor = Color.Magenta
            TestAllButton.Name = "TestAllButton"
            TestAllButton.Size = New Size(34, 29)
            TestAllButton.Text = "Test All (overnight testing)"
            ' 
            ' Magnify
            ' 
            Magnify.DisplayStyle = ToolStripItemDisplayStyle.Image
            Magnify.Image = Global.Main.My.Resources.Resources.magnify
            Magnify.ImageTransparentColor = Color.Magenta
            Magnify.Name = "Magnify"
            Magnify.Size = New Size(34, 29)
            Magnify.Text = "Magnify selected rectangle"
            ' 
            ' PixelViewerButton
            ' 
            PixelViewerButton.DisplayStyle = ToolStripItemDisplayStyle.Image
            PixelViewerButton.Image = Global.Main.My.Resources.Resources.PixelViewer
            PixelViewerButton.ImageTransparentColor = Color.Magenta
            PixelViewerButton.Name = "PixelViewerButton"
            PixelViewerButton.Size = New Size(34, 29)
            PixelViewerButton.Text = "Display pixels as text"
            ' 
            ' RecentList
            ' 
            RecentList.DisplayStyle = ToolStripItemDisplayStyle.Text
            RecentList.ImageTransparentColor = Color.Magenta
            RecentList.Name = "RecentList"
            RecentList.Size = New Size(82, 29)
            RecentList.Text = "Recent"
            RecentList.ToolTipText = "Recently selected algorithms"
            ' 
            ' AvailableAlgorithms
            ' 
            AvailableAlgorithms.Name = "AvailableAlgorithms"
            AvailableAlgorithms.Size = New Size(300, 34)
            ' 
            ' AtoZButton
            ' 
            AtoZButton.DisplayStyle = ToolStripItemDisplayStyle.Text
            AtoZButton.ImageTransparentColor = Color.Magenta
            AtoZButton.Name = "AtoZButton"
            AtoZButton.Size = New Size(45, 29)
            AtoZButton.Text = "A-Z"
            AtoZButton.ToolTipText = "Select an algorithm group"
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
            CameraSwitching.Font = New Font("Microsoft Sans Serif", 12.0F)
            CameraSwitching.Location = New Point(33, 133)
            CameraSwitching.Name = "CameraSwitching"
            CameraSwitching.Size = New Size(202, 29)
            CameraSwitching.TabIndex = 2
            CameraSwitching.Text = "CameraSwitching"
            ' 
            ' CamSwitchProgress
            ' 
            CamSwitchProgress.BackColor = SystemColors.MenuHighlight
            CamSwitchProgress.Location = New Point(33, 165)
            CamSwitchProgress.Name = "CamSwitchProgress"
            CamSwitchProgress.Size = New Size(202, 27)
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
            GLControl.Location = New Point(710, 172)
            GLControl.Margin = New Padding(5, 6, 5, 6)
            GLControl.Name = "GLControl"
            GLControl.OpenGLVersion = SharpGL.Version.OpenGLVersion.OpenGL2_1
            GLControl.RenderContextType = SharpGL.RenderContextType.DIBSection
            GLControl.RenderTrigger = SharpGL.RenderTrigger.TimerBased
            GLControl.Size = New Size(457, 346)
            GLControl.TabIndex = 4
            GLControl.Visible = False
            ' 
            ' Main
            ' 
            AutoScaleDimensions = New SizeF(10.0F, 25.0F)
            AutoScaleMode = AutoScaleMode.Font
            ClientSize = New Size(1582, 748)
            Controls.Add(GLControl)
            Controls.Add(CamSwitchProgress)
            Controls.Add(CameraSwitching)
            Controls.Add(XYLoc)
            Controls.Add(ToolStrip1)
            Name = "Main"
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
        Friend WithEvents GLControl As SharpGL.OpenGLControl

    End Class
End Namespace