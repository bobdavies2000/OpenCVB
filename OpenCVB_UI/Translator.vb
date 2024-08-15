Public Class Translator
    Private Sub Translator_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        WebView.Url = New Uri("https://www.google.com") ' New Uri("https://www.codeconvert.ai/app")
        Me.Top = 0
        Me.Left = 0
        Label1.Left = 10
        Label1.Top = WebView.Top + WebView.Height + 3
    End Sub
End Class