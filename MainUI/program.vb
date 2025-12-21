Imports System.Windows.Forms
Imports System.IO

Namespace MainUI
    Module Program
        <STAThread()>
        Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            
            ' Find the MainUI project directory
            Dim executingAssemblyPath As String = System.Reflection.Assembly.GetExecutingAssembly().Location
            Dim currentDir = New DirectoryInfo(Path.GetDirectoryName(executingAssemblyPath))
            Dim projectDirectory As String = ""
            
            ' Navigate up the directory tree to find MainUI.vbproj
            While currentDir IsNot Nothing
                Dim vbprojFile = currentDir.GetFiles("MainUI.vbproj")
                If vbprojFile.Length > 0 Then
                    projectDirectory = currentDir.FullName
                    Exit While
                End If
                currentDir = currentDir.Parent
            End While
            
            Application.Run(New MainUI(projectDirectory))
        End Sub
    End Module
End Namespace

