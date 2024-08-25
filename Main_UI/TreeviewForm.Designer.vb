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
        components = New ComponentModel.Container()
        TreeView1 = New TreeView()
        TreeViewTimer = New Timer(components)
        PercentTime = New TextBox()
        Timer1 = New Timer(components)
        SuspendLayout()
        ' 
        ' TreeView1
        ' 
        TreeView1.Dock = DockStyle.Fill
        TreeView1.Location = New Point(0, 0)
        TreeView1.Margin = New Padding(3, 4, 3, 4)
        TreeView1.Name = "TreeView1"
        TreeView1.Size = New Size(820, 242)
        TreeView1.TabIndex = 0
        ' 
        ' TreeViewTimer
        ' 
        TreeViewTimer.Enabled = True
        ' 
        ' PercentTime
        ' 
        PercentTime.Location = New Point(406, 13)
        PercentTime.Margin = New Padding(3, 4, 3, 4)
        PercentTime.Multiline = True
        PercentTime.Name = "PercentTime"
        PercentTime.ScrollBars = ScrollBars.Vertical
        PercentTime.Size = New Size(413, 83)
        PercentTime.TabIndex = 3
        ' 
        ' Timer1
        ' 
        Timer1.Enabled = True
        Timer1.Interval = 1000
        ' 
        ' TreeviewForm
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(820, 242)
        Controls.Add(PercentTime)
        Controls.Add(TreeView1)
        Margin = New Padding(4, 6, 4, 6)
        Name = "TreeviewForm"
        ShowInTaskbar = False
        StartPosition = FormStartPosition.CenterParent
        Text = "Click any entry in the tree to view intermediate results."
        ResumeLayout(False)
        PerformLayout()

    End Sub
    Friend WithEvents TreeView1 As TreeView
    Friend WithEvents TreeViewTimer As Timer
    Friend WithEvents PercentTime As TextBox
    Friend WithEvents Timer1 As Timer
End Class
