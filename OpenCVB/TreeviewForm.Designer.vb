<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class TreeviewForm
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
        Me.components = New System.ComponentModel.Container()
        Me.TreeView1 = New System.Windows.Forms.TreeView()
        Me.TreeViewTimer = New System.Windows.Forms.Timer(Me.components)
        Me.PercentTime = New System.Windows.Forms.TextBox()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'TreeView1
        '
        Me.TreeView1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TreeView1.Location = New System.Drawing.Point(0, 0)
        Me.TreeView1.Name = "TreeView1"
        Me.TreeView1.Size = New System.Drawing.Size(886, 194)
        Me.TreeView1.TabIndex = 0
        '
        'TreeViewTimer
        '
        Me.TreeViewTimer.Enabled = True
        '
        'PercentTime
        '
        Me.PercentTime.Location = New System.Drawing.Point(514, 12)
        Me.PercentTime.Multiline = True
        Me.PercentTime.Name = "PercentTime"
        Me.PercentTime.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.PercentTime.Size = New System.Drawing.Size(372, 67)
        Me.PercentTime.TabIndex = 3
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        Me.Timer1.Interval = 1000
        '
        'TreeviewForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(886, 194)
        Me.Controls.Add(Me.PercentTime)
        Me.Controls.Add(Me.TreeView1)
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Name = "TreeviewForm"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Click any entry in the tree to view intermediate results."
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents TreeView1 As TreeView
    Friend WithEvents TreeViewTimer As Timer
    Friend WithEvents PercentTime As TextBox
    Friend WithEvents Timer1 As Timer
End Class
