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
        components = New ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Main_UI))
        ToolStrip1 = New ToolStrip()
        BackButton = New ToolStripButton()
        ForwardButton = New ToolStripButton()
        PausePlayButton = New ToolStripButton()
        OptionsButton = New ToolStripButton()
        TestAllButton = New ToolStripButton()
        TreeButton = New ToolStripButton()
        PixelViewerButton = New ToolStripButton()
        BluePlusButton = New ToolStripButton()
        ComplexityButton = New ToolStripButton()
        TranslateButton = New ToolStripButton()
        Advice = New ToolStripButton()
        RecentList = New ToolStripDropDownButton()
        AvailableAlgorithms = New ToolStripComboBox()
        GroupName = New ToolStripComboBox()
        AlgorithmDesc = New TextBox()
        fpsTimer = New Timer(components)
        TestAllTimer = New Timer(components)
        ComplexityTimer = New Timer(components)
        XYLoc = New Label()
        MenuStrip1 = New MenuStrip()
        FileToolStripMenuItem = New ToolStripMenuItem()
        ExitToolStripMenuItem = New ToolStripMenuItem()
        AboutToolStripMenuItem = New ToolStripMenuItem()
        RefreshTimer = New Timer(components)
        ToolTip1 = New ToolTip(components)
        ToolStrip1.SuspendLayout()
        MenuStrip1.SuspendLayout()
        SuspendLayout()
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.ImageScalingSize = New Size(24, 24)
        ToolStrip1.Items.AddRange(New ToolStripItem() {BackButton, ForwardButton, PausePlayButton, OptionsButton, TestAllButton, TreeButton, PixelViewerButton, BluePlusButton, ComplexityButton, TranslateButton, Advice, RecentList, AvailableAlgorithms, GroupName})
        ToolStrip1.Location = New Point(0, 33)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.Size = New Size(1556, 34)
        ToolStrip1.TabIndex = 0
        ToolStrip1.Text = "ToolStrip1"
        ' 
        ' BackButton
        ' 
        BackButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        BackButton.Image = CType(resources.GetObject("BackButton.Image"), Image)
        BackButton.ImageTransparentColor = Color.Magenta
        BackButton.Name = "BackButton"
        BackButton.Size = New Size(34, 29)
        BackButton.Text = "Back to Previous Algorithm"
        ' 
        ' ForwardButton
        ' 
        ForwardButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        ForwardButton.Image = CType(resources.GetObject("ForwardButton.Image"), Image)
        ForwardButton.ImageTransparentColor = Color.Magenta
        ForwardButton.Name = "ForwardButton"
        ForwardButton.Size = New Size(34, 29)
        ForwardButton.Text = "Forward to the next algorithm"
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
        OptionsButton.Text = "OpenCVB Settings"
        ' 
        ' TestAllButton
        ' 
        TestAllButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        TestAllButton.Image = CType(resources.GetObject("TestAllButton.Image"), Image)
        TestAllButton.ImageTransparentColor = Color.Magenta
        TestAllButton.Name = "TestAllButton"
        TestAllButton.Size = New Size(34, 29)
        TestAllButton.Text = "Test All Algorithms"
        ' 
        ' TreeButton
        ' 
        TreeButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        TreeButton.Image = CType(resources.GetObject("TreeButton.Image"), Image)
        TreeButton.ImageTransparentColor = Color.Magenta
        TreeButton.Name = "TreeButton"
        TreeButton.Size = New Size(34, 29)
        TreeButton.Text = "Treeview to see performance and explore algorithm stack"
        ' 
        ' PixelViewerButton
        ' 
        PixelViewerButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        PixelViewerButton.Image = CType(resources.GetObject("PixelViewerButton.Image"), Image)
        PixelViewerButton.ImageTransparentColor = Color.Magenta
        PixelViewerButton.Name = "PixelViewerButton"
        PixelViewerButton.Size = New Size(34, 29)
        PixelViewerButton.Text = "PixelViewer to see pixels under the cursor"
        ' 
        ' BluePlusButton
        ' 
        BluePlusButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        BluePlusButton.Image = CType(resources.GetObject("BluePlusButton.Image"), Image)
        BluePlusButton.ImageTransparentColor = Color.Magenta
        BluePlusButton.Name = "BluePlusButton"
        BluePlusButton.Size = New Size(34, 29)
        BluePlusButton.Text = "Add new OpenGL, Python, C#, C++, or VB.Net algorithms"
        ' 
        ' ComplexityButton
        ' 
        ComplexityButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        ComplexityButton.Image = CType(resources.GetObject("ComplexityButton.Image"), Image)
        ComplexityButton.ImageTransparentColor = Color.Magenta
        ComplexityButton.Name = "ComplexityButton"
        ComplexityButton.Size = New Size(34, 29)
        ComplexityButton.Text = "Measure an algorithm's complexity"
        ' 
        ' TranslateButton
        ' 
        TranslateButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        TranslateButton.Image = CType(resources.GetObject("TranslateButton.Image"), Image)
        TranslateButton.ImageTransparentColor = Color.Magenta
        TranslateButton.Name = "TranslateButton"
        TranslateButton.Size = New Size(34, 29)
        TranslateButton.Text = "Translate algorithms to C#, C++, or VB.Net"
        ' 
        ' Advice
        ' 
        Advice.DisplayStyle = ToolStripItemDisplayStyle.Image
        Advice.Image = CType(resources.GetObject("Advice.Image"), Image)
        Advice.ImageTransparentColor = Color.Magenta
        Advice.Name = "Advice"
        Advice.Size = New Size(34, 29)
        Advice.Text = "Show any advice on options for the current algorithm"
        ' 
        ' RecentList
        ' 
        RecentList.DisplayStyle = ToolStripItemDisplayStyle.Text
        RecentList.Image = CType(resources.GetObject("RecentList.Image"), Image)
        RecentList.ImageTransparentColor = Color.Magenta
        RecentList.Name = "RecentList"
        RecentList.Size = New Size(82, 29)
        RecentList.Text = "Recent"
        RecentList.ToolTipText = "The list of recently used algorithms"
        ' 
        ' AvailableAlgorithms
        ' 
        AvailableAlgorithms.AutoCompleteMode = AutoCompleteMode.SuggestAppend
        AvailableAlgorithms.AutoCompleteSource = AutoCompleteSource.ListItems
        AvailableAlgorithms.Name = "AvailableAlgorithms"
        AvailableAlgorithms.Size = New Size(300, 34)
        ' 
        ' GroupName
        ' 
        GroupName.AutoCompleteMode = AutoCompleteMode.Suggest
        GroupName.AutoCompleteSource = AutoCompleteSource.ListItems
        GroupName.Name = "GroupName"
        GroupName.Size = New Size(300, 34)
        ' 
        ' AlgorithmDesc
        ' 
        AlgorithmDesc.Location = New Point(1124, 36)
        AlgorithmDesc.Multiline = True
        AlgorithmDesc.Name = "AlgorithmDesc"
        AlgorithmDesc.ScrollBars = ScrollBars.Vertical
        AlgorithmDesc.Size = New Size(158, 62)
        AlgorithmDesc.TabIndex = 2
        ' 
        ' fpsTimer
        ' 
        ' 
        ' TestAllTimer
        ' 
        ' 
        ' ComplexityTimer
        ' 
        ' 
        ' XYLoc
        ' 
        XYLoc.AutoSize = True
        XYLoc.Location = New Point(12, 906)
        XYLoc.Name = "XYLoc"
        XYLoc.Size = New Size(60, 25)
        XYLoc.TabIndex = 3
        XYLoc.Text = "XYLoc"
        ' 
        ' MenuStrip1
        ' 
        MenuStrip1.ImageScalingSize = New Size(24, 24)
        MenuStrip1.Items.AddRange(New ToolStripItem() {FileToolStripMenuItem, AboutToolStripMenuItem})
        MenuStrip1.Location = New Point(0, 0)
        MenuStrip1.Name = "MenuStrip1"
        MenuStrip1.Size = New Size(1556, 33)
        MenuStrip1.TabIndex = 4
        MenuStrip1.Text = "MenuStrip1"
        ' 
        ' FileToolStripMenuItem
        ' 
        FileToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {ExitToolStripMenuItem})
        FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        FileToolStripMenuItem.Size = New Size(54, 29)
        FileToolStripMenuItem.Text = "&File"
        ' 
        ' ExitToolStripMenuItem
        ' 
        ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        ExitToolStripMenuItem.Size = New Size(141, 34)
        ExitToolStripMenuItem.Text = "E&xit"
        ' 
        ' AboutToolStripMenuItem
        ' 
        AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
        AboutToolStripMenuItem.Size = New Size(78, 29)
        AboutToolStripMenuItem.Text = "About"
        ' 
        ' RefreshTimer
        ' 
        RefreshTimer.Enabled = True
        RefreshTimer.Interval = 10
        ' 
        ' Main_UI
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1556, 935)
        Controls.Add(XYLoc)
        Controls.Add(AlgorithmDesc)
        Controls.Add(ToolStrip1)
        Controls.Add(MenuStrip1)
        KeyPreview = True
        MainMenuStrip = MenuStrip1
        Name = "Main_UI"
        Text = "OpenCVB Main Form"
        ToolStrip1.ResumeLayout(False)
        ToolStrip1.PerformLayout()
        MenuStrip1.ResumeLayout(False)
        MenuStrip1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
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
    Friend WithEvents GroupName As ToolStripComboBox
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

End Class
