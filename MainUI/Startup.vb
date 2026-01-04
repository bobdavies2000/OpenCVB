Imports System.Windows.Forms
Imports System.IO
Module Startup
    <STAThread()>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New MainApp.MainUI())
    End Sub
End Module