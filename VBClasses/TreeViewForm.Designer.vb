<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class TreeViewForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        TreeView1 = New TreeView()
        Timer2 = New Timer(components)
        PercentTime = New RichTextBox()
        SuspendLayout()
        ' 
        ' TreeView1
        ' 
        TreeView1.Location = New Point(0, 0)
        TreeView1.Margin = New Padding(4)
        TreeView1.Name = "TreeView1"
        TreeView1.Size = New Size(286, 1136)
        TreeView1.TabIndex = 0
        ' 
        ' Timer2
        ' 
        Timer2.Enabled = True
        Timer2.Interval = 3000
        ' 
        ' PercentTime
        ' 
        PercentTime.Location = New Point(286, 5)
        PercentTime.Name = "PercentTime"
        PercentTime.Size = New Size(237, 29)
        PercentTime.TabIndex = 2
        PercentTime.Text = ""
        ' 
        ' TreeViewForm
        ' 
        AutoScaleDimensions = New SizeF(12F, 30F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(859, 1140)
        Controls.Add(PercentTime)
        Controls.Add(TreeView1)
        Margin = New Padding(4)
        Name = "TreeViewForm"
        Text = "TreeViewForm"
        ResumeLayout(False)
    End Sub

    Friend WithEvents TreeView1 As TreeView
    Friend WithEvents Timer2 As Timer
    Friend WithEvents PercentTime As RichTextBox
End Class
