Imports System.Windows.Forms
Imports System.IO
Module Program
    <STAThread()>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New Main.MainUI())
    End Sub
End Module