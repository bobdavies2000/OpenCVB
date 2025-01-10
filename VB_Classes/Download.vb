Imports cv = OpenCvSharp
Imports  System.IO
Imports System.Net
Imports System.Threading
Public Class Download_Databases : Inherits TaskParent
    Dim downloadActive As Boolean
    Dim pythonActive As Boolean
    Dim linkAddress As String = ""
    Dim zippedBuffer As New MemoryStream
    Dim downloadIndex = -1
    Dim options As New Options_Databases
    Dim filename As String = ""
    Public Sub New()
        desc = "Multi-threaded (responsive) download of the iBug 300W face database.  Not using iBug yet but planning to..."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.RunOpt()

        'Dim fileToDecompress = New FileInfo(task.HomeDir + "Data/" + filename)
        'Dim downloadDir = New DirectoryInfo(task.HomeDir + "Data/" + Mid(fileToDecompress.Name, 1, Len(fileToDecompress.Name) - Len(".tar.gz")))
        'If downloadActive And pythonActive = False Then
        '    SetTrueText("Downloading active (takes a while).  Current download size = " + Format(zippedBuffer.Length / 1000, "###,##0") + "k bytes" + vbCrLf +
        '                  "Download is " + Format(zippedBuffer.Length / 1797000000, "#0%") + " complete", New cv.Point(40, 200))
        'Else
        '    If pythonActive Then
        '        SetTrueText("Unzipping files to " + downloadDir.FullName, New cv.Point(40, 200))
        '    Else
        '        If linkAddress <> "" Then
        '            If downloadDir.Exists Then
        '                SetTrueText("The database " + downloadDir.Name + " has been downloaded and is ready for use.", New cv.Point(40, 100))
        '                Exit Sub
        '            End If
        '            downloadActive = True
        '            Dim downloadThread = New Thread(
        '                Sub()
        '                    Dim client = HttpWebRequest.CreateHttp(linkAddress)
        '                    Dim response = client.GetResponse()
        '                    Dim responseStream = response.GetResponseStream()
        '                    zippedBuffer = New MemoryStream
        '                    If fileToDecompress.Exists = False Then
        '                        responseStream.CopyTo(zippedBuffer)
        '                        File.WriteAllBytes(fileToDecompress.FullName, zippedBuffer.ToArray)
        '                    End If

        '                    If fileToDecompress.Name.EndsWith(".tar.gz") Then
        '                        task.showConsoleLog = False
        '                        pythonActive = True
        '                        Dim pyScript = task.HomeDir + "Data/extractTarFiles.py"
        '                        Dim fs = New StreamWriter(pyScript)
        '                        fs.WriteLine("import tarfile")
        '                        fs.WriteLine("import os")
        '                        fs.WriteLine("os.chdir(""" + task.HomeDir + "Data/" + """)")
        '                        fs.WriteLine("tar = tarfile.open(""" + fileToDecompress.Name + """)")
        '                        fs.WriteLine("tar.extractall()")
        '                        fs.WriteLine("tar.close")
        '                        fs.Close()

        '                        task.pythonTaskName = pyScript
        '                        Dim p As New Process
        '                        p.StartInfo.FileName = "python"
        '                        p.StartInfo.WorkingDirectory = task.HomeDir + "Data"
        '                        p.StartInfo.Arguments = """" + task.pythonTaskName + """"
        '                        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
        '                        p.Start()
        '                        p.WaitForExit()

        '                        My.Computer.FileSystem.DeleteFile(pyScript)
        '                    End If
        '                    My.Computer.FileSystem.DeleteFile(fileToDecompress.FullName)
        '                    downloadActive = False
        '                    pythonActive = False
        '                End Sub)
        '            downloadThread.Start()
        '        Else
        '            SetTrueText("Check the database to be downloaded in the Options nearby.", New cv.Point(40, 200))
        '        End If
        '    End If
        'End If
    End Sub
End Class
