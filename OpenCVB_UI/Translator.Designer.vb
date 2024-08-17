<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Translator
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
        translate = New Button()
        CopyResultsBack = New Button()
        Algorithms = New ComboBox()
        Label1 = New Label()
        WebView = New Microsoft.Web.WebView2.WinForms.WebView2()
        Timer1 = New Timer(components)
        Timer2 = New Timer(components)
        Timer3 = New Timer(components)
        rtb = New RichTextBox()
        CType(WebView, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' translate
        ' 
        translate.Location = New Point(351, 30)
        translate.Margin = New Padding(3, 4, 3, 4)
        translate.Name = "translate"
        translate.Size = New Size(202, 69)
        translate.TabIndex = 1
        translate.Text = "Translate"
        translate.UseVisualStyleBackColor = True
        ' 
        ' CopyResultsBack
        ' 
        CopyResultsBack.Location = New Point(560, 30)
        CopyResultsBack.Margin = New Padding(3, 4, 3, 4)
        CopyResultsBack.Name = "CopyResultsBack"
        CopyResultsBack.Size = New Size(202, 69)
        CopyResultsBack.TabIndex = 2
        CopyResultsBack.Text = "Touchup"
        CopyResultsBack.UseVisualStyleBackColor = True
        ' 
        ' Algorithms
        ' 
        Algorithms.FormattingEnabled = True
        Algorithms.Location = New Point(13, 64)
        Algorithms.Margin = New Padding(3, 4, 3, 4)
        Algorithms.Name = "Algorithms"
        Algorithms.Size = New Size(311, 33)
        Algorithms.TabIndex = 4
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(13, 35)
        Label1.Name = "Label1"
        Label1.Size = New Size(191, 25)
        Label1.TabIndex = 5
        Label1.Text = "Algorithm to Translate:"
        ' 
        ' WebView
        ' 
        WebView.AllowExternalDrop = True
        WebView.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        WebView.CreationProperties = Nothing
        WebView.DefaultBackgroundColor = Color.White
        WebView.Location = New Point(13, 104)
        WebView.Name = "WebView"
        WebView.Size = New Size(1531, 1488)
        WebView.TabIndex = 6
        WebView.ZoomFactor = 1R
        ' 
        ' Timer1
        ' 
        Timer1.Enabled = True
        Timer1.Interval = 50
        ' 
        ' Timer2
        ' 
        Timer2.Interval = 1000
        ' 
        ' Timer3
        ' 
        ' 
        ' rtb
        ' 
        rtb.Location = New Point(1265, 24)
        rtb.Name = "rtb"
        rtb.Size = New Size(129, 54)
        rtb.TabIndex = 7
        rtb.Text = ""
        ' 
        ' Translator
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1555, 1604)
        Controls.Add(rtb)
        Controls.Add(WebView)
        Controls.Add(Label1)
        Controls.Add(Algorithms)
        Controls.Add(CopyResultsBack)
        Controls.Add(translate)
        Margin = New Padding(3, 4, 3, 4)
        MaximizeBox = False
        MinimizeBox = False
        Name = "Translator"
        Text = "Translate OpenCVB Algorithms using CodeConvert.AI"
        CType(WebView, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()

    End Sub
    Friend WithEvents translate As Button
    Friend WithEvents CopyResultsBack As Button
    Friend WithEvents Algorithms As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents WebView As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents Timer1 As Timer
    Friend WithEvents Timer2 As Timer
    Friend WithEvents Timer3 As Timer
    Friend WithEvents rtb As RichTextBox
End Class
