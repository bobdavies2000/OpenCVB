<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class TranslatorForm
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
        components = New ComponentModel.Container()
        WebView = New Microsoft.Web.WebView2.WinForms.WebView2()
        Translate = New Button()
        Label1 = New Label()
        Timer1 = New Timer(components)
        Algorithms = New ComboBox()
        Label2 = New Label()
        SortAlgorithms = New ComboBox()
        LoadData = New Button()
        CopyResultsBack = New Button()
        rtb = New RichTextBox()
        Timer2 = New Timer(components)
        Timer3 = New Timer(components)
        CType(WebView, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' WebView
        ' 
        WebView.AllowExternalDrop = True
        WebView.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        WebView.CreationProperties = Nothing
        WebView.DefaultBackgroundColor = Color.White
        WebView.Location = New Point(12, 116)
        WebView.Name = "WebView"
        WebView.Size = New Size(1536, 1454)
        WebView.TabIndex = 0
        WebView.ZoomFactor = 1R
        ' 
        ' Translate
        ' 
        Translate.Location = New Point(12, 26)
        Translate.Name = "Translate"
        Translate.Size = New Size(159, 56)
        Translate.TabIndex = 1
        Translate.Text = "Translate"
        Translate.UseVisualStyleBackColor = True
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(682, 26)
        Label1.Name = "Label1"
        Label1.Size = New Size(63, 25)
        Label1.TabIndex = 2
        Label1.Text = "Label1"
        ' 
        ' Timer1
        ' 
        Timer1.Enabled = True
        Timer1.Interval = 50
        ' 
        ' Algorithms
        ' 
        Algorithms.FormattingEnabled = True
        Algorithms.Location = New Point(614, 39)
        Algorithms.Name = "Algorithms"
        Algorithms.Size = New Size(294, 33)
        Algorithms.TabIndex = 0
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(614, 11)
        Label2.Name = "Label2"
        Label2.Size = New Size(139, 25)
        Label2.TabIndex = 4
        Label2.Text = "Input Algorithm"
        ' 
        ' SortAlgorithms
        ' 
        SortAlgorithms.FormattingEnabled = True
        SortAlgorithms.Location = New Point(1230, 39)
        SortAlgorithms.Name = "SortAlgorithms"
        SortAlgorithms.Size = New Size(294, 33)
        SortAlgorithms.Sorted = True
        SortAlgorithms.TabIndex = 5
        SortAlgorithms.Visible = False
        ' 
        ' LoadData
        ' 
        LoadData.Location = New Point(1415, 78)
        LoadData.Name = "LoadData"
        LoadData.Size = New Size(133, 56)
        LoadData.TabIndex = 6
        LoadData.Text = "Load Data"
        LoadData.UseVisualStyleBackColor = True
        LoadData.Visible = False
        ' 
        ' CopyResultsBack
        ' 
        CopyResultsBack.Location = New Point(177, 26)
        CopyResultsBack.Name = "CopyResultsBack"
        CopyResultsBack.Size = New Size(159, 56)
        CopyResultsBack.TabIndex = 8
        CopyResultsBack.Text = "Touchup Results"
        CopyResultsBack.UseVisualStyleBackColor = True
        ' 
        ' rtb
        ' 
        rtb.Location = New Point(940, 31)
        rtb.Name = "rtb"
        rtb.Size = New Size(131, 51)
        rtb.TabIndex = 9
        rtb.Text = ""
        rtb.Visible = False
        ' 
        ' Timer2
        ' 
        Timer2.Interval = 1000
        ' 
        ' Timer3
        ' 
        ' 
        ' TranslatorForm
        ' 
        AutoScaleDimensions = New SizeF(10F, 25F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1555, 1604)
        Controls.Add(rtb)
        Controls.Add(CopyResultsBack)
        Controls.Add(LoadData)
        Controls.Add(SortAlgorithms)
        Controls.Add(Label2)
        Controls.Add(Algorithms)
        Controls.Add(Label1)
        Controls.Add(Translate)
        Controls.Add(WebView)
        FormBorderStyle = FormBorderStyle.FixedSingle
        KeyPreview = True
        MaximizeBox = False
        MinimizeBox = False
        Name = "TranslatorForm"
        Text = "OpenCVB Translate Assistant"
        CType(WebView, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents WebView As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents Translate As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents Timer1 As Timer
    Friend WithEvents Algorithms As ComboBox
    Friend WithEvents Label2 As Label
    Friend WithEvents SortAlgorithms As ComboBox
    Friend WithEvents LoadData As Button
    Friend WithEvents CopyResultsBack As Button
    Friend WithEvents rtb As RichTextBox
    Friend WithEvents Timer2 As Timer
    Friend WithEvents Timer3 As Timer

End Class
