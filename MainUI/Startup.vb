Imports System.Windows.Forms
Imports System.IO
Imports System.Threading
Imports System.Runtime.InteropServices
Module Startup
    <STAThread()>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Dim showSplash As Boolean = GetSetting("OpenCVB", "ShowSplash", "ShowSplash", True)
        Dim splash As New MainApp.Splash()

        Dim Splashmessage = GetSetting("OpenCVB", "SplashMessage", "SplashMessage", "Loading...")
        splash.loadingLabel.Text = Splashmessage

        If showSplash Then splash.Show()
        Application.DoEvents()

        Dim mainForm As New MainApp.MainUI()
        Application.DoEvents()
        mainForm.Show()

        splash.Close()
        splash.Dispose()
        splash = Nothing
        Try
            Application.Run(mainForm)
        Catch ex As Exception
            Debug.WriteLine("Application Run Exception: " & ex.Message)
        End Try
    End Sub
End Module