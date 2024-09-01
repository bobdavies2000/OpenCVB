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
        XYLoc = New Label()
        WebView = New Microsoft.Web.WebView2.WinForms.WebView2()
        Timer1 = New Timer(components)
        Timer2 = New Timer(components)
        Timer3 = New Timer(components)
        Timer4 = New Timer(components)
        Label1 = New Label()
        Button1 = New Button()
        CType(WebView, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' translate
        ' 
        translate.Location = New Point(815, 43)
        translate.Margin = New Padding(3, 4, 3, 4)
        translate.Name = "translate"
        translate.Size = New Size(202, 69)
        translate.TabIndex = 1
        translate.Text = "Step 2: Translate"
        translate.UseVisualStyleBackColor = True
        ' 
        ' CopyResultsBack
        ' 
        CopyResultsBack.Location = New Point(1044, 43)
        CopyResultsBack.Margin = New Padding(3, 4, 3, 4)
        CopyResultsBack.Name = "CopyResultsBack"
        CopyResultsBack.Size = New Size(202, 69)
        CopyResultsBack.TabIndex = 2
        CopyResultsBack.Text = "Step 3: Touchup"
        CopyResultsBack.UseVisualStyleBackColor = True
        ' 
        ' Algorithms
        ' 
        Algorithms.FormattingEnabled = True
        Algorithms.Location = New Point(482, 79)
        Algorithms.Margin = New Padding(3, 4, 3, 4)
        Algorithms.Name = "Algorithms"
        Algorithms.Size = New Size(311, 33)
        Algorithms.TabIndex = 4
        ' 
        ' XYLoc
        ' 
        XYLoc.AutoSize = True
        XYLoc.Location = New Point(12, 1575)
        XYLoc.Name = "XYLoc"
        XYLoc.Size = New Size(60, 25)
        XYLoc.TabIndex = 5
        XYLoc.Text = "XYLoc"
        ' 
        ' WebView
        ' 
        WebView.AllowExternalDrop = True
        WebView.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        WebView.CreationProperties = Nothing
        WebView.DefaultBackgroundColor = Color.White
        WebView.Location = New Point(13, 155)
        WebView.Name = "WebView"
        WebView.Size = New Size(1530, 1417)
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
        ' Timer4
        ' 
        Timer4.Interval = 1000
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(482, 50)
        Label1.Name = "Label1"
        Label1.Size = New Size(318, 25)
        Label1.TabIndex = 9
        Label1.Text = "Step 1: Select an algorithm to Translate"
        ' 
        ' Button1
        ' 
        Button1.Location = New Point(251, 43)
        Button1.Margin = New Padding(3, 4, 3, 4)
        Button1.Name = "Button1"
        Button1.Size = New Size(203, 69)
        Button1.TabIndex = 10
        Button1.Text = "Use Current Algorithm"
        Button1.UseVisualStyleBackColor = True
        Button1.Visible = False
        ' 
        ' Translator
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = SystemColors.ControlDark
        ClientSize = New Size(1555, 1604)
        Controls.Add(Button1)
        Controls.Add(Label1)
        Controls.Add(WebView)
        Controls.Add(XYLoc)
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
    Friend WithEvents XYLoc As Label
    Friend WithEvents WebView As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents Timer1 As Timer
    Friend WithEvents Timer2 As Timer
    Friend WithEvents Timer3 As Timer
    Friend WithEvents Timer4 As Timer
    Friend WithEvents Label1 As Label
    Friend WithEvents Button1 As Button
End Class
