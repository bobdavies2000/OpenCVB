Imports System.Windows.Forms

Namespace MainUI
    Module Program
        <STAThread()>
        Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.Run(New MainUI)
        End Sub
    End Module
End Namespace

