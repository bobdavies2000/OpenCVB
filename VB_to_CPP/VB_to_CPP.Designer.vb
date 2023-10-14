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
        Me.SuspendLayout()
        '
        'vbList
        '
        Me.vbList.FormattingEnabled = True
        Me.vbList.Location = New System.Drawing.Point(12, 56)
        Me.vbList.Name = "vbList"
        Me.vbList.Size = New System.Drawing.Size(322, 21)
        Me.vbList.TabIndex = 0
        '
        'Label1
        '
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(12, 30)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(1268, 23)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "VB.Net Algorithms - select one to translate to C++      NOTE: this is not a gener" &
    "al purpose translator.  It is specific to OpenCVB VB.Net algorithms and only app" &
    "roximate."
        '
        'VBrtb
        '
        Me.VBrtb.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.VBrtb.Location = New System.Drawing.Point(12, 124)
        Me.VBrtb.Name = "VBrtb"
        Me.VBrtb.Size = New System.Drawing.Size(816, 978)
        Me.VBrtb.TabIndex = 2
        Me.VBrtb.Text = ""
        '
        'CPPrtb
        '
        Me.CPPrtb.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CPPrtb.Location = New System.Drawing.Point(834, 124)
        Me.CPPrtb.Name = "CPPrtb"
        Me.CPPrtb.Size = New System.Drawing.Size(1114, 978)
        Me.CPPrtb.TabIndex = 3
        Me.CPPrtb.Text = ""
        '
        'Label2
        '
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(12, 98)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(416, 23)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "VB.Net Code"
        '
        'Label3
        '
        Me.Label3.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(827, 98)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(416, 23)
        Me.Label3.TabIndex = 5
        Me.Label3.Text = "C++ Code (approximate)"
        '
        'VB_to_CPP
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1807, 1061)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.CPPrtb)
        Me.Controls.Add(Me.VBrtb)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.vbList)
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
End Class
