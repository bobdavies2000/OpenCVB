Imports System.Windows.Forms

Namespace MainForm
    Module Program
        <STAThread()>
        Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.Run(New MainForm)
        End Sub
    End Module
End Namespace

