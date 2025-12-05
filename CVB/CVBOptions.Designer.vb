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
        DisplayResolution = New GroupBox()
        Label3 = New Label()
        Label2 = New Label()
        Label1 = New Label()
        SnapCustom = New RadioButton()
        Snap320 = New RadioButton()
        Snap640 = New RadioButton()
        GroupBox1 = New GroupBox()
        Label4 = New Label()
        Button1 = New Button()
        showBatchConsole = New CheckBox()
        FontDialog1 = New FontDialog()
        CameraGroup = New FlowLayoutPanel()
        Resolutions = New FlowLayoutPanel()
        DisplayResolution.SuspendLayout()
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
        ' DisplayResolution
        ' 
        DisplayResolution.Controls.Add(Label3)
        DisplayResolution.Controls.Add(Label2)
        DisplayResolution.Controls.Add(Label1)
        DisplayResolution.Controls.Add(SnapCustom)
        DisplayResolution.Controls.Add(Snap320)
        DisplayResolution.Controls.Add(Snap640)
        DisplayResolution.Location = New Point(14, 526)
        DisplayResolution.Margin = New Padding(4)
        DisplayResolution.Name = "DisplayResolution"
        DisplayResolution.Padding = New Padding(4)
        DisplayResolution.Size = New Size(1178, 128)
        DisplayResolution.TabIndex = 4
        DisplayResolution.TabStop = False
        DisplayResolution.Text = "Display Resolution"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Font = New Font("Segoe UI", 8F)
        Label3.Location = New Point(776, 76)
        Label3.Margin = New Padding(4, 0, 4, 0)
        Label3.Name = "Label3"
        Label3.Size = New Size(184, 21)
        Label3.TabIndex = 5
        Label3.Text = "User-defined image sizes"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Font = New Font("Segoe UI", 8F)
        Label2.Location = New Point(430, 76)
        Label2.Margin = New Padding(4, 0, 4, 0)
        Label2.Name = "Label2"
        Label2.Size = New Size(229, 21)
        Label2.TabIndex = 4
        Label2.Text = "320x240 or 320x180 per image"
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Font = New Font("Segoe UI", 8F)
        Label1.Location = New Point(49, 76)
        Label1.Margin = New Padding(4, 0, 4, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(229, 21)
        Label1.TabIndex = 3
        Label1.Text = "640x480 or 640x360 per image"
        ' 
        ' SnapCustom
        ' 
        SnapCustom.AutoSize = True
        SnapCustom.Font = New Font("Segoe UI", 8F)
        SnapCustom.Location = New Point(749, 37)
        SnapCustom.Margin = New Padding(4)
        SnapCustom.Name = "SnapCustom"
        SnapCustom.Size = New Size(273, 25)
        SnapCustom.TabIndex = 2
        SnapCustom.TabStop = True
        SnapCustom.Text = "Custom - retain resized main form"
        SnapCustom.UseVisualStyleBackColor = True
        ' 
        ' Snap320
        ' 
        Snap320.AutoSize = True
        Snap320.Font = New Font("Segoe UI", 8F)
        Snap320.Location = New Point(401, 37)
        Snap320.Margin = New Padding(4)
        Snap320.Name = "Snap320"
        Snap320.Size = New Size(241, 25)
        Snap320.TabIndex = 1
        Snap320.TabStop = True
        Snap320.Text = "Autosized for Laptop Displays"
        Snap320.UseVisualStyleBackColor = True
        ' 
        ' Snap640
        ' 
        Snap640.AutoSize = True
        Snap640.Font = New Font("Segoe UI", 8F)
        Snap640.Location = New Point(17, 37)
        Snap640.Margin = New Padding(4)
        Snap640.Name = "Snap640"
        Snap640.Size = New Size(250, 25)
        Snap640.TabIndex = 0
        Snap640.TabStop = True
        Snap640.Text = "Autosized for Desktop Displays"
        Snap640.UseVisualStyleBackColor = True
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(Label4)
        GroupBox1.Controls.Add(Button1)
        GroupBox1.Controls.Add(showBatchConsole)
        GroupBox1.Location = New Point(14, 661)
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
        ClientSize = New Size(1210, 772)
        Controls.Add(Resolutions)
        Controls.Add(CameraGroup)
        Controls.Add(GroupBox1)
        Controls.Add(DisplayResolution)
        Controls.Add(Cancel_Button)
        Controls.Add(OKButton)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(4)
        Name = "CVBOptions"
        Text = "OpenCVB Global Settings"
        DisplayResolution.ResumeLayout(False)
        DisplayResolution.PerformLayout()
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents OKButton As Button
    Friend WithEvents Cancel_Button As Button
    Friend WithEvents DisplayResolution As GroupBox
    Friend WithEvents SnapCustom As RadioButton
    Friend WithEvents Snap320 As RadioButton
    Friend WithEvents Snap640 As RadioButton
    Friend WithEvents Label1 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents showBatchConsole As CheckBox
    Friend WithEvents Button1 As Button
    Friend WithEvents Label4 As Label
    Friend WithEvents FontDialog1 As FontDialog
    Friend WithEvents CameraGroup As FlowLayoutPanel
    Friend WithEvents Resolutions As FlowLayoutPanel
End Class
