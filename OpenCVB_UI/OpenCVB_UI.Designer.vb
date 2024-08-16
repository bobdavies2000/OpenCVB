<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OpenCVB_UI
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OpenCVB_UI))
        ToolStrip1 = New ToolStrip()
        ToolStripButton1 = New ToolStripButton()
        ToolStripButton2 = New ToolStripButton()
        PausePlayButton = New ToolStripButton()
        OptionsButton = New ToolStripButton()
        TestAllButton = New ToolStripButton()
        TreeButton = New ToolStripButton()
        ToolStripButton7 = New ToolStripButton()
        BluePlusButton = New ToolStripButton()
        ComplexityButton = New ToolStripButton()
        ToolStripButton10 = New ToolStripButton()
        Advice = New ToolStripButton()
        ToolStripButton12 = New ToolStripButton()
        AvailableAlgorithms = New ToolStripComboBox()
        GroupName = New ToolStripComboBox()
        AlgorithmDesc = New TextBox()
        fpsTimer = New Timer(components)
        TestAllTimer = New Timer(components)
        ComplexityTimer = New Timer(components)
        XYLoc = New Label()
        ToolStrip1.SuspendLayout()
        SuspendLayout()
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.ImageScalingSize = New Size(24, 24)
        ToolStrip1.Items.AddRange(New ToolStripItem() {ToolStripButton1, ToolStripButton2, PausePlayButton, OptionsButton, TestAllButton, TreeButton, ToolStripButton7, BluePlusButton, ComplexityButton, ToolStripButton10, Advice, ToolStripButton12, AvailableAlgorithms, GroupName})
        ToolStrip1.Location = New Point(0, 0)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.Size = New Size(1556, 34)
        ToolStrip1.TabIndex = 0
        ToolStrip1.Text = "ToolStrip1"
        ' 
        ' ToolStripButton1
        ' 
        ToolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Image
        ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), Image)
        ToolStripButton1.ImageTransparentColor = Color.Magenta
        ToolStripButton1.Name = "ToolStripButton1"
        ToolStripButton1.Size = New Size(34, 29)
        ToolStripButton1.Text = "ToolStripButton1"
        ' 
        ' ToolStripButton2
        ' 
        ToolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Image
        ToolStripButton2.Image = CType(resources.GetObject("ToolStripButton2.Image"), Image)
        ToolStripButton2.ImageTransparentColor = Color.Magenta
        ToolStripButton2.Name = "ToolStripButton2"
        ToolStripButton2.Size = New Size(34, 29)
        ToolStripButton2.Text = "ToolStripButton2"
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
        TreeButton.Text = "ToolStripButton6"
        ' 
        ' ToolStripButton7
        ' 
        ToolStripButton7.DisplayStyle = ToolStripItemDisplayStyle.Image
        ToolStripButton7.Image = CType(resources.GetObject("ToolStripButton7.Image"), Image)
        ToolStripButton7.ImageTransparentColor = Color.Magenta
        ToolStripButton7.Name = "ToolStripButton7"
        ToolStripButton7.Size = New Size(34, 29)
        ToolStripButton7.Text = "ToolStripButton7"
        ' 
        ' BluePlusButton
        ' 
        BluePlusButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        BluePlusButton.Image = CType(resources.GetObject("BluePlusButton.Image"), Image)
        BluePlusButton.ImageTransparentColor = Color.Magenta
        BluePlusButton.Name = "BluePlusButton"
        BluePlusButton.Size = New Size(34, 29)
        BluePlusButton.Text = "ToolStripButton8"
        ' 
        ' ComplexityButton
        ' 
        ComplexityButton.DisplayStyle = ToolStripItemDisplayStyle.Image
        ComplexityButton.Image = CType(resources.GetObject("ComplexityButton.Image"), Image)
        ComplexityButton.ImageTransparentColor = Color.Magenta
        ComplexityButton.Name = "ComplexityButton"
        ComplexityButton.Size = New Size(34, 29)
        ComplexityButton.Text = "ToolStripButton9"
        ' 
        ' ToolStripButton10
        ' 
        ToolStripButton10.DisplayStyle = ToolStripItemDisplayStyle.Image
        ToolStripButton10.Image = CType(resources.GetObject("ToolStripButton10.Image"), Image)
        ToolStripButton10.ImageTransparentColor = Color.Magenta
        ToolStripButton10.Name = "ToolStripButton10"
        ToolStripButton10.Size = New Size(34, 29)
        ToolStripButton10.Text = "ToolStripButton10"
        ' 
        ' Advice
        ' 
        Advice.DisplayStyle = ToolStripItemDisplayStyle.Image
        Advice.Image = CType(resources.GetObject("Advice.Image"), Image)
        Advice.ImageTransparentColor = Color.Magenta
        Advice.Name = "Advice"
        Advice.Size = New Size(34, 29)
        Advice.Text = "ToolStripButton11"
        ' 
        ' ToolStripButton12
        ' 
        ToolStripButton12.DisplayStyle = ToolStripItemDisplayStyle.Text
        ToolStripButton12.Image = CType(resources.GetObject("ToolStripButton12.Image"), Image)
        ToolStripButton12.ImageTransparentColor = Color.Magenta
        ToolStripButton12.Name = "ToolStripButton12"
        ToolStripButton12.Size = New Size(68, 29)
        ToolStripButton12.Text = "Recent"
        ' 
        ' AvailableAlgorithms
        ' 
        AvailableAlgorithms.Name = "AvailableAlgorithms"
        AvailableAlgorithms.Size = New Size(300, 34)
        ' 
        ' GroupName
        ' 
        GroupName.Name = "GroupName"
        GroupName.Size = New Size(300, 34)
        ' 
        ' AlgorithmDesc
        ' 
        AlgorithmDesc.Location = New Point(1066, 37)
        AlgorithmDesc.Multiline = True
        AlgorithmDesc.Name = "AlgorithmDesc"
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
        ' OpenCVB_UI
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1556, 935)
        Controls.Add(XYLoc)
        Controls.Add(AlgorithmDesc)
        Controls.Add(ToolStrip1)
        Name = "OpenCVB_UI"
        Text = "OpenCVB Main Form"
        ToolStrip1.ResumeLayout(False)
        ToolStrip1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents ToolStripButton1 As ToolStripButton
    Friend WithEvents ToolStripButton2 As ToolStripButton
    Friend WithEvents PausePlayButton As ToolStripButton
    Friend WithEvents OptionsButton As ToolStripButton
    Friend WithEvents TestAllButton As ToolStripButton
    Friend WithEvents TreeButton As ToolStripButton
    Friend WithEvents ToolStripButton7 As ToolStripButton
    Friend WithEvents BluePlusButton As ToolStripButton
    Friend WithEvents ComplexityButton As ToolStripButton
    Friend WithEvents ToolStripButton10 As ToolStripButton
    Friend WithEvents Advice As ToolStripButton
    Friend WithEvents ToolStripButton12 As ToolStripButton
    Friend WithEvents AvailableAlgorithms As ToolStripComboBox
    Friend WithEvents GroupName As ToolStripComboBox
    Friend WithEvents AlgorithmDesc As TextBox
    Friend WithEvents fpsTimer As Timer
    Friend WithEvents TestAllTimer As Timer
    Friend WithEvents ComplexityTimer As Timer
    Friend WithEvents XYLoc As Label

End Class
