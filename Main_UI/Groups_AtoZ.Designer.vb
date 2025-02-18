<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Groups_AtoZ
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
        Me.GroupDataView = New System.Windows.Forms.DataGridView()
        CType(Me.GroupDataView, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'GroupDataView
        '
        Me.GroupDataView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells
        Me.GroupDataView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells
        Me.GroupDataView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.GroupDataView.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupDataView.Location = New System.Drawing.Point(0, 0)
        Me.GroupDataView.Name = "GroupDataView"
        Me.GroupDataView.RowHeadersWidth = 62
        Me.GroupDataView.RowTemplate.Height = 28
        Me.GroupDataView.Size = New System.Drawing.Size(1994, 1358)
        Me.GroupDataView.TabIndex = 0
        '
        'Groups_AtoZ
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1994, 1358)
        Me.Controls.Add(Me.GroupDataView)
        Me.KeyPreview = True
        Me.Name = "Groups_AtoZ"
        Me.Text = "Groups_AtoZ"
        CType(Me.GroupDataView, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents GroupDataView As DataGridView
End Class
