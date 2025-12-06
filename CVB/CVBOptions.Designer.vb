<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class CVBOptions
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(CVBOptions))
        OKButton = New Button()
        Cancel_Button = New Button()
        GroupBox1 = New GroupBox()
        Label4 = New Label()
        Button1 = New Button()
        showBatchConsole = New CheckBox()
        FontDialog1 = New FontDialog()
        CameraGroup = New FlowLayoutPanel()
        Resolutions = New FlowLayoutPanel()
        GroupBox1.SuspendLayout()
        SuspendLayout()
        ' 
        ' OKButton
        ' 
        OKButton.Location = New Point(1016, 22)
        OKButton.Margin = New Padding(4)
        OKButton.Name = "OKButton"
        OKButton.Size = New Size(164, 49)
        OKButton.TabIndex = 0
        OKButton.Text = "OK"
        OKButton.UseVisualStyleBackColor = True
        ' 
        ' Cancel_Button
        ' 
        Cancel_Button.Location = New Point(1016, 78)
        Cancel_Button.Margin = New Padding(4)
        Cancel_Button.Name = "Cancel_Button"
        Cancel_Button.Size = New Size(164, 49)
        Cancel_Button.TabIndex = 1
        Cancel_Button.Text = "Cancel"
        Cancel_Button.UseVisualStyleBackColor = True
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(Label4)
        GroupBox1.Controls.Add(Button1)
        GroupBox1.Controls.Add(showBatchConsole)
        GroupBox1.Location = New Point(16, 526)
        GroupBox1.Margin = New Padding(4)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Padding = New Padding(4)
        GroupBox1.Size = New Size(1172, 104)
        GroupBox1.TabIndex = 5
        GroupBox1.TabStop = False
        GroupBox1.Text = "Other Global Settings"
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(643, 49)
        Label4.Margin = New Padding(4, 0, 4, 0)
        Label4.Name = "Label4"
        Label4.Size = New Size(351, 30)
        Label4.TabIndex = 2
        Label4.Text = "Select the font for all TrueType text"
        ' 
        ' Button1
        ' 
        Button1.Location = New Point(573, 44)
        Button1.Margin = New Padding(4)
        Button1.Name = "Button1"
        Button1.Size = New Size(62, 40)
        Button1.TabIndex = 1
        Button1.Text = "..."
        Button1.UseVisualStyleBackColor = True
        ' 
        ' showBatchConsole
        ' 
        showBatchConsole.AutoSize = True
        showBatchConsole.Location = New Point(14, 48)
        showBatchConsole.Margin = New Padding(4)
        showBatchConsole.Name = "showBatchConsole"
        showBatchConsole.Size = New Size(436, 34)
        showBatchConsole.TabIndex = 0
        showBatchConsole.Text = "Show Console Log for external processes"
        showBatchConsole.UseVisualStyleBackColor = True
        ' 
        ' CameraGroup
        ' 
        CameraGroup.FlowDirection = FlowDirection.TopDown
        CameraGroup.Location = New Point(16, 30)
        CameraGroup.Margin = New Padding(4)
        CameraGroup.Name = "CameraGroup"
        CameraGroup.Size = New Size(992, 191)
        CameraGroup.TabIndex = 6
        ' 
        ' Resolutions
        ' 
        Resolutions.Location = New Point(14, 228)
        Resolutions.Margin = New Padding(4)
        Resolutions.Name = "Resolutions"
        Resolutions.Size = New Size(994, 290)
        Resolutions.TabIndex = 7
        ' 
        ' CVBOptions
        ' 
        AutoScaleDimensions = New SizeF(12F, 30F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1210, 637)
        Controls.Add(Resolutions)
        Controls.Add(CameraGroup)
        Controls.Add(GroupBox1)
        Controls.Add(Cancel_Button)
        Controls.Add(OKButton)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(4)
        Name = "CVBOptions"
        Text = "OpenCVB Global Settings"
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents OKButton As Button
    Friend WithEvents Cancel_Button As Button
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents showBatchConsole As CheckBox
    Friend WithEvents Button1 As Button
    Friend WithEvents Label4 As Label
    Friend WithEvents FontDialog1 As FontDialog
    Friend WithEvents CameraGroup As FlowLayoutPanel
    Friend WithEvents Resolutions As FlowLayoutPanel
End Class
