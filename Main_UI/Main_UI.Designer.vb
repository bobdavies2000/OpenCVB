<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Main_UI
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Main_UI))
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.BackButton = New System.Windows.Forms.ToolStripButton()
        Me.ForwardButton = New System.Windows.Forms.ToolStripButton()
        Me.PausePlayButton = New System.Windows.Forms.ToolStripButton()
        Me.OptionsButton = New System.Windows.Forms.ToolStripButton()
        Me.TestAllButton = New System.Windows.Forms.ToolStripButton()
        Me.TreeButton = New System.Windows.Forms.ToolStripButton()
        Me.PixelViewerButton = New System.Windows.Forms.ToolStripButton()
        Me.Magnify = New System.Windows.Forms.ToolStripButton()
        Me.BluePlusButton = New System.Windows.Forms.ToolStripButton()
        Me.ComplexityButton = New System.Windows.Forms.ToolStripButton()
        Me.TranslateButton = New System.Windows.Forms.ToolStripButton()
        Me.Advice = New System.Windows.Forms.ToolStripButton()
        Me.RecentList = New System.Windows.Forms.ToolStripDropDownButton()
        Me.GroupButtonList = New System.Windows.Forms.ToolStripButton()
        Me.AvailableAlgorithms = New System.Windows.Forms.ToolStripComboBox()
        Me.GroupCombo = New System.Windows.Forms.ToolStripComboBox()
        Me.AlgorithmDesc = New System.Windows.Forms.TextBox()
        Me.fpsTimer = New System.Windows.Forms.Timer(Me.components)
        Me.TestAllTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ComplexityTimer = New System.Windows.Forms.Timer(Me.components)
        Me.XYLoc = New System.Windows.Forms.Label()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.FileToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ExitToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.RefreshTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.MagnifyTimer = New System.Windows.Forms.Timer(Me.components)
        Me.CameraSwitching = New System.Windows.Forms.Label()
        Me.CamSwitchProgress = New System.Windows.Forms.PictureBox()
        Me.CamSwitchTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ToolStrip1.SuspendLayout()
        Me.MenuStrip1.SuspendLayout()
        CType(Me.CamSwitchProgress, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'ToolStrip1
        '
        Me.ToolStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.BackButton, Me.ForwardButton, Me.PausePlayButton, Me.OptionsButton, Me.TestAllButton, Me.TreeButton, Me.PixelViewerButton, Me.Magnify, Me.BluePlusButton, Me.ComplexityButton, Me.TranslateButton, Me.Advice, Me.RecentList, Me.GroupButtonList, Me.AvailableAlgorithms, Me.GroupCombo})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 33)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(1583, 34)
        Me.ToolStrip1.TabIndex = 0
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'BackButton
        '
        Me.BackButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.BackButton.Image = CType(resources.GetObject("BackButton.Image"), System.Drawing.Image)
        Me.BackButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.BackButton.Name = "BackButton"
        Me.BackButton.Size = New System.Drawing.Size(34, 29)
        Me.BackButton.Text = "Back to Previous Algorithm"
        '
        'ForwardButton
        '
        Me.ForwardButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.ForwardButton.Image = CType(resources.GetObject("ForwardButton.Image"), System.Drawing.Image)
        Me.ForwardButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ForwardButton.Name = "ForwardButton"
        Me.ForwardButton.Size = New System.Drawing.Size(34, 29)
        Me.ForwardButton.Text = "Forward to the next algorithm"
        '
        'PausePlayButton
        '
        Me.PausePlayButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.PausePlayButton.Image = CType(resources.GetObject("PausePlayButton.Image"), System.Drawing.Image)
        Me.PausePlayButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.PausePlayButton.Name = "PausePlayButton"
        Me.PausePlayButton.Size = New System.Drawing.Size(34, 29)
        Me.PausePlayButton.Text = "Run Pause Button"
        '
        'OptionsButton
        '
        Me.OptionsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.OptionsButton.Image = CType(resources.GetObject("OptionsButton.Image"), System.Drawing.Image)
        Me.OptionsButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.OptionsButton.Name = "OptionsButton"
        Me.OptionsButton.Size = New System.Drawing.Size(34, 29)
        Me.OptionsButton.Text = "OpenCVB Settings"
        '
        'TestAllButton
        '
        Me.TestAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.TestAllButton.Image = CType(resources.GetObject("TestAllButton.Image"), System.Drawing.Image)
        Me.TestAllButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.TestAllButton.Name = "TestAllButton"
        Me.TestAllButton.Size = New System.Drawing.Size(34, 29)
        Me.TestAllButton.Text = "Test All Algorithms"
        '
        'TreeButton
        '
        Me.TreeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.TreeButton.Image = CType(resources.GetObject("TreeButton.Image"), System.Drawing.Image)
        Me.TreeButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.TreeButton.Name = "TreeButton"
        Me.TreeButton.Size = New System.Drawing.Size(34, 29)
        Me.TreeButton.Text = "Treeview to see performance and explore algorithm stack"
        '
        'PixelViewerButton
        '
        Me.PixelViewerButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.PixelViewerButton.Image = CType(resources.GetObject("PixelViewerButton.Image"), System.Drawing.Image)
        Me.PixelViewerButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.PixelViewerButton.Name = "PixelViewerButton"
        Me.PixelViewerButton.Size = New System.Drawing.Size(34, 29)
        Me.PixelViewerButton.Text = "PixelViewer to see pixels under the cursor"
        '
        'Magnify
        '
        Me.Magnify.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.Magnify.Image = CType(resources.GetObject("Magnify.Image"), System.Drawing.Image)
        Me.Magnify.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.Magnify.Name = "Magnify"
        Me.Magnify.Size = New System.Drawing.Size(34, 29)
        Me.Magnify.Text = "Magnify - click then draw a rectangle"
        Me.Magnify.ToolTipText = "Magnify - draw a rectangle then click"
        '
        'BluePlusButton
        '
        Me.BluePlusButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.BluePlusButton.Image = CType(resources.GetObject("BluePlusButton.Image"), System.Drawing.Image)
        Me.BluePlusButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.BluePlusButton.Name = "BluePlusButton"
        Me.BluePlusButton.Size = New System.Drawing.Size(34, 29)
        Me.BluePlusButton.Text = "Add new OpenGL, Python, C#, C++, or VB.Net algorithms"
        '
        'ComplexityButton
        '
        Me.ComplexityButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.ComplexityButton.Image = CType(resources.GetObject("ComplexityButton.Image"), System.Drawing.Image)
        Me.ComplexityButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ComplexityButton.Name = "ComplexityButton"
        Me.ComplexityButton.Size = New System.Drawing.Size(34, 29)
        Me.ComplexityButton.Text = "Measure an algorithm's complexity"
        '
        'TranslateButton
        '
        Me.TranslateButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.TranslateButton.Image = CType(resources.GetObject("TranslateButton.Image"), System.Drawing.Image)
        Me.TranslateButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.TranslateButton.Name = "TranslateButton"
        Me.TranslateButton.Size = New System.Drawing.Size(34, 29)
        Me.TranslateButton.Text = "Translate algorithms to C#, C++, or VB.Net"
        '
        'Advice
        '
        Me.Advice.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.Advice.Image = CType(resources.GetObject("Advice.Image"), System.Drawing.Image)
        Me.Advice.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.Advice.Name = "Advice"
        Me.Advice.Size = New System.Drawing.Size(34, 29)
        Me.Advice.Text = "Show any advice on options for the current algorithm"
        '
        'RecentList
        '
        Me.RecentList.AutoToolTip = False
        Me.RecentList.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.RecentList.Image = CType(resources.GetObject("RecentList.Image"), System.Drawing.Image)
        Me.RecentList.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.RecentList.Name = "RecentList"
        Me.RecentList.ShowDropDownArrow = False
        Me.RecentList.Size = New System.Drawing.Size(68, 29)
        Me.RecentList.Text = "Recent"
        '
        'GroupButtonList
        '
        Me.GroupButtonList.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.GroupButtonList.Image = CType(resources.GetObject("GroupButtonList.Image"), System.Drawing.Image)
        Me.GroupButtonList.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.GroupButtonList.Name = "GroupButtonList"
        Me.GroupButtonList.Size = New System.Drawing.Size(45, 29)
        Me.GroupButtonList.Text = "A-Z"
        Me.GroupButtonList.ToolTipText = "Jump to Algorithm Group"
        '
        'AvailableAlgorithms
        '
        Me.AvailableAlgorithms.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend
        Me.AvailableAlgorithms.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
        Me.AvailableAlgorithms.DropDownHeight = 500
        Me.AvailableAlgorithms.IntegralHeight = False
        Me.AvailableAlgorithms.MaxDropDownItems = 100
        Me.AvailableAlgorithms.Name = "AvailableAlgorithms"
        Me.AvailableAlgorithms.Size = New System.Drawing.Size(315, 34)
        '
        'GroupCombo
        '
        Me.GroupCombo.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest
        Me.GroupCombo.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
        Me.GroupCombo.Name = "GroupCombo"
        Me.GroupCombo.Size = New System.Drawing.Size(270, 34)
        '
        'AlgorithmDesc
        '
        Me.AlgorithmDesc.Location = New System.Drawing.Point(1204, 33)
        Me.AlgorithmDesc.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.AlgorithmDesc.Multiline = True
        Me.AlgorithmDesc.Name = "AlgorithmDesc"
        Me.AlgorithmDesc.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.AlgorithmDesc.Size = New System.Drawing.Size(367, 34)
        Me.AlgorithmDesc.TabIndex = 2
        '
        'fpsTimer
        '
        '
        'TestAllTimer
        '
        '
        'ComplexityTimer
        '
        '
        'XYLoc
        '
        Me.XYLoc.AutoSize = True
        Me.XYLoc.Location = New System.Drawing.Point(11, 725)
        Me.XYLoc.Name = "XYLoc"
        Me.XYLoc.Size = New System.Drawing.Size(57, 20)
        Me.XYLoc.TabIndex = 3
        Me.XYLoc.Text = "XYLoc"
        Me.XYLoc.Visible = False
        '
        'MenuStrip1
        '
        Me.MenuStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.FileToolStripMenuItem, Me.AboutToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Padding = New System.Windows.Forms.Padding(5, 2, 0, 2)
        Me.MenuStrip1.Size = New System.Drawing.Size(1583, 33)
        Me.MenuStrip1.TabIndex = 4
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'FileToolStripMenuItem
        '
        Me.FileToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ExitToolStripMenuItem})
        Me.FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        Me.FileToolStripMenuItem.Size = New System.Drawing.Size(54, 29)
        Me.FileToolStripMenuItem.Text = "&File"
        '
        'ExitToolStripMenuItem
        '
        Me.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        Me.ExitToolStripMenuItem.Size = New System.Drawing.Size(141, 34)
        Me.ExitToolStripMenuItem.Text = "E&xit"
        '
        'AboutToolStripMenuItem
        '
        Me.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
        Me.AboutToolStripMenuItem.Size = New System.Drawing.Size(78, 29)
        Me.AboutToolStripMenuItem.Text = "About"
        '
        'RefreshTimer
        '
        Me.RefreshTimer.Enabled = True
        Me.RefreshTimer.Interval = 10
        '
        'MagnifyTimer
        '
        Me.MagnifyTimer.Interval = 500
        '
        'CameraSwitching
        '
        Me.CameraSwitching.AutoSize = True
        Me.CameraSwitching.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CameraSwitching.Location = New System.Drawing.Point(25, 130)
        Me.CameraSwitching.Name = "CameraSwitching"
        Me.CameraSwitching.Size = New System.Drawing.Size(216, 29)
        Me.CameraSwitching.TabIndex = 5
        Me.CameraSwitching.Text = "Setting Up Camera"
        '
        'CamSwitchProgress
        '
        Me.CamSwitchProgress.BackColor = System.Drawing.SystemColors.MenuHighlight
        Me.CamSwitchProgress.Location = New System.Drawing.Point(30, 162)
        Me.CamSwitchProgress.Name = "CamSwitchProgress"
        Me.CamSwitchProgress.Size = New System.Drawing.Size(36, 36)
        Me.CamSwitchProgress.TabIndex = 6
        Me.CamSwitchProgress.TabStop = False
        '
        'CamSwitchTimer
        '
        Me.CamSwitchTimer.Enabled = True
        '
        'Main_UI
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1583, 748)
        Me.Controls.Add(Me.CamSwitchProgress)
        Me.Controls.Add(Me.CameraSwitching)
        Me.Controls.Add(Me.XYLoc)
        Me.Controls.Add(Me.AlgorithmDesc)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.KeyPreview = True
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.Name = "Main_UI"
        Me.Text = "OpenCVB Main Form"
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        CType(Me.CamSwitchProgress, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents BackButton As ToolStripButton
    Friend WithEvents ForwardButton As ToolStripButton
    Friend WithEvents PausePlayButton As ToolStripButton
    Friend WithEvents OptionsButton As ToolStripButton
    Friend WithEvents TestAllButton As ToolStripButton
    Friend WithEvents TreeButton As ToolStripButton
    Friend WithEvents PixelViewerButton As ToolStripButton
    Friend WithEvents BluePlusButton As ToolStripButton
    Friend WithEvents ComplexityButton As ToolStripButton
    Friend WithEvents TranslateButton As ToolStripButton
    Friend WithEvents Advice As ToolStripButton
    Friend WithEvents AvailableAlgorithms As ToolStripComboBox
    Friend WithEvents GroupCombo As ToolStripComboBox
    Friend WithEvents AlgorithmDesc As TextBox
    Friend WithEvents fpsTimer As Timer
    Friend WithEvents TestAllTimer As Timer
    Friend WithEvents ComplexityTimer As Timer
    Friend WithEvents XYLoc As Label
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents FileToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ExitToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents AboutToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents RefreshTimer As Timer
    Friend WithEvents RecentList As ToolStripDropDownButton
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents Magnify As ToolStripButton
    Friend WithEvents MagnifyTimer As Timer
    Friend WithEvents CameraSwitching As Label
    Friend WithEvents CamSwitchProgress As PictureBox
    Friend WithEvents CamSwitchTimer As Timer
    Friend WithEvents GroupButtonList As ToolStripButton
End Class
