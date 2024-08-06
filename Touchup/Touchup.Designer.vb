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
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.VB_To_CSharp = New System.Windows.Forms.RadioButton()
        Me.CSharp_To_CPP = New System.Windows.Forms.RadioButton()
        Me.CSharp_To_VB = New System.Windows.Forms.RadioButton()
        Me.CPP_To_CSharp = New System.Windows.Forms.RadioButton()
        Me.CPP_To_VB = New System.Windows.Forms.RadioButton()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(272, 32)
        Me.Label1.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(352, 20)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Step 0: Use CodeConverter.ai to start translation"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(272, 66)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(311, 20)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Step 1: Paste CodeConverter output below"
        '
        'translate
        '
        Me.translate.Location = New System.Drawing.Point(276, 91)
        Me.translate.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.translate.Name = "translate"
        Me.translate.Size = New System.Drawing.Size(397, 69)
        Me.translate.TabIndex = 2
        Me.translate.Text = "Step 2: Touchup Translated code "
        Me.translate.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(272, 174)
        Me.Label3.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(330, 20)
        Me.Label3.TabIndex = 3
        Me.Label3.Text = "Step 3: Copy code below to OpenCVB project"
        '
        'rtb
        '
        Me.rtb.Location = New System.Drawing.Point(22, 232)
        Me.rtb.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.rtb.Name = "rtb"
        Me.rtb.Size = New System.Drawing.Size(1368, 1355)
        Me.rtb.TabIndex = 4
        Me.rtb.Text = ""
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.CPP_To_VB)
        Me.GroupBox1.Controls.Add(Me.CPP_To_CSharp)
        Me.GroupBox1.Controls.Add(Me.CSharp_To_VB)
        Me.GroupBox1.Controls.Add(Me.CSharp_To_CPP)
        Me.GroupBox1.Controls.Add(Me.VB_To_CSharp)
        Me.GroupBox1.Location = New System.Drawing.Point(22, 12)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(214, 203)
        Me.GroupBox1.TabIndex = 5
        Me.GroupBox1.TabStop = False
        '
        'VB_To_CSharp
        '
        Me.VB_To_CSharp.AutoSize = True
        Me.VB_To_CSharp.Location = New System.Drawing.Point(23, 25)
        Me.VB_To_CSharp.Name = "VB_To_CSharp"
        Me.VB_To_CSharp.Size = New System.Drawing.Size(131, 24)
        Me.VB_To_CSharp.TabIndex = 0
        Me.VB_To_CSharp.TabStop = True
        Me.VB_To_CSharp.Text = "VB.Net to C# "
        Me.VB_To_CSharp.UseVisualStyleBackColor = True
        '
        'CSharp_To_CPP
        '
        Me.CSharp_To_CPP.AutoSize = True
        Me.CSharp_To_CPP.Location = New System.Drawing.Point(23, 91)
        Me.CSharp_To_CPP.Name = "CSharp_To_CPP"
        Me.CSharp_To_CPP.Size = New System.Drawing.Size(105, 24)
        Me.CSharp_To_CPP.TabIndex = 1
        Me.CSharp_To_CPP.TabStop = True
        Me.CSharp_To_CPP.Text = "C# to C++"
        Me.CSharp_To_CPP.UseVisualStyleBackColor = True
        '
        'CSharp_To_VB
        '
        Me.CSharp_To_VB.AutoSize = True
        Me.CSharp_To_VB.Location = New System.Drawing.Point(23, 58)
        Me.CSharp_To_VB.Name = "CSharp_To_VB"
        Me.CSharp_To_VB.Size = New System.Drawing.Size(127, 24)
        Me.CSharp_To_VB.TabIndex = 2
        Me.CSharp_To_VB.TabStop = True
        Me.CSharp_To_VB.Text = "C# to VB.Net"
        Me.CSharp_To_VB.UseVisualStyleBackColor = True
        '
        'CPP_To_CSharp
        '
        Me.CPP_To_CSharp.AutoSize = True
        Me.CPP_To_CSharp.Enabled = False
        Me.CPP_To_CSharp.Location = New System.Drawing.Point(23, 124)
        Me.CPP_To_CSharp.Name = "CPP_To_CSharp"
        Me.CPP_To_CSharp.Size = New System.Drawing.Size(105, 24)
        Me.CPP_To_CSharp.TabIndex = 3
        Me.CPP_To_CSharp.TabStop = True
        Me.CPP_To_CSharp.Text = "C++ to C#"
        Me.CPP_To_CSharp.UseVisualStyleBackColor = True
        '
        'CPP_To_VB
        '
        Me.CPP_To_VB.AutoSize = True
        Me.CPP_To_VB.Enabled = False
        Me.CPP_To_VB.Location = New System.Drawing.Point(23, 157)
        Me.CPP_To_VB.Name = "CPP_To_VB"
        Me.CPP_To_VB.Size = New System.Drawing.Size(136, 24)
        Me.CPP_To_VB.TabIndex = 4
        Me.CPP_To_VB.TabStop = True
        Me.CPP_To_VB.Text = "C++ to VB.Net"
        Me.CPP_To_VB.UseVisualStyleBackColor = True
        '
        'Touchup
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1398, 1593)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.rtb)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.translate)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Name = "Touchup"
        Me.Text = "Touchup AI-Generated C# code"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents translate As Button
    Friend WithEvents Label3 As Label
    Friend WithEvents rtb As RichTextBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents CSharp_To_VB As RadioButton
    Friend WithEvents CSharp_To_CPP As RadioButton
    Friend WithEvents VB_To_CSharp As RadioButton
    Friend WithEvents CPP_To_VB As RadioButton
    Friend WithEvents CPP_To_CSharp As RadioButton
End Class
