<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OpenCVB
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OpenCVB))
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.ToolStripButton1 = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripButton2 = New System.Windows.Forms.ToolStripButton()
        Me.PausePlayButton = New System.Windows.Forms.ToolStripButton()
        Me.OptionsButton = New System.Windows.Forms.ToolStripButton()
        Me.TestAllButton = New System.Windows.Forms.ToolStripButton()
        Me.TreeButton = New System.Windows.Forms.ToolStripButton()
        Me.PixelViewerButton = New System.Windows.Forms.ToolStripButton()
        Me.ToolStripButton3 = New System.Windows.Forms.ToolStripButton()
        Me.ComplexityButton = New System.Windows.Forms.ToolStripButton()
        Me.TranslateButton = New System.Windows.Forms.ToolStripButton()
        Me.Advice = New System.Windows.Forms.ToolStripButton()
        Me.RecentList = New System.Windows.Forms.ToolStripDropDownButton()
        Me.TestAllTimer = New System.Windows.Forms.Timer(Me.components)
        Me.fpsTimer = New System.Windows.Forms.Timer(Me.components)
        Me.AlgorithmDesc = New System.Windows.Forms.Label()
        Me.GroupName = New System.Windows.Forms.ComboBox()
        Me.AvailableAlgorithms = New System.Windows.Forms.ComboBox()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.MainMenu = New System.Windows.Forms.ToolStripMenuItem()
        Me.ExitCall = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.RefreshTimer = New System.Windows.Forms.Timer(Me.components)
        Me.XYloc = New System.Windows.Forms.Label()
        Me.ComplexityTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ToolStrip1.SuspendLayout()
        Me.MenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ToolStrip1
        '
        Me.ToolStrip1.AutoSize = False
        Me.ToolStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripButton1, Me.ToolStripButton2, Me.PausePlayButton, Me.OptionsButton, Me.TestAllButton, Me.TreeButton, Me.PixelViewerButton, Me.ToolStripButton3, Me.ComplexityButton, Me.TranslateButton, Me.Advice, Me.RecentList})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 31)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Padding = New System.Windows.Forms.Padding(0, 0, 3, 0)
        Me.ToolStrip1.Size = New System.Drawing.Size(1786, 59)
        Me.ToolStrip1.TabIndex = 0
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'ToolStripButton1
        '
        Me.ToolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), System.Drawing.Image)
        Me.ToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripButton1.Name = "ToolStripButton1"
        Me.ToolStripButton1.Size = New System.Drawing.Size(34, 54)
        Me.ToolStripButton1.Text = "Back to previous algorithm"
        '
        'ToolStripButton2
        '
        Me.ToolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.ToolStripButton2.Image = CType(resources.GetObject("ToolStripButton2.Image"), System.Drawing.Image)
        Me.ToolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripButton2.Name = "ToolStripButton2"
        Me.ToolStripButton2.Size = New System.Drawing.Size(34, 54)
        Me.ToolStripButton2.Text = "ToolStripButton2"
        Me.ToolStripButton2.ToolTipText = "Forward to next algorithm"
        '
        'PausePlayButton
        '
        Me.PausePlayButton.AutoToolTip = False
        Me.PausePlayButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.PausePlayButton.Image = CType(resources.GetObject("PausePlayButton.Image"), System.Drawing.Image)
        Me.PausePlayButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.PausePlayButton.Name = "PausePlayButton"
        Me.PausePlayButton.Size = New System.Drawing.Size(34, 54)
        Me.PausePlayButton.Text = "Run"
        Me.PausePlayButton.ToolTipText = "Pause/Play"
        '
        'OptionsButton
        '
        Me.OptionsButton.AutoToolTip = False
        Me.OptionsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.OptionsButton.Image = CType(resources.GetObject("OptionsButton.Image"), System.Drawing.Image)
        Me.OptionsButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.OptionsButton.Name = "OptionsButton"
        Me.OptionsButton.Size = New System.Drawing.Size(34, 54)
        Me.OptionsButton.Text = "Options"
        Me.OptionsButton.ToolTipText = "Camera Settings and Global Options"
        '
        'TestAllButton
        '
        Me.TestAllButton.AutoToolTip = False
        Me.TestAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.TestAllButton.Image = CType(resources.GetObject("TestAllButton.Image"), System.Drawing.Image)
        Me.TestAllButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.TestAllButton.Name = "TestAllButton"
        Me.TestAllButton.Size = New System.Drawing.Size(34, 54)
        Me.TestAllButton.Text = "Test All"
        Me.TestAllButton.ToolTipText = "Test each algorithm in succession"
        '
        'TreeButton
        '
        Me.TreeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.TreeButton.Image = Global.OpenCVB.My.Resources.Resources.Tree
        Me.TreeButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.TreeButton.Name = "TreeButton"
        Me.TreeButton.Size = New System.Drawing.Size(34, 54)
        Me.TreeButton.Text = "TreeButton"
        Me.TreeButton.ToolTipText = "TreeView: show how the algorithm was constructed with a tree view"
        '
        'PixelViewerButton
        '
        Me.PixelViewerButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.PixelViewerButton.Image = CType(resources.GetObject("PixelViewerButton.Image"), System.Drawing.Image)
        Me.PixelViewerButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.PixelViewerButton.Name = "PixelViewerButton"
        Me.PixelViewerButton.Size = New System.Drawing.Size(34, 54)
        Me.PixelViewerButton.ToolTipText = "PixelViewer: display pixel data by moving mouse over any image"
        '
        'ToolStripButton3
        '
        Me.ToolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.ToolStripButton3.Image = CType(resources.GetObject("ToolStripButton3.Image"), System.Drawing.Image)
        Me.ToolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripButton3.Name = "ToolStripButton3"
        Me.ToolStripButton3.Size = New System.Drawing.Size(34, 54)
        Me.ToolStripButton3.Text = "ToolStripButton3"
        Me.ToolStripButton3.ToolTipText = "Add a new OpenGL, C++, C#, PyStream, or VB.Net algorithm"
        '
        'ComplexityButton
        '
        Me.ComplexityButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.ComplexityButton.Image = CType(resources.GetObject("ComplexityButton.Image"), System.Drawing.Image)
        Me.ComplexityButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ComplexityButton.Name = "ComplexityButton"
        Me.ComplexityButton.Size = New System.Drawing.Size(34, 54)
        Me.ComplexityButton.Text = "ToolStripButton5"
        '
        'TranslateButton
        '
        Me.TranslateButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.TranslateButton.Image = CType(resources.GetObject("TranslateButton.Image"), System.Drawing.Image)
        Me.TranslateButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.TranslateButton.Name = "TranslateButton"
        Me.TranslateButton.Size = New System.Drawing.Size(34, 54)
        Me.TranslateButton.Text = "ToolStripButton4"
        '
        'Advice
        '
        Me.Advice.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.Advice.Image = CType(resources.GetObject("Advice.Image"), System.Drawing.Image)
        Me.Advice.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.Advice.Name = "Advice"
        Me.Advice.Size = New System.Drawing.Size(34, 54)
        Me.Advice.Text = "ToolStripButton4"
        Me.Advice.ToolTipText = "Get advice on adjustments available for this algorithm."
        '
        'RecentList
        '
        Me.RecentList.AutoToolTip = False
        Me.RecentList.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.RecentList.Image = CType(resources.GetObject("RecentList.Image"), System.Drawing.Image)
        Me.RecentList.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.RecentList.Name = "RecentList"
        Me.RecentList.Size = New System.Drawing.Size(82, 54)
        Me.RecentList.Text = "Recent"
        '
        'TestAllTimer
        '
        Me.TestAllTimer.Interval = 5000
        '
        'fpsTimer
        '
        Me.fpsTimer.Interval = 1000
        '
        'AlgorithmDesc
        '
        Me.AlgorithmDesc.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.AlgorithmDesc.Location = New System.Drawing.Point(1290, 38)
        Me.AlgorithmDesc.Name = "AlgorithmDesc"
        Me.AlgorithmDesc.Size = New System.Drawing.Size(484, 51)
        Me.AlgorithmDesc.TabIndex = 0
        Me.AlgorithmDesc.Text = "Algorithm Desc"
        '
        'GroupName
        '
        Me.GroupName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.GroupName.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupName.FormattingEnabled = True
        Me.GroupName.Location = New System.Drawing.Point(1013, 38)
        Me.GroupName.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.GroupName.Name = "GroupName"
        Me.GroupName.Size = New System.Drawing.Size(271, 34)
        Me.GroupName.TabIndex = 1
        '
        'AvailableAlgorithms
        '
        Me.AvailableAlgorithms.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.AvailableAlgorithms.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.AvailableAlgorithms.FormattingEnabled = True
        Me.AvailableAlgorithms.Location = New System.Drawing.Point(564, 38)
        Me.AvailableAlgorithms.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.AvailableAlgorithms.MaxDropDownItems = 25
        Me.AvailableAlgorithms.Name = "AvailableAlgorithms"
        Me.AvailableAlgorithms.Size = New System.Drawing.Size(443, 34)
        Me.AvailableAlgorithms.TabIndex = 0
        '
        'MenuStrip1
        '
        Me.MenuStrip1.GripMargin = New System.Windows.Forms.Padding(2, 2, 0, 2)
        Me.MenuStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MainMenu, Me.AboutToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Padding = New System.Windows.Forms.Padding(6, 1, 0, 1)
        Me.MenuStrip1.Size = New System.Drawing.Size(1786, 31)
        Me.MenuStrip1.TabIndex = 2
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'MainMenu
        '
        Me.MainMenu.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ExitCall, Me.ToolStripSeparator1})
        Me.MainMenu.Name = "MainMenu"
        Me.MainMenu.Size = New System.Drawing.Size(54, 29)
        Me.MainMenu.Text = "&File"
        '
        'ExitCall
        '
        Me.ExitCall.Name = "ExitCall"
        Me.ExitCall.Size = New System.Drawing.Size(141, 34)
        Me.ExitCall.Text = "E&xit"
        '
        'ToolStripSeparator1
        '
        Me.ToolStripSeparator1.Name = "ToolStripSeparator1"
        Me.ToolStripSeparator1.Size = New System.Drawing.Size(138, 6)
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
        'XYloc
        '
        Me.XYloc.AutoSize = True
        Me.XYloc.Location = New System.Drawing.Point(12, 1032)
        Me.XYloc.Name = "XYloc"
        Me.XYloc.Size = New System.Drawing.Size(51, 20)
        Me.XYloc.TabIndex = 3
        Me.XYloc.Text = "XYloc"
        '
        'ComplexityTimer
        '
        Me.ComplexityTimer.Interval = 30000
        '
        'OpenCVB
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1786, 1061)
        Me.Controls.Add(Me.XYloc)
        Me.Controls.Add(Me.AvailableAlgorithms)
        Me.Controls.Add(Me.GroupName)
        Me.Controls.Add(Me.AlgorithmDesc)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.DoubleBuffered = True
        Me.KeyPreview = True
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Margin = New System.Windows.Forms.Padding(3, 2, 3, 2)
        Me.Name = "OpenCVB"
        Me.Text = "OpenCVB"
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents TestAllButton As ToolStripButton
    Friend WithEvents TestAllTimer As Timer
    Friend WithEvents fpsTimer As Timer
    Friend WithEvents AlgorithmDesc As Label
    Friend WithEvents OptionsButton As ToolStripButton
    Friend WithEvents PausePlayButton As ToolStripButton
    Friend WithEvents GroupName As ComboBox
    Friend WithEvents AvailableAlgorithms As ComboBox
    Friend WithEvents TreeButton As ToolStripButton
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents MainMenu As ToolStripMenuItem
    Friend WithEvents ExitCall As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents AboutToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents PixelViewerButton As ToolStripButton
    Friend WithEvents ToolStripButton1 As ToolStripButton
    Friend WithEvents ToolStripButton2 As ToolStripButton
    Friend WithEvents RefreshTimer As Timer
    Friend WithEvents XYloc As Label
    Friend WithEvents ToolStripButton3 As ToolStripButton
    Friend WithEvents TranslateButton As ToolStripButton
    Friend WithEvents ComplexityButton As ToolStripButton
    Friend WithEvents ComplexityTimer As Timer
    Friend WithEvents Advice As ToolStripButton
    Friend WithEvents RecentList As ToolStripDropDownButton
End Class
