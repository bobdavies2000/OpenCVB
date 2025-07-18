<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class TreeviewForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.PercentTime = New System.Windows.Forms.TextBox()
        Me.Timer2 = New System.Windows.Forms.Timer(Me.components)
        Me.TreeView1 = New System.Windows.Forms.TreeView()
        Me.SuspendLayout()
        '
        'PercentTime
        '
        Me.PercentTime.Location = New System.Drawing.Point(386, 0)
        Me.PercentTime.Multiline = True
        Me.PercentTime.Name = "PercentTime"
        Me.PercentTime.Size = New System.Drawing.Size(303, 84)
        Me.PercentTime.TabIndex = 0
        '
        'Timer2
        '
        Me.Timer2.Enabled = True
        Me.Timer2.Interval = 1000
        '
        'TreeView1
        '
        Me.TreeView1.Location = New System.Drawing.Point(1, 0)
        Me.TreeView1.Name = "TreeView1"
        Me.TreeView1.Size = New System.Drawing.Size(222, 290)
        Me.TreeView1.TabIndex = 1
        '
        'TreeviewForm
        '
        Me.ClientSize = New System.Drawing.Size(688, 1052)
        Me.Controls.Add(Me.TreeView1)
        Me.Controls.Add(Me.PercentTime)
        Me.Name = "TreeviewForm"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents PercentTime As Windows.Forms.TextBox
    Friend WithEvents Timer2 As Windows.Forms.Timer
    Friend WithEvents TreeView1 As Windows.Forms.TreeView
End Class
