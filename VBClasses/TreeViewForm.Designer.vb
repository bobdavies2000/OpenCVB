<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class TreeViewForm
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
        components = New ComponentModel.Container()
        TreeView1 = New TreeView()
        PercentTime = New TextBox()
        Timer2 = New Timer(components)
        SuspendLayout()
        ' 
        ' TreeView1
        ' 
        TreeView1.Location = New Point(0, 0)
        TreeView1.Name = "TreeView1"
        TreeView1.Size = New Size(239, 947)
        TreeView1.TabIndex = 0
        ' 
        ' PercentTime
        ' 
        PercentTime.Location = New Point(450, 0)
        PercentTime.Name = "PercentTime"
        PercentTime.Size = New Size(151, 31)
        PercentTime.TabIndex = 1
        ' 
        ' Timer2
        ' 
        Timer2.Enabled = True
        Timer2.Interval = 1000
        ' 
        ' TreeviewForm
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(613, 950)
        Controls.Add(PercentTime)
        Controls.Add(TreeView1)
        Name = "TreeviewForm"
        Text = "TreeViewForm"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents TreeView1 As TreeView
    Friend WithEvents PercentTime As TextBox
    Friend WithEvents Timer2 As Timer
End Class
