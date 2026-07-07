Namespace MainApp
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Public Class Splash
        Inherits Form

        'Form overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(disposing As Boolean)
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
        Private titleLabel As Label
        Public loadingLabel As Label

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            titleLabel = New Label()
            loadingLabel = New Label()
            SuspendLayout()
            ' 
            ' titleLabel
            ' 
            titleLabel.Font = New Font("Segoe UI", 28F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
            titleLabel.Location = New Point(0, 45)
            titleLabel.Margin = New Padding(2, 0, 2, 0)
            titleLabel.Name = "titleLabel"
            titleLabel.Size = New Size(311, 45)
            titleLabel.TabIndex = 0
            titleLabel.Text = "OpenCVB"
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            ' 
            ' loadingLabel
            ' 
            loadingLabel.Font = New Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
            loadingLabel.Location = New Point(0, 102)
            loadingLabel.Margin = New Padding(2, 0, 2, 0)
            loadingLabel.Name = "loadingLabel"
            loadingLabel.Size = New Size(311, 56)
            loadingLabel.TabIndex = 1
            loadingLabel.Text = "Loading..."
            loadingLabel.TextAlign = ContentAlignment.MiddleCenter
            ' 
            ' Splash
            ' 
            AutoScaleDimensions = New SizeF(7F, 15F)
            AutoScaleMode = AutoScaleMode.Font
            BackColor = Color.White
            ClientSize = New Size(311, 188)
            Controls.Add(loadingLabel)
            Controls.Add(titleLabel)
            FormBorderStyle = FormBorderStyle.None
            Margin = New Padding(2, 2, 2, 2)
            Name = "Splash"
            StartPosition = FormStartPosition.CenterScreen
            Text = "OpenCVB"
            TopMost = True
            ResumeLayout(False)
        End Sub

    End Class
End Namespace

