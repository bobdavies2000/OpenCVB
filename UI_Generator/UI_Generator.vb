Imports System.IO
Imports System.Text.RegularExpressions
Module UI_GeneratorMain
    Sub Main()
        Console.WriteLine("Starting work to generate the user interface.")
        Dim cppAlgorithmInput = New FileInfo("../CPP_Classes/CPP_Algorithms.h")
        Dim CPPInput = New FileInfo("../CPP_Classes/CPP_AI_Generated.h")
        Dim CSInput = New FileInfo("../CS_Classes/CS_AI_Generated.cs")
        Dim VBcodeDir As New DirectoryInfo(CurDir() + "/../VB_classes/")
        If cppAlgorithmInput.Exists = False Then
            cppAlgorithmInput = New FileInfo("../../CPP_Classes/CPP_Algorithms.h")
            CPPInput = New FileInfo("../../CPP_Classes/CPP_AI_Generated.h")
            CSInput = New FileInfo("../../CS_Classes/CS_AI_Generated.cs")
            VBcodeDir = New DirectoryInfo(CurDir() + "/../../VB_classes/")
        End If
        Dim homeDir As New DirectoryInfo(VBcodeDir.FullName + "/../")

        Dim includeOnly = File.ReadAllLines(CPPInput.FullName)
        Dim cppLines As Integer
        For Each line In includeOnly
            line = Trim(line)
            If line.StartsWith("//") Then Continue For
            If line.Length = 0 Then Continue For
            cppLines += 1
        Next

        Dim AlgorithmList = File.ReadAllLines(CPPInput.FullName)
        For Each line In AlgorithmList
            line = Trim(line)
            If line.StartsWith("//") Then Continue For
            If line.Length = 0 Then Continue For
            cppLines += 1
        Next

        ' first read all the cpp functions that are present in the project
        Dim functionInput As New FileInfo(homeDir.FullName + "/CPP_Classes/CPP_Functions.h")
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

        Dim pythonAppDir As New IO.DirectoryInfo(homeDir.FullName + "/Python_Classes/")

        Dim vbProjFile As New FileInfo(homeDir.FullName + "/VB_Classes/VB_Classes.vbproj")
        Dim readVBProj = New StreamReader(vbProjFile.FullName)
        While readVBProj.EndOfStream = False
            Dim line = readVBProj.ReadLine()
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
        readVBProj.Close()

        ' we only want python files that are included in the Python_Classes Project.  Other Python files may be support modules or just experiments.
        Dim pythonFiles() As String = Directory.GetFiles(pythonAppDir.FullName, "*.py", SearchOption.AllDirectories)
        For Each pythonFile As String In pythonFiles
            fileNames.Add(pythonFile)
        Next

        Dim className As String = ""
        Dim CodeLineCount As Integer = cppLines  ' now adding in the C++ and C# lines...
        Dim sortedNames As New SortedList(Of String, Integer)
        Dim sIndex As Integer
        For Each fileName In fileNames
            If fileName.EndsWith(".py") And fileName.Contains("__init") = False Then
                Dim fileinfo As New FileInfo(fileName)
                sortedNames.Add(fileinfo.Name, sIndex)
                sIndex += 1
                fileName = fileinfo.FullName
            Else
                If fileName.EndsWith("VB_Parent.vb") = False Then
                    Dim nextFile As New System.IO.StreamReader(fileName)
                    While nextFile.Peek() <> -1
                        Dim line = Trim(nextFile.ReadLine())
                        line = Replace(line, vbTab, "")
                        If line IsNot Nothing Then
                            If line.Substring(0, 1) <> "'" Then
                                If Len(line) > 0 Then CodeLineCount += 1
                                If LCase(line).StartsWith("public class") Then
                                    Dim split As String() = Regex.Split(line, "\W+")
                                    If line.EndsWith(" : Inherits VB_Parent") Then className = split(2)
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

        Dim csSortedNames As New SortedList(Of String, Integer)
        Dim csIndex As Integer
        Dim csFileNames As New List(Of String)
        Dim csAdds As New List(Of String)
        ' we only want python files that are included in the Python_Classes Project.  Other Python files may be support modules or just experiments.
        Dim csAppDir As New IO.DirectoryInfo(homeDir.FullName + "/CS_Classes/")
        Dim csFiles() As String = Directory.GetFiles(csAppDir.FullName, "*.cs", SearchOption.AllDirectories)
        For Each csFile As String In csFiles
            csFileNames.Add(csFile)
        Next
        For Each fileName In csFileNames
            If fileName.EndsWith("CS_Parent.cs") = False Then
                Dim csName As String = ""
                Dim nextFile As New System.IO.StreamReader(fileName)
                While nextFile.Peek() <> -1
                    Dim line = Trim(nextFile.ReadLine())
                    If line Is Nothing Then Continue While
                    If line.Length > 1 Then
                        If line.Substring(0, 2) <> "//" Then
                            CodeLineCount += 1
                            If LCase(line).StartsWith("public class ") Then
                                Dim split As String() = Regex.Split(line, "\W+")
                                If line.EndsWith(" : CS_Parent") Then
                                    csName = split(2)
                                    If csAdds.Contains(fileName) = False Then
                                        csAdds.Add(fileName)
                                        fileNames.Add(fileName)
                                    End If
                                End If
                            End If
                            If LCase(line).StartsWith("public ") And csSortedNames.ContainsKey(csName) = False And csName <> "" Then
                                csSortedNames.Add(csName, csIndex)
                                csIndex += 1
                            End If
                        End If
                    End If
                End While
            End If
        Next


        Dim cleanNames As New List(Of String)
        Dim lastName As String = ""
        For i = 0 To sortedNames.Count - 1
            Dim nextName = sortedNames.ElementAt(i).Key
            If nextName <> lastName + ".py" Then cleanNames.Add(nextName)
            lastName = nextName
        Next


        ' CS output
        Dim CSlistInfo As New FileInfo(homeDir.FullName + "/CS_Classes/algorithmList.cs")
        Dim sw As New StreamWriter(CSlistInfo.FullName)
        sw.WriteLine("// this file is automatically generated in a pre-build step.  Do not waste your time modifying manually.")
        sw.WriteLine("using VB_Classes;")
        sw.WriteLine("using CS_Classes;")
        sw.WriteLine("namespace CS_Classes")
        sw.WriteLine("{")
        sw.WriteLine(vbTab + "public class CSAlgorithmList")
        sw.WriteLine(vbTab + "{")
        sw.WriteLine(vbTab + vbTab + "public CS_Parent createCSAlgorithm( string algorithmName, VB_Classes.VBtask task)")
        sw.WriteLine(vbTab + vbTab + "{")
        For Each csName In csSortedNames.Keys
            sw.WriteLine(vbTab + vbTab + vbTab + "if (algorithmName == """ + csName + """) return new " + csName + "(task);")
        Next
        sw.WriteLine(vbTab + vbTab + vbTab + "return new CSharp_AddWeighted_Basics(task);")
        sw.WriteLine(vbTab + vbTab + "}")
        sw.WriteLine(vbTab + "}")
        sw.WriteLine("}")
        sw.Close()





        Dim listInfo As New FileInfo(homeDir.FullName + "/UI_Generator/AlgorithmList.vb")
        sw = New StreamWriter(listInfo.FullName)
        sw.WriteLine("' this file is automatically generated in a pre-build step.  Do not waste your time modifying manually.")
        sw.WriteLine("Public Class AlgorithmList")
        sw.WriteLine("Public Enum functionNames")
        For i = 0 To unsortedFunctions.Count - 1
            sw.WriteLine(unsortedFunctions(i))
        Next
        sw.WriteLine("End Enum")

        sw.WriteLine("Public Function createVBAlgorithm( algorithmName as string) As VB_Parent")
        sw.WriteLine(vbTab + "If algorithmName.endsWith("".py"") then return new Python_Run()")
        ' sw.WriteLine(vbTab + "If algorithmName = ""CSharp_Basics"" then return new AddWeighted_Basics_CS(task)")
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

        Dim textInfo As New FileInfo(homeDir.FullName + "/Data/AlgorithmList.txt")
        sw = New StreamWriter(textInfo.FullName)
        sw.WriteLine("CodeLineCount = " + CStr(CodeLineCount))
        For i = 0 To cleanNames.Count - 1
            If cleanNames(i).StartsWith("CSV_Basics") Then
                For j = 0 To csSortedNames.Count - 1
                    sw.WriteLine(csSortedNames.ElementAt(j).Key)
                Next
                sw.WriteLine(cleanNames(i))
            ElseIf cleanNames(i).StartsWith("CPP_Basics") Then
                For j = 0 To functionNames.Count - 1
                    Dim functionText = functionNames.ElementAt(j).Key
                    sw.WriteLine(functionText.Substring(1))
                Next
            Else
                sw.WriteLine(cleanNames(i))
            End If
        Next
        sw.Close()

        Dim FilesInfo As New FileInfo(homeDir.FullName + "/Data/FileNames.txt")
        sw = New StreamWriter(FilesInfo.FullName)
        For i = 0 To fileNames.Count - 1
            sw.WriteLine(fileNames(i))
        Next
        sw.Close()
    End Sub
End Module
