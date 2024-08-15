<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Translator
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
        Me.WebView = New System.Windows.Forms.WebBrowser()
        Me.translate = New System.Windows.Forms.Button()
        Me.Touchup = New System.Windows.Forms.Button()
        Me.ComboBox1 = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'WebView
        '
        Me.WebView.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.WebView.Location = New System.Drawing.Point(0, 120)
        Me.WebView.MinimumSize = New System.Drawing.Size(20, 20)
        Me.WebView.Name = "WebView"
        Me.WebView.Size = New System.Drawing.Size(1556, 1598)
        Me.WebView.TabIndex = 0
        '
        'translate
        '
        Me.translate.Location = New System.Drawing.Point(316, 24)
        Me.translate.Name = "translate"
        Me.translate.Size = New System.Drawing.Size(182, 55)
        Me.translate.TabIndex = 1
        Me.translate.Text = "Translate"
        Me.translate.UseVisualStyleBackColor = True
        '
        'Touchup
        '
        Me.Touchup.Location = New System.Drawing.Point(504, 24)
        Me.Touchup.Name = "Touchup"
        Me.Touchup.Size = New System.Drawing.Size(182, 55)
        Me.Touchup.TabIndex = 2
        Me.Touchup.Text = "Touchup"
        Me.Touchup.UseVisualStyleBackColor = True
        '
        'ComboBox1
        '
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Location = New System.Drawing.Point(12, 51)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(280, 28)
        Me.ComboBox1.TabIndex = 4
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 28)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(168, 20)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "Algorithm to Translate:"
        '
        'Translator
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1556, 1718)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.ComboBox1)
        Me.Controls.Add(Me.Touchup)
        Me.Controls.Add(Me.translate)
        Me.Controls.Add(Me.WebView)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "Translator"
        Me.Text = "Translate OpenCVB Algorithms using CodeConvert.AI"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents WebView As WebBrowser
    Friend WithEvents translate As Button
    Friend WithEvents Touchup As Button
    Friend WithEvents ComboBox1 As ComboBox
    Friend WithEvents Label1 As Label
End Class
