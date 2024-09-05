<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Translator
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Translator))
        Me.translate = New System.Windows.Forms.Button()
        Me.CopyResultsBack = New System.Windows.Forms.Button()
        Me.Algorithms = New System.Windows.Forms.ComboBox()
        Me.XYLoc = New System.Windows.Forms.Label()
        Me.WebView = New Microsoft.Web.WebView2.WinForms.WebView2()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.Timer2 = New System.Windows.Forms.Timer(Me.components)
        Me.Timer3 = New System.Windows.Forms.Timer(Me.components)
        Me.Timer4 = New System.Windows.Forms.Timer(Me.components)
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Timer5 = New System.Windows.Forms.Timer(Me.components)
        CType(Me.WebView, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'translate
        '
        resources.ApplyResources(Me.translate, "translate")
        Me.translate.Name = "translate"
        Me.translate.UseVisualStyleBackColor = True
        '
        'CopyResultsBack
        '
        resources.ApplyResources(Me.CopyResultsBack, "CopyResultsBack")
        Me.CopyResultsBack.Name = "CopyResultsBack"
        Me.CopyResultsBack.UseVisualStyleBackColor = True
        '
        'Algorithms
        '
        Me.Algorithms.FormattingEnabled = True
        resources.ApplyResources(Me.Algorithms, "Algorithms")
        Me.Algorithms.Name = "Algorithms"
        '
        'XYLoc
        '
        resources.ApplyResources(Me.XYLoc, "XYLoc")
        Me.XYLoc.Name = "XYLoc"
        '
        'WebView
        '
        Me.WebView.AllowExternalDrop = True
        resources.ApplyResources(Me.WebView, "WebView")
        Me.WebView.CreationProperties = Nothing
        Me.WebView.DefaultBackgroundColor = System.Drawing.Color.White
        Me.WebView.Name = "WebView"
        Me.WebView.ZoomFactor = 1.0R
        '
        'Timer1
        '
        Me.Timer1.Enabled = True
        Me.Timer1.Interval = 50
        '
        'Timer2
        '
        Me.Timer2.Interval = 1000
        '
        'Timer3
        '
        '
        'Timer4
        '
        Me.Timer4.Interval = 1000
        '
        'Label1
        '
        resources.ApplyResources(Me.Label1, "Label1")
        Me.Label1.Name = "Label1"
        '
        'Button1
        '
        resources.ApplyResources(Me.Button1, "Button1")
        Me.Button1.Name = "Button1"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Timer5
        '
        Me.Timer5.Enabled = True
        Me.Timer5.Interval = 500
        '
        'Translator
        '
        resources.ApplyResources(Me, "$this")
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ControlDark
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.WebView)
        Me.Controls.Add(Me.XYLoc)
        Me.Controls.Add(Me.Algorithms)
        Me.Controls.Add(Me.CopyResultsBack)
        Me.Controls.Add(Me.translate)
        Me.Name = "Translator"
        CType(Me.WebView, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

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
    Friend WithEvents Timer5 As Timer
End Class
