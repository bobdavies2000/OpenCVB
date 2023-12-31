﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
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
        Me.SnapShotButton = New System.Windows.Forms.ToolStripButton()
        Me.TreeButton = New System.Windows.Forms.ToolStripButton()
        Me.PixelViewerButton = New System.Windows.Forms.ToolStripButton()
        Me.TestAllTimer = New System.Windows.Forms.Timer(Me.components)
        Me.fpsTimer = New System.Windows.Forms.Timer(Me.components)
        Me.AlgorithmDesc = New System.Windows.Forms.Label()
        Me.OpenCVkeyword = New System.Windows.Forms.ComboBox()
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.AvailableAlgorithms = New System.Windows.Forms.ComboBox()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.MainMenu = New System.Windows.Forms.ToolStripMenuItem()
        Me.ExitCall = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripSeparator1 = New System.Windows.Forms.ToolStripSeparator()
        Me.SurveyToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.CreateSurveyImagesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ValidateAllTreeViewsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.RefreshTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ValidateTreeView = New System.Windows.Forms.Timer(Me.components)
        Me.ToolStrip1.SuspendLayout()
        Me.MenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ToolStrip1
        '
        Me.ToolStrip1.AutoSize = False
        Me.ToolStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripButton1, Me.ToolStripButton2, Me.PausePlayButton, Me.OptionsButton, Me.TestAllButton, Me.SnapShotButton, Me.TreeButton, Me.PixelViewerButton})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 33)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Padding = New System.Windows.Forms.Padding(0, 0, 3, 0)
        Me.ToolStrip1.Size = New System.Drawing.Size(1786, 58)
        Me.ToolStrip1.TabIndex = 0
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'ToolStripButton1
        '
        Me.ToolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), System.Drawing.Image)
        Me.ToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripButton1.Name = "ToolStripButton1"
        Me.ToolStripButton1.Size = New System.Drawing.Size(34, 53)
        Me.ToolStripButton1.Text = "ToolStripButton1"
        '
        'ToolStripButton2
        '
        Me.ToolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.ToolStripButton2.Image = CType(resources.GetObject("ToolStripButton2.Image"), System.Drawing.Image)
        Me.ToolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripButton2.Name = "ToolStripButton2"
        Me.ToolStripButton2.Size = New System.Drawing.Size(34, 53)
        Me.ToolStripButton2.Text = "ToolStripButton2"
        '
        'PausePlayButton
        '
        Me.PausePlayButton.AutoToolTip = False
        Me.PausePlayButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.PausePlayButton.Image = CType(resources.GetObject("PausePlayButton.Image"), System.Drawing.Image)
        Me.PausePlayButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.PausePlayButton.Name = "PausePlayButton"
        Me.PausePlayButton.Size = New System.Drawing.Size(34, 53)
        Me.PausePlayButton.Text = "PausePlay"
        Me.PausePlayButton.ToolTipText = "Pause/Play"
        '
        'OptionsButton
        '
        Me.OptionsButton.AutoToolTip = False
        Me.OptionsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.OptionsButton.Image = CType(resources.GetObject("OptionsButton.Image"), System.Drawing.Image)
        Me.OptionsButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.OptionsButton.Name = "OptionsButton"
        Me.OptionsButton.Size = New System.Drawing.Size(34, 53)
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
        Me.TestAllButton.Size = New System.Drawing.Size(34, 53)
        Me.TestAllButton.Text = "Test All"
        Me.TestAllButton.ToolTipText = "Test each algorithm in succession"
        '
        'SnapShotButton
        '
        Me.SnapShotButton.AutoToolTip = False
        Me.SnapShotButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.SnapShotButton.Image = CType(resources.GetObject("SnapShotButton.Image"), System.Drawing.Image)
        Me.SnapShotButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.SnapShotButton.Name = "SnapShotButton"
        Me.SnapShotButton.Size = New System.Drawing.Size(34, 53)
        Me.SnapShotButton.ToolTipText = "Capture a snapshot of the images below and put it on the clipboard"
        Me.SnapShotButton.Visible = False
        '
        'TreeButton
        '
        Me.TreeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.TreeButton.Image = Global.OpenCVB.My.Resources.Resources.Tree
        Me.TreeButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.TreeButton.Name = "TreeButton"
        Me.TreeButton.Size = New System.Drawing.Size(34, 53)
        Me.TreeButton.Text = "TreeButton"
        Me.TreeButton.ToolTipText = "Show how the algorithm was constructed with a tree view"
        '
        'PixelViewerButton
        '
        Me.PixelViewerButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.PixelViewerButton.Image = CType(resources.GetObject("PixelViewerButton.Image"), System.Drawing.Image)
        Me.PixelViewerButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.PixelViewerButton.Name = "PixelViewerButton"
        Me.PixelViewerButton.Size = New System.Drawing.Size(34, 53)
        Me.PixelViewerButton.ToolTipText = "Display pixel data when the mouse moves over dst2 or dst3 below"
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
        Me.AlgorithmDesc.Location = New System.Drawing.Point(1099, 36)
        Me.AlgorithmDesc.Name = "AlgorithmDesc"
        Me.AlgorithmDesc.Size = New System.Drawing.Size(675, 51)
        Me.AlgorithmDesc.TabIndex = 0
        Me.AlgorithmDesc.Text = "Algorithm Desc"
        '
        'OpenCVkeyword
        '
        Me.OpenCVkeyword.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.OpenCVkeyword.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.OpenCVkeyword.FormattingEnabled = True
        Me.OpenCVkeyword.Location = New System.Drawing.Point(730, 39)
        Me.OpenCVkeyword.Name = "OpenCVkeyword"
        Me.OpenCVkeyword.Size = New System.Drawing.Size(363, 34)
        Me.OpenCVkeyword.TabIndex = 1
        '
        'AvailableAlgorithms
        '
        Me.AvailableAlgorithms.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.AvailableAlgorithms.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.AvailableAlgorithms.FormattingEnabled = True
        Me.AvailableAlgorithms.Location = New System.Drawing.Point(361, 39)
        Me.AvailableAlgorithms.Name = "AvailableAlgorithms"
        Me.AvailableAlgorithms.Size = New System.Drawing.Size(363, 34)
        Me.AvailableAlgorithms.TabIndex = 0
        '
        'MenuStrip1
        '
        Me.MenuStrip1.GripMargin = New System.Windows.Forms.Padding(2, 2, 0, 2)
        Me.MenuStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MainMenu, Me.SurveyToolStripMenuItem, Me.AboutToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(1786, 33)
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
        'SurveyToolStripMenuItem
        '
        Me.SurveyToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CreateSurveyImagesToolStripMenuItem, Me.ValidateAllTreeViewsToolStripMenuItem})
        Me.SurveyToolStripMenuItem.Name = "SurveyToolStripMenuItem"
        Me.SurveyToolStripMenuItem.Size = New System.Drawing.Size(81, 29)
        Me.SurveyToolStripMenuItem.Text = "Survey"
        '
        'CreateSurveyImagesToolStripMenuItem
        '
        Me.CreateSurveyImagesToolStripMenuItem.Name = "CreateSurveyImagesToolStripMenuItem"
        Me.CreateSurveyImagesToolStripMenuItem.Size = New System.Drawing.Size(397, 34)
        Me.CreateSurveyImagesToolStripMenuItem.Text = "Create Survey Images and Rankings"
        '
        'ValidateAllTreeViewsToolStripMenuItem
        '
        Me.ValidateAllTreeViewsToolStripMenuItem.Name = "ValidateAllTreeViewsToolStripMenuItem"
        Me.ValidateAllTreeViewsToolStripMenuItem.Size = New System.Drawing.Size(425, 34)
        Me.ValidateAllTreeViewsToolStripMenuItem.Text = "Validate All TreeView's (Admin use only)"
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
        'ValidateTreeView
        '
        Me.ValidateTreeView.Interval = 500
        '
        'OpenCVB
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1786, 1069)
        Me.Controls.Add(Me.AvailableAlgorithms)
        Me.Controls.Add(Me.OpenCVkeyword)
        Me.Controls.Add(Me.AlgorithmDesc)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.KeyPreview = True
        Me.MainMenuStrip = Me.MenuStrip1
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
    Friend WithEvents SnapShotButton As ToolStripButton
    Friend WithEvents OpenCVkeyword As ComboBox
    Friend WithEvents ToolTip1 As ToolTip
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
    Friend WithEvents SurveyToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CreateSurveyImagesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents RefreshTimer As Timer
    Friend WithEvents ValidateAllTreeViewsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ValidateTreeView As Timer
End Class
