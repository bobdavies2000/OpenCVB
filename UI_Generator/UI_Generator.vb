Imports System.IO
Imports System.Text.RegularExpressions
Module UI_GeneratorMain
    Sub Main()
        Dim VBcodeDir As New DirectoryInfo(CurDir() + "/../../vb_classes/")

        ' first read all the cpp functions that are present in the project
        Dim functionInput As New FileInfo(VBcodeDir.FullName + "../CPP_Classes/CPP_IncludeOnly.h")
        Dim srFunctions = New StreamReader(functionInput.FullName)
        Dim functionNames As New SortedList(Of String, String)
        Dim unsortedFunctions As New List(Of String)
        Dim cppLineCount As Integer
        While srFunctions.EndOfStream = False
            Dim line = srFunctions.ReadLine()
            If line.Trim.Length > 0 Then cppLineCount += 1
            If line.Contains("enum functions") Then
                While 1
                    line = Trim(srFunctions.ReadLine())
                    If line = "{" Then Continue While
                    If line = "};" Then Exit While
                    Dim split = line.Split(",")
                    If split(0).Contains("MAX_FUNCTION") Then Continue While
                    functionNames.Add(split(0).Substring(1), split(0))
                    unsortedFunctions.Add(split(0).Substring(1))
                End While
            End If
        End While
        srFunctions.Close()

        Dim swInclude As New StreamWriter(VBcodeDir.FullName + "../CPP_Classes/CPP_Names.h")
        swInclude.WriteLine("#pragma once")
        swInclude.WriteLine("vector<String> functionNames({")
        For Each name In unsortedFunctions
            swInclude.WriteLine("""" + name + """,")
        Next
        swInclude.WriteLine("});")
        swInclude.Close()

        Dim fileNames As New List(Of String)
        Dim fileEntries As String() = Directory.GetFiles(VBcodeDir.FullName)

        Dim pythonAppDir As New IO.DirectoryInfo(VBcodeDir.FullName)

        ' we only want to review the python files that are included in the VB_Classes Project.  Other Python files may be support modules or just experiments.
        Dim projFile As New FileInfo(VBcodeDir.FullName + "/VB_Classes.vbproj")
        Dim readProj = New StreamReader(projFile.FullName)
        While readProj.EndOfStream = False
            Dim line = readProj.ReadLine()
            If Trim(line).StartsWith("<Content Include=") Then
                If InStr(line, ".py""") Then
                    Dim startName = InStr(line, "Include=""")
                    line = Mid(line, startName + Len("Include="""))
                    Dim endName = InStr(line, """")
                    line = Mid(line, 1, endName - 1)
                    Dim pyFilename = New FileInfo(VBcodeDir.FullName + "/" + line)
                    fileNames.Add(pyFilename.FullName)
                End If
            End If
            If Trim(line).StartsWith("<Compile Include=") Then
                If InStr(line, ".vb""") Then
                    Dim startname = InStr(line, "=") + 2
                    line = Mid(line, startname)
                    Dim endName = InStr(line, """")
                    line = Mid(line, 1, endName - 1)
                    If line.Contains("AlgorithmList.vb") = False And line.Contains("My Project") = False Then fileNames.Add(VBcodeDir.FullName + "/" + line)
                End If
            End If
        End While
        readProj.Close()

        Dim className As String = ""
        Dim CodeLineCount As Integer = cppLineCount ' now adding in the C++ lines...
        Dim sortedNames As New SortedList(Of String, Integer)
        Dim sIndex As Integer
        For Each fileName In fileNames
            If fileName.EndsWith(".py") Then
                Dim fileinfo As New FileInfo(fileName)
                sortedNames.Add(fileinfo.Name, sIndex)
                sIndex += 1
                fileName = fileinfo.FullName
            Else
                If fileName.EndsWith("VB_Algorithm.vb") = False Then ' And fileName.EndsWith("Options.vb") = False 
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

        Dim listInfo As New FileInfo(CurDir() + "/../../UI_Generator/AlgorithmList.vb")
        Dim sw As New StreamWriter(listInfo.FullName)
        sw.WriteLine("' this file is automatically generated in a pre-build step.  Do not waste your time modifying manually.")
        sw.WriteLine("Public Class algorithmList")
        sw.WriteLine("Public Enum functionNames")
        For i = 0 To unsortedFunctions.Count - 1
            sw.WriteLine(unsortedFunctions(i))
        Next
        sw.WriteLine("End Enum")
        sw.WriteLine("Public Function createAlgorithm( algorithmName as string) As Object")
        sw.WriteLine(vbTab + "If algorithmName.endsWith("".py"") then return new Python_Run()")
        For i = 0 To cleanNames.Count - 1
            Dim nextName = cleanNames(i)
            'If nextName.StartsWith("Options_") = False Then
            If nextName.EndsWith(".py") = False Then
                sw.WriteLine("if algorithmName = """ + nextName + """ Then return new " + nextName)
            End If
            If nextName.StartsWith("CPP_Basics") Then
                For j = 0 To functionNames.Count - 1
                    Dim functionText = functionNames.ElementAt(j).Key
                    sw.WriteLine("if algorithmName = """ + "CPP_" + functionText + """ Then return new CPP_Basics(functionNames." + functionText + ")")
                Next
            End If
            'End If
        Next

        sw.WriteLine("return nothing")
        sw.WriteLine("End Function")
        sw.WriteLine("End Class")
        sw.Close()

        Dim textInfo As New FileInfo(VBcodeDir.FullName + "/../Data/AlgorithmList.txt")
        sw = New StreamWriter(textInfo.FullName)
        sw.WriteLine("CodeLineCount = " + CStr(CodeLineCount))
        For i = 0 To cleanNames.Count - 1
            If cleanNames(i) <> "CPP_Basics" Then ' cleanNames(i).StartsWith("Options_") = False And
                sw.WriteLine(cleanNames(i))
            End If
            If cleanNames(i).StartsWith("CPP_Basics") Then
                For j = 0 To functionNames.Count - 1
                    Dim functionText = functionNames.ElementAt(j).Key
                    sw.WriteLine("CPP_" + functionText)
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
