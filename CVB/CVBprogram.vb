Imports System.Windows.Forms

Namespace CVB
    Module Program
        <STAThread()>
        Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.Run(New MainForm)
        End Sub
    End Module
End Namespace

