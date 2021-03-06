Imports System.IO
Module VersionOpenCV
    Sub Main()
        Dim cmakeFile = New FileInfo("../../OpenCV/Build/CMakeVars.txt")
        If cmakeFile.Exists Then
            Dim currentVersion = GetSetting("OpenCVB", "openCV_Version", "openCV_Version", "4.5.2")
            Dim fs = New StreamReader(cmakeFile.FullName)
            Dim line As String
            Dim offset As Integer
            Dim versionStr As String = ""
            While fs.EndOfStream = False
                line = fs.ReadLine
                offset = InStr(line, "OPENCV_LIBVERSION=")
                If offset > 0 Then
                    versionStr = Mid(line, offset + Len("OPENCV_LIBVERSION="))
                    Exit While
                End If
            End While
            fs.Close()
            Dim prFile As New FileInfo("../../Data/PragmaLibs.h")
            If prFile.Exists Then
                If versionStr = currentVersion Then Exit Sub
            End If
            SaveSetting("OpenCVB", "openCV_Version", "openCV_Version", versionStr)
        End If
        Dim openCVLibDir = New DirectoryInfo("../../OpenCV/Build/lib/Release/")
        If openCVLibDir.Exists Then
            Dim libList = openCVLibDir.GetFiles("*.lib")
            Dim sw = New StreamWriter("../../Data/PragmaLibs.h")
            Dim swd = New StreamWriter("../../Data/PragmaLibsD.h")
            For Each libfile In libList
                If libfile.Name.StartsWith("opencv_") Then
                    Dim nextName = "OpenCV/Build/lib/Release/" + libfile.Name
                    sw.WriteLine("#pragma comment(lib, """ + nextName + """)")
                    nextName = "OpenCV/Build/lib/Debug/" + libfile.Name.Replace(".lib", "d.lib")
                    swd.WriteLine("#pragma comment(lib, """ + nextName + """)")
                End If
            Next
            sw.Close()
            swd.Close()
        Else
            MsgBox("OpenCV directory was not found.  #pragma lib list cannot be prepared.")
        End If
    End Sub
End Module
