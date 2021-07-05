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
        Me.ClickTreeLabel = New System.Windows.Forms.Label()
        Me.TreeViewTimer = New System.Windows.Forms.Timer(Me.components)
        Me.OK_Button = New System.Windows.Forms.Button()
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
        Me.PercentTime = New System.Windows.Forms.TextBox()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.Accumulate = New System.Windows.Forms.CheckBox()
        Me.TableLayoutPanel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'TreeView1
        '
        Me.TreeView1.Dock = System.Windows.Forms.DockStyle.Top
        Me.TreeView1.Location = New System.Drawing.Point(0, 0)
        Me.TreeView1.Name = "TreeView1"
        Me.TreeView1.Size = New System.Drawing.Size(943, 113)
        Me.TreeView1.TabIndex = 0
        '
        'ClickTreeLabel
        '
        Me.ClickTreeLabel.AutoSize = True
        Me.ClickTreeLabel.Location = New System.Drawing.Point(27, 145)
        Me.ClickTreeLabel.Name = "ClickTreeLabel"
        Me.ClickTreeLabel.Size = New System.Drawing.Size(357, 20)
        Me.ClickTreeLabel.TabIndex = 2
        Me.ClickTreeLabel.Text = "Click any tree entry to view its intermediate results"
        '
        'TreeViewTimer
        '
        Me.TreeViewTimer.Enabled = True
        '
        'OK_Button
        '
        Me.OK_Button.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.OK_Button.Location = New System.Drawing.Point(103, 5)
        Me.OK_Button.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.OK_Button.Name = "OK_Button"
        Me.OK_Button.Size = New System.Drawing.Size(92, 35)
        Me.OK_Button.TabIndex = 1
        Me.OK_Button.Text = "OK"
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TableLayoutPanel1.ColumnCount = 2
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.OK_Button, 1, 0)
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(718, 145)
        Me.TableLayoutPanel1.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 1
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(199, 45)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'PercentTime
        '
        Me.PercentTime.Location = New System.Drawing.Point(634, 12)
        Me.PercentTime.Multiline = True
        Me.PercentTime.Name = "PercentTime"
        Me.PercentTime.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.PercentTime.Size = New System.Drawing.Size(252, 67)
        Me.PercentTime.TabIndex = 3
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        Me.Timer1.Interval = 1000
        '
        'Accumulate
        '
        Me.Accumulate.AutoSize = True
        Me.Accumulate.Location = New System.Drawing.Point(31, 175)
        Me.Accumulate.Name = "Accumulate"
        Me.Accumulate.Size = New System.Drawing.Size(376, 24)
        Me.Accumulate.TabIndex = 4
        Me.Accumulate.Text = "Accumulate time (instead of latest interval times)"
        Me.Accumulate.UseVisualStyleBackColor = True
        '
        'TreeviewForm
        '
        Me.AcceptButton = Me.OK_Button
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(943, 194)
        Me.Controls.Add(Me.Accumulate)
        Me.Controls.Add(Me.PercentTime)
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
    Friend WithEvents TreeViewTimer As Timer
    Friend WithEvents OK_Button As Button
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents PercentTime As TextBox
    Friend WithEvents Timer1 As Timer
    Friend WithEvents Accumulate As CheckBox
End Class
