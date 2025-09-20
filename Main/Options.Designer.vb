<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Options
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Options))
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
        Label7 = New Label()
        TestAllDuration = New NumericUpDown()
        Label6 = New Label()
        Label5 = New Label()
        UpdateXRef = New Button()
        Label4 = New Label()
        Button1 = New Button()
        showBatchConsole = New CheckBox()
        FontDialog1 = New FontDialog()
        CameraGroup = New FlowLayoutPanel()
        Resolutions = New FlowLayoutPanel()
        DisplayResolution.SuspendLayout()
        GroupBox1.SuspendLayout()
        CType(TestAllDuration, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' OKButton
        ' 
        OKButton.Location = New Point(847, 18)
        OKButton.Name = "OKButton"
        OKButton.Size = New Size(137, 41)
        OKButton.TabIndex = 0
        OKButton.Text = "OK"
        OKButton.UseVisualStyleBackColor = True
        ' 
        ' Cancel_Button
        ' 
        Cancel_Button.Location = New Point(847, 65)
        Cancel_Button.Name = "Cancel_Button"
        Cancel_Button.Size = New Size(137, 41)
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
        DisplayResolution.Location = New Point(12, 438)
        DisplayResolution.Name = "DisplayResolution"
        DisplayResolution.Size = New Size(982, 107)
        DisplayResolution.TabIndex = 4
        DisplayResolution.TabStop = False
        DisplayResolution.Text = "Display Resolution"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Font = New Font("Segoe UI", 8F)
        Label3.Location = New Point(647, 63)
        Label3.Name = "Label3"
        Label3.Size = New Size(184, 21)
        Label3.TabIndex = 5
        Label3.Text = "User-defined image sizes"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Font = New Font("Segoe UI", 8F)
        Label2.Location = New Point(358, 63)
        Label2.Name = "Label2"
        Label2.Size = New Size(229, 21)
        Label2.TabIndex = 4
        Label2.Text = "320x240 or 320x180 per image"
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Font = New Font("Segoe UI", 8F)
        Label1.Location = New Point(41, 63)
        Label1.Name = "Label1"
        Label1.Size = New Size(229, 21)
        Label1.TabIndex = 3
        Label1.Text = "640x480 or 640x360 per image"
        ' 
        ' SnapCustom
        ' 
        SnapCustom.AutoSize = True
        SnapCustom.Font = New Font("Segoe UI", 8F)
        SnapCustom.Location = New Point(624, 31)
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
        Snap320.Location = New Point(334, 31)
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
        Snap640.Location = New Point(14, 31)
        Snap640.Name = "Snap640"
        Snap640.Size = New Size(250, 25)
        Snap640.TabIndex = 0
        Snap640.TabStop = True
        Snap640.Text = "Autosized for Desktop Displays"
        Snap640.UseVisualStyleBackColor = True
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(Label7)
        GroupBox1.Controls.Add(TestAllDuration)
        GroupBox1.Controls.Add(Label6)
        GroupBox1.Controls.Add(Label5)
        GroupBox1.Controls.Add(UpdateXRef)
        GroupBox1.Controls.Add(Label4)
        GroupBox1.Controls.Add(Button1)
        GroupBox1.Controls.Add(showBatchConsole)
        GroupBox1.Location = New Point(12, 551)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(977, 187)
        GroupBox1.TabIndex = 5
        GroupBox1.TabStop = False
        GroupBox1.Text = "Other Global Settings"
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(95, 143)
        Label7.Name = "Label7"
        Label7.Size = New Size(698, 25)
        Label7.TabIndex = 7
        Label7.Text = "Duration in seconds of each test when running ""Test All"" (there is a 5 second minimum)"
        ' 
        ' TestAllDuration
        ' 
        TestAllDuration.Location = New Point(10, 141)
        TestAllDuration.Minimum = New Decimal(New Integer() {5, 0, 0, 0})
        TestAllDuration.Name = "TestAllDuration"
        TestAllDuration.Size = New Size(76, 31)
        TestAllDuration.TabIndex = 6
        TestAllDuration.Value = New Decimal(New Integer() {5, 0, 0, 0})
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(695, 59)
        Label6.Name = "Label6"
        Label6.Size = New Size(215, 25)
        Label6.TabIndex = 5
        Label6.Text = "is only updated manually."
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(693, 34)
        Label5.Name = "Label5"
        Label5.Size = New Size(269, 25)
        Label5.TabIndex = 4
        Label5.Text = "The algorithm cross-reference is "
        ' 
        ' UpdateXRef
        ' 
        UpdateXRef.Location = New Point(408, 34)
        UpdateXRef.Name = "UpdateXRef"
        UpdateXRef.Size = New Size(277, 35)
        UpdateXRef.TabIndex = 3
        UpdateXRef.Text = "Update the Algorithm XRef"
        UpdateXRef.UseVisualStyleBackColor = True
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(64, 94)
        Label4.Name = "Label4"
        Label4.Size = New Size(284, 25)
        Label4.TabIndex = 2
        Label4.Text = "Select the font for all TrueType text"
        ' 
        ' Button1
        ' 
        Button1.Location = New Point(6, 90)
        Button1.Name = "Button1"
        Button1.Size = New Size(52, 33)
        Button1.TabIndex = 1
        Button1.Text = "..."
        Button1.UseVisualStyleBackColor = True
        ' 
        ' showBatchConsole
        ' 
        showBatchConsole.AutoSize = True
        showBatchConsole.Location = New Point(12, 40)
        showBatchConsole.Name = "showBatchConsole"
        showBatchConsole.Size = New Size(363, 29)
        showBatchConsole.TabIndex = 0
        showBatchConsole.Text = "Show Console Log for external processes"
        showBatchConsole.UseVisualStyleBackColor = True
        ' 
        ' CameraGroup
        ' 
        CameraGroup.FlowDirection = FlowDirection.TopDown
        CameraGroup.Location = New Point(13, 25)
        CameraGroup.Name = "CameraGroup"
        CameraGroup.Size = New Size(827, 159)
        CameraGroup.TabIndex = 6
        ' 
        ' Resolutions
        ' 
        Resolutions.Location = New Point(12, 190)
        Resolutions.Name = "Resolutions"
        Resolutions.Size = New Size(828, 242)
        Resolutions.TabIndex = 7
        ' 
        ' Options
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1008, 750)
        Controls.Add(Resolutions)
        Controls.Add(CameraGroup)
        Controls.Add(GroupBox1)
        Controls.Add(DisplayResolution)
        Controls.Add(Cancel_Button)
        Controls.Add(OKButton)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "Options"
        Text = "OpenCVB Global Settings"
        DisplayResolution.ResumeLayout(False)
        DisplayResolution.PerformLayout()
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        CType(TestAllDuration, ComponentModel.ISupportInitialize).EndInit()
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
    Friend WithEvents UpdateXRef As Button
    Friend WithEvents Label6 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents TestAllDuration As NumericUpDown
    Friend WithEvents Label7 As Label
    Friend WithEvents FontDialog1 As FontDialog
    Friend WithEvents CameraGroup As FlowLayoutPanel
    Friend WithEvents Resolutions As FlowLayoutPanel
End Class
