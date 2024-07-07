<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ImageForm
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
        Me.imagePic = New System.Windows.Forms.PictureBox()
        CType(Me.imagePic, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'imagePic
        '
        Me.imagePic.Location = New System.Drawing.Point(3, 4)
        Me.imagePic.Name = "imagePic"
        Me.imagePic.Size = New System.Drawing.Size(1195, 394)
        Me.imagePic.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.imagePic.TabIndex = 0
        Me.imagePic.TabStop = False
        '
        'ImageForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1440, 997)
        Me.Controls.Add(Me.imagePic)
        Me.Name = "ImageForm"
        Me.Text = "OptionsAlphaBlend"
        CType(Me.imagePic, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents imagePic As Windows.Forms.PictureBox
End Class
