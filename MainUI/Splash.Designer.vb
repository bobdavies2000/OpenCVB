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
            titleLabel.Location = New Point(0, 90)
            titleLabel.Margin = New Padding(4, 0, 4, 0)
            titleLabel.Name = "titleLabel"
            titleLabel.Size = New Size(533, 90)
            titleLabel.TabIndex = 0
            titleLabel.Text = "OpenCVB"
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            ' 
            ' loadingLabel
            ' 
            loadingLabel.Font = New Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
            loadingLabel.Location = New Point(0, 167)
            loadingLabel.Margin = New Padding(4, 0, 4, 0)
            loadingLabel.Name = "loadingLabel"
            loadingLabel.Size = New Size(533, 111)
            loadingLabel.TabIndex = 1
            loadingLabel.Text = "Loading..."
            loadingLabel.TextAlign = ContentAlignment.MiddleCenter
            ' 
            ' Splash
            ' 
            AutoScaleDimensions = New SizeF(12.0F, 30.0F)
            AutoScaleMode = AutoScaleMode.Font
            BackColor = Color.White
            ClientSize = New Size(533, 375)
            Controls.Add(loadingLabel)
            Controls.Add(titleLabel)
            FormBorderStyle = FormBorderStyle.None
            Margin = New Padding(4)
            Name = "Splash"
            StartPosition = FormStartPosition.CenterScreen
            Text = "OpenCVB"
            TopMost = True
            ResumeLayout(False)
        End Sub

    End Class
End Namespace

