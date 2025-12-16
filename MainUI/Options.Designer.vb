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
        FontDialog1 = New FontDialog()
        CameraGroup = New FlowLayoutPanel()
        Resolutions = New FlowLayoutPanel()
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
        ' Options
        ' 
        AutoScaleDimensions = New SizeF(12F, 30F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1210, 528)
        Controls.Add(Resolutions)
        Controls.Add(CameraGroup)
        Controls.Add(Cancel_Button)
        Controls.Add(OKButton)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(4)
        Name = "Options"
        Text = "OpenCVB Global Settings"
        ResumeLayout(False)
    End Sub

    Friend WithEvents OKButton As Button
    Friend WithEvents Cancel_Button As Button
    Friend WithEvents FontDialog1 As FontDialog
    Friend WithEvents CameraGroup As FlowLayoutPanel
    Friend WithEvents Resolutions As FlowLayoutPanel
End Class
