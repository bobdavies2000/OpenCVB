<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsCombo
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
        Me.Box = New System.Windows.Forms.ComboBox()
        Me.ComboLabel = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'Box
        '
        Me.Box.FormattingEnabled = True
        Me.Box.Location = New System.Drawing.Point(29, 88)
        Me.Box.Name = "Box"
        Me.Box.Size = New System.Drawing.Size(637, 28)
        Me.Box.TabIndex = 0
        '
        'ComboLabel
        '
        Me.ComboLabel.AutoSize = True
        Me.ComboLabel.Location = New System.Drawing.Point(29, 13)
        Me.ComboLabel.Name = "ComboLabel"
        Me.ComboLabel.Size = New System.Drawing.Size(57, 20)
        Me.ComboLabel.TabIndex = 1
        Me.ComboLabel.Text = "Label1"
        '
        'OptionsCombo
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(829, 265)
        Me.Controls.Add(Me.ComboLabel)
        Me.Controls.Add(Me.Box)
        Me.Name = "OptionsCombo"
        Me.Text = "OptionsCombo"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Box As Windows.Forms.ComboBox
    Friend WithEvents ComboLabel As Windows.Forms.Label
End Class
