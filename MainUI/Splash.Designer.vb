Namespace MainApp
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Public Class Splash
        Inherits Form

        'Form overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(disposing As Boolean)
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
        Private titleLabel As Label
        Private loadingLabel As Label

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Me.titleLabel = New Label()
            Me.loadingLabel = New Label()
            Me.SuspendLayout()
            '
            'titleLabel
            '
            Me.titleLabel.Font = New System.Drawing.Font("Segoe UI", 28.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
            Me.titleLabel.Location = New System.Drawing.Point(0, 60)
            Me.titleLabel.Name = "titleLabel"
            Me.titleLabel.Size = New System.Drawing.Size(400, 60)
            Me.titleLabel.TabIndex = 0
            Me.titleLabel.Text = "OpenCVB"
            Me.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            '
            'loadingLabel
            '
            Me.loadingLabel.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
            Me.loadingLabel.Location = New System.Drawing.Point(0, 140)
            Me.loadingLabel.Name = "loadingLabel"
            Me.loadingLabel.Size = New System.Drawing.Size(400, 30)
            Me.loadingLabel.TabIndex = 1
            Me.loadingLabel.Text = "Loading..."
            Me.loadingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            '
            'Splash
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.BackColor = System.Drawing.Color.White
            Me.ClientSize = New System.Drawing.Size(400, 250)
            Me.Controls.Add(Me.loadingLabel)
            Me.Controls.Add(Me.titleLabel)
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
            Me.Name = "Splash"
            Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
            Me.Text = "OpenCVB"
            Me.TopMost = True
            Me.ResumeLayout(False)
        End Sub

    End Class
End Namespace

