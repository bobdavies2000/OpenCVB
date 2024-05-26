Imports System.IO
Imports System.Text.RegularExpressions
Module UI_GeneratorMain
    Sub Main()
        Dim cppAlgorithmInput = New FileInfo("../CPP_Classes/CPP_Algorithms.h")
        Dim Input = New FileInfo("../CPP_Classes/CPP_AI_Generated.h")
        Dim VBcodeDir As New DirectoryInfo(CurDir)
        If cppAlgorithmInput.Exists = False Then
            cppAlgorithmInput = New FileInfo("../../CPP_Classes/CPP_Algorithms.h")
            Input = New FileInfo("../../CPP_Classes/CPP_AI_Generated.h")
        End If

        If CurDir.Contains("CPP_Classes") Then
            VBcodeDir = New DirectoryInfo(CurDir() + "/../VB_classes/")
        Else
            VBcodeDir = New DirectoryInfo(CurDir() + "/../../VB_classes/")
        End If

        Dim includeOnly = File.ReadAllLines(Input.FullName)
        Dim cppLines As Integer
        For Each line In includeOnly
            line = Trim(line)
            If line.StartsWith("//") Then Continue For
            If line.Length = 0 Then Continue For
            cppLines += 1
        Next

        Dim AlgorithmList = File.ReadAllLines(Input.FullName)
        For Each line In AlgorithmList
            line = Trim(line)
            If line.StartsWith("//") Then Continue For
            If line.Length = 0 Then Continue For
            cppLines += 1
        Next

        ' first read all the cpp functions that are present in the project
        Dim functionInput As New FileInfo(VBcodeDir.FullName + "../CPP_Classes/CPP_Functions.h")
        Dim srFunctions = New StreamReader(functionInput.FullName)
        Dim functionNames As New SortedList(Of String, String)
        Dim unsortedFunctions As New List(Of String)
        While srFunctions.EndOfStream = False
            Dim line = srFunctions.ReadLine()
            If line.Contains("enum functions") Then
                While 1
                    line = Trim(srFunctions.ReadLine())
                    If line = "{" Then Continue While
                    If line = "};" Then Exit While
                    Dim split = line.Split(",")
                    If split(0).Contains("MAX_FUNCTION") Then Continue While
                    functionNames.Add(split(0).Substring(0), split(0))
                    unsortedFunctions.Add(split(0).Substring(0))
                End While
            End If
        End While
        srFunctions.Close()

        Dim fileNames As New List(Of String)
        Dim fileEntries As String() = Directory.GetFiles(VBcodeDir.FullName)

        Dim pythonAppDir As New IO.DirectoryInfo(VBcodeDir.FullName + "/../Python_Classes/")

        Dim projFile As New FileInfo(VBcodeDir.FullName + "/VB_Classes.vbproj")
        Dim readProj = New StreamReader(projFile.FullName)
        While readProj.EndOfStream = False
            Dim line = readProj.ReadLine()
            If Trim(line).StartsWith("<Compile Include=") Then
                If InStr(line, ".vb""") Then
                    Dim startname = InStr(line, "=") + 2
                    line = Mid(line, startname)
                    Dim endName = InStr(line, """")
                    line = Mid(line, 1, endName - 1)
                    If line.Contains("AlgorithmList.vb") = False And line.Contains("My Project") = False Then fileNames.Add(VBcodeDir.FullName + line)
                End If
            End If
        End While
        readProj.Close()

        ' we only want python files that are included in the Python_Classes Project.  Other Python files may be support modules or just experiments.
        Dim pythonFiles() As String = Directory.GetFiles(pythonAppDir.FullName, "*.py", SearchOption.AllDirectories)
        For Each pythonFile As String In pythonFiles
            fileNames.Add(pythonFile)
        Next

        Dim className As String = ""
        Dim CodeLineCount As Integer = cppLines ' now adding in the C++ lines...
        Dim sortedNames As New SortedList(Of String, Integer)
        Dim sIndex As Integer
        For Each fileName In fileNames
            If fileName.EndsWith(".py") And fileName.Contains("__init") = False Then
                Dim fileinfo As New FileInfo(fileName)
                sortedNames.Add(fileinfo.Name, sIndex)
                sIndex += 1
                fileName = fileinfo.FullName
            Else
                If fileName.EndsWith("VB_Algorithm.vb") = False Then
                    Dim nextFile As New System.IO.StreamReader(fileName)
                    While nextFile.Peek() <> -1
                        Dim line = Trim(nextFile.ReadLine())
                        line = Replace(line, vbTab, "")
                        If line IsNot Nothing Then
                            If line.Substring(0, 1) <> "'" Then
                                If Len(line) > 0 Then CodeLineCount += 1
                                If LCase(line).StartsWith("public class") Then
                                    Dim split As String() = Regex.Split(line, "\W+")
                                    If line.EndsWith(" : Inherits VB_Algorithm") Then className = split(2)
                                End If
                                If LCase(line).StartsWith("public sub new(") And sortedNames.ContainsKey(className) = False Then
                                    sortedNames.Add(className, sIndex)
                                    sIndex += 1
                                End If
                            End If
                        End If
                    End While
                End If
            End If
        Next

        Dim cleanNames As New List(Of String)
        Dim lastName As String = ""
        For i = 0 To sortedNames.Count - 1
            Dim nextName = sortedNames.ElementAt(i).Key
            If nextName <> lastName + ".py" Then cleanNames.Add(nextName)
            lastName = nextName
        Next

        Dim listInfo As New FileInfo(VBcodeDir.FullName + "../UI_Generator/AlgorithmList.vb")
        Dim sw As New StreamWriter(listInfo.FullName)
        sw.WriteLine("' this file is automatically generated in a pre-build step.  Do not waste your time modifying manually.")
        sw.WriteLine("Public Class AlgorithmList")
        sw.WriteLine("Public Enum functionNames")
        For i = 0 To unsortedFunctions.Count - 1
            sw.WriteLine(unsortedFunctions(i))
        Next
        sw.WriteLine("End Enum")
        sw.WriteLine("Public Function createAlgorithm( algorithmName as string) As VB_Algorithm")
        sw.WriteLine(vbTab + "If algorithmName.endsWith("".py"") then return new Python_Run()")
        For i = 0 To cleanNames.Count - 1
            Dim nextName = cleanNames(i)
            If nextName.EndsWith(".py") = False Then
                sw.WriteLine(vbTab + "if algorithmName = """ + nextName + """ Then return new " + nextName)
            End If
            If nextName.StartsWith("CPP_Basics") Then
                For j = 0 To functionNames.Count - 1
                    Dim functionText = functionNames.ElementAt(j).Key
                    Dim func = functionText
                    functionText = functionText.Substring(1)
                    sw.WriteLine("if algorithmName = """ + functionText + """ Then return new CPP_Basics(functionNames." + func + ")")
                Next
            End If
        Next

        sw.WriteLine("return nothing")
        sw.WriteLine("End Function")
        sw.WriteLine("End Class")
        sw.Close()

        Dim textInfo As New FileInfo(VBcodeDir.FullName + "/../Data/AlgorithmList.txt")
        sw = New StreamWriter(textInfo.FullName)
        sw.WriteLine("CodeLineCount = " + CStr(CodeLineCount))
        For i = 0 To cleanNames.Count - 1
            If cleanNames(i) <> "CPP_Basics" Then
                sw.WriteLine(cleanNames(i))
            End If
            If cleanNames(i).StartsWith("CPP_Basics") Then
                For j = 0 To functionNames.Count - 1
                    Dim functionText = functionNames.ElementAt(j).Key
                    sw.WriteLine(functionText.Substring(1))
                Next
            End If
        Next
        sw.Close()

        Dim FilesInfo As New FileInfo(VBcodeDir.FullName + "/../Data/FileNames.txt")
        sw = New StreamWriter(FilesInfo.FullName)
        For i = 0 To fileNames.Count - 1
            sw.WriteLine(fileNames(i))
        Next
        sw.Close()
    End Sub
End Module
