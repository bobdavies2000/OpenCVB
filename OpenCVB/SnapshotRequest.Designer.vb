<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SnapshotRequest
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
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
        Me.OK_Button = New System.Windows.Forms.Button()
        Me.Cancel_Button = New System.Windows.Forms.Button()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.RGBDepth = New System.Windows.Forms.RadioButton()
        Me.ColorImage = New System.Windows.Forms.RadioButton()
        Me.Result1 = New System.Windows.Forms.RadioButton()
        Me.Result2 = New System.Windows.Forms.RadioButton()
        Me.AllImages = New System.Windows.Forms.RadioButton()
        Me.ClickOKlabel = New System.Windows.Forms.Label()
        Me.TableLayoutPanel1.SuspendLayout()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TableLayoutPanel1.ColumnCount = 2
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.OK_Button, 0, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.Cancel_Button, 1, 0)
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(416, 422)
        Me.TableLayoutPanel1.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 1
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(219, 45)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'OK_Button
        '
        Me.OK_Button.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.OK_Button.Location = New System.Drawing.Point(4, 5)
        Me.OK_Button.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.OK_Button.Name = "OK_Button"
        Me.OK_Button.Size = New System.Drawing.Size(100, 35)
        Me.OK_Button.TabIndex = 0
        Me.OK_Button.Text = "OK"
        '
        'Cancel_Button
        '
        Me.Cancel_Button.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Cancel_Button.Location = New System.Drawing.Point(114, 5)
        Me.Cancel_Button.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Cancel_Button.Name = "Cancel_Button"
        Me.Cancel_Button.Size = New System.Drawing.Size(100, 35)
        Me.Cancel_Button.TabIndex = 1
        Me.Cancel_Button.Text = "Cancel"
        '
        'PictureBox1
        '
        Me.PictureBox1.Location = New System.Drawing.Point(11, 11)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(629, 374)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox1.TabIndex = 1
        Me.PictureBox1.TabStop = False
        '
        'RGBDepth
        '
        Me.RGBDepth.AutoSize = True
        Me.RGBDepth.Location = New System.Drawing.Point(416, 93)
        Me.RGBDepth.Name = "RGBDepth"
        Me.RGBDepth.Size = New System.Drawing.Size(167, 24)
        Me.RGBDepth.TabIndex = 7
        Me.RGBDepth.TabStop = True
        Me.RGBDepth.Text = "Depth RGB Image"
        Me.RGBDepth.UseVisualStyleBackColor = True
        '
        'ColorImage
        '
        Me.ColorImage.AutoSize = True
        Me.ColorImage.Location = New System.Drawing.Point(128, 93)
        Me.ColorImage.Name = "ColorImage"
        Me.ColorImage.Size = New System.Drawing.Size(120, 24)
        Me.ColorImage.TabIndex = 8
        Me.ColorImage.TabStop = True
        Me.ColorImage.Text = "Color Image"
        Me.ColorImage.UseVisualStyleBackColor = True
        '
        'Result1
        '
        Me.Result1.AutoSize = True
        Me.Result1.Location = New System.Drawing.Point(128, 274)
        Me.Result1.Name = "Result1"
        Me.Result1.Size = New System.Drawing.Size(89, 24)
        Me.Result1.TabIndex = 10
        Me.Result1.TabStop = True
        Me.Result1.Text = "Result1"
        Me.Result1.UseVisualStyleBackColor = True
        '
        'Result2
        '
        Me.Result2.AutoSize = True
        Me.Result2.Location = New System.Drawing.Point(416, 274)
        Me.Result2.Name = "Result2"
        Me.Result2.Size = New System.Drawing.Size(89, 24)
        Me.Result2.TabIndex = 9
        Me.Result2.TabStop = True
        Me.Result2.Text = "Result2"
        Me.Result2.UseVisualStyleBackColor = True
        '
        'AllImages
        '
        Me.AllImages.AutoSize = True
        Me.AllImages.Location = New System.Drawing.Point(289, 181)
        Me.AllImages.Name = "AllImages"
        Me.AllImages.Size = New System.Drawing.Size(108, 24)
        Me.AllImages.TabIndex = 11
        Me.AllImages.TabStop = True
        Me.AllImages.Text = "All Images"
        Me.AllImages.UseVisualStyleBackColor = True
        '
        'ClickOKlabel
        '
        Me.ClickOKlabel.AutoSize = True
        Me.ClickOKlabel.Location = New System.Drawing.Point(7, 422)
        Me.ClickOKlabel.Name = "ClickOKlabel"
        Me.ClickOKlabel.Size = New System.Drawing.Size(310, 20)
        Me.ClickOKlabel.TabIndex = 12
        Me.ClickOKlabel.Text = "After clicking OK, image will be in clipboard."
        '
        'SnapshotRequest
        '
        Me.AcceptButton = Me.OK_Button
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.Cancel_Button
        Me.ClientSize = New System.Drawing.Size(652, 485)
        Me.Controls.Add(Me.ClickOKlabel)
        Me.Controls.Add(Me.AllImages)
        Me.Controls.Add(Me.Result1)
        Me.Controls.Add(Me.Result2)
        Me.Controls.Add(Me.ColorImage)
        Me.Controls.Add(Me.RGBDepth)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.TableLayoutPanel1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "SnapshotRequest"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "SnapshotRequest"
        Me.TableLayoutPanel1.ResumeLayout(False)
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents OK_Button As System.Windows.Forms.Button
    Friend WithEvents Cancel_Button As System.Windows.Forms.Button
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents RGBDepth As RadioButton
    Friend WithEvents ColorImage As RadioButton
    Friend WithEvents Result1 As RadioButton
    Friend WithEvents Result2 As RadioButton
    Friend WithEvents AllImages As RadioButton
    Friend WithEvents ClickOKlabel As Label
End Class
