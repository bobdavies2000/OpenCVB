<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsCombo
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.Box = New System.Windows.Forms.ComboBox()
        Me.ComboLabel = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'Box
        '
        Me.Box.FormattingEnabled = True
        Me.Box.Location = New System.Drawing.Point(19, 57)
        Me.Box.Margin = New System.Windows.Forms.Padding(2)
        Me.Box.Name = "Box"
        Me.Box.Size = New System.Drawing.Size(426, 21)
        Me.Box.TabIndex = 0
        '
        'ComboLabel
        '
        Me.ComboLabel.AutoSize = True
        Me.ComboLabel.Location = New System.Drawing.Point(19, 8)
        Me.ComboLabel.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.ComboLabel.Name = "ComboLabel"
        Me.ComboLabel.Size = New System.Drawing.Size(46, 13)
        Me.ComboLabel.TabIndex = 1
        Me.ComboLabel.Text = "labels(2)"
        '
        'OptionsCombo
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(553, 172)
        Me.Controls.Add(Me.ComboLabel)
        Me.Controls.Add(Me.Box)
        Me.Margin = New System.Windows.Forms.Padding(2)
        Me.Name = "OptionsCombo"
        Me.Text = "OptionsCombo"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Box As System.Windows.Forms.ComboBox
    Friend WithEvents ComboLabel As System.Windows.Forms.Label
End Class
