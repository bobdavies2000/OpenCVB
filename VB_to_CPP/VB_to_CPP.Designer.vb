<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class VB_to_CPP
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
        Me.vbList = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.VBrtb = New System.Windows.Forms.RichTextBox()
        Me.CPPrtb = New System.Windows.Forms.RichTextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.PrepareCPP = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'vbList
        '
        Me.vbList.FormattingEnabled = True
        Me.vbList.Location = New System.Drawing.Point(18, 86)
        Me.vbList.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.vbList.Name = "vbList"
        Me.vbList.Size = New System.Drawing.Size(481, 28)
        Me.vbList.TabIndex = 0
        '
        'Label1
        '
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(18, 46)
        Me.Label1.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(624, 35)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "VB.Net Algorithms - select one to translate to C++"
        '
        'VBrtb
        '
        Me.VBrtb.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.VBrtb.Location = New System.Drawing.Point(1, 191)
        Me.VBrtb.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.VBrtb.Name = "VBrtb"
        Me.VBrtb.Size = New System.Drawing.Size(995, 1439)
        Me.VBrtb.TabIndex = 2
        Me.VBrtb.Text = ""
        '
        'CPPrtb
        '
        Me.CPPrtb.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CPPrtb.Location = New System.Drawing.Point(1006, 191)
        Me.CPPrtb.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.CPPrtb.Name = "CPPrtb"
        Me.CPPrtb.Size = New System.Drawing.Size(968, 1439)
        Me.CPPrtb.TabIndex = 3
        Me.CPPrtb.Text = ""
        '
        'Label2
        '
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(18, 151)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(624, 35)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "Copy And paste this code into Google's Bard"
        '
        'Label3
        '
        Me.Label3.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(1016, 151)
        Me.Label3.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(624, 35)
        Me.Label3.TabIndex = 5
        Me.Label3.Text = "C++ Code - Paste output of translate from Bard here "
        '
        'PrepareCPP
        '
        Me.PrepareCPP.Location = New System.Drawing.Point(1015, 89)
        Me.PrepareCPP.Name = "PrepareCPP"
        Me.PrepareCPP.Size = New System.Drawing.Size(417, 37)
        Me.PrepareCPP.TabIndex = 6
        Me.PrepareCPP.Text = "Review and update the C++ Code"
        Me.PrepareCPP.UseVisualStyleBackColor = True
        '
        'VB_to_CPP
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1970, 1632)
        Me.Controls.Add(Me.PrepareCPP)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.CPPrtb)
        Me.Controls.Add(Me.VBrtb)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.vbList)
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Name = "VB_to_CPP"
        Me.Text = "VB_to_CPP - translate OpenCVB algorithms in VB.Net to an 'Include Only' C++ algor" &
    "ithm"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents vbList As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents VBrtb As RichTextBox
    Friend WithEvents CPPrtb As RichTextBox
    Friend WithEvents Label2 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents PrepareCPP As Button
End Class
