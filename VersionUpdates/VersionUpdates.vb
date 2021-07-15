Imports System.IO
Module VersionUpdates
    Sub Main()
        Dim openCVLibDir = New DirectoryInfo("OpenCV/Build/lib/Release/")
        If openCVLibDir.Exists Then
            Dim coreList = openCVLibDir.GetFiles("opencv_core*.lib")
            If coreList.Count <> 1 Then
                MsgBox("The OpenCV Core library has not been built!" + vbCrLf + "Have you run the 'PrepareTree.bat' file?")
            Else
                Dim coreLibName = New FileInfo(coreList(0).FullName)
                Dim outPragmaLibs As New FileInfo("CPP_Classes/PragmaLibs.h")
                Dim outPragmaLibsD As New FileInfo("CPP_Classes/PragmaLibsD.h")
                Dim libList = openCVLibDir.GetFiles("*.lib")
                Dim sw = New StreamWriter(outPragmaLibs.FullName)
                Dim swd = New StreamWriter(outPragmaLibsD.FullName)
                For Each libfile In libList
                    If libfile.Name.StartsWith("opencv_") And libfile.Name.Contains("python") = False Then
                        Dim nextName = "OpenCV/Build/lib/Release/" + libfile.Name
                        sw.WriteLine("#pragma comment(lib, """ + nextName + """)")
                        nextName = "OpenCV/Build/lib/Debug/" + libfile.Name.Replace(".lib", "d.lib")
                        swd.WriteLine("#pragma comment(lib, """ + nextName + """)")
                    End If
                Next
                sw.Close()
                swd.Close()
            End If
        Else
            MsgBox("OpenCV directory was not found.  Cannot prepare CPP_Classes/PragmaLibs.h")
        End If

        Dim myList = IO.Directory.GetDirectories("C:\.hunter\_Base", "Install", IO.SearchOption.AllDirectories)
        If myList.Count = 0 Then
            MsgBox("The c:\.Hunter package has not been installed.  Be sure to install OpenCVB with the Update_All.bat.")
        End If
        Dim nowTime = Now
        Dim minSeconds As Double = Double.MaxValue
        Dim mostRecentDir = New DirectoryInfo(myList(0))
        For i = 0 To myList.Count - 1
            Dim nextDir = New DirectoryInfo(myList(i))
            Dim dt = Directory.GetCreationTime(nextDir.FullName)
            Dim seconds = DateTime.Now.Subtract(dt).TotalSeconds
            If minSeconds > seconds Then
                minSeconds = seconds
                mostRecentDir = nextDir
            End If
        Next
        My.Computer.FileSystem.CopyDirectory(mostRecentDir.FullName, "Hunter", overwrite:=True)
    End Sub
End Module
