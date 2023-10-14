Imports System.IO
Module VersionUpdates
    Sub Main()
        Dim homeDir = New DirectoryInfo(CurDir())
        Dim openCVLibDir = New DirectoryInfo(homeDir.FullName + "/OpenCV/Build/lib/Release/")
        Dim libraryError = True
        If openCVLibDir.Exists Then
            Dim coreList = openCVLibDir.GetFiles("opencv_*.lib")
            If coreList.Count > 1 Then
                Dim coreLibName = New FileInfo(coreList(0).FullName)
                Dim outPragmaLibs As New FileInfo(homeDir.FullName + "/CPP_Classes/PragmaLibs.h")
                Dim libList = openCVLibDir.GetFiles("*.lib")
                Dim sw = New StreamWriter(outPragmaLibs.FullName)

                sw.WriteLine("#ifdef DEBUG")
                For Each libfile In libList
                    If libfile.Name.StartsWith("opencv_") And libfile.Name.Contains("python") = False And libfile.Name.Contains("alphamat") = False Then
                        Dim nextName = "OpenCV/Build/lib/Debug/" + libfile.Name.Replace(".lib", "d.lib")
                        sw.WriteLine("#pragma comment(lib, """ + nextName + """)")
                    End If
                Next

                sw.WriteLine("#else")
                For Each libfile In libList
                    If libfile.Name.StartsWith("opencv_") And libfile.Name.Contains("python") = False And libfile.Name.Contains("alphamat") = False Then
                        Dim nextName = "OpenCV/Build/lib/Release/" + libfile.Name
                        sw.WriteLine("#pragma comment(lib, """ + nextName + """)")
                    End If
                Next

                sw.WriteLine("#endif")
                sw.Close()

                libraryError = False
            End If
        End If

        If libraryError Then
            MsgBox("The OpenCV Core library was not successfully built!" + vbCrLf + "The opencv/Build/lib/release should contain all the libraries" + vbCrLf +
                   "VersionUpdates build the pragmalibs.h from the lib names.  It is looking in: " + vbCrLf + openCVLibDir.FullName)
        Else
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
        End If
    End Sub
End Module
