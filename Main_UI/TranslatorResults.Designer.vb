<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class TranslatorResults
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
        rtb = New RichTextBox()
        SuspendLayout()
        ' 
        ' rtb
        ' 
        rtb.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        rtb.Location = New Point(12, 12)
        rtb.Name = "rtb"
        rtb.Size = New Size(1086, 1191)
        rtb.TabIndex = 8
        rtb.Text = ""
        ' 
        ' TranslatorResults
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1110, 1215)
        Controls.Add(rtb)
        Name = "TranslatorResults"
        Text = "TranslatorResults"
        ResumeLayout(False)
    End Sub

    Friend WithEvents rtb As RichTextBox
End Class
