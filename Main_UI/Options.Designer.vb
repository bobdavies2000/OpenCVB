<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Options
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing And components IsNot Nothing Then
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
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.UpdateXRef = New System.Windows.Forms.Button()
        Me.TestAllDuration = New System.Windows.Forms.NumericUpDown()
        Me.DurationLabel = New System.Windows.Forms.Label()
        Me.fontInfo = New System.Windows.Forms.Label()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.showConsoleLog = New System.Windows.Forms.CheckBox()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.OKButton = New System.Windows.Forms.Button()
        Me.Cancel_Button = New System.Windows.Forms.Button()
        Me.FontDialog1 = New System.Windows.Forms.FontDialog()
        Me.CameraGroup = New System.Windows.Forms.FlowLayoutPanel()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.SnapCustom = New System.Windows.Forms.RadioButton()
        Me.Snap320 = New System.Windows.Forms.RadioButton()
        Me.Snap640 = New System.Windows.Forms.RadioButton()
        Me.Resolutions = New System.Windows.Forms.FlowLayoutPanel()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.FixedPalette = New System.Windows.Forms.CheckBox()
        Me.GroupBox2.SuspendLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox5.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.FixedPalette)
        Me.GroupBox2.Controls.Add(Me.Label7)
        Me.GroupBox2.Controls.Add(Me.Label6)
        Me.GroupBox2.Controls.Add(Me.UpdateXRef)
        Me.GroupBox2.Controls.Add(Me.TestAllDuration)
        Me.GroupBox2.Controls.Add(Me.DurationLabel)
        Me.GroupBox2.Controls.Add(Me.fontInfo)
        Me.GroupBox2.Controls.Add(Me.Button1)
        Me.GroupBox2.Controls.Add(Me.showConsoleLog)
        Me.GroupBox2.Location = New System.Drawing.Point(24, 543)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(962, 165)
        Me.GroupBox2.TabIndex = 5
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Other Global Settings"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(668, 52)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(185, 20)
        Me.Label7.TabIndex = 15
        Me.Label7.Text = "is only updated manually."
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(668, 32)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(239, 20)
        Me.Label6.TabIndex = 13
        Me.Label6.Text = "The algorithm cross-reference is "
        '
        'UpdateXRef
        '
        Me.UpdateXRef.Location = New System.Drawing.Point(422, 32)
        Me.UpdateXRef.Name = "UpdateXRef"
        Me.UpdateXRef.Size = New System.Drawing.Size(240, 38)
        Me.UpdateXRef.TabIndex = 12
        Me.UpdateXRef.Text = "Update the Algorithm XRef"
        Me.UpdateXRef.UseVisualStyleBackColor = True
        '
        'TestAllDuration
        '
        Me.TestAllDuration.Location = New System.Drawing.Point(19, 119)
        Me.TestAllDuration.Minimum = New Decimal(New Integer() {5, 0, 0, 0})
        Me.TestAllDuration.Name = "TestAllDuration"
        Me.TestAllDuration.ReadOnly = True
        Me.TestAllDuration.Size = New System.Drawing.Size(88, 26)
        Me.TestAllDuration.TabIndex = 8
        Me.TestAllDuration.Value = New Decimal(New Integer() {5, 0, 0, 0})
        '
        'DurationLabel
        '
        Me.DurationLabel.AutoSize = True
        Me.DurationLabel.Location = New System.Drawing.Point(114, 122)
        Me.DurationLabel.Name = "DurationLabel"
        Me.DurationLabel.Size = New System.Drawing.Size(620, 20)
        Me.DurationLabel.TabIndex = 7
        Me.DurationLabel.Text = "Duration in seconds of each test when running ""Test All"" (there is a 5 second min" &
    "imum)"
        '
        'fontInfo
        '
        Me.fontInfo.AutoSize = True
        Me.fontInfo.Location = New System.Drawing.Point(64, 75)
        Me.fontInfo.Name = "fontInfo"
        Me.fontInfo.Size = New System.Drawing.Size(255, 20)
        Me.fontInfo.TabIndex = 6
        Me.fontInfo.Text = "Select the font for all TrueType text"
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(14, 71)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(44, 29)
        Me.Button1.TabIndex = 5
        Me.Button1.Text = "..."
        Me.Button1.UseVisualStyleBackColor = True
        '
        'showConsoleLog
        '
        Me.showConsoleLog.AutoSize = True
        Me.showConsoleLog.Location = New System.Drawing.Point(16, 32)
        Me.showConsoleLog.Name = "showConsoleLog"
        Me.showConsoleLog.Size = New System.Drawing.Size(328, 24)
        Me.showConsoleLog.TabIndex = 2
        Me.showConsoleLog.Text = "Show Console Log for external processes"
        Me.showConsoleLog.UseVisualStyleBackColor = True
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'OKButton
        '
        Me.OKButton.Location = New System.Drawing.Point(850, 32)
        Me.OKButton.Name = "OKButton"
        Me.OKButton.Size = New System.Drawing.Size(142, 42)
        Me.OKButton.TabIndex = 10
        Me.OKButton.Text = "OK"
        Me.OKButton.UseVisualStyleBackColor = True
        '
        'Cancel_Button
        '
        Me.Cancel_Button.Location = New System.Drawing.Point(850, 83)
        Me.Cancel_Button.Name = "Cancel_Button"
        Me.Cancel_Button.Size = New System.Drawing.Size(142, 42)
        Me.Cancel_Button.TabIndex = 11
        Me.Cancel_Button.Text = "Cancel"
        Me.Cancel_Button.UseVisualStyleBackColor = True
        '
        'CameraGroup
        '
        Me.CameraGroup.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.CameraGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown
        Me.CameraGroup.Location = New System.Drawing.Point(24, 32)
        Me.CameraGroup.Name = "CameraGroup"
        Me.CameraGroup.Size = New System.Drawing.Size(821, 171)
        Me.CameraGroup.TabIndex = 13
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.Label3)
        Me.GroupBox5.Controls.Add(Me.Label2)
        Me.GroupBox5.Controls.Add(Me.Label1)
        Me.GroupBox5.Controls.Add(Me.SnapCustom)
        Me.GroupBox5.Controls.Add(Me.Snap320)
        Me.GroupBox5.Controls.Add(Me.Snap640)
        Me.GroupBox5.Location = New System.Drawing.Point(24, 438)
        Me.GroupBox5.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Padding = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.GroupBox5.Size = New System.Drawing.Size(962, 97)
        Me.GroupBox5.TabIndex = 14
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "Display Resolution"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(616, 57)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(188, 20)
        Me.Label3.TabIndex = 7
        Me.Label3.Text = "User-defined image sizes"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(322, 57)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(227, 20)
        Me.Label2.TabIndex = 6
        Me.Label2.Text = "320x240 or 320x180 per image"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(42, 57)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(227, 20)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "640x480 or 640x360 per image"
        '
        'SnapCustom
        '
        Me.SnapCustom.AutoSize = True
        Me.SnapCustom.Location = New System.Drawing.Point(591, 28)
        Me.SnapCustom.Name = "SnapCustom"
        Me.SnapCustom.Size = New System.Drawing.Size(271, 24)
        Me.SnapCustom.TabIndex = 4
        Me.SnapCustom.TabStop = True
        Me.SnapCustom.Text = "Custom - retain resized main form"
        Me.SnapCustom.UseVisualStyleBackColor = True
        '
        'Snap320
        '
        Me.Snap320.AutoSize = True
        Me.Snap320.Location = New System.Drawing.Point(298, 28)
        Me.Snap320.Name = "Snap320"
        Me.Snap320.Size = New System.Drawing.Size(245, 24)
        Me.Snap320.TabIndex = 3
        Me.Snap320.TabStop = True
        Me.Snap320.Text = "Autosized for Laptop Displays"
        Me.Snap320.UseVisualStyleBackColor = True
        '
        'Snap640
        '
        Me.Snap640.AutoSize = True
        Me.Snap640.Location = New System.Drawing.Point(14, 28)
        Me.Snap640.Name = "Snap640"
        Me.Snap640.Size = New System.Drawing.Size(255, 24)
        Me.Snap640.TabIndex = 2
        Me.Snap640.TabStop = True
        Me.Snap640.Text = "Autosized for Desktop Displays"
        Me.Snap640.UseVisualStyleBackColor = True
        '
        'Resolutions
        '
        Me.Resolutions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Resolutions.Location = New System.Drawing.Point(24, 229)
        Me.Resolutions.Name = "Resolutions"
        Me.Resolutions.Size = New System.Drawing.Size(821, 201)
        Me.Resolutions.TabIndex = 15
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(23, 9)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(135, 20)
        Me.Label4.TabIndex = 16
        Me.Label4.Text = "Camera Selection"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(20, 206)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(182, 20)
        Me.Label5.TabIndex = 17
        Me.Label5.Text = "Working Size Resolution"
        '
        'FixedPalette
        '
        Me.FixedPalette.AutoSize = True
        Me.FixedPalette.Checked = True
        Me.FixedPalette.CheckState = System.Windows.Forms.CheckState.Checked
        Me.FixedPalette.Location = New System.Drawing.Point(422, 86)
        Me.FixedPalette.Name = "FixedPalette"
        Me.FixedPalette.Size = New System.Drawing.Size(351, 24)
        Me.FixedPalette.TabIndex = 16
        Me.FixedPalette.Text = "On = fixed palette.  Off - random after restart."
        Me.FixedPalette.UseVisualStyleBackColor = True
        '
        'Options
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSize = True
        Me.ClientSize = New System.Drawing.Size(1008, 720)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Resolutions)
        Me.Controls.Add(Me.GroupBox5)
        Me.Controls.Add(Me.CameraGroup)
        Me.Controls.Add(Me.Cancel_Button)
        Me.Controls.Add(Me.OKButton)
        Me.Controls.Add(Me.GroupBox2)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.KeyPreview = True
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Name = "Options"
        Me.ShowInTaskbar = False
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show
        Me.Text = "OpenCVB Global Settings"
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

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
    Friend WithEvents TestAllDuration As NumericUpDown
    Friend WithEvents DurationLabel As Label
    Friend WithEvents fontInfo As Label
    Friend WithEvents Button1 As Button
    Friend WithEvents Label6 As Label
    Friend WithEvents UpdateXRef As Button
    Friend WithEvents Label7 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents FixedPalette As CheckBox
End Class
