<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Touchup
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
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.translate = New System.Windows.Forms.Button()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.rtb = New System.Windows.Forms.RichTextBox()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(18, 28)
        Me.Label1.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(340, 20)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Step 0: Paste VB.Net code into CodeConverter"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(18, 62)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(311, 20)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Step 1: Paste CodeConverter output below"
        '
        'translate
        '
        Me.translate.Location = New System.Drawing.Point(415, 13)
        Me.translate.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.translate.Name = "translate"
        Me.translate.Size = New System.Drawing.Size(406, 69)
        Me.translate.TabIndex = 2
        Me.translate.Text = "Step 2: Touchup Translated code "
        Me.translate.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(868, 37)
        Me.Label3.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(472, 20)
        Me.Label3.TabIndex = 3
        Me.Label3.Text = "Step 3: Paste code to CS_AI_Gen.cs class in CS_Classes project"
        '
        'rtb
        '
        Me.rtb.Location = New System.Drawing.Point(22, 107)
        Me.rtb.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.rtb.Name = "rtb"
        Me.rtb.Size = New System.Drawing.Size(1368, 1480)
        Me.rtb.TabIndex = 4
        Me.rtb.Text = ""
        '
        'Touchup
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1398, 1593)
        Me.Controls.Add(Me.rtb)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.translate)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Name = "Touchup"
        Me.Text = "Touchup AI-Generated C# code"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents translate As Button
    Friend WithEvents Label3 As Label
    Friend WithEvents rtb As RichTextBox
End Class
