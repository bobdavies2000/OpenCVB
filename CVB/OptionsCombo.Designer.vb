<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsCombo
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OptionsCombo))
        Box = New ComboBox()
        ComboLabel = New Label()
        SuspendLayout()
        ' 
        ' Box
        ' 
        Box.FormattingEnabled = True
        Box.Location = New Point(32, 110)
        Box.Margin = New Padding(3, 4, 3, 4)
        Box.Name = "Box"
        Box.Size = New Size(707, 33)
        Box.TabIndex = 0
        ' 
        ' ComboLabel
        ' 
        ComboLabel.AutoSize = True
        ComboLabel.Location = New Point(32, 15)
        ComboLabel.Name = "ComboLabel"
        ComboLabel.Size = New Size(77, 25)
        ComboLabel.TabIndex = 1
        ComboLabel.Text = "labels(2)"
        ' 
        ' OptionsCombo
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(922, 331)
        Controls.Add(ComboLabel)
        Controls.Add(Box)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(3, 4, 3, 4)
        Name = "OptionsCombo"
        Text = "OptionsCombo"
        ResumeLayout(False)
        PerformLayout()

    End Sub

    Friend WithEvents Box As System.Windows.Forms.ComboBox
    Friend WithEvents ComboLabel As System.Windows.Forms.Label
End Class
