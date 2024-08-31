<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainOptions
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
        GroupBox2 = New GroupBox()
        Label6 = New Label()
        UpdateXRef = New Button()
        TestAllDuration = New NumericUpDown()
        DurationLabel = New Label()
        fontInfo = New Label()
        Button1 = New Button()
        showConsoleLog = New CheckBox()
        OpenFileDialog1 = New OpenFileDialog()
        OKButton = New Button()
        Cancel_Button = New Button()
        FontDialog1 = New FontDialog()
        CameraGroup = New FlowLayoutPanel()
        GroupBox5 = New GroupBox()
        Label3 = New Label()
        Label2 = New Label()
        Label1 = New Label()
        SnapCustom = New RadioButton()
        Snap320 = New RadioButton()
        Snap640 = New RadioButton()
        Resolutions = New FlowLayoutPanel()
        Label4 = New Label()
        Label5 = New Label()
        TextBox1 = New TextBox()
        Label7 = New Label()
        GroupBox2.SuspendLayout()
        CType(TestAllDuration, ComponentModel.ISupportInitialize).BeginInit()
        GroupBox5.SuspendLayout()
        SuspendLayout()
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Controls.Add(Label7)
        GroupBox2.Controls.Add(TextBox1)
        GroupBox2.Controls.Add(Label6)
        GroupBox2.Controls.Add(UpdateXRef)
        GroupBox2.Controls.Add(TestAllDuration)
        GroupBox2.Controls.Add(DurationLabel)
        GroupBox2.Controls.Add(fontInfo)
        GroupBox2.Controls.Add(Button1)
        GroupBox2.Controls.Add(showConsoleLog)
        GroupBox2.Location = New Point(27, 556)
        GroupBox2.Margin = New Padding(3, 4, 3, 4)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Padding = New Padding(3, 4, 3, 4)
        GroupBox2.Size = New Size(1069, 206)
        GroupBox2.TabIndex = 5
        GroupBox2.TabStop = False
        GroupBox2.Text = "Other Global Settings"
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(742, 40)
        Label6.Name = "Label6"
        Label6.Size = New Size(269, 25)
        Label6.TabIndex = 13
        Label6.Text = "The algorithm cross-reference is "
        ' 
        ' UpdateXRef
        ' 
        UpdateXRef.Location = New Point(469, 40)
        UpdateXRef.Margin = New Padding(3, 4, 3, 4)
        UpdateXRef.Name = "UpdateXRef"
        UpdateXRef.Size = New Size(267, 48)
        UpdateXRef.TabIndex = 12
        UpdateXRef.Text = "Update the Algorithm XRef"
        UpdateXRef.UseVisualStyleBackColor = True
        ' 
        ' TestAllDuration
        ' 
        TestAllDuration.Location = New Point(21, 149)
        TestAllDuration.Margin = New Padding(3, 4, 3, 4)
        TestAllDuration.Minimum = New Decimal(New Integer() {5, 0, 0, 0})
        TestAllDuration.Name = "TestAllDuration"
        TestAllDuration.ReadOnly = True
        TestAllDuration.Size = New Size(98, 31)
        TestAllDuration.TabIndex = 8
        TestAllDuration.Value = New Decimal(New Integer() {5, 0, 0, 0})
        ' 
        ' DurationLabel
        ' 
        DurationLabel.AutoSize = True
        DurationLabel.Location = New Point(127, 152)
        DurationLabel.Name = "DurationLabel"
        DurationLabel.Size = New Size(698, 25)
        DurationLabel.TabIndex = 7
        DurationLabel.Text = "Duration in seconds of each test when running ""Test All"" (there is a 5 second minimum)"
        ' 
        ' fontInfo
        ' 
        fontInfo.AutoSize = True
        fontInfo.Location = New Point(71, 94)
        fontInfo.Name = "fontInfo"
        fontInfo.Size = New Size(284, 25)
        fontInfo.TabIndex = 6
        fontInfo.Text = "Select the font for all TrueType text"
        ' 
        ' Button1
        ' 
        Button1.Location = New Point(16, 89)
        Button1.Margin = New Padding(3, 4, 3, 4)
        Button1.Name = "Button1"
        Button1.Size = New Size(49, 36)
        Button1.TabIndex = 5
        Button1.Text = "..."
        Button1.UseVisualStyleBackColor = True
        ' 
        ' showConsoleLog
        ' 
        showConsoleLog.AutoSize = True
        showConsoleLog.Location = New Point(18, 40)
        showConsoleLog.Margin = New Padding(3, 4, 3, 4)
        showConsoleLog.Name = "showConsoleLog"
        showConsoleLog.Size = New Size(363, 29)
        showConsoleLog.TabIndex = 2
        showConsoleLog.Text = "Show Console Log for external processes"
        showConsoleLog.UseVisualStyleBackColor = True
        ' 
        ' OpenFileDialog1
        ' 
        OpenFileDialog1.FileName = "OpenFileDialog1"
        ' 
        ' OKButton
        ' 
        OKButton.Location = New Point(944, 40)
        OKButton.Margin = New Padding(3, 4, 3, 4)
        OKButton.Name = "OKButton"
        OKButton.Size = New Size(158, 52)
        OKButton.TabIndex = 10
        OKButton.Text = "OK"
        OKButton.UseVisualStyleBackColor = True
        ' 
        ' Cancel_Button
        ' 
        Cancel_Button.Location = New Point(944, 104)
        Cancel_Button.Margin = New Padding(3, 4, 3, 4)
        Cancel_Button.Name = "Cancel_Button"
        Cancel_Button.Size = New Size(158, 52)
        Cancel_Button.TabIndex = 11
        Cancel_Button.Text = "Cancel"
        Cancel_Button.UseVisualStyleBackColor = True
        ' 
        ' CameraGroup
        ' 
        CameraGroup.BorderStyle = BorderStyle.FixedSingle
        CameraGroup.FlowDirection = FlowDirection.TopDown
        CameraGroup.Location = New Point(27, 40)
        CameraGroup.Margin = New Padding(3, 4, 3, 4)
        CameraGroup.Name = "CameraGroup"
        CameraGroup.Size = New Size(912, 154)
        CameraGroup.TabIndex = 13
        ' 
        ' GroupBox5
        ' 
        GroupBox5.Controls.Add(Label3)
        GroupBox5.Controls.Add(Label2)
        GroupBox5.Controls.Add(Label1)
        GroupBox5.Controls.Add(SnapCustom)
        GroupBox5.Controls.Add(Snap320)
        GroupBox5.Controls.Add(Snap640)
        GroupBox5.Location = New Point(27, 425)
        GroupBox5.Margin = New Padding(4, 6, 4, 6)
        GroupBox5.Name = "GroupBox5"
        GroupBox5.Padding = New Padding(4, 6, 4, 6)
        GroupBox5.Size = New Size(1069, 121)
        GroupBox5.TabIndex = 14
        GroupBox5.TabStop = False
        GroupBox5.Text = "Display Resolution"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(684, 71)
        Label3.Name = "Label3"
        Label3.Size = New Size(210, 25)
        Label3.TabIndex = 7
        Label3.Text = "User-defined image sizes"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(358, 71)
        Label2.Name = "Label2"
        Label2.Size = New Size(260, 25)
        Label2.TabIndex = 6
        Label2.Text = "320x240 or 320x180 per image"
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(47, 71)
        Label1.Name = "Label1"
        Label1.Size = New Size(260, 25)
        Label1.TabIndex = 5
        Label1.Text = "640x480 or 640x360 per image"
        ' 
        ' SnapCustom
        ' 
        SnapCustom.AutoSize = True
        SnapCustom.Location = New Point(657, 35)
        SnapCustom.Margin = New Padding(3, 4, 3, 4)
        SnapCustom.Name = "SnapCustom"
        SnapCustom.Size = New Size(308, 29)
        SnapCustom.TabIndex = 4
        SnapCustom.TabStop = True
        SnapCustom.Text = "Custom - retain resized main form"
        SnapCustom.UseVisualStyleBackColor = True
        ' 
        ' Snap320
        ' 
        Snap320.AutoSize = True
        Snap320.Location = New Point(331, 35)
        Snap320.Margin = New Padding(3, 4, 3, 4)
        Snap320.Name = "Snap320"
        Snap320.Size = New Size(276, 29)
        Snap320.TabIndex = 3
        Snap320.TabStop = True
        Snap320.Text = "Autosized for Laptop Displays"
        Snap320.UseVisualStyleBackColor = True
        ' 
        ' Snap640
        ' 
        Snap640.AutoSize = True
        Snap640.Location = New Point(16, 35)
        Snap640.Margin = New Padding(3, 4, 3, 4)
        Snap640.Name = "Snap640"
        Snap640.Size = New Size(287, 29)
        Snap640.TabIndex = 2
        Snap640.TabStop = True
        Snap640.Text = "Autosized for Desktop Displays"
        Snap640.UseVisualStyleBackColor = True
        ' 
        ' Resolutions
        ' 
        Resolutions.BorderStyle = BorderStyle.FixedSingle
        Resolutions.Location = New Point(27, 230)
        Resolutions.Margin = New Padding(3, 4, 3, 4)
        Resolutions.Name = "Resolutions"
        Resolutions.Size = New Size(912, 184)
        Resolutions.TabIndex = 15
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(26, 11)
        Label4.Name = "Label4"
        Label4.Size = New Size(148, 25)
        Label4.TabIndex = 16
        Label4.Text = "Camera Selection"
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(26, 199)
        Label5.Name = "Label5"
        Label5.Size = New Size(203, 25)
        Label5.TabIndex = 17
        Label5.Text = "Working Size Resolution"
        ' 
        ' TextBox1
        ' 
        TextBox1.Location = New Point(958, 116)
        TextBox1.Name = "TextBox1"
        TextBox1.Size = New Size(150, 31)
        TextBox1.TabIndex = 14
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(742, 65)
        Label7.Name = "Label7"
        Label7.Size = New Size(215, 25)
        Label7.TabIndex = 15
        Label7.Text = "is only updated manually."
        ' 
        ' MainOptions
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        AutoSize = True
        ClientSize = New Size(1120, 770)
        Controls.Add(Label5)
        Controls.Add(Label4)
        Controls.Add(Resolutions)
        Controls.Add(GroupBox5)
        Controls.Add(CameraGroup)
        Controls.Add(Cancel_Button)
        Controls.Add(OKButton)
        Controls.Add(GroupBox2)
        FormBorderStyle = FormBorderStyle.FixedDialog
        KeyPreview = True
        Margin = New Padding(4, 6, 4, 6)
        Name = "MainOptions"
        ShowInTaskbar = False
        SizeGripStyle = SizeGripStyle.Show
        Text = "OpenCVB Global Settings"
        GroupBox2.ResumeLayout(False)
        GroupBox2.PerformLayout()
        CType(TestAllDuration, ComponentModel.ISupportInitialize).EndInit()
        GroupBox5.ResumeLayout(False)
        GroupBox5.PerformLayout()
        ResumeLayout(False)
        PerformLayout()

    End Sub
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents showConsoleLog As CheckBox
    Friend WithEvents OKButton As Button
    Friend WithEvents Cancel_Button As Button
    Friend WithEvents FontDialog1 As FontDialog
    Friend WithEvents CameraGroup As FlowLayoutPanel
    Friend WithEvents GroupBox5 As GroupBox
    Friend WithEvents Snap640 As RadioButton
    Friend WithEvents SnapCustom As RadioButton
    Friend WithEvents Snap320 As RadioButton
    Friend WithEvents Label3 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents Resolutions As FlowLayoutPanel
    Friend WithEvents Label4 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents TestAllDuration As NumericUpDown
    Friend WithEvents DurationLabel As Label
    Friend WithEvents fontInfo As Label
    Friend WithEvents Button1 As Button
    Friend WithEvents Label6 As Label
    Friend WithEvents UpdateXRef As Button
    Friend WithEvents Label7 As Label
    Friend WithEvents TextBox1 As TextBox
End Class
