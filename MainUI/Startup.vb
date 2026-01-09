Imports System.Windows.Forms
Imports System.IO
Imports System.Threading
Imports System.Runtime.InteropServices
Module Startup
    <STAThread()>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        ' Show splash screen
        Dim splash As New MainApp.Splash()
        'splash.Show()
        Application.DoEvents()

        Dim mainForm As New MainApp.MainUI()
        mainForm.Show()

        splash.Close()
        splash.Dispose()
        splash = Nothing

        ' Run application with main form
        Application.Run(mainForm)
    End Sub
End Module