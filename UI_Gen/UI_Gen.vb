Imports System.IO
Imports System.Text.RegularExpressions
Module UI_Gen
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
        Dim vbList As New SortedList(Of String, String)
        Dim pythonList As New SortedList(Of String, String)
        Dim cppList As New SortedList(Of String, String)
        Dim csList As New SortedList(Of String, String)
        Dim ccList As New SortedList(Of String, String)
        Dim cppManaged As New SortedList(Of String, String)
        Dim cppNative As New SortedList(Of String, String)
        Dim allButPython As New SortedList(Of String, String)
        Dim pyStream As New SortedList(Of String, String)
        Dim allList As New SortedList(Of String, String)
        For Each line In pyFiles
            If line.Contains("<Compile Include=") = False Then Continue For
            Dim split = line.Split("""")
            pythonList.Add(split(1), split(1))
            srcList.Add(PythonProjFile.DirectoryName + "\" + split(1))

            If split(1).EndsWith("_PS.py") Then pyStream.Add(split(1), split(1))
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
                        vbList.Add(classname, classname)
                        allButPython.Add(classname, classname)
                        allList.Add(classname, classname)
                    End If
                ElseIf line.StartsWith("public class ") Then
                    If line.EndsWith(" : VB_Parent") Then
                        Dim split As String() = Regex.Split(line, "\W+")
                        classname = split(2)
                        csList.Add(classname, classname)
                        allButPython.Add(classname, classname)
                        allList.Add(classname, classname)
                    End If
                ElseIf line.StartsWith("class") Then
                    If line.EndsWith("_CC") Or line.Contains(" : public CPP_Parent") Then
                        Dim split = line.Split(" ")
                        classname = split(1)
                        cppList.Add(classname, classname)
                        cppNative.Add(classname, classname)
                        allButPython.Add(classname, classname)
                        allList.Add(classname, classname)
                        ccList.Add(classname, classname)
                    End If
                ElseIf line.StartsWith("public ref class ") Then
                    If line.EndsWith(" : public VB_Parent") Then
                        Dim split = line.Split(" ")
                        classname = split(3)
                        cppList.Add(classname, classname)
                        cppManaged.Add(classname, classname)
                        allButPython.Add(classname, classname)
                        allList.Add(classname, classname)
                    End If
                End If
            Next
        Next


        ' CS output
        Dim CSlistInfo As New FileInfo(HomeDir.FullName + "CS_Classes\AlgorithmList.cs")
        Dim sw As New StreamWriter(CSlistInfo.FullName)
        sw.WriteLine("// this file is automatically generated in a pre-build step.  Any manual modifications will be lost.")
        sw.WriteLine("namespace CS_Classes")
        sw.WriteLine("{")
        sw.WriteLine(vbTab + "public class CSAlgorithmList")
        sw.WriteLine(vbTab + "{")
        sw.WriteLine(vbTab + vbTab + "public VB_Classes.VB_Parent createCSAlgorithm( string algorithmName)")
        sw.WriteLine(vbTab + vbTab + "{")
        For Each csName In csList.Keys
            sw.WriteLine(vbTab + vbTab + vbTab + "if (algorithmName == """ + csName + """) return new " + csName + "();")
        Next
        sw.WriteLine(vbTab + vbTab + vbTab + "return new AddWeighted_Basics_CS();")
        sw.WriteLine(vbTab + vbTab + "}")
        sw.WriteLine(vbTab + "}")
        sw.WriteLine("}")
        sw.Close()



        ' CPP_Enum.h
        sw = New StreamWriter(HomeDir.FullName + "CPP_Native/CPP_Enum.h")
        sw.WriteLine("#pragma once")
        sw.WriteLine("enum ccListFunctions")
        sw.WriteLine("{")
        For Each alg In ccList.Keys
            sw.WriteLine("_" + alg + ",")
        Next
        sw.WriteLine("};")
        sw.Close()



        ' C++ output
        'Dim CPPlistInfo As New FileInfo(HomeDir.FullName + "Main_UI\AlgorithmList.vb")
        'sw = New StreamWriter(CPPlistInfo.FullName)
        'sw.WriteLine("' this file is automatically generated in a pre-build step.  Any manual modifications will be lost.")
        'sw.WriteLine("Imports CS_Classes")
        'sw.WriteLine("Imports VB_Classes")
        'sw.WriteLine("Imports CPP_Classes")

        'sw.WriteLine("Public Class algorithmList")
        'sw.WriteLine("Public Enum ccFunctionNames")
        'For i = 0 To ccList.Count - 1
        '    sw.WriteLine(ccList(i))
        'Next
        'sw.WriteLine("End Enum")

        'sw.WriteLine(vbTab + "Public Function createAlgorithm(algorithmName as string) as Object")
        'sw.WriteLine(vbTab + "If algorithmName.endsWith("".py"") then return new Python_Run()")
        ''For Each cppName In cppSortedNames.Key
        ''    sw.WriteLine(vbTab + vbTab + "if algorithmName = """ + cppName + """ Then Return New " + cppName)
        ''Next
        'For Each nextName In cleanNames
        '    If nextName.StartsWith("CPP_Basics") Then Continue For
        '    If nextName.EndsWith("_CC") Then
        '        sw.WriteLine(vbTab + "If algorithmName = """ + nextName + """ Then return new CPP_Basics(ccFunctionNames._" + nextName + ")")
        '    Else
        '        If nextName.EndsWith(".py") = False Then
        '            sw.WriteLine(vbTab + "If algorithmName = """ + nextName + """ Then return new " + nextName)
        '        End If
        '    End If
        'Next
        'sw.WriteLine(vbTab + vbTab + "Return Nothing")
        'sw.WriteLine(vbTab + "End Function")
        'sw.WriteLine("End Class")
        'sw.Close()

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
