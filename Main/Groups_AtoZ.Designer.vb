<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Groups_AtoZ
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
        GroupDataView = New DataGridView()
        CType(GroupDataView, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' GroupDataView
        ' 
        GroupDataView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        GroupDataView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
        GroupDataView.BackgroundColor = SystemColors.ActiveCaption
        GroupDataView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        GroupDataView.Dock = DockStyle.Fill
        GroupDataView.Location = New Point(0, 0)
        GroupDataView.Name = "GroupDataView"
        GroupDataView.ReadOnly = True
        GroupDataView.RowHeadersWidth = 62
        GroupDataView.Size = New Size(1972, 1302)
        GroupDataView.TabIndex = 0
        ' 
        ' Groups_AtoZ
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1972, 1302)
        Controls.Add(GroupDataView)
        KeyPreview = True
        Name = "Groups_AtoZ"
        Text = "Groups_AtoZ"
        CType(GroupDataView, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents GroupDataView As DataGridView
End Class
