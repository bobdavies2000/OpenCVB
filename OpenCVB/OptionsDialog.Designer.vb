<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OptionsDialog
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
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.SnapToGrid = New System.Windows.Forms.CheckBox()
        Me.resolution1280 = New System.Windows.Forms.RadioButton()
        Me.resolution640 = New System.Windows.Forms.RadioButton()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.ShowConsoleLog = New System.Windows.Forms.CheckBox()
        Me.ShowLabels = New System.Windows.Forms.CheckBox()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.TestAllDuration = New System.Windows.Forms.NumericUpDown()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GroupBox6 = New System.Windows.Forms.GroupBox()
        Me.PythonExeName = New System.Windows.Forms.TextBox()
        Me.SelectPythonFile = New System.Windows.Forms.Button()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.OKButton = New System.Windows.Forms.Button()
        Me.Cancel_Button = New System.Windows.Forms.Button()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.fontInfo = New System.Windows.Forms.Label()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.FontDialog1 = New System.Windows.Forms.FontDialog()
        Me.CameraGroup = New System.Windows.Forms.FlowLayoutPanel()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox6.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.SnapToGrid)
        Me.GroupBox1.Controls.Add(Me.resolution1280)
        Me.GroupBox1.Controls.Add(Me.resolution640)
        Me.GroupBox1.Location = New System.Drawing.Point(21, 273)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(936, 105)
        Me.GroupBox1.TabIndex = 4
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Camera Input Resolution"
        '
        'SnapToGrid
        '
        Me.SnapToGrid.AutoSize = True
        Me.SnapToGrid.Location = New System.Drawing.Point(331, 37)
        Me.SnapToGrid.Name = "SnapToGrid"
        Me.SnapToGrid.Size = New System.Drawing.Size(414, 24)
        Me.SnapToGrid.TabIndex = 3
        Me.SnapToGrid.Text = "Snap to Grid - unchecked leaves main form size alone"
        Me.SnapToGrid.UseVisualStyleBackColor = True
        '
        'resolution1280
        '
        Me.resolution1280.AutoSize = True
        Me.resolution1280.Location = New System.Drawing.Point(18, 67)
        Me.resolution1280.Name = "resolution1280"
        Me.resolution1280.Size = New System.Drawing.Size(223, 24)
        Me.resolution1280.TabIndex = 1
        Me.resolution1280.TabStop = True
        Me.resolution1280.Text = "1280x720 - High resolution"
        Me.resolution1280.UseVisualStyleBackColor = True
        '
        'resolution640
        '
        Me.resolution640.AutoSize = True
        Me.resolution640.Location = New System.Drawing.Point(18, 37)
        Me.resolution640.Name = "resolution640"
        Me.resolution640.Size = New System.Drawing.Size(237, 24)
        Me.resolution640.TabIndex = 0
        Me.resolution640.TabStop = True
        Me.resolution640.Text = "640x480 - Medium resolution"
        Me.resolution640.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.ShowConsoleLog)
        Me.GroupBox2.Controls.Add(Me.ShowLabels)
        Me.GroupBox2.Location = New System.Drawing.Point(21, 389)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(936, 113)
        Me.GroupBox2.TabIndex = 5
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Global Options"
        '
        'ShowConsoleLog
        '
        Me.ShowConsoleLog.AutoSize = True
        Me.ShowConsoleLog.Location = New System.Drawing.Point(16, 70)
        Me.ShowConsoleLog.Name = "ShowConsoleLog"
        Me.ShowConsoleLog.Size = New System.Drawing.Size(630, 24)
        Me.ShowConsoleLog.TabIndex = 2
        Me.ShowConsoleLog.Text = "Show Console Log for external processes - external process messages will not show" &
    "."
        Me.ShowConsoleLog.UseVisualStyleBackColor = True
        '
        'ShowLabels
        '
        Me.ShowLabels.AutoSize = True
        Me.ShowLabels.Location = New System.Drawing.Point(16, 40)
        Me.ShowLabels.Name = "ShowLabels"
        Me.ShowLabels.Size = New System.Drawing.Size(175, 24)
        Me.ShowLabels.TabIndex = 1
        Me.ShowLabels.Text = "Show Image Labels"
        Me.ShowLabels.UseVisualStyleBackColor = True
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.TestAllDuration)
        Me.GroupBox4.Controls.Add(Me.Label1)
        Me.GroupBox4.Location = New System.Drawing.Point(21, 513)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(935, 99)
        Me.GroupBox4.TabIndex = 8
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Test All Options"
        '
        'TestAllDuration
        '
        Me.TestAllDuration.Location = New System.Drawing.Point(16, 50)
        Me.TestAllDuration.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.TestAllDuration.Name = "TestAllDuration"
        Me.TestAllDuration.ReadOnly = True
        Me.TestAllDuration.Size = New System.Drawing.Size(89, 26)
        Me.TestAllDuration.TabIndex = 2
        Me.TestAllDuration.Value = New Decimal(New Integer() {10, 0, 0, 0})
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(111, 52)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(620, 20)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Duration in seconds of each test when running ""Test All"" (there is a 5 second min" &
    "imum)"
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.PythonExeName)
        Me.GroupBox6.Controls.Add(Me.SelectPythonFile)
        Me.GroupBox6.Controls.Add(Me.Label2)
        Me.GroupBox6.Location = New System.Drawing.Point(21, 623)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(936, 123)
        Me.GroupBox6.TabIndex = 9
        Me.GroupBox6.TabStop = False
        Me.GroupBox6.Text = "Python "
        '
        'PythonExeName
        '
        Me.PythonExeName.Location = New System.Drawing.Point(64, 64)
        Me.PythonExeName.Name = "PythonExeName"
        Me.PythonExeName.Size = New System.Drawing.Size(869, 26)
        Me.PythonExeName.TabIndex = 4
        '
        'SelectPythonFile
        '
        Me.SelectPythonFile.Location = New System.Drawing.Point(15, 65)
        Me.SelectPythonFile.Name = "SelectPythonFile"
        Me.SelectPythonFile.Size = New System.Drawing.Size(43, 30)
        Me.SelectPythonFile.TabIndex = 3
        Me.SelectPythonFile.Text = "..."
        Me.SelectPythonFile.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(12, 37)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(552, 20)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Select the version of Python that should be used when running Python scripts"
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'OKButton
        '
        Me.OKButton.Location = New System.Drawing.Point(850, 33)
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
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.fontInfo)
        Me.GroupBox3.Controls.Add(Me.Button1)
        Me.GroupBox3.Controls.Add(Me.Label4)
        Me.GroupBox3.Location = New System.Drawing.Point(21, 762)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(936, 116)
        Me.GroupBox3.TabIndex = 12
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "TrueType Font "
        '
        'fontInfo
        '
        Me.fontInfo.AutoSize = True
        Me.fontInfo.Location = New System.Drawing.Point(72, 70)
        Me.fontInfo.Name = "fontInfo"
        Me.fontInfo.Size = New System.Drawing.Size(255, 20)
        Me.fontInfo.TabIndex = 4
        Me.fontInfo.Text = "Select the font for all TrueType text"
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(15, 68)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(43, 30)
        Me.Button1.TabIndex = 3
        Me.Button1.Text = "..."
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(12, 37)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(255, 20)
        Me.Label4.TabIndex = 1
        Me.Label4.Text = "Select the font for all TrueType text"
        '
        'CameraGroup
        '
        Me.CameraGroup.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.CameraGroup.FlowDirection = System.Windows.Forms.FlowDirection.TopDown
        Me.CameraGroup.Location = New System.Drawing.Point(24, 17)
        Me.CameraGroup.Name = "CameraGroup"
        Me.CameraGroup.Size = New System.Drawing.Size(821, 195)
        Me.CameraGroup.TabIndex = 13
        '
        'OptionsDialog
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1008, 887)
        Me.Controls.Add(Me.CameraGroup)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.Cancel_Button)
        Me.Controls.Add(Me.OKButton)
        Me.Controls.Add(Me.GroupBox6)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.KeyPreview = True
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.MinimizeBox = False
        Me.Name = "OptionsDialog"
        Me.ShowInTaskbar = False
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show
        Me.Text = "OpenCVB Global Settings"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox6.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents resolution1280 As RadioButton
    Friend WithEvents resolution640 As RadioButton
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents ShowLabels As CheckBox
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents TestAllDuration As NumericUpDown
    Friend WithEvents Label1 As Label
    Friend WithEvents GroupBox6 As GroupBox
    Friend WithEvents PythonExeName As TextBox
    Friend WithEvents SelectPythonFile As Button
    Friend WithEvents Label2 As Label
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents ShowConsoleLog As CheckBox
    Friend WithEvents OKButton As Button
    Friend WithEvents Cancel_Button As Button
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents Button1 As Button
    Friend WithEvents Label4 As Label
    Friend WithEvents fontInfo As Label
    Friend WithEvents FontDialog1 As FontDialog
    Friend WithEvents SnapToGrid As CheckBox
    Friend WithEvents CameraGroup As FlowLayoutPanel
End Class
