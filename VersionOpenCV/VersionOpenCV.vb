Imports System.IO
Module VersionOpenCV
    Sub Main()
        Dim openCVLibDir = New DirectoryInfo("../../OpenCV/Build/lib/Release/")
        If openCVLibDir.Exists Then
            Dim coreList = openCVLibDir.GetFiles("opencv_core*.lib")
            If coreList.Count <> 1 Then
                MsgBox("The OpenCV Core library has not been built!" + vbCrLf + "Have you run the 'PrepareTree.bat' file?")
            Else
                Dim coreLibName = New FileInfo(coreList(0).FullName)
                Dim currentCore = New FileInfo(GetSetting("OpenCVB", "CoreLibName", "CoreLibName", "NotThereYet"))
                If coreLibName.FullName <> currentCore.FullName Then
                    Dim libList = openCVLibDir.GetFiles("*.lib")
                    Dim sw = New StreamWriter("../../Data/PragmaLibs.h")
                    Dim swd = New StreamWriter("../../Data/PragmaLibsD.h")
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
                    SaveSetting("OpenCVB", "CoreLibName", "CoreLibName", coreLibName.FullName)
                End If
            End If
        Else
            MsgBox("OpenCV directory was not found.  #pragma lib list cannot be prepared.")
        End If
    End Sub
End Module
