Imports System.IO
Imports System.Text.RegularExpressions
Module Program
    Sub Main(args As String())
        Dim CCInput = New FileInfo("../../../../CPP_Native/CPP_NativeClasses.h")
        If CCInput.Exists = False Then
            CCInput = New FileInfo("../../../CPP_Native/CPP_NativeClasses.h")
        End If
        If CCInput.Exists = False Then
            MsgBox("The UI_Generator code needs to be reviewed." + vbCrLf + "Either UI_Generator has moved or projects reference have." + vbCrLf +
                   CCInput.FullName + " was not found.")
            Exit Sub
        End If

        Dim HomeDir As New DirectoryInfo(CCInput.DirectoryName + "/../")
        Dim srcList As New List(Of String)({HomeDir.FullName + "CPP_Classes/CPP_Classes.cpp",    ' all the managed C++ code
                                            HomeDir.FullName + "CS_Classes/CS_AI_Generated.cs",  ' all the C# code
                                            HomeDir.FullName + "CPP_Native/CPP_NativeClasses.h", ' all the native C++ code
                                            HomeDir.FullName + "CS_Classes/Non_AI.cs"})          ' all the old-style native code.
        Dim VBcodeDir As New DirectoryInfo(HomeDir.FullName + "VB_classes/") ' all the vb algorithms are here.

        Dim OptionsFile = New FileInfo(VBcodeDir.FullName + "Options.vb")
        Dim includeOptions = New FileInfo(HomeDir.FullName + "CPP_Native/Options.h")

        Dim indexTestFile = New FileInfo(HomeDir.FullName + "/Data/AlgorithmGroupNames.txt")
#If DEBUG Then
        If indexTestFile.Exists Then indexTestFile.Delete() ' force app to run when in debug mode.
#End If
        If indexTestFile.Exists Then
            If checkDates(New DirectoryInfo(HomeDir.FullName + "/CS_Classes/"), indexTestFile) = False Then
                If checkDates(New DirectoryInfo(HomeDir.FullName + "/VB_Classes/"), indexTestFile) = False Then
                    If checkDates(New DirectoryInfo(HomeDir.FullName + "/CPP_Native/"), indexTestFile) = False Then
                        If checkDates(New DirectoryInfo(HomeDir.FullName + "/CPP_Classes/"), indexTestFile) = False Then
                            Console.WriteLine("The user interface is already up to date.")
                            Exit Sub ' nothing to trigger 
                        End If
                    End If
                End If
            End If
        End If
        Console.WriteLine("Starting work to generate the user interface.")

        Dim PythonProjFile As New FileInfo(HomeDir.FullName + "/Python/Python.pyproj")
        Dim pyFiles = File.ReadAllLines(PythonProjFile.FullName)
        Dim algorithmList As New List(Of String)
        For Each line In pyFiles
            If line.Contains("<Compile Include=") = False Then Continue For
            Dim split = line.Split("""")
            algorithmList.Add(split(1))
            srcList.Add(PythonProjFile.DirectoryName + "\" + split(1))
        Next
        Dim fileEntries As String() = Directory.GetFiles(VBcodeDir.FullName)
        For Each fn In fileEntries
            If fn.Contains(".Designer") Then Continue For
            If fn.Contains("AssemblyInfo") Then Continue For
            If fn.Contains(".resx") Then Continue For
            If fn.Contains(".vbproj") Then Continue For
            srcList.Add(fn)
        Next
        ' read all the code, count the lines, and get the algorithm list.

        Dim totalLines As Integer
        For Each fn In srcList
            Dim srclines = File.ReadAllLines(fn)
            Dim classname As String = ""
            For Each line In srclines
                line = Trim(line)
                If line.Length = 0 Then Continue For
                If line.StartsWith("//") Then Continue For
                If line.StartsWith("'") Then Continue For
                If line = "{" Or line = "}" Then Continue For

                totalLines += 1

                If fn.EndsWith(".py") Then Continue For
                If line.StartsWith("Public Class") Then
                    Dim split As String() = Regex.Split(line, "\W+")
                    If line.EndsWith(" : Inherits VB_Parent") Then
                        classname = split(2)
                        algorithmList.Add(classname)
                    End If

                End If
            Next
        Next
        Dim k = 0
    End Sub

    Private Function checkDates(dirInfo As DirectoryInfo, algorithmGroupNames As FileInfo) As Boolean
        For Each fileInfo As FileInfo In dirInfo.GetFiles()
            If fileInfo.Name = "VB_Common.vb" Then Continue For
            If fileInfo.Name = "VB_Parent.vb" Then Continue For
            If fileInfo.Name = "VB_Task.vb" Then Continue For
            If fileInfo.Name = "VB_Externs.vb" Then Continue For
            If fileInfo.Name.StartsWith("Options") Then Continue For
            Dim result As Integer = DateTime.Compare(fileInfo.LastWriteTime, algorithmGroupNames.LastWriteTime)
            If result > 0 Then Return True
        Next
        Return False
    End Function
End Module
