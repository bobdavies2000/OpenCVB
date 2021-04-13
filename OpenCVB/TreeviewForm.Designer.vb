<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class TreeviewForm
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
        Me.components = New System.ComponentModel.Container()
        Me.TreeView1 = New System.Windows.Forms.TreeView()
        Me.ClickTreeLabel = New System.Windows.Forms.Label()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.OK_Button = New System.Windows.Forms.Button()
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
        Me.ReviewStandalone = New System.Windows.Forms.RadioButton()
        Me.ReviewDST = New System.Windows.Forms.RadioButton()
        Me.TableLayoutPanel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'TreeView1
        '
        Me.TreeView1.Dock = System.Windows.Forms.DockStyle.Top
        Me.TreeView1.Location = New System.Drawing.Point(0, 0)
        Me.TreeView1.Name = "TreeView1"
        Me.TreeView1.Size = New System.Drawing.Size(943, 75)
        Me.TreeView1.TabIndex = 1
        '
        'ClickTreeLabel
        '
        Me.ClickTreeLabel.AutoSize = True
        Me.ClickTreeLabel.Location = New System.Drawing.Point(12, 96)
        Me.ClickTreeLabel.Name = "ClickTreeLabel"
        Me.ClickTreeLabel.Size = New System.Drawing.Size(199, 20)
        Me.ClickTreeLabel.TabIndex = 2
        Me.ClickTreeLabel.Text = "Click any tree entry to run it"
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        '
        'OK_Button
        '
        Me.OK_Button.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.OK_Button.Location = New System.Drawing.Point(103, 5)
        Me.OK_Button.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.OK_Button.Name = "OK_Button"
        Me.OK_Button.Size = New System.Drawing.Size(92, 35)
        Me.OK_Button.TabIndex = 0
        Me.OK_Button.Text = "OK"
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TableLayoutPanel1.ColumnCount = 2
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.OK_Button, 1, 0)
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(699, 93)
        Me.TableLayoutPanel1.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 1
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(199, 45)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'ReviewStandalone
        '
        Me.ReviewStandalone.AutoSize = True
        Me.ReviewStandalone.Location = New System.Drawing.Point(54, 130)
        Me.ReviewStandalone.Name = "ReviewStandalone"
        Me.ReviewStandalone.Size = New System.Drawing.Size(398, 24)
        Me.ReviewStandalone.TabIndex = 3
        Me.ReviewStandalone.TabStop = True
        Me.ReviewStandalone.Text = "Clicking a tree entry will review its standalone results"
        Me.ReviewStandalone.UseVisualStyleBackColor = True
        '
        'ReviewDST
        '
        Me.ReviewDST.AutoSize = True
        Me.ReviewDST.Location = New System.Drawing.Point(54, 167)
        Me.ReviewDST.Name = "ReviewDST"
        Me.ReviewDST.Size = New System.Drawing.Size(508, 24)
        Me.ReviewDST.TabIndex = 4
        Me.ReviewDST.TabStop = True
        Me.ReviewDST.Text = "Clicking a tree entry will review its dst1 and dst2 intermediate results"
        Me.ReviewDST.UseVisualStyleBackColor = True
        '
        'TreeviewForm
        '
        Me.AcceptButton = Me.OK_Button
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(943, 194)
        Me.Controls.Add(Me.ReviewDST)
        Me.Controls.Add(Me.ReviewStandalone)
        Me.Controls.Add(Me.ClickTreeLabel)
        Me.Controls.Add(Me.TreeView1)
        Me.Controls.Add(Me.TableLayoutPanel1)
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Name = "TreeviewForm"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "TreeviewForm"
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents TreeView1 As TreeView
    Friend WithEvents ClickTreeLabel As Label
    Friend WithEvents Timer1 As Timer
    Friend WithEvents OK_Button As Button
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents ReviewStandalone As RadioButton
    Friend WithEvents ReviewDST As RadioButton
End Class
